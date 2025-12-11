using System;
using Vintagestory.API.Common;

namespace GloomeClasses.src.Utils;

/// <summary>
/// centralized logging utility for GloomeClasses.
/// consistent, tagged log messages following Vintage Story Logger API best practices.
///
/// usage:
///   Log.Debug(api, "MyClass", "Detailed debug info");
///   Log.Notification(api, "MyClass", "User-visible message");
///   Log.Error(api, "MyClass", exception);
/// </summary>
public static class Log
{
  /// <summary>
  /// logs a debug message (goes to client-debug.log only).
  /// use for detailed development information not visible to users.
  /// </summary>
  /// <param name="api">Core API instance</param>
  /// <param name="tag">Component name (e.g., "BlockMetalBarrel", "MeshCache")</param>
  /// <param name="message">Message text</param>
  public static void Debug(ICoreAPI api, string tag, string message)
  {
    api.Logger.Debug($"[GloomeClasses:{tag}] {message}");
  }

  /// <summary>
  /// logs a debug message with format arguments (goes to client-debug.log only).
  /// use for detailed development information not visible to users.
  /// </summary>
  /// <param name="api">Core API instance</param>
  /// <param name="tag">Component name</param>
  /// <param name="format">Format string (e.g., "Found {0} items")</param>
  /// <param name="args">Format arguments</param>
  public static void Debug(ICoreAPI api, string tag, string format, params object[] args)
  {
    api.Logger.Debug($"[GloomeClasses:{tag}] {format}", args);
  }

  /// <summary>
  /// logs a notification message (goes to client-main.log and may be visible in-game).
  /// use for important user-facing information about mod state changes.
  /// </summary>
  /// <param name="api">Core API instance</param>
  /// <param name="tag">Component name</param>
  /// <param name="message">Message text</param>
  public static void Notification(ICoreAPI api, string tag, string message)
  {
    api.Logger.Notification($"[GloomeClasses:{tag}] {message}");
  }

  /// <summary>
  /// logs a notification message with format arguments (goes to client-main.log).
  /// use for important user-facing information about mod state changes.
  /// </summary>
  /// <param name="api">Core API instance</param>
  /// <param name="tag">Component name</param>
  /// <param name="format">Format string</param>
  /// <param name="args">Format arguments</param>
  public static void Notification(ICoreAPI api, string tag, string format, params object[] args)
  {
    api.Logger.Notification($"[GloomeClasses:{tag}] {format}", args);
  }

  /// <summary>
  /// logs a warning message.
  /// use for recoverable problems that should be investigated.
  /// </summary>
  /// <param name="api">Core API instance</param>
  /// <param name="tag">Component name</param>
  /// <param name="message">Warning text</param>
  public static void Warning(ICoreAPI api, string tag, string message)
  {
    api.Logger.Warning($"[GloomeClasses:{tag}] {message}");
  }

  /// <summary>
  /// logs a warning message with format arguments.
  /// use for recoverable problems that should be investigated.
  /// </summary>
  /// <param name="api">Core API instance</param>
  /// <param name="tag">Component name</param>
  /// <param name="format">Format string</param>
  /// <param name="args">Format arguments</param>
  public static void Warning(ICoreAPI api, string tag, string format, params object[] args)
  {
    api.Logger.Warning($"[GloomeClasses:{tag}] {format}", args);
  }

  /// <summary>
  /// logs a warning with exception details.
  /// automatically includes stack trace from the exception.
  /// </summary>
  /// <param name="api">Core API instance</param>
  /// <param name="tag">Component name</param>
  /// <param name="message">Context message</param>
  /// <param name="exception">The exception to log</param>
  public static void Warning(ICoreAPI api, string tag, string message, Exception exception)
  {
    api.Logger.Warning($"[GloomeClasses:{tag}] {message}: {exception.Message}");
    api.Logger.Warning(exception);
  }

  /// <summary>
  /// logs an error message.
  /// use for serious problems that prevent functionality but don't crash the mod.
  /// </summary>
  /// <param name="api">Core API instance</param>
  /// <param name="tag">Component name</param>
  /// <param name="message">Error text</param>
  public static void Error(ICoreAPI api, string tag, string message)
  {
    api.Logger.Error($"[GloomeClasses:{tag}] {message}");
  }

  /// <summary>
  /// logs an error message with format arguments.
  /// use for serious problems that prevent functionality but don't crash the mod.
  /// </summary>
  /// <param name="api">Core API instance</param>
  /// <param name="tag">Component name</param>
  /// <param name="format">Format string</param>
  /// <param name="args">Format arguments</param>
  public static void Error(ICoreAPI api, string tag, string format, params object[] args)
  {
    api.Logger.Error($"[GloomeClasses:{tag}] {format}", args);
  }

  /// <summary>
  /// logs an error with exception details.
  /// automatically includes stack trace from the exception.
  /// </summary>
  /// <param name="api">Core API instance</param>
  /// <param name="tag">Component name</param>
  /// <param name="message">Context message describing what was being attempted</param>
  /// <param name="exception">The exception to log</param>
  public static void Error(ICoreAPI api, string tag, string message, Exception exception)
  {
    api.Logger.Error($"[GloomeClasses:{tag}] {message}: {exception.Message}");
    api.Logger.Error(exception);
  }

  /// <summary>
  /// logs verbose debug information (even more detailed than Debug).
  /// use sparingly for very detailed debugging of complex operations.
  /// </summary>
  /// <param name="api">Core API instance</param>
  /// <param name="tag">Component name</param>
  /// <param name="message">Detailed message</param>
  public static void VerboseDebug(ICoreAPI api, string tag, string message)
  {
    api.Logger.VerboseDebug($"[GloomeClasses:{tag}] {message}");
  }

  /// <summary>
  /// logs verbose debug information with format arguments.
  /// use sparingly for very detailed debugging of complex operations.
  /// </summary>
  /// <param name="api">Core API instance</param>
  /// <param name="tag">Component name</param>
  /// <param name="format">Format string</param>
  /// <param name="args">Format arguments</param>
  public static void VerboseDebug(ICoreAPI api, string tag, string format, params object[] args)
  {
    api.Logger.VerboseDebug($"[GloomeClasses:{tag}] {format}", args);
  }
}
