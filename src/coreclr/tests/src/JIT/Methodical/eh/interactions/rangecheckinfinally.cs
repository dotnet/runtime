// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            expectedOut.WriteLine("In finally");
            expectedOut.WriteLine("Done");
            expectedOut.WriteLine("1234");

            // Create and initialize test log object
            testLog = new TestUtil.TestLog(expectedOut);
        }
        static public int Main(string[] args)
        {
            int[] a;
            //Start recording
            testLog.StartRecording();
            a = new int[2];
            try
            {
            }
            finally
            {
                a[0] = 1234;
                Console.WriteLine("In finally");
            }
            Console.WriteLine("Done");
            Console.WriteLine(a[0]);

            // stop recoding
            testLog.StopRecording();

            return testLog.VerifyOutput();
        }
    }
}

