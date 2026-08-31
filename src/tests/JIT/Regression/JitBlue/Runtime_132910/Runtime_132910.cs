// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.Arm;
using Xunit;

public class Runtime_132910
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector<float> Repro() => Sve.CreateTrueMaskSingle(SveMaskPattern.VectorCount5);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector<long> CreateVectorCount16Mask() => Sve.CreateTrueMaskInt64(SveMaskPattern.VectorCount16);

    [ConditionalFact(typeof(Sve), nameof(Sve.IsSupported))]
    public static void TestEntryPoint()
    {
        _ = Repro();

        Vector<long> mask = CreateVectorCount16Mask();
        const int RequestedCount = 16;
        int activeElementCount = Vector<long>.Count >= RequestedCount ? RequestedCount : 0;

        for (int i = 0; i < Vector<long>.Count; i++)
        {
            Assert.Equal(i < activeElementCount ? -1L : 0L, mask[i]);
        }
    }
}
