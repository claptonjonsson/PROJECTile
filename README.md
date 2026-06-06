# PROJECTile

PROJECTile is a simple terminal project-management app for a single repository.
It stores project data in one repo-local file, `projectile.json`.

## What it is

PROJECTile is intentionally lightweight and focused:

- Project tasks (Todo / Doing / Done)
- Project resources and notes
- Code TODO notes discovered across your codebase

Everything is kept in `projectile.json` in the repository root, which is created when you first run the app in a folder.

By default, PROJECTile scans your codebase for `//TODO` comments and makes them available for tracking.

## Tech stack

- Single-binary native AOT C# app
- [Spectre.Console](https://spectreconsole.net/) terminal UI
- `projectile.json`-driven local persistence

## In-app commands

Inside the app, press `?` to open the built-in command help.

Common keys:

- `j` / `k` move up and down
- `h` go back
- `l` / `Enter` open or link
- `q` quit / back
- `a` add
- `e` edit
- `d` delete
- `m` move task status
- `r` refresh code TODO scan
- `?` show command help

## Why use it

PROJECTile is designed to stay close to your project: one repo, one JSON file, one terminal UI, and no extra external systems.
