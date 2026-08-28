using AttendanceRegister.Data;
using AttendanceRegister.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AttendanceRegister.Pages.Student
{
    public class QueryModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public QueryModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty(SupportsGet = true)]
        public int? LectureId { get; set; }

        public List<SelectListItem> LectureOptions { get; set; } = new();
        public AttendanceRecord? SelectedRecord { get; set; }
        public string? SelectedLectureLabel { get; set; }

        public async Task OnGetAsync()
        {
            LectureOptions = await _context.Lectures
                .OrderByDescending(l => l.Date)
                .Select(l => new SelectListItem($"{l.Date} - {l.Title}", l.Id.ToString()))
                .ToListAsync();

            if (LectureId.HasValue)
            {
                SelectedLectureLabel = LectureOptions.FirstOrDefault(o => o.Value == LectureId.Value.ToString())?.Text;

                var user = await _userManager.GetUserAsync(User);
                if (user is null) return;

                SelectedRecord = await _context.AttendanceRecords
                    .Include(a => a.Lecture)
                    .FirstOrDefaultAsync(a => a.LectureId == LectureId && a.StudentId == user.Id);
            }
        }

        // Appends the student's message to the record's Notes field so it shows up for the
        // lecturer on the Edit page - a lightweight query/dispute channel without a separate
        // messaging system.
        public async Task<IActionResult> OnPostRaiseQueryAsync(int recordId, string message)
        {
            var record = await _context.AttendanceRecords.FindAsync(recordId);
            var user = await _userManager.GetUserAsync(User);
            if (record is null || user is null || record.StudentId != user.Id)
            {
                return NotFound();
            }

            record.Notes = $"[Student query] {message}";
            await _context.SaveChangesAsync();

            TempData["StatusMessage"] = "Your query has been sent to your lecturer.";
            return RedirectToPage(new { LectureId = record.LectureId });
        }

        public string BadgeColor(AttendanceStatus status) => status switch
        {
            AttendanceStatus.Present => "success",
            AttendanceStatus.Late => "warning",
            AttendanceStatus.Excused => "info",
            _ => "danger"
        };
    }
}
