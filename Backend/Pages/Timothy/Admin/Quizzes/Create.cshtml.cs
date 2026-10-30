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
public sealed record QuizQuestion(long Id, int SequenceNumber, string Text, string Explanation, int Marks, List<QuizOption> Options);
//from ccourses somewhere. 
    public IReadOnlyList<QuizSummary> Quizzes { get; private set; } = [];

public string? ErrorMessage { get; set; }
    [BindProperty] public string Title { get; set; } = "";
    [BindProperty] public long CourseId { get; set; }
    [BindProperty] public string instructions { get; set; } = "";
    [BindProperty] public decimal pass_percentage { get; set; }
    [BindProperty] public bool IsPublished { get; set; }
    [BindProperty] public DateTime? created_at_utc {get; set;}
    //for the quizzes
    //for the questions
    [BindProperty] public string question_text { get; set; } = "";
    [BindProperty] public long quiz_id { get; set; }
    [BindProperty] public string explanation { get; set; } = "";
    [BindProperty] public int marks { get; set; }
    [BindProperty] public int sequence_number { get; set; }

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
                //match with bind properties otherwise error 
                command.Parameters.AddWithValue("@courseid", CourseId); 
                command.Parameters.AddWithValue("@title", Title);
                command.Parameters.AddWithValue("@instructions", instructions);
                command.Parameters.AddWithValue("@pass_percentage", pass_percentage); 
                command.Parameters.AddWithValue("@publish", IsPublished);
                command.Parameters.AddWithValue("@createdat",created_at_utc ?? (object)DBNull.Value);
                await command.ExecuteNonQueryAsync(cancellationToken); 
            }

            await transaction.CommitAsync(cancellationToken);
            return RedirectToPage("/Timothy/Admin/Quizzes/Index");
/*
            foreach (var questionText in QuizQuestion)
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
                 questionCmd.ExecuteNonQuery(); //remove 
 
        }

  
       string message = "New task added successfully."; 
        //return back to admin page. 
        return RedirectToPage("/Timothy/Admin/Quizzes/Index");
   */     }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            ModelState.AddModelError(string.Empty, "An error occurred while saving the quiz: " + ex.Message);
            Quizzes = await quizzes.GetAllAsync(cancellationToken);
            return Page();
        }
    }


}
