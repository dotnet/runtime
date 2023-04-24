// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace Test_FloatOvfToInt2
{
public class FloatOvfToInt
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool BreakUpFlow() => false;
    
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

    public static int TestValuesFloatLongVN()
    {
        float bigf = 100000000000000000000000000000.0f;
        
        if (BreakUpFlow())
            return 1000;
        
        if (FloatToLong(bigf) != FloatToLongInline(bigf)) return 401;
        if (FloatToUlong(bigf) != FloatToUlongInline(bigf)) return 402;
        if (FloatToLong(-bigf) != FloatToLongInline(-bigf)) return 403;
        if (FloatToUlong(-bigf) != FloatToUlongInline(-bigf)) return 404;

        bigf = 987654321001234567899876543210.0f;
        
        if (BreakUpFlow())
            return 1000;
        
        if (FloatToLong(bigf) != FloatToLongInline(bigf)) return 401;
        if (FloatToUlong(bigf) != FloatToUlongInline(bigf)) return 402;
        if (FloatToLong(-bigf) != FloatToLongInline(-bigf)) return 403;
        if (FloatToUlong(-bigf) != FloatToUlongInline(-bigf)) return 404;

        bigf = 254783961024896571038054632179.0f;
        
        if (BreakUpFlow())
            return 1000;
        
        if (FloatToLong(bigf) != FloatToLongInline(bigf)) return 401;
        if (FloatToUlong(bigf) != FloatToUlongInline(bigf)) return 402;
        if (FloatToLong(-bigf) != FloatToLongInline(-bigf)) return 403;
        if (FloatToUlong(-bigf) != FloatToUlongInline(-bigf)) return 404;

        return 100;
    }

    public static int TestValuesFloatLongImport()
    {
        float bigf = 100000000000000000000000000000.0f;
        if (FloatToLong(bigf) != FloatToLongInline(100000000000000000000000000000.0f)) return 501;
        if (FloatToUlong(bigf) != FloatToUlongInline(100000000000000000000000000000.0f)) return 502;
        if (FloatToLong(-bigf) != FloatToLongInline(-100000000000000000000000000000.0f)) return 503;
        if (FloatToUlong(-bigf) != FloatToUlongInline(-100000000000000000000000000000.0f)) return 504;

        bigf = 987654321001234567899876543210.0f;
        if (FloatToLong(bigf) != FloatToLongInline(987654321001234567899876543210.0f)) return 501;
        if (FloatToUlong(bigf) != FloatToUlongInline(987654321001234567899876543210.0f)) return 502;
        if (FloatToLong(-bigf) != FloatToLongInline(-987654321001234567899876543210.0f)) return 503;
        if (FloatToUlong(-bigf) != FloatToUlongInline(-987654321001234567899876543210.0f)) return 504;

        bigf = 254783961024896571038054632179.0f;
        if (FloatToLong(bigf) != FloatToLongInline(254783961024896571038054632179.0f)) return 501;
        if (FloatToUlong(bigf) != FloatToUlongInline(254783961024896571038054632179.0f)) return 502;
        if (FloatToLong(-bigf) != FloatToLongInline(-254783961024896571038054632179.0f)) return 503;
        if (FloatToUlong(-bigf) != FloatToUlongInline(-254783961024896571038054632179.0f)) return 504;

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

    public static int TestValuesFloatIntVN()
    {
        float bigf = 100000000000000000000000000000.0f;
        
        if (BreakUpFlow())
            return 1000;
        
        if (FloatToInt(bigf) != FloatToIntInline(bigf)) return 411;
        if (FloatToUint(bigf) != FloatToUintInline(bigf)) return 412;
        if (FloatToInt(-bigf) != FloatToIntInline(-bigf)) return 413;
        if (FloatToUint(-bigf) != FloatToUintInline(-bigf)) return 414;

        bigf = 987654321001234567899876543210.0f;
        
        if (BreakUpFlow())
            return 1000;
        
        if (FloatToInt(bigf) != FloatToIntInline(bigf)) return 411;
        if (FloatToUint(bigf) != FloatToUintInline(bigf)) return 412;
        if (FloatToInt(-bigf) != FloatToIntInline(-bigf)) return 413;
        if (FloatToUint(-bigf) != FloatToUintInline(-bigf)) return 414;

        bigf = 254783961024896571038054632179.0f;
        
        if (BreakUpFlow())
            return 1000;
        
        if (FloatToInt(bigf) != FloatToIntInline(bigf)) return 411;
        if (FloatToUint(bigf) != FloatToUintInline(bigf)) return 412;
        if (FloatToInt(-bigf) != FloatToIntInline(-bigf)) return 413;
        if (FloatToUint(-bigf) != FloatToUintInline(-bigf)) return 414;

        return 100;
    }

    public static int TestValuesFloatIntImport()
    {
        float bigf = 100000000000000000000000000000.0f;
        if (FloatToInt(bigf) != FloatToIntInline(100000000000000000000000000000.0f)) return 511;
        if (FloatToUint(bigf) != FloatToUintInline(100000000000000000000000000000.0f)) return 512;
        if (FloatToInt(-bigf) != FloatToIntInline(-100000000000000000000000000000.0f)) return 513;
        if (FloatToUint(-bigf) != FloatToUintInline(-100000000000000000000000000000.0f)) return 514;

        bigf = 987654321001234567899876543210.0f;
        if (FloatToInt(bigf) != FloatToIntInline(987654321001234567899876543210.0f)) return 511;
        if (FloatToUint(bigf) != FloatToUintInline(987654321001234567899876543210.0f)) return 512;
        if (FloatToInt(-bigf) != FloatToIntInline(-987654321001234567899876543210.0f)) return 513;
        if (FloatToUint(-bigf) != FloatToUintInline(-987654321001234567899876543210.0f)) return 514;

        bigf = 254783961024896571038054632179.0f;
        if (FloatToInt(bigf) != FloatToIntInline(254783961024896571038054632179.0f)) return 511;
        if (FloatToUint(bigf) != FloatToUintInline(254783961024896571038054632179.0f)) return 512;
        if (FloatToInt(-bigf) != FloatToIntInline(-254783961024896571038054632179.0f)) return 513;
        if (FloatToUint(-bigf) != FloatToUintInline(-254783961024896571038054632179.0f)) return 514;

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

    public static int TestValuesFloatShortVN()
    {
        float bigf = 100000000000000000000000000000.0f;

        if (BreakUpFlow())
            return 1000;
        
        if (FloatToShort(bigf) != FloatToShortInline(bigf)) return 421;
        if (FloatToUshort(bigf) != FloatToUshortInline(bigf)) return 422;
        if (FloatToShort(-bigf) != FloatToShortInline(-bigf)) return 423;
        if (FloatToUshort(-bigf) != FloatToUshortInline(-bigf)) return 424;

        bigf = 987654321001234567899876543210.0f;
        
        if (BreakUpFlow())
            return 1000;
        
        if (FloatToShort(bigf) != FloatToShortInline(bigf)) return 421;
        if (FloatToUshort(bigf) != FloatToUshortInline(bigf)) return 422;
        if (FloatToShort(-bigf) != FloatToShortInline(-bigf)) return 423;
        if (FloatToUshort(-bigf) != FloatToUshortInline(-bigf)) return 424;

        bigf = 254783961024896571038054632179.0f;
        
        if (BreakUpFlow())
            return 1000;
        
        if (FloatToShort(bigf) != FloatToShortInline(bigf)) return 421;
        if (FloatToUshort(bigf) != FloatToUshortInline(bigf)) return 422;
        if (FloatToShort(-bigf) != FloatToShortInline(-bigf)) return 423;
        if (FloatToUshort(-bigf) != FloatToUshortInline(-bigf)) return 424;

        return 100;
    }

    public static int TestValuesFloatShortImport()
    {
        float bigf = 100000000000000000000000000000.0f;
        if (FloatToShort(bigf) != FloatToShortInline(100000000000000000000000000000.0f)) return 521;
        if (FloatToUshort(bigf) != FloatToUshortInline(100000000000000000000000000000.0f)) return 522;
        if (FloatToShort(-bigf) != FloatToShortInline(-100000000000000000000000000000.0f)) return 523;
        if (FloatToUshort(-bigf) != FloatToUshortInline(-100000000000000000000000000000.0f)) return 524;

        bigf = 987654321001234567899876543210.0f;
        if (FloatToShort(bigf) != FloatToShortInline(987654321001234567899876543210.0f)) return 521;
        if (FloatToUshort(bigf) != FloatToUshortInline(987654321001234567899876543210.0f)) return 522;
        if (FloatToShort(-bigf) != FloatToShortInline(-987654321001234567899876543210.0f)) return 523;
        if (FloatToUshort(-bigf) != FloatToUshortInline(-987654321001234567899876543210.0f)) return 524;

        bigf = 254783961024896571038054632179.0f;
        if (FloatToShort(bigf) != FloatToShortInline(254783961024896571038054632179.0f)) return 521;
        if (FloatToUshort(bigf) != FloatToUshortInline(254783961024896571038054632179.0f)) return 522;
        if (FloatToShort(-bigf) != FloatToShortInline(-254783961024896571038054632179.0f)) return 523;
        if (FloatToUshort(-bigf) != FloatToUshortInline(-254783961024896571038054632179.0f)) return 524;

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

    public static int TestValuesFloatByteVN()
    {
        float bigf = 100000000000000000000000000000.0f;
        
        if (BreakUpFlow())
            return 1000;
        
        if (FloatToSbyte(bigf) != FloatToSbyteInline(bigf)) return 441;
        if (FloatToByte(bigf) != FloatToByteInline(bigf)) return 442;
        if (FloatToSbyte(-bigf) != FloatToSbyteInline(-bigf)) return 443;
        if (FloatToByte(-bigf) != FloatToByteInline(-bigf)) return 444;

        bigf = 987654321001234567899876543210.0f;
        
        if (BreakUpFlow())
            return 1000;
        
        if (FloatToSbyte(bigf) != FloatToSbyteInline(bigf)) return 441;
        if (FloatToByte(bigf) != FloatToByteInline(bigf)) return 442;
        if (FloatToSbyte(-bigf) != FloatToSbyteInline(-bigf)) return 443;
        if (FloatToByte(-bigf) != FloatToByteInline(-bigf)) return 444;

        bigf = 254783961024896571038054632179.0f;
        
        if (BreakUpFlow())
            return 1000;
        
        if (FloatToSbyte(bigf) != FloatToSbyteInline(bigf)) return 441;
        if (FloatToByte(bigf) != FloatToByteInline(bigf)) return 442;
        if (FloatToSbyte(-bigf) != FloatToSbyteInline(-bigf)) return 443;
        if (FloatToByte(-bigf) != FloatToByteInline(-bigf)) return 444;

        return 100;
    }

    public static int TestValuesFloatByteImport()
    {
        float bigf = 100000000000000000000000000000.0f;
        if (FloatToSbyte(bigf) != FloatToSbyteInline(100000000000000000000000000000.0f)) return 541;
        if (FloatToByte(bigf) != FloatToByteInline(100000000000000000000000000000.0f)) return 542;
        if (FloatToSbyte(-bigf) != FloatToSbyteInline(-100000000000000000000000000000.0f)) return 543;
        if (FloatToByte(-bigf) != FloatToByteInline(-100000000000000000000000000000.0f)) return 544;

        bigf = 987654321001234567899876543210.0f;
        if (FloatToSbyte(bigf) != FloatToSbyteInline(987654321001234567899876543210.0f)) return 541;
        if (FloatToByte(bigf) != FloatToByteInline(987654321001234567899876543210.0f)) return 542;
        if (FloatToSbyte(-bigf) != FloatToSbyteInline(-987654321001234567899876543210.0f)) return 543;
        if (FloatToByte(-bigf) != FloatToByteInline(-987654321001234567899876543210.0f)) return 544;

        bigf = 254783961024896571038054632179.0f;
        if (FloatToSbyte(bigf) != FloatToSbyteInline(254783961024896571038054632179.0f)) return 541;
        if (FloatToByte(bigf) != FloatToByteInline(254783961024896571038054632179.0f)) return 542;
        if (FloatToSbyte(-bigf) != FloatToSbyteInline(-254783961024896571038054632179.0f)) return 543;
        if (FloatToByte(-bigf) != FloatToByteInline(-254783961024896571038054632179.0f)) return 544;

        return 100;
    }

    public static int TestValuesDoubleLong()
    {
        double bigd = 100000000000000000000000000000.0;
        
        if (BreakUpFlow())
            return 1000;
        
        if (DoubleToLong(bigd) != DoubleToLongInline(bigd)) return 201;
        if (DoubleToUlong(bigd) != DoubleToUlongInline(bigd)) return 202;
        if (DoubleToLong(-bigd) != DoubleToLongInline(-bigd)) return 203;
        if (DoubleToUlong(-bigd) != DoubleToUlongInline(-bigd)) return 204;

        bigd = 987654321001234567899876543210.0;
        
        if (BreakUpFlow())
            return 1000;
        
        if (DoubleToLong(bigd) != DoubleToLongInline(bigd)) return 201;
        if (DoubleToUlong(bigd) != DoubleToUlongInline(bigd)) return 202;
        if (DoubleToLong(-bigd) != DoubleToLongInline(-bigd)) return 203;
        if (DoubleToUlong(-bigd) != DoubleToUlongInline(-bigd)) return 204;

        bigd = 254783961024896571038054632179.0;
        
        if (BreakUpFlow())
            return 1000;
        
        if (DoubleToLong(bigd) != DoubleToLongInline(bigd)) return 201;
        if (DoubleToUlong(bigd) != DoubleToUlongInline(bigd)) return 202;
        if (DoubleToLong(-bigd) != DoubleToLongInline(-bigd)) return 203;
        if (DoubleToUlong(-bigd) != DoubleToUlongInline(-bigd)) return 204;

        return 100;
    }

    public static int TestValuesDoubleLongVN()
    {
        double bigd = 100000000000000000000000000000.0;
        if (DoubleToLong(bigd) != DoubleToLongInline(bigd)) return 301;
        if (DoubleToUlong(bigd) != DoubleToUlongInline(bigd)) return 302;
        if (DoubleToLong(-bigd) != DoubleToLongInline(-bigd)) return 303;
        if (DoubleToUlong(-bigd) != DoubleToUlongInline(-bigd)) return 304;

        bigd = 987654321001234567899876543210.0;
        if (DoubleToLong(bigd) != DoubleToLongInline(bigd)) return 301;
        if (DoubleToUlong(bigd) != DoubleToUlongInline(bigd)) return 302;
        if (DoubleToLong(-bigd) != DoubleToLongInline(-bigd)) return 303;
        if (DoubleToUlong(-bigd) != DoubleToUlongInline(-bigd)) return 304;

        bigd = 254783961024896571038054632179.0;
        if (DoubleToLong(bigd) != DoubleToLongInline(bigd)) return 301;
        if (DoubleToUlong(bigd) != DoubleToUlongInline(bigd)) return 302;
        if (DoubleToLong(-bigd) != DoubleToLongInline(-bigd)) return 303;
        if (DoubleToUlong(-bigd) != DoubleToUlongInline(-bigd)) return 304;

        return 100;
    }

    public static int TestValuesDoubleLongImport()
    {
        double bigd = 100000000000000000000000000000.0;
        if (DoubleToLong(bigd) != DoubleToLongInline(100000000000000000000000000000.0)) return 601;
        if (DoubleToUlong(bigd) != DoubleToUlongInline(100000000000000000000000000000.0)) return 602;
        if (DoubleToLong(-bigd) != DoubleToLongInline(-100000000000000000000000000000.0)) return 603;
        if (DoubleToUlong(-bigd) != DoubleToUlongInline(-100000000000000000000000000000.0)) return 604;

        bigd = 987654321001234567899876543210.0;
        if (DoubleToLong(bigd) != DoubleToLongInline(987654321001234567899876543210.0)) return 601;
        if (DoubleToUlong(bigd) != DoubleToUlongInline(987654321001234567899876543210.0)) return 602;
        if (DoubleToLong(-bigd) != DoubleToLongInline(-987654321001234567899876543210.0)) return 603;
        if (DoubleToUlong(-bigd) != DoubleToUlongInline(-987654321001234567899876543210.0)) return 604;

        bigd = 254783961024896571038054632179.0;
        if (DoubleToLong(bigd) != DoubleToLongInline(254783961024896571038054632179.0)) return 601;
        if (DoubleToUlong(bigd) != DoubleToUlongInline(254783961024896571038054632179.0)) return 602;
        if (DoubleToLong(-bigd) != DoubleToLongInline(-254783961024896571038054632179.0)) return 603;
        if (DoubleToUlong(-bigd) != DoubleToUlongInline(-254783961024896571038054632179.0)) return 604;

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

    public static int TestValuesDoubleIntVN()
    {
        double bigd = 100000000000000000000000000000.0;
        
        if (BreakUpFlow())
            return 1000;
        
        if (DoubleToInt(bigd) != DoubleToIntInline(bigd)) return 311;
        if (DoubleToUint(bigd) != DoubleToUintInline(bigd)) return 312;
        if (DoubleToInt(-bigd) != DoubleToIntInline(-bigd)) return 313;
        if (DoubleToUint(-bigd) != DoubleToUintInline(-bigd)) return 314;

        bigd = 987654321001234567899876543210.0;
        
        if (BreakUpFlow())
            return 1000;
        
        if (DoubleToInt(bigd) != DoubleToIntInline(bigd)) return 311;
        if (DoubleToUint(bigd) != DoubleToUintInline(bigd)) return 312;
        if (DoubleToInt(-bigd) != DoubleToIntInline(-bigd)) return 313;
        if (DoubleToUint(-bigd) != DoubleToUintInline(-bigd)) return 314;

        bigd = 254783961024896571038054632179.0;
        
        if (BreakUpFlow())
            return 1000;
        
        if (DoubleToInt(bigd) != DoubleToIntInline(bigd)) return 311;
        if (DoubleToUint(bigd) != DoubleToUintInline(bigd)) return 312;
        if (DoubleToInt(-bigd) != DoubleToIntInline(-bigd)) return 313;
        if (DoubleToUint(-bigd) != DoubleToUintInline(-bigd)) return 314;

        return 100;
    }

    public static int TestValuesDoubleIntImport()
    {
        double bigd = 100000000000000000000000000000.0;
        if (DoubleToInt(bigd) != DoubleToIntInline(100000000000000000000000000000.0)) return 611;
        if (DoubleToUint(bigd) != DoubleToUintInline(100000000000000000000000000000.0)) return 612;
        if (DoubleToInt(-bigd) != DoubleToIntInline(-100000000000000000000000000000.0)) return 613;
        if (DoubleToUint(-bigd) != DoubleToUintInline(-100000000000000000000000000000.0)) return 614;

        bigd = 987654321001234567899876543210.0;
        if (DoubleToInt(bigd) != DoubleToIntInline(987654321001234567899876543210.0)) return 611;
        if (DoubleToUint(bigd) != DoubleToUintInline(987654321001234567899876543210.0)) return 612;
        if (DoubleToInt(-bigd) != DoubleToIntInline(-987654321001234567899876543210.0)) return 613;
        if (DoubleToUint(-bigd) != DoubleToUintInline(-987654321001234567899876543210.0)) return 614;

        bigd = 254783961024896571038054632179.0;
        if (DoubleToInt(bigd) != DoubleToIntInline(254783961024896571038054632179.0)) return 611;
        if (DoubleToUint(bigd) != DoubleToUintInline(254783961024896571038054632179.0)) return 612;
        if (DoubleToInt(-bigd) != DoubleToIntInline(-254783961024896571038054632179.0)) return 613;
        if (DoubleToUint(-bigd) != DoubleToUintInline(-254783961024896571038054632179.0)) return 614;

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

    public static int TestValuesDoubleShortVN()
    {
        double bigd = 100000000000000000000000000000.0;
        
        if (BreakUpFlow())
            return 1000;
        
        if (DoubleToShort(bigd) != DoubleToShortInline(bigd)) return 321;
        if (DoubleToUshort(bigd) != DoubleToUshortInline(bigd)) return 322;
        if (DoubleToShort(-bigd) != DoubleToShortInline(-bigd)) return 323;
        if (DoubleToUshort(-bigd) != DoubleToUshortInline(-bigd)) return 324;

        bigd = 987654321001234567899876543210.0;
        
        if (BreakUpFlow())
            return 1000;
        
        if (DoubleToShort(bigd) != DoubleToShortInline(bigd)) return 321;
        if (DoubleToUshort(bigd) != DoubleToUshortInline(bigd)) return 322;
        if (DoubleToShort(-bigd) != DoubleToShortInline(-bigd)) return 323;
        if (DoubleToUshort(-bigd) != DoubleToUshortInline(-bigd)) return 324;

        bigd = 254783961024896571038054632179.0;
        
        if (BreakUpFlow())
            return 1000;
        
        if (DoubleToShort(bigd) != DoubleToShortInline(bigd)) return 321;
        if (DoubleToUshort(bigd) != DoubleToUshortInline(bigd)) return 322;
        if (DoubleToShort(-bigd) != DoubleToShortInline(-bigd)) return 323;
        if (DoubleToUshort(-bigd) != DoubleToUshortInline(-bigd)) return 324;

        return 100;
    }

    public static int TestValuesDoubleShortImport()
    {
        double bigd = 100000000000000000000000000000.0;
        if (DoubleToShort(bigd) != DoubleToShortInline(100000000000000000000000000000.0)) return 621;
        if (DoubleToUshort(bigd) != DoubleToUshortInline(100000000000000000000000000000.0)) return 622;
        if (DoubleToShort(-bigd) != DoubleToShortInline(-100000000000000000000000000000.0)) return 623;
        if (DoubleToUshort(-bigd) != DoubleToUshortInline(-bigd)) return 624;

        bigd = 987654321001234567899876543210.0;
        if (DoubleToShort(bigd) != DoubleToShortInline(987654321001234567899876543210.0)) return 621;
        if (DoubleToUshort(bigd) != DoubleToUshortInline(987654321001234567899876543210.0)) return 622;
        if (DoubleToShort(-bigd) != DoubleToShortInline(-987654321001234567899876543210.0)) return 623;
        if (DoubleToUshort(-bigd) != DoubleToUshortInline(-987654321001234567899876543210.0)) return 624;

        bigd = 254783961024896571038054632179.0;
        if (DoubleToShort(bigd) != DoubleToShortInline(254783961024896571038054632179.0)) return 621;
        if (DoubleToUshort(bigd) != DoubleToUshortInline(254783961024896571038054632179.0)) return 622;
        if (DoubleToShort(-bigd) != DoubleToShortInline(-254783961024896571038054632179.0)) return 623;
        if (DoubleToUshort(-bigd) != DoubleToUshortInline(-254783961024896571038054632179.0)) return 624;

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

    public static int TestValuesDoubleByteVN()
    {
        double bigd = 100000000000000000000000000000.0;
        
        if (BreakUpFlow())
            return 1000;
        
        if (DoubleToSbyte(bigd) != DoubleToSbyteInline(bigd)) return 341;
        if (DoubleToByte(bigd) != DoubleToByteInline(bigd)) return 342;
        if (DoubleToSbyte(-bigd) != DoubleToSbyteInline(-bigd)) return 343;
        if (DoubleToByte(-bigd) != DoubleToByteInline(-bigd)) return 344;

        bigd = 987654321001234567899876543210.0;
        
        if (BreakUpFlow())
            return 1000;
        
        if (DoubleToSbyte(bigd) != DoubleToSbyteInline(bigd)) return 341;
        if (DoubleToByte(bigd) != DoubleToByteInline(bigd)) return 342;
        if (DoubleToSbyte(-bigd) != DoubleToSbyteInline(-bigd)) return 343;
        if (DoubleToByte(-bigd) != DoubleToByteInline(-bigd)) return 344;

        bigd = 254783961024896571038054632179.0;
        
        if (BreakUpFlow())
            return 1000;
        
        if (DoubleToSbyte(bigd) != DoubleToSbyteInline(bigd)) return 341;
        if (DoubleToByte(bigd) != DoubleToByteInline(bigd)) return 342;
        if (DoubleToSbyte(-bigd) != DoubleToSbyteInline(-bigd)) return 343;
        if (DoubleToByte(-bigd) != DoubleToByteInline(-bigd)) return 344;

        return 100;
    }

    public static int TestValuesDoubleByteImport()
    {
        double bigd = 100000000000000000000000000000.0;
        if (DoubleToSbyte(bigd) != DoubleToSbyteInline(100000000000000000000000000000.0)) return 641;
        if (DoubleToByte(bigd) != DoubleToByteInline(100000000000000000000000000000.0)) return 642;
        if (DoubleToSbyte(-bigd) != DoubleToSbyteInline(-100000000000000000000000000000.0)) return 643;
        if (DoubleToByte(-bigd) != DoubleToByteInline(-100000000000000000000000000000.0)) return 644;

        bigd = 987654321001234567899876543210.0;
        if (DoubleToSbyte(bigd) != DoubleToSbyteInline(987654321001234567899876543210.0)) return 641;
        if (DoubleToByte(bigd) != DoubleToByteInline(987654321001234567899876543210.0)) return 642;
        if (DoubleToSbyte(-bigd) != DoubleToSbyteInline(-987654321001234567899876543210.0)) return 643;
        if (DoubleToByte(-bigd) != DoubleToByteInline(-987654321001234567899876543210.0)) return 644;

        bigd = 254783961024896571038054632179.0;
        if (DoubleToSbyte(bigd) != DoubleToSbyteInline(987654321001234567899876543210.0)) return 641;
        if (DoubleToByte(bigd) != DoubleToByteInline(987654321001234567899876543210.0)) return 642;
        if (DoubleToSbyte(-bigd) != DoubleToSbyteInline(-987654321001234567899876543210.0)) return 643;
        if (DoubleToByte(-bigd) != DoubleToByteInline(-987654321001234567899876543210.0)) return 644;

        return 100;
    }

    public static int TestValues()
    {
        int res = TestValuesFloatLong(); if (res != 100) return res;
        res = TestValuesFloatLongVN(); if (res != 100) return res;
        res = TestValuesFloatLongImport(); if (res != 100) return res;
        res = TestValuesFloatInt(); if (res != 100) return res;
        res = TestValuesFloatIntVN(); if (res != 100) return res;
        res = TestValuesFloatIntImport(); if (res != 100) return res;
        res = TestValuesFloatShort(); if (res != 100) return res;
        res = TestValuesFloatShortImport(); if (res != 100) return res;
        res = TestValuesFloatShortVN(); if (res != 100) return res;
        res = TestValuesFloatByte(); if (res != 100) return res;
        res = TestValuesFloatByteImport(); if (res != 100) return res;
        res = TestValuesFloatByteVN(); if (res != 100) return res;

        res = TestValuesDoubleLong(); if (res != 100) return res;
        res = TestValuesDoubleLongVN(); if (res != 100) return res;
        res = TestValuesDoubleLongImport(); if (res != 100) return res;
        res = TestValuesDoubleInt(); if (res != 100) return res;
        res = TestValuesDoubleIntVN(); if (res != 100) return res;
        res = TestValuesDoubleIntImport(); if (res != 100) return res;
        res = TestValuesDoubleShort(); if (res != 100) return res;
        res = TestValuesDoubleShortVN(); if (res != 100) return res;
        res = TestValuesDoubleShortImport(); if (res != 100) return res;
        res = TestValuesDoubleByte(); if (res != 100) return res;
        res = TestValuesDoubleByteVN(); if (res != 100) return res;
        res = TestValuesDoubleByteImport(); if (res != 100) return res;

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
