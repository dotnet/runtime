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
        if(utc.BaseUtcOffset != TimeSpan.Zero)
        {
            return -1;
        }

        try
        {
            TimeZoneInfo tst = TimeZoneInfo.FindSystemTimeZoneById("Asia/Tokyo");
            if(utc == tst)
            {
                return -2;
            }
            if(tst.BaseUtcOffset == TimeSpan.Zero)
            {
                return -3;
            }
        }
        // some AzDO images don't have tzdata installed
        catch (TimeZoneNotFoundException tznfe)
        {
            if(tznfe.InnerException == null || tznfe.InnerException.GetType() != typeof(System.IO.DirectoryNotFoundException))
            {
                return -4;
            }
        }

        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "get_Invariant")]
        static extern bool GetInvariant(TimeZoneInfo t);

        return 100;
    }
}
