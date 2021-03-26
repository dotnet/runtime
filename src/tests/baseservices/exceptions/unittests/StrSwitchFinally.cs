// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

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

    public static int Main()
    {
        int nSuccesses = 0;
        int nFailures = 0;

        CountResults(new StrSwitchFinalTest().Run(),            ref nSuccesses, ref nFailures);
        
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

class StrSwitchFinalTest
{
    Trace _trace;
    static string _expected;
    
    static StrSwitchFinalTest()
    {
        // Create test writer object to hold expected output
        System.IO.StringWriter expectedOut = new System.IO.StringWriter();
        
        // Write expected output to string writer object
        expectedOut.WriteLine("s == one");
        expectedOut.WriteLine("In inner finally");
        expectedOut.WriteLine("In outer finally\r\n");
        expectedOut.WriteLine("s == two");
        expectedOut.WriteLine("After two");
        expectedOut.WriteLine("In inner finally");
        expectedOut.WriteLine("In outer finally\r\n");
        expectedOut.WriteLine("s == three");
        expectedOut.WriteLine("After three");
        expectedOut.WriteLine("Ok");
        expectedOut.WriteLine("After after three");
        expectedOut.WriteLine("In inner finally");
        expectedOut.WriteLine("Caught an exception\r\n");
        expectedOut.WriteLine("Ok\r\n");
        expectedOut.WriteLine("In outer finally\r\n");
        
        expectedOut.WriteLine("In four's finally");
        expectedOut.WriteLine("In inner finally");
        expectedOut.WriteLine("Caught an exception\r\n");
        
        expectedOut.WriteLine("Ok\r\n");
        
        expectedOut.WriteLine("In outer finally\r\n");
        
        expectedOut.WriteLine("s == five");
        expectedOut.WriteLine("Five's finally 0");
        expectedOut.WriteLine("Five's finally 1");
        expectedOut.WriteLine("Five's finally 2");
        expectedOut.WriteLine("In inner finally");
        expectedOut.WriteLine("In outer finally\r\n");
        
        expectedOut.WriteLine("Greater than five");
        expectedOut.WriteLine("in six's finally");
        expectedOut.WriteLine("In inner finally");
        expectedOut.WriteLine("In outer finally\r\n");

        _expected = expectedOut.ToString();
    }

    public int Run()
    {
        _trace = new Trace("StrSwitchFinalTest", _expected);
        
        string[] s = {"one", "two", "three", "four", "five", "six"};

        for(int i = 0; i < s.Length; i++) 
        {

        beginloop:
            try 
            {
                try 
                {
                    try 
                    {
                        switch(s[i]) 
                        {
                            case "one":
                                try 
                                {
                                    _trace.WriteLine("s == one");
                                } 
                                catch 
                                {
                                    _trace.WriteLine("Exception at one");
                                }
                                break;
                            case "two":
                                try 
                                {
                                    _trace.WriteLine("s == two");
                                } 
                                finally 
                                {
                                    _trace.WriteLine("After two");
                                }
                                break;
                            case "three":
                                try 
                                {
                                    try 
                                    {
                                        _trace.WriteLine("s == three");
                                    } 
                                    catch(System.Exception e) 
                                    {
                                        _trace.WriteLine(e.ToString());
                                        goto continueloop;
                                    }
                                } 
                                finally 
                                {
                                    _trace.WriteLine("After three");
                                    try 
                                    { 
                                        switch(s[s.Length-1]) 
                                        {
                                            case "six":
                                                _trace.WriteLine("Ok");
                                                _trace.WriteLine(s[s.Length]);
                                                goto label2;
                                            default:
                                                try 
                                                { 
                                                    _trace.WriteLine("Ack");
                                                    goto label;
                                                } 
                                                catch 
                                                {
                                                    _trace.WriteLine("I don't think so ...");
                                                }
                                                break;
                                        }
                                    label:
                                        _trace.WriteLine("Unreached");
                                        throw new Exception();
                                    } 
                                    finally 
                                    {
                                        _trace.WriteLine("After after three");
                                    }
                                label2:
                                    _trace.WriteLine("Unreached");
                        
                                }
                                goto continueloop;

                            case "four":
                                try 
                                {
                                    try 
                                    {
                                        _trace.WriteLine("s == " + s[s.Length]);
                                        try 
                                        {
                                        } 
                                        finally 
                                        {
                                            _trace.WriteLine("Unreached");
                                        }
                                    } 
                                    catch (Exception e) 
                                    {
                                        goto test;
                                    rethrowex:
                                        throw;
                                    test:
                                        if (e is System.ArithmeticException) 
                                        {

                                            try 
                                            {
                                                _trace.WriteLine("unreached ");
                                                goto finishfour;
                                            } 
                                            finally 
                                            {
                                                _trace.WriteLine("also unreached");
                                            }
                                        } 
                                        else 
                                        {
                                            goto rethrowex;
                                        }
                                    }
                                } 
                                finally 
                                {
                                    _trace.WriteLine("In four's finally");
                                }
                                finishfour:
                                    break;
                            case "five":
                                try 
                                {
                                    try 
                                    {
                                        try 
                                        {

                                            _trace.WriteLine("s == five");
                                        } 
                                        finally 
                                        {
                                            _trace.WriteLine("Five's finally 0");
                                        }
                                    } 
                                    catch (Exception) 
                                    {
                                        _trace.WriteLine("Unreached");
                                    } 
                                    finally 
                                    {
                                        _trace.WriteLine("Five's finally 1");
                                    }
                                    break;
                                } 
                                finally 
                                {
                                    _trace.WriteLine("Five's finally 2");
                                }
                            default:
                                try 
                                {
                                    _trace.WriteLine("Greater than five");
                                    goto finish;
                                } 
                                finally 
                                {
                                    _trace.WriteLine("in six's finally");
                        
                                }
                    
                        };
                        continue;
                    } 
                    finally 
                    {
                        _trace.WriteLine("In inner finally");
                    }
                }
                catch (Exception e) 
                {
                    _trace.WriteLine("Caught an exception\r\n");
                                            
                    switch(s[i]) 
                    {
                        case "three":
                            if (e is System.IndexOutOfRangeException) 
                            {
                                _trace.WriteLine("Ok\r\n");
                                i++;
                                goto beginloop;
                            }
                            _trace.WriteLine("Unreached\r\n");
                            break;
                        case "four":
                            if (e is System.IndexOutOfRangeException) 
                            {
                                _trace.WriteLine("Ok\r\n");
                                i++;
                                goto beginloop;
                            }
                            _trace.WriteLine("Unreached\r\n");
                            break;
                        default:
                            _trace.WriteLine("****** Unreached");
                            goto continueloop;
                    }
                    
                }

                _trace.WriteLine("Unreached");
            } 
            finally 
            {
                _trace.WriteLine("In outer finally\r\n");
            }

        continueloop:
            _trace.WriteLine("Continuing");
         
        }
        finish:

        return _trace.Match();
    }
}


