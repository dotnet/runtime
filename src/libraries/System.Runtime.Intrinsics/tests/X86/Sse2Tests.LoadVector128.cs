// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics.X86;
using Xunit;

namespace System.Runtime.Intrinsics.Tests.X86;

public sealed partial class Sse2Tests
{
    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0)]
    [InlineData(12345678, 87654321)]
    [InlineData(long.MinValue, long.MaxValue)]
    [InlineData(long.MaxValue, long.MinValue)]
    public void LoadVector128_nint_64Bit(long firstValue, long secondValue)
    {
        Span<nint> span = stackalloc nint[2];
        span[0] = (nint)firstValue;
        span[1] = (nint)secondValue;
        unsafe
        {
            fixed (nint* ptr = &span[0])
            {
                Vector128<nint> actualVector = Sse2.LoadVector128(ptr);
                Assert.Equal(firstValue, actualVector[0]);
                Assert.Equal(secondValue, actualVector[1]);
            }
        }
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0, 0)]
    [InlineData(12345678, 87654321)]
    [InlineData(int.MinValue, int.MaxValue)]
    [InlineData(int.MaxValue, int.MinValue)]
    public void LoadVector128_nint_32Bit(int firstValue, int secondValue)
    {
        Span<nint> span = stackalloc nint[2];
        span[0] = firstValue;
        span[1] = secondValue;
        unsafe
        {
            fixed (nint* ptr = &span[0])
            {
                Vector128<nint> actualVector = Sse2.LoadVector128(ptr);
                Assert.Equal(firstValue, actualVector[0]);
                Assert.Equal(secondValue, actualVector[1]);
            }
        }
    }

    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0)]
    [InlineData(12345678, 87654321)]
    [InlineData(ulong.MinValue, ulong.MaxValue)]
    [InlineData(ulong.MaxValue, ulong.MinValue)]
    public void LoadVector128_nuint_64Bit(ulong firstValue, ulong secondValue)
    {
        Span<nuint> span = stackalloc nuint[2];
        span[0] = (nuint)firstValue;
        span[1] = (nuint)secondValue;
        unsafe
        {
            fixed (nuint* ptr = &span[0])
            {
                Vector128<nuint> actualVector = Sse2.LoadVector128(ptr);
                Assert.Equal(firstValue, actualVector[0]);
                Assert.Equal(secondValue, actualVector[1]);
            }
        }
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0, 0)]
    [InlineData(12345678, 87654321)]
    [InlineData(uint.MinValue, uint.MaxValue)]
    [InlineData(uint.MaxValue, uint.MinValue)]
    public void LoadVector128_nuint_32Bit(uint firstValue, uint secondValue)
    {
        Span<nuint> span = stackalloc nuint[2];
        span[0] = firstValue;
        span[1] = secondValue;
        unsafe
        {
            fixed (nuint* ptr = &span[0])
            {
                Vector128<nuint> actualVector = Sse2.LoadVector128(ptr);
                Assert.Equal(firstValue, actualVector[0]);
                Assert.Equal(secondValue, actualVector[1]);
            }
        }
    }
}
