// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;
namespace Test_25param1a_cs
{
public class test
{
    private static int f1(int a1, int a2, int a3, int a4, int a5,
            int a6, int a7, int a8, int a9, int a10,
            int a11, int a12, int a13, int a14, int a15,
            int a16, int a17, int a18, int a19, int a20,
            int a21, int a22, int a23, int a24, int a25)
    {
        int sum = a1 + a2 + a3 + a4 + a5 + a6 + a7 + a8 + a9 + a10
            + a11 + a12 + a13 + a14 + a15 + a16 + a17 + a18 + a19
            + a20 + a21 + a22 + a23 + a24 + a25;
        Console.WriteLine("The sum is {0}", sum);
        return sum;
    }

    private static int f(int a1, int a2, int a3, int a4, int a5,
            int a6, int a7, int a8, int a9, int a10,
            int a11, int a12, int a13, int a14, int a15,
            int a16, int a17, int a18, int a19, int a20,
            int a21, int a22, int a23, int a24, int a25)
    {
        Console.WriteLine(a1);
        Console.WriteLine(a2);
        Console.WriteLine(a3);
        Console.WriteLine(a4);
        Console.WriteLine(a5);
        Console.WriteLine(a6);
        Console.WriteLine(a7);
        Console.WriteLine(a8);
        Console.WriteLine(a9);
        Console.WriteLine(a10);
        Console.WriteLine(a11);
        Console.WriteLine(a12);
        Console.WriteLine(a13);
        Console.WriteLine(a14);
        Console.WriteLine(a15);
        Console.WriteLine(a16);
        Console.WriteLine(a17);
        Console.WriteLine(a18);
        Console.WriteLine(a19);
        Console.WriteLine(a20);
        Console.WriteLine(a21);
        Console.WriteLine(a22);
        Console.WriteLine(a23);
        Console.WriteLine(a24);
        Console.WriteLine(a25);
        int sum = f1(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15,
                a16, a17, a18, a19, a20, a21, a22, a23, a24, a25);
        return sum;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        Console.WriteLine("Testing method of 25 parameters, all of int data type");
        int sum = f(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25);
        if (sum == 325)
            return 100;
        else
            return 1;
    }
}

}
