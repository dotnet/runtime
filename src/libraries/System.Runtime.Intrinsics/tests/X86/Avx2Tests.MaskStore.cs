// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics.X86;
using Xunit;

namespace System.Runtime.Intrinsics.Tests.X86;

public partial class Avx2Tests
{
    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0)]
    [InlineData(10, 0, 0, 0, 0, 0)]
    [InlineData(0, long.MaxValue, long.MinValue, long.MaxValue, 0, 0)]
    public void MaskStore_128_nint_64Bit(long left, long right, long maskLeft, long maskRight, long expectedLeft,
        long expectedRight)
    {
        Span<nint> actualData = stackalloc nint[2];

        var source = Vector128.Create(left, right).AsNInt();
        var mask = Vector128.Create(maskLeft, maskRight).AsNInt();
        var expected = Vector128.Create(expectedLeft, expectedRight).AsNInt();

        unsafe
        {
            fixed (nint* actualPtr = &actualData[0])
            {
                Avx2.MaskStore(actualPtr, mask, source);

                var actual = Sse2.LoadVector128(actualPtr);
                Assert.Equal(expected, actual);
            }
        }
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0)]
    [InlineData(10, 0, 0, 0, 0, 0)]
    [InlineData(0, int.MaxValue, int.MinValue, int.MaxValue, 0, 0)]
    public void MaskStore_128_nint_32Bit(int left, int right, int maskLeft, int maskRight, int expectedLeft,
        int expectedRight)
    {
        Span<nint> actualData = stackalloc nint[2];

        var source = Vector128.Create(left, right).AsNInt();
        var mask = Vector128.Create(maskLeft, maskRight).AsNInt();
        var expected = Vector128.Create(expectedLeft, expectedRight).AsNInt();

        unsafe
        {
            fixed (nint* actualPtr = &actualData[0])
            {
                Avx2.MaskStore(actualPtr, mask, source);

                var actual = Sse2.LoadVector128(actualPtr);
                Assert.Equal(expected, actual);
            }
        }
    }
    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0)]
    [InlineData(10, 0, 0, 0, 0, 0)]
    [InlineData(0, ulong.MaxValue, 0, ulong.MaxValue, 0, ulong.MaxValue)]
    public void MaskStore_128_nuint_64Bit(ulong left, ulong right, ulong maskLeft, ulong maskRight, ulong expectedLeft,
        ulong expectedRight)
    {
        Span<nuint> actualData = stackalloc nuint[2];

        var source = Vector128.Create(left, right).AsNUInt();
        var mask = Vector128.Create(maskLeft, maskRight).AsNUInt();
        var expected = Vector128.Create(expectedLeft, expectedRight).AsNUInt();

        unsafe
        {
            fixed (nuint* actualPtr = &actualData[0])
            {
                Avx2.MaskStore(actualPtr, mask, source);

                var actual = Sse2.LoadVector128(actualPtr);
                Assert.Equal(expected, actual);
            }
        }
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0)]
    [InlineData(10, 0, 0, 0, 0, 0)]
    [InlineData(0, uint.MaxValue, 0, uint.MaxValue, 0, uint.MaxValue)]
    public void MaskStore_128_nuint_32Bit(uint left, uint right, uint maskLeft, uint maskRight, uint expectedLeft,
        uint expectedRight)
    {
        Span<nuint> actualData = stackalloc nuint[2];

        var source = Vector128.Create(left, right).AsNUInt();
        var mask = Vector128.Create(maskLeft, maskRight).AsNUInt();
        var expected = Vector128.Create(expectedLeft, expectedRight).AsNUInt();

        unsafe
        {
            fixed (nuint* actualPtr = &actualData[0])
            {
                Avx2.MaskStore(actualPtr, mask, source);

                var actual = Sse2.LoadVector128(actualPtr);
                Assert.Equal(expected, actual);
            }
        }
    }

    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)]
    [InlineData(10, 0, 0, 0, 0, 0, 10, 0, 0, 0, 0, 0)]
    [InlineData(0, long.MaxValue, long.MinValue, long.MaxValue, 0, 0, 0, long.MaxValue, 0, 0, 0, 0)]
    public void MaskStore_256_nint_64Bit(long value1, long value2, long value3, long value4, long mask1, long mask2, long mask3, long mask4, long expected1, long expected2, long expected3, long expected4)
    {
        Span<nint> actualData = stackalloc nint[4];

        var source = Vector256.Create(value1, value2, value3, value4).AsNInt();
        var mask = Vector256.Create(mask1, mask2, mask3, mask4).AsNInt();
        var expected = Vector256.Create(expected1, expected2, expected3, expected4).AsNInt();

        unsafe
        {
            fixed (nint* actualPtr = &actualData[0])
            {
                Avx2.MaskStore(actualPtr, mask, source);

                var actual = Avx.LoadVector256(actualPtr);
                Assert.Equal(expected, actual);
            }
        }
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)]
    [InlineData(10, 0, 0, 0, 0, 0, 10, 0, 0, 0, 0, 0)]
    [InlineData(0, int.MaxValue, int.MinValue, int.MaxValue, 0, 0, 0, int.MaxValue, 0, 0, 0, 0)]
    public void MaskStore_256_nint_32Bit(int value1, int value2, int value3, int value4, int mask1, int mask2, int mask3, int mask4, int expected1, int expected2, int expected3, int expected4)
    {
        Span<nint> actualData = stackalloc nint[4];

        var source = Vector256.Create(value1, value2, value3, value4).AsNInt();
        var mask = Vector256.Create(mask1, mask2, mask3, mask4).AsNInt();
        var expected = Vector256.Create(expected1, expected2, expected3, expected4).AsNInt();

        unsafe
        {
            fixed (nint* actualPtr = &actualData[0])
            {
                Avx2.MaskStore(actualPtr, mask, source);

                var actual = Avx.LoadVector256(actualPtr);
                Assert.Equal(expected, actual);
            }
        }
    }

    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)]
    [InlineData(10, 0, 0, 0, 0, 0, 10, 0, 0, 0, 0, 0)]
    [InlineData(0, ulong.MaxValue, ulong.MinValue, ulong.MaxValue, 0, 0, 0, ulong.MaxValue, 0, 0, 0, ulong.MaxValue)]
    public void MaskStore_256_nuint_64Bit(ulong value1, ulong value2, ulong value3, ulong value4, ulong mask1, ulong mask2, ulong mask3, ulong mask4, ulong expected1, ulong expected2, ulong expected3, ulong expected4)
    {
        Span<nuint> actualData = stackalloc nuint[4];

        var source = Vector256.Create(value1, value2, value3, value4).AsNUInt();
        var mask = Vector256.Create(mask1, mask2, mask3, mask4).AsNUInt();
        var expected = Vector256.Create(expected1, expected2, expected3, expected4).AsNUInt();

        unsafe
        {
            fixed (nuint* actualPtr = &actualData[0])
            {
                Avx2.MaskStore(actualPtr, mask, source);

                var actual = Avx.LoadVector256(actualPtr);
                Assert.Equal(expected, actual);
            }
        }
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)]
    [InlineData(10, 0, 0, 0, 0, 0, 10, 0, 0, 0, 0, 0)]
    [InlineData(0, uint.MaxValue, uint.MinValue, uint.MaxValue, 0, 0, 0, uint.MaxValue, 0, 0, 0, uint.MaxValue)]
    public void MaskStore_256_nuint_32Bit(uint value1, uint value2, uint value3, uint value4, uint mask1, uint mask2, uint mask3, uint mask4, uint expected1, uint expected2, uint expected3, uint expected4)
    {
        Span<nuint> actualData = stackalloc nuint[4];

        var source = Vector256.Create(value1, value2, value3, value4).AsNUInt();
        var mask = Vector256.Create(mask1, mask2, mask3, mask4).AsNUInt();
        var expected = Vector256.Create(expected1, expected2, expected3, expected4).AsNUInt();

        unsafe
        {
            fixed (nuint* actualPtr = &actualData[0])
            {
                Avx2.MaskStore(actualPtr, mask, source);

                var actual = Avx.LoadVector256(actualPtr);
                Assert.Equal(expected, actual);
            }
        }
    }
}
