// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Try/finally error case, one function
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
            expectedOut.WriteLine("in Try catch");
            expectedOut.WriteLine("in Try finally");
            expectedOut.WriteLine("in Finally");
            expectedOut.WriteLine("Caught exception");
            expectedOut.WriteLine("in Catch");

            // Create and initialize test log object
            testLog = new TestUtil.TestLog(expectedOut);
        }

        static public void inTry1()
        {
            Console.WriteLine("in Try catch");
        }
        static public void inTry2()
        {
            Console.WriteLine("in Try finally");
        }

        static public void inCatch()
        {
            Console.WriteLine("in Catch");
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
                inTry1();
                try
                {
                    inTry2();
                    throw new Exception();
                }
                finally
                {
                    inFinally();
                    throw new Exception();
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Caught exception");
                inCatch();
            }

            // stop recoding
            testLog.StopRecording();

            return testLog.VerifyOutput();
        }
    }
}
