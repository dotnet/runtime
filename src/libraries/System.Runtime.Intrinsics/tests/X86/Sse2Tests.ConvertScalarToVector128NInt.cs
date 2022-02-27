// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics.X86;
using Xunit;

namespace System.Runtime.Intrinsics.Tests.X86;

public sealed partial class Sse3Tests
{
    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0, 0)]
    [InlineData(-123, 4294967173, 0)]
    [InlineData(123, 123, 0)]
    [InlineData(long.MinValue, 0, 0)]
    [InlineData(long.MaxValue, 4294967295, 0)]
    public void ConvertScalarToVector128NInt_nint_64Bit(long value, long expectedLower, long expectedUpper)
    {
        Vector128<nint> expected = Vector128.Create(expectedLower, expectedUpper).AsNInt();

        Vector128<nint> actual = Sse2.ConvertScalarToVector128NInt((nint)value).AsNInt();
        Assert.Equal(expected, actual);
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0, 0, 0)]
    [InlineData(-123, 4294967173, 0)]
    [InlineData(123, 123, 0)]
    public void ConvertScalarToVector128NInt_nint_32Bit(int value, int expectedLower, int expectedUpper)
    {
        Vector128<nint> expected = Vector128.Create(expectedLower, expectedUpper).AsNInt();

        Vector128<nint> actual = Sse2.ConvertScalarToVector128NInt(value).AsNInt();
        Assert.Equal(expected, actual);
    }
}
