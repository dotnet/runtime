// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public class Program
{

    private static TestUtil.TestLog testLog;

    static Program()
    {
        // Create test writer object to hold expected output
        System.IO.StringWriter expectedOut = new System.IO.StringWriter();

        // Write expected output to string writer object
        expectedOut.WriteLine("  in try");
        expectedOut.WriteLine("  in finally");
        expectedOut.WriteLine("  in try");
        expectedOut.WriteLine("  in finally");
        expectedOut.WriteLine("in finally");

        // Create and initialize test log object
        testLog = new TestUtil.TestLog(expectedOut);
    }

    internal static void Test(int count)
    {
        try
        {
            try
            {
                L1:
                try
                {
                    Console.WriteLine("  in try");
                    if (count-- == 0) return;
                }
                finally
                {
                    Console.WriteLine("  in finally");
                }
                goto L1;
            }
            finally
            {
                Console.WriteLine("in finally");
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    [Fact]
    public static int TestEntryPoint()
    {
        // start recording
        testLog.StartRecording();

        // run the test
        Test(1);

        // stop recoding
        testLog.StopRecording();

        return testLog.VerifyOutput();
    }
}
