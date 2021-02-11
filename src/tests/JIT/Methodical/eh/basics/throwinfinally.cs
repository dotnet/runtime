// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Throw from a finally block

using System;

public class a
{
    private static TestUtil.TestLog testLog;

    static a()
    {
        // Create test writer object to hold expected output
        System.IO.StringWriter expectedOut = new System.IO.StringWriter();

        // Write expected output to string writer object
        expectedOut.WriteLine("Main: In Try");
        expectedOut.WriteLine("In try");
        expectedOut.WriteLine("In finally");
        expectedOut.WriteLine("Main: Caught the exception");

        // Create and initialize test log object
        testLog = new TestUtil.TestLog(expectedOut);
    }

    public static void MiddleMethod()
    {
        try
        {
            Console.WriteLine("In try");
        }
        finally
        {
            Console.WriteLine("In finally");
            throw new System.ArgumentException();
            //			Console.WriteLine("Unreached...");
        }
    }

    public static int Main(string[] args)
    {
        //Start recording
        testLog.StartRecording();

        try
        {
            Console.WriteLine("Main: In Try");
            MiddleMethod();
        }
        catch
        {
            Console.WriteLine("Main: Caught the exception");
        }

        // stop recoding
        testLog.StopRecording();

        return testLog.VerifyOutput();
    }
}
