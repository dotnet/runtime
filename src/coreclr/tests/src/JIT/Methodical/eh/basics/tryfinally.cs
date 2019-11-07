// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Try finally, non error case
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
            expectedOut.WriteLine("in Try");
            expectedOut.WriteLine("in Finally");
            expectedOut.WriteLine("done");

            // Create and initialize test log object
            testLog = new TestUtil.TestLog(expectedOut);
        }

        static public void inTry()
        {
            Console.WriteLine("in Try");
        }

        static public void inFinally()
        {
            Console.WriteLine("in Finally");
        }

        static public int Main(string[] args)
        {
            //Start recording
            testLog.StartRecording();

            try
            {
                inTry();
            }
            finally
            {
                inFinally();
            }
            Console.WriteLine("done");

            // stop recoding
            testLog.StopRecording();

            return testLog.VerifyOutput();
        }
    }
}
