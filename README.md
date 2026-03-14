# Pomodoro Timer — Mendix Studio Pro Extension

A dockable Pomodoro timer that lives inside Mendix Studio Pro. Track 25-minute focus sessions without leaving the IDE.

## Features

- 25-minute work sessions with 5-minute short breaks and a 15-minute long break after every 4 sessions
- Circular countdown ring with Start / Pause / Reset controls
- Session progress dots (4 Pomodoros before a long break)
- Studio Pro native popup notification when a session ends
- Dark mode support

## Requirements

- Mendix Studio Pro 11.8+ (macOS)
- .NET 10 SDK (`brew install dotnet`)

## Build

```bash
cd /Users/joshua.moesa/workdir/github/PomodoroTimer
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

## Project Structure

```
PomodoroTimer/
├── manifest.json                          # Extension entry point
├── MyCompany.MyProject.PomodoroTimer.csproj
├── PomodoroMenuExtension.cs               # Adds menu item to Extensions menu
├── PomodoroPaneExtension.cs               # Registers the dockable pane
├── PomodoroPaneViewModel.cs               # WebView init + C#↔JS message bridge
└── PomodoroWebServer.cs                   # Serves the HTML/CSS/JS timer UI
```

## How it works

The timer UI is a self-contained HTML page served by a built-in local web server (`PomodoroWebServer`). Studio Pro embeds a WebView that loads this page. The countdown runs in JavaScript. When a session ends, JavaScript calls `postMessage` which C# receives and uses to show a Studio Pro notification.
