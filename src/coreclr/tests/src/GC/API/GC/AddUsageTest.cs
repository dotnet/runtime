// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/* AddUsageTest
 *
 * Tests GC.AddMemoryPressure by passing a valid value (AddMemoryPressureTest.Pressure)
 * and making sure the objects with added pressure get collected more times by
 * the GC than those without pressure.
 */


using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

public class Dummy
{
    public Dummy(long pressure)
    {
        GC.AddMemoryPressure(pressure);
    }

    public Dummy()
    {
    }
}



public class AddUsageTest
{
    public static int Pressure = 100000; // test will fail with values less than this
    private int _numTests = 0;


    private AddUsageTest()
    {
    }




    public bool AddTest()
    {
        _numTests++;

        int gcCount1 = GC.CollectionCount(0);
        for (int i = 0; i < 100; i++)
        {
            Dummy heavy = new Dummy(AddUsageTest.Pressure);
            int gen = GC.GetGeneration(heavy);
            if (gen != 0)
            {
                Console.WriteLine("Warning: newly-allocated dummy ended up in gen {0}", gen);
            }
            //GC.WaitForPendingFinalizers();
        }
        gcCount1 = GC.CollectionCount(0) - gcCount1;


        int gcCount2 = GC.CollectionCount(0);
        for (int i = 0; i < 100; i++)
        {
            Dummy light = new Dummy();
            int gen = GC.GetGeneration(light);
            if (gen != 0)
            {
                Console.WriteLine("Warning: newly-allocated dummy ended up in gen {0}", gen);
            }
            //GC.WaitForPendingFinalizers();
        }
        gcCount2 = GC.CollectionCount(0) - gcCount2;

        Console.WriteLine("{0} {1}", gcCount1, gcCount2);
        if (gcCount1 > gcCount2)
        {
            Console.WriteLine("AddTest Passed");
            Console.WriteLine();
            return true;
        }

        Console.WriteLine("AddTest Failed");
        Console.WriteLine();
        return false;
    }

    public bool RunTest()
    {
        int numPass = 0;


        if (AddTest())
            numPass++;

        return (numPass == _numTests);
    }

    public static int Main()
    {
        AddUsageTest test = new AddUsageTest();

        if (test.RunTest())
        {
            Console.WriteLine("Test Passed");
            return 100;
        }

        Console.WriteLine("Test Failed");
        return 1;
    }
}
