// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics.X86;
using Xunit;

namespace System.Runtime.Intrinsics.Tests.X86;

public sealed partial class Sse42Tests
{
    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0, 0)]
    [InlineData(1234, 1234, 0)]
    [InlineData(1234, 4321, 1804194867)]
    [InlineData(ulong.MinValue, ulong.MaxValue, 3293575501)]
    [InlineData(ulong.MaxValue, ulong.MinValue, 1943489909)]
    public void Crc32_nuint_64Bit(ulong crc, ulong data, ulong expectedResult)
    {
        nuint nativeCrc = (nuint)crc;
        nuint nativeData = (nuint)data;
        nuint nativeExpectedResult = (nuint)expectedResult;

        nuint actualResult = Sse42.Crc32(nativeCrc, nativeData);
        Assert.Equal(nativeExpectedResult, actualResult);
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0, 0, 0)]
    [InlineData(1234, 1234, 0)]
    [InlineData(1234, 4321, 2988438815)]
    [InlineData(uint.MinValue, uint.MaxValue, 3080238136)]
    [InlineData(uint.MaxValue, uint.MinValue, 3080238136)]
    public void Crc32_nuint_32Bit(uint crc, uint data, uint expectedResult)
    {
        nuint nativeCrc = crc;
        nuint nativeData = data;
        nuint nativeExpectedResult = expectedResult;

        nuint actualResult = Sse42.Crc32(nativeCrc, nativeData);
        Assert.Equal(nativeExpectedResult, actualResult);
    }
}
