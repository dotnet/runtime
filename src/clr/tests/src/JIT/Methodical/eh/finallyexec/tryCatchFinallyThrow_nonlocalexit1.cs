// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

class Class1
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

    static public int Main(string[] args)
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
