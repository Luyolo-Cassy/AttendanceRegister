namespace AttendanceRegister.Models
{
    public class Notification
    {
        public int Id { get; set; }

        public string StudentId { get; set; } = string.Empty;

        public ApplicationUser? Student { get; set; }

        public string Message { get; set; } = string.Empty;

        public DateTime SentAt { get; set; }

        public bool IsRead { get; set; }
    }
}
