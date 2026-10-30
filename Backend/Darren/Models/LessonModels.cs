namespace SkillBridge.Backend.Darren.Lessons
{
    public class CourseOption
    {
        public long Id { get; set; }
        public string Title { get; set; } = string.Empty;
    }

    public class LessonOption
    {
        public long Id { get; set; }
        public string Title { get; set; } = string.Empty;
    }

    public class LessonItem
    {
        public long Id { get; set; }
        public long CourseId { get; set; }
        public string CourseTitle { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int OrderIndex { get; set; }
        public int DurationMinutes { get; set; }
        public int ResourceCount { get; set; }
    }

    public class LessonInput
    {
        public long CourseId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Content { get; set; }
        public int OrderIndex { get; set; }
        public int DurationMinutes { get; set; }
    }

    public class ResourceItem
    {
        public long Id { get; set; }
        public long LessonId { get; set; }
        public string LessonTitle { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
    }

    public class ResourceInput
    {
        public long LessonId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
    }
}