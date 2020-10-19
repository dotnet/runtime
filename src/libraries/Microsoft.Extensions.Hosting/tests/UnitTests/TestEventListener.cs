// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Tracing;

namespace Microsoft.Extensions.Hosting.Tests
{
    internal class TestEventListener : EventListener
    {
        private volatile bool _disposed;

        private ConcurrentQueue<EventWrittenEventArgs> _events = new ConcurrentQueue<EventWrittenEventArgs>();

        public IEnumerable<EventWrittenEventArgs> EventData => _events;

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Name == "Microsoft-Extensions-Logging")
            {
                EnableEvents(eventSource, EventLevel.Informational);
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (!_disposed)
            {
                _events.Enqueue(eventData);
            }
        }

        public override void Dispose()
        {
            _disposed = true;
            base.Dispose();
        }
    }
}
