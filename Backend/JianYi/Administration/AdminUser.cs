using System.ComponentModel.DataAnnotations;
using SkillBridge.Web.Features.Accounts;

namespace SkillBridge.Web.Features.Administration;

// These records carry only the fields the admin screens need; passwords are never returned.
public sealed record AdminUser(long Id, string DisplayName, string Email, UserRole Role, bool IsActive, string SecurityStamp, DateTime CreatedAtUtc);

public sealed record AdminUserSearch(string Query = "", UserRole? Role = null, bool? IsActive = null, int Page = 1);

public sealed record AdminUserPage(IReadOnlyList<AdminUser> Items, int TotalCount, int Page, int PageSize)
{
    public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)TotalCount / PageSize));
}

public sealed record AdminUserInput(
    [property: Required, StringLength(80, MinimumLength = 2)] string DisplayName,
    [property: Required, EmailAddress, StringLength(254)] string Email,
    [property: EnumDataType(typeof(UserRole))] UserRole Role,
    bool IsActive);

public sealed class AdminUserNotFoundException() : Exception("This account no longer exists.");

public sealed class AdminUserChangedException() : Exception("This account changed after you opened it. Reload the page before trying again.");

public sealed class AdminUserProtectedException(string message) : Exception(message);

public sealed class AdminUserInUseException(Exception innerException)
    : Exception("This account has learning or discussion records. Deactivate it instead of deleting it.", innerException);

public sealed class AdminAccessDeniedException() : Exception("Your account no longer has administrator access. Sign in again.");
