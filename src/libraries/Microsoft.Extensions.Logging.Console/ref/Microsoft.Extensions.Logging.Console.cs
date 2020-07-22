// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace Microsoft.Extensions.Logging
{
    public static partial class ConsoleLoggerExtensions
    {
        public static Microsoft.Extensions.Logging.ILoggingBuilder AddConsole(this Microsoft.Extensions.Logging.ILoggingBuilder builder) { throw null; }
        public static Microsoft.Extensions.Logging.ILoggingBuilder AddConsole(this Microsoft.Extensions.Logging.ILoggingBuilder builder, System.Action<Microsoft.Extensions.Logging.Console.ConsoleLoggerOptions> configure) { throw null; }
        public static Microsoft.Extensions.Logging.ILoggingBuilder AddConsoleFormatter<TFormatter, TOptions>(this Microsoft.Extensions.Logging.ILoggingBuilder builder) where TFormatter : Microsoft.Extensions.Logging.Console.ConsoleFormatter where TOptions : Microsoft.Extensions.Logging.Console.ConsoleFormatterOptions { throw null; }
        public static Microsoft.Extensions.Logging.ILoggingBuilder AddConsoleFormatter<TFormatter, TOptions>(this Microsoft.Extensions.Logging.ILoggingBuilder builder, System.Action<TOptions> configure) where TFormatter : Microsoft.Extensions.Logging.Console.ConsoleFormatter where TOptions : Microsoft.Extensions.Logging.Console.ConsoleFormatterOptions { throw null; }
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
    public abstract partial class ConsoleFormatter
    {
        protected ConsoleFormatter(string name) { }
        public string Name { get { throw null; } }
        public abstract void Write<TState>(in Microsoft.Extensions.Logging.Abstractions.LogEntry<TState> logEntry, Microsoft.Extensions.Logging.IExternalScopeProvider scopeProvider, System.IO.TextWriter textWriter);
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
        public string TimestampFormat { get { throw null; } set { } }
        public bool UseUtcTimestamp { get { throw null; } set { } }
    }
    [System.ObsoleteAttribute("ConsoleLoggerFormat has been deprecated.", false)]
    public enum ConsoleLoggerFormat
    {
        Default = 0,
        Systemd = 1,
    }
    public partial class ConsoleLoggerOptions
    {
        public ConsoleLoggerOptions() { }
        [System.ObsoleteAttribute("ConsoleLoggerOptions.DisableColors has been deprecated. Please use SimpleConsoleFormatterOptions.DisableColors instead.", false)]
        public bool DisableColors { get { throw null; } set { } }
        [System.ObsoleteAttribute("ConsoleLoggerOptions.Format has been deprecated. Please use ConsoleLoggerOptions.FormatterName instead.", false)]
        public Microsoft.Extensions.Logging.Console.ConsoleLoggerFormat Format { get { throw null; } set { } }
        public string FormatterName { get { throw null; } set { } }
        [System.ObsoleteAttribute("ConsoleLoggerOptions.IncludeScopes has been deprecated. Please use ConsoleFormatterOptions.IncludeScopes instead.", false)]
        public bool IncludeScopes { get { throw null; } set { } }
        public Microsoft.Extensions.Logging.LogLevel LogToStandardErrorThreshold { get { throw null; } set { } }
        [System.ObsoleteAttribute("ConsoleLoggerOptions.TimestampFormat has been deprecated. Please use ConsoleFormatterOptions.TimestampFormat instead.", false)]
        public string TimestampFormat { get { throw null; } set { } }
        [System.ObsoleteAttribute("ConsoleLoggerOptions.UseUtcTimestamp has been deprecated. Please use ConsoleFormatterOptions.UseUtcTimestamp instead.", false)]
        public bool UseUtcTimestamp { get { throw null; } set { } }
    }
    [Microsoft.Extensions.Logging.ProviderAliasAttribute("Console")]
    public partial class ConsoleLoggerProvider : Microsoft.Extensions.Logging.ILoggerProvider, Microsoft.Extensions.Logging.ISupportExternalScope, System.IDisposable
    {
        public ConsoleLoggerProvider(Microsoft.Extensions.Options.IOptionsMonitor<Microsoft.Extensions.Logging.Console.ConsoleLoggerOptions> options) { }
        public ConsoleLoggerProvider(Microsoft.Extensions.Options.IOptionsMonitor<Microsoft.Extensions.Logging.Console.ConsoleLoggerOptions> options, System.Collections.Generic.IEnumerable<Microsoft.Extensions.Logging.Console.ConsoleFormatter> formatters) { }
        public Microsoft.Extensions.Logging.ILogger CreateLogger(string name) { throw null; }
        public void Dispose() { }
        public void SetScopeProvider(Microsoft.Extensions.Logging.IExternalScopeProvider scopeProvider) { }
    }
    public partial class JsonConsoleFormatterOptions : Microsoft.Extensions.Logging.Console.ConsoleFormatterOptions
    {
        public JsonConsoleFormatterOptions() { }
        public System.Text.Json.JsonWriterOptions JsonWriterOptions { get { throw null; } set { } }
    }
    public partial class SimpleConsoleFormatterOptions : Microsoft.Extensions.Logging.Console.ConsoleFormatterOptions
    {
        public SimpleConsoleFormatterOptions() { }
        public bool DisableColors { get { throw null; } set { } }
        public bool SingleLine { get { throw null; } set { } }
    }
}
