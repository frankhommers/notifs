using System.Runtime.InteropServices;
using CliWrap;

namespace Notifs;

public static class Notification
{
  public static async Task NotifyAsync(string applicationName, string title, string message)
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

    if (notificationCommand != null)
    {
      await notificationCommand.WithValidation(CommandResultValidation.None).ExecuteAsync();
    }
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