// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Throwing an exception in a class (static) constructor.
// NDPWhidbey 10958

using System;

public class Foo
{
    public static int x;

    static Foo()
    {
        int y = 0;
        x = 5 / y;
    }
}

public class Test
{
    private static TestUtil.TestLog testLog;

    static Test()
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

    public static int Main()
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
