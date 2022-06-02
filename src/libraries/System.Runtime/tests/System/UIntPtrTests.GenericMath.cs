// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Tests
{
    public class UIntPtrTests_GenericMath
    {
        //
        // IAdditionOperators
        //

        [Fact]
        public static void op_AdditionTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000000000000001), AdditionOperatorsHelper<nuint, nuint, nuint>.op_Addition(unchecked((nuint)0x0000000000000000), (nuint)1));
                Assert.Equal(unchecked((nuint)0x0000000000000002), AdditionOperatorsHelper<nuint, nuint, nuint>.op_Addition(unchecked((nuint)0x0000000000000001), (nuint)1));
                Assert.Equal(unchecked((nuint)0x8000000000000000), AdditionOperatorsHelper<nuint, nuint, nuint>.op_Addition(unchecked((nuint)0x7FFFFFFFFFFFFFFF), (nuint)1));
                Assert.Equal(unchecked((nuint)0x8000000000000001), AdditionOperatorsHelper<nuint, nuint, nuint>.op_Addition(unchecked((nuint)0x8000000000000000), (nuint)1));
                Assert.Equal(unchecked((nuint)0x0000000000000000), AdditionOperatorsHelper<nuint, nuint, nuint>.op_Addition(unchecked((nuint)0xFFFFFFFFFFFFFFFF), (nuint)1));
            }
            else
            {
                Assert.Equal((nuint)0x00000001, AdditionOperatorsHelper<nuint, nuint, nuint>.op_Addition((nuint)0x00000000, (nuint)1));
                Assert.Equal((nuint)0x00000002, AdditionOperatorsHelper<nuint, nuint, nuint>.op_Addition((nuint)0x00000001, (nuint)1));
                Assert.Equal((nuint)0x80000000, AdditionOperatorsHelper<nuint, nuint, nuint>.op_Addition((nuint)0x7FFFFFFF, (nuint)1));
                Assert.Equal((nuint)0x80000001, AdditionOperatorsHelper<nuint, nuint, nuint>.op_Addition((nuint)0x80000000, (nuint)1));
                Assert.Equal((nuint)0x00000000, AdditionOperatorsHelper<nuint, nuint, nuint>.op_Addition((nuint)0xFFFFFFFF, (nuint)1));
            }
        }

        [Fact]
        public static void op_CheckedAdditionTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000000000000001), AdditionOperatorsHelper<nuint, nuint, nuint>.op_CheckedAddition(unchecked((nuint)0x0000000000000000), (nuint)1));
                Assert.Equal(unchecked((nuint)0x0000000000000002), AdditionOperatorsHelper<nuint, nuint, nuint>.op_CheckedAddition(unchecked((nuint)0x0000000000000001), (nuint)1));
                Assert.Equal(unchecked((nuint)0x8000000000000000), AdditionOperatorsHelper<nuint, nuint, nuint>.op_CheckedAddition(unchecked((nuint)0x7FFFFFFFFFFFFFFF), (nuint)1));
                Assert.Equal(unchecked((nuint)0x8000000000000001), AdditionOperatorsHelper<nuint, nuint, nuint>.op_CheckedAddition(unchecked((nuint)0x8000000000000000), (nuint)1));

                Assert.Throws<OverflowException>(() => AdditionOperatorsHelper<nuint, nuint, nuint>.op_CheckedAddition(unchecked((nuint)0xFFFFFFFFFFFFFFFF), (nuint)1));
            }
            else
            {
                Assert.Equal((nuint)0x00000001, AdditionOperatorsHelper<nuint, nuint, nuint>.op_CheckedAddition((nuint)0x00000000, (nuint)1));
                Assert.Equal((nuint)0x00000002, AdditionOperatorsHelper<nuint, nuint, nuint>.op_CheckedAddition((nuint)0x00000001, (nuint)1));
                Assert.Equal((nuint)0x80000000, AdditionOperatorsHelper<nuint, nuint, nuint>.op_CheckedAddition((nuint)0x7FFFFFFF, (nuint)1));
                Assert.Equal((nuint)0x80000001, AdditionOperatorsHelper<nuint, nuint, nuint>.op_CheckedAddition((nuint)0x80000000, (nuint)1));

                Assert.Throws<OverflowException>(() => AdditionOperatorsHelper<nuint, nuint, nuint>.op_CheckedAddition((nuint)0xFFFFFFFF, (nuint)1));
            }
        }

        //
        // IAdditiveIdentity
        //

        [Fact]
        public static void AdditiveIdentityTest()
        {
            Assert.Equal((nuint)0x00000000, AdditiveIdentityHelper<nuint, nuint>.AdditiveIdentity);
        }

        //
        // IBinaryInteger
        //

        [Fact]
        public static void DivRemTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((unchecked((nuint)0x0000000000000000), unchecked((nuint)0x0000000000000000)), BinaryIntegerHelper<nuint>.DivRem(unchecked((nuint)0x0000000000000000), (nuint)2));
                Assert.Equal((unchecked((nuint)0x0000000000000000), unchecked((nuint)0x0000000000000001)), BinaryIntegerHelper<nuint>.DivRem(unchecked((nuint)0x0000000000000001), (nuint)2));
                Assert.Equal((unchecked((nuint)0x3FFFFFFFFFFFFFFF), unchecked((nuint)0x0000000000000001)), BinaryIntegerHelper<nuint>.DivRem(unchecked((nuint)0x7FFFFFFFFFFFFFFF), (nuint)2));
                Assert.Equal((unchecked((nuint)0x4000000000000000), unchecked((nuint)0x0000000000000000)), BinaryIntegerHelper<nuint>.DivRem(unchecked((nuint)0x8000000000000000), (nuint)2));
                Assert.Equal((unchecked((nuint)0x7FFFFFFFFFFFFFFF), unchecked((nuint)0x0000000000000001)), BinaryIntegerHelper<nuint>.DivRem(unchecked((nuint)0xFFFFFFFFFFFFFFFF), (nuint)2));
            }
            else
            {
                Assert.Equal(((nuint)0x00000000, (nuint)0x00000000), BinaryIntegerHelper<nuint>.DivRem((nuint)0x00000000, (nuint)2));
                Assert.Equal(((nuint)0x00000000, (nuint)0x00000001), BinaryIntegerHelper<nuint>.DivRem((nuint)0x00000001, (nuint)2));
                Assert.Equal(((nuint)0x3FFFFFFF, (nuint)0x00000001), BinaryIntegerHelper<nuint>.DivRem((nuint)0x7FFFFFFF, (nuint)2));
                Assert.Equal(((nuint)0x40000000, (nuint)0x00000000), BinaryIntegerHelper<nuint>.DivRem((nuint)0x80000000, (nuint)2));
                Assert.Equal(((nuint)0x7FFFFFFF, (nuint)0x00000001), BinaryIntegerHelper<nuint>.DivRem((nuint)0xFFFFFFFF, (nuint)2));
            }
        }

        [Fact]
        public static void LeadingZeroCountTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000000000000040), BinaryIntegerHelper<nuint>.LeadingZeroCount(unchecked((nuint)0x0000000000000000)));
                Assert.Equal(unchecked((nuint)0x000000000000003F), BinaryIntegerHelper<nuint>.LeadingZeroCount(unchecked((nuint)0x0000000000000001)));
                Assert.Equal(unchecked((nuint)0x0000000000000001), BinaryIntegerHelper<nuint>.LeadingZeroCount(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((nuint)0x0000000000000000), BinaryIntegerHelper<nuint>.LeadingZeroCount(unchecked((nuint)0x8000000000000000)));
                Assert.Equal(unchecked((nuint)0x0000000000000000), BinaryIntegerHelper<nuint>.LeadingZeroCount(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((nuint)0x0000000000000020, BinaryIntegerHelper<nuint>.LeadingZeroCount((nuint)0x00000000));
                Assert.Equal((nuint)0x000000000000001F, BinaryIntegerHelper<nuint>.LeadingZeroCount((nuint)0x00000001));
                Assert.Equal((nuint)0x0000000000000001, BinaryIntegerHelper<nuint>.LeadingZeroCount((nuint)0x7FFFFFFF));
                Assert.Equal((nuint)0x0000000000000000, BinaryIntegerHelper<nuint>.LeadingZeroCount((nuint)0x80000000));
                Assert.Equal((nuint)0x0000000000000000, BinaryIntegerHelper<nuint>.LeadingZeroCount((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void PopCountTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000000000000000), BinaryIntegerHelper<nuint>.PopCount(unchecked((nuint)0x0000000000000000)));
                Assert.Equal(unchecked((nuint)0x0000000000000001), BinaryIntegerHelper<nuint>.PopCount(unchecked((nuint)0x0000000000000001)));
                Assert.Equal(unchecked((nuint)0x000000000000003F), BinaryIntegerHelper<nuint>.PopCount(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((nuint)0x0000000000000001), BinaryIntegerHelper<nuint>.PopCount(unchecked((nuint)0x8000000000000000)));
                Assert.Equal(unchecked((nuint)0x0000000000000040), BinaryIntegerHelper<nuint>.PopCount(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((nuint)0x00000000, BinaryIntegerHelper<nuint>.PopCount((nuint)0x00000000));
                Assert.Equal((nuint)0x00000001, BinaryIntegerHelper<nuint>.PopCount((nuint)0x00000001));
                Assert.Equal((nuint)0x0000001F, BinaryIntegerHelper<nuint>.PopCount((nuint)0x7FFFFFFF));
                Assert.Equal((nuint)0x00000001, BinaryIntegerHelper<nuint>.PopCount((nuint)0x80000000));
                Assert.Equal((nuint)0x00000020, BinaryIntegerHelper<nuint>.PopCount((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void RotateLeftTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000000000000000), BinaryIntegerHelper<nuint>.RotateLeft(unchecked((nuint)0x0000000000000000), 1));
                Assert.Equal(unchecked((nuint)0x0000000000000002), BinaryIntegerHelper<nuint>.RotateLeft(unchecked((nuint)0x0000000000000001), 1));
                Assert.Equal(unchecked((nuint)0xFFFFFFFFFFFFFFFE), BinaryIntegerHelper<nuint>.RotateLeft(unchecked((nuint)0x7FFFFFFFFFFFFFFF), 1));
                Assert.Equal(unchecked((nuint)0x0000000000000001), BinaryIntegerHelper<nuint>.RotateLeft(unchecked((nuint)0x8000000000000000), 1));
                Assert.Equal(unchecked((nuint)0xFFFFFFFFFFFFFFFF), BinaryIntegerHelper<nuint>.RotateLeft(unchecked((nuint)0xFFFFFFFFFFFFFFFF), 1));
            }
            else
            {
                Assert.Equal((nuint)0x00000000, BinaryIntegerHelper<nuint>.RotateLeft((nuint)0x00000000, 1));
                Assert.Equal((nuint)0x00000002, BinaryIntegerHelper<nuint>.RotateLeft((nuint)0x00000001, 1));
                Assert.Equal((nuint)0xFFFFFFFE, BinaryIntegerHelper<nuint>.RotateLeft((nuint)0x7FFFFFFF, 1));
                Assert.Equal((nuint)0x00000001, BinaryIntegerHelper<nuint>.RotateLeft((nuint)0x80000000, 1));
                Assert.Equal((nuint)0xFFFFFFFF, BinaryIntegerHelper<nuint>.RotateLeft((nuint)0xFFFFFFFF, 1));
            }
        }

        [Fact]
        public static void RotateRightTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000000000000000), BinaryIntegerHelper<nuint>.RotateRight(unchecked((nuint)0x0000000000000000), 1));
                Assert.Equal(unchecked((nuint)0x8000000000000000), BinaryIntegerHelper<nuint>.RotateRight(unchecked((nuint)0x0000000000000001), 1));
                Assert.Equal(unchecked((nuint)0xBFFFFFFFFFFFFFFF), BinaryIntegerHelper<nuint>.RotateRight(unchecked((nuint)0x7FFFFFFFFFFFFFFF), 1));
                Assert.Equal(unchecked((nuint)0x4000000000000000), BinaryIntegerHelper<nuint>.RotateRight(unchecked((nuint)0x8000000000000000), 1));
                Assert.Equal(unchecked((nuint)0xFFFFFFFFFFFFFFFF), BinaryIntegerHelper<nuint>.RotateRight(unchecked((nuint)0xFFFFFFFFFFFFFFFF), 1));
            }
            else
            {
                Assert.Equal((nuint)0x00000000, BinaryIntegerHelper<nuint>.RotateRight((nuint)0x00000000, 1));
                Assert.Equal((nuint)0x80000000, BinaryIntegerHelper<nuint>.RotateRight((nuint)0x00000001, 1));
                Assert.Equal((nuint)0xBFFFFFFF, BinaryIntegerHelper<nuint>.RotateRight((nuint)0x7FFFFFFF, 1));
                Assert.Equal((nuint)0x40000000, BinaryIntegerHelper<nuint>.RotateRight((nuint)0x80000000, 1));
                Assert.Equal((nuint)0xFFFFFFFF, BinaryIntegerHelper<nuint>.RotateRight((nuint)0xFFFFFFFF, 1));
            }
        }

        [Fact]
        public static void TrailingZeroCountTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000000000000040), BinaryIntegerHelper<nuint>.TrailingZeroCount(unchecked((nuint)0x0000000000000000)));
                Assert.Equal(unchecked((nuint)0x0000000000000000), BinaryIntegerHelper<nuint>.TrailingZeroCount(unchecked((nuint)0x0000000000000001)));
                Assert.Equal(unchecked((nuint)0x0000000000000000), BinaryIntegerHelper<nuint>.TrailingZeroCount(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((nuint)0x000000000000003F), BinaryIntegerHelper<nuint>.TrailingZeroCount(unchecked((nuint)0x8000000000000000)));
                Assert.Equal(unchecked((nuint)0x0000000000000000), BinaryIntegerHelper<nuint>.TrailingZeroCount(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((nuint)0x00000020, BinaryIntegerHelper<nuint>.TrailingZeroCount((nuint)0x00000000));
                Assert.Equal((nuint)0x00000000, BinaryIntegerHelper<nuint>.TrailingZeroCount((nuint)0x00000001));
                Assert.Equal((nuint)0x00000000, BinaryIntegerHelper<nuint>.TrailingZeroCount((nuint)0x7FFFFFFF));
                Assert.Equal((nuint)0x0000001F, BinaryIntegerHelper<nuint>.TrailingZeroCount((nuint)0x80000000));
                Assert.Equal((nuint)0x00000000, BinaryIntegerHelper<nuint>.TrailingZeroCount((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void GetByteCountTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(8, BinaryIntegerHelper<nuint>.GetByteCount(unchecked((nuint)0x0000000000000000)));
                Assert.Equal(8, BinaryIntegerHelper<nuint>.GetByteCount(unchecked((nuint)0x0000000000000001)));
                Assert.Equal(8, BinaryIntegerHelper<nuint>.GetByteCount(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(8, BinaryIntegerHelper<nuint>.GetByteCount(unchecked((nuint)0x8000000000000000)));
                Assert.Equal(8, BinaryIntegerHelper<nuint>.GetByteCount(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal(4, BinaryIntegerHelper<nuint>.GetByteCount((nuint)0x00000000));
                Assert.Equal(4, BinaryIntegerHelper<nuint>.GetByteCount((nuint)0x00000001));
                Assert.Equal(4, BinaryIntegerHelper<nuint>.GetByteCount((nuint)0x7FFFFFFF));
                Assert.Equal(4, BinaryIntegerHelper<nuint>.GetByteCount((nuint)0x80000000));
                Assert.Equal(4, BinaryIntegerHelper<nuint>.GetByteCount((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void GetShortestBitLengthTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(0x00, BinaryIntegerHelper<nuint>.GetShortestBitLength(unchecked((nuint)0x0000000000000000)));
                Assert.Equal(0x01, BinaryIntegerHelper<nuint>.GetShortestBitLength(unchecked((nuint)0x0000000000000001)));
                Assert.Equal(0x3F, BinaryIntegerHelper<nuint>.GetShortestBitLength(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(0x40, BinaryIntegerHelper<nuint>.GetShortestBitLength(unchecked((nuint)0x8000000000000000)));
                Assert.Equal(0x40, BinaryIntegerHelper<nuint>.GetShortestBitLength(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal(0x00, BinaryIntegerHelper<nuint>.GetShortestBitLength((nuint)0x00000000));
                Assert.Equal(0x01, BinaryIntegerHelper<nuint>.GetShortestBitLength((nuint)0x00000001));
                Assert.Equal(0x1F, BinaryIntegerHelper<nuint>.GetShortestBitLength((nuint)0x7FFFFFFF));
                Assert.Equal(0x20, BinaryIntegerHelper<nuint>.GetShortestBitLength((nuint)0x80000000));
                Assert.Equal(0x20, BinaryIntegerHelper<nuint>.GetShortestBitLength((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void TryWriteBigEndianTest()
        {
            if (Environment.Is64BitProcess)
            {
                Span<byte> destination = stackalloc byte[8];
                int bytesWritten = 0;

                Assert.True(BinaryIntegerHelper<nuint>.TryWriteBigEndian(unchecked((nuint)0x0000000000000000), destination, out bytesWritten));
                Assert.Equal(8, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

                Assert.True(BinaryIntegerHelper<nuint>.TryWriteBigEndian(unchecked((nuint)0x0000000000000001), destination, out bytesWritten));
                Assert.Equal(8, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, destination.ToArray());

                Assert.True(BinaryIntegerHelper<nuint>.TryWriteBigEndian(unchecked((nuint)0x7FFFFFFFFFFFFFFF), destination, out bytesWritten));
                Assert.Equal(8, bytesWritten);
                Assert.Equal(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, destination.ToArray());

                Assert.True(BinaryIntegerHelper<nuint>.TryWriteBigEndian(unchecked((nuint)0x8000000000000000), destination, out bytesWritten));
                Assert.Equal(8, bytesWritten);
                Assert.Equal(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

                Assert.True(BinaryIntegerHelper<nuint>.TryWriteBigEndian(unchecked((nuint)0xFFFFFFFFFFFFFFFF), destination, out bytesWritten));
                Assert.Equal(8, bytesWritten);
                Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, destination.ToArray());

                Assert.False(BinaryIntegerHelper<nuint>.TryWriteBigEndian(default, Span<byte>.Empty, out bytesWritten));
                Assert.Equal(0, bytesWritten);
                Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, destination.ToArray());
            }
            else
            {
                Span<byte> destination = stackalloc byte[4];
                int bytesWritten = 0;

                Assert.True(BinaryIntegerHelper<nuint>.TryWriteBigEndian((nuint)0x00000000, destination, out bytesWritten));
                Assert.Equal(4, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

                Assert.True(BinaryIntegerHelper<nuint>.TryWriteBigEndian((nuint)0x00000001, destination, out bytesWritten));
                Assert.Equal(4, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x01 }, destination.ToArray());

                Assert.True(BinaryIntegerHelper<nuint>.TryWriteBigEndian((nuint)0x7FFFFFFF, destination, out bytesWritten));
                Assert.Equal(4, bytesWritten);
                Assert.Equal(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF }, destination.ToArray());

                Assert.True(BinaryIntegerHelper<nuint>.TryWriteBigEndian((nuint)0x80000000, destination, out bytesWritten));
                Assert.Equal(4, bytesWritten);
                Assert.Equal(new byte[] { 0x80, 0x00, 0x00, 0x00 }, destination.ToArray());

                Assert.True(BinaryIntegerHelper<nuint>.TryWriteBigEndian((nuint)0xFFFFFFFF, destination, out bytesWritten));
                Assert.Equal(4, bytesWritten);
                Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, destination.ToArray());

                Assert.False(BinaryIntegerHelper<nuint>.TryWriteBigEndian(default, Span<byte>.Empty, out bytesWritten));
                Assert.Equal(0, bytesWritten);
                Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, destination.ToArray());
            }
        }

        [Fact]
        public static void TryWriteLittleEndianTest()
        {
            if (Environment.Is64BitProcess)
            {
                Span<byte> destination = stackalloc byte[8];
                int bytesWritten = 0;

                Assert.True(BinaryIntegerHelper<nuint>.TryWriteLittleEndian(unchecked((nuint)0x0000000000000000), destination, out bytesWritten));
                Assert.Equal(8, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

                Assert.True(BinaryIntegerHelper<nuint>.TryWriteLittleEndian(unchecked((nuint)0x0000000000000001), destination, out bytesWritten));
                Assert.Equal(8, bytesWritten);
                Assert.Equal(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

                Assert.True(BinaryIntegerHelper<nuint>.TryWriteLittleEndian(unchecked((nuint)0x7FFFFFFFFFFFFFFF), destination, out bytesWritten));
                Assert.Equal(8, bytesWritten);
                Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, destination.ToArray());

                Assert.True(BinaryIntegerHelper<nuint>.TryWriteLittleEndian(unchecked((nuint)0x8000000000000000), destination, out bytesWritten));
                Assert.Equal(8, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, destination.ToArray());

                Assert.True(BinaryIntegerHelper<nuint>.TryWriteLittleEndian(unchecked((nuint)0xFFFFFFFFFFFFFFFF), destination, out bytesWritten));
                Assert.Equal(8, bytesWritten);
                Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, destination.ToArray());

                Assert.False(BinaryIntegerHelper<nuint>.TryWriteLittleEndian(default, Span<byte>.Empty, out bytesWritten));
                Assert.Equal(0, bytesWritten);
                Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, destination.ToArray());
            }
            else
            {
                Span<byte> destination = stackalloc byte[4];
                int bytesWritten = 0;

                Assert.True(BinaryIntegerHelper<nuint>.TryWriteLittleEndian((nuint)0x00000000, destination, out bytesWritten));
                Assert.Equal(4, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

                Assert.True(BinaryIntegerHelper<nuint>.TryWriteLittleEndian((nuint)0x00000001, destination, out bytesWritten));
                Assert.Equal(4, bytesWritten);
                Assert.Equal(new byte[] { 0x01, 0x00, 0x00, 0x00 }, destination.ToArray());

                Assert.True(BinaryIntegerHelper<nuint>.TryWriteLittleEndian((nuint)0x7FFFFFFF, destination, out bytesWritten));
                Assert.Equal(4, bytesWritten);
                Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0x7F }, destination.ToArray());

                Assert.True(BinaryIntegerHelper<nuint>.TryWriteLittleEndian((nuint)0x80000000, destination, out bytesWritten));
                Assert.Equal(4, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x80 }, destination.ToArray());

                Assert.True(BinaryIntegerHelper<nuint>.TryWriteLittleEndian((nuint)0xFFFFFFFF, destination, out bytesWritten));
                Assert.Equal(4, bytesWritten);
                Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, destination.ToArray());

                Assert.False(BinaryIntegerHelper<nuint>.TryWriteLittleEndian(default, Span<byte>.Empty, out bytesWritten));
                Assert.Equal(0, bytesWritten);
                Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, destination.ToArray());
            }
        }

        //
        // IBinaryNumber
        //

        [Fact]
        public static void IsPow2Test()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.False(BinaryNumberHelper<nuint>.IsPow2(unchecked((nuint)0x0000000000000000)));
                Assert.True(BinaryNumberHelper<nuint>.IsPow2(unchecked((nuint)0x0000000000000001)));
                Assert.False(BinaryNumberHelper<nuint>.IsPow2(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.True(BinaryNumberHelper<nuint>.IsPow2(unchecked((nuint)0x8000000000000000)));
                Assert.False(BinaryNumberHelper<nuint>.IsPow2(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.False(BinaryNumberHelper<nuint>.IsPow2((nuint)0x00000000));
                Assert.True(BinaryNumberHelper<nuint>.IsPow2((nuint)0x00000001));
                Assert.False(BinaryNumberHelper<nuint>.IsPow2((nuint)0x7FFFFFFF));
                Assert.True(BinaryNumberHelper<nuint>.IsPow2((nuint)0x80000000));
                Assert.False(BinaryNumberHelper<nuint>.IsPow2((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void Log2Test()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000000000000000), BinaryNumberHelper<nuint>.Log2(unchecked((nuint)0x0000000000000000)));
                Assert.Equal(unchecked((nuint)0x0000000000000000), BinaryNumberHelper<nuint>.Log2(unchecked((nuint)0x0000000000000001)));
                Assert.Equal(unchecked((nuint)0x000000000000003E), BinaryNumberHelper<nuint>.Log2(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((nuint)0x000000000000003F), BinaryNumberHelper<nuint>.Log2(unchecked((nuint)0x8000000000000000)));
                Assert.Equal(unchecked((nuint)0x000000000000003F), BinaryNumberHelper<nuint>.Log2(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((nuint)0x00000000, BinaryNumberHelper<nuint>.Log2((nuint)0x00000000));
                Assert.Equal((nuint)0x00000000, BinaryNumberHelper<nuint>.Log2((nuint)0x00000001));
                Assert.Equal((nuint)0x0000001E, BinaryNumberHelper<nuint>.Log2((nuint)0x7FFFFFFF));
                Assert.Equal((nuint)0x0000001F, BinaryNumberHelper<nuint>.Log2((nuint)0x80000000));
                Assert.Equal((nuint)0x0000001F, BinaryNumberHelper<nuint>.Log2((nuint)0xFFFFFFFF));
            }
        }

        //
        // IBitwiseOperators
        //

        [Fact]
        public static void op_BitwiseAndTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000000000000000), BitwiseOperatorsHelper<nuint, nuint, nuint>.op_BitwiseAnd(unchecked((nuint)0x0000000000000000), (nuint)1));
                Assert.Equal(unchecked((nuint)0x0000000000000001), BitwiseOperatorsHelper<nuint, nuint, nuint>.op_BitwiseAnd(unchecked((nuint)0x0000000000000001), (nuint)1));
                Assert.Equal(unchecked((nuint)0x0000000000000001), BitwiseOperatorsHelper<nuint, nuint, nuint>.op_BitwiseAnd(unchecked((nuint)0x7FFFFFFFFFFFFFFF), (nuint)1));
                Assert.Equal(unchecked((nuint)0x0000000000000000), BitwiseOperatorsHelper<nuint, nuint, nuint>.op_BitwiseAnd(unchecked((nuint)0x8000000000000000), (nuint)1));
                Assert.Equal(unchecked((nuint)0x0000000000000001), BitwiseOperatorsHelper<nuint, nuint, nuint>.op_BitwiseAnd(unchecked((nuint)0xFFFFFFFFFFFFFFFF), (nuint)1));
            }
            else
            {
                Assert.Equal((nuint)0x00000000, BitwiseOperatorsHelper<nuint, nuint, nuint>.op_BitwiseAnd((nuint)0x00000000, (nuint)1));
                Assert.Equal((nuint)0x00000001, BitwiseOperatorsHelper<nuint, nuint, nuint>.op_BitwiseAnd((nuint)0x00000001, (nuint)1));
                Assert.Equal((nuint)0x00000001, BitwiseOperatorsHelper<nuint, nuint, nuint>.op_BitwiseAnd((nuint)0x7FFFFFFF, (nuint)1));
                Assert.Equal((nuint)0x00000000, BitwiseOperatorsHelper<nuint, nuint, nuint>.op_BitwiseAnd((nuint)0x80000000, (nuint)1));
                Assert.Equal((nuint)0x00000001, BitwiseOperatorsHelper<nuint, nuint, nuint>.op_BitwiseAnd((nuint)0xFFFFFFFF, (nuint)1));
            }
        }

        [Fact]
        public static void op_BitwiseOrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000000000000001), BitwiseOperatorsHelper<nuint, nuint, nuint>.op_BitwiseOr(unchecked((nuint)0x0000000000000000), (nuint)1));
                Assert.Equal(unchecked((nuint)0x0000000000000001), BitwiseOperatorsHelper<nuint, nuint, nuint>.op_BitwiseOr(unchecked((nuint)0x0000000000000001), (nuint)1));
                Assert.Equal(unchecked((nuint)0x7FFFFFFFFFFFFFFF), BitwiseOperatorsHelper<nuint, nuint, nuint>.op_BitwiseOr(unchecked((nuint)0x7FFFFFFFFFFFFFFF), (nuint)1));
                Assert.Equal(unchecked((nuint)0x8000000000000001), BitwiseOperatorsHelper<nuint, nuint, nuint>.op_BitwiseOr(unchecked((nuint)0x8000000000000000), (nuint)1));
                Assert.Equal(unchecked((nuint)0xFFFFFFFFFFFFFFFF), BitwiseOperatorsHelper<nuint, nuint, nuint>.op_BitwiseOr(unchecked((nuint)0xFFFFFFFFFFFFFFFF), (nuint)1));
            }
            else
            {
                Assert.Equal((nuint)0x00000001, BitwiseOperatorsHelper<nuint, nuint, nuint>.op_BitwiseOr((nuint)0x00000000, (nuint)1));
                Assert.Equal((nuint)0x00000001, BitwiseOperatorsHelper<nuint, nuint, nuint>.op_BitwiseOr((nuint)0x00000001, (nuint)1));
                Assert.Equal((nuint)0x7FFFFFFF, BitwiseOperatorsHelper<nuint, nuint, nuint>.op_BitwiseOr((nuint)0x7FFFFFFF, (nuint)1));
                Assert.Equal((nuint)0x80000001, BitwiseOperatorsHelper<nuint, nuint, nuint>.op_BitwiseOr((nuint)0x80000000, (nuint)1));
                Assert.Equal((nuint)0xFFFFFFFF, BitwiseOperatorsHelper<nuint, nuint, nuint>.op_BitwiseOr((nuint)0xFFFFFFFF, (nuint)1));
            }
        }

        [Fact]
        public static void op_ExclusiveOrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000000000000001), BitwiseOperatorsHelper<nuint, nuint, nuint>.op_ExclusiveOr(unchecked((nuint)0x0000000000000000), (nuint)1));
                Assert.Equal(unchecked((nuint)0x0000000000000000), BitwiseOperatorsHelper<nuint, nuint, nuint>.op_ExclusiveOr(unchecked((nuint)0x0000000000000001), (nuint)1));
                Assert.Equal(unchecked((nuint)0x7FFFFFFFFFFFFFFE), BitwiseOperatorsHelper<nuint, nuint, nuint>.op_ExclusiveOr(unchecked((nuint)0x7FFFFFFFFFFFFFFF), (nuint)1));
                Assert.Equal(unchecked((nuint)0x8000000000000001), BitwiseOperatorsHelper<nuint, nuint, nuint>.op_ExclusiveOr(unchecked((nuint)0x8000000000000000), (nuint)1));
                Assert.Equal(unchecked((nuint)0xFFFFFFFFFFFFFFFE), BitwiseOperatorsHelper<nuint, nuint, nuint>.op_ExclusiveOr(unchecked((nuint)0xFFFFFFFFFFFFFFFF), (nuint)1));
            }
            else
            {
                Assert.Equal((nuint)0x00000001, BitwiseOperatorsHelper<nuint, nuint, nuint>.op_ExclusiveOr((nuint)0x00000000, (nuint)1));
                Assert.Equal((nuint)0x00000000, BitwiseOperatorsHelper<nuint, nuint, nuint>.op_ExclusiveOr((nuint)0x00000001, (nuint)1));
                Assert.Equal((nuint)0x7FFFFFFE, BitwiseOperatorsHelper<nuint, nuint, nuint>.op_ExclusiveOr((nuint)0x7FFFFFFF, (nuint)1));
                Assert.Equal((nuint)0x80000001, BitwiseOperatorsHelper<nuint, nuint, nuint>.op_ExclusiveOr((nuint)0x80000000, (nuint)1));
                Assert.Equal((nuint)0xFFFFFFFE, BitwiseOperatorsHelper<nuint, nuint, nuint>.op_ExclusiveOr((nuint)0xFFFFFFFF, (nuint)1));
            }
        }

        [Fact]
        public static void op_OnesComplementTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0xFFFFFFFFFFFFFFFF), BitwiseOperatorsHelper<nuint, nuint, nuint>.op_OnesComplement(unchecked((nuint)0x0000000000000000)));
                Assert.Equal(unchecked((nuint)0xFFFFFFFFFFFFFFFE), BitwiseOperatorsHelper<nuint, nuint, nuint>.op_OnesComplement(unchecked((nuint)0x0000000000000001)));
                Assert.Equal(unchecked((nuint)0x8000000000000000), BitwiseOperatorsHelper<nuint, nuint, nuint>.op_OnesComplement(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((nuint)0x7FFFFFFFFFFFFFFF), BitwiseOperatorsHelper<nuint, nuint, nuint>.op_OnesComplement(unchecked((nuint)0x8000000000000000)));
                Assert.Equal(unchecked((nuint)0x0000000000000000), BitwiseOperatorsHelper<nuint, nuint, nuint>.op_OnesComplement(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((nuint)0xFFFFFFFF, BitwiseOperatorsHelper<nuint, nuint, nuint>.op_OnesComplement((nuint)0x00000000));
                Assert.Equal((nuint)0xFFFFFFFE, BitwiseOperatorsHelper<nuint, nuint, nuint>.op_OnesComplement((nuint)0x00000001));
                Assert.Equal((nuint)0x80000000, BitwiseOperatorsHelper<nuint, nuint, nuint>.op_OnesComplement((nuint)0x7FFFFFFF));
                Assert.Equal((nuint)0x7FFFFFFF, BitwiseOperatorsHelper<nuint, nuint, nuint>.op_OnesComplement((nuint)0x80000000));
                Assert.Equal((nuint)0x00000000, BitwiseOperatorsHelper<nuint, nuint, nuint>.op_OnesComplement((nuint)0xFFFFFFFF));
            }
        }

        //
        // IComparisonOperators
        //

        [Fact]
        public static void op_GreaterThanTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.False(ComparisonOperatorsHelper<nuint, nuint>.op_GreaterThan(unchecked((nuint)0x0000000000000000), (nuint)1));
                Assert.False(ComparisonOperatorsHelper<nuint, nuint>.op_GreaterThan(unchecked((nuint)0x0000000000000001), (nuint)1));
                Assert.True(ComparisonOperatorsHelper<nuint, nuint>.op_GreaterThan(unchecked((nuint)0x7FFFFFFFFFFFFFFF), (nuint)1));
                Assert.True(ComparisonOperatorsHelper<nuint, nuint>.op_GreaterThan(unchecked((nuint)0x8000000000000000), (nuint)1));
                Assert.True(ComparisonOperatorsHelper<nuint, nuint>.op_GreaterThan(unchecked((nuint)0xFFFFFFFFFFFFFFFF), (nuint)1));
            }
            else
            {
                Assert.False(ComparisonOperatorsHelper<nuint, nuint>.op_GreaterThan((nuint)0x00000000, (nuint)1));
                Assert.False(ComparisonOperatorsHelper<nuint, nuint>.op_GreaterThan((nuint)0x00000001, (nuint)1));
                Assert.True(ComparisonOperatorsHelper<nuint, nuint>.op_GreaterThan((nuint)0x7FFFFFFF, (nuint)1));
                Assert.True(ComparisonOperatorsHelper<nuint, nuint>.op_GreaterThan((nuint)0x80000000, (nuint)1));
                Assert.True(ComparisonOperatorsHelper<nuint, nuint>.op_GreaterThan((nuint)0xFFFFFFFF, (nuint)1));
            }
        }

        [Fact]
        public static void op_GreaterThanOrEqualTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.False(ComparisonOperatorsHelper<nuint, nuint>.op_GreaterThanOrEqual(unchecked((nuint)0x0000000000000000), (nuint)1));
                Assert.True(ComparisonOperatorsHelper<nuint, nuint>.op_GreaterThanOrEqual(unchecked((nuint)0x0000000000000001), (nuint)1));
                Assert.True(ComparisonOperatorsHelper<nuint, nuint>.op_GreaterThanOrEqual(unchecked((nuint)0x7FFFFFFFFFFFFFFF), (nuint)1));
                Assert.True(ComparisonOperatorsHelper<nuint, nuint>.op_GreaterThanOrEqual(unchecked((nuint)0x8000000000000000), (nuint)1));
                Assert.True(ComparisonOperatorsHelper<nuint, nuint>.op_GreaterThanOrEqual(unchecked((nuint)0xFFFFFFFFFFFFFFFF), (nuint)1));
            }
            else
            {
                Assert.False(ComparisonOperatorsHelper<nuint, nuint>.op_GreaterThanOrEqual((nuint)0x00000000, (nuint)1));
                Assert.True(ComparisonOperatorsHelper<nuint, nuint>.op_GreaterThanOrEqual((nuint)0x00000001, (nuint)1));
                Assert.True(ComparisonOperatorsHelper<nuint, nuint>.op_GreaterThanOrEqual((nuint)0x7FFFFFFF, (nuint)1));
                Assert.True(ComparisonOperatorsHelper<nuint, nuint>.op_GreaterThanOrEqual((nuint)0x80000000, (nuint)1));
                Assert.True(ComparisonOperatorsHelper<nuint, nuint>.op_GreaterThanOrEqual((nuint)0xFFFFFFFF, (nuint)1));
            }
        }

        [Fact]
        public static void op_LessThanTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.True(ComparisonOperatorsHelper<nuint, nuint>.op_LessThan(unchecked((nuint)0x0000000000000000), (nuint)1));
                Assert.False(ComparisonOperatorsHelper<nuint, nuint>.op_LessThan(unchecked((nuint)0x0000000000000001), (nuint)1));
                Assert.False(ComparisonOperatorsHelper<nuint, nuint>.op_LessThan(unchecked((nuint)0x7FFFFFFFFFFFFFFF), (nuint)1));
                Assert.False(ComparisonOperatorsHelper<nuint, nuint>.op_LessThan(unchecked((nuint)0x8000000000000000), (nuint)1));
                Assert.False(ComparisonOperatorsHelper<nuint, nuint>.op_LessThan(unchecked((nuint)0xFFFFFFFFFFFFFFFF), (nuint)1));
            }
            else
            {
                Assert.True(ComparisonOperatorsHelper<nuint, nuint>.op_LessThan((nuint)0x00000000, (nuint)1));
                Assert.False(ComparisonOperatorsHelper<nuint, nuint>.op_LessThan((nuint)0x00000001, (nuint)1));
                Assert.False(ComparisonOperatorsHelper<nuint, nuint>.op_LessThan((nuint)0x7FFFFFFF, (nuint)1));
                Assert.False(ComparisonOperatorsHelper<nuint, nuint>.op_LessThan((nuint)0x80000000, (nuint)1));
                Assert.False(ComparisonOperatorsHelper<nuint, nuint>.op_LessThan((nuint)0xFFFFFFFF, (nuint)1));
            }
        }

        [Fact]
        public static void op_LessThanOrEqualTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.True(ComparisonOperatorsHelper<nuint, nuint>.op_LessThanOrEqual(unchecked((nuint)0x0000000000000000), (nuint)1));
                Assert.True(ComparisonOperatorsHelper<nuint, nuint>.op_LessThanOrEqual(unchecked((nuint)0x0000000000000001), (nuint)1));
                Assert.False(ComparisonOperatorsHelper<nuint, nuint>.op_LessThanOrEqual(unchecked((nuint)0x7FFFFFFFFFFFFFFF), (nuint)1));
                Assert.False(ComparisonOperatorsHelper<nuint, nuint>.op_LessThanOrEqual(unchecked((nuint)0x8000000000000000), (nuint)1));
                Assert.False(ComparisonOperatorsHelper<nuint, nuint>.op_LessThanOrEqual(unchecked((nuint)0xFFFFFFFFFFFFFFFF), (nuint)1));
            }
            else
            {
                Assert.True(ComparisonOperatorsHelper<nuint, nuint>.op_LessThanOrEqual((nuint)0x00000000, (nuint)1));
                Assert.True(ComparisonOperatorsHelper<nuint, nuint>.op_LessThanOrEqual((nuint)0x00000001, (nuint)1));
                Assert.False(ComparisonOperatorsHelper<nuint, nuint>.op_LessThanOrEqual((nuint)0x7FFFFFFF, (nuint)1));
                Assert.False(ComparisonOperatorsHelper<nuint, nuint>.op_LessThanOrEqual((nuint)0x80000000, (nuint)1));
                Assert.False(ComparisonOperatorsHelper<nuint, nuint>.op_LessThanOrEqual((nuint)0xFFFFFFFF, (nuint)1));
            }
        }

        //
        // IDecrementOperators
        //

        [Fact]
        public static void op_DecrementTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0xFFFFFFFFFFFFFFFF), DecrementOperatorsHelper<nuint>.op_Decrement(unchecked((nuint)0x0000000000000000)));
                Assert.Equal(unchecked((nuint)0x0000000000000000), DecrementOperatorsHelper<nuint>.op_Decrement(unchecked((nuint)0x0000000000000001)));
                Assert.Equal(unchecked((nuint)0x7FFFFFFFFFFFFFFE), DecrementOperatorsHelper<nuint>.op_Decrement(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((nuint)0x7FFFFFFFFFFFFFFF), DecrementOperatorsHelper<nuint>.op_Decrement(unchecked((nuint)0x8000000000000000)));
                Assert.Equal(unchecked((nuint)0xFFFFFFFFFFFFFFFE), DecrementOperatorsHelper<nuint>.op_Decrement(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((nuint)0xFFFFFFFF, DecrementOperatorsHelper<nuint>.op_Decrement((nuint)0x00000000));
                Assert.Equal((nuint)0x00000000, DecrementOperatorsHelper<nuint>.op_Decrement((nuint)0x00000001));
                Assert.Equal((nuint)0x7FFFFFFE, DecrementOperatorsHelper<nuint>.op_Decrement((nuint)0x7FFFFFFF));
                Assert.Equal((nuint)0x7FFFFFFF, DecrementOperatorsHelper<nuint>.op_Decrement((nuint)0x80000000));
                Assert.Equal((nuint)0xFFFFFFFE, DecrementOperatorsHelper<nuint>.op_Decrement((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void op_CheckedDecrementTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000000000000000), DecrementOperatorsHelper<nuint>.op_CheckedDecrement(unchecked((nuint)0x0000000000000001)));
                Assert.Equal(unchecked((nuint)0x7FFFFFFFFFFFFFFE), DecrementOperatorsHelper<nuint>.op_CheckedDecrement(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((nuint)0x7FFFFFFFFFFFFFFF), DecrementOperatorsHelper<nuint>.op_CheckedDecrement(unchecked((nuint)0x8000000000000000)));
                Assert.Equal(unchecked((nuint)0xFFFFFFFFFFFFFFFE), DecrementOperatorsHelper<nuint>.op_CheckedDecrement(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));

                Assert.Throws<OverflowException>(() => DecrementOperatorsHelper<nuint>.op_CheckedDecrement(unchecked((nuint)0x0000000000000000)));
            }
            else
            {
                Assert.Equal((nuint)0x00000000, DecrementOperatorsHelper<nuint>.op_CheckedDecrement((nuint)0x00000001));
                Assert.Equal((nuint)0x7FFFFFFE, DecrementOperatorsHelper<nuint>.op_CheckedDecrement((nuint)0x7FFFFFFF));
                Assert.Equal((nuint)0x7FFFFFFF, DecrementOperatorsHelper<nuint>.op_CheckedDecrement((nuint)0x80000000));
                Assert.Equal((nuint)0xFFFFFFFE, DecrementOperatorsHelper<nuint>.op_CheckedDecrement((nuint)0xFFFFFFFF));

                Assert.Throws<OverflowException>(() => DecrementOperatorsHelper<nuint>.op_CheckedDecrement((nuint)0x00000000));
            }
        }

        //
        // IDivisionOperators
        //

        [Fact]
        public static void op_DivisionTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000000000000000), DivisionOperatorsHelper<nuint, nuint, nuint>.op_Division(unchecked((nuint)0x0000000000000000), (nuint)2));
                Assert.Equal(unchecked((nuint)0x0000000000000000), DivisionOperatorsHelper<nuint, nuint, nuint>.op_Division(unchecked((nuint)0x0000000000000001), (nuint)2));
                Assert.Equal(unchecked((nuint)0x3FFFFFFFFFFFFFFF), DivisionOperatorsHelper<nuint, nuint, nuint>.op_Division(unchecked((nuint)0x7FFFFFFFFFFFFFFF), (nuint)2));
                Assert.Equal(unchecked((nuint)0x4000000000000000), DivisionOperatorsHelper<nuint, nuint, nuint>.op_Division(unchecked((nuint)0x8000000000000000), (nuint)2));
                Assert.Equal(unchecked((nuint)0x7FFFFFFFFFFFFFFF), DivisionOperatorsHelper<nuint, nuint, nuint>.op_Division(unchecked((nuint)0xFFFFFFFFFFFFFFFF), (nuint)2));

                Assert.Throws<DivideByZeroException>(() => DivisionOperatorsHelper<nuint, nuint, nuint>.op_Division(unchecked((nuint)0x0000000000000001), (nuint)0));
            }
            else
            {
                Assert.Equal((nuint)0x00000000, DivisionOperatorsHelper<nuint, nuint, nuint>.op_Division((nuint)0x00000000, (nuint)2));
                Assert.Equal((nuint)0x00000000, DivisionOperatorsHelper<nuint, nuint, nuint>.op_Division((nuint)0x00000001, (nuint)2));
                Assert.Equal((nuint)0x3FFFFFFF, DivisionOperatorsHelper<nuint, nuint, nuint>.op_Division((nuint)0x7FFFFFFF, (nuint)2));
                Assert.Equal((nuint)0x40000000, DivisionOperatorsHelper<nuint, nuint, nuint>.op_Division((nuint)0x80000000, (nuint)2));
                Assert.Equal((nuint)0x7FFFFFFF, DivisionOperatorsHelper<nuint, nuint, nuint>.op_Division((nuint)0xFFFFFFFF, (nuint)2));

                Assert.Throws<DivideByZeroException>(() => DivisionOperatorsHelper<nuint, nuint, nuint>.op_Division((nuint)0x00000001, (nuint)0));
            }
        }

        [Fact]
        public static void op_CheckedDivisionTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000000000000000), DivisionOperatorsHelper<nuint, nuint, nuint>.op_CheckedDivision(unchecked((nuint)0x0000000000000000), (nuint)2));
                Assert.Equal(unchecked((nuint)0x0000000000000000), DivisionOperatorsHelper<nuint, nuint, nuint>.op_CheckedDivision(unchecked((nuint)0x0000000000000001), (nuint)2));
                Assert.Equal(unchecked((nuint)0x3FFFFFFFFFFFFFFF), DivisionOperatorsHelper<nuint, nuint, nuint>.op_CheckedDivision(unchecked((nuint)0x7FFFFFFFFFFFFFFF), (nuint)2));
                Assert.Equal(unchecked((nuint)0x4000000000000000), DivisionOperatorsHelper<nuint, nuint, nuint>.op_CheckedDivision(unchecked((nuint)0x8000000000000000), (nuint)2));
                Assert.Equal(unchecked((nuint)0x7FFFFFFFFFFFFFFF), DivisionOperatorsHelper<nuint, nuint, nuint>.op_CheckedDivision(unchecked((nuint)0xFFFFFFFFFFFFFFFF), (nuint)2));

                Assert.Throws<DivideByZeroException>(() => DivisionOperatorsHelper<nuint, nuint, nuint>.op_CheckedDivision(unchecked((nuint)0x0000000000000001), (nuint)0));
            }
            else
            {
                Assert.Equal((nuint)0x00000000, DivisionOperatorsHelper<nuint, nuint, nuint>.op_CheckedDivision((nuint)0x00000000, (nuint)2));
                Assert.Equal((nuint)0x00000000, DivisionOperatorsHelper<nuint, nuint, nuint>.op_CheckedDivision((nuint)0x00000001, (nuint)2));
                Assert.Equal((nuint)0x3FFFFFFF, DivisionOperatorsHelper<nuint, nuint, nuint>.op_CheckedDivision((nuint)0x7FFFFFFF, (nuint)2));
                Assert.Equal((nuint)0x40000000, DivisionOperatorsHelper<nuint, nuint, nuint>.op_CheckedDivision((nuint)0x80000000, (nuint)2));
                Assert.Equal((nuint)0x7FFFFFFF, DivisionOperatorsHelper<nuint, nuint, nuint>.op_CheckedDivision((nuint)0xFFFFFFFF, (nuint)2));

                Assert.Throws<DivideByZeroException>(() => DivisionOperatorsHelper<nuint, nuint, nuint>.op_CheckedDivision((nuint)0x00000001, (nuint)0));
            }
        }

        //
        // IEqualityOperators
        //

        [Fact]
        public static void op_EqualityTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.False(EqualityOperatorsHelper<nuint, nuint>.op_Equality(unchecked((nuint)0x0000000000000000), (nuint)1));
                Assert.True(EqualityOperatorsHelper<nuint, nuint>.op_Equality(unchecked((nuint)0x0000000000000001), (nuint)1));
                Assert.False(EqualityOperatorsHelper<nuint, nuint>.op_Equality(unchecked((nuint)0x7FFFFFFFFFFFFFFF), (nuint)1));
                Assert.False(EqualityOperatorsHelper<nuint, nuint>.op_Equality(unchecked((nuint)0x8000000000000000), (nuint)1));
                Assert.False(EqualityOperatorsHelper<nuint, nuint>.op_Equality(unchecked((nuint)0xFFFFFFFFFFFFFFFF), (nuint)1));
            }
            else
            {
                Assert.False(EqualityOperatorsHelper<nuint, nuint>.op_Equality((nuint)0x00000000, (nuint)1));
                Assert.True(EqualityOperatorsHelper<nuint, nuint>.op_Equality((nuint)0x00000001, (nuint)1));
                Assert.False(EqualityOperatorsHelper<nuint, nuint>.op_Equality((nuint)0x7FFFFFFF, (nuint)1));
                Assert.False(EqualityOperatorsHelper<nuint, nuint>.op_Equality((nuint)0x80000000, (nuint)1));
                Assert.False(EqualityOperatorsHelper<nuint, nuint>.op_Equality((nuint)0xFFFFFFFF, (nuint)1));
            }
        }

        [Fact]
        public static void op_InequalityTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.True(EqualityOperatorsHelper<nuint, nuint>.op_Inequality(unchecked((nuint)0x0000000000000000), (nuint)1));
                Assert.False(EqualityOperatorsHelper<nuint, nuint>.op_Inequality(unchecked((nuint)0x0000000000000001), (nuint)1));
                Assert.True(EqualityOperatorsHelper<nuint, nuint>.op_Inequality(unchecked((nuint)0x7FFFFFFFFFFFFFFF), (nuint)1));
                Assert.True(EqualityOperatorsHelper<nuint, nuint>.op_Inequality(unchecked((nuint)0x8000000000000000), (nuint)1));
                Assert.True(EqualityOperatorsHelper<nuint, nuint>.op_Inequality(unchecked((nuint)0xFFFFFFFFFFFFFFFF), (nuint)1));
            }
            else
            {
                Assert.True(EqualityOperatorsHelper<nuint, nuint>.op_Inequality((nuint)0x00000000, (nuint)1));
                Assert.False(EqualityOperatorsHelper<nuint, nuint>.op_Inequality((nuint)0x00000001, (nuint)1));
                Assert.True(EqualityOperatorsHelper<nuint, nuint>.op_Inequality((nuint)0x7FFFFFFF, (nuint)1));
                Assert.True(EqualityOperatorsHelper<nuint, nuint>.op_Inequality((nuint)0x80000000, (nuint)1));
                Assert.True(EqualityOperatorsHelper<nuint, nuint>.op_Inequality((nuint)0xFFFFFFFF, (nuint)1));
            }
        }

        //
        // IIncrementOperators
        //

        [Fact]
        public static void op_IncrementTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000000000000001), IncrementOperatorsHelper<nuint>.op_Increment(unchecked((nuint)0x0000000000000000)));
                Assert.Equal(unchecked((nuint)0x0000000000000002), IncrementOperatorsHelper<nuint>.op_Increment(unchecked((nuint)0x0000000000000001)));
                Assert.Equal(unchecked((nuint)0x8000000000000000), IncrementOperatorsHelper<nuint>.op_Increment(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((nuint)0x8000000000000001), IncrementOperatorsHelper<nuint>.op_Increment(unchecked((nuint)0x8000000000000000)));
                Assert.Equal(unchecked((nuint)0x0000000000000000), IncrementOperatorsHelper<nuint>.op_Increment(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((nuint)0x00000001, IncrementOperatorsHelper<nuint>.op_Increment((nuint)0x00000000));
                Assert.Equal((nuint)0x00000002, IncrementOperatorsHelper<nuint>.op_Increment((nuint)0x00000001));
                Assert.Equal((nuint)0x80000000, IncrementOperatorsHelper<nuint>.op_Increment((nuint)0x7FFFFFFF));
                Assert.Equal((nuint)0x80000001, IncrementOperatorsHelper<nuint>.op_Increment((nuint)0x80000000));
                Assert.Equal((nuint)0x00000000, IncrementOperatorsHelper<nuint>.op_Increment((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void op_CheckedIncrementTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000000000000001), IncrementOperatorsHelper<nuint>.op_CheckedIncrement(unchecked((nuint)0x0000000000000000)));
                Assert.Equal(unchecked((nuint)0x0000000000000002), IncrementOperatorsHelper<nuint>.op_CheckedIncrement(unchecked((nuint)0x0000000000000001)));
                Assert.Equal(unchecked((nuint)0x8000000000000000), IncrementOperatorsHelper<nuint>.op_CheckedIncrement(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((nuint)0x8000000000000001), IncrementOperatorsHelper<nuint>.op_CheckedIncrement(unchecked((nuint)0x8000000000000000)));

                Assert.Throws<OverflowException>(() => IncrementOperatorsHelper<nuint>.op_CheckedIncrement(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((nuint)0x00000001, IncrementOperatorsHelper<nuint>.op_CheckedIncrement((nuint)0x00000000));
                Assert.Equal((nuint)0x00000002, IncrementOperatorsHelper<nuint>.op_CheckedIncrement((nuint)0x00000001));
                Assert.Equal((nuint)0x80000000, IncrementOperatorsHelper<nuint>.op_CheckedIncrement((nuint)0x7FFFFFFF));
                Assert.Equal((nuint)0x80000001, IncrementOperatorsHelper<nuint>.op_CheckedIncrement((nuint)0x80000000));

                Assert.Throws<OverflowException>(() => IncrementOperatorsHelper<nuint>.op_CheckedIncrement((nuint)0xFFFFFFFF));
            }
        }

        //
        // IMinMaxValue
        //

        [Fact]
        public static void MaxValueTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0xFFFFFFFFFFFFFFFF), MinMaxValueHelper<nuint>.MaxValue);
            }
            else
            {
                Assert.Equal((nuint)0xFFFFFFFF, MinMaxValueHelper<nuint>.MaxValue);
            }
        }

        [Fact]
        public static void MinValueTest()
        {
            Assert.Equal((nuint)0x00000000, MinMaxValueHelper<nuint>.MinValue);
        }

        //
        // IModulusOperators
        //

        [Fact]
        public static void op_ModulusTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000000000000000), ModulusOperatorsHelper<nuint, nuint, nuint>.op_Modulus(unchecked((nuint)0x0000000000000000), (nuint)2));
                Assert.Equal(unchecked((nuint)0x0000000000000001), ModulusOperatorsHelper<nuint, nuint, nuint>.op_Modulus(unchecked((nuint)0x0000000000000001), (nuint)2));
                Assert.Equal(unchecked((nuint)0x0000000000000001), ModulusOperatorsHelper<nuint, nuint, nuint>.op_Modulus(unchecked((nuint)0x7FFFFFFFFFFFFFFF), (nuint)2));
                Assert.Equal(unchecked((nuint)0x0000000000000000), ModulusOperatorsHelper<nuint, nuint, nuint>.op_Modulus(unchecked((nuint)0x8000000000000000), (nuint)2));
                Assert.Equal(unchecked((nuint)0x0000000000000001), ModulusOperatorsHelper<nuint, nuint, nuint>.op_Modulus(unchecked((nuint)0xFFFFFFFFFFFFFFFF), (nuint)2));

                Assert.Throws<DivideByZeroException>(() => ModulusOperatorsHelper<nuint, nuint, nuint>.op_Modulus(unchecked((nuint)0x0000000000000001), (nuint)0));
            }
            else
            {
                Assert.Equal((nuint)0x00000000, ModulusOperatorsHelper<nuint, nuint, nuint>.op_Modulus((nuint)0x00000000, (nuint)2));
                Assert.Equal((nuint)0x00000001, ModulusOperatorsHelper<nuint, nuint, nuint>.op_Modulus((nuint)0x00000001, (nuint)2));
                Assert.Equal((nuint)0x00000001, ModulusOperatorsHelper<nuint, nuint, nuint>.op_Modulus((nuint)0x7FFFFFFF, (nuint)2));
                Assert.Equal((nuint)0x00000000, ModulusOperatorsHelper<nuint, nuint, nuint>.op_Modulus((nuint)0x80000000, (nuint)2));
                Assert.Equal((nuint)0x00000001, ModulusOperatorsHelper<nuint, nuint, nuint>.op_Modulus((nuint)0xFFFFFFFF, (nuint)2));

                Assert.Throws<DivideByZeroException>(() => ModulusOperatorsHelper<nuint, nuint, nuint>.op_Modulus((nuint)0x00000001, (nuint)0));
            }
        }

        //
        // IMultiplicativeIdentity
        //

        [Fact]
        public static void MultiplicativeIdentityTest()
        {
            Assert.Equal((nuint)0x00000001, MultiplicativeIdentityHelper<nuint, nuint>.MultiplicativeIdentity);
        }

        //
        // IMultiplyOperators
        //

        [Fact]
        public static void op_MultiplyTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000000000000000), MultiplyOperatorsHelper<nuint, nuint, nuint>.op_Multiply(unchecked((nuint)0x0000000000000000), (nuint)2));
                Assert.Equal(unchecked((nuint)0x0000000000000002), MultiplyOperatorsHelper<nuint, nuint, nuint>.op_Multiply(unchecked((nuint)0x0000000000000001), (nuint)2));
                Assert.Equal(unchecked((nuint)0xFFFFFFFFFFFFFFFE), MultiplyOperatorsHelper<nuint, nuint, nuint>.op_Multiply(unchecked((nuint)0x7FFFFFFFFFFFFFFF), (nuint)2));
                Assert.Equal(unchecked((nuint)0x0000000000000000), MultiplyOperatorsHelper<nuint, nuint, nuint>.op_Multiply(unchecked((nuint)0x8000000000000000), (nuint)2));
                Assert.Equal(unchecked((nuint)0xFFFFFFFFFFFFFFFE), MultiplyOperatorsHelper<nuint, nuint, nuint>.op_Multiply(unchecked((nuint)0xFFFFFFFFFFFFFFFF), (nuint)2));
            }
            else
            {
                Assert.Equal((nuint)0x00000000, MultiplyOperatorsHelper<nuint, nuint, nuint>.op_Multiply((nuint)0x00000000, (nuint)2));
                Assert.Equal((nuint)0x00000002, MultiplyOperatorsHelper<nuint, nuint, nuint>.op_Multiply((nuint)0x00000001, (nuint)2));
                Assert.Equal((nuint)0xFFFFFFFE, MultiplyOperatorsHelper<nuint, nuint, nuint>.op_Multiply((nuint)0x7FFFFFFF, (nuint)2));
                Assert.Equal((nuint)0x00000000, MultiplyOperatorsHelper<nuint, nuint, nuint>.op_Multiply((nuint)0x80000000, (nuint)2));
                Assert.Equal((nuint)0xFFFFFFFE, MultiplyOperatorsHelper<nuint, nuint, nuint>.op_Multiply((nuint)0xFFFFFFFF, (nuint)2));
            }
        }

        [Fact]
        public static void op_CheckedMultiplyTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000000000000000), MultiplyOperatorsHelper<nuint, nuint, nuint>.op_CheckedMultiply(unchecked((nuint)0x0000000000000000), (nuint)2));
                Assert.Equal(unchecked((nuint)0x0000000000000002), MultiplyOperatorsHelper<nuint, nuint, nuint>.op_CheckedMultiply(unchecked((nuint)0x0000000000000001), (nuint)2));
                Assert.Equal(unchecked((nuint)0xFFFFFFFFFFFFFFFE), MultiplyOperatorsHelper<nuint, nuint, nuint>.op_CheckedMultiply(unchecked((nuint)0x7FFFFFFFFFFFFFFF), (nuint)2));

                Assert.Throws<OverflowException>(() => MultiplyOperatorsHelper<nuint, nuint, nuint>.op_CheckedMultiply(unchecked((nuint)0x8000000000000000), (nuint)2));
                Assert.Throws<OverflowException>(() => MultiplyOperatorsHelper<nuint, nuint, nuint>.op_CheckedMultiply(unchecked((nuint)0xFFFFFFFFFFFFFFFF), (nuint)2));
            }
            else
            {
                Assert.Equal((nuint)0x00000000, MultiplyOperatorsHelper<nuint, nuint, nuint>.op_CheckedMultiply((nuint)0x00000000, (nuint)2));
                Assert.Equal((nuint)0x00000002, MultiplyOperatorsHelper<nuint, nuint, nuint>.op_CheckedMultiply((nuint)0x00000001, (nuint)2));
                Assert.Equal((nuint)0xFFFFFFFE, MultiplyOperatorsHelper<nuint, nuint, nuint>.op_CheckedMultiply((nuint)0x7FFFFFFF, (nuint)2));

                Assert.Throws<OverflowException>(() => MultiplyOperatorsHelper<nuint, nuint, nuint>.op_CheckedMultiply((nuint)0x80000000, (nuint)2));
                Assert.Throws<OverflowException>(() => MultiplyOperatorsHelper<nuint, nuint, nuint>.op_CheckedMultiply((nuint)0xFFFFFFFF, (nuint)2));
            }
        }

        //
        // INumber
        //

        [Fact]
        public static void ClampTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000000000000001), NumberHelper<nuint>.Clamp(unchecked((nuint)0x0000000000000000), unchecked((nuint)0x0000000000000001), unchecked((nuint)0x000000000000003F)));
                Assert.Equal(unchecked((nuint)0x0000000000000001), NumberHelper<nuint>.Clamp(unchecked((nuint)0x0000000000000001), unchecked((nuint)0x0000000000000001), unchecked((nuint)0x000000000000003F)));
                Assert.Equal(unchecked((nuint)0x000000000000003F), NumberHelper<nuint>.Clamp(unchecked((nuint)0x7FFFFFFFFFFFFFFF), unchecked((nuint)0x0000000000000001), unchecked((nuint)0x000000000000003F)));
                Assert.Equal(unchecked((nuint)0x000000000000003F), NumberHelper<nuint>.Clamp(unchecked((nuint)0x8000000000000000), unchecked((nuint)0x0000000000000001), unchecked((nuint)0x000000000000003F)));
                Assert.Equal(unchecked((nuint)0x000000000000003F), NumberHelper<nuint>.Clamp(unchecked((nuint)0xFFFFFFFFFFFFFFFF), unchecked((nuint)0x0000000000000001), unchecked((nuint)0x000000000000003F)));
            }
            else
            {
                Assert.Equal((nuint)0x00000001, NumberHelper<nuint>.Clamp((nuint)0x00000000, (nuint)0x00000001, (nuint)0x0000003F));
                Assert.Equal((nuint)0x00000001, NumberHelper<nuint>.Clamp((nuint)0x00000001, (nuint)0x00000001, (nuint)0x0000003F));
                Assert.Equal((nuint)0x0000003F, NumberHelper<nuint>.Clamp((nuint)0x7FFFFFFF, (nuint)0x00000001, (nuint)0x0000003F));
                Assert.Equal((nuint)0x0000003F, NumberHelper<nuint>.Clamp((nuint)0x80000000, (nuint)0x00000001, (nuint)0x0000003F));
                Assert.Equal((nuint)0x0000003F, NumberHelper<nuint>.Clamp((nuint)0xFFFFFFFF, (nuint)0x00000001, (nuint)0x0000003F));
            }
        }

        [Fact]
        public static void MaxTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000000000000001), NumberHelper<nuint>.Max(unchecked((nuint)0x0000000000000000), (nuint)1));
                Assert.Equal(unchecked((nuint)0x0000000000000001), NumberHelper<nuint>.Max(unchecked((nuint)0x0000000000000001), (nuint)1));
                Assert.Equal(unchecked((nuint)0x7FFFFFFFFFFFFFFF), NumberHelper<nuint>.Max(unchecked((nuint)0x7FFFFFFFFFFFFFFF), (nuint)1));
                Assert.Equal(unchecked((nuint)0x8000000000000000), NumberHelper<nuint>.Max(unchecked((nuint)0x8000000000000000), (nuint)1));
                Assert.Equal(unchecked((nuint)0xFFFFFFFFFFFFFFFF), NumberHelper<nuint>.Max(unchecked((nuint)0xFFFFFFFFFFFFFFFF), (nuint)1));
            }
            else
            {
                Assert.Equal((nuint)0x00000001, NumberHelper<nuint>.Max((nuint)0x00000000, (nuint)1));
                Assert.Equal((nuint)0x00000001, NumberHelper<nuint>.Max((nuint)0x00000001, (nuint)1));
                Assert.Equal((nuint)0x7FFFFFFF, NumberHelper<nuint>.Max((nuint)0x7FFFFFFF, (nuint)1));
                Assert.Equal((nuint)0x80000000, NumberHelper<nuint>.Max((nuint)0x80000000, (nuint)1));
                Assert.Equal((nuint)0xFFFFFFFF, NumberHelper<nuint>.Max((nuint)0xFFFFFFFF, (nuint)1));
            }
        }

        [Fact]
        public static void MaxNumberTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000000000000001), NumberHelper<nuint>.MaxNumber(unchecked((nuint)0x0000000000000000), (nuint)1));
                Assert.Equal(unchecked((nuint)0x0000000000000001), NumberHelper<nuint>.MaxNumber(unchecked((nuint)0x0000000000000001), (nuint)1));
                Assert.Equal(unchecked((nuint)0x7FFFFFFFFFFFFFFF), NumberHelper<nuint>.MaxNumber(unchecked((nuint)0x7FFFFFFFFFFFFFFF), (nuint)1));
                Assert.Equal(unchecked((nuint)0x8000000000000000), NumberHelper<nuint>.MaxNumber(unchecked((nuint)0x8000000000000000), (nuint)1));
                Assert.Equal(unchecked((nuint)0xFFFFFFFFFFFFFFFF), NumberHelper<nuint>.MaxNumber(unchecked((nuint)0xFFFFFFFFFFFFFFFF), (nuint)1));
            }
            else
            {
                Assert.Equal((nuint)0x00000001, NumberHelper<nuint>.MaxNumber((nuint)0x00000000, (nuint)1));
                Assert.Equal((nuint)0x00000001, NumberHelper<nuint>.MaxNumber((nuint)0x00000001, (nuint)1));
                Assert.Equal((nuint)0x7FFFFFFF, NumberHelper<nuint>.MaxNumber((nuint)0x7FFFFFFF, (nuint)1));
                Assert.Equal((nuint)0x80000000, NumberHelper<nuint>.MaxNumber((nuint)0x80000000, (nuint)1));
                Assert.Equal((nuint)0xFFFFFFFF, NumberHelper<nuint>.MaxNumber((nuint)0xFFFFFFFF, (nuint)1));
            }
        }

        [Fact]
        public static void MinTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000000000000000), NumberHelper<nuint>.Min(unchecked((nuint)0x0000000000000000), (nuint)1));
                Assert.Equal(unchecked((nuint)0x0000000000000001), NumberHelper<nuint>.Min(unchecked((nuint)0x0000000000000001), (nuint)1));
                Assert.Equal(unchecked((nuint)0x0000000000000001), NumberHelper<nuint>.Min(unchecked((nuint)0x7FFFFFFFFFFFFFFF), (nuint)1));
                Assert.Equal(unchecked((nuint)0x0000000000000001), NumberHelper<nuint>.Min(unchecked((nuint)0x8000000000000000), (nuint)1));
                Assert.Equal(unchecked((nuint)0x0000000000000001), NumberHelper<nuint>.Min(unchecked((nuint)0xFFFFFFFFFFFFFFFF), (nuint)1));
            }
            else
            {
                Assert.Equal((nuint)0x00000000, NumberHelper<nuint>.Min((nuint)0x00000000, (nuint)1));
                Assert.Equal((nuint)0x00000001, NumberHelper<nuint>.Min((nuint)0x00000001, (nuint)1));
                Assert.Equal((nuint)0x00000001, NumberHelper<nuint>.Min((nuint)0x7FFFFFFF, (nuint)1));
                Assert.Equal((nuint)0x00000001, NumberHelper<nuint>.Min((nuint)0x80000000, (nuint)1));
                Assert.Equal((nuint)0x00000001, NumberHelper<nuint>.Min((nuint)0xFFFFFFFF, (nuint)1));
            }
        }

        [Fact]
        public static void MinNumberTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000000000000000), NumberHelper<nuint>.MinNumber(unchecked((nuint)0x0000000000000000), (nuint)1));
                Assert.Equal(unchecked((nuint)0x0000000000000001), NumberHelper<nuint>.MinNumber(unchecked((nuint)0x0000000000000001), (nuint)1));
                Assert.Equal(unchecked((nuint)0x0000000000000001), NumberHelper<nuint>.MinNumber(unchecked((nuint)0x7FFFFFFFFFFFFFFF), (nuint)1));
                Assert.Equal(unchecked((nuint)0x0000000000000001), NumberHelper<nuint>.MinNumber(unchecked((nuint)0x8000000000000000), (nuint)1));
                Assert.Equal(unchecked((nuint)0x0000000000000001), NumberHelper<nuint>.MinNumber(unchecked((nuint)0xFFFFFFFFFFFFFFFF), (nuint)1));
            }
            else
            {
                Assert.Equal((nuint)0x00000000, NumberHelper<nuint>.MinNumber((nuint)0x00000000, (nuint)1));
                Assert.Equal((nuint)0x00000001, NumberHelper<nuint>.MinNumber((nuint)0x00000001, (nuint)1));
                Assert.Equal((nuint)0x00000001, NumberHelper<nuint>.MinNumber((nuint)0x7FFFFFFF, (nuint)1));
                Assert.Equal((nuint)0x00000001, NumberHelper<nuint>.MinNumber((nuint)0x80000000, (nuint)1));
                Assert.Equal((nuint)0x00000001, NumberHelper<nuint>.MinNumber((nuint)0xFFFFFFFF, (nuint)1));
            }
        }

        [Fact]
        public static void SignTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(0, NumberHelper<nuint>.Sign(unchecked((nuint)0x0000000000000000)));
                Assert.Equal(1, NumberHelper<nuint>.Sign(unchecked((nuint)0x0000000000000001)));
                Assert.Equal(1, NumberHelper<nuint>.Sign(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(1, NumberHelper<nuint>.Sign(unchecked((nuint)0x8000000000000000)));
                Assert.Equal(1, NumberHelper<nuint>.Sign(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal(0, NumberHelper<nuint>.Sign((nuint)0x00000000));
                Assert.Equal(1, NumberHelper<nuint>.Sign((nuint)0x00000001));
                Assert.Equal(1, NumberHelper<nuint>.Sign((nuint)0x7FFFFFFF));
                Assert.Equal(1, NumberHelper<nuint>.Sign((nuint)0x80000000));
                Assert.Equal(1, NumberHelper<nuint>.Sign((nuint)0xFFFFFFFF));
            }
        }

        //
        // INumberBase
        //

        [Fact]
        public static void OneTest()
        {
            Assert.Equal((nuint)0x00000001, NumberBaseHelper<nuint>.One);
        }

        [Fact]
        public static void RadixTest()
        {
            Assert.Equal(2, NumberBaseHelper<nuint>.Radix);
        }

        [Fact]
        public static void ZeroTest()
        {
            Assert.Equal((nuint)0x00000000, NumberBaseHelper<nuint>.Zero);
        }

        [Fact]
        public static void AbsTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000000000000000), NumberBaseHelper<nuint>.Abs(unchecked((nuint)0x0000000000000000)));
                Assert.Equal(unchecked((nuint)0x0000000000000001), NumberBaseHelper<nuint>.Abs(unchecked((nuint)0x0000000000000001)));
                Assert.Equal(unchecked((nuint)0x7FFFFFFFFFFFFFFF), NumberBaseHelper<nuint>.Abs(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((nuint)0x8000000000000000), NumberBaseHelper<nuint>.Abs(unchecked((nuint)0x8000000000000000)));
                Assert.Equal(unchecked((nuint)0xFFFFFFFFFFFFFFFF), NumberBaseHelper<nuint>.Abs(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((nuint)0x00000000, NumberBaseHelper<nuint>.Abs((nuint)0x00000000));
                Assert.Equal((nuint)0x00000001, NumberBaseHelper<nuint>.Abs((nuint)0x00000001));
                Assert.Equal((nuint)0x7FFFFFFF, NumberBaseHelper<nuint>.Abs((nuint)0x7FFFFFFF));
                Assert.Equal((nuint)0x80000000, NumberBaseHelper<nuint>.Abs((nuint)0x80000000));
                Assert.Equal((nuint)0xFFFFFFFF, NumberBaseHelper<nuint>.Abs((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateCheckedFromByteTest()
        {
            Assert.Equal((nuint)0x00000000, NumberBaseHelper<nuint>.CreateChecked<byte>(0x00));
            Assert.Equal((nuint)0x00000001, NumberBaseHelper<nuint>.CreateChecked<byte>(0x01));
            Assert.Equal((nuint)0x0000007F, NumberBaseHelper<nuint>.CreateChecked<byte>(0x7F));
            Assert.Equal((nuint)0x00000080, NumberBaseHelper<nuint>.CreateChecked<byte>(0x80));
            Assert.Equal((nuint)0x000000FF, NumberBaseHelper<nuint>.CreateChecked<byte>(0xFF));
        }

        [Fact]
        public static void CreateCheckedFromCharTest()
        {
            Assert.Equal((nuint)0x00000000, NumberBaseHelper<nuint>.CreateChecked<char>((char)0x0000));
            Assert.Equal((nuint)0x00000001, NumberBaseHelper<nuint>.CreateChecked<char>((char)0x0001));
            Assert.Equal((nuint)0x00007FFF, NumberBaseHelper<nuint>.CreateChecked<char>((char)0x7FFF));
            Assert.Equal((nuint)0x00008000, NumberBaseHelper<nuint>.CreateChecked<char>((char)0x8000));
            Assert.Equal((nuint)0x0000FFFF, NumberBaseHelper<nuint>.CreateChecked<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateCheckedFromDecimalTest()
        {
            Assert.Equal((nuint)0x0000_0000_0000_0000, NumberBaseHelper<nuint>.CreateChecked<decimal>(-0.0m));
            Assert.Equal((nuint)0x0000_0000_0000_0000, NumberBaseHelper<nuint>.CreateChecked<decimal>(+0.0m));
            Assert.Equal((nuint)0x0000_0000_0000_0001, NumberBaseHelper<nuint>.CreateChecked<decimal>(+1.0m));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<nuint>.CreateChecked<decimal>(decimal.MinValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<nuint>.CreateChecked<decimal>(decimal.MaxValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<nuint>.CreateChecked<decimal>(decimal.MinusOne));
        }

        [Fact]
        [SkipOnMono("https://github.com/dotnet/runtime/issues/69794")]
        public static void CreateCheckedFromDoubleTest()
        {
            Assert.Equal((nuint)0x0000_0000, NumberBaseHelper<nuint>.CreateChecked<double>(+0.0));
            Assert.Equal((nuint)0x0000_0000, NumberBaseHelper<nuint>.CreateChecked<double>(-0.0));


            Assert.Equal((nuint)0x0000_0000, NumberBaseHelper<nuint>.CreateChecked<double>(-double.Epsilon));
            Assert.Equal((nuint)0x0000_0000, NumberBaseHelper<nuint>.CreateChecked<double>(+double.Epsilon));

            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000_0000_0000_0001), NumberBaseHelper<nuint>.CreateChecked<double>(+1.0));
                Assert.Equal(unchecked((nuint)0xFFFF_FFFF_FFFF_F800), NumberBaseHelper<nuint>.CreateChecked<double>(+18446744073709549568.0));

                Assert.Throws<OverflowException>(() => NumberBaseHelper<nuint>.CreateChecked<double>(-1.0));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<nuint>.CreateChecked<double>(+18446744073709551616.0));
            }
            else
            {
                Assert.Equal((nuint)0x0000_0001, NumberBaseHelper<nuint>.CreateChecked<double>(+1.0));
                Assert.Equal((nuint)0xFFFF_FFFF, NumberBaseHelper<nuint>.CreateChecked<double>(+4294967295.0));

                Assert.Throws<OverflowException>(() => NumberBaseHelper<nuint>.CreateChecked<double>(-1.0));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<nuint>.CreateChecked<double>(+4294967296.0));
            }

            Assert.Throws<OverflowException>(() => NumberBaseHelper<nuint>.CreateChecked<double>(double.PositiveInfinity));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<nuint>.CreateChecked<double>(double.NegativeInfinity));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<nuint>.CreateChecked<double>(double.MaxValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<nuint>.CreateChecked<double>(double.MinValue));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<nuint>.CreateChecked<double>(double.NaN));
        }

        [Fact]
        public static void CreateCheckedFromHalfTest()
        {
            Assert.Equal((nuint)0x0000_0000, NumberBaseHelper<nuint>.CreateChecked<Half>(Half.Zero));
            Assert.Equal((nuint)0x0000_0000, NumberBaseHelper<nuint>.CreateChecked<Half>(Half.NegativeZero));

            Assert.Equal((nuint)0x0000_0000, NumberBaseHelper<nuint>.CreateChecked<Half>(-Half.Epsilon));
            Assert.Equal((nuint)0x0000_0000, NumberBaseHelper<nuint>.CreateChecked<Half>(+Half.Epsilon));

            Assert.Equal((nuint)0x0000_0001, NumberBaseHelper<nuint>.CreateChecked<Half>(Half.One));
            Assert.Equal((nuint)0x0000_FFE0, NumberBaseHelper<nuint>.CreateChecked<Half>(Half.MaxValue));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<nuint>.CreateChecked<Half>(Half.NegativeOne));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<nuint>.CreateChecked<Half>(Half.PositiveInfinity));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<nuint>.CreateChecked<Half>(Half.NegativeInfinity));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<nuint>.CreateChecked<Half>(Half.MinValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<nuint>.CreateChecked<Half>(Half.NaN));
        }

        [Fact]
        public static void CreateCheckedFromInt16Test()
        {
            Assert.Equal((nuint)0x00000000, NumberBaseHelper<nuint>.CreateChecked<short>(0x0000));
            Assert.Equal((nuint)0x00000001, NumberBaseHelper<nuint>.CreateChecked<short>(0x0001));
            Assert.Equal((nuint)0x00007FFF, NumberBaseHelper<nuint>.CreateChecked<short>(0x7FFF));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<nuint>.CreateChecked<short>(unchecked((short)0x8000)));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<nuint>.CreateChecked<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromInt32Test()
        {
            Assert.Equal((nuint)0x00000000, NumberBaseHelper<nuint>.CreateChecked<int>(0x00000000));
            Assert.Equal((nuint)0x00000001, NumberBaseHelper<nuint>.CreateChecked<int>(0x00000001));
            Assert.Equal((nuint)0x7FFFFFFF, NumberBaseHelper<nuint>.CreateChecked<int>(0x7FFFFFFF));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<nuint>.CreateChecked<int>(unchecked((int)0x80000000)));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<nuint>.CreateChecked<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromInt64Test()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000000000000000), NumberBaseHelper<nuint>.CreateChecked<long>(0x0000000000000000));
                Assert.Equal(unchecked((nuint)0x0000000000000001), NumberBaseHelper<nuint>.CreateChecked<long>(0x0000000000000001));
                Assert.Equal(unchecked((nuint)0x7FFFFFFFFFFFFFFF), NumberBaseHelper<nuint>.CreateChecked<long>(0x7FFFFFFFFFFFFFFF));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<nuint>.CreateChecked<long>(unchecked((long)0x8000000000000000)));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<nuint>.CreateChecked<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((nuint)0x00000000, NumberBaseHelper<nuint>.CreateChecked<long>(0x0000000000000000));
                Assert.Equal((nuint)0x00000001, NumberBaseHelper<nuint>.CreateChecked<long>(0x0000000000000001));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<nuint>.CreateChecked<long>(0x7FFFFFFFFFFFFFFF));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<nuint>.CreateChecked<long>(unchecked((long)0x8000000000000000)));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<nuint>.CreateChecked<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateCheckedFromInt128Test()
        {
            Assert.Equal((nuint)0x00000000, NumberBaseHelper<nuint>.CreateChecked<Int128>(Int128.Zero));
            Assert.Equal((nuint)0x00000001, NumberBaseHelper<nuint>.CreateChecked<Int128>(Int128.One));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<nuint>.CreateChecked<Int128>(Int128.MaxValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<nuint>.CreateChecked<Int128>(Int128.MinValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<nuint>.CreateChecked<Int128>(Int128.NegativeOne));
        }

        [Fact]
        public static void CreateCheckedFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000000000000000), NumberBaseHelper<nuint>.CreateChecked<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal(unchecked((nuint)0x0000000000000001), NumberBaseHelper<nuint>.CreateChecked<nint>(unchecked((nint)0x0000000000000001)));
                Assert.Equal(unchecked((nuint)0x7FFFFFFFFFFFFFFF), NumberBaseHelper<nuint>.CreateChecked<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<nuint>.CreateChecked<nint>(unchecked((nint)0x8000000000000000)));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<nuint>.CreateChecked<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((nuint)0x00000000, NumberBaseHelper<nuint>.CreateChecked<nint>((nint)0x00000000));
                Assert.Equal((nuint)0x00000001, NumberBaseHelper<nuint>.CreateChecked<nint>((nint)0x00000001));
                Assert.Equal((nuint)0x7FFFFFFF, NumberBaseHelper<nuint>.CreateChecked<nint>((nint)0x7FFFFFFF));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<nuint>.CreateChecked<nint>(unchecked((nint)0x80000000)));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<nuint>.CreateChecked<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        [SkipOnMono("https://github.com/dotnet/runtime/issues/69794")]
        public static void CreateCheckedFromNFloatTest()
        {
            Assert.Equal((nuint)0x0000_0000, NumberBaseHelper<nuint>.CreateChecked<NFloat>(0.0f));
            Assert.Equal((nuint)0x0000_0000, NumberBaseHelper<nuint>.CreateChecked<NFloat>(NFloat.NegativeZero));

            Assert.Equal((nuint)0x0000_0000, NumberBaseHelper<nuint>.CreateChecked<NFloat>(-NFloat.Epsilon));
            Assert.Equal((nuint)0x0000_0000, NumberBaseHelper<nuint>.CreateChecked<NFloat>(+NFloat.Epsilon));

            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000_0000_0000_0001), NumberBaseHelper<nuint>.CreateChecked<NFloat>(1.0f));
                Assert.Equal(unchecked((nuint)0xFFFF_FFFF_FFFF_F800), NumberBaseHelper<nuint>.CreateChecked<NFloat>((NFloat)18446744073709549568.0));

                Assert.Throws<OverflowException>(() => NumberBaseHelper<nuint>.CreateChecked<NFloat>(-1.0f));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<nuint>.CreateChecked<NFloat>(+18446744073709551616.0f));
            }
            else
            {
                Assert.Equal((nuint)0x0000_0001, NumberBaseHelper<nuint>.CreateChecked<NFloat>(1.0f));
                Assert.Equal((nuint)0xFFFF_FF00, NumberBaseHelper<nuint>.CreateChecked<NFloat>(4294967040.0f));

                Assert.Throws<OverflowException>(() => NumberBaseHelper<nuint>.CreateChecked<NFloat>(-1.0f));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<nuint>.CreateChecked<NFloat>(+4294967296.0f));
            }

            Assert.Throws<OverflowException>(() => NumberBaseHelper<nuint>.CreateChecked<NFloat>(NFloat.PositiveInfinity));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<nuint>.CreateChecked<NFloat>(NFloat.NegativeInfinity));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<nuint>.CreateChecked<NFloat>(NFloat.MaxValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<nuint>.CreateChecked<NFloat>(NFloat.MinValue));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<nuint>.CreateChecked<NFloat>(NFloat.NaN));
        }

        [Fact]
        public static void CreateCheckedFromSByteTest()
        {
            Assert.Equal((nuint)0x00000000, NumberBaseHelper<nuint>.CreateChecked<sbyte>(0x00));
            Assert.Equal((nuint)0x00000001, NumberBaseHelper<nuint>.CreateChecked<sbyte>(0x01));
            Assert.Equal((nuint)0x0000007F, NumberBaseHelper<nuint>.CreateChecked<sbyte>(0x7F));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<nuint>.CreateChecked<sbyte>(unchecked((sbyte)0x80)));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<nuint>.CreateChecked<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        [SkipOnMono("https://github.com/dotnet/runtime/issues/69794")]
        public static void CreateCheckedFromSingleTest()
        {
            Assert.Equal((nuint)0x0000_0000, NumberBaseHelper<nuint>.CreateChecked<float>(+0.0f));
            Assert.Equal((nuint)0x0000_0000, NumberBaseHelper<nuint>.CreateChecked<float>(-0.0f));

            Assert.Equal((nuint)0x0000_0000, NumberBaseHelper<nuint>.CreateChecked<float>(-float.Epsilon));
            Assert.Equal((nuint)0x0000_0000, NumberBaseHelper<nuint>.CreateChecked<float>(-float.Epsilon));

            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000_0000_0000_0001), NumberBaseHelper<nuint>.CreateChecked<float>(+1.0f));
                Assert.Equal(unchecked((nuint)0xFFFF_FF00_0000_0000), NumberBaseHelper<nuint>.CreateChecked<float>(+18446742974197923840.0f));

                Assert.Throws<OverflowException>(() => NumberBaseHelper<nuint>.CreateChecked<float>(-1.0f));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<nuint>.CreateChecked<float>(+18446744073709551616.0f));
            }
            else
            {
                Assert.Equal((nuint)0x0000_0001, NumberBaseHelper<nuint>.CreateChecked<float>(+1.0f));
                Assert.Equal((nuint)0xFFFF_FF00, NumberBaseHelper<nuint>.CreateChecked<float>(+4294967040.0f));

                Assert.Throws<OverflowException>(() => NumberBaseHelper<nuint>.CreateChecked<float>(-1.0f));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<nuint>.CreateChecked<float>(+4294967296.0f));
            }

            Assert.Throws<OverflowException>(() => NumberBaseHelper<nuint>.CreateChecked<float>(float.PositiveInfinity));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<nuint>.CreateChecked<float>(float.NegativeInfinity));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<nuint>.CreateChecked<float>(float.MaxValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<nuint>.CreateChecked<float>(float.MinValue));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<nuint>.CreateChecked<float>(float.NaN));
        }

        [Fact]
        public static void CreateCheckedFromUInt16Test()
        {
            Assert.Equal((nuint)0x00000000, NumberBaseHelper<nuint>.CreateChecked<ushort>(0x0000));
            Assert.Equal((nuint)0x00000001, NumberBaseHelper<nuint>.CreateChecked<ushort>(0x0001));
            Assert.Equal((nuint)0x00007FFF, NumberBaseHelper<nuint>.CreateChecked<ushort>(0x7FFF));
            Assert.Equal((nuint)0x00008000, NumberBaseHelper<nuint>.CreateChecked<ushort>(0x8000));
            Assert.Equal((nuint)0x0000FFFF, NumberBaseHelper<nuint>.CreateChecked<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUInt32Test()
        {
            Assert.Equal((nuint)0x00000000, NumberBaseHelper<nuint>.CreateChecked<uint>(0x00000000));
            Assert.Equal((nuint)0x00000001, NumberBaseHelper<nuint>.CreateChecked<uint>(0x00000001));
            Assert.Equal((nuint)0x7FFFFFFF, NumberBaseHelper<nuint>.CreateChecked<uint>(0x7FFFFFFF));
            Assert.Equal((nuint)0x80000000, NumberBaseHelper<nuint>.CreateChecked<uint>(0x80000000));
            Assert.Equal((nuint)0xFFFFFFFF, NumberBaseHelper<nuint>.CreateChecked<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUInt64Test()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000000000000000), NumberBaseHelper<nuint>.CreateChecked<ulong>(0x0000000000000000));
                Assert.Equal(unchecked((nuint)0x0000000000000001), NumberBaseHelper<nuint>.CreateChecked<ulong>(0x0000000000000001));
                Assert.Equal(unchecked((nuint)0x7FFFFFFFFFFFFFFF), NumberBaseHelper<nuint>.CreateChecked<ulong>(0x7FFFFFFFFFFFFFFF));
                Assert.Equal(unchecked((nuint)0x8000000000000000), NumberBaseHelper<nuint>.CreateChecked<ulong>(0x8000000000000000));
                Assert.Equal(unchecked((nuint)0xFFFFFFFFFFFFFFFF), NumberBaseHelper<nuint>.CreateChecked<ulong>(0xFFFFFFFFFFFFFFFF));
            }
            else
            {
                Assert.Equal((nuint)0x00000000, NumberBaseHelper<nuint>.CreateChecked<ulong>(0x0000000000000000));
                Assert.Equal((nuint)0x00000001, NumberBaseHelper<nuint>.CreateChecked<ulong>(0x0000000000000001));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<nuint>.CreateChecked<ulong>(0x7FFFFFFFFFFFFFFF));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<nuint>.CreateChecked<ulong>(0x8000000000000000));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<nuint>.CreateChecked<ulong>(0xFFFFFFFFFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateCheckedFromUInt128Test()
        {
            Assert.Equal((nuint)0x00000000, NumberBaseHelper<nuint>.CreateChecked<UInt128>(UInt128.Zero));
            Assert.Equal((nuint)0x00000001, NumberBaseHelper<nuint>.CreateChecked<UInt128>(UInt128.One));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<nuint>.CreateChecked<UInt128>(UInt128Tests_GenericMath.Int128MaxValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<nuint>.CreateChecked<UInt128>(UInt128Tests_GenericMath.Int128MaxValuePlusOne));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<nuint>.CreateChecked<UInt128>(UInt128.MaxValue));
        }

        [Fact]
        public static void CreateCheckedFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000000000000000), NumberBaseHelper<nuint>.CreateChecked<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal(unchecked((nuint)0x0000000000000001), NumberBaseHelper<nuint>.CreateChecked<nuint>(unchecked((nuint)0x0000000000000001)));
                Assert.Equal(unchecked((nuint)0x7FFFFFFFFFFFFFFF), NumberBaseHelper<nuint>.CreateChecked<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((nuint)0x8000000000000000), NumberBaseHelper<nuint>.CreateChecked<nuint>(unchecked((nuint)0x8000000000000000)));
                Assert.Equal(unchecked((nuint)0xFFFFFFFFFFFFFFFF), NumberBaseHelper<nuint>.CreateChecked<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((nuint)0x00000000, NumberBaseHelper<nuint>.CreateChecked<nuint>((nuint)0x00000000));
                Assert.Equal((nuint)0x00000001, NumberBaseHelper<nuint>.CreateChecked<nuint>((nuint)0x00000001));
                Assert.Equal((nuint)0x7FFFFFFF, NumberBaseHelper<nuint>.CreateChecked<nuint>((nuint)0x7FFFFFFF));
                Assert.Equal((nuint)0x80000000, NumberBaseHelper<nuint>.CreateChecked<nuint>((nuint)0x80000000));
                Assert.Equal((nuint)0xFFFFFFFF, NumberBaseHelper<nuint>.CreateChecked<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateSaturatingFromByteTest()
        {
            Assert.Equal((nuint)0x00000000, NumberBaseHelper<nuint>.CreateSaturating<byte>(0x00));
            Assert.Equal((nuint)0x00000001, NumberBaseHelper<nuint>.CreateSaturating<byte>(0x01));
            Assert.Equal((nuint)0x0000007F, NumberBaseHelper<nuint>.CreateSaturating<byte>(0x7F));
            Assert.Equal((nuint)0x00000080, NumberBaseHelper<nuint>.CreateSaturating<byte>(0x80));
            Assert.Equal((nuint)0x000000FF, NumberBaseHelper<nuint>.CreateSaturating<byte>(0xFF));
        }

        [Fact]
        public static void CreateSaturatingFromCharTest()
        {
            Assert.Equal((nuint)0x00000000, NumberBaseHelper<nuint>.CreateSaturating<char>((char)0x0000));
            Assert.Equal((nuint)0x00000001, NumberBaseHelper<nuint>.CreateSaturating<char>((char)0x0001));
            Assert.Equal((nuint)0x00007FFF, NumberBaseHelper<nuint>.CreateSaturating<char>((char)0x7FFF));
            Assert.Equal((nuint)0x00008000, NumberBaseHelper<nuint>.CreateSaturating<char>((char)0x8000));
            Assert.Equal((nuint)0x0000FFFF, NumberBaseHelper<nuint>.CreateSaturating<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromDecimalTest()
        {
            Assert.Equal((nuint)0x0000_0000_0000_0000, NumberBaseHelper<nuint>.CreateSaturating<decimal>(-0.0m));
            Assert.Equal((nuint)0x0000_0000_0000_0000, NumberBaseHelper<nuint>.CreateSaturating<decimal>(+0.0m));
            Assert.Equal((nuint)0x0000_0000_0000_0001, NumberBaseHelper<nuint>.CreateSaturating<decimal>(+1.0m));

            Assert.Equal(nuint.MinValue, NumberBaseHelper<nuint>.CreateSaturating<decimal>(decimal.MinValue));
            Assert.Equal(nuint.MaxValue, NumberBaseHelper<nuint>.CreateSaturating<decimal>(decimal.MaxValue));
            Assert.Equal(nuint.MinValue, NumberBaseHelper<nuint>.CreateSaturating<decimal>(decimal.MinusOne));
        }

        [Fact]
        public static void CreateSaturatingFromDoubleTest()
        {
            Assert.Equal((nuint)0x0000_0000, NumberBaseHelper<nuint>.CreateSaturating<double>(+0.0));
            Assert.Equal((nuint)0x0000_0000, NumberBaseHelper<nuint>.CreateSaturating<double>(-0.0));

            Assert.Equal((nuint)0x0000_0000, NumberBaseHelper<nuint>.CreateSaturating<double>(-double.Epsilon));
            Assert.Equal((nuint)0x0000_0000, NumberBaseHelper<nuint>.CreateSaturating<double>(+double.Epsilon));

            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000_0000_0000_0001), NumberBaseHelper<nuint>.CreateSaturating<double>(+1.0));
                Assert.Equal(unchecked((nuint)0xFFFF_FFFF_FFFF_F800), NumberBaseHelper<nuint>.CreateSaturating<double>(+18446744073709549568.0));

                Assert.Equal(nuint.MinValue, NumberBaseHelper<nuint>.CreateSaturating<double>(-1.0));
                Assert.Equal(nuint.MaxValue, NumberBaseHelper<nuint>.CreateSaturating<double>(+18446744073709551616.0));
            }
            else
            {
                Assert.Equal((nuint)0x0000_0001, NumberBaseHelper<nuint>.CreateSaturating<double>(+1.0));
                Assert.Equal((nuint)0xFFFF_FFFF, NumberBaseHelper<nuint>.CreateSaturating<double>(+4294967295.0));

                Assert.Equal(nuint.MinValue, NumberBaseHelper<nuint>.CreateSaturating<double>(-1.0));
                Assert.Equal(nuint.MaxValue, NumberBaseHelper<nuint>.CreateSaturating<double>(+4294967296.0));
            }

            Assert.Equal(nuint.MaxValue, NumberBaseHelper<nuint>.CreateSaturating<double>(double.PositiveInfinity));
            Assert.Equal(nuint.MinValue, NumberBaseHelper<nuint>.CreateSaturating<double>(double.NegativeInfinity));

            Assert.Equal(nuint.MaxValue, NumberBaseHelper<nuint>.CreateSaturating<double>(double.MaxValue));
            Assert.Equal(nuint.MinValue, NumberBaseHelper<nuint>.CreateSaturating<double>(double.MinValue));

            Assert.Equal(nuint.MinValue, NumberBaseHelper<nuint>.CreateSaturating<double>(double.NaN));
        }

        [Fact]
        public static void CreateSaturatingFromHalfTest()
        {
            Assert.Equal((nuint)0x0000_0000, NumberBaseHelper<nuint>.CreateSaturating<Half>(Half.Zero));
            Assert.Equal((nuint)0x0000_0000, NumberBaseHelper<nuint>.CreateSaturating<Half>(Half.NegativeZero));

            Assert.Equal((nuint)0x0000_0000, NumberBaseHelper<nuint>.CreateSaturating<Half>(-Half.Epsilon));
            Assert.Equal((nuint)0x0000_0000, NumberBaseHelper<nuint>.CreateSaturating<Half>(+Half.Epsilon));

            Assert.Equal((nuint)0x0000_0001, NumberBaseHelper<nuint>.CreateSaturating<Half>(Half.One));
            Assert.Equal((nuint)0x0000_FFE0, NumberBaseHelper<nuint>.CreateSaturating<Half>(Half.MaxValue));

            Assert.Equal(nuint.MinValue, NumberBaseHelper<nuint>.CreateSaturating<Half>(Half.NegativeOne));

            Assert.Equal(nuint.MaxValue, NumberBaseHelper<nuint>.CreateSaturating<Half>(Half.PositiveInfinity));
            Assert.Equal(nuint.MinValue, NumberBaseHelper<nuint>.CreateSaturating<Half>(Half.NegativeInfinity));

            Assert.Equal(nuint.MinValue, NumberBaseHelper<nuint>.CreateSaturating<Half>(Half.MinValue));
            Assert.Equal(nuint.MinValue, NumberBaseHelper<nuint>.CreateSaturating<Half>(Half.NaN));
        }

        [Fact]
        public static void CreateSaturatingFromInt16Test()
        {
            Assert.Equal((nuint)0x00000000, NumberBaseHelper<nuint>.CreateSaturating<short>(0x0000));
            Assert.Equal((nuint)0x00000001, NumberBaseHelper<nuint>.CreateSaturating<short>(0x0001));
            Assert.Equal((nuint)0x00007FFF, NumberBaseHelper<nuint>.CreateSaturating<short>(0x7FFF));
            Assert.Equal((nuint)0x00000000, NumberBaseHelper<nuint>.CreateSaturating<short>(unchecked((short)0x8000)));
            Assert.Equal((nuint)0x00000000, NumberBaseHelper<nuint>.CreateSaturating<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromInt32Test()
        {
            Assert.Equal((nuint)0x00000000, NumberBaseHelper<nuint>.CreateSaturating<int>(0x00000000));
            Assert.Equal((nuint)0x00000001, NumberBaseHelper<nuint>.CreateSaturating<int>(0x00000001));
            Assert.Equal((nuint)0x7FFFFFFF, NumberBaseHelper<nuint>.CreateSaturating<int>(0x7FFFFFFF));
            Assert.Equal((nuint)0x00000000, NumberBaseHelper<nuint>.CreateSaturating<int>(unchecked((int)0x80000000)));
            Assert.Equal((nuint)0x00000000, NumberBaseHelper<nuint>.CreateSaturating<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromInt64Test()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000000000000000), NumberBaseHelper<nuint>.CreateSaturating<long>(0x0000000000000000));
                Assert.Equal(unchecked((nuint)0x0000000000000001), NumberBaseHelper<nuint>.CreateSaturating<long>(0x0000000000000001));
                Assert.Equal(unchecked((nuint)0x7FFFFFFFFFFFFFFF), NumberBaseHelper<nuint>.CreateSaturating<long>(0x7FFFFFFFFFFFFFFF));
                Assert.Equal(unchecked((nuint)0x0000000000000000), NumberBaseHelper<nuint>.CreateSaturating<long>(unchecked((long)0x8000000000000000)));
                Assert.Equal(unchecked((nuint)0x0000000000000000), NumberBaseHelper<nuint>.CreateSaturating<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((nuint)0x00000000, NumberBaseHelper<nuint>.CreateSaturating<long>(0x0000000000000000));
                Assert.Equal((nuint)0x00000001, NumberBaseHelper<nuint>.CreateSaturating<long>(0x0000000000000001));
                Assert.Equal((nuint)0xFFFFFFFF, NumberBaseHelper<nuint>.CreateSaturating<long>(0x7FFFFFFFFFFFFFFF));
                Assert.Equal((nuint)0x00000000, NumberBaseHelper<nuint>.CreateSaturating<long>(unchecked((long)0x8000000000000000)));
                Assert.Equal((nuint)0x00000000, NumberBaseHelper<nuint>.CreateSaturating<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateSaturatingFromInt128Test()
        {
            Assert.Equal((nuint)0x0000_0000, NumberBaseHelper<nuint>.CreateSaturating<Int128>(Int128.Zero));
            Assert.Equal((nuint)0x0000_0001, NumberBaseHelper<nuint>.CreateSaturating<Int128>(Int128.One));
            Assert.Equal(nuint.MaxValue, NumberBaseHelper<nuint>.CreateSaturating<Int128>(Int128.MaxValue));
            Assert.Equal(nuint.MinValue, NumberBaseHelper<nuint>.CreateSaturating<Int128>(Int128.MinValue));
            Assert.Equal(nuint.MinValue, NumberBaseHelper<nuint>.CreateSaturating<Int128>(Int128.NegativeOne));
        }

        [Fact]
        public static void CreateSaturatingFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000000000000000), NumberBaseHelper<nuint>.CreateSaturating<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal(unchecked((nuint)0x0000000000000001), NumberBaseHelper<nuint>.CreateSaturating<nint>(unchecked((nint)0x0000000000000001)));
                Assert.Equal(unchecked((nuint)0x7FFFFFFFFFFFFFFF), NumberBaseHelper<nuint>.CreateSaturating<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((nuint)0x0000000000000000), NumberBaseHelper<nuint>.CreateSaturating<nint>(unchecked((nint)0x8000000000000000)));
                Assert.Equal(unchecked((nuint)0x0000000000000000), NumberBaseHelper<nuint>.CreateSaturating<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((nuint)0x00000000, NumberBaseHelper<nuint>.CreateSaturating<nint>((nint)0x00000000));
                Assert.Equal((nuint)0x00000001, NumberBaseHelper<nuint>.CreateSaturating<nint>((nint)0x00000001));
                Assert.Equal((nuint)0x7FFFFFFF, NumberBaseHelper<nuint>.CreateSaturating<nint>((nint)0x7FFFFFFF));
                Assert.Equal((nuint)0x00000000, NumberBaseHelper<nuint>.CreateSaturating<nint>(unchecked((nint)0x80000000)));
                Assert.Equal((nuint)0x00000000, NumberBaseHelper<nuint>.CreateSaturating<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateSaturatingFromNFloatTest()
        {
            Assert.Equal((nuint)0x0000_0000, NumberBaseHelper<nuint>.CreateSaturating<NFloat>(0.0f));
            Assert.Equal((nuint)0x0000_0000, NumberBaseHelper<nuint>.CreateSaturating<NFloat>(NFloat.NegativeZero));

            Assert.Equal((nuint)0x0000_0000, NumberBaseHelper<nuint>.CreateSaturating<NFloat>(-NFloat.Epsilon));
            Assert.Equal((nuint)0x0000_0000, NumberBaseHelper<nuint>.CreateSaturating<NFloat>(+NFloat.Epsilon));

            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000_0000_0000_0001), NumberBaseHelper<nuint>.CreateSaturating<NFloat>(1.0f));
                Assert.Equal(unchecked((nuint)0xFFFF_FFFF_FFFF_F800), NumberBaseHelper<nuint>.CreateSaturating<NFloat>((NFloat)18446744073709549568.0));

                Assert.Equal(nuint.MinValue, NumberBaseHelper<nuint>.CreateSaturating<NFloat>(-1.0f));
                Assert.Equal(nuint.MaxValue, NumberBaseHelper<nuint>.CreateSaturating<NFloat>(+18446744073709551616.0f));
            }
            else
            {
                Assert.Equal((nuint)0x0000_0001, NumberBaseHelper<nuint>.CreateSaturating<NFloat>(1.0f));
                Assert.Equal((nuint)0xFFFF_FF00, NumberBaseHelper<nuint>.CreateSaturating<NFloat>(4294967040.0f));

                Assert.Equal(nuint.MinValue, NumberBaseHelper<nuint>.CreateSaturating<NFloat>(-1.0f));
                Assert.Equal(nuint.MaxValue, NumberBaseHelper<nuint>.CreateSaturating<NFloat>(+4294967296.0f));
            }

            Assert.Equal(nuint.MaxValue, NumberBaseHelper<nuint>.CreateSaturating<NFloat>(NFloat.PositiveInfinity));
            Assert.Equal(nuint.MinValue, NumberBaseHelper<nuint>.CreateSaturating<NFloat>(NFloat.NegativeInfinity));

            Assert.Equal(nuint.MaxValue, NumberBaseHelper<nuint>.CreateSaturating<NFloat>(NFloat.MaxValue));
            Assert.Equal(nuint.MinValue, NumberBaseHelper<nuint>.CreateSaturating<NFloat>(NFloat.MinValue));

            Assert.Equal(nuint.MinValue, NumberBaseHelper<nuint>.CreateSaturating<NFloat>(NFloat.NaN));
        }

        [Fact]
        public static void CreateSaturatingFromSByteTest()
        {
            Assert.Equal((nuint)0x00000000, NumberBaseHelper<nuint>.CreateSaturating<sbyte>(0x00));
            Assert.Equal((nuint)0x00000001, NumberBaseHelper<nuint>.CreateSaturating<sbyte>(0x01));
            Assert.Equal((nuint)0x0000007F, NumberBaseHelper<nuint>.CreateSaturating<sbyte>(0x7F));
            Assert.Equal((nuint)0x00000000, NumberBaseHelper<nuint>.CreateSaturating<sbyte>(unchecked((sbyte)0x80)));
            Assert.Equal((nuint)0x00000000, NumberBaseHelper<nuint>.CreateSaturating<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateSaturatingFromSingleTest()
        {
            Assert.Equal((nuint)0x0000_0000, NumberBaseHelper<nuint>.CreateSaturating<float>(+0.0f));
            Assert.Equal((nuint)0x0000_0000, NumberBaseHelper<nuint>.CreateSaturating<float>(-0.0f));

            Assert.Equal((nuint)0x0000_0000, NumberBaseHelper<nuint>.CreateSaturating<float>(-float.Epsilon));
            Assert.Equal((nuint)0x0000_0000, NumberBaseHelper<nuint>.CreateSaturating<float>(-float.Epsilon));

            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000_0000_0000_0001), NumberBaseHelper<nuint>.CreateSaturating<float>(+1.0f));
                Assert.Equal(unchecked((nuint)0xFFFF_FF00_0000_0000), NumberBaseHelper<nuint>.CreateSaturating<float>(+18446742974197923840.0f));

                Assert.Equal(nuint.MinValue, NumberBaseHelper<nuint>.CreateSaturating<float>(-1.0f));
                Assert.Equal(nuint.MaxValue, NumberBaseHelper<nuint>.CreateSaturating<float>(+18446744073709551616.0f));
            }
            else
            {
                Assert.Equal((nuint)0x0000_0001, NumberBaseHelper<nuint>.CreateSaturating<float>(+1.0f));
                Assert.Equal((nuint)0xFFFF_FF00, NumberBaseHelper<nuint>.CreateSaturating<float>(+4294967040.0f));

                Assert.Equal(nuint.MinValue, NumberBaseHelper<nuint>.CreateSaturating<float>(-1.0f));
                Assert.Equal(nuint.MaxValue, NumberBaseHelper<nuint>.CreateSaturating<float>(+4294967296.0f));
            }

            Assert.Equal(nuint.MaxValue, NumberBaseHelper<nuint>.CreateSaturating<float>(float.PositiveInfinity));
            Assert.Equal(nuint.MinValue, NumberBaseHelper<nuint>.CreateSaturating<float>(float.NegativeInfinity));

            Assert.Equal(nuint.MaxValue, NumberBaseHelper<nuint>.CreateSaturating<float>(float.MaxValue));
            Assert.Equal(nuint.MinValue, NumberBaseHelper<nuint>.CreateSaturating<float>(float.MinValue));

            Assert.Equal(nuint.MinValue, NumberBaseHelper<nuint>.CreateSaturating<float>(float.NaN));
        }

        [Fact]
        public static void CreateSaturatingFromUInt16Test()
        {
            Assert.Equal((nuint)0x00000000, NumberBaseHelper<nuint>.CreateSaturating<ushort>(0x0000));
            Assert.Equal((nuint)0x00000001, NumberBaseHelper<nuint>.CreateSaturating<ushort>(0x0001));
            Assert.Equal((nuint)0x00007FFF, NumberBaseHelper<nuint>.CreateSaturating<ushort>(0x7FFF));
            Assert.Equal((nuint)0x00008000, NumberBaseHelper<nuint>.CreateSaturating<ushort>(0x8000));
            Assert.Equal((nuint)0x0000FFFF, NumberBaseHelper<nuint>.CreateSaturating<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUInt32Test()
        {
            Assert.Equal((nuint)0x00000000, NumberBaseHelper<nuint>.CreateSaturating<uint>(0x00000000));
            Assert.Equal((nuint)0x00000001, NumberBaseHelper<nuint>.CreateSaturating<uint>(0x00000001));
            Assert.Equal((nuint)0x7FFFFFFF, NumberBaseHelper<nuint>.CreateSaturating<uint>(0x7FFFFFFF));
            Assert.Equal((nuint)0x80000000, NumberBaseHelper<nuint>.CreateSaturating<uint>(0x80000000));
            Assert.Equal((nuint)0xFFFFFFFF, NumberBaseHelper<nuint>.CreateSaturating<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUInt64Test()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000000000000000), NumberBaseHelper<nuint>.CreateSaturating<ulong>(0x0000000000000000));
                Assert.Equal(unchecked((nuint)0x0000000000000001), NumberBaseHelper<nuint>.CreateSaturating<ulong>(0x0000000000000001));
                Assert.Equal(unchecked((nuint)0x7FFFFFFFFFFFFFFF), NumberBaseHelper<nuint>.CreateSaturating<ulong>(0x7FFFFFFFFFFFFFFF));
                Assert.Equal(unchecked((nuint)0x8000000000000000), NumberBaseHelper<nuint>.CreateSaturating<ulong>(0x8000000000000000));
                Assert.Equal(unchecked((nuint)0xFFFFFFFFFFFFFFFF), NumberBaseHelper<nuint>.CreateSaturating<ulong>(0xFFFFFFFFFFFFFFFF));
            }
            else
            {
                Assert.Equal((nuint)0x00000000, NumberBaseHelper<nuint>.CreateSaturating<ulong>(0x0000000000000000));
                Assert.Equal((nuint)0x00000001, NumberBaseHelper<nuint>.CreateSaturating<ulong>(0x0000000000000001));
                Assert.Equal((nuint)0xFFFFFFFF, NumberBaseHelper<nuint>.CreateSaturating<ulong>(0x7FFFFFFFFFFFFFFF));
                Assert.Equal((nuint)0xFFFFFFFF, NumberBaseHelper<nuint>.CreateSaturating<ulong>(0x8000000000000000));
                Assert.Equal((nuint)0xFFFFFFFF, NumberBaseHelper<nuint>.CreateSaturating<ulong>(0xFFFFFFFFFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateSaturatingFromUInt128Test()
        {
            Assert.Equal((nuint)0x00000000, NumberBaseHelper<nuint>.CreateSaturating<UInt128>(UInt128.Zero));
            Assert.Equal((nuint)0x00000001, NumberBaseHelper<nuint>.CreateSaturating<UInt128>(UInt128.One));
            Assert.Equal(nuint.MaxValue, NumberBaseHelper<nuint>.CreateSaturating<UInt128>(UInt128Tests_GenericMath.Int128MaxValue));
            Assert.Equal(nuint.MaxValue, NumberBaseHelper<nuint>.CreateSaturating<UInt128>(UInt128Tests_GenericMath.Int128MaxValuePlusOne));
            Assert.Equal(nuint.MaxValue, NumberBaseHelper<nuint>.CreateSaturating<UInt128>(UInt128.MaxValue));
        }

        [Fact]
        public static void CreateSaturatingFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000000000000000), NumberBaseHelper<nuint>.CreateSaturating<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal(unchecked((nuint)0x0000000000000001), NumberBaseHelper<nuint>.CreateSaturating<nuint>(unchecked((nuint)0x0000000000000001)));
                Assert.Equal(unchecked((nuint)0x7FFFFFFFFFFFFFFF), NumberBaseHelper<nuint>.CreateSaturating<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((nuint)0x8000000000000000), NumberBaseHelper<nuint>.CreateSaturating<nuint>(unchecked((nuint)0x8000000000000000)));
                Assert.Equal(unchecked((nuint)0xFFFFFFFFFFFFFFFF), NumberBaseHelper<nuint>.CreateSaturating<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((nuint)0x00000000, NumberBaseHelper<nuint>.CreateSaturating<nuint>((nuint)0x00000000));
                Assert.Equal((nuint)0x00000001, NumberBaseHelper<nuint>.CreateSaturating<nuint>((nuint)0x00000001));
                Assert.Equal((nuint)0x7FFFFFFF, NumberBaseHelper<nuint>.CreateSaturating<nuint>((nuint)0x7FFFFFFF));
                Assert.Equal((nuint)0x80000000, NumberBaseHelper<nuint>.CreateSaturating<nuint>((nuint)0x80000000));
                Assert.Equal((nuint)0xFFFFFFFF, NumberBaseHelper<nuint>.CreateSaturating<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateTruncatingFromByteTest()
        {
            Assert.Equal((nuint)0x00000000, NumberBaseHelper<nuint>.CreateTruncating<byte>(0x00));
            Assert.Equal((nuint)0x00000001, NumberBaseHelper<nuint>.CreateTruncating<byte>(0x01));
            Assert.Equal((nuint)0x0000007F, NumberBaseHelper<nuint>.CreateTruncating<byte>(0x7F));
            Assert.Equal((nuint)0x00000080, NumberBaseHelper<nuint>.CreateTruncating<byte>(0x80));
            Assert.Equal((nuint)0x000000FF, NumberBaseHelper<nuint>.CreateTruncating<byte>(0xFF));
        }

        [Fact]
        public static void CreateTruncatingFromCharTest()
        {
            Assert.Equal((nuint)0x00000000, NumberBaseHelper<nuint>.CreateTruncating<char>((char)0x0000));
            Assert.Equal((nuint)0x00000001, NumberBaseHelper<nuint>.CreateTruncating<char>((char)0x0001));
            Assert.Equal((nuint)0x00007FFF, NumberBaseHelper<nuint>.CreateTruncating<char>((char)0x7FFF));
            Assert.Equal((nuint)0x00008000, NumberBaseHelper<nuint>.CreateTruncating<char>((char)0x8000));
            Assert.Equal((nuint)0x0000FFFF, NumberBaseHelper<nuint>.CreateTruncating<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromDecimalTest()
        {
            Assert.Equal((nuint)0x0000_0000_0000_0000, NumberBaseHelper<nuint>.CreateTruncating<decimal>(-0.0m));
            Assert.Equal((nuint)0x0000_0000_0000_0000, NumberBaseHelper<nuint>.CreateTruncating<decimal>(+0.0m));
            Assert.Equal((nuint)0x0000_0000_0000_0001, NumberBaseHelper<nuint>.CreateTruncating<decimal>(+1.0m));

            Assert.Equal(nuint.MinValue, NumberBaseHelper<nuint>.CreateTruncating<decimal>(decimal.MinValue));
            Assert.Equal(nuint.MaxValue, NumberBaseHelper<nuint>.CreateTruncating<decimal>(decimal.MaxValue));
            Assert.Equal(nuint.MinValue, NumberBaseHelper<nuint>.CreateTruncating<decimal>(decimal.MinusOne));
        }

        [Fact]
        public static void CreateTruncatingFromDoubleTest()
        {
            Assert.Equal((nuint)0x0000_0000, NumberBaseHelper<nuint>.CreateTruncating<double>(+0.0));
            Assert.Equal((nuint)0x0000_0000, NumberBaseHelper<nuint>.CreateTruncating<double>(-0.0));

            Assert.Equal((nuint)0x0000_0000, NumberBaseHelper<nuint>.CreateTruncating<double>(-double.Epsilon));
            Assert.Equal((nuint)0x0000_0000, NumberBaseHelper<nuint>.CreateTruncating<double>(+double.Epsilon));

            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000_0000_0000_0001), NumberBaseHelper<nuint>.CreateTruncating<double>(+1.0));
                Assert.Equal(unchecked((nuint)0xFFFF_FFFF_FFFF_F800), NumberBaseHelper<nuint>.CreateTruncating<double>(+18446744073709549568.0));

                Assert.Equal(nuint.MinValue, NumberBaseHelper<nuint>.CreateTruncating<double>(-1.0));
                Assert.Equal(nuint.MaxValue, NumberBaseHelper<nuint>.CreateTruncating<double>(+18446744073709551616.0));
            }
            else
            {
                Assert.Equal((nuint)0x0000_0001, NumberBaseHelper<nuint>.CreateTruncating<double>(+1.0));
                Assert.Equal((nuint)0xFFFF_FFFF, NumberBaseHelper<nuint>.CreateTruncating<double>(+4294967295.0));

                Assert.Equal(nuint.MinValue, NumberBaseHelper<nuint>.CreateTruncating<double>(-1.0));
                Assert.Equal(nuint.MaxValue, NumberBaseHelper<nuint>.CreateTruncating<double>(+4294967296.0));
            }

            Assert.Equal(nuint.MaxValue, NumberBaseHelper<nuint>.CreateTruncating<double>(double.PositiveInfinity));
            Assert.Equal(nuint.MinValue, NumberBaseHelper<nuint>.CreateTruncating<double>(double.NegativeInfinity));

            Assert.Equal(nuint.MaxValue, NumberBaseHelper<nuint>.CreateTruncating<double>(double.MaxValue));
            Assert.Equal(nuint.MinValue, NumberBaseHelper<nuint>.CreateTruncating<double>(double.MinValue));

            Assert.Equal(nuint.MinValue, NumberBaseHelper<nuint>.CreateTruncating<double>(double.NaN));
        }

        [Fact]
        public static void CreateTruncatingFromHalfTest()
        {
            Assert.Equal((nuint)0x0000_0000, NumberBaseHelper<nuint>.CreateTruncating<Half>(Half.Zero));
            Assert.Equal((nuint)0x0000_0000, NumberBaseHelper<nuint>.CreateTruncating<Half>(Half.NegativeZero));

            Assert.Equal((nuint)0x0000_0000, NumberBaseHelper<nuint>.CreateTruncating<Half>(-Half.Epsilon));
            Assert.Equal((nuint)0x0000_0000, NumberBaseHelper<nuint>.CreateTruncating<Half>(+Half.Epsilon));

            Assert.Equal((nuint)0x0000_0001, NumberBaseHelper<nuint>.CreateTruncating<Half>(Half.One));
            Assert.Equal((nuint)0x0000_FFE0, NumberBaseHelper<nuint>.CreateTruncating<Half>(Half.MaxValue));

            Assert.Equal(nuint.MinValue, NumberBaseHelper<nuint>.CreateTruncating<Half>(Half.NegativeOne));

            Assert.Equal(nuint.MaxValue, NumberBaseHelper<nuint>.CreateTruncating<Half>(Half.PositiveInfinity));
            Assert.Equal(nuint.MinValue, NumberBaseHelper<nuint>.CreateTruncating<Half>(Half.NegativeInfinity));

            Assert.Equal(nuint.MinValue, NumberBaseHelper<nuint>.CreateTruncating<Half>(Half.MinValue));
            Assert.Equal(nuint.MinValue, NumberBaseHelper<nuint>.CreateTruncating<Half>(Half.NaN));
        }

        [Fact]
        public static void CreateTruncatingFromInt16Test()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000000000000000), NumberBaseHelper<nuint>.CreateTruncating<short>(0x0000));
                Assert.Equal(unchecked((nuint)0x0000000000000001), NumberBaseHelper<nuint>.CreateTruncating<short>(0x0001));
                Assert.Equal(unchecked((nuint)0x0000000000007FFF), NumberBaseHelper<nuint>.CreateTruncating<short>(0x7FFF));
                Assert.Equal(unchecked((nuint)0xFFFFFFFFFFFF8000), NumberBaseHelper<nuint>.CreateTruncating<short>(unchecked((short)0x8000)));
                Assert.Equal(unchecked((nuint)0xFFFFFFFFFFFFFFFF), NumberBaseHelper<nuint>.CreateTruncating<short>(unchecked((short)0xFFFF)));
            }
            else
            {
                Assert.Equal((nuint)0x00000000, NumberBaseHelper<nuint>.CreateTruncating<short>(0x0000));
                Assert.Equal((nuint)0x00000001, NumberBaseHelper<nuint>.CreateTruncating<short>(0x0001));
                Assert.Equal((nuint)0x00007FFF, NumberBaseHelper<nuint>.CreateTruncating<short>(0x7FFF));
                Assert.Equal((nuint)0xFFFF8000, NumberBaseHelper<nuint>.CreateTruncating<short>(unchecked((short)0x8000)));
                Assert.Equal((nuint)0xFFFFFFFF, NumberBaseHelper<nuint>.CreateTruncating<short>(unchecked((short)0xFFFF)));
            }
        }

        [Fact]
        public static void CreateTruncatingFromInt32Test()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000000000000000), NumberBaseHelper<nuint>.CreateTruncating<int>(0x00000000));
                Assert.Equal(unchecked((nuint)0x0000000000000001), NumberBaseHelper<nuint>.CreateTruncating<int>(0x00000001));
                Assert.Equal(unchecked((nuint)0x000000007FFFFFFF), NumberBaseHelper<nuint>.CreateTruncating<int>(0x7FFFFFFF));
                Assert.Equal(unchecked((nuint)0xFFFFFFFF80000000), NumberBaseHelper<nuint>.CreateTruncating<int>(unchecked((int)0x80000000)));
                Assert.Equal(unchecked((nuint)0xFFFFFFFFFFFFFFFF), NumberBaseHelper<nuint>.CreateTruncating<int>(unchecked((int)0xFFFFFFFF)));
            }
            else
            {
                Assert.Equal((nuint)0x00000000, NumberBaseHelper<nuint>.CreateTruncating<int>(0x00000000));
                Assert.Equal((nuint)0x00000001, NumberBaseHelper<nuint>.CreateTruncating<int>(0x00000001));
                Assert.Equal((nuint)0x7FFFFFFF, NumberBaseHelper<nuint>.CreateTruncating<int>(0x7FFFFFFF));
                Assert.Equal((nuint)0x80000000, NumberBaseHelper<nuint>.CreateTruncating<int>(unchecked((int)0x80000000)));
                Assert.Equal((nuint)0xFFFFFFFF, NumberBaseHelper<nuint>.CreateTruncating<int>(unchecked((int)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateTruncatingFromInt64Test()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000000000000000), NumberBaseHelper<nuint>.CreateTruncating<long>(0x0000000000000000));
                Assert.Equal(unchecked((nuint)0x0000000000000001), NumberBaseHelper<nuint>.CreateTruncating<long>(0x0000000000000001));
                Assert.Equal(unchecked((nuint)0x7FFFFFFFFFFFFFFF), NumberBaseHelper<nuint>.CreateTruncating<long>(0x7FFFFFFFFFFFFFFF));
                Assert.Equal(unchecked((nuint)0x8000000000000000), NumberBaseHelper<nuint>.CreateTruncating<long>(unchecked((long)0x8000000000000000)));
                Assert.Equal(unchecked((nuint)0xFFFFFFFFFFFFFFFF), NumberBaseHelper<nuint>.CreateTruncating<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((nuint)0x00000000, NumberBaseHelper<nuint>.CreateTruncating<long>(0x0000000000000000));
                Assert.Equal((nuint)0x00000001, NumberBaseHelper<nuint>.CreateTruncating<long>(0x0000000000000001));
                Assert.Equal((nuint)0xFFFFFFFF, NumberBaseHelper<nuint>.CreateTruncating<long>(0x7FFFFFFFFFFFFFFF));
                Assert.Equal((nuint)0x00000000, NumberBaseHelper<nuint>.CreateTruncating<long>(unchecked((long)0x8000000000000000)));
                Assert.Equal((nuint)0xFFFFFFFF, NumberBaseHelper<nuint>.CreateTruncating<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateTruncatingFromInt128Test()
        {
            Assert.Equal((nuint)0x0000_0000, NumberBaseHelper<nuint>.CreateTruncating<Int128>(Int128.Zero));
            Assert.Equal((nuint)0x0000_0001, NumberBaseHelper<nuint>.CreateTruncating<Int128>(Int128.One));
            Assert.Equal(nuint.MaxValue, NumberBaseHelper<nuint>.CreateTruncating<Int128>(Int128.MaxValue));
            Assert.Equal(nuint.MinValue, NumberBaseHelper<nuint>.CreateTruncating<Int128>(Int128.MinValue));
            Assert.Equal(nuint.MaxValue, NumberBaseHelper<nuint>.CreateTruncating<Int128>(Int128.NegativeOne));
        }

        [Fact]
        public static void CreateTruncatingFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000000000000000), NumberBaseHelper<nuint>.CreateTruncating<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal(unchecked((nuint)0x0000000000000001), NumberBaseHelper<nuint>.CreateTruncating<nint>(unchecked((nint)0x0000000000000001)));
                Assert.Equal(unchecked((nuint)0x7FFFFFFFFFFFFFFF), NumberBaseHelper<nuint>.CreateTruncating<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((nuint)0x8000000000000000), NumberBaseHelper<nuint>.CreateTruncating<nint>(unchecked((nint)0x8000000000000000)));
                Assert.Equal(unchecked((nuint)0xFFFFFFFFFFFFFFFF), NumberBaseHelper<nuint>.CreateTruncating<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((nuint)0x00000000, NumberBaseHelper<nuint>.CreateTruncating<nint>((nint)0x00000000));
                Assert.Equal((nuint)0x00000001, NumberBaseHelper<nuint>.CreateTruncating<nint>((nint)0x00000001));
                Assert.Equal((nuint)0x7FFFFFFF, NumberBaseHelper<nuint>.CreateTruncating<nint>((nint)0x7FFFFFFF));
                Assert.Equal((nuint)0x80000000, NumberBaseHelper<nuint>.CreateTruncating<nint>(unchecked((nint)0x80000000)));
                Assert.Equal((nuint)0xFFFFFFFF, NumberBaseHelper<nuint>.CreateTruncating<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateTruncatingFromNFloatTest()
        {
            Assert.Equal((nuint)0x0000_0000, NumberBaseHelper<nuint>.CreateTruncating<NFloat>(0.0f));
            Assert.Equal((nuint)0x0000_0000, NumberBaseHelper<nuint>.CreateTruncating<NFloat>(NFloat.NegativeZero));

            Assert.Equal((nuint)0x0000_0000, NumberBaseHelper<nuint>.CreateTruncating<NFloat>(-NFloat.Epsilon));
            Assert.Equal((nuint)0x0000_0000, NumberBaseHelper<nuint>.CreateTruncating<NFloat>(+NFloat.Epsilon));

            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000_0000_0000_0001), NumberBaseHelper<nuint>.CreateTruncating<NFloat>(1.0f));
                Assert.Equal(unchecked((nuint)0xFFFF_FFFF_FFFF_F800), NumberBaseHelper<nuint>.CreateTruncating<NFloat>((NFloat)18446744073709549568.0));

                Assert.Equal(nuint.MinValue, NumberBaseHelper<nuint>.CreateTruncating<NFloat>(-1.0f));
                Assert.Equal(nuint.MaxValue, NumberBaseHelper<nuint>.CreateTruncating<NFloat>(+18446744073709551616.0f));
            }
            else
            {
                Assert.Equal((nuint)0x0000_0001, NumberBaseHelper<nuint>.CreateTruncating<NFloat>(1.0f));
                Assert.Equal((nuint)0xFFFF_FF00, NumberBaseHelper<nuint>.CreateTruncating<NFloat>(4294967040.0f));

                Assert.Equal(nuint.MinValue, NumberBaseHelper<nuint>.CreateTruncating<NFloat>(-1.0f));
                Assert.Equal(nuint.MaxValue, NumberBaseHelper<nuint>.CreateTruncating<NFloat>(+4294967296.0f));
            }

            Assert.Equal(nuint.MaxValue, NumberBaseHelper<nuint>.CreateTruncating<NFloat>(NFloat.PositiveInfinity));
            Assert.Equal(nuint.MinValue, NumberBaseHelper<nuint>.CreateTruncating<NFloat>(NFloat.NegativeInfinity));

            Assert.Equal(nuint.MaxValue, NumberBaseHelper<nuint>.CreateTruncating<NFloat>(NFloat.MaxValue));
            Assert.Equal(nuint.MinValue, NumberBaseHelper<nuint>.CreateTruncating<NFloat>(NFloat.MinValue));

            Assert.Equal(nuint.MinValue, NumberBaseHelper<nuint>.CreateTruncating<NFloat>(NFloat.NaN));
        }

        [Fact]
        public static void CreateTruncatingFromSByteTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000000000000000), NumberBaseHelper<nuint>.CreateTruncating<sbyte>(0x00));
                Assert.Equal(unchecked((nuint)0x0000000000000001), NumberBaseHelper<nuint>.CreateTruncating<sbyte>(0x01));
                Assert.Equal(unchecked((nuint)0x000000000000007F), NumberBaseHelper<nuint>.CreateTruncating<sbyte>(0x7F));
                Assert.Equal(unchecked((nuint)0xFFFFFFFFFFFFFF80), NumberBaseHelper<nuint>.CreateTruncating<sbyte>(unchecked((sbyte)0x80)));
                Assert.Equal(unchecked((nuint)0xFFFFFFFFFFFFFFFF), NumberBaseHelper<nuint>.CreateTruncating<sbyte>(unchecked((sbyte)0xFF)));
            }
            else
            {
                Assert.Equal((nuint)0x00000000, NumberBaseHelper<nuint>.CreateTruncating<sbyte>(0x00));
                Assert.Equal((nuint)0x00000001, NumberBaseHelper<nuint>.CreateTruncating<sbyte>(0x01));
                Assert.Equal((nuint)0x0000007F, NumberBaseHelper<nuint>.CreateTruncating<sbyte>(0x7F));
                Assert.Equal((nuint)0xFFFFFF80, NumberBaseHelper<nuint>.CreateTruncating<sbyte>(unchecked((sbyte)0x80)));
                Assert.Equal((nuint)0xFFFFFFFF, NumberBaseHelper<nuint>.CreateTruncating<sbyte>(unchecked((sbyte)0xFF)));
            }
        }

        [Fact]
        public static void CreateTruncatingFromSingleTest()
        {
            Assert.Equal((nuint)0x0000_0000, NumberBaseHelper<nuint>.CreateTruncating<float>(+0.0f));
            Assert.Equal((nuint)0x0000_0000, NumberBaseHelper<nuint>.CreateTruncating<float>(-0.0f));

            Assert.Equal((nuint)0x0000_0000, NumberBaseHelper<nuint>.CreateTruncating<float>(-float.Epsilon));
            Assert.Equal((nuint)0x0000_0000, NumberBaseHelper<nuint>.CreateTruncating<float>(-float.Epsilon));

            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000_0000_0000_0001), NumberBaseHelper<nuint>.CreateTruncating<float>(+1.0f));
                Assert.Equal(unchecked((nuint)0xFFFF_FF00_0000_0000), NumberBaseHelper<nuint>.CreateTruncating<float>(+18446742974197923840.0f));

                Assert.Equal(nuint.MinValue, NumberBaseHelper<nuint>.CreateTruncating<float>(-1.0f));
                Assert.Equal(nuint.MaxValue, NumberBaseHelper<nuint>.CreateTruncating<float>(+18446744073709551616.0f));
            }
            else
            {
                Assert.Equal((nuint)0x0000_0001, NumberBaseHelper<nuint>.CreateTruncating<float>(+1.0f));
                Assert.Equal((nuint)0xFFFF_FF00, NumberBaseHelper<nuint>.CreateTruncating<float>(+4294967040.0f));

                Assert.Equal(nuint.MinValue, NumberBaseHelper<nuint>.CreateTruncating<float>(-1.0f));
                Assert.Equal(nuint.MaxValue, NumberBaseHelper<nuint>.CreateTruncating<float>(+4294967296.0f));
            }

            Assert.Equal(nuint.MaxValue, NumberBaseHelper<nuint>.CreateTruncating<float>(float.PositiveInfinity));
            Assert.Equal(nuint.MinValue, NumberBaseHelper<nuint>.CreateTruncating<float>(float.NegativeInfinity));

            Assert.Equal(nuint.MaxValue, NumberBaseHelper<nuint>.CreateTruncating<float>(float.MaxValue));
            Assert.Equal(nuint.MinValue, NumberBaseHelper<nuint>.CreateTruncating<float>(float.MinValue));

            Assert.Equal(nuint.MinValue, NumberBaseHelper<nuint>.CreateTruncating<float>(float.NaN));
        }

        [Fact]
        public static void CreateTruncatingFromUInt16Test()
        {
            Assert.Equal((nuint)0x00000000, NumberBaseHelper<nuint>.CreateTruncating<ushort>(0x0000));
            Assert.Equal((nuint)0x00000001, NumberBaseHelper<nuint>.CreateTruncating<ushort>(0x0001));
            Assert.Equal((nuint)0x00007FFF, NumberBaseHelper<nuint>.CreateTruncating<ushort>(0x7FFF));
            Assert.Equal((nuint)0x00008000, NumberBaseHelper<nuint>.CreateTruncating<ushort>(0x8000));
            Assert.Equal((nuint)0x0000FFFF, NumberBaseHelper<nuint>.CreateTruncating<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt32Test()
        {
            Assert.Equal((nuint)0x00000000, NumberBaseHelper<nuint>.CreateTruncating<uint>(0x00000000));
            Assert.Equal((nuint)0x00000001, NumberBaseHelper<nuint>.CreateTruncating<uint>(0x00000001));
            Assert.Equal((nuint)0x7FFFFFFF, NumberBaseHelper<nuint>.CreateTruncating<uint>(0x7FFFFFFF));
            Assert.Equal((nuint)0x80000000, NumberBaseHelper<nuint>.CreateTruncating<uint>(0x80000000));
            Assert.Equal((nuint)0xFFFFFFFF, NumberBaseHelper<nuint>.CreateTruncating<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt64Test()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000000000000000), NumberBaseHelper<nuint>.CreateTruncating<ulong>(0x0000000000000000));
                Assert.Equal(unchecked((nuint)0x0000000000000001), NumberBaseHelper<nuint>.CreateTruncating<ulong>(0x0000000000000001));
                Assert.Equal(unchecked((nuint)0x7FFFFFFFFFFFFFFF), NumberBaseHelper<nuint>.CreateTruncating<ulong>(0x7FFFFFFFFFFFFFFF));
                Assert.Equal(unchecked((nuint)0x8000000000000000), NumberBaseHelper<nuint>.CreateTruncating<ulong>(0x8000000000000000));
                Assert.Equal(unchecked((nuint)0xFFFFFFFFFFFFFFFF), NumberBaseHelper<nuint>.CreateTruncating<ulong>(0xFFFFFFFFFFFFFFFF));
            }
            else
            {
                Assert.Equal((nuint)0x00000000, NumberBaseHelper<nuint>.CreateTruncating<ulong>(0x0000000000000000));
                Assert.Equal((nuint)0x00000001, NumberBaseHelper<nuint>.CreateTruncating<ulong>(0x0000000000000001));
                Assert.Equal((nuint)0xFFFFFFFF, NumberBaseHelper<nuint>.CreateTruncating<ulong>(0x7FFFFFFFFFFFFFFF));
                Assert.Equal((nuint)0x00000000, NumberBaseHelper<nuint>.CreateTruncating<ulong>(0x8000000000000000));
                Assert.Equal((nuint)0xFFFFFFFF, NumberBaseHelper<nuint>.CreateTruncating<ulong>(0xFFFFFFFFFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateTruncatingFromUInt128Test()
        {
            Assert.Equal((nuint)0x00000000, NumberBaseHelper<nuint>.CreateTruncating<UInt128>(UInt128.Zero));
            Assert.Equal((nuint)0x00000001, NumberBaseHelper<nuint>.CreateTruncating<UInt128>(UInt128.One));
            Assert.Equal(nuint.MaxValue, NumberBaseHelper<nuint>.CreateTruncating<UInt128>(UInt128Tests_GenericMath.Int128MaxValue));
            Assert.Equal(nuint.MinValue, NumberBaseHelper<nuint>.CreateTruncating<UInt128>(UInt128Tests_GenericMath.Int128MaxValuePlusOne));
            Assert.Equal(nuint.MaxValue, NumberBaseHelper<nuint>.CreateTruncating<UInt128>(UInt128.MaxValue));
        }

        [Fact]
        public static void CreateTruncatingFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000000000000000), NumberBaseHelper<nuint>.CreateTruncating<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal(unchecked((nuint)0x0000000000000001), NumberBaseHelper<nuint>.CreateTruncating<nuint>(unchecked((nuint)0x0000000000000001)));
                Assert.Equal(unchecked((nuint)0x7FFFFFFFFFFFFFFF), NumberBaseHelper<nuint>.CreateTruncating<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((nuint)0x8000000000000000), NumberBaseHelper<nuint>.CreateTruncating<nuint>(unchecked((nuint)0x8000000000000000)));
                Assert.Equal(unchecked((nuint)0xFFFFFFFFFFFFFFFF), NumberBaseHelper<nuint>.CreateTruncating<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((nuint)0x00000000, NumberBaseHelper<nuint>.CreateTruncating<nuint>((nuint)0x00000000));
                Assert.Equal((nuint)0x00000001, NumberBaseHelper<nuint>.CreateTruncating<nuint>((nuint)0x00000001));
                Assert.Equal((nuint)0x7FFFFFFF, NumberBaseHelper<nuint>.CreateTruncating<nuint>((nuint)0x7FFFFFFF));
                Assert.Equal((nuint)0x80000000, NumberBaseHelper<nuint>.CreateTruncating<nuint>((nuint)0x80000000));
                Assert.Equal((nuint)0xFFFFFFFF, NumberBaseHelper<nuint>.CreateTruncating<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void IsCanonicalTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.True(NumberBaseHelper<nuint>.IsCanonical(unchecked((nuint)0x0000000000000000)));
                Assert.True(NumberBaseHelper<nuint>.IsCanonical(unchecked((nuint)0x0000000000000001)));
                Assert.True(NumberBaseHelper<nuint>.IsCanonical(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.True(NumberBaseHelper<nuint>.IsCanonical(unchecked((nuint)0x8000000000000000)));
                Assert.True(NumberBaseHelper<nuint>.IsCanonical(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.True(NumberBaseHelper<nuint>.IsCanonical((nuint)0x00000000));
                Assert.True(NumberBaseHelper<nuint>.IsCanonical((nuint)0x00000001));
                Assert.True(NumberBaseHelper<nuint>.IsCanonical((nuint)0x7FFFFFFF));
                Assert.True(NumberBaseHelper<nuint>.IsCanonical((nuint)0x80000000));
                Assert.True(NumberBaseHelper<nuint>.IsCanonical((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void IsComplexNumberTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.False(NumberBaseHelper<nuint>.IsComplexNumber(unchecked((nuint)0x0000000000000000)));
                Assert.False(NumberBaseHelper<nuint>.IsComplexNumber(unchecked((nuint)0x0000000000000001)));
                Assert.False(NumberBaseHelper<nuint>.IsComplexNumber(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.False(NumberBaseHelper<nuint>.IsComplexNumber(unchecked((nuint)0x8000000000000000)));
                Assert.False(NumberBaseHelper<nuint>.IsComplexNumber(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.False(NumberBaseHelper<nuint>.IsComplexNumber((nuint)0x00000000));
                Assert.False(NumberBaseHelper<nuint>.IsComplexNumber((nuint)0x00000001));
                Assert.False(NumberBaseHelper<nuint>.IsComplexNumber((nuint)0x7FFFFFFF));
                Assert.False(NumberBaseHelper<nuint>.IsComplexNumber((nuint)0x80000000));
                Assert.False(NumberBaseHelper<nuint>.IsComplexNumber((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void IsEvenIntegerTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.True(NumberBaseHelper<nuint>.IsEvenInteger(unchecked((nuint)0x0000000000000000)));
                Assert.False(NumberBaseHelper<nuint>.IsEvenInteger(unchecked((nuint)0x0000000000000001)));
                Assert.False(NumberBaseHelper<nuint>.IsEvenInteger(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.True(NumberBaseHelper<nuint>.IsEvenInteger(unchecked((nuint)0x8000000000000000)));
                Assert.False(NumberBaseHelper<nuint>.IsEvenInteger(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.True(NumberBaseHelper<nuint>.IsEvenInteger((nuint)0x00000000));
                Assert.False(NumberBaseHelper<nuint>.IsEvenInteger((nuint)0x00000001));
                Assert.False(NumberBaseHelper<nuint>.IsEvenInteger((nuint)0x7FFFFFFF));
                Assert.True(NumberBaseHelper<nuint>.IsEvenInteger((nuint)0x80000000));
                Assert.False(NumberBaseHelper<nuint>.IsEvenInteger((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void IsFiniteTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.True(NumberBaseHelper<nuint>.IsFinite(unchecked((nuint)0x0000000000000000)));
                Assert.True(NumberBaseHelper<nuint>.IsFinite(unchecked((nuint)0x0000000000000001)));
                Assert.True(NumberBaseHelper<nuint>.IsFinite(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.True(NumberBaseHelper<nuint>.IsFinite(unchecked((nuint)0x8000000000000000)));
                Assert.True(NumberBaseHelper<nuint>.IsFinite(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.True(NumberBaseHelper<nuint>.IsFinite((nuint)0x00000000));
                Assert.True(NumberBaseHelper<nuint>.IsFinite((nuint)0x00000001));
                Assert.True(NumberBaseHelper<nuint>.IsFinite((nuint)0x7FFFFFFF));
                Assert.True(NumberBaseHelper<nuint>.IsFinite((nuint)0x80000000));
                Assert.True(NumberBaseHelper<nuint>.IsFinite((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void IsImaginaryNumberTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.False(NumberBaseHelper<nuint>.IsImaginaryNumber(unchecked((nuint)0x0000000000000000)));
                Assert.False(NumberBaseHelper<nuint>.IsImaginaryNumber(unchecked((nuint)0x0000000000000001)));
                Assert.False(NumberBaseHelper<nuint>.IsImaginaryNumber(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.False(NumberBaseHelper<nuint>.IsImaginaryNumber(unchecked((nuint)0x8000000000000000)));
                Assert.False(NumberBaseHelper<nuint>.IsImaginaryNumber(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.False(NumberBaseHelper<nuint>.IsImaginaryNumber((nuint)0x00000000));
                Assert.False(NumberBaseHelper<nuint>.IsImaginaryNumber((nuint)0x00000001));
                Assert.False(NumberBaseHelper<nuint>.IsImaginaryNumber((nuint)0x7FFFFFFF));
                Assert.False(NumberBaseHelper<nuint>.IsImaginaryNumber((nuint)0x80000000));
                Assert.False(NumberBaseHelper<nuint>.IsImaginaryNumber((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void IsInfinityTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.False(NumberBaseHelper<nuint>.IsInfinity(unchecked((nuint)0x0000000000000000)));
                Assert.False(NumberBaseHelper<nuint>.IsInfinity(unchecked((nuint)0x0000000000000001)));
                Assert.False(NumberBaseHelper<nuint>.IsInfinity(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.False(NumberBaseHelper<nuint>.IsInfinity(unchecked((nuint)0x8000000000000000)));
                Assert.False(NumberBaseHelper<nuint>.IsInfinity(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.False(NumberBaseHelper<nuint>.IsInfinity((nuint)0x00000000));
                Assert.False(NumberBaseHelper<nuint>.IsInfinity((nuint)0x00000001));
                Assert.False(NumberBaseHelper<nuint>.IsInfinity((nuint)0x7FFFFFFF));
                Assert.False(NumberBaseHelper<nuint>.IsInfinity((nuint)0x80000000));
                Assert.False(NumberBaseHelper<nuint>.IsInfinity((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void IsIntegerTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.True(NumberBaseHelper<nuint>.IsInteger(unchecked((nuint)0x0000000000000000)));
                Assert.True(NumberBaseHelper<nuint>.IsInteger(unchecked((nuint)0x0000000000000001)));
                Assert.True(NumberBaseHelper<nuint>.IsInteger(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.True(NumberBaseHelper<nuint>.IsInteger(unchecked((nuint)0x8000000000000000)));
                Assert.True(NumberBaseHelper<nuint>.IsInteger(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.True(NumberBaseHelper<nuint>.IsInteger((nuint)0x00000000));
                Assert.True(NumberBaseHelper<nuint>.IsInteger((nuint)0x00000001));
                Assert.True(NumberBaseHelper<nuint>.IsInteger((nuint)0x7FFFFFFF));
                Assert.True(NumberBaseHelper<nuint>.IsInteger((nuint)0x80000000));
                Assert.True(NumberBaseHelper<nuint>.IsInteger((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void IsNaNTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.False(NumberBaseHelper<nuint>.IsNaN(unchecked((nuint)0x0000000000000000)));
                Assert.False(NumberBaseHelper<nuint>.IsNaN(unchecked((nuint)0x0000000000000001)));
                Assert.False(NumberBaseHelper<nuint>.IsNaN(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.False(NumberBaseHelper<nuint>.IsNaN(unchecked((nuint)0x8000000000000000)));
                Assert.False(NumberBaseHelper<nuint>.IsNaN(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.False(NumberBaseHelper<nuint>.IsNaN((nuint)0x00000000));
                Assert.False(NumberBaseHelper<nuint>.IsNaN((nuint)0x00000001));
                Assert.False(NumberBaseHelper<nuint>.IsNaN((nuint)0x7FFFFFFF));
                Assert.False(NumberBaseHelper<nuint>.IsNaN((nuint)0x80000000));
                Assert.False(NumberBaseHelper<nuint>.IsNaN((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void IsNegativeTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.False(NumberBaseHelper<nuint>.IsNegative(unchecked((nuint)0x0000000000000000)));
                Assert.False(NumberBaseHelper<nuint>.IsNegative(unchecked((nuint)0x0000000000000001)));
                Assert.False(NumberBaseHelper<nuint>.IsNegative(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.False(NumberBaseHelper<nuint>.IsNegative(unchecked((nuint)0x8000000000000000)));
                Assert.False(NumberBaseHelper<nuint>.IsNegative(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.False(NumberBaseHelper<nuint>.IsNegative((nuint)0x00000000));
                Assert.False(NumberBaseHelper<nuint>.IsNegative((nuint)0x00000001));
                Assert.False(NumberBaseHelper<nuint>.IsNegative((nuint)0x7FFFFFFF));
                Assert.False(NumberBaseHelper<nuint>.IsNegative((nuint)0x80000000));
                Assert.False(NumberBaseHelper<nuint>.IsNegative((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void IsNegativeInfinityTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.False(NumberBaseHelper<nuint>.IsNegativeInfinity(unchecked((nuint)0x0000000000000000)));
                Assert.False(NumberBaseHelper<nuint>.IsNegativeInfinity(unchecked((nuint)0x0000000000000001)));
                Assert.False(NumberBaseHelper<nuint>.IsNegativeInfinity(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.False(NumberBaseHelper<nuint>.IsNegativeInfinity(unchecked((nuint)0x8000000000000000)));
                Assert.False(NumberBaseHelper<nuint>.IsNegativeInfinity(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.False(NumberBaseHelper<nuint>.IsNegativeInfinity((nuint)0x00000000));
                Assert.False(NumberBaseHelper<nuint>.IsNegativeInfinity((nuint)0x00000001));
                Assert.False(NumberBaseHelper<nuint>.IsNegativeInfinity((nuint)0x7FFFFFFF));
                Assert.False(NumberBaseHelper<nuint>.IsNegativeInfinity((nuint)0x80000000));
                Assert.False(NumberBaseHelper<nuint>.IsNegativeInfinity((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void IsNormalTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.False(NumberBaseHelper<nuint>.IsNormal(unchecked((nuint)0x0000000000000000)));
                Assert.True(NumberBaseHelper<nuint>.IsNormal(unchecked((nuint)0x0000000000000001)));
                Assert.True(NumberBaseHelper<nuint>.IsNormal(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.True(NumberBaseHelper<nuint>.IsNormal(unchecked((nuint)0x8000000000000000)));
                Assert.True(NumberBaseHelper<nuint>.IsNormal(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.False(NumberBaseHelper<nuint>.IsNormal((nuint)0x00000000));
                Assert.True(NumberBaseHelper<nuint>.IsNormal((nuint)0x00000001));
                Assert.True(NumberBaseHelper<nuint>.IsNormal((nuint)0x7FFFFFFF));
                Assert.True(NumberBaseHelper<nuint>.IsNormal((nuint)0x80000000));
                Assert.True(NumberBaseHelper<nuint>.IsNormal((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void IsOddIntegerTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.False(NumberBaseHelper<nuint>.IsOddInteger(unchecked((nuint)0x0000000000000000)));
                Assert.True(NumberBaseHelper<nuint>.IsOddInteger(unchecked((nuint)0x0000000000000001)));
                Assert.True(NumberBaseHelper<nuint>.IsOddInteger(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.False(NumberBaseHelper<nuint>.IsOddInteger(unchecked((nuint)0x8000000000000000)));
                Assert.True(NumberBaseHelper<nuint>.IsOddInteger(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.False(NumberBaseHelper<nuint>.IsOddInteger((nuint)0x00000000));
                Assert.True(NumberBaseHelper<nuint>.IsOddInteger((nuint)0x00000001));
                Assert.True(NumberBaseHelper<nuint>.IsOddInteger((nuint)0x7FFFFFFF));
                Assert.False(NumberBaseHelper<nuint>.IsOddInteger((nuint)0x80000000));
                Assert.True(NumberBaseHelper<nuint>.IsOddInteger((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void IsPositiveTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.True(NumberBaseHelper<nuint>.IsPositive(unchecked((nuint)0x0000000000000000)));
                Assert.True(NumberBaseHelper<nuint>.IsPositive(unchecked((nuint)0x0000000000000001)));
                Assert.True(NumberBaseHelper<nuint>.IsPositive(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.True(NumberBaseHelper<nuint>.IsPositive(unchecked((nuint)0x8000000000000000)));
                Assert.True(NumberBaseHelper<nuint>.IsPositive(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.True(NumberBaseHelper<nuint>.IsPositive((nuint)0x00000000));
                Assert.True(NumberBaseHelper<nuint>.IsPositive((nuint)0x00000001));
                Assert.True(NumberBaseHelper<nuint>.IsPositive((nuint)0x7FFFFFFF));
                Assert.True(NumberBaseHelper<nuint>.IsPositive((nuint)0x80000000));
                Assert.True(NumberBaseHelper<nuint>.IsPositive((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void IsPositiveInfinityTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.False(NumberBaseHelper<nuint>.IsPositiveInfinity(unchecked((nuint)0x0000000000000000)));
                Assert.False(NumberBaseHelper<nuint>.IsPositiveInfinity(unchecked((nuint)0x0000000000000001)));
                Assert.False(NumberBaseHelper<nuint>.IsPositiveInfinity(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.False(NumberBaseHelper<nuint>.IsPositiveInfinity(unchecked((nuint)0x8000000000000000)));
                Assert.False(NumberBaseHelper<nuint>.IsPositiveInfinity(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.False(NumberBaseHelper<nuint>.IsPositiveInfinity((nuint)0x00000000));
                Assert.False(NumberBaseHelper<nuint>.IsPositiveInfinity((nuint)0x00000001));
                Assert.False(NumberBaseHelper<nuint>.IsPositiveInfinity((nuint)0x7FFFFFFF));
                Assert.False(NumberBaseHelper<nuint>.IsPositiveInfinity((nuint)0x80000000));
                Assert.False(NumberBaseHelper<nuint>.IsPositiveInfinity((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void IsRealNumberTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.True(NumberBaseHelper<nuint>.IsRealNumber(unchecked((nuint)0x0000000000000000)));
                Assert.True(NumberBaseHelper<nuint>.IsRealNumber(unchecked((nuint)0x0000000000000001)));
                Assert.True(NumberBaseHelper<nuint>.IsRealNumber(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.True(NumberBaseHelper<nuint>.IsRealNumber(unchecked((nuint)0x8000000000000000)));
                Assert.True(NumberBaseHelper<nuint>.IsRealNumber(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.True(NumberBaseHelper<nuint>.IsRealNumber((nuint)0x00000000));
                Assert.True(NumberBaseHelper<nuint>.IsRealNumber((nuint)0x00000001));
                Assert.True(NumberBaseHelper<nuint>.IsRealNumber((nuint)0x7FFFFFFF));
                Assert.True(NumberBaseHelper<nuint>.IsRealNumber((nuint)0x80000000));
                Assert.True(NumberBaseHelper<nuint>.IsRealNumber((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void IsSubnormalTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.False(NumberBaseHelper<nuint>.IsSubnormal(unchecked((nuint)0x0000000000000000)));
                Assert.False(NumberBaseHelper<nuint>.IsSubnormal(unchecked((nuint)0x0000000000000001)));
                Assert.False(NumberBaseHelper<nuint>.IsSubnormal(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.False(NumberBaseHelper<nuint>.IsSubnormal(unchecked((nuint)0x8000000000000000)));
                Assert.False(NumberBaseHelper<nuint>.IsSubnormal(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.False(NumberBaseHelper<nuint>.IsSubnormal((nuint)0x00000000));
                Assert.False(NumberBaseHelper<nuint>.IsSubnormal((nuint)0x00000001));
                Assert.False(NumberBaseHelper<nuint>.IsSubnormal((nuint)0x7FFFFFFF));
                Assert.False(NumberBaseHelper<nuint>.IsSubnormal((nuint)0x80000000));
                Assert.False(NumberBaseHelper<nuint>.IsSubnormal((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void IsZeroTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.True(NumberBaseHelper<nuint>.IsZero(unchecked((nuint)0x0000000000000000)));
                Assert.False(NumberBaseHelper<nuint>.IsZero(unchecked((nuint)0x0000000000000001)));
                Assert.False(NumberBaseHelper<nuint>.IsZero(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.False(NumberBaseHelper<nuint>.IsZero(unchecked((nuint)0x8000000000000000)));
                Assert.False(NumberBaseHelper<nuint>.IsZero(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.True(NumberBaseHelper<nuint>.IsZero((nuint)0x00000000));
                Assert.False(NumberBaseHelper<nuint>.IsZero((nuint)0x00000001));
                Assert.False(NumberBaseHelper<nuint>.IsZero((nuint)0x7FFFFFFF));
                Assert.False(NumberBaseHelper<nuint>.IsZero((nuint)0x80000000));
                Assert.False(NumberBaseHelper<nuint>.IsZero((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void MaxMagnitudeTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000000000000001), NumberBaseHelper<nuint>.MaxMagnitude(unchecked((nuint)0x0000000000000000), (nuint)1));
                Assert.Equal(unchecked((nuint)0x0000000000000001), NumberBaseHelper<nuint>.MaxMagnitude(unchecked((nuint)0x0000000000000001), (nuint)1));
                Assert.Equal(unchecked((nuint)0x7FFFFFFFFFFFFFFF), NumberBaseHelper<nuint>.MaxMagnitude(unchecked((nuint)0x7FFFFFFFFFFFFFFF), (nuint)1));
                Assert.Equal(unchecked((nuint)0x8000000000000000), NumberBaseHelper<nuint>.MaxMagnitude(unchecked((nuint)0x8000000000000000), (nuint)1));
                Assert.Equal(unchecked((nuint)0xFFFFFFFFFFFFFFFF), NumberBaseHelper<nuint>.MaxMagnitude(unchecked((nuint)0xFFFFFFFFFFFFFFFF), (nuint)1));
            }
            else
            {
                Assert.Equal((nuint)0x00000001, NumberBaseHelper<nuint>.MaxMagnitude((nuint)0x00000000, (nuint)1));
                Assert.Equal((nuint)0x00000001, NumberBaseHelper<nuint>.MaxMagnitude((nuint)0x00000001, (nuint)1));
                Assert.Equal((nuint)0x7FFFFFFF, NumberBaseHelper<nuint>.MaxMagnitude((nuint)0x7FFFFFFF, (nuint)1));
                Assert.Equal((nuint)0x80000000, NumberBaseHelper<nuint>.MaxMagnitude((nuint)0x80000000, (nuint)1));
                Assert.Equal((nuint)0xFFFFFFFF, NumberBaseHelper<nuint>.MaxMagnitude((nuint)0xFFFFFFFF, (nuint)1));
            }
        }

        [Fact]
        public static void MaxMagnitudeNumberTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000000000000001), NumberBaseHelper<nuint>.MaxMagnitudeNumber(unchecked((nuint)0x0000000000000000), (nuint)1));
                Assert.Equal(unchecked((nuint)0x0000000000000001), NumberBaseHelper<nuint>.MaxMagnitudeNumber(unchecked((nuint)0x0000000000000001), (nuint)1));
                Assert.Equal(unchecked((nuint)0x7FFFFFFFFFFFFFFF), NumberBaseHelper<nuint>.MaxMagnitudeNumber(unchecked((nuint)0x7FFFFFFFFFFFFFFF), (nuint)1));
                Assert.Equal(unchecked((nuint)0x8000000000000000), NumberBaseHelper<nuint>.MaxMagnitudeNumber(unchecked((nuint)0x8000000000000000), (nuint)1));
                Assert.Equal(unchecked((nuint)0xFFFFFFFFFFFFFFFF), NumberBaseHelper<nuint>.MaxMagnitudeNumber(unchecked((nuint)0xFFFFFFFFFFFFFFFF), (nuint)1));
            }
            else
            {
                Assert.Equal((nuint)0x00000001, NumberBaseHelper<nuint>.MaxMagnitudeNumber((nuint)0x00000000, (nuint)1));
                Assert.Equal((nuint)0x00000001, NumberBaseHelper<nuint>.MaxMagnitudeNumber((nuint)0x00000001, (nuint)1));
                Assert.Equal((nuint)0x7FFFFFFF, NumberBaseHelper<nuint>.MaxMagnitudeNumber((nuint)0x7FFFFFFF, (nuint)1));
                Assert.Equal((nuint)0x80000000, NumberBaseHelper<nuint>.MaxMagnitudeNumber((nuint)0x80000000, (nuint)1));
                Assert.Equal((nuint)0xFFFFFFFF, NumberBaseHelper<nuint>.MaxMagnitudeNumber((nuint)0xFFFFFFFF, (nuint)1));
            }
        }

        [Fact]
        public static void MinMagnitudeTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000000000000000), NumberBaseHelper<nuint>.MinMagnitude(unchecked((nuint)0x0000000000000000), (nuint)1));
                Assert.Equal(unchecked((nuint)0x0000000000000001), NumberBaseHelper<nuint>.MinMagnitude(unchecked((nuint)0x0000000000000001), (nuint)1));
                Assert.Equal(unchecked((nuint)0x0000000000000001), NumberBaseHelper<nuint>.MinMagnitude(unchecked((nuint)0x7FFFFFFFFFFFFFFF), (nuint)1));
                Assert.Equal(unchecked((nuint)0x0000000000000001), NumberBaseHelper<nuint>.MinMagnitude(unchecked((nuint)0x8000000000000000), (nuint)1));
                Assert.Equal(unchecked((nuint)0x0000000000000001), NumberBaseHelper<nuint>.MinMagnitude(unchecked((nuint)0xFFFFFFFFFFFFFFFF), (nuint)1));
            }
            else
            {
                Assert.Equal((nuint)0x00000000, NumberBaseHelper<nuint>.MinMagnitude((nuint)0x00000000, (nuint)1));
                Assert.Equal((nuint)0x00000001, NumberBaseHelper<nuint>.MinMagnitude((nuint)0x00000001, (nuint)1));
                Assert.Equal((nuint)0x00000001, NumberBaseHelper<nuint>.MinMagnitude((nuint)0x7FFFFFFF, (nuint)1));
                Assert.Equal((nuint)0x00000001, NumberBaseHelper<nuint>.MinMagnitude((nuint)0x80000000, (nuint)1));
                Assert.Equal((nuint)0x00000001, NumberBaseHelper<nuint>.MinMagnitude((nuint)0xFFFFFFFF, (nuint)1));
            }
        }

        [Fact]
        public static void MinMagnitudeNumberTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000000000000000), NumberBaseHelper<nuint>.MinMagnitudeNumber(unchecked((nuint)0x0000000000000000), (nuint)1));
                Assert.Equal(unchecked((nuint)0x0000000000000001), NumberBaseHelper<nuint>.MinMagnitudeNumber(unchecked((nuint)0x0000000000000001), (nuint)1));
                Assert.Equal(unchecked((nuint)0x0000000000000001), NumberBaseHelper<nuint>.MinMagnitudeNumber(unchecked((nuint)0x7FFFFFFFFFFFFFFF), (nuint)1));
                Assert.Equal(unchecked((nuint)0x0000000000000001), NumberBaseHelper<nuint>.MinMagnitudeNumber(unchecked((nuint)0x8000000000000000), (nuint)1));
                Assert.Equal(unchecked((nuint)0x0000000000000001), NumberBaseHelper<nuint>.MinMagnitudeNumber(unchecked((nuint)0xFFFFFFFFFFFFFFFF), (nuint)1));
            }
            else
            {
                Assert.Equal((nuint)0x00000000, NumberBaseHelper<nuint>.MinMagnitudeNumber((nuint)0x00000000, (nuint)1));
                Assert.Equal((nuint)0x00000001, NumberBaseHelper<nuint>.MinMagnitudeNumber((nuint)0x00000001, (nuint)1));
                Assert.Equal((nuint)0x00000001, NumberBaseHelper<nuint>.MinMagnitudeNumber((nuint)0x7FFFFFFF, (nuint)1));
                Assert.Equal((nuint)0x00000001, NumberBaseHelper<nuint>.MinMagnitudeNumber((nuint)0x80000000, (nuint)1));
                Assert.Equal((nuint)0x00000001, NumberBaseHelper<nuint>.MinMagnitudeNumber((nuint)0xFFFFFFFF, (nuint)1));
            }
        }

        //
        // IShiftOperators
        //

        [Fact]
        public static void op_LeftShiftTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000000000000000), ShiftOperatorsHelper<nuint, nuint>.op_LeftShift(unchecked((nuint)0x0000000000000000), 1));
                Assert.Equal(unchecked((nuint)0x0000000000000002), ShiftOperatorsHelper<nuint, nuint>.op_LeftShift(unchecked((nuint)0x0000000000000001), 1));
                Assert.Equal(unchecked((nuint)0xFFFFFFFFFFFFFFFE), ShiftOperatorsHelper<nuint, nuint>.op_LeftShift(unchecked((nuint)0x7FFFFFFFFFFFFFFF), 1));
                Assert.Equal(unchecked((nuint)0x0000000000000000), ShiftOperatorsHelper<nuint, nuint>.op_LeftShift(unchecked((nuint)0x8000000000000000), 1));
                Assert.Equal(unchecked((nuint)0xFFFFFFFFFFFFFFFE), ShiftOperatorsHelper<nuint, nuint>.op_LeftShift(unchecked((nuint)0xFFFFFFFFFFFFFFFF), 1));
            }
            else
            {
                Assert.Equal((nuint)0x00000000, ShiftOperatorsHelper<nuint, nuint>.op_LeftShift((nuint)0x00000000, 1));
                Assert.Equal((nuint)0x00000002, ShiftOperatorsHelper<nuint, nuint>.op_LeftShift((nuint)0x00000001, 1));
                Assert.Equal((nuint)0xFFFFFFFE, ShiftOperatorsHelper<nuint, nuint>.op_LeftShift((nuint)0x7FFFFFFF, 1));
                Assert.Equal((nuint)0x00000000, ShiftOperatorsHelper<nuint, nuint>.op_LeftShift((nuint)0x80000000, 1));
                Assert.Equal((nuint)0xFFFFFFFE, ShiftOperatorsHelper<nuint, nuint>.op_LeftShift((nuint)0xFFFFFFFF, 1));
            }
        }

        [Fact]
        public static void op_RightShiftTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000000000000000), ShiftOperatorsHelper<nuint, nuint>.op_RightShift(unchecked((nuint)0x0000000000000000), 1));
                Assert.Equal(unchecked((nuint)0x0000000000000000), ShiftOperatorsHelper<nuint, nuint>.op_RightShift(unchecked((nuint)0x0000000000000001), 1));
                Assert.Equal(unchecked((nuint)0x3FFFFFFFFFFFFFFF), ShiftOperatorsHelper<nuint, nuint>.op_RightShift(unchecked((nuint)0x7FFFFFFFFFFFFFFF), 1));
                Assert.Equal(unchecked((nuint)0x4000000000000000), ShiftOperatorsHelper<nuint, nuint>.op_RightShift(unchecked((nuint)0x8000000000000000), 1));
                Assert.Equal(unchecked((nuint)0x7FFFFFFFFFFFFFFF), ShiftOperatorsHelper<nuint, nuint>.op_RightShift(unchecked((nuint)0xFFFFFFFFFFFFFFFF), 1));
            }
            else
            {
                Assert.Equal((nuint)0x00000000, ShiftOperatorsHelper<nuint, nuint>.op_RightShift((nuint)0x00000000, 1));
                Assert.Equal((nuint)0x00000000, ShiftOperatorsHelper<nuint, nuint>.op_RightShift((nuint)0x00000001, 1));
                Assert.Equal((nuint)0x3FFFFFFF, ShiftOperatorsHelper<nuint, nuint>.op_RightShift((nuint)0x7FFFFFFF, 1));
                Assert.Equal((nuint)0x40000000, ShiftOperatorsHelper<nuint, nuint>.op_RightShift((nuint)0x80000000, 1));
                Assert.Equal((nuint)0x7FFFFFFF, ShiftOperatorsHelper<nuint, nuint>.op_RightShift((nuint)0xFFFFFFFF, 1));
            }
        }

        [Fact]
        public static void op_UnsignedRightShiftTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000000000000000), ShiftOperatorsHelper<nuint, nuint>.op_UnsignedRightShift(unchecked((nuint)0x0000000000000000), 1));
                Assert.Equal(unchecked((nuint)0x0000000000000000), ShiftOperatorsHelper<nuint, nuint>.op_UnsignedRightShift(unchecked((nuint)0x0000000000000001), 1));
                Assert.Equal(unchecked((nuint)0x3FFFFFFFFFFFFFFF), ShiftOperatorsHelper<nuint, nuint>.op_UnsignedRightShift(unchecked((nuint)0x7FFFFFFFFFFFFFFF), 1));
                Assert.Equal(unchecked((nuint)0x4000000000000000), ShiftOperatorsHelper<nuint, nuint>.op_UnsignedRightShift(unchecked((nuint)0x8000000000000000), 1));
                Assert.Equal(unchecked((nuint)0x7FFFFFFFFFFFFFFF), ShiftOperatorsHelper<nuint, nuint>.op_UnsignedRightShift(unchecked((nuint)0xFFFFFFFFFFFFFFFF), 1));
            }
            else
            {
                Assert.Equal((nuint)0x00000000, ShiftOperatorsHelper<nuint, nuint>.op_UnsignedRightShift((nuint)0x00000000, 1));
                Assert.Equal((nuint)0x00000000, ShiftOperatorsHelper<nuint, nuint>.op_UnsignedRightShift((nuint)0x00000001, 1));
                Assert.Equal((nuint)0x3FFFFFFF, ShiftOperatorsHelper<nuint, nuint>.op_UnsignedRightShift((nuint)0x7FFFFFFF, 1));
                Assert.Equal((nuint)0x40000000, ShiftOperatorsHelper<nuint, nuint>.op_UnsignedRightShift((nuint)0x80000000, 1));
                Assert.Equal((nuint)0x7FFFFFFF, ShiftOperatorsHelper<nuint, nuint>.op_UnsignedRightShift((nuint)0xFFFFFFFF, 1));
            }
        }

        //
        // ISubtractionOperators
        //

        [Fact]
        public static void op_SubtractionTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0xFFFFFFFFFFFFFFFF), SubtractionOperatorsHelper<nuint, nuint, nuint>.op_Subtraction(unchecked((nuint)0x0000000000000000), (nuint)1));
                Assert.Equal(unchecked((nuint)0x0000000000000000), SubtractionOperatorsHelper<nuint, nuint, nuint>.op_Subtraction(unchecked((nuint)0x0000000000000001), (nuint)1));
                Assert.Equal(unchecked((nuint)0x7FFFFFFFFFFFFFFE), SubtractionOperatorsHelper<nuint, nuint, nuint>.op_Subtraction(unchecked((nuint)0x7FFFFFFFFFFFFFFF), (nuint)1));
                Assert.Equal(unchecked((nuint)0x7FFFFFFFFFFFFFFF), SubtractionOperatorsHelper<nuint, nuint, nuint>.op_Subtraction(unchecked((nuint)0x8000000000000000), (nuint)1));
                Assert.Equal(unchecked((nuint)0xFFFFFFFFFFFFFFFE), SubtractionOperatorsHelper<nuint, nuint, nuint>.op_Subtraction(unchecked((nuint)0xFFFFFFFFFFFFFFFF), (nuint)1));
            }
            else
            {
                Assert.Equal((nuint)0xFFFFFFFF, SubtractionOperatorsHelper<nuint, nuint, nuint>.op_Subtraction((nuint)0x00000000, (nuint)1));
                Assert.Equal((nuint)0x00000000, SubtractionOperatorsHelper<nuint, nuint, nuint>.op_Subtraction((nuint)0x00000001, (nuint)1));
                Assert.Equal((nuint)0x7FFFFFFE, SubtractionOperatorsHelper<nuint, nuint, nuint>.op_Subtraction((nuint)0x7FFFFFFF, (nuint)1));
                Assert.Equal((nuint)0x7FFFFFFF, SubtractionOperatorsHelper<nuint, nuint, nuint>.op_Subtraction((nuint)0x80000000, (nuint)1));
                Assert.Equal((nuint)0xFFFFFFFE, SubtractionOperatorsHelper<nuint, nuint, nuint>.op_Subtraction((nuint)0xFFFFFFFF, (nuint)1));
            }
        }

        [Fact]
        public static void op_CheckedSubtractionTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000000000000000), SubtractionOperatorsHelper<nuint, nuint, nuint>.op_CheckedSubtraction(unchecked((nuint)0x0000000000000001), (nuint)1));
                Assert.Equal(unchecked((nuint)0x7FFFFFFFFFFFFFFE), SubtractionOperatorsHelper<nuint, nuint, nuint>.op_CheckedSubtraction(unchecked((nuint)0x7FFFFFFFFFFFFFFF), (nuint)1));
                Assert.Equal(unchecked((nuint)0x7FFFFFFFFFFFFFFF), SubtractionOperatorsHelper<nuint, nuint, nuint>.op_CheckedSubtraction(unchecked((nuint)0x8000000000000000), (nuint)1));
                Assert.Equal(unchecked((nuint)0xFFFFFFFFFFFFFFFE), SubtractionOperatorsHelper<nuint, nuint, nuint>.op_CheckedSubtraction(unchecked((nuint)0xFFFFFFFFFFFFFFFF), (nuint)1));

                Assert.Throws<OverflowException>(() => SubtractionOperatorsHelper<nuint, nuint, nuint>.op_CheckedSubtraction(unchecked((nuint)0x0000000000000000), (nuint)1));
            }
            else
            {
                Assert.Equal((nuint)0x00000000, SubtractionOperatorsHelper<nuint, nuint, nuint>.op_CheckedSubtraction((nuint)0x00000001, (nuint)1));
                Assert.Equal((nuint)0x7FFFFFFE, SubtractionOperatorsHelper<nuint, nuint, nuint>.op_CheckedSubtraction((nuint)0x7FFFFFFF, (nuint)1));
                Assert.Equal((nuint)0x7FFFFFFF, SubtractionOperatorsHelper<nuint, nuint, nuint>.op_CheckedSubtraction((nuint)0x80000000, (nuint)1));
                Assert.Equal((nuint)0xFFFFFFFE, SubtractionOperatorsHelper<nuint, nuint, nuint>.op_CheckedSubtraction((nuint)0xFFFFFFFF, (nuint)1));

                Assert.Throws<OverflowException>(() => SubtractionOperatorsHelper<nuint, nuint, nuint>.op_CheckedSubtraction((nuint)0x00000000, (nuint)1));
            }
        }

        //
        // IUnaryNegationOperators
        //

        [Fact]
        public static void op_UnaryNegationTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000000000000000), UnaryNegationOperatorsHelper<nuint, nuint>.op_UnaryNegation(unchecked((nuint)0x0000000000000000)));
                Assert.Equal(unchecked((nuint)0xFFFFFFFFFFFFFFFF), UnaryNegationOperatorsHelper<nuint, nuint>.op_UnaryNegation(unchecked((nuint)0x0000000000000001)));
                Assert.Equal(unchecked((nuint)0x8000000000000001), UnaryNegationOperatorsHelper<nuint, nuint>.op_UnaryNegation(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((nuint)0x8000000000000000), UnaryNegationOperatorsHelper<nuint, nuint>.op_UnaryNegation(unchecked((nuint)0x8000000000000000)));
                Assert.Equal(unchecked((nuint)0x0000000000000001), UnaryNegationOperatorsHelper<nuint, nuint>.op_UnaryNegation(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((nuint)0x00000000, UnaryNegationOperatorsHelper<nuint, nuint>.op_UnaryNegation((nuint)0x00000000));
                Assert.Equal((nuint)0xFFFFFFFF, UnaryNegationOperatorsHelper<nuint, nuint>.op_UnaryNegation((nuint)0x00000001));
                Assert.Equal((nuint)0x80000001, UnaryNegationOperatorsHelper<nuint, nuint>.op_UnaryNegation((nuint)0x7FFFFFFF));
                Assert.Equal((nuint)0x80000000, UnaryNegationOperatorsHelper<nuint, nuint>.op_UnaryNegation((nuint)0x80000000));
                Assert.Equal((nuint)0x00000001, UnaryNegationOperatorsHelper<nuint, nuint>.op_UnaryNegation((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void op_CheckedUnaryNegationTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000000000000000), UnaryNegationOperatorsHelper<nuint, nuint>.op_CheckedUnaryNegation(unchecked((nuint)0x0000000000000000)));

                Assert.Throws<OverflowException>(() => UnaryNegationOperatorsHelper<nuint, nuint>.op_CheckedUnaryNegation(unchecked((nuint)0x0000000000000001)));
                Assert.Throws<OverflowException>(() => UnaryNegationOperatorsHelper<nuint, nuint>.op_CheckedUnaryNegation(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Throws<OverflowException>(() => UnaryNegationOperatorsHelper<nuint, nuint>.op_CheckedUnaryNegation(unchecked((nuint)0x8000000000000000)));
                Assert.Throws<OverflowException>(() => UnaryNegationOperatorsHelper<nuint, nuint>.op_CheckedUnaryNegation(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((nuint)0x00000000, UnaryNegationOperatorsHelper<nuint, nuint>.op_CheckedUnaryNegation((nuint)0x00000000));

                Assert.Throws<OverflowException>(() => UnaryNegationOperatorsHelper<nuint, nuint>.op_CheckedUnaryNegation((nuint)0x00000001));
                Assert.Throws<OverflowException>(() => UnaryNegationOperatorsHelper<nuint, nuint>.op_CheckedUnaryNegation((nuint)0x7FFFFFFF));
                Assert.Throws<OverflowException>(() => UnaryNegationOperatorsHelper<nuint, nuint>.op_CheckedUnaryNegation((nuint)0x80000000));
                Assert.Throws<OverflowException>(() => UnaryNegationOperatorsHelper<nuint, nuint>.op_CheckedUnaryNegation((nuint)0xFFFFFFFF));
            }
        }

        //
        // IUnaryPlusOperators
        //

        [Fact]
        public static void op_UnaryPlusTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000000000000000), UnaryPlusOperatorsHelper<nuint, nuint>.op_UnaryPlus(unchecked((nuint)0x0000000000000000)));
                Assert.Equal(unchecked((nuint)0x0000000000000001), UnaryPlusOperatorsHelper<nuint, nuint>.op_UnaryPlus(unchecked((nuint)0x0000000000000001)));
                Assert.Equal(unchecked((nuint)0x7FFFFFFFFFFFFFFF), UnaryPlusOperatorsHelper<nuint, nuint>.op_UnaryPlus(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((nuint)0x8000000000000000), UnaryPlusOperatorsHelper<nuint, nuint>.op_UnaryPlus(unchecked((nuint)0x8000000000000000)));
                Assert.Equal(unchecked((nuint)0xFFFFFFFFFFFFFFFF), UnaryPlusOperatorsHelper<nuint, nuint>.op_UnaryPlus(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((nuint)0x00000000, UnaryPlusOperatorsHelper<nuint, nuint>.op_UnaryPlus((nuint)0x00000000));
                Assert.Equal((nuint)0x00000001, UnaryPlusOperatorsHelper<nuint, nuint>.op_UnaryPlus((nuint)0x00000001));
                Assert.Equal((nuint)0x7FFFFFFF, UnaryPlusOperatorsHelper<nuint, nuint>.op_UnaryPlus((nuint)0x7FFFFFFF));
                Assert.Equal((nuint)0x80000000, UnaryPlusOperatorsHelper<nuint, nuint>.op_UnaryPlus((nuint)0x80000000));
                Assert.Equal((nuint)0xFFFFFFFF, UnaryPlusOperatorsHelper<nuint, nuint>.op_UnaryPlus((nuint)0xFFFFFFFF));
            }
        }

        //
        // IParsable and ISpanParsable
        //

        [Theory]
        [MemberData(nameof(UIntPtrTests.Parse_Valid_TestData), MemberType = typeof(UIntPtrTests))]
        public static void ParseValidStringTest(string value, NumberStyles style, IFormatProvider provider, nuint expected)
        {
            nuint result;

            // Default style and provider
            if ((style == NumberStyles.Integer) && (provider is null))
            {
                Assert.True(ParsableHelper<nuint>.TryParse(value, provider, out result));
                Assert.Equal(expected, result);
                Assert.Equal(expected, ParsableHelper<nuint>.Parse(value, provider));
            }

            // Default provider
            if (provider is null)
            {
                Assert.Equal(expected, NumberBaseHelper<nuint>.Parse(value, style, provider));

                // Substitute default NumberFormatInfo
                Assert.True(NumberBaseHelper<nuint>.TryParse(value, style, new NumberFormatInfo(), out result));
                Assert.Equal(expected, result);
                Assert.Equal(expected, NumberBaseHelper<nuint>.Parse(value, style, new NumberFormatInfo()));
            }

            // Default style
            if (style == NumberStyles.Integer)
            {
                Assert.Equal(expected, ParsableHelper<nuint>.Parse(value, provider));
            }

            // Full overloads
            Assert.True(NumberBaseHelper<nuint>.TryParse(value, style, provider, out result));
            Assert.Equal(expected, result);
            Assert.Equal(expected, NumberBaseHelper<nuint>.Parse(value, style, provider));
        }

        [Theory]
        [MemberData(nameof(UIntPtrTests.Parse_Invalid_TestData), MemberType = typeof(UIntPtrTests))]
        public static void ParseInvalidStringTest(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            nuint result;

            // Default style and provider
            if ((style == NumberStyles.Integer) && (provider is null))
            {
                Assert.False(ParsableHelper<nuint>.TryParse(value, provider, out result));
                Assert.Equal(default(nuint), result);
                Assert.Throws(exceptionType, () => ParsableHelper<nuint>.Parse(value, provider));
            }

            // Default provider
            if (provider is null)
            {
                Assert.Throws(exceptionType, () => NumberBaseHelper<nuint>.Parse(value, style, provider));

                // Substitute default NumberFormatInfo
                Assert.False(NumberBaseHelper<nuint>.TryParse(value, style, new NumberFormatInfo(), out result));
                Assert.Equal(default(nuint), result);
                Assert.Throws(exceptionType, () => NumberBaseHelper<nuint>.Parse(value, style, new NumberFormatInfo()));
            }

            // Default style
            if (style == NumberStyles.Integer)
            {
                Assert.Throws(exceptionType, () => ParsableHelper<nuint>.Parse(value, provider));
            }

            // Full overloads
            Assert.False(NumberBaseHelper<nuint>.TryParse(value, style, provider, out result));
            Assert.Equal(default(nuint), result);
            Assert.Throws(exceptionType, () => NumberBaseHelper<nuint>.Parse(value, style, provider));
        }

        [Theory]
        [MemberData(nameof(UIntPtrTests.Parse_ValidWithOffsetCount_TestData), MemberType = typeof(UIntPtrTests))]
        public static void ParseValidSpanTest(string value, int offset, int count, NumberStyles style, IFormatProvider provider, nuint expected)
        {
            nuint result;

            // Default style and provider
            if ((style == NumberStyles.Integer) && (provider is null))
            {
                Assert.True(SpanParsableHelper<nuint>.TryParse(value.AsSpan(offset, count), provider, out result));
                Assert.Equal(expected, result);
            }

            Assert.Equal(expected, NumberBaseHelper<nuint>.Parse(value.AsSpan(offset, count), style, provider));

            Assert.True(NumberBaseHelper<nuint>.TryParse(value.AsSpan(offset, count), style, provider, out result));
            Assert.Equal(expected, result);
        }

        [Theory]
        [MemberData(nameof(UIntPtrTests.Parse_Invalid_TestData), MemberType = typeof(UIntPtrTests))]
        public static void ParseInvalidSpanTest(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            if (value is null)
            {
                return;
            }

            nuint result;

            // Default style and provider
            if ((style == NumberStyles.Integer) && (provider is null))
            {
                Assert.False(SpanParsableHelper<nuint>.TryParse(value.AsSpan(), provider, out result));
                Assert.Equal(default(nuint), result);
            }

            Assert.Throws(exceptionType, () => NumberBaseHelper<nuint>.Parse(value.AsSpan(), style, provider));

            Assert.False(NumberBaseHelper<nuint>.TryParse(value.AsSpan(), style, provider, out result));
            Assert.Equal(default(nuint), result);
        }
    }
}
