// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Diagnostics.TraceSourceTests
{
    using Method = TestTraceListener.Method;

    public class TraceListenerClassTests
    {
        [Fact]
        public void NameTest()
        {
            var listener = new TestTraceListener();
            listener.Name = null;
            Assert.Equal("", listener.Name);
        }

        [Fact]
        public void IndentLevelTest()
        {
            var listener = new TestTraceListener();
            listener.IndentLevel = 0;
            Assert.Equal(0, listener.IndentLevel);
            listener.IndentLevel = 2;
            Assert.Equal(2, listener.IndentLevel);
            listener.IndentLevel = -1;
            Assert.Equal(0, listener.IndentLevel);
        }

        [Fact]
        public void IndentSizeTest()
        {
            var listener = new TestTraceListener();
            listener.IndentSize = 0;
            Assert.Equal(0, listener.IndentSize);
            listener.IndentSize = 2;
            Assert.Equal(2, listener.IndentSize);
            Assert.Throws<ArgumentOutOfRangeException>(() => listener.IndentSize = -1);
        }

        [Fact]
        public void FilterTest()
        {
            var listener = new TestTraceListener();
            listener.Filter = new SourceFilter("TestSource");
            Assert.NotNull(listener.Filter);
        }

        [Fact]
        public void TraceOutputOptionsTest()
        {
            var listener = new TestTraceListener();
            listener.TraceOutputOptions = TraceOptions.None;

            // NOTE: TraceOptions includes values for 0x01 and 0x20 in .NET Framework 4.5, but not in CoreFX
            // These assertions test for those missing values, and the exceptional condition that
            // maintains compatibility with 4.5
            var missingValue = (TraceOptions)0x01;
            listener.TraceOutputOptions = missingValue;

            missingValue = (TraceOptions)0x20;
            listener.TraceOutputOptions = missingValue;

            var badValue = (TraceOptions)0x80;
            Assert.Throws<ArgumentOutOfRangeException>(() => listener.TraceOutputOptions = badValue);
        }

        [Fact]
        public void WriteObjectTest()
        {
            var listener = new TestTraceListener();
            listener.Filter = new TestTraceFilter(false);
            listener.Write((object)"Message");
            Assert.Equal(0, listener.GetCallCount(Method.Write));

            listener.Filter = new TestTraceFilter(true);

            listener.Write((object)null);
            Assert.Equal(0, listener.GetCallCount(Method.Write));

            listener.Write((object)"Message");
            Assert.Equal(1, listener.GetCallCount(Method.Write));
        }

        [Fact]
        public void WriteCategoryTest()
        {
            var listener = new TestTraceListener();
            listener.Filter = new TestTraceFilter(false);
            listener.Write("Message", "Category");
            Assert.Equal(0, listener.GetCallCount(Method.Write));

            listener.Filter = new TestTraceFilter(true);

            listener.Write("Message", null);
            Assert.Equal(1, listener.GetCallCount(Method.Write));

            listener.Write("Message", "Category");
            Assert.Equal(2, listener.GetCallCount(Method.Write));

            listener.Write(null, "Category");
            Assert.Equal(3, listener.GetCallCount(Method.Write));
        }

        [Fact]
        public void WriteCategoryTest2()
        {
            var listener = new TestTraceListener();
            listener.Filter = new TestTraceFilter(false);
            listener.Write((object)"Message", "Category");
            Assert.Equal(0, listener.GetCallCount(Method.Write));

            listener.Filter = new TestTraceFilter(true);

            listener.Write((object)"Message", null);
            Assert.Equal(1, listener.GetCallCount(Method.Write));

            listener.Write((object)"Message", "Category");
            Assert.Equal(2, listener.GetCallCount(Method.Write));

            listener.Write((object)null, "Category");
            Assert.Equal(3, listener.GetCallCount(Method.Write));
        }

        [Fact]
        public void IndentTest()
        {
            var listener = new TestTextTraceListener();
            listener.IndentLevel = 2;
            listener.IndentSize = 4;

            listener.Write("Message");
            listener.Flush();
            Assert.Equal("        Message", listener.Output);

            listener = new TestTextTraceListener();
            listener.IndentLevel = 1;
            listener.IndentSize = 3;

            listener.Write("Message");
            listener.Flush();
            Assert.Equal("   Message", listener.Output);
        }

        [Fact]
        public void WriteLineObjectTest()
        {
            var listener = new TestTraceListener();
            listener.Filter = new TestTraceFilter(false);
            listener.WriteLine((object)"Message");
            Assert.Equal(0, listener.GetCallCount(Method.WriteLine));

            listener.Filter = new TestTraceFilter(true);

            // NOTE: Writing null will result in a newline being written
            listener.WriteLine((object)null);
            Assert.Equal(1, listener.GetCallCount(Method.WriteLine));

            listener.WriteLine((object)"Message");
            Assert.Equal(2, listener.GetCallCount(Method.WriteLine));
        }

        [Fact]
        public void WriteLineCategoryTest()
        {
            var listener = new TestTraceListener();
            listener.Filter = new TestTraceFilter(false);
            listener.WriteLine("Message", "Category");
            Assert.Equal(0, listener.GetCallCount(Method.WriteLine));

            listener.Filter = new TestTraceFilter(true);

            listener.WriteLine("Message", null);
            Assert.Equal(1, listener.GetCallCount(Method.WriteLine));

            listener.WriteLine("Message", "Category");
            Assert.Equal(2, listener.GetCallCount(Method.WriteLine));

            listener.WriteLine(null, "Category");
            Assert.Equal(3, listener.GetCallCount(Method.WriteLine));
        }

        [Fact]
        public void WriteLineCategoryTest2()
        {
            var listener = new TestTraceListener();
            listener.Filter = new TestTraceFilter(false);
            listener.WriteLine((object)"Message", "Category");
            Assert.Equal(0, listener.GetCallCount(Method.WriteLine));

            listener.Filter = new TestTraceFilter(true);

            listener.WriteLine((object)"Message", null);
            Assert.Equal(1, listener.GetCallCount(Method.WriteLine));

            listener.WriteLine((object)"Message", "Category");
            Assert.Equal(2, listener.GetCallCount(Method.WriteLine));

            listener.WriteLine((object)null, "Category");
            Assert.Equal(3, listener.GetCallCount(Method.WriteLine));
        }

        [Fact]
        public void TraceDataTest()
        {
            var cache = new TraceEventCache();
            var listener = new TestTextTraceListener();
            listener.Filter = new TestTraceFilter(false);
            listener.TraceData(cache, "Source", TraceEventType.Critical, 1, new object());
            Assert.Equal(0, listener.WriteCount);

            listener = new TestTextTraceListener();
            listener.TraceData(cache, "Source", TraceEventType.Critical, 1, new object());
            var expected = 2; // header and message.
            Assert.Equal(expected, listener.WriteCount);

            listener = new TestTextTraceListener();
            listener.TraceData(cache, "Source", TraceEventType.Critical, 1, (object)null);
            Assert.Equal(expected, listener.WriteCount);
        }

        [Fact]
        public void TraceDataTest2()
        {
            var cache = new TraceEventCache();
            var listener = new TestTextTraceListener();
            listener.Filter = new TestTraceFilter(false);
            listener.TraceData(cache, "Source", TraceEventType.Critical, 1, new object[0]);
            Assert.Equal(0, listener.WriteCount);

            listener = new TestTextTraceListener();
            listener.TraceData(cache, "Source", TraceEventType.Critical, 1, (object[])null);
            var expected = 2; // header and message.
            Assert.Equal(expected, listener.WriteCount);

            listener = new TestTextTraceListener();
            listener.TraceData(cache, "Source", TraceEventType.Critical, 1, "Arg1", "Arg2");
            Assert.Equal(expected, listener.WriteCount);

            listener = new TestTextTraceListener();
            listener.TraceData(cache, "Source", TraceEventType.Critical, 1, "Arg1", null);
            Assert.Equal(expected, listener.WriteCount);
        }

        [Fact]
        public void TraceEventTest()
        {
            var cache = new TraceEventCache();
            var listener = new TestTextTraceListener();
            listener.Filter = new TestTraceFilter(false);
            listener.TraceEvent(cache, "Source", TraceEventType.Critical, 1);
            Assert.Equal(0, listener.WriteCount);

            listener = new TestTextTraceListener();
            listener.TraceEvent(cache, "Source", TraceEventType.Critical, 1);
            var expected = 2; // header and message.
            Assert.Equal(expected, listener.WriteCount);
        }

        [Fact]
        public void TraceEventTest2()
        {
            var cache = new TraceEventCache();
            var listener = new TestTextTraceListener();
            listener.Filter = new TestTraceFilter(false);
            listener.TraceEvent(cache, "Source", TraceEventType.Critical, 1, "Format", "arg1");
            Assert.Equal(0, listener.WriteCount);

            listener = new TestTextTraceListener();
            listener.TraceEvent(cache, "Source", TraceEventType.Critical, 1, "Format", "arg1");
            var expected = 2; // header and message.
            Assert.Equal(expected, listener.WriteCount);
        }

        [Theory]
        [InlineData(TraceOptions.None, 0)]
        [InlineData(TraceOptions.Timestamp, 1)]
        [InlineData(TraceOptions.ProcessId | TraceOptions.ThreadId, 2)]
        [InlineData(TraceOptions.DateTime | TraceOptions.Timestamp, 2)]
        public void WriteFooterTest(TraceOptions opts, int flagCount)
        {
            var cache = new TraceEventCache();
            var listener = new TestTextTraceListener();
            listener.TraceOutputOptions = opts;
            listener.Filter = new TestTraceFilter(false);
            listener.TraceEvent(cache, "Source", TraceEventType.Critical, 1);
            Assert.Equal(0, listener.WriteCount);

            var baseExpected = 2; // header + message
            var expected = baseExpected;

            listener = new TestTextTraceListener();
            listener.TraceOutputOptions = opts;
            listener.TraceEvent(null, "Source", TraceEventType.Critical, 1);
            Assert.Equal(expected, listener.WriteCount);

            // Two calls to write per flag, one call for writing the indent, one for the message.
            expected = baseExpected + flagCount * 2;
            listener = new TestTextTraceListener();
            listener.TraceOutputOptions = opts;
            listener.TraceEvent(cache, "Source", TraceEventType.Critical, 1);
            Assert.Equal(expected, listener.WriteCount);
        }
    }
}
