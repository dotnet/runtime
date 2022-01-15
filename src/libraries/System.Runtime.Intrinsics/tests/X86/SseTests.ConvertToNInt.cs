// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics.X86;
using Xunit;

namespace System.Runtime.Intrinsics.Tests.X86;

public sealed partial class SseTests
{
    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0b0000_0000, 0b0000_0000, 0)]
    public void ConvertToNInt_nint_64Bit(long lower, long upper, long expectedResult)
    {
        nint expectedNativeResult = (nint)expectedResult;

        Vector128<float> vector = Vector128.Create(lower, upper).AsSingle();
        nint actualResult = Sse.ConvertToNInt(vector);

        Assert.Equal(expectedNativeResult, actualResult);
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0b0000_0000, 0b0000_0000, 0)]
    public void ConvertToNInt_nint_32Bit(long lower, long upper, int expectedResult)
    {
        nint expectedNativeResult = expectedResult;

        Vector128<float> vector = Vector128.Create(lower, upper).AsSingle();
        nint actualResult = Sse.ConvertToNInt(vector);

        Assert.Equal(expectedNativeResult, actualResult);
    }
}
