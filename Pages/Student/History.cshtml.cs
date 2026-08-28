using AttendanceRegister.Data;
using AttendanceRegister.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AttendanceRegister.Pages.Student
{
    public class HistoryModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HistoryModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public List<AttendanceRecord> Records { get; set; } = new();

        public async Task OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return;

            Records = await _context.AttendanceRecords
                .Include(a => a.Lecture)
                .Where(a => a.StudentId == user.Id)
                .OrderByDescending(a => a.Lecture!.Date)
                .ToListAsync();
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
