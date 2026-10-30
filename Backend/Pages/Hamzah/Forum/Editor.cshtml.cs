using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkillBridge.Web.Features.Community;

namespace SkillBridge.Web.Pages.Hamzah.Forum;

[Authorize]
public sealed class EditorModel(IForumReader forum, IForumWriter forumWriter) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public long? Id { get; set; }

    [BindProperty]
    [Required(ErrorMessage = "Please give your discussion a title.")]
    [StringLength(120, MinimumLength = 5, ErrorMessage = "Titles must be between 5 and 120 characters.")]
    public string? Title { get; set; }

    [BindProperty]
    [Required(ErrorMessage = "Please write the discussion body.")]
    [StringLength(5000, MinimumLength = 10, ErrorMessage = "The body must be between 10 and 5,000 characters.")]
    public string? Body { get; set; }

    public bool IsEditing => Id is > 0;

    private bool TryUserId(out long userId) =>
        long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId) && userId > 0;

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (!IsEditing) return Page();
        if (!TryUserId(out var userId)) return Challenge();

        var thread = await forum.GetThreadAsync(Id!.Value, cancellationToken);
        if (thread is null) return NotFound();
        if (thread.AuthorId != userId && !User.IsInRole("Admin")) return Forbid();

        Title = thread.Title;
        Body = thread.Body;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!TryUserId(out var userId)) return Challenge();
        Title = Title?.Trim();
        Body = Body?.Trim();
        if (Title?.Length < 5) ModelState.AddModelError(nameof(Title), "Use at least 5 characters after trimming spaces.");
        if (Body?.Length < 10) ModelState.AddModelError(nameof(Body), "Use at least 10 characters after trimming spaces.");
        if (!ModelState.IsValid) return Page();

        if (IsEditing)
        {
            var updated = await forumWriter.UpdateThreadAsync(Id!.Value, userId, User.IsInRole("Admin"), Title!.Trim(), Body!.Trim(), cancellationToken);
            if (!updated) return Forbid();

            return RedirectToPage("/Hamzah/Forum/View", new { id = Id.Value });
        }

        var threadId = await forumWriter.CreateThreadAsync(userId, Title!.Trim(), Body!.Trim(), cancellationToken);
        return RedirectToPage("/Hamzah/Forum/View", new { id = threadId });
    }
}