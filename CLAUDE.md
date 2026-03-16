# Claude Context — PomodoroTimer

## What this project is

A Mendix Studio Pro extension written in C# (.NET 10). It adds a dockable Pomodoro timer pane to the IDE. Extensions are loaded by Studio Pro at startup from the app's `extensions/` folder.

## Key facts

- **Target**: Studio Pro 11.8 (macOS + Windows), `Mendix.StudioPro.ExtensionsAPI` v11.8.0
- **Framework**: net10.0
- **Pattern**: MEF (Managed Extensibility Framework) — `[Export]` / `[ImportingConstructor]` attributes wire everything together; Studio Pro does the instantiation
- **Deploy target**: `/Users/joshua.moesa/workdir/Mendix/MCPDemo-main_2/extensions/PomodoroTimer/` (post-build rsync only runs if this path exists — safe for other contributors)
- **Launch flag required**: `--enable-extension-development`

## Build

```bash
dotnet build MyCompany.MyProject.PomodoroTimer.csproj
```

Post-build rsync copies output automatically to the Mendix app's extensions folder, but only if the target path exists (conditional on `Exists(...)` in the `.csproj`). Safe to build on any machine.

## Architecture

```
PomodoroMenuExtension   → adds "Open Pomodoro Timer" to Extensions menu
PomodoroPaneExtension   → registers dockable pane; injects IMessageBoxService,
                          PomodoroHistoryStore, PomodoroStoryStore
PomodoroPaneViewModel   → sets Title, loads WebView URL, handles JS postMessage
PomodoroHistoryStore    → MEF singleton; in-memory list of completed sessions
PomodoroStoryStore      → MEF singleton; in-memory list of user stories
PomodoroWebServer       → serves /pomodoro, /history, /stories routes
```

## Known API constraints (discovered via reflection on the DLL)

- `DockablePaneExtension` does **not** have a `ViewMenuCaption` property in 11.8.0 — it was removed. Use `Title` on the view model instead.
- `PomodoroWebServer` now has constructor parameters so `[method: ImportingConstructor]` is required.
- Pane title is set via `Title = "..."` inside `InitWebView()` on the view model.

## JS ↔ C# message bridge

JavaScript sends messages using:
```js
window.webkit.messageHandlers.studioPro.postMessage(payload)  // macOS WebKit
window.chrome.webview.postMessage(payload)                     // Windows WebView2
```

All messages are JSON strings. C# parses with `System.Text.Json.JsonDocument`.

### Message types (JS → C#)

| type | fields | action |
|------|--------|--------|
| `WorkComplete` | `task` | adds record to `PomodoroHistoryStore`, shows notification |
| `BreakComplete` | — | shows notification |
| `AddStory` | `story` | adds to `PomodoroStoryStore` |
| `RemoveStory` | `story` | removes from `PomodoroStoryStore` |

## UI state management pattern

**Do not use server fetches to update the UI after mutations.** C# message delivery is async and the fetch will race against the store update.

Instead:
- `localHistory` and `localStories` are JS arrays that are the source of truth for the current view
- UI (`renderHistory()`, `renderStories()`) updates immediately from these local arrays
- C# store is updated via `sendMessage` for persistence across pane close/reopen
- Server fetch (`loadHistory()`, `loadStories()`) is only called on page load to restore state

## Routes served by PomodoroWebServer

| route | response |
|-------|----------|
| `/pomodoro` | full HTML/CSS/JS timer page |
| `/history` | JSON array `[{number, task, time}]` |
| `/stories` | JSON array of strings |

## Timer durations

Configurable in the Settings panel (⚙). Stored in the JS `DURATIONS` object (`const` with mutable properties). Applied immediately if the timer is not running; otherwise take effect next phase.

## Distribution

`.mxmodule` files are gitignored. Releases are distributed by attaching the `.mxmodule` to a GitHub Release tag. The Mendix Marketplace syncs from the GitHub Release when the repo is linked as the component source.
