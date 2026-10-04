using Microsoft.AspNetCore.Mvc.RazorPages;
using SkillBridge.Web.Features.Courses;

namespace SkillBridge.Web.Pages;

public sealed class IndexModel(ICourseReader courses) : PageModel
{
    public IReadOnlyList<CourseSummary> FeaturedCourses { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        // Reuse the catalogue reader so the home page only advertises published courses.
        FeaturedCourses = (await courses.GetPublishedAsync(null, cancellationToken)).Take(2).ToArray();
    }
}
