// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Test_tryCatchFinallyThrow_nonlocalexit3_cs
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
        expectedOut.WriteLine("  in middle1 L1A");
        expectedOut.WriteLine("  in middle1 try");
        expectedOut.WriteLine("  in middle1 finally");
        expectedOut.WriteLine("    in middle2 L2A");
        expectedOut.WriteLine("    in middle2 try");
        expectedOut.WriteLine("    in middle2 finally");
        expectedOut.WriteLine("    in middle2 L2A");
        expectedOut.WriteLine("    in middle2 try");
        expectedOut.WriteLine("    in middle2 finally");
        expectedOut.WriteLine("    in middle2 L2A");
        expectedOut.WriteLine("    in middle2 try");
        expectedOut.WriteLine("    in middle2 finally");
        expectedOut.WriteLine("in main catch");
        expectedOut.WriteLine("in main finally");

        // Create and initialize test log object
        testLog = new TestUtil.TestLog(expectedOut);
    }

    static public void Middle1(int i)
    {
        L1A:
        Console.WriteLine("  in middle1 L1A");
        try
        {
            Console.WriteLine("  in middle1 try");
            if (i == 0) goto L1B;
            goto L1A;
        }
        catch
        {
            Console.WriteLine("  in middle1 catch");
        }
        finally
        {
            Console.WriteLine("  in middle1 finally");
            Middle2(i + 1);
            if (i == 0) throw new Exception();
            i--;
        }
        Console.WriteLine("  after middle1 finally");
        L1B:
        Console.WriteLine("  in middle1 L1");
    }

    static public void Middle2(int i)
    {
        L2A:
        Console.WriteLine("    in middle2 L2A");
        try
        {
            Console.WriteLine("    in middle2 try");
            if (i == 0) goto L2B;
            goto L2A;
        }
        catch
        {
            Console.WriteLine("    in middle2 catch");
        }
        finally
        {
            Console.WriteLine("    in middle2 finally");
            if (i == 0) throw new Exception();
            i--;
        }
        Console.WriteLine("    after middle2 finally");
        L2B:
        Console.WriteLine("    in middle2 L2B");
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
            Middle1(i + 1);
        }
        catch
        {
            Console.WriteLine("in main catch");
        }
        finally
        {
            Console.WriteLine("in main finally");
        }

        // stop recoding
        testLog.StopRecording();

        return testLog.VerifyOutput();
    }
}
}
