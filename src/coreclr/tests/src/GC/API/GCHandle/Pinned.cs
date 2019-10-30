// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Tests GCHandleType.Pinned .. the pinned object should not be collected.

using System;
using System.Runtime.InteropServices;

public class Test
{
    public static int Main()
    {
        int[] array = new int[25];

        Console.WriteLine("Allocating a pinned handle to object..");
        GCHandle handle = GCHandle.Alloc(array, GCHandleType.Pinned); // Pinned handle

        //int gen1 = GC.GetGeneration(array);
        //Console.WriteLine("Object is in generation " + gen1);

        IntPtr addr1 = handle.AddrOfPinnedObject();

        // ensuring that GC happens even with /debug mode
        array = null;
        GC.Collect();

        //int gen2 = GC.GetGeneration(array);
        //Console.WriteLine("Object is in generation " + gen2);

        IntPtr addr2 = handle.AddrOfPinnedObject();

        if (addr1 == addr2)
        {
            Console.WriteLine("Test for GCHandleType.Pinned passed!");
            return 100;
        }
        else
        {
            Console.WriteLine("Test for GCHandleType.Pinned failed!");
            return 1;
        }
    }
}
