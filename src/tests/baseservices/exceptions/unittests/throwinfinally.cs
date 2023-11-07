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

        CountResults(new ThrowInFinallyTest().Run(),            ref nSuccesses, ref nFailures);
        
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


class ThrowInFinallyTest
{
    Trace _trace;
    
    void Dumb()
    {
        _trace.Write("2");
        try
        {
            _trace.Write("3");
            try 
            {
                _trace.Write("4");
                try 
                {
                    _trace.Write("5");
                    throw new Exception("A");
                } 
                finally
                {
                    _trace.Write("6");
                    throw new Exception("B");
                }
            } 
            finally
            {
                _trace.Write("7");
                throw new Exception("C");
            }
        }
        finally
        {
            _trace.Write("8");
        }
    }

    public int Run() 
    {
        _trace = new Trace("ThrowInFinallyTest", "0123456789Ca");
        
        _trace.Write("0");
        try
        {
            _trace.Write("1");
            Dumb();
        }
        catch(Exception e)
        {
            Console.WriteLine(e);
            _trace.Write("9");
            _trace.Write(e.Message);
        }
        _trace.Write("a");
        return _trace.Match();
   }
}
