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
        // Based on further calibration it is likley to be adjusted lower.
        // In relatively rare cases of a slow GetCurrentProcessorNumber, it may be recalibrated to a higher number.
        private static int s_processorIdRefreshRate = 50;
        // We will not adjust higher than this though.
        private const int MaxIdRefreshRate = 5000;

        private static int RefreshCurrentProcessorId()
        {
            double[]? calibrationSamples = s_CalibrationSamples;
            if (calibrationSamples != null)
                CalibrateOnce(calibrationSamples);

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
            if (s_processorIdRefreshRate <= 2)
                return Thread.GetCurrentProcessorNumber();

            int currentProcessorIdCache = t_currentProcessorIdCache--;
            if ((currentProcessorIdCache & ProcessorIdCacheCountDownMask) == 0)
            {
                return RefreshCurrentProcessorId();
            }

            return currentProcessorIdCache >> ProcessorIdCacheShift;
        }

        // We must collect multiple samples to account for irregularities caused by GC and context switches.
        // Why we keep an array of samples and do not adjust as we go:
        //   We expect that we will adjust the refresh rate down. If we do that early, we may end up forcing all
        //   the calibration work to happen much sooner. There is no urgency in being calibrated while the app
        //   is in start-up mode. That would just add to the "rush hour" traffic.
        private static int s_CalibrationToDo;
        private static int s_CalibrationDone;
        // 10 is chosen to budget the sampling under 5 msec total, assuming 0.5 msec per sample.
        private const int CalibrationSampleCount = 10;
        private static double[]? s_CalibrationSamples = new double[CalibrationSampleCount * 2];

        private static void CalibrateOnce(double[] calibrationSamples)
        {
            if (s_CalibrationToDo >= CalibrationSampleCount)
                return;

            int sample = Interlocked.Increment(ref s_CalibrationToDo) - 1;
            if (sample >= CalibrationSampleCount)
                return;

            // Actual calibration step. Let's try to fit into ~50 usec.
            int id = 0;
            long t1 = 0;
            long twentyMicrosecond = Stopwatch.Frequency / 50000;
            int iters = 1;

            // double the sample size until it is 1 msec.
            // we may spend up to 40 usec in this loop in a worst case.
            while (t1 < twentyMicrosecond)
            {
                iters *= 2;
                t1 = Stopwatch.GetTimestamp();
                for (int i = 0; i < iters; i++)
                {
                    id = Thread.GetCurrentProcessorNumber();
                }
                t1 = Stopwatch.GetTimestamp() - t1;
            }

            // assuming TLS takes 1/2 of ProcessorNumber time or less, this should take 10 usec or less
            long t2 = Stopwatch.GetTimestamp();
            for (int i = 0; i < iters; i++)
            {
                UninlinedThreadStatic();
            }
            long t3 = Stopwatch.GetTimestamp();

            // if we have useful measurements, record a sample
            if (id >= 0 && t1 > 0 && t3 - t2 > 0)
            {
                calibrationSamples[sample * 2] = (double)t1 / iters;            // ID
                calibrationSamples[sample * 2 + 1] = (double)(t3 - t2) / iters; // TLS
            }
            else
            {
                // API is not functional or clock did not go forward.
                // just pretend it was a very expensive sample with default ratio.
                calibrationSamples[sample * 2] = (double)Stopwatch.Frequency * 50; // 50 sec;
                calibrationSamples[sample * 2 + 1] = Stopwatch.Frequency; // 1 sec
            }

            // If this was the last sample computed, get best times and update the ratio of ID to TLS.
            if (Interlocked.Increment(ref s_CalibrationDone) == CalibrationSampleCount)
            {
                double idMin = double.MaxValue;
                double tlsMin = double.MaxValue;
                for (int i = 0; i < CalibrationSampleCount; i++)
                {
                    idMin = Math.Min(idMin, calibrationSamples[i * 2]);       //ID
                    tlsMin = Math.Min(tlsMin, calibrationSamples[i * 2 + 1]); //TLS
                }

                s_CalibrationSamples = null;
                s_processorIdRefreshRate = Math.Min(MaxIdRefreshRate, (int)(idMin / tlsMin));
            }
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
