// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics.X86;
using Xunit;

namespace System.Runtime.Intrinsics.Tests.X86;

public sealed partial class Avx2Tests
{
    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0)]
    [InlineData(long.MinValue, long.MaxValue)]
    [InlineData(long.MaxValue, long.MinValue)]
    public void BroadcastVector128ToVector256_nint_64Bit(long left, long right)
    {
        Span<nint> source = stackalloc nint[2];
        source[0] = (nint)left;
        source[1] = (nint)right;

        unsafe
        {
            fixed (nint* ptr = &source[0])
            {
                Vector256<nint> result = Avx2.BroadcastVector128ToVector256(ptr);
                Assert.Equal(left, result[0]);
                Assert.Equal(right, result[1]);
                Assert.Equal(left, result[2]);
                Assert.Equal(right, result[3]);
            }
        }
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0, 0)]
    [InlineData(int.MinValue, int.MaxValue)]
    [InlineData(int.MaxValue, int.MinValue)]
    public void BroadcastVector128ToVector256_nint_32Bit(int left, int right)
    {
        Span<nint> source = stackalloc nint[2];
        source[0] = left;
        source[1] = right;

        unsafe
        {
            fixed (nint* ptr = &source[0])
            {
                Vector256<nint> result = Avx2.BroadcastVector128ToVector256(ptr);
                Assert.Equal(left, result[0]);
                Assert.Equal(right, result[1]);
                Assert.Equal(left, result[2]);
                Assert.Equal(right, result[3]);
            }
        }
    }

    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0)]
    [InlineData(ulong.MinValue, ulong.MaxValue)]
    [InlineData(ulong.MaxValue, ulong.MinValue)]
    public void BroadcastVector128ToVector256_nuint_64Bit(ulong left, ulong right)
    {
        Span<nuint> source = stackalloc nuint[2];
        source[0] = (nuint)left;
        source[1] = (nuint)right;

        unsafe
        {
            fixed (nuint* ptr = &source[0])
            {
                Vector256<nuint> result = Avx2.BroadcastVector128ToVector256(ptr);
                Assert.Equal(left, result[0]);
                Assert.Equal(right, result[1]);
                Assert.Equal(left, result[2]);
                Assert.Equal(right, result[3]);
            }
        }
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0, 0)]
    [InlineData(uint.MinValue, uint.MaxValue)]
    [InlineData(uint.MaxValue, uint.MinValue)]
    public void BroadcastVector128ToVector256_nuint_32Bit(uint left, uint right)
    {
        Span<nuint> source = stackalloc nuint[2];
        source[0] = left;
        source[1] = right;

        unsafe
        {
            fixed (nuint* ptr = &source[0])
            {
                Vector256<nuint> result = Avx2.BroadcastVector128ToVector256(ptr);
                Assert.Equal(left, result[0]);
                Assert.Equal(right, result[1]);
                Assert.Equal(left, result[2]);
                Assert.Equal(right, result[3]);
            }
        }
    }
}
