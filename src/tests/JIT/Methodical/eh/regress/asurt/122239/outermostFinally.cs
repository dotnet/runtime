// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace test
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
            expectedOut.WriteLine("\t\tThrowing an exception here");
            expectedOut.WriteLine("catch 1");
            expectedOut.WriteLine("finally 1");

            // Create and initialize test log object
            testLog = new TestUtil.TestLog(expectedOut);
        }

        static int Main(string[] args)
        {
            int[] array = { 1, 2, 3, 4, 5, 6 };

            //Start recording
            testLog.StartRecording();
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
                    Console.WriteLine("\t\tThrowing an exception here");
                    Console.WriteLine(array[array.Length]);
                    try
                    {
                    }
                    finally
                    {
                    }
                }
            }
            catch (IndexOutOfRangeException)
            {
                Console.WriteLine("catch 1");
            }
            finally
            {
                Console.WriteLine("finally 1");
            }
            // stop recoding
            testLog.StopRecording();

            return testLog.VerifyOutput();
        }
    }
}
