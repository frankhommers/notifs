# Notifs

Simple cross-platform desktop notifications for .NET.

![NuGet](https://img.shields.io/nuget/v/Notifs)
![CI](https://github.com/frankhommers/notifs/actions/workflows/ci.yml/badge.svg)

## Installation

```sh
dotnet add package Notifs
```

## Usage

```csharp
await Notifs.Notification.NotifyAsync("MyApp", "Title", "Message body");
```

### Notification routing modes

```csharp
var options = new Notifs.NotificationOptions
{
    Mode = Notifs.NotificationMode.AutoDesktopFirst,
    TerminalPreference = Notifs.TerminalNotificationPreference.Auto,
    DisableDesktopFallback = false,
    ThrowOnFailure = false
};

await Notifs.Notification.NotifyAsync("MyApp", "Title", "Message body", options);
```

Available modes:

- `AutoDesktopFirst` (default): desktop first, terminal fallback (`OSC 777`, then `OSC 9`, then `BEL`)
- `AutoTerminalFirst`: terminal first (`OSC 777`, then `OSC 9`, then `BEL`), desktop fallback
- `DesktopOnly`: desktop notifications only
- `TerminalOnly`: terminal notifications only
- `Off`: disable notifications

Additional options:

- `TerminalPreference`: `Auto` (default), `Osc9Only`, `BelOnly`
- `DisableDesktopFallback`: disables fallback path in auto modes (`false` by default)
- `ThrowOnFailure`: throws when no route can deliver (`false` by default)
- `EnableDebugOutput`: writes routing/debug output to stderr (`false` by default)

## Platform support

| Platform | Implementation       | Requirement               |
|----------|----------------------|---------------------------|
| Windows  | Toast (WinRT via PS) | PowerShell                |
| macOS    | osascript            | Built-in                  |
| Linux    | libnotify            | `notify-send` installed   |

Terminal notifications are auto-detected for supported terminals such as Ghostty, WezTerm, and iTerm2. When available, routing prefers `OSC 777`, then `OSC 9`, then `BEL`.

On Linux, install `notify-send` via your package manager:

```sh
# Debian/Ubuntu
apt install libnotify-bin
# Fedora
dnf install libnotify
```

## Manual smoke test (macOS)

Use this script to manually verify desktop and terminal notification behavior on macOS (for example Ghostty focus/timing behavior):

```sh
./scripts/smoke-notifications-macos.sh
```

Useful flags:

- `--sleep-before=SECONDS`: wait before terminal sends (default `5`)
- `--manual`: enable interactive yes/no prompts

## License

MIT — see [LICENSE](LICENSE)
