// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            filter.ShouldTrace(cache, "Source", TraceEventType.Critical, 1, "Message", new object[] { "arg1" }, null, new object[] { "Data1" });
        }
    }
}
