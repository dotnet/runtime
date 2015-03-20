// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.Diagnostics.TraceSourceTests
{
    using Method = TestTraceListener.Method;

    public class TraceSourceTests
    {
        [Fact]
        public void DefaultListener()
        {
            var trace = new TraceSource("TestTraceSource");
            Assert.Equal(1, trace.Listeners.Count);
            Assert.IsType<DefaultTraceListener>(trace.Listeners[0]);
        }

        [Fact]
        public void DefaultLevel()
        {
            var trace = new TraceSource("TestTraceSource");
            Assert.Equal(SourceLevels.Off, trace.Switch.Level);
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

        [Fact]
        public void TraceEvent()
        {
            var trace = new TraceSource("TestTraceSource", SourceLevels.All);
            var listener = new TestTraceListener();
            trace.Listeners.Add(listener);
            trace.TraceEvent(TraceEventType.Verbose, 0);
            Assert.Equal(1, listener.GetCallCount(Method.TraceEvent));
            trace.TraceEvent(TraceEventType.Verbose, 0, "Message");
            Assert.Equal(2, listener.GetCallCount(Method.TraceEvent));
            trace.TraceEvent(TraceEventType.Verbose, 0, "Format {0}", "arg0", 1);
            Assert.Equal(3, listener.GetCallCount(Method.TraceEvent));
        }

        [Fact]
        public void TraceData()
        {
            var trace = new TraceSource("TestTraceSource", SourceLevels.All);
            var listener = new TestTraceListener();
            trace.Listeners.Add(listener);
            trace.TraceData(TraceEventType.Verbose, 0, null);
            Assert.Equal(1, listener.GetCallCount(Method.TraceData));
            trace.TraceData(TraceEventType.Verbose, 0, "Data1", 2);
            Assert.Equal(2, listener.GetCallCount(Method.TraceData));
        }

        [Fact]
        public void TraceInformation()
        {
            var trace = new TraceSource("TestTraceSource", SourceLevels.All);
            var listener = new TestTraceListener();
            trace.Listeners.Add(listener);
            trace.TraceInformation(null);
            Assert.Equal(0, listener.GetCallCount(Method.TraceData));
            Assert.Equal(0, listener.GetCallCount(Method.Write));
            Assert.Equal(1, listener.GetCallCount(Method.TraceEvent));
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
    }
}
