// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Xunit;

public static unsafe class AvxState
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void StoreZero256(ref Vector256<int> destination)
    {
        // X64-NOT: vzeroupper
        destination = Vector256<int>.Zero;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void StoreZero512(ref Vector512<int> destination)
    {
        // X64-NOT: vzeroupper
        destination = Vector512<int>.Zero;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void StoreScalar256(ref Vector256<int> destination, int value)
    {
        // X64-NOT: vzeroupper
        destination = Vector256.CreateScalar(value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void StoreAlignedZero256(float* destination)
    {
        // X64-NOT: vzeroupper
        if (Avx.IsSupported)
        {
            Avx.StoreAligned(destination, Vector256<float>.Zero);
        }
        else
        {
            Vector256<float>.Zero.Store(destination);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Widen128(ref Vector256<int> destination, ref Vector128<int> source, int value)
    {
        // X64-NOT: vmovaps {{ymm[0-9]+}}, {{ymm[0-9]+}}
        Vector128<int> vector = Vector128.Create(value);
        destination = vector.ToVector256Unsafe();
        source = vector;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Widen256(ref Vector512<int> destination, ref Vector256<int> source, int value)
    {
        // X64-NOT: vmovaps {{zmm[0-9]+}}, {{zmm[0-9]+}}
        Vector256<int> vector = Vector256.Create(value);
        destination = vector.ToVector512Unsafe();
        source = vector;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector128<int> Lower128(ref Vector256<int> source)
    {
        // X64-NOT: vzeroupper
        return source.GetLower();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CallUnmanaged(ref Vector128<float> destination, delegate* unmanaged<int> callback)
    {
        // X64: {{vzeroupper| xorps }}
        destination = Vector128<float>.Zero;
        return callback();
    }

    [UnmanagedCallersOnly]
    private static int Callback() => 123;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Divide256(Vector256<int> left, Vector256<int> right)
    {
        // X64-NOT: vxorpd ymm
        return (left / right).GetElement(0);
    }

    [Fact]
    public static void TestStores()
    {
        Vector256<int> vector256 = Vector256.Create(-1);
        StoreZero256(ref vector256);
        Assert.Equal(Vector256<int>.Zero, vector256);
        StoreScalar256(ref vector256, 123);
        Assert.Equal(Vector256.CreateScalar(123), vector256);

        Vector512<int> vector512 = Vector512.Create(-1);
        StoreZero512(ref vector512);
        Assert.Equal(Vector512<int>.Zero, vector512);

        byte* buffer = stackalloc byte[95];
        float* aligned = (float*)(((nuint)buffer + 31) & ~(nuint)31);
        Vector256.Create(1.0f).Store(aligned);
        StoreAlignedZero256(aligned);
        Assert.Equal(Vector256<float>.Zero, Vector256.Load(aligned));

        Vector128<int> vector128 = default;
        Widen128(ref vector256, ref vector128, 123);
        Assert.Equal(Vector128.Create(123), vector128);
        Assert.Equal(vector128, vector256.GetLower());
        Widen256(ref vector512, ref vector256, 456);
        Assert.Equal(Vector256.Create(456), vector256);
        Assert.Equal(vector256, vector512.GetLower());
        Assert.Equal(Vector128.Create(456), Lower128(ref vector256));

        Vector128<float> floats = Vector128.Create(1.0f);
        Assert.Equal(123, CallUnmanaged(ref floats, &Callback));
        Assert.Equal(Vector128<float>.Zero, floats);
        Assert.Equal(40, Divide256(Vector256.Create(120), Vector256.Create(3)));
    }
}
