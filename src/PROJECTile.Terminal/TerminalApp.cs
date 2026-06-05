using PROJECTile.Core.Models;
using PROJECTile.Core.Persistence;
using PROJECTile.Core.Services;
using Spectre.Console;

namespace PROJECTile.Terminal;

internal sealed partial class TerminalApp
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
}
