namespace SkillBridge.Web.Features.Administration;

// The service uses this contract without needing to know MySQL commands or connection details.
public interface IAdminUserStore
{
    Task<AdminUserPage> SearchAsync(AdminUserSearch search, CancellationToken cancellationToken);
    Task<AdminUser> GetAsync(long userId, CancellationToken cancellationToken);
    Task<AdminUser> CreateAsync(long actingAdminId, AdminUserInput input, string normalizedEmail, string passwordHash, string securityStamp, CancellationToken cancellationToken);
    Task<AdminUser> UpdateAsync(long actingAdminId, long userId, string expectedStamp, AdminUserInput input, string normalizedEmail, string securityStamp, CancellationToken cancellationToken);
    Task DeleteAsync(long actingAdminId, long userId, string expectedStamp, CancellationToken cancellationToken);
}
