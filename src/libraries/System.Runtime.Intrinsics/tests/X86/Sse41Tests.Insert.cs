// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics.X86;
using Xunit;

namespace System.Runtime.Intrinsics.Tests.X86;

public sealed partial class Sse41Tests
{
    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0)]
    [InlineData(0, 0, 1, 0, 1, 0)]
    [InlineData(0, 1, 1, 0, 1, 1)]
    [InlineData(0, 1, 123, 0, 123, 1)]
    [InlineData(0, 1, 123, 1, 0, 123)]
    public void Insert_nint_64Bit(long lower, long upper, long data, byte index, long expectedLower, long expectedUpper)
    {
        Vector128<nint> vector = Vector128.Create(lower, upper).AsNInt();
        Vector128<nint> expectedVector = Vector128.Create(expectedLower, expectedUpper).AsNInt();

        Vector128<nint> actualVector = Sse41.Insert(vector, (nint)data, index);
        Assert.Equal(expectedVector, actualVector);
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0)]
    [InlineData(0, 0, 1, 0, 1, 0)]
    [InlineData(0, 1, 1, 0, 1, 1)]
    [InlineData(0, 1, 123, 0, 123, 1)]
    [InlineData(0, 1, 123, 1, 0, 123)]
    public void Insert_nint_32Bit(int lower, int upper, int data, byte index, int expectedLower, int expectedUpper)
    {
        Vector128<nint> vector = Vector128.Create(lower, upper).AsNInt();
        Vector128<nint> expectedVector = Vector128.Create(expectedLower, expectedUpper).AsNInt();

        Vector128<nint> actualVector = Sse41.Insert(vector, data, index);
        Assert.Equal(expectedVector, actualVector);
    }

    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0)]
    [InlineData(0, 0, 1, 0, 1, 0)]
    [InlineData(0, 1, 1, 0, 1, 1)]
    [InlineData(0, 1, 123, 0, 123, 1)]
    [InlineData(0, 1, 123, 1, 0, 123)]
    public void Insert_nuint_64Bit(ulong lower, ulong upper, ulong data, byte index, ulong expectedLower, ulong expectedUpper)
    {
        Vector128<nuint> vector = Vector128.Create(lower, upper).AsNUInt();
        Vector128<nuint> expectedVector = Vector128.Create(expectedLower, expectedUpper).AsNUInt();

        Vector128<nuint> actualVector = Sse41.Insert(vector, (nuint)data, index);
        Assert.Equal(expectedVector, actualVector);
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0)]
    [InlineData(0, 0, 1, 0, 1, 0)]
    [InlineData(0, 1, 1, 0, 1, 1)]
    [InlineData(0, 1, 123, 0, 123, 1)]
    [InlineData(0, 1, 123, 1, 0, 123)]
    public void Insert_nuint_32Bit(uint lower, uint upper, uint data, byte index, uint expectedLower, uint expectedUpper)
    {
        Vector128<nuint> vector = Vector128.Create(lower, upper).AsNUInt();
        Vector128<nuint> expectedVector = Vector128.Create(expectedLower, expectedUpper).AsNUInt();

        Vector128<nuint> actualVector = Sse41.Insert(vector, data, index);
        Assert.Equal(expectedVector, actualVector);
    }
}
