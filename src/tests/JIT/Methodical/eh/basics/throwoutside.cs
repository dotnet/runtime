// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Throw from outside of an EH region

using System;
using Xunit;

namespace Test_throwoutside_basics
{
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
            Console.WriteLine("Caught");
        }
        Console.WriteLine("Pass");

        // stop recoding
        testLog.StopRecording();

        return testLog.VerifyOutput();
    }
}
}
