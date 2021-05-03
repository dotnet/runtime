// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Diagnostics.TraceSourceTests
{
    public class TraceEventCacheClassTests
    {
        [Fact]
        public void DateTimePropertyTest()
        {
            var cache = new TraceEventCache();
            var begin = DateTime.UtcNow;
            var dt = cache.DateTime;
            var end = DateTime.UtcNow;
            Assert.True(dt >= begin);
            Assert.True(dt <= end);
            var cached = cache.DateTime;
            Assert.Equal(dt, cached);
        }

        [Fact]
        public void ProcessIdTest()
        {
            var cache = new TraceEventCache();
            int id = cache.ProcessId;
            int expected = Environment.ProcessId;
            Assert.Equal((int)expected, id);
        }

        [Fact]
        public void ThreadIdTest()
        {
            var cache = new TraceEventCache();
            var id = cache.ThreadId;
            Assert.NotNull(id);
            Assert.NotEqual("", id);
        }

        [Fact]
        public void TimestampTest()
        {
            var cache = new TraceEventCache();
            var dt1 = cache.Timestamp;
            var dt2 = cache.Timestamp;
            Assert.Equal(dt1, dt2);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50958", typeof(PlatformDetection), nameof(PlatformDetection.IsBrowser), nameof(PlatformDetection.IsMonoAOT))]
        public void CallstackTest_NotEmpty()
        {
            var cache = new TraceEventCache();
            Assert.NotEmpty(cache.Callstack);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50958", typeof(PlatformDetection), nameof(PlatformDetection.IsBrowser), nameof(PlatformDetection.IsMonoAOT))]
        public void CallstackTest_ContainsExpectedFrames()
        {
            var cache = new TraceEventCache();
            Assert.Contains("System.Environment.get_StackTrace()", cache.Callstack);
        }

        [Fact]
        public void LogicalOperationStack()
        {
            var cache = new TraceEventCache();
            var logicalOperationStack = cache.LogicalOperationStack;
            Assert.Equal(0, logicalOperationStack.Count);
            Trace.CorrelationManager.StartLogicalOperation("SecondaryThread");
            Trace.CorrelationManager.StartLogicalOperation("MainThread");
            Assert.NotNull(logicalOperationStack);
            Assert.Equal(2, logicalOperationStack.Count);
            Assert.Equal("MainThread", logicalOperationStack.Pop().ToString());
            Assert.Equal("SecondaryThread", logicalOperationStack.Peek().ToString());
            Trace.CorrelationManager.StopLogicalOperation();
            Assert.Equal(0, logicalOperationStack.Count);
        }
    }
}
