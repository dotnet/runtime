// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace Test_FloatInfinitiesToInt
{
public class FloatOvfToInt
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long FloatToLong(float f)
    {
        return (long)f;
    }
    public static long FloatToLongInline(float f)
    {
        return (long)f;
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ulong FloatToUlong(float f)
    {
        return (ulong)f;
    }
    public static ulong FloatToUlongInline(float f)
    {
        return (ulong)f;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int FloatToInt(float f)
    {
        return (int)f;
    }
    public static int FloatToIntInline(float f)
    {
        return (int)f;
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static uint FloatToUint(float f)
    {
        return (uint)f;
    }
    public static uint FloatToUintInline(float f)
    {
        return (uint)f;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static short FloatToShort(float f)
    {
        return (short)f;
    }
    public static short FloatToShortInline(float f)
    {
        return (short)f;
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ushort FloatToUshort(float f)
    {
        return (ushort)f;
    }
    public static ushort FloatToUshortInline(float f)
    {
        return (ushort)f;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static sbyte FloatToSbyte(float f)
    {
        return (sbyte)f;
    }
    public static sbyte FloatToSbyteInline(float f)
    {
        return (sbyte)f;
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static byte FloatToByte(float f)
    {
        return (byte)f;
    }
    public static byte FloatToByteInline(float f)
    {
        return (byte)f;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long DoubleToLong(double d)
    {
        return (long)d;
    }
    public static long DoubleToLongInline(double d)
    {
        return (long)d;
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ulong DoubleToUlong(double d)
    {
        return (ulong)d;
    }
    public static ulong DoubleToUlongInline(double d)
    {
        return (ulong)d;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int DoubleToInt(double d)
    {
        return (int)d;
    }
    public static int DoubleToIntInline(double d)
    {
        return (int)d;
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static uint DoubleToUint(double d)
    {
        return (uint)d;
    }
    public static uint DoubleToUintInline(double d)
    {
        return (uint)d;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static short DoubleToShort(double d)
    {
        return (short)d;
    }
    public static short DoubleToShortInline(double d)
    {
        return (short)d;
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ushort DoubleToUshort(double d)
    {
        return (ushort)d;
    }
    public static ushort DoubleToUshortInline(double d)
    {
        return (ushort)d;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static sbyte DoubleToSbyte(double d)
    {
        return (sbyte)d;
    }
    public static sbyte DoubleToSbyteInline(double d)
    {
        return (sbyte)d;
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static byte DoubleToByte(double d)
    {
        return (byte)d;
    }
    public static byte DoubleToByteInline(double d)
    {
        return (byte)d;
    }

    public static void PrintValues()
    {
        float inff = 1.0f / 0.0f;
        Console.WriteLine("InfF to long = 0x{0}", FloatToLong(inff).ToString("x"));
        Console.WriteLine("InfF to ulong = 0x{0}", FloatToUlong(inff).ToString("x"));
        Console.WriteLine("-InfF to long = 0x{0}", FloatToLong(-inff).ToString("x"));
        Console.WriteLine("-InfF to ulong = 0x{0}", FloatToUlong(-inff).ToString("x"));
        Console.WriteLine("");
        Console.WriteLine("InfF to int = 0x{0}", FloatToInt(inff).ToString("x"));
        Console.WriteLine("InfF to uint = 0x{0}", FloatToUint(inff).ToString("x"));
        Console.WriteLine("-InfF to int = 0x{0}", FloatToInt(-inff).ToString("x"));
        Console.WriteLine("-InfF to uint = 0x{0}", FloatToUint(-inff).ToString("x"));
        Console.WriteLine("");
        Console.WriteLine("InfF to short = 0x{0}", FloatToShort(inff).ToString("x"));
        Console.WriteLine("InfF to ushort = 0x{0}", FloatToUshort(inff).ToString("x"));
        Console.WriteLine("-InfF to short = 0x{0}", FloatToShort(-inff).ToString("x"));
        Console.WriteLine("-InfF to ushort = 0x{0}", FloatToUshort(-inff).ToString("x"));
        Console.WriteLine("");
        Console.WriteLine("InfF to sbyte = 0x{0}", FloatToSbyte(inff).ToString("x"));
        Console.WriteLine("InfF to byte = 0x{0}", FloatToByte(inff).ToString("x"));
        Console.WriteLine("-InfF to sbyte = 0x{0}", FloatToSbyte(-inff).ToString("x"));
        Console.WriteLine("-InfF to byte = 0x{0}", FloatToByte(-inff).ToString("x"));
        Console.WriteLine("");

        double infd = 1.0 / 0.0;
        Console.WriteLine("InfD to long = 0x{0}", DoubleToLong(infd).ToString("x"));
        Console.WriteLine("InfD to ulong = 0x{0}", DoubleToUlong(infd).ToString("x"));
        Console.WriteLine("-InfD to long = 0x{0}", DoubleToLong(-infd).ToString("x"));
        Console.WriteLine("-InfD to ulong = 0x{0}", DoubleToUlong(-infd).ToString("x"));
        Console.WriteLine("");
        Console.WriteLine("InfD to int = 0x{0}", DoubleToInt(infd).ToString("x"));
        Console.WriteLine("InfD to uint = 0x{0}", DoubleToUint(infd).ToString("x"));
        Console.WriteLine("-InfD to int = 0x{0}", DoubleToInt(-infd).ToString("x"));
        Console.WriteLine("-InfD to uint = 0x{0}", DoubleToUint(-infd).ToString("x"));
        Console.WriteLine("");
        Console.WriteLine("InfD to short = 0x{0}", DoubleToShort(infd).ToString("x"));
        Console.WriteLine("InfD to ushort = 0x{0}", DoubleToUshort(infd).ToString("x"));
        Console.WriteLine("-InfD to short = 0x{0}", DoubleToShort(-infd).ToString("x"));
        Console.WriteLine("-InfD to ushort = 0x{0}", DoubleToUshort(-infd).ToString("x"));
        Console.WriteLine("");
        Console.WriteLine("InfD to sbyte = 0x{0}", DoubleToSbyte(infd).ToString("x"));
        Console.WriteLine("InfD to byte = 0x{0}", DoubleToByte(infd).ToString("x"));
        Console.WriteLine("-InfD to sbyte = 0x{0}", DoubleToSbyte(-infd).ToString("x"));
        Console.WriteLine("-InfD to byte = 0x{0}", DoubleToByte(-infd).ToString("x"));
        Console.WriteLine("");
    }

    public static int TestValuesFloatLong()
    {
        float inff = 1.0f / 0.0f;
        if (FloatToLong(inff) != FloatToLongInline(inff)) return 101;
        if (FloatToUlong(inff) != FloatToUlongInline(inff)) return 102;
        if (FloatToLong(-inff) != FloatToLongInline(-inff)) return 103;
        if (FloatToUlong(-inff) != FloatToUlongInline(-inff)) return 104;
        return 100;
    }

    public static int TestValuesFloatInt()
    {
        float inff = 1.0f / 0.0f;
        if (FloatToInt(inff) != FloatToIntInline(inff)) return 111;
        if (FloatToUint(inff) != FloatToUintInline(inff)) return 112;
        if (FloatToInt(-inff) != FloatToIntInline(-inff)) return 113;
        if (FloatToUint(-inff) != FloatToUintInline(-inff)) return 114;
        return 100;
    }

    public static int TestValuesFloatShort()
    {
        float inff = 1.0f / 0.0f;
        if (FloatToShort(inff) != FloatToShortInline(inff)) return 121;
        if (FloatToUshort(inff) != FloatToUshortInline(inff)) return 122;
        if (FloatToShort(-inff) != FloatToShortInline(-inff)) return 123;
        if (FloatToUshort(-inff) != FloatToUshortInline(-inff)) return 124;
        return 100;
    }

    public static int TestValuesFloatByte()
    {
        float inff = 1.0f / 0.0f;
        if (FloatToSbyte(inff) != FloatToSbyteInline(inff)) return 141;
        if (FloatToByte(inff) != FloatToByteInline(inff)) return 142;
        if (FloatToSbyte(-inff) != FloatToSbyteInline(-inff)) return 143;
        if (FloatToByte(-inff) != FloatToByteInline(-inff)) return 144;
        return 100;
    }

    public static int TestValuesDoubleLong()
    {
        double infd = 1.0 / 0.0;
        if (DoubleToLong(infd) != DoubleToLongInline(infd)) return 201;
        if (DoubleToUlong(infd) != DoubleToUlongInline(infd)) return 202;
        if (DoubleToLong(-infd) != DoubleToLongInline(-infd)) return 203;
        if (DoubleToUlong(-infd) != DoubleToUlongInline(-infd)) return 204;
        return 100;
    }

    public static int TestValuesDoubleInt()
    {
        double infd = 1.0 / 0.0;
        if (DoubleToInt(infd) != DoubleToIntInline(infd)) return 211;
        if (DoubleToUint(infd) != DoubleToUintInline(infd)) return 212;
        if (DoubleToInt(-infd) != DoubleToIntInline(-infd)) return 213;
        if (DoubleToUint(-infd) != DoubleToUintInline(-infd)) return 214;
        return 100;
    }

    public static int TestValuesDoubleShort()
    {
        double infd = 1.0 / 0.0;
        if (DoubleToShort(infd) != DoubleToShortInline(infd)) return 221;
        if (DoubleToUshort(infd) != DoubleToUshortInline(infd)) return 222;
        if (DoubleToShort(-infd) != DoubleToShortInline(-infd)) return 223;
        if (DoubleToUshort(-infd) != DoubleToUshortInline(-infd)) return 224;
        return 100;
    }

    public static int TestValuesDoubleByte()
    {
        double infd = 1.0 / 0.0;
        if (DoubleToSbyte(infd) != DoubleToSbyteInline(infd)) return 241;
        if (DoubleToByte(infd) != DoubleToByteInline(infd)) return 242;
        if (DoubleToSbyte(-infd) != DoubleToSbyteInline(-infd)) return 243;
        if (DoubleToByte(-infd) != DoubleToByteInline(-infd)) return 244;
        return 100;
    }

    public static int TestValues()
    {
        int res = TestValuesFloatLong(); if (res != 100) return res;
        res = TestValuesFloatInt(); if (res != 100) return res;
        res = TestValuesFloatShort(); if (res != 100) return res;
        res = TestValuesFloatByte(); if (res != 100) return res;

        res = TestValuesDoubleLong(); if (res != 100) return res;
        res = TestValuesDoubleInt(); if (res != 100) return res;
        res = TestValuesDoubleShort(); if (res != 100) return res;
        res = TestValuesDoubleByte(); if (res != 100) return res;

        return res;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        int res = TestValues();
        Console.WriteLine("Test " + (res == 100 ? "passed" : "failed"));
        return res;
    }
}
}
