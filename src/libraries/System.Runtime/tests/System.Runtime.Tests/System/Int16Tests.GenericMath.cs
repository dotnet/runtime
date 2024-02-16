// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Tests
{
    public class Int16Tests_GenericMath
    {
        //
        // IAdditionOperators
        //

        [Fact]
        public static void op_AdditionTest()
        {
            Assert.Equal((short)0x0001, AdditionOperatorsHelper<short, short, short>.op_Addition((short)0x0000, (short)1));
            Assert.Equal((short)0x0002, AdditionOperatorsHelper<short, short, short>.op_Addition((short)0x0001, (short)1));
            Assert.Equal(unchecked((short)0x8000), AdditionOperatorsHelper<short, short, short>.op_Addition((short)0x7FFF, (short)1));
            Assert.Equal(unchecked((short)0x8001), AdditionOperatorsHelper<short, short, short>.op_Addition(unchecked((short)0x8000), (short)1));
            Assert.Equal((short)0x0000, AdditionOperatorsHelper<short, short, short>.op_Addition(unchecked((short)0xFFFF), (short)1));
        }

        [Fact]
        public static void op_CheckedAdditionTest()
        {
            Assert.Equal((short)0x0001, AdditionOperatorsHelper<short, short, short>.op_CheckedAddition((short)0x0000, (short)1));
            Assert.Equal((short)0x0002, AdditionOperatorsHelper<short, short, short>.op_CheckedAddition((short)0x0001, (short)1));
            Assert.Equal(unchecked((short)0x8001), AdditionOperatorsHelper<short, short, short>.op_CheckedAddition(unchecked((short)0x8000), (short)1));
            Assert.Equal((short)0x0000, AdditionOperatorsHelper<short, short, short>.op_CheckedAddition(unchecked((short)0xFFFF), (short)1));

            Assert.Throws<OverflowException>(() => AdditionOperatorsHelper<short, short, short>.op_CheckedAddition((short)0x7FFF, (short)1));
        }

        //
        // IAdditiveIdentity
        //

        [Fact]
        public static void AdditiveIdentityTest()
        {
            Assert.Equal((short)0x0000, AdditiveIdentityHelper<short, short>.AdditiveIdentity);
        }

        //
        // IBinaryInteger
        //

        [Fact]
        public static void DivRemTest()
        {
            Assert.Equal(((short)0x0000, (short)0x0000), BinaryIntegerHelper<short>.DivRem((short)0x0000, (short)2));
            Assert.Equal(((short)0x0000, (short)0x0001), BinaryIntegerHelper<short>.DivRem((short)0x0001, (short)2));
            Assert.Equal(((short)0x3FFF, (short)0x0001), BinaryIntegerHelper<short>.DivRem((short)0x7FFF, (short)2));
            Assert.Equal((unchecked((short)0xC000), (short)0x0000), BinaryIntegerHelper<short>.DivRem(unchecked((short)0x8000), (short)2));
            Assert.Equal(((short)0x0000, unchecked((short)0xFFFF)), BinaryIntegerHelper<short>.DivRem(unchecked((short)0xFFFF), (short)2));
        }

        [Fact]
        public static void LeadingZeroCountTest()
        {
            Assert.Equal((short)0x0010, BinaryIntegerHelper<short>.LeadingZeroCount((short)0x0000));
            Assert.Equal((short)0x000F, BinaryIntegerHelper<short>.LeadingZeroCount((short)0x0001));
            Assert.Equal((short)0x0001, BinaryIntegerHelper<short>.LeadingZeroCount((short)0x7FFF));
            Assert.Equal((short)0x0000, BinaryIntegerHelper<short>.LeadingZeroCount(unchecked((short)0x8000)));
            Assert.Equal((short)0x0000, BinaryIntegerHelper<short>.LeadingZeroCount(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void PopCountTest()
        {
            Assert.Equal((short)0x0000, BinaryIntegerHelper<short>.PopCount((short)0x0000));
            Assert.Equal((short)0x0001, BinaryIntegerHelper<short>.PopCount((short)0x0001));
            Assert.Equal((short)0x000F, BinaryIntegerHelper<short>.PopCount((short)0x7FFF));
            Assert.Equal((short)0x0001, BinaryIntegerHelper<short>.PopCount(unchecked((short)0x8000)));
            Assert.Equal((short)0x0010, BinaryIntegerHelper<short>.PopCount(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void RotateLeftTest()
        {
            Assert.Equal((short)0x0000, BinaryIntegerHelper<short>.RotateLeft((short)0x0000, 1));
            Assert.Equal((short)0x0002, BinaryIntegerHelper<short>.RotateLeft((short)0x0001, 1));
            Assert.Equal(unchecked((short)0xFFFE), BinaryIntegerHelper<short>.RotateLeft((short)0x7FFF, 1));
            Assert.Equal((short)0x0001, BinaryIntegerHelper<short>.RotateLeft(unchecked((short)0x8000), 1));
            Assert.Equal(unchecked((short)0xFFFF), BinaryIntegerHelper<short>.RotateLeft(unchecked((short)0xFFFF), 1));
        }

        [Fact]
        public static void RotateRightTest()
        {
            Assert.Equal((short)0x0000, BinaryIntegerHelper<short>.RotateRight((short)0x0000, 1));
            Assert.Equal(unchecked((short)0x8000), BinaryIntegerHelper<short>.RotateRight((short)0x0001, 1));
            Assert.Equal(unchecked((short)0xBFFF), BinaryIntegerHelper<short>.RotateRight((short)0x7FFF, 1));
            Assert.Equal((short)0x4000, BinaryIntegerHelper<short>.RotateRight(unchecked((short)0x8000), 1));
            Assert.Equal(unchecked((short)0xFFFF), BinaryIntegerHelper<short>.RotateRight(unchecked((short)0xFFFF), 1));
        }

        [Fact]
        public static void TrailingZeroCountTest()
        {
            Assert.Equal((short)0x0010, BinaryIntegerHelper<short>.TrailingZeroCount((short)0x0000));
            Assert.Equal((short)0x0000, BinaryIntegerHelper<short>.TrailingZeroCount((short)0x0001));
            Assert.Equal((short)0x0000, BinaryIntegerHelper<short>.TrailingZeroCount((short)0x7FFF));
            Assert.Equal((short)0x000F, BinaryIntegerHelper<short>.TrailingZeroCount(unchecked((short)0x8000)));
            Assert.Equal((short)0x0000, BinaryIntegerHelper<short>.TrailingZeroCount(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void TryReadBigEndianByteTest()
        {
            short result;

            Assert.True(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0x00 }, isUnsigned: true, out result));
            Assert.Equal((short)0x0000, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0x01 }, isUnsigned: true, out result));
            Assert.Equal((short)0x0001, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0x7F }, isUnsigned: true, out result));
            Assert.Equal((short)0x007F, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0x80 }, isUnsigned: true, out result));
            Assert.Equal((short)0x0080, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0xFF }, isUnsigned: true, out result));
            Assert.Equal((short)0x00FF, result);
        }

        [Fact]
        public static void TryReadBigEndianInt16Test()
        {
            short result;

            Assert.True(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((short)0x0000, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal((short)0x0001, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal((short)0x0080, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0x01, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((short)0x0100, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0x7F, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((short)0x7FFF, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0x80, 0x00 }, isUnsigned: false, out result));
            Assert.Equal(unchecked((short)0x8000), result);

            Assert.True(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal(unchecked((short)0xFF7F), result);

            Assert.True(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(unchecked((short)0xFFFF), result);
        }

        [Fact]
        public static void TryReadBigEndianInt32Test()
        {
            short result;

            Assert.True(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((short)0x0000, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal((short)0x0001, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal((short)0x0080, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0x01, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((short)0x0000, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((short)0x0000, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0x80, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((short)0x0000, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal(unchecked((short)0xFF7F), result);

            Assert.True(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(unchecked((short)0xFFFF), result);
        }

        [Fact]
        public static void TryReadBigEndianInt64Test()
        {
            short result;

            Assert.True(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((short)0x0000, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal((short)0x0001, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal((short)0x0080, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((short)0x0000, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((short)0x0000, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((short)0x0000, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal(unchecked((short)0xFF7F), result);

            Assert.True(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(unchecked((short)0xFFFF), result);
        }

        [Fact]
        public static void TryReadBigEndianInt96Test()
        {
            short result;

            Assert.True(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((short)0x0000, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal((short)0x0001, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal((short)0x0080, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((short)0x0000, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((short)0x0000, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((short)0x0000, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal(unchecked((short)0xFF7F), result);

            Assert.True(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(unchecked((short)0xFFFF), result);
        }

        [Fact]
        public static void TryReadBigEndianInt128Test()
        {
            short result;

            Assert.True(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((short)0x0000, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal((short)0x0001, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal((short)0x0080, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((short)0x0000, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((short)0x0000, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((short)0x0000, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal(unchecked((short)0xFF7F), result);

            Assert.True(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(unchecked((short)0xFFFF), result);
        }

        [Fact]
        public static void TryReadBigEndianSByteTest()
        {
            short result;

            Assert.True(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0x00 }, isUnsigned: false, out result));
            Assert.Equal((short)0x0000, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0x01 }, isUnsigned: false, out result));
            Assert.Equal((short)0x0001, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0x7F }, isUnsigned: false, out result));
            Assert.Equal((short)0x007F, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0x80 }, isUnsigned: false, out result));
            Assert.Equal(unchecked((short)0xFF80), result);

            Assert.True(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0xFF }, isUnsigned: false, out result));
            Assert.Equal(unchecked((short)0xFFFF), result);
        }

        [Fact]
        public static void TryReadBigEndianUInt16Test()
        {
            short result;

            Assert.True(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((short)0x0000, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal((short)0x0001, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal((short)0x0080, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0x01, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((short)0x0100, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0x7F, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((short)0x7FFF, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0x80, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((short)0x0000, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal((short)0x0000, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((short)0x0000, result);
        }

        [Fact]
        public static void TryReadBigEndianUInt32Test()
        {
            short result;

            Assert.True(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((short)0x0000, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal((short)0x0001, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal((short)0x0080, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0x01, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((short)0x0000, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((short)0x0000, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0x80, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((short)0x0000, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal((short)0x0000, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((short)0x0000, result);
        }

        [Fact]
        public static void TryReadBigEndianUInt64Test()
        {
            short result;

            Assert.True(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((short)0x0000, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal((short)0x0001, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal((short)0x0080, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((short)0x0000, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((short)0x0000, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((short)0x0000, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal((short)0x0000, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((short)0x0000, result);
        }

        [Fact]
        public static void TryReadBigEndianUInt96Test()
        {
            short result;

            Assert.True(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((short)0x0000, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal((short)0x0001, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal((short)0x0080, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((short)0x0000, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((short)0x0000, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((short)0x0000, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal((short)0x0000, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((short)0x0000, result);
        }

        [Fact]
        public static void TryReadBigEndianUInt128Test()
        {
            short result;

            Assert.True(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((short)0x0000, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal((short)0x0001, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal((short)0x0080, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((short)0x0000, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((short)0x0000, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((short)0x0000, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal((short)0x0000, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((short)0x0000, result);
        }

        [Fact]
        public static void TryReadLittleEndianByteTest()
        {
            short result;

            Assert.True(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0x00 }, isUnsigned: true, out result));
            Assert.Equal((short)0x0000, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0x01 }, isUnsigned: true, out result));
            Assert.Equal((short)0x0001, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0x7F }, isUnsigned: true, out result));
            Assert.Equal((short)0x007F, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0x80 }, isUnsigned: true, out result));
            Assert.Equal((short)0x0080, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0xFF }, isUnsigned: true, out result));
            Assert.Equal((short)0x00FF, result);
        }

        [Fact]
        public static void TryReadLittleEndianInt16Test()
        {
            short result;

            Assert.True(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((short)0x0000, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal((short)0x0100, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal(unchecked((short)0x8000), result);

            Assert.True(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0x01, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((short)0x0001, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(unchecked((short)0xFF7F), result);

            Assert.True(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0x80, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((short)0x0080, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal((short)0x7FFF, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(unchecked((short)0xFFFF), result);
        }

        [Fact]
        public static void TryReadLittleEndianInt32Test()
        {
            short result;

            Assert.True(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((short)0x0000, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal((short)0x0000, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal((short)0x0000, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0x01, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((short)0x0001, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(unchecked((short)0xFF7F), result);

            Assert.True(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0x80, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((short)0x0080, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal((short)0x0000, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(unchecked((short)0xFFFF), result);
        }

        [Fact]
        public static void TryReadLittleEndianInt64Test()
        {
            short result;

            Assert.True(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((short)0x0000, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal((short)0x0000, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal((short)0x0000, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((short)0x0001, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(unchecked((short)0xFF7F), result);

            Assert.True(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((short)0x0080, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal((short)0x0000, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(unchecked((short)0xFFFF), result);
        }

        [Fact]
        public static void TryReadLittleEndianInt96Test()
        {
            short result;

            Assert.True(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((short)0x0000, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal((short)0x0000, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal((short)0x0000, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((short)0x0001, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(unchecked((short)0xFF7F), result);

            Assert.True(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((short)0x0080, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal((short)0x0000, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(unchecked((short)0xFFFF), result);
        }

        [Fact]
        public static void TryReadLittleEndianInt128Test()
        {
            short result;

            Assert.True(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((short)0x0000, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal((short)0x0000, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal((short)0x0000, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((short)0x0001, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(unchecked((short)0xFF7F), result);

            Assert.True(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((short)0x0080, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal((short)0x0000, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(unchecked((short)0xFFFF), result);
        }

        [Fact]
        public static void TryReadLittleEndianSByteTest()
        {
            short result;

            Assert.True(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0x00 }, isUnsigned: false, out result));
            Assert.Equal((short)0x0000, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0x01 }, isUnsigned: false, out result));
            Assert.Equal((short)0x0001, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0x7F }, isUnsigned: false, out result));
            Assert.Equal((short)0x007F, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0x80 }, isUnsigned: false, out result));
            Assert.Equal(unchecked((short)0xFF80), result);

            Assert.True(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0xFF }, isUnsigned: false, out result));
            Assert.Equal(unchecked((short)0xFFFF), result);
        }

        [Fact]
        public static void TryReadLittleEndianUInt16Test()
        {
            short result;

            Assert.True(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((short)0x0000, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal((short)0x0100, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal((short)0x0000, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0x01, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((short)0x0001, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((short)0x0000, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0x80, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((short)0x0080, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal((short)0x7FFF, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((short)0x0000, result);
        }

        [Fact]
        public static void TryReadLittleEndianUInt32Test()
        {
            short result;

            Assert.True(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((short)0x0000, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal((short)0x0000, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal((short)0x0000, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0x01, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((short)0x0001, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((short)0x0000, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0x80, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((short)0x0080, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal((short)0x0000, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((short)0x0000, result);
        }

        [Fact]
        public static void TryReadLittleEndianUInt64Test()
        {
            short result;

            Assert.True(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((short)0x0000, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal((short)0x0000, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal((short)0x0000, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((short)0x0001, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((short)0x0000, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((short)0x0080, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal((short)0x0000, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((short)0x0000, result);
        }

        [Fact]
        public static void TryReadLittleEndianUInt96Test()
        {
            short result;

            Assert.True(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((short)0x0000, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal((short)0x0000, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal((short)0x0000, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((short)0x0001, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((short)0x0000, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((short)0x0080, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal((short)0x0000, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((short)0x0000, result);
        }

        [Fact]
        public static void TryReadLittleEndianUInt128Test()
        {
            short result;

            Assert.True(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((short)0x0000, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal((short)0x0000, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal((short)0x0000, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((short)0x0001, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((short)0x0000, result);

            Assert.True(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((short)0x0080, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal((short)0x0000, result);

            Assert.False(BinaryIntegerHelper<short>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((short)0x0000, result);
        }

        [Fact]
        public static void GetByteCountTest()
        {
            Assert.Equal(2, BinaryIntegerHelper<short>.GetByteCount((short)0x0000));
            Assert.Equal(2, BinaryIntegerHelper<short>.GetByteCount((short)0x0001));
            Assert.Equal(2, BinaryIntegerHelper<short>.GetByteCount((short)0x7FFF));
            Assert.Equal(2, BinaryIntegerHelper<short>.GetByteCount(unchecked((short)0x8000)));
            Assert.Equal(2, BinaryIntegerHelper<short>.GetByteCount(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void GetShortestBitLengthTest()
        {
            Assert.Equal(0x00, BinaryIntegerHelper<short>.GetShortestBitLength((short)0x0000));
            Assert.Equal(0x01, BinaryIntegerHelper<short>.GetShortestBitLength((short)0x0001));
            Assert.Equal(0x0F, BinaryIntegerHelper<short>.GetShortestBitLength((short)0x7FFF));
            Assert.Equal(0x10, BinaryIntegerHelper<short>.GetShortestBitLength(unchecked((short)0x8000)));
            Assert.Equal(0x01, BinaryIntegerHelper<short>.GetShortestBitLength(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void TryWriteBigEndianTest()
        {
            Span<byte> destination = stackalloc byte[2];
            int bytesWritten = 0;

            Assert.True(BinaryIntegerHelper<short>.TryWriteBigEndian((short)0x0000, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x00 }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<short>.TryWriteBigEndian((short)0x0001, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x01 }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<short>.TryWriteBigEndian((short)0x7FFF, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0x7F, 0xFF }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<short>.TryWriteBigEndian(unchecked((short)0x8000), destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0x80, 0x00 }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<short>.TryWriteBigEndian(unchecked((short)0xFFFF), destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0xFF }, destination.ToArray());

            Assert.False(BinaryIntegerHelper<short>.TryWriteBigEndian(default, Span<byte>.Empty, out bytesWritten));
            Assert.Equal(0, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0xFF }, destination.ToArray());
        }

        [Fact]
        public static void TryWriteLittleEndianTest()
        {
            Span<byte> destination = stackalloc byte[2];
            int bytesWritten = 0;

            Assert.True(BinaryIntegerHelper<short>.TryWriteLittleEndian((short)0x0000, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x00 }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<short>.TryWriteLittleEndian((short)0x0001, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0x01, 0x00 }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<short>.TryWriteLittleEndian((short)0x7FFF, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0x7F }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<short>.TryWriteLittleEndian(unchecked((short)0x8000), destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x80 }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<short>.TryWriteLittleEndian(unchecked((short)0xFFFF), destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0xFF }, destination.ToArray());

            Assert.False(BinaryIntegerHelper<short>.TryWriteLittleEndian(default, Span<byte>.Empty, out bytesWritten));
            Assert.Equal(0, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0xFF }, destination.ToArray());
        }

        //
        // IBinaryNumber
        //

        [Fact]
        public static void AllBitsSetTest()
        {
            Assert.Equal(unchecked((short)0xFFFF), BinaryNumberHelper<short>.AllBitsSet);
            Assert.Equal((short)0, ~BinaryNumberHelper<short>.AllBitsSet);
        }

        [Fact]
        public static void IsPow2Test()
        {
            Assert.False(BinaryNumberHelper<short>.IsPow2((short)0x0000));
            Assert.True(BinaryNumberHelper<short>.IsPow2((short)0x0001));
            Assert.False(BinaryNumberHelper<short>.IsPow2((short)0x7FFF));
            Assert.False(BinaryNumberHelper<short>.IsPow2(unchecked((short)0x8000)));
            Assert.False(BinaryNumberHelper<short>.IsPow2(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void Log2Test()
        {
            Assert.Equal((short)0x0000, BinaryNumberHelper<short>.Log2((short)0x0000));
            Assert.Equal((short)0x0000, BinaryNumberHelper<short>.Log2((short)0x0001));
            Assert.Equal((short)0x000E, BinaryNumberHelper<short>.Log2((short)0x7FFF));
            Assert.Throws<ArgumentOutOfRangeException>(() => BinaryNumberHelper<short>.Log2(unchecked((short)0x8000)));
            Assert.Throws<ArgumentOutOfRangeException>(() => BinaryNumberHelper<short>.Log2(unchecked((short)0xFFFF)));
        }

        //
        // IBitwiseOperators
        //

        [Fact]
        public static void op_BitwiseAndTest()
        {
            Assert.Equal((short)0x0000, BitwiseOperatorsHelper<short, short, short>.op_BitwiseAnd((short)0x0000, (short)1));
            Assert.Equal((short)0x0001, BitwiseOperatorsHelper<short, short, short>.op_BitwiseAnd((short)0x0001, (short)1));
            Assert.Equal((short)0x0001, BitwiseOperatorsHelper<short, short, short>.op_BitwiseAnd((short)0x7FFF, (short)1));
            Assert.Equal((short)0x0000, BitwiseOperatorsHelper<short, short, short>.op_BitwiseAnd(unchecked((short)0x8000), (short)1));
            Assert.Equal((short)0x0001, BitwiseOperatorsHelper<short, short, short>.op_BitwiseAnd(unchecked((short)0xFFFF), (short)1));
        }

        [Fact]
        public static void op_BitwiseOrTest()
        {
            Assert.Equal((short)0x0001, BitwiseOperatorsHelper<short, short, short>.op_BitwiseOr((short)0x0000, (short)1));
            Assert.Equal((short)0x0001, BitwiseOperatorsHelper<short, short, short>.op_BitwiseOr((short)0x0001, (short)1));
            Assert.Equal((short)0x7FFF, BitwiseOperatorsHelper<short, short, short>.op_BitwiseOr((short)0x7FFF, (short)1));
            Assert.Equal(unchecked((short)0x8001), BitwiseOperatorsHelper<short, short, short>.op_BitwiseOr(unchecked((short)0x8000), (short)1));
            Assert.Equal(unchecked((short)0xFFFF), BitwiseOperatorsHelper<short, short, short>.op_BitwiseOr(unchecked((short)0xFFFF), (short)1));
        }

        [Fact]
        public static void op_ExclusiveOrTest()
        {
            Assert.Equal((short)0x0001, BitwiseOperatorsHelper<short, short, short>.op_ExclusiveOr((short)0x0000, (short)1));
            Assert.Equal((short)0x0000, BitwiseOperatorsHelper<short, short, short>.op_ExclusiveOr((short)0x0001, (short)1));
            Assert.Equal((short)0x7FFE, BitwiseOperatorsHelper<short, short, short>.op_ExclusiveOr((short)0x7FFF, (short)1));
            Assert.Equal(unchecked((short)0x8001), BitwiseOperatorsHelper<short, short, short>.op_ExclusiveOr(unchecked((short)0x8000), (short)1));
            Assert.Equal(unchecked((short)0xFFFE), BitwiseOperatorsHelper<short, short, short>.op_ExclusiveOr(unchecked((short)0xFFFF), (short)1));
        }

        [Fact]
        public static void op_OnesComplementTest()
        {
            Assert.Equal(unchecked((short)0xFFFF), BitwiseOperatorsHelper<short, short, short>.op_OnesComplement((short)0x0000));
            Assert.Equal(unchecked((short)0xFFFE), BitwiseOperatorsHelper<short, short, short>.op_OnesComplement((short)0x0001));
            Assert.Equal(unchecked((short)0x8000), BitwiseOperatorsHelper<short, short, short>.op_OnesComplement((short)0x7FFF));
            Assert.Equal((short)0x7FFF, BitwiseOperatorsHelper<short, short, short>.op_OnesComplement(unchecked((short)0x8000)));
            Assert.Equal((short)0x0000, BitwiseOperatorsHelper<short, short, short>.op_OnesComplement(unchecked((short)0xFFFF)));
        }

        //
        // IComparisonOperators
        //

        [Fact]
        public static void op_GreaterThanTest()
        {
            Assert.False(ComparisonOperatorsHelper<short, short, bool>.op_GreaterThan((short)0x0000, (short)1));
            Assert.False(ComparisonOperatorsHelper<short, short, bool>.op_GreaterThan((short)0x0001, (short)1));
            Assert.True(ComparisonOperatorsHelper<short, short, bool>.op_GreaterThan((short)0x7FFF, (short)1));
            Assert.False(ComparisonOperatorsHelper<short, short, bool>.op_GreaterThan(unchecked((short)0x8000), (short)1));
            Assert.False(ComparisonOperatorsHelper<short, short, bool>.op_GreaterThan(unchecked((short)0xFFFF), (short)1));
        }

        [Fact]
        public static void op_GreaterThanOrEqualTest()
        {
            Assert.False(ComparisonOperatorsHelper<short, short, bool>.op_GreaterThanOrEqual((short)0x0000, (short)1));
            Assert.True(ComparisonOperatorsHelper<short, short, bool>.op_GreaterThanOrEqual((short)0x0001, (short)1));
            Assert.True(ComparisonOperatorsHelper<short, short, bool>.op_GreaterThanOrEqual((short)0x7FFF, (short)1));
            Assert.False(ComparisonOperatorsHelper<short, short, bool>.op_GreaterThanOrEqual(unchecked((short)0x8000), (short)1));
            Assert.False(ComparisonOperatorsHelper<short, short, bool>.op_GreaterThanOrEqual(unchecked((short)0xFFFF), (short)1));
        }

        [Fact]
        public static void op_LessThanTest()
        {
            Assert.True(ComparisonOperatorsHelper<short, short, bool>.op_LessThan((short)0x0000, (short)1));
            Assert.False(ComparisonOperatorsHelper<short, short, bool>.op_LessThan((short)0x0001, (short)1));
            Assert.False(ComparisonOperatorsHelper<short, short, bool>.op_LessThan((short)0x7FFF, (short)1));
            Assert.True(ComparisonOperatorsHelper<short, short, bool>.op_LessThan(unchecked((short)0x8000), (short)1));
            Assert.True(ComparisonOperatorsHelper<short, short, bool>.op_LessThan(unchecked((short)0xFFFF), (short)1));
        }

        [Fact]
        public static void op_LessThanOrEqualTest()
        {
            Assert.True(ComparisonOperatorsHelper<short, short, bool>.op_LessThanOrEqual((short)0x0000, (short)1));
            Assert.True(ComparisonOperatorsHelper<short, short, bool>.op_LessThanOrEqual((short)0x0001, (short)1));
            Assert.False(ComparisonOperatorsHelper<short, short, bool>.op_LessThanOrEqual((short)0x7FFF, (short)1));
            Assert.True(ComparisonOperatorsHelper<short, short, bool>.op_LessThanOrEqual(unchecked((short)0x8000), (short)1));
            Assert.True(ComparisonOperatorsHelper<short, short, bool>.op_LessThanOrEqual(unchecked((short)0xFFFF), (short)1));
        }

        //
        // IDecrementOperators
        //

        [Fact]
        public static void op_DecrementTest()
        {
            Assert.Equal(unchecked((short)0xFFFF), DecrementOperatorsHelper<short>.op_Decrement((short)0x0000));
            Assert.Equal((short)0x0000, DecrementOperatorsHelper<short>.op_Decrement((short)0x0001));
            Assert.Equal((short)0x7FFE, DecrementOperatorsHelper<short>.op_Decrement((short)0x7FFF));
            Assert.Equal((short)0x7FFF, DecrementOperatorsHelper<short>.op_Decrement(unchecked((short)0x8000)));
            Assert.Equal(unchecked((short)0xFFFE), DecrementOperatorsHelper<short>.op_Decrement(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void op_CheckedDecrementTest()
        {
            Assert.Equal(unchecked((short)0xFFFF), DecrementOperatorsHelper<short>.op_CheckedDecrement((short)0x0000));
            Assert.Equal((short)0x0000, DecrementOperatorsHelper<short>.op_CheckedDecrement((short)0x0001));
            Assert.Equal((short)0x7FFE, DecrementOperatorsHelper<short>.op_CheckedDecrement((short)0x7FFF));
            Assert.Equal(unchecked((short)0xFFFE), DecrementOperatorsHelper<short>.op_CheckedDecrement(unchecked((short)0xFFFF)));

            Assert.Throws<OverflowException>(() => DecrementOperatorsHelper<short>.op_CheckedDecrement(unchecked((short)0x8000)));
        }

        //
        // IDivisionOperators
        //

        [Fact]
        public static void op_DivisionTest()
        {
            Assert.Equal((short)0x0000, DivisionOperatorsHelper<short, short, short>.op_Division((short)0x0000, (short)2));
            Assert.Equal((short)0x0000, DivisionOperatorsHelper<short, short, short>.op_Division((short)0x0001, (short)2));
            Assert.Equal((short)0x3FFF, DivisionOperatorsHelper<short, short, short>.op_Division((short)0x7FFF, (short)2));
            Assert.Equal(unchecked((short)0xC000), DivisionOperatorsHelper<short, short, short>.op_Division(unchecked((short)0x8000), (short)2));
            Assert.Equal((short)0x0000, DivisionOperatorsHelper<short, short, short>.op_Division(unchecked((short)0xFFFF), (short)2));

            Assert.Throws<DivideByZeroException>(() => DivisionOperatorsHelper<short, short, short>.op_Division((short)0x0000, (short)0));
            Assert.Throws<DivideByZeroException>(() => DivisionOperatorsHelper<short, short, short>.op_Division((short)0x0001, (short)0));
            Assert.Throws<DivideByZeroException>(() => DivisionOperatorsHelper<short, short, short>.op_Division(unchecked((short)0xFFFF), (short)0));
        }

        [Fact]
        public static void op_CheckedDivisionTest()
        {
            Assert.Equal((short)0x0000, DivisionOperatorsHelper<short, short, short>.op_CheckedDivision((short)0x0000, (short)2));
            Assert.Equal((short)0x0000, DivisionOperatorsHelper<short, short, short>.op_CheckedDivision((short)0x0001, (short)2));
            Assert.Equal((short)0x3FFF, DivisionOperatorsHelper<short, short, short>.op_CheckedDivision((short)0x7FFF, (short)2));
            Assert.Equal(unchecked((short)0xC000), DivisionOperatorsHelper<short, short, short>.op_CheckedDivision(unchecked((short)0x8000), (short)2));
            Assert.Equal((short)0x0000, DivisionOperatorsHelper<short, short, short>.op_CheckedDivision(unchecked((short)0xFFFF), (short)2));

            Assert.Throws<DivideByZeroException>(() => DivisionOperatorsHelper<short, short, short>.op_CheckedDivision((short)0x0000, (short)0));
            Assert.Throws<DivideByZeroException>(() => DivisionOperatorsHelper<short, short, short>.op_CheckedDivision((short)0x0001, (short)0));
            Assert.Throws<DivideByZeroException>(() => DivisionOperatorsHelper<short, short, short>.op_CheckedDivision(unchecked((short)0xFFFF), (short)0));
        }

        //
        // IEqualityOperators
        //

        [Fact]
        public static void op_EqualityTest()
        {
            Assert.False(EqualityOperatorsHelper<short, short, bool>.op_Equality((short)0x0000, (short)1));
            Assert.True(EqualityOperatorsHelper<short, short, bool>.op_Equality((short)0x0001, (short)1));
            Assert.False(EqualityOperatorsHelper<short, short, bool>.op_Equality((short)0x7FFF, (short)1));
            Assert.False(EqualityOperatorsHelper<short, short, bool>.op_Equality(unchecked((short)0x8000), (short)1));
            Assert.False(EqualityOperatorsHelper<short, short, bool>.op_Equality(unchecked((short)0xFFFF), (short)1));
        }

        [Fact]
        public static void op_InequalityTest()
        {
            Assert.True(EqualityOperatorsHelper<short, short, bool>.op_Inequality((short)0x0000, (short)1));
            Assert.False(EqualityOperatorsHelper<short, short, bool>.op_Inequality((short)0x0001, (short)1));
            Assert.True(EqualityOperatorsHelper<short, short, bool>.op_Inequality((short)0x7FFF, (short)1));
            Assert.True(EqualityOperatorsHelper<short, short, bool>.op_Inequality(unchecked((short)0x8000), (short)1));
            Assert.True(EqualityOperatorsHelper<short, short, bool>.op_Inequality(unchecked((short)0xFFFF), (short)1));
        }

        //
        // IIncrementOperators
        //

        [Fact]
        public static void op_IncrementTest()
        {
            Assert.Equal((short)0x0001, IncrementOperatorsHelper<short>.op_Increment((short)0x0000));
            Assert.Equal((short)0x0002, IncrementOperatorsHelper<short>.op_Increment((short)0x0001));
            Assert.Equal(unchecked((short)0x8000), IncrementOperatorsHelper<short>.op_Increment((short)0x7FFF));
            Assert.Equal(unchecked((short)0x8001), IncrementOperatorsHelper<short>.op_Increment(unchecked((short)0x8000)));
            Assert.Equal((short)0x0000, IncrementOperatorsHelper<short>.op_Increment(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void op_CheckedIncrementTest()
        {
            Assert.Equal((short)0x0001, IncrementOperatorsHelper<short>.op_CheckedIncrement((short)0x0000));
            Assert.Equal((short)0x0002, IncrementOperatorsHelper<short>.op_CheckedIncrement((short)0x0001));
            Assert.Equal(unchecked((short)0x8001), IncrementOperatorsHelper<short>.op_CheckedIncrement(unchecked((short)0x8000)));
            Assert.Equal((short)0x0000, IncrementOperatorsHelper<short>.op_CheckedIncrement(unchecked((short)0xFFFF)));

            Assert.Throws<OverflowException>(() => IncrementOperatorsHelper<short>.op_CheckedIncrement((short)0x7FFF));
        }

        //
        // IMinMaxValue
        //

        [Fact]
        public static void MaxValueTest()
        {
            Assert.Equal((short)0x7FFF, MinMaxValueHelper<short>.MaxValue);
        }

        [Fact]
        public static void MinValueTest()
        {
            Assert.Equal(unchecked((short)0x8000), MinMaxValueHelper<short>.MinValue);
        }

        //
        // IModulusOperators
        //

        [Fact]
        public static void op_ModulusTest()
        {
            Assert.Equal((short)0x0000, ModulusOperatorsHelper<short, short, short>.op_Modulus((short)0x0000, (short)2));
            Assert.Equal((short)0x0001, ModulusOperatorsHelper<short, short, short>.op_Modulus((short)0x0001, (short)2));
            Assert.Equal((short)0x0001, ModulusOperatorsHelper<short, short, short>.op_Modulus((short)0x7FFF, (short)2));
            Assert.Equal((short)0x0000, ModulusOperatorsHelper<short, short, short>.op_Modulus(unchecked((short)0x8000), (short)2));
            Assert.Equal(unchecked((short)0xFFFF), ModulusOperatorsHelper<short, short, short>.op_Modulus(unchecked((short)0xFFFF), (short)2));

            Assert.Throws<DivideByZeroException>(() => ModulusOperatorsHelper<short, short, short>.op_Modulus((short)0x0001, (short)0));
        }

        //
        // IMultiplicativeIdentity
        //

        [Fact]
        public static void MultiplicativeIdentityTest()
        {
            Assert.Equal((short)0x0001, MultiplicativeIdentityHelper<short, short>.MultiplicativeIdentity);
        }

        //
        // IMultiplyOperators
        //

        [Fact]
        public static void op_MultiplyTest()
        {
            Assert.Equal((short)0x0000, MultiplyOperatorsHelper<short, short, short>.op_Multiply((short)0x0000, (short)2));
            Assert.Equal((short)0x0002, MultiplyOperatorsHelper<short, short, short>.op_Multiply((short)0x0001, (short)2));
            Assert.Equal(unchecked((short)0xFFFE), MultiplyOperatorsHelper<short, short, short>.op_Multiply((short)0x7FFF, (short)2));
            Assert.Equal((short)0x0000, MultiplyOperatorsHelper<short, short, short>.op_Multiply(unchecked((short)0x8000), (short)2));
            Assert.Equal(unchecked((short)0xFFFE), MultiplyOperatorsHelper<short, short, short>.op_Multiply(unchecked((short)0xFFFF), (short)2));
        }

        [Fact]
        public static void op_CheckedMultiplyTest()
        {
            Assert.Equal((short)0x0000, MultiplyOperatorsHelper<short, short, short>.op_CheckedMultiply((short)0x0000, (short)2));
            Assert.Equal((short)0x0002, MultiplyOperatorsHelper<short, short, short>.op_CheckedMultiply((short)0x0001, (short)2));
            Assert.Equal(unchecked((short)0xFFFE), MultiplyOperatorsHelper<short, short, short>.op_CheckedMultiply(unchecked((short)0xFFFF), (short)2));

            Assert.Throws<OverflowException>(() => MultiplyOperatorsHelper<short, short, short>.op_CheckedMultiply((short)0x7FFF, (short)2));
            Assert.Throws<OverflowException>(() => MultiplyOperatorsHelper<short, short, short>.op_CheckedMultiply(unchecked((short)0x8000), (short)2));
        }

        //
        // INumber
        //

        [Fact]
        public static void ClampTest()
        {
            Assert.Equal((short)0x0000, NumberHelper<short>.Clamp((short)0x0000, unchecked((short)0xFFC0), (short)0x003F));
            Assert.Equal((short)0x0001, NumberHelper<short>.Clamp((short)0x0001, unchecked((short)0xFFC0), (short)0x003F));
            Assert.Equal((short)0x003F, NumberHelper<short>.Clamp((short)0x7FFF, unchecked((short)0xFFC0), (short)0x003F));
            Assert.Equal(unchecked((short)0xFFC0), NumberHelper<short>.Clamp(unchecked((short)0x8000), unchecked((short)0xFFC0), (short)0x003F));
            Assert.Equal(unchecked((short)0xFFFF), NumberHelper<short>.Clamp(unchecked((short)0xFFFF), unchecked((short)0xFFC0), (short)0x003F));
        }

        [Fact]
        public static void MaxTest()
        {
            Assert.Equal((short)0x0001, NumberHelper<short>.Max((short)0x0000, (short)1));
            Assert.Equal((short)0x0001, NumberHelper<short>.Max((short)0x0001, (short)1));
            Assert.Equal((short)0x7FFF, NumberHelper<short>.Max((short)0x7FFF, (short)1));
            Assert.Equal((short)0x0001, NumberHelper<short>.Max(unchecked((short)0x8000), (short)1));
            Assert.Equal((short)0x0001, NumberHelper<short>.Max(unchecked((short)0xFFFF), (short)1));
        }

        [Fact]
        public static void MaxNumberTest()
        {
            Assert.Equal((short)0x0001, NumberHelper<short>.MaxNumber((short)0x0000, (short)1));
            Assert.Equal((short)0x0001, NumberHelper<short>.MaxNumber((short)0x0001, (short)1));
            Assert.Equal((short)0x7FFF, NumberHelper<short>.MaxNumber((short)0x7FFF, (short)1));
            Assert.Equal((short)0x0001, NumberHelper<short>.MaxNumber(unchecked((short)0x8000), (short)1));
            Assert.Equal((short)0x0001, NumberHelper<short>.MaxNumber(unchecked((short)0xFFFF), (short)1));
        }

        [Fact]
        public static void MinTest()
        {
            Assert.Equal((short)0x0000, NumberHelper<short>.Min((short)0x0000, (short)1));
            Assert.Equal((short)0x0001, NumberHelper<short>.Min((short)0x0001, (short)1));
            Assert.Equal((short)0x0001, NumberHelper<short>.Min((short)0x7FFF, (short)1));
            Assert.Equal(unchecked((short)0x8000), NumberHelper<short>.Min(unchecked((short)0x8000), (short)1));
            Assert.Equal(unchecked((short)0xFFFF), NumberHelper<short>.Min(unchecked((short)0xFFFF), (short)1));
        }

        [Fact]
        public static void MinNumberTest()
        {
            Assert.Equal((short)0x0000, NumberHelper<short>.MinNumber((short)0x0000, (short)1));
            Assert.Equal((short)0x0001, NumberHelper<short>.MinNumber((short)0x0001, (short)1));
            Assert.Equal((short)0x0001, NumberHelper<short>.MinNumber((short)0x7FFF, (short)1));
            Assert.Equal(unchecked((short)0x8000), NumberHelper<short>.MinNumber(unchecked((short)0x8000), (short)1));
            Assert.Equal(unchecked((short)0xFFFF), NumberHelper<short>.MinNumber(unchecked((short)0xFFFF), (short)1));
        }

        [Fact]
        public static void SignTest()
        {
            Assert.Equal(0, NumberHelper<short>.Sign((short)0x0000));
            Assert.Equal(1, NumberHelper<short>.Sign((short)0x0001));
            Assert.Equal(1, NumberHelper<short>.Sign((short)0x7FFF));
            Assert.Equal(-1, NumberHelper<short>.Sign(unchecked((short)0x8000)));
            Assert.Equal(-1, NumberHelper<short>.Sign(unchecked((short)0xFFFF)));
        }

        //
        // INumberBase
        //

        [Fact]
        public static void OneTest()
        {
            Assert.Equal((short)0x0001, NumberBaseHelper<short>.One);
        }

        [Fact]
        public static void RadixTest()
        {
            Assert.Equal(2, NumberBaseHelper<short>.Radix);
        }

        [Fact]
        public static void ZeroTest()
        {
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.Zero);
        }

        [Fact]
        public static void AbsTest()
        {
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.Abs((short)0x0000));
            Assert.Equal((short)0x0001, NumberBaseHelper<short>.Abs((short)0x0001));
            Assert.Equal((short)0x7FFF, NumberBaseHelper<short>.Abs((short)0x7FFF));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<short>.Abs(unchecked((short)0x8000)));
            Assert.Equal((short)0x0001, NumberBaseHelper<short>.Abs(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromByteTest()
        {
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateChecked<byte>(0x00));
            Assert.Equal((short)0x0001, NumberBaseHelper<short>.CreateChecked<byte>(0x01));
            Assert.Equal((short)0x007F, NumberBaseHelper<short>.CreateChecked<byte>(0x7F));
            Assert.Equal((short)0x0080, NumberBaseHelper<short>.CreateChecked<byte>(0x80));
            Assert.Equal((short)0x00FF, NumberBaseHelper<short>.CreateChecked<byte>(0xFF));
        }

        [Fact]
        public static void CreateCheckedFromCharTest()
        {
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateChecked<char>((char)0x0000));
            Assert.Equal((short)0x0001, NumberBaseHelper<short>.CreateChecked<char>((char)0x0001));
            Assert.Equal((short)0x7FFF, NumberBaseHelper<short>.CreateChecked<char>((char)0x7FFF));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<short>.CreateChecked<char>((char)0x8000));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<short>.CreateChecked<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateCheckedFromDecimalTest()
        {
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateChecked<decimal>(-0.0m));
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateChecked<decimal>(+0.0m));
            Assert.Equal((short)0x0001, NumberBaseHelper<short>.CreateChecked<decimal>(+1.0m));

            Assert.Equal(unchecked((short)0xFFFF), NumberBaseHelper<short>.CreateChecked<decimal>(-1.0m));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<short>.CreateChecked<decimal>(decimal.MinValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<short>.CreateChecked<decimal>(decimal.MaxValue));
        }

        [Fact]
        public static void CreateCheckedFromDoubleTest()
        {
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateChecked<double>(+0.0));
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateChecked<double>(-0.0));

            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateChecked<double>(+double.Epsilon));
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateChecked<double>(-double.Epsilon));

            Assert.Equal((short)0x0001, NumberBaseHelper<short>.CreateChecked<double>(+1.0));
            Assert.Equal(unchecked((short)0xFFFF), NumberBaseHelper<short>.CreateChecked<double>(-1.0));

            Assert.Equal((short)0x7FFF, NumberBaseHelper<short>.CreateChecked<double>(+32767.0));
            Assert.Equal(unchecked((short)0x8000), NumberBaseHelper<short>.CreateChecked<double>(-32768.0));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<short>.CreateChecked<double>(+32768.0));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<short>.CreateChecked<double>(-32769.0));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<short>.CreateChecked<double>(double.MaxValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<short>.CreateChecked<double>(double.MinValue));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<short>.CreateChecked<double>(double.PositiveInfinity));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<short>.CreateChecked<double>(double.NegativeInfinity));
        }

        [Fact]
        public static void CreateCheckedFromHalfTest()
        {
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateChecked<Half>(Half.Zero));
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateChecked<Half>(Half.NegativeZero));

            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateChecked<Half>(+Half.Epsilon));
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateChecked<Half>(-Half.Epsilon));

            Assert.Equal((short)0x0001, NumberBaseHelper<short>.CreateChecked<Half>(Half.One));
            Assert.Equal(unchecked((short)0xFFFF), NumberBaseHelper<short>.CreateChecked<Half>(Half.NegativeOne));

            Assert.Equal((short)0x7FF0, NumberBaseHelper<short>.CreateChecked<Half>((Half)32752.0f));
            Assert.Equal(unchecked((short)0x8000), NumberBaseHelper<short>.CreateChecked<Half>((Half)(-32768.0f)));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<short>.CreateChecked<Half>((Half)32768.0f));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<short>.CreateChecked<Half>((Half)(-32800.0f)));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<short>.CreateChecked<Half>(Half.MaxValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<short>.CreateChecked<Half>(Half.MinValue));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<short>.CreateChecked<Half>(Half.PositiveInfinity));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<short>.CreateChecked<Half>(Half.NegativeInfinity));
        }

        [Fact]
        public static void CreateCheckedFromInt16Test()
        {
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateChecked<short>(0x0000));
            Assert.Equal((short)0x0001, NumberBaseHelper<short>.CreateChecked<short>(0x0001));
            Assert.Equal((short)0x7FFF, NumberBaseHelper<short>.CreateChecked<short>(0x7FFF));
            Assert.Equal(unchecked((short)0x8000), NumberBaseHelper<short>.CreateChecked<short>(unchecked((short)0x8000)));
            Assert.Equal(unchecked((short)0xFFFF), NumberBaseHelper<short>.CreateChecked<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromInt32Test()
        {
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateChecked<int>(0x00000000));
            Assert.Equal((short)0x0001, NumberBaseHelper<short>.CreateChecked<int>(0x00000001));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<short>.CreateChecked<int>(0x7FFFFFFF));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<short>.CreateChecked<int>(unchecked((int)0x80000000)));
            Assert.Equal(unchecked((short)0xFFFF), NumberBaseHelper<short>.CreateChecked<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromInt64Test()
        {
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateChecked<long>(0x0000000000000000));
            Assert.Equal((short)0x0001, NumberBaseHelper<short>.CreateChecked<long>(0x0000000000000001));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<short>.CreateChecked<long>(0x7FFFFFFFFFFFFFFF));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<short>.CreateChecked<long>(unchecked((long)0x8000000000000000)));
            Assert.Equal(unchecked((short)0xFFFF), NumberBaseHelper<short>.CreateChecked<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromInt128Test()
        {
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateChecked<Int128>(Int128.Zero));
            Assert.Equal((short)0x0001, NumberBaseHelper<short>.CreateChecked<Int128>(Int128.One));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<short>.CreateChecked<Int128>(Int128.MaxValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<short>.CreateChecked<Int128>(Int128.MinValue));
            Assert.Equal(unchecked((short)0xFFFF), NumberBaseHelper<short>.CreateChecked<Int128>(Int128.NegativeOne));
        }

        [Fact]
        public static void CreateCheckedFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateChecked<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal((short)0x0001, NumberBaseHelper<short>.CreateChecked<nint>(unchecked((nint)0x0000000000000001)));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<short>.CreateChecked<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<short>.CreateChecked<nint>(unchecked((nint)0x8000000000000000)));
                Assert.Equal(unchecked((short)0xFFFF), NumberBaseHelper<short>.CreateChecked<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateChecked<nint>((nint)0x00000000));
                Assert.Equal((short)0x0001, NumberBaseHelper<short>.CreateChecked<nint>((nint)0x00000001));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<short>.CreateChecked<nint>((nint)0x7FFFFFFF));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<short>.CreateChecked<nint>(unchecked((nint)0x80000000)));
                Assert.Equal(unchecked((short)0xFFFF), NumberBaseHelper<short>.CreateChecked<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateCheckedFromNFloatTest()
        {
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateChecked<NFloat>(+0.0f));
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateChecked<NFloat>(-0.0f));

            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateChecked<NFloat>(+NFloat.Epsilon));
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateChecked<NFloat>(-NFloat.Epsilon));

            Assert.Equal((short)0x0001, NumberBaseHelper<short>.CreateChecked<NFloat>(+1.0f));
            Assert.Equal(unchecked((short)0xFFFF), NumberBaseHelper<short>.CreateChecked<NFloat>(-1.0f));

            Assert.Equal((short)0x7FFF, NumberBaseHelper<short>.CreateChecked<NFloat>(+32767.0f));
            Assert.Equal(unchecked((short)0x8000), NumberBaseHelper<short>.CreateChecked<NFloat>(-32768.0f));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<short>.CreateChecked<NFloat>(+32768.0f));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<short>.CreateChecked<NFloat>(-32769.0f));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<short>.CreateChecked<NFloat>(NFloat.MaxValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<short>.CreateChecked<NFloat>(NFloat.MinValue));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<short>.CreateChecked<NFloat>(NFloat.PositiveInfinity));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<short>.CreateChecked<NFloat>(NFloat.NegativeInfinity));
        }

        [Fact]
        public static void CreateCheckedFromSByteTest()
        {
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateChecked<sbyte>(0x00));
            Assert.Equal((short)0x0001, NumberBaseHelper<short>.CreateChecked<sbyte>(0x01));
            Assert.Equal((short)0x007F, NumberBaseHelper<short>.CreateChecked<sbyte>(0x7F));
            Assert.Equal(unchecked((short)0xFF80), NumberBaseHelper<short>.CreateChecked<sbyte>(unchecked((sbyte)0x80)));
            Assert.Equal(unchecked((short)0xFFFF), NumberBaseHelper<short>.CreateChecked<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateCheckedFromSingleTest()
        {
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateChecked<float>(+0.0f));
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateChecked<float>(-0.0f));

            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateChecked<float>(+float.Epsilon));
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateChecked<float>(-float.Epsilon));

            Assert.Equal((short)0x0001, NumberBaseHelper<short>.CreateChecked<float>(+1.0f));
            Assert.Equal(unchecked((short)0xFFFF), NumberBaseHelper<short>.CreateChecked<float>(-1.0f));

            Assert.Equal((short)0x7FFF, NumberBaseHelper<short>.CreateChecked<float>(+32767.0f));
            Assert.Equal(unchecked((short)0x8000), NumberBaseHelper<short>.CreateChecked<float>(-32768.0f));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<short>.CreateChecked<float>(+32768.0f));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<short>.CreateChecked<float>(-32769.0f));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<short>.CreateChecked<float>(float.MaxValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<short>.CreateChecked<float>(float.MinValue));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<short>.CreateChecked<float>(float.PositiveInfinity));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<short>.CreateChecked<float>(float.NegativeInfinity));
        }

        [Fact]
        public static void CreateCheckedFromUInt16Test()
        {
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateChecked<ushort>(0x0000));
            Assert.Equal((short)0x0001, NumberBaseHelper<short>.CreateChecked<ushort>(0x0001));
            Assert.Equal((short)0x7FFF, NumberBaseHelper<short>.CreateChecked<ushort>(0x7FFF));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<short>.CreateChecked<ushort>(0x8000));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<short>.CreateChecked<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUInt32Test()
        {
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateChecked<uint>(0x00000000));
            Assert.Equal((short)0x0001, NumberBaseHelper<short>.CreateChecked<uint>(0x00000001));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<short>.CreateChecked<uint>(0x7FFFFFFF));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<short>.CreateChecked<uint>(0x80000000));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<short>.CreateChecked<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUInt64Test()
        {
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateChecked<ulong>(0x0000000000000000));
            Assert.Equal((short)0x0001, NumberBaseHelper<short>.CreateChecked<ulong>(0x0000000000000001));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<short>.CreateChecked<ulong>(0x7FFFFFFFFFFFFFFF));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<short>.CreateChecked<ulong>(0x8000000000000000));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<short>.CreateChecked<ulong>(0xFFFFFFFFFFFFFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUInt128Test()
        {
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateChecked<UInt128>(UInt128.Zero));
            Assert.Equal((short)0x0001, NumberBaseHelper<short>.CreateChecked<UInt128>(UInt128.One));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<short>.CreateChecked<UInt128>(UInt128Tests_GenericMath.Int128MaxValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<short>.CreateChecked<UInt128>(UInt128Tests_GenericMath.Int128MaxValuePlusOne));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<short>.CreateChecked<UInt128>(UInt128.MaxValue));
        }

        [Fact]
        public static void CreateCheckedFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateChecked<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal((short)0x0001, NumberBaseHelper<short>.CreateChecked<nuint>(unchecked((nuint)0x0000000000000001)));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<short>.CreateChecked<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<short>.CreateChecked<nuint>(unchecked((nuint)0x8000000000000000)));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<short>.CreateChecked<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateChecked<nuint>((nuint)0x00000000));
                Assert.Equal((short)0x0001, NumberBaseHelper<short>.CreateChecked<nuint>((nuint)0x00000001));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<short>.CreateChecked<nuint>((nuint)0x7FFFFFFF));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<short>.CreateChecked<nuint>((nuint)0x80000000));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<short>.CreateChecked<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateSaturatingFromByteTest()
        {
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateSaturating<byte>(0x00));
            Assert.Equal((short)0x0001, NumberBaseHelper<short>.CreateSaturating<byte>(0x01));
            Assert.Equal((short)0x007F, NumberBaseHelper<short>.CreateSaturating<byte>(0x7F));
            Assert.Equal((short)0x0080, NumberBaseHelper<short>.CreateSaturating<byte>(0x80));
            Assert.Equal((short)0x00FF, NumberBaseHelper<short>.CreateSaturating<byte>(0xFF));
        }

        [Fact]
        public static void CreateSaturatingFromCharTest()
        {
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateSaturating<char>((char)0x0000));
            Assert.Equal((short)0x0001, NumberBaseHelper<short>.CreateSaturating<char>((char)0x0001));
            Assert.Equal((short)0x7FFF, NumberBaseHelper<short>.CreateSaturating<char>((char)0x7FFF));
            Assert.Equal((short)0x7FFF, NumberBaseHelper<short>.CreateSaturating<char>((char)0x8000));
            Assert.Equal((short)0x7FFF, NumberBaseHelper<short>.CreateSaturating<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromDecimalTest()
        {
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateSaturating<decimal>(-0.0m));
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateSaturating<decimal>(+0.0m));
            Assert.Equal((short)0x0001, NumberBaseHelper<short>.CreateSaturating<decimal>(+1.0m));

            Assert.Equal(unchecked((short)0xFFFF), NumberBaseHelper<short>.CreateSaturating<decimal>(-1.0m));

            Assert.Equal(unchecked((short)0x8000), NumberBaseHelper<short>.CreateSaturating<decimal>(decimal.MinValue));
            Assert.Equal(unchecked((short)0x7FFF), NumberBaseHelper<short>.CreateSaturating<decimal>(decimal.MaxValue));
        }

        [Fact]
        public static void CreateSaturatingFromDoubleTest()
        {
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateSaturating<double>(+0.0));
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateSaturating<double>(-0.0));

            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateSaturating<double>(+double.Epsilon));
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateSaturating<double>(-double.Epsilon));

            Assert.Equal((short)0x0001, NumberBaseHelper<short>.CreateSaturating<double>(+1.0));
            Assert.Equal(unchecked((short)0xFFFF), NumberBaseHelper<short>.CreateSaturating<double>(-1.0));

            Assert.Equal((short)0x7FFF, NumberBaseHelper<short>.CreateSaturating<double>(+32767.0));
            Assert.Equal(unchecked((short)0x8000), NumberBaseHelper<short>.CreateSaturating<double>(-32768.0));

            Assert.Equal(unchecked((short)0x7FFF), NumberBaseHelper<short>.CreateSaturating<double>(+32768.0));
            Assert.Equal(unchecked((short)0x8000), NumberBaseHelper<short>.CreateSaturating<double>(-32769.0));

            Assert.Equal(unchecked((short)0x7FFF), NumberBaseHelper<short>.CreateSaturating<double>(double.MaxValue));
            Assert.Equal(unchecked((short)0x8000), NumberBaseHelper<short>.CreateSaturating<double>(double.MinValue));

            Assert.Equal(unchecked((short)0x7FFF), NumberBaseHelper<short>.CreateSaturating<double>(double.PositiveInfinity));
            Assert.Equal(unchecked((short)0x8000), NumberBaseHelper<short>.CreateSaturating<double>(double.NegativeInfinity));
        }

        [Fact]
        public static void CreateSaturatingFromHalfTest()
        {
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateSaturating<Half>(Half.Zero));
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateSaturating<Half>(Half.NegativeZero));

            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateSaturating<Half>(+Half.Epsilon));
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateSaturating<Half>(-Half.Epsilon));

            Assert.Equal((short)0x0001, NumberBaseHelper<short>.CreateSaturating<Half>(Half.One));
            Assert.Equal(unchecked((short)0xFFFF), NumberBaseHelper<short>.CreateSaturating<Half>(Half.NegativeOne));

            Assert.Equal((short)0x7FF0, NumberBaseHelper<short>.CreateSaturating<Half>((Half)32752.0f));
            Assert.Equal(unchecked((short)0x8000), NumberBaseHelper<short>.CreateSaturating<Half>((Half)(-32768.0f)));

            Assert.Equal(unchecked((short)0x7FFF), NumberBaseHelper<short>.CreateSaturating<Half>((Half)32768.0f));
            Assert.Equal(unchecked((short)0x8000), NumberBaseHelper<short>.CreateSaturating<Half>((Half)(-32800.0f)));

            Assert.Equal(unchecked((short)0x7FFF), NumberBaseHelper<short>.CreateSaturating<Half>(Half.MaxValue));
            Assert.Equal(unchecked((short)0x8000), NumberBaseHelper<short>.CreateSaturating<Half>(Half.MinValue));

            Assert.Equal(unchecked((short)0x7FFF), NumberBaseHelper<short>.CreateSaturating<Half>(Half.PositiveInfinity));
            Assert.Equal(unchecked((short)0x8000), NumberBaseHelper<short>.CreateSaturating<Half>(Half.NegativeInfinity));
        }

        [Fact]
        public static void CreateSaturatingFromInt16Test()
        {
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateSaturating<short>(0x0000));
            Assert.Equal((short)0x0001, NumberBaseHelper<short>.CreateSaturating<short>(0x0001));
            Assert.Equal((short)0x7FFF, NumberBaseHelper<short>.CreateSaturating<short>(0x7FFF));
            Assert.Equal(unchecked((short)0x8000), NumberBaseHelper<short>.CreateSaturating<short>(unchecked((short)0x8000)));
            Assert.Equal(unchecked((short)0xFFFF), NumberBaseHelper<short>.CreateSaturating<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromInt32Test()
        {
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateSaturating<int>(0x00000000));
            Assert.Equal((short)0x0001, NumberBaseHelper<short>.CreateSaturating<int>(0x00000001));
            Assert.Equal((short)0x7FFF, NumberBaseHelper<short>.CreateSaturating<int>(0x7FFFFFFF));
            Assert.Equal(unchecked((short)0x8000), NumberBaseHelper<short>.CreateSaturating<int>(unchecked((int)0x80000000)));
            Assert.Equal(unchecked((short)0xFFFF), NumberBaseHelper<short>.CreateSaturating<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromInt64Test()
        {
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateSaturating<long>(0x0000000000000000));
            Assert.Equal((short)0x0001, NumberBaseHelper<short>.CreateSaturating<long>(0x0000000000000001));
            Assert.Equal((short)0x7FFF, NumberBaseHelper<short>.CreateSaturating<long>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal(unchecked((short)0x8000), NumberBaseHelper<short>.CreateSaturating<long>(unchecked((long)0x8000000000000000)));
            Assert.Equal(unchecked((short)0xFFFF), NumberBaseHelper<short>.CreateSaturating<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromInt128Test()
        {
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateSaturating<Int128>(Int128.Zero));
            Assert.Equal((short)0x0001, NumberBaseHelper<short>.CreateSaturating<Int128>(Int128.One));
            Assert.Equal(unchecked((short)0x7FFF), NumberBaseHelper<short>.CreateSaturating<Int128>(Int128.MaxValue));
            Assert.Equal(unchecked((short)0x8000), NumberBaseHelper<short>.CreateSaturating<Int128>(Int128.MinValue));
            Assert.Equal(unchecked((short)0xFFFF), NumberBaseHelper<short>.CreateSaturating<Int128>(Int128.NegativeOne));
        }

        [Fact]
        public static void CreateSaturatingFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateSaturating<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal((short)0x0001, NumberBaseHelper<short>.CreateSaturating<nint>(unchecked((nint)0x0000000000000001)));
                Assert.Equal((short)0x7FFF, NumberBaseHelper<short>.CreateSaturating<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((short)0x8000), NumberBaseHelper<short>.CreateSaturating<nint>(unchecked((nint)0x8000000000000000)));
                Assert.Equal(unchecked((short)0xFFFF), NumberBaseHelper<short>.CreateSaturating<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateSaturating<nint>((nint)0x00000000));
                Assert.Equal((short)0x0001, NumberBaseHelper<short>.CreateSaturating<nint>((nint)0x00000001));
                Assert.Equal((short)0x7FFF, NumberBaseHelper<short>.CreateSaturating<nint>((nint)0x7FFFFFFF));
                Assert.Equal(unchecked((short)0x8000), NumberBaseHelper<short>.CreateSaturating<nint>(unchecked((nint)0x80000000)));
                Assert.Equal(unchecked((short)0xFFFF), NumberBaseHelper<short>.CreateSaturating<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateSaturatingFromNFloatTest()
        {
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateSaturating<NFloat>(+0.0f));
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateSaturating<NFloat>(-0.0f));

            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateSaturating<NFloat>(+NFloat.Epsilon));
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateSaturating<NFloat>(-NFloat.Epsilon));

            Assert.Equal((short)0x0001, NumberBaseHelper<short>.CreateSaturating<NFloat>(+1.0f));
            Assert.Equal(unchecked((short)0xFFFF), NumberBaseHelper<short>.CreateSaturating<NFloat>(-1.0f));

            Assert.Equal((short)0x7FFF, NumberBaseHelper<short>.CreateSaturating<NFloat>(+32767.0f));
            Assert.Equal(unchecked((short)0x8000), NumberBaseHelper<short>.CreateSaturating<NFloat>(-32768.0f));

            Assert.Equal(unchecked((short)0x7FFF), NumberBaseHelper<short>.CreateSaturating<NFloat>(+32768.0f));
            Assert.Equal(unchecked((short)0x8000), NumberBaseHelper<short>.CreateSaturating<NFloat>(-32769.0f));

            Assert.Equal(unchecked((short)0x7FFF), NumberBaseHelper<short>.CreateSaturating<NFloat>(NFloat.MaxValue));
            Assert.Equal(unchecked((short)0x8000), NumberBaseHelper<short>.CreateSaturating<NFloat>(NFloat.MinValue));

            Assert.Equal(unchecked((short)0x7FFF), NumberBaseHelper<short>.CreateSaturating<NFloat>(NFloat.PositiveInfinity));
            Assert.Equal(unchecked((short)0x8000), NumberBaseHelper<short>.CreateSaturating<NFloat>(NFloat.NegativeInfinity));
        }

        [Fact]
        public static void CreateSaturatingFromSByteTest()
        {
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateSaturating<sbyte>(0x00));
            Assert.Equal((short)0x0001, NumberBaseHelper<short>.CreateSaturating<sbyte>(0x01));
            Assert.Equal((short)0x007F, NumberBaseHelper<short>.CreateSaturating<sbyte>(0x7F));
            Assert.Equal(unchecked((short)0xFF80), NumberBaseHelper<short>.CreateSaturating<sbyte>(unchecked((sbyte)0x80)));
            Assert.Equal(unchecked((short)0xFFFF), NumberBaseHelper<short>.CreateSaturating<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateSaturatingFromSingleTest()
        {
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateSaturating<float>(+0.0f));
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateSaturating<float>(-0.0f));

            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateSaturating<float>(+float.Epsilon));
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateSaturating<float>(-float.Epsilon));

            Assert.Equal((short)0x0001, NumberBaseHelper<short>.CreateSaturating<float>(+1.0f));
            Assert.Equal(unchecked((short)0xFFFF), NumberBaseHelper<short>.CreateSaturating<float>(-1.0f));

            Assert.Equal((short)0x7FFF, NumberBaseHelper<short>.CreateSaturating<float>(+32767.0f));
            Assert.Equal(unchecked((short)0x8000), NumberBaseHelper<short>.CreateSaturating<float>(-32768.0f));

            Assert.Equal(unchecked((short)0x7FFF), NumberBaseHelper<short>.CreateSaturating<float>(+32768.0f));
            Assert.Equal(unchecked((short)0x8000), NumberBaseHelper<short>.CreateSaturating<float>(-32769.0f));

            Assert.Equal(unchecked((short)0x7FFF), NumberBaseHelper<short>.CreateSaturating<float>(float.MaxValue));
            Assert.Equal(unchecked((short)0x8000), NumberBaseHelper<short>.CreateSaturating<float>(float.MinValue));

            Assert.Equal(unchecked((short)0x7FFF), NumberBaseHelper<short>.CreateSaturating<float>(float.PositiveInfinity));
            Assert.Equal(unchecked((short)0x8000), NumberBaseHelper<short>.CreateSaturating<float>(float.NegativeInfinity));
        }

        [Fact]
        public static void CreateSaturatingFromUInt16Test()
        {
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateSaturating<ushort>(0x0000));
            Assert.Equal((short)0x0001, NumberBaseHelper<short>.CreateSaturating<ushort>(0x0001));
            Assert.Equal((short)0x7FFF, NumberBaseHelper<short>.CreateSaturating<ushort>(0x7FFF));
            Assert.Equal((short)0x7FFF, NumberBaseHelper<short>.CreateSaturating<ushort>(0x8000));
            Assert.Equal((short)0x7FFF, NumberBaseHelper<short>.CreateSaturating<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUInt32Test()
        {
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateSaturating<uint>(0x00000000));
            Assert.Equal((short)0x0001, NumberBaseHelper<short>.CreateSaturating<uint>(0x00000001));
            Assert.Equal((short)0x7FFF, NumberBaseHelper<short>.CreateSaturating<uint>(0x7FFFFFFF));
            Assert.Equal((short)0x7FFF, NumberBaseHelper<short>.CreateSaturating<uint>(0x80000000));
            Assert.Equal((short)0x7FFF, NumberBaseHelper<short>.CreateSaturating<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUInt64Test()
        {
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateSaturating<ulong>(0x0000000000000000));
            Assert.Equal((short)0x0001, NumberBaseHelper<short>.CreateSaturating<ulong>(0x0000000000000001));
            Assert.Equal((short)0x7FFF, NumberBaseHelper<short>.CreateSaturating<ulong>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal((short)0x7FFF, NumberBaseHelper<short>.CreateSaturating<ulong>(0x8000000000000000));
            Assert.Equal((short)0x7FFF, NumberBaseHelper<short>.CreateSaturating<ulong>(0xFFFFFFFFFFFFFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUInt128Test()
        {
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateSaturating<UInt128>(UInt128.Zero));
            Assert.Equal((short)0x0001, NumberBaseHelper<short>.CreateSaturating<UInt128>(UInt128.One));
            Assert.Equal(unchecked((short)0x7FFF), NumberBaseHelper<short>.CreateSaturating<UInt128>(UInt128Tests_GenericMath.Int128MaxValue));
            Assert.Equal(unchecked((short)0x7FFF), NumberBaseHelper<short>.CreateSaturating<UInt128>(UInt128Tests_GenericMath.Int128MaxValuePlusOne));
            Assert.Equal(unchecked((short)0x7FFF), NumberBaseHelper<short>.CreateSaturating<UInt128>(UInt128.MaxValue));
        }

        [Fact]
        public static void CreateSaturatingFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateSaturating<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal((short)0x0001, NumberBaseHelper<short>.CreateSaturating<nuint>(unchecked((nuint)0x0000000000000001)));
                Assert.Equal((short)0x7FFF, NumberBaseHelper<short>.CreateSaturating<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal((short)0x7FFF, NumberBaseHelper<short>.CreateSaturating<nuint>(unchecked((nuint)0x8000000000000000)));
                Assert.Equal((short)0x7FFF, NumberBaseHelper<short>.CreateSaturating<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateSaturating<nuint>((nuint)0x00000000));
                Assert.Equal((short)0x0001, NumberBaseHelper<short>.CreateSaturating<nuint>((nuint)0x00000001));
                Assert.Equal((short)0x7FFF, NumberBaseHelper<short>.CreateSaturating<nuint>((nuint)0x7FFFFFFF));
                Assert.Equal((short)0x7FFF, NumberBaseHelper<short>.CreateSaturating<nuint>((nuint)0x80000000));
                Assert.Equal((short)0x7FFF, NumberBaseHelper<short>.CreateSaturating<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateTruncatingFromByteTest()
        {
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateTruncating<byte>(0x00));
            Assert.Equal((short)0x0001, NumberBaseHelper<short>.CreateTruncating<byte>(0x01));
            Assert.Equal((short)0x007F, NumberBaseHelper<short>.CreateTruncating<byte>(0x7F));
            Assert.Equal((short)0x0080, NumberBaseHelper<short>.CreateTruncating<byte>(0x80));
            Assert.Equal((short)0x00FF, NumberBaseHelper<short>.CreateTruncating<byte>(0xFF));
        }

        [Fact]
        public static void CreateTruncatingFromCharTest()
        {
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateTruncating<char>((char)0x0000));
            Assert.Equal((short)0x0001, NumberBaseHelper<short>.CreateTruncating<char>((char)0x0001));
            Assert.Equal((short)0x7FFF, NumberBaseHelper<short>.CreateTruncating<char>((char)0x7FFF));
            Assert.Equal(unchecked((short)0x8000), NumberBaseHelper<short>.CreateTruncating<char>((char)0x8000));
            Assert.Equal(unchecked((short)0xFFFF), NumberBaseHelper<short>.CreateTruncating<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromDecimalTest()
        {
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateTruncating<decimal>(-0.0m));
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateTruncating<decimal>(+0.0m));
            Assert.Equal((short)0x0001, NumberBaseHelper<short>.CreateTruncating<decimal>(+1.0m));

            Assert.Equal(unchecked((short)0xFFFF), NumberBaseHelper<short>.CreateTruncating<decimal>(-1.0m));

            Assert.Equal(unchecked((short)0x8000), NumberBaseHelper<short>.CreateTruncating<decimal>(decimal.MinValue));
            Assert.Equal(unchecked((short)0x7FFF), NumberBaseHelper<short>.CreateTruncating<decimal>(decimal.MaxValue));
        }

        [Fact]
        public static void CreateTruncatingFromDoubleTest()
        {
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateTruncating<double>(+0.0));
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateTruncating<double>(-0.0));

            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateTruncating<double>(+double.Epsilon));
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateTruncating<double>(-double.Epsilon));

            Assert.Equal((short)0x0001, NumberBaseHelper<short>.CreateTruncating<double>(+1.0));
            Assert.Equal(unchecked((short)0xFFFF), NumberBaseHelper<short>.CreateTruncating<double>(-1.0));

            Assert.Equal((short)0x7FFF, NumberBaseHelper<short>.CreateTruncating<double>(+32767.0));
            Assert.Equal(unchecked((short)0x8000), NumberBaseHelper<short>.CreateTruncating<double>(-32768.0));

            Assert.Equal(unchecked((short)0x7FFF), NumberBaseHelper<short>.CreateTruncating<double>(+32768.0));
            Assert.Equal(unchecked((short)0x8000), NumberBaseHelper<short>.CreateTruncating<double>(-32769.0));

            Assert.Equal(unchecked((short)0x7FFF), NumberBaseHelper<short>.CreateTruncating<double>(double.MaxValue));
            Assert.Equal(unchecked((short)0x8000), NumberBaseHelper<short>.CreateTruncating<double>(double.MinValue));

            Assert.Equal(unchecked((short)0x7FFF), NumberBaseHelper<short>.CreateTruncating<double>(double.PositiveInfinity));
            Assert.Equal(unchecked((short)0x8000), NumberBaseHelper<short>.CreateTruncating<double>(double.NegativeInfinity));
        }

        [Fact]
        public static void CreateTruncatingFromHalfTest()
        {
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateTruncating<Half>(Half.Zero));
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateTruncating<Half>(Half.NegativeZero));

            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateTruncating<Half>(+Half.Epsilon));
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateTruncating<Half>(-Half.Epsilon));

            Assert.Equal((short)0x0001, NumberBaseHelper<short>.CreateTruncating<Half>(Half.One));
            Assert.Equal(unchecked((short)0xFFFF), NumberBaseHelper<short>.CreateTruncating<Half>(Half.NegativeOne));

            Assert.Equal((short)0x7FF0, NumberBaseHelper<short>.CreateTruncating<Half>((Half)32752.0f));
            Assert.Equal(unchecked((short)0x8000), NumberBaseHelper<short>.CreateTruncating<Half>((Half)(-32768.0f)));

            Assert.Equal(unchecked((short)0x7FFF), NumberBaseHelper<short>.CreateTruncating<Half>((Half)32768.0f));
            Assert.Equal(unchecked((short)0x8000), NumberBaseHelper<short>.CreateTruncating<Half>((Half)(-32800.0f)));

            Assert.Equal(unchecked((short)0x7FFF), NumberBaseHelper<short>.CreateTruncating<Half>(Half.MaxValue));
            Assert.Equal(unchecked((short)0x8000), NumberBaseHelper<short>.CreateTruncating<Half>(Half.MinValue));

            Assert.Equal(unchecked((short)0x7FFF), NumberBaseHelper<short>.CreateTruncating<Half>(Half.PositiveInfinity));
            Assert.Equal(unchecked((short)0x8000), NumberBaseHelper<short>.CreateTruncating<Half>(Half.NegativeInfinity));
        }

        [Fact]
        public static void CreateTruncatingFromInt16Test()
        {
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateTruncating<short>(0x0000));
            Assert.Equal((short)0x0001, NumberBaseHelper<short>.CreateTruncating<short>(0x0001));
            Assert.Equal((short)0x7FFF, NumberBaseHelper<short>.CreateTruncating<short>(0x7FFF));
            Assert.Equal(unchecked((short)0x8000), NumberBaseHelper<short>.CreateTruncating<short>(unchecked((short)0x8000)));
            Assert.Equal(unchecked((short)0xFFFF), NumberBaseHelper<short>.CreateTruncating<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateTruncatingFromInt32Test()
        {
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateTruncating<int>(0x00000000));
            Assert.Equal((short)0x0001, NumberBaseHelper<short>.CreateTruncating<int>(0x00000001));
            Assert.Equal(unchecked((short)0xFFFF), NumberBaseHelper<short>.CreateTruncating<int>(0x7FFFFFFF));
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateTruncating<int>(unchecked((int)0x80000000)));
            Assert.Equal(unchecked((short)0xFFFF), NumberBaseHelper<short>.CreateTruncating<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateTruncatingFromInt64Test()
        {
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateTruncating<long>(0x0000000000000000));
            Assert.Equal((short)0x0001, NumberBaseHelper<short>.CreateTruncating<long>(0x0000000000000001));
            Assert.Equal(unchecked((short)0xFFFF), NumberBaseHelper<short>.CreateTruncating<long>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateTruncating<long>(unchecked((long)0x8000000000000000)));
            Assert.Equal(unchecked((short)0xFFFF), NumberBaseHelper<short>.CreateTruncating<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void CreateTruncatingFromInt128Test()
        {
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateTruncating<Int128>(Int128.Zero));
            Assert.Equal((short)0x0001, NumberBaseHelper<short>.CreateTruncating<Int128>(Int128.One));
            Assert.Equal(unchecked((short)0xFFFF), NumberBaseHelper<short>.CreateTruncating<Int128>(Int128.MaxValue));
            Assert.Equal(unchecked((short)0x0000), NumberBaseHelper<short>.CreateTruncating<Int128>(Int128.MinValue));
            Assert.Equal(unchecked((short)0xFFFF), NumberBaseHelper<short>.CreateTruncating<Int128>(Int128.NegativeOne));
        }

        [Fact]
        public static void CreateTruncatingFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateTruncating<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal((short)0x0001, NumberBaseHelper<short>.CreateTruncating<nint>(unchecked((nint)0x0000000000000001)));
                Assert.Equal(unchecked((short)0xFFFF), NumberBaseHelper<short>.CreateTruncating<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateTruncating<nint>(unchecked((nint)0x8000000000000000)));
                Assert.Equal(unchecked((short)0xFFFF), NumberBaseHelper<short>.CreateTruncating<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateTruncating<nint>((nint)0x00000000));
                Assert.Equal((short)0x0001, NumberBaseHelper<short>.CreateTruncating<nint>((nint)0x00000001));
                Assert.Equal(unchecked((short)0xFFFF), NumberBaseHelper<short>.CreateTruncating<nint>((nint)0x7FFFFFFF));
                Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateTruncating<nint>(unchecked((nint)0x80000000)));
                Assert.Equal(unchecked((short)0xFFFF), NumberBaseHelper<short>.CreateTruncating<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateTruncatingFromNFloatTest()
        {
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateTruncating<NFloat>(+0.0f));
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateTruncating<NFloat>(-0.0f));

            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateTruncating<NFloat>(+NFloat.Epsilon));
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateTruncating<NFloat>(-NFloat.Epsilon));

            Assert.Equal((short)0x0001, NumberBaseHelper<short>.CreateTruncating<NFloat>(+1.0f));
            Assert.Equal(unchecked((short)0xFFFF), NumberBaseHelper<short>.CreateTruncating<NFloat>(-1.0f));

            Assert.Equal((short)0x7FFF, NumberBaseHelper<short>.CreateTruncating<NFloat>(+32767.0f));
            Assert.Equal(unchecked((short)0x8000), NumberBaseHelper<short>.CreateTruncating<NFloat>(-32768.0f));

            Assert.Equal(unchecked((short)0x7FFF), NumberBaseHelper<short>.CreateTruncating<NFloat>(+32768.0f));
            Assert.Equal(unchecked((short)0x8000), NumberBaseHelper<short>.CreateTruncating<NFloat>(-32769.0f));

            Assert.Equal(unchecked((short)0x7FFF), NumberBaseHelper<short>.CreateTruncating<NFloat>(NFloat.MaxValue));
            Assert.Equal(unchecked((short)0x8000), NumberBaseHelper<short>.CreateTruncating<NFloat>(NFloat.MinValue));

            Assert.Equal(unchecked((short)0x7FFF), NumberBaseHelper<short>.CreateTruncating<NFloat>(NFloat.PositiveInfinity));
            Assert.Equal(unchecked((short)0x8000), NumberBaseHelper<short>.CreateTruncating<NFloat>(NFloat.NegativeInfinity));
        }

        [Fact]
        public static void CreateTruncatingFromSByteTest()
        {
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateTruncating<sbyte>(0x00));
            Assert.Equal((short)0x0001, NumberBaseHelper<short>.CreateTruncating<sbyte>(0x01));
            Assert.Equal((short)0x007F, NumberBaseHelper<short>.CreateTruncating<sbyte>(0x7F));
            Assert.Equal(unchecked((short)0xFF80), NumberBaseHelper<short>.CreateTruncating<sbyte>(unchecked((sbyte)0x80)));
            Assert.Equal(unchecked((short)0xFFFF), NumberBaseHelper<short>.CreateTruncating<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateTruncatingFromSingleTest()
        {
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateTruncating<float>(+0.0f));
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateTruncating<float>(-0.0f));

            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateTruncating<float>(+float.Epsilon));
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateTruncating<float>(-float.Epsilon));

            Assert.Equal((short)0x0001, NumberBaseHelper<short>.CreateTruncating<float>(+1.0f));
            Assert.Equal(unchecked((short)0xFFFF), NumberBaseHelper<short>.CreateTruncating<float>(-1.0f));

            Assert.Equal((short)0x7FFF, NumberBaseHelper<short>.CreateTruncating<float>(+32767.0f));
            Assert.Equal(unchecked((short)0x8000), NumberBaseHelper<short>.CreateTruncating<float>(-32768.0f));

            Assert.Equal(unchecked((short)0x7FFF), NumberBaseHelper<short>.CreateTruncating<float>(+32768.0f));
            Assert.Equal(unchecked((short)0x8000), NumberBaseHelper<short>.CreateTruncating<float>(-32769.0f));

            Assert.Equal(unchecked((short)0x7FFF), NumberBaseHelper<short>.CreateTruncating<float>(float.MaxValue));
            Assert.Equal(unchecked((short)0x8000), NumberBaseHelper<short>.CreateTruncating<float>(float.MinValue));

            Assert.Equal(unchecked((short)0x7FFF), NumberBaseHelper<short>.CreateTruncating<float>(float.PositiveInfinity));
            Assert.Equal(unchecked((short)0x8000), NumberBaseHelper<short>.CreateTruncating<float>(float.NegativeInfinity));
        }

        [Fact]
        public static void CreateTruncatingFromUInt16Test()
        {
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateTruncating<ushort>(0x0000));
            Assert.Equal((short)0x0001, NumberBaseHelper<short>.CreateTruncating<ushort>(0x0001));
            Assert.Equal((short)0x7FFF, NumberBaseHelper<short>.CreateTruncating<ushort>(0x7FFF));
            Assert.Equal(unchecked((short)0x8000), NumberBaseHelper<short>.CreateTruncating<ushort>(0x8000));
            Assert.Equal(unchecked((short)0xFFFF), NumberBaseHelper<short>.CreateTruncating<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt32Test()
        {
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateTruncating<uint>(0x00000000));
            Assert.Equal((short)0x0001, NumberBaseHelper<short>.CreateTruncating<uint>(0x00000001));
            Assert.Equal(unchecked((short)0xFFFF), NumberBaseHelper<short>.CreateTruncating<uint>(0x7FFFFFFF));
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateTruncating<uint>(0x80000000));
            Assert.Equal(unchecked((short)0xFFFF), NumberBaseHelper<short>.CreateTruncating<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt64Test()
        {
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateTruncating<ulong>(0x0000000000000000));
            Assert.Equal((short)0x0001, NumberBaseHelper<short>.CreateTruncating<ulong>(0x0000000000000001));
            Assert.Equal(unchecked((short)0xFFFF), NumberBaseHelper<short>.CreateTruncating<ulong>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateTruncating<ulong>(0x8000000000000000));
            Assert.Equal(unchecked((short)0xFFFF), NumberBaseHelper<short>.CreateTruncating<ulong>(0xFFFFFFFFFFFFFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt128Test()
        {
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateTruncating<UInt128>(UInt128.Zero));
            Assert.Equal((short)0x0001, NumberBaseHelper<short>.CreateTruncating<UInt128>(UInt128.One));
            Assert.Equal(unchecked((short)0xFFFF), NumberBaseHelper<short>.CreateTruncating<UInt128>(UInt128Tests_GenericMath.Int128MaxValue));
            Assert.Equal(unchecked((short)0x0000), NumberBaseHelper<short>.CreateTruncating<UInt128>(UInt128Tests_GenericMath.Int128MaxValuePlusOne));
            Assert.Equal(unchecked((short)0xFFFF), NumberBaseHelper<short>.CreateTruncating<UInt128>(UInt128.MaxValue));
        }

        [Fact]
        public static void CreateTruncatingFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateTruncating<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal((short)0x0001, NumberBaseHelper<short>.CreateTruncating<nuint>(unchecked((nuint)0x0000000000000001)));
                Assert.Equal(unchecked((short)0xFFFF), NumberBaseHelper<short>.CreateTruncating<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateTruncating<nuint>(unchecked((nuint)0x8000000000000000)));
                Assert.Equal(unchecked((short)0xFFFF), NumberBaseHelper<short>.CreateTruncating<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateTruncating<nuint>((nuint)0x00000000));
                Assert.Equal((short)0x0001, NumberBaseHelper<short>.CreateTruncating<nuint>((nuint)0x00000001));
                Assert.Equal(unchecked((short)0xFFFF), NumberBaseHelper<short>.CreateTruncating<nuint>((nuint)0x7FFFFFFF));
                Assert.Equal((short)0x0000, NumberBaseHelper<short>.CreateTruncating<nuint>((nuint)0x80000000));
                Assert.Equal(unchecked((short)0xFFFF), NumberBaseHelper<short>.CreateTruncating<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void IsCanonicalTest()
        {
            Assert.True(NumberBaseHelper<short>.IsCanonical((short)0x0000));
            Assert.True(NumberBaseHelper<short>.IsCanonical((short)0x0001));
            Assert.True(NumberBaseHelper<short>.IsCanonical((short)0x7FFF));
            Assert.True(NumberBaseHelper<short>.IsCanonical(unchecked((short)0x8000)));
            Assert.True(NumberBaseHelper<short>.IsCanonical(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void IsComplexNumberTest()
        {
            Assert.False(NumberBaseHelper<short>.IsComplexNumber((short)0x0000));
            Assert.False(NumberBaseHelper<short>.IsComplexNumber((short)0x0001));
            Assert.False(NumberBaseHelper<short>.IsComplexNumber((short)0x7FFF));
            Assert.False(NumberBaseHelper<short>.IsComplexNumber(unchecked((short)0x8000)));
            Assert.False(NumberBaseHelper<short>.IsComplexNumber(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void IsEvenIntegerTest()
        {
            Assert.True(NumberBaseHelper<short>.IsEvenInteger((short)0x0000));
            Assert.False(NumberBaseHelper<short>.IsEvenInteger((short)0x0001));
            Assert.False(NumberBaseHelper<short>.IsEvenInteger((short)0x7FFF));
            Assert.True(NumberBaseHelper<short>.IsEvenInteger(unchecked((short)0x8000)));
            Assert.False(NumberBaseHelper<short>.IsEvenInteger(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void IsFiniteTest()
        {
            Assert.True(NumberBaseHelper<short>.IsFinite((short)0x0000));
            Assert.True(NumberBaseHelper<short>.IsFinite((short)0x0001));
            Assert.True(NumberBaseHelper<short>.IsFinite((short)0x7FFF));
            Assert.True(NumberBaseHelper<short>.IsFinite(unchecked((short)0x8000)));
            Assert.True(NumberBaseHelper<short>.IsFinite(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void IsImaginaryNumberTest()
        {
            Assert.False(NumberBaseHelper<short>.IsImaginaryNumber((short)0x0000));
            Assert.False(NumberBaseHelper<short>.IsImaginaryNumber((short)0x0001));
            Assert.False(NumberBaseHelper<short>.IsImaginaryNumber((short)0x7FFF));
            Assert.False(NumberBaseHelper<short>.IsImaginaryNumber(unchecked((short)0x8000)));
            Assert.False(NumberBaseHelper<short>.IsImaginaryNumber(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void IsInfinityTest()
        {
            Assert.False(NumberBaseHelper<short>.IsInfinity((short)0x0000));
            Assert.False(NumberBaseHelper<short>.IsInfinity((short)0x0001));
            Assert.False(NumberBaseHelper<short>.IsInfinity((short)0x7FFF));
            Assert.False(NumberBaseHelper<short>.IsInfinity(unchecked((short)0x8000)));
            Assert.False(NumberBaseHelper<short>.IsInfinity(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void IsIntegerTest()
        {
            Assert.True(NumberBaseHelper<short>.IsInteger((short)0x0000));
            Assert.True(NumberBaseHelper<short>.IsInteger((short)0x0001));
            Assert.True(NumberBaseHelper<short>.IsInteger((short)0x7FFF));
            Assert.True(NumberBaseHelper<short>.IsInteger(unchecked((short)0x8000)));
            Assert.True(NumberBaseHelper<short>.IsInteger(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void IsNaNTest()
        {
            Assert.False(NumberBaseHelper<short>.IsNaN((short)0x0000));
            Assert.False(NumberBaseHelper<short>.IsNaN((short)0x0001));
            Assert.False(NumberBaseHelper<short>.IsNaN((short)0x7FFF));
            Assert.False(NumberBaseHelper<short>.IsNaN(unchecked((short)0x8000)));
            Assert.False(NumberBaseHelper<short>.IsNaN(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void IsNegativeTest()
        {
            Assert.False(NumberBaseHelper<short>.IsNegative((short)0x0000));
            Assert.False(NumberBaseHelper<short>.IsNegative((short)0x0001));
            Assert.False(NumberBaseHelper<short>.IsNegative((short)0x7FFF));
            Assert.True(NumberBaseHelper<short>.IsNegative(unchecked((short)0x8000)));
            Assert.True(NumberBaseHelper<short>.IsNegative(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void IsNegativeInfinityTest()
        {
            Assert.False(NumberBaseHelper<short>.IsNegativeInfinity((short)0x0000));
            Assert.False(NumberBaseHelper<short>.IsNegativeInfinity((short)0x0001));
            Assert.False(NumberBaseHelper<short>.IsNegativeInfinity((short)0x7FFF));
            Assert.False(NumberBaseHelper<short>.IsNegativeInfinity(unchecked((short)0x8000)));
            Assert.False(NumberBaseHelper<short>.IsNegativeInfinity(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void IsNormalTest()
        {
            Assert.False(NumberBaseHelper<short>.IsNormal((short)0x0000));
            Assert.True(NumberBaseHelper<short>.IsNormal((short)0x0001));
            Assert.True(NumberBaseHelper<short>.IsNormal((short)0x7FFF));
            Assert.True(NumberBaseHelper<short>.IsNormal(unchecked((short)0x8000)));
            Assert.True(NumberBaseHelper<short>.IsNormal(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void IsOddIntegerTest()
        {
            Assert.False(NumberBaseHelper<short>.IsOddInteger((short)0x0000));
            Assert.True(NumberBaseHelper<short>.IsOddInteger((short)0x0001));
            Assert.True(NumberBaseHelper<short>.IsOddInteger((short)0x7FFF));
            Assert.False(NumberBaseHelper<short>.IsOddInteger(unchecked((short)0x8000)));
            Assert.True(NumberBaseHelper<short>.IsOddInteger(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void IsPositiveTest()
        {
            Assert.True(NumberBaseHelper<short>.IsPositive((short)0x0000));
            Assert.True(NumberBaseHelper<short>.IsPositive((short)0x0001));
            Assert.True(NumberBaseHelper<short>.IsPositive((short)0x7FFF));
            Assert.False(NumberBaseHelper<short>.IsPositive(unchecked((short)0x8000)));
            Assert.False(NumberBaseHelper<short>.IsPositive(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void IsPositiveInfinityTest()
        {
            Assert.False(NumberBaseHelper<short>.IsPositiveInfinity((short)0x0000));
            Assert.False(NumberBaseHelper<short>.IsPositiveInfinity((short)0x0001));
            Assert.False(NumberBaseHelper<short>.IsPositiveInfinity((short)0x7FFF));
            Assert.False(NumberBaseHelper<short>.IsPositiveInfinity(unchecked((short)0x8000)));
            Assert.False(NumberBaseHelper<short>.IsPositiveInfinity(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void IsRealNumberTest()
        {
            Assert.True(NumberBaseHelper<short>.IsRealNumber((short)0x0000));
            Assert.True(NumberBaseHelper<short>.IsRealNumber((short)0x0001));
            Assert.True(NumberBaseHelper<short>.IsRealNumber((short)0x7FFF));
            Assert.True(NumberBaseHelper<short>.IsRealNumber(unchecked((short)0x8000)));
            Assert.True(NumberBaseHelper<short>.IsRealNumber(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void IsSubnormalTest()
        {
            Assert.False(NumberBaseHelper<short>.IsSubnormal((short)0x0000));
            Assert.False(NumberBaseHelper<short>.IsSubnormal((short)0x0001));
            Assert.False(NumberBaseHelper<short>.IsSubnormal((short)0x7FFF));
            Assert.False(NumberBaseHelper<short>.IsSubnormal(unchecked((short)0x8000)));
            Assert.False(NumberBaseHelper<short>.IsSubnormal(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void IsZeroTest()
        {
            Assert.True(NumberBaseHelper<short>.IsZero((short)0x0000));
            Assert.False(NumberBaseHelper<short>.IsZero((short)0x0001));
            Assert.False(NumberBaseHelper<short>.IsZero((short)0x7FFF));
            Assert.False(NumberBaseHelper<short>.IsZero(unchecked((short)0x8000)));
            Assert.False(NumberBaseHelper<short>.IsZero(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void MaxMagnitudeTest()
        {
            Assert.Equal((short)0x0001, NumberBaseHelper<short>.MaxMagnitude((short)0x0000, (short)1));
            Assert.Equal((short)0x0001, NumberBaseHelper<short>.MaxMagnitude((short)0x0001, (short)1));
            Assert.Equal((short)0x7FFF, NumberBaseHelper<short>.MaxMagnitude((short)0x7FFF, (short)1));
            Assert.Equal(unchecked((short)0x8000), NumberBaseHelper<short>.MaxMagnitude(unchecked((short)0x8000), (short)1));
            Assert.Equal((short)0x0001, NumberBaseHelper<short>.MaxMagnitude(unchecked((short)0xFFFF), (short)1));
        }

        [Fact]
        public static void MaxMagnitudeNumberTest()
        {
            Assert.Equal((short)0x0001, NumberBaseHelper<short>.MaxMagnitudeNumber((short)0x0000, (short)1));
            Assert.Equal((short)0x0001, NumberBaseHelper<short>.MaxMagnitudeNumber((short)0x0001, (short)1));
            Assert.Equal((short)0x7FFF, NumberBaseHelper<short>.MaxMagnitudeNumber((short)0x7FFF, (short)1));
            Assert.Equal(unchecked((short)0x8000), NumberBaseHelper<short>.MaxMagnitudeNumber(unchecked((short)0x8000), (short)1));
            Assert.Equal((short)0x0001, NumberBaseHelper<short>.MaxMagnitudeNumber(unchecked((short)0xFFFF), (short)1));
        }

        [Fact]
        public static void MinMagnitudeTest()
        {
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.MinMagnitude((short)0x0000, (short)1));
            Assert.Equal((short)0x0001, NumberBaseHelper<short>.MinMagnitude((short)0x0001, (short)1));
            Assert.Equal((short)0x0001, NumberBaseHelper<short>.MinMagnitude((short)0x7FFF, (short)1));
            Assert.Equal((short)0x0001, NumberBaseHelper<short>.MinMagnitude(unchecked((short)0x8000), (short)1));
            Assert.Equal(unchecked((short)0xFFFF), NumberBaseHelper<short>.MinMagnitude(unchecked((short)0xFFFF), (short)1));
        }

        [Fact]
        public static void MinMagnitudeNumberTest()
        {
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.MinMagnitudeNumber((short)0x0000, (short)1));
            Assert.Equal((short)0x0001, NumberBaseHelper<short>.MinMagnitudeNumber((short)0x0001, (short)1));
            Assert.Equal((short)0x0001, NumberBaseHelper<short>.MinMagnitudeNumber((short)0x7FFF, (short)1));
            Assert.Equal((short)0x0001, NumberBaseHelper<short>.MinMagnitudeNumber(unchecked((short)0x8000), (short)1));
            Assert.Equal(unchecked((short)0xFFFF), NumberBaseHelper<short>.MinMagnitudeNumber(unchecked((short)0xFFFF), (short)1));
        }

        //
        // IShiftOperators
        //

        [Fact]
        public static void op_LeftShiftTest()
        {
            Assert.Equal((short)0x0000, ShiftOperatorsHelper<short, int, short>.op_LeftShift((short)0x0000, 1));
            Assert.Equal((short)0x0002, ShiftOperatorsHelper<short, int, short>.op_LeftShift((short)0x0001, 1));
            Assert.Equal(unchecked((short)0xFFFE), ShiftOperatorsHelper<short, int, short>.op_LeftShift((short)0x7FFF, 1));
            Assert.Equal((short)0x0000, ShiftOperatorsHelper<short, int, short>.op_LeftShift(unchecked((short)0x8000), 1));
            Assert.Equal(unchecked((short)0xFFFE), ShiftOperatorsHelper<short, int, short>.op_LeftShift(unchecked((short)0xFFFF), 1));
        }

        [Fact]
        public static void op_RightShiftTest()
        {
            Assert.Equal((short)0x0000, ShiftOperatorsHelper<short, int, short>.op_RightShift((short)0x0000, 1));
            Assert.Equal((short)0x0000, ShiftOperatorsHelper<short, int, short>.op_RightShift((short)0x0001, 1));
            Assert.Equal((short)0x3FFF, ShiftOperatorsHelper<short, int, short>.op_RightShift((short)0x7FFF, 1));
            Assert.Equal(unchecked((short)0xC000), ShiftOperatorsHelper<short, int, short>.op_RightShift(unchecked((short)0x8000), 1));
            Assert.Equal(unchecked((short)0xFFFF), ShiftOperatorsHelper<short, int, short>.op_RightShift(unchecked((short)0xFFFF), 1));
        }

        [Fact]
        public static void op_UnsignedRightShiftTest()
        {
            Assert.Equal((short)0x0000, ShiftOperatorsHelper<short, int, short>.op_UnsignedRightShift((short)0x0000, 1));
            Assert.Equal((short)0x0000, ShiftOperatorsHelper<short, int, short>.op_UnsignedRightShift((short)0x0001, 1));
            Assert.Equal((short)0x3FFF, ShiftOperatorsHelper<short, int, short>.op_UnsignedRightShift((short)0x7FFF, 1));
            Assert.Equal((short)0x4000, ShiftOperatorsHelper<short, int, short>.op_UnsignedRightShift(unchecked((short)0x8000), 1));
            Assert.Equal((short)0x7FFF, ShiftOperatorsHelper<short, int, short>.op_UnsignedRightShift(unchecked((short)0xFFFF), 1));
        }

        //
        // ISignedNumber
        //

        [Fact]
        public static void NegativeOneTest()
        {
            Assert.Equal(unchecked((short)0xFFFF), SignedNumberHelper<short>.NegativeOne);
        }

        //
        // ISubtractionOperators
        //

        [Fact]
        public static void op_SubtractionTest()
        {
            Assert.Equal(unchecked((short)0xFFFF), SubtractionOperatorsHelper<short, short, short>.op_Subtraction((short)0x0000, (short)1));
            Assert.Equal((short)0x0000, SubtractionOperatorsHelper<short, short, short>.op_Subtraction((short)0x0001, (short)1));
            Assert.Equal((short)0x7FFE, SubtractionOperatorsHelper<short, short, short>.op_Subtraction((short)0x7FFF, (short)1));
            Assert.Equal((short)0x7FFF, SubtractionOperatorsHelper<short, short, short>.op_Subtraction(unchecked((short)0x8000), (short)1));
            Assert.Equal(unchecked((short)0xFFFE), SubtractionOperatorsHelper<short, short, short>.op_Subtraction(unchecked((short)0xFFFF), (short)1));
        }

        [Fact]
        public static void op_CheckedSubtractionTest()
        {
            Assert.Equal(unchecked((short)0xFFFF), SubtractionOperatorsHelper<short, short, short>.op_CheckedSubtraction((short)0x0000, (short)1));
            Assert.Equal((short)0x0000, SubtractionOperatorsHelper<short, short, short>.op_CheckedSubtraction((short)0x0001, (short)1));
            Assert.Equal((short)0x7FFE, SubtractionOperatorsHelper<short, short, short>.op_CheckedSubtraction((short)0x7FFF, (short)1));
            Assert.Equal(unchecked((short)0xFFFE), SubtractionOperatorsHelper<short, short, short>.op_CheckedSubtraction(unchecked((short)0xFFFF), (short)1));

            Assert.Throws<OverflowException>(() => SubtractionOperatorsHelper<short, short, short>.op_CheckedSubtraction(unchecked((short)0x8000), (short)1));
        }

        //
        // IUnaryNegationOperators
        //

        [Fact]
        public static void op_UnaryNegationTest()
        {
            Assert.Equal((short)0x0000, UnaryNegationOperatorsHelper<short, short>.op_UnaryNegation((short)0x0000));
            Assert.Equal(unchecked((short)0xFFFF), UnaryNegationOperatorsHelper<short, short>.op_UnaryNegation((short)0x0001));
            Assert.Equal(unchecked((short)0x8001), UnaryNegationOperatorsHelper<short, short>.op_UnaryNegation((short)0x7FFF));
            Assert.Equal(unchecked((short)0x8000), UnaryNegationOperatorsHelper<short, short>.op_UnaryNegation(unchecked((short)0x8000)));
            Assert.Equal((short)0x0001, UnaryNegationOperatorsHelper<short, short>.op_UnaryNegation(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void op_CheckedUnaryNegationTest()
        {
            Assert.Equal((short)0x0000, UnaryNegationOperatorsHelper<short, short>.op_CheckedUnaryNegation((short)0x0000));
            Assert.Equal(unchecked((short)0xFFFF), UnaryNegationOperatorsHelper<short, short>.op_CheckedUnaryNegation((short)0x0001));
            Assert.Equal(unchecked((short)0x8001), UnaryNegationOperatorsHelper<short, short>.op_CheckedUnaryNegation((short)0x7FFF));
            Assert.Equal((short)0x0001, UnaryNegationOperatorsHelper<short, short>.op_CheckedUnaryNegation(unchecked((short)0xFFFF)));

            Assert.Throws<OverflowException>(() => UnaryNegationOperatorsHelper<short, short>.op_CheckedUnaryNegation(unchecked((short)0x8000)));
        }

        //
        // IUnaryPlusOperators
        //

        [Fact]
        public static void op_UnaryPlusTest()
        {
            Assert.Equal((short)0x0000, UnaryPlusOperatorsHelper<short, short>.op_UnaryPlus((short)0x0000));
            Assert.Equal((short)0x0001, UnaryPlusOperatorsHelper<short, short>.op_UnaryPlus((short)0x0001));
            Assert.Equal((short)0x7FFF, UnaryPlusOperatorsHelper<short, short>.op_UnaryPlus((short)0x7FFF));
            Assert.Equal(unchecked((short)0x8000), UnaryPlusOperatorsHelper<short, short>.op_UnaryPlus(unchecked((short)0x8000)));
            Assert.Equal(unchecked((short)0xFFFF), UnaryPlusOperatorsHelper<short, short>.op_UnaryPlus(unchecked((short)0xFFFF)));
        }

        //
        // IParsable and ISpanParsable
        //

        [Theory]
        [MemberData(nameof(Int16Tests.Parse_Valid_TestData), MemberType = typeof(Int16Tests))]
        public static void ParseValidStringTest(string value, NumberStyles style, IFormatProvider provider, short expected)
        {
            short result;

            // Default style and provider
            if ((style == NumberStyles.Integer) && (provider is null))
            {
                Assert.True(ParsableHelper<short>.TryParse(value, provider, out result));
                Assert.Equal(expected, result);
                Assert.Equal(expected, ParsableHelper<short>.Parse(value, provider));
            }

            // Default provider
            if (provider is null)
            {
                Assert.Equal(expected, NumberBaseHelper<short>.Parse(value, style, provider));

                // Substitute default NumberFormatInfo
                Assert.True(NumberBaseHelper<short>.TryParse(value, style, new NumberFormatInfo(), out result));
                Assert.Equal(expected, result);
                Assert.Equal(expected, NumberBaseHelper<short>.Parse(value, style, new NumberFormatInfo()));
            }

            // Default style
            if (style == NumberStyles.Integer)
            {
                Assert.Equal(expected, ParsableHelper<short>.Parse(value, provider));
            }

            // Full overloads
            Assert.True(NumberBaseHelper<short>.TryParse(value, style, provider, out result));
            Assert.Equal(expected, result);
            Assert.Equal(expected, NumberBaseHelper<short>.Parse(value, style, provider));
        }

        [Theory]
        [MemberData(nameof(Int16Tests.Parse_Invalid_TestData), MemberType = typeof(Int16Tests))]
        public static void ParseInvalidStringTest(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            short result;

            // Default style and provider
            if ((style == NumberStyles.Integer) && (provider is null))
            {
                Assert.False(ParsableHelper<short>.TryParse(value, provider, out result));
                Assert.Equal(default(short), result);
                Assert.Throws(exceptionType, () => ParsableHelper<short>.Parse(value, provider));
            }

            // Default provider
            if (provider is null)
            {
                Assert.Throws(exceptionType, () => NumberBaseHelper<short>.Parse(value, style, provider));

                // Substitute default NumberFormatInfo
                Assert.False(NumberBaseHelper<short>.TryParse(value, style, new NumberFormatInfo(), out result));
                Assert.Equal(default(short), result);
                Assert.Throws(exceptionType, () => NumberBaseHelper<short>.Parse(value, style, new NumberFormatInfo()));
            }

            // Default style
            if (style == NumberStyles.Integer)
            {
                Assert.Throws(exceptionType, () => ParsableHelper<short>.Parse(value, provider));
            }

            // Full overloads
            Assert.False(NumberBaseHelper<short>.TryParse(value, style, provider, out result));
            Assert.Equal(default(short), result);
            Assert.Throws(exceptionType, () => NumberBaseHelper<short>.Parse(value, style, provider));
        }

        [Theory]
        [MemberData(nameof(Int16Tests.Parse_ValidWithOffsetCount_TestData), MemberType = typeof(Int16Tests))]
        public static void ParseValidSpanTest(string value, int offset, int count, NumberStyles style, IFormatProvider provider, short expected)
        {
            short result;

            // Default style and provider
            if ((style == NumberStyles.Integer) && (provider is null))
            {
                Assert.True(SpanParsableHelper<short>.TryParse(value.AsSpan(offset, count), provider, out result));
                Assert.Equal(expected, result);
            }

            Assert.Equal(expected, NumberBaseHelper<short>.Parse(value.AsSpan(offset, count), style, provider));

            Assert.True(NumberBaseHelper<short>.TryParse(value.AsSpan(offset, count), style, provider, out result));
            Assert.Equal(expected, result);
        }

        [Theory]
        [MemberData(nameof(Int16Tests.Parse_Invalid_TestData), MemberType = typeof(Int16Tests))]
        public static void ParseInvalidSpanTest(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            if (value is null)
            {
                return;
            }

            short result;

            // Default style and provider
            if ((style == NumberStyles.Integer) && (provider is null))
            {
                Assert.False(SpanParsableHelper<short>.TryParse(value.AsSpan(), provider, out result));
                Assert.Equal(default(short), result);
            }

            Assert.Throws(exceptionType, () => NumberBaseHelper<short>.Parse(value.AsSpan(), style, provider));

            Assert.False(NumberBaseHelper<short>.TryParse(value.AsSpan(), style, provider, out result));
            Assert.Equal(default(short), result);
        }
    }
}
