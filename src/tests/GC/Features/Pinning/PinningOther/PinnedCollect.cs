// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Tests Collect with pinned objects
// Arr has both Normal and Pinned handles

using System;
using System.Runtime.InteropServices;
using System.Security;

public class Test_PinnedCollect
{
    public static int Main()
    {
        float[] arr = new float[100];
        GCHandle handle1 = GCUtil.Alloc(arr, GCHandleType.Pinned);
        GCHandle handle2 = GCUtil.Alloc(arr, GCHandleType.Normal);

        IntPtr oldaddr, newaddr;

        oldaddr = GCUtil.AddrOfPinnedObject(handle1);
        Console.WriteLine("Address of obj: {0}", oldaddr);

        GC.Collect();
        GC.WaitForPendingFinalizers();

        //			handle1.Free();		// arr should only have normal handle now
        GCUtil.Free(ref handle1);
        GC.Collect();
        GC.WaitForPendingFinalizers();

        GC.Collect();
        GC.WaitForPendingFinalizers();

        try
        {
            Console.WriteLine("Address of obj: {0}", handle1.AddrOfPinnedObject());
        }
        catch (Exception e)
        {
            Console.WriteLine("Caught: " + e);
        }

        arr = null;
        GC.Collect();

        // Pinning the arr again..it should have moved
        GCHandle handle3 = GCUtil.Alloc(arr, GCHandleType.Pinned);
        newaddr = GCUtil.AddrOfPinnedObject(handle3);

        Console.WriteLine("Address of obj: {0}", newaddr);

        if (oldaddr == newaddr)
        {
            Console.WriteLine("Test failed!");
            return 1;
        }
        else
        {
            Console.WriteLine("Test passed!");
            return 100;
        }
    }
}

