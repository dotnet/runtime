// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

public class Runtime_133085
{
    private struct Struct128
    {
        public ulong U0;
        public ulong U1;
    }

    private struct Struct256
    {
        public ulong U0;
        public ulong U1;
        public ulong U2;
        public ulong U3;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Equal(Vector128<ulong>.Zero, ZeroVector128FullOpts());
        Assert.Equal(Vector256<ulong>.Zero, ZeroVector256FullOpts());
        Assert.Equal(Vector128<ulong>.Zero, ZeroVector128Tier0());
        Assert.Equal(Vector256<ulong>.Zero, ZeroVector256Tier0());
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector128<ulong> ZeroVector128FullOpts()
    {
        Unsafe.SkipInit(out Vector128<ulong> result);
        Unsafe.As<Vector128<ulong>, Struct128>(ref result) = default;
        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector256<ulong> ZeroVector256FullOpts()
    {
        Unsafe.SkipInit(out Vector256<ulong> result);
        Unsafe.As<Vector256<ulong>, Struct256>(ref result) = default;
        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector128<ulong> ZeroVector128Tier0()
    {
        Unsafe.SkipInit(out Vector128<ulong> result);
        Unsafe.As<Vector128<ulong>, Struct128>(ref result) = default;
        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector256<ulong> ZeroVector256Tier0()
    {
        Unsafe.SkipInit(out Vector256<ulong> result);
        Unsafe.As<Vector256<ulong>, Struct256>(ref result) = default;
        return result;
    }
}
