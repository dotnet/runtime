// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Unit tests for the masks conversion optimization
// Uses vectors as masks and vice versa.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Threading;
using Xunit;

public class ConstantMasks
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Consume<T>(T value) { }

    [Fact]
    public static void TestEntryPoint()
    {
        if (Sve.IsSupported)
        {
            Vector<int> op1 = Vector.Create<int>(11);
            Vector<int> op2 = Vector.Create<int>(22);
            Vector<int> op3 = Vector.Create<int>(33);
            Vector<long> opl1 = Vector.Create<long>(44);
            Vector<long> opl2 = Vector.Create<long>(55);

            CndSelectEmbedded(op1, op2, op3);
            CndSelectEmbeddedFalseMask(op1, op2);
            CndSelectEmbeddedZero(op1, op2);
            CndSelectEmbeddedTrueMask(op1, op2);
            CndSelectEmbeddedAllBits(op1, op2);

            CndSelectOptionalEmbedded(op1, op2, op3);
            CndSelectOptionalEmbeddedFalseMask(op1, op2);
            CndSelectOptionalEmbeddedZero(op1, op2);
            CndSelectOptionalEmbeddedTrueMask(op1, op2);
            CndSelectOptionalEmbeddedAllBits(op1, op2);

            CndSelectEmbeddedOneOp(op1, op2);
            CndSelectEmbeddedOneOpFalseMask(op1, op2);
            CndSelectEmbeddedOneOpZero(op1, op2);
            CndSelectEmbeddedOneOpTrueMask(op1);
            CndSelectEmbeddedOneOpAllBits(op1);

            CndSelectEmbeddedReduction(opl1, op2, opl2);
            CndSelectEmbeddedReductionFalseMask(op1, opl1);
            CndSelectEmbeddedReductionZero(op1, opl1);
            CndSelectEmbeddedReductionTrueMask(op1, opl1);
            CndSelectEmbeddedReductionAllBits(op1, opl1);
        }
    }

    // SVE operation (with embedded mask) inside a conditional select

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void CndSelectEmbedded(Vector<int> mask, Vector<int> op1, Vector<int> op2) {
        //ARM64-FULL-LINE: sabd {{z[0-9]+}}.s, {{p[0-9]+}}/m, {{z[0-9]+}}.s, {{z[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: movz {{.*}}
        Vector<int> result = Sve.ConditionalSelect(mask, Sve.AbsoluteDifference(op1, op2), op1);
        Consume(result);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void CndSelectEmbeddedFalseMask(Vector<int> op1, Vector<int> op2) {
        //ARM64-FULL-LINE: mov v0.16b, v1.16b
        Vector<int> result = Sve.ConditionalSelect(Sve.CreateFalseMaskInt32(), Sve.AbsoluteDifference(op1, op2), op2);
        Consume(result);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void CndSelectEmbeddedZero(Vector<int> op1, Vector<int> op2) {
        //ARM64-FULL-LINE: mov v0.16b, v1.16b
        Vector<int> result = Sve.ConditionalSelect(Vector<int>.Zero, Sve.AbsoluteDifference(op1, op2), op2);
        Consume(result);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void CndSelectEmbeddedTrueMask(Vector<int> op1, Vector<int> op2) {
        //ARM64-FULL-LINE: ptrue {{p[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: movprfx {{z[0-9]+}}.s, {{p[0-9]+}}/z, {{z[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: sabd {{z[0-9]+}}.s, {{p[0-9]+}}/m, {{z[0-9]+}}.s, {{z[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: movz {{.*}}
        Vector<int> result = Sve.ConditionalSelect(Sve.CreateTrueMaskInt32(), Sve.AbsoluteDifference(op1, op2), op1);
        Consume(result);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void CndSelectEmbeddedAllBits(Vector<int> op1, Vector<int> op2) {
        //ARM64-FULL-LINE: ptrue {{p[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: movprfx {{z[0-9]+}}.s, {{p[0-9]+}}/z, {{z[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: sabd {{z[0-9]+}}.s, {{p[0-9]+}}/m, {{z[0-9]+}}.s, {{z[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: movz {{.*}}
        Vector<int> result = Sve.ConditionalSelect(Vector<int>.AllBitsSet, Sve.AbsoluteDifference(op1, op2), op1);
        Consume(result);
    }


    // SVE operation (with optional embedded mask) inside a conditional select

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void CndSelectOptionalEmbedded(Vector<int> mask, Vector<int> op1, Vector<int> op2) {
        //ARM64-FULL-LINE: add {{z[0-9]+}}.s, {{p[0-9]+}}/m, {{z[0-9]+}}.s, {{z[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: movz {{.*}}
        Vector<int> result = Sve.ConditionalSelect(mask, Sve.Add(op1, op2), op1);
        Consume(result);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void CndSelectOptionalEmbeddedFalseMask(Vector<int> op1, Vector<int> op2) {
        //ARM64-FULL-LINE: mov v0.16b, v1.16b
        Vector<int> result = Sve.ConditionalSelect(Sve.CreateFalseMaskInt32(), Sve.Add(op1, op2), op2);
        Consume(result);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void CndSelectOptionalEmbeddedZero(Vector<int> op1, Vector<int> op2) {
        //ARM64-FULL-LINE: mov v0.16b, v1.16b
        Vector<int> result = Sve.ConditionalSelect(Vector<int>.Zero, Sve.Add(op1, op2), op2);
        Consume(result);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void CndSelectOptionalEmbeddedTrueMask(Vector<int> op1, Vector<int> op2) {
        //ARM64-FULL-LINE: add {{z[0-9]+}}.s, {{z[0-9]+}}.s, {{z[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: movz {{.*}}
        Vector<int> result = Sve.ConditionalSelect(Sve.CreateTrueMaskInt32(), Sve.Add(op1, op2), op1);
        Consume(result);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void CndSelectOptionalEmbeddedAllBits(Vector<int> op1, Vector<int> op2) {
        //ARM64-FULL-LINE: add {{z[0-9]+}}.s, {{z[0-9]+}}.s, {{z[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: movz {{.*}}
        Vector<int> result = Sve.ConditionalSelect(Vector<int>.AllBitsSet, Sve.Add(op1, op2), op1);
        Consume(result);
    }


    // SVE one op operation (with embedded mask) inside a conditional select

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void CndSelectEmbeddedOneOp(Vector<int> mask, Vector<int> op1) {
        //ARM64-FULL-LINE: abs {{z[0-9]+}}.s, {{p[0-9]+}}/m, {{z[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: movz {{.*}}
        Vector<int> result = Sve.ConditionalSelect(mask, Sve.Abs(op1), op1);
        Consume(result);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void CndSelectEmbeddedOneOpFalseMask(Vector<int> dummy, Vector<int> op1) {
        //ARM64-FULL-LINE: mov v0.16b, v1.16b
        Vector<int> result = Sve.ConditionalSelect(Sve.CreateFalseMaskInt32(), Sve.Abs(op1), op1);
        Consume(result);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void CndSelectEmbeddedOneOpZero(Vector<int> dummy, Vector<int> op1) {
        //ARM64-FULL-LINE: mov v0.16b, v1.16b
        Vector<int> result = Sve.ConditionalSelect(Vector<int>.Zero, Sve.Abs(op1), op1);
        Consume(result);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void CndSelectEmbeddedOneOpTrueMask(Vector<int> op1) {
        //ARM64-FULL-LINE: ptrue {{p[0-9]+}}.s
        //ARM64-FULL-LINE: abs {{z[0-9]+}}.s, {{p[0-9]+}}/m, {{z[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: movz {{.*}}
        Vector<int> result = Sve.ConditionalSelect(Sve.CreateTrueMaskInt32(), Sve.Abs(op1), op1);
        Consume(result);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void CndSelectEmbeddedOneOpAllBits(Vector<int> op1) {
        //ARM64-FULL-LINE: ptrue {{p[0-9]+}}.s
        //ARM64-FULL-LINE: abs {{z[0-9]+}}.s, {{p[0-9]+}}/m, {{z[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: movz {{.*}}
        Vector<int> result = Sve.ConditionalSelect(Vector<int>.AllBitsSet, Sve.Abs(op1), op1);
        Consume(result);
    }


    // SVE reduction operation (with embedded mask) inside a conditional select.
    // The op and conditional select cannot be combined into one instruction.

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void CndSelectEmbeddedReduction(Vector<long> mask, Vector<int> op1, Vector<long> opf) {
        //ARM64-FULL-LINE: cmpne {{p[0-9]+}}.d, {{p[0-9]+}}/z, {{z[0-9]+}}.d, #0
        //ARM64-FULL-LINE-NEXT: ptrue {{p[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: saddv {{d[0-9]+}}, {{p[0-9]+}}, {{z[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: sel {{z[0-9]+}}.d, {{p[0-9]+}}, {{z[0-9]+}}.d, {{z[0-9]+}}.d
        Vector<long> result = Sve.ConditionalSelect(mask, Sve.AddAcross(op1), opf);
        Consume(result);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void CndSelectEmbeddedReductionFalseMask(Vector<int> op1, Vector<long> opf) {
        //ARM64-FULL-LINE: mov v0.16b, v1.16b
        Vector<long> result = Sve.ConditionalSelect(Sve.CreateFalseMaskInt64(), Sve.AddAcross(op1), opf);
        Consume(result);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void CndSelectEmbeddedReductionZero(Vector<int> op1, Vector<long> opf) {
        //ARM64-FULL-LINE: mov v0.16b, v1.16b
        Vector<long> result = Sve.ConditionalSelect(Vector<long>.Zero, Sve.AddAcross(op1), opf);
        Consume(result);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void CndSelectEmbeddedReductionTrueMask(Vector<int> op1, Vector<long> opf) {
        //ARM64-FULL-LINE: ptrue {{p[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: saddv {{d[0-9]+}}, {{p[0-9]+}}, {{z[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: movz {{.*}}
        Vector<long> result = Sve.ConditionalSelect(Sve.CreateTrueMaskInt64(), Sve.AddAcross(op1), opf);
        Consume(result);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void CndSelectEmbeddedReductionAllBits(Vector<int> op1, Vector<long> opf) {
        //ARM64-FULL-LINE: ptrue {{p[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: saddv {{d[0-9]+}}, {{p[0-9]+}}, {{z[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: movz {{.*}}
        Vector<long> result = Sve.ConditionalSelect(Vector<long>.AllBitsSet, Sve.AddAcross(op1), opf);
        Consume(result);
    }

}
