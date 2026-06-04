using System.Text.Json.Serialization;

namespace PROJECTile.Core.Models;

public sealed class CodeTodoNote
{
    public string FilePath { get; set; } = "";
    public string NormalizedText { get; set; } = "";
    public int OccurrenceIndex { get; set; }
    public string Body { get; set; } = "";

    [JsonIgnore]
    public string TargetId => CodeTodoIdentity.CreateId(FilePath, NormalizedText, OccurrenceIndex);
}
