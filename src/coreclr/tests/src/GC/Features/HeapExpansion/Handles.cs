// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*
This test stimulates heap expansion with both Pinned and unpinned handles
*/

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TestLibrary;

public class Test
{
    public static List<GCHandle> list = new List<GCHandle>();
    public static List<GCHandle> pinnedList = new List<GCHandle>();
    public static int index = -1;

    public static int Main()
    {
        TestFramework.LogInformation("First Alloc");
        GCUtil.Alloc(1024 * 1024, 50);
        GCUtil.FreeNonPins();
        GC.Collect();

        TestFramework.LogInformation("Second Alloc");
        GCUtil.Alloc(1024 * 1024, 50);
        GCUtil.FreeNonPins();
        GC.Collect();

        GCUtil.FreePins();

        TestFramework.LogInformation("Test passed");
        return 100;
    }
}
