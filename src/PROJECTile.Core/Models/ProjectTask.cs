namespace PROJECTile.Core.Models;

public sealed class ProjectTask
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public ProjectTaskStatus Status { get; set; } = ProjectTaskStatus.Todo;
}
