// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;

class Program
{
    static bool s_caughtException;

    static uint s_uint_value = int.MaxValue + 1U;
    static int  s_int_result = 0;

    static int  s_int_value =  -1;
    static uint s_uint_result = 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static int CastToIntChecked(uint value)
    {
	// checked cast of uint to int
        return checked((int)value);
    }

    // Testing a checked cast of uint to int -- the inlining case
    //
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Test1()
    {
        int result = CastToIntChecked(s_uint_value);
	s_int_result = result;
	Console.WriteLine("Result is " + result);
    }

    // Testing a checked cast of uint to int -- the non-inlining case
    //
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Test2()
    {
        uint copy = 0;
        try
        {
            s_caughtException = false;

            copy = s_uint_value;

            int result = checked((int)copy);
            s_int_result = result;
            Console.WriteLine("Result is " + result);
        }
        catch (System.OverflowException ex)
        {
            s_caughtException = true;
            Console.WriteLine("CORRECT: " + ex);
            copy = 0;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static uint CastToUIntChecked(int value)
    {
	// checked cast of int to uint
        return checked((uint)value);
    }

    // Testing a checked cast of int to uint -- the inlining case
    //
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Test3()
    {
        uint result = CastToUIntChecked(s_int_value);
	s_uint_result = result;
	Console.WriteLine("Result is " + result);
    }

    // Testing a checked cast of int to uint -- the non-inlining case
    //
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Test4()
    {
        uint copy = 0;
        try
        {
            s_caughtException = false;

            copy = s_uint_value;

            int result = checked((int)copy);
            s_int_result = result;
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

        try
        {
            Test3();
        }
        catch (System.OverflowException ex)
        {
            s_caughtException = true;
            Console.WriteLine("CORRECT: " + ex);
        }

        if (s_caughtException == false)
        {
            Console.WriteLine("FAILED - Test3");
            failed = true;
        }

        Test4();
        if (s_caughtException == false)
        {
            Console.WriteLine("FAILED - Test4");
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
