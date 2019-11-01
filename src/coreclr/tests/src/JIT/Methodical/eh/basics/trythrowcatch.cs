// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Try catch error case
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
            expectedOut.WriteLine("In catch");

            // Create and initialize test log object
            testLog = new TestUtil.TestLog(expectedOut);
        }

        static public void inTry()
        {
            Console.WriteLine("In try");
            throw new Exception();
        }

        static public void inCatch()
        {
            Console.WriteLine("In catch");
        }
        static public void inFinally() { }

        static public int Main(string[] args)
        {
            //Start recording
            testLog.StartRecording();

            try
            {
                inTry();
            }
            catch (Exception)
            {
                inCatch();
            }

            // stop recoding
            testLog.StopRecording();

            return testLog.VerifyOutput();
        }
    }
}
