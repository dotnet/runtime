// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// A method throws an exception in both try and finally nested in try/catch.
// NDPWhidbey 10962

using System;

namespace Test
{
    class Class1
    {

        private static TestUtil.TestLog testLog;

        static Class1()
        {
            // Create test writer object to hold expected output
            System.IO.StringWriter expectedOut = new System.IO.StringWriter();

            // Write expected output to string writer object
            expectedOut.WriteLine("try 1");
            expectedOut.WriteLine("\ttry 1.1");
            expectedOut.WriteLine("\tfinally 1.1");
            expectedOut.WriteLine("finally 1");
            expectedOut.WriteLine("try 1");
            expectedOut.WriteLine("\ttry 1.1");
            expectedOut.WriteLine("\tfinally 1.1");
            expectedOut.WriteLine("finally 1");

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
                    test();
                }
                finally
                {
                    test();
                }
            }
            catch
            {
            }

            // stop recoding
            testLog.StopRecording();

            return testLog.VerifyOutput();
        }

        static void test()
        {
            try
            {
                Console.WriteLine("try 1");
                try
                {
                    Console.WriteLine("\ttry 1.1");
                }
                finally
                {
                    Console.WriteLine("\tfinally 1.1");
                    throw new Exception();
                }
            }
            finally
            {
                Console.WriteLine("finally 1");
            }
        }

    }

}
