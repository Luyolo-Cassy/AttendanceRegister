namespace AttendanceRegister.Services
{
    public interface IAttendanceCodeGenerator
    {
        string GenerateCode();
    }

    // Generates a short, human-readable code a lecturer can read out or write on a slide.
    // Excludes visually ambiguous characters (0/O, 1/I) so students can type it correctly.
    public class AttendanceCodeGenerator : IAttendanceCodeGenerator
    {
        private const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
        private static readonly Random Random = new();

        public string GenerateCode()
        {
            var chars = new char[5];
            for (var i = 0; i < chars.Length; i++)
            {
                chars[i] = Alphabet[Random.Next(Alphabet.Length)];
            }
            return new string(chars);
        }
    }
}
