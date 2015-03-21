// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using Xunit;

namespace System.Diagnostics.TraceSourceTests
{
    public class DefaultTraceListenerClassTests
    {
        private class TestDefaultTraceListener : DefaultTraceListener
        {
            private StringWriter _writer;

            public TestDefaultTraceListener()
            {
                _writer = new StringWriter();
            }

            public String Output
            {
                get { return _writer.ToString(); }
            }

            public override void Write(string message)
            {
                _writer.Write(message);
            }
        }


        [Fact]
        public void Constructor()
        {
            var listener = new DefaultTraceListener();
            Assert.Equal("Default", listener.Name);
        }

        [Fact]
        public void Fail()
        {
            var listener = new TestDefaultTraceListener();
            listener.Fail("FAIL");
            Assert.Contains("FAIL", listener.Output);
        }

        [Fact]
        public void TraceEvent()
        {
            var listener = new TestDefaultTraceListener();
            listener.TraceEvent(new TraceEventCache(), "Test", TraceEventType.Critical, 1);
            Assert.Contains("Test", listener.Output);
        }

        [Fact]
        public void WriteNull()
        {
            var listener = new DefaultTraceListener();
            // implied assert, does not throw
            listener.Write(null);
        }

        [Fact]
        public void WriteLongMessage()
        {
            var listener = new DefaultTraceListener();
            var longString = new String('a', 0x40000);
            listener.Write(longString);
            // nothing to assert, the output is written to Debug.Write
            // this simply provides code-coverage
        }
    }
}
