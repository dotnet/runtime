// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Threading.RateLimiting
{
    internal static class RateLimiterHelper
    {
        internal static readonly double TickFrequency = (double)TimeSpan.TicksPerSecond / Stopwatch.Frequency;
#if !NET
        public static TimeSpan GetElapsedTime(long startTimestamp) => new TimeSpan((long)((Stopwatch.GetTimestamp() - startTimestamp) * TickFrequency));
#endif
    }
}
