// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

public static class Program
{
    [DllImport("__Internal")]
    public static extern void mono_ios_set_summary (string value);

    public static async Task<int> Main(string[] args)
    {
        mono_ios_set_summary($"Starting functional test");
        Console.WriteLine("Done!");
        await Task.Delay(5000);

        DateTimeOffset utcNow = DateTimeOffset.UtcNow;
            
        string utcTime = utcNow.ToString();
        string localTime = utcNow.ToLocalTime().ToString();

        Console.WriteLine("UTCNOW: " + utcTime);
        Console.WriteLine("LocalTime: " + localTime);

        if (utcTime != localTime)
        {
            Console.WriteLine("NOT EQUAL");
        }
        else
        {
            Console.WriteLine("EQUAL");
        }

        return 42;
    }
}
