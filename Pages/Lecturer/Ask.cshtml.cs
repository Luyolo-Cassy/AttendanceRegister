using System.ComponentModel.DataAnnotations;
using System.Text;
using AttendanceRegister.Data;
using AttendanceRegister.Models;
using AttendanceRegister.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AttendanceRegister.Pages.Lecturer
{
    public class AskModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IAiAssistantService _ai;

        public AskModel(ApplicationDbContext context, IAiAssistantService ai)
        {
            _context = context;
            _ai = ai;
        }

        [BindProperty, Required]
        public string Question { get; set; } = string.Empty;

        public string? Answer { get; set; }

        private const string SystemPrompt =
            "You are answering a lecturer's question about their class attendance data. You are " +
            "given a data summary below - answer ONLY from that data, do not invent students or " +
            "numbers that aren't there. If the data doesn't let you answer precisely, say so and " +
            "give the closest useful answer. Keep the answer concise and skimmable (short " +
            "paragraphs or a short list). Do not use markdown headers.";

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var dataSummary = await BuildDataSummaryAsync();
            var prompt = $"DATA:\n{dataSummary}\n\nQUESTION: {Question}";
            Answer = await _ai.CompleteAsync(SystemPrompt, prompt);

            return Page();
        }

        // Builds a compact per-student table the AI can reason over. Capped implicitly by
        // class size (~100 students here) - for a much larger course this would need paging
        // or pre-aggregation instead of dumping every row into the prompt.
        private async Task<string> BuildDataSummaryAsync()
        {
            var lectures = await _context.Lectures.OrderBy(l => l.Date).ToListAsync();
            var studentIds = await RoleQueries.GetUserIdsInRoleAsync(_context, SeedData.StudentRole);
            var users = await _context.Users.Where(u => studentIds.Contains(u.Id)).ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine($"Total lectures: {lectures.Count}");
            sb.AppendLine("Lecture dates: " + string.Join(", ", lectures.Select(l => l.Date.ToString())));
            sb.AppendLine();
            sb.AppendLine("Per-student attendance (name, student no, status per lecture in date order):");

            foreach (var user in users)
            {
                var records = await _context.AttendanceRecords
                    .Include(a => a.Lecture)
                    .Where(a => a.StudentId == user.Id)
                    .ToListAsync();

                var byLectureId = records.ToDictionary(r => r.LectureId, r => r.Status.ToString());
                var statusSequence = lectures.Select(l => byLectureId.GetValueOrDefault(l.Id, "NoRecord"));

                sb.AppendLine($"- {user.FullName} ({user.StudentNumber}): {string.Join(",", statusSequence)}");
            }

            return sb.ToString();
        }
    }
}
