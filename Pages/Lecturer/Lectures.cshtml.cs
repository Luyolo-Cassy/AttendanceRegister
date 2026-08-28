using System.ComponentModel.DataAnnotations;
using AttendanceRegister.Data;
using AttendanceRegister.Models;
using AttendanceRegister.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AttendanceRegister.Pages.Lecturer
{
    public class LecturesModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAttendanceCodeGenerator _codeGenerator;

        public LecturesModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IAttendanceCodeGenerator codeGenerator)
        {
            _context = context;
            _userManager = userManager;
            _codeGenerator = codeGenerator;
        }

        public List<Lecture> Lectures { get; set; } = new();
        public Dictionary<int, int> PresentCounts { get; set; } = new();

        [BindProperty]
        public NewLectureInput NewLecture { get; set; } = new();

        public class NewLectureInput
        {
            [Required, StringLength(150)]
            public string Title { get; set; } = string.Empty;

            [Required]
            public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);
        }

        public async Task OnGetAsync()
        {
            await LoadAsync();
        }

        private async Task LoadAsync()
        {
            Lectures = await _context.Lectures.OrderByDescending(l => l.Date).ToListAsync();
            PresentCounts = await _context.AttendanceRecords
                .Where(a => a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late)
                .GroupBy(a => a.LectureId)
                .Select(g => new { LectureId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.LectureId, x => x.Count);
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadAsync();
                return Page();
            }

            var user = await _userManager.GetUserAsync(User);

            // Guard against the (very unlikely) chance of generating a code that's already
            // in use by another lecture - keeps the unique-code assumption in CheckIn valid.
            string code;
            do
            {
                code = _codeGenerator.GenerateCode();
            } while (await _context.Lectures.AnyAsync(l => l.CheckInCode == code));

            _context.Lectures.Add(new Lecture
            {
                Title = NewLecture.Title,
                Date = NewLecture.Date,
                CheckInCode = code,
                CreatedByLecturerId = user?.Id
            });
            await _context.SaveChangesAsync();

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostOpenWindowAsync(int id)
        {
            var lecture = await _context.Lectures.FindAsync(id);
            if (lecture is not null)
            {
                lecture.CheckInOpensAt = DateTime.UtcNow;
                lecture.CheckInClosesAt = DateTime.UtcNow.AddMinutes(15);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostCloseWindowAsync(int id)
        {
            var lecture = await _context.Lectures.FindAsync(id);
            if (lecture is not null)
            {
                lecture.CheckInClosesAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }
    }
}
