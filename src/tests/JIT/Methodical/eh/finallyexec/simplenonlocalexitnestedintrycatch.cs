// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// try/finally embedded in a try catch with a nonlocal exit 
using System;
using Xunit;

namespace hello_simplenonlocalexitnestedintrycatch_finallyexec_cs
{
    public class Class1
    {
        private static TestUtil.TestLog testLog;
        static Class1()
        {
            // Create test writer object to hold expected output
            System.IO.StringWriter expectedOut = new System.IO.StringWriter();

            // Write expected output to string writer object
            expectedOut.WriteLine("in finally");
            expectedOut.WriteLine("done");

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
                    goto done;
                    Console.WriteLine("in try");
                }
                finally
                {
                    Console.WriteLine("in finally");
                }
                Console.WriteLine("after finally");
            }
            catch
            {
                Console.WriteLine("caught in main");
            }
            done:
            Console.WriteLine("done");
            // stop recoding
            testLog.StopRecording();

            return testLog.VerifyOutput();
        }

    }
}

