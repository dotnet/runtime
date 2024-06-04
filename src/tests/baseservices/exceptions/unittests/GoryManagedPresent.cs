// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Xunit;

public class TestSet
{
    static void CountResults(int testReturnValue, ref int nSuccesses, ref int nFailures)
    {
        if (100 == testReturnValue)
        {
            nSuccesses++;
        }
        else
        {
            nFailures++;
        }
    }

    [Fact]
    public static int TestEntryPoint()
    {
        int nSuccesses = 0;
        int nFailures = 0;

        CountResults(new GoryManagedPresentTest().Run(),        ref nSuccesses, ref nFailures); // FAIL: needs skip to parent code <TODO> investigate </TODO>
        
        if (0 == nFailures)
        {
            Console.WriteLine("OVERALL PASS: " + nSuccesses + " tests");
            return 100;
        }
        else
        {
            Console.WriteLine("OVERALL FAIL: " + nFailures + " tests failed");
            return 999;
        }
    }
}

class GoryManagedPresentTest
{
    Trace _trace;
    
    void foo(int dummy)
    {
        _trace.Write("1");
        try
        {
            _trace.Write("2");
            try 
            {
                _trace.Write("3");
                if (1234 == dummy)
                {
                    goto MyLabel;
                }
                _trace.Write("....");
            }
            finally
            {
                _trace.Write("4");
            }
        }
        finally
        {
            _trace.Write("5");
            if (1234 == dummy)
            {
                int i = 0;
                int q = 167 / i;
            }
        }

        _trace.Write("****");

    MyLabel:
        _trace.Write("~~~~");
    }

    public int Run()
    {
        _trace = new Trace("GoryManagedPresentTest", "0123456");
        try
        {
            _trace.Write("0");
            foo(1234);
            _trace.Write("%%%%");
        }
        catch(Exception e)
        {
            Console.WriteLine(e);
            _trace.Write("6");
        }

        return _trace.Match();
    }
}

