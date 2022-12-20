// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Tests
{
    public class UInt16Tests_GenericMath
    {
        //
        // IAdditionOperators
        //

        [Fact]
        public static void op_AdditionTest()
        {
            Assert.Equal((ushort)0x0001, AdditionOperatorsHelper<ushort, ushort, ushort>.op_Addition((ushort)0x0000, (ushort)1));
            Assert.Equal((ushort)0x0002, AdditionOperatorsHelper<ushort, ushort, ushort>.op_Addition((ushort)0x0001, (ushort)1));
            Assert.Equal((ushort)0x8000, AdditionOperatorsHelper<ushort, ushort, ushort>.op_Addition((ushort)0x7FFF, (ushort)1));
            Assert.Equal((ushort)0x8001, AdditionOperatorsHelper<ushort, ushort, ushort>.op_Addition((ushort)0x8000, (ushort)1));
            Assert.Equal((ushort)0x0000, AdditionOperatorsHelper<ushort, ushort, ushort>.op_Addition((ushort)0xFFFF, (ushort)1));
        }

        [Fact]
        public static void op_CheckedAdditionTest()
        {
            Assert.Equal((ushort)0x0001, AdditionOperatorsHelper<ushort, ushort, ushort>.op_CheckedAddition((ushort)0x0000, (ushort)1));
            Assert.Equal((ushort)0x0002, AdditionOperatorsHelper<ushort, ushort, ushort>.op_CheckedAddition((ushort)0x0001, (ushort)1));
            Assert.Equal((ushort)0x8000, AdditionOperatorsHelper<ushort, ushort, ushort>.op_CheckedAddition((ushort)0x7FFF, (ushort)1));
            Assert.Equal((ushort)0x8001, AdditionOperatorsHelper<ushort, ushort, ushort>.op_CheckedAddition((ushort)0x8000, (ushort)1));

            Assert.Throws<OverflowException>(() => AdditionOperatorsHelper<ushort, ushort, ushort>.op_CheckedAddition((ushort)0xFFFF, (ushort)1));
        }

        //
        // IAdditiveIdentity
        //

        [Fact]
        public static void AdditiveIdentityTest()
        {
            Assert.Equal((ushort)0x0000, AdditiveIdentityHelper<ushort, ushort>.AdditiveIdentity);
        }

        //
        // IBinaryInteger
        //

        [Fact]
        public static void DivRemTest()
        {
            Assert.Equal(((ushort)0x0000, (ushort)0x0000), BinaryIntegerHelper<ushort>.DivRem((ushort)0x0000, (ushort)2));
            Assert.Equal(((ushort)0x0000, (ushort)0x0001), BinaryIntegerHelper<ushort>.DivRem((ushort)0x0001, (ushort)2));
            Assert.Equal(((ushort)0x3FFF, (ushort)0x0001), BinaryIntegerHelper<ushort>.DivRem((ushort)0x7FFF, (ushort)2));
            Assert.Equal(((ushort)0x4000, (ushort)0x0000), BinaryIntegerHelper<ushort>.DivRem((ushort)0x8000, (ushort)2));
            Assert.Equal(((ushort)0x7FFF, (ushort)0x0001), BinaryIntegerHelper<ushort>.DivRem((ushort)0xFFFF, (ushort)2));
        }

        [Fact]
        public static void LeadingZeroCountTest()
        {
            Assert.Equal((ushort)0x0010, BinaryIntegerHelper<ushort>.LeadingZeroCount((ushort)0x0000));
            Assert.Equal((ushort)0x000F, BinaryIntegerHelper<ushort>.LeadingZeroCount((ushort)0x0001));
            Assert.Equal((ushort)0x0001, BinaryIntegerHelper<ushort>.LeadingZeroCount((ushort)0x7FFF));
            Assert.Equal((ushort)0x0000, BinaryIntegerHelper<ushort>.LeadingZeroCount((ushort)0x8000));
            Assert.Equal((ushort)0x0000, BinaryIntegerHelper<ushort>.LeadingZeroCount((ushort)0xFFFF));
        }

        [Fact]
        public static void PopCountTest()
        {
            Assert.Equal((ushort)0x0000, BinaryIntegerHelper<ushort>.PopCount((ushort)0x0000));
            Assert.Equal((ushort)0x0001, BinaryIntegerHelper<ushort>.PopCount((ushort)0x0001));
            Assert.Equal((ushort)0x000F, BinaryIntegerHelper<ushort>.PopCount((ushort)0x7FFF));
            Assert.Equal((ushort)0x0001, BinaryIntegerHelper<ushort>.PopCount((ushort)0x8000));
            Assert.Equal((ushort)0x0010, BinaryIntegerHelper<ushort>.PopCount((ushort)0xFFFF));
        }

        [Fact]
        public static void RotateLeftTest()
        {
            Assert.Equal((ushort)0x0000, BinaryIntegerHelper<ushort>.RotateLeft((ushort)0x0000, 1));
            Assert.Equal((ushort)0x0002, BinaryIntegerHelper<ushort>.RotateLeft((ushort)0x0001, 1));
            Assert.Equal((ushort)0xFFFE, BinaryIntegerHelper<ushort>.RotateLeft((ushort)0x7FFF, 1));
            Assert.Equal((ushort)0x0001, BinaryIntegerHelper<ushort>.RotateLeft((ushort)0x8000, 1));
            Assert.Equal((ushort)0xFFFF, BinaryIntegerHelper<ushort>.RotateLeft((ushort)0xFFFF, 1));
        }

        [Fact]
        public static void RotateRightTest()
        {
            Assert.Equal((ushort)0x0000, BinaryIntegerHelper<ushort>.RotateRight((ushort)0x0000, 1));
            Assert.Equal((ushort)0x8000, BinaryIntegerHelper<ushort>.RotateRight((ushort)0x0001, 1));
            Assert.Equal((ushort)0xBFFF, BinaryIntegerHelper<ushort>.RotateRight((ushort)0x7FFF, 1));
            Assert.Equal((ushort)0x4000, BinaryIntegerHelper<ushort>.RotateRight((ushort)0x8000, 1));
            Assert.Equal((ushort)0xFFFF, BinaryIntegerHelper<ushort>.RotateRight((ushort)0xFFFF, 1));
        }

        [Fact]
        public static void TrailingZeroCountTest()
        {
            Assert.Equal((ushort)0x0010, BinaryIntegerHelper<ushort>.TrailingZeroCount((ushort)0x0000));
            Assert.Equal((ushort)0x0000, BinaryIntegerHelper<ushort>.TrailingZeroCount((ushort)0x0001));
            Assert.Equal((ushort)0x0000, BinaryIntegerHelper<ushort>.TrailingZeroCount((ushort)0x7FFF));
            Assert.Equal((ushort)0x000F, BinaryIntegerHelper<ushort>.TrailingZeroCount((ushort)0x8000));
            Assert.Equal((ushort)0x0000, BinaryIntegerHelper<ushort>.TrailingZeroCount((ushort)0xFFFF));
        }

        [Fact]
        public static void TryReadBigEndianByteTest()
        {
            ushort result;

            Assert.True(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0x00 }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.True(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0x01 }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0001, result);

            Assert.True(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0x7F }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x007F, result);

            Assert.True(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0x80 }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0080, result);

            Assert.True(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0xFF }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x00FF, result);
        }

        [Fact]
        public static void TryReadBigEndianInt16Test()
        {
            ushort result;

            Assert.True(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.True(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0001, result);

            Assert.True(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0080, result);

            Assert.True(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0x01, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0100, result);

            Assert.True(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0x7F, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x7FFF, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0x80, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0000, result);
        }

        [Fact]
        public static void TryReadBigEndianInt32Test()
        {
            ushort result;

            Assert.True(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.True(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0001, result);

            Assert.True(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0080, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0x01, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0x80, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0000, result);
        }

        [Fact]
        public static void TryReadBigEndianInt64Test()
        {
            ushort result;

            Assert.True(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.True(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0001, result);

            Assert.True(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0080, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0000, result);
        }

        [Fact]
        public static void TryReadBigEndianInt96Test()
        {
            ushort result;

            Assert.True(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.True(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0001, result);

            Assert.True(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0080, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0000, result);
        }

        [Fact]
        public static void TryReadBigEndianInt128Test()
        {
            ushort result;

            Assert.True(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.True(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0001, result);

            Assert.True(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0080, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0000, result);
        }

        [Fact]
        public static void TryReadBigEndianSByteTest()
        {
            ushort result;

            Assert.True(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0x00 }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.True(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0x01 }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0001, result);

            Assert.True(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0x7F }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x007F, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0x80 }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0xFF }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0000, result);
        }

        [Fact]
        public static void TryReadBigEndianUInt16Test()
        {
            ushort result;

            Assert.True(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.True(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0001, result);

            Assert.True(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0080, result);

            Assert.True(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0x01, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0100, result);

            Assert.True(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0x7F, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x7FFF, result);

            Assert.True(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0x80, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x8000, result);

            Assert.True(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal((ushort)0xFF7F, result);

            Assert.True(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((ushort)0xFFFF, result);
        }

        [Fact]
        public static void TryReadBigEndianUInt32Test()
        {
            ushort result;

            Assert.True(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.True(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0001, result);

            Assert.True(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0080, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0x01, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0x80, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0000, result);
        }

        [Fact]
        public static void TryReadBigEndianUInt64Test()
        {
            ushort result;

            Assert.True(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.True(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0001, result);

            Assert.True(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0080, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0000, result);
        }

        [Fact]
        public static void TryReadBigEndianUInt96Test()
        {
            ushort result;

            Assert.True(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.True(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0001, result);

            Assert.True(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0080, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0000, result);
        }

        [Fact]
        public static void TryReadBigEndianUInt128Test()
        {
            ushort result;

            Assert.True(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.True(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0001, result);

            Assert.True(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0080, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0000, result);
        }

        [Fact]
        public static void TryReadLittleEndianByteTest()
        {
            ushort result;

            Assert.True(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0x00 }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.True(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0x01 }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0001, result);

            Assert.True(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0x7F }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x007F, result);

            Assert.True(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0x80 }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0080, result);

            Assert.True(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0xFF }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x00FF, result);
        }

        [Fact]
        public static void TryReadLittleEndianInt16Test()
        {
            ushort result;

            Assert.True(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.True(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0100, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.True(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0x01, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0001, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.True(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0x80, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0080, result);

            Assert.True(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x7FFF, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0000, result);
        }

        [Fact]
        public static void TryReadLittleEndianInt32Test()
        {
            ushort result;

            Assert.True(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.True(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0x01, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0001, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.True(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0x80, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0080, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0000, result);
        }

        [Fact]
        public static void TryReadLittleEndianInt64Test()
        {
            ushort result;

            Assert.True(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.True(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0001, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.True(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0080, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0000, result);
        }

        [Fact]
        public static void TryReadLittleEndianInt96Test()
        {
            ushort result;

            Assert.True(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.True(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0001, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.True(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0080, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0000, result);
        }

        [Fact]
        public static void TryReadLittleEndianInt128Test()
        {
            ushort result;

            Assert.True(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.True(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0001, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.True(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0080, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0000, result);
        }

        [Fact]
        public static void TryReadLittleEndianSByteTest()
        {
            ushort result;

            Assert.True(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0x00 }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.True(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0x01 }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0001, result);

            Assert.True(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0x7F }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x007F, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0x80 }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0xFF }, isUnsigned: false, out result));
            Assert.Equal((ushort)0x0000, result);
        }

        [Fact]
        public static void TryReadLittleEndianUInt16Test()
        {
            ushort result;

            Assert.True(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.True(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0100, result);

            Assert.True(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x8000, result);

            Assert.True(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0x01, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0001, result);

            Assert.True(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((ushort)0xFF7F, result);

            Assert.True(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0x80, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0080, result);

            Assert.True(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x7FFF, result);

            Assert.True(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((ushort)0xFFFF, result);
        }

        [Fact]
        public static void TryReadLittleEndianUInt32Test()
        {
            ushort result;

            Assert.True(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.True(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0x01, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0001, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.True(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0x80, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0080, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0000, result);
        }

        [Fact]
        public static void TryReadLittleEndianUInt64Test()
        {
            ushort result;

            Assert.True(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.True(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0001, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.True(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0080, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0000, result);
        }

        [Fact]
        public static void TryReadLittleEndianUInt96Test()
        {
            ushort result;

            Assert.True(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.True(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0001, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.True(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0080, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0000, result);
        }

        [Fact]
        public static void TryReadLittleEndianUInt128Test()
        {
            ushort result;

            Assert.True(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.True(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0001, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.True(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0080, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(BinaryIntegerHelper<ushort>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((ushort)0x0000, result);
        }

        [Fact]
        public static void GetByteCountTest()
        {
            Assert.Equal(2, BinaryIntegerHelper<ushort>.GetByteCount((ushort)0x0000));
            Assert.Equal(2, BinaryIntegerHelper<ushort>.GetByteCount((ushort)0x0001));
            Assert.Equal(2, BinaryIntegerHelper<ushort>.GetByteCount((ushort)0x7FFF));
            Assert.Equal(2, BinaryIntegerHelper<ushort>.GetByteCount((ushort)0x8000));
            Assert.Equal(2, BinaryIntegerHelper<ushort>.GetByteCount((ushort)0xFFFF));
        }

        [Fact]
        public static void GetShortestBitLengthTest()
        {
            Assert.Equal(0x00, BinaryIntegerHelper<ushort>.GetShortestBitLength((ushort)0x0000));
            Assert.Equal(0x01, BinaryIntegerHelper<ushort>.GetShortestBitLength((ushort)0x0001));
            Assert.Equal(0x0F, BinaryIntegerHelper<ushort>.GetShortestBitLength((ushort)0x7FFF));
            Assert.Equal(0x10, BinaryIntegerHelper<ushort>.GetShortestBitLength((ushort)0x8000));
            Assert.Equal(0x10, BinaryIntegerHelper<ushort>.GetShortestBitLength((ushort)0xFFFF));
        }

        [Fact]
        public static void TryWriteBigEndianTest()
        {
            Span<byte> destination = stackalloc byte[2];
            int bytesWritten = 0;

            Assert.True(BinaryIntegerHelper<ushort>.TryWriteBigEndian((ushort)0x0000, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x00 }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<ushort>.TryWriteBigEndian((ushort)0x0001, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x01 }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<ushort>.TryWriteBigEndian((ushort)0x7FFF, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0x7F, 0xFF }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<ushort>.TryWriteBigEndian((ushort)0x8000, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0x80, 0x00 }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<ushort>.TryWriteBigEndian((ushort)0xFFFF, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0xFF }, destination.ToArray());

            Assert.False(BinaryIntegerHelper<ushort>.TryWriteBigEndian(default, Span<byte>.Empty, out bytesWritten));
            Assert.Equal(0, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0xFF }, destination.ToArray());
        }

        [Fact]
        public static void TryWriteLittleEndianTest()
        {
            Span<byte> destination = stackalloc byte[2];
            int bytesWritten = 0;

            Assert.True(BinaryIntegerHelper<ushort>.TryWriteLittleEndian((ushort)0x0000, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x00 }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<ushort>.TryWriteLittleEndian((ushort)0x0001, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0x01, 0x00 }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<ushort>.TryWriteLittleEndian((ushort)0x7FFF, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0x7F }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<ushort>.TryWriteLittleEndian((ushort)0x8000, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x80 }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<ushort>.TryWriteLittleEndian((ushort)0xFFFF, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0xFF }, destination.ToArray());

            Assert.False(BinaryIntegerHelper<ushort>.TryWriteLittleEndian(default, Span<byte>.Empty, out bytesWritten));
            Assert.Equal(0, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0xFF }, destination.ToArray());
        }

        //
        // IBinaryNumber
        //


        [Fact]
        public static void AllBitsSetTest()
        {
            Assert.Equal((ushort)0xFFFF, BinaryNumberHelper<ushort>.AllBitsSet);
            Assert.Equal((ushort)0, (ushort)~BinaryNumberHelper<ushort>.AllBitsSet);
        }

        [Fact]
        public static void IsPow2Test()
        {
            Assert.False(BinaryNumberHelper<ushort>.IsPow2((ushort)0x0000));
            Assert.True(BinaryNumberHelper<ushort>.IsPow2((ushort)0x0001));
            Assert.False(BinaryNumberHelper<ushort>.IsPow2((ushort)0x7FFF));
            Assert.True(BinaryNumberHelper<ushort>.IsPow2((ushort)0x8000));
            Assert.False(BinaryNumberHelper<ushort>.IsPow2((ushort)0xFFFF));
        }

        [Fact]
        public static void Log2Test()
        {
            Assert.Equal((ushort)0x0000, BinaryNumberHelper<ushort>.Log2((ushort)0x0000));
            Assert.Equal((ushort)0x0000, BinaryNumberHelper<ushort>.Log2((ushort)0x0001));
            Assert.Equal((ushort)0x000E, BinaryNumberHelper<ushort>.Log2((ushort)0x7FFF));
            Assert.Equal((ushort)0x000F, BinaryNumberHelper<ushort>.Log2((ushort)0x8000));
            Assert.Equal((ushort)0x000F, BinaryNumberHelper<ushort>.Log2((ushort)0xFFFF));
        }

        //
        // IBitwiseOperators
        //

        [Fact]
        public static void op_BitwiseAndTest()
        {
            Assert.Equal((ushort)0x0000, BitwiseOperatorsHelper<ushort, ushort, ushort>.op_BitwiseAnd((ushort)0x0000, (ushort)1));
            Assert.Equal((ushort)0x0001, BitwiseOperatorsHelper<ushort, ushort, ushort>.op_BitwiseAnd((ushort)0x0001, (ushort)1));
            Assert.Equal((ushort)0x0001, BitwiseOperatorsHelper<ushort, ushort, ushort>.op_BitwiseAnd((ushort)0x7FFF, (ushort)1));
            Assert.Equal((ushort)0x0000, BitwiseOperatorsHelper<ushort, ushort, ushort>.op_BitwiseAnd((ushort)0x8000, (ushort)1));
            Assert.Equal((ushort)0x0001, BitwiseOperatorsHelper<ushort, ushort, ushort>.op_BitwiseAnd((ushort)0xFFFF, (ushort)1));
        }

        [Fact]
        public static void op_BitwiseOrTest()
        {
            Assert.Equal((ushort)0x0001, BitwiseOperatorsHelper<ushort, ushort, ushort>.op_BitwiseOr((ushort)0x0000, (ushort)1));
            Assert.Equal((ushort)0x0001, BitwiseOperatorsHelper<ushort, ushort, ushort>.op_BitwiseOr((ushort)0x0001, (ushort)1));
            Assert.Equal((ushort)0x7FFF, BitwiseOperatorsHelper<ushort, ushort, ushort>.op_BitwiseOr((ushort)0x7FFF, (ushort)1));
            Assert.Equal((ushort)0x8001, BitwiseOperatorsHelper<ushort, ushort, ushort>.op_BitwiseOr((ushort)0x8000, (ushort)1));
            Assert.Equal((ushort)0xFFFF, BitwiseOperatorsHelper<ushort, ushort, ushort>.op_BitwiseOr((ushort)0xFFFF, (ushort)1));
        }

        [Fact]
        public static void op_ExclusiveOrTest()
        {
            Assert.Equal((ushort)0x0001, BitwiseOperatorsHelper<ushort, ushort, ushort>.op_ExclusiveOr((ushort)0x0000, (ushort)1));
            Assert.Equal((ushort)0x0000, BitwiseOperatorsHelper<ushort, ushort, ushort>.op_ExclusiveOr((ushort)0x0001, (ushort)1));
            Assert.Equal((ushort)0x7FFE, BitwiseOperatorsHelper<ushort, ushort, ushort>.op_ExclusiveOr((ushort)0x7FFF, (ushort)1));
            Assert.Equal((ushort)0x8001, BitwiseOperatorsHelper<ushort, ushort, ushort>.op_ExclusiveOr((ushort)0x8000, (ushort)1));
            Assert.Equal((ushort)0xFFFE, BitwiseOperatorsHelper<ushort, ushort, ushort>.op_ExclusiveOr((ushort)0xFFFF, (ushort)1));
        }

        [Fact]
        public static void op_OnesComplementTest()
        {
            Assert.Equal((ushort)0xFFFF, BitwiseOperatorsHelper<ushort, ushort, ushort>.op_OnesComplement((ushort)0x0000));
            Assert.Equal((ushort)0xFFFE, BitwiseOperatorsHelper<ushort, ushort, ushort>.op_OnesComplement((ushort)0x0001));
            Assert.Equal((ushort)0x8000, BitwiseOperatorsHelper<ushort, ushort, ushort>.op_OnesComplement((ushort)0x7FFF));
            Assert.Equal((ushort)0x7FFF, BitwiseOperatorsHelper<ushort, ushort, ushort>.op_OnesComplement((ushort)0x8000));
            Assert.Equal((ushort)0x0000, BitwiseOperatorsHelper<ushort, ushort, ushort>.op_OnesComplement((ushort)0xFFFF));
        }

        //
        // IComparisonOperators
        //

        [Fact]
        public static void op_GreaterThanTest()
        {
            Assert.False(ComparisonOperatorsHelper<ushort, ushort, bool>.op_GreaterThan((ushort)0x0000, (ushort)1));
            Assert.False(ComparisonOperatorsHelper<ushort, ushort, bool>.op_GreaterThan((ushort)0x0001, (ushort)1));
            Assert.True(ComparisonOperatorsHelper<ushort, ushort, bool>.op_GreaterThan((ushort)0x7FFF, (ushort)1));
            Assert.True(ComparisonOperatorsHelper<ushort, ushort, bool>.op_GreaterThan((ushort)0x8000, (ushort)1));
            Assert.True(ComparisonOperatorsHelper<ushort, ushort, bool>.op_GreaterThan((ushort)0xFFFF, (ushort)1));
        }

        [Fact]
        public static void op_GreaterThanOrEqualTest()
        {
            Assert.False(ComparisonOperatorsHelper<ushort, ushort, bool>.op_GreaterThanOrEqual((ushort)0x0000, (ushort)1));
            Assert.True(ComparisonOperatorsHelper<ushort, ushort, bool>.op_GreaterThanOrEqual((ushort)0x0001, (ushort)1));
            Assert.True(ComparisonOperatorsHelper<ushort, ushort, bool>.op_GreaterThanOrEqual((ushort)0x7FFF, (ushort)1));
            Assert.True(ComparisonOperatorsHelper<ushort, ushort, bool>.op_GreaterThanOrEqual((ushort)0x8000, (ushort)1));
            Assert.True(ComparisonOperatorsHelper<ushort, ushort, bool>.op_GreaterThanOrEqual((ushort)0xFFFF, (ushort)1));
        }

        [Fact]
        public static void op_LessThanTest()
        {
            Assert.True(ComparisonOperatorsHelper<ushort, ushort, bool>.op_LessThan((ushort)0x0000, (ushort)1));
            Assert.False(ComparisonOperatorsHelper<ushort, ushort, bool>.op_LessThan((ushort)0x0001, (ushort)1));
            Assert.False(ComparisonOperatorsHelper<ushort, ushort, bool>.op_LessThan((ushort)0x7FFF, (ushort)1));
            Assert.False(ComparisonOperatorsHelper<ushort, ushort, bool>.op_LessThan((ushort)0x8000, (ushort)1));
            Assert.False(ComparisonOperatorsHelper<ushort, ushort, bool>.op_LessThan((ushort)0xFFFF, (ushort)1));
        }

        [Fact]
        public static void op_LessThanOrEqualTest()
        {
            Assert.True(ComparisonOperatorsHelper<ushort, ushort, bool>.op_LessThanOrEqual((ushort)0x0000, (ushort)1));
            Assert.True(ComparisonOperatorsHelper<ushort, ushort, bool>.op_LessThanOrEqual((ushort)0x0001, (ushort)1));
            Assert.False(ComparisonOperatorsHelper<ushort, ushort, bool>.op_LessThanOrEqual((ushort)0x7FFF, (ushort)1));
            Assert.False(ComparisonOperatorsHelper<ushort, ushort, bool>.op_LessThanOrEqual((ushort)0x8000, (ushort)1));
            Assert.False(ComparisonOperatorsHelper<ushort, ushort, bool>.op_LessThanOrEqual((ushort)0xFFFF, (ushort)1));
        }

        //
        // IDecrementOperators
        //

        [Fact]
        public static void op_DecrementTest()
        {
            Assert.Equal((ushort)0xFFFF, DecrementOperatorsHelper<ushort>.op_Decrement((ushort)0x0000));
            Assert.Equal((ushort)0x0000, DecrementOperatorsHelper<ushort>.op_Decrement((ushort)0x0001));
            Assert.Equal((ushort)0x7FFE, DecrementOperatorsHelper<ushort>.op_Decrement((ushort)0x7FFF));
            Assert.Equal((ushort)0x7FFF, DecrementOperatorsHelper<ushort>.op_Decrement((ushort)0x8000));
            Assert.Equal((ushort)0xFFFE, DecrementOperatorsHelper<ushort>.op_Decrement((ushort)0xFFFF));
        }

        [Fact]
        public static void op_CheckedDecrementTest()
        {
            Assert.Equal((ushort)0x0000, DecrementOperatorsHelper<ushort>.op_CheckedDecrement((ushort)0x0001));
            Assert.Equal((ushort)0x7FFE, DecrementOperatorsHelper<ushort>.op_CheckedDecrement((ushort)0x7FFF));
            Assert.Equal((ushort)0x7FFF, DecrementOperatorsHelper<ushort>.op_CheckedDecrement((ushort)0x8000));
            Assert.Equal((ushort)0xFFFE, DecrementOperatorsHelper<ushort>.op_CheckedDecrement((ushort)0xFFFF));

            Assert.Throws<OverflowException>(() => DecrementOperatorsHelper<ushort>.op_CheckedDecrement((ushort)0x0000));
        }

        //
        // IDivisionOperators
        //

        [Fact]
        public static void op_DivisionTest()
        {
            Assert.Equal((ushort)0x0000, DivisionOperatorsHelper<ushort, ushort, ushort>.op_Division((ushort)0x0000, (ushort)2));
            Assert.Equal((ushort)0x0000, DivisionOperatorsHelper<ushort, ushort, ushort>.op_Division((ushort)0x0001, (ushort)2));
            Assert.Equal((ushort)0x3FFF, DivisionOperatorsHelper<ushort, ushort, ushort>.op_Division((ushort)0x7FFF, (ushort)2));
            Assert.Equal((ushort)0x4000, DivisionOperatorsHelper<ushort, ushort, ushort>.op_Division((ushort)0x8000, (ushort)2));
            Assert.Equal((ushort)0x7FFF, DivisionOperatorsHelper<ushort, ushort, ushort>.op_Division((ushort)0xFFFF, (ushort)2));

            Assert.Throws<DivideByZeroException>(() => DivisionOperatorsHelper<ushort, ushort, ushort>.op_Division((ushort)0x0001, (ushort)0));
        }

        [Fact]
        public static void op_CheckedDivisionTest()
        {
            Assert.Equal((ushort)0x0000, DivisionOperatorsHelper<ushort, ushort, ushort>.op_CheckedDivision((ushort)0x0000, (ushort)2));
            Assert.Equal((ushort)0x0000, DivisionOperatorsHelper<ushort, ushort, ushort>.op_CheckedDivision((ushort)0x0001, (ushort)2));
            Assert.Equal((ushort)0x3FFF, DivisionOperatorsHelper<ushort, ushort, ushort>.op_CheckedDivision((ushort)0x7FFF, (ushort)2));
            Assert.Equal((ushort)0x4000, DivisionOperatorsHelper<ushort, ushort, ushort>.op_CheckedDivision((ushort)0x8000, (ushort)2));
            Assert.Equal((ushort)0x7FFF, DivisionOperatorsHelper<ushort, ushort, ushort>.op_CheckedDivision((ushort)0xFFFF, (ushort)2));

            Assert.Throws<DivideByZeroException>(() => DivisionOperatorsHelper<ushort, ushort, ushort>.op_CheckedDivision((ushort)0x0001, (ushort)0));
        }

        //
        // IEqualityOperators
        //

        [Fact]
        public static void op_EqualityTest()
        {
            Assert.False(EqualityOperatorsHelper<ushort, ushort, bool>.op_Equality((ushort)0x0000, (ushort)1));
            Assert.True(EqualityOperatorsHelper<ushort, ushort, bool>.op_Equality((ushort)0x0001, (ushort)1));
            Assert.False(EqualityOperatorsHelper<ushort, ushort, bool>.op_Equality((ushort)0x7FFF, (ushort)1));
            Assert.False(EqualityOperatorsHelper<ushort, ushort, bool>.op_Equality((ushort)0x8000, (ushort)1));
            Assert.False(EqualityOperatorsHelper<ushort, ushort, bool>.op_Equality((ushort)0xFFFF, (ushort)1));
        }

        [Fact]
        public static void op_InequalityTest()
        {
            Assert.True(EqualityOperatorsHelper<ushort, ushort, bool>.op_Inequality((ushort)0x0000, (ushort)1));
            Assert.False(EqualityOperatorsHelper<ushort, ushort, bool>.op_Inequality((ushort)0x0001, (ushort)1));
            Assert.True(EqualityOperatorsHelper<ushort, ushort, bool>.op_Inequality((ushort)0x7FFF, (ushort)1));
            Assert.True(EqualityOperatorsHelper<ushort, ushort, bool>.op_Inequality((ushort)0x8000, (ushort)1));
            Assert.True(EqualityOperatorsHelper<ushort, ushort, bool>.op_Inequality((ushort)0xFFFF, (ushort)1));
        }

        //
        // IIncrementOperators
        //

        [Fact]
        public static void op_IncrementTest()
        {
            Assert.Equal((ushort)0x0001, IncrementOperatorsHelper<ushort>.op_Increment((ushort)0x0000));
            Assert.Equal((ushort)0x0002, IncrementOperatorsHelper<ushort>.op_Increment((ushort)0x0001));
            Assert.Equal((ushort)0x8000, IncrementOperatorsHelper<ushort>.op_Increment((ushort)0x7FFF));
            Assert.Equal((ushort)0x8001, IncrementOperatorsHelper<ushort>.op_Increment((ushort)0x8000));
            Assert.Equal((ushort)0x0000, IncrementOperatorsHelper<ushort>.op_Increment((ushort)0xFFFF));
        }

        [Fact]
        public static void op_CheckedIncrementTest()
        {
            Assert.Equal((ushort)0x0001, IncrementOperatorsHelper<ushort>.op_CheckedIncrement((ushort)0x0000));
            Assert.Equal((ushort)0x0002, IncrementOperatorsHelper<ushort>.op_CheckedIncrement((ushort)0x0001));
            Assert.Equal((ushort)0x8000, IncrementOperatorsHelper<ushort>.op_CheckedIncrement((ushort)0x7FFF));
            Assert.Equal((ushort)0x8001, IncrementOperatorsHelper<ushort>.op_CheckedIncrement((ushort)0x8000));

            Assert.Throws<OverflowException>(() => IncrementOperatorsHelper<ushort>.op_CheckedIncrement((ushort)0xFFFF));
        }

        //
        // IMinMaxValue
        //

        [Fact]
        public static void MaxValueTest()
        {
            Assert.Equal((ushort)0xFFFF, MinMaxValueHelper<ushort>.MaxValue);
        }

        [Fact]
        public static void MinValueTest()
        {
            Assert.Equal((ushort)0x0000, MinMaxValueHelper<ushort>.MinValue);
        }

        //
        // IModulusOperators
        //

        [Fact]
        public static void op_ModulusTest()
        {
            Assert.Equal((ushort)0x0000, ModulusOperatorsHelper<ushort, ushort, ushort>.op_Modulus((ushort)0x0000, (ushort)2));
            Assert.Equal((ushort)0x0001, ModulusOperatorsHelper<ushort, ushort, ushort>.op_Modulus((ushort)0x0001, (ushort)2));
            Assert.Equal((ushort)0x0001, ModulusOperatorsHelper<ushort, ushort, ushort>.op_Modulus((ushort)0x7FFF, (ushort)2));
            Assert.Equal((ushort)0x0000, ModulusOperatorsHelper<ushort, ushort, ushort>.op_Modulus((ushort)0x8000, (ushort)2));
            Assert.Equal((ushort)0x0001, ModulusOperatorsHelper<ushort, ushort, ushort>.op_Modulus((ushort)0xFFFF, (ushort)2));

            Assert.Throws<DivideByZeroException>(() => ModulusOperatorsHelper<ushort, ushort, ushort>.op_Modulus((ushort)0x0001, (ushort)0));
        }

        //
        // IMultiplicativeIdentity
        //

        [Fact]
        public static void MultiplicativeIdentityTest()
        {
            Assert.Equal((ushort)0x0001, MultiplicativeIdentityHelper<ushort, ushort>.MultiplicativeIdentity);
        }

        //
        // IMultiplyOperators
        //

        [Fact]
        public static void op_MultiplyTest()
        {
            Assert.Equal((ushort)0x0000, MultiplyOperatorsHelper<ushort, ushort, ushort>.op_Multiply((ushort)0x0000, (ushort)2));
            Assert.Equal((ushort)0x0002, MultiplyOperatorsHelper<ushort, ushort, ushort>.op_Multiply((ushort)0x0001, (ushort)2));
            Assert.Equal((ushort)0xFFFE, MultiplyOperatorsHelper<ushort, ushort, ushort>.op_Multiply((ushort)0x7FFF, (ushort)2));
            Assert.Equal((ushort)0x0000, MultiplyOperatorsHelper<ushort, ushort, ushort>.op_Multiply((ushort)0x8000, (ushort)2));
            Assert.Equal((ushort)0xFFFE, MultiplyOperatorsHelper<ushort, ushort, ushort>.op_Multiply((ushort)0xFFFF, (ushort)2));
        }

        [Fact]
        public static void op_CheckedMultiplyTest()
        {
            Assert.Equal((ushort)0x0000, MultiplyOperatorsHelper<ushort, ushort, ushort>.op_CheckedMultiply((ushort)0x0000, (ushort)2));
            Assert.Equal((ushort)0x0002, MultiplyOperatorsHelper<ushort, ushort, ushort>.op_CheckedMultiply((ushort)0x0001, (ushort)2));
            Assert.Equal((ushort)0xFFFE, MultiplyOperatorsHelper<ushort, ushort, ushort>.op_CheckedMultiply((ushort)0x7FFF, (ushort)2));

            Assert.Throws<OverflowException>(() => MultiplyOperatorsHelper<ushort, ushort, ushort>.op_CheckedMultiply((ushort)0x8000, (ushort)2));
            Assert.Throws<OverflowException>(() => MultiplyOperatorsHelper<ushort, ushort, ushort>.op_CheckedMultiply((ushort)0xFFFF, (ushort)2));
        }

        //
        // INumber
        //

        [Fact]
        public static void ClampTest()
        {
            Assert.Equal((ushort)0x0001, NumberHelper<ushort>.Clamp((ushort)0x0000, (ushort)0x0001, (ushort)0x003F));
            Assert.Equal((ushort)0x0001, NumberHelper<ushort>.Clamp((ushort)0x0001, (ushort)0x0001, (ushort)0x003F));
            Assert.Equal((ushort)0x003F, NumberHelper<ushort>.Clamp((ushort)0x7FFF, (ushort)0x0001, (ushort)0x003F));
            Assert.Equal((ushort)0x003F, NumberHelper<ushort>.Clamp((ushort)0x8000, (ushort)0x0001, (ushort)0x003F));
            Assert.Equal((ushort)0x003F, NumberHelper<ushort>.Clamp((ushort)0xFFFF, (ushort)0x0001, (ushort)0x003F));
        }

        [Fact]
        public static void MaxTest()
        {
            Assert.Equal((ushort)0x0001, NumberHelper<ushort>.Max((ushort)0x0000, (ushort)1));
            Assert.Equal((ushort)0x0001, NumberHelper<ushort>.Max((ushort)0x0001, (ushort)1));
            Assert.Equal((ushort)0x7FFF, NumberHelper<ushort>.Max((ushort)0x7FFF, (ushort)1));
            Assert.Equal((ushort)0x8000, NumberHelper<ushort>.Max((ushort)0x8000, (ushort)1));
            Assert.Equal((ushort)0xFFFF, NumberHelper<ushort>.Max((ushort)0xFFFF, (ushort)1));
        }

        [Fact]
        public static void MaxNumberTest()
        {
            Assert.Equal((ushort)0x0001, NumberHelper<ushort>.MaxNumber((ushort)0x0000, (ushort)1));
            Assert.Equal((ushort)0x0001, NumberHelper<ushort>.MaxNumber((ushort)0x0001, (ushort)1));
            Assert.Equal((ushort)0x7FFF, NumberHelper<ushort>.MaxNumber((ushort)0x7FFF, (ushort)1));
            Assert.Equal((ushort)0x8000, NumberHelper<ushort>.MaxNumber((ushort)0x8000, (ushort)1));
            Assert.Equal((ushort)0xFFFF, NumberHelper<ushort>.MaxNumber((ushort)0xFFFF, (ushort)1));
        }

        [Fact]
        public static void MinTest()
        {
            Assert.Equal((ushort)0x0000, NumberHelper<ushort>.Min((ushort)0x0000, (ushort)1));
            Assert.Equal((ushort)0x0001, NumberHelper<ushort>.Min((ushort)0x0001, (ushort)1));
            Assert.Equal((ushort)0x0001, NumberHelper<ushort>.Min((ushort)0x7FFF, (ushort)1));
            Assert.Equal((ushort)0x0001, NumberHelper<ushort>.Min((ushort)0x8000, (ushort)1));
            Assert.Equal((ushort)0x0001, NumberHelper<ushort>.Min((ushort)0xFFFF, (ushort)1));
        }

        [Fact]
        public static void MinNumberTest()
        {
            Assert.Equal((ushort)0x0000, NumberHelper<ushort>.MinNumber((ushort)0x0000, (ushort)1));
            Assert.Equal((ushort)0x0001, NumberHelper<ushort>.MinNumber((ushort)0x0001, (ushort)1));
            Assert.Equal((ushort)0x0001, NumberHelper<ushort>.MinNumber((ushort)0x7FFF, (ushort)1));
            Assert.Equal((ushort)0x0001, NumberHelper<ushort>.MinNumber((ushort)0x8000, (ushort)1));
            Assert.Equal((ushort)0x0001, NumberHelper<ushort>.MinNumber((ushort)0xFFFF, (ushort)1));
        }

        [Fact]
        public static void SignTest()
        {
            Assert.Equal(0, NumberHelper<ushort>.Sign((ushort)0x0000));
            Assert.Equal(1, NumberHelper<ushort>.Sign((ushort)0x0001));
            Assert.Equal(1, NumberHelper<ushort>.Sign((ushort)0x7FFF));
            Assert.Equal(1, NumberHelper<ushort>.Sign((ushort)0x8000));
            Assert.Equal(1, NumberHelper<ushort>.Sign((ushort)0xFFFF));
        }

        //
        // INumberBase
        //

        [Fact]
        public static void OneTest()
        {
            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.One);
        }

        [Fact]
        public static void RadixTest()
        {
            Assert.Equal(2, NumberBaseHelper<ushort>.Radix);
        }

        [Fact]
        public static void ZeroTest()
        {
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.Zero);
        }

        [Fact]
        public static void AbsTest()
        {
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.Abs((ushort)0x0000));
            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.Abs((ushort)0x0001));
            Assert.Equal((ushort)0x7FFF, NumberBaseHelper<ushort>.Abs((ushort)0x7FFF));
            Assert.Equal((ushort)0x8000, NumberBaseHelper<ushort>.Abs((ushort)0x8000));
            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.Abs((ushort)0xFFFF));
        }

        [Fact]
        public static void CreateCheckedFromByteTest()
        {
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateChecked<byte>(0x00));
            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.CreateChecked<byte>(0x01));
            Assert.Equal((ushort)0x007F, NumberBaseHelper<ushort>.CreateChecked<byte>(0x7F));
            Assert.Equal((ushort)0x0080, NumberBaseHelper<ushort>.CreateChecked<byte>(0x80));
            Assert.Equal((ushort)0x00FF, NumberBaseHelper<ushort>.CreateChecked<byte>(0xFF));
        }

        [Fact]
        public static void CreateCheckedFromCharTest()
        {
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateChecked<char>((char)0x0000));
            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.CreateChecked<char>((char)0x0001));
            Assert.Equal((ushort)0x7FFF, NumberBaseHelper<ushort>.CreateChecked<char>((char)0x7FFF));
            Assert.Equal((ushort)0x8000, NumberBaseHelper<ushort>.CreateChecked<char>((char)0x8000));
            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateChecked<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateCheckedFromDecimalTest()
        {
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateChecked<decimal>(-0.0m));
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateChecked<decimal>(+0.0m));
            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.CreateChecked<decimal>(+1.0m));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<ushort>.CreateChecked<decimal>(decimal.MinValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<ushort>.CreateChecked<decimal>(decimal.MaxValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<ushort>.CreateChecked<decimal>(decimal.MinusOne));
        }

        [Fact]
        public static void CreateCheckedFromDoubleTest()
        {
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateChecked<double>(+0.0));
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateChecked<double>(-0.0));


            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateChecked<double>(-double.Epsilon));
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateChecked<double>(+double.Epsilon));

            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.CreateChecked<double>(+1.0));
            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateChecked<double>(+65535.0));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<ushort>.CreateChecked<double>(-1.0));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<ushort>.CreateChecked<double>(+65536.0));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<ushort>.CreateChecked<double>(double.PositiveInfinity));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<ushort>.CreateChecked<double>(double.NegativeInfinity));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<ushort>.CreateChecked<double>(double.MaxValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<ushort>.CreateChecked<double>(double.MinValue));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<ushort>.CreateChecked<double>(double.NaN));
        }

        [Fact]
        public static void CreateCheckedFromHalfTest()
        {
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateChecked<Half>(Half.Zero));
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateChecked<Half>(Half.NegativeZero));

            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateChecked<Half>(-Half.Epsilon));
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateChecked<Half>(+Half.Epsilon));

            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.CreateChecked<Half>(Half.One));
            Assert.Equal((ushort)0xFFE0, NumberBaseHelper<ushort>.CreateChecked<Half>(Half.MaxValue));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<ushort>.CreateChecked<Half>(Half.NegativeOne));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<ushort>.CreateChecked<Half>(Half.PositiveInfinity));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<ushort>.CreateChecked<Half>(Half.NegativeInfinity));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<ushort>.CreateChecked<Half>(Half.MinValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<ushort>.CreateChecked<Half>(Half.NaN));
        }

        [Fact]
        public static void CreateCheckedFromInt16Test()
        {
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateChecked<short>(0x0000));
            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.CreateChecked<short>(0x0001));
            Assert.Equal((ushort)0x7FFF, NumberBaseHelper<ushort>.CreateChecked<short>(0x7FFF));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<ushort>.CreateChecked<short>(unchecked((short)0x8000)));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<ushort>.CreateChecked<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromInt32Test()
        {
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateChecked<int>(0x00000000));
            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.CreateChecked<int>(0x00000001));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<ushort>.CreateChecked<int>(0x7FFFFFFF));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<ushort>.CreateChecked<int>(unchecked((int)0x80000000)));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<ushort>.CreateChecked<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromInt64Test()
        {
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateChecked<long>(0x0000000000000000));
            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.CreateChecked<long>(0x0000000000000001));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<ushort>.CreateChecked<long>(0x7FFFFFFFFFFFFFFF));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<ushort>.CreateChecked<long>(unchecked((long)0x8000000000000000)));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<ushort>.CreateChecked<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromInt128Test()
        {
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateChecked<Int128>(Int128.Zero));
            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.CreateChecked<Int128>(Int128.One));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<ushort>.CreateChecked<Int128>(Int128.MaxValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<ushort>.CreateChecked<Int128>(Int128.MinValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<ushort>.CreateChecked<Int128>(Int128.NegativeOne));
        }

        [Fact]
        public static void CreateCheckedFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateChecked<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.CreateChecked<nint>(unchecked((nint)0x0000000000000001)));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<ushort>.CreateChecked<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<ushort>.CreateChecked<nint>(unchecked((nint)0x8000000000000000)));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<ushort>.CreateChecked<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateChecked<nint>((nint)0x00000000));
                Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.CreateChecked<nint>((nint)0x00000001));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<ushort>.CreateChecked<nint>((nint)0x7FFFFFFF));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<ushort>.CreateChecked<nint>(unchecked((nint)0x80000000)));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<ushort>.CreateChecked<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateCheckedFromNFloatTest()
        {
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateChecked<NFloat>(0.0f));
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateChecked<NFloat>(NFloat.NegativeZero));

            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateChecked<NFloat>(-NFloat.Epsilon));
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateChecked<NFloat>(+NFloat.Epsilon));

            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.CreateChecked<NFloat>(1.0f));
            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateChecked<NFloat>(65535.0f));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<ushort>.CreateChecked<NFloat>(-1.0f));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<ushort>.CreateChecked<NFloat>(+65536.0f));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<ushort>.CreateChecked<NFloat>(NFloat.PositiveInfinity));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<ushort>.CreateChecked<NFloat>(NFloat.NegativeInfinity));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<ushort>.CreateChecked<NFloat>(NFloat.MaxValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<ushort>.CreateChecked<NFloat>(NFloat.MinValue));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<ushort>.CreateChecked<NFloat>(NFloat.NaN));
        }

        [Fact]
        public static void CreateCheckedFromSByteTest()
        {
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateChecked<sbyte>(0x00));
            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.CreateChecked<sbyte>(0x01));
            Assert.Equal((ushort)0x007F, NumberBaseHelper<ushort>.CreateChecked<sbyte>(0x7F));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<ushort>.CreateChecked<sbyte>(unchecked((sbyte)0x80)));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<ushort>.CreateChecked<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateCheckedFromSingleTest()
        {
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateChecked<float>(+0.0f));
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateChecked<float>(-0.0f));

            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateChecked<float>(-float.Epsilon));
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateChecked<float>(-float.Epsilon));

            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.CreateChecked<float>(+1.0f));
            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateChecked<float>(+65535.0f));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<ushort>.CreateChecked<float>(-1.0f));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<ushort>.CreateChecked<float>(+65536.0f));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<ushort>.CreateChecked<float>(float.PositiveInfinity));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<ushort>.CreateChecked<float>(float.NegativeInfinity));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<ushort>.CreateChecked<float>(float.MaxValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<ushort>.CreateChecked<float>(float.MinValue));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<ushort>.CreateChecked<float>(float.NaN));
        }

        [Fact]
        public static void CreateCheckedFromUInt16Test()
        {
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateChecked<ushort>(0x0000));
            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.CreateChecked<ushort>(0x0001));
            Assert.Equal((ushort)0x7FFF, NumberBaseHelper<ushort>.CreateChecked<ushort>(0x7FFF));
            Assert.Equal((ushort)0x8000, NumberBaseHelper<ushort>.CreateChecked<ushort>(0x8000));
            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateChecked<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUInt32Test()
        {
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateChecked<uint>(0x00000000));
            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.CreateChecked<uint>(0x00000001));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<ushort>.CreateChecked<uint>(0x7FFFFFFF));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<ushort>.CreateChecked<uint>(0x80000000));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<ushort>.CreateChecked<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUInt64Test()
        {
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateChecked<ulong>(0x0000000000000000));
            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.CreateChecked<ulong>(0x0000000000000001));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<ushort>.CreateChecked<ulong>(0x7FFFFFFFFFFFFFFF));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<ushort>.CreateChecked<ulong>(0x8000000000000000));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<ushort>.CreateChecked<ulong>(0xFFFFFFFFFFFFFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUInt128Test()
        {
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateChecked<UInt128>(UInt128.Zero));
            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.CreateChecked<UInt128>(UInt128.One));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<ushort>.CreateChecked<UInt128>(UInt128Tests_GenericMath.Int128MaxValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<ushort>.CreateChecked<UInt128>(UInt128Tests_GenericMath.Int128MaxValuePlusOne));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<ushort>.CreateChecked<UInt128>(UInt128.MaxValue));
        }

        [Fact]
        public static void CreateCheckedFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateChecked<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.CreateChecked<nuint>(unchecked((nuint)0x0000000000000001)));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<ushort>.CreateChecked<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<ushort>.CreateChecked<nuint>(unchecked((nuint)0x8000000000000000)));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<ushort>.CreateChecked<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateChecked<nuint>((nuint)0x00000000));
                Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.CreateChecked<nuint>((nuint)0x00000001));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<ushort>.CreateChecked<nuint>((nuint)0x7FFFFFFF));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<ushort>.CreateChecked<nuint>((nuint)0x80000000));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<ushort>.CreateChecked<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateSaturatingFromByteTest()
        {
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateSaturating<byte>(0x00));
            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.CreateSaturating<byte>(0x01));
            Assert.Equal((ushort)0x007F, NumberBaseHelper<ushort>.CreateSaturating<byte>(0x7F));
            Assert.Equal((ushort)0x0080, NumberBaseHelper<ushort>.CreateSaturating<byte>(0x80));
            Assert.Equal((ushort)0x00FF, NumberBaseHelper<ushort>.CreateSaturating<byte>(0xFF));
        }

        [Fact]
        public static void CreateSaturatingFromCharTest()
        {
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateSaturating<char>((char)0x0000));
            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.CreateSaturating<char>((char)0x0001));
            Assert.Equal((ushort)0x7FFF, NumberBaseHelper<ushort>.CreateSaturating<char>((char)0x7FFF));
            Assert.Equal((ushort)0x8000, NumberBaseHelper<ushort>.CreateSaturating<char>((char)0x8000));
            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateSaturating<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromDecimalTest()
        {
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateSaturating<decimal>(-0.0m));
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateSaturating<decimal>(+0.0m));
            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.CreateSaturating<decimal>(+1.0m));

            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateSaturating<decimal>(decimal.MinValue));
            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateSaturating<decimal>(decimal.MaxValue));
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateSaturating<decimal>(decimal.MinusOne));
        }

        [Fact]
        public static void CreateSaturatingFromDoubleTest()
        {
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateSaturating<double>(+0.0));
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateSaturating<double>(-0.0));


            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateSaturating<double>(-double.Epsilon));
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateSaturating<double>(+double.Epsilon));

            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.CreateSaturating<double>(+1.0));
            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateSaturating<double>(+65535.0));

            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateSaturating<double>(-1.0));
            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateSaturating<double>(+65536.0));

            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateSaturating<double>(double.PositiveInfinity));
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateSaturating<double>(double.NegativeInfinity));

            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateSaturating<double>(double.MaxValue));
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateSaturating<double>(double.MinValue));

            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateSaturating<double>(double.NaN));
        }

        [Fact]
        public static void CreateSaturatingFromHalfTest()
        {
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateSaturating<Half>(Half.Zero));
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateSaturating<Half>(Half.NegativeZero));

            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateSaturating<Half>(-Half.Epsilon));
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateSaturating<Half>(+Half.Epsilon));

            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.CreateSaturating<Half>(Half.One));
            Assert.Equal((ushort)0xFFE0, NumberBaseHelper<ushort>.CreateSaturating<Half>(Half.MaxValue));

            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateSaturating<Half>(Half.NegativeOne));

            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateSaturating<Half>(Half.PositiveInfinity));
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateSaturating<Half>(Half.NegativeInfinity));

            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateSaturating<Half>(Half.MinValue));
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateSaturating<Half>(Half.NaN));
        }

        [Fact]
        public static void CreateSaturatingFromInt16Test()
        {
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateSaturating<short>(0x0000));
            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.CreateSaturating<short>(0x0001));
            Assert.Equal((ushort)0x7FFF, NumberBaseHelper<ushort>.CreateSaturating<short>(0x7FFF));
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateSaturating<short>(unchecked((short)0x8000)));
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateSaturating<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromInt32Test()
        {
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateSaturating<int>(0x00000000));
            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.CreateSaturating<int>(0x00000001));
            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateSaturating<int>(0x7FFFFFFF));
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateSaturating<int>(unchecked((int)0x80000000)));
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateSaturating<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromInt64Test()
        {
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateSaturating<long>(0x0000000000000000));
            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.CreateSaturating<long>(0x0000000000000001));
            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateSaturating<long>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateSaturating<long>(unchecked((long)0x8000000000000000)));
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateSaturating<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromInt128Test()
        {
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateSaturating<Int128>(Int128.Zero));
            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.CreateSaturating<Int128>(Int128.One));
            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateSaturating<Int128>(Int128.MaxValue));
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateSaturating<Int128>(Int128.MinValue));
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateSaturating<Int128>(Int128.NegativeOne));
        } 

        [Fact]
        public static void CreateSaturatingFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateSaturating<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.CreateSaturating<nint>(unchecked((nint)0x0000000000000001)));
                Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateSaturating<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateSaturating<nint>(unchecked((nint)0x8000000000000000)));
                Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateSaturating<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateSaturating<nint>((nint)0x00000000));
                Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.CreateSaturating<nint>((nint)0x00000001));
                Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateSaturating<nint>((nint)0x7FFFFFFF));
                Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateSaturating<nint>(unchecked((nint)0x80000000)));
                Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateSaturating<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateSaturatingFromNFloatTest()
        {
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateSaturating<NFloat>(0.0f));
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateSaturating<NFloat>(NFloat.NegativeZero));

            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateSaturating<NFloat>(-NFloat.Epsilon));
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateSaturating<NFloat>(+NFloat.Epsilon));

            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.CreateSaturating<NFloat>(1.0f));
            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateSaturating<NFloat>(65535.0f));

            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateSaturating<NFloat>(-1.0f));
            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateSaturating<NFloat>(+65536.0f));

            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateSaturating<NFloat>(NFloat.PositiveInfinity));
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateSaturating<NFloat>(NFloat.NegativeInfinity));

            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateSaturating<NFloat>(NFloat.MaxValue));
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateSaturating<NFloat>(NFloat.MinValue));

            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateSaturating<NFloat>(NFloat.NaN));
        }

        [Fact]
        public static void CreateSaturatingFromSByteTest()
        {
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateSaturating<sbyte>(0x00));
            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.CreateSaturating<sbyte>(0x01));
            Assert.Equal((ushort)0x007F, NumberBaseHelper<ushort>.CreateSaturating<sbyte>(0x7F));
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateSaturating<sbyte>(unchecked((sbyte)0x80)));
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateSaturating<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateSaturatingFromSingleTest()
        {
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateSaturating<float>(+0.0f));
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateSaturating<float>(-0.0f));

            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateSaturating<float>(-float.Epsilon));
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateSaturating<float>(-float.Epsilon));

            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.CreateSaturating<float>(+1.0f));
            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateSaturating<float>(+65535.0f));

            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateSaturating<float>(-1.0f));
            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateSaturating<float>(+65536.0f));

            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateSaturating<float>(float.PositiveInfinity));
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateSaturating<float>(float.NegativeInfinity));

            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateSaturating<float>(float.MaxValue));
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateSaturating<float>(float.MinValue));

            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateSaturating<float>(float.NaN));
        }

        [Fact]
        public static void CreateSaturatingFromUInt16Test()
        {
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateSaturating<ushort>(0x0000));
            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.CreateSaturating<ushort>(0x0001));
            Assert.Equal((ushort)0x7FFF, NumberBaseHelper<ushort>.CreateSaturating<ushort>(0x7FFF));
            Assert.Equal((ushort)0x8000, NumberBaseHelper<ushort>.CreateSaturating<ushort>(0x8000));
            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateSaturating<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUInt32Test()
        {
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateSaturating<uint>(0x00000000));
            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.CreateSaturating<uint>(0x00000001));
            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateSaturating<uint>(0x7FFFFFFF));
            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateSaturating<uint>(0x80000000));
            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateSaturating<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUInt64Test()
        {
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateSaturating<ulong>(0x0000000000000000));
            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.CreateSaturating<ulong>(0x0000000000000001));
            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateSaturating<ulong>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateSaturating<ulong>(0x8000000000000000));
            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateSaturating<ulong>(0xFFFFFFFFFFFFFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUInt128Test()
        {
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateSaturating<UInt128>(UInt128.Zero));
            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.CreateSaturating<UInt128>(UInt128.One));
            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateSaturating<UInt128>(UInt128Tests_GenericMath.Int128MaxValue));
            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateSaturating<UInt128>(UInt128Tests_GenericMath.Int128MaxValuePlusOne));
            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateSaturating<UInt128>(UInt128.MaxValue));
        }

        [Fact]
        public static void CreateSaturatingFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateSaturating<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.CreateSaturating<nuint>(unchecked((nuint)0x0000000000000001)));
                Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateSaturating<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateSaturating<nuint>(unchecked((nuint)0x8000000000000000)));
                Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateSaturating<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateSaturating<nuint>((nuint)0x00000000));
                Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.CreateSaturating<nuint>((nuint)0x00000001));
                Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateSaturating<nuint>((nuint)0x7FFFFFFF));
                Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateSaturating<nuint>((nuint)0x80000000));
                Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateSaturating<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateTruncatingFromByteTest()
        {
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateTruncating<byte>(0x00));
            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.CreateTruncating<byte>(0x01));
            Assert.Equal((ushort)0x007F, NumberBaseHelper<ushort>.CreateTruncating<byte>(0x7F));
            Assert.Equal((ushort)0x0080, NumberBaseHelper<ushort>.CreateTruncating<byte>(0x80));
            Assert.Equal((ushort)0x00FF, NumberBaseHelper<ushort>.CreateTruncating<byte>(0xFF));
        }

        [Fact]
        public static void CreateTruncatingFromCharTest()
        {
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateTruncating<char>((char)0x0000));
            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.CreateTruncating<char>((char)0x0001));
            Assert.Equal((ushort)0x7FFF, NumberBaseHelper<ushort>.CreateTruncating<char>((char)0x7FFF));
            Assert.Equal((ushort)0x8000, NumberBaseHelper<ushort>.CreateTruncating<char>((char)0x8000));
            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateTruncating<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromDecimalTest()
        {
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateTruncating<decimal>(-0.0m));
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateTruncating<decimal>(+0.0m));
            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.CreateTruncating<decimal>(+1.0m));

            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateTruncating<decimal>(decimal.MinValue));
            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateTruncating<decimal>(decimal.MaxValue));
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateTruncating<decimal>(decimal.MinusOne));
        }

        [Fact]
        public static void CreateTruncatingFromDoubleTest()
        {
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateTruncating<double>(+0.0));
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateTruncating<double>(-0.0));


            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateTruncating<double>(-double.Epsilon));
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateTruncating<double>(+double.Epsilon));

            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.CreateTruncating<double>(+1.0));
            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateTruncating<double>(+65535.0));

            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateTruncating<double>(-1.0));
            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateTruncating<double>(+65536.0));

            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateTruncating<double>(double.PositiveInfinity));
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateTruncating<double>(double.NegativeInfinity));

            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateTruncating<double>(double.MaxValue));
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateTruncating<double>(double.MinValue));

            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateTruncating<double>(double.NaN));
        }

        [Fact]
        public static void CreateTruncatingFromHalfTest()
        {
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateTruncating<Half>(Half.Zero));
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateTruncating<Half>(Half.NegativeZero));

            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateTruncating<Half>(-Half.Epsilon));
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateTruncating<Half>(+Half.Epsilon));

            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.CreateTruncating<Half>(Half.One));
            Assert.Equal((ushort)0xFFE0, NumberBaseHelper<ushort>.CreateTruncating<Half>(Half.MaxValue));

            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateTruncating<Half>(Half.NegativeOne));

            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateTruncating<Half>(Half.PositiveInfinity));
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateTruncating<Half>(Half.NegativeInfinity));

            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateTruncating<Half>(Half.MinValue));
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateTruncating<Half>(Half.NaN));
        }

        [Fact]
        public static void CreateTruncatingFromInt16Test()
        {
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateTruncating<short>(0x0000));
            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.CreateTruncating<short>(0x0001));
            Assert.Equal((ushort)0x7FFF, NumberBaseHelper<ushort>.CreateTruncating<short>(0x7FFF));
            Assert.Equal((ushort)0x8000, NumberBaseHelper<ushort>.CreateTruncating<short>(unchecked((short)0x8000)));
            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateTruncating<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateTruncatingFromInt32Test()
        {
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateTruncating<int>(0x00000000));
            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.CreateTruncating<int>(0x00000001));
            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateTruncating<int>(0x7FFFFFFF));
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateTruncating<int>(unchecked((int)0x80000000)));
            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateTruncating<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateTruncatingFromInt64Test()
        {
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateTruncating<long>(0x0000000000000000));
            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.CreateTruncating<long>(0x0000000000000001));
            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateTruncating<long>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateTruncating<long>(unchecked((long)0x8000000000000000)));
            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateTruncating<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void CreateTruncatingFromInt128Test()
        {
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateTruncating<Int128>(Int128.Zero));
            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.CreateTruncating<Int128>(Int128.One));
            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateTruncating<Int128>(Int128.MaxValue));
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateTruncating<Int128>(Int128.MinValue));
            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateTruncating<Int128>(Int128.NegativeOne));
        }

        [Fact]
        public static void CreateTruncatingFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateTruncating<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.CreateTruncating<nint>(unchecked((nint)0x0000000000000001)));
                Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateTruncating<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateTruncating<nint>(unchecked((nint)0x8000000000000000)));
                Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateTruncating<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateTruncating<nint>((nint)0x00000000));
                Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.CreateTruncating<nint>((nint)0x00000001));
                Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateTruncating<nint>((nint)0x7FFFFFFF));
                Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateTruncating<nint>(unchecked((nint)0x80000000)));
                Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateTruncating<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateTruncatingFromNFloatTest()
        {
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateTruncating<NFloat>(0.0f));
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateTruncating<NFloat>(NFloat.NegativeZero));

            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateTruncating<NFloat>(-NFloat.Epsilon));
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateTruncating<NFloat>(+NFloat.Epsilon));

            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.CreateTruncating<NFloat>(1.0f));
            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateTruncating<NFloat>(65535.0f));

            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateTruncating<NFloat>(-1.0f));
            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateTruncating<NFloat>(+65536.0f));

            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateTruncating<NFloat>(NFloat.PositiveInfinity));
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateTruncating<NFloat>(NFloat.NegativeInfinity));

            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateTruncating<NFloat>(NFloat.MaxValue));
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateTruncating<NFloat>(NFloat.MinValue));

            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateTruncating<NFloat>(NFloat.NaN));
        }

        [Fact]
        public static void CreateTruncatingFromSByteTest()
        {
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateTruncating<sbyte>(0x00));
            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.CreateTruncating<sbyte>(0x01));
            Assert.Equal((ushort)0x007F, NumberBaseHelper<ushort>.CreateTruncating<sbyte>(0x7F));
            Assert.Equal((ushort)0xFF80, NumberBaseHelper<ushort>.CreateTruncating<sbyte>(unchecked((sbyte)0x80)));
            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateTruncating<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateTruncatingFromSingleTest()
        {
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateTruncating<float>(+0.0f));
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateTruncating<float>(-0.0f));

            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateTruncating<float>(-float.Epsilon));
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateTruncating<float>(-float.Epsilon));

            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.CreateTruncating<float>(+1.0f));
            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateTruncating<float>(+65535.0f));

            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateTruncating<float>(-1.0f));
            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateTruncating<float>(+65536.0f));

            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateTruncating<float>(float.PositiveInfinity));
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateTruncating<float>(float.NegativeInfinity));

            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateTruncating<float>(float.MaxValue));
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateTruncating<float>(float.MinValue));

            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateTruncating<float>(float.NaN));
        }

        [Fact]
        public static void CreateTruncatingFromUInt16Test()
        {
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateTruncating<ushort>(0x0000));
            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.CreateTruncating<ushort>(0x0001));
            Assert.Equal((ushort)0x7FFF, NumberBaseHelper<ushort>.CreateTruncating<ushort>(0x7FFF));
            Assert.Equal((ushort)0x8000, NumberBaseHelper<ushort>.CreateTruncating<ushort>(0x8000));
            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateTruncating<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt32Test()
        {
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateTruncating<uint>(0x00000000));
            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.CreateTruncating<uint>(0x00000001));
            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateTruncating<uint>(0x7FFFFFFF));
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateTruncating<uint>(0x80000000));
            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateTruncating<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt64Test()
        {
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateTruncating<ulong>(0x0000000000000000));
            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.CreateTruncating<ulong>(0x0000000000000001));
            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateTruncating<ulong>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateTruncating<ulong>(0x8000000000000000));
            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateTruncating<ulong>(0xFFFFFFFFFFFFFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt128Test()
        {
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateTruncating<UInt128>(UInt128.Zero));
            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.CreateTruncating<UInt128>(UInt128.One));
            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateTruncating<UInt128>(UInt128Tests_GenericMath.Int128MaxValue));
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateTruncating<UInt128>(UInt128Tests_GenericMath.Int128MaxValuePlusOne));
            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateTruncating<UInt128>(UInt128.MaxValue));
        }

        [Fact]
        public static void CreateTruncatingFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateTruncating<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.CreateTruncating<nuint>(unchecked((nuint)0x0000000000000001)));
                Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateTruncating<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateTruncating<nuint>(unchecked((nuint)0x8000000000000000)));
                Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateTruncating<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateTruncating<nuint>((nuint)0x00000000));
                Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.CreateTruncating<nuint>((nuint)0x00000001));
                Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateTruncating<nuint>((nuint)0x7FFFFFFF));
                Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateTruncating<nuint>((nuint)0x80000000));
                Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateTruncating<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void IsCanonicalTest()
        {
            Assert.True(NumberBaseHelper<ushort>.IsCanonical((ushort)0x0000));
            Assert.True(NumberBaseHelper<ushort>.IsCanonical((ushort)0x0001));
            Assert.True(NumberBaseHelper<ushort>.IsCanonical((ushort)0x7FFF));
            Assert.True(NumberBaseHelper<ushort>.IsCanonical((ushort)0x8000));
            Assert.True(NumberBaseHelper<ushort>.IsCanonical((ushort)0xFFFF));
        }

        [Fact]
        public static void IsComplexNumberTest()
        {
            Assert.False(NumberBaseHelper<ushort>.IsComplexNumber((ushort)0x0000));
            Assert.False(NumberBaseHelper<ushort>.IsComplexNumber((ushort)0x0001));
            Assert.False(NumberBaseHelper<ushort>.IsComplexNumber((ushort)0x7FFF));
            Assert.False(NumberBaseHelper<ushort>.IsComplexNumber((ushort)0x8000));
            Assert.False(NumberBaseHelper<ushort>.IsComplexNumber((ushort)0xFFFF));
        }

        [Fact]
        public static void IsEvenIntegerTest()
        {
            Assert.True(NumberBaseHelper<ushort>.IsEvenInteger((ushort)0x0000));
            Assert.False(NumberBaseHelper<ushort>.IsEvenInteger((ushort)0x0001));
            Assert.False(NumberBaseHelper<ushort>.IsEvenInteger((ushort)0x7FFF));
            Assert.True(NumberBaseHelper<ushort>.IsEvenInteger((ushort)0x8000));
            Assert.False(NumberBaseHelper<ushort>.IsEvenInteger((ushort)0xFFFF));
        }

        [Fact]
        public static void IsFiniteTest()
        {
            Assert.True(NumberBaseHelper<ushort>.IsFinite((ushort)0x0000));
            Assert.True(NumberBaseHelper<ushort>.IsFinite((ushort)0x0001));
            Assert.True(NumberBaseHelper<ushort>.IsFinite((ushort)0x7FFF));
            Assert.True(NumberBaseHelper<ushort>.IsFinite((ushort)0x8000));
            Assert.True(NumberBaseHelper<ushort>.IsFinite((ushort)0xFFFF));
        }

        [Fact]
        public static void IsImaginaryNumberTest()
        {
            Assert.False(NumberBaseHelper<ushort>.IsImaginaryNumber((ushort)0x0000));
            Assert.False(NumberBaseHelper<ushort>.IsImaginaryNumber((ushort)0x0001));
            Assert.False(NumberBaseHelper<ushort>.IsImaginaryNumber((ushort)0x7FFF));
            Assert.False(NumberBaseHelper<ushort>.IsImaginaryNumber((ushort)0x8000));
            Assert.False(NumberBaseHelper<ushort>.IsImaginaryNumber((ushort)0xFFFF));
        }

        [Fact]
        public static void IsInfinityTest()
        {
            Assert.False(NumberBaseHelper<ushort>.IsInfinity((ushort)0x0000));
            Assert.False(NumberBaseHelper<ushort>.IsInfinity((ushort)0x0001));
            Assert.False(NumberBaseHelper<ushort>.IsInfinity((ushort)0x7FFF));
            Assert.False(NumberBaseHelper<ushort>.IsInfinity((ushort)0x8000));
            Assert.False(NumberBaseHelper<ushort>.IsInfinity((ushort)0xFFFF));
        }

        [Fact]
        public static void IsIntegerTest()
        {
            Assert.True(NumberBaseHelper<ushort>.IsInteger((ushort)0x0000));
            Assert.True(NumberBaseHelper<ushort>.IsInteger((ushort)0x0001));
            Assert.True(NumberBaseHelper<ushort>.IsInteger((ushort)0x7FFF));
            Assert.True(NumberBaseHelper<ushort>.IsInteger((ushort)0x8000));
            Assert.True(NumberBaseHelper<ushort>.IsInteger((ushort)0xFFFF));
        }

        [Fact]
        public static void IsNaNTest()
        {
            Assert.False(NumberBaseHelper<ushort>.IsNaN((ushort)0x0000));
            Assert.False(NumberBaseHelper<ushort>.IsNaN((ushort)0x0001));
            Assert.False(NumberBaseHelper<ushort>.IsNaN((ushort)0x7FFF));
            Assert.False(NumberBaseHelper<ushort>.IsNaN((ushort)0x8000));
            Assert.False(NumberBaseHelper<ushort>.IsNaN((ushort)0xFFFF));
        }

        [Fact]
        public static void IsNegativeTest()
        {
            Assert.False(NumberBaseHelper<ushort>.IsNegative((ushort)0x0000));
            Assert.False(NumberBaseHelper<ushort>.IsNegative((ushort)0x0001));
            Assert.False(NumberBaseHelper<ushort>.IsNegative((ushort)0x7FFF));
            Assert.False(NumberBaseHelper<ushort>.IsNegative((ushort)0x8000));
            Assert.False(NumberBaseHelper<ushort>.IsNegative((ushort)0xFFFF));
        }

        [Fact]
        public static void IsNegativeInfinityTest()
        {
            Assert.False(NumberBaseHelper<ushort>.IsNegativeInfinity((ushort)0x0000));
            Assert.False(NumberBaseHelper<ushort>.IsNegativeInfinity((ushort)0x0001));
            Assert.False(NumberBaseHelper<ushort>.IsNegativeInfinity((ushort)0x7FFF));
            Assert.False(NumberBaseHelper<ushort>.IsNegativeInfinity((ushort)0x8000));
            Assert.False(NumberBaseHelper<ushort>.IsNegativeInfinity((ushort)0xFFFF));
        }

        [Fact]
        public static void IsNormalTest()
        {
            Assert.False(NumberBaseHelper<ushort>.IsNormal((ushort)0x0000));
            Assert.True(NumberBaseHelper<ushort>.IsNormal((ushort)0x0001));
            Assert.True(NumberBaseHelper<ushort>.IsNormal((ushort)0x7FFF));
            Assert.True(NumberBaseHelper<ushort>.IsNormal((ushort)0x8000));
            Assert.True(NumberBaseHelper<ushort>.IsNormal((ushort)0xFFFF));
        }

        [Fact]
        public static void IsOddIntegerTest()
        {
            Assert.False(NumberBaseHelper<ushort>.IsOddInteger((ushort)0x0000));
            Assert.True(NumberBaseHelper<ushort>.IsOddInteger((ushort)0x0001));
            Assert.True(NumberBaseHelper<ushort>.IsOddInteger((ushort)0x7FFF));
            Assert.False(NumberBaseHelper<ushort>.IsOddInteger((ushort)0x8000));
            Assert.True(NumberBaseHelper<ushort>.IsOddInteger((ushort)0xFFFF));
        }

        [Fact]
        public static void IsPositiveTest()
        {
            Assert.True(NumberBaseHelper<ushort>.IsPositive((ushort)0x0000));
            Assert.True(NumberBaseHelper<ushort>.IsPositive((ushort)0x0001));
            Assert.True(NumberBaseHelper<ushort>.IsPositive((ushort)0x7FFF));
            Assert.True(NumberBaseHelper<ushort>.IsPositive((ushort)0x8000));
            Assert.True(NumberBaseHelper<ushort>.IsPositive((ushort)0xFFFF));
        }

        [Fact]
        public static void IsPositiveInfinityTest()
        {
            Assert.False(NumberBaseHelper<ushort>.IsPositiveInfinity((ushort)0x0000));
            Assert.False(NumberBaseHelper<ushort>.IsPositiveInfinity((ushort)0x0001));
            Assert.False(NumberBaseHelper<ushort>.IsPositiveInfinity((ushort)0x7FFF));
            Assert.False(NumberBaseHelper<ushort>.IsPositiveInfinity((ushort)0x8000));
            Assert.False(NumberBaseHelper<ushort>.IsPositiveInfinity((ushort)0xFFFF));
        }

        [Fact]
        public static void IsRealNumberTest()
        {
            Assert.True(NumberBaseHelper<ushort>.IsRealNumber((ushort)0x0000));
            Assert.True(NumberBaseHelper<ushort>.IsRealNumber((ushort)0x0001));
            Assert.True(NumberBaseHelper<ushort>.IsRealNumber((ushort)0x7FFF));
            Assert.True(NumberBaseHelper<ushort>.IsRealNumber((ushort)0x8000));
            Assert.True(NumberBaseHelper<ushort>.IsRealNumber((ushort)0xFFFF));
        }

        [Fact]
        public static void IsSubnormalTest()
        {
            Assert.False(NumberBaseHelper<ushort>.IsSubnormal((ushort)0x0000));
            Assert.False(NumberBaseHelper<ushort>.IsSubnormal((ushort)0x0001));
            Assert.False(NumberBaseHelper<ushort>.IsSubnormal((ushort)0x7FFF));
            Assert.False(NumberBaseHelper<ushort>.IsSubnormal((ushort)0x8000));
            Assert.False(NumberBaseHelper<ushort>.IsSubnormal((ushort)0xFFFF));
        }

        [Fact]
        public static void IsZeroTest()
        {
            Assert.True(NumberBaseHelper<ushort>.IsZero((ushort)0x0000));
            Assert.False(NumberBaseHelper<ushort>.IsZero((ushort)0x0001));
            Assert.False(NumberBaseHelper<ushort>.IsZero((ushort)0x7FFF));
            Assert.False(NumberBaseHelper<ushort>.IsZero((ushort)0x8000));
            Assert.False(NumberBaseHelper<ushort>.IsZero((ushort)0xFFFF));
        }

        [Fact]
        public static void MaxMagnitudeTest()
        {
            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.MaxMagnitude((ushort)0x0000, (ushort)1));
            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.MaxMagnitude((ushort)0x0001, (ushort)1));
            Assert.Equal((ushort)0x7FFF, NumberBaseHelper<ushort>.MaxMagnitude((ushort)0x7FFF, (ushort)1));
            Assert.Equal((ushort)0x8000, NumberBaseHelper<ushort>.MaxMagnitude((ushort)0x8000, (ushort)1));
            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.MaxMagnitude((ushort)0xFFFF, (ushort)1));
        }

        [Fact]
        public static void MaxMagnitudeNumberTest()
        {
            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.MaxMagnitudeNumber((ushort)0x0000, (ushort)1));
            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.MaxMagnitudeNumber((ushort)0x0001, (ushort)1));
            Assert.Equal((ushort)0x7FFF, NumberBaseHelper<ushort>.MaxMagnitudeNumber((ushort)0x7FFF, (ushort)1));
            Assert.Equal((ushort)0x8000, NumberBaseHelper<ushort>.MaxMagnitudeNumber((ushort)0x8000, (ushort)1));
            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.MaxMagnitudeNumber((ushort)0xFFFF, (ushort)1));
        }

        [Fact]
        public static void MinMagnitudeTest()
        {
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.MinMagnitude((ushort)0x0000, (ushort)1));
            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.MinMagnitude((ushort)0x0001, (ushort)1));
            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.MinMagnitude((ushort)0x7FFF, (ushort)1));
            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.MinMagnitude((ushort)0x8000, (ushort)1));
            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.MinMagnitude((ushort)0xFFFF, (ushort)1));
        }

        [Fact]
        public static void MinMagnitudeNumberTest()
        {
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.MinMagnitudeNumber((ushort)0x0000, (ushort)1));
            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.MinMagnitudeNumber((ushort)0x0001, (ushort)1));
            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.MinMagnitudeNumber((ushort)0x7FFF, (ushort)1));
            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.MinMagnitudeNumber((ushort)0x8000, (ushort)1));
            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.MinMagnitudeNumber((ushort)0xFFFF, (ushort)1));
        }

        //
        // IShiftOperators
        //

        [Fact]
        public static void op_LeftShiftTest()
        {
            Assert.Equal((ushort)0x0000, ShiftOperatorsHelper<ushort, int, ushort>.op_LeftShift((ushort)0x0000, 1));
            Assert.Equal((ushort)0x0002, ShiftOperatorsHelper<ushort, int, ushort>.op_LeftShift((ushort)0x0001, 1));
            Assert.Equal((ushort)0xFFFE, ShiftOperatorsHelper<ushort, int, ushort>.op_LeftShift((ushort)0x7FFF, 1));
            Assert.Equal((ushort)0x0000, ShiftOperatorsHelper<ushort, int, ushort>.op_LeftShift((ushort)0x8000, 1));
            Assert.Equal((ushort)0xFFFE, ShiftOperatorsHelper<ushort, int, ushort>.op_LeftShift((ushort)0xFFFF, 1));
        }

        [Fact]
        public static void op_RightShiftTest()
        {
            Assert.Equal((ushort)0x0000, ShiftOperatorsHelper<ushort, int, ushort>.op_RightShift((ushort)0x0000, 1));
            Assert.Equal((ushort)0x0000, ShiftOperatorsHelper<ushort, int, ushort>.op_RightShift((ushort)0x0001, 1));
            Assert.Equal((ushort)0x3FFF, ShiftOperatorsHelper<ushort, int, ushort>.op_RightShift((ushort)0x7FFF, 1));
            Assert.Equal((ushort)0x4000, ShiftOperatorsHelper<ushort, int, ushort>.op_RightShift((ushort)0x8000, 1));
            Assert.Equal((ushort)0x7FFF, ShiftOperatorsHelper<ushort, int, ushort>.op_RightShift((ushort)0xFFFF, 1));
        }

        [Fact]
        public static void op_UnsignedRightShiftTest()
        {
            Assert.Equal((ushort)0x0000, ShiftOperatorsHelper<ushort, int, ushort>.op_UnsignedRightShift((ushort)0x0000, 1));
            Assert.Equal((ushort)0x0000, ShiftOperatorsHelper<ushort, int, ushort>.op_UnsignedRightShift((ushort)0x0001, 1));
            Assert.Equal((ushort)0x3FFF, ShiftOperatorsHelper<ushort, int, ushort>.op_UnsignedRightShift((ushort)0x7FFF, 1));
            Assert.Equal((ushort)0x4000, ShiftOperatorsHelper<ushort, int, ushort>.op_UnsignedRightShift((ushort)0x8000, 1));
            Assert.Equal((ushort)0x7FFF, ShiftOperatorsHelper<ushort, int, ushort>.op_UnsignedRightShift((ushort)0xFFFF, 1));
        }

        //
        // ISubtractionOperators
        //

        [Fact]
        public static void op_SubtractionTest()
        {
            Assert.Equal((ushort)0xFFFF, SubtractionOperatorsHelper<ushort, ushort, ushort>.op_Subtraction((ushort)0x0000, (ushort)1));
            Assert.Equal((ushort)0x0000, SubtractionOperatorsHelper<ushort, ushort, ushort>.op_Subtraction((ushort)0x0001, (ushort)1));
            Assert.Equal((ushort)0x7FFE, SubtractionOperatorsHelper<ushort, ushort, ushort>.op_Subtraction((ushort)0x7FFF, (ushort)1));
            Assert.Equal((ushort)0x7FFF, SubtractionOperatorsHelper<ushort, ushort, ushort>.op_Subtraction((ushort)0x8000, (ushort)1));
            Assert.Equal((ushort)0xFFFE, SubtractionOperatorsHelper<ushort, ushort, ushort>.op_Subtraction((ushort)0xFFFF, (ushort)1));
        }

        [Fact]
        public static void op_CheckedSubtractionTest()
        {
            Assert.Equal((ushort)0x0000, SubtractionOperatorsHelper<ushort, ushort, ushort>.op_CheckedSubtraction((ushort)0x0001, (ushort)1));
            Assert.Equal((ushort)0x7FFE, SubtractionOperatorsHelper<ushort, ushort, ushort>.op_CheckedSubtraction((ushort)0x7FFF, (ushort)1));
            Assert.Equal((ushort)0x7FFF, SubtractionOperatorsHelper<ushort, ushort, ushort>.op_CheckedSubtraction((ushort)0x8000, (ushort)1));
            Assert.Equal((ushort)0xFFFE, SubtractionOperatorsHelper<ushort, ushort, ushort>.op_CheckedSubtraction((ushort)0xFFFF, (ushort)1));

            Assert.Throws<OverflowException>(() => SubtractionOperatorsHelper<ushort, ushort, ushort>.op_CheckedSubtraction((ushort)0x0000, (ushort)1));
        }

        //
        // IUnaryNegationOperators
        //

        [Fact]
        public static void op_UnaryNegationTest()
        {
            Assert.Equal((ushort)0x0000, UnaryNegationOperatorsHelper<ushort, ushort>.op_UnaryNegation((ushort)0x0000));
            Assert.Equal((ushort)0xFFFF, UnaryNegationOperatorsHelper<ushort, ushort>.op_UnaryNegation((ushort)0x0001));
            Assert.Equal((ushort)0x8001, UnaryNegationOperatorsHelper<ushort, ushort>.op_UnaryNegation((ushort)0x7FFF));
            Assert.Equal((ushort)0x8000, UnaryNegationOperatorsHelper<ushort, ushort>.op_UnaryNegation((ushort)0x8000));
            Assert.Equal((ushort)0x0001, UnaryNegationOperatorsHelper<ushort, ushort>.op_UnaryNegation((ushort)0xFFFF));
        }

        [Fact]
        public static void op_CheckedUnaryNegationTest()
        {
            Assert.Equal((ushort)0x0000, UnaryNegationOperatorsHelper<ushort, ushort>.op_CheckedUnaryNegation((ushort)0x0000));

            Assert.Throws<OverflowException>(() => UnaryNegationOperatorsHelper<ushort, ushort>.op_CheckedUnaryNegation((ushort)0x0001));
            Assert.Throws<OverflowException>(() => UnaryNegationOperatorsHelper<ushort, ushort>.op_CheckedUnaryNegation((ushort)0x7FFF));
            Assert.Throws<OverflowException>(() => UnaryNegationOperatorsHelper<ushort, ushort>.op_CheckedUnaryNegation((ushort)0x8000));
            Assert.Throws<OverflowException>(() => UnaryNegationOperatorsHelper<ushort, ushort>.op_CheckedUnaryNegation((ushort)0xFFFF));
        }

        //
        // IUnaryPlusOperators
        //

        [Fact]
        public static void op_UnaryPlusTest()
        {
            Assert.Equal((ushort)0x0000, UnaryPlusOperatorsHelper<ushort, ushort>.op_UnaryPlus((ushort)0x0000));
            Assert.Equal((ushort)0x0001, UnaryPlusOperatorsHelper<ushort, ushort>.op_UnaryPlus((ushort)0x0001));
            Assert.Equal((ushort)0x7FFF, UnaryPlusOperatorsHelper<ushort, ushort>.op_UnaryPlus((ushort)0x7FFF));
            Assert.Equal((ushort)0x8000, UnaryPlusOperatorsHelper<ushort, ushort>.op_UnaryPlus((ushort)0x8000));
            Assert.Equal((ushort)0xFFFF, UnaryPlusOperatorsHelper<ushort, ushort>.op_UnaryPlus((ushort)0xFFFF));
        }

        //
        // IParsable and ISpanParsable
        //

        [Theory]
        [MemberData(nameof(UInt16Tests.Parse_Valid_TestData), MemberType = typeof(UInt16Tests))]
        public static void ParseValidStringTest(string value, NumberStyles style, IFormatProvider provider, ushort expected)
        {
            ushort result;

            // Default style and provider
            if ((style == NumberStyles.Integer) && (provider is null))
            {
                Assert.True(ParsableHelper<ushort>.TryParse(value, provider, out result));
                Assert.Equal(expected, result);
                Assert.Equal(expected, ParsableHelper<ushort>.Parse(value, provider));
            }

            // Default provider
            if (provider is null)
            {
                Assert.Equal(expected, NumberBaseHelper<ushort>.Parse(value, style, provider));

                // Substitute default NumberFormatInfo
                Assert.True(NumberBaseHelper<ushort>.TryParse(value, style, new NumberFormatInfo(), out result));
                Assert.Equal(expected, result);
                Assert.Equal(expected, NumberBaseHelper<ushort>.Parse(value, style, new NumberFormatInfo()));
            }

            // Default style
            if (style == NumberStyles.Integer)
            {
                Assert.Equal(expected, ParsableHelper<ushort>.Parse(value, provider));
            }

            // Full overloads
            Assert.True(NumberBaseHelper<ushort>.TryParse(value, style, provider, out result));
            Assert.Equal(expected, result);
            Assert.Equal(expected, NumberBaseHelper<ushort>.Parse(value, style, provider));
        }

        [Theory]
        [MemberData(nameof(UInt16Tests.Parse_Invalid_TestData), MemberType = typeof(UInt16Tests))]
        public static void ParseInvalidStringTest(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            ushort result;

            // Default style and provider
            if ((style == NumberStyles.Integer) && (provider is null))
            {
                Assert.False(ParsableHelper<ushort>.TryParse(value, provider, out result));
                Assert.Equal(default(ushort), result);
                Assert.Throws(exceptionType, () => ParsableHelper<ushort>.Parse(value, provider));
            }

            // Default provider
            if (provider is null)
            {
                Assert.Throws(exceptionType, () => NumberBaseHelper<ushort>.Parse(value, style, provider));

                // Substitute default NumberFormatInfo
                Assert.False(NumberBaseHelper<ushort>.TryParse(value, style, new NumberFormatInfo(), out result));
                Assert.Equal(default(ushort), result);
                Assert.Throws(exceptionType, () => NumberBaseHelper<ushort>.Parse(value, style, new NumberFormatInfo()));
            }

            // Default style
            if (style == NumberStyles.Integer)
            {
                Assert.Throws(exceptionType, () => ParsableHelper<ushort>.Parse(value, provider));
            }

            // Full overloads
            Assert.False(NumberBaseHelper<ushort>.TryParse(value, style, provider, out result));
            Assert.Equal(default(ushort), result);
            Assert.Throws(exceptionType, () => NumberBaseHelper<ushort>.Parse(value, style, provider));
        }

        [Theory]
        [MemberData(nameof(UInt16Tests.Parse_ValidWithOffsetCount_TestData), MemberType = typeof(UInt16Tests))]
        public static void ParseValidSpanTest(string value, int offset, int count, NumberStyles style, IFormatProvider provider, ushort expected)
        {
            ushort result;

            // Default style and provider
            if ((style == NumberStyles.Integer) && (provider is null))
            {
                Assert.True(SpanParsableHelper<ushort>.TryParse(value.AsSpan(offset, count), provider, out result));
                Assert.Equal(expected, result);
            }

            Assert.Equal(expected, NumberBaseHelper<ushort>.Parse(value.AsSpan(offset, count), style, provider));

            Assert.True(NumberBaseHelper<ushort>.TryParse(value.AsSpan(offset, count), style, provider, out result));
            Assert.Equal(expected, result);
        }

        [Theory]
        [MemberData(nameof(UInt16Tests.Parse_Invalid_TestData), MemberType = typeof(UInt16Tests))]
        public static void ParseInvalidSpanTest(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            if (value is null)
            {
                return;
            }

            ushort result;

            // Default style and provider
            if ((style == NumberStyles.Integer) && (provider is null))
            {
                Assert.False(SpanParsableHelper<ushort>.TryParse(value.AsSpan(), provider, out result));
                Assert.Equal(default(ushort), result);
            }

            Assert.Throws(exceptionType, () => NumberBaseHelper<ushort>.Parse(value.AsSpan(), style, provider));

            Assert.False(NumberBaseHelper<ushort>.TryParse(value.AsSpan(), style, provider, out result));
            Assert.Equal(default(ushort), result);
        }
    }
}
