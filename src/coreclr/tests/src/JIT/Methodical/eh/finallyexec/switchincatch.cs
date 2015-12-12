// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// switch in catch 

using System;

namespace strswitch
{
    /// <summary>
    /// Summary description for Class1.
    /// </summary>
    class Class1
    {
        private static TestUtil.TestLog testLog;

        static Class1()
        {
            // Create test writer object to hold expected output
            System.IO.StringWriter expectedOut = new System.IO.StringWriter();

            // Write expected output to string writer object
            expectedOut.WriteLine("In inner finally");
            expectedOut.WriteLine("In outer finally\n");
            expectedOut.WriteLine("In inner finally");
            expectedOut.WriteLine("In outer finally\n");
            expectedOut.WriteLine("In inner finally");
            expectedOut.WriteLine("In outer finally\n");
            expectedOut.WriteLine("In inner finally");
            expectedOut.WriteLine("In outer finally\n");
            expectedOut.WriteLine("In inner finally");
            expectedOut.WriteLine("In outer finally\n");
            expectedOut.WriteLine("In inner finally");
            expectedOut.WriteLine("In outer finally\n");

            // Create and initialize test log object
            testLog = new TestUtil.TestLog(expectedOut);
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static int Main(string[] args)
        {
            string[] s = { "one", "two", "three", "four", "five", "six" };
            //Start recording
            testLog.StartRecording();

            for (int i = 0; i < s.Length; i++)
            {

                beginloop:
                try
                {
                    try
                    {
                        try
                        {
                            continue;
                        }
                        finally
                        {
                            Console.WriteLine("In inner finally");
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        switch (s[i])
                        {
                            case "three":
                                i++;
                                goto beginloop;
                            default:
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
            // stop recoding
            testLog.StopRecording();
            return testLog.VerifyOutput();

        }
    }
}
