# Personal working style for this project

## Main rule
- Before changing anything, first explain the plan in 3-6 short points.
- Do not make code changes until I approve the plan.
- Prefer the simplest working solution.
- Do not add advanced patterns, abstractions, optimization, events, managers, interfaces, generics, dependency injection, scriptable architecture, or extra systems unless I explicitly ask for them.

## Code style
- Write beginner-friendly Unity C#.
- Keep scripts small and easy to read.
- One script = one clear responsibility.
- Prefer direct and obvious solutions over clever ones.
- Avoid overengineering.
- Avoid making many files if one simple file is enough.
- Keep methods short.
- Keep public fields simple when appropriate for a student project.

## Naming
- Use clear but simple names.
- Names can be a little casual, but they must still be understandable.
- Do not invent overly professional or enterprise-style names.
- Prefer names like `moveSpeed`, `jumpPower`, `isOnGround`, `coinCount`, `hurtTimer`.
- If unsure, choose the most obvious and plain name.

## Comments
- Comments should be rare and only when they help.
- Comments should sound natural and simple, not overly formal.
- Short reminder-style comments are okay.
- Do not add many comments.
- Do not write “AI-style” explanatory essays inside code.
- Small casual comments are okay sometimes, for example like:
  - `// jump a bit higher here`
  - `// idk why but this works`
  - `// маленькая задержка (´・ω・`)`

## How to work with me
- Explain things simply, as if I am still learning.
- If there is a risky change, warn me first.
- If there are multiple options, give me the easiest one first.
- When editing code, keep the current project structure unless I ask to reorganize it.
- For Unity, prefer solutions that are easy to inspect in the Editor.

## When using assets
- First search the project for matching asset names before asking me.
- If several assets match, list 2-5 likely candidates and ask me to choose.
- Do not assume a specific asset if the filename is ambiguous.
- If the asset is visual and the filename is not enough, ask me to attach or point to the file.

## Output format
- First: short plan
- Then: what file(s) you want to touch
- Then: the code change
- Then: a very short explanation of what changed