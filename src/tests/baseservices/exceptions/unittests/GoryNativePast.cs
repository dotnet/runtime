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

        CountResults(new GoryNativePastTest().Run(),            ref nSuccesses, ref nFailures);
        
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

class GoryNativePastTest
{
    Trace _trace;
    
    void bar()
    {
        _trace.Write("2");
        throw new Exception("6");
    }

    void foo()
    {
        _trace.Write("1");
        try
        {
            bar();
        }
        finally
        {
            _trace.Write("3");
        }
    }

    public int Run()
    {
        _trace = new Trace("GoryNativePastTest", "0123456");
        
        _trace.Write("0");
        try
        {
            try 
            {
                foo();
            } 
            catch(Exception e)
            {
                Console.WriteLine(e);
                _trace.Write("4");
                throw;
            }
        }
        catch(Exception e)
        {
            _trace.Write("5");
            _trace.Write(e.Message);
        }
        return _trace.Match();
    }
}

