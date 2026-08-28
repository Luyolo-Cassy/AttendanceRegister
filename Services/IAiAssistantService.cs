namespace AttendanceRegister.Services
{
    public record ChatTurn(string Role, string Content); // Role: "user" or "assistant"

    public interface IAiAssistantService
    {
        // Single-shot: one system prompt, one user message, one reply. Used by the nudge
        // drafter and the "ask about your class" query box, where each request is independent.
        Task<string> CompleteAsync(string systemPrompt, string userMessage, CancellationToken ct = default);

        // Multi-turn: used by the floating chat widget, which keeps its own transcript
        // client-side and resends it each turn (the app has no server-side chat session store).
        Task<string> ChatAsync(string systemPrompt, IEnumerable<ChatTurn> history, CancellationToken ct = default);
    }
}
