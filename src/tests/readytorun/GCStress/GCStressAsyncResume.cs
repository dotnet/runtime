// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public class GCStressAsyncResume
{
    [Fact]
    public static void TestConcurrentAsyncCalls()
    {
        const int WorkerCount = 4;
        using Barrier start = new Barrier(WorkerCount);
        var resume = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var workers = new Thread[WorkerCount];
        var objectResults = new Task<object>[WorkerCount];
        var stringResults = new Task<string>[WorkerCount];
        object value = new object();
        string other = new string('s', 42);

        for (int i = 0; i < WorkerCount; i++)
        {
            int worker = i;
            workers[i] = new Thread(() =>
            {
                start.SignalAndWait();
                objectResults[worker] = R2RGCStressAsync(value, resume.Task);
                stringResults[worker] = R2RGCStressAsync(other, resume.Task);
            });
            workers[i].Start();
        }

        foreach (Thread worker in workers)
        {
            worker.Join();
        }

        for (int i = 0; i < WorkerCount; i++)
        {
            Assert.False(objectResults[i].IsCompleted);
            Assert.False(stringResults[i].IsCompleted);
        }

        int before = GC.CollectionCount(0);
        resume.SetResult(true);
        Task.WaitAll(objectResults);
        Task.WaitAll(stringResults);
        Assert.True(GC.CollectionCount(0) > before, "R2R async-resume GC stress did not collect.");

        for (int i = 0; i < WorkerCount; i++)
        {
            Assert.Same(value, objectResults[i].Result);
            Assert.Same(other, stringResults[i].Result);
        }

        Console.WriteLine("R2R GC stress concurrent async calls: passed");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<T> R2RGCStressAsync<T>(T value, Task resume) where T : class
    {
        int[] live = new[] { 42 };
        await resume;
        Assert.Equal(42, live[0]);
        return value;
    }
}
