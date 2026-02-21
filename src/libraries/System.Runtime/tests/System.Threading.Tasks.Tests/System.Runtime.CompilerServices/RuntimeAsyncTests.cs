// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.Threading.Tasks.Tests
{
    public class RuntimeAsyncTests
    {
        private static bool IsRemoteExecutorAndRuntimeAsyncSupported => RemoteExecutor.IsSupported && PlatformDetection.IsRuntimeAsyncSupported;

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        static async Task Func()
        {
            await Task.Delay(1);
            await Task.Yield();
        }

        [ConditionalFact(nameof(IsRemoteExecutorAndRuntimeAsyncSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/124072", typeof(PlatformDetection), nameof(PlatformDetection.IsInterpreter))]
        public void RuntimeAsync_TaskCompleted()
        {
            RemoteExecutor.Invoke(async () =>
            {

                // NOTE: This depends on private implementation details generally only used by the debugger.
                // If those ever change, this test will need to be updated as well.

                typeof(Task).GetField("s_asyncDebuggingEnabled", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, true);

                for (int i = 0; i < 1000; i++)
                {
                    await Func();
                }

                int taskCount = ((Dictionary<int, long>)typeof(Task).GetField("s_runtimeAsyncTaskTimestamps", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null)).Count;
                int continuationCount = ((Dictionary<object, long>)typeof(Task).GetField("s_runtimeAsyncContinuationTimestamps", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null)).Count;
                Assert.InRange(taskCount, 0, 10); // some other tasks may be created by the runtime, so this is just using a reasonably small upper bound
                Assert.InRange(continuationCount, 0, 10);
            }).Dispose();
        }
    }
}
