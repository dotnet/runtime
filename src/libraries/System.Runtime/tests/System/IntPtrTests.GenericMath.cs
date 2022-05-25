// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Tests
{
    public class IntPtrTests_GenericMath
    {
        //
        // IAdditionOperators
        //

        [Fact]
        public static void op_AdditionTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0x0000000000000001), AdditionOperatorsHelper<nint, nint, nint>.op_Addition(unchecked((nint)0x0000000000000000), (nint)1));
                Assert.Equal(unchecked((nint)0x0000000000000002), AdditionOperatorsHelper<nint, nint, nint>.op_Addition(unchecked((nint)0x0000000000000001), (nint)1));
                Assert.Equal(unchecked((nint)0x8000000000000000), AdditionOperatorsHelper<nint, nint, nint>.op_Addition(unchecked((nint)0x7FFFFFFFFFFFFFFF), (nint)1));
                Assert.Equal(unchecked((nint)0x8000000000000001), AdditionOperatorsHelper<nint, nint, nint>.op_Addition(unchecked((nint)0x8000000000000000), (nint)1));
                Assert.Equal(unchecked((nint)0x0000000000000000), AdditionOperatorsHelper<nint, nint, nint>.op_Addition(unchecked((nint)0xFFFFFFFFFFFFFFFF), (nint)1));
            }
            else
            {
                Assert.Equal((nint)0x00000001, AdditionOperatorsHelper<nint, nint, nint>.op_Addition((nint)0x00000000, (nint)1));
                Assert.Equal((nint)0x00000002, AdditionOperatorsHelper<nint, nint, nint>.op_Addition((nint)0x00000001, (nint)1));
                Assert.Equal(unchecked((nint)0x80000000), AdditionOperatorsHelper<nint, nint, nint>.op_Addition((nint)0x7FFFFFFF, (nint)1));
                Assert.Equal(unchecked((nint)0x80000001), AdditionOperatorsHelper<nint, nint, nint>.op_Addition(unchecked((nint)0x80000000), (nint)1));
                Assert.Equal((nint)0x00000000, AdditionOperatorsHelper<nint, nint, nint>.op_Addition(unchecked((nint)0xFFFFFFFF), (nint)1));
            }
        }

        [Fact]
        public static void op_CheckedAdditionTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0x0000000000000001), AdditionOperatorsHelper<nint, nint, nint>.op_CheckedAddition(unchecked((nint)0x0000000000000000), (nint)1));
                Assert.Equal(unchecked((nint)0x0000000000000002), AdditionOperatorsHelper<nint, nint, nint>.op_CheckedAddition(unchecked((nint)0x0000000000000001), (nint)1));
                Assert.Equal(unchecked((nint)0x8000000000000001), AdditionOperatorsHelper<nint, nint, nint>.op_CheckedAddition(unchecked((nint)0x8000000000000000), (nint)1));
                Assert.Equal(unchecked((nint)0x0000000000000000), AdditionOperatorsHelper<nint, nint, nint>.op_CheckedAddition(unchecked((nint)0xFFFFFFFFFFFFFFFF), (nint)1));

                Assert.Throws<OverflowException>(() => AdditionOperatorsHelper<nint, nint, nint>.op_CheckedAddition(unchecked((nint)0x7FFFFFFFFFFFFFFF), (nint)1));
            }
            else
            {
                Assert.Equal((nint)0x00000001, AdditionOperatorsHelper<nint, nint, nint>.op_CheckedAddition((nint)0x00000000, (nint)1));
                Assert.Equal((nint)0x00000002, AdditionOperatorsHelper<nint, nint, nint>.op_CheckedAddition((nint)0x00000001, (nint)1));
                Assert.Equal(unchecked((nint)0x80000001), AdditionOperatorsHelper<nint, nint, nint>.op_CheckedAddition(unchecked((nint)0x80000000), (nint)1));
                Assert.Equal((nint)0x00000000, AdditionOperatorsHelper<nint, nint, nint>.op_CheckedAddition(unchecked((nint)0xFFFFFFFF), (nint)1));

                Assert.Throws<OverflowException>(() => AdditionOperatorsHelper<nint, nint, nint>.op_CheckedAddition((nint)0x7FFFFFFF, (nint)1));
            }
        }

        //
        // IAdditiveIdentity
        //

        [Fact]
        public static void AdditiveIdentityTest()
        {
            Assert.Equal((nint)0x00000000, AdditiveIdentityHelper<nint, nint>.AdditiveIdentity);
        }

        //
        // IBinaryInteger
        //

        [Fact]
        public static void DivRemTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((unchecked((nint)0x0000000000000000), unchecked((nint)0x0000000000000000)), BinaryIntegerHelper<nint>.DivRem(unchecked((nint)0x0000000000000000), (nint)2));
                Assert.Equal((unchecked((nint)0x0000000000000000), unchecked((nint)0x0000000000000001)), BinaryIntegerHelper<nint>.DivRem(unchecked((nint)0x0000000000000001), (nint)2));
                Assert.Equal((unchecked((nint)0x3FFFFFFFFFFFFFFF), unchecked((nint)0x0000000000000001)), BinaryIntegerHelper<nint>.DivRem(unchecked((nint)0x7FFFFFFFFFFFFFFF), (nint)2));
                Assert.Equal((unchecked((nint)0xC000000000000000), unchecked((nint)0x0000000000000000)), BinaryIntegerHelper<nint>.DivRem(unchecked((nint)0x8000000000000000), (nint)2));
                Assert.Equal((unchecked((nint)0x0000000000000000), unchecked((nint)0xFFFFFFFFFFFFFFFF)), BinaryIntegerHelper<nint>.DivRem(unchecked((nint)0xFFFFFFFFFFFFFFFF), (nint)2));
            }
            else
            {
                Assert.Equal(((nint)0x00000000, (nint)0x00000000), BinaryIntegerHelper<nint>.DivRem((nint)0x00000000, (nint)2));
                Assert.Equal(((nint)0x00000000, (nint)0x00000001), BinaryIntegerHelper<nint>.DivRem((nint)0x00000001, (nint)2));
                Assert.Equal(((nint)0x3FFFFFFF, (nint)0x00000001), BinaryIntegerHelper<nint>.DivRem((nint)0x7FFFFFFF, (nint)2));
                Assert.Equal((unchecked((nint)0xC0000000), (nint)0x00000000), BinaryIntegerHelper<nint>.DivRem(unchecked((nint)0x80000000), (nint)2));
                Assert.Equal(((nint)0x00000000, unchecked((nint)0xFFFFFFFF)), BinaryIntegerHelper<nint>.DivRem(unchecked((nint)0xFFFFFFFF), (nint)2));
            }
        }

        [Fact]
        public static void LeadingZeroCountTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0x0000000000000040), BinaryIntegerHelper<nint>.LeadingZeroCount(unchecked((nint)0x0000000000000000)));
                Assert.Equal(unchecked((nint)0x000000000000003F), BinaryIntegerHelper<nint>.LeadingZeroCount(unchecked((nint)0x0000000000000001)));
                Assert.Equal(unchecked((nint)0x0000000000000001), BinaryIntegerHelper<nint>.LeadingZeroCount(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((nint)0x0000000000000000), BinaryIntegerHelper<nint>.LeadingZeroCount(unchecked((nint)0x8000000000000000)));
                Assert.Equal(unchecked((nint)0x0000000000000000), BinaryIntegerHelper<nint>.LeadingZeroCount(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((nint)0x0000000000000020, BinaryIntegerHelper<nint>.LeadingZeroCount((nint)0x00000000));
                Assert.Equal((nint)0x000000000000001F, BinaryIntegerHelper<nint>.LeadingZeroCount((nint)0x00000001));
                Assert.Equal((nint)0x0000000000000001, BinaryIntegerHelper<nint>.LeadingZeroCount((nint)0x7FFFFFFF));
                Assert.Equal((nint)0x0000000000000000, BinaryIntegerHelper<nint>.LeadingZeroCount(unchecked((nint)0x80000000)));
                Assert.Equal((nint)0x0000000000000000, BinaryIntegerHelper<nint>.LeadingZeroCount(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void PopCountTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0x0000000000000000), BinaryIntegerHelper<nint>.PopCount(unchecked((nint)0x0000000000000000)));
                Assert.Equal(unchecked((nint)0x0000000000000001), BinaryIntegerHelper<nint>.PopCount(unchecked((nint)0x0000000000000001)));
                Assert.Equal(unchecked((nint)0x000000000000003F), BinaryIntegerHelper<nint>.PopCount(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((nint)0x0000000000000001), BinaryIntegerHelper<nint>.PopCount(unchecked((nint)0x8000000000000000)));
                Assert.Equal(unchecked((nint)0x0000000000000040), BinaryIntegerHelper<nint>.PopCount(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((nint)0x00000000, BinaryIntegerHelper<nint>.PopCount((nint)0x00000000));
                Assert.Equal((nint)0x00000001, BinaryIntegerHelper<nint>.PopCount((nint)0x00000001));
                Assert.Equal((nint)0x0000001F, BinaryIntegerHelper<nint>.PopCount((nint)0x7FFFFFFF));
                Assert.Equal((nint)0x00000001, BinaryIntegerHelper<nint>.PopCount(unchecked((nint)0x80000000)));
                Assert.Equal((nint)0x00000020, BinaryIntegerHelper<nint>.PopCount(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void RotateLeftTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0x0000000000000000), BinaryIntegerHelper<nint>.RotateLeft(unchecked((nint)0x0000000000000000), 1));
                Assert.Equal(unchecked((nint)0x0000000000000002), BinaryIntegerHelper<nint>.RotateLeft(unchecked((nint)0x0000000000000001), 1));
                Assert.Equal(unchecked((nint)0xFFFFFFFFFFFFFFFE), BinaryIntegerHelper<nint>.RotateLeft(unchecked((nint)0x7FFFFFFFFFFFFFFF), 1));
                Assert.Equal(unchecked((nint)0x0000000000000001), BinaryIntegerHelper<nint>.RotateLeft(unchecked((nint)0x8000000000000000), 1));
                Assert.Equal(unchecked((nint)0xFFFFFFFFFFFFFFFF), BinaryIntegerHelper<nint>.RotateLeft(unchecked((nint)0xFFFFFFFFFFFFFFFF), 1));
            }
            else
            {
                Assert.Equal((nint)0x00000000, BinaryIntegerHelper<nint>.RotateLeft((nint)0x00000000, 1));
                Assert.Equal((nint)0x00000002, BinaryIntegerHelper<nint>.RotateLeft((nint)0x00000001, 1));
                Assert.Equal(unchecked((nint)0xFFFFFFFE), BinaryIntegerHelper<nint>.RotateLeft((nint)0x7FFFFFFF, 1));
                Assert.Equal((nint)0x00000001, BinaryIntegerHelper<nint>.RotateLeft(unchecked((nint)0x80000000), 1));
                Assert.Equal(unchecked((nint)0xFFFFFFFF), BinaryIntegerHelper<nint>.RotateLeft(unchecked((nint)0xFFFFFFFF), 1));
            }
        }

        [Fact]
        public static void RotateRightTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0x0000000000000000), BinaryIntegerHelper<nint>.RotateRight(unchecked((nint)0x0000000000000000), 1));
                Assert.Equal(unchecked((nint)0x8000000000000000), BinaryIntegerHelper<nint>.RotateRight(unchecked((nint)0x0000000000000001), 1));
                Assert.Equal(unchecked((nint)0xBFFFFFFFFFFFFFFF), BinaryIntegerHelper<nint>.RotateRight(unchecked((nint)0x7FFFFFFFFFFFFFFF), 1));
                Assert.Equal(unchecked((nint)0x4000000000000000), BinaryIntegerHelper<nint>.RotateRight(unchecked((nint)0x8000000000000000), 1));
                Assert.Equal(unchecked((nint)0xFFFFFFFFFFFFFFFF), BinaryIntegerHelper<nint>.RotateRight(unchecked((nint)0xFFFFFFFFFFFFFFFF), 1));
            }
            else
            {
                Assert.Equal((nint)0x00000000, BinaryIntegerHelper<nint>.RotateRight((nint)0x00000000, 1));
                Assert.Equal(unchecked((nint)0x80000000), BinaryIntegerHelper<nint>.RotateRight((nint)0x00000001, 1));
                Assert.Equal(unchecked((nint)0xBFFFFFFF), BinaryIntegerHelper<nint>.RotateRight((nint)0x7FFFFFFF, 1));
                Assert.Equal((nint)0x40000000, BinaryIntegerHelper<nint>.RotateRight(unchecked((nint)0x80000000), 1));
                Assert.Equal(unchecked((nint)0xFFFFFFFF), BinaryIntegerHelper<nint>.RotateRight(unchecked((nint)0xFFFFFFFF), 1));
            }
        }

        [Fact]
        public static void TrailingZeroCountTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0x0000000000000040), BinaryIntegerHelper<nint>.TrailingZeroCount(unchecked((nint)0x0000000000000000)));
                Assert.Equal(unchecked((nint)0x0000000000000000), BinaryIntegerHelper<nint>.TrailingZeroCount(unchecked((nint)0x0000000000000001)));
                Assert.Equal(unchecked((nint)0x0000000000000000), BinaryIntegerHelper<nint>.TrailingZeroCount(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((nint)0x000000000000003F), BinaryIntegerHelper<nint>.TrailingZeroCount(unchecked((nint)0x8000000000000000)));
                Assert.Equal(unchecked((nint)0x0000000000000000), BinaryIntegerHelper<nint>.TrailingZeroCount(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((nint)0x00000020, BinaryIntegerHelper<nint>.TrailingZeroCount((nint)0x00000000));
                Assert.Equal((nint)0x00000000, BinaryIntegerHelper<nint>.TrailingZeroCount((nint)0x00000001));
                Assert.Equal((nint)0x00000000, BinaryIntegerHelper<nint>.TrailingZeroCount((nint)0x7FFFFFFF));
                Assert.Equal((nint)0x0000001F, BinaryIntegerHelper<nint>.TrailingZeroCount(unchecked((nint)0x80000000)));
                Assert.Equal((nint)0x00000000, BinaryIntegerHelper<nint>.TrailingZeroCount(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void GetByteCountTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(8, BinaryIntegerHelper<nint>.GetByteCount(unchecked((nint)0x0000000000000000)));
                Assert.Equal(8, BinaryIntegerHelper<nint>.GetByteCount(unchecked((nint)0x0000000000000001)));
                Assert.Equal(8, BinaryIntegerHelper<nint>.GetByteCount(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(8, BinaryIntegerHelper<nint>.GetByteCount(unchecked((nint)0x8000000000000000)));
                Assert.Equal(8, BinaryIntegerHelper<nint>.GetByteCount(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal(4, BinaryIntegerHelper<nint>.GetByteCount((nint)0x00000000));
                Assert.Equal(4, BinaryIntegerHelper<nint>.GetByteCount((nint)0x00000001));
                Assert.Equal(4, BinaryIntegerHelper<nint>.GetByteCount((nint)0x7FFFFFFF));
                Assert.Equal(4, BinaryIntegerHelper<nint>.GetByteCount(unchecked((nint)0x80000000)));
                Assert.Equal(4, BinaryIntegerHelper<nint>.GetByteCount(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void GetShortestBitLengthTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(0x00, BinaryIntegerHelper<nint>.GetShortestBitLength(unchecked((nint)0x0000000000000000)));
                Assert.Equal(0x01, BinaryIntegerHelper<nint>.GetShortestBitLength(unchecked((nint)0x0000000000000001)));
                Assert.Equal(0x3F, BinaryIntegerHelper<nint>.GetShortestBitLength(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(0x40, BinaryIntegerHelper<nint>.GetShortestBitLength(unchecked((nint)0x8000000000000000)));
                Assert.Equal(0x01, BinaryIntegerHelper<nint>.GetShortestBitLength(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal(0x00, BinaryIntegerHelper<nint>.GetShortestBitLength((nint)0x00000000));
                Assert.Equal(0x01, BinaryIntegerHelper<nint>.GetShortestBitLength((nint)0x00000001));
                Assert.Equal(0x1F, BinaryIntegerHelper<nint>.GetShortestBitLength((nint)0x7FFFFFFF));
                Assert.Equal(0x20, BinaryIntegerHelper<nint>.GetShortestBitLength(unchecked((nint)0x80000000)));
                Assert.Equal(0x01, BinaryIntegerHelper<nint>.GetShortestBitLength(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void TryWriteBigEndianTest()
        {
            if (Environment.Is64BitProcess)
            {
                Span<byte> destination = stackalloc byte[8];
                int bytesWritten = 0;

                Assert.True(BinaryIntegerHelper<nint>.TryWriteBigEndian(unchecked((nint)0x0000000000000000), destination, out bytesWritten));
                Assert.Equal(8, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

                Assert.True(BinaryIntegerHelper<nint>.TryWriteBigEndian(unchecked((nint)0x0000000000000001), destination, out bytesWritten));
                Assert.Equal(8, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, destination.ToArray());

                Assert.True(BinaryIntegerHelper<nint>.TryWriteBigEndian(unchecked((nint)0x7FFFFFFFFFFFFFFF), destination, out bytesWritten));
                Assert.Equal(8, bytesWritten);
                Assert.Equal(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, destination.ToArray());

                Assert.True(BinaryIntegerHelper<nint>.TryWriteBigEndian(unchecked((nint)0x8000000000000000), destination, out bytesWritten));
                Assert.Equal(8, bytesWritten);
                Assert.Equal(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

                Assert.True(BinaryIntegerHelper<nint>.TryWriteBigEndian(unchecked((nint)0xFFFFFFFFFFFFFFFF), destination, out bytesWritten));
                Assert.Equal(8, bytesWritten);
                Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, destination.ToArray());

                Assert.False(BinaryIntegerHelper<nint>.TryWriteBigEndian(default, Span<byte>.Empty, out bytesWritten));
                Assert.Equal(0, bytesWritten);
                Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, destination.ToArray());
            }
            else
            {
                Span<byte> destination = stackalloc byte[4];
                int bytesWritten = 0;

                Assert.True(BinaryIntegerHelper<nint>.TryWriteBigEndian((nint)0x00000000, destination, out bytesWritten));
                Assert.Equal(4, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

                Assert.True(BinaryIntegerHelper<nint>.TryWriteBigEndian((nint)0x00000001, destination, out bytesWritten));
                Assert.Equal(4, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x01 }, destination.ToArray());

                Assert.True(BinaryIntegerHelper<nint>.TryWriteBigEndian((nint)0x7FFFFFFF, destination, out bytesWritten));
                Assert.Equal(4, bytesWritten);
                Assert.Equal(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF }, destination.ToArray());

                Assert.True(BinaryIntegerHelper<nint>.TryWriteBigEndian(unchecked((nint)0x80000000), destination, out bytesWritten));
                Assert.Equal(4, bytesWritten);
                Assert.Equal(new byte[] { 0x80, 0x00, 0x00, 0x00 }, destination.ToArray());

                Assert.True(BinaryIntegerHelper<nint>.TryWriteBigEndian(unchecked((nint)0xFFFFFFFF), destination, out bytesWritten));
                Assert.Equal(4, bytesWritten);
                Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, destination.ToArray());

                Assert.False(BinaryIntegerHelper<nint>.TryWriteBigEndian(default, Span<byte>.Empty, out bytesWritten));
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

                Assert.True(BinaryIntegerHelper<nint>.TryWriteLittleEndian(unchecked((nint)0x0000000000000000), destination, out bytesWritten));
                Assert.Equal(8, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

                Assert.True(BinaryIntegerHelper<nint>.TryWriteLittleEndian(unchecked((nint)0x0000000000000001), destination, out bytesWritten));
                Assert.Equal(8, bytesWritten);
                Assert.Equal(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

                Assert.True(BinaryIntegerHelper<nint>.TryWriteLittleEndian(unchecked((nint)0x7FFFFFFFFFFFFFFF), destination, out bytesWritten));
                Assert.Equal(8, bytesWritten);
                Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, destination.ToArray());

                Assert.True(BinaryIntegerHelper<nint>.TryWriteLittleEndian(unchecked((nint)0x8000000000000000), destination, out bytesWritten));
                Assert.Equal(8, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, destination.ToArray());

                Assert.True(BinaryIntegerHelper<nint>.TryWriteLittleEndian(unchecked((nint)0xFFFFFFFFFFFFFFFF), destination, out bytesWritten));
                Assert.Equal(8, bytesWritten);
                Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, destination.ToArray());

                Assert.False(BinaryIntegerHelper<nint>.TryWriteLittleEndian(default, Span<byte>.Empty, out bytesWritten));
                Assert.Equal(0, bytesWritten);
                Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, destination.ToArray());
            }
            else
            {
                Span<byte> destination = stackalloc byte[4];
                int bytesWritten = 0;

                Assert.True(BinaryIntegerHelper<nint>.TryWriteLittleEndian((nint)0x00000000, destination, out bytesWritten));
                Assert.Equal(4, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

                Assert.True(BinaryIntegerHelper<nint>.TryWriteLittleEndian((nint)0x00000001, destination, out bytesWritten));
                Assert.Equal(4, bytesWritten);
                Assert.Equal(new byte[] { 0x01, 0x00, 0x00, 0x00 }, destination.ToArray());

                Assert.True(BinaryIntegerHelper<nint>.TryWriteLittleEndian((nint)0x7FFFFFFF, destination, out bytesWritten));
                Assert.Equal(4, bytesWritten);
                Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0x7F }, destination.ToArray());

                Assert.True(BinaryIntegerHelper<nint>.TryWriteLittleEndian(unchecked((nint)0x80000000), destination, out bytesWritten));
                Assert.Equal(4, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x80 }, destination.ToArray());

                Assert.True(BinaryIntegerHelper<nint>.TryWriteLittleEndian(unchecked((nint)0xFFFFFFFF), destination, out bytesWritten));
                Assert.Equal(4, bytesWritten);
                Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, destination.ToArray());

                Assert.False(BinaryIntegerHelper<nint>.TryWriteLittleEndian(default, Span<byte>.Empty, out bytesWritten));
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
                Assert.False(BinaryNumberHelper<nint>.IsPow2(unchecked((nint)0x0000000000000000)));
                Assert.True(BinaryNumberHelper<nint>.IsPow2(unchecked((nint)0x0000000000000001)));
                Assert.False(BinaryNumberHelper<nint>.IsPow2(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.False(BinaryNumberHelper<nint>.IsPow2(unchecked((nint)0x8000000000000000)));
                Assert.False(BinaryNumberHelper<nint>.IsPow2(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.False(BinaryNumberHelper<nint>.IsPow2((nint)0x00000000));
                Assert.True(BinaryNumberHelper<nint>.IsPow2((nint)0x00000001));
                Assert.False(BinaryNumberHelper<nint>.IsPow2((nint)0x7FFFFFFF));
                Assert.False(BinaryNumberHelper<nint>.IsPow2(unchecked((nint)0x80000000)));
                Assert.False(BinaryNumberHelper<nint>.IsPow2(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void Log2Test()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0x0000000000000000), BinaryNumberHelper<nint>.Log2(unchecked((nint)0x0000000000000000)));
                Assert.Equal(unchecked((nint)0x0000000000000000), BinaryNumberHelper<nint>.Log2(unchecked((nint)0x0000000000000001)));
                Assert.Equal(unchecked((nint)0x000000000000003E), BinaryNumberHelper<nint>.Log2(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Throws<ArgumentOutOfRangeException>(() => BinaryNumberHelper<nint>.Log2(unchecked((nint)0x8000000000000000)));
                Assert.Throws<ArgumentOutOfRangeException>(() => BinaryNumberHelper<nint>.Log2(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((nint)0x00000000, BinaryNumberHelper<nint>.Log2((nint)0x00000000));
                Assert.Equal((nint)0x00000000, BinaryNumberHelper<nint>.Log2((nint)0x00000001));
                Assert.Equal((nint)0x0000001E, BinaryNumberHelper<nint>.Log2((nint)0x7FFFFFFF));
                Assert.Throws<ArgumentOutOfRangeException>(() => BinaryNumberHelper<nint>.Log2(unchecked((nint)0x80000000)));
                Assert.Throws<ArgumentOutOfRangeException>(() => BinaryNumberHelper<nint>.Log2(unchecked((nint)0xFFFFFFFF)));
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
                Assert.Equal(unchecked((nint)0x0000000000000000), BitwiseOperatorsHelper<nint, nint, nint>.op_BitwiseAnd(unchecked((nint)0x0000000000000000), (nint)1));
                Assert.Equal(unchecked((nint)0x0000000000000001), BitwiseOperatorsHelper<nint, nint, nint>.op_BitwiseAnd(unchecked((nint)0x0000000000000001), (nint)1));
                Assert.Equal(unchecked((nint)0x0000000000000001), BitwiseOperatorsHelper<nint, nint, nint>.op_BitwiseAnd(unchecked((nint)0x7FFFFFFFFFFFFFFF), (nint)1));
                Assert.Equal(unchecked((nint)0x0000000000000000), BitwiseOperatorsHelper<nint, nint, nint>.op_BitwiseAnd(unchecked((nint)0x8000000000000000), (nint)1));
                Assert.Equal(unchecked((nint)0x0000000000000001), BitwiseOperatorsHelper<nint, nint, nint>.op_BitwiseAnd(unchecked((nint)0xFFFFFFFFFFFFFFFF), (nint)1));
            }
            else
            {
                Assert.Equal((nint)0x00000000, BitwiseOperatorsHelper<nint, nint, nint>.op_BitwiseAnd((nint)0x00000000, (nint)1));
                Assert.Equal((nint)0x00000001, BitwiseOperatorsHelper<nint, nint, nint>.op_BitwiseAnd((nint)0x00000001, (nint)1));
                Assert.Equal((nint)0x00000001, BitwiseOperatorsHelper<nint, nint, nint>.op_BitwiseAnd((nint)0x7FFFFFFF, (nint)1));
                Assert.Equal((nint)0x00000000, BitwiseOperatorsHelper<nint, nint, nint>.op_BitwiseAnd(unchecked((nint)0x80000000), (nint)1));
                Assert.Equal((nint)0x00000001, BitwiseOperatorsHelper<nint, nint, nint>.op_BitwiseAnd(unchecked((nint)0xFFFFFFFF), (nint)1));
            }
        }

        [Fact]
        public static void op_BitwiseOrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0x0000000000000001), BitwiseOperatorsHelper<nint, nint, nint>.op_BitwiseOr(unchecked((nint)0x0000000000000000), (nint)1));
                Assert.Equal(unchecked((nint)0x0000000000000001), BitwiseOperatorsHelper<nint, nint, nint>.op_BitwiseOr(unchecked((nint)0x0000000000000001), (nint)1));
                Assert.Equal(unchecked((nint)0x7FFFFFFFFFFFFFFF), BitwiseOperatorsHelper<nint, nint, nint>.op_BitwiseOr(unchecked((nint)0x7FFFFFFFFFFFFFFF), (nint)1));
                Assert.Equal(unchecked((nint)0x8000000000000001), BitwiseOperatorsHelper<nint, nint, nint>.op_BitwiseOr(unchecked((nint)0x8000000000000000), (nint)1));
                Assert.Equal(unchecked((nint)0xFFFFFFFFFFFFFFFF), BitwiseOperatorsHelper<nint, nint, nint>.op_BitwiseOr(unchecked((nint)0xFFFFFFFFFFFFFFFF), (nint)1));
            }
            else
            {
                Assert.Equal((nint)0x00000001, BitwiseOperatorsHelper<nint, nint, nint>.op_BitwiseOr((nint)0x00000000, (nint)1));
                Assert.Equal((nint)0x00000001, BitwiseOperatorsHelper<nint, nint, nint>.op_BitwiseOr((nint)0x00000001, (nint)1));
                Assert.Equal((nint)0x7FFFFFFF, BitwiseOperatorsHelper<nint, nint, nint>.op_BitwiseOr((nint)0x7FFFFFFF, (nint)1));
                Assert.Equal(unchecked((nint)0x80000001), BitwiseOperatorsHelper<nint, nint, nint>.op_BitwiseOr(unchecked((nint)0x80000000), (nint)1));
                Assert.Equal(unchecked((nint)0xFFFFFFFF), BitwiseOperatorsHelper<nint, nint, nint>.op_BitwiseOr(unchecked((nint)0xFFFFFFFF), (nint)1));
            }
        }

        [Fact]
        public static void op_ExclusiveOrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0x0000000000000001), BitwiseOperatorsHelper<nint, nint, nint>.op_ExclusiveOr(unchecked((nint)0x0000000000000000), (nint)1));
                Assert.Equal(unchecked((nint)0x0000000000000000), BitwiseOperatorsHelper<nint, nint, nint>.op_ExclusiveOr(unchecked((nint)0x0000000000000001), (nint)1));
                Assert.Equal(unchecked((nint)0x7FFFFFFFFFFFFFFE), BitwiseOperatorsHelper<nint, nint, nint>.op_ExclusiveOr(unchecked((nint)0x7FFFFFFFFFFFFFFF), (nint)1));
                Assert.Equal(unchecked((nint)0x8000000000000001), BitwiseOperatorsHelper<nint, nint, nint>.op_ExclusiveOr(unchecked((nint)0x8000000000000000), (nint)1));
                Assert.Equal(unchecked((nint)0xFFFFFFFFFFFFFFFE), BitwiseOperatorsHelper<nint, nint, nint>.op_ExclusiveOr(unchecked((nint)0xFFFFFFFFFFFFFFFF), (nint)1));
            }
            else
            {
                Assert.Equal((nint)0x00000001, BitwiseOperatorsHelper<nint, nint, nint>.op_ExclusiveOr((nint)0x00000000, (nint)1));
                Assert.Equal((nint)0x00000000, BitwiseOperatorsHelper<nint, nint, nint>.op_ExclusiveOr((nint)0x00000001, (nint)1));
                Assert.Equal((nint)0x7FFFFFFE, BitwiseOperatorsHelper<nint, nint, nint>.op_ExclusiveOr((nint)0x7FFFFFFF, (nint)1));
                Assert.Equal(unchecked((nint)0x80000001), BitwiseOperatorsHelper<nint, nint, nint>.op_ExclusiveOr(unchecked((nint)0x80000000), (nint)1));
                Assert.Equal(unchecked((nint)0xFFFFFFFE), BitwiseOperatorsHelper<nint, nint, nint>.op_ExclusiveOr(unchecked((nint)0xFFFFFFFF), (nint)1));
            }
        }

        [Fact]
        public static void op_OnesComplementTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0xFFFFFFFFFFFFFFFF), BitwiseOperatorsHelper<nint, nint, nint>.op_OnesComplement(unchecked((nint)0x0000000000000000)));
                Assert.Equal(unchecked((nint)0xFFFFFFFFFFFFFFFE), BitwiseOperatorsHelper<nint, nint, nint>.op_OnesComplement(unchecked((nint)0x0000000000000001)));
                Assert.Equal(unchecked((nint)0x8000000000000000), BitwiseOperatorsHelper<nint, nint, nint>.op_OnesComplement(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((nint)0x7FFFFFFFFFFFFFFF), BitwiseOperatorsHelper<nint, nint, nint>.op_OnesComplement(unchecked((nint)0x8000000000000000)));
                Assert.Equal(unchecked((nint)0x0000000000000000), BitwiseOperatorsHelper<nint, nint, nint>.op_OnesComplement(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal(unchecked((nint)0xFFFFFFFF), BitwiseOperatorsHelper<nint, nint, nint>.op_OnesComplement((nint)0x00000000));
                Assert.Equal(unchecked((nint)0xFFFFFFFE), BitwiseOperatorsHelper<nint, nint, nint>.op_OnesComplement((nint)0x00000001));
                Assert.Equal(unchecked((nint)0x80000000), BitwiseOperatorsHelper<nint, nint, nint>.op_OnesComplement((nint)0x7FFFFFFF));
                Assert.Equal((nint)0x7FFFFFFF, BitwiseOperatorsHelper<nint, nint, nint>.op_OnesComplement(unchecked((nint)0x80000000)));
                Assert.Equal((nint)0x00000000, BitwiseOperatorsHelper<nint, nint, nint>.op_OnesComplement(unchecked((nint)0xFFFFFFFF)));
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
                Assert.False(ComparisonOperatorsHelper<nint, nint>.op_GreaterThan(unchecked((nint)0x0000000000000000), (nint)1));
                Assert.False(ComparisonOperatorsHelper<nint, nint>.op_GreaterThan(unchecked((nint)0x0000000000000001), (nint)1));
                Assert.True(ComparisonOperatorsHelper<nint, nint>.op_GreaterThan(unchecked((nint)0x7FFFFFFFFFFFFFFF), (nint)1));
                Assert.False(ComparisonOperatorsHelper<nint, nint>.op_GreaterThan(unchecked((nint)0x8000000000000000), (nint)1));
                Assert.False(ComparisonOperatorsHelper<nint, nint>.op_GreaterThan(unchecked((nint)0xFFFFFFFFFFFFFFFF), (nint)1));
            }
            else
            {
                Assert.False(ComparisonOperatorsHelper<nint, nint>.op_GreaterThan((nint)0x00000000, (nint)1));
                Assert.False(ComparisonOperatorsHelper<nint, nint>.op_GreaterThan((nint)0x00000001, (nint)1));
                Assert.True(ComparisonOperatorsHelper<nint, nint>.op_GreaterThan((nint)0x7FFFFFFF, (nint)1));
                Assert.False(ComparisonOperatorsHelper<nint, nint>.op_GreaterThan(unchecked((nint)0x80000000), (nint)1));
                Assert.False(ComparisonOperatorsHelper<nint, nint>.op_GreaterThan(unchecked((nint)0xFFFFFFFF), (nint)1));
            }
        }

        [Fact]
        public static void op_GreaterThanOrEqualTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.False(ComparisonOperatorsHelper<nint, nint>.op_GreaterThanOrEqual(unchecked((nint)0x0000000000000000), (nint)1));
                Assert.True(ComparisonOperatorsHelper<nint, nint>.op_GreaterThanOrEqual(unchecked((nint)0x0000000000000001), (nint)1));
                Assert.True(ComparisonOperatorsHelper<nint, nint>.op_GreaterThanOrEqual(unchecked((nint)0x7FFFFFFFFFFFFFFF), (nint)1));
                Assert.False(ComparisonOperatorsHelper<nint, nint>.op_GreaterThanOrEqual(unchecked((nint)0x8000000000000000), (nint)1));
                Assert.False(ComparisonOperatorsHelper<nint, nint>.op_GreaterThanOrEqual(unchecked((nint)0xFFFFFFFFFFFFFFFF), (nint)1));
            }
            else
            {
                Assert.False(ComparisonOperatorsHelper<nint, nint>.op_GreaterThanOrEqual((nint)0x00000000, (nint)1));
                Assert.True(ComparisonOperatorsHelper<nint, nint>.op_GreaterThanOrEqual((nint)0x00000001, (nint)1));
                Assert.True(ComparisonOperatorsHelper<nint, nint>.op_GreaterThanOrEqual((nint)0x7FFFFFFF, (nint)1));
                Assert.False(ComparisonOperatorsHelper<nint, nint>.op_GreaterThanOrEqual(unchecked((nint)0x80000000), (nint)1));
                Assert.False(ComparisonOperatorsHelper<nint, nint>.op_GreaterThanOrEqual(unchecked((nint)0xFFFFFFFF), (nint)1));
            }
        }

        [Fact]
        public static void op_LessThanTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.True(ComparisonOperatorsHelper<nint, nint>.op_LessThan(unchecked((nint)0x0000000000000000), (nint)1));
                Assert.False(ComparisonOperatorsHelper<nint, nint>.op_LessThan(unchecked((nint)0x0000000000000001), (nint)1));
                Assert.False(ComparisonOperatorsHelper<nint, nint>.op_LessThan(unchecked((nint)0x7FFFFFFFFFFFFFFF), (nint)1));
                Assert.True(ComparisonOperatorsHelper<nint, nint>.op_LessThan(unchecked((nint)0x8000000000000000), (nint)1));
                Assert.True(ComparisonOperatorsHelper<nint, nint>.op_LessThan(unchecked((nint)0xFFFFFFFFFFFFFFFF), (nint)1));
            }
            else
            {
                Assert.True(ComparisonOperatorsHelper<nint, nint>.op_LessThan((nint)0x00000000, (nint)1));
                Assert.False(ComparisonOperatorsHelper<nint, nint>.op_LessThan((nint)0x00000001, (nint)1));
                Assert.False(ComparisonOperatorsHelper<nint, nint>.op_LessThan((nint)0x7FFFFFFF, (nint)1));
                Assert.True(ComparisonOperatorsHelper<nint, nint>.op_LessThan(unchecked((nint)0x80000000), (nint)1));
                Assert.True(ComparisonOperatorsHelper<nint, nint>.op_LessThan(unchecked((nint)0xFFFFFFFF), (nint)1));
            }
        }

        [Fact]
        public static void op_LessThanOrEqualTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.True(ComparisonOperatorsHelper<nint, nint>.op_LessThanOrEqual(unchecked((nint)0x0000000000000000), (nint)1));
                Assert.True(ComparisonOperatorsHelper<nint, nint>.op_LessThanOrEqual(unchecked((nint)0x0000000000000001), (nint)1));
                Assert.False(ComparisonOperatorsHelper<nint, nint>.op_LessThanOrEqual(unchecked((nint)0x7FFFFFFFFFFFFFFF), (nint)1));
                Assert.True(ComparisonOperatorsHelper<nint, nint>.op_LessThanOrEqual(unchecked((nint)0x8000000000000000), (nint)1));
                Assert.True(ComparisonOperatorsHelper<nint, nint>.op_LessThanOrEqual(unchecked((nint)0xFFFFFFFFFFFFFFFF), (nint)1));
            }
            else
            {
                Assert.True(ComparisonOperatorsHelper<nint, nint>.op_LessThanOrEqual((nint)0x00000000, (nint)1));
                Assert.True(ComparisonOperatorsHelper<nint, nint>.op_LessThanOrEqual((nint)0x00000001, (nint)1));
                Assert.False(ComparisonOperatorsHelper<nint, nint>.op_LessThanOrEqual((nint)0x7FFFFFFF, (nint)1));
                Assert.True(ComparisonOperatorsHelper<nint, nint>.op_LessThanOrEqual(unchecked((nint)0x80000000), (nint)1));
                Assert.True(ComparisonOperatorsHelper<nint, nint>.op_LessThanOrEqual(unchecked((nint)0xFFFFFFFF), (nint)1));
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
                Assert.Equal(unchecked((nint)0xFFFFFFFFFFFFFFFF), DecrementOperatorsHelper<nint>.op_Decrement(unchecked((nint)0x0000000000000000)));
                Assert.Equal(unchecked((nint)0x0000000000000000), DecrementOperatorsHelper<nint>.op_Decrement(unchecked((nint)0x0000000000000001)));
                Assert.Equal(unchecked((nint)0x7FFFFFFFFFFFFFFE), DecrementOperatorsHelper<nint>.op_Decrement(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((nint)0x7FFFFFFFFFFFFFFF), DecrementOperatorsHelper<nint>.op_Decrement(unchecked((nint)0x8000000000000000)));
                Assert.Equal(unchecked((nint)0xFFFFFFFFFFFFFFFE), DecrementOperatorsHelper<nint>.op_Decrement(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal(unchecked((nint)0xFFFFFFFF), DecrementOperatorsHelper<nint>.op_Decrement((nint)0x00000000));
                Assert.Equal((nint)0x00000000, DecrementOperatorsHelper<nint>.op_Decrement((nint)0x00000001));
                Assert.Equal((nint)0x7FFFFFFE, DecrementOperatorsHelper<nint>.op_Decrement((nint)0x7FFFFFFF));
                Assert.Equal((nint)0x7FFFFFFF, DecrementOperatorsHelper<nint>.op_Decrement(unchecked((nint)0x80000000)));
                Assert.Equal(unchecked((nint)0xFFFFFFFE), DecrementOperatorsHelper<nint>.op_Decrement(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void op_CheckedDecrementTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0xFFFFFFFFFFFFFFFF), DecrementOperatorsHelper<nint>.op_CheckedDecrement(unchecked((nint)0x0000000000000000)));
                Assert.Equal(unchecked((nint)0x0000000000000000), DecrementOperatorsHelper<nint>.op_CheckedDecrement(unchecked((nint)0x0000000000000001)));
                Assert.Equal(unchecked((nint)0x7FFFFFFFFFFFFFFE), DecrementOperatorsHelper<nint>.op_CheckedDecrement(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((nint)0xFFFFFFFFFFFFFFFE), DecrementOperatorsHelper<nint>.op_CheckedDecrement(unchecked((nint)0xFFFFFFFFFFFFFFFF)));

                Assert.Throws<OverflowException>(() => DecrementOperatorsHelper<nint>.op_CheckedDecrement(unchecked((nint)0x8000000000000000)));
            }
            else
            {
                Assert.Equal(unchecked((nint)0xFFFFFFFF), DecrementOperatorsHelper<nint>.op_CheckedDecrement((nint)0x00000000));
                Assert.Equal((nint)0x00000000, DecrementOperatorsHelper<nint>.op_CheckedDecrement((nint)0x00000001));
                Assert.Equal((nint)0x7FFFFFFE, DecrementOperatorsHelper<nint>.op_CheckedDecrement((nint)0x7FFFFFFF));
                Assert.Equal(unchecked((nint)0xFFFFFFFE), DecrementOperatorsHelper<nint>.op_CheckedDecrement(unchecked((nint)0xFFFFFFFF)));

                Assert.Throws<OverflowException>(() => DecrementOperatorsHelper<nint>.op_CheckedDecrement(unchecked((nint)0x80000000)));
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
                Assert.Equal(unchecked((nint)0x0000000000000000), DivisionOperatorsHelper<nint, nint, nint>.op_Division(unchecked((nint)0x0000000000000000), (nint)2));
                Assert.Equal(unchecked((nint)0x0000000000000000), DivisionOperatorsHelper<nint, nint, nint>.op_Division(unchecked((nint)0x0000000000000001), (nint)2));
                Assert.Equal(unchecked((nint)0x3FFFFFFFFFFFFFFF), DivisionOperatorsHelper<nint, nint, nint>.op_Division(unchecked((nint)0x7FFFFFFFFFFFFFFF), (nint)2));
                Assert.Equal(unchecked((nint)0xC000000000000000), DivisionOperatorsHelper<nint, nint, nint>.op_Division(unchecked((nint)0x8000000000000000), (nint)2));
                Assert.Equal(unchecked((nint)0x0000000000000000), DivisionOperatorsHelper<nint, nint, nint>.op_Division(unchecked((nint)0xFFFFFFFFFFFFFFFF), (nint)2));

                Assert.Throws<DivideByZeroException>(() => DivisionOperatorsHelper<nint, nint, nint>.op_Division(unchecked((nint)0x0000000000000001), (nint)0));
            }
            else
            {
                Assert.Equal((nint)0x00000000, DivisionOperatorsHelper<nint, nint, nint>.op_Division((nint)0x00000000, (nint)2));
                Assert.Equal((nint)0x00000000, DivisionOperatorsHelper<nint, nint, nint>.op_Division((nint)0x00000001, (nint)2));
                Assert.Equal((nint)0x3FFFFFFF, DivisionOperatorsHelper<nint, nint, nint>.op_Division((nint)0x7FFFFFFF, (nint)2));
                Assert.Equal(unchecked((nint)0xC0000000), DivisionOperatorsHelper<nint, nint, nint>.op_Division(unchecked((nint)0x80000000), (nint)2));
                Assert.Equal((nint)0x00000000, DivisionOperatorsHelper<nint, nint, nint>.op_Division(unchecked((nint)0xFFFFFFFF), (nint)2));

                Assert.Throws<DivideByZeroException>(() => DivisionOperatorsHelper<nint, nint, nint>.op_Division((nint)0x00000001, (nint)0));
            }
        }

        [Fact]
        public static void op_CheckedDivisionTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0x0000000000000000), DivisionOperatorsHelper<nint, nint, nint>.op_CheckedDivision(unchecked((nint)0x0000000000000000), (nint)2));
                Assert.Equal(unchecked((nint)0x0000000000000000), DivisionOperatorsHelper<nint, nint, nint>.op_CheckedDivision(unchecked((nint)0x0000000000000001), (nint)2));
                Assert.Equal(unchecked((nint)0x3FFFFFFFFFFFFFFF), DivisionOperatorsHelper<nint, nint, nint>.op_CheckedDivision(unchecked((nint)0x7FFFFFFFFFFFFFFF), (nint)2));
                Assert.Equal(unchecked((nint)0xC000000000000000), DivisionOperatorsHelper<nint, nint, nint>.op_CheckedDivision(unchecked((nint)0x8000000000000000), (nint)2));
                Assert.Equal(unchecked((nint)0x0000000000000000), DivisionOperatorsHelper<nint, nint, nint>.op_CheckedDivision(unchecked((nint)0xFFFFFFFFFFFFFFFF), (nint)2));

                Assert.Throws<DivideByZeroException>(() => DivisionOperatorsHelper<nint, nint, nint>.op_CheckedDivision(unchecked((nint)0x0000000000000001), (nint)0));
            }
            else
            {
                Assert.Equal((nint)0x00000000, DivisionOperatorsHelper<nint, nint, nint>.op_CheckedDivision((nint)0x00000000, (nint)2));
                Assert.Equal((nint)0x00000000, DivisionOperatorsHelper<nint, nint, nint>.op_CheckedDivision((nint)0x00000001, (nint)2));
                Assert.Equal((nint)0x3FFFFFFF, DivisionOperatorsHelper<nint, nint, nint>.op_CheckedDivision((nint)0x7FFFFFFF, (nint)2));
                Assert.Equal(unchecked((nint)0xC0000000), DivisionOperatorsHelper<nint, nint, nint>.op_CheckedDivision(unchecked((nint)0x80000000), (nint)2));
                Assert.Equal((nint)0x00000000, DivisionOperatorsHelper<nint, nint, nint>.op_CheckedDivision(unchecked((nint)0xFFFFFFFF), (nint)2));

                Assert.Throws<DivideByZeroException>(() => DivisionOperatorsHelper<nint, nint, nint>.op_CheckedDivision((nint)0x00000001, (nint)0));
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
                Assert.False(EqualityOperatorsHelper<nint, nint>.op_Equality(unchecked((nint)0x0000000000000000), (nint)1));
                Assert.True(EqualityOperatorsHelper<nint, nint>.op_Equality(unchecked((nint)0x0000000000000001), (nint)1));
                Assert.False(EqualityOperatorsHelper<nint, nint>.op_Equality(unchecked((nint)0x7FFFFFFFFFFFFFFF), (nint)1));
                Assert.False(EqualityOperatorsHelper<nint, nint>.op_Equality(unchecked((nint)0x8000000000000000), (nint)1));
                Assert.False(EqualityOperatorsHelper<nint, nint>.op_Equality(unchecked((nint)0xFFFFFFFFFFFFFFFF), (nint)1));
            }
            else
            {
                Assert.False(EqualityOperatorsHelper<nint, nint>.op_Equality((nint)0x00000000, (nint)1));
                Assert.True(EqualityOperatorsHelper<nint, nint>.op_Equality((nint)0x00000001, (nint)1));
                Assert.False(EqualityOperatorsHelper<nint, nint>.op_Equality((nint)0x7FFFFFFF, (nint)1));
                Assert.False(EqualityOperatorsHelper<nint, nint>.op_Equality(unchecked((nint)0x80000000), (nint)1));
                Assert.False(EqualityOperatorsHelper<nint, nint>.op_Equality(unchecked((nint)0xFFFFFFFF), (nint)1));
            }
        }

        [Fact]
        public static void op_InequalityTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.True(EqualityOperatorsHelper<nint, nint>.op_Inequality(unchecked((nint)0x0000000000000000), (nint)1));
                Assert.False(EqualityOperatorsHelper<nint, nint>.op_Inequality(unchecked((nint)0x0000000000000001), (nint)1));
                Assert.True(EqualityOperatorsHelper<nint, nint>.op_Inequality(unchecked((nint)0x7FFFFFFFFFFFFFFF), (nint)1));
                Assert.True(EqualityOperatorsHelper<nint, nint>.op_Inequality(unchecked((nint)0x8000000000000000), (nint)1));
                Assert.True(EqualityOperatorsHelper<nint, nint>.op_Inequality(unchecked((nint)0xFFFFFFFFFFFFFFFF), (nint)1));
            }
            else
            {
                Assert.True(EqualityOperatorsHelper<nint, nint>.op_Inequality((nint)0x00000000, (nint)1));
                Assert.False(EqualityOperatorsHelper<nint, nint>.op_Inequality((nint)0x00000001, (nint)1));
                Assert.True(EqualityOperatorsHelper<nint, nint>.op_Inequality((nint)0x7FFFFFFF, (nint)1));
                Assert.True(EqualityOperatorsHelper<nint, nint>.op_Inequality(unchecked((nint)0x80000000), (nint)1));
                Assert.True(EqualityOperatorsHelper<nint, nint>.op_Inequality(unchecked((nint)0xFFFFFFFF), (nint)1));
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
                Assert.Equal(unchecked((nint)0x0000000000000001), IncrementOperatorsHelper<nint>.op_Increment(unchecked((nint)0x0000000000000000)));
                Assert.Equal(unchecked((nint)0x0000000000000002), IncrementOperatorsHelper<nint>.op_Increment(unchecked((nint)0x0000000000000001)));
                Assert.Equal(unchecked((nint)0x8000000000000000), IncrementOperatorsHelper<nint>.op_Increment(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((nint)0x8000000000000001), IncrementOperatorsHelper<nint>.op_Increment(unchecked((nint)0x8000000000000000)));
                Assert.Equal(unchecked((nint)0x0000000000000000), IncrementOperatorsHelper<nint>.op_Increment(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((nint)0x00000001, IncrementOperatorsHelper<nint>.op_Increment((nint)0x00000000));
                Assert.Equal((nint)0x00000002, IncrementOperatorsHelper<nint>.op_Increment((nint)0x00000001));
                Assert.Equal(unchecked((nint)0x80000000), IncrementOperatorsHelper<nint>.op_Increment((nint)0x7FFFFFFF));
                Assert.Equal(unchecked((nint)0x80000001), IncrementOperatorsHelper<nint>.op_Increment(unchecked((nint)0x80000000)));
                Assert.Equal((nint)0x00000000, IncrementOperatorsHelper<nint>.op_Increment(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void op_CheckedIncrementTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0x0000000000000001), IncrementOperatorsHelper<nint>.op_CheckedIncrement(unchecked((nint)0x0000000000000000)));
                Assert.Equal(unchecked((nint)0x0000000000000002), IncrementOperatorsHelper<nint>.op_CheckedIncrement(unchecked((nint)0x0000000000000001)));
                Assert.Equal(unchecked((nint)0x8000000000000001), IncrementOperatorsHelper<nint>.op_CheckedIncrement(unchecked((nint)0x8000000000000000)));
                Assert.Equal(unchecked((nint)0x0000000000000000), IncrementOperatorsHelper<nint>.op_CheckedIncrement(unchecked((nint)0xFFFFFFFFFFFFFFFF)));

                Assert.Throws<OverflowException>(() => IncrementOperatorsHelper<nint>.op_CheckedIncrement(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                
            }
            else
            {
                Assert.Equal((nint)0x00000001, IncrementOperatorsHelper<nint>.op_CheckedIncrement((nint)0x00000000));
                Assert.Equal((nint)0x00000002, IncrementOperatorsHelper<nint>.op_CheckedIncrement((nint)0x00000001));
                Assert.Equal(unchecked((nint)0x80000001), IncrementOperatorsHelper<nint>.op_CheckedIncrement(unchecked((nint)0x80000000)));
                Assert.Equal((nint)0x00000000, IncrementOperatorsHelper<nint>.op_CheckedIncrement(unchecked((nint)0xFFFFFFFF)));

                Assert.Throws<OverflowException>(() => IncrementOperatorsHelper<nint>.op_CheckedIncrement((nint)0x7FFFFFFF));
            }
        }

        //
        // IMinMagnitudeMaxValue
        //

        [Fact]
        public static void MaxValueTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0x7FFFFFFFFFFFFFFF), MinMaxValueHelper<nint>.MaxValue);
            }
            else
            {
                Assert.Equal((nint)0x7FFFFFFF, MinMaxValueHelper<nint>.MaxValue);
            }
        }

        [Fact]
        public static void MinValueTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0x8000000000000000), MinMaxValueHelper<nint>.MinValue);
            }
            else
            {
                Assert.Equal(unchecked((nint)0x80000000), MinMaxValueHelper<nint>.MinValue);
            }
        }

        //
        // IModulusOperators
        //

        [Fact]
        public static void op_ModulusTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0x0000000000000000), ModulusOperatorsHelper<nint, nint, nint>.op_Modulus(unchecked((nint)0x0000000000000000), (nint)2));
                Assert.Equal(unchecked((nint)0x0000000000000001), ModulusOperatorsHelper<nint, nint, nint>.op_Modulus(unchecked((nint)0x0000000000000001), (nint)2));
                Assert.Equal(unchecked((nint)0x0000000000000001), ModulusOperatorsHelper<nint, nint, nint>.op_Modulus(unchecked((nint)0x7FFFFFFFFFFFFFFF), (nint)2));
                Assert.Equal(unchecked((nint)0x0000000000000000), ModulusOperatorsHelper<nint, nint, nint>.op_Modulus(unchecked((nint)0x8000000000000000), (nint)2));
                Assert.Equal(unchecked((nint)0xFFFFFFFFFFFFFFFF), ModulusOperatorsHelper<nint, nint, nint>.op_Modulus(unchecked((nint)0xFFFFFFFFFFFFFFFF), (nint)2));

                Assert.Throws<DivideByZeroException>(() => ModulusOperatorsHelper<nint, nint, nint>.op_Modulus(unchecked((nint)0x0000000000000001), (nint)0));
            }
            else
            {
                Assert.Equal((nint)0x00000000, ModulusOperatorsHelper<nint, nint, nint>.op_Modulus((nint)0x00000000, (nint)2));
                Assert.Equal((nint)0x00000001, ModulusOperatorsHelper<nint, nint, nint>.op_Modulus((nint)0x00000001, (nint)2));
                Assert.Equal((nint)0x00000001, ModulusOperatorsHelper<nint, nint, nint>.op_Modulus((nint)0x7FFFFFFF, (nint)2));
                Assert.Equal((nint)0x00000000, ModulusOperatorsHelper<nint, nint, nint>.op_Modulus(unchecked((nint)0x80000000), (nint)2));
                Assert.Equal(unchecked((nint)0xFFFFFFFF), ModulusOperatorsHelper<nint, nint, nint>.op_Modulus(unchecked((nint)0xFFFFFFFF), (nint)2));

                Assert.Throws<DivideByZeroException>(() => ModulusOperatorsHelper<nint, nint, nint>.op_Modulus((nint)0x00000001, (nint)0));
            }
        }

        //
        // IMultiplicativeIdentity
        //

        [Fact]
        public static void MultiplicativeIdentityTest()
        {
            Assert.Equal((nint)0x00000001, MultiplicativeIdentityHelper<nint, nint>.MultiplicativeIdentity);
        }

        //
        // IMultiplyOperators
        //

        [Fact]
        public static void op_MultiplyTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0x0000000000000000), MultiplyOperatorsHelper<nint, nint, nint>.op_Multiply(unchecked((nint)0x0000000000000000), (nint)2));
                Assert.Equal(unchecked((nint)0x0000000000000002), MultiplyOperatorsHelper<nint, nint, nint>.op_Multiply(unchecked((nint)0x0000000000000001), (nint)2));
                Assert.Equal(unchecked((nint)0xFFFFFFFFFFFFFFFE), MultiplyOperatorsHelper<nint, nint, nint>.op_Multiply(unchecked((nint)0x7FFFFFFFFFFFFFFF), (nint)2));
                Assert.Equal(unchecked((nint)0x0000000000000000), MultiplyOperatorsHelper<nint, nint, nint>.op_Multiply(unchecked((nint)0x8000000000000000), (nint)2));
                Assert.Equal(unchecked((nint)0xFFFFFFFFFFFFFFFE), MultiplyOperatorsHelper<nint, nint, nint>.op_Multiply(unchecked((nint)0xFFFFFFFFFFFFFFFF), (nint)2));
            }
            else
            {
                Assert.Equal((nint)0x00000000, MultiplyOperatorsHelper<nint, nint, nint>.op_Multiply((nint)0x00000000, (nint)2));
                Assert.Equal((nint)0x00000002, MultiplyOperatorsHelper<nint, nint, nint>.op_Multiply((nint)0x00000001, (nint)2));
                Assert.Equal(unchecked((nint)0xFFFFFFFE), MultiplyOperatorsHelper<nint, nint, nint>.op_Multiply((nint)0x7FFFFFFF, (nint)2));
                Assert.Equal((nint)0x00000000, MultiplyOperatorsHelper<nint, nint, nint>.op_Multiply(unchecked((nint)0x80000000), (nint)2));
                Assert.Equal(unchecked((nint)0xFFFFFFFE), MultiplyOperatorsHelper<nint, nint, nint>.op_Multiply(unchecked((nint)0xFFFFFFFF), (nint)2));
            }
        }

        [Fact]
        public static void op_CheckedMultiplyTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0x0000000000000000), MultiplyOperatorsHelper<nint, nint, nint>.op_CheckedMultiply(unchecked((nint)0x0000000000000000), (nint)2));
                Assert.Equal(unchecked((nint)0x0000000000000002), MultiplyOperatorsHelper<nint, nint, nint>.op_CheckedMultiply(unchecked((nint)0x0000000000000001), (nint)2));
                Assert.Equal(unchecked((nint)0xFFFFFFFFFFFFFFFE), MultiplyOperatorsHelper<nint, nint, nint>.op_CheckedMultiply(unchecked((nint)0xFFFFFFFFFFFFFFFF), (nint)2));

                Assert.Throws<OverflowException>(() => MultiplyOperatorsHelper<nint, nint, nint>.op_CheckedMultiply(unchecked((nint)0x7FFFFFFFFFFFFFFF), (nint)2));
                Assert.Throws<OverflowException>(() => MultiplyOperatorsHelper<nint, nint, nint>.op_CheckedMultiply(unchecked((nint)0x8000000000000000), (nint)2));
            }
            else
            {
                Assert.Equal((nint)0x00000000, MultiplyOperatorsHelper<nint, nint, nint>.op_CheckedMultiply((nint)0x00000000, (nint)2));
                Assert.Equal((nint)0x00000002, MultiplyOperatorsHelper<nint, nint, nint>.op_CheckedMultiply((nint)0x00000001, (nint)2));
                Assert.Equal(unchecked((nint)0xFFFFFFFE), MultiplyOperatorsHelper<nint, nint, nint>.op_CheckedMultiply(unchecked((nint)0xFFFFFFFF), (nint)2));

                Assert.Throws<OverflowException>(() => MultiplyOperatorsHelper<nint, nint, nint>.op_CheckedMultiply((nint)0x7FFFFFFF, (nint)2));
                Assert.Throws<OverflowException>(() => MultiplyOperatorsHelper<nint, nint, nint>.op_CheckedMultiply(unchecked((nint)0x80000000), (nint)2));
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
                Assert.Equal(unchecked((nint)0x0000000000000000), NumberHelper<nint>.Clamp(unchecked((nint)0x0000000000000000), unchecked((nint)0xFFFFFFFFFFFFFFC0), unchecked((nint)0x000000000000003F)));
                Assert.Equal(unchecked((nint)0x0000000000000001), NumberHelper<nint>.Clamp(unchecked((nint)0x0000000000000001), unchecked((nint)0xFFFFFFFFFFFFFFC0), unchecked((nint)0x000000000000003F)));
                Assert.Equal(unchecked((nint)0x000000000000003F), NumberHelper<nint>.Clamp(unchecked((nint)0x7FFFFFFFFFFFFFFF), unchecked((nint)0xFFFFFFFFFFFFFFC0), unchecked((nint)0x000000000000003F)));
                Assert.Equal(unchecked((nint)0xFFFFFFFFFFFFFFC0), NumberHelper<nint>.Clamp(unchecked((nint)0x8000000000000000), unchecked((nint)0xFFFFFFFFFFFFFFC0), unchecked((nint)0x000000000000003F)));
                Assert.Equal(unchecked((nint)0xFFFFFFFFFFFFFFFF), NumberHelper<nint>.Clamp(unchecked((nint)0xFFFFFFFFFFFFFFFF), unchecked((nint)0xFFFFFFFFFFFFFFC0), unchecked((nint)0x000000000000003F)));
            }
            else
            {
                Assert.Equal((nint)0x00000000, NumberHelper<nint>.Clamp((nint)0x00000000, unchecked((nint)0xFFFFFFC0), (nint)0x0000003F));
                Assert.Equal((nint)0x00000001, NumberHelper<nint>.Clamp((nint)0x00000001, unchecked((nint)0xFFFFFFC0), (nint)0x0000003F));
                Assert.Equal((nint)0x0000003F, NumberHelper<nint>.Clamp((nint)0x7FFFFFFF, unchecked((nint)0xFFFFFFC0), (nint)0x0000003F));
                Assert.Equal(unchecked((nint)0xFFFFFFC0), NumberHelper<nint>.Clamp(unchecked((nint)0x80000000), unchecked((nint)0xFFFFFFC0), (nint)0x0000003F));
                Assert.Equal(unchecked((nint)0xFFFFFFFF), NumberHelper<nint>.Clamp(unchecked((nint)0xFFFFFFFF), unchecked((nint)0xFFFFFFC0), (nint)0x0000003F));
            }
        }

        [Fact]
        public static void MaxTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0x0000000000000001), NumberHelper<nint>.Max(unchecked((nint)0x0000000000000000), (nint)1));
                Assert.Equal(unchecked((nint)0x0000000000000001), NumberHelper<nint>.Max(unchecked((nint)0x0000000000000001), (nint)1));
                Assert.Equal(unchecked((nint)0x7FFFFFFFFFFFFFFF), NumberHelper<nint>.Max(unchecked((nint)0x7FFFFFFFFFFFFFFF), (nint)1));
                Assert.Equal(unchecked((nint)0x0000000000000001), NumberHelper<nint>.Max(unchecked((nint)0x8000000000000000), (nint)1));
                Assert.Equal(unchecked((nint)0x0000000000000001), NumberHelper<nint>.Max(unchecked((nint)0xFFFFFFFFFFFFFFFF), (nint)1));
            }
            else
            {
                Assert.Equal((nint)0x00000001, NumberHelper<nint>.Max((nint)0x00000000, (nint)1));
                Assert.Equal((nint)0x00000001, NumberHelper<nint>.Max((nint)0x00000001, (nint)1));
                Assert.Equal((nint)0x7FFFFFFF, NumberHelper<nint>.Max((nint)0x7FFFFFFF, (nint)1));
                Assert.Equal((nint)0x00000001, NumberHelper<nint>.Max(unchecked((nint)0x80000000), (nint)1));
                Assert.Equal((nint)0x00000001, NumberHelper<nint>.Max(unchecked((nint)0xFFFFFFFF), (nint)1));
            }
        }

        [Fact]
        public static void MaxNumberTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0x0000000000000001), NumberHelper<nint>.MaxNumber(unchecked((nint)0x0000000000000000), (nint)1));
                Assert.Equal(unchecked((nint)0x0000000000000001), NumberHelper<nint>.MaxNumber(unchecked((nint)0x0000000000000001), (nint)1));
                Assert.Equal(unchecked((nint)0x7FFFFFFFFFFFFFFF), NumberHelper<nint>.MaxNumber(unchecked((nint)0x7FFFFFFFFFFFFFFF), (nint)1));
                Assert.Equal(unchecked((nint)0x0000000000000001), NumberHelper<nint>.MaxNumber(unchecked((nint)0x8000000000000000), (nint)1));
                Assert.Equal(unchecked((nint)0x0000000000000001), NumberHelper<nint>.MaxNumber(unchecked((nint)0xFFFFFFFFFFFFFFFF), (nint)1));
            }
            else
            {
                Assert.Equal((nint)0x00000001, NumberHelper<nint>.MaxNumber((nint)0x00000000, (nint)1));
                Assert.Equal((nint)0x00000001, NumberHelper<nint>.MaxNumber((nint)0x00000001, (nint)1));
                Assert.Equal((nint)0x7FFFFFFF, NumberHelper<nint>.MaxNumber((nint)0x7FFFFFFF, (nint)1));
                Assert.Equal((nint)0x00000001, NumberHelper<nint>.MaxNumber(unchecked((nint)0x80000000), (nint)1));
                Assert.Equal((nint)0x00000001, NumberHelper<nint>.MaxNumber(unchecked((nint)0xFFFFFFFF), (nint)1));
            }
        }

        [Fact]
        public static void MinTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0x0000000000000000), NumberHelper<nint>.Min(unchecked((nint)0x0000000000000000), (nint)1));
                Assert.Equal(unchecked((nint)0x0000000000000001), NumberHelper<nint>.Min(unchecked((nint)0x0000000000000001), (nint)1));
                Assert.Equal(unchecked((nint)0x0000000000000001), NumberHelper<nint>.Min(unchecked((nint)0x7FFFFFFFFFFFFFFF), (nint)1));
                Assert.Equal(unchecked((nint)0x8000000000000000), NumberHelper<nint>.Min(unchecked((nint)0x8000000000000000), (nint)1));
                Assert.Equal(unchecked((nint)0xFFFFFFFFFFFFFFFF), NumberHelper<nint>.Min(unchecked((nint)0xFFFFFFFFFFFFFFFF), (nint)1));
            }
            else
            {
                Assert.Equal((nint)0x00000000, NumberHelper<nint>.Min((nint)0x00000000, (nint)1));
                Assert.Equal((nint)0x00000001, NumberHelper<nint>.Min((nint)0x00000001, (nint)1));
                Assert.Equal((nint)0x00000001, NumberHelper<nint>.Min((nint)0x7FFFFFFF, (nint)1));
                Assert.Equal(unchecked((nint)0x80000000), NumberHelper<nint>.Min(unchecked((nint)0x80000000), (nint)1));
                Assert.Equal(unchecked((nint)0xFFFFFFFF), NumberHelper<nint>.Min(unchecked((nint)0xFFFFFFFF), (nint)1));
            }
        }

        [Fact]
        public static void MinNumberTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0x0000000000000000), NumberHelper<nint>.MinNumber(unchecked((nint)0x0000000000000000), (nint)1));
                Assert.Equal(unchecked((nint)0x0000000000000001), NumberHelper<nint>.MinNumber(unchecked((nint)0x0000000000000001), (nint)1));
                Assert.Equal(unchecked((nint)0x0000000000000001), NumberHelper<nint>.MinNumber(unchecked((nint)0x7FFFFFFFFFFFFFFF), (nint)1));
                Assert.Equal(unchecked((nint)0x8000000000000000), NumberHelper<nint>.MinNumber(unchecked((nint)0x8000000000000000), (nint)1));
                Assert.Equal(unchecked((nint)0xFFFFFFFFFFFFFFFF), NumberHelper<nint>.MinNumber(unchecked((nint)0xFFFFFFFFFFFFFFFF), (nint)1));
            }
            else
            {
                Assert.Equal((nint)0x00000000, NumberHelper<nint>.MinNumber((nint)0x00000000, (nint)1));
                Assert.Equal((nint)0x00000001, NumberHelper<nint>.MinNumber((nint)0x00000001, (nint)1));
                Assert.Equal((nint)0x00000001, NumberHelper<nint>.MinNumber((nint)0x7FFFFFFF, (nint)1));
                Assert.Equal(unchecked((nint)0x80000000), NumberHelper<nint>.MinNumber(unchecked((nint)0x80000000), (nint)1));
                Assert.Equal(unchecked((nint)0xFFFFFFFF), NumberHelper<nint>.MinNumber(unchecked((nint)0xFFFFFFFF), (nint)1));
            }
        }

        [Fact]
        public static void SignTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(0, NumberHelper<nint>.Sign(unchecked((nint)0x0000000000000000)));
                Assert.Equal(1, NumberHelper<nint>.Sign(unchecked((nint)0x0000000000000001)));
                Assert.Equal(1, NumberHelper<nint>.Sign(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(-1, NumberHelper<nint>.Sign(unchecked((nint)0x8000000000000000)));
                Assert.Equal(-1, NumberHelper<nint>.Sign(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal(0, NumberHelper<nint>.Sign((nint)0x00000000));
                Assert.Equal(1, NumberHelper<nint>.Sign((nint)0x00000001));
                Assert.Equal(1, NumberHelper<nint>.Sign((nint)0x7FFFFFFF));
                Assert.Equal(-1, NumberHelper<nint>.Sign(unchecked((nint)0x80000000)));
                Assert.Equal(-1, NumberHelper<nint>.Sign(unchecked((nint)0xFFFFFFFF)));
            }
        }

        //
        // INumberBase
        //

        [Fact]
        public static void OneTest()
        {
            Assert.Equal((nint)0x00000001, NumberBaseHelper<nint>.One);
        }

        [Fact]
        public static void RadixTest()
        {
            Assert.Equal(2, NumberBaseHelper<nint>.Radix);
        }

        [Fact]
        public static void ZeroTest()
        {
            Assert.Equal((nint)0x00000000, NumberBaseHelper<nint>.Zero);
        }

        [Fact]
        public static void AbsTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0x0000000000000000), NumberBaseHelper<nint>.Abs(unchecked((nint)0x0000000000000000)));
                Assert.Equal(unchecked((nint)0x0000000000000001), NumberBaseHelper<nint>.Abs(unchecked((nint)0x0000000000000001)));
                Assert.Equal(unchecked((nint)0x7FFFFFFFFFFFFFFF), NumberBaseHelper<nint>.Abs(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<nint>.Abs(unchecked((nint)0x8000000000000000)));
                Assert.Equal(unchecked((nint)0x0000000000000001), NumberBaseHelper<nint>.Abs(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((nint)0x00000000, NumberBaseHelper<nint>.Abs((nint)0x00000000));
                Assert.Equal((nint)0x00000001, NumberBaseHelper<nint>.Abs((nint)0x00000001));
                Assert.Equal((nint)0x7FFFFFFF, NumberBaseHelper<nint>.Abs((nint)0x7FFFFFFF));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<nint>.Abs(unchecked((nint)0x80000000)));
                Assert.Equal((nint)0x00000001, NumberBaseHelper<nint>.Abs(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateCheckedFromByteTest()
        {
            Assert.Equal((nint)0x00000000, NumberBaseHelper<nint>.CreateChecked<byte>(0x00));
            Assert.Equal((nint)0x00000001, NumberBaseHelper<nint>.CreateChecked<byte>(0x01));
            Assert.Equal((nint)0x0000007F, NumberBaseHelper<nint>.CreateChecked<byte>(0x7F));
            Assert.Equal((nint)0x00000080, NumberBaseHelper<nint>.CreateChecked<byte>(0x80));
            Assert.Equal((nint)0x000000FF, NumberBaseHelper<nint>.CreateChecked<byte>(0xFF));
        }

        [Fact]
        public static void CreateCheckedFromCharTest()
        {
            Assert.Equal((nint)0x00000000, NumberBaseHelper<nint>.CreateChecked<char>((char)0x0000));
            Assert.Equal((nint)0x00000001, NumberBaseHelper<nint>.CreateChecked<char>((char)0x0001));
            Assert.Equal((nint)0x00007FFF, NumberBaseHelper<nint>.CreateChecked<char>((char)0x7FFF));
            Assert.Equal((nint)0x00008000, NumberBaseHelper<nint>.CreateChecked<char>((char)0x8000));
            Assert.Equal((nint)0x0000FFFF, NumberBaseHelper<nint>.CreateChecked<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateCheckedFromDecimalTest()
        {
            Assert.Equal((nint)0x0000_0000_0000_0000, NumberBaseHelper<nint>.CreateChecked<decimal>(-0.0m));
            Assert.Equal((nint)0x0000_0000_0000_0000, NumberBaseHelper<nint>.CreateChecked<decimal>(+0.0m));
            Assert.Equal((nint)0x0000_0000_0000_0001, NumberBaseHelper<nint>.CreateChecked<decimal>(+1.0m));

            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0xFFFF_FFFF_FFFF_FFFF), NumberBaseHelper<nint>.CreateChecked<decimal>(-1.0m));
            }
            else
            {
                Assert.Equal(unchecked((nint)0xFFFF_FFFF), NumberBaseHelper<nint>.CreateChecked<decimal>(-1.0m));
            }

            Assert.Throws<OverflowException>(() => NumberBaseHelper<nint>.CreateChecked<decimal>(decimal.MinValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<nint>.CreateChecked<decimal>(decimal.MaxValue));
        }

        [Fact]
        [SkipOnMono("https://github.com/dotnet/runtime/issues/69795")]
        public static void CreateCheckedFromDoubleTest()
        {
            Assert.Equal((nint)0x0000_0000, NumberBaseHelper<nint>.CreateChecked<double>(+0.0));
            Assert.Equal((nint)0x0000_0000_0000_0000, NumberBaseHelper<nint>.CreateChecked<double>(-0.0));

            Assert.Equal((nint)0x0000_0000, NumberBaseHelper<nint>.CreateChecked<double>(+double.Epsilon));
            Assert.Equal((nint)0x0000_0000, NumberBaseHelper<nint>.CreateChecked<double>(-double.Epsilon));

            Assert.Equal((nint)0x0000_0001, NumberBaseHelper<nint>.CreateChecked<double>(+1.0));

            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0xFFFF_FFFF_FFFF_FFFF), NumberBaseHelper<nint>.CreateChecked<double>(-1.0));

                Assert.Equal(unchecked((nint)0x7FFF_FFFF_FFFF_FC00), NumberBaseHelper<nint>.CreateChecked<double>(+9223372036854774784.0));
                Assert.Equal(unchecked((nint)0x8000_0000_0000_0000), NumberBaseHelper<nint>.CreateChecked<double>(-9223372036854775808.0));

                Assert.Throws<OverflowException>(() => NumberBaseHelper<nint>.CreateChecked<double>(+9223372036854775808.0));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<nint>.CreateChecked<double>(-9223372036854777856.0));
            }
            else
            {
                Assert.Equal(unchecked((nint)0xFFFF_FFFF), NumberBaseHelper<nint>.CreateChecked<double>(-1.0));

                Assert.Equal((nint)0x7FFF_FFFF, NumberBaseHelper<nint>.CreateChecked<double>(+2147483647.0));
                Assert.Equal(unchecked((nint)0x8000_0000), NumberBaseHelper<nint>.CreateChecked<double>(-2147483648.0));

                Assert.Throws<OverflowException>(() => NumberBaseHelper<nint>.CreateChecked<double>(+2147483648.0));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<nint>.CreateChecked<double>(-2147483649.0));
            }

            Assert.Throws<OverflowException>(() => NumberBaseHelper<nint>.CreateChecked<double>(double.MaxValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<nint>.CreateChecked<double>(double.MinValue));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<nint>.CreateChecked<double>(double.PositiveInfinity));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<nint>.CreateChecked<double>(double.NegativeInfinity));
        }

        [Fact]
        public static void CreateCheckedFromHalfTest()
        {
            Assert.Equal((nint)0x0000_0000, NumberBaseHelper<nint>.CreateChecked<Half>(Half.Zero));
            Assert.Equal((nint)0x0000_0000, NumberBaseHelper<nint>.CreateChecked<Half>(Half.NegativeZero));

            Assert.Equal((nint)0x0000_0000, NumberBaseHelper<nint>.CreateChecked<Half>(+Half.Epsilon));
            Assert.Equal((nint)0x0000_0000, NumberBaseHelper<nint>.CreateChecked<Half>(-Half.Epsilon));

            Assert.Equal((nint)0x0000_0001, NumberBaseHelper<nint>.CreateChecked<Half>(Half.One));

            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0xFFFF_FFFF_FFFF_FFFF), NumberBaseHelper<nint>.CreateChecked<Half>(Half.NegativeOne));

                Assert.Equal((nint)0x0000_0000_0000_FFE0, NumberBaseHelper<nint>.CreateChecked<Half>(Half.MaxValue));
                Assert.Equal(unchecked((nint)0xFFFF_FFFF_FFFF_0020), NumberBaseHelper<nint>.CreateChecked<Half>(Half.MinValue));
            }
            else
            {
                Assert.Equal(unchecked((nint)0xFFFF_FFFF), NumberBaseHelper<nint>.CreateChecked<Half>(Half.NegativeOne));

                Assert.Equal((nint)0x0000_FFE0, NumberBaseHelper<nint>.CreateChecked<Half>(Half.MaxValue));
                Assert.Equal(unchecked((nint)0xFFFF_0020), NumberBaseHelper<nint>.CreateChecked<Half>(Half.MinValue));
            }

            Assert.Throws<OverflowException>(() => NumberBaseHelper<nint>.CreateChecked<Half>(Half.PositiveInfinity));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<nint>.CreateChecked<Half>(Half.NegativeInfinity));
        }

        [Fact]
        public static void CreateCheckedFromInt16Test()
        {
            Assert.Equal((nint)0x00000000, NumberBaseHelper<nint>.CreateChecked<short>(0x0000));
            Assert.Equal((nint)0x00000001, NumberBaseHelper<nint>.CreateChecked<short>(0x0001));
            Assert.Equal((nint)0x00007FFF, NumberBaseHelper<nint>.CreateChecked<short>(0x7FFF));
            Assert.Equal(unchecked((nint)(int)0xFFFF8000), NumberBaseHelper<nint>.CreateChecked<short>(unchecked((short)0x8000)));
            Assert.Equal(unchecked((nint)(int)0xFFFFFFFF), NumberBaseHelper<nint>.CreateChecked<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromInt32Test()
        {
            Assert.Equal((nint)0x00000000, NumberBaseHelper<nint>.CreateChecked<int>(0x00000000));
            Assert.Equal((nint)0x00000001, NumberBaseHelper<nint>.CreateChecked<int>(0x00000001));
            Assert.Equal((nint)0x7FFFFFFF, NumberBaseHelper<nint>.CreateChecked<int>(0x7FFFFFFF));
            Assert.Equal(unchecked((nint)(int)0x80000000), NumberBaseHelper<nint>.CreateChecked<int>(unchecked((int)0x80000000)));
            Assert.Equal(unchecked((nint)(int)0xFFFFFFFF), NumberBaseHelper<nint>.CreateChecked<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromInt64Test()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0x0000000000000000), NumberBaseHelper<nint>.CreateChecked<long>(0x0000000000000000));
                Assert.Equal(unchecked((nint)0x0000000000000001), NumberBaseHelper<nint>.CreateChecked<long>(0x0000000000000001));
                Assert.Equal(unchecked((nint)0x7FFFFFFFFFFFFFFF), NumberBaseHelper<nint>.CreateChecked<long>(0x7FFFFFFFFFFFFFFF));
                Assert.Equal(unchecked((nint)0x8000000000000000), NumberBaseHelper<nint>.CreateChecked<long>(unchecked((long)0x8000000000000000)));
                Assert.Equal(unchecked((nint)0xFFFFFFFFFFFFFFFF), NumberBaseHelper<nint>.CreateChecked<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((nint)0x00000000, NumberBaseHelper<nint>.CreateChecked<long>(0x0000000000000000));
                Assert.Equal((nint)0x00000001, NumberBaseHelper<nint>.CreateChecked<long>(0x0000000000000001));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<nint>.CreateChecked<long>(0x7FFFFFFFFFFFFFFF));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<nint>.CreateChecked<long>(unchecked((long)0x8000000000000000)));
                Assert.Equal(unchecked((nint)0xFFFFFFFF), NumberBaseHelper<nint>.CreateChecked<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateCheckedFromInt128Test()
        {
            Assert.Equal((nint)0x0000_0000, NumberBaseHelper<nint>.CreateChecked<Int128>(Int128.Zero));
            Assert.Equal((nint)0x0000_0001, NumberBaseHelper<nint>.CreateChecked<Int128>(Int128.One));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<nint>.CreateChecked<Int128>(Int128.MaxValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<nint>.CreateChecked<Int128>(Int128.MinValue));

            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0xFFFF_FFFF_FFFF_FFFF), NumberBaseHelper<nint>.CreateChecked<Int128>(Int128.NegativeOne));
            }
            else
            {
                Assert.Equal(unchecked((nint)0xFFFF_FFFF), NumberBaseHelper<nint>.CreateChecked<Int128>(Int128.NegativeOne));
            }
        }

        [Fact]
        public static void CreateCheckedFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0x0000000000000000), NumberBaseHelper<nint>.CreateChecked<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal(unchecked((nint)0x0000000000000001), NumberBaseHelper<nint>.CreateChecked<nint>(unchecked((nint)0x0000000000000001)));
                Assert.Equal(unchecked((nint)0x7FFFFFFFFFFFFFFF), NumberBaseHelper<nint>.CreateChecked<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((nint)0x8000000000000000), NumberBaseHelper<nint>.CreateChecked<nint>(unchecked((nint)0x8000000000000000)));
                Assert.Equal(unchecked((nint)0xFFFFFFFFFFFFFFFF), NumberBaseHelper<nint>.CreateChecked<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((nint)0x00000000, NumberBaseHelper<nint>.CreateChecked<nint>((nint)0x00000000));
                Assert.Equal((nint)0x00000001, NumberBaseHelper<nint>.CreateChecked<nint>((nint)0x00000001));
                Assert.Equal((nint)0x7FFFFFFF, NumberBaseHelper<nint>.CreateChecked<nint>((nint)0x7FFFFFFF));
                Assert.Equal(unchecked((nint)0x80000000), NumberBaseHelper<nint>.CreateChecked<nint>(unchecked(unchecked((nint)0x80000000))));
                Assert.Equal(unchecked((nint)0xFFFFFFFF), NumberBaseHelper<nint>.CreateChecked<nint>(unchecked(unchecked((nint)0xFFFFFFFF))));
            }
        }

        [Fact]
        [SkipOnMono("https://github.com/dotnet/runtime/issues/69795")]
        public static void CreateCheckedFromNFloatTest()
        {
            Assert.Equal((nint)0x0000_0000, NumberBaseHelper<nint>.CreateChecked<NFloat>(+0.0f));
            Assert.Equal((nint)0x0000_0000_0000_0000, NumberBaseHelper<nint>.CreateChecked<NFloat>(-0.0f));

            Assert.Equal((nint)0x0000_0000, NumberBaseHelper<nint>.CreateChecked<NFloat>(+NFloat.Epsilon));
            Assert.Equal((nint)0x0000_0000, NumberBaseHelper<nint>.CreateChecked<NFloat>(-NFloat.Epsilon));

            Assert.Equal((nint)0x0000_0001, NumberBaseHelper<nint>.CreateChecked<NFloat>(+1.0f));

            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0xFFFF_FFFF_FFFF_FFFF), NumberBaseHelper<nint>.CreateChecked<NFloat>((NFloat)(-1.0)));

                Assert.Equal(unchecked((nint)0x7FFF_FFFF_FFFF_FC00), NumberBaseHelper<nint>.CreateChecked<NFloat>((NFloat)(+9223372036854774784.0)));
                Assert.Equal(unchecked((nint)0x8000_0000_0000_0000), NumberBaseHelper<nint>.CreateChecked<NFloat>((NFloat)(-9223372036854775808.0)));

                Assert.Throws<OverflowException>(() => NumberBaseHelper<nint>.CreateChecked<NFloat>((NFloat)(+9223372036854775808.0)));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<nint>.CreateChecked<NFloat>((NFloat)(-9223372036854777856.0)));
            }
            else
            {
                Assert.Equal(unchecked((nint)0xFFFF_FFFF), NumberBaseHelper<nint>.CreateChecked<NFloat>(-1.0f));

                Assert.Equal((nint)0x7FFF_FF80, NumberBaseHelper<nint>.CreateChecked<NFloat>(+2147483520.0f));
                Assert.Equal(unchecked((nint)0x8000_0000), NumberBaseHelper<nint>.CreateChecked<NFloat>(-2147483648.0f));

                Assert.Throws<OverflowException>(() => NumberBaseHelper<nint>.CreateChecked<NFloat>(+2147483648.0f));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<nint>.CreateChecked<NFloat>(-2147483904.0f));
            }

            Assert.Throws<OverflowException>(() => NumberBaseHelper<nint>.CreateChecked<NFloat>(NFloat.MaxValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<nint>.CreateChecked<NFloat>(NFloat.MinValue));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<nint>.CreateChecked<NFloat>(NFloat.PositiveInfinity));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<nint>.CreateChecked<NFloat>(NFloat.NegativeInfinity));
        }

        [Fact]
        public static void CreateCheckedFromSByteTest()
        {
            Assert.Equal((nint)0x00000000, NumberBaseHelper<nint>.CreateChecked<sbyte>(0x00));
            Assert.Equal((nint)0x00000001, NumberBaseHelper<nint>.CreateChecked<sbyte>(0x01));
            Assert.Equal((nint)0x0000007F, NumberBaseHelper<nint>.CreateChecked<sbyte>(0x7F));
            Assert.Equal(unchecked((nint)(int)0xFFFFFF80), NumberBaseHelper<nint>.CreateChecked<sbyte>(unchecked((sbyte)0x80)));
            Assert.Equal(unchecked((nint)(int)0xFFFFFFFF), NumberBaseHelper<nint>.CreateChecked<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        [SkipOnMono("https://github.com/dotnet/runtime/issues/69795")]
        public static void CreateCheckedFromSingleTest()
        {
            Assert.Equal((nint)0x0000_0000, NumberBaseHelper<nint>.CreateChecked<float>(+0.0f));
            Assert.Equal((nint)0x0000_0000_0000_0000, NumberBaseHelper<nint>.CreateChecked<float>(-0.0f));

            Assert.Equal((nint)0x0000_0000, NumberBaseHelper<nint>.CreateChecked<float>(+float.Epsilon));
            Assert.Equal((nint)0x0000_0000, NumberBaseHelper<nint>.CreateChecked<float>(-float.Epsilon));

            Assert.Equal((nint)0x0000_0001, NumberBaseHelper<nint>.CreateChecked<float>(+1.0f));

            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0xFFFF_FFFF_FFFF_FFFF), NumberBaseHelper<nint>.CreateChecked<float>(-1.0f));

                Assert.Equal(unchecked((nint)0x7FFF_FF80_0000_0000), NumberBaseHelper<nint>.CreateChecked<float>(+9223371487098961920.0f));
                Assert.Equal(unchecked((nint)0x8000_0000_0000_0000), NumberBaseHelper<nint>.CreateChecked<float>(-9223372036854775808.0f));

                Assert.Throws<OverflowException>(() => NumberBaseHelper<nint>.CreateChecked<float>(+9223372036854775808.0f));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<nint>.CreateChecked<float>(-9223373136366403584.0f));
            }
            else
            {
                Assert.Equal(unchecked((nint)0xFFFF_FFFF), NumberBaseHelper<nint>.CreateChecked<float>(-1.0f));

                Assert.Equal((nint)0x7FFF_FF80, NumberBaseHelper<nint>.CreateChecked<float>(+2147483520.0f));
                Assert.Equal(unchecked((nint)0x8000_0000), NumberBaseHelper<nint>.CreateChecked<float>(-2147483648.0f));

                Assert.Throws<OverflowException>(() => NumberBaseHelper<nint>.CreateChecked<float>(+2147483648.0f));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<nint>.CreateChecked<float>(-2147483904.0f));
            }

            Assert.Throws<OverflowException>(() => NumberBaseHelper<nint>.CreateChecked<float>(float.MaxValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<nint>.CreateChecked<float>(float.MinValue));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<nint>.CreateChecked<float>(float.PositiveInfinity));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<nint>.CreateChecked<float>(float.NegativeInfinity));
        }

        [Fact]
        public static void CreateCheckedFromUInt16Test()
        {
            Assert.Equal((nint)0x00000000, NumberBaseHelper<nint>.CreateChecked<ushort>(0x0000));
            Assert.Equal((nint)0x00000001, NumberBaseHelper<nint>.CreateChecked<ushort>(0x0001));
            Assert.Equal((nint)0x00007FFF, NumberBaseHelper<nint>.CreateChecked<ushort>(0x7FFF));
            Assert.Equal((nint)0x00008000, NumberBaseHelper<nint>.CreateChecked<ushort>(0x8000));
            Assert.Equal((nint)0x0000FFFF, NumberBaseHelper<nint>.CreateChecked<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUInt32Test()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0x0000000000000000), NumberBaseHelper<nint>.CreateChecked<uint>(0x00000000));
                Assert.Equal(unchecked((nint)0x0000000000000001), NumberBaseHelper<nint>.CreateChecked<uint>(0x00000001));
                Assert.Equal(unchecked((nint)0x000000007FFFFFFF), NumberBaseHelper<nint>.CreateChecked<uint>(0x7FFFFFFF));
                Assert.Equal(unchecked((nint)0x0000000080000000), NumberBaseHelper<nint>.CreateChecked<uint>(0x80000000));
                Assert.Equal(unchecked((nint)0x00000000FFFFFFFF), NumberBaseHelper<nint>.CreateChecked<uint>(0xFFFFFFFF));
            }
            else
            {
                Assert.Equal((nint)0x00000000, NumberBaseHelper<nint>.CreateChecked<uint>(0x00000000));
                Assert.Equal((nint)0x00000001, NumberBaseHelper<nint>.CreateChecked<uint>(0x00000001));
                Assert.Equal((nint)0x7FFFFFFF, NumberBaseHelper<nint>.CreateChecked<uint>(0x7FFFFFFF));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<nint>.CreateChecked<uint>(0x80000000));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<nint>.CreateChecked<uint>(0xFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateCheckedFromUInt64Test()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0x0000000000000000), NumberBaseHelper<nint>.CreateChecked<ulong>(0x0000000000000000));
                Assert.Equal(unchecked((nint)0x0000000000000001), NumberBaseHelper<nint>.CreateChecked<ulong>(0x0000000000000001));
                Assert.Equal(unchecked((nint)0x7FFFFFFFFFFFFFFF), NumberBaseHelper<nint>.CreateChecked<ulong>(0x7FFFFFFFFFFFFFFF));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<nint>.CreateChecked<ulong>(0x8000000000000000));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<nint>.CreateChecked<ulong>(0xFFFFFFFFFFFFFFFF));
            }
            else
            {
                Assert.Equal((nint)0x00000000, NumberBaseHelper<nint>.CreateChecked<ulong>(0x0000000000000000));
                Assert.Equal((nint)0x00000001, NumberBaseHelper<nint>.CreateChecked<ulong>(0x0000000000000001));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<nint>.CreateChecked<ulong>(0x7FFFFFFFFFFFFFFF));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<nint>.CreateChecked<ulong>(0x8000000000000000));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<nint>.CreateChecked<ulong>(0xFFFFFFFFFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateCheckedFromUInt128Test()
        {
            Assert.Equal((nint)0x0000_0000, NumberBaseHelper<nint>.CreateChecked<UInt128>(UInt128.Zero));
            Assert.Equal((nint)0x0000_0001, NumberBaseHelper<nint>.CreateChecked<UInt128>(UInt128.One));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<nint>.CreateChecked<UInt128>(UInt128Tests_GenericMath.Int128MaxValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<nint>.CreateChecked<UInt128>(UInt128Tests_GenericMath.Int128MaxValuePlusOne));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<nint>.CreateChecked<UInt128>(UInt128.MaxValue));
        }

        [Fact]
        public static void CreateCheckedFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0x0000000000000000), NumberBaseHelper<nint>.CreateChecked<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal(unchecked((nint)0x0000000000000001), NumberBaseHelper<nint>.CreateChecked<nuint>(unchecked((nuint)0x0000000000000001)));
                Assert.Equal(unchecked((nint)0x7FFFFFFFFFFFFFFF), NumberBaseHelper<nint>.CreateChecked<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<nint>.CreateChecked<nuint>(unchecked((nuint)0x8000000000000000)));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<nint>.CreateChecked<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((nint)0x00000000, NumberBaseHelper<nint>.CreateChecked<nuint>((nuint)0x00000000));
                Assert.Equal((nint)0x00000001, NumberBaseHelper<nint>.CreateChecked<nuint>((nuint)0x00000001));
                Assert.Equal((nint)0x7FFFFFFF, NumberBaseHelper<nint>.CreateChecked<nuint>((nuint)0x7FFFFFFF));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<nint>.CreateChecked<nuint>(unchecked((nuint)0x80000000)));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<nint>.CreateChecked<nuint>(unchecked((nuint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateSaturatingFromByteTest()
        {
            Assert.Equal((nint)0x00000000, NumberBaseHelper<nint>.CreateSaturating<byte>(0x00));
            Assert.Equal((nint)0x00000001, NumberBaseHelper<nint>.CreateSaturating<byte>(0x01));
            Assert.Equal((nint)0x0000007F, NumberBaseHelper<nint>.CreateSaturating<byte>(0x7F));
            Assert.Equal((nint)0x00000080, NumberBaseHelper<nint>.CreateSaturating<byte>(0x80));
            Assert.Equal((nint)0x000000FF, NumberBaseHelper<nint>.CreateSaturating<byte>(0xFF));
        }

        [Fact]
        public static void CreateSaturatingFromCharTest()
        {
            Assert.Equal((nint)0x00000000, NumberBaseHelper<nint>.CreateSaturating<char>((char)0x0000));
            Assert.Equal((nint)0x00000001, NumberBaseHelper<nint>.CreateSaturating<char>((char)0x0001));
            Assert.Equal((nint)0x00007FFF, NumberBaseHelper<nint>.CreateSaturating<char>((char)0x7FFF));
            Assert.Equal((nint)0x00008000, NumberBaseHelper<nint>.CreateSaturating<char>((char)0x8000));
            Assert.Equal((nint)0x0000FFFF, NumberBaseHelper<nint>.CreateSaturating<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromDecimalTest()
        {
            Assert.Equal((nint)0x0000_0000_0000_0000, NumberBaseHelper<nint>.CreateSaturating<decimal>(-0.0m));
            Assert.Equal((nint)0x0000_0000_0000_0000, NumberBaseHelper<nint>.CreateSaturating<decimal>(+0.0m));
            Assert.Equal((nint)0x0000_0000_0000_0001, NumberBaseHelper<nint>.CreateSaturating<decimal>(+1.0m));

            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0xFFFF_FFFF_FFFF_FFFF), NumberBaseHelper<nint>.CreateSaturating<decimal>(-1.0m));
            }
            else
            {
                Assert.Equal(unchecked((nint)0xFFFF_FFFF), NumberBaseHelper<nint>.CreateSaturating<decimal>(-1.0m));
            }

            Assert.Equal(nint.MinValue, NumberBaseHelper<nint>.CreateSaturating<decimal>(decimal.MinValue));
            Assert.Equal(nint.MaxValue, NumberBaseHelper<nint>.CreateSaturating<decimal>(decimal.MaxValue));
        }

        [Fact]
        public static void CreateSaturatingFromDoubleTest()
        {
            Assert.Equal((nint)0x0000_0000, NumberBaseHelper<nint>.CreateSaturating<double>(+0.0));
            Assert.Equal((nint)0x0000_0000_0000_0000, NumberBaseHelper<nint>.CreateSaturating<double>(-0.0));

            Assert.Equal((nint)0x0000_0000, NumberBaseHelper<nint>.CreateSaturating<double>(+double.Epsilon));
            Assert.Equal((nint)0x0000_0000, NumberBaseHelper<nint>.CreateSaturating<double>(-double.Epsilon));

            Assert.Equal((nint)0x0000_0001, NumberBaseHelper<nint>.CreateSaturating<double>(+1.0));

            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0xFFFF_FFFF_FFFF_FFFF), NumberBaseHelper<nint>.CreateSaturating<double>(-1.0));

                Assert.Equal(unchecked((nint)0x7FFF_FFFF_FFFF_FC00), NumberBaseHelper<nint>.CreateSaturating<double>(+9223372036854774784.0));
                Assert.Equal(unchecked((nint)0x8000_0000_0000_0000), NumberBaseHelper<nint>.CreateSaturating<double>(-9223372036854775808.0));

                Assert.Equal(nint.MaxValue, NumberBaseHelper<nint>.CreateSaturating<double>(+9223372036854775808.0));
                Assert.Equal(nint.MinValue, NumberBaseHelper<nint>.CreateSaturating<double>(-9223372036854777856.0));
            }
            else
            {
                Assert.Equal(unchecked((nint)0xFFFF_FFFF), NumberBaseHelper<nint>.CreateSaturating<double>(-1.0));

                Assert.Equal((nint)0x7FFF_FFFF, NumberBaseHelper<nint>.CreateSaturating<double>(+2147483647.0));
                Assert.Equal(unchecked((nint)0x8000_0000), NumberBaseHelper<nint>.CreateSaturating<double>(-2147483648.0));

                Assert.Equal(nint.MaxValue, NumberBaseHelper<nint>.CreateSaturating<double>(+2147483648.0));
                Assert.Equal(nint.MinValue, NumberBaseHelper<nint>.CreateSaturating<double>(-2147483649.0));
            }

            Assert.Equal(nint.MaxValue, NumberBaseHelper<nint>.CreateSaturating<double>(double.MaxValue));
            Assert.Equal(nint.MinValue, NumberBaseHelper<nint>.CreateSaturating<double>(double.MinValue));

            Assert.Equal(nint.MaxValue, NumberBaseHelper<nint>.CreateSaturating<double>(double.PositiveInfinity));
            Assert.Equal(nint.MinValue, NumberBaseHelper<nint>.CreateSaturating<double>(double.NegativeInfinity));
        }

        [Fact]
        public static void CreateSaturatingFromHalfTest()
        {
            Assert.Equal((nint)0x0000_0000, NumberBaseHelper<nint>.CreateSaturating<Half>(Half.Zero));
            Assert.Equal((nint)0x0000_0000, NumberBaseHelper<nint>.CreateSaturating<Half>(Half.NegativeZero));

            Assert.Equal((nint)0x0000_0000, NumberBaseHelper<nint>.CreateSaturating<Half>(+Half.Epsilon));
            Assert.Equal((nint)0x0000_0000, NumberBaseHelper<nint>.CreateSaturating<Half>(-Half.Epsilon));

            Assert.Equal((nint)0x0000_0001, NumberBaseHelper<nint>.CreateSaturating<Half>(Half.One));

            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0xFFFF_FFFF_FFFF_FFFF), NumberBaseHelper<nint>.CreateSaturating<Half>(Half.NegativeOne));

                Assert.Equal((nint)0x0000_0000_0000_FFE0, NumberBaseHelper<nint>.CreateSaturating<Half>(Half.MaxValue));
                Assert.Equal(unchecked((nint)0xFFFF_FFFF_FFFF_0020), NumberBaseHelper<nint>.CreateSaturating<Half>(Half.MinValue));
            }
            else
            {
                Assert.Equal(unchecked((nint)0xFFFF_FFFF), NumberBaseHelper<nint>.CreateSaturating<Half>(Half.NegativeOne));

                Assert.Equal((nint)0x0000_FFE0, NumberBaseHelper<nint>.CreateSaturating<Half>(Half.MaxValue));
                Assert.Equal(unchecked((nint)0xFFFF_0020), NumberBaseHelper<nint>.CreateSaturating<Half>(Half.MinValue));
            }

            Assert.Equal(nint.MaxValue, NumberBaseHelper<nint>.CreateSaturating<Half>(Half.PositiveInfinity));
            Assert.Equal(nint.MinValue, NumberBaseHelper<nint>.CreateSaturating<Half>(Half.NegativeInfinity));
        }

        [Fact]
        public static void CreateSaturatingFromInt16Test()
        {
            Assert.Equal((nint)0x00000000, NumberBaseHelper<nint>.CreateSaturating<short>(0x0000));
            Assert.Equal((nint)0x00000001, NumberBaseHelper<nint>.CreateSaturating<short>(0x0001));
            Assert.Equal((nint)0x00007FFF, NumberBaseHelper<nint>.CreateSaturating<short>(0x7FFF));
            Assert.Equal(unchecked((nint)(int)0xFFFF8000), NumberBaseHelper<nint>.CreateSaturating<short>(unchecked((short)0x8000)));
            Assert.Equal(unchecked((nint)(int)0xFFFFFFFF), NumberBaseHelper<nint>.CreateSaturating<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromInt32Test()
        {
            Assert.Equal((nint)0x00000000, NumberBaseHelper<nint>.CreateSaturating<int>(0x00000000));
            Assert.Equal((nint)0x00000001, NumberBaseHelper<nint>.CreateSaturating<int>(0x00000001));
            Assert.Equal((nint)0x7FFFFFFF, NumberBaseHelper<nint>.CreateSaturating<int>(0x7FFFFFFF));
            Assert.Equal(unchecked((nint)(int)0x80000000), NumberBaseHelper<nint>.CreateSaturating<int>(unchecked((int)0x80000000)));
            Assert.Equal(unchecked((nint)(int)0xFFFFFFFF), NumberBaseHelper<nint>.CreateSaturating<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromInt64Test()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0x0000000000000000), NumberBaseHelper<nint>.CreateSaturating<long>(0x0000000000000000));
                Assert.Equal(unchecked((nint)0x0000000000000001), NumberBaseHelper<nint>.CreateSaturating<long>(0x0000000000000001));
                Assert.Equal(unchecked((nint)0x7FFFFFFFFFFFFFFF), NumberBaseHelper<nint>.CreateSaturating<long>(0x7FFFFFFFFFFFFFFF));
                Assert.Equal(unchecked((nint)0x8000000000000000), NumberBaseHelper<nint>.CreateSaturating<long>(unchecked((long)0x8000000000000000)));
                Assert.Equal(unchecked((nint)0xFFFFFFFFFFFFFFFF), NumberBaseHelper<nint>.CreateSaturating<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((nint)0x00000000, NumberBaseHelper<nint>.CreateSaturating<long>(0x0000000000000000));
                Assert.Equal((nint)0x00000001, NumberBaseHelper<nint>.CreateSaturating<long>(0x0000000000000001));
                Assert.Equal(unchecked((nint)0x7FFFFFFF), NumberBaseHelper<nint>.CreateSaturating<long>(0x7FFFFFFFFFFFFFFF));
                Assert.Equal(unchecked((nint)0x80000000), NumberBaseHelper<nint>.CreateSaturating<long>(unchecked((long)0x8000000000000000)));
                Assert.Equal(unchecked((nint)0xFFFFFFFF), NumberBaseHelper<nint>.CreateSaturating<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateSaturatingFromInt128Test()
        {
            Assert.Equal((nint)0x0000_0000, NumberBaseHelper<nint>.CreateSaturating<Int128>(Int128.Zero));
            Assert.Equal((nint)0x0000_0001, NumberBaseHelper<nint>.CreateSaturating<Int128>(Int128.One));

            Assert.Equal(nint.MaxValue, NumberBaseHelper<nint>.CreateSaturating<Int128>(Int128.MaxValue));
            Assert.Equal(nint.MinValue, NumberBaseHelper<nint>.CreateSaturating<Int128>(Int128.MinValue));

            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0xFFFF_FFFF_FFFF_FFFF), NumberBaseHelper<nint>.CreateSaturating<Int128>(Int128.NegativeOne));
            }
            else
            {
                Assert.Equal(unchecked((nint)0xFFFF_FFFF), NumberBaseHelper<nint>.CreateSaturating<Int128>(Int128.NegativeOne));
            }
        }

        [Fact]
        public static void CreateSaturatingFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0x0000000000000000), NumberBaseHelper<nint>.CreateSaturating<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal(unchecked((nint)0x0000000000000001), NumberBaseHelper<nint>.CreateSaturating<nint>(unchecked((nint)0x0000000000000001)));
                Assert.Equal(unchecked((nint)0x7FFFFFFFFFFFFFFF), NumberBaseHelper<nint>.CreateSaturating<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((nint)0x8000000000000000), NumberBaseHelper<nint>.CreateSaturating<nint>(unchecked((nint)0x8000000000000000)));
                Assert.Equal(unchecked((nint)0xFFFFFFFFFFFFFFFF), NumberBaseHelper<nint>.CreateSaturating<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((nint)0x00000000, NumberBaseHelper<nint>.CreateSaturating<nint>((nint)0x00000000));
                Assert.Equal((nint)0x00000001, NumberBaseHelper<nint>.CreateSaturating<nint>((nint)0x00000001));
                Assert.Equal((nint)0x7FFFFFFF, NumberBaseHelper<nint>.CreateSaturating<nint>((nint)0x7FFFFFFF));
                Assert.Equal(unchecked((nint)0x80000000), NumberBaseHelper<nint>.CreateSaturating<nint>(unchecked(unchecked((nint)0x80000000))));
                Assert.Equal(unchecked((nint)0xFFFFFFFF), NumberBaseHelper<nint>.CreateSaturating<nint>(unchecked(unchecked((nint)0xFFFFFFFF))));
            }
        }

        [Fact]
        public static void CreateSaturatingFromNFloatTest()
        {
            Assert.Equal((nint)0x0000_0000, NumberBaseHelper<nint>.CreateSaturating<NFloat>(+0.0f));
            Assert.Equal((nint)0x0000_0000_0000_0000, NumberBaseHelper<nint>.CreateSaturating<NFloat>(-0.0f));

            Assert.Equal((nint)0x0000_0000, NumberBaseHelper<nint>.CreateSaturating<NFloat>(+NFloat.Epsilon));
            Assert.Equal((nint)0x0000_0000, NumberBaseHelper<nint>.CreateSaturating<NFloat>(-NFloat.Epsilon));

            Assert.Equal((nint)0x0000_0001, NumberBaseHelper<nint>.CreateSaturating<NFloat>(+1.0f));

            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0xFFFF_FFFF_FFFF_FFFF), NumberBaseHelper<nint>.CreateSaturating<NFloat>((NFloat)(-1.0)));

                Assert.Equal(unchecked((nint)0x7FFF_FFFF_FFFF_FC00), NumberBaseHelper<nint>.CreateSaturating<NFloat>((NFloat)(+9223372036854774784.0)));
                Assert.Equal(unchecked((nint)0x8000_0000_0000_0000), NumberBaseHelper<nint>.CreateSaturating<NFloat>((NFloat)(-9223372036854775808.0)));

                Assert.Equal(nint.MaxValue, NumberBaseHelper<nint>.CreateSaturating<NFloat>((NFloat)(+9223372036854775808.0)));
                Assert.Equal(nint.MinValue, NumberBaseHelper<nint>.CreateSaturating<NFloat>((NFloat)(-9223372036854777856.0)));
            }
            else
            {
                Assert.Equal(unchecked((nint)0xFFFF_FFFF), NumberBaseHelper<nint>.CreateSaturating<NFloat>(-1.0f));

                Assert.Equal((nint)0x7FFF_FF80, NumberBaseHelper<nint>.CreateSaturating<NFloat>(+2147483520.0f));
                Assert.Equal(unchecked((nint)0x8000_0000), NumberBaseHelper<nint>.CreateSaturating<NFloat>(-2147483648.0f));

                Assert.Equal(nint.MaxValue, NumberBaseHelper<nint>.CreateSaturating<NFloat>(+2147483648.0f));
                Assert.Equal(nint.MinValue, NumberBaseHelper<nint>.CreateSaturating<NFloat>(-2147483904.0f));
            }

            Assert.Equal(nint.MaxValue, NumberBaseHelper<nint>.CreateSaturating<NFloat>(NFloat.MaxValue));
            Assert.Equal(nint.MinValue, NumberBaseHelper<nint>.CreateSaturating<NFloat>(NFloat.MinValue));

            Assert.Equal(nint.MaxValue, NumberBaseHelper<nint>.CreateSaturating<NFloat>(NFloat.PositiveInfinity));
            Assert.Equal(nint.MinValue, NumberBaseHelper<nint>.CreateSaturating<NFloat>(NFloat.NegativeInfinity));
        }

        [Fact]
        public static void CreateSaturatingFromSByteTest()
        {
            Assert.Equal((nint)0x00000000, NumberBaseHelper<nint>.CreateSaturating<sbyte>(0x00));
            Assert.Equal((nint)0x00000001, NumberBaseHelper<nint>.CreateSaturating<sbyte>(0x01));
            Assert.Equal((nint)0x0000007F, NumberBaseHelper<nint>.CreateSaturating<sbyte>(0x7F));
            Assert.Equal(unchecked((nint)(int)0xFFFFFF80), NumberBaseHelper<nint>.CreateSaturating<sbyte>(unchecked((sbyte)0x80)));
            Assert.Equal(unchecked((nint)(int)0xFFFFFFFF), NumberBaseHelper<nint>.CreateSaturating<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateSaturatingFromSingleTest()
        {
            Assert.Equal((nint)0x0000_0000, NumberBaseHelper<nint>.CreateSaturating<float>(+0.0f));
            Assert.Equal((nint)0x0000_0000_0000_0000, NumberBaseHelper<nint>.CreateSaturating<float>(-0.0f));

            Assert.Equal((nint)0x0000_0000, NumberBaseHelper<nint>.CreateSaturating<float>(+float.Epsilon));
            Assert.Equal((nint)0x0000_0000, NumberBaseHelper<nint>.CreateSaturating<float>(-float.Epsilon));

            Assert.Equal((nint)0x0000_0001, NumberBaseHelper<nint>.CreateSaturating<float>(+1.0f));

            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0xFFFF_FFFF_FFFF_FFFF), NumberBaseHelper<nint>.CreateSaturating<float>(-1.0f));

                Assert.Equal(unchecked((nint)0x7FFF_FF80_0000_0000), NumberBaseHelper<nint>.CreateSaturating<float>(+9223371487098961920.0f));
                Assert.Equal(unchecked((nint)0x8000_0000_0000_0000), NumberBaseHelper<nint>.CreateSaturating<float>(-9223372036854775808.0f));

                Assert.Equal(nint.MaxValue, NumberBaseHelper<nint>.CreateSaturating<float>(+9223372036854775808.0f));
                Assert.Equal(nint.MinValue, NumberBaseHelper<nint>.CreateSaturating<float>(-9223373136366403584.0f));
            }
            else
            {
                Assert.Equal(unchecked((nint)0xFFFF_FFFF), NumberBaseHelper<nint>.CreateSaturating<float>(-1.0f));

                Assert.Equal((nint)0x7FFF_FF80, NumberBaseHelper<nint>.CreateSaturating<float>(+2147483520.0f));
                Assert.Equal(unchecked((nint)0x8000_0000), NumberBaseHelper<nint>.CreateSaturating<float>(-2147483648.0f));

                Assert.Equal(nint.MaxValue, NumberBaseHelper<nint>.CreateSaturating<float>(+2147483648.0f));
                Assert.Equal(nint.MinValue, NumberBaseHelper<nint>.CreateSaturating<float>(-2147483904.0f));
            }

            Assert.Equal(nint.MaxValue, NumberBaseHelper<nint>.CreateSaturating<float>(float.MaxValue));
            Assert.Equal(nint.MinValue, NumberBaseHelper<nint>.CreateSaturating<float>(float.MinValue));

            Assert.Equal(nint.MaxValue, NumberBaseHelper<nint>.CreateSaturating<float>(float.PositiveInfinity));
            Assert.Equal(nint.MinValue, NumberBaseHelper<nint>.CreateSaturating<float>(float.NegativeInfinity));
        }

        [Fact]
        public static void CreateSaturatingFromUInt16Test()
        {
            Assert.Equal((nint)0x00000000, NumberBaseHelper<nint>.CreateSaturating<ushort>(0x0000));
            Assert.Equal((nint)0x00000001, NumberBaseHelper<nint>.CreateSaturating<ushort>(0x0001));
            Assert.Equal((nint)0x00007FFF, NumberBaseHelper<nint>.CreateSaturating<ushort>(0x7FFF));
            Assert.Equal((nint)0x00008000, NumberBaseHelper<nint>.CreateSaturating<ushort>(0x8000));
            Assert.Equal((nint)0x0000FFFF, NumberBaseHelper<nint>.CreateSaturating<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUInt32Test()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0x0000000000000000), NumberBaseHelper<nint>.CreateSaturating<uint>(0x00000000));
                Assert.Equal(unchecked((nint)0x0000000000000001), NumberBaseHelper<nint>.CreateSaturating<uint>(0x00000001));
                Assert.Equal(unchecked((nint)0x000000007FFFFFFF), NumberBaseHelper<nint>.CreateSaturating<uint>(0x7FFFFFFF));
                Assert.Equal(unchecked((nint)0x0000000080000000), NumberBaseHelper<nint>.CreateSaturating<uint>(0x80000000));
                Assert.Equal(unchecked((nint)0x00000000FFFFFFFF), NumberBaseHelper<nint>.CreateSaturating<uint>(0xFFFFFFFF));
            }
            else
            {
                Assert.Equal((nint)0x00000000, NumberBaseHelper<nint>.CreateSaturating<uint>(0x00000000));
                Assert.Equal((nint)0x00000001, NumberBaseHelper<nint>.CreateSaturating<uint>(0x00000001));
                Assert.Equal((nint)0x7FFFFFFF, NumberBaseHelper<nint>.CreateSaturating<uint>(0x7FFFFFFF));
                Assert.Equal((nint)0x7FFFFFFF, NumberBaseHelper<nint>.CreateSaturating<uint>(0x80000000));
                Assert.Equal((nint)0x7FFFFFFF, NumberBaseHelper<nint>.CreateSaturating<uint>(0xFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateSaturatingFromUInt64Test()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0x0000000000000000), NumberBaseHelper<nint>.CreateSaturating<ulong>(0x0000000000000000));
                Assert.Equal(unchecked((nint)0x0000000000000001), NumberBaseHelper<nint>.CreateSaturating<ulong>(0x0000000000000001));
                Assert.Equal(unchecked((nint)0x7FFFFFFFFFFFFFFF), NumberBaseHelper<nint>.CreateSaturating<ulong>(0x7FFFFFFFFFFFFFFF));
                Assert.Equal(unchecked((nint)0x7FFFFFFFFFFFFFFF), NumberBaseHelper<nint>.CreateSaturating<ulong>(0x8000000000000000));
                Assert.Equal(unchecked((nint)0x7FFFFFFFFFFFFFFF), NumberBaseHelper<nint>.CreateSaturating<ulong>(0xFFFFFFFFFFFFFFFF));
            }
            else
            {
                Assert.Equal((nint)0x00000000, NumberBaseHelper<nint>.CreateSaturating<ulong>(0x0000000000000000));
                Assert.Equal((nint)0x00000001, NumberBaseHelper<nint>.CreateSaturating<ulong>(0x0000000000000001));
                Assert.Equal((nint)0x7FFFFFFF, NumberBaseHelper<nint>.CreateSaturating<ulong>(0x7FFFFFFFFFFFFFFF));
                Assert.Equal((nint)0x7FFFFFFF, NumberBaseHelper<nint>.CreateSaturating<ulong>(0x8000000000000000));
                Assert.Equal((nint)0x7FFFFFFF, NumberBaseHelper<nint>.CreateSaturating<ulong>(0xFFFFFFFFFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateSaturatingFromUInt128Test()
        {
            Assert.Equal((nint)0x0000_0000, NumberBaseHelper<nint>.CreateSaturating<UInt128>(UInt128.Zero));
            Assert.Equal((nint)0x0000_0001, NumberBaseHelper<nint>.CreateSaturating<UInt128>(UInt128.One));
            Assert.Equal(nint.MaxValue, NumberBaseHelper<nint>.CreateSaturating<UInt128>(UInt128Tests_GenericMath.Int128MaxValue));
            Assert.Equal(nint.MaxValue, NumberBaseHelper<nint>.CreateSaturating<UInt128>(UInt128Tests_GenericMath.Int128MaxValuePlusOne));
            Assert.Equal(nint.MaxValue, NumberBaseHelper<nint>.CreateSaturating<UInt128>(UInt128.MaxValue));
        }

        [Fact]
        public static void CreateSaturatingFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0x0000000000000000), NumberBaseHelper<nint>.CreateSaturating<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal(unchecked((nint)0x0000000000000001), NumberBaseHelper<nint>.CreateSaturating<nuint>(unchecked((nuint)0x0000000000000001)));
                Assert.Equal(unchecked((nint)0x7FFFFFFFFFFFFFFF), NumberBaseHelper<nint>.CreateSaturating<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((nint)0x7FFFFFFFFFFFFFFF), NumberBaseHelper<nint>.CreateSaturating<nuint>(unchecked((nuint)0x8000000000000000)));
                Assert.Equal(unchecked((nint)0x7FFFFFFFFFFFFFFF), NumberBaseHelper<nint>.CreateSaturating<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((nint)0x00000000, NumberBaseHelper<nint>.CreateSaturating<nuint>((nuint)0x00000000));
                Assert.Equal((nint)0x00000001, NumberBaseHelper<nint>.CreateSaturating<nuint>((nuint)0x00000001));
                Assert.Equal((nint)0x7FFFFFFF, NumberBaseHelper<nint>.CreateSaturating<nuint>((nuint)0x7FFFFFFF));
                Assert.Equal((nint)0x7FFFFFFF, NumberBaseHelper<nint>.CreateSaturating<nuint>(unchecked((nuint)0x80000000)));
                Assert.Equal((nint)0x7FFFFFFF, NumberBaseHelper<nint>.CreateSaturating<nuint>(unchecked((nuint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateTruncatingFromByteTest()
        {
            Assert.Equal((nint)0x00000000, NumberBaseHelper<nint>.CreateTruncating<byte>(0x00));
            Assert.Equal((nint)0x00000001, NumberBaseHelper<nint>.CreateTruncating<byte>(0x01));
            Assert.Equal((nint)0x0000007F, NumberBaseHelper<nint>.CreateTruncating<byte>(0x7F));
            Assert.Equal((nint)0x00000080, NumberBaseHelper<nint>.CreateTruncating<byte>(0x80));
            Assert.Equal((nint)0x000000FF, NumberBaseHelper<nint>.CreateTruncating<byte>(0xFF));
        }

        [Fact]
        public static void CreateTruncatingFromCharTest()
        {
            Assert.Equal((nint)0x00000000, NumberBaseHelper<nint>.CreateTruncating<char>((char)0x0000));
            Assert.Equal((nint)0x00000001, NumberBaseHelper<nint>.CreateTruncating<char>((char)0x0001));
            Assert.Equal((nint)0x00007FFF, NumberBaseHelper<nint>.CreateTruncating<char>((char)0x7FFF));
            Assert.Equal((nint)0x00008000, NumberBaseHelper<nint>.CreateTruncating<char>((char)0x8000));
            Assert.Equal((nint)0x0000FFFF, NumberBaseHelper<nint>.CreateTruncating<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromDecimalTest()
        {
            Assert.Equal((nint)0x0000_0000_0000_0000, NumberBaseHelper<nint>.CreateTruncating<decimal>(-0.0m));
            Assert.Equal((nint)0x0000_0000_0000_0000, NumberBaseHelper<nint>.CreateTruncating<decimal>(+0.0m));
            Assert.Equal((nint)0x0000_0000_0000_0001, NumberBaseHelper<nint>.CreateTruncating<decimal>(+1.0m));

            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0xFFFF_FFFF_FFFF_FFFF), NumberBaseHelper<nint>.CreateTruncating<decimal>(-1.0m));
            }
            else
            {
                Assert.Equal(unchecked((nint)0xFFFF_FFFF), NumberBaseHelper<nint>.CreateTruncating<decimal>(-1.0m));
            }

            Assert.Equal(nint.MinValue, NumberBaseHelper<nint>.CreateTruncating<decimal>(decimal.MinValue));
            Assert.Equal(nint.MaxValue, NumberBaseHelper<nint>.CreateTruncating<decimal>(decimal.MaxValue));
        }

        [Fact]
        public static void CreateTruncatingFromDoubleTest()
        {
            Assert.Equal((nint)0x0000_0000, NumberBaseHelper<nint>.CreateTruncating<double>(+0.0));
            Assert.Equal((nint)0x0000_0000_0000_0000, NumberBaseHelper<nint>.CreateTruncating<double>(-0.0));

            Assert.Equal((nint)0x0000_0000, NumberBaseHelper<nint>.CreateTruncating<double>(+double.Epsilon));
            Assert.Equal((nint)0x0000_0000, NumberBaseHelper<nint>.CreateTruncating<double>(-double.Epsilon));

            Assert.Equal((nint)0x0000_0001, NumberBaseHelper<nint>.CreateTruncating<double>(+1.0));

            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0xFFFF_FFFF_FFFF_FFFF), NumberBaseHelper<nint>.CreateTruncating<double>(-1.0));

                Assert.Equal(unchecked((nint)0x7FFF_FFFF_FFFF_FC00), NumberBaseHelper<nint>.CreateTruncating<double>(+9223372036854774784.0));
                Assert.Equal(unchecked((nint)0x8000_0000_0000_0000), NumberBaseHelper<nint>.CreateTruncating<double>(-9223372036854775808.0));

                Assert.Equal(nint.MaxValue, NumberBaseHelper<nint>.CreateTruncating<double>(+9223372036854775808.0));
                Assert.Equal(nint.MinValue, NumberBaseHelper<nint>.CreateTruncating<double>(-9223372036854777856.0));
            }
            else
            {
                Assert.Equal(unchecked((nint)0xFFFF_FFFF), NumberBaseHelper<nint>.CreateTruncating<double>(-1.0));

                Assert.Equal((nint)0x7FFF_FFFF, NumberBaseHelper<nint>.CreateTruncating<double>(+2147483647.0));
                Assert.Equal(unchecked((nint)0x8000_0000), NumberBaseHelper<nint>.CreateTruncating<double>(-2147483648.0));

                Assert.Equal(nint.MaxValue, NumberBaseHelper<nint>.CreateTruncating<double>(+2147483648.0));
                Assert.Equal(nint.MinValue, NumberBaseHelper<nint>.CreateTruncating<double>(-2147483649.0));
            }

            Assert.Equal(nint.MaxValue, NumberBaseHelper<nint>.CreateTruncating<double>(double.MaxValue));
            Assert.Equal(nint.MinValue, NumberBaseHelper<nint>.CreateTruncating<double>(double.MinValue));

            Assert.Equal(nint.MaxValue, NumberBaseHelper<nint>.CreateTruncating<double>(double.PositiveInfinity));
            Assert.Equal(nint.MinValue, NumberBaseHelper<nint>.CreateTruncating<double>(double.NegativeInfinity));
        }

        [Fact]
        public static void CreateTruncatingFromHalfTest()
        {
            Assert.Equal((nint)0x0000_0000, NumberBaseHelper<nint>.CreateTruncating<Half>(Half.Zero));
            Assert.Equal((nint)0x0000_0000, NumberBaseHelper<nint>.CreateTruncating<Half>(Half.NegativeZero));

            Assert.Equal((nint)0x0000_0000, NumberBaseHelper<nint>.CreateTruncating<Half>(+Half.Epsilon));
            Assert.Equal((nint)0x0000_0000, NumberBaseHelper<nint>.CreateTruncating<Half>(-Half.Epsilon));

            Assert.Equal((nint)0x0000_0001, NumberBaseHelper<nint>.CreateTruncating<Half>(Half.One));

            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0xFFFF_FFFF_FFFF_FFFF), NumberBaseHelper<nint>.CreateTruncating<Half>(Half.NegativeOne));

                Assert.Equal((nint)0x0000_0000_0000_FFE0, NumberBaseHelper<nint>.CreateTruncating<Half>(Half.MaxValue));
                Assert.Equal(unchecked((nint)0xFFFF_FFFF_FFFF_0020), NumberBaseHelper<nint>.CreateTruncating<Half>(Half.MinValue));
            }
            else
            {
                Assert.Equal(unchecked((nint)0xFFFF_FFFF), NumberBaseHelper<nint>.CreateTruncating<Half>(Half.NegativeOne));

                Assert.Equal((nint)0x0000_FFE0, NumberBaseHelper<nint>.CreateTruncating<Half>(Half.MaxValue));
                Assert.Equal(unchecked((nint)0xFFFF_0020), NumberBaseHelper<nint>.CreateTruncating<Half>(Half.MinValue));
            }

            Assert.Equal(nint.MaxValue, NumberBaseHelper<nint>.CreateTruncating<Half>(Half.PositiveInfinity));
            Assert.Equal(nint.MinValue, NumberBaseHelper<nint>.CreateTruncating<Half>(Half.NegativeInfinity));
        }

        [Fact]
        public static void CreateTruncatingFromInt16Test()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0x0000000000000000), NumberBaseHelper<nint>.CreateTruncating<short>(0x0000));
                Assert.Equal(unchecked((nint)0x0000000000000001), NumberBaseHelper<nint>.CreateTruncating<short>(0x0001));
                Assert.Equal(unchecked((nint)0x0000000000007FFF), NumberBaseHelper<nint>.CreateTruncating<short>(0x7FFF));
                Assert.Equal(unchecked((nint)0xFFFFFFFFFFFF8000), NumberBaseHelper<nint>.CreateTruncating<short>(unchecked((short)0x8000)));
                Assert.Equal(unchecked((nint)0xFFFFFFFFFFFFFFFF), NumberBaseHelper<nint>.CreateTruncating<short>(unchecked((short)0xFFFF)));
            }
            else
            {
                Assert.Equal((nint)0x00000000, NumberBaseHelper<nint>.CreateTruncating<short>(0x0000));
                Assert.Equal((nint)0x00000001, NumberBaseHelper<nint>.CreateTruncating<short>(0x0001));
                Assert.Equal((nint)0x00007FFF, NumberBaseHelper<nint>.CreateTruncating<short>(0x7FFF));
                Assert.Equal(unchecked((nint)0xFFFF8000), NumberBaseHelper<nint>.CreateTruncating<short>(unchecked((short)0x8000)));
                Assert.Equal(unchecked((nint)0xFFFFFFFF), NumberBaseHelper<nint>.CreateTruncating<short>(unchecked((short)0xFFFF)));
            }
        }

        [Fact]
        public static void CreateTruncatingFromInt32Test()
        {
            Assert.Equal((nint)0x00000000, NumberBaseHelper<nint>.CreateTruncating<int>(0x00000000));
            Assert.Equal((nint)0x00000001, NumberBaseHelper<nint>.CreateTruncating<int>(0x00000001));
            Assert.Equal((nint)0x7FFFFFFF, NumberBaseHelper<nint>.CreateTruncating<int>(0x7FFFFFFF));
            Assert.Equal(unchecked((nint)(int)0x80000000), NumberBaseHelper<nint>.CreateTruncating<int>(unchecked((int)0x80000000)));
            Assert.Equal(unchecked((nint)(int)0xFFFFFFFF), NumberBaseHelper<nint>.CreateTruncating<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateTruncatingFromInt64Test()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0x0000000000000000), NumberBaseHelper<nint>.CreateTruncating<long>(0x0000000000000000));
                Assert.Equal(unchecked((nint)0x0000000000000001), NumberBaseHelper<nint>.CreateTruncating<long>(0x0000000000000001));
                Assert.Equal(unchecked((nint)0x7FFFFFFFFFFFFFFF), NumberBaseHelper<nint>.CreateTruncating<long>(0x7FFFFFFFFFFFFFFF));
                Assert.Equal(unchecked((nint)0x8000000000000000), NumberBaseHelper<nint>.CreateTruncating<long>(unchecked((long)0x8000000000000000)));
                Assert.Equal(unchecked((nint)0xFFFFFFFFFFFFFFFF), NumberBaseHelper<nint>.CreateTruncating<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((nint)0x00000000, NumberBaseHelper<nint>.CreateTruncating<long>(0x0000000000000000));
                Assert.Equal((nint)0x00000001, NumberBaseHelper<nint>.CreateTruncating<long>(0x0000000000000001));
                Assert.Equal(unchecked((nint)0xFFFFFFFF), NumberBaseHelper<nint>.CreateTruncating<long>(0x7FFFFFFFFFFFFFFF));
                Assert.Equal((nint)0x00000000, NumberBaseHelper<nint>.CreateTruncating<long>(unchecked((long)0x8000000000000000)));
                Assert.Equal(unchecked((nint)0xFFFFFFFF), NumberBaseHelper<nint>.CreateTruncating<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateTruncatingFromInt128Test()
        {
            Assert.Equal((nint)0x0000_0000, NumberBaseHelper<nint>.CreateTruncating<Int128>(Int128.Zero));
            Assert.Equal((nint)0x0000_0001, NumberBaseHelper<nint>.CreateTruncating<Int128>(Int128.One));

            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0xFFFF_FFFF_FFFF_FFFF), NumberBaseHelper<nint>.CreateTruncating<Int128>(Int128.MaxValue));
                Assert.Equal(unchecked((nint)0x0000_0000_0000_0000), NumberBaseHelper<nint>.CreateTruncating<Int128>(Int128.MinValue));
                Assert.Equal(unchecked((nint)0xFFFF_FFFF_FFFF_FFFF), NumberBaseHelper<nint>.CreateTruncating<Int128>(Int128.NegativeOne));
            }
            else
            {
                Assert.Equal(unchecked((nint)0xFFFF_FFFF), NumberBaseHelper<nint>.CreateTruncating<Int128>(Int128.MaxValue));
                Assert.Equal(unchecked((nint)0x0000_0000), NumberBaseHelper<nint>.CreateTruncating<Int128>(Int128.MinValue));
                Assert.Equal(unchecked((nint)0xFFFF_FFFF), NumberBaseHelper<nint>.CreateTruncating<Int128>(Int128.NegativeOne));
            }
        }

        [Fact]
        public static void CreateTruncatingFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0x0000000000000000), NumberBaseHelper<nint>.CreateTruncating<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal(unchecked((nint)0x0000000000000001), NumberBaseHelper<nint>.CreateTruncating<nint>(unchecked((nint)0x0000000000000001)));
                Assert.Equal(unchecked((nint)0x7FFFFFFFFFFFFFFF), NumberBaseHelper<nint>.CreateTruncating<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((nint)0x8000000000000000), NumberBaseHelper<nint>.CreateTruncating<nint>(unchecked((nint)0x8000000000000000)));
                Assert.Equal(unchecked((nint)0xFFFFFFFFFFFFFFFF), NumberBaseHelper<nint>.CreateTruncating<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((nint)0x00000000, NumberBaseHelper<nint>.CreateTruncating<nint>((nint)0x00000000));
                Assert.Equal((nint)0x00000001, NumberBaseHelper<nint>.CreateTruncating<nint>((nint)0x00000001));
                Assert.Equal((nint)0x7FFFFFFF, NumberBaseHelper<nint>.CreateTruncating<nint>((nint)0x7FFFFFFF));
                Assert.Equal(unchecked((nint)0x80000000), NumberBaseHelper<nint>.CreateTruncating<nint>(unchecked(unchecked((nint)0x80000000))));
                Assert.Equal(unchecked((nint)0xFFFFFFFF), NumberBaseHelper<nint>.CreateTruncating<nint>(unchecked(unchecked((nint)0xFFFFFFFF))));
            }
        }

        [Fact]
        public static void CreateTruncatingFromNFloatTest()
        {
            Assert.Equal((nint)0x0000_0000, NumberBaseHelper<nint>.CreateTruncating<NFloat>(+0.0f));
            Assert.Equal((nint)0x0000_0000_0000_0000, NumberBaseHelper<nint>.CreateTruncating<NFloat>(-0.0f));

            Assert.Equal((nint)0x0000_0000, NumberBaseHelper<nint>.CreateTruncating<NFloat>(+NFloat.Epsilon));
            Assert.Equal((nint)0x0000_0000, NumberBaseHelper<nint>.CreateTruncating<NFloat>(-NFloat.Epsilon));

            Assert.Equal((nint)0x0000_0001, NumberBaseHelper<nint>.CreateTruncating<NFloat>(+1.0f));

            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0xFFFF_FFFF_FFFF_FFFF), NumberBaseHelper<nint>.CreateTruncating<NFloat>((NFloat)(-1.0)));

                Assert.Equal(unchecked((nint)0x7FFF_FFFF_FFFF_FC00), NumberBaseHelper<nint>.CreateTruncating<NFloat>((NFloat)(+9223372036854774784.0)));
                Assert.Equal(unchecked((nint)0x8000_0000_0000_0000), NumberBaseHelper<nint>.CreateTruncating<NFloat>((NFloat)(-9223372036854775808.0)));

                Assert.Equal(nint.MaxValue, NumberBaseHelper<nint>.CreateTruncating<NFloat>((NFloat)(+9223372036854775808.0)));
                Assert.Equal(nint.MinValue, NumberBaseHelper<nint>.CreateTruncating<NFloat>((NFloat)(-9223372036854777856.0)));
            }
            else
            {
                Assert.Equal(unchecked((nint)0xFFFF_FFFF), NumberBaseHelper<nint>.CreateTruncating<NFloat>(-1.0f));

                Assert.Equal((nint)0x7FFF_FF80, NumberBaseHelper<nint>.CreateTruncating<NFloat>(+2147483520.0f));
                Assert.Equal(unchecked((nint)0x8000_0000), NumberBaseHelper<nint>.CreateTruncating<NFloat>(-2147483648.0f));

                Assert.Equal(nint.MaxValue, NumberBaseHelper<nint>.CreateTruncating<NFloat>(+2147483648.0f));
                Assert.Equal(nint.MinValue, NumberBaseHelper<nint>.CreateTruncating<NFloat>(-2147483904.0f));
            }

            Assert.Equal(nint.MaxValue, NumberBaseHelper<nint>.CreateTruncating<NFloat>(NFloat.MaxValue));
            Assert.Equal(nint.MinValue, NumberBaseHelper<nint>.CreateTruncating<NFloat>(NFloat.MinValue));

            Assert.Equal(nint.MaxValue, NumberBaseHelper<nint>.CreateTruncating<NFloat>(NFloat.PositiveInfinity));
            Assert.Equal(nint.MinValue, NumberBaseHelper<nint>.CreateTruncating<NFloat>(NFloat.NegativeInfinity));
        }

        [Fact]
        public static void CreateTruncatingFromSByteTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0x0000000000000000), NumberBaseHelper<nint>.CreateTruncating<sbyte>(0x00));
                Assert.Equal(unchecked((nint)0x0000000000000001), NumberBaseHelper<nint>.CreateTruncating<sbyte>(0x01));
                Assert.Equal(unchecked((nint)0x000000000000007F), NumberBaseHelper<nint>.CreateTruncating<sbyte>(0x7F));
                Assert.Equal(unchecked((nint)0xFFFFFFFFFFFFFF80), NumberBaseHelper<nint>.CreateTruncating<sbyte>(unchecked((sbyte)0x80)));
                Assert.Equal(unchecked((nint)0xFFFFFFFFFFFFFFFF), NumberBaseHelper<nint>.CreateTruncating<sbyte>(unchecked((sbyte)0xFF)));
            }
            else
            {
                Assert.Equal((nint)0x00000000, NumberBaseHelper<nint>.CreateTruncating<sbyte>(0x00));
                Assert.Equal((nint)0x00000001, NumberBaseHelper<nint>.CreateTruncating<sbyte>(0x01));
                Assert.Equal((nint)0x0000007F, NumberBaseHelper<nint>.CreateTruncating<sbyte>(0x7F));
                Assert.Equal(unchecked((nint)0xFFFFFF80), NumberBaseHelper<nint>.CreateTruncating<sbyte>(unchecked((sbyte)0x80)));
                Assert.Equal(unchecked((nint)0xFFFFFFFF), NumberBaseHelper<nint>.CreateTruncating<sbyte>(unchecked((sbyte)0xFF)));
            }
        }

        [Fact]
        public static void CreateTruncatingFromSingleTest()
        {
            Assert.Equal((nint)0x0000_0000, NumberBaseHelper<nint>.CreateTruncating<float>(+0.0f));
            Assert.Equal((nint)0x0000_0000_0000_0000, NumberBaseHelper<nint>.CreateTruncating<float>(-0.0f));

            Assert.Equal((nint)0x0000_0000, NumberBaseHelper<nint>.CreateTruncating<float>(+float.Epsilon));
            Assert.Equal((nint)0x0000_0000, NumberBaseHelper<nint>.CreateTruncating<float>(-float.Epsilon));

            Assert.Equal((nint)0x0000_0001, NumberBaseHelper<nint>.CreateTruncating<float>(+1.0f));

            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0xFFFF_FFFF_FFFF_FFFF), NumberBaseHelper<nint>.CreateTruncating<float>(-1.0f));

                Assert.Equal(unchecked((nint)0x7FFF_FF80_0000_0000), NumberBaseHelper<nint>.CreateTruncating<float>(+9223371487098961920.0f));
                Assert.Equal(unchecked((nint)0x8000_0000_0000_0000), NumberBaseHelper<nint>.CreateTruncating<float>(-9223372036854775808.0f));

                Assert.Equal(nint.MaxValue, NumberBaseHelper<nint>.CreateTruncating<float>(+9223372036854775808.0f));
                Assert.Equal(nint.MinValue, NumberBaseHelper<nint>.CreateTruncating<float>(-9223373136366403584.0f));
            }
            else
            {
                Assert.Equal(unchecked((nint)0xFFFF_FFFF), NumberBaseHelper<nint>.CreateTruncating<float>(-1.0f));

                Assert.Equal((nint)0x7FFF_FF80, NumberBaseHelper<nint>.CreateTruncating<float>(+2147483520.0f));
                Assert.Equal(unchecked((nint)0x8000_0000), NumberBaseHelper<nint>.CreateTruncating<float>(-2147483648.0f));

                Assert.Equal(nint.MaxValue, NumberBaseHelper<nint>.CreateTruncating<float>(+2147483648.0f));
                Assert.Equal(nint.MinValue, NumberBaseHelper<nint>.CreateTruncating<float>(-2147483904.0f));
            }

            Assert.Equal(nint.MaxValue, NumberBaseHelper<nint>.CreateTruncating<float>(float.MaxValue));
            Assert.Equal(nint.MinValue, NumberBaseHelper<nint>.CreateTruncating<float>(float.MinValue));

            Assert.Equal(nint.MaxValue, NumberBaseHelper<nint>.CreateTruncating<float>(float.PositiveInfinity));
            Assert.Equal(nint.MinValue, NumberBaseHelper<nint>.CreateTruncating<float>(float.NegativeInfinity));
        }

        [Fact]
        public static void CreateTruncatingFromUInt16Test()
        {
            Assert.Equal((nint)0x00000000, NumberBaseHelper<nint>.CreateTruncating<ushort>(0x0000));
            Assert.Equal((nint)0x00000001, NumberBaseHelper<nint>.CreateTruncating<ushort>(0x0001));
            Assert.Equal((nint)0x00007FFF, NumberBaseHelper<nint>.CreateTruncating<ushort>(0x7FFF));
            Assert.Equal((nint)0x00008000, NumberBaseHelper<nint>.CreateTruncating<ushort>(0x8000));
            Assert.Equal((nint)0x0000FFFF, NumberBaseHelper<nint>.CreateTruncating<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt32Test()
        {
            Assert.Equal((nint)0x00000000, NumberBaseHelper<nint>.CreateTruncating<uint>(0x00000000));
            Assert.Equal((nint)0x00000001, NumberBaseHelper<nint>.CreateTruncating<uint>(0x00000001));
            Assert.Equal((nint)0x7FFFFFFF, NumberBaseHelper<nint>.CreateTruncating<uint>(0x7FFFFFFF));
            Assert.Equal(unchecked((nint)0x80000000), NumberBaseHelper<nint>.CreateTruncating<uint>(0x80000000));
            Assert.Equal(unchecked((nint)0xFFFFFFFF), NumberBaseHelper<nint>.CreateTruncating<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt64Test()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0x0000000000000000), NumberBaseHelper<nint>.CreateTruncating<ulong>(0x0000000000000000));
                Assert.Equal(unchecked((nint)0x0000000000000001), NumberBaseHelper<nint>.CreateTruncating<ulong>(0x0000000000000001));
                Assert.Equal(unchecked((nint)0x7FFFFFFFFFFFFFFF), NumberBaseHelper<nint>.CreateTruncating<ulong>(0x7FFFFFFFFFFFFFFF));
                Assert.Equal(unchecked((nint)0x8000000000000000), NumberBaseHelper<nint>.CreateTruncating<ulong>(0x8000000000000000));
                Assert.Equal(unchecked((nint)0xFFFFFFFFFFFFFFFF), NumberBaseHelper<nint>.CreateTruncating<ulong>(0xFFFFFFFFFFFFFFFF));
            }
            else
            {
                Assert.Equal((nint)0x00000000, NumberBaseHelper<nint>.CreateTruncating<ulong>(0x0000000000000000));
                Assert.Equal((nint)0x00000001, NumberBaseHelper<nint>.CreateTruncating<ulong>(0x0000000000000001));
                Assert.Equal(unchecked((nint)0xFFFFFFFF), NumberBaseHelper<nint>.CreateTruncating<ulong>(0x7FFFFFFFFFFFFFFF));
                Assert.Equal((nint)0x00000000, NumberBaseHelper<nint>.CreateTruncating<ulong>(0x8000000000000000));
                Assert.Equal(unchecked((nint)0xFFFFFFFF), NumberBaseHelper<nint>.CreateTruncating<ulong>(0xFFFFFFFFFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateTruncatingFromUInt128Test()
        {
            Assert.Equal((nint)0x0000_0000, NumberBaseHelper<nint>.CreateTruncating<UInt128>(UInt128.Zero));
            Assert.Equal((nint)0x0000_0001, NumberBaseHelper<nint>.CreateTruncating<UInt128>(UInt128.One));

            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0xFFFF_FFFF_FFFF_FFFF), NumberBaseHelper<nint>.CreateTruncating<UInt128>(UInt128Tests_GenericMath.Int128MaxValue));
                Assert.Equal(unchecked((nint)0x0000_0000_0000_0000), NumberBaseHelper<nint>.CreateTruncating<UInt128>(UInt128Tests_GenericMath.Int128MaxValuePlusOne));
                Assert.Equal(unchecked((nint)0xFFFF_FFFF_FFFF_FFFF), NumberBaseHelper<nint>.CreateTruncating<UInt128>(UInt128.MaxValue));
            }
            else
            {
                Assert.Equal(unchecked((nint)0xFFFF_FFFF), NumberBaseHelper<nint>.CreateTruncating<UInt128>(UInt128Tests_GenericMath.Int128MaxValue));
                Assert.Equal(unchecked((nint)0x0000_0000), NumberBaseHelper<nint>.CreateTruncating<UInt128>(UInt128Tests_GenericMath.Int128MaxValuePlusOne));
                Assert.Equal(unchecked((nint)0xFFFF_FFFF), NumberBaseHelper<nint>.CreateTruncating<UInt128>(UInt128.MaxValue));
            }
        }

        [Fact]
        public static void CreateTruncatingFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0x0000000000000000), NumberBaseHelper<nint>.CreateTruncating<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal(unchecked((nint)0x0000000000000001), NumberBaseHelper<nint>.CreateTruncating<nuint>(unchecked((nuint)0x0000000000000001)));
                Assert.Equal(unchecked((nint)0x7FFFFFFFFFFFFFFF), NumberBaseHelper<nint>.CreateTruncating<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((nint)0x8000000000000000), NumberBaseHelper<nint>.CreateTruncating<nuint>(unchecked((nuint)0x8000000000000000)));
                Assert.Equal(unchecked((nint)0xFFFFFFFFFFFFFFFF), NumberBaseHelper<nint>.CreateTruncating<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((nint)0x00000000, NumberBaseHelper<nint>.CreateTruncating<nuint>((nuint)0x00000000));
                Assert.Equal((nint)0x00000001, NumberBaseHelper<nint>.CreateTruncating<nuint>((nuint)0x00000001));
                Assert.Equal((nint)0x7FFFFFFF, NumberBaseHelper<nint>.CreateTruncating<nuint>((nuint)0x7FFFFFFF));
                Assert.Equal(unchecked((nint)0x80000000), NumberBaseHelper<nint>.CreateTruncating<nuint>(unchecked((nuint)0x80000000)));
                Assert.Equal(unchecked((nint)0xFFFFFFFF), NumberBaseHelper<nint>.CreateTruncating<nuint>(unchecked((nuint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void IsCanonicalTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.True(NumberBaseHelper<nint>.IsCanonical(unchecked((nint)0x0000000000000000)));
                Assert.True(NumberBaseHelper<nint>.IsCanonical(unchecked((nint)0x0000000000000001)));
                Assert.True(NumberBaseHelper<nint>.IsCanonical(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.True(NumberBaseHelper<nint>.IsCanonical(unchecked((nint)0x8000000000000000)));
                Assert.True(NumberBaseHelper<nint>.IsCanonical(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.True(NumberBaseHelper<nint>.IsCanonical((nint)0x00000000));
                Assert.True(NumberBaseHelper<nint>.IsCanonical((nint)0x00000001));
                Assert.True(NumberBaseHelper<nint>.IsCanonical((nint)0x7FFFFFFF));
                Assert.True(NumberBaseHelper<nint>.IsCanonical(unchecked((nint)0x80000000)));
                Assert.True(NumberBaseHelper<nint>.IsCanonical(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void IsComplexNumberTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.False(NumberBaseHelper<nint>.IsComplexNumber(unchecked((nint)0x0000000000000000)));
                Assert.False(NumberBaseHelper<nint>.IsComplexNumber(unchecked((nint)0x0000000000000001)));
                Assert.False(NumberBaseHelper<nint>.IsComplexNumber(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.False(NumberBaseHelper<nint>.IsComplexNumber(unchecked((nint)0x8000000000000000)));
                Assert.False(NumberBaseHelper<nint>.IsComplexNumber(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.False(NumberBaseHelper<nint>.IsComplexNumber((nint)0x00000000));
                Assert.False(NumberBaseHelper<nint>.IsComplexNumber((nint)0x00000001));
                Assert.False(NumberBaseHelper<nint>.IsComplexNumber((nint)0x7FFFFFFF));
                Assert.False(NumberBaseHelper<nint>.IsComplexNumber(unchecked((nint)0x80000000)));
                Assert.False(NumberBaseHelper<nint>.IsComplexNumber(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void IsEvenIntegerTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.True(NumberBaseHelper<nint>.IsEvenInteger(unchecked((nint)0x0000000000000000)));
                Assert.False(NumberBaseHelper<nint>.IsEvenInteger(unchecked((nint)0x0000000000000001)));
                Assert.False(NumberBaseHelper<nint>.IsEvenInteger(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.True(NumberBaseHelper<nint>.IsEvenInteger(unchecked((nint)0x8000000000000000)));
                Assert.False(NumberBaseHelper<nint>.IsEvenInteger(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.True(NumberBaseHelper<nint>.IsEvenInteger((nint)0x00000000));
                Assert.False(NumberBaseHelper<nint>.IsEvenInteger((nint)0x00000001));
                Assert.False(NumberBaseHelper<nint>.IsEvenInteger((nint)0x7FFFFFFF));
                Assert.True(NumberBaseHelper<nint>.IsEvenInteger(unchecked((nint)0x80000000)));
                Assert.False(NumberBaseHelper<nint>.IsEvenInteger(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void IsFiniteTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.True(NumberBaseHelper<nint>.IsFinite(unchecked((nint)0x0000000000000000)));
                Assert.True(NumberBaseHelper<nint>.IsFinite(unchecked((nint)0x0000000000000001)));
                Assert.True(NumberBaseHelper<nint>.IsFinite(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.True(NumberBaseHelper<nint>.IsFinite(unchecked((nint)0x8000000000000000)));
                Assert.True(NumberBaseHelper<nint>.IsFinite(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.True(NumberBaseHelper<nint>.IsFinite((nint)0x00000000));
                Assert.True(NumberBaseHelper<nint>.IsFinite((nint)0x00000001));
                Assert.True(NumberBaseHelper<nint>.IsFinite((nint)0x7FFFFFFF));
                Assert.True(NumberBaseHelper<nint>.IsFinite(unchecked((nint)0x80000000)));
                Assert.True(NumberBaseHelper<nint>.IsFinite(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void IsImaginaryNumberTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.False(NumberBaseHelper<nint>.IsImaginaryNumber(unchecked((nint)0x0000000000000000)));
                Assert.False(NumberBaseHelper<nint>.IsImaginaryNumber(unchecked((nint)0x0000000000000001)));
                Assert.False(NumberBaseHelper<nint>.IsImaginaryNumber(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.False(NumberBaseHelper<nint>.IsImaginaryNumber(unchecked((nint)0x8000000000000000)));
                Assert.False(NumberBaseHelper<nint>.IsImaginaryNumber(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.False(NumberBaseHelper<nint>.IsImaginaryNumber((nint)0x00000000));
                Assert.False(NumberBaseHelper<nint>.IsImaginaryNumber((nint)0x00000001));
                Assert.False(NumberBaseHelper<nint>.IsImaginaryNumber((nint)0x7FFFFFFF));
                Assert.False(NumberBaseHelper<nint>.IsImaginaryNumber(unchecked((nint)0x80000000)));
                Assert.False(NumberBaseHelper<nint>.IsImaginaryNumber(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void IsInfinityTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.False(NumberBaseHelper<nint>.IsInfinity(unchecked((nint)0x0000000000000000)));
                Assert.False(NumberBaseHelper<nint>.IsInfinity(unchecked((nint)0x0000000000000001)));
                Assert.False(NumberBaseHelper<nint>.IsInfinity(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.False(NumberBaseHelper<nint>.IsInfinity(unchecked((nint)0x8000000000000000)));
                Assert.False(NumberBaseHelper<nint>.IsInfinity(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.False(NumberBaseHelper<nint>.IsInfinity((nint)0x00000000));
                Assert.False(NumberBaseHelper<nint>.IsInfinity((nint)0x00000001));
                Assert.False(NumberBaseHelper<nint>.IsInfinity((nint)0x7FFFFFFF));
                Assert.False(NumberBaseHelper<nint>.IsInfinity(unchecked((nint)0x80000000)));
                Assert.False(NumberBaseHelper<nint>.IsInfinity(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void IsIntegerTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.True(NumberBaseHelper<nint>.IsInteger(unchecked((nint)0x0000000000000000)));
                Assert.True(NumberBaseHelper<nint>.IsInteger(unchecked((nint)0x0000000000000001)));
                Assert.True(NumberBaseHelper<nint>.IsInteger(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.True(NumberBaseHelper<nint>.IsInteger(unchecked((nint)0x8000000000000000)));
                Assert.True(NumberBaseHelper<nint>.IsInteger(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.True(NumberBaseHelper<nint>.IsInteger((nint)0x00000000));
                Assert.True(NumberBaseHelper<nint>.IsInteger((nint)0x00000001));
                Assert.True(NumberBaseHelper<nint>.IsInteger((nint)0x7FFFFFFF));
                Assert.True(NumberBaseHelper<nint>.IsInteger(unchecked((nint)0x80000000)));
                Assert.True(NumberBaseHelper<nint>.IsInteger(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void IsNaNTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.False(NumberBaseHelper<nint>.IsNaN(unchecked((nint)0x0000000000000000)));
                Assert.False(NumberBaseHelper<nint>.IsNaN(unchecked((nint)0x0000000000000001)));
                Assert.False(NumberBaseHelper<nint>.IsNaN(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.False(NumberBaseHelper<nint>.IsNaN(unchecked((nint)0x8000000000000000)));
                Assert.False(NumberBaseHelper<nint>.IsNaN(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.False(NumberBaseHelper<nint>.IsNaN((nint)0x00000000));
                Assert.False(NumberBaseHelper<nint>.IsNaN((nint)0x00000001));
                Assert.False(NumberBaseHelper<nint>.IsNaN((nint)0x7FFFFFFF));
                Assert.False(NumberBaseHelper<nint>.IsNaN(unchecked((nint)0x80000000)));
                Assert.False(NumberBaseHelper<nint>.IsNaN(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void IsNegativeTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.False(NumberBaseHelper<nint>.IsNegative(unchecked((nint)0x0000000000000000)));
                Assert.False(NumberBaseHelper<nint>.IsNegative(unchecked((nint)0x0000000000000001)));
                Assert.False(NumberBaseHelper<nint>.IsNegative(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.True(NumberBaseHelper<nint>.IsNegative(unchecked((nint)0x8000000000000000)));
                Assert.True(NumberBaseHelper<nint>.IsNegative(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.False(NumberBaseHelper<nint>.IsNegative((nint)0x00000000));
                Assert.False(NumberBaseHelper<nint>.IsNegative((nint)0x00000001));
                Assert.False(NumberBaseHelper<nint>.IsNegative((nint)0x7FFFFFFF));
                Assert.True(NumberBaseHelper<nint>.IsNegative(unchecked((nint)0x80000000)));
                Assert.True(NumberBaseHelper<nint>.IsNegative(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void IsNegativeInfinityTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.False(NumberBaseHelper<nint>.IsNegativeInfinity(unchecked((nint)0x0000000000000000)));
                Assert.False(NumberBaseHelper<nint>.IsNegativeInfinity(unchecked((nint)0x0000000000000001)));
                Assert.False(NumberBaseHelper<nint>.IsNegativeInfinity(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.False(NumberBaseHelper<nint>.IsNegativeInfinity(unchecked((nint)0x8000000000000000)));
                Assert.False(NumberBaseHelper<nint>.IsNegativeInfinity(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.False(NumberBaseHelper<nint>.IsNegativeInfinity((nint)0x00000000));
                Assert.False(NumberBaseHelper<nint>.IsNegativeInfinity((nint)0x00000001));
                Assert.False(NumberBaseHelper<nint>.IsNegativeInfinity((nint)0x7FFFFFFF));
                Assert.False(NumberBaseHelper<nint>.IsNegativeInfinity(unchecked((nint)0x80000000)));
                Assert.False(NumberBaseHelper<nint>.IsNegativeInfinity(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void IsNormalTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.False(NumberBaseHelper<nint>.IsNormal(unchecked((nint)0x0000000000000000)));
                Assert.True(NumberBaseHelper<nint>.IsNormal(unchecked((nint)0x0000000000000001)));
                Assert.True(NumberBaseHelper<nint>.IsNormal(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.True(NumberBaseHelper<nint>.IsNormal(unchecked((nint)0x8000000000000000)));
                Assert.True(NumberBaseHelper<nint>.IsNormal(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.False(NumberBaseHelper<nint>.IsNormal((nint)0x00000000));
                Assert.True(NumberBaseHelper<nint>.IsNormal((nint)0x00000001));
                Assert.True(NumberBaseHelper<nint>.IsNormal((nint)0x7FFFFFFF));
                Assert.True(NumberBaseHelper<nint>.IsNormal(unchecked((nint)0x80000000)));
                Assert.True(NumberBaseHelper<nint>.IsNormal(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void IsOddIntegerTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.False(NumberBaseHelper<nint>.IsOddInteger(unchecked((nint)0x0000000000000000)));
                Assert.True(NumberBaseHelper<nint>.IsOddInteger(unchecked((nint)0x0000000000000001)));
                Assert.True(NumberBaseHelper<nint>.IsOddInteger(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.False(NumberBaseHelper<nint>.IsOddInteger(unchecked((nint)0x8000000000000000)));
                Assert.True(NumberBaseHelper<nint>.IsOddInteger(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.False(NumberBaseHelper<nint>.IsOddInteger((nint)0x00000000));
                Assert.True(NumberBaseHelper<nint>.IsOddInteger((nint)0x00000001));
                Assert.True(NumberBaseHelper<nint>.IsOddInteger((nint)0x7FFFFFFF));
                Assert.False(NumberBaseHelper<nint>.IsOddInteger(unchecked((nint)0x80000000)));
                Assert.True(NumberBaseHelper<nint>.IsOddInteger(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void IsPositiveTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.True(NumberBaseHelper<nint>.IsPositive(unchecked((nint)0x0000000000000000)));
                Assert.True(NumberBaseHelper<nint>.IsPositive(unchecked((nint)0x0000000000000001)));
                Assert.True(NumberBaseHelper<nint>.IsPositive(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.False(NumberBaseHelper<nint>.IsPositive(unchecked((nint)0x8000000000000000)));
                Assert.False(NumberBaseHelper<nint>.IsPositive(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.True(NumberBaseHelper<nint>.IsPositive((nint)0x00000000));
                Assert.True(NumberBaseHelper<nint>.IsPositive((nint)0x00000001));
                Assert.True(NumberBaseHelper<nint>.IsPositive((nint)0x7FFFFFFF));
                Assert.False(NumberBaseHelper<nint>.IsPositive(unchecked((nint)0x80000000)));
                Assert.False(NumberBaseHelper<nint>.IsPositive(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void IsPositiveInfinityTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.False(NumberBaseHelper<nint>.IsPositiveInfinity(unchecked((nint)0x0000000000000000)));
                Assert.False(NumberBaseHelper<nint>.IsPositiveInfinity(unchecked((nint)0x0000000000000001)));
                Assert.False(NumberBaseHelper<nint>.IsPositiveInfinity(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.False(NumberBaseHelper<nint>.IsPositiveInfinity(unchecked((nint)0x8000000000000000)));
                Assert.False(NumberBaseHelper<nint>.IsPositiveInfinity(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.False(NumberBaseHelper<nint>.IsPositiveInfinity((nint)0x00000000));
                Assert.False(NumberBaseHelper<nint>.IsPositiveInfinity((nint)0x00000001));
                Assert.False(NumberBaseHelper<nint>.IsPositiveInfinity((nint)0x7FFFFFFF));
                Assert.False(NumberBaseHelper<nint>.IsPositiveInfinity(unchecked((nint)0x80000000)));
                Assert.False(NumberBaseHelper<nint>.IsPositiveInfinity(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void IsRealNumberTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.True(NumberBaseHelper<nint>.IsRealNumber(unchecked((nint)0x0000000000000000)));
                Assert.True(NumberBaseHelper<nint>.IsRealNumber(unchecked((nint)0x0000000000000001)));
                Assert.True(NumberBaseHelper<nint>.IsRealNumber(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.True(NumberBaseHelper<nint>.IsRealNumber(unchecked((nint)0x8000000000000000)));
                Assert.True(NumberBaseHelper<nint>.IsRealNumber(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.True(NumberBaseHelper<nint>.IsRealNumber((nint)0x00000000));
                Assert.True(NumberBaseHelper<nint>.IsRealNumber((nint)0x00000001));
                Assert.True(NumberBaseHelper<nint>.IsRealNumber((nint)0x7FFFFFFF));
                Assert.True(NumberBaseHelper<nint>.IsRealNumber(unchecked((nint)0x80000000)));
                Assert.True(NumberBaseHelper<nint>.IsRealNumber(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void IsSubnormalTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.False(NumberBaseHelper<nint>.IsSubnormal(unchecked((nint)0x0000000000000000)));
                Assert.False(NumberBaseHelper<nint>.IsSubnormal(unchecked((nint)0x0000000000000001)));
                Assert.False(NumberBaseHelper<nint>.IsSubnormal(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.False(NumberBaseHelper<nint>.IsSubnormal(unchecked((nint)0x8000000000000000)));
                Assert.False(NumberBaseHelper<nint>.IsSubnormal(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.False(NumberBaseHelper<nint>.IsSubnormal((nint)0x00000000));
                Assert.False(NumberBaseHelper<nint>.IsSubnormal((nint)0x00000001));
                Assert.False(NumberBaseHelper<nint>.IsSubnormal((nint)0x7FFFFFFF));
                Assert.False(NumberBaseHelper<nint>.IsSubnormal(unchecked((nint)0x80000000)));
                Assert.False(NumberBaseHelper<nint>.IsSubnormal(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void IsZeroTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.True(NumberBaseHelper<nint>.IsZero(unchecked((nint)0x0000000000000000)));
                Assert.False(NumberBaseHelper<nint>.IsZero(unchecked((nint)0x0000000000000001)));
                Assert.False(NumberBaseHelper<nint>.IsZero(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.False(NumberBaseHelper<nint>.IsZero(unchecked((nint)0x8000000000000000)));
                Assert.False(NumberBaseHelper<nint>.IsZero(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.True(NumberBaseHelper<nint>.IsZero((nint)0x00000000));
                Assert.False(NumberBaseHelper<nint>.IsZero((nint)0x00000001));
                Assert.False(NumberBaseHelper<nint>.IsZero((nint)0x7FFFFFFF));
                Assert.False(NumberBaseHelper<nint>.IsZero(unchecked((nint)0x80000000)));
                Assert.False(NumberBaseHelper<nint>.IsZero(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void MaxMagnitudeTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0x0000000000000001), NumberBaseHelper<nint>.MaxMagnitude(unchecked((nint)0x0000000000000000), (nint)1));
                Assert.Equal(unchecked((nint)0x0000000000000001), NumberBaseHelper<nint>.MaxMagnitude(unchecked((nint)0x0000000000000001), (nint)1));
                Assert.Equal(unchecked((nint)0x7FFFFFFFFFFFFFFF), NumberBaseHelper<nint>.MaxMagnitude(unchecked((nint)0x7FFFFFFFFFFFFFFF), (nint)1));
                Assert.Equal(unchecked((nint)0x8000000000000000), NumberBaseHelper<nint>.MaxMagnitude(unchecked((nint)0x8000000000000000), (nint)1));
                Assert.Equal(unchecked((nint)0x0000000000000001), NumberBaseHelper<nint>.MaxMagnitude(unchecked((nint)0xFFFFFFFFFFFFFFFF), (nint)1));
            }
            else
            {
                Assert.Equal((nint)0x00000001, NumberBaseHelper<nint>.MaxMagnitude((nint)0x00000000, (nint)1));
                Assert.Equal((nint)0x00000001, NumberBaseHelper<nint>.MaxMagnitude((nint)0x00000001, (nint)1));
                Assert.Equal((nint)0x7FFFFFFF, NumberBaseHelper<nint>.MaxMagnitude((nint)0x7FFFFFFF, (nint)1));
                Assert.Equal(unchecked((nint)0x80000000), NumberBaseHelper<nint>.MaxMagnitude(unchecked((nint)0x80000000), (nint)1));
                Assert.Equal((nint)0x00000001, NumberBaseHelper<nint>.MaxMagnitude(unchecked((nint)0xFFFFFFFF), (nint)1));
            }
        }

        [Fact]
        public static void MaxMagnitudeNumberTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0x0000000000000001), NumberBaseHelper<nint>.MaxMagnitudeNumber(unchecked((nint)0x0000000000000000), (nint)1));
                Assert.Equal(unchecked((nint)0x0000000000000001), NumberBaseHelper<nint>.MaxMagnitudeNumber(unchecked((nint)0x0000000000000001), (nint)1));
                Assert.Equal(unchecked((nint)0x7FFFFFFFFFFFFFFF), NumberBaseHelper<nint>.MaxMagnitudeNumber(unchecked((nint)0x7FFFFFFFFFFFFFFF), (nint)1));
                Assert.Equal(unchecked((nint)0x8000000000000000), NumberBaseHelper<nint>.MaxMagnitudeNumber(unchecked((nint)0x8000000000000000), (nint)1));
                Assert.Equal(unchecked((nint)0x0000000000000001), NumberBaseHelper<nint>.MaxMagnitudeNumber(unchecked((nint)0xFFFFFFFFFFFFFFFF), (nint)1));
            }
            else
            {
                Assert.Equal((nint)0x00000001, NumberBaseHelper<nint>.MaxMagnitudeNumber((nint)0x00000000, (nint)1));
                Assert.Equal((nint)0x00000001, NumberBaseHelper<nint>.MaxMagnitudeNumber((nint)0x00000001, (nint)1));
                Assert.Equal((nint)0x7FFFFFFF, NumberBaseHelper<nint>.MaxMagnitudeNumber((nint)0x7FFFFFFF, (nint)1));
                Assert.Equal(unchecked((nint)0x80000000), NumberBaseHelper<nint>.MaxMagnitudeNumber(unchecked((nint)0x80000000), (nint)1));
                Assert.Equal((nint)0x00000001, NumberBaseHelper<nint>.MaxMagnitudeNumber(unchecked((nint)0xFFFFFFFF), (nint)1));
            }
        }

        [Fact]
        public static void MinMagnitudeTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0x0000000000000000), NumberBaseHelper<nint>.MinMagnitude(unchecked((nint)0x0000000000000000), (nint)1));
                Assert.Equal(unchecked((nint)0x0000000000000001), NumberBaseHelper<nint>.MinMagnitude(unchecked((nint)0x0000000000000001), (nint)1));
                Assert.Equal(unchecked((nint)0x0000000000000001), NumberBaseHelper<nint>.MinMagnitude(unchecked((nint)0x7FFFFFFFFFFFFFFF), (nint)1));
                Assert.Equal(unchecked((nint)0x0000000000000001), NumberBaseHelper<nint>.MinMagnitude(unchecked((nint)0x8000000000000000), (nint)1));
                Assert.Equal(unchecked((nint)0xFFFFFFFFFFFFFFFF), NumberBaseHelper<nint>.MinMagnitude(unchecked((nint)0xFFFFFFFFFFFFFFFF), (nint)1));
            }
            else
            {
                Assert.Equal((nint)0x00000000, NumberBaseHelper<nint>.MinMagnitude((nint)0x00000000, (nint)1));
                Assert.Equal((nint)0x00000001, NumberBaseHelper<nint>.MinMagnitude((nint)0x00000001, (nint)1));
                Assert.Equal((nint)0x00000001, NumberBaseHelper<nint>.MinMagnitude((nint)0x7FFFFFFF, (nint)1));
                Assert.Equal((nint)0x00000001, NumberBaseHelper<nint>.MinMagnitude(unchecked((nint)0x80000000), (nint)1));
                Assert.Equal(unchecked((nint)0xFFFFFFFF), NumberBaseHelper<nint>.MinMagnitude(unchecked((nint)0xFFFFFFFF), (nint)1));
            }
        }

        [Fact]
        public static void MinMagnitudeNumberTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0x0000000000000000), NumberBaseHelper<nint>.MinMagnitudeNumber(unchecked((nint)0x0000000000000000), (nint)1));
                Assert.Equal(unchecked((nint)0x0000000000000001), NumberBaseHelper<nint>.MinMagnitudeNumber(unchecked((nint)0x0000000000000001), (nint)1));
                Assert.Equal(unchecked((nint)0x0000000000000001), NumberBaseHelper<nint>.MinMagnitudeNumber(unchecked((nint)0x7FFFFFFFFFFFFFFF), (nint)1));
                Assert.Equal(unchecked((nint)0x0000000000000001), NumberBaseHelper<nint>.MinMagnitudeNumber(unchecked((nint)0x8000000000000000), (nint)1));
                Assert.Equal(unchecked((nint)0xFFFFFFFFFFFFFFFF), NumberBaseHelper<nint>.MinMagnitudeNumber(unchecked((nint)0xFFFFFFFFFFFFFFFF), (nint)1));
            }
            else
            {
                Assert.Equal((nint)0x00000000, NumberBaseHelper<nint>.MinMagnitudeNumber((nint)0x00000000, (nint)1));
                Assert.Equal((nint)0x00000001, NumberBaseHelper<nint>.MinMagnitudeNumber((nint)0x00000001, (nint)1));
                Assert.Equal((nint)0x00000001, NumberBaseHelper<nint>.MinMagnitudeNumber((nint)0x7FFFFFFF, (nint)1));
                Assert.Equal((nint)0x00000001, NumberBaseHelper<nint>.MinMagnitudeNumber(unchecked((nint)0x80000000), (nint)1));
                Assert.Equal(unchecked((nint)0xFFFFFFFF), NumberBaseHelper<nint>.MinMagnitudeNumber(unchecked((nint)0xFFFFFFFF), (nint)1));
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
                Assert.Equal(unchecked((nint)0x0000000000000000), ShiftOperatorsHelper<nint, nint>.op_LeftShift(unchecked((nint)0x0000000000000000), 1));
                Assert.Equal(unchecked((nint)0x0000000000000002), ShiftOperatorsHelper<nint, nint>.op_LeftShift(unchecked((nint)0x0000000000000001), 1));
                Assert.Equal(unchecked((nint)0xFFFFFFFFFFFFFFFE), ShiftOperatorsHelper<nint, nint>.op_LeftShift(unchecked((nint)0x7FFFFFFFFFFFFFFF), 1));
                Assert.Equal(unchecked((nint)0x0000000000000000), ShiftOperatorsHelper<nint, nint>.op_LeftShift(unchecked((nint)0x8000000000000000), 1));
                Assert.Equal(unchecked((nint)0xFFFFFFFFFFFFFFFE), ShiftOperatorsHelper<nint, nint>.op_LeftShift(unchecked((nint)0xFFFFFFFFFFFFFFFF), 1));
            }
            else
            {
                Assert.Equal((nint)0x00000000, ShiftOperatorsHelper<nint, nint>.op_LeftShift((nint)0x00000000, 1));
                Assert.Equal((nint)0x00000002, ShiftOperatorsHelper<nint, nint>.op_LeftShift((nint)0x00000001, 1));
                Assert.Equal(unchecked((nint)0xFFFFFFFE), ShiftOperatorsHelper<nint, nint>.op_LeftShift((nint)0x7FFFFFFF, 1));
                Assert.Equal((nint)0x00000000, ShiftOperatorsHelper<nint, nint>.op_LeftShift(unchecked((nint)0x80000000), 1));
                Assert.Equal(unchecked((nint)0xFFFFFFFE), ShiftOperatorsHelper<nint, nint>.op_LeftShift(unchecked((nint)0xFFFFFFFF), 1));
            }
        }

        [Fact]
        public static void op_RightShiftTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0x0000000000000000), ShiftOperatorsHelper<nint, nint>.op_RightShift(unchecked((nint)0x0000000000000000), 1));
                Assert.Equal(unchecked((nint)0x0000000000000000), ShiftOperatorsHelper<nint, nint>.op_RightShift(unchecked((nint)0x0000000000000001), 1));
                Assert.Equal(unchecked((nint)0x3FFFFFFFFFFFFFFF), ShiftOperatorsHelper<nint, nint>.op_RightShift(unchecked((nint)0x7FFFFFFFFFFFFFFF), 1));
                Assert.Equal(unchecked((nint)0xC000000000000000), ShiftOperatorsHelper<nint, nint>.op_RightShift(unchecked((nint)0x8000000000000000), 1));
                Assert.Equal(unchecked((nint)0xFFFFFFFFFFFFFFFF), ShiftOperatorsHelper<nint, nint>.op_RightShift(unchecked((nint)0xFFFFFFFFFFFFFFFF), 1));
            }
            else
            {
                Assert.Equal((nint)0x00000000, ShiftOperatorsHelper<nint, nint>.op_RightShift((nint)0x00000000, 1));
                Assert.Equal((nint)0x00000000, ShiftOperatorsHelper<nint, nint>.op_RightShift((nint)0x00000001, 1));
                Assert.Equal((nint)0x3FFFFFFF, ShiftOperatorsHelper<nint, nint>.op_RightShift((nint)0x7FFFFFFF, 1));
                Assert.Equal(unchecked((nint)0xC0000000), ShiftOperatorsHelper<nint, nint>.op_RightShift(unchecked((nint)0x80000000), 1));
                Assert.Equal(unchecked((nint)0xFFFFFFFF), ShiftOperatorsHelper<nint, nint>.op_RightShift(unchecked((nint)0xFFFFFFFF), 1));
            }
        }

        [Fact]
        public static void op_UnsignedRightShiftTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0x0000000000000000), ShiftOperatorsHelper<nint, nint>.op_UnsignedRightShift(unchecked((nint)0x0000000000000000), 1));
                Assert.Equal(unchecked((nint)0x0000000000000000), ShiftOperatorsHelper<nint, nint>.op_UnsignedRightShift(unchecked((nint)0x0000000000000001), 1));
                Assert.Equal(unchecked((nint)0x3FFFFFFFFFFFFFFF), ShiftOperatorsHelper<nint, nint>.op_UnsignedRightShift(unchecked((nint)0x7FFFFFFFFFFFFFFF), 1));
                Assert.Equal(unchecked((nint)0x4000000000000000), ShiftOperatorsHelper<nint, nint>.op_UnsignedRightShift(unchecked((nint)0x8000000000000000), 1));
                Assert.Equal(unchecked((nint)0x7FFFFFFFFFFFFFFF), ShiftOperatorsHelper<nint, nint>.op_UnsignedRightShift(unchecked((nint)0xFFFFFFFFFFFFFFFF), 1));
            }
            else
            {
                Assert.Equal((nint)0x00000000, ShiftOperatorsHelper<nint, nint>.op_UnsignedRightShift((nint)0x00000000, 1));
                Assert.Equal((nint)0x00000000, ShiftOperatorsHelper<nint, nint>.op_UnsignedRightShift((nint)0x00000001, 1));
                Assert.Equal((nint)0x3FFFFFFF, ShiftOperatorsHelper<nint, nint>.op_UnsignedRightShift((nint)0x7FFFFFFF, 1));
                Assert.Equal((nint)0x40000000, ShiftOperatorsHelper<nint, nint>.op_UnsignedRightShift(unchecked((nint)0x80000000), 1));
                Assert.Equal((nint)0x7FFFFFFF, ShiftOperatorsHelper<nint, nint>.op_UnsignedRightShift(unchecked((nint)0xFFFFFFFF), 1));
            }
        }

        //
        // ISignedNumber
        //

        [Fact]
        public static void NegativeOneTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0xFFFFFFFFFFFFFFFF), SignedNumberHelper<nint>.NegativeOne);
            }
            else
            {
                Assert.Equal(unchecked((nint)0xFFFFFFFF), SignedNumberHelper<nint>.NegativeOne);
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
                Assert.Equal(unchecked((nint)0xFFFFFFFFFFFFFFFF), SubtractionOperatorsHelper<nint, nint, nint>.op_Subtraction(unchecked((nint)0x0000000000000000), (nint)1));
                Assert.Equal(unchecked((nint)0x0000000000000000), SubtractionOperatorsHelper<nint, nint, nint>.op_Subtraction(unchecked((nint)0x0000000000000001), (nint)1));
                Assert.Equal(unchecked((nint)0x7FFFFFFFFFFFFFFE), SubtractionOperatorsHelper<nint, nint, nint>.op_Subtraction(unchecked((nint)0x7FFFFFFFFFFFFFFF), (nint)1));
                Assert.Equal(unchecked((nint)0x7FFFFFFFFFFFFFFF), SubtractionOperatorsHelper<nint, nint, nint>.op_Subtraction(unchecked((nint)0x8000000000000000), (nint)1));
                Assert.Equal(unchecked((nint)0xFFFFFFFFFFFFFFFE), SubtractionOperatorsHelper<nint, nint, nint>.op_Subtraction(unchecked((nint)0xFFFFFFFFFFFFFFFF), (nint)1));
            }
            else
            {
                Assert.Equal(unchecked((nint)0xFFFFFFFF), SubtractionOperatorsHelper<nint, nint, nint>.op_Subtraction((nint)0x00000000, (nint)1));
                Assert.Equal((nint)0x00000000, SubtractionOperatorsHelper<nint, nint, nint>.op_Subtraction((nint)0x00000001, (nint)1));
                Assert.Equal((nint)0x7FFFFFFE, SubtractionOperatorsHelper<nint, nint, nint>.op_Subtraction((nint)0x7FFFFFFF, (nint)1));
                Assert.Equal((nint)0x7FFFFFFF, SubtractionOperatorsHelper<nint, nint, nint>.op_Subtraction(unchecked((nint)0x80000000), (nint)1));
                Assert.Equal(unchecked((nint)0xFFFFFFFE), SubtractionOperatorsHelper<nint, nint, nint>.op_Subtraction(unchecked((nint)0xFFFFFFFF), (nint)1));
            }
        }

        [Fact]
        public static void op_CheckedSubtractionTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0xFFFFFFFFFFFFFFFF), SubtractionOperatorsHelper<nint, nint, nint>.op_CheckedSubtraction(unchecked((nint)0x0000000000000000), (nint)1));
                Assert.Equal(unchecked((nint)0x0000000000000000), SubtractionOperatorsHelper<nint, nint, nint>.op_CheckedSubtraction(unchecked((nint)0x0000000000000001), (nint)1));
                Assert.Equal(unchecked((nint)0x7FFFFFFFFFFFFFFE), SubtractionOperatorsHelper<nint, nint, nint>.op_CheckedSubtraction(unchecked((nint)0x7FFFFFFFFFFFFFFF), (nint)1));
                Assert.Equal(unchecked((nint)0xFFFFFFFFFFFFFFFE), SubtractionOperatorsHelper<nint, nint, nint>.op_CheckedSubtraction(unchecked((nint)0xFFFFFFFFFFFFFFFF), (nint)1));

                Assert.Throws<OverflowException>(() => SubtractionOperatorsHelper<nint, nint, nint>.op_CheckedSubtraction(unchecked((nint)0x8000000000000000), (nint)1));
            }
            else
            {
                Assert.Equal(unchecked((nint)0xFFFFFFFF), SubtractionOperatorsHelper<nint, nint, nint>.op_CheckedSubtraction((nint)0x00000000, (nint)1));
                Assert.Equal((nint)0x00000000, SubtractionOperatorsHelper<nint, nint, nint>.op_CheckedSubtraction((nint)0x00000001, (nint)1));
                Assert.Equal((nint)0x7FFFFFFE, SubtractionOperatorsHelper<nint, nint, nint>.op_CheckedSubtraction((nint)0x7FFFFFFF, (nint)1));
                Assert.Equal(unchecked((nint)0xFFFFFFFE), SubtractionOperatorsHelper<nint, nint, nint>.op_CheckedSubtraction(unchecked((nint)0xFFFFFFFF), (nint)1));

                Assert.Throws<OverflowException>(() => SubtractionOperatorsHelper<nint, nint, nint>.op_CheckedSubtraction(unchecked((nint)0x80000000), (nint)1));
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
                Assert.Equal(unchecked((nint)0x0000000000000000), UnaryNegationOperatorsHelper<nint, nint>.op_UnaryNegation(unchecked((nint)0x0000000000000000)));
                Assert.Equal(unchecked((nint)0xFFFFFFFFFFFFFFFF), UnaryNegationOperatorsHelper<nint, nint>.op_UnaryNegation(unchecked((nint)0x0000000000000001)));
                Assert.Equal(unchecked((nint)0x8000000000000001), UnaryNegationOperatorsHelper<nint, nint>.op_UnaryNegation(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((nint)0x8000000000000000), UnaryNegationOperatorsHelper<nint, nint>.op_UnaryNegation(unchecked((nint)0x8000000000000000)));
                Assert.Equal(unchecked((nint)0x0000000000000001), UnaryNegationOperatorsHelper<nint, nint>.op_UnaryNegation(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((nint)0x00000000, UnaryNegationOperatorsHelper<nint, nint>.op_UnaryNegation((nint)0x00000000));
                Assert.Equal(unchecked((nint)0xFFFFFFFF), UnaryNegationOperatorsHelper<nint, nint>.op_UnaryNegation((nint)0x00000001));
                Assert.Equal(unchecked((nint)0x80000001), UnaryNegationOperatorsHelper<nint, nint>.op_UnaryNegation((nint)0x7FFFFFFF));
                Assert.Equal(unchecked((nint)0x80000000), UnaryNegationOperatorsHelper<nint, nint>.op_UnaryNegation(unchecked((nint)0x80000000)));
                Assert.Equal((nint)0x00000001, UnaryNegationOperatorsHelper<nint, nint>.op_UnaryNegation(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void op_CheckedUnaryNegationTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0x0000000000000000), UnaryNegationOperatorsHelper<nint, nint>.op_CheckedUnaryNegation(unchecked((nint)0x0000000000000000)));
                Assert.Equal(unchecked((nint)0xFFFFFFFFFFFFFFFF), UnaryNegationOperatorsHelper<nint, nint>.op_CheckedUnaryNegation(unchecked((nint)0x0000000000000001)));
                Assert.Equal(unchecked((nint)0x8000000000000001), UnaryNegationOperatorsHelper<nint, nint>.op_CheckedUnaryNegation(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((nint)0x0000000000000001), UnaryNegationOperatorsHelper<nint, nint>.op_CheckedUnaryNegation(unchecked((nint)0xFFFFFFFFFFFFFFFF)));

                Assert.Throws<OverflowException>(() => UnaryNegationOperatorsHelper<nint, nint>.op_CheckedUnaryNegation(unchecked((nint)0x8000000000000000)));
            }
            else
            {
                Assert.Equal((nint)0x00000000, UnaryNegationOperatorsHelper<nint, nint>.op_CheckedUnaryNegation((nint)0x00000000));
                Assert.Equal(unchecked((nint)0xFFFFFFFF), UnaryNegationOperatorsHelper<nint, nint>.op_CheckedUnaryNegation((nint)0x00000001));
                Assert.Equal(unchecked((nint)0x80000001), UnaryNegationOperatorsHelper<nint, nint>.op_CheckedUnaryNegation((nint)0x7FFFFFFF));
                Assert.Equal((nint)0x00000001, UnaryNegationOperatorsHelper<nint, nint>.op_CheckedUnaryNegation(unchecked((nint)0xFFFFFFFF)));

                Assert.Throws<OverflowException>(() => UnaryNegationOperatorsHelper<nint, nint>.op_CheckedUnaryNegation(unchecked((nint)0x80000000)));
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
                Assert.Equal(unchecked((nint)0x0000000000000000), UnaryPlusOperatorsHelper<nint, nint>.op_UnaryPlus(unchecked((nint)0x0000000000000000)));
                Assert.Equal(unchecked((nint)0x0000000000000001), UnaryPlusOperatorsHelper<nint, nint>.op_UnaryPlus(unchecked((nint)0x0000000000000001)));
                Assert.Equal(unchecked((nint)0x7FFFFFFFFFFFFFFF), UnaryPlusOperatorsHelper<nint, nint>.op_UnaryPlus(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((nint)0x8000000000000000), UnaryPlusOperatorsHelper<nint, nint>.op_UnaryPlus(unchecked((nint)0x8000000000000000)));
                Assert.Equal(unchecked((nint)0xFFFFFFFFFFFFFFFF), UnaryPlusOperatorsHelper<nint, nint>.op_UnaryPlus(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((nint)0x00000000, UnaryPlusOperatorsHelper<nint, nint>.op_UnaryPlus((nint)0x00000000));
                Assert.Equal((nint)0x00000001, UnaryPlusOperatorsHelper<nint, nint>.op_UnaryPlus((nint)0x00000001));
                Assert.Equal((nint)0x7FFFFFFF, UnaryPlusOperatorsHelper<nint, nint>.op_UnaryPlus((nint)0x7FFFFFFF));
                Assert.Equal(unchecked((nint)0x80000000), UnaryPlusOperatorsHelper<nint, nint>.op_UnaryPlus(unchecked((nint)0x80000000)));
                Assert.Equal(unchecked((nint)0xFFFFFFFF), UnaryPlusOperatorsHelper<nint, nint>.op_UnaryPlus(unchecked((nint)0xFFFFFFFF)));
            }
        }

        //
        // IParsable and ISpanParsable
        //

        [Theory]
        [MemberData(nameof(IntPtrTests.Parse_Valid_TestData), MemberType = typeof(IntPtrTests))]
        public static void ParseValidStringTest(string value, NumberStyles style, IFormatProvider provider, nint expected)
        {
            nint result;

            // Default style and provider
            if ((style == NumberStyles.Integer) && (provider is null))
            {
                Assert.True(ParsableHelper<nint>.TryParse(value, provider, out result));
                Assert.Equal(expected, result);
                Assert.Equal(expected, ParsableHelper<nint>.Parse(value, provider));
            }

            // Default provider
            if (provider is null)
            {
                Assert.Equal(expected, NumberBaseHelper<nint>.Parse(value, style, provider));

                // Substitute default NumberFormatInfo
                Assert.True(NumberBaseHelper<nint>.TryParse(value, style, new NumberFormatInfo(), out result));
                Assert.Equal(expected, result);
                Assert.Equal(expected, NumberBaseHelper<nint>.Parse(value, style, new NumberFormatInfo()));
            }

            // Default style
            if (style == NumberStyles.Integer)
            {
                Assert.Equal(expected, ParsableHelper<nint>.Parse(value, provider));
            }

            // Full overloads
            Assert.True(NumberBaseHelper<nint>.TryParse(value, style, provider, out result));
            Assert.Equal(expected, result);
            Assert.Equal(expected, NumberBaseHelper<nint>.Parse(value, style, provider));
        }

        [Theory]
        [MemberData(nameof(IntPtrTests.Parse_Invalid_TestData), MemberType = typeof(IntPtrTests))]
        public static void ParseInvalidStringTest(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            nint result;

            // Default style and provider
            if ((style == NumberStyles.Integer) && (provider is null))
            {
                Assert.False(ParsableHelper<nint>.TryParse(value, provider, out result));
                Assert.Equal(default(nint), result);
                Assert.Throws(exceptionType, () => ParsableHelper<nint>.Parse(value, provider));
            }

            // Default provider
            if (provider is null)
            {
                Assert.Throws(exceptionType, () => NumberBaseHelper<nint>.Parse(value, style, provider));

                // Substitute default NumberFormatInfo
                Assert.False(NumberBaseHelper<nint>.TryParse(value, style, new NumberFormatInfo(), out result));
                Assert.Equal(default(nint), result);
                Assert.Throws(exceptionType, () => NumberBaseHelper<nint>.Parse(value, style, new NumberFormatInfo()));
            }

            // Default style
            if (style == NumberStyles.Integer)
            {
                Assert.Throws(exceptionType, () => ParsableHelper<nint>.Parse(value, provider));
            }

            // Full overloads
            Assert.False(NumberBaseHelper<nint>.TryParse(value, style, provider, out result));
            Assert.Equal(default(nint), result);
            Assert.Throws(exceptionType, () => NumberBaseHelper<nint>.Parse(value, style, provider));
        }

        [Theory]
        [MemberData(nameof(IntPtrTests.Parse_ValidWithOffsetCount_TestData), MemberType = typeof(IntPtrTests))]
        public static void ParseValidSpanTest(string value, int offset, int count, NumberStyles style, IFormatProvider provider, nint expected)
        {
            nint result;

            // Default style and provider
            if ((style == NumberStyles.Integer) && (provider is null))
            {
                Assert.True(SpanParsableHelper<nint>.TryParse(value.AsSpan(offset, count), provider, out result));
                Assert.Equal(expected, result);
            }

            Assert.Equal(expected, NumberBaseHelper<nint>.Parse(value.AsSpan(offset, count), style, provider));

            Assert.True(NumberBaseHelper<nint>.TryParse(value.AsSpan(offset, count), style, provider, out result));
            Assert.Equal(expected, result);
        }

        [Theory]
        [MemberData(nameof(IntPtrTests.Parse_Invalid_TestData), MemberType = typeof(IntPtrTests))]
        public static void ParseInvalidSpanTest(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            if (value is null)
            {
                return;
            }

            nint result;

            // Default style and provider
            if ((style == NumberStyles.Integer) && (provider is null))
            {
                Assert.False(SpanParsableHelper<nint>.TryParse(value.AsSpan(), provider, out result));
                Assert.Equal(default(nint), result);
            }

            Assert.Throws(exceptionType, () => NumberBaseHelper<nint>.Parse(value.AsSpan(), style, provider));

            Assert.False(NumberBaseHelper<nint>.TryParse(value.AsSpan(), style, provider, out result));
            Assert.Equal(default(nint), result);
        }
    }
}
