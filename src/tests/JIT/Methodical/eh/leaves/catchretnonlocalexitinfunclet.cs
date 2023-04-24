// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// A nonlocal exit and a catchret in a funclet where the destination label is also in the same funclet 
// cause confusion when we're building FG for the funclet (114611)


using System;
using Xunit;

namespace hello_catchretnonlocalexitinfunclet_leaves_cs
{
    public class Class1
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
        [Fact]
        static public int TestEntryPoint()
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

