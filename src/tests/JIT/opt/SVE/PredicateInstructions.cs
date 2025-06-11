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
            ZipLow();
            ZipHigh();
            UnzipOdd();
            UnzipEven();
            TransposeOdd();
            TransposeEven();
            ReverseElement();
            And();
            BitwiseClear();
            Xor();
            Or();
            ConditionalSelect();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<short> ZipLow()
    {
        //ARM64-FULL-LINE: zip1 {{p[0-9]+}}.h, {{p[0-9]+}}.h, {{p[0-9]+}}.h
        return Sve.ZipLow(Vector<short>.Zero, Sve.CreateTrueMaskInt16());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<uint> ZipHigh()
    {
        //ARM64-FULL-LINE: zip2 {{p[0-9]+}}.s, {{p[0-9]+}}.s, {{p[0-9]+}}.s
        return Sve.ZipHigh(Sve.CreateTrueMaskUInt32(), Sve.CreateTrueMaskUInt32());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<sbyte> UnzipEven()
    {
        //ARM64-FULL-LINE: uzp1 {{p[0-9]+}}.b, {{p[0-9]+}}.b, {{p[0-9]+}}.b
        return Sve.UnzipEven(Sve.CreateTrueMaskSByte(), Vector<sbyte>.Zero);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<short> UnzipOdd()
    {
        //ARM64-FULL-LINE: uzp2 {{p[0-9]+}}.h, {{p[0-9]+}}.h, {{p[0-9]+}}.h
        return Sve.UnzipOdd(Sve.CreateTrueMaskInt16(), Sve.CreateFalseMaskInt16());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<long> TransposeEven()
    {
        //ARM64-FULL-LINE: trn1 {{p[0-9]+}}.d, {{p[0-9]+}}.d, {{p[0-9]+}}.d
        return Sve.TransposeEven(Sve.CreateFalseMaskInt64(), Sve.CreateTrueMaskInt64());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<short> TransposeOdd()
    {
        //ARM64-FULL-LINE: trn2 {{p[0-9]+}}.h, {{p[0-9]+}}.h, {{p[0-9]+}}.h
        return Sve.TransposeOdd(Vector<short>.Zero, Sve.CreateTrueMaskInt16());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<short> ReverseElement()
    {
        //ARM64-FULL-LINE: rev {{p[0-9]+}}.h, {{p[0-9]+}}.h
        return Sve.ReverseElement(Sve.CreateTrueMaskInt16());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<short> And()
    {
        //ARM64-FULL-LINE: and {{p[0-9]+}}.b, {{p[0-9]+}}/z, {{p[0-9]+}}.b, {{p[0-9]+}}.b
        return Sve.ConditionalSelect(
            Sve.CreateTrueMaskInt16(),
            Sve.And(Sve.CreateTrueMaskInt16(), Sve.CreateTrueMaskInt16()),
            Vector<short>.Zero
        );
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<short> BitwiseClear()
    {
        //ARM64-FULL-LINE: bic {{p[0-9]+}}.b, {{p[0-9]+}}/z, {{p[0-9]+}}.b, {{p[0-9]+}}.b
        return Sve.ConditionalSelect(
            Sve.CreateFalseMaskInt16(),
            Sve.BitwiseClear(Sve.CreateTrueMaskInt16(), Sve.CreateTrueMaskInt16()),
            Vector<short>.Zero
        );
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<int> Xor()
    {
        //ARM64-FULL-LINE: eor {{p[0-9]+}}.b, {{p[0-9]+}}/z, {{p[0-9]+}}.b, {{p[0-9]+}}.b
        return Sve.ConditionalSelect(
            Sve.CreateTrueMaskInt32(),
            Sve.Xor(Sve.CreateTrueMaskInt32(), Sve.CreateTrueMaskInt32()),
            Vector<int>.Zero
        );
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<short> Or()
    {
        //ARM64-FULL-LINE: orr {{p[0-9]+}}.b, {{p[0-9]+}}/z, {{p[0-9]+}}.b, {{p[0-9]+}}.b
        return Sve.ConditionalSelect(
            Sve.CreateTrueMaskInt16(),
            Sve.Or(Sve.CreateTrueMaskInt16(), Sve.CreateTrueMaskInt16()),
            Vector<short>.Zero
        );
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<int> ConditionalSelect()
    {
        //ARM64-FULL-LINE: sel {{p[0-9]+}}.b, {{p[0-9]+}}, {{p[0-9]+}}.b, {{p[0-9]+}}.b
        return Sve.ConditionalSelect(
            Vector<int>.Zero,
            Sve.CreateFalseMaskInt32(),
            Sve.CreateTrueMaskInt32()
        );
    }
}