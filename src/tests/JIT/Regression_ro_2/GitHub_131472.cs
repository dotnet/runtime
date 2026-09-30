// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Xunit;

public class GitHub_131472
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static FloatComparisonMode Opaque(FloatComparisonMode value) => value;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector256<float> Compare(Vector256<float> l, Vector256<float> r, FloatComparisonMode m) => Avx.Compare(l, r, m);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    private static Vector256<float> Reference(Vector256<float> l, Vector256<float> r, FloatComparisonMode m) => Avx.Compare(l, r, m);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector512<float> Compare(Vector512<float> l, Vector512<float> r, FloatComparisonMode m) => Avx512F.Compare(l, r, m);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    private static Vector512<float> Reference(Vector512<float> l, Vector512<float> r, FloatComparisonMode m) => Avx512F.Compare(l, r, m);

    [Fact]
    public static void TestEntryPoint()
    {
        // A non-constant mode must expand without leaving right/mode uninitialized when Compare is
        // promoted to its mask-returning form. Vector256 exercises the optional EVEX promotion;
        // Vector512 exercises the mandatory-mask path.
        for (int i = 0; i <= (int)FloatComparisonMode.UnorderedTrueSignaling; i++)
        {
            FloatComparisonMode mode = Opaque((FloatComparisonMode)i);

            if (Avx.IsSupported)
            {
                Vector256<float> l = Vector256.Create(1f, 2f, 3f, 4f, 5f, 6f, 7f, 8f);
                Vector256<float> r = Vector256.Create(8f, 2f, 6f, 4f, 4f, 6f, 2f, 8f);
                Assert.Equal(Reference(l, r, mode), Compare(l, r, mode));
            }

            if (Avx512F.IsSupported)
            {
                Vector512<float> l = Vector512.Create(1f, 2f, 3f, 4f, 5f, 6f, 7f, 8f, 9f, 10f, 11f, 12f, 13f, 14f, 15f, 16f);
                Vector512<float> r = Vector512.Create(8f, 2f, 6f, 4f, 4f, 6f, 2f, 8f, 16f, 10f, 12f, 12f, 12f, 14f, 10f, 16f);
                Assert.Equal(Reference(l, r, mode), Compare(l, r, mode));
            }
        }
    }
}
