string appName = "AppName \\ 'Hello' \"World!\"";
string title = "Title \\ 'Hello' \"World!\"";
string message = "Message \\ 'Hello' \"World!\"";
await Notifs.Notification.NotifyAsync(appName, title, message);
Console.WriteLine($"App Name: {appName}");
Console.WriteLine($"Title: {title}");
Console.WriteLine($"Message: {message}");