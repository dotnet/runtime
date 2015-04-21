// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Diagnostics.TextWriterTraceListenerTests
{
    internal class TestTraceFilter : TraceFilter
    {
        private readonly bool _shouldTrace;

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
