// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Test_loopinfinally_cs
{
public class Class1
{
    private static TestUtil.TestLog testLog;

    static Class1()
    {
        // Create test writer object to hold expected output
        System.IO.StringWriter expectedOut = new System.IO.StringWriter();

        // Write expected output to string writer object
        expectedOut.WriteLine("Caught an exception");
        expectedOut.WriteLine("In finally, i = 3");
        expectedOut.WriteLine("In finally, i = 4");
        expectedOut.WriteLine("In finally, i = 5");
        expectedOut.WriteLine("In finally, i = 6");

        // Create and initialize test log object
        testLog = new TestUtil.TestLog(expectedOut);
    }

    [Fact]
    public static int TestEntryPoint()
    {
        //Start recording
        testLog.StartRecording();

        int i = 3;
        bool visited = false;

        before_try:
        try
        {
            if (i == 3)
            {
                throw new IndexOutOfRangeException();
            }
        }
        catch (Exception)
        {
            //to prevent infinite loops
            if (visited) { Console.WriteLine("Error, finally never called..."); goto early_exit; }

            Console.WriteLine("Caught an exception");

            visited = true;
            goto before_try;
        }
        finally
        {
            inside_finally:
            Console.WriteLine("In finally, i = {0}", i);
            i++;
            if (i % 2 == 0) goto inside_finally;
        }

        early_exit:

        // stop recoding
        testLog.StopRecording();

        return testLog.VerifyOutput();
    }
}
}
