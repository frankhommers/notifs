using System.Runtime.InteropServices;
using CliWrap;

namespace Notifs;

public enum NotificationMode
{
  AutoDesktopFirst,
  AutoTerminalFirst,
  DesktopOnly,
  TerminalOnly,
  Off
}

public enum TerminalNotificationPreference
{
  Auto,
  Osc9Only,
  BelOnly
}

public sealed class NotificationOptions
{
  public NotificationMode Mode { get; set; } = NotificationMode.AutoDesktopFirst;
  public TerminalNotificationPreference TerminalPreference { get; set; } = TerminalNotificationPreference.Auto;
  public bool DisableDesktopFallback { get; set; }
  public bool ThrowOnFailure { get; set; }
  public bool EnableDebugOutput { get; set; }
}

public static class Notification
{
  public static async Task NotifyAsync(string applicationName, string title, string message)
  {
    await NotifyAsync(applicationName, title, message, options: null);
  }

  public static async Task NotifyAsync(
    string applicationName,
    string title,
    string message,
    NotificationOptions? options)
  {
    NotificationOptions resolvedOptions = options ?? new NotificationOptions();
    if (resolvedOptions.Mode == NotificationMode.Off)
    {
      return;
    }

    TerminalCapabilities terminalCapabilities = NotificationRouting.DetectTerminalCapabilities(
      Environment.GetEnvironmentVariable,
      Console.IsOutputRedirected);

    IReadOnlyList<NotificationRoute> routeOrder = NotificationRouting.GetRouteOrder(
      resolvedOptions,
      terminalCapabilities);

    if (resolvedOptions.EnableDebugOutput)
    {
      Console.Error.WriteLine(
        $"[notifs] mode={resolvedOptions.Mode}, terminalPreference={resolvedOptions.TerminalPreference}, " +
        $"supportsOsc9={terminalCapabilities.SupportsOsc9}, supportsOsc777={terminalCapabilities.SupportsOsc777}, supportsBel={terminalCapabilities.SupportsBel}, " +
        $"isOutputRedirected={Console.IsOutputRedirected}");
    }

    foreach (NotificationRoute route in routeOrder)
    {
      if (resolvedOptions.EnableDebugOutput)
      {
        Console.Error.WriteLine($"[notifs] trying route={route}");
      }

      bool delivered = route switch
      {
        NotificationRoute.Desktop => await TrySendDesktopNotificationAsync(applicationName, title, message),
        NotificationRoute.Osc9 => TrySendOsc9Notification(title, message),
        NotificationRoute.Osc777 => TrySendOsc777Notification(title, message),
        NotificationRoute.Bel => TrySendTerminalBell(),
        _ => false
      };

      if (delivered)
      {
        if (resolvedOptions.EnableDebugOutput)
        {
          Console.Error.WriteLine($"[notifs] delivered route={route}");
        }
        return;
      }

      if (resolvedOptions.EnableDebugOutput)
      {
        Console.Error.WriteLine($"[notifs] failed route={route}");
      }
    }

    if (resolvedOptions.ThrowOnFailure)
    {
      throw new InvalidOperationException("No notification backend could deliver the message.");
    }
  }

  private static async Task<bool> TrySendDesktopNotificationAsync(string applicationName, string title, string message)
  {
    Command? notificationCommand = null;
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
    {
      notificationCommand = CreateLinuxNotificationCommand(applicationName, title, message);
    }
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
      notificationCommand = CreateWindowsNotificationCommand(applicationName, title, message);
    }
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
    {
      notificationCommand = CreateMacOsNotificationCommand(applicationName, title, message);
    }

    if (notificationCommand == null)
    {
      return false;
    }

    try
    {
      CommandResult result = await notificationCommand.WithValidation(CommandResultValidation.None).ExecuteAsync();
      return result.ExitCode == 0;
    }
    catch
    {
      return false;
    }
  }

  private static bool TrySendOsc9Notification(string title, string message)
  {
    try
    {
      string payload = SanitizeTerminalText($"{title}: {message}");
      Console.Out.Write($"\x1b]9;{payload}\x07");
      Console.Out.Flush();
      return true;
    }
    catch
    {
      return false;
    }
  }

  private static bool TrySendTerminalBell()
  {
    try
    {
      Console.Out.Write('\x07');
      Console.Out.Flush();
      return true;
    }
    catch
    {
      return false;
    }
  }

  private static bool TrySendOsc777Notification(string title, string message)
  {
    try
    {
      string safeTitle = SanitizeTerminalText(title);
      string safeMessage = SanitizeTerminalText(message);
      Console.Out.Write($"\x1b]777;notify;{safeTitle};{safeMessage}\x07");
      Console.Out.Flush();
      return true;
    }
    catch
    {
      return false;
    }
  }

  private static string SanitizeTerminalText(string value)
  {
    return value
      .Replace("\u001b", " ")
      .Replace("\u0007", " ")
      .Replace("\r", " ")
      .Replace("\n", " ");
  }

  private static Command CreateMacOsNotificationCommand(string applicationName, string title, string message)
  {
    return Cli.Wrap("osascript")
      .WithArguments(args => args
                       .Add("-e").Add("on run argv")
                       .Add("-e").Add("display notification (item 2 of argv) with title (item 1 of argv)")
                       .Add("-e").Add("end run")
                       .Add("--")
                       .Add(title)
                       .Add(message));
  }

  private static Command CreateWindowsNotificationCommand(string applicationName, string title, string message)
  {
    string template = $$"""
                        <toast activationType="protocol" duration="Short">
                            <visual>
                                <binding template="ToastGeneric">
                                    <!-- <image placement="appLogoOverride" hint-crop="circle" src="icon-source-here" /> -->
                                    <text><![CDATA[{{title}}]]></text>
                                    <text><![CDATA[{{message}}]]></text>
                                </binding>
                            </visual>
                          <audio silent="true" />
                          <!--
                            <actions>
                                <action activationType="{.Type}" content="{.Label}" arguments="{.Arguments}" />
                            </actions>
                          -->
                        </toast>
                        """;

    template = template
      .Replace(@"""", @"\""\""");

    applicationName = applicationName
      .Replace(@"""", @"\""\""");

    string powerShellCommandContent = $"""
                                       [Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime] | Out-Null
                                       [Windows.UI.Notifications.ToastNotification, Windows.UI.Notifications, ContentType = WindowsRuntime] | Out-Null
                                       [Windows.Data.Xml.Dom.XmlDocument, Windows.Data.Xml.Dom.XmlDocument, ContentType = WindowsRuntime] | Out-Null
                                       $template = \"{template}\"
                                       $xml = New-Object Windows.Data.Xml.Dom.XmlDocument
                                       $xml.LoadXml($template)
                                       $toast = New-Object Windows.UI.Notifications.ToastNotification $xml
                                       $toast.Tag = \"{Guid.NewGuid():N}\"
                                       $toast.Group = \"App:{applicationName.GetHashCode()}\"
                                       $toast.ExpirationTime = [DateTimeOffset]::Now.AddMinutes(5)
                                       $notifier = [Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier(\"{applicationName}\")
                                       $notifier.Show($toast);
                                       """;

    return Cli.Wrap("powershell")
      .WithArguments($"-NoLogo -NoProfile -ExecutionPolicy ByPass -Command \"{powerShellCommandContent}\"");
  }

  private static Command CreateWindowsAlternativeNotificationCommand(
    string applicationName,
    string title,
    string message)
  {
    message = message
      .Replace(@"""", @"\""\""");
    title = title
      .Replace(@"""", @"\""\""");

    string command = $"""
                      [System.Reflection.Assembly]::LoadWithPartialName(\"System.Windows.Forms\");
                      [System.Reflection.Assembly]::LoadWithPartialName(\"System.Drawing\");
                      $icon = New-Object System.Windows.Forms.NotifyIcon;
                      $icon.Icon = [System.Drawing.SystemIcons]::Information;
                      $icon.BalloonTipTitle = \"{title}\";
                      $icon.BalloonTipText = \"{message}\";
                      $icon.Visible = $true;
                      $icon.ShowBalloonTip(5000);
                      """;
    return Cli.Wrap("powershell")
      .WithArguments($"-Command \"{command}\"");
  }

  private static Command CreateLinuxNotificationCommand(string applicationName, string title, string message)
  {
    message = message
      .Replace(@"\", @"\\")
      .Replace(@"""", @"\""");
    title = title
      .Replace(@"\", @"\\")
      .Replace(@"""", @"\""");
    return Cli.Wrap("notify-send")
      .WithArguments($"\"{title}\" \"{message}\"");
  }
}
