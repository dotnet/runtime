// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;

public class BringUpTest_FltCast
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
        //  * - Int32/Int64/UInt32/UInt64 are a special case where this will overflow as T.MaxValue is not representable and rounds up to (T.MaxValue + 1)
        //  *
        //  * NegativePi, which should return -3 but which will overflow for unsigned values and should return 0 instead
        //  * PositivePi, which should return +3 and not overflow

        int numFailing = 0;

        // Single -> Int8

        numFailing += Validate<sbyte>(float.NaN, 0, expectsOverflow: true, Unchecked.SingleToInt8, Checked.SingleToInt8);

        numFailing += Validate<sbyte>(float.NegativeInfinity, sbyte.MinValue, expectsOverflow: true, Unchecked.SingleToInt8, Checked.SingleToInt8);
        numFailing += Validate<sbyte>(float.PositiveInfinity, sbyte.MaxValue, expectsOverflow: true, Unchecked.SingleToInt8, Checked.SingleToInt8);

        numFailing += Validate<sbyte>(-129.0f, sbyte.MinValue, expectsOverflow: true, Unchecked.SingleToInt8, Checked.SingleToInt8);
        numFailing += Validate<sbyte>(+128.0f, sbyte.MaxValue, expectsOverflow: true, Unchecked.SingleToInt8, Checked.SingleToInt8);

        numFailing += Validate<sbyte>(-128.99998f, sbyte.MinValue, expectsOverflow: false, Unchecked.SingleToInt8, Checked.SingleToInt8);
        numFailing += Validate<sbyte>(+127.99999f, sbyte.MaxValue, expectsOverflow: false, Unchecked.SingleToInt8, Checked.SingleToInt8);

        numFailing += Validate<sbyte>(-128.0f, sbyte.MinValue, expectsOverflow: false, Unchecked.SingleToInt8, Checked.SingleToInt8);
        numFailing += Validate<sbyte>(+127.0f, sbyte.MaxValue, expectsOverflow: false, Unchecked.SingleToInt8, Checked.SingleToInt8);

        numFailing += Validate<sbyte>(-MathF.PI, -3, expectsOverflow: false, Unchecked.SingleToInt8, Checked.SingleToInt8);
        numFailing += Validate<sbyte>(+MathF.PI, +3, expectsOverflow: false, Unchecked.SingleToInt8, Checked.SingleToInt8);

        // Single -> Int16

        numFailing += Validate<short>(float.NaN, 0, expectsOverflow: true, Unchecked.SingleToInt16, Checked.SingleToInt16);

        numFailing += Validate<short>(float.NegativeInfinity, short.MinValue, expectsOverflow: true, Unchecked.SingleToInt16, Checked.SingleToInt16);
        numFailing += Validate<short>(float.PositiveInfinity, short.MaxValue, expectsOverflow: true, Unchecked.SingleToInt16, Checked.SingleToInt16);

        numFailing += Validate<short>(-32769.0f, short.MinValue, expectsOverflow: true, Unchecked.SingleToInt16, Checked.SingleToInt16);
        numFailing += Validate<short>(+32768.0f, short.MaxValue, expectsOverflow: true, Unchecked.SingleToInt16, Checked.SingleToInt16);

        numFailing += Validate<short>(-32768.996f, short.MinValue, expectsOverflow: false, Unchecked.SingleToInt16, Checked.SingleToInt16);
        numFailing += Validate<short>(+32767.998f, short.MaxValue, expectsOverflow: false, Unchecked.SingleToInt16, Checked.SingleToInt16);

        numFailing += Validate<short>(-32768.0f, short.MinValue, expectsOverflow: false, Unchecked.SingleToInt16, Checked.SingleToInt16);
        numFailing += Validate<short>(+32767.0f, short.MaxValue, expectsOverflow: false, Unchecked.SingleToInt16, Checked.SingleToInt16);

        numFailing += Validate<short>(-MathF.PI, -3, expectsOverflow: false, Unchecked.SingleToInt16, Checked.SingleToInt16);
        numFailing += Validate<short>(+MathF.PI, +3, expectsOverflow: false, Unchecked.SingleToInt16, Checked.SingleToInt16);

        // Single -> Int32

        numFailing += Validate<int>(float.NaN, 0, expectsOverflow: true, Unchecked.SingleToInt32, Checked.SingleToInt32);

        numFailing += Validate<int>(float.NegativeInfinity, int.MinValue, expectsOverflow: true, Unchecked.SingleToInt32, Checked.SingleToInt32);
        numFailing += Validate<int>(float.PositiveInfinity, int.MaxValue, expectsOverflow: true, Unchecked.SingleToInt32, Checked.SingleToInt32);

        numFailing += Validate<int>(-2147483904.0f, int.MinValue, expectsOverflow: true, Unchecked.SingleToInt32, Checked.SingleToInt32);
        numFailing += Validate<int>(+2147483648.0f, int.MaxValue, expectsOverflow: true, Unchecked.SingleToInt32, Checked.SingleToInt32);

        numFailing += Validate<int>(-2147483648.0f, int.MinValue, expectsOverflow: false, Unchecked.SingleToInt32, Checked.SingleToInt32);
        numFailing += Validate<int>(+2147483520.0f, 2147483520, expectsOverflow: false, Unchecked.SingleToInt32, Checked.SingleToInt32);

        numFailing += Validate<int>(-2147483648.0f, int.MinValue, expectsOverflow: false, Unchecked.SingleToInt32, Checked.SingleToInt32);
        numFailing += Validate<int>(+2147483647.0f, int.MaxValue, expectsOverflow: true, Unchecked.SingleToInt32, Checked.SingleToInt32);

        numFailing += Validate<int>(-MathF.PI, -3, expectsOverflow: false, Unchecked.SingleToInt32, Checked.SingleToInt32);
        numFailing += Validate<int>(+MathF.PI, +3, expectsOverflow: false, Unchecked.SingleToInt32, Checked.SingleToInt32);

        // Single -> Int64

        numFailing += Validate<long>(float.NaN, 0, expectsOverflow: true, Unchecked.SingleToInt64, Checked.SingleToInt64);

        numFailing += Validate<long>(float.NegativeInfinity, long.MinValue, expectsOverflow: true, Unchecked.SingleToInt64, Checked.SingleToInt64);
        numFailing += Validate<long>(float.PositiveInfinity, long.MaxValue, expectsOverflow: true, Unchecked.SingleToInt64, Checked.SingleToInt64);

        numFailing += Validate<long>(-9223373136366403584.0f, long.MinValue, expectsOverflow: true, Unchecked.SingleToInt64, Checked.SingleToInt64);
        numFailing += Validate<long>(+9223372036854775808.0f, long.MaxValue, expectsOverflow: true, Unchecked.SingleToInt64, Checked.SingleToInt64);

        numFailing += Validate<long>(-9223372036854775808.0f, long.MinValue, expectsOverflow: false, Unchecked.SingleToInt64, Checked.SingleToInt64);
        numFailing += Validate<long>(+9223371487098961920.0f, 9223371487098961920, expectsOverflow: false, Unchecked.SingleToInt64, Checked.SingleToInt64);

        numFailing += Validate<long>(-9223372036854775808.0f, long.MinValue, expectsOverflow: false, Unchecked.SingleToInt64, Checked.SingleToInt64);
        numFailing += Validate<long>(+9223372036854775807.0f, long.MaxValue, expectsOverflow: true, Unchecked.SingleToInt64, Checked.SingleToInt64);

        numFailing += Validate<long>(-MathF.PI, -3, expectsOverflow: false, Unchecked.SingleToInt64, Checked.SingleToInt64);
        numFailing += Validate<long>(+MathF.PI, +3, expectsOverflow: false, Unchecked.SingleToInt64, Checked.SingleToInt64);

        // Single -> UInt8

        numFailing += Validate<byte>(float.NegativeInfinity, byte.MinValue, expectsOverflow: true, Unchecked.SingleToUInt8, Checked.SingleToUInt8);
        numFailing += Validate<byte>(float.NaN, 0, expectsOverflow: true, Unchecked.SingleToUInt8, Checked.SingleToUInt8);
        numFailing += Validate<byte>(float.PositiveInfinity, byte.MaxValue, expectsOverflow: true, Unchecked.SingleToUInt8, Checked.SingleToUInt8);

        numFailing += Validate<byte>(-1.000f, byte.MinValue, expectsOverflow: true, Unchecked.SingleToUInt8, Checked.SingleToUInt8);
        numFailing += Validate<byte>(+256.0f, byte.MaxValue, expectsOverflow: true, Unchecked.SingleToUInt8, Checked.SingleToUInt8);

        numFailing += Validate<byte>(-0.99999994f, byte.MinValue, expectsOverflow: false, Unchecked.SingleToUInt8, Checked.SingleToUInt8);
        numFailing += Validate<byte>(+255.999980f, byte.MaxValue, expectsOverflow: false, Unchecked.SingleToUInt8, Checked.SingleToUInt8);

        numFailing += Validate<byte>(-0.000f, byte.MinValue, expectsOverflow: false, Unchecked.SingleToUInt8, Checked.SingleToUInt8);
        numFailing += Validate<byte>(+255.0f, byte.MaxValue, expectsOverflow: false, Unchecked.SingleToUInt8, Checked.SingleToUInt8);

        numFailing += Validate<byte>(-MathF.PI, -0, expectsOverflow: true, Unchecked.SingleToUInt8, Checked.SingleToUInt8);
        numFailing += Validate<byte>(+MathF.PI, +3, expectsOverflow: false, Unchecked.SingleToUInt8, Checked.SingleToUInt8);

        // Single -> UInt16

        numFailing += Validate<ushort>(float.NaN, 0, expectsOverflow: true, Unchecked.SingleToUInt16, Checked.SingleToUInt16);

        numFailing += Validate<ushort>(float.NegativeInfinity, ushort.MinValue, expectsOverflow: true, Unchecked.SingleToUInt16, Checked.SingleToUInt16);
        numFailing += Validate<ushort>(float.PositiveInfinity, ushort.MaxValue, expectsOverflow: true, Unchecked.SingleToUInt16, Checked.SingleToUInt16);

        numFailing += Validate<ushort>(-1.00000f, ushort.MinValue, expectsOverflow: true, Unchecked.SingleToUInt16, Checked.SingleToUInt16);
        numFailing += Validate<ushort>(+65536.0f, ushort.MaxValue, expectsOverflow: true, Unchecked.SingleToUInt16, Checked.SingleToUInt16);

        numFailing += Validate<ushort>(-0.99999994f, ushort.MinValue, expectsOverflow: false, Unchecked.SingleToUInt16, Checked.SingleToUInt16);
        numFailing += Validate<ushort>(+65535.9960f, ushort.MaxValue, expectsOverflow: false, Unchecked.SingleToUInt16, Checked.SingleToUInt16);

        numFailing += Validate<ushort>(-0.00000f, ushort.MinValue, expectsOverflow: false, Unchecked.SingleToUInt16, Checked.SingleToUInt16);
        numFailing += Validate<ushort>(+65535.0f, ushort.MaxValue, expectsOverflow: false, Unchecked.SingleToUInt16, Checked.SingleToUInt16);

        numFailing += Validate<ushort>(-MathF.PI, -0, expectsOverflow: true, Unchecked.SingleToUInt16, Checked.SingleToUInt16);
        numFailing += Validate<ushort>(+MathF.PI, +3, expectsOverflow: false, Unchecked.SingleToUInt16, Checked.SingleToUInt16);

        // Single -> UInt32

        numFailing += Validate<uint>(float.NaN, 0, expectsOverflow: true, Unchecked.SingleToUInt32, Checked.SingleToUInt32);

        numFailing += Validate<uint>(float.NegativeInfinity, uint.MinValue, expectsOverflow: true, Unchecked.SingleToUInt32, Checked.SingleToUInt32);
        numFailing += Validate<uint>(float.PositiveInfinity, uint.MaxValue, expectsOverflow: true, Unchecked.SingleToUInt32, Checked.SingleToUInt32);

        numFailing += Validate<uint>(-1.0000000000f, uint.MinValue, expectsOverflow: true, Unchecked.SingleToUInt32, Checked.SingleToUInt32);
        numFailing += Validate<uint>(+4294967296.0f, uint.MaxValue, expectsOverflow: true, Unchecked.SingleToUInt32, Checked.SingleToUInt32);

        numFailing += Validate<uint>(-0.9999999400f, uint.MinValue, expectsOverflow: false, Unchecked.SingleToUInt32, Checked.SingleToUInt32);
        numFailing += Validate<uint>(+4294967040.0f, 4294967040, expectsOverflow: false, Unchecked.SingleToUInt32, Checked.SingleToUInt32);

        numFailing += Validate<uint>(-0.0000000000f, uint.MinValue, expectsOverflow: false, Unchecked.SingleToUInt32, Checked.SingleToUInt32);
        numFailing += Validate<uint>(+4294967295.0f, uint.MaxValue, expectsOverflow: true, Unchecked.SingleToUInt32, Checked.SingleToUInt32);

        numFailing += Validate<uint>(-MathF.PI, -0, expectsOverflow: true, Unchecked.SingleToUInt32, Checked.SingleToUInt32);
        numFailing += Validate<uint>(+MathF.PI, +3, expectsOverflow: false, Unchecked.SingleToUInt32, Checked.SingleToUInt32);

        // Single -> UInt64

        numFailing += Validate<ulong>(float.NaN, 0, expectsOverflow: true, Unchecked.SingleToUInt64, Checked.SingleToUInt64);

        numFailing += Validate<ulong>(float.NegativeInfinity, ulong.MinValue, expectsOverflow: true, Unchecked.SingleToUInt64, Checked.SingleToUInt64);
        numFailing += Validate<ulong>(float.PositiveInfinity, ulong.MaxValue, expectsOverflow: true, Unchecked.SingleToUInt64, Checked.SingleToUInt64);

        numFailing += Validate<ulong>(-1.00000000000000000000f, ulong.MinValue, expectsOverflow: true, Unchecked.SingleToUInt64, Checked.SingleToUInt64);
        numFailing += Validate<ulong>(+18446744073709551616.0f, ulong.MaxValue, expectsOverflow: true, Unchecked.SingleToUInt64, Checked.SingleToUInt64);

        numFailing += Validate<ulong>(-0.99999994000000000000f, ulong.MinValue, expectsOverflow: false, Unchecked.SingleToUInt64, Checked.SingleToUInt64);
        numFailing += Validate<ulong>(+18446742974197923840.0f, 18446742974197923840, expectsOverflow: false, Unchecked.SingleToUInt64, Checked.SingleToUInt64);

        numFailing += Validate<ulong>(-0.00000000000000000000f, ulong.MinValue, expectsOverflow: false, Unchecked.SingleToUInt64, Checked.SingleToUInt64);
        numFailing += Validate<ulong>(+18446744073709551615.0f, ulong.MaxValue, expectsOverflow: true, Unchecked.SingleToUInt64, Checked.SingleToUInt64);

        numFailing += Validate<ulong>(-MathF.PI, -0, expectsOverflow: true, Unchecked.SingleToUInt64, Checked.SingleToUInt64);
        numFailing += Validate<ulong>(+MathF.PI, +3, expectsOverflow: false, Unchecked.SingleToUInt64, Checked.SingleToUInt64);

        return (numFailing == 0) ? Pass : Fail;
    }

    public static int Validate<T>(float x, T expected, bool expectsOverflow, Func<float, T> uncheckedFunc, Func<float, T> checkedFunc)
        where T : IEquatable<T>
    {
        int numFailing = 0;

        T uncheckedResult = uncheckedFunc(x);

        if (!uncheckedResult.Equals(expected))
        {
            Console.WriteLine($"Unchecked conversion for Single -> {typeof(T)} failed; Input: {x}; Expected {expected}; Actual {uncheckedResult}");
            numFailing += 1;
        }

        var caughtOverflow = false;

        try
        {
            T checkedResult = checkedFunc(x);
            if (!checkedResult.Equals(expected))
            {
                Console.WriteLine($"Checked conversion for Single -> {typeof(T)} failed; Input: {x}; Expected {expected}; Actual {checkedResult}");
                numFailing += 1;
            }
        }
        catch (OverflowException)
        {
            caughtOverflow = true;
        }

        if (caughtOverflow != expectsOverflow)
        {
            Console.WriteLine($"Checked conversion for Single -> {typeof(T)} failed; Input: {x}; Expected Overflow {expectsOverflow}; Caught Overflow {caughtOverflow}");
            numFailing += 1;
        }

        return numFailing;
    }
}

public class Unchecked
{
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static sbyte SingleToInt8(float x) => unchecked((sbyte)(x));

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static short SingleToInt16(float x) => unchecked((short)(x));

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int SingleToInt32(float x) => unchecked((int)(x));

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static long SingleToInt64(float x) => unchecked((long)(x));

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static byte SingleToUInt8(float x) => unchecked((byte)(x));

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static ushort SingleToUInt16(float x) => unchecked((ushort)(x));

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static uint SingleToUInt32(float x) => unchecked((uint)(x));

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static ulong SingleToUInt64(float x) => unchecked((ulong)(x));
}

public class Checked
{
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static sbyte SingleToInt8(float x) => checked((sbyte)(x));

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static short SingleToInt16(float x) => checked((short)(x));

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int SingleToInt32(float x) => checked((int)(x));

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static long SingleToInt64(float x) => checked((long)(x));

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static byte SingleToUInt8(float x) => checked((byte)(x));

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static ushort SingleToUInt16(float x) => checked((ushort)(x));

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static uint SingleToUInt32(float x) => checked((uint)(x));

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static ulong SingleToUInt64(float x) => checked((ulong)(x));
}
