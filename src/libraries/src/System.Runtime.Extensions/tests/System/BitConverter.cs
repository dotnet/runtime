// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Xunit;

public static class BitConverterTests
{
    [Fact]
    public static void DoubleToInt64Bits()
    {
        Double input = 123456.3234;
        Int64 result = BitConverter.DoubleToInt64Bits(input);
        Assert.Equal(4683220267154373240L, result);
        Double roundtripped = BitConverter.Int64BitsToDouble(result);
        Assert.Equal(input, roundtripped);
    }

    [Fact]
    public static void RoundtripBoolean()
    {
        Byte[] bytes = BitConverter.GetBytes(true);
        Assert.Equal(1, bytes.Length);
        Assert.Equal(1, bytes[0]);
        Assert.True(BitConverter.ToBoolean(bytes, 0));

        bytes = BitConverter.GetBytes(false);
        Assert.Equal(1, bytes.Length);
        Assert.Equal(0, bytes[0]);
        Assert.False(BitConverter.ToBoolean(bytes, 0));
    }

    [Fact]
    public static void RoundtripChar()
    {
        Char input = 'A';
        Byte[] expected = { 0x41, 0 };
        VerifyRoundtrip(BitConverter.GetBytes, BitConverter.ToChar, input, expected);
    }

    [Fact]
    public static void RoundtripDouble()
    {
        Double input = 123456.3234;
        Byte[] expected = { 0x78, 0x7a, 0xa5, 0x2c, 0x05, 0x24, 0xfe, 0x40 };
        VerifyRoundtrip(BitConverter.GetBytes, BitConverter.ToDouble, input, expected);
    }

    [Fact]
    public static void RoundtripSingle()
    {
        Single input = 8392.34f;
        Byte[] expected = { 0x5c, 0x21, 0x03, 0x46 };
        VerifyRoundtrip(BitConverter.GetBytes, BitConverter.ToSingle, input, expected);
    }

    [Fact]
    public static void RoundtripInt16()
    {
        Int16 input = 0x1234;
        Byte[] expected = { 0x34, 0x12 };
        VerifyRoundtrip(BitConverter.GetBytes, BitConverter.ToInt16, input, expected);
    }

    [Fact]
    public static void RoundtripInt32()
    {
        Int32 input = 0x12345678;
        Byte[] expected = { 0x78, 0x56, 0x34, 0x12 };
        VerifyRoundtrip(BitConverter.GetBytes, BitConverter.ToInt32, input, expected);
    }

    [Fact]
    public static void RoundtripInt64()
    {
        Int64 input = 0x0123456789abcdef;
        Byte[] expected = { 0xef, 0xcd, 0xab, 0x89, 0x67, 0x45, 0x23, 0x01 };
        VerifyRoundtrip(BitConverter.GetBytes, BitConverter.ToInt64, input, expected);
    }

    [Fact]
    public static void RoundtripUInt16()
    {
        UInt16 input = 0x1234;
        Byte[] expected = { 0x34, 0x12 };
        VerifyRoundtrip(BitConverter.GetBytes, BitConverter.ToUInt16, input, expected);
    }

    [Fact]
    public static void RoundtripUInt32()
    {
        UInt32 input = 0x12345678;
        Byte[] expected = { 0x78, 0x56, 0x34, 0x12 };
        VerifyRoundtrip(BitConverter.GetBytes, BitConverter.ToUInt32, input, expected);
    }

    [Fact]
    public static void RoundtripUInt64()
    {
        UInt64 input = 0x0123456789abcdef;
        Byte[] expected = { 0xef, 0xcd, 0xab, 0x89, 0x67, 0x45, 0x23, 0x01 };
        VerifyRoundtrip(BitConverter.GetBytes, BitConverter.ToUInt64, input, expected);
    }

    [Fact]
    public static void RoundtripString()
    {
        Byte[] bytes = { 0x12, 0x34, 0x56, 0x78, 0x9a };

        Assert.Equal("12-34-56-78-9A", BitConverter.ToString(bytes));
        Assert.Equal("56-78-9A", BitConverter.ToString(bytes, 2));
        Assert.Equal("56", BitConverter.ToString(bytes, 2, 1));
        Assert.Equal(String.Empty, BitConverter.ToString(new byte[0]));
    }

    private static void VerifyRoundtrip<TInput>(Func<TInput, Byte[]> getBytes, Func<Byte[], int, TInput> convertBack, TInput input, Byte[] expectedBytes)
    {
        Byte[] bytes = getBytes(input);
        Assert.Equal(expectedBytes.Length, bytes.Length);

        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(expectedBytes);
        }

        Assert.Equal(expectedBytes, bytes);
        Assert.Equal(input, convertBack(bytes, 0));
    }
}
