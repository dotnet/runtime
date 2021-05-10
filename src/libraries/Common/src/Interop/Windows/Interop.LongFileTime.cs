// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

internal static partial class Interop
{
    /// <summary>
    /// 100-nanosecond intervals (ticks) since January 1, 1601 (UTC).
    /// </summary>
    /// <remarks>
    /// For NT times that are defined as longs (LARGE_INTEGER, etc.).
    /// Do NOT use for FILETIME unless you are POSITIVE it will fall on an
    /// 8 byte boundary.
    /// </remarks>
    internal struct LongFileTime
    {
#pragma warning disable CS0649
        /// <summary>
        /// 100-nanosecond intervals (ticks) since January 1, 1601 (UTC).
        /// </summary>
        internal long TicksSince1601;
#pragma warning restore CS0649

        internal DateTimeOffset ToDateTimeOffset() => new DateTimeOffset(DateTime.FromFileTimeUtc(TicksSince1601));
    }
}
