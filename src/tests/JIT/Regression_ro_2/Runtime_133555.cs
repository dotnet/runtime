// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

public class Runtime_133555
{
    [Fact]
    public static void TestEntryPoint()
    {
        Assert.True(Equal(Vector128<byte>.AllBitsSet, Vector128<byte>.Zero, Vector128<long>.AllBitsSet));
        Assert.False(Equal(Vector128<byte>.AllBitsSet, Vector128<byte>.Zero, Vector128<long>.Zero));
    }

    [Fact]
    public static void TestConstantComparison()
    {
        Assert.True(EqualConstant(Vector128.Create(-1L, 0L).AsByte(), Vector128<byte>.Zero));
        Assert.False(EqualConstant(Vector128<byte>.AllBitsSet, Vector128<byte>.Zero));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Equal(Vector128<byte> left, Vector128<byte> right, Vector128<long> other) =>
        Vector128.GreaterThan(left, right).AsInt64() == other;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool EqualConstant(Vector128<byte> left, Vector128<byte> right) =>
        Vector128.GreaterThan(left, right).AsInt64() == Vector128.Create(-1L, 0L);
}
