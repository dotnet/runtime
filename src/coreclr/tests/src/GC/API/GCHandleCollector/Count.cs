// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

public class Count
{
    private int _totalTestCount = 0;


    // count should be 0 by default
    public bool EmptyTest()
    {
        _totalTestCount++;

        HandleCollector hc = new HandleCollector(null, 1);

        if (hc.Count != 0)
        {
            Console.WriteLine("EmptyTest Failed!");
            return false;
        }

        Console.WriteLine("EmptyTest Passed!");
        return true;
    }


    public bool AddTest()
    {
        _totalTestCount++;


        HandleCollector hc = new HandleCollector(null, 1);

        for (int i = 1; i <= 1000; i++)
        {
            hc.Add();
            if (hc.Count != i)
            {
                Console.WriteLine("AddTest Failed!");
                return false;
            }
        }

        Console.WriteLine("AddTest Passed!");
        return true;
    }


    public bool RemoveTest()
    {
        _totalTestCount++;


        HandleCollector hc = new HandleCollector(null, 1);

        for (int i = 1; i <= 1000; i++)
        {
            hc.Add();
        }

        for (int i = 999; i >= 0; i--)
        {
            hc.Remove();

            if (hc.Count != i)
            {
                Console.WriteLine("RemoveTest Failed!");
                return false;
            }
        }

        Console.WriteLine("RemoveTest Passed!");
        return true;
    }



    public bool StressTest()
    {
        _totalTestCount++;


        HandleCollector hc = new HandleCollector(null, 1);

        for (int i = 1; i <= 10000000; i++)
        {
            hc.Add();
            if (hc.Count != i)
            {
                Console.WriteLine("StressTest1 Failed!");
                return false;
            }
        }


        for (int i = 9999999; i <= 0; i++)
        {
            hc.Remove();
            if (hc.Count != i)
            {
                Console.WriteLine("StressTest2 Failed!");
                return false;
            }
        }

        Console.WriteLine("StressTest Passed!");
        return true;
    }



    public bool MixedTest()
    {
        _totalTestCount++;


        HandleCollector hc = new HandleCollector(null, 1);

        int i, j, k;

        for (i = 1; i <= 100; i++)
        {
            hc.Add();
            if (hc.Count != i)
            {
                Console.WriteLine("MixedTest1 Failed!");
                return false;
            }
        }

        i--;

        for (j = 1; j <= 50; j++)
        {
            hc.Remove();

            if (hc.Count != i - j)
            {
                Console.WriteLine("MixedTest2 Failed!");
                return false;
            }
        }

        j--;

        for (k = 1; k <= 50; k++)
        {
            hc.Add();
            if (hc.Count != (i - j) + k)
            {
                Console.WriteLine("MixedTest3 Failed!");
                return false;
            }
        }

        k--;

        // do check here
        if (hc.Count != (i - j + k))
        {
            Console.WriteLine("MixedTest Failed!");
            Console.WriteLine("Count: {0}", hc.Count);
            Console.WriteLine("{0}", (i - j + k));
            return false;
        }

        Console.WriteLine("MixedTest Passed!");
        return true;
    }


    public bool RunTest()
    {
        int count = 0;

        if (EmptyTest())
        {
            count++;
        }

        if (AddTest())
        {
            count++;
        }

        if (RemoveTest())
        {
            count++;
        }


        if (StressTest())
        {
            count++;
        }

        if (MixedTest())
        {
            count++;
        }

        Console.WriteLine();
        return (count == _totalTestCount);
    }


    public static int Main()
    {
        Count c = new Count();

        if (c.RunTest())
        {
            Console.WriteLine("Test Passed!");
            return 100;
        }

        Console.WriteLine("Test Failed!");
        return 1;
    }
}
