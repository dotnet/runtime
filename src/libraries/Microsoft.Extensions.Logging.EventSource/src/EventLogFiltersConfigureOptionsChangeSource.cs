// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging.EventSource;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Logging
{
    internal sealed class EventLogFiltersConfigureOptionsChangeSource : IOptionsChangeTokenSource<LoggerFilterOptions>
    {
        private readonly LoggingEventSource _eventSource;

        public EventLogFiltersConfigureOptionsChangeSource(LoggingEventSource eventSource)
        {
            _eventSource = eventSource;
        }

        public IChangeToken GetChangeToken() => _eventSource.GetFilterChangeToken();

        public string? Name { get; }
    }
}
