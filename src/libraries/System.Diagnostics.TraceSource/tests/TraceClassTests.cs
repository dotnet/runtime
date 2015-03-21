// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.Diagnostics.TraceSourceTests
{
    using Method = TestTraceListener.Method;

    public class TraceClassTests
    {
        public TraceClassTests()
        {
            // restore Trace static state
            Trace.IndentLevel = 0;
            Trace.IndentSize = DiagnosticsConfiguration.IndentSize;
            Trace.UseGlobalLock = DiagnosticsConfiguration.UseGlobalLock;
            Trace.AutoFlush = DiagnosticsConfiguration.AutoFlush;
            Trace.Listeners.Clear();
            Trace.Listeners.Add(new DefaultTraceListener());
        }

        [Fact]
        public void AutoFlush()
        {
            Trace.AutoFlush = false;
            Assert.False(Trace.AutoFlush);
            Trace.AutoFlush = true;
            Assert.True(Trace.AutoFlush);
        }

        [Fact]
        public void UseGlobalLock()
        {
            Trace.UseGlobalLock = true;
            Assert.True(Trace.UseGlobalLock);
            Trace.UseGlobalLock = false;
            Assert.False(Trace.UseGlobalLock);
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(2, 2)]
        [InlineData(-2, 0)]
        public void IndentLevel(int indent, int expected)
        {
            Trace.Listeners.Clear();
            Trace.Listeners.Add(new DefaultTraceListener());
            Trace.IndentLevel = indent;
            Assert.Equal(expected, Trace.IndentLevel);
            foreach (TraceListener listener in Trace.Listeners)
            {
                Assert.Equal(expected, listener.IndentLevel);
            }
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(2, 2)]
        [InlineData(-2, 0)]
        public void IndentSize(int indent, int expected)
        {
            Trace.Listeners.Clear();
            Trace.Listeners.Add(new DefaultTraceListener());
            Trace.IndentSize = indent;
            Assert.Equal(expected, Trace.IndentSize);
            foreach (TraceListener listener in Trace.Listeners)
            {
                Assert.Equal(expected, listener.IndentSize);
            }
        }

        [Fact]
        public void Flush()
        {
            var listener = new TestTextTraceListener();
            Trace.Listeners.Clear();
            Trace.AutoFlush = false;
            Trace.Listeners.Add(listener);
            Trace.Write("Test");
            Assert.DoesNotContain("Test", listener.Output);
            Trace.Flush();
            Assert.Contains("Test", listener.Output);
        }

        [Fact]
        public void Close()
        {
            var listener = new TestTraceListener();
            Trace.Listeners.Add(listener);
            Trace.Close();
            Assert.Equal(1, listener.GetCallCount(Method.Dispose));
        }

        [Fact]
        public void Assert1()
        {
            var listener = new TestTraceListener();
            Trace.Listeners.Add(listener);
            Trace.Assert(true);
            Assert.Equal(0, listener.GetCallCount(Method.WriteLine));
            Assert.Equal(0, listener.GetCallCount(Method.Fail));
            Trace.Assert(false);
            Assert.Equal(0, listener.GetCallCount(Method.WriteLine));
            Assert.Equal(1, listener.GetCallCount(Method.Fail));
        }

        [Fact]
        public void Assert2()
        {
            var listener = new TestTraceListener();
            var text = new TestTextTraceListener();
            Trace.Listeners.Add(listener);
            Trace.Listeners.Add(text);
            Trace.Assert(true, "Message");
            Assert.Equal(0, listener.GetCallCount(Method.WriteLine));
            Assert.Equal(0, listener.GetCallCount(Method.Fail));
            text.Flush();
            Assert.DoesNotContain("Message", text.Output);
            Trace.Assert(false, "Message");
            Assert.Equal(0, listener.GetCallCount(Method.WriteLine));
            Assert.Equal(1, listener.GetCallCount(Method.Fail));
            text.Flush();
            Assert.Contains("Message", text.Output);
        }

        [Fact]
        public void Assert3()
        {
            var listener = new TestTraceListener();
            var text = new TestTextTraceListener();
            Trace.Listeners.Add(listener);
            Trace.Listeners.Add(text);
            Trace.Assert(true, "Message", "Detail");
            Assert.Equal(0, listener.GetCallCount(Method.WriteLine));
            Assert.Equal(0, listener.GetCallCount(Method.Fail));
            text.Flush();
            Assert.DoesNotContain("Message", text.Output);
            Trace.Assert(false, "Message", "Detail");
            Assert.Equal(0, listener.GetCallCount(Method.WriteLine));
            Assert.Equal(1, listener.GetCallCount(Method.Fail));
            text.Flush();
            Assert.Contains("Message", text.Output);
            Assert.Contains("Detail", text.Output);
        }


        [Fact]
        public void Write()
        {
            var listener = new TestTextTraceListener();
            Trace.Listeners.Add(listener);
            Trace.Write("Message", "Category");
            Trace.Flush();
            Assert.Equal("Category: Message", listener.Output);
        }

        [Fact]
        public void WriteObject1()
        {
            var listener = new TestTextTraceListener();
            Trace.Listeners.Add(listener);
            Trace.Write((Object)"Text");
            listener.Flush();
            Assert.Equal("Text", listener.Output);
        }

        [Fact]
        public void WriteObject2()
        {
            var listener = new TestTextTraceListener();
            Trace.Listeners.Add(listener);
            Trace.Write((Object)"Message", "Category");
            Trace.Flush();
            Assert.Equal("Category: Message", listener.Output);
        }

        [Fact]
        public void WriteLineObject()
        {
            var listener = new TestTextTraceListener();
            Trace.Listeners.Add(listener);
            Trace.WriteLine((Object)"Text");
            listener.Flush();
            Assert.Equal("Text\r\n", listener.Output);
        }

        [Fact]
        public void WriteLine1()
        {
            var listener = new TestTextTraceListener();
            Trace.Listeners.Add(listener);
            Trace.WriteLine("Message", "Category");
            listener.Flush();
            Assert.Equal("Category: Message\r\n", listener.Output);
        }

        [Fact]
        public void WriteLine2()
        {
            var listener = new TestTextTraceListener();
            Trace.Listeners.Add(listener);
            Trace.WriteLine((Object)"Message", "Category");
            listener.Flush();
            Assert.Equal("Category: Message\r\n", listener.Output);
        }

        [Fact]
        public void WriteIf1()
        {
            var listener = new TestTraceListener();
            Trace.Listeners.Add(listener);
            Trace.WriteIf(false, (Object)"Message");
            Assert.Equal(0, listener.GetCallCount(Method.Write));
            Trace.WriteIf(true, (Object)"Message");
            Assert.Equal(1, listener.GetCallCount(Method.Write));
        }

        [Fact]
        public void WriteIf2()
        {
            var listener = new TestTraceListener();
            Trace.Listeners.Add(listener);
            Trace.WriteIf(false, "Message");
            Assert.Equal(0, listener.GetCallCount(Method.Write));
            Trace.WriteIf(true, "Message");
            Assert.Equal(1, listener.GetCallCount(Method.Write));
        }

        [Fact]
        public void WriteIf3()
        {
            var listener = new TestTraceListener();
            Trace.Listeners.Add(listener);
            Trace.WriteIf(false, (Object)"Message", "Category");
            Assert.Equal(0, listener.GetCallCount(Method.Write));
            Trace.WriteIf(true, (Object)"Message", "Category");
            Assert.Equal(1, listener.GetCallCount(Method.Write));
        }

        [Fact]
        public void WriteIf4()
        {
            var listener = new TestTraceListener();
            Trace.Listeners.Add(listener);
            Trace.WriteIf(false, "Message", "Category");
            Assert.Equal(0, listener.GetCallCount(Method.Write));
            Trace.WriteIf(true, "Message", "Category");
            Assert.Equal(1, listener.GetCallCount(Method.Write));
        }


        [Fact]
        public void WriteLineIf1()
        {
            var listener = new TestTraceListener();
            Trace.Listeners.Add(listener);
            Trace.WriteLineIf(false, (Object)"Message");
            Assert.Equal(0, listener.GetCallCount(Method.WriteLine));
            Trace.WriteLineIf(true, (Object)"Message");
            Assert.Equal(1, listener.GetCallCount(Method.WriteLine));
        }

        [Fact]
        public void WriteLineIf2()
        {
            var listener = new TestTraceListener();
            Trace.Listeners.Add(listener);
            Trace.WriteLineIf(false, "Message");
            Assert.Equal(0, listener.GetCallCount(Method.WriteLine));
            Trace.WriteLineIf(true, "Message");
            Assert.Equal(1, listener.GetCallCount(Method.WriteLine));
        }

        [Fact]
        public void WriteLineIf3()
        {
            var listener = new TestTraceListener();
            Trace.Listeners.Add(listener);
            Trace.WriteLineIf(false, (Object)"Message", "Category");
            Assert.Equal(0, listener.GetCallCount(Method.WriteLine));
            Trace.WriteLineIf(true, (Object)"Message", "Category");
            Assert.Equal(1, listener.GetCallCount(Method.WriteLine));
        }

        [Fact]
        public void WriteLineIf4()
        {
            var listener = new TestTraceListener();
            Trace.Listeners.Add(listener);
            Trace.WriteLineIf(false, "Message", "Category");
            Assert.Equal(0, listener.GetCallCount(Method.WriteLine));
            Trace.WriteLineIf(true, "Message", "Category");
            Assert.Equal(1, listener.GetCallCount(Method.WriteLine));
        }

        [Fact]
        public void Fail()
        {
            var listener = new TestTraceListener();
            Trace.Listeners.Add(listener);
            Trace.Fail("Text");
            Assert.Equal(1, listener.GetCallCount(Method.Fail));
            Trace.Fail("Text", "Detail");
            Assert.Equal(2, listener.GetCallCount(Method.Fail));
        }

        [Fact]
        public void TraceTest01()
        {
            Refresh();
            try
            {
                var textTL = new TestTextTraceListener();
                Trace.Listeners.Add(textTL);
                Trace.IndentLevel = 0;
                Trace.WriteLine("Message start.");
                Trace.IndentSize = 2;
                Trace.IndentLevel = 2;
                Trace.Write("This message should be indented.");
                Trace.TraceError("This error not be indented.");
                Trace.TraceError("{0}", "This error is indendented");
                Trace.TraceWarning("This warning is indented");
                Trace.TraceWarning("{0}", "This warning is also indented");
                Trace.TraceInformation("This information in indented");
                Trace.TraceInformation("{0}", "This information is also indented");
                Trace.IndentSize = 0;
                Trace.IndentLevel = 0;
                Trace.WriteLine("Message end.");
                textTL.Flush();
                var expected =
                    String.Format(
                        "Message start.\r\n    This message should be indented.{0} Error: 0 : This error not be indented.\r\n    {0} Error: 0 : This error is indendented\r\n    {0} Warning: 0 : This warning is indented\r\n    {0} Warning: 0 : This warning is also indented\r\n    {0} Information: 0 : This information in indented\r\n    {0} Information: 0 : This information is also indented\r\nMessage end.\r\n",
                        "DEFAULT_APPNAME" //DEFAULT_APPNAME this a bug which needs to be fixed.
                    );

                Assert.Equal(expected, textTL.Output);
            }
            finally
            {
                Refresh();
            }
        }

        [Fact]
        public void TraceTest02()
        {
            //Check the same using Indent() and Unindent() message.
            Refresh();
            try
            {
                var textTL = new TestTextTraceListener();
                Trace.Listeners.Add(textTL);
                Trace.IndentLevel = 0;
                Trace.IndentSize = 2;
                Trace.WriteLineIf(true, "Message start.");
                Trace.Indent();
                Trace.Indent();
                Trace.WriteIf(true, "This message should be indented.");
                Trace.WriteIf(false, "This message should be ignored.");
                Trace.Indent();
                Trace.WriteLine("This should not be indented.");
                Trace.WriteLineIf(false, "This message will be ignored");
                Trace.Fail("This failure is reported", "with a detailed message");
                Trace.Assert(false);
                Trace.Assert(false, "This assert is reported");
                Trace.Assert(true, "This assert is not reported");
                Trace.Unindent();
                Trace.Unindent();
                Trace.Unindent();
                Trace.WriteLine("Message end.");
                textTL.Flush();
                var expected = "Message start.\r\n    This message should be indented.This should not be indented.\r\n      Fail: This failure is reported with a detailed message\r\n      Fail: \r\n      Fail: This assert is reported\r\nMessage end.\r\n";
                Assert.Equal(expected, textTL.Output);
            }
            finally
            {
                Refresh();
            }
        }

        private static void Refresh()
        {
            Trace.Refresh();
            Trace.Listeners.RemoveAt(0);
        }
    }
}
