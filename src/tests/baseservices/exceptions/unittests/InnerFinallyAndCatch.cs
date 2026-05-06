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

        CountResults(new InnerFinallyAndCatchTest().Run(),      ref nSuccesses, ref nFailures);
        
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

class InnerFinallyAndCatchTest
{
    Trace _trace;

    public int Run() 
    {
        _trace = new Trace("InnerFinallyAndCatchTest", "abcdefghijklm13");

        int x = 7, y = 0, z;

        int count = 0; 

        try 
        {
            _trace.Write("a");
            count++;
            try
            {
                _trace.Write("b");
                count++;
            }
            finally // 1
            {
                try
                {
                    _trace.Write("c");
                    count++;
                }
                finally // 2
                {
                    try
                    {
                        try 
                        {
                            _trace.Write("d");
                            count++;
                        } 
                        finally // 3
                        {
                            _trace.Write("e");
                            count++;
                            try  
                            { 
                                _trace.Write("f");
                                count++;
                            } 
                            finally  // 4
                            {
                                _trace.Write("g");
                                count++;
                                z = x / y;
                            }
                            _trace.Write("@@");
                            count++;
                        }
                    }
                    catch (Exception) // C2
                    {
                        _trace.Write("h");
                        count++;
                    }
                    _trace.Write("i");
                    count++;
                }
                _trace.Write("j");
                count++;
            }
            _trace.Write("k");
            count++;
        } 
        catch (Exception) // C1
        {
            _trace.Write("!!");
            count++;
        } 
        finally  // 0
        {
            _trace.Write("l");
            count++;
        }
        
        _trace.Write("m");
        count++;

        _trace.Write(count.ToString());

        return _trace.Match();
    }
}

