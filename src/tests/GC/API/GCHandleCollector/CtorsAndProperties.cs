// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

public class Handler
{
    private int _totalTestCount = 0;


    public bool GetInitialThresholdTest()
    {
        _totalTestCount++;
        int count = 0;

        HandleCollector hc = null;

        int[] initialValues = { 0, 1, 2, 3, 1000, 10000, Int32.MaxValue / 2, Int32.MaxValue };

        foreach (int i in initialValues)
        {
            hc = new HandleCollector(null, i);
            if (hc.InitialThreshold == i)
                count++;
        }

        if (count != initialValues.Length)
        {
            Console.WriteLine("GetInitialThresholdTest Failed!");
            return false;
        }

        return true;
    }


    public bool GetMaximumThresholdTest()
    {
        _totalTestCount++;
        int count = 0;

        HandleCollector hc = null;

        int[] maxValues = { 0, 1, 2, 3, 1000, 10000, Int32.MaxValue / 2, Int32.MaxValue };

        foreach (int i in maxValues)
        {
            hc = new HandleCollector(null, 0, i);
            if (hc.MaximumThreshold == i)
                count++;
        }

        if (count != maxValues.Length)
        {
            Console.WriteLine("GetMaximumThresholdTest Failed!");
            return false;
        }

        return true;
    }


    public bool GetName()
    {
        _totalTestCount++;
        int count = 0;

        HandleCollector hc = null;

        string[] names = { String.Empty, "a", "name", "name with spaces", new String('a', 50000), "\uA112\uA0E4\uA0F9" };

        foreach (string s in names)
        {
            hc = new HandleCollector(s, 0);
            if (hc.Name == s)
                count++;
        }

        if (count != names.Length)
        {
            Console.WriteLine("GetNameTest Failed!");
            return false;
        }

        return true;
    }


    public bool RunTest()
    {
        int count = 0;

        if (GetInitialThresholdTest())
        {
            count++;
        }

        if (GetMaximumThresholdTest())
        {
            count++;
        }

        if (GetName())
        {
            count++;
        }


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
