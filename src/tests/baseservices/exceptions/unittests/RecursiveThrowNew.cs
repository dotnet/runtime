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

        CountResults(new RecursiveThrowNew().Run(),             ref nSuccesses, ref nFailures);
        
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

public class RecursiveThrowNew
{
    Trace _trace;

    public int Run()
    {
        _trace = new Trace("RecursiveThrowNew", "210C0(eX)C1(e0)C2(e1)CM(e2)");
        
        try
        {
            LoveToRecurse(2);
        }
        catch (Exception e)
        {
            _trace.Write("CM(" + e.Message + ")");
            Console.WriteLine(e);
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
                throw new Exception("eX");
            }
            else
            {
                SeparatorMethod(i - 1);
            }
        }
        catch (Exception e)
        {
            _trace.Write("C" + i.ToString() + "(" + e.Message + ")");
            Console.WriteLine(e);
            throw new Exception("e" + i.ToString());
        }
    }
}


