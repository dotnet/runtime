// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class CMyException : System.Exception
{
}

public class CTest
{
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    private static void UseShort(short x)
    {
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    private static void UseByte(byte x)
    {
    }


    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    private static unsafe void CheckDoubleAlignment(double* p)
    {
        if (((int)p % sizeof(double)) != 0)
            throw new CMyException();
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    private static unsafe void TestLocals1(short a, double b, byte c, double d)
    {
        short i16;
        double d1;
        byte i8;
        double d2;

        i16 = a;
        i8 = c;
        d1 = b;
        d2 = d;

        CheckDoubleAlignment(&d1);
        CheckDoubleAlignment(&d2);

        i16 += (short)d1;
        i8 += (byte)d2;

        UseShort(i16);
        UseByte(i8);
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    private static unsafe void TestLocals2(short a, double b, byte c, double d)
    {
        char c1;
        double d1;
        CMyException e1 = new CMyException();
        byte b1;
        short s1;
        double d2;
        int[] a1 = new int[1];
        int i1;
        int i2;
        int i3;
        int i4;
        int i5;
        double d3;
        byte b3;
        byte b5;
        double d5;
        sbyte b4 = 5;
        double d6;
        int i6;

        c1 = (char)a;
        b1 = b3 = c;
        d1 = b;
        d2 = d;
        d3 = d1 + d2;
        d5 = d1 * 3;
        i1 = a;
        i2 = c;
        i3 = a + c;
        i4 = a - c;
        i5 = i1--;
        i6 = i3 * i4;
        s1 = (short)(a + 5);
        b4 += (sbyte)c;

        byte b2 = (byte)-b1;
        double d4 = d3 / 2;
        b5 = (byte)b;
        d6 = b1++;

        CheckDoubleAlignment(&d1);
        CheckDoubleAlignment(&d2);
        CheckDoubleAlignment(&d3);
        CheckDoubleAlignment(&d4);
        CheckDoubleAlignment(&d5);
        CheckDoubleAlignment(&d6);

        b3 -= (byte)(d5 * b4 - b5);
        s1 += (short)(d1 + d6 - (i1 + i2 + i3 + i4 + i5 + i6));
        b1 += (byte)(b3 + d2 - (i1 * 3 + i2 - i3 - i4 * i5 - (i6 >> 2)));

        UseShort(s1);
        UseByte(b1);
    }

    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            TestLocals1(1, 2, 3, 4);
            TestLocals2(1, 2, 3, 4);
        }
        catch (CMyException)
        {
            Console.WriteLine("FAILED");
            return 101;
        }
        Console.WriteLine("PASSED");
        return 100;
    }
}
