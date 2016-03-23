// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/* CollectionCountTest
 *
 * Tests GC.CollectionCount by passing it invalid values (<0) and
 * values that are too large (>GC.MaxGeneration).
 * It then tests valid values (0<=x<=GC.MaxGeneration)
 * by making sure result is at least the number of manual collections
 * (GC.Collect) per generation (must be at least, since the GC may collect
 * on it's own).
 */

using System;

public class CollectionCountTest
{
    private const int numTests = 3;

    private Int32[] _negValues = { -1, -10, -10000, Int32.MinValue };
    private Int32[] _largeValues = { GC.MaxGeneration + 1, Int32.MaxValue / 2, Int32.MaxValue - 1, Int32.MaxValue };

    private CollectionCountTest()
    {
    }

    // Checks that CollectionCount correctly counts collections to higher generations
    public bool CollectionTest()
    {
        GC.Collect(2);
        if (GC.CollectionCount(2) < 1)
        {
            Console.WriteLine("Failure at CollectionTest(2)");
            return false;
        }

        GC.Collect(1);
        if (GC.CollectionCount(1) < 2)
        {
            Console.WriteLine("Failure at CollectionTest(1)");
            return false;
        }

        GC.Collect(0);
        if (GC.CollectionCount(0) < 3)
        {
            Console.WriteLine("Failure at CollectionTest(0)");
            return false;
        }

        Console.WriteLine("CollectionTest passed");
        return true;
    }

    // Checks that CollectionCount correctly throws an exception on values < 0
    public bool NegativeTest()
    {
        bool retVal = true;

        foreach (int i in _negValues)
        {
            try
            {
                GC.CollectionCount(i);
                retVal = false;
            }
            catch (ArgumentOutOfRangeException)
            {
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                retVal = false;
            }
            if (!retVal)
            {
                Console.WriteLine("Failure at NegativeTest");
                break;
            }
        }

        if (retVal)
            Console.WriteLine("NegativeTest passed");
        return retVal;
    }


    // Checks that CollectionCount returns 0 when passed 0
    public bool LargeValuesTest()
    {
        bool retVal = true;

        foreach (int i in _largeValues)
        {
            try
            {
                retVal = (GC.CollectionCount(i) == 0);
                if (!retVal)
                {
                    Console.WriteLine("Failure at LargeValueTest: {0}", i);
                    break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                retVal = false;
            }
        }
        if (retVal)
            Console.WriteLine("LargeValueTest passed");
        return retVal;
    }


    public bool RunTest()
    {
        int passedCount = 0;

        if (NegativeTest())
            passedCount++;
        if (LargeValuesTest())
            passedCount++;
        if (CollectionTest())
            passedCount++;


        return (passedCount == numTests);
    }


    public static int Main()
    {
        CollectionCountTest test = new CollectionCountTest();

        if (test.RunTest())
        {
            Console.WriteLine("Test Passed");
            return 100;
        }

        Console.WriteLine("Test Failed");
        return 1;
    }
}
