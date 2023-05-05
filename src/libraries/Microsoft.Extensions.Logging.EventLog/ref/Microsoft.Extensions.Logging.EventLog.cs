// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace Microsoft.Extensions.Logging
{
    public static partial class EventLoggerFactoryExtensions
    {
        [System.ObsoleteAttribute("This method is obsolete and will be removed in a future version. The recommended alternative is AddEventLog(this ILoggingBuilder builder).")]
        public static Microsoft.Extensions.Logging.ILoggerFactory AddEventLog(this Microsoft.Extensions.Logging.ILoggerFactory factory) { throw null; }
        [System.ObsoleteAttribute("This method is obsolete and will be removed in a future version. The recommended alternative is AddEventLog(this ILoggingBuilder builder).")]
        public static Microsoft.Extensions.Logging.ILoggerFactory AddEventLog(this Microsoft.Extensions.Logging.ILoggerFactory factory, Microsoft.Extensions.Logging.EventLog.EventLogSettings settings) { throw null; }
        [System.ObsoleteAttribute("This method is obsolete and will be removed in a future version. The recommended alternative is AddEventLog(this ILoggingBuilder builder).")]
        public static Microsoft.Extensions.Logging.ILoggerFactory AddEventLog(this Microsoft.Extensions.Logging.ILoggerFactory factory, Microsoft.Extensions.Logging.LogLevel minLevel) { throw null; }
        public static Microsoft.Extensions.Logging.ILoggingBuilder AddEventLog(this Microsoft.Extensions.Logging.ILoggingBuilder builder) { throw null; }
        public static Microsoft.Extensions.Logging.ILoggingBuilder AddEventLog(this Microsoft.Extensions.Logging.ILoggingBuilder builder, Microsoft.Extensions.Logging.EventLog.EventLogSettings settings) { throw null; }
        public static Microsoft.Extensions.Logging.ILoggingBuilder AddEventLog(this Microsoft.Extensions.Logging.ILoggingBuilder builder, System.Action<Microsoft.Extensions.Logging.EventLog.EventLogSettings> configure) { throw null; }
    }
}
namespace Microsoft.Extensions.Logging.EventLog
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public sealed partial class EventLogLogger : Microsoft.Extensions.Logging.ILogger
    {
        public EventLogLogger(string name, Microsoft.Extensions.Logging.EventLog.EventLogSettings settings, Microsoft.Extensions.Logging.IExternalScopeProvider? externalScopeProvider) { }
        public Microsoft.Extensions.Logging.EventLog.Internal.IEventLog EventLog { get { throw null; } }
        public System.IDisposable? BeginScope<TState>(TState state) where TState : notnull { throw null; }
        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) { throw null; }
        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state, System.Exception? exception, System.Func<TState, System.Exception?, string> formatter) { }
    }
    [Microsoft.Extensions.Logging.ProviderAliasAttribute("EventLog")]
    public partial class EventLogLoggerProvider : Microsoft.Extensions.Logging.ILoggerProvider, Microsoft.Extensions.Logging.ISupportExternalScope, System.IDisposable
    {
        public EventLogLoggerProvider() { }
        public EventLogLoggerProvider(Microsoft.Extensions.Logging.EventLog.EventLogSettings? settings) { }
        public EventLogLoggerProvider(Microsoft.Extensions.Options.IOptions<Microsoft.Extensions.Logging.EventLog.EventLogSettings> options) { }
        public Microsoft.Extensions.Logging.ILogger CreateLogger(string name) { throw null; }
        public void Dispose() { }
        public void SetScopeProvider(Microsoft.Extensions.Logging.IExternalScopeProvider scopeProvider) { }
    }
    public partial class EventLogSettings
    {
        public EventLogSettings() { }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public Microsoft.Extensions.Logging.EventLog.Internal.IEventLog EventLog { get { throw null; } set { } }
        public System.Func<string, Microsoft.Extensions.Logging.LogLevel, bool>? Filter { get { throw null; } set { } }
        public string? LogName { get { throw null; } set { } }
        public string? MachineName { get { throw null; } set { } }
        public string? SourceName { get { throw null; } set { } }
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
    public sealed partial class WindowsEventLog : Microsoft.Extensions.Logging.EventLog.Internal.IEventLog
    {
        public WindowsEventLog(string logName, string machineName, string sourceName) { }
        public int? DefaultEventId { get { throw null; } set { } }
        public System.Diagnostics.EventLog DiagnosticsEventLog { get { throw null; } }
        public int MaxMessageSize { get { throw null; } }
        public void WriteEntry(string message, System.Diagnostics.EventLogEntryType type, int eventID, short category) { }
    }
}
namespace Microsoft.Extensions.Logging.EventLog.Internal
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public partial interface IEventLog
    {
        int? DefaultEventId { get; }
        int MaxMessageSize { get; }
        void WriteEntry(string message, System.Diagnostics.EventLogEntryType type, int eventID, short category);
    }
}
