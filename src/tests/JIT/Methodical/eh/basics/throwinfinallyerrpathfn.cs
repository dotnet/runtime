// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Throw from a finally,  error case

using System;
using Xunit;

namespace Test_throwinfinallyerrpathfn_basics
{
public class a
{
    private static TestUtil.TestLog testLog;

    static a()
    {
        // Create test writer object to hold expected output
        System.IO.StringWriter expectedOut = new System.IO.StringWriter();

        // Write expected output to string writer object
        expectedOut.WriteLine("In try, throwing");
        expectedOut.WriteLine("In finally, throwing");
        expectedOut.WriteLine("Pass");

        // Create and initialize test log object
        testLog = new TestUtil.TestLog(expectedOut);
    }

    public static void MiddleMethod()
    {
        try
        {
            Console.WriteLine("In try, throwing");
            throw new Exception();
            Console.WriteLine("Unreached");
        }
        finally
        {
            Console.WriteLine("In finally, throwing");
            throw new Exception();
        }
        Console.WriteLine("Unreached...");
    }

    [Fact]
    public static int TestEntryPoint()
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
}
