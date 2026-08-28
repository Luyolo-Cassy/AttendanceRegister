using System.ComponentModel.DataAnnotations;

namespace AttendanceRegister.Models
{
    // One row per lecture session. CheckInCode + the open/close window are the anti-proxy
    // mechanism: a lecturer opens attendance in the room and displays the code, so a student
    // can't check in from home. This is one of the "bonus" innovations beyond plain sign-in.
    public class Lecture
    {
        public int Id { get; set; }

        [Required, StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public DateOnly Date { get; set; }

        [Required, StringLength(6)]
        public string CheckInCode { get; set; } = string.Empty;

        // Null = attendance not open yet / already closed.
        public DateTime? CheckInOpensAt { get; set; }
        public DateTime? CheckInClosesAt { get; set; }

        // Nullable: better to record "unknown creator" than corrupt referential integrity
        // with an empty-string FK when the current lecturer can't be resolved (e.g. a stale
        // login cookie after a database reset during development).
        public string? CreatedByLecturerId { get; set; }
        public ApplicationUser? CreatedByLecturer { get; set; }

        public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();

        public bool IsCheckInOpen(DateTime nowUtc) =>
            CheckInOpensAt.HasValue && CheckInClosesAt.HasValue &&
            nowUtc >= CheckInOpensAt.Value && nowUtc <= CheckInClosesAt.Value;
    }
}
