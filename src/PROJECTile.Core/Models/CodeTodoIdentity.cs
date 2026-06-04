using System.Text;

namespace PROJECTile.Core.Models;

public static class CodeTodoIdentity
{
    public static string CreateId(string filePath, string normalizedText, int occurrenceIndex)
    {
        return $"{Encode(filePath)}.{occurrenceIndex}.{Encode(normalizedText)}";
    }

    private static string Encode(string value)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
