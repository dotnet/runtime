// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics.X86;
using Xunit;

namespace System.Runtime.Intrinsics.Tests.X86;

public sealed partial class Sse2Tests
{
    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0)]
    [InlineData(255, 255, 255, 255, 0, 0)]
    [InlineData(240, 230, 230, 240, 22, 22)]
    [InlineData(255, 0, 255, 255, 0, 255)]
    [InlineData(-255, -255, -255, -255, 0, 0)]
    [InlineData(long.MaxValue, long.MinValue, long.MinValue, long.MaxValue, -1, -1)]
    public void Xor_nint_64Bit(long lowerLeft, long upperLeft, long lowerRight, long upperRight, long lowerExpected, long upperExpected)
    {
        Vector128<nint> left = Vector128.Create(lowerLeft, upperLeft).AsNInt();
        Vector128<nint> right = Vector128.Create(lowerRight, upperRight).AsNInt();
        Vector128<nint> expected = Vector128.Create(lowerExpected, upperExpected).AsNInt();

        Vector128<nint> actual = Sse2.Xor(left, right);
        Assert.Equal(expected, actual);
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0)]
    [InlineData(255, 255, 255, 255, 0, 0)]
    [InlineData(240, 230, 230, 240, 22, 22)]
    [InlineData(255, 0, 255, 255, 0, 255)]
    [InlineData(-255, -255, -255, -255, 0, 0)]
    public void Xor_nint_32Bit(int lowerLeft, int upperLeft, int lowerRight, int upperRight, int lowerExpected, int upperExpected)
    {
        Vector128<nint> left = Vector128.Create(lowerLeft, upperLeft).AsNInt();
        Vector128<nint> right = Vector128.Create(lowerRight, upperRight).AsNInt();
        Vector128<nint> expected = Vector128.Create(lowerExpected, upperExpected).AsNInt();

        Vector128<nint> actual = Sse2.Xor(left, right);
        Assert.Equal(expected, actual);
    }

    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0)]
    [InlineData(255, 255, 255, 255, 0, 0)]
    [InlineData(240, 230, 230, 240, 22, 22)]
    [InlineData(255, 0, 255, 255, 0, 255)]
    [InlineData(7, 64, 1, 0, 6, 64)]
    public void Xor_nuint_64Bit(ulong lowerLeft, ulong upperLeft, ulong lowerRight, ulong upperRight, ulong lowerExpected, ulong upperExpected)
    {
        Vector128<nuint> left = Vector128.Create(lowerLeft, upperLeft).AsNUInt();
        Vector128<nuint> right = Vector128.Create(lowerRight, upperRight).AsNUInt();
        Vector128<nuint> expected = Vector128.Create(lowerExpected, upperExpected).AsNUInt();

        Vector128<nuint> actual = Sse2.Xor(left, right);
        Assert.Equal(expected, actual);
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0)]
    [InlineData(255, 255, 255, 255, 0, 0)]
    [InlineData(240, 230, 230, 240, 22, 22)]
    [InlineData(255, 0, 255, 255, 0, 255)]
    [InlineData(7, 64, 1, 0, 6, 64)]
    public void Xor_nuint_32Bit(uint lowerLeft, uint upperLeft, uint lowerRight, uint upperRight, uint lowerExpected, uint upperExpected)
    {
        Vector128<nuint> left = Vector128.Create(lowerLeft, upperLeft).AsNUInt();
        Vector128<nuint> right = Vector128.Create(lowerRight, upperRight).AsNUInt();
        Vector128<nuint> expected = Vector128.Create(lowerExpected, upperExpected).AsNUInt();

        Vector128<nuint> actual = Sse2.Xor(left, right);
        Assert.Equal(expected, actual);
    }
}
