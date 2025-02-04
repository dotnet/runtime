// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

/// <summary>
/// Ensures setting InvariantTimezone = true still works in a trimmed app.
/// </summary>
class Program
{
    static int Main(string[] args)
    {
        if(!GetInvariant(null))
        {
            return -99;
        }

        TimeZoneInfo utc = TimeZoneInfo.FindSystemTimeZoneById("UTC");
        if(utc != TimeZoneInfo.Utc)
        {
            return -1;
        }
        if(utc.BaseUtcOffset != TimeSpan.Zero)
        {
            return -2;
        }
        if(TimeZoneInfo.Local == TimeZoneInfo.Utc)
        {
            return -3;
        }
        var tzs = TimeZoneInfo.GetSystemTimeZones();
        if(tzs.Count != 1)
        {
            Console.WriteLine($"tzs.Count {tzs.Count}");
            return -4;
        }
        if(tzs[0] != TimeZoneInfo.Utc)
        {
            Console.WriteLine($"GetSystemTimeZones()[0] {tzs[0]}");
            return -5;
        }
        try
        {
            TimeZoneInfo tst = TimeZoneInfo.FindSystemTimeZoneById("Asia/Tokyo");
            return -6;
        }
        catch (TimeZoneNotFoundException)
        {
            // expected
        }
        return 100;

        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "get_Invariant")]
        static extern bool GetInvariant(TimeZoneInfo t);
    }
}
