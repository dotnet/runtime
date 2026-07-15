// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace System.Diagnostics;

/// <summary>Provides downlevel polyfills for static methods on <see cref="Stopwatch"/>.</summary>
internal static class StopwatchPolyfills
{
    private static readonly double s_tickFrequency = (double)TimeSpan.TicksPerSecond / Stopwatch.Frequency;

    extension(Stopwatch)
    {
        public static TimeSpan GetElapsedTime(long startingTimestamp) =>
            new((long)((Stopwatch.GetTimestamp() - startingTimestamp) * s_tickFrequency));

        public static TimeSpan GetElapsedTime(long startingTimestamp, long endingTimestamp) =>
            new((long)((endingTimestamp - startingTimestamp) * s_tickFrequency));
    }
}
