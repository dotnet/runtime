// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Tests GCHandle.Free()

using System;
using System.Runtime.InteropServices;
using Xunit;

public class Test_Free
{
    [Fact]
    public static int TestEntryPoint()
    {
        int[] array = new int[25];

        Console.WriteLine("Allocating a handle to object..");
        GCHandle handle = GCHandle.Alloc(array);

        Console.WriteLine("Freeing the handle...");
        handle.Free();

        bool ans = handle.IsAllocated;
        if (ans)
            Console.WriteLine("GCHandle is allocated");

        if (ans == false)
        {
            Console.WriteLine("Test for GCHandle.Free() passed!");
            return 100;
        }
        else
        {
            Console.WriteLine("Test for GCHandle.Free() failed!");
            return 1;
        }
    }
}
