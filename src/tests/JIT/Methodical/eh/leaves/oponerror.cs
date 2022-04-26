// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


// catch ret inside try region
// note: this is NOT the test case
// after vswhidbey:5875 is fixed, intry will be outside the outer try block

using System;
using Xunit;

namespace hello_oponerror_leaves_cs
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
            expectedOut.WriteLine("test2");
            expectedOut.WriteLine("end outer catch test2");
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
            goto begin2;

            begin:
            String s = "test1";

            begin2:
            s = "test2";

            intry:
            try
            {
                Console.WriteLine(s);
                throw new Exception();
            }
            catch
            {
                try
                {
                    if (i == 3) goto intry; // catch ret
                    if (i >= 0) goto incatch;
                    if (i < 0) goto begin; // catch ret

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

