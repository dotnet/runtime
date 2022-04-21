// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;
namespace Test_25param2a_cs
{
internal struct VT
{
    public int m;
}
public class CL
{
    public int n;
    public CL(int a)
    {
        n = a;
    }
}
public class test
{
    private static int f1(short a1, ushort a2, int a3, uint a4, long a5,
            ulong a6, byte a7, sbyte a8, Decimal a9, int[] a10,
            VT a11, CL a12, int a13, int a14, int a15,
            int a16, int a17, int a18, int a19, int a20,
            int a21, int a22, int a23, int a24, int a25)
    {
        int sum = (int)(a1 + a2 + a3 + a4 + (int)a5 + (int)a6 + a7 + a8 + (int)a9 + a10[0]
            + a11.m + a12.n + a13 + a14 + a15 + a16 + a17 + a18 + a19
            + a20 + a21 + a22 + a23 + a24 + a25);
        Console.WriteLine("The sum is {0}", sum);
        return sum;
    }

    private static int f(short a1, ushort a2, int a3, uint a4, long a5,
            ulong a6, byte a7, sbyte a8, Decimal a9, int[] a10,
            VT a11, CL a12, int a13, int a14, int a15,
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
        Console.WriteLine(a10[0]);
        Console.WriteLine(a11.m);
        Console.WriteLine(a12.n);
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
        Console.WriteLine("Testing method of 25 parameters, mixed data type");
        VT vt = new VT();
        vt.m = 11;
        CL cl = new CL(12);
        int sum = f(1, 2, 3, 4, 5, 6, 7, 8, 9, new int[1] { 10 }, vt, cl, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25);
        if (sum == 325) return 100;
        else return 1;
    }
}

}
