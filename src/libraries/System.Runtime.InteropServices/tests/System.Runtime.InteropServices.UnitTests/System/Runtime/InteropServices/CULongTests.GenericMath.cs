// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class CULongTests_GenericMath
    {
        [Fact]
        public static void AdditiveIdentityTest()
        {
            Assert.Equal((CULong)0x00000000, AdditiveIdentityHelper<CULong, CULong>.AdditiveIdentity);
        }

        [Fact]
        public static void MinValueTest()
        {
            Assert.Equal((CULong)0x00000000, MinMaxValueHelper<CULong>.MinValue);
        }

        [Fact]
        public static void MaxValueTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CULong)0xFFFFFFFFFFFFFFFF), MinMaxValueHelper<CULong>.MaxValue);
            }
            else
            {
                Assert.Equal((CULong)0xFFFFFFFF, MinMaxValueHelper<CULong>.MaxValue);
            }
        }

        [Fact]
        public static void MultiplicativeIdentityTest()
        {
            Assert.Equal((CULong)0x00000001, MultiplicativeIdentityHelper<CULong, CULong>.MultiplicativeIdentity);
        }

        [Fact]
        public static void OneTest()
        {
            Assert.Equal((CULong)0x00000001, NumberBaseHelper<CULong>.One);
        }

        [Fact]
        public static void ZeroTest()
        {
            Assert.Equal((CULong)0x00000000, NumberBaseHelper<CULong>.Zero);
        }

        [Fact]
        public static void op_AdditionTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CULong)0x0000000000000001), AdditionOperatorsHelper<CULong, CULong, CULong>.op_Addition(unchecked((CULong)0x0000000000000000), (CULong)1));
                Assert.Equal(unchecked((CULong)0x0000000000000002), AdditionOperatorsHelper<CULong, CULong, CULong>.op_Addition(unchecked((CULong)0x0000000000000001), (CULong)1));
                Assert.Equal(unchecked((CULong)0x8000000000000000), AdditionOperatorsHelper<CULong, CULong, CULong>.op_Addition(unchecked((CULong)0x7FFFFFFFFFFFFFFF), (CULong)1));
                Assert.Equal(unchecked((CULong)0x8000000000000001), AdditionOperatorsHelper<CULong, CULong, CULong>.op_Addition(unchecked((CULong)0x8000000000000000), (CULong)1));
                Assert.Equal(unchecked((CULong)0x0000000000000000), AdditionOperatorsHelper<CULong, CULong, CULong>.op_Addition(unchecked((CULong)0xFFFFFFFFFFFFFFFF), (CULong)1));
            }
            else
            {
                Assert.Equal((CULong)0x00000001, AdditionOperatorsHelper<CULong, CULong, CULong>.op_Addition((CULong)0x00000000, (CULong)1));
                Assert.Equal((CULong)0x00000002, AdditionOperatorsHelper<CULong, CULong, CULong>.op_Addition((CULong)0x00000001, (CULong)1));
                Assert.Equal((CULong)0x80000000, AdditionOperatorsHelper<CULong, CULong, CULong>.op_Addition((CULong)0x7FFFFFFF, (CULong)1));
                Assert.Equal((CULong)0x80000001, AdditionOperatorsHelper<CULong, CULong, CULong>.op_Addition((CULong)0x80000000, (CULong)1));
                Assert.Equal((CULong)0x00000000, AdditionOperatorsHelper<CULong, CULong, CULong>.op_Addition((CULong)0xFFFFFFFF, (CULong)1));
            }
        }

        [Fact]
        public static void op_CheckedAdditionTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CULong)0x0000000000000001), AdditionOperatorsHelper<CULong, CULong, CULong>.op_CheckedAddition(unchecked((CULong)0x0000000000000000), (CULong)1));
                Assert.Equal(unchecked((CULong)0x0000000000000002), AdditionOperatorsHelper<CULong, CULong, CULong>.op_CheckedAddition(unchecked((CULong)0x0000000000000001), (CULong)1));
                Assert.Equal(unchecked((CULong)0x8000000000000000), AdditionOperatorsHelper<CULong, CULong, CULong>.op_CheckedAddition(unchecked((CULong)0x7FFFFFFFFFFFFFFF), (CULong)1));
                Assert.Equal(unchecked((CULong)0x8000000000000001), AdditionOperatorsHelper<CULong, CULong, CULong>.op_CheckedAddition(unchecked((CULong)0x8000000000000000), (CULong)1));

                Assert.Throws<OverflowException>(() => AdditionOperatorsHelper<CULong, CULong, CULong>.op_CheckedAddition(unchecked((CULong)0xFFFFFFFFFFFFFFFF), (CULong)1));
            }
            else
            {
                Assert.Equal((CULong)0x00000001, AdditionOperatorsHelper<CULong, CULong, CULong>.op_CheckedAddition((CULong)0x00000000, (CULong)1));
                Assert.Equal((CULong)0x00000002, AdditionOperatorsHelper<CULong, CULong, CULong>.op_CheckedAddition((CULong)0x00000001, (CULong)1));
                Assert.Equal((CULong)0x80000000, AdditionOperatorsHelper<CULong, CULong, CULong>.op_CheckedAddition((CULong)0x7FFFFFFF, (CULong)1));
                Assert.Equal((CULong)0x80000001, AdditionOperatorsHelper<CULong, CULong, CULong>.op_CheckedAddition((CULong)0x80000000, (CULong)1));

                Assert.Throws<OverflowException>(() => AdditionOperatorsHelper<CULong, CULong, CULong>.op_CheckedAddition((CULong)0xFFFFFFFF, (CULong)1));
            }
        }

        [Fact]
        public static void LeadingZeroCountTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CULong)0x0000000000000040), BinaryIntegerHelper<CULong>.LeadingZeroCount(unchecked((CULong)0x0000000000000000)));
                Assert.Equal(unchecked((CULong)0x000000000000003F), BinaryIntegerHelper<CULong>.LeadingZeroCount(unchecked((CULong)0x0000000000000001)));
                Assert.Equal(unchecked((CULong)0x0000000000000001), BinaryIntegerHelper<CULong>.LeadingZeroCount(unchecked((CULong)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((CULong)0x0000000000000000), BinaryIntegerHelper<CULong>.LeadingZeroCount(unchecked((CULong)0x8000000000000000)));
                Assert.Equal(unchecked((CULong)0x0000000000000000), BinaryIntegerHelper<CULong>.LeadingZeroCount(unchecked((CULong)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((CULong)0x0000000000000020, BinaryIntegerHelper<CULong>.LeadingZeroCount((CULong)0x00000000));
                Assert.Equal((CULong)0x000000000000001F, BinaryIntegerHelper<CULong>.LeadingZeroCount((CULong)0x00000001));
                Assert.Equal((CULong)0x0000000000000001, BinaryIntegerHelper<CULong>.LeadingZeroCount((CULong)0x7FFFFFFF));
                Assert.Equal((CULong)0x0000000000000000, BinaryIntegerHelper<CULong>.LeadingZeroCount((CULong)0x80000000));
                Assert.Equal((CULong)0x0000000000000000, BinaryIntegerHelper<CULong>.LeadingZeroCount((CULong)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void PopCountTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CULong)0x0000000000000000), BinaryIntegerHelper<CULong>.PopCount(unchecked((CULong)0x0000000000000000)));
                Assert.Equal(unchecked((CULong)0x0000000000000001), BinaryIntegerHelper<CULong>.PopCount(unchecked((CULong)0x0000000000000001)));
                Assert.Equal(unchecked((CULong)0x000000000000003F), BinaryIntegerHelper<CULong>.PopCount(unchecked((CULong)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((CULong)0x0000000000000001), BinaryIntegerHelper<CULong>.PopCount(unchecked((CULong)0x8000000000000000)));
                Assert.Equal(unchecked((CULong)0x0000000000000040), BinaryIntegerHelper<CULong>.PopCount(unchecked((CULong)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((CULong)0x00000000, BinaryIntegerHelper<CULong>.PopCount((CULong)0x00000000));
                Assert.Equal((CULong)0x00000001, BinaryIntegerHelper<CULong>.PopCount((CULong)0x00000001));
                Assert.Equal((CULong)0x0000001F, BinaryIntegerHelper<CULong>.PopCount((CULong)0x7FFFFFFF));
                Assert.Equal((CULong)0x00000001, BinaryIntegerHelper<CULong>.PopCount((CULong)0x80000000));
                Assert.Equal((CULong)0x00000020, BinaryIntegerHelper<CULong>.PopCount((CULong)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void RotateLeftTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CULong)0x0000000000000000), BinaryIntegerHelper<CULong>.RotateLeft(unchecked((CULong)0x0000000000000000), 1));
                Assert.Equal(unchecked((CULong)0x0000000000000002), BinaryIntegerHelper<CULong>.RotateLeft(unchecked((CULong)0x0000000000000001), 1));
                Assert.Equal(unchecked((CULong)0xFFFFFFFFFFFFFFFE), BinaryIntegerHelper<CULong>.RotateLeft(unchecked((CULong)0x7FFFFFFFFFFFFFFF), 1));
                Assert.Equal(unchecked((CULong)0x0000000000000001), BinaryIntegerHelper<CULong>.RotateLeft(unchecked((CULong)0x8000000000000000), 1));
                Assert.Equal(unchecked((CULong)0xFFFFFFFFFFFFFFFF), BinaryIntegerHelper<CULong>.RotateLeft(unchecked((CULong)0xFFFFFFFFFFFFFFFF), 1));
            }
            else
            {
                Assert.Equal((CULong)0x00000000, BinaryIntegerHelper<CULong>.RotateLeft((CULong)0x00000000, 1));
                Assert.Equal((CULong)0x00000002, BinaryIntegerHelper<CULong>.RotateLeft((CULong)0x00000001, 1));
                Assert.Equal((CULong)0xFFFFFFFE, BinaryIntegerHelper<CULong>.RotateLeft((CULong)0x7FFFFFFF, 1));
                Assert.Equal((CULong)0x00000001, BinaryIntegerHelper<CULong>.RotateLeft((CULong)0x80000000, 1));
                Assert.Equal((CULong)0xFFFFFFFF, BinaryIntegerHelper<CULong>.RotateLeft((CULong)0xFFFFFFFF, 1));
            }
        }

        [Fact]
        public static void RotateRightTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CULong)0x0000000000000000), BinaryIntegerHelper<CULong>.RotateRight(unchecked((CULong)0x0000000000000000), 1));
                Assert.Equal(unchecked((CULong)0x8000000000000000), BinaryIntegerHelper<CULong>.RotateRight(unchecked((CULong)0x0000000000000001), 1));
                Assert.Equal(unchecked((CULong)0xBFFFFFFFFFFFFFFF), BinaryIntegerHelper<CULong>.RotateRight(unchecked((CULong)0x7FFFFFFFFFFFFFFF), 1));
                Assert.Equal(unchecked((CULong)0x4000000000000000), BinaryIntegerHelper<CULong>.RotateRight(unchecked((CULong)0x8000000000000000), 1));
                Assert.Equal(unchecked((CULong)0xFFFFFFFFFFFFFFFF), BinaryIntegerHelper<CULong>.RotateRight(unchecked((CULong)0xFFFFFFFFFFFFFFFF), 1));
            }
            else
            {
                Assert.Equal((CULong)0x00000000, BinaryIntegerHelper<CULong>.RotateRight((CULong)0x00000000, 1));
                Assert.Equal((CULong)0x80000000, BinaryIntegerHelper<CULong>.RotateRight((CULong)0x00000001, 1));
                Assert.Equal((CULong)0xBFFFFFFF, BinaryIntegerHelper<CULong>.RotateRight((CULong)0x7FFFFFFF, 1));
                Assert.Equal((CULong)0x40000000, BinaryIntegerHelper<CULong>.RotateRight((CULong)0x80000000, 1));
                Assert.Equal((CULong)0xFFFFFFFF, BinaryIntegerHelper<CULong>.RotateRight((CULong)0xFFFFFFFF, 1));
            }
        }

        [Fact]
        public static void TrailingZeroCountTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CULong)0x0000000000000040), BinaryIntegerHelper<CULong>.TrailingZeroCount(unchecked((CULong)0x0000000000000000)));
                Assert.Equal(unchecked((CULong)0x0000000000000000), BinaryIntegerHelper<CULong>.TrailingZeroCount(unchecked((CULong)0x0000000000000001)));
                Assert.Equal(unchecked((CULong)0x0000000000000000), BinaryIntegerHelper<CULong>.TrailingZeroCount(unchecked((CULong)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((CULong)0x000000000000003F), BinaryIntegerHelper<CULong>.TrailingZeroCount(unchecked((CULong)0x8000000000000000)));
                Assert.Equal(unchecked((CULong)0x0000000000000000), BinaryIntegerHelper<CULong>.TrailingZeroCount(unchecked((CULong)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((CULong)0x00000020, BinaryIntegerHelper<CULong>.TrailingZeroCount((CULong)0x00000000));
                Assert.Equal((CULong)0x00000000, BinaryIntegerHelper<CULong>.TrailingZeroCount((CULong)0x00000001));
                Assert.Equal((CULong)0x00000000, BinaryIntegerHelper<CULong>.TrailingZeroCount((CULong)0x7FFFFFFF));
                Assert.Equal((CULong)0x0000001F, BinaryIntegerHelper<CULong>.TrailingZeroCount((CULong)0x80000000));
                Assert.Equal((CULong)0x00000000, BinaryIntegerHelper<CULong>.TrailingZeroCount((CULong)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void GetShortestBitLengthTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(0x00, BinaryIntegerHelper<CULong>.GetShortestBitLength(unchecked((CULong)0x0000000000000000)));
                Assert.Equal(0x01, BinaryIntegerHelper<CULong>.GetShortestBitLength(unchecked((CULong)0x0000000000000001)));
                Assert.Equal(0x3F, BinaryIntegerHelper<CULong>.GetShortestBitLength(unchecked((CULong)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(0x40, BinaryIntegerHelper<CULong>.GetShortestBitLength(unchecked((CULong)0x8000000000000000)));
                Assert.Equal(0x40, BinaryIntegerHelper<CULong>.GetShortestBitLength(unchecked((CULong)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal(0x00, BinaryIntegerHelper<CULong>.GetShortestBitLength((CULong)0x00000000));
                Assert.Equal(0x01, BinaryIntegerHelper<CULong>.GetShortestBitLength((CULong)0x00000001));
                Assert.Equal(0x1F, BinaryIntegerHelper<CULong>.GetShortestBitLength((CULong)0x7FFFFFFF));
                Assert.Equal(0x20, BinaryIntegerHelper<CULong>.GetShortestBitLength((CULong)0x80000000));
                Assert.Equal(0x20, BinaryIntegerHelper<CULong>.GetShortestBitLength((CULong)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void IsPow2Test()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.False(BinaryNumberHelper<CULong>.IsPow2(unchecked((CULong)0x0000000000000000)));
                Assert.True(BinaryNumberHelper<CULong>.IsPow2(unchecked((CULong)0x0000000000000001)));
                Assert.False(BinaryNumberHelper<CULong>.IsPow2(unchecked((CULong)0x7FFFFFFFFFFFFFFF)));
                Assert.True(BinaryNumberHelper<CULong>.IsPow2(unchecked((CULong)0x8000000000000000)));
                Assert.False(BinaryNumberHelper<CULong>.IsPow2(unchecked((CULong)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.False(BinaryNumberHelper<CULong>.IsPow2((CULong)0x00000000));
                Assert.True(BinaryNumberHelper<CULong>.IsPow2((CULong)0x00000001));
                Assert.False(BinaryNumberHelper<CULong>.IsPow2((CULong)0x7FFFFFFF));
                Assert.True(BinaryNumberHelper<CULong>.IsPow2((CULong)0x80000000));
                Assert.False(BinaryNumberHelper<CULong>.IsPow2((CULong)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void Log2Test()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CULong)0x0000000000000000), BinaryNumberHelper<CULong>.Log2(unchecked((CULong)0x0000000000000000)));
                Assert.Equal(unchecked((CULong)0x0000000000000000), BinaryNumberHelper<CULong>.Log2(unchecked((CULong)0x0000000000000001)));
                Assert.Equal(unchecked((CULong)0x000000000000003E), BinaryNumberHelper<CULong>.Log2(unchecked((CULong)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((CULong)0x000000000000003F), BinaryNumberHelper<CULong>.Log2(unchecked((CULong)0x8000000000000000)));
                Assert.Equal(unchecked((CULong)0x000000000000003F), BinaryNumberHelper<CULong>.Log2(unchecked((CULong)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((CULong)0x00000000, BinaryNumberHelper<CULong>.Log2((CULong)0x00000000));
                Assert.Equal((CULong)0x00000000, BinaryNumberHelper<CULong>.Log2((CULong)0x00000001));
                Assert.Equal((CULong)0x0000001E, BinaryNumberHelper<CULong>.Log2((CULong)0x7FFFFFFF));
                Assert.Equal((CULong)0x0000001F, BinaryNumberHelper<CULong>.Log2((CULong)0x80000000));
                Assert.Equal((CULong)0x0000001F, BinaryNumberHelper<CULong>.Log2((CULong)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void op_BitwiseAndTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CULong)0x0000000000000000), BitwiseOperatorsHelper<CULong, CULong, CULong>.op_BitwiseAnd(unchecked((CULong)0x0000000000000000), (CULong)1));
                Assert.Equal(unchecked((CULong)0x0000000000000001), BitwiseOperatorsHelper<CULong, CULong, CULong>.op_BitwiseAnd(unchecked((CULong)0x0000000000000001), (CULong)1));
                Assert.Equal(unchecked((CULong)0x0000000000000001), BitwiseOperatorsHelper<CULong, CULong, CULong>.op_BitwiseAnd(unchecked((CULong)0x7FFFFFFFFFFFFFFF), (CULong)1));
                Assert.Equal(unchecked((CULong)0x0000000000000000), BitwiseOperatorsHelper<CULong, CULong, CULong>.op_BitwiseAnd(unchecked((CULong)0x8000000000000000), (CULong)1));
                Assert.Equal(unchecked((CULong)0x0000000000000001), BitwiseOperatorsHelper<CULong, CULong, CULong>.op_BitwiseAnd(unchecked((CULong)0xFFFFFFFFFFFFFFFF), (CULong)1));
            }
            else
            {
                Assert.Equal((CULong)0x00000000, BitwiseOperatorsHelper<CULong, CULong, CULong>.op_BitwiseAnd((CULong)0x00000000, (CULong)1));
                Assert.Equal((CULong)0x00000001, BitwiseOperatorsHelper<CULong, CULong, CULong>.op_BitwiseAnd((CULong)0x00000001, (CULong)1));
                Assert.Equal((CULong)0x00000001, BitwiseOperatorsHelper<CULong, CULong, CULong>.op_BitwiseAnd((CULong)0x7FFFFFFF, (CULong)1));
                Assert.Equal((CULong)0x00000000, BitwiseOperatorsHelper<CULong, CULong, CULong>.op_BitwiseAnd((CULong)0x80000000, (CULong)1));
                Assert.Equal((CULong)0x00000001, BitwiseOperatorsHelper<CULong, CULong, CULong>.op_BitwiseAnd((CULong)0xFFFFFFFF, (CULong)1));
            }
        }

        [Fact]
        public static void op_BitwiseOrTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CULong)0x0000000000000001), BitwiseOperatorsHelper<CULong, CULong, CULong>.op_BitwiseOr(unchecked((CULong)0x0000000000000000), (CULong)1));
                Assert.Equal(unchecked((CULong)0x0000000000000001), BitwiseOperatorsHelper<CULong, CULong, CULong>.op_BitwiseOr(unchecked((CULong)0x0000000000000001), (CULong)1));
                Assert.Equal(unchecked((CULong)0x7FFFFFFFFFFFFFFF), BitwiseOperatorsHelper<CULong, CULong, CULong>.op_BitwiseOr(unchecked((CULong)0x7FFFFFFFFFFFFFFF), (CULong)1));
                Assert.Equal(unchecked((CULong)0x8000000000000001), BitwiseOperatorsHelper<CULong, CULong, CULong>.op_BitwiseOr(unchecked((CULong)0x8000000000000000), (CULong)1));
                Assert.Equal(unchecked((CULong)0xFFFFFFFFFFFFFFFF), BitwiseOperatorsHelper<CULong, CULong, CULong>.op_BitwiseOr(unchecked((CULong)0xFFFFFFFFFFFFFFFF), (CULong)1));
            }
            else
            {
                Assert.Equal((CULong)0x00000001, BitwiseOperatorsHelper<CULong, CULong, CULong>.op_BitwiseOr((CULong)0x00000000, (CULong)1));
                Assert.Equal((CULong)0x00000001, BitwiseOperatorsHelper<CULong, CULong, CULong>.op_BitwiseOr((CULong)0x00000001, (CULong)1));
                Assert.Equal((CULong)0x7FFFFFFF, BitwiseOperatorsHelper<CULong, CULong, CULong>.op_BitwiseOr((CULong)0x7FFFFFFF, (CULong)1));
                Assert.Equal((CULong)0x80000001, BitwiseOperatorsHelper<CULong, CULong, CULong>.op_BitwiseOr((CULong)0x80000000, (CULong)1));
                Assert.Equal((CULong)0xFFFFFFFF, BitwiseOperatorsHelper<CULong, CULong, CULong>.op_BitwiseOr((CULong)0xFFFFFFFF, (CULong)1));
            }
        }

        [Fact]
        public static void op_ExclusiveOrTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CULong)0x0000000000000001), BitwiseOperatorsHelper<CULong, CULong, CULong>.op_ExclusiveOr(unchecked((CULong)0x0000000000000000), (CULong)1));
                Assert.Equal(unchecked((CULong)0x0000000000000000), BitwiseOperatorsHelper<CULong, CULong, CULong>.op_ExclusiveOr(unchecked((CULong)0x0000000000000001), (CULong)1));
                Assert.Equal(unchecked((CULong)0x7FFFFFFFFFFFFFFE), BitwiseOperatorsHelper<CULong, CULong, CULong>.op_ExclusiveOr(unchecked((CULong)0x7FFFFFFFFFFFFFFF), (CULong)1));
                Assert.Equal(unchecked((CULong)0x8000000000000001), BitwiseOperatorsHelper<CULong, CULong, CULong>.op_ExclusiveOr(unchecked((CULong)0x8000000000000000), (CULong)1));
                Assert.Equal(unchecked((CULong)0xFFFFFFFFFFFFFFFE), BitwiseOperatorsHelper<CULong, CULong, CULong>.op_ExclusiveOr(unchecked((CULong)0xFFFFFFFFFFFFFFFF), (CULong)1));
            }
            else
            {
                Assert.Equal((CULong)0x00000001, BitwiseOperatorsHelper<CULong, CULong, CULong>.op_ExclusiveOr((CULong)0x00000000, (CULong)1));
                Assert.Equal((CULong)0x00000000, BitwiseOperatorsHelper<CULong, CULong, CULong>.op_ExclusiveOr((CULong)0x00000001, (CULong)1));
                Assert.Equal((CULong)0x7FFFFFFE, BitwiseOperatorsHelper<CULong, CULong, CULong>.op_ExclusiveOr((CULong)0x7FFFFFFF, (CULong)1));
                Assert.Equal((CULong)0x80000001, BitwiseOperatorsHelper<CULong, CULong, CULong>.op_ExclusiveOr((CULong)0x80000000, (CULong)1));
                Assert.Equal((CULong)0xFFFFFFFE, BitwiseOperatorsHelper<CULong, CULong, CULong>.op_ExclusiveOr((CULong)0xFFFFFFFF, (CULong)1));
            }
        }

        [Fact]
        public static void op_OnesComplementTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CULong)0xFFFFFFFFFFFFFFFF), BitwiseOperatorsHelper<CULong, CULong, CULong>.op_OnesComplement(unchecked((CULong)0x0000000000000000)));
                Assert.Equal(unchecked((CULong)0xFFFFFFFFFFFFFFFE), BitwiseOperatorsHelper<CULong, CULong, CULong>.op_OnesComplement(unchecked((CULong)0x0000000000000001)));
                Assert.Equal(unchecked((CULong)0x8000000000000000), BitwiseOperatorsHelper<CULong, CULong, CULong>.op_OnesComplement(unchecked((CULong)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((CULong)0x7FFFFFFFFFFFFFFF), BitwiseOperatorsHelper<CULong, CULong, CULong>.op_OnesComplement(unchecked((CULong)0x8000000000000000)));
                Assert.Equal(unchecked((CULong)0x0000000000000000), BitwiseOperatorsHelper<CULong, CULong, CULong>.op_OnesComplement(unchecked((CULong)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((CULong)0xFFFFFFFF, BitwiseOperatorsHelper<CULong, CULong, CULong>.op_OnesComplement((CULong)0x00000000));
                Assert.Equal((CULong)0xFFFFFFFE, BitwiseOperatorsHelper<CULong, CULong, CULong>.op_OnesComplement((CULong)0x00000001));
                Assert.Equal((CULong)0x80000000, BitwiseOperatorsHelper<CULong, CULong, CULong>.op_OnesComplement((CULong)0x7FFFFFFF));
                Assert.Equal((CULong)0x7FFFFFFF, BitwiseOperatorsHelper<CULong, CULong, CULong>.op_OnesComplement((CULong)0x80000000));
                Assert.Equal((CULong)0x00000000, BitwiseOperatorsHelper<CULong, CULong, CULong>.op_OnesComplement((CULong)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void op_LessThanTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.True(ComparisonOperatorsHelper<CULong, CULong>.op_LessThan(unchecked((CULong)0x0000000000000000), (CULong)1));
                Assert.False(ComparisonOperatorsHelper<CULong, CULong>.op_LessThan(unchecked((CULong)0x0000000000000001), (CULong)1));
                Assert.False(ComparisonOperatorsHelper<CULong, CULong>.op_LessThan(unchecked((CULong)0x7FFFFFFFFFFFFFFF), (CULong)1));
                Assert.False(ComparisonOperatorsHelper<CULong, CULong>.op_LessThan(unchecked((CULong)0x8000000000000000), (CULong)1));
                Assert.False(ComparisonOperatorsHelper<CULong, CULong>.op_LessThan(unchecked((CULong)0xFFFFFFFFFFFFFFFF), (CULong)1));
            }
            else
            {
                Assert.True(ComparisonOperatorsHelper<CULong, CULong>.op_LessThan((CULong)0x00000000, (CULong)1));
                Assert.False(ComparisonOperatorsHelper<CULong, CULong>.op_LessThan((CULong)0x00000001, (CULong)1));
                Assert.False(ComparisonOperatorsHelper<CULong, CULong>.op_LessThan((CULong)0x7FFFFFFF, (CULong)1));
                Assert.False(ComparisonOperatorsHelper<CULong, CULong>.op_LessThan((CULong)0x80000000, (CULong)1));
                Assert.False(ComparisonOperatorsHelper<CULong, CULong>.op_LessThan((CULong)0xFFFFFFFF, (CULong)1));
            }
        }

        [Fact]
        public static void op_LessThanOrEqualTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.True(ComparisonOperatorsHelper<CULong, CULong>.op_LessThanOrEqual(unchecked((CULong)0x0000000000000000), (CULong)1));
                Assert.True(ComparisonOperatorsHelper<CULong, CULong>.op_LessThanOrEqual(unchecked((CULong)0x0000000000000001), (CULong)1));
                Assert.False(ComparisonOperatorsHelper<CULong, CULong>.op_LessThanOrEqual(unchecked((CULong)0x7FFFFFFFFFFFFFFF), (CULong)1));
                Assert.False(ComparisonOperatorsHelper<CULong, CULong>.op_LessThanOrEqual(unchecked((CULong)0x8000000000000000), (CULong)1));
                Assert.False(ComparisonOperatorsHelper<CULong, CULong>.op_LessThanOrEqual(unchecked((CULong)0xFFFFFFFFFFFFFFFF), (CULong)1));
            }
            else
            {
                Assert.True(ComparisonOperatorsHelper<CULong, CULong>.op_LessThanOrEqual((CULong)0x00000000, (CULong)1));
                Assert.True(ComparisonOperatorsHelper<CULong, CULong>.op_LessThanOrEqual((CULong)0x00000001, (CULong)1));
                Assert.False(ComparisonOperatorsHelper<CULong, CULong>.op_LessThanOrEqual((CULong)0x7FFFFFFF, (CULong)1));
                Assert.False(ComparisonOperatorsHelper<CULong, CULong>.op_LessThanOrEqual((CULong)0x80000000, (CULong)1));
                Assert.False(ComparisonOperatorsHelper<CULong, CULong>.op_LessThanOrEqual((CULong)0xFFFFFFFF, (CULong)1));
            }
        }

        [Fact]
        public static void op_GreaterThanTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.False(ComparisonOperatorsHelper<CULong, CULong>.op_GreaterThan(unchecked((CULong)0x0000000000000000), (CULong)1));
                Assert.False(ComparisonOperatorsHelper<CULong, CULong>.op_GreaterThan(unchecked((CULong)0x0000000000000001), (CULong)1));
                Assert.True(ComparisonOperatorsHelper<CULong, CULong>.op_GreaterThan(unchecked((CULong)0x7FFFFFFFFFFFFFFF), (CULong)1));
                Assert.True(ComparisonOperatorsHelper<CULong, CULong>.op_GreaterThan(unchecked((CULong)0x8000000000000000), (CULong)1));
                Assert.True(ComparisonOperatorsHelper<CULong, CULong>.op_GreaterThan(unchecked((CULong)0xFFFFFFFFFFFFFFFF), (CULong)1));
            }
            else
            {
                Assert.False(ComparisonOperatorsHelper<CULong, CULong>.op_GreaterThan((CULong)0x00000000, (CULong)1));
                Assert.False(ComparisonOperatorsHelper<CULong, CULong>.op_GreaterThan((CULong)0x00000001, (CULong)1));
                Assert.True(ComparisonOperatorsHelper<CULong, CULong>.op_GreaterThan((CULong)0x7FFFFFFF, (CULong)1));
                Assert.True(ComparisonOperatorsHelper<CULong, CULong>.op_GreaterThan((CULong)0x80000000, (CULong)1));
                Assert.True(ComparisonOperatorsHelper<CULong, CULong>.op_GreaterThan((CULong)0xFFFFFFFF, (CULong)1));
            }
        }

        [Fact]
        public static void op_GreaterThanOrEqualTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.False(ComparisonOperatorsHelper<CULong, CULong>.op_GreaterThanOrEqual(unchecked((CULong)0x0000000000000000), (CULong)1));
                Assert.True(ComparisonOperatorsHelper<CULong, CULong>.op_GreaterThanOrEqual(unchecked((CULong)0x0000000000000001), (CULong)1));
                Assert.True(ComparisonOperatorsHelper<CULong, CULong>.op_GreaterThanOrEqual(unchecked((CULong)0x7FFFFFFFFFFFFFFF), (CULong)1));
                Assert.True(ComparisonOperatorsHelper<CULong, CULong>.op_GreaterThanOrEqual(unchecked((CULong)0x8000000000000000), (CULong)1));
                Assert.True(ComparisonOperatorsHelper<CULong, CULong>.op_GreaterThanOrEqual(unchecked((CULong)0xFFFFFFFFFFFFFFFF), (CULong)1));
            }
            else
            {
                Assert.False(ComparisonOperatorsHelper<CULong, CULong>.op_GreaterThanOrEqual((CULong)0x00000000, (CULong)1));
                Assert.True(ComparisonOperatorsHelper<CULong, CULong>.op_GreaterThanOrEqual((CULong)0x00000001, (CULong)1));
                Assert.True(ComparisonOperatorsHelper<CULong, CULong>.op_GreaterThanOrEqual((CULong)0x7FFFFFFF, (CULong)1));
                Assert.True(ComparisonOperatorsHelper<CULong, CULong>.op_GreaterThanOrEqual((CULong)0x80000000, (CULong)1));
                Assert.True(ComparisonOperatorsHelper<CULong, CULong>.op_GreaterThanOrEqual((CULong)0xFFFFFFFF, (CULong)1));
            }
        }

        [Fact]
        public static void op_DecrementTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CULong)0xFFFFFFFFFFFFFFFF), DecrementOperatorsHelper<CULong>.op_Decrement(unchecked((CULong)0x0000000000000000)));
                Assert.Equal(unchecked((CULong)0x0000000000000000), DecrementOperatorsHelper<CULong>.op_Decrement(unchecked((CULong)0x0000000000000001)));
                Assert.Equal(unchecked((CULong)0x7FFFFFFFFFFFFFFE), DecrementOperatorsHelper<CULong>.op_Decrement(unchecked((CULong)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((CULong)0x7FFFFFFFFFFFFFFF), DecrementOperatorsHelper<CULong>.op_Decrement(unchecked((CULong)0x8000000000000000)));
                Assert.Equal(unchecked((CULong)0xFFFFFFFFFFFFFFFE), DecrementOperatorsHelper<CULong>.op_Decrement(unchecked((CULong)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((CULong)0xFFFFFFFF, DecrementOperatorsHelper<CULong>.op_Decrement((CULong)0x00000000));
                Assert.Equal((CULong)0x00000000, DecrementOperatorsHelper<CULong>.op_Decrement((CULong)0x00000001));
                Assert.Equal((CULong)0x7FFFFFFE, DecrementOperatorsHelper<CULong>.op_Decrement((CULong)0x7FFFFFFF));
                Assert.Equal((CULong)0x7FFFFFFF, DecrementOperatorsHelper<CULong>.op_Decrement((CULong)0x80000000));
                Assert.Equal((CULong)0xFFFFFFFE, DecrementOperatorsHelper<CULong>.op_Decrement((CULong)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void op_CheckedDecrementTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CULong)0x0000000000000000), DecrementOperatorsHelper<CULong>.op_CheckedDecrement(unchecked((CULong)0x0000000000000001)));
                Assert.Equal(unchecked((CULong)0x7FFFFFFFFFFFFFFE), DecrementOperatorsHelper<CULong>.op_CheckedDecrement(unchecked((CULong)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((CULong)0x7FFFFFFFFFFFFFFF), DecrementOperatorsHelper<CULong>.op_CheckedDecrement(unchecked((CULong)0x8000000000000000)));
                Assert.Equal(unchecked((CULong)0xFFFFFFFFFFFFFFFE), DecrementOperatorsHelper<CULong>.op_CheckedDecrement(unchecked((CULong)0xFFFFFFFFFFFFFFFF)));

                Assert.Throws<OverflowException>(() => DecrementOperatorsHelper<CULong>.op_CheckedDecrement(unchecked((CULong)0x0000000000000000)));
            }
            else
            {
                Assert.Equal((CULong)0x00000000, DecrementOperatorsHelper<CULong>.op_CheckedDecrement((CULong)0x00000001));
                Assert.Equal((CULong)0x7FFFFFFE, DecrementOperatorsHelper<CULong>.op_CheckedDecrement((CULong)0x7FFFFFFF));
                Assert.Equal((CULong)0x7FFFFFFF, DecrementOperatorsHelper<CULong>.op_CheckedDecrement((CULong)0x80000000));
                Assert.Equal((CULong)0xFFFFFFFE, DecrementOperatorsHelper<CULong>.op_CheckedDecrement((CULong)0xFFFFFFFF));

                Assert.Throws<OverflowException>(() => DecrementOperatorsHelper<CULong>.op_CheckedDecrement((CULong)0x00000000));
            }
        }

        [Fact]
        public static void op_DivisionTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CULong)0x0000000000000000), DivisionOperatorsHelper<CULong, CULong, CULong>.op_Division(unchecked((CULong)0x0000000000000000), (CULong)2));
                Assert.Equal(unchecked((CULong)0x0000000000000000), DivisionOperatorsHelper<CULong, CULong, CULong>.op_Division(unchecked((CULong)0x0000000000000001), (CULong)2));
                Assert.Equal(unchecked((CULong)0x3FFFFFFFFFFFFFFF), DivisionOperatorsHelper<CULong, CULong, CULong>.op_Division(unchecked((CULong)0x7FFFFFFFFFFFFFFF), (CULong)2));
                Assert.Equal(unchecked((CULong)0x4000000000000000), DivisionOperatorsHelper<CULong, CULong, CULong>.op_Division(unchecked((CULong)0x8000000000000000), (CULong)2));
                Assert.Equal(unchecked((CULong)0x7FFFFFFFFFFFFFFF), DivisionOperatorsHelper<CULong, CULong, CULong>.op_Division(unchecked((CULong)0xFFFFFFFFFFFFFFFF), (CULong)2));

                Assert.Throws<DivideByZeroException>(() => DivisionOperatorsHelper<CULong, CULong, CULong>.op_Division(unchecked((CULong)0x0000000000000001), (CULong)0));
            }
            else
            {
                Assert.Equal((CULong)0x00000000, DivisionOperatorsHelper<CULong, CULong, CULong>.op_Division((CULong)0x00000000, (CULong)2));
                Assert.Equal((CULong)0x00000000, DivisionOperatorsHelper<CULong, CULong, CULong>.op_Division((CULong)0x00000001, (CULong)2));
                Assert.Equal((CULong)0x3FFFFFFF, DivisionOperatorsHelper<CULong, CULong, CULong>.op_Division((CULong)0x7FFFFFFF, (CULong)2));
                Assert.Equal((CULong)0x40000000, DivisionOperatorsHelper<CULong, CULong, CULong>.op_Division((CULong)0x80000000, (CULong)2));
                Assert.Equal((CULong)0x7FFFFFFF, DivisionOperatorsHelper<CULong, CULong, CULong>.op_Division((CULong)0xFFFFFFFF, (CULong)2));

                Assert.Throws<DivideByZeroException>(() => DivisionOperatorsHelper<CULong, CULong, CULong>.op_Division((CULong)0x00000001, (CULong)0));
            }
        }

        [Fact]
        public static void op_CheckedDivisionTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CULong)0x0000000000000000), DivisionOperatorsHelper<CULong, CULong, CULong>.op_CheckedDivision(unchecked((CULong)0x0000000000000000), (CULong)2));
                Assert.Equal(unchecked((CULong)0x0000000000000000), DivisionOperatorsHelper<CULong, CULong, CULong>.op_CheckedDivision(unchecked((CULong)0x0000000000000001), (CULong)2));
                Assert.Equal(unchecked((CULong)0x3FFFFFFFFFFFFFFF), DivisionOperatorsHelper<CULong, CULong, CULong>.op_CheckedDivision(unchecked((CULong)0x7FFFFFFFFFFFFFFF), (CULong)2));
                Assert.Equal(unchecked((CULong)0x4000000000000000), DivisionOperatorsHelper<CULong, CULong, CULong>.op_CheckedDivision(unchecked((CULong)0x8000000000000000), (CULong)2));
                Assert.Equal(unchecked((CULong)0x7FFFFFFFFFFFFFFF), DivisionOperatorsHelper<CULong, CULong, CULong>.op_CheckedDivision(unchecked((CULong)0xFFFFFFFFFFFFFFFF), (CULong)2));

                Assert.Throws<DivideByZeroException>(() => DivisionOperatorsHelper<CULong, CULong, CULong>.op_CheckedDivision(unchecked((CULong)0x0000000000000001), (CULong)0));
            }
            else
            {
                Assert.Equal((CULong)0x00000000, DivisionOperatorsHelper<CULong, CULong, CULong>.op_CheckedDivision((CULong)0x00000000, (CULong)2));
                Assert.Equal((CULong)0x00000000, DivisionOperatorsHelper<CULong, CULong, CULong>.op_CheckedDivision((CULong)0x00000001, (CULong)2));
                Assert.Equal((CULong)0x3FFFFFFF, DivisionOperatorsHelper<CULong, CULong, CULong>.op_CheckedDivision((CULong)0x7FFFFFFF, (CULong)2));
                Assert.Equal((CULong)0x40000000, DivisionOperatorsHelper<CULong, CULong, CULong>.op_CheckedDivision((CULong)0x80000000, (CULong)2));
                Assert.Equal((CULong)0x7FFFFFFF, DivisionOperatorsHelper<CULong, CULong, CULong>.op_CheckedDivision((CULong)0xFFFFFFFF, (CULong)2));

                Assert.Throws<DivideByZeroException>(() => DivisionOperatorsHelper<CULong, CULong, CULong>.op_CheckedDivision((CULong)0x00000001, (CULong)0));
            }
        }

        [Fact]
        public static void op_EqualityTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.False(EqualityOperatorsHelper<CULong, CULong>.op_Equality(unchecked((CULong)0x0000000000000000), (CULong)1));
                Assert.True(EqualityOperatorsHelper<CULong, CULong>.op_Equality(unchecked((CULong)0x0000000000000001), (CULong)1));
                Assert.False(EqualityOperatorsHelper<CULong, CULong>.op_Equality(unchecked((CULong)0x7FFFFFFFFFFFFFFF), (CULong)1));
                Assert.False(EqualityOperatorsHelper<CULong, CULong>.op_Equality(unchecked((CULong)0x8000000000000000), (CULong)1));
                Assert.False(EqualityOperatorsHelper<CULong, CULong>.op_Equality(unchecked((CULong)0xFFFFFFFFFFFFFFFF), (CULong)1));
            }
            else
            {
                Assert.False(EqualityOperatorsHelper<CULong, CULong>.op_Equality((CULong)0x00000000, (CULong)1));
                Assert.True(EqualityOperatorsHelper<CULong, CULong>.op_Equality((CULong)0x00000001, (CULong)1));
                Assert.False(EqualityOperatorsHelper<CULong, CULong>.op_Equality((CULong)0x7FFFFFFF, (CULong)1));
                Assert.False(EqualityOperatorsHelper<CULong, CULong>.op_Equality((CULong)0x80000000, (CULong)1));
                Assert.False(EqualityOperatorsHelper<CULong, CULong>.op_Equality((CULong)0xFFFFFFFF, (CULong)1));
            }
        }

        [Fact]
        public static void op_InequalityTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.True(EqualityOperatorsHelper<CULong, CULong>.op_Inequality(unchecked((CULong)0x0000000000000000), (CULong)1));
                Assert.False(EqualityOperatorsHelper<CULong, CULong>.op_Inequality(unchecked((CULong)0x0000000000000001), (CULong)1));
                Assert.True(EqualityOperatorsHelper<CULong, CULong>.op_Inequality(unchecked((CULong)0x7FFFFFFFFFFFFFFF), (CULong)1));
                Assert.True(EqualityOperatorsHelper<CULong, CULong>.op_Inequality(unchecked((CULong)0x8000000000000000), (CULong)1));
                Assert.True(EqualityOperatorsHelper<CULong, CULong>.op_Inequality(unchecked((CULong)0xFFFFFFFFFFFFFFFF), (CULong)1));
            }
            else
            {
                Assert.True(EqualityOperatorsHelper<CULong, CULong>.op_Inequality((CULong)0x00000000, (CULong)1));
                Assert.False(EqualityOperatorsHelper<CULong, CULong>.op_Inequality((CULong)0x00000001, (CULong)1));
                Assert.True(EqualityOperatorsHelper<CULong, CULong>.op_Inequality((CULong)0x7FFFFFFF, (CULong)1));
                Assert.True(EqualityOperatorsHelper<CULong, CULong>.op_Inequality((CULong)0x80000000, (CULong)1));
                Assert.True(EqualityOperatorsHelper<CULong, CULong>.op_Inequality((CULong)0xFFFFFFFF, (CULong)1));
            }
        }

        [Fact]
        public static void op_IncrementTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CULong)0x0000000000000001), IncrementOperatorsHelper<CULong>.op_Increment(unchecked((CULong)0x0000000000000000)));
                Assert.Equal(unchecked((CULong)0x0000000000000002), IncrementOperatorsHelper<CULong>.op_Increment(unchecked((CULong)0x0000000000000001)));
                Assert.Equal(unchecked((CULong)0x8000000000000000), IncrementOperatorsHelper<CULong>.op_Increment(unchecked((CULong)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((CULong)0x8000000000000001), IncrementOperatorsHelper<CULong>.op_Increment(unchecked((CULong)0x8000000000000000)));
                Assert.Equal(unchecked((CULong)0x0000000000000000), IncrementOperatorsHelper<CULong>.op_Increment(unchecked((CULong)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((CULong)0x00000001, IncrementOperatorsHelper<CULong>.op_Increment((CULong)0x00000000));
                Assert.Equal((CULong)0x00000002, IncrementOperatorsHelper<CULong>.op_Increment((CULong)0x00000001));
                Assert.Equal((CULong)0x80000000, IncrementOperatorsHelper<CULong>.op_Increment((CULong)0x7FFFFFFF));
                Assert.Equal((CULong)0x80000001, IncrementOperatorsHelper<CULong>.op_Increment((CULong)0x80000000));
                Assert.Equal((CULong)0x00000000, IncrementOperatorsHelper<CULong>.op_Increment((CULong)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void op_CheckedIncrementTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CULong)0x0000000000000001), IncrementOperatorsHelper<CULong>.op_CheckedIncrement(unchecked((CULong)0x0000000000000000)));
                Assert.Equal(unchecked((CULong)0x0000000000000002), IncrementOperatorsHelper<CULong>.op_CheckedIncrement(unchecked((CULong)0x0000000000000001)));
                Assert.Equal(unchecked((CULong)0x8000000000000000), IncrementOperatorsHelper<CULong>.op_CheckedIncrement(unchecked((CULong)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((CULong)0x8000000000000001), IncrementOperatorsHelper<CULong>.op_CheckedIncrement(unchecked((CULong)0x8000000000000000)));

                Assert.Throws<OverflowException>(() => IncrementOperatorsHelper<CULong>.op_CheckedIncrement(unchecked((CULong)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((CULong)0x00000001, IncrementOperatorsHelper<CULong>.op_CheckedIncrement((CULong)0x00000000));
                Assert.Equal((CULong)0x00000002, IncrementOperatorsHelper<CULong>.op_CheckedIncrement((CULong)0x00000001));
                Assert.Equal((CULong)0x80000000, IncrementOperatorsHelper<CULong>.op_CheckedIncrement((CULong)0x7FFFFFFF));
                Assert.Equal((CULong)0x80000001, IncrementOperatorsHelper<CULong>.op_CheckedIncrement((CULong)0x80000000));

                Assert.Throws<OverflowException>(() => IncrementOperatorsHelper<CULong>.op_CheckedIncrement((CULong)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void op_ModulusTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CULong)0x0000000000000000), ModulusOperatorsHelper<CULong, CULong, CULong>.op_Modulus(unchecked((CULong)0x0000000000000000), (CULong)2));
                Assert.Equal(unchecked((CULong)0x0000000000000001), ModulusOperatorsHelper<CULong, CULong, CULong>.op_Modulus(unchecked((CULong)0x0000000000000001), (CULong)2));
                Assert.Equal(unchecked((CULong)0x0000000000000001), ModulusOperatorsHelper<CULong, CULong, CULong>.op_Modulus(unchecked((CULong)0x7FFFFFFFFFFFFFFF), (CULong)2));
                Assert.Equal(unchecked((CULong)0x0000000000000000), ModulusOperatorsHelper<CULong, CULong, CULong>.op_Modulus(unchecked((CULong)0x8000000000000000), (CULong)2));
                Assert.Equal(unchecked((CULong)0x0000000000000001), ModulusOperatorsHelper<CULong, CULong, CULong>.op_Modulus(unchecked((CULong)0xFFFFFFFFFFFFFFFF), (CULong)2));

                Assert.Throws<DivideByZeroException>(() => ModulusOperatorsHelper<CULong, CULong, CULong>.op_Modulus(unchecked((CULong)0x0000000000000001), (CULong)0));
            }
            else
            {
                Assert.Equal((CULong)0x00000000, ModulusOperatorsHelper<CULong, CULong, CULong>.op_Modulus((CULong)0x00000000, (CULong)2));
                Assert.Equal((CULong)0x00000001, ModulusOperatorsHelper<CULong, CULong, CULong>.op_Modulus((CULong)0x00000001, (CULong)2));
                Assert.Equal((CULong)0x00000001, ModulusOperatorsHelper<CULong, CULong, CULong>.op_Modulus((CULong)0x7FFFFFFF, (CULong)2));
                Assert.Equal((CULong)0x00000000, ModulusOperatorsHelper<CULong, CULong, CULong>.op_Modulus((CULong)0x80000000, (CULong)2));
                Assert.Equal((CULong)0x00000001, ModulusOperatorsHelper<CULong, CULong, CULong>.op_Modulus((CULong)0xFFFFFFFF, (CULong)2));

                Assert.Throws<DivideByZeroException>(() => ModulusOperatorsHelper<CULong, CULong, CULong>.op_Modulus((CULong)0x00000001, (CULong)0));
            }
        }

        [Fact]
        public static void op_MultiplyTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CULong)0x0000000000000000), MultiplyOperatorsHelper<CULong, CULong, CULong>.op_Multiply(unchecked((CULong)0x0000000000000000), (CULong)2));
                Assert.Equal(unchecked((CULong)0x0000000000000002), MultiplyOperatorsHelper<CULong, CULong, CULong>.op_Multiply(unchecked((CULong)0x0000000000000001), (CULong)2));
                Assert.Equal(unchecked((CULong)0xFFFFFFFFFFFFFFFE), MultiplyOperatorsHelper<CULong, CULong, CULong>.op_Multiply(unchecked((CULong)0x7FFFFFFFFFFFFFFF), (CULong)2));
                Assert.Equal(unchecked((CULong)0x0000000000000000), MultiplyOperatorsHelper<CULong, CULong, CULong>.op_Multiply(unchecked((CULong)0x8000000000000000), (CULong)2));
                Assert.Equal(unchecked((CULong)0xFFFFFFFFFFFFFFFE), MultiplyOperatorsHelper<CULong, CULong, CULong>.op_Multiply(unchecked((CULong)0xFFFFFFFFFFFFFFFF), (CULong)2));
            }
            else
            {
                Assert.Equal((CULong)0x00000000, MultiplyOperatorsHelper<CULong, CULong, CULong>.op_Multiply((CULong)0x00000000, (CULong)2));
                Assert.Equal((CULong)0x00000002, MultiplyOperatorsHelper<CULong, CULong, CULong>.op_Multiply((CULong)0x00000001, (CULong)2));
                Assert.Equal((CULong)0xFFFFFFFE, MultiplyOperatorsHelper<CULong, CULong, CULong>.op_Multiply((CULong)0x7FFFFFFF, (CULong)2));
                Assert.Equal((CULong)0x00000000, MultiplyOperatorsHelper<CULong, CULong, CULong>.op_Multiply((CULong)0x80000000, (CULong)2));
                Assert.Equal((CULong)0xFFFFFFFE, MultiplyOperatorsHelper<CULong, CULong, CULong>.op_Multiply((CULong)0xFFFFFFFF, (CULong)2));
            }
        }

        [Fact]
        public static void op_CheckedMultiplyTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CULong)0x0000000000000000), MultiplyOperatorsHelper<CULong, CULong, CULong>.op_CheckedMultiply(unchecked((CULong)0x0000000000000000), (CULong)2));
                Assert.Equal(unchecked((CULong)0x0000000000000002), MultiplyOperatorsHelper<CULong, CULong, CULong>.op_CheckedMultiply(unchecked((CULong)0x0000000000000001), (CULong)2));
                Assert.Equal(unchecked((CULong)0xFFFFFFFFFFFFFFFE), MultiplyOperatorsHelper<CULong, CULong, CULong>.op_CheckedMultiply(unchecked((CULong)0x7FFFFFFFFFFFFFFF), (CULong)2));

                Assert.Throws<OverflowException>(() => MultiplyOperatorsHelper<CULong, CULong, CULong>.op_CheckedMultiply(unchecked((CULong)0x8000000000000000), (CULong)2));
                Assert.Throws<OverflowException>(() => MultiplyOperatorsHelper<CULong, CULong, CULong>.op_CheckedMultiply(unchecked((CULong)0xFFFFFFFFFFFFFFFF), (CULong)2));
            }
            else
            {
                Assert.Equal((CULong)0x00000000, MultiplyOperatorsHelper<CULong, CULong, CULong>.op_CheckedMultiply((CULong)0x00000000, (CULong)2));
                Assert.Equal((CULong)0x00000002, MultiplyOperatorsHelper<CULong, CULong, CULong>.op_CheckedMultiply((CULong)0x00000001, (CULong)2));
                Assert.Equal((CULong)0xFFFFFFFE, MultiplyOperatorsHelper<CULong, CULong, CULong>.op_CheckedMultiply((CULong)0x7FFFFFFF, (CULong)2));

                Assert.Throws<OverflowException>(() => MultiplyOperatorsHelper<CULong, CULong, CULong>.op_CheckedMultiply((CULong)0x80000000, (CULong)2));
                Assert.Throws<OverflowException>(() => MultiplyOperatorsHelper<CULong, CULong, CULong>.op_CheckedMultiply((CULong)0xFFFFFFFF, (CULong)2));
            }
        }

        [Fact]
        public static void AbsTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CULong)0x0000000000000000), NumberHelper<CULong>.Abs(unchecked((CULong)0x0000000000000000)));
                Assert.Equal(unchecked((CULong)0x0000000000000001), NumberHelper<CULong>.Abs(unchecked((CULong)0x0000000000000001)));
                Assert.Equal(unchecked((CULong)0x7FFFFFFFFFFFFFFF), NumberHelper<CULong>.Abs(unchecked((CULong)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((CULong)0x8000000000000000), NumberHelper<CULong>.Abs(unchecked((CULong)0x8000000000000000)));
                Assert.Equal(unchecked((CULong)0xFFFFFFFFFFFFFFFF), NumberHelper<CULong>.Abs(unchecked((CULong)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((CULong)0x00000000, NumberHelper<CULong>.Abs((CULong)0x00000000));
                Assert.Equal((CULong)0x00000001, NumberHelper<CULong>.Abs((CULong)0x00000001));
                Assert.Equal((CULong)0x7FFFFFFF, NumberHelper<CULong>.Abs((CULong)0x7FFFFFFF));
                Assert.Equal((CULong)0x80000000, NumberHelper<CULong>.Abs((CULong)0x80000000));
                Assert.Equal((CULong)0xFFFFFFFF, NumberHelper<CULong>.Abs((CULong)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void ClampTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CULong)0x0000000000000001), NumberHelper<CULong>.Clamp(unchecked((CULong)0x0000000000000000), unchecked((CULong)0x0000000000000001), unchecked((CULong)0x000000000000003F)));
                Assert.Equal(unchecked((CULong)0x0000000000000001), NumberHelper<CULong>.Clamp(unchecked((CULong)0x0000000000000001), unchecked((CULong)0x0000000000000001), unchecked((CULong)0x000000000000003F)));
                Assert.Equal(unchecked((CULong)0x000000000000003F), NumberHelper<CULong>.Clamp(unchecked((CULong)0x7FFFFFFFFFFFFFFF), unchecked((CULong)0x0000000000000001), unchecked((CULong)0x000000000000003F)));
                Assert.Equal(unchecked((CULong)0x000000000000003F), NumberHelper<CULong>.Clamp(unchecked((CULong)0x8000000000000000), unchecked((CULong)0x0000000000000001), unchecked((CULong)0x000000000000003F)));
                Assert.Equal(unchecked((CULong)0x000000000000003F), NumberHelper<CULong>.Clamp(unchecked((CULong)0xFFFFFFFFFFFFFFFF), unchecked((CULong)0x0000000000000001), unchecked((CULong)0x000000000000003F)));
            }
            else
            {
                Assert.Equal((CULong)0x00000001, NumberHelper<CULong>.Clamp((CULong)0x00000000, (CULong)0x00000001, (CULong)0x0000003F));
                Assert.Equal((CULong)0x00000001, NumberHelper<CULong>.Clamp((CULong)0x00000001, (CULong)0x00000001, (CULong)0x0000003F));
                Assert.Equal((CULong)0x0000003F, NumberHelper<CULong>.Clamp((CULong)0x7FFFFFFF, (CULong)0x00000001, (CULong)0x0000003F));
                Assert.Equal((CULong)0x0000003F, NumberHelper<CULong>.Clamp((CULong)0x80000000, (CULong)0x00000001, (CULong)0x0000003F));
                Assert.Equal((CULong)0x0000003F, NumberHelper<CULong>.Clamp((CULong)0xFFFFFFFF, (CULong)0x00000001, (CULong)0x0000003F));
            }
        }

        [Fact]
        public static void CreateCheckedFromByteTest()
        {
            Assert.Equal((CULong)0x00000000, NumberHelper<CULong>.CreateChecked<byte>(0x00));
            Assert.Equal((CULong)0x00000001, NumberHelper<CULong>.CreateChecked<byte>(0x01));
            Assert.Equal((CULong)0x0000007F, NumberHelper<CULong>.CreateChecked<byte>(0x7F));
            Assert.Equal((CULong)0x00000080, NumberHelper<CULong>.CreateChecked<byte>(0x80));
            Assert.Equal((CULong)0x000000FF, NumberHelper<CULong>.CreateChecked<byte>(0xFF));
        }

        [Fact]
        public static void CreateCheckedFromCharTest()
        {
            Assert.Equal((CULong)0x00000000, NumberHelper<CULong>.CreateChecked<char>((char)0x0000));
            Assert.Equal((CULong)0x00000001, NumberHelper<CULong>.CreateChecked<char>((char)0x0001));
            Assert.Equal((CULong)0x00007FFF, NumberHelper<CULong>.CreateChecked<char>((char)0x7FFF));
            Assert.Equal((CULong)0x00008000, NumberHelper<CULong>.CreateChecked<char>((char)0x8000));
            Assert.Equal((CULong)0x0000FFFF, NumberHelper<CULong>.CreateChecked<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateCheckedFromInt16Test()
        {
            Assert.Equal((CULong)0x00000000, NumberHelper<CULong>.CreateChecked<short>(0x0000));
            Assert.Equal((CULong)0x00000001, NumberHelper<CULong>.CreateChecked<short>(0x0001));
            Assert.Equal((CULong)0x00007FFF, NumberHelper<CULong>.CreateChecked<short>(0x7FFF));
            Assert.Throws<OverflowException>(() => NumberHelper<CULong>.CreateChecked<short>(unchecked((short)0x8000)));
            Assert.Throws<OverflowException>(() => NumberHelper<CULong>.CreateChecked<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromInt32Test()
        {
            Assert.Equal((CULong)0x00000000, NumberHelper<CULong>.CreateChecked<int>(0x00000000));
            Assert.Equal((CULong)0x00000001, NumberHelper<CULong>.CreateChecked<int>(0x00000001));
            Assert.Equal((CULong)0x7FFFFFFF, NumberHelper<CULong>.CreateChecked<int>(0x7FFFFFFF));
            Assert.Throws<OverflowException>(() => NumberHelper<CULong>.CreateChecked<int>(unchecked((int)0x80000000)));
            Assert.Throws<OverflowException>(() => NumberHelper<CULong>.CreateChecked<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromInt64Test()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CULong)0x0000000000000000), NumberHelper<CULong>.CreateChecked<long>(0x0000000000000000));
                Assert.Equal(unchecked((CULong)0x0000000000000001), NumberHelper<CULong>.CreateChecked<long>(0x0000000000000001));
                Assert.Equal(unchecked((CULong)0x7FFFFFFFFFFFFFFF), NumberHelper<CULong>.CreateChecked<long>(0x7FFFFFFFFFFFFFFF));
                Assert.Throws<OverflowException>(() => NumberHelper<CULong>.CreateChecked<long>(unchecked((long)0x8000000000000000)));
                Assert.Throws<OverflowException>(() => NumberHelper<CULong>.CreateChecked<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((CULong)0x00000000, NumberHelper<CULong>.CreateChecked<long>(0x0000000000000000));
                Assert.Equal((CULong)0x00000001, NumberHelper<CULong>.CreateChecked<long>(0x0000000000000001));
                Assert.Throws<OverflowException>(() => NumberHelper<CULong>.CreateChecked<long>(0x7FFFFFFFFFFFFFFF));
                Assert.Throws<OverflowException>(() => NumberHelper<CULong>.CreateChecked<long>(unchecked((long)0x8000000000000000)));
                Assert.Throws<OverflowException>(() => NumberHelper<CULong>.CreateChecked<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateCheckedFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CULong)0x00000000), NumberHelper<CULong>.CreateChecked<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal(unchecked((CULong)0x00000001), NumberHelper<CULong>.CreateChecked<nint>(unchecked((nint)0x0000000000000001)));

                if (OperatingSystem.IsWindows())
                {
                    Assert.Throws<OverflowException>(() => NumberHelper<CULong>.CreateChecked<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                }
                else
                {
                    Assert.Equal(unchecked((CULong)0x7FFFFFFFFFFFFFFF), NumberHelper<CULong>.CreateChecked<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                }

                Assert.Throws<OverflowException>(() => NumberHelper<CULong>.CreateChecked<nint>(unchecked((nint)0x8000000000000000)));
                Assert.Throws<OverflowException>(() => NumberHelper<CULong>.CreateChecked<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((CULong)0x00000000, NumberHelper<CULong>.CreateChecked<nint>((nint)0x00000000));
                Assert.Equal((CULong)0x00000001, NumberHelper<CULong>.CreateChecked<nint>((nint)0x00000001));
                Assert.Equal((CULong)0x7FFFFFFF, NumberHelper<CULong>.CreateChecked<nint>((nint)0x7FFFFFFF));
                Assert.Throws<OverflowException>(() => NumberHelper<CULong>.CreateChecked<nint>(unchecked((nint)0x80000000)));
                Assert.Throws<OverflowException>(() => NumberHelper<CULong>.CreateChecked<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateCheckedFromSByteTest()
        {
            Assert.Equal((CULong)0x00000000, NumberHelper<CULong>.CreateChecked<sbyte>(0x00));
            Assert.Equal((CULong)0x00000001, NumberHelper<CULong>.CreateChecked<sbyte>(0x01));
            Assert.Equal((CULong)0x0000007F, NumberHelper<CULong>.CreateChecked<sbyte>(0x7F));
            Assert.Throws<OverflowException>(() => NumberHelper<CULong>.CreateChecked<sbyte>(unchecked((sbyte)0x80)));
            Assert.Throws<OverflowException>(() => NumberHelper<CULong>.CreateChecked<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateCheckedFromUInt16Test()
        {
            Assert.Equal((CULong)0x00000000, NumberHelper<CULong>.CreateChecked<ushort>(0x0000));
            Assert.Equal((CULong)0x00000001, NumberHelper<CULong>.CreateChecked<ushort>(0x0001));
            Assert.Equal((CULong)0x00007FFF, NumberHelper<CULong>.CreateChecked<ushort>(0x7FFF));
            Assert.Equal((CULong)0x00008000, NumberHelper<CULong>.CreateChecked<ushort>(0x8000));
            Assert.Equal((CULong)0x0000FFFF, NumberHelper<CULong>.CreateChecked<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUInt32Test()
        {
            Assert.Equal((CULong)0x00000000, NumberHelper<CULong>.CreateChecked<uint>(0x00000000));
            Assert.Equal((CULong)0x00000001, NumberHelper<CULong>.CreateChecked<uint>(0x00000001));
            Assert.Equal((CULong)0x7FFFFFFF, NumberHelper<CULong>.CreateChecked<uint>(0x7FFFFFFF));
            Assert.Equal((CULong)0x80000000, NumberHelper<CULong>.CreateChecked<uint>(0x80000000));
            Assert.Equal((CULong)0xFFFFFFFF, NumberHelper<CULong>.CreateChecked<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUInt64Test()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CULong)0x0000000000000000), NumberHelper<CULong>.CreateChecked<ulong>(0x0000000000000000));
                Assert.Equal(unchecked((CULong)0x0000000000000001), NumberHelper<CULong>.CreateChecked<ulong>(0x0000000000000001));
                Assert.Equal(unchecked((CULong)0x7FFFFFFFFFFFFFFF), NumberHelper<CULong>.CreateChecked<ulong>(0x7FFFFFFFFFFFFFFF));
                Assert.Equal(unchecked((CULong)0x8000000000000000), NumberHelper<CULong>.CreateChecked<ulong>(0x8000000000000000));
                Assert.Equal(unchecked((CULong)0xFFFFFFFFFFFFFFFF), NumberHelper<CULong>.CreateChecked<ulong>(0xFFFFFFFFFFFFFFFF));
            }
            else
            {
                Assert.Equal((CULong)0x00000000, NumberHelper<CULong>.CreateChecked<ulong>(0x0000000000000000));
                Assert.Equal((CULong)0x00000001, NumberHelper<CULong>.CreateChecked<ulong>(0x0000000000000001));
                Assert.Throws<OverflowException>(() => NumberHelper<CULong>.CreateChecked<ulong>(0x7FFFFFFFFFFFFFFF));
                Assert.Throws<OverflowException>(() => NumberHelper<CULong>.CreateChecked<ulong>(0x8000000000000000));
                Assert.Throws<OverflowException>(() => NumberHelper<CULong>.CreateChecked<ulong>(0xFFFFFFFFFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateCheckedFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CULong)0x00000000), NumberHelper<CULong>.CreateChecked<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal(unchecked((CULong)0x00000001), NumberHelper<CULong>.CreateChecked<nuint>(unchecked((nuint)0x0000000000000001)));

                if (OperatingSystem.IsWindows())
                {
                    Assert.Throws<OverflowException>(() => NumberHelper<CULong>.CreateChecked<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                    Assert.Throws<OverflowException>(() => NumberHelper<CULong>.CreateChecked<nuint>(unchecked((nuint)0x8000000000000000)));
                    Assert.Throws<OverflowException>(() => NumberHelper<CULong>.CreateChecked<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
                }
                else
                {
                    Assert.Equal(unchecked((CULong)0x7FFFFFFFFFFFFFFF), NumberHelper<CULong>.CreateChecked<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                    Assert.Equal(unchecked((CULong)0x8000000000000000), NumberHelper<CULong>.CreateChecked<nuint>(unchecked((nuint)0x8000000000000000)));
                    Assert.Equal(unchecked((CULong)0xFFFFFFFFFFFFFFFF), NumberHelper<CULong>.CreateChecked<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
                }
            }
            else
            {
                Assert.Equal((CULong)0x00000000, NumberHelper<CULong>.CreateChecked<nuint>((nuint)0x00000000));
                Assert.Equal((CULong)0x00000001, NumberHelper<CULong>.CreateChecked<nuint>((nuint)0x00000001));
                Assert.Equal((CULong)0x7FFFFFFF, NumberHelper<CULong>.CreateChecked<nuint>((nuint)0x7FFFFFFF));
                Assert.Equal((CULong)0x80000000, NumberHelper<CULong>.CreateChecked<nuint>((nuint)0x80000000));
                Assert.Equal((CULong)0xFFFFFFFF, NumberHelper<CULong>.CreateChecked<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateSaturatingFromByteTest()
        {
            Assert.Equal((CULong)0x00000000, NumberHelper<CULong>.CreateSaturating<byte>(0x00));
            Assert.Equal((CULong)0x00000001, NumberHelper<CULong>.CreateSaturating<byte>(0x01));
            Assert.Equal((CULong)0x0000007F, NumberHelper<CULong>.CreateSaturating<byte>(0x7F));
            Assert.Equal((CULong)0x00000080, NumberHelper<CULong>.CreateSaturating<byte>(0x80));
            Assert.Equal((CULong)0x000000FF, NumberHelper<CULong>.CreateSaturating<byte>(0xFF));
        }

        [Fact]
        public static void CreateSaturatingFromCharTest()
        {
            Assert.Equal((CULong)0x00000000, NumberHelper<CULong>.CreateSaturating<char>((char)0x0000));
            Assert.Equal((CULong)0x00000001, NumberHelper<CULong>.CreateSaturating<char>((char)0x0001));
            Assert.Equal((CULong)0x00007FFF, NumberHelper<CULong>.CreateSaturating<char>((char)0x7FFF));
            Assert.Equal((CULong)0x00008000, NumberHelper<CULong>.CreateSaturating<char>((char)0x8000));
            Assert.Equal((CULong)0x0000FFFF, NumberHelper<CULong>.CreateSaturating<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromInt16Test()
        {
            Assert.Equal((CULong)0x00000000, NumberHelper<CULong>.CreateSaturating<short>(0x0000));
            Assert.Equal((CULong)0x00000001, NumberHelper<CULong>.CreateSaturating<short>(0x0001));
            Assert.Equal((CULong)0x00007FFF, NumberHelper<CULong>.CreateSaturating<short>(0x7FFF));
            Assert.Equal((CULong)0x00000000, NumberHelper<CULong>.CreateSaturating<short>(unchecked((short)0x8000)));
            Assert.Equal((CULong)0x00000000, NumberHelper<CULong>.CreateSaturating<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromInt32Test()
        {
            Assert.Equal((CULong)0x00000000, NumberHelper<CULong>.CreateSaturating<int>(0x00000000));
            Assert.Equal((CULong)0x00000001, NumberHelper<CULong>.CreateSaturating<int>(0x00000001));
            Assert.Equal((CULong)0x7FFFFFFF, NumberHelper<CULong>.CreateSaturating<int>(0x7FFFFFFF));
            Assert.Equal((CULong)0x00000000, NumberHelper<CULong>.CreateSaturating<int>(unchecked((int)0x80000000)));
            Assert.Equal((CULong)0x00000000, NumberHelper<CULong>.CreateSaturating<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromInt64Test()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CULong)0x0000000000000000), NumberHelper<CULong>.CreateSaturating<long>(0x0000000000000000));
                Assert.Equal(unchecked((CULong)0x0000000000000001), NumberHelper<CULong>.CreateSaturating<long>(0x0000000000000001));
                Assert.Equal(unchecked((CULong)0x7FFFFFFFFFFFFFFF), NumberHelper<CULong>.CreateSaturating<long>(0x7FFFFFFFFFFFFFFF));
                Assert.Equal(unchecked((CULong)0x0000000000000000), NumberHelper<CULong>.CreateSaturating<long>(unchecked((long)0x8000000000000000)));
                Assert.Equal(unchecked((CULong)0x0000000000000000), NumberHelper<CULong>.CreateSaturating<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((CULong)0x00000000, NumberHelper<CULong>.CreateSaturating<long>(0x0000000000000000));
                Assert.Equal((CULong)0x00000001, NumberHelper<CULong>.CreateSaturating<long>(0x0000000000000001));
                Assert.Equal((CULong)0xFFFFFFFF, NumberHelper<CULong>.CreateSaturating<long>(0x7FFFFFFFFFFFFFFF));
                Assert.Equal((CULong)0x00000000, NumberHelper<CULong>.CreateSaturating<long>(unchecked((long)0x8000000000000000)));
                Assert.Equal((CULong)0x00000000, NumberHelper<CULong>.CreateSaturating<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateSaturatingFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CULong)0x00000000), NumberHelper<CULong>.CreateSaturating<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal(unchecked((CULong)0x00000001), NumberHelper<CULong>.CreateSaturating<nint>(unchecked((nint)0x0000000000000001)));

                if (OperatingSystem.IsWindows())
                {
                    Assert.Equal(unchecked((CULong)0xFFFFFFFF), NumberHelper<CULong>.CreateSaturating<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                    Assert.Equal(unchecked((CULong)0x00000000), NumberHelper<CULong>.CreateSaturating<nint>(unchecked((nint)0x8000000000000000)));
                    Assert.Equal(unchecked((CULong)0x00000000), NumberHelper<CULong>.CreateSaturating<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
                }
                else
                {
                    Assert.Equal(unchecked((CULong)0x7FFFFFFFFFFFFFFF), NumberHelper<CULong>.CreateSaturating<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                    Assert.Equal(unchecked((CULong)0x0000000000000000), NumberHelper<CULong>.CreateSaturating<nint>(unchecked((nint)0x8000000000000000)));
                    Assert.Equal(unchecked((CULong)0x0000000000000000), NumberHelper<CULong>.CreateSaturating<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
                }
            }
            else
            {
                Assert.Equal((CULong)0x00000000, NumberHelper<CULong>.CreateSaturating<nint>((nint)0x00000000));
                Assert.Equal((CULong)0x00000001, NumberHelper<CULong>.CreateSaturating<nint>((nint)0x00000001));
                Assert.Equal((CULong)0x7FFFFFFF, NumberHelper<CULong>.CreateSaturating<nint>((nint)0x7FFFFFFF));
                Assert.Equal((CULong)0x00000000, NumberHelper<CULong>.CreateSaturating<nint>(unchecked((nint)0x80000000)));
                Assert.Equal((CULong)0x00000000, NumberHelper<CULong>.CreateSaturating<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateSaturatingFromSByteTest()
        {
            Assert.Equal((CULong)0x00000000, NumberHelper<CULong>.CreateSaturating<sbyte>(0x00));
            Assert.Equal((CULong)0x00000001, NumberHelper<CULong>.CreateSaturating<sbyte>(0x01));
            Assert.Equal((CULong)0x0000007F, NumberHelper<CULong>.CreateSaturating<sbyte>(0x7F));
            Assert.Equal((CULong)0x00000000, NumberHelper<CULong>.CreateSaturating<sbyte>(unchecked((sbyte)0x80)));
            Assert.Equal((CULong)0x00000000, NumberHelper<CULong>.CreateSaturating<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateSaturatingFromUInt16Test()
        {
            Assert.Equal((CULong)0x00000000, NumberHelper<CULong>.CreateSaturating<ushort>(0x0000));
            Assert.Equal((CULong)0x00000001, NumberHelper<CULong>.CreateSaturating<ushort>(0x0001));
            Assert.Equal((CULong)0x00007FFF, NumberHelper<CULong>.CreateSaturating<ushort>(0x7FFF));
            Assert.Equal((CULong)0x00008000, NumberHelper<CULong>.CreateSaturating<ushort>(0x8000));
            Assert.Equal((CULong)0x0000FFFF, NumberHelper<CULong>.CreateSaturating<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUInt32Test()
        {
            Assert.Equal((CULong)0x00000000, NumberHelper<CULong>.CreateSaturating<uint>(0x00000000));
            Assert.Equal((CULong)0x00000001, NumberHelper<CULong>.CreateSaturating<uint>(0x00000001));
            Assert.Equal((CULong)0x7FFFFFFF, NumberHelper<CULong>.CreateSaturating<uint>(0x7FFFFFFF));
            Assert.Equal((CULong)0x80000000, NumberHelper<CULong>.CreateSaturating<uint>(0x80000000));
            Assert.Equal((CULong)0xFFFFFFFF, NumberHelper<CULong>.CreateSaturating<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUInt64Test()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CULong)0x0000000000000000), NumberHelper<CULong>.CreateSaturating<ulong>(0x0000000000000000));
                Assert.Equal(unchecked((CULong)0x0000000000000001), NumberHelper<CULong>.CreateSaturating<ulong>(0x0000000000000001));
                Assert.Equal(unchecked((CULong)0x7FFFFFFFFFFFFFFF), NumberHelper<CULong>.CreateSaturating<ulong>(0x7FFFFFFFFFFFFFFF));
                Assert.Equal(unchecked((CULong)0x8000000000000000), NumberHelper<CULong>.CreateSaturating<ulong>(0x8000000000000000));
                Assert.Equal(unchecked((CULong)0xFFFFFFFFFFFFFFFF), NumberHelper<CULong>.CreateSaturating<ulong>(0xFFFFFFFFFFFFFFFF));
            }
            else
            {
                Assert.Equal((CULong)0x00000000, NumberHelper<CULong>.CreateSaturating<ulong>(0x0000000000000000));
                Assert.Equal((CULong)0x00000001, NumberHelper<CULong>.CreateSaturating<ulong>(0x0000000000000001));
                Assert.Equal((CULong)0xFFFFFFFF, NumberHelper<CULong>.CreateSaturating<ulong>(0x7FFFFFFFFFFFFFFF));
                Assert.Equal((CULong)0xFFFFFFFF, NumberHelper<CULong>.CreateSaturating<ulong>(0x8000000000000000));
                Assert.Equal((CULong)0xFFFFFFFF, NumberHelper<CULong>.CreateSaturating<ulong>(0xFFFFFFFFFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateSaturatingFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CULong)0x00000000), NumberHelper<CULong>.CreateSaturating<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal(unchecked((CULong)0x00000001), NumberHelper<CULong>.CreateSaturating<nuint>(unchecked((nuint)0x0000000000000001)));

                if (OperatingSystem.IsWindows())
                {
                    Assert.Equal(unchecked((CULong)0xFFFFFFFF), NumberHelper<CULong>.CreateSaturating<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                    Assert.Equal(unchecked((CULong)0xFFFFFFFF), NumberHelper<CULong>.CreateSaturating<nuint>(unchecked((nuint)0x8000000000000000)));
                    Assert.Equal(unchecked((CULong)0xFFFFFFFF), NumberHelper<CULong>.CreateSaturating<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
                }
                else
                {
                    Assert.Equal(unchecked((CULong)0x7FFFFFFFFFFFFFFF), NumberHelper<CULong>.CreateSaturating<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                    Assert.Equal(unchecked((CULong)0x8000000000000000), NumberHelper<CULong>.CreateSaturating<nuint>(unchecked((nuint)0x8000000000000000)));
                    Assert.Equal(unchecked((CULong)0xFFFFFFFFFFFFFFFF), NumberHelper<CULong>.CreateSaturating<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
                }
            }
            else
            {
                Assert.Equal((CULong)0x00000000, NumberHelper<CULong>.CreateSaturating<nuint>((nuint)0x00000000));
                Assert.Equal((CULong)0x00000001, NumberHelper<CULong>.CreateSaturating<nuint>((nuint)0x00000001));
                Assert.Equal((CULong)0x7FFFFFFF, NumberHelper<CULong>.CreateSaturating<nuint>((nuint)0x7FFFFFFF));
                Assert.Equal((CULong)0x80000000, NumberHelper<CULong>.CreateSaturating<nuint>((nuint)0x80000000));
                Assert.Equal((CULong)0xFFFFFFFF, NumberHelper<CULong>.CreateSaturating<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateTruncatingFromByteTest()
        {
            Assert.Equal((CULong)0x00000000, NumberHelper<CULong>.CreateTruncating<byte>(0x00));
            Assert.Equal((CULong)0x00000001, NumberHelper<CULong>.CreateTruncating<byte>(0x01));
            Assert.Equal((CULong)0x0000007F, NumberHelper<CULong>.CreateTruncating<byte>(0x7F));
            Assert.Equal((CULong)0x00000080, NumberHelper<CULong>.CreateTruncating<byte>(0x80));
            Assert.Equal((CULong)0x000000FF, NumberHelper<CULong>.CreateTruncating<byte>(0xFF));
        }

        [Fact]
        public static void CreateTruncatingFromCharTest()
        {
            Assert.Equal((CULong)0x00000000, NumberHelper<CULong>.CreateTruncating<char>((char)0x0000));
            Assert.Equal((CULong)0x00000001, NumberHelper<CULong>.CreateTruncating<char>((char)0x0001));
            Assert.Equal((CULong)0x00007FFF, NumberHelper<CULong>.CreateTruncating<char>((char)0x7FFF));
            Assert.Equal((CULong)0x00008000, NumberHelper<CULong>.CreateTruncating<char>((char)0x8000));
            Assert.Equal((CULong)0x0000FFFF, NumberHelper<CULong>.CreateTruncating<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromInt16Test()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CULong)0x0000000000000000), NumberHelper<CULong>.CreateTruncating<short>(0x0000));
                Assert.Equal(unchecked((CULong)0x0000000000000001), NumberHelper<CULong>.CreateTruncating<short>(0x0001));
                Assert.Equal(unchecked((CULong)0x0000000000007FFF), NumberHelper<CULong>.CreateTruncating<short>(0x7FFF));
                Assert.Equal(unchecked((CULong)0xFFFFFFFFFFFF8000), NumberHelper<CULong>.CreateTruncating<short>(unchecked((short)0x8000)));
                Assert.Equal(unchecked((CULong)0xFFFFFFFFFFFFFFFF), NumberHelper<CULong>.CreateTruncating<short>(unchecked((short)0xFFFF)));
            }
            else
            {
                Assert.Equal((CULong)0x00000000, NumberHelper<CULong>.CreateTruncating<short>(0x0000));
                Assert.Equal((CULong)0x00000001, NumberHelper<CULong>.CreateTruncating<short>(0x0001));
                Assert.Equal((CULong)0x00007FFF, NumberHelper<CULong>.CreateTruncating<short>(0x7FFF));
                Assert.Equal((CULong)0xFFFF8000, NumberHelper<CULong>.CreateTruncating<short>(unchecked((short)0x8000)));
                Assert.Equal((CULong)0xFFFFFFFF, NumberHelper<CULong>.CreateTruncating<short>(unchecked((short)0xFFFF)));
            }
        }

        [Fact]
        public static void CreateTruncatingFromInt32Test()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CULong)0x0000000000000000), NumberHelper<CULong>.CreateTruncating<int>(0x00000000));
                Assert.Equal(unchecked((CULong)0x0000000000000001), NumberHelper<CULong>.CreateTruncating<int>(0x00000001));
                Assert.Equal(unchecked((CULong)0x000000007FFFFFFF), NumberHelper<CULong>.CreateTruncating<int>(0x7FFFFFFF));
                Assert.Equal(unchecked((CULong)0xFFFFFFFF80000000), NumberHelper<CULong>.CreateTruncating<int>(unchecked((int)0x80000000)));
                Assert.Equal(unchecked((CULong)0xFFFFFFFFFFFFFFFF), NumberHelper<CULong>.CreateTruncating<int>(unchecked((int)0xFFFFFFFF)));
            }
            else
            {
                Assert.Equal((CULong)0x00000000, NumberHelper<CULong>.CreateTruncating<int>(0x00000000));
                Assert.Equal((CULong)0x00000001, NumberHelper<CULong>.CreateTruncating<int>(0x00000001));
                Assert.Equal((CULong)0x7FFFFFFF, NumberHelper<CULong>.CreateTruncating<int>(0x7FFFFFFF));
                Assert.Equal((CULong)0x80000000, NumberHelper<CULong>.CreateTruncating<int>(unchecked((int)0x80000000)));
                Assert.Equal((CULong)0xFFFFFFFF, NumberHelper<CULong>.CreateTruncating<int>(unchecked((int)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateTruncatingFromInt64Test()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CULong)0x0000000000000000), NumberHelper<CULong>.CreateTruncating<long>(0x0000000000000000));
                Assert.Equal(unchecked((CULong)0x0000000000000001), NumberHelper<CULong>.CreateTruncating<long>(0x0000000000000001));
                Assert.Equal(unchecked((CULong)0x7FFFFFFFFFFFFFFF), NumberHelper<CULong>.CreateTruncating<long>(0x7FFFFFFFFFFFFFFF));
                Assert.Equal(unchecked((CULong)0x8000000000000000), NumberHelper<CULong>.CreateTruncating<long>(unchecked((long)0x8000000000000000)));
                Assert.Equal(unchecked((CULong)0xFFFFFFFFFFFFFFFF), NumberHelper<CULong>.CreateTruncating<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((CULong)0x00000000, NumberHelper<CULong>.CreateTruncating<long>(0x0000000000000000));
                Assert.Equal((CULong)0x00000001, NumberHelper<CULong>.CreateTruncating<long>(0x0000000000000001));
                Assert.Equal((CULong)0xFFFFFFFF, NumberHelper<CULong>.CreateTruncating<long>(0x7FFFFFFFFFFFFFFF));
                Assert.Equal((CULong)0x00000000, NumberHelper<CULong>.CreateTruncating<long>(unchecked((long)0x8000000000000000)));
                Assert.Equal((CULong)0xFFFFFFFF, NumberHelper<CULong>.CreateTruncating<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateTruncatingFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CULong)0x00000000), NumberHelper<CULong>.CreateTruncating<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal(unchecked((CULong)0x00000001), NumberHelper<CULong>.CreateTruncating<nint>(unchecked((nint)0x0000000000000001)));

                if (OperatingSystem.IsWindows())
                {
                    Assert.Equal(unchecked((CULong)0xFFFFFFFF), NumberHelper<CULong>.CreateTruncating<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                    Assert.Equal(unchecked((CULong)0x00000000), NumberHelper<CULong>.CreateTruncating<nint>(unchecked((nint)0x8000000000000000)));
                    Assert.Equal(unchecked((CULong)0xFFFFFFFF), NumberHelper<CULong>.CreateTruncating<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
                }
                else
                {
                    Assert.Equal(unchecked((CULong)0x7FFFFFFFFFFFFFFF), NumberHelper<CULong>.CreateTruncating<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                    Assert.Equal(unchecked((CULong)0x8000000000000000), NumberHelper<CULong>.CreateTruncating<nint>(unchecked((nint)0x8000000000000000)));
                    Assert.Equal(unchecked((CULong)0xFFFFFFFFFFFFFFFF), NumberHelper<CULong>.CreateTruncating<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
                }
            }
            else
            {
                Assert.Equal((CULong)0x00000000, NumberHelper<CULong>.CreateTruncating<nint>((nint)0x00000000));
                Assert.Equal((CULong)0x00000001, NumberHelper<CULong>.CreateTruncating<nint>((nint)0x00000001));
                Assert.Equal((CULong)0x7FFFFFFF, NumberHelper<CULong>.CreateTruncating<nint>((nint)0x7FFFFFFF));
                Assert.Equal((CULong)0x80000000, NumberHelper<CULong>.CreateTruncating<nint>(unchecked((nint)0x80000000)));
                Assert.Equal((CULong)0xFFFFFFFF, NumberHelper<CULong>.CreateTruncating<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateTruncatingFromSByteTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CULong)0x0000000000000000), NumberHelper<CULong>.CreateTruncating<sbyte>(0x00));
                Assert.Equal(unchecked((CULong)0x0000000000000001), NumberHelper<CULong>.CreateTruncating<sbyte>(0x01));
                Assert.Equal(unchecked((CULong)0x000000000000007F), NumberHelper<CULong>.CreateTruncating<sbyte>(0x7F));
                Assert.Equal(unchecked((CULong)0xFFFFFFFFFFFFFF80), NumberHelper<CULong>.CreateTruncating<sbyte>(unchecked((sbyte)0x80)));
                Assert.Equal(unchecked((CULong)0xFFFFFFFFFFFFFFFF), NumberHelper<CULong>.CreateTruncating<sbyte>(unchecked((sbyte)0xFF)));
            }
            else
            {
                Assert.Equal((CULong)0x00000000, NumberHelper<CULong>.CreateTruncating<sbyte>(0x00));
                Assert.Equal((CULong)0x00000001, NumberHelper<CULong>.CreateTruncating<sbyte>(0x01));
                Assert.Equal((CULong)0x0000007F, NumberHelper<CULong>.CreateTruncating<sbyte>(0x7F));
                Assert.Equal((CULong)0xFFFFFF80, NumberHelper<CULong>.CreateTruncating<sbyte>(unchecked((sbyte)0x80)));
                Assert.Equal((CULong)0xFFFFFFFF, NumberHelper<CULong>.CreateTruncating<sbyte>(unchecked((sbyte)0xFF)));
            }
        }

        [Fact]
        public static void CreateTruncatingFromUInt16Test()
        {
            Assert.Equal((CULong)0x00000000, NumberHelper<CULong>.CreateTruncating<ushort>(0x0000));
            Assert.Equal((CULong)0x00000001, NumberHelper<CULong>.CreateTruncating<ushort>(0x0001));
            Assert.Equal((CULong)0x00007FFF, NumberHelper<CULong>.CreateTruncating<ushort>(0x7FFF));
            Assert.Equal((CULong)0x00008000, NumberHelper<CULong>.CreateTruncating<ushort>(0x8000));
            Assert.Equal((CULong)0x0000FFFF, NumberHelper<CULong>.CreateTruncating<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt32Test()
        {
            Assert.Equal((CULong)0x00000000, NumberHelper<CULong>.CreateTruncating<uint>(0x00000000));
            Assert.Equal((CULong)0x00000001, NumberHelper<CULong>.CreateTruncating<uint>(0x00000001));
            Assert.Equal((CULong)0x7FFFFFFF, NumberHelper<CULong>.CreateTruncating<uint>(0x7FFFFFFF));
            Assert.Equal((CULong)0x80000000, NumberHelper<CULong>.CreateTruncating<uint>(0x80000000));
            Assert.Equal((CULong)0xFFFFFFFF, NumberHelper<CULong>.CreateTruncating<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt64Test()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CULong)0x0000000000000000), NumberHelper<CULong>.CreateTruncating<ulong>(0x0000000000000000));
                Assert.Equal(unchecked((CULong)0x0000000000000001), NumberHelper<CULong>.CreateTruncating<ulong>(0x0000000000000001));

                if (OperatingSystem.IsWindows())
                {
                    Assert.Equal(unchecked((CULong)0xFFFFFFFF), NumberHelper<CULong>.CreateTruncating<ulong>(0x7FFFFFFFFFFFFFFF));
                    Assert.Equal(unchecked((CULong)0xFFFFFFFF), NumberHelper<CULong>.CreateTruncating<ulong>(0x8000000000000000));
                    Assert.Equal(unchecked((CULong)0xFFFFFFFF), NumberHelper<CULong>.CreateTruncating<ulong>(0xFFFFFFFFFFFFFFFF));
                }
                else
                {
                    Assert.Equal(unchecked((CULong)0x7FFFFFFFFFFFFFFF), NumberHelper<CULong>.CreateTruncating<ulong>(0x7FFFFFFFFFFFFFFF));
                    Assert.Equal(unchecked((CULong)0x8000000000000000), NumberHelper<CULong>.CreateTruncating<ulong>(0x8000000000000000));
                    Assert.Equal(unchecked((CULong)0xFFFFFFFFFFFFFFFF), NumberHelper<CULong>.CreateTruncating<ulong>(0xFFFFFFFFFFFFFFFF));
                }
            }
            else
            {
                Assert.Equal((CULong)0x00000000, NumberHelper<CULong>.CreateTruncating<ulong>(0x0000000000000000));
                Assert.Equal((CULong)0x00000001, NumberHelper<CULong>.CreateTruncating<ulong>(0x0000000000000001));
                Assert.Equal((CULong)0xFFFFFFFF, NumberHelper<CULong>.CreateTruncating<ulong>(0x7FFFFFFFFFFFFFFF));
                Assert.Equal((CULong)0x00000000, NumberHelper<CULong>.CreateTruncating<ulong>(0x8000000000000000));
                Assert.Equal((CULong)0xFFFFFFFF, NumberHelper<CULong>.CreateTruncating<ulong>(0xFFFFFFFFFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateTruncatingFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CULong)0x00000000), NumberHelper<CULong>.CreateTruncating<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal(unchecked((CULong)0x00000001), NumberHelper<CULong>.CreateTruncating<nuint>(unchecked((nuint)0x0000000000000001)));

                if (OperatingSystem.IsWindows())
                {
                    Assert.Equal(unchecked((CULong)0xFFFFFFFF), NumberHelper<CULong>.CreateTruncating<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                    Assert.Equal(unchecked((CULong)0x00000000), NumberHelper<CULong>.CreateTruncating<nuint>(unchecked((nuint)0x8000000000000000)));
                    Assert.Equal(unchecked((CULong)0xFFFFFFFF), NumberHelper<CULong>.CreateTruncating<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
                }
                else
                {
                    Assert.Equal(unchecked((CULong)0x7FFFFFFFFFFFFFFF), NumberHelper<CULong>.CreateTruncating<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                    Assert.Equal(unchecked((CULong)0x8000000000000000), NumberHelper<CULong>.CreateTruncating<nuint>(unchecked((nuint)0x8000000000000000)));
                    Assert.Equal(unchecked((CULong)0xFFFFFFFFFFFFFFFF), NumberHelper<CULong>.CreateTruncating<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
                }
            }
            else
            {
                Assert.Equal((CULong)0x00000000, NumberHelper<CULong>.CreateTruncating<nuint>((nuint)0x00000000));
                Assert.Equal((CULong)0x00000001, NumberHelper<CULong>.CreateTruncating<nuint>((nuint)0x00000001));
                Assert.Equal((CULong)0x7FFFFFFF, NumberHelper<CULong>.CreateTruncating<nuint>((nuint)0x7FFFFFFF));
                Assert.Equal((CULong)0x80000000, NumberHelper<CULong>.CreateTruncating<nuint>((nuint)0x80000000));
                Assert.Equal((CULong)0xFFFFFFFF, NumberHelper<CULong>.CreateTruncating<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void DivRemTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal((unchecked((CULong)0x0000000000000000), unchecked((CULong)0x0000000000000000)), BinaryIntegerHelper<CULong>.DivRem(unchecked((CULong)0x0000000000000000), (CULong)2));
                Assert.Equal((unchecked((CULong)0x0000000000000000), unchecked((CULong)0x0000000000000001)), BinaryIntegerHelper<CULong>.DivRem(unchecked((CULong)0x0000000000000001), (CULong)2));
                Assert.Equal((unchecked((CULong)0x3FFFFFFFFFFFFFFF), unchecked((CULong)0x0000000000000001)), BinaryIntegerHelper<CULong>.DivRem(unchecked((CULong)0x7FFFFFFFFFFFFFFF), (CULong)2));
                Assert.Equal((unchecked((CULong)0x4000000000000000), unchecked((CULong)0x0000000000000000)), BinaryIntegerHelper<CULong>.DivRem(unchecked((CULong)0x8000000000000000), (CULong)2));
                Assert.Equal((unchecked((CULong)0x7FFFFFFFFFFFFFFF), unchecked((CULong)0x0000000000000001)), BinaryIntegerHelper<CULong>.DivRem(unchecked((CULong)0xFFFFFFFFFFFFFFFF), (CULong)2));
            }
            else
            {
                Assert.Equal(((CULong)0x00000000, (CULong)0x00000000), BinaryIntegerHelper<CULong>.DivRem((CULong)0x00000000, (CULong)2));
                Assert.Equal(((CULong)0x00000000, (CULong)0x00000001), BinaryIntegerHelper<CULong>.DivRem((CULong)0x00000001, (CULong)2));
                Assert.Equal(((CULong)0x3FFFFFFF, (CULong)0x00000001), BinaryIntegerHelper<CULong>.DivRem((CULong)0x7FFFFFFF, (CULong)2));
                Assert.Equal(((CULong)0x40000000, (CULong)0x00000000), BinaryIntegerHelper<CULong>.DivRem((CULong)0x80000000, (CULong)2));
                Assert.Equal(((CULong)0x7FFFFFFF, (CULong)0x00000001), BinaryIntegerHelper<CULong>.DivRem((CULong)0xFFFFFFFF, (CULong)2));
            }
        }

        [Fact]
        public static void MaxTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CULong)0x0000000000000001), NumberHelper<CULong>.Max(unchecked((CULong)0x0000000000000000), (CULong)1));
                Assert.Equal(unchecked((CULong)0x0000000000000001), NumberHelper<CULong>.Max(unchecked((CULong)0x0000000000000001), (CULong)1));
                Assert.Equal(unchecked((CULong)0x7FFFFFFFFFFFFFFF), NumberHelper<CULong>.Max(unchecked((CULong)0x7FFFFFFFFFFFFFFF), (CULong)1));
                Assert.Equal(unchecked((CULong)0x8000000000000000), NumberHelper<CULong>.Max(unchecked((CULong)0x8000000000000000), (CULong)1));
                Assert.Equal(unchecked((CULong)0xFFFFFFFFFFFFFFFF), NumberHelper<CULong>.Max(unchecked((CULong)0xFFFFFFFFFFFFFFFF), (CULong)1));
            }
            else
            {
                Assert.Equal((CULong)0x00000001, NumberHelper<CULong>.Max((CULong)0x00000000, (CULong)1));
                Assert.Equal((CULong)0x00000001, NumberHelper<CULong>.Max((CULong)0x00000001, (CULong)1));
                Assert.Equal((CULong)0x7FFFFFFF, NumberHelper<CULong>.Max((CULong)0x7FFFFFFF, (CULong)1));
                Assert.Equal((CULong)0x80000000, NumberHelper<CULong>.Max((CULong)0x80000000, (CULong)1));
                Assert.Equal((CULong)0xFFFFFFFF, NumberHelper<CULong>.Max((CULong)0xFFFFFFFF, (CULong)1));
            }
        }

        [Fact]
        public static void MinTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CULong)0x0000000000000000), NumberHelper<CULong>.Min(unchecked((CULong)0x0000000000000000), (CULong)1));
                Assert.Equal(unchecked((CULong)0x0000000000000001), NumberHelper<CULong>.Min(unchecked((CULong)0x0000000000000001), (CULong)1));
                Assert.Equal(unchecked((CULong)0x0000000000000001), NumberHelper<CULong>.Min(unchecked((CULong)0x7FFFFFFFFFFFFFFF), (CULong)1));
                Assert.Equal(unchecked((CULong)0x0000000000000001), NumberHelper<CULong>.Min(unchecked((CULong)0x8000000000000000), (CULong)1));
                Assert.Equal(unchecked((CULong)0x0000000000000001), NumberHelper<CULong>.Min(unchecked((CULong)0xFFFFFFFFFFFFFFFF), (CULong)1));
            }
            else
            {
                Assert.Equal((CULong)0x00000000, NumberHelper<CULong>.Min((CULong)0x00000000, (CULong)1));
                Assert.Equal((CULong)0x00000001, NumberHelper<CULong>.Min((CULong)0x00000001, (CULong)1));
                Assert.Equal((CULong)0x00000001, NumberHelper<CULong>.Min((CULong)0x7FFFFFFF, (CULong)1));
                Assert.Equal((CULong)0x00000001, NumberHelper<CULong>.Min((CULong)0x80000000, (CULong)1));
                Assert.Equal((CULong)0x00000001, NumberHelper<CULong>.Min((CULong)0xFFFFFFFF, (CULong)1));
            }
        }

        [Fact]
        public static void SignTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(0, NumberHelper<CULong>.Sign(unchecked((CULong)0x0000000000000000)));
                Assert.Equal(1, NumberHelper<CULong>.Sign(unchecked((CULong)0x0000000000000001)));
                Assert.Equal(1, NumberHelper<CULong>.Sign(unchecked((CULong)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(1, NumberHelper<CULong>.Sign(unchecked((CULong)0x8000000000000000)));
                Assert.Equal(1, NumberHelper<CULong>.Sign(unchecked((CULong)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal(0, NumberHelper<CULong>.Sign((CULong)0x00000000));
                Assert.Equal(1, NumberHelper<CULong>.Sign((CULong)0x00000001));
                Assert.Equal(1, NumberHelper<CULong>.Sign((CULong)0x7FFFFFFF));
                Assert.Equal(1, NumberHelper<CULong>.Sign((CULong)0x80000000));
                Assert.Equal(1, NumberHelper<CULong>.Sign((CULong)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void TryCreateFromByteTest()
        {
            CULong result;

            Assert.True(NumberHelper<CULong>.TryCreate<byte>(0x00, out result));
            Assert.Equal((CULong)0x00000000, result);

            Assert.True(NumberHelper<CULong>.TryCreate<byte>(0x01, out result));
            Assert.Equal((CULong)0x00000001, result);

            Assert.True(NumberHelper<CULong>.TryCreate<byte>(0x7F, out result));
            Assert.Equal((CULong)0x0000007F, result);

            Assert.True(NumberHelper<CULong>.TryCreate<byte>(0x80, out result));
            Assert.Equal((CULong)0x00000080, result);

            Assert.True(NumberHelper<CULong>.TryCreate<byte>(0xFF, out result));
            Assert.Equal((CULong)0x000000FF, result);
        }

        [Fact]
        public static void TryCreateFromCharTest()
        {
            CULong result;

            Assert.True(NumberHelper<CULong>.TryCreate<char>((char)0x0000, out result));
            Assert.Equal((CULong)0x00000000, result);

            Assert.True(NumberHelper<CULong>.TryCreate<char>((char)0x0001, out result));
            Assert.Equal((CULong)0x00000001, result);

            Assert.True(NumberHelper<CULong>.TryCreate<char>((char)0x7FFF, out result));
            Assert.Equal((CULong)0x00007FFF, result);

            Assert.True(NumberHelper<CULong>.TryCreate<char>((char)0x8000, out result));
            Assert.Equal((CULong)0x00008000, result);

            Assert.True(NumberHelper<CULong>.TryCreate<char>((char)0xFFFF, out result));
            Assert.Equal((CULong)0x0000FFFF, result);
        }

        [Fact]
        public static void TryCreateFromInt16Test()
        {
            CULong result;

            Assert.True(NumberHelper<CULong>.TryCreate<short>(0x0000, out result));
            Assert.Equal((CULong)0x00000000, result);

            Assert.True(NumberHelper<CULong>.TryCreate<short>(0x0001, out result));
            Assert.Equal((CULong)0x00000001, result);

            Assert.True(NumberHelper<CULong>.TryCreate<short>(0x7FFF, out result));
            Assert.Equal((CULong)0x00007FFF, result);

            Assert.False(NumberHelper<CULong>.TryCreate<short>(unchecked((short)0x8000), out result));
            Assert.Equal((CULong)0x00000000, result);

            Assert.False(NumberHelper<CULong>.TryCreate<short>(unchecked((short)0xFFFF), out result));
            Assert.Equal((CULong)0x00000000, result);
        }

        [Fact]
        public static void TryCreateFromInt32Test()
        {
            CULong result;

            Assert.True(NumberHelper<CULong>.TryCreate<int>(0x00000000, out result));
            Assert.Equal((CULong)0x00000000, result);

            Assert.True(NumberHelper<CULong>.TryCreate<int>(0x00000001, out result));
            Assert.Equal((CULong)0x00000001, result);

            Assert.True(NumberHelper<CULong>.TryCreate<int>(0x7FFFFFFF, out result));
            Assert.Equal((CULong)0x7FFFFFFF, result);

            Assert.False(NumberHelper<CULong>.TryCreate<int>(unchecked((int)0x80000000), out result));
            Assert.Equal((CULong)0x00000000, result);

            Assert.False(NumberHelper<CULong>.TryCreate<int>(unchecked((int)0xFFFFFFFF), out result));
            Assert.Equal((CULong)0x00000000, result);
        }

        [Fact]
        public static void TryCreateFromInt64Test()
        {
            CULong result;

            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.True(NumberHelper<CULong>.TryCreate<long>(0x0000000000000000, out result));
                Assert.Equal(unchecked((CULong)0x0000000000000000), result);

                Assert.True(NumberHelper<CULong>.TryCreate<long>(0x0000000000000001, out result));
                Assert.Equal(unchecked((CULong)0x0000000000000001), result);

                Assert.True(NumberHelper<CULong>.TryCreate<long>(0x7FFFFFFFFFFFFFFF, out result));
                Assert.Equal(unchecked((CULong)0x7FFFFFFFFFFFFFFF), result);

                Assert.False(NumberHelper<CULong>.TryCreate<long>(unchecked((long)0x8000000000000000), out result));
                Assert.Equal(unchecked((CULong)0x0000000000000000), result);

                Assert.False(NumberHelper<CULong>.TryCreate<long>(unchecked((long)0xFFFFFFFFFFFFFFFF), out result));
                Assert.Equal(unchecked((CULong)0x0000000000000000), result);
            }
            else
            {
                Assert.True(NumberHelper<CULong>.TryCreate<long>(0x0000000000000000, out result));
                Assert.Equal((CULong)0x00000000, result);

                Assert.True(NumberHelper<CULong>.TryCreate<long>(0x0000000000000001, out result));
                Assert.Equal((CULong)0x00000001, result);

                Assert.False(NumberHelper<CULong>.TryCreate<long>(0x7FFFFFFFFFFFFFFF, out result));
                Assert.Equal((CULong)0x00000000, result);

                Assert.False(NumberHelper<CULong>.TryCreate<long>(unchecked((long)0x8000000000000000), out result));
                Assert.Equal((CULong)0x00000000, result);

                Assert.False(NumberHelper<CULong>.TryCreate<long>(unchecked((long)0xFFFFFFFFFFFFFFFF), out result));
                Assert.Equal((CULong)0x00000000, result);
            }
        }

        [Fact]
        public static void TryCreateFromIntPtrTest()
        {
            CULong result;

            if (Environment.Is64BitProcess)
            {
                Assert.True(NumberHelper<CULong>.TryCreate<nint>(unchecked((nint)0x0000000000000000), out result));
                Assert.Equal(unchecked((CULong)0x00000000), result);

                Assert.True(NumberHelper<CULong>.TryCreate<nint>(unchecked((nint)0x0000000000000001), out result));
                Assert.Equal(unchecked((CULong)0x00000001), result);

                if (OperatingSystem.IsWindows())
                {
                    Assert.False(NumberHelper<CULong>.TryCreate<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF), out result));
                    Assert.Equal(unchecked((CULong)0x00000000), result); 
                }
                else
                {
                    Assert.True(NumberHelper<CULong>.TryCreate<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF), out result));
                    Assert.Equal(unchecked((CULong)0x7FFFFFFFFFFFFFFF), result);
                }

                Assert.False(NumberHelper<CULong>.TryCreate<nint>(unchecked((nint)0x8000000000000000), out result));
                Assert.Equal(unchecked((CULong)0x00000000), result);

                Assert.False(NumberHelper<CULong>.TryCreate<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF), out result));
                Assert.Equal(unchecked((CULong)0x00000000), result);
            }
            else
            {
                Assert.True(NumberHelper<CULong>.TryCreate<nint>((nint)0x00000000, out result));
                Assert.Equal((CULong)0x00000000, result);

                Assert.True(NumberHelper<CULong>.TryCreate<nint>((nint)0x00000001, out result));
                Assert.Equal((CULong)0x00000001, result);

                Assert.True(NumberHelper<CULong>.TryCreate<nint>((nint)0x7FFFFFFF, out result));
                Assert.Equal((CULong)0x7FFFFFFF, result);

                Assert.False(NumberHelper<CULong>.TryCreate<nint>(unchecked((nint)0x80000000), out result));
                Assert.Equal((CULong)0x00000000, result);

                Assert.False(NumberHelper<CULong>.TryCreate<nint>(unchecked((nint)0xFFFFFFFF), out result));
                Assert.Equal((CULong)0x00000000, result);
            }
        }

        [Fact]
        public static void TryCreateFromSByteTest()
        {
            CULong result;

            Assert.True(NumberHelper<CULong>.TryCreate<sbyte>(0x00, out result));
            Assert.Equal((CULong)0x00000000, result);

            Assert.True(NumberHelper<CULong>.TryCreate<sbyte>(0x01, out result));
            Assert.Equal((CULong)0x00000001, result);

            Assert.True(NumberHelper<CULong>.TryCreate<sbyte>(0x7F, out result));
            Assert.Equal((CULong)0x0000007F, result);

            Assert.False(NumberHelper<CULong>.TryCreate<sbyte>(unchecked((sbyte)0x80), out result));
            Assert.Equal((CULong)0x00000000, result);

            Assert.False(NumberHelper<CULong>.TryCreate<sbyte>(unchecked((sbyte)0xFF), out result));
            Assert.Equal((CULong)0x00000000, result);
        }

        [Fact]
        public static void TryCreateFromUInt16Test()
        {
            CULong result;

            Assert.True(NumberHelper<CULong>.TryCreate<ushort>(0x0000, out result));
            Assert.Equal((CULong)0x00000000, result);

            Assert.True(NumberHelper<CULong>.TryCreate<ushort>(0x0001, out result));
            Assert.Equal((CULong)0x00000001, result);

            Assert.True(NumberHelper<CULong>.TryCreate<ushort>(0x7FFF, out result));
            Assert.Equal((CULong)0x00007FFF, result);

            Assert.True(NumberHelper<CULong>.TryCreate<ushort>(0x8000, out result));
            Assert.Equal((CULong)0x00008000, result);

            Assert.True(NumberHelper<CULong>.TryCreate<ushort>(0xFFFF, out result));
            Assert.Equal((CULong)0x0000FFFF, result);
        }

        [Fact]
        public static void TryCreateFromUInt32Test()
        {
            CULong result;

            Assert.True(NumberHelper<CULong>.TryCreate<uint>(0x00000000, out result));
            Assert.Equal((CULong)0x00000000, result);

            Assert.True(NumberHelper<CULong>.TryCreate<uint>(0x00000001, out result));
            Assert.Equal((CULong)0x00000001, result);

            Assert.True(NumberHelper<CULong>.TryCreate<uint>(0x7FFFFFFF, out result));
            Assert.Equal((CULong)0x7FFFFFFF, result);

            Assert.True(NumberHelper<CULong>.TryCreate<uint>(0x80000000, out result));
            Assert.Equal((CULong)0x80000000, result);

            Assert.True(NumberHelper<CULong>.TryCreate<uint>(0xFFFFFFFF, out result));
            Assert.Equal((CULong)0xFFFFFFFF, result);
        }

        [Fact]
        public static void TryCreateFromUInt64Test()
        {
            CULong result;

            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.True(NumberHelper<CULong>.TryCreate<ulong>(0x0000000000000000, out result));
                Assert.Equal(unchecked((CULong)0x0000000000000000), result);

                Assert.True(NumberHelper<CULong>.TryCreate<ulong>(0x0000000000000001, out result));
                Assert.Equal(unchecked((CULong)0x00000000000000001), result);

                Assert.True(NumberHelper<CULong>.TryCreate<ulong>(0x7FFFFFFFFFFFFFFF, out result));
                Assert.Equal(unchecked((CULong)0x7FFFFFFFFFFFFFFF), result);

                Assert.True(NumberHelper<CULong>.TryCreate<ulong>(0x8000000000000000, out result));
                Assert.Equal(unchecked((CULong)0x8000000000000000), result);

                Assert.True(NumberHelper<CULong>.TryCreate<ulong>(0xFFFFFFFFFFFFFFFF, out result));
                Assert.Equal(unchecked((CULong)0xFFFFFFFFFFFFFFFF), result);
            }
            else
            {
                Assert.True(NumberHelper<CULong>.TryCreate<ulong>(0x0000000000000000, out result));
                Assert.Equal((CULong)0x00000000, result);

                Assert.True(NumberHelper<CULong>.TryCreate<ulong>(0x0000000000000001, out result));
                Assert.Equal((CULong)0x00000001, result);

                Assert.False(NumberHelper<CULong>.TryCreate<ulong>(0x7FFFFFFFFFFFFFFF, out result));
                Assert.Equal((CULong)0x00000000, result);

                Assert.False(NumberHelper<CULong>.TryCreate<ulong>(0x8000000000000000, out result));
                Assert.Equal((CULong)0x00000000, result);

                Assert.False(NumberHelper<CULong>.TryCreate<ulong>(0xFFFFFFFFFFFFFFFF, out result));
                Assert.Equal((CULong)0x00000000, result);
            }
        }

        [Fact]
        public static void TryCreateFromUIntPtrTest()
        {
            CULong result;

            if (Environment.Is64BitProcess)
            {
                Assert.True(NumberHelper<CULong>.TryCreate<nuint>(unchecked((nuint)0x0000000000000000), out result));
                Assert.Equal(unchecked((CULong)0x00000000), result);

                Assert.True(NumberHelper<CULong>.TryCreate<nuint>(unchecked((nuint)0x0000000000000001), out result));
                Assert.Equal(unchecked((CULong)0x00000001), result);

                if (OperatingSystem.IsWindows())
                {
                    Assert.False(NumberHelper<CULong>.TryCreate<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF), out result));
                    Assert.Equal(unchecked((CULong)0x0000000000000000), result);

                    Assert.False(NumberHelper<CULong>.TryCreate<nuint>(unchecked((nuint)0x8000000000000000), out result));
                    Assert.Equal(unchecked((CULong)0x0000000000000000), result);

                    Assert.False(NumberHelper<CULong>.TryCreate<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF), out result));
                    Assert.Equal(unchecked((CULong)0x0000000000000000), result);
                }
                else
                {
                    Assert.True(NumberHelper<CULong>.TryCreate<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF), out result));
                    Assert.Equal(unchecked((CULong)0x7FFFFFFFFFFFFFFF), result);

                    Assert.True(NumberHelper<CULong>.TryCreate<nuint>(unchecked((nuint)0x8000000000000000), out result));
                    Assert.Equal(unchecked((CULong)0x8000000000000000), result);

                    Assert.True(NumberHelper<CULong>.TryCreate<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF), out result));
                    Assert.Equal(unchecked((CULong)0xFFFFFFFFFFFFFFFF), result);
                }
            }
            else
            {
                Assert.True(NumberHelper<CULong>.TryCreate<nuint>((nuint)0x00000000, out result));
                Assert.Equal((CULong)0x00000000, result);

                Assert.True(NumberHelper<CULong>.TryCreate<nuint>((nuint)0x00000001, out result));
                Assert.Equal((CULong)0x00000001, result);

                Assert.True(NumberHelper<CULong>.TryCreate<nuint>((nuint)0x7FFFFFFF, out result));
                Assert.Equal((CULong)0x7FFFFFFF, result);

                Assert.True(NumberHelper<CULong>.TryCreate<nuint>(unchecked((nuint)0x80000000), out result));
                Assert.Equal((CULong)0x80000000, result);

                Assert.True(NumberHelper<CULong>.TryCreate<nuint>(unchecked((nuint)0xFFFFFFFF), out result));
                Assert.Equal((CULong)0xFFFFFFFF, result);
            }
        }

        [Fact]
        public static void GetByteCountTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(8, BinaryIntegerHelper<CULong>.GetByteCount(unchecked((CULong)0x0000000000000000)));
                Assert.Equal(8, BinaryIntegerHelper<CULong>.GetByteCount(unchecked((CULong)0x0000000000000001)));
                Assert.Equal(8, BinaryIntegerHelper<CULong>.GetByteCount(unchecked((CULong)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(8, BinaryIntegerHelper<CULong>.GetByteCount(unchecked((CULong)0x8000000000000000)));
                Assert.Equal(8, BinaryIntegerHelper<CULong>.GetByteCount(unchecked((CULong)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal(4, BinaryIntegerHelper<CULong>.GetByteCount((CULong)0x00000000));
                Assert.Equal(4, BinaryIntegerHelper<CULong>.GetByteCount((CULong)0x00000001));
                Assert.Equal(4, BinaryIntegerHelper<CULong>.GetByteCount((CULong)0x7FFFFFFF));
                Assert.Equal(4, BinaryIntegerHelper<CULong>.GetByteCount((CULong)0x80000000));
                Assert.Equal(4, BinaryIntegerHelper<CULong>.GetByteCount((CULong)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void TryWriteLittleEndianTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Span<byte> destination = stackalloc byte[8];
                int bytesWritten = 0;

                Assert.True(BinaryIntegerHelper<CULong>.TryWriteLittleEndian(unchecked((CULong)0x0000000000000000), destination, out bytesWritten));
                Assert.Equal(8, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

                Assert.True(BinaryIntegerHelper<CULong>.TryWriteLittleEndian(unchecked((CULong)0x0000000000000001), destination, out bytesWritten));
                Assert.Equal(8, bytesWritten);
                Assert.Equal(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

                Assert.True(BinaryIntegerHelper<CULong>.TryWriteLittleEndian(unchecked((CULong)0x7FFFFFFFFFFFFFFF), destination, out bytesWritten));
                Assert.Equal(8, bytesWritten);
                Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, destination.ToArray());

                Assert.True(BinaryIntegerHelper<CULong>.TryWriteLittleEndian(unchecked((CULong)0x8000000000000000), destination, out bytesWritten));
                Assert.Equal(8, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, destination.ToArray());

                Assert.True(BinaryIntegerHelper<CULong>.TryWriteLittleEndian(unchecked((CULong)0xFFFFFFFFFFFFFFFF), destination, out bytesWritten));
                Assert.Equal(8, bytesWritten);
                Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, destination.ToArray());

                Assert.False(BinaryIntegerHelper<CULong>.TryWriteLittleEndian(default, Span<byte>.Empty, out bytesWritten));
                Assert.Equal(0, bytesWritten);
                Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, destination.ToArray());
            }
            else
            {
                Span<byte> destination = stackalloc byte[4];
                int bytesWritten = 0;

                Assert.True(BinaryIntegerHelper<CULong>.TryWriteLittleEndian((CULong)0x00000000, destination, out bytesWritten));
                Assert.Equal(4, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

                Assert.True(BinaryIntegerHelper<CULong>.TryWriteLittleEndian((CULong)0x00000001, destination, out bytesWritten));
                Assert.Equal(4, bytesWritten);
                Assert.Equal(new byte[] { 0x01, 0x00, 0x00, 0x00 }, destination.ToArray());

                Assert.True(BinaryIntegerHelper<CULong>.TryWriteLittleEndian((CULong)0x7FFFFFFF, destination, out bytesWritten));
                Assert.Equal(4, bytesWritten);
                Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0x7F }, destination.ToArray());

                Assert.True(BinaryIntegerHelper<CULong>.TryWriteLittleEndian((CULong)0x80000000, destination, out bytesWritten));
                Assert.Equal(4, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x80 }, destination.ToArray());

                Assert.True(BinaryIntegerHelper<CULong>.TryWriteLittleEndian((CULong)0xFFFFFFFF, destination, out bytesWritten));
                Assert.Equal(4, bytesWritten);
                Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, destination.ToArray());

                Assert.False(BinaryIntegerHelper<CULong>.TryWriteLittleEndian(default, Span<byte>.Empty, out bytesWritten));
                Assert.Equal(0, bytesWritten);
                Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, destination.ToArray());
            }
        }

        [Fact]
        public static void op_LeftShiftTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CULong)0x0000000000000000), ShiftOperatorsHelper<CULong, CULong>.op_LeftShift(unchecked((CULong)0x0000000000000000), 1));
                Assert.Equal(unchecked((CULong)0x0000000000000002), ShiftOperatorsHelper<CULong, CULong>.op_LeftShift(unchecked((CULong)0x0000000000000001), 1));
                Assert.Equal(unchecked((CULong)0xFFFFFFFFFFFFFFFE), ShiftOperatorsHelper<CULong, CULong>.op_LeftShift(unchecked((CULong)0x7FFFFFFFFFFFFFFF), 1));
                Assert.Equal(unchecked((CULong)0x0000000000000000), ShiftOperatorsHelper<CULong, CULong>.op_LeftShift(unchecked((CULong)0x8000000000000000), 1));
                Assert.Equal(unchecked((CULong)0xFFFFFFFFFFFFFFFE), ShiftOperatorsHelper<CULong, CULong>.op_LeftShift(unchecked((CULong)0xFFFFFFFFFFFFFFFF), 1));
            }
            else
            {
                Assert.Equal((CULong)0x00000000, ShiftOperatorsHelper<CULong, CULong>.op_LeftShift((CULong)0x00000000, 1));
                Assert.Equal((CULong)0x00000002, ShiftOperatorsHelper<CULong, CULong>.op_LeftShift((CULong)0x00000001, 1));
                Assert.Equal((CULong)0xFFFFFFFE, ShiftOperatorsHelper<CULong, CULong>.op_LeftShift((CULong)0x7FFFFFFF, 1));
                Assert.Equal((CULong)0x00000000, ShiftOperatorsHelper<CULong, CULong>.op_LeftShift((CULong)0x80000000, 1));
                Assert.Equal((CULong)0xFFFFFFFE, ShiftOperatorsHelper<CULong, CULong>.op_LeftShift((CULong)0xFFFFFFFF, 1));
            }
        }

        [Fact]
        public static void op_RightShiftTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CULong)0x0000000000000000), ShiftOperatorsHelper<CULong, CULong>.op_RightShift(unchecked((CULong)0x0000000000000000), 1));
                Assert.Equal(unchecked((CULong)0x0000000000000000), ShiftOperatorsHelper<CULong, CULong>.op_RightShift(unchecked((CULong)0x0000000000000001), 1));
                Assert.Equal(unchecked((CULong)0x3FFFFFFFFFFFFFFF), ShiftOperatorsHelper<CULong, CULong>.op_RightShift(unchecked((CULong)0x7FFFFFFFFFFFFFFF), 1));
                Assert.Equal(unchecked((CULong)0x4000000000000000), ShiftOperatorsHelper<CULong, CULong>.op_RightShift(unchecked((CULong)0x8000000000000000), 1));
                Assert.Equal(unchecked((CULong)0x7FFFFFFFFFFFFFFF), ShiftOperatorsHelper<CULong, CULong>.op_RightShift(unchecked((CULong)0xFFFFFFFFFFFFFFFF), 1));
            }
            else
            {
                Assert.Equal((CULong)0x00000000, ShiftOperatorsHelper<CULong, CULong>.op_RightShift((CULong)0x00000000, 1));
                Assert.Equal((CULong)0x00000000, ShiftOperatorsHelper<CULong, CULong>.op_RightShift((CULong)0x00000001, 1));
                Assert.Equal((CULong)0x3FFFFFFF, ShiftOperatorsHelper<CULong, CULong>.op_RightShift((CULong)0x7FFFFFFF, 1));
                Assert.Equal((CULong)0x40000000, ShiftOperatorsHelper<CULong, CULong>.op_RightShift((CULong)0x80000000, 1));
                Assert.Equal((CULong)0x7FFFFFFF, ShiftOperatorsHelper<CULong, CULong>.op_RightShift((CULong)0xFFFFFFFF, 1));
            }
        }

        [Fact]
        public static void op_UnsignedRightShiftTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CULong)0x0000000000000000), ShiftOperatorsHelper<CULong, CULong>.op_UnsignedRightShift(unchecked((CULong)0x0000000000000000), 1));
                Assert.Equal(unchecked((CULong)0x0000000000000000), ShiftOperatorsHelper<CULong, CULong>.op_UnsignedRightShift(unchecked((CULong)0x0000000000000001), 1));
                Assert.Equal(unchecked((CULong)0x3FFFFFFFFFFFFFFF), ShiftOperatorsHelper<CULong, CULong>.op_UnsignedRightShift(unchecked((CULong)0x7FFFFFFFFFFFFFFF), 1));
                Assert.Equal(unchecked((CULong)0x4000000000000000), ShiftOperatorsHelper<CULong, CULong>.op_UnsignedRightShift(unchecked((CULong)0x8000000000000000), 1));
                Assert.Equal(unchecked((CULong)0x7FFFFFFFFFFFFFFF), ShiftOperatorsHelper<CULong, CULong>.op_UnsignedRightShift(unchecked((CULong)0xFFFFFFFFFFFFFFFF), 1));
            }
            else
            {
                Assert.Equal((CULong)0x00000000, ShiftOperatorsHelper<CULong, CULong>.op_UnsignedRightShift((CULong)0x00000000, 1));
                Assert.Equal((CULong)0x00000000, ShiftOperatorsHelper<CULong, CULong>.op_UnsignedRightShift((CULong)0x00000001, 1));
                Assert.Equal((CULong)0x3FFFFFFF, ShiftOperatorsHelper<CULong, CULong>.op_UnsignedRightShift((CULong)0x7FFFFFFF, 1));
                Assert.Equal((CULong)0x40000000, ShiftOperatorsHelper<CULong, CULong>.op_UnsignedRightShift((CULong)0x80000000, 1));
                Assert.Equal((CULong)0x7FFFFFFF, ShiftOperatorsHelper<CULong, CULong>.op_UnsignedRightShift((CULong)0xFFFFFFFF, 1));
            }
        }

        [Fact]
        public static void op_SubtractionTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CULong)0xFFFFFFFFFFFFFFFF), SubtractionOperatorsHelper<CULong, CULong, CULong>.op_Subtraction(unchecked((CULong)0x0000000000000000), (CULong)1));
                Assert.Equal(unchecked((CULong)0x0000000000000000), SubtractionOperatorsHelper<CULong, CULong, CULong>.op_Subtraction(unchecked((CULong)0x0000000000000001), (CULong)1));
                Assert.Equal(unchecked((CULong)0x7FFFFFFFFFFFFFFE), SubtractionOperatorsHelper<CULong, CULong, CULong>.op_Subtraction(unchecked((CULong)0x7FFFFFFFFFFFFFFF), (CULong)1));
                Assert.Equal(unchecked((CULong)0x7FFFFFFFFFFFFFFF), SubtractionOperatorsHelper<CULong, CULong, CULong>.op_Subtraction(unchecked((CULong)0x8000000000000000), (CULong)1));
                Assert.Equal(unchecked((CULong)0xFFFFFFFFFFFFFFFE), SubtractionOperatorsHelper<CULong, CULong, CULong>.op_Subtraction(unchecked((CULong)0xFFFFFFFFFFFFFFFF), (CULong)1));
            }
            else
            {
                Assert.Equal((CULong)0xFFFFFFFF, SubtractionOperatorsHelper<CULong, CULong, CULong>.op_Subtraction((CULong)0x00000000, (CULong)1));
                Assert.Equal((CULong)0x00000000, SubtractionOperatorsHelper<CULong, CULong, CULong>.op_Subtraction((CULong)0x00000001, (CULong)1));
                Assert.Equal((CULong)0x7FFFFFFE, SubtractionOperatorsHelper<CULong, CULong, CULong>.op_Subtraction((CULong)0x7FFFFFFF, (CULong)1));
                Assert.Equal((CULong)0x7FFFFFFF, SubtractionOperatorsHelper<CULong, CULong, CULong>.op_Subtraction((CULong)0x80000000, (CULong)1));
                Assert.Equal((CULong)0xFFFFFFFE, SubtractionOperatorsHelper<CULong, CULong, CULong>.op_Subtraction((CULong)0xFFFFFFFF, (CULong)1));
            }
        }

        [Fact]
        public static void op_CheckedSubtractionTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CULong)0x0000000000000000), SubtractionOperatorsHelper<CULong, CULong, CULong>.op_CheckedSubtraction(unchecked((CULong)0x0000000000000001), (CULong)1));
                Assert.Equal(unchecked((CULong)0x7FFFFFFFFFFFFFFE), SubtractionOperatorsHelper<CULong, CULong, CULong>.op_CheckedSubtraction(unchecked((CULong)0x7FFFFFFFFFFFFFFF), (CULong)1));
                Assert.Equal(unchecked((CULong)0x7FFFFFFFFFFFFFFF), SubtractionOperatorsHelper<CULong, CULong, CULong>.op_CheckedSubtraction(unchecked((CULong)0x8000000000000000), (CULong)1));
                Assert.Equal(unchecked((CULong)0xFFFFFFFFFFFFFFFE), SubtractionOperatorsHelper<CULong, CULong, CULong>.op_CheckedSubtraction(unchecked((CULong)0xFFFFFFFFFFFFFFFF), (CULong)1));

                Assert.Throws<OverflowException>(() => SubtractionOperatorsHelper<CULong, CULong, CULong>.op_CheckedSubtraction(unchecked((CULong)0x0000000000000000), (CULong)1));
            }
            else
            {
                Assert.Equal((CULong)0x00000000, SubtractionOperatorsHelper<CULong, CULong, CULong>.op_CheckedSubtraction((CULong)0x00000001, (CULong)1));
                Assert.Equal((CULong)0x7FFFFFFE, SubtractionOperatorsHelper<CULong, CULong, CULong>.op_CheckedSubtraction((CULong)0x7FFFFFFF, (CULong)1));
                Assert.Equal((CULong)0x7FFFFFFF, SubtractionOperatorsHelper<CULong, CULong, CULong>.op_CheckedSubtraction((CULong)0x80000000, (CULong)1));
                Assert.Equal((CULong)0xFFFFFFFE, SubtractionOperatorsHelper<CULong, CULong, CULong>.op_CheckedSubtraction((CULong)0xFFFFFFFF, (CULong)1));

                Assert.Throws<OverflowException>(() => SubtractionOperatorsHelper<CULong, CULong, CULong>.op_CheckedSubtraction((CULong)0x00000000, (CULong)1));
            }
        }

        [Fact]
        public static void op_UnaryNegationTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CULong)0x0000000000000000), UnaryNegationOperatorsHelper<CULong, CULong>.op_UnaryNegation(unchecked((CULong)0x0000000000000000)));
                Assert.Equal(unchecked((CULong)0xFFFFFFFFFFFFFFFF), UnaryNegationOperatorsHelper<CULong, CULong>.op_UnaryNegation(unchecked((CULong)0x0000000000000001)));
                Assert.Equal(unchecked((CULong)0x8000000000000001), UnaryNegationOperatorsHelper<CULong, CULong>.op_UnaryNegation(unchecked((CULong)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((CULong)0x8000000000000000), UnaryNegationOperatorsHelper<CULong, CULong>.op_UnaryNegation(unchecked((CULong)0x8000000000000000)));
                Assert.Equal(unchecked((CULong)0x0000000000000001), UnaryNegationOperatorsHelper<CULong, CULong>.op_UnaryNegation(unchecked((CULong)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((CULong)0x00000000, UnaryNegationOperatorsHelper<CULong, CULong>.op_UnaryNegation((CULong)0x00000000));
                Assert.Equal((CULong)0xFFFFFFFF, UnaryNegationOperatorsHelper<CULong, CULong>.op_UnaryNegation((CULong)0x00000001));
                Assert.Equal((CULong)0x80000001, UnaryNegationOperatorsHelper<CULong, CULong>.op_UnaryNegation((CULong)0x7FFFFFFF));
                Assert.Equal((CULong)0x80000000, UnaryNegationOperatorsHelper<CULong, CULong>.op_UnaryNegation((CULong)0x80000000));
                Assert.Equal((CULong)0x00000001, UnaryNegationOperatorsHelper<CULong, CULong>.op_UnaryNegation((CULong)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void op_CheckedUnaryNegationTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CULong)0x0000000000000000), UnaryNegationOperatorsHelper<CULong, CULong>.op_CheckedUnaryNegation(unchecked((CULong)0x0000000000000000)));

                Assert.Throws<OverflowException>(() => UnaryNegationOperatorsHelper<CULong, CULong>.op_CheckedUnaryNegation(unchecked((CULong)0x0000000000000001)));
                Assert.Throws<OverflowException>(() => UnaryNegationOperatorsHelper<CULong, CULong>.op_CheckedUnaryNegation(unchecked((CULong)0x7FFFFFFFFFFFFFFF)));
                Assert.Throws<OverflowException>(() => UnaryNegationOperatorsHelper<CULong, CULong>.op_CheckedUnaryNegation(unchecked((CULong)0x8000000000000000)));
                Assert.Throws<OverflowException>(() => UnaryNegationOperatorsHelper<CULong, CULong>.op_CheckedUnaryNegation(unchecked((CULong)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((CULong)0x00000000, UnaryNegationOperatorsHelper<CULong, CULong>.op_CheckedUnaryNegation((CULong)0x00000000));

                Assert.Throws<OverflowException>(() => UnaryNegationOperatorsHelper<CULong, CULong>.op_CheckedUnaryNegation((CULong)0x00000001));
                Assert.Throws<OverflowException>(() => UnaryNegationOperatorsHelper<CULong, CULong>.op_CheckedUnaryNegation((CULong)0x7FFFFFFF));
                Assert.Throws<OverflowException>(() => UnaryNegationOperatorsHelper<CULong, CULong>.op_CheckedUnaryNegation((CULong)0x80000000));
                Assert.Throws<OverflowException>(() => UnaryNegationOperatorsHelper<CULong, CULong>.op_CheckedUnaryNegation((CULong)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void op_UnaryPlusTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CULong)0x0000000000000000), UnaryPlusOperatorsHelper<CULong, CULong>.op_UnaryPlus(unchecked((CULong)0x0000000000000000)));
                Assert.Equal(unchecked((CULong)0x0000000000000001), UnaryPlusOperatorsHelper<CULong, CULong>.op_UnaryPlus(unchecked((CULong)0x0000000000000001)));
                Assert.Equal(unchecked((CULong)0x7FFFFFFFFFFFFFFF), UnaryPlusOperatorsHelper<CULong, CULong>.op_UnaryPlus(unchecked((CULong)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((CULong)0x8000000000000000), UnaryPlusOperatorsHelper<CULong, CULong>.op_UnaryPlus(unchecked((CULong)0x8000000000000000)));
                Assert.Equal(unchecked((CULong)0xFFFFFFFFFFFFFFFF), UnaryPlusOperatorsHelper<CULong, CULong>.op_UnaryPlus(unchecked((CULong)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((CULong)0x00000000, UnaryPlusOperatorsHelper<CULong, CULong>.op_UnaryPlus((CULong)0x00000000));
                Assert.Equal((CULong)0x00000001, UnaryPlusOperatorsHelper<CULong, CULong>.op_UnaryPlus((CULong)0x00000001));
                Assert.Equal((CULong)0x7FFFFFFF, UnaryPlusOperatorsHelper<CULong, CULong>.op_UnaryPlus((CULong)0x7FFFFFFF));
                Assert.Equal((CULong)0x80000000, UnaryPlusOperatorsHelper<CULong, CULong>.op_UnaryPlus((CULong)0x80000000));
                Assert.Equal((CULong)0xFFFFFFFF, UnaryPlusOperatorsHelper<CULong, CULong>.op_UnaryPlus((CULong)0xFFFFFFFF));
            }
        }
    }
}
