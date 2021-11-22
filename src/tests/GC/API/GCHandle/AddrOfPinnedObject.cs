// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Tests GCHandle.AddrOfPinnedObject() .. The address of a pinned object remains same even after a collection

using System;
using System.Runtime.InteropServices;

public class Test_AddrOfPinnedObject
{
    public static int Main()
    {
        int[] array = new int[25];

        Console.WriteLine("Allocating a pinned handle to object..");
        GCHandle handle = GCHandle.Alloc(array, GCHandleType.Pinned);  // pinned this object.

        IntPtr addr1 = handle.AddrOfPinnedObject();
        Console.WriteLine("AddrOfPinnedObject = {0}", addr1);

        GC.Collect();
        IntPtr addr2 = handle.AddrOfPinnedObject();
        Console.WriteLine("After Collection AddrOfPinnedObject = {0}", addr2);

        if (addr1 == addr2)
        {
            Console.WriteLine("Test for GCHandle.AddrOfPinnedObject() passed!");
            return 100;
        }
        else
        {
            Console.WriteLine("Test for GCHandle.AddrOfPinnedObject() failed!");
            return 1;
        }
    }
}
