using System.ComponentModel.DataAnnotations;
using AttendanceRegister.Data;
using AttendanceRegister.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AttendanceRegister.Pages.Student
{
    public class CheckInModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CheckInModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        [Required, StringLength(6, MinimumLength = 4)]
        public string Code { get; set; } = string.Empty;

        public CheckInResult? Result { get; set; }

        public record CheckInResult(bool Success, string Message);

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Challenge();

            var now = DateTime.UtcNow;
            var lecture = await _context.Lectures
                .FirstOrDefaultAsync(l => l.CheckInCode == Code.Trim().ToUpper());

            if (lecture is null)
            {
                Result = new CheckInResult(false, "That code doesn't match any lecture.");
                return Page();
            }

            if (!lecture.IsCheckInOpen(now))
            {
                Result = new CheckInResult(false, $"Attendance for {lecture.Title} isn't open right now.");
                return Page();
            }

            var existing = await _context.AttendanceRecords
                .FirstOrDefaultAsync(a => a.LectureId == lecture.Id && a.StudentId == user.Id);

            if (existing is not null)
            {
                Result = new CheckInResult(true, $"You're already checked in for {lecture.Title}.");
                return Page();
            }

            _context.AttendanceRecords.Add(new AttendanceRecord
            {
                LectureId = lecture.Id,
                StudentId = user.Id,
                Status = AttendanceStatus.Present,
                RecordedBy = "Student",
                RecordedAtUtc = now
            });
            await _context.SaveChangesAsync();

            Result = new CheckInResult(true, $"Checked in for {lecture.Title}. See you there!");
            return Page();
        }
    }
}
