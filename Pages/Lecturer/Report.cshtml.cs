using System.Text.Json;
using AttendanceRegister.Data;
using AttendanceRegister.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AttendanceRegister.Pages.Lecturer
{
    public class ReportModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public ReportModel(ApplicationDbContext context) => _context = context;

        public string LabelsJson { get; set; } = "[]";
        public string PresentCountsJson { get; set; } = "[]";
        public string AbsentCountsJson { get; set; } = "[]";

        public List<AtRiskStudent> AtRiskStudents { get; set; } = new();

        public record AtRiskStudent(string FullName, string? StudentNumber, double Rate);

        public async Task OnGetAsync()
        {
            var lectures = await _context.Lectures.OrderBy(l => l.Date).ToListAsync();

            var presentByLecture = await _context.AttendanceRecords
                .Where(a => a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late)
                .GroupBy(a => a.LectureId)
                .Select(g => new { LectureId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.LectureId, x => x.Count);

            var absentByLecture = await _context.AttendanceRecords
                .Where(a => a.Status == AttendanceStatus.Absent)
                .GroupBy(a => a.LectureId)
                .Select(g => new { LectureId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.LectureId, x => x.Count);

            LabelsJson = JsonSerializer.Serialize(lectures.Select(l => l.Date.ToString("MM-dd")));
            PresentCountsJson = JsonSerializer.Serialize(lectures.Select(l => presentByLecture.GetValueOrDefault(l.Id, 0)));
            AbsentCountsJson = JsonSerializer.Serialize(lectures.Select(l => absentByLecture.GetValueOrDefault(l.Id, 0)));

            var totalLectures = lectures.Count;
            if (totalLectures > 0)
            {
                var studentIds = await RoleQueries.GetUserIdsInRoleAsync(_context, SeedData.StudentRole);
                var students = await _context.Users.Where(u => studentIds.Contains(u.Id)).ToListAsync();
                foreach (var student in students)
                {
                    var present = await _context.AttendanceRecords
                        .CountAsync(a => a.StudentId == student.Id
                            && (a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late));
                    var rate = 100.0 * present / totalLectures;
                    if (rate < 75)
                    {
                        AtRiskStudents.Add(new AtRiskStudent(student.FullName, student.StudentNumber, rate));
                    }
                }
                AtRiskStudents = AtRiskStudents.OrderBy(s => s.Rate).ToList();
            }
        }
    }
}
