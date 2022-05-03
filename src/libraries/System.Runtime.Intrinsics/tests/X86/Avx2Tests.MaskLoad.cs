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
    public void MaskLoad_128_nint_64Bit(long left, long right, long maskLeft, long maskRight, long expectedLeft,
        long expectedRight)
    {
        Span<nint> value = stackalloc nint[2];
        value[0] = (nint)left;
        value[1] = (nint)right;

        var mask = Vector128.Create(maskLeft, maskRight).AsNInt();
        var expected = Vector128.Create(expectedLeft, expectedRight).AsNInt();

        unsafe
        {
            fixed (nint* valuePtr = &value[0])
            {
                var actual = Avx2.MaskLoad(valuePtr, mask);
                Assert.Equal(expected, actual);
            }
        }
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0)]
    [InlineData(10, 0, 0, 0, 0, 0)]
    [InlineData(0, int.MaxValue, int.MinValue, int.MaxValue, 0, 0)]
    public void MaskLoad_128_nint_32Bit(int left, int right, int maskLeft, int maskRight, int expectedLeft,
        int expectedRight)
    {
        Span<nint> value = stackalloc nint[2];
        value[0] = left;
        value[1] = right;

        var mask = Vector128.Create(maskLeft, maskRight).AsNInt();
        var expected = Vector128.Create(expectedLeft, expectedRight).AsNInt();

        unsafe
        {
            fixed (nint* valuePtr = &value[0])
            {
                var actual = Avx2.MaskLoad(valuePtr, mask);
                Assert.Equal(expected, actual);
            }
        }
    }
    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0)]
    [InlineData(10, 0, 0, 0, 0, 0)]
    [InlineData(0, ulong.MaxValue, 0, ulong.MaxValue, 0, ulong.MaxValue)]
    public void MaskLoad_128_nuint_64Bit(ulong left, ulong right, ulong maskLeft, ulong maskRight, ulong expectedLeft,
        ulong expectedRight)
    {
        Span<nuint> value = stackalloc nuint[2];
        value[0] = (nuint)left;
        value[1] = (nuint)right;

        var mask = Vector128.Create(maskLeft, maskRight).AsNUInt();
        var expected = Vector128.Create(expectedLeft, expectedRight).AsNUInt();

        unsafe
        {
            fixed (nuint* valuePtr = &value[0])
            {
                var actual = Avx2.MaskLoad(valuePtr, mask);
                Assert.Equal(expected, actual);
            }
        }
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0)]
    [InlineData(10, 0, 0, 0, 0, 0)]
    [InlineData(0, uint.MaxValue, 0, uint.MaxValue, 0, uint.MaxValue)]
    public void MaskLoad_128_nuint_32Bit(uint left, uint right, uint maskLeft, uint maskRight, uint expectedLeft,
        uint expectedRight)
    {
        Span<nuint> value = stackalloc nuint[2];
        value[0] = left;
        value[1] = right;

        var mask = Vector128.Create(maskLeft, maskRight).AsNUInt();
        var expected = Vector128.Create(expectedLeft, expectedRight).AsNUInt();

        unsafe
        {
            fixed (nuint* valuePtr = &value[0])
            {
                var actual = Avx2.MaskLoad(valuePtr, mask);
                Assert.Equal(expected, actual);
            }
        }
    }

    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)]
    [InlineData(10, 0, 0, 0, 0, 0, 10, 0, 0, 0, 0, 0)]
    [InlineData(0, long.MaxValue, long.MinValue, long.MaxValue, 0, 0, 0, long.MaxValue, 0, 0, 0, 0)]
    public void MaskLoad_256_nint_64Bit(long value1, long value2, long value3, long value4, long mask1, long mask2, long mask3, long mask4, long expected1, long expected2, long expected3, long expected4)
    {
        Span<nint> value = stackalloc nint[4];
        value[0] = (nint)value1;
        value[1] = (nint)value2;
        value[2] = (nint)value3;
        value[3] = (nint)value4;

        var mask = Vector256.Create(mask1, mask2, mask3, mask4).AsNInt();
        var expected = Vector256.Create(expected1, expected2, expected3, expected4).AsNInt();

        unsafe
        {
            fixed (nint* valuePtr = &value[0])
            {
                var actual = Avx2.MaskLoad(valuePtr, mask);
                Assert.Equal(expected, actual);
            }
        }
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)]
    [InlineData(10, 0, 0, 0, 0, 0, 10, 0, 0, 0, 0, 0)]
    [InlineData(0, int.MaxValue, int.MinValue, int.MaxValue, 0, 0, 0, int.MaxValue, 0, 0, 0, 0)]
    public void MaskLoad_256_nint_32Bit(int value1, int value2, int value3, int value4, int mask1, int mask2, int mask3, int mask4, int expected1, int expected2, int expected3, int expected4)
    {
        Span<nint> value = stackalloc nint[4];
        value[0] = value1;
        value[1] = value2;
        value[2] = value3;
        value[3] = value4;

        var mask = Vector256.Create(mask1, mask2, mask3, mask4).AsNInt();
        var expected = Vector256.Create(expected1, expected2, expected3, expected4).AsNInt();

        unsafe
        {
            fixed (nint* valuePtr = &value[0])
            {
                var actual = Avx2.MaskLoad(valuePtr, mask);
                Assert.Equal(expected, actual);
            }
        }
    }

    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)]
    [InlineData(10, 0, 0, 0, 0, 0, 10, 0, 0, 0, 0, 0)]
    [InlineData(0, ulong.MaxValue, ulong.MinValue, ulong.MaxValue, 0, 0, 0, ulong.MaxValue, 0, 0, 0, ulong.MaxValue)]
    public void MaskLoad_256_nuint_64Bit(ulong value1, ulong value2, ulong value3, ulong value4, ulong mask1, ulong mask2, ulong mask3, ulong mask4, ulong expected1, ulong expected2, ulong expected3, ulong expected4)
    {
        Span<nuint> value = stackalloc nuint[4];
        value[0] = (nuint)value1;
        value[1] = (nuint)value2;
        value[2] = (nuint)value3;
        value[3] = (nuint)value4;

        var mask = Vector256.Create(mask1, mask2, mask3, mask4).AsNUInt();
        var expected = Vector256.Create(expected1, expected2, expected3, expected4).AsNUInt();

        unsafe
        {
            fixed (nuint* valuePtr = &value[0])
            {
                var actual = Avx2.MaskLoad(valuePtr, mask);
                Assert.Equal(expected, actual);
            }
        }
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)]
    [InlineData(10, 0, 0, 0, 0, 0, 10, 0, 0, 0, 0, 0)]
    [InlineData(0, uint.MaxValue, uint.MinValue, uint.MaxValue, 0, 0, 0, uint.MaxValue, 0, 0, 0, uint.MaxValue)]
    public void MaskLoad_256_nuint_32Bit(uint value1, uint value2, uint value3, uint value4, uint mask1, uint mask2, uint mask3, uint mask4, uint expected1, uint expected2, uint expected3, uint expected4)
    {
        Span<nuint> value = stackalloc nuint[4];
        value[0] = value1;
        value[1] = value2;
        value[2] = value3;
        value[3] = value4;

        var mask = Vector256.Create(mask1, mask2, mask3, mask4).AsNUInt();
        var expected = Vector256.Create(expected1, expected2, expected3, expected4).AsNUInt();

        unsafe
        {
            fixed (nuint* valuePtr = &value[0])
            {
                var actual = Avx2.MaskLoad(valuePtr, mask);
                Assert.Equal(expected, actual);
            }
        }
    }
}
