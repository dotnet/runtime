// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Reflection;
using Xunit;

namespace System.Diagnostics.TraceSourceTests
{
    using Method = TestTraceListener.Method;

    public class TraceClassTests : IDisposable
    {
        private readonly string TestRunnerAssemblyName = PlatformDetection.IsFullFramework ? 
            Path.GetFileName(Environment.GetCommandLineArgs()[0]) : Assembly.GetEntryAssembly().GetName().Name;

        void IDisposable.Dispose()
        {
            TraceTestHelper.ResetState();
        }

        [Fact]
        public void AutoFlushTest()
        {
            Trace.AutoFlush = false;
            Assert.False(Trace.AutoFlush);
            Trace.AutoFlush = true;
            Assert.True(Trace.AutoFlush);
        }

        [Fact]
        public void UseGlobalLockTest()
        {
            Trace.UseGlobalLock = true;
            Assert.True(Trace.UseGlobalLock);
            Trace.UseGlobalLock = false;
            Assert.False(Trace.UseGlobalLock);
        }

        [Fact]
        public void RefreshTest()
        {
            Trace.UseGlobalLock = true;
            Assert.True(Trace.UseGlobalLock);
            // NOTE: Refresh does not reset to default config values
            Trace.Refresh();
            Assert.True(Trace.UseGlobalLock);
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(2, 2)]
        [InlineData(-2, 0)]
        public void IndentLevelTest(int indent, int expected)
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
        public void IndentSizeTest(int indent, int expected)
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
        public void FlushTest()
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
        public void CloseTest()
        {
            var listener = new TestTraceListener();
            Trace.Listeners.Add(listener);
            Trace.Close();
            Assert.Equal(1, listener.GetCallCount(Method.Close));
        }

        [Fact]
        public void Assert1Test()
        {
            var listener = new TestTraceListener();
            // We have to clear the listeners list on Trace since there is a trace listener by default with AssertUiEnabled = true in Desktop and that will pop up an assert window with Trace.Fail
            Trace.Listeners.Clear();
            Trace.Listeners.Add(listener);
            Trace.Assert(true);
            Assert.Equal(0, listener.GetCallCount(Method.WriteLine));
            Assert.Equal(0, listener.GetCallCount(Method.Fail));
            Trace.Assert(false);
            Assert.Equal(0, listener.GetCallCount(Method.WriteLine));
            Assert.Equal(1, listener.GetCallCount(Method.Fail));
        }

        [Fact]
        public void Assert2Test()
        {
            var listener = new TestTraceListener();
            var text = new TestTextTraceListener();
            // We have to clear the listeners list on Trace since there is a trace listener by default with AssertUiEnabled = true in Desktop and that will pop up an assert window with Trace.Fail
            Trace.Listeners.Clear();
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
        public void Assert3Test()
        {
            var listener = new TestTraceListener();
            var text = new TestTextTraceListener();
            // We have to clear the listeners list on Trace since there is a trace listener by default with AssertUiEnabled = true in Desktop and that will pop up an assert window with Trace.Fail
            Trace.Listeners.Clear();
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
        public void WriteTest()
        {
            var listener = new TestTextTraceListener();
            Trace.Listeners.Add(listener);
            Trace.Write("Message", "Category");
            Trace.Flush();
            Assert.Equal("Category: Message", listener.Output);
        }

        [Fact]
        public void WriteObject1Test()
        {
            var listener = new TestTextTraceListener();
            Trace.Listeners.Add(listener);
            Trace.Write((object)"Text");
            listener.Flush();
            Assert.Equal("Text", listener.Output);
        }

        [Fact]
        public void WriteObject2Test()
        {
            var listener = new TestTextTraceListener();
            Trace.Listeners.Add(listener);
            Trace.Write((object)"Message", "Category");
            Trace.Flush();
            Assert.Equal("Category: Message", listener.Output);
        }

        [Fact]
        public void WriteLineObjectTest()
        {
            var listener = new TestTextTraceListener();
            Trace.Listeners.Add(listener);
            Trace.WriteLine((object)"Text");
            listener.Flush();
            Assert.Equal("Text" + Environment.NewLine, listener.Output);
        }

        [Fact]
        public void WriteLine1Test()
        {
            var listener = new TestTextTraceListener();
            Trace.Listeners.Add(listener);
            Trace.WriteLine("Message", "Category");
            listener.Flush();
            Assert.Equal("Category: Message" + Environment.NewLine, listener.Output);
        }

        [Fact]
        public void WriteLine2Test()
        {
            var listener = new TestTextTraceListener();
            Trace.Listeners.Add(listener);
            Trace.WriteLine((object)"Message", "Category");
            listener.Flush();
            Assert.Equal("Category: Message" + Environment.NewLine, listener.Output);
        }

        [Fact]
        public void WriteIf1Test()
        {
            var listener = new TestTraceListener();
            Trace.Listeners.Add(listener);
            Trace.WriteIf(false, (object)"Message");
            Assert.Equal(0, listener.GetCallCount(Method.Write));
            Trace.WriteIf(true, (object)"Message");
            Assert.Equal(1, listener.GetCallCount(Method.Write));
        }

        [Fact]
        public void WriteIf2Test()
        {
            var listener = new TestTraceListener();
            Trace.Listeners.Add(listener);
            Trace.WriteIf(false, "Message");
            Assert.Equal(0, listener.GetCallCount(Method.Write));
            Trace.WriteIf(true, "Message");
            Assert.Equal(1, listener.GetCallCount(Method.Write));
        }

        [Fact]
        public void WriteIf3Test()
        {
            var listener = new TestTraceListener();
            Trace.Listeners.Add(listener);
            Trace.WriteIf(false, (object)"Message", "Category");
            Assert.Equal(0, listener.GetCallCount(Method.Write));
            Trace.WriteIf(true, (object)"Message", "Category");
            Assert.Equal(1, listener.GetCallCount(Method.Write));
        }

        [Fact]
        public void WriteIf4Test()
        {
            var listener = new TestTraceListener();
            Trace.Listeners.Add(listener);
            Trace.WriteIf(false, "Message", "Category");
            Assert.Equal(0, listener.GetCallCount(Method.Write));
            Trace.WriteIf(true, "Message", "Category");
            Assert.Equal(1, listener.GetCallCount(Method.Write));
        }

        [Fact]
        public void WriteLineIf1Test()
        {
            var listener = new TestTraceListener();
            Trace.Listeners.Add(listener);
            Trace.WriteLineIf(false, (object)"Message");
            Assert.Equal(0, listener.GetCallCount(Method.WriteLine));
            Trace.WriteLineIf(true, (object)"Message");
            Assert.Equal(1, listener.GetCallCount(Method.WriteLine));
        }

        [Fact]
        public void WriteLineIf2Test()
        {
            var listener = new TestTraceListener();
            Trace.Listeners.Add(listener);
            Trace.WriteLineIf(false, "Message");
            Assert.Equal(0, listener.GetCallCount(Method.WriteLine));
            Trace.WriteLineIf(true, "Message");
            Assert.Equal(1, listener.GetCallCount(Method.WriteLine));
        }

        [Fact]
        public void WriteLineIf3Test()
        {
            var listener = new TestTraceListener();
            Trace.Listeners.Add(listener);
            Trace.WriteLineIf(false, (object)"Message", "Category");
            Assert.Equal(0, listener.GetCallCount(Method.WriteLine));
            Trace.WriteLineIf(true, (object)"Message", "Category");
            Assert.Equal(1, listener.GetCallCount(Method.WriteLine));
        }

        [Fact]
        public void WriteLineIf4Test()
        {
            var listener = new TestTraceListener();
            Trace.Listeners.Add(listener);
            Trace.WriteLineIf(false, "Message", "Category");
            Assert.Equal(0, listener.GetCallCount(Method.WriteLine));
            Trace.WriteLineIf(true, "Message", "Category");
            Assert.Equal(1, listener.GetCallCount(Method.WriteLine));
        }

        [Fact]
        public void FailTest()
        {
            var listener = new TestTraceListener();
            // We have to clear the listeners list on Trace since there is a trace listener by default with AssertUiEnabled = true in Desktop and that will pop up an assert window with Trace.Fail
            Trace.Listeners.Clear();
            Trace.Listeners.Add(listener);
            Trace.Fail("Text");
            Assert.Equal(1, listener.GetCallCount(Method.Fail));
            Trace.Fail("Text", "Detail");
            Assert.Equal(2, listener.GetCallCount(Method.Fail));
        }

        [Fact]
        public void TraceTest01()
        {
            var textTL = new TestTextTraceListener();
            Trace.Listeners.Add(textTL);
            Trace.IndentLevel = 0;
            Trace.WriteLine("Message start.");
            Trace.IndentSize = 2;
            Trace.IndentLevel = 2;
            Trace.Write("This message should be indented.");
            Trace.TraceError("This error not be indented.");
            Trace.TraceError("{0}", "This error is indented");
            Trace.TraceWarning("This warning is indented");
            Trace.TraceWarning("{0}", "This warning is also indented");
            Trace.TraceInformation("This information in indented");
            Trace.TraceInformation("{0}", "This information is also indented");
            Trace.IndentSize = 0;
            Trace.IndentLevel = 0;
            Trace.WriteLine("Message end.");
            textTL.Flush();
            string newLine = Environment.NewLine;
            var expected =
                string.Format(
                    "Message start." + newLine + "    This message should be indented.{0} Error: 0 : This error not be indented." + newLine + "    {0} Error: 0 : This error is indented" + newLine + "    {0} Warning: 0 : This warning is indented" + newLine + "    {0} Warning: 0 : This warning is also indented" + newLine + "    {0} Information: 0 : This information in indented" + newLine + "    {0} Information: 0 : This information is also indented" + newLine + "Message end." + newLine + "",
                    TestRunnerAssemblyName
                );

            Assert.Equal(expected, textTL.Output);
        }

        [Fact]
        public void TraceTest02()
        {
            string newLine = Environment.NewLine;
            var textTL = new TestTextTraceListener();
            Trace.Listeners.Clear();
            Trace.Listeners.Add(textTL);
            Trace.IndentLevel = 0;
            Trace.Fail("");
            textTL.Flush();
            var fail = textTL.Output.TrimEnd(newLine.ToCharArray());

            textTL = new TestTextTraceListener();
            // We have to clear the listeners list on Trace since there is a trace listener by default with AssertUiEnabled = true in Desktop and that will pop up an assert window with Trace.Fail
            Trace.Listeners.Clear();
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
            newLine = Environment.NewLine;
            var expected = "Message start." + newLine + "    This message should be indented.This should not be indented." + newLine + "      " + fail + "This failure is reported with a detailed message" + newLine + "      " + fail + newLine + "      " + fail + "This assert is reported" + newLine + "Message end." + newLine;
            Assert.Equal(expected, textTL.Output);
        }
    }
}
