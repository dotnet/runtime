// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*
This test stimulates heap expansion on the finalizer thread
*/

using System;
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

    public static int Main()
    {
        Test t = new Test();
        TestFramework.LogInformation("First Alloc");
        GCUtil.Alloc(1024 * 1024, 50);
        t = null;
        GC.Collect();
        GC.WaitForPendingFinalizers();
        TestFramework.LogInformation("Second Alloc");
        GCUtil.Alloc(1024 * 1024, 50);
        GCUtil.FreePins();

        TestFramework.LogInformation("Test passed");
        return 100;
    }
}
