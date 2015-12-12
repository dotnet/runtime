// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


// 119019
// execute the same throw in handler (int f1, f2) twice (accomplished by calling f1 twice)

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
            System.Exception exp = new System.Exception();
            expectedOut.WriteLine("In f1");
            expectedOut.WriteLine("In f2");
            expectedOut.WriteLine("In f2's catch " + exp.Message);
            expectedOut.WriteLine("In f1's catch " + exp.Message);
            expectedOut.WriteLine("In main's catch1 " + exp.Message);
            expectedOut.WriteLine("In f1");
            expectedOut.WriteLine("In f2");
            expectedOut.WriteLine("In f2's catch " + exp.Message);
            expectedOut.WriteLine("In f1's catch " + exp.Message);
            expectedOut.WriteLine("In main's catch2 " + exp.Message);

            // Create and initialize test log object
            testLog = new TestUtil.TestLog(expectedOut);
        }

        static public void f3()
        {
            throw new Exception();
        }

        static public void f2()
        {
            try
            {
                Console.WriteLine("In f2");
                f3();
            }
            catch (Exception e)
            {
                Console.WriteLine("In f2's catch " + e.Message);
                throw;
            }
        }

        static public void f1()
        {
            try
            {
                Console.WriteLine("In f1");
                f2();
            }
            catch (Exception e)
            {
                Console.WriteLine("In f1's catch " + e.Message);
                throw;
            }
        }

        static public int Main(string[] args)
        {
            //Start recording
            testLog.StartRecording();

            try
            {
                f1();
            }
            catch (Exception e)
            {
                Console.WriteLine("In main's catch1 " + e.Message);
            }

            try
            {
                f1();
            }
            catch (Exception e)
            {
                Console.WriteLine("In main's catch2 " + e.Message);
            }

            // stop recoding
            testLog.StopRecording();

            return testLog.VerifyOutput();
        }
    }
}

