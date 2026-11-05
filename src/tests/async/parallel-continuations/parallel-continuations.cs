// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public class Async2ParallelContinuations
{
    [ConditionalFact(typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.IsMultithreadingSupported))]
    public static void TestParallelContinuations()
    {
        TaskCompletionSource tcs = new TaskCompletionSource();

        // One runtime async continuation should run inline, while the other is queued to the thread pool.
        using ThreadLocal<int> tl = new();
        var o = new Async2ParallelContinuations();
        Task<int> t1 = o.AwaitVirtualTaskThenReturnThreadLocal(tcs, tl);
        Task<int> t2 = o.AwaitVirtualTaskThenReturnThreadLocal(tcs, tl);

        tl.Value = 42;
        tcs.SetResult();
        // Use Task.Wait() below instead of awaiting to keep this thread busy
        // and avoid returning it to the thread pool.
        Task.WhenAll(t1, t2).Wait();

        Assert.True((t1.Result == 42 && t2.Result == 0) || (t1.Result == 0 && t2.Result == 42),
            $"Expected one inline continuation (42) and one queued continuation (0), got {t1.Result} and {t2.Result}.");
    }

    private async Task<int> AwaitVirtualTaskThenReturnThreadLocal(TaskCompletionSource tcs, ThreadLocal<int> tl)
    {
        await VirtualTask(tcs);
        return tl.Value;
    }

    public virtual Task VirtualTask(TaskCompletionSource tcs) => tcs.Task;
}
