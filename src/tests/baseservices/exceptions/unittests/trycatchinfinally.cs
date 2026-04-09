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

		CountResults(new TryCatchInFinallyTest().Run(),         ref nSuccesses, ref nFailures);
        
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

class TryCatchInFinallyTest
{
    Trace _trace;
    
    public int Run()
    {
        _trace = new Trace("TryCatchInFinallyTest", "0123456");
        
        _trace.Write("0");
        try
        {
            _trace.Write("1");
        }
        finally
        {
            _trace.Write("2");
            try
            {
                _trace.Write("3");
                throw new InvalidProgramException();
            }
            catch(InvalidProgramException e)
            {
                Console.WriteLine(e);
                _trace.Write("4");
            }
            _trace.Write("5");
        }
        _trace.Write("6");

        return _trace.Match();
    }
}
