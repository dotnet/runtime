// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace Microsoft.Extensions.Logging
{
    public static partial class DebugLoggerFactoryExtensions
    {
        [System.ObsoleteAttribute("This method is obsolete and will be removed in a future version. The recommended alternative is AddDebug(this ILoggingBuilder builder).")]
        public static Microsoft.Extensions.Logging.ILoggerFactory AddDebug(this Microsoft.Extensions.Logging.ILoggerFactory factory) { throw null; }
        [System.ObsoleteAttribute("This method is obsolete and will be removed in a future version. The recommended alternative is AddDebug(this ILoggingBuilder builder).")]
        public static Microsoft.Extensions.Logging.ILoggerFactory AddDebug(this Microsoft.Extensions.Logging.ILoggerFactory factory, Microsoft.Extensions.Logging.LogLevel minLevel) { throw null; }
        [System.ObsoleteAttribute("This method is obsolete and will be removed in a future version. The recommended alternative is AddDebug(this ILoggingBuilder builder).")]
        public static Microsoft.Extensions.Logging.ILoggerFactory AddDebug(this Microsoft.Extensions.Logging.ILoggerFactory factory, System.Func<string, Microsoft.Extensions.Logging.LogLevel, bool> filter) { throw null; }
        public static Microsoft.Extensions.Logging.ILoggingBuilder AddDebug(this Microsoft.Extensions.Logging.ILoggingBuilder builder) { throw null; }
    }
}
namespace Microsoft.Extensions.Logging.Debug
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public sealed partial class DebugLogger : Microsoft.Extensions.Logging.ILogger
    {
        public DebugLogger(string name) { }
        public System.IDisposable BeginScope<TState>(TState state) where TState : notnull { throw null; }
        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) { throw null; }
        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state, System.Exception? exception, System.Func<TState, System.Exception?, string> formatter) { }
    }
    [Microsoft.Extensions.Logging.ProviderAliasAttribute("Debug")]
    public partial class DebugLoggerProvider : Microsoft.Extensions.Logging.ILoggerProvider, System.IDisposable
    {
        public DebugLoggerProvider() { }
        public Microsoft.Extensions.Logging.ILogger CreateLogger(string name) { throw null; }
        public void Dispose() { }
    }
}
