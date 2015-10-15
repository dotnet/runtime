// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// compile with: csc /o+ <filename.cs>

using System;
using System.Runtime.CompilerServices;


public class Bug426480
{
    public static int s_i = 0;
    public static bool first_time_through = true;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static void foo()
    {
        int a = s_i;

        if (first_time_through)
        {
            first_time_through = false;
            s_i = 5 / s_i;
        }

        s_i += a;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static void bar()
    {
        foo();
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int Main(String[] args)
    {
        try
        {
            // saves the caller context
            foo();
        }
        catch (DivideByZeroException)
        {
            // eat the expected exception
        }

        try
        {
            //uses stale context in epilogue checking
            bar();
        }
        catch (Exception e)
        {
            Console.WriteLine("Unexpected Exception!");
            Console.WriteLine(e);
            Console.WriteLine();
            Console.WriteLine("Test Failed");
            return 1;
        }

        Console.WriteLine("Test Passed");
        return 100;
    }
}
