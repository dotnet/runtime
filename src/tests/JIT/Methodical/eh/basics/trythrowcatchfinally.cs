// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// try { throw } catch {} finally {}
using System;
using Xunit;

namespace Test_trythrowcatchfinally_cs
{
public class test
{
    private static TestUtil.TestLog testLog;

    static test()
    {
        // Create test writer object to hold expected output
        System.IO.StringWriter expectedOut = new System.IO.StringWriter();

        // Write expected output to string writer object
        expectedOut.WriteLine("In try1");
        expectedOut.WriteLine("In catch1");
        expectedOut.WriteLine("In finally1");
        expectedOut.WriteLine("Done");

        // Create and initialize test log object
        testLog = new TestUtil.TestLog(expectedOut);
    }

    [Fact]
    public static int TestEntryPoint()
    {
        //Start recording
        testLog.StartRecording();

        try
        {
            Console.WriteLine("In try1");

            throw new Exception();
        }
        catch (Exception)
        {
            Console.WriteLine("In catch1");

        }
        finally
        {
            Console.WriteLine("In finally1");
        }

        Console.WriteLine("Done");

        // stop recoding
        testLog.StopRecording();

        return testLog.VerifyOutput();
    }
}
}
