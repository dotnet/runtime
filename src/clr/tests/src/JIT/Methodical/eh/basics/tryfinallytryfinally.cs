// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
            expectedOut.WriteLine("In try");
            expectedOut.WriteLine("In finally");
            expectedOut.WriteLine("In try");
            expectedOut.WriteLine("In finally");

            // Create and initialize test log object
            testLog = new TestUtil.TestLog(expectedOut);
        }

        static public int Main(string[] args)
        {
            //Start recording
            testLog.StartRecording();

            try
            {
                Console.WriteLine("In try");
            }
            finally
            {
                Console.WriteLine("In finally");
            }
            try
            {
                Console.WriteLine("In try");
            }
            finally
            {
                Console.WriteLine("In finally");
            }

            // stop recoding
            testLog.StopRecording();

            return testLog.VerifyOutput();
        }
    }
}
