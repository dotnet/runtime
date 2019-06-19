// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;

class Program
{
    static bool s_caughtException;
    static uint s_value = int.MaxValue + 1U;
    static int  s_result = 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static int CastToIntChecked(uint value)
    {
        return checked((int)value);
    }

    // Testing a checked cast to Uint -- the inlining case
    //
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Test1()
    {
        int result = CastToIntChecked(s_value);
	s_result = result;
	Console.WriteLine("Result is " + result);
    }

    // Testing a checked cast to Uint -- the non-inlining case
    //
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Test2()
    {
        uint copy = 0;
        try
        {
            s_caughtException = false;

            copy = s_value;

            int result = checked((int)copy);
            s_result = result;
            Console.WriteLine("Result is " + result);
        }
        catch (System.OverflowException ex)
        {
            s_caughtException = true;
            Console.WriteLine("CORRECT: " + ex);
            copy = 0;
        }
    }

    static int Main()
    {
        bool failed = false;

        try
        {
            Test1();
        }
        catch (System.OverflowException ex)
        {
            s_caughtException = true;
            Console.WriteLine("CORRECT: " + ex);
        }

        if (s_caughtException == false)
        {
            Console.WriteLine("FAILED - Test1");
            failed = true;
        }

        Test2();
        if (s_caughtException == false)
        {
            Console.WriteLine("FAILED - Test2");
            failed = true;
        }

        if (failed)
        {
            return 101;
        }
        else
        {
            return 100;
        }
    }
}
