// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace System.Diagnostics.TraceSourceTests
{
    // TraceListener that can be used to inspect the trace output in tests
    internal class TestTextTraceListener : TraceListener
    {
        private StringWriter _writer;
        private StringWriter _output;

        private int _writeCount;

        public TestTextTraceListener()
        {
            _writer = new StringWriter();
            _output = new StringWriter();
            _writeCount = 0;
        }

        /// <summary>
        /// Gets the number of times Write() was called for test assertions.
        /// </summary>
        public int WriteCount { get { return _writeCount; } }

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
            _writeCount++;
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
}
