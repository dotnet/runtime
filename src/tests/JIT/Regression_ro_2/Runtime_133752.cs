// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

public class Runtime_133752
{
    [Fact]
    public static void TestVector128()
    {
        Assert.Equal(
            Vector128.Create(uint.MaxValue, 3u, 1u, 1u),
            Divide128(Vector128.Create(uint.MaxValue, 9u, 1u, 1u), Vector128.Create(1u, 3u, 1u, 1u)));

        Assert.Equal(
            Vector128.Create(0x80000001u, 0x7FFFFFFFu, 3u, 0x80000000u),
            Divide128(Vector128.Create(0x80000001u, 0x7FFFFFFFu, 9u, 0x80000000u), Vector128.Create(1u, 1u, 3u, 1u)));
    }

    [Fact]
    public static void TestVector256()
    {
        Assert.Equal(
            Vector256.Create(uint.MaxValue, 3u, 1u, 1u, 1u, uint.MaxValue, 3000000000u, 4u),
            Divide256(
                Vector256.Create(uint.MaxValue, 9u, 1u, 1u, 7u, uint.MaxValue, 3000000000u, 8u),
                Vector256.Create(1u, 3u, 1u, 1u, 7u, 1u, 1u, 2u)));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector128<uint> Divide128(Vector128<uint> left, Vector128<uint> right) => left / right;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector256<uint> Divide256(Vector256<uint> left, Vector256<uint> right) => left / right;
}
