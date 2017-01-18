// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace System.Diagnostics.TraceSourceTests
{
    // mock TraceListener to make assertions against TraceSource behavior
    internal class TestTraceListener : TraceListener
    {
        public enum Method
        {
            TraceEvent = 0,
            TraceData,
            TraceTransfer,
            Dispose,
            Write,
            WriteLine,
            Flush,
            Fail,
            //NOTE: update MethodEnumCount if values are added
        }

        private const int MethodEnumCount = 8;

        public TestTraceListener(bool threadSafe = false)
            : this(null, threadSafe)
        {
        }

        public TestTraceListener(String name, bool threadSafe = false)
            : base(name)
        {
            _threadSafe = threadSafe;
            _calls = new int[MethodEnumCount];
        }

        private bool _threadSafe;
        private int[] _calls;

        public override bool IsThreadSafe
        {
            get
            {
                return _threadSafe;
            }
        }

        /// <summary>
        /// Gets the number of times any of the public methods on this instance were called.
        /// </summary>
        public int GetCallCount(Method group)
        {
            return _calls[(int)group];
        }

        private void Call(Method method)
        {
            _calls[(int)method]++;
        }

        protected override void Dispose(bool disposing)
        {
            Call(Method.Dispose);
        }

        public override void Fail(string message)
        {
            Call(Method.Fail);
        }

        public override void Fail(string message, string detailMessage)
        {
            Call(Method.Fail);
        }

        public override void Flush()
        {
            Call(Method.Flush);
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

        public override void TraceTransfer(TraceEventCache eventCache, String source, int id, string message, Guid relatedActivityId)
        {
            Call(Method.TraceTransfer);
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
