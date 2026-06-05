namespace PROJECTile.Core.Models;

public sealed class CodeTodoScanOptions
{
    public List<string> Markers { get; set; } = [];
    public List<string> IgnoredDirectories { get; set; } = [];
}
