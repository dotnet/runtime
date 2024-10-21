// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;
using Xunit.Sdk;

namespace System.Threading.Tasks.Tests
{
    public class AsyncIteratorMethodBuilderTests
    {
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void AsyncIteratorMethodBuilder_TaskCompleted()
        {
            RemoteExecutor.Invoke(() =>
            {
                static async IAsyncEnumerable<int> Func(TaskCompletionSource tcs)
                {
                    await tcs.Task;
                    yield return 1;
                }

                // NOTE: This depends on private implementation details generally only used by the debugger.
                // If those ever change, this test will need to be updated as well.

                typeof(Task).GetField("s_asyncDebuggingEnabled", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, true);

                for (int i = 0; i < 1000; i++)
                {
                    TaskCompletionSource tcs = new();
                    IAsyncEnumerator<int> e = Func(tcs).GetAsyncEnumerator();
                    Task t = e.MoveNextAsync().AsTask();
                    tcs.SetResult();
                    t.Wait();
                    e.MoveNextAsync().AsTask().Wait();
                }

                int activeCount = ((dynamic)typeof(Task).GetField("s_currentActiveTasks", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null)).Count;
                Assert.InRange(activeCount, 0, 10); // some other tasks may be created by the runtime, so this is just using a reasonably small upper bound
            }).Dispose();
        }
    }
}
