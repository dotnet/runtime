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
            Vector<short> vecs  = Vector.Create<short>(2);
            Vector<int>   veci  = Vector.Create<int>(3);
            Vector<uint>  vecui = Vector.Create<uint>(5);
            Vector<long>  vecl  = Vector.Create<long>(7);

            ZipLow();
            ZipHigh(vecui);
            UnzipOdd();
            UnzipEven();
            TransposeOdd();
            TransposeEven(vecl);
            ReverseElement();
            And();
            BitwiseClear();
            Xor();
            Or();
            ConditionalSelect(veci);

            ZipLowMask();
            ZipHighMask(vecui);
            UnzipOddMask();
            UnzipEvenMask();
            TransposeOddMask();
            TransposeEvenMask(vecl);
            ReverseElementMask();
            AndMask();
            BitwiseClearMask();
            XorMask();
            OrMask();
            ConditionalSelectMask(veci);

            UnzipEvenZipLowMask();
            TransposeEvenAndMask(vecs);

        }
    }

    // These should not use the predicate variants

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<short> ZipLow()
    {
        //ARM64-FULL-LINE: zip1 {{z[0-9]+}}.h, {{z[0-9]+}}.h, {{z[0-9]+}}.h
        return Sve.ZipLow(Vector<short>.Zero, Sve.CreateTrueMaskInt16());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<uint> ZipHigh(Vector<uint> v)
    {
        //ARM64-FULL-LINE: zip2 {{z[0-9]+}}.s, {{z[0-9]+}}.s, {{z[0-9]+}}.s
        return Sve.ZipHigh(Sve.CreateTrueMaskUInt32(), v);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<sbyte> UnzipEven()
    {
        //ARM64-FULL-LINE: uzp1 {{z[0-9]+}}.b, {{z[0-9]+}}.b, {{z[0-9]+}}.b
        return Sve.UnzipEven(Sve.CreateTrueMaskSByte(), Vector<sbyte>.Zero);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<short> UnzipOdd()
    {
        //ARM64-FULL-LINE: uzp2 {{z[0-9]+}}.h, {{z[0-9]+}}.h, {{z[0-9]+}}.h
        return Sve.UnzipOdd(Sve.CreateTrueMaskInt16(), Sve.CreateFalseMaskInt16());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<long> TransposeEven(Vector<long> v)
    {
        //ARM64-FULL-LINE: trn1 {{z[0-9]+}}.d, {{z[0-9]+}}.d, {{z[0-9]+}}.d
        return Sve.TransposeEven(v, Sve.CreateTrueMaskInt64());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<short> TransposeOdd()
    {
        //ARM64-FULL-LINE: trn2 {{z[0-9]+}}.h, {{z[0-9]+}}.h, {{z[0-9]+}}.h
        return Sve.TransposeOdd(Vector<short>.Zero, Sve.CreateTrueMaskInt16());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<short> ReverseElement()
    {
        //ARM64-FULL-LINE: rev {{z[0-9]+}}.h, {{z[0-9]+}}.h
        return Sve.ReverseElement(Sve.CreateTrueMaskInt16());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<short> And()
    {
        //ARM64-FULL-LINE: and {{z[0-9]+}}.h, {{z[0-9]+}}.h, {{z[0-9]+}}.h
        return Sve.ConditionalSelect(
            Sve.CreateTrueMaskInt16(),
            Sve.And(Sve.CreateTrueMaskInt16(), Sve.CreateTrueMaskInt16()),
            Vector<short>.Zero
        );
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<short> BitwiseClear()
    {
        //ARM64-FULL-LINE: bic {{z[0-9]+}}.h, {{p[0-9]+}}/m, {{z[0-9]+}}.h, {{z[0-9]+}}.h
        return Sve.ConditionalSelect(
            Sve.CreateTrueMaskInt16(SveMaskPattern.VectorCount1),
            Sve.BitwiseClear(Sve.CreateTrueMaskInt16(), Sve.CreateTrueMaskInt16()),
            Vector<short>.Zero
        );
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<int> Xor()
    {
        //ARM64-FULL-LINE: eor {{z[0-9]+}}.s, {{p[0-9]+}}/m, {{z[0-9]+}}.s, {{z[0-9]+}}.s
        return Sve.ConditionalSelect(
            Sve.CreateTrueMaskInt32(SveMaskPattern.VectorCount3),
            Sve.Xor(Sve.CreateTrueMaskInt32(), Sve.CreateTrueMaskInt32()),
            Vector<int>.Zero
        );
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<short> Or()
    {
        //ARM64-FULL-LINE: orr {{z[0-9]+}}.h, {{p[0-9]+}}/m, {{z[0-9]+}}.h, {{z[0-9]+}}.h
        return Sve.ConditionalSelect(
            Sve.CreateTrueMaskInt16(SveMaskPattern.VectorCount1),
            Sve.Or(Sve.CreateTrueMaskInt16(), Sve.CreateTrueMaskInt16()),
            Vector<short>.Zero
        );
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<int> ConditionalSelect(Vector<int> v)
    {
        // Use a passed in vector for the mask to prevent optimising away the select
        //ARM64-FULL-LINE: sel {{z[0-9]+}}.s, {{p[0-9]+}}, {{z[0-9]+}}.s, {{z[0-9]+}}.s
        return Sve.ConditionalSelect(
            v,
            Sve.CreateFalseMaskInt32(),
            Sve.CreateTrueMaskInt32()
        );
    }


    // These should use the predicate variants.
    // CreateBreakAfterMask is used as the first argument is a predicate.


    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<short> ZipLowMask()
    {
        //ARM64-FULL-LINE: zip1 {{p[0-9]+}}.h, {{p[0-9]+}}.h, {{p[0-9]+}}.h
        return Sve.CreateBreakAfterMask(Sve.ZipLow(Vector<short>.Zero, Sve.CreateTrueMaskInt16()), Sve.CreateTrueMaskInt16());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<uint> ZipHighMask(Vector<uint> v)
    {
        //ARM64-FULL-LINE: zip2 {{p[0-9]+}}.s, {{p[0-9]+}}.s, {{p[0-9]+}}.s
        return Sve.CreateBreakAfterMask(Sve.ZipHigh(Sve.CreateTrueMaskUInt32(), v), Sve.CreateTrueMaskUInt32());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<sbyte> UnzipEvenMask()
    {
        //ARM64-FULL-LINE: uzp1 {{p[0-9]+}}.b, {{p[0-9]+}}.b, {{p[0-9]+}}.b
        return Sve.CreateBreakAfterMask(Sve.UnzipEven(Sve.CreateTrueMaskSByte(), Vector<sbyte>.Zero), Sve.CreateTrueMaskSByte());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<short> UnzipOddMask()
    {
        //ARM64-FULL-LINE: uzp2 {{p[0-9]+}}.h, {{p[0-9]+}}.h, {{p[0-9]+}}.h
        return Sve.CreateBreakAfterMask(Sve.UnzipOdd(Sve.CreateTrueMaskInt16(), Sve.CreateFalseMaskInt16()), Sve.CreateTrueMaskInt16());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<long> TransposeEvenMask(Vector<long> v)
    {
        //ARM64-FULL-LINE: trn1 {{p[0-9]+}}.d, {{p[0-9]+}}.d, {{p[0-9]+}}.d
        return Sve.CreateBreakAfterMask(Sve.TransposeEven(v, Sve.CreateTrueMaskInt64()), Sve.CreateFalseMaskInt64());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<short> TransposeOddMask()
    {
        //ARM64-FULL-LINE: trn2 {{p[0-9]+}}.h, {{p[0-9]+}}.h, {{p[0-9]+}}.h
        return Sve.CreateBreakAfterMask(Sve.TransposeOdd(Vector<short>.Zero, Sve.CreateTrueMaskInt16()), Sve.CreateTrueMaskInt16());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<short> ReverseElementMask()
    {
        //ARM64-FULL-LINE: rev {{p[0-9]+}}.h, {{p[0-9]+}}.h
        return Sve.CreateBreakAfterMask(Sve.ReverseElement(Sve.CreateTrueMaskInt16()), Sve.CreateFalseMaskInt16());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<short> AndMask()
    {
        //ARM64-FULL-LINE: and {{p[0-9]+}}.b, {{p[0-9]+}}/z, {{p[0-9]+}}.b, {{p[0-9]+}}.b
        return Sve.CreateBreakAfterMask(Sve.ConditionalSelect(
            Sve.CreateTrueMaskInt16(),
            Sve.And(Sve.CreateTrueMaskInt16(), Sve.CreateTrueMaskInt16()),
            Vector<short>.Zero
        ), Sve.CreateFalseMaskInt16());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<short> BitwiseClearMask()
    {
        //ARM64-FULL-LINE: bic {{p[0-9]+}}.b, {{p[0-9]+}}/z, {{p[0-9]+}}.b, {{p[0-9]+}}.b
        return Sve.CreateBreakAfterMask(Sve.ConditionalSelect(
            Sve.CreateTrueMaskInt16(),
            Sve.BitwiseClear(Sve.CreateTrueMaskInt16(), Sve.CreateTrueMaskInt16()),
            Vector<short>.Zero
        ), Sve.CreateFalseMaskInt16());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<int> XorMask()
    {
        //ARM64-FULL-LINE: eor {{p[0-9]+}}.b, {{p[0-9]+}}/z, {{p[0-9]+}}.b, {{p[0-9]+}}.b
        return Sve.CreateBreakAfterMask(Sve.ConditionalSelect(
            Sve.CreateTrueMaskInt32(),
            Sve.Xor(Sve.CreateTrueMaskInt32(), Sve.CreateTrueMaskInt32()),
            Vector<int>.Zero
        ), Sve.CreateFalseMaskInt32());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<short> OrMask()
    {
        //ARM64-FULL-LINE: orr {{p[0-9]+}}.b, {{p[0-9]+}}/z, {{p[0-9]+}}.b, {{p[0-9]+}}.b
        return Sve.CreateBreakAfterMask(Sve.ConditionalSelect(
            Sve.CreateTrueMaskInt16(),
            Sve.Or(Sve.CreateTrueMaskInt16(), Sve.CreateTrueMaskInt16()),
            Vector<short>.Zero
        ), Sve.CreateFalseMaskInt16());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<int> ConditionalSelectMask(Vector<int> v)
    {
        // Use a passed in vector for the mask to prevent optimising away the select
        //ARM64-FULL-LINE: sel {{p[0-9]+}}.b, {{p[0-9]+}}, {{p[0-9]+}}.b, {{p[0-9]+}}.b
        return Sve.CreateBreakAfterMask(Sve.ConditionalSelect(
            v,
            Sve.CreateFalseMaskInt32(),
            Sve.CreateTrueMaskInt32()
        ), Sve.CreateFalseMaskInt32());
    }

    // These have multiple uses of the predicate variants

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<short> UnzipEvenZipLowMask()
    {
        //ARM64-FULL-LINE: zip1 {{p[0-9]+}}.h, {{p[0-9]+}}.h, {{p[0-9]+}}.h
        //ARM64-FULL-LINE: uzp1 {{p[0-9]+}}.h, {{p[0-9]+}}.h, {{p[0-9]+}}.h
        return Sve.CreateBreakAfterMask(Sve.UnzipEven(Sve.ZipLow(Vector<short>.Zero, Sve.CreateTrueMaskInt16()), Vector<short>.Zero), Sve.CreateTrueMaskInt16());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<short> TransposeEvenAndMask(Vector<short> v)
    {
        //ARM64-FULL-LINE: and {{p[0-9]+}}.b, {{p[0-9]+}}/z, {{p[0-9]+}}.b, {{p[0-9]+}}.b
        //ARM64-FULL-LINE: trn1 {{p[0-9]+}}.h, {{p[0-9]+}}.h, {{p[0-9]+}}.h
        return Sve.CreateBreakAfterMask(Sve.TransposeEven(Sve.CreateTrueMaskInt16(), Sve.ConditionalSelect(
            Sve.CreateTrueMaskInt16(),
            Sve.And(v, Sve.CreateTrueMaskInt16()),
            Vector<short>.Zero
        )), Sve.CreateFalseMaskInt16());

    }
}
