using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using SkillBridge.Backend.Darren.Lessons;

namespace SkillBridge.Backend.Darren.Data
{
    public class MySqlLessonReader : ILessonReader
    {
        private readonly string _connectionString;

        public MySqlLessonReader(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("SkillBridge") 
                ?? throw new InvalidOperationException("Connection string 'SkillBridge' not found.");
        }

        private async Task<MySqlConnection> GetOpenConnectionAsync()
        {
            var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
            return connection;
        }

        public async Task<List<CourseOption>> GetCourseOptionsAsync()
        {
            var courses = new List<CourseOption>();
            await using var connection = await GetOpenConnectionAsync();
            await using var command = connection.CreateCommand();
            
            command.CommandText = "SELECT id, title FROM courses ORDER BY title ASC;";
            
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                courses.Add(new CourseOption
                {
                    Id = reader.GetInt64("id"),
                    Title = reader.GetString("title")
                });
            }

            return courses;
        }

        public async Task<List<LessonItem>> GetLessonsAsync(long? courseId, string? search)
        {
            var lessons = new List<LessonItem>();
            await using var connection = await GetOpenConnectionAsync();
            await using var command = connection.CreateCommand();

            var sql = @"
                SELECT 
                    l.id, 
                    l.course_id, 
                    c.title AS course_title, 
                    l.title, 
                    l.sequence_number AS order_index, 
                    l.duration_minutes,
                    (SELECT COUNT(*) FROM resources r WHERE r.lesson_id = l.id) AS resource_count
                FROM lessons l
                JOIN courses c ON l.course_id = c.id
                WHERE 1=1";

            if (courseId.HasValue)
            {
                sql += " AND l.course_id = @CourseId";
                command.Parameters.AddWithValue("@CourseId", courseId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                sql += " AND l.title LIKE @Search";
                command.Parameters.AddWithValue("@Search", $"%{search.Trim()}%");
            }

            sql += " ORDER BY c.title ASC, l.sequence_number ASC;";
            command.CommandText = sql;

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                lessons.Add(new LessonItem
                {
                    Id = reader.GetInt64("id"),
                    CourseId = reader.GetInt64("course_id"),
                    CourseTitle = reader.GetString("course_title"),
                    Title = reader.GetString("title"),
                    OrderIndex = reader.GetInt32("order_index"),
                    DurationMinutes = reader.GetInt32("duration_minutes"),
                    ResourceCount = reader.GetInt32("resource_count")
                });
            }

            return lessons;
        }

        public async Task<LessonItem?> GetLessonByIdAsync(long id)
        {
            await using var connection = await GetOpenConnectionAsync();
            await using var command = connection.CreateCommand();

            command.CommandText = @"
                SELECT 
                    l.id, 
                    l.course_id, 
                    c.title AS course_title, 
                    l.title, 
                    l.body AS content,
                    l.sequence_number AS order_index, 
                    l.duration_minutes,
                    (SELECT COUNT(*) FROM resources r WHERE r.lesson_id = l.id) AS resource_count
                FROM lessons l
                JOIN courses c ON l.course_id = c.id
                WHERE l.id = @Id;";

            command.Parameters.AddWithValue("@Id", id);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new LessonItem
                {
                    Id = reader.GetInt64("id"),
                    CourseId = reader.GetInt64("course_id"),
                    CourseTitle = reader.GetString("course_title"),
                    Title = reader.GetString("title"),
                    Content = reader.IsDBNull(reader.GetOrdinal("content")) ? "" : reader.GetString("content"),
                    OrderIndex = reader.GetInt32("order_index"),
                    DurationMinutes = reader.GetInt32("duration_minutes"),
                    ResourceCount = reader.GetInt32("resource_count")
                };
            }

            return null;
        }

        public async Task<long> CreateLessonAsync(LessonInput input)
        {
            await using var connection = await GetOpenConnectionAsync();
            await using var command = connection.CreateCommand();

            command.CommandText = @"
                INSERT INTO lessons (course_id, title, summary, body, sequence_number, duration_minutes) 
                VALUES (@CourseId, @Title, @Title, @Content, @SortOrder, @DurationMinutes);
                SELECT LAST_INSERT_ID();";

            command.Parameters.AddWithValue("@CourseId", input.CourseId);
            command.Parameters.AddWithValue("@Title", input.Title);
            command.Parameters.AddWithValue("@Content", input.Content ?? string.Empty);
            command.Parameters.AddWithValue("@SortOrder", input.OrderIndex);
            command.Parameters.AddWithValue("@DurationMinutes", input.DurationMinutes);

            var newId = Convert.ToInt64(await command.ExecuteScalarAsync());
            return newId;
        }

        public async Task UpdateLessonAsync(long id, LessonInput input)
        {
            await using var connection = await GetOpenConnectionAsync();
            await using var command = connection.CreateCommand();

            command.CommandText = @"
                UPDATE lessons 
                SET course_id = @CourseId, title = @Title, body = @Content, sequence_number = @SortOrder, duration_minutes = @DurationMinutes
                WHERE id = @Id;";

            command.Parameters.AddWithValue("@Id", id);
            command.Parameters.AddWithValue("@CourseId", input.CourseId);
            command.Parameters.AddWithValue("@Title", input.Title);
            command.Parameters.AddWithValue("@Content", input.Content ?? string.Empty);
            command.Parameters.AddWithValue("@SortOrder", input.OrderIndex);
            command.Parameters.AddWithValue("@DurationMinutes", input.DurationMinutes);

            await command.ExecuteNonQueryAsync();
        }

        public async Task DeleteLessonAsync(long id)
        {
            await using var connection = await GetOpenConnectionAsync();
            await using var command = connection.CreateCommand();

            command.CommandText = "DELETE FROM lessons WHERE id = @Id;";
            command.Parameters.AddWithValue("@Id", id);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<List<LessonOption>> GetLessonOptionsAsync()
        {
            var lessons = new List<LessonOption>();
            await using var connection = await GetOpenConnectionAsync();
            await using var command = connection.CreateCommand();

            command.CommandText = "SELECT id, title FROM lessons ORDER BY title ASC;";

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                lessons.Add(new LessonOption
                {
                    Id = reader.GetInt64("id"),
                    Title = reader.GetString("title")
                });
            }

            return lessons;
        }

        public async Task<List<ResourceItem>> GetResourcesAsync(long? lessonId, string? search)
        {
            var resources = new List<ResourceItem>();
            await using var connection = await GetOpenConnectionAsync();
            await using var command = connection.CreateCommand();

            var sql = @"
                SELECT 
                    r.id, 
                    r.lesson_id, 
                    l.title AS lesson_title, 
                    r.title, 
                    r.storage_key AS url, 
                    r.resource_type AS file_type
                FROM resources r
                JOIN lessons l ON r.lesson_id = l.id
                WHERE 1=1";

            if (lessonId.HasValue)
            {
                sql += " AND r.lesson_id = @LessonId";
                command.Parameters.AddWithValue("@LessonId", lessonId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                sql += " AND r.title LIKE @Search";
                command.Parameters.AddWithValue("@Search", $"%{search.Trim()}%");
            }

            sql += " ORDER BY r.id DESC;";
            command.CommandText = sql;

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                resources.Add(new ResourceItem
                {
                    Id = reader.GetInt64("id"),
                    LessonId = reader.GetInt64("lesson_id"),
                    LessonTitle = reader.GetString("lesson_title"),
                    Title = reader.GetString("title"),
                    Url = reader.GetString("url"),
                    FileType = reader.GetString("file_type")
                });
            }

            return resources;
        }

        public async Task<ResourceItem?> GetResourceByIdAsync(long id)
        {
            await using var connection = await GetOpenConnectionAsync();
            await using var command = connection.CreateCommand();

            command.CommandText = @"
                SELECT r.id, r.lesson_id, l.title AS lesson_title, r.title, r.storage_key AS url, r.resource_type AS file_type
                FROM resources r
                JOIN lessons l ON r.lesson_id = l.id
                WHERE r.id = @Id;";

            command.Parameters.AddWithValue("@Id", id);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new ResourceItem
                {
                    Id = reader.GetInt64("id"),
                    LessonId = reader.GetInt64("lesson_id"),
                    LessonTitle = reader.GetString("lesson_title"),
                    Title = reader.GetString("title"),
                    Url = reader.GetString("url"),
                    FileType = reader.GetString("file_type")
                };
            }

            return null;
        }

        public async Task<List<ResourceItem>> GetResourcesByLessonIdAsync(long lessonId)
        {
            return await GetResourcesAsync(lessonId, null);
        }

        public async Task DeleteResourceAsync(long id)
        {
            await using var connection = await GetOpenConnectionAsync();
            await using var command = connection.CreateCommand();

            command.CommandText = "DELETE FROM resources WHERE id = @Id;";
            command.Parameters.AddWithValue("@Id", id);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<bool> IsLessonCompletedAsync(long userId, long lessonId)
        {
            await using var connection = await GetOpenConnectionAsync();
            await using var command = connection.CreateCommand();

            command.CommandText = "SELECT COUNT(*) FROM lesson_progress WHERE user_id = @UserId AND lesson_id = @LessonId AND is_completed=TRUE;";
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@LessonId", lessonId);

            var count = Convert.ToInt64(await command.ExecuteScalarAsync());
            return count > 0;
        }

        public async Task ToggleLessonCompletionAsync(long userId, long lessonId)
        {
            var isCompleted = await IsLessonCompletedAsync(userId, lessonId);
            await using var connection = await GetOpenConnectionAsync();
            await using var command = connection.CreateCommand();

            if (isCompleted)
            {
                command.CommandText = "DELETE FROM lesson_progress WHERE user_id = @UserId AND lesson_id = @LessonId AND is_completed=TRUE;";
            }
            else
            {
                command.CommandText = "INSERT INTO lesson_progress (user_id, lesson_id, is_completed, completed_at_utc) VALUES (@UserId, @LessonId, TRUE, UTC_TIMESTAMP()) ON DUPLICATE KEY UPDATE is_completed=TRUE, completed_at_utc=UTC_TIMESTAMP();";
            }

            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@LessonId", lessonId);

            await command.ExecuteNonQueryAsync();
        }
    }
}