// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#if !NET
using System.Diagnostics;

namespace System.Threading.RateLimiting
{
    internal static class RateLimiterHelper
    {
        private static readonly double TickFrequency = (double)TimeSpan.TicksPerSecond / Stopwatch.Frequency;
        public static TimeStamp GetElapsedTime(long startTimestamp)
        {
            new TimeSpan((long)((Stopwatch.GetTimestamp() - startTimestamp) * TickFrequency));
        }
    }
}
#endif