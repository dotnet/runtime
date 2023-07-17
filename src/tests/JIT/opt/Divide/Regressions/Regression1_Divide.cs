// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Program
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ushort GetUShortValue()
    {
        return 24648;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int GetByteMaxValue()
    {
        return byte.MaxValue;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int GetSByteMaxValue()
    {
        return sbyte.MaxValue;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static uint Test1(ushort vr6)
    {
        ushort vr3 = 1;
        ushort vr4 = (ushort)~vr3;
        return (byte)((byte)vr6 / (byte)((byte)vr4 | 1));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static uint Test2()
    {
        ushort vr3 = 1;
        ushort vr4 = (ushort)~vr3;
        ushort vr6 = 24648;
        return (byte)((byte)vr6 / (byte)((byte)vr4 | 1));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ushort Test3()
    {
        ushort vr3 = 1;
        ushort vr4 = (ushort)~vr3;
        return (byte)((byte)vr4 | 1);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static uint Test4()
    {
        ushort vr1 = 24648;
        return (byte)((byte)vr1 / GetByteMaxValue());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Test5()
    {
        ushort vr3 = 1;
        ushort vr4 = (ushort)~vr3;
        ushort vr6 = 24648;
        return (sbyte)((sbyte)vr6 / (sbyte)((sbyte)vr4 | 1));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static uint Test6()
    {
        return (byte)((byte)GetUShortValue() / GetByteMaxValue());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static uint Test7()
    {
        ushort vr1 = 24648;
        return (byte)((byte)vr1 / GetByteMaxValue());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static uint Test8()
    {
        ushort vr1 = GetUShortValue();
        return (byte)((byte)vr1 / GetByteMaxValue());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Test9()
    {
        ushort vr1 = 24648;
        return (sbyte)((sbyte)vr1 / GetSByteMaxValue());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Test10(ushort vr6)
    {
        ushort vr3 = 1;
        ushort vr4 = (ushort)~vr3;
        return (sbyte)((sbyte)vr6 / (sbyte)((sbyte)vr4 | 1));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ushort Test11(int v)
    {
        return (ushort)((ushort)1 / (ushort)v);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static short Test12(int v)
    {
        return (short)((short)1 / (short)v);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ushort Test13(int v)
    {
        return (ushort)((ushort)v / 2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ushort Test14(int v1, int v2)
    {
        return (ushort)((ushort)v1 / (ushort)v2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static short Test15()
    {
        short y = short.MinValue;
        return unchecked((short)(y / -1));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static short Test16(short y)
    {
        return unchecked((short)(y / -1));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static short Test17()
    {
        try
        {
            short y = short.MinValue;
            return checked((short)(y / -1));
        }
        catch (ArithmeticException)
        {
            return 456;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static short Test18(int v)
    {
        return (short)((short)v / 2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static short Test19(int x, int y)
    {
        return (short)((short)x / (short)y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static short Test20(short x, short y)
    {
        return (short)(x / y);
    }

    [Fact]
    public static int TestEntryPoint()
    {
        var result1 = Test1(24648);
        var result2 = Test2();
        var result3 = Test3();
        var result4 = Test4();
        var result5 = Test5();
        var result6 = Test6();
        var result7 = Test7();
        var result8 = Test8();
        var result9 = Test9();
        var result10 = Test10(24648);
        var result11 = Test11(0x10001);
        var result12 = Test12(0x10001);
        var result13 = Test13(0x10000);
        var result14 = Test14(1, 0x10001);
        var result15 = Test15();
        var result16 = Test16(short.MinValue);
        var result17 = Test17();
        var result18 = Test18(0x10000);
        var result19 = Test19(0x10000, 2);
        var result20 = Test20(0, 2);

        if (result1 != 0)
            return 0;

        if (result2 != 0)
            return 0;

        if (result3 != 255)
            return 0;

        if (result4 != 0)
            return 0;

        if (result5 != -72)
            return 0;

        if (result6 != 0)
            return 0;

        if (result7 != 0)
            return 0;

        if (result8 != 0)
            return 0;

        if (result9 != 0)
            return 0;

        if (result10 != -72)
            return 0;

        if (result11 != 1)
            return 0;

        if (result12 != 1)
            return 0;

        if (result13 != 0)
            return 0;

        if (result14 != 1)
            return 0;

        if (result15 != -32768)
            return 0;

        if (result16 != -32768)
            return 0;

        if (result17 != 456)
            return 0;

        if (result18 != 0)
            return 0;

        if (result19 != 0)
            return 0;

        if (result20 != 0)
            return 0;

        return 100;
    }
}
