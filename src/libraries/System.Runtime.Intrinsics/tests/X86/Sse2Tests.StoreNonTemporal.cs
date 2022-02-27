// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics.X86;
using Xunit;

namespace System.Runtime.Intrinsics.Tests.X86;

public sealed partial class Sse2Tests
{
    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0)]
    [InlineData(12345678)]
    [InlineData(long.MinValue)]
    [InlineData(long.MaxValue)]
    public void StoreNonTemporal_nint_64Bit(long value)
    {
        Span<nint> span = stackalloc nint[2];
        unsafe
        {
            fixed (nint* ptr = &span[0])
            {
                nint nativeValue = (nint)value;
                Sse2.StoreNonTemporal(ptr, nativeValue);
                Assert.Equal(nativeValue, span[0]);
            }

            fixed (nint* ptr = &span[1])
            {
                nint nativeValue = (nint)value;
                Sse2.StoreNonTemporal(ptr, nativeValue);
                Assert.Equal(nativeValue, span[1]);
            }
        }
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0)]
    [InlineData(12345678)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void StoreNonTemporal_nint_32Bit(int value)
    {
        Span<nint> span = stackalloc nint[2];
        unsafe
        {
            fixed (nint* ptr = &span[0])
            {
                nint nativeValue = value;
                Sse2.StoreNonTemporal(ptr, nativeValue);
                Assert.Equal(nativeValue, span[0]);
            }

            fixed (nint* ptr = &span[1])
            {
                nint nativeValue = value;
                Sse2.StoreNonTemporal(ptr, nativeValue);
                Assert.Equal(nativeValue, span[1]);
            }
        }
    }

    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0)]
    [InlineData(12345678)]
    [InlineData(ulong.MinValue)]
    [InlineData(ulong.MaxValue)]
    public void StoreNonTemporal_nuint_64Bit(ulong value)
    {
        Span<nuint> span = stackalloc nuint[2];
        unsafe
        {
            fixed (nuint* ptr = &span[0])
            {
                nuint nativeValue = (nuint)value;
                Sse2.StoreNonTemporal(ptr, nativeValue);
                Assert.Equal(nativeValue, span[0]);
            }

            fixed (nuint* ptr = &span[1])
            {
                nuint nativeValue = (nuint)value;
                Sse2.StoreNonTemporal(ptr, nativeValue);
                Assert.Equal(nativeValue, span[1]);
            }
        }
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0)]
    [InlineData(12345678)]
    [InlineData(uint.MinValue)]
    [InlineData(uint.MaxValue)]
    public void StoreNonTemporal_nuint_32Bit(uint value)
    {
        Span<nuint> span = stackalloc nuint[2];
        unsafe
        {
            fixed (nuint* ptr = &span[0])
            {
                nuint nativeValue = value;
                Sse2.StoreNonTemporal(ptr, nativeValue);
                Assert.Equal(nativeValue, span[0]);
            }

            fixed (nuint* ptr = &span[1])
            {
                nuint nativeValue = value;
                Sse2.StoreNonTemporal(ptr, nativeValue);
                Assert.Equal(nativeValue, span[1]);
            }
        }
    }
}
