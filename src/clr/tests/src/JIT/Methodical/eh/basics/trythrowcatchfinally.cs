// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// try { throw } catch {} finally {}
using System;

class test
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

    public static int Main()
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
