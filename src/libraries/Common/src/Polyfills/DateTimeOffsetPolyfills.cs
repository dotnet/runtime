// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System;

/// <summary>Provides downlevel polyfills for instance members on <see cref="DateTimeOffset"/>.</summary>
internal static class DateTimeOffsetPolyfills
{
    extension(DateTimeOffset value)
    {
        public int TotalOffsetMinutes =>
            (int)(value.Offset.Ticks / TimeSpan.TicksPerMinute);
    }
}
