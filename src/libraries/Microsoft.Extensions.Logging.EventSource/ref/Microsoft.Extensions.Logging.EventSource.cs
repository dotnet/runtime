// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace Microsoft.Extensions.Logging
{
    public static partial class EventSourceLoggerFactoryExtensions
    {
        public static Microsoft.Extensions.Logging.ILoggingBuilder AddEventSourceLogger(this Microsoft.Extensions.Logging.ILoggingBuilder builder) { throw null; }
    }
}
namespace Microsoft.Extensions.Logging.EventSource
{
    [Microsoft.Extensions.Logging.ProviderAliasAttribute("EventSource")]
    public partial class EventSourceLoggerProvider : Microsoft.Extensions.Logging.ILoggerProvider, System.IDisposable
    {
        public EventSourceLoggerProvider(Microsoft.Extensions.Logging.EventSource.LoggingEventSource eventSource) { }
        public Microsoft.Extensions.Logging.ILogger CreateLogger(string categoryName) { throw null; }
        public void Dispose() { }
    }
    [System.Diagnostics.Tracing.EventSourceAttribute(Name="Microsoft-Extensions-Logging")]
    public sealed partial class LoggingEventSource : System.Diagnostics.Tracing.EventSource
    {
        internal LoggingEventSource() { }
        protected override void OnEventCommand(System.Diagnostics.Tracing.EventCommandEventArgs command) { }
        public static partial class Keywords
        {
            public const System.Diagnostics.Tracing.EventKeywords FormattedMessage = (System.Diagnostics.Tracing.EventKeywords)(4);
            public const System.Diagnostics.Tracing.EventKeywords JsonMessage = (System.Diagnostics.Tracing.EventKeywords)(8);
            public const System.Diagnostics.Tracing.EventKeywords Message = (System.Diagnostics.Tracing.EventKeywords)(2);
            public const System.Diagnostics.Tracing.EventKeywords Meta = (System.Diagnostics.Tracing.EventKeywords)(1);
        }
    }
}
