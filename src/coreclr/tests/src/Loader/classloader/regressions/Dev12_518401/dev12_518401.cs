// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/* Regression Test for Dev12 bug #518401: Bug in accessing x64 bit Multidimensional Arrays in x64
 * 
 * Change description: Store the callee saved registers more often in hand generated assembly helper.
*/
using System;

#pragma warning disable 169
struct MyStruct
{
    byte a, b, c;
}
#pragma warning restore 169

class My
{
    static void foo<T>(T[,] s)
    {
        s[0, 1] = s[1, 0];
    }

    static int Main()
    {
        try
        {
            Object o1 = new Object();
            Object o2 = new Object();
            Object o3 = new Object();
            Object o4 = new Object();

            foo(new MyStruct[2, 2]); //corrupts registry

            o1.ToString();
            o2.ToString();
            o3.ToString();
            o4.ToString();
        }
        catch (Exception e)
        {
            Console.WriteLine("Unexpected exception: " + e);
            return 102;
        }

        Console.WriteLine("Pass");
        return 100;
    }
}
