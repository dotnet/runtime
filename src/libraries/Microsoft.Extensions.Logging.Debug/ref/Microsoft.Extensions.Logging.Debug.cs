// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace Microsoft.Extensions.Logging
{
    public static partial class DebugLoggerFactoryExtensions
    {
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.ObsoleteAttribute("This method is retained only for compatibility. The recommended alternative is AddDebug(this ILoggingBuilder builder).", error: true)]
        public static Microsoft.Extensions.Logging.ILoggerFactory AddDebug(this Microsoft.Extensions.Logging.ILoggerFactory factory) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.ObsoleteAttribute("This method is retained only for compatibility. The recommended alternative is AddDebug(this ILoggingBuilder builder).", error: true)]
        public static Microsoft.Extensions.Logging.ILoggerFactory AddDebug(this Microsoft.Extensions.Logging.ILoggerFactory factory, Microsoft.Extensions.Logging.LogLevel minLevel) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.ObsoleteAttribute("This method is retained only for compatibility. The recommended alternative is AddDebug(this ILoggingBuilder builder).", error: true)]
        public static Microsoft.Extensions.Logging.ILoggerFactory AddDebug(this Microsoft.Extensions.Logging.ILoggerFactory factory, System.Func<string, Microsoft.Extensions.Logging.LogLevel, bool> filter) { throw null; }
        public static Microsoft.Extensions.Logging.ILoggingBuilder AddDebug(this Microsoft.Extensions.Logging.ILoggingBuilder builder) { throw null; }
    }
}
namespace Microsoft.Extensions.Logging.Debug
{
    [Microsoft.Extensions.Logging.ProviderAliasAttribute("Debug")]
    public partial class DebugLoggerProvider : Microsoft.Extensions.Logging.ILoggerProvider, System.IDisposable
    {
        public DebugLoggerProvider() { }
        public Microsoft.Extensions.Logging.ILogger CreateLogger(string name) { throw null; }
        public void Dispose() { }
    }
}
