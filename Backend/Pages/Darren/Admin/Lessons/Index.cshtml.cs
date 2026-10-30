using System.Collections.Generic;
using System.Threading.Tasks;
using MySqlConnector;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkillBridge.Backend.Darren.Lessons;

namespace SkillBridge.Backend.Pages.Darren.Admin.Lessons
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly ILessonReader _lessonReader;

        public IndexModel(ILessonReader lessonReader)
        {
            _lessonReader = lessonReader;
        }

        public List<LessonItem> Lessons { get; set; } = new();
        public List<CourseOption> Courses { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public long? CourseId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        [TempData]
        public string? StatusMessage { get; set; }

        public async Task OnGetAsync()
        {
            Courses = await _lessonReader.GetCourseOptionsAsync();
            Lessons = await _lessonReader.GetLessonsAsync(CourseId, Search);
        }

        public async Task<IActionResult> OnPostDeleteAsync(long id)
        {
            try
            {
                await _lessonReader.DeleteLessonAsync(id);
                StatusMessage = "Lesson deleted successfully.";
            }
            catch (MySqlException exception) when (exception.Number == 1451)
            {
                StatusMessage = "This lesson has resources or learner progress and cannot be deleted.";
            }
            return RedirectToPage();
        }
    }
}