// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// long chain of methods

using System;
using Xunit;
namespace Test_25param3a_cs
{
public class test
{
    private static int f1(int a1, int a2, int a3, int a4, int a5,
            int a6, int a7, int a8, int a9, int a10,
            int a11, int a12, int a13, int a14, int a15,
            int a16, int a17, int a18, int a19, int a20,
            int a21, int a22, int a23, int a24, int a25)
    {
        Console.WriteLine(a1);
        int sum = f2(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15,
                a16, a17, a18, a19, a20, a21, a22, a23, a24, a25);
        return sum;
    }

    private static int f2(int a1, int a2, int a3, int a4, int a5,
            int a6, int a7, int a8, int a9, int a10,
            int a11, int a12, int a13, int a14, int a15,
            int a16, int a17, int a18, int a19, int a20,
            int a21, int a22, int a23, int a24, int a25)
    {
        Console.WriteLine(a2);
        int sum = f3(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15,
                a16, a17, a18, a19, a20, a21, a22, a23, a24, a25);
        return sum;
    }

    private static int f3(int a1, int a2, int a3, int a4, int a5,
            int a6, int a7, int a8, int a9, int a10,
            int a11, int a12, int a13, int a14, int a15,
            int a16, int a17, int a18, int a19, int a20,
            int a21, int a22, int a23, int a24, int a25)
    {
        Console.WriteLine(a3);
        int sum = f4(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15,
                a16, a17, a18, a19, a20, a21, a22, a23, a24, a25);
        return sum;
    }

    private static int f4(int a1, int a2, int a3, int a4, int a5,
            int a6, int a7, int a8, int a9, int a10,
            int a11, int a12, int a13, int a14, int a15,
            int a16, int a17, int a18, int a19, int a20,
            int a21, int a22, int a23, int a24, int a25)
    {
        Console.WriteLine(a4);
        int sum = f5(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15,
                a16, a17, a18, a19, a20, a21, a22, a23, a24, a25);
        return sum;
    }

    private static int f5(int a1, int a2, int a3, int a4, int a5,
            int a6, int a7, int a8, int a9, int a10,
            int a11, int a12, int a13, int a14, int a15,
            int a16, int a17, int a18, int a19, int a20,
            int a21, int a22, int a23, int a24, int a25)
    {
        Console.WriteLine(a5);
        int sum = f6(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15,
                a16, a17, a18, a19, a20, a21, a22, a23, a24, a25);
        return sum;
    }

    private static int f6(int a1, int a2, int a3, int a4, int a5,
            int a6, int a7, int a8, int a9, int a10,
            int a11, int a12, int a13, int a14, int a15,
            int a16, int a17, int a18, int a19, int a20,
            int a21, int a22, int a23, int a24, int a25)
    {
        Console.WriteLine(a6);
        int sum = f7(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15,
                a16, a17, a18, a19, a20, a21, a22, a23, a24, a25);
        return sum;
    }

    private static int f7(int a1, int a2, int a3, int a4, int a5,
            int a6, int a7, int a8, int a9, int a10,
            int a11, int a12, int a13, int a14, int a15,
            int a16, int a17, int a18, int a19, int a20,
            int a21, int a22, int a23, int a24, int a25)
    {
        Console.WriteLine(a7);
        int sum = f8(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15,
                a16, a17, a18, a19, a20, a21, a22, a23, a24, a25);
        return sum;
    }

    private static int f8(int a1, int a2, int a3, int a4, int a5,
            int a6, int a7, int a8, int a9, int a10,
            int a11, int a12, int a13, int a14, int a15,
            int a16, int a17, int a18, int a19, int a20,
            int a21, int a22, int a23, int a24, int a25)
    {
        Console.WriteLine(a8);
        int sum = f9(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15,
                a16, a17, a18, a19, a20, a21, a22, a23, a24, a25);
        return sum;
    }

    private static int f9(int a1, int a2, int a3, int a4, int a5,
            int a6, int a7, int a8, int a9, int a10,
            int a11, int a12, int a13, int a14, int a15,
            int a16, int a17, int a18, int a19, int a20,
            int a21, int a22, int a23, int a24, int a25)
    {
        Console.WriteLine(a9);
        int sum = f10(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15,
                a16, a17, a18, a19, a20, a21, a22, a23, a24, a25);
        return sum;
    }

    private static int f10(int a1, int a2, int a3, int a4, int a5,
            int a6, int a7, int a8, int a9, int a10,
            int a11, int a12, int a13, int a14, int a15,
            int a16, int a17, int a18, int a19, int a20,
            int a21, int a22, int a23, int a24, int a25)
    {
        Console.WriteLine(a10);
        int sum = f11(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15,
                a16, a17, a18, a19, a20, a21, a22, a23, a24, a25);
        return sum;
    }

    private static int f11(int a1, int a2, int a3, int a4, int a5,
            int a6, int a7, int a8, int a9, int a10,
            int a11, int a12, int a13, int a14, int a15,
            int a16, int a17, int a18, int a19, int a20,
            int a21, int a22, int a23, int a24, int a25)
    {
        Console.WriteLine(a11);
        int sum = f12(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15,
                a16, a17, a18, a19, a20, a21, a22, a23, a24, a25);
        return sum;
    }

    private static int f12(int a1, int a2, int a3, int a4, int a5,
            int a6, int a7, int a8, int a9, int a10,
            int a11, int a12, int a13, int a14, int a15,
            int a16, int a17, int a18, int a19, int a20,
            int a21, int a22, int a23, int a24, int a25)
    {
        Console.WriteLine(a12);
        int sum = f13(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15,
                a16, a17, a18, a19, a20, a21, a22, a23, a24, a25);
        return sum;
    }

    private static int f13(int a1, int a2, int a3, int a4, int a5,
            int a6, int a7, int a8, int a9, int a10,
            int a11, int a12, int a13, int a14, int a15,
            int a16, int a17, int a18, int a19, int a20,
            int a21, int a22, int a23, int a24, int a25)
    {
        Console.WriteLine(a13);
        int sum = f14(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15,
                a16, a17, a18, a19, a20, a21, a22, a23, a24, a25);
        return sum;
    }

    private static int f14(int a1, int a2, int a3, int a4, int a5,
            int a6, int a7, int a8, int a9, int a10,
            int a11, int a12, int a13, int a14, int a15,
            int a16, int a17, int a18, int a19, int a20,
            int a21, int a22, int a23, int a24, int a25)
    {
        Console.WriteLine(a14);
        int sum = f15(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15,
                a16, a17, a18, a19, a20, a21, a22, a23, a24, a25);
        return sum;
    }

    private static int f15(int a1, int a2, int a3, int a4, int a5,
            int a6, int a7, int a8, int a9, int a10,
            int a11, int a12, int a13, int a14, int a15,
            int a16, int a17, int a18, int a19, int a20,
            int a21, int a22, int a23, int a24, int a25)
    {
        Console.WriteLine(a15);
        int sum = f16(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15,
                a16, a17, a18, a19, a20, a21, a22, a23, a24, a25);
        return sum;
    }

    private static int f16(int a1, int a2, int a3, int a4, int a5,
            int a6, int a7, int a8, int a9, int a10,
            int a11, int a12, int a13, int a14, int a15,
            int a16, int a17, int a18, int a19, int a20,
            int a21, int a22, int a23, int a24, int a25)
    {
        Console.WriteLine(a16);
        int sum = f17(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15,
                a16, a17, a18, a19, a20, a21, a22, a23, a24, a25);
        return sum;
    }

    private static int f17(int a1, int a2, int a3, int a4, int a5,
            int a6, int a7, int a8, int a9, int a10,
            int a11, int a12, int a13, int a14, int a15,
            int a16, int a17, int a18, int a19, int a20,
            int a21, int a22, int a23, int a24, int a25)
    {
        Console.WriteLine(a17);
        int sum = f18(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15,
                a16, a17, a18, a19, a20, a21, a22, a23, a24, a25);
        return sum;
    }

    private static int f18(int a1, int a2, int a3, int a4, int a5,
            int a6, int a7, int a8, int a9, int a10,
            int a11, int a12, int a13, int a14, int a15,
            int a16, int a17, int a18, int a19, int a20,
            int a21, int a22, int a23, int a24, int a25)
    {
        Console.WriteLine(a18);
        int sum = f19(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15,
                a16, a17, a18, a19, a20, a21, a22, a23, a24, a25);
        return sum;
    }

    private static int f19(int a1, int a2, int a3, int a4, int a5,
            int a6, int a7, int a8, int a9, int a10,
            int a11, int a12, int a13, int a14, int a15,
            int a16, int a17, int a18, int a19, int a20,
            int a21, int a22, int a23, int a24, int a25)
    {
        Console.WriteLine(a19);
        int sum = f20(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15,
                a16, a17, a18, a19, a20, a21, a22, a23, a24, a25);
        return sum;
    }

    private static int f20(int a1, int a2, int a3, int a4, int a5,
            int a6, int a7, int a8, int a9, int a10,
            int a11, int a12, int a13, int a14, int a15,
            int a16, int a17, int a18, int a19, int a20,
            int a21, int a22, int a23, int a24, int a25)
    {
        Console.WriteLine(a20);
        int sum = f21(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15,
                a16, a17, a18, a19, a20, a21, a22, a23, a24, a25);
        return sum;
    }

    private static int f21(int a1, int a2, int a3, int a4, int a5,
            int a6, int a7, int a8, int a9, int a10,
            int a11, int a12, int a13, int a14, int a15,
            int a16, int a17, int a18, int a19, int a20,
            int a21, int a22, int a23, int a24, int a25)
    {
        Console.WriteLine(a21);
        int sum = f22(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15,
                a16, a17, a18, a19, a20, a21, a22, a23, a24, a25);
        return sum;
    }

    private static int f22(int a1, int a2, int a3, int a4, int a5,
            int a6, int a7, int a8, int a9, int a10,
            int a11, int a12, int a13, int a14, int a15,
            int a16, int a17, int a18, int a19, int a20,
            int a21, int a22, int a23, int a24, int a25)
    {
        Console.WriteLine(a22);
        int sum = f23(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15,
                a16, a17, a18, a19, a20, a21, a22, a23, a24, a25);
        return sum;
    }

    private static int f23(int a1, int a2, int a3, int a4, int a5,
            int a6, int a7, int a8, int a9, int a10,
            int a11, int a12, int a13, int a14, int a15,
            int a16, int a17, int a18, int a19, int a20,
            int a21, int a22, int a23, int a24, int a25)
    {
        Console.WriteLine(a23);
        int sum = f24(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15,
                a16, a17, a18, a19, a20, a21, a22, a23, a24, a25);
        return sum;
    }

    private static int f24(int a1, int a2, int a3, int a4, int a5,
            int a6, int a7, int a8, int a9, int a10,
            int a11, int a12, int a13, int a14, int a15,
            int a16, int a17, int a18, int a19, int a20,
            int a21, int a22, int a23, int a24, int a25)
    {
        Console.WriteLine(a24);
        int sum = f25(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15,
                a16, a17, a18, a19, a20, a21, a22, a23, a24, a25);
        return sum;
    }

    private static int f25(int a1, int a2, int a3, int a4, int a5,
            int a6, int a7, int a8, int a9, int a10,
            int a11, int a12, int a13, int a14, int a15,
            int a16, int a17, int a18, int a19, int a20,
            int a21, int a22, int a23, int a24, int a25)
    {
        Console.WriteLine(a25);
        int sum = a1 + a2 + a3 + a4 + a5 + a6 + a7 + a8 + a9 + a10
            + a11 + a12 + a13 + a14 + a15 + a16 + a17 + a18 + a19
            + a20 + a21 + a22 + a23 + a24 + a25;
        return sum;
    }


    private static int f(int a1, int a2, int a3, int a4, int a5,
            int a6, int a7, int a8, int a9, int a10,
            int a11, int a12, int a13, int a14, int a15,
            int a16, int a17, int a18, int a19, int a20,
            int a21, int a22, int a23, int a24, int a25)
    {
        int sum = f1(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15,
                a16, a17, a18, a19, a20, a21, a22, a23, a24, a25);
        return sum;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        Console.WriteLine("Testing method of 25 parameters, all of int data type, long chain of method calls");
        int sum = f(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25);
        Console.WriteLine("The sum is {0}", sum);
        if (sum == 325)
            return 100;
        else
            return 1;
    }
}

}
