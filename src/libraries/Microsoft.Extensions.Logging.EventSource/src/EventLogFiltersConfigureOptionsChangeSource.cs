// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Logging.EventSource;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Logging
{
    internal class EventLogFiltersConfigureOptionsChangeSource: IOptionsChangeTokenSource<LoggerFilterOptions>
    {
        private readonly LoggingEventSource _eventSource;

        public EventLogFiltersConfigureOptionsChangeSource(LoggingEventSource eventSource)
        {
            _eventSource = eventSource;
        }

        public IChangeToken GetChangeToken() => _eventSource.GetFilterChangeToken();

        public string Name { get; }
    }
}