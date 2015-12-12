// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
            expectedOut.WriteLine(" try 1");
            expectedOut.WriteLine("\t try 1.1");
            expectedOut.WriteLine("\t finally 1.1");
            expectedOut.WriteLine("\t\t try 1.1.1");
            expectedOut.WriteLine("\t\t Throwing an exception here!");
            expectedOut.WriteLine("\t\t finally 1.1.1");
            expectedOut.WriteLine(" catch 1");
            expectedOut.WriteLine(" finally 1");

            // Create and initialize test log object
            testLog = new TestUtil.TestLog(expectedOut);
        }
        static int Main(string[] args)
        {
            int x = 7, y = 0, z;
            //Start recording
            testLog.StartRecording();

            try
            {
                Console.WriteLine(" try 1");
                try
                {
                    Console.WriteLine("\t try 1.1");
                }
                finally
                {
                    Console.WriteLine("\t finally 1.1");
                    try
                    {
                        Console.WriteLine("\t\t try 1.1.1");
                        Console.WriteLine("\t\t Throwing an exception here!");
                        z = x / y;
                    }
                    finally
                    {
                        Console.WriteLine("\t\t finally 1.1.1");
                    }
                }
            }
            catch (Exception)
            {
                Console.WriteLine(" catch 1");
            }
            finally
            {
                Console.WriteLine(" finally 1");
            }
            // stop recoding
            testLog.StopRecording();
            return testLog.VerifyOutput();
        }
    }
}
