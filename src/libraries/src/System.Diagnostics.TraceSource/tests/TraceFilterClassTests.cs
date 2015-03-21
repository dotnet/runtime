// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.Diagnostics.TraceSourceTests
{
    public class TraceFilterClassTests
    {
        class TestTraceFilter : TraceFilter
        {
            private bool _shouldTrace;

            public TestTraceFilter(bool shouldTrace)
            {
                _shouldTrace = shouldTrace;
            }

            public override bool ShouldTrace(TraceEventCache cache, string source, TraceEventType eventType, int id, string formatOrMessage, object[] args, object data1, object[] data)
            {
                return _shouldTrace;
            }
        }

        [Fact]
        public void ShouldTrace()
        {
            var cache = new TraceEventCache();
            var filter = new TestTraceFilter(true);
            filter.ShouldTrace(cache, "Source", TraceEventType.Critical, 1, "Message");
            filter.ShouldTrace(cache, "Source", TraceEventType.Critical, 1, "Format", new Object[] { "arg1" });
            filter.ShouldTrace(cache, "Source", TraceEventType.Critical, 1, "Message", new Object[] { "arg1" }, null);
            filter.ShouldTrace(cache, "Source", TraceEventType.Critical, 1, "Message", new Object[] { "arg1" }, null, new Object[] { "Data1" });
        }
    }
}
