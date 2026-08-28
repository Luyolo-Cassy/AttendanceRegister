using AttendanceRegister.Data;
using AttendanceRegister.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AttendanceRegister.Pages.Lecturer
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public IndexModel(ApplicationDbContext context) => _context = context;

        public int TotalLectures { get; set; }
        public int TotalStudents { get; set; }
        public double AverageAttendance { get; set; }
        public int AtRiskCount { get; set; }
        public List<AttendanceRecord> QueriedRecords { get; set; } = new();

        public async Task OnGetAsync()
        {
            TotalLectures = await _context.Lectures.CountAsync();

            // Whether someone is a student is a role fact, not "did they happen to fill in a
            // student number field" - StudentNumber alone was letting mis-registered or
            // partially-filled-in accounts get miscounted. RoleQueries centralises the
            // correct join so every page that needs "who is actually a Student" agrees.
            var studentIds = await RoleQueries.GetUserIdsInRoleAsync(_context, SeedData.StudentRole);
            TotalStudents = studentIds.Count;

            if (TotalLectures > 0 && TotalStudents > 0)
            {
                var perStudentRates = new List<double>();
                foreach (var studentId in studentIds)
                {
                    var present = await _context.AttendanceRecords
                        .CountAsync(a => a.StudentId == studentId
                            && (a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late));
                    var rate = 100.0 * present / TotalLectures;
                    perStudentRates.Add(rate);
                    if (rate < 75) AtRiskCount++;
                }
                AverageAttendance = perStudentRates.Average();
            }

            QueriedRecords = await _context.AttendanceRecords
                .Include(a => a.Student)
                .Include(a => a.Lecture)
                .Where(a => a.Notes != null && a.Notes.StartsWith("[Student query]"))
                .OrderByDescending(a => a.RecordedAtUtc)
                .ToListAsync();
        }
    }
}
