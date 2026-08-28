using AttendanceRegister.Data;
using AttendanceRegister.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AttendanceRegister.Pages.Lecturer
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public EditModel(ApplicationDbContext context) => _context = context;

        [BindProperty]
        public AttendanceRecord Record { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var record = await _context.AttendanceRecords
                .Include(a => a.Student)
                .Include(a => a.Lecture)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (record is null) return NotFound();

            Record = record;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var record = await _context.AttendanceRecords
                .Include(a => a.Student)
                .Include(a => a.Lecture)
                .FirstOrDefaultAsync(a => a.Id == Record.Id);

            if (record is null) return NotFound();

            record.Status = Record.Status;
            record.Notes = Record.Notes;
            record.RecordedBy = "Lecturer";
            record.RecordedAtUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            TempData["StatusMessage"] = "Attendance record updated.";
            return RedirectToPage("/Lecturer/Index");
        }
    }
}
