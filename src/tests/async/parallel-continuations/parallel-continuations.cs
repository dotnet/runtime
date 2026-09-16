// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public class Async2ParallelContinuations
{
    [ConditionalFact(typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.IsMultithreadingSupported))]
    public static async Task TestParallelContinuations()
    {
        TaskCompletionSource tcs = new TaskCompletionSource();

        // We expect multiple runtime async continuations to be invoked in parallel; one is invoked synchronously,
        // the other is invoked on the thread pool.
        using ThreadLocal<int> tl = new();
        var o = new Async2ParallelContinuations();
        Task<int> t1 = o.AwaitVirtualTaskThenReturnThreadLocal(tcs, tl);
        Task<int> t2 = o.AwaitVirtualTaskThenReturnThreadLocal(tcs, tl);

        tl.Value = 42;
        tcs.SetResult();
        int[] results = await Task.WhenAll(t1, t2);

        Assert.True((results[0] == 42 && results[1] == 0) || (results[0] == 0 && results[1] == 42));
    }

    private async Task<int> AwaitVirtualTaskThenReturnThreadLocal(TaskCompletionSource tcs, ThreadLocal<int> tl)
    {
        await VirtualTask(tcs);
        return tl.Value;
    }

    public virtual Task VirtualTask(TaskCompletionSource tcs) => tcs.Task;
}
