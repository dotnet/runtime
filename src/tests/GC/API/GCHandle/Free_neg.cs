// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Tests GCHandle.Free()

using System;
using System.Runtime.InteropServices;

public class Test
{
    public static int Main()
    {
        int[] array = new int[25];

        bool pass = false;

        Console.WriteLine("Allocating a handle to object..");
        GCHandle handle = GCHandle.Alloc(array);

        handle.Free();

        Console.WriteLine("Freeing the handle...");

        try
        {
            handle.Free();
        }
        catch (InvalidOperationException)
        {
            Console.WriteLine("Expected InvalidOperationException");
            pass = true;
        }
        catch (Exception)
        {
            Console.WriteLine("This should NOT throw an exception:");
            pass = false;
        }

        if (pass)
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
