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

       CountResults(new RecurseTest().Run(),                   ref nSuccesses, ref nFailures);
        
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

class RecurseTest
{
    Trace _trace;
    
    void DoTest(int level)
    {
        _trace.Write(level.ToString());
        if (level <= 0)
            return;

        try
        {
            throw new Exception("" + (level - 1));
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            _trace.Write(e.Message);
            DoTest(level - 2);
        }
    }

    public int Run()
    {
        int     n = 8;
        string  expected = "";

        // create expected result string
        for (int i = n; i >= 0; i--)
        {
            expected += i.ToString();
        }

        _trace = new Trace("RecurseTest", expected);
        
        DoTest(n);

        return _trace.Match();
    }
}

