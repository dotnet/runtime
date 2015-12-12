// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Throw from a try block nested in finally, non error case

using System;

public class a
{
    private static TestUtil.TestLog testLog;

    static a()
    {
        // Create test writer object to hold expected output
        System.IO.StringWriter expectedOut = new System.IO.StringWriter();

        // Write expected output to string writer object
        expectedOut.WriteLine("In outer try");
        expectedOut.WriteLine("In outer finally");
        expectedOut.WriteLine("In inner try");
        expectedOut.WriteLine("In inner catch");
        expectedOut.WriteLine("Done.");

        // Create and initialize test log object
        testLog = new TestUtil.TestLog(expectedOut);
    }

    public static void MiddleMethod()
    {
        try
        {
            Console.WriteLine("In outer try");
        }
        finally
        {
            Console.WriteLine("In outer finally");
            try
            {
                Console.WriteLine("In inner try");
                throw new System.ArgumentException();
                Console.WriteLine("Unreached");
            }
            catch
            {
                Console.WriteLine("In inner catch");
            }
        }
        Console.WriteLine("Done.");
    }

    public static int Main(string[] args)
    {
        //Start recording
        testLog.StartRecording();

        MiddleMethod();

        // stop recoding
        testLog.StopRecording();

        return testLog.VerifyOutput();
    }
}
