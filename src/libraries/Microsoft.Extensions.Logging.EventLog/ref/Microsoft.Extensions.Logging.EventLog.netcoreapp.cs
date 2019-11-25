// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
        public EventLogLoggerProvider(Microsoft.Extensions.Logging.EventLog.EventLogSettings settings) { }
        public EventLogLoggerProvider(Microsoft.Extensions.Options.IOptions<Microsoft.Extensions.Logging.EventLog.EventLogSettings> options) { }
        public Microsoft.Extensions.Logging.ILogger CreateLogger(string name) { throw null; }
        public void Dispose() { }
        public void SetScopeProvider(Microsoft.Extensions.Logging.IExternalScopeProvider scopeProvider) { }
    }
    public partial class EventLogSettings
    {
        public EventLogSettings() { }
        public System.Func<string, Microsoft.Extensions.Logging.LogLevel, bool> Filter { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string LogName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string MachineName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string SourceName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
}
