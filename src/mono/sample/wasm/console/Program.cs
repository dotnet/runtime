// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using System.Globalization;

public class Test
{
    public static async Task<int> Main(string[] args)
    {
        await Task.Delay(1);
        Console.WriteLine("Hello World!");
        TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById("America/Los_Angeles");
        Console.WriteLine(tz.DisplayName);
        Console.WriteLine(tz.StandardName);
        Console.WriteLine(tz.DaylightName);
        for (int i = 0; i < args.Length; i++) {
            Console.WriteLine($"args[{i}] = {args[i]}");
        }
        return args.Length;
    }
}