using PROJECTile.Core.Models;

namespace PROJECTile.Terminal;

internal sealed partial class TerminalApp
{
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
}
