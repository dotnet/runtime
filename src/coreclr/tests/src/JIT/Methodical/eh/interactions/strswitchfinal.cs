// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/* switch with string values contained in a loop with various try/catch and try/finally constructs */

using System;

namespace strswitch
{
    internal class Class1
    {
        private static TestUtil.TestLog s_testLog;

        static Class1()
        {
            System.IO.StringWriter expectedOut = new System.IO.StringWriter();

            expectedOut.WriteLine("s == one");
            expectedOut.WriteLine("In inner finally");
            expectedOut.WriteLine("In outer finally\n");
            expectedOut.WriteLine("s == two");
            expectedOut.WriteLine("After two");
            expectedOut.WriteLine("In inner finally");
            expectedOut.WriteLine("In outer finally\n");
            expectedOut.WriteLine("s == three");
            expectedOut.WriteLine("After three");
            expectedOut.WriteLine("Ok");
            expectedOut.WriteLine("After after three");
            expectedOut.WriteLine("In inner finally");
            expectedOut.WriteLine("Caught an exception\n");
            expectedOut.WriteLine("Ok\n");
            expectedOut.WriteLine("In outer finally\n");

            expectedOut.WriteLine("In four's finally");
            expectedOut.WriteLine("In inner finally");
            expectedOut.WriteLine("Caught an exception\n");

            expectedOut.WriteLine("Ok\n");

            expectedOut.WriteLine("In outer finally\n");

            expectedOut.WriteLine("s == five");
            expectedOut.WriteLine("Five's finally 0");
            expectedOut.WriteLine("Five's finally 1");
            expectedOut.WriteLine("Five's finally 2");
            expectedOut.WriteLine("In inner finally");
            expectedOut.WriteLine("In outer finally\n");

            expectedOut.WriteLine("Greater than five");
            expectedOut.WriteLine("in six's finally");
            expectedOut.WriteLine("In inner finally");
            expectedOut.WriteLine("In outer finally\n");

            s_testLog = new TestUtil.TestLog(expectedOut);
        }

        private static int Main(string[] args)
        {
            string[] s = { "one", "two", "three", "four", "five", "six" };
            s_testLog.StartRecording();
            for (int i = 0; i < s.Length; i++)
            {
            beginloop:
                try
                {
                    try
                    {
                        try
                        {
                            switch (s[i])
                            {
                                case "one":
                                    try
                                    {
                                        Console.WriteLine("s == one");
                                    }
                                    catch
                                    {
                                        Console.WriteLine("Exception at one");
                                    }
                                    break;
                                case "two":
                                    try
                                    {
                                        Console.WriteLine("s == two");
                                    }
                                    finally
                                    {
                                        Console.WriteLine("After two");
                                    }
                                    break;
                                case "three":
                                    try
                                    {
                                        try
                                        {
                                            Console.WriteLine("s == three");
                                        }
                                        catch (System.Exception e)
                                        {
                                            Console.WriteLine(e);
                                            goto continueloop;
                                        }
                                    }
                                    finally
                                    {
                                        Console.WriteLine("After three");
                                        try
                                        {
                                            switch (s[s.Length - 1])
                                            {
                                                case "six":
                                                    Console.WriteLine("Ok");
                                                    Console.WriteLine(s[s.Length]);
                                                    goto label2;
                                                default:
                                                    try
                                                    {
                                                        Console.WriteLine("Ack");
                                                        goto label;
                                                    }
                                                    catch
                                                    {
                                                        Console.WriteLine("I don't think so ...");
                                                    }
                                                    break;
                                            }
                                        label:
                                            Console.WriteLine("Unreached");
                                            throw new Exception();
                                        }
                                        finally
                                        {
                                            Console.WriteLine("After after three");
                                        }
                                    label2:
                                        Console.WriteLine("Unreached");
                                    }
                                    goto continueloop;

                                case "four":
                                    try
                                    {
                                        try
                                        {
                                            Console.WriteLine("s == " + s[s.Length]);
                                            try
                                            {
                                            }
                                            finally
                                            {
                                                Console.WriteLine("Unreached");
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
                                                    Console.WriteLine("unreached ");
                                                    goto finishfour;
                                                }
                                                finally
                                                {
                                                    Console.WriteLine("also unreached");
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
                                        Console.WriteLine("In four's finally");
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
                                                Console.WriteLine("s == five");
                                            }
                                            finally
                                            {
                                                Console.WriteLine("Five's finally 0");
                                            }
                                        }
                                        catch (Exception)
                                        {
                                            Console.WriteLine("Unreached");
                                        }
                                        finally
                                        {
                                            Console.WriteLine("Five's finally 1");
                                        }
                                        break;
                                    }
                                    finally
                                    {
                                        Console.WriteLine("Five's finally 2");
                                    }
                                default:
                                    try
                                    {
                                        Console.WriteLine("Greater than five");
                                        goto finish;
                                    }
                                    finally
                                    {
                                        Console.WriteLine("in six's finally");
                                    }
                            };
                            continue;
                        }
                        finally
                        {
                            Console.WriteLine("In inner finally");
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Caught an exception\n");

                        switch (s[i])
                        {
                            case "three":
                                if (e is System.IndexOutOfRangeException)
                                {
                                    Console.WriteLine("Ok\n");
                                    i++;
                                    goto beginloop;
                                }
                                Console.WriteLine("Unreached\n");
                                break;
                            case "four":
                                if (e is System.IndexOutOfRangeException)
                                {
                                    Console.WriteLine("Ok\n");
                                    i++;
                                    goto beginloop;
                                }
                                Console.WriteLine("Unreached\n");
                                break;
                            default:
                                Console.WriteLine("****** Unreached");
                                goto continueloop;
                        }
                    }

                    Console.WriteLine("Unreached");
                }
                finally
                {
                    Console.WriteLine("In outer finally\n");
                }

            continueloop:
                Console.WriteLine("Continuing");
            }
        finish:
            s_testLog.StopRecording();

            return s_testLog.VerifyOutput();
        }
    }
}
