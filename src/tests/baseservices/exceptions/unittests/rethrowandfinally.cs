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

        CountResults(new RethrowAndFinallysTest().Run(),        ref nSuccesses, ref nFailures);

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

public class RethrowAndFinallysTest
{
    Trace _trace;
    
    public int Run()
    {
        _trace = new Trace("RethrowAndFinallysTest", "abcdefF3ED2CB1A[done]");
        try 
        {
            _trace.Write("a");
            try
            {
                _trace.Write("b");
                try 
                {
                    _trace.Write("c");
                    try
                    {
                        _trace.Write("d");
                        try 
                        {
                            _trace.Write("e");
                            try
                            {
                                _trace.Write("f");
                                throw new Exception("ex1");
                            }
                            finally
                            {
                                _trace.Write("F");
                            }
                        }
                        catch(Exception e) 
                        {
                            Console.WriteLine(e);
                            _trace.Write("3");
                            throw;
                        }
                        finally
                        {
                            _trace.Write("E");
                        }
                    }
                    finally
                    {
                        _trace.Write("D");
                    }
                }
                catch(Exception e) 
                {
                    Console.WriteLine(e);
                    _trace.Write("2");
                    throw;
                }
                finally
                {
                    _trace.Write("C");
                }
            }
            finally
            {
                _trace.Write("B");
            }
        }
        catch(Exception e) 
        {
            Console.WriteLine(e);
            _trace.Write("1");
        }
        finally
        {
            _trace.Write("A");
        }

        _trace.Write("[done]");

        return _trace.Match();
    }
}


