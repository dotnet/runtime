// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Xunit;

namespace System.Numerics.Tests
{
    public class BigIntegerTests_GenericMath
    {
        internal static readonly BigInteger ByteMaxValue = new BigInteger(byte.MaxValue);

        internal static readonly BigInteger Int16MaxValue = new BigInteger(short.MaxValue);

        internal static readonly BigInteger Int16MaxValuePlusOne = new BigInteger(short.MaxValue + 1U);

        internal static readonly BigInteger Int16MinValue = new BigInteger(short.MinValue);

        internal static readonly BigInteger Int32MaxValue = new BigInteger(int.MaxValue);

        internal static readonly BigInteger Int32MaxValuePlusOne = new BigInteger(int.MaxValue + 1U);

        internal static readonly BigInteger Int32MinValue = new BigInteger(int.MinValue);

        internal static readonly BigInteger Int64MaxValue = new BigInteger(long.MaxValue);

        internal static readonly BigInteger Int64MaxValueMinusOne = new BigInteger(long.MaxValue - 1L);

        internal static readonly BigInteger Int64MaxValuePlusOne = new BigInteger(long.MaxValue + 1UL);

        internal static readonly BigInteger Int64MaxValuePlusTwo = new BigInteger(long.MaxValue + 2UL);

        internal static readonly BigInteger Int64MinValue = new BigInteger(long.MinValue);

        internal static readonly BigInteger Int64MinValueMinusOne = new BigInteger(new byte[] {
            0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0x7F,
            0xFF, 0xFF, 0xFF, 0xFF
        });

        internal static readonly BigInteger Int64MinValuePlusOne = new BigInteger(long.MinValue + 1L);

        internal static readonly BigInteger Int64MinValueTimesTwo = new BigInteger(new byte[] {
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0xFF, 0xFF, 0xFF, 0xFF,
        });

        internal static readonly BigInteger Int128MaxValue = new BigInteger(new byte[] {
            0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0x7F,
        });

        internal static readonly BigInteger Int128MaxValuePlusOne = new BigInteger(new byte[] {
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x80,
        }, isUnsigned: true);

        internal static readonly BigInteger Int128MinValue = new BigInteger(new byte[] {
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x80,
        });

        internal static readonly BigInteger Int128MinValueMinusOne = new BigInteger(new byte[] {
            0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0x7F,
            0xFF, 0xFF, 0xFF, 0xFF
        });

        internal static readonly BigInteger Int128MinValuePlusOne = new BigInteger(new byte[] {
            0x01, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x80,
        });

        internal static readonly BigInteger Int128MinValueTimesTwo = new BigInteger(new byte[] {
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0xFF, 0xFF, 0xFF, 0xFF,
        });

        internal static readonly BigInteger TwoPow64 = new BigInteger(new byte[] {
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x01
        });

        internal static readonly BigInteger NegativeOne = new BigInteger(-1);

        internal static readonly BigInteger NegativeTwo = new BigInteger(-2);

        internal static readonly BigInteger NegativeTwoPow64 = new BigInteger(new byte[] {
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0xFF
        });

        internal static readonly BigInteger NegativeTwoPow64PlusOne = new BigInteger(new byte[] {
            0x01, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0xFF
        });

        internal static readonly BigInteger One = new BigInteger(1);

        internal static readonly BigInteger SByteMaxValue = new BigInteger(sbyte.MaxValue);

        internal static readonly BigInteger SByteMaxValuePlusOne = new BigInteger(sbyte.MaxValue + 1U);

        internal static readonly BigInteger SByteMinValue = new BigInteger(sbyte.MinValue);

        internal static readonly BigInteger Two = new BigInteger(2);

        internal static readonly BigInteger UInt16MaxValue = new BigInteger(ushort.MaxValue);

        internal static readonly BigInteger UInt32MaxValue = new BigInteger(uint.MaxValue);

        internal static readonly BigInteger UInt64MaxValue = new BigInteger(ulong.MaxValue);

        internal static readonly BigInteger UInt64MaxValueMinusOne = new BigInteger(ulong.MaxValue - 1);

        internal static readonly BigInteger UInt64MaxValueTimesTwo = new BigInteger(new byte[] {
            0xFE, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF,
            0x01
        }, isUnsigned: true);

        internal static readonly BigInteger UInt128MaxValue = new BigInteger(new byte[] {
            0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF,
        }, isUnsigned: true);

        internal static readonly BigInteger Zero = new BigInteger(0);

        //
        // IAdditionOperators
        //

        [Fact]
        public static void op_AdditionTest()
        {
            Assert.Equal(One, AdditionOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_Addition(Zero, 1));
            Assert.Equal(Two, AdditionOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_Addition(One, 1));
            Assert.Equal(Int64MaxValuePlusOne, AdditionOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_Addition(Int64MaxValue, 1));

            Assert.Equal(Int64MinValuePlusOne, AdditionOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_Addition(Int64MinValue, 1));
            Assert.Equal(Zero, AdditionOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_Addition(NegativeOne, 1));

            Assert.Equal(Int64MaxValuePlusTwo, AdditionOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_Addition(Int64MaxValuePlusOne, 1));
            Assert.Equal(TwoPow64, AdditionOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_Addition(UInt64MaxValue, 1));
        }

        [Fact]
        public static void op_CheckedAdditionTest()
        {
            Assert.Equal(One, AdditionOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_CheckedAddition(Zero, 1));
            Assert.Equal(Two, AdditionOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_CheckedAddition(One, 1));
            Assert.Equal(Int64MaxValuePlusOne, AdditionOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_CheckedAddition(Int64MaxValue, 1));

            Assert.Equal(Int64MinValuePlusOne, AdditionOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_CheckedAddition(Int64MinValue, 1));
            Assert.Equal(Zero, AdditionOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_CheckedAddition(NegativeOne, 1));

            Assert.Equal(Int64MaxValuePlusTwo, AdditionOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_CheckedAddition(Int64MaxValuePlusOne, 1));
            Assert.Equal(TwoPow64, AdditionOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_CheckedAddition(UInt64MaxValue, 1));
        }

        //
        // IAdditiveIdentity
        //

        [Fact]
        public static void AdditiveIdentityTest()
        {
            Assert.Equal(Zero, AdditiveIdentityHelper<BigInteger, BigInteger>.AdditiveIdentity);
        }

        //
        // IBinaryInteger
        //

        [Fact]
        public static void DivRemTest()
        {
            Assert.Equal((Zero, Zero), BinaryIntegerHelper<BigInteger>.DivRem(Zero, 2));
            Assert.Equal((Zero, One), BinaryIntegerHelper<BigInteger>.DivRem(One, 2));
            Assert.Equal(((BigInteger)0x3FFFFFFFFFFFFFFF, One), BinaryIntegerHelper<BigInteger>.DivRem(Int64MaxValue, 2));

            Assert.Equal((unchecked((BigInteger)(long)0xC000000000000000), Zero), BinaryIntegerHelper<BigInteger>.DivRem(Int64MinValue, 2));
            Assert.Equal((Zero, NegativeOne), BinaryIntegerHelper<BigInteger>.DivRem(NegativeOne, 2));

            Assert.Equal((unchecked((BigInteger)0x4000000000000000), Zero), BinaryIntegerHelper<BigInteger>.DivRem(Int64MaxValuePlusOne, 2));
            Assert.Equal((Int64MaxValue, One), BinaryIntegerHelper<BigInteger>.DivRem(UInt64MaxValue, 2));
        }

        [Fact]
        public static void LeadingZeroCountTest()
        {
            Assert.Equal((BigInteger)32, BinaryIntegerHelper<BigInteger>.LeadingZeroCount(Zero));
            Assert.Equal((BigInteger)31, BinaryIntegerHelper<BigInteger>.LeadingZeroCount(One));
            Assert.Equal((BigInteger)1, BinaryIntegerHelper<BigInteger>.LeadingZeroCount(Int64MaxValue));

            Assert.Equal((BigInteger)0, BinaryIntegerHelper<BigInteger>.LeadingZeroCount(Int64MinValue));
            Assert.Equal((BigInteger)0, BinaryIntegerHelper<BigInteger>.LeadingZeroCount(NegativeOne));

            Assert.Equal((BigInteger)0, BinaryIntegerHelper<BigInteger>.LeadingZeroCount(Int64MaxValuePlusOne));
            Assert.Equal((BigInteger)0, BinaryIntegerHelper<BigInteger>.LeadingZeroCount(UInt64MaxValue));
        }

        [Fact]
        public static void PopCountTest()
        {
            Assert.Equal((BigInteger)0, BinaryIntegerHelper<BigInteger>.PopCount(Zero));
            Assert.Equal((BigInteger)1, BinaryIntegerHelper<BigInteger>.PopCount(One));
            Assert.Equal((BigInteger)63, BinaryIntegerHelper<BigInteger>.PopCount(Int64MaxValue));

            Assert.Equal((BigInteger)1, BinaryIntegerHelper<BigInteger>.PopCount(Int64MinValue));
            Assert.Equal((BigInteger)32, BinaryIntegerHelper<BigInteger>.PopCount(NegativeOne));

            Assert.Equal((BigInteger)1, BinaryIntegerHelper<BigInteger>.PopCount(Int64MaxValuePlusOne));
            Assert.Equal((BigInteger)64, BinaryIntegerHelper<BigInteger>.PopCount(UInt64MaxValue));
        }

        [Fact]
        public static void RotateLeftTest()
        {
            Assert.Equal((BigInteger)0x00000000, BinaryIntegerHelper<BigInteger>.RotateLeft(Zero, 1));
            Assert.Equal((BigInteger)0x00000000, BinaryIntegerHelper<BigInteger>.RotateLeft(Zero, 32));
            Assert.Equal((BigInteger)0x00000000, BinaryIntegerHelper<BigInteger>.RotateLeft(Zero, 33));

            Assert.Equal((BigInteger)0x00000002, BinaryIntegerHelper<BigInteger>.RotateLeft(One, 1));
            Assert.Equal((BigInteger)0x00000001, BinaryIntegerHelper<BigInteger>.RotateLeft(One, 32));
            Assert.Equal((BigInteger)0x00000002, BinaryIntegerHelper<BigInteger>.RotateLeft(One, 33));

            Assert.Equal((BigInteger)0xFFFFFFFFFFFFFFFE, BinaryIntegerHelper<BigInteger>.RotateLeft(Int64MaxValue, 1));
            Assert.Equal((BigInteger)0xFFFFFFFF7FFFFFFF, BinaryIntegerHelper<BigInteger>.RotateLeft(Int64MaxValue, 32));
            Assert.Equal((BigInteger)0xFFFFFFFEFFFFFFFF, BinaryIntegerHelper<BigInteger>.RotateLeft(Int64MaxValue, 33));

            Assert.Equal((BigInteger)0x0000000000000001, BinaryIntegerHelper<BigInteger>.RotateLeft(Int64MinValue, 1));
            Assert.Equal((BigInteger)0x0000000080000000, BinaryIntegerHelper<BigInteger>.RotateLeft(Int64MinValue, 32));
            Assert.Equal((BigInteger)0x0000000100000000, BinaryIntegerHelper<BigInteger>.RotateLeft(Int64MinValue, 33));

            Assert.Equal(unchecked((BigInteger)(int)0xFFFFFFFF), BinaryIntegerHelper<BigInteger>.RotateLeft(NegativeOne, 1));
            Assert.Equal(unchecked((BigInteger)(int)0xFFFFFFFF), BinaryIntegerHelper<BigInteger>.RotateLeft(NegativeOne, 32));
            Assert.Equal(unchecked((BigInteger)(int)0xFFFFFFFF), BinaryIntegerHelper<BigInteger>.RotateLeft(NegativeOne, 33));

            Assert.Equal((BigInteger)0x0000000000000001, BinaryIntegerHelper<BigInteger>.RotateLeft(Int64MaxValuePlusOne, 1));
            Assert.Equal((BigInteger)0x0000000080000000, BinaryIntegerHelper<BigInteger>.RotateLeft(Int64MaxValuePlusOne, 32));
            Assert.Equal((BigInteger)0x0000000100000000, BinaryIntegerHelper<BigInteger>.RotateLeft(Int64MaxValuePlusOne, 33));

            Assert.Equal((BigInteger)0xFFFFFFFFFFFFFFFF, BinaryIntegerHelper<BigInteger>.RotateLeft(UInt64MaxValue, 1));
            Assert.Equal((BigInteger)0xFFFFFFFFFFFFFFFF, BinaryIntegerHelper<BigInteger>.RotateLeft(UInt64MaxValue, 32));
            Assert.Equal((BigInteger)0xFFFFFFFFFFFFFFFF, BinaryIntegerHelper<BigInteger>.RotateLeft(UInt64MaxValue, 33));

            Assert.Equal((BigInteger)0x00000000, BinaryIntegerHelper<BigInteger>.RotateLeft(Zero, -1));
            Assert.Equal((BigInteger)0x00000000, BinaryIntegerHelper<BigInteger>.RotateLeft(Zero, -32));
            Assert.Equal((BigInteger)0x00000000, BinaryIntegerHelper<BigInteger>.RotateLeft(Zero, -33));

            Assert.Equal((BigInteger)0x80000000, BinaryIntegerHelper<BigInteger>.RotateLeft(One, -1));
            Assert.Equal((BigInteger)0x00000001, BinaryIntegerHelper<BigInteger>.RotateLeft(One, -32));
            Assert.Equal((BigInteger)0x80000000, BinaryIntegerHelper<BigInteger>.RotateLeft(One, -33));

            Assert.Equal((BigInteger)0xBFFFFFFFFFFFFFFF, BinaryIntegerHelper<BigInteger>.RotateLeft(Int64MaxValue, -1));
            Assert.Equal((BigInteger)0xFFFFFFFF7FFFFFFF, BinaryIntegerHelper<BigInteger>.RotateLeft(Int64MaxValue, -32));
            Assert.Equal((BigInteger)0xFFFFFFFFBFFFFFFF, BinaryIntegerHelper<BigInteger>.RotateLeft(Int64MaxValue, -33));

            Assert.Equal((BigInteger)0x4000000000000000, BinaryIntegerHelper<BigInteger>.RotateLeft(Int64MinValue, -1));
            Assert.Equal((BigInteger)0x0000000080000000, BinaryIntegerHelper<BigInteger>.RotateLeft(Int64MinValue, -32));
            Assert.Equal((BigInteger)0x0000000040000000, BinaryIntegerHelper<BigInteger>.RotateLeft(Int64MinValue, -33));

            Assert.Equal(unchecked((BigInteger)(int)0xFFFFFFFF), BinaryIntegerHelper<BigInteger>.RotateLeft(NegativeOne, -1));
            Assert.Equal(unchecked((BigInteger)(int)0xFFFFFFFF), BinaryIntegerHelper<BigInteger>.RotateLeft(NegativeOne, -32));
            Assert.Equal(unchecked((BigInteger)(int)0xFFFFFFFF), BinaryIntegerHelper<BigInteger>.RotateLeft(NegativeOne, -33));

            Assert.Equal((BigInteger)0x4000000000000000, BinaryIntegerHelper<BigInteger>.RotateLeft(Int64MaxValuePlusOne, -1));
            Assert.Equal((BigInteger)0x0000000080000000, BinaryIntegerHelper<BigInteger>.RotateLeft(Int64MaxValuePlusOne, -32));
            Assert.Equal((BigInteger)0x0000000040000000, BinaryIntegerHelper<BigInteger>.RotateLeft(Int64MaxValuePlusOne, -33));

            Assert.Equal((BigInteger)0xFFFFFFFFFFFFFFFF, BinaryIntegerHelper<BigInteger>.RotateLeft(UInt64MaxValue, -1));
            Assert.Equal((BigInteger)0xFFFFFFFFFFFFFFFF, BinaryIntegerHelper<BigInteger>.RotateLeft(UInt64MaxValue, -32));
            Assert.Equal((BigInteger)0xFFFFFFFFFFFFFFFF, BinaryIntegerHelper<BigInteger>.RotateLeft(UInt64MaxValue, -33));
        }

        [Fact]
        public static void RotateRightTest()
        {
            Assert.Equal((BigInteger)0x00000000, BinaryIntegerHelper<BigInteger>.RotateRight(Zero, 1));
            Assert.Equal((BigInteger)0x00000000, BinaryIntegerHelper<BigInteger>.RotateRight(Zero, 32));
            Assert.Equal((BigInteger)0x00000000, BinaryIntegerHelper<BigInteger>.RotateRight(Zero, 33));

            Assert.Equal((BigInteger)0x80000000, BinaryIntegerHelper<BigInteger>.RotateRight(One, 1));
            Assert.Equal((BigInteger)0x00000001, BinaryIntegerHelper<BigInteger>.RotateRight(One, 32));
            Assert.Equal((BigInteger)0x80000000, BinaryIntegerHelper<BigInteger>.RotateRight(One, 33));

            Assert.Equal((BigInteger)0xBFFFFFFFFFFFFFFF, BinaryIntegerHelper<BigInteger>.RotateRight(Int64MaxValue, 1));
            Assert.Equal((BigInteger)0xFFFFFFFF7FFFFFFF, BinaryIntegerHelper<BigInteger>.RotateRight(Int64MaxValue, 32));
            Assert.Equal((BigInteger)0xFFFFFFFFBFFFFFFF, BinaryIntegerHelper<BigInteger>.RotateRight(Int64MaxValue, 33));

            Assert.Equal((BigInteger)0x4000000000000000, BinaryIntegerHelper<BigInteger>.RotateRight(Int64MinValue, 1));
            Assert.Equal((BigInteger)0x0000000080000000, BinaryIntegerHelper<BigInteger>.RotateRight(Int64MinValue, 32));
            Assert.Equal((BigInteger)0x0000000040000000, BinaryIntegerHelper<BigInteger>.RotateRight(Int64MinValue, 33));

            Assert.Equal(unchecked((BigInteger)(int)0xFFFFFFFF), BinaryIntegerHelper<BigInteger>.RotateRight(NegativeOne, 1));
            Assert.Equal(unchecked((BigInteger)(int)0xFFFFFFFF), BinaryIntegerHelper<BigInteger>.RotateRight(NegativeOne, 32));
            Assert.Equal(unchecked((BigInteger)(int)0xFFFFFFFF), BinaryIntegerHelper<BigInteger>.RotateRight(NegativeOne, 33));

            Assert.Equal((BigInteger)0x4000000000000000, BinaryIntegerHelper<BigInteger>.RotateRight(Int64MaxValuePlusOne, 1));
            Assert.Equal((BigInteger)0x0000000080000000, BinaryIntegerHelper<BigInteger>.RotateRight(Int64MaxValuePlusOne, 32));
            Assert.Equal((BigInteger)0x0000000040000000, BinaryIntegerHelper<BigInteger>.RotateRight(Int64MaxValuePlusOne, 33));

            Assert.Equal((BigInteger)0xFFFFFFFFFFFFFFFF, BinaryIntegerHelper<BigInteger>.RotateRight(UInt64MaxValue, 1));
            Assert.Equal((BigInteger)0xFFFFFFFFFFFFFFFF, BinaryIntegerHelper<BigInteger>.RotateRight(UInt64MaxValue, 32));
            Assert.Equal((BigInteger)0xFFFFFFFFFFFFFFFF, BinaryIntegerHelper<BigInteger>.RotateRight(UInt64MaxValue, 33));

            Assert.Equal((BigInteger)0x00000000, BinaryIntegerHelper<BigInteger>.RotateRight(Zero, -1));
            Assert.Equal((BigInteger)0x00000000, BinaryIntegerHelper<BigInteger>.RotateRight(Zero, -32));
            Assert.Equal((BigInteger)0x00000000, BinaryIntegerHelper<BigInteger>.RotateRight(Zero, -33));

            Assert.Equal((BigInteger)0x00000002, BinaryIntegerHelper<BigInteger>.RotateRight(One, -1));
            Assert.Equal((BigInteger)0x00000001, BinaryIntegerHelper<BigInteger>.RotateRight(One, -32));
            Assert.Equal((BigInteger)0x00000002, BinaryIntegerHelper<BigInteger>.RotateRight(One, -33));

            Assert.Equal((BigInteger)0xFFFFFFFFFFFFFFFE, BinaryIntegerHelper<BigInteger>.RotateRight(Int64MaxValue, -1));
            Assert.Equal((BigInteger)0xFFFFFFFF7FFFFFFF, BinaryIntegerHelper<BigInteger>.RotateRight(Int64MaxValue, -32));
            Assert.Equal((BigInteger)0xFFFFFFFEFFFFFFFF, BinaryIntegerHelper<BigInteger>.RotateRight(Int64MaxValue, -33));

            Assert.Equal((BigInteger)0x0000000000000001, BinaryIntegerHelper<BigInteger>.RotateRight(Int64MinValue, -1));
            Assert.Equal((BigInteger)0x0000000080000000, BinaryIntegerHelper<BigInteger>.RotateRight(Int64MinValue, -32));
            Assert.Equal((BigInteger)0x0000000100000000, BinaryIntegerHelper<BigInteger>.RotateRight(Int64MinValue, -33));

            Assert.Equal(unchecked((BigInteger)(int)0xFFFFFFFF), BinaryIntegerHelper<BigInteger>.RotateRight(NegativeOne, -1));
            Assert.Equal(unchecked((BigInteger)(int)0xFFFFFFFF), BinaryIntegerHelper<BigInteger>.RotateRight(NegativeOne, -32));
            Assert.Equal(unchecked((BigInteger)(int)0xFFFFFFFF), BinaryIntegerHelper<BigInteger>.RotateRight(NegativeOne, -33));

            Assert.Equal((BigInteger)0x0000000000000001, BinaryIntegerHelper<BigInteger>.RotateRight(Int64MaxValuePlusOne, -1));
            Assert.Equal((BigInteger)0x0000000080000000, BinaryIntegerHelper<BigInteger>.RotateRight(Int64MaxValuePlusOne, -32));
            Assert.Equal((BigInteger)0x0000000100000000, BinaryIntegerHelper<BigInteger>.RotateRight(Int64MaxValuePlusOne, -33));

            Assert.Equal((BigInteger)0xFFFFFFFFFFFFFFFF, BinaryIntegerHelper<BigInteger>.RotateRight(UInt64MaxValue, -1));
            Assert.Equal((BigInteger)0xFFFFFFFFFFFFFFFF, BinaryIntegerHelper<BigInteger>.RotateRight(UInt64MaxValue, -32));
            Assert.Equal((BigInteger)0xFFFFFFFFFFFFFFFF, BinaryIntegerHelper<BigInteger>.RotateRight(UInt64MaxValue, -33));
        }

        [Fact]
        public static void TrailingZeroCountTest()
        {
            Assert.Equal((BigInteger)32, BinaryIntegerHelper<BigInteger>.TrailingZeroCount(Zero));
            Assert.Equal((BigInteger)0, BinaryIntegerHelper<BigInteger>.TrailingZeroCount(One));
            Assert.Equal((BigInteger)0, BinaryIntegerHelper<BigInteger>.TrailingZeroCount(Int64MaxValue));

            Assert.Equal((BigInteger)63, BinaryIntegerHelper<BigInteger>.TrailingZeroCount(Int64MinValue));
            Assert.Equal((BigInteger)0, BinaryIntegerHelper<BigInteger>.TrailingZeroCount(NegativeOne));

            Assert.Equal((BigInteger)63, BinaryIntegerHelper<BigInteger>.TrailingZeroCount(Int64MaxValuePlusOne));
            Assert.Equal((BigInteger)0, BinaryIntegerHelper<BigInteger>.TrailingZeroCount(UInt64MaxValue));
        }

        [Fact]
        public static void GetByteCountTest()
        {
            Assert.Equal(4, BinaryIntegerHelper<BigInteger>.GetByteCount(Zero));
            Assert.Equal(4, BinaryIntegerHelper<BigInteger>.GetByteCount(One));
            Assert.Equal(8, BinaryIntegerHelper<BigInteger>.GetByteCount(Int64MaxValue));

            Assert.Equal(8, BinaryIntegerHelper<BigInteger>.GetByteCount(Int64MinValue));
            Assert.Equal(4, BinaryIntegerHelper<BigInteger>.GetByteCount(NegativeOne));

            Assert.Equal(8, BinaryIntegerHelper<BigInteger>.GetByteCount(Int64MaxValuePlusOne));
            Assert.Equal(8, BinaryIntegerHelper<BigInteger>.GetByteCount(UInt64MaxValue));

            Assert.Equal(16, BinaryIntegerHelper<BigInteger>.GetByteCount(Int128MaxValue));
            Assert.Equal(16, BinaryIntegerHelper<BigInteger>.GetByteCount(Int128MinValue));
            Assert.Equal(20, BinaryIntegerHelper<BigInteger>.GetByteCount(Int128MinValueMinusOne));
            Assert.Equal(16, BinaryIntegerHelper<BigInteger>.GetByteCount(Int128MinValuePlusOne));
            Assert.Equal(20, BinaryIntegerHelper<BigInteger>.GetByteCount(Int128MinValueTimesTwo));
            Assert.Equal(16, BinaryIntegerHelper<BigInteger>.GetByteCount(Int128MaxValuePlusOne));
            Assert.Equal(16, BinaryIntegerHelper<BigInteger>.GetByteCount(UInt128MaxValue));
        }

        [Fact]
        public static void GetShortestBitLengthTest()
        {
            Assert.Equal(0x00, BinaryIntegerHelper<BigInteger>.GetShortestBitLength(Zero));
            Assert.Equal(0x01, BinaryIntegerHelper<BigInteger>.GetShortestBitLength(One));
            Assert.Equal(0x3F, BinaryIntegerHelper<BigInteger>.GetShortestBitLength(Int64MaxValue));

            Assert.Equal(0x40, BinaryIntegerHelper<BigInteger>.GetShortestBitLength(Int64MinValue));
            Assert.Equal(0x01, BinaryIntegerHelper<BigInteger>.GetShortestBitLength(NegativeOne));

            Assert.Equal(0x40, BinaryIntegerHelper<BigInteger>.GetShortestBitLength(Int64MaxValuePlusOne));
            Assert.Equal(0x40, BinaryIntegerHelper<BigInteger>.GetShortestBitLength(UInt64MaxValue));

            Assert.Equal(0x7F, BinaryIntegerHelper<BigInteger>.GetShortestBitLength(Int128MaxValue));
            Assert.Equal(0x80, BinaryIntegerHelper<BigInteger>.GetShortestBitLength(Int128MinValue));
            Assert.Equal(0x81, BinaryIntegerHelper<BigInteger>.GetShortestBitLength(Int128MinValueMinusOne));
            Assert.Equal(0x80, BinaryIntegerHelper<BigInteger>.GetShortestBitLength(Int128MinValuePlusOne));
            Assert.Equal(0x81, BinaryIntegerHelper<BigInteger>.GetShortestBitLength(Int128MinValueTimesTwo));
            Assert.Equal(0x80, BinaryIntegerHelper<BigInteger>.GetShortestBitLength(Int128MaxValuePlusOne));
            Assert.Equal(0x80, BinaryIntegerHelper<BigInteger>.GetShortestBitLength(UInt128MaxValue));
        }

        [Fact]
        public static void TryWriteBigEndianTest()
        {
            Span<byte> destination = stackalloc byte[20];
            int bytesWritten = 0;

            Assert.True(BinaryIntegerHelper<BigInteger>.TryWriteBigEndian(Zero, destination, out bytesWritten));
            Assert.Equal(4, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00 }, destination.Slice(0, 4).ToArray());

            Assert.True(BinaryIntegerHelper<BigInteger>.TryWriteBigEndian(One, destination, out bytesWritten));
            Assert.Equal(4, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x01 }, destination.Slice(0, 4).ToArray());

            Assert.True(BinaryIntegerHelper<BigInteger>.TryWriteBigEndian(Int64MaxValue, destination, out bytesWritten));
            Assert.Equal(8, bytesWritten);
            Assert.Equal(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, destination.Slice(0, 8).ToArray());

            Assert.True(BinaryIntegerHelper<BigInteger>.TryWriteBigEndian(Int64MinValue, destination, out bytesWritten));
            Assert.Equal(8, bytesWritten);
            Assert.Equal(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, destination.Slice(0, 8).ToArray());

            Assert.True(BinaryIntegerHelper<BigInteger>.TryWriteBigEndian(NegativeOne, destination, out bytesWritten));
            Assert.Equal(4, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, destination.Slice(0, 4).ToArray());

            Assert.True(BinaryIntegerHelper<BigInteger>.TryWriteBigEndian(Int64MaxValuePlusOne, destination, out bytesWritten));
            Assert.Equal(8, bytesWritten);
            Assert.Equal(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, destination.Slice(0, 8).ToArray());

            Assert.True(BinaryIntegerHelper<BigInteger>.TryWriteBigEndian(UInt64MaxValue, destination, out bytesWritten));
            Assert.Equal(8, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, destination.Slice(0, 8).ToArray());

            Assert.True(BinaryIntegerHelper<BigInteger>.TryWriteBigEndian(Int128MaxValue, destination, out bytesWritten));
            Assert.Equal(16, bytesWritten);
            Assert.Equal(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, destination.Slice(0, 16).ToArray());

            Assert.True(BinaryIntegerHelper<BigInteger>.TryWriteBigEndian(Int128MinValue, destination, out bytesWritten));
            Assert.Equal(16, bytesWritten);
            Assert.Equal(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, destination.Slice(0, 16).ToArray());

            Assert.True(BinaryIntegerHelper<BigInteger>.TryWriteBigEndian(Int128MinValueMinusOne, destination, out bytesWritten));
            Assert.Equal(20, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, destination.Slice(0, 20).ToArray());

            Assert.True(BinaryIntegerHelper<BigInteger>.TryWriteBigEndian(Int128MinValuePlusOne, destination, out bytesWritten));
            Assert.Equal(16, bytesWritten);
            Assert.Equal(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, destination.Slice(0, 16).ToArray());

            Assert.True(BinaryIntegerHelper<BigInteger>.TryWriteBigEndian(Int128MinValueTimesTwo, destination, out bytesWritten));
            Assert.Equal(20, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, destination.Slice(0, 20).ToArray());

            Assert.True(BinaryIntegerHelper<BigInteger>.TryWriteBigEndian(Int128MaxValuePlusOne, destination, out bytesWritten));
            Assert.Equal(16, bytesWritten);
            Assert.Equal(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, destination.Slice(0, 16).ToArray());

            Assert.True(BinaryIntegerHelper<BigInteger>.TryWriteBigEndian(UInt128MaxValue, destination, out bytesWritten));
            Assert.Equal(16, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, destination.Slice(0, 16).ToArray());

            Assert.False(BinaryIntegerHelper<BigInteger>.TryWriteBigEndian(default, Span<byte>.Empty, out bytesWritten));
            Assert.Equal(0, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());
        }

        [Fact]
        public static void TryWriteLittleEndianTest()
        {
            Span<byte> destination = stackalloc byte[20];
            int bytesWritten = 0;

            Assert.True(BinaryIntegerHelper<BigInteger>.TryWriteLittleEndian(Zero, destination, out bytesWritten));
            Assert.Equal(4, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00 }, destination.Slice(0, 4).ToArray());

            Assert.True(BinaryIntegerHelper<BigInteger>.TryWriteLittleEndian(One, destination, out bytesWritten));
            Assert.Equal(4, bytesWritten);
            Assert.Equal(new byte[] { 0x01, 0x00, 0x00, 0x00 }, destination.Slice(0, 4).ToArray());

            Assert.True(BinaryIntegerHelper<BigInteger>.TryWriteLittleEndian(Int64MaxValue, destination, out bytesWritten));
            Assert.Equal(8, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, destination.Slice(0, 8).ToArray());

            Assert.True(BinaryIntegerHelper<BigInteger>.TryWriteLittleEndian(Int64MinValue, destination, out bytesWritten));
            Assert.Equal(8, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, destination.Slice(0, 8).ToArray());

            Assert.True(BinaryIntegerHelper<BigInteger>.TryWriteLittleEndian(NegativeOne, destination, out bytesWritten));
            Assert.Equal(4, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, destination.Slice(0, 4).ToArray());

            Assert.True(BinaryIntegerHelper<BigInteger>.TryWriteLittleEndian(Int64MaxValuePlusOne, destination, out bytesWritten));
            Assert.Equal(8, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, destination.Slice(0, 8).ToArray());

            Assert.True(BinaryIntegerHelper<BigInteger>.TryWriteLittleEndian(UInt64MaxValue, destination, out bytesWritten));
            Assert.Equal(8, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, destination.Slice(0, 8).ToArray());

            Assert.True(BinaryIntegerHelper<BigInteger>.TryWriteLittleEndian(Int128MaxValue, destination, out bytesWritten));
            Assert.Equal(16, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, destination.Slice(0, 16).ToArray());

            Assert.True(BinaryIntegerHelper<BigInteger>.TryWriteLittleEndian(Int128MinValue, destination, out bytesWritten));
            Assert.Equal(16, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, destination.Slice(0, 16).ToArray());

            Assert.True(BinaryIntegerHelper<BigInteger>.TryWriteLittleEndian(Int128MinValueMinusOne, destination, out bytesWritten));
            Assert.Equal(20, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F, 0xFF, 0xFF, 0xFF, 0xFF }, destination.Slice(0, 20).ToArray());

            Assert.True(BinaryIntegerHelper<BigInteger>.TryWriteLittleEndian(Int128MinValuePlusOne, destination, out bytesWritten));
            Assert.Equal(16, bytesWritten);
            Assert.Equal(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, destination.Slice(0, 16).ToArray());

            Assert.True(BinaryIntegerHelper<BigInteger>.TryWriteLittleEndian(Int128MinValueTimesTwo, destination, out bytesWritten));
            Assert.Equal(20, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF }, destination.Slice(0, 20).ToArray());

            Assert.True(BinaryIntegerHelper<BigInteger>.TryWriteLittleEndian(Int128MaxValuePlusOne, destination, out bytesWritten));
            Assert.Equal(16, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, destination.Slice(0, 16).ToArray());

            Assert.True(BinaryIntegerHelper<BigInteger>.TryWriteLittleEndian(UInt128MaxValue, destination, out bytesWritten));
            Assert.Equal(16, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, destination.Slice(0, 16).ToArray());

            Assert.False(BinaryIntegerHelper<BigInteger>.TryWriteLittleEndian(default, Span<byte>.Empty, out bytesWritten));
            Assert.Equal(0, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, destination.ToArray());
        }

        //
        // IBinaryNumber
        //

        [Fact]
        public static void IsPow2Test()
        {
            Assert.False(BinaryNumberHelper<BigInteger>.IsPow2(Zero));
            Assert.True(BinaryNumberHelper<BigInteger>.IsPow2(One));
            Assert.False(BinaryNumberHelper<BigInteger>.IsPow2(Int64MaxValue));

            Assert.False(BinaryNumberHelper<BigInteger>.IsPow2(Int64MinValue));
            Assert.False(BinaryNumberHelper<BigInteger>.IsPow2(NegativeOne));

            Assert.True(BinaryNumberHelper<BigInteger>.IsPow2(Int64MaxValuePlusOne));
            Assert.False(BinaryNumberHelper<BigInteger>.IsPow2(UInt64MaxValue));
        }

        [Fact]
        public static void Log2Test()
        {
            Assert.Equal((BigInteger)0, BinaryNumberHelper<BigInteger>.Log2(Zero));
            Assert.Equal((BigInteger)0, BinaryNumberHelper<BigInteger>.Log2(One));
            Assert.Equal((BigInteger)62, BinaryNumberHelper<BigInteger>.Log2(Int64MaxValue));

            Assert.Throws<ArgumentOutOfRangeException>(() => BinaryNumberHelper<BigInteger>.Log2(Int64MinValue));
            Assert.Throws<ArgumentOutOfRangeException>(() => BinaryNumberHelper<BigInteger>.Log2(NegativeOne));

            Assert.Equal((BigInteger)63, BinaryNumberHelper<BigInteger>.Log2(Int64MaxValuePlusOne));
            Assert.Equal((BigInteger)63, BinaryNumberHelper<BigInteger>.Log2(UInt64MaxValue));
        }

        //
        // IBitwiseOperators
        //

        [Fact]
        public static void op_BitwiseAndTest()
        {
            Assert.Equal((BigInteger)0x00000000, BitwiseOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_BitwiseAnd(Zero, 1));
            Assert.Equal((BigInteger)0x00000001, BitwiseOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_BitwiseAnd(One, 1));
            Assert.Equal((BigInteger)0x00000001, BitwiseOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_BitwiseAnd(Int64MaxValue, 1));

            Assert.Equal((BigInteger)0x00000000, BitwiseOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_BitwiseAnd(Int64MinValue, 1));
            Assert.Equal((BigInteger)0x00000001, BitwiseOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_BitwiseAnd(NegativeOne, 1));

            Assert.Equal((BigInteger)0x00000000, BitwiseOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_BitwiseAnd(Int64MaxValuePlusOne, 1));
            Assert.Equal((BigInteger)0x00000001, BitwiseOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_BitwiseAnd(UInt64MaxValue, 1));
        }

        [Fact]
        public static void op_BitwiseOrTest()
        {
            Assert.Equal((BigInteger)0x00000001, BitwiseOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_BitwiseOr(Zero, 1));
            Assert.Equal((BigInteger)0x00000001, BitwiseOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_BitwiseOr(One, 1));
            Assert.Equal((BigInteger)0x7FFFFFFFFFFFFFFF, BitwiseOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_BitwiseOr(Int64MaxValue, 1));

            Assert.Equal(unchecked((BigInteger)(long)0x8000000000000001), BitwiseOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_BitwiseOr(Int64MinValue, 1));
            Assert.Equal(unchecked((BigInteger)(int)0xFFFFFFFF), BitwiseOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_BitwiseOr(NegativeOne, 1));

            Assert.Equal((BigInteger)0x8000000000000001, BitwiseOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_BitwiseOr(Int64MaxValuePlusOne, 1));
            Assert.Equal((BigInteger)0xFFFFFFFFFFFFFFFF, BitwiseOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_BitwiseOr(UInt64MaxValue, 1));
        }

        [Fact]
        public static void op_ExclusiveOrTest()
        {
            Assert.Equal((BigInteger)0x00000001, BitwiseOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_ExclusiveOr(Zero, 1));
            Assert.Equal((BigInteger)0x00000000, BitwiseOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_ExclusiveOr(One, 1));
            Assert.Equal((BigInteger)0x7FFFFFFFFFFFFFFE, BitwiseOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_ExclusiveOr(Int64MaxValue, 1));

            Assert.Equal(unchecked((BigInteger)(long)0x8000000000000001), BitwiseOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_ExclusiveOr(Int64MinValue, 1));
            Assert.Equal(unchecked((BigInteger)(int)0xFFFFFFFE), BitwiseOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_ExclusiveOr(NegativeOne, 1));

            Assert.Equal((BigInteger)0x8000000000000001, BitwiseOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_ExclusiveOr(Int64MaxValuePlusOne, 1));
            Assert.Equal((BigInteger)0xFFFFFFFFFFFFFFFE, BitwiseOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_ExclusiveOr(UInt64MaxValue, 1));
        }

        [Fact]
        public static void op_OnesComplementTest()
        {
            Assert.Equal(unchecked((BigInteger)(int)0xFFFFFFFF), BitwiseOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_OnesComplement(Zero));
            Assert.Equal(unchecked((BigInteger)(int)0xFFFFFFFE), BitwiseOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_OnesComplement(One));
            Assert.Equal(unchecked((BigInteger)(long)0x8000000000000000), BitwiseOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_OnesComplement(Int64MaxValue));

            Assert.Equal((BigInteger)0x7FFFFFFFFFFFFFFF, BitwiseOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_OnesComplement(Int64MinValue));
            Assert.Equal((BigInteger)0x00000000, BitwiseOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_OnesComplement(NegativeOne));

            Assert.Equal(Int64MinValueMinusOne, BitwiseOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_OnesComplement(Int64MaxValuePlusOne));

            Assert.Equal(NegativeTwoPow64, BitwiseOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_OnesComplement(UInt64MaxValue));
        }

        //
        // IComparisonOperators
        //

        [Fact]
        public static void op_GreaterThanTest()
        {
            Assert.False(ComparisonOperatorsHelper<BigInteger, BigInteger>.op_GreaterThan(Zero, 1));
            Assert.False(ComparisonOperatorsHelper<BigInteger, BigInteger>.op_GreaterThan(One, 1));
            Assert.True(ComparisonOperatorsHelper<BigInteger, BigInteger>.op_GreaterThan(Int64MaxValue, 1));

            Assert.False(ComparisonOperatorsHelper<BigInteger, BigInteger>.op_GreaterThan(Int64MinValue, 1));
            Assert.False(ComparisonOperatorsHelper<BigInteger, BigInteger>.op_GreaterThan(NegativeOne, 1));

            Assert.True(ComparisonOperatorsHelper<BigInteger, BigInteger>.op_GreaterThan(Int64MaxValuePlusOne, 1));
            Assert.True(ComparisonOperatorsHelper<BigInteger, BigInteger>.op_GreaterThan(UInt64MaxValue, 1));
        }

        [Fact]
        public static void op_GreaterThanOrEqualTest()
        {
            Assert.False(ComparisonOperatorsHelper<BigInteger, BigInteger>.op_GreaterThanOrEqual(Zero, 1));
            Assert.True(ComparisonOperatorsHelper<BigInteger, BigInteger>.op_GreaterThanOrEqual(One, 1));
            Assert.True(ComparisonOperatorsHelper<BigInteger, BigInteger>.op_GreaterThanOrEqual(Int64MaxValue, 1));

            Assert.False(ComparisonOperatorsHelper<BigInteger, BigInteger>.op_GreaterThanOrEqual(Int64MinValue, 1));
            Assert.False(ComparisonOperatorsHelper<BigInteger, BigInteger>.op_GreaterThanOrEqual(NegativeOne, 1));

            Assert.True(ComparisonOperatorsHelper<BigInteger, BigInteger>.op_GreaterThanOrEqual(Int64MaxValuePlusOne, 1));
            Assert.True(ComparisonOperatorsHelper<BigInteger, BigInteger>.op_GreaterThanOrEqual(UInt64MaxValue, 1));
        }

        [Fact]
        public static void op_LessThanTest()
        {
            Assert.True(ComparisonOperatorsHelper<BigInteger, BigInteger>.op_LessThan(Zero, 1));
            Assert.False(ComparisonOperatorsHelper<BigInteger, BigInteger>.op_LessThan(One, 1));
            Assert.False(ComparisonOperatorsHelper<BigInteger, BigInteger>.op_LessThan(Int64MaxValue, 1));

            Assert.True(ComparisonOperatorsHelper<BigInteger, BigInteger>.op_LessThan(Int64MinValue, 1));
            Assert.True(ComparisonOperatorsHelper<BigInteger, BigInteger>.op_LessThan(NegativeOne, 1));

            Assert.False(ComparisonOperatorsHelper<BigInteger, BigInteger>.op_LessThan(Int64MaxValuePlusOne, 1));
            Assert.False(ComparisonOperatorsHelper<BigInteger, BigInteger>.op_LessThan(UInt64MaxValue, 1));
        }

        [Fact]
        public static void op_LessThanOrEqualTest()
        {
            Assert.True(ComparisonOperatorsHelper<BigInteger, BigInteger>.op_LessThanOrEqual(Zero, 1));
            Assert.True(ComparisonOperatorsHelper<BigInteger, BigInteger>.op_LessThanOrEqual(One, 1));
            Assert.False(ComparisonOperatorsHelper<BigInteger, BigInteger>.op_LessThanOrEqual(Int64MaxValue, 1));

            Assert.True(ComparisonOperatorsHelper<BigInteger, BigInteger>.op_LessThanOrEqual(Int64MinValue, 1));
            Assert.True(ComparisonOperatorsHelper<BigInteger, BigInteger>.op_LessThanOrEqual(NegativeOne, 1));

            Assert.False(ComparisonOperatorsHelper<BigInteger, BigInteger>.op_LessThanOrEqual(Int64MaxValuePlusOne, 1));
            Assert.False(ComparisonOperatorsHelper<BigInteger, BigInteger>.op_LessThanOrEqual(UInt64MaxValue, 1));
        }

        //
        // IDecrementOperators
        //

        [Fact]
        public static void op_DecrementTest()
        {
            Assert.Equal(UInt64MaxValue, DecrementOperatorsHelper<BigInteger>.op_Decrement(TwoPow64));

            Assert.Equal(NegativeOne, DecrementOperatorsHelper<BigInteger>.op_Decrement(Zero));
            Assert.Equal(Zero, DecrementOperatorsHelper<BigInteger>.op_Decrement(One));
            Assert.Equal(Int64MaxValueMinusOne, DecrementOperatorsHelper<BigInteger>.op_Decrement(Int64MaxValue));

            Assert.Equal(Int64MinValueMinusOne, DecrementOperatorsHelper<BigInteger>.op_Decrement(Int64MinValue));
            Assert.Equal(NegativeTwo, DecrementOperatorsHelper<BigInteger>.op_Decrement(NegativeOne));

            Assert.Equal(Int64MaxValue, DecrementOperatorsHelper<BigInteger>.op_Decrement(Int64MaxValuePlusOne));
            Assert.Equal(UInt64MaxValueMinusOne, DecrementOperatorsHelper<BigInteger>.op_Decrement(UInt64MaxValue));
        }

        [Fact]
        public static void op_CheckedDecrementTest()
        {
            Assert.Equal(UInt64MaxValue, DecrementOperatorsHelper<BigInteger>.op_CheckedDecrement(TwoPow64));

            Assert.Equal(NegativeOne, DecrementOperatorsHelper<BigInteger>.op_CheckedDecrement(Zero));
            Assert.Equal(Zero, DecrementOperatorsHelper<BigInteger>.op_CheckedDecrement(One));
            Assert.Equal(Int64MaxValueMinusOne, DecrementOperatorsHelper<BigInteger>.op_CheckedDecrement(Int64MaxValue));

            Assert.Equal(Int64MinValueMinusOne, DecrementOperatorsHelper<BigInteger>.op_CheckedDecrement(Int64MinValue));
            Assert.Equal(NegativeTwo, DecrementOperatorsHelper<BigInteger>.op_CheckedDecrement(NegativeOne));

            Assert.Equal(Int64MaxValue, DecrementOperatorsHelper<BigInteger>.op_CheckedDecrement(Int64MaxValuePlusOne));
            Assert.Equal(UInt64MaxValueMinusOne, DecrementOperatorsHelper<BigInteger>.op_CheckedDecrement(UInt64MaxValue));
        }

        //
        // IDivisionOperators
        //

        [Fact]
        public static void op_DivisionTest()
        {
            Assert.Equal(Zero, DivisionOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_Division(Zero, 2));
            Assert.Equal(Zero, DivisionOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_Division(One, 2));
            Assert.Equal((BigInteger)0x3FFFFFFFFFFFFFFF, DivisionOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_Division(Int64MaxValue, 2));

            Assert.Equal(unchecked((BigInteger)(long)0xC000000000000000), DivisionOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_Division(Int64MinValue, 2));
            Assert.Equal(Zero, DivisionOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_Division(NegativeOne, 2));

            Assert.Equal(unchecked((BigInteger)0x4000000000000000), DivisionOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_Division(Int64MaxValuePlusOne, 2));
            Assert.Equal(Int64MaxValue, DivisionOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_Division(UInt64MaxValue, 2));

            Assert.Throws<DivideByZeroException>(() => DivisionOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_Division(One, 0));
        }

        [Fact]
        public static void op_CheckedDivisionTest()
        {
            Assert.Equal(Zero, DivisionOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_CheckedDivision(Zero, 2));
            Assert.Equal(Zero, DivisionOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_CheckedDivision(One, 2));
            Assert.Equal((BigInteger)0x3FFFFFFFFFFFFFFF, DivisionOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_CheckedDivision(Int64MaxValue, 2));

            Assert.Equal(unchecked((BigInteger)(long)0xC000000000000000), DivisionOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_CheckedDivision(Int64MinValue, 2));
            Assert.Equal(Zero, DivisionOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_CheckedDivision(NegativeOne, 2));

            Assert.Equal(unchecked((BigInteger)0x4000000000000000), DivisionOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_CheckedDivision(Int64MaxValuePlusOne, 2));
            Assert.Equal(Int64MaxValue, DivisionOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_CheckedDivision(UInt64MaxValue, 2));

            Assert.Throws<DivideByZeroException>(() => DivisionOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_CheckedDivision(One, 0));
        }

        //
        // IEqualityOperators
        //

        [Fact]
        public static void op_EqualityTest()
        {
            Assert.False(EqualityOperatorsHelper<BigInteger, BigInteger>.op_Equality(Zero, 1));
            Assert.True(EqualityOperatorsHelper<BigInteger, BigInteger>.op_Equality(One, 1));
            Assert.False(EqualityOperatorsHelper<BigInteger, BigInteger>.op_Equality(Int64MaxValue, 1));

            Assert.False(EqualityOperatorsHelper<BigInteger, BigInteger>.op_Equality(Int64MinValue, 1));
            Assert.False(EqualityOperatorsHelper<BigInteger, BigInteger>.op_Equality(NegativeOne, 1));

            Assert.False(EqualityOperatorsHelper<BigInteger, BigInteger>.op_Equality(Int64MaxValuePlusOne, 1));
            Assert.False(EqualityOperatorsHelper<BigInteger, BigInteger>.op_Equality(UInt64MaxValue, 1));
        }

        [Fact]
        public static void op_InequalityTest()
        {
            Assert.True(EqualityOperatorsHelper<BigInteger, BigInteger>.op_Inequality(Zero, 1));
            Assert.False(EqualityOperatorsHelper<BigInteger, BigInteger>.op_Inequality(One, 1));
            Assert.True(EqualityOperatorsHelper<BigInteger, BigInteger>.op_Inequality(Int64MaxValue, 1));

            Assert.True(EqualityOperatorsHelper<BigInteger, BigInteger>.op_Inequality(Int64MinValue, 1));
            Assert.True(EqualityOperatorsHelper<BigInteger, BigInteger>.op_Inequality(NegativeOne, 1));

            Assert.True(EqualityOperatorsHelper<BigInteger, BigInteger>.op_Inequality(Int64MaxValuePlusOne, 1));
            Assert.True(EqualityOperatorsHelper<BigInteger, BigInteger>.op_Inequality(UInt64MaxValue, 1));
        }

        //
        // IIncrementOperators
        //

        [Fact]
        public static void op_IncrementTest()
        {
            Assert.Equal(One, IncrementOperatorsHelper<BigInteger>.op_Increment(Zero));
            Assert.Equal(Two, IncrementOperatorsHelper<BigInteger>.op_Increment(One));
            Assert.Equal(Int64MaxValuePlusOne, IncrementOperatorsHelper<BigInteger>.op_Increment(Int64MaxValue));

            Assert.Equal(Int64MinValuePlusOne, IncrementOperatorsHelper<BigInteger>.op_Increment(Int64MinValue));
            Assert.Equal(Zero, IncrementOperatorsHelper<BigInteger>.op_Increment(NegativeOne));

            Assert.Equal(Int64MaxValuePlusTwo, IncrementOperatorsHelper<BigInteger>.op_Increment(Int64MaxValuePlusOne));
            Assert.Equal(TwoPow64, IncrementOperatorsHelper<BigInteger>.op_Increment(UInt64MaxValue));
        }

        [Fact]
        public static void op_CheckedIncrementTest()
        {
            Assert.Equal(One, IncrementOperatorsHelper<BigInteger>.op_CheckedIncrement(Zero));
            Assert.Equal(Two, IncrementOperatorsHelper<BigInteger>.op_CheckedIncrement(One));
            Assert.Equal(Int64MaxValuePlusOne, IncrementOperatorsHelper<BigInteger>.op_CheckedIncrement(Int64MaxValue));

            Assert.Equal(Int64MinValuePlusOne, IncrementOperatorsHelper<BigInteger>.op_CheckedIncrement(Int64MinValue));
            Assert.Equal(Zero, IncrementOperatorsHelper<BigInteger>.op_CheckedIncrement(NegativeOne));

            Assert.Equal(Int64MaxValuePlusTwo, IncrementOperatorsHelper<BigInteger>.op_CheckedIncrement(Int64MaxValuePlusOne));
            Assert.Equal(TwoPow64, IncrementOperatorsHelper<BigInteger>.op_CheckedIncrement(UInt64MaxValue));
        }

        //
        // IModulusOperators
        //

        [Fact]
        public static void op_ModulusTest()
        {
            Assert.Equal(Zero, ModulusOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_Modulus(Zero, 2));
            Assert.Equal(One, ModulusOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_Modulus(One, 2));
            Assert.Equal(One, ModulusOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_Modulus(Int64MaxValue, 2));

            Assert.Equal(Zero, ModulusOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_Modulus(Int64MinValue, 2));
            Assert.Equal(NegativeOne, ModulusOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_Modulus(NegativeOne, 2));

            Assert.Equal(Zero, ModulusOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_Modulus(Int64MaxValuePlusOne, 2));
            Assert.Equal(One, ModulusOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_Modulus(UInt64MaxValue, 2));

            Assert.Throws<DivideByZeroException>(() => ModulusOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_Modulus(One, 0));
        }

        //
        // IMultiplicativeIdentity
        //

        [Fact]
        public static void MultiplicativeIdentityTest()
        {
            Assert.Equal(One, MultiplicativeIdentityHelper<BigInteger, BigInteger>.MultiplicativeIdentity);
        }

        //
        // IMultiplyOperators
        //

        [Fact]
        public static void op_MultiplyTest()
        {
            Assert.Equal(Zero, MultiplyOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_Multiply(Zero, 2));
            Assert.Equal(Two, MultiplyOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_Multiply(One, 2));
            Assert.Equal(unchecked((BigInteger)0xFFFFFFFFFFFFFFFE), MultiplyOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_Multiply(Int64MaxValue, 2));

            Assert.Equal(Int64MinValueTimesTwo, MultiplyOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_Multiply(Int64MinValue, 2));
            Assert.Equal(NegativeTwo, MultiplyOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_Multiply(NegativeOne, 2));

            Assert.Equal(TwoPow64, MultiplyOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_Multiply(Int64MaxValuePlusOne, 2));
            Assert.Equal(UInt64MaxValueTimesTwo, MultiplyOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_Multiply(UInt64MaxValue, 2));
        }

        [Fact]
        public static void op_CheckedMultiplyTest()
        {
            Assert.Equal(Zero, MultiplyOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_CheckedMultiply(Zero, 2));
            Assert.Equal(Two, MultiplyOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_CheckedMultiply(One, 2));
            Assert.Equal(unchecked((BigInteger)0xFFFFFFFFFFFFFFFE), MultiplyOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_CheckedMultiply(Int64MaxValue, 2));

            Assert.Equal(Int64MinValueTimesTwo, MultiplyOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_CheckedMultiply(Int64MinValue, 2));
            Assert.Equal(NegativeTwo, MultiplyOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_CheckedMultiply(NegativeOne, 2));

            Assert.Equal(TwoPow64, MultiplyOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_CheckedMultiply(Int64MaxValuePlusOne, 2));
            Assert.Equal(UInt64MaxValueTimesTwo, MultiplyOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_CheckedMultiply(UInt64MaxValue, 2));
        }

        //
        // INumber
        //

        [Fact]
        public static void ClampTest()
        {
            Assert.Equal((BigInteger)0x00, NumberHelper<BigInteger>.Clamp(Zero, unchecked((BigInteger)(int)0xFFFFFFC0), 0x003F));
            Assert.Equal((BigInteger)0x01, NumberHelper<BigInteger>.Clamp(One, unchecked((BigInteger)(int)0xFFFFFFC0), 0x003F));
            Assert.Equal((BigInteger)0x3F, NumberHelper<BigInteger>.Clamp(Int64MaxValue, unchecked((BigInteger)(int)0xFFFFFFC0), 0x003F));

            Assert.Equal(unchecked((BigInteger)(int)0xFFFFFFC0), NumberHelper<BigInteger>.Clamp(Int64MinValue, unchecked((BigInteger)(int)0xFFFFFFC0), 0x003F));
            Assert.Equal(NegativeOne, NumberHelper<BigInteger>.Clamp(NegativeOne, unchecked((BigInteger)(int)0xFFFFFFC0), 0x003F));

            Assert.Equal((BigInteger)0x3F, NumberHelper<BigInteger>.Clamp(Int64MaxValuePlusOne, unchecked((BigInteger)(int)0xFFFFFFC0), 0x003F));
            Assert.Equal((BigInteger)0x3F, NumberHelper<BigInteger>.Clamp(UInt64MaxValue, unchecked((BigInteger)(int)0xFFFFFFC0), 0x003F));
        }

        [Fact]
        public static void MaxTest()
        {
            Assert.Equal(One, NumberHelper<BigInteger>.Max(Zero, 1));
            Assert.Equal(One, NumberHelper<BigInteger>.Max(One, 1));
            Assert.Equal(Int64MaxValue, NumberHelper<BigInteger>.Max(Int64MaxValue, 1));

            Assert.Equal(One, NumberHelper<BigInteger>.Max(Int64MinValue, 1));
            Assert.Equal(One, NumberHelper<BigInteger>.Max(NegativeOne, 1));

            Assert.Equal(Int64MaxValuePlusOne, NumberHelper<BigInteger>.Max(Int64MaxValuePlusOne, 1));
            Assert.Equal(UInt64MaxValue, NumberHelper<BigInteger>.Max(UInt64MaxValue, 1));
        }

        [Fact]
        public static void MaxNumberTest()
        {
            Assert.Equal(One, NumberHelper<BigInteger>.MaxNumber(Zero, 1));
            Assert.Equal(One, NumberHelper<BigInteger>.MaxNumber(One, 1));
            Assert.Equal(Int64MaxValue, NumberHelper<BigInteger>.MaxNumber(Int64MaxValue, 1));

            Assert.Equal(One, NumberHelper<BigInteger>.MaxNumber(Int64MinValue, 1));
            Assert.Equal(One, NumberHelper<BigInteger>.MaxNumber(NegativeOne, 1));

            Assert.Equal(Int64MaxValuePlusOne, NumberHelper<BigInteger>.MaxNumber(Int64MaxValuePlusOne, 1));
            Assert.Equal(UInt64MaxValue, NumberHelper<BigInteger>.MaxNumber(UInt64MaxValue, 1));
        }

        [Fact]
        public static void MinTest()
        {
            Assert.Equal(Zero, NumberHelper<BigInteger>.Min(Zero, 1));
            Assert.Equal(One, NumberHelper<BigInteger>.Min(One, 1));
            Assert.Equal(One, NumberHelper<BigInteger>.Min(Int64MaxValue, 1));

            Assert.Equal(Int64MinValue, NumberHelper<BigInteger>.Min(Int64MinValue, 1));
            Assert.Equal(NegativeOne, NumberHelper<BigInteger>.Min(NegativeOne, 1));

            Assert.Equal(One, NumberHelper<BigInteger>.Min(Int64MaxValuePlusOne, 1));
            Assert.Equal(One, NumberHelper<BigInteger>.Min(UInt64MaxValue, 1));
        }

        [Fact]
        public static void MinNumberTest()
        {
            Assert.Equal(Zero, NumberHelper<BigInteger>.MinNumber(Zero, 1));
            Assert.Equal(One, NumberHelper<BigInteger>.MinNumber(One, 1));
            Assert.Equal(One, NumberHelper<BigInteger>.MinNumber(Int64MaxValue, 1));

            Assert.Equal(Int64MinValue, NumberHelper<BigInteger>.MinNumber(Int64MinValue, 1));
            Assert.Equal(NegativeOne, NumberHelper<BigInteger>.MinNumber(NegativeOne, 1));

            Assert.Equal(One, NumberHelper<BigInteger>.MinNumber(Int64MaxValuePlusOne, 1));
            Assert.Equal(One, NumberHelper<BigInteger>.MinNumber(UInt64MaxValue, 1));
        }

        [Fact]
        public static void SignTest()
        {
            Assert.Equal(0, NumberHelper<BigInteger>.Sign(Zero));
            Assert.Equal(1, NumberHelper<BigInteger>.Sign(One));
            Assert.Equal(1, NumberHelper<BigInteger>.Sign(Int64MaxValue));

            Assert.Equal(-1, NumberHelper<BigInteger>.Sign(Int64MinValue));
            Assert.Equal(-1, NumberHelper<BigInteger>.Sign(NegativeOne));

            Assert.Equal(1, NumberHelper<BigInteger>.Sign(Int64MaxValuePlusOne));
            Assert.Equal(1, NumberHelper<BigInteger>.Sign(UInt64MaxValue));
        }

        //
        // INumberBase
        //

        [Fact]
        public static void OneTest()
        {
            Assert.Equal(One, NumberBaseHelper<BigInteger>.One);
        }

        [Fact]
        public static void ZeroTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.Zero);
        }

        [Fact]
        public static void AbsTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.Abs(Zero));
            Assert.Equal(One, NumberBaseHelper<BigInteger>.Abs(One));
            Assert.Equal(Int64MaxValue, NumberBaseHelper<BigInteger>.Abs(Int64MaxValue));

            Assert.Equal(Int64MaxValuePlusOne, NumberBaseHelper<BigInteger>.Abs(Int64MinValue));
            Assert.Equal(One, NumberBaseHelper<BigInteger>.Abs(NegativeOne));

            Assert.Equal(Int64MaxValuePlusOne, NumberBaseHelper<BigInteger>.Abs(Int64MaxValuePlusOne));
            Assert.Equal(UInt64MaxValue, NumberBaseHelper<BigInteger>.Abs(UInt64MaxValue));
        }

        [Fact]
        public static void CreateCheckedFromByteTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateChecked<byte>(0x00));
            Assert.Equal(One, NumberBaseHelper<BigInteger>.CreateChecked<byte>(0x01));
            Assert.Equal(SByteMaxValue, NumberBaseHelper<BigInteger>.CreateChecked<byte>(0x7F));
            Assert.Equal(SByteMaxValuePlusOne, NumberBaseHelper<BigInteger>.CreateChecked<byte>(0x80));
            Assert.Equal(ByteMaxValue, NumberBaseHelper<BigInteger>.CreateChecked<byte>(0xFF));
        }

        [Fact]
        public static void CreateCheckedFromCharTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateChecked<char>((char)0x0000));
            Assert.Equal(One, NumberBaseHelper<BigInteger>.CreateChecked<char>((char)0x0001));
            Assert.Equal(Int16MaxValue, NumberBaseHelper<BigInteger>.CreateChecked<char>((char)0x7FFF));
            Assert.Equal(Int16MaxValuePlusOne, NumberBaseHelper<BigInteger>.CreateChecked<char>((char)0x8000));
            Assert.Equal(UInt16MaxValue, NumberBaseHelper<BigInteger>.CreateChecked<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateCheckedFromInt16Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateChecked<short>(0x0000));
            Assert.Equal(One, NumberBaseHelper<BigInteger>.CreateChecked<short>(0x0001));
            Assert.Equal(Int16MaxValue, NumberBaseHelper<BigInteger>.CreateChecked<short>(0x7FFF));
            Assert.Equal(Int16MinValue, NumberBaseHelper<BigInteger>.CreateChecked<short>(unchecked((short)0x8000)));
            Assert.Equal(NegativeOne, NumberBaseHelper<BigInteger>.CreateChecked<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromInt32Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateChecked<int>(0x00000000));
            Assert.Equal(One, NumberBaseHelper<BigInteger>.CreateChecked<int>(0x00000001));
            Assert.Equal(Int32MaxValue, NumberBaseHelper<BigInteger>.CreateChecked<int>(0x7FFFFFFF));
            Assert.Equal(Int32MinValue, NumberBaseHelper<BigInteger>.CreateChecked<int>(unchecked((int)0x80000000)));
            Assert.Equal(NegativeOne, NumberBaseHelper<BigInteger>.CreateChecked<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromInt64Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateChecked<long>(0x00000000));
            Assert.Equal(One, NumberBaseHelper<BigInteger>.CreateChecked<long>(0x00000001));
            Assert.Equal(Int64MaxValue, NumberBaseHelper<BigInteger>.CreateChecked<long>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal(Int64MinValue, NumberBaseHelper<BigInteger>.CreateChecked<long>(unchecked(unchecked((long)0x8000000000000000))));
            Assert.Equal(NegativeOne, NumberBaseHelper<BigInteger>.CreateChecked<long>(unchecked(unchecked((long)0xFFFFFFFFFFFFFFFF))));
        }

        [Fact]
        public static void CreateCheckedFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateChecked<nint>(unchecked((nint)0x00000000)));
                Assert.Equal(One, NumberBaseHelper<BigInteger>.CreateChecked<nint>(unchecked((nint)0x00000001)));
                Assert.Equal(Int64MaxValue, NumberBaseHelper<BigInteger>.CreateChecked<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(Int64MinValue, NumberBaseHelper<BigInteger>.CreateChecked<nint>(unchecked((nint)0x8000000000000000)));
                Assert.Equal(NegativeOne, NumberBaseHelper<BigInteger>.CreateChecked<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateChecked<nint>((nint)0x00000000));
                Assert.Equal(One, NumberBaseHelper<BigInteger>.CreateChecked<nint>((nint)0x00000001));
                Assert.Equal(Int32MaxValue, NumberBaseHelper<BigInteger>.CreateChecked<nint>((nint)0x7FFFFFFF));
                Assert.Equal(Int32MinValue, NumberBaseHelper<BigInteger>.CreateChecked<nint>(unchecked((nint)0x80000000)));
                Assert.Equal(NegativeOne, NumberBaseHelper<BigInteger>.CreateChecked<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateCheckedFromSByteTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateChecked<sbyte>(0x00));
            Assert.Equal(One, NumberBaseHelper<BigInteger>.CreateChecked<sbyte>(0x01));
            Assert.Equal(SByteMaxValue, NumberBaseHelper<BigInteger>.CreateChecked<sbyte>(0x7F));
            Assert.Equal(SByteMinValue, NumberBaseHelper<BigInteger>.CreateChecked<sbyte>(unchecked((sbyte)0x80)));
            Assert.Equal(NegativeOne, NumberBaseHelper<BigInteger>.CreateChecked<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateCheckedFromUInt16Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateChecked<ushort>(0x0000));
            Assert.Equal(One, NumberBaseHelper<BigInteger>.CreateChecked<ushort>(0x0001));
            Assert.Equal(Int16MaxValue, NumberBaseHelper<BigInteger>.CreateChecked<ushort>(0x7FFF));
            Assert.Equal(Int16MaxValuePlusOne, NumberBaseHelper<BigInteger>.CreateChecked<ushort>(0x8000));
            Assert.Equal(UInt16MaxValue, NumberBaseHelper<BigInteger>.CreateChecked<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUInt32Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateChecked<uint>(0x00000000));
            Assert.Equal(One, NumberBaseHelper<BigInteger>.CreateChecked<uint>(0x00000001));
            Assert.Equal(Int32MaxValue, NumberBaseHelper<BigInteger>.CreateChecked<uint>(0x7FFFFFFF));
            Assert.Equal(Int32MaxValuePlusOne, NumberBaseHelper<BigInteger>.CreateChecked<uint>(0x80000000));
            Assert.Equal(UInt32MaxValue, NumberBaseHelper<BigInteger>.CreateChecked<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUInt64Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateChecked<ulong>(0x00000000));
            Assert.Equal(One, NumberBaseHelper<BigInteger>.CreateChecked<ulong>(0x00000001));
            Assert.Equal(Int64MaxValue, NumberBaseHelper<BigInteger>.CreateChecked<ulong>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal(Int64MaxValuePlusOne, NumberBaseHelper<BigInteger>.CreateChecked<ulong>(0x8000000000000000));
            Assert.Equal(UInt64MaxValue, NumberBaseHelper<BigInteger>.CreateChecked<ulong>(0xFFFFFFFFFFFFFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateChecked<nuint>(unchecked((nuint)0x00000000)));
                Assert.Equal(One, NumberBaseHelper<BigInteger>.CreateChecked<nuint>(unchecked((nuint)0x00000001)));
                Assert.Equal(Int64MaxValue, NumberBaseHelper<BigInteger>.CreateChecked<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(Int64MaxValuePlusOne, NumberBaseHelper<BigInteger>.CreateChecked<nuint>(unchecked((nuint)0x8000000000000000)));
                Assert.Equal(UInt64MaxValue, NumberBaseHelper<BigInteger>.CreateChecked<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateChecked<nuint>((nuint)0x00000000));
                Assert.Equal(One, NumberBaseHelper<BigInteger>.CreateChecked<nuint>((nuint)0x00000001));
                Assert.Equal(Int32MaxValue, NumberBaseHelper<BigInteger>.CreateChecked<nuint>((nuint)0x7FFFFFFF));
                Assert.Equal(Int32MaxValuePlusOne, NumberBaseHelper<BigInteger>.CreateChecked<nuint>((nuint)0x80000000));
                Assert.Equal(UInt32MaxValue, NumberBaseHelper<BigInteger>.CreateChecked<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateSaturatingFromByteTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateSaturating<byte>(0x00));
            Assert.Equal(One, NumberBaseHelper<BigInteger>.CreateSaturating<byte>(0x01));
            Assert.Equal(SByteMaxValue, NumberBaseHelper<BigInteger>.CreateSaturating<byte>(0x7F));
            Assert.Equal(SByteMaxValuePlusOne, NumberBaseHelper<BigInteger>.CreateSaturating<byte>(0x80));
            Assert.Equal(ByteMaxValue, NumberBaseHelper<BigInteger>.CreateSaturating<byte>(0xFF));
        }

        [Fact]
        public static void CreateSaturatingFromCharTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateSaturating<char>((char)0x0000));
            Assert.Equal(One, NumberBaseHelper<BigInteger>.CreateSaturating<char>((char)0x0001));
            Assert.Equal(Int16MaxValue, NumberBaseHelper<BigInteger>.CreateSaturating<char>((char)0x7FFF));
            Assert.Equal(Int16MaxValuePlusOne, NumberBaseHelper<BigInteger>.CreateSaturating<char>((char)0x8000));
            Assert.Equal(UInt16MaxValue, NumberBaseHelper<BigInteger>.CreateSaturating<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromInt16Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateSaturating<short>(0x0000));
            Assert.Equal(One, NumberBaseHelper<BigInteger>.CreateSaturating<short>(0x0001));
            Assert.Equal(Int16MaxValue, NumberBaseHelper<BigInteger>.CreateSaturating<short>(0x7FFF));
            Assert.Equal(Int16MinValue, NumberBaseHelper<BigInteger>.CreateSaturating<short>(unchecked((short)0x8000)));
            Assert.Equal(NegativeOne, NumberBaseHelper<BigInteger>.CreateSaturating<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromInt32Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateSaturating<int>(0x00000000));
            Assert.Equal(One, NumberBaseHelper<BigInteger>.CreateSaturating<int>(0x00000001));
            Assert.Equal(Int32MaxValue, NumberBaseHelper<BigInteger>.CreateSaturating<int>(0x7FFFFFFF));
            Assert.Equal(Int32MinValue, NumberBaseHelper<BigInteger>.CreateSaturating<int>(unchecked((int)0x80000000)));
            Assert.Equal(NegativeOne, NumberBaseHelper<BigInteger>.CreateSaturating<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromInt64Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateSaturating<long>(0x00000000));
            Assert.Equal(One, NumberBaseHelper<BigInteger>.CreateSaturating<long>(0x00000001));
            Assert.Equal(Int64MaxValue, NumberBaseHelper<BigInteger>.CreateSaturating<long>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal(Int64MinValue, NumberBaseHelper<BigInteger>.CreateSaturating<long>(unchecked((long)0x8000000000000000)));
            Assert.Equal(NegativeOne, NumberBaseHelper<BigInteger>.CreateSaturating<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateSaturating<nint>(unchecked((nint)0x00000000)));
                Assert.Equal(One, NumberBaseHelper<BigInteger>.CreateSaturating<nint>(unchecked((nint)0x00000001)));
                Assert.Equal(Int64MaxValue, NumberBaseHelper<BigInteger>.CreateSaturating<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(Int64MinValue, NumberBaseHelper<BigInteger>.CreateSaturating<nint>(unchecked((nint)0x8000000000000000)));
                Assert.Equal(NegativeOne, NumberBaseHelper<BigInteger>.CreateSaturating<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateSaturating<nint>((nint)0x00000000));
                Assert.Equal(One, NumberBaseHelper<BigInteger>.CreateSaturating<nint>((nint)0x00000001));
                Assert.Equal(Int32MaxValue, NumberBaseHelper<BigInteger>.CreateSaturating<nint>((nint)0x7FFFFFFF));
                Assert.Equal(Int32MinValue, NumberBaseHelper<BigInteger>.CreateSaturating<nint>(unchecked((nint)0x80000000)));
                Assert.Equal(NegativeOne, NumberBaseHelper<BigInteger>.CreateSaturating<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateSaturatingFromSByteTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateSaturating<sbyte>(0x00));
            Assert.Equal(One, NumberBaseHelper<BigInteger>.CreateSaturating<sbyte>(0x01));
            Assert.Equal(SByteMaxValue, NumberBaseHelper<BigInteger>.CreateSaturating<sbyte>(0x7F));
            Assert.Equal(SByteMinValue, NumberBaseHelper<BigInteger>.CreateSaturating<sbyte>(unchecked((sbyte)0x80)));
            Assert.Equal(NegativeOne, NumberBaseHelper<BigInteger>.CreateSaturating<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateSaturatingFromUInt16Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateSaturating<ushort>(0x0000));
            Assert.Equal(One, NumberBaseHelper<BigInteger>.CreateSaturating<ushort>(0x0001));
            Assert.Equal(Int16MaxValue, NumberBaseHelper<BigInteger>.CreateSaturating<ushort>(0x7FFF));
            Assert.Equal(Int16MaxValuePlusOne, NumberBaseHelper<BigInteger>.CreateSaturating<ushort>(0x8000));
            Assert.Equal(UInt16MaxValue, NumberBaseHelper<BigInteger>.CreateSaturating<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUInt32Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateSaturating<uint>(0x00000000));
            Assert.Equal(One, NumberBaseHelper<BigInteger>.CreateSaturating<uint>(0x00000001));
            Assert.Equal(Int32MaxValue, NumberBaseHelper<BigInteger>.CreateSaturating<uint>(0x7FFFFFFF));
            Assert.Equal(Int32MaxValuePlusOne, NumberBaseHelper<BigInteger>.CreateSaturating<uint>(0x80000000));
            Assert.Equal(UInt32MaxValue, NumberBaseHelper<BigInteger>.CreateSaturating<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUInt64Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateSaturating<ulong>(0x00000000));
            Assert.Equal(One, NumberBaseHelper<BigInteger>.CreateSaturating<ulong>(0x00000001));
            Assert.Equal(Int64MaxValue, NumberBaseHelper<BigInteger>.CreateSaturating<ulong>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal(Int64MaxValuePlusOne, NumberBaseHelper<BigInteger>.CreateSaturating<ulong>(0x8000000000000000));
            Assert.Equal(UInt64MaxValue, NumberBaseHelper<BigInteger>.CreateSaturating<ulong>(0xFFFFFFFFFFFFFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateSaturating<nuint>(unchecked((nuint)0x00000000)));
                Assert.Equal(One, NumberBaseHelper<BigInteger>.CreateSaturating<nuint>(unchecked((nuint)0x00000001)));
                Assert.Equal(Int64MaxValue, NumberBaseHelper<BigInteger>.CreateSaturating<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(Int64MaxValuePlusOne, NumberBaseHelper<BigInteger>.CreateSaturating<nuint>(unchecked((nuint)0x8000000000000000)));
                Assert.Equal(UInt64MaxValue, NumberBaseHelper<BigInteger>.CreateSaturating<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateSaturating<nuint>((nuint)0x00000000));
                Assert.Equal(One, NumberBaseHelper<BigInteger>.CreateSaturating<nuint>((nuint)0x00000001));
                Assert.Equal(Int32MaxValue, NumberBaseHelper<BigInteger>.CreateSaturating<nuint>((nuint)0x7FFFFFFF));
                Assert.Equal(Int32MaxValuePlusOne, NumberBaseHelper<BigInteger>.CreateSaturating<nuint>((nuint)0x80000000));
                Assert.Equal(UInt32MaxValue, NumberBaseHelper<BigInteger>.CreateSaturating<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateTruncatingFromByteTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateTruncating<byte>(0x00));
            Assert.Equal(One, NumberBaseHelper<BigInteger>.CreateTruncating<byte>(0x01));
            Assert.Equal(SByteMaxValue, NumberBaseHelper<BigInteger>.CreateTruncating<byte>(0x7F));
            Assert.Equal(SByteMaxValuePlusOne, NumberBaseHelper<BigInteger>.CreateTruncating<byte>(0x80));
            Assert.Equal(ByteMaxValue, NumberBaseHelper<BigInteger>.CreateTruncating<byte>(0xFF));
        }

        [Fact]
        public static void CreateTruncatingFromCharTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateTruncating<char>((char)0x0000));
            Assert.Equal(One, NumberBaseHelper<BigInteger>.CreateTruncating<char>((char)0x0001));
            Assert.Equal(Int16MaxValue, NumberBaseHelper<BigInteger>.CreateTruncating<char>((char)0x7FFF));
            Assert.Equal(Int16MaxValuePlusOne, NumberBaseHelper<BigInteger>.CreateTruncating<char>((char)0x8000));
            Assert.Equal(UInt16MaxValue, NumberBaseHelper<BigInteger>.CreateTruncating<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromInt16Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateTruncating<short>(0x0000));
            Assert.Equal(One, NumberBaseHelper<BigInteger>.CreateTruncating<short>(0x0001));
            Assert.Equal(Int16MaxValue, NumberBaseHelper<BigInteger>.CreateTruncating<short>(0x7FFF));
            Assert.Equal(Int16MinValue, NumberBaseHelper<BigInteger>.CreateTruncating<short>(unchecked((short)0x8000)));
            Assert.Equal(NegativeOne, NumberBaseHelper<BigInteger>.CreateTruncating<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateTruncatingFromInt32Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateTruncating<int>(0x00000000));
            Assert.Equal(One, NumberBaseHelper<BigInteger>.CreateTruncating<int>(0x00000001));
            Assert.Equal(Int32MaxValue, NumberBaseHelper<BigInteger>.CreateTruncating<int>(0x7FFFFFFF));
            Assert.Equal(Int32MinValue, NumberBaseHelper<BigInteger>.CreateTruncating<int>(unchecked((int)0x80000000)));
            Assert.Equal(NegativeOne, NumberBaseHelper<BigInteger>.CreateTruncating<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateTruncatingFromInt64Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateTruncating<long>(0x00000000));
            Assert.Equal(One, NumberBaseHelper<BigInteger>.CreateTruncating<long>(0x00000001));
            Assert.Equal(Int64MaxValue, NumberBaseHelper<BigInteger>.CreateTruncating<long>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal(Int64MinValue, NumberBaseHelper<BigInteger>.CreateTruncating<long>(unchecked((long)0x8000000000000000)));
            Assert.Equal(NegativeOne, NumberBaseHelper<BigInteger>.CreateTruncating<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void CreateTruncatingFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateTruncating<nint>(unchecked((nint)0x00000000)));
                Assert.Equal(One, NumberBaseHelper<BigInteger>.CreateTruncating<nint>(unchecked((nint)0x00000001)));
                Assert.Equal(Int64MaxValue, NumberBaseHelper<BigInteger>.CreateTruncating<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(Int64MinValue, NumberBaseHelper<BigInteger>.CreateTruncating<nint>(unchecked((nint)0x8000000000000000)));
                Assert.Equal(NegativeOne, NumberBaseHelper<BigInteger>.CreateTruncating<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateTruncating<nint>((nint)0x00000000));
                Assert.Equal(One, NumberBaseHelper<BigInteger>.CreateTruncating<nint>((nint)0x00000001));
                Assert.Equal(Int32MaxValue, NumberBaseHelper<BigInteger>.CreateTruncating<nint>((nint)0x7FFFFFFF));
                Assert.Equal(Int32MinValue, NumberBaseHelper<BigInteger>.CreateTruncating<nint>(unchecked((nint)0x80000000)));
                Assert.Equal(NegativeOne, NumberBaseHelper<BigInteger>.CreateTruncating<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateTruncatingFromSByteTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateTruncating<sbyte>(0x00));
            Assert.Equal(One, NumberBaseHelper<BigInteger>.CreateTruncating<sbyte>(0x01));
            Assert.Equal(SByteMaxValue, NumberBaseHelper<BigInteger>.CreateTruncating<sbyte>(0x7F));
            Assert.Equal(SByteMinValue, NumberBaseHelper<BigInteger>.CreateTruncating<sbyte>(unchecked((sbyte)0x80)));
            Assert.Equal(NegativeOne, NumberBaseHelper<BigInteger>.CreateTruncating<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateTruncatingFromUInt16Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateTruncating<ushort>(0x0000));
            Assert.Equal(One, NumberBaseHelper<BigInteger>.CreateTruncating<ushort>(0x0001));
            Assert.Equal(Int16MaxValue, NumberBaseHelper<BigInteger>.CreateTruncating<ushort>(0x7FFF));
            Assert.Equal(Int16MaxValuePlusOne, NumberBaseHelper<BigInteger>.CreateTruncating<ushort>(0x8000));
            Assert.Equal(UInt16MaxValue, NumberBaseHelper<BigInteger>.CreateTruncating<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt32Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateTruncating<uint>(0x00000000));
            Assert.Equal(One, NumberBaseHelper<BigInteger>.CreateTruncating<uint>(0x00000001));
            Assert.Equal(Int32MaxValue, NumberBaseHelper<BigInteger>.CreateTruncating<uint>(0x7FFFFFFF));
            Assert.Equal(Int32MaxValuePlusOne, NumberBaseHelper<BigInteger>.CreateTruncating<uint>(0x80000000));
            Assert.Equal(UInt32MaxValue, NumberBaseHelper<BigInteger>.CreateTruncating<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt64Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateTruncating<ulong>(0x00000000));
            Assert.Equal(One, NumberBaseHelper<BigInteger>.CreateTruncating<ulong>(0x00000001));
            Assert.Equal(Int64MaxValue, NumberBaseHelper<BigInteger>.CreateTruncating<ulong>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal(Int64MaxValuePlusOne, NumberBaseHelper<BigInteger>.CreateTruncating<ulong>(0x8000000000000000));
            Assert.Equal(UInt64MaxValue, NumberBaseHelper<BigInteger>.CreateTruncating<ulong>(0xFFFFFFFFFFFFFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateTruncating<nuint>(unchecked((nuint)0x00000000)));
                Assert.Equal(One, NumberBaseHelper<BigInteger>.CreateTruncating<nuint>(unchecked((nuint)0x00000001)));
                Assert.Equal(Int64MaxValue, NumberBaseHelper<BigInteger>.CreateTruncating<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(Int64MaxValuePlusOne, NumberBaseHelper<BigInteger>.CreateTruncating<nuint>(unchecked((nuint)0x8000000000000000)));
                Assert.Equal(UInt64MaxValue, NumberBaseHelper<BigInteger>.CreateTruncating<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateTruncating<nuint>((nuint)0x00000000));
                Assert.Equal(One, NumberBaseHelper<BigInteger>.CreateTruncating<nuint>((nuint)0x00000001));
                Assert.Equal(Int32MaxValue, NumberBaseHelper<BigInteger>.CreateTruncating<nuint>((nuint)0x7FFFFFFF));
                Assert.Equal(Int32MaxValuePlusOne, NumberBaseHelper<BigInteger>.CreateTruncating<nuint>((nuint)0x80000000));
                Assert.Equal(UInt32MaxValue, NumberBaseHelper<BigInteger>.CreateTruncating<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void IsFiniteTest()
        {
            Assert.True(NumberBaseHelper<BigInteger>.IsFinite(Zero));
            Assert.True(NumberBaseHelper<BigInteger>.IsFinite(One));
            Assert.True(NumberBaseHelper<BigInteger>.IsFinite(Int64MaxValue));

            Assert.True(NumberBaseHelper<BigInteger>.IsFinite(Int64MinValue));
            Assert.True(NumberBaseHelper<BigInteger>.IsFinite(NegativeOne));

            Assert.True(NumberBaseHelper<BigInteger>.IsFinite(Int64MaxValuePlusOne));
            Assert.True(NumberBaseHelper<BigInteger>.IsFinite(UInt64MaxValue));
        }

        [Fact]
        public static void IsInfinityTest()
        {
            Assert.False(NumberBaseHelper<BigInteger>.IsInfinity(Zero));
            Assert.False(NumberBaseHelper<BigInteger>.IsInfinity(One));
            Assert.False(NumberBaseHelper<BigInteger>.IsInfinity(Int64MaxValue));

            Assert.False(NumberBaseHelper<BigInteger>.IsInfinity(Int64MinValue));
            Assert.False(NumberBaseHelper<BigInteger>.IsInfinity(NegativeOne));

            Assert.False(NumberBaseHelper<BigInteger>.IsInfinity(Int64MaxValuePlusOne));
            Assert.False(NumberBaseHelper<BigInteger>.IsInfinity(UInt64MaxValue));
        }

        [Fact]
        public static void IsNaNTest()
        {
            Assert.False(NumberBaseHelper<BigInteger>.IsNaN(Zero));
            Assert.False(NumberBaseHelper<BigInteger>.IsNaN(One));
            Assert.False(NumberBaseHelper<BigInteger>.IsNaN(Int64MaxValue));

            Assert.False(NumberBaseHelper<BigInteger>.IsNaN(Int64MinValue));
            Assert.False(NumberBaseHelper<BigInteger>.IsNaN(NegativeOne));

            Assert.False(NumberBaseHelper<BigInteger>.IsNaN(Int64MaxValuePlusOne));
            Assert.False(NumberBaseHelper<BigInteger>.IsNaN(UInt64MaxValue));
        }

        [Fact]
        public static void IsNegativeTest()
        {
            Assert.False(NumberBaseHelper<BigInteger>.IsNegative(Zero));
            Assert.False(NumberBaseHelper<BigInteger>.IsNegative(One));
            Assert.False(NumberBaseHelper<BigInteger>.IsNegative(Int64MaxValue));

            Assert.True(NumberBaseHelper<BigInteger>.IsNegative(Int64MinValue));
            Assert.True(NumberBaseHelper<BigInteger>.IsNegative(NegativeOne));

            Assert.False(NumberBaseHelper<BigInteger>.IsNegative(Int64MaxValuePlusOne));
            Assert.False(NumberBaseHelper<BigInteger>.IsNegative(UInt64MaxValue));
        }

        [Fact]
        public static void IsNegativeInfinityTest()
        {
            Assert.False(NumberBaseHelper<BigInteger>.IsNegativeInfinity(Zero));
            Assert.False(NumberBaseHelper<BigInteger>.IsNegativeInfinity(One));
            Assert.False(NumberBaseHelper<BigInteger>.IsNegativeInfinity(Int64MaxValue));

            Assert.False(NumberBaseHelper<BigInteger>.IsNegativeInfinity(Int64MinValue));
            Assert.False(NumberBaseHelper<BigInteger>.IsNegativeInfinity(NegativeOne));

            Assert.False(NumberBaseHelper<BigInteger>.IsNegativeInfinity(Int64MaxValuePlusOne));
            Assert.False(NumberBaseHelper<BigInteger>.IsNegativeInfinity(UInt64MaxValue));
        }

        [Fact]
        public static void IsNormalTest()
        {
            Assert.False(NumberBaseHelper<BigInteger>.IsNormal(Zero));
            Assert.True(NumberBaseHelper<BigInteger>.IsNormal(One));
            Assert.True(NumberBaseHelper<BigInteger>.IsNormal(Int64MaxValue));

            Assert.True(NumberBaseHelper<BigInteger>.IsNormal(Int64MinValue));
            Assert.True(NumberBaseHelper<BigInteger>.IsNormal(NegativeOne));

            Assert.True(NumberBaseHelper<BigInteger>.IsNormal(Int64MaxValuePlusOne));
            Assert.True(NumberBaseHelper<BigInteger>.IsNormal(UInt64MaxValue));
        }

        [Fact]
        public static void IsPositiveInfinityTest()
        {
            Assert.False(NumberBaseHelper<BigInteger>.IsPositiveInfinity(Zero));
            Assert.False(NumberBaseHelper<BigInteger>.IsPositiveInfinity(One));
            Assert.False(NumberBaseHelper<BigInteger>.IsPositiveInfinity(Int64MaxValue));

            Assert.False(NumberBaseHelper<BigInteger>.IsPositiveInfinity(Int64MinValue));
            Assert.False(NumberBaseHelper<BigInteger>.IsPositiveInfinity(NegativeOne));

            Assert.False(NumberBaseHelper<BigInteger>.IsPositiveInfinity(Int64MaxValuePlusOne));
            Assert.False(NumberBaseHelper<BigInteger>.IsPositiveInfinity(UInt64MaxValue));
        }

        [Fact]
        public static void IsSubnormalTest()
        {
            Assert.False(NumberBaseHelper<BigInteger>.IsSubnormal(Zero));
            Assert.False(NumberBaseHelper<BigInteger>.IsSubnormal(One));
            Assert.False(NumberBaseHelper<BigInteger>.IsSubnormal(Int64MaxValue));

            Assert.False(NumberBaseHelper<BigInteger>.IsSubnormal(Int64MinValue));
            Assert.False(NumberBaseHelper<BigInteger>.IsSubnormal(NegativeOne));

            Assert.False(NumberBaseHelper<BigInteger>.IsSubnormal(Int64MaxValuePlusOne));
            Assert.False(NumberBaseHelper<BigInteger>.IsSubnormal(UInt64MaxValue));
        }

        [Fact]
        public static void MaxMagnitudeTest()
        {
            Assert.Equal(One, NumberBaseHelper<BigInteger>.MaxMagnitude(Zero, 1));
            Assert.Equal(One, NumberBaseHelper<BigInteger>.MaxMagnitude(One, 1));
            Assert.Equal(Int64MaxValue, NumberBaseHelper<BigInteger>.MaxMagnitude(Int64MaxValue, 1));

            Assert.Equal(Int64MinValue, NumberBaseHelper<BigInteger>.MaxMagnitude(Int64MinValue, 1));
            Assert.Equal(One, NumberBaseHelper<BigInteger>.MaxMagnitude(NegativeOne, 1));

            Assert.Equal(Int64MaxValuePlusOne, NumberBaseHelper<BigInteger>.MaxMagnitude(Int64MaxValuePlusOne, 1));
            Assert.Equal(UInt64MaxValue, NumberBaseHelper<BigInteger>.MaxMagnitude(UInt64MaxValue, 1));
        }

        [Fact]
        public static void MaxMagnitudeNumberTest()
        {
            Assert.Equal(One, NumberBaseHelper<BigInteger>.MaxMagnitudeNumber(Zero, 1));
            Assert.Equal(One, NumberBaseHelper<BigInteger>.MaxMagnitudeNumber(One, 1));
            Assert.Equal(Int64MaxValue, NumberBaseHelper<BigInteger>.MaxMagnitudeNumber(Int64MaxValue, 1));

            Assert.Equal(Int64MinValue, NumberBaseHelper<BigInteger>.MaxMagnitudeNumber(Int64MinValue, 1));
            Assert.Equal(One, NumberBaseHelper<BigInteger>.MaxMagnitudeNumber(NegativeOne, 1));

            Assert.Equal(Int64MaxValuePlusOne, NumberBaseHelper<BigInteger>.MaxMagnitudeNumber(Int64MaxValuePlusOne, 1));
            Assert.Equal(UInt64MaxValue, NumberBaseHelper<BigInteger>.MaxMagnitudeNumber(UInt64MaxValue, 1));
        }

        [Fact]
        public static void MinMagnitudeTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.MinMagnitude(Zero, 1));
            Assert.Equal(One, NumberBaseHelper<BigInteger>.MinMagnitude(One, 1));
            Assert.Equal(One, NumberBaseHelper<BigInteger>.MinMagnitude(Int64MaxValue, 1));

            Assert.Equal(One, NumberBaseHelper<BigInteger>.MinMagnitude(Int64MinValue, 1));
            Assert.Equal(NegativeOne, NumberBaseHelper<BigInteger>.MinMagnitude(NegativeOne, 1));

            Assert.Equal(One, NumberBaseHelper<BigInteger>.MinMagnitude(Int64MaxValuePlusOne, 1));
            Assert.Equal(One, NumberBaseHelper<BigInteger>.MinMagnitude(UInt64MaxValue, 1));
        }

        [Fact]
        public static void MinMagnitudeNumberTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.MinMagnitudeNumber(Zero, 1));
            Assert.Equal(One, NumberBaseHelper<BigInteger>.MinMagnitudeNumber(One, 1));
            Assert.Equal(One, NumberBaseHelper<BigInteger>.MinMagnitudeNumber(Int64MaxValue, 1));

            Assert.Equal(One, NumberBaseHelper<BigInteger>.MinMagnitudeNumber(Int64MinValue, 1));
            Assert.Equal(NegativeOne, NumberBaseHelper<BigInteger>.MinMagnitudeNumber(NegativeOne, 1));

            Assert.Equal(One, NumberBaseHelper<BigInteger>.MinMagnitudeNumber(Int64MaxValuePlusOne, 1));
            Assert.Equal(One, NumberBaseHelper<BigInteger>.MinMagnitudeNumber(UInt64MaxValue, 1));
        }

        [Fact]
        public static void TryCreateFromByteTest()
        {
            BigInteger result;

            Assert.True(NumberBaseHelper<BigInteger>.TryCreate<byte>(0x00, out result));
            Assert.Equal(Zero, result);

            Assert.True(NumberBaseHelper<BigInteger>.TryCreate<byte>(0x01, out result));
            Assert.Equal(One, result);

            Assert.True(NumberBaseHelper<BigInteger>.TryCreate<byte>(0x7F, out result));
            Assert.Equal(SByteMaxValue, result);

            Assert.True(NumberBaseHelper<BigInteger>.TryCreate<byte>(0x80, out result));
            Assert.Equal(SByteMaxValuePlusOne, result);

            Assert.True(NumberBaseHelper<BigInteger>.TryCreate<byte>(0xFF, out result));
            Assert.Equal(ByteMaxValue, result);
        }

        [Fact]
        public static void TryCreateFromCharTest()
        {
            BigInteger result;

            Assert.True(NumberBaseHelper<BigInteger>.TryCreate<char>((char)0x0000, out result));
            Assert.Equal(Zero, result);

            Assert.True(NumberBaseHelper<BigInteger>.TryCreate<char>((char)0x0001, out result));
            Assert.Equal(One, result);

            Assert.True(NumberBaseHelper<BigInteger>.TryCreate<char>((char)0x7FFF, out result));
            Assert.Equal(Int16MaxValue, result);

            Assert.True(NumberBaseHelper<BigInteger>.TryCreate<char>((char)0x8000, out result));
            Assert.Equal(Int16MaxValuePlusOne, result);

            Assert.True(NumberBaseHelper<BigInteger>.TryCreate<char>((char)0xFFFF, out result));
            Assert.Equal(UInt16MaxValue, result);
        }

        [Fact]
        public static void TryCreateFromInt16Test()
        {
            BigInteger result;

            Assert.True(NumberBaseHelper<BigInteger>.TryCreate<short>(0x0000, out result));
            Assert.Equal(Zero, result);

            Assert.True(NumberBaseHelper<BigInteger>.TryCreate<short>(0x0001, out result));
            Assert.Equal(One, result);

            Assert.True(NumberBaseHelper<BigInteger>.TryCreate<short>(0x7FFF, out result));
            Assert.Equal(Int16MaxValue, result);

            Assert.True(NumberBaseHelper<BigInteger>.TryCreate<short>(unchecked((short)0x8000), out result));
            Assert.Equal(Int16MinValue, result);

            Assert.True(NumberBaseHelper<BigInteger>.TryCreate<short>(unchecked((short)0xFFFF), out result));
            Assert.Equal(NegativeOne, result);
        }

        [Fact]
        public static void TryCreateFromInt32Test()
        {
            BigInteger result;

            Assert.True(NumberBaseHelper<BigInteger>.TryCreate<int>(0x00000000, out result));
            Assert.Equal(Zero, result);

            Assert.True(NumberBaseHelper<BigInteger>.TryCreate<int>(0x00000001, out result));
            Assert.Equal(One, result);

            Assert.True(NumberBaseHelper<BigInteger>.TryCreate<int>(0x7FFFFFFF, out result));
            Assert.Equal(Int32MaxValue, result);

            Assert.True(NumberBaseHelper<BigInteger>.TryCreate<int>(unchecked((int)0x80000000), out result));
            Assert.Equal(Int32MinValue, result);

            Assert.True(NumberBaseHelper<BigInteger>.TryCreate<int>(unchecked((int)0xFFFFFFFF), out result));
            Assert.Equal(NegativeOne, result);
        }

        [Fact]
        public static void TryCreateFromInt64Test()
        {
            BigInteger result;

            Assert.True(NumberBaseHelper<BigInteger>.TryCreate<long>(0x00000000, out result));
            Assert.Equal(Zero, result);

            Assert.True(NumberBaseHelper<BigInteger>.TryCreate<long>(0x00000001, out result));
            Assert.Equal(One, result);

            Assert.True(NumberBaseHelper<BigInteger>.TryCreate<long>(0x7FFFFFFFFFFFFFFF, out result));
            Assert.Equal(Int64MaxValue, result);

            Assert.True(NumberBaseHelper<BigInteger>.TryCreate<long>(unchecked((long)0x8000000000000000), out result));
            Assert.Equal(Int64MinValue, result);

            Assert.True(NumberBaseHelper<BigInteger>.TryCreate<long>(unchecked((long)0xFFFFFFFFFFFFFFFF), out result));
            Assert.Equal(NegativeOne, result);
        }

        [Fact]
        public static void TryCreateFromIntPtrTest()
        {
            BigInteger result;

            if (Environment.Is64BitProcess)
            {
                Assert.True(NumberBaseHelper<BigInteger>.TryCreate<nint>(unchecked((nint)0x00000000), out result));
                Assert.Equal(Zero, result);

                Assert.True(NumberBaseHelper<BigInteger>.TryCreate<nint>(unchecked((nint)0x00000001), out result));
                Assert.Equal(One, result);

                Assert.True(NumberBaseHelper<BigInteger>.TryCreate<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF), out result));
                Assert.Equal(Int64MaxValue, result);

                Assert.True(NumberBaseHelper<BigInteger>.TryCreate<nint>(unchecked((nint)0x8000000000000000), out result));
                Assert.Equal(Int64MinValue, result);

                Assert.True(NumberBaseHelper<BigInteger>.TryCreate<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF), out result));
                Assert.Equal(NegativeOne, result);
            }
            else
            {
                Assert.True(NumberBaseHelper<BigInteger>.TryCreate<nint>((nint)0x00000000, out result));
                Assert.Equal(Zero, result);

                Assert.True(NumberBaseHelper<BigInteger>.TryCreate<nint>((nint)0x00000001, out result));
                Assert.Equal(One, result);

                Assert.True(NumberBaseHelper<BigInteger>.TryCreate<nint>((nint)0x7FFFFFFF, out result));
                Assert.Equal(Int32MaxValue, result);

                Assert.True(NumberBaseHelper<BigInteger>.TryCreate<nint>(unchecked((nint)0x80000000), out result));
                Assert.Equal(Int32MinValue, result);

                Assert.True(NumberBaseHelper<BigInteger>.TryCreate<nint>(unchecked((nint)0xFFFFFFFF), out result));
                Assert.Equal(NegativeOne, result);
            }
        }

        [Fact]
        public static void TryCreateFromSByteTest()
        {
            BigInteger result;

            Assert.True(NumberBaseHelper<BigInteger>.TryCreate<sbyte>(0x00, out result));
            Assert.Equal(Zero, result);

            Assert.True(NumberBaseHelper<BigInteger>.TryCreate<sbyte>(0x01, out result));
            Assert.Equal(One, result);

            Assert.True(NumberBaseHelper<BigInteger>.TryCreate<sbyte>(0x7F, out result));
            Assert.Equal(SByteMaxValue, result);

            Assert.True(NumberBaseHelper<BigInteger>.TryCreate<sbyte>(unchecked((sbyte)0x80), out result));
            Assert.Equal(SByteMinValue, result);

            Assert.True(NumberBaseHelper<BigInteger>.TryCreate<sbyte>(unchecked((sbyte)0xFF), out result));
            Assert.Equal(NegativeOne, result);
        }

        [Fact]
        public static void TryCreateFromUInt16Test()
        {
            BigInteger result;

            Assert.True(NumberBaseHelper<BigInteger>.TryCreate<ushort>(0x0000, out result));
            Assert.Equal(Zero, result);

            Assert.True(NumberBaseHelper<BigInteger>.TryCreate<ushort>(0x0001, out result));
            Assert.Equal(One, result);

            Assert.True(NumberBaseHelper<BigInteger>.TryCreate<ushort>(0x7FFF, out result));
            Assert.Equal(Int16MaxValue, result);

            Assert.True(NumberBaseHelper<BigInteger>.TryCreate<ushort>(0x8000, out result));
            Assert.Equal(Int16MaxValuePlusOne, result);

            Assert.True(NumberBaseHelper<BigInteger>.TryCreate<ushort>(0xFFFF, out result));
            Assert.Equal(UInt16MaxValue, result);
        }

        [Fact]
        public static void TryCreateFromUInt32Test()
        {
            BigInteger result;

            Assert.True(NumberBaseHelper<BigInteger>.TryCreate<uint>(0x00000000, out result));
            Assert.Equal(Zero, result);

            Assert.True(NumberBaseHelper<BigInteger>.TryCreate<uint>(0x00000001, out result));
            Assert.Equal(One, result);

            Assert.True(NumberBaseHelper<BigInteger>.TryCreate<uint>(0x7FFFFFFF, out result));
            Assert.Equal(Int32MaxValue, result);

            Assert.True(NumberBaseHelper<BigInteger>.TryCreate<uint>(0x80000000, out result));
            Assert.Equal(Int32MaxValuePlusOne, result);

            Assert.True(NumberBaseHelper<BigInteger>.TryCreate<uint>(0xFFFFFFFF, out result));
            Assert.Equal(UInt32MaxValue, result);
        }

        [Fact]
        public static void TryCreateFromUInt64Test()
        {
            BigInteger result;

            Assert.True(NumberBaseHelper<BigInteger>.TryCreate<ulong>(0x00000000, out result));
            Assert.Equal(Zero, result);

            Assert.True(NumberBaseHelper<BigInteger>.TryCreate<ulong>(0x00000001, out result));
            Assert.Equal(One, result);

            Assert.True(NumberBaseHelper<BigInteger>.TryCreate<ulong>(0x7FFFFFFFFFFFFFFF, out result));
            Assert.Equal(Int64MaxValue, result);

            Assert.True(NumberBaseHelper<BigInteger>.TryCreate<ulong>(0x8000000000000000, out result));
            Assert.Equal(Int64MaxValuePlusOne, result);

            Assert.True(NumberBaseHelper<BigInteger>.TryCreate<ulong>(0xFFFFFFFFFFFFFFFF, out result));
            Assert.Equal(UInt64MaxValue, result);
        }

        [Fact]
        public static void TryCreateFromUIntPtrTest()
        {
            BigInteger result;

            if (Environment.Is64BitProcess)
            {
                Assert.True(NumberBaseHelper<BigInteger>.TryCreate<nuint>(unchecked((nuint)0x00000000), out result));
                Assert.Equal(Zero, result);

                Assert.True(NumberBaseHelper<BigInteger>.TryCreate<nuint>(unchecked((nuint)0x00000001), out result));
                Assert.Equal(One, result);

                Assert.True(NumberBaseHelper<BigInteger>.TryCreate<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF), out result));
                Assert.Equal(Int64MaxValue, result);

                Assert.True(NumberBaseHelper<BigInteger>.TryCreate<nuint>(unchecked((nuint)0x8000000000000000), out result));
                Assert.Equal(Int64MaxValuePlusOne, result);

                Assert.True(NumberBaseHelper<BigInteger>.TryCreate<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF), out result));
                Assert.Equal(UInt64MaxValue, result);
            }
            else
            {
                Assert.True(NumberBaseHelper<BigInteger>.TryCreate<nuint>((nuint)0x00000000, out result));
                Assert.Equal(Zero, result);

                Assert.True(NumberBaseHelper<BigInteger>.TryCreate<nuint>((nuint)0x00000001, out result));
                Assert.Equal(One, result);

                Assert.True(NumberBaseHelper<BigInteger>.TryCreate<nuint>((nuint)0x7FFFFFFF, out result));
                Assert.Equal(Int32MaxValue, result);

                Assert.True(NumberBaseHelper<BigInteger>.TryCreate<nuint>(unchecked((nuint)0x80000000), out result));
                Assert.Equal(Int32MaxValuePlusOne, result);

                Assert.True(NumberBaseHelper<BigInteger>.TryCreate<nuint>(unchecked((nuint)0xFFFFFFFF), out result));
                Assert.Equal(UInt32MaxValue, result);
            }
        }

        //
        // IShiftOperators
        //

        [Fact]
        public static void op_LeftShiftTest()
        {
            Assert.Equal(Zero, ShiftOperatorsHelper<BigInteger, BigInteger>.op_LeftShift(Zero, 1));
            Assert.Equal(Two, ShiftOperatorsHelper<BigInteger, BigInteger>.op_LeftShift(One, 1));
            Assert.Equal(unchecked((BigInteger)0xFFFFFFFFFFFFFFFE), ShiftOperatorsHelper<BigInteger, BigInteger>.op_LeftShift(Int64MaxValue, 1));

            Assert.Equal(NegativeTwoPow64, ShiftOperatorsHelper<BigInteger, BigInteger>.op_LeftShift(Int64MinValue, 1));
            Assert.Equal(unchecked((BigInteger)(int)0xFFFFFFFE), ShiftOperatorsHelper<BigInteger, BigInteger>.op_LeftShift(NegativeOne, 1));

            Assert.Equal(TwoPow64, ShiftOperatorsHelper<BigInteger, BigInteger>.op_LeftShift(Int64MaxValuePlusOne, 1));
            Assert.Equal(UInt64MaxValueTimesTwo, ShiftOperatorsHelper<BigInteger, BigInteger>.op_LeftShift(UInt64MaxValue, 1));
        }

        [Fact]
        public static void op_RightShiftTest()
        {
            Assert.Equal(Zero, ShiftOperatorsHelper<BigInteger, BigInteger>.op_RightShift(Zero, 1));
            Assert.Equal(Zero, ShiftOperatorsHelper<BigInteger, BigInteger>.op_RightShift(One, 1));
            Assert.Equal((BigInteger)0x3FFFFFFFFFFFFFFF, ShiftOperatorsHelper<BigInteger, BigInteger>.op_RightShift(Int64MaxValue, 1));

            Assert.Equal(unchecked((BigInteger)(long)0xC000000000000000), ShiftOperatorsHelper<BigInteger, BigInteger>.op_RightShift(Int64MinValue, 1));
            Assert.Equal(NegativeOne, ShiftOperatorsHelper<BigInteger, BigInteger>.op_RightShift(NegativeOne, 1));

            Assert.Equal(unchecked((BigInteger)0x4000000000000000), ShiftOperatorsHelper<BigInteger, BigInteger>.op_RightShift(Int64MaxValuePlusOne, 1));
            Assert.Equal(Int64MaxValue, ShiftOperatorsHelper<BigInteger, BigInteger>.op_RightShift(UInt64MaxValue, 1));
        }

        [Fact]
        public static void op_UnsignedRightShiftTest()
        {
            Assert.Equal(Zero, ShiftOperatorsHelper<BigInteger, BigInteger>.op_UnsignedRightShift(Zero, 1));
            Assert.Equal(Zero, ShiftOperatorsHelper<BigInteger, BigInteger>.op_UnsignedRightShift(One, 1));
            Assert.Equal((BigInteger)0x3FFFFFFFFFFFFFFF, ShiftOperatorsHelper<BigInteger, BigInteger>.op_UnsignedRightShift(Int64MaxValue, 1));

            Assert.Equal((BigInteger)0x4000000000000000, ShiftOperatorsHelper<BigInteger, BigInteger>.op_UnsignedRightShift(Int64MinValue, 1));
            Assert.Equal(Int32MaxValue, ShiftOperatorsHelper<BigInteger, BigInteger>.op_UnsignedRightShift(NegativeOne, 1));

            Assert.Equal((BigInteger)0x4000000000000000, ShiftOperatorsHelper<BigInteger, BigInteger>.op_UnsignedRightShift(Int64MaxValuePlusOne, 1));
            Assert.Equal(Int64MaxValue, ShiftOperatorsHelper<BigInteger, BigInteger>.op_UnsignedRightShift(UInt64MaxValue, 1));
        }

        //
        // ISignedNumber
        //

        [Fact]
        public static void NegativeOneTest()
        {
            Assert.Equal(NegativeOne, SignedNumberHelper<BigInteger>.NegativeOne);
        }

        //
        // ISubtractionOperators
        //

        [Fact]
        public static void op_SubtractionTest()
        {
            Assert.Equal(NegativeOne, SubtractionOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_Subtraction(Zero, 1));
            Assert.Equal(Zero, SubtractionOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_Subtraction(One, 1));
            Assert.Equal(Int64MaxValueMinusOne, SubtractionOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_Subtraction(Int64MaxValue, 1));

            Assert.Equal(Int64MinValueMinusOne, SubtractionOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_Subtraction(Int64MinValue, 1));
            Assert.Equal(NegativeTwo, SubtractionOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_Subtraction(NegativeOne, 1));

            Assert.Equal(Int64MaxValue, SubtractionOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_Subtraction(Int64MaxValuePlusOne, 1));
            Assert.Equal(UInt64MaxValueMinusOne, SubtractionOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_Subtraction(UInt64MaxValue, 1));
        }

        [Fact]
        public static void op_CheckedSubtractionTest()
        {
            Assert.Equal(NegativeOne, SubtractionOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_CheckedSubtraction(Zero, 1));
            Assert.Equal(Zero, SubtractionOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_CheckedSubtraction(One, 1));
            Assert.Equal(Int64MaxValueMinusOne, SubtractionOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_CheckedSubtraction(Int64MaxValue, 1));

            Assert.Equal(Int64MinValueMinusOne, SubtractionOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_CheckedSubtraction(Int64MinValue, 1));
            Assert.Equal(NegativeTwo, SubtractionOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_CheckedSubtraction(NegativeOne, 1));

            Assert.Equal(Int64MaxValue, SubtractionOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_CheckedSubtraction(Int64MaxValuePlusOne, 1));
            Assert.Equal(UInt64MaxValueMinusOne, SubtractionOperatorsHelper<BigInteger, BigInteger, BigInteger>.op_CheckedSubtraction(UInt64MaxValue, 1));
        }

        //
        // IUnaryNegationOperators
        //

        [Fact]
        public static void op_UnaryNegationTest()
        {
            Assert.Equal(Zero, UnaryNegationOperatorsHelper<BigInteger, BigInteger>.op_UnaryNegation(Zero));
            Assert.Equal(NegativeOne, UnaryNegationOperatorsHelper<BigInteger, BigInteger>.op_UnaryNegation(One));
            Assert.Equal(Int64MinValuePlusOne, UnaryNegationOperatorsHelper<BigInteger, BigInteger>.op_UnaryNegation(Int64MaxValue));

            Assert.Equal(Int64MaxValuePlusOne, UnaryNegationOperatorsHelper<BigInteger, BigInteger>.op_UnaryNegation(Int64MinValue));
            Assert.Equal(One, UnaryNegationOperatorsHelper<BigInteger, BigInteger>.op_UnaryNegation(NegativeOne));

            Assert.Equal(Int64MinValue, UnaryNegationOperatorsHelper<BigInteger, BigInteger>.op_UnaryNegation(Int64MaxValuePlusOne));
            Assert.Equal(NegativeTwoPow64PlusOne, UnaryNegationOperatorsHelper<BigInteger, BigInteger>.op_UnaryNegation(UInt64MaxValue));
        }

        [Fact]
        public static void op_CheckedUnaryNegationTest()
        {
            Assert.Equal(Zero, UnaryNegationOperatorsHelper<BigInteger, BigInteger>.op_CheckedUnaryNegation(Zero));
            Assert.Equal(NegativeOne, UnaryNegationOperatorsHelper<BigInteger, BigInteger>.op_CheckedUnaryNegation(One));
            Assert.Equal(Int64MinValuePlusOne, UnaryNegationOperatorsHelper<BigInteger, BigInteger>.op_CheckedUnaryNegation(Int64MaxValue));

            Assert.Equal(Int64MaxValuePlusOne, UnaryNegationOperatorsHelper<BigInteger, BigInteger>.op_CheckedUnaryNegation(Int64MinValue));
            Assert.Equal(One, UnaryNegationOperatorsHelper<BigInteger, BigInteger>.op_CheckedUnaryNegation(NegativeOne));

            Assert.Equal(Int64MinValue, UnaryNegationOperatorsHelper<BigInteger, BigInteger>.op_CheckedUnaryNegation(Int64MaxValuePlusOne));
            Assert.Equal(NegativeTwoPow64PlusOne, UnaryNegationOperatorsHelper<BigInteger, BigInteger>.op_CheckedUnaryNegation(UInt64MaxValue));
        }

        //
        // IUnaryPlusOperators
        //

        [Fact]
        public static void op_UnaryPlusTest()
        {
            Assert.Equal(Zero, UnaryPlusOperatorsHelper<BigInteger, BigInteger>.op_UnaryPlus(Zero));
            Assert.Equal(One, UnaryPlusOperatorsHelper<BigInteger, BigInteger>.op_UnaryPlus(One));
            Assert.Equal(Int64MaxValue, UnaryPlusOperatorsHelper<BigInteger, BigInteger>.op_UnaryPlus(Int64MaxValue));

            Assert.Equal(Int64MinValue, UnaryPlusOperatorsHelper<BigInteger, BigInteger>.op_UnaryPlus(Int64MinValue));
            Assert.Equal(NegativeOne, UnaryPlusOperatorsHelper<BigInteger, BigInteger>.op_UnaryPlus(NegativeOne));

            Assert.Equal(Int64MaxValuePlusOne, UnaryPlusOperatorsHelper<BigInteger, BigInteger>.op_UnaryPlus(Int64MaxValuePlusOne));
            Assert.Equal(UInt64MaxValue, UnaryPlusOperatorsHelper<BigInteger, BigInteger>.op_UnaryPlus(UInt64MaxValue));
        }
    }
}
