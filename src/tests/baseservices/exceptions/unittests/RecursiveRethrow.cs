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

       CountResults(new RecursiveRethrow().Run(),              ref nSuccesses, ref nFailures);
        
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

public class RecursiveRethrow
{
    Trace _trace;

    public int Run()
    {
        _trace = new Trace("RecursiveRethrow", "210C0C1C2RecursionIsFun");
        
        try
        {
            LoveToRecurse(2);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
			_trace.Write(e.Message);
        }

        return _trace.Match();
    }


    void SeparatorMethod(int i)
    {
        LoveToRecurse(i);
    }

    void LoveToRecurse(int i)
    {
        try
        {
            _trace.Write(i.ToString());
            if (0 == i)
            {
                throw new Exception("RecursionIsFun");
            }
            else
            {
                SeparatorMethod(i - 1);
            }
        }
        catch (Exception e)
        {
            _trace.Write("C" + i.ToString());
            Console.WriteLine(e);
            throw e;
        }
    }
}

