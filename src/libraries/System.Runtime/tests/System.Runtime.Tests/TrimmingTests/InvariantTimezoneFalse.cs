// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Threading;
using System.Runtime.CompilerServices;

/// <summary>
/// Ensures setting InvariantTimezone = false still works in a trimmed app.
/// </summary>
class Program
{
    static int Main(string[] args)
    {
        if(GetInvariant(null))
        {
            return -99;
        }

        TimeZoneInfo utc = TimeZoneInfo.FindSystemTimeZoneById("UTC");
        TimeZoneInfo tst = TimeZoneInfo.FindSystemTimeZoneById("Asia/Tokyo");
        if(utc == tst)
        {
            return -1;
        }
        if(utc.BaseUtcOffset != TimeSpan.Zero)
        {
            return -2;
        }
        if(tst.BaseUtcOffset == TimeSpan.Zero)
        {
            return -3;
        }

        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "get_Invariant")]
        static extern bool GetInvariant(TimeZoneInfo t);

        return 100;
    }
}
