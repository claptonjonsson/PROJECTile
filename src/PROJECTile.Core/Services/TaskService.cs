using PROJECTile.Core.Models;

namespace PROJECTile.Core.Services;

public sealed class TaskService
{
    private readonly ProjectDocument _document;

    public TaskService(ProjectDocument document)
    {
        _document = document;
    }

    public ProjectTask Add(string title, string body, ProjectTaskStatus status)
    {
        ProjectTask task = new()
        {
            Id = ServiceHelpers.NewId(),
            Title = ServiceHelpers.CleanRequired(title, "Task title"),
            Body = body.Trim(),
            Status = status
        };

        _document.Tasks.Add(task);
        return task;
    }

    public void Update(string id, string title, string body)
    {
        ProjectTask task = FindRequired(id);
        task.Title = ServiceHelpers.CleanRequired(title, "Task title");
        task.Body = body.Trim();
    }

    public void Move(string id, ProjectTaskStatus status)
    {
        FindRequired(id).Status = status;
    }

    public void MoveNext(string id)
    {
        ProjectTask task = FindRequired(id);
        task.Status = task.Status switch
        {
            ProjectTaskStatus.Todo => ProjectTaskStatus.Doing,
            ProjectTaskStatus.Doing => ProjectTaskStatus.Done,
            _ => ProjectTaskStatus.Todo
        };
    }

    public void Delete(string id)
    {
        _document.Tasks.RemoveAll(task => task.Id == id);
        _document.ResourceLinks.RemoveAll(link =>
            link.TargetType == ResourceLinkTargetType.Task && link.TargetId == id);
    }

    public ProjectTask? Find(string id)
    {
        return _document.Tasks.FirstOrDefault(task => task.Id == id);
    }

    private ProjectTask FindRequired(string id)
    {
        return ServiceHelpers.FindRequired(Find(id), "Task", id);
    }
}
