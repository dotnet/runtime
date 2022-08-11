// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Tests
{
    public class Int128Tests_GenericMath
    {
        internal static readonly Int128 ByteMaxValue = new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_00FF);

        internal static readonly Int128 Int16MaxValue = new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_7FFF);

        internal static readonly Int128 Int16MaxValuePlusOne = new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_8000);

        internal static readonly Int128 Int16MinValue = new Int128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_8000);

        internal static readonly Int128 Int32MaxValue = new Int128(0x0000_0000_0000_0000, 0x0000_0000_7FFF_FFFF);

        internal static readonly Int128 Int32MaxValuePlusOne = new Int128(0x0000_0000_0000_0000, 0x0000_0000_8000_0000);

        internal static readonly Int128 Int32MinValue = new Int128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_8000_0000);

        internal static readonly Int128 Int64MaxValue = new Int128(0x0000_0000_0000_0000, 0x7FFF_FFFF_FFFF_FFFF);

        internal static readonly Int128 Int64MaxValuePlusOne = new Int128(0x0000_0000_0000_0000, 0x8000_0000_0000_0000);

        internal static readonly Int128 Int64MinValue = new Int128(0xFFFF_FFFF_FFFF_FFFF, 0x8000_0000_0000_0000);

        internal static readonly Int128 MaxValue = new Int128(0x7FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF);

        internal static readonly Int128 MaxValueMinusOne = new Int128(0x7FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFE);

        internal static readonly Int128 MinValue = new Int128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000);

        internal static readonly Int128 MinValuePlusOne = new Int128(0x8000_0000_0000_0000, 0x0000_0000_0000_0001);

        internal static readonly Int128 NegativeOne = new Int128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF);

        internal static readonly Int128 NegativeTwo = new Int128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFE);

        internal static readonly Int128 One = new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001);

        internal static readonly Int128 SByteMaxValue = new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_007F);

        internal static readonly Int128 SByteMaxValuePlusOne = new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0080);

        internal static readonly Int128 SByteMinValue = new Int128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FF80);

        internal static readonly Int128 Two = new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0002);

        internal static readonly Int128 UInt16MaxValue = new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_FFFF);

        internal static readonly Int128 UInt32MaxValue = new Int128(0x0000_0000_0000_0000, 0x0000_0000_FFFF_FFFF);

        internal static readonly Int128 UInt64MaxValue = new Int128(0x0000_0000_0000_0000, 0xFFFF_FFFF_FFFF_FFFF);

        internal static readonly Int128 Zero = new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000);

        //
        // IAdditionOperators
        //

        [Fact]
        public static void op_AdditionTest()
        {
            Assert.Equal(One, AdditionOperatorsHelper<Int128, Int128, Int128>.op_Addition(Zero, 1));
            Assert.Equal(Two, AdditionOperatorsHelper<Int128, Int128, Int128>.op_Addition(One, 1));
            Assert.Equal(MinValue, AdditionOperatorsHelper<Int128, Int128, Int128>.op_Addition(MaxValue, 1));
            Assert.Equal(MinValuePlusOne, AdditionOperatorsHelper<Int128, Int128, Int128>.op_Addition(MinValue, 1));
            Assert.Equal(Zero, AdditionOperatorsHelper<Int128, Int128, Int128>.op_Addition(NegativeOne, 1));
        }

        [Fact]
        public static void op_CheckedAdditionTest()
        {
            Assert.Equal(One, AdditionOperatorsHelper<Int128, Int128, Int128>.op_CheckedAddition(Zero, 1));
            Assert.Equal(Two, AdditionOperatorsHelper<Int128, Int128, Int128>.op_CheckedAddition(One, 1));
            Assert.Equal(MinValuePlusOne, AdditionOperatorsHelper<Int128, Int128, Int128>.op_CheckedAddition(MinValue, 1));
            Assert.Equal(Zero, AdditionOperatorsHelper<Int128, Int128, Int128>.op_CheckedAddition(NegativeOne, 1));

            Assert.Throws<OverflowException>(() => AdditionOperatorsHelper<Int128, Int128, Int128>.op_CheckedAddition(MaxValue, 1));
        }

        //
        // IAdditiveIdentity
        //

        [Fact]
        public static void AdditiveIdentityTest()
        {
            Assert.Equal(Zero, AdditiveIdentityHelper<Int128, Int128>.AdditiveIdentity);
        }

        //
        // IBinaryInteger
        //

        [Fact]
        public static void DivRemTest()
        {
            Assert.Equal((Zero, Zero), BinaryIntegerHelper<Int128>.DivRem(Zero, 2));
            Assert.Equal((Zero, One), BinaryIntegerHelper<Int128>.DivRem(One, 2));
            Assert.Equal((new Int128(0x3FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), One), BinaryIntegerHelper<Int128>.DivRem(MaxValue, 2));
            Assert.Equal((new Int128(0xC000_0000_0000_0000, 0x0000_0000_0000_0000), Zero), BinaryIntegerHelper<Int128>.DivRem(MinValue, 2));
            Assert.Equal((Zero, NegativeOne), BinaryIntegerHelper<Int128>.DivRem(NegativeOne, 2));
        }

        [Fact]
        public static void LeadingZeroCountTest()
        {
            Assert.Equal(0x80, BinaryIntegerHelper<Int128>.LeadingZeroCount(Zero));
            Assert.Equal(0x7F, BinaryIntegerHelper<Int128>.LeadingZeroCount(One));
            Assert.Equal(0x01, BinaryIntegerHelper<Int128>.LeadingZeroCount(MaxValue));
            Assert.Equal(0x00, BinaryIntegerHelper<Int128>.LeadingZeroCount(MinValue));
            Assert.Equal(0x00, BinaryIntegerHelper<Int128>.LeadingZeroCount(NegativeOne));
        }

        [Fact]
        public static void PopCountTest()
        {
            Assert.Equal(0x00, BinaryIntegerHelper<Int128>.PopCount(Zero));
            Assert.Equal(0x01, BinaryIntegerHelper<Int128>.PopCount(One));
            Assert.Equal(0x7F, BinaryIntegerHelper<Int128>.PopCount(MaxValue));
            Assert.Equal(0x01, BinaryIntegerHelper<Int128>.PopCount(MinValue));
            Assert.Equal(0x80, BinaryIntegerHelper<Int128>.PopCount(NegativeOne));
        }

        [Fact]
        public static void RotateLeftTest()
        {
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), BinaryIntegerHelper<Int128>.RotateLeft(Zero, 1));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0002), BinaryIntegerHelper<Int128>.RotateLeft(One, 1));
            Assert.Equal(new Int128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFE), BinaryIntegerHelper<Int128>.RotateLeft(MaxValue, 1));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), BinaryIntegerHelper<Int128>.RotateLeft(MinValue, 1));
            Assert.Equal(new Int128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), BinaryIntegerHelper<Int128>.RotateLeft(NegativeOne, 1));
        }

        [Fact]
        public static void RotateRightTest()
        {
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), BinaryIntegerHelper<Int128>.RotateRight(Zero, 1));
            Assert.Equal(new Int128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000), BinaryIntegerHelper<Int128>.RotateRight(One, 1));
            Assert.Equal(new Int128(0xBFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), BinaryIntegerHelper<Int128>.RotateRight(MaxValue, 1));
            Assert.Equal(new Int128(0x4000_0000_0000_0000, 0x0000_0000_0000_0000), BinaryIntegerHelper<Int128>.RotateRight(MinValue, 1));
            Assert.Equal(new Int128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), BinaryIntegerHelper<Int128>.RotateRight(NegativeOne, 1));
        }

        [Fact]
        public static void TrailingZeroCountTest()
        {
            Assert.Equal(0x80, BinaryIntegerHelper<Int128>.TrailingZeroCount(Zero));
            Assert.Equal(0x00, BinaryIntegerHelper<Int128>.TrailingZeroCount(One));
            Assert.Equal(0x00, BinaryIntegerHelper<Int128>.TrailingZeroCount(MaxValue));
            Assert.Equal(0x7F, BinaryIntegerHelper<Int128>.TrailingZeroCount(MinValue));
            Assert.Equal(0x00, BinaryIntegerHelper<Int128>.TrailingZeroCount(NegativeOne));
        }

        [Fact]
        public static void TryReadBigEndianByteTest()
        {
            Int128 result;

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0x00 }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0x01 }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0x7F }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_007F), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0x80 }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0080), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0xFF }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_00FF), result);
        }

        [Fact]
        public static void TryReadBigEndianInt16Test()
        {
            Int128 result;

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0080), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0x01, 0x00 }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0100), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0x7F, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_7FFF), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0x80, 0x00 }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_8000), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FF7F), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), result);
        }

        [Fact]
        public static void TryReadBigEndianInt32Test()
        {
            Int128 result;

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0080), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0x01, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0100_0000), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_7FFF_FFFF), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0x80, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_8000_0000), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FF7F), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), result);
        }

        [Fact]
        public static void TryReadBigEndianInt64Test()
        {
            Int128 result;

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0080), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0100_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x7FFF_FFFF_FFFF_FFFF), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0xFFFF_FFFF_FFFF_FFFF, 0x8000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FF7F), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), result);
        }

        [Fact]
        public static void TryReadBigEndianInt96Test()
        {
            Int128 result;

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0080), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0x0000_0000_0100_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0x0000_0000_7FFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0xFFFF_FFFF_8000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FF7F), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), result);
        }

        [Fact]
        public static void TryReadBigEndianInt128Test()
        {
            Int128 result;

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0080), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0x0100_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0x7FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FF7F), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), result);
        }

        [Fact]
        public static void TryReadBigEndianSByteTest()
        {
            Int128 result;

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0x00 }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0x01 }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0x7F }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_007F), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0x80 }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FF80), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0xFF }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), result);
        }

        [Fact]
        public static void TryReadBigEndianUInt16Test()
        {
            Int128 result;

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0080), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0x01, 0x00 }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0100), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0x7F, 0xFF }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_7FFF), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0x80, 0x00 }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_8000), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_FF7F), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_FFFF), result);
        }

        [Fact]
        public static void TryReadBigEndianUInt32Test()
        {
            Int128 result;

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0080), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0x01, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0100_0000), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_7FFF_FFFF), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0x80, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_8000_0000), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_FFFF_FF7F), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_FFFF_FFFF), result);
        }

        [Fact]
        public static void TryReadBigEndianUInt64Test()
        {
            Int128 result;

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0080), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0100_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x7FFF_FFFF_FFFF_FFFF), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x8000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0xFFFF_FFFF_FFFF_FF7F), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0xFFFF_FFFF_FFFF_FFFF), result);
        }

        [Fact]
        public static void TryReadBigEndianUInt96Test()
        {
            Int128 result;

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0080), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0100_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_7FFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_8000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FF7F), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), result);
        }

        [Fact]
        public static void TryReadBigEndianUInt128Test()
        {
            Int128 result;

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0080), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0100_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x7FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), result);

            Assert.False(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.False(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.False(BinaryIntegerHelper<Int128>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);
        }

        [Fact]
        public static void TryReadLittleEndianByteTest()
        {
            Int128 result;

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0x00 }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0x01 }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0x7F }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_007F), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0x80 }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0080), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0xFF }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_00FF), result);
        }

        [Fact]
        public static void TryReadLittleEndianInt16Test()
        {
            Int128 result;

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0100), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_8000), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0x01, 0x00 }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FF7F), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0x80, 0x00 }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0080), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_7FFF), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), result);
        }

        [Fact]
        public static void TryReadLittleEndianInt32Test()
        {
            Int128 result;

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0100_0000), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_8000_0000), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0x01, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FF7F), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0x80, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0080), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_7FFF_FFFF), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), result);
        }

        [Fact]
        public static void TryReadLittleEndianInt64Test()
        {
            Int128 result;

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0100_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0xFFFF_FFFF_FFFF_FFFF, 0x8000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FF7F), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0080), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x7FFF_FFFF_FFFF_FFFF), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), result);
        }

        [Fact]
        public static void TryReadLittleEndianInt96Test()
        {
            Int128 result;

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0x0000_0000_0100_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0xFFFF_FFFF_8000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FF7F), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0080), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0x0000_0000_7FFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), result);
        }

        [Fact]
        public static void TryReadLittleEndianInt128Test()
        {
            Int128 result;

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0x0100_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FF7F), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0080), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0x7FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), result);
        }

        [Fact]
        public static void TryReadLittleEndianSByteTest()
        {
            Int128 result;

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0x00 }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0x01 }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0x7F }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_007F), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0x80 }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FF80), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0xFF }, isUnsigned: false, out result));
            Assert.Equal(new Int128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), result);
        }

        [Fact]
        public static void TryReadLittleEndianUInt16Test()
        {
            Int128 result;

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0100), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_8000), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0x01, 0x00 }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_FF7F), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0x80, 0x00 }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0080), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_7FFF), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_FFFF), result);
        }

        [Fact]
        public static void TryReadLittleEndianUInt32Test()
        {
            Int128 result;

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0100_0000), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_8000_0000), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0x01, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_FFFF_FF7F), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0x80, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0080), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_7FFF_FFFF), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_FFFF_FFFF), result);
        }

        [Fact]
        public static void TryReadLittleEndianUInt64Test()
        {
            Int128 result;

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0100_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x8000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0xFFFF_FFFF_FFFF_FF7F), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0080), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x7FFF_FFFF_FFFF_FFFF), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0xFFFF_FFFF_FFFF_FFFF), result);
        }

        [Fact]
        public static void TryReadLittleEndianUInt96Test()
        {
            Int128 result;

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0100_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_8000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FF7F), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0080), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_7FFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), result);
        }

        [Fact]
        public static void TryReadLittleEndianUInt128Test()
        {
            Int128 result;

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0100_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.False(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), result);

            Assert.False(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0080), result);

            Assert.True(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x7FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), result);

            Assert.False(BinaryIntegerHelper<Int128>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);
        }

        [Fact]
        public static void GetByteCountTest()
        {
            Assert.Equal(16, BinaryIntegerHelper<Int128>.GetByteCount(Zero));
            Assert.Equal(16, BinaryIntegerHelper<Int128>.GetByteCount(One));
            Assert.Equal(16, BinaryIntegerHelper<Int128>.GetByteCount(MaxValue));
            Assert.Equal(16, BinaryIntegerHelper<Int128>.GetByteCount(MinValue));
            Assert.Equal(16, BinaryIntegerHelper<Int128>.GetByteCount(NegativeOne));
        }

        [Fact]
        public static void GetShortestBitLengthTest()
        {
            Assert.Equal(0x00, BinaryIntegerHelper<Int128>.GetShortestBitLength(Zero));
            Assert.Equal(0x01, BinaryIntegerHelper<Int128>.GetShortestBitLength(One));
            Assert.Equal(0x7F, BinaryIntegerHelper<Int128>.GetShortestBitLength(MaxValue));
            Assert.Equal(0x80, BinaryIntegerHelper<Int128>.GetShortestBitLength(MinValue));
            Assert.Equal(0x01, BinaryIntegerHelper<Int128>.GetShortestBitLength(NegativeOne));
        }

        [Fact]
        public static void TryWriteBigEndianTest()
        {
            Span<byte> destination = stackalloc byte[16];
            int bytesWritten = 0;

            Assert.True(BinaryIntegerHelper<Int128>.TryWriteBigEndian(Zero, destination, out bytesWritten));
            Assert.Equal(16, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<Int128>.TryWriteBigEndian(One, destination, out bytesWritten));
            Assert.Equal(16, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<Int128>.TryWriteBigEndian(MaxValue, destination, out bytesWritten));
            Assert.Equal(16, bytesWritten);
            Assert.Equal(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<Int128>.TryWriteBigEndian(MinValue, destination, out bytesWritten));
            Assert.Equal(16, bytesWritten);
            Assert.Equal(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<Int128>.TryWriteBigEndian(NegativeOne, destination, out bytesWritten));
            Assert.Equal(16, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, destination.ToArray());

            Assert.False(BinaryIntegerHelper<Int128>.TryWriteBigEndian(default, Span<byte>.Empty, out bytesWritten));
            Assert.Equal(0, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, destination.ToArray());
        }

        [Fact]
        public static void TryWriteLittleEndianTest()
        {
            Span<byte> destination = stackalloc byte[16];
            int bytesWritten = 0;

            Assert.True(BinaryIntegerHelper<Int128>.TryWriteLittleEndian(Zero, destination, out bytesWritten));
            Assert.Equal(16, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<Int128>.TryWriteLittleEndian(One, destination, out bytesWritten));
            Assert.Equal(16, bytesWritten);
            Assert.Equal(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<Int128>.TryWriteLittleEndian(MaxValue, destination, out bytesWritten));
            Assert.Equal(16, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<Int128>.TryWriteLittleEndian(MinValue, destination, out bytesWritten));
            Assert.Equal(16, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<Int128>.TryWriteLittleEndian(NegativeOne, destination, out bytesWritten));
            Assert.Equal(16, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, destination.ToArray());

            Assert.False(BinaryIntegerHelper<Int128>.TryWriteLittleEndian(default, Span<byte>.Empty, out bytesWritten));
            Assert.Equal(0, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, destination.ToArray());
        }

        //
        // IBinaryNumber
        //

        [Fact]
        public static void AllBitsSetTest()
        {
            Int128 compare = new Int128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF);
            Assert.Equal(compare, BinaryNumberHelper<Int128>.AllBitsSet);
            Assert.Equal((Int128)0, ~BinaryNumberHelper<Int128>.AllBitsSet);
        }

        [Fact]
        public static void IsPow2Test()
        {
            Assert.False(BinaryNumberHelper<Int128>.IsPow2(Zero));
            Assert.True(BinaryNumberHelper<Int128>.IsPow2(One));
            Assert.False(BinaryNumberHelper<Int128>.IsPow2(MaxValue));
            Assert.False(BinaryNumberHelper<Int128>.IsPow2(MinValue));
            Assert.False(BinaryNumberHelper<Int128>.IsPow2(NegativeOne));
        }

        [Fact]
        public static void Log2Test()
        {
            Assert.Equal(0x00, BinaryNumberHelper<Int128>.Log2(Zero));
            Assert.Equal(0x00, BinaryNumberHelper<Int128>.Log2(One));
            Assert.Equal(0x7E, BinaryNumberHelper<Int128>.Log2(MaxValue));
            Assert.Throws<ArgumentOutOfRangeException>(() => BinaryNumberHelper<Int128>.Log2(MinValue));
            Assert.Throws<ArgumentOutOfRangeException>(() => BinaryNumberHelper<Int128>.Log2(NegativeOne));
        }

        //
        // IBitwiseOperators
        //

        [Fact]
        public static void op_BitwiseAndTest()
        {
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), BitwiseOperatorsHelper<Int128, Int128, Int128>.op_BitwiseAnd(Zero, 1));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), BitwiseOperatorsHelper<Int128, Int128, Int128>.op_BitwiseAnd(One, 1));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), BitwiseOperatorsHelper<Int128, Int128, Int128>.op_BitwiseAnd(MaxValue, 1));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), BitwiseOperatorsHelper<Int128, Int128, Int128>.op_BitwiseAnd(MinValue, 1));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), BitwiseOperatorsHelper<Int128, Int128, Int128>.op_BitwiseAnd(NegativeOne, 1));
        }

        [Fact]
        public static void op_BitwiseOrTest()
        {
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), BitwiseOperatorsHelper<Int128, Int128, Int128>.op_BitwiseOr(Zero, 1));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), BitwiseOperatorsHelper<Int128, Int128, Int128>.op_BitwiseOr(One, 1));
            Assert.Equal(new Int128(0x7FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), BitwiseOperatorsHelper<Int128, Int128, Int128>.op_BitwiseOr(MaxValue, 1));
            Assert.Equal(new Int128(0x8000_0000_0000_0000, 0x0000_0000_0000_0001), BitwiseOperatorsHelper<Int128, Int128, Int128>.op_BitwiseOr(MinValue, 1));
            Assert.Equal(new Int128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), BitwiseOperatorsHelper<Int128, Int128, Int128>.op_BitwiseOr(NegativeOne, 1));
        }

        [Fact]
        public static void op_ExclusiveOrTest()
        {
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), BitwiseOperatorsHelper<Int128, Int128, Int128>.op_ExclusiveOr(Zero, 1));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), BitwiseOperatorsHelper<Int128, Int128, Int128>.op_ExclusiveOr(One, 1));
            Assert.Equal(new Int128(0x7FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFE), BitwiseOperatorsHelper<Int128, Int128, Int128>.op_ExclusiveOr(MaxValue, 1));
            Assert.Equal(new Int128(0x8000_0000_0000_0000, 0x0000_0000_0000_0001), BitwiseOperatorsHelper<Int128, Int128, Int128>.op_ExclusiveOr(MinValue, 1));
            Assert.Equal(new Int128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFE), BitwiseOperatorsHelper<Int128, Int128, Int128>.op_ExclusiveOr(NegativeOne, 1));
        }

        [Fact]
        public static void op_OnesComplementTest()
        {
            Assert.Equal(new Int128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), BitwiseOperatorsHelper<Int128, Int128, Int128>.op_OnesComplement(Zero));
            Assert.Equal(new Int128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFE), BitwiseOperatorsHelper<Int128, Int128, Int128>.op_OnesComplement(One));
            Assert.Equal(new Int128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000), BitwiseOperatorsHelper<Int128, Int128, Int128>.op_OnesComplement(MaxValue));
            Assert.Equal(new Int128(0x7FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), BitwiseOperatorsHelper<Int128, Int128, Int128>.op_OnesComplement(MinValue));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), BitwiseOperatorsHelper<Int128, Int128, Int128>.op_OnesComplement(NegativeOne));
        }

        //
        // IComparisonOperators
        //

        [Fact]
        public static void op_GreaterThanTest()
        {
            Assert.False(ComparisonOperatorsHelper<Int128, Int128, bool>.op_GreaterThan(Zero, 1));
            Assert.False(ComparisonOperatorsHelper<Int128, Int128, bool>.op_GreaterThan(One, 1));
            Assert.True(ComparisonOperatorsHelper<Int128, Int128, bool>.op_GreaterThan(MaxValue, 1));
            Assert.False(ComparisonOperatorsHelper<Int128, Int128, bool>.op_GreaterThan(MinValue, 1));
            Assert.False(ComparisonOperatorsHelper<Int128, Int128, bool>.op_GreaterThan(NegativeOne, 1));
        }

        [Fact]
        public static void op_GreaterThanOrEqualTest()
        {
            Assert.False(ComparisonOperatorsHelper<Int128, Int128, bool>.op_GreaterThanOrEqual(Zero, 1));
            Assert.True(ComparisonOperatorsHelper<Int128, Int128, bool>.op_GreaterThanOrEqual(One, 1));
            Assert.True(ComparisonOperatorsHelper<Int128, Int128, bool>.op_GreaterThanOrEqual(MaxValue, 1));
            Assert.False(ComparisonOperatorsHelper<Int128, Int128, bool>.op_GreaterThanOrEqual(MinValue, 1));
            Assert.False(ComparisonOperatorsHelper<Int128, Int128, bool>.op_GreaterThanOrEqual(NegativeOne, 1));
        }

        [Fact]
        public static void op_LessThanTest()
        {
            Assert.True(ComparisonOperatorsHelper<Int128, Int128, bool>.op_LessThan(Zero, 1));
            Assert.False(ComparisonOperatorsHelper<Int128, Int128, bool>.op_LessThan(One, 1));
            Assert.False(ComparisonOperatorsHelper<Int128, Int128, bool>.op_LessThan(MaxValue, 1));
            Assert.True(ComparisonOperatorsHelper<Int128, Int128, bool>.op_LessThan(MinValue, 1));
            Assert.True(ComparisonOperatorsHelper<Int128, Int128, bool>.op_LessThan(NegativeOne, 1));
        }

        [Fact]
        public static void op_LessThanOrEqualTest()
        {
            Assert.True(ComparisonOperatorsHelper<Int128, Int128, bool>.op_LessThanOrEqual(Zero, 1));
            Assert.True(ComparisonOperatorsHelper<Int128, Int128, bool>.op_LessThanOrEqual(One, 1));
            Assert.False(ComparisonOperatorsHelper<Int128, Int128, bool>.op_LessThanOrEqual(MaxValue, 1));
            Assert.True(ComparisonOperatorsHelper<Int128, Int128, bool>.op_LessThanOrEqual(MinValue, 1));
            Assert.True(ComparisonOperatorsHelper<Int128, Int128, bool>.op_LessThanOrEqual(NegativeOne, 1));
        }

        //
        // IDecrementOperators
        //

        [Fact]
        public static void op_DecrementTest()
        {
            Assert.Equal(NegativeOne, DecrementOperatorsHelper<Int128>.op_Decrement(Zero));
            Assert.Equal(Zero, DecrementOperatorsHelper<Int128>.op_Decrement(One));
            Assert.Equal(MaxValueMinusOne, DecrementOperatorsHelper<Int128>.op_Decrement(MaxValue));
            Assert.Equal(MaxValue, DecrementOperatorsHelper<Int128>.op_Decrement(MinValue));
            Assert.Equal(NegativeTwo, DecrementOperatorsHelper<Int128>.op_Decrement(NegativeOne));
        }

        [Fact]
        public static void op_CheckedDecrementTest()
        {
            Assert.Equal(NegativeOne, DecrementOperatorsHelper<Int128>.op_CheckedDecrement(Zero));
            Assert.Equal(Zero, DecrementOperatorsHelper<Int128>.op_CheckedDecrement(One));
            Assert.Equal(MaxValueMinusOne, DecrementOperatorsHelper<Int128>.op_CheckedDecrement(MaxValue));
            Assert.Equal(NegativeTwo, DecrementOperatorsHelper<Int128>.op_CheckedDecrement(NegativeOne));

            Assert.Throws<OverflowException>(() => DecrementOperatorsHelper<Int128>.op_CheckedDecrement(MinValue));
        }

        //
        // IDivisionOperators
        //

        [Fact]
        public static void op_DivisionTest()
        {
            Assert.Equal(Zero, DivisionOperatorsHelper<Int128, Int128, Int128>.op_Division(Zero, 2));
            Assert.Equal(Zero, DivisionOperatorsHelper<Int128, Int128, Int128>.op_Division(One, 2));
            Assert.Equal(new Int128(0x3FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), DivisionOperatorsHelper<Int128, Int128, Int128>.op_Division(MaxValue, 2));
            Assert.Equal(new Int128(0xC000_0000_0000_0000, 0x0000_0000_0000_0000), DivisionOperatorsHelper<Int128, Int128, Int128>.op_Division(MinValue, 2));
            Assert.Equal(Zero, DivisionOperatorsHelper<Int128, Int128, Int128>.op_Division(NegativeOne, 2));

            Assert.Throws<DivideByZeroException>(() => DivisionOperatorsHelper<Int128, Int128, Int128>.op_Division(One, 0));
        }

        [Fact]
        public static void op_CheckedDivisionTest()
        {
            Assert.Equal(Zero, DivisionOperatorsHelper<Int128, Int128, Int128>.op_CheckedDivision(Zero, 2));
            Assert.Equal(Zero, DivisionOperatorsHelper<Int128, Int128, Int128>.op_CheckedDivision(One, 2));
            Assert.Equal(new Int128(0x3FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), DivisionOperatorsHelper<Int128, Int128, Int128>.op_CheckedDivision(MaxValue, 2));
            Assert.Equal(new Int128(0xC000_0000_0000_0000, 0x0000_0000_0000_0000), DivisionOperatorsHelper<Int128, Int128, Int128>.op_CheckedDivision(MinValue, 2));
            Assert.Equal(Zero, DivisionOperatorsHelper<Int128, Int128, Int128>.op_CheckedDivision(NegativeOne, 2));

            Assert.Throws<DivideByZeroException>(() => DivisionOperatorsHelper<Int128, Int128, Int128>.op_CheckedDivision(One, 0));
        }

        //
        // IEqualityOperators
        //

        [Fact]
        public static void op_EqualityTest()
        {
            Assert.False(EqualityOperatorsHelper<Int128, Int128, bool>.op_Equality(Zero, 1));
            Assert.True(EqualityOperatorsHelper<Int128, Int128, bool>.op_Equality(One, 1));
            Assert.False(EqualityOperatorsHelper<Int128, Int128, bool>.op_Equality(MaxValue, 1));
            Assert.False(EqualityOperatorsHelper<Int128, Int128, bool>.op_Equality(MinValue, 1));
            Assert.False(EqualityOperatorsHelper<Int128, Int128, bool>.op_Equality(NegativeOne, 1));
        }

        [Fact]
        public static void op_InequalityTest()
        {
            Assert.True(EqualityOperatorsHelper<Int128, Int128, bool>.op_Inequality(Zero, 1));
            Assert.False(EqualityOperatorsHelper<Int128, Int128, bool>.op_Inequality(One, 1));
            Assert.True(EqualityOperatorsHelper<Int128, Int128, bool>.op_Inequality(MaxValue, 1));
            Assert.True(EqualityOperatorsHelper<Int128, Int128, bool>.op_Inequality(MinValue, 1));
            Assert.True(EqualityOperatorsHelper<Int128, Int128, bool>.op_Inequality(NegativeOne, 1));
        }

        //
        // IIncrementOperators
        //

        [Fact]
        public static void op_IncrementTest()
        {
            Assert.Equal(One, IncrementOperatorsHelper<Int128>.op_Increment(Zero));
            Assert.Equal(Two, IncrementOperatorsHelper<Int128>.op_Increment(One));
            Assert.Equal(MinValue, IncrementOperatorsHelper<Int128>.op_Increment(MaxValue));
            Assert.Equal(MinValuePlusOne, IncrementOperatorsHelper<Int128>.op_Increment(MinValue));
            Assert.Equal(Zero, IncrementOperatorsHelper<Int128>.op_Increment(NegativeOne));
        }

        [Fact]
        public static void op_CheckedIncrementTest()
        {
            Assert.Equal(One, IncrementOperatorsHelper<Int128>.op_CheckedIncrement(Zero));
            Assert.Equal(Two, IncrementOperatorsHelper<Int128>.op_CheckedIncrement(One));
            Assert.Equal(MinValuePlusOne, IncrementOperatorsHelper<Int128>.op_CheckedIncrement(MinValue));
            Assert.Equal(Zero, IncrementOperatorsHelper<Int128>.op_CheckedIncrement(NegativeOne));

            Assert.Throws<OverflowException>(() => IncrementOperatorsHelper<Int128>.op_CheckedIncrement(MaxValue));
        }

        //
        // IMinMaxValue
        //

        [Fact]
        public static void MaxValueTest()
        {
            Assert.Equal(MaxValue, MinMaxValueHelper<Int128>.MaxValue);
        }

        [Fact]
        public static void MinValueTest()
        {
            Assert.Equal(MinValue, MinMaxValueHelper<Int128>.MinValue);
        }

        //
        // IModulusOperators
        //

        [Fact]
        public static void op_ModulusTest()
        {
            Assert.Equal(Zero, ModulusOperatorsHelper<Int128, Int128, Int128>.op_Modulus(Zero, 2));
            Assert.Equal(One, ModulusOperatorsHelper<Int128, Int128, Int128>.op_Modulus(One, 2));
            Assert.Equal(One, ModulusOperatorsHelper<Int128, Int128, Int128>.op_Modulus(MaxValue, 2));
            Assert.Equal(Zero, ModulusOperatorsHelper<Int128, Int128, Int128>.op_Modulus(MinValue, 2));
            Assert.Equal(NegativeOne, ModulusOperatorsHelper<Int128, Int128, Int128>.op_Modulus(NegativeOne, 2));

            Assert.Throws<DivideByZeroException>(() => ModulusOperatorsHelper<Int128, Int128, Int128>.op_Modulus(One, 0));
        }

        //
        // IMultiplicativeIdentity
        //

        [Fact]
        public static void MultiplicativeIdentityTest()
        {
            Assert.Equal(One, MultiplicativeIdentityHelper<Int128, Int128>.MultiplicativeIdentity);
        }

        //
        // IMultiplyOperators
        //

        [Fact]
        public static void op_MultiplyTest()
        {
            Assert.Equal(Zero, MultiplyOperatorsHelper<Int128, Int128, Int128>.op_Multiply(Zero, 2));
            Assert.Equal(Two, MultiplyOperatorsHelper<Int128, Int128, Int128>.op_Multiply(One, 2));
            Assert.Equal(NegativeTwo, MultiplyOperatorsHelper<Int128, Int128, Int128>.op_Multiply(MaxValue, 2));
            Assert.Equal(Zero, MultiplyOperatorsHelper<Int128, Int128, Int128>.op_Multiply(MinValue, 2));
            Assert.Equal(NegativeTwo, MultiplyOperatorsHelper<Int128, Int128, Int128>.op_Multiply(NegativeOne, 2));
        }

        [Fact]
        public static void op_CheckedMultiplyTest()
        {
            Assert.Equal(Zero, MultiplyOperatorsHelper<Int128, Int128, Int128>.op_CheckedMultiply(Zero, 2));
            Assert.Equal(Two, MultiplyOperatorsHelper<Int128, Int128, Int128>.op_CheckedMultiply(One, 2));
            Assert.Equal(NegativeTwo, MultiplyOperatorsHelper<Int128, Int128, Int128>.op_CheckedMultiply(NegativeOne, 2));

            Assert.Throws<OverflowException>(() => MultiplyOperatorsHelper<Int128, Int128, Int128>.op_CheckedMultiply(MaxValue, 2));
            Assert.Throws<OverflowException>(() => MultiplyOperatorsHelper<Int128, Int128, Int128>.op_CheckedMultiply(MinValue, 2));
        }

        //
        // INumber
        //

        [Fact]
        public static void ClampTest()
        {
            Assert.Equal(Zero, NumberHelper<Int128>.Clamp(Zero, new Int128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FF80), 0x007F));
            Assert.Equal(One, NumberHelper<Int128>.Clamp(One, new Int128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FF80), 0x007F));
            Assert.Equal(0x007F, NumberHelper<Int128>.Clamp(MaxValue, new Int128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FF80), 0x007F));
            Assert.Equal(new Int128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FF80), NumberHelper<Int128>.Clamp(MinValue, new Int128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FF80), 0x007F));
            Assert.Equal(NegativeOne, NumberHelper<Int128>.Clamp(NegativeOne, new Int128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FF80), 0x007F));
        }

        [Fact]
        public static void MaxTest()
        {
            Assert.Equal(One, NumberHelper<Int128>.Max(Zero, 1));
            Assert.Equal(One, NumberHelper<Int128>.Max(One, 1));
            Assert.Equal(MaxValue, NumberHelper<Int128>.Max(MaxValue, 1));
            Assert.Equal(One, NumberHelper<Int128>.Max(MinValue, 1));
            Assert.Equal(One, NumberHelper<Int128>.Max(NegativeOne, 1));
        }

        [Fact]
        public static void MaxNumberTest()
        {
            Assert.Equal(One, NumberHelper<Int128>.MaxNumber(Zero, 1));
            Assert.Equal(One, NumberHelper<Int128>.MaxNumber(One, 1));
            Assert.Equal(MaxValue, NumberHelper<Int128>.MaxNumber(MaxValue, 1));
            Assert.Equal(One, NumberHelper<Int128>.MaxNumber(MinValue, 1));
            Assert.Equal(One, NumberHelper<Int128>.MaxNumber(NegativeOne, 1));
        }

        [Fact]
        public static void MinTest()
        {
            Assert.Equal(Zero, NumberHelper<Int128>.Min(Zero, 1));
            Assert.Equal(One, NumberHelper<Int128>.Min(One, 1));
            Assert.Equal(One, NumberHelper<Int128>.Min(MaxValue, 1));
            Assert.Equal(MinValue, NumberHelper<Int128>.Min(MinValue, 1));
            Assert.Equal(NegativeOne, NumberHelper<Int128>.Min(NegativeOne, 1));
        }

        [Fact]
        public static void MinNumberTest()
        {
            Assert.Equal(Zero, NumberHelper<Int128>.MinNumber(Zero, 1));
            Assert.Equal(One, NumberHelper<Int128>.MinNumber(One, 1));
            Assert.Equal(One, NumberHelper<Int128>.MinNumber(MaxValue, 1));
            Assert.Equal(MinValue, NumberHelper<Int128>.MinNumber(MinValue, 1));
            Assert.Equal(NegativeOne, NumberHelper<Int128>.MinNumber(NegativeOne, 1));
        }

        [Fact]
        public static void SignTest()
        {
            Assert.Equal(0, NumberHelper<Int128>.Sign(Zero));
            Assert.Equal(1, NumberHelper<Int128>.Sign(One));
            Assert.Equal(1, NumberHelper<Int128>.Sign(MaxValue));
            Assert.Equal(-1, NumberHelper<Int128>.Sign(MinValue));
            Assert.Equal(-1, NumberHelper<Int128>.Sign(NegativeOne));
        }

        //
        // INumberBase
        //

        [Fact]
        public static void OneTest()
        {
            Assert.Equal(One, NumberBaseHelper<Int128>.One);
        }

        [Fact]
        public static void RadixTest()
        {
            Assert.Equal(2, NumberBaseHelper<Int128>.Radix);
        }

        [Fact]
        public static void ZeroTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<Int128>.Zero);
        }

        [Fact]
        public static void AbsTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<Int128>.Abs(Zero));
            Assert.Equal(One, NumberBaseHelper<Int128>.Abs(One));
            Assert.Equal(MaxValue, NumberBaseHelper<Int128>.Abs(MaxValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<Int128>.Abs(MinValue));
            Assert.Equal(One, NumberBaseHelper<Int128>.Abs(NegativeOne));
        }

        [Fact]
        public static void CreateCheckedFromByteTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateChecked<byte>(0x00));
            Assert.Equal(One, NumberBaseHelper<Int128>.CreateChecked<byte>(0x01));
            Assert.Equal(SByteMaxValue, NumberBaseHelper<Int128>.CreateChecked<byte>(0x7F));
            Assert.Equal(SByteMaxValuePlusOne, NumberBaseHelper<Int128>.CreateChecked<byte>(0x80));
            Assert.Equal(ByteMaxValue, NumberBaseHelper<Int128>.CreateChecked<byte>(0xFF));
        }

        [Fact]
        public static void CreateCheckedFromCharTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateChecked<char>((char)0x0000));
            Assert.Equal(One, NumberBaseHelper<Int128>.CreateChecked<char>((char)0x0001));
            Assert.Equal(Int16MaxValue, NumberBaseHelper<Int128>.CreateChecked<char>((char)0x7FFF));
            Assert.Equal(Int16MaxValuePlusOne, NumberBaseHelper<Int128>.CreateChecked<char>((char)0x8000));
            Assert.Equal(UInt16MaxValue, NumberBaseHelper<Int128>.CreateChecked<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateCheckedFromDecimalTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateChecked<decimal>(decimal.Zero));

            Assert.Equal(One, NumberBaseHelper<Int128>.CreateChecked<decimal>(decimal.One));
            Assert.Equal(NegativeOne, NumberBaseHelper<Int128>.CreateChecked<decimal>(decimal.MinusOne));

            Assert.Equal(new Int128(0x0000_0000_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), NumberBaseHelper<Int128>.CreateChecked<decimal>(decimal.MaxValue));
            Assert.Equal(new Int128(0xFFFF_FFFF_0000_0000, 0x0000_0000_0000_0001), NumberBaseHelper<Int128>.CreateChecked<decimal>(decimal.MinValue));
        }

        [Fact]
        public static void CreateCheckedFromDoubleTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateChecked<double>(+0.0));
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateChecked<double>(-0.0));

            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateChecked<double>(+double.Epsilon));
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateChecked<double>(-double.Epsilon));

            Assert.Equal(One, NumberBaseHelper<Int128>.CreateChecked<double>(+1.0));
            Assert.Equal(NegativeOne, NumberBaseHelper<Int128>.CreateChecked<double>(-1.0));

            Assert.Equal(new Int128(0x7FFF_FFFF_FFFF_FC00, 0x0000_0000_0000_0000), NumberBaseHelper<Int128>.CreateChecked<double>(+170141183460469212842221372237303250944.0));
            Assert.Equal(new Int128(0x8000_0000_0000_0400, 0x0000_0000_0000_0000), NumberBaseHelper<Int128>.CreateChecked<double>(-170141183460469212842221372237303250944.0));

            Assert.Equal(MinValue, NumberBaseHelper<Int128>.CreateChecked<double>(-170141183460469231731687303715884105728.0));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<Int128>.CreateChecked<double>(+170141183460469231731687303715884105728.0));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<Int128>.CreateChecked<double>(-170141183460469269510619166673045815296.0));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<Int128>.CreateChecked<double>(double.MaxValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<Int128>.CreateChecked<double>(double.MinValue));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<Int128>.CreateChecked<double>(double.PositiveInfinity));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<Int128>.CreateChecked<double>(double.NegativeInfinity));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<Int128>.CreateChecked<double>(double.NaN));
        }

        [Fact]
        public static void CreateCheckedFromHalfTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateChecked<Half>((Half)(+0.0)));
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateChecked<Half>((Half)(-0.0)));

            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateChecked<Half>(+Half.Epsilon));
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateChecked<Half>(-Half.Epsilon));

            Assert.Equal(One, NumberBaseHelper<Int128>.CreateChecked<Half>((Half)(+1.0)));
            Assert.Equal(NegativeOne, NumberBaseHelper<Int128>.CreateChecked<Half>((Half)(-1.0)));

            Assert.Equal(+65504, NumberBaseHelper<Int128>.CreateChecked<Half>(Half.MaxValue));
            Assert.Equal(-65504, NumberBaseHelper<Int128>.CreateChecked<Half>(Half.MinValue));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<Int128>.CreateChecked<Half>(Half.PositiveInfinity));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<Int128>.CreateChecked<Half>(Half.NegativeInfinity));
        }

        [Fact]
        public static void CreateCheckedFromInt16Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateChecked<short>(0x0000));
            Assert.Equal(One, NumberBaseHelper<Int128>.CreateChecked<short>(0x0001));
            Assert.Equal(Int16MaxValue, NumberBaseHelper<Int128>.CreateChecked<short>(0x7FFF));
            Assert.Equal(Int16MinValue, NumberBaseHelper<Int128>.CreateChecked<short>(unchecked((short)0x8000)));
            Assert.Equal(NegativeOne, NumberBaseHelper<Int128>.CreateChecked<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromInt32Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateChecked<int>(0x00000000));
            Assert.Equal(One, NumberBaseHelper<Int128>.CreateChecked<int>(0x00000001));
            Assert.Equal(Int32MaxValue, NumberBaseHelper<Int128>.CreateChecked<int>(0x7FFFFFFF));
            Assert.Equal(Int32MinValue, NumberBaseHelper<Int128>.CreateChecked<int>(unchecked((int)0x80000000)));
            Assert.Equal(NegativeOne, NumberBaseHelper<Int128>.CreateChecked<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromInt64Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateChecked<long>(0x0000000000000000));
            Assert.Equal(One, NumberBaseHelper<Int128>.CreateChecked<long>(0x0000000000000001));
            Assert.Equal(Int64MaxValue, NumberBaseHelper<Int128>.CreateChecked<long>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal(Int64MinValue, NumberBaseHelper<Int128>.CreateChecked<long>(unchecked((long)0x8000000000000000)));
            Assert.Equal(NegativeOne, NumberBaseHelper<Int128>.CreateChecked<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromInt128Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateChecked<Int128>(Int128.Zero));
            Assert.Equal(One, NumberBaseHelper<Int128>.CreateChecked<Int128>(Int128.One));
            Assert.Equal(MaxValue, NumberBaseHelper<Int128>.CreateChecked<Int128>(Int128.MaxValue));
            Assert.Equal(MinValue, NumberBaseHelper<Int128>.CreateChecked<Int128>(Int128.MinValue));
            Assert.Equal(NegativeOne, NumberBaseHelper<Int128>.CreateChecked<Int128>(Int128.NegativeOne));
        }

        [Fact]
        public static void CreateCheckedFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateChecked<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal(One, NumberBaseHelper<Int128>.CreateChecked<nint>(unchecked((nint)0x0000000000000001)));
                Assert.Equal(Int64MaxValue, NumberBaseHelper<Int128>.CreateChecked<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(Int64MinValue, NumberBaseHelper<Int128>.CreateChecked<nint>(unchecked((nint)0x8000000000000000)));
                Assert.Equal(NegativeOne, NumberBaseHelper<Int128>.CreateChecked<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateChecked<nint>((nint)0x00000000));
                Assert.Equal(One, NumberBaseHelper<Int128>.CreateChecked<nint>((nint)0x00000001));
                Assert.Equal(Int32MaxValue, NumberBaseHelper<Int128>.CreateChecked<nint>((nint)0x7FFFFFFF));
                Assert.Equal(Int32MinValue, NumberBaseHelper<Int128>.CreateChecked<nint>(unchecked((nint)0x80000000)));
                Assert.Equal(NegativeOne, NumberBaseHelper<Int128>.CreateChecked<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateCheckedFromNFloatTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateChecked<NFloat>(+0.0f));
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateChecked<NFloat>(-0.0f));

            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateChecked<NFloat>(+NFloat.Epsilon));
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateChecked<NFloat>(-NFloat.Epsilon));

            Assert.Equal(One, NumberBaseHelper<Int128>.CreateChecked<NFloat>(+1.0f));
            Assert.Equal(NegativeOne, NumberBaseHelper<Int128>.CreateChecked<NFloat>(-1.0f));

            if (Environment.Is64BitProcess)
            {
                Assert.Equal(new Int128(0x7FFF_FFFF_FFFF_FC00, 0x0000_0000_0000_0000), NumberBaseHelper<Int128>.CreateChecked<NFloat>((NFloat)(+170141183460469212842221372237303250944.0)));
                Assert.Equal(new Int128(0x8000_0000_0000_0400, 0x0000_0000_0000_0000), NumberBaseHelper<Int128>.CreateChecked<NFloat>((NFloat)(-170141183460469212842221372237303250944.0)));

                Assert.Equal(MinValue, NumberBaseHelper<Int128>.CreateChecked<NFloat>((NFloat)(-170141183460469231731687303715884105728.0)));

                Assert.Throws<OverflowException>(() => NumberBaseHelper<Int128>.CreateChecked<NFloat>((NFloat)(+170141183460469231731687303715884105728.0)));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<Int128>.CreateChecked<NFloat>((NFloat)(-170141183460469269510619166673045815296.0)));
            }
            else
            {
                Assert.Equal(new Int128(0x7FFF_FF80_0000_0000, 0x0000_0000_0000_0000), NumberBaseHelper<Int128>.CreateChecked<NFloat>(+170141173319264429905852091742258462720.0f));
                Assert.Equal(new Int128(0x8000_0080_0000_0000, 0x0000_0000_0000_0000), NumberBaseHelper<Int128>.CreateChecked<NFloat>(-170141173319264429905852091742258462720.0f));

                Assert.Equal(MinValue, NumberBaseHelper<Int128>.CreateChecked<NFloat>(-170141183460469231731687303715884105728.0f));

                Assert.Throws<OverflowException>(() => NumberBaseHelper<Int128>.CreateChecked<NFloat>(+170141183460469231731687303715884105728.0f));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<Int128>.CreateChecked<NFloat>(-170141203742878835383357727663135391744.0f));
            }

            Assert.Throws<OverflowException>(() => NumberBaseHelper<Int128>.CreateChecked<NFloat>(NFloat.MaxValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<Int128>.CreateChecked<NFloat>(NFloat.MinValue));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<Int128>.CreateChecked<NFloat>(NFloat.PositiveInfinity));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<Int128>.CreateChecked<NFloat>(NFloat.NegativeInfinity));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<Int128>.CreateChecked<NFloat>(NFloat.NaN));
        }

        [Fact]
        public static void CreateCheckedFromSByteTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateChecked<sbyte>(0x00));
            Assert.Equal(One, NumberBaseHelper<Int128>.CreateChecked<sbyte>(0x01));
            Assert.Equal(SByteMaxValue, NumberBaseHelper<Int128>.CreateChecked<sbyte>(0x7F));
            Assert.Equal(SByteMinValue, NumberBaseHelper<Int128>.CreateChecked<sbyte>(unchecked((sbyte)0x80)));
            Assert.Equal(NegativeOne, NumberBaseHelper<Int128>.CreateChecked<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateCheckedFromSingleTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateChecked<float>(+0.0f));
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateChecked<float>(-0.0f));

            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateChecked<float>(+float.Epsilon));
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateChecked<float>(-float.Epsilon));

            Assert.Equal(One, NumberBaseHelper<Int128>.CreateChecked<float>(+1.0f));
            Assert.Equal(NegativeOne, NumberBaseHelper<Int128>.CreateChecked<float>(-1.0f));

            Assert.Equal(new Int128(0x7FFF_FF80_0000_0000, 0x0000_0000_0000_0000), NumberBaseHelper<Int128>.CreateChecked<float>(+170141173319264429905852091742258462720.0f));
            Assert.Equal(new Int128(0x8000_0080_0000_0000, 0x0000_0000_0000_0000), NumberBaseHelper<Int128>.CreateChecked<float>(-170141173319264429905852091742258462720.0f));

            Assert.Equal(MinValue, NumberBaseHelper<Int128>.CreateChecked<float>(-170141183460469231731687303715884105728.0f));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<Int128>.CreateChecked<float>(+170141183460469231731687303715884105728.0f));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<Int128>.CreateChecked<float>(-170141203742878835383357727663135391744.0f));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<Int128>.CreateChecked<float>(float.MaxValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<Int128>.CreateChecked<float>(float.MinValue));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<Int128>.CreateChecked<float>(float.PositiveInfinity));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<Int128>.CreateChecked<float>(float.NegativeInfinity));
        }

        [Fact]
        public static void CreateCheckedFromUInt16Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateChecked<ushort>(0x0000));
            Assert.Equal(One, NumberBaseHelper<Int128>.CreateChecked<ushort>(0x0001));
            Assert.Equal(Int16MaxValue, NumberBaseHelper<Int128>.CreateChecked<ushort>(0x7FFF));
            Assert.Equal(Int16MaxValuePlusOne, NumberBaseHelper<Int128>.CreateChecked<ushort>(0x8000));
            Assert.Equal(UInt16MaxValue, NumberBaseHelper<Int128>.CreateChecked<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUInt32Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateChecked<uint>(0x00000000));
            Assert.Equal(One, NumberBaseHelper<Int128>.CreateChecked<uint>(0x00000001));
            Assert.Equal(Int32MaxValue, NumberBaseHelper<Int128>.CreateChecked<uint>(0x7FFFFFFF));
            Assert.Equal(Int32MaxValuePlusOne, NumberBaseHelper<Int128>.CreateChecked<uint>(0x80000000));
            Assert.Equal(UInt32MaxValue, NumberBaseHelper<Int128>.CreateChecked<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUInt64Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateChecked<ulong>(0x0000000000000000));
            Assert.Equal(One, NumberBaseHelper<Int128>.CreateChecked<ulong>(0x0000000000000001));
            Assert.Equal(Int64MaxValue, NumberBaseHelper<Int128>.CreateChecked<ulong>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal(Int64MaxValuePlusOne, NumberBaseHelper<Int128>.CreateChecked<ulong>(0x8000000000000000));
            Assert.Equal(UInt64MaxValue, NumberBaseHelper<Int128>.CreateChecked<ulong>(0xFFFFFFFFFFFFFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUInt128Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateChecked<UInt128>(UInt128.Zero));
            Assert.Equal(One, NumberBaseHelper<Int128>.CreateChecked<UInt128>(UInt128.One));
            Assert.Equal(MaxValue, NumberBaseHelper<Int128>.CreateChecked<UInt128>(UInt128Tests_GenericMath.Int128MaxValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<Int128>.CreateChecked<UInt128>(UInt128Tests_GenericMath.Int128MaxValuePlusOne));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<Int128>.CreateChecked<UInt128>(UInt128.MaxValue));
        }

        [Fact]
        public static void CreateCheckedFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateChecked<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal(One, NumberBaseHelper<Int128>.CreateChecked<nuint>(unchecked((nuint)0x0000000000000001)));
                Assert.Equal(Int64MaxValue, NumberBaseHelper<Int128>.CreateChecked<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(Int64MaxValuePlusOne, NumberBaseHelper<Int128>.CreateChecked<nuint>(unchecked((nuint)0x8000000000000000)));
                Assert.Equal(UInt64MaxValue, NumberBaseHelper<Int128>.CreateChecked<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateChecked<nuint>((nuint)0x00000000));
                Assert.Equal(One, NumberBaseHelper<Int128>.CreateChecked<nuint>((nuint)0x00000001));
                Assert.Equal(Int32MaxValue, NumberBaseHelper<Int128>.CreateChecked<nuint>((nuint)0x7FFFFFFF));
                Assert.Equal(Int32MaxValuePlusOne, NumberBaseHelper<Int128>.CreateChecked<nuint>((nuint)0x80000000));
                Assert.Equal(UInt32MaxValue, NumberBaseHelper<Int128>.CreateChecked<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateSaturatingFromByteTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateSaturating<byte>(0x00));
            Assert.Equal(One, NumberBaseHelper<Int128>.CreateSaturating<byte>(0x01));
            Assert.Equal(SByteMaxValue, NumberBaseHelper<Int128>.CreateSaturating<byte>(0x7F));
            Assert.Equal(SByteMaxValuePlusOne, NumberBaseHelper<Int128>.CreateSaturating<byte>(0x80));
            Assert.Equal(ByteMaxValue, NumberBaseHelper<Int128>.CreateSaturating<byte>(0xFF));
        }

        [Fact]
        public static void CreateSaturatingFromCharTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateSaturating<char>((char)0x0000));
            Assert.Equal(One, NumberBaseHelper<Int128>.CreateSaturating<char>((char)0x0001));
            Assert.Equal(Int16MaxValue, NumberBaseHelper<Int128>.CreateSaturating<char>((char)0x7FFF));
            Assert.Equal(Int16MaxValuePlusOne, NumberBaseHelper<Int128>.CreateSaturating<char>((char)0x8000));
            Assert.Equal(UInt16MaxValue, NumberBaseHelper<Int128>.CreateSaturating<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromDecimalTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateSaturating<decimal>(decimal.Zero));

            Assert.Equal(One, NumberBaseHelper<Int128>.CreateSaturating<decimal>(decimal.One));
            Assert.Equal(NegativeOne, NumberBaseHelper<Int128>.CreateSaturating<decimal>(decimal.MinusOne));

            Assert.Equal(new Int128(0x0000_0000_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), NumberBaseHelper<Int128>.CreateSaturating<decimal>(decimal.MaxValue));
            Assert.Equal(new Int128(0xFFFF_FFFF_0000_0000, 0x0000_0000_0000_0001), NumberBaseHelper<Int128>.CreateSaturating<decimal>(decimal.MinValue));
        }

        [Fact]
        public static void CreateSaturatingFromDoubleTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateSaturating<double>(+0.0));
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateSaturating<double>(-0.0));

            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateSaturating<double>(+double.Epsilon));
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateSaturating<double>(-double.Epsilon));

            Assert.Equal(One, NumberBaseHelper<Int128>.CreateSaturating<double>(+1.0));
            Assert.Equal(NegativeOne, NumberBaseHelper<Int128>.CreateSaturating<double>(-1.0));

            Assert.Equal(new Int128(0x7FFF_FFFF_FFFF_FC00, 0x0000_0000_0000_0000), NumberBaseHelper<Int128>.CreateSaturating<double>(+170141183460469212842221372237303250944.0));
            Assert.Equal(new Int128(0x8000_0000_0000_0400, 0x0000_0000_0000_0000), NumberBaseHelper<Int128>.CreateSaturating<double>(-170141183460469212842221372237303250944.0));
            Assert.Equal(MinValue, NumberBaseHelper<Int128>.CreateSaturating<double>(-170141183460469231731687303715884105728.0));

            Assert.Equal(MaxValue, NumberBaseHelper<Int128>.CreateSaturating<double>(+170141183460469231731687303715884105728.0));
            Assert.Equal(MinValue, NumberBaseHelper<Int128>.CreateSaturating<double>(-170141183460469269510619166673045815296.0));

            Assert.Equal(MaxValue, NumberBaseHelper<Int128>.CreateSaturating<double>(double.MaxValue));
            Assert.Equal(MinValue, NumberBaseHelper<Int128>.CreateSaturating<double>(double.MinValue));

            Assert.Equal(MaxValue, NumberBaseHelper<Int128>.CreateSaturating<double>(double.PositiveInfinity));
            Assert.Equal(MinValue, NumberBaseHelper<Int128>.CreateSaturating<double>(double.NegativeInfinity));
        }

        [Fact]
        public static void CreateSaturatingFromHalfTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateSaturating<Half>((Half)(+0.0)));
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateSaturating<Half>((Half)(-0.0)));

            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateSaturating<Half>(+Half.Epsilon));
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateSaturating<Half>(-Half.Epsilon));

            Assert.Equal(One, NumberBaseHelper<Int128>.CreateSaturating<Half>((Half)(+1.0)));
            Assert.Equal(NegativeOne, NumberBaseHelper<Int128>.CreateSaturating<Half>((Half)(-1.0)));

            Assert.Equal(+65504, NumberBaseHelper<Int128>.CreateSaturating<Half>(Half.MaxValue));
            Assert.Equal(-65504, NumberBaseHelper<Int128>.CreateSaturating<Half>(Half.MinValue));

            Assert.Equal(MaxValue, NumberBaseHelper<Int128>.CreateSaturating<Half>(Half.PositiveInfinity));
            Assert.Equal(MinValue, NumberBaseHelper<Int128>.CreateSaturating<Half>(Half.NegativeInfinity));
        }

        [Fact]
        public static void CreateSaturatingFromInt16Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateSaturating<short>(0x0000));
            Assert.Equal(One, NumberBaseHelper<Int128>.CreateSaturating<short>(0x0001));
            Assert.Equal(Int16MaxValue, NumberBaseHelper<Int128>.CreateSaturating<short>(0x7FFF));
            Assert.Equal(Int16MinValue, NumberBaseHelper<Int128>.CreateSaturating<short>(unchecked((short)0x8000)));
            Assert.Equal(NegativeOne, NumberBaseHelper<Int128>.CreateSaturating<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromInt32Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateSaturating<int>(0x00000000));
            Assert.Equal(One, NumberBaseHelper<Int128>.CreateSaturating<int>(0x00000001));
            Assert.Equal(Int32MaxValue, NumberBaseHelper<Int128>.CreateSaturating<int>(0x7FFFFFFF));
            Assert.Equal(Int32MinValue, NumberBaseHelper<Int128>.CreateSaturating<int>(unchecked((int)0x80000000)));
            Assert.Equal(NegativeOne, NumberBaseHelper<Int128>.CreateSaturating<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromInt64Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateSaturating<long>(0x0000000000000000));
            Assert.Equal(One, NumberBaseHelper<Int128>.CreateSaturating<long>(0x0000000000000001));
            Assert.Equal(Int64MaxValue, NumberBaseHelper<Int128>.CreateSaturating<long>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal(Int64MinValue, NumberBaseHelper<Int128>.CreateSaturating<long>(unchecked((long)0x8000000000000000)));
            Assert.Equal(NegativeOne, NumberBaseHelper<Int128>.CreateSaturating<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromInt128Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateSaturating<Int128>(Int128.Zero));
            Assert.Equal(One, NumberBaseHelper<Int128>.CreateSaturating<Int128>(Int128.One));
            Assert.Equal(MaxValue, NumberBaseHelper<Int128>.CreateSaturating<Int128>(Int128.MaxValue));
            Assert.Equal(MinValue, NumberBaseHelper<Int128>.CreateSaturating<Int128>(Int128.MinValue));
            Assert.Equal(NegativeOne, NumberBaseHelper<Int128>.CreateSaturating<Int128>(Int128.NegativeOne));
        }

        [Fact]
        public static void CreateSaturatingFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateSaturating<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal(One, NumberBaseHelper<Int128>.CreateSaturating<nint>(unchecked((nint)0x0000000000000001)));
                Assert.Equal(Int64MaxValue, NumberBaseHelper<Int128>.CreateSaturating<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(Int64MinValue, NumberBaseHelper<Int128>.CreateSaturating<nint>(unchecked((nint)0x8000000000000000)));
                Assert.Equal(NegativeOne, NumberBaseHelper<Int128>.CreateSaturating<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateSaturating<nint>((nint)0x00000000));
                Assert.Equal(One, NumberBaseHelper<Int128>.CreateSaturating<nint>((nint)0x00000001));
                Assert.Equal(Int32MaxValue, NumberBaseHelper<Int128>.CreateSaturating<nint>((nint)0x7FFFFFFF));
                Assert.Equal(Int32MinValue, NumberBaseHelper<Int128>.CreateSaturating<nint>(unchecked((nint)0x80000000)));
                Assert.Equal(NegativeOne, NumberBaseHelper<Int128>.CreateSaturating<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateSaturatingFromNFloatTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateSaturating<NFloat>(+0.0f));
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateSaturating<NFloat>(-0.0f));

            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateSaturating<NFloat>(+NFloat.Epsilon));
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateSaturating<NFloat>(-NFloat.Epsilon));

            Assert.Equal(One, NumberBaseHelper<Int128>.CreateSaturating<NFloat>(+1.0f));
            Assert.Equal(NegativeOne, NumberBaseHelper<Int128>.CreateSaturating<NFloat>(-1.0f));

            if (Environment.Is64BitProcess)
            {
                Assert.Equal(new Int128(0x7FFF_FFFF_FFFF_FC00, 0x0000_0000_0000_0000), NumberBaseHelper<Int128>.CreateSaturating<NFloat>((NFloat)(+170141183460469212842221372237303250944.0)));
                Assert.Equal(new Int128(0x8000_0000_0000_0400, 0x0000_0000_0000_0000), NumberBaseHelper<Int128>.CreateSaturating<NFloat>((NFloat)(-170141183460469212842221372237303250944.0)));

                Assert.Equal(MinValue, NumberBaseHelper<Int128>.CreateSaturating<NFloat>((NFloat)(-170141183460469231731687303715884105728.0)));

                Assert.Equal(MaxValue, NumberBaseHelper<Int128>.CreateSaturating<NFloat>((NFloat)(+170141183460469231731687303715884105728.0)));
                Assert.Equal(MinValue, NumberBaseHelper<Int128>.CreateSaturating<NFloat>((NFloat)(-170141183460469269510619166673045815296.0)));
            }
            else
            {
                Assert.Equal(new Int128(0x7FFF_FF80_0000_0000, 0x0000_0000_0000_0000), NumberBaseHelper<Int128>.CreateSaturating<NFloat>(+170141173319264429905852091742258462720.0f));
                Assert.Equal(new Int128(0x8000_0080_0000_0000, 0x0000_0000_0000_0000), NumberBaseHelper<Int128>.CreateSaturating<NFloat>(-170141173319264429905852091742258462720.0f));

                Assert.Equal(MinValue, NumberBaseHelper<Int128>.CreateSaturating<NFloat>(-170141183460469231731687303715884105728.0f));

                Assert.Equal(MaxValue, NumberBaseHelper<Int128>.CreateSaturating<NFloat>(+170141183460469231731687303715884105728.0f));
                Assert.Equal(MinValue, NumberBaseHelper<Int128>.CreateSaturating<NFloat>(-170141203742878835383357727663135391744.0f));
            }

            Assert.Equal(MaxValue, NumberBaseHelper<Int128>.CreateSaturating<NFloat>(NFloat.MaxValue));
            Assert.Equal(MinValue, NumberBaseHelper<Int128>.CreateSaturating<NFloat>(NFloat.MinValue));

            Assert.Equal(MaxValue, NumberBaseHelper<Int128>.CreateSaturating<NFloat>(NFloat.PositiveInfinity));
            Assert.Equal(MinValue, NumberBaseHelper<Int128>.CreateSaturating<NFloat>(NFloat.NegativeInfinity));

            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateSaturating<NFloat>(NFloat.NaN));
        }

        [Fact]
        public static void CreateSaturatingFromSByteTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateSaturating<sbyte>(0x00));
            Assert.Equal(One, NumberBaseHelper<Int128>.CreateSaturating<sbyte>(0x01));
            Assert.Equal(SByteMaxValue, NumberBaseHelper<Int128>.CreateSaturating<sbyte>(0x7F));
            Assert.Equal(SByteMinValue, NumberBaseHelper<Int128>.CreateSaturating<sbyte>(unchecked((sbyte)0x80)));
            Assert.Equal(NegativeOne, NumberBaseHelper<Int128>.CreateSaturating<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateSaturatingFromSingleTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateSaturating<float>(+0.0f));
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateSaturating<float>(-0.0f));

            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateSaturating<float>(+float.Epsilon));
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateSaturating<float>(-float.Epsilon));

            Assert.Equal(One, NumberBaseHelper<Int128>.CreateSaturating<float>(+1.0f));
            Assert.Equal(NegativeOne, NumberBaseHelper<Int128>.CreateSaturating<float>(-1.0f));

            Assert.Equal(new Int128(0x7FFF_FF80_0000_0000, 0x0000_0000_0000_0000), NumberBaseHelper<Int128>.CreateSaturating<float>(+170141173319264429905852091742258462720.0f));
            Assert.Equal(new Int128(0x8000_0080_0000_0000, 0x0000_0000_0000_0000), NumberBaseHelper<Int128>.CreateSaturating<float>(-170141173319264429905852091742258462720.0f));
            Assert.Equal(MinValue, NumberBaseHelper<Int128>.CreateSaturating<float>(-170141183460469231731687303715884105728.0f));

            Assert.Equal(MaxValue, NumberBaseHelper<Int128>.CreateSaturating<float>(+170141183460469231731687303715884105728.0f));
            Assert.Equal(MinValue, NumberBaseHelper<Int128>.CreateSaturating<float>(-170141203742878835383357727663135391744.0f));

            Assert.Equal(MaxValue, NumberBaseHelper<Int128>.CreateSaturating<float>(float.MaxValue));
            Assert.Equal(MinValue, NumberBaseHelper<Int128>.CreateSaturating<float>(float.MinValue));

            Assert.Equal(MaxValue, NumberBaseHelper<Int128>.CreateSaturating<float>(float.PositiveInfinity));
            Assert.Equal(MinValue, NumberBaseHelper<Int128>.CreateSaturating<float>(float.NegativeInfinity));
        }

        [Fact]
        public static void CreateSaturatingFromUInt16Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateSaturating<ushort>(0x0000));
            Assert.Equal(One, NumberBaseHelper<Int128>.CreateSaturating<ushort>(0x0001));
            Assert.Equal(Int16MaxValue, NumberBaseHelper<Int128>.CreateSaturating<ushort>(0x7FFF));
            Assert.Equal(Int16MaxValuePlusOne, NumberBaseHelper<Int128>.CreateSaturating<ushort>(0x8000));
            Assert.Equal(UInt16MaxValue, NumberBaseHelper<Int128>.CreateSaturating<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUInt32Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateSaturating<uint>(0x00000000));
            Assert.Equal(One, NumberBaseHelper<Int128>.CreateSaturating<uint>(0x00000001));
            Assert.Equal(Int32MaxValue, NumberBaseHelper<Int128>.CreateSaturating<uint>(0x7FFFFFFF));
            Assert.Equal(Int32MaxValuePlusOne, NumberBaseHelper<Int128>.CreateSaturating<uint>(0x80000000));
            Assert.Equal(UInt32MaxValue, NumberBaseHelper<Int128>.CreateSaturating<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUInt64Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateSaturating<ulong>(0x0000000000000000));
            Assert.Equal(One, NumberBaseHelper<Int128>.CreateSaturating<ulong>(0x0000000000000001));
            Assert.Equal(Int64MaxValue, NumberBaseHelper<Int128>.CreateSaturating<ulong>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal(Int64MaxValuePlusOne, NumberBaseHelper<Int128>.CreateSaturating<ulong>(0x8000000000000000));
            Assert.Equal(UInt64MaxValue, NumberBaseHelper<Int128>.CreateSaturating<ulong>(0xFFFFFFFFFFFFFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUInt128Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateSaturating<UInt128>(UInt128.Zero));
            Assert.Equal(One, NumberBaseHelper<Int128>.CreateSaturating<UInt128>(UInt128.One));
            Assert.Equal(MaxValue, NumberBaseHelper<Int128>.CreateSaturating<UInt128>(UInt128Tests_GenericMath.Int128MaxValue));
            Assert.Equal(MaxValue, NumberBaseHelper<Int128>.CreateSaturating<UInt128>(UInt128Tests_GenericMath.Int128MaxValuePlusOne));
            Assert.Equal(MaxValue, NumberBaseHelper<Int128>.CreateSaturating<UInt128>(UInt128.MaxValue));
        }

        [Fact]
        public static void CreateSaturatingFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateSaturating<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal(One, NumberBaseHelper<Int128>.CreateSaturating<nuint>(unchecked((nuint)0x0000000000000001)));
                Assert.Equal(Int64MaxValue, NumberBaseHelper<Int128>.CreateSaturating<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(Int64MaxValuePlusOne, NumberBaseHelper<Int128>.CreateSaturating<nuint>(unchecked((nuint)0x8000000000000000)));
                Assert.Equal(UInt64MaxValue, NumberBaseHelper<Int128>.CreateSaturating<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateSaturating<nuint>((nuint)0x00000000));
                Assert.Equal(One, NumberBaseHelper<Int128>.CreateSaturating<nuint>((nuint)0x00000001));
                Assert.Equal(Int32MaxValue, NumberBaseHelper<Int128>.CreateSaturating<nuint>((nuint)0x7FFFFFFF));
                Assert.Equal(Int32MaxValuePlusOne, NumberBaseHelper<Int128>.CreateSaturating<nuint>((nuint)0x80000000));
                Assert.Equal(UInt32MaxValue, NumberBaseHelper<Int128>.CreateSaturating<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateTruncatingFromByteTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateTruncating<byte>(0x00));
            Assert.Equal(One, NumberBaseHelper<Int128>.CreateTruncating<byte>(0x01));
            Assert.Equal(SByteMaxValue, NumberBaseHelper<Int128>.CreateTruncating<byte>(0x7F));
            Assert.Equal(SByteMaxValuePlusOne, NumberBaseHelper<Int128>.CreateTruncating<byte>(0x80));
            Assert.Equal(ByteMaxValue, NumberBaseHelper<Int128>.CreateTruncating<byte>(0xFF));
        }

        [Fact]
        public static void CreateTruncatingFromCharTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateTruncating<char>((char)0x0000));
            Assert.Equal(One, NumberBaseHelper<Int128>.CreateTruncating<char>((char)0x0001));
            Assert.Equal(Int16MaxValue, NumberBaseHelper<Int128>.CreateTruncating<char>((char)0x7FFF));
            Assert.Equal(Int16MaxValuePlusOne, NumberBaseHelper<Int128>.CreateTruncating<char>((char)0x8000));
            Assert.Equal(UInt16MaxValue, NumberBaseHelper<Int128>.CreateTruncating<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromDecimalTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateTruncating<decimal>(decimal.Zero));

            Assert.Equal(One, NumberBaseHelper<Int128>.CreateTruncating<decimal>(decimal.One));
            Assert.Equal(NegativeOne, NumberBaseHelper<Int128>.CreateTruncating<decimal>(decimal.MinusOne));

            Assert.Equal(new Int128(0x0000_0000_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), NumberBaseHelper<Int128>.CreateTruncating<decimal>(decimal.MaxValue));
            Assert.Equal(new Int128(0xFFFF_FFFF_0000_0000, 0x0000_0000_0000_0001), NumberBaseHelper<Int128>.CreateTruncating<decimal>(decimal.MinValue));
        }

        [Fact]
        public static void CreateTruncatingFromDoubleTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateTruncating<double>(+0.0));
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateTruncating<double>(-0.0));

            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateTruncating<double>(+double.Epsilon));
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateTruncating<double>(-double.Epsilon));

            Assert.Equal(One, NumberBaseHelper<Int128>.CreateTruncating<double>(+1.0));
            Assert.Equal(NegativeOne, NumberBaseHelper<Int128>.CreateTruncating<double>(-1.0));

            Assert.Equal(new Int128(0x7FFF_FFFF_FFFF_FC00, 0x0000_0000_0000_0000), NumberBaseHelper<Int128>.CreateTruncating<double>(+170141183460469212842221372237303250944.0));
            Assert.Equal(new Int128(0x8000_0000_0000_0400, 0x0000_0000_0000_0000), NumberBaseHelper<Int128>.CreateTruncating<double>(-170141183460469212842221372237303250944.0));
            Assert.Equal(MinValue, NumberBaseHelper<Int128>.CreateTruncating<double>(-170141183460469231731687303715884105728.0));

            Assert.Equal(MaxValue, NumberBaseHelper<Int128>.CreateTruncating<double>(+170141183460469231731687303715884105728.0));
            Assert.Equal(MinValue, NumberBaseHelper<Int128>.CreateTruncating<double>(-170141183460469269510619166673045815296.0));

            Assert.Equal(MaxValue, NumberBaseHelper<Int128>.CreateTruncating<double>(double.MaxValue));
            Assert.Equal(MinValue, NumberBaseHelper<Int128>.CreateTruncating<double>(double.MinValue));

            Assert.Equal(MaxValue, NumberBaseHelper<Int128>.CreateTruncating<double>(double.PositiveInfinity));
            Assert.Equal(MinValue, NumberBaseHelper<Int128>.CreateTruncating<double>(double.NegativeInfinity));
        }

        [Fact]
        public static void CreateTruncatingFromHalfTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateTruncating<Half>((Half)(+0.0)));
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateTruncating<Half>((Half)(-0.0)));

            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateTruncating<Half>(+Half.Epsilon));
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateTruncating<Half>(-Half.Epsilon));

            Assert.Equal(One, NumberBaseHelper<Int128>.CreateTruncating<Half>((Half)(+1.0)));
            Assert.Equal(NegativeOne, NumberBaseHelper<Int128>.CreateTruncating<Half>((Half)(-1.0)));

            Assert.Equal(+65504, NumberBaseHelper<Int128>.CreateTruncating<Half>(Half.MaxValue));
            Assert.Equal(-65504, NumberBaseHelper<Int128>.CreateTruncating<Half>(Half.MinValue));

            Assert.Equal(MaxValue, NumberBaseHelper<Int128>.CreateTruncating<Half>(Half.PositiveInfinity));
            Assert.Equal(MinValue, NumberBaseHelper<Int128>.CreateTruncating<Half>(Half.NegativeInfinity));
        }

        [Fact]
        public static void CreateTruncatingFromInt16Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateTruncating<short>(0x0000));
            Assert.Equal(One, NumberBaseHelper<Int128>.CreateTruncating<short>(0x0001));
            Assert.Equal(Int16MaxValue, NumberBaseHelper<Int128>.CreateTruncating<short>(0x7FFF));
            Assert.Equal(Int16MinValue, NumberBaseHelper<Int128>.CreateTruncating<short>(unchecked((short)0x8000)));
            Assert.Equal(NegativeOne, NumberBaseHelper<Int128>.CreateTruncating<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateTruncatingFromInt32Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateTruncating<int>(0x00000000));
            Assert.Equal(One, NumberBaseHelper<Int128>.CreateTruncating<int>(0x00000001));
            Assert.Equal(Int32MaxValue, NumberBaseHelper<Int128>.CreateTruncating<int>(0x7FFFFFFF));
            Assert.Equal(Int32MinValue, NumberBaseHelper<Int128>.CreateTruncating<int>(unchecked((int)0x80000000)));
            Assert.Equal(NegativeOne, NumberBaseHelper<Int128>.CreateTruncating<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateTruncatingFromInt64Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateTruncating<long>(0x0000000000000000));
            Assert.Equal(One, NumberBaseHelper<Int128>.CreateTruncating<long>(0x0000000000000001));
            Assert.Equal(Int64MaxValue, NumberBaseHelper<Int128>.CreateTruncating<long>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal(Int64MinValue, NumberBaseHelper<Int128>.CreateTruncating<long>(unchecked((long)0x8000000000000000)));
            Assert.Equal(NegativeOne, NumberBaseHelper<Int128>.CreateTruncating<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void CreateTruncatingFromInt128Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateTruncating<Int128>(Int128.Zero));
            Assert.Equal(One, NumberBaseHelper<Int128>.CreateTruncating<Int128>(Int128.One));
            Assert.Equal(MaxValue, NumberBaseHelper<Int128>.CreateTruncating<Int128>(Int128.MaxValue));
            Assert.Equal(MinValue, NumberBaseHelper<Int128>.CreateTruncating<Int128>(Int128.MinValue));
            Assert.Equal(NegativeOne, NumberBaseHelper<Int128>.CreateTruncating<Int128>(Int128.NegativeOne));
        }

        [Fact]
        public static void CreateTruncatingFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateTruncating<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal(One, NumberBaseHelper<Int128>.CreateTruncating<nint>(unchecked((nint)0x0000000000000001)));
                Assert.Equal(Int64MaxValue, NumberBaseHelper<Int128>.CreateTruncating<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(Int64MinValue, NumberBaseHelper<Int128>.CreateTruncating<nint>(unchecked((nint)0x8000000000000000)));
                Assert.Equal(NegativeOne, NumberBaseHelper<Int128>.CreateTruncating<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateTruncating<nint>((nint)0x00000000));
                Assert.Equal(One, NumberBaseHelper<Int128>.CreateTruncating<nint>((nint)0x00000001));
                Assert.Equal(Int32MaxValue, NumberBaseHelper<Int128>.CreateTruncating<nint>((nint)0x7FFFFFFF));
                Assert.Equal(Int32MinValue, NumberBaseHelper<Int128>.CreateTruncating<nint>(unchecked((nint)0x80000000)));
                Assert.Equal(NegativeOne, NumberBaseHelper<Int128>.CreateTruncating<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateTruncatingFromNFloatTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateTruncating<NFloat>(+0.0f));
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateTruncating<NFloat>(-0.0f));

            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateTruncating<NFloat>(+NFloat.Epsilon));
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateTruncating<NFloat>(-NFloat.Epsilon));

            Assert.Equal(One, NumberBaseHelper<Int128>.CreateTruncating<NFloat>(+1.0f));
            Assert.Equal(NegativeOne, NumberBaseHelper<Int128>.CreateTruncating<NFloat>(-1.0f));

            if (Environment.Is64BitProcess)
            {
                Assert.Equal(new Int128(0x7FFF_FFFF_FFFF_FC00, 0x0000_0000_0000_0000), NumberBaseHelper<Int128>.CreateTruncating<NFloat>((NFloat)(+170141183460469212842221372237303250944.0)));
                Assert.Equal(new Int128(0x8000_0000_0000_0400, 0x0000_0000_0000_0000), NumberBaseHelper<Int128>.CreateTruncating<NFloat>((NFloat)(-170141183460469212842221372237303250944.0)));

                Assert.Equal(MinValue, NumberBaseHelper<Int128>.CreateTruncating<NFloat>((NFloat)(-170141183460469231731687303715884105728.0)));

                Assert.Equal(MaxValue, NumberBaseHelper<Int128>.CreateTruncating<NFloat>((NFloat)(+170141183460469231731687303715884105728.0)));
                Assert.Equal(MinValue, NumberBaseHelper<Int128>.CreateTruncating<NFloat>((NFloat)(-170141183460469269510619166673045815296.0)));
            }
            else
            {
                Assert.Equal(new Int128(0x7FFF_FF80_0000_0000, 0x0000_0000_0000_0000), NumberBaseHelper<Int128>.CreateTruncating<NFloat>(+170141173319264429905852091742258462720.0f));
                Assert.Equal(new Int128(0x8000_0080_0000_0000, 0x0000_0000_0000_0000), NumberBaseHelper<Int128>.CreateTruncating<NFloat>(-170141173319264429905852091742258462720.0f));

                Assert.Equal(MinValue, NumberBaseHelper<Int128>.CreateTruncating<NFloat>(-170141183460469231731687303715884105728.0f));

                Assert.Equal(MaxValue, NumberBaseHelper<Int128>.CreateTruncating<NFloat>(+170141183460469231731687303715884105728.0f));
                Assert.Equal(MinValue, NumberBaseHelper<Int128>.CreateTruncating<NFloat>(-170141203742878835383357727663135391744.0f));
            }

            Assert.Equal(MaxValue, NumberBaseHelper<Int128>.CreateTruncating<NFloat>(NFloat.MaxValue));
            Assert.Equal(MinValue, NumberBaseHelper<Int128>.CreateTruncating<NFloat>(NFloat.MinValue));

            Assert.Equal(MaxValue, NumberBaseHelper<Int128>.CreateTruncating<NFloat>(NFloat.PositiveInfinity));
            Assert.Equal(MinValue, NumberBaseHelper<Int128>.CreateTruncating<NFloat>(NFloat.NegativeInfinity));

            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateTruncating<NFloat>(NFloat.NaN));
        }

        [Fact]
        public static void CreateTruncatingFromSByteTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateTruncating<sbyte>(0x00));
            Assert.Equal(One, NumberBaseHelper<Int128>.CreateTruncating<sbyte>(0x01));
            Assert.Equal(SByteMaxValue, NumberBaseHelper<Int128>.CreateTruncating<sbyte>(0x7F));
            Assert.Equal(SByteMinValue, NumberBaseHelper<Int128>.CreateTruncating<sbyte>(unchecked((sbyte)0x80)));
            Assert.Equal(NegativeOne, NumberBaseHelper<Int128>.CreateTruncating<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateTruncatingFromSingleTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateTruncating<float>(+0.0f));
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateTruncating<float>(-0.0f));

            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateTruncating<float>(+float.Epsilon));
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateTruncating<float>(-float.Epsilon));

            Assert.Equal(One, NumberBaseHelper<Int128>.CreateTruncating<float>(+1.0f));
            Assert.Equal(NegativeOne, NumberBaseHelper<Int128>.CreateTruncating<float>(-1.0f));

            Assert.Equal(new Int128(0x7FFF_FF80_0000_0000, 0x0000_0000_0000_0000), NumberBaseHelper<Int128>.CreateTruncating<float>(+170141173319264429905852091742258462720.0f));
            Assert.Equal(new Int128(0x8000_0080_0000_0000, 0x0000_0000_0000_0000), NumberBaseHelper<Int128>.CreateTruncating<float>(-170141173319264429905852091742258462720.0f));
            Assert.Equal(MinValue, NumberBaseHelper<Int128>.CreateTruncating<float>(-170141183460469231731687303715884105728.0f));

            Assert.Equal(MaxValue, NumberBaseHelper<Int128>.CreateTruncating<float>(+170141183460469231731687303715884105728.0f));
            Assert.Equal(MinValue, NumberBaseHelper<Int128>.CreateTruncating<float>(-170141203742878835383357727663135391744.0f));

            Assert.Equal(MaxValue, NumberBaseHelper<Int128>.CreateTruncating<float>(float.MaxValue));
            Assert.Equal(MinValue, NumberBaseHelper<Int128>.CreateTruncating<float>(float.MinValue));

            Assert.Equal(MaxValue, NumberBaseHelper<Int128>.CreateTruncating<float>(float.PositiveInfinity));
            Assert.Equal(MinValue, NumberBaseHelper<Int128>.CreateTruncating<float>(float.NegativeInfinity));
        }

        [Fact]
        public static void CreateTruncatingFromUInt16Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateTruncating<ushort>(0x0000));
            Assert.Equal(One, NumberBaseHelper<Int128>.CreateTruncating<ushort>(0x0001));
            Assert.Equal(Int16MaxValue, NumberBaseHelper<Int128>.CreateTruncating<ushort>(0x7FFF));
            Assert.Equal(Int16MaxValuePlusOne, NumberBaseHelper<Int128>.CreateTruncating<ushort>(0x8000));
            Assert.Equal(UInt16MaxValue, NumberBaseHelper<Int128>.CreateTruncating<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt32Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateTruncating<uint>(0x00000000));
            Assert.Equal(One, NumberBaseHelper<Int128>.CreateTruncating<uint>(0x00000001));
            Assert.Equal(Int32MaxValue, NumberBaseHelper<Int128>.CreateTruncating<uint>(0x7FFFFFFF));
            Assert.Equal(Int32MaxValuePlusOne, NumberBaseHelper<Int128>.CreateTruncating<uint>(0x80000000));
            Assert.Equal(UInt32MaxValue, NumberBaseHelper<Int128>.CreateTruncating<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt64Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateTruncating<ulong>(0x0000000000000000));
            Assert.Equal(One, NumberBaseHelper<Int128>.CreateTruncating<ulong>(0x0000000000000001));
            Assert.Equal(Int64MaxValue, NumberBaseHelper<Int128>.CreateTruncating<ulong>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal(Int64MaxValuePlusOne, NumberBaseHelper<Int128>.CreateTruncating<ulong>(0x8000000000000000));
            Assert.Equal(UInt64MaxValue, NumberBaseHelper<Int128>.CreateTruncating<ulong>(0xFFFFFFFFFFFFFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt128Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateTruncating<UInt128>(UInt128.Zero));
            Assert.Equal(One, NumberBaseHelper<Int128>.CreateTruncating<UInt128>(UInt128.One));
            Assert.Equal(MaxValue, NumberBaseHelper<Int128>.CreateTruncating<UInt128>(UInt128Tests_GenericMath.Int128MaxValue));
            Assert.Equal(MinValue, NumberBaseHelper<Int128>.CreateTruncating<UInt128>(UInt128Tests_GenericMath.Int128MaxValuePlusOne));
            Assert.Equal(NegativeOne, NumberBaseHelper<Int128>.CreateTruncating<UInt128>(UInt128.MaxValue));
        }

        [Fact]
        public static void CreateTruncatingFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateTruncating<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal(One, NumberBaseHelper<Int128>.CreateTruncating<nuint>(unchecked((nuint)0x0000000000000001)));
                Assert.Equal(Int64MaxValue, NumberBaseHelper<Int128>.CreateTruncating<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(Int64MaxValuePlusOne, NumberBaseHelper<Int128>.CreateTruncating<nuint>(unchecked((nuint)0x8000000000000000)));
                Assert.Equal(UInt64MaxValue, NumberBaseHelper<Int128>.CreateTruncating<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateTruncating<nuint>((nuint)0x00000000));
                Assert.Equal(One, NumberBaseHelper<Int128>.CreateTruncating<nuint>((nuint)0x00000001));
                Assert.Equal(Int32MaxValue, NumberBaseHelper<Int128>.CreateTruncating<nuint>((nuint)0x7FFFFFFF));
                Assert.Equal(Int32MaxValuePlusOne, NumberBaseHelper<Int128>.CreateTruncating<nuint>((nuint)0x80000000));
                Assert.Equal(UInt32MaxValue, NumberBaseHelper<Int128>.CreateTruncating<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void IsCanonicalTest()
        {
            Assert.True(NumberBaseHelper<Int128>.IsCanonical(Zero));
            Assert.True(NumberBaseHelper<Int128>.IsCanonical(One));
            Assert.True(NumberBaseHelper<Int128>.IsCanonical(MaxValue));
            Assert.True(NumberBaseHelper<Int128>.IsCanonical(MinValue));
            Assert.True(NumberBaseHelper<Int128>.IsCanonical(NegativeOne));
        }

        [Fact]
        public static void IsComplexNumberTest()
        {
            Assert.False(NumberBaseHelper<Int128>.IsComplexNumber(Zero));
            Assert.False(NumberBaseHelper<Int128>.IsComplexNumber(One));
            Assert.False(NumberBaseHelper<Int128>.IsComplexNumber(MaxValue));
            Assert.False(NumberBaseHelper<Int128>.IsComplexNumber(MinValue));
            Assert.False(NumberBaseHelper<Int128>.IsComplexNumber(NegativeOne));
        }

        [Fact]
        public static void IsEvenIntegerTest()
        {
            Assert.True(NumberBaseHelper<Int128>.IsEvenInteger(Zero));
            Assert.False(NumberBaseHelper<Int128>.IsEvenInteger(One));
            Assert.False(NumberBaseHelper<Int128>.IsEvenInteger(MaxValue));
            Assert.True(NumberBaseHelper<Int128>.IsEvenInteger(MinValue));
            Assert.False(NumberBaseHelper<Int128>.IsEvenInteger(NegativeOne));
        }

        [Fact]
        public static void IsFiniteTest()
        {
            Assert.True(NumberBaseHelper<Int128>.IsFinite(Zero));
            Assert.True(NumberBaseHelper<Int128>.IsFinite(One));
            Assert.True(NumberBaseHelper<Int128>.IsFinite(MaxValue));
            Assert.True(NumberBaseHelper<Int128>.IsFinite(MinValue));
            Assert.True(NumberBaseHelper<Int128>.IsFinite(NegativeOne));
        }

        [Fact]
        public static void IsImaginaryNumberTest()
        {
            Assert.False(NumberBaseHelper<Int128>.IsImaginaryNumber(Zero));
            Assert.False(NumberBaseHelper<Int128>.IsImaginaryNumber(One));
            Assert.False(NumberBaseHelper<Int128>.IsImaginaryNumber(MaxValue));
            Assert.False(NumberBaseHelper<Int128>.IsImaginaryNumber(MinValue));
            Assert.False(NumberBaseHelper<Int128>.IsImaginaryNumber(NegativeOne));
        }

        [Fact]
        public static void IsInfinityTest()
        {
            Assert.False(NumberBaseHelper<Int128>.IsInfinity(Zero));
            Assert.False(NumberBaseHelper<Int128>.IsInfinity(One));
            Assert.False(NumberBaseHelper<Int128>.IsInfinity(MaxValue));
            Assert.False(NumberBaseHelper<Int128>.IsInfinity(MinValue));
            Assert.False(NumberBaseHelper<Int128>.IsInfinity(NegativeOne));
        }

        [Fact]
        public static void IsIntegerTest()
        {
            Assert.True(NumberBaseHelper<Int128>.IsInteger(Zero));
            Assert.True(NumberBaseHelper<Int128>.IsInteger(One));
            Assert.True(NumberBaseHelper<Int128>.IsInteger(MaxValue));
            Assert.True(NumberBaseHelper<Int128>.IsInteger(MinValue));
            Assert.True(NumberBaseHelper<Int128>.IsInteger(NegativeOne));
        }

        [Fact]
        public static void IsNaNTest()
        {
            Assert.False(NumberBaseHelper<Int128>.IsNaN(Zero));
            Assert.False(NumberBaseHelper<Int128>.IsNaN(One));
            Assert.False(NumberBaseHelper<Int128>.IsNaN(MaxValue));
            Assert.False(NumberBaseHelper<Int128>.IsNaN(MinValue));
            Assert.False(NumberBaseHelper<Int128>.IsNaN(NegativeOne));
        }

        [Fact]
        public static void IsNegativeTest()
        {
            Assert.False(NumberBaseHelper<Int128>.IsNegative(Zero));
            Assert.False(NumberBaseHelper<Int128>.IsNegative(One));
            Assert.False(NumberBaseHelper<Int128>.IsNegative(MaxValue));
            Assert.True(NumberBaseHelper<Int128>.IsNegative(MinValue));
            Assert.True(NumberBaseHelper<Int128>.IsNegative(NegativeOne));
        }

        [Fact]
        public static void IsNegativeInfinityTest()
        {
            Assert.False(NumberBaseHelper<Int128>.IsNegativeInfinity(Zero));
            Assert.False(NumberBaseHelper<Int128>.IsNegativeInfinity(One));
            Assert.False(NumberBaseHelper<Int128>.IsNegativeInfinity(MaxValue));
            Assert.False(NumberBaseHelper<Int128>.IsNegativeInfinity(MinValue));
            Assert.False(NumberBaseHelper<Int128>.IsNegativeInfinity(NegativeOne));
        }

        [Fact]
        public static void IsNormalTest()
        {
            Assert.False(NumberBaseHelper<Int128>.IsNormal(Zero));
            Assert.True(NumberBaseHelper<Int128>.IsNormal(One));
            Assert.True(NumberBaseHelper<Int128>.IsNormal(MaxValue));
            Assert.True(NumberBaseHelper<Int128>.IsNormal(MinValue));
            Assert.True(NumberBaseHelper<Int128>.IsNormal(NegativeOne));
        }

        [Fact]
        public static void IsOddIntegerTest()
        {
            Assert.False(NumberBaseHelper<Int128>.IsOddInteger(Zero));
            Assert.True(NumberBaseHelper<Int128>.IsOddInteger(One));
            Assert.True(NumberBaseHelper<Int128>.IsOddInteger(MaxValue));
            Assert.False(NumberBaseHelper<Int128>.IsOddInteger(MinValue));
            Assert.True(NumberBaseHelper<Int128>.IsOddInteger(NegativeOne));
        }

        [Fact]
        public static void IsPositiveTest()
        {
            Assert.True(NumberBaseHelper<Int128>.IsPositive(Zero));
            Assert.True(NumberBaseHelper<Int128>.IsPositive(One));
            Assert.True(NumberBaseHelper<Int128>.IsPositive(MaxValue));
            Assert.False(NumberBaseHelper<Int128>.IsPositive(MinValue));
            Assert.False(NumberBaseHelper<Int128>.IsPositive(NegativeOne));
        }

        [Fact]
        public static void IsPositiveInfinityTest()
        {
            Assert.False(NumberBaseHelper<Int128>.IsPositiveInfinity(Zero));
            Assert.False(NumberBaseHelper<Int128>.IsPositiveInfinity(One));
            Assert.False(NumberBaseHelper<Int128>.IsPositiveInfinity(MaxValue));
            Assert.False(NumberBaseHelper<Int128>.IsPositiveInfinity(MinValue));
            Assert.False(NumberBaseHelper<Int128>.IsPositiveInfinity(NegativeOne));
        }

        [Fact]
        public static void IsRealNumberTest()
        {
            Assert.True(NumberBaseHelper<Int128>.IsRealNumber(Zero));
            Assert.True(NumberBaseHelper<Int128>.IsRealNumber(One));
            Assert.True(NumberBaseHelper<Int128>.IsRealNumber(MaxValue));
            Assert.True(NumberBaseHelper<Int128>.IsRealNumber(MinValue));
            Assert.True(NumberBaseHelper<Int128>.IsRealNumber(NegativeOne));
        }

        [Fact]
        public static void IsSubnormalTest()
        {
            Assert.False(NumberBaseHelper<Int128>.IsSubnormal(Zero));
            Assert.False(NumberBaseHelper<Int128>.IsSubnormal(One));
            Assert.False(NumberBaseHelper<Int128>.IsSubnormal(MaxValue));
            Assert.False(NumberBaseHelper<Int128>.IsSubnormal(MinValue));
            Assert.False(NumberBaseHelper<Int128>.IsSubnormal(NegativeOne));
        }

        [Fact]
        public static void IsZeroTest()
        {
            Assert.True(NumberBaseHelper<Int128>.IsZero(Zero));
            Assert.False(NumberBaseHelper<Int128>.IsZero(One));
            Assert.False(NumberBaseHelper<Int128>.IsZero(MaxValue));
            Assert.False(NumberBaseHelper<Int128>.IsZero(MinValue));
            Assert.False(NumberBaseHelper<Int128>.IsZero(NegativeOne));
        }

        [Fact]
        public static void MaxMagnitudeTest()
        {
            Assert.Equal(One, NumberBaseHelper<Int128>.MaxMagnitude(Zero, 1));
            Assert.Equal(One, NumberBaseHelper<Int128>.MaxMagnitude(One, 1));
            Assert.Equal(MaxValue, NumberBaseHelper<Int128>.MaxMagnitude(MaxValue, 1));
            Assert.Equal(MinValue, NumberBaseHelper<Int128>.MaxMagnitude(MinValue, 1));
            Assert.Equal(One, NumberBaseHelper<Int128>.MaxMagnitude(NegativeOne, 1));
        }

        [Fact]
        public static void MaxMagnitudeNumberTest()
        {
            Assert.Equal(One, NumberBaseHelper<Int128>.MaxMagnitudeNumber(Zero, 1));
            Assert.Equal(One, NumberBaseHelper<Int128>.MaxMagnitudeNumber(One, 1));
            Assert.Equal(MaxValue, NumberBaseHelper<Int128>.MaxMagnitudeNumber(MaxValue, 1));
            Assert.Equal(MinValue, NumberBaseHelper<Int128>.MaxMagnitudeNumber(MinValue, 1));
            Assert.Equal(One, NumberBaseHelper<Int128>.MaxMagnitudeNumber(NegativeOne, 1));
        }

        [Fact]
        public static void MinMagnitudeTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<Int128>.MinMagnitude(Zero, 1));
            Assert.Equal(One, NumberBaseHelper<Int128>.MinMagnitude(One, 1));
            Assert.Equal(One, NumberBaseHelper<Int128>.MinMagnitude(MaxValue, 1));
            Assert.Equal(One, NumberBaseHelper<Int128>.MinMagnitude(MinValue, 1));
            Assert.Equal(NegativeOne, NumberBaseHelper<Int128>.MinMagnitude(NegativeOne, 1));
        }

        [Fact]
        public static void MinMagnitudeNumberTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<Int128>.MinMagnitudeNumber(Zero, 1));
            Assert.Equal(One, NumberBaseHelper<Int128>.MinMagnitudeNumber(One, 1));
            Assert.Equal(One, NumberBaseHelper<Int128>.MinMagnitudeNumber(MaxValue, 1));
            Assert.Equal(One, NumberBaseHelper<Int128>.MinMagnitudeNumber(MinValue, 1));
            Assert.Equal(NegativeOne, NumberBaseHelper<Int128>.MinMagnitudeNumber(NegativeOne, 1));
        }

        //
        // IShiftOperators
        //

        [Fact]
        public static void op_LeftShiftTest()
        {
            Assert.Equal(Zero, ShiftOperatorsHelper<Int128, int, Int128>.op_LeftShift(Zero, 1));
            Assert.Equal(Two, ShiftOperatorsHelper<Int128, int, Int128>.op_LeftShift(One, 1));
            Assert.Equal(NegativeTwo, ShiftOperatorsHelper<Int128, int, Int128>.op_LeftShift(MaxValue, 1));
            Assert.Equal(Zero, ShiftOperatorsHelper<Int128, int, Int128>.op_LeftShift(MinValue, 1));
            Assert.Equal(NegativeTwo, ShiftOperatorsHelper<Int128, int, Int128>.op_LeftShift(NegativeOne, 1));
        }

        [Fact]
        public static void op_RightShiftTest()
        {
            Assert.Equal(Zero, ShiftOperatorsHelper<Int128, int, Int128>.op_RightShift(Zero, 1));
            Assert.Equal(Zero, ShiftOperatorsHelper<Int128, int, Int128>.op_RightShift(One, 1));
            Assert.Equal(new Int128(0x3FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), ShiftOperatorsHelper<Int128, int, Int128>.op_RightShift(MaxValue, 1));
            Assert.Equal(new Int128(0xC000_0000_0000_0000, 0x0000_0000_0000_0000), ShiftOperatorsHelper<Int128, int, Int128>.op_RightShift(MinValue, 1));
            Assert.Equal(NegativeOne, ShiftOperatorsHelper<Int128, int, Int128>.op_RightShift(NegativeOne, 1));
        }

        [Fact]
        public static void op_UnsignedRightShiftTest()
        {
            Assert.Equal(Zero, ShiftOperatorsHelper<Int128, int, Int128>.op_UnsignedRightShift(Zero, 1));
            Assert.Equal(Zero, ShiftOperatorsHelper<Int128, int, Int128>.op_UnsignedRightShift(One, 1));
            Assert.Equal(new Int128(0x3FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), ShiftOperatorsHelper<Int128, int, Int128>.op_UnsignedRightShift(MaxValue, 1));
            Assert.Equal(new Int128(0x4000_0000_0000_0000, 0x0000_0000_0000_0000), ShiftOperatorsHelper<Int128, int, Int128>.op_UnsignedRightShift(MinValue, 1));
            Assert.Equal(MaxValue, ShiftOperatorsHelper<Int128, int, Int128>.op_UnsignedRightShift(NegativeOne, 1));
        }

        //
        // ISignedNumber
        //

        [Fact]
        public static void NegativeOneTest()
        {
            Assert.Equal(NegativeOne, SignedNumberHelper<Int128>.NegativeOne);
        }

        //
        // ISubtractionOperators
        //

        [Fact]
        public static void op_SubtractionTest()
        {
            Assert.Equal(NegativeOne, SubtractionOperatorsHelper<Int128, Int128, Int128>.op_Subtraction(Zero, 1));
            Assert.Equal(Zero, SubtractionOperatorsHelper<Int128, Int128, Int128>.op_Subtraction(One, 1));
            Assert.Equal(MaxValueMinusOne, SubtractionOperatorsHelper<Int128, Int128, Int128>.op_Subtraction(MaxValue, 1));
            Assert.Equal(MaxValue, SubtractionOperatorsHelper<Int128, Int128, Int128>.op_Subtraction(MinValue, 1));
            Assert.Equal(NegativeTwo, SubtractionOperatorsHelper<Int128, Int128, Int128>.op_Subtraction(NegativeOne, 1));
        }

        [Fact]
        public static void op_CheckedSubtractionTest()
        {
            Assert.Equal(NegativeOne, SubtractionOperatorsHelper<Int128, Int128, Int128>.op_CheckedSubtraction(Zero, 1));
            Assert.Equal(Zero, SubtractionOperatorsHelper<Int128, Int128, Int128>.op_CheckedSubtraction(One, 1));
            Assert.Equal(MaxValueMinusOne, SubtractionOperatorsHelper<Int128, Int128, Int128>.op_CheckedSubtraction(MaxValue, 1));
            Assert.Equal(NegativeTwo, SubtractionOperatorsHelper<Int128, Int128, Int128>.op_CheckedSubtraction(NegativeOne, 1));

            Assert.Throws<OverflowException>(() => SubtractionOperatorsHelper<Int128, Int128, Int128>.op_CheckedSubtraction(MinValue, 1));
        }

        //
        // IUnaryNegationOperators
        //

        [Fact]
        public static void op_UnaryNegationTest()
        {
            Assert.Equal(Zero, UnaryNegationOperatorsHelper<Int128, Int128>.op_UnaryNegation(Zero));
            Assert.Equal(NegativeOne, UnaryNegationOperatorsHelper<Int128, Int128>.op_UnaryNegation(One));
            Assert.Equal(MinValuePlusOne, UnaryNegationOperatorsHelper<Int128, Int128>.op_UnaryNegation(MaxValue));
            Assert.Equal(MinValue, UnaryNegationOperatorsHelper<Int128, Int128>.op_UnaryNegation(MinValue));
            Assert.Equal(One, UnaryNegationOperatorsHelper<Int128, Int128>.op_UnaryNegation(NegativeOne));
        }

        [Fact]
        public static void op_CheckedUnaryNegationTest()
        {
            Assert.Equal(Zero, UnaryNegationOperatorsHelper<Int128, Int128>.op_CheckedUnaryNegation(Zero));
            Assert.Equal(NegativeOne, UnaryNegationOperatorsHelper<Int128, Int128>.op_CheckedUnaryNegation(One));
            Assert.Equal(MinValuePlusOne, UnaryNegationOperatorsHelper<Int128, Int128>.op_CheckedUnaryNegation(MaxValue));
            Assert.Equal(One, UnaryNegationOperatorsHelper<Int128, Int128>.op_CheckedUnaryNegation(NegativeOne));

            Assert.Throws<OverflowException>(() => UnaryNegationOperatorsHelper<Int128, Int128>.op_CheckedUnaryNegation(MinValue));
        }

        //
        // IUnaryPlusOperators
        //

        [Fact]
        public static void op_UnaryPlusTest()
        {
            Assert.Equal(Zero, UnaryPlusOperatorsHelper<Int128, Int128>.op_UnaryPlus(Zero));
            Assert.Equal(One, UnaryPlusOperatorsHelper<Int128, Int128>.op_UnaryPlus(One));
            Assert.Equal(MaxValue, UnaryPlusOperatorsHelper<Int128, Int128>.op_UnaryPlus(MaxValue));
            Assert.Equal(MinValue, UnaryPlusOperatorsHelper<Int128, Int128>.op_UnaryPlus(MinValue));
            Assert.Equal(NegativeOne, UnaryPlusOperatorsHelper<Int128, Int128>.op_UnaryPlus(NegativeOne));
        }
    }
}
