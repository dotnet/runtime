// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Non local exit from a catch handler nested inside another catch handler

using System;
using Xunit;

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
        expectedOut.WriteLine("In try2");
        expectedOut.WriteLine("In try3");
        expectedOut.WriteLine("In catch3");
        expectedOut.WriteLine("In finally2");
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
            try
            {
                Console.WriteLine("In try2");
                try
                {
                    Console.WriteLine("In try3");
                    throw new Exception();
                }
                catch
                {
                    Console.WriteLine("In catch3");
                    goto L;
                }
            }
            finally
            {
                Console.WriteLine("In finally2");
            }
        }
        finally
        {
            Console.WriteLine("In finally1");
        }

        Console.WriteLine("Never executed");
        L:
        Console.WriteLine("Done");
        // stop recoding
        testLog.StopRecording();

        return testLog.VerifyOutput();
    }
}
