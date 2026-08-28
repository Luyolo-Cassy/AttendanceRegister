using System.ComponentModel.DataAnnotations;
using AttendanceRegister.Data;
using AttendanceRegister.Models;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AttendanceRegister.Pages.Lecturer
{
    public class UploadModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public UploadModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty, Required]
        public IFormFile? UploadFile { get; set; }

        public ImportSummary? Summary { get; set; }

        public class ImportSummary
        {
            public int LecturesCreated { get; set; }
            public int LecturesMatched { get; set; }
            public int AccountsCreated { get; set; }
            public int RecordsCreated { get; set; }
            public int RecordsUpdated { get; set; }
            public int RowsSkipped { get; set; }
            public List<string> SkippedStudentNumbers { get; set; } = new();
        }

        public void OnGet() { }

        private static XLWorkbook? TryOpenWorkbook(MemoryStream buffer, out string? error)
        {
            try
            {
                error = null;
                return new XLWorkbook(buffer);
            }
            catch (Exception)
            {
                error = "That file couldn't be read as an Excel workbook. Make sure it's a genuine .xlsx file (not .xls or a renamed .csv) and try again.";
                return null;
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (UploadFile is null || UploadFile.Length == 0)
            {
                ModelState.AddModelError(nameof(UploadFile), "Please choose a .xlsx file.");
                return Page();
            }

            var lecturer = await _userManager.GetUserAsync(User);
            var summary = new ImportSummary();

            // Copy into a MemoryStream first: ClosedXML/OpenXML needs a fully seekable stream,
            // and the stream ASP.NET Core hands back from IFormFile.OpenReadStream() isn't
            // always safe to read directly - this was throwing "File contains corrupted data"
            // even for a valid .xlsx before this fix.
            using var buffer = new MemoryStream();
            await UploadFile.CopyToAsync(buffer);
            buffer.Position = 0;

            using var workbook = TryOpenWorkbook(buffer, out var openError);
            if (workbook is null)
            {
                ModelState.AddModelError(nameof(UploadFile), openError!);
                return Page();
            }

            var worksheet = workbook.Worksheet(1);
            var headerRow = worksheet.Row(1);

            // Columns 1 and 2 are Student Name / Student No; every column after that is a
            // lecture date. Reading the header once up front keeps the per-row loop simple.
            var lastColumn = worksheet.LastColumnUsed()!.ColumnNumber();
            var dateColumns = new List<(int Column, DateOnly Date)>();
            for (var col = 3; col <= lastColumn; col++)
            {
                var headerText = headerRow.Cell(col).GetString().Trim();
                if (DateOnly.TryParse(headerText, out var parsedDate))
                {
                    dateColumns.Add((col, parsedDate));
                }
            }

            // Find-or-create a Lecture per date column so re-uploading the same file is safe
            // (matches existing lectures instead of duplicating them).
            var lecturesByDate = new Dictionary<DateOnly, Lecture>();
            foreach (var (_, date) in dateColumns)
            {
                if (lecturesByDate.ContainsKey(date)) continue;

                var existing = await _context.Lectures.FirstOrDefaultAsync(l => l.Date == date);
                if (existing is not null)
                {
                    lecturesByDate[date] = existing;
                    summary.LecturesMatched++;
                }
                else
                {
                    var created = new Lecture
                    {
                        Title = $"Lecture - {date:yyyy-MM-dd}",
                        Date = date,
                        CheckInCode = "IMPRT" + date.DayOfYear, // not used for self check-in on imported lectures
                        CreatedByLecturerId = lecturer?.Id
                    };
                    _context.Lectures.Add(created);
                    lecturesByDate[date] = created;
                    summary.LecturesCreated++;
                }
            }
            await _context.SaveChangesAsync();

            var lastRow = worksheet.LastRowUsed()!.RowNumber();
            for (var row = 2; row <= lastRow; row++)
            {
                var studentNo = worksheet.Cell(row, 2).GetString().Trim();
                if (string.IsNullOrWhiteSpace(studentNo)) continue;
                var studentName = worksheet.Cell(row, 1).GetString().Trim();

                var student = await _context.Users.FirstOrDefaultAsync(u => u.StudentNumber == studentNo);

                if (student is null)
                {
                    // Bulk-created accounts get a shared default password since a spreadsheet
                    // import has no way to collect one per student - fine for coursework/demo
                    // data, not for onboarding real students (they'd register themselves).
                    var email = $"{studentNo.ToLower()}@student.uct.ac.za";
                    var newUser = new ApplicationUser
                    {
                        UserName = email,
                        Email = email,
                        FullName = string.IsNullOrWhiteSpace(studentName) ? studentNo : studentName,
                        StudentNumber = studentNo,
                        EmailConfirmed = true
                    };

                    var createResult = await _userManager.CreateAsync(newUser, "Student@2026!");
                    if (createResult.Succeeded)
                    {
                        await _userManager.AddToRoleAsync(newUser, SeedData.StudentRole);
                        student = newUser;
                        summary.AccountsCreated++;
                    }
                }

                if (student is null)
                {
                    summary.RowsSkipped++;
                    summary.SkippedStudentNumbers.Add(studentNo);
                    continue;
                }

                foreach (var (col, date) in dateColumns)
                {
                    var cellValue = worksheet.Cell(row, col).GetString().Trim();
                    var status = cellValue == "1" ? AttendanceStatus.Present : AttendanceStatus.Absent;
                    var lecture = lecturesByDate[date];

                    var record = await _context.AttendanceRecords
                        .FirstOrDefaultAsync(a => a.LectureId == lecture.Id && a.StudentId == student.Id);

                    if (record is null)
                    {
                        _context.AttendanceRecords.Add(new AttendanceRecord
                        {
                            LectureId = lecture.Id,
                            StudentId = student.Id,
                            Status = status,
                            RecordedBy = "Import"
                        });
                        summary.RecordsCreated++;
                    }
                    else
                    {
                        record.Status = status;
                        record.RecordedBy = "Import";
                        record.RecordedAtUtc = DateTime.UtcNow;
                        summary.RecordsUpdated++;
                    }
                }
            }

            await _context.SaveChangesAsync();
            Summary = summary;
            return Page();
        }
    }
}
