# PomodoroTimer

[![standard-readme compliant](https://img.shields.io/badge/readme%20style-standard-brightgreen.svg?style=flat-square)](https://github.com/RichardLitt/standard-readme)
[![.NET](https://img.shields.io/badge/.NET-10-purple?style=flat-square)](https://dotnet.microsoft.com)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg?style=flat-square)](LICENSE)

A dockable Pomodoro timer extension for Mendix Studio Pro. Track focus sessions against your user stories without leaving the IDE.

## Table of Contents

- [Background](#background)
- [Install](#install)
- [Usage](#usage)
- [Publishing](#publishing)
- [Maintainers](#maintainers)
- [Contributing](#contributing)
- [License](#license)

## Background

Mendix Studio Pro supports custom extensions via the [Extensibility API](https://docs.mendix.com/apidocs-mxsdk/apidocs/studio-pro-11/extensibility-api/). This extension adds a dockable Pomodoro timer pane to the IDE so developers can manage focus sessions and user story tracking without switching context.

**Features:**

- Configurable work / short break / long break durations (defaults: 25 / 5 / 15 min)
- Circular countdown ring with Start / Pause / Reset controls
- Session progress dots (4 Pomodoros before a long break)
- User story tracking — type or select from a personal story list before each session
- Session history — see every completed Pomodoro with task name and time
- Settings panel — manage your user story list and customize durations
- Studio Pro native popup notification when a session ends
- Dark mode support

**How it works:**

The timer UI is a self-contained HTML page served by a built-in local web server (`PomodoroWebServer`). Studio Pro embeds a WebView that loads this page. The countdown runs in JavaScript. When a session ends, JavaScript calls `postMessage` which C# receives, stores the record in `PomodoroHistoryStore`, and shows a Studio Pro notification.

User stories and history persist for the lifetime of the Studio Pro session. Both are cleared when Studio Pro is quit.

## Install

**Prerequisites:**

- Mendix Studio Pro 11.8+
- .NET 10 SDK

**Build:**

```bash
git clone https://github.com/joshuamoesa/PomodoroTimer.git
cd PomodoroTimer
dotnet build MyCompany.MyProject.PomodoroTimer.csproj
```

The post-build step automatically copies the compiled output to your Mendix app's `extensions/PomodoroTimer/` folder if it exists on your machine. Contributors without that path will have the step silently skipped.

**Load in Studio Pro:**

1. Build the project (see above)
2. Open the target Mendix app in Studio Pro
3. Press `F4` to synchronize the app directory
4. Restart Studio Pro with the extension development flag:

**macOS:**
```bash
open -a "Mendix Studio Pro 11.8.0 Beta" --args --enable-extension-development
```

**Windows:**
```
"C:\Program Files\Mendix\11.8.0\modeler\studiopro.exe" --enable-extension-development
```

5. Go to **Extensions → Open Pomodoro Timer**

## Usage

1. Type a task or select a user story from the dropdown
2. Click **Start** to begin a 25-minute focus session
3. A Studio Pro notification appears when the session ends
4. Completed sessions are logged in the **History** panel
5. Click **⚙** to open Settings — manage user stories and adjust durations

## Publishing

The packaged extension is distributed as a `.mxmodule` file attached to a GitHub Release. The Mendix Marketplace can sync directly from a GitHub Release tag.

1. Tag the release commit and push:

```bash
git tag v1.0.0
git push origin v1.0.0
```

2. On GitHub, create a Release from the tag and attach the `.mxmodule` file
3. On [Mendix Marketplace](https://marketplace.mendix.com), click **Add Content** and link the GitHub Release as the component source

`.mxmodule` files are excluded from git via `.gitignore` — always distribute via GitHub Releases, not by committing to the repo.

## Maintainers

[@joshuamoesa](https://github.com/joshuamoesa)

## Contributing

Issues and pull requests are welcome. For significant changes, open an issue first to discuss the approach.

All SDK objects used in the extension are loaded via MEF. Any contribution touching the C# extension classes must maintain the `[Export]` / `[ImportingConstructor]` pattern or Studio Pro will silently fail to load the extension.

## License

[MIT](LICENSE) © Joshua Moesa
