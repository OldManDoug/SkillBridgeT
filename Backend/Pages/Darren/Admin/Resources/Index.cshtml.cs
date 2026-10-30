using Microsoft.AspNetCore.Mvc.RazorPages;
using SkillBridge.Backend.Darren.Lessons;
namespace SkillBridge.Backend.Pages.Darren.Admin.Resources;
public class IndexModel(ILessonReader lessons) : PageModel
{
    public List<SkillBridge.Backend.Darren.Lessons.ResourceItem> Resources { get; private set; } = [];
    public async Task OnGetAsync() => Resources = await lessons.GetResourcesAsync(null, null);
}
