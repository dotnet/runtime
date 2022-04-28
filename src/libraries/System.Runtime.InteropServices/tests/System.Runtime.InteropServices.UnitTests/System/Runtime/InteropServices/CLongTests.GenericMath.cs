// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class CLongTests_GenericMath
    {
        [Fact]
        public static void AdditiveIdentityTest()
        {
            Assert.Equal((CLong)0x00000000, AdditiveIdentityHelper<CLong, CLong>.AdditiveIdentity);
        }

        [Fact]
        public static void MinValueTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CLong)0x8000000000000000), MinMaxValueHelper<CLong>.MinValue);
            }
            else
            {
                Assert.Equal(unchecked((CLong)0x80000000), MinMaxValueHelper<CLong>.MinValue);
            }
        }

        [Fact]
        public static void MaxValueTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CLong)0x7FFFFFFFFFFFFFFF), MinMaxValueHelper<CLong>.MaxValue);
            }
            else
            {
                Assert.Equal((CLong)0x7FFFFFFF, MinMaxValueHelper<CLong>.MaxValue);
            }
        }

        [Fact]
        public static void MultiplicativeIdentityTest()
        {
            Assert.Equal((CLong)0x00000001, MultiplicativeIdentityHelper<CLong, CLong>.MultiplicativeIdentity);
        }

        [Fact]
        public static void NegativeOneTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CLong)0xFFFFFFFFFFFFFFFF), SignedNumberHelper<CLong>.NegativeOne);
            }
            else
            {
                Assert.Equal(unchecked((CLong)0xFFFFFFFF), SignedNumberHelper<CLong>.NegativeOne);
            }
        }

        [Fact]
        public static void OneTest()
        {
            Assert.Equal((CLong)0x00000001, NumberBaseHelper<CLong>.One);
        }

        [Fact]
        public static void ZeroTest()
        {
            Assert.Equal((CLong)0x00000000, NumberBaseHelper<CLong>.Zero);
        }

        [Fact]
        public static void op_AdditionTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CLong)0x0000000000000001), AdditionOperatorsHelper<CLong, CLong, CLong>.op_Addition(unchecked((CLong)0x0000000000000000), (CLong)1));
                Assert.Equal(unchecked((CLong)0x0000000000000002), AdditionOperatorsHelper<CLong, CLong, CLong>.op_Addition(unchecked((CLong)0x0000000000000001), (CLong)1));
                Assert.Equal(unchecked((CLong)0x8000000000000000), AdditionOperatorsHelper<CLong, CLong, CLong>.op_Addition(unchecked((CLong)0x7FFFFFFFFFFFFFFF), (CLong)1));
                Assert.Equal(unchecked((CLong)0x8000000000000001), AdditionOperatorsHelper<CLong, CLong, CLong>.op_Addition(unchecked((CLong)0x8000000000000000), (CLong)1));
                Assert.Equal(unchecked((CLong)0x0000000000000000), AdditionOperatorsHelper<CLong, CLong, CLong>.op_Addition(unchecked((CLong)0xFFFFFFFFFFFFFFFF), (CLong)1));
            }
            else
            {
                Assert.Equal((CLong)0x00000001, AdditionOperatorsHelper<CLong, CLong, CLong>.op_Addition((CLong)0x00000000, (CLong)1));
                Assert.Equal((CLong)0x00000002, AdditionOperatorsHelper<CLong, CLong, CLong>.op_Addition((CLong)0x00000001, (CLong)1));
                Assert.Equal(unchecked((CLong)0x80000000), AdditionOperatorsHelper<CLong, CLong, CLong>.op_Addition((CLong)0x7FFFFFFF, (CLong)1));
                Assert.Equal(unchecked((CLong)0x80000001), AdditionOperatorsHelper<CLong, CLong, CLong>.op_Addition(unchecked((CLong)0x80000000), (CLong)1));
                Assert.Equal((CLong)0x00000000, AdditionOperatorsHelper<CLong, CLong, CLong>.op_Addition(unchecked((CLong)0xFFFFFFFF), (CLong)1));
            }
        }

        [Fact]
        public static void op_CheckedAdditionTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CLong)0x0000000000000001), AdditionOperatorsHelper<CLong, CLong, CLong>.op_CheckedAddition(unchecked((CLong)0x0000000000000000), (CLong)1));
                Assert.Equal(unchecked((CLong)0x0000000000000002), AdditionOperatorsHelper<CLong, CLong, CLong>.op_CheckedAddition(unchecked((CLong)0x0000000000000001), (CLong)1));
                Assert.Equal(unchecked((CLong)0x8000000000000001), AdditionOperatorsHelper<CLong, CLong, CLong>.op_CheckedAddition(unchecked((CLong)0x8000000000000000), (CLong)1));
                Assert.Equal(unchecked((CLong)0x0000000000000000), AdditionOperatorsHelper<CLong, CLong, CLong>.op_CheckedAddition(unchecked((CLong)0xFFFFFFFFFFFFFFFF), (CLong)1));

                Assert.Throws<OverflowException>(() => AdditionOperatorsHelper<CLong, CLong, CLong>.op_CheckedAddition(unchecked((CLong)0x7FFFFFFFFFFFFFFF), (CLong)1));
            }
            else
            {
                Assert.Equal((CLong)0x00000001, AdditionOperatorsHelper<CLong, CLong, CLong>.op_CheckedAddition((CLong)0x00000000, (CLong)1));
                Assert.Equal((CLong)0x00000002, AdditionOperatorsHelper<CLong, CLong, CLong>.op_CheckedAddition((CLong)0x00000001, (CLong)1));
                Assert.Equal(unchecked((CLong)0x80000001), AdditionOperatorsHelper<CLong, CLong, CLong>.op_CheckedAddition(unchecked((CLong)0x80000000), (CLong)1));
                Assert.Equal((CLong)0x00000000, AdditionOperatorsHelper<CLong, CLong, CLong>.op_CheckedAddition(unchecked((CLong)0xFFFFFFFF), (CLong)1));

                Assert.Throws<OverflowException>(() => AdditionOperatorsHelper<CLong, CLong, CLong>.op_CheckedAddition((CLong)0x7FFFFFFF, (CLong)1));
            }
        }

        [Fact]
        public static void LeadingZeroCountTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CLong)0x0000000000000040), BinaryIntegerHelper<CLong>.LeadingZeroCount(unchecked((CLong)0x0000000000000000)));
                Assert.Equal(unchecked((CLong)0x000000000000003F), BinaryIntegerHelper<CLong>.LeadingZeroCount(unchecked((CLong)0x0000000000000001)));
                Assert.Equal(unchecked((CLong)0x0000000000000001), BinaryIntegerHelper<CLong>.LeadingZeroCount(unchecked((CLong)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((CLong)0x0000000000000000), BinaryIntegerHelper<CLong>.LeadingZeroCount(unchecked((CLong)0x8000000000000000)));
                Assert.Equal(unchecked((CLong)0x0000000000000000), BinaryIntegerHelper<CLong>.LeadingZeroCount(unchecked((CLong)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((CLong)0x0000000000000020, BinaryIntegerHelper<CLong>.LeadingZeroCount((CLong)0x00000000));
                Assert.Equal((CLong)0x000000000000001F, BinaryIntegerHelper<CLong>.LeadingZeroCount((CLong)0x00000001));
                Assert.Equal((CLong)0x0000000000000001, BinaryIntegerHelper<CLong>.LeadingZeroCount((CLong)0x7FFFFFFF));
                Assert.Equal((CLong)0x0000000000000000, BinaryIntegerHelper<CLong>.LeadingZeroCount(unchecked((CLong)0x80000000)));
                Assert.Equal((CLong)0x0000000000000000, BinaryIntegerHelper<CLong>.LeadingZeroCount(unchecked((CLong)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void PopCountTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CLong)0x0000000000000000), BinaryIntegerHelper<CLong>.PopCount(unchecked((CLong)0x0000000000000000)));
                Assert.Equal(unchecked((CLong)0x0000000000000001), BinaryIntegerHelper<CLong>.PopCount(unchecked((CLong)0x0000000000000001)));
                Assert.Equal(unchecked((CLong)0x000000000000003F), BinaryIntegerHelper<CLong>.PopCount(unchecked((CLong)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((CLong)0x0000000000000001), BinaryIntegerHelper<CLong>.PopCount(unchecked((CLong)0x8000000000000000)));
                Assert.Equal(unchecked((CLong)0x0000000000000040), BinaryIntegerHelper<CLong>.PopCount(unchecked((CLong)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((CLong)0x00000000, BinaryIntegerHelper<CLong>.PopCount((CLong)0x00000000));
                Assert.Equal((CLong)0x00000001, BinaryIntegerHelper<CLong>.PopCount((CLong)0x00000001));
                Assert.Equal((CLong)0x0000001F, BinaryIntegerHelper<CLong>.PopCount((CLong)0x7FFFFFFF));
                Assert.Equal((CLong)0x00000001, BinaryIntegerHelper<CLong>.PopCount(unchecked((CLong)0x80000000)));
                Assert.Equal((CLong)0x00000020, BinaryIntegerHelper<CLong>.PopCount(unchecked((CLong)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void RotateLeftTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CLong)0x0000000000000000), BinaryIntegerHelper<CLong>.RotateLeft(unchecked((CLong)0x0000000000000000), 1));
                Assert.Equal(unchecked((CLong)0x0000000000000002), BinaryIntegerHelper<CLong>.RotateLeft(unchecked((CLong)0x0000000000000001), 1));
                Assert.Equal(unchecked((CLong)0xFFFFFFFFFFFFFFFE), BinaryIntegerHelper<CLong>.RotateLeft(unchecked((CLong)0x7FFFFFFFFFFFFFFF), 1));
                Assert.Equal(unchecked((CLong)0x0000000000000001), BinaryIntegerHelper<CLong>.RotateLeft(unchecked((CLong)0x8000000000000000), 1));
                Assert.Equal(unchecked((CLong)0xFFFFFFFFFFFFFFFF), BinaryIntegerHelper<CLong>.RotateLeft(unchecked((CLong)0xFFFFFFFFFFFFFFFF), 1));
            }
            else
            {
                Assert.Equal((CLong)0x00000000, BinaryIntegerHelper<CLong>.RotateLeft((CLong)0x00000000, 1));
                Assert.Equal((CLong)0x00000002, BinaryIntegerHelper<CLong>.RotateLeft((CLong)0x00000001, 1));
                Assert.Equal(unchecked((CLong)0xFFFFFFFE), BinaryIntegerHelper<CLong>.RotateLeft((CLong)0x7FFFFFFF, 1));
                Assert.Equal((CLong)0x00000001, BinaryIntegerHelper<CLong>.RotateLeft(unchecked((CLong)0x80000000), 1));
                Assert.Equal(unchecked((CLong)0xFFFFFFFF), BinaryIntegerHelper<CLong>.RotateLeft(unchecked((CLong)0xFFFFFFFF), 1));
            }
        }

        [Fact]
        public static void RotateRightTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CLong)0x0000000000000000), BinaryIntegerHelper<CLong>.RotateRight(unchecked((CLong)0x0000000000000000), 1));
                Assert.Equal(unchecked((CLong)0x8000000000000000), BinaryIntegerHelper<CLong>.RotateRight(unchecked((CLong)0x0000000000000001), 1));
                Assert.Equal(unchecked((CLong)0xBFFFFFFFFFFFFFFF), BinaryIntegerHelper<CLong>.RotateRight(unchecked((CLong)0x7FFFFFFFFFFFFFFF), 1));
                Assert.Equal(unchecked((CLong)0x4000000000000000), BinaryIntegerHelper<CLong>.RotateRight(unchecked((CLong)0x8000000000000000), 1));
                Assert.Equal(unchecked((CLong)0xFFFFFFFFFFFFFFFF), BinaryIntegerHelper<CLong>.RotateRight(unchecked((CLong)0xFFFFFFFFFFFFFFFF), 1));
            }
            else
            {
                Assert.Equal((CLong)0x00000000, BinaryIntegerHelper<CLong>.RotateRight((CLong)0x00000000, 1));
                Assert.Equal(unchecked((CLong)0x80000000), BinaryIntegerHelper<CLong>.RotateRight((CLong)0x00000001, 1));
                Assert.Equal(unchecked((CLong)0xBFFFFFFF), BinaryIntegerHelper<CLong>.RotateRight((CLong)0x7FFFFFFF, 1));
                Assert.Equal((CLong)0x40000000, BinaryIntegerHelper<CLong>.RotateRight(unchecked((CLong)0x80000000), 1));
                Assert.Equal(unchecked((CLong)0xFFFFFFFF), BinaryIntegerHelper<CLong>.RotateRight(unchecked((CLong)0xFFFFFFFF), 1));
            }
        }

        [Fact]
        public static void TrailingZeroCountTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CLong)0x0000000000000040), BinaryIntegerHelper<CLong>.TrailingZeroCount(unchecked((CLong)0x0000000000000000)));
                Assert.Equal(unchecked((CLong)0x0000000000000000), BinaryIntegerHelper<CLong>.TrailingZeroCount(unchecked((CLong)0x0000000000000001)));
                Assert.Equal(unchecked((CLong)0x0000000000000000), BinaryIntegerHelper<CLong>.TrailingZeroCount(unchecked((CLong)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((CLong)0x000000000000003F), BinaryIntegerHelper<CLong>.TrailingZeroCount(unchecked((CLong)0x8000000000000000)));
                Assert.Equal(unchecked((CLong)0x0000000000000000), BinaryIntegerHelper<CLong>.TrailingZeroCount(unchecked((CLong)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((CLong)0x00000020, BinaryIntegerHelper<CLong>.TrailingZeroCount((CLong)0x00000000));
                Assert.Equal((CLong)0x00000000, BinaryIntegerHelper<CLong>.TrailingZeroCount((CLong)0x00000001));
                Assert.Equal((CLong)0x00000000, BinaryIntegerHelper<CLong>.TrailingZeroCount((CLong)0x7FFFFFFF));
                Assert.Equal((CLong)0x0000001F, BinaryIntegerHelper<CLong>.TrailingZeroCount(unchecked((CLong)0x80000000)));
                Assert.Equal((CLong)0x00000000, BinaryIntegerHelper<CLong>.TrailingZeroCount(unchecked((CLong)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void GetShortestBitLengthTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(0x00, BinaryIntegerHelper<CLong>.GetShortestBitLength(unchecked((CLong)0x0000000000000000)));
                Assert.Equal(0x01, BinaryIntegerHelper<CLong>.GetShortestBitLength(unchecked((CLong)0x0000000000000001)));
                Assert.Equal(0x3F, BinaryIntegerHelper<CLong>.GetShortestBitLength(unchecked((CLong)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(0x40, BinaryIntegerHelper<CLong>.GetShortestBitLength(unchecked((CLong)0x8000000000000000)));
                Assert.Equal(0x01, BinaryIntegerHelper<CLong>.GetShortestBitLength(unchecked((CLong)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal(0x00, BinaryIntegerHelper<CLong>.GetShortestBitLength((CLong)0x00000000));
                Assert.Equal(0x01, BinaryIntegerHelper<CLong>.GetShortestBitLength((CLong)0x00000001));
                Assert.Equal(0x1F, BinaryIntegerHelper<CLong>.GetShortestBitLength((CLong)0x7FFFFFFF));
                Assert.Equal(0x20, BinaryIntegerHelper<CLong>.GetShortestBitLength(unchecked((CLong)0x80000000)));
                Assert.Equal(0x01, BinaryIntegerHelper<CLong>.GetShortestBitLength(unchecked((CLong)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void IsPow2Test()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.False(BinaryNumberHelper<CLong>.IsPow2(unchecked((CLong)0x0000000000000000)));
                Assert.True(BinaryNumberHelper<CLong>.IsPow2(unchecked((CLong)0x0000000000000001)));
                Assert.False(BinaryNumberHelper<CLong>.IsPow2(unchecked((CLong)0x7FFFFFFFFFFFFFFF)));
                Assert.False(BinaryNumberHelper<CLong>.IsPow2(unchecked((CLong)0x8000000000000000)));
                Assert.False(BinaryNumberHelper<CLong>.IsPow2(unchecked((CLong)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.False(BinaryNumberHelper<CLong>.IsPow2((CLong)0x00000000));
                Assert.True(BinaryNumberHelper<CLong>.IsPow2((CLong)0x00000001));
                Assert.False(BinaryNumberHelper<CLong>.IsPow2((CLong)0x7FFFFFFF));
                Assert.False(BinaryNumberHelper<CLong>.IsPow2(unchecked((CLong)0x80000000)));
                Assert.False(BinaryNumberHelper<CLong>.IsPow2(unchecked((CLong)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void Log2Test()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CLong)0x0000000000000000), BinaryNumberHelper<CLong>.Log2(unchecked((CLong)0x0000000000000000)));
                Assert.Equal(unchecked((CLong)0x0000000000000000), BinaryNumberHelper<CLong>.Log2(unchecked((CLong)0x0000000000000001)));
                Assert.Equal(unchecked((CLong)0x000000000000003E), BinaryNumberHelper<CLong>.Log2(unchecked((CLong)0x7FFFFFFFFFFFFFFF)));
                Assert.Throws<ArgumentOutOfRangeException>(() => BinaryNumberHelper<CLong>.Log2(unchecked((CLong)0x8000000000000000)));
                Assert.Throws<ArgumentOutOfRangeException>(() => BinaryNumberHelper<CLong>.Log2(unchecked((CLong)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((CLong)0x00000000, BinaryNumberHelper<CLong>.Log2((CLong)0x00000000));
                Assert.Equal((CLong)0x00000000, BinaryNumberHelper<CLong>.Log2((CLong)0x00000001));
                Assert.Equal((CLong)0x0000001E, BinaryNumberHelper<CLong>.Log2((CLong)0x7FFFFFFF));
                Assert.Throws<ArgumentOutOfRangeException>(() => BinaryNumberHelper<CLong>.Log2(unchecked((CLong)0x80000000)));
                Assert.Throws<ArgumentOutOfRangeException>(() => BinaryNumberHelper<CLong>.Log2(unchecked((CLong)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void op_BitwiseAndTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CLong)0x0000000000000000), BitwiseOperatorsHelper<CLong, CLong, CLong>.op_BitwiseAnd(unchecked((CLong)0x0000000000000000), (CLong)1));
                Assert.Equal(unchecked((CLong)0x0000000000000001), BitwiseOperatorsHelper<CLong, CLong, CLong>.op_BitwiseAnd(unchecked((CLong)0x0000000000000001), (CLong)1));
                Assert.Equal(unchecked((CLong)0x0000000000000001), BitwiseOperatorsHelper<CLong, CLong, CLong>.op_BitwiseAnd(unchecked((CLong)0x7FFFFFFFFFFFFFFF), (CLong)1));
                Assert.Equal(unchecked((CLong)0x0000000000000000), BitwiseOperatorsHelper<CLong, CLong, CLong>.op_BitwiseAnd(unchecked((CLong)0x8000000000000000), (CLong)1));
                Assert.Equal(unchecked((CLong)0x0000000000000001), BitwiseOperatorsHelper<CLong, CLong, CLong>.op_BitwiseAnd(unchecked((CLong)0xFFFFFFFFFFFFFFFF), (CLong)1));
            }
            else
            {
                Assert.Equal((CLong)0x00000000, BitwiseOperatorsHelper<CLong, CLong, CLong>.op_BitwiseAnd((CLong)0x00000000, (CLong)1));
                Assert.Equal((CLong)0x00000001, BitwiseOperatorsHelper<CLong, CLong, CLong>.op_BitwiseAnd((CLong)0x00000001, (CLong)1));
                Assert.Equal((CLong)0x00000001, BitwiseOperatorsHelper<CLong, CLong, CLong>.op_BitwiseAnd((CLong)0x7FFFFFFF, (CLong)1));
                Assert.Equal((CLong)0x00000000, BitwiseOperatorsHelper<CLong, CLong, CLong>.op_BitwiseAnd(unchecked((CLong)0x80000000), (CLong)1));
                Assert.Equal((CLong)0x00000001, BitwiseOperatorsHelper<CLong, CLong, CLong>.op_BitwiseAnd(unchecked((CLong)0xFFFFFFFF), (CLong)1));
            }
        }

        [Fact]
        public static void op_BitwiseOrTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CLong)0x0000000000000001), BitwiseOperatorsHelper<CLong, CLong, CLong>.op_BitwiseOr(unchecked((CLong)0x0000000000000000), (CLong)1));
                Assert.Equal(unchecked((CLong)0x0000000000000001), BitwiseOperatorsHelper<CLong, CLong, CLong>.op_BitwiseOr(unchecked((CLong)0x0000000000000001), (CLong)1));
                Assert.Equal(unchecked((CLong)0x7FFFFFFFFFFFFFFF), BitwiseOperatorsHelper<CLong, CLong, CLong>.op_BitwiseOr(unchecked((CLong)0x7FFFFFFFFFFFFFFF), (CLong)1));
                Assert.Equal(unchecked((CLong)0x8000000000000001), BitwiseOperatorsHelper<CLong, CLong, CLong>.op_BitwiseOr(unchecked((CLong)0x8000000000000000), (CLong)1));
                Assert.Equal(unchecked((CLong)0xFFFFFFFFFFFFFFFF), BitwiseOperatorsHelper<CLong, CLong, CLong>.op_BitwiseOr(unchecked((CLong)0xFFFFFFFFFFFFFFFF), (CLong)1));
            }
            else
            {
                Assert.Equal((CLong)0x00000001, BitwiseOperatorsHelper<CLong, CLong, CLong>.op_BitwiseOr((CLong)0x00000000, (CLong)1));
                Assert.Equal((CLong)0x00000001, BitwiseOperatorsHelper<CLong, CLong, CLong>.op_BitwiseOr((CLong)0x00000001, (CLong)1));
                Assert.Equal((CLong)0x7FFFFFFF, BitwiseOperatorsHelper<CLong, CLong, CLong>.op_BitwiseOr((CLong)0x7FFFFFFF, (CLong)1));
                Assert.Equal(unchecked((CLong)0x80000001), BitwiseOperatorsHelper<CLong, CLong, CLong>.op_BitwiseOr(unchecked((CLong)0x80000000), (CLong)1));
                Assert.Equal(unchecked((CLong)0xFFFFFFFF), BitwiseOperatorsHelper<CLong, CLong, CLong>.op_BitwiseOr(unchecked((CLong)0xFFFFFFFF), (CLong)1));
            }
        }

        [Fact]
        public static void op_ExclusiveOrTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CLong)0x0000000000000001), BitwiseOperatorsHelper<CLong, CLong, CLong>.op_ExclusiveOr(unchecked((CLong)0x0000000000000000), (CLong)1));
                Assert.Equal(unchecked((CLong)0x0000000000000000), BitwiseOperatorsHelper<CLong, CLong, CLong>.op_ExclusiveOr(unchecked((CLong)0x0000000000000001), (CLong)1));
                Assert.Equal(unchecked((CLong)0x7FFFFFFFFFFFFFFE), BitwiseOperatorsHelper<CLong, CLong, CLong>.op_ExclusiveOr(unchecked((CLong)0x7FFFFFFFFFFFFFFF), (CLong)1));
                Assert.Equal(unchecked((CLong)0x8000000000000001), BitwiseOperatorsHelper<CLong, CLong, CLong>.op_ExclusiveOr(unchecked((CLong)0x8000000000000000), (CLong)1));
                Assert.Equal(unchecked((CLong)0xFFFFFFFFFFFFFFFE), BitwiseOperatorsHelper<CLong, CLong, CLong>.op_ExclusiveOr(unchecked((CLong)0xFFFFFFFFFFFFFFFF), (CLong)1));
            }
            else
            {
                Assert.Equal((CLong)0x00000001, BitwiseOperatorsHelper<CLong, CLong, CLong>.op_ExclusiveOr((CLong)0x00000000, (CLong)1));
                Assert.Equal((CLong)0x00000000, BitwiseOperatorsHelper<CLong, CLong, CLong>.op_ExclusiveOr((CLong)0x00000001, (CLong)1));
                Assert.Equal((CLong)0x7FFFFFFE, BitwiseOperatorsHelper<CLong, CLong, CLong>.op_ExclusiveOr((CLong)0x7FFFFFFF, (CLong)1));
                Assert.Equal(unchecked((CLong)0x80000001), BitwiseOperatorsHelper<CLong, CLong, CLong>.op_ExclusiveOr(unchecked((CLong)0x80000000), (CLong)1));
                Assert.Equal(unchecked((CLong)0xFFFFFFFE), BitwiseOperatorsHelper<CLong, CLong, CLong>.op_ExclusiveOr(unchecked((CLong)0xFFFFFFFF), (CLong)1));
            }
        }

        [Fact]
        public static void op_OnesComplementTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CLong)0xFFFFFFFFFFFFFFFF), BitwiseOperatorsHelper<CLong, CLong, CLong>.op_OnesComplement(unchecked((CLong)0x0000000000000000)));
                Assert.Equal(unchecked((CLong)0xFFFFFFFFFFFFFFFE), BitwiseOperatorsHelper<CLong, CLong, CLong>.op_OnesComplement(unchecked((CLong)0x0000000000000001)));
                Assert.Equal(unchecked((CLong)0x8000000000000000), BitwiseOperatorsHelper<CLong, CLong, CLong>.op_OnesComplement(unchecked((CLong)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((CLong)0x7FFFFFFFFFFFFFFF), BitwiseOperatorsHelper<CLong, CLong, CLong>.op_OnesComplement(unchecked((CLong)0x8000000000000000)));
                Assert.Equal(unchecked((CLong)0x0000000000000000), BitwiseOperatorsHelper<CLong, CLong, CLong>.op_OnesComplement(unchecked((CLong)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal(unchecked((CLong)0xFFFFFFFF), BitwiseOperatorsHelper<CLong, CLong, CLong>.op_OnesComplement((CLong)0x00000000));
                Assert.Equal(unchecked((CLong)0xFFFFFFFE), BitwiseOperatorsHelper<CLong, CLong, CLong>.op_OnesComplement((CLong)0x00000001));
                Assert.Equal(unchecked((CLong)0x80000000), BitwiseOperatorsHelper<CLong, CLong, CLong>.op_OnesComplement((CLong)0x7FFFFFFF));
                Assert.Equal((CLong)0x7FFFFFFF, BitwiseOperatorsHelper<CLong, CLong, CLong>.op_OnesComplement(unchecked((CLong)0x80000000)));
                Assert.Equal((CLong)0x00000000, BitwiseOperatorsHelper<CLong, CLong, CLong>.op_OnesComplement(unchecked((CLong)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void op_LessThanTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.True(ComparisonOperatorsHelper<CLong, CLong>.op_LessThan(unchecked((CLong)0x0000000000000000), (CLong)1));
                Assert.False(ComparisonOperatorsHelper<CLong, CLong>.op_LessThan(unchecked((CLong)0x0000000000000001), (CLong)1));
                Assert.False(ComparisonOperatorsHelper<CLong, CLong>.op_LessThan(unchecked((CLong)0x7FFFFFFFFFFFFFFF), (CLong)1));
                Assert.True(ComparisonOperatorsHelper<CLong, CLong>.op_LessThan(unchecked((CLong)0x8000000000000000), (CLong)1));
                Assert.True(ComparisonOperatorsHelper<CLong, CLong>.op_LessThan(unchecked((CLong)0xFFFFFFFFFFFFFFFF), (CLong)1));
            }
            else
            {
                Assert.True(ComparisonOperatorsHelper<CLong, CLong>.op_LessThan((CLong)0x00000000, (CLong)1));
                Assert.False(ComparisonOperatorsHelper<CLong, CLong>.op_LessThan((CLong)0x00000001, (CLong)1));
                Assert.False(ComparisonOperatorsHelper<CLong, CLong>.op_LessThan((CLong)0x7FFFFFFF, (CLong)1));
                Assert.True(ComparisonOperatorsHelper<CLong, CLong>.op_LessThan(unchecked((CLong)0x80000000), (CLong)1));
                Assert.True(ComparisonOperatorsHelper<CLong, CLong>.op_LessThan(unchecked((CLong)0xFFFFFFFF), (CLong)1));
            }
        }

        [Fact]
        public static void op_LessThanOrEqualTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.True(ComparisonOperatorsHelper<CLong, CLong>.op_LessThanOrEqual(unchecked((CLong)0x0000000000000000), (CLong)1));
                Assert.True(ComparisonOperatorsHelper<CLong, CLong>.op_LessThanOrEqual(unchecked((CLong)0x0000000000000001), (CLong)1));
                Assert.False(ComparisonOperatorsHelper<CLong, CLong>.op_LessThanOrEqual(unchecked((CLong)0x7FFFFFFFFFFFFFFF), (CLong)1));
                Assert.True(ComparisonOperatorsHelper<CLong, CLong>.op_LessThanOrEqual(unchecked((CLong)0x8000000000000000), (CLong)1));
                Assert.True(ComparisonOperatorsHelper<CLong, CLong>.op_LessThanOrEqual(unchecked((CLong)0xFFFFFFFFFFFFFFFF), (CLong)1));
            }
            else
            {
                Assert.True(ComparisonOperatorsHelper<CLong, CLong>.op_LessThanOrEqual((CLong)0x00000000, (CLong)1));
                Assert.True(ComparisonOperatorsHelper<CLong, CLong>.op_LessThanOrEqual((CLong)0x00000001, (CLong)1));
                Assert.False(ComparisonOperatorsHelper<CLong, CLong>.op_LessThanOrEqual((CLong)0x7FFFFFFF, (CLong)1));
                Assert.True(ComparisonOperatorsHelper<CLong, CLong>.op_LessThanOrEqual(unchecked((CLong)0x80000000), (CLong)1));
                Assert.True(ComparisonOperatorsHelper<CLong, CLong>.op_LessThanOrEqual(unchecked((CLong)0xFFFFFFFF), (CLong)1));
            }
        }

        [Fact]
        public static void op_GreaterThanTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.False(ComparisonOperatorsHelper<CLong, CLong>.op_GreaterThan(unchecked((CLong)0x0000000000000000), (CLong)1));
                Assert.False(ComparisonOperatorsHelper<CLong, CLong>.op_GreaterThan(unchecked((CLong)0x0000000000000001), (CLong)1));
                Assert.True(ComparisonOperatorsHelper<CLong, CLong>.op_GreaterThan(unchecked((CLong)0x7FFFFFFFFFFFFFFF), (CLong)1));
                Assert.False(ComparisonOperatorsHelper<CLong, CLong>.op_GreaterThan(unchecked((CLong)0x8000000000000000), (CLong)1));
                Assert.False(ComparisonOperatorsHelper<CLong, CLong>.op_GreaterThan(unchecked((CLong)0xFFFFFFFFFFFFFFFF), (CLong)1));
            }
            else
            {
                Assert.False(ComparisonOperatorsHelper<CLong, CLong>.op_GreaterThan((CLong)0x00000000, (CLong)1));
                Assert.False(ComparisonOperatorsHelper<CLong, CLong>.op_GreaterThan((CLong)0x00000001, (CLong)1));
                Assert.True(ComparisonOperatorsHelper<CLong, CLong>.op_GreaterThan((CLong)0x7FFFFFFF, (CLong)1));
                Assert.False(ComparisonOperatorsHelper<CLong, CLong>.op_GreaterThan(unchecked((CLong)0x80000000), (CLong)1));
                Assert.False(ComparisonOperatorsHelper<CLong, CLong>.op_GreaterThan(unchecked((CLong)0xFFFFFFFF), (CLong)1));
            }
        }

        [Fact]
        public static void op_GreaterThanOrEqualTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.False(ComparisonOperatorsHelper<CLong, CLong>.op_GreaterThanOrEqual(unchecked((CLong)0x0000000000000000), (CLong)1));
                Assert.True(ComparisonOperatorsHelper<CLong, CLong>.op_GreaterThanOrEqual(unchecked((CLong)0x0000000000000001), (CLong)1));
                Assert.True(ComparisonOperatorsHelper<CLong, CLong>.op_GreaterThanOrEqual(unchecked((CLong)0x7FFFFFFFFFFFFFFF), (CLong)1));
                Assert.False(ComparisonOperatorsHelper<CLong, CLong>.op_GreaterThanOrEqual(unchecked((CLong)0x8000000000000000), (CLong)1));
                Assert.False(ComparisonOperatorsHelper<CLong, CLong>.op_GreaterThanOrEqual(unchecked((CLong)0xFFFFFFFFFFFFFFFF), (CLong)1));
            }
            else
            {
                Assert.False(ComparisonOperatorsHelper<CLong, CLong>.op_GreaterThanOrEqual((CLong)0x00000000, (CLong)1));
                Assert.True(ComparisonOperatorsHelper<CLong, CLong>.op_GreaterThanOrEqual((CLong)0x00000001, (CLong)1));
                Assert.True(ComparisonOperatorsHelper<CLong, CLong>.op_GreaterThanOrEqual((CLong)0x7FFFFFFF, (CLong)1));
                Assert.False(ComparisonOperatorsHelper<CLong, CLong>.op_GreaterThanOrEqual(unchecked((CLong)0x80000000), (CLong)1));
                Assert.False(ComparisonOperatorsHelper<CLong, CLong>.op_GreaterThanOrEqual(unchecked((CLong)0xFFFFFFFF), (CLong)1));
            }
        }

        [Fact]
        public static void op_DecrementTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CLong)0xFFFFFFFFFFFFFFFF), DecrementOperatorsHelper<CLong>.op_Decrement(unchecked((CLong)0x0000000000000000)));
                Assert.Equal(unchecked((CLong)0x0000000000000000), DecrementOperatorsHelper<CLong>.op_Decrement(unchecked((CLong)0x0000000000000001)));
                Assert.Equal(unchecked((CLong)0x7FFFFFFFFFFFFFFE), DecrementOperatorsHelper<CLong>.op_Decrement(unchecked((CLong)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((CLong)0x7FFFFFFFFFFFFFFF), DecrementOperatorsHelper<CLong>.op_Decrement(unchecked((CLong)0x8000000000000000)));
                Assert.Equal(unchecked((CLong)0xFFFFFFFFFFFFFFFE), DecrementOperatorsHelper<CLong>.op_Decrement(unchecked((CLong)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal(unchecked((CLong)0xFFFFFFFF), DecrementOperatorsHelper<CLong>.op_Decrement((CLong)0x00000000));
                Assert.Equal((CLong)0x00000000, DecrementOperatorsHelper<CLong>.op_Decrement((CLong)0x00000001));
                Assert.Equal((CLong)0x7FFFFFFE, DecrementOperatorsHelper<CLong>.op_Decrement((CLong)0x7FFFFFFF));
                Assert.Equal((CLong)0x7FFFFFFF, DecrementOperatorsHelper<CLong>.op_Decrement(unchecked((CLong)0x80000000)));
                Assert.Equal(unchecked((CLong)0xFFFFFFFE), DecrementOperatorsHelper<CLong>.op_Decrement(unchecked((CLong)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void op_CheckedDecrementTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CLong)0xFFFFFFFFFFFFFFFF), DecrementOperatorsHelper<CLong>.op_CheckedDecrement(unchecked((CLong)0x0000000000000000)));
                Assert.Equal(unchecked((CLong)0x0000000000000000), DecrementOperatorsHelper<CLong>.op_CheckedDecrement(unchecked((CLong)0x0000000000000001)));
                Assert.Equal(unchecked((CLong)0x7FFFFFFFFFFFFFFE), DecrementOperatorsHelper<CLong>.op_CheckedDecrement(unchecked((CLong)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((CLong)0xFFFFFFFFFFFFFFFE), DecrementOperatorsHelper<CLong>.op_CheckedDecrement(unchecked((CLong)0xFFFFFFFFFFFFFFFF)));

                Assert.Throws<OverflowException>(() => DecrementOperatorsHelper<CLong>.op_CheckedDecrement(unchecked((CLong)0x8000000000000000)));
            }
            else
            {
                Assert.Equal(unchecked((CLong)0xFFFFFFFF), DecrementOperatorsHelper<CLong>.op_CheckedDecrement((CLong)0x00000000));
                Assert.Equal((CLong)0x00000000, DecrementOperatorsHelper<CLong>.op_CheckedDecrement((CLong)0x00000001));
                Assert.Equal((CLong)0x7FFFFFFE, DecrementOperatorsHelper<CLong>.op_CheckedDecrement((CLong)0x7FFFFFFF));
                Assert.Equal(unchecked((CLong)0xFFFFFFFE), DecrementOperatorsHelper<CLong>.op_CheckedDecrement(unchecked((CLong)0xFFFFFFFF)));

                Assert.Throws<OverflowException>(() => DecrementOperatorsHelper<CLong>.op_CheckedDecrement(unchecked((CLong)0x80000000)));
            }
        }

        [Fact]
        public static void op_DivisionTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CLong)0x0000000000000000), DivisionOperatorsHelper<CLong, CLong, CLong>.op_Division(unchecked((CLong)0x0000000000000000), (CLong)2));
                Assert.Equal(unchecked((CLong)0x0000000000000000), DivisionOperatorsHelper<CLong, CLong, CLong>.op_Division(unchecked((CLong)0x0000000000000001), (CLong)2));
                Assert.Equal(unchecked((CLong)0x3FFFFFFFFFFFFFFF), DivisionOperatorsHelper<CLong, CLong, CLong>.op_Division(unchecked((CLong)0x7FFFFFFFFFFFFFFF), (CLong)2));
                Assert.Equal(unchecked((CLong)0xC000000000000000), DivisionOperatorsHelper<CLong, CLong, CLong>.op_Division(unchecked((CLong)0x8000000000000000), (CLong)2));
                Assert.Equal(unchecked((CLong)0x0000000000000000), DivisionOperatorsHelper<CLong, CLong, CLong>.op_Division(unchecked((CLong)0xFFFFFFFFFFFFFFFF), (CLong)2));

                Assert.Throws<DivideByZeroException>(() => DivisionOperatorsHelper<CLong, CLong, CLong>.op_Division(unchecked((CLong)0x0000000000000001), (CLong)0));
            }
            else
            {
                Assert.Equal((CLong)0x00000000, DivisionOperatorsHelper<CLong, CLong, CLong>.op_Division((CLong)0x00000000, (CLong)2));
                Assert.Equal((CLong)0x00000000, DivisionOperatorsHelper<CLong, CLong, CLong>.op_Division((CLong)0x00000001, (CLong)2));
                Assert.Equal((CLong)0x3FFFFFFF, DivisionOperatorsHelper<CLong, CLong, CLong>.op_Division((CLong)0x7FFFFFFF, (CLong)2));
                Assert.Equal(unchecked((CLong)0xC0000000), DivisionOperatorsHelper<CLong, CLong, CLong>.op_Division(unchecked((CLong)0x80000000), (CLong)2));
                Assert.Equal((CLong)0x00000000, DivisionOperatorsHelper<CLong, CLong, CLong>.op_Division(unchecked((CLong)0xFFFFFFFF), (CLong)2));

                Assert.Throws<DivideByZeroException>(() => DivisionOperatorsHelper<CLong, CLong, CLong>.op_Division((CLong)0x00000001, (CLong)0));
            }
        }

        [Fact]
        public static void op_CheckedDivisionTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CLong)0x0000000000000000), DivisionOperatorsHelper<CLong, CLong, CLong>.op_CheckedDivision(unchecked((CLong)0x0000000000000000), (CLong)2));
                Assert.Equal(unchecked((CLong)0x0000000000000000), DivisionOperatorsHelper<CLong, CLong, CLong>.op_CheckedDivision(unchecked((CLong)0x0000000000000001), (CLong)2));
                Assert.Equal(unchecked((CLong)0x3FFFFFFFFFFFFFFF), DivisionOperatorsHelper<CLong, CLong, CLong>.op_CheckedDivision(unchecked((CLong)0x7FFFFFFFFFFFFFFF), (CLong)2));
                Assert.Equal(unchecked((CLong)0xC000000000000000), DivisionOperatorsHelper<CLong, CLong, CLong>.op_CheckedDivision(unchecked((CLong)0x8000000000000000), (CLong)2));
                Assert.Equal(unchecked((CLong)0x0000000000000000), DivisionOperatorsHelper<CLong, CLong, CLong>.op_CheckedDivision(unchecked((CLong)0xFFFFFFFFFFFFFFFF), (CLong)2));

                Assert.Throws<DivideByZeroException>(() => DivisionOperatorsHelper<CLong, CLong, CLong>.op_CheckedDivision(unchecked((CLong)0x0000000000000001), (CLong)0));
            }
            else
            {
                Assert.Equal((CLong)0x00000000, DivisionOperatorsHelper<CLong, CLong, CLong>.op_CheckedDivision((CLong)0x00000000, (CLong)2));
                Assert.Equal((CLong)0x00000000, DivisionOperatorsHelper<CLong, CLong, CLong>.op_CheckedDivision((CLong)0x00000001, (CLong)2));
                Assert.Equal((CLong)0x3FFFFFFF, DivisionOperatorsHelper<CLong, CLong, CLong>.op_CheckedDivision((CLong)0x7FFFFFFF, (CLong)2));
                Assert.Equal(unchecked((CLong)0xC0000000), DivisionOperatorsHelper<CLong, CLong, CLong>.op_CheckedDivision(unchecked((CLong)0x80000000), (CLong)2));
                Assert.Equal((CLong)0x00000000, DivisionOperatorsHelper<CLong, CLong, CLong>.op_CheckedDivision(unchecked((CLong)0xFFFFFFFF), (CLong)2));

                Assert.Throws<DivideByZeroException>(() => DivisionOperatorsHelper<CLong, CLong, CLong>.op_CheckedDivision((CLong)0x00000001, (CLong)0));
            }
        }

        [Fact]
        public static void op_EqualityTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.False(EqualityOperatorsHelper<CLong, CLong>.op_Equality(unchecked((CLong)0x0000000000000000), (CLong)1));
                Assert.True(EqualityOperatorsHelper<CLong, CLong>.op_Equality(unchecked((CLong)0x0000000000000001), (CLong)1));
                Assert.False(EqualityOperatorsHelper<CLong, CLong>.op_Equality(unchecked((CLong)0x7FFFFFFFFFFFFFFF), (CLong)1));
                Assert.False(EqualityOperatorsHelper<CLong, CLong>.op_Equality(unchecked((CLong)0x8000000000000000), (CLong)1));
                Assert.False(EqualityOperatorsHelper<CLong, CLong>.op_Equality(unchecked((CLong)0xFFFFFFFFFFFFFFFF), (CLong)1));
            }
            else
            {
                Assert.False(EqualityOperatorsHelper<CLong, CLong>.op_Equality((CLong)0x00000000, (CLong)1));
                Assert.True(EqualityOperatorsHelper<CLong, CLong>.op_Equality((CLong)0x00000001, (CLong)1));
                Assert.False(EqualityOperatorsHelper<CLong, CLong>.op_Equality((CLong)0x7FFFFFFF, (CLong)1));
                Assert.False(EqualityOperatorsHelper<CLong, CLong>.op_Equality(unchecked((CLong)0x80000000), (CLong)1));
                Assert.False(EqualityOperatorsHelper<CLong, CLong>.op_Equality(unchecked((CLong)0xFFFFFFFF), (CLong)1));
            }
        }

        [Fact]
        public static void op_InequalityTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.True(EqualityOperatorsHelper<CLong, CLong>.op_Inequality(unchecked((CLong)0x0000000000000000), (CLong)1));
                Assert.False(EqualityOperatorsHelper<CLong, CLong>.op_Inequality(unchecked((CLong)0x0000000000000001), (CLong)1));
                Assert.True(EqualityOperatorsHelper<CLong, CLong>.op_Inequality(unchecked((CLong)0x7FFFFFFFFFFFFFFF), (CLong)1));
                Assert.True(EqualityOperatorsHelper<CLong, CLong>.op_Inequality(unchecked((CLong)0x8000000000000000), (CLong)1));
                Assert.True(EqualityOperatorsHelper<CLong, CLong>.op_Inequality(unchecked((CLong)0xFFFFFFFFFFFFFFFF), (CLong)1));
            }
            else
            {
                Assert.True(EqualityOperatorsHelper<CLong, CLong>.op_Inequality((CLong)0x00000000, (CLong)1));
                Assert.False(EqualityOperatorsHelper<CLong, CLong>.op_Inequality((CLong)0x00000001, (CLong)1));
                Assert.True(EqualityOperatorsHelper<CLong, CLong>.op_Inequality((CLong)0x7FFFFFFF, (CLong)1));
                Assert.True(EqualityOperatorsHelper<CLong, CLong>.op_Inequality(unchecked((CLong)0x80000000), (CLong)1));
                Assert.True(EqualityOperatorsHelper<CLong, CLong>.op_Inequality(unchecked((CLong)0xFFFFFFFF), (CLong)1));
            }
        }

        [Fact]
        public static void op_IncrementTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CLong)0x0000000000000001), IncrementOperatorsHelper<CLong>.op_Increment(unchecked((CLong)0x0000000000000000)));
                Assert.Equal(unchecked((CLong)0x0000000000000002), IncrementOperatorsHelper<CLong>.op_Increment(unchecked((CLong)0x0000000000000001)));
                Assert.Equal(unchecked((CLong)0x8000000000000000), IncrementOperatorsHelper<CLong>.op_Increment(unchecked((CLong)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((CLong)0x8000000000000001), IncrementOperatorsHelper<CLong>.op_Increment(unchecked((CLong)0x8000000000000000)));
                Assert.Equal(unchecked((CLong)0x0000000000000000), IncrementOperatorsHelper<CLong>.op_Increment(unchecked((CLong)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((CLong)0x00000001, IncrementOperatorsHelper<CLong>.op_Increment((CLong)0x00000000));
                Assert.Equal((CLong)0x00000002, IncrementOperatorsHelper<CLong>.op_Increment((CLong)0x00000001));
                Assert.Equal(unchecked((CLong)0x80000000), IncrementOperatorsHelper<CLong>.op_Increment((CLong)0x7FFFFFFF));
                Assert.Equal(unchecked((CLong)0x80000001), IncrementOperatorsHelper<CLong>.op_Increment(unchecked((CLong)0x80000000)));
                Assert.Equal((CLong)0x00000000, IncrementOperatorsHelper<CLong>.op_Increment(unchecked((CLong)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void op_CheckedIncrementTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CLong)0x0000000000000001), IncrementOperatorsHelper<CLong>.op_CheckedIncrement(unchecked((CLong)0x0000000000000000)));
                Assert.Equal(unchecked((CLong)0x0000000000000002), IncrementOperatorsHelper<CLong>.op_CheckedIncrement(unchecked((CLong)0x0000000000000001)));
                Assert.Equal(unchecked((CLong)0x8000000000000001), IncrementOperatorsHelper<CLong>.op_CheckedIncrement(unchecked((CLong)0x8000000000000000)));
                Assert.Equal(unchecked((CLong)0x0000000000000000), IncrementOperatorsHelper<CLong>.op_CheckedIncrement(unchecked((CLong)0xFFFFFFFFFFFFFFFF)));

                Assert.Throws<OverflowException>(() => IncrementOperatorsHelper<CLong>.op_CheckedIncrement(unchecked((CLong)0x7FFFFFFFFFFFFFFF)));

            }
            else
            {
                Assert.Equal((CLong)0x00000001, IncrementOperatorsHelper<CLong>.op_CheckedIncrement((CLong)0x00000000));
                Assert.Equal((CLong)0x00000002, IncrementOperatorsHelper<CLong>.op_CheckedIncrement((CLong)0x00000001));
                Assert.Equal(unchecked((CLong)0x80000001), IncrementOperatorsHelper<CLong>.op_CheckedIncrement(unchecked((CLong)0x80000000)));
                Assert.Equal((CLong)0x00000000, IncrementOperatorsHelper<CLong>.op_CheckedIncrement(unchecked((CLong)0xFFFFFFFF)));

                Assert.Throws<OverflowException>(() => IncrementOperatorsHelper<CLong>.op_CheckedIncrement((CLong)0x7FFFFFFF));
            }
        }

        [Fact]
        public static void op_ModulusTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CLong)0x0000000000000000), ModulusOperatorsHelper<CLong, CLong, CLong>.op_Modulus(unchecked((CLong)0x0000000000000000), (CLong)2));
                Assert.Equal(unchecked((CLong)0x0000000000000001), ModulusOperatorsHelper<CLong, CLong, CLong>.op_Modulus(unchecked((CLong)0x0000000000000001), (CLong)2));
                Assert.Equal(unchecked((CLong)0x0000000000000001), ModulusOperatorsHelper<CLong, CLong, CLong>.op_Modulus(unchecked((CLong)0x7FFFFFFFFFFFFFFF), (CLong)2));
                Assert.Equal(unchecked((CLong)0x0000000000000000), ModulusOperatorsHelper<CLong, CLong, CLong>.op_Modulus(unchecked((CLong)0x8000000000000000), (CLong)2));
                Assert.Equal(unchecked((CLong)0xFFFFFFFFFFFFFFFF), ModulusOperatorsHelper<CLong, CLong, CLong>.op_Modulus(unchecked((CLong)0xFFFFFFFFFFFFFFFF), (CLong)2));

                Assert.Throws<DivideByZeroException>(() => ModulusOperatorsHelper<CLong, CLong, CLong>.op_Modulus(unchecked((CLong)0x0000000000000001), (CLong)0));
            }
            else
            {
                Assert.Equal((CLong)0x00000000, ModulusOperatorsHelper<CLong, CLong, CLong>.op_Modulus((CLong)0x00000000, (CLong)2));
                Assert.Equal((CLong)0x00000001, ModulusOperatorsHelper<CLong, CLong, CLong>.op_Modulus((CLong)0x00000001, (CLong)2));
                Assert.Equal((CLong)0x00000001, ModulusOperatorsHelper<CLong, CLong, CLong>.op_Modulus((CLong)0x7FFFFFFF, (CLong)2));
                Assert.Equal((CLong)0x00000000, ModulusOperatorsHelper<CLong, CLong, CLong>.op_Modulus(unchecked((CLong)0x80000000), (CLong)2));
                Assert.Equal(unchecked((CLong)0xFFFFFFFF), ModulusOperatorsHelper<CLong, CLong, CLong>.op_Modulus(unchecked((CLong)0xFFFFFFFF), (CLong)2));

                Assert.Throws<DivideByZeroException>(() => ModulusOperatorsHelper<CLong, CLong, CLong>.op_Modulus((CLong)0x00000001, (CLong)0));
            }
        }

        [Fact]
        public static void op_MultiplyTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CLong)0x0000000000000000), MultiplyOperatorsHelper<CLong, CLong, CLong>.op_Multiply(unchecked((CLong)0x0000000000000000), (CLong)2));
                Assert.Equal(unchecked((CLong)0x0000000000000002), MultiplyOperatorsHelper<CLong, CLong, CLong>.op_Multiply(unchecked((CLong)0x0000000000000001), (CLong)2));
                Assert.Equal(unchecked((CLong)0xFFFFFFFFFFFFFFFE), MultiplyOperatorsHelper<CLong, CLong, CLong>.op_Multiply(unchecked((CLong)0x7FFFFFFFFFFFFFFF), (CLong)2));
                Assert.Equal(unchecked((CLong)0x0000000000000000), MultiplyOperatorsHelper<CLong, CLong, CLong>.op_Multiply(unchecked((CLong)0x8000000000000000), (CLong)2));
                Assert.Equal(unchecked((CLong)0xFFFFFFFFFFFFFFFE), MultiplyOperatorsHelper<CLong, CLong, CLong>.op_Multiply(unchecked((CLong)0xFFFFFFFFFFFFFFFF), (CLong)2));
            }
            else
            {
                Assert.Equal((CLong)0x00000000, MultiplyOperatorsHelper<CLong, CLong, CLong>.op_Multiply((CLong)0x00000000, (CLong)2));
                Assert.Equal((CLong)0x00000002, MultiplyOperatorsHelper<CLong, CLong, CLong>.op_Multiply((CLong)0x00000001, (CLong)2));
                Assert.Equal(unchecked((CLong)0xFFFFFFFE), MultiplyOperatorsHelper<CLong, CLong, CLong>.op_Multiply((CLong)0x7FFFFFFF, (CLong)2));
                Assert.Equal((CLong)0x00000000, MultiplyOperatorsHelper<CLong, CLong, CLong>.op_Multiply(unchecked((CLong)0x80000000), (CLong)2));
                Assert.Equal(unchecked((CLong)0xFFFFFFFE), MultiplyOperatorsHelper<CLong, CLong, CLong>.op_Multiply(unchecked((CLong)0xFFFFFFFF), (CLong)2));
            }
        }

        [Fact]
        public static void op_CheckedMultiplyTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CLong)0x0000000000000000), MultiplyOperatorsHelper<CLong, CLong, CLong>.op_CheckedMultiply(unchecked((CLong)0x0000000000000000), (CLong)2));
                Assert.Equal(unchecked((CLong)0x0000000000000002), MultiplyOperatorsHelper<CLong, CLong, CLong>.op_CheckedMultiply(unchecked((CLong)0x0000000000000001), (CLong)2));
                Assert.Equal(unchecked((CLong)0xFFFFFFFFFFFFFFFE), MultiplyOperatorsHelper<CLong, CLong, CLong>.op_CheckedMultiply(unchecked((CLong)0xFFFFFFFFFFFFFFFF), (CLong)2));

                Assert.Throws<OverflowException>(() => MultiplyOperatorsHelper<CLong, CLong, CLong>.op_CheckedMultiply(unchecked((CLong)0x7FFFFFFFFFFFFFFF), (CLong)2));
                Assert.Throws<OverflowException>(() => MultiplyOperatorsHelper<CLong, CLong, CLong>.op_CheckedMultiply(unchecked((CLong)0x8000000000000000), (CLong)2));
            }
            else
            {
                Assert.Equal((CLong)0x00000000, MultiplyOperatorsHelper<CLong, CLong, CLong>.op_CheckedMultiply((CLong)0x00000000, (CLong)2));
                Assert.Equal((CLong)0x00000002, MultiplyOperatorsHelper<CLong, CLong, CLong>.op_CheckedMultiply((CLong)0x00000001, (CLong)2));
                Assert.Equal(unchecked((CLong)0xFFFFFFFE), MultiplyOperatorsHelper<CLong, CLong, CLong>.op_CheckedMultiply(unchecked((CLong)0xFFFFFFFF), (CLong)2));

                Assert.Throws<OverflowException>(() => MultiplyOperatorsHelper<CLong, CLong, CLong>.op_CheckedMultiply((CLong)0x7FFFFFFF, (CLong)2));
                Assert.Throws<OverflowException>(() => MultiplyOperatorsHelper<CLong, CLong, CLong>.op_CheckedMultiply(unchecked((CLong)0x80000000), (CLong)2));
            }
        }

        [Fact]
        public static void AbsTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CLong)0x0000000000000000), NumberHelper<CLong>.Abs(unchecked((CLong)0x0000000000000000)));
                Assert.Equal(unchecked((CLong)0x0000000000000001), NumberHelper<CLong>.Abs(unchecked((CLong)0x0000000000000001)));
                Assert.Equal(unchecked((CLong)0x7FFFFFFFFFFFFFFF), NumberHelper<CLong>.Abs(unchecked((CLong)0x7FFFFFFFFFFFFFFF)));
                Assert.Throws<OverflowException>(() => NumberHelper<CLong>.Abs(unchecked((CLong)0x8000000000000000)));
                Assert.Equal(unchecked((CLong)0x0000000000000001), NumberHelper<CLong>.Abs(unchecked((CLong)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((CLong)0x00000000, NumberHelper<CLong>.Abs((CLong)0x00000000));
                Assert.Equal((CLong)0x00000001, NumberHelper<CLong>.Abs((CLong)0x00000001));
                Assert.Equal((CLong)0x7FFFFFFF, NumberHelper<CLong>.Abs((CLong)0x7FFFFFFF));
                Assert.Throws<OverflowException>(() => NumberHelper<CLong>.Abs(unchecked((CLong)0x80000000)));
                Assert.Equal((CLong)0x00000001, NumberHelper<CLong>.Abs(unchecked((CLong)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void ClampTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CLong)0x0000000000000000), NumberHelper<CLong>.Clamp(unchecked((CLong)0x0000000000000000), unchecked((CLong)0xFFFFFFFFFFFFFFC0), unchecked((CLong)0x000000000000003F)));
                Assert.Equal(unchecked((CLong)0x0000000000000001), NumberHelper<CLong>.Clamp(unchecked((CLong)0x0000000000000001), unchecked((CLong)0xFFFFFFFFFFFFFFC0), unchecked((CLong)0x000000000000003F)));
                Assert.Equal(unchecked((CLong)0x000000000000003F), NumberHelper<CLong>.Clamp(unchecked((CLong)0x7FFFFFFFFFFFFFFF), unchecked((CLong)0xFFFFFFFFFFFFFFC0), unchecked((CLong)0x000000000000003F)));
                Assert.Equal(unchecked((CLong)0xFFFFFFFFFFFFFFC0), NumberHelper<CLong>.Clamp(unchecked((CLong)0x8000000000000000), unchecked((CLong)0xFFFFFFFFFFFFFFC0), unchecked((CLong)0x000000000000003F)));
                Assert.Equal(unchecked((CLong)0xFFFFFFFFFFFFFFFF), NumberHelper<CLong>.Clamp(unchecked((CLong)0xFFFFFFFFFFFFFFFF), unchecked((CLong)0xFFFFFFFFFFFFFFC0), unchecked((CLong)0x000000000000003F)));
            }
            else
            {
                Assert.Equal((CLong)0x00000000, NumberHelper<CLong>.Clamp((CLong)0x00000000, unchecked((CLong)0xFFFFFFC0), (CLong)0x0000003F));
                Assert.Equal((CLong)0x00000001, NumberHelper<CLong>.Clamp((CLong)0x00000001, unchecked((CLong)0xFFFFFFC0), (CLong)0x0000003F));
                Assert.Equal((CLong)0x0000003F, NumberHelper<CLong>.Clamp((CLong)0x7FFFFFFF, unchecked((CLong)0xFFFFFFC0), (CLong)0x0000003F));
                Assert.Equal(unchecked((CLong)0xFFFFFFC0), NumberHelper<CLong>.Clamp(unchecked((CLong)0x80000000), unchecked((CLong)0xFFFFFFC0), (CLong)0x0000003F));
                Assert.Equal(unchecked((CLong)0xFFFFFFFF), NumberHelper<CLong>.Clamp(unchecked((CLong)0xFFFFFFFF), unchecked((CLong)0xFFFFFFC0), (CLong)0x0000003F));
            }
        }

        [Fact]
        public static void CreateCheckedFromByteTest()
        {
            Assert.Equal((CLong)0x00000000, NumberHelper<CLong>.CreateChecked<byte>(0x00));
            Assert.Equal((CLong)0x00000001, NumberHelper<CLong>.CreateChecked<byte>(0x01));
            Assert.Equal((CLong)0x0000007F, NumberHelper<CLong>.CreateChecked<byte>(0x7F));
            Assert.Equal((CLong)0x00000080, NumberHelper<CLong>.CreateChecked<byte>(0x80));
            Assert.Equal((CLong)0x000000FF, NumberHelper<CLong>.CreateChecked<byte>(0xFF));
        }

        [Fact]
        public static void CreateCheckedFromCharTest()
        {
            Assert.Equal((CLong)0x00000000, NumberHelper<CLong>.CreateChecked<char>((char)0x0000));
            Assert.Equal((CLong)0x00000001, NumberHelper<CLong>.CreateChecked<char>((char)0x0001));
            Assert.Equal((CLong)0x00007FFF, NumberHelper<CLong>.CreateChecked<char>((char)0x7FFF));
            Assert.Equal((CLong)0x00008000, NumberHelper<CLong>.CreateChecked<char>((char)0x8000));
            Assert.Equal((CLong)0x0000FFFF, NumberHelper<CLong>.CreateChecked<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateCheckedFromInt16Test()
        {
            Assert.Equal((CLong)0x00000000, NumberHelper<CLong>.CreateChecked<short>(0x0000));
            Assert.Equal((CLong)0x00000001, NumberHelper<CLong>.CreateChecked<short>(0x0001));
            Assert.Equal((CLong)0x00007FFF, NumberHelper<CLong>.CreateChecked<short>(0x7FFF));
            Assert.Equal(unchecked((CLong)(int)0xFFFF8000), NumberHelper<CLong>.CreateChecked<short>(unchecked((short)0x8000)));
            Assert.Equal(unchecked((CLong)(int)0xFFFFFFFF), NumberHelper<CLong>.CreateChecked<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromInt32Test()
        {
            Assert.Equal((CLong)0x00000000, NumberHelper<CLong>.CreateChecked<int>(0x00000000));
            Assert.Equal((CLong)0x00000001, NumberHelper<CLong>.CreateChecked<int>(0x00000001));
            Assert.Equal((CLong)0x7FFFFFFF, NumberHelper<CLong>.CreateChecked<int>(0x7FFFFFFF));
            Assert.Equal(unchecked((CLong)(int)0x80000000), NumberHelper<CLong>.CreateChecked<int>(unchecked((int)0x80000000)));
            Assert.Equal(unchecked((CLong)(int)0xFFFFFFFF), NumberHelper<CLong>.CreateChecked<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromInt64Test()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CLong)0x0000000000000000), NumberHelper<CLong>.CreateChecked<long>(0x0000000000000000));
                Assert.Equal(unchecked((CLong)0x0000000000000001), NumberHelper<CLong>.CreateChecked<long>(0x0000000000000001));
                Assert.Equal(unchecked((CLong)0x7FFFFFFFFFFFFFFF), NumberHelper<CLong>.CreateChecked<long>(0x7FFFFFFFFFFFFFFF));
                Assert.Equal(unchecked((CLong)0x8000000000000000), NumberHelper<CLong>.CreateChecked<long>(unchecked((long)0x8000000000000000)));
                Assert.Equal(unchecked((CLong)0xFFFFFFFFFFFFFFFF), NumberHelper<CLong>.CreateChecked<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((CLong)0x00000000, NumberHelper<CLong>.CreateChecked<long>(0x0000000000000000));
                Assert.Equal((CLong)0x00000001, NumberHelper<CLong>.CreateChecked<long>(0x0000000000000001));
                Assert.Throws<OverflowException>(() => NumberHelper<CLong>.CreateChecked<long>(0x7FFFFFFFFFFFFFFF));
                Assert.Throws<OverflowException>(() => NumberHelper<CLong>.CreateChecked<long>(unchecked((long)0x8000000000000000)));
                Assert.Equal(unchecked((CLong)0xFFFFFFFF), NumberHelper<CLong>.CreateChecked<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateCheckedFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CLong)0x00000000), NumberHelper<CLong>.CreateChecked<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal(unchecked((CLong)0x00000001), NumberHelper<CLong>.CreateChecked<nint>(unchecked((nint)0x0000000000000001)));

                if (OperatingSystem.IsWindows())
                {
                    Assert.Throws<OverflowException>(() => NumberHelper<CLong>.CreateChecked<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                    Assert.Throws<OverflowException>(() => NumberHelper<CLong>.CreateChecked<nint>(unchecked((nint)0x8000000000000000)));
                    Assert.Equal(unchecked((CLong)0xFFFFFFFF), NumberHelper<CLong>.CreateChecked<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
                }
                else
                {
                    Assert.Equal(unchecked((CLong)0x7FFFFFFFFFFFFFFF), NumberHelper<CLong>.CreateChecked<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                    Assert.Equal(unchecked((CLong)0x8000000000000000), NumberHelper<CLong>.CreateChecked<nint>(unchecked((nint)0x8000000000000000)));
                    Assert.Equal(unchecked((CLong)0xFFFFFFFFFFFFFFFF), NumberHelper<CLong>.CreateChecked<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
                }
            }
            else
            {
                Assert.Equal((CLong)0x00000000, NumberHelper<CLong>.CreateChecked<nint>((nint)0x00000000));
                Assert.Equal((CLong)0x00000001, NumberHelper<CLong>.CreateChecked<nint>((nint)0x00000001));
                Assert.Equal((CLong)0x7FFFFFFF, NumberHelper<CLong>.CreateChecked<nint>((nint)0x7FFFFFFF));
                Assert.Equal(unchecked((CLong)0x80000000), NumberHelper<CLong>.CreateChecked<nint>(unchecked(unchecked((nint)0x80000000))));
                Assert.Equal(unchecked((CLong)0xFFFFFFFF), NumberHelper<CLong>.CreateChecked<nint>(unchecked(unchecked((nint)0xFFFFFFFF))));
            }
        }

        [Fact]
        public static void CreateCheckedFromSByteTest()
        {
            Assert.Equal((CLong)0x00000000, NumberHelper<CLong>.CreateChecked<sbyte>(0x00));
            Assert.Equal((CLong)0x00000001, NumberHelper<CLong>.CreateChecked<sbyte>(0x01));
            Assert.Equal((CLong)0x0000007F, NumberHelper<CLong>.CreateChecked<sbyte>(0x7F));
            Assert.Equal(unchecked((CLong)(int)0xFFFFFF80), NumberHelper<CLong>.CreateChecked<sbyte>(unchecked((sbyte)0x80)));
            Assert.Equal(unchecked((CLong)(int)0xFFFFFFFF), NumberHelper<CLong>.CreateChecked<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateCheckedFromUInt16Test()
        {
            Assert.Equal((CLong)0x00000000, NumberHelper<CLong>.CreateChecked<ushort>(0x0000));
            Assert.Equal((CLong)0x00000001, NumberHelper<CLong>.CreateChecked<ushort>(0x0001));
            Assert.Equal((CLong)0x00007FFF, NumberHelper<CLong>.CreateChecked<ushort>(0x7FFF));
            Assert.Equal((CLong)0x00008000, NumberHelper<CLong>.CreateChecked<ushort>(0x8000));
            Assert.Equal((CLong)0x0000FFFF, NumberHelper<CLong>.CreateChecked<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUInt32Test()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CLong)0x0000000000000000), NumberHelper<CLong>.CreateChecked<uint>(0x00000000));
                Assert.Equal(unchecked((CLong)0x0000000000000001), NumberHelper<CLong>.CreateChecked<uint>(0x00000001));
                Assert.Equal(unchecked((CLong)0x000000007FFFFFFF), NumberHelper<CLong>.CreateChecked<uint>(0x7FFFFFFF));
                Assert.Equal(unchecked((CLong)0x0000000080000000), NumberHelper<CLong>.CreateChecked<uint>(0x80000000));
                Assert.Equal(unchecked((CLong)0x00000000FFFFFFFF), NumberHelper<CLong>.CreateChecked<uint>(0xFFFFFFFF));
            }
            else
            {
                Assert.Equal((CLong)0x00000000, NumberHelper<CLong>.CreateChecked<uint>(0x00000000));
                Assert.Equal((CLong)0x00000001, NumberHelper<CLong>.CreateChecked<uint>(0x00000001));
                Assert.Equal((CLong)0x7FFFFFFF, NumberHelper<CLong>.CreateChecked<uint>(0x7FFFFFFF));
                Assert.Throws<OverflowException>(() => NumberHelper<CLong>.CreateChecked<uint>(0x80000000));
                Assert.Throws<OverflowException>(() => NumberHelper<CLong>.CreateChecked<uint>(0xFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateCheckedFromUInt64Test()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CLong)0x0000000000000000), NumberHelper<CLong>.CreateChecked<ulong>(0x0000000000000000));
                Assert.Equal(unchecked((CLong)0x0000000000000001), NumberHelper<CLong>.CreateChecked<ulong>(0x0000000000000001));
                Assert.Equal(unchecked((CLong)0x7FFFFFFFFFFFFFFF), NumberHelper<CLong>.CreateChecked<ulong>(0x7FFFFFFFFFFFFFFF));
                Assert.Throws<OverflowException>(() => NumberHelper<CLong>.CreateChecked<ulong>(0x8000000000000000));
                Assert.Throws<OverflowException>(() => NumberHelper<CLong>.CreateChecked<ulong>(0xFFFFFFFFFFFFFFFF));
            }
            else
            {
                Assert.Equal((CLong)0x00000000, NumberHelper<CLong>.CreateChecked<ulong>(0x0000000000000000));
                Assert.Equal((CLong)0x00000001, NumberHelper<CLong>.CreateChecked<ulong>(0x0000000000000001));
                Assert.Throws<OverflowException>(() => NumberHelper<CLong>.CreateChecked<ulong>(0x7FFFFFFFFFFFFFFF));
                Assert.Throws<OverflowException>(() => NumberHelper<CLong>.CreateChecked<ulong>(0x8000000000000000));
                Assert.Throws<OverflowException>(() => NumberHelper<CLong>.CreateChecked<ulong>(0xFFFFFFFFFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateCheckedFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CLong)0x00000000), NumberHelper<CLong>.CreateChecked<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal(unchecked((CLong)0x00000001), NumberHelper<CLong>.CreateChecked<nuint>(unchecked((nuint)0x0000000000000001)));

                if (OperatingSystem.IsWindows())
                {
                    Assert.Throws<OverflowException>(() => NumberHelper<CLong>.CreateChecked<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                }
                else
                {
                    Assert.Equal(unchecked((CLong)0x7FFFFFFFFFFFFFFF), NumberHelper<CLong>.CreateChecked<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                }

                Assert.Throws<OverflowException>(() => NumberHelper<CLong>.CreateChecked<nuint>(unchecked((nuint)0x8000000000000000)));
                Assert.Throws<OverflowException>(() => NumberHelper<CLong>.CreateChecked<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((CLong)0x00000000, NumberHelper<CLong>.CreateChecked<nuint>((nuint)0x00000000));
                Assert.Equal((CLong)0x00000001, NumberHelper<CLong>.CreateChecked<nuint>((nuint)0x00000001));
                Assert.Equal((CLong)0x7FFFFFFF, NumberHelper<CLong>.CreateChecked<nuint>((nuint)0x7FFFFFFF));
                Assert.Throws<OverflowException>(() => NumberHelper<CLong>.CreateChecked<nuint>(unchecked((nuint)0x80000000)));
                Assert.Throws<OverflowException>(() => NumberHelper<CLong>.CreateChecked<nuint>(unchecked((nuint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateSaturatingFromByteTest()
        {
            Assert.Equal((CLong)0x00000000, NumberHelper<CLong>.CreateSaturating<byte>(0x00));
            Assert.Equal((CLong)0x00000001, NumberHelper<CLong>.CreateSaturating<byte>(0x01));
            Assert.Equal((CLong)0x0000007F, NumberHelper<CLong>.CreateSaturating<byte>(0x7F));
            Assert.Equal((CLong)0x00000080, NumberHelper<CLong>.CreateSaturating<byte>(0x80));
            Assert.Equal((CLong)0x000000FF, NumberHelper<CLong>.CreateSaturating<byte>(0xFF));
        }

        [Fact]
        public static void CreateSaturatingFromCharTest()
        {
            Assert.Equal((CLong)0x00000000, NumberHelper<CLong>.CreateSaturating<char>((char)0x0000));
            Assert.Equal((CLong)0x00000001, NumberHelper<CLong>.CreateSaturating<char>((char)0x0001));
            Assert.Equal((CLong)0x00007FFF, NumberHelper<CLong>.CreateSaturating<char>((char)0x7FFF));
            Assert.Equal((CLong)0x00008000, NumberHelper<CLong>.CreateSaturating<char>((char)0x8000));
            Assert.Equal((CLong)0x0000FFFF, NumberHelper<CLong>.CreateSaturating<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromInt16Test()
        {
            Assert.Equal((CLong)0x00000000, NumberHelper<CLong>.CreateSaturating<short>(0x0000));
            Assert.Equal((CLong)0x00000001, NumberHelper<CLong>.CreateSaturating<short>(0x0001));
            Assert.Equal((CLong)0x00007FFF, NumberHelper<CLong>.CreateSaturating<short>(0x7FFF));
            Assert.Equal(unchecked((CLong)(int)0xFFFF8000), NumberHelper<CLong>.CreateSaturating<short>(unchecked((short)0x8000)));
            Assert.Equal(unchecked((CLong)(int)0xFFFFFFFF), NumberHelper<CLong>.CreateSaturating<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromInt32Test()
        {
            Assert.Equal((CLong)0x00000000, NumberHelper<CLong>.CreateSaturating<int>(0x00000000));
            Assert.Equal((CLong)0x00000001, NumberHelper<CLong>.CreateSaturating<int>(0x00000001));
            Assert.Equal((CLong)0x7FFFFFFF, NumberHelper<CLong>.CreateSaturating<int>(0x7FFFFFFF));
            Assert.Equal(unchecked((CLong)(int)0x80000000), NumberHelper<CLong>.CreateSaturating<int>(unchecked((int)0x80000000)));
            Assert.Equal(unchecked((CLong)(int)0xFFFFFFFF), NumberHelper<CLong>.CreateSaturating<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromInt64Test()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CLong)0x0000000000000000), NumberHelper<CLong>.CreateSaturating<long>(0x0000000000000000));
                Assert.Equal(unchecked((CLong)0x0000000000000001), NumberHelper<CLong>.CreateSaturating<long>(0x0000000000000001));
                Assert.Equal(unchecked((CLong)0x7FFFFFFFFFFFFFFF), NumberHelper<CLong>.CreateSaturating<long>(0x7FFFFFFFFFFFFFFF));
                Assert.Equal(unchecked((CLong)0x8000000000000000), NumberHelper<CLong>.CreateSaturating<long>(unchecked((long)0x8000000000000000)));
                Assert.Equal(unchecked((CLong)0xFFFFFFFFFFFFFFFF), NumberHelper<CLong>.CreateSaturating<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((CLong)0x00000000, NumberHelper<CLong>.CreateSaturating<long>(0x0000000000000000));
                Assert.Equal((CLong)0x00000001, NumberHelper<CLong>.CreateSaturating<long>(0x0000000000000001));
                Assert.Equal(unchecked((CLong)0x7FFFFFFF), NumberHelper<CLong>.CreateSaturating<long>(0x7FFFFFFFFFFFFFFF));
                Assert.Equal(unchecked((CLong)0x80000000), NumberHelper<CLong>.CreateSaturating<long>(unchecked((long)0x8000000000000000)));
                Assert.Equal(unchecked((CLong)0xFFFFFFFF), NumberHelper<CLong>.CreateSaturating<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateSaturatingFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CLong)0x00000000), NumberHelper<CLong>.CreateSaturating<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal(unchecked((CLong)0x00000001), NumberHelper<CLong>.CreateSaturating<nint>(unchecked((nint)0x0000000000000001)));

                if (OperatingSystem.IsWindows())
                {
                    Assert.Equal(unchecked((CLong)0x7FFFFFFF), NumberHelper<CLong>.CreateSaturating<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                    Assert.Equal(unchecked((CLong)0x80000000), NumberHelper<CLong>.CreateSaturating<nint>(unchecked((nint)0x8000000000000000)));
                    Assert.Equal(unchecked((CLong)0xFFFFFFFF), NumberHelper<CLong>.CreateSaturating<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
                }
                else
                {
                    Assert.Equal(unchecked((CLong)0x7FFFFFFFFFFFFFFF), NumberHelper<CLong>.CreateSaturating<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                    Assert.Equal(unchecked((CLong)0x8000000000000000), NumberHelper<CLong>.CreateSaturating<nint>(unchecked((nint)0x8000000000000000)));
                    Assert.Equal(unchecked((CLong)0xFFFFFFFFFFFFFFFF), NumberHelper<CLong>.CreateSaturating<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
                }
            }
            else
            {
                Assert.Equal((CLong)0x00000000, NumberHelper<CLong>.CreateSaturating<nint>((nint)0x00000000));
                Assert.Equal((CLong)0x00000001, NumberHelper<CLong>.CreateSaturating<nint>((nint)0x00000001));
                Assert.Equal((CLong)0x7FFFFFFF, NumberHelper<CLong>.CreateSaturating<nint>((nint)0x7FFFFFFF));
                Assert.Equal(unchecked((CLong)0x80000000), NumberHelper<CLong>.CreateSaturating<nint>(unchecked(unchecked((nint)0x80000000))));
                Assert.Equal(unchecked((CLong)0xFFFFFFFF), NumberHelper<CLong>.CreateSaturating<nint>(unchecked(unchecked((nint)0xFFFFFFFF))));
            }
        }

        [Fact]
        public static void CreateSaturatingFromSByteTest()
        {
            Assert.Equal((CLong)0x00000000, NumberHelper<CLong>.CreateSaturating<sbyte>(0x00));
            Assert.Equal((CLong)0x00000001, NumberHelper<CLong>.CreateSaturating<sbyte>(0x01));
            Assert.Equal((CLong)0x0000007F, NumberHelper<CLong>.CreateSaturating<sbyte>(0x7F));
            Assert.Equal(unchecked((CLong)(int)0xFFFFFF80), NumberHelper<CLong>.CreateSaturating<sbyte>(unchecked((sbyte)0x80)));
            Assert.Equal(unchecked((CLong)(int)0xFFFFFFFF), NumberHelper<CLong>.CreateSaturating<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateSaturatingFromUInt16Test()
        {
            Assert.Equal((CLong)0x00000000, NumberHelper<CLong>.CreateSaturating<ushort>(0x0000));
            Assert.Equal((CLong)0x00000001, NumberHelper<CLong>.CreateSaturating<ushort>(0x0001));
            Assert.Equal((CLong)0x00007FFF, NumberHelper<CLong>.CreateSaturating<ushort>(0x7FFF));
            Assert.Equal((CLong)0x00008000, NumberHelper<CLong>.CreateSaturating<ushort>(0x8000));
            Assert.Equal((CLong)0x0000FFFF, NumberHelper<CLong>.CreateSaturating<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUInt32Test()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CLong)0x0000000000000000), NumberHelper<CLong>.CreateSaturating<uint>(0x00000000));
                Assert.Equal(unchecked((CLong)0x0000000000000001), NumberHelper<CLong>.CreateSaturating<uint>(0x00000001));
                Assert.Equal(unchecked((CLong)0x000000007FFFFFFF), NumberHelper<CLong>.CreateSaturating<uint>(0x7FFFFFFF));
                Assert.Equal(unchecked((CLong)0x0000000080000000), NumberHelper<CLong>.CreateSaturating<uint>(0x80000000));
                Assert.Equal(unchecked((CLong)0x00000000FFFFFFFF), NumberHelper<CLong>.CreateSaturating<uint>(0xFFFFFFFF));
            }
            else
            {
                Assert.Equal((CLong)0x00000000, NumberHelper<CLong>.CreateSaturating<uint>(0x00000000));
                Assert.Equal((CLong)0x00000001, NumberHelper<CLong>.CreateSaturating<uint>(0x00000001));
                Assert.Equal((CLong)0x7FFFFFFF, NumberHelper<CLong>.CreateSaturating<uint>(0x7FFFFFFF));
                Assert.Equal((CLong)0x7FFFFFFF, NumberHelper<CLong>.CreateSaturating<uint>(0x80000000));
                Assert.Equal((CLong)0x7FFFFFFF, NumberHelper<CLong>.CreateSaturating<uint>(0xFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateSaturatingFromUInt64Test()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CLong)0x0000000000000000), NumberHelper<CLong>.CreateSaturating<ulong>(0x0000000000000000));
                Assert.Equal(unchecked((CLong)0x0000000000000001), NumberHelper<CLong>.CreateSaturating<ulong>(0x0000000000000001));
                Assert.Equal(unchecked((CLong)0x7FFFFFFFFFFFFFFF), NumberHelper<CLong>.CreateSaturating<ulong>(0x7FFFFFFFFFFFFFFF));
                Assert.Equal(unchecked((CLong)0x7FFFFFFFFFFFFFFF), NumberHelper<CLong>.CreateSaturating<ulong>(0x8000000000000000));
                Assert.Equal(unchecked((CLong)0x7FFFFFFFFFFFFFFF), NumberHelper<CLong>.CreateSaturating<ulong>(0xFFFFFFFFFFFFFFFF));
            }
            else
            {
                Assert.Equal((CLong)0x00000000, NumberHelper<CLong>.CreateSaturating<ulong>(0x0000000000000000));
                Assert.Equal((CLong)0x00000001, NumberHelper<CLong>.CreateSaturating<ulong>(0x0000000000000001));
                Assert.Equal((CLong)0x7FFFFFFF, NumberHelper<CLong>.CreateSaturating<ulong>(0x7FFFFFFFFFFFFFFF));
                Assert.Equal((CLong)0x7FFFFFFF, NumberHelper<CLong>.CreateSaturating<ulong>(0x8000000000000000));
                Assert.Equal((CLong)0x7FFFFFFF, NumberHelper<CLong>.CreateSaturating<ulong>(0xFFFFFFFFFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateSaturatingFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CLong)0x00000000), NumberHelper<CLong>.CreateSaturating<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal(unchecked((CLong)0x00000001), NumberHelper<CLong>.CreateSaturating<nuint>(unchecked((nuint)0x0000000000000001)));

                if (OperatingSystem.IsWindows())
                {
                    Assert.Equal(unchecked((CLong)0x7FFFFFFF), NumberHelper<CLong>.CreateSaturating<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                    Assert.Equal(unchecked((CLong)0x7FFFFFFF), NumberHelper<CLong>.CreateSaturating<nuint>(unchecked((nuint)0x8000000000000000)));
                    Assert.Equal(unchecked((CLong)0x7FFFFFFF), NumberHelper<CLong>.CreateSaturating<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
                }
                else
                {
                    Assert.Equal(unchecked((CLong)0x7FFFFFFFFFFFFFFF), NumberHelper<CLong>.CreateSaturating<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                    Assert.Equal(unchecked((CLong)0x7FFFFFFFFFFFFFFF), NumberHelper<CLong>.CreateSaturating<nuint>(unchecked((nuint)0x8000000000000000)));
                    Assert.Equal(unchecked((CLong)0x7FFFFFFFFFFFFFFF), NumberHelper<CLong>.CreateSaturating<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
                }
            }
            else
            {
                Assert.Equal((CLong)0x00000000, NumberHelper<CLong>.CreateSaturating<nuint>((nuint)0x00000000));
                Assert.Equal((CLong)0x00000001, NumberHelper<CLong>.CreateSaturating<nuint>((nuint)0x00000001));
                Assert.Equal((CLong)0x7FFFFFFF, NumberHelper<CLong>.CreateSaturating<nuint>((nuint)0x7FFFFFFF));
                Assert.Equal((CLong)0x7FFFFFFF, NumberHelper<CLong>.CreateSaturating<nuint>(unchecked((nuint)0x80000000)));
                Assert.Equal((CLong)0x7FFFFFFF, NumberHelper<CLong>.CreateSaturating<nuint>(unchecked((nuint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateTruncatingFromByteTest()
        {
            Assert.Equal((CLong)0x00000000, NumberHelper<CLong>.CreateTruncating<byte>(0x00));
            Assert.Equal((CLong)0x00000001, NumberHelper<CLong>.CreateTruncating<byte>(0x01));
            Assert.Equal((CLong)0x0000007F, NumberHelper<CLong>.CreateTruncating<byte>(0x7F));
            Assert.Equal((CLong)0x00000080, NumberHelper<CLong>.CreateTruncating<byte>(0x80));
            Assert.Equal((CLong)0x000000FF, NumberHelper<CLong>.CreateTruncating<byte>(0xFF));
        }

        [Fact]
        public static void CreateTruncatingFromCharTest()
        {
            Assert.Equal((CLong)0x00000000, NumberHelper<CLong>.CreateTruncating<char>((char)0x0000));
            Assert.Equal((CLong)0x00000001, NumberHelper<CLong>.CreateTruncating<char>((char)0x0001));
            Assert.Equal((CLong)0x00007FFF, NumberHelper<CLong>.CreateTruncating<char>((char)0x7FFF));
            Assert.Equal((CLong)0x00008000, NumberHelper<CLong>.CreateTruncating<char>((char)0x8000));
            Assert.Equal((CLong)0x0000FFFF, NumberHelper<CLong>.CreateTruncating<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromInt16Test()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CLong)0x0000000000000000), NumberHelper<CLong>.CreateTruncating<short>(0x0000));
                Assert.Equal(unchecked((CLong)0x0000000000000001), NumberHelper<CLong>.CreateTruncating<short>(0x0001));
                Assert.Equal(unchecked((CLong)0x0000000000007FFF), NumberHelper<CLong>.CreateTruncating<short>(0x7FFF));
                Assert.Equal(unchecked((CLong)0xFFFFFFFFFFFF8000), NumberHelper<CLong>.CreateTruncating<short>(unchecked((short)0x8000)));
                Assert.Equal(unchecked((CLong)0xFFFFFFFFFFFFFFFF), NumberHelper<CLong>.CreateTruncating<short>(unchecked((short)0xFFFF)));
            }
            else
            {
                Assert.Equal((CLong)0x00000000, NumberHelper<CLong>.CreateTruncating<short>(0x0000));
                Assert.Equal((CLong)0x00000001, NumberHelper<CLong>.CreateTruncating<short>(0x0001));
                Assert.Equal((CLong)0x00007FFF, NumberHelper<CLong>.CreateTruncating<short>(0x7FFF));
                Assert.Equal(unchecked((CLong)0xFFFF8000), NumberHelper<CLong>.CreateTruncating<short>(unchecked((short)0x8000)));
                Assert.Equal(unchecked((CLong)0xFFFFFFFF), NumberHelper<CLong>.CreateTruncating<short>(unchecked((short)0xFFFF)));
            }
        }

        [Fact]
        public static void CreateTruncatingFromInt32Test()
        {
            Assert.Equal((CLong)0x00000000, NumberHelper<CLong>.CreateTruncating<int>(0x00000000));
            Assert.Equal((CLong)0x00000001, NumberHelper<CLong>.CreateTruncating<int>(0x00000001));
            Assert.Equal((CLong)0x7FFFFFFF, NumberHelper<CLong>.CreateTruncating<int>(0x7FFFFFFF));
            Assert.Equal(unchecked((CLong)(int)0x80000000), NumberHelper<CLong>.CreateTruncating<int>(unchecked((int)0x80000000)));
            Assert.Equal(unchecked((CLong)(int)0xFFFFFFFF), NumberHelper<CLong>.CreateTruncating<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateTruncatingFromInt64Test()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CLong)0x0000000000000000), NumberHelper<CLong>.CreateTruncating<long>(0x0000000000000000));
                Assert.Equal(unchecked((CLong)0x0000000000000001), NumberHelper<CLong>.CreateTruncating<long>(0x0000000000000001));
                Assert.Equal(unchecked((CLong)0x7FFFFFFFFFFFFFFF), NumberHelper<CLong>.CreateTruncating<long>(0x7FFFFFFFFFFFFFFF));
                Assert.Equal(unchecked((CLong)0x8000000000000000), NumberHelper<CLong>.CreateTruncating<long>(unchecked((long)0x8000000000000000)));
                Assert.Equal(unchecked((CLong)0xFFFFFFFFFFFFFFFF), NumberHelper<CLong>.CreateTruncating<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((CLong)0x00000000, NumberHelper<CLong>.CreateTruncating<long>(0x0000000000000000));
                Assert.Equal((CLong)0x00000001, NumberHelper<CLong>.CreateTruncating<long>(0x0000000000000001));
                Assert.Equal(unchecked((CLong)0xFFFFFFFF), NumberHelper<CLong>.CreateTruncating<long>(0x7FFFFFFFFFFFFFFF));
                Assert.Equal((CLong)0x00000000, NumberHelper<CLong>.CreateTruncating<long>(unchecked((long)0x8000000000000000)));
                Assert.Equal(unchecked((CLong)0xFFFFFFFF), NumberHelper<CLong>.CreateTruncating<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateTruncatingFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CLong)0x00000000), NumberHelper<CLong>.CreateTruncating<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal(unchecked((CLong)0x00000001), NumberHelper<CLong>.CreateTruncating<nint>(unchecked((nint)0x0000000000000001)));

                if (OperatingSystem.IsWindows())
                {
                    Assert.Equal(unchecked((CLong)0xFFFFFFFF), NumberHelper<CLong>.CreateTruncating<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                    Assert.Equal(unchecked((CLong)0x00000000), NumberHelper<CLong>.CreateTruncating<nint>(unchecked((nint)0x8000000000000000)));
                    Assert.Equal(unchecked((CLong)0xFFFFFFFF), NumberHelper<CLong>.CreateTruncating<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
                }
                else
                {
                    Assert.Equal(unchecked((CLong)0x7FFFFFFFFFFFFFFF), NumberHelper<CLong>.CreateTruncating<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                    Assert.Equal(unchecked((CLong)0x8000000000000000), NumberHelper<CLong>.CreateTruncating<nint>(unchecked((nint)0x8000000000000000)));
                    Assert.Equal(unchecked((CLong)0xFFFFFFFFFFFFFFFF), NumberHelper<CLong>.CreateTruncating<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
                }
            }
            else
            {
                Assert.Equal((CLong)0x00000000, NumberHelper<CLong>.CreateTruncating<nint>((nint)0x00000000));
                Assert.Equal((CLong)0x00000001, NumberHelper<CLong>.CreateTruncating<nint>((nint)0x00000001));
                Assert.Equal((CLong)0x7FFFFFFF, NumberHelper<CLong>.CreateTruncating<nint>((nint)0x7FFFFFFF));
                Assert.Equal(unchecked((CLong)0x80000000), NumberHelper<CLong>.CreateTruncating<nint>(unchecked(unchecked((nint)0x80000000))));
                Assert.Equal(unchecked((CLong)0xFFFFFFFF), NumberHelper<CLong>.CreateTruncating<nint>(unchecked(unchecked((nint)0xFFFFFFFF))));
            }
        }

        [Fact]
        public static void CreateTruncatingFromSByteTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CLong)0x0000000000000000), NumberHelper<CLong>.CreateTruncating<sbyte>(0x00));
                Assert.Equal(unchecked((CLong)0x0000000000000001), NumberHelper<CLong>.CreateTruncating<sbyte>(0x01));
                Assert.Equal(unchecked((CLong)0x000000000000007F), NumberHelper<CLong>.CreateTruncating<sbyte>(0x7F));
                Assert.Equal(unchecked((CLong)0xFFFFFFFFFFFFFF80), NumberHelper<CLong>.CreateTruncating<sbyte>(unchecked((sbyte)0x80)));
                Assert.Equal(unchecked((CLong)0xFFFFFFFFFFFFFFFF), NumberHelper<CLong>.CreateTruncating<sbyte>(unchecked((sbyte)0xFF)));
            }
            else
            {
                Assert.Equal((CLong)0x00000000, NumberHelper<CLong>.CreateTruncating<sbyte>(0x00));
                Assert.Equal((CLong)0x00000001, NumberHelper<CLong>.CreateTruncating<sbyte>(0x01));
                Assert.Equal((CLong)0x0000007F, NumberHelper<CLong>.CreateTruncating<sbyte>(0x7F));
                Assert.Equal(unchecked((CLong)0xFFFFFF80), NumberHelper<CLong>.CreateTruncating<sbyte>(unchecked((sbyte)0x80)));
                Assert.Equal(unchecked((CLong)0xFFFFFFFF), NumberHelper<CLong>.CreateTruncating<sbyte>(unchecked((sbyte)0xFF)));
            }
        }

        [Fact]
        public static void CreateTruncatingFromUInt16Test()
        {
            Assert.Equal((CLong)0x00000000, NumberHelper<CLong>.CreateTruncating<ushort>(0x0000));
            Assert.Equal((CLong)0x00000001, NumberHelper<CLong>.CreateTruncating<ushort>(0x0001));
            Assert.Equal((CLong)0x00007FFF, NumberHelper<CLong>.CreateTruncating<ushort>(0x7FFF));
            Assert.Equal((CLong)0x00008000, NumberHelper<CLong>.CreateTruncating<ushort>(0x8000));
            Assert.Equal((CLong)0x0000FFFF, NumberHelper<CLong>.CreateTruncating<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt32Test()
        {
            Assert.Equal((CLong)0x00000000, NumberHelper<CLong>.CreateTruncating<uint>(0x00000000));
            Assert.Equal((CLong)0x00000001, NumberHelper<CLong>.CreateTruncating<uint>(0x00000001));
            Assert.Equal((CLong)0x7FFFFFFF, NumberHelper<CLong>.CreateTruncating<uint>(0x7FFFFFFF));
            Assert.Equal(unchecked((CLong)0x80000000), NumberHelper<CLong>.CreateTruncating<uint>(0x80000000));
            Assert.Equal(unchecked((CLong)0xFFFFFFFF), NumberHelper<CLong>.CreateTruncating<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt64Test()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CLong)0x0000000000000000), NumberHelper<CLong>.CreateTruncating<ulong>(0x0000000000000000));
                Assert.Equal(unchecked((CLong)0x0000000000000001), NumberHelper<CLong>.CreateTruncating<ulong>(0x0000000000000001));
                Assert.Equal(unchecked((CLong)0x7FFFFFFFFFFFFFFF), NumberHelper<CLong>.CreateTruncating<ulong>(0x7FFFFFFFFFFFFFFF));
                Assert.Equal(unchecked((CLong)0x8000000000000000), NumberHelper<CLong>.CreateTruncating<ulong>(0x8000000000000000));
                Assert.Equal(unchecked((CLong)0xFFFFFFFFFFFFFFFF), NumberHelper<CLong>.CreateTruncating<ulong>(0xFFFFFFFFFFFFFFFF));
            }
            else
            {
                Assert.Equal((CLong)0x00000000, NumberHelper<CLong>.CreateTruncating<ulong>(0x0000000000000000));
                Assert.Equal((CLong)0x00000001, NumberHelper<CLong>.CreateTruncating<ulong>(0x0000000000000001));
                Assert.Equal(unchecked((CLong)0xFFFFFFFF), NumberHelper<CLong>.CreateTruncating<ulong>(0x7FFFFFFFFFFFFFFF));
                Assert.Equal((CLong)0x00000000, NumberHelper<CLong>.CreateTruncating<ulong>(0x8000000000000000));
                Assert.Equal(unchecked((CLong)0xFFFFFFFF), NumberHelper<CLong>.CreateTruncating<ulong>(0xFFFFFFFFFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateTruncatingFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CLong)0x00000000), NumberHelper<CLong>.CreateTruncating<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal(unchecked((CLong)0x00000001), NumberHelper<CLong>.CreateTruncating<nuint>(unchecked((nuint)0x0000000000000001)));

                if (OperatingSystem.IsWindows())
                {
                    Assert.Equal(unchecked((CLong)0xFFFFFFFF), NumberHelper<CLong>.CreateTruncating<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                    Assert.Equal(unchecked((CLong)0x00000000), NumberHelper<CLong>.CreateTruncating<nuint>(unchecked((nuint)0x8000000000000000)));
                    Assert.Equal(unchecked((CLong)0xFFFFFFFF), NumberHelper<CLong>.CreateTruncating<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
                }
                else
                {
                    Assert.Equal(unchecked((CLong)0x7FFFFFFFFFFFFFFF), NumberHelper<CLong>.CreateTruncating<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                    Assert.Equal(unchecked((CLong)0x8000000000000000), NumberHelper<CLong>.CreateTruncating<nuint>(unchecked((nuint)0x8000000000000000)));
                    Assert.Equal(unchecked((CLong)0xFFFFFFFFFFFFFFFF), NumberHelper<CLong>.CreateTruncating<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
                }
            }
            else
            {
                Assert.Equal((CLong)0x00000000, NumberHelper<CLong>.CreateTruncating<nuint>((nuint)0x00000000));
                Assert.Equal((CLong)0x00000001, NumberHelper<CLong>.CreateTruncating<nuint>((nuint)0x00000001));
                Assert.Equal((CLong)0x7FFFFFFF, NumberHelper<CLong>.CreateTruncating<nuint>((nuint)0x7FFFFFFF));
                Assert.Equal(unchecked((CLong)0x80000000), NumberHelper<CLong>.CreateTruncating<nuint>(unchecked((nuint)0x80000000)));
                Assert.Equal(unchecked((CLong)0xFFFFFFFF), NumberHelper<CLong>.CreateTruncating<nuint>(unchecked((nuint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void DivRemTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal((unchecked((CLong)0x0000000000000000), unchecked((CLong)0x0000000000000000)), BinaryIntegerHelper<CLong>.DivRem(unchecked((CLong)0x0000000000000000), (CLong)2));
                Assert.Equal((unchecked((CLong)0x0000000000000000), unchecked((CLong)0x0000000000000001)), BinaryIntegerHelper<CLong>.DivRem(unchecked((CLong)0x0000000000000001), (CLong)2));
                Assert.Equal((unchecked((CLong)0x3FFFFFFFFFFFFFFF), unchecked((CLong)0x0000000000000001)), BinaryIntegerHelper<CLong>.DivRem(unchecked((CLong)0x7FFFFFFFFFFFFFFF), (CLong)2));
                Assert.Equal((unchecked((CLong)0xC000000000000000), unchecked((CLong)0x0000000000000000)), BinaryIntegerHelper<CLong>.DivRem(unchecked((CLong)0x8000000000000000), (CLong)2));
                Assert.Equal((unchecked((CLong)0x0000000000000000), unchecked((CLong)0xFFFFFFFFFFFFFFFF)), BinaryIntegerHelper<CLong>.DivRem(unchecked((CLong)0xFFFFFFFFFFFFFFFF), (CLong)2));
            }
            else
            {
                Assert.Equal(((CLong)0x00000000, (CLong)0x00000000), BinaryIntegerHelper<CLong>.DivRem((CLong)0x00000000, (CLong)2));
                Assert.Equal(((CLong)0x00000000, (CLong)0x00000001), BinaryIntegerHelper<CLong>.DivRem((CLong)0x00000001, (CLong)2));
                Assert.Equal(((CLong)0x3FFFFFFF, (CLong)0x00000001), BinaryIntegerHelper<CLong>.DivRem((CLong)0x7FFFFFFF, (CLong)2));
                Assert.Equal((unchecked((CLong)0xC0000000), (CLong)0x00000000), BinaryIntegerHelper<CLong>.DivRem(unchecked((CLong)0x80000000), (CLong)2));
                Assert.Equal(((CLong)0x00000000, unchecked((CLong)0xFFFFFFFF)), BinaryIntegerHelper<CLong>.DivRem(unchecked((CLong)0xFFFFFFFF), (CLong)2));
            }
        }

        [Fact]
        public static void MaxTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CLong)0x0000000000000001), NumberHelper<CLong>.Max(unchecked((CLong)0x0000000000000000), (CLong)1));
                Assert.Equal(unchecked((CLong)0x0000000000000001), NumberHelper<CLong>.Max(unchecked((CLong)0x0000000000000001), (CLong)1));
                Assert.Equal(unchecked((CLong)0x7FFFFFFFFFFFFFFF), NumberHelper<CLong>.Max(unchecked((CLong)0x7FFFFFFFFFFFFFFF), (CLong)1));
                Assert.Equal(unchecked((CLong)0x0000000000000001), NumberHelper<CLong>.Max(unchecked((CLong)0x8000000000000000), (CLong)1));
                Assert.Equal(unchecked((CLong)0x0000000000000001), NumberHelper<CLong>.Max(unchecked((CLong)0xFFFFFFFFFFFFFFFF), (CLong)1));
            }
            else
            {
                Assert.Equal((CLong)0x00000001, NumberHelper<CLong>.Max((CLong)0x00000000, (CLong)1));
                Assert.Equal((CLong)0x00000001, NumberHelper<CLong>.Max((CLong)0x00000001, (CLong)1));
                Assert.Equal((CLong)0x7FFFFFFF, NumberHelper<CLong>.Max((CLong)0x7FFFFFFF, (CLong)1));
                Assert.Equal((CLong)0x00000001, NumberHelper<CLong>.Max(unchecked((CLong)0x80000000), (CLong)1));
                Assert.Equal((CLong)0x00000001, NumberHelper<CLong>.Max(unchecked((CLong)0xFFFFFFFF), (CLong)1));
            }
        }

        [Fact]
        public static void MinTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CLong)0x0000000000000000), NumberHelper<CLong>.Min(unchecked((CLong)0x0000000000000000), (CLong)1));
                Assert.Equal(unchecked((CLong)0x0000000000000001), NumberHelper<CLong>.Min(unchecked((CLong)0x0000000000000001), (CLong)1));
                Assert.Equal(unchecked((CLong)0x0000000000000001), NumberHelper<CLong>.Min(unchecked((CLong)0x7FFFFFFFFFFFFFFF), (CLong)1));
                Assert.Equal(unchecked((CLong)0x8000000000000000), NumberHelper<CLong>.Min(unchecked((CLong)0x8000000000000000), (CLong)1));
                Assert.Equal(unchecked((CLong)0xFFFFFFFFFFFFFFFF), NumberHelper<CLong>.Min(unchecked((CLong)0xFFFFFFFFFFFFFFFF), (CLong)1));
            }
            else
            {
                Assert.Equal((CLong)0x00000000, NumberHelper<CLong>.Min((CLong)0x00000000, (CLong)1));
                Assert.Equal((CLong)0x00000001, NumberHelper<CLong>.Min((CLong)0x00000001, (CLong)1));
                Assert.Equal((CLong)0x00000001, NumberHelper<CLong>.Min((CLong)0x7FFFFFFF, (CLong)1));
                Assert.Equal(unchecked((CLong)0x80000000), NumberHelper<CLong>.Min(unchecked((CLong)0x80000000), (CLong)1));
                Assert.Equal(unchecked((CLong)0xFFFFFFFF), NumberHelper<CLong>.Min(unchecked((CLong)0xFFFFFFFF), (CLong)1));
            }
        }

        [Fact]
        public static void SignTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(0, NumberHelper<CLong>.Sign(unchecked((CLong)0x0000000000000000)));
                Assert.Equal(1, NumberHelper<CLong>.Sign(unchecked((CLong)0x0000000000000001)));
                Assert.Equal(1, NumberHelper<CLong>.Sign(unchecked((CLong)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(-1, NumberHelper<CLong>.Sign(unchecked((CLong)0x8000000000000000)));
                Assert.Equal(-1, NumberHelper<CLong>.Sign(unchecked((CLong)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal(0, NumberHelper<CLong>.Sign((CLong)0x00000000));
                Assert.Equal(1, NumberHelper<CLong>.Sign((CLong)0x00000001));
                Assert.Equal(1, NumberHelper<CLong>.Sign((CLong)0x7FFFFFFF));
                Assert.Equal(-1, NumberHelper<CLong>.Sign(unchecked((CLong)0x80000000)));
                Assert.Equal(-1, NumberHelper<CLong>.Sign(unchecked((CLong)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void TryCreateFromByteTest()
        {
            CLong result;

            Assert.True(NumberHelper<CLong>.TryCreate<byte>(0x00, out result));
            Assert.Equal((CLong)0x00000000, result);

            Assert.True(NumberHelper<CLong>.TryCreate<byte>(0x01, out result));
            Assert.Equal((CLong)0x00000001, result);

            Assert.True(NumberHelper<CLong>.TryCreate<byte>(0x7F, out result));
            Assert.Equal((CLong)0x0000007F, result);

            Assert.True(NumberHelper<CLong>.TryCreate<byte>(0x80, out result));
            Assert.Equal((CLong)0x00000080, result);

            Assert.True(NumberHelper<CLong>.TryCreate<byte>(0xFF, out result));
            Assert.Equal((CLong)0x000000FF, result);
        }

        [Fact]
        public static void TryCreateFromCharTest()
        {
            CLong result;

            Assert.True(NumberHelper<CLong>.TryCreate<char>((char)0x0000, out result));
            Assert.Equal((CLong)0x00000000, result);

            Assert.True(NumberHelper<CLong>.TryCreate<char>((char)0x0001, out result));
            Assert.Equal((CLong)0x00000001, result);

            Assert.True(NumberHelper<CLong>.TryCreate<char>((char)0x7FFF, out result));
            Assert.Equal((CLong)0x00007FFF, result);

            Assert.True(NumberHelper<CLong>.TryCreate<char>((char)0x8000, out result));
            Assert.Equal((CLong)0x00008000, result);

            Assert.True(NumberHelper<CLong>.TryCreate<char>((char)0xFFFF, out result));
            Assert.Equal((CLong)0x0000FFFF, result);
        }

        [Fact]
        public static void TryCreateFromInt16Test()
        {
            CLong result;

            Assert.True(NumberHelper<CLong>.TryCreate<short>(0x0000, out result));
            Assert.Equal((CLong)0x00000000, result);

            Assert.True(NumberHelper<CLong>.TryCreate<short>(0x0001, out result));
            Assert.Equal((CLong)0x00000001, result);

            Assert.True(NumberHelper<CLong>.TryCreate<short>(0x7FFF, out result));
            Assert.Equal((CLong)0x00007FFF, result);

            Assert.True(NumberHelper<CLong>.TryCreate<short>(unchecked((short)0x8000), out result));
            Assert.Equal(unchecked((CLong)(int)0xFFFF8000), result);

            Assert.True(NumberHelper<CLong>.TryCreate<short>(unchecked((short)0xFFFF), out result));
            Assert.Equal(unchecked((CLong)(int)0xFFFFFFFF), result);
        }

        [Fact]
        public static void TryCreateFromInt32Test()
        {
            CLong result;

            Assert.True(NumberHelper<CLong>.TryCreate<int>(0x00000000, out result));
            Assert.Equal((CLong)0x00000000, result);

            Assert.True(NumberHelper<CLong>.TryCreate<int>(0x00000001, out result));
            Assert.Equal((CLong)0x00000001, result);

            Assert.True(NumberHelper<CLong>.TryCreate<int>(0x7FFFFFFF, out result));
            Assert.Equal((CLong)0x7FFFFFFF, result);

            Assert.True(NumberHelper<CLong>.TryCreate<int>(unchecked((int)0x80000000), out result));
            Assert.Equal(unchecked((CLong)(int)0x80000000), result);

            Assert.True(NumberHelper<CLong>.TryCreate<int>(unchecked((int)0xFFFFFFFF), out result));
            Assert.Equal(unchecked((CLong)(int)0xFFFFFFFF), result);
        }

        [Fact]
        public static void TryCreateFromInt64Test()
        {
            CLong result;

            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.True(NumberHelper<CLong>.TryCreate<long>(0x0000000000000000, out result));
                Assert.Equal(unchecked((CLong)0x0000000000000000), result);

                Assert.True(NumberHelper<CLong>.TryCreate<long>(0x0000000000000001, out result));
                Assert.Equal(unchecked((CLong)0x0000000000000001), result);

                Assert.True(NumberHelper<CLong>.TryCreate<long>(0x7FFFFFFFFFFFFFFF, out result));
                Assert.Equal(unchecked((CLong)0x7FFFFFFFFFFFFFFF), result);

                Assert.True(NumberHelper<CLong>.TryCreate<long>(unchecked((long)0x8000000000000000), out result));
                Assert.Equal(unchecked((CLong)0x8000000000000000), result);

                Assert.True(NumberHelper<CLong>.TryCreate<long>(unchecked((long)0xFFFFFFFFFFFFFFFF), out result));
                Assert.Equal(unchecked((CLong)0xFFFFFFFFFFFFFFFF), result);
            }
            else
            {
                Assert.True(NumberHelper<CLong>.TryCreate<long>(0x0000000000000000, out result));
                Assert.Equal((CLong)0x00000000, result);

                Assert.True(NumberHelper<CLong>.TryCreate<long>(0x0000000000000001, out result));
                Assert.Equal((CLong)0x00000001, result);

                Assert.False(NumberHelper<CLong>.TryCreate<long>(0x7FFFFFFFFFFFFFFF, out result));
                Assert.Equal((CLong)0x00000000, result);

                Assert.False(NumberHelper<CLong>.TryCreate<long>(unchecked((long)0x8000000000000000), out result));
                Assert.Equal((CLong)0x00000000, result);

                Assert.True(NumberHelper<CLong>.TryCreate<long>(unchecked((long)0xFFFFFFFFFFFFFFFF), out result));
                Assert.Equal(unchecked((CLong)0xFFFFFFFF), result);
            }
        }

        [Fact]
        public static void TryCreateFromIntPtrTest()
        {
            CLong result;

            if (Environment.Is64BitProcess)
            {
                Assert.True(NumberHelper<CLong>.TryCreate<nint>(unchecked((nint)0x0000000000000000), out result));
                Assert.Equal(unchecked((CLong)0x00000000), result);

                Assert.True(NumberHelper<CLong>.TryCreate<nint>(unchecked((nint)0x0000000000000001), out result));
                Assert.Equal(unchecked((CLong)0x00000001), result);

                if (OperatingSystem.IsWindows())
                {
                    Assert.False(NumberHelper<CLong>.TryCreate<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF), out result));
                    Assert.Equal(unchecked((CLong)0x00000000), result);

                    Assert.False(NumberHelper<CLong>.TryCreate<nint>(unchecked((nint)0x8000000000000000), out result));
                    Assert.Equal(unchecked((CLong)0x00000000), result);

                    Assert.True(NumberHelper<CLong>.TryCreate<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF), out result));
                    Assert.Equal(unchecked((CLong)0xFFFFFFFF), result);
                }
                else
                {
                    Assert.True(NumberHelper<CLong>.TryCreate<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF), out result));
                    Assert.Equal(unchecked((CLong)0x7FFFFFFFFFFFFFFF), result);

                    Assert.True(NumberHelper<CLong>.TryCreate<nint>(unchecked((nint)0x8000000000000000), out result));
                    Assert.Equal(unchecked((CLong)0x8000000000000000), result);

                    Assert.True(NumberHelper<CLong>.TryCreate<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF), out result));
                    Assert.Equal(unchecked((CLong)0xFFFFFFFFFFFFFFFF), result);
                }
            }
            else
            {
                Assert.True(NumberHelper<CLong>.TryCreate<nint>((nint)0x00000000, out result));
                Assert.Equal((CLong)0x00000000, result);

                Assert.True(NumberHelper<CLong>.TryCreate<nint>((nint)0x00000001, out result));
                Assert.Equal((CLong)0x00000001, result);

                Assert.True(NumberHelper<CLong>.TryCreate<nint>((nint)0x7FFFFFFF, out result));
                Assert.Equal((CLong)0x7FFFFFFF, result);

                Assert.True(NumberHelper<CLong>.TryCreate<nint>(unchecked(unchecked((nint)0x80000000)), out result));
                Assert.Equal(unchecked((CLong)0x80000000), result);

                Assert.True(NumberHelper<CLong>.TryCreate<nint>(unchecked(unchecked((nint)0xFFFFFFFF)), out result));
                Assert.Equal(unchecked((CLong)0xFFFFFFFF), result);
            }
        }

        [Fact]
        public static void TryCreateFromSByteTest()
        {
            CLong result;

            Assert.True(NumberHelper<CLong>.TryCreate<sbyte>(0x00, out result));
            Assert.Equal((CLong)0x00000000, result);

            Assert.True(NumberHelper<CLong>.TryCreate<sbyte>(0x01, out result));
            Assert.Equal((CLong)0x00000001, result);

            Assert.True(NumberHelper<CLong>.TryCreate<sbyte>(0x7F, out result));
            Assert.Equal((CLong)0x0000007F, result);

            Assert.True(NumberHelper<CLong>.TryCreate<sbyte>(unchecked((sbyte)0x80), out result));
            Assert.Equal(unchecked((CLong)(int)0xFFFFFF80), result);

            Assert.True(NumberHelper<CLong>.TryCreate<sbyte>(unchecked((sbyte)0xFF), out result));
            Assert.Equal(unchecked((CLong)(int)0xFFFFFFFF), result);
        }

        [Fact]
        public static void TryCreateFromUInt16Test()
        {
            CLong result;

            Assert.True(NumberHelper<CLong>.TryCreate<ushort>(0x0000, out result));
            Assert.Equal((CLong)0x00000000, result);

            Assert.True(NumberHelper<CLong>.TryCreate<ushort>(0x0001, out result));
            Assert.Equal((CLong)0x00000001, result);

            Assert.True(NumberHelper<CLong>.TryCreate<ushort>(0x7FFF, out result));
            Assert.Equal((CLong)0x00007FFF, result);

            Assert.True(NumberHelper<CLong>.TryCreate<ushort>(0x8000, out result));
            Assert.Equal((CLong)0x00008000, result);

            Assert.True(NumberHelper<CLong>.TryCreate<ushort>(0xFFFF, out result));
            Assert.Equal((CLong)0x0000FFFF, result);
        }

        [Fact]
        public static void TryCreateFromUInt32Test()
        {
            CLong result;

            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.True(NumberHelper<CLong>.TryCreate<uint>(0x00000000, out result));
                Assert.Equal((CLong)0x00000000, result);

                Assert.True(NumberHelper<CLong>.TryCreate<uint>(0x00000001, out result));
                Assert.Equal((CLong)0x00000001, result);

                Assert.True(NumberHelper<CLong>.TryCreate<uint>(0x7FFFFFFF, out result));
                Assert.Equal((CLong)0x7FFFFFFF, result);

                Assert.True(NumberHelper<CLong>.TryCreate<uint>(0x80000000, out result));
                Assert.Equal(unchecked((CLong)0x0000000080000000), result);

                Assert.True(NumberHelper<CLong>.TryCreate<uint>(0xFFFFFFFF, out result));
                Assert.Equal(unchecked((CLong)0x00000000FFFFFFFF), result);
            }
            else
            {
                Assert.True(NumberHelper<CLong>.TryCreate<uint>(0x00000000, out result));
                Assert.Equal((CLong)0x00000000, result);

                Assert.True(NumberHelper<CLong>.TryCreate<uint>(0x00000001, out result));
                Assert.Equal((CLong)0x00000001, result);

                Assert.True(NumberHelper<CLong>.TryCreate<uint>(0x7FFFFFFF, out result));
                Assert.Equal((CLong)0x7FFFFFFF, result);

                Assert.False(NumberHelper<CLong>.TryCreate<uint>(0x80000000, out result));
                Assert.Equal((CLong)0x00000000, result);

                Assert.False(NumberHelper<CLong>.TryCreate<uint>(0xFFFFFFFF, out result));
                Assert.Equal((CLong)0x00000000, result);
            }
        }

        [Fact]
        public static void TryCreateFromUInt64Test()
        {
            CLong result;

            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.True(NumberHelper<CLong>.TryCreate<ulong>(0x0000000000000000, out result));
                Assert.Equal(unchecked((CLong)0x0000000000000000), result);

                Assert.True(NumberHelper<CLong>.TryCreate<ulong>(0x0000000000000001, out result));
                Assert.Equal(unchecked((CLong)0x00000000000000001), result);

                Assert.True(NumberHelper<CLong>.TryCreate<ulong>(0x7FFFFFFFFFFFFFFF, out result));
                Assert.Equal(unchecked((CLong)0x7FFFFFFFFFFFFFFF), result);

                Assert.False(NumberHelper<CLong>.TryCreate<ulong>(0x8000000000000000, out result));
                Assert.Equal((CLong)0x0000000000000000, result);

                Assert.False(NumberHelper<CLong>.TryCreate<ulong>(0xFFFFFFFFFFFFFFFF, out result));
                Assert.Equal((CLong)0x0000000000000000, result);
            }
            else
            {
                Assert.True(NumberHelper<CLong>.TryCreate<ulong>(0x0000000000000000, out result));
                Assert.Equal((CLong)0x00000000, result);

                Assert.True(NumberHelper<CLong>.TryCreate<ulong>(0x0000000000000001, out result));
                Assert.Equal((CLong)0x00000001, result);

                Assert.False(NumberHelper<CLong>.TryCreate<ulong>(0x7FFFFFFFFFFFFFFF, out result));
                Assert.Equal((CLong)0x00000000, result);

                Assert.False(NumberHelper<CLong>.TryCreate<ulong>(0x8000000000000000, out result));
                Assert.Equal((CLong)0x00000000, result);

                Assert.False(NumberHelper<CLong>.TryCreate<ulong>(0xFFFFFFFFFFFFFFFF, out result));
                Assert.Equal((CLong)0x00000000, result);
            }
        }

        [Fact]
        public static void TryCreateFromUIntPtrTest()
        {
            CLong result;

            if (Environment.Is64BitProcess)
            {
                Assert.True(NumberHelper<CLong>.TryCreate<nuint>(unchecked((nuint)0x0000000000000000), out result));
                Assert.Equal(unchecked((CLong)0x00000000), result);

                Assert.True(NumberHelper<CLong>.TryCreate<nuint>(unchecked((nuint)0x0000000000000001), out result));
                Assert.Equal(unchecked((CLong)0x00000001), result);

                if (OperatingSystem.IsWindows())
                {
                    Assert.False(NumberHelper<CLong>.TryCreate<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF), out result));
                    Assert.Equal(unchecked((CLong)0x00000000), result);

                    Assert.False(NumberHelper<CLong>.TryCreate<nuint>(unchecked((nuint)0x8000000000000000), out result));
                    Assert.Equal((CLong)0x00000000, result);

                    Assert.False(NumberHelper<CLong>.TryCreate<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF), out result));
                    Assert.Equal((CLong)0x00000000, result);
                }
                else
                {
                    Assert.True(NumberHelper<CLong>.TryCreate<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF), out result));
                    Assert.Equal(unchecked((CLong)0x7FFFFFFFFFFFFFFF), result);

                    Assert.False(NumberHelper<CLong>.TryCreate<nuint>(unchecked((nuint)0x8000000000000000), out result));
                    Assert.Equal((CLong)0x0000000000000000, result);

                    Assert.False(NumberHelper<CLong>.TryCreate<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF), out result));
                    Assert.Equal((CLong)0x0000000000000000, result);
                }
            }
            else
            {
                Assert.True(NumberHelper<CLong>.TryCreate<nuint>((nuint)0x00000000, out result));
                Assert.Equal((CLong)0x00000000, result);

                Assert.True(NumberHelper<CLong>.TryCreate<nuint>((nuint)0x00000001, out result));
                Assert.Equal((CLong)0x00000001, result);

                Assert.True(NumberHelper<CLong>.TryCreate<nuint>((nuint)0x7FFFFFFF, out result));
                Assert.Equal((CLong)0x7FFFFFFF, result);

                Assert.False(NumberHelper<CLong>.TryCreate<nuint>(unchecked(unchecked((nuint)0x80000000)), out result));
                Assert.Equal((CLong)0x00000000, result);

                Assert.False(NumberHelper<CLong>.TryCreate<nuint>(unchecked(unchecked((nuint)0xFFFFFFFF)), out result));
                Assert.Equal((CLong)0x00000000, result);
            }
        }

        [Fact]
        public static void GetByteCountTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(8, BinaryIntegerHelper<CLong>.GetByteCount(unchecked((CLong)0x0000000000000000)));
                Assert.Equal(8, BinaryIntegerHelper<CLong>.GetByteCount(unchecked((CLong)0x0000000000000001)));
                Assert.Equal(8, BinaryIntegerHelper<CLong>.GetByteCount(unchecked((CLong)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(8, BinaryIntegerHelper<CLong>.GetByteCount(unchecked((CLong)0x8000000000000000)));
                Assert.Equal(8, BinaryIntegerHelper<CLong>.GetByteCount(unchecked((CLong)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal(4, BinaryIntegerHelper<CLong>.GetByteCount((CLong)0x00000000));
                Assert.Equal(4, BinaryIntegerHelper<CLong>.GetByteCount((CLong)0x00000001));
                Assert.Equal(4, BinaryIntegerHelper<CLong>.GetByteCount((CLong)0x7FFFFFFF));
                Assert.Equal(4, BinaryIntegerHelper<CLong>.GetByteCount(unchecked((CLong)0x80000000)));
                Assert.Equal(4, BinaryIntegerHelper<CLong>.GetByteCount(unchecked((CLong)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void TryWriteLittleEndianTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Span<byte> destination = stackalloc byte[8];
                int bytesWritten = 0;

                Assert.True(BinaryIntegerHelper<CLong>.TryWriteLittleEndian(unchecked((CLong)0x0000000000000000), destination, out bytesWritten));
                Assert.Equal(8, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

                Assert.True(BinaryIntegerHelper<CLong>.TryWriteLittleEndian(unchecked((CLong)0x0000000000000001), destination, out bytesWritten));
                Assert.Equal(8, bytesWritten);
                Assert.Equal(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

                Assert.True(BinaryIntegerHelper<CLong>.TryWriteLittleEndian(unchecked((CLong)0x7FFFFFFFFFFFFFFF), destination, out bytesWritten));
                Assert.Equal(8, bytesWritten);
                Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, destination.ToArray());

                Assert.True(BinaryIntegerHelper<CLong>.TryWriteLittleEndian(unchecked((CLong)0x8000000000000000), destination, out bytesWritten));
                Assert.Equal(8, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, destination.ToArray());

                Assert.True(BinaryIntegerHelper<CLong>.TryWriteLittleEndian(unchecked((CLong)0xFFFFFFFFFFFFFFFF), destination, out bytesWritten));
                Assert.Equal(8, bytesWritten);
                Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, destination.ToArray());

                Assert.False(BinaryIntegerHelper<CLong>.TryWriteLittleEndian(default, Span<byte>.Empty, out bytesWritten));
                Assert.Equal(0, bytesWritten);
                Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, destination.ToArray());
            }
            else
            {
                Span<byte> destination = stackalloc byte[4];
                int bytesWritten = 0;

                Assert.True(BinaryIntegerHelper<CLong>.TryWriteLittleEndian((CLong)0x00000000, destination, out bytesWritten));
                Assert.Equal(4, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

                Assert.True(BinaryIntegerHelper<CLong>.TryWriteLittleEndian((CLong)0x00000001, destination, out bytesWritten));
                Assert.Equal(4, bytesWritten);
                Assert.Equal(new byte[] { 0x01, 0x00, 0x00, 0x00 }, destination.ToArray());

                Assert.True(BinaryIntegerHelper<CLong>.TryWriteLittleEndian((CLong)0x7FFFFFFF, destination, out bytesWritten));
                Assert.Equal(4, bytesWritten);
                Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0x7F }, destination.ToArray());

                Assert.True(BinaryIntegerHelper<CLong>.TryWriteLittleEndian(unchecked((CLong)0x80000000), destination, out bytesWritten));
                Assert.Equal(4, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x80 }, destination.ToArray());

                Assert.True(BinaryIntegerHelper<CLong>.TryWriteLittleEndian(unchecked((CLong)0xFFFFFFFF), destination, out bytesWritten));
                Assert.Equal(4, bytesWritten);
                Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, destination.ToArray());

                Assert.False(BinaryIntegerHelper<CLong>.TryWriteLittleEndian(default, Span<byte>.Empty, out bytesWritten));
                Assert.Equal(0, bytesWritten);
                Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, destination.ToArray());
            }
        }

        [Fact]
        public static void op_LeftShiftTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CLong)0x0000000000000000), ShiftOperatorsHelper<CLong, CLong>.op_LeftShift(unchecked((CLong)0x0000000000000000), 1));
                Assert.Equal(unchecked((CLong)0x0000000000000002), ShiftOperatorsHelper<CLong, CLong>.op_LeftShift(unchecked((CLong)0x0000000000000001), 1));
                Assert.Equal(unchecked((CLong)0xFFFFFFFFFFFFFFFE), ShiftOperatorsHelper<CLong, CLong>.op_LeftShift(unchecked((CLong)0x7FFFFFFFFFFFFFFF), 1));
                Assert.Equal(unchecked((CLong)0x0000000000000000), ShiftOperatorsHelper<CLong, CLong>.op_LeftShift(unchecked((CLong)0x8000000000000000), 1));
                Assert.Equal(unchecked((CLong)0xFFFFFFFFFFFFFFFE), ShiftOperatorsHelper<CLong, CLong>.op_LeftShift(unchecked((CLong)0xFFFFFFFFFFFFFFFF), 1));
            }
            else
            {
                Assert.Equal((CLong)0x00000000, ShiftOperatorsHelper<CLong, CLong>.op_LeftShift((CLong)0x00000000, 1));
                Assert.Equal((CLong)0x00000002, ShiftOperatorsHelper<CLong, CLong>.op_LeftShift((CLong)0x00000001, 1));
                Assert.Equal(unchecked((CLong)0xFFFFFFFE), ShiftOperatorsHelper<CLong, CLong>.op_LeftShift((CLong)0x7FFFFFFF, 1));
                Assert.Equal((CLong)0x00000000, ShiftOperatorsHelper<CLong, CLong>.op_LeftShift(unchecked((CLong)0x80000000), 1));
                Assert.Equal(unchecked((CLong)0xFFFFFFFE), ShiftOperatorsHelper<CLong, CLong>.op_LeftShift(unchecked((CLong)0xFFFFFFFF), 1));
            }
        }

        [Fact]
        public static void op_RightShiftTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CLong)0x0000000000000000), ShiftOperatorsHelper<CLong, CLong>.op_RightShift(unchecked((CLong)0x0000000000000000), 1));
                Assert.Equal(unchecked((CLong)0x0000000000000000), ShiftOperatorsHelper<CLong, CLong>.op_RightShift(unchecked((CLong)0x0000000000000001), 1));
                Assert.Equal(unchecked((CLong)0x3FFFFFFFFFFFFFFF), ShiftOperatorsHelper<CLong, CLong>.op_RightShift(unchecked((CLong)0x7FFFFFFFFFFFFFFF), 1));
                Assert.Equal(unchecked((CLong)0xC000000000000000), ShiftOperatorsHelper<CLong, CLong>.op_RightShift(unchecked((CLong)0x8000000000000000), 1));
                Assert.Equal(unchecked((CLong)0xFFFFFFFFFFFFFFFF), ShiftOperatorsHelper<CLong, CLong>.op_RightShift(unchecked((CLong)0xFFFFFFFFFFFFFFFF), 1));
            }
            else
            {
                Assert.Equal((CLong)0x00000000, ShiftOperatorsHelper<CLong, CLong>.op_RightShift((CLong)0x00000000, 1));
                Assert.Equal((CLong)0x00000000, ShiftOperatorsHelper<CLong, CLong>.op_RightShift((CLong)0x00000001, 1));
                Assert.Equal((CLong)0x3FFFFFFF, ShiftOperatorsHelper<CLong, CLong>.op_RightShift((CLong)0x7FFFFFFF, 1));
                Assert.Equal(unchecked((CLong)0xC0000000), ShiftOperatorsHelper<CLong, CLong>.op_RightShift(unchecked((CLong)0x80000000), 1));
                Assert.Equal(unchecked((CLong)0xFFFFFFFF), ShiftOperatorsHelper<CLong, CLong>.op_RightShift(unchecked((CLong)0xFFFFFFFF), 1));
            }
        }

        [Fact]
        public static void op_UnsignedRightShiftTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CLong)0x0000000000000000), ShiftOperatorsHelper<CLong, CLong>.op_UnsignedRightShift(unchecked((CLong)0x0000000000000000), 1));
                Assert.Equal(unchecked((CLong)0x0000000000000000), ShiftOperatorsHelper<CLong, CLong>.op_UnsignedRightShift(unchecked((CLong)0x0000000000000001), 1));
                Assert.Equal(unchecked((CLong)0x3FFFFFFFFFFFFFFF), ShiftOperatorsHelper<CLong, CLong>.op_UnsignedRightShift(unchecked((CLong)0x7FFFFFFFFFFFFFFF), 1));
                Assert.Equal(unchecked((CLong)0x4000000000000000), ShiftOperatorsHelper<CLong, CLong>.op_UnsignedRightShift(unchecked((CLong)0x8000000000000000), 1));
                Assert.Equal(unchecked((CLong)0x7FFFFFFFFFFFFFFF), ShiftOperatorsHelper<CLong, CLong>.op_UnsignedRightShift(unchecked((CLong)0xFFFFFFFFFFFFFFFF), 1));
            }
            else
            {
                Assert.Equal((CLong)0x00000000, ShiftOperatorsHelper<CLong, CLong>.op_UnsignedRightShift((CLong)0x00000000, 1));
                Assert.Equal((CLong)0x00000000, ShiftOperatorsHelper<CLong, CLong>.op_UnsignedRightShift((CLong)0x00000001, 1));
                Assert.Equal((CLong)0x3FFFFFFF, ShiftOperatorsHelper<CLong, CLong>.op_UnsignedRightShift((CLong)0x7FFFFFFF, 1));
                Assert.Equal((CLong)0x40000000, ShiftOperatorsHelper<CLong, CLong>.op_UnsignedRightShift(unchecked((CLong)0x80000000), 1));
                Assert.Equal((CLong)0x7FFFFFFF, ShiftOperatorsHelper<CLong, CLong>.op_UnsignedRightShift(unchecked((CLong)0xFFFFFFFF), 1));
            }
        }

        [Fact]
        public static void op_SubtractionTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CLong)0xFFFFFFFFFFFFFFFF), SubtractionOperatorsHelper<CLong, CLong, CLong>.op_Subtraction(unchecked((CLong)0x0000000000000000), (CLong)1));
                Assert.Equal(unchecked((CLong)0x0000000000000000), SubtractionOperatorsHelper<CLong, CLong, CLong>.op_Subtraction(unchecked((CLong)0x0000000000000001), (CLong)1));
                Assert.Equal(unchecked((CLong)0x7FFFFFFFFFFFFFFE), SubtractionOperatorsHelper<CLong, CLong, CLong>.op_Subtraction(unchecked((CLong)0x7FFFFFFFFFFFFFFF), (CLong)1));
                Assert.Equal(unchecked((CLong)0x7FFFFFFFFFFFFFFF), SubtractionOperatorsHelper<CLong, CLong, CLong>.op_Subtraction(unchecked((CLong)0x8000000000000000), (CLong)1));
                Assert.Equal(unchecked((CLong)0xFFFFFFFFFFFFFFFE), SubtractionOperatorsHelper<CLong, CLong, CLong>.op_Subtraction(unchecked((CLong)0xFFFFFFFFFFFFFFFF), (CLong)1));
            }
            else
            {
                Assert.Equal(unchecked((CLong)0xFFFFFFFF), SubtractionOperatorsHelper<CLong, CLong, CLong>.op_Subtraction((CLong)0x00000000, (CLong)1));
                Assert.Equal((CLong)0x00000000, SubtractionOperatorsHelper<CLong, CLong, CLong>.op_Subtraction((CLong)0x00000001, (CLong)1));
                Assert.Equal((CLong)0x7FFFFFFE, SubtractionOperatorsHelper<CLong, CLong, CLong>.op_Subtraction((CLong)0x7FFFFFFF, (CLong)1));
                Assert.Equal((CLong)0x7FFFFFFF, SubtractionOperatorsHelper<CLong, CLong, CLong>.op_Subtraction(unchecked((CLong)0x80000000), (CLong)1));
                Assert.Equal(unchecked((CLong)0xFFFFFFFE), SubtractionOperatorsHelper<CLong, CLong, CLong>.op_Subtraction(unchecked((CLong)0xFFFFFFFF), (CLong)1));
            }
        }

        [Fact]
        public static void op_CheckedSubtractionTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CLong)0xFFFFFFFFFFFFFFFF), SubtractionOperatorsHelper<CLong, CLong, CLong>.op_CheckedSubtraction(unchecked((CLong)0x0000000000000000), (CLong)1));
                Assert.Equal(unchecked((CLong)0x0000000000000000), SubtractionOperatorsHelper<CLong, CLong, CLong>.op_CheckedSubtraction(unchecked((CLong)0x0000000000000001), (CLong)1));
                Assert.Equal(unchecked((CLong)0x7FFFFFFFFFFFFFFE), SubtractionOperatorsHelper<CLong, CLong, CLong>.op_CheckedSubtraction(unchecked((CLong)0x7FFFFFFFFFFFFFFF), (CLong)1));
                Assert.Equal(unchecked((CLong)0xFFFFFFFFFFFFFFFE), SubtractionOperatorsHelper<CLong, CLong, CLong>.op_CheckedSubtraction(unchecked((CLong)0xFFFFFFFFFFFFFFFF), (CLong)1));

                Assert.Throws<OverflowException>(() => SubtractionOperatorsHelper<CLong, CLong, CLong>.op_CheckedSubtraction(unchecked((CLong)0x8000000000000000), (CLong)1));
            }
            else
            {
                Assert.Equal(unchecked((CLong)0xFFFFFFFF), SubtractionOperatorsHelper<CLong, CLong, CLong>.op_CheckedSubtraction((CLong)0x00000000, (CLong)1));
                Assert.Equal((CLong)0x00000000, SubtractionOperatorsHelper<CLong, CLong, CLong>.op_CheckedSubtraction((CLong)0x00000001, (CLong)1));
                Assert.Equal((CLong)0x7FFFFFFE, SubtractionOperatorsHelper<CLong, CLong, CLong>.op_CheckedSubtraction((CLong)0x7FFFFFFF, (CLong)1));
                Assert.Equal(unchecked((CLong)0xFFFFFFFE), SubtractionOperatorsHelper<CLong, CLong, CLong>.op_CheckedSubtraction(unchecked((CLong)0xFFFFFFFF), (CLong)1));

                Assert.Throws<OverflowException>(() => SubtractionOperatorsHelper<CLong, CLong, CLong>.op_CheckedSubtraction(unchecked((CLong)0x80000000), (CLong)1));
            }
        }

        [Fact]
        public static void op_UnaryNegationTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CLong)0x0000000000000000), UnaryNegationOperatorsHelper<CLong, CLong>.op_UnaryNegation(unchecked((CLong)0x0000000000000000)));
                Assert.Equal(unchecked((CLong)0xFFFFFFFFFFFFFFFF), UnaryNegationOperatorsHelper<CLong, CLong>.op_UnaryNegation(unchecked((CLong)0x0000000000000001)));
                Assert.Equal(unchecked((CLong)0x8000000000000001), UnaryNegationOperatorsHelper<CLong, CLong>.op_UnaryNegation(unchecked((CLong)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((CLong)0x8000000000000000), UnaryNegationOperatorsHelper<CLong, CLong>.op_UnaryNegation(unchecked((CLong)0x8000000000000000)));
                Assert.Equal(unchecked((CLong)0x0000000000000001), UnaryNegationOperatorsHelper<CLong, CLong>.op_UnaryNegation(unchecked((CLong)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((CLong)0x00000000, UnaryNegationOperatorsHelper<CLong, CLong>.op_UnaryNegation((CLong)0x00000000));
                Assert.Equal(unchecked((CLong)0xFFFFFFFF), UnaryNegationOperatorsHelper<CLong, CLong>.op_UnaryNegation((CLong)0x00000001));
                Assert.Equal(unchecked((CLong)0x80000001), UnaryNegationOperatorsHelper<CLong, CLong>.op_UnaryNegation((CLong)0x7FFFFFFF));
                Assert.Equal(unchecked((CLong)0x80000000), UnaryNegationOperatorsHelper<CLong, CLong>.op_UnaryNegation(unchecked((CLong)0x80000000)));
                Assert.Equal((CLong)0x00000001, UnaryNegationOperatorsHelper<CLong, CLong>.op_UnaryNegation(unchecked((CLong)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void op_CheckedUnaryNegationTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CLong)0x0000000000000000), UnaryNegationOperatorsHelper<CLong, CLong>.op_CheckedUnaryNegation(unchecked((CLong)0x0000000000000000)));
                Assert.Equal(unchecked((CLong)0xFFFFFFFFFFFFFFFF), UnaryNegationOperatorsHelper<CLong, CLong>.op_CheckedUnaryNegation(unchecked((CLong)0x0000000000000001)));
                Assert.Equal(unchecked((CLong)0x8000000000000001), UnaryNegationOperatorsHelper<CLong, CLong>.op_CheckedUnaryNegation(unchecked((CLong)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((CLong)0x0000000000000001), UnaryNegationOperatorsHelper<CLong, CLong>.op_CheckedUnaryNegation(unchecked((CLong)0xFFFFFFFFFFFFFFFF)));

                Assert.Throws<OverflowException>(() => UnaryNegationOperatorsHelper<CLong, CLong>.op_CheckedUnaryNegation(unchecked((CLong)0x8000000000000000)));
            }
            else
            {
                Assert.Equal((CLong)0x00000000, UnaryNegationOperatorsHelper<CLong, CLong>.op_CheckedUnaryNegation((CLong)0x00000000));
                Assert.Equal(unchecked((CLong)0xFFFFFFFF), UnaryNegationOperatorsHelper<CLong, CLong>.op_CheckedUnaryNegation((CLong)0x00000001));
                Assert.Equal(unchecked((CLong)0x80000001), UnaryNegationOperatorsHelper<CLong, CLong>.op_CheckedUnaryNegation((CLong)0x7FFFFFFF));
                Assert.Equal((CLong)0x00000001, UnaryNegationOperatorsHelper<CLong, CLong>.op_CheckedUnaryNegation(unchecked((CLong)0xFFFFFFFF)));

                Assert.Throws<OverflowException>(() => UnaryNegationOperatorsHelper<CLong, CLong>.op_CheckedUnaryNegation(unchecked((CLong)0x80000000)));
            }
        }

        [Fact]
        public static void op_UnaryPlusTest()
        {
            if (!OperatingSystem.IsWindows() && Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((CLong)0x0000000000000000), UnaryPlusOperatorsHelper<CLong, CLong>.op_UnaryPlus(unchecked((CLong)0x0000000000000000)));
                Assert.Equal(unchecked((CLong)0x0000000000000001), UnaryPlusOperatorsHelper<CLong, CLong>.op_UnaryPlus(unchecked((CLong)0x0000000000000001)));
                Assert.Equal(unchecked((CLong)0x7FFFFFFFFFFFFFFF), UnaryPlusOperatorsHelper<CLong, CLong>.op_UnaryPlus(unchecked((CLong)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((CLong)0x8000000000000000), UnaryPlusOperatorsHelper<CLong, CLong>.op_UnaryPlus(unchecked((CLong)0x8000000000000000)));
                Assert.Equal(unchecked((CLong)0xFFFFFFFFFFFFFFFF), UnaryPlusOperatorsHelper<CLong, CLong>.op_UnaryPlus(unchecked((CLong)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((CLong)0x00000000, UnaryPlusOperatorsHelper<CLong, CLong>.op_UnaryPlus((CLong)0x00000000));
                Assert.Equal((CLong)0x00000001, UnaryPlusOperatorsHelper<CLong, CLong>.op_UnaryPlus((CLong)0x00000001));
                Assert.Equal((CLong)0x7FFFFFFF, UnaryPlusOperatorsHelper<CLong, CLong>.op_UnaryPlus((CLong)0x7FFFFFFF));
                Assert.Equal(unchecked((CLong)0x80000000), UnaryPlusOperatorsHelper<CLong, CLong>.op_UnaryPlus(unchecked((CLong)0x80000000)));
                Assert.Equal(unchecked((CLong)0xFFFFFFFF), UnaryPlusOperatorsHelper<CLong, CLong>.op_UnaryPlus(unchecked((CLong)0xFFFFFFFF)));
            }
        }
    }
}
