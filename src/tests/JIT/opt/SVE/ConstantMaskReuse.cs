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
        Vector<ulong> mask1 = Vector128.CreateScalar(0x1UL).AsVector();
        Vector<ulong> mask2 = Vector128.CreateScalar(0x2UL).AsVector();

        Consume(PTrueMultipleCreateTrueMask(a, b, c));
        Consume(PTrueMultipleEmbeddedMask(a, b, c));
        Consume(PTrueDifferentPatterns(a, b, c));
        Consume(PTrueDifferentElementSizes(a));
        Consume(PTrueMultipleConversionTrueMask(mask1, mask2));
        Consume(PTrueSeparatedByCall(a, b));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector<int> Identity(Vector<int> value) => value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector<int> PTrueMultipleCreateTrueMask(Vector<int> a, Vector<int> b, Vector<int> c)
    {
        //ARM64: ptrue {{p[0-9]+}}.s
        //ARM64-NOT: ptrue
        Vector<int> mask1 = Sve.CreateBreakAfterMask(Sve.CompareGreaterThan(a, b), Sve.CreateTrueMaskInt32());
        Vector<int> mask2 = Sve.CreateBreakAfterMask(Sve.CompareLessThan(a, b), Sve.CreateTrueMaskInt32());
        Vector<int> result1 = Sve.ConditionalSelect(mask1, a, b);
        Vector<int> result2 = Sve.ConditionalSelect(mask2, b, c);
        return Sve.Add(result1, result2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector<int> PTrueMultipleEmbeddedMask(Vector<int> a, Vector<int> b, Vector<int> c)
    {
        //ARM64: ptrue {{p[0-9]+}}.s
        //ARM64-NOT: ptrue
        Vector<int> result1 = Sve.AbsoluteDifference(a, b);
        Vector<int> result2 = Sve.Abs(c);
        return Sve.Add(result1, result2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector<int> PTrueDifferentPatterns(Vector<int> a, Vector<int> b, Vector<int> c)
    {
        //ARM64: ptrue {{p[0-9]+}}.s
        //ARM64: ptrue {{p[0-9]+}}.b, vl1
        //ARM64: ptrue {{p[0-9]+}}.s, vl2
        //ARM64-NOT: ptrue
        Vector<int> mask1 = Sve.CreateBreakAfterMask(Sve.CompareGreaterThan(a, b), Sve.CreateTrueMaskInt32(SveMaskPattern.VectorCount1));
        Vector<int> mask2 = Sve.CreateBreakAfterMask(Sve.CompareLessThan(a, b), Sve.CreateTrueMaskInt32(SveMaskPattern.VectorCount2));
        Vector<int> result1 = Sve.ConditionalSelect(mask1, a, b);
        Vector<int> result2 = Sve.ConditionalSelect(mask2, b, c);
        return Sve.Add(result1, result2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector<int> PTrueDifferentElementSizes(Vector<int> value)
    {
        //ARM64: ptrue {{p[0-9]+}}.s
        //ARM64: ptrue {{p[0-9]+}}.h
        //ARM64-NOT: ptrue
        Vector<int> result1 = Sve.Abs(value);
        Vector<short> result2 = Sve.Negate((Vector<short>)value);
        return Sve.Add(result1, (Vector<int>)result2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong PTrueMultipleConversionTrueMask(Vector<ulong> mask1, Vector<ulong> mask2)
    {
        //ARM64: ptrue {{p[0-9]+}}.d
        //ARM64-NOT: ptrue
        ulong result1 = Sve.SaturatingDecrementByActiveElementCount(0UL, mask1);
        ulong result2 = Sve.SaturatingDecrementByActiveElementCount(0UL, mask2);
        return result1 + result2;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector<int> PTrueSeparatedByCall(Vector<int> a, Vector<int> b)
    {
        //ARM64: ptrue {{p[0-9]+}}.s
        //ARM64-NOT: ptrue
        //ARM64: ptrue {{p[0-9]+}}.s
        //ARM64-NOT: ptrue
        Vector<int> result1 = Sve.Abs(a);
        Consume(result1);
        Vector<int> result2 = Sve.Negate(Identity(b));
        Consume(result2);
        return result1;
    }
}
