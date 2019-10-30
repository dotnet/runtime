// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Tests GCHandle.IsAllocated 

using System;
using System.Runtime.InteropServices;

public class Test
{
    public static int Main()
    {
        int[] array = new int[25];

        Console.WriteLine("Allocating a handle to object..");
        GCHandle handle = GCHandle.Alloc(array);

        bool ans1 = handle.IsAllocated;

        Console.Write("GCHandle.IsAllocated = ");

        if (ans1)
            Console.WriteLine("True");
        else
            Console.WriteLine("False");

        Console.WriteLine("Freeing the handle...");
        handle.Free();

        bool ans2 = handle.IsAllocated;
        Console.Write("GCHandle.IsAllocated = ");
        if (ans2)
            Console.WriteLine("True");
        else
            Console.WriteLine("False");

        if ((ans1 == true) && (ans2 == false))
        {
            Console.WriteLine("Test for GCHandle.IsAllocated passed!");
            return 100;
        }
        else
        {
            Console.WriteLine("Test for GCHandle.IsAllocated failed!");
            return 1;
        }
    }
}
