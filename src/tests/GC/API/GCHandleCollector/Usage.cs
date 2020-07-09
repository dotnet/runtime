// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/* TEST: Usage
 * DESCRIPTION: Three usage scenarios that monitor the number of live handles and GC Collections
 */

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// the class that holds the HandleCollectors
public class HandleCollectorTest
{
    private static HandleCollector s_hc = new HandleCollector("hc", 100);

    public HandleCollectorTest()
    {
        s_hc.Add();
    }

    public static int Count
    {
        get { return s_hc.Count; }
    }

    ~HandleCollectorTest()
    {
        s_hc.Remove();
    }

    public static void Reset()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        s_hc = new HandleCollector("hc", 100);
    }
}


public class Usage
{
    private int _numTests = 0;
    private int _numInstances = 100;
    private const int deltaPercent = 10;


    // ensures GC Collections occur when handle count exceeds maximum
    private bool Case1()
    {
        _numTests++;

        // clear GC
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        int original = GC.CollectionCount(0);

        HandleCollectorTest h;

        // create objects and let them go out of scope
        for (int i = 0; i < _numInstances; i++)
            h = new HandleCollectorTest();

        h = null;

        GC.WaitForPendingFinalizers();

        // Collection should not have occurred
        if (GC.CollectionCount(0) != original)
        {
            Console.WriteLine("Early collection!");
            Console.WriteLine("Case 1 Failed!");
            return false;
        }

        new HandleCollectorTest();

        if ((GC.CollectionCount(0) - original) > 0)
        {
            Console.WriteLine("Case 1 Passed!");
            return true;
        }

        Console.WriteLine("Expected collection did not occur!");
        Console.WriteLine("Case 1 Failed!");
        return false;
    }

    // ensures GC Collection does not occur when handle count stays below maximum
    private bool Case2()
    {
        _numTests++;
        int handleCount = 0;

        for (int i = 0; i < _numInstances; i++)
        {
            new HandleCollectorTest();

            GC.WaitForPendingFinalizers();

            handleCount = HandleCollectorTest.Count;
            //Note that the GC should occur when handle count is 101 but it will happen at anytime after a creation and we stick to the previous
            //count to avoid error
        }

        Console.WriteLine("{0}, {1}", handleCount, _numInstances);

        if (handleCount == _numInstances)
        {
            Console.WriteLine("Case 2 Passed!");
            return true;
        }

        Console.WriteLine("Case 2 Failed!");
        return false;
    }


    // ensures GC Collections frequency decrease by threshold
    private bool Case3()
    {
        _numTests++;

        int gcCount = GC.CollectionCount(2);
        int handleCount = HandleCollectorTest.Count;
        int prevHandleCount = HandleCollectorTest.Count;

        List<HandleCollectorTest> list = new List<HandleCollectorTest>();

        for (int i = 0; i < deltaPercent; i++)
        {
            do
            {
                HandleCollectorTest h = new HandleCollectorTest();
                if ((HandleCollectorTest.Count % 2) == 0)
                    list.Add(h);
                GC.WaitForPendingFinalizers();
                if (GC.CollectionCount(2) != gcCount)
                {
                    gcCount = GC.CollectionCount(2);
                    break;
                }
                else
                    handleCount = HandleCollectorTest.Count;
            } while (true);

            // ensure threshold is increasing
            if (!CheckPercentageIncrease(handleCount, prevHandleCount))
            {
                Console.WriteLine("Percentage not increasing, performing Collect/WFPF/Collect cycle");
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                if (handleCount == HandleCollectorTest.Count)
                {
                    Console.WriteLine("No handles finalized in Collect/WFPF/Collect cycle");
                    return false;
                }
            }
            prevHandleCount = handleCount;
        }


        Console.WriteLine("Case 3 Passed!");
        return true;
    }


    // Checks that the threshold increases are within 0.2 error margine of deltaPercent
    private bool CheckPercentageIncrease(int current, int previous)
    {
        bool retValue = true;
        if (previous != 0)
        {
            double value = ((double)(current - previous)) / (double)previous;
            double expected = (double)deltaPercent / 100;
            double errorMargin = Math.Abs((double)(value - expected) / (double)expected);
            retValue = (errorMargin < 0.2);
        }

        return retValue;
    }


    public bool RunTest()
    {
        int numPassed = 0;

        if (Case1())
        {
            numPassed++;
        }

        HandleCollectorTest.Reset();

        if (Case2())
        {
            numPassed++;
        }

        HandleCollectorTest.Reset();

        if (Case3())
        {
            numPassed++;
        }

        return (numPassed == _numTests);
    }


    public static int Main()
    {
        if (GC.CollectionCount(0) > 20)
        {
            Console.WriteLine("GC Stress is enabled");
            Console.WriteLine("Abort Test");
            return 100;
        }

        Usage u = new Usage();

        if (u.RunTest())
        {
            Console.WriteLine();
            Console.WriteLine("Test Passed!");
            return 100;
        }

        Console.WriteLine();
        Console.WriteLine("Test Failed!");
        return 1;
    }
}
