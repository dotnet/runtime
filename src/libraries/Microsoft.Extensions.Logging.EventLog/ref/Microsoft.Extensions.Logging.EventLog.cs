// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace Microsoft.Extensions.Logging
{
    public static partial class EventLoggerFactoryExtensions
    {
        public static Microsoft.Extensions.Logging.ILoggingBuilder AddEventLog(this Microsoft.Extensions.Logging.ILoggingBuilder builder) { throw null; }
        public static Microsoft.Extensions.Logging.ILoggingBuilder AddEventLog(this Microsoft.Extensions.Logging.ILoggingBuilder builder, Microsoft.Extensions.Logging.EventLog.EventLogSettings settings) { throw null; }
        public static Microsoft.Extensions.Logging.ILoggingBuilder AddEventLog(this Microsoft.Extensions.Logging.ILoggingBuilder builder, System.Action<Microsoft.Extensions.Logging.EventLog.EventLogSettings> configure) { throw null; }
    }
}
namespace Microsoft.Extensions.Logging.EventLog
{
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
        public System.Func<string, Microsoft.Extensions.Logging.LogLevel, bool>? Filter { get { throw null; } set { } }
        public string? LogName { get { throw null; } set { } }
        public string? MachineName { get { throw null; } set { } }
        public string? SourceName { get { throw null; } set { } }
    }
}
