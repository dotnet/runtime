// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*
A test to make sure that calls to finally for nonlocal exits are
done outside of trybody 
    try {
        goto nonlocal_exit;
    } finally {
        throw; // if the finally is being called from trybody, the finally will be executed 
           // multiple times
    }
*/
using System;

namespace hello
{
    class Class1
    {

        private static TestUtil.TestLog testLog;

        static Class1()
        {
            // Create test writer object to hold expected output
            System.IO.StringWriter expectedOut = new System.IO.StringWriter();

            // Write expected output to string writer object
            expectedOut.WriteLine("In main's try");
            expectedOut.WriteLine("in finally");
            expectedOut.WriteLine("caught in main");
            expectedOut.WriteLine("Passed");

            // Create and initialize test log object
            testLog = new TestUtil.TestLog(expectedOut);
        }

        static public void Middle(int i)
        {
            try
            {
                if (i == 0) goto done;
                Console.WriteLine("in try");
            }
            finally
            {
                Console.WriteLine("in finally");
                if (i == 0) throw new Exception();
            }
            Console.WriteLine("after finally");
            done:
            Console.WriteLine("done");

        }

        static public int Main(string[] args)
        {
            //Start recording
            testLog.StartRecording();
            try
            {
                Console.WriteLine("In main's try");
                Middle(args.Length);
            }
            catch
            {
                Console.WriteLine("caught in main");
            }
            Console.WriteLine("Passed");

            // stop recoding
            testLog.StopRecording();

            return testLog.VerifyOutput();
        }
    }
}

