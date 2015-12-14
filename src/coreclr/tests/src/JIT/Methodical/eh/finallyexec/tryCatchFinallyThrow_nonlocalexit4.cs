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
        expectedOut.WriteLine("-in foo try [1]");
        expectedOut.WriteLine("--in foo try [2]");
        expectedOut.WriteLine("--in foo finally [2]");
        expectedOut.WriteLine("-in foo catch [1]");
        expectedOut.WriteLine("-in foo finally [1]");
        expectedOut.WriteLine("in main catch");

        // Create and initialize test log object
        testLog = new TestUtil.TestLog(expectedOut);
    }

    static public void foo(int i)
    {
        try
        {
            Console.WriteLine("-in foo try [1]");
            try
            {
                Console.WriteLine("--in foo try [2]");
                if (i == 0) goto L1;
            }
            catch
            {
                Console.WriteLine("--in foo catch [2]");
            }
            finally
            {
                Console.WriteLine("--in foo finally [2]");
                if (i == 0) throw new Exception();
            }
        }
        catch
        {
            Console.WriteLine("-in foo catch [1]");
        }
        finally
        {
            Console.WriteLine("-in foo finally [1]");
            if (i == 0) throw new Exception();
        }
        Console.WriteLine("after finally");
        L1:
        Console.WriteLine("foo L1");
    }


    static public int Main(string[] args)
    {
        // start recording
        testLog.StartRecording();

        int i = Environment.TickCount != 0 ? 0 : 1;
        try
        {
            Console.WriteLine("in main try");
            foo(i);
        }
        catch
        {
            Console.WriteLine("in main catch");
        }

        // stop recording
        testLog.StopRecording();

        return testLog.VerifyOutput();
    }
}
