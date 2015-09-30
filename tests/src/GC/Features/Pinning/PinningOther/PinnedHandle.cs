// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Tests Pinned handle
// Should throw an InvalidOperationException for accessing the AddrOfPinnedObject()
// for a different type of handle.

using System;
using System.Runtime.InteropServices;

public class Test
{
    public static int Main()
    {
        int[] arr = new int[100];
        GCHandle handle = GCUtil.Alloc(arr, GCHandleType.Pinned);
        GCHandle hhnd = GCUtil.Alloc(handle, GCHandleType.Normal);

        GC.Collect();
        GC.WaitForPendingFinalizers();

        Console.WriteLine("Address of obj: {0}", GCUtil.AddrOfPinnedObject(handle));
        try
        {
            Console.WriteLine("Address of handle {0}", GCUtil.AddrOfPinnedObject(hhnd));
        }
        catch (Exception e)
        {
            Console.WriteLine("Caught: {0}", e.ToString());
            Console.WriteLine("Test passed");
            return 100;
        }

        return 1;
    }
}
