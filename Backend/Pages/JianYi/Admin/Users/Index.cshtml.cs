using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkillBridge.Web.Features.Accounts;
using SkillBridge.Web.Features.Administration;
using SkillBridge.Web.Infrastructure.Authentication;
using SkillBridge.Web.Infrastructure.Data;

namespace SkillBridge.Web.Pages.Admin.Users;

[Authorize(Roles = "Admin")]
public sealed class IndexModel(AdminUserService users) : PageModel
{
    // SupportsGet lets the search form and paging links supply these query-string values.
    [BindProperty(SupportsGet = true), StringLength(100)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true), EnumDataType(typeof(UserRole))]
    public UserRole? Role { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool? IsActive { get; set; }

    [BindProperty(SupportsGet = true), Range(1, 100000)]
    public int PageNumber { get; set; } = 1;

    [TempData]
    public string? StatusMessage { get; set; }

    public AdminUserPage Results { get; private set; } = new([], 0, 1, 20);
    public bool HasLoaded { get; private set; }
    public long CurrentUserId => AccountSession.UserId(User);

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return Page();

        await LoadUsersAsync(cancellationToken);
        return Page();
    }

    // A handler named OnPostDeleteAsync receives only POSTs with ?handler=Delete.
    // Razor Pages checks the form's anti-forgery token before this method runs.
    public async Task<IActionResult> OnPostDeleteAsync(
        [FromForm] long userId,
        [FromForm] string? expectedStamp,
        [FromForm] bool confirmDelete,
        CancellationToken cancellationToken)
    {
        if (!confirmDelete)
            ModelState.AddModelError(string.Empty, "Tick the confirmation box before deleting a user.");

        if (userId <= 0 || expectedStamp is null || !Regex.IsMatch(expectedStamp, "\\A[0-9a-f]{32}\\z"))
            ModelState.AddModelError(string.Empty, "The user details are invalid. Refresh the list and try again.");

        if (!ModelState.IsValid)
        {
            await LoadUsersAsync(cancellationToken);
            return Page();
        }

        try
        {
            // The original stamp prevents a stale page from deleting a recently changed account.
            // The service also checks admin access, dependencies and protected accounts.
            await users.DeleteAsync(CurrentUserId, userId, expectedStamp!, cancellationToken);
            StatusMessage = "The unused user account has been deleted.";

            // Redirect after a successful POST so refreshing the page cannot repeat the deletion.
            return RedirectToPage(new { Search, Role, IsActive, PageNumber });
        }
        catch (AdminUserNotFoundException)
        {
            ModelState.AddModelError(string.Empty, "This user no longer exists. The list has been refreshed.");
        }
        catch (AdminUserChangedException)
        {
            ModelState.AddModelError(string.Empty, "This account changed after you opened the list. Review the refreshed details before trying again.");
        }
        catch (AdminUserProtectedException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
        }
        catch (AdminUserInUseException)
        {
            ModelState.AddModelError(string.Empty, "This user has learning or discussion records and cannot be deleted. Edit the user and deactivate the account instead.");
        }
        catch (AdminAccessDeniedException)
        {
            return Forbid();
        }
        catch (ValidationException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
        }
        catch (DatabaseUnavailableException)
        {
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            ModelState.AddModelError(string.Empty, "User management is temporarily unavailable. Please try again shortly.");
            return Page();
        }

        await LoadUsersAsync(cancellationToken);
        return Page();
    }

    private async Task LoadUsersAsync(CancellationToken cancellationToken)
    {
        // Invalid filters must not be sent to the database, even when a delete form was submitted.
        if (Search?.Length > 100 || Role is { } role && !Enum.IsDefined(role) || PageNumber is < 1 or > 100000)
            return;

        try
        {
            Results = await users.SearchAsync(new AdminUserSearch(Search?.Trim() ?? string.Empty, Role, IsActive, PageNumber), cancellationToken);
            HasLoaded = true;
        }
        catch (DatabaseUnavailableException)
        {
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            ModelState.AddModelError(string.Empty, "The user list is temporarily unavailable. Please try again shortly.");
        }
    }
}
