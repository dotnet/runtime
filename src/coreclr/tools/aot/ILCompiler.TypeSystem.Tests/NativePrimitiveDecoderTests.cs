// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using Internal.NativeFormat;

using Xunit;

namespace TypeSystemTests;

public unsafe class NativePrimitiveDecoderTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void DecodeFiveByteInteger_Truncated(int length)
    {
        byte* buffer = stackalloc byte[] { 0x0f, 0xff, 0xff, 0xff, 0xff };
        byte* stream = buffer;

        Assert.Throws<BadImageFormatException>(() => NativePrimitiveDecoder.DecodeUnsigned(ref stream, buffer + length));
        Assert.True(stream == buffer);
        Assert.Throws<BadImageFormatException>(() => NativePrimitiveDecoder.DecodeSigned(ref stream, buffer + length));
        Assert.True(stream == buffer);
        Assert.Throws<BadImageFormatException>(() => NativePrimitiveDecoder.DecodeUnsignedLong(ref stream, buffer + length));
        Assert.True(stream == buffer);
        Assert.Throws<BadImageFormatException>(() => NativePrimitiveDecoder.DecodeSignedLong(ref stream, buffer + length));
        Assert.True(stream == buffer);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public void DecodeNineByteInteger_Truncated(int length)
    {
        byte* buffer = stackalloc byte[] { 0x1f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff };
        byte* stream = buffer;

        Assert.Throws<BadImageFormatException>(() => NativePrimitiveDecoder.DecodeUnsignedLong(ref stream, buffer + length));
        Assert.True(stream == buffer);
        Assert.Throws<BadImageFormatException>(() => NativePrimitiveDecoder.DecodeSignedLong(ref stream, buffer + length));
        Assert.True(stream == buffer);
    }

    [Theory]
    [InlineData(0x12345678u)]
    [InlineData(uint.MaxValue)]
    public void DecodeFiveByteInteger_ExactLength(uint value)
    {
        byte* buffer = stackalloc byte[5];
        buffer[0] = 0x0f;
        BinaryPrimitives.WriteUInt32LittleEndian(new Span<byte>(buffer + 1, 4), value);
        byte* stream = buffer;

        Assert.Equal(value, NativePrimitiveDecoder.DecodeUnsigned(ref stream, buffer + 5));
        Assert.True(stream == buffer + 5);
        stream = buffer;
        Assert.Equal(unchecked((int)value), NativePrimitiveDecoder.DecodeSigned(ref stream, buffer + 5));
        Assert.True(stream == buffer + 5);
        stream = buffer;
        Assert.Equal((ulong)value, NativePrimitiveDecoder.DecodeUnsignedLong(ref stream, buffer + 5));
        Assert.True(stream == buffer + 5);
        stream = buffer;
        Assert.Equal((long)unchecked((int)value), NativePrimitiveDecoder.DecodeSignedLong(ref stream, buffer + 5));
        Assert.True(stream == buffer + 5);
    }

    [Theory]
    [InlineData(0x123456789abcdef0ul)]
    [InlineData(ulong.MaxValue)]
    public void DecodeNineByteInteger_ExactLength(ulong value)
    {
        byte* buffer = stackalloc byte[9];
        buffer[0] = 0x1f;
        BinaryPrimitives.WriteUInt64LittleEndian(new Span<byte>(buffer + 1, 8), value);
        byte* stream = buffer;

        Assert.Equal(value, NativePrimitiveDecoder.DecodeUnsignedLong(ref stream, buffer + 9));
        Assert.True(stream == buffer + 9);
        stream = buffer;
        Assert.Equal(unchecked((long)value), NativePrimitiveDecoder.DecodeSignedLong(ref stream, buffer + 9));
        Assert.True(stream == buffer + 9);
    }
}
