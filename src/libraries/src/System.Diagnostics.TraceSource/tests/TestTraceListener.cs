// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

namespace System.Diagnostics.TraceSourceTests
{

    // TraceListener that can be used to inspect the trace output in tests
    internal class TestTextTraceListener : TraceListener
    {
        private StringWriter _writer;
        private StringWriter _output;

        public TestTextTraceListener()
        {
            _writer = new StringWriter();
            _output = new StringWriter();
        }

        public String Output
        {
            get { return _output.ToString(); }
        }

        public override void Flush()
        {
            _output.Write(_writer.ToString());
            _writer.GetStringBuilder().Clear();
        }

        public override void Write(string message)
        {
            if (NeedIndent) WriteIndent();
            _writer.Write(message);
        }

        public override void WriteLine(string message)
        {
            if (NeedIndent) WriteIndent();
            Write(message);
            _writer.WriteLine();
            NeedIndent = true;
        }
    }

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
            Flush,
            Fail,
            //NOTE: update MethodEnumCount if values are added
        }

        const int MethodEnumCount = 7;

        public TestTraceListener(bool threadSafe = false)
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
