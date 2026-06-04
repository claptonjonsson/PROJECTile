using System.Text.Json;
using PROJECTile.Core.Models;

namespace PROJECTile.Core.Persistence;

public sealed class ProjectStore
{
    public const string FileName = "projectile.json";

    private readonly string _filePath;

    public ProjectStore(string workspaceRoot)
    {
        WorkspaceRoot = Path.GetFullPath(workspaceRoot);
        _filePath = Path.Combine(WorkspaceRoot, FileName);
    }

    public string WorkspaceRoot { get; }
    public string FilePath => _filePath;

    public ProjectDocument LoadOrCreate()
    {
        if (!File.Exists(_filePath))
        {
            ProjectDocument created = CreateDefault();
            Save(created);
            return created;
        }

        using FileStream stream = File.OpenRead(_filePath);
        ProjectDocument? document = JsonSerializer.Deserialize(stream, ProjectJsonContext.Default.ProjectDocument);
        if (document is null)
        {
            throw new InvalidOperationException($"{FileName} is empty or invalid.");
        }

        if (document.SchemaVersion != 1)
        {
            throw new InvalidOperationException($"Unsupported projectile.json schema version {document.SchemaVersion}.");
        }

        document.Tasks ??= [];
        document.Resources ??= [];
        document.ResourceLinks ??= [];
        document.CodeTodoNotes ??= [];
        return document;
    }

    public void Save(ProjectDocument document)
    {
        document.SchemaVersion = 1;

        string? directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using FileStream stream = File.Create(_filePath);
        JsonSerializer.Serialize(stream, document, ProjectJsonContext.Default.ProjectDocument);
    }

    public static ProjectDocument CreateDefault()
    {
        return new ProjectDocument { SchemaVersion = 1 };
    }
}
