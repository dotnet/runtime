// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Tests multiple handles for same object


using System;
using System.Runtime.InteropServices;

public class Test_PinnedMultiple
{
    public static int Main()
    {
        int[] arr = new int[1000];
        GCHandle[] arrhandle = new GCHandle[10000]; // array of handles to the same object
        IntPtr[] oldaddress = new IntPtr[10000];        // store old addresses
        IntPtr[] newaddress = new IntPtr[10000];        // store new addresses

        for (int i = 0; i < 10000; i++)
        {
            arrhandle[i] = GCUtil.Alloc(arr, GCHandleType.Pinned);
            oldaddress[i] = GCUtil.AddrOfPinnedObject(arrhandle[i]);
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();

        for (int i = 0; i < 10000; i++)
        {
            newaddress[i] = GCUtil.AddrOfPinnedObject(arrhandle[i]);
        }

        for (int i = 0; i < 10000; i++)
        {
            if (oldaddress[i] != newaddress[i])
            {
                Console.WriteLine("Test failed");
                return 1;
            }
        }

        Console.WriteLine("Test passed");
        return 100;
    }
}
