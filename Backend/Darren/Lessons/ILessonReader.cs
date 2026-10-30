using System.Collections.Generic;
using System.Threading.Tasks;

namespace SkillBridge.Backend.Darren.Lessons
{
    public interface ILessonReader
    {
        Task<List<CourseOption>> GetCourseOptionsAsync();
        Task<List<LessonItem>> GetLessonsAsync(long? courseId, string? search);
        Task<LessonItem?> GetLessonByIdAsync(long id);
        Task<long> CreateLessonAsync(LessonInput input);
        Task UpdateLessonAsync(long id, LessonInput input);
        Task DeleteLessonAsync(long id);

        Task<List<LessonOption>> GetLessonOptionsAsync();
        Task<List<ResourceItem>> GetResourcesAsync(long? lessonId, string? search);
        Task<ResourceItem?> GetResourceByIdAsync(long id);
        Task<List<ResourceItem>> GetResourcesByLessonIdAsync(long lessonId);
        Task DeleteResourceAsync(long id);

        Task<bool> IsLessonCompletedAsync(long userId, long lessonId);
        Task ToggleLessonCompletionAsync(long userId, long lessonId);
    }
}