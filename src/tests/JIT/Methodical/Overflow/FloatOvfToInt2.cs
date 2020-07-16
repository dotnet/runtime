// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

internal class FloatOvfToInt
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
        float bigf = 100000000000000000000000000000.0f;
        Console.WriteLine("F to long = 0x{0}", FloatToLong(bigf).ToString("x"));
        Console.WriteLine("F to ulong = 0x{0}", FloatToUlong(bigf).ToString("x"));
        Console.WriteLine("-F to long = 0x{0}", FloatToLong(-bigf).ToString("x"));
        Console.WriteLine("-F to ulong = 0x{0}", FloatToUlong(-bigf).ToString("x"));
        Console.WriteLine("");
        Console.WriteLine("F to int = 0x{0}", FloatToInt(bigf).ToString("x"));
        Console.WriteLine("F to uint = 0x{0}", FloatToUint(bigf).ToString("x"));
        Console.WriteLine("-F to int = 0x{0}", FloatToInt(-bigf).ToString("x"));
        Console.WriteLine("-F to uint = 0x{0}", FloatToUint(-bigf).ToString("x"));
        Console.WriteLine("");
        Console.WriteLine("F to short = 0x{0}", FloatToShort(bigf).ToString("x"));
        Console.WriteLine("F to ushort = 0x{0}", FloatToUshort(bigf).ToString("x"));
        Console.WriteLine("-F to short = 0x{0}", FloatToShort(-bigf).ToString("x"));
        Console.WriteLine("-F to ushort = 0x{0}", FloatToUshort(-bigf).ToString("x"));
        Console.WriteLine("");
        Console.WriteLine("F to sbyte = 0x{0}", FloatToSbyte(bigf).ToString("x"));
        Console.WriteLine("F to byte = 0x{0}", FloatToByte(bigf).ToString("x"));
        Console.WriteLine("-F to sbyte = 0x{0}", FloatToSbyte(-bigf).ToString("x"));
        Console.WriteLine("-F to byte = 0x{0}", FloatToByte(-bigf).ToString("x"));
        Console.WriteLine("");

        double bigd = 100000000000000000000000000000.0;
        Console.WriteLine("D to long = 0x{0}", DoubleToLong(bigd).ToString("x"));
        Console.WriteLine("D to ulong = 0x{0}", DoubleToUlong(bigd).ToString("x"));
        Console.WriteLine("-D to long = 0x{0}", DoubleToLong(-bigd).ToString("x"));
        Console.WriteLine("-D to ulong = 0x{0}", DoubleToUlong(-bigd).ToString("x"));
        Console.WriteLine("");
        Console.WriteLine("D to int = 0x{0}", DoubleToInt(bigd).ToString("x"));
        Console.WriteLine("D to uint = 0x{0}", DoubleToUint(bigd).ToString("x"));
        Console.WriteLine("-D to int = 0x{0}", DoubleToInt(-bigd).ToString("x"));
        Console.WriteLine("-D to uint = 0x{0}", DoubleToUint(-bigd).ToString("x"));
        Console.WriteLine("");
        Console.WriteLine("D to short = 0x{0}", DoubleToShort(bigd).ToString("x"));
        Console.WriteLine("D to ushort = 0x{0}", DoubleToUshort(bigd).ToString("x"));
        Console.WriteLine("-D to short = 0x{0}", DoubleToShort(-bigd).ToString("x"));
        Console.WriteLine("-D to ushort = 0x{0}", DoubleToUshort(-bigd).ToString("x"));
        Console.WriteLine("");
        Console.WriteLine("D to sbyte = 0x{0}", DoubleToSbyte(bigd).ToString("x"));
        Console.WriteLine("D to byte = 0x{0}", DoubleToByte(bigd).ToString("x"));
        Console.WriteLine("-D to sbyte = 0x{0}", DoubleToSbyte(-bigd).ToString("x"));
        Console.WriteLine("-D to byte = 0x{0}", DoubleToByte(-bigd).ToString("x"));
        Console.WriteLine("");
    }

    public static int TestValuesFloatLong()
    {
        float bigf = 100000000000000000000000000000.0f;
        if (FloatToLong(bigf) != FloatToLongInline(bigf)) return 101;
        if (FloatToUlong(bigf) != FloatToUlongInline(bigf)) return 102;
        if (FloatToLong(-bigf) != FloatToLongInline(-bigf)) return 103;
        if (FloatToUlong(-bigf) != FloatToUlongInline(-bigf)) return 104;

        bigf = 987654321001234567899876543210.0f;
        if (FloatToLong(bigf) != FloatToLongInline(bigf)) return 101;
        if (FloatToUlong(bigf) != FloatToUlongInline(bigf)) return 102;
        if (FloatToLong(-bigf) != FloatToLongInline(-bigf)) return 103;
        if (FloatToUlong(-bigf) != FloatToUlongInline(-bigf)) return 104;

        bigf = 254783961024896571038054632179.0f;
        if (FloatToLong(bigf) != FloatToLongInline(bigf)) return 101;
        if (FloatToUlong(bigf) != FloatToUlongInline(bigf)) return 102;
        if (FloatToLong(-bigf) != FloatToLongInline(-bigf)) return 103;
        if (FloatToUlong(-bigf) != FloatToUlongInline(-bigf)) return 104;

        return 100;
    }

    public static int TestValuesFloatInt()
    {
        float bigf = 100000000000000000000000000000.0f;
        if (FloatToInt(bigf) != FloatToIntInline(bigf)) return 111;
        if (FloatToUint(bigf) != FloatToUintInline(bigf)) return 112;
        if (FloatToInt(-bigf) != FloatToIntInline(-bigf)) return 113;
        if (FloatToUint(-bigf) != FloatToUintInline(-bigf)) return 114;

        bigf = 987654321001234567899876543210.0f;
        if (FloatToInt(bigf) != FloatToIntInline(bigf)) return 111;
        if (FloatToUint(bigf) != FloatToUintInline(bigf)) return 112;
        if (FloatToInt(-bigf) != FloatToIntInline(-bigf)) return 113;
        if (FloatToUint(-bigf) != FloatToUintInline(-bigf)) return 114;

        bigf = 254783961024896571038054632179.0f;
        if (FloatToInt(bigf) != FloatToIntInline(bigf)) return 111;
        if (FloatToUint(bigf) != FloatToUintInline(bigf)) return 112;
        if (FloatToInt(-bigf) != FloatToIntInline(-bigf)) return 113;
        if (FloatToUint(-bigf) != FloatToUintInline(-bigf)) return 114;

        return 100;
    }

    public static int TestValuesFloatShort()
    {
        float bigf = 100000000000000000000000000000.0f;
        if (FloatToShort(bigf) != FloatToShortInline(bigf)) return 121;
        if (FloatToUshort(bigf) != FloatToUshortInline(bigf)) return 122;
        if (FloatToShort(-bigf) != FloatToShortInline(-bigf)) return 123;
        if (FloatToUshort(-bigf) != FloatToUshortInline(-bigf)) return 124;

        bigf = 987654321001234567899876543210.0f;
        if (FloatToShort(bigf) != FloatToShortInline(bigf)) return 121;
        if (FloatToUshort(bigf) != FloatToUshortInline(bigf)) return 122;
        if (FloatToShort(-bigf) != FloatToShortInline(-bigf)) return 123;
        if (FloatToUshort(-bigf) != FloatToUshortInline(-bigf)) return 124;

        bigf = 254783961024896571038054632179.0f;
        if (FloatToShort(bigf) != FloatToShortInline(bigf)) return 121;
        if (FloatToUshort(bigf) != FloatToUshortInline(bigf)) return 122;
        if (FloatToShort(-bigf) != FloatToShortInline(-bigf)) return 123;
        if (FloatToUshort(-bigf) != FloatToUshortInline(-bigf)) return 124;

        return 100;
    }

    public static int TestValuesFloatByte()
    {
        float bigf = 100000000000000000000000000000.0f;
        if (FloatToSbyte(bigf) != FloatToSbyteInline(bigf)) return 141;
        if (FloatToByte(bigf) != FloatToByteInline(bigf)) return 142;
        if (FloatToSbyte(-bigf) != FloatToSbyteInline(-bigf)) return 143;
        if (FloatToByte(-bigf) != FloatToByteInline(-bigf)) return 144;

        bigf = 987654321001234567899876543210.0f;
        if (FloatToSbyte(bigf) != FloatToSbyteInline(bigf)) return 141;
        if (FloatToByte(bigf) != FloatToByteInline(bigf)) return 142;
        if (FloatToSbyte(-bigf) != FloatToSbyteInline(-bigf)) return 143;
        if (FloatToByte(-bigf) != FloatToByteInline(-bigf)) return 144;

        bigf = 254783961024896571038054632179.0f;
        if (FloatToSbyte(bigf) != FloatToSbyteInline(bigf)) return 141;
        if (FloatToByte(bigf) != FloatToByteInline(bigf)) return 142;
        if (FloatToSbyte(-bigf) != FloatToSbyteInline(-bigf)) return 143;
        if (FloatToByte(-bigf) != FloatToByteInline(-bigf)) return 144;

        return 100;
    }

    public static int TestValuesDoubleLong()
    {
        double bigd = 100000000000000000000000000000.0;
        if (DoubleToLong(bigd) != DoubleToLongInline(bigd)) return 201;
        if (DoubleToUlong(bigd) != DoubleToUlongInline(bigd)) return 202;
        if (DoubleToLong(-bigd) != DoubleToLongInline(-bigd)) return 203;
        if (DoubleToUlong(-bigd) != DoubleToUlongInline(-bigd)) return 204;

        bigd = 987654321001234567899876543210.0;
        if (DoubleToLong(bigd) != DoubleToLongInline(bigd)) return 201;
        if (DoubleToUlong(bigd) != DoubleToUlongInline(bigd)) return 202;
        if (DoubleToLong(-bigd) != DoubleToLongInline(-bigd)) return 203;
        if (DoubleToUlong(-bigd) != DoubleToUlongInline(-bigd)) return 204;

        bigd = 254783961024896571038054632179.0;
        if (DoubleToLong(bigd) != DoubleToLongInline(bigd)) return 201;
        if (DoubleToUlong(bigd) != DoubleToUlongInline(bigd)) return 202;
        if (DoubleToLong(-bigd) != DoubleToLongInline(-bigd)) return 203;
        if (DoubleToUlong(-bigd) != DoubleToUlongInline(-bigd)) return 204;

        return 100;
    }

    public static int TestValuesDoubleInt()
    {
        double bigd = 100000000000000000000000000000.0;
        if (DoubleToInt(bigd) != DoubleToIntInline(bigd)) return 211;
        if (DoubleToUint(bigd) != DoubleToUintInline(bigd)) return 212;
        if (DoubleToInt(-bigd) != DoubleToIntInline(-bigd)) return 213;
        if (DoubleToUint(-bigd) != DoubleToUintInline(-bigd)) return 214;

        bigd = 987654321001234567899876543210.0;
        if (DoubleToInt(bigd) != DoubleToIntInline(bigd)) return 211;
        if (DoubleToUint(bigd) != DoubleToUintInline(bigd)) return 212;
        if (DoubleToInt(-bigd) != DoubleToIntInline(-bigd)) return 213;
        if (DoubleToUint(-bigd) != DoubleToUintInline(-bigd)) return 214;

        bigd = 254783961024896571038054632179.0;
        if (DoubleToInt(bigd) != DoubleToIntInline(bigd)) return 211;
        if (DoubleToUint(bigd) != DoubleToUintInline(bigd)) return 212;
        if (DoubleToInt(-bigd) != DoubleToIntInline(-bigd)) return 213;
        if (DoubleToUint(-bigd) != DoubleToUintInline(-bigd)) return 214;

        return 100;
    }

    public static int TestValuesDoubleShort()
    {
        double bigd = 100000000000000000000000000000.0;
        if (DoubleToShort(bigd) != DoubleToShortInline(bigd)) return 221;
        if (DoubleToUshort(bigd) != DoubleToUshortInline(bigd)) return 222;
        if (DoubleToShort(-bigd) != DoubleToShortInline(-bigd)) return 223;
        if (DoubleToUshort(-bigd) != DoubleToUshortInline(-bigd)) return 224;

        bigd = 987654321001234567899876543210.0;
        if (DoubleToShort(bigd) != DoubleToShortInline(bigd)) return 221;
        if (DoubleToUshort(bigd) != DoubleToUshortInline(bigd)) return 222;
        if (DoubleToShort(-bigd) != DoubleToShortInline(-bigd)) return 223;
        if (DoubleToUshort(-bigd) != DoubleToUshortInline(-bigd)) return 224;

        bigd = 254783961024896571038054632179.0;
        if (DoubleToShort(bigd) != DoubleToShortInline(bigd)) return 221;
        if (DoubleToUshort(bigd) != DoubleToUshortInline(bigd)) return 222;
        if (DoubleToShort(-bigd) != DoubleToShortInline(-bigd)) return 223;
        if (DoubleToUshort(-bigd) != DoubleToUshortInline(-bigd)) return 224;

        return 100;
    }

    public static int TestValuesDoubleByte()
    {
        double bigd = 100000000000000000000000000000.0;
        if (DoubleToSbyte(bigd) != DoubleToSbyteInline(bigd)) return 241;
        if (DoubleToByte(bigd) != DoubleToByteInline(bigd)) return 242;
        if (DoubleToSbyte(-bigd) != DoubleToSbyteInline(-bigd)) return 243;
        if (DoubleToByte(-bigd) != DoubleToByteInline(-bigd)) return 244;

        bigd = 987654321001234567899876543210.0;
        if (DoubleToSbyte(bigd) != DoubleToSbyteInline(bigd)) return 241;
        if (DoubleToByte(bigd) != DoubleToByteInline(bigd)) return 242;
        if (DoubleToSbyte(-bigd) != DoubleToSbyteInline(-bigd)) return 243;
        if (DoubleToByte(-bigd) != DoubleToByteInline(-bigd)) return 244;

        bigd = 254783961024896571038054632179.0;
        if (DoubleToSbyte(bigd) != DoubleToSbyteInline(bigd)) return 241;
        if (DoubleToByte(bigd) != DoubleToByteInline(bigd)) return 242;
        if (DoubleToSbyte(-bigd) != DoubleToSbyteInline(-bigd)) return 243;
        if (DoubleToByte(-bigd) != DoubleToByteInline(-bigd)) return 244;

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

    public static void Usage()
    {
        Console.WriteLine("FloatOvfToInt [print|test]");
    }

    public static int Main(String[] args)
    {
        if (args.Length != 1)
        {
            int res = TestValues();
            Console.WriteLine("Test " + (res == 100 ? "passed" : "failed"));
            return res;
        }
        switch (args[0])
        {
            case "print":
                PrintValues();
                break;
            case "test":
                int res = TestValues();
                Console.WriteLine("Test " + (res == 100 ? "passed" : "failed"));
                return res;
            default:
                Usage();
                break;
        }
        return 0;
    }
}
