using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkillBridge.Web.Features.Quizzes;

namespace SkillBridge.Web.Pages.Quizzes;
//https://www.c-sharpcorner.com/article/creating-dynamic-web-quizzes-using-C-Sharp-and-Asp-Net
public sealed class QuizModel(IQuizReader quizzes, QuizAttemptStore attempts) : PageModel
{
    public SavedAttempt Attempt { get; private set; } = null!;
    public QuizOverview Quiz { get; private set; } = null!;
    public List<QuizQuestion> Questions { get; private set; } = [];
    [BindProperty] public Dictionary<long, long> Selections { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(long attemptId, CancellationToken cancellationToken)
    {
        if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Challenge();
        var attempt = await attempts.FindAsync(attemptId, userId, cancellationToken);
        if (attempt is null) return NotFound();
        var quiz = await quizzes.GetOverviewAsync(attempt.QuizId, userId, User.IsInRole("Admin"), cancellationToken);
        if (quiz is null) return NotFound();
        if (attempt.IsSubmitted) return RedirectToPage("/Timothy/Quizzes/Review", new { attemptId });
        Attempt = attempt;
        Quiz = quiz;
        Questions = await attempts.QuestionsAsync(quiz.Id, cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(long attemptId, CancellationToken cancellationToken)
    {
        var result = await OnGetAsync(attemptId, cancellationToken);
        if (result is not PageResult) return result;
        if (!ModelState.IsValid)
        {
            ModelState.AddModelError(string.Empty, "Choose one answer for every question.");
            return Page();
        }
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (!await attempts.SubmitAsync(attemptId, userId, Selections, cancellationToken))
        {
            ModelState.AddModelError(string.Empty, "Choose one answer for every question. If the quiz has changed, reload it and try again.");
            return Page();
        }
        return RedirectToPage("/Timothy/Quizzes/Review", new { attemptId });
    }
}
