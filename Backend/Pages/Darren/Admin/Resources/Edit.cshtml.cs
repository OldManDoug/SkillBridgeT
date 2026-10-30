using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace SkillBridge.Backend.Pages.Darren.Admin.Resources
{
    public class ResourceFormModel
    {
        public long Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public long LessonId { get; set; }
        public string Type { get; set; } = "PDF";
        public string Description { get; set; } = string.Empty;
        public string? Transcript { get; set; }
    }

    public class EditModel : PageModel
    {
        [BindProperty]
        public ResourceFormModel Resource { get; set; } = new();

        [BindProperty]
        public IFormFile? UploadedFile { get; set; }

        public bool IsEditMode => Resource.Id > 0;

        public IActionResult OnGet(long? id)
        {
            if (id.HasValue) return NotFound();
            return Page();
        }

        public IActionResult OnPost()
        {
            ModelState.AddModelError(string.Empty, "Resource upload is not implemented yet. No file was saved.");
            return Page();
        }
    }
}
