using System.ComponentModel.DataAnnotations;

namespace AttendanceRegister.Models
{
    public enum AttendanceStatus
    {
        Present,
        Absent,
        Late,
        Excused
    }

    // One row per (student, lecture) pair. RecordedBy + RecordedAtUtc give an audit trail so
    // a lecturer can see whether a record came from the student's own check-in or from a
    // lecturer edit/import, which matters when a student queries a disputed record.
    public class AttendanceRecord
    {
        public int Id { get; set; }

        [Required]
        public int LectureId { get; set; }
        public Lecture? Lecture { get; set; }

        [Required]
        public string StudentId { get; set; } = string.Empty;
        public ApplicationUser? Student { get; set; }

        [Required]
        public AttendanceStatus Status { get; set; }

        public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;

        // "Student" (self check-in), "Lecturer" (manual edit), or "Import" (spreadsheet upload).
        [StringLength(20)]
        public string RecordedBy { get; set; } = "Lecturer";

        [StringLength(300)]
        public string? Notes { get; set; }
    }
}
