// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics.X86;
using Xunit;

namespace System.Runtime.Intrinsics.Tests.X86;

public sealed partial class Sse41Tests
{
    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0)]
    [InlineData(123, 0)]
    [InlineData(123, 123)]
    [InlineData(long.MaxValue, long.MinValue)]
    [InlineData(long.MinValue, long.MaxValue)]
    public void LoadAlignedVector128NonTemporal_nint_64Bit(long left, long right)
    {
        Vector128<nint> expectedVector = Vector128.Create(left, right).AsNInt();

        Span<nint> span = stackalloc nint[2];
        span[0] = (nint)left;
        span[1] = (nint)right;

        unsafe
        {
            fixed (nint* ptr = &span[0])
            {
                Vector128<nint> actualVector = Sse41.LoadAlignedVector128NonTemporal(ptr);
                Assert.Equal(expectedVector, actualVector);
            }
        }
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0, 0)]
    [InlineData(123, 0)]
    [InlineData(123, 123)]
    [InlineData(int.MaxValue, int.MinValue)]
    [InlineData(int.MinValue, int.MaxValue)]
    public void LoadAlignedVector128NonTemporal_nint_32Bit(int left, int right)
    {
        Vector128<nint> expectedVector = Vector128.Create(left, right).AsNInt();

        Span<nint> span = stackalloc nint[2];
        span[0] = left;
        span[1] = right;

        unsafe
        {
            fixed (nint* ptr = &span[0])
            {
                Vector128<nint> actualVector = Sse41.LoadAlignedVector128NonTemporal(ptr);
                Assert.Equal(expectedVector, actualVector);
            }
        }
    }

    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0)]
    [InlineData(123, 0)]
    [InlineData(123, 123)]
    [InlineData(ulong.MaxValue, ulong.MinValue)]
    [InlineData(ulong.MinValue, ulong.MaxValue)]
    public void LoadAlignedVector128NonTemporal_nuint_64Bit(ulong left, ulong right)
    {
        Vector128<nuint> expectedVector = Vector128.Create(left, right).AsNUInt();

        Span<nuint> span = stackalloc nuint[2];
        span[0] = (nuint)left;
        span[1] = (nuint)right;

        unsafe
        {
            fixed (nuint* ptr = &span[0])
            {
                Vector128<nuint> actualVector = Sse41.LoadAlignedVector128NonTemporal(ptr);
                Assert.Equal(expectedVector, actualVector);
            }
        }
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0, 0)]
    [InlineData(123, 0)]
    [InlineData(123, 123)]
    [InlineData(uint.MaxValue, uint.MinValue)]
    [InlineData(uint.MinValue, uint.MaxValue)]
    public void LoadAlignedVector128NonTemporal_nuint_32Bit(uint left, uint right)
    {
        Vector128<nuint> expectedVector = Vector128.Create(left, right).AsNUInt();

        Span<nuint> span = stackalloc nuint[2];
        span[0] = left;
        span[1] = right;

        unsafe
        {
            fixed (nuint* ptr = &span[0])
            {
                Vector128<nuint> actualVector = Sse41.LoadAlignedVector128NonTemporal(ptr);
                Assert.Equal(expectedVector, actualVector);
            }
        }
    }
}
