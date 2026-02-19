using Notifs;

string appName = "NotifsTestApp";
string title = "Notifs test";
string message = "Desktop/terminal notification check";

NotificationMode mode = NotificationMode.AutoDesktopFirst;
TerminalNotificationPreference terminalPreference = TerminalNotificationPreference.Auto;
bool disableDesktopFallback = false;
bool throwOnFailure = true;
bool verboseRouting = false;

for (int i = 0; i < args.Length; i++)
{
  string arg = args[i];
  if ((arg == "--mode" || arg == "-m") && i + 1 < args.Length)
  {
    mode = ParseMode(args[++i]);
    continue;
  }

  if ((arg == "--terminal" || arg == "-t") && i + 1 < args.Length)
  {
    terminalPreference = ParseTerminalPreference(args[++i]);
    continue;
  }

  if (arg == "--no-desktop-fallback")
  {
    disableDesktopFallback = true;
    continue;
  }

  if (arg == "--no-throw")
  {
    throwOnFailure = false;
    continue;
  }

  if (arg == "--verbose-routing")
  {
    verboseRouting = true;
    continue;
  }

  if ((arg == "--title" || arg == "-T") && i + 1 < args.Length)
  {
    title = args[++i];
    continue;
  }

  if ((arg == "--message" || arg == "-M") && i + 1 < args.Length)
  {
    message = args[++i];
    continue;
  }

  if (arg == "--help" || arg == "-h")
  {
    PrintHelp();
    return;
  }

  Console.WriteLine($"Unknown argument: {arg}");
  PrintHelp();
  return;
}

Console.WriteLine($"TERM_PROGRAM={Environment.GetEnvironmentVariable("TERM_PROGRAM") ?? "<null>"}");
Console.WriteLine($"TERM={Environment.GetEnvironmentVariable("TERM") ?? "<null>"}");
Console.WriteLine($"WT_SESSION={Environment.GetEnvironmentVariable("WT_SESSION") ?? "<null>"}");
Console.WriteLine($"Mode={mode}, Terminal={terminalPreference}, DisableDesktopFallback={disableDesktopFallback}, ThrowOnFailure={throwOnFailure}");

await Notification.NotifyAsync(
  appName,
  title,
  message,
  new NotificationOptions
  {
    Mode = mode,
    TerminalPreference = terminalPreference,
    DisableDesktopFallback = disableDesktopFallback,
    ThrowOnFailure = throwOnFailure,
    EnableDebugOutput = verboseRouting
  });

Console.WriteLine("Notification call completed.");

static NotificationMode ParseMode(string value)
{
  return value.Trim().ToLowerInvariant() switch
  {
    "auto" or "autodesktopfirst" => NotificationMode.AutoDesktopFirst,
    "autoterminalfirst" or "autoterminal" => NotificationMode.AutoTerminalFirst,
    "desktop" or "desktoponly" => NotificationMode.DesktopOnly,
    "terminal" or "terminalonly" => NotificationMode.TerminalOnly,
    "off" => NotificationMode.Off,
    _ => throw new ArgumentException($"Unsupported mode: {value}")
  };
}

static TerminalNotificationPreference ParseTerminalPreference(string value)
{
  return value.Trim().ToLowerInvariant() switch
  {
    "auto" => TerminalNotificationPreference.Auto,
    "osc9" or "osc9only" => TerminalNotificationPreference.Osc9Only,
    "bel" or "belonly" => TerminalNotificationPreference.BelOnly,
    _ => throw new ArgumentException($"Unsupported terminal preference: {value}")
  };
}

static void PrintHelp()
{
  Console.WriteLine("Usage: dotnet run --project src/NotifsTestApp -- [options]");
  Console.WriteLine("Options:");
  Console.WriteLine("  -m, --mode <auto|autoterminal|desktop|terminal|off>");
  Console.WriteLine("  -t, --terminal <auto|osc9|bel>");
  Console.WriteLine("      --no-desktop-fallback");
  Console.WriteLine("      --no-throw");
  Console.WriteLine("      --verbose-routing");
  Console.WriteLine("  -T, --title <text>");
  Console.WriteLine("  -M, --message <text>");
  Console.WriteLine("  -h, --help");
}
