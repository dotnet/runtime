// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging.EventSource;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Logging
{
    internal sealed class EventLogFiltersConfigureOptions : IConfigureOptions<LoggerFilterOptions>
    {
        private readonly LoggingEventSource _eventSource;

        public EventLogFiltersConfigureOptions(LoggingEventSource eventSource)
        {
            _eventSource = eventSource;
        }

        public void Configure(LoggerFilterOptions options)
        {
            foreach (LoggerFilterRule rule in _eventSource.GetFilterRules())
            {
                options.Rules.Add(rule);
            }
        }
    }
}
