// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.Diagnostics.TraceSourceTests
{
    using Method = TestTraceListener.Method;

    public sealed class TraceSourceClassTests
    {
        [Fact]
        public void ConstrutorArguments()
        {
            Assert.Throws<ArgumentNullException>(() => new TraceSource(null));
            Assert.Throws<ArgumentException>(() => new TraceSource(""));
        }

        [Fact]
        public void DefaultListener()
        {
            var trace = new TraceSource("TestTraceSource");
            Assert.Equal(1, trace.Listeners.Count);
            Assert.IsType<DefaultTraceListener>(trace.Listeners[0]);
        }

        [Fact]
        public void SetSourceSwitchException()
        {
            var trace = new TraceSource("TestTraceSource");
            Assert.Throws<ArgumentNullException>(() => trace.Switch = null);
        }

        [Fact]
        public void SetSourceSwitch()
        {
            var trace = new TraceSource("TestTraceSource");
            var @switch = new SourceSwitch("TestTraceSwitch");
            trace.Switch = @switch;
            Assert.Equal(@switch, trace.Switch);
        }

        [Fact]
        public void DefaultLevel()
        {
            var trace = new TraceSource("TestTraceSource");
            Assert.Equal(SourceLevels.Off, trace.Switch.Level);
        }

        [Fact]
        public void Close()
        {
            var trace = new TraceSource("T1", SourceLevels.All);
            trace.Listeners.Clear();

            var listener = new TestTraceListener();
            trace.Listeners.Add(listener);
            trace.Close();
            // NOTE: this assertion fails on .net 4.5
            // where TraceSource.Close calls TraceListener.Close, not Dispose.
            Assert.Equal(1, listener.GetCallCount(Method.Dispose));
            // Assert that writing to a closed TraceSource is not an error.
            trace.TraceEvent(TraceEventType.Critical, 0);
        }

        [Fact]
        public void Prune()
        {
            var strongTrace = new TraceSource("TestTraceSource");
            var traceRef = new WeakReference(new TraceSource("TestTraceSource"));
            Assert.True(traceRef.IsAlive);
            GC.Collect(2);
            TraceSource.RefreshAll();
            Assert.False(traceRef.IsAlive);
        }

        [Fact]
        public void TraceInformation()
        {
            var trace = new TraceSource("TestTraceSource", SourceLevels.All);
            var listener = new TestTraceListener();
            trace.Listeners.Add(listener);
            trace.TraceInformation("message");
            Assert.Equal(0, listener.GetCallCount(Method.TraceData));
            Assert.Equal(0, listener.GetCallCount(Method.Write));
            Assert.Equal(1, listener.GetCallCount(Method.TraceEvent));
            trace.TraceInformation("format", "arg1", "arg2");
            Assert.Equal(2, listener.GetCallCount(Method.TraceEvent));
        }

        [Theory]
        [InlineData(SourceLevels.Off, TraceEventType.Critical, 0)]
        [InlineData(SourceLevels.Critical, TraceEventType.Critical, 1)]
        [InlineData(SourceLevels.Critical, TraceEventType.Error, 0)]
        [InlineData(SourceLevels.Error, TraceEventType.Error, 1)]
        [InlineData(SourceLevels.Error, TraceEventType.Warning, 0)]
        [InlineData(SourceLevels.Warning, TraceEventType.Warning, 1)]
        [InlineData(SourceLevels.Warning, TraceEventType.Information, 0)]
        [InlineData(SourceLevels.Information, TraceEventType.Information, 1)]
        [InlineData(SourceLevels.Information, TraceEventType.Verbose, 0)]
        [InlineData(SourceLevels.Verbose, TraceEventType.Verbose, 1)]
        [InlineData(SourceLevels.All, TraceEventType.Critical, 1)]
        [InlineData(SourceLevels.All, TraceEventType.Verbose, 1)]
        // NOTE: tests to cover a TraceEventType value that is not in CoreFX (0x20 == TraceEventType.Start in 4.5)
        [InlineData(SourceLevels.Verbose, (TraceEventType)0x20, 0)]
        [InlineData(SourceLevels.All, (TraceEventType)0x20, 1)]
        public void SwitchLevel(SourceLevels sourceLevel, TraceEventType messageLevel, int expected)
        {
            var trace = new TraceSource("TestTraceSource");
            var listener = new TestTraceListener();
            trace.Listeners.Add(listener);
            trace.Switch.Level = sourceLevel;
            trace.TraceEvent(messageLevel, 0);
            Assert.Equal(expected, listener.GetCallCount(Method.TraceEvent));
        }
    }

    public sealed class TraceSourceTests_Default : TraceSourceTestsBase
    {
        // default mode: GlobalLock = true, AutoFlush = false, ThreadSafeListener = false
    }

    public sealed class TraceSourceTests_AutoFlush : TraceSourceTestsBase
    {
        internal override bool AutoFlush
        {
            get { return true; }
        }
    }

    public sealed class TraceSourceTests_NoGlobalLock : TraceSourceTestsBase
    {
        internal override bool UseGlobalLock
        {
            get { return false; }
        }
    }

    public sealed class TraceSourceTests_NoGlobalLock_AutoFlush : TraceSourceTestsBase
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

    public sealed class TraceSourceTests_ThreadSafeListener : TraceSourceTestsBase
    {
        internal override bool ThreadSafeListener
        {
            get { return true; }
        }
    }

    public sealed class TraceSourceTests_ThreadSafeListener_AutoFlush : TraceSourceTestsBase
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
    public abstract class TraceSourceTestsBase
    {
        public TraceSourceTestsBase()
        {
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
            var trace = new TraceSource("TestTraceSource", SourceLevels.All);
            var listener = GetTraceListener();
            trace.Listeners.Add(listener);
            trace.Flush();
            Assert.Equal(1, listener.GetCallCount(Method.Flush));
        }

        [Fact]
        public void TraceEvent1()
        {
            var trace = new TraceSource("TestTraceSource", SourceLevels.All);
            var listener = GetTraceListener();
            trace.Listeners.Add(listener);
            trace.TraceEvent(TraceEventType.Verbose, 0);
            Assert.Equal(1, listener.GetCallCount(Method.TraceEvent));
        }

        [Fact]
        public void TraceEvent2()
        {
            var trace = new TraceSource("TestTraceSource", SourceLevels.All);
            var listener = GetTraceListener();
            trace.Listeners.Add(listener);
            trace.TraceEvent(TraceEventType.Verbose, 0, "Message");
            Assert.Equal(1, listener.GetCallCount(Method.TraceEvent));
            var flushExpected = AutoFlush ? 1 : 0;
            Assert.Equal(flushExpected, listener.GetCallCount(Method.Flush));
        }

        [Fact]
        public void TraceEvent3()
        {
            var trace = new TraceSource("TestTraceSource", SourceLevels.All);
            var listener = GetTraceListener();
            trace.Listeners.Add(listener);
            trace.TraceEvent(TraceEventType.Verbose, 0, "Format", "Arg1", "Arg2");
            Assert.Equal(1, listener.GetCallCount(Method.TraceEvent));
            var flushExpected = AutoFlush ? 1 : 0;
            Assert.Equal(flushExpected, listener.GetCallCount(Method.Flush));
        }

        [Fact]
        public void TraceData1()
        {
            var trace = new TraceSource("TestTraceSource", SourceLevels.All);
            var listener = GetTraceListener();
            trace.Listeners.Add(listener);
            trace.TraceData(TraceEventType.Verbose, 0, new Object());
            Assert.Equal(1, listener.GetCallCount(Method.TraceData));
            var flushExpected = AutoFlush ? 1 : 0;
            Assert.Equal(flushExpected, listener.GetCallCount(Method.Flush));
        }

        [Fact]
        public void TraceData2()
        {
            var trace = new TraceSource("TestTraceSource", SourceLevels.All);
            var listener = GetTraceListener();
            trace.Listeners.Add(listener);
            trace.TraceData(TraceEventType.Verbose, 0, new Object[0]);
            Assert.Equal(1, listener.GetCallCount(Method.TraceData));
            var flushExpected = AutoFlush ? 1 : 0;
            Assert.Equal(flushExpected, listener.GetCallCount(Method.Flush));
        }
    }
}
