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

        CountResults(new PendingTest().Run(),                   ref nSuccesses, ref nFailures);
        
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


class PendingTest
{
    Trace _trace;
    
    void f3()
    {
        throw new Exception();
    } 

    void f2()
    {
        try
        {
            _trace.Write("1");
            f3();
        } 
        catch(Exception e) 
        {
            Console.WriteLine(e);
            _trace.Write("2");
            throw;
        }
    }

    void f1()
    {
        try
        {
            _trace.Write("0");
            f2();
        }
        catch(Exception e)
        {
            Console.WriteLine(e);
            _trace.Write("3");
            throw e;
        }
    }

    public int Run()
    {
        _trace = new Trace("PendingTest", "0123401235");
            
        try
        {
            f1();
        }
        catch(Exception e) 
        {
            Console.WriteLine(e);
            _trace.Write("4");
        }

        try
        {
            f1();
        }
        catch(Exception e) 
        {
            Console.WriteLine(e);
            _trace.Write("5");
        }

        return _trace.Match();
    }
}



