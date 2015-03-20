// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Diagnostics.TraceSourceTests
{
    // mock TraceListener to make assertions against TraceSource behavior
    internal class TestTraceListener : TraceListener
    {
        public enum Method
        {
            TraceEvent = 0,
            TraceData,
            Dispose,
            Write,
            WriteLine,
        }

        int[] _calls;

        /// <summary>
        /// Gets the number of times any of the public methods on this instance were called.
        /// </summary>
        public int GetCallCount(Method group) {
            return _calls[(int) group]; 
        }

        private void Call(Method method)
        {
            _calls[(int)method]++;
        }
        
        public TestTraceListener()
        {
            _calls = new int[5];
        }

        protected override void Dispose(bool disposing)
        {
            Call(Method.Dispose);
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id)
        {
            Call(Method.TraceEvent);
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args)
        {
            Call(Method.TraceEvent);
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
        {
            Call(Method.TraceEvent);
        }

        public override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, object data)
        {
            Call(Method.TraceData);
        }

        public override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, params object[] data)
        {
            Call(Method.TraceData);
        }

        public override void Write(string message)
        {
            Call(Method.Write);
        }

        public override void WriteLine(string message)
        {
            Call(Method.WriteLine);
        }
    }
}