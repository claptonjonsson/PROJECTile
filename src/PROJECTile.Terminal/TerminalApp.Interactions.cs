using PROJECTile.Core.Models;
using PROJECTile.Core.Services;
using Spectre.Console;

namespace PROJECTile.Terminal;

internal sealed partial class TerminalApp
{
    private bool AddItem()
    {
        if (_section == ProjectSection.Resources)
        {
            string title = PromptText("Resource title", "");
            if (string.IsNullOrWhiteSpace(title))
            {
                return true;
            }

            string body = PromptText("Resource body", "");
            ProjectResource resource = _resources.Add(title, body);
            Save();
            _detail = ListEntry.FromResource(resource);
            _focus = AppFocus.Detail;
            return true;
        }

        string taskTitle = PromptText("Task title", "");
        if (string.IsNullOrWhiteSpace(taskTitle))
        {
            return true;
        }

        string taskBody = PromptText("Task body", "");
        ProjectTask task = _tasks.Add(taskTitle, taskBody, StatusForSection(_section));
        Save();
        _detail = ListEntry.FromTask(task);
        _focus = AppFocus.Detail;
        return true;
    }

    private bool EditSelected()
    {
        ListEntry? entry = _detail ?? GetSelectedEntry();
        if (entry?.Task is not null)
        {
            string title = PromptText("Task title", entry.Task.Title);
            if (string.IsNullOrWhiteSpace(title))
            {
                return true;
            }

            string body = PromptText("Task body", entry.Task.Body);
            _tasks.Update(entry.Task.Id, title, body);
            Save();
        }
        else if (entry?.Resource is not null)
        {
            string title = PromptText("Resource title", entry.Resource.Title);
            if (string.IsNullOrWhiteSpace(title))
            {
                return true;
            }

            string body = PromptText("Resource body", entry.Resource.Body);
            _resources.Update(entry.Resource.Id, title, body);
            Save();
        }
        else if (entry?.CodeTodo is not null)
        {
            string body = PromptText("Code TODO note", entry.CodeTodo.NoteBody);
            CodeTodoScanner.SetNote(_document, entry.CodeTodo, body);
            Save();
            RefreshCodeTodos();
            _detail = FindCodeTodoDetail(entry.CodeTodo.TargetId);
        }

        return true;
    }

    private bool DeleteSelected()
    {
        ListEntry? entry = _detail ?? GetSelectedEntry();
        if (entry?.Task is not null && Confirm($"Delete task '{entry.Task.Title}'?"))
        {
            _tasks.Delete(entry.Task.Id);
            AfterDeleted();
        }
        else if (entry?.Resource is not null && Confirm($"Delete resource '{entry.Resource.Title}'?"))
        {
            _resources.Delete(entry.Resource.Id);
            AfterDeleted();
        }

        return true;
    }

    private bool MoveSelectedTask()
    {
        ListEntry? entry = _detail ?? GetSelectedEntry();
        if (entry?.Task is null)
        {
            return true;
        }

        _tasks.MoveNext(entry.Task.Id);
        Save();
        ClampListIndex();

        if (_focus == AppFocus.Detail)
        {
            _detail = ListEntry.FromTask(entry.Task);
        }

        return true;
    }

    private bool Refresh()
    {
        RefreshCodeTodos();
        ShowMessage("[green]Code TODOs refreshed.[/]");
        return true;
    }

    private bool Help()
    {
        AnsiConsole.Clear();
        Grid grid = new();
        grid.AddColumn();
        grid.AddRow(new Markup("[bold]Keys[/]"));
        grid.AddRow(new Markup("[green]j/k[/] move"));
        grid.AddRow(new Markup("[green]h[/] back to nav/list"));
        grid.AddRow(new Markup("[green]l/Enter[/] open; on detail, link resource"));
        grid.AddRow(new Markup("[green]q[/] quit/back"));
        grid.AddRow(new Markup("[green]a[/] add saved task/resource"));
        grid.AddRow(new Markup("[green]e[/] edit saved item or code TODO note"));
        grid.AddRow(new Markup("[green]d[/] delete saved task/resource"));
        grid.AddRow(new Markup("[green]m[/] move saved task status"));
        grid.AddRow(new Markup("[green]r[/] refresh code TODO scan"));
        grid.AddRow(new Markup(""));
        grid.AddRow(new Markup("[grey]Press any key.[/]"));
        AnsiConsole.Write(new Panel(grid).Header("Help"));
        Console.ReadKey(intercept: true);
        return true;
    }

    private void LinkCurrentDetail()
    {
        if (_detail?.Resource is not null)
        {
            LinkFromResource(_detail.Resource);
        }
        else if (_detail?.Task is not null)
        {
            LinkResourceToTarget(ResourceLinkTargetType.Task, _detail.Task.Id);
        }
        else if (_detail?.CodeTodo is not null)
        {
            LinkResourceToTarget(ResourceLinkTargetType.CodeTodo, _detail.CodeTodo.TargetId);
        }
    }

    private void LinkFromResource(ProjectResource resource)
    {
        string choice = Choose("Link resource to", ["Saved task", "Code TODO", "Cancel"]);
        if (choice == "Saved task")
        {
            ProjectTask? task = ChooseTask();
            if (task is not null)
            {
                _resources.LinkToTask(resource.Id, task.Id);
                Save();
            }
        }
        else if (choice == "Code TODO")
        {
            CodeTodoItem? codeTodo = ChooseCodeTodo();
            if (codeTodo is not null)
            {
                _resources.LinkToCodeTodo(resource.Id, codeTodo);
                Save();
            }
        }
    }

    private void LinkResourceToTarget(ResourceLinkTargetType targetType, string targetId)
    {
        ProjectResource? resource = ChooseResource();
        if (resource is null)
        {
            return;
        }

        if (targetType == ResourceLinkTargetType.Task)
        {
            _resources.LinkToTask(resource.Id, targetId);
        }
        else
        {
            _resources.LinkToCodeTodo(resource.Id, targetId);
        }

        Save();
    }

    private ProjectTask? ChooseTask()
    {
        Dictionary<string, ProjectTask> choices = _document.Tasks
            .Select((task, index) => ($"{index + 1}. [{task.Status}] {task.Title}", task))
            .ToDictionary(pair => pair.Item1, pair => pair.task, StringComparer.Ordinal);

        string? selected = ChooseOptional("Task", choices.Keys);
        return selected is null ? null : choices[selected];
    }

    private ProjectResource? ChooseResource()
    {
        Dictionary<string, ProjectResource> choices = _document.Resources
            .Select((resource, index) => ($"{index + 1}. {resource.Title}", resource))
            .ToDictionary(pair => pair.Item1, pair => pair.resource, StringComparer.Ordinal);

        string? selected = ChooseOptional("Resource", choices.Keys);
        return selected is null ? null : choices[selected];
    }

    private CodeTodoItem? ChooseCodeTodo()
    {
        Dictionary<string, CodeTodoItem> choices = _codeTodos
            .Select((item, index) => ($"{index + 1}. {item.DisplayTitle}", item))
            .ToDictionary(pair => pair.Item1, pair => pair.item, StringComparer.Ordinal);

        string? selected = ChooseOptional("Code TODO", choices.Keys);
        return selected is null ? null : choices[selected];
    }

    private string Choose(string title, IEnumerable<string> choices)
    {
        return AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title(title)
                .AddChoices(choices));
    }

    private string? ChooseOptional(string title, IEnumerable<string> choices)
    {
        List<string> allChoices = choices.ToList();
        if (allChoices.Count == 0)
        {
            ShowMessage($"[yellow]No {Markup.Escape(title.ToLowerInvariant())} items available.[/]");
            return null;
        }

        allChoices.Add("Cancel");
        string selected = Choose(title, allChoices);
        return selected == "Cancel" ? null : selected;
    }

    private void AfterDeleted()
    {
        Save();
        _detail = null;
        _focus = AppFocus.List;
        ClampListIndex();
    }

    private string PromptText(string label, string current)
    {
        AnsiConsole.Clear();
        TextPrompt<string> prompt = new TextPrompt<string>($"{label}:").AllowEmpty();
        if (current.Length > 0)
        {
            prompt.DefaultValue(current).ShowDefaultValue();
        }

        return AnsiConsole.Prompt(prompt);
    }

    private static bool Confirm(string message)
    {
        AnsiConsole.Clear();
        return AnsiConsole.Confirm(message);
    }

    private static void ShowMessage(string markup)
    {
        AnsiConsole.Clear();
        AnsiConsole.MarkupLine(markup);
        AnsiConsole.MarkupLine("[grey]Press any key.[/]");
        Console.ReadKey(intercept: true);
    }
}
