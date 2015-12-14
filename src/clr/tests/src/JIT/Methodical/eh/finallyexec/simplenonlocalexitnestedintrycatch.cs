// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// try/finally embedded in a try catch with a nonlocal exit 
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
            expectedOut.WriteLine("in finally");
            expectedOut.WriteLine("done");

            // Create and initialize test log object
            testLog = new TestUtil.TestLog(expectedOut);
        }

        static public int Main(string[] args)
        {
            //Start recording
            testLog.StartRecording();
            try
            {
                try
                {
                    if (args.Length == 0) goto done;
                    Console.WriteLine("in try");
                }
                finally
                {
                    Console.WriteLine("in finally");
                }
                Console.WriteLine("after finally");
            }
            catch
            {
                Console.WriteLine("caught in main");
            }
            done:
            Console.WriteLine("done");
            // stop recoding
            testLog.StopRecording();

            return testLog.VerifyOutput();
        }

    }
}

