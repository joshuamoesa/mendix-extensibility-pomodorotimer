# Pomodoro Timer — Mendix Studio Pro Extension

A dockable Pomodoro timer that lives inside Mendix Studio Pro. Track focus sessions against your user stories without leaving the IDE.

## Features

- Configurable work / short break / long break durations (defaults: 25 / 5 / 15 min)
- Circular countdown ring with Start / Pause / Reset controls
- Session progress dots (4 Pomodoros before a long break)
- **User story tracking** — type or select from a personal story list before each session
- **Session history** — see every completed Pomodoro with task name and time
- **Settings panel** — manage your user story list and customize durations
- Studio Pro native popup notification when a session ends
- Dark mode support

## Requirements

- Mendix Studio Pro 11.8+ (macOS)
- .NET 10 SDK (`brew install dotnet`)

## Build

```bash
dotnet build MyCompany.MyProject.PomodoroTimer.csproj
```

The post-build step automatically copies the output to:
```
/Users/joshua.moesa/workdir/Mendix/MCPDemo-main_2/extensions/PomodoroTimer/
```

## Install in Studio Pro

1. Build the project (see above)
2. Open the target app in Studio Pro
3. Press `F4` (Synchronize App Directory)
4. Restart Studio Pro with the extension development flag:

```bash
open -a "Mendix Studio Pro 11.8.0 Beta" --args --enable-extension-development
```

5. Go to **Extensions → Open Pomodoro Timer**

## Usage

1. Type a task or select a user story from the dropdown
2. Click **Start** to begin a 25-minute focus session
3. A Studio Pro notification appears when the session ends
4. Completed sessions are logged in the **History** panel
5. Click **⚙** to open Settings — manage user stories and adjust durations

## Project Structure

```
PomodoroTimer/
├── manifest.json                          # Extension entry point
├── MyCompany.MyProject.PomodoroTimer.csproj
├── PomodoroMenuExtension.cs               # Adds menu item to Extensions menu
├── PomodoroPaneExtension.cs               # Registers the dockable pane
├── PomodoroPaneViewModel.cs               # WebView init + C#↔JS message bridge
├── PomodoroHistoryStore.cs                # In-memory session log (MEF singleton)
├── PomodoroStoryStore.cs                  # In-memory user story list (MEF singleton)
└── PomodoroWebServer.cs                   # Serves the HTML/CSS/JS timer UI
```

## How it works

The timer UI is a self-contained HTML page served by a built-in local web server (`PomodoroWebServer`). Studio Pro embeds a WebView that loads this page. The countdown runs in JavaScript. When a session ends, JavaScript calls `postMessage` which C# receives, stores the record in `PomodoroHistoryStore`, and shows a Studio Pro notification.

User stories and history persist for the lifetime of the Studio Pro session. Both are cleared when Studio Pro is quit.
