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

public class Async2VaryingYields
{
    [Fact]
    public static void TestEntryPoint()
    {
        Task.Run(AsyncEntry).Wait();
    }

    public static async Task AsyncEntry()
    {
        string[] args = Environment.GetCommandLineArgs();
        if (args.Length < 3)
        {
            Console.WriteLine("Use: corerun.exe <path to varying-yields.dll> <depth> <suspend frequency> [--use-direct-yield]");
            return;
        }

        if (!GCSettings.IsServerGC)
            Console.WriteLine("*** Warning: Server GC is disabled, set DOTNET_gcServer=1 ***");

        int depth = int.Parse(args[1]);
        // Yield on average every X calls.
        int yieldFrequency = int.Parse(args[2]);
        Benchmark warmupBm = new Benchmark(0.0001);
        warmupBm.Warmup = true;
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 40; j++)
            {
                await warmupBm.Run(0);
            }

            // Make sure we tier up...
            await Task.Delay(500);
        }

        Console.WriteLine("Warmup done, running benchmark");

        Benchmark bm = new(yieldFrequency == 0 ? 0 : (1 / (double)yieldFrequency));

        List<long> results = new();
        for (int i = 0; i < 16; i++)
        {
            bm.NumYields = 0;
            long numIters = await bm.Run(depth);
            Console.WriteLine($"iters={numIters} suspensions={bm.NumYields}");
            results.Add(numIters);
        }

        results.Sort();
        double avg = results.Skip(3).Take(10).Average();
        Console.WriteLine("Result = {0}", (long)avg);
    }

    private class Benchmark
    {
        private readonly Random _rand = new();
        private readonly double _yieldProbability;
        public ulong NumYields;
        public int Sink;
        public bool Warmup;

        public Benchmark(double yieldProbability) => _yieldProbability = yieldProbability;

public
#if ASYNC1_TASK
        async Task<long>
#elif ASYNC1_VALUETASK
        async ValueTask<long>
#else
        async2 long
#endif
        Run(int depth)
        {
            int liveState1 = depth * 3 + (int)(1 / _yieldProbability);
            int liveState2 = depth;
            double liveState3 = _yieldProbability;

            if (depth == 0)
                return await Loop();

            long result = await Run(depth - 1);
            Sink = (int)liveState1 + (int)liveState2 + (int)(1 / liveState3) + depth;
            return result;
        }

private
#if ASYNC1_TASK
        async Task<long>
#elif ASYNC1_VALUETASK
        async ValueTask<long>
#else
        async2 long
#endif
        Loop()
        {
            int time = Warmup ? 5 : 500;

            Stopwatch timer = Stopwatch.StartNew();

            long numIters = 0;
            while (timer.ElapsedMilliseconds < time)
            {
                for (int i = 0; i < 20; i++)
                {
                    numIters += await DoYields();
                }
            }

            return numIters;
        }

private
#if ASYNC1_TASK
        async Task<int>
#elif ASYNC1_VALUETASK
        async ValueTask<int>
#else
        async2 int
#endif
        DoYields()
        {
            int numIters = 0;

            if (_rand.NextDouble() < _yieldProbability)
            {
                if (s_useDirectYield)
                    await new DirectYieldAwaitable(NumYields);
                else
                    await Task.Yield();

                NumYields++;
            }

            numIters++;

            if (_rand.NextDouble() < _yieldProbability)
            {
                if (s_useDirectYield)
                    await new DirectYieldAwaitable(NumYields);
                else
                    await Task.Yield();

                NumYields++;
            }

            numIters++;

            if (_rand.NextDouble() < _yieldProbability)
            {
                if (s_useDirectYield)
                    await new DirectYieldAwaitable(NumYields);
                else
                    await Task.Yield();

                NumYields++;
            }

            numIters++;
            return numIters;
        }

        private static readonly bool s_useDirectYield = Environment.GetCommandLineArgs().Contains("--use-direct-yield");

        private struct DirectYieldAwaitable
        {
            private readonly ulong _numYields;

            public DirectYieldAwaitable(ulong numYields) => _numYields = numYields;

            public DirectYieldAwaiter GetAwaiter() => new DirectYieldAwaiter(_numYields);

            public struct DirectYieldAwaiter : ICriticalNotifyCompletion
            {
                private readonly ulong _numYields;

                public DirectYieldAwaiter(ulong numYields) => _numYields = numYields;

                public bool IsCompleted => false;

                public void OnCompleted(Action continuation)
                {
                    if (_numYields % 512 == 0)
                        ThreadPool.UnsafeQueueUserWorkItem(static act => act(), continuation, true);
                    else
                        continuation();
                }

                public void UnsafeOnCompleted(Action continuation)
                {
                    if (_numYields % 512 == 0)
                        ThreadPool.UnsafeQueueUserWorkItem(static act => act(), continuation, true);
                    else
                        continuation();
                }

                public void GetResult() { }
            }
        }

    }
}
