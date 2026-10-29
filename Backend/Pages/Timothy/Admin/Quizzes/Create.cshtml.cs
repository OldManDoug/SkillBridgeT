using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkillBridge.Web.Features.Quizzes;
using SkillBridge.Web.Infrastructure.Data;
using MySqlConnector;

namespace SkillBridge.Web.Pages.Admin.Quizzes;

[Authorize(Roles = "Admin")]
public sealed class CreateModel(IQuizReader quizzes, MySqlConnectionFactory connections) : PageModel
{

    public IReadOnlyList<QuizSummary> Quizzes { get; private set; } = [];

    [BindProperty] public string Title { get; set; } = string.Empty;
    [BindProperty] public long CourseId { get; set; }
    [BindProperty] public string Instructions { get; set; } = string.Empty;
    [BindProperty] public decimal PassPercentage { get; set; }
    [BindProperty] public bool IsPublished { get; set; }
    //for the questions

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Quizzes = await quizzes.GetAllAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            Quizzes = await quizzes.GetAllAsync(cancellationToken);
            return Page();
        }

        await using var connection = await connections.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            string sql = "INSERT INTO quizzes (course_id, title, instructions, pass_percentage, is_published, created_at_utc) VALUES (@courseid, @title, @instructions, @pass_percentage, @publish, @createdat);"; 
            
            using (var command = new MySqlCommand(sql, connection, (MySqlTransaction)transaction)) 
            {
                command.Parameters.AddWithValue("@courseid", CourseId); 
                command.Parameters.AddWithValue("@title", title);
                command.Parameters.AddWithValue("@instructions", ifnstructions);
                command.Parameters.AddWithValue("@pass_percentage", pass_percentage); 
                command.Parameters.AddWithValue("@publish", is_published);
                command.Parameters.AddWithValue("@createdat",created_at_utc);
                await command.ExecuteNonQueryAsync(cancellationToken); 
            }

            await transaction.CommitAsync(cancellationToken);
            return RedirectToPage("/Timothy/Admin/Quizzes/Index");

            foreach (var questionText in QuestionList)
        {
            string insertQuestionQuery = "INSERT INTO Questions (quiz_id,question_text,explanation,marks,sequence_number) VALUES (@QuizId, @QuestionText,@Explanation,@Marks,@Sequencenum);";
            
            using (var questionCmd = new MySqlCommand(insertQuestionQuery, connection, (MySqlTransaction)transaction))
            {
             questionCmd.Parameters.AddWithValue("@QuizId",quiz_id);
                questionCmd.Parameters.AddWithValue("@QuestionText", question_text);
                questionCmd.Parameters.AddWithValue("@Explanation",explanation);
                questionCmd.Parameters.AddWithValue("@Marks",marks);
                questionCmd.Parameters.AddWithValue("@Sequencenum", sequence_number);
                //question as a sequence number 
                
                await questionCmd.ExecuteNonQueryAsync();
            }
        }

       questionCmd.ExecuteNonQuery(); 
 
       string message = "New task added successfully."; 
        //return back to admin page. 
        return RedirectToPage("/Timothy/Admin/Quizzes/Index");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            ModelState.AddModelError(string.Empty, "An error occurred while saving the quiz: " + ex.Message);
            Quizzes = await quizzes.GetAllAsync(cancellationToken);
            return Page();
        }
    }


}
