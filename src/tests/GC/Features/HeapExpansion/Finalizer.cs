// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*
This test stimulates heap expansion on the finalizer thread
*/

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TestLibrary;

public class Test
{
    ~Test()
    {
        TestFramework.LogInformation("First Alloc in Finalizer");
        GCUtil.Alloc2(1024 * 512, 30);
        GCUtil.FreeNonPins2();
        GC.Collect();

        TestFramework.LogInformation("Second Alloc in Finalizer");
        GCUtil.Alloc2(1024 * 512, 50);
        GCUtil.FreePins2();
    }

    // Clearing stack references can't be relied upon to actually remove references to an object, so instead, create and remove
    // the reference to a finalizable object in a function that won't be inlined.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CreateAndReleaseFinalizable()
    {
        var t = new Test();
    }

    public static int Main()
    {
        CreateAndReleaseFinalizable();
        TestFramework.LogInformation("First Alloc");
        GCUtil.Alloc(1024 * 1024, 50);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        TestFramework.LogInformation("Second Alloc");
        GCUtil.Alloc(1024 * 1024, 50);
        GCUtil.FreePins();

        TestFramework.LogInformation("Test passed");
        return 100;
    }
}
