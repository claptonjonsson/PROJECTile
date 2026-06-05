using PROJECTile.Core.Models;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace PROJECTile.Terminal;

internal sealed partial class TerminalApp
{
    private void Render()
    {
        AnsiConsole.Clear();

        Layout layout = new Layout("root")
            .SplitColumns(
                new Layout("nav").Size(24),
                new Layout("main"));

        layout["nav"].Update(new Panel(BuildNav()).Header("PROJECTile"));
        layout["main"].Update(new Panel(BuildMain()).Header(BuildHeader()));

        AnsiConsole.Write(layout);
        AnsiConsole.MarkupLine("[grey]j/k move  h back  l/enter open/link  a add  e edit  d delete  m move  r refresh  ? help  q quit/back[/]");
    }

    private IRenderable BuildNav()
    {
        Grid grid = new();
        grid.AddColumn();

        for (int index = 0; index < Sections.Length; index++)
        {
            ProjectSection section = Sections[index];
            string marker = _focus == AppFocus.Nav && index == _navIndex ? ">" : " ";
            string label = section.ToString();
            string style = section == _section ? "lime" : "white";
            grid.AddRow(new Markup($"[{style}]{marker} {label}[/]"));
        }

        return grid;
    }

    private IRenderable BuildMain()
    {
        return _focus == AppFocus.Detail && _detail is not null
            ? BuildDetail(_detail)
            : BuildList();
    }

    private IRenderable BuildList()
    {
        IReadOnlyList<ListEntry> entries = BuildEntries();
        if (entries.Count == 0)
        {
            return new Markup("[grey]No items.[/]");
        }

        Table table = new Table().NoBorder();
        table.AddColumn("");
        table.AddColumn("Type");
        table.AddColumn("Title");

        for (int index = 0; index < entries.Count; index++)
        {
            ListEntry entry = entries[index];
            string marker = _focus == AppFocus.List && index == _listIndex ? "[lime]>[/]" : " ";
            table.AddRow(marker, Markup.Escape(entry.Kind), Markup.Escape(entry.Title));
        }

        return table;
    }

    private IRenderable BuildDetail(ListEntry entry)
    {
        Grid grid = new();
        grid.AddColumn();

        if (entry.Task is not null)
        {
            grid.AddRow(new Markup($"[bold]{Markup.Escape(entry.Task.Title)}[/]"));
            grid.AddRow(new Markup($"Status: [green]{entry.Task.Status}[/]"));
            AddBody(grid, entry.Task.Body);
            AddLinkedResources(grid, ResourceLinkTargetType.Task, entry.Task.Id);
        }
        else if (entry.Resource is not null)
        {
            grid.AddRow(new Markup($"[bold]{Markup.Escape(entry.Resource.Title)}[/]"));
            AddBody(grid, entry.Resource.Body);
            AddResourceTargets(grid, entry.Resource.Id);
            grid.AddRow(new Markup("[grey]Press l to link this resource.[/]"));
        }
        else if (entry.CodeTodo is not null)
        {
            grid.AddRow(new Markup($"[bold]{Markup.Escape(entry.CodeTodo.NormalizedText)}[/]"));
            grid.AddRow(new Markup($"File: [green]{Markup.Escape(entry.CodeTodo.FilePath)}:{entry.CodeTodo.LineNumber}[/]"));
            AddBody(grid, entry.CodeTodo.NoteBody.Length == 0 ? "No note." : entry.CodeTodo.NoteBody);
            AddLinkedResources(grid, ResourceLinkTargetType.CodeTodo, entry.CodeTodo.TargetId);
        }

        return grid;
    }

    private void AddBody(Grid grid, string body)
    {
        grid.AddRow(new Markup(""));
        grid.AddRow(new Markup(Markup.Escape(body.Length == 0 ? "No body." : body)));
        grid.AddRow(new Markup(""));
    }

    private void AddLinkedResources(Grid grid, ResourceLinkTargetType targetType, string targetId)
    {
        IReadOnlyList<ProjectResource> linked = _resources.FindResourcesFor(targetType, targetId);
        grid.AddRow(new Markup("[bold]Linked resources[/]"));

        if (linked.Count == 0)
        {
            grid.AddRow(new Markup("[grey]None. Press l to link one.[/]"));
            return;
        }

        foreach (ProjectResource resource in linked)
        {
            grid.AddRow(new Markup($"- {Markup.Escape(resource.Title)}"));
        }
    }

    private void AddResourceTargets(Grid grid, string resourceId)
    {
        IReadOnlyList<ResourceLink> links = _document.ResourceLinks
            .Where(link => link.ResourceId == resourceId)
            .ToList();

        grid.AddRow(new Markup("[bold]Linked items[/]"));
        if (links.Count == 0)
        {
            grid.AddRow(new Markup("[grey]None.[/]"));
            return;
        }

        foreach (ResourceLink link in links)
        {
            grid.AddRow(new Markup($"- {Markup.Escape(DescribeLink(link))}"));
        }
    }

    private string DescribeLink(ResourceLink link)
    {
        if (link.TargetType == ResourceLinkTargetType.Task)
        {
            ProjectTask? task = _tasks.Find(link.TargetId);
            return task is null ? "Missing task" : $"Task [{task.Status}] {task.Title}";
        }

        CodeTodoItem? codeTodo = _codeTodos.FirstOrDefault(item => item.TargetId == link.TargetId);
        return codeTodo is null ? "Missing code TODO" : $"Code TODO {codeTodo.DisplayTitle}";
    }

    private string BuildHeader()
    {
        return _focus == AppFocus.Detail ? "Detail" : _section.ToString();
    }
}
