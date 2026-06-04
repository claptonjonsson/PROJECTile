using PROJECTile.Core.Models;
using PROJECTile.Core.Persistence;
using PROJECTile.Core.Services;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace PROJECTile.Terminal;

internal sealed class TerminalApp
{
    private static readonly ProjectSection[] Sections =
    [
        ProjectSection.Todo,
        ProjectSection.Doing,
        ProjectSection.Done,
        ProjectSection.Resources
    ];

    private readonly ProjectStore _store;
    private readonly CodeTodoScanner _scanner;
    private ProjectDocument _document;
    private TaskService _tasks;
    private ResourceService _resources;
    private IReadOnlyList<CodeTodoItem> _codeTodos = [];
    private AppFocus _focus = AppFocus.List;
    private ProjectSection _section = ProjectSection.Todo;
    private int _navIndex;
    private int _listIndex;
    private ListEntry? _detail;

    public TerminalApp(string workspaceRoot)
    {
        _store = new ProjectStore(workspaceRoot);
        _scanner = new CodeTodoScanner(workspaceRoot);
        _document = ProjectStore.CreateDefault();
        _tasks = new TaskService(_document);
        _resources = new ResourceService(_document);
    }

    public void Run()
    {
        _document = _store.LoadOrCreate();
        RebindServices();
        RefreshCodeTodos();

        bool running = true;
        while (running)
        {
            Render();
            ConsoleKeyInfo key = Console.ReadKey(intercept: true);

            try
            {
                running = HandleKey(key);
            }
            catch (Exception ex)
            {
                ShowMessage($"[red]{Markup.Escape(ex.Message)}[/]");
            }
        }

        AnsiConsole.Clear();
    }

    private bool HandleKey(ConsoleKeyInfo key)
    {
        if (key.Key == ConsoleKey.Enter)
        {
            OpenOrLink();
            return true;
        }

        return key.KeyChar switch
        {
            'j' => MoveDown(),
            'k' => MoveUp(),
            'h' => Back(),
            'l' => OpenOrLink(),
            'q' => QuitOrBack(),
            'a' => AddItem(),
            'e' => EditSelected(),
            'd' => DeleteSelected(),
            'm' => MoveSelectedTask(),
            'r' => Refresh(),
            '?' => Help(),
            _ => true
        };
    }

    private bool MoveDown()
    {
        if (_focus == AppFocus.Nav)
        {
            _navIndex = Math.Min(_navIndex + 1, Sections.Length - 1);
            _section = Sections[_navIndex];
            ClampListIndex();
        }
        else if (_focus == AppFocus.List)
        {
            _listIndex = Math.Min(_listIndex + 1, Math.Max(0, BuildEntries().Count - 1));
        }

        return true;
    }

    private bool MoveUp()
    {
        if (_focus == AppFocus.Nav)
        {
            _navIndex = Math.Max(0, _navIndex - 1);
            _section = Sections[_navIndex];
            ClampListIndex();
        }
        else if (_focus == AppFocus.List)
        {
            _listIndex = Math.Max(0, _listIndex - 1);
        }

        return true;
    }

    private bool Back()
    {
        if (_focus == AppFocus.Detail)
        {
            _detail = null;
            _focus = AppFocus.List;
        }
        else if (_focus == AppFocus.List)
        {
            _focus = AppFocus.Nav;
        }

        return true;
    }

    private bool OpenOrLink()
    {
        if (_focus == AppFocus.Nav)
        {
            _focus = AppFocus.List;
            return true;
        }

        if (_focus == AppFocus.List)
        {
            _detail = GetSelectedEntry();
            if (_detail is not null)
            {
                _focus = AppFocus.Detail;
            }

            return true;
        }

        LinkCurrentDetail();
        return true;
    }

    private bool QuitOrBack()
    {
        if (_focus == AppFocus.Nav)
        {
            return false;
        }

        return Back();
    }

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

    private void Save()
    {
        _store.Save(_document);
    }

    private void RefreshCodeTodos()
    {
        _codeTodos = _scanner.Scan(_document);
        ClampListIndex();
    }

    private void RebindServices()
    {
        _tasks = new TaskService(_document);
        _resources = new ResourceService(_document);
    }

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

    private IReadOnlyList<ListEntry> BuildEntries()
    {
        if (_section == ProjectSection.Resources)
        {
            return _document.Resources.Select(ListEntry.FromResource).ToList();
        }

        ProjectTaskStatus status = StatusForSection(_section);
        List<ListEntry> entries = _document.Tasks
            .Where(task => task.Status == status)
            .Select(ListEntry.FromTask)
            .ToList();

        if (_section == ProjectSection.Todo)
        {
            entries.AddRange(_codeTodos.Select(ListEntry.FromCodeTodo));
        }

        return entries;
    }

    private ListEntry? GetSelectedEntry()
    {
        IReadOnlyList<ListEntry> entries = BuildEntries();
        if (entries.Count == 0)
        {
            return null;
        }

        ClampListIndex();
        return entries[_listIndex];
    }

    private ListEntry? FindCodeTodoDetail(string targetId)
    {
        CodeTodoItem? item = _codeTodos.FirstOrDefault(todo => todo.TargetId == targetId);
        return item is null ? null : ListEntry.FromCodeTodo(item);
    }

    private void ClampListIndex()
    {
        int max = Math.Max(0, BuildEntries().Count - 1);
        _listIndex = Math.Min(_listIndex, max);
    }

    private static ProjectTaskStatus StatusForSection(ProjectSection section)
    {
        return section switch
        {
            ProjectSection.Doing => ProjectTaskStatus.Doing,
            ProjectSection.Done => ProjectTaskStatus.Done,
            _ => ProjectTaskStatus.Todo
        };
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
