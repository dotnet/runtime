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

            CndSelectEmbeddedF(op1, op2, op3);
            CndSelectEmbeddedZ(op1, op2, op3);
            CndSelectEmbeddedFalseMaskF(op1, op2);
            CndSelectEmbeddedFalseMaskZ(op1, op2);
            CndSelectEmbeddedZeroF(op1, op2);
            CndSelectEmbeddedZeroZ(op1, op2);
            CndSelectEmbeddedTrueMaskF(op1, op2);
            CndSelectEmbeddedTrueMaskZ(op1, op2);
            CndSelectEmbeddedAllBitsF(op1, op2);
            CndSelectEmbeddedAllBitsZ(op1, op2);

            CndSelectOptionalEmbeddedF(op1, op2, op3);
            CndSelectOptionalEmbeddedZ(op1, op2, op3);
            CndSelectOptionalEmbeddedFalseMaskF(op1, op2);
            CndSelectOptionalEmbeddedFalseMaskZ(op1, op2);
            CndSelectOptionalEmbeddedZeroF(op1, op2);
            CndSelectOptionalEmbeddedZeroZ(op1, op2);
            CndSelectOptionalEmbeddedTrueMaskF(op1, op2);
            CndSelectOptionalEmbeddedTrueMaskZ(op1, op2);
            CndSelectOptionalEmbeddedAllBitsF(op1, op2);
            CndSelectOptionalEmbeddedAllBitsZ(op1, op2);

            CndSelectEmbeddedReductionF(opl1, op2);
            CndSelectEmbeddedReductionZ(opl1, op2);
            CndSelectEmbeddedReductionFalseMaskF(op1);
            CndSelectEmbeddedReductionFalseMaskZ(op1);
            CndSelectEmbeddedReductionZeroF(op1);
            CndSelectEmbeddedReductionZeroZ(op1);
            CndSelectEmbeddedReductionTrueMaskF(op1);
            CndSelectEmbeddedReductionTrueMaskZ(op1);
            CndSelectEmbeddedReductionAllBitsF(op1);
            CndSelectEmbeddedReductionAllBitsZ(op1);
        }
    }

    // SVE operation (with embedded mask) inside a conditional select

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void CndSelectEmbeddedF(Vector<int> mask, Vector<int> op1, Vector<int> op2) {
        //ARM6-FULL-LINE: sabd {{z[0-9]+}}.s, {{p[0-9]+}}/m, {{z[0-9]+}}.s, {{z[0-9]+}}.s
        var result = Sve.ConditionalSelect(mask, Sve.AbsoluteDifference(op1, op2), Sve.CreateFalseMaskInt32());
        Consume(result);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void CndSelectEmbeddedZ(Vector<int> mask, Vector<int> op1, Vector<int> op2) {
        //ARM6-FULL-LINE: sabd {{z[0-9]+}}.s, {{p[0-9]+}}/m, {{z[0-9]+}}.s, {{z[0-9]+}}.s
        var result = Sve.ConditionalSelect(mask, Sve.AbsoluteDifference(op1, op2), Vector<int>.Zero);
        Consume(result);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void CndSelectEmbeddedFalseMaskF(Vector<int> op1, Vector<int> op2) {
        //ARM6-FULL-LINE: movi {{v[0-9]+}}.4s, #0
        var result = Sve.ConditionalSelect(Sve.CreateFalseMaskInt32(), Sve.AbsoluteDifference(op1, op2), Sve.CreateFalseMaskInt32());
        Consume(result);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void CndSelectEmbeddedFalseMaskZ(Vector<int> op1, Vector<int> op2) {
        //ARM6-FULL-LINE: movi {{v[0-9]+}}.4s, #0
        var result = Sve.ConditionalSelect(Sve.CreateFalseMaskInt32(), Sve.AbsoluteDifference(op1, op2), Vector<int>.Zero);
        Consume(result);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void CndSelectEmbeddedZeroF(Vector<int> op1, Vector<int> op2) {
        //ARM6-FULL-LINE: movi {{v[0-9]+}}.4s, #0
        var result = Sve.ConditionalSelect(Vector<int>.Zero, Sve.AbsoluteDifference(op1, op2), Sve.CreateFalseMaskInt32());
        Consume(result);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void CndSelectEmbeddedZeroZ(Vector<int> op1, Vector<int> op2) {
        //ARM6-FULL-LINE: movi {{v[0-9]+}}.4s, #0
        var result = Sve.ConditionalSelect(Vector<int>.Zero, Sve.AbsoluteDifference(op1, op2), Vector<int>.Zero);
        Consume(result);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void CndSelectEmbeddedTrueMaskF(Vector<int> op1, Vector<int> op2) {
        //ARM6-FULL-LINE: ptrue {{p[0-9]+}}.s
        //ARM6-FULL-LINE-NEXT: sabd {{z[0-9]+}}.s, {{p[0-9]+}}/m, {{z[0-9]+}}.s, {{z[0-9]+}}.s
        var result = Sve.ConditionalSelect(Sve.CreateTrueMaskInt32(), Sve.AbsoluteDifference(op1, op2), Sve.CreateFalseMaskInt32());
        Consume(result);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void CndSelectEmbeddedTrueMaskZ(Vector<int> op1, Vector<int> op2) {
        //ARM6-FULL-LINE: ptrue {{p[0-9]+}}.s
        //ARM6-FULL-LINE-NEXT: movprfx {{z[0-9]+}}.s, {{p[0-9]+}}/m, {{z[0-9]+}}.s
        //ARM6-FULL-LINE-NEXT: sabd {{z[0-9]+}}.s, {{p[0-9]+}}/m, {{z[0-9]+}}.s, {{z[0-9]+}}.s
        var result = Sve.ConditionalSelect(Sve.CreateTrueMaskInt32(), Sve.AbsoluteDifference(op1, op2), Vector<int>.Zero);
        Consume(result);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void CndSelectEmbeddedAllBitsF(Vector<int> op1, Vector<int> op2) {
        //ARM6-FULL-LINE: mvni {{v[0-9]+}}.4s, #0
        //ARM6-FULL-LINE-NEXT: cmpne {{p[0-9]+}}.s, {{p[0-9]+}}/z, {{z[0-9]+}}.s, #0
        //ARM6-FULL-LINE-NEXT: movprfx {{z[0-9]+}}.s, {{p[0-9]+}}/m, {{z[0-9]+}}.s
        //ARM6-FULL-LINE-NEXT: sabd {{z[0-9]+}}.s, {{p[0-9]+}}/m, {{z[0-9]+}}.s, {{z[0-9]+}}.s
        var result = Sve.ConditionalSelect(Vector<int>.AllBitsSet, Sve.AbsoluteDifference(op1, op2), Sve.CreateFalseMaskInt32());
        Consume(result);
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    static void CndSelectEmbeddedAllBitsZ(Vector<int> op1, Vector<int> op2) {
        //ARM6-FULL-LINE: mvni {{v[0-9]+}}.4s, #0
        //ARM6-FULL-LINE-NEXT: cmpne {{p[0-9]+}}.s, {{p[0-9]+}}/z, {{z[0-9]+}}.s, #0
        //ARM6-FULL-LINE-NEXT: sabd {{z[0-9]+}}.s, {{p[0-9]+}}/m, {{z[0-9]+}}.s, {{z[0-9]+}}.s
        var result = Sve.ConditionalSelect(Vector<int>.AllBitsSet, Sve.AbsoluteDifference(op1, op2), Vector<int>.Zero);
        Consume(result);
    }

    // SVE operation (with optional embedded mask) inside a conditional select


    [MethodImpl(MethodImplOptions.NoInlining)]
    static void CndSelectOptionalEmbeddedF(Vector<int> mask, Vector<int> op1, Vector<int> op2) {
        //ARM6-FULL-LINE: add {{z[0-9]+}}.s, {{p[0-9]+}}/m, {{z[0-9]+}}.s, {{z[0-9]+}}.s
        var result = Sve.ConditionalSelect(mask, Sve.Add(op1, op2), Sve.CreateFalseMaskInt32());
        Consume(result);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void CndSelectOptionalEmbeddedZ(Vector<int> mask, Vector<int> op1, Vector<int> op2) {
        //ARM6-FULL-LINE: add {{z[0-9]+}}.s, {{p[0-9]+}}/m, {{z[0-9]+}}.s, {{z[0-9]+}}.s
        var result = Sve.ConditionalSelect(mask, Sve.Add(op1, op2), Vector<int>.Zero);
        Consume(result);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void CndSelectOptionalEmbeddedFalseMaskF(Vector<int> op1, Vector<int> op2) {
        //ARM6-FULL-LINE: movi {{v[0-9]+}}.4s, #0
        var result = Sve.ConditionalSelect(Sve.CreateFalseMaskInt32(), Sve.Add(op1, op2), Sve.CreateFalseMaskInt32());
        Consume(result);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void CndSelectOptionalEmbeddedFalseMaskZ(Vector<int> op1, Vector<int> op2) {
        //ARM6-FULL-LINE: movi {{v[0-9]+}}.4s, #0
        var result = Sve.ConditionalSelect(Sve.CreateFalseMaskInt32(), Sve.Add(op1, op2), Vector<int>.Zero);
        Consume(result);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void CndSelectOptionalEmbeddedZeroF(Vector<int> op1, Vector<int> op2) {
        //ARM6-FULL-LINE: movi {{v[0-9]+}}.4s, #0
        var result = Sve.ConditionalSelect(Vector<int>.Zero, Sve.Add(op1, op2), Sve.CreateFalseMaskInt32());
        Consume(result);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void CndSelectOptionalEmbeddedZeroZ(Vector<int> op1, Vector<int> op2) {
        //ARM6-FULL-LINE: movi {{v[0-9]+}}.4s, #0
        var result = Sve.ConditionalSelect(Vector<int>.Zero, Sve.Add(op1, op2), Vector<int>.Zero);
        Consume(result);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void CndSelectOptionalEmbeddedTrueMaskF(Vector<int> op1, Vector<int> op2) {
        //ARM64-FULL-LINE: add {{z[0-9]+}}.s, {{z[0-9]+}}.s, {{z[0-9]+}}.s
        Vector<int> result = Sve.ConditionalSelect(Sve.CreateTrueMaskInt32(), Sve.Add(op1, op2), Sve.CreateFalseMaskInt32());
        Consume(result);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void CndSelectOptionalEmbeddedTrueMaskZ(Vector<int> op1, Vector<int> op2) {
        //ARM64-FULL-LINE: add {{z[0-9]+}}.s, {{z[0-9]+}}.s, {{z[0-9]+}}.s
        Vector<int> result = Sve.ConditionalSelect(Sve.CreateTrueMaskInt32(), Sve.Add(op1, op2), Vector<int>.Zero);
        Consume(result);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void CndSelectOptionalEmbeddedAllBitsF(Vector<int> op1, Vector<int> op2) {
        //ARM6-FULL-LINE: add {{z[0-9]+}}.s, {{z[0-9]+}}.s, {{z[0-9]+}}.s
        var result = Sve.ConditionalSelect(Vector<int>.AllBitsSet, Sve.Add(op1, op2), Sve.CreateFalseMaskInt32());
        Consume(result);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void CndSelectOptionalEmbeddedAllBitsZ(Vector<int> op1, Vector<int> op2) {
        //ARM6-FULL-LINE: add {{z[0-9]+}}.s, {{z[0-9]+}}.s, {{z[0-9]+}}.s
        var result = Sve.ConditionalSelect(Vector<int>.AllBitsSet, Sve.Add(op1, op2), Vector<int>.Zero);
        Consume(result);
    }

    // SVE reduction operation (with embedded mask) inside a conditional select.
    // The op and conditional select cannot be combined into one instruction.

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void CndSelectEmbeddedReductionF(Vector<long> mask, Vector<int> op1) {
        //ARM64-FULL-LINE: cmpne {{p[0-9]+}}.d, {{p[0-9]+}}/z, {{z[0-9]+}}.d, #0
        //ARM64-FULL-LINE: ptrue {{p[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: saddv {{d[0-9]+}}, {{p[0-9]+}}, {{z[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: movi {{v[0-9]+}}.4s, #0
        //ARM64-FULL-LINE-NEXT: sel {{z[0-9]+}}.d, {{p[0-9]+}}, {{z[0-9]+}}.d, {{z[0-9]+}}.d
        Vector<long> result = Sve.ConditionalSelect(mask, Sve.AddAcross(op1), Sve.CreateFalseMaskInt64());
        Consume(result);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void CndSelectEmbeddedReductionZ(Vector<long> mask, Vector<int> op1) {
        //ARM64-FULL-LINE: cmpne {{p[0-9]+}}.d, {{p[0-9]+}}/z, {{z[0-9]+}}.d, #0
        //ARM64-FULL-LINE-NEXT: ptrue {{p[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: saddv {{d[0-9]+}}, {{p[0-9]+}}, {{z[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: movi {{v[0-9]+}}.4s, #0
        //ARM64-FULL-LINE-NEXT: sel {{z[0-9]+}}.d, {{p[0-9]+}}, {{z[0-9]+}}.d, {{z[0-9]+}}.d
        Vector<long> result = Sve.ConditionalSelect(mask, Sve.AddAcross(op1), Vector<long>.Zero);
        Consume(result);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void CndSelectEmbeddedReductionFalseMaskF(Vector<int> op1) {
        //ARM64-FULL-LINE: movi v0.4s, #0
        Vector<long> result = Sve.ConditionalSelect(Sve.CreateFalseMaskInt64(), Sve.AddAcross(op1), Sve.CreateFalseMaskInt64());
        Consume(result);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void CndSelectEmbeddedReductionFalseMaskZ(Vector<int> op1) {
        //ARM64-FULL-LINE: movi v0.4s, #0
        Vector<long> result = Sve.ConditionalSelect(Sve.CreateFalseMaskInt64(), Sve.AddAcross(op1), Vector<long>.Zero);
        Consume(result);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void CndSelectEmbeddedReductionZeroF(Vector<int> op1) {
        //ARM64-FULL-LINE: movi v0.4s, #0
        Vector<long> result = Sve.ConditionalSelect(Vector<long>.Zero, Sve.AddAcross(op1), Sve.CreateFalseMaskInt64());
        Consume(result);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void CndSelectEmbeddedReductionZeroZ(Vector<int> op1) {
        //ARM64-FULL-LINE: movi v0.4s, #0
        Vector<long> result = Sve.ConditionalSelect(Vector<long>.Zero, Sve.AddAcross(op1), Vector<long>.Zero);
        Consume(result);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void CndSelectEmbeddedReductionTrueMaskF(Vector<int> op1) {
        //ARM64-FULL-LINE: ptrue {{p[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: saddv {{d[0-9]+}}, {{p[0-9]+}}, {{z[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: movz {{.*}}
        Vector<long> result = Sve.ConditionalSelect(Sve.CreateTrueMaskInt64(), Sve.AddAcross(op1), Sve.CreateFalseMaskInt64());
        Consume(result);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void CndSelectEmbeddedReductionTrueMaskZ(Vector<int> op1) {
        //ARM64-FULL-LINE: ptrue {{p[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: saddv {{d[0-9]+}}, {{p[0-9]+}}, {{z[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: movz {{.*}}
        Vector<long> result = Sve.ConditionalSelect(Sve.CreateTrueMaskInt64(), Sve.AddAcross(op1), Vector<long>.Zero);
        Consume(result);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void CndSelectEmbeddedReductionAllBitsF(Vector<int> op1) {
        //ARM64-FULL-LINE: ptrue {{p[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: saddv {{d[0-9]+}}, {{p[0-9]+}}, {{z[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: movz {{.*}}
        Vector<long> result = Sve.ConditionalSelect(Vector<long>.AllBitsSet, Sve.AddAcross(op1), Sve.CreateFalseMaskInt64());
        Consume(result);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void CndSelectEmbeddedReductionAllBitsZ(Vector<int> op1) {
        //ARM64-FULL-LINE: ptrue {{p[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: saddv {{d[0-9]+}}, {{p[0-9]+}}, {{z[0-9]+}}.s
        //ARM64-FULL-LINE-NEXT: movz {{.*}}
        Vector<long> result = Sve.ConditionalSelect(Vector<long>.AllBitsSet, Sve.AddAcross(op1), Vector<long>.Zero);
        Consume(result);
    }
}
