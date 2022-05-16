// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Throw in catch handler

using System;
using Xunit;

namespace hello_throwincatch_basics_cs
{
    public class Class1
    {
        private static TestUtil.TestLog testLog;

        static Class1()
        {
            // Create test writer object to hold expected output
            System.IO.StringWriter expectedOut = new System.IO.StringWriter();

            // Write expected output to string writer object
            expectedOut.WriteLine("In try");
            expectedOut.WriteLine("In try 2, 1st throw");
            expectedOut.WriteLine("In 1st catch, 2nd throw");
            expectedOut.WriteLine("In 2nd catch");
            expectedOut.WriteLine("Done");

            // Create and initialize test log object
            testLog = new TestUtil.TestLog(expectedOut);
        }

        [Fact]
        static public int TestEntryPoint()
        {
            //Start recording
            testLog.StartRecording();

            try
            {
                try
                {
                    Console.WriteLine("In try");
                    try
                    {
                        Console.WriteLine("In try 2, 1st throw");
                        throw new Exception();
                    }
                    catch
                    {
                        Console.WriteLine("In 1st catch, 2nd throw");
                        throw new Exception();
                    }
                }
                catch
                {
                    Console.WriteLine("In 2nd catch");
                }
            }
            catch
            {
                Console.WriteLine("Unreached");
            }
            Console.WriteLine("Done");

            // stop recoding
            testLog.StopRecording();

            return testLog.VerifyOutput();
        }
    }
}
