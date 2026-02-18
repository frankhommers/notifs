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
await Notifs.Notifs.NotifyAsync("MyApp", "Title", "Message body");
```

## Platform support

| Platform | Implementation       | Requirement               |
|----------|----------------------|---------------------------|
| Windows  | Toast (WinRT via PS) | PowerShell                |
| macOS    | osascript            | Built-in                  |
| Linux    | libnotify            | `notify-send` installed   |

On Linux, install `notify-send` via your package manager:

```sh
# Debian/Ubuntu
apt install libnotify-bin
# Fedora
dnf install libnotify
```

## License

MIT — see [LICENSE](LICENSE)
