// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using Xunit;

public class ConstantMaskReuse
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Consume<T>(T value)
    {
    }

    [Fact]
    public static void TestEntryPoint()
    {
        if (!Sve.IsSupported)
        {
            return;
        }

        Vector<int> a = Vector.Create(11);
        Vector<int> b = Vector.Create(22);
        Vector<int> c = Vector.Create(33);
        int[] values = new int[32];
        Vector<ulong> mask1 = Vector128.CreateScalar(0x1UL).AsVector();
        Vector<ulong> mask2 = Vector128.CreateScalar(0x2UL).AsVector();

        Consume(PTrueSingleCompareMask(a, b));
        Consume(PTrueSingleCreateTrueMask(a, b));
        Consume(PTrueSingleCreateTrueMaskPattern(a, b));
        Consume(PFalseSingleCreateFalseMask(a, b));
        Consume(PTrueSingleAllBitsMask(a, b));
        Consume(PTrueSingleEmbeddedMask(a, b));
        Consume(PTrueSingleConversionTrueMask(mask1));

        Consume(PTrueMultipleCompareMask(a, b));
        Consume(PTrueMultipleCreateTrueMask(a, b, c));
        Consume(PTrueMultipleCreateTrueMaskPattern(a, b, c));
        Consume(PFalseMultipleCreateFalseMask(a, b, c));
        Consume(PFalseMultipleLoadMasks(values));
        Consume(PTrueMultipleAllBitsMask(a, b, c));
        Consume(PTrueMultipleEmbeddedMask(a, b, c));
        Consume(PTrueMultipleMixedSources(a, b, c));
        Consume(PTrueMultipleConversionTrueMask(mask1, mask2));

        if (Sve2.IsSupported)
        {
            Vector<double> d = Vector.Create(42.0);
            Vector<byte> e = Vector.Create((byte)42);

            Consume(PTrueMultipleSve2Log2Negate(d));
            Consume(PTrueMultipleSve2ZeroExtend8(e));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector<int> PTrueSingleCompareMask(Vector<int> a, Vector<int> b)
    {
        //ARM64-FULL-LINE: ptrue {{p[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: cmpgt {{p[0-9]+}}.s, {{p[0-9]+}}/z, {{z[0-9]+}}.s, {{z[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: mov {{z[0-9]+}}.s, {{p[0-9]+}}/z, #1
        return Sve.CompareGreaterThan(a, b);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector<int> PTrueSingleCreateTrueMask(Vector<int> a, Vector<int> b)
    {
        //ARM64-FULL-LINE: ptrue {{p[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: cmpgt {{p[0-9]+}}.s, {{p[0-9]+}}/z, {{z[0-9]+}}.s, {{z[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: brka {{p[0-9]+}}.b, {{p[0-9]+}}/z, {{p[0-9]+}}.b
        //ARM64-FULL-LINE-NEXT: mov {{z[0-9]+}}.s, {{p[0-9]+}}/z, #1
        return Sve.CreateBreakAfterMask(Sve.CompareGreaterThan(a, b), Sve.CreateTrueMaskInt32());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector<int> PTrueSingleCreateTrueMaskPattern(Vector<int> a, Vector<int> b)
    {
        //ARM64-FULL-LINE: ptrue {{p[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: cmpgt {{p[0-9]+}}.s, {{p[0-9]+}}/z, {{z[0-9]+}}.s, {{z[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: ptrue {{p[0-9]+}}.b, vl1
        //ARM64-FULL-LINE-NEXT: brka {{p[0-9]+}}.b, {{p[0-9]+}}/z, {{p[0-9]+}}.b
        //ARM64-FULL-LINE-NEXT: mov {{z[0-9]+}}.s, {{p[0-9]+}}/z, #1
        return Sve.CreateBreakAfterMask(Sve.CompareGreaterThan(a, b), Sve.CreateTrueMaskInt32(SveMaskPattern.VectorCount1));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector<int> PFalseSingleCreateFalseMask(Vector<int> a, Vector<int> b)
    {
        //ARM64-FULL-LINE: ptrue {{p[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: cmpgt {{p[0-9]+}}.s, {{p[0-9]+}}/z, {{z[0-9]+}}.s, {{z[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: pfalse {{p[0-9]+}}.b
        //ARM64-FULL-LINE-NEXT: brka {{p[0-9]+}}.b, {{p[0-9]+}}/z, {{p[0-9]+}}.b
        //ARM64-FULL-LINE-NEXT: mov {{z[0-9]+}}.s, {{p[0-9]+}}/z, #1
        return Sve.CreateBreakAfterMask(Sve.CompareGreaterThan(a, b), Sve.CreateFalseMaskInt32());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector<int> PTrueSingleAllBitsMask(Vector<int> a, Vector<int> b)
    {
        //ARM64-FULL-LINE: ptrue {{p[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: movprfx {{z[0-9]+}}.s, {{p[0-9]+}}/z, {{z[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: sabd {{z[0-9]+}}.s, {{p[0-9]+}}/m, {{z[0-9]+}}.s, {{z[0-9]+}}.s
        return Sve.ConditionalSelect(Vector<int>.AllBitsSet, Sve.AbsoluteDifference(a, b), a);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector<int> PTrueSingleEmbeddedMask(Vector<int> a, Vector<int> b)
    {
        //ARM64-FULL-LINE: ptrue {{p[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: movprfx {{z[0-9]+}}.s, {{p[0-9]+}}/z, {{z[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: sabd {{z[0-9]+}}.s, {{p[0-9]+}}/m, {{z[0-9]+}}.s, {{z[0-9]+}}.s
        return Sve.AbsoluteDifference(a, b);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong PTrueSingleConversionTrueMask(Vector<ulong> mask)
    {
        //ARM64-FULL-LINE: ptrue {{p[0-9]+}}.d
        //ARM64-FULL-LINE-NEXT: cmpne {{p[0-9]+}}.d, {{p[0-9]+}}/z, {{z[0-9]+}}.d, #0
        //ARM64-FULL-LINE-NEXT: mov {{x[0-9]+}}, xzr
        //ARM64-FULL-LINE-NEXT: sqdecp {{x[0-9]+}}, {{p[0-9]+}}.d
        return Sve.SaturatingDecrementByActiveElementCount(0UL, mask);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector<int> PTrueMultipleCompareMask(Vector<int> a, Vector<int> b)
    {
        //ARM64-FULL-LINE: ptrue {{p[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: cmpgt {{p[0-9]+}}.s, {{p[0-9]+}}/z, {{z[0-9]+}}.s, {{z[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: sel {{z[0-9]+}}.s, {{p[0-9]+}}, {{z[0-9]+}}.s, {{z[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: cmpgt {{p[0-9]+}}.s, {{p[0-9]+}}/z, {{z[0-9]+}}.s, {{z[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: mov {{z[0-9]+}}.s, {{p[0-9]+}}/m, {{z[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: add {{z[0-9]+}}.s, {{z[0-9]+}}.s, {{z[0-9]+}}.s
        Vector<int> mask1 = Sve.CompareGreaterThan(a, b);
        Vector<int> mask2 = Sve.CompareLessThan(a, b);
        Vector<int> result1 = Sve.ConditionalSelect(mask1, a, b);
        Vector<int> result2 = Sve.ConditionalSelect(mask2, b, a);
        return Sve.Add(result1, result2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector<int> PTrueMultipleCreateTrueMask(Vector<int> a, Vector<int> b, Vector<int> c)
    {
        //ARM64-FULL-LINE: ptrue {{p[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: cmpgt {{p[0-9]+}}.s, {{p[0-9]+}}/z, {{z[0-9]+}}.s, {{z[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: brka {{p[0-9]+}}.b, {{p[0-9]+}}/z, {{p[0-9]+}}.b
        //ARM64-FULL-LINE-NEXT: sel {{z[0-9]+}}.s, {{p[0-9]+}}, {{z[0-9]+}}.s, {{z[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: cmpgt {{p[0-9]+}}.s, {{p[0-9]+}}/z, {{z[0-9]+}}.s, {{z[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: brka {{p[0-9]+}}.b, {{p[0-9]+}}/z, {{p[0-9]+}}.b
        //ARM64-FULL-LINE-NEXT: sel {{z[0-9]+}}.s, {{p[0-9]+}}, {{z[0-9]+}}.s, {{z[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: add {{z[0-9]+}}.s, {{z[0-9]+}}.s, {{z[0-9]+}}.s
        Vector<int> mask1 = Sve.CreateBreakAfterMask(Sve.CompareGreaterThan(a, b), Sve.CreateTrueMaskInt32());
        Vector<int> mask2 = Sve.CreateBreakAfterMask(Sve.CompareLessThan(a, b), Sve.CreateTrueMaskInt32());
        Vector<int> result1 = Sve.ConditionalSelect(mask1, a, b);
        Vector<int> result2 = Sve.ConditionalSelect(mask2, b, c);
        return Sve.Add(result1, result2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector<int> PTrueMultipleCreateTrueMaskPattern(Vector<int> a, Vector<int> b, Vector<int> c)
    {
        //ARM64-FULL-LINE: ptrue {{p[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: cmpgt {{p[0-9]+}}.s, {{p[0-9]+}}/z, {{z[0-9]+}}.s, {{z[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: ptrue {{p[0-9]+}}.b, vl1
        //ARM64-FULL-LINE-NEXT: brka {{p[0-9]+}}.b, {{p[0-9]+}}/z, {{p[0-9]+}}.b
        //ARM64-FULL-LINE-NEXT: sel {{z[0-9]+}}.s, {{p[0-9]+}}, {{z[0-9]+}}.s, {{z[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: cmpgt {{p[0-9]+}}.s, {{p[0-9]+}}/z, {{z[0-9]+}}.s, {{z[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: brka {{p[0-9]+}}.b, {{p[0-9]+}}/z, {{p[0-9]+}}.b
        //ARM64-FULL-LINE-NEXT: sel {{z[0-9]+}}.s, {{p[0-9]+}}, {{z[0-9]+}}.s, {{z[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: add {{z[0-9]+}}.s, {{z[0-9]+}}.s, {{z[0-9]+}}.s
        Vector<int> mask1 = Sve.CreateBreakAfterMask(Sve.CompareGreaterThan(a, b), Sve.CreateTrueMaskInt32(SveMaskPattern.VectorCount1));
        Vector<int> mask2 = Sve.CreateBreakAfterMask(Sve.CompareLessThan(a, b), Sve.CreateTrueMaskInt32(SveMaskPattern.VectorCount1));
        Vector<int> result1 = Sve.ConditionalSelect(mask1, a, b);
        Vector<int> result2 = Sve.ConditionalSelect(mask2, b, c);
        return Sve.Add(result1, result2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector<int> PFalseMultipleCreateFalseMask(Vector<int> a, Vector<int> b, Vector<int> c)
    {
        //ARM64-FULL-LINE: ptrue {{p[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: cmpgt {{p[0-9]+}}.s, {{p[0-9]+}}/z, {{z[0-9]+}}.s, {{z[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: pfalse {{p[0-9]+}}.b
        //ARM64-FULL-LINE-NEXT: brka {{p[0-9]+}}.b, {{p[0-9]+}}/z, {{p[0-9]+}}.b
        //ARM64-FULL-LINE-NEXT: sel {{z[0-9]+}}.s, {{p[0-9]+}}, {{z[0-9]+}}.s, {{z[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: cmpgt {{p[0-9]+}}.s, {{p[0-9]+}}/z, {{z[0-9]+}}.s, {{z[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: brka {{p[0-9]+}}.b, {{p[0-9]+}}/z, {{p[0-9]+}}.b
        //ARM64-FULL-LINE-NEXT: sel {{z[0-9]+}}.s, {{p[0-9]+}}, {{z[0-9]+}}.s, {{z[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: add {{z[0-9]+}}.s, {{z[0-9]+}}.s, {{z[0-9]+}}.s
        Vector<int> mask1 = Sve.CreateBreakAfterMask(Sve.CompareGreaterThan(a, b), Sve.CreateFalseMaskInt32());
        Vector<int> mask2 = Sve.CreateBreakAfterMask(Sve.CompareLessThan(a, b), Sve.CreateFalseMaskInt32());
        Vector<int> result1 = Sve.ConditionalSelect(mask1, a, b);
        Vector<int> result2 = Sve.ConditionalSelect(mask2, b, c);
        return Sve.Add(result1, result2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe Vector<int> PFalseMultipleLoadMasks(int[] values)
    {
        //ARM64-FULL-LINE: pfalse {{p[0-9]+}}.b
        //ARM64-FULL-LINE-NEXT: wrffr {{p[0-9]+}}.b
        //ARM64-FULL-LINE-NEXT: ldnf1w  { {{z[0-9]+}}.s }, {{p[0-9]+}}/z, [{{x[0-9]+}}]
        //ARM64-FULL-LINE-NEXT: add {{x[0-9]+}}, {{x[0-9]+}}, #4
        //ARM64-FULL-LINE-NEXT: ldnf1w  { {{z[0-9]+}}.s }, {{p[0-9]+}}/z, [{{x[0-9]+}}]
        //ARM64-FULL-LINE-NEXT: add {{z[0-9]+}}.s, {{z[0-9]+}}.s, {{z[0-9]+}}.s
        fixed (int* ptr = values)
        {
            Vector<int> result1 = Sve.LoadVectorNonFaulting(Sve.CreateFalseMaskInt32(), ptr);
            Vector<int> result2 = Sve.LoadVectorNonFaulting(Vector<int>.Zero, ptr + 1);
            return Sve.Add(result1, result2);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector<int> PTrueMultipleAllBitsMask(Vector<int> a, Vector<int> b, Vector<int> c)
    {
        //ARM64-FULL-LINE: ptrue {{p[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: movprfx {{z[0-9]+}}.s, {{p[0-9]+}}/z, {{z[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: sabd {{z[0-9]+}}.s, {{p[0-9]+}}/m, {{z[0-9]+}}.s, {{z[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: movprfx {{z[0-9]+}}.s, {{p[0-9]+}}/z, {{z[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: abs {{z[0-9]+}}.s, {{p[0-9]+}}/m, {{z[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: add {{z[0-9]+}}.s, {{z[0-9]+}}.s, {{z[0-9]+}}.s
        Vector<int> result1 = Sve.ConditionalSelect(Vector<int>.AllBitsSet, Sve.AbsoluteDifference(a, b), a);
        Vector<int> result2 = Sve.ConditionalSelect(Vector<int>.AllBitsSet, Sve.Abs(c), c);
        return Sve.Add(result1, result2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector<int> PTrueMultipleEmbeddedMask(Vector<int> a, Vector<int> b, Vector<int> c)
    {
        //ARM64-FULL-LINE: ptrue {{p[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: movprfx {{z[0-9]+}}.s, {{p[0-9]+}}/z, {{z[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: sabd {{z[0-9]+}}.s, {{p[0-9]+}}/m, {{z[0-9]+}}.s, {{z[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: movprfx {{z[0-9]+}}.s, {{p[0-9]+}}/z, {{z[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: abs {{z[0-9]+}}.s, {{p[0-9]+}}/m, {{z[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: add {{z[0-9]+}}.s, {{z[0-9]+}}.s, {{z[0-9]+}}.s
        Vector<int> result1 = Sve.AbsoluteDifference(a, b);
        Vector<int> result2 = Sve.Abs(c);
        return Sve.Add(result1, result2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector<int> PTrueMultipleMixedSources(Vector<int> a, Vector<int> b, Vector<int> c)
    {
        //ARM64-FULL-LINE: ptrue {{p[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: cmpgt {{p[0-9]+}}.s, {{p[0-9]+}}/z, {{z[0-9]+}}.s, {{z[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: brka {{p[0-9]+}}.b, {{p[0-9]+}}/z, {{p[0-9]+}}.b
        //ARM64-FULL-LINE-NEXT: sabd {{z[0-9]+}}.s, {{p[0-9]+}}/m, {{z[0-9]+}}.s, {{z[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: movprfx {{z[0-9]+}}.s, {{p[0-9]+}}/z, {{z[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: abs {{z[0-9]+}}.s, {{p[0-9]+}}/m, {{z[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: add {{z[0-9]+}}.s, {{z[0-9]+}}.s, {{z[0-9]+}}.s
        Vector<int> mask = Sve.CreateBreakAfterMask(Sve.CompareGreaterThan(a, b), Sve.CreateTrueMaskInt32());
        Vector<int> result1 = Sve.ConditionalSelect(mask, Sve.AbsoluteDifference(a, b), a);
        Vector<int> result2 = Sve.ConditionalSelect(Vector<int>.AllBitsSet, Sve.Abs(c), c);
        return Sve.Add(result1, result2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong PTrueMultipleConversionTrueMask(Vector<ulong> mask1, Vector<ulong> mask2)
    {
        //ARM64-FULL-LINE: ptrue {{p[0-9]+}}.d
        //ARM64-FULL-LINE-NEXT: cmpne {{p[0-9]+}}.d, {{p[0-9]+}}/z, {{z[0-9]+}}.d, #0
        //ARM64-FULL-LINE-NEXT: mov {{x[0-9]+}}, xzr
        //ARM64-FULL-LINE-NEXT: sqdecp {{x[0-9]+}}, {{p[0-9]+}}.d
        //ARM64-FULL-LINE-NEXT: cmpne {{p[0-9]+}}.d, {{p[0-9]+}}/z, {{z[0-9]+}}.d, #0
        //ARM64-FULL-LINE-NEXT: mov {{x[0-9]+}}, xzr
        //ARM64-FULL-LINE-NEXT: sqdecp {{x[0-9]+}}, {{p[0-9]+}}.d
        //ARM64-FULL-LINE-NEXT: add {{x[0-9]+}}, {{x[0-9]+}}, {{x[0-9]+}}
        ulong result1 = Sve.SaturatingDecrementByActiveElementCount(0UL, mask1);
        ulong result2 = Sve.SaturatingDecrementByActiveElementCount(0UL, mask2);
        return result1 + result2;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector<double> PTrueMultipleSve2Log2Negate(Vector<double> value)
    {
        //ARM64-FULL-LINE: ptrue {{p[0-9]+}}.d
        //ARM64-FULL-LINE-NEXT: flogb {{z[0-9]+}}.d, {{p[0-9]+}}/m, {{z[0-9]+}}.d
        //ARM64-FULL-LINE-NEXT: neg {{z[0-9]+}}.d, {{p[0-9]+}}/m, {{z[0-9]+}}.d
        Vector<long> exponent = Sve2.Log2(value);
        Vector<long> scale = Sve.Negate(exponent);
        return Sve.Scale(value, scale);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector<ushort> PTrueMultipleSve2ZeroExtend8(Vector<byte> value)
    {
        //ARM64-FULL-LINE: ptrue {{p[0-9]+}}.h
        //ARM64-FULL-LINE-NEXT: uxtb {{z[0-9]+}}.h, {{p[0-9]+}}/m, {{z[0-9]+}}.h
        //ARM64-FULL-LINE-NEXT: uxtb {{z[0-9]+}}.h, {{p[0-9]+}}/m, {{z[0-9]+}}.h
        Vector<ushort> result1 = Sve.ZeroExtend8((Vector<ushort>)value);
        Vector<ushort> result2 = Sve.ZeroExtend8((Vector<ushort>)Sve.Add(value, value));
        return Sve.Add(result1, result2);
    }
}
