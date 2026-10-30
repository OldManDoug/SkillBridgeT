using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkillBridge.Web.Features.Quizzes;
using SkillBridge.Web.Infrastructure.Data;
using MySqlConnector;

namespace SkillBridge.Web.Pages.Admin.Quizzes;

[Authorize(Roles = "Admin")]
public sealed class DeleteModel(IQuizReader quizzes, MySqlConnectionFactory connections,IConfiguration _configuration) : PageModel
{
    public async Task<IActionResult> OnPostAsync(long quizId, CancellationToken cancellationToken)
    {
        await using var connection = await connections.OpenAsync(cancellationToken);
        await using var delete = new MySqlCommand(" DELETE FROM quizzes WHERE id = @quizId;", connection); 
        //@
        delete.Parameters.AddWithValue("@quizId", quizId); 
        await delete.ExecuteNonQueryAsync(cancellationToken); 

        return RedirectToPage("Timothy/Admin/Quizzes/Index");
    }
}