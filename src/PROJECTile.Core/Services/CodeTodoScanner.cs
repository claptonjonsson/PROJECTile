using PROJECTile.Core.Models;

namespace PROJECTile.Core.Services;

public sealed class CodeTodoScanner
{
    private static readonly string TodoMarker = "/" + "/TODO";

    private static readonly string[] IgnoredDirectories =
    [
        ".git",
        "bin",
        "obj",
        ".vs",
        ".idea",
        "node_modules",
        "artifacts"
    ];

    private readonly string _workspaceRoot;

    public CodeTodoScanner(string workspaceRoot)
    {
        _workspaceRoot = Path.GetFullPath(workspaceRoot);
    }

    public IReadOnlyList<CodeTodoItem> Scan(ProjectDocument document)
    {
        List<CodeTodoItem> items = [];
        Dictionary<string, int> occurrences = new(StringComparer.Ordinal);

        foreach (string filePath in EnumerateFiles(_workspaceRoot))
        {
            ScanFile(filePath, document, occurrences, items);
        }

        return items
            .OrderBy(item => item.FilePath, StringComparer.Ordinal)
            .ThenBy(item => item.LineNumber)
            .ToList();
    }

    public static CodeTodoNote SetNote(ProjectDocument document, CodeTodoItem item, string body)
    {
        CodeTodoNote? note = document.CodeTodoNotes.FirstOrDefault(existing =>
            existing.FilePath == item.FilePath &&
            existing.NormalizedText == item.NormalizedText &&
            existing.OccurrenceIndex == item.OccurrenceIndex);

        if (note is null)
        {
            note = new CodeTodoNote
            {
                FilePath = item.FilePath,
                NormalizedText = item.NormalizedText,
                OccurrenceIndex = item.OccurrenceIndex
            };
            document.CodeTodoNotes.Add(note);
        }

        note.Body = body.Trim();
        return note;
    }

    private void ScanFile(
        string filePath,
        ProjectDocument document,
        Dictionary<string, int> occurrences,
        List<CodeTodoItem> items)
    {
        string relativePath = Path.GetRelativePath(_workspaceRoot, filePath)
            .Replace(Path.DirectorySeparatorChar, '/');

        int lineNumber = 0;
        foreach (string line in File.ReadLines(filePath))
        {
            lineNumber++;
            int markerIndex = line.IndexOf(TodoMarker, StringComparison.Ordinal);
            if (markerIndex < 0)
            {
                continue;
            }

            string normalizedText = NormalizeTodoText(line[(markerIndex + TodoMarker.Length)..]);
            string occurrenceKey = $"{relativePath}\n{normalizedText}";
            occurrences.TryGetValue(occurrenceKey, out int occurrenceIndex);
            occurrences[occurrenceKey] = occurrenceIndex + 1;

            string noteBody = FindNoteBody(document, relativePath, normalizedText, occurrenceIndex);
            items.Add(new CodeTodoItem
            {
                FilePath = relativePath,
                LineNumber = lineNumber,
                NormalizedText = normalizedText,
                OccurrenceIndex = occurrenceIndex,
                NoteBody = noteBody
            });
        }
    }

    private static IEnumerable<string> EnumerateFiles(string root)
    {
        Stack<string> pending = new();
        pending.Push(root);

        while (pending.Count > 0)
        {
            string directory = pending.Pop();

            foreach (string childDirectory in Directory.EnumerateDirectories(directory))
            {
                string name = Path.GetFileName(childDirectory);
                if (!IgnoredDirectories.Contains(name, StringComparer.OrdinalIgnoreCase))
                {
                    pending.Push(childDirectory);
                }
            }

            foreach (string file in Directory.EnumerateFiles(directory))
            {
                yield return file;
            }
        }
    }

    private static string NormalizeTodoText(string text)
    {
        return string.Join(' ', text.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    private static string FindNoteBody(
        ProjectDocument document,
        string filePath,
        string normalizedText,
        int occurrenceIndex)
    {
        return document.CodeTodoNotes.FirstOrDefault(note =>
            note.FilePath == filePath &&
            note.NormalizedText == normalizedText &&
            note.OccurrenceIndex == occurrenceIndex)?.Body ?? "";
    }
}
