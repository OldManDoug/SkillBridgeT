namespace SkillBridge.Web.Features.Administration;

public sealed record DashboardSummary(int Members, int PublishedCourses, int DraftCourses, int Lessons)
{
    public int Users { get; init; }
    public int Enrolments { get; init; }
    public int QuizAttempts { get; init; }
    public int Threads { get; init; }
}

public interface IDashboardReader
{
    Task<DashboardSummary> GetSummaryAsync(CancellationToken cancellationToken);
}
