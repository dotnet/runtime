// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Throw from outside of an EH region

using System;

public class a
{
    private static TestUtil.TestLog testLog;

    static a()
    {
        // Create test writer object to hold expected output
        System.IO.StringWriter expectedOut = new System.IO.StringWriter();

        // Write expected output to string writer object
        expectedOut.WriteLine("In middle method, throwing");
        expectedOut.WriteLine("Caught");
        expectedOut.WriteLine("Pass");

        // Create and initialize test log object
        testLog = new TestUtil.TestLog(expectedOut);
    }

    public static void MiddleMethod()
    {
        Console.WriteLine("In middle method, throwing");
        throw new Exception();
    }

    public static int Main(string[] args)
    {
        //Start recording
        testLog.StartRecording();

        try
        {
            MiddleMethod();
        }
        catch
        {
            Console.WriteLine("Caught");
        }
        Console.WriteLine("Pass");

        // stop recoding
        testLog.StopRecording();

        return testLog.VerifyOutput();
    }
}
