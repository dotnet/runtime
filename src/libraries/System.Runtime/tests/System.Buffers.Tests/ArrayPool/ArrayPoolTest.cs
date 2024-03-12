// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Threading;
using Microsoft.DotNet.RemoteExecutor;

namespace System.Buffers.ArrayPool.Tests
{
    public abstract class ArrayPoolTest
    {
        protected static class EventIds
        {
            public const int BufferRented = 1;
            public const int BufferAllocated = 2;
            public const int BufferReturned = 3;
            public const int BufferTrimmed = 4;
            public const int BufferTrimPoll = 5;
        }

        protected static int RunWithListener(Action body, EventLevel level, Action<EventWrittenEventArgs> callback)
        {
            using (TestEventListener listener = new TestEventListener("System.Buffers.ArrayPoolEventSource", level))
            {
                int count = 0;
                listener.RunWithCallback(e =>
                {
                    Interlocked.Increment(ref count);
                    callback(e);
                }, body);
                return count;
            }
        }

        protected static void RemoteInvokeWithTrimming(Action method, int timeout = RemoteExecutor.FailWaitTimeoutMilliseconds)
        {
            var options = new RemoteInvokeOptions
            {
                TimeOut = timeout
            };

            options.StartInfo.UseShellExecute = false;

            RemoteExecutor.Invoke(method, options).Dispose();
        }
    }
}
