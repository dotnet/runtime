// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Runtime.InteropServices;
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
        public static void RadixTest()
        {
            Assert.Equal(2, NumberBaseHelper<BigInteger>.Radix);
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
        public static void CreateCheckedFromDecimalTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateChecked<decimal>(decimal.Zero));

            Assert.Equal(One, NumberBaseHelper<BigInteger>.CreateChecked<decimal>(decimal.One));
            Assert.Equal(NegativeOne, NumberBaseHelper<BigInteger>.CreateChecked<decimal>(decimal.MinusOne));

            Assert.Equal((BigInteger)(new Int128(0x0000_0000_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)), NumberBaseHelper<BigInteger>.CreateChecked<decimal>(decimal.MaxValue));
            Assert.Equal((BigInteger)(new Int128(0xFFFF_FFFF_0000_0000, 0x0000_0000_0000_0001)), NumberBaseHelper<BigInteger>.CreateChecked<decimal>(decimal.MinValue));
        }

        [Fact]
        public static void CreateCheckedFromDoubleTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateChecked<double>(+0.0));
            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateChecked<double>(-0.0));

            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateChecked<double>(+double.Epsilon));
            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateChecked<double>(-double.Epsilon));

            Assert.Equal(One, NumberBaseHelper<BigInteger>.CreateChecked<double>(+1.0));
            Assert.Equal(NegativeOne, NumberBaseHelper<BigInteger>.CreateChecked<double>(-1.0));

            Assert.Equal((BigInteger)(new Int128(0x7FFF_FFFF_FFFF_FC00, 0x0000_0000_0000_0000)), NumberBaseHelper<BigInteger>.CreateChecked<double>(+170141183460469212842221372237303250944.0));
            Assert.Equal((BigInteger)(new Int128(0x8000_0000_0000_0400, 0x0000_0000_0000_0000)), NumberBaseHelper<BigInteger>.CreateChecked<double>(-170141183460469212842221372237303250944.0));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<BigInteger>.CreateChecked<double>(double.PositiveInfinity));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<BigInteger>.CreateChecked<double>(double.NegativeInfinity));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<BigInteger>.CreateChecked<double>(double.NaN));
        }

        [Fact]
        public static void CreateCheckedFromHalfTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateChecked<Half>((Half)(+0.0)));
            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateChecked<Half>((Half)(-0.0)));

            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateChecked<Half>(+Half.Epsilon));
            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateChecked<Half>(-Half.Epsilon));

            Assert.Equal(One, NumberBaseHelper<BigInteger>.CreateChecked<Half>((Half)(+1.0)));
            Assert.Equal(NegativeOne, NumberBaseHelper<BigInteger>.CreateChecked<Half>((Half)(-1.0)));

            Assert.Equal(+65504, NumberBaseHelper<BigInteger>.CreateChecked<Half>(Half.MaxValue));
            Assert.Equal(-65504, NumberBaseHelper<BigInteger>.CreateChecked<Half>(Half.MinValue));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<BigInteger>.CreateChecked<Half>(Half.PositiveInfinity));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<BigInteger>.CreateChecked<Half>(Half.NegativeInfinity));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<BigInteger>.CreateChecked<Half>(Half.NaN));
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
        public static void CreateCheckedFromInt128Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateChecked<Int128>(Int128.Zero));
            Assert.Equal(One, NumberBaseHelper<Int128>.CreateChecked<Int128>(Int128.One));
            Assert.Equal(Int128MaxValue, NumberBaseHelper<Int128>.CreateChecked<Int128>(Int128.MaxValue));
            Assert.Equal(Int128MinValue, NumberBaseHelper<Int128>.CreateChecked<Int128>(Int128.MinValue));
            Assert.Equal(NegativeOne, NumberBaseHelper<Int128>.CreateChecked<Int128>(Int128.NegativeOne));
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
        public static void CreateCheckedFromSingleTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateChecked<float>(+0.0f));
            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateChecked<float>(-0.0f));

            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateChecked<float>(+float.Epsilon));
            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateChecked<float>(-float.Epsilon));

            Assert.Equal(One, NumberBaseHelper<BigInteger>.CreateChecked<float>(+1.0f));
            Assert.Equal(NegativeOne, NumberBaseHelper<BigInteger>.CreateChecked<float>(-1.0f));

            Assert.Equal((BigInteger)(new Int128(0x7FFF_FF80_0000_0000, 0x0000_0000_0000_0000)), NumberBaseHelper<BigInteger>.CreateChecked<float>(+170141173319264429905852091742258462720.0f));
            Assert.Equal((BigInteger)(new Int128(0x8000_0080_0000_0000, 0x0000_0000_0000_0000)), NumberBaseHelper<BigInteger>.CreateChecked<float>(-170141173319264429905852091742258462720.0f));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<BigInteger>.CreateChecked<float>(float.PositiveInfinity));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<BigInteger>.CreateChecked<float>(float.NegativeInfinity));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<BigInteger>.CreateChecked<float>(float.NaN));
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
        public static void CreateCheckedFromUInt128Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateChecked<UInt128>(UInt128.Zero));
            Assert.Equal(One, NumberBaseHelper<BigInteger>.CreateChecked<UInt128>(UInt128.One));
            Assert.Equal(Int128MaxValue, NumberBaseHelper<BigInteger>.CreateChecked<UInt128>(new UInt128(0x7FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)));
            Assert.Equal(Int128MaxValuePlusOne, NumberBaseHelper<BigInteger>.CreateChecked<UInt128>(new UInt128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000)));
            Assert.Equal(UInt128MaxValue, NumberBaseHelper<BigInteger>.CreateChecked<UInt128>(UInt128.MaxValue));
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
        public static void CreateSaturatingFromDecimalTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateSaturating<decimal>(decimal.Zero));

            Assert.Equal(One, NumberBaseHelper<BigInteger>.CreateSaturating<decimal>(decimal.One));
            Assert.Equal(NegativeOne, NumberBaseHelper<BigInteger>.CreateSaturating<decimal>(decimal.MinusOne));

            Assert.Equal((BigInteger)(new Int128(0x0000_0000_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)), NumberBaseHelper<BigInteger>.CreateSaturating<decimal>(decimal.MaxValue));
            Assert.Equal((BigInteger)(new Int128(0xFFFF_FFFF_0000_0000, 0x0000_0000_0000_0001)), NumberBaseHelper<BigInteger>.CreateSaturating<decimal>(decimal.MinValue));
        }

        [Fact]
        public static void CreateSaturatingFromDoubleTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateSaturating<double>(+0.0));
            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateSaturating<double>(-0.0));

            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateSaturating<double>(+double.Epsilon));
            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateSaturating<double>(-double.Epsilon));

            Assert.Equal(One, NumberBaseHelper<BigInteger>.CreateSaturating<double>(+1.0));
            Assert.Equal(NegativeOne, NumberBaseHelper<BigInteger>.CreateSaturating<double>(-1.0));

            Assert.Equal((BigInteger)(new Int128(0x7FFF_FFFF_FFFF_FC00, 0x0000_0000_0000_0000)), NumberBaseHelper<BigInteger>.CreateSaturating<double>(+170141183460469212842221372237303250944.0));
            Assert.Equal((BigInteger)(new Int128(0x8000_0000_0000_0400, 0x0000_0000_0000_0000)), NumberBaseHelper<BigInteger>.CreateSaturating<double>(-170141183460469212842221372237303250944.0));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<BigInteger>.CreateSaturating<double>(double.PositiveInfinity));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<BigInteger>.CreateSaturating<double>(double.NegativeInfinity));

            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateSaturating<double>(double.NaN));
        }

        [Fact]
        public static void CreateSaturatingFromHalfTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateSaturating<Half>((Half)(+0.0)));
            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateSaturating<Half>((Half)(-0.0)));

            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateSaturating<Half>(+Half.Epsilon));
            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateSaturating<Half>(-Half.Epsilon));

            Assert.Equal(One, NumberBaseHelper<BigInteger>.CreateSaturating<Half>((Half)(+1.0)));
            Assert.Equal(NegativeOne, NumberBaseHelper<BigInteger>.CreateSaturating<Half>((Half)(-1.0)));

            Assert.Equal(+65504, NumberBaseHelper<BigInteger>.CreateSaturating<Half>(Half.MaxValue));
            Assert.Equal(-65504, NumberBaseHelper<BigInteger>.CreateSaturating<Half>(Half.MinValue));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<BigInteger>.CreateSaturating<Half>(Half.PositiveInfinity));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<BigInteger>.CreateSaturating<Half>(Half.NegativeInfinity));

            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateSaturating<Half>(Half.NaN));
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
        public static void CreateSaturatingFromInt128Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateSaturating<Int128>(Int128.Zero));
            Assert.Equal(One, NumberBaseHelper<Int128>.CreateSaturating<Int128>(Int128.One));
            Assert.Equal(Int128MaxValue, NumberBaseHelper<Int128>.CreateSaturating<Int128>(Int128.MaxValue));
            Assert.Equal(Int128MinValue, NumberBaseHelper<Int128>.CreateSaturating<Int128>(Int128.MinValue));
            Assert.Equal(NegativeOne, NumberBaseHelper<Int128>.CreateSaturating<Int128>(Int128.NegativeOne));
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
        public static void CreateSaturatingFromSingleTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateSaturating<float>(+0.0f));
            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateSaturating<float>(-0.0f));

            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateSaturating<float>(+float.Epsilon));
            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateSaturating<float>(-float.Epsilon));

            Assert.Equal(One, NumberBaseHelper<BigInteger>.CreateSaturating<float>(+1.0f));
            Assert.Equal(NegativeOne, NumberBaseHelper<BigInteger>.CreateSaturating<float>(-1.0f));

            Assert.Equal((BigInteger)(new Int128(0x7FFF_FF80_0000_0000, 0x0000_0000_0000_0000)), NumberBaseHelper<BigInteger>.CreateSaturating<float>(+170141173319264429905852091742258462720.0f));
            Assert.Equal((BigInteger)(new Int128(0x8000_0080_0000_0000, 0x0000_0000_0000_0000)), NumberBaseHelper<BigInteger>.CreateSaturating<float>(-170141173319264429905852091742258462720.0f));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<BigInteger>.CreateSaturating<float>(float.PositiveInfinity));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<BigInteger>.CreateSaturating<float>(float.NegativeInfinity));

            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateSaturating<float>(float.NaN));
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
        public static void CreateSaturatingFromUInt128Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateSaturating<UInt128>(UInt128.Zero));
            Assert.Equal(One, NumberBaseHelper<BigInteger>.CreateSaturating<UInt128>(UInt128.One));
            Assert.Equal(Int128MaxValue, NumberBaseHelper<BigInteger>.CreateSaturating<UInt128>(new UInt128(0x7FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)));
            Assert.Equal(Int128MaxValuePlusOne, NumberBaseHelper<BigInteger>.CreateSaturating<UInt128>(new UInt128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000)));
            Assert.Equal(UInt128MaxValue, NumberBaseHelper<BigInteger>.CreateSaturating<UInt128>(UInt128.MaxValue));
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
        public static void CreateTruncatingFromDecimalTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateTruncating<decimal>(decimal.Zero));

            Assert.Equal(One, NumberBaseHelper<BigInteger>.CreateTruncating<decimal>(decimal.One));
            Assert.Equal(NegativeOne, NumberBaseHelper<BigInteger>.CreateTruncating<decimal>(decimal.MinusOne));

            Assert.Equal((BigInteger)(new Int128(0x0000_0000_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)), NumberBaseHelper<BigInteger>.CreateTruncating<decimal>(decimal.MaxValue));
            Assert.Equal((BigInteger)(new Int128(0xFFFF_FFFF_0000_0000, 0x0000_0000_0000_0001)), NumberBaseHelper<BigInteger>.CreateTruncating<decimal>(decimal.MinValue));
        }

        [Fact]
        public static void CreateTruncatingFromDoubleTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateTruncating<double>(+0.0));
            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateTruncating<double>(-0.0));

            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateTruncating<double>(+double.Epsilon));
            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateTruncating<double>(-double.Epsilon));

            Assert.Equal(One, NumberBaseHelper<BigInteger>.CreateTruncating<double>(+1.0));
            Assert.Equal(NegativeOne, NumberBaseHelper<BigInteger>.CreateTruncating<double>(-1.0));

            Assert.Equal((BigInteger)(new Int128(0x7FFF_FFFF_FFFF_FC00, 0x0000_0000_0000_0000)), NumberBaseHelper<BigInteger>.CreateTruncating<double>(+170141183460469212842221372237303250944.0));
            Assert.Equal((BigInteger)(new Int128(0x8000_0000_0000_0400, 0x0000_0000_0000_0000)), NumberBaseHelper<BigInteger>.CreateTruncating<double>(-170141183460469212842221372237303250944.0));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<BigInteger>.CreateTruncating<double>(double.PositiveInfinity));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<BigInteger>.CreateTruncating<double>(double.NegativeInfinity));

            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateTruncating<double>(double.NaN));
        }

        [Fact]
        public static void CreateTruncatingFromHalfTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateTruncating<Half>((Half)(+0.0)));
            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateTruncating<Half>((Half)(-0.0)));

            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateTruncating<Half>(+Half.Epsilon));
            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateTruncating<Half>(-Half.Epsilon));

            Assert.Equal(One, NumberBaseHelper<BigInteger>.CreateTruncating<Half>((Half)(+1.0)));
            Assert.Equal(NegativeOne, NumberBaseHelper<BigInteger>.CreateTruncating<Half>((Half)(-1.0)));

            Assert.Equal(+65504, NumberBaseHelper<BigInteger>.CreateTruncating<Half>(Half.MaxValue));
            Assert.Equal(-65504, NumberBaseHelper<BigInteger>.CreateTruncating<Half>(Half.MinValue));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<BigInteger>.CreateTruncating<Half>(Half.PositiveInfinity));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<BigInteger>.CreateTruncating<Half>(Half.NegativeInfinity));

            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateTruncating<Half>(Half.NaN));
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
        public static void CreateTruncatingFromInt128Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateTruncating<Int128>(Int128.Zero));
            Assert.Equal(One, NumberBaseHelper<Int128>.CreateTruncating<Int128>(Int128.One));
            Assert.Equal(Int128MaxValue, NumberBaseHelper<Int128>.CreateTruncating<Int128>(Int128.MaxValue));
            Assert.Equal(Int128MinValue, NumberBaseHelper<Int128>.CreateTruncating<Int128>(Int128.MinValue));
            Assert.Equal(NegativeOne, NumberBaseHelper<Int128>.CreateTruncating<Int128>(Int128.NegativeOne));
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
        public static void CreateTruncatingFromSingleTest()
        {
            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateTruncating<float>(+0.0f));
            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateTruncating<float>(-0.0f));

            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateTruncating<float>(+float.Epsilon));
            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateTruncating<float>(-float.Epsilon));

            Assert.Equal(One, NumberBaseHelper<BigInteger>.CreateTruncating<float>(+1.0f));
            Assert.Equal(NegativeOne, NumberBaseHelper<BigInteger>.CreateTruncating<float>(-1.0f));

            Assert.Equal((BigInteger)(new Int128(0x7FFF_FF80_0000_0000, 0x0000_0000_0000_0000)), NumberBaseHelper<BigInteger>.CreateTruncating<float>(+170141173319264429905852091742258462720.0f));
            Assert.Equal((BigInteger)(new Int128(0x8000_0080_0000_0000, 0x0000_0000_0000_0000)), NumberBaseHelper<BigInteger>.CreateTruncating<float>(-170141173319264429905852091742258462720.0f));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<BigInteger>.CreateTruncating<float>(float.PositiveInfinity));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<BigInteger>.CreateTruncating<float>(float.NegativeInfinity));

            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateTruncating<float>(float.NaN));
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
        public static void CreateTruncatingFromUInt128Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<BigInteger>.CreateTruncating<UInt128>(UInt128.Zero));
            Assert.Equal(One, NumberBaseHelper<BigInteger>.CreateTruncating<UInt128>(UInt128.One));
            Assert.Equal(Int128MaxValue, NumberBaseHelper<BigInteger>.CreateTruncating<UInt128>(new UInt128(0x7FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)));
            Assert.Equal(Int128MaxValuePlusOne, NumberBaseHelper<BigInteger>.CreateTruncating<UInt128>(new UInt128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000)));
            Assert.Equal(UInt128MaxValue, NumberBaseHelper<BigInteger>.CreateTruncating<UInt128>(UInt128.MaxValue));
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
        public static void IsCanonicalTest()
        {
            Assert.True(NumberBaseHelper<BigInteger>.IsCanonical(Zero));
            Assert.True(NumberBaseHelper<BigInteger>.IsCanonical(One));
            Assert.True(NumberBaseHelper<BigInteger>.IsCanonical(Int64MaxValue));

            Assert.True(NumberBaseHelper<BigInteger>.IsCanonical(Int64MinValue));
            Assert.True(NumberBaseHelper<BigInteger>.IsCanonical(NegativeOne));

            Assert.True(NumberBaseHelper<BigInteger>.IsCanonical(Int64MaxValuePlusOne));
            Assert.True(NumberBaseHelper<BigInteger>.IsCanonical(UInt64MaxValue));
        }

        [Fact]
        public static void IsComplexNumberTest()
        {
            Assert.False(NumberBaseHelper<BigInteger>.IsComplexNumber(Zero));
            Assert.False(NumberBaseHelper<BigInteger>.IsComplexNumber(One));
            Assert.False(NumberBaseHelper<BigInteger>.IsComplexNumber(Int64MaxValue));

            Assert.False(NumberBaseHelper<BigInteger>.IsComplexNumber(Int64MinValue));
            Assert.False(NumberBaseHelper<BigInteger>.IsComplexNumber(NegativeOne));

            Assert.False(NumberBaseHelper<BigInteger>.IsComplexNumber(Int64MaxValuePlusOne));
            Assert.False(NumberBaseHelper<BigInteger>.IsComplexNumber(UInt64MaxValue));
        }

        [Fact]
        public static void IsEvenIntegerTest()
        {
            Assert.True(NumberBaseHelper<BigInteger>.IsEvenInteger(Zero));
            Assert.False(NumberBaseHelper<BigInteger>.IsEvenInteger(One));
            Assert.False(NumberBaseHelper<BigInteger>.IsEvenInteger(Int64MaxValue));

            Assert.True(NumberBaseHelper<BigInteger>.IsEvenInteger(Int64MinValue));
            Assert.False(NumberBaseHelper<BigInteger>.IsEvenInteger(NegativeOne));

            Assert.True(NumberBaseHelper<BigInteger>.IsEvenInteger(Int64MaxValuePlusOne));
            Assert.False(NumberBaseHelper<BigInteger>.IsEvenInteger(UInt64MaxValue));
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
        public static void IsImaginaryNumberTest()
        {
            Assert.False(NumberBaseHelper<BigInteger>.IsImaginaryNumber(Zero));
            Assert.False(NumberBaseHelper<BigInteger>.IsImaginaryNumber(One));
            Assert.False(NumberBaseHelper<BigInteger>.IsImaginaryNumber(Int64MaxValue));

            Assert.False(NumberBaseHelper<BigInteger>.IsImaginaryNumber(Int64MinValue));
            Assert.False(NumberBaseHelper<BigInteger>.IsImaginaryNumber(NegativeOne));

            Assert.False(NumberBaseHelper<BigInteger>.IsImaginaryNumber(Int64MaxValuePlusOne));
            Assert.False(NumberBaseHelper<BigInteger>.IsImaginaryNumber(UInt64MaxValue));
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
        public static void IsIntegerTest()
        {
            Assert.True(NumberBaseHelper<BigInteger>.IsInteger(Zero));
            Assert.True(NumberBaseHelper<BigInteger>.IsInteger(One));
            Assert.True(NumberBaseHelper<BigInteger>.IsInteger(Int64MaxValue));

            Assert.True(NumberBaseHelper<BigInteger>.IsInteger(Int64MinValue));
            Assert.True(NumberBaseHelper<BigInteger>.IsInteger(NegativeOne));

            Assert.True(NumberBaseHelper<BigInteger>.IsInteger(Int64MaxValuePlusOne));
            Assert.True(NumberBaseHelper<BigInteger>.IsInteger(UInt64MaxValue));
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
        public static void IsOddIntegerTest()
        {
            Assert.False(NumberBaseHelper<BigInteger>.IsOddInteger(Zero));
            Assert.True(NumberBaseHelper<BigInteger>.IsOddInteger(One));
            Assert.True(NumberBaseHelper<BigInteger>.IsOddInteger(Int64MaxValue));

            Assert.False(NumberBaseHelper<BigInteger>.IsOddInteger(Int64MinValue));
            Assert.True(NumberBaseHelper<BigInteger>.IsOddInteger(NegativeOne));

            Assert.False(NumberBaseHelper<BigInteger>.IsOddInteger(Int64MaxValuePlusOne));
            Assert.True(NumberBaseHelper<BigInteger>.IsOddInteger(UInt64MaxValue));
        }

        [Fact]
        public static void IsPositiveTest()
        {
            Assert.True(NumberBaseHelper<BigInteger>.IsPositive(Zero));
            Assert.True(NumberBaseHelper<BigInteger>.IsPositive(One));
            Assert.True(NumberBaseHelper<BigInteger>.IsPositive(Int64MaxValue));

            Assert.False(NumberBaseHelper<BigInteger>.IsPositive(Int64MinValue));
            Assert.False(NumberBaseHelper<BigInteger>.IsPositive(NegativeOne));

            Assert.True(NumberBaseHelper<BigInteger>.IsPositive(Int64MaxValuePlusOne));
            Assert.True(NumberBaseHelper<BigInteger>.IsPositive(UInt64MaxValue));
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
        public static void IsRealNumberTest()
        {
            Assert.True(NumberBaseHelper<BigInteger>.IsRealNumber(Zero));
            Assert.True(NumberBaseHelper<BigInteger>.IsRealNumber(One));
            Assert.True(NumberBaseHelper<BigInteger>.IsRealNumber(Int64MaxValue));

            Assert.True(NumberBaseHelper<BigInteger>.IsRealNumber(Int64MinValue));
            Assert.True(NumberBaseHelper<BigInteger>.IsRealNumber(NegativeOne));

            Assert.True(NumberBaseHelper<BigInteger>.IsRealNumber(Int64MaxValuePlusOne));
            Assert.True(NumberBaseHelper<BigInteger>.IsRealNumber(UInt64MaxValue));
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
        public static void IsZeroTest()
        {
            Assert.True(NumberBaseHelper<BigInteger>.IsZero(Zero));
            Assert.False(NumberBaseHelper<BigInteger>.IsZero(One));
            Assert.False(NumberBaseHelper<BigInteger>.IsZero(Int64MaxValue));

            Assert.False(NumberBaseHelper<BigInteger>.IsZero(Int64MinValue));
            Assert.False(NumberBaseHelper<BigInteger>.IsZero(NegativeOne));

            Assert.False(NumberBaseHelper<BigInteger>.IsZero(Int64MaxValuePlusOne));
            Assert.False(NumberBaseHelper<BigInteger>.IsZero(UInt64MaxValue));
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

        //
        // INumberBase.TryConvertTo
        //

        [Fact]
        public static void TryConvertToCheckedByteTest()
        {
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateChecked<BigInteger>(Zero));
            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.CreateChecked<BigInteger>(One));
            Assert.Equal((byte)0x7F, NumberBaseHelper<byte>.CreateChecked<BigInteger>(SByteMaxValue));
            Assert.Equal((byte)0x80, NumberBaseHelper<byte>.CreateChecked<BigInteger>(SByteMaxValuePlusOne));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateChecked<BigInteger>(ByteMaxValue));
        }

        [Fact]
        public static void TryConvertToCheckedCharTest()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateChecked<BigInteger>(Zero));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateChecked<BigInteger>(One));
            Assert.Equal((char)0x7FFF, NumberBaseHelper<char>.CreateChecked<BigInteger>(Int16MaxValue));
            Assert.Equal((char)0x8000, NumberBaseHelper<char>.CreateChecked<BigInteger>(Int16MaxValuePlusOne));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateChecked<BigInteger>(UInt16MaxValue));
        }

        [Fact]
        public static void TryConvertToCheckedDecimalTest()
        {
            Assert.Equal(decimal.Zero, NumberBaseHelper<decimal>.CreateChecked<BigInteger>(Zero));

            Assert.Equal(decimal.One, NumberBaseHelper<decimal>.CreateChecked<BigInteger>(One));
            Assert.Equal(decimal.MinusOne, NumberBaseHelper<decimal>.CreateChecked<BigInteger>(NegativeOne));

            Assert.Equal(decimal.MaxValue, NumberBaseHelper<decimal>.CreateChecked<BigInteger>((BigInteger)(new Int128(0x0000_0000_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF))));
            Assert.Equal(decimal.MinValue, NumberBaseHelper<decimal>.CreateChecked<BigInteger>((BigInteger)(new Int128(0xFFFF_FFFF_0000_0000, 0x0000_0000_0000_0001))));
        }

        [Fact]
        public static void TryConvertToCheckedDoubleTest()
        {
            Assert.Equal(+0.0, NumberBaseHelper<double>.CreateChecked<BigInteger>(Zero));

            Assert.Equal(+1.0, NumberBaseHelper<double>.CreateChecked<BigInteger>(One));
            Assert.Equal(-1.0, NumberBaseHelper<double>.CreateChecked<BigInteger>(NegativeOne));

            Assert.Equal(+170141183460469212842221372237303250944.0, NumberBaseHelper<double>.CreateChecked<BigInteger>((BigInteger)(new Int128(0x7FFF_FFFF_FFFF_FC00, 0x0000_0000_0000_0000))));
            Assert.Equal(-170141183460469212842221372237303250944.0, NumberBaseHelper<double>.CreateChecked<BigInteger>((BigInteger)(new Int128(0x8000_0000_0000_0400, 0x0000_0000_0000_0000))));
        }

        [Fact]
        public static void TryConvertToCheckedHalfTest()
        {
            Assert.Equal((Half)(+0.0), NumberBaseHelper<Half>.CreateChecked<BigInteger>(Zero));

            Assert.Equal((Half)(+1.0), NumberBaseHelper<Half>.CreateChecked<BigInteger>(One));
            Assert.Equal((Half)(-1.0), NumberBaseHelper<Half>.CreateChecked<BigInteger>(NegativeOne));

            Assert.Equal(Half.MaxValue, NumberBaseHelper<Half>.CreateChecked<BigInteger>(+65504));
            Assert.Equal(Half.MinValue, NumberBaseHelper<Half>.CreateChecked<BigInteger>(-65504));
        }

        [Fact]
        public static void TryConvertToCheckedInt16Test()
        {
            Assert.Equal(0x0000, NumberBaseHelper<short>.CreateChecked<BigInteger>(Zero));
            Assert.Equal(0x0001, NumberBaseHelper<short>.CreateChecked<BigInteger>(One));
            Assert.Equal(0x7FFF, NumberBaseHelper<short>.CreateChecked<BigInteger>(Int16MaxValue));
            Assert.Equal(unchecked((short)0x8000), NumberBaseHelper<short>.CreateChecked<BigInteger>(Int16MinValue));
            Assert.Equal(unchecked((short)0xFFFF), NumberBaseHelper<short>.CreateChecked<BigInteger>(NegativeOne));
        }

        [Fact]
        public static void TryConvertToCheckedInt32Test()
        {
            Assert.Equal(0x0000_0000, NumberBaseHelper<int>.CreateChecked<BigInteger>(Zero));
            Assert.Equal(0x0000_0001, NumberBaseHelper<int>.CreateChecked<BigInteger>(One));
            Assert.Equal(0x7FFF_FFFF, NumberBaseHelper<int>.CreateChecked<BigInteger>(Int32MaxValue));
            Assert.Equal(unchecked((int)0x8000_0000), NumberBaseHelper<int>.CreateChecked<BigInteger>(Int32MinValue));
            Assert.Equal(unchecked((int)0xFFFF_FFFF), NumberBaseHelper<int>.CreateChecked<BigInteger>(NegativeOne));
        }

        [Fact]
        public static void TryConvertToCheckedInt64Test()
        {
            Assert.Equal(0x0000_0000_0000_0000, NumberBaseHelper<long>.CreateChecked<BigInteger>(Zero));
            Assert.Equal(0x0000_0000_0000_0001, NumberBaseHelper<long>.CreateChecked<BigInteger>(One));
            Assert.Equal(0x7FFF_FFFF_FFFF_FFFF, NumberBaseHelper<long>.CreateChecked<BigInteger>(Int64MaxValue));
            Assert.Equal(unchecked(unchecked((long)0x8000_0000_0000_0000)), NumberBaseHelper<long>.CreateChecked<BigInteger>(Int64MinValue));
            Assert.Equal(unchecked(unchecked((long)0xFFFF_FFFF_FFFF_FFFF)), NumberBaseHelper<long>.CreateChecked<BigInteger>(NegativeOne));
        }

        [Fact]
        public static void TryConvertToCheckedInt128Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateChecked<Int128>(Int128.Zero));
            Assert.Equal(One, NumberBaseHelper<Int128>.CreateChecked<Int128>(Int128.One));
            Assert.Equal(Int128MaxValue, NumberBaseHelper<Int128>.CreateChecked<Int128>(Int128.MaxValue));
            Assert.Equal(Int128MinValue, NumberBaseHelper<Int128>.CreateChecked<Int128>(Int128.MinValue));
            Assert.Equal(NegativeOne, NumberBaseHelper<Int128>.CreateChecked<Int128>(Int128.NegativeOne));
        }

        [Fact]
        public static void TryConvertToCheckedIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0x0000_0000_0000_0000), NumberBaseHelper<nint>.CreateChecked<BigInteger>(Zero));
                Assert.Equal(unchecked((nint)0x0000_0000_0000_0001), NumberBaseHelper<nint>.CreateChecked<BigInteger>(One));
                Assert.Equal(unchecked((nint)0x7FFF_FFFF_FFFF_FFFF), NumberBaseHelper<nint>.CreateChecked<BigInteger>(Int64MaxValue));
                Assert.Equal(unchecked((nint)0x8000_0000_0000_0000), NumberBaseHelper<nint>.CreateChecked<BigInteger>(Int64MinValue));
                Assert.Equal(unchecked((nint)0xFFFF_FFFF_FFFF_FFFF), NumberBaseHelper<nint>.CreateChecked<BigInteger>(NegativeOne));
            }
            else
            {
                Assert.Equal((nint)0x0000_0000, NumberBaseHelper<nint>.CreateChecked<BigInteger>(Zero));
                Assert.Equal((nint)0x0000_0001, NumberBaseHelper<nint>.CreateChecked<BigInteger>(One));
                Assert.Equal((nint)0x7FFF_FFFF, NumberBaseHelper<nint>.CreateChecked<BigInteger>(Int32MaxValue));
                Assert.Equal(unchecked((nint)0x8000_0000), NumberBaseHelper<nint>.CreateChecked<BigInteger>(Int32MinValue));
                Assert.Equal(unchecked((nint)0xFFFF_FFFF), NumberBaseHelper<nint>.CreateChecked<BigInteger>(NegativeOne));
            }
        }

        [Fact]
        public static void TryConvertToCheckedSByteTest()
        {
            Assert.Equal(0x00, NumberBaseHelper<sbyte>.CreateChecked<BigInteger>(Zero));
            Assert.Equal(0x01, NumberBaseHelper<sbyte>.CreateChecked<BigInteger>(One));
            Assert.Equal(0x7F, NumberBaseHelper<sbyte>.CreateChecked<BigInteger>(SByteMaxValue));
            Assert.Equal(unchecked((sbyte)0x80), NumberBaseHelper<sbyte>.CreateChecked<BigInteger>(SByteMinValue));
            Assert.Equal(unchecked((sbyte)0xFF), NumberBaseHelper<sbyte>.CreateChecked<BigInteger>(NegativeOne));
        }

        [Fact]
        public static void TryConvertToCheckedSingleTest()
        {
            Assert.Equal(+0.0f, NumberBaseHelper<float>.CreateChecked<BigInteger>(Zero));

            Assert.Equal(+1.0f, NumberBaseHelper<float>.CreateChecked<BigInteger>(One));
            Assert.Equal(-1.0f, NumberBaseHelper<float>.CreateChecked<BigInteger>(NegativeOne));

            Assert.Equal(+170141173319264429905852091742258462720.0f, NumberBaseHelper<float>.CreateChecked<BigInteger>((BigInteger)(new Int128(0x7FFF_FF80_0000_0000, 0x0000_0000_0000_0000))));
            Assert.Equal(-170141173319264429905852091742258462720.0f, NumberBaseHelper<float>.CreateChecked<BigInteger>((BigInteger)(new Int128(0x8000_0080_0000_0000, 0x0000_0000_0000_0000))));
        }

        [Fact]
        public static void TryConvertToCheckedUInt16Test()
        {
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateChecked<BigInteger>(Zero));
            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.CreateChecked<BigInteger>(One));
            Assert.Equal((ushort)0x7FFF, NumberBaseHelper<ushort>.CreateChecked<BigInteger>(Int16MaxValue));
            Assert.Equal((ushort)0x8000, NumberBaseHelper<ushort>.CreateChecked<BigInteger>(Int16MaxValuePlusOne));
            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateChecked<BigInteger>(UInt16MaxValue));
        }

        [Fact]
        public static void TryConvertToCheckedUInt32Test()
        {
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateChecked<BigInteger>(Zero));
            Assert.Equal((uint)0x0000_0001, NumberBaseHelper<uint>.CreateChecked<BigInteger>(One));
            Assert.Equal((uint)0x7FFF_FFFF, NumberBaseHelper<uint>.CreateChecked<BigInteger>(Int32MaxValue));
            Assert.Equal((uint)0x8000_0000, NumberBaseHelper<uint>.CreateChecked<BigInteger>(Int32MaxValuePlusOne));
            Assert.Equal((uint)0xFFFF_FFFF, NumberBaseHelper<uint>.CreateChecked<BigInteger>(UInt32MaxValue));
        }

        [Fact]
        public static void TryConvertToCheckedUInt64Test()
        {
            Assert.Equal((ulong)0x0000_0000_0000_0000, NumberBaseHelper<ulong>.CreateChecked<BigInteger>(Zero));
            Assert.Equal((ulong)0x0000_0000_0000_0001, NumberBaseHelper<ulong>.CreateChecked<BigInteger>(One));
            Assert.Equal((ulong)0x7FFF_FFFF_FFFF_FFFF, NumberBaseHelper<ulong>.CreateChecked<BigInteger>(Int64MaxValue));
            Assert.Equal((ulong)0x8000_0000_0000_0000, NumberBaseHelper<ulong>.CreateChecked<BigInteger>(Int64MaxValuePlusOne));
            Assert.Equal((ulong)0xFFFF_FFFF_FFFF_FFFF, NumberBaseHelper<ulong>.CreateChecked<BigInteger>(UInt64MaxValue));
        }

        [Fact]
        public static void TryConvertToCheckedUInt128Test()
        {
            Assert.Equal(UInt128.Zero, NumberBaseHelper<UInt128>.CreateChecked<BigInteger>(Zero));
            Assert.Equal(UInt128.One, NumberBaseHelper<UInt128>.CreateChecked<BigInteger>(One));
            Assert.Equal(new UInt128(0x7FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), NumberBaseHelper<UInt128>.CreateChecked<BigInteger>(Int128MaxValue));
            Assert.Equal(new UInt128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000), NumberBaseHelper<UInt128>.CreateChecked<BigInteger>(Int128MaxValuePlusOne));
            Assert.Equal(UInt128.MaxValue, NumberBaseHelper<UInt128>.CreateChecked<BigInteger>(UInt128MaxValue));
        }

        [Fact]
        public static void TryConvertToCheckedUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000_0000_0000_0000), NumberBaseHelper<nuint>.CreateChecked<BigInteger>(Zero));
                Assert.Equal(unchecked((nuint)0x0000_0000_0000_0001), NumberBaseHelper<nuint>.CreateChecked<BigInteger>(One));
                Assert.Equal(unchecked((nuint)0x7FFF_FFFF_FFFF_FFFF), NumberBaseHelper<nuint>.CreateChecked<BigInteger>(Int64MaxValue));
                Assert.Equal(unchecked((nuint)0x8000_0000_0000_0000), NumberBaseHelper<nuint>.CreateChecked<BigInteger>(Int64MaxValuePlusOne));
                Assert.Equal(unchecked((nuint)0xFFFF_FFFF_FFFF_FFFF), NumberBaseHelper<nuint>.CreateChecked<BigInteger>(UInt64MaxValue));
            }
            else
            {
                Assert.Equal((nuint)0x0000_0000, NumberBaseHelper<nuint>.CreateChecked<BigInteger>(Zero));
                Assert.Equal((nuint)0x0000_0001, NumberBaseHelper<nuint>.CreateChecked<BigInteger>(One));
                Assert.Equal((nuint)0x7FFF_FFFF, NumberBaseHelper<nuint>.CreateChecked<BigInteger>(Int32MaxValue));
                Assert.Equal((nuint)0x8000_0000, NumberBaseHelper<nuint>.CreateChecked<BigInteger>(Int32MaxValuePlusOne));
                Assert.Equal((nuint)0xFFFF_FFFF, NumberBaseHelper<nuint>.CreateChecked<BigInteger>(UInt32MaxValue));
            }
        }

        [Fact]
        public static void TryConvertToSaturatingByteTest()
        {
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateSaturating<BigInteger>(Zero));
            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.CreateSaturating<BigInteger>(One));
            Assert.Equal((byte)0x7F, NumberBaseHelper<byte>.CreateSaturating<BigInteger>(SByteMaxValue));
            Assert.Equal((byte)0x80, NumberBaseHelper<byte>.CreateSaturating<BigInteger>(SByteMaxValuePlusOne));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateSaturating<BigInteger>(ByteMaxValue));
        }

        [Fact]
        public static void TryConvertToSaturatingCharTest()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<BigInteger>(Zero));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateSaturating<BigInteger>(One));
            Assert.Equal((char)0x7FFF, NumberBaseHelper<char>.CreateSaturating<BigInteger>(Int16MaxValue));
            Assert.Equal((char)0x8000, NumberBaseHelper<char>.CreateSaturating<BigInteger>(Int16MaxValuePlusOne));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateSaturating<BigInteger>(UInt16MaxValue));
        }

        [Fact]
        public static void TryConvertToSaturatingDecimalTest()
        {
            Assert.Equal(decimal.Zero, NumberBaseHelper<decimal>.CreateSaturating<BigInteger>(Zero));

            Assert.Equal(decimal.One, NumberBaseHelper<decimal>.CreateSaturating<BigInteger>(One));
            Assert.Equal(decimal.MinusOne, NumberBaseHelper<decimal>.CreateSaturating<BigInteger>(NegativeOne));

            Assert.Equal(decimal.MaxValue, NumberBaseHelper<decimal>.CreateSaturating<BigInteger>((BigInteger)(new Int128(0x0000_0000_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF))));
            Assert.Equal(decimal.MinValue, NumberBaseHelper<decimal>.CreateSaturating<BigInteger>((BigInteger)(new Int128(0xFFFF_FFFF_0000_0000, 0x0000_0000_0000_0001))));
        }

        [Fact]
        public static void TryConvertToSaturatingDoubleTest()
        {
            Assert.Equal(+0.0, NumberBaseHelper<double>.CreateSaturating<BigInteger>(Zero));

            Assert.Equal(+1.0, NumberBaseHelper<double>.CreateSaturating<BigInteger>(One));
            Assert.Equal(-1.0, NumberBaseHelper<double>.CreateSaturating<BigInteger>(NegativeOne));

            Assert.Equal(+170141183460469212842221372237303250944.0, NumberBaseHelper<double>.CreateSaturating<BigInteger>((BigInteger)(new Int128(0x7FFF_FFFF_FFFF_FC00, 0x0000_0000_0000_0000))));
            Assert.Equal(-170141183460469212842221372237303250944.0, NumberBaseHelper<double>.CreateSaturating<BigInteger>((BigInteger)(new Int128(0x8000_0000_0000_0400, 0x0000_0000_0000_0000))));
        }

        [Fact]
        public static void TryConvertToSaturatingHalfTest()
        {
            Assert.Equal((Half)(+0.0), NumberBaseHelper<Half>.CreateSaturating<BigInteger>(Zero));

            Assert.Equal((Half)(+1.0), NumberBaseHelper<Half>.CreateSaturating<BigInteger>(One));
            Assert.Equal((Half)(-1.0), NumberBaseHelper<Half>.CreateSaturating<BigInteger>(NegativeOne));

            Assert.Equal(Half.MaxValue, NumberBaseHelper<Half>.CreateSaturating<BigInteger>(+65504));
            Assert.Equal(Half.MinValue, NumberBaseHelper<Half>.CreateSaturating<BigInteger>(-65504));
        }

        [Fact]
        public static void TryConvertToSaturatingInt16Test()
        {
            Assert.Equal(0x0000, NumberBaseHelper<short>.CreateSaturating<BigInteger>(Zero));
            Assert.Equal(0x0001, NumberBaseHelper<short>.CreateSaturating<BigInteger>(One));
            Assert.Equal(0x7FFF, NumberBaseHelper<short>.CreateSaturating<BigInteger>(Int16MaxValue));
            Assert.Equal(unchecked((short)0x8000), NumberBaseHelper<short>.CreateSaturating<BigInteger>(Int16MinValue));
            Assert.Equal(unchecked((short)0xFFFF), NumberBaseHelper<short>.CreateSaturating<BigInteger>(NegativeOne));
        }

        [Fact]
        public static void TryConvertToSaturatingInt32Test()
        {
            Assert.Equal(0x0000_0000, NumberBaseHelper<int>.CreateSaturating<BigInteger>(Zero));
            Assert.Equal(0x0000_0001, NumberBaseHelper<int>.CreateSaturating<BigInteger>(One));
            Assert.Equal(0x7FFF_FFFF, NumberBaseHelper<int>.CreateSaturating<BigInteger>(Int32MaxValue));
            Assert.Equal(unchecked((int)0x8000_0000), NumberBaseHelper<int>.CreateSaturating<BigInteger>(Int32MinValue));
            Assert.Equal(unchecked((int)0xFFFF_FFFF), NumberBaseHelper<int>.CreateSaturating<BigInteger>(NegativeOne));
        }

        [Fact]
        public static void TryConvertToSaturatingInt64Test()
        {
            Assert.Equal(0x0000_0000_0000_0000, NumberBaseHelper<long>.CreateSaturating<BigInteger>(Zero));
            Assert.Equal(0x0000_0000_0000_0001, NumberBaseHelper<long>.CreateSaturating<BigInteger>(One));
            Assert.Equal(0x7FFF_FFFF_FFFF_FFFF, NumberBaseHelper<long>.CreateSaturating<BigInteger>(Int64MaxValue));
            Assert.Equal(unchecked((long)0x8000_0000_0000_0000), NumberBaseHelper<long>.CreateSaturating<BigInteger>(Int64MinValue));
            Assert.Equal(unchecked((long)0xFFFF_FFFF_FFFF_FFFF), NumberBaseHelper<long>.CreateSaturating<BigInteger>(NegativeOne));
        }

        [Fact]
        public static void TryConvertToSaturatingInt128Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateSaturating<Int128>(Int128.Zero));
            Assert.Equal(One, NumberBaseHelper<Int128>.CreateSaturating<Int128>(Int128.One));
            Assert.Equal(Int128MaxValue, NumberBaseHelper<Int128>.CreateSaturating<Int128>(Int128.MaxValue));
            Assert.Equal(Int128MinValue, NumberBaseHelper<Int128>.CreateSaturating<Int128>(Int128.MinValue));
            Assert.Equal(NegativeOne, NumberBaseHelper<Int128>.CreateSaturating<Int128>(Int128.NegativeOne));
        }

        [Fact]
        public static void TryConvertToSaturatingIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0x0000_0000_0000_0000), NumberBaseHelper<nint>.CreateSaturating<BigInteger>(Zero));
                Assert.Equal(unchecked((nint)0x0000_0000_0000_0001), NumberBaseHelper<nint>.CreateSaturating<BigInteger>(One));
                Assert.Equal(unchecked((nint)0x7FFF_FFFF_FFFF_FFFF), NumberBaseHelper<nint>.CreateSaturating<BigInteger>(Int64MaxValue));
                Assert.Equal(unchecked((nint)0x8000_0000_0000_0000), NumberBaseHelper<nint>.CreateSaturating<BigInteger>(Int64MinValue));
                Assert.Equal(unchecked((nint)0xFFFF_FFFF_FFFF_FFFF), NumberBaseHelper<nint>.CreateSaturating<BigInteger>(NegativeOne));
            }
            else
            {
                Assert.Equal((nint)0x0000_0000, NumberBaseHelper<nint>.CreateSaturating<BigInteger>(Zero));
                Assert.Equal((nint)0x0000_0001, NumberBaseHelper<nint>.CreateSaturating<BigInteger>(One));
                Assert.Equal((nint)0x7FFF_FFFF, NumberBaseHelper<nint>.CreateSaturating<BigInteger>(Int32MaxValue));
                Assert.Equal(unchecked((nint)0x8000_0000), NumberBaseHelper<nint>.CreateSaturating<BigInteger>(Int32MinValue));
                Assert.Equal(unchecked((nint)0xFFFF_FFFF), NumberBaseHelper<nint>.CreateSaturating<BigInteger>(NegativeOne));
            }
        }

        [Fact]
        public static void TryConvertToSaturatingSByteTest()
        {
            Assert.Equal(0x00, NumberBaseHelper<sbyte>.CreateSaturating<BigInteger>(Zero));
            Assert.Equal(0x01, NumberBaseHelper<sbyte>.CreateSaturating<BigInteger>(One));
            Assert.Equal(0x7F, NumberBaseHelper<sbyte>.CreateSaturating<BigInteger>(SByteMaxValue));
            Assert.Equal(unchecked((sbyte)0x80), NumberBaseHelper<sbyte>.CreateSaturating<BigInteger>(SByteMinValue));
            Assert.Equal(unchecked((sbyte)0xFF), NumberBaseHelper<sbyte>.CreateSaturating<BigInteger>(NegativeOne));
        }

        [Fact]
        public static void TryConvertToSaturatingSingleTest()
        {
            Assert.Equal(+0.0f, NumberBaseHelper<float>.CreateSaturating<BigInteger>(Zero));

            Assert.Equal(+1.0f, NumberBaseHelper<float>.CreateSaturating<BigInteger>(One));
            Assert.Equal(-1.0f, NumberBaseHelper<float>.CreateSaturating<BigInteger>(NegativeOne));

            Assert.Equal(+170141173319264429905852091742258462720.0f, NumberBaseHelper<float>.CreateSaturating<BigInteger>((BigInteger)(new Int128(0x7FFF_FF80_0000_0000, 0x0000_0000_0000_0000))));
            Assert.Equal(-170141173319264429905852091742258462720.0f, NumberBaseHelper<float>.CreateSaturating<BigInteger>((BigInteger)(new Int128(0x8000_0080_0000_0000, 0x0000_0000_0000_0000))));
        }

        [Fact]
        public static void TryConvertToSaturatingUInt16Test()
        {
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateSaturating<BigInteger>(Zero));
            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.CreateSaturating<BigInteger>(One));
            Assert.Equal((ushort)0x7FFF, NumberBaseHelper<ushort>.CreateSaturating<BigInteger>(Int16MaxValue));
            Assert.Equal((ushort)0x8000, NumberBaseHelper<ushort>.CreateSaturating<BigInteger>(Int16MaxValuePlusOne));
            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateSaturating<BigInteger>(UInt16MaxValue));
        }

        [Fact]
        public static void TryConvertToSaturatingUInt32Test()
        {
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateSaturating<BigInteger>(Zero));
            Assert.Equal((uint)0x0000_0001, NumberBaseHelper<uint>.CreateSaturating<BigInteger>(One));
            Assert.Equal((uint)0x7FFF_FFFF, NumberBaseHelper<uint>.CreateSaturating<BigInteger>(Int32MaxValue));
            Assert.Equal((uint)0x8000_0000, NumberBaseHelper<uint>.CreateSaturating<BigInteger>(Int32MaxValuePlusOne));
            Assert.Equal((uint)0xFFFF_FFFF, NumberBaseHelper<uint>.CreateSaturating<BigInteger>(UInt32MaxValue));
        }

        [Fact]
        public static void TryConvertToSaturatingUInt64Test()
        {
            Assert.Equal((ulong)0x0000_0000_0000_0000, NumberBaseHelper<ulong>.CreateSaturating<BigInteger>(Zero));
            Assert.Equal((ulong)0x0000_0000_0000_0001, NumberBaseHelper<ulong>.CreateSaturating<BigInteger>(One));
            Assert.Equal((ulong)0x7FFF_FFFF_FFFF_FFFF, NumberBaseHelper<ulong>.CreateSaturating<BigInteger>(Int64MaxValue));
            Assert.Equal((ulong)0x8000_0000_0000_0000, NumberBaseHelper<ulong>.CreateSaturating<BigInteger>(Int64MaxValuePlusOne));
            Assert.Equal((ulong)0xFFFF_FFFF_FFFF_FFFF, NumberBaseHelper<ulong>.CreateSaturating<BigInteger>(UInt64MaxValue));
        }

        [Fact]
        public static void TryConvertToSaturatingUInt128Test()
        {
            Assert.Equal(UInt128.Zero, NumberBaseHelper<UInt128>.CreateSaturating<BigInteger>(Zero));
            Assert.Equal(UInt128.One, NumberBaseHelper<UInt128>.CreateSaturating<BigInteger>(One));
            Assert.Equal(new UInt128(0x7FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), NumberBaseHelper<UInt128>.CreateSaturating<BigInteger>(Int128MaxValue));
            Assert.Equal(new UInt128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000), NumberBaseHelper<UInt128>.CreateSaturating<BigInteger>(Int128MaxValuePlusOne));
            Assert.Equal(UInt128.MaxValue, NumberBaseHelper<UInt128>.CreateSaturating<BigInteger>(UInt128MaxValue));
        }

        [Fact]
        public static void TryConvertToSaturatingUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000_0000_0000_0000), NumberBaseHelper<nuint>.CreateSaturating<BigInteger>(Zero));
                Assert.Equal(unchecked((nuint)0x0000_0000_0000_0001), NumberBaseHelper<nuint>.CreateSaturating<BigInteger>(One));
                Assert.Equal(unchecked((nuint)0x7FFF_FFFF_FFFF_FFFF), NumberBaseHelper<nuint>.CreateSaturating<BigInteger>(Int64MaxValue));
                Assert.Equal(unchecked((nuint)0x8000_0000_0000_0000), NumberBaseHelper<nuint>.CreateSaturating<BigInteger>(Int64MaxValuePlusOne));
                Assert.Equal(unchecked((nuint)0xFFFF_FFFF_FFFF_FFFF), NumberBaseHelper<nuint>.CreateSaturating<BigInteger>(UInt64MaxValue));
            }
            else
            {
                Assert.Equal((nuint)0x0000_0000, NumberBaseHelper<nuint>.CreateSaturating<BigInteger>(Zero));
                Assert.Equal((nuint)0x0000_0001, NumberBaseHelper<nuint>.CreateSaturating<BigInteger>(One));
                Assert.Equal((nuint)0x7FFF_FFFF, NumberBaseHelper<nuint>.CreateSaturating<BigInteger>(Int32MaxValue));
                Assert.Equal((nuint)0x8000_0000, NumberBaseHelper<nuint>.CreateSaturating<BigInteger>(Int32MaxValuePlusOne));
                Assert.Equal((nuint)0xFFFF_FFFF, NumberBaseHelper<nuint>.CreateSaturating<BigInteger>(UInt32MaxValue));
            }
        }

        [Fact]
        public static void TryConvertToTruncatingByteTest()
        {
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateTruncating<BigInteger>(Zero));
            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.CreateTruncating<BigInteger>(One));
            Assert.Equal((byte)0x7F, NumberBaseHelper<byte>.CreateTruncating<BigInteger>(SByteMaxValue));
            Assert.Equal((byte)0x80, NumberBaseHelper<byte>.CreateTruncating<BigInteger>(SByteMaxValuePlusOne));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateTruncating<BigInteger>(ByteMaxValue));
        }

        [Fact]
        public static void TryConvertToTruncatingCharTest()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<BigInteger>(Zero));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateTruncating<BigInteger>(One));
            Assert.Equal((char)0x7FFF, NumberBaseHelper<char>.CreateTruncating<BigInteger>(Int16MaxValue));
            Assert.Equal((char)0x8000, NumberBaseHelper<char>.CreateTruncating<BigInteger>(Int16MaxValuePlusOne));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateTruncating<BigInteger>(UInt16MaxValue));
        }

        [Fact]
        public static void TryConvertToTruncatingDecimalTest()
        {
            Assert.Equal(decimal.Zero, NumberBaseHelper<decimal>.CreateTruncating<BigInteger>(Zero));

            Assert.Equal(decimal.One, NumberBaseHelper<decimal>.CreateTruncating<BigInteger>(One));
            Assert.Equal(decimal.MinusOne, NumberBaseHelper<decimal>.CreateTruncating<BigInteger>(NegativeOne));

            Assert.Equal(decimal.MaxValue, NumberBaseHelper<decimal>.CreateTruncating<BigInteger>((BigInteger)(new Int128(0x0000_0000_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF))));
            Assert.Equal(decimal.MinValue, NumberBaseHelper<decimal>.CreateTruncating<BigInteger>((BigInteger)(new Int128(0xFFFF_FFFF_0000_0000, 0x0000_0000_0000_0001))));
        }

        [Fact]
        public static void TryConvertToTruncatingDoubleTest()
        {
            Assert.Equal(+0.0, NumberBaseHelper<double>.CreateTruncating<BigInteger>(Zero));

            Assert.Equal(+1.0, NumberBaseHelper<double>.CreateTruncating<BigInteger>(One));
            Assert.Equal(-1.0, NumberBaseHelper<double>.CreateTruncating<BigInteger>(NegativeOne));

            Assert.Equal(+170141183460469212842221372237303250944.0, NumberBaseHelper<double>.CreateTruncating<BigInteger>((BigInteger)(new Int128(0x7FFF_FFFF_FFFF_FC00, 0x0000_0000_0000_0000))));
            Assert.Equal(-170141183460469212842221372237303250944.0, NumberBaseHelper<double>.CreateTruncating<BigInteger>((BigInteger)(new Int128(0x8000_0000_0000_0400, 0x0000_0000_0000_0000))));
        }

        [Fact]
        public static void TryConvertToTruncatingHalfTest()
        {
            Assert.Equal((Half)(+0.0), NumberBaseHelper<Half>.CreateTruncating<BigInteger>(Zero));

            Assert.Equal((Half)(+1.0), NumberBaseHelper<Half>.CreateTruncating<BigInteger>(One));
            Assert.Equal((Half)(-1.0), NumberBaseHelper<Half>.CreateTruncating<BigInteger>(NegativeOne));

            Assert.Equal(Half.MaxValue, NumberBaseHelper<Half>.CreateTruncating<BigInteger>(+65504));
            Assert.Equal(Half.MinValue, NumberBaseHelper<Half>.CreateTruncating<BigInteger>(-65504));
        }

        [Fact]
        public static void TryConvertToTruncatingInt16Test()
        {
            Assert.Equal(0x0000, NumberBaseHelper<short>.CreateTruncating<BigInteger>(Zero));
            Assert.Equal(0x0001, NumberBaseHelper<short>.CreateTruncating<BigInteger>(One));
            Assert.Equal(0x7FFF, NumberBaseHelper<short>.CreateTruncating<BigInteger>(Int16MaxValue));
            Assert.Equal(unchecked((short)0x8000), NumberBaseHelper<short>.CreateTruncating<BigInteger>(Int16MinValue));
            Assert.Equal(unchecked((short)0xFFFF), NumberBaseHelper<short>.CreateTruncating<BigInteger>(NegativeOne));
        }

        [Fact]
        public static void TryConvertToTruncatingInt32Test()
        {
            Assert.Equal(0x0000_0000, NumberBaseHelper<int>.CreateTruncating<BigInteger>(Zero));
            Assert.Equal(0x0000_0001, NumberBaseHelper<int>.CreateTruncating<BigInteger>(One));
            Assert.Equal(0x7FFF_FFFF, NumberBaseHelper<int>.CreateTruncating<BigInteger>(Int32MaxValue));
            Assert.Equal(unchecked((int)0x8000_0000), NumberBaseHelper<int>.CreateTruncating<BigInteger>(Int32MinValue));
            Assert.Equal(unchecked((int)0xFFFF_FFFF), NumberBaseHelper<int>.CreateTruncating<BigInteger>(NegativeOne));
        }

        [Fact]
        public static void TryConvertToTruncatingInt64Test()
        {
            Assert.Equal(0x0000_0000_0000_0000, NumberBaseHelper<long>.CreateTruncating<BigInteger>(Zero));
            Assert.Equal(0x0000_0000_0000_0001, NumberBaseHelper<long>.CreateTruncating<BigInteger>(One));
            Assert.Equal(0x7FFF_FFFF_FFFF_FFFF, NumberBaseHelper<long>.CreateTruncating<BigInteger>(Int64MaxValue));
            Assert.Equal(unchecked((long)0x8000_0000_0000_0000), NumberBaseHelper<long>.CreateTruncating<BigInteger>(Int64MinValue));
            Assert.Equal(unchecked((long)0xFFFF_FFFF_FFFF_FFFF), NumberBaseHelper<long>.CreateTruncating<BigInteger>(NegativeOne));
        }

        [Fact]
        public static void TryConvertToTruncatingInt128Test()
        {
            Assert.Equal(Zero, NumberBaseHelper<Int128>.CreateTruncating<Int128>(Int128.Zero));
            Assert.Equal(One, NumberBaseHelper<Int128>.CreateTruncating<Int128>(Int128.One));
            Assert.Equal(Int128MaxValue, NumberBaseHelper<Int128>.CreateTruncating<Int128>(Int128.MaxValue));
            Assert.Equal(Int128MinValue, NumberBaseHelper<Int128>.CreateTruncating<Int128>(Int128.MinValue));
            Assert.Equal(NegativeOne, NumberBaseHelper<Int128>.CreateTruncating<Int128>(Int128.NegativeOne));
        }

        [Fact]
        public static void TryConvertToTruncatingIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0x0000_0000), NumberBaseHelper<nint>.CreateTruncating<BigInteger>(Zero));
                Assert.Equal(unchecked((nint)0x0000_0001), NumberBaseHelper<nint>.CreateTruncating<BigInteger>(One));
                Assert.Equal(unchecked((nint)0x7FFF_FFFF_FFFF_FFFF), NumberBaseHelper<nint>.CreateTruncating<BigInteger>(Int64MaxValue));
                Assert.Equal(unchecked((nint)0x8000_0000_0000_0000), NumberBaseHelper<nint>.CreateTruncating<BigInteger>(Int64MinValue));
                Assert.Equal(unchecked((nint)0xFFFF_FFFF_FFFF_FFFF), NumberBaseHelper<nint>.CreateTruncating<BigInteger>(NegativeOne));
            }
            else
            {
                Assert.Equal((nint)0x0000_0000, NumberBaseHelper<nint>.CreateTruncating<BigInteger>(Zero));
                Assert.Equal((nint)0x0000_0001, NumberBaseHelper<nint>.CreateTruncating<BigInteger>(One));
                Assert.Equal((nint)0x7FFF_FFFF, NumberBaseHelper<nint>.CreateTruncating<BigInteger>(Int32MaxValue));
                Assert.Equal(unchecked((nint)0x8000_0000), NumberBaseHelper<nint>.CreateTruncating<BigInteger>(Int32MinValue));
                Assert.Equal(unchecked((nint)0xFFFF_FFFF), NumberBaseHelper<nint>.CreateTruncating<BigInteger>(NegativeOne));
            }
        }

        [Fact]
        public static void TryConvertToTruncatingSByteTest()
        {
            Assert.Equal(0x00, NumberBaseHelper<sbyte>.CreateTruncating<BigInteger>(Zero));
            Assert.Equal(0x01, NumberBaseHelper<sbyte>.CreateTruncating<BigInteger>(One));
            Assert.Equal(0x7F, NumberBaseHelper<sbyte>.CreateTruncating<BigInteger>(SByteMaxValue));
            Assert.Equal(unchecked((sbyte)0x80), NumberBaseHelper<sbyte>.CreateTruncating<BigInteger>(SByteMinValue));
            Assert.Equal(unchecked((sbyte)0xFF), NumberBaseHelper<sbyte>.CreateTruncating<BigInteger>(NegativeOne));
        }

        [Fact]
        public static void TryConvertToTruncatingSingleTest()
        {
            Assert.Equal(+0.0f, NumberBaseHelper<float>.CreateTruncating<BigInteger>(Zero));

            Assert.Equal(+1.0f, NumberBaseHelper<float>.CreateTruncating<BigInteger>(One));
            Assert.Equal(-1.0f, NumberBaseHelper<float>.CreateTruncating<BigInteger>(NegativeOne));

            Assert.Equal(+170141173319264429905852091742258462720.0f, NumberBaseHelper<float>.CreateTruncating<BigInteger>((BigInteger)(new Int128(0x7FFF_FF80_0000_0000, 0x0000_0000_0000_0000))));
            Assert.Equal(-170141173319264429905852091742258462720.0f, NumberBaseHelper<float>.CreateTruncating<BigInteger>((BigInteger)(new Int128(0x8000_0080_0000_0000, 0x0000_0000_0000_0000))));
        }

        [Fact]
        public static void TryConvertToTruncatingUInt16Test()
        {
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateTruncating<BigInteger>(Zero));
            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.CreateTruncating<BigInteger>(One));
            Assert.Equal((ushort)0x7FFF, NumberBaseHelper<ushort>.CreateTruncating<BigInteger>(Int16MaxValue));
            Assert.Equal((ushort)0x8000, NumberBaseHelper<ushort>.CreateTruncating<BigInteger>(Int16MaxValuePlusOne));
            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateTruncating<BigInteger>(UInt16MaxValue));
        }

        [Fact]
        public static void TryConvertToTruncatingUInt32Test()
        {
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateTruncating<BigInteger>(Zero));
            Assert.Equal((uint)0x0000_0001, NumberBaseHelper<uint>.CreateTruncating<BigInteger>(One));
            Assert.Equal((uint)0x7FFF_FFFF, NumberBaseHelper<uint>.CreateTruncating<BigInteger>(Int32MaxValue));
            Assert.Equal((uint)0x8000_0000, NumberBaseHelper<uint>.CreateTruncating<BigInteger>(Int32MaxValuePlusOne));
            Assert.Equal((uint)0xFFFF_FFFF, NumberBaseHelper<uint>.CreateTruncating<BigInteger>(UInt32MaxValue));
        }

        [Fact]
        public static void TryConvertToTruncatingUInt64Test()
        {
            Assert.Equal((ulong)0x0000_0000_0000_0000, NumberBaseHelper<ulong>.CreateTruncating<BigInteger>(Zero));
            Assert.Equal((ulong)0x0000_0000_0000_0001, NumberBaseHelper<ulong>.CreateTruncating<BigInteger>(One));
            Assert.Equal((ulong)0x7FFF_FFFF_FFFF_FFFF, NumberBaseHelper<ulong>.CreateTruncating<BigInteger>(Int64MaxValue));
            Assert.Equal((ulong)0x8000_0000_0000_0000, NumberBaseHelper<ulong>.CreateTruncating<BigInteger>(Int64MaxValuePlusOne));
            Assert.Equal((ulong)0xFFFF_FFFF_FFFF_FFFF, NumberBaseHelper<ulong>.CreateTruncating<BigInteger>(UInt64MaxValue));
        }

        [Fact]
        public static void TryConvertToTruncatingUInt128Test()
        {
            Assert.Equal(UInt128.Zero, NumberBaseHelper<UInt128>.CreateTruncating<BigInteger>(Zero));
            Assert.Equal(UInt128.One, NumberBaseHelper<UInt128>.CreateTruncating<BigInteger>(One));
            Assert.Equal(new UInt128(0x7FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), NumberBaseHelper<UInt128>.CreateTruncating<BigInteger>(Int128MaxValue));
            Assert.Equal(new UInt128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000), NumberBaseHelper<UInt128>.CreateTruncating<BigInteger>(Int128MaxValuePlusOne));
            Assert.Equal(UInt128.MaxValue, NumberBaseHelper<UInt128>.CreateTruncating<BigInteger>(UInt128MaxValue));
        }

        [Fact]
        public static void TryConvertToTruncatingUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x00000000), NumberBaseHelper<nuint>.CreateTruncating<BigInteger>(Zero));
                Assert.Equal(unchecked((nuint)0x00000001), NumberBaseHelper<nuint>.CreateTruncating<BigInteger>(One));
                Assert.Equal(unchecked((nuint)0x7FFFFFFFFFFFFFFF), NumberBaseHelper<nuint>.CreateTruncating<BigInteger>(Int64MaxValue));
                Assert.Equal(unchecked((nuint)0x8000000000000000), NumberBaseHelper<nuint>.CreateTruncating<BigInteger>(Int64MaxValuePlusOne));
                Assert.Equal(unchecked((nuint)0xFFFFFFFFFFFFFFFF), NumberBaseHelper<nuint>.CreateTruncating<BigInteger>(UInt64MaxValue));
            }
            else
            {
                Assert.Equal((nuint)0x00000000, NumberBaseHelper<nuint>.CreateTruncating<BigInteger>(Zero));
                Assert.Equal((nuint)0x00000001, NumberBaseHelper<nuint>.CreateTruncating<BigInteger>(One));
                Assert.Equal((nuint)0x7FFFFFFF, NumberBaseHelper<nuint>.CreateTruncating<BigInteger>(Int32MaxValue));
                Assert.Equal((nuint)0x80000000, NumberBaseHelper<nuint>.CreateTruncating<BigInteger>(Int32MaxValuePlusOne));
                Assert.Equal((nuint)0xFFFFFFFF, NumberBaseHelper<nuint>.CreateTruncating<BigInteger>(UInt32MaxValue));
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
