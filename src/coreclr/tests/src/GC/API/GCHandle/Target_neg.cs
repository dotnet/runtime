// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Tests GCHandle.Target negative scenarios

using System;
using System.Runtime.InteropServices;

public class Test
{
    public static int Main()
    {
        bool passed = true;

        Object o = new Object();
        GCHandle gch = GCHandle.Alloc(o);
        gch.Free();

        try
        {
            gch.Target = o;
        }
        catch (InvalidOperationException)
        {
            Console.WriteLine("Caught expected InvalidOperationException");
        }
        catch (Exception)
        {
            Console.WriteLine("Caught unexpected exception!");

            Console.WriteLine("Test1 Failed!");
            passed = false;
        }

        try
        {
            Object o2 = gch.Target;
        }
        catch (InvalidOperationException)
        {
            Console.WriteLine("Caught expected InvalidOperationException");
        }
        catch (Exception)
        {
            Console.WriteLine("Caught unexpected exception!");
            Console.WriteLine("Test2 Failed!");
            passed = false;
        }

        try
        {
            Object o2 = gch.Target;
        }
        catch (InvalidOperationException)
        {
            Console.WriteLine("Caught expected InvalidOperationException");
        }
        catch (Exception)
        {
            Console.WriteLine("Caught unexpected exception!");
            Console.WriteLine("Test3 Failed!");
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
