// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Threading
{
    internal static class ProcessorIdCache
    {
        // The upper bits of t_currentProcessorIdCache are the currentProcessorId. The lower bits of
        // the t_currentProcessorIdCache are counting down to get it periodically refreshed.
        // TODO: Consider flushing the currentProcessorIdCache on Wait operations or similar
        // actions that are likely to result in changing the executing core
        [ThreadStatic]
        private static int t_currentProcessorIdCache;

        private const int ProcessorIdCacheShift = 16;
        private const int ProcessorIdCacheCountDownMask = (1 << ProcessorIdCacheShift) - 1;
        // Refresh rate of the cache. Will be derived from a speed check of GetCurrentProcessorNumber API.
        private static int s_processorIdRefreshRate;
        // We will not adjust higher than this though.
        private const int MaxIdRefreshRate = 5000;

        private static int RefreshCurrentProcessorId()
        {
            int currentProcessorId = Thread.GetCurrentProcessorNumber();

            // On Unix, GetCurrentProcessorNumber() is implemented in terms of sched_getcpu, which
            // doesn't exist on all platforms.  On those it doesn't exist on, GetCurrentProcessorNumber()
            // returns -1.  As a fallback in that case and to spread the threads across the buckets
            // by default, we use the current managed thread ID as a proxy.
            if (currentProcessorId < 0)
                currentProcessorId = Environment.CurrentManagedThreadId;

            Debug.Assert(s_processorIdRefreshRate <= ProcessorIdCacheCountDownMask);

            // Mask with int.MaxValue to ensure the execution Id is not negative
            t_currentProcessorIdCache = ((currentProcessorId << ProcessorIdCacheShift) & int.MaxValue) | s_processorIdRefreshRate;

            return currentProcessorId;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetCurrentProcessorId()
        {
            int currentProcessorIdCache = t_currentProcessorIdCache--;
            if ((currentProcessorIdCache & ProcessorIdCacheCountDownMask) == 0)
            {
                return RefreshCurrentProcessorId();
            }

            return currentProcessorIdCache >> ProcessorIdCacheShift;
        }

        // If GetCurrentProcessorNumber takes any nontrivial time (compared to TLS access), return false.
        // Check more than once - to make sure it was not because TLS was delayed by GC or a context switch.
        internal static bool ProcessorNumberSpeedCheck()
        {
            // NOTE: We do not check the frequency of the Stopwatch.
            //       The frequency often does not match the actual timer refresh rate anyways.
            //       If the resolution, precision or access time to the timer are inadequate for our measures here,
            //       the test will fail anyways.

            double minID = double.MaxValue;
            double minTLS = double.MaxValue;

            // warm up the code paths.
            UninlinedThreadStatic();
            // also check if API is actually functional (-1 means not supported)
            if (Thread.GetCurrentProcessorNumber() < 0)
            {
                s_processorIdRefreshRate = ProcessorIdCacheCountDownMask;
                return false;
            }

            long oneMicrosecond = Stopwatch.Frequency / 1000000 + 1;
            for (int i = 0; i < 10; i++)
            {
                // we will measure at least 16 iterations and at least 1 microsecond
                long t;
                int iters = 8;
                do
                {
                    iters *= 2;
                    t = Stopwatch.GetTimestamp();
                    for (int j = 0; j < iters; j++)
                    {
                        Thread.GetCurrentProcessorNumber();
                    }
                    t = Stopwatch.GetTimestamp() - t;
                } while (t < oneMicrosecond);

                minID = Math.Min(minID, (double)t / iters);

                // we will measure at least 1 microsecond,
                // and use at least 1/2 of ProcID iterations
                // we assume that TLS can't be more than 2x slower than ProcID
                iters /= 4;
                do
                {
                    iters *= 2;
                    t = Stopwatch.GetTimestamp();
                    for (int j = 0; j < iters; j++)
                    {
                        UninlinedThreadStatic();
                    }
                    t = Stopwatch.GetTimestamp() - t;
                } while (t < oneMicrosecond);

                minTLS = Math.Min(minTLS, (double)t / iters);
            }

            // A few words about choosing cache refresh rate:
            //
            // There are too reasons why data structures use core affinity:
            // 1) To improve locality - avoid running on one core and using data in other core's cache.
            // 2) To reduce sharing - avoid multiple threads using the same piece of data.
            //
            // Scenarios with large footprint, like striped caches, are sensitive to both parts. It is desirable to access
            // large data from the "right" core.
            // In scenarios where the state is small, like a striped counter, it is mostly about sharing.
            // Otherwise the state is small and occasionally moving counter to a different core via cache miss is not a big deal.
            //
            // In scenarios that care more about sharing precise results of GetCurrentProcessorNumber may not justify
            // the cost unless the underlying implementation is very cheap.
            // In such cases it is desirable to amortize the cost over multiple accesses by caching in a ThreadStatic.
            //
            // In addition to the data structure, the benefits also depend on use pattern and on concurrency level.
            // I.E. if an array pool user only rents array "just in case" but does not actually use it, and concurrency level is low,
            // a longer refresh would be beneficial since that could lower the API cost.
            // If array is actually used, then there is benefit from higher precision of the API and shorter refresh is more attractive.
            //
            // Overall we do not know the ideal refresh rate and using some kind of dynamic feedback is unlikely to be feasible.
            // Experiments have shown, however, that 5x amortization rate is a good enough balance between precision and cost of the API.
            s_processorIdRefreshRate = Math.Min((int)(minID * 5 / minTLS), MaxIdRefreshRate);

            // In a case if GetCurrentProcessorNumber is particularly fast, like it happens on platforms supporting RDPID instruction,
            // caching is not an improvement, thus it is desirable to bypass the cache entirely.
            // Such systems consistently derive the refresh rate at or below 2-3, while the next tier, RDTSCP based implementations result in ~10,
            // so we use "5" as a criteria to separate "fast" machines from the rest.
            return s_processorIdRefreshRate <= 5;
        }

        // NoInlining is to make sure JIT does not CSE and to have a better perf proxy for TLS access.
        // Measuring inlined ThreadStatic in a loop results in underestimates and unnecessary caching.
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static int UninlinedThreadStatic()
        {
            return t_currentProcessorIdCache;
        }
    }
}
