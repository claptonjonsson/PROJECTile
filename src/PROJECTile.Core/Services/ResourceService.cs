using PROJECTile.Core.Models;

namespace PROJECTile.Core.Services;

public sealed class ResourceService
{
    private readonly ProjectDocument _document;

    public ResourceService(ProjectDocument document)
    {
        _document = document;
    }

    public ProjectResource Add(string title, string body)
    {
        ProjectResource resource = new()
        {
            Id = NewId(),
            Title = CleanRequired(title, "Resource title"),
            Body = body.Trim()
        };

        _document.Resources.Add(resource);
        return resource;
    }

    public void Update(string id, string title, string body)
    {
        ProjectResource resource = FindRequired(id);
        resource.Title = CleanRequired(title, "Resource title");
        resource.Body = body.Trim();
    }

    public void Delete(string id)
    {
        _document.Resources.RemoveAll(resource => resource.Id == id);
        _document.ResourceLinks.RemoveAll(link => link.ResourceId == id);
    }

    public void LinkToTask(string resourceId, string taskId)
    {
        FindRequired(resourceId);
        AddLink(resourceId, ResourceLinkTargetType.Task, taskId);
    }

    public void LinkToCodeTodo(string resourceId, CodeTodoItem item)
    {
        LinkToCodeTodo(resourceId, item.TargetId);
    }

    public void LinkToCodeTodo(string resourceId, string targetId)
    {
        FindRequired(resourceId);
        AddLink(resourceId, ResourceLinkTargetType.CodeTodo, targetId);
    }

    public IReadOnlyList<ProjectResource> FindResourcesFor(ResourceLinkTargetType targetType, string targetId)
    {
        HashSet<string> ids = _document.ResourceLinks
            .Where(link => link.TargetType == targetType && link.TargetId == targetId)
            .Select(link => link.ResourceId)
            .ToHashSet(StringComparer.Ordinal);

        return _document.Resources
            .Where(resource => ids.Contains(resource.Id))
            .OrderBy(resource => resource.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public ProjectResource? Find(string id)
    {
        return _document.Resources.FirstOrDefault(resource => resource.Id == id);
    }

    private void AddLink(string resourceId, ResourceLinkTargetType targetType, string targetId)
    {
        bool exists = _document.ResourceLinks.Any(link =>
            link.ResourceId == resourceId &&
            link.TargetType == targetType &&
            link.TargetId == targetId);

        if (!exists)
        {
            _document.ResourceLinks.Add(new ResourceLink
            {
                ResourceId = resourceId,
                TargetType = targetType,
                TargetId = targetId
            });
        }
    }

    private ProjectResource FindRequired(string id)
    {
        return Find(id) ?? throw new InvalidOperationException($"Resource '{id}' was not found.");
    }

    private static string CleanRequired(string value, string name)
    {
        string trimmed = value.Trim();
        return trimmed.Length == 0
            ? throw new ArgumentException($"{name} is required.", nameof(value))
            : trimmed;
    }

    private static string NewId()
    {
        return Guid.NewGuid().ToString("N");
    }
}
