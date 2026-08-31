// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Throwing an exception in a class (static) constructor.
// NDPWhidbey 10958

using System;
using Xunit;

public class Foo
{
    public static int x;

    static Foo()
    {
        int y = 0;
        x = 5 / y;
    }
}

public class Test_throwinclassconstructor
{
    private static TestUtil.TestLog testLog;

    static Test_throwinclassconstructor()
    {
        // Create test writer object to hold expected output
        System.IO.StringWriter expectedOut = new System.IO.StringWriter();

        // Write expected output to string writer object
        expectedOut.WriteLine(" try");
        expectedOut.WriteLine("\t try");
        expectedOut.WriteLine("\t finally");
        expectedOut.WriteLine(" catch");

        // Create and initialize test log object
        testLog = new TestUtil.TestLog(expectedOut);
    }

    [Fact]
    public static int TestEntryPoint()
    {
        //Start recording
        testLog.StartRecording();

        int i = 0;
        try
        {
            System.Console.WriteLine(" try");
            try
            {
                System.Console.WriteLine("\t try");
                i = Foo.x;
            }
            finally
            {
                System.Console.WriteLine("\t finally");
                i = Foo.x;
            }
        }
        catch (System.Exception)
        {
            System.Console.WriteLine(" catch");
        }

        // stop recoding
        testLog.StopRecording();

        return testLog.VerifyOutput();
    }
}
