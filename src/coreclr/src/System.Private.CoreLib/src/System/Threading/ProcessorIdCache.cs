// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        // 50 is our best guess.
        // Based on speed check it will be adjusted, typically lower.
        // In relatively rare cases of a slow GetCurrentProcessorNumber, it may be adjusted to a higher number.
        private static int s_processorIdRefreshRate = 50;
        // We will not adjust higher than this though.
        private const int MaxIdRefreshRate = 5000;

        private static int RefreshCurrentProcessorId()
        {
            int currentProcessorId = Thread.GetCurrentProcessorNumber();

            // On Unix, GetCurrentProcessorNumber() is implemented in terms of sched_getcpu, which
            // doesn't exist on all platforms.  On those it doesn't exist on, GetCurrentProcessorNumber()
            // returns -1.  As a fallback in that case and to spread the threads across the buckets
            // by default, we use the current managed thread ID as a proxy.
            if (currentProcessorId < 0) currentProcessorId = Environment.CurrentManagedThreadId;

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
        internal static unsafe int ProcessorNumberSpeedCheck()
        {
            // NOTE: We do not check the frequency of the Stopwatch.
            //       The frequency often does not match the actual timer refresh rate anyways.
            //       If the resolution, precision or access time to the timer are inadequate for our measures here,
            //       the test will fail anyways.

            // default values pretend to be very long samples with default ratio
            double minID = Stopwatch.Frequency * 50;  // 50 sec
            double minTLS = Stopwatch.Frequency;      // 1 sec

            // warm up the code paths.
            UninlinedThreadStatic();
            if (Thread.GetCurrentProcessorNumber() < 0)
                return MaxIdRefreshRate;

            long oneMicrosecond = Stopwatch.Frequency / 1000000;
            for (int i = 0; i < 10; i++)
            {
                // we will measure at least 16 iterations and at least 1 microsecond
                int iters = 16;
                long t1 = 0;
                while (t1 < oneMicrosecond)
                {
                    iters *= 2;
                    t1 = Stopwatch.GetTimestamp();
                    for (int j = 0; j < iters; j++)
                    {
                        Thread.GetCurrentProcessorNumber();
                    }
                    t1 = Stopwatch.GetTimestamp() - t1;
                }

                minID = Math.Min(minID, (double)t1 / iters);

                // we will measure at least 16 iterations and at least 1 microsecond
                iters = 16;
                t1 = 0;
                while (t1 < oneMicrosecond)
                {
                    iters *= 2;
                    t1 = Stopwatch.GetTimestamp();
                    for (int j = 0; j < iters; j++)
                    {
                        UninlinedThreadStatic();
                    }
                    t1 = Stopwatch.GetTimestamp() - t1;
                }

                minTLS = Math.Min(minTLS, (double)t1 / iters);
            }

            s_processorIdRefreshRate = Math.Min((int)(minID / minTLS), MaxIdRefreshRate);
            return s_processorIdRefreshRate;
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
