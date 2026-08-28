using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace AttendanceRegister.Services
{
    public class GeminiAssistantService : IAiAssistantService
    {
        private readonly HttpClient _http;
        private readonly GeminiOptions _options;

        public GeminiAssistantService(
            HttpClient http,
            IOptions<GeminiOptions> options)
        {
            _http = http;
            _options = options.Value;
        }

        public Task<string> CompleteAsync(
            string systemPrompt,
            string userMessage,
            CancellationToken ct = default)
        {
            return ChatAsync(
                systemPrompt,
                new[] { new ChatTurn("user", userMessage) },
                ct);
        }

        public async Task<string> ChatAsync(
            string systemPrompt,
            IEnumerable<ChatTurn> history,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                return "Gemini API key is not configured.";
            }

            var conversation = new StringBuilder();

            conversation.AppendLine($"System: {systemPrompt}");

            foreach (var turn in history)
            {
                conversation.AppendLine($"{turn.Role}: {turn.Content}");
            }

            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text = conversation.ToString()
                            }
                        }
                    }
                }
            };

            var url =
                $"https://generativelanguage.googleapis.com/v1beta/models/{_options.Model}:generateContent?key={_options.ApiKey}";

            try
            {
                var response = await _http.PostAsync(
                    url,
                    new StringContent(
                        JsonSerializer.Serialize(payload),
                        Encoding.UTF8,
                        "application/json"),
                    ct);

                var body = await response.Content.ReadAsStringAsync(ct);

                if (!response.IsSuccessStatusCode)
                {
                    return $"Gemini request failed ({(int)response.StatusCode}): {body}";
                }

                using var doc = JsonDocument.Parse(body);

                return doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString()
                    ?? "No response returned.";
            }
            catch (Exception ex)
            {
                return $"Gemini error: {ex.Message}";
            }
        }
    }
}
