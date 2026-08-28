using Microsoft.AspNetCore.Identity;

namespace AttendanceRegister.Models
{
    // Extends the built-in Identity user with the extra fields the case study asks for.
    // StudentNumber is only meaningful for accounts in the "Student" role, so it stays nullable
    // rather than forcing lecturers to have a fake one (avoids hard-coded placeholder data).
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;

        public string? StudentNumber { get; set; }

        public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
    }
}
