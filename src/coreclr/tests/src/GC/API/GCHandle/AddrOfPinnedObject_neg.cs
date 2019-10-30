// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Negative Test for GCHandle.AddrOfPinnedObject()...should throw and exception when handle is not pinned.

using System;
using System.Runtime.InteropServices;

public class Test
{
    public static int Main()
    {
        int[] array = new int[25];
        bool passed = true;

        Console.WriteLine("Allocating a normal handle to object..");
        GCHandle handle = GCHandle.Alloc(array);  // handle is NOT pinned.

        try
        {
            IntPtr addr = handle.AddrOfPinnedObject();
            Console.WriteLine("AddrOfPinnedObject = {0}", addr);
        }
        catch (InvalidOperationException)
        {
            Console.WriteLine("Caught expected exception");
        }
        catch (Exception)
        {
            Console.WriteLine("Caught unexpected exception!");
            Console.WriteLine("Test1 Failed!");
            passed = false;
        }

        handle.Free();

        try
        {
            IntPtr addr = handle.AddrOfPinnedObject();
            Console.WriteLine("AddrOfPinnedObject = {0}", addr);
        }
        catch (InvalidOperationException)
        {
            Console.WriteLine("Caught expected exception");
        }
        catch (Exception)
        {
            Console.WriteLine("Caught unexpected exception!");
            Console.WriteLine("Test1 Failed!");
            passed = false;
        }


        if (!passed)
        {
            Console.WriteLine("Test Failed!");
            return 1;
        }

        Console.WriteLine("Test Passed!");
        return 100;
    }
}
