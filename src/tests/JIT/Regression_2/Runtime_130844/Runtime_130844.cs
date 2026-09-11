// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Exercises vector equality/inequality comparisons whose mask element type can
// differ from the declared comparison base type, hitting the mask-inversion path
// in LowerHWIntrinsicCmpOp on hardware with AVX512 (EVEX mask + KORTEST). The
// element count is below 8 so an incorrect base type would produce spurious high
// bits in the inverted constant mask.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

public static class Runtime_130844
{
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static bool EqDouble128(Vector128<double> a, Vector128<double> b) => a == b;

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static bool NeDouble128(Vector128<double> a, Vector128<double> b) => a != b;

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static bool EqDouble256(Vector256<double> a, Vector256<double> b) => a == b;

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static bool NeDouble256(Vector256<double> a, Vector256<double> b) => a != b;

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static bool EqLong128(Vector128<long> a, Vector128<long> b) => a == b;

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static bool NeLong128(Vector128<long> a, Vector128<long> b) => a != b;

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static bool EqLong256(Vector256<long> a, Vector256<long> b) => a == b;

    // Reinterpret pattern: the comparison mask is produced with one element type
    // but consumed against an AllBitsSet of a different element type.
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static bool EqualsAsInt128(Vector128<double> a, Vector128<double> b)
        => Vector128.Equals(a, b).AsInt32() == Vector128<int>.AllBitsSet;

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static bool NotEqualsAsInt256(Vector256<double> a, Vector256<double> b)
        => Vector256.Equals(a, b).AsInt32() != Vector256<int>.AllBitsSet;

    [Fact]
    public static void TestEntryPoint()
    {
        Vector128<double> d128 = Vector128.Create(1.0, 2.0);
        Vector128<double> d128b = Vector128.Create(1.0, 9.0);
        Vector256<double> d256 = Vector256.Create(1.0, 2.0, 3.0, 4.0);
        Vector256<double> d256b = Vector256.Create(1.0, 2.0, 3.0, 9.0);

        Assert.True(EqDouble128(d128, d128));
        Assert.False(EqDouble128(d128, d128b));
        Assert.False(NeDouble128(d128, d128));
        Assert.True(NeDouble128(d128, d128b));

        Assert.True(EqDouble256(d256, d256));
        Assert.False(EqDouble256(d256, d256b));
        Assert.False(NeDouble256(d256, d256));
        Assert.True(NeDouble256(d256, d256b));

        Vector128<long> l128 = Vector128.Create(1L, 2L);
        Vector128<long> l128b = Vector128.Create(1L, 9L);
        Vector256<long> l256 = Vector256.Create(1L, 2L, 3L, 4L);
        Vector256<long> l256b = Vector256.Create(1L, 2L, 3L, 9L);

        Assert.True(EqLong128(l128, l128));
        Assert.False(EqLong128(l128, l128b));
        Assert.False(NeLong128(l128, l128));
        Assert.True(NeLong128(l128, l128b));

        Assert.True(EqLong256(l256, l256));
        Assert.False(EqLong256(l256, l256b));

        Assert.True(EqualsAsInt128(d128, d128));
        Assert.False(EqualsAsInt128(d128, d128b));

        Assert.False(NotEqualsAsInt256(d256, d256));
        Assert.True(NotEqualsAsInt256(d256, d256b));
    }
}
