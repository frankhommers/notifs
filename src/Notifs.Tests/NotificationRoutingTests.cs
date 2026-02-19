using Xunit;

namespace Notifs.Tests;

public class NotificationRoutingTests
{
  [Fact]
  public void DetectTerminalCapabilities_SupportsOsc9_ForGhostty()
  {
    TerminalCapabilities capabilities = NotificationRouting.DetectTerminalCapabilities(
      key => key == "TERM_PROGRAM" ? "Ghostty" : null,
      isOutputRedirected: false);

    Assert.True(capabilities.SupportsOsc9);
    Assert.True(capabilities.SupportsOsc777);
    Assert.True(capabilities.SupportsBel);
  }

  [Fact]
  public void DetectTerminalCapabilities_DisablesOsc9_ForWindowsTerminal()
  {
    TerminalCapabilities capabilities = NotificationRouting.DetectTerminalCapabilities(
      key => key == "WT_SESSION" ? "1" : null,
      isOutputRedirected: false);

    Assert.False(capabilities.SupportsOsc9);
    Assert.False(capabilities.SupportsOsc777);
    Assert.True(capabilities.SupportsBel);
  }

  [Fact]
  public void DetectTerminalCapabilities_DisablesTerminal_WhenOutputIsRedirected()
  {
    TerminalCapabilities capabilities = NotificationRouting.DetectTerminalCapabilities(
      _ => "Ghostty",
      isOutputRedirected: true);

    Assert.False(capabilities.SupportsOsc9);
    Assert.False(capabilities.SupportsOsc777);
    Assert.False(capabilities.SupportsBel);
  }

  [Fact]
  public void RouteOrder_AutoDesktopFirst_PrefersDesktop()
  {
    TerminalCapabilities capabilities = new(supportsOsc9: true, supportsOsc777: true, supportsBel: true);
    NotificationOptions options = new()
    {
      Mode = NotificationMode.AutoDesktopFirst
    };

    IReadOnlyList<NotificationRoute> routes = NotificationRouting.GetRouteOrder(
      options,
      capabilities);

    Assert.Equal(
      new[] { NotificationRoute.Desktop, NotificationRoute.Osc777, NotificationRoute.Osc9, NotificationRoute.Bel },
      routes);
  }

  [Fact]
  public void RouteOrder_AutoTerminalFirst_PrefersTerminal()
  {
    TerminalCapabilities capabilities = new(supportsOsc9: true, supportsOsc777: true, supportsBel: true);
    NotificationOptions options = new()
    {
      Mode = NotificationMode.AutoTerminalFirst
    };

    IReadOnlyList<NotificationRoute> routes = NotificationRouting.GetRouteOrder(
      options,
      capabilities);

    Assert.Equal(
      new[] { NotificationRoute.Osc777, NotificationRoute.Osc9, NotificationRoute.Bel, NotificationRoute.Desktop },
      routes);
  }

  [Fact]
  public void RouteOrder_DesktopOnly_UsesDesktopOnly()
  {
    TerminalCapabilities capabilities = new(supportsOsc9: true, supportsOsc777: true, supportsBel: true);
    NotificationOptions options = new()
    {
      Mode = NotificationMode.DesktopOnly
    };

    IReadOnlyList<NotificationRoute> routes = NotificationRouting.GetRouteOrder(
      options,
      capabilities);

    Assert.Equal(new[] { NotificationRoute.Desktop }, routes);
  }

  [Fact]
  public void RouteOrder_TerminalOnly_UsesTerminalOnly()
  {
    TerminalCapabilities capabilities = new(supportsOsc9: false, supportsOsc777: false, supportsBel: true);
    NotificationOptions options = new()
    {
      Mode = NotificationMode.TerminalOnly
    };

    IReadOnlyList<NotificationRoute> routes = NotificationRouting.GetRouteOrder(
      options,
      capabilities);

    Assert.Equal(new[] { NotificationRoute.Bel }, routes);
  }

  [Fact]
  public void RouteOrder_Off_ReturnsNoRoutes()
  {
    TerminalCapabilities capabilities = new(supportsOsc9: true, supportsOsc777: true, supportsBel: true);
    NotificationOptions options = new()
    {
      Mode = NotificationMode.Off
    };

    IReadOnlyList<NotificationRoute> routes = NotificationRouting.GetRouteOrder(
      options,
      capabilities);

    Assert.Empty(routes);
  }

  [Fact]
  public void RouteOrder_AutoDesktopFirst_DisableDesktopFallback_OnlyUsesDesktop()
  {
    TerminalCapabilities capabilities = new(supportsOsc9: true, supportsOsc777: true, supportsBel: true);
    NotificationOptions options = new()
    {
      Mode = NotificationMode.AutoDesktopFirst,
      DisableDesktopFallback = true
    };

    IReadOnlyList<NotificationRoute> routes = NotificationRouting.GetRouteOrder(options, capabilities);

    Assert.Equal(new[] { NotificationRoute.Desktop }, routes);
  }

  [Fact]
  public void RouteOrder_AutoTerminalFirst_DisableDesktopFallback_OnlyUsesTerminal()
  {
    TerminalCapabilities capabilities = new(supportsOsc9: true, supportsOsc777: true, supportsBel: true);
    NotificationOptions options = new()
    {
      Mode = NotificationMode.AutoTerminalFirst,
      DisableDesktopFallback = true
    };

    IReadOnlyList<NotificationRoute> routes = NotificationRouting.GetRouteOrder(options, capabilities);

    Assert.Equal(new[] { NotificationRoute.Osc777, NotificationRoute.Osc9, NotificationRoute.Bel }, routes);
  }

  [Fact]
  public void RouteOrder_TerminalOnly_Osc9Only_UsesOnlyOsc9()
  {
    TerminalCapabilities capabilities = new(supportsOsc9: true, supportsOsc777: true, supportsBel: true);
    NotificationOptions options = new()
    {
      Mode = NotificationMode.TerminalOnly,
      TerminalPreference = TerminalNotificationPreference.Osc9Only
    };

    IReadOnlyList<NotificationRoute> routes = NotificationRouting.GetRouteOrder(options, capabilities);

    Assert.Equal(new[] { NotificationRoute.Osc9 }, routes);
  }

  [Fact]
  public void RouteOrder_TerminalOnly_BelOnly_UsesOnlyBel()
  {
    TerminalCapabilities capabilities = new(supportsOsc9: true, supportsOsc777: true, supportsBel: true);
    NotificationOptions options = new()
    {
      Mode = NotificationMode.TerminalOnly,
      TerminalPreference = TerminalNotificationPreference.BelOnly
    };

    IReadOnlyList<NotificationRoute> routes = NotificationRouting.GetRouteOrder(options, capabilities);

    Assert.Equal(new[] { NotificationRoute.Bel }, routes);
  }
}
