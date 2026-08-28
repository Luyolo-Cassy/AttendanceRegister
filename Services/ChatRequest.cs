namespace AttendanceRegister.Services
{
    public class ChatRequest
    {
        public List<ChatMessageDto> History { get; set; } = new();
    }

    public class ChatMessageDto
    {
        public string Role { get; set; } = "user";
        public string Content { get; set; } = string.Empty;
    }
}
