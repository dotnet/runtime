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
    public static int TestConditionalSelectConstants()
    {
        bool fail = false;

        if (Sve.IsSupported)
        {
            var r1 = Sve.AddAcross(ConditionalSelect1CC());
            Console.WriteLine(r1[0]);
            if (r1[0] != 15)
            {
                fail = true;
            }

            var r2 = Sve.AddAcross(ConditionalSelect1FT());
            Console.WriteLine(r2[0]);
            if (r2[0] != -3)
            {
                fail = true;
            }

            var r3 = Sve.AddAcross(ConditionalSelect16TF());
            Console.WriteLine(r3[0]);
            if (r3[0] != 4080)
            {
                fail = true;
            }

            var r4 = Sve.AddAcross(ConditionalSelect2CT());
            Console.WriteLine(r4[0]);
            if (r4[0] != 16)
            {
                fail = true;
            }

            var r5 = ConditionalSelectConsts();
            Console.WriteLine(r5);
            if (r5 != 5)
            {
                fail = true;
            }
        }

        if (fail)
        {
            return 101;
        }
        return 100;
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

    [MethodImpl(MethodImplOptions.NoInlining)]
    static sbyte ConditionalSelectConsts()
    {
        var vec = Sve.ConditionalSelect(Vector128.CreateScalar((sbyte)49).AsVector(),
                                        Vector128.CreateScalar((sbyte)0).AsVector(),
                                        Vector.Create<sbyte>(107));
        return Sve.ConditionalExtractLastActiveElement(Vector128.CreateScalar((sbyte)0).AsVector(), 5, vec);
    }
}
