// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics.X86;
using Xunit;

namespace System.Runtime.Intrinsics.Tests.X86;

public sealed partial class Sse3Tests
{
    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0)]
    [InlineData(1, 123)]
    [InlineData(long.MaxValue, long.MinValue)]
    [InlineData(long.MaxValue, long.MaxValue)]
    public void LoadDquVector128_nint_64Bit(long lower, long upper)
    {
        Span<nint> span = stackalloc nint[2];
        span[0] = (nint)lower;
        span[1] = (nint)upper;

        unsafe
        {
            fixed (nint* ptr = &span[0])
            {
                Vector128<nint> vector = Sse3.LoadDquVector128(ptr);
                Assert.Equal(span[0], vector[0]);
                Assert.Equal(span[1], vector[1]);
            }
        }
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0, 0)]
    [InlineData(1, 123)]
    [InlineData(int.MaxValue, int.MinValue)]
    [InlineData(int.MaxValue, int.MaxValue)]
    public void LoadDquVector128_nint_32Bit(int lower, int upper)
    {
        Span<nint> span = stackalloc nint[2];
        span[0] = lower;
        span[1] = upper;

        unsafe
        {
            fixed (nint* ptr = &span[0])
            {
                Vector128<nint> vector = Sse3.LoadDquVector128(ptr);
                Assert.Equal(span[0], vector[0]);
                Assert.Equal(span[1], vector[1]);
            }
        }
    }

    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0)]
    [InlineData(1, 123)]
    [InlineData(ulong.MaxValue, ulong.MinValue)]
    [InlineData(ulong.MaxValue, ulong.MaxValue)]
    public void LoadDquVector128_nuint_64Bit(ulong lower, ulong upper)
    {
        Span<nuint> span = stackalloc nuint[2];
        span[0] = (nuint)lower;
        span[1] = (nuint)upper;

        unsafe
        {
            fixed (nuint* ptr = &span[0])
            {
                Vector128<nuint> vector = Sse3.LoadDquVector128(ptr);
                Assert.Equal(span[0], vector[0]);
                Assert.Equal(span[1], vector[1]);
            }
        }
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0, 0)]
    [InlineData(1, 123)]
    [InlineData(uint.MaxValue, uint.MinValue)]
    [InlineData(uint.MaxValue, uint.MaxValue)]
    public void LoadDquVector128_nuint_32Bit(uint lower, uint upper)
    {
        Span<nuint> span = stackalloc nuint[2];
        span[0] = lower;
        span[1] = upper;

        unsafe
        {
            fixed (nuint* ptr = &span[0])
            {
                Vector128<nuint> vector = Sse3.LoadDquVector128(ptr);
                Assert.Equal(span[0], vector[0]);
                Assert.Equal(span[1], vector[1]);
            }
        }
    }
}
