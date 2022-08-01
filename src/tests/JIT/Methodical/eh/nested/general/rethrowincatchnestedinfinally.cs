// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Re-throw an exception in catch nested in finally which is nested in try with catch and finally.
// NDPWhidbey 10959

using System;
using Xunit;

namespace Test_rethrowincatchnestedinfinally_cs
{

    public class Class1
    {

        private static TestUtil.TestLog testLog;

        static Class1()
        {
            // Create test writer object to hold expected output
            System.IO.StringWriter expectedOut = new System.IO.StringWriter();

            // Write expected output to string writer object
            expectedOut.WriteLine(" try");
            expectedOut.WriteLine("\t try");
            expectedOut.WriteLine("\t finally");
            expectedOut.WriteLine("\t\t try \t [throwing an exception here]");
            expectedOut.WriteLine("\t\t catch \t [re-throwing the same exception]");
            expectedOut.WriteLine(" catch");
            expectedOut.WriteLine(" finally");
            expectedOut.WriteLine(" inside loop i = 0");
            expectedOut.WriteLine(" inside loop i = 1");
            expectedOut.WriteLine(" inside loop i = 2");
            expectedOut.WriteLine(" inside loop i = 3");

            // Create and initialize test log object
            testLog = new TestUtil.TestLog(expectedOut);
        }

        [Fact]
        static public int TestEntryPoint()
        {
            //Start recording
            testLog.StartRecording();

            try
            {
                Console.WriteLine(" try");
                try
                {
                    Console.WriteLine("\t try");
                }
                finally
                {
                    Console.WriteLine("\t finally");
                    try
                    {
                        Console.WriteLine("\t\t try \t [throwing an exception here]");
                        int x = 0;
                        int y = 5 / x;
                    }
                    catch (System.Exception)
                    {
                        Console.WriteLine("\t\t catch \t [re-throwing the same exception]");
                        throw;
                    }
                }
            }
            catch (System.Exception)
            {
                Console.WriteLine(" catch");
            }
            finally
            {
                Console.WriteLine(" finally");
            }
            for (int i = 0; i < 4; ++i)
            {
                Console.WriteLine(" inside loop i = " + i);
            }

            // stop recoding
            testLog.StopRecording();

            return testLog.VerifyOutput();
        }
    }
}

