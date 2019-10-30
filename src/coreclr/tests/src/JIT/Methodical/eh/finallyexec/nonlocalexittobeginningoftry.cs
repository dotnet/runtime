// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// try/finally embedded in a try catch with a nonlocal exit to the beginning of try block
// to make sure that we don't execute the finally unnecessarily 
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
            expectedOut.WriteLine("in try1");
            expectedOut.WriteLine("in finally 2");
            expectedOut.WriteLine("in try1");
            expectedOut.WriteLine("in finally 1");
            expectedOut.WriteLine("done");

            // Create and initialize test log object
            testLog = new TestUtil.TestLog(expectedOut);
        }

        static public int Main(string[] args)
        {
            //Start recording
            testLog.StartRecording();
            int i = 0;
            try
            {
                begintry1:
                Console.WriteLine("in try1");
                if (i > 0) goto done;
                try
                {
                    if (args.Length == 0)
                    {
                        i++;
                        goto begintry1;
                    }
                }
                finally
                {
                    Console.WriteLine("in finally 2");
                }
            }
            finally
            {
                Console.WriteLine("in finally 1");
            }
            Console.WriteLine("after finally");
            Console.WriteLine("unreached");
            done:
            Console.WriteLine("done");
            // stop recoding
            testLog.StopRecording();

            return testLog.VerifyOutput();
        }

    }
}

