// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Tests Pinning many objects
// Here we create 2500 arrays and pin them all

using System;
using System.Runtime.InteropServices;
public class Test_PinnedMany
{
    public static int Main()
    {
        int NUM = 2500;
        int[][] arr = new int[NUM][];
        GCHandle[] handle = new GCHandle[NUM];

        IntPtr[] oldaddr = new IntPtr[NUM];

        for (int i = 0; i < NUM; i++)
        {
            arr[i] = new int[NUM];
            handle[i] = GCUtil.Alloc(arr[i], GCHandleType.Pinned);
            oldaddr[i] = GCUtil.AddrOfPinnedObject(handle[i]);
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();

        for (int i = 0; i < NUM; i++)
        {
            if (GCUtil.AddrOfPinnedObject(handle[i]) != oldaddr[i])
            {
                Console.WriteLine("Test failed!");
                return 1;
            }
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();

        for (int i = 0; i < NUM; i++)
        {
            if (handle[i].IsAllocated != true)
            {
                Console.WriteLine("Test failed!");
                return 1;
            }
        }

        Console.WriteLine("Test passed!");
        return 100;
    }
}
