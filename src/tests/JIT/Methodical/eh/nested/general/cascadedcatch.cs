// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// non-local exits in a catch handler nested in another catch handler

using System;
using Xunit;

namespace hello_cascadedcatch_general_cs
{
    public class Class1
    {
        private static TestUtil.TestLog testLog;

        static Class1()
        {
            // Create test writer object to hold expected output
            System.IO.StringWriter expectedOut = new System.IO.StringWriter();

            // Write expected output to string writer object
            expectedOut.WriteLine("in Try");
            expectedOut.WriteLine("in Try");
            expectedOut.WriteLine("in Try");
            expectedOut.WriteLine("L5");
            expectedOut.WriteLine("in Catch");
            expectedOut.WriteLine("in Try");
            expectedOut.WriteLine("in Catch");
            expectedOut.WriteLine("L4");
            expectedOut.WriteLine("in Try");
            expectedOut.WriteLine("in Catch");
            expectedOut.WriteLine("L4");
            expectedOut.WriteLine("L5");
            expectedOut.WriteLine("in Catch");
            expectedOut.WriteLine("in Finally");
            expectedOut.WriteLine("in Finally");

            // Create and initialize test log object
            testLog = new TestUtil.TestLog(expectedOut);
        }

        static int i;

        static public void inTry()
        {
            Console.WriteLine("in Try");
            i++;
            if (i > 3)
                throw new Exception();
        }

        static public void inCatch()
        {
            Console.WriteLine("in Catch");
        }

        static public void inFinally()
        {
            Console.WriteLine("in Finally");
        }

        [Fact]
        static public int TestEntryPoint()
        {
            string[] args = new string[] {};

            //Start recording
            testLog.StartRecording();

            i = 0;
            L1:
            if (i > 0) goto L3;
            try
            {
                inTry();
                try
                {
                    inTry();
                    L2:
                    try
                    { // catch Exception
                        inTry();
                        throw new Exception();
                    }
                    catch (Exception e)
                    {
                        L5:
                        Console.WriteLine("L5");
                        inCatch();
                        if (i == 5) goto L1;
                        try
                        { // catch System
                            inTry();
                        }
                        catch (Exception e1)
                        {
                            inCatch();
                            if (i == 0) goto L1;
                            if (i == 1) goto L2;
                            L4:
                            Console.WriteLine("L4");
                            if (i == 5) goto L5;
                            try
                            {
                                inTry();
                            }
                            catch (Exception e2)
                            {
                                inCatch();
                                if (i == 0) goto L3;
                                if (i == 1) goto L2;
                                if (i > 1) goto L4;
                                Console.WriteLine("Unreached\n");
                                try
                                {
                                    for (int ii = 0; ii < 10; ii++)
                                    {
                                        try
                                        {
                                            Console.WriteLine(args[ii]);
                                        }
                                        finally
                                        {
                                            Console.WriteLine("Unreached finally\n");
                                        }
                                    }
                                }
                                catch
                                {
                                    Console.WriteLine("Unreached catch\n");
                                    switch (i)
                                    {
                                        case 0: goto L1;
                                        case 3: goto L2;
                                        case 4: goto L4;
                                        default: break;
                                    }
                                    goto L5;
                                }
                            }
                        }
                    }
                }
                finally
                {
                    inFinally();
                }
            }
            finally
            {
                inFinally();
            }
            L3:

            // stop recoding
            testLog.StopRecording();

            return testLog.VerifyOutput();
        } // Main
    } // class
} // namespace

