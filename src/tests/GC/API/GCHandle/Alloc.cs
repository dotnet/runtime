// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Tests GCHandle.Alloc()

using System;
using System.Runtime.InteropServices;

public class Test_Alloc
{
    public static int Main()
    {
        int[] array = new int[25];

        Console.WriteLine("Allocating a handle to object..");
        GCHandle handle = GCHandle.Alloc(array);

        bool ans = handle.IsAllocated;
        if (ans)
            Console.WriteLine("GCHandle is allocated = ");

        if (ans == true)
        {
            Console.WriteLine("Test for GCHandle.Alloc() passed!");
            return 100;
        }
        else
        {
            Console.WriteLine("Test for GCHandle.Alloc() failed!");
            return 1;
        }
    }
}
