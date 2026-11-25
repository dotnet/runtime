// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.Arm;
using Xunit;

public class Runtime_132910
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector<float> Repro() => Sve.CreateTrueMaskSingle(SveMaskPattern.VectorCount5);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector<float> CreateVectorCount256Mask() => Sve.CreateTrueMaskSingle(SveMaskPattern.VectorCount256);

    [ConditionalFact(typeof(Sve), nameof(Sve.IsSupported))]
    public static void TestEntryPoint()
    {
        const int VectorCount5 = 5;

        AssertMask(Repro(), Vector<float>.Count >= VectorCount5 ? VectorCount5 : 0);
        AssertMask(CreateVectorCount256Mask(), 0);
    }

    private static void AssertMask(Vector<float> mask, int activeElementCount)
    {
        for (int i = 0; i < Vector<float>.Count; i++)
        {
            Assert.Equal(i < activeElementCount ? -1 : 0, BitConverter.SingleToInt32Bits(mask[i]));
        }
    }
}
