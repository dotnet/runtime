// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

/*
    csc /o+ InlineRecursion.cs

    Expected:

        Caught DivideByZeroException: System.DivideByZeroException: Attempted to divide by zero.
           at MainApp.Foo()
           at MainApp.Main()
        Passed!

    Any other outcome is a bug.        
*/

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class MainApp
{

    static int one = 1;
    static int zero = 0;
    static int result;

    internal static void Foo()
    {
        result = one / zero;
        Foo();
    }

    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            try
            {
                Foo();
                Console.WriteLine("Return from Foo without any exception.");
                Console.WriteLine("Failed.");
                return 101;
            }
            catch (DivideByZeroException ex)
            {
                Console.WriteLine("Caught DivideByZeroException: " + ex.ToString());
                Console.WriteLine("Passed!");
                return 100;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Caught this unpected exception: " + ex.ToString());
            Console.WriteLine("Failed.");
            return 101;
        }

    }

}


