-- Optional demo discussions for the forum pages.
-- Run once in MySQL Workbench after the main skillbridge.sql.
-- Uses the first registered user as the demo author.
USE skillbridge;

SET @author := (SELECT id FROM users ORDER BY id LIMIT 1);

INSERT INTO forum_threads (author_id, title, body, is_pinned)
SELECT @author,
       'Welcome to the SkillBridge community',
       'This pinned thread is a short introduction to the community space. Ask questions about any course, share what you are building, and help other members when you can. Keep posts practical, kind and on topic so the discussions stay useful for everyone.',
       TRUE
WHERE @author IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM forum_threads WHERE title = 'Welcome to the SkillBridge community');

INSERT INTO forum_threads (author_id, title, body)
SELECT @author,
       'How do I undo my last commit in Git?',
       'I accidentally committed a file that was only meant for local testing. What is the safest way to undo the last commit while keeping my changes in the working directory? I have not pushed to the remote yet, so nothing is shared with my team.'
WHERE @author IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM forum_threads WHERE title = 'How do I undo my last commit in Git?');

INSERT INTO forum_threads (author_id, title, body)
SELECT @author,
       'Best way to practise SQL JOINs?',
       'I understand single-table SELECT queries but I keep mixing up INNER JOIN and LEFT JOIN when three tables are involved. Does anyone have a small practice exercise or a way of thinking about joins that made it click for you? The SQL Fundamentals course is still a draft, so I am looking for extra practice.'
WHERE @author IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM forum_threads WHERE title = 'Best way to practise SQL JOINs?');

INSERT INTO forum_replies (thread_id, author_id, body)
SELECT t.id, @author,
       'Since you have not pushed yet, git reset --soft HEAD~1 will move the branch back one commit and keep all your changes staged, so you can unstage the test file and recommit.'
FROM forum_threads t
WHERE t.title = 'How do I undo my last commit in Git?'
  AND @author IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 FROM forum_replies r
      WHERE r.thread_id = t.id
        AND r.body LIKE 'Since you have not pushed yet%'
  );

INSERT INTO forum_replies (thread_id, author_id, body)
SELECT t.id, @author,
       'What helped me was drawing the two tables on paper and marking which rows match the join condition. An INNER JOIN only keeps the marked rows, while a LEFT JOIN keeps every row from the left table and fills gaps with NULL.'
FROM forum_threads t
WHERE t.title = 'Best way to practise SQL JOINs?'
  AND @author IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 FROM forum_replies r
      WHERE r.thread_id = t.id
        AND r.body LIKE 'What helped me was drawing%'
  );