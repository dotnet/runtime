// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_133783
{
    [Fact]
    public static void TestEntryPoint()
    {
        // DOTNET_EnableAVX2=0 exercises the BSF/BSR fallbacks on newer x86 CPUs.
        for (uint value = 0; value <= 3; value++)
        {
            Assert.Equal(value < 2, Log2IsZero(value));
            Assert.Equal(value >= 2, Log2NotZero(value));
            Assert.Equal((value & 1) != 0, TzcIsZero(value));
            Assert.Equal(value < 2, Log2IsZero((ulong)value));
            Assert.Equal(value >= 2, Log2NotZero((ulong)value));
            Assert.Equal((value & 1) != 0, TzcIsZero((ulong)value));
        }

        Assert.False(Log2IsZero(1UL << 32));
        Assert.True(Log2NotZero(1UL << 63));
        Assert.False(TzcIsZero(1UL << 32));
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static bool Log2IsZero(uint value) => BitOperations.Log2(value) == 0;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static bool Log2NotZero(uint value) => BitOperations.Log2(value) != 0;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static bool TzcIsZero(uint value) => BitOperations.TrailingZeroCount(value) == 0;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static bool Log2IsZero(ulong value) => BitOperations.Log2(value) == 0;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static bool Log2NotZero(ulong value) => BitOperations.Log2(value) != 0;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static bool TzcIsZero(ulong value) => BitOperations.TrailingZeroCount(value) == 0;
}
