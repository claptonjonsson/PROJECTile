using PROJECTile.Core.Models;

namespace PROJECTile.Terminal;

internal sealed class ListEntry
{
    private ListEntry(ProjectTask? task, ProjectResource? resource, CodeTodoItem? codeTodo)
    {
        Task = task;
        Resource = resource;
        CodeTodo = codeTodo;
    }

    public ProjectTask? Task { get; }
    public ProjectResource? Resource { get; }
    public CodeTodoItem? CodeTodo { get; }

    public string StableId => Task?.Id ?? Resource?.Id ?? CodeTodo?.TargetId ?? "";
    public string Kind => Task is not null ? "Task" : Resource is not null ? "Resource" : "Code TODO";
    public string Title => Task?.Title ?? Resource?.Title ?? CodeTodo?.DisplayTitle ?? "";

    public static ListEntry FromTask(ProjectTask task)
    {
        return new ListEntry(task, null, null);
    }

    public static ListEntry FromResource(ProjectResource resource)
    {
        return new ListEntry(null, resource, null);
    }

    public static ListEntry FromCodeTodo(CodeTodoItem codeTodo)
    {
        return new ListEntry(null, null, codeTodo);
    }
}
