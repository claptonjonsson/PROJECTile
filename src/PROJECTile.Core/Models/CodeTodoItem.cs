namespace PROJECTile.Core.Models;

public sealed class CodeTodoItem
{
    public string FilePath { get; init; } = "";
    public int LineNumber { get; init; }
    public string NormalizedText { get; init; } = "";
    public int OccurrenceIndex { get; init; }
    public string NoteBody { get; init; } = "";

    public string TargetId => CodeTodoIdentity.CreateId(FilePath, NormalizedText, OccurrenceIndex);
    public string DisplayTitle => $"{FilePath}:{LineNumber} {NormalizedText}";
}
