// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// Tests GC.GetTotalPauseDuration()

using System;
using System.Diagnostics;
using System.Reflection;

public class Test_Collect
{
    public static int Main()
    {
        Stopwatch sw = Stopwatch.StartNew();
        GC.Collect();
        sw.Stop();
        TimeSpan elapsed = sw.Elapsed;
        TimeSpan totalPauseDuration = (TimeSpan)typeof(GC).GetMethod("GetTotalPauseDuration", BindingFlags.Public | BindingFlags.Static).Invoke(null, null);
        GCMemoryInfo memoryInfo = GC.GetGCMemoryInfo();
        TimeSpan lastGcDuration = memoryInfo.PauseDurations[0];

        // These conditions assume the only GC in the process 
        // is the one we just triggered. This makes the test incompatible 
        // with any changes that might introduce extra GCs.

        if (TimeSpan.Zero < totalPauseDuration &&
            totalPauseDuration <= elapsed &&
            lastGcDuration == totalPauseDuration)
        {
            return 100;
        }
        else
        {
            return 101;
        }
    }
}
