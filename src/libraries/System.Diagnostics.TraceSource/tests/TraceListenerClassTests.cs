// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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

        [Theory]
        [InlineData(TraceOptions.Callstack)]
        [InlineData(TraceOptions.DateTime)]
        [InlineData(TraceOptions.LogicalOperationStack)]
        [InlineData(TraceOptions.None)]
        [InlineData(TraceOptions.ProcessId)]
        [InlineData(TraceOptions.ThreadId)]
        [InlineData(TraceOptions.Timestamp)]
        public void TraceOutputOptionsTest_Valid(TraceOptions options)
        {
            var listener = new TestTraceListener();
            listener.TraceOutputOptions = options;
            Assert.Equal(options, listener.TraceOutputOptions);
        }

        [Theory]
        [InlineData((TraceOptions)0x80)]
        public void TraceOutputOptionsTest_Invalid(TraceOptions options)
        {
            var listener = new TestTraceListener();
            Assert.Throws<ArgumentOutOfRangeException>(() => listener.TraceOutputOptions = options);
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
        [InlineData(TraceOptions.DateTime | TraceOptions.Timestamp | TraceOptions.Callstack, 3)]
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

        [Fact]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void WriteFooterTest_Callstack()
        {
            var cache = new TraceEventCache();
            var listener = new TestTextTraceListener() { TraceOutputOptions = TraceOptions.Callstack };
            listener.TraceEvent(cache, "Source", TraceEventType.Critical, 42);
            listener.Flush();

            Assert.Contains("Callstack=", listener.Output);
            Assert.Contains(nameof(WriteFooterTest_Callstack), listener.Output);
        }

        [Fact]
        public void WriteFooterTest_DateTime()
        {
            var cache = new TraceEventCache();
            var listener = new TestTextTraceListener() { TraceOutputOptions = TraceOptions.DateTime };
            listener.TraceEvent(cache, "Source", TraceEventType.Critical, 42);
            listener.Flush();

            Assert.Contains("DateTime=", listener.Output);
            Assert.Contains(cache.DateTime.ToString("o"), listener.Output);
        }

        [Fact]
        public void WriteFooterTest_LogicalOperationStack()
        {
            for (int i = 0; i <= 3; i++)
            {
                int[] items = Enumerable.Range(42, i).ToArray();

                foreach (int item in items)
                {
                    Trace.CorrelationManager.LogicalOperationStack.Push(item.ToString());
                }

                try
                {
                    var cache = new TraceEventCache();
                    var listener = new TestTextTraceListener() { TraceOutputOptions = TraceOptions.LogicalOperationStack };
                    listener.TraceEvent(cache, "Source", TraceEventType.Critical, 42);
                    listener.Flush();

                    Assert.Contains("LogicalOperationStack=", listener.Output);
                    Assert.Contains(string.Join(", ", items.Reverse()), listener.Output);
                }
                finally
                {
                    foreach (int _ in items)
                    {
                        Trace.CorrelationManager.LogicalOperationStack.Pop();
                    }
                }
            }
        }

        [Fact]
        public void WriteFooterTest_ProcessId()
        {
            var cache = new TraceEventCache();
            var listener = new TestTextTraceListener() { TraceOutputOptions = TraceOptions.ProcessId };
            listener.TraceOutputOptions = TraceOptions.ProcessId;
            listener.TraceEvent(cache, "Source", TraceEventType.Critical, 42);
            listener.Flush();

            Assert.Contains("ProcessId=", listener.Output);
            Assert.Contains(cache.ProcessId.ToString(), listener.Output);
        }

        [Fact]
        public void WriteFooterTest_ThreadId()
        {
            var cache = new TraceEventCache();
            var listener = new TestTextTraceListener() { TraceOutputOptions = TraceOptions.ThreadId };
            listener.TraceEvent(cache, "Source", TraceEventType.Critical, 42);
            listener.Flush();

            Assert.Contains("ThreadId=", listener.Output);
            Assert.Contains(cache.ThreadId.ToString(), listener.Output);
        }

        [Fact]
        public void WriteFooterTest_Timestamp()
        {
            var cache = new TraceEventCache();
            var listener = new TestTextTraceListener() { TraceOutputOptions = TraceOptions.Timestamp };
            listener.TraceEvent(cache, "Source", TraceEventType.Critical, 42);
            listener.Flush();

            Assert.Contains("Timestamp=", listener.Output);
            Assert.Contains(cache.Timestamp.ToString(), listener.Output);
        }
    }
}
