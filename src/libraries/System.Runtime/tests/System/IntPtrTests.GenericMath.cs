// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Runtime.Versioning;
using Xunit;

namespace System.Tests
{
    public class IntPtrTests_GenericMath
    {
        [Fact]
        public static void AdditiveIdentityTest()
        {
            Assert.Equal((nint)0x00000000, AdditiveIdentityHelper<nint, nint>.AdditiveIdentity);
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
        public static void MultiplicativeIdentityTest()
        {
            Assert.Equal((nint)0x00000001, MultiplicativeIdentityHelper<nint, nint>.MultiplicativeIdentity);
        }

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

        [Fact]
        public static void OneTest()
        {
            Assert.Equal((nint)0x00000001, NumberBaseHelper<nint>.One);
        }

        [Fact]
        public static void ZeroTest()
        {
            Assert.Equal((nint)0x00000000, NumberBaseHelper<nint>.Zero);
        }

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

        [Fact]
        public static void AbsTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0x0000000000000000), NumberHelper<nint>.Abs(unchecked((nint)0x0000000000000000)));
                Assert.Equal(unchecked((nint)0x0000000000000001), NumberHelper<nint>.Abs(unchecked((nint)0x0000000000000001)));
                Assert.Equal(unchecked((nint)0x7FFFFFFFFFFFFFFF), NumberHelper<nint>.Abs(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Throws<OverflowException>(() => NumberHelper<nint>.Abs(unchecked((nint)0x8000000000000000)));
                Assert.Equal(unchecked((nint)0x0000000000000001), NumberHelper<nint>.Abs(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((nint)0x00000000, NumberHelper<nint>.Abs((nint)0x00000000));
                Assert.Equal((nint)0x00000001, NumberHelper<nint>.Abs((nint)0x00000001));
                Assert.Equal((nint)0x7FFFFFFF, NumberHelper<nint>.Abs((nint)0x7FFFFFFF));
                Assert.Throws<OverflowException>(() => NumberHelper<nint>.Abs(unchecked((nint)0x80000000)));
                Assert.Equal((nint)0x00000001, NumberHelper<nint>.Abs(unchecked((nint)0xFFFFFFFF)));
            }
        }

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
        public static void CreateCheckedFromByteTest()
        {
            Assert.Equal((nint)0x00000000, NumberHelper<nint>.CreateChecked<byte>(0x00));
            Assert.Equal((nint)0x00000001, NumberHelper<nint>.CreateChecked<byte>(0x01));
            Assert.Equal((nint)0x0000007F, NumberHelper<nint>.CreateChecked<byte>(0x7F));
            Assert.Equal((nint)0x00000080, NumberHelper<nint>.CreateChecked<byte>(0x80));
            Assert.Equal((nint)0x000000FF, NumberHelper<nint>.CreateChecked<byte>(0xFF));
        }

        [Fact]
        public static void CreateCheckedFromCharTest()
        {
            Assert.Equal((nint)0x00000000, NumberHelper<nint>.CreateChecked<char>((char)0x0000));
            Assert.Equal((nint)0x00000001, NumberHelper<nint>.CreateChecked<char>((char)0x0001));
            Assert.Equal((nint)0x00007FFF, NumberHelper<nint>.CreateChecked<char>((char)0x7FFF));
            Assert.Equal((nint)0x00008000, NumberHelper<nint>.CreateChecked<char>((char)0x8000));
            Assert.Equal((nint)0x0000FFFF, NumberHelper<nint>.CreateChecked<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateCheckedFromInt16Test()
        {
            Assert.Equal((nint)0x00000000, NumberHelper<nint>.CreateChecked<short>(0x0000));
            Assert.Equal((nint)0x00000001, NumberHelper<nint>.CreateChecked<short>(0x0001));
            Assert.Equal((nint)0x00007FFF, NumberHelper<nint>.CreateChecked<short>(0x7FFF));
            Assert.Equal(unchecked((nint)(int)0xFFFF8000), NumberHelper<nint>.CreateChecked<short>(unchecked((short)0x8000)));
            Assert.Equal(unchecked((nint)(int)0xFFFFFFFF), NumberHelper<nint>.CreateChecked<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromInt32Test()
        {
            Assert.Equal((nint)0x00000000, NumberHelper<nint>.CreateChecked<int>(0x00000000));
            Assert.Equal((nint)0x00000001, NumberHelper<nint>.CreateChecked<int>(0x00000001));
            Assert.Equal((nint)0x7FFFFFFF, NumberHelper<nint>.CreateChecked<int>(0x7FFFFFFF));
            Assert.Equal(unchecked((nint)(int)0x80000000), NumberHelper<nint>.CreateChecked<int>(unchecked((int)0x80000000)));
            Assert.Equal(unchecked((nint)(int)0xFFFFFFFF), NumberHelper<nint>.CreateChecked<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromInt64Test()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0x0000000000000000), NumberHelper<nint>.CreateChecked<long>(0x0000000000000000));
                Assert.Equal(unchecked((nint)0x0000000000000001), NumberHelper<nint>.CreateChecked<long>(0x0000000000000001));
                Assert.Equal(unchecked((nint)0x7FFFFFFFFFFFFFFF), NumberHelper<nint>.CreateChecked<long>(0x7FFFFFFFFFFFFFFF));
                Assert.Equal(unchecked((nint)0x8000000000000000), NumberHelper<nint>.CreateChecked<long>(unchecked((long)0x8000000000000000)));
                Assert.Equal(unchecked((nint)0xFFFFFFFFFFFFFFFF), NumberHelper<nint>.CreateChecked<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((nint)0x00000000, NumberHelper<nint>.CreateChecked<long>(0x0000000000000000));
                Assert.Equal((nint)0x00000001, NumberHelper<nint>.CreateChecked<long>(0x0000000000000001));
                Assert.Throws<OverflowException>(() => NumberHelper<nint>.CreateChecked<long>(0x7FFFFFFFFFFFFFFF));
                Assert.Throws<OverflowException>(() => NumberHelper<nint>.CreateChecked<long>(unchecked((long)0x8000000000000000)));
                Assert.Equal(unchecked((nint)0xFFFFFFFF), NumberHelper<nint>.CreateChecked<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateCheckedFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0x0000000000000000), NumberHelper<nint>.CreateChecked<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal(unchecked((nint)0x0000000000000001), NumberHelper<nint>.CreateChecked<nint>(unchecked((nint)0x0000000000000001)));
                Assert.Equal(unchecked((nint)0x7FFFFFFFFFFFFFFF), NumberHelper<nint>.CreateChecked<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((nint)0x8000000000000000), NumberHelper<nint>.CreateChecked<nint>(unchecked((nint)0x8000000000000000)));
                Assert.Equal(unchecked((nint)0xFFFFFFFFFFFFFFFF), NumberHelper<nint>.CreateChecked<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((nint)0x00000000, NumberHelper<nint>.CreateChecked<nint>((nint)0x00000000));
                Assert.Equal((nint)0x00000001, NumberHelper<nint>.CreateChecked<nint>((nint)0x00000001));
                Assert.Equal((nint)0x7FFFFFFF, NumberHelper<nint>.CreateChecked<nint>((nint)0x7FFFFFFF));
                Assert.Equal(unchecked((nint)0x80000000), NumberHelper<nint>.CreateChecked<nint>(unchecked(unchecked((nint)0x80000000))));
                Assert.Equal(unchecked((nint)0xFFFFFFFF), NumberHelper<nint>.CreateChecked<nint>(unchecked(unchecked((nint)0xFFFFFFFF))));
            }
        }

        [Fact]
        public static void CreateCheckedFromSByteTest()
        {
            Assert.Equal((nint)0x00000000, NumberHelper<nint>.CreateChecked<sbyte>(0x00));
            Assert.Equal((nint)0x00000001, NumberHelper<nint>.CreateChecked<sbyte>(0x01));
            Assert.Equal((nint)0x0000007F, NumberHelper<nint>.CreateChecked<sbyte>(0x7F));
            Assert.Equal(unchecked((nint)(int)0xFFFFFF80), NumberHelper<nint>.CreateChecked<sbyte>(unchecked((sbyte)0x80)));
            Assert.Equal(unchecked((nint)(int)0xFFFFFFFF), NumberHelper<nint>.CreateChecked<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateCheckedFromUInt16Test()
        {
            Assert.Equal((nint)0x00000000, NumberHelper<nint>.CreateChecked<ushort>(0x0000));
            Assert.Equal((nint)0x00000001, NumberHelper<nint>.CreateChecked<ushort>(0x0001));
            Assert.Equal((nint)0x00007FFF, NumberHelper<nint>.CreateChecked<ushort>(0x7FFF));
            Assert.Equal((nint)0x00008000, NumberHelper<nint>.CreateChecked<ushort>(0x8000));
            Assert.Equal((nint)0x0000FFFF, NumberHelper<nint>.CreateChecked<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUInt32Test()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0x0000000000000000), NumberHelper<nint>.CreateChecked<uint>(0x00000000));
                Assert.Equal(unchecked((nint)0x0000000000000001), NumberHelper<nint>.CreateChecked<uint>(0x00000001));
                Assert.Equal(unchecked((nint)0x000000007FFFFFFF), NumberHelper<nint>.CreateChecked<uint>(0x7FFFFFFF));
                Assert.Equal(unchecked((nint)0x0000000080000000), NumberHelper<nint>.CreateChecked<uint>(0x80000000));
                Assert.Equal(unchecked((nint)0x00000000FFFFFFFF), NumberHelper<nint>.CreateChecked<uint>(0xFFFFFFFF));
            }
            else
            {
                Assert.Equal((nint)0x00000000, NumberHelper<nint>.CreateChecked<uint>(0x00000000));
                Assert.Equal((nint)0x00000001, NumberHelper<nint>.CreateChecked<uint>(0x00000001));
                Assert.Equal((nint)0x7FFFFFFF, NumberHelper<nint>.CreateChecked<uint>(0x7FFFFFFF));
                Assert.Throws<OverflowException>(() => NumberHelper<nint>.CreateChecked<uint>(0x80000000));
                Assert.Throws<OverflowException>(() => NumberHelper<nint>.CreateChecked<uint>(0xFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateCheckedFromUInt64Test()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0x0000000000000000), NumberHelper<nint>.CreateChecked<ulong>(0x0000000000000000));
                Assert.Equal(unchecked((nint)0x0000000000000001), NumberHelper<nint>.CreateChecked<ulong>(0x0000000000000001));
                Assert.Equal(unchecked((nint)0x7FFFFFFFFFFFFFFF), NumberHelper<nint>.CreateChecked<ulong>(0x7FFFFFFFFFFFFFFF));
                Assert.Throws<OverflowException>(() => NumberHelper<nint>.CreateChecked<ulong>(0x8000000000000000));
                Assert.Throws<OverflowException>(() => NumberHelper<nint>.CreateChecked<ulong>(0xFFFFFFFFFFFFFFFF));
            }
            else
            {
                Assert.Equal((nint)0x00000000, NumberHelper<nint>.CreateChecked<ulong>(0x0000000000000000));
                Assert.Equal((nint)0x00000001, NumberHelper<nint>.CreateChecked<ulong>(0x0000000000000001));
                Assert.Throws<OverflowException>(() => NumberHelper<nint>.CreateChecked<ulong>(0x7FFFFFFFFFFFFFFF));
                Assert.Throws<OverflowException>(() => NumberHelper<nint>.CreateChecked<ulong>(0x8000000000000000));
                Assert.Throws<OverflowException>(() => NumberHelper<nint>.CreateChecked<ulong>(0xFFFFFFFFFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateCheckedFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0x0000000000000000), NumberHelper<nint>.CreateChecked<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal(unchecked((nint)0x0000000000000001), NumberHelper<nint>.CreateChecked<nuint>(unchecked((nuint)0x0000000000000001)));
                Assert.Equal(unchecked((nint)0x7FFFFFFFFFFFFFFF), NumberHelper<nint>.CreateChecked<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Throws<OverflowException>(() => NumberHelper<nint>.CreateChecked<nuint>(unchecked((nuint)0x8000000000000000)));
                Assert.Throws<OverflowException>(() => NumberHelper<nint>.CreateChecked<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((nint)0x00000000, NumberHelper<nint>.CreateChecked<nuint>((nuint)0x00000000));
                Assert.Equal((nint)0x00000001, NumberHelper<nint>.CreateChecked<nuint>((nuint)0x00000001));
                Assert.Equal((nint)0x7FFFFFFF, NumberHelper<nint>.CreateChecked<nuint>((nuint)0x7FFFFFFF));
                Assert.Throws<OverflowException>(() => NumberHelper<nint>.CreateChecked<nuint>(unchecked((nuint)0x80000000)));
                Assert.Throws<OverflowException>(() => NumberHelper<nint>.CreateChecked<nuint>(unchecked((nuint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateSaturatingFromByteTest()
        {
            Assert.Equal((nint)0x00000000, NumberHelper<nint>.CreateSaturating<byte>(0x00));
            Assert.Equal((nint)0x00000001, NumberHelper<nint>.CreateSaturating<byte>(0x01));
            Assert.Equal((nint)0x0000007F, NumberHelper<nint>.CreateSaturating<byte>(0x7F));
            Assert.Equal((nint)0x00000080, NumberHelper<nint>.CreateSaturating<byte>(0x80));
            Assert.Equal((nint)0x000000FF, NumberHelper<nint>.CreateSaturating<byte>(0xFF));
        }

        [Fact]
        public static void CreateSaturatingFromCharTest()
        {
            Assert.Equal((nint)0x00000000, NumberHelper<nint>.CreateSaturating<char>((char)0x0000));
            Assert.Equal((nint)0x00000001, NumberHelper<nint>.CreateSaturating<char>((char)0x0001));
            Assert.Equal((nint)0x00007FFF, NumberHelper<nint>.CreateSaturating<char>((char)0x7FFF));
            Assert.Equal((nint)0x00008000, NumberHelper<nint>.CreateSaturating<char>((char)0x8000));
            Assert.Equal((nint)0x0000FFFF, NumberHelper<nint>.CreateSaturating<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromInt16Test()
        {
            Assert.Equal((nint)0x00000000, NumberHelper<nint>.CreateSaturating<short>(0x0000));
            Assert.Equal((nint)0x00000001, NumberHelper<nint>.CreateSaturating<short>(0x0001));
            Assert.Equal((nint)0x00007FFF, NumberHelper<nint>.CreateSaturating<short>(0x7FFF));
            Assert.Equal(unchecked((nint)(int)0xFFFF8000), NumberHelper<nint>.CreateSaturating<short>(unchecked((short)0x8000)));
            Assert.Equal(unchecked((nint)(int)0xFFFFFFFF), NumberHelper<nint>.CreateSaturating<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromInt32Test()
        {
            Assert.Equal((nint)0x00000000, NumberHelper<nint>.CreateSaturating<int>(0x00000000));
            Assert.Equal((nint)0x00000001, NumberHelper<nint>.CreateSaturating<int>(0x00000001));
            Assert.Equal((nint)0x7FFFFFFF, NumberHelper<nint>.CreateSaturating<int>(0x7FFFFFFF));
            Assert.Equal(unchecked((nint)(int)0x80000000), NumberHelper<nint>.CreateSaturating<int>(unchecked((int)0x80000000)));
            Assert.Equal(unchecked((nint)(int)0xFFFFFFFF), NumberHelper<nint>.CreateSaturating<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromInt64Test()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0x0000000000000000), NumberHelper<nint>.CreateSaturating<long>(0x0000000000000000));
                Assert.Equal(unchecked((nint)0x0000000000000001), NumberHelper<nint>.CreateSaturating<long>(0x0000000000000001));
                Assert.Equal(unchecked((nint)0x7FFFFFFFFFFFFFFF), NumberHelper<nint>.CreateSaturating<long>(0x7FFFFFFFFFFFFFFF));
                Assert.Equal(unchecked((nint)0x8000000000000000), NumberHelper<nint>.CreateSaturating<long>(unchecked((long)0x8000000000000000)));
                Assert.Equal(unchecked((nint)0xFFFFFFFFFFFFFFFF), NumberHelper<nint>.CreateSaturating<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((nint)0x00000000, NumberHelper<nint>.CreateSaturating<long>(0x0000000000000000));
                Assert.Equal((nint)0x00000001, NumberHelper<nint>.CreateSaturating<long>(0x0000000000000001));
                Assert.Equal(unchecked((nint)0x7FFFFFFF), NumberHelper<nint>.CreateSaturating<long>(0x7FFFFFFFFFFFFFFF));
                Assert.Equal(unchecked((nint)0x80000000), NumberHelper<nint>.CreateSaturating<long>(unchecked((long)0x8000000000000000)));
                Assert.Equal(unchecked((nint)0xFFFFFFFF), NumberHelper<nint>.CreateSaturating<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateSaturatingFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0x0000000000000000), NumberHelper<nint>.CreateSaturating<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal(unchecked((nint)0x0000000000000001), NumberHelper<nint>.CreateSaturating<nint>(unchecked((nint)0x0000000000000001)));
                Assert.Equal(unchecked((nint)0x7FFFFFFFFFFFFFFF), NumberHelper<nint>.CreateSaturating<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((nint)0x8000000000000000), NumberHelper<nint>.CreateSaturating<nint>(unchecked((nint)0x8000000000000000)));
                Assert.Equal(unchecked((nint)0xFFFFFFFFFFFFFFFF), NumberHelper<nint>.CreateSaturating<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((nint)0x00000000, NumberHelper<nint>.CreateSaturating<nint>((nint)0x00000000));
                Assert.Equal((nint)0x00000001, NumberHelper<nint>.CreateSaturating<nint>((nint)0x00000001));
                Assert.Equal((nint)0x7FFFFFFF, NumberHelper<nint>.CreateSaturating<nint>((nint)0x7FFFFFFF));
                Assert.Equal(unchecked((nint)0x80000000), NumberHelper<nint>.CreateSaturating<nint>(unchecked(unchecked((nint)0x80000000))));
                Assert.Equal(unchecked((nint)0xFFFFFFFF), NumberHelper<nint>.CreateSaturating<nint>(unchecked(unchecked((nint)0xFFFFFFFF))));
            }
        }

        [Fact]
        public static void CreateSaturatingFromSByteTest()
        {
            Assert.Equal((nint)0x00000000, NumberHelper<nint>.CreateSaturating<sbyte>(0x00));
            Assert.Equal((nint)0x00000001, NumberHelper<nint>.CreateSaturating<sbyte>(0x01));
            Assert.Equal((nint)0x0000007F, NumberHelper<nint>.CreateSaturating<sbyte>(0x7F));
            Assert.Equal(unchecked((nint)(int)0xFFFFFF80), NumberHelper<nint>.CreateSaturating<sbyte>(unchecked((sbyte)0x80)));
            Assert.Equal(unchecked((nint)(int)0xFFFFFFFF), NumberHelper<nint>.CreateSaturating<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateSaturatingFromUInt16Test()
        {
            Assert.Equal((nint)0x00000000, NumberHelper<nint>.CreateSaturating<ushort>(0x0000));
            Assert.Equal((nint)0x00000001, NumberHelper<nint>.CreateSaturating<ushort>(0x0001));
            Assert.Equal((nint)0x00007FFF, NumberHelper<nint>.CreateSaturating<ushort>(0x7FFF));
            Assert.Equal((nint)0x00008000, NumberHelper<nint>.CreateSaturating<ushort>(0x8000));
            Assert.Equal((nint)0x0000FFFF, NumberHelper<nint>.CreateSaturating<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUInt32Test()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0x0000000000000000), NumberHelper<nint>.CreateSaturating<uint>(0x00000000));
                Assert.Equal(unchecked((nint)0x0000000000000001), NumberHelper<nint>.CreateSaturating<uint>(0x00000001));
                Assert.Equal(unchecked((nint)0x000000007FFFFFFF), NumberHelper<nint>.CreateSaturating<uint>(0x7FFFFFFF));
                Assert.Equal(unchecked((nint)0x0000000080000000), NumberHelper<nint>.CreateSaturating<uint>(0x80000000));
                Assert.Equal(unchecked((nint)0x00000000FFFFFFFF), NumberHelper<nint>.CreateSaturating<uint>(0xFFFFFFFF));
            }
            else
            {
                Assert.Equal((nint)0x00000000, NumberHelper<nint>.CreateSaturating<uint>(0x00000000));
                Assert.Equal((nint)0x00000001, NumberHelper<nint>.CreateSaturating<uint>(0x00000001));
                Assert.Equal((nint)0x7FFFFFFF, NumberHelper<nint>.CreateSaturating<uint>(0x7FFFFFFF));
                Assert.Equal((nint)0x7FFFFFFF, NumberHelper<nint>.CreateSaturating<uint>(0x80000000));
                Assert.Equal((nint)0x7FFFFFFF, NumberHelper<nint>.CreateSaturating<uint>(0xFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateSaturatingFromUInt64Test()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0x0000000000000000), NumberHelper<nint>.CreateSaturating<ulong>(0x0000000000000000));
                Assert.Equal(unchecked((nint)0x0000000000000001), NumberHelper<nint>.CreateSaturating<ulong>(0x0000000000000001));
                Assert.Equal(unchecked((nint)0x7FFFFFFFFFFFFFFF), NumberHelper<nint>.CreateSaturating<ulong>(0x7FFFFFFFFFFFFFFF));
                Assert.Equal(unchecked((nint)0x7FFFFFFFFFFFFFFF), NumberHelper<nint>.CreateSaturating<ulong>(0x8000000000000000));
                Assert.Equal(unchecked((nint)0x7FFFFFFFFFFFFFFF), NumberHelper<nint>.CreateSaturating<ulong>(0xFFFFFFFFFFFFFFFF));
            }
            else
            {
                Assert.Equal((nint)0x00000000, NumberHelper<nint>.CreateSaturating<ulong>(0x0000000000000000));
                Assert.Equal((nint)0x00000001, NumberHelper<nint>.CreateSaturating<ulong>(0x0000000000000001));
                Assert.Equal((nint)0x7FFFFFFF, NumberHelper<nint>.CreateSaturating<ulong>(0x7FFFFFFFFFFFFFFF));
                Assert.Equal((nint)0x7FFFFFFF, NumberHelper<nint>.CreateSaturating<ulong>(0x8000000000000000));
                Assert.Equal((nint)0x7FFFFFFF, NumberHelper<nint>.CreateSaturating<ulong>(0xFFFFFFFFFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateSaturatingFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0x0000000000000000), NumberHelper<nint>.CreateSaturating<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal(unchecked((nint)0x0000000000000001), NumberHelper<nint>.CreateSaturating<nuint>(unchecked((nuint)0x0000000000000001)));
                Assert.Equal(unchecked((nint)0x7FFFFFFFFFFFFFFF), NumberHelper<nint>.CreateSaturating<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((nint)0x7FFFFFFFFFFFFFFF), NumberHelper<nint>.CreateSaturating<nuint>(unchecked((nuint)0x8000000000000000)));
                Assert.Equal(unchecked((nint)0x7FFFFFFFFFFFFFFF), NumberHelper<nint>.CreateSaturating<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((nint)0x00000000, NumberHelper<nint>.CreateSaturating<nuint>((nuint)0x00000000));
                Assert.Equal((nint)0x00000001, NumberHelper<nint>.CreateSaturating<nuint>((nuint)0x00000001));
                Assert.Equal((nint)0x7FFFFFFF, NumberHelper<nint>.CreateSaturating<nuint>((nuint)0x7FFFFFFF));
                Assert.Equal((nint)0x7FFFFFFF, NumberHelper<nint>.CreateSaturating<nuint>(unchecked((nuint)0x80000000)));
                Assert.Equal((nint)0x7FFFFFFF, NumberHelper<nint>.CreateSaturating<nuint>(unchecked((nuint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateTruncatingFromByteTest()
        {
            Assert.Equal((nint)0x00000000, NumberHelper<nint>.CreateTruncating<byte>(0x00));
            Assert.Equal((nint)0x00000001, NumberHelper<nint>.CreateTruncating<byte>(0x01));
            Assert.Equal((nint)0x0000007F, NumberHelper<nint>.CreateTruncating<byte>(0x7F));
            Assert.Equal((nint)0x00000080, NumberHelper<nint>.CreateTruncating<byte>(0x80));
            Assert.Equal((nint)0x000000FF, NumberHelper<nint>.CreateTruncating<byte>(0xFF));
        }

        [Fact]
        public static void CreateTruncatingFromCharTest()
        {
            Assert.Equal((nint)0x00000000, NumberHelper<nint>.CreateTruncating<char>((char)0x0000));
            Assert.Equal((nint)0x00000001, NumberHelper<nint>.CreateTruncating<char>((char)0x0001));
            Assert.Equal((nint)0x00007FFF, NumberHelper<nint>.CreateTruncating<char>((char)0x7FFF));
            Assert.Equal((nint)0x00008000, NumberHelper<nint>.CreateTruncating<char>((char)0x8000));
            Assert.Equal((nint)0x0000FFFF, NumberHelper<nint>.CreateTruncating<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromInt16Test()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0x0000000000000000), NumberHelper<nint>.CreateTruncating<short>(0x0000));
                Assert.Equal(unchecked((nint)0x0000000000000001), NumberHelper<nint>.CreateTruncating<short>(0x0001));
                Assert.Equal(unchecked((nint)0x0000000000007FFF), NumberHelper<nint>.CreateTruncating<short>(0x7FFF));
                Assert.Equal(unchecked((nint)0xFFFFFFFFFFFF8000), NumberHelper<nint>.CreateTruncating<short>(unchecked((short)0x8000)));
                Assert.Equal(unchecked((nint)0xFFFFFFFFFFFFFFFF), NumberHelper<nint>.CreateTruncating<short>(unchecked((short)0xFFFF)));
            }
            else
            {
                Assert.Equal((nint)0x00000000, NumberHelper<nint>.CreateTruncating<short>(0x0000));
                Assert.Equal((nint)0x00000001, NumberHelper<nint>.CreateTruncating<short>(0x0001));
                Assert.Equal((nint)0x00007FFF, NumberHelper<nint>.CreateTruncating<short>(0x7FFF));
                Assert.Equal(unchecked((nint)0xFFFF8000), NumberHelper<nint>.CreateTruncating<short>(unchecked((short)0x8000)));
                Assert.Equal(unchecked((nint)0xFFFFFFFF), NumberHelper<nint>.CreateTruncating<short>(unchecked((short)0xFFFF)));
            }
        }

        [Fact]
        public static void CreateTruncatingFromInt32Test()
        {
            Assert.Equal((nint)0x00000000, NumberHelper<nint>.CreateTruncating<int>(0x00000000));
            Assert.Equal((nint)0x00000001, NumberHelper<nint>.CreateTruncating<int>(0x00000001));
            Assert.Equal((nint)0x7FFFFFFF, NumberHelper<nint>.CreateTruncating<int>(0x7FFFFFFF));
            Assert.Equal(unchecked((nint)(int)0x80000000), NumberHelper<nint>.CreateTruncating<int>(unchecked((int)0x80000000)));
            Assert.Equal(unchecked((nint)(int)0xFFFFFFFF), NumberHelper<nint>.CreateTruncating<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateTruncatingFromInt64Test()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0x0000000000000000), NumberHelper<nint>.CreateTruncating<long>(0x0000000000000000));
                Assert.Equal(unchecked((nint)0x0000000000000001), NumberHelper<nint>.CreateTruncating<long>(0x0000000000000001));
                Assert.Equal(unchecked((nint)0x7FFFFFFFFFFFFFFF), NumberHelper<nint>.CreateTruncating<long>(0x7FFFFFFFFFFFFFFF));
                Assert.Equal(unchecked((nint)0x8000000000000000), NumberHelper<nint>.CreateTruncating<long>(unchecked((long)0x8000000000000000)));
                Assert.Equal(unchecked((nint)0xFFFFFFFFFFFFFFFF), NumberHelper<nint>.CreateTruncating<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((nint)0x00000000, NumberHelper<nint>.CreateTruncating<long>(0x0000000000000000));
                Assert.Equal((nint)0x00000001, NumberHelper<nint>.CreateTruncating<long>(0x0000000000000001));
                Assert.Equal(unchecked((nint)0xFFFFFFFF), NumberHelper<nint>.CreateTruncating<long>(0x7FFFFFFFFFFFFFFF));
                Assert.Equal((nint)0x00000000, NumberHelper<nint>.CreateTruncating<long>(unchecked((long)0x8000000000000000)));
                Assert.Equal(unchecked((nint)0xFFFFFFFF), NumberHelper<nint>.CreateTruncating<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateTruncatingFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0x0000000000000000), NumberHelper<nint>.CreateTruncating<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal(unchecked((nint)0x0000000000000001), NumberHelper<nint>.CreateTruncating<nint>(unchecked((nint)0x0000000000000001)));
                Assert.Equal(unchecked((nint)0x7FFFFFFFFFFFFFFF), NumberHelper<nint>.CreateTruncating<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((nint)0x8000000000000000), NumberHelper<nint>.CreateTruncating<nint>(unchecked((nint)0x8000000000000000)));
                Assert.Equal(unchecked((nint)0xFFFFFFFFFFFFFFFF), NumberHelper<nint>.CreateTruncating<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((nint)0x00000000, NumberHelper<nint>.CreateTruncating<nint>((nint)0x00000000));
                Assert.Equal((nint)0x00000001, NumberHelper<nint>.CreateTruncating<nint>((nint)0x00000001));
                Assert.Equal((nint)0x7FFFFFFF, NumberHelper<nint>.CreateTruncating<nint>((nint)0x7FFFFFFF));
                Assert.Equal(unchecked((nint)0x80000000), NumberHelper<nint>.CreateTruncating<nint>(unchecked(unchecked((nint)0x80000000))));
                Assert.Equal(unchecked((nint)0xFFFFFFFF), NumberHelper<nint>.CreateTruncating<nint>(unchecked(unchecked((nint)0xFFFFFFFF))));
            }
        }

        [Fact]
        public static void CreateTruncatingFromSByteTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0x0000000000000000), NumberHelper<nint>.CreateTruncating<sbyte>(0x00));
                Assert.Equal(unchecked((nint)0x0000000000000001), NumberHelper<nint>.CreateTruncating<sbyte>(0x01));
                Assert.Equal(unchecked((nint)0x000000000000007F), NumberHelper<nint>.CreateTruncating<sbyte>(0x7F));
                Assert.Equal(unchecked((nint)0xFFFFFFFFFFFFFF80), NumberHelper<nint>.CreateTruncating<sbyte>(unchecked((sbyte)0x80)));
                Assert.Equal(unchecked((nint)0xFFFFFFFFFFFFFFFF), NumberHelper<nint>.CreateTruncating<sbyte>(unchecked((sbyte)0xFF)));
            }
            else
            {
                Assert.Equal((nint)0x00000000, NumberHelper<nint>.CreateTruncating<sbyte>(0x00));
                Assert.Equal((nint)0x00000001, NumberHelper<nint>.CreateTruncating<sbyte>(0x01));
                Assert.Equal((nint)0x0000007F, NumberHelper<nint>.CreateTruncating<sbyte>(0x7F));
                Assert.Equal(unchecked((nint)0xFFFFFF80), NumberHelper<nint>.CreateTruncating<sbyte>(unchecked((sbyte)0x80)));
                Assert.Equal(unchecked((nint)0xFFFFFFFF), NumberHelper<nint>.CreateTruncating<sbyte>(unchecked((sbyte)0xFF)));
            }
        }

        [Fact]
        public static void CreateTruncatingFromUInt16Test()
        {
            Assert.Equal((nint)0x00000000, NumberHelper<nint>.CreateTruncating<ushort>(0x0000));
            Assert.Equal((nint)0x00000001, NumberHelper<nint>.CreateTruncating<ushort>(0x0001));
            Assert.Equal((nint)0x00007FFF, NumberHelper<nint>.CreateTruncating<ushort>(0x7FFF));
            Assert.Equal((nint)0x00008000, NumberHelper<nint>.CreateTruncating<ushort>(0x8000));
            Assert.Equal((nint)0x0000FFFF, NumberHelper<nint>.CreateTruncating<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt32Test()
        {
            Assert.Equal((nint)0x00000000, NumberHelper<nint>.CreateTruncating<uint>(0x00000000));
            Assert.Equal((nint)0x00000001, NumberHelper<nint>.CreateTruncating<uint>(0x00000001));
            Assert.Equal((nint)0x7FFFFFFF, NumberHelper<nint>.CreateTruncating<uint>(0x7FFFFFFF));
            Assert.Equal(unchecked((nint)0x80000000), NumberHelper<nint>.CreateTruncating<uint>(0x80000000));
            Assert.Equal(unchecked((nint)0xFFFFFFFF), NumberHelper<nint>.CreateTruncating<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt64Test()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0x0000000000000000), NumberHelper<nint>.CreateTruncating<ulong>(0x0000000000000000));
                Assert.Equal(unchecked((nint)0x0000000000000001), NumberHelper<nint>.CreateTruncating<ulong>(0x0000000000000001));
                Assert.Equal(unchecked((nint)0x7FFFFFFFFFFFFFFF), NumberHelper<nint>.CreateTruncating<ulong>(0x7FFFFFFFFFFFFFFF));
                Assert.Equal(unchecked((nint)0x8000000000000000), NumberHelper<nint>.CreateTruncating<ulong>(0x8000000000000000));
                Assert.Equal(unchecked((nint)0xFFFFFFFFFFFFFFFF), NumberHelper<nint>.CreateTruncating<ulong>(0xFFFFFFFFFFFFFFFF));
            }
            else
            {
                Assert.Equal((nint)0x00000000, NumberHelper<nint>.CreateTruncating<ulong>(0x0000000000000000));
                Assert.Equal((nint)0x00000001, NumberHelper<nint>.CreateTruncating<ulong>(0x0000000000000001));
                Assert.Equal(unchecked((nint)0xFFFFFFFF), NumberHelper<nint>.CreateTruncating<ulong>(0x7FFFFFFFFFFFFFFF));
                Assert.Equal((nint)0x00000000, NumberHelper<nint>.CreateTruncating<ulong>(0x8000000000000000));
                Assert.Equal(unchecked((nint)0xFFFFFFFF), NumberHelper<nint>.CreateTruncating<ulong>(0xFFFFFFFFFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateTruncatingFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0x0000000000000000), NumberHelper<nint>.CreateTruncating<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal(unchecked((nint)0x0000000000000001), NumberHelper<nint>.CreateTruncating<nuint>(unchecked((nuint)0x0000000000000001)));
                Assert.Equal(unchecked((nint)0x7FFFFFFFFFFFFFFF), NumberHelper<nint>.CreateTruncating<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((nint)0x8000000000000000), NumberHelper<nint>.CreateTruncating<nuint>(unchecked((nuint)0x8000000000000000)));
                Assert.Equal(unchecked((nint)0xFFFFFFFFFFFFFFFF), NumberHelper<nint>.CreateTruncating<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((nint)0x00000000, NumberHelper<nint>.CreateTruncating<nuint>((nuint)0x00000000));
                Assert.Equal((nint)0x00000001, NumberHelper<nint>.CreateTruncating<nuint>((nuint)0x00000001));
                Assert.Equal((nint)0x7FFFFFFF, NumberHelper<nint>.CreateTruncating<nuint>((nuint)0x7FFFFFFF));
                Assert.Equal(unchecked((nint)0x80000000), NumberHelper<nint>.CreateTruncating<nuint>(unchecked((nuint)0x80000000)));
                Assert.Equal(unchecked((nint)0xFFFFFFFF), NumberHelper<nint>.CreateTruncating<nuint>(unchecked((nuint)0xFFFFFFFF)));
            }
        }

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

        [Fact]
        public static void TryCreateFromByteTest()
        {
            nint result;

            Assert.True(NumberHelper<nint>.TryCreate<byte>(0x00, out result));
            Assert.Equal((nint)0x00000000, result);

            Assert.True(NumberHelper<nint>.TryCreate<byte>(0x01, out result));
            Assert.Equal((nint)0x00000001, result);

            Assert.True(NumberHelper<nint>.TryCreate<byte>(0x7F, out result));
            Assert.Equal((nint)0x0000007F, result);

            Assert.True(NumberHelper<nint>.TryCreate<byte>(0x80, out result));
            Assert.Equal((nint)0x00000080, result);

            Assert.True(NumberHelper<nint>.TryCreate<byte>(0xFF, out result));
            Assert.Equal((nint)0x000000FF, result);
        }

        [Fact]
        public static void TryCreateFromCharTest()
        {
            nint result;

            Assert.True(NumberHelper<nint>.TryCreate<char>((char)0x0000, out result));
            Assert.Equal((nint)0x00000000, result);

            Assert.True(NumberHelper<nint>.TryCreate<char>((char)0x0001, out result));
            Assert.Equal((nint)0x00000001, result);

            Assert.True(NumberHelper<nint>.TryCreate<char>((char)0x7FFF, out result));
            Assert.Equal((nint)0x00007FFF, result);

            Assert.True(NumberHelper<nint>.TryCreate<char>((char)0x8000, out result));
            Assert.Equal((nint)0x00008000, result);

            Assert.True(NumberHelper<nint>.TryCreate<char>((char)0xFFFF, out result));
            Assert.Equal((nint)0x0000FFFF, result);
        }

        [Fact]
        public static void TryCreateFromInt16Test()
        {
            nint result;

            Assert.True(NumberHelper<nint>.TryCreate<short>(0x0000, out result));
            Assert.Equal((nint)0x00000000, result);

            Assert.True(NumberHelper<nint>.TryCreate<short>(0x0001, out result));
            Assert.Equal((nint)0x00000001, result);

            Assert.True(NumberHelper<nint>.TryCreate<short>(0x7FFF, out result));
            Assert.Equal((nint)0x00007FFF, result);

            Assert.True(NumberHelper<nint>.TryCreate<short>(unchecked((short)0x8000), out result));
            Assert.Equal(unchecked((nint)(int)0xFFFF8000), result);

            Assert.True(NumberHelper<nint>.TryCreate<short>(unchecked((short)0xFFFF), out result));
            Assert.Equal(unchecked((nint)(int)0xFFFFFFFF), result);
        }

        [Fact]
        public static void TryCreateFromInt32Test()
        {
            nint result;

            Assert.True(NumberHelper<nint>.TryCreate<int>(0x00000000, out result));
            Assert.Equal((nint)0x00000000, result);

            Assert.True(NumberHelper<nint>.TryCreate<int>(0x00000001, out result));
            Assert.Equal((nint)0x00000001, result);

            Assert.True(NumberHelper<nint>.TryCreate<int>(0x7FFFFFFF, out result));
            Assert.Equal((nint)0x7FFFFFFF, result);

            Assert.True(NumberHelper<nint>.TryCreate<int>(unchecked((int)0x80000000), out result));
            Assert.Equal(unchecked((nint)(int)0x80000000), result);

            Assert.True(NumberHelper<nint>.TryCreate<int>(unchecked((int)0xFFFFFFFF), out result));
            Assert.Equal(unchecked((nint)(int)0xFFFFFFFF), result);
        }

        [Fact]
        public static void TryCreateFromInt64Test()
        {
            nint result;

            if (Environment.Is64BitProcess)
            {
                Assert.True(NumberHelper<nint>.TryCreate<long>(0x0000000000000000, out result));
                Assert.Equal(unchecked((nint)0x0000000000000000), result);

                Assert.True(NumberHelper<nint>.TryCreate<long>(0x0000000000000001, out result));
                Assert.Equal(unchecked((nint)0x0000000000000001), result);

                Assert.True(NumberHelper<nint>.TryCreate<long>(0x7FFFFFFFFFFFFFFF, out result));
                Assert.Equal(unchecked((nint)0x7FFFFFFFFFFFFFFF), result);

                Assert.True(NumberHelper<nint>.TryCreate<long>(unchecked((long)0x8000000000000000), out result));
                Assert.Equal(unchecked((nint)0x8000000000000000), result);

                Assert.True(NumberHelper<nint>.TryCreate<long>(unchecked((long)0xFFFFFFFFFFFFFFFF), out result));
                Assert.Equal(unchecked((nint)0xFFFFFFFFFFFFFFFF), result);
            }
            else
            {
                Assert.True(NumberHelper<nint>.TryCreate<long>(0x0000000000000000, out result));
                Assert.Equal((nint)0x00000000, result);

                Assert.True(NumberHelper<nint>.TryCreate<long>(0x0000000000000001, out result));
                Assert.Equal((nint)0x00000001, result);

                Assert.False(NumberHelper<nint>.TryCreate<long>(0x7FFFFFFFFFFFFFFF, out result));
                Assert.Equal((nint)0x00000000, result);

                Assert.False(NumberHelper<nint>.TryCreate<long>(unchecked((long)0x8000000000000000), out result));
                Assert.Equal((nint)0x00000000, result);

                Assert.True(NumberHelper<nint>.TryCreate<long>(unchecked((long)0xFFFFFFFFFFFFFFFF), out result));
                Assert.Equal(unchecked((nint)0xFFFFFFFF), result);
            }
        }

        [Fact]
        public static void TryCreateFromIntPtrTest()
        {
            nint result;

            if (Environment.Is64BitProcess)
            {
                Assert.True(NumberHelper<nint>.TryCreate<nint>(unchecked((nint)0x0000000000000000), out result));
                Assert.Equal(unchecked((nint)0x0000000000000000), result);

                Assert.True(NumberHelper<nint>.TryCreate<nint>(unchecked((nint)0x0000000000000001), out result));
                Assert.Equal(unchecked((nint)0x0000000000000001), result);

                Assert.True(NumberHelper<nint>.TryCreate<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF), out result));
                Assert.Equal(unchecked((nint)0x7FFFFFFFFFFFFFFF), result);

                Assert.True(NumberHelper<nint>.TryCreate<nint>(unchecked((nint)0x8000000000000000), out result));
                Assert.Equal(unchecked((nint)0x8000000000000000), result);

                Assert.True(NumberHelper<nint>.TryCreate<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF), out result));
                Assert.Equal(unchecked((nint)0xFFFFFFFFFFFFFFFF), result);
            }
            else
            {
                Assert.True(NumberHelper<nint>.TryCreate<nint>((nint)0x00000000, out result));
                Assert.Equal((nint)0x00000000, result);

                Assert.True(NumberHelper<nint>.TryCreate<nint>((nint)0x00000001, out result));
                Assert.Equal((nint)0x00000001, result);

                Assert.True(NumberHelper<nint>.TryCreate<nint>((nint)0x7FFFFFFF, out result));
                Assert.Equal((nint)0x7FFFFFFF, result);

                Assert.True(NumberHelper<nint>.TryCreate<nint>(unchecked(unchecked((nint)0x80000000)), out result));
                Assert.Equal(unchecked((nint)0x80000000), result);

                Assert.True(NumberHelper<nint>.TryCreate<nint>(unchecked(unchecked((nint)0xFFFFFFFF)), out result));
                Assert.Equal(unchecked((nint)0xFFFFFFFF), result);
            }
        }

        [Fact]
        public static void TryCreateFromSByteTest()
        {
            nint result;

            Assert.True(NumberHelper<nint>.TryCreate<sbyte>(0x00, out result));
            Assert.Equal((nint)0x00000000, result);

            Assert.True(NumberHelper<nint>.TryCreate<sbyte>(0x01, out result));
            Assert.Equal((nint)0x00000001, result);

            Assert.True(NumberHelper<nint>.TryCreate<sbyte>(0x7F, out result));
            Assert.Equal((nint)0x0000007F, result);

            Assert.True(NumberHelper<nint>.TryCreate<sbyte>(unchecked((sbyte)0x80), out result));
            Assert.Equal(unchecked((nint)(int)0xFFFFFF80), result);

            Assert.True(NumberHelper<nint>.TryCreate<sbyte>(unchecked((sbyte)0xFF), out result));
            Assert.Equal(unchecked((nint)(int)0xFFFFFFFF), result);
        }

        [Fact]
        public static void TryCreateFromUInt16Test()
        {
            nint result;

            Assert.True(NumberHelper<nint>.TryCreate<ushort>(0x0000, out result));
            Assert.Equal((nint)0x00000000, result);

            Assert.True(NumberHelper<nint>.TryCreate<ushort>(0x0001, out result));
            Assert.Equal((nint)0x00000001, result);

            Assert.True(NumberHelper<nint>.TryCreate<ushort>(0x7FFF, out result));
            Assert.Equal((nint)0x00007FFF, result);

            Assert.True(NumberHelper<nint>.TryCreate<ushort>(0x8000, out result));
            Assert.Equal((nint)0x00008000, result);

            Assert.True(NumberHelper<nint>.TryCreate<ushort>(0xFFFF, out result));
            Assert.Equal((nint)0x0000FFFF, result);
        }

        [Fact]
        public static void TryCreateFromUInt32Test()
        {
            nint result;

            if (Environment.Is64BitProcess)
            {
                Assert.True(NumberHelper<nint>.TryCreate<uint>(0x00000000, out result));
                Assert.Equal((nint)0x00000000, result);

                Assert.True(NumberHelper<nint>.TryCreate<uint>(0x00000001, out result));
                Assert.Equal((nint)0x00000001, result);

                Assert.True(NumberHelper<nint>.TryCreate<uint>(0x7FFFFFFF, out result));
                Assert.Equal((nint)0x7FFFFFFF, result);

                Assert.True(NumberHelper<nint>.TryCreate<uint>(0x80000000, out result));
                Assert.Equal(unchecked((nint)0x0000000080000000), result);

                Assert.True(NumberHelper<nint>.TryCreate<uint>(0xFFFFFFFF, out result));
                Assert.Equal(unchecked((nint)0x00000000FFFFFFFF), result);
            }
            else
            {
                Assert.True(NumberHelper<nint>.TryCreate<uint>(0x00000000, out result));
                Assert.Equal((nint)0x00000000, result);

                Assert.True(NumberHelper<nint>.TryCreate<uint>(0x00000001, out result));
                Assert.Equal((nint)0x00000001, result);

                Assert.True(NumberHelper<nint>.TryCreate<uint>(0x7FFFFFFF, out result));
                Assert.Equal((nint)0x7FFFFFFF, result);

                Assert.False(NumberHelper<nint>.TryCreate<uint>(0x80000000, out result));
                Assert.Equal((nint)0x00000000, result);

                Assert.False(NumberHelper<nint>.TryCreate<uint>(0xFFFFFFFF, out result));
                Assert.Equal((nint)0x00000000, result);
            }
        }

        [Fact]
        public static void TryCreateFromUInt64Test()
        {
            nint result;

            if (Environment.Is64BitProcess)
            {
                Assert.True(NumberHelper<nint>.TryCreate<ulong>(0x0000000000000000, out result));
                Assert.Equal(unchecked((nint)0x0000000000000000), result);

                Assert.True(NumberHelper<nint>.TryCreate<ulong>(0x0000000000000001, out result));
                Assert.Equal(unchecked((nint)0x00000000000000001), result);

                Assert.True(NumberHelper<nint>.TryCreate<ulong>(0x7FFFFFFFFFFFFFFF, out result));
                Assert.Equal(unchecked((nint)0x7FFFFFFFFFFFFFFF), result);

                Assert.False(NumberHelper<nint>.TryCreate<ulong>(0x8000000000000000, out result));
                Assert.Equal((nint)0x0000000000000000, result);

                Assert.False(NumberHelper<nint>.TryCreate<ulong>(0xFFFFFFFFFFFFFFFF, out result));
                Assert.Equal((nint)0x0000000000000000, result);
            }
            else
            {
                Assert.True(NumberHelper<nint>.TryCreate<ulong>(0x0000000000000000, out result));
                Assert.Equal((nint)0x00000000, result);

                Assert.True(NumberHelper<nint>.TryCreate<ulong>(0x0000000000000001, out result));
                Assert.Equal((nint)0x00000001, result);

                Assert.False(NumberHelper<nint>.TryCreate<ulong>(0x7FFFFFFFFFFFFFFF, out result));
                Assert.Equal((nint)0x00000000, result);

                Assert.False(NumberHelper<nint>.TryCreate<ulong>(0x8000000000000000, out result));
                Assert.Equal((nint)0x00000000, result);

                Assert.False(NumberHelper<nint>.TryCreate<ulong>(0xFFFFFFFFFFFFFFFF, out result));
                Assert.Equal((nint)0x00000000, result);
            }
        }

        [Fact]
        public static void TryCreateFromUIntPtrTest()
        {
            nint result;

            if (Environment.Is64BitProcess)
            {
                Assert.True(NumberHelper<nint>.TryCreate<nuint>(unchecked((nuint)0x0000000000000000), out result));
                Assert.Equal(unchecked((nint)0x0000000000000000), result);

                Assert.True(NumberHelper<nint>.TryCreate<nuint>(unchecked((nuint)0x0000000000000001), out result));
                Assert.Equal(unchecked((nint)0x0000000000000001), result);

                Assert.True(NumberHelper<nint>.TryCreate<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF), out result));
                Assert.Equal(unchecked((nint)0x7FFFFFFFFFFFFFFF), result);

                Assert.False(NumberHelper<nint>.TryCreate<nuint>(unchecked((nuint)0x8000000000000000), out result));
                Assert.Equal((nint)0x0000000000000000, result);

                Assert.False(NumberHelper<nint>.TryCreate<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF), out result));
                Assert.Equal((nint)0x0000000000000000, result);
            }
            else
            {
                Assert.True(NumberHelper<nint>.TryCreate<nuint>((nuint)0x00000000, out result));
                Assert.Equal((nint)0x00000000, result);

                Assert.True(NumberHelper<nint>.TryCreate<nuint>((nuint)0x00000001, out result));
                Assert.Equal((nint)0x00000001, result);

                Assert.True(NumberHelper<nint>.TryCreate<nuint>((nuint)0x7FFFFFFF, out result));
                Assert.Equal((nint)0x7FFFFFFF, result);

                Assert.False(NumberHelper<nint>.TryCreate<nuint>(unchecked(unchecked((nuint)0x80000000)), out result));
                Assert.Equal((nint)0x00000000, result);

                Assert.False(NumberHelper<nint>.TryCreate<nuint>(unchecked(unchecked((nuint)0xFFFFFFFF)), out result));
                Assert.Equal((nint)0x00000000, result);
            }
        }

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
                Assert.Equal(expected, NumberHelper<nint>.Parse(value, style, provider));

                // Substitute default NumberFormatInfo
                Assert.True(NumberHelper<nint>.TryParse(value, style, new NumberFormatInfo(), out result));
                Assert.Equal(expected, result);
                Assert.Equal(expected, NumberHelper<nint>.Parse(value, style, new NumberFormatInfo()));
            }

            // Default style
            if (style == NumberStyles.Integer)
            {
                Assert.Equal(expected, ParsableHelper<nint>.Parse(value, provider));
            }

            // Full overloads
            Assert.True(NumberHelper<nint>.TryParse(value, style, provider, out result));
            Assert.Equal(expected, result);
            Assert.Equal(expected, NumberHelper<nint>.Parse(value, style, provider));
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
                Assert.Throws(exceptionType, () => NumberHelper<nint>.Parse(value, style, provider));

                // Substitute default NumberFormatInfo
                Assert.False(NumberHelper<nint>.TryParse(value, style, new NumberFormatInfo(), out result));
                Assert.Equal(default(nint), result);
                Assert.Throws(exceptionType, () => NumberHelper<nint>.Parse(value, style, new NumberFormatInfo()));
            }

            // Default style
            if (style == NumberStyles.Integer)
            {
                Assert.Throws(exceptionType, () => ParsableHelper<nint>.Parse(value, provider));
            }

            // Full overloads
            Assert.False(NumberHelper<nint>.TryParse(value, style, provider, out result));
            Assert.Equal(default(nint), result);
            Assert.Throws(exceptionType, () => NumberHelper<nint>.Parse(value, style, provider));
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

            Assert.Equal(expected, NumberHelper<nint>.Parse(value.AsSpan(offset, count), style, provider));

            Assert.True(NumberHelper<nint>.TryParse(value.AsSpan(offset, count), style, provider, out result));
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

            Assert.Throws(exceptionType, () => NumberHelper<nint>.Parse(value.AsSpan(), style, provider));

            Assert.False(NumberHelper<nint>.TryParse(value.AsSpan(), style, provider, out result));
            Assert.Equal(default(nint), result);
        }
    }
}
