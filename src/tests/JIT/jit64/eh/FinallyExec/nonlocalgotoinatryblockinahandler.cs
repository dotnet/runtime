// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//112256
// [karimf] If we happen to have a try that is in nested CATCHs, and the try has a nonLocalGoto 
// [only legal if it gos all the way back to the root!], then we replace the nonlocal 
// LEAVE with an OPGOTO to the beginning of the cascading CATCHRET chain to unwind the stack...

using System;
using Xunit;

public class simple
{
    private static TestUtil.TestLog testLog;

    static simple()
    {
        // Create test writer object to hold expected output
        System.IO.StringWriter expectedOut = new System.IO.StringWriter();

        // Write expected output to string writer object
        expectedOut.WriteLine("Begin test...");
        expectedOut.WriteLine("In main try");
        expectedOut.WriteLine("In main catch");
        expectedOut.WriteLine("In inner try");
        expectedOut.WriteLine("End test...");
        // Create and initialize test log object
        testLog = new TestUtil.TestLog(expectedOut);
    }
    [Fact]
    public static int TestEntryPoint()
    {
        //Start recording
        testLog.StartRecording();
        Console.WriteLine("Begin test...");
        try
        {
            Console.WriteLine("In main try");

            throw new Exception();
        }
        catch
        {
            Console.WriteLine("In main catch");

            try
            {
                Console.WriteLine("In inner try");
                goto L;
            }
            catch
            {
                Console.WriteLine("In inner catch");
            }
        }

        Console.WriteLine("DEAD CODE!");

        L:

        Console.WriteLine("End test...");
        // stop recoding
        testLog.StopRecording();

        return testLog.VerifyOutput();
    }
}
