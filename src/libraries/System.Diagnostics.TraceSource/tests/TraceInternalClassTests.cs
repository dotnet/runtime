// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.Diagnostics.TraceSourceTests
{
    using Method = TestTraceListener.Method;

    public sealed class TraceTests
    {
        [Fact]
        public void Unindent()
        {
            Trace.IndentLevel = 1;
            Trace.Unindent();
            Assert.Equal(0, Trace.IndentLevel);
            Trace.Unindent();
            Assert.Equal(0, Trace.IndentLevel);
        }
    }

    public sealed class TraceTests_Default : TraceTestsBase
    {
        // default mode: GlobalLock = true, AutoFlush = false, ThreadSafeListener = false
    }

    public sealed class TraceTests_AutoFlush : TraceTestsBase
    {
        internal override bool AutoFlush
        {
            get { return true; }
        }
    }

    public sealed class TraceTests_NoGlobalLock : TraceTestsBase
    {
        internal override bool UseGlobalLock
        {
            get { return false; }
        }
    }

    public sealed class TraceTests_NoGlobalLock_AutoFlush : TraceTestsBase
    {
        internal override bool UseGlobalLock
        {
            get { return false; }
        }

        internal override bool AutoFlush
        {
            get { return true; }
        }
    }

    public sealed class TraceTests_ThreadSafeListener : TraceTestsBase
    {
        internal override bool ThreadSafeListener
        {
            get { return true; }
        }
    }

    public sealed class TraceTests_ThreadSafeListener_AutoFlush : TraceTestsBase
    {
        internal override bool ThreadSafeListener
        {
            get { return true; }
        }

        internal override bool AutoFlush
        {
            get { return true; }
        }
    }

    // Defines abstract tests that will be executed in different modes via the above concrete classes.
    public abstract class TraceTestsBase
    {
        public TraceTestsBase()
        {
            TraceTestHelper.ResetState();
            Trace.AutoFlush = AutoFlush;
            Trace.UseGlobalLock = UseGlobalLock;
        }

        // properties are overridden to define different "modes" of execution
        internal virtual bool UseGlobalLock
        {
            get
            {
                // ThreadSafeListener is only meaningful when not using a global lock, 
                // so UseGlobalLock will be auto-disabled in that mode.
                return true && !ThreadSafeListener;
            }
        }

        internal virtual bool AutoFlush
        {
            get { return false; }
        }

        internal virtual bool ThreadSafeListener
        {
            get { return false; }
        }

        private TestTraceListener GetTraceListener()
        {
            return new TestTraceListener(ThreadSafeListener);
        }

        [Fact]
        public void FlushTest()
        {
            var listener = GetTraceListener();
            Trace.Listeners.Add(listener);
            Trace.Flush();
            Assert.Equal(1, listener.GetCallCount(Method.Flush));
        }

        [Fact]
        public void TraceEvent1Test()
        {
            var listener = GetTraceListener();
            Trace.Listeners.Add(listener);
            Trace.TraceError("Message");
            Assert.Equal(1, listener.GetCallCount(Method.TraceEvent));
        }

        [Fact]
        public void TraceEvent2Test()
        {
            var listener = GetTraceListener();
            Trace.Listeners.Add(listener);
            Trace.TraceError("Message", "Arg1", "Arg2");
            Assert.Equal(1, listener.GetCallCount(Method.TraceEvent));
            var flushExpected = AutoFlush ? 1 : 0;
            Assert.Equal(flushExpected, listener.GetCallCount(Method.Flush));
        }

        [Fact]
        public void WriteObjectTest()
        {
            var listener = GetTraceListener();
            Trace.Listeners.Add(listener);
            Trace.Write((object)"Message");
            Assert.Equal(1, listener.GetCallCount(Method.Write));
            var flushExpected = AutoFlush ? 1 : 0;
            Assert.Equal(flushExpected, listener.GetCallCount(Method.Flush));
        }

        [Fact]
        public void WriteTest()
        {
            var listener = GetTraceListener();
            Trace.Listeners.Add(listener);
            Trace.Write("Message");
            Assert.Equal(1, listener.GetCallCount(Method.Write));
            var flushExpected = AutoFlush ? 1 : 0;
            Assert.Equal(flushExpected, listener.GetCallCount(Method.Flush));
        }

        [Fact]
        public void Write2Test()
        {
            var listener = GetTraceListener();
            Trace.Listeners.Add(listener);
            Trace.Write("Message", "Category");
            Assert.Equal(1, listener.GetCallCount(Method.Write));
            var flushExpected = AutoFlush ? 1 : 0;
            Assert.Equal(flushExpected, listener.GetCallCount(Method.Flush));
        }

        [Fact]
        public void WriteObject2Test()
        {
            var listener = GetTraceListener();
            Trace.Listeners.Add(listener);
            Trace.Write((Object)"Message", "Category");
            Assert.Equal(1, listener.GetCallCount(Method.Write));
            var flushExpected = AutoFlush ? 1 : 0;
            Assert.Equal(flushExpected, listener.GetCallCount(Method.Flush));
        }

        [Fact]
        public void WriteLineTest()
        {
            var listener = GetTraceListener();
            Trace.Listeners.Add(listener);
            Trace.WriteLine("Message");
            Assert.Equal(1, listener.GetCallCount(Method.WriteLine));
            var flushExpected = AutoFlush ? 1 : 0;
            Assert.Equal(flushExpected, listener.GetCallCount(Method.Flush));
        }

        [Fact]
        public void WriteLineObjectTest()
        {
            var listener = GetTraceListener();
            Trace.Listeners.Add(listener);
            Trace.WriteLine((Object)"Message");
            Assert.Equal(1, listener.GetCallCount(Method.WriteLine));
            var flushExpected = AutoFlush ? 1 : 0;
            Assert.Equal(flushExpected, listener.GetCallCount(Method.Flush));
        }

        [Fact]
        public void WriteLine2Test()
        {
            var listener = GetTraceListener();
            Trace.Listeners.Add(listener);
            Trace.WriteLine("Message", "Category");
            Assert.Equal(1, listener.GetCallCount(Method.WriteLine));
            var flushExpected = AutoFlush ? 1 : 0;
            Assert.Equal(flushExpected, listener.GetCallCount(Method.Flush));
        }

        [Fact]
        public void WriteLineObject2Test()
        {
            var listener = GetTraceListener();
            Trace.Listeners.Add(listener);
            Trace.WriteLine((Object)"Message", "Category");
            Assert.Equal(1, listener.GetCallCount(Method.WriteLine));
            var flushExpected = AutoFlush ? 1 : 0;
            Assert.Equal(flushExpected, listener.GetCallCount(Method.Flush));
        }

        [Fact]
        public void FailTest()
        {
            var listener = GetTraceListener();
            Trace.Listeners.Add(listener);
            Trace.Fail("Message");
            Assert.Equal(1, listener.GetCallCount(Method.Fail));
            var flushExpected = AutoFlush ? 1 : 0;
            Assert.Equal(flushExpected, listener.GetCallCount(Method.Flush));
        }

        [Fact]
        public void Fail2Test()
        {
            var listener = GetTraceListener();
            Trace.Listeners.Add(listener);
            Trace.Fail("Message", "Category");
            Assert.Equal(1, listener.GetCallCount(Method.Fail));
            var flushExpected = AutoFlush ? 1 : 0;
            Assert.Equal(flushExpected, listener.GetCallCount(Method.Flush));
        }
    }
}
