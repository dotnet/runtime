// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Tests
{
    public class Int64Tests_GenericMath
    {
        //
        // IAdditionOperators
        //

        [Fact]
        public static void op_AdditionTest()
        {
            Assert.Equal((long)0x0000000000000001, AdditionOperatorsHelper<long, long, long>.op_Addition((long)0x0000000000000000, 1));
            Assert.Equal((long)0x0000000000000002, AdditionOperatorsHelper<long, long, long>.op_Addition((long)0x0000000000000001, 1));
            Assert.Equal(unchecked((long)0x8000000000000000), AdditionOperatorsHelper<long, long, long>.op_Addition((long)0x7FFFFFFFFFFFFFFF, 1));
            Assert.Equal(unchecked((long)0x8000000000000001), AdditionOperatorsHelper<long, long, long>.op_Addition(unchecked((long)0x8000000000000000), 1));
            Assert.Equal((long)0x0000000000000000, AdditionOperatorsHelper<long, long, long>.op_Addition(unchecked((long)0xFFFFFFFFFFFFFFFF), 1));
        }

        [Fact]
        public static void op_CheckedAdditionTest()
        {
            Assert.Equal((long)0x0000000000000001, AdditionOperatorsHelper<long, long, long>.op_CheckedAddition((long)0x0000000000000000, 1));
            Assert.Equal((long)0x0000000000000002, AdditionOperatorsHelper<long, long, long>.op_CheckedAddition((long)0x0000000000000001, 1));
            Assert.Equal(unchecked((long)0x8000000000000001), AdditionOperatorsHelper<long, long, long>.op_CheckedAddition(unchecked((long)0x8000000000000000), 1));
            Assert.Equal((long)0x0000000000000000, AdditionOperatorsHelper<long, long, long>.op_CheckedAddition(unchecked((long)0xFFFFFFFFFFFFFFFF), 1));

            Assert.Throws<OverflowException>(() => AdditionOperatorsHelper<long, long, long>.op_CheckedAddition((long)0x7FFFFFFFFFFFFFFF, 1));
        }

        //
        // IAdditiveIdentity
        //

        [Fact]
        public static void AdditiveIdentityTest()
        {
            Assert.Equal((long)0x0000000000000000, AdditiveIdentityHelper<long, long>.AdditiveIdentity);
        }

        //
        // IBinaryInteger
        //

        [Fact]
        public static void DivRemTest()
        {
            Assert.Equal(((long)0x0000000000000000, (long)0x0000000000000000), BinaryIntegerHelper<long>.DivRem((long)0x0000000000000000, 2));
            Assert.Equal(((long)0x0000000000000000, (long)0x0000000000000001), BinaryIntegerHelper<long>.DivRem((long)0x0000000000000001, 2));
            Assert.Equal(((long)0x3FFFFFFFFFFFFFFF, (long)0x0000000000000001), BinaryIntegerHelper<long>.DivRem((long)0x7FFFFFFFFFFFFFFF, 2));
            Assert.Equal((unchecked((long)0xC000000000000000), (long)0x0000000000000000), BinaryIntegerHelper<long>.DivRem(unchecked((long)0x8000000000000000), 2));
            Assert.Equal(((long)0x0000000000000000, unchecked((long)0xFFFFFFFFFFFFFFFF)), BinaryIntegerHelper<long>.DivRem(unchecked((long)0xFFFFFFFFFFFFFFFF), 2));
        }

        [Fact]
        public static void LeadingZeroCountTest()
        {
            Assert.Equal((long)0x0000000000000040, BinaryIntegerHelper<long>.LeadingZeroCount((long)0x0000000000000000));
            Assert.Equal((long)0x000000000000003F, BinaryIntegerHelper<long>.LeadingZeroCount((long)0x0000000000000001));
            Assert.Equal((long)0x0000000000000001, BinaryIntegerHelper<long>.LeadingZeroCount((long)0x7FFFFFFFFFFFFFFF));
            Assert.Equal((long)0x0000000000000000, BinaryIntegerHelper<long>.LeadingZeroCount(unchecked((long)0x8000000000000000)));
            Assert.Equal((long)0x0000000000000000, BinaryIntegerHelper<long>.LeadingZeroCount(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void PopCountTest()
        {
            Assert.Equal((long)0x0000000000000000, BinaryIntegerHelper<long>.PopCount((long)0x0000000000000000));
            Assert.Equal((long)0x0000000000000001, BinaryIntegerHelper<long>.PopCount((long)0x0000000000000001));
            Assert.Equal((long)0x000000000000003F, BinaryIntegerHelper<long>.PopCount((long)0x7FFFFFFFFFFFFFFF));
            Assert.Equal((long)0x0000000000000001, BinaryIntegerHelper<long>.PopCount(unchecked((long)0x8000000000000000)));
            Assert.Equal((long)0x0000000000000040, BinaryIntegerHelper<long>.PopCount(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void RotateLeftTest()
        {
            Assert.Equal((long)0x0000000000000000, BinaryIntegerHelper<long>.RotateLeft((long)0x0000000000000000, 1));
            Assert.Equal((long)0x0000000000000002, BinaryIntegerHelper<long>.RotateLeft((long)0x0000000000000001, 1));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFE), BinaryIntegerHelper<long>.RotateLeft((long)0x7FFFFFFFFFFFFFFF, 1));
            Assert.Equal((long)0x0000000000000001, BinaryIntegerHelper<long>.RotateLeft(unchecked((long)0x8000000000000000), 1));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), BinaryIntegerHelper<long>.RotateLeft(unchecked((long)0xFFFFFFFFFFFFFFFF), 1));
        }

        [Fact]
        public static void RotateRightTest()
        {
            Assert.Equal((long)0x0000000000000000, BinaryIntegerHelper<long>.RotateRight((long)0x0000000000000000, 1));
            Assert.Equal(unchecked((long)0x8000000000000000), BinaryIntegerHelper<long>.RotateRight((long)0x0000000000000001, 1));
            Assert.Equal(unchecked((long)0xBFFFFFFFFFFFFFFF), BinaryIntegerHelper<long>.RotateRight((long)0x7FFFFFFFFFFFFFFF, 1));
            Assert.Equal((long)0x4000000000000000, BinaryIntegerHelper<long>.RotateRight(unchecked((long)0x8000000000000000), 1));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), BinaryIntegerHelper<long>.RotateRight(unchecked((long)0xFFFFFFFFFFFFFFFF), 1));
        }

        [Fact]
        public static void TrailingZeroCountTest()
        {
            Assert.Equal((long)0x0000000000000040, BinaryIntegerHelper<long>.TrailingZeroCount((long)0x0000000000000000));
            Assert.Equal((long)0x0000000000000000, BinaryIntegerHelper<long>.TrailingZeroCount((long)0x0000000000000001));
            Assert.Equal((long)0x0000000000000000, BinaryIntegerHelper<long>.TrailingZeroCount((long)0x7FFFFFFFFFFFFFFF));
            Assert.Equal((long)0x000000000000003F, BinaryIntegerHelper<long>.TrailingZeroCount(unchecked((long)0x8000000000000000)));
            Assert.Equal((long)0x0000000000000000, BinaryIntegerHelper<long>.TrailingZeroCount(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void TryReadBigEndianByteTest()
        {
            long result;

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0x00 }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_0000, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0x01 }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_0001, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0x7F }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_007F, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0x80 }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_0080, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0xFF }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_00FF, result);
        }

        [Fact]
        public static void TryReadBigEndianInt16Test()
        {
            long result;

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((long)0x0000_0000_0000_0000, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal((long)0x0000_0000_0000_0001, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal((long)0x0000_0000_0000_0080, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0x01, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((long)0x0000_0000_0000_0100, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0x7F, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((long)0x0000_0000_0000_7FFF, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0x80, 0x00 }, isUnsigned: false, out result));
            Assert.Equal(unchecked((long)0xFFFF_FFFF_FFFF_8000), result);

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal(unchecked((long)0xFFFF_FFFF_FFFF_FF7F), result);

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(unchecked((long)0xFFFF_FFFF_FFFF_FFFF), result);
        }

        [Fact]
        public static void TryReadBigEndianInt32Test()
        {
            long result;

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((long)0x0000_0000_0000_0000, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal((long)0x0000_0000_0000_0001, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal((long)0x0000_0000_0000_0080, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0x01, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((long)0x0000_0000_0100_0000, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((long)0x0000_0000_7FFF_FFFF, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0x80, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal(unchecked((long)0xFFFF_FFFF_8000_0000), result);

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal(unchecked((long)0xFFFF_FFFF_FFFF_FF7F), result);

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(unchecked((long)0xFFFF_FFFF_FFFF_FFFF), result);
        }

        [Fact]
        public static void TryReadBigEndianInt64Test()
        {
            long result;

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((long)0x0000_0000_0000_0000, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal((long)0x0000_0000_0000_0001, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal((long)0x0000_0000_0000_0080, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((long)0x0100_0000_0000_0000, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((long)0x7FFF_FFFF_FFFF_FFFF, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal(unchecked((long)0x8000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal(unchecked((long)0xFFFF_FFFF_FFFF_FF7F), result);

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(unchecked((long)0xFFFF_FFFF_FFFF_FFFF), result);
        }

        [Fact]
        public static void TryReadBigEndianInt96Test()
        {
            long result;

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((long)0x0000_0000_0000_0000, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal((long)0x0000_0000_0000_0001, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal((long)0x0000_0000_0000_0080, result);

            Assert.False(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((long)0x0000_0000_0000_0000, result);

            Assert.False(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((long)0x0000_0000_0000_0000, result);

            Assert.False(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((long)0x0000_0000_0000_0000, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal(unchecked((long)0xFFFF_FFFF_FFFF_FF7F), result);

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(unchecked((long)0xFFFF_FFFF_FFFF_FFFF), result);
        }

        [Fact]
        public static void TryReadBigEndianInt128Test()
        {
            long result;

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((long)0x0000_0000_0000_0000, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal((long)0x0000_0000_0000_0001, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal((long)0x0000_0000_0000_0080, result);

            Assert.False(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((long)0x0000_0000_0000_0000, result);

            Assert.False(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((long)0x0000_0000_0000_0000, result);

            Assert.False(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((long)0x0000_0000_0000_0000, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal(unchecked((long)0xFFFF_FFFF_FFFF_FF7F), result);

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(unchecked((long)0xFFFF_FFFF_FFFF_FFFF), result);
        }

        [Fact]
        public static void TryReadBigEndianSByteTest()
        {
            long result;

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0x00 }, isUnsigned: false, out result));
            Assert.Equal((long)0x0000_0000_0000_0000, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0x01 }, isUnsigned: false, out result));
            Assert.Equal((long)0x0000_0000_0000_0001, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0x7F }, isUnsigned: false, out result));
            Assert.Equal((long)0x0000_0000_0000_007F, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0x80 }, isUnsigned: false, out result));
            Assert.Equal(unchecked((long)0xFFFF_FFFF_FFFF_FF80), result);

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0xFF }, isUnsigned: false, out result));
            Assert.Equal(unchecked((long)0xFFFF_FFFF_FFFF_FFFF), result);
        }

        [Fact]
        public static void TryReadBigEndianUInt16Test()
        {
            long result;

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_0000, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_0001, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_0080, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0x01, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_0100, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0x7F, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_7FFF, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0x80, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_8000, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_FF7F, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_FFFF, result);
        }

        [Fact]
        public static void TryReadBigEndianUInt32Test()
        {
            long result;

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_0000, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_0001, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_0080, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0x01, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0100_0000, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_7FFF_FFFF, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0x80, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_8000_0000, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_FFFF_FF7F, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_FFFF_FFFF, result);
        }

        [Fact]
        public static void TryReadBigEndianUInt64Test()
        {
            long result;

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_0000, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_0001, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_0080, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((long)0x0100_0000_0000_0000, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((long)0x7FFF_FFFF_FFFF_FFFF, result);

            Assert.False(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_0000, result);

            Assert.False(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_0000, result);

            Assert.False(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_0000, result);
        }

        [Fact]
        public static void TryReadBigEndianUInt96Test()
        {
            long result;

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_0000, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_0001, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_0080, result);

            Assert.False(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_0000, result);

            Assert.False(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_0000, result);

            Assert.False(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_0000, result);

            Assert.False(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_0000, result);

            Assert.False(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_0000, result);
        }

        [Fact]
        public static void TryReadBigEndianUInt128Test()
        {
            long result;

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_0000, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_0001, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_0080, result);

            Assert.False(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_0000, result);

            Assert.False(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_0000, result);

            Assert.False(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_0000, result);

            Assert.False(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_0000, result);

            Assert.False(BinaryIntegerHelper<long>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_0000, result);
        }

        [Fact]
        public static void TryReadLittleEndianByteTest()
        {
            long result;

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0x00 }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_0000, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0x01 }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_0001, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0x7F }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_007F, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0x80 }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_0080, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0xFF }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_00FF, result);
        }

        [Fact]
        public static void TryReadLittleEndianInt16Test()
        {
            long result;

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((long)0x0000_0000_0000_0000, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal((long)0x0000_0000_0000_0100, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal(unchecked((long)0xFFFF_FFFF_FFFF_8000), result);

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0x01, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((long)0x0000_0000_0000_0001, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(unchecked((long)0xFFFF_FFFF_FFFF_FF7F), result);

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0x80, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((long)0x0000_0000_0000_0080, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal((long)0x0000_0000_0000_7FFF, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(unchecked((long)0xFFFF_FFFF_FFFF_FFFF), result);
        }

        [Fact]
        public static void TryReadLittleEndianInt32Test()
        {
            long result;

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((long)0x0000_0000_0000_0000, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal((long)0x0000_0000_0100_0000, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal(unchecked((long)0xFFFF_FFFF_8000_0000), result);

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0x01, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((long)0x0000_0000_0000_0001, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(unchecked((long)0xFFFF_FFFF_FFFF_FF7F), result);

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0x80, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((long)0x0000_0000_0000_0080, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal((long)0x0000_0000_7FFF_FFFF, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(unchecked((long)0xFFFF_FFFF_FFFF_FFFF), result);
        }

        [Fact]
        public static void TryReadLittleEndianInt64Test()
        {
            long result;

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((long)0x0000_0000_0000_0000, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal((long)0x0100_0000_0000_0000, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal(unchecked((long)0x8000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((long)0x0000_0000_0000_0001, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(unchecked((long)0xFFFF_FFFF_FFFF_FF7F), result);

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((long)0x0000_0000_0000_0080, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal((long)0x7FFF_FFFF_FFFF_FFFF, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(unchecked((long)0xFFFF_FFFF_FFFF_FFFF), result);
        }

        [Fact]
        public static void TryReadLittleEndianInt96Test()
        {
            long result;

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((long)0x0000_0000_0000_0000, result);

            Assert.False(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal((long)0x0000_0000_0000_0000, result);

            Assert.False(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal((long)0x0000_0000_0000_0000, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((long)0x0000_0000_0000_0001, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(unchecked((long)0xFFFF_FFFF_FFFF_FF7F), result);

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((long)0x0000_0000_0000_0080, result);

            Assert.False(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal((long)0x0000_0000_0000_0000, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(unchecked((long)0xFFFF_FFFF_FFFF_FFFF), result);
        }

        [Fact]
        public static void TryReadLittleEndianInt128Test()
        {
            long result;

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((long)0x0000_0000_0000_0000, result);

            Assert.False(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal((long)0x0000_0000_0000_0000, result);

            Assert.False(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal((long)0x0000_0000_0000_0000, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((long)0x0000_0000_0000_0001, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(unchecked((long)0xFFFF_FFFF_FFFF_FF7F), result);

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((long)0x0000_0000_0000_0080, result);

            Assert.False(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal((long)0x0000_0000_0000_0000, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(unchecked((long)0xFFFF_FFFF_FFFF_FFFF), result);
        }

        [Fact]
        public static void TryReadLittleEndianSByteTest()
        {
            long result;

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0x00 }, isUnsigned: false, out result));
            Assert.Equal((long)0x0000_0000_0000_0000, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0x01 }, isUnsigned: false, out result));
            Assert.Equal((long)0x0000_0000_0000_0001, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0x7F }, isUnsigned: false, out result));
            Assert.Equal((long)0x0000_0000_0000_007F, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0x80 }, isUnsigned: false, out result));
            Assert.Equal(unchecked((long)0xFFFF_FFFF_FFFF_FF80), result);

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0xFF }, isUnsigned: false, out result));
            Assert.Equal(unchecked((long)0xFFFF_FFFF_FFFF_FFFF), result);
        }

        [Fact]
        public static void TryReadLittleEndianUInt16Test()
        {
            long result;

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_0000, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_0100, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_8000, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0x01, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_0001, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_FF7F, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0x80, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_0080, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_7FFF, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_FFFF, result);
        }

        [Fact]
        public static void TryReadLittleEndianUInt32Test()
        {
            long result;

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_0000, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0100_0000, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_8000_0000, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0x01, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_0001, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_FFFF_FF7F, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0x80, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_0080, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_7FFF_FFFF, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_FFFF_FFFF, result);
        }

        [Fact]
        public static void TryReadLittleEndianUInt64Test()
        {
            long result;

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_0000, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal((long)0x0100_0000_0000_0000, result);

            Assert.False(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_0000, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_0001, result);

            Assert.False(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_0000, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_0080, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal((long)0x7FFF_FFFF_FFFF_FFFF, result);

            Assert.False(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_0000, result);
        }

        [Fact]
        public static void TryReadLittleEndianUInt96Test()
        {
            long result;

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_0000, result);

            Assert.False(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_0000, result);

            Assert.False(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_0000, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_0001, result);

            Assert.False(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_0000, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_0080, result);

            Assert.False(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_0000, result);

            Assert.False(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_0000, result);
        }

        [Fact]
        public static void TryReadLittleEndianUInt128Test()
        {
            long result;

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_0000, result);

            Assert.False(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_0000, result);

            Assert.False(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_0000, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_0001, result);

            Assert.False(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_0000, result);

            Assert.True(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_0080, result);

            Assert.False(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_0000, result);

            Assert.False(BinaryIntegerHelper<long>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((long)0x0000_0000_0000_0000, result);
        }

        [Fact]
        public static void GetByteCountTest()
        {
            Assert.Equal(8, BinaryIntegerHelper<long>.GetByteCount((long)0x0000000000000000));
            Assert.Equal(8, BinaryIntegerHelper<long>.GetByteCount((long)0x0000000000000001));
            Assert.Equal(8, BinaryIntegerHelper<long>.GetByteCount((long)0x7FFFFFFFFFFFFFFF));
            Assert.Equal(8, BinaryIntegerHelper<long>.GetByteCount(unchecked((long)0x8000000000000000)));
            Assert.Equal(8, BinaryIntegerHelper<long>.GetByteCount(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void GetShortestBitLengthTest()
        {
            Assert.Equal(0x00, BinaryIntegerHelper<long>.GetShortestBitLength((long)0x0000000000000000));
            Assert.Equal(0x01, BinaryIntegerHelper<long>.GetShortestBitLength((long)0x0000000000000001));
            Assert.Equal(0x3F, BinaryIntegerHelper<long>.GetShortestBitLength((long)0x7FFFFFFFFFFFFFFF));
            Assert.Equal(0x40, BinaryIntegerHelper<long>.GetShortestBitLength(unchecked((long)0x8000000000000000)));
            Assert.Equal(0x01, BinaryIntegerHelper<long>.GetShortestBitLength(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void TryWriteBigEndianTest()
        {
            Span<byte> destination = stackalloc byte[8];
            int bytesWritten = 0;

            Assert.True(BinaryIntegerHelper<long>.TryWriteBigEndian((long)0x0000000000000000, destination, out bytesWritten));
            Assert.Equal(8, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<long>.TryWriteBigEndian((long)0x0000000000000001, destination, out bytesWritten));
            Assert.Equal(8, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<long>.TryWriteBigEndian((long)0x7FFFFFFFFFFFFFFF, destination, out bytesWritten));
            Assert.Equal(8, bytesWritten);
            Assert.Equal(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<long>.TryWriteBigEndian(unchecked((long)0x8000000000000000), destination, out bytesWritten));
            Assert.Equal(8, bytesWritten);
            Assert.Equal(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<long>.TryWriteBigEndian(unchecked((long)0xFFFFFFFFFFFFFFFF), destination, out bytesWritten));
            Assert.Equal(8, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, destination.ToArray());

            Assert.False(BinaryIntegerHelper<long>.TryWriteBigEndian(default, Span<byte>.Empty, out bytesWritten));
            Assert.Equal(0, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, destination.ToArray());
        }

        [Fact]
        public static void TryWriteLittleEndianTest()
        {
            Span<byte> destination = stackalloc byte[8];
            int bytesWritten = 0;

            Assert.True(BinaryIntegerHelper<long>.TryWriteLittleEndian((long)0x0000000000000000, destination, out bytesWritten));
            Assert.Equal(8, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<long>.TryWriteLittleEndian((long)0x0000000000000001, destination, out bytesWritten));
            Assert.Equal(8, bytesWritten);
            Assert.Equal(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<long>.TryWriteLittleEndian((long)0x7FFFFFFFFFFFFFFF, destination, out bytesWritten));
            Assert.Equal(8, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<long>.TryWriteLittleEndian(unchecked((long)0x8000000000000000), destination, out bytesWritten));
            Assert.Equal(8, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<long>.TryWriteLittleEndian(unchecked((long)0xFFFFFFFFFFFFFFFF), destination, out bytesWritten));
            Assert.Equal(8, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, destination.ToArray());

            Assert.False(BinaryIntegerHelper<long>.TryWriteLittleEndian(default, Span<byte>.Empty, out bytesWritten));
            Assert.Equal(0, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, destination.ToArray());
        }

        //
        // IBinaryNumber
        //

        [Fact]
        public static void AllBitsSetTest()
        {
            Assert.Equal(unchecked((long)0xFFFF_FFFF_FFFF_FFFF), BinaryNumberHelper<long>.AllBitsSet);
            Assert.Equal(0L, ~BinaryNumberHelper<long>.AllBitsSet);
        }

        [Fact]
        public static void IsPow2Test()
        {
            Assert.False(BinaryNumberHelper<long>.IsPow2((long)0x0000000000000000));
            Assert.True(BinaryNumberHelper<long>.IsPow2((long)0x0000000000000001));
            Assert.False(BinaryNumberHelper<long>.IsPow2((long)0x7FFFFFFFFFFFFFFF));
            Assert.False(BinaryNumberHelper<long>.IsPow2(unchecked((long)0x8000000000000000)));
            Assert.False(BinaryNumberHelper<long>.IsPow2(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void Log2Test()
        {
            Assert.Equal((long)0x0000000000000000, BinaryNumberHelper<long>.Log2((long)0x0000000000000000));
            Assert.Equal((long)0x0000000000000000, BinaryNumberHelper<long>.Log2((long)0x0000000000000001));
            Assert.Equal((long)0x000000000000003E, BinaryNumberHelper<long>.Log2((long)0x7FFFFFFFFFFFFFFF));
            Assert.Throws<ArgumentOutOfRangeException>(() => BinaryNumberHelper<long>.Log2(unchecked((long)0x8000000000000000)));
            Assert.Throws<ArgumentOutOfRangeException>(() => BinaryNumberHelper<long>.Log2(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        //
        // IBitwiseOperators
        //

        [Fact]
        public static void op_BitwiseAndTest()
        {
            Assert.Equal((long)0x0000000000000000, BitwiseOperatorsHelper<long, long, long>.op_BitwiseAnd((long)0x0000000000000000, 1));
            Assert.Equal((long)0x0000000000000001, BitwiseOperatorsHelper<long, long, long>.op_BitwiseAnd((long)0x0000000000000001, 1));
            Assert.Equal((long)0x0000000000000001, BitwiseOperatorsHelper<long, long, long>.op_BitwiseAnd((long)0x7FFFFFFFFFFFFFFF, 1));
            Assert.Equal((long)0x0000000000000000, BitwiseOperatorsHelper<long, long, long>.op_BitwiseAnd(unchecked((long)0x8000000000000000), 1));
            Assert.Equal((long)0x0000000000000001, BitwiseOperatorsHelper<long, long, long>.op_BitwiseAnd(unchecked((long)0xFFFFFFFFFFFFFFFF), 1));
        }

        [Fact]
        public static void op_BitwiseOrTest()
        {
            Assert.Equal((long)0x0000000000000001, BitwiseOperatorsHelper<long, long, long>.op_BitwiseOr((long)0x0000000000000000, 1));
            Assert.Equal((long)0x0000000000000001, BitwiseOperatorsHelper<long, long, long>.op_BitwiseOr((long)0x0000000000000001, 1));
            Assert.Equal((long)0x7FFFFFFFFFFFFFFF, BitwiseOperatorsHelper<long, long, long>.op_BitwiseOr((long)0x7FFFFFFFFFFFFFFF, 1));
            Assert.Equal(unchecked((long)0x8000000000000001), BitwiseOperatorsHelper<long, long, long>.op_BitwiseOr(unchecked((long)0x8000000000000000), 1));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), BitwiseOperatorsHelper<long, long, long>.op_BitwiseOr(unchecked((long)0xFFFFFFFFFFFFFFFF), 1));
        }

        [Fact]
        public static void op_ExclusiveOrTest()
        {
            Assert.Equal((long)0x0000000000000001, BitwiseOperatorsHelper<long, long, long>.op_ExclusiveOr((long)0x0000000000000000, 1));
            Assert.Equal((long)0x0000000000000000, BitwiseOperatorsHelper<long, long, long>.op_ExclusiveOr((long)0x0000000000000001, 1));
            Assert.Equal((long)0x7FFFFFFFFFFFFFFE, BitwiseOperatorsHelper<long, long, long>.op_ExclusiveOr((long)0x7FFFFFFFFFFFFFFF, 1));
            Assert.Equal(unchecked((long)0x8000000000000001), BitwiseOperatorsHelper<long, long, long>.op_ExclusiveOr(unchecked((long)0x8000000000000000), 1));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFE), BitwiseOperatorsHelper<long, long, long>.op_ExclusiveOr(unchecked((long)0xFFFFFFFFFFFFFFFF), 1));
        }

        [Fact]
        public static void op_OnesComplementTest()
        {
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), BitwiseOperatorsHelper<long, long, long>.op_OnesComplement((long)0x0000000000000000));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFE), BitwiseOperatorsHelper<long, long, long>.op_OnesComplement((long)0x0000000000000001));
            Assert.Equal(unchecked((long)0x8000000000000000), BitwiseOperatorsHelper<long, long, long>.op_OnesComplement((long)0x7FFFFFFFFFFFFFFF));
            Assert.Equal((long)0x7FFFFFFFFFFFFFFF, BitwiseOperatorsHelper<long, long, long>.op_OnesComplement(unchecked((long)0x8000000000000000)));
            Assert.Equal((long)0x0000000000000000, BitwiseOperatorsHelper<long, long, long>.op_OnesComplement(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        //
        // IComparisonOperators
        //

        [Fact]
        public static void op_GreaterThanTest()
        {
            Assert.False(ComparisonOperatorsHelper<long, long, bool>.op_GreaterThan((long)0x0000000000000000, 1));
            Assert.False(ComparisonOperatorsHelper<long, long, bool>.op_GreaterThan((long)0x0000000000000001, 1));
            Assert.True(ComparisonOperatorsHelper<long, long, bool>.op_GreaterThan((long)0x7FFFFFFFFFFFFFFF, 1));
            Assert.False(ComparisonOperatorsHelper<long, long, bool>.op_GreaterThan(unchecked((long)0x8000000000000000), 1));
            Assert.False(ComparisonOperatorsHelper<long, long, bool>.op_GreaterThan(unchecked((long)0xFFFFFFFFFFFFFFFF), 1));
        }

        [Fact]
        public static void op_GreaterThanOrEqualTest()
        {
            Assert.False(ComparisonOperatorsHelper<long, long, bool>.op_GreaterThanOrEqual((long)0x0000000000000000, 1));
            Assert.True(ComparisonOperatorsHelper<long, long, bool>.op_GreaterThanOrEqual((long)0x0000000000000001, 1));
            Assert.True(ComparisonOperatorsHelper<long, long, bool>.op_GreaterThanOrEqual((long)0x7FFFFFFFFFFFFFFF, 1));
            Assert.False(ComparisonOperatorsHelper<long, long, bool>.op_GreaterThanOrEqual(unchecked((long)0x8000000000000000), 1));
            Assert.False(ComparisonOperatorsHelper<long, long, bool>.op_GreaterThanOrEqual(unchecked((long)0xFFFFFFFFFFFFFFFF), 1));
        }

        [Fact]
        public static void op_LessThanTest()
        {
            Assert.True(ComparisonOperatorsHelper<long, long, bool>.op_LessThan((long)0x0000000000000000, 1));
            Assert.False(ComparisonOperatorsHelper<long, long, bool>.op_LessThan((long)0x0000000000000001, 1));
            Assert.False(ComparisonOperatorsHelper<long, long, bool>.op_LessThan((long)0x7FFFFFFFFFFFFFFF, 1));
            Assert.True(ComparisonOperatorsHelper<long, long, bool>.op_LessThan(unchecked((long)0x8000000000000000), 1));
            Assert.True(ComparisonOperatorsHelper<long, long, bool>.op_LessThan(unchecked((long)0xFFFFFFFFFFFFFFFF), 1));
        }

        [Fact]
        public static void op_LessThanOrEqualTest()
        {
            Assert.True(ComparisonOperatorsHelper<long, long, bool>.op_LessThanOrEqual((long)0x0000000000000000, 1));
            Assert.True(ComparisonOperatorsHelper<long, long, bool>.op_LessThanOrEqual((long)0x0000000000000001, 1));
            Assert.False(ComparisonOperatorsHelper<long, long, bool>.op_LessThanOrEqual((long)0x7FFFFFFFFFFFFFFF, 1));
            Assert.True(ComparisonOperatorsHelper<long, long, bool>.op_LessThanOrEqual(unchecked((long)0x8000000000000000), 1));
            Assert.True(ComparisonOperatorsHelper<long, long, bool>.op_LessThanOrEqual(unchecked((long)0xFFFFFFFFFFFFFFFF), 1));
        }

        //
        // IDecrementOperators
        //

        [Fact]
        public static void op_DecrementTest()
        {
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), DecrementOperatorsHelper<long>.op_Decrement((long)0x0000000000000000));
            Assert.Equal((long)0x0000000000000000, DecrementOperatorsHelper<long>.op_Decrement((long)0x0000000000000001));
            Assert.Equal((long)0x7FFFFFFFFFFFFFFE, DecrementOperatorsHelper<long>.op_Decrement((long)0x7FFFFFFFFFFFFFFF));
            Assert.Equal((long)0x7FFFFFFFFFFFFFFF, DecrementOperatorsHelper<long>.op_Decrement(unchecked((long)0x8000000000000000)));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFE), DecrementOperatorsHelper<long>.op_Decrement(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void op_CheckedDecrementTest()
        {
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), DecrementOperatorsHelper<long>.op_CheckedDecrement((long)0x0000000000000000));
            Assert.Equal((long)0x0000000000000000, DecrementOperatorsHelper<long>.op_CheckedDecrement((long)0x0000000000000001));
            Assert.Equal((long)0x7FFFFFFFFFFFFFFE, DecrementOperatorsHelper<long>.op_CheckedDecrement((long)0x7FFFFFFFFFFFFFFF));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFE), DecrementOperatorsHelper<long>.op_CheckedDecrement(unchecked((long)0xFFFFFFFFFFFFFFFF)));

            Assert.Throws<OverflowException>(() => DecrementOperatorsHelper<long>.op_CheckedDecrement(unchecked((long)0x8000000000000000)));
        }

        //
        // IDivisionOperators
        //

        [Fact]
        public static void op_DivisionTest()
        {
            Assert.Equal((long)0x0000000000000000, DivisionOperatorsHelper<long, long, long>.op_Division((long)0x0000000000000000, 2));
            Assert.Equal((long)0x0000000000000000, DivisionOperatorsHelper<long, long, long>.op_Division((long)0x0000000000000001, 2));
            Assert.Equal((long)0x3FFFFFFFFFFFFFFF, DivisionOperatorsHelper<long, long, long>.op_Division((long)0x7FFFFFFFFFFFFFFF, 2));
            Assert.Equal(unchecked((long)0xC000000000000000), DivisionOperatorsHelper<long, long, long>.op_Division(unchecked((long)0x8000000000000000), 2));
            Assert.Equal((long)0x0000000000000000, DivisionOperatorsHelper<long, long, long>.op_Division(unchecked((long)0xFFFFFFFFFFFFFFFF), 2));

            Assert.Throws<DivideByZeroException>(() => DivisionOperatorsHelper<long, long, long>.op_Division((long)0x0000000000000000, 0));
            Assert.Throws<DivideByZeroException>(() => DivisionOperatorsHelper<long, long, long>.op_Division((long)0x0000000000000001, 0));
            Assert.Throws<DivideByZeroException>(() => DivisionOperatorsHelper<long, long, long>.op_Division(unchecked((long)0xFFFFFFFFFFFFFFFF), 0));
        }

        [Fact]
        public static void op_CheckedDivisionTest()
        {
            Assert.Equal((long)0x0000000000000000, DivisionOperatorsHelper<long, long, long>.op_CheckedDivision((long)0x0000000000000000, 2));
            Assert.Equal((long)0x0000000000000000, DivisionOperatorsHelper<long, long, long>.op_CheckedDivision((long)0x0000000000000001, 2));
            Assert.Equal((long)0x3FFFFFFFFFFFFFFF, DivisionOperatorsHelper<long, long, long>.op_CheckedDivision((long)0x7FFFFFFFFFFFFFFF, 2));
            Assert.Equal(unchecked((long)0xC000000000000000), DivisionOperatorsHelper<long, long, long>.op_CheckedDivision(unchecked((long)0x8000000000000000), 2));
            Assert.Equal((long)0x0000000000000000, DivisionOperatorsHelper<long, long, long>.op_CheckedDivision(unchecked((long)0xFFFFFFFFFFFFFFFF), 2));

            Assert.Throws<DivideByZeroException>(() => DivisionOperatorsHelper<long, long, long>.op_CheckedDivision((long)0x0000000000000000, 0));
            Assert.Throws<DivideByZeroException>(() => DivisionOperatorsHelper<long, long, long>.op_CheckedDivision((long)0x0000000000000001, 0));
            Assert.Throws<DivideByZeroException>(() => DivisionOperatorsHelper<long, long, long>.op_CheckedDivision(unchecked((long)0xFFFFFFFFFFFFFFFF), 0));
        }

        //
        // IEqualityOperators
        //

        [Fact]
        public static void op_EqualityTest()
        {
            Assert.False(EqualityOperatorsHelper<long, long, bool>.op_Equality((long)0x0000000000000000, 1));
            Assert.True(EqualityOperatorsHelper<long, long, bool>.op_Equality((long)0x0000000000000001, 1));
            Assert.False(EqualityOperatorsHelper<long, long, bool>.op_Equality((long)0x7FFFFFFFFFFFFFFF, 1));
            Assert.False(EqualityOperatorsHelper<long, long, bool>.op_Equality(unchecked((long)0x8000000000000000), 1));
            Assert.False(EqualityOperatorsHelper<long, long, bool>.op_Equality(unchecked((long)0xFFFFFFFFFFFFFFFF), 1));
        }

        [Fact]
        public static void op_InequalityTest()
        {
            Assert.True(EqualityOperatorsHelper<long, long, bool>.op_Inequality((long)0x0000000000000000, 1));
            Assert.False(EqualityOperatorsHelper<long, long, bool>.op_Inequality((long)0x0000000000000001, 1));
            Assert.True(EqualityOperatorsHelper<long, long, bool>.op_Inequality((long)0x7FFFFFFFFFFFFFFF, 1));
            Assert.True(EqualityOperatorsHelper<long, long, bool>.op_Inequality(unchecked((long)0x8000000000000000), 1));
            Assert.True(EqualityOperatorsHelper<long, long, bool>.op_Inequality(unchecked((long)0xFFFFFFFFFFFFFFFF), 1));
        }

        //
        // IIncrementOperators
        //

        [Fact]
        public static void op_IncrementTest()
        {
            Assert.Equal((long)0x0000000000000001, IncrementOperatorsHelper<long>.op_Increment((long)0x0000000000000000));
            Assert.Equal((long)0x0000000000000002, IncrementOperatorsHelper<long>.op_Increment((long)0x0000000000000001));
            Assert.Equal(unchecked((long)0x8000000000000000), IncrementOperatorsHelper<long>.op_Increment((long)0x7FFFFFFFFFFFFFFF));
            Assert.Equal(unchecked((long)0x8000000000000001), IncrementOperatorsHelper<long>.op_Increment(unchecked((long)0x8000000000000000)));
            Assert.Equal((long)0x0000000000000000, IncrementOperatorsHelper<long>.op_Increment(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void op_CheckedIncrementTest()
        {
            Assert.Equal((long)0x0000000000000001, IncrementOperatorsHelper<long>.op_CheckedIncrement((long)0x0000000000000000));
            Assert.Equal((long)0x0000000000000002, IncrementOperatorsHelper<long>.op_CheckedIncrement((long)0x0000000000000001));
            Assert.Equal(unchecked((long)0x8000000000000001), IncrementOperatorsHelper<long>.op_CheckedIncrement(unchecked((long)0x8000000000000000)));
            Assert.Equal((long)0x0000000000000000, IncrementOperatorsHelper<long>.op_CheckedIncrement(unchecked((long)0xFFFFFFFFFFFFFFFF)));

            Assert.Throws<OverflowException>(() => IncrementOperatorsHelper<long>.op_CheckedIncrement((long)0x7FFFFFFFFFFFFFFF));
        }

        //
        // IMinMaxValue
        //

        [Fact]
        public static void MaxValueTest()
        {
            Assert.Equal((long)0x7FFFFFFFFFFFFFFF, MinMaxValueHelper<long>.MaxValue);
        }

        [Fact]
        public static void MinValueTest()
        {
            Assert.Equal(unchecked((long)0x8000000000000000), MinMaxValueHelper<long>.MinValue);
        }

        //
        // IModulusOperators
        //

        [Fact]
        public static void op_ModulusTest()
        {
            Assert.Equal((long)0x0000000000000000, ModulusOperatorsHelper<long, long, long>.op_Modulus((long)0x0000000000000000, 2));
            Assert.Equal((long)0x0000000000000001, ModulusOperatorsHelper<long, long, long>.op_Modulus((long)0x0000000000000001, 2));
            Assert.Equal((long)0x0000000000000001, ModulusOperatorsHelper<long, long, long>.op_Modulus((long)0x7FFFFFFFFFFFFFFF, 2));
            Assert.Equal((long)0x0000000000000000, ModulusOperatorsHelper<long, long, long>.op_Modulus(unchecked((long)0x8000000000000000), 2));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), ModulusOperatorsHelper<long, long, long>.op_Modulus(unchecked((long)0xFFFFFFFFFFFFFFFF), 2));

            Assert.Throws<DivideByZeroException>(() => ModulusOperatorsHelper<long, long, long>.op_Modulus((long)0x0000000000000001, 0));
        }

        //
        // IMultiplicativeIdentity
        //

        [Fact]
        public static void MultiplicativeIdentityTest()
        {
            Assert.Equal((long)0x0000000000000001, MultiplicativeIdentityHelper<long, long>.MultiplicativeIdentity);
        }

        //
        // IMultiplyOperators
        //

        [Fact]
        public static void op_MultiplyTest()
        {
            Assert.Equal((long)0x0000000000000000, MultiplyOperatorsHelper<long, long, long>.op_Multiply((long)0x0000000000000000, 2));
            Assert.Equal((long)0x0000000000000002, MultiplyOperatorsHelper<long, long, long>.op_Multiply((long)0x0000000000000001, 2));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFE), MultiplyOperatorsHelper<long, long, long>.op_Multiply((long)0x7FFFFFFFFFFFFFFF, 2));
            Assert.Equal((long)0x0000000000000000, MultiplyOperatorsHelper<long, long, long>.op_Multiply(unchecked((long)0x8000000000000000), 2));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFE), MultiplyOperatorsHelper<long, long, long>.op_Multiply(unchecked((long)0xFFFFFFFFFFFFFFFF), 2));
        }

        [Fact]
        public static void op_CheckedMultiplyTest()
        {
            Assert.Equal((long)0x0000000000000000, MultiplyOperatorsHelper<long, long, long>.op_CheckedMultiply((long)0x0000000000000000, 2));
            Assert.Equal((long)0x0000000000000002, MultiplyOperatorsHelper<long, long, long>.op_CheckedMultiply((long)0x0000000000000001, 2));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFE), MultiplyOperatorsHelper<long, long, long>.op_CheckedMultiply(unchecked((long)0xFFFFFFFFFFFFFFFF), 2));

            Assert.Throws<OverflowException>(() => MultiplyOperatorsHelper<long, long, long>.op_CheckedMultiply((long)0x7FFFFFFFFFFFFFFF, 2));
            Assert.Throws<OverflowException>(() => MultiplyOperatorsHelper<long, long, long>.op_CheckedMultiply(unchecked((long)0x8000000000000000), 2));
        }

        //
        // INumber
        //

        [Fact]
        public static void ClampTest()
        {
            Assert.Equal((long)0x0000000000000000, NumberHelper<long>.Clamp((long)0x0000000000000000, unchecked((long)0xFFFFFFFFFFFFFFC0), 0x003F));
            Assert.Equal((long)0x0000000000000001, NumberHelper<long>.Clamp((long)0x0000000000000001, unchecked((long)0xFFFFFFFFFFFFFFC0), 0x003F));
            Assert.Equal((long)0x000000000000003F, NumberHelper<long>.Clamp((long)0x7FFFFFFFFFFFFFFF, unchecked((long)0xFFFFFFFFFFFFFFC0), 0x003F));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFC0), NumberHelper<long>.Clamp(unchecked((long)0x8000000000000000), unchecked((long)0xFFFFFFFFFFFFFFC0), 0x003F));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), NumberHelper<long>.Clamp(unchecked((long)0xFFFFFFFFFFFFFFFF), unchecked((long)0xFFFFFFFFFFFFFFC0), 0x003F));
        }

        [Fact]
        public static void MaxTest()
        {
            Assert.Equal((long)0x0000000000000001, NumberHelper<long>.Max((long)0x0000000000000000, 1));
            Assert.Equal((long)0x0000000000000001, NumberHelper<long>.Max((long)0x0000000000000001, 1));
            Assert.Equal((long)0x7FFFFFFFFFFFFFFF, NumberHelper<long>.Max((long)0x7FFFFFFFFFFFFFFF, 1));
            Assert.Equal((long)0x0000000000000001, NumberHelper<long>.Max(unchecked((long)0x8000000000000000), 1));
            Assert.Equal((long)0x0000000000000001, NumberHelper<long>.Max(unchecked((long)0xFFFFFFFFFFFFFFFF), 1));
        }

        [Fact]
        public static void MaxNumberTest()
        {
            Assert.Equal((long)0x0000000000000001, NumberHelper<long>.MaxNumber((long)0x0000000000000000, 1));
            Assert.Equal((long)0x0000000000000001, NumberHelper<long>.MaxNumber((long)0x0000000000000001, 1));
            Assert.Equal((long)0x7FFFFFFFFFFFFFFF, NumberHelper<long>.MaxNumber((long)0x7FFFFFFFFFFFFFFF, 1));
            Assert.Equal((long)0x0000000000000001, NumberHelper<long>.MaxNumber(unchecked((long)0x8000000000000000), 1));
            Assert.Equal((long)0x0000000000000001, NumberHelper<long>.MaxNumber(unchecked((long)0xFFFFFFFFFFFFFFFF), 1));
        }

        [Fact]
        public static void MinTest()
        {
            Assert.Equal((long)0x0000000000000000, NumberHelper<long>.Min((long)0x0000000000000000, 1));
            Assert.Equal((long)0x0000000000000001, NumberHelper<long>.Min((long)0x0000000000000001, 1));
            Assert.Equal((long)0x0000000000000001, NumberHelper<long>.Min((long)0x7FFFFFFFFFFFFFFF, 1));
            Assert.Equal(unchecked((long)0x8000000000000000), NumberHelper<long>.Min(unchecked((long)0x8000000000000000), 1));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), NumberHelper<long>.Min(unchecked((long)0xFFFFFFFFFFFFFFFF), 1));
        }

        [Fact]
        public static void MinNumberTest()
        {
            Assert.Equal((long)0x0000000000000000, NumberHelper<long>.MinNumber((long)0x0000000000000000, 1));
            Assert.Equal((long)0x0000000000000001, NumberHelper<long>.MinNumber((long)0x0000000000000001, 1));
            Assert.Equal((long)0x0000000000000001, NumberHelper<long>.MinNumber((long)0x7FFFFFFFFFFFFFFF, 1));
            Assert.Equal(unchecked((long)0x8000000000000000), NumberHelper<long>.MinNumber(unchecked((long)0x8000000000000000), 1));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), NumberHelper<long>.MinNumber(unchecked((long)0xFFFFFFFFFFFFFFFF), 1));
        }

        [Fact]
        public static void SignTest()
        {
            Assert.Equal(0, NumberHelper<long>.Sign((long)0x0000000000000000));
            Assert.Equal(1, NumberHelper<long>.Sign((long)0x0000000000000001));
            Assert.Equal(1, NumberHelper<long>.Sign((long)0x7FFFFFFFFFFFFFFF));
            Assert.Equal(-1, NumberHelper<long>.Sign(unchecked((long)0x8000000000000000)));
            Assert.Equal(-1, NumberHelper<long>.Sign(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        //
        // INumberBase
        //

        [Fact]
        public static void OneTest()
        {
            Assert.Equal((long)0x0000000000000001, NumberBaseHelper<long>.One);
        }

        [Fact]
        public static void RadixTest()
        {
            Assert.Equal(2, NumberBaseHelper<long>.Radix);
        }

        [Fact]
        public static void ZeroTest()
        {
            Assert.Equal((long)0x0000000000000000, NumberBaseHelper<long>.Zero);
        }

        [Fact]
        public static void AbsTest()
        {
            Assert.Equal((long)0x0000000000000000, NumberBaseHelper<long>.Abs((long)0x0000000000000000));
            Assert.Equal((long)0x0000000000000001, NumberBaseHelper<long>.Abs((long)0x0000000000000001));
            Assert.Equal((long)0x7FFFFFFFFFFFFFFF, NumberBaseHelper<long>.Abs((long)0x7FFFFFFFFFFFFFFF));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<long>.Abs(unchecked((long)0x8000000000000000)));
            Assert.Equal((long)0x0000000000000001, NumberBaseHelper<long>.Abs(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromByteTest()
        {
            Assert.Equal((long)0x0000000000000000, NumberBaseHelper<long>.CreateChecked<byte>(0x00));
            Assert.Equal((long)0x0000000000000001, NumberBaseHelper<long>.CreateChecked<byte>(0x01));
            Assert.Equal((long)0x000000000000007F, NumberBaseHelper<long>.CreateChecked<byte>(0x7F));
            Assert.Equal((long)0x0000000000000080, NumberBaseHelper<long>.CreateChecked<byte>(0x80));
            Assert.Equal((long)0x00000000000000FF, NumberBaseHelper<long>.CreateChecked<byte>(0xFF));
        }

        [Fact]
        public static void CreateCheckedFromCharTest()
        {
            Assert.Equal((long)0x0000000000000000, NumberBaseHelper<long>.CreateChecked<char>((char)0x0000));
            Assert.Equal((long)0x0000000000000001, NumberBaseHelper<long>.CreateChecked<char>((char)0x0001));
            Assert.Equal((long)0x0000000000007FFF, NumberBaseHelper<long>.CreateChecked<char>((char)0x7FFF));
            Assert.Equal((long)0x0000000000008000, NumberBaseHelper<long>.CreateChecked<char>((char)0x8000));
            Assert.Equal((long)0x000000000000FFFF, NumberBaseHelper<long>.CreateChecked<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateCheckedFromDecimalTest()
        {
            Assert.Equal((long)0x0000_0000_0000_0000, NumberBaseHelper<long>.CreateChecked<decimal>(-0.0m));
            Assert.Equal((long)0x0000_0000_0000_0000, NumberBaseHelper<long>.CreateChecked<decimal>(+0.0m));
            Assert.Equal((long)0x0000_0000_0000_0001, NumberBaseHelper<long>.CreateChecked<decimal>(+1.0m));

            Assert.Equal(unchecked((long)0xFFFF_FFFF_FFFF_FFFF), NumberBaseHelper<long>.CreateChecked<decimal>(-1.0m));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<long>.CreateChecked<decimal>(decimal.MinValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<long>.CreateChecked<decimal>(decimal.MaxValue));
        }

        [Fact]
        public static void CreateCheckedFromDoubleTest()
        {
            Assert.Equal((long)0x0000_0000_0000_0000, NumberBaseHelper<long>.CreateChecked<double>(+0.0));
            Assert.Equal((long)0x0000_0000_0000_0000, NumberBaseHelper<long>.CreateChecked<double>(-0.0));

            Assert.Equal((long)0x0000_0000_0000_0000, NumberBaseHelper<long>.CreateChecked<double>(+double.Epsilon));
            Assert.Equal((long)0x0000_0000_0000_0000, NumberBaseHelper<long>.CreateChecked<double>(-double.Epsilon));

            Assert.Equal((long)0x0000_0000_0000_0001, NumberBaseHelper<long>.CreateChecked<double>(+1.0));
            Assert.Equal(unchecked((long)0xFFFF_FFFF_FFFF_FFFF), NumberBaseHelper<long>.CreateChecked<double>(-1.0));

            Assert.Equal((long)0x7FFF_FFFF_FFFF_FC00, NumberBaseHelper<long>.CreateChecked<double>(+9223372036854774784.0));
            Assert.Equal(unchecked((long)0x8000_0000_0000_0000), NumberBaseHelper<long>.CreateChecked<double>(-9223372036854775808.0));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<long>.CreateChecked<double>(+9223372036854775808.0));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<long>.CreateChecked<double>(-9223372036854777856.0));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<long>.CreateChecked<double>(double.MaxValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<long>.CreateChecked<double>(double.MinValue));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<long>.CreateChecked<double>(double.PositiveInfinity));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<long>.CreateChecked<double>(double.NegativeInfinity));
        }

        [Fact]
        public static void CreateCheckedFromHalfTest()
        {
            Assert.Equal((long)0x0000_0000_0000_0000, NumberBaseHelper<long>.CreateChecked<Half>(Half.Zero));
            Assert.Equal((long)0x0000_0000_0000_0000, NumberBaseHelper<long>.CreateChecked<Half>(Half.NegativeZero));

            Assert.Equal((long)0x0000_0000_0000_0000, NumberBaseHelper<long>.CreateChecked<Half>(+Half.Epsilon));
            Assert.Equal((long)0x0000_0000_0000_0000, NumberBaseHelper<long>.CreateChecked<Half>(-Half.Epsilon));

            Assert.Equal((long)0x0000_0000_0000_0001, NumberBaseHelper<long>.CreateChecked<Half>(Half.One));
            Assert.Equal(unchecked((long)0xFFFF_FFFF_FFFF_FFFF), NumberBaseHelper<long>.CreateChecked<Half>(Half.NegativeOne));

            Assert.Equal((long)0x0000_0000_0000_FFE0, NumberBaseHelper<long>.CreateChecked<Half>(Half.MaxValue));
            Assert.Equal(unchecked((long)0xFFFF_FFFF_FFFF_0020), NumberBaseHelper<long>.CreateChecked<Half>(Half.MinValue));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<long>.CreateChecked<Half>(Half.PositiveInfinity));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<long>.CreateChecked<Half>(Half.NegativeInfinity));
        }

        [Fact]
        public static void CreateCheckedFromInt16Test()
        {
            Assert.Equal((long)0x0000000000000000, NumberBaseHelper<long>.CreateChecked<short>(0x0000));
            Assert.Equal((long)0x0000000000000001, NumberBaseHelper<long>.CreateChecked<short>(0x0001));
            Assert.Equal((long)0x0000000000007FFF, NumberBaseHelper<long>.CreateChecked<short>(0x7FFF));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFF8000), NumberBaseHelper<long>.CreateChecked<short>(unchecked((short)0x8000)));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), NumberBaseHelper<long>.CreateChecked<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromInt32Test()
        {
            Assert.Equal((long)0x0000000000000000, NumberBaseHelper<long>.CreateChecked<int>(0x00000000));
            Assert.Equal((long)0x0000000000000001, NumberBaseHelper<long>.CreateChecked<int>(0x00000001));
            Assert.Equal((long)0x000000007FFFFFFF, NumberBaseHelper<long>.CreateChecked<int>(0x7FFFFFFF));
            Assert.Equal(unchecked((long)0xFFFFFFFF80000000), NumberBaseHelper<long>.CreateChecked<int>(unchecked((int)0x80000000)));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), NumberBaseHelper<long>.CreateChecked<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromInt64Test()
        {
            Assert.Equal((long)0x0000000000000000, NumberBaseHelper<long>.CreateChecked<long>(0x0000000000000000));
            Assert.Equal((long)0x0000000000000001, NumberBaseHelper<long>.CreateChecked<long>(0x0000000000000001));
            Assert.Equal((long)0x7FFFFFFFFFFFFFFF, NumberBaseHelper<long>.CreateChecked<long>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal(unchecked((long)0x8000000000000000), NumberBaseHelper<long>.CreateChecked<long>(unchecked(unchecked((long)0x8000000000000000))));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), NumberBaseHelper<long>.CreateChecked<long>(unchecked(unchecked((long)0xFFFFFFFFFFFFFFFF))));
        }

        [Fact]
        public static void CreateCheckedFromInt128Test()
        {
            Assert.Equal((long)0x0000_0000_0000_0000, NumberBaseHelper<long>.CreateChecked<Int128>(Int128.Zero));
            Assert.Equal((long)0x0000_0000_0000_0001, NumberBaseHelper<long>.CreateChecked<Int128>(Int128.One));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<long>.CreateChecked<Int128>(Int128.MaxValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<long>.CreateChecked<Int128>(Int128.MinValue));
            Assert.Equal(unchecked((long)0xFFFF_FFFF_FFFF_FFFF), NumberBaseHelper<long>.CreateChecked<Int128>(Int128.NegativeOne));
        }

        [Fact]
        public static void CreateCheckedFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((long)0x0000000000000000, NumberBaseHelper<long>.CreateChecked<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal((long)0x0000000000000001, NumberBaseHelper<long>.CreateChecked<nint>(unchecked((nint)0x0000000000000001)));
                Assert.Equal((long)0x7FFFFFFFFFFFFFFF, NumberBaseHelper<long>.CreateChecked<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((long)0x8000000000000000), NumberBaseHelper<long>.CreateChecked<nint>(unchecked((nint)0x8000000000000000)));
                Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), NumberBaseHelper<long>.CreateChecked<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((long)0x0000000000000000, NumberBaseHelper<long>.CreateChecked<nint>((nint)0x00000000));
                Assert.Equal((long)0x0000000000000001, NumberBaseHelper<long>.CreateChecked<nint>((nint)0x00000001));
                Assert.Equal((long)0x000000007FFFFFFF, NumberBaseHelper<long>.CreateChecked<nint>((nint)0x7FFFFFFF));
                Assert.Equal(unchecked((long)0xFFFFFFFF80000000), NumberBaseHelper<long>.CreateChecked<nint>(unchecked((nint)0x80000000)));
                Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), NumberBaseHelper<long>.CreateChecked<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateCheckedFromNFloatTest()
        {
            Assert.Equal((long)0x0000_0000_0000_0000, NumberBaseHelper<long>.CreateChecked<NFloat>(+0.0f));
            Assert.Equal((long)0x0000_0000_0000_0000, NumberBaseHelper<long>.CreateChecked<NFloat>(-0.0f));

            Assert.Equal((long)0x0000_0000_0000_0000, NumberBaseHelper<long>.CreateChecked<NFloat>(+NFloat.Epsilon));
            Assert.Equal((long)0x0000_0000_0000_0000, NumberBaseHelper<long>.CreateChecked<NFloat>(-NFloat.Epsilon));

            Assert.Equal((long)0x0000_0000_0000_0001, NumberBaseHelper<long>.CreateChecked<NFloat>(+1.0f));
            Assert.Equal(unchecked((long)0xFFFF_FFFF_FFFF_FFFF), NumberBaseHelper<long>.CreateChecked<NFloat>(-1.0f));

            if (Environment.Is64BitProcess)
            {
                Assert.Equal((long)0x7FFF_FFFF_FFFF_FC00, NumberBaseHelper<long>.CreateChecked<NFloat>((NFloat)(+9223372036854774784.0)));
                Assert.Equal(unchecked((long)0x8000_0000_0000_0000), NumberBaseHelper<long>.CreateChecked<NFloat>((NFloat)(-9223372036854775808.0)));

                Assert.Throws<OverflowException>(() => NumberBaseHelper<long>.CreateChecked<NFloat>((NFloat)(+9223372036854775808.0)));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<long>.CreateChecked<NFloat>((NFloat)(-9223372036854777856.0)));
            }
            else
            {
                Assert.Equal((long)0x7FFF_FF80_0000_0000, NumberBaseHelper<long>.CreateChecked<float>(+9223371487098961920.0f));
                Assert.Equal(unchecked((long)0x8000_0000_0000_0000), NumberBaseHelper<long>.CreateChecked<float>(-9223372036854775808.0f));

                Assert.Throws<OverflowException>(() => NumberBaseHelper<long>.CreateChecked<float>(+9223372036854775808.0f));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<long>.CreateChecked<float>(-9223373136366403584.0f));
            }

            Assert.Throws<OverflowException>(() => NumberBaseHelper<long>.CreateChecked<NFloat>(NFloat.MaxValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<long>.CreateChecked<NFloat>(NFloat.MinValue));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<long>.CreateChecked<NFloat>(NFloat.PositiveInfinity));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<long>.CreateChecked<NFloat>(NFloat.NegativeInfinity));
        }

        [Fact]
        public static void CreateCheckedFromSByteTest()
        {
            Assert.Equal((long)0x0000000000000000, NumberBaseHelper<long>.CreateChecked<sbyte>(0x00));
            Assert.Equal((long)0x0000000000000001, NumberBaseHelper<long>.CreateChecked<sbyte>(0x01));
            Assert.Equal((long)0x000000000000007F, NumberBaseHelper<long>.CreateChecked<sbyte>(0x7F));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFF80), NumberBaseHelper<long>.CreateChecked<sbyte>(unchecked((sbyte)0x80)));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), NumberBaseHelper<long>.CreateChecked<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateCheckedFromSingleTest()
        {
            Assert.Equal((long)0x0000_0000_0000_0000, NumberBaseHelper<long>.CreateChecked<float>(+0.0f));
            Assert.Equal((long)0x0000_0000_0000_0000, NumberBaseHelper<long>.CreateChecked<float>(-0.0f));

            Assert.Equal((long)0x0000_0000_0000_0000, NumberBaseHelper<long>.CreateChecked<float>(+float.Epsilon));
            Assert.Equal((long)0x0000_0000_0000_0000, NumberBaseHelper<long>.CreateChecked<float>(-float.Epsilon));

            Assert.Equal((long)0x0000_0000_0000_0001, NumberBaseHelper<long>.CreateChecked<float>(+1.0f));
            Assert.Equal(unchecked((long)0xFFFF_FFFF_FFFF_FFFF), NumberBaseHelper<long>.CreateChecked<float>(-1.0f));

            Assert.Equal((long)0x7FFF_FF80_0000_0000, NumberBaseHelper<long>.CreateChecked<float>(+9223371487098961920.0f));
            Assert.Equal(unchecked((long)0x8000_0000_0000_0000), NumberBaseHelper<long>.CreateChecked<float>(-9223372036854775808.0f));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<long>.CreateChecked<float>(+9223372036854775808.0f));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<long>.CreateChecked<float>(-9223373136366403584.0f));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<long>.CreateChecked<float>(float.MaxValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<long>.CreateChecked<float>(float.MinValue));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<long>.CreateChecked<float>(float.PositiveInfinity));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<long>.CreateChecked<float>(float.NegativeInfinity));
        }

        [Fact]
        public static void CreateCheckedFromUInt16Test()
        {
            Assert.Equal((long)0x0000000000000000, NumberBaseHelper<long>.CreateChecked<ushort>(0x0000));
            Assert.Equal((long)0x0000000000000001, NumberBaseHelper<long>.CreateChecked<ushort>(0x0001));
            Assert.Equal((long)0x0000000000007FFF, NumberBaseHelper<long>.CreateChecked<ushort>(0x7FFF));
            Assert.Equal((long)0x0000000000008000, NumberBaseHelper<long>.CreateChecked<ushort>(0x8000));
            Assert.Equal((long)0x000000000000FFFF, NumberBaseHelper<long>.CreateChecked<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUInt32Test()
        {
            Assert.Equal((long)0x0000000000000000, NumberBaseHelper<long>.CreateChecked<uint>(0x00000000));
            Assert.Equal((long)0x0000000000000001, NumberBaseHelper<long>.CreateChecked<uint>(0x00000001));
            Assert.Equal((long)0x000000007FFFFFFF, NumberBaseHelper<long>.CreateChecked<uint>(0x7FFFFFFF));
            Assert.Equal((long)0x0000000080000000, NumberBaseHelper<long>.CreateChecked<uint>(0x80000000));
            Assert.Equal((long)0x00000000FFFFFFFF, NumberBaseHelper<long>.CreateChecked<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUInt64Test()
        {
            Assert.Equal((long)0x0000000000000000, NumberBaseHelper<long>.CreateChecked<ulong>(0x0000000000000000));
            Assert.Equal((long)0x0000000000000001, NumberBaseHelper<long>.CreateChecked<ulong>(0x0000000000000001));
            Assert.Equal((long)0x7FFFFFFFFFFFFFFF, NumberBaseHelper<long>.CreateChecked<ulong>(0x7FFFFFFFFFFFFFFF));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<long>.CreateChecked<ulong>(0x8000000000000000));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<long>.CreateChecked<ulong>(0xFFFFFFFFFFFFFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUInt128Test()
        {
            Assert.Equal((long)0x0000_0000_0000_0000, NumberBaseHelper<long>.CreateChecked<UInt128>(UInt128.Zero));
            Assert.Equal((long)0x0000_0000_0000_0001, NumberBaseHelper<long>.CreateChecked<UInt128>(UInt128.One));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<long>.CreateChecked<UInt128>(UInt128Tests_GenericMath.Int128MaxValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<long>.CreateChecked<UInt128>(UInt128Tests_GenericMath.Int128MaxValuePlusOne));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<long>.CreateChecked<UInt128>(UInt128.MaxValue));
        }

        [Fact]
        public static void CreateCheckedFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((long)0x0000000000000000, NumberBaseHelper<long>.CreateChecked<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal((long)0x0000000000000001, NumberBaseHelper<long>.CreateChecked<nuint>(unchecked((nuint)0x0000000000000001)));
                Assert.Equal((long)0x7FFFFFFFFFFFFFFF, NumberBaseHelper<long>.CreateChecked<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<long>.CreateChecked<nuint>(unchecked((nuint)0x8000000000000000)));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<long>.CreateChecked<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((long)0x0000000000000000, NumberBaseHelper<long>.CreateChecked<nuint>((nuint)0x00000000));
                Assert.Equal((long)0x0000000000000001, NumberBaseHelper<long>.CreateChecked<nuint>((nuint)0x00000001));
                Assert.Equal((long)0x000000007FFFFFFF, NumberBaseHelper<long>.CreateChecked<nuint>((nuint)0x7FFFFFFF));
                Assert.Equal((long)0x0000000080000000, NumberBaseHelper<long>.CreateChecked<nuint>((nuint)0x80000000));
                Assert.Equal((long)0x00000000FFFFFFFF, NumberBaseHelper<long>.CreateChecked<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateSaturatingFromByteTest()
        {
            Assert.Equal((long)0x0000000000000000, NumberBaseHelper<long>.CreateSaturating<byte>(0x00));
            Assert.Equal((long)0x0000000000000001, NumberBaseHelper<long>.CreateSaturating<byte>(0x01));
            Assert.Equal((long)0x000000000000007F, NumberBaseHelper<long>.CreateSaturating<byte>(0x7F));
            Assert.Equal((long)0x0000000000000080, NumberBaseHelper<long>.CreateSaturating<byte>(0x80));
            Assert.Equal((long)0x00000000000000FF, NumberBaseHelper<long>.CreateSaturating<byte>(0xFF));
        }

        [Fact]
        public static void CreateSaturatingFromCharTest()
        {
            Assert.Equal((long)0x0000000000000000, NumberBaseHelper<long>.CreateSaturating<char>((char)0x0000));
            Assert.Equal((long)0x0000000000000001, NumberBaseHelper<long>.CreateSaturating<char>((char)0x0001));
            Assert.Equal((long)0x0000000000007FFF, NumberBaseHelper<long>.CreateSaturating<char>((char)0x7FFF));
            Assert.Equal((long)0x0000000000008000, NumberBaseHelper<long>.CreateSaturating<char>((char)0x8000));
            Assert.Equal((long)0x000000000000FFFF, NumberBaseHelper<long>.CreateSaturating<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromDecimalTest()
        {
            Assert.Equal((long)0x0000_0000_0000_0000, NumberBaseHelper<long>.CreateSaturating<decimal>(-0.0m));
            Assert.Equal((long)0x0000_0000_0000_0000, NumberBaseHelper<long>.CreateSaturating<decimal>(+0.0m));
            Assert.Equal((long)0x0000_0000_0000_0001, NumberBaseHelper<long>.CreateSaturating<decimal>(+1.0m));

            Assert.Equal(unchecked((long)0xFFFF_FFFF_FFFF_FFFF), NumberBaseHelper<long>.CreateSaturating<decimal>(-1.0m));

            Assert.Equal(unchecked((long)0x8000_0000_0000_0000), NumberBaseHelper<long>.CreateSaturating<decimal>(decimal.MinValue));
            Assert.Equal(unchecked((long)0x7FFF_FFFF_FFFF_FFFF), NumberBaseHelper<long>.CreateSaturating<decimal>(decimal.MaxValue));
        }

        [Fact]
        public static void CreateSaturatingFromDoubleTest()
        {
            Assert.Equal((long)0x0000_0000_0000_0000, NumberBaseHelper<long>.CreateSaturating<double>(+0.0));
            Assert.Equal((long)0x0000_0000_0000_0000, NumberBaseHelper<long>.CreateSaturating<double>(-0.0));

            Assert.Equal((long)0x0000_0000_0000_0000, NumberBaseHelper<long>.CreateSaturating<double>(+double.Epsilon));
            Assert.Equal((long)0x0000_0000_0000_0000, NumberBaseHelper<long>.CreateSaturating<double>(-double.Epsilon));

            Assert.Equal((long)0x0000_0000_0000_0001, NumberBaseHelper<long>.CreateSaturating<double>(+1.0));
            Assert.Equal(unchecked((long)0xFFFF_FFFF_FFFF_FFFF), NumberBaseHelper<long>.CreateSaturating<double>(-1.0));

            Assert.Equal((long)0x7FFF_FFFF_FFFF_FC00, NumberBaseHelper<long>.CreateSaturating<double>(+9223372036854774784.0));
            Assert.Equal(unchecked((long)0x8000_0000_0000_0000), NumberBaseHelper<long>.CreateSaturating<double>(-9223372036854775808.0));

            Assert.Equal(unchecked((long)0x7FFF_FFFF_FFFF_FFFF), NumberBaseHelper<long>.CreateSaturating<double>(+9223372036854775808.0));
            Assert.Equal(unchecked((long)0x8000_0000_0000_0000), NumberBaseHelper<long>.CreateSaturating<double>(-9223372036854777856.0));

            Assert.Equal(unchecked((long)0x7FFF_FFFF_FFFF_FFFF), NumberBaseHelper<long>.CreateSaturating<double>(double.MaxValue));
            Assert.Equal(unchecked((long)0x8000_0000_0000_0000), NumberBaseHelper<long>.CreateSaturating<double>(double.MinValue));

            Assert.Equal(unchecked((long)0x7FFF_FFFF_FFFF_FFFF), NumberBaseHelper<long>.CreateSaturating<double>(double.PositiveInfinity));
            Assert.Equal(unchecked((long)0x8000_0000_0000_0000), NumberBaseHelper<long>.CreateSaturating<double>(double.NegativeInfinity));
        }

        [Fact]
        public static void CreateSaturatingFromHalfTest()
        {
            Assert.Equal((long)0x0000_0000_0000_0000, NumberBaseHelper<long>.CreateSaturating<Half>(Half.Zero));
            Assert.Equal((long)0x0000_0000_0000_0000, NumberBaseHelper<long>.CreateSaturating<Half>(Half.NegativeZero));

            Assert.Equal((long)0x0000_0000_0000_0000, NumberBaseHelper<long>.CreateSaturating<Half>(+Half.Epsilon));
            Assert.Equal((long)0x0000_0000_0000_0000, NumberBaseHelper<long>.CreateSaturating<Half>(-Half.Epsilon));

            Assert.Equal((long)0x0000_0000_0000_0001, NumberBaseHelper<long>.CreateSaturating<Half>(Half.One));
            Assert.Equal(unchecked((long)0xFFFF_FFFF_FFFF_FFFF), NumberBaseHelper<long>.CreateSaturating<Half>(Half.NegativeOne));

            Assert.Equal((long)0x0000_0000_0000_FFE0, NumberBaseHelper<long>.CreateSaturating<Half>(Half.MaxValue));
            Assert.Equal(unchecked((long)0xFFFF_FFFF_FFFF_0020), NumberBaseHelper<long>.CreateSaturating<Half>(Half.MinValue));

            Assert.Equal(unchecked((long)0x7FFF_FFFF_FFFF_FFFF), NumberBaseHelper<long>.CreateSaturating<Half>(Half.PositiveInfinity));
            Assert.Equal(unchecked((long)0x8000_0000_0000_0000), NumberBaseHelper<long>.CreateSaturating<Half>(Half.NegativeInfinity));
        }

        [Fact]
        public static void CreateSaturatingFromInt16Test()
        {
            Assert.Equal((long)0x0000000000000000, NumberBaseHelper<long>.CreateSaturating<short>(0x0000));
            Assert.Equal((long)0x0000000000000001, NumberBaseHelper<long>.CreateSaturating<short>(0x0001));
            Assert.Equal((long)0x0000000000007FFF, NumberBaseHelper<long>.CreateSaturating<short>(0x7FFF));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFF8000), NumberBaseHelper<long>.CreateSaturating<short>(unchecked((short)0x8000)));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), NumberBaseHelper<long>.CreateSaturating<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromInt32Test()
        {
            Assert.Equal((long)0x0000000000000000, NumberBaseHelper<long>.CreateSaturating<int>(0x00000000));
            Assert.Equal((long)0x0000000000000001, NumberBaseHelper<long>.CreateSaturating<int>(0x00000001));
            Assert.Equal((long)0x000000007FFFFFFF, NumberBaseHelper<long>.CreateSaturating<int>(0x7FFFFFFF));
            Assert.Equal(unchecked((long)0xFFFFFFFF80000000), NumberBaseHelper<long>.CreateSaturating<int>(unchecked((int)0x80000000)));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), NumberBaseHelper<long>.CreateSaturating<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromInt64Test()
        {
            Assert.Equal((long)0x0000000000000000, NumberBaseHelper<long>.CreateSaturating<long>(0x0000000000000000));
            Assert.Equal((long)0x0000000000000001, NumberBaseHelper<long>.CreateSaturating<long>(0x0000000000000001));
            Assert.Equal((long)0x7FFFFFFFFFFFFFFF, NumberBaseHelper<long>.CreateSaturating<long>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal(unchecked((long)0x8000000000000000), NumberBaseHelper<long>.CreateSaturating<long>(unchecked(unchecked((long)0x8000000000000000))));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), NumberBaseHelper<long>.CreateSaturating<long>(unchecked(unchecked((long)0xFFFFFFFFFFFFFFFF))));
        }

        [Fact]
        public static void CreateSaturatingFromInt128Test()
        {
            Assert.Equal((long)0x0000_0000_0000_0000, NumberBaseHelper<long>.CreateSaturating<Int128>(Int128.Zero));
            Assert.Equal((long)0x0000_0000_0000_0001, NumberBaseHelper<long>.CreateSaturating<Int128>(Int128.One));
            Assert.Equal(unchecked((long)0x7FFF_FFFF_FFFF_FFFF), NumberBaseHelper<long>.CreateSaturating<Int128>(Int128.MaxValue));
            Assert.Equal(unchecked((long)0x8000_0000_0000_0000), NumberBaseHelper<long>.CreateSaturating<Int128>(Int128.MinValue));
            Assert.Equal(unchecked((long)0xFFFF_FFFF_FFFF_FFFF), NumberBaseHelper<long>.CreateSaturating<Int128>(Int128.NegativeOne));
        }

        [Fact]
        public static void CreateSaturatingFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((long)0x0000000000000000, NumberBaseHelper<long>.CreateSaturating<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal((long)0x0000000000000001, NumberBaseHelper<long>.CreateSaturating<nint>(unchecked((nint)0x0000000000000001)));
                Assert.Equal((long)0x7FFFFFFFFFFFFFFF, NumberBaseHelper<long>.CreateSaturating<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((long)0x8000000000000000), NumberBaseHelper<long>.CreateSaturating<nint>(unchecked((nint)0x8000000000000000)));
                Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), NumberBaseHelper<long>.CreateSaturating<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((long)0x0000000000000000, NumberBaseHelper<long>.CreateSaturating<nint>((nint)0x00000000));
                Assert.Equal((long)0x0000000000000001, NumberBaseHelper<long>.CreateSaturating<nint>((nint)0x00000001));
                Assert.Equal((long)0x000000007FFFFFFF, NumberBaseHelper<long>.CreateSaturating<nint>((nint)0x7FFFFFFF));
                Assert.Equal(unchecked((long)0xFFFFFFFF80000000), NumberBaseHelper<long>.CreateSaturating<nint>(unchecked((nint)0x80000000)));
                Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), NumberBaseHelper<long>.CreateSaturating<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateSaturatingFromNFloatTest()
        {
            Assert.Equal((long)0x0000_0000_0000_0000, NumberBaseHelper<long>.CreateSaturating<NFloat>(+0.0f));
            Assert.Equal((long)0x0000_0000_0000_0000, NumberBaseHelper<long>.CreateSaturating<NFloat>(-0.0f));

            Assert.Equal((long)0x0000_0000_0000_0000, NumberBaseHelper<long>.CreateSaturating<NFloat>(+NFloat.Epsilon));
            Assert.Equal((long)0x0000_0000_0000_0000, NumberBaseHelper<long>.CreateSaturating<NFloat>(-NFloat.Epsilon));

            Assert.Equal((long)0x0000_0000_0000_0001, NumberBaseHelper<long>.CreateSaturating<NFloat>(+1.0f));
            Assert.Equal(unchecked((long)0xFFFF_FFFF_FFFF_FFFF), NumberBaseHelper<long>.CreateSaturating<NFloat>(-1.0f));

            if (Environment.Is64BitProcess)
            {
                Assert.Equal((long)0x7FFF_FFFF_FFFF_FC00, NumberBaseHelper<long>.CreateSaturating<NFloat>((NFloat)(+9223372036854774784.0)));
                Assert.Equal(unchecked((long)0x8000_0000_0000_0000), NumberBaseHelper<long>.CreateSaturating<NFloat>((NFloat)(-9223372036854775808.0)));

                Assert.Equal(unchecked((long)0x7FFF_FFFF_FFFF_FFFF), NumberBaseHelper<long>.CreateSaturating<NFloat>((NFloat)(+9223372036854775808.0)));
                Assert.Equal(unchecked((long)0x8000_0000_0000_0000), NumberBaseHelper<long>.CreateSaturating<NFloat>((NFloat)(-9223372036854777856.0)));
            }
            else
            {
                Assert.Equal((long)0x7FFF_FF80_0000_0000, NumberBaseHelper<long>.CreateSaturating<float>(+9223371487098961920.0f));
                Assert.Equal(unchecked((long)0x8000_0000_0000_0000), NumberBaseHelper<long>.CreateSaturating<float>(-9223372036854775808.0f));

                Assert.Equal(unchecked((long)0x7FFF_FFFF_FFFF_FFFF), NumberBaseHelper<long>.CreateSaturating<float>(+9223372036854775808.0f));
                Assert.Equal(unchecked((long)0x8000_0000_0000_0000), NumberBaseHelper<long>.CreateSaturating<float>(-9223373136366403584.0f));
            }

            Assert.Equal(unchecked((long)0x7FFF_FFFF_FFFF_FFFF), NumberBaseHelper<long>.CreateSaturating<NFloat>(NFloat.MaxValue));
            Assert.Equal(unchecked((long)0x8000_0000_0000_0000), NumberBaseHelper<long>.CreateSaturating<NFloat>(NFloat.MinValue));

            Assert.Equal(unchecked((long)0x7FFF_FFFF_FFFF_FFFF), NumberBaseHelper<long>.CreateSaturating<NFloat>(NFloat.PositiveInfinity));
            Assert.Equal(unchecked((long)0x8000_0000_0000_0000), NumberBaseHelper<long>.CreateSaturating<NFloat>(NFloat.NegativeInfinity));
        }

        [Fact]
        public static void CreateSaturatingFromSByteTest()
        {
            Assert.Equal((long)0x0000000000000000, NumberBaseHelper<long>.CreateSaturating<sbyte>(0x00));
            Assert.Equal((long)0x0000000000000001, NumberBaseHelper<long>.CreateSaturating<sbyte>(0x01));
            Assert.Equal((long)0x000000000000007F, NumberBaseHelper<long>.CreateSaturating<sbyte>(0x7F));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFF80), NumberBaseHelper<long>.CreateSaturating<sbyte>(unchecked((sbyte)0x80)));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), NumberBaseHelper<long>.CreateSaturating<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateSaturatingFromSingleTest()
        {
            Assert.Equal((long)0x0000_0000_0000_0000, NumberBaseHelper<long>.CreateSaturating<float>(+0.0f));
            Assert.Equal((long)0x0000_0000_0000_0000, NumberBaseHelper<long>.CreateSaturating<float>(-0.0f));

            Assert.Equal((long)0x0000_0000_0000_0000, NumberBaseHelper<long>.CreateSaturating<float>(+float.Epsilon));
            Assert.Equal((long)0x0000_0000_0000_0000, NumberBaseHelper<long>.CreateSaturating<float>(-float.Epsilon));

            Assert.Equal((long)0x0000_0000_0000_0001, NumberBaseHelper<long>.CreateSaturating<float>(+1.0f));
            Assert.Equal(unchecked((long)0xFFFF_FFFF_FFFF_FFFF), NumberBaseHelper<long>.CreateSaturating<float>(-1.0f));

            Assert.Equal((long)0x7FFF_FF80_0000_0000, NumberBaseHelper<long>.CreateSaturating<float>(+9223371487098961920.0f));
            Assert.Equal(unchecked((long)0x8000_0000_0000_0000), NumberBaseHelper<long>.CreateSaturating<float>(-9223372036854775808.0f));

            Assert.Equal(unchecked((long)0x7FFF_FFFF_FFFF_FFFF), NumberBaseHelper<long>.CreateSaturating<float>(+9223372036854775808.0f));
            Assert.Equal(unchecked((long)0x8000_0000_0000_0000), NumberBaseHelper<long>.CreateSaturating<float>(-9223373136366403584.0f));

            Assert.Equal(unchecked((long)0x7FFF_FFFF_FFFF_FFFF), NumberBaseHelper<long>.CreateSaturating<float>(float.MaxValue));
            Assert.Equal(unchecked((long)0x8000_0000_0000_0000), NumberBaseHelper<long>.CreateSaturating<float>(float.MinValue));

            Assert.Equal(unchecked((long)0x7FFF_FFFF_FFFF_FFFF), NumberBaseHelper<long>.CreateSaturating<float>(float.PositiveInfinity));
            Assert.Equal(unchecked((long)0x8000_0000_0000_0000), NumberBaseHelper<long>.CreateSaturating<float>(float.NegativeInfinity));
        }

        [Fact]
        public static void CreateSaturatingFromUInt16Test()
        {
            Assert.Equal((long)0x0000000000000000, NumberBaseHelper<long>.CreateSaturating<ushort>(0x0000));
            Assert.Equal((long)0x0000000000000001, NumberBaseHelper<long>.CreateSaturating<ushort>(0x0001));
            Assert.Equal((long)0x0000000000007FFF, NumberBaseHelper<long>.CreateSaturating<ushort>(0x7FFF));
            Assert.Equal((long)0x0000000000008000, NumberBaseHelper<long>.CreateSaturating<ushort>(0x8000));
            Assert.Equal((long)0x000000000000FFFF, NumberBaseHelper<long>.CreateSaturating<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUInt32Test()
        {
            Assert.Equal((long)0x0000000000000000, NumberBaseHelper<long>.CreateSaturating<uint>(0x00000000));
            Assert.Equal((long)0x0000000000000001, NumberBaseHelper<long>.CreateSaturating<uint>(0x00000001));
            Assert.Equal((long)0x000000007FFFFFFF, NumberBaseHelper<long>.CreateSaturating<uint>(0x7FFFFFFF));
            Assert.Equal((long)0x0000000080000000, NumberBaseHelper<long>.CreateSaturating<uint>(0x80000000));
            Assert.Equal((long)0x00000000FFFFFFFF, NumberBaseHelper<long>.CreateSaturating<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUInt64Test()
        {
            Assert.Equal((long)0x0000000000000000, NumberBaseHelper<long>.CreateSaturating<ulong>(0x0000000000000000));
            Assert.Equal((long)0x0000000000000001, NumberBaseHelper<long>.CreateSaturating<ulong>(0x0000000000000001));
            Assert.Equal((long)0x7FFFFFFFFFFFFFFF, NumberBaseHelper<long>.CreateSaturating<ulong>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal((long)0x7FFFFFFFFFFFFFFF, NumberBaseHelper<long>.CreateSaturating<ulong>(0x8000000000000000));
            Assert.Equal((long)0x7FFFFFFFFFFFFFFF, NumberBaseHelper<long>.CreateSaturating<ulong>(0xFFFFFFFFFFFFFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUInt128Test()
        {
            Assert.Equal((long)0x0000_0000_0000_0000, NumberBaseHelper<long>.CreateSaturating<UInt128>(UInt128.Zero));
            Assert.Equal((long)0x0000_0000_0000_0001, NumberBaseHelper<long>.CreateSaturating<UInt128>(UInt128.One));
            Assert.Equal((long)0x7FFF_FFFF_FFFF_FFFF, NumberBaseHelper<long>.CreateSaturating<UInt128>(UInt128Tests_GenericMath.Int128MaxValue));
            Assert.Equal((long)0x7FFF_FFFF_FFFF_FFFF, NumberBaseHelper<long>.CreateSaturating<UInt128>(UInt128Tests_GenericMath.Int128MaxValuePlusOne));
            Assert.Equal((long)0x7FFF_FFFF_FFFF_FFFF, NumberBaseHelper<long>.CreateSaturating<UInt128>(UInt128.MaxValue));
        }

        [Fact]
        public static void CreateSaturatingFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((long)0x0000000000000000, NumberBaseHelper<long>.CreateSaturating<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal((long)0x0000000000000001, NumberBaseHelper<long>.CreateSaturating<nuint>(unchecked((nuint)0x0000000000000001)));
                Assert.Equal((long)0x7FFFFFFFFFFFFFFF, NumberBaseHelper<long>.CreateSaturating<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal((long)0x7FFFFFFFFFFFFFFF, NumberBaseHelper<long>.CreateSaturating<nuint>(unchecked((nuint)0x8000000000000000)));
                Assert.Equal((long)0x7FFFFFFFFFFFFFFF, NumberBaseHelper<long>.CreateSaturating<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((long)0x0000000000000000, NumberBaseHelper<long>.CreateSaturating<nuint>((nuint)0x00000000));
                Assert.Equal((long)0x0000000000000001, NumberBaseHelper<long>.CreateSaturating<nuint>((nuint)0x00000001));
                Assert.Equal((long)0x000000007FFFFFFF, NumberBaseHelper<long>.CreateSaturating<nuint>((nuint)0x7FFFFFFF));
                Assert.Equal((long)0x0000000080000000, NumberBaseHelper<long>.CreateSaturating<nuint>((nuint)0x80000000));
                Assert.Equal((long)0x00000000FFFFFFFF, NumberBaseHelper<long>.CreateSaturating<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateTruncatingFromByteTest()
        {
            Assert.Equal((long)0x0000000000000000, NumberBaseHelper<long>.CreateTruncating<byte>(0x00));
            Assert.Equal((long)0x0000000000000001, NumberBaseHelper<long>.CreateTruncating<byte>(0x01));
            Assert.Equal((long)0x000000000000007F, NumberBaseHelper<long>.CreateTruncating<byte>(0x7F));
            Assert.Equal((long)0x0000000000000080, NumberBaseHelper<long>.CreateTruncating<byte>(0x80));
            Assert.Equal((long)0x00000000000000FF, NumberBaseHelper<long>.CreateTruncating<byte>(0xFF));
        }

        [Fact]
        public static void CreateTruncatingFromCharTest()
        {
            Assert.Equal((long)0x0000000000000000, NumberBaseHelper<long>.CreateTruncating<char>((char)0x0000));
            Assert.Equal((long)0x0000000000000001, NumberBaseHelper<long>.CreateTruncating<char>((char)0x0001));
            Assert.Equal((long)0x0000000000007FFF, NumberBaseHelper<long>.CreateTruncating<char>((char)0x7FFF));
            Assert.Equal((long)0x0000000000008000, NumberBaseHelper<long>.CreateTruncating<char>((char)0x8000));
            Assert.Equal((long)0x000000000000FFFF, NumberBaseHelper<long>.CreateTruncating<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromDecimalTest()
        {
            Assert.Equal((long)0x0000_0000_0000_0000, NumberBaseHelper<long>.CreateTruncating<decimal>(-0.0m));
            Assert.Equal((long)0x0000_0000_0000_0000, NumberBaseHelper<long>.CreateTruncating<decimal>(+0.0m));
            Assert.Equal((long)0x0000_0000_0000_0001, NumberBaseHelper<long>.CreateTruncating<decimal>(+1.0m));

            Assert.Equal(unchecked((long)0xFFFF_FFFF_FFFF_FFFF), NumberBaseHelper<long>.CreateTruncating<decimal>(-1.0m));

            Assert.Equal(unchecked((long)0x8000_0000_0000_0000), NumberBaseHelper<long>.CreateTruncating<decimal>(decimal.MinValue));
            Assert.Equal(unchecked((long)0x7FFF_FFFF_FFFF_FFFF), NumberBaseHelper<long>.CreateTruncating<decimal>(decimal.MaxValue));
        }

        [Fact]
        public static void CreateTruncatingFromDoubleTest()
        {
            Assert.Equal((long)0x0000_0000_0000_0000, NumberBaseHelper<long>.CreateTruncating<double>(+0.0));
            Assert.Equal((long)0x0000_0000_0000_0000, NumberBaseHelper<long>.CreateTruncating<double>(-0.0));

            Assert.Equal((long)0x0000_0000_0000_0000, NumberBaseHelper<long>.CreateTruncating<double>(+double.Epsilon));
            Assert.Equal((long)0x0000_0000_0000_0000, NumberBaseHelper<long>.CreateTruncating<double>(-double.Epsilon));

            Assert.Equal((long)0x0000_0000_0000_0001, NumberBaseHelper<long>.CreateTruncating<double>(+1.0));
            Assert.Equal(unchecked((long)0xFFFF_FFFF_FFFF_FFFF), NumberBaseHelper<long>.CreateTruncating<double>(-1.0));

            Assert.Equal((long)0x7FFF_FFFF_FFFF_FC00, NumberBaseHelper<long>.CreateTruncating<double>(+9223372036854774784.0));
            Assert.Equal(unchecked((long)0x8000_0000_0000_0000), NumberBaseHelper<long>.CreateTruncating<double>(-9223372036854775808.0));

            Assert.Equal(unchecked((long)0x7FFF_FFFF_FFFF_FFFF), NumberBaseHelper<long>.CreateTruncating<double>(+9223372036854775808.0));
            Assert.Equal(unchecked((long)0x8000_0000_0000_0000), NumberBaseHelper<long>.CreateTruncating<double>(-9223372036854777856.0));

            Assert.Equal(unchecked((long)0x7FFF_FFFF_FFFF_FFFF), NumberBaseHelper<long>.CreateTruncating<double>(double.MaxValue));
            Assert.Equal(unchecked((long)0x8000_0000_0000_0000), NumberBaseHelper<long>.CreateTruncating<double>(double.MinValue));

            Assert.Equal(unchecked((long)0x7FFF_FFFF_FFFF_FFFF), NumberBaseHelper<long>.CreateTruncating<double>(double.PositiveInfinity));
            Assert.Equal(unchecked((long)0x8000_0000_0000_0000), NumberBaseHelper<long>.CreateTruncating<double>(double.NegativeInfinity));
        }

        [Fact]
        public static void CreateTruncatingFromHalfTest()
        {
            Assert.Equal((long)0x0000_0000_0000_0000, NumberBaseHelper<long>.CreateTruncating<Half>(Half.Zero));
            Assert.Equal((long)0x0000_0000_0000_0000, NumberBaseHelper<long>.CreateTruncating<Half>(Half.NegativeZero));

            Assert.Equal((long)0x0000_0000_0000_0000, NumberBaseHelper<long>.CreateTruncating<Half>(+Half.Epsilon));
            Assert.Equal((long)0x0000_0000_0000_0000, NumberBaseHelper<long>.CreateTruncating<Half>(-Half.Epsilon));

            Assert.Equal((long)0x0000_0000_0000_0001, NumberBaseHelper<long>.CreateTruncating<Half>(Half.One));
            Assert.Equal(unchecked((long)0xFFFF_FFFF_FFFF_FFFF), NumberBaseHelper<long>.CreateTruncating<Half>(Half.NegativeOne));

            Assert.Equal((long)0x0000_0000_0000_FFE0, NumberBaseHelper<long>.CreateTruncating<Half>(Half.MaxValue));
            Assert.Equal(unchecked((long)0xFFFF_FFFF_FFFF_0020), NumberBaseHelper<long>.CreateTruncating<Half>(Half.MinValue));

            Assert.Equal(unchecked((long)0x7FFF_FFFF_FFFF_FFFF), NumberBaseHelper<long>.CreateTruncating<Half>(Half.PositiveInfinity));
            Assert.Equal(unchecked((long)0x8000_0000_0000_0000), NumberBaseHelper<long>.CreateTruncating<Half>(Half.NegativeInfinity));
        }

        [Fact]
        public static void CreateTruncatingFromInt16Test()
        {
            Assert.Equal((long)0x0000000000000000, NumberBaseHelper<long>.CreateTruncating<short>(0x0000));
            Assert.Equal((long)0x0000000000000001, NumberBaseHelper<long>.CreateTruncating<short>(0x0001));
            Assert.Equal((long)0x0000000000007FFF, NumberBaseHelper<long>.CreateTruncating<short>(0x7FFF));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFF8000), NumberBaseHelper<long>.CreateTruncating<short>(unchecked((short)0x8000)));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), NumberBaseHelper<long>.CreateTruncating<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateTruncatingFromInt32Test()
        {
            Assert.Equal((long)0x0000000000000000, NumberBaseHelper<long>.CreateTruncating<int>(0x00000000));
            Assert.Equal((long)0x0000000000000001, NumberBaseHelper<long>.CreateTruncating<int>(0x00000001));
            Assert.Equal((long)0x000000007FFFFFFF, NumberBaseHelper<long>.CreateTruncating<int>(0x7FFFFFFF));
            Assert.Equal(unchecked((long)0xFFFFFFFF80000000), NumberBaseHelper<long>.CreateTruncating<int>(unchecked((int)0x80000000)));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), NumberBaseHelper<long>.CreateTruncating<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateTruncatingFromInt64Test()
        {
            Assert.Equal((long)0x0000000000000000, NumberBaseHelper<long>.CreateTruncating<long>(0x0000000000000000));
            Assert.Equal((long)0x0000000000000001, NumberBaseHelper<long>.CreateTruncating<long>(0x0000000000000001));
            Assert.Equal((long)0x7FFFFFFFFFFFFFFF, NumberBaseHelper<long>.CreateTruncating<long>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal(unchecked((long)0x8000000000000000), NumberBaseHelper<long>.CreateTruncating<long>(unchecked(unchecked((long)0x8000000000000000))));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), NumberBaseHelper<long>.CreateTruncating<long>(unchecked(unchecked((long)0xFFFFFFFFFFFFFFFF))));
        }

        [Fact]
        public static void CreateTruncatingFromInt128Test()
        {
            Assert.Equal(unchecked((long)0x0000_0000_0000_0000), NumberBaseHelper<long>.CreateTruncating<Int128>(Int128.Zero));
            Assert.Equal(unchecked((long)0x0000_0000_0000_0001), NumberBaseHelper<long>.CreateTruncating<Int128>(Int128.One));
            Assert.Equal(unchecked((long)0xFFFF_FFFF_FFFF_FFFF), NumberBaseHelper<long>.CreateTruncating<Int128>(Int128.MaxValue));
            Assert.Equal(unchecked((long)0x0000_0000_0000_0000), NumberBaseHelper<long>.CreateTruncating<Int128>(Int128.MinValue));
            Assert.Equal(unchecked((long)0xFFFF_FFFF_FFFF_FFFF), NumberBaseHelper<long>.CreateTruncating<Int128>(Int128.NegativeOne));
        }

        [Fact]
        public static void CreateTruncatingFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((long)0x0000000000000000, NumberBaseHelper<long>.CreateTruncating<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal((long)0x0000000000000001, NumberBaseHelper<long>.CreateTruncating<nint>(unchecked((nint)0x0000000000000001)));
                Assert.Equal((long)0x7FFFFFFFFFFFFFFF, NumberBaseHelper<long>.CreateTruncating<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((long)0x8000000000000000), NumberBaseHelper<long>.CreateTruncating<nint>(unchecked((nint)0x8000000000000000)));
                Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), NumberBaseHelper<long>.CreateTruncating<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((long)0x0000000000000000, NumberBaseHelper<long>.CreateTruncating<nint>((nint)0x00000000));
                Assert.Equal((long)0x0000000000000001, NumberBaseHelper<long>.CreateTruncating<nint>((nint)0x00000001));
                Assert.Equal((long)0x000000007FFFFFFF, NumberBaseHelper<long>.CreateTruncating<nint>((nint)0x7FFFFFFF));
                Assert.Equal(unchecked((long)0xFFFFFFFF80000000), NumberBaseHelper<long>.CreateTruncating<nint>(unchecked((nint)0x80000000)));
                Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), NumberBaseHelper<long>.CreateTruncating<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateTruncatingFromNFloatTest()
        {
            Assert.Equal((long)0x0000_0000_0000_0000, NumberBaseHelper<long>.CreateTruncating<NFloat>(+0.0f));
            Assert.Equal((long)0x0000_0000_0000_0000, NumberBaseHelper<long>.CreateTruncating<NFloat>(-0.0f));

            Assert.Equal((long)0x0000_0000_0000_0000, NumberBaseHelper<long>.CreateTruncating<NFloat>(+NFloat.Epsilon));
            Assert.Equal((long)0x0000_0000_0000_0000, NumberBaseHelper<long>.CreateTruncating<NFloat>(-NFloat.Epsilon));

            Assert.Equal((long)0x0000_0000_0000_0001, NumberBaseHelper<long>.CreateTruncating<NFloat>(+1.0f));
            Assert.Equal(unchecked((long)0xFFFF_FFFF_FFFF_FFFF), NumberBaseHelper<long>.CreateTruncating<NFloat>(-1.0f));

            if (Environment.Is64BitProcess)
            {
                Assert.Equal((long)0x7FFF_FFFF_FFFF_FC00, NumberBaseHelper<long>.CreateTruncating<NFloat>((NFloat)(+9223372036854774784.0)));
                Assert.Equal(unchecked((long)0x8000_0000_0000_0000), NumberBaseHelper<long>.CreateTruncating<NFloat>((NFloat)(-9223372036854775808.0)));

                Assert.Equal(unchecked((long)0x7FFF_FFFF_FFFF_FFFF), NumberBaseHelper<long>.CreateTruncating<NFloat>((NFloat)(+9223372036854775808.0)));
                Assert.Equal(unchecked((long)0x8000_0000_0000_0000), NumberBaseHelper<long>.CreateTruncating<NFloat>((NFloat)(-9223372036854777856.0)));
            }
            else
            {
                Assert.Equal((long)0x7FFF_FF80_0000_0000, NumberBaseHelper<long>.CreateTruncating<float>(+9223371487098961920.0f));
                Assert.Equal(unchecked((long)0x8000_0000_0000_0000), NumberBaseHelper<long>.CreateTruncating<float>(-9223372036854775808.0f));

                Assert.Equal(unchecked((long)0x7FFF_FFFF_FFFF_FFFF), NumberBaseHelper<long>.CreateTruncating<float>(+9223372036854775808.0f));
                Assert.Equal(unchecked((long)0x8000_0000_0000_0000), NumberBaseHelper<long>.CreateTruncating<float>(-9223373136366403584.0f));
            }

            Assert.Equal(unchecked((long)0x7FFF_FFFF_FFFF_FFFF), NumberBaseHelper<long>.CreateTruncating<NFloat>(NFloat.MaxValue));
            Assert.Equal(unchecked((long)0x8000_0000_0000_0000), NumberBaseHelper<long>.CreateTruncating<NFloat>(NFloat.MinValue));

            Assert.Equal(unchecked((long)0x7FFF_FFFF_FFFF_FFFF), NumberBaseHelper<long>.CreateTruncating<NFloat>(NFloat.PositiveInfinity));
            Assert.Equal(unchecked((long)0x8000_0000_0000_0000), NumberBaseHelper<long>.CreateTruncating<NFloat>(NFloat.NegativeInfinity));
        }

        [Fact]
        public static void CreateTruncatingFromSByteTest()
        {
            Assert.Equal((long)0x0000000000000000, NumberBaseHelper<long>.CreateTruncating<sbyte>(0x00));
            Assert.Equal((long)0x0000000000000001, NumberBaseHelper<long>.CreateTruncating<sbyte>(0x01));
            Assert.Equal((long)0x000000000000007F, NumberBaseHelper<long>.CreateTruncating<sbyte>(0x7F));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFF80), NumberBaseHelper<long>.CreateTruncating<sbyte>(unchecked((sbyte)0x80)));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), NumberBaseHelper<long>.CreateTruncating<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateTruncatingFromSingleTest()
        {
            Assert.Equal((long)0x0000_0000_0000_0000, NumberBaseHelper<long>.CreateTruncating<float>(+0.0f));
            Assert.Equal((long)0x0000_0000_0000_0000, NumberBaseHelper<long>.CreateTruncating<float>(-0.0f));

            Assert.Equal((long)0x0000_0000_0000_0000, NumberBaseHelper<long>.CreateTruncating<float>(+float.Epsilon));
            Assert.Equal((long)0x0000_0000_0000_0000, NumberBaseHelper<long>.CreateTruncating<float>(-float.Epsilon));

            Assert.Equal((long)0x0000_0000_0000_0001, NumberBaseHelper<long>.CreateTruncating<float>(+1.0f));
            Assert.Equal(unchecked((long)0xFFFF_FFFF_FFFF_FFFF), NumberBaseHelper<long>.CreateTruncating<float>(-1.0f));

            Assert.Equal((long)0x7FFF_FF80_0000_0000, NumberBaseHelper<long>.CreateTruncating<float>(+9223371487098961920.0f));
            Assert.Equal(unchecked((long)0x8000_0000_0000_0000), NumberBaseHelper<long>.CreateTruncating<float>(-9223372036854775808.0f));

            Assert.Equal(unchecked((long)0x7FFF_FFFF_FFFF_FFFF), NumberBaseHelper<long>.CreateTruncating<float>(+9223372036854775808.0f));
            Assert.Equal(unchecked((long)0x8000_0000_0000_0000), NumberBaseHelper<long>.CreateTruncating<float>(-9223373136366403584.0f));

            Assert.Equal(unchecked((long)0x7FFF_FFFF_FFFF_FFFF), NumberBaseHelper<long>.CreateTruncating<float>(float.MaxValue));
            Assert.Equal(unchecked((long)0x8000_0000_0000_0000), NumberBaseHelper<long>.CreateTruncating<float>(float.MinValue));

            Assert.Equal(unchecked((long)0x7FFF_FFFF_FFFF_FFFF), NumberBaseHelper<long>.CreateTruncating<float>(float.PositiveInfinity));
            Assert.Equal(unchecked((long)0x8000_0000_0000_0000), NumberBaseHelper<long>.CreateTruncating<float>(float.NegativeInfinity));
        }

        [Fact]
        public static void CreateTruncatingFromUInt16Test()
        {
            Assert.Equal((long)0x0000000000000000, NumberBaseHelper<long>.CreateTruncating<ushort>(0x0000));
            Assert.Equal((long)0x0000000000000001, NumberBaseHelper<long>.CreateTruncating<ushort>(0x0001));
            Assert.Equal((long)0x0000000000007FFF, NumberBaseHelper<long>.CreateTruncating<ushort>(0x7FFF));
            Assert.Equal((long)0x0000000000008000, NumberBaseHelper<long>.CreateTruncating<ushort>(0x8000));
            Assert.Equal((long)0x000000000000FFFF, NumberBaseHelper<long>.CreateTruncating<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt32Test()
        {
            Assert.Equal((long)0x0000000000000000, NumberBaseHelper<long>.CreateTruncating<uint>(0x00000000));
            Assert.Equal((long)0x0000000000000001, NumberBaseHelper<long>.CreateTruncating<uint>(0x00000001));
            Assert.Equal((long)0x000000007FFFFFFF, NumberBaseHelper<long>.CreateTruncating<uint>(0x7FFFFFFF));
            Assert.Equal((long)0x0000000080000000, NumberBaseHelper<long>.CreateTruncating<uint>(0x80000000));
            Assert.Equal((long)0x00000000FFFFFFFF, NumberBaseHelper<long>.CreateTruncating<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt64Test()
        {
            Assert.Equal((long)0x0000000000000000, NumberBaseHelper<long>.CreateTruncating<ulong>(0x0000000000000000));
            Assert.Equal((long)0x0000000000000001, NumberBaseHelper<long>.CreateTruncating<ulong>(0x0000000000000001));
            Assert.Equal((long)0x7FFFFFFFFFFFFFFF, NumberBaseHelper<long>.CreateTruncating<ulong>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal(unchecked((long)0x8000000000000000), NumberBaseHelper<long>.CreateTruncating<ulong>(0x8000000000000000));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), NumberBaseHelper<long>.CreateTruncating<ulong>(0xFFFFFFFFFFFFFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt128Test()
        {
            Assert.Equal(unchecked((long)0x0000_0000_0000_0000), NumberBaseHelper<long>.CreateTruncating<UInt128>(UInt128.Zero));
            Assert.Equal(unchecked((long)0x0000_0000_0000_0001), NumberBaseHelper<long>.CreateTruncating<UInt128>(UInt128.One));
            Assert.Equal(unchecked((long)0xFFFF_FFFF_FFFF_FFFF), NumberBaseHelper<long>.CreateTruncating<UInt128>(UInt128Tests_GenericMath.Int128MaxValue));
            Assert.Equal(unchecked((long)0x0000_0000_0000_0000), NumberBaseHelper<long>.CreateTruncating<UInt128>(UInt128Tests_GenericMath.Int128MaxValuePlusOne));
            Assert.Equal(unchecked((long)0xFFFF_FFFF_FFFF_FFFF), NumberBaseHelper<long>.CreateTruncating<UInt128>(UInt128.MaxValue));
        }

        [Fact]
        public static void CreateTruncatingFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((long)0x0000000000000000, NumberBaseHelper<long>.CreateTruncating<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal((long)0x0000000000000001, NumberBaseHelper<long>.CreateTruncating<nuint>(unchecked((nuint)0x0000000000000001)));
                Assert.Equal((long)0x7FFFFFFFFFFFFFFF, NumberBaseHelper<long>.CreateTruncating<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((long)0x8000000000000000), NumberBaseHelper<long>.CreateTruncating<nuint>(unchecked((nuint)0x8000000000000000)));
                Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), NumberBaseHelper<long>.CreateTruncating<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((long)0x0000000000000000, NumberBaseHelper<long>.CreateTruncating<nuint>((nuint)0x00000000));
                Assert.Equal((long)0x0000000000000001, NumberBaseHelper<long>.CreateTruncating<nuint>((nuint)0x00000001));
                Assert.Equal((long)0x000000007FFFFFFF, NumberBaseHelper<long>.CreateTruncating<nuint>((nuint)0x7FFFFFFF));
                Assert.Equal((long)0x0000000080000000, NumberBaseHelper<long>.CreateTruncating<nuint>((nuint)0x80000000));
                Assert.Equal((long)0x00000000FFFFFFFF, NumberBaseHelper<long>.CreateTruncating<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void IsCanonicalTest()
        {
            Assert.True(NumberBaseHelper<long>.IsCanonical((long)0x0000000000000000));
            Assert.True(NumberBaseHelper<long>.IsCanonical((long)0x0000000000000001));
            Assert.True(NumberBaseHelper<long>.IsCanonical((long)0x7FFFFFFFFFFFFFFF));
            Assert.True(NumberBaseHelper<long>.IsCanonical(unchecked((long)0x8000000000000000)));
            Assert.True(NumberBaseHelper<long>.IsCanonical(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void IsComplexNumberTest()
        {
            Assert.False(NumberBaseHelper<long>.IsComplexNumber((long)0x0000000000000000));
            Assert.False(NumberBaseHelper<long>.IsComplexNumber((long)0x0000000000000001));
            Assert.False(NumberBaseHelper<long>.IsComplexNumber((long)0x7FFFFFFFFFFFFFFF));
            Assert.False(NumberBaseHelper<long>.IsComplexNumber(unchecked((long)0x8000000000000000)));
            Assert.False(NumberBaseHelper<long>.IsComplexNumber(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void IsEvenIntegerTest()
        {
            Assert.True(NumberBaseHelper<long>.IsEvenInteger((long)0x0000000000000000));
            Assert.False(NumberBaseHelper<long>.IsEvenInteger((long)0x0000000000000001));
            Assert.False(NumberBaseHelper<long>.IsEvenInteger((long)0x7FFFFFFFFFFFFFFF));
            Assert.True(NumberBaseHelper<long>.IsEvenInteger(unchecked((long)0x8000000000000000)));
            Assert.False(NumberBaseHelper<long>.IsEvenInteger(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void IsFiniteTest()
        {
            Assert.True(NumberBaseHelper<long>.IsFinite((long)0x0000000000000000));
            Assert.True(NumberBaseHelper<long>.IsFinite((long)0x0000000000000001));
            Assert.True(NumberBaseHelper<long>.IsFinite((long)0x7FFFFFFFFFFFFFFF));
            Assert.True(NumberBaseHelper<long>.IsFinite(unchecked((long)0x8000000000000000)));
            Assert.True(NumberBaseHelper<long>.IsFinite(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void IsImaginaryNumberTest()
        {
            Assert.False(NumberBaseHelper<long>.IsImaginaryNumber((long)0x0000000000000000));
            Assert.False(NumberBaseHelper<long>.IsImaginaryNumber((long)0x0000000000000001));
            Assert.False(NumberBaseHelper<long>.IsImaginaryNumber((long)0x7FFFFFFFFFFFFFFF));
            Assert.False(NumberBaseHelper<long>.IsImaginaryNumber(unchecked((long)0x8000000000000000)));
            Assert.False(NumberBaseHelper<long>.IsImaginaryNumber(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void IsInfinityTest()
        {
            Assert.False(NumberBaseHelper<long>.IsInfinity((long)0x0000000000000000));
            Assert.False(NumberBaseHelper<long>.IsInfinity((long)0x0000000000000001));
            Assert.False(NumberBaseHelper<long>.IsInfinity((long)0x7FFFFFFFFFFFFFFF));
            Assert.False(NumberBaseHelper<long>.IsInfinity(unchecked((long)0x8000000000000000)));
            Assert.False(NumberBaseHelper<long>.IsInfinity(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void IsIntegerTest()
        {
            Assert.True(NumberBaseHelper<long>.IsInteger((long)0x0000000000000000));
            Assert.True(NumberBaseHelper<long>.IsInteger((long)0x0000000000000001));
            Assert.True(NumberBaseHelper<long>.IsInteger((long)0x7FFFFFFFFFFFFFFF));
            Assert.True(NumberBaseHelper<long>.IsInteger(unchecked((long)0x8000000000000000)));
            Assert.True(NumberBaseHelper<long>.IsInteger(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void IsNaNTest()
        {
            Assert.False(NumberBaseHelper<long>.IsNaN((long)0x0000000000000000));
            Assert.False(NumberBaseHelper<long>.IsNaN((long)0x0000000000000001));
            Assert.False(NumberBaseHelper<long>.IsNaN((long)0x7FFFFFFFFFFFFFFF));
            Assert.False(NumberBaseHelper<long>.IsNaN(unchecked((long)0x8000000000000000)));
            Assert.False(NumberBaseHelper<long>.IsNaN(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void IsNegativeTest()
        {
            Assert.False(NumberBaseHelper<long>.IsNegative((long)0x0000000000000000));
            Assert.False(NumberBaseHelper<long>.IsNegative((long)0x0000000000000001));
            Assert.False(NumberBaseHelper<long>.IsNegative((long)0x7FFFFFFFFFFFFFFF));
            Assert.True(NumberBaseHelper<long>.IsNegative(unchecked((long)0x8000000000000000)));
            Assert.True(NumberBaseHelper<long>.IsNegative(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void IsNegativeInfinityTest()
        {
            Assert.False(NumberBaseHelper<long>.IsNegativeInfinity((long)0x0000000000000000));
            Assert.False(NumberBaseHelper<long>.IsNegativeInfinity((long)0x0000000000000001));
            Assert.False(NumberBaseHelper<long>.IsNegativeInfinity((long)0x7FFFFFFFFFFFFFFF));
            Assert.False(NumberBaseHelper<long>.IsNegativeInfinity(unchecked((long)0x8000000000000000)));
            Assert.False(NumberBaseHelper<long>.IsNegativeInfinity(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void IsNormalTest()
        {
            Assert.False(NumberBaseHelper<long>.IsNormal((long)0x0000000000000000));
            Assert.True(NumberBaseHelper<long>.IsNormal((long)0x0000000000000001));
            Assert.True(NumberBaseHelper<long>.IsNormal((long)0x7FFFFFFFFFFFFFFF));
            Assert.True(NumberBaseHelper<long>.IsNormal(unchecked((long)0x8000000000000000)));
            Assert.True(NumberBaseHelper<long>.IsNormal(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void IsOddIntegerTest()
        {
            Assert.False(NumberBaseHelper<long>.IsOddInteger((long)0x0000000000000000));
            Assert.True(NumberBaseHelper<long>.IsOddInteger((long)0x0000000000000001));
            Assert.True(NumberBaseHelper<long>.IsOddInteger((long)0x7FFFFFFFFFFFFFFF));
            Assert.False(NumberBaseHelper<long>.IsOddInteger(unchecked((long)0x8000000000000000)));
            Assert.True(NumberBaseHelper<long>.IsOddInteger(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void IsPositiveTest()
        {
            Assert.True(NumberBaseHelper<long>.IsPositive((long)0x0000000000000000));
            Assert.True(NumberBaseHelper<long>.IsPositive((long)0x0000000000000001));
            Assert.True(NumberBaseHelper<long>.IsPositive((long)0x7FFFFFFFFFFFFFFF));
            Assert.False(NumberBaseHelper<long>.IsPositive(unchecked((long)0x8000000000000000)));
            Assert.False(NumberBaseHelper<long>.IsPositive(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void IsPositiveInfinityTest()
        {
            Assert.False(NumberBaseHelper<long>.IsPositiveInfinity((long)0x0000000000000000));
            Assert.False(NumberBaseHelper<long>.IsPositiveInfinity((long)0x0000000000000001));
            Assert.False(NumberBaseHelper<long>.IsPositiveInfinity((long)0x7FFFFFFFFFFFFFFF));
            Assert.False(NumberBaseHelper<long>.IsPositiveInfinity(unchecked((long)0x8000000000000000)));
            Assert.False(NumberBaseHelper<long>.IsPositiveInfinity(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void IsRealNumberTest()
        {
            Assert.True(NumberBaseHelper<long>.IsRealNumber((long)0x0000000000000000));
            Assert.True(NumberBaseHelper<long>.IsRealNumber((long)0x0000000000000001));
            Assert.True(NumberBaseHelper<long>.IsRealNumber((long)0x7FFFFFFFFFFFFFFF));
            Assert.True(NumberBaseHelper<long>.IsRealNumber(unchecked((long)0x8000000000000000)));
            Assert.True(NumberBaseHelper<long>.IsRealNumber(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void IsSubnormalTest()
        {
            Assert.False(NumberBaseHelper<long>.IsSubnormal((long)0x0000000000000000));
            Assert.False(NumberBaseHelper<long>.IsSubnormal((long)0x0000000000000001));
            Assert.False(NumberBaseHelper<long>.IsSubnormal((long)0x7FFFFFFFFFFFFFFF));
            Assert.False(NumberBaseHelper<long>.IsSubnormal(unchecked((long)0x8000000000000000)));
            Assert.False(NumberBaseHelper<long>.IsSubnormal(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void IsZeroTest()
        {
            Assert.True(NumberBaseHelper<long>.IsZero((long)0x0000000000000000));
            Assert.False(NumberBaseHelper<long>.IsZero((long)0x0000000000000001));
            Assert.False(NumberBaseHelper<long>.IsZero((long)0x7FFFFFFFFFFFFFFF));
            Assert.False(NumberBaseHelper<long>.IsZero(unchecked((long)0x8000000000000000)));
            Assert.False(NumberBaseHelper<long>.IsZero(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void MaxMagnitudeTest()
        {
            Assert.Equal((long)0x0000000000000001, NumberBaseHelper<long>.MaxMagnitude((long)0x0000000000000000, 1));
            Assert.Equal((long)0x0000000000000001, NumberBaseHelper<long>.MaxMagnitude((long)0x0000000000000001, 1));
            Assert.Equal((long)0x7FFFFFFFFFFFFFFF, NumberBaseHelper<long>.MaxMagnitude((long)0x7FFFFFFFFFFFFFFF, 1));
            Assert.Equal(unchecked((long)0x8000000000000000), NumberBaseHelper<long>.MaxMagnitude(unchecked((long)0x8000000000000000), 1));
            Assert.Equal((long)0x0000000000000001, NumberBaseHelper<long>.MaxMagnitude(unchecked((long)0xFFFFFFFFFFFFFFFF), 1));
        }

        [Fact]
        public static void MaxMagnitudeNumberTest()
        {
            Assert.Equal((long)0x0000000000000001, NumberBaseHelper<long>.MaxMagnitudeNumber((long)0x0000000000000000, 1));
            Assert.Equal((long)0x0000000000000001, NumberBaseHelper<long>.MaxMagnitudeNumber((long)0x0000000000000001, 1));
            Assert.Equal((long)0x7FFFFFFFFFFFFFFF, NumberBaseHelper<long>.MaxMagnitudeNumber((long)0x7FFFFFFFFFFFFFFF, 1));
            Assert.Equal(unchecked((long)0x8000000000000000), NumberBaseHelper<long>.MaxMagnitudeNumber(unchecked((long)0x8000000000000000), 1));
            Assert.Equal((long)0x0000000000000001, NumberBaseHelper<long>.MaxMagnitudeNumber(unchecked((long)0xFFFFFFFFFFFFFFFF), 1));
        }

        [Fact]
        public static void MinMagnitudeTest()
        {
            Assert.Equal((long)0x0000000000000000, NumberBaseHelper<long>.MinMagnitude((long)0x0000000000000000, 1));
            Assert.Equal((long)0x0000000000000001, NumberBaseHelper<long>.MinMagnitude((long)0x0000000000000001, 1));
            Assert.Equal((long)0x0000000000000001, NumberBaseHelper<long>.MinMagnitude((long)0x7FFFFFFFFFFFFFFF, 1));
            Assert.Equal((long)0x0000000000000001, NumberBaseHelper<long>.MinMagnitude(unchecked((long)0x8000000000000000), 1));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), NumberBaseHelper<long>.MinMagnitude(unchecked((long)0xFFFFFFFFFFFFFFFF), 1));
        }

        [Fact]
        public static void MinMagnitudeNumberTest()
        {
            Assert.Equal((long)0x0000000000000000, NumberBaseHelper<long>.MinMagnitudeNumber((long)0x0000000000000000, 1));
            Assert.Equal((long)0x0000000000000001, NumberBaseHelper<long>.MinMagnitudeNumber((long)0x0000000000000001, 1));
            Assert.Equal((long)0x0000000000000001, NumberBaseHelper<long>.MinMagnitudeNumber((long)0x7FFFFFFFFFFFFFFF, 1));
            Assert.Equal((long)0x0000000000000001, NumberBaseHelper<long>.MinMagnitudeNumber(unchecked((long)0x8000000000000000), 1));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), NumberBaseHelper<long>.MinMagnitudeNumber(unchecked((long)0xFFFFFFFFFFFFFFFF), 1));
        }

        //
        // IShiftOperators
        //

        [Fact]
        public static void op_LeftShiftTest()
        {
            Assert.Equal((long)0x0000000000000000, ShiftOperatorsHelper<long, int, long>.op_LeftShift((long)0x0000000000000000, 1));
            Assert.Equal((long)0x0000000000000002, ShiftOperatorsHelper<long, int, long>.op_LeftShift((long)0x0000000000000001, 1));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFE), ShiftOperatorsHelper<long, int, long>.op_LeftShift((long)0x7FFFFFFFFFFFFFFF, 1));
            Assert.Equal((long)0x0000000000000000, ShiftOperatorsHelper<long, int, long>.op_LeftShift(unchecked((long)0x8000000000000000), 1));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFE), ShiftOperatorsHelper<long, int, long>.op_LeftShift(unchecked((long)0xFFFFFFFFFFFFFFFF), 1));
        }

        [Fact]
        public static void op_RightShiftTest()
        {
            Assert.Equal((long)0x0000000000000000, ShiftOperatorsHelper<long, int, long>.op_RightShift((long)0x0000000000000000, 1));
            Assert.Equal((long)0x0000000000000000, ShiftOperatorsHelper<long, int, long>.op_RightShift((long)0x0000000000000001, 1));
            Assert.Equal((long)0x3FFFFFFFFFFFFFFF, ShiftOperatorsHelper<long, int, long>.op_RightShift((long)0x7FFFFFFFFFFFFFFF, 1));
            Assert.Equal(unchecked((long)0xC000000000000000), ShiftOperatorsHelper<long, int, long>.op_RightShift(unchecked((long)0x8000000000000000), 1));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), ShiftOperatorsHelper<long, int, long>.op_RightShift(unchecked((long)0xFFFFFFFFFFFFFFFF), 1));
        }

        [Fact]
        public static void op_UnsignedRightShiftTest()
        {
            Assert.Equal((long)0x0000000000000000, ShiftOperatorsHelper<long, int, long>.op_UnsignedRightShift((long)0x0000000000000000, 1));
            Assert.Equal((long)0x0000000000000000, ShiftOperatorsHelper<long, int, long>.op_UnsignedRightShift((long)0x0000000000000001, 1));
            Assert.Equal((long)0x3FFFFFFFFFFFFFFF, ShiftOperatorsHelper<long, int, long>.op_UnsignedRightShift((long)0x7FFFFFFFFFFFFFFF, 1));
            Assert.Equal((long)0x4000000000000000, ShiftOperatorsHelper<long, int, long>.op_UnsignedRightShift(unchecked((long)0x8000000000000000), 1));
            Assert.Equal((long)0x7FFFFFFFFFFFFFFF, ShiftOperatorsHelper<long, int, long>.op_UnsignedRightShift(unchecked((long)0xFFFFFFFFFFFFFFFF), 1));
        }

        //
        // ISignedNumber
        //

        [Fact]
        public static void NegativeOneTest()
        {
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), SignedNumberHelper<long>.NegativeOne);
        }

        //
        // ISubtractionOperators
        //

        [Fact]
        public static void op_SubtractionTest()
        {
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), SubtractionOperatorsHelper<long, long, long>.op_Subtraction((long)0x0000000000000000, 1));
            Assert.Equal((long)0x0000000000000000, SubtractionOperatorsHelper<long, long, long>.op_Subtraction((long)0x0000000000000001, 1));
            Assert.Equal((long)0x7FFFFFFFFFFFFFFE, SubtractionOperatorsHelper<long, long, long>.op_Subtraction((long)0x7FFFFFFFFFFFFFFF, 1));
            Assert.Equal((long)0x7FFFFFFFFFFFFFFF, SubtractionOperatorsHelper<long, long, long>.op_Subtraction(unchecked((long)0x8000000000000000), 1));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFE), SubtractionOperatorsHelper<long, long, long>.op_Subtraction(unchecked((long)0xFFFFFFFFFFFFFFFF), 1));
        }

        [Fact]
        public static void op_CheckedSubtractionTest()
        {
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), SubtractionOperatorsHelper<long, long, long>.op_CheckedSubtraction((long)0x0000000000000000, 1));
            Assert.Equal((long)0x0000000000000000, SubtractionOperatorsHelper<long, long, long>.op_CheckedSubtraction((long)0x0000000000000001, 1));
            Assert.Equal((long)0x7FFFFFFFFFFFFFFE, SubtractionOperatorsHelper<long, long, long>.op_CheckedSubtraction((long)0x7FFFFFFFFFFFFFFF, 1));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFE), SubtractionOperatorsHelper<long, long, long>.op_CheckedSubtraction(unchecked((long)0xFFFFFFFFFFFFFFFF), 1));

            Assert.Throws<OverflowException>(() => SubtractionOperatorsHelper<long, long, long>.op_CheckedSubtraction(unchecked((long)0x8000000000000000), 1));
        }

        //
        // IUnaryNegationOperators
        //

        [Fact]
        public static void op_UnaryNegationTest()
        {
            Assert.Equal((long)0x0000000000000000, UnaryNegationOperatorsHelper<long, long>.op_UnaryNegation((long)0x0000000000000000));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), UnaryNegationOperatorsHelper<long, long>.op_UnaryNegation((long)0x0000000000000001));
            Assert.Equal(unchecked((long)0x8000000000000001), UnaryNegationOperatorsHelper<long, long>.op_UnaryNegation((long)0x7FFFFFFFFFFFFFFF));
            Assert.Equal(unchecked((long)0x8000000000000000), UnaryNegationOperatorsHelper<long, long>.op_UnaryNegation(unchecked((long)0x8000000000000000)));
            Assert.Equal((long)0x0000000000000001, UnaryNegationOperatorsHelper<long, long>.op_UnaryNegation(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void op_CheckedUnaryNegationTest()
        {
            Assert.Equal((long)0x0000000000000000, UnaryNegationOperatorsHelper<long, long>.op_CheckedUnaryNegation((long)0x0000000000000000));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), UnaryNegationOperatorsHelper<long, long>.op_CheckedUnaryNegation((long)0x0000000000000001));
            Assert.Equal(unchecked((long)0x8000000000000001), UnaryNegationOperatorsHelper<long, long>.op_CheckedUnaryNegation((long)0x7FFFFFFFFFFFFFFF));
            Assert.Equal((long)0x0000000000000001, UnaryNegationOperatorsHelper<long, long>.op_CheckedUnaryNegation(unchecked((long)0xFFFFFFFFFFFFFFFF)));

            Assert.Throws<OverflowException>(() => UnaryNegationOperatorsHelper<long, long>.op_CheckedUnaryNegation(unchecked((long)0x8000000000000000)));
        }

        //
        // IUnaryPlusOperators
        //

        [Fact]
        public static void op_UnaryPlusTest()
        {
            Assert.Equal((long)0x0000000000000000, UnaryPlusOperatorsHelper<long, long>.op_UnaryPlus((long)0x0000000000000000));
            Assert.Equal((long)0x0000000000000001, UnaryPlusOperatorsHelper<long, long>.op_UnaryPlus((long)0x0000000000000001));
            Assert.Equal((long)0x7FFFFFFFFFFFFFFF, UnaryPlusOperatorsHelper<long, long>.op_UnaryPlus((long)0x7FFFFFFFFFFFFFFF));
            Assert.Equal(unchecked((long)0x8000000000000000), UnaryPlusOperatorsHelper<long, long>.op_UnaryPlus(unchecked((long)0x8000000000000000)));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), UnaryPlusOperatorsHelper<long, long>.op_UnaryPlus(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        //
        // IParsable and ISpanParsable
        //

        [Theory]
        [MemberData(nameof(Int64Tests.Parse_Valid_TestData), MemberType = typeof(Int64Tests))]
        public static void ParseValidStringTest(string value, NumberStyles style, IFormatProvider provider, long expected)
        {
            long result;

            // Default style and provider
            if ((style == NumberStyles.Integer) && (provider is null))
            {
                Assert.True(ParsableHelper<long>.TryParse(value, provider, out result));
                Assert.Equal(expected, result);
                Assert.Equal(expected, ParsableHelper<long>.Parse(value, provider));
            }

            // Default provider
            if (provider is null)
            {
                Assert.Equal(expected, NumberBaseHelper<long>.Parse(value, style, provider));

                // Substitute default NumberFormatInfo
                Assert.True(NumberBaseHelper<long>.TryParse(value, style, new NumberFormatInfo(), out result));
                Assert.Equal(expected, result);
                Assert.Equal(expected, NumberBaseHelper<long>.Parse(value, style, new NumberFormatInfo()));
            }

            // Default style
            if (style == NumberStyles.Integer)
            {
                Assert.Equal(expected, ParsableHelper<long>.Parse(value, provider));
            }

            // Full overloads
            Assert.True(NumberBaseHelper<long>.TryParse(value, style, provider, out result));
            Assert.Equal(expected, result);
            Assert.Equal(expected, NumberBaseHelper<long>.Parse(value, style, provider));
        }

        [Theory]
        [MemberData(nameof(Int64Tests.Parse_Invalid_TestData), MemberType = typeof(Int64Tests))]
        public static void ParseInvalidStringTest(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            long result;

            // Default style and provider
            if ((style == NumberStyles.Integer) && (provider is null))
            {
                Assert.False(ParsableHelper<long>.TryParse(value, provider, out result));
                Assert.Equal(default(long), result);
                Assert.Throws(exceptionType, () => ParsableHelper<long>.Parse(value, provider));
            }

            // Default provider
            if (provider is null)
            {
                Assert.Throws(exceptionType, () => NumberBaseHelper<long>.Parse(value, style, provider));

                // Substitute default NumberFormatInfo
                Assert.False(NumberBaseHelper<long>.TryParse(value, style, new NumberFormatInfo(), out result));
                Assert.Equal(default(long), result);
                Assert.Throws(exceptionType, () => NumberBaseHelper<long>.Parse(value, style, new NumberFormatInfo()));
            }

            // Default style
            if (style == NumberStyles.Integer)
            {
                Assert.Throws(exceptionType, () => ParsableHelper<long>.Parse(value, provider));
            }

            // Full overloads
            Assert.False(NumberBaseHelper<long>.TryParse(value, style, provider, out result));
            Assert.Equal(default(long), result);
            Assert.Throws(exceptionType, () => NumberBaseHelper<long>.Parse(value, style, provider));
        }

        [Theory]
        [MemberData(nameof(Int64Tests.Parse_ValidWithOffsetCount_TestData), MemberType = typeof(Int64Tests))]
        public static void ParseValidSpanTest(string value, int offset, int count, NumberStyles style, IFormatProvider provider, long expected)
        {
            long result;

            // Default style and provider
            if ((style == NumberStyles.Integer) && (provider is null))
            {
                Assert.True(SpanParsableHelper<long>.TryParse(value.AsSpan(offset, count), provider, out result));
                Assert.Equal(expected, result);
            }

            Assert.Equal(expected, NumberBaseHelper<long>.Parse(value.AsSpan(offset, count), style, provider));

            Assert.True(NumberBaseHelper<long>.TryParse(value.AsSpan(offset, count), style, provider, out result));
            Assert.Equal(expected, result);
        }

        [Theory]
        [MemberData(nameof(Int64Tests.Parse_Invalid_TestData), MemberType = typeof(Int64Tests))]
        public static void ParseInvalidSpanTest(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            if (value is null)
            {
                return;
            }

            long result;

            // Default style and provider
            if ((style == NumberStyles.Integer) && (provider is null))
            {
                Assert.False(SpanParsableHelper<long>.TryParse(value.AsSpan(), provider, out result));
                Assert.Equal(default(long), result);
            }

            Assert.Throws(exceptionType, () => NumberBaseHelper<long>.Parse(value.AsSpan(), style, provider));

            Assert.False(NumberBaseHelper<long>.TryParse(value.AsSpan(), style, provider, out result));
            Assert.Equal(default(long), result);
        }
    }
}
