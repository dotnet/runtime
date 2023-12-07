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

        CountResults(new ThrowInCatchTest().Run(),              ref nSuccesses, ref nFailures);
        
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

class ThrowInCatchTest
{
    Trace _trace;
    
    public int Run()
    {
        _trace = new Trace("ThrowInCatchTest", "0123456");
        _trace.Write("0");
        try 
        {
            _trace.Write("1");
            try 
            {
                _trace.Write("2");
                throw new Exception(".....");
            } 
            catch(Exception e)
            {
                Console.WriteLine(e);
                _trace.Write("3");
                throw new Exception("5");
            }
        } 
        catch(Exception e)
        {
            Console.WriteLine(e);
            _trace.Write("4");
            _trace.Write(e.Message);
        }
        _trace.Write("6");
        return _trace.Match();
    }
}


