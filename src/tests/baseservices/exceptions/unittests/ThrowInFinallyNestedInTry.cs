// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Xunit;

//
// main
//

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

        CountResults(new ThrowInFinallyNestedInTryTest().Run(), ref nSuccesses, ref nFailures); // FAIL: needs skip to parent code <TODO> investigate </TODO>
        
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

public class ThrowInFinallyNestedInTryTest 
{
    Trace _trace;
    
    void MiddleMethod() 
    {
        _trace.Write("2");
        try 
        {
            _trace.Write("3");
            try 
            {
                _trace.Write("4");
            } 
            finally 
            {
                _trace.Write("5");
                try 
                {
                    _trace.Write("6");
                    throw new System.ArgumentException();
                } 
                finally 
                {
                    _trace.Write("7");
                }
            }
        } 
        finally 
        {
            _trace.Write("8");
        }
    }

    public int Run()
    {
        _trace = new Trace("ThrowInFinallyNestedInTryTest", "0123456789a");
        
        _trace.Write("0");
        try 
        {
            _trace.Write("1");
            MiddleMethod();
        } 
        catch(Exception e)
        {
            Console.WriteLine(e);
            _trace.Write("9");
        }
        _trace.Write("a");
        
        return _trace.Match();
    }
}


