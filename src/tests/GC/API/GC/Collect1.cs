// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Tests GC.Collect(1)

using System;

public class Test
{
    public static int Main()
    {
        int[] array = new int[25];
        int agen1 = GC.GetGeneration(array);
        Console.WriteLine("Array is in generation: " + agen1);

        GC.Collect();

        Object obj = new Object();
        int ogen1 = GC.GetGeneration(obj);
        Console.WriteLine("Object is in generation: " + ogen1);

        Console.WriteLine("Collect(1)");
        GC.Collect(1);

        int agen2 = GC.GetGeneration(array);
        int ogen2 = GC.GetGeneration(obj);

        Console.WriteLine("Array is in generation: {0}", agen2);
        Console.WriteLine("Object is in generation: {0}", ogen2);

        if (agen2 > ogen2)
        {  // gen 0,1 collected
            Console.WriteLine("Test for GC.Collect(1) passed!");
            return 100;
        }
        else if (agen2 == ogen2 && agen2 == GC.MaxGeneration)
        {
            // both got collected, possibly because of GC Stress
            Console.WriteLine("Test for GC.Collect(1) passed!");
            return 100;
        }
        else
        {
            Console.WriteLine("Test for GC.Collect(1) failed!");
            return 1;
        }
    }
}
