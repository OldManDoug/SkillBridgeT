using SkillBridge.Web.Features.Administration;
using SkillBridge.Web.Infrastructure.Data;

namespace SkillBridge.Web.Infrastructure.Administration;

public sealed class MySqlDashboardReader(MySqlConnectionFactory connectionFactory) : IDashboardReader
{
    public async Task<DashboardSummary> GetSummaryAsync(CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        // These counts come from the database on each visit, so the dashboard reflects saved changes.
        command.CommandText = """
            SELECT (SELECT COUNT(*) FROM users u INNER JOIN roles r ON r.id = u.role_id WHERE r.name = 'Member') AS members,
                   (SELECT COUNT(*) FROM courses WHERE is_published = TRUE) AS published_courses,
                   (SELECT COUNT(*) FROM courses WHERE is_published = FALSE) AS draft_courses,
                   (SELECT COUNT(*) FROM lessons) AS lessons,
                   (SELECT COUNT(*) FROM users) AS users,
                   (SELECT COUNT(*) FROM enrolments) AS enrolments,
                   (SELECT COUNT(*) FROM attempts) AS quiz_attempts,
                   (SELECT COUNT(*) FROM forum_threads) AS threads;
            """;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);
        return new DashboardSummary(reader.GetInt32("members"), reader.GetInt32("published_courses"), reader.GetInt32("draft_courses"), reader.GetInt32("lessons"))
        {
            Users = reader.GetInt32("users"),
            Enrolments = reader.GetInt32("enrolments"),
            QuizAttempts = reader.GetInt32("quiz_attempts"),
            Threads = reader.GetInt32("threads")
        };
    }
}
