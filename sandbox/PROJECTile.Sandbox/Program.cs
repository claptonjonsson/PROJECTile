using PROJECTile.Core.Persistence;
using PROJECTile.Core.Services;

string workspaceRoot = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();
ProjectStore store = new(workspaceRoot);
var document = store.LoadOrCreate();
var todos = new CodeTodoScanner(workspaceRoot).Scan(document);

Console.WriteLine($"Store: {store.FilePath}");
Console.WriteLine($"Tasks: {document.Tasks.Count}");
Console.WriteLine($"Resources: {document.Resources.Count}");
Console.WriteLine($"Code TODOs: {todos.Count}");
