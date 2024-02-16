// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Tests
{
    public class UInt32Tests_GenericMath
    {
        //
        // IAdditionOperators
        //

        [Fact]
        public static void op_AdditionTest()
        {
            Assert.Equal((uint)0x00000001, AdditionOperatorsHelper<uint, uint, uint>.op_Addition((uint)0x00000000, 1));
            Assert.Equal((uint)0x00000002, AdditionOperatorsHelper<uint, uint, uint>.op_Addition((uint)0x00000001, 1));
            Assert.Equal((uint)0x80000000, AdditionOperatorsHelper<uint, uint, uint>.op_Addition((uint)0x7FFFFFFF, 1));
            Assert.Equal((uint)0x80000001, AdditionOperatorsHelper<uint, uint, uint>.op_Addition((uint)0x80000000, 1));
            Assert.Equal((uint)0x00000000, AdditionOperatorsHelper<uint, uint, uint>.op_Addition((uint)0xFFFFFFFF, 1));
        }

        [Fact]
        public static void op_CheckedAdditionTest()
        {
            Assert.Equal((uint)0x00000001, AdditionOperatorsHelper<uint, uint, uint>.op_CheckedAddition((uint)0x00000000, 1));
            Assert.Equal((uint)0x00000002, AdditionOperatorsHelper<uint, uint, uint>.op_CheckedAddition((uint)0x00000001, 1));
            Assert.Equal((uint)0x80000000, AdditionOperatorsHelper<uint, uint, uint>.op_CheckedAddition((uint)0x7FFFFFFF, 1));
            Assert.Equal((uint)0x80000001, AdditionOperatorsHelper<uint, uint, uint>.op_CheckedAddition((uint)0x80000000, 1));


            Assert.Throws<OverflowException>(() => AdditionOperatorsHelper<uint, uint, uint>.op_CheckedAddition((uint)0xFFFFFFFF, 1));
        }

        //
        // IAdditiveIdentity
        //

        [Fact]
        public static void AdditiveIdentityTest()
        {
            Assert.Equal((uint)0x00000000, AdditiveIdentityHelper<uint, uint>.AdditiveIdentity);
        }

        //
        // IBinaryInteger
        //

        [Fact]
        public static void DivRemTest()
        {
            Assert.Equal(((uint)0x00000000, (uint)0x00000000), BinaryIntegerHelper<uint>.DivRem((uint)0x00000000, 2));
            Assert.Equal(((uint)0x00000000, (uint)0x00000001), BinaryIntegerHelper<uint>.DivRem((uint)0x00000001, 2));
            Assert.Equal(((uint)0x3FFFFFFF, (uint)0x00000001), BinaryIntegerHelper<uint>.DivRem((uint)0x7FFFFFFF, 2));
            Assert.Equal(((uint)0x40000000, (uint)0x00000000), BinaryIntegerHelper<uint>.DivRem((uint)0x80000000, 2));
            Assert.Equal(((uint)0x7FFFFFFF, (uint)0x00000001), BinaryIntegerHelper<uint>.DivRem((uint)0xFFFFFFFF, 2));
        }

        [Fact]
        public static void LeadingZeroCountTest()
        {
            Assert.Equal((uint)0x00000020, BinaryIntegerHelper<uint>.LeadingZeroCount((uint)0x00000000));
            Assert.Equal((uint)0x0000001F, BinaryIntegerHelper<uint>.LeadingZeroCount((uint)0x00000001));
            Assert.Equal((uint)0x00000001, BinaryIntegerHelper<uint>.LeadingZeroCount((uint)0x7FFFFFFF));
            Assert.Equal((uint)0x00000000, BinaryIntegerHelper<uint>.LeadingZeroCount((uint)0x80000000));
            Assert.Equal((uint)0x00000000, BinaryIntegerHelper<uint>.LeadingZeroCount((uint)0xFFFFFFFF));
        }

        [Fact]
        public static void PopCountTest()
        {
            Assert.Equal((uint)0x00000000, BinaryIntegerHelper<uint>.PopCount((uint)0x00000000));
            Assert.Equal((uint)0x00000001, BinaryIntegerHelper<uint>.PopCount((uint)0x00000001));
            Assert.Equal((uint)0x0000001F, BinaryIntegerHelper<uint>.PopCount((uint)0x7FFFFFFF));
            Assert.Equal((uint)0x00000001, BinaryIntegerHelper<uint>.PopCount((uint)0x80000000));
            Assert.Equal((uint)0x00000020, BinaryIntegerHelper<uint>.PopCount((uint)0xFFFFFFFF));
        }

        [Fact]
        public static void RotateLeftTest()
        {
            Assert.Equal((uint)0x00000000, BinaryIntegerHelper<uint>.RotateLeft((uint)0x00000000, 1));
            Assert.Equal((uint)0x00000002, BinaryIntegerHelper<uint>.RotateLeft((uint)0x00000001, 1));
            Assert.Equal((uint)0xFFFFFFFE, BinaryIntegerHelper<uint>.RotateLeft((uint)0x7FFFFFFF, 1));
            Assert.Equal((uint)0x00000001, BinaryIntegerHelper<uint>.RotateLeft((uint)0x80000000, 1));
            Assert.Equal((uint)0xFFFFFFFF, BinaryIntegerHelper<uint>.RotateLeft((uint)0xFFFFFFFF, 1));
        }

        [Fact]
        public static void RotateRightTest()
        {
            Assert.Equal((uint)0x00000000, BinaryIntegerHelper<uint>.RotateRight((uint)0x00000000, 1));
            Assert.Equal((uint)0x80000000, BinaryIntegerHelper<uint>.RotateRight((uint)0x00000001, 1));
            Assert.Equal((uint)0xBFFFFFFF, BinaryIntegerHelper<uint>.RotateRight((uint)0x7FFFFFFF, 1));
            Assert.Equal((uint)0x40000000, BinaryIntegerHelper<uint>.RotateRight((uint)0x80000000, 1));
            Assert.Equal((uint)0xFFFFFFFF, BinaryIntegerHelper<uint>.RotateRight((uint)0xFFFFFFFF, 1));
        }

        [Fact]
        public static void TrailingZeroCountTest()
        {
            Assert.Equal((uint)0x00000020, BinaryIntegerHelper<uint>.TrailingZeroCount((uint)0x00000000));
            Assert.Equal((uint)0x00000000, BinaryIntegerHelper<uint>.TrailingZeroCount((uint)0x00000001));
            Assert.Equal((uint)0x00000000, BinaryIntegerHelper<uint>.TrailingZeroCount((uint)0x7FFFFFFF));
            Assert.Equal((uint)0x0000001F, BinaryIntegerHelper<uint>.TrailingZeroCount((uint)0x80000000));
            Assert.Equal((uint)0x00000000, BinaryIntegerHelper<uint>.TrailingZeroCount((uint)0xFFFFFFFF));
        }

        [Fact]
        public static void TryReadBigEndianByteTest()
        {
            uint result;

            Assert.True(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0x00 }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0x01 }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_0001, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0x7F }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_007F, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0x80 }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_0080, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0xFF }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_00FF, result);
        }

        [Fact]
        public static void TryReadBigEndianInt16Test()
        {
            uint result;

            Assert.True(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0001, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0080, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0x01, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0100, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0x7F, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_7FFF, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0x80, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0000, result);
        }

        [Fact]
        public static void TryReadBigEndianInt32Test()
        {
            uint result;

            Assert.True(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0001, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0080, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0x01, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0100_0000, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((uint)0x7FFF_FFFF, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0x80, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000, result);
        }

        [Fact]
        public static void TryReadBigEndianInt64Test()
        {
            uint result;

            Assert.True(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0001, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0080, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0000, result);
        }

        [Fact]
        public static void TryReadBigEndianInt96Test()
        {
            uint result;

            Assert.True(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0001, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0080, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0000, result);
        }

        [Fact]
        public static void TryReadBigEndianInt128Test()
        {
            uint result;

            Assert.True(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0001, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0080, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0000, result);
        }

        [Fact]
        public static void TryReadBigEndianSByteTest()
        {
            uint result;

            Assert.True(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0x00 }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0x01 }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0001, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0x7F }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_007F, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0x80 }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0xFF }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0000, result);
        }

        [Fact]
        public static void TryReadBigEndianUInt16Test()
        {
            uint result;

            Assert.True(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_0001, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_0080, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0x01, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_0100, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0x7F, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_7FFF, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0x80, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_8000, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_FF7F, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_FFFF, result);
        }

        [Fact]
        public static void TryReadBigEndianUInt32Test()
        {
            uint result;

            Assert.True(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_0001, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_0080, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0x01, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0100_0000, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((uint)0x7FFF_FFFF, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0x80, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((uint)0x8000_0000, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal((uint)0xFFFF_FF7F, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((uint)0xFFFF_FFFF, result);
        }

        [Fact]
        public static void TryReadBigEndianUInt64Test()
        {
            uint result;

            Assert.True(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_0001, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_0080, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_0000, result);
        }

        [Fact]
        public static void TryReadBigEndianUInt96Test()
        {
            uint result;

            Assert.True(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_0001, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_0080, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_0000, result);
        }

        [Fact]
        public static void TryReadBigEndianUInt128Test()
        {
            uint result;

            Assert.True(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_0001, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_0080, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadBigEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_0000, result);
        }

        [Fact]
        public static void TryReadLittleEndianByteTest()
        {
            uint result;

            Assert.True(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0x00 }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0x01 }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_0001, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0x7F }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_007F, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0x80 }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_0080, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0xFF }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_00FF, result);
        }

        [Fact]
        public static void TryReadLittleEndianInt16Test()
        {
            uint result;

            Assert.True(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0100, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0x01, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0001, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0x80, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0080, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_7FFF, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0000, result);
        }

        [Fact]
        public static void TryReadLittleEndianInt32Test()
        {
            uint result;

            Assert.True(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0100_0000, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0x01, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0001, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0x80, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0080, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal((uint)0x7FFF_FFFF, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0000, result);
        }

        [Fact]
        public static void TryReadLittleEndianInt64Test()
        {
            uint result;

            Assert.True(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0001, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0080, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0000, result);
        }

        [Fact]
        public static void TryReadLittleEndianInt96Test()
        {
            uint result;

            Assert.True(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0001, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0080, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0000, result);
        }

        [Fact]
        public static void TryReadLittleEndianInt128Test()
        {
            uint result;

            Assert.True(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0001, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0080, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0000, result);
        }

        [Fact]
        public static void TryReadLittleEndianSByteTest()
        {
            uint result;

            Assert.True(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0x00 }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0x01 }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0001, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0x7F }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_007F, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0x80 }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0xFF }, isUnsigned: false, out result));
            Assert.Equal((uint)0x0000_0000, result);
        }

        [Fact]
        public static void TryReadLittleEndianUInt16Test()
        {
            uint result;

            Assert.True(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_0100, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_8000, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0x01, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_0001, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_FF7F, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0x80, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_0080, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_7FFF, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_FFFF, result);
        }

        [Fact]
        public static void TryReadLittleEndianUInt32Test()
        {
            uint result;

            Assert.True(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0100_0000, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal((uint)0x8000_0000, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0x01, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_0001, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((uint)0xFFFF_FF7F, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0x80, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_0080, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal((uint)0x7FFF_FFFF, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((uint)0xFFFF_FFFF, result);
        }

        [Fact]
        public static void TryReadLittleEndianUInt64Test()
        {
            uint result;

            Assert.True(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_0001, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_0080, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_0000, result);
        }

        [Fact]
        public static void TryReadLittleEndianUInt96Test()
        {
            uint result;

            Assert.True(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_0001, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_0080, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_0000, result);
        }

        [Fact]
        public static void TryReadLittleEndianUInt128Test()
        {
            uint result;

            Assert.True(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_0001, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.True(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_0080, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_0000, result);

            Assert.False(BinaryIntegerHelper<uint>.TryReadLittleEndian(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, isUnsigned: true, out result));
            Assert.Equal((uint)0x0000_0000, result);
        }

        [Fact]
        public static void GetByteCountTest()
        {
            Assert.Equal(4, BinaryIntegerHelper<uint>.GetByteCount((uint)0x00000000));
            Assert.Equal(4, BinaryIntegerHelper<uint>.GetByteCount((uint)0x00000001));
            Assert.Equal(4, BinaryIntegerHelper<uint>.GetByteCount((uint)0x7FFFFFFF));
            Assert.Equal(4, BinaryIntegerHelper<uint>.GetByteCount((uint)0x80000000));
            Assert.Equal(4, BinaryIntegerHelper<uint>.GetByteCount((uint)0xFFFFFFFF));
        }

        [Fact]
        public static void GetShortestBitLengthTest()
        {
            Assert.Equal(0x00, BinaryIntegerHelper<uint>.GetShortestBitLength((uint)0x00000000));
            Assert.Equal(0x01, BinaryIntegerHelper<uint>.GetShortestBitLength((uint)0x00000001));
            Assert.Equal(0x1F, BinaryIntegerHelper<uint>.GetShortestBitLength((uint)0x7FFFFFFF));
            Assert.Equal(0x20, BinaryIntegerHelper<uint>.GetShortestBitLength((uint)0x80000000));
            Assert.Equal(0x20, BinaryIntegerHelper<uint>.GetShortestBitLength((uint)0xFFFFFFFF));
        }

        [Fact]
        public static void TryWriteBigEndianTest()
        {
            Span<byte> destination = stackalloc byte[4];
            int bytesWritten = 0;

            Assert.True(BinaryIntegerHelper<uint>.TryWriteBigEndian((uint)0x00000000, destination, out bytesWritten));
            Assert.Equal(4, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<uint>.TryWriteBigEndian((uint)0x00000001, destination, out bytesWritten));
            Assert.Equal(4, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x01 }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<uint>.TryWriteBigEndian((uint)0x7FFFFFFF, destination, out bytesWritten));
            Assert.Equal(4, bytesWritten);
            Assert.Equal(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<uint>.TryWriteBigEndian((uint)0x80000000, destination, out bytesWritten));
            Assert.Equal(4, bytesWritten);
            Assert.Equal(new byte[] { 0x80, 0x00, 0x00, 0x00 }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<uint>.TryWriteBigEndian((uint)0xFFFFFFFF, destination, out bytesWritten));
            Assert.Equal(4, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, destination.ToArray());

            Assert.False(BinaryIntegerHelper<uint>.TryWriteBigEndian(default, Span<byte>.Empty, out bytesWritten));
            Assert.Equal(0, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, destination.ToArray());
        }

        [Fact]
        public static void TryWriteLittleEndianTest()
        {
            Span<byte> destination = stackalloc byte[4];
            int bytesWritten = 0;

            Assert.True(BinaryIntegerHelper<uint>.TryWriteLittleEndian((uint)0x00000000, destination, out bytesWritten));
            Assert.Equal(4, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<uint>.TryWriteLittleEndian((uint)0x00000001, destination, out bytesWritten));
            Assert.Equal(4, bytesWritten);
            Assert.Equal(new byte[] { 0x01, 0x00, 0x00, 0x00 }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<uint>.TryWriteLittleEndian((uint)0x7FFFFFFF, destination, out bytesWritten));
            Assert.Equal(4, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0x7F }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<uint>.TryWriteLittleEndian((uint)0x80000000, destination, out bytesWritten));
            Assert.Equal(4, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x80 }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<uint>.TryWriteLittleEndian((uint)0xFFFFFFFF, destination, out bytesWritten));
            Assert.Equal(4, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, destination.ToArray());

            Assert.False(BinaryIntegerHelper<uint>.TryWriteLittleEndian(default, Span<byte>.Empty, out bytesWritten));
            Assert.Equal(0, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, destination.ToArray());
        }

        //
        // IBinaryNumber
        //


        [Fact]
        public static void AllBitsSetTest()
        {
            Assert.Equal(0xFFFF_FFFF, BinaryNumberHelper<uint>.AllBitsSet);
            Assert.Equal(0U, ~BinaryNumberHelper<uint>.AllBitsSet);
        }

        [Fact]
        public static void IsPow2Test()
        {
            Assert.False(BinaryNumberHelper<uint>.IsPow2((uint)0x00000000));
            Assert.True(BinaryNumberHelper<uint>.IsPow2((uint)0x00000001));
            Assert.False(BinaryNumberHelper<uint>.IsPow2((uint)0x7FFFFFFF));
            Assert.True(BinaryNumberHelper<uint>.IsPow2((uint)0x80000000));
            Assert.False(BinaryNumberHelper<uint>.IsPow2((uint)0xFFFFFFFF));
        }

        [Fact]
        public static void Log2Test()
        {
            Assert.Equal((uint)0x00000000, BinaryNumberHelper<uint>.Log2((uint)0x00000000));
            Assert.Equal((uint)0x00000000, BinaryNumberHelper<uint>.Log2((uint)0x00000001));
            Assert.Equal((uint)0x0000001E, BinaryNumberHelper<uint>.Log2((uint)0x7FFFFFFF));
            Assert.Equal((uint)0x0000001F, BinaryNumberHelper<uint>.Log2((uint)0x80000000));
            Assert.Equal((uint)0x0000001F, BinaryNumberHelper<uint>.Log2((uint)0xFFFFFFFF));
        }

        //
        // IBitwiseOperators
        //

        [Fact]
        public static void op_BitwiseAndTest()
        {
            Assert.Equal((uint)0x00000000, BitwiseOperatorsHelper<uint, uint, uint>.op_BitwiseAnd((uint)0x00000000, 1));
            Assert.Equal((uint)0x00000001, BitwiseOperatorsHelper<uint, uint, uint>.op_BitwiseAnd((uint)0x00000001, 1));
            Assert.Equal((uint)0x00000001, BitwiseOperatorsHelper<uint, uint, uint>.op_BitwiseAnd((uint)0x7FFFFFFF, 1));
            Assert.Equal((uint)0x00000000, BitwiseOperatorsHelper<uint, uint, uint>.op_BitwiseAnd((uint)0x80000000, 1));
            Assert.Equal((uint)0x00000001, BitwiseOperatorsHelper<uint, uint, uint>.op_BitwiseAnd((uint)0xFFFFFFFF, 1));
        }

        [Fact]
        public static void op_BitwiseOrTest()
        {
            Assert.Equal((uint)0x00000001, BitwiseOperatorsHelper<uint, uint, uint>.op_BitwiseOr((uint)0x00000000, 1));
            Assert.Equal((uint)0x00000001, BitwiseOperatorsHelper<uint, uint, uint>.op_BitwiseOr((uint)0x00000001, 1));
            Assert.Equal((uint)0x7FFFFFFF, BitwiseOperatorsHelper<uint, uint, uint>.op_BitwiseOr((uint)0x7FFFFFFF, 1));
            Assert.Equal((uint)0x80000001, BitwiseOperatorsHelper<uint, uint, uint>.op_BitwiseOr((uint)0x80000000, 1));
            Assert.Equal((uint)0xFFFFFFFF, BitwiseOperatorsHelper<uint, uint, uint>.op_BitwiseOr((uint)0xFFFFFFFF, 1));
        }

        [Fact]
        public static void op_ExclusiveOrTest()
        {
            Assert.Equal((uint)0x00000001, BitwiseOperatorsHelper<uint, uint, uint>.op_ExclusiveOr((uint)0x00000000, 1));
            Assert.Equal((uint)0x00000000, BitwiseOperatorsHelper<uint, uint, uint>.op_ExclusiveOr((uint)0x00000001, 1));
            Assert.Equal((uint)0x7FFFFFFE, BitwiseOperatorsHelper<uint, uint, uint>.op_ExclusiveOr((uint)0x7FFFFFFF, 1));
            Assert.Equal((uint)0x80000001, BitwiseOperatorsHelper<uint, uint, uint>.op_ExclusiveOr((uint)0x80000000, 1));
            Assert.Equal((uint)0xFFFFFFFE, BitwiseOperatorsHelper<uint, uint, uint>.op_ExclusiveOr((uint)0xFFFFFFFF, 1));
        }

        [Fact]
        public static void op_OnesComplementTest()
        {
            Assert.Equal((uint)0xFFFFFFFF, BitwiseOperatorsHelper<uint, uint, uint>.op_OnesComplement((uint)0x00000000));
            Assert.Equal((uint)0xFFFFFFFE, BitwiseOperatorsHelper<uint, uint, uint>.op_OnesComplement((uint)0x00000001));
            Assert.Equal((uint)0x80000000, BitwiseOperatorsHelper<uint, uint, uint>.op_OnesComplement((uint)0x7FFFFFFF));
            Assert.Equal((uint)0x7FFFFFFF, BitwiseOperatorsHelper<uint, uint, uint>.op_OnesComplement((uint)0x80000000));
            Assert.Equal((uint)0x00000000, BitwiseOperatorsHelper<uint, uint, uint>.op_OnesComplement((uint)0xFFFFFFFF));
        }

        //
        // IComparisonOperators
        //

        [Fact]
        public static void op_GreaterThanTest()
        {
            Assert.False(ComparisonOperatorsHelper<uint, uint, bool>.op_GreaterThan((uint)0x00000000, 1));
            Assert.False(ComparisonOperatorsHelper<uint, uint, bool>.op_GreaterThan((uint)0x00000001, 1));
            Assert.True(ComparisonOperatorsHelper<uint, uint, bool>.op_GreaterThan((uint)0x7FFFFFFF, 1));
            Assert.True(ComparisonOperatorsHelper<uint, uint, bool>.op_GreaterThan((uint)0x80000000, 1));
            Assert.True(ComparisonOperatorsHelper<uint, uint, bool>.op_GreaterThan((uint)0xFFFFFFFF, 1));
        }

        [Fact]
        public static void op_GreaterThanOrEqualTest()
        {
            Assert.False(ComparisonOperatorsHelper<uint, uint, bool>.op_GreaterThanOrEqual((uint)0x00000000, 1));
            Assert.True(ComparisonOperatorsHelper<uint, uint, bool>.op_GreaterThanOrEqual((uint)0x00000001, 1));
            Assert.True(ComparisonOperatorsHelper<uint, uint, bool>.op_GreaterThanOrEqual((uint)0x7FFFFFFF, 1));
            Assert.True(ComparisonOperatorsHelper<uint, uint, bool>.op_GreaterThanOrEqual((uint)0x80000000, 1));
            Assert.True(ComparisonOperatorsHelper<uint, uint, bool>.op_GreaterThanOrEqual((uint)0xFFFFFFFF, 1));
        }

        [Fact]
        public static void op_LessThanTest()
        {
            Assert.True(ComparisonOperatorsHelper<uint, uint, bool>.op_LessThan((uint)0x00000000, 1));
            Assert.False(ComparisonOperatorsHelper<uint, uint, bool>.op_LessThan((uint)0x00000001, 1));
            Assert.False(ComparisonOperatorsHelper<uint, uint, bool>.op_LessThan((uint)0x7FFFFFFF, 1));
            Assert.False(ComparisonOperatorsHelper<uint, uint, bool>.op_LessThan((uint)0x80000000, 1));
            Assert.False(ComparisonOperatorsHelper<uint, uint, bool>.op_LessThan((uint)0xFFFFFFFF, 1));
        }

        [Fact]
        public static void op_LessThanOrEqualTest()
        {
            Assert.True(ComparisonOperatorsHelper<uint, uint, bool>.op_LessThanOrEqual((uint)0x00000000, 1));
            Assert.True(ComparisonOperatorsHelper<uint, uint, bool>.op_LessThanOrEqual((uint)0x00000001, 1));
            Assert.False(ComparisonOperatorsHelper<uint, uint, bool>.op_LessThanOrEqual((uint)0x7FFFFFFF, 1));
            Assert.False(ComparisonOperatorsHelper<uint, uint, bool>.op_LessThanOrEqual((uint)0x80000000, 1));
            Assert.False(ComparisonOperatorsHelper<uint, uint, bool>.op_LessThanOrEqual((uint)0xFFFFFFFF, 1));
        }

        //
        // IDecrementOperators
        //

        [Fact]
        public static void op_DecrementTest()
        {
            Assert.Equal((uint)0xFFFFFFFF, DecrementOperatorsHelper<uint>.op_Decrement((uint)0x00000000));
            Assert.Equal((uint)0x00000000, DecrementOperatorsHelper<uint>.op_Decrement((uint)0x00000001));
            Assert.Equal((uint)0x7FFFFFFE, DecrementOperatorsHelper<uint>.op_Decrement((uint)0x7FFFFFFF));
            Assert.Equal((uint)0x7FFFFFFF, DecrementOperatorsHelper<uint>.op_Decrement((uint)0x80000000));
            Assert.Equal((uint)0xFFFFFFFE, DecrementOperatorsHelper<uint>.op_Decrement((uint)0xFFFFFFFF));
        }

        [Fact]
        public static void op_CheckedDecrementTest()
        {
            Assert.Equal((uint)0x00000000, DecrementOperatorsHelper<uint>.op_CheckedDecrement((uint)0x00000001));
            Assert.Equal((uint)0x7FFFFFFE, DecrementOperatorsHelper<uint>.op_CheckedDecrement((uint)0x7FFFFFFF));
            Assert.Equal((uint)0x7FFFFFFF, DecrementOperatorsHelper<uint>.op_CheckedDecrement((uint)0x80000000));
            Assert.Equal((uint)0xFFFFFFFE, DecrementOperatorsHelper<uint>.op_CheckedDecrement((uint)0xFFFFFFFF));

            Assert.Throws<OverflowException>(() => DecrementOperatorsHelper<uint>.op_CheckedDecrement((uint)0x00000000));
        }

        //
        // IDivisionOperators
        //

        [Fact]
        public static void op_DivisionTest()
        {
            Assert.Equal((uint)0x00000000, DivisionOperatorsHelper<uint, uint, uint>.op_Division((uint)0x00000000, 2));
            Assert.Equal((uint)0x00000000, DivisionOperatorsHelper<uint, uint, uint>.op_Division((uint)0x00000001, 2));
            Assert.Equal((uint)0x3FFFFFFF, DivisionOperatorsHelper<uint, uint, uint>.op_Division((uint)0x7FFFFFFF, 2));
            Assert.Equal((uint)0x40000000, DivisionOperatorsHelper<uint, uint, uint>.op_Division((uint)0x80000000, 2));
            Assert.Equal((uint)0x7FFFFFFF, DivisionOperatorsHelper<uint, uint, uint>.op_Division((uint)0xFFFFFFFF, 2));

            Assert.Throws<DivideByZeroException>(() => DivisionOperatorsHelper<uint, uint, uint>.op_Division((uint)0x00000000, 0));
            Assert.Throws<DivideByZeroException>(() => DivisionOperatorsHelper<uint, uint, uint>.op_Division((uint)0x00000001, 0));
            Assert.Throws<DivideByZeroException>(() => DivisionOperatorsHelper<uint, uint, uint>.op_Division((uint)0xFFFFFFFF, 0));
        }

        [Fact]
        public static void op_CheckedDivisionTest()
        {
            Assert.Equal((uint)0x00000000, DivisionOperatorsHelper<uint, uint, uint>.op_CheckedDivision((uint)0x00000000, 2));
            Assert.Equal((uint)0x00000000, DivisionOperatorsHelper<uint, uint, uint>.op_CheckedDivision((uint)0x00000001, 2));
            Assert.Equal((uint)0x3FFFFFFF, DivisionOperatorsHelper<uint, uint, uint>.op_CheckedDivision((uint)0x7FFFFFFF, 2));
            Assert.Equal((uint)0x40000000, DivisionOperatorsHelper<uint, uint, uint>.op_CheckedDivision((uint)0x80000000, 2));
            Assert.Equal((uint)0x7FFFFFFF, DivisionOperatorsHelper<uint, uint, uint>.op_CheckedDivision((uint)0xFFFFFFFF, 2));

            Assert.Throws<DivideByZeroException>(() => DivisionOperatorsHelper<uint, uint, uint>.op_CheckedDivision((uint)0x00000000, 0));
            Assert.Throws<DivideByZeroException>(() => DivisionOperatorsHelper<uint, uint, uint>.op_CheckedDivision((uint)0x00000001, 0));
            Assert.Throws<DivideByZeroException>(() => DivisionOperatorsHelper<uint, uint, uint>.op_CheckedDivision((uint)0xFFFFFFFF, 0));
        }

        //
        // IEqualityOperators
        //

        [Fact]
        public static void op_EqualityTest()
        {
            Assert.False(EqualityOperatorsHelper<uint, uint, bool>.op_Equality((uint)0x00000000, 1));
            Assert.True(EqualityOperatorsHelper<uint, uint, bool>.op_Equality((uint)0x00000001, 1));
            Assert.False(EqualityOperatorsHelper<uint, uint, bool>.op_Equality((uint)0x7FFFFFFF, 1));
            Assert.False(EqualityOperatorsHelper<uint, uint, bool>.op_Equality((uint)0x80000000, 1));
            Assert.False(EqualityOperatorsHelper<uint, uint, bool>.op_Equality((uint)0xFFFFFFFF, 1));
        }

        [Fact]
        public static void op_InequalityTest()
        {
            Assert.True(EqualityOperatorsHelper<uint, uint, bool>.op_Inequality((uint)0x00000000, 1));
            Assert.False(EqualityOperatorsHelper<uint, uint, bool>.op_Inequality((uint)0x00000001, 1));
            Assert.True(EqualityOperatorsHelper<uint, uint, bool>.op_Inequality((uint)0x7FFFFFFF, 1));
            Assert.True(EqualityOperatorsHelper<uint, uint, bool>.op_Inequality((uint)0x80000000, 1));
            Assert.True(EqualityOperatorsHelper<uint, uint, bool>.op_Inequality((uint)0xFFFFFFFF, 1));
        }

        //
        // IIncrementOperators
        //

        [Fact]
        public static void op_IncrementTest()
        {
            Assert.Equal((uint)0x00000001, IncrementOperatorsHelper<uint>.op_Increment((uint)0x00000000));
            Assert.Equal((uint)0x00000002, IncrementOperatorsHelper<uint>.op_Increment((uint)0x00000001));
            Assert.Equal((uint)0x80000000, IncrementOperatorsHelper<uint>.op_Increment((uint)0x7FFFFFFF));
            Assert.Equal((uint)0x80000001, IncrementOperatorsHelper<uint>.op_Increment((uint)0x80000000));
            Assert.Equal((uint)0x00000000, IncrementOperatorsHelper<uint>.op_Increment((uint)0xFFFFFFFF));
        }

        [Fact]
        public static void op_CheckedIncrementTest()
        {
            Assert.Equal((uint)0x00000001, IncrementOperatorsHelper<uint>.op_CheckedIncrement((uint)0x00000000));
            Assert.Equal((uint)0x00000002, IncrementOperatorsHelper<uint>.op_CheckedIncrement((uint)0x00000001));
            Assert.Equal((uint)0x80000000, IncrementOperatorsHelper<uint>.op_CheckedIncrement((uint)0x7FFFFFFF));
            Assert.Equal((uint)0x80000001, IncrementOperatorsHelper<uint>.op_CheckedIncrement((uint)0x80000000));

            Assert.Throws<OverflowException>(() => IncrementOperatorsHelper<uint>.op_CheckedIncrement((uint)0xFFFFFFFF));
        }

        //
        // IMinMaxValue
        //

        [Fact]
        public static void MaxValueTest()
        {
            Assert.Equal((uint)0xFFFFFFFF, MinMaxValueHelper<uint>.MaxValue);
        }

        [Fact]
        public static void MinValueTest()
        {
            Assert.Equal((uint)0x00000000, MinMaxValueHelper<uint>.MinValue);
        }

        //
        // IModulusOperators
        //

        [Fact]
        public static void op_ModulusTest()
        {
            Assert.Equal((uint)0x00000000, ModulusOperatorsHelper<uint, uint, uint>.op_Modulus((uint)0x00000000, 2));
            Assert.Equal((uint)0x00000001, ModulusOperatorsHelper<uint, uint, uint>.op_Modulus((uint)0x00000001, 2));
            Assert.Equal((uint)0x00000001, ModulusOperatorsHelper<uint, uint, uint>.op_Modulus((uint)0x7FFFFFFF, 2));
            Assert.Equal((uint)0x00000000, ModulusOperatorsHelper<uint, uint, uint>.op_Modulus((uint)0x80000000, 2));
            Assert.Equal((uint)0x00000001, ModulusOperatorsHelper<uint, uint, uint>.op_Modulus((uint)0xFFFFFFFF, 2));

            Assert.Throws<DivideByZeroException>(() => ModulusOperatorsHelper<uint, uint, uint>.op_Modulus((uint)0x00000001, 0));
        }

        //
        // IMultiplicativeIdentity
        //

        [Fact]
        public static void MultiplicativeIdentityTest()
        {
            Assert.Equal((uint)0x00000001, MultiplicativeIdentityHelper<uint, uint>.MultiplicativeIdentity);
        }

        //
        // IMultiplyOperators
        //

        [Fact]
        public static void op_MultiplyTest()
        {
            Assert.Equal((uint)0x00000000, MultiplyOperatorsHelper<uint, uint, uint>.op_Multiply((uint)0x00000000, 2));
            Assert.Equal((uint)0x00000002, MultiplyOperatorsHelper<uint, uint, uint>.op_Multiply((uint)0x00000001, 2));
            Assert.Equal((uint)0xFFFFFFFE, MultiplyOperatorsHelper<uint, uint, uint>.op_Multiply((uint)0x7FFFFFFF, 2));
            Assert.Equal((uint)0x00000000, MultiplyOperatorsHelper<uint, uint, uint>.op_Multiply((uint)0x80000000, 2));
            Assert.Equal((uint)0xFFFFFFFE, MultiplyOperatorsHelper<uint, uint, uint>.op_Multiply((uint)0xFFFFFFFF, 2));
        }

        [Fact]
        public static void op_CheckedMultiplyTest()
        {
            Assert.Equal((uint)0x00000000, MultiplyOperatorsHelper<uint, uint, uint>.op_CheckedMultiply((uint)0x00000000, 2));
            Assert.Equal((uint)0x00000002, MultiplyOperatorsHelper<uint, uint, uint>.op_CheckedMultiply((uint)0x00000001, 2));
            Assert.Equal((uint)0xFFFFFFFE, MultiplyOperatorsHelper<uint, uint, uint>.op_CheckedMultiply((uint)0x7FFFFFFF, 2));

            Assert.Throws<OverflowException>(() => MultiplyOperatorsHelper<uint, uint, uint>.op_CheckedMultiply((uint)0x80000000, 2));
            Assert.Throws<OverflowException>(() => MultiplyOperatorsHelper<uint, uint, uint>.op_CheckedMultiply((uint)0xFFFFFFFF, 2));
        }

        //
        // INumber
        //

        [Fact]
        public static void ClampTest()
        {
            Assert.Equal((uint)0x00000001, NumberHelper<uint>.Clamp((uint)0x00000000, 0x0001, 0x003F));
            Assert.Equal((uint)0x00000001, NumberHelper<uint>.Clamp((uint)0x00000001, 0x0001, 0x003F));
            Assert.Equal((uint)0x0000003F, NumberHelper<uint>.Clamp((uint)0x7FFFFFFF, 0x0001, 0x003F));
            Assert.Equal((uint)0x0000003F, NumberHelper<uint>.Clamp((uint)0x80000000, 0x0001, 0x003F));
            Assert.Equal((uint)0x0000003F, NumberHelper<uint>.Clamp((uint)0xFFFFFFFF, 0x0001, 0x003F));
        }

        [Fact]
        public static void MaxTest()
        {
            Assert.Equal((uint)0x00000001, NumberHelper<uint>.Max((uint)0x00000000, 1));
            Assert.Equal((uint)0x00000001, NumberHelper<uint>.Max((uint)0x00000001, 1));
            Assert.Equal((uint)0x7FFFFFFF, NumberHelper<uint>.Max((uint)0x7FFFFFFF, 1));
            Assert.Equal((uint)0x80000000, NumberHelper<uint>.Max((uint)0x80000000, 1));
            Assert.Equal((uint)0xFFFFFFFF, NumberHelper<uint>.Max((uint)0xFFFFFFFF, 1));
        }

        [Fact]
        public static void MaxNumberTest()
        {
            Assert.Equal((uint)0x00000001, NumberHelper<uint>.MaxNumber((uint)0x00000000, 1));
            Assert.Equal((uint)0x00000001, NumberHelper<uint>.MaxNumber((uint)0x00000001, 1));
            Assert.Equal((uint)0x7FFFFFFF, NumberHelper<uint>.MaxNumber((uint)0x7FFFFFFF, 1));
            Assert.Equal((uint)0x80000000, NumberHelper<uint>.MaxNumber((uint)0x80000000, 1));
            Assert.Equal((uint)0xFFFFFFFF, NumberHelper<uint>.MaxNumber((uint)0xFFFFFFFF, 1));
        }

        [Fact]
        public static void MinTest()
        {
            Assert.Equal((uint)0x00000000, NumberHelper<uint>.Min((uint)0x00000000, 1));
            Assert.Equal((uint)0x00000001, NumberHelper<uint>.Min((uint)0x00000001, 1));
            Assert.Equal((uint)0x00000001, NumberHelper<uint>.Min((uint)0x7FFFFFFF, 1));
            Assert.Equal((uint)0x00000001, NumberHelper<uint>.Min((uint)0x80000000, 1));
            Assert.Equal((uint)0x00000001, NumberHelper<uint>.Min((uint)0xFFFFFFFF, 1));
        }

        [Fact]
        public static void MinNumberTest()
        {
            Assert.Equal((uint)0x00000000, NumberHelper<uint>.MinNumber((uint)0x00000000, 1));
            Assert.Equal((uint)0x00000001, NumberHelper<uint>.MinNumber((uint)0x00000001, 1));
            Assert.Equal((uint)0x00000001, NumberHelper<uint>.MinNumber((uint)0x7FFFFFFF, 1));
            Assert.Equal((uint)0x00000001, NumberHelper<uint>.MinNumber((uint)0x80000000, 1));
            Assert.Equal((uint)0x00000001, NumberHelper<uint>.MinNumber((uint)0xFFFFFFFF, 1));
        }

        [Fact]
        public static void SignTest()
        {
            Assert.Equal(0, NumberHelper<uint>.Sign((uint)0x00000000));
            Assert.Equal(1, NumberHelper<uint>.Sign((uint)0x00000001));
            Assert.Equal(1, NumberHelper<uint>.Sign((uint)0x7FFFFFFF));
            Assert.Equal(1, NumberHelper<uint>.Sign((uint)0x80000000));
            Assert.Equal(1, NumberHelper<uint>.Sign((uint)0xFFFFFFFF));
        }

        //
        // INumberBase
        //

        [Fact]
        public static void OneTest()
        {
            Assert.Equal((uint)0x00000001, NumberBaseHelper<uint>.One);
        }

        [Fact]
        public static void RadixTest()
        {
            Assert.Equal(2, NumberBaseHelper<uint>.Radix);
        }

        [Fact]
        public static void ZeroTest()
        {
            Assert.Equal((uint)0x00000000, NumberBaseHelper<uint>.Zero);
        }

        [Fact]
        public static void AbsTest()
        {
            Assert.Equal((uint)0x00000000, NumberBaseHelper<uint>.Abs((uint)0x00000000));
            Assert.Equal((uint)0x00000001, NumberBaseHelper<uint>.Abs((uint)0x00000001));
            Assert.Equal((uint)0x7FFFFFFF, NumberBaseHelper<uint>.Abs((uint)0x7FFFFFFF));
            Assert.Equal((uint)0x80000000, NumberBaseHelper<uint>.Abs((uint)0x80000000));
            Assert.Equal((uint)0xFFFFFFFF, NumberBaseHelper<uint>.Abs((uint)0xFFFFFFFF));
        }

        [Fact]
        public static void CreateCheckedFromByteTest()
        {
            Assert.Equal((uint)0x00000000, NumberBaseHelper<uint>.CreateChecked<byte>(0x00));
            Assert.Equal((uint)0x00000001, NumberBaseHelper<uint>.CreateChecked<byte>(0x01));
            Assert.Equal((uint)0x0000007F, NumberBaseHelper<uint>.CreateChecked<byte>(0x7F));
            Assert.Equal((uint)0x00000080, NumberBaseHelper<uint>.CreateChecked<byte>(0x80));
            Assert.Equal((uint)0x000000FF, NumberBaseHelper<uint>.CreateChecked<byte>(0xFF));
        }

        [Fact]
        public static void CreateCheckedFromCharTest()
        {
            Assert.Equal((uint)0x00000000, NumberBaseHelper<uint>.CreateChecked<char>((char)0x0000));
            Assert.Equal((uint)0x00000001, NumberBaseHelper<uint>.CreateChecked<char>((char)0x0001));
            Assert.Equal((uint)0x00007FFF, NumberBaseHelper<uint>.CreateChecked<char>((char)0x7FFF));
            Assert.Equal((uint)0x00008000, NumberBaseHelper<uint>.CreateChecked<char>((char)0x8000));
            Assert.Equal((uint)0x0000FFFF, NumberBaseHelper<uint>.CreateChecked<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateCheckedFromDecimalTest()
        {
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateChecked<decimal>(-0.0m));
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateChecked<decimal>(+0.0m));
            Assert.Equal((uint)0x0000_0001, NumberBaseHelper<uint>.CreateChecked<decimal>(+1.0m));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<uint>.CreateChecked<decimal>(decimal.MinValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<uint>.CreateChecked<decimal>(decimal.MaxValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<uint>.CreateChecked<decimal>(decimal.MinusOne));
        }

        [Fact]
        public static void CreateCheckedFromDoubleTest()
        {
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateChecked<double>(+0.0));
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateChecked<double>(-0.0));


            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateChecked<double>(-double.Epsilon));
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateChecked<double>(+double.Epsilon));

            Assert.Equal((uint)0x0000_0001, NumberBaseHelper<uint>.CreateChecked<double>(+1.0));
            Assert.Equal((uint)0xFFFF_FFFF, NumberBaseHelper<uint>.CreateChecked<double>(+4294967295.0));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<uint>.CreateChecked<double>(-1.0));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<uint>.CreateChecked<double>(+4294967296.0));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<uint>.CreateChecked<double>(double.PositiveInfinity));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<uint>.CreateChecked<double>(double.NegativeInfinity));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<uint>.CreateChecked<double>(double.MaxValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<uint>.CreateChecked<double>(double.MinValue));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<uint>.CreateChecked<double>(double.NaN));
        }

        [Fact]
        public static void CreateCheckedFromHalfTest()
        {
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateChecked<Half>(Half.Zero));
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateChecked<Half>(Half.NegativeZero));

            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateChecked<Half>(-Half.Epsilon));
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateChecked<Half>(+Half.Epsilon));

            Assert.Equal((uint)0x0000_0001, NumberBaseHelper<uint>.CreateChecked<Half>(Half.One));
            Assert.Equal((uint)0x0000_FFE0, NumberBaseHelper<uint>.CreateChecked<Half>(Half.MaxValue));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<uint>.CreateChecked<Half>(Half.NegativeOne));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<uint>.CreateChecked<Half>(Half.PositiveInfinity));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<uint>.CreateChecked<Half>(Half.NegativeInfinity));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<uint>.CreateChecked<Half>(Half.MinValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<uint>.CreateChecked<Half>(Half.NaN));
        }

        [Fact]
        public static void CreateCheckedFromInt16Test()
        {
            Assert.Equal((uint)0x00000000, NumberBaseHelper<uint>.CreateChecked<short>(0x0000));
            Assert.Equal((uint)0x00000001, NumberBaseHelper<uint>.CreateChecked<short>(0x0001));
            Assert.Equal((uint)0x00007FFF, NumberBaseHelper<uint>.CreateChecked<short>(0x7FFF));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<uint>.CreateChecked<short>(unchecked((short)0x8000)));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<uint>.CreateChecked<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromInt32Test()
        {
            Assert.Equal((uint)0x00000000, NumberBaseHelper<uint>.CreateChecked<int>(0x00000000));
            Assert.Equal((uint)0x00000001, NumberBaseHelper<uint>.CreateChecked<int>(0x00000001));
            Assert.Equal((uint)0x7FFFFFFF, NumberBaseHelper<uint>.CreateChecked<int>(0x7FFFFFFF));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<uint>.CreateChecked<int>(unchecked((int)0x80000000)));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<uint>.CreateChecked<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromInt64Test()
        {
            Assert.Equal((uint)0x00000000, NumberBaseHelper<uint>.CreateChecked<long>(0x0000000000000000));
            Assert.Equal((uint)0x00000001, NumberBaseHelper<uint>.CreateChecked<long>(0x0000000000000001));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<uint>.CreateChecked<long>(0x7FFFFFFFFFFFFFFF));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<uint>.CreateChecked<long>(unchecked((long)0x8000000000000000)));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<uint>.CreateChecked<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromInt128Test()
        {
            Assert.Equal((uint)0x00000000, NumberBaseHelper<uint>.CreateChecked<Int128>(Int128.Zero));
            Assert.Equal((uint)0x00000001, NumberBaseHelper<uint>.CreateChecked<Int128>(Int128.One));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<uint>.CreateChecked<Int128>(Int128.MaxValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<uint>.CreateChecked<Int128>(Int128.MinValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<uint>.CreateChecked<Int128>(Int128.NegativeOne));
        }

        [Fact]
        public static void CreateCheckedFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((uint)0x00000000, NumberBaseHelper<uint>.CreateChecked<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal((uint)0x00000001, NumberBaseHelper<uint>.CreateChecked<nint>(unchecked((nint)0x0000000000000001)));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<uint>.CreateChecked<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<uint>.CreateChecked<nint>(unchecked((nint)0x8000000000000000)));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<uint>.CreateChecked<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((uint)0x00000000, NumberBaseHelper<uint>.CreateChecked<nint>((nint)0x00000000));
                Assert.Equal((uint)0x00000001, NumberBaseHelper<uint>.CreateChecked<nint>((nint)0x00000001));
                Assert.Equal((uint)0x7FFFFFFF, NumberBaseHelper<uint>.CreateChecked<nint>((nint)0x7FFFFFFF));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<uint>.CreateChecked<nint>(unchecked((nint)0x80000000)));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<uint>.CreateChecked<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateCheckedFromNFloatTest()
        {
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateChecked<NFloat>(0.0f));
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateChecked<NFloat>(NFloat.NegativeZero));

            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateChecked<NFloat>(-NFloat.Epsilon));
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateChecked<NFloat>(+NFloat.Epsilon));

            if (Environment.Is64BitProcess)
            {
                Assert.Equal((uint)0x0000_0001, NumberBaseHelper<uint>.CreateChecked<NFloat>(1.0f));
                Assert.Equal((uint)0xFFFF_FFFF, NumberBaseHelper<uint>.CreateChecked<NFloat>((NFloat)(4294967295.0)));

                Assert.Throws<OverflowException>(() => NumberBaseHelper<uint>.CreateChecked<NFloat>(-1.0f));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<uint>.CreateChecked<NFloat>(+4294967296.0f));
            }
            else
            {
                Assert.Equal((uint)0x0000_0001, NumberBaseHelper<uint>.CreateChecked<NFloat>(1.0f));
                Assert.Equal((uint)0xFFFF_FF00, NumberBaseHelper<uint>.CreateChecked<NFloat>(4294967040.0f));

                Assert.Throws<OverflowException>(() => NumberBaseHelper<uint>.CreateChecked<NFloat>(-1.0f));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<uint>.CreateChecked<NFloat>(+4294967296.0f));
            }

            Assert.Throws<OverflowException>(() => NumberBaseHelper<uint>.CreateChecked<NFloat>(NFloat.PositiveInfinity));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<uint>.CreateChecked<NFloat>(NFloat.NegativeInfinity));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<uint>.CreateChecked<NFloat>(NFloat.MaxValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<uint>.CreateChecked<NFloat>(NFloat.MinValue));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<uint>.CreateChecked<NFloat>(NFloat.NaN));
        }

        [Fact]
        public static void CreateCheckedFromSByteTest()
        {
            Assert.Equal((uint)0x00000000, NumberBaseHelper<uint>.CreateChecked<sbyte>(0x00));
            Assert.Equal((uint)0x00000001, NumberBaseHelper<uint>.CreateChecked<sbyte>(0x01));
            Assert.Equal((uint)0x0000007F, NumberBaseHelper<uint>.CreateChecked<sbyte>(0x7F));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<uint>.CreateChecked<sbyte>(unchecked((sbyte)0x80)));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<uint>.CreateChecked<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateCheckedFromSingleTest()
        {
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateChecked<float>(+0.0f));
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateChecked<float>(-0.0f));

            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateChecked<float>(-float.Epsilon));
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateChecked<float>(-float.Epsilon));

            Assert.Equal((uint)0x0000_0001, NumberBaseHelper<uint>.CreateChecked<float>(+1.0f));
            Assert.Equal((uint)0xFFFF_FF00, NumberBaseHelper<uint>.CreateChecked<float>(+4294967040.0f));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<uint>.CreateChecked<float>(-1.0f));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<uint>.CreateChecked<float>(+4294967296.0f));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<uint>.CreateChecked<float>(float.PositiveInfinity));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<uint>.CreateChecked<float>(float.NegativeInfinity));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<uint>.CreateChecked<float>(float.MaxValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<uint>.CreateChecked<float>(float.MinValue));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<uint>.CreateChecked<float>(float.NaN));
        }

        [Fact]
        public static void CreateCheckedFromUInt16Test()
        {
            Assert.Equal((uint)0x00000000, NumberBaseHelper<uint>.CreateChecked<ushort>(0x0000));
            Assert.Equal((uint)0x00000001, NumberBaseHelper<uint>.CreateChecked<ushort>(0x0001));
            Assert.Equal((uint)0x00007FFF, NumberBaseHelper<uint>.CreateChecked<ushort>(0x7FFF));
            Assert.Equal((uint)0x00008000, NumberBaseHelper<uint>.CreateChecked<ushort>(0x8000));
            Assert.Equal((uint)0x0000FFFF, NumberBaseHelper<uint>.CreateChecked<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUInt32Test()
        {
            Assert.Equal((uint)0x00000000, NumberBaseHelper<uint>.CreateChecked<uint>(0x00000000));
            Assert.Equal((uint)0x00000001, NumberBaseHelper<uint>.CreateChecked<uint>(0x00000001));
            Assert.Equal((uint)0x7FFFFFFF, NumberBaseHelper<uint>.CreateChecked<uint>(0x7FFFFFFF));
            Assert.Equal((uint)0x80000000, NumberBaseHelper<uint>.CreateChecked<uint>(0x80000000));
            Assert.Equal((uint)0xFFFFFFFF, NumberBaseHelper<uint>.CreateChecked<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUInt64Test()
        {
            Assert.Equal((uint)0x00000000, NumberBaseHelper<uint>.CreateChecked<ulong>(0x0000000000000000));
            Assert.Equal((uint)0x00000001, NumberBaseHelper<uint>.CreateChecked<ulong>(0x0000000000000001));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<uint>.CreateChecked<ulong>(0x7FFFFFFFFFFFFFFF));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<uint>.CreateChecked<ulong>(0x8000000000000000));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<uint>.CreateChecked<ulong>(0xFFFFFFFFFFFFFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUInt128Test()
        {
            Assert.Equal((uint)0x00000000, NumberBaseHelper<uint>.CreateChecked<UInt128>(UInt128.Zero));
            Assert.Equal((uint)0x00000001, NumberBaseHelper<uint>.CreateChecked<UInt128>(UInt128.One));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<uint>.CreateChecked<UInt128>(UInt128Tests_GenericMath.Int128MaxValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<uint>.CreateChecked<UInt128>(UInt128Tests_GenericMath.Int128MaxValuePlusOne));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<uint>.CreateChecked<UInt128>(UInt128.MaxValue));
        }

        [Fact]
        public static void CreateCheckedFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((uint)0x00000000, NumberBaseHelper<uint>.CreateChecked<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal((uint)0x00000001, NumberBaseHelper<uint>.CreateChecked<nuint>(unchecked((nuint)0x0000000000000001)));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<uint>.CreateChecked<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<uint>.CreateChecked<nuint>(unchecked((nuint)0x8000000000000000)));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<uint>.CreateChecked<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((uint)0x00000000, NumberBaseHelper<uint>.CreateChecked<nuint>((nuint)0x00000000));
                Assert.Equal((uint)0x00000001, NumberBaseHelper<uint>.CreateChecked<nuint>((nuint)0x00000001));
                Assert.Equal((uint)0x7FFFFFFF, NumberBaseHelper<uint>.CreateChecked<nuint>((nuint)0x7FFFFFFF));
                Assert.Equal((uint)0x80000000, NumberBaseHelper<uint>.CreateChecked<nuint>((nuint)0x80000000));
                Assert.Equal((uint)0xFFFFFFFF, NumberBaseHelper<uint>.CreateChecked<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateSaturatingFromByteTest()
        {
            Assert.Equal((uint)0x00000000, NumberBaseHelper<uint>.CreateSaturating<byte>(0x00));
            Assert.Equal((uint)0x00000001, NumberBaseHelper<uint>.CreateSaturating<byte>(0x01));
            Assert.Equal((uint)0x0000007F, NumberBaseHelper<uint>.CreateSaturating<byte>(0x7F));
            Assert.Equal((uint)0x00000080, NumberBaseHelper<uint>.CreateSaturating<byte>(0x80));
            Assert.Equal((uint)0x000000FF, NumberBaseHelper<uint>.CreateSaturating<byte>(0xFF));
        }

        [Fact]
        public static void CreateSaturatingFromCharTest()
        {
            Assert.Equal((uint)0x00000000, NumberBaseHelper<uint>.CreateSaturating<char>((char)0x0000));
            Assert.Equal((uint)0x00000001, NumberBaseHelper<uint>.CreateSaturating<char>((char)0x0001));
            Assert.Equal((uint)0x00007FFF, NumberBaseHelper<uint>.CreateSaturating<char>((char)0x7FFF));
            Assert.Equal((uint)0x00008000, NumberBaseHelper<uint>.CreateSaturating<char>((char)0x8000));
            Assert.Equal((uint)0x0000FFFF, NumberBaseHelper<uint>.CreateSaturating<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromDecimalTest()
        {
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateSaturating<decimal>(-0.0m));
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateSaturating<decimal>(+0.0m));
            Assert.Equal((uint)0x0000_0001, NumberBaseHelper<uint>.CreateSaturating<decimal>(+1.0m));

            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateSaturating<decimal>(decimal.MinValue));
            Assert.Equal((uint)0xFFFF_FFFF, NumberBaseHelper<uint>.CreateSaturating<decimal>(decimal.MaxValue));
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateSaturating<decimal>(decimal.MinusOne));
        }

        [Fact]
        public static void CreateSaturatingFromDoubleTest()
        {
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateSaturating<double>(+0.0));
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateSaturating<double>(-0.0));


            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateSaturating<double>(-double.Epsilon));
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateSaturating<double>(+double.Epsilon));

            Assert.Equal((uint)0x0000_0001, NumberBaseHelper<uint>.CreateSaturating<double>(+1.0));
            Assert.Equal((uint)0xFFFF_FFFF, NumberBaseHelper<uint>.CreateSaturating<double>(+4294967295.0));

            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateSaturating<double>(-1.0));
            Assert.Equal((uint)0xFFFF_FFFF, NumberBaseHelper<uint>.CreateSaturating<double>(+4294967296.0));

            Assert.Equal((uint)0xFFFF_FFFF, NumberBaseHelper<uint>.CreateSaturating<double>(double.PositiveInfinity));
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateSaturating<double>(double.NegativeInfinity));

            Assert.Equal((uint)0xFFFF_FFFF, NumberBaseHelper<uint>.CreateSaturating<double>(double.MaxValue));
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateSaturating<double>(double.MinValue));

            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateSaturating<double>(double.NaN));
        }

        [Fact]
        public static void CreateSaturatingFromHalfTest()
        {
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateSaturating<Half>(Half.Zero));
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateSaturating<Half>(Half.NegativeZero));

            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateSaturating<Half>(-Half.Epsilon));
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateSaturating<Half>(+Half.Epsilon));

            Assert.Equal((uint)0x0000_0001, NumberBaseHelper<uint>.CreateSaturating<Half>(Half.One));
            Assert.Equal((uint)0x0000_FFE0, NumberBaseHelper<uint>.CreateSaturating<Half>(Half.MaxValue));

            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateSaturating<Half>(Half.NegativeOne));

            Assert.Equal((uint)0xFFFF_FFFF, NumberBaseHelper<uint>.CreateSaturating<Half>(Half.PositiveInfinity));
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateSaturating<Half>(Half.NegativeInfinity));

            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateSaturating<Half>(Half.MinValue));
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateSaturating<Half>(Half.NaN));
        }

        [Fact]
        public static void CreateSaturatingFromInt16Test()
        {
            Assert.Equal((uint)0x00000000, NumberBaseHelper<uint>.CreateSaturating<short>(0x0000));
            Assert.Equal((uint)0x00000001, NumberBaseHelper<uint>.CreateSaturating<short>(0x0001));
            Assert.Equal((uint)0x00007FFF, NumberBaseHelper<uint>.CreateSaturating<short>(0x7FFF));
            Assert.Equal((uint)0x00000000, NumberBaseHelper<uint>.CreateSaturating<short>(unchecked((short)0x8000)));
            Assert.Equal((uint)0x00000000, NumberBaseHelper<uint>.CreateSaturating<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromInt32Test()
        {
            Assert.Equal((uint)0x00000000, NumberBaseHelper<uint>.CreateSaturating<int>(0x00000000));
            Assert.Equal((uint)0x00000001, NumberBaseHelper<uint>.CreateSaturating<int>(0x00000001));
            Assert.Equal((uint)0x7FFFFFFF, NumberBaseHelper<uint>.CreateSaturating<int>(0x7FFFFFFF));
            Assert.Equal((uint)0x00000000, NumberBaseHelper<uint>.CreateSaturating<int>(unchecked((int)0x80000000)));
            Assert.Equal((uint)0x00000000, NumberBaseHelper<uint>.CreateSaturating<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromInt64Test()
        {
            Assert.Equal((uint)0x00000000, NumberBaseHelper<uint>.CreateSaturating<long>(0x0000000000000000));
            Assert.Equal((uint)0x00000001, NumberBaseHelper<uint>.CreateSaturating<long>(0x0000000000000001));
            Assert.Equal((uint)0xFFFFFFFF, NumberBaseHelper<uint>.CreateSaturating<long>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal((uint)0x00000000, NumberBaseHelper<uint>.CreateSaturating<long>(unchecked((long)0x8000000000000000)));
            Assert.Equal((uint)0x00000000, NumberBaseHelper<uint>.CreateSaturating<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromInt128Test()
        {
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateSaturating<Int128>(Int128.Zero));
            Assert.Equal((uint)0x0000_0001, NumberBaseHelper<uint>.CreateSaturating<Int128>(Int128.One));
            Assert.Equal((uint)0xFFFF_FFFF, NumberBaseHelper<uint>.CreateSaturating<Int128>(Int128.MaxValue));
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateSaturating<Int128>(Int128.MinValue));
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateSaturating<Int128>(Int128.NegativeOne));
        }

        [Fact]
        public static void CreateSaturatingFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((uint)0x00000000, NumberBaseHelper<uint>.CreateSaturating<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal((uint)0x00000001, NumberBaseHelper<uint>.CreateSaturating<nint>(unchecked((nint)0x0000000000000001)));
                Assert.Equal((uint)0xFFFFFFFF, NumberBaseHelper<uint>.CreateSaturating<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal((uint)0x00000000, NumberBaseHelper<uint>.CreateSaturating<nint>(unchecked((nint)0x8000000000000000)));
                Assert.Equal((uint)0x00000000, NumberBaseHelper<uint>.CreateSaturating<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((uint)0x00000000, NumberBaseHelper<uint>.CreateSaturating<nint>((nint)0x00000000));
                Assert.Equal((uint)0x00000001, NumberBaseHelper<uint>.CreateSaturating<nint>((nint)0x00000001));
                Assert.Equal((uint)0x7FFFFFFF, NumberBaseHelper<uint>.CreateSaturating<nint>((nint)0x7FFFFFFF));
                Assert.Equal((uint)0x00000000, NumberBaseHelper<uint>.CreateSaturating<nint>(unchecked((nint)0x80000000)));
                Assert.Equal((uint)0x00000000, NumberBaseHelper<uint>.CreateSaturating<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateSaturatingFromNFloatTest()
        {
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateSaturating<NFloat>(0.0f));
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateSaturating<NFloat>(NFloat.NegativeZero));

            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateSaturating<NFloat>(-NFloat.Epsilon));
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateSaturating<NFloat>(+NFloat.Epsilon));

            if (Environment.Is64BitProcess)
            {
                Assert.Equal((uint)0x0000_0001, NumberBaseHelper<uint>.CreateSaturating<NFloat>(1.0f));
                Assert.Equal((uint)0xFFFF_FFFF, NumberBaseHelper<uint>.CreateSaturating<NFloat>((NFloat)(4294967295.0)));

                Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateSaturating<NFloat>(-1.0f));
                Assert.Equal((uint)0xFFFF_FFFF, NumberBaseHelper<uint>.CreateSaturating<NFloat>(+4294967296.0f));
            }
            else
            {
                Assert.Equal((uint)0x0000_0001, NumberBaseHelper<uint>.CreateSaturating<NFloat>(1.0f));
                Assert.Equal((uint)0xFFFF_FF00, NumberBaseHelper<uint>.CreateSaturating<NFloat>(4294967040.0f));

                Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateSaturating<NFloat>(-1.0f));
                Assert.Equal((uint)0xFFFF_FFFF, NumberBaseHelper<uint>.CreateSaturating<NFloat>(+4294967296.0f));
            }

            Assert.Equal((uint)0xFFFF_FFFF, NumberBaseHelper<uint>.CreateSaturating<NFloat>(NFloat.PositiveInfinity));
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateSaturating<NFloat>(NFloat.NegativeInfinity));

            Assert.Equal((uint)0xFFFF_FFFF, NumberBaseHelper<uint>.CreateSaturating<NFloat>(NFloat.MaxValue));
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateSaturating<NFloat>(NFloat.MinValue));

            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateSaturating<NFloat>(NFloat.NaN));
        }

        [Fact]
        public static void CreateSaturatingFromSByteTest()
        {
            Assert.Equal((uint)0x00000000, NumberBaseHelper<uint>.CreateSaturating<sbyte>(0x00));
            Assert.Equal((uint)0x00000001, NumberBaseHelper<uint>.CreateSaturating<sbyte>(0x01));
            Assert.Equal((uint)0x0000007F, NumberBaseHelper<uint>.CreateSaturating<sbyte>(0x7F));
            Assert.Equal((uint)0x00000000, NumberBaseHelper<uint>.CreateSaturating<sbyte>(unchecked((sbyte)0x80)));
            Assert.Equal((uint)0x00000000, NumberBaseHelper<uint>.CreateSaturating<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateSaturatingFromSingleTest()
        {
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateSaturating<float>(+0.0f));
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateSaturating<float>(-0.0f));

            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateSaturating<float>(-float.Epsilon));
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateSaturating<float>(-float.Epsilon));

            Assert.Equal((uint)0x0000_0001, NumberBaseHelper<uint>.CreateSaturating<float>(+1.0f));
            Assert.Equal((uint)0xFFFF_FF00, NumberBaseHelper<uint>.CreateSaturating<float>(+4294967040.0f));

            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateSaturating<float>(-1.0f));
            Assert.Equal((uint)0xFFFF_FFFF, NumberBaseHelper<uint>.CreateSaturating<float>(+4294967296.0f));

            Assert.Equal((uint)0xFFFF_FFFF, NumberBaseHelper<uint>.CreateSaturating<float>(float.PositiveInfinity));
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateSaturating<float>(float.NegativeInfinity));

            Assert.Equal((uint)0xFFFF_FFFF, NumberBaseHelper<uint>.CreateSaturating<float>(float.MaxValue));
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateSaturating<float>(float.MinValue));

            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateSaturating<float>(float.NaN));
        }

        [Fact]
        public static void CreateSaturatingFromUInt16Test()
        {
            Assert.Equal((uint)0x00000000, NumberBaseHelper<uint>.CreateSaturating<ushort>(0x0000));
            Assert.Equal((uint)0x00000001, NumberBaseHelper<uint>.CreateSaturating<ushort>(0x0001));
            Assert.Equal((uint)0x00007FFF, NumberBaseHelper<uint>.CreateSaturating<ushort>(0x7FFF));
            Assert.Equal((uint)0x00008000, NumberBaseHelper<uint>.CreateSaturating<ushort>(0x8000));
            Assert.Equal((uint)0x0000FFFF, NumberBaseHelper<uint>.CreateSaturating<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUInt32Test()
        {
            Assert.Equal((uint)0x00000000, NumberBaseHelper<uint>.CreateSaturating<uint>(0x00000000));
            Assert.Equal((uint)0x00000001, NumberBaseHelper<uint>.CreateSaturating<uint>(0x00000001));
            Assert.Equal((uint)0x7FFFFFFF, NumberBaseHelper<uint>.CreateSaturating<uint>(0x7FFFFFFF));
            Assert.Equal((uint)0x80000000, NumberBaseHelper<uint>.CreateSaturating<uint>(0x80000000));
            Assert.Equal((uint)0xFFFFFFFF, NumberBaseHelper<uint>.CreateSaturating<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUInt64Test()
        {
            Assert.Equal((uint)0x00000000, NumberBaseHelper<uint>.CreateSaturating<ulong>(0x0000000000000000));
            Assert.Equal((uint)0x00000001, NumberBaseHelper<uint>.CreateSaturating<ulong>(0x0000000000000001));
            Assert.Equal((uint)0xFFFFFFFF, NumberBaseHelper<uint>.CreateSaturating<ulong>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal((uint)0xFFFFFFFF, NumberBaseHelper<uint>.CreateSaturating<ulong>(0x8000000000000000));
            Assert.Equal((uint)0xFFFFFFFF, NumberBaseHelper<uint>.CreateSaturating<ulong>(0xFFFFFFFFFFFFFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUInt128Test()
        {
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateSaturating<UInt128>(UInt128.Zero));
            Assert.Equal((uint)0x0000_0001, NumberBaseHelper<uint>.CreateSaturating<UInt128>(UInt128.One));
            Assert.Equal((uint)0xFFFF_FFFF, NumberBaseHelper<uint>.CreateSaturating<UInt128>(UInt128Tests_GenericMath.Int128MaxValue));
            Assert.Equal((uint)0xFFFF_FFFF, NumberBaseHelper<uint>.CreateSaturating<UInt128>(UInt128Tests_GenericMath.Int128MaxValuePlusOne));
            Assert.Equal((uint)0xFFFF_FFFF, NumberBaseHelper<uint>.CreateSaturating<UInt128>(UInt128.MaxValue));
        }

        [Fact]
        public static void CreateSaturatingFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((uint)0x00000000, NumberBaseHelper<uint>.CreateSaturating<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal((uint)0x00000001, NumberBaseHelper<uint>.CreateSaturating<nuint>(unchecked((nuint)0x0000000000000001)));
                Assert.Equal((uint)0xFFFFFFFF, NumberBaseHelper<uint>.CreateSaturating<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal((uint)0xFFFFFFFF, NumberBaseHelper<uint>.CreateSaturating<nuint>(unchecked((nuint)0x8000000000000000)));
                Assert.Equal((uint)0xFFFFFFFF, NumberBaseHelper<uint>.CreateSaturating<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((uint)0x00000000, NumberBaseHelper<uint>.CreateSaturating<nuint>((nuint)0x00000000));
                Assert.Equal((uint)0x00000001, NumberBaseHelper<uint>.CreateSaturating<nuint>((nuint)0x00000001));
                Assert.Equal((uint)0x7FFFFFFF, NumberBaseHelper<uint>.CreateSaturating<nuint>((nuint)0x7FFFFFFF));
                Assert.Equal((uint)0x80000000, NumberBaseHelper<uint>.CreateSaturating<nuint>((nuint)0x80000000));
                Assert.Equal((uint)0xFFFFFFFF, NumberBaseHelper<uint>.CreateSaturating<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateTruncatingFromByteTest()
        {
            Assert.Equal((uint)0x00000000, NumberBaseHelper<uint>.CreateTruncating<byte>(0x00));
            Assert.Equal((uint)0x00000001, NumberBaseHelper<uint>.CreateTruncating<byte>(0x01));
            Assert.Equal((uint)0x0000007F, NumberBaseHelper<uint>.CreateTruncating<byte>(0x7F));
            Assert.Equal((uint)0x00000080, NumberBaseHelper<uint>.CreateTruncating<byte>(0x80));
            Assert.Equal((uint)0x000000FF, NumberBaseHelper<uint>.CreateTruncating<byte>(0xFF));
        }

        [Fact]
        public static void CreateTruncatingFromCharTest()
        {
            Assert.Equal((uint)0x00000000, NumberBaseHelper<uint>.CreateTruncating<char>((char)0x0000));
            Assert.Equal((uint)0x00000001, NumberBaseHelper<uint>.CreateTruncating<char>((char)0x0001));
            Assert.Equal((uint)0x00007FFF, NumberBaseHelper<uint>.CreateTruncating<char>((char)0x7FFF));
            Assert.Equal((uint)0x00008000, NumberBaseHelper<uint>.CreateTruncating<char>((char)0x8000));
            Assert.Equal((uint)0x0000FFFF, NumberBaseHelper<uint>.CreateTruncating<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromDecimalTest()
        {
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateTruncating<decimal>(-0.0m));
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateTruncating<decimal>(+0.0m));
            Assert.Equal((uint)0x0000_0001, NumberBaseHelper<uint>.CreateTruncating<decimal>(+1.0m));

            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateTruncating<decimal>(decimal.MinValue));
            Assert.Equal((uint)0xFFFF_FFFF, NumberBaseHelper<uint>.CreateTruncating<decimal>(decimal.MaxValue));
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateTruncating<decimal>(decimal.MinusOne));
        }

        [Fact]
        public static void CreateTruncatingFromDoubleTest()
        {
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateTruncating<double>(+0.0));
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateTruncating<double>(-0.0));


            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateTruncating<double>(-double.Epsilon));
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateTruncating<double>(+double.Epsilon));

            Assert.Equal((uint)0x0000_0001, NumberBaseHelper<uint>.CreateTruncating<double>(+1.0));
            Assert.Equal((uint)0xFFFF_FFFF, NumberBaseHelper<uint>.CreateTruncating<double>(+4294967295.0));

            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateTruncating<double>(-1.0));
            Assert.Equal((uint)0xFFFF_FFFF, NumberBaseHelper<uint>.CreateTruncating<double>(+4294967296.0));

            Assert.Equal((uint)0xFFFF_FFFF, NumberBaseHelper<uint>.CreateTruncating<double>(double.PositiveInfinity));
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateTruncating<double>(double.NegativeInfinity));

            Assert.Equal((uint)0xFFFF_FFFF, NumberBaseHelper<uint>.CreateTruncating<double>(double.MaxValue));
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateTruncating<double>(double.MinValue));

            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateTruncating<double>(double.NaN));
        }

        [Fact]
        public static void CreateTruncatingFromHalfTest()
        {
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateTruncating<Half>(Half.Zero));
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateTruncating<Half>(Half.NegativeZero));

            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateTruncating<Half>(-Half.Epsilon));
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateTruncating<Half>(+Half.Epsilon));

            Assert.Equal((uint)0x0000_0001, NumberBaseHelper<uint>.CreateTruncating<Half>(Half.One));
            Assert.Equal((uint)0x0000_FFE0, NumberBaseHelper<uint>.CreateTruncating<Half>(Half.MaxValue));

            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateTruncating<Half>(Half.NegativeOne));

            Assert.Equal((uint)0xFFFF_FFFF, NumberBaseHelper<uint>.CreateTruncating<Half>(Half.PositiveInfinity));
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateTruncating<Half>(Half.NegativeInfinity));

            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateTruncating<Half>(Half.MinValue));
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateTruncating<Half>(Half.NaN));
        }

        [Fact]
        public static void CreateTruncatingFromInt16Test()
        {
            Assert.Equal((uint)0x00000000, NumberBaseHelper<uint>.CreateTruncating<short>(0x0000));
            Assert.Equal((uint)0x00000001, NumberBaseHelper<uint>.CreateTruncating<short>(0x0001));
            Assert.Equal((uint)0x00007FFF, NumberBaseHelper<uint>.CreateTruncating<short>(0x7FFF));
            Assert.Equal((uint)0xFFFF8000, NumberBaseHelper<uint>.CreateTruncating<short>(unchecked((short)0x8000)));
            Assert.Equal((uint)0xFFFFFFFF, NumberBaseHelper<uint>.CreateTruncating<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateTruncatingFromInt32Test()
        {
            Assert.Equal((uint)0x00000000, NumberBaseHelper<uint>.CreateTruncating<int>(0x00000000));
            Assert.Equal((uint)0x00000001, NumberBaseHelper<uint>.CreateTruncating<int>(0x00000001));
            Assert.Equal((uint)0x7FFFFFFF, NumberBaseHelper<uint>.CreateTruncating<int>(0x7FFFFFFF));
            Assert.Equal((uint)0x80000000, NumberBaseHelper<uint>.CreateTruncating<int>(unchecked((int)0x80000000)));
            Assert.Equal((uint)0xFFFFFFFF, NumberBaseHelper<uint>.CreateTruncating<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateTruncatingFromInt64Test()
        {
            Assert.Equal((uint)0x00000000, NumberBaseHelper<uint>.CreateTruncating<long>(0x0000000000000000));
            Assert.Equal((uint)0x00000001, NumberBaseHelper<uint>.CreateTruncating<long>(0x0000000000000001));
            Assert.Equal((uint)0xFFFFFFFF, NumberBaseHelper<uint>.CreateTruncating<long>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal((uint)0x00000000, NumberBaseHelper<uint>.CreateTruncating<long>(unchecked((long)0x8000000000000000)));
            Assert.Equal((uint)0xFFFFFFFF, NumberBaseHelper<uint>.CreateTruncating<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void CreateTruncatingFromInt128Test()
        {
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateTruncating<Int128>(Int128.Zero));
            Assert.Equal((uint)0x0000_0001, NumberBaseHelper<uint>.CreateTruncating<Int128>(Int128.One));
            Assert.Equal((uint)0xFFFF_FFFF, NumberBaseHelper<uint>.CreateTruncating<Int128>(Int128.MaxValue));
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateTruncating<Int128>(Int128.MinValue));
            Assert.Equal((uint)0xFFFF_FFFF, NumberBaseHelper<uint>.CreateTruncating<Int128>(Int128.NegativeOne));
        }

        [Fact]
        public static void CreateTruncatingFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((uint)0x00000000, NumberBaseHelper<uint>.CreateTruncating<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal((uint)0x00000001, NumberBaseHelper<uint>.CreateTruncating<nint>(unchecked((nint)0x0000000000000001)));
                Assert.Equal((uint)0xFFFFFFFF, NumberBaseHelper<uint>.CreateTruncating<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal((uint)0x00000000, NumberBaseHelper<uint>.CreateTruncating<nint>(unchecked((nint)0x8000000000000000)));
                Assert.Equal((uint)0xFFFFFFFF, NumberBaseHelper<uint>.CreateTruncating<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((uint)0x00000000, NumberBaseHelper<uint>.CreateTruncating<nint>((nint)0x00000000));
                Assert.Equal((uint)0x00000001, NumberBaseHelper<uint>.CreateTruncating<nint>((nint)0x00000001));
                Assert.Equal((uint)0x7FFFFFFF, NumberBaseHelper<uint>.CreateTruncating<nint>((nint)0x7FFFFFFF));
                Assert.Equal((uint)0x80000000, NumberBaseHelper<uint>.CreateTruncating<nint>(unchecked((nint)0x80000000)));
                Assert.Equal((uint)0xFFFFFFFF, NumberBaseHelper<uint>.CreateTruncating<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateTruncatingFromNFloatTest()
        {
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateTruncating<NFloat>(0.0f));
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateTruncating<NFloat>(NFloat.NegativeZero));

            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateTruncating<NFloat>(-NFloat.Epsilon));
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateTruncating<NFloat>(+NFloat.Epsilon));

            if (Environment.Is64BitProcess)
            {
                Assert.Equal((uint)0x0000_0001, NumberBaseHelper<uint>.CreateTruncating<NFloat>(1.0f));
                Assert.Equal((uint)0xFFFF_FFFF, NumberBaseHelper<uint>.CreateTruncating<NFloat>((NFloat)(4294967295.0)));

                Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateTruncating<NFloat>(-1.0f));
                Assert.Equal((uint)0xFFFF_FFFF, NumberBaseHelper<uint>.CreateTruncating<NFloat>(+4294967296.0f));
            }
            else
            {
                Assert.Equal((uint)0x0000_0001, NumberBaseHelper<uint>.CreateTruncating<NFloat>(1.0f));
                Assert.Equal((uint)0xFFFF_FF00, NumberBaseHelper<uint>.CreateTruncating<NFloat>(4294967040.0f));

                Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateTruncating<NFloat>(-1.0f));
                Assert.Equal((uint)0xFFFF_FFFF, NumberBaseHelper<uint>.CreateTruncating<NFloat>(+4294967296.0f));
            }

            Assert.Equal((uint)0xFFFF_FFFF, NumberBaseHelper<uint>.CreateTruncating<NFloat>(NFloat.PositiveInfinity));
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateTruncating<NFloat>(NFloat.NegativeInfinity));

            Assert.Equal((uint)0xFFFF_FFFF, NumberBaseHelper<uint>.CreateTruncating<NFloat>(NFloat.MaxValue));
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateTruncating<NFloat>(NFloat.MinValue));

            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateTruncating<NFloat>(NFloat.NaN));
        }

        [Fact]
        public static void CreateTruncatingFromSByteTest()
        {
            Assert.Equal((uint)0x00000000, NumberBaseHelper<uint>.CreateTruncating<sbyte>(0x00));
            Assert.Equal((uint)0x00000001, NumberBaseHelper<uint>.CreateTruncating<sbyte>(0x01));
            Assert.Equal((uint)0x0000007F, NumberBaseHelper<uint>.CreateTruncating<sbyte>(0x7F));
            Assert.Equal((uint)0xFFFFFF80, NumberBaseHelper<uint>.CreateTruncating<sbyte>(unchecked((sbyte)0x80)));
            Assert.Equal((uint)0xFFFFFFFF, NumberBaseHelper<uint>.CreateTruncating<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateTruncatingFromSingleTest()
        {
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateTruncating<float>(+0.0f));
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateTruncating<float>(-0.0f));

            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateTruncating<float>(-float.Epsilon));
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateTruncating<float>(-float.Epsilon));

            Assert.Equal((uint)0x0000_0001, NumberBaseHelper<uint>.CreateTruncating<float>(+1.0f));
            Assert.Equal((uint)0xFFFF_FF00, NumberBaseHelper<uint>.CreateTruncating<float>(+4294967040.0f));

            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateTruncating<float>(-1.0f));
            Assert.Equal((uint)0xFFFF_FFFF, NumberBaseHelper<uint>.CreateTruncating<float>(+4294967296.0f));

            Assert.Equal((uint)0xFFFF_FFFF, NumberBaseHelper<uint>.CreateTruncating<float>(float.PositiveInfinity));
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateTruncating<float>(float.NegativeInfinity));

            Assert.Equal((uint)0xFFFF_FFFF, NumberBaseHelper<uint>.CreateTruncating<float>(float.MaxValue));
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateTruncating<float>(float.MinValue));

            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateTruncating<float>(float.NaN));
        }

        [Fact]
        public static void CreateTruncatingFromUInt16Test()
        {
            Assert.Equal((uint)0x00000000, NumberBaseHelper<uint>.CreateTruncating<ushort>(0x0000));
            Assert.Equal((uint)0x00000001, NumberBaseHelper<uint>.CreateTruncating<ushort>(0x0001));
            Assert.Equal((uint)0x00007FFF, NumberBaseHelper<uint>.CreateTruncating<ushort>(0x7FFF));
            Assert.Equal((uint)0x00008000, NumberBaseHelper<uint>.CreateTruncating<ushort>(0x8000));
            Assert.Equal((uint)0x0000FFFF, NumberBaseHelper<uint>.CreateTruncating<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt32Test()
        {
            Assert.Equal((uint)0x00000000, NumberBaseHelper<uint>.CreateTruncating<uint>(0x00000000));
            Assert.Equal((uint)0x00000001, NumberBaseHelper<uint>.CreateTruncating<uint>(0x00000001));
            Assert.Equal((uint)0x7FFFFFFF, NumberBaseHelper<uint>.CreateTruncating<uint>(0x7FFFFFFF));
            Assert.Equal((uint)0x80000000, NumberBaseHelper<uint>.CreateTruncating<uint>(0x80000000));
            Assert.Equal((uint)0xFFFFFFFF, NumberBaseHelper<uint>.CreateTruncating<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt64Test()
        {
            Assert.Equal((uint)0x00000000, NumberBaseHelper<uint>.CreateTruncating<ulong>(0x0000000000000000));
            Assert.Equal((uint)0x00000001, NumberBaseHelper<uint>.CreateTruncating<ulong>(0x0000000000000001));
            Assert.Equal((uint)0xFFFFFFFF, NumberBaseHelper<uint>.CreateTruncating<ulong>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal((uint)0x00000000, NumberBaseHelper<uint>.CreateTruncating<ulong>(0x8000000000000000));
            Assert.Equal((uint)0xFFFFFFFF, NumberBaseHelper<uint>.CreateTruncating<ulong>(0xFFFFFFFFFFFFFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt128Test()
        {
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateTruncating<UInt128>(UInt128.Zero));
            Assert.Equal((uint)0x0000_0001, NumberBaseHelper<uint>.CreateTruncating<UInt128>(UInt128.One));
            Assert.Equal((uint)0xFFFF_FFFF, NumberBaseHelper<uint>.CreateTruncating<UInt128>(UInt128Tests_GenericMath.Int128MaxValue));
            Assert.Equal((uint)0x0000_0000, NumberBaseHelper<uint>.CreateTruncating<UInt128>(UInt128Tests_GenericMath.Int128MaxValuePlusOne));
            Assert.Equal((uint)0xFFFF_FFFF, NumberBaseHelper<uint>.CreateTruncating<UInt128>(UInt128.MaxValue));
        }

        [Fact]
        public static void CreateTruncatingFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((uint)0x00000000, NumberBaseHelper<uint>.CreateTruncating<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal((uint)0x00000001, NumberBaseHelper<uint>.CreateTruncating<nuint>(unchecked((nuint)0x0000000000000001)));
                Assert.Equal((uint)0xFFFFFFFF, NumberBaseHelper<uint>.CreateTruncating<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal((uint)0x00000000, NumberBaseHelper<uint>.CreateTruncating<nuint>(unchecked((nuint)0x8000000000000000)));
                Assert.Equal((uint)0xFFFFFFFF, NumberBaseHelper<uint>.CreateTruncating<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((uint)0x00000000, NumberBaseHelper<uint>.CreateTruncating<nuint>((nuint)0x00000000));
                Assert.Equal((uint)0x00000001, NumberBaseHelper<uint>.CreateTruncating<nuint>((nuint)0x00000001));
                Assert.Equal((uint)0x7FFFFFFF, NumberBaseHelper<uint>.CreateTruncating<nuint>((nuint)0x7FFFFFFF));
                Assert.Equal((uint)0x80000000, NumberBaseHelper<uint>.CreateTruncating<nuint>((nuint)0x80000000));
                Assert.Equal((uint)0xFFFFFFFF, NumberBaseHelper<uint>.CreateTruncating<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void IsCanonicalTest()
        {
            Assert.True(NumberBaseHelper<uint>.IsCanonical((uint)0x00000000));
            Assert.True(NumberBaseHelper<uint>.IsCanonical((uint)0x00000001));
            Assert.True(NumberBaseHelper<uint>.IsCanonical((uint)0x7FFFFFFF));
            Assert.True(NumberBaseHelper<uint>.IsCanonical((uint)0x80000000));
            Assert.True(NumberBaseHelper<uint>.IsCanonical((uint)0xFFFFFFFF));
        }

        [Fact]
        public static void IsComplexNumberTest()
        {
            Assert.False(NumberBaseHelper<uint>.IsComplexNumber((uint)0x00000000));
            Assert.False(NumberBaseHelper<uint>.IsComplexNumber((uint)0x00000001));
            Assert.False(NumberBaseHelper<uint>.IsComplexNumber((uint)0x7FFFFFFF));
            Assert.False(NumberBaseHelper<uint>.IsComplexNumber((uint)0x80000000));
            Assert.False(NumberBaseHelper<uint>.IsComplexNumber((uint)0xFFFFFFFF));
        }

        [Fact]
        public static void IsEvenIntegerTest()
        {
            Assert.True(NumberBaseHelper<uint>.IsEvenInteger((uint)0x00000000));
            Assert.False(NumberBaseHelper<uint>.IsEvenInteger((uint)0x00000001));
            Assert.False(NumberBaseHelper<uint>.IsEvenInteger((uint)0x7FFFFFFF));
            Assert.True(NumberBaseHelper<uint>.IsEvenInteger((uint)0x80000000));
            Assert.False(NumberBaseHelper<uint>.IsEvenInteger((uint)0xFFFFFFFF));
        }

        [Fact]
        public static void IsFiniteTest()
        {
            Assert.True(NumberBaseHelper<uint>.IsFinite((uint)0x00000000));
            Assert.True(NumberBaseHelper<uint>.IsFinite((uint)0x00000001));
            Assert.True(NumberBaseHelper<uint>.IsFinite((uint)0x7FFFFFFF));
            Assert.True(NumberBaseHelper<uint>.IsFinite((uint)0x80000000));
            Assert.True(NumberBaseHelper<uint>.IsFinite((uint)0xFFFFFFFF));
        }

        [Fact]
        public static void IsImaginaryNumberTest()
        {
            Assert.False(NumberBaseHelper<uint>.IsImaginaryNumber((uint)0x00000000));
            Assert.False(NumberBaseHelper<uint>.IsImaginaryNumber((uint)0x00000001));
            Assert.False(NumberBaseHelper<uint>.IsImaginaryNumber((uint)0x7FFFFFFF));
            Assert.False(NumberBaseHelper<uint>.IsImaginaryNumber((uint)0x80000000));
            Assert.False(NumberBaseHelper<uint>.IsImaginaryNumber((uint)0xFFFFFFFF));
        }

        [Fact]
        public static void IsInfinityTest()
        {
            Assert.False(NumberBaseHelper<uint>.IsInfinity((uint)0x00000000));
            Assert.False(NumberBaseHelper<uint>.IsInfinity((uint)0x00000001));
            Assert.False(NumberBaseHelper<uint>.IsInfinity((uint)0x7FFFFFFF));
            Assert.False(NumberBaseHelper<uint>.IsInfinity((uint)0x80000000));
            Assert.False(NumberBaseHelper<uint>.IsInfinity((uint)0xFFFFFFFF));
        }

        [Fact]
        public static void IsIntegerTest()
        {
            Assert.True(NumberBaseHelper<uint>.IsInteger((uint)0x00000000));
            Assert.True(NumberBaseHelper<uint>.IsInteger((uint)0x00000001));
            Assert.True(NumberBaseHelper<uint>.IsInteger((uint)0x7FFFFFFF));
            Assert.True(NumberBaseHelper<uint>.IsInteger((uint)0x80000000));
            Assert.True(NumberBaseHelper<uint>.IsInteger((uint)0xFFFFFFFF));
        }

        [Fact]
        public static void IsNaNTest()
        {
            Assert.False(NumberBaseHelper<uint>.IsNaN((uint)0x00000000));
            Assert.False(NumberBaseHelper<uint>.IsNaN((uint)0x00000001));
            Assert.False(NumberBaseHelper<uint>.IsNaN((uint)0x7FFFFFFF));
            Assert.False(NumberBaseHelper<uint>.IsNaN((uint)0x80000000));
            Assert.False(NumberBaseHelper<uint>.IsNaN((uint)0xFFFFFFFF));
        }

        [Fact]
        public static void IsNegativeTest()
        {
            Assert.False(NumberBaseHelper<uint>.IsNegative((uint)0x00000000));
            Assert.False(NumberBaseHelper<uint>.IsNegative((uint)0x00000001));
            Assert.False(NumberBaseHelper<uint>.IsNegative((uint)0x7FFFFFFF));
            Assert.False(NumberBaseHelper<uint>.IsNegative((uint)0x80000000));
            Assert.False(NumberBaseHelper<uint>.IsNegative((uint)0xFFFFFFFF));
        }

        [Fact]
        public static void IsNegativeInfinityTest()
        {
            Assert.False(NumberBaseHelper<uint>.IsNegativeInfinity((uint)0x00000000));
            Assert.False(NumberBaseHelper<uint>.IsNegativeInfinity((uint)0x00000001));
            Assert.False(NumberBaseHelper<uint>.IsNegativeInfinity((uint)0x7FFFFFFF));
            Assert.False(NumberBaseHelper<uint>.IsNegativeInfinity((uint)0x80000000));
            Assert.False(NumberBaseHelper<uint>.IsNegativeInfinity((uint)0xFFFFFFFF));
        }

        [Fact]
        public static void IsNormalTest()
        {
            Assert.False(NumberBaseHelper<uint>.IsNormal((uint)0x00000000));
            Assert.True(NumberBaseHelper<uint>.IsNormal((uint)0x00000001));
            Assert.True(NumberBaseHelper<uint>.IsNormal((uint)0x7FFFFFFF));
            Assert.True(NumberBaseHelper<uint>.IsNormal((uint)0x80000000));
            Assert.True(NumberBaseHelper<uint>.IsNormal((uint)0xFFFFFFFF));
        }

        [Fact]
        public static void IsOddIntegerTest()
        {
            Assert.False(NumberBaseHelper<uint>.IsOddInteger((uint)0x00000000));
            Assert.True(NumberBaseHelper<uint>.IsOddInteger((uint)0x00000001));
            Assert.True(NumberBaseHelper<uint>.IsOddInteger((uint)0x7FFFFFFF));
            Assert.False(NumberBaseHelper<uint>.IsOddInteger((uint)0x80000000));
            Assert.True(NumberBaseHelper<uint>.IsOddInteger((uint)0xFFFFFFFF));
        }

        [Fact]
        public static void IsPositiveTest()
        {
            Assert.True(NumberBaseHelper<uint>.IsPositive((uint)0x00000000));
            Assert.True(NumberBaseHelper<uint>.IsPositive((uint)0x00000001));
            Assert.True(NumberBaseHelper<uint>.IsPositive((uint)0x7FFFFFFF));
            Assert.True(NumberBaseHelper<uint>.IsPositive((uint)0x80000000));
            Assert.True(NumberBaseHelper<uint>.IsPositive((uint)0xFFFFFFFF));
        }

        [Fact]
        public static void IsPositiveInfinityTest()
        {
            Assert.False(NumberBaseHelper<uint>.IsPositiveInfinity((uint)0x00000000));
            Assert.False(NumberBaseHelper<uint>.IsPositiveInfinity((uint)0x00000001));
            Assert.False(NumberBaseHelper<uint>.IsPositiveInfinity((uint)0x7FFFFFFF));
            Assert.False(NumberBaseHelper<uint>.IsPositiveInfinity((uint)0x80000000));
            Assert.False(NumberBaseHelper<uint>.IsPositiveInfinity((uint)0xFFFFFFFF));
        }

        [Fact]
        public static void IsRealNumberTest()
        {
            Assert.True(NumberBaseHelper<uint>.IsRealNumber((uint)0x00000000));
            Assert.True(NumberBaseHelper<uint>.IsRealNumber((uint)0x00000001));
            Assert.True(NumberBaseHelper<uint>.IsRealNumber((uint)0x7FFFFFFF));
            Assert.True(NumberBaseHelper<uint>.IsRealNumber((uint)0x80000000));
            Assert.True(NumberBaseHelper<uint>.IsRealNumber((uint)0xFFFFFFFF));
        }

        [Fact]
        public static void IsSubnormalTest()
        {
            Assert.False(NumberBaseHelper<uint>.IsSubnormal((uint)0x00000000));
            Assert.False(NumberBaseHelper<uint>.IsSubnormal((uint)0x00000001));
            Assert.False(NumberBaseHelper<uint>.IsSubnormal((uint)0x7FFFFFFF));
            Assert.False(NumberBaseHelper<uint>.IsSubnormal((uint)0x80000000));
            Assert.False(NumberBaseHelper<uint>.IsSubnormal((uint)0xFFFFFFFF));
        }

        [Fact]
        public static void IsZeroTest()
        {
            Assert.True(NumberBaseHelper<uint>.IsZero((uint)0x00000000));
            Assert.False(NumberBaseHelper<uint>.IsZero((uint)0x00000001));
            Assert.False(NumberBaseHelper<uint>.IsZero((uint)0x7FFFFFFF));
            Assert.False(NumberBaseHelper<uint>.IsZero((uint)0x80000000));
            Assert.False(NumberBaseHelper<uint>.IsZero((uint)0xFFFFFFFF));
        }

        [Fact]
        public static void MaxMagnitudeTest()
        {
            Assert.Equal((uint)0x00000001, NumberBaseHelper<uint>.MaxMagnitude((uint)0x00000000, 1));
            Assert.Equal((uint)0x00000001, NumberBaseHelper<uint>.MaxMagnitude((uint)0x00000001, 1));
            Assert.Equal((uint)0x7FFFFFFF, NumberBaseHelper<uint>.MaxMagnitude((uint)0x7FFFFFFF, 1));
            Assert.Equal((uint)0x80000000, NumberBaseHelper<uint>.MaxMagnitude((uint)0x80000000, 1));
            Assert.Equal((uint)0xFFFFFFFF, NumberBaseHelper<uint>.MaxMagnitude((uint)0xFFFFFFFF, 1));
        }

        [Fact]
        public static void MaxMagnitudeNumberTest()
        {
            Assert.Equal((uint)0x00000001, NumberBaseHelper<uint>.MaxMagnitudeNumber((uint)0x00000000, 1));
            Assert.Equal((uint)0x00000001, NumberBaseHelper<uint>.MaxMagnitudeNumber((uint)0x00000001, 1));
            Assert.Equal((uint)0x7FFFFFFF, NumberBaseHelper<uint>.MaxMagnitudeNumber((uint)0x7FFFFFFF, 1));
            Assert.Equal((uint)0x80000000, NumberBaseHelper<uint>.MaxMagnitudeNumber((uint)0x80000000, 1));
            Assert.Equal((uint)0xFFFFFFFF, NumberBaseHelper<uint>.MaxMagnitudeNumber((uint)0xFFFFFFFF, 1));
        }

        [Fact]
        public static void MinMagnitudeTest()
        {
            Assert.Equal((uint)0x00000000, NumberBaseHelper<uint>.MinMagnitude((uint)0x00000000, 1));
            Assert.Equal((uint)0x00000001, NumberBaseHelper<uint>.MinMagnitude((uint)0x00000001, 1));
            Assert.Equal((uint)0x00000001, NumberBaseHelper<uint>.MinMagnitude((uint)0x7FFFFFFF, 1));
            Assert.Equal((uint)0x00000001, NumberBaseHelper<uint>.MinMagnitude((uint)0x80000000, 1));
            Assert.Equal((uint)0x00000001, NumberBaseHelper<uint>.MinMagnitude((uint)0xFFFFFFFF, 1));
        }

        [Fact]
        public static void MinMagnitudeNumberTest()
        {
            Assert.Equal((uint)0x00000000, NumberBaseHelper<uint>.MinMagnitudeNumber((uint)0x00000000, 1));
            Assert.Equal((uint)0x00000001, NumberBaseHelper<uint>.MinMagnitudeNumber((uint)0x00000001, 1));
            Assert.Equal((uint)0x00000001, NumberBaseHelper<uint>.MinMagnitudeNumber((uint)0x7FFFFFFF, 1));
            Assert.Equal((uint)0x00000001, NumberBaseHelper<uint>.MinMagnitudeNumber((uint)0x80000000, 1));
            Assert.Equal((uint)0x00000001, NumberBaseHelper<uint>.MinMagnitudeNumber((uint)0xFFFFFFFF, 1));
        }

        //
        // IShiftOperators
        //

        [Fact]
        public static void op_LeftShiftTest()
        {
            Assert.Equal((uint)0x00000000, ShiftOperatorsHelper<uint, int, uint>.op_LeftShift((uint)0x00000000, 1));
            Assert.Equal((uint)0x00000002, ShiftOperatorsHelper<uint, int, uint>.op_LeftShift((uint)0x00000001, 1));
            Assert.Equal((uint)0xFFFFFFFE, ShiftOperatorsHelper<uint, int, uint>.op_LeftShift((uint)0x7FFFFFFF, 1));
            Assert.Equal((uint)0x00000000, ShiftOperatorsHelper<uint, int, uint>.op_LeftShift((uint)0x80000000, 1));
            Assert.Equal((uint)0xFFFFFFFE, ShiftOperatorsHelper<uint, int, uint>.op_LeftShift((uint)0xFFFFFFFF, 1));
        }

        [Fact]
        public static void op_RightShiftTest()
        {
            Assert.Equal((uint)0x00000000, ShiftOperatorsHelper<uint, int, uint>.op_RightShift((uint)0x00000000, 1));
            Assert.Equal((uint)0x00000000, ShiftOperatorsHelper<uint, int, uint>.op_RightShift((uint)0x00000001, 1));
            Assert.Equal((uint)0x3FFFFFFF, ShiftOperatorsHelper<uint, int, uint>.op_RightShift((uint)0x7FFFFFFF, 1));
            Assert.Equal((uint)0x40000000, ShiftOperatorsHelper<uint, int, uint>.op_RightShift((uint)0x80000000, 1));
            Assert.Equal((uint)0x7FFFFFFF, ShiftOperatorsHelper<uint, int, uint>.op_RightShift((uint)0xFFFFFFFF, 1));
        }

        [Fact]
        public static void op_UnsignedRightShiftTest()
        {
            Assert.Equal((uint)0x00000000, ShiftOperatorsHelper<uint, int, uint>.op_UnsignedRightShift((uint)0x00000000, 1));
            Assert.Equal((uint)0x00000000, ShiftOperatorsHelper<uint, int, uint>.op_UnsignedRightShift((uint)0x00000001, 1));
            Assert.Equal((uint)0x3FFFFFFF, ShiftOperatorsHelper<uint, int, uint>.op_UnsignedRightShift((uint)0x7FFFFFFF, 1));
            Assert.Equal((uint)0x40000000, ShiftOperatorsHelper<uint, int, uint>.op_UnsignedRightShift((uint)0x80000000, 1));
            Assert.Equal((uint)0x7FFFFFFF, ShiftOperatorsHelper<uint, int, uint>.op_UnsignedRightShift((uint)0xFFFFFFFF, 1));
        }

        //
        // ISubtractionOperators
        //

        [Fact]
        public static void op_SubtractionTest()
        {
            Assert.Equal((uint)0xFFFFFFFF, SubtractionOperatorsHelper<uint, uint, uint>.op_Subtraction((uint)0x00000000, 1));
            Assert.Equal((uint)0x00000000, SubtractionOperatorsHelper<uint, uint, uint>.op_Subtraction((uint)0x00000001, 1));
            Assert.Equal((uint)0x7FFFFFFE, SubtractionOperatorsHelper<uint, uint, uint>.op_Subtraction((uint)0x7FFFFFFF, 1));
            Assert.Equal((uint)0x7FFFFFFF, SubtractionOperatorsHelper<uint, uint, uint>.op_Subtraction((uint)0x80000000, 1));
            Assert.Equal((uint)0xFFFFFFFE, SubtractionOperatorsHelper<uint, uint, uint>.op_Subtraction((uint)0xFFFFFFFF, 1));
        }

        [Fact]
        public static void op_CheckedSubtractionTest()
        {
            Assert.Equal((uint)0x00000000, SubtractionOperatorsHelper<uint, uint, uint>.op_CheckedSubtraction((uint)0x00000001, 1));
            Assert.Equal((uint)0x7FFFFFFE, SubtractionOperatorsHelper<uint, uint, uint>.op_CheckedSubtraction((uint)0x7FFFFFFF, 1));
            Assert.Equal((uint)0x7FFFFFFF, SubtractionOperatorsHelper<uint, uint, uint>.op_CheckedSubtraction((uint)0x80000000, 1));
            Assert.Equal((uint)0xFFFFFFFE, SubtractionOperatorsHelper<uint, uint, uint>.op_CheckedSubtraction((uint)0xFFFFFFFF, 1));

            Assert.Throws<OverflowException>(() => SubtractionOperatorsHelper<uint, uint, uint>.op_CheckedSubtraction((uint)0x00000000, 1));
        }

        //
        // IUnaryNegationOperators
        //

        [Fact]
        public static void op_UnaryNegationTest()
        {
            Assert.Equal((uint)0x00000000, UnaryNegationOperatorsHelper<uint, uint>.op_UnaryNegation((uint)0x00000000));
            Assert.Equal((uint)0xFFFFFFFF, UnaryNegationOperatorsHelper<uint, uint>.op_UnaryNegation((uint)0x00000001));
            Assert.Equal((uint)0x80000001, UnaryNegationOperatorsHelper<uint, uint>.op_UnaryNegation((uint)0x7FFFFFFF));
            Assert.Equal((uint)0x80000000, UnaryNegationOperatorsHelper<uint, uint>.op_UnaryNegation((uint)0x80000000));
            Assert.Equal((uint)0x00000001, UnaryNegationOperatorsHelper<uint, uint>.op_UnaryNegation((uint)0xFFFFFFFF));
        }

        [Fact]
        public static void op_CheckedUnaryNegationTest()
        {
            Assert.Equal((uint)0x00000000, UnaryNegationOperatorsHelper<uint, uint>.op_CheckedUnaryNegation((uint)0x00000000));

            Assert.Throws<OverflowException>(() => UnaryNegationOperatorsHelper<uint, uint>.op_CheckedUnaryNegation((uint)0x00000001));
            Assert.Throws<OverflowException>(() => UnaryNegationOperatorsHelper<uint, uint>.op_CheckedUnaryNegation((uint)0x7FFFFFFF));
            Assert.Throws<OverflowException>(() => UnaryNegationOperatorsHelper<uint, uint>.op_CheckedUnaryNegation((uint)0x80000000));
            Assert.Throws<OverflowException>(() => UnaryNegationOperatorsHelper<uint, uint>.op_CheckedUnaryNegation((uint)0xFFFFFFFF));
        }

        //
        // IUnaryPlusOperators
        //

        [Fact]
        public static void op_UnaryPlusTest()
        {
            Assert.Equal((uint)0x00000000, UnaryPlusOperatorsHelper<uint, uint>.op_UnaryPlus((uint)0x00000000));
            Assert.Equal((uint)0x00000001, UnaryPlusOperatorsHelper<uint, uint>.op_UnaryPlus((uint)0x00000001));
            Assert.Equal((uint)0x7FFFFFFF, UnaryPlusOperatorsHelper<uint, uint>.op_UnaryPlus((uint)0x7FFFFFFF));
            Assert.Equal((uint)0x80000000, UnaryPlusOperatorsHelper<uint, uint>.op_UnaryPlus((uint)0x80000000));
            Assert.Equal((uint)0xFFFFFFFF, UnaryPlusOperatorsHelper<uint, uint>.op_UnaryPlus((uint)0xFFFFFFFF));
        }

        //
        // IParsable and ISpanParsable
        //

        [Theory]
        [MemberData(nameof(UInt32Tests.Parse_Valid_TestData), MemberType = typeof(UInt32Tests))]
        public static void ParseValidStringTest(string value, NumberStyles style, IFormatProvider provider, uint expected)
        {
            uint result;

            // Default style and provider
            if ((style == NumberStyles.Integer) && (provider is null))
            {
                Assert.True(ParsableHelper<uint>.TryParse(value, provider, out result));
                Assert.Equal(expected, result);
                Assert.Equal(expected, ParsableHelper<uint>.Parse(value, provider));
            }

            // Default provider
            if (provider is null)
            {
                Assert.Equal(expected, NumberBaseHelper<uint>.Parse(value, style, provider));

                // Substitute default NumberFormatInfo
                Assert.True(NumberBaseHelper<uint>.TryParse(value, style, new NumberFormatInfo(), out result));
                Assert.Equal(expected, result);
                Assert.Equal(expected, NumberBaseHelper<uint>.Parse(value, style, new NumberFormatInfo()));
            }

            // Default style
            if (style == NumberStyles.Integer)
            {
                Assert.Equal(expected, ParsableHelper<uint>.Parse(value, provider));
            }

            // Full overloads
            Assert.True(NumberBaseHelper<uint>.TryParse(value, style, provider, out result));
            Assert.Equal(expected, result);
            Assert.Equal(expected, NumberBaseHelper<uint>.Parse(value, style, provider));
        }

        [Theory]
        [MemberData(nameof(UInt32Tests.Parse_Invalid_TestData), MemberType = typeof(UInt32Tests))]
        public static void ParseInvalidStringTest(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            uint result;

            // Default style and provider
            if ((style == NumberStyles.Integer) && (provider is null))
            {
                Assert.False(ParsableHelper<uint>.TryParse(value, provider, out result));
                Assert.Equal(default(uint), result);
                Assert.Throws(exceptionType, () => ParsableHelper<uint>.Parse(value, provider));
            }

            // Default provider
            if (provider is null)
            {
                Assert.Throws(exceptionType, () => NumberBaseHelper<uint>.Parse(value, style, provider));

                // Substitute default NumberFormatInfo
                Assert.False(NumberBaseHelper<uint>.TryParse(value, style, new NumberFormatInfo(), out result));
                Assert.Equal(default(uint), result);
                Assert.Throws(exceptionType, () => NumberBaseHelper<uint>.Parse(value, style, new NumberFormatInfo()));
            }

            // Default style
            if (style == NumberStyles.Integer)
            {
                Assert.Throws(exceptionType, () => ParsableHelper<uint>.Parse(value, provider));
            }

            // Full overloads
            Assert.False(NumberBaseHelper<uint>.TryParse(value, style, provider, out result));
            Assert.Equal(default(uint), result);
            Assert.Throws(exceptionType, () => NumberBaseHelper<uint>.Parse(value, style, provider));
        }

        [Theory]
        [MemberData(nameof(UInt32Tests.Parse_ValidWithOffsetCount_TestData), MemberType = typeof(UInt32Tests))]
        public static void ParseValidSpanTest(string value, int offset, int count, NumberStyles style, IFormatProvider provider, uint expected)
        {
            uint result;

            // Default style and provider
            if ((style == NumberStyles.Integer) && (provider is null))
            {
                Assert.True(SpanParsableHelper<uint>.TryParse(value.AsSpan(offset, count), provider, out result));
                Assert.Equal(expected, result);
            }

            Assert.Equal(expected, NumberBaseHelper<uint>.Parse(value.AsSpan(offset, count), style, provider));

            Assert.True(NumberBaseHelper<uint>.TryParse(value.AsSpan(offset, count), style, provider, out result));
            Assert.Equal(expected, result);
        }

        [Theory]
        [MemberData(nameof(UInt32Tests.Parse_Invalid_TestData), MemberType = typeof(UInt32Tests))]
        public static void ParseInvalidSpanTest(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            if (value is null)
            {
                return;
            }

            uint result;

            // Default style and provider
            if ((style == NumberStyles.Integer) && (provider is null))
            {
                Assert.False(SpanParsableHelper<uint>.TryParse(value.AsSpan(), provider, out result));
                Assert.Equal(default(uint), result);
            }

            Assert.Throws(exceptionType, () => NumberBaseHelper<uint>.Parse(value.AsSpan(), style, provider));

            Assert.False(NumberBaseHelper<uint>.TryParse(value.AsSpan(), style, provider, out result));
            Assert.Equal(default(uint), result);
        }
    }
}
