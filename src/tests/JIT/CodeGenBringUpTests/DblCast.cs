// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;

public class BringUpTest_DblCast
{
    const int Pass = 100;
    const int Fail = -1;

    public static int Main()
    {
        // Each of the below scenarios tests a given value in both the checked and unchecked contexts. If all scenarios pass
        // Validate<T>(...) returns 0. Otherwise it returns a positive number for each failed scenario.
        //
        // Each conversion group validates the following scenarios:
        //  * NaN, which should return 0 or overflow
        //  *
        //  * NegativeInfinity, which should return T.MinValue or overflow
        //  * PositiveInfinity, which should return T.MaxValue or overflow
        //  *
        //  * The nearest value to T.MinValue which does overflow, which should return T.MinValue
        //  * The nearest value to T.MaxValue which does overflow, which should return T.MaxValue
        //  *
        //  * The nearest value to T.MinValue which does not overflow, which should return T.MinValue
        //  * The nearest value to T.MaxValue which does not overflow, which should return T.MaxValue
        //  *
        //  * T.MinValue, which should return T.MinValue and not overflow
        //  * T.MaxValue, which should return T.MaxValue and not overflow
        //  * - Int64/UInt64 are a special case where this will overflow as T.MaxValue is not representable and rounds up to (T.MaxValue + 1)
        //  *
        //  * NegativePi, which should return -3 but which will overflow for unsigned values and should return 0 instead
        //  * PositivePi, which should return +3 and not overflow

        int numFailing = 0;

        // Double -> Int8

        numFailing += Validate<sbyte>(double.NaN, 0, expectsOverflow: true, Unchecked.DoubleToInt8, Checked.DoubleToInt8);

        numFailing += Validate<sbyte>(double.NegativeInfinity, sbyte.MinValue, expectsOverflow: true, Unchecked.DoubleToInt8, Checked.DoubleToInt8);
        numFailing += Validate<sbyte>(double.PositiveInfinity, sbyte.MaxValue, expectsOverflow: true, Unchecked.DoubleToInt8, Checked.DoubleToInt8);

        numFailing += Validate<sbyte>(-129.0, sbyte.MinValue, expectsOverflow: true, Unchecked.DoubleToInt8, Checked.DoubleToInt8);
        numFailing += Validate<sbyte>(+128.0, sbyte.MaxValue, expectsOverflow: true, Unchecked.DoubleToInt8, Checked.DoubleToInt8);

        numFailing += Validate<sbyte>(-128.99999999999997, sbyte.MinValue, expectsOverflow: false, Unchecked.DoubleToInt8, Checked.DoubleToInt8);
        numFailing += Validate<sbyte>(+127.99999999999999, sbyte.MaxValue, expectsOverflow: false, Unchecked.DoubleToInt8, Checked.DoubleToInt8);

        numFailing += Validate<sbyte>(-128.0, sbyte.MinValue, expectsOverflow: false, Unchecked.DoubleToInt8, Checked.DoubleToInt8);
        numFailing += Validate<sbyte>(+127.0, sbyte.MaxValue, expectsOverflow: false, Unchecked.DoubleToInt8, Checked.DoubleToInt8);

        numFailing += Validate<sbyte>(-Math.PI, -3, expectsOverflow: false, Unchecked.DoubleToInt8, Checked.DoubleToInt8);
        numFailing += Validate<sbyte>(+Math.PI, +3, expectsOverflow: false, Unchecked.DoubleToInt8, Checked.DoubleToInt8);

        // Double -> Int16

        numFailing += Validate<short>(double.NaN, 0, expectsOverflow: true, Unchecked.DoubleToInt16, Checked.DoubleToInt16);

        numFailing += Validate<short>(double.NegativeInfinity, short.MinValue, expectsOverflow: true, Unchecked.DoubleToInt16, Checked.DoubleToInt16);
        numFailing += Validate<short>(double.PositiveInfinity, short.MaxValue, expectsOverflow: true, Unchecked.DoubleToInt16, Checked.DoubleToInt16);

        numFailing += Validate<short>(-32769.0, short.MinValue, expectsOverflow: true, Unchecked.DoubleToInt16, Checked.DoubleToInt16);
        numFailing += Validate<short>(+32768.0, short.MaxValue, expectsOverflow: true, Unchecked.DoubleToInt16, Checked.DoubleToInt16);

        numFailing += Validate<short>(-32768.999999999990, short.MinValue, expectsOverflow: false, Unchecked.DoubleToInt16, Checked.DoubleToInt16);
        numFailing += Validate<short>(+32767.999999999996, short.MaxValue, expectsOverflow: false, Unchecked.DoubleToInt16, Checked.DoubleToInt16);

        numFailing += Validate<short>(-32768.0, short.MinValue, expectsOverflow: false, Unchecked.DoubleToInt16, Checked.DoubleToInt16);
        numFailing += Validate<short>(+32767.0, short.MaxValue, expectsOverflow: false, Unchecked.DoubleToInt16, Checked.DoubleToInt16);

        numFailing += Validate<short>(-Math.PI, -3, expectsOverflow: false, Unchecked.DoubleToInt16, Checked.DoubleToInt16);
        numFailing += Validate<short>(+Math.PI, +3, expectsOverflow: false, Unchecked.DoubleToInt16, Checked.DoubleToInt16);

        // Double -> Int32

        numFailing += Validate<int>(double.NaN, 0, expectsOverflow: true, Unchecked.DoubleToInt32, Checked.DoubleToInt32);

        numFailing += Validate<int>(double.NegativeInfinity, int.MinValue, expectsOverflow: true, Unchecked.DoubleToInt32, Checked.DoubleToInt32);
        numFailing += Validate<int>(double.PositiveInfinity, int.MaxValue, expectsOverflow: true, Unchecked.DoubleToInt32, Checked.DoubleToInt32);

        numFailing += Validate<int>(-2147483649.0, int.MinValue, expectsOverflow: true, Unchecked.DoubleToInt32, Checked.DoubleToInt32);
        numFailing += Validate<int>(+2147483648.0, int.MaxValue, expectsOverflow: true, Unchecked.DoubleToInt32, Checked.DoubleToInt32);

        numFailing += Validate<int>(-2147483648.9999995, int.MinValue, expectsOverflow: false, Unchecked.DoubleToInt32, Checked.DoubleToInt32);
        numFailing += Validate<int>(+2147483647.9999998, int.MaxValue, expectsOverflow: false, Unchecked.DoubleToInt32, Checked.DoubleToInt32);

        numFailing += Validate<int>(-2147483648.0, int.MinValue, expectsOverflow: false, Unchecked.DoubleToInt32, Checked.DoubleToInt32);
        numFailing += Validate<int>(+2147483647.0, int.MaxValue, expectsOverflow: false, Unchecked.DoubleToInt32, Checked.DoubleToInt32);

        numFailing += Validate<int>(-Math.PI, -3, expectsOverflow: false, Unchecked.DoubleToInt32, Checked.DoubleToInt32);
        numFailing += Validate<int>(+Math.PI, +3, expectsOverflow: false, Unchecked.DoubleToInt32, Checked.DoubleToInt32);

        // Double -> Int64

        numFailing += Validate<long>(double.NaN, 0, expectsOverflow: true, Unchecked.DoubleToInt64, Checked.DoubleToInt64);

        numFailing += Validate<long>(double.NegativeInfinity, long.MinValue, expectsOverflow: true, Unchecked.DoubleToInt64, Checked.DoubleToInt64);
        numFailing += Validate<long>(double.PositiveInfinity, long.MaxValue, expectsOverflow: true, Unchecked.DoubleToInt64, Checked.DoubleToInt64);

        numFailing += Validate<long>(-9223372036854777856.0, long.MinValue, expectsOverflow: true, Unchecked.DoubleToInt64, Checked.DoubleToInt64);
        numFailing += Validate<long>(+9223372036854775808.0, long.MaxValue, expectsOverflow: true, Unchecked.DoubleToInt64, Checked.DoubleToInt64);

        numFailing += Validate<long>(-9223372036854775808.0, long.MinValue, expectsOverflow: false, Unchecked.DoubleToInt64, Checked.DoubleToInt64);
        numFailing += Validate<long>(+9223372036854774784.0, 9223372036854774784, expectsOverflow: false, Unchecked.DoubleToInt64, Checked.DoubleToInt64);

        numFailing += Validate<long>(-9223372036854775808.0, long.MinValue, expectsOverflow: false, Unchecked.DoubleToInt64, Checked.DoubleToInt64);
        numFailing += Validate<long>(+9223372036854775807.0, long.MaxValue, expectsOverflow: true, Unchecked.DoubleToInt64, Checked.DoubleToInt64);

        numFailing += Validate<long>(-Math.PI, -3, expectsOverflow: false, Unchecked.DoubleToInt64, Checked.DoubleToInt64);
        numFailing += Validate<long>(+Math.PI, +3, expectsOverflow: false, Unchecked.DoubleToInt64, Checked.DoubleToInt64);

        // Double -> UInt8

        numFailing += Validate<byte>(double.NegativeInfinity, byte.MinValue, expectsOverflow: true, Unchecked.DoubleToUInt8, Checked.DoubleToUInt8);
        numFailing += Validate<byte>(double.NaN, 0, expectsOverflow: true, Unchecked.DoubleToUInt8, Checked.DoubleToUInt8);
        numFailing += Validate<byte>(double.PositiveInfinity, byte.MaxValue, expectsOverflow: true, Unchecked.DoubleToUInt8, Checked.DoubleToUInt8);

        numFailing += Validate<byte>(-1.000, byte.MinValue, expectsOverflow: true, Unchecked.DoubleToUInt8, Checked.DoubleToUInt8);
        numFailing += Validate<byte>(+256.0, byte.MaxValue, expectsOverflow: true, Unchecked.DoubleToUInt8, Checked.DoubleToUInt8);

        numFailing += Validate<byte>(-0.9999999999999999, byte.MinValue, expectsOverflow: false, Unchecked.DoubleToUInt8, Checked.DoubleToUInt8);
        numFailing += Validate<byte>(+255.99999999999997, byte.MaxValue, expectsOverflow: false, Unchecked.DoubleToUInt8, Checked.DoubleToUInt8);

        numFailing += Validate<byte>(-0.000, byte.MinValue, expectsOverflow: false, Unchecked.DoubleToUInt8, Checked.DoubleToUInt8);
        numFailing += Validate<byte>(+255.0, byte.MaxValue, expectsOverflow: false, Unchecked.DoubleToUInt8, Checked.DoubleToUInt8);

        numFailing += Validate<byte>(-Math.PI, -0, expectsOverflow: true, Unchecked.DoubleToUInt8, Checked.DoubleToUInt8);
        numFailing += Validate<byte>(+Math.PI, +3, expectsOverflow: false, Unchecked.DoubleToUInt8, Checked.DoubleToUInt8);

        // Double -> UInt16

        numFailing += Validate<ushort>(double.NaN, 0, expectsOverflow: true, Unchecked.DoubleToUInt16, Checked.DoubleToUInt16);

        numFailing += Validate<ushort>(double.NegativeInfinity, ushort.MinValue, expectsOverflow: true, Unchecked.DoubleToUInt16, Checked.DoubleToUInt16);
        numFailing += Validate<ushort>(double.PositiveInfinity, ushort.MaxValue, expectsOverflow: true, Unchecked.DoubleToUInt16, Checked.DoubleToUInt16);

        numFailing += Validate<ushort>(-1.00000, ushort.MinValue, expectsOverflow: true, Unchecked.DoubleToUInt16, Checked.DoubleToUInt16);
        numFailing += Validate<ushort>(+65536.0, ushort.MaxValue, expectsOverflow: true, Unchecked.DoubleToUInt16, Checked.DoubleToUInt16);

        numFailing += Validate<ushort>(-0.9999999999999999, ushort.MinValue, expectsOverflow: false, Unchecked.DoubleToUInt16, Checked.DoubleToUInt16);
        numFailing += Validate<ushort>(+65535.999999999990, ushort.MaxValue, expectsOverflow: false, Unchecked.DoubleToUInt16, Checked.DoubleToUInt16);

        numFailing += Validate<ushort>(-0.00000, ushort.MinValue, expectsOverflow: false, Unchecked.DoubleToUInt16, Checked.DoubleToUInt16);
        numFailing += Validate<ushort>(+65535.0, ushort.MaxValue, expectsOverflow: false, Unchecked.DoubleToUInt16, Checked.DoubleToUInt16);

        numFailing += Validate<ushort>(-Math.PI, -0, expectsOverflow: true, Unchecked.DoubleToUInt16, Checked.DoubleToUInt16);
        numFailing += Validate<ushort>(+Math.PI, +3, expectsOverflow: false, Unchecked.DoubleToUInt16, Checked.DoubleToUInt16);

        // Double -> UInt32

        numFailing += Validate<uint>(double.NaN, 0, expectsOverflow: true, Unchecked.DoubleToUInt32, Checked.DoubleToUInt32);

        numFailing += Validate<uint>(double.NegativeInfinity, uint.MinValue, expectsOverflow: true, Unchecked.DoubleToUInt32, Checked.DoubleToUInt32);
        numFailing += Validate<uint>(double.PositiveInfinity, uint.MaxValue, expectsOverflow: true, Unchecked.DoubleToUInt32, Checked.DoubleToUInt32);

        numFailing += Validate<uint>(-1.0000000000, uint.MinValue, expectsOverflow: true, Unchecked.DoubleToUInt32, Checked.DoubleToUInt32);
        numFailing += Validate<uint>(+4294967296.0, uint.MaxValue, expectsOverflow: true, Unchecked.DoubleToUInt32, Checked.DoubleToUInt32);

        numFailing += Validate<uint>(-0.9999999999999999, uint.MinValue, expectsOverflow: false, Unchecked.DoubleToUInt32, Checked.DoubleToUInt32);
        numFailing += Validate<uint>(+4294967295.9999995, uint.MaxValue, expectsOverflow: false, Unchecked.DoubleToUInt32, Checked.DoubleToUInt32);

        numFailing += Validate<uint>(-0.0000000000, uint.MinValue, expectsOverflow: false, Unchecked.DoubleToUInt32, Checked.DoubleToUInt32);
        numFailing += Validate<uint>(+4294967295.0, uint.MaxValue, expectsOverflow: false, Unchecked.DoubleToUInt32, Checked.DoubleToUInt32);

        numFailing += Validate<uint>(-Math.PI, -0, expectsOverflow: true, Unchecked.DoubleToUInt32, Checked.DoubleToUInt32);
        numFailing += Validate<uint>(+Math.PI, +3, expectsOverflow: false, Unchecked.DoubleToUInt32, Checked.DoubleToUInt32);

        // Double -> UInt64

        numFailing += Validate<ulong>(double.NaN, 0, expectsOverflow: true, Unchecked.DoubleToUInt64, Checked.DoubleToUInt64);

        numFailing += Validate<ulong>(double.NegativeInfinity, ulong.MinValue, expectsOverflow: true, Unchecked.DoubleToUInt64, Checked.DoubleToUInt64);
        numFailing += Validate<ulong>(double.PositiveInfinity, ulong.MaxValue, expectsOverflow: true, Unchecked.DoubleToUInt64, Checked.DoubleToUInt64);

        numFailing += Validate<ulong>(-1.00000000000000000000, ulong.MinValue, expectsOverflow: true, Unchecked.DoubleToUInt64, Checked.DoubleToUInt64);
        numFailing += Validate<ulong>(+18446744073709551616.0, ulong.MaxValue, expectsOverflow: true, Unchecked.DoubleToUInt64, Checked.DoubleToUInt64);

        numFailing += Validate<ulong>(-0.99999999999999990000, ulong.MinValue, expectsOverflow: false, Unchecked.DoubleToUInt64, Checked.DoubleToUInt64);
        numFailing += Validate<ulong>(+18446744073709549568.0, 18446744073709549568, expectsOverflow: false, Unchecked.DoubleToUInt64, Checked.DoubleToUInt64);

        numFailing += Validate<ulong>(-0.00000000000000000000, ulong.MinValue, expectsOverflow: false, Unchecked.DoubleToUInt64, Checked.DoubleToUInt64);
        numFailing += Validate<ulong>(+18446744073709551615.0, ulong.MaxValue, expectsOverflow: true, Unchecked.DoubleToUInt64, Checked.DoubleToUInt64);

        numFailing += Validate<ulong>(-Math.PI, -0, expectsOverflow: true, Unchecked.DoubleToUInt64, Checked.DoubleToUInt64);
        numFailing += Validate<ulong>(+Math.PI, +3, expectsOverflow: false, Unchecked.DoubleToUInt64, Checked.DoubleToUInt64);

        return (numFailing == 0) ? Pass : Fail;
    }

    public static int Validate<T>(double x, T expected, bool expectsOverflow, Func<double, T> uncheckedFunc, Func<double, T> checkedFunc)
        where T : IEquatable<T>
    {
        int numFailing = 0;

        T uncheckedResult = uncheckedFunc(x);

        if (!uncheckedResult.Equals(expected))
        {
            Console.WriteLine($"Unchecked conversion for Double -> {typeof(T)} failed; Input: {x}; Expected {expected}; Actual {uncheckedResult}");
            numFailing += 1;
        }

        var caughtOverflow = false;

        try
        {
            T checkedResult = checkedFunc(x);
            if (!checkedResult.Equals(expected))
            {
                Console.WriteLine($"Checked conversion for Double -> {typeof(T)} failed; Input: {x}; Expected {expected}; Actual {checkedResult}");
                numFailing += 1;
            }
        }
        catch (OverflowException)
        {
            caughtOverflow = true;
        }

        if (caughtOverflow != expectsOverflow)
        {
            Console.WriteLine($"Checked conversion for Double -> {typeof(T)} failed; Input: {x}; Expected Overflow {expectsOverflow}; Caught Overflow {caughtOverflow}");
            numFailing += 1;
        }

        return numFailing;
    }
}

public class Unchecked
{
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static sbyte DoubleToInt8(double x) => unchecked((sbyte)(x));

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static short DoubleToInt16(double x) => unchecked((short)(x));

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int DoubleToInt32(double x) => unchecked((int)(x));

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static long DoubleToInt64(double x) => unchecked((long)(x));

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static byte DoubleToUInt8(double x) => unchecked((byte)(x));

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static ushort DoubleToUInt16(double x) => unchecked((ushort)(x));

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static uint DoubleToUInt32(double x) => unchecked((uint)(x));

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static ulong DoubleToUInt64(double x) => unchecked((ulong)(x));
}

public class Checked
{
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static sbyte DoubleToInt8(double x) => checked((sbyte)(x));

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static short DoubleToInt16(double x) => checked((short)(x));

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int DoubleToInt32(double x) => checked((int)(x));

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static long DoubleToInt64(double x) => checked((long)(x));

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static byte DoubleToUInt8(double x) => checked((byte)(x));

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static ushort DoubleToUInt16(double x) => checked((ushort)(x));

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static uint DoubleToUInt32(double x) => checked((uint)(x));

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static ulong DoubleToUInt64(double x) => checked((ulong)(x));
}
