// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// local goto in a handler should not cause us to add the goto into the nonlocal handler map
// 112209
using System;
using Xunit;

namespace Test_localgotoinahandler_cs
{
public class Class1
{
    private static TestUtil.TestLog testLog;

    static Class1()
    {
        // Create test writer object to hold expected output
        System.IO.StringWriter expectedOut = new System.IO.StringWriter();

        // Write expected output to string writer object
        expectedOut.WriteLine("In main try...");
        expectedOut.WriteLine("In main finally...");
        expectedOut.WriteLine("In inner try 1...");
        expectedOut.WriteLine("In inner try 2...");
        expectedOut.WriteLine("Back in inner try 1...");
        expectedOut.WriteLine("Now in inner finally...");
        // Create and initialize test log object
        testLog = new TestUtil.TestLog(expectedOut);
    }
    [Fact]
    public static int TestEntryPoint()
    {
        //Start recording
        testLog.StartRecording();

        try
        {
            Console.WriteLine("In main try...");
        }
        finally
        {
            Console.WriteLine("In main finally...");

            try
            {
                Console.WriteLine("In inner try 1...");

                try
                {
                    Console.WriteLine("In inner try 2...");

                    goto LABEL;
                }
                catch
                {
                    Console.WriteLine("Will never see this catch...");
                }

                Console.WriteLine("Will never see this code, jumping over it!");

                LABEL:
                Console.WriteLine("Back in inner try 1...");

            }
            finally
            {
                Console.WriteLine("Now in inner finally...");
            }
        }
        // stop recoding
        testLog.StopRecording();

        return testLog.VerifyOutput();
    }
}
}
