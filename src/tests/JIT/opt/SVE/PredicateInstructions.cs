// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using Xunit;

public class PredicateInstructions
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    [Fact]
    public static void TestPredicateInstructions()
    {
        if (Sve.IsSupported)
        {
            Vector<sbyte>  vecsb = Vector.Create<sbyte>(2);
            Vector<short>  vecs  = Vector.Create<short>(2);
            Vector<ushort> vecus = Vector.Create<ushort>(2);
            Vector<int>    veci  = Vector.Create<int>(3);
            Vector<uint>   vecui = Vector.Create<uint>(5);
            Vector<long>   vecl  = Vector.Create<long>(7);

            ZipLowMask(vecs, vecs);
            ZipHighMask(vecui, vecui);
            UnzipOddMask(vecs, vecs);
            UnzipEvenMask(vecsb, vecsb);
            TransposeEvenMask(vecl, vecl);
            TransposeOddMask(vecs, vecs);
            ReverseElementMask(vecs, vecs);
            AndMask(vecs, vecs);
            BitwiseClearMask(vecs, vecs);
            XorMask(veci, veci);
            OrMask(vecs, vecs);
            ConditionalSelectMask(veci, veci, veci);

            UnzipEvenZipLowMask(vecs, vecs);
            TransposeEvenAndMask(vecs, vecs, vecs);

        }
    }

    // These should use the predicate variants.
    // Sve intrinsics that return masks (Compare) or use mask arguments (CreateBreakAfterMask) are used
    // to ensure masks are used.


    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<short> ZipLowMask(Vector<short> a, Vector<short> b)
    {
        //ARM64-FULL-LINE: zip1 {{p[0-9]+}}.h, {{p[0-9]+}}.h, {{p[0-9]+}}.h
        return Sve.ZipLow(Sve.CompareGreaterThan(a, b), Sve.CompareEqual(a, b));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<uint> ZipHighMask(Vector<uint> a, Vector<uint> b)
    {
        //ARM64-FULL-LINE: zip2 {{p[0-9]+}}.s, {{p[0-9]+}}.s, {{p[0-9]+}}.s
        return Sve.CreateBreakAfterMask(Sve.ZipHigh(Sve.CompareGreaterThan(a, b), Sve.CompareEqual(a, b)), Sve.CreateTrueMaskUInt32());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<sbyte> UnzipEvenMask(Vector<sbyte> a, Vector<sbyte> b)
    {
        //ARM64-FULL-LINE: uzp1 {{p[0-9]+}}.b, {{p[0-9]+}}.b, {{p[0-9]+}}.b
        return Sve.UnzipEven(Sve.CompareGreaterThan(a, b), Sve.CompareEqual(a, b));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<short> UnzipOddMask(Vector<short> a, Vector<short> b)
    {
        //ARM64-FULL-LINE: uzp2 {{p[0-9]+}}.h, {{p[0-9]+}}.h, {{p[0-9]+}}.h
        return Sve.CreateBreakAfterMask(Sve.UnzipOdd(Sve.CompareGreaterThan(a, b), Sve.CompareEqual(a, b)), Sve.CreateTrueMaskInt16());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<long> TransposeEvenMask(Vector<long> a, Vector<long> b)
    {
        //ARM64-FULL-LINE: trn1 {{p[0-9]+}}.d, {{p[0-9]+}}.d, {{p[0-9]+}}.d
        return Sve.CreateBreakAfterMask(Sve.TransposeEven(Sve.CompareGreaterThan(a, b), Sve.CompareEqual(a, b)), Sve.CreateFalseMaskInt64());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<short> TransposeOddMask(Vector<short> a, Vector<short> b)
    {
        //ARM64-FULL-LINE: trn2 {{p[0-9]+}}.h, {{p[0-9]+}}.h, {{p[0-9]+}}.h
        return Sve.TransposeOdd(Sve.CompareGreaterThan(a, b), Sve.CompareEqual(a, b));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<short> ReverseElementMask(Vector<short> a, Vector<short> b)
    {
        //ARM64-FULL-LINE: rev {{p[0-9]+}}.h, {{p[0-9]+}}.h
        return Sve.CreateBreakAfterMask(Sve.ReverseElement(Sve.CompareGreaterThan(a, b)), Sve.CreateFalseMaskInt16());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<short> AndMask(Vector<short> a, Vector<short> b)
    {
        //ARM64-FULL-LINE: and {{p[0-9]+}}.b, {{p[0-9]+}}/z, {{p[0-9]+}}.b, {{p[0-9]+}}.b
        return Sve.CreateBreakAfterMask(
            Sve.ConditionalSelect(
                Sve.CreateTrueMaskInt16(),
                Sve.And(Sve.CompareGreaterThan(a, b), Sve.CompareEqual(a, b)),
                Vector<short>.Zero),
            Sve.CreateFalseMaskInt16());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<short> BitwiseClearMask(Vector<short> a, Vector<short> b)
    {
        //ARM64-FULL-LINE: bic {{p[0-9]+}}.b, {{p[0-9]+}}/z, {{p[0-9]+}}.b, {{p[0-9]+}}.b
        return Sve.ConditionalSelect(
                Sve.CreateTrueMaskInt16(),
                Sve.BitwiseClear(Sve.CompareGreaterThan(a, b), Sve.CompareEqual(a, b)),
                Vector<short>.Zero);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<int> XorMask(Vector<int> a, Vector<int> b)
    {
        //ARM64-FULL-LINE: eor {{p[0-9]+}}.b, {{p[0-9]+}}/z, {{p[0-9]+}}.b, {{p[0-9]+}}.b
        return Sve.CreateBreakAfterMask(
            Sve.ConditionalSelect(
                Sve.CreateTrueMaskInt32(),
                Sve.Xor(Sve.CompareGreaterThan(a, b), Sve.CompareEqual(a, b)),
                Vector<int>.Zero),
            Sve.CreateFalseMaskInt32());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<short> OrMask(Vector<short> a, Vector<short> b)
    {
        //ARM64-FULL-LINE: orr {{p[0-9]+}}.b, {{p[0-9]+}}/z, {{p[0-9]+}}.b, {{p[0-9]+}}.b
        return Sve.ConditionalSelect(
                Sve.CreateTrueMaskInt16(),
                Sve.Or(Sve.CompareGreaterThan(a, b), Sve.CompareEqual(a, b)),
                Vector<short>.Zero);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<int> ConditionalSelectMask(Vector<int> v, Vector<int> a, Vector<int> b)
    {
        // Use a passed in vector for the mask to prevent optimising away the select
        //ARM64-FULL-LINE: sel {{p[0-9]+}}.b, {{p[0-9]+}}, {{p[0-9]+}}.b, {{p[0-9]+}}.b
        return Sve.CreateBreakAfterMask(
            Sve.ConditionalSelect(v, Sve.CompareGreaterThan(a, b), Sve.CompareEqual(a, b)),
            Sve.CreateFalseMaskInt32());
    }

    // These have multiple uses of the predicate variants

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<short> UnzipEvenZipLowMask(Vector<short> a, Vector<short> b)
    {
        //ARM64-FULL-LINE: zip1 {{p[0-9]+}}.h, {{p[0-9]+}}.h, {{p[0-9]+}}.h
        //ARM64-FULL-LINE: uzp1 {{p[0-9]+}}.h, {{p[0-9]+}}.h, {{p[0-9]+}}.h
        return Sve.CreateBreakAfterMask(
            Sve.UnzipEven(
                Sve.ZipLow(Sve.CompareGreaterThan(a, b), Sve.CompareEqual(a, b)),
                Sve.CompareLessThan(a, b)),
            Sve.CreateTrueMaskInt16());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<short> TransposeEvenAndMask(Vector<short> v, Vector<short> a, Vector<short> b)
    {
        //ARM64-FULL-LINE: and {{p[0-9]+}}.b, {{p[0-9]+}}/z, {{p[0-9]+}}.b, {{p[0-9]+}}.b
        //ARM64-FULL-LINE: trn1 {{p[0-9]+}}.h, {{p[0-9]+}}.h, {{p[0-9]+}}.h
        return Sve.TransposeEven(
                Sve.CompareGreaterThan(a, b),
                Sve.ConditionalSelect(
                    Sve.CreateTrueMaskInt16(),
                    Sve.And(Sve.CompareGreaterThan(a, b), Sve.CompareEqual(a, b)),
                    Sve.CompareLessThan(a, b)));
    }
}
