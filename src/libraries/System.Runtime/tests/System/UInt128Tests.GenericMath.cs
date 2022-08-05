// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Tests
{
    public class UInt128Tests_GenericMath
    {
        internal static readonly UInt128 ByteMaxValue = new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_00FF);

        internal static readonly UInt128 Int16MaxValue = new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_7FFF);

        internal static readonly UInt128 Int16MaxValuePlusOne = new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_8000);

        internal static readonly UInt128 Int16MinValue = new UInt128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_8000);

        internal static readonly UInt128 Int32MaxValue = new UInt128(0x0000_0000_0000_0000, 0x0000_0000_7FFF_FFFF);

        internal static readonly UInt128 Int32MaxValuePlusOne = new UInt128(0x0000_0000_0000_0000, 0x0000_0000_8000_0000);

        internal static readonly UInt128 Int32MinValue = new UInt128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_8000_0000);

        internal static readonly UInt128 Int64MaxValue = new UInt128(0x0000_0000_0000_0000, 0x7FFF_FFFF_FFFF_FFFF);

        internal static readonly UInt128 Int64MaxValuePlusOne = new UInt128(0x0000_0000_0000_0000, 0x8000_0000_0000_0000);

        internal static readonly UInt128 Int64MinValue = new UInt128(0xFFFF_FFFF_FFFF_FFFF, 0x8000_0000_0000_0000);

        internal static readonly UInt128 Int128MaxValue = new UInt128(0x7FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF);

        internal static readonly UInt128 Int128MaxValueMinusOne = new UInt128(0x7FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFE);

        internal static readonly UInt128 Int128MaxValuePlusOne = new UInt128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000);

        internal static readonly UInt128 Int128MaxValuePlusTwo = new UInt128(0x8000_0000_0000_0000, 0x0000_0000_0000_0001);

        internal static readonly UInt128 MaxValue = new UInt128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF);

        internal static readonly UInt128 MaxValueMinusOne = new UInt128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFE);

        internal static readonly UInt128 One = new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001);

        internal static readonly UInt128 SByteMaxValue = new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_007F);

        internal static readonly UInt128 SByteMaxValuePlusOne = new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0080);

        internal static readonly UInt128 SByteMinValue = new UInt128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FF80);

        internal static readonly UInt128 Two = new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0002);

        internal static readonly UInt128 UInt16MaxValue = new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_FFFF);

        internal static readonly UInt128 UInt32MaxValue = new UInt128(0x0000_0000_0000_0000, 0x0000_0000_FFFF_FFFF);

        internal static readonly UInt128 UInt64MaxValue = new UInt128(0x0000_0000_0000_0000, 0xFFFF_FFFF_FFFF_FFFF);

        internal static readonly UInt128 Zero = new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000);

        //
        // IAdditionOperators
        //

        [Fact]
        public static void op_AdditionTest()
        {
            Assert.Equal(One, AdditionOperatorsHelper<UInt128, UInt128, UInt128>.op_Addition(Zero, 1U));
            Assert.Equal(Two, AdditionOperatorsHelper<UInt128, UInt128, UInt128>.op_Addition(One, 1U));
            Assert.Equal(Int128MaxValuePlusOne, AdditionOperatorsHelper<UInt128, UInt128, UInt128>.op_Addition(Int128MaxValue, 1U));
            Assert.Equal(Int128MaxValuePlusTwo, AdditionOperatorsHelper<UInt128, UInt128, UInt128>.op_Addition(Int128MaxValuePlusOne, 1U));
            Assert.Equal(Zero, AdditionOperatorsHelper<UInt128, UInt128, UInt128>.op_Addition(MaxValue, 1U));
        }

        [Fact]
        public static void op_CheckedAdditionTest()
        {
            Assert.Equal(One, AdditionOperatorsHelper<UInt128, UInt128, UInt128>.op_CheckedAddition(Zero, 1U));
            Assert.Equal(Two, AdditionOperatorsHelper<UInt128, UInt128, UInt128>.op_CheckedAddition(One, 1U));
            Assert.Equal(Int128MaxValuePlusOne, AdditionOperatorsHelper<UInt128, UInt128, UInt128>.op_CheckedAddition(Int128MaxValue, 1U));
            Assert.Equal(Int128MaxValuePlusTwo, AdditionOperatorsHelper<UInt128, UInt128, UInt128>.op_CheckedAddition(Int128MaxValuePlusOne, 1U));

            Assert.Throws<OverflowException>(() => AdditionOperatorsHelper<UInt128, UInt128, UInt128>.op_CheckedAddition(MaxValue, 1U));
        }

        //
        // IAdditiveIdentity
        //

        [Fact]
        public static void AdditiveIdentityTest()
        {
            Assert.Equal(Zero, AdditiveIdentityHelper<UInt128, UInt128>.AdditiveIdentity);
        }

        //
        // IBinaryInteger
        //

        [Fact]
        public static void DivRemTest()
        {
            Assert.Equal((Zero, Zero), BinaryIntegerHelper<UInt128>.DivRem(Zero, 2U));
            Assert.Equal((Zero, One), BinaryIntegerHelper<UInt128>.DivRem(One, 2U));
            Assert.Equal((new UInt128(0x3FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), One), BinaryIntegerHelper<UInt128>.DivRem(Int128MaxValue, 2U));
            Assert.Equal((new UInt128(0x4000_0000_0000_0000, 0x0000_0000_0000_0000), Zero), BinaryIntegerHelper<UInt128>.DivRem(Int128MaxValuePlusOne, 2U));
            Assert.Equal((Int128MaxValue, One), BinaryIntegerHelper<UInt128>.DivRem(MaxValue, 2U));
        }

        [Fact]
        public static void LeadingZeroCountTest()
        {
            Assert.Equal(0x80U, BinaryIntegerHelper<UInt128>.LeadingZeroCount(Zero));
            Assert.Equal(0x7FU, BinaryIntegerHelper<UInt128>.LeadingZeroCount(One));
            Assert.Equal(0x01U, BinaryIntegerHelper<UInt128>.LeadingZeroCount(Int128MaxValue));
            Assert.Equal(0x00U, BinaryIntegerHelper<UInt128>.LeadingZeroCount(Int128MaxValuePlusOne));
            Assert.Equal(0x00U, BinaryIntegerHelper<UInt128>.LeadingZeroCount(MaxValue));
        }

        [Fact]
        public static void PopCountTest()
        {
            Assert.Equal(0x00U, BinaryIntegerHelper<UInt128>.PopCount(Zero));
            Assert.Equal(0x01U, BinaryIntegerHelper<UInt128>.PopCount(One));
            Assert.Equal(0x7FU, BinaryIntegerHelper<UInt128>.PopCount(Int128MaxValue));
            Assert.Equal(0x01U, BinaryIntegerHelper<UInt128>.PopCount(Int128MaxValuePlusOne));
            Assert.Equal(0x80U, BinaryIntegerHelper<UInt128>.PopCount(MaxValue));
        }

        [Fact]
        public static void RotateLeftTest()
        {
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), BinaryIntegerHelper<UInt128>.RotateLeft(Zero, 1));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0002), BinaryIntegerHelper<UInt128>.RotateLeft(One, 1));
            Assert.Equal(new UInt128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFE), BinaryIntegerHelper<UInt128>.RotateLeft(Int128MaxValue, 1));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), BinaryIntegerHelper<UInt128>.RotateLeft(Int128MaxValuePlusOne, 1));
            Assert.Equal(new UInt128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), BinaryIntegerHelper<UInt128>.RotateLeft(MaxValue, 1));
        }

        [Fact]
        public static void RotateRightTest()
        {
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), BinaryIntegerHelper<UInt128>.RotateRight(Zero, 1));
            Assert.Equal(new UInt128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000), BinaryIntegerHelper<UInt128>.RotateRight(One, 1));
            Assert.Equal(new UInt128(0xBFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), BinaryIntegerHelper<UInt128>.RotateRight(Int128MaxValue, 1));
            Assert.Equal(new UInt128(0x4000_0000_0000_0000, 0x0000_0000_0000_0000), BinaryIntegerHelper<UInt128>.RotateRight(Int128MaxValuePlusOne, 1));
            Assert.Equal(new UInt128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), BinaryIntegerHelper<UInt128>.RotateRight(MaxValue, 1));
        }

        [Fact]
        public static void TrailingZeroCountTest()
        {
            Assert.Equal(0x80U, BinaryIntegerHelper<UInt128>.TrailingZeroCount(Zero));
            Assert.Equal(0x00U, BinaryIntegerHelper<UInt128>.TrailingZeroCount(One));
            Assert.Equal(0x00U, BinaryIntegerHelper<UInt128>.TrailingZeroCount(Int128MaxValue));
            Assert.Equal(0x7FU, BinaryIntegerHelper<UInt128>.TrailingZeroCount(Int128MaxValuePlusOne));
            Assert.Equal(0x00U, BinaryIntegerHelper<UInt128>.TrailingZeroCount(MaxValue));
        }

        [Fact]
        public static void TryReadBigEndianByteTest()
        {
            UInt128 result;

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0x00 }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0x01 }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0x7F }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_007F), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0x80 }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0080), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0xFF }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_00FF), result);
        }

        [Fact]
        public static void TryReadBigEndianInt16Test()
        {
            UInt128 result;

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0080), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0x01, 0x00 }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0100), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0x7F, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_7FFF), result);

            Assert.False(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0x80, 0x00 }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.False(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.False(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);
        }

        [Fact]
        public static void TryReadBigEndianInt32Test()
        {
            UInt128 result;

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0080), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0x01, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0100_0000), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_7FFF_FFFF), result);

            Assert.False(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0x80, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.False(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.False(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);
        }

        [Fact]
        public static void TryReadBigEndianInt64Test()
        {
            UInt128 result;

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0080), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0100_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x7FFF_FFFF_FFFF_FFFF), result);

            Assert.False(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.False(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.False(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);
        }

        [Fact]
        public static void TryReadBigEndianInt96Test()
        {
            UInt128 result;

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0080), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0100_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_7FFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), result);

            Assert.False(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.False(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.False(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);
        }

        [Fact]
        public static void TryReadBigEndianInt128Test()
        {
            UInt128 result;

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0080), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0100_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x7FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), result);

            Assert.False(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.False(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.False(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);
        }

        [Fact]
        public static void TryReadBigEndianSByteTest()
        {
            UInt128 result;

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0x00 }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0x01 }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0x7F }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_007F), result);

            Assert.False(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0x80 }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.False(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0xFF }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);
        }

        [Fact]
        public static void TryReadBigEndianUInt16Test()
        {
            UInt128 result;

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0080), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0x01, 0x00 }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0100), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0x7F, 0xFF }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_7FFF), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0x80, 0x00 }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_8000), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_FF7F), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_FFFF), result);
        }

        [Fact]
        public static void TryReadBigEndianUInt32Test()
        {
            UInt128 result;

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0080), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0x01, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0100_0000), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_7FFF_FFFF), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0x80, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_8000_0000), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_FFFF_FF7F), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_FFFF_FFFF), result);
        }

        [Fact]
        public static void TryReadBigEndianUInt64Test()
        {
            UInt128 result;

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0080), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0100_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x7FFF_FFFF_FFFF_FFFF), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x8000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0xFFFF_FFFF_FFFF_FF7F), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0xFFFF_FFFF_FFFF_FFFF), result);
        }

        [Fact]
        public static void TryReadBigEndianUInt96Test()
        {
            UInt128 result;

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0080), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0100_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_7FFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_8000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FF7F), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), result);
        }

        [Fact]
        public static void TryReadBigEndianUInt128Test()
        {
            UInt128 result;

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0080), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0100_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x7FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FF7F), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), result);
        }

        [Fact]
        public static void TryReadLittleEndianByteTest()
        {
            UInt128 result;

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0x00 }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0x01 }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0x7F }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_007F), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0x80 }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0080), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0xFF }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_00FF), result);
        }

        [Fact]
        public static void TryReadLittleEndianInt16Test()
        {
            UInt128 result;

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0100), result);

            Assert.False(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0x01, 0x00 }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), result);

            Assert.False(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0x80, 0x00 }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0080), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_7FFF), result);

            Assert.False(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);
        }

        [Fact]
        public static void TryReadLittleEndianInt32Test()
        {
            UInt128 result;

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0100_0000), result);

            Assert.False(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0x01, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), result);

            Assert.False(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0x80, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0080), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_7FFF_FFFF), result);

            Assert.False(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);
        }

        [Fact]
        public static void TryReadLittleEndianInt64Test()
        {
            UInt128 result;

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0100_0000_0000_0000), result);

            Assert.False(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), result);

            Assert.False(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0080), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x7FFF_FFFF_FFFF_FFFF), result);

            Assert.False(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);
        }

        [Fact]
        public static void TryReadLittleEndianInt96Test()
        {
            UInt128 result;

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0100_0000, 0x0000_0000_0000_0000), result);

            Assert.False(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), result);

            Assert.False(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0080), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_7FFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), result);

            Assert.False(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);
        }

        [Fact]
        public static void TryReadLittleEndianInt128Test()
        {
            UInt128 result;

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0100_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.False(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), result);

            Assert.False(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0080), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x7FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), result);

            Assert.False(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);
        }

        [Fact]
        public static void TryReadLittleEndianSByteTest()
        {
            UInt128 result;

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0x00 }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0x01 }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0x7F }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_007F), result);

            Assert.False(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0x80 }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.False(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0xFF }, isUnsigned: false, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);
        }

        [Fact]
        public static void TryReadLittleEndianUInt16Test()
        {
            UInt128 result;

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0100), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_8000), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0x01, 0x00 }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_FF7F), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0x80, 0x00 }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0080), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_7FFF), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_FFFF), result);
        }

        [Fact]
        public static void TryReadLittleEndianUInt32Test()
        {
            UInt128 result;

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0100_0000), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_8000_0000), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0x01, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_FFFF_FF7F), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0x80, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0080), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_7FFF_FFFF), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_FFFF_FFFF), result);
        }

        [Fact]
        public static void TryReadLittleEndianUInt64Test()
        {
            UInt128 result;

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0100_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x8000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0xFFFF_FFFF_FFFF_FF7F), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0080), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x7FFF_FFFF_FFFF_FFFF), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0xFFFF_FFFF_FFFF_FFFF), result);
        }

        [Fact]
        public static void TryReadLittleEndianUInt96Test()
        {
            UInt128 result;

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0100_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_8000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FF7F), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0080), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_7FFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), result);
        }

        [Fact]
        public static void TryReadLittleEndianUInt128Test()
        {
            UInt128 result;

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0100_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FF7F), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0080), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0x7FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), result);

            Assert.True(BinaryIntegerHelper<UInt128>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal(new UInt128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), result);
        }

        [Fact]
        public static void GetByteCountTest()
        {
            Assert.Equal(16, BinaryIntegerHelper<UInt128>.GetByteCount(Zero));
            Assert.Equal(16, BinaryIntegerHelper<UInt128>.GetByteCount(One));
            Assert.Equal(16, BinaryIntegerHelper<UInt128>.GetByteCount(Int128MaxValue));
            Assert.Equal(16, BinaryIntegerHelper<UInt128>.GetByteCount(Int128MaxValuePlusOne));
            Assert.Equal(16, BinaryIntegerHelper<UInt128>.GetByteCount(MaxValue));
        }

        [Fact]
        public static void GetShortestBitLengthTest()
        {
            Assert.Equal(0x00, BinaryIntegerHelper<UInt128>.GetShortestBitLength(Zero));
            Assert.Equal(0x01, BinaryIntegerHelper<UInt128>.GetShortestBitLength(One));
            Assert.Equal(0x7F, BinaryIntegerHelper<UInt128>.GetShortestBitLength(Int128MaxValue));
            Assert.Equal(0x80, BinaryIntegerHelper<UInt128>.GetShortestBitLength(Int128MaxValuePlusOne));
            Assert.Equal(0x80, BinaryIntegerHelper<UInt128>.GetShortestBitLength(MaxValue));
        }

        [Fact]
        public static void TryWriteBigEndianTest()
        {
            Span<byte> destination = stackalloc byte[16];
            int bytesWritten = 0;

            Assert.True(BinaryIntegerHelper<UInt128>.TryWriteBigEndian(Zero, destination, out bytesWritten));
            Assert.Equal(16, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<UInt128>.TryWriteBigEndian(One, destination, out bytesWritten));
            Assert.Equal(16, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<UInt128>.TryWriteBigEndian(Int128MaxValue, destination, out bytesWritten));
            Assert.Equal(16, bytesWritten);
            Assert.Equal(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<UInt128>.TryWriteBigEndian(Int128MaxValuePlusOne, destination, out bytesWritten));
            Assert.Equal(16, bytesWritten);
            Assert.Equal(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<UInt128>.TryWriteBigEndian(MaxValue, destination, out bytesWritten));
            Assert.Equal(16, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, destination.ToArray());

            Assert.False(BinaryIntegerHelper<UInt128>.TryWriteBigEndian(default, Span<byte>.Empty, out bytesWritten));
            Assert.Equal(0, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, destination.ToArray());
        }

        [Fact]
        public static void TryWriteLittleEndianTest()
        {
            Span<byte> destination = stackalloc byte[16];
            int bytesWritten = 0;

            Assert.True(BinaryIntegerHelper<UInt128>.TryWriteLittleEndian(Zero, destination, out bytesWritten));
            Assert.Equal(16, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<UInt128>.TryWriteLittleEndian(One, destination, out bytesWritten));
            Assert.Equal(16, bytesWritten);
            Assert.Equal(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<UInt128>.TryWriteLittleEndian(Int128MaxValue, destination, out bytesWritten));
            Assert.Equal(16, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<UInt128>.TryWriteLittleEndian(Int128MaxValuePlusOne, destination, out bytesWritten));
            Assert.Equal(16, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<UInt128>.TryWriteLittleEndian(MaxValue, destination, out bytesWritten));
            Assert.Equal(16, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, destination.ToArray());

            Assert.False(BinaryIntegerHelper<UInt128>.TryWriteLittleEndian(default, Span<byte>.Empty, out bytesWritten));
            Assert.Equal(0, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, destination.ToArray());
        }

        //
        // IBinaryNumber
        //

        [Fact]
        public static void AllBitsSetTest()
        {
            UInt128 compare = new UInt128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF);
            Assert.Equal(compare, BinaryNumberHelper<UInt128>.AllBitsSet);
            Assert.Equal((UInt128)0, ~BinaryNumberHelper<UInt128>.AllBitsSet);
        }

        [Fact]
        public static void IsPow2Test()
        {
            Assert.False(BinaryNumberHelper<UInt128>.IsPow2(Zero));
            Assert.True(BinaryNumberHelper<UInt128>.IsPow2(One));
            Assert.False(BinaryNumberHelper<UInt128>.IsPow2(Int128MaxValue));
            Assert.True(BinaryNumberHelper<UInt128>.IsPow2(Int128MaxValuePlusOne));
            Assert.False(BinaryNumberHelper<UInt128>.IsPow2(MaxValue));
        }

        [Fact]
        public static void Log2Test()
        {
            Assert.Equal(0x00U, BinaryNumberHelper<UInt128>.Log2(Zero));
            Assert.Equal(0x00U, BinaryNumberHelper<UInt128>.Log2(One));
            Assert.Equal(0x7EU, BinaryNumberHelper<UInt128>.Log2(Int128MaxValue));
            Assert.Equal(0x7FU, BinaryNumberHelper<UInt128>.Log2(Int128MaxValuePlusOne));
            Assert.Equal(0x7FU, BinaryNumberHelper<UInt128>.Log2(MaxValue));
        }

        //
        // IBitwiseOperators
        //

        [Fact]
        public static void op_BitwiseAndTest()
        {
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), BitwiseOperatorsHelper<UInt128, UInt128, UInt128>.op_BitwiseAnd(Zero, 1U));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), BitwiseOperatorsHelper<UInt128, UInt128, UInt128>.op_BitwiseAnd(One, 1U));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), BitwiseOperatorsHelper<UInt128, UInt128, UInt128>.op_BitwiseAnd(Int128MaxValue, 1U));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), BitwiseOperatorsHelper<UInt128, UInt128, UInt128>.op_BitwiseAnd(Int128MaxValuePlusOne, 1U));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), BitwiseOperatorsHelper<UInt128, UInt128, UInt128>.op_BitwiseAnd(MaxValue, 1U));
        }

        [Fact]
        public static void op_BitwiseOrTest()
        {
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), BitwiseOperatorsHelper<UInt128, UInt128, UInt128>.op_BitwiseOr(Zero, 1U));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), BitwiseOperatorsHelper<UInt128, UInt128, UInt128>.op_BitwiseOr(One, 1U));
            Assert.Equal(new UInt128(0x7FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), BitwiseOperatorsHelper<UInt128, UInt128, UInt128>.op_BitwiseOr(Int128MaxValue, 1U));
            Assert.Equal(new UInt128(0x8000_0000_0000_0000, 0x0000_0000_0000_0001), BitwiseOperatorsHelper<UInt128, UInt128, UInt128>.op_BitwiseOr(Int128MaxValuePlusOne, 1U));
            Assert.Equal(new UInt128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), BitwiseOperatorsHelper<UInt128, UInt128, UInt128>.op_BitwiseOr(MaxValue, 1U));
        }

        [Fact]
        public static void op_ExclusiveOrTest()
        {
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), BitwiseOperatorsHelper<UInt128, UInt128, UInt128>.op_ExclusiveOr(Zero, 1U));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), BitwiseOperatorsHelper<UInt128, UInt128, UInt128>.op_ExclusiveOr(One, 1U));
            Assert.Equal(new UInt128(0x7FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFE), BitwiseOperatorsHelper<UInt128, UInt128, UInt128>.op_ExclusiveOr(Int128MaxValue, 1U));
            Assert.Equal(new UInt128(0x8000_0000_0000_0000, 0x0000_0000_0000_0001), BitwiseOperatorsHelper<UInt128, UInt128, UInt128>.op_ExclusiveOr(Int128MaxValuePlusOne, 1U));
            Assert.Equal(new UInt128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFE), BitwiseOperatorsHelper<UInt128, UInt128, UInt128>.op_ExclusiveOr(MaxValue, 1U));
        }

        [Fact]
        public static void op_OnesComplementTest()
        {
            Assert.Equal(new UInt128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), BitwiseOperatorsHelper<UInt128, UInt128, UInt128>.op_OnesComplement(Zero));
            Assert.Equal(new UInt128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFE), BitwiseOperatorsHelper<UInt128, UInt128, UInt128>.op_OnesComplement(One));
            Assert.Equal(new UInt128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000), BitwiseOperatorsHelper<UInt128, UInt128, UInt128>.op_OnesComplement(Int128MaxValue));
            Assert.Equal(new UInt128(0x7FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), BitwiseOperatorsHelper<UInt128, UInt128, UInt128>.op_OnesComplement(Int128MaxValuePlusOne));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), BitwiseOperatorsHelper<UInt128, UInt128, UInt128>.op_OnesComplement(MaxValue));
        }

        //
        // IComparisonOperators
        //

        [Fact]
        public static void op_GreaterThanTest()
        {
            Assert.False(ComparisonOperatorsHelper<UInt128, UInt128, bool>.op_GreaterThan(Zero, 1U));
            Assert.False(ComparisonOperatorsHelper<UInt128, UInt128, bool>.op_GreaterThan(One, 1U));
            Assert.True(ComparisonOperatorsHelper<UInt128, UInt128, bool>.op_GreaterThan(Int128MaxValue, 1U));
            Assert.True(ComparisonOperatorsHelper<UInt128, UInt128, bool>.op_GreaterThan(Int128MaxValuePlusOne, 1U));
            Assert.True(ComparisonOperatorsHelper<UInt128, UInt128, bool>.op_GreaterThan(MaxValue, 1U));
        }

        [Fact]
        public static void op_GreaterThanOrEqualTest()
        {
            Assert.False(ComparisonOperatorsHelper<UInt128, UInt128, bool>.op_GreaterThanOrEqual(Zero, 1U));
            Assert.True(ComparisonOperatorsHelper<UInt128, UInt128, bool>.op_GreaterThanOrEqual(One, 1U));
            Assert.True(ComparisonOperatorsHelper<UInt128, UInt128, bool>.op_GreaterThanOrEqual(Int128MaxValue, 1U));
            Assert.True(ComparisonOperatorsHelper<UInt128, UInt128, bool>.op_GreaterThanOrEqual(Int128MaxValuePlusOne, 1U));
            Assert.True(ComparisonOperatorsHelper<UInt128, UInt128, bool>.op_GreaterThanOrEqual(MaxValue, 1U));
        }

        [Fact]
        public static void op_LessThanTest()
        {
            Assert.True(ComparisonOperatorsHelper<UInt128, UInt128, bool>.op_LessThan(Zero, 1U));
            Assert.False(ComparisonOperatorsHelper<UInt128, UInt128, bool>.op_LessThan(One, 1U));
            Assert.False(ComparisonOperatorsHelper<UInt128, UInt128, bool>.op_LessThan(Int128MaxValue, 1U));
            Assert.False(ComparisonOperatorsHelper<UInt128, UInt128, bool>.op_LessThan(Int128MaxValuePlusOne, 1U));
            Assert.False(ComparisonOperatorsHelper<UInt128, UInt128, bool>.op_LessThan(MaxValue, 1U));
        }

        [Fact]
        public static void op_LessThanOrEqualTest()
        {
            Assert.True(ComparisonOperatorsHelper<UInt128, UInt128, bool>.op_LessThanOrEqual(Zero, 1U));
            Assert.True(ComparisonOperatorsHelper<UInt128, UInt128, bool>.op_LessThanOrEqual(One, 1U));
            Assert.False(ComparisonOperatorsHelper<UInt128, UInt128, bool>.op_LessThanOrEqual(Int128MaxValue, 1U));
            Assert.False(ComparisonOperatorsHelper<UInt128, UInt128, bool>.op_LessThanOrEqual(Int128MaxValuePlusOne, 1U));
            Assert.False(ComparisonOperatorsHelper<UInt128, UInt128, bool>.op_LessThanOrEqual(MaxValue, 1U));
        }

        //
        // IDecrementOperators
        //

        [Fact]
        public static void op_DecrementTest()
        {
            Assert.Equal(MaxValue, DecrementOperatorsHelper<UInt128>.op_Decrement(Zero));
            Assert.Equal(Zero, DecrementOperatorsHelper<UInt128>.op_Decrement(One));
            Assert.Equal(Int128MaxValueMinusOne, DecrementOperatorsHelper<UInt128>.op_Decrement(Int128MaxValue));
            Assert.Equal(Int128MaxValue, DecrementOperatorsHelper<UInt128>.op_Decrement(Int128MaxValuePlusOne));
            Assert.Equal(MaxValueMinusOne, DecrementOperatorsHelper<UInt128>.op_Decrement(MaxValue));
        }

        [Fact]
        public static void op_CheckedDecrementTest()
        {
            Assert.Equal(Zero, DecrementOperatorsHelper<UInt128>.op_CheckedDecrement(One));
            Assert.Equal(Int128MaxValueMinusOne, DecrementOperatorsHelper<UInt128>.op_CheckedDecrement(Int128MaxValue));
            Assert.Equal(Int128MaxValue, DecrementOperatorsHelper<UInt128>.op_CheckedDecrement(Int128MaxValuePlusOne));
            Assert.Equal(MaxValueMinusOne, DecrementOperatorsHelper<UInt128>.op_CheckedDecrement(MaxValue));

            Assert.Throws<OverflowException>(() => DecrementOperatorsHelper<UInt128>.op_CheckedDecrement(Zero));
        }

        //
        // IDivisionOperators
        //

        [Fact]
        public static void op_DivisionTest()
        {
            Assert.Equal(Zero, DivisionOperatorsHelper<UInt128, UInt128, UInt128>.op_Division(Zero, 2U));
            Assert.Equal(Zero, DivisionOperatorsHelper<UInt128, UInt128, UInt128>.op_Division(One, 2U));
            Assert.Equal(new UInt128(0x3FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), DivisionOperatorsHelper<UInt128, UInt128, UInt128>.op_Division(Int128MaxValue, 2U));
            Assert.Equal(new UInt128(0x4000_0000_0000_0000, 0x0000_0000_0000_0000), DivisionOperatorsHelper<UInt128, UInt128, UInt128>.op_Division(Int128MaxValuePlusOne, 2U));
            Assert.Equal(Int128MaxValue, DivisionOperatorsHelper<UInt128, UInt128, UInt128>.op_Division(MaxValue, 2U));

            Assert.Throws<DivideByZeroException>(() => DivisionOperatorsHelper<UInt128, UInt128, UInt128>.op_Division(One, 0U));
        }

        [Fact]
        public static void op_CheckedDivisionTest()
        {
            Assert.Equal(Zero, DivisionOperatorsHelper<UInt128, UInt128, UInt128>.op_CheckedDivision(Zero, 2U));
            Assert.Equal(Zero, DivisionOperatorsHelper<UInt128, UInt128, UInt128>.op_CheckedDivision(One, 2U));
            Assert.Equal(new UInt128(0x3FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), DivisionOperatorsHelper<UInt128, UInt128, UInt128>.op_CheckedDivision(Int128MaxValue, 2U));
            Assert.Equal(new UInt128(0x4000_0000_0000_0000, 0x0000_0000_0000_0000), DivisionOperatorsHelper<UInt128, UInt128, UInt128>.op_CheckedDivision(Int128MaxValuePlusOne, 2U));
            Assert.Equal(Int128MaxValue, DivisionOperatorsHelper<UInt128, UInt128, UInt128>.op_CheckedDivision(MaxValue, 2U));

            Assert.Throws<DivideByZeroException>(() => DivisionOperatorsHelper<UInt128, UInt128, UInt128>.op_CheckedDivision(One, 0U));
        }

        //
        // IEqualityOperators
        //

        [Fact]
        public static void op_EqualityTest()
        {
            Assert.False(EqualityOperatorsHelper<UInt128, UInt128, bool>.op_Equality(Zero, 1U));
            Assert.True(EqualityOperatorsHelper<UInt128, UInt128, bool>.op_Equality(One, 1U));
            Assert.False(EqualityOperatorsHelper<UInt128, UInt128, bool>.op_Equality(Int128MaxValue, 1U));
            Assert.False(EqualityOperatorsHelper<UInt128, UInt128, bool>.op_Equality(Int128MaxValuePlusOne, 1U));
            Assert.False(EqualityOperatorsHelper<UInt128, UInt128, bool>.op_Equality(MaxValue, 1U));
        }

        [Fact]
        public static void op_InequalityTest()
        {
            Assert.True(EqualityOperatorsHelper<UInt128, UInt128, bool>.op_Inequality(Zero, 1U));
            Assert.False(EqualityOperatorsHelper<UInt128, UInt128, bool>.op_Inequality(One, 1U));
            Assert.True(EqualityOperatorsHelper<UInt128, UInt128, bool>.op_Inequality(Int128MaxValue, 1U));
            Assert.True(EqualityOperatorsHelper<UInt128, UInt128, bool>.op_Inequality(Int128MaxValuePlusOne, 1U));
            Assert.True(EqualityOperatorsHelper<UInt128, UInt128, bool>.op_Inequality(MaxValue, 1U));
        }

        //
        // IIncrementOperators
        //

        [Fact]
        public static void op_IncrementTest()
        {
            Assert.Equal(One, IncrementOperatorsHelper<UInt128>.op_Increment(Zero));
            Assert.Equal(Two, IncrementOperatorsHelper<UInt128>.op_Increment(One));
            Assert.Equal(Int128MaxValuePlusOne, IncrementOperatorsHelper<UInt128>.op_Increment(Int128MaxValue));
            Assert.Equal(Int128MaxValuePlusTwo, IncrementOperatorsHelper<UInt128>.op_Increment(Int128MaxValuePlusOne));
            Assert.Equal(Zero, IncrementOperatorsHelper<UInt128>.op_Increment(MaxValue));
        }

        [Fact]
        public static void op_CheckedIncrementTest()
        {
            Assert.Equal(One, IncrementOperatorsHelper<UInt128>.op_CheckedIncrement(Zero));
            Assert.Equal(Two, IncrementOperatorsHelper<UInt128>.op_CheckedIncrement(One));
            Assert.Equal(Int128MaxValuePlusOne, IncrementOperatorsHelper<UInt128>.op_CheckedIncrement(Int128MaxValue));
            Assert.Equal(Int128MaxValuePlusTwo, IncrementOperatorsHelper<UInt128>.op_CheckedIncrement(Int128MaxValuePlusOne));

            Assert.Throws<OverflowException>(() => IncrementOperatorsHelper<UInt128>.op_CheckedIncrement(MaxValue));
        }

        //
        // IMinMaxValue
        //

        [Fact]
        public static void MaxValueTest()
        {
            Assert.Equal(MaxValue, MinMaxValueHelper<UInt128>.MaxValue);
        }

        [Fact]
        public static void MinValueTest()
        {
            Assert.Equal(Zero, MinMaxValueHelper<UInt128>.MinValue);
        }

        //
        // IModulusOperators
        //

        [Fact]
        public static void op_ModulusTest()
        {
            Assert.Equal(Zero, ModulusOperatorsHelper<UInt128, UInt128, UInt128>.op_Modulus(Zero, 2U));
            Assert.Equal(One, ModulusOperatorsHelper<UInt128, UInt128, UInt128>.op_Modulus(One, 2U));
            Assert.Equal(One, ModulusOperatorsHelper<UInt128, UInt128, UInt128>.op_Modulus(Int128MaxValue, 2U));
            Assert.Equal(Zero, ModulusOperatorsHelper<UInt128, UInt128, UInt128>.op_Modulus(Int128MaxValuePlusOne, 2U));
            Assert.Equal(One, ModulusOperatorsHelper<UInt128, UInt128, UInt128>.op_Modulus(MaxValue, 2U));

            Assert.Throws<DivideByZeroException>(() => ModulusOperatorsHelper<UInt128, UInt128, UInt128>.op_Modulus(One, 0U));
        }

        //
        // IMultiplicativeIdentity
        //

        [Fact]
        public static void MultiplicativeIdentityTest()
        {
            Assert.Equal(One, MultiplicativeIdentityHelper<UInt128, UInt128>.MultiplicativeIdentity);
        }

        //
        // IMultiplyOperators
        //

        [Fact]
        public static void op_MultiplyTest()
        {
            Assert.Equal(Zero, MultiplyOperatorsHelper<UInt128, UInt128, UInt128>.op_Multiply(Zero, 2U));
            Assert.Equal(Two, MultiplyOperatorsHelper<UInt128, UInt128, UInt128>.op_Multiply(One, 2U));
            Assert.Equal(MaxValueMinusOne, MultiplyOperatorsHelper<UInt128, UInt128, UInt128>.op_Multiply(Int128MaxValue, 2U));
            Assert.Equal(Zero, MultiplyOperatorsHelper<UInt128, UInt128, UInt128>.op_Multiply(Int128MaxValuePlusOne, 2U));
            Assert.Equal(MaxValueMinusOne, MultiplyOperatorsHelper<UInt128, UInt128, UInt128>.op_Multiply(MaxValue, 2U));
        }
        [Fact]
        public static void op_CheckedMultiplyTest()
        {
            Assert.Equal(Zero, MultiplyOperatorsHelper<UInt128, UInt128, UInt128>.op_CheckedMultiply(Zero, 2U));
            Assert.Equal(Two, MultiplyOperatorsHelper<UInt128, UInt128, UInt128>.op_CheckedMultiply(One, 2U));
            Assert.Equal(MaxValueMinusOne, MultiplyOperatorsHelper<UInt128, UInt128, UInt128>.op_CheckedMultiply(Int128MaxValue, 2U));

            Assert.Throws<OverflowException>(() => MultiplyOperatorsHelper<UInt128, UInt128, UInt128>.op_CheckedMultiply(Int128MaxValuePlusOne, 2U));
            Assert.Throws<OverflowException>(() => MultiplyOperatorsHelper<UInt128, UInt128, UInt128>.op_CheckedMultiply(MaxValue, 2U));
        }

        //
        // INumber
        //

        [Fact]
        public static void ClampTest()
        {
            Assert.Equal(One, NumberHelper<UInt128>.Clamp(Zero, 0x0001U, 0x007FU));
            Assert.Equal(One, NumberHelper<UInt128>.Clamp(One, 0x0001U, 0x007FU));
            Assert.Equal(0x007FU, NumberHelper<UInt128>.Clamp(Int128MaxValue, 0x0001U, 0x007FU));
            Assert.Equal(0x007FU, NumberHelper<UInt128>.Clamp(Int128MaxValuePlusOne, 0x0001U, 0x007FU));
            Assert.Equal(0x007FU, NumberHelper<UInt128>.Clamp(MaxValue, 0x0001U, 0x007FU));
        }

        [Fact]
        public static void MaxTest()
        {
            Assert.Equal(One, NumberHelper<UInt128>.Max(Zero, 1U));
            Assert.Equal(One, NumberHelper<UInt128>.Max(One, 1U));
            Assert.Equal(Int128MaxValue, NumberHelper<UInt128>.Max(Int128MaxValue, 1U));
            Assert.Equal(Int128MaxValuePlusOne, NumberHelper<UInt128>.Max(Int128MaxValuePlusOne, 1U));
            Assert.Equal(MaxValue, NumberHelper<UInt128>.Max(MaxValue, 1U));
        }

        [Fact]
        public static void MaxNumberTest()
        {
            Assert.Equal(One, NumberHelper<UInt128>.MaxNumber(Zero, 1U));
            Assert.Equal(One, NumberHelper<UInt128>.MaxNumber(One, 1U));
            Assert.Equal(Int128MaxValue, NumberHelper<UInt128>.MaxNumber(Int128MaxValue, 1U));
            Assert.Equal(Int128MaxValuePlusOne, NumberHelper<UInt128>.MaxNumber(Int128MaxValuePlusOne, 1U));
            Assert.Equal(MaxValue, NumberHelper<UInt128>.MaxNumber(MaxValue, 1U));
        }

        [Fact]
        public static void MinTest()
        {
            Assert.Equal(Zero, NumberHelper<UInt128>.Min(Zero, 1U));
            Assert.Equal(One, NumberHelper<UInt128>.Min(One, 1U));
            Assert.Equal(One, NumberHelper<UInt128>.Min(Int128MaxValue, 1U));
            Assert.Equal(One, NumberHelper<UInt128>.Min(Int128MaxValuePlusOne, 1U));
            Assert.Equal(One, NumberHelper<UInt128>.Min(MaxValue, 1U));
        }

        [Fact]
        public static void MinNumberTest()
        {
            Assert.Equal(Zero, NumberHelper<UInt128>.MinNumber(Zero, 1U));
            Assert.Equal(One, NumberHelper<UInt128>.MinNumber(One, 1U));
            Assert.Equal(One, NumberHelper<UInt128>.MinNumber(Int128MaxValue, 1U));
            Assert.Equal(One, NumberHelper<UInt128>.MinNumber(Int128MaxValuePlusOne, 1U));
            Assert.Equal(One, NumberHelper<UInt128>.MinNumber(MaxValue, 1U));
        }

        [Fact]
        public static void SignTest()
        {
            Assert.Equal(0, NumberHelper<UInt128>.Sign(Zero));
            Assert.Equal(1, NumberHelper<UInt128>.Sign(One));
            Assert.Equal(1, NumberHelper<UInt128>.Sign(Int128MaxValue));
            Assert.Equal(1, NumberHelper<UInt128>.Sign(Int128MaxValuePlusOne));
            Assert.Equal(1, NumberHelper<UInt128>.Sign(MaxValue));
        }

        //
        // INumberBase
        //

        [Fact]
        public static void OneTest()
        {
            Assert.Equal(One, NumberBaseHelper<UInt128>.One);
        }

        [Fact]
        public static void RadixTest()
        {
            Assert.Equal(2, NumberBaseHelper<UInt128>.Radix);
        }

        [Fact]
        public static void ZeroTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.Zero);
        }

        [Fact]
        public static void AbsTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.Abs(Zero));
            Assert.Equal(One, NumberBaseHelper<UInt128>.Abs(One));
            Assert.Equal(Int128MaxValue, NumberBaseHelper<UInt128>.Abs(Int128MaxValue));
            Assert.Equal(Int128MaxValuePlusOne, NumberBaseHelper<UInt128>.Abs(Int128MaxValuePlusOne));
            Assert.Equal(MaxValue, NumberBaseHelper<UInt128>.Abs(MaxValue));
        }

        [Fact]
        public static void CreateCheckedFromByteTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateChecked<byte>(0x00));
            Assert.Equal(One, NumberBaseHelper<UInt128>.CreateChecked<byte>(0x01));
            Assert.Equal(SByteMaxValue, NumberBaseHelper<UInt128>.CreateChecked<byte>(0x7F));
            Assert.Equal(SByteMaxValuePlusOne, NumberBaseHelper<UInt128>.CreateChecked<byte>(0x80));
            Assert.Equal(ByteMaxValue, NumberBaseHelper<UInt128>.CreateChecked<byte>(0xFF));
        }

        [Fact]
        public static void CreateCheckedFromCharTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateChecked<char>((char)0x0000));
            Assert.Equal(One, NumberBaseHelper<UInt128>.CreateChecked<char>((char)0x0001));
            Assert.Equal(Int16MaxValue, NumberBaseHelper<UInt128>.CreateChecked<char>((char)0x7FFF));
            Assert.Equal(Int16MaxValuePlusOne, NumberBaseHelper<UInt128>.CreateChecked<char>((char)0x8000));
            Assert.Equal(UInt16MaxValue, NumberBaseHelper<UInt128>.CreateChecked<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateCheckedFromDecimalTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateChecked<decimal>(decimal.Zero));
            Assert.Equal(One, NumberBaseHelper<UInt128>.CreateChecked<decimal>(decimal.One));

            Assert.Equal(new UInt128(0x0000_0000_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), NumberBaseHelper<UInt128>.CreateChecked<decimal>(decimal.MaxValue));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<UInt128>.CreateChecked<decimal>(decimal.MinValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<UInt128>.CreateChecked<decimal>(decimal.MinusOne));
        }

        [Fact]
        public static void CreateCheckedFromDoubleTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateChecked<double>(+0.0));
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateChecked<double>(-0.0));

            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateChecked<double>(+double.Epsilon));
            Assert.Equal(One, NumberBaseHelper<UInt128>.CreateChecked<double>(+1.0));

            Assert.Equal(new UInt128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000), NumberBaseHelper<UInt128>.CreateChecked<double>(+170141183460469231731687303715884105728.0));
            Assert.Equal(new UInt128(0xFFFF_FFFF_FFFF_F800, 0x0000_0000_0000_0000), NumberBaseHelper<UInt128>.CreateChecked<double>(+340282366920938425684442744474606501888.0));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<UInt128>.CreateChecked<double>(-double.Epsilon));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<UInt128>.CreateChecked<double>(-1.0));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<UInt128>.CreateChecked<double>(+340282366920938463463374607431768211456.0));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<UInt128>.CreateChecked<double>(-340282366920938425684442744474606501888.0));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<UInt128>.CreateChecked<double>(double.MaxValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<UInt128>.CreateChecked<double>(double.MinValue));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<UInt128>.CreateChecked<double>(double.PositiveInfinity));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<UInt128>.CreateChecked<double>(double.NegativeInfinity));
        }

        [Fact]
        public static void CreateCheckedFromHalfTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateChecked<Half>((Half)(+0.0)));
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateChecked<Half>((Half)(-0.0)));

            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateChecked<Half>(+Half.Epsilon));
            Assert.Equal(One, NumberBaseHelper<UInt128>.CreateChecked<Half>((Half)(+1.0)));
            Assert.Equal(+65504U, NumberBaseHelper<UInt128>.CreateChecked<Half>(Half.MaxValue));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<UInt128>.CreateChecked<Half>(-Half.Epsilon));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<UInt128>.CreateChecked<Half>((Half)(-1.0)));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<UInt128>.CreateChecked<Half>(Half.MinValue));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<UInt128>.CreateChecked<Half>(Half.PositiveInfinity));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<UInt128>.CreateChecked<Half>(Half.NegativeInfinity));
        }

        [Fact]
        public static void CreateCheckedFromInt16Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateChecked<short>(0x0000));
            Assert.Equal(One, NumberBaseHelper<UInt128>.CreateChecked<short>(0x0001));
            Assert.Equal(Int16MaxValue, NumberBaseHelper<UInt128>.CreateChecked<short>(0x7FFF));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<UInt128>.CreateChecked<short>(unchecked((short)0x8000)));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<UInt128>.CreateChecked<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromInt32Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateChecked<int>(0x00000000));
            Assert.Equal(One, NumberBaseHelper<UInt128>.CreateChecked<int>(0x00000001));
            Assert.Equal(Int32MaxValue, NumberBaseHelper<UInt128>.CreateChecked<int>(0x7FFFFFFF));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<UInt128>.CreateChecked<int>(unchecked((int)0x80000000)));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<UInt128>.CreateChecked<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromInt64Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateChecked<long>(0x0000000000000000));
            Assert.Equal(One, NumberBaseHelper<UInt128>.CreateChecked<long>(0x0000000000000001));
            Assert.Equal(Int64MaxValue, NumberBaseHelper<UInt128>.CreateChecked<long>(0x7FFFFFFFFFFFFFFF));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<UInt128>.CreateChecked<long>(unchecked((long)0x8000000000000000)));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<UInt128>.CreateChecked<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromInt128Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateChecked<Int128>(Int128.Zero));
            Assert.Equal(One, NumberBaseHelper<UInt128>.CreateChecked<Int128>(Int128.One));
            Assert.Equal(Int128MaxValue, NumberBaseHelper<UInt128>.CreateChecked<Int128>(Int128.MaxValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<UInt128>.CreateChecked<Int128>(Int128.MinValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<UInt128>.CreateChecked<Int128>(Int128.NegativeOne));
        }

        [Fact]
        public static void CreateCheckedFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateChecked<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal(One, NumberBaseHelper<UInt128>.CreateChecked<nint>(unchecked((nint)0x0000000000000001)));
                Assert.Equal(Int64MaxValue, NumberBaseHelper<UInt128>.CreateChecked<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<UInt128>.CreateChecked<nint>(unchecked((nint)0x8000000000000000)));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<UInt128>.CreateChecked<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateChecked<nint>((nint)0x00000000));
                Assert.Equal(One, NumberBaseHelper<UInt128>.CreateChecked<nint>((nint)0x00000001));
                Assert.Equal(Int32MaxValue, NumberBaseHelper<UInt128>.CreateChecked<nint>((nint)0x7FFFFFFF));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<UInt128>.CreateChecked<nint>(unchecked((nint)0x80000000)));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<UInt128>.CreateChecked<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateCheckedFromNFloatTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateChecked<NFloat>(+0.0f));
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateChecked<NFloat>(-0.0f));

            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateChecked<NFloat>(+NFloat.Epsilon));
            Assert.Equal(One, NumberBaseHelper<UInt128>.CreateChecked<NFloat>(+1.0f));

            if (Environment.Is64BitProcess)
            {
                Assert.Equal(new UInt128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000), NumberBaseHelper<UInt128>.CreateChecked<NFloat>((NFloat)(+170141183460469231731687303715884105728.0)));
                Assert.Equal(new UInt128(0xFFFF_FFFF_FFFF_F800, 0x0000_0000_0000_0000), NumberBaseHelper<UInt128>.CreateChecked<NFloat>((NFloat)(+340282366920938425684442744474606501888.0)));

                Assert.Throws<OverflowException>(() => NumberBaseHelper<UInt128>.CreateChecked<NFloat>((NFloat)(+340282366920938463463374607431768211456.0)));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<UInt128>.CreateChecked<NFloat>((NFloat)(-340282366920938425684442744474606501888.0)));

                Assert.Throws<OverflowException>(() => NumberBaseHelper<UInt128>.CreateChecked<NFloat>(NFloat.MaxValue));
            }
            else
            {
                Assert.Equal(new UInt128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000), NumberBaseHelper<UInt128>.CreateChecked<NFloat>(+170141183460469231731687303715884105728.0f));
                Assert.Equal(new UInt128(0xFFFF_FF00_0000_0000, 0x0000_0000_0000_0000), NumberBaseHelper<UInt128>.CreateChecked<NFloat>(float.MaxValue));
            }

            Assert.Throws<OverflowException>(() => NumberBaseHelper<UInt128>.CreateChecked<NFloat>(-NFloat.Epsilon));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<UInt128>.CreateChecked<NFloat>(-1.0f));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<UInt128>.CreateChecked<NFloat>(NFloat.MinValue));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<UInt128>.CreateChecked<NFloat>(NFloat.PositiveInfinity));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<UInt128>.CreateChecked<NFloat>(NFloat.NegativeInfinity));
        }

        [Fact]
        public static void CreateCheckedFromSByteTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateChecked<sbyte>(0x00));
            Assert.Equal(One, NumberBaseHelper<UInt128>.CreateChecked<sbyte>(0x01));
            Assert.Equal(SByteMaxValue, NumberBaseHelper<UInt128>.CreateChecked<sbyte>(0x7F));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<UInt128>.CreateChecked<sbyte>(unchecked((sbyte)0x80)));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<UInt128>.CreateChecked<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateCheckedFromSingleTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateChecked<float>(+0.0f));
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateChecked<float>(-0.0f));

            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateChecked<float>(+float.Epsilon));
            Assert.Equal(One, NumberBaseHelper<UInt128>.CreateChecked<float>(+1.0f));

            Assert.Equal(new UInt128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000), NumberBaseHelper<UInt128>.CreateChecked<float>(+170141183460469231731687303715884105728.0f));
            Assert.Equal(new UInt128(0xFFFF_FF00_0000_0000, 0x0000_0000_0000_0000), NumberBaseHelper<UInt128>.CreateChecked<float>(float.MaxValue));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<UInt128>.CreateChecked<float>(-float.Epsilon));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<UInt128>.CreateChecked<float>(-1.0f));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<UInt128>.CreateChecked<float>(float.MinValue));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<UInt128>.CreateChecked<float>(float.PositiveInfinity));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<UInt128>.CreateChecked<float>(float.NegativeInfinity));
        }

        [Fact]
        public static void CreateCheckedFromUInt16Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateChecked<ushort>(0x0000));
            Assert.Equal(One, NumberBaseHelper<UInt128>.CreateChecked<ushort>(0x0001));
            Assert.Equal(Int16MaxValue, NumberBaseHelper<UInt128>.CreateChecked<ushort>(0x7FFF));
            Assert.Equal(Int16MaxValuePlusOne, NumberBaseHelper<UInt128>.CreateChecked<ushort>(0x8000));
            Assert.Equal(UInt16MaxValue, NumberBaseHelper<UInt128>.CreateChecked<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUInt32Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateChecked<uint>(0x00000000));
            Assert.Equal(One, NumberBaseHelper<UInt128>.CreateChecked<uint>(0x00000001));
            Assert.Equal(Int32MaxValue, NumberBaseHelper<UInt128>.CreateChecked<uint>(0x7FFFFFFF));
            Assert.Equal(Int32MaxValuePlusOne, NumberBaseHelper<UInt128>.CreateChecked<uint>(0x80000000));
            Assert.Equal(UInt32MaxValue, NumberBaseHelper<UInt128>.CreateChecked<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUInt64Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateChecked<ulong>(0x0000000000000000));
            Assert.Equal(One, NumberBaseHelper<UInt128>.CreateChecked<ulong>(0x0000000000000001));
            Assert.Equal(Int64MaxValue, NumberBaseHelper<UInt128>.CreateChecked<ulong>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal(Int64MaxValuePlusOne, NumberBaseHelper<UInt128>.CreateChecked<ulong>(0x8000000000000000));
            Assert.Equal(UInt64MaxValue, NumberBaseHelper<UInt128>.CreateChecked<ulong>(0xFFFFFFFFFFFFFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUInt128Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateChecked<UInt128>(UInt128.Zero));
            Assert.Equal(One, NumberBaseHelper<UInt128>.CreateChecked<UInt128>(UInt128.One));
            Assert.Equal(Int128MaxValue, NumberBaseHelper<UInt128>.CreateChecked<UInt128>(Int128MaxValue));
            Assert.Equal(Int128MaxValuePlusOne, NumberBaseHelper<UInt128>.CreateChecked<UInt128>(Int128MaxValuePlusOne));
            Assert.Equal(MaxValue, NumberBaseHelper<UInt128>.CreateChecked<UInt128>(UInt128.MaxValue));
        }

        [Fact]
        public static void CreateCheckedFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateChecked<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal(One, NumberBaseHelper<UInt128>.CreateChecked<nuint>(unchecked((nuint)0x0000000000000001)));
                Assert.Equal(Int64MaxValue, NumberBaseHelper<UInt128>.CreateChecked<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(Int64MaxValuePlusOne, NumberBaseHelper<UInt128>.CreateChecked<nuint>(unchecked((nuint)0x8000000000000000)));
                Assert.Equal(UInt64MaxValue, NumberBaseHelper<UInt128>.CreateChecked<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateChecked<nuint>((nuint)0x00000000));
                Assert.Equal(One, NumberBaseHelper<UInt128>.CreateChecked<nuint>((nuint)0x00000001));
                Assert.Equal(Int32MaxValue, NumberBaseHelper<UInt128>.CreateChecked<nuint>((nuint)0x7FFFFFFF));
                Assert.Equal(Int32MaxValuePlusOne, NumberBaseHelper<UInt128>.CreateChecked<nuint>((nuint)0x80000000));
                Assert.Equal(UInt32MaxValue, NumberBaseHelper<UInt128>.CreateChecked<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateSaturatingFromByteTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateSaturating<byte>(0x00));
            Assert.Equal(One, NumberBaseHelper<UInt128>.CreateSaturating<byte>(0x01));
            Assert.Equal(SByteMaxValue, NumberBaseHelper<UInt128>.CreateSaturating<byte>(0x7F));
            Assert.Equal(SByteMaxValuePlusOne, NumberBaseHelper<UInt128>.CreateSaturating<byte>(0x80));
            Assert.Equal(ByteMaxValue, NumberBaseHelper<UInt128>.CreateSaturating<byte>(0xFF));
        }

        [Fact]
        public static void CreateSaturatingFromCharTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateSaturating<char>((char)0x0000));
            Assert.Equal(One, NumberBaseHelper<UInt128>.CreateSaturating<char>((char)0x0001));
            Assert.Equal(Int16MaxValue, NumberBaseHelper<UInt128>.CreateSaturating<char>((char)0x7FFF));
            Assert.Equal(Int16MaxValuePlusOne, NumberBaseHelper<UInt128>.CreateSaturating<char>((char)0x8000));
            Assert.Equal(UInt16MaxValue, NumberBaseHelper<UInt128>.CreateSaturating<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromDecimalTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateSaturating<decimal>(decimal.Zero));
            Assert.Equal(One, NumberBaseHelper<UInt128>.CreateSaturating<decimal>(decimal.One));

            Assert.Equal(new UInt128(0x0000_0000_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), NumberBaseHelper<UInt128>.CreateSaturating<decimal>(decimal.MaxValue));

            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateSaturating<decimal>(decimal.MinValue));
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateSaturating<decimal>(decimal.MinusOne));
        }

        [Fact]
        public static void CreateSaturatingFromDoubleTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateSaturating<double>(+0.0));
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateSaturating<double>(-0.0));

            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateSaturating<double>(+double.Epsilon));
            Assert.Equal(One, NumberBaseHelper<UInt128>.CreateSaturating<double>(+1.0));

            Assert.Equal(new UInt128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000), NumberBaseHelper<UInt128>.CreateSaturating<double>(+170141183460469231731687303715884105728.0));
            Assert.Equal(new UInt128(0xFFFF_FFFF_FFFF_F800, 0x0000_0000_0000_0000), NumberBaseHelper<UInt128>.CreateSaturating<double>(+340282366920938425684442744474606501888.0));

            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateSaturating<double>(-double.Epsilon));
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateSaturating<double>(-1.0));

            Assert.Equal(MaxValue, NumberBaseHelper<UInt128>.CreateSaturating<double>(+340282366920938463463374607431768211456.0));
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateSaturating<double>(-340282366920938425684442744474606501888.0));

            Assert.Equal(MaxValue, NumberBaseHelper<UInt128>.CreateSaturating<double>(double.MaxValue));
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateSaturating<double>(double.MinValue));

            Assert.Equal(MaxValue, NumberBaseHelper<UInt128>.CreateSaturating<double>(double.PositiveInfinity));
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateSaturating<double>(double.NegativeInfinity));
        }

        [Fact]
        public static void CreateSaturatingFromHalfTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateSaturating<Half>((Half)(+0.0)));
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateSaturating<Half>((Half)(-0.0)));

            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateSaturating<Half>(+Half.Epsilon));
            Assert.Equal(One, NumberBaseHelper<UInt128>.CreateSaturating<Half>((Half)(+1.0)));
            Assert.Equal(+65504U, NumberBaseHelper<UInt128>.CreateSaturating<Half>(Half.MaxValue));

            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateSaturating<Half>(-Half.Epsilon));
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateSaturating<Half>((Half)(-1.0)));
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateSaturating<Half>(Half.MinValue));

            Assert.Equal(MaxValue, NumberBaseHelper<UInt128>.CreateSaturating<Half>(Half.PositiveInfinity));
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateSaturating<Half>(Half.NegativeInfinity));
        }

        [Fact]
        public static void CreateSaturatingFromInt16Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateSaturating<short>(0x0000));
            Assert.Equal(One, NumberBaseHelper<UInt128>.CreateSaturating<short>(0x0001));
            Assert.Equal(Int16MaxValue, NumberBaseHelper<UInt128>.CreateSaturating<short>(0x7FFF));
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateSaturating<short>(unchecked((short)0x8000)));
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateSaturating<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromInt32Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateSaturating<int>(0x00000000));
            Assert.Equal(One, NumberBaseHelper<UInt128>.CreateSaturating<int>(0x00000001));
            Assert.Equal(Int32MaxValue, NumberBaseHelper<UInt128>.CreateSaturating<int>(0x7FFFFFFF));
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateSaturating<int>(unchecked((int)0x80000000)));
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateSaturating<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromInt64Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateSaturating<long>(0x0000000000000000));
            Assert.Equal(One, NumberBaseHelper<UInt128>.CreateSaturating<long>(0x0000000000000001));
            Assert.Equal(Int64MaxValue, NumberBaseHelper<UInt128>.CreateSaturating<long>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateSaturating<long>(unchecked((long)0x8000000000000000)));
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateSaturating<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromInt128Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateSaturating<Int128>(Int128.Zero));
            Assert.Equal(One, NumberBaseHelper<UInt128>.CreateSaturating<Int128>(Int128.One));
            Assert.Equal(Int128MaxValue, NumberBaseHelper<UInt128>.CreateSaturating<Int128>(Int128.MaxValue));
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateSaturating<Int128>(Int128.MinValue));
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateSaturating<Int128>(Int128.NegativeOne));
        }

        [Fact]
        public static void CreateSaturatingFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateSaturating<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal(One, NumberBaseHelper<UInt128>.CreateSaturating<nint>(unchecked((nint)0x0000000000000001)));
                Assert.Equal(Int64MaxValue, NumberBaseHelper<UInt128>.CreateSaturating<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateSaturating<nint>(unchecked((nint)0x8000000000000000)));
                Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateSaturating<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateSaturating<nint>((nint)0x00000000));
                Assert.Equal(One, NumberBaseHelper<UInt128>.CreateSaturating<nint>((nint)0x00000001));
                Assert.Equal(Int32MaxValue, NumberBaseHelper<UInt128>.CreateSaturating<nint>((nint)0x7FFFFFFF));
                Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateSaturating<nint>(unchecked((nint)0x80000000)));
                Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateSaturating<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateSaturatingFromNFloatTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateSaturating<NFloat>(+0.0f));
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateSaturating<NFloat>(-0.0f));

            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateSaturating<NFloat>(+NFloat.Epsilon));
            Assert.Equal(One, NumberBaseHelper<UInt128>.CreateSaturating<NFloat>(+1.0f));

            if (Environment.Is64BitProcess)
            {
                Assert.Equal(new UInt128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000), NumberBaseHelper<UInt128>.CreateSaturating<NFloat>((NFloat)(+170141183460469231731687303715884105728.0)));
                Assert.Equal(new UInt128(0xFFFF_FFFF_FFFF_F800, 0x0000_0000_0000_0000), NumberBaseHelper<UInt128>.CreateSaturating<NFloat>((NFloat)(+340282366920938425684442744474606501888.0)));

                Assert.Equal(MaxValue, NumberBaseHelper<UInt128>.CreateSaturating<NFloat>((NFloat)(+340282366920938463463374607431768211456.0)));
                Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateSaturating<NFloat>((NFloat)(-340282366920938425684442744474606501888.0)));

                Assert.Equal(MaxValue, NumberBaseHelper<UInt128>.CreateSaturating<NFloat>(NFloat.MaxValue));
            }
            else
            {
                Assert.Equal(new UInt128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000), NumberBaseHelper<UInt128>.CreateSaturating<NFloat>(+170141183460469231731687303715884105728.0f));
                Assert.Equal(new UInt128(0xFFFF_FF00_0000_0000, 0x0000_0000_0000_0000), NumberBaseHelper<UInt128>.CreateSaturating<NFloat>(float.MaxValue));
            }

            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateSaturating<NFloat>(-NFloat.Epsilon));
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateSaturating<NFloat>(-1.0f));

            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateSaturating<NFloat>(NFloat.MinValue));

            Assert.Equal(MaxValue, NumberBaseHelper<UInt128>.CreateSaturating<NFloat>(NFloat.PositiveInfinity));
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateSaturating<NFloat>(NFloat.NegativeInfinity));
        }

        [Fact]
        public static void CreateSaturatingFromSByteTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateSaturating<sbyte>(0x00));
            Assert.Equal(One, NumberBaseHelper<UInt128>.CreateSaturating<sbyte>(0x01));
            Assert.Equal(SByteMaxValue, NumberBaseHelper<UInt128>.CreateSaturating<sbyte>(0x7F));
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateSaturating<sbyte>(unchecked((sbyte)0x80)));
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateSaturating<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateSaturatingFromSingleTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateSaturating<float>(+0.0f));
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateSaturating<float>(-0.0f));

            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateSaturating<float>(+float.Epsilon));
            Assert.Equal(One, NumberBaseHelper<UInt128>.CreateSaturating<float>(+1.0f));

            Assert.Equal(new UInt128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000), NumberBaseHelper<UInt128>.CreateSaturating<float>(+170141183460469231731687303715884105728.0f));
            Assert.Equal(new UInt128(0xFFFF_FF00_0000_0000, 0x0000_0000_0000_0000), NumberBaseHelper<UInt128>.CreateSaturating<float>(float.MaxValue));

            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateSaturating<float>(-float.Epsilon));
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateSaturating<float>(-1.0f));
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateSaturating<float>(float.MinValue));

            Assert.Equal(MaxValue, NumberBaseHelper<UInt128>.CreateSaturating<float>(float.PositiveInfinity));
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateSaturating<float>(float.NegativeInfinity));
        }

        [Fact]
        public static void CreateSaturatingFromUInt16Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateSaturating<ushort>(0x0000));
            Assert.Equal(One, NumberBaseHelper<UInt128>.CreateSaturating<ushort>(0x0001));
            Assert.Equal(Int16MaxValue, NumberBaseHelper<UInt128>.CreateSaturating<ushort>(0x7FFF));
            Assert.Equal(Int16MaxValuePlusOne, NumberBaseHelper<UInt128>.CreateSaturating<ushort>(0x8000));
            Assert.Equal(UInt16MaxValue, NumberBaseHelper<UInt128>.CreateSaturating<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUInt32Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateSaturating<uint>(0x00000000));
            Assert.Equal(One, NumberBaseHelper<UInt128>.CreateSaturating<uint>(0x00000001));
            Assert.Equal(Int32MaxValue, NumberBaseHelper<UInt128>.CreateSaturating<uint>(0x7FFFFFFF));
            Assert.Equal(Int32MaxValuePlusOne, NumberBaseHelper<UInt128>.CreateSaturating<uint>(0x80000000));
            Assert.Equal(UInt32MaxValue, NumberBaseHelper<UInt128>.CreateSaturating<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUInt64Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateSaturating<ulong>(0x0000000000000000));
            Assert.Equal(One, NumberBaseHelper<UInt128>.CreateSaturating<ulong>(0x0000000000000001));
            Assert.Equal(Int64MaxValue, NumberBaseHelper<UInt128>.CreateSaturating<ulong>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal(Int64MaxValuePlusOne, NumberBaseHelper<UInt128>.CreateSaturating<ulong>(0x8000000000000000));
            Assert.Equal(UInt64MaxValue, NumberBaseHelper<UInt128>.CreateSaturating<ulong>(0xFFFFFFFFFFFFFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUInt128Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateSaturating<UInt128>(UInt128.Zero));
            Assert.Equal(One, NumberBaseHelper<UInt128>.CreateSaturating<UInt128>(UInt128.One));
            Assert.Equal(Int128MaxValue, NumberBaseHelper<UInt128>.CreateSaturating<UInt128>(Int128MaxValue));
            Assert.Equal(Int128MaxValuePlusOne, NumberBaseHelper<UInt128>.CreateSaturating<UInt128>(Int128MaxValuePlusOne));
            Assert.Equal(MaxValue, NumberBaseHelper<UInt128>.CreateSaturating<UInt128>(UInt128.MaxValue));
        }

        [Fact]
        public static void CreateSaturatingFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateSaturating<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal(One, NumberBaseHelper<UInt128>.CreateSaturating<nuint>(unchecked((nuint)0x0000000000000001)));
                Assert.Equal(Int64MaxValue, NumberBaseHelper<UInt128>.CreateSaturating<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(Int64MaxValuePlusOne, NumberBaseHelper<UInt128>.CreateSaturating<nuint>(unchecked((nuint)0x8000000000000000)));
                Assert.Equal(UInt64MaxValue, NumberBaseHelper<UInt128>.CreateSaturating<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateSaturating<nuint>((nuint)0x00000000));
                Assert.Equal(One, NumberBaseHelper<UInt128>.CreateSaturating<nuint>((nuint)0x00000001));
                Assert.Equal(Int32MaxValue, NumberBaseHelper<UInt128>.CreateSaturating<nuint>((nuint)0x7FFFFFFF));
                Assert.Equal(Int32MaxValuePlusOne, NumberBaseHelper<UInt128>.CreateSaturating<nuint>((nuint)0x80000000));
                Assert.Equal(UInt32MaxValue, NumberBaseHelper<UInt128>.CreateSaturating<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateTruncatingFromByteTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateTruncating<byte>(0x00));
            Assert.Equal(One, NumberBaseHelper<UInt128>.CreateTruncating<byte>(0x01));
            Assert.Equal(SByteMaxValue, NumberBaseHelper<UInt128>.CreateTruncating<byte>(0x7F));
            Assert.Equal(SByteMaxValuePlusOne, NumberBaseHelper<UInt128>.CreateTruncating<byte>(0x80));
            Assert.Equal(ByteMaxValue, NumberBaseHelper<UInt128>.CreateTruncating<byte>(0xFF));
        }

        [Fact]
        public static void CreateTruncatingFromCharTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateTruncating<char>((char)0x0000));
            Assert.Equal(One, NumberBaseHelper<UInt128>.CreateTruncating<char>((char)0x0001));
            Assert.Equal(Int16MaxValue, NumberBaseHelper<UInt128>.CreateTruncating<char>((char)0x7FFF));
            Assert.Equal(Int16MaxValuePlusOne, NumberBaseHelper<UInt128>.CreateTruncating<char>((char)0x8000));
            Assert.Equal(UInt16MaxValue, NumberBaseHelper<UInt128>.CreateTruncating<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromDecimalTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateTruncating<decimal>(decimal.Zero));
            Assert.Equal(One, NumberBaseHelper<UInt128>.CreateTruncating<decimal>(decimal.One));

            Assert.Equal(new UInt128(0x0000_0000_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), NumberBaseHelper<UInt128>.CreateTruncating<decimal>(decimal.MaxValue));

            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateTruncating<decimal>(decimal.MinValue));
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateTruncating<decimal>(decimal.MinusOne));
        }

        [Fact]
        public static void CreateTruncatingFromDoubleTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateTruncating<double>(+0.0));
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateTruncating<double>(-0.0));

            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateTruncating<double>(+double.Epsilon));
            Assert.Equal(One, NumberBaseHelper<UInt128>.CreateTruncating<double>(+1.0));

            Assert.Equal(new UInt128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000), NumberBaseHelper<UInt128>.CreateTruncating<double>(+170141183460469231731687303715884105728.0));
            Assert.Equal(new UInt128(0xFFFF_FFFF_FFFF_F800, 0x0000_0000_0000_0000), NumberBaseHelper<UInt128>.CreateTruncating<double>(+340282366920938425684442744474606501888.0));

            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateTruncating<double>(-double.Epsilon));
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateTruncating<double>(-1.0));

            Assert.Equal(MaxValue, NumberBaseHelper<UInt128>.CreateTruncating<double>(+340282366920938463463374607431768211456.0));
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateTruncating<double>(-340282366920938425684442744474606501888.0));

            Assert.Equal(MaxValue, NumberBaseHelper<UInt128>.CreateTruncating<double>(double.MaxValue));
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateTruncating<double>(double.MinValue));

            Assert.Equal(MaxValue, NumberBaseHelper<UInt128>.CreateTruncating<double>(double.PositiveInfinity));
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateTruncating<double>(double.NegativeInfinity));
        }

        [Fact]
        public static void CreateTruncatingFromHalfTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateTruncating<Half>((Half)(+0.0)));
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateTruncating<Half>((Half)(-0.0)));

            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateTruncating<Half>(+Half.Epsilon));
            Assert.Equal(One, NumberBaseHelper<UInt128>.CreateTruncating<Half>((Half)(+1.0)));
            Assert.Equal(+65504U, NumberBaseHelper<UInt128>.CreateTruncating<Half>(Half.MaxValue));

            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateTruncating<Half>(-Half.Epsilon));
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateTruncating<Half>((Half)(-1.0)));
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateTruncating<Half>(Half.MinValue));

            Assert.Equal(MaxValue, NumberBaseHelper<UInt128>.CreateTruncating<Half>(Half.PositiveInfinity));
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateTruncating<Half>(Half.NegativeInfinity));
        }

        [Fact]
        public static void CreateTruncatingFromInt16Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateTruncating<short>(0x0000));
            Assert.Equal(One, NumberBaseHelper<UInt128>.CreateTruncating<short>(0x0001));
            Assert.Equal(Int16MaxValue, NumberBaseHelper<UInt128>.CreateTruncating<short>(0x7FFF));
            Assert.Equal(new UInt128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_8000), NumberBaseHelper<UInt128>.CreateTruncating<short>(unchecked((short)0x8000)));
            Assert.Equal(MaxValue, NumberBaseHelper<UInt128>.CreateTruncating<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateTruncatingFromInt32Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateTruncating<int>(0x00000000));
            Assert.Equal(One, NumberBaseHelper<UInt128>.CreateTruncating<int>(0x00000001));
            Assert.Equal(Int32MaxValue, NumberBaseHelper<UInt128>.CreateTruncating<int>(0x7FFFFFFF));
            Assert.Equal(new UInt128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_8000_0000), NumberBaseHelper<UInt128>.CreateTruncating<int>(unchecked((int)0x80000000)));
            Assert.Equal(MaxValue, NumberBaseHelper<UInt128>.CreateTruncating<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateTruncatingFromInt64Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateTruncating<long>(0x0000000000000000));
            Assert.Equal(One, NumberBaseHelper<UInt128>.CreateTruncating<long>(0x0000000000000001));
            Assert.Equal(Int64MaxValue, NumberBaseHelper<UInt128>.CreateTruncating<long>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal(new UInt128(0xFFFF_FFFF_FFFF_FFFF, 0x8000_0000_0000_0000), NumberBaseHelper<UInt128>.CreateTruncating<long>(unchecked((long)0x8000000000000000)));
            Assert.Equal(MaxValue, NumberBaseHelper<UInt128>.CreateTruncating<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void CreateTruncatingFromInt128Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateTruncating<Int128>(Int128.Zero));
            Assert.Equal(One, NumberBaseHelper<UInt128>.CreateTruncating<Int128>(Int128.One));
            Assert.Equal(Int128MaxValue, NumberBaseHelper<UInt128>.CreateTruncating<Int128>(Int128.MaxValue));
            Assert.Equal(Int128MaxValuePlusOne, NumberBaseHelper<UInt128>.CreateTruncating<Int128>(Int128.MinValue));
            Assert.Equal(MaxValue, NumberBaseHelper<UInt128>.CreateTruncating<Int128>(Int128.NegativeOne));
        }

        [Fact]
        public static void CreateTruncatingFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateTruncating<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal(One, NumberBaseHelper<UInt128>.CreateTruncating<nint>(unchecked((nint)0x0000000000000001)));
                Assert.Equal(Int64MaxValue, NumberBaseHelper<UInt128>.CreateTruncating<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(new UInt128(0xFFFF_FFFF_FFFF_FFFF, 0x8000_0000_0000_0000), NumberBaseHelper<UInt128>.CreateTruncating<nint>(unchecked((nint)0x8000000000000000)));
                Assert.Equal(MaxValue, NumberBaseHelper<UInt128>.CreateTruncating<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateTruncating<nint>((nint)0x00000000));
                Assert.Equal(One, NumberBaseHelper<UInt128>.CreateTruncating<nint>((nint)0x00000001));
                Assert.Equal(Int32MaxValue, NumberBaseHelper<UInt128>.CreateTruncating<nint>((nint)0x7FFFFFFF));
                Assert.Equal(new UInt128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_8000_0000), NumberBaseHelper<UInt128>.CreateTruncating<nint>(unchecked((nint)0x80000000)));
                Assert.Equal(MaxValue, NumberBaseHelper<UInt128>.CreateTruncating<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateTruncatingFromNFloatTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateTruncating<NFloat>(+0.0f));
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateTruncating<NFloat>(-0.0f));

            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateTruncating<NFloat>(+NFloat.Epsilon));
            Assert.Equal(One, NumberBaseHelper<UInt128>.CreateTruncating<NFloat>(+1.0f));

            if (Environment.Is64BitProcess)
            {
                Assert.Equal(new UInt128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000), NumberBaseHelper<UInt128>.CreateTruncating<NFloat>((NFloat)(+170141183460469231731687303715884105728.0)));
                Assert.Equal(new UInt128(0xFFFF_FFFF_FFFF_F800, 0x0000_0000_0000_0000), NumberBaseHelper<UInt128>.CreateTruncating<NFloat>((NFloat)(+340282366920938425684442744474606501888.0)));

                Assert.Equal(MaxValue, NumberBaseHelper<UInt128>.CreateTruncating<NFloat>((NFloat)(+340282366920938463463374607431768211456.0)));
                Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateTruncating<NFloat>((NFloat)(-340282366920938425684442744474606501888.0)));

                Assert.Equal(MaxValue, NumberBaseHelper<UInt128>.CreateTruncating<NFloat>(NFloat.MaxValue));
            }
            else
            {
                Assert.Equal(new UInt128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000), NumberBaseHelper<UInt128>.CreateTruncating<NFloat>(+170141183460469231731687303715884105728.0f));
                Assert.Equal(new UInt128(0xFFFF_FF00_0000_0000, 0x0000_0000_0000_0000), NumberBaseHelper<UInt128>.CreateTruncating<NFloat>(float.MaxValue));
            }

            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateTruncating<NFloat>(-NFloat.Epsilon));
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateTruncating<NFloat>(-1.0f));

            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateTruncating<NFloat>(NFloat.MinValue));

            Assert.Equal(MaxValue, NumberBaseHelper<UInt128>.CreateTruncating<NFloat>(NFloat.PositiveInfinity));
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateTruncating<NFloat>(NFloat.NegativeInfinity));
        }

        [Fact]
        public static void CreateTruncatingFromSByteTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateTruncating<sbyte>(0x00));
            Assert.Equal(One, NumberBaseHelper<UInt128>.CreateTruncating<sbyte>(0x01));
            Assert.Equal(SByteMaxValue, NumberBaseHelper<UInt128>.CreateTruncating<sbyte>(0x7F));
            Assert.Equal(new UInt128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FF80), NumberBaseHelper<UInt128>.CreateTruncating<sbyte>(unchecked((sbyte)0x80)));
            Assert.Equal(MaxValue, NumberBaseHelper<UInt128>.CreateTruncating<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateTruncatingFromSingleTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateTruncating<float>(+0.0f));
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateTruncating<float>(-0.0f));

            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateTruncating<float>(+float.Epsilon));
            Assert.Equal(One, NumberBaseHelper<UInt128>.CreateTruncating<float>(+1.0f));

            Assert.Equal(new UInt128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000), NumberBaseHelper<UInt128>.CreateTruncating<float>(+170141183460469231731687303715884105728.0f));
            Assert.Equal(new UInt128(0xFFFF_FF00_0000_0000, 0x0000_0000_0000_0000), NumberBaseHelper<UInt128>.CreateTruncating<float>(float.MaxValue));

            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateTruncating<float>(-float.Epsilon));
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateTruncating<float>(-1.0f));
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateTruncating<float>(float.MinValue));

            Assert.Equal(MaxValue, NumberBaseHelper<UInt128>.CreateTruncating<float>(float.PositiveInfinity));
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateTruncating<float>(float.NegativeInfinity));
        }

        [Fact]
        public static void CreateTruncatingFromUInt16Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateTruncating<ushort>(0x0000));
            Assert.Equal(One, NumberBaseHelper<UInt128>.CreateTruncating<ushort>(0x0001));
            Assert.Equal(Int16MaxValue, NumberBaseHelper<UInt128>.CreateTruncating<ushort>(0x7FFF));
            Assert.Equal(Int16MaxValuePlusOne, NumberBaseHelper<UInt128>.CreateTruncating<ushort>(0x8000));
            Assert.Equal(UInt16MaxValue, NumberBaseHelper<UInt128>.CreateTruncating<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt32Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateTruncating<uint>(0x00000000));
            Assert.Equal(One, NumberBaseHelper<UInt128>.CreateTruncating<uint>(0x00000001));
            Assert.Equal(Int32MaxValue, NumberBaseHelper<UInt128>.CreateTruncating<uint>(0x7FFFFFFF));
            Assert.Equal(Int32MaxValuePlusOne, NumberBaseHelper<UInt128>.CreateTruncating<uint>(0x80000000));
            Assert.Equal(UInt32MaxValue, NumberBaseHelper<UInt128>.CreateTruncating<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt64Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateTruncating<ulong>(0x0000000000000000));
            Assert.Equal(One, NumberBaseHelper<UInt128>.CreateTruncating<ulong>(0x0000000000000001));
            Assert.Equal(Int64MaxValue, NumberBaseHelper<UInt128>.CreateTruncating<ulong>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal(Int64MaxValuePlusOne, NumberBaseHelper<UInt128>.CreateTruncating<ulong>(0x8000000000000000));
            Assert.Equal(UInt64MaxValue, NumberBaseHelper<UInt128>.CreateTruncating<ulong>(0xFFFFFFFFFFFFFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt128Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateTruncating<UInt128>(UInt128.Zero));
            Assert.Equal(One, NumberBaseHelper<UInt128>.CreateTruncating<UInt128>(UInt128.One));
            Assert.Equal(Int128MaxValue, NumberBaseHelper<UInt128>.CreateTruncating<UInt128>(Int128MaxValue));
            Assert.Equal(Int128MaxValuePlusOne, NumberBaseHelper<UInt128>.CreateTruncating<UInt128>(Int128MaxValuePlusOne));
            Assert.Equal(MaxValue, NumberBaseHelper<UInt128>.CreateTruncating<UInt128>(UInt128.MaxValue));
        }

        [Fact]
        public static void CreateTruncatingFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateTruncating<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal(One, NumberBaseHelper<UInt128>.CreateTruncating<nuint>(unchecked((nuint)0x0000000000000001)));
                Assert.Equal(Int64MaxValue, NumberBaseHelper<UInt128>.CreateTruncating<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(Int64MaxValuePlusOne, NumberBaseHelper<UInt128>.CreateTruncating<nuint>(unchecked((nuint)0x8000000000000000)));
                Assert.Equal(UInt64MaxValue, NumberBaseHelper<UInt128>.CreateTruncating<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal(Zero, NumberBaseHelper<UInt128>.CreateTruncating<nuint>((nuint)0x00000000));
                Assert.Equal(One, NumberBaseHelper<UInt128>.CreateTruncating<nuint>((nuint)0x00000001));
                Assert.Equal(Int32MaxValue, NumberBaseHelper<UInt128>.CreateTruncating<nuint>((nuint)0x7FFFFFFF));
                Assert.Equal(Int32MaxValuePlusOne, NumberBaseHelper<UInt128>.CreateTruncating<nuint>((nuint)0x80000000));
                Assert.Equal(UInt32MaxValue, NumberBaseHelper<UInt128>.CreateTruncating<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void IsCanonicalTest()
        {
            Assert.True(NumberBaseHelper<UInt128>.IsCanonical(Zero));
            Assert.True(NumberBaseHelper<UInt128>.IsCanonical(One));
            Assert.True(NumberBaseHelper<UInt128>.IsCanonical(Int128MaxValue));
            Assert.True(NumberBaseHelper<UInt128>.IsCanonical(Int128MaxValuePlusOne));
            Assert.True(NumberBaseHelper<UInt128>.IsCanonical(MaxValue));
        }

        [Fact]
        public static void IsComplexNumberTest()
        {
            Assert.False(NumberBaseHelper<UInt128>.IsComplexNumber(Zero));
            Assert.False(NumberBaseHelper<UInt128>.IsComplexNumber(One));
            Assert.False(NumberBaseHelper<UInt128>.IsComplexNumber(Int128MaxValue));
            Assert.False(NumberBaseHelper<UInt128>.IsComplexNumber(Int128MaxValuePlusOne));
            Assert.False(NumberBaseHelper<UInt128>.IsComplexNumber(MaxValue));
        }

        [Fact]
        public static void IsEvenIntegerTest()
        {
            Assert.True(NumberBaseHelper<UInt128>.IsEvenInteger(Zero));
            Assert.False(NumberBaseHelper<UInt128>.IsEvenInteger(One));
            Assert.False(NumberBaseHelper<UInt128>.IsEvenInteger(Int128MaxValue));
            Assert.True(NumberBaseHelper<UInt128>.IsEvenInteger(Int128MaxValuePlusOne));
            Assert.False(NumberBaseHelper<UInt128>.IsEvenInteger(MaxValue));
        }

        [Fact]
        public static void IsFiniteTest()
        {
            Assert.True(NumberBaseHelper<UInt128>.IsFinite(Zero));
            Assert.True(NumberBaseHelper<UInt128>.IsFinite(One));
            Assert.True(NumberBaseHelper<UInt128>.IsFinite(Int128MaxValue));
            Assert.True(NumberBaseHelper<UInt128>.IsFinite(Int128MaxValuePlusOne));
            Assert.True(NumberBaseHelper<UInt128>.IsFinite(MaxValue));
        }

        [Fact]
        public static void IsImaginaryNumberTest()
        {
            Assert.False(NumberBaseHelper<UInt128>.IsImaginaryNumber(Zero));
            Assert.False(NumberBaseHelper<UInt128>.IsImaginaryNumber(One));
            Assert.False(NumberBaseHelper<UInt128>.IsImaginaryNumber(Int128MaxValue));
            Assert.False(NumberBaseHelper<UInt128>.IsImaginaryNumber(Int128MaxValuePlusOne));
            Assert.False(NumberBaseHelper<UInt128>.IsImaginaryNumber(MaxValue));
        }

        [Fact]
        public static void IsInfinityTest()
        {
            Assert.False(NumberBaseHelper<UInt128>.IsInfinity(Zero));
            Assert.False(NumberBaseHelper<UInt128>.IsInfinity(One));
            Assert.False(NumberBaseHelper<UInt128>.IsInfinity(Int128MaxValue));
            Assert.False(NumberBaseHelper<UInt128>.IsInfinity(Int128MaxValuePlusOne));
            Assert.False(NumberBaseHelper<UInt128>.IsInfinity(MaxValue));
        }

        [Fact]
        public static void IsIntegerTest()
        {
            Assert.True(NumberBaseHelper<UInt128>.IsInteger(Zero));
            Assert.True(NumberBaseHelper<UInt128>.IsInteger(One));
            Assert.True(NumberBaseHelper<UInt128>.IsInteger(Int128MaxValue));
            Assert.True(NumberBaseHelper<UInt128>.IsInteger(Int128MaxValuePlusOne));
            Assert.True(NumberBaseHelper<UInt128>.IsInteger(MaxValue));
        }

        [Fact]
        public static void IsNaNTest()
        {
            Assert.False(NumberBaseHelper<UInt128>.IsNaN(Zero));
            Assert.False(NumberBaseHelper<UInt128>.IsNaN(One));
            Assert.False(NumberBaseHelper<UInt128>.IsNaN(Int128MaxValue));
            Assert.False(NumberBaseHelper<UInt128>.IsNaN(Int128MaxValuePlusOne));
            Assert.False(NumberBaseHelper<UInt128>.IsNaN(MaxValue));
        }

        [Fact]
        public static void IsNegativeTest()
        {
            Assert.False(NumberBaseHelper<UInt128>.IsNegative(Zero));
            Assert.False(NumberBaseHelper<UInt128>.IsNegative(One));
            Assert.False(NumberBaseHelper<UInt128>.IsNegative(Int128MaxValue));
            Assert.False(NumberBaseHelper<UInt128>.IsNegative(Int128MaxValuePlusOne));
            Assert.False(NumberBaseHelper<UInt128>.IsNegative(MaxValue));
        }

        [Fact]
        public static void IsNegativeInfinityTest()
        {
            Assert.False(NumberBaseHelper<UInt128>.IsNegativeInfinity(Zero));
            Assert.False(NumberBaseHelper<UInt128>.IsNegativeInfinity(One));
            Assert.False(NumberBaseHelper<UInt128>.IsNegativeInfinity(Int128MaxValue));
            Assert.False(NumberBaseHelper<UInt128>.IsNegativeInfinity(Int128MaxValuePlusOne));
            Assert.False(NumberBaseHelper<UInt128>.IsNegativeInfinity(MaxValue));
        }

        [Fact]
        public static void IsNormalTest()
        {
            Assert.False(NumberBaseHelper<UInt128>.IsNormal(Zero));
            Assert.True(NumberBaseHelper<UInt128>.IsNormal(One));
            Assert.True(NumberBaseHelper<UInt128>.IsNormal(Int128MaxValue));
            Assert.True(NumberBaseHelper<UInt128>.IsNormal(Int128MaxValuePlusOne));
            Assert.True(NumberBaseHelper<UInt128>.IsNormal(MaxValue));
        }

        [Fact]
        public static void IsOddIntegerTest()
        {
            Assert.False(NumberBaseHelper<UInt128>.IsOddInteger(Zero));
            Assert.True(NumberBaseHelper<UInt128>.IsOddInteger(One));
            Assert.True(NumberBaseHelper<UInt128>.IsOddInteger(Int128MaxValue));
            Assert.False(NumberBaseHelper<UInt128>.IsOddInteger(Int128MaxValuePlusOne));
            Assert.True(NumberBaseHelper<UInt128>.IsOddInteger(MaxValue));
        }

        [Fact]
        public static void IsPositiveTest()
        {
            Assert.True(NumberBaseHelper<UInt128>.IsPositive(Zero));
            Assert.True(NumberBaseHelper<UInt128>.IsPositive(One));
            Assert.True(NumberBaseHelper<UInt128>.IsPositive(Int128MaxValue));
            Assert.True(NumberBaseHelper<UInt128>.IsPositive(Int128MaxValuePlusOne));
            Assert.True(NumberBaseHelper<UInt128>.IsPositive(MaxValue));
        }

        [Fact]
        public static void IsPositiveInfinityTest()
        {
            Assert.False(NumberBaseHelper<UInt128>.IsPositiveInfinity(Zero));
            Assert.False(NumberBaseHelper<UInt128>.IsPositiveInfinity(One));
            Assert.False(NumberBaseHelper<UInt128>.IsPositiveInfinity(Int128MaxValue));
            Assert.False(NumberBaseHelper<UInt128>.IsPositiveInfinity(Int128MaxValuePlusOne));
            Assert.False(NumberBaseHelper<UInt128>.IsPositiveInfinity(MaxValue));
        }

        [Fact]
        public static void IsRealNumberTest()
        {
            Assert.True(NumberBaseHelper<UInt128>.IsRealNumber(Zero));
            Assert.True(NumberBaseHelper<UInt128>.IsRealNumber(One));
            Assert.True(NumberBaseHelper<UInt128>.IsRealNumber(Int128MaxValue));
            Assert.True(NumberBaseHelper<UInt128>.IsRealNumber(Int128MaxValuePlusOne));
            Assert.True(NumberBaseHelper<UInt128>.IsRealNumber(MaxValue));
        }

        [Fact]
        public static void IsSubnormalTest()
        {
            Assert.False(NumberBaseHelper<UInt128>.IsSubnormal(Zero));
            Assert.False(NumberBaseHelper<UInt128>.IsSubnormal(One));
            Assert.False(NumberBaseHelper<UInt128>.IsSubnormal(Int128MaxValue));
            Assert.False(NumberBaseHelper<UInt128>.IsSubnormal(Int128MaxValuePlusOne));
            Assert.False(NumberBaseHelper<UInt128>.IsSubnormal(MaxValue));
        }

        [Fact]
        public static void IsZeroTest()
        {
            Assert.True(NumberBaseHelper<UInt128>.IsZero(Zero));
            Assert.False(NumberBaseHelper<UInt128>.IsZero(One));
            Assert.False(NumberBaseHelper<UInt128>.IsZero(Int128MaxValue));
            Assert.False(NumberBaseHelper<UInt128>.IsZero(Int128MaxValuePlusOne));
            Assert.False(NumberBaseHelper<UInt128>.IsZero(MaxValue));
        }

        [Fact]
        public static void MaxMagnitudeTest()
        {
            Assert.Equal(One, NumberBaseHelper<UInt128>.MaxMagnitude(Zero, 1));
            Assert.Equal(One, NumberBaseHelper<UInt128>.MaxMagnitude(One, 1));
            Assert.Equal(Int128MaxValue, NumberBaseHelper<UInt128>.MaxMagnitude(Int128MaxValue, 1));
            Assert.Equal(Int128MaxValuePlusOne, NumberBaseHelper<UInt128>.MaxMagnitude(Int128MaxValuePlusOne, 1));
            Assert.Equal(MaxValue, NumberBaseHelper<UInt128>.MaxMagnitude(MaxValue, 1));
        }

        [Fact]
        public static void MaxMagnitudeNumberTest()
        {
            Assert.Equal(One, NumberBaseHelper<UInt128>.MaxMagnitudeNumber(Zero, 1));
            Assert.Equal(One, NumberBaseHelper<UInt128>.MaxMagnitudeNumber(One, 1));
            Assert.Equal(Int128MaxValue, NumberBaseHelper<UInt128>.MaxMagnitudeNumber(Int128MaxValue, 1));
            Assert.Equal(Int128MaxValuePlusOne, NumberBaseHelper<UInt128>.MaxMagnitudeNumber(Int128MaxValuePlusOne, 1));
            Assert.Equal(MaxValue, NumberBaseHelper<UInt128>.MaxMagnitudeNumber(MaxValue, 1));
        }

        [Fact]
        public static void MinMagnitudeTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.MinMagnitude(Zero, 1));
            Assert.Equal(One, NumberBaseHelper<UInt128>.MinMagnitude(One, 1));
            Assert.Equal(One, NumberBaseHelper<UInt128>.MinMagnitude(Int128MaxValue, 1));
            Assert.Equal(One, NumberBaseHelper<UInt128>.MinMagnitude(Int128MaxValuePlusOne, 1));
            Assert.Equal(One, NumberBaseHelper<UInt128>.MinMagnitude(MaxValue, 1));
        }

        [Fact]
        public static void MinMagnitudeNumberTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<UInt128>.MinMagnitudeNumber(Zero, 1));
            Assert.Equal(One, NumberBaseHelper<UInt128>.MinMagnitudeNumber(One, 1));
            Assert.Equal(One, NumberBaseHelper<UInt128>.MinMagnitudeNumber(Int128MaxValue, 1));
            Assert.Equal(One, NumberBaseHelper<UInt128>.MinMagnitudeNumber(Int128MaxValuePlusOne, 1));
            Assert.Equal(One, NumberBaseHelper<UInt128>.MinMagnitudeNumber(MaxValue, 1));
        }

        //
        // IShiftOperators
        //

        [Fact]
        public static void op_LeftShiftTest()
        {
            Assert.Equal(Zero, ShiftOperatorsHelper<UInt128, int, UInt128>.op_LeftShift(Zero, 1));
            Assert.Equal(Two, ShiftOperatorsHelper<UInt128, int, UInt128>.op_LeftShift(One, 1));
            Assert.Equal(MaxValueMinusOne, ShiftOperatorsHelper<UInt128, int, UInt128>.op_LeftShift(Int128MaxValue, 1));
            Assert.Equal(Zero, ShiftOperatorsHelper<UInt128, int, UInt128>.op_LeftShift(Int128MaxValuePlusOne, 1));
            Assert.Equal(MaxValueMinusOne, ShiftOperatorsHelper<UInt128, int, UInt128>.op_LeftShift(MaxValue, 1));
        }

        [Fact]
        public static void op_RightShiftTest()
        {
            Assert.Equal(Zero, ShiftOperatorsHelper<UInt128, int, UInt128>.op_RightShift(Zero, 1));
            Assert.Equal(Zero, ShiftOperatorsHelper<UInt128, int, UInt128>.op_RightShift(One, 1));
            Assert.Equal(new UInt128(0x3FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), ShiftOperatorsHelper<UInt128, int, UInt128>.op_RightShift(Int128MaxValue, 1));
            Assert.Equal(new UInt128(0x4000_0000_0000_0000, 0x0000_0000_0000_0000), ShiftOperatorsHelper<UInt128, int, UInt128>.op_RightShift(Int128MaxValuePlusOne, 1));
            Assert.Equal(Int128MaxValue, ShiftOperatorsHelper<UInt128, int, UInt128>.op_RightShift(MaxValue, 1));
        }

        [Fact]
        public static void op_UnsignedRightShiftTest()
        {
            Assert.Equal(Zero, ShiftOperatorsHelper<UInt128, int, UInt128>.op_UnsignedRightShift(Zero, 1));
            Assert.Equal(Zero, ShiftOperatorsHelper<UInt128, int, UInt128>.op_UnsignedRightShift(One, 1));
            Assert.Equal(new UInt128(0x3FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), ShiftOperatorsHelper<UInt128, int, UInt128>.op_UnsignedRightShift(Int128MaxValue, 1));
            Assert.Equal(new UInt128(0x4000_0000_0000_0000, 0x0000_0000_0000_0000), ShiftOperatorsHelper<UInt128, int, UInt128>.op_UnsignedRightShift(Int128MaxValuePlusOne, 1));
            Assert.Equal(Int128MaxValue, ShiftOperatorsHelper<UInt128, int, UInt128>.op_UnsignedRightShift(MaxValue, 1));
        }

        //
        // ISubtractionOperators
        //

        [Fact]
        public static void op_SubtractionTest()
        {
            Assert.Equal(MaxValue, SubtractionOperatorsHelper<UInt128, UInt128, UInt128>.op_Subtraction(Zero, 1U));
            Assert.Equal(Zero, SubtractionOperatorsHelper<UInt128, UInt128, UInt128>.op_Subtraction(One, 1U));
            Assert.Equal(Int128MaxValueMinusOne, SubtractionOperatorsHelper<UInt128, UInt128, UInt128>.op_Subtraction(Int128MaxValue, 1U));
            Assert.Equal(Int128MaxValue, SubtractionOperatorsHelper<UInt128, UInt128, UInt128>.op_Subtraction(Int128MaxValuePlusOne, 1U));
            Assert.Equal(MaxValueMinusOne, SubtractionOperatorsHelper<UInt128, UInt128, UInt128>.op_Subtraction(MaxValue, 1U));
        }

        [Fact]
        public static void op_CheckedSubtractionTest()
        {
            Assert.Equal(Zero, SubtractionOperatorsHelper<UInt128, UInt128, UInt128>.op_CheckedSubtraction(One, 1U));
            Assert.Equal(Int128MaxValueMinusOne, SubtractionOperatorsHelper<UInt128, UInt128, UInt128>.op_CheckedSubtraction(Int128MaxValue, 1U));
            Assert.Equal(Int128MaxValue, SubtractionOperatorsHelper<UInt128, UInt128, UInt128>.op_CheckedSubtraction(Int128MaxValuePlusOne, 1U));
            Assert.Equal(MaxValueMinusOne, SubtractionOperatorsHelper<UInt128, UInt128, UInt128>.op_CheckedSubtraction(MaxValue, 1U));

            Assert.Throws<OverflowException>(() => SubtractionOperatorsHelper<UInt128, UInt128, UInt128>.op_CheckedSubtraction(Zero, 1U));
        }

        //
        // IUnaryNegationOperators
        //

        [Fact]
        public static void op_UnaryNegationTest()
        {
            Assert.Equal(Zero, UnaryNegationOperatorsHelper<UInt128, UInt128>.op_UnaryNegation(Zero));
            Assert.Equal(MaxValue, UnaryNegationOperatorsHelper<UInt128, UInt128>.op_UnaryNegation(One));
            Assert.Equal(Int128MaxValuePlusTwo, UnaryNegationOperatorsHelper<UInt128, UInt128>.op_UnaryNegation(Int128MaxValue));
            Assert.Equal(Int128MaxValuePlusOne, UnaryNegationOperatorsHelper<UInt128, UInt128>.op_UnaryNegation(Int128MaxValuePlusOne));
            Assert.Equal(One, UnaryNegationOperatorsHelper<UInt128, UInt128>.op_UnaryNegation(MaxValue));
        }

        [Fact]
        public static void op_CheckedUnaryNegationTest()
        {
            Assert.Equal(Zero, UnaryNegationOperatorsHelper<UInt128, UInt128>.op_CheckedUnaryNegation(Zero));

            Assert.Throws<OverflowException>(() => UnaryNegationOperatorsHelper<UInt128, UInt128>.op_CheckedUnaryNegation(One));
            Assert.Throws<OverflowException>(() => UnaryNegationOperatorsHelper<UInt128, UInt128>.op_CheckedUnaryNegation(Int128MaxValue));
            Assert.Throws<OverflowException>(() => UnaryNegationOperatorsHelper<UInt128, UInt128>.op_CheckedUnaryNegation(Int128MaxValuePlusOne));
            Assert.Throws<OverflowException>(() => UnaryNegationOperatorsHelper<UInt128, UInt128>.op_CheckedUnaryNegation(MaxValue));
        }

        //
        // IUnaryPlusOperators
        //

        [Fact]
        public static void op_UnaryPlusTest()
        {
            Assert.Equal(Zero, UnaryPlusOperatorsHelper<UInt128, UInt128>.op_UnaryPlus(Zero));
            Assert.Equal(One, UnaryPlusOperatorsHelper<UInt128, UInt128>.op_UnaryPlus(One));
            Assert.Equal(Int128MaxValue, UnaryPlusOperatorsHelper<UInt128, UInt128>.op_UnaryPlus(Int128MaxValue));
            Assert.Equal(Int128MaxValuePlusOne, UnaryPlusOperatorsHelper<UInt128, UInt128>.op_UnaryPlus(Int128MaxValuePlusOne));
            Assert.Equal(MaxValue, UnaryPlusOperatorsHelper<UInt128, UInt128>.op_UnaryPlus(MaxValue));
        }
    }
}
