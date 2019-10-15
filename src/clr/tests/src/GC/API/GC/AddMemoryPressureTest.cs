// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/* AddMemoryPressureTest
 *
 * Tests GC.AddMemoryPressure by passing it values that are too small (<=0) and
 * values that are too large (>Int32.MaxValue on 32-bit).
 * The stress test doubles the pressure (2xInt32.MaxValue), and verifies
 * valid behaviour.
 */




using System;
using System.Diagnostics;
using System.Security;
using System.Runtime.InteropServices;

public class Dummy
{
    private long _pressure = 0;
    private int _numTimes = 0;

    public Dummy(bool heavy)
    {
        if (heavy)
        {
            _pressure = Int32.MaxValue;
            _numTimes = 2;
            for (int i = 0; i < _numTimes; i++)
                GC.AddMemoryPressure(_pressure);
        }
    }

    ~Dummy()
    {
        for (int i = 0; i < _numTimes; i++)
            GC.RemoveMemoryPressure(_pressure);
    }
}

public class AddMemoryPressureTest
{
    public int TestCount = 0;

    private long[] _negValues = { 0, -1, Int32.MinValue - (long)1, Int64.MinValue / (long)2, Int64.MinValue };
    private long[] _largeValues = { Int32.MaxValue + (long)1, Int64.MaxValue };


    private AddMemoryPressureTest()
    {
    }


    public bool TooSmallTest()
    {
        TestCount++;
        bool retVal = true;

        foreach (long i in _negValues)
        {
            try
            {
                GC.AddMemoryPressure(i);
                Console.WriteLine("Failure at TooSmallTest: {0}", i);
                retVal = false;
                break;
            }
            catch (ArgumentOutOfRangeException)
            {
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Failure at TooSmallTest: {0}", i);
                retVal = false;
                break;
            }
        }

        if (retVal)
            Console.WriteLine("TooSmallTest Passed");
        return retVal;
    }


    public bool TooLargeTest()
    {
        TestCount++;

        bool retVal = true;

        foreach (long i in _largeValues)
        {
            try
            {
                GC.AddMemoryPressure(i);
                // this should not throw exception on 64-bit
                if (IntPtr.Size == Marshal.SizeOf(new Int32()))
                {
                    Console.WriteLine("Failure at LargeValueTest: {0}", i);
                    retVal = false;
                    break;
                }
                else
                {
                    GC.RemoveMemoryPressure(i);
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                // this should not throw exception on 64-bit
                if (IntPtr.Size == Marshal.SizeOf(new Int64()))
                {
                    Console.WriteLine("Failure at LargeValueTest: {0}", i);
                    retVal = false;
                    break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                retVal = false;
                break;
            }
        }

        if (retVal)
            Console.WriteLine("TooLargeTest Passed");
        return retVal;
    }


    public bool StressTest()
    {
        TestCount++;

        Console.WriteLine("StressTest Started...");

        int gcCount1 = createDummies(true);


        int gcCount2 = createDummies(false);

        Console.WriteLine("{0} {1}", gcCount1, gcCount2);
        if (gcCount1 > gcCount2)
        {
            Console.WriteLine("StressTest Passed");
            Console.WriteLine();
            return true;
        }

        Console.WriteLine("StressTest Failed");

        Console.WriteLine();
        return false;
    }


    private int createDummies(bool heavy)
    {
        int gcCount = GC.CollectionCount(0);

        for (int i = 0; i < 100; i++)
        {
            Dummy dummy = new Dummy(heavy);
            int gen = GC.GetGeneration(dummy);
            if (gen != 0)
            {
                Console.WriteLine("Warning: newly-allocated dummy ended up in gen {0}", gen);
            }
        }

        return GC.CollectionCount(0) - gcCount;
    }


    public bool RunTest()
    {
        int passCount = 0;

        if (TooSmallTest())
            passCount++;

        if (TooLargeTest())
            passCount++;

        if (StressTest())
            passCount++;

        return (passCount == TestCount);
    }


    public static int Main()
    {
        AddMemoryPressureTest test = new AddMemoryPressureTest();

        if (test.RunTest())
        {
            Console.WriteLine("Test Passed");
            return 100;
        }

        Console.WriteLine("Test Failed");
        return 1;
    }
}
