// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

/// <summary>
/// Debuggee app for cDAC stress log dump tests.
/// Performs allocations and GC to generate stress log entries, then crashes
/// so a dump is produced for analysis.
/// </summary>
internal static class Program
{
    private static void Main()
    {
        // Perform some work to generate stress log entries.
        // GC operations produce many log messages across facilities.
        var list = new List<object>();
        for (int i = 0; i < 1000; i++)
            list.Add(new byte[1024]);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Environment.FailFast("cDAC dump test: StressLog debuggee intentional crash");
    }
}
