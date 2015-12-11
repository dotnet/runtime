// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// simple rethrow test 

using System;

namespace hello
{
    class Class1
    {
        private static TestUtil.TestLog testLog;

        static Class1()
        {
            // Create test writer object to hold expected output
            System.IO.StringWriter expectedOut = new System.IO.StringWriter();

            // Write expected output to string writer object
            expectedOut.WriteLine("In outer try");
            expectedOut.WriteLine("In inner try");
            expectedOut.WriteLine("In inner catch");
            expectedOut.WriteLine("In outer catch");

            // Create and initialize test log object
            testLog = new TestUtil.TestLog(expectedOut);
        }

        static public int Main(string[] args)
        {
            //Start recording
            testLog.StartRecording();

            try
            {
                Console.WriteLine("In outer try");
                try
                {
                    Console.WriteLine("In inner try");
                    throw new Exception();
                }
                catch
                {
                    Console.WriteLine("In inner catch");
                    throw;
                }
            }
            catch
            {
                Console.WriteLine("In outer catch");
            }

            // stop recoding
            testLog.StopRecording();

            return testLog.VerifyOutput();
        }
    }
}

