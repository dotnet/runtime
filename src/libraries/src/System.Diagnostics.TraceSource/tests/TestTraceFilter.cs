// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Diagnostics.TraceSourceTests
{
    internal class TestTraceFilter : TraceFilter
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
}
