// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public class Async2ParallelContinuations
{
    [Fact]
    public static void TestParallelContinuations()
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
        Task.WhenAll(t1, t2).Wait();

        Assert.True((t1.Result == 42 && t2.Result == 0) || (t1.Result == 0 && t2.Result == 42));
    }

    private async Task<int> AwaitVirtualTaskThenReturnThreadLocal(TaskCompletionSource tcs, ThreadLocal<int> tl)
    {
        await VirtualTask(tcs);
        return tl.Value;
    }

    public virtual Task VirtualTask(TaskCompletionSource tcs) => tcs.Task;
}
