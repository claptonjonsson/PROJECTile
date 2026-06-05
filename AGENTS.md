# AGENTS

PROJECTile is a .NET terminal app for managing project tasks, resources, documents, and code TODOs.

## Structure

- `src/PROJECTile.Core`: domain models, persistence, and services.
- `src/PROJECTile.Terminal`: Spectre.Console terminal UI.
- `sandbox/PROJECTile.Sandbox`: local playground for manual experiments.
- `.github/workflows`: CI and release automation.
- `.skills/coding-guidelines`: repo coding guidelines for reviewable code.

## Agent guidance

- Be terse: read/search only what is needed, summarize noisy output, avoid repeating unchanged context.
- Be constructively critical: call out risks and weaker alternatives.
- For code changes, follow `.skills/coding-guidelines/SKILL.md`.

## References

- Spectre.Console: https://spectreconsole.net/
- Native AOT: https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/
