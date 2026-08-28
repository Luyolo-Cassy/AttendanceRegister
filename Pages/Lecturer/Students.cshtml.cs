using AttendanceRegister.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AttendanceRegister.Pages.Lecturer
{
    public class StudentsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public StudentsModel(ApplicationDbContext context) => _context = context;

        public List<StudentRow> Students { get; set; } = new();

        public record StudentRow(string FullName, string? Email, string? StudentNumber, int RecordCount);

        public async Task OnGetAsync()
        {
            var studentIds = await RoleQueries.GetUserIdsInRoleAsync(_context, SeedData.StudentRole);

            var users = await _context.Users
                .Where(u => studentIds.Contains(u.Id))
                .ToListAsync();

            var recordCounts = await _context.AttendanceRecords
                .Where(a => studentIds.Contains(a.StudentId))
                .GroupBy(a => a.StudentId)
                .Select(g => new { StudentId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.StudentId, x => x.Count);

            Students = users
                .Select(u => new StudentRow(u.FullName, u.Email, u.StudentNumber, recordCounts.GetValueOrDefault(u.Id, 0)))
                .OrderBy(s => s.FullName)
                .ToList();
        }
    }
}
