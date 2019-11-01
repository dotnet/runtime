// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Diagnostics.TraceSourceTests
{
    public class EventTypeFilterClassTests
    {
        [Fact]
        public void Constructor()
        {
            var filter = new EventTypeFilter(SourceLevels.Error);
            Assert.Equal(SourceLevels.Error, filter.EventType);
        }

        [Fact]
        public void ShouldTrace()
        {
            var cache = new TraceEventCache();
            var filter = new EventTypeFilter(SourceLevels.Error);
            Assert.True(filter.ShouldTrace(cache, null, TraceEventType.Critical, 0, null, null, null, null));
            Assert.False(filter.ShouldTrace(cache, null, TraceEventType.Warning, 0, null, null, null, null));
        }

        [Fact]
        public void EventType()
        {
            var filter = new EventTypeFilter(SourceLevels.Error);
            Assert.Equal(SourceLevels.Error, filter.EventType);
            filter.EventType = SourceLevels.Off;
            Assert.Equal(SourceLevels.Off, filter.EventType);
        }
    }
}
