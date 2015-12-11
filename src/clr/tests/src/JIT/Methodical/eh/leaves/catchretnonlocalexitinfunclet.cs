// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// A nonlocal exit and a catchret in a funclet where the destination label is also in the same funclet 
// cause confusion when we're building FG for the funclet (114611)


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
            expectedOut.WriteLine("1234");
            expectedOut.WriteLine("end outer catch test");
            expectedOut.WriteLine("1234");
            // Create and initialize test log object
            testLog = new TestUtil.TestLog(expectedOut);
        }
        static public int Main(string[] args)
        {
            //Start recording
            testLog.StartRecording();

            int i = 1234;
            Console.WriteLine(i);
            String s = "test";
            try
            {
                throw new Exception();
            }
            catch
            {
                try
                {
                    if (i != 0) goto incatch;
                }
                catch
                {
                    if (i != 0) goto incatch;
                    Console.WriteLine("end inner catch");
                }
                Console.WriteLine("unreached");

                incatch:
                Console.WriteLine("end outer catch " + s);
            }
            Console.WriteLine(i);
            // stop recoding
            testLog.StopRecording();

            return testLog.VerifyOutput();

        }
    }
}

