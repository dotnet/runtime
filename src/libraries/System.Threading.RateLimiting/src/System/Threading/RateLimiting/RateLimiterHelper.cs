// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Threading.RateLimiting
{
    internal static class RateLimiterHelper
    {
        public static TimeSpan? GetElapsedTime(long? startTimestamp)
        {
            if (startTimestamp is null)
            {
                return null;
            }

            return Stopwatch.GetElapsedTime(startTimestamp.Value);
        }

        public static TimeSpan GetElapsedTime(long startTimestamp, long endTimestamp)
        {
            return Stopwatch.GetElapsedTime(startTimestamp, endTimestamp);
        }
    }
}
