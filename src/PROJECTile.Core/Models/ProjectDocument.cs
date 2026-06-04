namespace PROJECTile.Core.Models;

public sealed class ProjectDocument
{
    public int SchemaVersion { get; set; } = 1;
    public List<ProjectTask> Tasks { get; set; } = [];
    public List<ProjectResource> Resources { get; set; } = [];
    public List<ResourceLink> ResourceLinks { get; set; } = [];
    public List<CodeTodoNote> CodeTodoNotes { get; set; } = [];
}
