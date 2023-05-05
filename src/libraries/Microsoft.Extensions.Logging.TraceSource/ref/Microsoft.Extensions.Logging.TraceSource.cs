// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace Microsoft.Extensions.Logging
{
    public static partial class TraceSourceFactoryExtensions
    {
        [System.ObsoleteAttribute("This method is obsolete and will be removed in a future version. The recommended alternative is AddTraceSource(this ILoggingBuilder builder).")]
        public static Microsoft.Extensions.Logging.ILoggerFactory AddTraceSource(this Microsoft.Extensions.Logging.ILoggerFactory factory, System.Diagnostics.SourceSwitch sourceSwitch) { throw null; }
        [System.ObsoleteAttribute("This method is obsolete and will be removed in a future version. The recommended alternative is AddTraceSource(this ILoggingBuilder builder).")]
        public static Microsoft.Extensions.Logging.ILoggerFactory AddTraceSource(this Microsoft.Extensions.Logging.ILoggerFactory factory, System.Diagnostics.SourceSwitch sourceSwitch, System.Diagnostics.TraceListener listener) { throw null; }
        [System.ObsoleteAttribute("This method is obsolete and will be removed in a future version. The recommended alternative is AddTraceSource(this ILoggingBuilder builder).")]
        public static Microsoft.Extensions.Logging.ILoggerFactory AddTraceSource(this Microsoft.Extensions.Logging.ILoggerFactory factory, string switchName) { throw null; }
        [System.ObsoleteAttribute("This method is obsolete and will be removed in a future version. The recommended alternative is AddTraceSource(this ILoggingBuilder builder).")]
        public static Microsoft.Extensions.Logging.ILoggerFactory AddTraceSource(this Microsoft.Extensions.Logging.ILoggerFactory factory, string switchName, System.Diagnostics.TraceListener listener) { throw null; }
        public static Microsoft.Extensions.Logging.ILoggingBuilder AddTraceSource(this Microsoft.Extensions.Logging.ILoggingBuilder builder, System.Diagnostics.SourceSwitch sourceSwitch) { throw null; }
        public static Microsoft.Extensions.Logging.ILoggingBuilder AddTraceSource(this Microsoft.Extensions.Logging.ILoggingBuilder builder, System.Diagnostics.SourceSwitch sourceSwitch, System.Diagnostics.TraceListener listener) { throw null; }
        public static Microsoft.Extensions.Logging.ILoggingBuilder AddTraceSource(this Microsoft.Extensions.Logging.ILoggingBuilder builder, string switchName) { throw null; }
        public static Microsoft.Extensions.Logging.ILoggingBuilder AddTraceSource(this Microsoft.Extensions.Logging.ILoggingBuilder builder, string switchName, System.Diagnostics.TraceListener listener) { throw null; }
    }
}
namespace Microsoft.Extensions.Logging.TraceSource
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public sealed partial class TraceSourceLogger : Microsoft.Extensions.Logging.ILogger
    {
        public TraceSourceLogger(System.Diagnostics.TraceSource traceSource) { }
        public System.IDisposable BeginScope<TState>(TState state) where TState : notnull { throw null; }
        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) { throw null; }
        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state, System.Exception? exception, System.Func<TState, System.Exception?, string> formatter) { }
    }
    [Microsoft.Extensions.Logging.ProviderAliasAttribute("TraceSource")]
    public partial class TraceSourceLoggerProvider : Microsoft.Extensions.Logging.ILoggerProvider, System.IDisposable
    {
        public TraceSourceLoggerProvider(System.Diagnostics.SourceSwitch rootSourceSwitch) { }
        public TraceSourceLoggerProvider(System.Diagnostics.SourceSwitch rootSourceSwitch, System.Diagnostics.TraceListener? rootTraceListener) { }
        public Microsoft.Extensions.Logging.ILogger CreateLogger(string name) { throw null; }
        public void Dispose() { }
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public sealed partial class TraceSourceScope : System.IDisposable
    {
        public TraceSourceScope(object state) { }
        public void Dispose() { }
    }
}
