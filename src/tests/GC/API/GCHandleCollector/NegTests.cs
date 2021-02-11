// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

public class Handler
{
    private int _totalTestCount = 0;

    // tests various invalid constructor values
    public bool ConstructorTest()
    {
        _totalTestCount++;

        HandleCollector hc = null;
        int count = 0;
        int testcount = 0;

        try
        {
            testcount++;
            // negative maxThreshold
            hc = new HandleCollector(null, 0, -1);
        }
        catch (System.ArgumentOutOfRangeException)
        {
            count++;
        }

        try
        {
            testcount++;
            // negative initialThreshold
            hc = new HandleCollector(null, -1, 0);
        }
        catch (System.ArgumentOutOfRangeException)
        {
            count++;
        }

        try
        {
            testcount++;
            // negative maxThreshold & initialThreshold
            hc = new HandleCollector(null, -1, -1);
        }
        catch (System.ArgumentOutOfRangeException)
        {
            count++;
        }

        try
        {
            testcount++;
            // maxThreshold < initialThreshold
            hc = new HandleCollector(null, 1, 0);
        }
        catch (System.ArgumentException)
        {
            count++;
        }


        if (count < testcount)
        {
            Console.WriteLine("ConstructorTest Failed!");
            return false;
        }


        Console.WriteLine("ConstructorTest Passed!");
        return true;
    }


    // should throw InvalidOperationException if removing when Count == 0
    public bool RemoveTest()
    {
        _totalTestCount++;

        HandleCollector hc = new HandleCollector(null, 1);

        if (hc.Count != 0)
        {
            Console.WriteLine("Count value not zero: {0}!", hc.Count);
            Console.WriteLine("RemoveTest Aborted!");
            return false;
        }

        try
        {
            hc.Remove();
        }
        catch (InvalidOperationException)
        {
            Console.WriteLine("RemoveTest Passed!");
            return true;
        }

        Console.WriteLine("RemoveTest Failed!");
        return false;
    }


    // should throw InvalidOperationException if adding when Count == int.MaxValue
    // unfortunately this test takes too long to run (~30 mins on a 1.8MHz machine)
    public bool AddTest()
    {
        _totalTestCount++;

        HandleCollector hc = new HandleCollector(null, int.MaxValue);

        for (int i = 1; i < int.MaxValue; i++)
        {
            hc.Add();
            if (hc.Count != i)
            {
                Console.WriteLine("AddTest Failed!1");
                Console.WriteLine("i: {0}", i);
                Console.WriteLine("count: {0}", hc.Count);
                return false;
            }
        }

        try
        {
            hc.Add(); // int.MaxValue+1
        }
        catch (InvalidOperationException)
        {
            Console.WriteLine("AddTest Passed!");
            return true;
        }

        Console.WriteLine("AddTest Failed!2");
        Console.WriteLine(hc.Count);
        return false;
    }


    public bool RunTest()
    {
        int count = 0;

        if (ConstructorTest())
        {
            count++;
        }

        if (RemoveTest())
        {
            count++;
        }

        // if (AddTest()) {
        //     count++;
        // }

        return (count == _totalTestCount);
    }


    public static int Main()
    {
        Handler h = new Handler();

        if (h.RunTest())
        {
            Console.WriteLine("Test Passed!");
            return 100;
        }

        Console.WriteLine("Test Failed!");
        return 1;
    }
}
