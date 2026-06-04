using PROJECTile.Terminal;

string workspaceRoot = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();
TerminalApp app = new(workspaceRoot);
app.Run();
