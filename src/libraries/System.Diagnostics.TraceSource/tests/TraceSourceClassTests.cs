// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace System.Diagnostics.TraceSourceTests
{
    using Method = TestTraceListener.Method;

    public sealed class TraceSourceClassTests
    {
        [Fact]
        public void ConstrutorExceptionTest()
        {
            Assert.Throws<ArgumentNullException>(() => new TraceSource(null));
            AssertExtensions.Throws<ArgumentException>("name", null, () => new TraceSource(""));
        }

        [Fact]
        public void DefaultListenerTest()
        {
            var trace = new TraceSource("TestTraceSource");
            Assert.Equal(1, trace.Listeners.Count);
            Assert.IsType<DefaultTraceListener>(trace.Listeners[0]);
        }

        [Fact]
        public void SetSourceSwitchExceptionTest()
        {
            var trace = new TraceSource("TestTraceSource");
            Assert.Throws<ArgumentNullException>(() => trace.Switch = null);
        }

        [Fact]
        public void SetSourceSwitchTest()
        {
            var trace = new TraceSource("TestTraceSource");
            var @switch = new SourceSwitch("TestTraceSwitch");
            trace.Switch = @switch;
            Assert.Equal(@switch, trace.Switch);
        }

        [Fact]
        public void DefaultLevelTest()
        {
            var trace = new TraceSource("TestTraceSource");
            Assert.Equal(SourceLevels.Off, trace.Switch.Level);
        }

        [Fact]
        public void CloseTest()
        {
            var trace = new TraceSource("T1", SourceLevels.All);
            trace.Listeners.Clear();

            var listener = new TestTraceListener();
            trace.Listeners.Add(listener);
            trace.Close();
            Assert.Equal(1, listener.GetCallCount(Method.Close));
            // Assert that writing to a closed TraceSource is not an error.
            trace.TraceEvent(TraceEventType.Critical, 0);
        }

        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        static WeakReference PruneMakeRef()
        {
            return new WeakReference(new TraceSource("TestTraceSource"));
        }

        [Fact]
        public void PruneTest()
        {
            var strongTrace = new TraceSource("TestTraceSource");
            var traceRef = PruneMakeRef();
            Assert.True(traceRef.IsAlive);
            GC.Collect(2);
            Trace.Refresh();
            Assert.False(traceRef.IsAlive);
            GC.Collect(2);
            Trace.Refresh();
        }

        [Fact]
        public void TraceInformationTest()
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
        public void SwitchLevelTest(SourceLevels sourceLevel, TraceEventType messageLevel, int expected)
        {
            var trace = new TraceSource("TestTraceSource");
            var listener = new TestTraceListener();
            trace.Listeners.Add(listener);
            trace.Switch.Level = sourceLevel;
            trace.TraceEvent(messageLevel, 0);
            Assert.Equal(expected, listener.GetCallCount(Method.TraceEvent));
        }

        [Fact]
        public void NullSourceName()
        {
            AssertExtensions.Throws<ArgumentNullException>("name", () => new TraceSource(null));
            AssertExtensions.Throws<ArgumentNullException>("name", () => new TraceSource(null, SourceLevels.All));
        }

        [Fact]
        public void EmptySourceName()
        {
            AssertExtensions.Throws<ArgumentException>("name", null, () => new TraceSource(string.Empty));
            AssertExtensions.Throws<ArgumentException>("name", null, () => new TraceSource(string.Empty, SourceLevels.All));
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
    public abstract class TraceSourceTestsBase : IDisposable
    {

        void IDisposable.Dispose()
        {
            TraceTestHelper.ResetState();
        }

        public TraceSourceTestsBase()
        {            
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
            var trace = new TraceSource("TestTraceSource", SourceLevels.All);
            var listener = GetTraceListener();
            trace.Listeners.Add(listener);
            trace.Flush();
            Assert.Equal(1, listener.GetCallCount(Method.Flush));
        }

        [Fact]
        public void TraceEvent1Test()
        {
            var trace = new TraceSource("TestTraceSource", SourceLevels.All);
            var listener = GetTraceListener();
            trace.Listeners.Add(listener);
            trace.TraceEvent(TraceEventType.Verbose, 0);
            Assert.Equal(1, listener.GetCallCount(Method.TraceEvent));
        }

        [Fact]
        public void TraceEvent2Test()
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
        public void TraceEvent3Test()
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
        public void TraceData1Test()
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
        public void TraceData2Test()
        {
            var trace = new TraceSource("TestTraceSource", SourceLevels.All);
            var listener = GetTraceListener();
            trace.Listeners.Add(listener);
            trace.TraceData(TraceEventType.Verbose, 0, new Object[0]);
            Assert.Equal(1, listener.GetCallCount(Method.TraceData));
            var flushExpected = AutoFlush ? 1 : 0;
            Assert.Equal(flushExpected, listener.GetCallCount(Method.Flush));
        }

        [Fact]
        public void TraceTransferTest()
        {
            var trace = new TraceSource("TestTraceSource", SourceLevels.All);
            var listener = GetTraceListener();
            trace.Listeners.Add(listener);
            trace.TraceTransfer(1, "Trace transfer test message", Trace.CorrelationManager.ActivityId);
            Assert.Equal(1, listener.GetCallCount(Method.TraceTransfer));
            var flushExpected = AutoFlush ? 1 : 0;
            Assert.Equal(flushExpected, listener.GetCallCount(Method.Flush));
        }
    }
}
