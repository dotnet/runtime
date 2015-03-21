// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.Diagnostics.TraceSourceTests
{
    using Method = TestTraceListener.Method;

    public sealed class TraceInternalTests
    {
        [Fact]
        public void Unindent()
        {
            TraceInternal.IndentLevel = 1;
            TraceInternal.Unindent();
            Assert.Equal(0, Trace.IndentLevel);
            TraceInternal.Unindent();
            Assert.Equal(0, Trace.IndentLevel);
        }
    }

    public sealed class TraceInternalTests_Default : TraceInternalTestsBase
    {
        // default mode: GlobalLock = true, AutoFlush = false, ThreadSafeListener = false
    }

    public sealed class TraceInternalTests_AutoFlush : TraceInternalTestsBase
    {
        internal override bool AutoFlush
        {
            get { return true; }
        }
    }

    public sealed class TraceInternalTests_NoGlobalLock : TraceInternalTestsBase
    {
        internal override bool UseGlobalLock
        {
            get { return false; }
        }
    }

    public sealed class TraceInternalTests_NoGlobalLock_AutoFlush : TraceInternalTestsBase
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

    public sealed class TraceInternalTests_ThreadSafeListener : TraceInternalTestsBase
    {
        internal override bool ThreadSafeListener
        {
            get { return true; }
        }
    }

    public sealed class TraceInternalTests_ThreadSafeListener_AutoFlush : TraceInternalTestsBase
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
    public abstract class TraceInternalTestsBase
    {
        public TraceInternalTestsBase()
        {
            TraceInternal.Listeners.Clear();
            TraceInternal.UseGlobalLock = UseGlobalLock;
            TraceInternal.AutoFlush = AutoFlush;
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
        public void Flush()
        {
            var listener = GetTraceListener();
            TraceInternal.Listeners.Add(listener);
            TraceInternal.Flush();
            Assert.Equal(1, listener.GetCallCount(Method.Flush));
        }

        [Fact]
        public void TraceEvent1()
        {
            var listener = GetTraceListener();
            TraceInternal.Listeners.Add(listener);
            TraceInternal.TraceEvent(TraceEventType.Verbose, 0, "Format", new Object[] { "Arg1" });
            Assert.Equal(1, listener.GetCallCount(Method.TraceEvent));
        }

        [Fact]
        public void TraceEvent2()
        {
            var listener = GetTraceListener();
            TraceInternal.Listeners.Add(listener);
            TraceInternal.TraceEvent(TraceEventType.Verbose, 0, "Message");
            Assert.Equal(1, listener.GetCallCount(Method.TraceEvent));
            var flushExpected = AutoFlush ? 1 : 0;
            Assert.Equal(flushExpected, listener.GetCallCount(Method.Flush));
        }

        [Fact]
        public void TraceEvent3()
        {
            var listener = GetTraceListener();
            TraceInternal.Listeners.Add(listener);
            TraceInternal.TraceEvent(TraceEventType.Verbose, 0, "Format", "Arg1", "Arg2");
            Assert.Equal(1, listener.GetCallCount(Method.TraceEvent));
            var flushExpected = AutoFlush ? 1 : 0;
            Assert.Equal(flushExpected, listener.GetCallCount(Method.Flush));

            TraceInternal.TraceEvent(TraceEventType.Verbose, 0, "Format", (Object[])null);
            Assert.Equal(2, listener.GetCallCount(Method.TraceEvent));
            flushExpected = AutoFlush ? 2 : 0;
            Assert.Equal(flushExpected, listener.GetCallCount(Method.Flush));
        }

        [Fact]
        public void WriteObject()
        {
            var listener = GetTraceListener();
            TraceInternal.Listeners.Add(listener);
            TraceInternal.Write((object)"Message");
            Assert.Equal(1, listener.GetCallCount(Method.Write));
            var flushExpected = AutoFlush ? 1 : 0;
            Assert.Equal(flushExpected, listener.GetCallCount(Method.Flush));
        }

        [Fact]
        public void Write()
        {
            var listener = GetTraceListener();
            TraceInternal.Listeners.Add(listener);
            TraceInternal.Write("Message");
            Assert.Equal(1, listener.GetCallCount(Method.Write));
            var flushExpected = AutoFlush ? 1 : 0;
            Assert.Equal(flushExpected, listener.GetCallCount(Method.Flush));
        }

        [Fact]
        public void Write2()
        {
            var listener = GetTraceListener();
            TraceInternal.Listeners.Add(listener);
            TraceInternal.Write("Message", "Category");
            Assert.Equal(1, listener.GetCallCount(Method.Write));
            var flushExpected = AutoFlush ? 1 : 0;
            Assert.Equal(flushExpected, listener.GetCallCount(Method.Flush));
        }

        [Fact]
        public void WriteObject2()
        {
            var listener = GetTraceListener();
            TraceInternal.Listeners.Add(listener);
            TraceInternal.Write((Object)"Message", "Category");
            Assert.Equal(1, listener.GetCallCount(Method.Write));
            var flushExpected = AutoFlush ? 1 : 0;
            Assert.Equal(flushExpected, listener.GetCallCount(Method.Flush));
        }

        [Fact]
        public void WriteLine()
        {
            var listener = GetTraceListener();
            TraceInternal.Listeners.Add(listener);
            TraceInternal.WriteLine("Message");
            Assert.Equal(1, listener.GetCallCount(Method.WriteLine));
            var flushExpected = AutoFlush ? 1 : 0;
            Assert.Equal(flushExpected, listener.GetCallCount(Method.Flush));
        }

        [Fact]
        public void WriteLineObject()
        {
            var listener = GetTraceListener();
            TraceInternal.Listeners.Add(listener);
            TraceInternal.WriteLine((Object)"Message");
            Assert.Equal(1, listener.GetCallCount(Method.WriteLine));
            var flushExpected = AutoFlush ? 1 : 0;
            Assert.Equal(flushExpected, listener.GetCallCount(Method.Flush));
        }

        [Fact]
        public void WriteLine2()
        {
            var listener = GetTraceListener();
            TraceInternal.Listeners.Add(listener);
            TraceInternal.WriteLine("Message", "Category");
            Assert.Equal(1, listener.GetCallCount(Method.WriteLine));
            var flushExpected = AutoFlush ? 1 : 0;
            Assert.Equal(flushExpected, listener.GetCallCount(Method.Flush));
        }

        [Fact]
        public void WriteLineObject2()
        {
            var listener = GetTraceListener();
            TraceInternal.Listeners.Add(listener);
            TraceInternal.WriteLine((Object)"Message", "Category");
            Assert.Equal(1, listener.GetCallCount(Method.WriteLine));
            var flushExpected = AutoFlush ? 1 : 0;
            Assert.Equal(flushExpected, listener.GetCallCount(Method.Flush));
        }

        [Fact]
        public void Fail()
        {
            var listener = GetTraceListener();
            TraceInternal.Listeners.Add(listener);
            TraceInternal.Fail("Message");
            Assert.Equal(1, listener.GetCallCount(Method.Fail));
            var flushExpected = AutoFlush ? 1 : 0;
            Assert.Equal(flushExpected, listener.GetCallCount(Method.Flush));
        }

        [Fact]
        public void Fail2()
        {
            var listener = GetTraceListener();
            TraceInternal.Listeners.Add(listener);
            TraceInternal.Fail("Message", "Category");
            Assert.Equal(1, listener.GetCallCount(Method.Fail));
            var flushExpected = AutoFlush ? 1 : 0;
            Assert.Equal(flushExpected, listener.GetCallCount(Method.Flush));
        }
    }
}
