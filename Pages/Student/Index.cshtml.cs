using AttendanceRegister.Data;
using AttendanceRegister.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AttendanceRegister.Pages.Student
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public IndexModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public string FullName { get; set; } = string.Empty;
        public double AttendancePercent { get; set; }
        public int CurrentStreak { get; set; }
        public int TotalRecorded { get; set; }
        public int TotalLectures { get; set; }
        public Lecture? OpenLecture { get; set; }
        public List<AttendanceRecord> RecentRecords { get; set; } = new();

        public async Task OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return;
            FullName = user.FullName;

            var records = await _context.AttendanceRecords
                .Include(a => a.Lecture)
                .Where(a => a.StudentId == user.Id)
                .OrderByDescending(a => a.Lecture!.Date)
                .ToListAsync();

            TotalLectures = await _context.Lectures.CountAsync();
            TotalRecorded = records.Count;

            var presentCount = records.Count(r => r.Status is AttendanceStatus.Present or AttendanceStatus.Late);
            AttendancePercent = TotalLectures == 0 ? 0 : 100.0 * presentCount / TotalLectures;

            // Streak = consecutive "present" records starting from the most recent lecture.
            var streak = 0;
            foreach (var record in records)
            {
                if (record.Status is AttendanceStatus.Present or AttendanceStatus.Late)
                {
                    streak++;
                }
                else
                {
                    break;
                }
            }
            CurrentStreak = streak;

            var now = DateTime.UtcNow;
            OpenLecture = await _context.Lectures
                .Where(l => l.CheckInOpensAt != null && l.CheckInClosesAt != null
                    && l.CheckInOpensAt <= now && l.CheckInClosesAt >= now)
                .FirstOrDefaultAsync();

            RecentRecords = records.Take(10).ToList();
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
