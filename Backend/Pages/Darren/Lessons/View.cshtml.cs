using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkillBridge.Web.Features.Lessons;

namespace SkillBridge.Web.Pages.Lessons;

[Authorize]
public sealed class ViewModel(ILessonReader lessons, SkillBridge.Backend.Darren.Lessons.ILessonReader progress) : PageModel
{
    public bool IsCompleted { get; private set; }
    public LessonDetail Detail { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync(long id, CancellationToken cancellationToken)
    {
        if (id <= 0) return NotFound();
        if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Challenge();

        var lesson = await lessons.GetByIdAsync(id, userId, User.IsInRole("Admin"), cancellationToken);
        if (lesson is null) return NotFound();

        Detail = lesson;
        IsCompleted = await progress.IsLessonCompletedAsync(userId, id);
        return Page();
    }
    public async Task<IActionResult> OnPostToggleCompletionAsync(long id, CancellationToken cancellationToken)
    {
        var result = await OnGetAsync(id, cancellationToken);
        if (result is not PageResult) return result;
        if (User.IsInRole("Admin")) return Forbid();
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await progress.ToggleLessonCompletionAsync(userId, id);
        return RedirectToPage(new { id });
    }
}
