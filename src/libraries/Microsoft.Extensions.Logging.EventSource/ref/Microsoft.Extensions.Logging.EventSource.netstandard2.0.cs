// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
