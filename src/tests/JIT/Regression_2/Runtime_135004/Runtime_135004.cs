// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Xunit;

// IsRedundantStackMov ignored EVEX embedded masking, so a full load or store of
// a stack local was dropped when it followed a masked store of the same register
// to that local.

public unsafe class Runtime_135004
{
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static void MaskedStoreThenLoad512(Vector512<float>* src, Vector512<float>* mask, Vector512<float>* dst)
    {
        Vector512<float> y = *src + *src;
        Vector512<float> v = Vector512.Create(-1f);
        Avx512F.MaskStore((float*)&v, *mask, y);
        *dst = v;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static float MaskedStoreThenStore512(Vector512<float>* src, Vector512<float>* mask)
    {
        Vector512<float> y = *src + *src;
        Vector512<float> v = Vector512.Create(-1f);
        Avx512F.MaskStore((float*)&v, *mask, y);
        Unsafe.Write(&v, y);
        return Sum(&v);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static float MaskedStoreThenStore256(Vector256<float>* src, Vector256<float>* mask)
    {
        Vector256<float> y = *src + *src;
        Vector256<float> v = Vector256.Create(-1f);
        Avx512F.VL.MaskStore((float*)&v, *mask, y);
        Unsafe.Write(&v, y);
        return Sum(&v);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static float Sum(Vector512<float>* p) => Vector512.Sum(*p);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static float Sum(Vector256<float>* p) => Vector256.Sum(*p);

    [Fact]
    public static void TestEntryPoint()
    {
        if (!Avx512F.VL.IsSupported)
        {
            return;
        }

        // Only element 0 is selected by the mask.
        Vector512<float> src512 = Vector512.Create(1f);
        Vector512<float> mask512 = Vector512.CreateScalar(-0f);
        Vector256<float> src256 = Vector256.Create(1f);
        Vector256<float> mask256 = Vector256.CreateScalar(-0f);

        Vector512<float> dst512;
        MaskedStoreThenLoad512(&src512, &mask512, &dst512);
        Assert.Equal(Vector512.Create(-1f).WithElement(0, 2f), dst512);

        Assert.Equal(32f, MaskedStoreThenStore512(&src512, &mask512));
        Assert.Equal(16f, MaskedStoreThenStore256(&src256, &mask256));
    }
}
