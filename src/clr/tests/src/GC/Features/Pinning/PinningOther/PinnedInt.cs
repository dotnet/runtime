// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Tests Pinning of Int

using System;
using System.Runtime.InteropServices;

public class Test
{
    public static int Main()
    {
        int i = 10;
        Object temp1, temp2;

        GCHandle handle = GCUtil.Alloc(i, GCHandleType.Pinned);


        temp1 = GCUtil.GetTarget(handle);
        Console.WriteLine(temp1);
        GC.Collect();
        GC.WaitForPendingFinalizers();

        temp2 = GCUtil.GetTarget(handle);
        Console.WriteLine(temp2);

        if (temp1 == temp2)
        {
            Console.WriteLine("Test passed");
            return 100;
        }
        else
        {
            Console.WriteLine("Test failed");
            return 1;
        }
    }
}
