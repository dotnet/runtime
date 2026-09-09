// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// Floating-point semantics that a soft-float target (RISC-V lp64, where the
// JIT expands every FP operation into a helper call) must reproduce bit for
// bit. All expected values are exact IEEE 754 results, so the test is equally
// valid on hard-float targets: arithmetic rounding, comparison semantics with
// NaN (including the unordered branch forms), the saturating .NET conversions
// to every integer width, the checked conversions, conversions from every
// integer width, float <-> double, negation, remainder, Math intrinsics that
// become calls, values passed and returned across call boundaries in integer
// registers, and the sign extension of float bits reinterpreted as int.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class SoftFloat
{
    static int s_failures;

    static void Check(bool ok, string name)
    {
        if (!ok)
        {
            s_failures++;
            Console.WriteLine("FAIL: " + name);
        }
    }

    static void CheckBits(double actual, ulong expected, string name)
        => Check((ulong)BitConverter.DoubleToInt64Bits(actual) == expected,
                 name + ": " + BitConverter.DoubleToInt64Bits(actual).ToString("X16") + " != " + expected.ToString("X16"));

    static void CheckBits(float actual, uint expected, string name)
        => Check((uint)BitConverter.SingleToInt32Bits(actual) == expected,
                 name + ": " + BitConverter.SingleToInt32Bits(actual).ToString("X8") + " != " + expected.ToString("X8"));

    [MethodImpl(MethodImplOptions.NoInlining)] static double D(double x) => x;
    [MethodImpl(MethodImplOptions.NoInlining)] static float F(float x) => x;
    [MethodImpl(MethodImplOptions.NoInlining)] static int I(int x) => x;
    [MethodImpl(MethodImplOptions.NoInlining)] static long L(long x) => x;

    static void Arithmetic()
    {
        double a = D(0.1), b = D(0.2);
        CheckBits(a + b, 0x3FD3333333333334, "0.1 + 0.2");
        CheckBits(D(1.0) / D(3.0), 0x3FD5555555555555, "1 / 3");
        CheckBits(D(100.0) / D(7.0), 0x402C924924924925, "100 / 7");
        CheckBits(D(3.7) * D(2.1), 0x401F147AE147AE15, "3.7 * 2.1");
        CheckBits(D(1e308) * D(10.0), 0x7FF0000000000000, "overflow to +inf");
        CheckBits(D(-1e308) * D(10.0), 0xFFF0000000000000, "overflow to -inf");
        CheckBits(D(double.Epsilon) * D(0.5), 0x0000000000000000, "underflow to 0");
        CheckBits(D(0.0) + D(-0.0), 0x0000000000000000, "0 + -0 = +0");
        CheckBits(D(-0.0) - D(0.0), 0x8000000000000000, "-0 - 0 = -0");
        CheckBits(D(-0.0) * D(1.0), 0x8000000000000000, "-0 * 1 = -0");
        Check(double.IsNaN(D(double.PositiveInfinity) - D(double.PositiveInfinity)), "inf - inf");
        Check(double.IsNaN(D(0.0) / D(0.0)), "0 / 0");
        CheckBits(D(1.0) / D(0.0), 0x7FF0000000000000, "1 / 0");
        CheckBits(D(-1.0) / D(0.0), 0xFFF0000000000000, "-1 / 0");
        CheckBits(D(9007199254740992.0) + D(1.0), 0x4340000000000000, "2^53 + 1 rounds to even");

        float fa = F(0.1f), fb = F(0.2f);
        CheckBits(fa + fb, 0x3E99999Au, "0.1f + 0.2f");
        CheckBits(F(1.0f) / F(3.0f), 0x3EAAAAABu, "1f / 3f");
        CheckBits(F(7.0f) / F(3.0f), 0x40155555u, "7f / 3f");
        CheckBits(F(1e10f) * F(1e10f), 0x60AD78ECu, "1e10f * 1e10f");
        CheckBits(F(float.MaxValue) * F(2.0f), 0x7F800000u, "float overflow to +inf");
        CheckBits(F(-0.0f) * F(1.0f), 0x80000000u, "-0f * 1f = -0f");
        Check(float.IsNaN(F(0.0f) / F(0.0f)), "0f / 0f");

        // Remainder goes through the pre-existing helpers.
        Check(D(5.5) % D(2.0) == 1.5, "5.5 % 2");
        Check(D(-5.5) % D(2.0) == -1.5, "-5.5 % 2");
        Check(D(5.5) % D(double.PositiveInfinity) == 5.5, "x % inf");
        Check(double.IsNaN(D(5.5) % D(0.0)), "x % 0");
        Check(F(5.5f) % F(2.0f) == 1.5f, "5.5f % 2f");

        // The same expression twice: CSE / value numbering of the helper calls.
        double c1 = a * b + a / b;
        double c2 = a * b + a / b;
        Check(BitConverter.DoubleToInt64Bits(c1) == BitConverter.DoubleToInt64Bits(c2), "CSE");
    }

    static void Negation()
    {
        CheckBits(-D(1.5), 0xBFF8000000000000, "-1.5");
        CheckBits(-D(0.0), 0x8000000000000000, "-(+0)");
        CheckBits(-D(-0.0), 0x0000000000000000, "-(-0)");
        Check(double.IsNaN(-D(double.NaN)), "-NaN");
        CheckBits(-D(double.PositiveInfinity), 0xFFF0000000000000, "-inf");
        CheckBits(-F(1.5f), 0xBFC00000u, "-1.5f");
        CheckBits(-F(0.0f), 0x80000000u, "-(+0f)");
        Check(float.IsNaN(-F(float.NaN)), "-NaNf");
    }

    static void Comparisons()
    {
        double nan = D(double.NaN), one = D(1.0), two = D(2.0), nz = D(-0.0), pz = D(0.0);

        Check(!(nan == nan), "NaN == NaN");
        Check(nan != nan, "NaN != NaN");
        Check(!(nan < one) && !(nan <= one) && !(nan > one) && !(nan >= one), "NaN ordered compares");
        Check(!(one < nan) && !(one <= nan) && !(one > nan) && !(one >= nan), "ordered compares with NaN rhs");
        Check(one < two && one <= two && two > one && two >= one && one <= one && one >= one, "ordered");
        Check(!(two < one) && !(two <= one) && !(one > two) && !(one >= two), "ordered false");
        Check(nz == pz && !(nz < pz) && nz <= pz && nz >= pz, "-0 == +0");
        Check(D(double.NegativeInfinity) < D(double.MinValue) && D(double.MaxValue) < D(double.PositiveInfinity), "infinities");

        // Unordered branch forms (bge.un etc.) come from the negated conditions.
        int hits = 0;
        if (!(nan < one)) hits |= 1;   // uge
        if (!(nan <= one)) hits |= 2;  // ugt
        if (!(nan > one)) hits |= 4;   // ule
        if (!(nan >= one)) hits |= 8;  // ult
        if (!(nan == one)) hits |= 16; // une
        Check(hits == 31, "unordered branches with NaN");
        hits = 0;
        if (!(one < two)) hits |= 1;
        if (!(two <= one)) hits |= 2;
        if (!(one > two)) hits |= 4;
        if (!(two >= one)) hits |= 8;
        if (!(one == two)) hits |= 16;
        Check(hits == 2 + 4 + 16, "unordered branches, ordered operands");

        float fn = F(float.NaN), f1 = F(1.0f), f2 = F(2.0f);
        Check(!(fn == fn) && fn != fn && !(fn < f1) && !(fn >= f1), "float NaN compares");
        Check(f1 < f2 && f2 >= f1 && !(f2 <= f1), "float ordered");
        Check(F(-0.0f) == F(0.0f), "-0f == +0f");
    }

    static void ToInteger()
    {
        // .NET semantics: NaN -> 0, saturation to the range of the destination.
        Check((int)D(double.NaN) == 0, "(int)NaN");
        Check((int)D(1e10) == int.MaxValue, "(int)1e10");
        Check((int)D(-1e10) == int.MinValue, "(int)-1e10");
        Check((int)D(2147483647.9) == int.MaxValue, "(int)2147483647.9");
        Check((int)D(-2147483648.9) == int.MinValue, "(int)-2147483648.9");
        Check((int)D(-1.9) == -1 && (int)D(1.9) == 1, "(int) truncation");
        Check((int)D(double.PositiveInfinity) == int.MaxValue && (int)D(double.NegativeInfinity) == int.MinValue, "(int)inf");
        Check((uint)D(-1.0) == 0 && (uint)D(double.NaN) == 0, "(uint) negative/NaN");
        Check((uint)D(5e9) == uint.MaxValue && (uint)D(4294967295.9) == uint.MaxValue, "(uint) saturation");
        Check((uint)D(3.99) == 3 && (uint)D(4294967295.0) == uint.MaxValue, "(uint) values");
        Check((long)D(double.NaN) == 0 && (long)D(1e30) == long.MaxValue && (long)D(-1e30) == long.MinValue, "(long)");
        Check((long)D(-9223372036854775808.0) == long.MinValue && (long)D(9223372036854775807.0) == long.MaxValue, "(long) edges");
        Check((ulong)D(-1.0) == 0 && (ulong)D(1e30) == ulong.MaxValue && (ulong)D(18446744073709551615.0) == ulong.MaxValue, "(ulong)");
        Check((ulong)D(9223372036854775808.0) == 9223372036854775808UL, "(ulong)2^63");
        // Conversions to the small integer types saturate as well (.NET 11).
        Check((byte)D(300.0) == 255 && (byte)D(-5.0) == 0 && (byte)D(double.NaN) == 0, "(byte)");
        Check((sbyte)D(-200.0) == -128 && (sbyte)D(200.0) == 127, "(sbyte)");
        Check((short)D(70000.0) == 32767 && (ushort)D(70000.0) == 65535 && (ushort)D(-1.0) == 0, "(short)/(ushort)");

        Check((int)F(1e10f) == int.MaxValue && (int)F(float.NaN) == 0 && (int)F(-2.5f) == -2, "(int)float");
        Check((long)F(1e30f) == long.MaxValue && (ulong)F(-1.0f) == 0, "(long)/(ulong) float");
        Check((byte)F(300.0f) == 255, "(byte)float");

        // Checked conversions throw for NaN and out-of-range values.
        Check(Throws(() => checked((int)D(1e10))), "checked (int)1e10");
        Check(Throws(() => checked((int)D(double.NaN))), "checked (int)NaN");
        Check(Throws(() => checked((uint)D(-1.0))), "checked (uint)-1");
        Check(Throws(() => checked((long)D(1e30))), "checked (long)1e30");
        Check(Throws(() => checked((byte)D(256.0))), "checked (byte)256");
        Check(checked((int)D(-2147483648.0)) == int.MinValue && checked((int)D(2147483647.0)) == int.MaxValue, "checked (int) edges");
        Check(checked((long)F(1e18f)) == 999999984306749440L, "checked (long)1e18f");
    }

    static void FromInteger()
    {
        CheckBits((double)I(int.MinValue), 0xC1E0000000000000, "(double)int.MinValue");
        CheckBits((double)(uint)I(-1), 0x41EFFFFFFFE00000, "(double)uint.MaxValue");
        CheckBits((double)L(long.MaxValue), 0x43E0000000000000, "(double)long.MaxValue");
        CheckBits((double)(ulong)L(-1), 0x43F0000000000000, "(double)ulong.MaxValue");
        CheckBits((double)L(long.MinValue), 0xC3E0000000000000, "(double)long.MinValue");
        CheckBits((double)L(9007199254740993), 0x4340000000000000, "(double)(2^53+1) rounds to even");
        CheckBits((float)I(16777217), 0x4B800000u, "(float)16777217");
        CheckBits((float)L(long.MaxValue), 0x5F000000u, "(float)long.MaxValue");
        CheckBits((float)(ulong)L(-1), 0x5F800000u, "(float)ulong.MaxValue");
        CheckBits((float)(uint)I(-1), 0x4F800000u, "(float)uint.MaxValue");
        CheckBits((double)(short)I(-3), 0xC008000000000000, "(double)short");
        CheckBits((double)(byte)I(255), 0x406FE00000000000, "(double)byte");
    }

    static void FloatDouble()
    {
        CheckBits((double)F(0.1f), 0x3FB99999A0000000, "(double)0.1f");
        CheckBits((float)D(0.1), 0x3DCCCCCDu, "(float)0.1");
        CheckBits((float)D(1e300), 0x7F800000u, "(float)1e300");
        CheckBits((float)D(-1e300), 0xFF800000u, "(float)-1e300");
        CheckBits((float)D(1e-50), 0x00000000u, "(float)1e-50");
        CheckBits((float)D(-0.0), 0x80000000u, "(float)-0.0");
        Check(float.IsNaN((float)D(double.NaN)), "(float)NaN");
        Check(double.IsNaN((double)F(float.NaN)), "(double)NaNf");
        CheckBits((double)(float)D(16777217.0), 0x4170000000000000, "(double)(float)16777217.0");
    }

    static void Bits()
    {
        // A float in an integer register may carry undefined upper bits; the
        // reinterpretation as int must be a proper sign-extended int.
        int bits = BitConverter.SingleToInt32Bits(F(-1.5f));
        Check(bits == -1077936128, "SingleToInt32Bits(-1.5f)");
        Check(bits < 0, "float sign bit as int sign");
        Check((long)bits == -1077936128L, "float bits widened");
        Check(BitConverter.Int32BitsToSingle(bits) == -1.5f, "round trip");
        Check(BitConverter.DoubleToInt64Bits(D(-2.0)) == unchecked((long)0xC000000000000000), "DoubleToInt64Bits");
        Check(BitConverter.Int64BitsToDouble(0x3FF0000000000000) == 1.0, "Int64BitsToDouble");
    }

    struct Pair { public double A; public float B; public int C; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static double Sum10(double a, double b, double c, double d, double e, double f, double g, double h, double i, double j)
        => a + b + c + d + e + f + g + h + i + j;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static float Mixed(int a, float b, long c, double d, float e) => (float)(a + b + c + d + e);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static float SumF11(float a, float b, float c, float d, float e, float f, float g, float h, float i, float j, float k)
        => a + b + c + d + e + f + g + h + i + j + k;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static double Mixed12(float a, double b, int c, float d, double e, long f, float g, double h, float i, double j, float k, double l)
        => a + b + c + d + e + f + g + h + i + j + k + l;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Pair MakePair(double a, float b, int c) => new Pair { A = a * 2, B = b * 2, C = c * 2 };

    static void Calls()
    {
        Check(Sum10(1, 2, 3, 4, 5, 6, 7, 8, 9, 10) == 55.0, "10 double args (registers and stack)");
        Check(Mixed(1, 2.5f, 3, 4.25, 5.5f) == 16.25f, "mixed int/float args");
        Check(SumF11(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11) == 66.0f, "11 float args (registers and stack)");
        Check(Mixed12(0.5f, 1.5, 2, 3.5f, 4.5, 5, 6.5f, 7.5, 8.5f, 9.5, 10.5f, 11.5) == 71.0, "12 mixed args on registers and stack");
        Pair p = MakePair(1.5, 2.5f, 3);
        Check(p.A == 3.0 && p.B == 5.0f && p.C == 6, "struct with FP fields");
        double[] arr = { 1.5, 2.5, 3.5 };
        double s = 0;
        foreach (double v in arr) s += v;
        Check(s == 7.5, "array of doubles");
        float[] farr = { 1.5f, -2.5f };
        Check(farr[0] + farr[1] == -1.0f, "array of floats");
    }

    static void Intrinsics()
    {
        CheckBits(Math.Sqrt(D(2.0)), 0x3FF6A09E667F3BCD, "Sqrt(2)");
        Check(double.IsNaN(Math.Sqrt(D(-1.0))), "Sqrt(-1)");
        CheckBits(Math.Abs(D(-0.0)), 0x0000000000000000, "Abs(-0)");
        Check(Math.Abs(D(-3.5)) == 3.5 && MathF.Abs(F(-3.5f)) == 3.5f, "Abs");
        Check(double.IsNaN(Math.Max(D(double.NaN), D(1.0))) && double.IsNaN(Math.Min(D(1.0), D(double.NaN))), "Max/Min NaN");
        Check(Math.Max(D(1.0), D(2.0)) == 2.0 && Math.Min(D(1.0), D(2.0)) == 1.0, "Max/Min");
        Check(Math.Max(I(3), I(4)) == 4 && Math.Min(L(-1), L(1)) == -1, "integer Max/Min unaffected");
        Check(Math.Floor(D(-1.5)) == -2.0 && Math.Ceiling(D(1.2)) == 2.0 && Math.Round(D(2.5)) == 2.0, "Floor/Ceiling/Round");
        Check(Math.Truncate(D(-1.7)) == -1.0, "Truncate");
    }

    static bool Throws(Func<object> f)
    {
        try { _ = f(); } catch (OverflowException) { return true; }
        return false;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        Arithmetic();
        Negation();
        Comparisons();
        ToInteger();
        FromInteger();
        FloatDouble();
        Bits();
        Calls();
        Intrinsics();
        if (s_failures != 0)
        {
            Console.WriteLine(s_failures + " failure(s)");
            return 1;
        }
        return 100;
    }
}
