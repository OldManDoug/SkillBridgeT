namespace SkillBridge.Web.Features.Quizzes;

public interface IQuizReader
{
    Task<QuizOverview?> GetOverviewAsync(long quizId, long userId, bool canPreviewDraft, CancellationToken cancellationToken);
    Task<IReadOnlyList<QuizSummary>> GetAllAsync(CancellationToken cancellationToken);
    
    // update create delete in here this is for all the quiz necessities
    
}
