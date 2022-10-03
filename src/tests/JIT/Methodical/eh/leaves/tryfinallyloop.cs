// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace hello_tryfinallyloop_leaves_cs
{
    public class Class1
    {
        private static TestUtil.TestLog testLog;

        static Class1()
        {
            // Create test writer object to hold expected output
            System.IO.StringWriter expectedOut = new System.IO.StringWriter();

            // Write expected output to string writer object
            expectedOut.WriteLine("in loop, i = 0");
            expectedOut.WriteLine("in loop, i = 1");
            expectedOut.WriteLine("in loop, i = 2");
            expectedOut.WriteLine("in loop, i = 3");
            expectedOut.WriteLine("in loop, i = 4");
            expectedOut.WriteLine("in loop, i = 5");
            expectedOut.WriteLine("in loop, i = 6");
            expectedOut.WriteLine("in loop, i = 7");
            expectedOut.WriteLine("in loop, i = 8");
            expectedOut.WriteLine("in loop, i = 9");
            expectedOut.WriteLine("in Finally\n");
            expectedOut.WriteLine("Done");

            // Create and initialize test log object
            testLog = new TestUtil.TestLog(expectedOut);
        }
        static public void inTry()
        {
            Console.WriteLine("in Try\n");
            throw new Exception();
        }

        static public void inFinally()
        {
            Console.WriteLine("in Finally\n");
        }
        [Fact]
        static public int TestEntryPoint()
        {
            int i = 0;
            //Start recording
            testLog.StartRecording();
            try
            {
                L:
                Console.WriteLine("in loop, i = " + i);
                i += 1;
                if (i == 10) goto finish;
                goto L;

            }
            finally
            {
                inFinally();
            }
            finish:
            Console.WriteLine("Done");
            // stop recoding
            testLog.StopRecording();

            return testLog.VerifyOutput();

        }
    }
}

