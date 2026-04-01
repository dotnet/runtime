// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System;

/// <summary>Provides downlevel polyfills for members on <see cref="DateTimeOffset"/>.</summary>
internal static class DateTimeOffsetPolyfills
{
    private const long UnixEpochTicks = 719162L * 10000 * 1000 * 60 * 60 * 24;

    extension(DateTimeOffset value)
    {
        public int TotalOffsetMinutes =>
            (int)(value.Offset.Ticks / TimeSpan.TicksPerMinute);
    }

    extension(DateTimeOffset)
    {
        public static DateTimeOffset UnixEpoch => new DateTimeOffset(UnixEpochTicks, TimeSpan.Zero);
    }
}
