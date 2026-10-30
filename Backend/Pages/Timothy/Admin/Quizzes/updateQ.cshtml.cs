using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkillBridge.Web.Features.Quizzes;
using SkillBridge.Web.Infrastructure.Data;
using MySqlConnector;

namespace SkillBridge.Web.Pages.Admin.Quizzes;

[Authorize(Roles = "Admin")]
public sealed class UpdateModel(IQuizReader quizzes, MySqlConnectionFactory connection, IConfiguration _configuration) : PageModel
{
     
   [BindProperty]
    public long QuizId { get; set; }

    [BindProperty]
    public long CourseId { get; set; } // Display-only or tracked for reference

    [BindProperty]
    public string Title { get; set; } = string.Empty;

    [BindProperty]
    public string Instructions { get; set; } = string.Empty;

    [BindProperty]
    public int PassPercentage { get; set; }

    [BindProperty]
    public bool IsPublished { get; set; }

    [TempData]
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        if (!User.IsInRole("Admin")) return RedirectToPage("/Login");

        await LoadQuizDataAsync(id, cancellationToken);
        if (QuizId == 0) return NotFound();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!User.IsInRole("Admin")) return RedirectToPage("/Login");

        if (!ModelState.IsValid) return Page();

        try
        {
            string connString = _configuration.GetConnectionString("DefaultConnection") ?? "";
            await using var conn = new MySqlConnection(connString);
            await conn.OpenAsync(cancellationToken);

            // Update everything EXCEPT quiz id and course id
            string sql = """
                UPDATE quizzes 
                SET title = @title, 
                    instructions = @instructions, 
                    pass_percentage = @passPercentage, 
                    is_published = @isPublished 
                WHERE id = @quizId;
                """;

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@title", Title.Trim());
            cmd.Parameters.AddWithValue("@instructions", Instructions.Trim());
            cmd.Parameters.AddWithValue("@passPercentage", PassPercentage);
            cmd.Parameters.AddWithValue("@isPublished", IsPublished);
            cmd.Parameters.AddWithValue("@quizId", QuizId);

            await cmd.ExecuteNonQueryAsync(cancellationToken);

            SuccessMessage = "Quiz updated successfully.";
            return RedirectToPage("/Admin/Quizzes/Index");
        }
        catch (Exception)
        {
            ErrorMessage = "Failed to update quiz. Please try again.";
            return Page();
        }
    }

    private async Task LoadQuizDataAsync(long id, CancellationToken cancellationToken)
    {
        string connString = _configuration.GetConnectionString("DefaultConnection") ?? "";
        await using var conn = new MySqlConnection(connString);
        await conn.OpenAsync(cancellationToken);

        string sql = "SELECT id, course_id, title, instructions, pass_percentage, is_published FROM quizzes WHERE id = @id;";
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", id);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            QuizId = reader.GetInt64("id");
            CourseId = reader.GetInt64("course_id");
            Title = reader.GetString("title");
            Instructions = reader.GetString("instructions");
            PassPercentage = reader.GetInt32("pass_percentage");
            IsPublished = reader.GetBoolean("is_published");
        }
        // return RedirectToPage("/Timothy/Admin/Quizzes/Index");
    } 
   
}
