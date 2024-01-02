// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//#define ASYNC1_TASK
//#define ASYNC1_VALUETASK

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public class Async2MinCallCostMicrobench
{
    [Fact]
    public static void TestEntryPoint()
    {
        Task.Run(AsyncEntry).Wait();
    }

    public static async Task AsyncEntry()
    {
        if (!GCSettings.IsServerGC)
            Console.WriteLine("*** Warning: Server GC is disabled, set DOTNET_gcServer=1 ***");

        time = 0.5;

        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 40; j++)
            {
                await RunBench("AsyncCallingAsync");
                await RunBench("AsyncCallingValueTaskAsync");
                await RunBench("AsyncCallingAsync2");
                await RunBench("Async2CallingAsync");
                await RunBench("Async2CallingValueTaskAsync");
                await RunBench("Async2CallingAsync2");
                await RunBench("Async2CallingAsync2NoInlining");
                await RunBench("Async2CallingAsync2WithContextSave");
                await RunBench("Sync2CallingSync");
            }

            // Make sure we tier up...
            await Task.Delay(500);
        }

        Console.WriteLine("Warmup done, running benchmark");
        printResult = true;
        time = 1000;
        await RunBench("AsyncCallingAsync");
        await RunBench("AsyncCallingValueTaskAsync");
        await RunBench("AsyncCallingAsync2");
        await RunBench("Async2CallingAsync");
        await RunBench("Async2CallingValueTaskAsync");
        await RunBench("Async2CallingAsync2");
        await RunBench("Async2CallingAsync2NoInlining");
        await RunBench("Async2CallingAsync2WithContextSave");
        await RunBench("Sync2CallingSync");
    }

    static double time = 10.0;
    static bool printResult = false;
    private static async Task RunBench(string type)
    {
        if (printResult)
            Console.WriteLine($"Running benchmark on '{type}' methods");

        List<long> results = new();
        for (int i = 0; i < 16; i++)
        {
            long numIters = await Run(type);
//            Console.WriteLine($"iters={numIters}");
            results.Add(numIters);
        }

        results.Sort();
        double avg = results.Skip(3).Take(10).Average();
        if (printResult)
            Console.WriteLine("Result = {0}", (long)avg);
    }

    private static async Task<long> Run(string type)
    {
        if (type == "AsyncCallingAsync")
            return await AsyncCallingAsync();
        if (type == "AsyncCallingValueTaskAsync")
            return await AsyncCallingValueTaskAsync();
        if (type == "AsyncCallingAsync2")
            return await AsyncCallingAsync2();
        if (type == "AsyncCallingYield")
            return await AsyncCallingYield();
        if (type == "Async2CallingAsync")
            return await Async2CallingAsync();
        if (type == "Async2CallingValueTaskAsync")
            return await Async2CallingValueTaskAsync();
        if (type == "Async2CallingAsync2")
            return await Async2CallingAsync2();
        if (type == "Async2CallingAsync2NoInlining")
            return await Async2CallingAsync2NoInlining();
        if (type == "Async2CallingYield")
            return await Async2CallingYield();
        if (type == "Sync2CallingSync")
            return Sync2CallingSync();
        if (type == "Async2CallingAsync2WithContextSave")
            return await Async2CallingAsync2WithContextSave();

        return 0;
    }
#pragma warning disable CS1998

    private static async Task<long> AsyncCallingAsync()
    {
        Stopwatch timer = Stopwatch.StartNew();

        long numIters = 0;

        while (timer.ElapsedMilliseconds < time)
        {
            for (int i = 0; i < 10; i++)
            {
                await EmptyAsync();
            }
            numIters++;
        }
        return numIters * 10;
    }

    private static async Task<long> AsyncCallingValueTaskAsync()
    {
        Stopwatch timer = Stopwatch.StartNew();

        long numIters = 0;

        while (timer.ElapsedMilliseconds < time)
        {
            for (int i = 0; i < 10; i++)
            {
                await EmptyValueTaskAsync();
            }
            numIters++;
        }
        return numIters * 10;
    }

    private static async Task<long> AsyncCallingAsync2()
    {
        Stopwatch timer = Stopwatch.StartNew();

        long numIters = 0;

        while (timer.ElapsedMilliseconds < time)
        {
            for (int i = 0; i < 10; i++)
            {
                await EmptyAsync2();
            }
            numIters++;
        }
        return numIters * 10;
    }

    private static async2 Task<long> Async2CallingAsync()
    {
        Stopwatch timer = Stopwatch.StartNew();

        long numIters = 0;

        while (timer.ElapsedMilliseconds < time)
        {
            for (int i = 0; i < 10; i++)
            {
                await EmptyAsync();
            }
            numIters++;
        }
        return numIters * 10;
    }

    private static async2 Task<long> Async2CallingValueTaskAsync()
    {
        Stopwatch timer = Stopwatch.StartNew();

        long numIters = 0;

        while (timer.ElapsedMilliseconds < time)
        {
            for (int i = 0; i < 10; i++)
            {
                await EmptyValueTaskAsync();
            }
            numIters++;
        }
        return numIters * 10;
    }

    private static async2 Task<long> Async2CallingAsync2()
    {
        Stopwatch timer = Stopwatch.StartNew();

        long numIters = 0;

        while (timer.ElapsedMilliseconds < time)
        {
            for (int i = 0; i < 10; i++)
            {
                await EmptyAsync2();
            }
            numIters++;
        }
        return numIters * 10;
    }

    private static async2 Task<long> Async2CallingAsync2NoInlining()
    {
        Stopwatch timer = Stopwatch.StartNew();

        long numIters = 0;

        while (timer.ElapsedMilliseconds < time)
        {
            for (int i = 0; i < 10; i++)
            {
                await EmptyAsync2NoInlining();
            }
            numIters++;
        }
        return numIters * 10;
    }

    private static async2 Task<long> Async2CallingAsync2WithContextSave()
    {
        FakeThread thread = CurrentThread;
        if (thread == null)
        {
            CurrentThread = new FakeThread();
            thread = CurrentThread;
        }

        Stopwatch timer = Stopwatch.StartNew();

        long numIters = 0;

        while (timer.ElapsedMilliseconds < time)
        {
            for (int i = 0; i < 10; i++)
            {
                await EmptyAsync2WithContextSave();
            }
            numIters++;
        }
        return numIters * 10;
    }

    public class FakeSyncContext {}
    public class FakeExecContext
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void RestoreChangedContextToThread(FakeThread thread, FakeExecContext execContext, FakeExecContext newExecContext)
        {
        }
    }
    public class FakeThread
    {
        public FakeSyncContext _syncContext;
        public FakeExecContext _execContext;
    }
    [ThreadStatic]
    public static FakeThread CurrentThread;


    private static async Task<long> AsyncCallingYield()
    {
        Stopwatch timer = Stopwatch.StartNew();

        long numIters = 0;

        while (timer.ElapsedMilliseconds < time)
        {
            for (int i = 0; i < 10; i++)
            {
                await Task.Yield();
            }
            numIters++;
        }
        return numIters * 10;
    }

    private static async2 Task<long> Async2CallingYield()
    {
        Stopwatch timer = Stopwatch.StartNew();

        long numIters = 0;

        while (timer.ElapsedMilliseconds < time)
        {
            for (int i = 0; i < 10; i++)
            {
                await Task.Yield();
            }
            numIters++;
        }
        return numIters * 10;
    }

    private static long Sync2CallingSync()
    {
        Stopwatch timer = Stopwatch.StartNew();

        long numIters = 0;

        while (timer.ElapsedMilliseconds < time)
        {
            for (int i = 0; i < 10; i++)
            {
                EmptyMethod();
            }
            numIters++;
        }
        return numIters * 10;
    }

    private static async Task EmptyAsync()
    {
        // Add some work that forces the method to be a real async method
        if (time == 0)
            await Task.Yield();
        return;
    }

    private static async ValueTask EmptyValueTaskAsync()
    {
        // Add some work that forces the method to be a real async method
        if (time == 0)
            await Task.Yield();
        return;
    }

    private static async2 Task EmptyAsync2()
    {
        // Add some work that forces the method to be a real async method
        if (time == 0)
            await Task.Yield();
        return;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async2 Task EmptyAsync2NoInlining()
    {
        // Add some work that forces the method to be a real async method
        if (time == 0)
            await Task.Yield();
        return;
    }

    // This simulates async2 capturing the same amount of state that existing async needs to capture to handle the current semantics around async locals and synchronizationcontext
    private static async2 Task EmptyAsync2WithContextSave()
    {
        FakeThread thread = CurrentThread;
        FakeExecContext? previousExecutionCtx = thread._execContext;
        FakeSyncContext? previousSyncCtx = thread._syncContext;

        try
        {
            // Add some work that forces the method to be a real async method
            if (time == 0)
                await Task.Yield();
        }
        finally
        {
            // The common case is that these have not changed, so avoid the cost of a write barrier if not needed.
            if (previousSyncCtx != thread._syncContext)
            {
                // Restore changed SynchronizationContext back to previous
                thread._syncContext = previousSyncCtx;
            }

            FakeExecContext? currentExecutionCtx = thread._execContext;
            if (previousExecutionCtx != currentExecutionCtx)
            {
                FakeExecContext.RestoreChangedContextToThread(thread, previousExecutionCtx, currentExecutionCtx);
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void EmptyMethod()
    {
        return;
    }
}
