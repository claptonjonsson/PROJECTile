using PROJECTile.Core.Models;

namespace PROJECTile.Core.Services;

public sealed class CodeTodoScanner
{
    private static readonly string DefaultTodoMarker = "/" + "/TODO";

    private static readonly string[] DefaultMarkers = [DefaultTodoMarker];

    private static readonly string[] DefaultIgnoredDirectories =
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
    private readonly CodeTodoScanOptions _scanOptions;

    public CodeTodoScanner(string workspaceRoot, CodeTodoScanOptions? scanOptions = null)
    {
        _workspaceRoot = Path.GetFullPath(workspaceRoot);
        _scanOptions = scanOptions ?? new CodeTodoScanOptions();
    }

    public IReadOnlyList<CodeTodoItem> Scan(ProjectDocument document)
    {
        return Scan(document, document.CodeTodoScan);
    }

    public IReadOnlyList<CodeTodoItem> Scan(ProjectDocument document, CodeTodoScanOptions? scanOptions)
    {
        List<CodeTodoItem> items = [];
        Dictionary<string, int> occurrences = new(StringComparer.Ordinal);
        CodeTodoScanOptions effectiveScanOptions = CreateEffectiveScanOptions(_scanOptions, scanOptions);

        foreach (string filePath in EnumerateFiles(_workspaceRoot, effectiveScanOptions.IgnoredDirectories))
        {
            ScanFile(filePath, document, effectiveScanOptions.Markers, occurrences, items);
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
        IReadOnlyList<string> markers,
        Dictionary<string, int> occurrences,
        List<CodeTodoItem> items)
    {
        string relativePath = Path.GetRelativePath(_workspaceRoot, filePath)
            .Replace(Path.DirectorySeparatorChar, '/');

        int lineNumber = 0;
        foreach (string line in File.ReadLines(filePath))
        {
            lineNumber++;
            foreach ((int MarkerIndex, string Marker) markerMatch in FindMarkerMatches(line, markers))
            {
                int todoTextStart = markerMatch.MarkerIndex + markerMatch.Marker.Length;
                string normalizedText = NormalizeTodoText(line[todoTextStart..]);
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
    }

    private static IEnumerable<string> EnumerateFiles(string root, IReadOnlyList<string> ignoredDirectories)
    {
        HashSet<string> ignoredDirectoryNames = ignoredDirectories.ToHashSet(StringComparer.OrdinalIgnoreCase);
        Stack<string> pending = new();
        pending.Push(root);

        while (pending.Count > 0)
        {
            string directory = pending.Pop();

            foreach (string childDirectory in Directory.EnumerateDirectories(directory))
            {
                string name = Path.GetFileName(childDirectory);
                if (!ignoredDirectoryNames.Contains(name))
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

    private static IEnumerable<(int MarkerIndex, string Marker)> FindMarkerMatches(
        string line,
        IReadOnlyList<string> markers)
    {
        return markers
            .Select(marker => (MarkerIndex: line.IndexOf(marker, StringComparison.Ordinal), Marker: marker))
            .Where(match => match.MarkerIndex >= 0)
            .OrderBy(match => match.MarkerIndex);
    }

    private static CodeTodoScanOptions CreateEffectiveScanOptions(params CodeTodoScanOptions?[] scanOptions)
    {
        return new CodeTodoScanOptions
        {
            Markers = CleanValues(
                DefaultMarkers,
                scanOptions.SelectMany(options => options?.Markers ?? []),
                StringComparer.Ordinal),
            IgnoredDirectories = CleanValues(
                DefaultIgnoredDirectories,
                scanOptions.SelectMany(options => options?.IgnoredDirectories ?? []),
                StringComparer.OrdinalIgnoreCase)
        };
    }

    private static List<string> CleanValues(
        IEnumerable<string> defaultValues,
        IEnumerable<string> projectValues,
        StringComparer comparer)
    {
        HashSet<string> seen = new(comparer);
        List<string> values = [];

        foreach (string value in defaultValues.Concat(projectValues))
        {
            string trimmed = value.Trim();
            if (trimmed.Length > 0 && seen.Add(trimmed))
            {
                values.Add(trimmed);
            }
        }

        return values;
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
