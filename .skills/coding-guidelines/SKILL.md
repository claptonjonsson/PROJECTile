---
name: coding-guidelines
description: Use when writing or refactoring code in this repository; emphasizes human-reviewable code structure, intention-revealing names, small methods, and strict file-size discipline.
---

# Coding Guidelines

Write code for human review first.

## Core principle

- Optimize for code that a reviewer can understand locally, without reconstructing hidden intent.
- Prefer small, obvious changes over clever or broad rewrites.
- Make behavior visible through names, structure, and control flow.

## Names

- Use verbose, intention-revealing method, variable, and type names.
- Prefer domain-specific names over generic names like `data`, `result`, `manager`, `handler`, `item`, or `process`.
- Name methods after the decision or effect they own.
- Avoid abbreviations unless they are standard in the domain.

## Comments

- Prefer clearer names and smaller structure over explanatory comments.
- Use comments only for context the code cannot express, such as external constraints, non-obvious tradeoffs, or compatibility requirements.
- Do not comment what the next line already says.

## Methods and functions

- Keep methods short enough to review in one screen when practical.
- Give each method one clear responsibility.
- Split validation, transformation, persistence, and side effects into visibly separate steps.
- Prefer early returns over deeply nested branching.
- Avoid long parameter lists; group cohesive inputs into named types when that improves clarity.

## Files

- Prefer files under 500 lines.
- Treat 800 lines as a warning sign.
- Do not exceed 800 lines unless there is a strong, explicit reason.
- If a file approaches the limit, split by responsibility, not by arbitrary buckets.

## Reviewability checks

- Can a reviewer understand the change from the local diff?
- Are responsibilities separated enough that failures are easy to locate?
- Are names specific enough that comments are rarely needed?
- Is the smallest useful behavior changed, without speculative restructuring?
- Are edge cases visible near the logic that handles them?

## What to avoid

- Clever code that saves lines but hides intent.
- Large methods with mixed validation, mutation, IO, and formatting.
- Vague helper names.
- Premature abstraction.
- Refactors that change style and behavior at the same time.
- Files that grow because adding code was easier than finding the right boundary.
