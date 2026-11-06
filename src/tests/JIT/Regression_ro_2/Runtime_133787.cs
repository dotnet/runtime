// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Xunit;

public class Runtime_133787
{
    [ConditionalFact(typeof(Sse2), nameof(Sse2.IsSupported))]
    public static void TestEntryPoint()
    {
        Vector128<byte> value = Vector128.Create(
            (byte)1, 2, 3, 4, 5, 6, 7, 8,
            9, 10, 11, 12, 13, 14, 15, 16);

        Vector128<byte> expected = Vector128.Create(
            (byte)2, 3, 4, 5, 6, 7, 8, 9,
            10, 11, 12, 13, 14, 15, 16, 0);

        Assert.Equal(expected, NestedShuffle(value, 1));
        Assert.Equal(expected, Shuffle(value, 1));
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector128<byte> Shuffle(Vector128<byte> value, byte count)
    {
        Vector128<byte> indices = Vector128.Create(
            (byte)0, 1, 2, 3, 4, 5, 6, 7,
            8, 9, 10, 11, 12, 13, 14, 15);

        return Vector128.Shuffle(Sse2.ShiftRightLogical128BitLane(value, count), indices);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector128<byte> NestedShuffle(Vector128<byte> value, byte count)
    {
        Vector128<byte> indices = Vector128.Create(
            (byte)0, 1, 2, 3, 4, 5, 6, 7,
            8, 9, 10, 11, 12, 13, 14, 15);

        return Vector128.Shuffle(
            Vector128.Shuffle(
                Vector128.Shuffle(
                    Vector128.Shuffle(Sse2.ShiftRightLogical128BitLane(value, count), indices),
                    indices),
                indices),
            indices);
    }
}
