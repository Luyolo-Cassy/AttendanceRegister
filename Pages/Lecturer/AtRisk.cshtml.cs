using AttendanceRegister.Data;
using AttendanceRegister.Models;
using AttendanceRegister.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AttendanceRegister.Pages.Lecturer
{
    public class AtRiskModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IAiAssistantService _ai;

        public AtRiskModel(ApplicationDbContext context, IAiAssistantService ai)
        {
            _context = context;
            _ai = ai;
        }

        public record AtRiskStudent(string StudentId, string FullName, string? StudentNumber, double Rate, string PatternSummary);

        public List<AtRiskStudent> Students { get; set; } = new();
        public Dictionary<string, string> Drafts { get; set; } = new();

        private const string SystemPrompt =
            "You are helping a South African university lecturer write short, kind, direct " +
            "messages to students with low lecture attendance. Write ONE short message (3-5 " +
            "sentences), no subject line, no greeting boilerplate like 'Dear student'. Be " +
            "specific about the pattern described, not generic. Encourage them to reach out if " +
            "something is going on, and remind them attendance affects their DP (duly performed) " +
            "status. Do not invent facts beyond what's given. Output only the message text.";

        public async Task OnGetAsync()
        {
            Students = await LoadAtRiskStudentsAsync();
        }

        public async Task<IActionResult> OnPostGenerateOneAsync(string studentId)
        {
            Students = await LoadAtRiskStudentsAsync();
            var student = Students.FirstOrDefault(s => s.StudentId == studentId);
            if (student is not null)
            {
                Drafts[studentId] = await _ai.CompleteAsync(SystemPrompt, BuildPrompt(student));
            }
            return Page();
        }

        public async Task<IActionResult> OnPostGenerateAllAsync()
        {
            Students = await LoadAtRiskStudentsAsync();
            foreach (var student in Students)
            {
                Drafts[student.StudentId] = await _ai.CompleteAsync(SystemPrompt, BuildPrompt(student));
            }
            return Page();
        }

        private static string BuildPrompt(AtRiskStudent s) =>
            $"Student: {s.FullName}\nAttendance rate: {s.Rate:0}%\nPattern: {s.PatternSummary}\n\nDraft the message.";

        private async Task<List<AtRiskStudent>> LoadAtRiskStudentsAsync()
        {
            var totalLectures = await _context.Lectures.CountAsync();
            if (totalLectures == 0) return new List<AtRiskStudent>();

            var studentIds = await RoleQueries.GetUserIdsInRoleAsync(_context, SeedData.StudentRole);
            var users = await _context.Users.Where(u => studentIds.Contains(u.Id)).ToListAsync();

            var result = new List<AtRiskStudent>();
            foreach (var user in users)
            {
                var records = await _context.AttendanceRecords
                    .Include(a => a.Lecture)
                    .Where(a => a.StudentId == user.Id)
                    .OrderBy(a => a.Lecture!.Date)
                    .ToListAsync();

                var present = records.Count(r => r.Status is AttendanceStatus.Present or AttendanceStatus.Late);
                var rate = 100.0 * present / totalLectures;
                if (rate >= 75) continue;

                result.Add(new AtRiskStudent(user.Id, user.FullName, user.StudentNumber, rate, SummarizePattern(records, totalLectures)));
            }

            return result.OrderBy(s => s.Rate).ToList();
        }

        // Turns raw records into a short factual description the AI can work from, so the
        // draft reflects the real pattern (recent slide vs. always-low) instead of generic
        // "attend more" text.
        private static string SummarizePattern(List<AttendanceRecord> records, int totalLectures)
        {
            var missed = totalLectures - records.Count(r => r.Status is AttendanceStatus.Present or AttendanceStatus.Late);
            var recent = records.TakeLast(5).ToList();
            var recentMissed = recent.Count(r => r.Status is AttendanceStatus.Absent);
            var neverMarked = totalLectures - records.Count;

            var parts = new List<string> { $"{missed} of {totalLectures} lectures missed overall" };
            if (neverMarked > 0) parts.Add($"{neverMarked} lectures with no record at all (possibly before enrolling, or missed sign-in)");
            if (recent.Any()) parts.Add($"{recentMissed} of the last {recent.Count} lectures absent");

            return string.Join("; ", parts);
        }


        public async Task<IActionResult> OnPostSendAsync(
    string studentId,
    string message)
        {
            var notification = new Notification
            {
                StudentId = studentId,
                Message = message,
                SentAt = DateTime.Now,
                IsRead = false
            };

            _context.Notifications.Add(notification);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Message sent successfully.";

            return RedirectToPage();
        }
    }

}
