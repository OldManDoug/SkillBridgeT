using MySqlConnector;
using SkillBridge.Web.Features.Accounts;
using SkillBridge.Web.Features.Administration;
using SkillBridge.Web.Infrastructure.Data;

namespace SkillBridge.Web.Infrastructure.Administration;

public sealed class MySqlAdminUserStore(MySqlConnectionFactory connections) : IAdminUserStore
{
    private const int PageSize = 20;
    private const string UserColumns = "u.id, u.display_name, u.email, r.name AS role_name, u.is_active, u.security_stamp, u.created_at_utc";
    private const string SearchFilter = """
        WHERE (@hasQuery = FALSE OR u.display_name LIKE @query ESCAPE '!' OR u.email LIKE @query ESCAPE '!')
          AND (@hasRole = FALSE OR r.name = @role)
          AND (@hasStatus = FALSE OR u.is_active = @isActive)
        """;

    public async Task<AdminUserPage> SearchAsync(AdminUserSearch search, CancellationToken cancellationToken)
    {
        await using var connection = await connections.OpenAsync(cancellationToken);
        await using var countCommand = new MySqlCommand($"SELECT COUNT(*) FROM users u JOIN roles r ON r.id = u.role_id {SearchFilter}", connection);
        AddSearchParameters(countCommand, search);
        var totalCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync(cancellationToken));
        var totalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / PageSize));
        var page = Math.Clamp(search.Page, 1, totalPages);

        // Parameters keep search text separate from SQL; LIMIT keeps each request small.
        await using var command = new MySqlCommand($"""
            SELECT {UserColumns} FROM users u JOIN roles r ON r.id = u.role_id
            {SearchFilter} ORDER BY u.id DESC LIMIT @pageSize OFFSET @offset
            """, connection);
        AddSearchParameters(command, search);
        command.Parameters.AddWithValue("@pageSize", PageSize);
        command.Parameters.AddWithValue("@offset", (long)(page - 1) * PageSize);
        var users = await ReadUsersAsync(command, cancellationToken);
        return new AdminUserPage(users, totalCount, page, PageSize);
    }

    public async Task<AdminUser> GetAsync(long userId, CancellationToken cancellationToken)
    {
        await using var connection = await connections.OpenAsync(cancellationToken);
        return await ReadUserAsync(connection, null, userId, cancellationToken);
    }

    public async Task<AdminUser> CreateAsync(long actingAdminId, AdminUserInput input, string normalizedEmail, string passwordHash, string securityStamp, CancellationToken cancellationToken)
    {
        await using var connection = await connections.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await LockAndCheckAdminAsync(connection, transaction, actingAdminId, actingAdminId, cancellationToken);

        await using var command = new MySqlCommand("""
            INSERT INTO users (display_name, email, normalized_email, password_hash, role_id, is_active, security_stamp)
            VALUES (@name, @email, @normalizedEmail, @passwordHash, (SELECT id FROM roles WHERE name = @role), @isActive, @stamp)
            """, connection, transaction);
        AddInputParameters(command, input, normalizedEmail, securityStamp);
        command.Parameters.AddWithValue("@passwordHash", passwordHash);
        await ExecuteUniqueAsync(command, cancellationToken);
        var user = await ReadUserAsync(connection, transaction, command.LastInsertedId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return user;
    }

    public async Task<AdminUser> UpdateAsync(long actingAdminId, long userId, string expectedStamp, AdminUserInput input, string normalizedEmail, string securityStamp, CancellationToken cancellationToken)
    {
        await using var connection = await connections.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var user = await LockAndCheckAdminAsync(connection, transaction, actingAdminId, userId, cancellationToken);
        CheckUnchanged(user, expectedStamp);
        await ProtectAdminAsync(connection, transaction, actingAdminId, user, input.Role, input.IsActive, cancellationToken);

        await using var command = new MySqlCommand("""
            UPDATE users SET display_name = @name, email = @email, normalized_email = @normalizedEmail,
                role_id = (SELECT id FROM roles WHERE name = @role), is_active = @isActive, security_stamp = @stamp
            WHERE id = @id AND security_stamp = @expectedStamp
            """, connection, transaction);
        AddInputParameters(command, input, normalizedEmail, securityStamp);
        command.Parameters.AddWithValue("@id", userId);
        command.Parameters.AddWithValue("@expectedStamp", expectedStamp);
        if (await ExecuteUniqueAsync(command, cancellationToken) != 1)
            throw new AdminUserChangedException();

        await transaction.CommitAsync(cancellationToken);
        return user with { DisplayName = input.DisplayName, Email = input.Email, Role = input.Role, IsActive = input.IsActive, SecurityStamp = securityStamp };
    }

    public async Task DeleteAsync(long actingAdminId, long userId, string expectedStamp, CancellationToken cancellationToken)
    {
        await using var connection = await connections.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var user = await LockAndCheckAdminAsync(connection, transaction, actingAdminId, userId, cancellationToken);
        CheckUnchanged(user, expectedStamp);
        if (actingAdminId == userId)
            throw new AdminUserProtectedException("You cannot delete your own administrator account.");

        await ProtectAdminAsync(connection, transaction, actingAdminId, user, user.Role, false, cancellationToken);
        await using var command = new MySqlCommand("DELETE FROM users WHERE id = @id AND security_stamp = @expectedStamp", connection, transaction);
        command.Parameters.AddWithValue("@id", userId);
        command.Parameters.AddWithValue("@expectedStamp", expectedStamp);
        try
        {
            // Foreign keys refuse deletion when any learning or discussion record still uses this account.
            if (await command.ExecuteNonQueryAsync(cancellationToken) != 1)
                throw new AdminUserChangedException();
        }
        catch (MySqlException exception) when (exception.Number == 1451)
        {
            throw new AdminUserInUseException(exception);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task<AdminUser> LockAndCheckAdminAsync(MySqlConnection connection, MySqlTransaction transaction, long actingAdminId, long targetUserId, CancellationToken cancellationToken)
    {
        // All admin changes lock this same row first, so two requests cannot remove the last admin together.
        await using var roleCommand = new MySqlCommand("SELECT id FROM roles WHERE name = 'Admin' FOR UPDATE", connection, transaction);
        if (await roleCommand.ExecuteScalarAsync(cancellationToken) is null)
            throw new InvalidOperationException("The Admin role is missing. Import the database schema first.");

        await using var command = new MySqlCommand($"""
            SELECT {UserColumns} FROM users u JOIN roles r ON r.id = u.role_id
            WHERE u.id IN (@actorId, @targetId) ORDER BY u.id FOR UPDATE
            """, connection, transaction);
        command.Parameters.AddWithValue("@actorId", actingAdminId);
        command.Parameters.AddWithValue("@targetId", targetUserId);
        var users = await ReadUsersAsync(command, cancellationToken);
        var actor = users.FirstOrDefault(user => user.Id == actingAdminId);
        if (actor is not { Role: UserRole.Admin, IsActive: true })
            throw new AdminAccessDeniedException();

        // Check the database again here: a previously issued login cookie may hold an outdated role.
        return users.FirstOrDefault(user => user.Id == targetUserId) ?? throw new AdminUserNotFoundException();
    }

    private static async Task ProtectAdminAsync(MySqlConnection connection, MySqlTransaction transaction, long actingAdminId, AdminUser user, UserRole newRole, bool isActive, CancellationToken cancellationToken)
    {
        if (user.Role != UserRole.Admin || !user.IsActive || (newRole == UserRole.Admin && isActive))
            return;

        if (actingAdminId == user.Id)
            throw new AdminUserProtectedException("You cannot deactivate your own administrator account or remove your own Admin role.");

        await using var command = new MySqlCommand("""
            SELECT COUNT(*) FROM users u JOIN roles r ON r.id = u.role_id
            WHERE r.name = 'Admin' AND u.is_active = TRUE
            """, connection, transaction);
        if (Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) <= 1)
            throw new AdminUserProtectedException("At least one active administrator must remain.");
    }

    private static void CheckUnchanged(AdminUser user, string expectedStamp)
    {
        if (user.SecurityStamp != expectedStamp)
            throw new AdminUserChangedException();
    }

    private static async Task<AdminUser> ReadUserAsync(MySqlConnection connection, MySqlTransaction? transaction, long userId, CancellationToken cancellationToken)
    {
        await using var command = new MySqlCommand($"SELECT {UserColumns} FROM users u JOIN roles r ON r.id = u.role_id WHERE u.id = @id", connection, transaction);
        command.Parameters.AddWithValue("@id", userId);
        var users = await ReadUsersAsync(command, cancellationToken);
        return users.SingleOrDefault() ?? throw new AdminUserNotFoundException();
    }

    private static async Task<IReadOnlyList<AdminUser>> ReadUsersAsync(MySqlCommand command, CancellationToken cancellationToken)
    {
        var users = new List<AdminUser>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            users.Add(new AdminUser(reader.GetInt64("id"), reader.GetString("display_name"), reader.GetString("email"),
                Enum.Parse<UserRole>(reader.GetString("role_name")), reader.GetBoolean("is_active"), reader.GetString("security_stamp"),
                DateTime.SpecifyKind(reader.GetDateTime("created_at_utc"), DateTimeKind.Utc)));
        }

        return users;
    }

    private static async Task<int> ExecuteUniqueAsync(MySqlCommand command, CancellationToken cancellationToken)
    {
        try
        {
            return await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (MySqlException exception) when (exception.Number == 1062)
        {
            throw new DuplicateEmailException(exception);
        }
    }

    private static void AddInputParameters(MySqlCommand command, AdminUserInput input, string normalizedEmail, string securityStamp)
    {
        command.Parameters.AddWithValue("@name", input.DisplayName);
        command.Parameters.AddWithValue("@email", input.Email);
        command.Parameters.AddWithValue("@normalizedEmail", normalizedEmail);
        command.Parameters.AddWithValue("@role", input.Role.ToString());
        command.Parameters.AddWithValue("@isActive", input.IsActive);
        command.Parameters.AddWithValue("@stamp", securityStamp);
    }

    private static void AddSearchParameters(MySqlCommand command, AdminUserSearch search)
    {
        var escapedQuery = search.Query.Replace("!", "!!").Replace("%", "!%").Replace("_", "!_");
        command.Parameters.AddWithValue("@hasQuery", search.Query.Length > 0);
        command.Parameters.AddWithValue("@query", $"%{escapedQuery}%");
        command.Parameters.AddWithValue("@hasRole", search.Role.HasValue);
        command.Parameters.AddWithValue("@role", search.Role?.ToString() ?? string.Empty);
        command.Parameters.AddWithValue("@hasStatus", search.IsActive.HasValue);
        command.Parameters.AddWithValue("@isActive", search.IsActive ?? false);
    }
}
