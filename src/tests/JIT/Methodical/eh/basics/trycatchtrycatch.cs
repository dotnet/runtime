// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace hello_trycatchtrycatch_basics_cs
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
            expectedOut.WriteLine("In try");

            // Create and initialize test log object
            testLog = new TestUtil.TestLog(expectedOut);
        }

        static public void inTry()
        {
            Console.WriteLine("In try");
        }

        static public void inCatch()
        {
            Console.WriteLine("In catch");
        }

        static public void inFinally()
        {
            Console.WriteLine("In finally");
        }

        [Fact]
        static public int TestEntryPoint()
        {
            //Start recording
            testLog.StartRecording();

            try
            {
                inTry();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                inCatch();
            }
            try
            {
                inTry();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                inCatch();
            }

            // stop recoding
            testLog.StopRecording();

            return testLog.VerifyOutput();
        }
    }
}
