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

public class Async2EHMicrobench
{
    public static int Main()
    {
        Task.Run(AsyncEntry).Wait();

        Console.WriteLine("Test Passed");
        return 100;
    }

    [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(false)]
    public static async Task AsyncEntry()
    {
        if (!GCSettings.IsServerGC)
            Console.WriteLine("*** Warning: Server GC is disabled, set DOTNET_gcServer=1 ***");

        string[] args = Environment.GetCommandLineArgs();
        int depth = args.Length > 1 ? int.Parse(args[1]) : 2;
        Console.WriteLine("Using depth = {0}", depth);

        // Yield N times before throwing
        int yieldFrequency = args.Length > 2 ? int.Parse(args[2]) : 1;
        Console.WriteLine("Yielding {0} times before throwing", yieldFrequency);

        // Inject a finally every N frames
        int finallyRate = args.Length > 3 ? int.Parse(args[3]) : 1000;
        Console.WriteLine("With a try/finally block every {0} frames", finallyRate);

        // Throw or return
        bool throwOrReturn = args.Length > 4 ? String.Equals(args[4], "throw", StringComparison.OrdinalIgnoreCase) : true;
        Console.WriteLine($"Which will {(throwOrReturn ? "throw" : "return")} when finished yielding");

        Benchmark warmupBm = new Benchmark(5, 5, 2, throwOrReturn);
        warmupBm.Warmup = true;
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 40; j++)
            {
                await warmupBm.Run("Async2");
                await warmupBm.Run("Async2WithContextSaveRestore");
                await warmupBm.Run("Task");
                await warmupBm.Run("ValueTask");
            }

            // Make sure we tier up...
            await Task.Delay(500);
        }

        Console.WriteLine("Warmup done, running benchmark");
        await RunBench(yieldFrequency, depth, finallyRate, throwOrReturn, "Async2");
        await RunBench(yieldFrequency, depth, finallyRate, throwOrReturn, "Async2WithContextSaveRestore");
        await RunBench(yieldFrequency, depth, finallyRate, throwOrReturn, "Task");
        await RunBench(yieldFrequency, depth, finallyRate, throwOrReturn, "ValueTask");
    }

    [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(false)]
    private static async Task RunBench(int yieldCount, int depth, int finallyRate, bool throwOrReturn, string type)
    {

        Benchmark bm = new(yieldCount, depth, finallyRate, throwOrReturn);
        Console.WriteLine($"Running benchmark on '{type}' methods");

        List<long> results = new();
        for (int i = 0; i < 16; i++)
        {
            long numIters = await bm.Run(type);
//            Console.WriteLine($"iters={numIters}");
            results.Add(numIters);
        }

        results.Sort();
        double avg = results.Skip(3).Take(10).Average();
        Console.WriteLine("Result = {0}", (long)avg);
    }

    private class Benchmark
    {
        private readonly int _yieldCount;
        private readonly int _depth;
        private readonly int _finallyRate;
        private readonly bool _throwOrReturn;
        public int Sink;
        public bool Warmup;

        public Benchmark(int yieldCount, int depth, int finallyRate, bool throwOrReturn)
        {
            _yieldCount = yieldCount;
            _depth = depth;
            _finallyRate = finallyRate;
            _throwOrReturn = throwOrReturn;
        }

        public async Task<long> Run(string type)
        {
            if (type == "Async2")
                return await RunAsync2(_depth);
            if (type == "Async2WithContextSaveRestore")
                return await RunAsync2WithContextSaveRestore(_depth);
            if (type == "Task")
                return await RunTask(_depth);
            if (type == "ValueTask")
                return await RunValueTask(_depth);
            return 0;
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(false)]
        public async Task<long> RunTask(int depth)
        {
            int liveState1 = depth * 3 + _yieldCount;
            int liveState2 = depth;
            double liveState3 = _yieldCount;

            if (depth == 0)
            {
                int currentAwaitCount = 0;

                while (currentAwaitCount < _yieldCount)
                {
                    currentAwaitCount++;
                    await Task.Yield();
                }

                if (_throwOrReturn)
                    throw new Exception();
                return 8375983;
            }

            long result = 0;

            if (depth == _depth)
            {
                int time = Warmup ? 5 : 250;

                Stopwatch timer = Stopwatch.StartNew();

                long numIters = 0;

                while (timer.ElapsedMilliseconds < time)
                {
                    try
                    {
                        result = await RunTask(depth - 1);
                    }
                    catch (Exception e)
                    {
                    }
                    numIters++;

                }
                return numIters;
            }
            else if ((depth % _finallyRate) == 0)
            {
                try
                {
                    result = await RunTask(depth - 1);
                }
                finally
                {
                    Sink = (int)liveState1 + (int)liveState2 + (int)(1 / liveState3) + depth;
                }
            }
            else
            {
                result = await RunTask(depth - 1);
            }

            Sink = (int)liveState1 + (int)liveState2 + (int)(1 / liveState3) + depth;
            return result;
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(false)]
        public async ValueTask<long> RunValueTask(int depth)
        {
            int liveState1 = depth * 3 + _yieldCount;
            int liveState2 = depth;
            double liveState3 = _yieldCount;

            if (depth == 0)
            {
                int currentAwaitCount = 0;

                while (currentAwaitCount < _yieldCount)
                {
                    currentAwaitCount++;
                    await Task.Yield();
                }

                if (_throwOrReturn)
                    throw new Exception();
                return 8375983;
            }

            long result = 0;

            if (depth == _depth)
            {
                int time = Warmup ? 5 : 250;

                Stopwatch timer = Stopwatch.StartNew();

                long numIters = 0;

                while (timer.ElapsedMilliseconds < time)
                {
                    try
                    {
                        result = await RunValueTask(depth - 1);
                    }
                    catch (Exception e)
                    {

                    }
                    numIters++;

                }
                return numIters;
            }
            else if ((depth % _finallyRate) == 0)
            {
                try
                {
                    result = await RunValueTask(depth - 1);
                }
                finally
                {
                    Sink = (int)liveState1 + (int)liveState2 + (int)(1 / liveState3) + depth;
                }
            }
            else
            {
                result = await RunValueTask(depth - 1);
            }

            Sink = (int)liveState1 + (int)liveState2 + (int)(1 / liveState3) + depth;
            return result;
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

        // This case is used to test the impact of save/restore of the sync and execution context on performance
        // The intent here is to measure what the performance impact of maintaining the current async semantics with
        // the new implementation.
        public async Task<long> RunAsync2WithContextSaveRestore(int depth)
        {
            FakeThread thread = CurrentThread;
            if (thread == null)
            {
                CurrentThread = new FakeThread();
                thread = CurrentThread;
            }

            FakeExecContext? previousExecutionCtx = thread._execContext;
            FakeSyncContext? previousSyncCtx = thread._syncContext;

            try
            {
                int liveState1 = depth * 3 + _yieldCount;
                int liveState2 = depth;
                double liveState3 = _yieldCount;

                if (depth == 0)
                {
                    int currentAwaitCount = 0;

                    while (currentAwaitCount < _yieldCount)
                    {
                        currentAwaitCount++;
                        await Task.Yield();
                    }

                    if (_throwOrReturn)
                        throw new Exception();
                    return 8375983;
                }

                long result = 0;

                if (depth == _depth)
                {
                    int time = Warmup ? 5 : 250;

                    Stopwatch timer = Stopwatch.StartNew();

                    long numIters = 0;

                    while (timer.ElapsedMilliseconds < time)
                    {
                        try
                        {
                            result = await RunAsync2WithContextSaveRestore(depth - 1);
                        }
                        catch (Exception e)
                        {

                        }
                        numIters++;

                    }
                    return numIters;
                }
                else if ((depth % _finallyRate) == 0)
                {
                    try
                    {
                        result = await RunAsync2WithContextSaveRestore(depth - 1);
                    }
                    finally
                    {
                        Sink = (int)liveState1 + (int)liveState2 + (int)(1 / liveState3) + depth;
                    }
                }
                else
                {
                    result = await RunAsync2WithContextSaveRestore(depth - 1);
                }

                Sink = (int)liveState1 + (int)liveState2 + (int)(1 / liveState3) + depth;
                return result;
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

        public async Task<long> RunAsync2(int depth)
        {
            int liveState1 = depth * 3 + _yieldCount;
            int liveState2 = depth;
            double liveState3 = _yieldCount;

            if (depth == 0)
            {
                int currentAwaitCount = 0;

                while (currentAwaitCount < _yieldCount)
                {
                    currentAwaitCount++;
                    await Task.Yield();
                }

                if (_throwOrReturn)
                    throw new Exception();
                return 8375983;
            }

            long result = 0;

            if (depth == _depth)
            {
                int time = Warmup ? 5 : 250;

                Stopwatch timer = Stopwatch.StartNew();

                long numIters = 0;

                while (timer.ElapsedMilliseconds < time)
                {
                    try
                    {
                        result = await RunAsync2(depth - 1);
                    }
                    catch (Exception e)
                    {

                    }
                    numIters++;

                }
                return numIters;
            }
            else if ((depth % _finallyRate) == 0)
            {
                try
                {
                    result = await RunAsync2(depth - 1);
                }
                finally
                {
                    Sink = (int)liveState1 + (int)liveState2 + (int)(1 / liveState3) + depth;
                }
            }
            else
            {
                result = await RunAsync2(depth - 1);
            }

            Sink = (int)liveState1 + (int)liveState2 + (int)(1 / liveState3) + depth;
            return result;
        }
    }
}
