// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace test_uncaughtException
{

    public class Class1
    {

        private static TestUtil.TestLog testLog;

        static Class1()
        {
            // Create test writer object to hold expected output
            System.IO.StringWriter expectedOut = new System.IO.StringWriter();

            // Write expected output to string writer object
            expectedOut.WriteLine(" try 1");
            expectedOut.WriteLine("\t try 1.1");
            expectedOut.WriteLine("\t throwing an exception here!");
            expectedOut.WriteLine("\t catch 1.1");
            expectedOut.WriteLine("\t\t try 1.1.1");
            expectedOut.WriteLine("\t\t finally 1.1.1");
            expectedOut.WriteLine("\t throwing another exception here!");
            expectedOut.WriteLine(" catch 1");
            expectedOut.WriteLine(" finally 1");

            // Create and initialize test log object
            testLog = new TestUtil.TestLog(expectedOut);
        }
        [Fact]
        public static int TestEntryPoint()
        {
            //Start recording
            testLog.StartRecording();
            try
            {
                Console.WriteLine(" try 1");
                try
                {
                    Console.WriteLine("\t try 1.1");
                    Console.WriteLine("\t throwing an exception here!");
                    throw new System.ArithmeticException("My ArithmeticException");
                }
                catch (Exception)
                {
                    Console.WriteLine("\t catch 1.1");
                    goto inner_try;
                    throw_exception:
                    Console.WriteLine("\t throwing another exception here!");
                    throw new System.ArithmeticException("My ArithmeticException");
                    inner_try:
                    try
                    {
                        Console.WriteLine("\t\t try 1.1.1");
                    }
                    finally
                    {
                        Console.WriteLine("\t\t finally 1.1.1");
                    }
                    goto throw_exception;
                }
            }
            catch (Exception)
            {
                Console.WriteLine(" catch 1");
            }
            finally
            {
                Console.WriteLine(" finally 1");
            }
            // stop recoding
            testLog.StopRecording();

            return testLog.VerifyOutput();
        }
    }
}
