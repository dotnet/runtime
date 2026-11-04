// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System;

/// <summary>Provides downlevel polyfills for members on <see cref="DateTimeOffset"/>.</summary>
internal static class DateTimeOffsetPolyfills
{
    private static readonly DateTimeOffset s_unixEpoch = new(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);

    extension(DateTimeOffset value)
    {
        public static DateTimeOffset UnixEpoch => s_unixEpoch;

        public int TotalOffsetMinutes =>
            (int)(value.Offset.Ticks / TimeSpan.TicksPerMinute);
    }
}
