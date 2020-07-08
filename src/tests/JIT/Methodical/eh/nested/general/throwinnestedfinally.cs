// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Throw from a try block nested in 2 finallys

using System;

public class a
{
    private static TestUtil.TestLog testLog;

    static a()
    {
        // Create test writer object to hold expected output
        System.IO.StringWriter expectedOut = new System.IO.StringWriter();

        // Write expected output to string writer object
        expectedOut.WriteLine("In outer try - 0");
        expectedOut.WriteLine("In outer finally - 0");
        expectedOut.WriteLine("In outer try - 1");
        expectedOut.WriteLine("In outer finally - 1");
        expectedOut.WriteLine("In inner try");
        expectedOut.WriteLine("In inner finally");
        expectedOut.WriteLine("Pass");

        // Create and initialize test log object
        testLog = new TestUtil.TestLog(expectedOut);
    }

    public static void MiddleMethod()
    {
        try
        {
            Console.WriteLine("In outer try - 0");
        }
        finally
        {
            Console.WriteLine("In outer finally - 0");
            try
            {
                Console.WriteLine("In outer try - 1");
            }
            finally
            {
                Console.WriteLine("In outer finally - 1");
                try
                {
                    Console.WriteLine("In inner try");
                    throw new System.ArgumentException();
                    Console.WriteLine("Unreached");
                }
                finally
                {
                    Console.WriteLine("In inner finally");
                }
            }
        }
        Console.WriteLine("Unreached...");
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
            Console.WriteLine("Pass");
        }

        // stop recoding
        testLog.StopRecording();

        return testLog.VerifyOutput();
    }
}
