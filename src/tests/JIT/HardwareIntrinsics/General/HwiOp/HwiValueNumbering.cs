// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Runtime.CompilerServices;
using Xunit;

// Tests that we value number certain intrinsics correctly.
//
public unsafe class HwiValueNumbering
{
    [Fact]
    public static void TestProblemWithLoadLow_Sse()
    {
        if (Sse.IsSupported)
        {
            ProblemWithLoadLow_Sse();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ProblemWithLoadLow_Sse()
    {
        var data = stackalloc float[2];
        data[0] = 1;
        data[1] = 2;
        JitUse(data);

        Vector128<float> a = Vector128<float>.Zero;
        Vector128<float> b = Sse.LoadLow(a, data);
        Vector128<float> c = Sse.LoadLow(a, data + 1);

        // Make sure we take into account the address operand.
        Assert.NotEqual(b.AsInt32().GetElement(0), c.AsInt32().GetElement(0));

        // Make sure we take the heap state into account.
        b = Sse.LoadLow(a, data);
        data[0] = 3;
        c = Sse.LoadLow(a, data);
        Assert.NotEqual(b.AsInt32().GetElement(0), c.AsInt32().GetElement(0));
    }

    [Fact]
    public static void TestProblemWithLoadLow_Sse2()
    {
        if (Sse2.IsSupported)
        {
            ProblemWithLoadLow_Sse2();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ProblemWithLoadLow_Sse2()
    {
        var data = stackalloc double[2];
        data[0] = 1;
        data[1] = 2;
        JitUse(data);

        Vector128<double> a = Vector128<double>.Zero;
        Vector128<double> b = Sse2.LoadLow(a, data);
        Vector128<double> c = Sse2.LoadLow(a, data + 1);

        // Make sure we take into account the address operand.
        Assert.NotEqual(b.AsInt64().GetElement(0), c.AsInt64().GetElement(0));

        // Make sure we take the heap state into account.
        b = Sse2.LoadLow(a, data);
        data[0] = 3;
        c = Sse2.LoadLow(a, data);
        Assert.NotEqual(b.AsInt64().GetElement(0), c.AsInt64().GetElement(0));
    }

    [Fact]
    public static void TestProblemWithLoadHigh_Sse()
    {
        if (Sse.IsSupported)
        {
            ProblemWithLoadHigh_Sse();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ProblemWithLoadHigh_Sse()
    {
        var data = stackalloc float[2];
        data[0] = 1;
        data[1] = 2;
        JitUse(data);

        Vector128<float> a = Vector128<float>.Zero;
        Vector128<float> b = Sse.LoadHigh(a, data);
        Vector128<float> c = Sse.LoadHigh(a, data + 1);

        // Make sure we take into account the address operand.
        Assert.NotEqual(b.AsInt64().GetElement(1), c.AsInt64().GetElement(1));

        // Make sure we take the heap state into account.
        b = Sse.LoadHigh(a, data);
        data[0] = 3;
        c = Sse.LoadHigh(a, data);
        Assert.NotEqual(b.AsInt64().GetElement(1), c.AsInt64().GetElement(1));
    }

    [Fact]
    public static void TestProblemWithLoadHigh_Sse2()
    {
        if (Sse2.IsSupported)
        {
            ProblemWithLoadHigh_Sse2();
        }
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ProblemWithLoadHigh_Sse2()
    {
        var data = stackalloc double[2];
        data[0] = 1;
        data[1] = 2;
        JitUse(data);

        Vector128<double> a = Vector128<double>.Zero;
        Vector128<double> b = Sse2.LoadHigh(a, data);
        Vector128<double> c = Sse2.LoadHigh(a, data + 1);

        // Make sure we take into account the address operand.
        Assert.NotEqual(b.AsInt64().GetElement(1), c.AsInt64().GetElement(1));

        // Make sure we take the heap state into account.
        b = Sse2.LoadHigh(a, data);
        data[0] = 3;
        c = Sse2.LoadHigh(a, data);
        Assert.NotEqual(b.AsInt64().GetElement(1), c.AsInt64().GetElement(1));
    }

    [Fact]
    public static void TestProblemWithMaskLoad_Avx()
    {
        if (Avx.IsSupported)
        {
            ProblemWithMaskLoad_Avx();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ProblemWithMaskLoad_Avx()
    {
        const double Mask = -0.0;

        var data = stackalloc double[2];
        data[0] = 1;
        data[1] = 1;
        JitUse(data);

        // Make sure we take mask into account.
        var v1 = Avx.MaskLoad(data, Vector128.Create(0, Mask));
        if (v1.GetElement(0) == 0)
        {
            var v2 = Avx.MaskLoad(data, Vector128.Create(Mask, 0));
            Assert.NotEqual(0, v2.GetElement(0));
        }
    }

    [Fact]
    public static void TestProblemWithMaskLoad_Avx2()
    {
        if (Avx2.IsSupported)
        {
            ProblemWithMaskLoad_Avx2();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ProblemWithMaskLoad_Avx2()
    {
        const long Mask = -0x8000000000000000;

        var data = stackalloc long[2];
        data[0] = 1;
        data[1] = 1;
        JitUse(data);

        // Make sure we take mask into account.
        var v1 = Avx2.MaskLoad(data, Vector128.Create(0, Mask));
        if (v1.GetElement(0) == 0)
        {
            var v2 = Avx2.MaskLoad(data, Vector128.Create(Mask, 0));
            Assert.NotEqual(0, v2.GetElement(0));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void JitUse<T>(T* arg) where T : unmanaged { }
}
