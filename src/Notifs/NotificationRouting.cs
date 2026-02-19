namespace Notifs;

internal enum NotificationRoute
{
  Desktop,
  Osc9,
  Osc777,
  Bel
}

internal readonly struct TerminalCapabilities
{
  public TerminalCapabilities(bool supportsOsc9, bool supportsOsc777, bool supportsBel)
  {
    SupportsOsc9 = supportsOsc9;
    SupportsOsc777 = supportsOsc777;
    SupportsBel = supportsBel;
  }

  public bool SupportsOsc9 { get; }
  public bool SupportsOsc777 { get; }
  public bool SupportsBel { get; }
}

internal static class NotificationRouting
{
  public static TerminalCapabilities DetectTerminalCapabilities(
    Func<string, string?> getEnvironmentVariable,
    bool isOutputRedirected)
  {
    if (isOutputRedirected)
    {
      return new TerminalCapabilities(supportsOsc9: false, supportsOsc777: false, supportsBel: false);
    }

    if (!string.IsNullOrWhiteSpace(getEnvironmentVariable("WT_SESSION")))
    {
      return new TerminalCapabilities(supportsOsc9: false, supportsOsc777: false, supportsBel: true);
    }

    string termProgram = Normalize(getEnvironmentVariable("TERM_PROGRAM"));
    string term = Normalize(getEnvironmentVariable("TERM"));
    bool hasItermSession = !string.IsNullOrWhiteSpace(getEnvironmentVariable("ITERM_SESSION_ID"));

    bool supportsOsc9 = termProgram == "ghostty"
      || termProgram == "wezterm"
      || termProgram == "itermapp"
      || hasItermSession
      || term == "wezterm"
      || term == "weztermmux"
      || term == "xtermkitty"
      || term == "xtermghostty";

    bool supportsOsc777 = termProgram == "ghostty"
      || termProgram == "wezterm"
      || term == "wezterm"
      || term == "weztermmux"
      || term == "xtermghostty";

    return new TerminalCapabilities(supportsOsc9, supportsOsc777, supportsBel: true);
  }

  public static IReadOnlyList<NotificationRoute> GetRouteOrder(
    NotificationOptions options,
    TerminalCapabilities terminalCapabilities)
  {
    NotificationMode mode = options.Mode;
    List<NotificationRoute> routes = new();

    switch (mode)
    {
      case NotificationMode.AutoDesktopFirst:
        routes.Add(NotificationRoute.Desktop);
        if (!options.DisableDesktopFallback)
        {
          AddTerminalRoutes(routes, terminalCapabilities, options.TerminalPreference);
        }
        break;
      case NotificationMode.AutoTerminalFirst:
        AddTerminalRoutes(routes, terminalCapabilities, options.TerminalPreference);
        if (!options.DisableDesktopFallback)
        {
          routes.Add(NotificationRoute.Desktop);
        }
        break;
      case NotificationMode.DesktopOnly:
        routes.Add(NotificationRoute.Desktop);
        break;
      case NotificationMode.TerminalOnly:
        AddTerminalRoutes(routes, terminalCapabilities, options.TerminalPreference);
        break;
      case NotificationMode.Off:
        break;
      default:
        routes.Add(NotificationRoute.Desktop);
        if (!options.DisableDesktopFallback)
        {
          AddTerminalRoutes(routes, terminalCapabilities, options.TerminalPreference);
        }
        break;
    }

    return routes;
  }

  private static void AddTerminalRoutes(
    List<NotificationRoute> routes,
    TerminalCapabilities terminalCapabilities,
    TerminalNotificationPreference terminalPreference)
  {
    if (terminalPreference == TerminalNotificationPreference.Osc9Only)
    {
      if (terminalCapabilities.SupportsOsc9)
      {
        routes.Add(NotificationRoute.Osc9);
      }
      return;
    }

    if (terminalPreference == TerminalNotificationPreference.BelOnly)
    {
      if (terminalCapabilities.SupportsBel)
      {
        routes.Add(NotificationRoute.Bel);
      }
      return;
    }

    if (terminalCapabilities.SupportsOsc777)
    {
      routes.Add(NotificationRoute.Osc777);
    }

    if (terminalCapabilities.SupportsOsc9)
    {
      routes.Add(NotificationRoute.Osc9);
    }

    if (terminalCapabilities.SupportsBel)
    {
      routes.Add(NotificationRoute.Bel);
    }
  }

  private static string Normalize(string? value)
  {
    if (string.IsNullOrWhiteSpace(value))
    {
      return string.Empty;
    }

    char[] normalized = value
      .Trim()
      .Where(c => c != ' ' && c != '-' && c != '_' && c != '.')
      .Select(char.ToLowerInvariant)
      .ToArray();

    return new string(normalized);
  }
}
