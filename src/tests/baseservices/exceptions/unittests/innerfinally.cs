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

        CountResults(new InnerFinallyTest().Run(),              ref nSuccesses, ref nFailures);
        
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

class InnerFinallyTest
{
    Trace _trace;

    public InnerFinallyTest() 
    {
        // Create test writer object to hold expected output
        System.IO.StringWriter expectedOut = new System.IO.StringWriter();

        // Write expected output to string writer object
        expectedOut.WriteLine(" try 1");
        expectedOut.WriteLine("\t try 1.1");
        expectedOut.WriteLine("\t finally 1.1");
        expectedOut.WriteLine("\t\t try 1.1.1");
        expectedOut.WriteLine("\t\t Throwing an exception here!");
        expectedOut.WriteLine("\t\t finally 1.1.1");
        expectedOut.WriteLine(" catch 1");
        expectedOut.WriteLine(" finally 1");
        
        _trace = new Trace("InnerFinallyTest", expectedOut.ToString());
    }
    
    public int Run() 
    {
        int x = 7, y = 0, z;

        try 
        {
            _trace.WriteLine(" try 1");
            try 
            {
                _trace.WriteLine("\t try 1.1");
            } 
            finally 
            {
                _trace.WriteLine("\t finally 1.1");
                try  
                { 
                    _trace.WriteLine("\t\t try 1.1.1");
                    _trace.WriteLine("\t\t Throwing an exception here!");
                    z = x / y;
                } 
                finally  
                {
                    _trace.WriteLine("\t\t finally 1.1.1");
                }
            }
        } 
        catch (Exception) 
        {
            _trace.WriteLine(" catch 1");
        } 
        finally  
        {
            _trace.WriteLine(" finally 1");
        }
        
        return _trace.Match();
    }
}


