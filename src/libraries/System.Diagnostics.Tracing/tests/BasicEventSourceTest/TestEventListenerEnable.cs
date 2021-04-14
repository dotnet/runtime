// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Tracing;
using Xunit;

namespace BasicEventSourceTests
{
    public class TestEventListenerEnable
    {
        [Fact]
        public void EnableEventsRespectsHigherEventLevel()
        {
            using var eventSource = new TestEventSource();
            using var listener = new TestEventListener();

            Assert.False(eventSource.IsEnabled());

            listener.EnableEvents(eventSource, EventLevel.Critical);
            Assert.True(eventSource.IsEnabled(EventLevel.Critical, EventKeywords.All));
            Assert.False(eventSource.IsEnabled(EventLevel.Informational, EventKeywords.All));
            Assert.False(eventSource.IsEnabled(EventLevel.Verbose, EventKeywords.All));

            listener.EnableEvents(eventSource, EventLevel.Informational);
            Assert.True(eventSource.IsEnabled(EventLevel.Critical, EventKeywords.All));
            Assert.True(eventSource.IsEnabled(EventLevel.Informational, EventKeywords.All));
            Assert.False(eventSource.IsEnabled(EventLevel.Verbose, EventKeywords.All));

            listener.EnableEvents(eventSource, EventLevel.LogAlways);
            Assert.True(eventSource.IsEnabled(EventLevel.Critical, EventKeywords.All));
            Assert.True(eventSource.IsEnabled(EventLevel.Informational, EventKeywords.All));
            Assert.True(eventSource.IsEnabled(EventLevel.Verbose, EventKeywords.All));
        }

        private sealed class TestEventListener : EventListener { }

        private sealed class TestEventSource : EventSource { }
    }
}
