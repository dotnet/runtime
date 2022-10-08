// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Test_tryCatchFinallyThrow_nonlocalexit1_cs
{
public class Class1
{
    private static TestUtil.TestLog testLog;

    static Class1()
    {
        // Create test writer object to hold expected output
        System.IO.StringWriter expectedOut = new System.IO.StringWriter();

        // Write expected output to string writer object
        expectedOut.WriteLine("in main try");
        expectedOut.WriteLine("  in middle1 try");
        expectedOut.WriteLine("  in middle1 finally");
        expectedOut.WriteLine("in main catch");

        // Create and initialize test log object
        testLog = new TestUtil.TestLog(expectedOut);
    }

    static public void Middle1(int i)
    {
        try
        {
            Console.WriteLine("  in middle1 try");
            if (i == 0) goto L1;
        }
        catch
        {
            Console.WriteLine("  in middle1 catch");
        }
        finally
        {
            Console.WriteLine("  in middle1 finally");
            if (i == 0) throw new Exception();
        }
        Console.WriteLine("after finally");
        L1:
        Console.WriteLine("middle1 L1");
    }

    [Fact]
    static public int TestEntryPoint()
    {
        // start recording
        testLog.StartRecording();

        int i = Environment.TickCount != 0 ? 0 : 1;
        try
        {
            Console.WriteLine("in main try");
            Middle1(i);
        }
        catch
        {
            Console.WriteLine("in main catch");
        }

        // stop recoding
        testLog.StopRecording();

        return testLog.VerifyOutput();
    }
}
}
