// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace Microsoft.Extensions.Logging
{
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
    public static partial class ConsoleLoggerExtensions
    {
        [System.ObsoleteAttribute("TODO")]
        public static Microsoft.Extensions.Logging.ILoggerFactory AddConsole(this Microsoft.Extensions.Logging.ILoggerFactory factory) { throw null; }
        [System.ObsoleteAttribute("TODO")]
        public static Microsoft.Extensions.Logging.ILoggerFactory AddConsole(this Microsoft.Extensions.Logging.ILoggerFactory factory, Microsoft.Extensions.Configuration.IConfiguration configuration) { throw null; }
        [System.ObsoleteAttribute("TODO")]
        public static Microsoft.Extensions.Logging.ILoggerFactory AddConsole(this Microsoft.Extensions.Logging.ILoggerFactory factory, Microsoft.Extensions.Logging.Console.IConsoleLoggerSettings settings) { throw null; }
        [System.ObsoleteAttribute("TODO")]
        public static Microsoft.Extensions.Logging.ILoggerFactory AddConsole(this Microsoft.Extensions.Logging.ILoggerFactory factory, Microsoft.Extensions.Logging.LogLevel minLevel) { throw null; }
        [System.ObsoleteAttribute("TODO")]
        public static Microsoft.Extensions.Logging.ILoggerFactory AddConsole(this Microsoft.Extensions.Logging.ILoggerFactory factory, Microsoft.Extensions.Logging.LogLevel minLevel, bool includeScopes) { throw null; }
        [System.ObsoleteAttribute("TODO")]
        public static Microsoft.Extensions.Logging.ILoggerFactory AddConsole(this Microsoft.Extensions.Logging.ILoggerFactory factory, bool includeScopes) { throw null; }
        [System.ObsoleteAttribute("TODO")]
        public static Microsoft.Extensions.Logging.ILoggerFactory AddConsole(this Microsoft.Extensions.Logging.ILoggerFactory factory, System.Func<string, Microsoft.Extensions.Logging.LogLevel, bool> filter) { throw null; }
        [System.ObsoleteAttribute("TODO")]
        public static Microsoft.Extensions.Logging.ILoggerFactory AddConsole(this Microsoft.Extensions.Logging.ILoggerFactory factory, System.Func<string, Microsoft.Extensions.Logging.LogLevel, bool> filter, bool includeScopes) { throw null; }
        public static Microsoft.Extensions.Logging.ILoggingBuilder AddConsole(this Microsoft.Extensions.Logging.ILoggingBuilder builder) { throw null; }
        public static Microsoft.Extensions.Logging.ILoggingBuilder AddConsole(this Microsoft.Extensions.Logging.ILoggingBuilder builder, System.Action<Microsoft.Extensions.Logging.Console.ConsoleLoggerOptions> configure) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute("Binding TOptions to configuration values may require generating dynamic code at runtime.")]
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("TOptions's dependent types may have their members trimmed. Ensure all required members are preserved.")]
        public static Microsoft.Extensions.Logging.ILoggingBuilder AddConsoleFormatter<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] TFormatter, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)] TOptions>(this Microsoft.Extensions.Logging.ILoggingBuilder builder) where TFormatter : Microsoft.Extensions.Logging.Console.ConsoleFormatter where TOptions : Microsoft.Extensions.Logging.Console.ConsoleFormatterOptions { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute("Binding TOptions to configuration values may require generating dynamic code at runtime.")]
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("TOptions's dependent types may have their members trimmed. Ensure all required members are preserved.")]
        public static Microsoft.Extensions.Logging.ILoggingBuilder AddConsoleFormatter<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] TFormatter, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)] TOptions>(this Microsoft.Extensions.Logging.ILoggingBuilder builder, System.Action<TOptions> configure) where TFormatter : Microsoft.Extensions.Logging.Console.ConsoleFormatter where TOptions : Microsoft.Extensions.Logging.Console.ConsoleFormatterOptions { throw null; }
        public static Microsoft.Extensions.Logging.ILoggingBuilder AddJsonConsole(this Microsoft.Extensions.Logging.ILoggingBuilder builder) { throw null; }
        public static Microsoft.Extensions.Logging.ILoggingBuilder AddJsonConsole(this Microsoft.Extensions.Logging.ILoggingBuilder builder, System.Action<Microsoft.Extensions.Logging.Console.JsonConsoleFormatterOptions> configure) { throw null; }
        public static Microsoft.Extensions.Logging.ILoggingBuilder AddSimpleConsole(this Microsoft.Extensions.Logging.ILoggingBuilder builder) { throw null; }
        public static Microsoft.Extensions.Logging.ILoggingBuilder AddSimpleConsole(this Microsoft.Extensions.Logging.ILoggingBuilder builder, System.Action<Microsoft.Extensions.Logging.Console.SimpleConsoleFormatterOptions> configure) { throw null; }
        public static Microsoft.Extensions.Logging.ILoggingBuilder AddSystemdConsole(this Microsoft.Extensions.Logging.ILoggingBuilder builder) { throw null; }
        public static Microsoft.Extensions.Logging.ILoggingBuilder AddSystemdConsole(this Microsoft.Extensions.Logging.ILoggingBuilder builder, System.Action<Microsoft.Extensions.Logging.Console.ConsoleFormatterOptions> configure) { throw null; }
    }
}
namespace Microsoft.Extensions.Logging.Console
{
    [System.ObsoleteAttribute("TODO")]
    public partial class ConfigurationConsoleLoggerSettings : Microsoft.Extensions.Logging.Console.IConsoleLoggerSettings
    {
        public ConfigurationConsoleLoggerSettings(Microsoft.Extensions.Configuration.IConfiguration configuration) { }
        public Microsoft.Extensions.Primitives.IChangeToken ChangeToken { get { throw null; } }
        public bool IncludeScopes { get { throw null; } }
        public Microsoft.Extensions.Logging.Console.IConsoleLoggerSettings Reload() { throw null; }
        public bool TryGetSwitch(string name, out Microsoft.Extensions.Logging.LogLevel level) { throw null; }
    }
    public abstract partial class ConsoleFormatter
    {
        protected ConsoleFormatter(string name) { }
        public string Name { get { throw null; } }
        public abstract void Write<TState>(in Microsoft.Extensions.Logging.Abstractions.LogEntry<TState> logEntry, Microsoft.Extensions.Logging.IExternalScopeProvider? scopeProvider, System.IO.TextWriter textWriter);
    }
    public static partial class ConsoleFormatterNames
    {
        public const string Json = "json";
        public const string Simple = "simple";
        public const string Systemd = "systemd";
    }
    public partial class ConsoleFormatterOptions
    {
        public ConsoleFormatterOptions() { }
        public bool IncludeScopes { get { throw null; } set { } }
        [System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("DateTimeFormat")]
        public string? TimestampFormat { get { throw null; } set { } }
        public bool UseUtcTimestamp { get { throw null; } set { } }
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
    public sealed partial class ConsoleLogger : Microsoft.Extensions.Logging.ILogger
    {
        internal ConsoleLogger() { }
        public System.IDisposable BeginScope<TState>(TState state) where TState : notnull { throw null; }
        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) { throw null; }
        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state, System.Exception? exception, System.Func<TState, System.Exception?, string> formatter) { }
    }
    [System.ObsoleteAttribute("ConsoleLoggerFormat has been deprecated.")]
    public enum ConsoleLoggerFormat
    {
        Default = 0,
        Systemd = 1,
    }
    public partial class ConsoleLoggerOptions
    {
        public ConsoleLoggerOptions() { }
        [System.ObsoleteAttribute("ConsoleLoggerOptions.DisableColors has been deprecated. Use SimpleConsoleFormatterOptions.ColorBehavior instead.")]
        public bool DisableColors { get { throw null; } set { } }
        [System.ObsoleteAttribute("ConsoleLoggerOptions.Format has been deprecated. Use ConsoleLoggerOptions.FormatterName instead.")]
        public Microsoft.Extensions.Logging.Console.ConsoleLoggerFormat Format { get { throw null; } set { } }
        public string? FormatterName { get { throw null; } set { } }
        [System.ObsoleteAttribute("ConsoleLoggerOptions.IncludeScopes has been deprecated. Use ConsoleFormatterOptions.IncludeScopes instead.")]
        public bool IncludeScopes { get { throw null; } set { } }
        public Microsoft.Extensions.Logging.LogLevel LogToStandardErrorThreshold { get { throw null; } set { } }
        public int MaxQueueLength { get { throw null; } set { } }
        public Microsoft.Extensions.Logging.Console.ConsoleLoggerQueueFullMode QueueFullMode { get { throw null; } set { } }
        [System.ObsoleteAttribute("ConsoleLoggerOptions.TimestampFormat has been deprecated. Use ConsoleFormatterOptions.TimestampFormat instead.")]
        public string? TimestampFormat { get { throw null; } set { } }
        [System.ObsoleteAttribute("ConsoleLoggerOptions.UseUtcTimestamp has been deprecated. Use ConsoleFormatterOptions.UseUtcTimestamp instead.")]
        public bool UseUtcTimestamp { get { throw null; } set { } }
    }
    [Microsoft.Extensions.Logging.ProviderAliasAttribute("Console")]
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
    public partial class ConsoleLoggerProvider : Microsoft.Extensions.Logging.ILoggerProvider, Microsoft.Extensions.Logging.ISupportExternalScope, System.IDisposable
    {
        [System.ObsoleteAttribute("TODO")]
        public ConsoleLoggerProvider(Microsoft.Extensions.Logging.Console.IConsoleLoggerSettings settings) { }
        public ConsoleLoggerProvider(Microsoft.Extensions.Options.IOptionsMonitor<Microsoft.Extensions.Logging.Console.ConsoleLoggerOptions> options) { }
        public ConsoleLoggerProvider(Microsoft.Extensions.Options.IOptionsMonitor<Microsoft.Extensions.Logging.Console.ConsoleLoggerOptions> options, System.Collections.Generic.IEnumerable<Microsoft.Extensions.Logging.Console.ConsoleFormatter>? formatters) { }
        [System.ObsoleteAttribute("TODO")]
        public ConsoleLoggerProvider(System.Func<string, Microsoft.Extensions.Logging.LogLevel, bool> filter, bool includeScopes) { }
        [System.ObsoleteAttribute("TODO")]
        public ConsoleLoggerProvider(System.Func<string, Microsoft.Extensions.Logging.LogLevel, bool> filter, bool includeScopes, bool disableColors) { }
        public Microsoft.Extensions.Logging.ILogger CreateLogger(string name) { throw null; }
        public void Dispose() { }
        public void SetScopeProvider(Microsoft.Extensions.Logging.IExternalScopeProvider scopeProvider) { }
    }
    public enum ConsoleLoggerQueueFullMode
    {
        Wait = 0,
        DropWrite = 1,
    }
    [System.ObsoleteAttribute("TODO")]
    public partial class ConsoleLoggerSettings : Microsoft.Extensions.Logging.Console.IConsoleLoggerSettings
    {
        public ConsoleLoggerSettings() { }
        public Microsoft.Extensions.Primitives.IChangeToken ChangeToken { get { throw null; } set { } }
        public bool DisableColors { get { throw null; } set { } }
        public bool IncludeScopes { get { throw null; } set { } }
        public System.Collections.Generic.IDictionary<string, Microsoft.Extensions.Logging.LogLevel> Switches { get { throw null; } set { } }
        public Microsoft.Extensions.Logging.Console.IConsoleLoggerSettings Reload() { throw null; }
        public bool TryGetSwitch(string name, out Microsoft.Extensions.Logging.LogLevel level) { throw null; }
    }
    [System.ObsoleteAttribute("TODO")]
    public partial class ConsoleLogScope
    {
        internal ConsoleLogScope() { }
        public static Microsoft.Extensions.Logging.Console.ConsoleLogScope Current { get { throw null; } set { } }
        public Microsoft.Extensions.Logging.Console.ConsoleLogScope Parent { get { throw null; } }
        public static System.IDisposable Push(string name, object state) { throw null; }
        public override string ToString() { throw null; }
    }
    [System.ObsoleteAttribute("TODO")]
    public partial interface IConsoleLoggerSettings
    {
        Microsoft.Extensions.Primitives.IChangeToken ChangeToken { get; }
        bool IncludeScopes { get; }
        Microsoft.Extensions.Logging.Console.IConsoleLoggerSettings Reload();
        bool TryGetSwitch(string name, out Microsoft.Extensions.Logging.LogLevel level);
    }
    public partial class JsonConsoleFormatterOptions : Microsoft.Extensions.Logging.Console.ConsoleFormatterOptions
    {
        public JsonConsoleFormatterOptions() { }
        public System.Text.Json.JsonWriterOptions JsonWriterOptions { get { throw null; } set { } }
    }
    public enum LoggerColorBehavior
    {
        Default = 0,
        Enabled = 1,
        Disabled = 2,
    }
    public partial class SimpleConsoleFormatterOptions : Microsoft.Extensions.Logging.Console.ConsoleFormatterOptions
    {
        public SimpleConsoleFormatterOptions() { }
        public Microsoft.Extensions.Logging.Console.LoggerColorBehavior ColorBehavior { get { throw null; } set { } }
        public bool SingleLine { get { throw null; } set { } }
    }
}
namespace Microsoft.Extensions.Logging.Console.Internal
{
    [System.ObsoleteAttribute("TODO")]
    public partial class AnsiLogConsole : Microsoft.Extensions.Logging.Console.Internal.IConsole
    {
        public AnsiLogConsole(Microsoft.Extensions.Logging.Console.Internal.IAnsiSystemConsole systemConsole) { }
        public void Flush() { }
        public void Write(string message, System.ConsoleColor? background, System.ConsoleColor? foreground) { }
        public void WriteLine(string message, System.ConsoleColor? background, System.ConsoleColor? foreground) { }
    }
    [System.ObsoleteAttribute("TODO")]
    public partial class ConsoleLoggerProcessor
    {
        public Microsoft.Extensions.Logging.Console.Internal.IConsole Console;
        public ConsoleLoggerProcessor() { }
        public void Dispose() { }
        public virtual void EnqueueMessage(Microsoft.Extensions.Logging.Console.Internal.LogMessageEntry message) { }
    }
    [System.ObsoleteAttribute("TODO")]
    public partial interface IAnsiSystemConsole
    {
        void Write(string message);
        void WriteLine(string message);
    }
    [System.ObsoleteAttribute("TODO")]
    public partial interface IConsole
    {
        void Flush();
        void Write(string message, System.ConsoleColor? background, System.ConsoleColor? foreground);
        void WriteLine(string message, System.ConsoleColor? background, System.ConsoleColor? foreground);
    }
    [System.ObsoleteAttribute("TODO")]
    public sealed partial class LogMessageEntry
    {
        internal LogMessageEntry() { }
        public System.ConsoleColor? LevelBackground;
        public System.ConsoleColor? LevelForeground;
        public string LevelString;
        public string Message;
        public System.ConsoleColor? MessageColor;
    }
    [System.ObsoleteAttribute("TODO")]
    public partial class WindowsLogConsole : Microsoft.Extensions.Logging.Console.Internal.IConsole
    {
        public WindowsLogConsole() { }
        public void Flush() { }
        public void Write(string message, System.ConsoleColor? background, System.ConsoleColor? foreground) { }
        public void WriteLine(string message, System.ConsoleColor? background, System.ConsoleColor? foreground) { }
    }
}
