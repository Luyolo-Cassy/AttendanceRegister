using AttendanceRegister.Data;
using AttendanceRegister.Models;
using Microsoft.EntityFrameworkCore;

namespace AttendanceRegister.Services
{
    public static class ChatContextBuilder
    {
        public static async Task<string> BuildSystemPromptAsync(ApplicationDbContext context, ApplicationUser user, bool isLecturer)
        {
            var basePrompt =
                "You are the help assistant embedded in a university attendance register web app " +
                "(INF3003W). You can explain how to use the app (checking in with a code, querying " +
                "a record, uploading a spreadsheet, etc.) and answer questions about the person's " +
                "own attendance data given below. You cannot perform actions (you can't check " +
                "someone in, edit a record, or send anything) - tell the person which page to use " +
                "for that instead. Keep answers short and conversational, no markdown headers.";

            if (isLecturer)
            {
                var totalLectures = await context.Lectures.CountAsync();
                var totalStudents = (await RoleQueries.GetUserIdsInRoleAsync(context, SeedData.StudentRole)).Count;
                return basePrompt + $"\n\nYou're talking to {user.FullName}, a lecturer. " +
                    $"Their course currently has {totalLectures} lectures recorded and {totalStudents} registered students. " +
                    "For detailed per-student questions, point them to the 'Ask About Your Class' page, which has full data access.";
            }

            var records = await context.AttendanceRecords
                .Include(a => a.Lecture)
                .Where(a => a.StudentId == user.Id)
                .OrderByDescending(a => a.Lecture!.Date)
                .Take(10)
                .ToListAsync();

            var recordLines = records.Any()
                ? string.Join("; ", records.Select(r => $"{r.Lecture?.Date}: {r.Status}"))
                : "No attendance records yet.";

            return basePrompt + $"\n\nYou're talking to {user.FullName} ({user.StudentNumber}), a student. " +
                $"Their 10 most recent attendance records: {recordLines}";
        }
    }
}
