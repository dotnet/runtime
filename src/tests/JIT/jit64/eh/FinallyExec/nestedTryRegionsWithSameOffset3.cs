// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

class Program
{

    private static TestUtil.TestLog testLog;

    static Program()
    {
        // Create test writer object to hold expected output
        System.IO.StringWriter expectedOut = new System.IO.StringWriter();

        // Write expected output to string writer object
        expectedOut.WriteLine("   in try");
        expectedOut.WriteLine("   in finally");
        expectedOut.WriteLine("   in try");
        expectedOut.WriteLine("   in finally");
        expectedOut.WriteLine("  in finally");
        expectedOut.WriteLine("   in try");
        expectedOut.WriteLine("   in finally");
        expectedOut.WriteLine("  in finally");
        expectedOut.WriteLine(" in finally");
        expectedOut.WriteLine("in catch");

        // Create and initialize test log object
        testLog = new TestUtil.TestLog(expectedOut);
    }

    public static void Test(int count)
    {
        try
        {
            try
            {
                L1:
                try
                {
                    try
                    {
                        L2:
                        try
                        {
                            Console.WriteLine("   in try");
                            if (count-- == 0) goto G1;
                            if (count < 0) throw new Exception();
                        }
                        finally
                        {
                            Console.WriteLine("   in finally");
                        }
                        goto L2;
                    }
                    finally
                    {
                        Console.WriteLine("  in finally");
                    }
                }
                catch (Exception)
                {
                    throw;
                }
                G1:
                goto L1;
            }
            finally
            {
                Console.WriteLine(" in finally");
            }
        }
        catch (Exception)
        {
            Console.WriteLine("in catch");
        }
    }

    static int Main(string[] args)
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
