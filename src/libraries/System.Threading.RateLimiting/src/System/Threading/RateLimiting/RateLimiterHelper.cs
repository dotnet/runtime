// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Threading.RateLimiting
{
    internal static class RateLimiterHelper
    {
#if !NET
        private static readonly double TickFrequency = (double)TimeSpan.TicksPerSecond / Stopwatch.Frequency;
#endif
        public static TimeSpan? GetElapsedTime(long? startTimestamp)
        {
            if (startTimestamp is null)
            {
                return null;
            }

#if NET
            return Stopwatch.GetElapsedTime(startTimestamp.Value);
#else
            return new((long)((Stopwatch.GetTimestamp() - startTimestamp.Value) * TickFrequency));
#endif
        }

        public static TimeSpan GetElapsedTime(long startTimestamp, long endTimestamp)
        {
#if NET
            return Stopwatch.GetElapsedTime(startTimestamp, endTimestamp);
#else
            return new((long)((endTimestamp - startTimestamp) * TickFrequency));
#endif
        }
    }
}
