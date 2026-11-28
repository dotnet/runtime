// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using Xunit;

public class ConditionalSelectConstants
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    [Fact]
    public static void TestConditionalSelectConstants()
    {
        if (Sve.IsSupported)
        {
            Assert.Equal(Vector.Create<int>(-1), Smoke());
            Assert.Equal((Vector<int>.Count - 1) * 4 + 3, (int)Sve.AddAcross(ConditionalSelect1CC())[0]);
            Assert.Equal((Vector<int>.Count - 1) * -1, (int)Sve.AddAcross(ConditionalSelect1FT())[0]);
            Assert.Equal(4080, (int)Sve.AddAcross(ConditionalSelect16TF())[0]);
            Assert.Equal((Vector<int>.Count - 2) * -1 + 2 * 9, (int)Sve.AddAcross(ConditionalSelect2CT())[0]);
            Assert.Equal(5, ConditionalSelectConsts());
            Assert.False(ConditionalSelectConstsNoMaskPattern());
            Assert.Equal(0, ConditionalSelectConstsNoMaskPattern2());
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<int> Smoke()
    {
        return Sve.CreateTrueMaskInt32();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<int> ConditionalSelect1CC()
    {
        return Sve.ConditionalSelect(
            Sve.CreateTrueMaskInt32(SveMaskPattern.VectorCount1),
            Vector.Create<int>(3),
            Vector.Create<int>(4)
        );
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<int> ConditionalSelect1FT()
    {
        return Sve.ConditionalSelect(
            Sve.CreateTrueMaskInt32(SveMaskPattern.VectorCount1),
            Sve.CreateFalseMaskInt32(),
            Sve.CreateTrueMaskInt32()
        );
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<byte> ConditionalSelect16TF()
    {
        return Sve.ConditionalSelect(
            Sve.CreateTrueMaskByte(SveMaskPattern.VectorCount16),
            Sve.CreateTrueMaskByte(),
            Sve.CreateFalseMaskByte()
        );
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<int> ConditionalSelect2CT()
    {
        return Sve.ConditionalSelect(
            Sve.CreateTrueMaskInt32(SveMaskPattern.VectorCount2),
            Vector.Create<int>(9),
            Sve.CreateTrueMaskInt32()
        );
    }

    // Valuenum will optimise away the ConditionalSelect.

    [MethodImpl(MethodImplOptions.NoInlining)]
    static sbyte ConditionalSelectConsts()
    {
        var vec = Sve.ConditionalSelect(Vector128.CreateScalar((sbyte)49).AsVector(),
                                        Vector128.CreateScalar((sbyte)0).AsVector(),
                                        Vector.Create<sbyte>(107));
        return Sve.ConditionalExtractLastActiveElement(Vector128.CreateScalar((sbyte)0).AsVector(), 5, vec);
    }

    // Valuenum will optimise away the ConditionalSelect.
    // vr0 is a constant mask, but is not a known pattern.

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool ConditionalSelectConstsNoMaskPattern()
    {
        var vr2 = Vector128.CreateScalar(5653592783208606001L).AsVector();
        var vr3 = Vector128.CreateScalar(6475288982576452694L).AsVector();
        var vr4 = Vector.Create<long>(1);
        var vr0 = Sve.ConditionalSelect(vr2, vr3, vr4);
        var vr5 = Vector128.CreateScalar((long)0).AsVector();
        return Sve.TestFirstTrue(vr0, vr5);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int ConditionalSelectConstsNoMaskPattern2()
    {
        var vr0 = Sve.ConditionalSelect(Vector128.CreateScalar((short)1).AsVector(), Vector.Create<short>(0), Vector.Create<short>(1));
        return Sve.ConditionalExtractAfterLastActiveElement(vr0, 1, Vector.Create<short>(0));
    }
}
