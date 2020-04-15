// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace Microsoft.Extensions.Logging
{
    public static partial class ConsoleLoggerExtensions
    {
        public static Microsoft.Extensions.Logging.ILoggingBuilder AddConsole(this Microsoft.Extensions.Logging.ILoggingBuilder builder) { throw null; }
        public static Microsoft.Extensions.Logging.ILoggingBuilder AddConsole(this Microsoft.Extensions.Logging.ILoggingBuilder builder, System.Action<Microsoft.Extensions.Logging.Console.ConsoleLoggerOptions> configure) { throw null; }
    }
}
namespace Microsoft.Extensions.Logging.Console
{
    public enum ConsoleLoggerFormat
    {
        Default = 0,
        Systemd = 1,
    }
    public partial class ConsoleLoggerOptions
    {
        public ConsoleLoggerOptions() { }
        public bool DisableColors { get { throw null; } set { } }
        public Microsoft.Extensions.Logging.Console.ConsoleLoggerFormat Format { get { throw null; } set { } }
        public string Formatter { get { throw null; } set { } }
        public bool IncludeScopes { get { throw null; } set { } }
        public Microsoft.Extensions.Logging.LogLevel LogToStandardErrorThreshold { get { throw null; } set { } }
        public string TimestampFormat { get { throw null; } set { } }
        public bool UseUtcTimestamp { get { throw null; } set { } }
    }
    [Microsoft.Extensions.Logging.ProviderAliasAttribute("Console")]
    public partial class ConsoleLoggerProvider : Microsoft.Extensions.Logging.ILoggerProvider, Microsoft.Extensions.Logging.ISupportExternalScope, System.IDisposable
    {
        public ConsoleLoggerProvider(Microsoft.Extensions.Options.IOptionsMonitor<Microsoft.Extensions.Logging.Console.ConsoleLoggerOptions> options, System.Collections.Generic.IEnumerable<Microsoft.Extensions.Logging.Console.ILogFormatter> formatters) { }
        public Microsoft.Extensions.Logging.ILogger CreateLogger(string name) { throw null; }
        public void Dispose() { }
        public void SetScopeProvider(Microsoft.Extensions.Logging.IExternalScopeProvider scopeProvider) { }
    }
    public partial class DefaultLogFormatter : Microsoft.Extensions.Logging.Console.ILogFormatter
    {
        public DefaultLogFormatter() { }
        public string Name { get { throw null; } }
        public Microsoft.Extensions.Logging.Console.LogMessageEntry Format(Microsoft.Extensions.Logging.LogLevel logLevel, string logName, int eventId, string message, System.Exception exception) { throw null; }
    }
    public partial class DefaultLogFormatterOptions
    {
        public DefaultLogFormatterOptions() { }
    }
    public partial interface ILogFormatter
    {
        string Name { get; }
        Microsoft.Extensions.Logging.Console.LogMessageEntry Format(Microsoft.Extensions.Logging.LogLevel logLevel, string logName, int eventId, string message, System.Exception exception);
    }
    public readonly partial struct LogMessageEntry
    {
        public readonly System.ConsoleColor? LevelBackground;
        public readonly System.ConsoleColor? LevelForeground;
        public readonly string LevelString;
        public readonly bool LogAsError;
        public readonly string Message;
        public readonly System.ConsoleColor? MessageColor;
        public readonly string TimeStamp;
        public LogMessageEntry(string message, string timeStamp = null, string levelString = null, System.ConsoleColor? levelBackground = default(System.ConsoleColor?), System.ConsoleColor? levelForeground = default(System.ConsoleColor?), System.ConsoleColor? messageColor = default(System.ConsoleColor?), bool logAsError = false) { throw null; }
    }
    public partial class SystemdLogFormatter : Microsoft.Extensions.Logging.Console.ILogFormatter
    {
        public SystemdLogFormatter() { }
        public string Name { get { throw null; } }
        public Microsoft.Extensions.Logging.Console.LogMessageEntry Format(Microsoft.Extensions.Logging.LogLevel logLevel, string logName, int eventId, string message, System.Exception exception) { throw null; }
    }
    public partial class SystemdLogFormatterOptions
    {
        public SystemdLogFormatterOptions() { }
    }
}
