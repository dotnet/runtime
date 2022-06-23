// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// catch ret to the beginning of the inner try 
// we will need to use the il after the C# compiler is fixed

using System;
using Xunit;

namespace strswitch_catchrettoinnertry_cs
{

    public class Class1
    {
        private static TestUtil.TestLog testLog;

        static Class1()
        {
            // Create test writer object to hold expected output
            System.IO.StringWriter expectedOut = new System.IO.StringWriter();

            // Write expected output to string writer object
            expectedOut.WriteLine("Caught an exception");
            expectedOut.WriteLine("In outer finally");
            expectedOut.WriteLine("bye");
            expectedOut.WriteLine("In outer finally");

            // Create and initialize test log object
            testLog = new TestUtil.TestLog(expectedOut);
        }

        [Fact]
        public static int TestEntryPoint()
        {
            //Start recording
            testLog.StartRecording();

            int i = 3;

            beginloop:
            try
            {
                try
                {
                    if (i == 3)
                    {
                        throw new IndexOutOfRangeException();
                    }
                    else if (i == 4)
                    {
                        Console.WriteLine("bye");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Caught an exception");
                    i++;
                    goto beginloop;
                }
            }
            finally
            {
                Console.WriteLine("In outer finally");
            }

            continueloop:

            finish:
            // stop recoding
            testLog.StopRecording();

            return testLog.VerifyOutput();
        } //  main
    }
}
