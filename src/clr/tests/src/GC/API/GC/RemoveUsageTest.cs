// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/* RemoveUsageTest
 *
 * Tests GC.RemoveMemoryPressure by passing a valid value (RemoveMemoryPressureTest.Pressure)
 * and making sure the objects with Removed pressure get collected less times by
 * the GC than those with pressure.
 */


using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

public class Dummy
{
    private long _pressure;

    public Dummy(long pressure)
    {
        _pressure = pressure;
        GC.AddMemoryPressure(pressure);
    }

    public Dummy() { }

    ~Dummy()
    {
        if (_pressure > 0)
            GC.RemoveMemoryPressure(_pressure);
    }
}


public class RemoveUsageTest
{
    public static int Pressure = 100000; // test will fail with values less than this
    private int _numTests = 0;

    private RemoveUsageTest()
    {
    }


    public bool RemoveTest()
    {
        _numTests++;

        int gcCount1 = GC.CollectionCount(0);
        for (int i = 0; i < 100; i++)
        {
            Dummy heavy = new Dummy(RemoveUsageTest.Pressure);
            int gen = GC.GetGeneration(heavy);
            if (gen != 0)
            {
                Console.WriteLine("Warning: newly-allocated dummy ended up in gen {0}", gen);
            }
            GC.WaitForPendingFinalizers();
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
            GC.WaitForPendingFinalizers();
        }
        gcCount2 = GC.CollectionCount(0) - gcCount2;

        Console.WriteLine("{0} {1}", gcCount1, gcCount2);
        if (gcCount1 > gcCount2)
        {
            Console.WriteLine("RemoveTest Passed");
            Console.WriteLine();
            return true;
        }

        Console.WriteLine("RemoveTest Failed");
        Console.WriteLine();
        return false;
    }

    public bool RunTest()
    {
        int numPass = 0;

        if (RemoveTest())
            numPass++;

        return (numPass == _numTests);
    }

    public static int Main()
    {
        RemoveUsageTest test = new RemoveUsageTest();

        if (test.RunTest())
        {
            Console.WriteLine("Test Passed");
            return 100;
        }

        Console.WriteLine("Test Failed");
        return 1;
    }
}
