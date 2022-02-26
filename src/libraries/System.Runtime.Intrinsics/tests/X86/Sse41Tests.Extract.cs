// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics.X86;
using Xunit;

namespace System.Runtime.Intrinsics.Tests.X86;

public sealed partial class Sse41Tests
{
    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(1, 0, 0, 1)]
    [InlineData(0, 123, 1, 0)]
    [InlineData(321, 123, 0, 321)]
    public void Extract_nint_64Bit(long lower, long upper, byte index, long expectedResult)
    {
        Vector128<nint> vector = Vector128.Create(lower, upper).AsNInt();
        nint nativeExpectedResult = (nint)expectedResult;

        nint actualResult = Sse41.Extract(vector, index);
        Assert.Equal(nativeExpectedResult, actualResult);
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(1, 0, 0, 1)]
    [InlineData(0, 123, 1, 0)]
    [InlineData(321, 123, 0, 321)]
    public void Extract_nint_32Bit(int lower, int upper, byte index, int expectedResult)
    {
        Vector128<nint> vector = Vector128.Create(lower, upper).AsNInt();
        nint nativeExpectedResult = expectedResult;

        nint actualResult = Sse41.Extract(vector, index);
        Assert.Equal(nativeExpectedResult, actualResult);
    }

    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(1, 0, 0, 1)]
    [InlineData(0, 123, 1, 123)]
    [InlineData(321, 123, 0, 321)]
    public void Extract_nuint_64Bit(ulong lower, ulong upper, byte index, ulong expectedResult)
    {
        Vector128<nuint> vector = Vector128.Create(lower, upper).AsNUInt();
        nuint nativeExpectedResult = (nuint)expectedResult;

        nuint actualResult = Sse41.Extract(vector, index);
        Assert.Equal(nativeExpectedResult, actualResult);
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(1, 0, 0, 1)]
    [InlineData(0, 123, 1, 0)]
    [InlineData(321, 123, 0, 321)]
    public void Extract_nuint_32Bit(uint lower, uint upper, byte index, uint expectedResult)
    {
        Vector128<nuint> vector = Vector128.Create(lower, upper).AsNUInt();
        nuint nativeExpectedResult = expectedResult;

        nuint actualResult = Sse41.Extract(vector, index);
        Assert.Equal(nativeExpectedResult, actualResult);
    }
}
