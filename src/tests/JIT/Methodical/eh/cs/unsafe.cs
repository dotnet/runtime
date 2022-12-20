// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// unsafe memory access in a funclet

using System;
using Xunit;

namespace Test_unsafe
{
public class Test
{

    private static TestUtil.TestLog testLog;

    static Test()
    {
        // Create test writer object to hold expected output
        System.IO.StringWriter expectedOut = new System.IO.StringWriter();

        // Write expected output to string writer object
        expectedOut.WriteLine("arr[1] at 0x1 is 2");
        expectedOut.WriteLine("arr[2] at 0x2 is 3");
        expectedOut.WriteLine("arr[3] at 0x3 is 4");
        expectedOut.WriteLine("arr[4] at 0x4 is 5");
        expectedOut.WriteLine("After try");
        expectedOut.WriteLine("Done");

        // Create and initialize test log object
        testLog = new TestUtil.TestLog(expectedOut);
    }

    unsafe static void WriteLocations(byte[] arr)
    {
        fixed (byte* p_arr = arr)
        {
            try
            {
                throw new Exception();
            }
            catch
            {
                byte* p_elem = p_arr;
                byte* p_prev = p_arr;
                p_elem++;
                for (int i = 1; i < arr.Length; i++)
                {
                    byte value = *p_elem;
                    Console.WriteLine("arr[{0}] at 0x{1:X} is {2}", i, (uint)(p_elem - p_prev), value);
                    p_elem++;
                }
            }
        }
    }

    [Fact]
    public static int TestEntryPoint()
    {
        //Start recording
        testLog.StartRecording();

        try
        {
            byte[] arr = new byte[] { 1, 2, 3, 4, 5 };
            WriteLocations(arr);
        }
        catch
        {
            Console.WriteLine("In catch, Unreached\n");
            goto done;
        }
        Console.WriteLine("After try");
        done:
        Console.WriteLine("Done");
        // stop recoding
        testLog.StopRecording();

        return testLog.VerifyOutput();
    }
}
}
