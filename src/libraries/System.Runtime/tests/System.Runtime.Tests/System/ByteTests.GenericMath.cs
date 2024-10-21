// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Tests
{
    public class ByteTests_GenericMath
    {
        //
        // IAdditionOperators
        //

        [Fact]
        public static void op_AdditionTest()
        {
            Assert.Equal((byte)0x01, AdditionOperatorsHelper<byte, byte, byte>.op_Addition((byte)0x00, (byte)1));
            Assert.Equal((byte)0x02, AdditionOperatorsHelper<byte, byte, byte>.op_Addition((byte)0x01, (byte)1));
            Assert.Equal((byte)0x80, AdditionOperatorsHelper<byte, byte, byte>.op_Addition((byte)0x7F, (byte)1));
            Assert.Equal((byte)0x81, AdditionOperatorsHelper<byte, byte, byte>.op_Addition((byte)0x80, (byte)1));
            Assert.Equal((byte)0x00, AdditionOperatorsHelper<byte, byte, byte>.op_Addition((byte)0xFF, (byte)1));
        }

        [Fact]
        public static void op_CheckedAdditionTest()
        {
            Assert.Equal((byte)0x01, AdditionOperatorsHelper<byte, byte, byte>.op_CheckedAddition((byte)0x00, (byte)1));
            Assert.Equal((byte)0x02, AdditionOperatorsHelper<byte, byte, byte>.op_CheckedAddition((byte)0x01, (byte)1));
            Assert.Equal((byte)0x80, AdditionOperatorsHelper<byte, byte, byte>.op_CheckedAddition((byte)0x7F, (byte)1));
            Assert.Equal((byte)0x81, AdditionOperatorsHelper<byte, byte, byte>.op_CheckedAddition((byte)0x80, (byte)1));

            Assert.Throws<OverflowException>(() => AdditionOperatorsHelper<byte, byte, byte>.op_CheckedAddition((byte)0xFF, (byte)1));
        }

        //
        // IAdditiveIdentity
        //

        [Fact]
        public static void AdditiveIdentityTest()
        {
            Assert.Equal((byte)0x00, AdditiveIdentityHelper<byte, byte>.AdditiveIdentity);
        }

        //
        // IBinaryInteger
        //

        [Fact]
        public static void DivRemTest()
        {
            Assert.Equal(((byte)0x00, (byte)0x00), BinaryIntegerHelper<byte>.DivRem((byte)0x00, (byte)2));
            Assert.Equal(((byte)0x00, (byte)0x01), BinaryIntegerHelper<byte>.DivRem((byte)0x01, (byte)2));
            Assert.Equal(((byte)0x3F, (byte)0x01), BinaryIntegerHelper<byte>.DivRem((byte)0x7F, (byte)2));
            Assert.Equal(((byte)0x40, (byte)0x00), BinaryIntegerHelper<byte>.DivRem((byte)0x80, (byte)2));
            Assert.Equal(((byte)0x7F, (byte)0x01), BinaryIntegerHelper<byte>.DivRem((byte)0xFF, (byte)2));
        }

        [Fact]
        public static void LeadingZeroCountTest()
        {
            Assert.Equal((byte)0x08, BinaryIntegerHelper<byte>.LeadingZeroCount((byte)0x00));
            Assert.Equal((byte)0x07, BinaryIntegerHelper<byte>.LeadingZeroCount((byte)0x01));
            Assert.Equal((byte)0x01, BinaryIntegerHelper<byte>.LeadingZeroCount((byte)0x7F));
            Assert.Equal((byte)0x00, BinaryIntegerHelper<byte>.LeadingZeroCount((byte)0x80));
            Assert.Equal((byte)0x00, BinaryIntegerHelper<byte>.LeadingZeroCount((byte)0xFF));
        }

        [Fact]
        public static void PopCountTest()
        {
            Assert.Equal((byte)0x00, BinaryIntegerHelper<byte>.PopCount((byte)0x00));
            Assert.Equal((byte)0x01, BinaryIntegerHelper<byte>.PopCount((byte)0x01));
            Assert.Equal((byte)0x07, BinaryIntegerHelper<byte>.PopCount((byte)0x7F));
            Assert.Equal((byte)0x01, BinaryIntegerHelper<byte>.PopCount((byte)0x80));
            Assert.Equal((byte)0x08, BinaryIntegerHelper<byte>.PopCount((byte)0xFF));
        }

        [Fact]
        public static void RotateLeftTest()
        {
            Assert.Equal((byte)0x00, BinaryIntegerHelper<byte>.RotateLeft((byte)0x00, 1));
            Assert.Equal((byte)0x02, BinaryIntegerHelper<byte>.RotateLeft((byte)0x01, 1));
            Assert.Equal((byte)0xFE, BinaryIntegerHelper<byte>.RotateLeft((byte)0x7F, 1));
            Assert.Equal((byte)0x01, BinaryIntegerHelper<byte>.RotateLeft((byte)0x80, 1));
            Assert.Equal((byte)0xFF, BinaryIntegerHelper<byte>.RotateLeft((byte)0xFF, 1));
        }

        [Fact]
        public static void RotateRightTest()
        {
            Assert.Equal((byte)0x00, BinaryIntegerHelper<byte>.RotateRight((byte)0x00, 1));
            Assert.Equal((byte)0x80, BinaryIntegerHelper<byte>.RotateRight((byte)0x01, 1));
            Assert.Equal((byte)0xBF, BinaryIntegerHelper<byte>.RotateRight((byte)0x7F, 1));
            Assert.Equal((byte)0x40, BinaryIntegerHelper<byte>.RotateRight((byte)0x80, 1));
            Assert.Equal((byte)0xFF, BinaryIntegerHelper<byte>.RotateRight((byte)0xFF, 1));
        }

        [Fact]
        public static void TrailingZeroCountTest()
        {
            Assert.Equal((byte)0x08, BinaryIntegerHelper<byte>.TrailingZeroCount((byte)0x00));
            Assert.Equal((byte)0x00, BinaryIntegerHelper<byte>.TrailingZeroCount((byte)0x01));
            Assert.Equal((byte)0x00, BinaryIntegerHelper<byte>.TrailingZeroCount((byte)0x7F));
            Assert.Equal((byte)0x07, BinaryIntegerHelper<byte>.TrailingZeroCount((byte)0x80));
            Assert.Equal((byte)0x00, BinaryIntegerHelper<byte>.TrailingZeroCount((byte)0xFF));
        }

        [Fact]
        public static void TryReadBigEndianByteTest()
        {
            byte result;

            Assert.True(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0x00 }, isUnsigned: true, out result));
            Assert.Equal((byte)0x00, result);

            Assert.True(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0x01 }, isUnsigned: true, out result));
            Assert.Equal((byte)0x01, result);

            Assert.True(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0x7F }, isUnsigned: true, out result));
            Assert.Equal((byte)0x7F, result);

            Assert.True(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0x80 }, isUnsigned: true, out result));
            Assert.Equal((byte)0x80, result);

            Assert.True(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0xFF }, isUnsigned: true, out result));
            Assert.Equal((byte)0xFF, result);
        }

        [Fact]
        public static void TryReadBigEndianInt16Test()
        {
            byte result;

            Assert.True(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((byte)0x00, result);

            Assert.True(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal((byte)0x01, result);

            Assert.True(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal((byte)0x80, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0x01, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0x7F, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0x80, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((byte)0x00, result);
        }

        [Fact]
        public static void TryReadBigEndianInt32Test()
        {
            byte result;

            Assert.True(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((byte)0x00, result);

            Assert.True(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal((byte)0x01, result);

            Assert.True(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal((byte)0x80, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0x01, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0x80, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((byte)0x00, result);
        }

        [Fact]
        public static void TryReadBigEndianInt64Test()
        {
            byte result;

            Assert.True(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((byte)0x00, result);

            Assert.True(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal((byte)0x01, result);

            Assert.True(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal((byte)0x80, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((byte)0x00, result);
        }

        [Fact]
        public static void TryReadBigEndianInt96Test()
        {
            byte result;

            Assert.True(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((byte)0x00, result);

            Assert.True(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal((byte)0x01, result);

            Assert.True(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal((byte)0x80, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((byte)0x00, result);
        }

        [Fact]
        public static void TryReadBigEndianInt128Test()
        {
            byte result;

            Assert.True(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((byte)0x00, result);

            Assert.True(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal((byte)0x01, result);

            Assert.True(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal((byte)0x80, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((byte)0x00, result);
        }

        [Fact]
        public static void TryReadBigEndianSByteTest()
        {
            byte result;

            Assert.True(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0x00 }, isUnsigned: false, out result));
            Assert.Equal((byte)0x00, result);

            Assert.True(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0x01 }, isUnsigned: false, out result));
            Assert.Equal((byte)0x01, result);

            Assert.True(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0x7F }, isUnsigned: false, out result));
            Assert.Equal((byte)0x7F, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0x80 }, isUnsigned: false, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0xFF }, isUnsigned: false, out result));
            Assert.Equal((byte)0x00, result);
        }

        [Fact]
        public static void TryReadBigEndianUInt16Test()
        {
            byte result;

            Assert.True(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((byte)0x00, result);

            Assert.True(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal((byte)0x01, result);

            Assert.True(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal((byte)0x80, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0x01, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0x7F, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0x80, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((byte)0x00, result);
        }

        [Fact]
        public static void TryReadBigEndianUInt32Test()
        {
            byte result;

            Assert.True(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((byte)0x00, result);

            Assert.True(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal((byte)0x01, result);

            Assert.True(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal((byte)0x80, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0x01, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0x80, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((byte)0x00, result);
        }

        [Fact]
        public static void TryReadBigEndianUInt64Test()
        {
            byte result;

            Assert.True(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((byte)0x00, result);

            Assert.True(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal((byte)0x01, result);

            Assert.True(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal((byte)0x80, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((byte)0x00, result);
        }

        [Fact]
        public static void TryReadBigEndianUInt96Test()
        {
            byte result;

            Assert.True(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((byte)0x00, result);

            Assert.True(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal((byte)0x01, result);

            Assert.True(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal((byte)0x80, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((byte)0x00, result);
        }

        [Fact]
        public static void TryReadBigEndianUInt128Test()
        {
            byte result;

            Assert.True(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((byte)0x00, result);

            Assert.True(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal((byte)0x01, result);

            Assert.True(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal((byte)0x80, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((byte)0x00, result);
        }

        [Fact]
        public static void TryReadLittleEndianByteTest()
        {
            byte result;

            Assert.True(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0x00 }, isUnsigned: true, out result));
            Assert.Equal((byte)0x00, result);

            Assert.True(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0x01 }, isUnsigned: true, out result));
            Assert.Equal((byte)0x01, result);

            Assert.True(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0x7F }, isUnsigned: true, out result));
            Assert.Equal((byte)0x7F, result);

            Assert.True(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0x80 }, isUnsigned: true, out result));
            Assert.Equal((byte)0x80, result);

            Assert.True(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0xFF }, isUnsigned: true, out result));
            Assert.Equal((byte)0xFF, result);
        }

        [Fact]
        public static void TryReadLittleEndianInt16Test()
        {
            byte result;

            Assert.True(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal((byte)0x00, result);

            Assert.True(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0x01, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((byte)0x01, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((byte)0x00, result);

            Assert.True(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0x80, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((byte)0x80, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((byte)0x00, result);
        }

        [Fact]
        public static void TryReadLittleEndianInt32Test()
        {
            byte result;

            Assert.True(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal((byte)0x00, result);

            Assert.True(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0x01, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((byte)0x01, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((byte)0x00, result);

            Assert.True(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0x80, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((byte)0x80, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((byte)0x00, result);
        }

        [Fact]
        public static void TryReadLittleEndianInt64Test()
        {
            byte result;

            Assert.True(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal((byte)0x00, result);

            Assert.True(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((byte)0x01, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((byte)0x00, result);

            Assert.True(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((byte)0x80, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((byte)0x00, result);
        }

        [Fact]
        public static void TryReadLittleEndianInt96Test()
        {
            byte result;

            Assert.True(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal((byte)0x00, result);

            Assert.True(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((byte)0x01, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((byte)0x00, result);

            Assert.True(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((byte)0x80, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((byte)0x00, result);
        }

        [Fact]
        public static void TryReadLittleEndianInt128Test()
        {
            byte result;

            Assert.True(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal((byte)0x00, result);

            Assert.True(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((byte)0x01, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((byte)0x00, result);

            Assert.True(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((byte)0x80, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((byte)0x00, result);
        }

        [Fact]
        public static void TryReadLittleEndianSByteTest()
        {
            byte result;

            Assert.True(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0x00 }, isUnsigned: false, out result));
            Assert.Equal((byte)0x00, result);

            Assert.True(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0x01 }, isUnsigned: false, out result));
            Assert.Equal((byte)0x01, result);

            Assert.True(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0x7F }, isUnsigned: false, out result));
            Assert.Equal((byte)0x7F, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0x80 }, isUnsigned: false, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0xFF }, isUnsigned: false, out result));
            Assert.Equal((byte)0x00, result);
        }

        [Fact]
        public static void TryReadLittleEndianUInt16Test()
        {
            byte result;

            Assert.True(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal((byte)0x00, result);

            Assert.True(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0x01, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((byte)0x01, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((byte)0x00, result);

            Assert.True(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0x80, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((byte)0x80, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((byte)0x00, result);
        }

        [Fact]
        public static void TryReadLittleEndianUInt32Test()
        {
            byte result;

            Assert.True(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal((byte)0x00, result);

            Assert.True(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0x01, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((byte)0x01, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((byte)0x00, result);

            Assert.True(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0x80, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((byte)0x80, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((byte)0x00, result);
        }

        [Fact]
        public static void TryReadLittleEndianUInt64Test()
        {
            byte result;

            Assert.True(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal((byte)0x00, result);

            Assert.True(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((byte)0x01, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((byte)0x00, result);

            Assert.True(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((byte)0x80, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((byte)0x00, result);
        }

        [Fact]
        public static void TryReadLittleEndianUInt96Test()
        {
            byte result;

            Assert.True(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal((byte)0x00, result);

            Assert.True(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((byte)0x01, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((byte)0x00, result);

            Assert.True(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((byte)0x80, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((byte)0x00, result);
        }

        [Fact]
        public static void TryReadLittleEndianUInt128Test()
        {
            byte result;

            Assert.True(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal((byte)0x00, result);

            Assert.True(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((byte)0x01, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((byte)0x00, result);

            Assert.True(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((byte)0x80, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(BinaryIntegerHelper<byte>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((byte)0x00, result);
        }

        [Fact]
        public static void GetByteCountTest()
        {
            Assert.Equal(1, BinaryIntegerHelper<byte>.GetByteCount((byte)0x00));
            Assert.Equal(1, BinaryIntegerHelper<byte>.GetByteCount((byte)0x01));
            Assert.Equal(1, BinaryIntegerHelper<byte>.GetByteCount((byte)0x7F));
            Assert.Equal(1, BinaryIntegerHelper<byte>.GetByteCount((byte)0x80));
            Assert.Equal(1, BinaryIntegerHelper<byte>.GetByteCount((byte)0xFF));
        }

        [Fact]
        public static void GetShortestBitLengthTest()
        {
            Assert.Equal(0x00, BinaryIntegerHelper<byte>.GetShortestBitLength((byte)0x00));
            Assert.Equal(0x01, BinaryIntegerHelper<byte>.GetShortestBitLength((byte)0x01));
            Assert.Equal(0x07, BinaryIntegerHelper<byte>.GetShortestBitLength((byte)0x7F));
            Assert.Equal(0x08, BinaryIntegerHelper<byte>.GetShortestBitLength((byte)0x80));
            Assert.Equal(0x08, BinaryIntegerHelper<byte>.GetShortestBitLength((byte)0xFF));
        }

        [Fact]
        public static void TryWriteBigEndianTest()
        {
            Span<byte> destination = stackalloc byte[1];
            int bytesWritten = 0;

            Assert.True(BinaryIntegerHelper<byte>.TryWriteBigEndian((byte)0x00, destination, out bytesWritten));
            Assert.Equal(1, bytesWritten);
            Assert.Equal(new byte[] { 0x00 }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<byte>.TryWriteBigEndian((byte)0x01, destination, out bytesWritten));
            Assert.Equal(1, bytesWritten);
            Assert.Equal(new byte[] { 0x01 }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<byte>.TryWriteBigEndian((byte)0x7F, destination, out bytesWritten));
            Assert.Equal(1, bytesWritten);
            Assert.Equal(new byte[] { 0x7F }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<byte>.TryWriteBigEndian((byte)0x80, destination, out bytesWritten));
            Assert.Equal(1, bytesWritten);
            Assert.Equal(new byte[] { 0x80 }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<byte>.TryWriteBigEndian((byte)0xFF, destination, out bytesWritten));
            Assert.Equal(1, bytesWritten);
            Assert.Equal(new byte[] { 0xFF }, destination.ToArray());

            Assert.False(BinaryIntegerHelper<byte>.TryWriteBigEndian(default, Span<byte>.Empty, out bytesWritten));
            Assert.Equal(0, bytesWritten);
            Assert.Equal(new byte[] { 0xFF }, destination.ToArray());
        }

        [Fact]
        public static void TryWriteLittleEndianTest()
        {
            Span<byte> destination = stackalloc byte[1];
            int bytesWritten = 0;

            Assert.True(BinaryIntegerHelper<byte>.TryWriteLittleEndian((byte)0x00, destination, out bytesWritten));
            Assert.Equal(1, bytesWritten);
            Assert.Equal(new byte[] { 0x00 }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<byte>.TryWriteLittleEndian((byte)0x01, destination, out bytesWritten));
            Assert.Equal(1, bytesWritten);
            Assert.Equal(new byte[] { 0x01 }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<byte>.TryWriteLittleEndian((byte)0x7F, destination, out bytesWritten));
            Assert.Equal(1, bytesWritten);
            Assert.Equal(new byte[] { 0x7F }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<byte>.TryWriteLittleEndian((byte)0x80, destination, out bytesWritten));
            Assert.Equal(1, bytesWritten);
            Assert.Equal(new byte[] { 0x80 }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<byte>.TryWriteLittleEndian((byte)0xFF, destination, out bytesWritten));
            Assert.Equal(1, bytesWritten);
            Assert.Equal(new byte[] { 0xFF }, destination.ToArray());

            Assert.False(BinaryIntegerHelper<byte>.TryWriteLittleEndian(default, Span<byte>.Empty, out bytesWritten));
            Assert.Equal(0, bytesWritten);
            Assert.Equal(new byte[] { 0xFF }, destination.ToArray());
        }

        //
        // IBinaryNumber
        //

        [Fact]
        public static void AllBitsSetTest()
        {
            Assert.Equal((byte)0xFF, BinaryNumberHelper<byte>.AllBitsSet);
            Assert.Equal((byte)0, (byte)~BinaryNumberHelper<byte>.AllBitsSet);
        }

        [Fact]
        public static void IsPow2Test()
        {
            Assert.False(BinaryNumberHelper<byte>.IsPow2((byte)0x00));
            Assert.True(BinaryNumberHelper<byte>.IsPow2((byte)0x01));
            Assert.False(BinaryNumberHelper<byte>.IsPow2((byte)0x7F));
            Assert.True(BinaryNumberHelper<byte>.IsPow2((byte)0x80));
            Assert.False(BinaryNumberHelper<byte>.IsPow2((byte)0xFF));
        }

        [Fact]
        public static void Log2Test()
        {
            Assert.Equal((byte)0x00, BinaryNumberHelper<byte>.Log2((byte)0x00));
            Assert.Equal((byte)0x00, BinaryNumberHelper<byte>.Log2((byte)0x01));
            Assert.Equal((byte)0x06, BinaryNumberHelper<byte>.Log2((byte)0x7F));
            Assert.Equal((byte)0x07, BinaryNumberHelper<byte>.Log2((byte)0x80));
            Assert.Equal((byte)0x07, BinaryNumberHelper<byte>.Log2((byte)0xFF));
        }

        //
        // IBitwiseOperators
        //

        [Fact]
        public static void op_BitwiseAndTest()
        {
            Assert.Equal((byte)0x00, BitwiseOperatorsHelper<byte, byte, byte>.op_BitwiseAnd((byte)0x00, (byte)1));
            Assert.Equal((byte)0x01, BitwiseOperatorsHelper<byte, byte, byte>.op_BitwiseAnd((byte)0x01, (byte)1));
            Assert.Equal((byte)0x01, BitwiseOperatorsHelper<byte, byte, byte>.op_BitwiseAnd((byte)0x7F, (byte)1));
            Assert.Equal((byte)0x00, BitwiseOperatorsHelper<byte, byte, byte>.op_BitwiseAnd((byte)0x80, (byte)1));
            Assert.Equal((byte)0x01, BitwiseOperatorsHelper<byte, byte, byte>.op_BitwiseAnd((byte)0xFF, (byte)1));
        }

        [Fact]
        public static void op_BitwiseOrTest()
        {
            Assert.Equal((byte)0x01, BitwiseOperatorsHelper<byte, byte, byte>.op_BitwiseOr((byte)0x00, (byte)1));
            Assert.Equal((byte)0x01, BitwiseOperatorsHelper<byte, byte, byte>.op_BitwiseOr((byte)0x01, (byte)1));
            Assert.Equal((byte)0x7F, BitwiseOperatorsHelper<byte, byte, byte>.op_BitwiseOr((byte)0x7F, (byte)1));
            Assert.Equal((byte)0x81, BitwiseOperatorsHelper<byte, byte, byte>.op_BitwiseOr((byte)0x80, (byte)1));
            Assert.Equal((byte)0xFF, BitwiseOperatorsHelper<byte, byte, byte>.op_BitwiseOr((byte)0xFF, (byte)1));
        }

        [Fact]
        public static void op_ExclusiveOrTest()
        {
            Assert.Equal((byte)0x01, BitwiseOperatorsHelper<byte, byte, byte>.op_ExclusiveOr((byte)0x00, (byte)1));
            Assert.Equal((byte)0x00, BitwiseOperatorsHelper<byte, byte, byte>.op_ExclusiveOr((byte)0x01, (byte)1));
            Assert.Equal((byte)0x7E, BitwiseOperatorsHelper<byte, byte, byte>.op_ExclusiveOr((byte)0x7F, (byte)1));
            Assert.Equal((byte)0x81, BitwiseOperatorsHelper<byte, byte, byte>.op_ExclusiveOr((byte)0x80, (byte)1));
            Assert.Equal((byte)0xFE, BitwiseOperatorsHelper<byte, byte, byte>.op_ExclusiveOr((byte)0xFF, (byte)1));
        }

        [Fact]
        public static void op_OnesComplementTest()
        {
            Assert.Equal((byte)0xFF, BitwiseOperatorsHelper<byte, byte, byte>.op_OnesComplement((byte)0x00));
            Assert.Equal((byte)0xFE, BitwiseOperatorsHelper<byte, byte, byte>.op_OnesComplement((byte)0x01));
            Assert.Equal((byte)0x80, BitwiseOperatorsHelper<byte, byte, byte>.op_OnesComplement((byte)0x7F));
            Assert.Equal((byte)0x7F, BitwiseOperatorsHelper<byte, byte, byte>.op_OnesComplement((byte)0x80));
            Assert.Equal((byte)0x00, BitwiseOperatorsHelper<byte, byte, byte>.op_OnesComplement((byte)0xFF));
        }

        //
        // IComparisonOperators
        //

        [Fact]
        public static void op_GreaterThanTest()
        {
            Assert.False(ComparisonOperatorsHelper<byte, byte, bool>.op_GreaterThan((byte)0x00, (byte)1));
            Assert.False(ComparisonOperatorsHelper<byte, byte, bool>.op_GreaterThan((byte)0x01, (byte)1));
            Assert.True(ComparisonOperatorsHelper<byte, byte, bool>.op_GreaterThan((byte)0x7F, (byte)1));
            Assert.True(ComparisonOperatorsHelper<byte, byte, bool>.op_GreaterThan((byte)0x80, (byte)1));
            Assert.True(ComparisonOperatorsHelper<byte, byte, bool>.op_GreaterThan((byte)0xFF, (byte)1));
        }

        [Fact]
        public static void op_GreaterThanOrEqualTest()
        {
            Assert.False(ComparisonOperatorsHelper<byte, byte, bool>.op_GreaterThanOrEqual((byte)0x00, (byte)1));
            Assert.True(ComparisonOperatorsHelper<byte, byte, bool>.op_GreaterThanOrEqual((byte)0x01, (byte)1));
            Assert.True(ComparisonOperatorsHelper<byte, byte, bool>.op_GreaterThanOrEqual((byte)0x7F, (byte)1));
            Assert.True(ComparisonOperatorsHelper<byte, byte, bool>.op_GreaterThanOrEqual((byte)0x80, (byte)1));
            Assert.True(ComparisonOperatorsHelper<byte, byte, bool>.op_GreaterThanOrEqual((byte)0xFF, (byte)1));
        }

        [Fact]
        public static void op_LessThanTest()
        {
            Assert.True(ComparisonOperatorsHelper<byte, byte, bool>.op_LessThan((byte)0x00, (byte)1));
            Assert.False(ComparisonOperatorsHelper<byte, byte, bool>.op_LessThan((byte)0x01, (byte)1));
            Assert.False(ComparisonOperatorsHelper<byte, byte, bool>.op_LessThan((byte)0x7F, (byte)1));
            Assert.False(ComparisonOperatorsHelper<byte, byte, bool>.op_LessThan((byte)0x80, (byte)1));
            Assert.False(ComparisonOperatorsHelper<byte, byte, bool>.op_LessThan((byte)0xFF, (byte)1));
        }

        [Fact]
        public static void op_LessThanOrEqualTest()
        {
            Assert.True(ComparisonOperatorsHelper<byte, byte, bool>.op_LessThanOrEqual((byte)0x00, (byte)1));
            Assert.True(ComparisonOperatorsHelper<byte, byte, bool>.op_LessThanOrEqual((byte)0x01, (byte)1));
            Assert.False(ComparisonOperatorsHelper<byte, byte, bool>.op_LessThanOrEqual((byte)0x7F, (byte)1));
            Assert.False(ComparisonOperatorsHelper<byte, byte, bool>.op_LessThanOrEqual((byte)0x80, (byte)1));
            Assert.False(ComparisonOperatorsHelper<byte, byte, bool>.op_LessThanOrEqual((byte)0xFF, (byte)1));
        }

        //
        // IDecrementOperators
        //

        [Fact]
        public static void op_DecrementTest()
        {
            Assert.Equal((byte)0xFF, DecrementOperatorsHelper<byte>.op_Decrement((byte)0x00));
            Assert.Equal((byte)0x00, DecrementOperatorsHelper<byte>.op_Decrement((byte)0x01));
            Assert.Equal((byte)0x7E, DecrementOperatorsHelper<byte>.op_Decrement((byte)0x7F));
            Assert.Equal((byte)0x7F, DecrementOperatorsHelper<byte>.op_Decrement((byte)0x80));
            Assert.Equal((byte)0xFE, DecrementOperatorsHelper<byte>.op_Decrement((byte)0xFF));
        }

        [Fact]
        public static void op_CheckedDecrementTest()
        {
            Assert.Equal((byte)0x00, DecrementOperatorsHelper<byte>.op_CheckedDecrement((byte)0x01));
            Assert.Equal((byte)0x7E, DecrementOperatorsHelper<byte>.op_CheckedDecrement((byte)0x7F));
            Assert.Equal((byte)0x7F, DecrementOperatorsHelper<byte>.op_Decrement((byte)0x80));
            Assert.Equal((byte)0xFE, DecrementOperatorsHelper<byte>.op_CheckedDecrement((byte)0xFF));

            Assert.Throws<OverflowException>(() => DecrementOperatorsHelper<byte>.op_CheckedDecrement((byte)0x00));
        }

        //
        // IDivisionOperators
        //

        [Fact]
        public static void op_DivisionTest()
        {
            Assert.Equal((byte)0x00, DivisionOperatorsHelper<byte, byte, byte>.op_Division((byte)0x00, (byte)2));
            Assert.Equal((byte)0x00, DivisionOperatorsHelper<byte, byte, byte>.op_Division((byte)0x01, (byte)2));
            Assert.Equal((byte)0x3F, DivisionOperatorsHelper<byte, byte, byte>.op_Division((byte)0x7F, (byte)2));
            Assert.Equal((byte)0x40, DivisionOperatorsHelper<byte, byte, byte>.op_Division((byte)0x80, (byte)2));
            Assert.Equal((byte)0x7F, DivisionOperatorsHelper<byte, byte, byte>.op_Division((byte)0xFF, (byte)2));

            Assert.Throws<DivideByZeroException>(() => DivisionOperatorsHelper<byte, byte, byte>.op_Division((byte)0x01, (byte)0));
        }

        [Fact]
        public static void op_CheckedDivisionTest()
        {
            Assert.Equal((byte)0x00, DivisionOperatorsHelper<byte, byte, byte>.op_CheckedDivision((byte)0x00, (byte)2));
            Assert.Equal((byte)0x00, DivisionOperatorsHelper<byte, byte, byte>.op_CheckedDivision((byte)0x01, (byte)2));
            Assert.Equal((byte)0x3F, DivisionOperatorsHelper<byte, byte, byte>.op_CheckedDivision((byte)0x7F, (byte)2));
            Assert.Equal((byte)0x40, DivisionOperatorsHelper<byte, byte, byte>.op_CheckedDivision((byte)0x80, (byte)2));
            Assert.Equal((byte)0x7F, DivisionOperatorsHelper<byte, byte, byte>.op_CheckedDivision((byte)0xFF, (byte)2));

            Assert.Throws<DivideByZeroException>(() => DivisionOperatorsHelper<byte, byte, byte>.op_CheckedDivision((byte)0x01, (byte)0));
        }

        //
        // IEqualityOperators
        //

        [Fact]
        public static void op_EqualityTest()
        {
            Assert.False(EqualityOperatorsHelper<byte, byte, bool>.op_Equality((byte)0x00, (byte)1));
            Assert.True(EqualityOperatorsHelper<byte, byte, bool>.op_Equality((byte)0x01, (byte)1));
            Assert.False(EqualityOperatorsHelper<byte, byte, bool>.op_Equality((byte)0x7F, (byte)1));
            Assert.False(EqualityOperatorsHelper<byte, byte, bool>.op_Equality((byte)0x80, (byte)1));
            Assert.False(EqualityOperatorsHelper<byte, byte, bool>.op_Equality((byte)0xFF, (byte)1));
        }

        [Fact]
        public static void op_InequalityTest()
        {
            Assert.True(EqualityOperatorsHelper<byte, byte, bool>.op_Inequality((byte)0x00, (byte)1));
            Assert.False(EqualityOperatorsHelper<byte, byte, bool>.op_Inequality((byte)0x01, (byte)1));
            Assert.True(EqualityOperatorsHelper<byte, byte, bool>.op_Inequality((byte)0x7F, (byte)1));
            Assert.True(EqualityOperatorsHelper<byte, byte, bool>.op_Inequality((byte)0x80, (byte)1));
            Assert.True(EqualityOperatorsHelper<byte, byte, bool>.op_Inequality((byte)0xFF, (byte)1));
        }

        //
        // IIncrementOperators
        //

        [Fact]
        public static void op_IncrementTest()
        {
            Assert.Equal((byte)0x01, IncrementOperatorsHelper<byte>.op_Increment((byte)0x00));
            Assert.Equal((byte)0x02, IncrementOperatorsHelper<byte>.op_Increment((byte)0x01));
            Assert.Equal((byte)0x80, IncrementOperatorsHelper<byte>.op_Increment((byte)0x7F));
            Assert.Equal((byte)0x81, IncrementOperatorsHelper<byte>.op_Increment((byte)0x80));
            Assert.Equal((byte)0x00, IncrementOperatorsHelper<byte>.op_Increment((byte)0xFF));
        }

        [Fact]
        public static void op_CheckedIncrementTest()
        {
            Assert.Equal((byte)0x01, IncrementOperatorsHelper<byte>.op_CheckedIncrement((byte)0x00));
            Assert.Equal((byte)0x02, IncrementOperatorsHelper<byte>.op_CheckedIncrement((byte)0x01));
            Assert.Equal((byte)0x80, IncrementOperatorsHelper<byte>.op_Increment((byte)0x7F));
            Assert.Equal((byte)0x81, IncrementOperatorsHelper<byte>.op_CheckedIncrement((byte)0x80));

            Assert.Throws<OverflowException>(() => IncrementOperatorsHelper<byte>.op_CheckedIncrement((byte)0xFF));
        }

        //
        // IMinMaxValue
        //

        [Fact]
        public static void MaxValueTest()
        {
            Assert.Equal((byte)0xFF, MinMaxValueHelper<byte>.MaxValue);
        }

        [Fact]
        public static void MinValueTest()
        {
            Assert.Equal((byte)0x00, MinMaxValueHelper<byte>.MinValue);
        }

        //
        // IModulusOperators
        //

        [Fact]
        public static void op_ModulusTest()
        {
            Assert.Equal((byte)0x00, ModulusOperatorsHelper<byte, byte, byte>.op_Modulus((byte)0x00, (byte)2));
            Assert.Equal((byte)0x01, ModulusOperatorsHelper<byte, byte, byte>.op_Modulus((byte)0x01, (byte)2));
            Assert.Equal((byte)0x01, ModulusOperatorsHelper<byte, byte, byte>.op_Modulus((byte)0x7F, (byte)2));
            Assert.Equal((byte)0x00, ModulusOperatorsHelper<byte, byte, byte>.op_Modulus((byte)0x80, (byte)2));
            Assert.Equal((byte)0x01, ModulusOperatorsHelper<byte, byte, byte>.op_Modulus((byte)0xFF, (byte)2));

            Assert.Throws<DivideByZeroException>(() => ModulusOperatorsHelper<byte, byte, byte>.op_Modulus((byte)0x01, (byte)0));
        }

        //
        // IMultiplicativeIdentity
        //

        [Fact]
        public static void MultiplicativeIdentityTest()
        {
            Assert.Equal((byte)0x01, MultiplicativeIdentityHelper<byte, byte>.MultiplicativeIdentity);
        }

        //
        // IMultiplyOperators
        //

        [Fact]
        public static void op_MultiplyTest()
        {
            Assert.Equal((byte)0x00, MultiplyOperatorsHelper<byte, byte, byte>.op_Multiply((byte)0x00, (byte)2));
            Assert.Equal((byte)0x02, MultiplyOperatorsHelper<byte, byte, byte>.op_Multiply((byte)0x01, (byte)2));
            Assert.Equal((byte)0xFE, MultiplyOperatorsHelper<byte, byte, byte>.op_Multiply((byte)0x7F, (byte)2));
            Assert.Equal((byte)0x00, MultiplyOperatorsHelper<byte, byte, byte>.op_Multiply((byte)0x80, (byte)2));
            Assert.Equal((byte)0xFE, MultiplyOperatorsHelper<byte, byte, byte>.op_Multiply((byte)0xFF, (byte)2));
        }

        [Fact]
        public static void op_CheckedMultiplyTest()
        {
            Assert.Equal((byte)0x00, MultiplyOperatorsHelper<byte, byte, byte>.op_CheckedMultiply((byte)0x00, (byte)2));
            Assert.Equal((byte)0x02, MultiplyOperatorsHelper<byte, byte, byte>.op_CheckedMultiply((byte)0x01, (byte)2));
            Assert.Equal((byte)0xFE, MultiplyOperatorsHelper<byte, byte, byte>.op_CheckedMultiply((byte)0x7F, (byte)2));

            Assert.Throws<OverflowException>(() => MultiplyOperatorsHelper<byte, byte, byte>.op_CheckedMultiply((byte)0x80, (byte)2));
            Assert.Throws<OverflowException>(() => MultiplyOperatorsHelper<byte, byte, byte>.op_CheckedMultiply((byte)0xFF, (byte)2));
        }

        //
        // INumber
        //

        [Fact]
        public static void ClampTest()
        {
            Assert.Equal((byte)0x01, NumberHelper<byte>.Clamp((byte)0x00, (byte)0x01, (byte)0x3F));
            Assert.Equal((byte)0x01, NumberHelper<byte>.Clamp((byte)0x01, (byte)0x01, (byte)0x3F));
            Assert.Equal((byte)0x3F, NumberHelper<byte>.Clamp((byte)0x7F, (byte)0x01, (byte)0x3F));
            Assert.Equal((byte)0x3F, NumberHelper<byte>.Clamp((byte)0x80, (byte)0x01, (byte)0x3F));
            Assert.Equal((byte)0x3F, NumberHelper<byte>.Clamp((byte)0xFF, (byte)0x01, (byte)0x3F));
        }

        [Fact]
        public static void MaxTest()
        {
            Assert.Equal((byte)0x01, NumberHelper<byte>.Max((byte)0x00, (byte)1));
            Assert.Equal((byte)0x01, NumberHelper<byte>.Max((byte)0x01, (byte)1));
            Assert.Equal((byte)0x7F, NumberHelper<byte>.Max((byte)0x7F, (byte)1));
            Assert.Equal((byte)0x80, NumberHelper<byte>.Max((byte)0x80, (byte)1));
            Assert.Equal((byte)0xFF, NumberHelper<byte>.Max((byte)0xFF, (byte)1));
        }

        [Fact]
        public static void MaxNumberTest()
        {
            Assert.Equal((byte)0x01, NumberHelper<byte>.MaxNumber((byte)0x00, (byte)1));
            Assert.Equal((byte)0x01, NumberHelper<byte>.MaxNumber((byte)0x01, (byte)1));
            Assert.Equal((byte)0x7F, NumberHelper<byte>.MaxNumber((byte)0x7F, (byte)1));
            Assert.Equal((byte)0x80, NumberHelper<byte>.MaxNumber((byte)0x80, (byte)1));
            Assert.Equal((byte)0xFF, NumberHelper<byte>.MaxNumber((byte)0xFF, (byte)1));
        }

        [Fact]
        public static void MinTest()
        {
            Assert.Equal((byte)0x00, NumberHelper<byte>.Min((byte)0x00, (byte)1));
            Assert.Equal((byte)0x01, NumberHelper<byte>.Min((byte)0x01, (byte)1));
            Assert.Equal((byte)0x01, NumberHelper<byte>.Min((byte)0x7F, (byte)1));
            Assert.Equal((byte)0x01, NumberHelper<byte>.Min((byte)0x80, (byte)1));
            Assert.Equal((byte)0x01, NumberHelper<byte>.Min((byte)0xFF, (byte)1));
        }

        [Fact]
        public static void MinNumberTest()
        {
            Assert.Equal((byte)0x00, NumberHelper<byte>.MinNumber((byte)0x00, (byte)1));
            Assert.Equal((byte)0x01, NumberHelper<byte>.MinNumber((byte)0x01, (byte)1));
            Assert.Equal((byte)0x01, NumberHelper<byte>.MinNumber((byte)0x7F, (byte)1));
            Assert.Equal((byte)0x01, NumberHelper<byte>.MinNumber((byte)0x80, (byte)1));
            Assert.Equal((byte)0x01, NumberHelper<byte>.MinNumber((byte)0xFF, (byte)1));
        }

        [Fact]
        public static void SignTest()
        {
            Assert.Equal(0, NumberHelper<byte>.Sign((byte)0x00));
            Assert.Equal(1, NumberHelper<byte>.Sign((byte)0x01));
            Assert.Equal(1, NumberHelper<byte>.Sign((byte)0x7F));
            Assert.Equal(1, NumberHelper<byte>.Sign((byte)0x80));
            Assert.Equal(1, NumberHelper<byte>.Sign((byte)0xFF));
        }

        //
        // INumberBase
        //

        [Fact]
        public static void OneTest()
        {
            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.One);
        }

        [Fact]
        public static void RadixTest()
        {
            Assert.Equal(2, NumberBaseHelper<byte>.Radix);
        }

        [Fact]
        public static void ZeroTest()
        {
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.Zero);
        }

        [Fact]
        public static void AbsTest()
        {
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.Abs((byte)0x00));
            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.Abs((byte)0x01));
            Assert.Equal((byte)0x7F, NumberBaseHelper<byte>.Abs((byte)0x7F));
            Assert.Equal((byte)0x80, NumberBaseHelper<byte>.Abs((byte)0x80));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.Abs((byte)0xFF));
        }

        [Fact]
        public static void CreateCheckedFromByteTest()
        {
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateChecked<byte>(0x00));
            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.CreateChecked<byte>(0x01));
            Assert.Equal((byte)0x7F, NumberBaseHelper<byte>.CreateChecked<byte>(0x7F));
            Assert.Equal((byte)0x80, NumberBaseHelper<byte>.CreateChecked<byte>(0x80));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateChecked<byte>(0xFF));
        }

        [Fact]
        public static void CreateCheckedFromCharTest()
        {
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateChecked<char>((char)0x0000));
            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.CreateChecked<char>((char)0x0001));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<char>((char)0x7FFF));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<char>((char)0x8000));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateCheckedFromDecimalTest()
        {
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateChecked<decimal>(-0.0m));
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateChecked<decimal>(+0.0m));
            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.CreateChecked<decimal>(+1.0m));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<decimal>(decimal.MinValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<decimal>(decimal.MaxValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<decimal>(decimal.MinusOne));
        }

        [Fact]
        public static void CreateCheckedFromDoubleTest()
        {
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateChecked<double>(+0.0));
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateChecked<double>(-0.0));

            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateChecked<double>(-double.Epsilon));
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateChecked<double>(+double.Epsilon));

            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.CreateChecked<double>(+1.0));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateChecked<double>(+255.0));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<double>(-1.0));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<double>(+256.0));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<double>(double.PositiveInfinity));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<double>(double.NegativeInfinity));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<double>(double.MaxValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<double>(double.MinValue));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<double>(double.NaN));
        }

        [Fact]
        public static void CreateCheckedFromHalfTest()
        {
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateChecked<Half>(Half.Zero));
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateChecked<Half>(Half.NegativeZero));

            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateChecked<Half>(-Half.Epsilon));
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateChecked<Half>(+Half.Epsilon));

            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.CreateChecked<Half>(Half.One));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateChecked<Half>((Half)255.0f));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<Half>(Half.NegativeOne));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<Half>((Half)256.0f));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<Half>(Half.PositiveInfinity));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<Half>(Half.NegativeInfinity));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<Half>(Half.MaxValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<Half>(Half.MinValue));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<Half>(Half.NaN));
        }

        [Fact]
        public static void CreateCheckedFromInt16Test()
        {
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateChecked<short>(0x0000));
            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.CreateChecked<short>(0x0001));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<short>(0x7FFF));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<short>(unchecked((short)0x8000)));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromInt32Test()
        {
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateChecked<int>(0x00000000));
            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.CreateChecked<int>(0x00000001));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<int>(0x7FFFFFFF));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<int>(unchecked((int)0x80000000)));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromInt64Test()
        {
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateChecked<long>(0x0000000000000000));
            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.CreateChecked<long>(0x0000000000000001));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<long>(0x7FFFFFFFFFFFFFFF));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<long>(unchecked((long)0x8000000000000000)));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromInt128Test()
        {
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateChecked<Int128>(Int128.Zero));
            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.CreateChecked<Int128>(Int128.One));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<Int128>(Int128.MaxValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<Int128>(Int128.MinValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<Int128>(Int128.NegativeOne));
        }

        [Fact]
        public static void CreateCheckedFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateChecked<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal((byte)0x01, NumberBaseHelper<byte>.CreateChecked<nint>(unchecked((nint)0x0000000000000001)));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<nint>(unchecked((nint)0x8000000000000000)));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateChecked<nint>((nint)0x00000000));
                Assert.Equal((byte)0x01, NumberBaseHelper<byte>.CreateChecked<nint>((nint)0x00000001));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<nint>((nint)0x7FFFFFFF));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<nint>(unchecked((nint)0x80000000)));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateCheckedFromNFloatTest()
        {
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateChecked<NFloat>(0.0f));
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateChecked<NFloat>(NFloat.NegativeZero));

            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateChecked<NFloat>(-NFloat.Epsilon));
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateChecked<NFloat>(+NFloat.Epsilon));

            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.CreateChecked<NFloat>(+1.0f));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateChecked<NFloat>(+255.0f));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<NFloat>(-1.0f));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<NFloat>(+256.0f));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<NFloat>(NFloat.PositiveInfinity));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<NFloat>(NFloat.NegativeInfinity));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<NFloat>(NFloat.MaxValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<NFloat>(NFloat.MinValue));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<NFloat>(NFloat.NaN));
        }

        [Fact]
        public static void CreateCheckedFromSByteTest()
        {
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateChecked<sbyte>(0x00));
            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.CreateChecked<sbyte>(0x01));
            Assert.Equal((byte)0x7F, NumberBaseHelper<byte>.CreateChecked<sbyte>(0x7F));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<sbyte>(unchecked((sbyte)0x80)));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateCheckedFromSingleTest()
        {
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateChecked<float>(+0.0f));
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateChecked<float>(-0.0f));

            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateChecked<float>(-float.Epsilon));
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateChecked<float>(+float.Epsilon));

            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.CreateChecked<float>(+1.0f));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateChecked<float>(+255.0f));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<float>(-1.0f));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<float>(+256.0f));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<float>(float.PositiveInfinity));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<float>(float.NegativeInfinity));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<float>(float.MaxValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<float>(float.MinValue));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<float>(float.NaN));
        }

        [Fact]
        public static void CreateCheckedFromUInt16Test()
        {
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateChecked<ushort>(0x0000));
            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.CreateChecked<ushort>(0x0001));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<ushort>(0x7FFF));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<ushort>(0x8000));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUInt32Test()
        {
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateChecked<uint>(0x00000000));
            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.CreateChecked<uint>(0x00000001));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<uint>(0x7FFFFFFF));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<uint>(0x80000000));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUInt64Test()
        {
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateChecked<ulong>(0x0000000000000000));
            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.CreateChecked<ulong>(0x0000000000000001));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<ulong>(0x7FFFFFFFFFFFFFFF));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<ulong>(0x8000000000000000));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<ulong>(0xFFFFFFFFFFFFFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUInt128Test()
        {
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateChecked<UInt128>(UInt128.Zero));
            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.CreateChecked<UInt128>(UInt128.One));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<UInt128>(UInt128Tests_GenericMath.Int128MaxValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<UInt128>(UInt128Tests_GenericMath.Int128MaxValuePlusOne));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<UInt128>(UInt128.MaxValue));
        }

        [Fact]
        public static void CreateCheckedFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateChecked<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal((byte)0x01, NumberBaseHelper<byte>.CreateChecked<nuint>(unchecked((nuint)0x0000000000000001)));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<nuint>(unchecked((nuint)0x8000000000000000)));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateChecked<nuint>((nuint)0x00000000));
                Assert.Equal((byte)0x01, NumberBaseHelper<byte>.CreateChecked<nuint>((nuint)0x00000001));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<nuint>((nuint)0x7FFFFFFF));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<nuint>((nuint)0x80000000));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateSaturatingFromByteTest()
        {
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateSaturating<byte>(0x00));
            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.CreateSaturating<byte>(0x01));
            Assert.Equal((byte)0x7F, NumberBaseHelper<byte>.CreateSaturating<byte>(0x7F));
            Assert.Equal((byte)0x80, NumberBaseHelper<byte>.CreateSaturating<byte>(0x80));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateSaturating<byte>(0xFF));
        }

        [Fact]
        public static void CreateSaturatingFromCharTest()
        {
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateSaturating<char>((char)0x0000));
            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.CreateSaturating<char>((char)0x0001));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateSaturating<char>((char)0x7FFF));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateSaturating<char>((char)0x8000));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateSaturating<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromDecimalTest()
        {
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateSaturating<decimal>(-0.0m));
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateSaturating<decimal>(+0.0m));
            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.CreateSaturating<decimal>(+1.0m));

            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateSaturating<decimal>(decimal.MinValue));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateSaturating<decimal>(decimal.MaxValue));
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateSaturating<decimal>(decimal.MinusOne));
        }

        [Fact]
        public static void CreateSaturatingFromDoubleTest()
        {
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateSaturating<double>(+0.0));
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateSaturating<double>(-0.0));

            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateSaturating<double>(-double.Epsilon));
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateSaturating<double>(+double.Epsilon));

            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.CreateSaturating<double>(+1.0));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateSaturating<double>(+255.0));

            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateSaturating<double>(-1.0));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateSaturating<double>(+256.0));
                                     
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateSaturating<double>(double.PositiveInfinity));
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateSaturating<double>(double.NegativeInfinity));
                                     
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateSaturating<double>(double.MaxValue));
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateSaturating<double>(double.MinValue));
                                     
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateSaturating<double>(double.NaN));
        }

        [Fact]
        public static void CreateSaturatingFromHalfTest()
        {
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateSaturating<Half>(Half.Zero));
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateSaturating<Half>(Half.NegativeZero));

            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateSaturating<Half>(-Half.Epsilon));
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateSaturating<Half>(+Half.Epsilon));

            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.CreateSaturating<Half>(Half.One));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateSaturating<Half>((Half)255.0f));

            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateSaturating<Half>(Half.NegativeOne));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateSaturating<Half>((Half)256.0f));

            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateSaturating<Half>(Half.PositiveInfinity));
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateSaturating<Half>(Half.NegativeInfinity));

            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateSaturating<Half>(Half.MaxValue));
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateSaturating<Half>(Half.MinValue));

            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateSaturating<Half>(Half.NaN));
        }

        [Fact]
        public static void CreateSaturatingFromInt16Test()
        {
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateSaturating<short>(0x0000));
            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.CreateSaturating<short>(0x0001));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateSaturating<short>(0x7FFF));
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateSaturating<short>(unchecked((short)0x8000)));
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateSaturating<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromInt32Test()
        {
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateSaturating<int>(0x00000000));
            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.CreateSaturating<int>(0x00000001));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateSaturating<int>(0x7FFFFFFF));
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateSaturating<int>(unchecked((int)0x80000000)));
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateSaturating<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromInt64Test()
        {
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateSaturating<long>(0x0000000000000000));
            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.CreateSaturating<long>(0x0000000000000001));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateSaturating<long>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateSaturating<long>(unchecked((long)0x8000000000000000)));
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateSaturating<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromInt128Test()
        {
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateSaturating<Int128>(Int128.Zero));
            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.CreateSaturating<Int128>(Int128.One));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateSaturating<Int128>(Int128.MaxValue));
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateSaturating<Int128>(Int128.MinValue));
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateSaturating<Int128>(Int128.NegativeOne));
        }

        [Fact]
        public static void CreateSaturatingFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateSaturating<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal((byte)0x01, NumberBaseHelper<byte>.CreateSaturating<nint>(unchecked((nint)0x0000000000000001)));
                Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateSaturating<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateSaturating<nint>(unchecked((nint)0x8000000000000000)));
                Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateSaturating<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateSaturating<nint>((nint)0x00000000));
                Assert.Equal((byte)0x01, NumberBaseHelper<byte>.CreateSaturating<nint>((nint)0x00000001));
                Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateSaturating<nint>((nint)0x7FFFFFFF));
                Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateSaturating<nint>(unchecked((nint)0x80000000)));
                Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateSaturating<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateSaturatingFromNFloatTest()
        {
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateSaturating<NFloat>(0.0f));
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateSaturating<NFloat>(NFloat.NegativeZero));

            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateSaturating<NFloat>(-NFloat.Epsilon));
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateSaturating<NFloat>(+NFloat.Epsilon));

            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.CreateSaturating<NFloat>(+1.0f));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateSaturating<NFloat>(+255.0f));

            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateSaturating<NFloat>(-1.0f));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateSaturating<NFloat>(+256.0f));

            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateSaturating<NFloat>(NFloat.PositiveInfinity));
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateSaturating<NFloat>(NFloat.NegativeInfinity));

            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateSaturating<NFloat>(NFloat.MaxValue));
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateSaturating<NFloat>(NFloat.MinValue));

            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateSaturating<NFloat>(NFloat.NaN));
        }

        [Fact]
        public static void CreateSaturatingFromSByteTest()
        {
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateSaturating<sbyte>(0x00));
            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.CreateSaturating<sbyte>(0x01));
            Assert.Equal((byte)0x7F, NumberBaseHelper<byte>.CreateSaturating<sbyte>(0x7F));
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateSaturating<sbyte>(unchecked((sbyte)0x80)));
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateSaturating<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateSaturatingFromSingleTest()
        {
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateSaturating<float>(+0.0f));
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateSaturating<float>(-0.0f));

            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateSaturating<float>(-float.Epsilon));
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateSaturating<float>(+float.Epsilon));

            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.CreateSaturating<float>(+1.0f));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateSaturating<float>(+255.0f));

            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateSaturating<float>(-1.0f));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateSaturating<float>(+256.0f));

            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateSaturating<float>(float.PositiveInfinity));
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateSaturating<float>(float.NegativeInfinity));

            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateSaturating<float>(float.MaxValue));
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateSaturating<float>(float.MinValue));

            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateSaturating<float>(float.NaN));
        }

        [Fact]
        public static void CreateSaturatingFromUInt16Test()
        {
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateSaturating<ushort>(0x0000));
            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.CreateSaturating<ushort>(0x0001));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateSaturating<ushort>(0x7FFF));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateSaturating<ushort>(0x8000));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateSaturating<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUInt32Test()
        {
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateSaturating<uint>(0x00000000));
            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.CreateSaturating<uint>(0x00000001));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateSaturating<uint>(0x7FFFFFFF));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateSaturating<uint>(0x80000000));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateSaturating<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUInt64Test()
        {
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateSaturating<ulong>(0x0000000000000000));
            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.CreateSaturating<ulong>(0x0000000000000001));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateSaturating<ulong>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateSaturating<ulong>(0x8000000000000000));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateSaturating<ulong>(0xFFFFFFFFFFFFFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUInt128Test()
        {
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateSaturating<UInt128>(UInt128.Zero));
            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.CreateSaturating<UInt128>(UInt128.One));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateSaturating<UInt128>(UInt128Tests_GenericMath.Int128MaxValue));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateSaturating<UInt128>(UInt128Tests_GenericMath.Int128MaxValuePlusOne));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateSaturating<UInt128>(UInt128.MaxValue));
        }

        [Fact]
        public static void CreateSaturatingFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateSaturating<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal((byte)0x01, NumberBaseHelper<byte>.CreateSaturating<nuint>(unchecked((nuint)0x0000000000000001)));
                Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateSaturating<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateSaturating<nuint>(unchecked((nuint)0x8000000000000000)));
                Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateSaturating<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateSaturating<nuint>((nuint)0x00000000));
                Assert.Equal((byte)0x01, NumberBaseHelper<byte>.CreateSaturating<nuint>((nuint)0x00000001));
                Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateSaturating<nuint>((nuint)0x7FFFFFFF));
                Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateSaturating<nuint>((nuint)0x80000000));
                Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateSaturating<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateTruncatingFromByteTest()
        {
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateTruncating<byte>(0x00));
            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.CreateTruncating<byte>(0x01));
            Assert.Equal((byte)0x7F, NumberBaseHelper<byte>.CreateTruncating<byte>(0x7F));
            Assert.Equal((byte)0x80, NumberBaseHelper<byte>.CreateTruncating<byte>(0x80));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateTruncating<byte>(0xFF));
        }

        [Fact]
        public static void CreateTruncatingFromCharTest()
        {
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateTruncating<char>((char)0x0000));
            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.CreateTruncating<char>((char)0x0001));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateTruncating<char>((char)0x7FFF));
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateTruncating<char>((char)0x8000));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateTruncating<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromDecimalTest()
        {
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateTruncating<decimal>(-0.0m));
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateTruncating<decimal>(+0.0m));
            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.CreateTruncating<decimal>(+1.0m));

            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateTruncating<decimal>(decimal.MinValue));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateTruncating<decimal>(decimal.MaxValue));
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateTruncating<decimal>(decimal.MinusOne));
        }

        [Fact]
        public static void CreateTruncatingFromDoubleTest()
        {
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateTruncating<double>(+0.0));
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateTruncating<double>(-0.0));

            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateTruncating<double>(-double.Epsilon));
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateTruncating<double>(+double.Epsilon));

            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.CreateTruncating<double>(+1.0));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateTruncating<double>(+255.0));

            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateTruncating<double>(-1.0));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateTruncating<double>(+256.0));

            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateTruncating<double>(double.PositiveInfinity));
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateTruncating<double>(double.NegativeInfinity));

            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateTruncating<double>(double.MaxValue));
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateTruncating<double>(double.MinValue));

            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateTruncating<double>(double.NaN));
        }

        [Fact]
        public static void CreateTruncatingFromHalfTest()
        {
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateTruncating<Half>(Half.Zero));
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateTruncating<Half>(Half.NegativeZero));

            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateTruncating<Half>(-Half.Epsilon));
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateTruncating<Half>(+Half.Epsilon));

            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.CreateTruncating<Half>(Half.One));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateTruncating<Half>((Half)255.0f));

            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateTruncating<Half>(Half.NegativeOne));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateTruncating<Half>((Half)256.0f));

            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateTruncating<Half>(Half.PositiveInfinity));
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateTruncating<Half>(Half.NegativeInfinity));

            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateTruncating<Half>(Half.MaxValue));
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateTruncating<Half>(Half.MinValue));

            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateTruncating<Half>(Half.NaN));
        }

        [Fact]
        public static void CreateTruncatingFromInt16Test()
        {
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateTruncating<short>(0x0000));
            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.CreateTruncating<short>(0x0001));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateTruncating<short>(0x7FFF));
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateTruncating<short>(unchecked((short)0x8000)));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateTruncating<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateTruncatingFromInt32Test()
        {
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateTruncating<int>(0x00000000));
            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.CreateTruncating<int>(0x00000001));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateTruncating<int>(0x7FFFFFFF));
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateTruncating<int>(unchecked((int)0x80000000)));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateTruncating<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateTruncatingFromInt64Test()
        {
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateTruncating<long>(0x0000000000000000));
            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.CreateTruncating<long>(0x0000000000000001));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateTruncating<long>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateTruncating<long>(unchecked((long)0x8000000000000000)));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateTruncating<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void CreateTruncatingFromInt128Test()
        {
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateTruncating<Int128>(Int128.Zero));
            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.CreateTruncating<Int128>(Int128.One));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateTruncating<Int128>(Int128.MaxValue));
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateTruncating<Int128>(Int128.MinValue));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateTruncating<Int128>(Int128.NegativeOne));
        }

        [Fact]
        public static void CreateTruncatingFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateTruncating<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal((byte)0x01, NumberBaseHelper<byte>.CreateTruncating<nint>(unchecked((nint)0x0000000000000001)));
                Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateTruncating<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateTruncating<nint>(unchecked((nint)0x8000000000000000)));
                Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateTruncating<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateTruncating<nint>((nint)0x00000000));
                Assert.Equal((byte)0x01, NumberBaseHelper<byte>.CreateTruncating<nint>((nint)0x00000001));
                Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateTruncating<nint>((nint)0x7FFFFFFF));
                Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateTruncating<nint>(unchecked((nint)0x80000000)));
                Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateTruncating<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateTruncatingFromNFloatTest()
        {
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateTruncating<NFloat>(0.0f));
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateTruncating<NFloat>(NFloat.NegativeZero));

            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateTruncating<NFloat>(-NFloat.Epsilon));
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateTruncating<NFloat>(+NFloat.Epsilon));

            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.CreateTruncating<NFloat>(+1.0f));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateTruncating<NFloat>(+255.0f));

            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateTruncating<NFloat>(-1.0f));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateTruncating<NFloat>(+256.0f));

            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateTruncating<NFloat>(NFloat.PositiveInfinity));
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateTruncating<NFloat>(NFloat.NegativeInfinity));

            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateTruncating<NFloat>(NFloat.MaxValue));
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateTruncating<NFloat>(NFloat.MinValue));

            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateTruncating<NFloat>(NFloat.NaN));
        }

        [Fact]
        public static void CreateTruncatingFromSByteTest()
        {
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateTruncating<sbyte>(0x00));
            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.CreateTruncating<sbyte>(0x01));
            Assert.Equal((byte)0x7F, NumberBaseHelper<byte>.CreateTruncating<sbyte>(0x7F));
            Assert.Equal((byte)0x80, NumberBaseHelper<byte>.CreateTruncating<sbyte>(unchecked((sbyte)0x80)));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateTruncating<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateTruncatingFromSingleTest()
        {
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateTruncating<float>(+0.0f));
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateTruncating<float>(-0.0f));

            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateTruncating<float>(-float.Epsilon));
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateTruncating<float>(+float.Epsilon));

            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.CreateTruncating<float>(+1.0f));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateTruncating<float>(+255.0f));

            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateTruncating<float>(-1.0f));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateTruncating<float>(+256.0f));

            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateTruncating<float>(float.PositiveInfinity));
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateTruncating<float>(float.NegativeInfinity));

            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateTruncating<float>(float.MaxValue));
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateTruncating<float>(float.MinValue));

            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateTruncating<float>(float.NaN));
        }

        [Fact]
        public static void CreateTruncatingFromUInt16Test()
        {
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateTruncating<ushort>(0x0000));
            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.CreateTruncating<ushort>(0x0001));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateTruncating<ushort>(0x7FFF));
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateTruncating<ushort>(0x8000));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateTruncating<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt32Test()
        {
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateTruncating<uint>(0x00000000));
            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.CreateTruncating<uint>(0x00000001));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateTruncating<uint>(0x7FFFFFFF));
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateTruncating<uint>(0x80000000));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateTruncating<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt64Test()
        {
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateTruncating<ulong>(0x0000000000000000));
            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.CreateTruncating<ulong>(0x0000000000000001));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateTruncating<ulong>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateTruncating<ulong>(0x8000000000000000));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateTruncating<ulong>(0xFFFFFFFFFFFFFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt128Test()
        {
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateTruncating<UInt128>(UInt128.Zero));
            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.CreateTruncating<UInt128>(UInt128.One));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateTruncating<UInt128>(UInt128Tests_GenericMath.Int128MaxValue));
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateTruncating<UInt128>(UInt128Tests_GenericMath.Int128MaxValuePlusOne));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateTruncating<UInt128>(UInt128.MaxValue));
        }

        [Fact]
        public static void CreateTruncatingFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateTruncating<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal((byte)0x01, NumberBaseHelper<byte>.CreateTruncating<nuint>(unchecked((nuint)0x0000000000000001)));
                Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateTruncating<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateTruncating<nuint>(unchecked((nuint)0x8000000000000000)));
                Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateTruncating<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateTruncating<nuint>((nuint)0x00000000));
                Assert.Equal((byte)0x01, NumberBaseHelper<byte>.CreateTruncating<nuint>((nuint)0x00000001));
                Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateTruncating<nuint>((nuint)0x7FFFFFFF));
                Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateTruncating<nuint>((nuint)0x80000000));
                Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateTruncating<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void IsCanonicalTest()
        {
            Assert.True(NumberBaseHelper<byte>.IsCanonical((byte)0x00));
            Assert.True(NumberBaseHelper<byte>.IsCanonical((byte)0x01));
            Assert.True(NumberBaseHelper<byte>.IsCanonical((byte)0x7F));
            Assert.True(NumberBaseHelper<byte>.IsCanonical((byte)0x80));
            Assert.True(NumberBaseHelper<byte>.IsCanonical((byte)0xFF));
        }

        [Fact]
        public static void IsComplexNumberTest()
        {
            Assert.False(NumberBaseHelper<byte>.IsComplexNumber((byte)0x00));
            Assert.False(NumberBaseHelper<byte>.IsComplexNumber((byte)0x01));
            Assert.False(NumberBaseHelper<byte>.IsComplexNumber((byte)0x7F));
            Assert.False(NumberBaseHelper<byte>.IsComplexNumber((byte)0x80));
            Assert.False(NumberBaseHelper<byte>.IsComplexNumber((byte)0xFF));
        }

        [Fact]
        public static void IsEvenIntegerTest()
        {
            Assert.True(NumberBaseHelper<byte>.IsEvenInteger((byte)0x00));
            Assert.False(NumberBaseHelper<byte>.IsEvenInteger((byte)0x01));
            Assert.False(NumberBaseHelper<byte>.IsEvenInteger((byte)0x7F));
            Assert.True(NumberBaseHelper<byte>.IsEvenInteger((byte)0x80));
            Assert.False(NumberBaseHelper<byte>.IsEvenInteger((byte)0xFF));
        }

        [Fact]
        public static void IsFiniteTest()
        {
            Assert.True(NumberBaseHelper<byte>.IsFinite((byte)0x00));
            Assert.True(NumberBaseHelper<byte>.IsFinite((byte)0x01));
            Assert.True(NumberBaseHelper<byte>.IsFinite((byte)0x7F));
            Assert.True(NumberBaseHelper<byte>.IsFinite((byte)0x80));
            Assert.True(NumberBaseHelper<byte>.IsFinite((byte)0xFF));
        }

        [Fact]
        public static void IsImaginaryNumberTest()
        {
            Assert.False(NumberBaseHelper<byte>.IsImaginaryNumber((byte)0x00));
            Assert.False(NumberBaseHelper<byte>.IsImaginaryNumber((byte)0x01));
            Assert.False(NumberBaseHelper<byte>.IsImaginaryNumber((byte)0x7F));
            Assert.False(NumberBaseHelper<byte>.IsImaginaryNumber((byte)0x80));
            Assert.False(NumberBaseHelper<byte>.IsImaginaryNumber((byte)0xFF));
        }

        [Fact]
        public static void IsInfinityTest()
        {
            Assert.False(NumberBaseHelper<byte>.IsInfinity((byte)0x00));
            Assert.False(NumberBaseHelper<byte>.IsInfinity((byte)0x01));
            Assert.False(NumberBaseHelper<byte>.IsInfinity((byte)0x7F));
            Assert.False(NumberBaseHelper<byte>.IsInfinity((byte)0x80));
            Assert.False(NumberBaseHelper<byte>.IsInfinity((byte)0xFF));
        }

        [Fact]
        public static void IsIntegerTest()
        {
            Assert.True(NumberBaseHelper<byte>.IsInteger((byte)0x00));
            Assert.True(NumberBaseHelper<byte>.IsInteger((byte)0x01));
            Assert.True(NumberBaseHelper<byte>.IsInteger((byte)0x7F));
            Assert.True(NumberBaseHelper<byte>.IsInteger((byte)0x80));
            Assert.True(NumberBaseHelper<byte>.IsInteger((byte)0xFF));
        }

        [Fact]
        public static void IsNaNTest()
        {
            Assert.False(NumberBaseHelper<byte>.IsNaN((byte)0x00));
            Assert.False(NumberBaseHelper<byte>.IsNaN((byte)0x01));
            Assert.False(NumberBaseHelper<byte>.IsNaN((byte)0x7F));
            Assert.False(NumberBaseHelper<byte>.IsNaN((byte)0x80));
            Assert.False(NumberBaseHelper<byte>.IsNaN((byte)0xFF));
        }

        [Fact]
        public static void IsNegativeTest()
        {
            Assert.False(NumberBaseHelper<byte>.IsNegative((byte)0x00));
            Assert.False(NumberBaseHelper<byte>.IsNegative((byte)0x01));
            Assert.False(NumberBaseHelper<byte>.IsNegative((byte)0x7F));
            Assert.False(NumberBaseHelper<byte>.IsNegative((byte)0x80));
            Assert.False(NumberBaseHelper<byte>.IsNegative((byte)0xFF));
        }

        [Fact]
        public static void IsNegativeInfinityTest()
        {
            Assert.False(NumberBaseHelper<byte>.IsNegativeInfinity((byte)0x00));
            Assert.False(NumberBaseHelper<byte>.IsNegativeInfinity((byte)0x01));
            Assert.False(NumberBaseHelper<byte>.IsNegativeInfinity((byte)0x7F));
            Assert.False(NumberBaseHelper<byte>.IsNegativeInfinity((byte)0x80));
            Assert.False(NumberBaseHelper<byte>.IsNegativeInfinity((byte)0xFF));
        }

        [Fact]
        public static void IsNormalTest()
        {
            Assert.False(NumberBaseHelper<byte>.IsNormal((byte)0x00));
            Assert.True(NumberBaseHelper<byte>.IsNormal((byte)0x01));
            Assert.True(NumberBaseHelper<byte>.IsNormal((byte)0x7F));
            Assert.True(NumberBaseHelper<byte>.IsNormal((byte)0x80));
            Assert.True(NumberBaseHelper<byte>.IsNormal((byte)0xFF));
        }

        [Fact]
        public static void IsOddIntegerTest()
        {
            Assert.False(NumberBaseHelper<byte>.IsOddInteger((byte)0x00));
            Assert.True(NumberBaseHelper<byte>.IsOddInteger((byte)0x01));
            Assert.True(NumberBaseHelper<byte>.IsOddInteger((byte)0x7F));
            Assert.False(NumberBaseHelper<byte>.IsOddInteger((byte)0x80));
            Assert.True(NumberBaseHelper<byte>.IsOddInteger((byte)0xFF));
        }

        [Fact]
        public static void IsPositiveTest()
        {
            Assert.True(NumberBaseHelper<byte>.IsPositive((byte)0x00));
            Assert.True(NumberBaseHelper<byte>.IsPositive((byte)0x01));
            Assert.True(NumberBaseHelper<byte>.IsPositive((byte)0x7F));
            Assert.True(NumberBaseHelper<byte>.IsPositive((byte)0x80));
            Assert.True(NumberBaseHelper<byte>.IsPositive((byte)0xFF));
        }

        [Fact]
        public static void IsPositiveInfinityTest()
        {
            Assert.False(NumberBaseHelper<byte>.IsPositiveInfinity((byte)0x00));
            Assert.False(NumberBaseHelper<byte>.IsPositiveInfinity((byte)0x01));
            Assert.False(NumberBaseHelper<byte>.IsPositiveInfinity((byte)0x7F));
            Assert.False(NumberBaseHelper<byte>.IsPositiveInfinity((byte)0x80));
            Assert.False(NumberBaseHelper<byte>.IsPositiveInfinity((byte)0xFF));
        }

        [Fact]
        public static void IsRealNumberTest()
        {
            Assert.True(NumberBaseHelper<byte>.IsRealNumber((byte)0x00));
            Assert.True(NumberBaseHelper<byte>.IsRealNumber((byte)0x01));
            Assert.True(NumberBaseHelper<byte>.IsRealNumber((byte)0x7F));
            Assert.True(NumberBaseHelper<byte>.IsRealNumber((byte)0x80));
            Assert.True(NumberBaseHelper<byte>.IsRealNumber((byte)0xFF));
        }

        [Fact]
        public static void IsSubnormalTest()
        {
            Assert.False(NumberBaseHelper<byte>.IsSubnormal((byte)0x00));
            Assert.False(NumberBaseHelper<byte>.IsSubnormal((byte)0x01));
            Assert.False(NumberBaseHelper<byte>.IsSubnormal((byte)0x7F));
            Assert.False(NumberBaseHelper<byte>.IsSubnormal((byte)0x80));
            Assert.False(NumberBaseHelper<byte>.IsSubnormal((byte)0xFF));
        }

        [Fact]
        public static void IsZeroTest()
        {
            Assert.True(NumberBaseHelper<byte>.IsZero((byte)0x00));
            Assert.False(NumberBaseHelper<byte>.IsZero((byte)0x01));
            Assert.False(NumberBaseHelper<byte>.IsZero((byte)0x7F));
            Assert.False(NumberBaseHelper<byte>.IsZero((byte)0x80));
            Assert.False(NumberBaseHelper<byte>.IsZero((byte)0xFF));
        }

        [Fact]
        public static void MaxMagnitudeTest()
        {
            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.MaxMagnitude((byte)0x00, (byte)1));
            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.MaxMagnitude((byte)0x01, (byte)1));
            Assert.Equal((byte)0x7F, NumberBaseHelper<byte>.MaxMagnitude((byte)0x7F, (byte)1));
            Assert.Equal((byte)0x80, NumberBaseHelper<byte>.MaxMagnitude((byte)0x80, (byte)1));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.MaxMagnitude((byte)0xFF, (byte)1));
        }

        [Fact]
        public static void MaxMagnitudeNumberTest()
        {
            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.MaxMagnitudeNumber((byte)0x00, (byte)1));
            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.MaxMagnitudeNumber((byte)0x01, (byte)1));
            Assert.Equal((byte)0x7F, NumberBaseHelper<byte>.MaxMagnitudeNumber((byte)0x7F, (byte)1));
            Assert.Equal((byte)0x80, NumberBaseHelper<byte>.MaxMagnitudeNumber((byte)0x80, (byte)1));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.MaxMagnitudeNumber((byte)0xFF, (byte)1));
        }

        [Fact]
        public static void MinMagnitudeTest()
        {
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.MinMagnitude((byte)0x00, (byte)1));
            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.MinMagnitude((byte)0x01, (byte)1));
            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.MinMagnitude((byte)0x7F, (byte)1));
            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.MinMagnitude((byte)0x80, (byte)1));
            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.MinMagnitude((byte)0xFF, (byte)1));
        }

        [Fact]
        public static void MinMagnitudeNumberTest()
        {
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.MinMagnitudeNumber((byte)0x00, (byte)1));
            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.MinMagnitudeNumber((byte)0x01, (byte)1));
            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.MinMagnitudeNumber((byte)0x7F, (byte)1));
            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.MinMagnitudeNumber((byte)0x80, (byte)1));
            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.MinMagnitudeNumber((byte)0xFF, (byte)1));
        }

        //
        // IShiftOperators
        //

        [Fact]
        public static void op_LeftShiftTest()
        {
            Assert.Equal((byte)0x00, ShiftOperatorsHelper<byte, int, byte>.op_LeftShift((byte)0x00, 1));
            Assert.Equal((byte)0x02, ShiftOperatorsHelper<byte, int, byte>.op_LeftShift((byte)0x01, 1));
            Assert.Equal((byte)0xFE, ShiftOperatorsHelper<byte, int, byte>.op_LeftShift((byte)0x7F, 1));
            Assert.Equal((byte)0x00, ShiftOperatorsHelper<byte, int, byte>.op_LeftShift((byte)0x80, 1));
            Assert.Equal((byte)0xFE, ShiftOperatorsHelper<byte, int, byte>.op_LeftShift((byte)0xFF, 1));
        }

        [Fact]
        public static void op_RightShiftTest()
        {
            Assert.Equal((byte)0x00, ShiftOperatorsHelper<byte, int, byte>.op_RightShift((byte)0x00, 1));
            Assert.Equal((byte)0x00, ShiftOperatorsHelper<byte, int, byte>.op_RightShift((byte)0x01, 1));
            Assert.Equal((byte)0x3F, ShiftOperatorsHelper<byte, int, byte>.op_RightShift((byte)0x7F, 1));
            Assert.Equal((byte)0x40, ShiftOperatorsHelper<byte, int, byte>.op_RightShift((byte)0x80, 1));
            Assert.Equal((byte)0x7F, ShiftOperatorsHelper<byte, int, byte>.op_RightShift((byte)0xFF, 1));
        }

        [Fact]
        public static void op_UnsignedRightShiftTest()
        {
            Assert.Equal((byte)0x00, ShiftOperatorsHelper<byte, int, byte>.op_UnsignedRightShift((byte)0x00, 1));
            Assert.Equal((byte)0x00, ShiftOperatorsHelper<byte, int, byte>.op_UnsignedRightShift((byte)0x01, 1));
            Assert.Equal((byte)0x3F, ShiftOperatorsHelper<byte, int, byte>.op_UnsignedRightShift((byte)0x7F, 1));
            Assert.Equal((byte)0x40, ShiftOperatorsHelper<byte, int, byte>.op_UnsignedRightShift((byte)0x80, 1));
            Assert.Equal((byte)0x7F, ShiftOperatorsHelper<byte, int, byte>.op_UnsignedRightShift((byte)0xFF, 1));
        }

        //
        // ISubtractionOperators
        //

        [Fact]
        public static void op_SubtractionTest()
        {
            Assert.Equal((byte)0xFF, SubtractionOperatorsHelper<byte, byte, byte>.op_Subtraction((byte)0x00, (byte)1));
            Assert.Equal((byte)0x00, SubtractionOperatorsHelper<byte, byte, byte>.op_Subtraction((byte)0x01, (byte)1));
            Assert.Equal((byte)0x7E, SubtractionOperatorsHelper<byte, byte, byte>.op_Subtraction((byte)0x7F, (byte)1));
            Assert.Equal((byte)0x7F, SubtractionOperatorsHelper<byte, byte, byte>.op_Subtraction((byte)0x80, (byte)1));
            Assert.Equal((byte)0xFE, SubtractionOperatorsHelper<byte, byte, byte>.op_Subtraction((byte)0xFF, (byte)1));
        }

        [Fact]
        public static void op_CheckedSubtractionTest()
        {
            Assert.Equal((byte)0x00, SubtractionOperatorsHelper<byte, byte, byte>.op_CheckedSubtraction((byte)0x01, (byte)1));
            Assert.Equal((byte)0x7E, SubtractionOperatorsHelper<byte, byte, byte>.op_CheckedSubtraction((byte)0x7F, (byte)1));
            Assert.Equal((byte)0x7F, SubtractionOperatorsHelper<byte, byte, byte>.op_CheckedSubtraction((byte)0x80, (byte)1));
            Assert.Equal((byte)0xFE, SubtractionOperatorsHelper<byte, byte, byte>.op_CheckedSubtraction((byte)0xFF, (byte)1));

            Assert.Throws<OverflowException>(() => SubtractionOperatorsHelper<byte, byte, byte>.op_CheckedSubtraction((byte)0x00, (byte)1));
        }

        //
        // IUnaryNegationOperators
        //

        [Fact]
        public static void op_UnaryNegationTest()
        {
            Assert.Equal((byte)0x00, UnaryNegationOperatorsHelper<byte, byte>.op_UnaryNegation((byte)0x00));
            Assert.Equal((byte)0xFF, UnaryNegationOperatorsHelper<byte, byte>.op_UnaryNegation((byte)0x01));
            Assert.Equal((byte)0x81, UnaryNegationOperatorsHelper<byte, byte>.op_UnaryNegation((byte)0x7F));
            Assert.Equal((byte)0x80, UnaryNegationOperatorsHelper<byte, byte>.op_UnaryNegation((byte)0x80));
            Assert.Equal((byte)0x01, UnaryNegationOperatorsHelper<byte, byte>.op_UnaryNegation((byte)0xFF));
        }

        [Fact]
        public static void op_CheckedUnaryNegationTest()
        {
            Assert.Equal((byte)0x00, UnaryNegationOperatorsHelper<byte, byte>.op_CheckedUnaryNegation((byte)0x00));

            Assert.Throws<OverflowException>(() => UnaryNegationOperatorsHelper<byte, byte>.op_CheckedUnaryNegation((byte)0x01));
            Assert.Throws<OverflowException>(() => UnaryNegationOperatorsHelper<byte, byte>.op_CheckedUnaryNegation((byte)0x7F));
            Assert.Throws<OverflowException>(() => UnaryNegationOperatorsHelper<byte, byte>.op_CheckedUnaryNegation((byte)0x80));
            Assert.Throws<OverflowException>(() => UnaryNegationOperatorsHelper<byte, byte>.op_CheckedUnaryNegation((byte)0xFF));
        }

        //
        // IUnaryPlusOperators
        //

        [Fact]
        public static void op_UnaryPlusTest()
        {
            Assert.Equal((byte)0x00, UnaryPlusOperatorsHelper<byte, byte>.op_UnaryPlus((byte)0x00));
            Assert.Equal((byte)0x01, UnaryPlusOperatorsHelper<byte, byte>.op_UnaryPlus((byte)0x01));
            Assert.Equal((byte)0x7F, UnaryPlusOperatorsHelper<byte, byte>.op_UnaryPlus((byte)0x7F));
            Assert.Equal((byte)0x80, UnaryPlusOperatorsHelper<byte, byte>.op_UnaryPlus((byte)0x80));
            Assert.Equal((byte)0xFF, UnaryPlusOperatorsHelper<byte, byte>.op_UnaryPlus((byte)0xFF));
        }

        //
        // IParsable and ISpanParsable
        //

        [Theory]
        [MemberData(nameof(ByteTests.Parse_Valid_TestData), MemberType = typeof(ByteTests))]
        public static void ParseValidStringTest(string value, NumberStyles style, IFormatProvider provider, byte expected)
        {
            byte result;

            // Default style and provider
            if ((style == NumberStyles.Integer) && (provider is null))
            {
                Assert.True(ParsableHelper<byte>.TryParse(value, provider, out result));
                Assert.Equal(expected, result);
                Assert.Equal(expected, ParsableHelper<byte>.Parse(value, provider));
            }

            // Default provider
            if (provider is null)
            {
                Assert.Equal(expected, NumberBaseHelper<byte>.Parse(value, style, provider));

                // Substitute default NumberFormatInfo
                Assert.True(NumberBaseHelper<byte>.TryParse(value, style, new NumberFormatInfo(), out result));
                Assert.Equal(expected, result);
                Assert.Equal(expected, NumberBaseHelper<byte>.Parse(value, style, new NumberFormatInfo()));
            }

            // Default style
            if (style == NumberStyles.Integer)
            {
                Assert.Equal(expected, ParsableHelper<byte>.Parse(value, provider));
            }

            // Full overloads
            Assert.True(NumberBaseHelper<byte>.TryParse(value, style, provider, out result));
            Assert.Equal(expected, result);
            Assert.Equal(expected, NumberBaseHelper<byte>.Parse(value, style, provider));
        }

        [Theory]
        [MemberData(nameof(ByteTests.Parse_Invalid_TestData), MemberType = typeof(ByteTests))]
        public static void ParseInvalidStringTest(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            byte result;

            // Default style and provider
            if ((style == NumberStyles.Integer) && (provider is null))
            {
                Assert.False(ParsableHelper<byte>.TryParse(value, provider, out result));
                Assert.Equal(default(byte), result);
                Assert.Throws(exceptionType, () => ParsableHelper<byte>.Parse(value, provider));
            }

            // Default provider
            if (provider is null)
            {
                Assert.Throws(exceptionType, () => NumberBaseHelper<byte>.Parse(value, style, provider));

                // Substitute default NumberFormatInfo
                Assert.False(NumberBaseHelper<byte>.TryParse(value, style, new NumberFormatInfo(), out result));
                Assert.Equal(default(byte), result);
                Assert.Throws(exceptionType, () => NumberBaseHelper<byte>.Parse(value, style, new NumberFormatInfo()));
            }

            // Default style
            if (style == NumberStyles.Integer)
            {
                Assert.Throws(exceptionType, () => ParsableHelper<byte>.Parse(value, provider));
            }

            // Full overloads
            Assert.False(NumberBaseHelper<byte>.TryParse(value, style, provider, out result));
            Assert.Equal(default(byte), result);
            Assert.Throws(exceptionType, () => NumberBaseHelper<byte>.Parse(value, style, provider));
        }

        [Theory]
        [MemberData(nameof(ByteTests.Parse_ValidWithOffsetCount_TestData), MemberType = typeof(ByteTests))]
        public static void ParseValidSpanTest(string value, int offset, int count, NumberStyles style, IFormatProvider provider, byte expected)
        {
            byte result;

            // Default style and provider
            if ((style == NumberStyles.Integer) && (provider is null))
            {
                Assert.True(SpanParsableHelper<byte>.TryParse(value.AsSpan(offset, count), provider, out result));
                Assert.Equal(expected, result);
            }

            Assert.Equal(expected, NumberBaseHelper<byte>.Parse(value.AsSpan(offset, count), style, provider));

            Assert.True(NumberBaseHelper<byte>.TryParse(value.AsSpan(offset, count), style, provider, out result));
            Assert.Equal(expected, result);
        }

        [Theory]
        [MemberData(nameof(ByteTests.Parse_Invalid_TestData), MemberType = typeof(ByteTests))]
        public static void ParseInvalidSpanTest(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            if (value is null)
            {
                return;
            }

            byte result;

            // Default style and provider
            if ((style == NumberStyles.Integer) && (provider is null))
            {
                Assert.False(SpanParsableHelper<byte>.TryParse(value.AsSpan(), provider, out result));
                Assert.Equal(default(byte), result);
            }

            Assert.Throws(exceptionType, () => NumberBaseHelper<byte>.Parse(value.AsSpan(), style, provider));

            Assert.False(NumberBaseHelper<byte>.TryParse(value.AsSpan(), style, provider, out result));
            Assert.Equal(default(byte), result);
        }
    }
}
