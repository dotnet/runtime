// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Xunit;

namespace Runtime_137901;

public class Runtime_137901
{
    [ConditionalFact(typeof(Avx), nameof(Avx.IsSupported))]
    public static void ConvertVector256DoubleToInt32()
    {
        Vector256<double> d = Vector256.Create(1.0, 2.0, 3.0, 4.0);
        Vector128<long> c = Vector128.Create(-1L, -1L);

        Assert.Equal(Vector128.Create(1, 2, 3, 4), Select256To128(d, Vector128.Create(1L, 1L), Vector128<long>.Zero, c).AsInt32());
        Assert.Equal(Vector128.Create(1, 2, -1, -1), Select256To128(d, Vector128.Create(1L, 0L), Vector128<long>.Zero, c).AsInt32());
    }

    [ConditionalFact(typeof(Sse2), nameof(Sse2.IsSupported))]
    public static void ConvertVector128DoubleToSingle()
    {
        Vector128<double> d = Vector128.Create(1.0, 2.0);
        Vector128<double> c = Vector128.Create(-1.0, -1.0);
        Vector128<double> expected = Vector128.Create(Vector128.Create(1.0f, 2.0f, 0.0f, 0.0f).AsDouble().GetElement(0), -1.0);

        Assert.Equal(expected, Select128To64(d, Vector128.Create(1.0, 0.0), Vector128<double>.Zero, c));
    }

    [ConditionalFact(typeof(Avx512F), nameof(Avx512F.IsSupported))]
    public static void ConvertVector512DoubleToInt32()
    {
        Vector512<double> d = Vector512.Create(1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0);
        Vector256<long> c = Vector256.Create(-1L);

        Assert.Equal(Vector256.Create(1, 2, 3, 4, 5, 6, 7, 8), Select512To256(d, Vector256.Create(1L), Vector256<long>.Zero, c).AsInt32());
        Assert.Equal(Vector256.Create(1, 2, -1, -1, 5, 6, -1, -1), Select512To256(d, Vector256.Create(1L, 0L, 1L, 0L), Vector256<long>.Zero, c).AsInt32());
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector128<long> Select256To128(Vector256<double> d, Vector128<long> a, Vector128<long> b, Vector128<long> c)
    {
        Vector128<long> conv = Avx.ConvertToVector128Int32WithTruncation(d).AsInt64();
        return Vector128.ConditionalSelect(Vector128.GreaterThan(a, b), conv, c);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector128<double> Select128To64(Vector128<double> d, Vector128<double> a, Vector128<double> b, Vector128<double> c)
    {
        Vector128<double> conv = Sse2.ConvertToVector128Single(d).AsDouble();
        return Vector128.ConditionalSelect(Vector128.GreaterThan(a, b), conv, c);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector256<long> Select512To256(Vector512<double> d, Vector256<long> a, Vector256<long> b, Vector256<long> c)
    {
        Vector256<long> conv = Avx512F.ConvertToVector256Int32WithTruncation(d).AsInt64();
        return Vector256.ConditionalSelect(Vector256.GreaterThan(a, b), conv, c);
    }
}
