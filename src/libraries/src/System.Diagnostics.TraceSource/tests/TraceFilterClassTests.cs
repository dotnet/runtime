// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.Diagnostics.TraceSourceTests
{
    public class TraceFilterClassTests
    {
        [Fact]
        public void ShouldTraceTest()
        {
            var cache = new TraceEventCache();
            var filter = new TestTraceFilter(true);
            filter.ShouldTrace(cache, "Source", TraceEventType.Critical, 1, "Message", new Object[] { "arg1" }, null, new Object[] { "Data1" });
        }
    }
}
