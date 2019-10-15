// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

/// <summary>
/// Regression test case for Dev10 851479 bug: Stackoverflow in .NET when using self referencing generics along with type constraints to another type parameter.
/// </summary>
class Program
{
    static Int32 Main()
    {
        Program p = new Program();

        if (p.Run())
        {
            Console.WriteLine("PASS");
            return 100;
        }
        else
        {
            Console.WriteLine("FAIL");
            return -1;
        }
    }

    public Boolean Run()
    {
        try
        {
            var B = new B();
            System.Console.WriteLine(B);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Got unexpected error: " + ex);
            return false;
        }

        return true;
    }
}

class A<T, U>
    where T : U
    where U : A<T, U> { }

class B : A<B, B>
{
}
