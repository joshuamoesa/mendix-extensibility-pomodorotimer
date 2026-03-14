# Claude Context — PomodoroTimer

## What this project is

A Mendix Studio Pro extension written in C# (.NET 10). It adds a dockable Pomodoro timer pane to the IDE. Extensions are loaded by Studio Pro at startup from the app's `extensions/` folder.

## Key facts

- **Target**: Studio Pro 11.8 (macOS), `Mendix.StudioPro.ExtensionsAPI` v11.8.0
- **Framework**: net10.0
- **Pattern**: MEF (Managed Extensibility Framework) — `[Export]` / `[ImportingConstructor]` attributes wire everything together; Studio Pro does the instantiation
- **Deploy target**: `/Users/joshua.moesa/workdir/Mendix/MCPDemo-main_2/extensions/PomodoroTimer/`
- **Launch flag required**: `--enable-extension-development`

## Build

```bash
dotnet build MyCompany.MyProject.PomodoroTimer.csproj
```

Post-build rsync copies output automatically to the Mendix app's extensions folder.

## Architecture

```
PomodoroMenuExtension   → adds "Open Pomodoro Timer" to Extensions menu
PomodoroPaneExtension   → registers dockable pane, injects IMessageBoxService
PomodoroPaneViewModel   → sets Title, loads WebView URL, handles JS postMessage
PomodoroWebServer       → serves /pomodoro route with inline HTML/CSS/JS
```

## Known API constraints (discovered via reflection on the DLL)

- `DockablePaneExtension` does **not** have a `ViewMenuCaption` property in 11.8.0 — it was removed. Use `Title` on the view model instead.
- `PomodoroWebServer` uses no constructor parameters, so `[method: ImportingConstructor]` must not be applied to it.
- Pane title is set via `Title = "..."` inside `InitWebView()` on the view model.

## JS ↔ C# message bridge

JavaScript sends messages using:
```js
window.webkit.messageHandlers.studioPro.postMessage(...)  // macOS WebKit
window.chrome.webview.postMessage(...)                     // Windows WebView2
```

C# receives in `PomodoroPaneViewModel.OnMessageReceived`. Messages: `WorkComplete`, `BreakComplete`.
