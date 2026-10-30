using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkillBridge.Web.Features.Community;

namespace SkillBridge.Web.Pages.Hamzah.Forum;

public sealed class IndexModel(IForumReader forum) : PageModel
{
    public IReadOnlyList<DiscussionSummary> Discussions { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        Discussions = await forum.GetRecentAsync(cancellationToken);
        return Page();
    }
}