// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime
{
    public static partial class JitInfo
    {
        private const long TicksPerMillisecond = 10000;
        private const long TicksPerSecond = TicksPerMillisecond * 1000;

        // "Frequency" stores the frequency of the high-resolution performance counter,
        // if one exists. Otherwise it will store TicksPerSecond.
        // The frequency cannot change while the system is running,
        // so we only need to initialize it once.
        public static readonly long Frequency = System.Diagnostics.Stopwatch.QueryPerformanceFrequency();

        // pre calculating the tick frequency for quickly converting from QPC ticks to DateTime ticks
        private static readonly double s_tickFrequency = (double)TicksPerSecond / Frequency;

        /// <summary>
        /// Get the amount of time the JIT Compiler has spent compiling methods. If <paramref name="currentThread"/> is true,
        /// then this value is scoped to the current thread, otherwise, this is a global value.
        /// </summary>
        /// <param name="currentThread">Whether the returned value should be specific to the current thread. Default: false</param>
        /// <returns>The amount of time the JIT Compiler has spent compiling methods.</returns>
        public static TimeSpan GetCompilationTime(bool currentThread = false)
        {
            // See System.Diagnostics.Stopwatch.GetElapsedDateTimeTicks()
            return TimeSpan.FromTicks((long)(GetCompilationTimeInTicks(currentThread) * s_tickFrequency));
        }
    }
}