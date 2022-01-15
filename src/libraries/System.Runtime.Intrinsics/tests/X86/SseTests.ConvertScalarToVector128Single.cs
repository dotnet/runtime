// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics.X86;
using Xunit;

namespace System.Runtime.Intrinsics.Tests.X86;

public sealed partial class SseTests
{
    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0b0000_0000, 0b0000_0000, 0, 0b0000_0000, 0b0000_0000)]
    public void ConvertScalarToVector128Single_nint_64Bit(long lower, long upper, long value, long expectedLower, long expectedUpper)
    {
        Vector128<float> expectedResult = Vector128.Create(expectedLower, expectedUpper).AsSingle();
        nint nativeValue = (nint)value;

        Vector128<float> vector = Vector128.Create(lower, upper).AsSingle();
        Vector128<float> actualResult = Sse.ConvertScalarToVector128Single(vector, nativeValue);

        Assert.Equal(expectedResult, actualResult);
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0b0000_0000, 0b0000_0000, 0, 0b0000_0000, 0b0000_0000)]
    public void ConvertScalarToVector128Single_nint_32Bit(long lower, long upper, int value, long expectedLower, long expectedUpper)
    {
        Vector128<float> expectedResult = Vector128.Create(expectedLower, expectedUpper).AsSingle();

        Vector128<float> vector = Vector128.Create(lower, upper).AsSingle();
        Vector128<float> actualResult = Sse.ConvertScalarToVector128Single(vector, value);

        Assert.Equal(expectedResult, actualResult);
    }
}
