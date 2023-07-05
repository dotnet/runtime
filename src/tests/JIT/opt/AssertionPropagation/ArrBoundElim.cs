// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Program
{
    private static int returnCode = 100;

    [Fact]
    public static int TestEntryPoint()
    {
        RunTestThrows(Tests.MulOutsideRange);
        RunTestThrows(Tests.MulOverflow);
        RunTestThrows(Tests.LshOutsideRange);

        RunTestNoThrow(Tests.MulInsideRange);
        RunTestNoThrow(Tests.LshInsideRange);

        return returnCode;
    }

    private static void RunTestThrows(Action action)
    {
        try
        {
            action();
            Console.WriteLine("failed " + action.Method.Name);
            returnCode--;
        }
        catch (Exception)
        {

        }
    }

    private static void RunTestNoThrow(Action action)
    {
        try
        {
            action();
        }
        catch (Exception)
        {
            Console.WriteLine("failed " + action.Method.Name);
            returnCode--;
        }
    }
}

public static class Tests
{
    private static byte[] smallArr => new byte[10];

    // RangeCheck analysis should eliminate the bounds check on
    // smallArr.
    public static void MulInsideRange()
    {
        for (int i = 0; i < 3; i++)
        {
            smallArr[i*3] = 17;
        }
    }

    // RangeCheck analysis should keep the bounds check on
    // smallArr.
    public static void MulOutsideRange()
    {
        for (int i = 0; i < 3; i++)
        {
            smallArr[i*5] = 17;
        }
    }

    private static byte[] bigArr => new byte[268435460];

    // RangeCheck analysis should detect that the multiplcation
    // overflows and keep all range checks for bigArr. bigArr
    // size, and the bounds on the loop were carefully chosen to
    // potentially spoof the RangeCheck analysis to eliminate a bound
    // check IF overflow detection on GT_MUL for RangeCheck is implemented
    // incorrectly.
    public static void MulOverflow()
    {
        for (int i = 0; i < 39768215; i++)
        {
            bigArr[i*402653184] = 17;
        }
    }

    // RangeCheck analysis should eliminate the bounds check on
    // smallArr.
    public static void LshInsideRange()
    {
        for (int i = 0; i < 3; i++)
        {
            smallArr[i<<1] = 17;
        }
    }

    // RangeCheck analysis should keep the bounds check on
    // smallArr.
    public static void LshOutsideRange()
    {
        for (int i = 0; i < 3; i++)
        {
            smallArr[i<<3] = 17;
        }
    }

}
