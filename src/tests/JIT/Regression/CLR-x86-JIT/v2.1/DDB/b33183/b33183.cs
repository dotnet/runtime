// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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

class MainApp
{

    static int one = 1;
    static int zero = 0;
    static int result;

    public static void Foo()
    {
        result = one / zero;
        Foo();
    }

    public static int Main()
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


