// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace hello_tryfinallytryfinally_basics_cs
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
            expectedOut.WriteLine("In finally");
            expectedOut.WriteLine("In try");
            expectedOut.WriteLine("In finally");

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
                Console.WriteLine("In try");
            }
            finally
            {
                Console.WriteLine("In finally");
            }
            try
            {
                Console.WriteLine("In try");
            }
            finally
            {
                Console.WriteLine("In finally");
            }

            // stop recoding
            testLog.StopRecording();

            return testLog.VerifyOutput();
        }
    }
}
