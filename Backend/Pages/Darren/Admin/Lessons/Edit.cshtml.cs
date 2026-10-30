using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using MySqlConnector;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkillBridge.Backend.Darren.Lessons;

namespace SkillBridge.Backend.Pages.Darren.Admin.Lessons
{
    [Authorize(Roles = "Admin")]
    public class EditModel : PageModel
    {
        private readonly ILessonReader _lessonReader;

        public EditModel(ILessonReader lessonReader)
        {
            _lessonReader = lessonReader;
        }

        [BindProperty(SupportsGet = true)]
        public long? Id { get; set; }

        public bool IsCreate => !Id.HasValue;

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public List<CourseOption> Courses { get; set; } = new();

        public class InputModel
        {
            [Range(1, long.MaxValue, ErrorMessage = "Please select a course.")]
            public long CourseId { get; set; }

            [Required(ErrorMessage = "Title is required.")]
            [StringLength(120, MinimumLength = 3)]
            public string Title { get; set; } = string.Empty;

            [Required, StringLength(20000, MinimumLength = 20)]
            public string? Content { get; set; }

            [Range(1, 999, ErrorMessage = "Order must be between 1 and 999.")]
            public int OrderIndex { get; set; } = 1;

            [Range(1, 240)]
            public int DurationMinutes { get; set; } = 10;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            Courses = await _lessonReader.GetCourseOptionsAsync();

            if (Id.HasValue)
            {
                var lesson = await _lessonReader.GetLessonByIdAsync(Id.Value);
                if (lesson == null)
                    return NotFound();

                Input = new InputModel
                {
                    CourseId = lesson.CourseId,
                    Title = lesson.Title,
                    Content = lesson.Content,
                    OrderIndex = lesson.OrderIndex,
                    DurationMinutes = lesson.DurationMinutes
                };
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            Courses = await _lessonReader.GetCourseOptionsAsync();
            if (!Courses.Any(course => course.Id == Input.CourseId)) ModelState.AddModelError("Input.CourseId", "Select an existing course.");
            if (Id.HasValue && await _lessonReader.GetLessonByIdAsync(Id.Value) is null) return NotFound();
            if (!ModelState.IsValid)
            {
                Courses = await _lessonReader.GetCourseOptionsAsync();
                return Page();
            }

            var lessonInput = new LessonInput
            {
                CourseId = Input.CourseId,
                Title = Input.Title.Trim(),
                Content = Input.Content,
                OrderIndex = Input.OrderIndex,
                DurationMinutes = Input.DurationMinutes
            };

            try
            {
            if (IsCreate)
            {
                await _lessonReader.CreateLessonAsync(lessonInput);
                TempData["StatusMessage"] = "Lesson created successfully.";
            }
            else
            {
                await _lessonReader.UpdateLessonAsync(Id!.Value, lessonInput);
                TempData["StatusMessage"] = "Lesson updated successfully.";
            }

            }
            catch (MySqlException exception) when (exception.Number is 1062 or 1452 or 3819 or 4025)
            {
                ModelState.AddModelError(string.Empty, "Check the field limits and choose an unused order number for this course.");
                return Page();
            }
            return RedirectToPage("Index");
        }
    }
}