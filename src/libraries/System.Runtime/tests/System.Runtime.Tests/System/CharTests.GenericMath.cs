// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Xunit;

namespace System.Tests
{
    public class CharTests_GenericMath
    {
        //
        // IAdditionOperators
        //

        [Fact]
        public static void op_AdditionTest()
        {
            Assert.Equal((char)0x0001, AdditionOperatorsHelper<char, char, char>.op_Addition((char)0x0000, (char)1));
            Assert.Equal((char)0x0002, AdditionOperatorsHelper<char, char, char>.op_Addition((char)0x0001, (char)1));
            Assert.Equal((char)0x8000, AdditionOperatorsHelper<char, char, char>.op_Addition((char)0x7FFF, (char)1));
            Assert.Equal((char)0x8001, AdditionOperatorsHelper<char, char, char>.op_Addition((char)0x8000, (char)1));
            Assert.Equal((char)0x0000, AdditionOperatorsHelper<char, char, char>.op_Addition((char)0xFFFF, (char)1));
        }

        [Fact]
        public static void op_CheckedAdditionTest()
        {
            Assert.Equal((char)0x0001, AdditionOperatorsHelper<char, char, char>.op_CheckedAddition((char)0x0000, (char)1));
            Assert.Equal((char)0x0002, AdditionOperatorsHelper<char, char, char>.op_CheckedAddition((char)0x0001, (char)1));
            Assert.Equal((char)0x8000, AdditionOperatorsHelper<char, char, char>.op_CheckedAddition((char)0x7FFF, (char)1));
            Assert.Equal((char)0x8001, AdditionOperatorsHelper<char, char, char>.op_CheckedAddition((char)0x8000, (char)1));

            Assert.Throws<OverflowException>(() => AdditionOperatorsHelper<char, char, char>.op_CheckedAddition((char)0xFFFF, (char)1));
        }

        //
        // IAdditiveIdentity
        //

        [Fact]
        public static void AdditiveIdentityTest()
        {
            Assert.Equal((char)0x0000, AdditiveIdentityHelper<char, char>.AdditiveIdentity);
        }

        //
        // IBinaryInteger
        //

        [Fact]
        public static void DivRemTest()
        {
            Assert.Equal(((char)0x0000, (char)0x0000), BinaryIntegerHelper<char>.DivRem((char)0x0000, (char)2));
            Assert.Equal(((char)0x0000, (char)0x0001), BinaryIntegerHelper<char>.DivRem((char)0x0001, (char)2));
            Assert.Equal(((char)0x3FFF, (char)0x0001), BinaryIntegerHelper<char>.DivRem((char)0x7FFF, (char)2));
            Assert.Equal(((char)0x4000, (char)0x0000), BinaryIntegerHelper<char>.DivRem((char)0x8000, (char)2));
            Assert.Equal(((char)0x7FFF, (char)0x0001), BinaryIntegerHelper<char>.DivRem((char)0xFFFF, (char)2));
        }

        [Fact]
        public static void LeadingZeroCountTest()
        {
            Assert.Equal((char)0x0010, BinaryIntegerHelper<char>.LeadingZeroCount((char)0x0000));
            Assert.Equal((char)0x000F, BinaryIntegerHelper<char>.LeadingZeroCount((char)0x0001));
            Assert.Equal((char)0x0001, BinaryIntegerHelper<char>.LeadingZeroCount((char)0x7FFF));
            Assert.Equal((char)0x0000, BinaryIntegerHelper<char>.LeadingZeroCount((char)0x8000));
            Assert.Equal((char)0x0000, BinaryIntegerHelper<char>.LeadingZeroCount((char)0xFFFF));
        }

        [Fact]
        public static void PopCountTest()
        {
            Assert.Equal((char)0x0000, BinaryIntegerHelper<char>.PopCount((char)0x0000));
            Assert.Equal((char)0x0001, BinaryIntegerHelper<char>.PopCount((char)0x0001));
            Assert.Equal((char)0x000F, BinaryIntegerHelper<char>.PopCount((char)0x7FFF));
            Assert.Equal((char)0x0001, BinaryIntegerHelper<char>.PopCount((char)0x8000));
            Assert.Equal((char)0x0010, BinaryIntegerHelper<char>.PopCount((char)0xFFFF));
        }

        [Fact]
        public static void RotateLeftTest()
        {
            Assert.Equal((char)0x0000, BinaryIntegerHelper<char>.RotateLeft((char)0x0000, 1));
            Assert.Equal((char)0x0002, BinaryIntegerHelper<char>.RotateLeft((char)0x0001, 1));
            Assert.Equal((char)0xFFFE, BinaryIntegerHelper<char>.RotateLeft((char)0x7FFF, 1));
            Assert.Equal((char)0x0001, BinaryIntegerHelper<char>.RotateLeft((char)0x8000, 1));
            Assert.Equal((char)0xFFFF, BinaryIntegerHelper<char>.RotateLeft((char)0xFFFF, 1));
        }

        [Fact]
        public static void RotateRightTest()
        {
            Assert.Equal((char)0x0000, BinaryIntegerHelper<char>.RotateRight((char)0x0000, 1));
            Assert.Equal((char)0x8000, BinaryIntegerHelper<char>.RotateRight((char)0x0001, 1));
            Assert.Equal((char)0xBFFF, BinaryIntegerHelper<char>.RotateRight((char)0x7FFF, 1));
            Assert.Equal((char)0x4000, BinaryIntegerHelper<char>.RotateRight((char)0x8000, 1));
            Assert.Equal((char)0xFFFF, BinaryIntegerHelper<char>.RotateRight((char)0xFFFF, 1));
        }

        [Fact]
        public static void TrailingZeroCountTest()
        {
            Assert.Equal((char)0x0010, BinaryIntegerHelper<char>.TrailingZeroCount((char)0x0000));
            Assert.Equal((char)0x0000, BinaryIntegerHelper<char>.TrailingZeroCount((char)0x0001));
            Assert.Equal((char)0x0000, BinaryIntegerHelper<char>.TrailingZeroCount((char)0x7FFF));
            Assert.Equal((char)0x000F, BinaryIntegerHelper<char>.TrailingZeroCount((char)0x8000));
            Assert.Equal((char)0x0000, BinaryIntegerHelper<char>.TrailingZeroCount((char)0xFFFF));
        }

        [Fact]
        public static void TryReadBigEndianByteTest()
        {
            char result;

            Assert.True(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0x00 }, isUnsigned: true, out result));
            Assert.Equal((char)0x0000, result);

            Assert.True(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0x01 }, isUnsigned: true, out result));
            Assert.Equal((char)0x0001, result);

            Assert.True(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0x7F }, isUnsigned: true, out result));
            Assert.Equal((char)0x007F, result);

            Assert.True(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0x80 }, isUnsigned: true, out result));
            Assert.Equal((char)0x0080, result);

            Assert.True(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0xFF }, isUnsigned: true, out result));
            Assert.Equal((char)0x00FF, result);
        }

        [Fact]
        public static void TryReadBigEndianInt16Test()
        {
            char result;

            Assert.True(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((char)0x0000, result);

            Assert.True(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal((char)0x0001, result);

            Assert.True(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal((char)0x0080, result);

            Assert.True(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0x01, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((char)0x0100, result);

            Assert.True(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0x7F, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((char)0x7FFF, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0x80, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((char)0x0000, result);
        }

        [Fact]
        public static void TryReadBigEndianInt32Test()
        {
            char result;

            Assert.True(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((char)0x0000, result);

            Assert.True(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal((char)0x0001, result);

            Assert.True(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal((char)0x0080, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0x01, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0x80, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((char)0x0000, result);
        }

        [Fact]
        public static void TryReadBigEndianInt64Test()
        {
            char result;

            Assert.True(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((char)0x0000, result);

            Assert.True(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal((char)0x0001, result);

            Assert.True(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal((char)0x0080, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((char)0x0000, result);
        }

        [Fact]
        public static void TryReadBigEndianInt96Test()
        {
            char result;

            Assert.True(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((char)0x0000, result);

            Assert.True(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal((char)0x0001, result);

            Assert.True(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal((char)0x0080, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((char)0x0000, result);
        }

        [Fact]
        public static void TryReadBigEndianInt128Test()
        {
            char result;

            Assert.True(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((char)0x0000, result);

            Assert.True(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal((char)0x0001, result);

            Assert.True(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal((char)0x0080, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((char)0x0000, result);
        }

        [Fact]
        public static void TryReadBigEndianSByteTest()
        {
            char result;

            Assert.True(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0x00 }, isUnsigned: false, out result));
            Assert.Equal((char)0x0000, result);

            Assert.True(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0x01 }, isUnsigned: false, out result));
            Assert.Equal((char)0x0001, result);

            Assert.True(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0x7F }, isUnsigned: false, out result));
            Assert.Equal((char)0x007F, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0x80 }, isUnsigned: false, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0xFF }, isUnsigned: false, out result));
            Assert.Equal((char)0x0000, result);
        }

        [Fact]
        public static void TryReadBigEndianUInt16Test()
        {
            char result;

            Assert.True(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((char)0x0000, result);

            Assert.True(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal((char)0x0001, result);

            Assert.True(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal((char)0x0080, result);

            Assert.True(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0x01, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((char)0x0100, result);

            Assert.True(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0x7F, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((char)0x7FFF, result);

            Assert.True(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0x80, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((char)0x8000, result);

            Assert.True(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal((char)0xFF7F, result);

            Assert.True(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((char)0xFFFF, result);
        }

        [Fact]
        public static void TryReadBigEndianUInt32Test()
        {
            char result;

            Assert.True(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((char)0x0000, result);

            Assert.True(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal((char)0x0001, result);

            Assert.True(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal((char)0x0080, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0x01, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0x80, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((char)0x0000, result);
        }

        [Fact]
        public static void TryReadBigEndianUInt64Test()
        {
            char result;

            Assert.True(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((char)0x0000, result);

            Assert.True(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal((char)0x0001, result);

            Assert.True(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal((char)0x0080, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((char)0x0000, result);
        }

        [Fact]
        public static void TryReadBigEndianUInt96Test()
        {
            char result;

            Assert.True(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((char)0x0000, result);

            Assert.True(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal((char)0x0001, result);

            Assert.True(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal((char)0x0080, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((char)0x0000, result);
        }

        [Fact]
        public static void TryReadBigEndianUInt128Test()
        {
            char result;

            Assert.True(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((char)0x0000, result);

            Assert.True(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal((char)0x0001, result);

            Assert.True(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal((char)0x0080, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((char)0x0000, result);
        }

        [Fact]
        public static void TryReadLittleEndianByteTest()
        {
            char result;

            Assert.True(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0x00 }, isUnsigned: true, out result));
            Assert.Equal((char)0x0000, result);

            Assert.True(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0x01 }, isUnsigned: true, out result));
            Assert.Equal((char)0x0001, result);

            Assert.True(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0x7F }, isUnsigned: true, out result));
            Assert.Equal((char)0x007F, result);

            Assert.True(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0x80 }, isUnsigned: true, out result));
            Assert.Equal((char)0x0080, result);

            Assert.True(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0xFF }, isUnsigned: true, out result));
            Assert.Equal((char)0x00FF, result);
        }

        [Fact]
        public static void TryReadLittleEndianInt16Test()
        {
            char result;

            Assert.True(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((char)0x0000, result);

            Assert.True(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal((char)0x0100, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal((char)0x0000, result);

            Assert.True(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0x01, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((char)0x0001, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((char)0x00, result);

            Assert.True(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0x80, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((char)0x0080, result);

            Assert.True(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal((char)0x7FFF, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((char)0x0000, result);
        }

        [Fact]
        public static void TryReadLittleEndianInt32Test()
        {
            char result;

            Assert.True(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal((char)0x0000, result);

            Assert.True(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0x01, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((char)0x0001, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((char)0x0000, result);

            Assert.True(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0x80, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((char)0x0080, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((char)0x0000, result);
        }

        [Fact]
        public static void TryReadLittleEndianInt64Test()
        {
            char result;

            Assert.True(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal((char)0x0000, result);

            Assert.True(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((char)0x0001, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((char)0x0000, result);

            Assert.True(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((char)0x0080, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((char)0x0000, result);
        }

        [Fact]
        public static void TryReadLittleEndianInt96Test()
        {
            char result;

            Assert.True(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal((char)0x0000, result);

            Assert.True(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((char)0x0001, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((char)0x0000, result);

            Assert.True(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((char)0x0080, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((char)0x0000, result);
        }

        [Fact]
        public static void TryReadLittleEndianInt128Test()
        {
            char result;

            Assert.True(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal((char)0x0000, result);

            Assert.True(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((char)0x0001, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((char)0x0000, result);

            Assert.True(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((char)0x0080, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((char)0x0000, result);
        }

        [Fact]
        public static void TryReadLittleEndianSByteTest()
        {
            char result;

            Assert.True(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0x00 }, isUnsigned: false, out result));
            Assert.Equal((char)0x0000, result);

            Assert.True(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0x01 }, isUnsigned: false, out result));
            Assert.Equal((char)0x0001, result);

            Assert.True(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0x7F }, isUnsigned: false, out result));
            Assert.Equal((char)0x007F, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0x80 }, isUnsigned: false, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0xFF }, isUnsigned: false, out result));
            Assert.Equal((char)0x0000, result);
        }

        [Fact]
        public static void TryReadLittleEndianUInt16Test()
        {
            char result;

            Assert.True(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((char)0x0000, result);

            Assert.True(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal((char)0x0100, result);

            Assert.True(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal((char)0x8000, result);

            Assert.True(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0x01, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((char)0x0001, result);

            Assert.True(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((char)0xFF7F, result);

            Assert.True(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0x80, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((char)0x0080, result);

            Assert.True(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal((char)0x7FFF, result);

            Assert.True(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((char)0xFFFF, result);
        }

        [Fact]
        public static void TryReadLittleEndianUInt32Test()
        {
            char result;

            Assert.True(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal((char)0x0000, result);

            Assert.True(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0x01, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((char)0x0001, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((char)0x0000, result);

            Assert.True(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0x80, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((char)0x0080, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((char)0x0000, result);
        }

        [Fact]
        public static void TryReadLittleEndianUInt64Test()
        {
            char result;

            Assert.True(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal((char)0x0000, result);

            Assert.True(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((char)0x0001, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((char)0x0000, result);

            Assert.True(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((char)0x0080, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((char)0x0000, result);
        }

        [Fact]
        public static void TryReadLittleEndianUInt96Test()
        {
            char result;

            Assert.True(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal((char)0x0000, result);

            Assert.True(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((char)0x0001, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((char)0x0000, result);

            Assert.True(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((char)0x0080, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((char)0x0000, result);
        }

        [Fact]
        public static void TryReadLittleEndianUInt128Test()
        {
            char result;

            Assert.True(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal((char)0x0000, result);

            Assert.True(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((char)0x0001, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((char)0x0000, result);

            Assert.True(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((char)0x0080, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(BinaryIntegerHelper<char>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((char)0x0000, result);
        }

        [Fact]
        public static void GetByteCountTest()
        {
            Assert.Equal(2, BinaryIntegerHelper<char>.GetByteCount((char)0x0000));
            Assert.Equal(2, BinaryIntegerHelper<char>.GetByteCount((char)0x0001));
            Assert.Equal(2, BinaryIntegerHelper<char>.GetByteCount((char)0x7FFF));
            Assert.Equal(2, BinaryIntegerHelper<char>.GetByteCount((char)0x8000));
            Assert.Equal(2, BinaryIntegerHelper<char>.GetByteCount((char)0xFFFF));
        }

        [Fact]
        public static void GetShortestBitLengthTest()
        {
            Assert.Equal(0x00, BinaryIntegerHelper<char>.GetShortestBitLength((char)0x0000));
            Assert.Equal(0x01, BinaryIntegerHelper<char>.GetShortestBitLength((char)0x0001));
            Assert.Equal(0x0F, BinaryIntegerHelper<char>.GetShortestBitLength((char)0x7FFF));
            Assert.Equal(0x10, BinaryIntegerHelper<char>.GetShortestBitLength((char)0x8000));
            Assert.Equal(0x10, BinaryIntegerHelper<char>.GetShortestBitLength((char)0xFFFF));
        }

        [Fact]
        public static void TryWriteBigEndianTest()
        {
            Span<byte> destination = stackalloc byte[2];
            int bytesWritten = 0;

            Assert.True(BinaryIntegerHelper<char>.TryWriteBigEndian((char)0x0000, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x00 }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<char>.TryWriteBigEndian((char)0x0001, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x01 }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<char>.TryWriteBigEndian((char)0x7FFF, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0x7F, 0xFF }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<char>.TryWriteBigEndian((char)0x8000, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0x80, 0x00 }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<char>.TryWriteBigEndian((char)0xFFFF, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0xFF }, destination.ToArray());

            Assert.False(BinaryIntegerHelper<char>.TryWriteBigEndian(default, Span<byte>.Empty, out bytesWritten));
            Assert.Equal(0, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0xFF }, destination.ToArray());
        }

        [Fact]
        public static void TryWriteLittleEndianTest()
        {
            Span<byte> destination = stackalloc byte[2];
            int bytesWritten = 0;

            Assert.True(BinaryIntegerHelper<char>.TryWriteLittleEndian((char)0x0000, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x00 }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<char>.TryWriteLittleEndian((char)0x0001, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0x01, 0x00 }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<char>.TryWriteLittleEndian((char)0x7FFF, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0x7F }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<char>.TryWriteLittleEndian((char)0x8000, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x80 }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<char>.TryWriteLittleEndian((char)0xFFFF, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0xFF }, destination.ToArray());

            Assert.False(BinaryIntegerHelper<char>.TryWriteLittleEndian(default, Span<byte>.Empty, out bytesWritten));
            Assert.Equal(0, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0xFF }, destination.ToArray());
        }

        //
        // IBinaryNumber
        //

        [Fact]
        public static void AllBitsSetTest()
        {
            Assert.Equal((char)0xFFFF, BinaryNumberHelper<char>.AllBitsSet);
            Assert.Equal((char)0, (char)~BinaryNumberHelper<char>.AllBitsSet);
        }

        [Fact]
        public static void IsPow2Test()
        {
            Assert.False(BinaryNumberHelper<char>.IsPow2((char)0x0000));
            Assert.True(BinaryNumberHelper<char>.IsPow2((char)0x0001));
            Assert.False(BinaryNumberHelper<char>.IsPow2((char)0x7FFF));
            Assert.True(BinaryNumberHelper<char>.IsPow2((char)0x8000));
            Assert.False(BinaryNumberHelper<char>.IsPow2((char)0xFFFF));
        }

        [Fact]
        public static void Log2Test()
        {
            Assert.Equal((char)0x0000, BinaryNumberHelper<char>.Log2((char)0x0000));
            Assert.Equal((char)0x0000, BinaryNumberHelper<char>.Log2((char)0x0001));
            Assert.Equal((char)0x000E, BinaryNumberHelper<char>.Log2((char)0x7FFF));
            Assert.Equal((char)0x000F, BinaryNumberHelper<char>.Log2((char)0x8000));
            Assert.Equal((char)0x000F, BinaryNumberHelper<char>.Log2((char)0xFFFF));
        }

        //
        // IBitwiseOperators
        //

        [Fact]
        public static void op_BitwiseAndTest()
        {
            Assert.Equal((char)0x0000, BitwiseOperatorsHelper<char, char, char>.op_BitwiseAnd((char)0x0000, (char)1));
            Assert.Equal((char)0x0001, BitwiseOperatorsHelper<char, char, char>.op_BitwiseAnd((char)0x0001, (char)1));
            Assert.Equal((char)0x0001, BitwiseOperatorsHelper<char, char, char>.op_BitwiseAnd((char)0x7FFF, (char)1));
            Assert.Equal((char)0x0000, BitwiseOperatorsHelper<char, char, char>.op_BitwiseAnd((char)0x8000, (char)1));
            Assert.Equal((char)0x0001, BitwiseOperatorsHelper<char, char, char>.op_BitwiseAnd((char)0xFFFF, (char)1));
        }

        [Fact]
        public static void op_BitwiseOrTest()
        {
            Assert.Equal((char)0x0001, BitwiseOperatorsHelper<char, char, char>.op_BitwiseOr((char)0x0000, (char)1));
            Assert.Equal((char)0x0001, BitwiseOperatorsHelper<char, char, char>.op_BitwiseOr((char)0x0001, (char)1));
            Assert.Equal((char)0x7FFF, BitwiseOperatorsHelper<char, char, char>.op_BitwiseOr((char)0x7FFF, (char)1));
            Assert.Equal((char)0x8001, BitwiseOperatorsHelper<char, char, char>.op_BitwiseOr((char)0x8000, (char)1));
            Assert.Equal((char)0xFFFF, BitwiseOperatorsHelper<char, char, char>.op_BitwiseOr((char)0xFFFF, (char)1));
        }

        [Fact]
        public static void op_ExclusiveOrTest()
        {
            Assert.Equal((char)0x0001, BitwiseOperatorsHelper<char, char, char>.op_ExclusiveOr((char)0x0000, (char)1));
            Assert.Equal((char)0x0000, BitwiseOperatorsHelper<char, char, char>.op_ExclusiveOr((char)0x0001, (char)1));
            Assert.Equal((char)0x7FFE, BitwiseOperatorsHelper<char, char, char>.op_ExclusiveOr((char)0x7FFF, (char)1));
            Assert.Equal((char)0x8001, BitwiseOperatorsHelper<char, char, char>.op_ExclusiveOr((char)0x8000, (char)1));
            Assert.Equal((char)0xFFFE, BitwiseOperatorsHelper<char, char, char>.op_ExclusiveOr((char)0xFFFF, (char)1));
        }

        [Fact]
        public static void op_OnesComplementTest()
        {
            Assert.Equal((char)0xFFFF, BitwiseOperatorsHelper<char, char, char>.op_OnesComplement((char)0x0000));
            Assert.Equal((char)0xFFFE, BitwiseOperatorsHelper<char, char, char>.op_OnesComplement((char)0x0001));
            Assert.Equal((char)0x8000, BitwiseOperatorsHelper<char, char, char>.op_OnesComplement((char)0x7FFF));
            Assert.Equal((char)0x7FFF, BitwiseOperatorsHelper<char, char, char>.op_OnesComplement((char)0x8000));
            Assert.Equal((char)0x0000, BitwiseOperatorsHelper<char, char, char>.op_OnesComplement((char)0xFFFF));
        }

        //
        // IComparisonOperators
        //

        [Fact]
        public static void op_GreaterThanTest()
        {
            Assert.False(ComparisonOperatorsHelper<char, char, bool>.op_GreaterThan((char)0x0000, (char)1));
            Assert.False(ComparisonOperatorsHelper<char, char, bool>.op_GreaterThan((char)0x0001, (char)1));
            Assert.True(ComparisonOperatorsHelper<char, char, bool>.op_GreaterThan((char)0x7FFF, (char)1));
            Assert.True(ComparisonOperatorsHelper<char, char, bool>.op_GreaterThan((char)0x8000, (char)1));
            Assert.True(ComparisonOperatorsHelper<char, char, bool>.op_GreaterThan((char)0xFFFF, (char)1));
        }

        [Fact]
        public static void op_GreaterThanOrEqualTest()
        {
            Assert.False(ComparisonOperatorsHelper<char, char, bool>.op_GreaterThanOrEqual((char)0x0000, (char)1));
            Assert.True(ComparisonOperatorsHelper<char, char, bool>.op_GreaterThanOrEqual((char)0x0001, (char)1));
            Assert.True(ComparisonOperatorsHelper<char, char, bool>.op_GreaterThanOrEqual((char)0x7FFF, (char)1));
            Assert.True(ComparisonOperatorsHelper<char, char, bool>.op_GreaterThanOrEqual((char)0x8000, (char)1));
            Assert.True(ComparisonOperatorsHelper<char, char, bool>.op_GreaterThanOrEqual((char)0xFFFF, (char)1));
        }

        [Fact]
        public static void op_LessThanTest()
        {
            Assert.True(ComparisonOperatorsHelper<char, char, bool>.op_LessThan((char)0x0000, (char)1));
            Assert.False(ComparisonOperatorsHelper<char, char, bool>.op_LessThan((char)0x0001, (char)1));
            Assert.False(ComparisonOperatorsHelper<char, char, bool>.op_LessThan((char)0x7FFF, (char)1));
            Assert.False(ComparisonOperatorsHelper<char, char, bool>.op_LessThan((char)0x8000, (char)1));
            Assert.False(ComparisonOperatorsHelper<char, char, bool>.op_LessThan((char)0xFFFF, (char)1));
        }

        [Fact]
        public static void op_LessThanOrEqualTest()
        {
            Assert.True(ComparisonOperatorsHelper<char, char, bool>.op_LessThanOrEqual((char)0x0000, (char)1));
            Assert.True(ComparisonOperatorsHelper<char, char, bool>.op_LessThanOrEqual((char)0x0001, (char)1));
            Assert.False(ComparisonOperatorsHelper<char, char, bool>.op_LessThanOrEqual((char)0x7FFF, (char)1));
            Assert.False(ComparisonOperatorsHelper<char, char, bool>.op_LessThanOrEqual((char)0x8000, (char)1));
            Assert.False(ComparisonOperatorsHelper<char, char, bool>.op_LessThanOrEqual((char)0xFFFF, (char)1));
        }

        //
        // IDecrementOperators
        //

        [Fact]
        public static void op_DecrementTest()
        {
            Assert.Equal((char)0xFFFF, DecrementOperatorsHelper<char>.op_Decrement((char)0x0000));
            Assert.Equal((char)0x0000, DecrementOperatorsHelper<char>.op_Decrement((char)0x0001));
            Assert.Equal((char)0x7FFE, DecrementOperatorsHelper<char>.op_Decrement((char)0x7FFF));
            Assert.Equal((char)0x7FFF, DecrementOperatorsHelper<char>.op_Decrement((char)0x8000));
            Assert.Equal((char)0xFFFE, DecrementOperatorsHelper<char>.op_Decrement((char)0xFFFF));
        }

        [Fact]
        public static void op_CheckedDecrementTest()
        {
            Assert.Equal((char)0x0000, DecrementOperatorsHelper<char>.op_CheckedDecrement((char)0x0001));
            Assert.Equal((char)0x7FFE, DecrementOperatorsHelper<char>.op_CheckedDecrement((char)0x7FFF));
            Assert.Equal((char)0x7FFF, DecrementOperatorsHelper<char>.op_CheckedDecrement((char)0x8000));
            Assert.Equal((char)0xFFFE, DecrementOperatorsHelper<char>.op_CheckedDecrement((char)0xFFFF));

            Assert.Throws<OverflowException>(() => DecrementOperatorsHelper<char>.op_CheckedDecrement((char)0x0000));
        }

        //
        // IDivisionOperators
        //

        [Fact]
        public static void op_DivisionTest()
        {
            Assert.Equal((char)0x0000, DivisionOperatorsHelper<char, char, char>.op_Division((char)0x0000, (char)2));
            Assert.Equal((char)0x0000, DivisionOperatorsHelper<char, char, char>.op_Division((char)0x0001, (char)2));
            Assert.Equal((char)0x3FFF, DivisionOperatorsHelper<char, char, char>.op_Division((char)0x7FFF, (char)2));
            Assert.Equal((char)0x4000, DivisionOperatorsHelper<char, char, char>.op_Division((char)0x8000, (char)2));
            Assert.Equal((char)0x7FFF, DivisionOperatorsHelper<char, char, char>.op_Division((char)0xFFFF, (char)2));

            Assert.Throws<DivideByZeroException>(() => DivisionOperatorsHelper<char, char, char>.op_Division((char)0x0001, (char)0));
        }

        [Fact]
        public static void op_CheckedDivisionTest()
        {
            Assert.Equal((char)0x0000, DivisionOperatorsHelper<char, char, char>.op_CheckedDivision((char)0x0000, (char)2));
            Assert.Equal((char)0x0000, DivisionOperatorsHelper<char, char, char>.op_CheckedDivision((char)0x0001, (char)2));
            Assert.Equal((char)0x3FFF, DivisionOperatorsHelper<char, char, char>.op_CheckedDivision((char)0x7FFF, (char)2));
            Assert.Equal((char)0x4000, DivisionOperatorsHelper<char, char, char>.op_CheckedDivision((char)0x8000, (char)2));
            Assert.Equal((char)0x7FFF, DivisionOperatorsHelper<char, char, char>.op_CheckedDivision((char)0xFFFF, (char)2));

            Assert.Throws<DivideByZeroException>(() => DivisionOperatorsHelper<char, char, char>.op_CheckedDivision((char)0x0001, (char)0));
        }

        //
        // IEqualityOperators
        //

        [Fact]
        public static void op_EqualityTest()
        {
            Assert.False(EqualityOperatorsHelper<char, char, bool>.op_Equality((char)0x0000, (char)1));
            Assert.True(EqualityOperatorsHelper<char, char, bool>.op_Equality((char)0x0001, (char)1));
            Assert.False(EqualityOperatorsHelper<char, char, bool>.op_Equality((char)0x7FFF, (char)1));
            Assert.False(EqualityOperatorsHelper<char, char, bool>.op_Equality((char)0x8000, (char)1));
            Assert.False(EqualityOperatorsHelper<char, char, bool>.op_Equality((char)0xFFFF, (char)1));
        }

        [Fact]
        public static void op_InequalityTest()
        {
            Assert.True(EqualityOperatorsHelper<char, char, bool>.op_Inequality((char)0x0000, (char)1));
            Assert.False(EqualityOperatorsHelper<char, char, bool>.op_Inequality((char)0x0001, (char)1));
            Assert.True(EqualityOperatorsHelper<char, char, bool>.op_Inequality((char)0x7FFF, (char)1));
            Assert.True(EqualityOperatorsHelper<char, char, bool>.op_Inequality((char)0x8000, (char)1));
            Assert.True(EqualityOperatorsHelper<char, char, bool>.op_Inequality((char)0xFFFF, (char)1));
        }

        //
        // IIncrementOperators
        //

        [Fact]
        public static void op_IncrementTest()
        {
            Assert.Equal((char)0x0001, IncrementOperatorsHelper<char>.op_Increment((char)0x0000));
            Assert.Equal((char)0x0002, IncrementOperatorsHelper<char>.op_Increment((char)0x0001));
            Assert.Equal((char)0x8000, IncrementOperatorsHelper<char>.op_Increment((char)0x7FFF));
            Assert.Equal((char)0x8001, IncrementOperatorsHelper<char>.op_Increment((char)0x8000));
            Assert.Equal((char)0x0000, IncrementOperatorsHelper<char>.op_Increment((char)0xFFFF));
        }

        [Fact]
        public static void op_CheckedIncrementTest()
        {
            Assert.Equal((char)0x0001, IncrementOperatorsHelper<char>.op_CheckedIncrement((char)0x0000));
            Assert.Equal((char)0x0002, IncrementOperatorsHelper<char>.op_CheckedIncrement((char)0x0001));
            Assert.Equal((char)0x8000, IncrementOperatorsHelper<char>.op_CheckedIncrement((char)0x7FFF));
            Assert.Equal((char)0x8001, IncrementOperatorsHelper<char>.op_CheckedIncrement((char)0x8000));

            Assert.Throws<OverflowException>(() => IncrementOperatorsHelper<char>.op_CheckedIncrement((char)0xFFFF));
        }

        //
        // IMinMaxValue
        //

        [Fact]
        public static void MaxValueTest()
        {
            Assert.Equal((char)0xFFFF, MinMaxValueHelper<char>.MaxValue);
        }

        [Fact]
        public static void MinValueTest()
        {
            Assert.Equal((char)0x0000, MinMaxValueHelper<char>.MinValue);
        }

        //
        // IModulusOperators
        //

        [Fact]
        public static void op_ModulusTest()
        {
            Assert.Equal((char)0x0000, ModulusOperatorsHelper<char, char, char>.op_Modulus((char)0x0000, (char)2));
            Assert.Equal((char)0x0001, ModulusOperatorsHelper<char, char, char>.op_Modulus((char)0x0001, (char)2));
            Assert.Equal((char)0x0001, ModulusOperatorsHelper<char, char, char>.op_Modulus((char)0x7FFF, (char)2));
            Assert.Equal((char)0x0000, ModulusOperatorsHelper<char, char, char>.op_Modulus((char)0x8000, (char)2));
            Assert.Equal((char)0x0001, ModulusOperatorsHelper<char, char, char>.op_Modulus((char)0xFFFF, (char)2));

            Assert.Throws<DivideByZeroException>(() => ModulusOperatorsHelper<char, char, char>.op_Modulus((char)0x0001, (char)0));
        }

        //
        // IMultiplicativeIdentity
        //

        [Fact]
        public static void MultiplicativeIdentityTest()
        {
            Assert.Equal((char)0x0001, MultiplicativeIdentityHelper<char, char>.MultiplicativeIdentity);
        }

        //
        // IMultiplyOperators
        //

        [Fact]
        public static void op_MultiplyTest()
        {
            Assert.Equal((char)0x0000, MultiplyOperatorsHelper<char, char, char>.op_Multiply((char)0x0000, (char)2));
            Assert.Equal((char)0x0002, MultiplyOperatorsHelper<char, char, char>.op_Multiply((char)0x0001, (char)2));
            Assert.Equal((char)0xFFFE, MultiplyOperatorsHelper<char, char, char>.op_Multiply((char)0x7FFF, (char)2));
            Assert.Equal((char)0x0000, MultiplyOperatorsHelper<char, char, char>.op_Multiply((char)0x8000, (char)2));
            Assert.Equal((char)0xFFFE, MultiplyOperatorsHelper<char, char, char>.op_Multiply((char)0xFFFF, (char)2));
        }

        [Fact]
        public static void op_CheckedMultiplyTest()
        {
            Assert.Equal((char)0x0000, MultiplyOperatorsHelper<char, char, char>.op_CheckedMultiply((char)0x0000, (char)2));
            Assert.Equal((char)0x0002, MultiplyOperatorsHelper<char, char, char>.op_CheckedMultiply((char)0x0001, (char)2));
            Assert.Equal((char)0xFFFE, MultiplyOperatorsHelper<char, char, char>.op_CheckedMultiply((char)0x7FFF, (char)2));

            Assert.Throws<OverflowException>(() => MultiplyOperatorsHelper<char, char, char>.op_CheckedMultiply((char)0x8000, (char)2));
            Assert.Throws<OverflowException>(() => MultiplyOperatorsHelper<char, char, char>.op_CheckedMultiply((char)0xFFFF, (char)2));
        }

        //
        // INumber
        //

        [Fact]
        public static void ClampTest()
        {
            Assert.Equal((char)0x0001, NumberHelper<char>.Clamp((char)0x0000, (char)0x0001, (char)0x003F));
            Assert.Equal((char)0x0001, NumberHelper<char>.Clamp((char)0x0001, (char)0x0001, (char)0x003F));
            Assert.Equal((char)0x003F, NumberHelper<char>.Clamp((char)0x7FFF, (char)0x0001, (char)0x003F));
            Assert.Equal((char)0x003F, NumberHelper<char>.Clamp((char)0x8000, (char)0x0001, (char)0x003F));
            Assert.Equal((char)0x003F, NumberHelper<char>.Clamp((char)0xFFFF, (char)0x0001, (char)0x003F));
        }

        [Fact]
        public static void MaxTest()
        {
            Assert.Equal((char)0x0001, NumberHelper<char>.Max((char)0x0000, (char)1));
            Assert.Equal((char)0x0001, NumberHelper<char>.Max((char)0x0001, (char)1));
            Assert.Equal((char)0x7FFF, NumberHelper<char>.Max((char)0x7FFF, (char)1));
            Assert.Equal((char)0x8000, NumberHelper<char>.Max((char)0x8000, (char)1));
            Assert.Equal((char)0xFFFF, NumberHelper<char>.Max((char)0xFFFF, (char)1));
        }

        [Fact]
        public static void MaxNumberTest()
        {
            Assert.Equal((char)0x0001, NumberHelper<char>.MaxNumber((char)0x0000, (char)1));
            Assert.Equal((char)0x0001, NumberHelper<char>.MaxNumber((char)0x0001, (char)1));
            Assert.Equal((char)0x7FFF, NumberHelper<char>.MaxNumber((char)0x7FFF, (char)1));
            Assert.Equal((char)0x8000, NumberHelper<char>.MaxNumber((char)0x8000, (char)1));
            Assert.Equal((char)0xFFFF, NumberHelper<char>.MaxNumber((char)0xFFFF, (char)1));
        }

        [Fact]
        public static void MinTest()
        {
            Assert.Equal((char)0x0000, NumberHelper<char>.Min((char)0x0000, (char)1));
            Assert.Equal((char)0x0001, NumberHelper<char>.Min((char)0x0001, (char)1));
            Assert.Equal((char)0x0001, NumberHelper<char>.Min((char)0x7FFF, (char)1));
            Assert.Equal((char)0x0001, NumberHelper<char>.Min((char)0x8000, (char)1));
            Assert.Equal((char)0x0001, NumberHelper<char>.Min((char)0xFFFF, (char)1));
        }

        [Fact]
        public static void MinNumberTest()
        {
            Assert.Equal((char)0x0000, NumberHelper<char>.MinNumber((char)0x0000, (char)1));
            Assert.Equal((char)0x0001, NumberHelper<char>.MinNumber((char)0x0001, (char)1));
            Assert.Equal((char)0x0001, NumberHelper<char>.MinNumber((char)0x7FFF, (char)1));
            Assert.Equal((char)0x0001, NumberHelper<char>.MinNumber((char)0x8000, (char)1));
            Assert.Equal((char)0x0001, NumberHelper<char>.MinNumber((char)0xFFFF, (char)1));
        }

        [Fact]
        public static void SignTest()
        {
            Assert.Equal(0, NumberHelper<char>.Sign((char)0x0000));
            Assert.Equal(1, NumberHelper<char>.Sign((char)0x0001));
            Assert.Equal(1, NumberHelper<char>.Sign((char)0x7FFF));
            Assert.Equal(1, NumberHelper<char>.Sign((char)0x8000));
            Assert.Equal(1, NumberHelper<char>.Sign((char)0xFFFF));
        }

        //
        // INumberBase
        //

        [Fact]
        public static void OneTest()
        {
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.One);
        }

        [Fact]
        public static void RadixTest()
        {
            Assert.Equal(2, NumberBaseHelper<char>.Radix);
        }

        [Fact]
        public static void ZeroTest()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.Zero);
        }

        [Fact]
        public static void AbsTest()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.Abs((char)0x0000));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.Abs((char)0x0001));
            Assert.Equal((char)0x7FFF, NumberBaseHelper<char>.Abs((char)0x7FFF));
            Assert.Equal((char)0x8000, NumberBaseHelper<char>.Abs((char)0x8000));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.Abs((char)0xFFFF));
        }

        [Fact]
        public static void CreateCheckedFromByteTest()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateChecked<byte>(0x00));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateChecked<byte>(0x01));
            Assert.Equal((char)0x007F, NumberBaseHelper<char>.CreateChecked<byte>(0x7F));
            Assert.Equal((char)0x0080, NumberBaseHelper<char>.CreateChecked<byte>(0x80));
            Assert.Equal((char)0x00FF, NumberBaseHelper<char>.CreateChecked<byte>(0xFF));
        }

        [Fact]
        public static void CreateCheckedFromCharTest()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateChecked<char>((char)0x0000));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateChecked<char>((char)0x0001));
            Assert.Equal((char)0x7FFF, NumberBaseHelper<char>.CreateChecked<char>((char)0x7FFF));
            Assert.Equal((char)0x8000, NumberBaseHelper<char>.CreateChecked<char>((char)0x8000));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateChecked<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateCheckedFromDecimalTest()
        {
            Assert.Equal((char)0x00, NumberBaseHelper<char>.CreateChecked<decimal>(-0.0m));
            Assert.Equal((char)0x00, NumberBaseHelper<char>.CreateChecked<decimal>(+0.0m));
            Assert.Equal((char)0x01, NumberBaseHelper<char>.CreateChecked<decimal>(+1.0m));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<decimal>(decimal.MinValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<decimal>(decimal.MaxValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<decimal>(decimal.MinusOne));
        }

        [Fact]
        public static void CreateCheckedFromDoubleTest()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateChecked<double>(+0.0));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateChecked<double>(-0.0));


            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateChecked<double>(-double.Epsilon));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateChecked<double>(+double.Epsilon));

            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateChecked<double>(+1.0));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateChecked<double>(+65535.0));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<double>(-1.0));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<double>(+65536.0));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<double>(double.PositiveInfinity));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<double>(double.NegativeInfinity));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<double>(double.MaxValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<double>(double.MinValue));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<double>(double.NaN));
        }

        [Fact]
        public static void CreateCheckedFromHalfTest()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateChecked<Half>(Half.Zero));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateChecked<Half>(Half.NegativeZero));

            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateChecked<Half>(-Half.Epsilon));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateChecked<Half>(+Half.Epsilon));

            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateChecked<Half>(Half.One));
            Assert.Equal((char)0xFFE0, NumberBaseHelper<char>.CreateChecked<Half>(Half.MaxValue));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<Half>(Half.NegativeOne));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<Half>(Half.PositiveInfinity));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<Half>(Half.NegativeInfinity));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<Half>(Half.MinValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<Half>(Half.NaN));
        }

        [Fact]
        public static void CreateCheckedFromInt16Test()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateChecked<short>(0x0000));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateChecked<short>(0x0001));
            Assert.Equal((char)0x7FFF, NumberBaseHelper<char>.CreateChecked<short>(0x7FFF));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<short>(unchecked((short)0x8000)));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromInt32Test()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateChecked<int>(0x00000000));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateChecked<int>(0x00000001));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<int>(0x7FFFFFFF));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<int>(unchecked((int)0x80000000)));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromInt64Test()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateChecked<long>(0x0000000000000000));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateChecked<long>(0x0000000000000001));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<long>(0x7FFFFFFFFFFFFFFF));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<long>(unchecked((long)0x8000000000000000)));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromInt128Test()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateChecked<Int128>(Int128.Zero));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateChecked<Int128>(Int128.One));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<Int128>(Int128.MaxValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<Int128>(Int128.MinValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<Int128>(Int128.NegativeOne));
        }

        [Fact]
        public static void CreateCheckedFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateChecked<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateChecked<nint>(unchecked((nint)0x0000000000000001)));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<nint>(unchecked((nint)0x8000000000000000)));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateChecked<nint>((nint)0x00000000));
                Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateChecked<nint>((nint)0x00000001));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<nint>((nint)0x7FFFFFFF));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<nint>(unchecked((nint)0x80000000)));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateCheckedFromNFloatTest()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateChecked<NFloat>(0.0f));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateChecked<NFloat>(NFloat.NegativeZero));

            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateChecked<NFloat>(-NFloat.Epsilon));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateChecked<NFloat>(+NFloat.Epsilon));

            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateChecked<NFloat>(1.0f));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateChecked<NFloat>(65535.0f));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<NFloat>(-1.0f));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<NFloat>(+65536.0f));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<NFloat>(NFloat.PositiveInfinity));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<NFloat>(NFloat.NegativeInfinity));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<NFloat>(NFloat.MaxValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<NFloat>(NFloat.MinValue));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<NFloat>(NFloat.NaN));
        }

        [Fact]
        public static void CreateCheckedFromSByteTest()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateChecked<sbyte>(0x00));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateChecked<sbyte>(0x01));
            Assert.Equal((char)0x007F, NumberBaseHelper<char>.CreateChecked<sbyte>(0x7F));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<sbyte>(unchecked((sbyte)0x80)));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateCheckedFromSingleTest()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateChecked<float>(+0.0f));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateChecked<float>(-0.0f));

            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateChecked<float>(-float.Epsilon));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateChecked<float>(-float.Epsilon));

            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateChecked<float>(+1.0f));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateChecked<float>(+65535.0f));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<float>(-1.0f));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<float>(+65536.0f));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<float>(float.PositiveInfinity));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<float>(float.NegativeInfinity));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<float>(float.MaxValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<float>(float.MinValue));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<float>(float.NaN));
        }

        [Fact]
        public static void CreateCheckedFromUInt16Test()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateChecked<ushort>(0x0000));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateChecked<ushort>(0x0001));
            Assert.Equal((char)0x7FFF, NumberBaseHelper<char>.CreateChecked<ushort>(0x7FFF));
            Assert.Equal((char)0x8000, NumberBaseHelper<char>.CreateChecked<ushort>(0x8000));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateChecked<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUInt32Test()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateChecked<uint>(0x00000000));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateChecked<uint>(0x00000001));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<uint>(0x7FFFFFFF));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<uint>(0x80000000));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUInt64Test()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateChecked<ulong>(0x0000000000000000));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateChecked<ulong>(0x0000000000000001));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<ulong>(0x7FFFFFFFFFFFFFFF));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<ulong>(0x8000000000000000));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<ulong>(0xFFFFFFFFFFFFFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUInt128Test()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateChecked<UInt128>(UInt128.Zero));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateChecked<UInt128>(UInt128.One));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<UInt128>(UInt128Tests_GenericMath.Int128MaxValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<UInt128>(UInt128Tests_GenericMath.Int128MaxValuePlusOne));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<UInt128>(UInt128.MaxValue));
        }

        [Fact]
        public static void CreateCheckedFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateChecked<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateChecked<nuint>(unchecked((nuint)0x0000000000000001)));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<nuint>(unchecked((nuint)0x8000000000000000)));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateChecked<nuint>((nuint)0x00000000));
                Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateChecked<nuint>((nuint)0x00000001));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<nuint>((nuint)0x7FFFFFFF));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<nuint>((nuint)0x80000000));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateSaturatingFromByteTest()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<byte>(0x00));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateSaturating<byte>(0x01));
            Assert.Equal((char)0x007F, NumberBaseHelper<char>.CreateSaturating<byte>(0x7F));
            Assert.Equal((char)0x0080, NumberBaseHelper<char>.CreateSaturating<byte>(0x80));
            Assert.Equal((char)0x00FF, NumberBaseHelper<char>.CreateSaturating<byte>(0xFF));
        }

        [Fact]
        public static void CreateSaturatingFromCharTest()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<char>((char)0x0000));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateSaturating<char>((char)0x0001));
            Assert.Equal((char)0x7FFF, NumberBaseHelper<char>.CreateSaturating<char>((char)0x7FFF));
            Assert.Equal((char)0x8000, NumberBaseHelper<char>.CreateSaturating<char>((char)0x8000));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateSaturating<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromDecimalTest()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<decimal>(-0.0m));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<decimal>(+0.0m));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateSaturating<decimal>(+1.0m));

            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<decimal>(decimal.MinValue));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateSaturating<decimal>(decimal.MaxValue));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<decimal>(decimal.MinusOne));
        }

        [Fact]
        public static void CreateSaturatingFromDoubleTest()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<double>(+0.0));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<double>(-0.0));


            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<double>(-double.Epsilon));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<double>(+double.Epsilon));

            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateSaturating<double>(+1.0));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateSaturating<double>(+65535.0));

            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<double>(-1.0));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateSaturating<double>(+65536.0));

            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateSaturating<double>(double.PositiveInfinity));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<double>(double.NegativeInfinity));

            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateSaturating<double>(double.MaxValue));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<double>(double.MinValue));

            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<double>(double.NaN));
        }

        [Fact]
        public static void CreateSaturatingFromHalfTest()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<Half>(Half.Zero));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<Half>(Half.NegativeZero));

            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<Half>(-Half.Epsilon));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<Half>(+Half.Epsilon));

            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateSaturating<Half>(Half.One));
            Assert.Equal((char)0xFFE0, NumberBaseHelper<char>.CreateSaturating<Half>(Half.MaxValue));

            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<Half>(Half.NegativeOne));

            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateSaturating<Half>(Half.PositiveInfinity));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<Half>(Half.NegativeInfinity));

            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<Half>(Half.MinValue));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<Half>(Half.NaN));
        }

        [Fact]
        public static void CreateSaturatingFromInt16Test()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<short>(0x0000));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateSaturating<short>(0x0001));
            Assert.Equal((char)0x7FFF, NumberBaseHelper<char>.CreateSaturating<short>(0x7FFF));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<short>(unchecked((short)0x8000)));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromInt32Test()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<int>(0x00000000));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateSaturating<int>(0x00000001));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateSaturating<int>(0x7FFFFFFF));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<int>(unchecked((int)0x80000000)));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromInt64Test()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<long>(0x0000000000000000));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateSaturating<long>(0x0000000000000001));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateSaturating<long>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<long>(unchecked((long)0x8000000000000000)));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromInt128Test()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<Int128>(Int128.Zero));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateSaturating<Int128>(Int128.One));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateSaturating<Int128>(Int128.MaxValue));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<Int128>(Int128.MinValue));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<Int128>(Int128.NegativeOne));
        }

        [Fact]
        public static void CreateSaturatingFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateSaturating<nint>(unchecked((nint)0x0000000000000001)));
                Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateSaturating<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<nint>(unchecked((nint)0x8000000000000000)));
                Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<nint>((nint)0x00000000));
                Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateSaturating<nint>((nint)0x00000001));
                Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateSaturating<nint>((nint)0x7FFFFFFF));
                Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<nint>(unchecked((nint)0x80000000)));
                Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateSaturatingFromNFloatTest()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<NFloat>(0.0f));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<NFloat>(NFloat.NegativeZero));

            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<NFloat>(-NFloat.Epsilon));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<NFloat>(+NFloat.Epsilon));

            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateSaturating<NFloat>(1.0f));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateSaturating<NFloat>(65535.0f));

            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<NFloat>(-1.0f));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateSaturating<NFloat>(+65536.0f));

            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateSaturating<NFloat>(NFloat.PositiveInfinity));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<NFloat>(NFloat.NegativeInfinity));

            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateSaturating<NFloat>(NFloat.MaxValue));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<NFloat>(NFloat.MinValue));

            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<NFloat>(NFloat.NaN));
        }

        [Fact]
        public static void CreateSaturatingFromSByteTest()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<sbyte>(0x00));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateSaturating<sbyte>(0x01));
            Assert.Equal((char)0x007F, NumberBaseHelper<char>.CreateSaturating<sbyte>(0x7F));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<sbyte>(unchecked((sbyte)0x80)));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateSaturatingFromSingleTest()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<float>(+0.0f));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<float>(-0.0f));

            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<float>(-float.Epsilon));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<float>(-float.Epsilon));

            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateSaturating<float>(+1.0f));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateSaturating<float>(+65535.0f));

            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<float>(-1.0f));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateSaturating<float>(+65536.0f));

            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateSaturating<float>(float.PositiveInfinity));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<float>(float.NegativeInfinity));

            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateSaturating<float>(float.MaxValue));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<float>(float.MinValue));

            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<float>(float.NaN));
        }

        [Fact]
        public static void CreateSaturatingFromUInt16Test()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<ushort>(0x0000));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateSaturating<ushort>(0x0001));
            Assert.Equal((char)0x7FFF, NumberBaseHelper<char>.CreateSaturating<ushort>(0x7FFF));
            Assert.Equal((char)0x8000, NumberBaseHelper<char>.CreateSaturating<ushort>(0x8000));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateSaturating<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUInt32Test()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<uint>(0x00000000));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateSaturating<uint>(0x00000001));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateSaturating<uint>(0x7FFFFFFF));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateSaturating<uint>(0x80000000));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateSaturating<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUInt64Test()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<ulong>(0x0000000000000000));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateSaturating<ulong>(0x0000000000000001));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateSaturating<ulong>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateSaturating<ulong>(0x8000000000000000));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateSaturating<ulong>(0xFFFFFFFFFFFFFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUInt128Test()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<UInt128>(UInt128.Zero));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateSaturating<UInt128>(UInt128.One));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateSaturating<UInt128>(UInt128Tests_GenericMath.Int128MaxValue));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateSaturating<UInt128>(UInt128Tests_GenericMath.Int128MaxValuePlusOne));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateSaturating<UInt128>(UInt128.MaxValue));
        }

        [Fact]
        public static void CreateSaturatingFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateSaturating<nuint>(unchecked((nuint)0x0000000000000001)));
                Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateSaturating<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateSaturating<nuint>(unchecked((nuint)0x8000000000000000)));
                Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateSaturating<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<nuint>((nuint)0x00000000));
                Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateSaturating<nuint>((nuint)0x00000001));
                Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateSaturating<nuint>((nuint)0x7FFFFFFF));
                Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateSaturating<nuint>((nuint)0x80000000));
                Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateSaturating<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateTruncatingFromByteTest()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<byte>(0x00));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateTruncating<byte>(0x01));
            Assert.Equal((char)0x007F, NumberBaseHelper<char>.CreateTruncating<byte>(0x7F));
            Assert.Equal((char)0x0080, NumberBaseHelper<char>.CreateTruncating<byte>(0x80));
            Assert.Equal((char)0x00FF, NumberBaseHelper<char>.CreateTruncating<byte>(0xFF));
        }

        [Fact]
        public static void CreateTruncatingFromCharTest()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<char>((char)0x0000));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateTruncating<char>((char)0x0001));
            Assert.Equal((char)0x7FFF, NumberBaseHelper<char>.CreateTruncating<char>((char)0x7FFF));
            Assert.Equal((char)0x8000, NumberBaseHelper<char>.CreateTruncating<char>((char)0x8000));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateTruncating<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromDecimalTest()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<decimal>(-0.0m));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<decimal>(+0.0m));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateTruncating<decimal>(+1.0m));

            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<decimal>(decimal.MinValue));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateTruncating<decimal>(decimal.MaxValue));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<decimal>(decimal.MinusOne));
        }

        [Fact]
        public static void CreateTruncatingFromDoubleTest()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<double>(+0.0));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<double>(-0.0));


            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<double>(-double.Epsilon));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<double>(+double.Epsilon));

            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateTruncating<double>(+1.0));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateTruncating<double>(+65535.0));

            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<double>(-1.0));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateTruncating<double>(+65536.0));

            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateTruncating<double>(double.PositiveInfinity));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<double>(double.NegativeInfinity));

            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateTruncating<double>(double.MaxValue));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<double>(double.MinValue));

            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<double>(double.NaN));
        }

        [Fact]
        public static void CreateTruncatingFromHalfTest()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<Half>(Half.Zero));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<Half>(Half.NegativeZero));

            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<Half>(-Half.Epsilon));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<Half>(+Half.Epsilon));

            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateTruncating<Half>(Half.One));
            Assert.Equal((char)0xFFE0, NumberBaseHelper<char>.CreateTruncating<Half>(Half.MaxValue));

            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<Half>(Half.NegativeOne));

            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateTruncating<Half>(Half.PositiveInfinity));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<Half>(Half.NegativeInfinity));

            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<Half>(Half.MinValue));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<Half>(Half.NaN));
        }

        [Fact]
        public static void CreateTruncatingFromInt16Test()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<short>(0x0000));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateTruncating<short>(0x0001));
            Assert.Equal((char)0x7FFF, NumberBaseHelper<char>.CreateTruncating<short>(0x7FFF));
            Assert.Equal((char)0x8000, NumberBaseHelper<char>.CreateTruncating<short>(unchecked((short)0x8000)));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateTruncating<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateTruncatingFromInt32Test()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<int>(0x00000000));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateTruncating<int>(0x00000001));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateTruncating<int>(0x7FFFFFFF));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<int>(unchecked((int)0x80000000)));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateTruncating<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateTruncatingFromInt64Test()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<long>(0x0000000000000000));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateTruncating<long>(0x0000000000000001));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateTruncating<long>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<long>(unchecked((long)0x8000000000000000)));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateTruncating<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void CreateTruncatingFromInt128Test()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<Int128>(Int128.Zero));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateTruncating<Int128>(Int128.One));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateTruncating<Int128>(Int128.MaxValue));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<Int128>(Int128.MinValue));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateTruncating<Int128>(Int128.NegativeOne));
        }

        [Fact]
        public static void CreateTruncatingFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateTruncating<nint>(unchecked((nint)0x0000000000000001)));
                Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateTruncating<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<nint>(unchecked((nint)0x8000000000000000)));
                Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateTruncating<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<nint>((nint)0x00000000));
                Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateTruncating<nint>((nint)0x00000001));
                Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateTruncating<nint>((nint)0x7FFFFFFF));
                Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<nint>(unchecked((nint)0x80000000)));
                Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateTruncating<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateTruncatingFromNFloatTest()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<NFloat>(0.0f));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<NFloat>(NFloat.NegativeZero));

            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<NFloat>(-NFloat.Epsilon));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<NFloat>(+NFloat.Epsilon));

            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateTruncating<NFloat>(1.0f));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateTruncating<NFloat>(65535.0f));

            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<NFloat>(-1.0f));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateTruncating<NFloat>(+65536.0f));

            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateTruncating<NFloat>(NFloat.PositiveInfinity));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<NFloat>(NFloat.NegativeInfinity));

            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateTruncating<NFloat>(NFloat.MaxValue));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<NFloat>(NFloat.MinValue));

            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<NFloat>(NFloat.NaN));
        }

        [Fact]
        public static void CreateTruncatingFromSByteTest()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<sbyte>(0x00));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateTruncating<sbyte>(0x01));
            Assert.Equal((char)0x007F, NumberBaseHelper<char>.CreateTruncating<sbyte>(0x7F));
            Assert.Equal((char)0xFF80, NumberBaseHelper<char>.CreateTruncating<sbyte>(unchecked((sbyte)0x80)));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateTruncating<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateTruncatingFromSingleTest()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<float>(+0.0f));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<float>(-0.0f));

            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<float>(-float.Epsilon));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<float>(-float.Epsilon));

            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateTruncating<float>(+1.0f));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateTruncating<float>(+65535.0f));

            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<float>(-1.0f));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateTruncating<float>(+65536.0f));

            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateTruncating<float>(float.PositiveInfinity));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<float>(float.NegativeInfinity));

            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateTruncating<float>(float.MaxValue));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<float>(float.MinValue));

            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<float>(float.NaN));
        }

        [Fact]
        public static void CreateTruncatingFromUInt16Test()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<ushort>(0x0000));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateTruncating<ushort>(0x0001));
            Assert.Equal((char)0x7FFF, NumberBaseHelper<char>.CreateTruncating<ushort>(0x7FFF));
            Assert.Equal((char)0x8000, NumberBaseHelper<char>.CreateTruncating<ushort>(0x8000));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateTruncating<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt32Test()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<uint>(0x00000000));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateTruncating<uint>(0x00000001));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateTruncating<uint>(0x7FFFFFFF));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<uint>(0x80000000));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateTruncating<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt64Test()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<ulong>(0x0000000000000000));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateTruncating<ulong>(0x0000000000000001));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateTruncating<ulong>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<ulong>(0x8000000000000000));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateTruncating<ulong>(0xFFFFFFFFFFFFFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt128Test()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<UInt128>(UInt128.Zero));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateTruncating<UInt128>(UInt128.One));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateTruncating<UInt128>(UInt128Tests_GenericMath.Int128MaxValue));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<UInt128>(UInt128Tests_GenericMath.Int128MaxValuePlusOne));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateTruncating<UInt128>(UInt128.MaxValue));
        }

        [Fact]
        public static void CreateTruncatingFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateTruncating<nuint>(unchecked((nuint)0x0000000000000001)));
                Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateTruncating<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<nuint>(unchecked((nuint)0x8000000000000000)));
                Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateTruncating<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<nuint>((nuint)0x00000000));
                Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateTruncating<nuint>((nuint)0x00000001));
                Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateTruncating<nuint>((nuint)0x7FFFFFFF));
                Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<nuint>((nuint)0x80000000));
                Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateTruncating<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void IsCanonicalTest()
        {
            Assert.True(NumberBaseHelper<char>.IsCanonical((char)0x0000));
            Assert.True(NumberBaseHelper<char>.IsCanonical((char)0x0001));
            Assert.True(NumberBaseHelper<char>.IsCanonical((char)0x7FFF));
            Assert.True(NumberBaseHelper<char>.IsCanonical((char)0x8000));
            Assert.True(NumberBaseHelper<char>.IsCanonical((char)0xFFFF));
        }

        [Fact]
        public static void IsComplexNumberTest()
        {
            Assert.False(NumberBaseHelper<char>.IsComplexNumber((char)0x0000));
            Assert.False(NumberBaseHelper<char>.IsComplexNumber((char)0x0001));
            Assert.False(NumberBaseHelper<char>.IsComplexNumber((char)0x7FFF));
            Assert.False(NumberBaseHelper<char>.IsComplexNumber((char)0x8000));
            Assert.False(NumberBaseHelper<char>.IsComplexNumber((char)0xFFFF));
        }

        [Fact]
        public static void IsEvenIntegerTest()
        {
            Assert.True(NumberBaseHelper<char>.IsEvenInteger((char)0x0000));
            Assert.False(NumberBaseHelper<char>.IsEvenInteger((char)0x0001));
            Assert.False(NumberBaseHelper<char>.IsEvenInteger((char)0x7FFF));
            Assert.True(NumberBaseHelper<char>.IsEvenInteger((char)0x8000));
            Assert.False(NumberBaseHelper<char>.IsEvenInteger((char)0xFFFF));
        }

        [Fact]
        public static void IsFiniteTest()
        {
            Assert.True(NumberBaseHelper<char>.IsFinite((char)0x0000));
            Assert.True(NumberBaseHelper<char>.IsFinite((char)0x0001));
            Assert.True(NumberBaseHelper<char>.IsFinite((char)0x7FFF));
            Assert.True(NumberBaseHelper<char>.IsFinite((char)0x8000));
            Assert.True(NumberBaseHelper<char>.IsFinite((char)0xFFFF));
        }

        [Fact]
        public static void IsImaginaryNumberTest()
        {
            Assert.False(NumberBaseHelper<char>.IsImaginaryNumber((char)0x0000));
            Assert.False(NumberBaseHelper<char>.IsImaginaryNumber((char)0x0001));
            Assert.False(NumberBaseHelper<char>.IsImaginaryNumber((char)0x7FFF));
            Assert.False(NumberBaseHelper<char>.IsImaginaryNumber((char)0x8000));
            Assert.False(NumberBaseHelper<char>.IsImaginaryNumber((char)0xFFFF));
        }

        [Fact]
        public static void IsInfinityTest()
        {
            Assert.False(NumberBaseHelper<char>.IsInfinity((char)0x0000));
            Assert.False(NumberBaseHelper<char>.IsInfinity((char)0x0001));
            Assert.False(NumberBaseHelper<char>.IsInfinity((char)0x7FFF));
            Assert.False(NumberBaseHelper<char>.IsInfinity((char)0x8000));
            Assert.False(NumberBaseHelper<char>.IsInfinity((char)0xFFFF));
        }

        [Fact]
        public static void IsIntegerTest()
        {
            Assert.True(NumberBaseHelper<char>.IsInteger((char)0x0000));
            Assert.True(NumberBaseHelper<char>.IsInteger((char)0x0001));
            Assert.True(NumberBaseHelper<char>.IsInteger((char)0x7FFF));
            Assert.True(NumberBaseHelper<char>.IsInteger((char)0x8000));
            Assert.True(NumberBaseHelper<char>.IsInteger((char)0xFFFF));
        }

        [Fact]
        public static void IsNaNTest()
        {
            Assert.False(NumberBaseHelper<char>.IsNaN((char)0x0000));
            Assert.False(NumberBaseHelper<char>.IsNaN((char)0x0001));
            Assert.False(NumberBaseHelper<char>.IsNaN((char)0x7FFF));
            Assert.False(NumberBaseHelper<char>.IsNaN((char)0x8000));
            Assert.False(NumberBaseHelper<char>.IsNaN((char)0xFFFF));
        }

        [Fact]
        public static void IsNegativeTest()
        {
            Assert.False(NumberBaseHelper<char>.IsNegative((char)0x0000));
            Assert.False(NumberBaseHelper<char>.IsNegative((char)0x0001));
            Assert.False(NumberBaseHelper<char>.IsNegative((char)0x7FFF));
            Assert.False(NumberBaseHelper<char>.IsNegative((char)0x8000));
            Assert.False(NumberBaseHelper<char>.IsNegative((char)0xFFFF));
        }

        [Fact]
        public static void IsNegativeInfinityTest()
        {
            Assert.False(NumberBaseHelper<char>.IsNegativeInfinity((char)0x0000));
            Assert.False(NumberBaseHelper<char>.IsNegativeInfinity((char)0x0001));
            Assert.False(NumberBaseHelper<char>.IsNegativeInfinity((char)0x7FFF));
            Assert.False(NumberBaseHelper<char>.IsNegativeInfinity((char)0x8000));
            Assert.False(NumberBaseHelper<char>.IsNegativeInfinity((char)0xFFFF));
        }

        [Fact]
        public static void IsNormalTest()
        {
            Assert.False(NumberBaseHelper<char>.IsNormal((char)0x0000));
            Assert.True(NumberBaseHelper<char>.IsNormal((char)0x0001));
            Assert.True(NumberBaseHelper<char>.IsNormal((char)0x7FFF));
            Assert.True(NumberBaseHelper<char>.IsNormal((char)0x8000));
            Assert.True(NumberBaseHelper<char>.IsNormal((char)0xFFFF));
        }

        [Fact]
        public static void IsOddIntegerTest()
        {
            Assert.False(NumberBaseHelper<char>.IsOddInteger((char)0x0000));
            Assert.True(NumberBaseHelper<char>.IsOddInteger((char)0x0001));
            Assert.True(NumberBaseHelper<char>.IsOddInteger((char)0x7FFF));
            Assert.False(NumberBaseHelper<char>.IsOddInteger((char)0x8000));
            Assert.True(NumberBaseHelper<char>.IsOddInteger((char)0xFFFF));
        }

        [Fact]
        public static void IsPositiveTest()
        {
            Assert.True(NumberBaseHelper<char>.IsPositive((char)0x0000));
            Assert.True(NumberBaseHelper<char>.IsPositive((char)0x0001));
            Assert.True(NumberBaseHelper<char>.IsPositive((char)0x7FFF));
            Assert.True(NumberBaseHelper<char>.IsPositive((char)0x8000));
            Assert.True(NumberBaseHelper<char>.IsPositive((char)0xFFFF));
        }

        [Fact]
        public static void IsPositiveInfinityTest()
        {
            Assert.False(NumberBaseHelper<char>.IsPositiveInfinity((char)0x0000));
            Assert.False(NumberBaseHelper<char>.IsPositiveInfinity((char)0x0001));
            Assert.False(NumberBaseHelper<char>.IsPositiveInfinity((char)0x7FFF));
            Assert.False(NumberBaseHelper<char>.IsPositiveInfinity((char)0x8000));
            Assert.False(NumberBaseHelper<char>.IsPositiveInfinity((char)0xFFFF));
        }

        [Fact]
        public static void IsRealNumberTest()
        {
            Assert.True(NumberBaseHelper<char>.IsRealNumber((char)0x0000));
            Assert.True(NumberBaseHelper<char>.IsRealNumber((char)0x0001));
            Assert.True(NumberBaseHelper<char>.IsRealNumber((char)0x7FFF));
            Assert.True(NumberBaseHelper<char>.IsRealNumber((char)0x8000));
            Assert.True(NumberBaseHelper<char>.IsRealNumber((char)0xFFFF));
        }

        [Fact]
        public static void IsSubnormalTest()
        {
            Assert.False(NumberBaseHelper<char>.IsSubnormal((char)0x0000));
            Assert.False(NumberBaseHelper<char>.IsSubnormal((char)0x0001));
            Assert.False(NumberBaseHelper<char>.IsSubnormal((char)0x7FFF));
            Assert.False(NumberBaseHelper<char>.IsSubnormal((char)0x8000));
            Assert.False(NumberBaseHelper<char>.IsSubnormal((char)0xFFFF));
        }

        [Fact]
        public static void IsZeroTest()
        {
            Assert.True(NumberBaseHelper<char>.IsZero((char)0x0000));
            Assert.False(NumberBaseHelper<char>.IsZero((char)0x0001));
            Assert.False(NumberBaseHelper<char>.IsZero((char)0x7FFF));
            Assert.False(NumberBaseHelper<char>.IsZero((char)0x8000));
            Assert.False(NumberBaseHelper<char>.IsZero((char)0xFFFF));
        }

        [Fact]
        public static void MinMagnitudeMagnitude()
        {
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.MaxMagnitude((char)0x0000, (char)1));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.MaxMagnitude((char)0x0001, (char)1));
            Assert.Equal((char)0x7FFF, NumberBaseHelper<char>.MaxMagnitude((char)0x7FFF, (char)1));
            Assert.Equal((char)0x8000, NumberBaseHelper<char>.MaxMagnitude((char)0x8000, (char)1));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.MaxMagnitude((char)0xFFFF, (char)1));
        }

        [Fact]
        public static void MaxMagnitudeNumberTest()
        {
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.MaxMagnitudeNumber((char)0x0000, (char)1));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.MaxMagnitudeNumber((char)0x0001, (char)1));
            Assert.Equal((char)0x7FFF, NumberBaseHelper<char>.MaxMagnitudeNumber((char)0x7FFF, (char)1));
            Assert.Equal((char)0x8000, NumberBaseHelper<char>.MaxMagnitudeNumber((char)0x8000, (char)1));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.MaxMagnitudeNumber((char)0xFFFF, (char)1));
        }

        [Fact]
        public static void MinMagnitudeTest()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.MinMagnitude((char)0x0000, (char)1));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.MinMagnitude((char)0x0001, (char)1));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.MinMagnitude((char)0x7FFF, (char)1));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.MinMagnitude((char)0x8000, (char)1));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.MinMagnitude((char)0xFFFF, (char)1));
        }

        [Fact]
        public static void MinMagnitudeNumberTest()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.MinMagnitudeNumber((char)0x0000, (char)1));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.MinMagnitudeNumber((char)0x0001, (char)1));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.MinMagnitudeNumber((char)0x7FFF, (char)1));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.MinMagnitudeNumber((char)0x8000, (char)1));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.MinMagnitudeNumber((char)0xFFFF, (char)1));
        }

        //
        // IShiftOperators
        //

        [Fact]
        public static void op_LeftShiftTest()
        {
            Assert.Equal((char)0x0000, ShiftOperatorsHelper<char, int, char>.op_LeftShift((char)0x0000, 1));
            Assert.Equal((char)0x0002, ShiftOperatorsHelper<char, int, char>.op_LeftShift((char)0x0001, 1));
            Assert.Equal((char)0xFFFE, ShiftOperatorsHelper<char, int, char>.op_LeftShift((char)0x7FFF, 1));
            Assert.Equal((char)0x0000, ShiftOperatorsHelper<char, int, char>.op_LeftShift((char)0x8000, 1));
            Assert.Equal((char)0xFFFE, ShiftOperatorsHelper<char, int, char>.op_LeftShift((char)0xFFFF, 1));
        }

        [Fact]
        public static void op_RightShiftTest()
        {
            Assert.Equal((char)0x0000, ShiftOperatorsHelper<char, int, char>.op_RightShift((char)0x0000, 1));
            Assert.Equal((char)0x0000, ShiftOperatorsHelper<char, int, char>.op_RightShift((char)0x0001, 1));
            Assert.Equal((char)0x3FFF, ShiftOperatorsHelper<char, int, char>.op_RightShift((char)0x7FFF, 1));
            Assert.Equal((char)0x4000, ShiftOperatorsHelper<char, int, char>.op_RightShift((char)0x8000, 1));
            Assert.Equal((char)0x7FFF, ShiftOperatorsHelper<char, int, char>.op_RightShift((char)0xFFFF, 1));
        }

        [Fact]
        public static void op_UnsignedRightShiftTest()
        {
            Assert.Equal((char)0x0000, ShiftOperatorsHelper<char, int, char>.op_UnsignedRightShift((char)0x0000, 1));
            Assert.Equal((char)0x0000, ShiftOperatorsHelper<char, int, char>.op_UnsignedRightShift((char)0x0001, 1));
            Assert.Equal((char)0x3FFF, ShiftOperatorsHelper<char, int, char>.op_UnsignedRightShift((char)0x7FFF, 1));
            Assert.Equal((char)0x4000, ShiftOperatorsHelper<char, int, char>.op_UnsignedRightShift((char)0x8000, 1));
            Assert.Equal((char)0x7FFF, ShiftOperatorsHelper<char, int, char>.op_UnsignedRightShift((char)0xFFFF, 1));
        }

        //
        // ISubtractionOperators
        //

        [Fact]
        public static void op_SubtractionTest()
        {
            Assert.Equal((char)0xFFFF, SubtractionOperatorsHelper<char, char, char>.op_Subtraction((char)0x0000, (char)1));
            Assert.Equal((char)0x0000, SubtractionOperatorsHelper<char, char, char>.op_Subtraction((char)0x0001, (char)1));
            Assert.Equal((char)0x7FFE, SubtractionOperatorsHelper<char, char, char>.op_Subtraction((char)0x7FFF, (char)1));
            Assert.Equal((char)0x7FFF, SubtractionOperatorsHelper<char, char, char>.op_Subtraction((char)0x8000, (char)1));
            Assert.Equal((char)0xFFFE, SubtractionOperatorsHelper<char, char, char>.op_Subtraction((char)0xFFFF, (char)1));
        }

        [Fact]
        public static void op_CheckedSubtractionTest()
        {
            Assert.Equal((char)0x0000, SubtractionOperatorsHelper<char, char, char>.op_CheckedSubtraction((char)0x0001, (char)1));
            Assert.Equal((char)0x7FFE, SubtractionOperatorsHelper<char, char, char>.op_CheckedSubtraction((char)0x7FFF, (char)1));
            Assert.Equal((char)0x7FFF, SubtractionOperatorsHelper<char, char, char>.op_CheckedSubtraction((char)0x8000, (char)1));
            Assert.Equal((char)0xFFFE, SubtractionOperatorsHelper<char, char, char>.op_CheckedSubtraction((char)0xFFFF, (char)1));

            Assert.Throws<OverflowException>(() => SubtractionOperatorsHelper<char, char, char>.op_CheckedSubtraction((char)0x0000, (char)1));
        }

        //
        // IUnaryNegationOperators
        //

        [Fact]
        public static void op_UnaryNegationTest()
        {
            Assert.Equal((char)0x0000, UnaryNegationOperatorsHelper<char, char>.op_UnaryNegation((char)0x0000));
            Assert.Equal((char)0xFFFF, UnaryNegationOperatorsHelper<char, char>.op_UnaryNegation((char)0x0001));
            Assert.Equal((char)0x8001, UnaryNegationOperatorsHelper<char, char>.op_UnaryNegation((char)0x7FFF));
            Assert.Equal((char)0x8000, UnaryNegationOperatorsHelper<char, char>.op_UnaryNegation((char)0x8000));
            Assert.Equal((char)0x0001, UnaryNegationOperatorsHelper<char, char>.op_UnaryNegation((char)0xFFFF));
        }

        [Fact]
        public static void op_CheckedUnaryNegationTest()
        {
            Assert.Equal((char)0x0000, UnaryNegationOperatorsHelper<char, char>.op_CheckedUnaryNegation((char)0x0000));

            Assert.Throws<OverflowException>(() => UnaryNegationOperatorsHelper<char, char>.op_CheckedUnaryNegation((char)0x0001));
            Assert.Throws<OverflowException>(() => UnaryNegationOperatorsHelper<char, char>.op_CheckedUnaryNegation((char)0x7FFF));
            Assert.Throws<OverflowException>(() => UnaryNegationOperatorsHelper<char, char>.op_CheckedUnaryNegation((char)0x8000));
            Assert.Throws<OverflowException>(() => UnaryNegationOperatorsHelper<char, char>.op_CheckedUnaryNegation((char)0xFFFF));
        }

        //
        // IUnaryPlusOperators
        //

        [Fact]
        public static void op_UnaryPlusTest()
        {
            Assert.Equal((char)0x0000, UnaryPlusOperatorsHelper<char, char>.op_UnaryPlus((char)0x0000));
            Assert.Equal((char)0x0001, UnaryPlusOperatorsHelper<char, char>.op_UnaryPlus((char)0x0001));
            Assert.Equal((char)0x7FFF, UnaryPlusOperatorsHelper<char, char>.op_UnaryPlus((char)0x7FFF));
            Assert.Equal((char)0x8000, UnaryPlusOperatorsHelper<char, char>.op_UnaryPlus((char)0x8000));
            Assert.Equal((char)0xFFFF, UnaryPlusOperatorsHelper<char, char>.op_UnaryPlus((char)0xFFFF));
        }
    }
}
