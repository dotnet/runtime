// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Runtime.Versioning;
using Xunit;

namespace System.Tests
{
    public class UIntPtrTests_GenericMath
    {
        [Fact]
        public static void AdditiveIdentityTest()
        {
            Assert.Equal((nuint)0x00000000, AdditiveIdentityHelper<nuint, nuint>.AdditiveIdentity);
        }

        [Fact]
        public static void MinValueTest()
        {
            Assert.Equal((nuint)0x00000000, MinMaxValueHelper<nuint>.MinValue);
        }

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
        public static void MultiplicativeIdentityTest()
        {
            Assert.Equal((nuint)0x00000001, MultiplicativeIdentityHelper<nuint, nuint>.MultiplicativeIdentity);
        }

        [Fact]
        public static void OneTest()
        {
            Assert.Equal((nuint)0x00000001, NumberBaseHelper<nuint>.One);
        }

        [Fact]
        public static void ZeroTest()
        {
            Assert.Equal((nuint)0x00000000, NumberBaseHelper<nuint>.Zero);
        }

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

        [Fact]
        public static void AbsTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000000000000000), NumberHelper<nuint>.Abs(unchecked((nuint)0x0000000000000000)));
                Assert.Equal(unchecked((nuint)0x0000000000000001), NumberHelper<nuint>.Abs(unchecked((nuint)0x0000000000000001)));
                Assert.Equal(unchecked((nuint)0x7FFFFFFFFFFFFFFF), NumberHelper<nuint>.Abs(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((nuint)0x8000000000000000), NumberHelper<nuint>.Abs(unchecked((nuint)0x8000000000000000)));
                Assert.Equal(unchecked((nuint)0xFFFFFFFFFFFFFFFF), NumberHelper<nuint>.Abs(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((nuint)0x00000000, NumberHelper<nuint>.Abs((nuint)0x00000000));
                Assert.Equal((nuint)0x00000001, NumberHelper<nuint>.Abs((nuint)0x00000001));
                Assert.Equal((nuint)0x7FFFFFFF, NumberHelper<nuint>.Abs((nuint)0x7FFFFFFF));
                Assert.Equal((nuint)0x80000000, NumberHelper<nuint>.Abs((nuint)0x80000000));
                Assert.Equal((nuint)0xFFFFFFFF, NumberHelper<nuint>.Abs((nuint)0xFFFFFFFF));
            }
        }

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
        public static void CreateCheckedFromByteTest()
        {
            Assert.Equal((nuint)0x00000000, NumberHelper<nuint>.CreateChecked<byte>(0x00));
            Assert.Equal((nuint)0x00000001, NumberHelper<nuint>.CreateChecked<byte>(0x01));
            Assert.Equal((nuint)0x0000007F, NumberHelper<nuint>.CreateChecked<byte>(0x7F));
            Assert.Equal((nuint)0x00000080, NumberHelper<nuint>.CreateChecked<byte>(0x80));
            Assert.Equal((nuint)0x000000FF, NumberHelper<nuint>.CreateChecked<byte>(0xFF));
        }

        [Fact]
        public static void CreateCheckedFromCharTest()
        {
            Assert.Equal((nuint)0x00000000, NumberHelper<nuint>.CreateChecked<char>((char)0x0000));
            Assert.Equal((nuint)0x00000001, NumberHelper<nuint>.CreateChecked<char>((char)0x0001));
            Assert.Equal((nuint)0x00007FFF, NumberHelper<nuint>.CreateChecked<char>((char)0x7FFF));
            Assert.Equal((nuint)0x00008000, NumberHelper<nuint>.CreateChecked<char>((char)0x8000));
            Assert.Equal((nuint)0x0000FFFF, NumberHelper<nuint>.CreateChecked<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateCheckedFromInt16Test()
        {
            Assert.Equal((nuint)0x00000000, NumberHelper<nuint>.CreateChecked<short>(0x0000));
            Assert.Equal((nuint)0x00000001, NumberHelper<nuint>.CreateChecked<short>(0x0001));
            Assert.Equal((nuint)0x00007FFF, NumberHelper<nuint>.CreateChecked<short>(0x7FFF));
            Assert.Throws<OverflowException>(() => NumberHelper<nuint>.CreateChecked<short>(unchecked((short)0x8000)));
            Assert.Throws<OverflowException>(() => NumberHelper<nuint>.CreateChecked<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromInt32Test()
        {
            Assert.Equal((nuint)0x00000000, NumberHelper<nuint>.CreateChecked<int>(0x00000000));
            Assert.Equal((nuint)0x00000001, NumberHelper<nuint>.CreateChecked<int>(0x00000001));
            Assert.Equal((nuint)0x7FFFFFFF, NumberHelper<nuint>.CreateChecked<int>(0x7FFFFFFF));
            Assert.Throws<OverflowException>(() => NumberHelper<nuint>.CreateChecked<int>(unchecked((int)0x80000000)));
            Assert.Throws<OverflowException>(() => NumberHelper<nuint>.CreateChecked<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromInt64Test()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000000000000000), NumberHelper<nuint>.CreateChecked<long>(0x0000000000000000));
                Assert.Equal(unchecked((nuint)0x0000000000000001), NumberHelper<nuint>.CreateChecked<long>(0x0000000000000001));
                Assert.Equal(unchecked((nuint)0x7FFFFFFFFFFFFFFF), NumberHelper<nuint>.CreateChecked<long>(0x7FFFFFFFFFFFFFFF));
                Assert.Throws<OverflowException>(() => NumberHelper<nuint>.CreateChecked<long>(unchecked((long)0x8000000000000000)));
                Assert.Throws<OverflowException>(() => NumberHelper<nuint>.CreateChecked<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((nuint)0x00000000, NumberHelper<nuint>.CreateChecked<long>(0x0000000000000000));
                Assert.Equal((nuint)0x00000001, NumberHelper<nuint>.CreateChecked<long>(0x0000000000000001));
                Assert.Throws<OverflowException>(() => NumberHelper<nuint>.CreateChecked<long>(0x7FFFFFFFFFFFFFFF));
                Assert.Throws<OverflowException>(() => NumberHelper<nuint>.CreateChecked<long>(unchecked((long)0x8000000000000000)));
                Assert.Throws<OverflowException>(() => NumberHelper<nuint>.CreateChecked<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateCheckedFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000000000000000), NumberHelper<nuint>.CreateChecked<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal(unchecked((nuint)0x0000000000000001), NumberHelper<nuint>.CreateChecked<nint>(unchecked((nint)0x0000000000000001)));
                Assert.Equal(unchecked((nuint)0x7FFFFFFFFFFFFFFF), NumberHelper<nuint>.CreateChecked<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Throws<OverflowException>(() => NumberHelper<nuint>.CreateChecked<nint>(unchecked((nint)0x8000000000000000)));
                Assert.Throws<OverflowException>(() => NumberHelper<nuint>.CreateChecked<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((nuint)0x00000000, NumberHelper<nuint>.CreateChecked<nint>((nint)0x00000000));
                Assert.Equal((nuint)0x00000001, NumberHelper<nuint>.CreateChecked<nint>((nint)0x00000001));
                Assert.Equal((nuint)0x7FFFFFFF, NumberHelper<nuint>.CreateChecked<nint>((nint)0x7FFFFFFF));
                Assert.Throws<OverflowException>(() => NumberHelper<nuint>.CreateChecked<nint>(unchecked((nint)0x80000000)));
                Assert.Throws<OverflowException>(() => NumberHelper<nuint>.CreateChecked<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateCheckedFromSByteTest()
        {
            Assert.Equal((nuint)0x00000000, NumberHelper<nuint>.CreateChecked<sbyte>(0x00));
            Assert.Equal((nuint)0x00000001, NumberHelper<nuint>.CreateChecked<sbyte>(0x01));
            Assert.Equal((nuint)0x0000007F, NumberHelper<nuint>.CreateChecked<sbyte>(0x7F));
            Assert.Throws<OverflowException>(() => NumberHelper<nuint>.CreateChecked<sbyte>(unchecked((sbyte)0x80)));
            Assert.Throws<OverflowException>(() => NumberHelper<nuint>.CreateChecked<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateCheckedFromUInt16Test()
        {
            Assert.Equal((nuint)0x00000000, NumberHelper<nuint>.CreateChecked<ushort>(0x0000));
            Assert.Equal((nuint)0x00000001, NumberHelper<nuint>.CreateChecked<ushort>(0x0001));
            Assert.Equal((nuint)0x00007FFF, NumberHelper<nuint>.CreateChecked<ushort>(0x7FFF));
            Assert.Equal((nuint)0x00008000, NumberHelper<nuint>.CreateChecked<ushort>(0x8000));
            Assert.Equal((nuint)0x0000FFFF, NumberHelper<nuint>.CreateChecked<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUInt32Test()
        {
            Assert.Equal((nuint)0x00000000, NumberHelper<nuint>.CreateChecked<uint>(0x00000000));
            Assert.Equal((nuint)0x00000001, NumberHelper<nuint>.CreateChecked<uint>(0x00000001));
            Assert.Equal((nuint)0x7FFFFFFF, NumberHelper<nuint>.CreateChecked<uint>(0x7FFFFFFF));
            Assert.Equal((nuint)0x80000000, NumberHelper<nuint>.CreateChecked<uint>(0x80000000));
            Assert.Equal((nuint)0xFFFFFFFF, NumberHelper<nuint>.CreateChecked<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUInt64Test()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000000000000000), NumberHelper<nuint>.CreateChecked<ulong>(0x0000000000000000));
                Assert.Equal(unchecked((nuint)0x0000000000000001), NumberHelper<nuint>.CreateChecked<ulong>(0x0000000000000001));
                Assert.Equal(unchecked((nuint)0x7FFFFFFFFFFFFFFF), NumberHelper<nuint>.CreateChecked<ulong>(0x7FFFFFFFFFFFFFFF));
                Assert.Equal(unchecked((nuint)0x8000000000000000), NumberHelper<nuint>.CreateChecked<ulong>(0x8000000000000000));
                Assert.Equal(unchecked((nuint)0xFFFFFFFFFFFFFFFF), NumberHelper<nuint>.CreateChecked<ulong>(0xFFFFFFFFFFFFFFFF));
            }
            else
            {
                Assert.Equal((nuint)0x00000000, NumberHelper<nuint>.CreateChecked<ulong>(0x0000000000000000));
                Assert.Equal((nuint)0x00000001, NumberHelper<nuint>.CreateChecked<ulong>(0x0000000000000001));
                Assert.Throws<OverflowException>(() => NumberHelper<nuint>.CreateChecked<ulong>(0x7FFFFFFFFFFFFFFF));
                Assert.Throws<OverflowException>(() => NumberHelper<nuint>.CreateChecked<ulong>(0x8000000000000000));
                Assert.Throws<OverflowException>(() => NumberHelper<nuint>.CreateChecked<ulong>(0xFFFFFFFFFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateCheckedFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000000000000000), NumberHelper<nuint>.CreateChecked<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal(unchecked((nuint)0x0000000000000001), NumberHelper<nuint>.CreateChecked<nuint>(unchecked((nuint)0x0000000000000001)));
                Assert.Equal(unchecked((nuint)0x7FFFFFFFFFFFFFFF), NumberHelper<nuint>.CreateChecked<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((nuint)0x8000000000000000), NumberHelper<nuint>.CreateChecked<nuint>(unchecked((nuint)0x8000000000000000)));
                Assert.Equal(unchecked((nuint)0xFFFFFFFFFFFFFFFF), NumberHelper<nuint>.CreateChecked<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((nuint)0x00000000, NumberHelper<nuint>.CreateChecked<nuint>((nuint)0x00000000));
                Assert.Equal((nuint)0x00000001, NumberHelper<nuint>.CreateChecked<nuint>((nuint)0x00000001));
                Assert.Equal((nuint)0x7FFFFFFF, NumberHelper<nuint>.CreateChecked<nuint>((nuint)0x7FFFFFFF));
                Assert.Equal((nuint)0x80000000, NumberHelper<nuint>.CreateChecked<nuint>((nuint)0x80000000));
                Assert.Equal((nuint)0xFFFFFFFF, NumberHelper<nuint>.CreateChecked<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateSaturatingFromByteTest()
        {
            Assert.Equal((nuint)0x00000000, NumberHelper<nuint>.CreateSaturating<byte>(0x00));
            Assert.Equal((nuint)0x00000001, NumberHelper<nuint>.CreateSaturating<byte>(0x01));
            Assert.Equal((nuint)0x0000007F, NumberHelper<nuint>.CreateSaturating<byte>(0x7F));
            Assert.Equal((nuint)0x00000080, NumberHelper<nuint>.CreateSaturating<byte>(0x80));
            Assert.Equal((nuint)0x000000FF, NumberHelper<nuint>.CreateSaturating<byte>(0xFF));
        }

        [Fact]
        public static void CreateSaturatingFromCharTest()
        {
            Assert.Equal((nuint)0x00000000, NumberHelper<nuint>.CreateSaturating<char>((char)0x0000));
            Assert.Equal((nuint)0x00000001, NumberHelper<nuint>.CreateSaturating<char>((char)0x0001));
            Assert.Equal((nuint)0x00007FFF, NumberHelper<nuint>.CreateSaturating<char>((char)0x7FFF));
            Assert.Equal((nuint)0x00008000, NumberHelper<nuint>.CreateSaturating<char>((char)0x8000));
            Assert.Equal((nuint)0x0000FFFF, NumberHelper<nuint>.CreateSaturating<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromInt16Test()
        {
            Assert.Equal((nuint)0x00000000, NumberHelper<nuint>.CreateSaturating<short>(0x0000));
            Assert.Equal((nuint)0x00000001, NumberHelper<nuint>.CreateSaturating<short>(0x0001));
            Assert.Equal((nuint)0x00007FFF, NumberHelper<nuint>.CreateSaturating<short>(0x7FFF));
            Assert.Equal((nuint)0x00000000, NumberHelper<nuint>.CreateSaturating<short>(unchecked((short)0x8000)));
            Assert.Equal((nuint)0x00000000, NumberHelper<nuint>.CreateSaturating<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromInt32Test()
        {
            Assert.Equal((nuint)0x00000000, NumberHelper<nuint>.CreateSaturating<int>(0x00000000));
            Assert.Equal((nuint)0x00000001, NumberHelper<nuint>.CreateSaturating<int>(0x00000001));
            Assert.Equal((nuint)0x7FFFFFFF, NumberHelper<nuint>.CreateSaturating<int>(0x7FFFFFFF));
            Assert.Equal((nuint)0x00000000, NumberHelper<nuint>.CreateSaturating<int>(unchecked((int)0x80000000)));
            Assert.Equal((nuint)0x00000000, NumberHelper<nuint>.CreateSaturating<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromInt64Test()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000000000000000), NumberHelper<nuint>.CreateSaturating<long>(0x0000000000000000));
                Assert.Equal(unchecked((nuint)0x0000000000000001), NumberHelper<nuint>.CreateSaturating<long>(0x0000000000000001));
                Assert.Equal(unchecked((nuint)0x7FFFFFFFFFFFFFFF), NumberHelper<nuint>.CreateSaturating<long>(0x7FFFFFFFFFFFFFFF));
                Assert.Equal(unchecked((nuint)0x0000000000000000), NumberHelper<nuint>.CreateSaturating<long>(unchecked((long)0x8000000000000000)));
                Assert.Equal(unchecked((nuint)0x0000000000000000), NumberHelper<nuint>.CreateSaturating<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((nuint)0x00000000, NumberHelper<nuint>.CreateSaturating<long>(0x0000000000000000));
                Assert.Equal((nuint)0x00000001, NumberHelper<nuint>.CreateSaturating<long>(0x0000000000000001));
                Assert.Equal((nuint)0xFFFFFFFF, NumberHelper<nuint>.CreateSaturating<long>(0x7FFFFFFFFFFFFFFF));
                Assert.Equal((nuint)0x00000000, NumberHelper<nuint>.CreateSaturating<long>(unchecked((long)0x8000000000000000)));
                Assert.Equal((nuint)0x00000000, NumberHelper<nuint>.CreateSaturating<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateSaturatingFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000000000000000), NumberHelper<nuint>.CreateSaturating<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal(unchecked((nuint)0x0000000000000001), NumberHelper<nuint>.CreateSaturating<nint>(unchecked((nint)0x0000000000000001)));
                Assert.Equal(unchecked((nuint)0x7FFFFFFFFFFFFFFF), NumberHelper<nuint>.CreateSaturating<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((nuint)0x0000000000000000), NumberHelper<nuint>.CreateSaturating<nint>(unchecked((nint)0x8000000000000000)));
                Assert.Equal(unchecked((nuint)0x0000000000000000), NumberHelper<nuint>.CreateSaturating<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((nuint)0x00000000, NumberHelper<nuint>.CreateSaturating<nint>((nint)0x00000000));
                Assert.Equal((nuint)0x00000001, NumberHelper<nuint>.CreateSaturating<nint>((nint)0x00000001));
                Assert.Equal((nuint)0x7FFFFFFF, NumberHelper<nuint>.CreateSaturating<nint>((nint)0x7FFFFFFF));
                Assert.Equal((nuint)0x00000000, NumberHelper<nuint>.CreateSaturating<nint>(unchecked((nint)0x80000000)));
                Assert.Equal((nuint)0x00000000, NumberHelper<nuint>.CreateSaturating<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateSaturatingFromSByteTest()
        {
            Assert.Equal((nuint)0x00000000, NumberHelper<nuint>.CreateSaturating<sbyte>(0x00));
            Assert.Equal((nuint)0x00000001, NumberHelper<nuint>.CreateSaturating<sbyte>(0x01));
            Assert.Equal((nuint)0x0000007F, NumberHelper<nuint>.CreateSaturating<sbyte>(0x7F));
            Assert.Equal((nuint)0x00000000, NumberHelper<nuint>.CreateSaturating<sbyte>(unchecked((sbyte)0x80)));
            Assert.Equal((nuint)0x00000000, NumberHelper<nuint>.CreateSaturating<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateSaturatingFromUInt16Test()
        {
            Assert.Equal((nuint)0x00000000, NumberHelper<nuint>.CreateSaturating<ushort>(0x0000));
            Assert.Equal((nuint)0x00000001, NumberHelper<nuint>.CreateSaturating<ushort>(0x0001));
            Assert.Equal((nuint)0x00007FFF, NumberHelper<nuint>.CreateSaturating<ushort>(0x7FFF));
            Assert.Equal((nuint)0x00008000, NumberHelper<nuint>.CreateSaturating<ushort>(0x8000));
            Assert.Equal((nuint)0x0000FFFF, NumberHelper<nuint>.CreateSaturating<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUInt32Test()
        {
            Assert.Equal((nuint)0x00000000, NumberHelper<nuint>.CreateSaturating<uint>(0x00000000));
            Assert.Equal((nuint)0x00000001, NumberHelper<nuint>.CreateSaturating<uint>(0x00000001));
            Assert.Equal((nuint)0x7FFFFFFF, NumberHelper<nuint>.CreateSaturating<uint>(0x7FFFFFFF));
            Assert.Equal((nuint)0x80000000, NumberHelper<nuint>.CreateSaturating<uint>(0x80000000));
            Assert.Equal((nuint)0xFFFFFFFF, NumberHelper<nuint>.CreateSaturating<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUInt64Test()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000000000000000), NumberHelper<nuint>.CreateSaturating<ulong>(0x0000000000000000));
                Assert.Equal(unchecked((nuint)0x0000000000000001), NumberHelper<nuint>.CreateSaturating<ulong>(0x0000000000000001));
                Assert.Equal(unchecked((nuint)0x7FFFFFFFFFFFFFFF), NumberHelper<nuint>.CreateSaturating<ulong>(0x7FFFFFFFFFFFFFFF));
                Assert.Equal(unchecked((nuint)0x8000000000000000), NumberHelper<nuint>.CreateSaturating<ulong>(0x8000000000000000));
                Assert.Equal(unchecked((nuint)0xFFFFFFFFFFFFFFFF), NumberHelper<nuint>.CreateSaturating<ulong>(0xFFFFFFFFFFFFFFFF));
            }
            else
            {
                Assert.Equal((nuint)0x00000000, NumberHelper<nuint>.CreateSaturating<ulong>(0x0000000000000000));
                Assert.Equal((nuint)0x00000001, NumberHelper<nuint>.CreateSaturating<ulong>(0x0000000000000001));
                Assert.Equal((nuint)0xFFFFFFFF, NumberHelper<nuint>.CreateSaturating<ulong>(0x7FFFFFFFFFFFFFFF));
                Assert.Equal((nuint)0xFFFFFFFF, NumberHelper<nuint>.CreateSaturating<ulong>(0x8000000000000000));
                Assert.Equal((nuint)0xFFFFFFFF, NumberHelper<nuint>.CreateSaturating<ulong>(0xFFFFFFFFFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateSaturatingFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000000000000000), NumberHelper<nuint>.CreateSaturating<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal(unchecked((nuint)0x0000000000000001), NumberHelper<nuint>.CreateSaturating<nuint>(unchecked((nuint)0x0000000000000001)));
                Assert.Equal(unchecked((nuint)0x7FFFFFFFFFFFFFFF), NumberHelper<nuint>.CreateSaturating<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((nuint)0x8000000000000000), NumberHelper<nuint>.CreateSaturating<nuint>(unchecked((nuint)0x8000000000000000)));
                Assert.Equal(unchecked((nuint)0xFFFFFFFFFFFFFFFF), NumberHelper<nuint>.CreateSaturating<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((nuint)0x00000000, NumberHelper<nuint>.CreateSaturating<nuint>((nuint)0x00000000));
                Assert.Equal((nuint)0x00000001, NumberHelper<nuint>.CreateSaturating<nuint>((nuint)0x00000001));
                Assert.Equal((nuint)0x7FFFFFFF, NumberHelper<nuint>.CreateSaturating<nuint>((nuint)0x7FFFFFFF));
                Assert.Equal((nuint)0x80000000, NumberHelper<nuint>.CreateSaturating<nuint>((nuint)0x80000000));
                Assert.Equal((nuint)0xFFFFFFFF, NumberHelper<nuint>.CreateSaturating<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateTruncatingFromByteTest()
        {
            Assert.Equal((nuint)0x00000000, NumberHelper<nuint>.CreateTruncating<byte>(0x00));
            Assert.Equal((nuint)0x00000001, NumberHelper<nuint>.CreateTruncating<byte>(0x01));
            Assert.Equal((nuint)0x0000007F, NumberHelper<nuint>.CreateTruncating<byte>(0x7F));
            Assert.Equal((nuint)0x00000080, NumberHelper<nuint>.CreateTruncating<byte>(0x80));
            Assert.Equal((nuint)0x000000FF, NumberHelper<nuint>.CreateTruncating<byte>(0xFF));
        }

        [Fact]
        public static void CreateTruncatingFromCharTest()
        {
            Assert.Equal((nuint)0x00000000, NumberHelper<nuint>.CreateTruncating<char>((char)0x0000));
            Assert.Equal((nuint)0x00000001, NumberHelper<nuint>.CreateTruncating<char>((char)0x0001));
            Assert.Equal((nuint)0x00007FFF, NumberHelper<nuint>.CreateTruncating<char>((char)0x7FFF));
            Assert.Equal((nuint)0x00008000, NumberHelper<nuint>.CreateTruncating<char>((char)0x8000));
            Assert.Equal((nuint)0x0000FFFF, NumberHelper<nuint>.CreateTruncating<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromInt16Test()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000000000000000), NumberHelper<nuint>.CreateTruncating<short>(0x0000));
                Assert.Equal(unchecked((nuint)0x0000000000000001), NumberHelper<nuint>.CreateTruncating<short>(0x0001));
                Assert.Equal(unchecked((nuint)0x0000000000007FFF), NumberHelper<nuint>.CreateTruncating<short>(0x7FFF));
                Assert.Equal(unchecked((nuint)0xFFFFFFFFFFFF8000), NumberHelper<nuint>.CreateTruncating<short>(unchecked((short)0x8000)));
                Assert.Equal(unchecked((nuint)0xFFFFFFFFFFFFFFFF), NumberHelper<nuint>.CreateTruncating<short>(unchecked((short)0xFFFF)));
            }
            else
            {
                Assert.Equal((nuint)0x00000000, NumberHelper<nuint>.CreateTruncating<short>(0x0000));
                Assert.Equal((nuint)0x00000001, NumberHelper<nuint>.CreateTruncating<short>(0x0001));
                Assert.Equal((nuint)0x00007FFF, NumberHelper<nuint>.CreateTruncating<short>(0x7FFF));
                Assert.Equal((nuint)0xFFFF8000, NumberHelper<nuint>.CreateTruncating<short>(unchecked((short)0x8000)));
                Assert.Equal((nuint)0xFFFFFFFF, NumberHelper<nuint>.CreateTruncating<short>(unchecked((short)0xFFFF)));
            }
        }

        [Fact]
        public static void CreateTruncatingFromInt32Test()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000000000000000), NumberHelper<nuint>.CreateTruncating<int>(0x00000000));
                Assert.Equal(unchecked((nuint)0x0000000000000001), NumberHelper<nuint>.CreateTruncating<int>(0x00000001));
                Assert.Equal(unchecked((nuint)0x000000007FFFFFFF), NumberHelper<nuint>.CreateTruncating<int>(0x7FFFFFFF));
                Assert.Equal(unchecked((nuint)0xFFFFFFFF80000000), NumberHelper<nuint>.CreateTruncating<int>(unchecked((int)0x80000000)));
                Assert.Equal(unchecked((nuint)0xFFFFFFFFFFFFFFFF), NumberHelper<nuint>.CreateTruncating<int>(unchecked((int)0xFFFFFFFF)));
            }
            else
            {
                Assert.Equal((nuint)0x00000000, NumberHelper<nuint>.CreateTruncating<int>(0x00000000));
                Assert.Equal((nuint)0x00000001, NumberHelper<nuint>.CreateTruncating<int>(0x00000001));
                Assert.Equal((nuint)0x7FFFFFFF, NumberHelper<nuint>.CreateTruncating<int>(0x7FFFFFFF));
                Assert.Equal((nuint)0x80000000, NumberHelper<nuint>.CreateTruncating<int>(unchecked((int)0x80000000)));
                Assert.Equal((nuint)0xFFFFFFFF, NumberHelper<nuint>.CreateTruncating<int>(unchecked((int)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateTruncatingFromInt64Test()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000000000000000), NumberHelper<nuint>.CreateTruncating<long>(0x0000000000000000));
                Assert.Equal(unchecked((nuint)0x0000000000000001), NumberHelper<nuint>.CreateTruncating<long>(0x0000000000000001));
                Assert.Equal(unchecked((nuint)0x7FFFFFFFFFFFFFFF), NumberHelper<nuint>.CreateTruncating<long>(0x7FFFFFFFFFFFFFFF));
                Assert.Equal(unchecked((nuint)0x8000000000000000), NumberHelper<nuint>.CreateTruncating<long>(unchecked((long)0x8000000000000000)));
                Assert.Equal(unchecked((nuint)0xFFFFFFFFFFFFFFFF), NumberHelper<nuint>.CreateTruncating<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((nuint)0x00000000, NumberHelper<nuint>.CreateTruncating<long>(0x0000000000000000));
                Assert.Equal((nuint)0x00000001, NumberHelper<nuint>.CreateTruncating<long>(0x0000000000000001));
                Assert.Equal((nuint)0xFFFFFFFF, NumberHelper<nuint>.CreateTruncating<long>(0x7FFFFFFFFFFFFFFF));
                Assert.Equal((nuint)0x00000000, NumberHelper<nuint>.CreateTruncating<long>(unchecked((long)0x8000000000000000)));
                Assert.Equal((nuint)0xFFFFFFFF, NumberHelper<nuint>.CreateTruncating<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateTruncatingFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000000000000000), NumberHelper<nuint>.CreateTruncating<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal(unchecked((nuint)0x0000000000000001), NumberHelper<nuint>.CreateTruncating<nint>(unchecked((nint)0x0000000000000001)));
                Assert.Equal(unchecked((nuint)0x7FFFFFFFFFFFFFFF), NumberHelper<nuint>.CreateTruncating<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((nuint)0x8000000000000000), NumberHelper<nuint>.CreateTruncating<nint>(unchecked((nint)0x8000000000000000)));
                Assert.Equal(unchecked((nuint)0xFFFFFFFFFFFFFFFF), NumberHelper<nuint>.CreateTruncating<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((nuint)0x00000000, NumberHelper<nuint>.CreateTruncating<nint>((nint)0x00000000));
                Assert.Equal((nuint)0x00000001, NumberHelper<nuint>.CreateTruncating<nint>((nint)0x00000001));
                Assert.Equal((nuint)0x7FFFFFFF, NumberHelper<nuint>.CreateTruncating<nint>((nint)0x7FFFFFFF));
                Assert.Equal((nuint)0x80000000, NumberHelper<nuint>.CreateTruncating<nint>(unchecked((nint)0x80000000)));
                Assert.Equal((nuint)0xFFFFFFFF, NumberHelper<nuint>.CreateTruncating<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateTruncatingFromSByteTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000000000000000), NumberHelper<nuint>.CreateTruncating<sbyte>(0x00));
                Assert.Equal(unchecked((nuint)0x0000000000000001), NumberHelper<nuint>.CreateTruncating<sbyte>(0x01));
                Assert.Equal(unchecked((nuint)0x000000000000007F), NumberHelper<nuint>.CreateTruncating<sbyte>(0x7F));
                Assert.Equal(unchecked((nuint)0xFFFFFFFFFFFFFF80), NumberHelper<nuint>.CreateTruncating<sbyte>(unchecked((sbyte)0x80)));
                Assert.Equal(unchecked((nuint)0xFFFFFFFFFFFFFFFF), NumberHelper<nuint>.CreateTruncating<sbyte>(unchecked((sbyte)0xFF)));
            }
            else
            {
                Assert.Equal((nuint)0x00000000, NumberHelper<nuint>.CreateTruncating<sbyte>(0x00));
                Assert.Equal((nuint)0x00000001, NumberHelper<nuint>.CreateTruncating<sbyte>(0x01));
                Assert.Equal((nuint)0x0000007F, NumberHelper<nuint>.CreateTruncating<sbyte>(0x7F));
                Assert.Equal((nuint)0xFFFFFF80, NumberHelper<nuint>.CreateTruncating<sbyte>(unchecked((sbyte)0x80)));
                Assert.Equal((nuint)0xFFFFFFFF, NumberHelper<nuint>.CreateTruncating<sbyte>(unchecked((sbyte)0xFF)));
            }
        }

        [Fact]
        public static void CreateTruncatingFromUInt16Test()
        {
            Assert.Equal((nuint)0x00000000, NumberHelper<nuint>.CreateTruncating<ushort>(0x0000));
            Assert.Equal((nuint)0x00000001, NumberHelper<nuint>.CreateTruncating<ushort>(0x0001));
            Assert.Equal((nuint)0x00007FFF, NumberHelper<nuint>.CreateTruncating<ushort>(0x7FFF));
            Assert.Equal((nuint)0x00008000, NumberHelper<nuint>.CreateTruncating<ushort>(0x8000));
            Assert.Equal((nuint)0x0000FFFF, NumberHelper<nuint>.CreateTruncating<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt32Test()
        {
            Assert.Equal((nuint)0x00000000, NumberHelper<nuint>.CreateTruncating<uint>(0x00000000));
            Assert.Equal((nuint)0x00000001, NumberHelper<nuint>.CreateTruncating<uint>(0x00000001));
            Assert.Equal((nuint)0x7FFFFFFF, NumberHelper<nuint>.CreateTruncating<uint>(0x7FFFFFFF));
            Assert.Equal((nuint)0x80000000, NumberHelper<nuint>.CreateTruncating<uint>(0x80000000));
            Assert.Equal((nuint)0xFFFFFFFF, NumberHelper<nuint>.CreateTruncating<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt64Test()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000000000000000), NumberHelper<nuint>.CreateTruncating<ulong>(0x0000000000000000));
                Assert.Equal(unchecked((nuint)0x0000000000000001), NumberHelper<nuint>.CreateTruncating<ulong>(0x0000000000000001));
                Assert.Equal(unchecked((nuint)0x7FFFFFFFFFFFFFFF), NumberHelper<nuint>.CreateTruncating<ulong>(0x7FFFFFFFFFFFFFFF));
                Assert.Equal(unchecked((nuint)0x8000000000000000), NumberHelper<nuint>.CreateTruncating<ulong>(0x8000000000000000));
                Assert.Equal(unchecked((nuint)0xFFFFFFFFFFFFFFFF), NumberHelper<nuint>.CreateTruncating<ulong>(0xFFFFFFFFFFFFFFFF));
            }
            else
            {
                Assert.Equal((nuint)0x00000000, NumberHelper<nuint>.CreateTruncating<ulong>(0x0000000000000000));
                Assert.Equal((nuint)0x00000001, NumberHelper<nuint>.CreateTruncating<ulong>(0x0000000000000001));
                Assert.Equal((nuint)0xFFFFFFFF, NumberHelper<nuint>.CreateTruncating<ulong>(0x7FFFFFFFFFFFFFFF));
                Assert.Equal((nuint)0x00000000, NumberHelper<nuint>.CreateTruncating<ulong>(0x8000000000000000));
                Assert.Equal((nuint)0xFFFFFFFF, NumberHelper<nuint>.CreateTruncating<ulong>(0xFFFFFFFFFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateTruncatingFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000000000000000), NumberHelper<nuint>.CreateTruncating<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal(unchecked((nuint)0x0000000000000001), NumberHelper<nuint>.CreateTruncating<nuint>(unchecked((nuint)0x0000000000000001)));
                Assert.Equal(unchecked((nuint)0x7FFFFFFFFFFFFFFF), NumberHelper<nuint>.CreateTruncating<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((nuint)0x8000000000000000), NumberHelper<nuint>.CreateTruncating<nuint>(unchecked((nuint)0x8000000000000000)));
                Assert.Equal(unchecked((nuint)0xFFFFFFFFFFFFFFFF), NumberHelper<nuint>.CreateTruncating<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((nuint)0x00000000, NumberHelper<nuint>.CreateTruncating<nuint>((nuint)0x00000000));
                Assert.Equal((nuint)0x00000001, NumberHelper<nuint>.CreateTruncating<nuint>((nuint)0x00000001));
                Assert.Equal((nuint)0x7FFFFFFF, NumberHelper<nuint>.CreateTruncating<nuint>((nuint)0x7FFFFFFF));
                Assert.Equal((nuint)0x80000000, NumberHelper<nuint>.CreateTruncating<nuint>((nuint)0x80000000));
                Assert.Equal((nuint)0xFFFFFFFF, NumberHelper<nuint>.CreateTruncating<nuint>((nuint)0xFFFFFFFF));
            }
        }

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

        [Fact]
        public static void TryCreateFromByteTest()
        {
            nuint result;

            Assert.True(NumberHelper<nuint>.TryCreate<byte>(0x00, out result));
            Assert.Equal((nuint)0x00000000, result);

            Assert.True(NumberHelper<nuint>.TryCreate<byte>(0x01, out result));
            Assert.Equal((nuint)0x00000001, result);

            Assert.True(NumberHelper<nuint>.TryCreate<byte>(0x7F, out result));
            Assert.Equal((nuint)0x0000007F, result);

            Assert.True(NumberHelper<nuint>.TryCreate<byte>(0x80, out result));
            Assert.Equal((nuint)0x00000080, result);

            Assert.True(NumberHelper<nuint>.TryCreate<byte>(0xFF, out result));
            Assert.Equal((nuint)0x000000FF, result);
        }

        [Fact]
        public static void TryCreateFromCharTest()
        {
            nuint result;

            Assert.True(NumberHelper<nuint>.TryCreate<char>((char)0x0000, out result));
            Assert.Equal((nuint)0x00000000, result);

            Assert.True(NumberHelper<nuint>.TryCreate<char>((char)0x0001, out result));
            Assert.Equal((nuint)0x00000001, result);

            Assert.True(NumberHelper<nuint>.TryCreate<char>((char)0x7FFF, out result));
            Assert.Equal((nuint)0x00007FFF, result);

            Assert.True(NumberHelper<nuint>.TryCreate<char>((char)0x8000, out result));
            Assert.Equal((nuint)0x00008000, result);

            Assert.True(NumberHelper<nuint>.TryCreate<char>((char)0xFFFF, out result));
            Assert.Equal((nuint)0x0000FFFF, result);
        }

        [Fact]
        public static void TryCreateFromInt16Test()
        {
            nuint result;

            Assert.True(NumberHelper<nuint>.TryCreate<short>(0x0000, out result));
            Assert.Equal((nuint)0x00000000, result);

            Assert.True(NumberHelper<nuint>.TryCreate<short>(0x0001, out result));
            Assert.Equal((nuint)0x00000001, result);

            Assert.True(NumberHelper<nuint>.TryCreate<short>(0x7FFF, out result));
            Assert.Equal((nuint)0x00007FFF, result);

            Assert.False(NumberHelper<nuint>.TryCreate<short>(unchecked((short)0x8000), out result));
            Assert.Equal((nuint)0x00000000, result);

            Assert.False(NumberHelper<nuint>.TryCreate<short>(unchecked((short)0xFFFF), out result));
            Assert.Equal((nuint)0x00000000, result);
        }

        [Fact]
        public static void TryCreateFromInt32Test()
        {
            nuint result;

            Assert.True(NumberHelper<nuint>.TryCreate<int>(0x00000000, out result));
            Assert.Equal((nuint)0x00000000, result);

            Assert.True(NumberHelper<nuint>.TryCreate<int>(0x00000001, out result));
            Assert.Equal((nuint)0x00000001, result);

            Assert.True(NumberHelper<nuint>.TryCreate<int>(0x7FFFFFFF, out result));
            Assert.Equal((nuint)0x7FFFFFFF, result);

            Assert.False(NumberHelper<nuint>.TryCreate<int>(unchecked((int)0x80000000), out result));
            Assert.Equal((nuint)0x00000000, result);

            Assert.False(NumberHelper<nuint>.TryCreate<int>(unchecked((int)0xFFFFFFFF), out result));
            Assert.Equal((nuint)0x00000000, result);
        }

        [Fact]
        public static void TryCreateFromInt64Test()
        {
            nuint result;

            if (Environment.Is64BitProcess)
            {
                Assert.True(NumberHelper<nuint>.TryCreate<long>(0x0000000000000000, out result));
                Assert.Equal(unchecked((nuint)0x0000000000000000), result);

                Assert.True(NumberHelper<nuint>.TryCreate<long>(0x0000000000000001, out result));
                Assert.Equal(unchecked((nuint)0x0000000000000001), result);

                Assert.True(NumberHelper<nuint>.TryCreate<long>(0x7FFFFFFFFFFFFFFF, out result));
                Assert.Equal(unchecked((nuint)0x7FFFFFFFFFFFFFFF), result);

                Assert.False(NumberHelper<nuint>.TryCreate<long>(unchecked((long)0x8000000000000000), out result));
                Assert.Equal(unchecked((nuint)0x0000000000000000), result);

                Assert.False(NumberHelper<nuint>.TryCreate<long>(unchecked((long)0xFFFFFFFFFFFFFFFF), out result));
                Assert.Equal(unchecked((nuint)0x0000000000000000), result);
            }
            else
            {
                Assert.True(NumberHelper<nuint>.TryCreate<long>(0x0000000000000000, out result));
                Assert.Equal((nuint)0x00000000, result);

                Assert.True(NumberHelper<nuint>.TryCreate<long>(0x0000000000000001, out result));
                Assert.Equal((nuint)0x00000001, result);

                Assert.False(NumberHelper<nuint>.TryCreate<long>(0x7FFFFFFFFFFFFFFF, out result));
                Assert.Equal((nuint)0x00000000, result);

                Assert.False(NumberHelper<nuint>.TryCreate<long>(unchecked((long)0x8000000000000000), out result));
                Assert.Equal((nuint)0x00000000, result);

                Assert.False(NumberHelper<nuint>.TryCreate<long>(unchecked((long)0xFFFFFFFFFFFFFFFF), out result));
                Assert.Equal((nuint)0x00000000, result);
            }
        }

        [Fact]
        public static void TryCreateFromIntPtrTest()
        {
            nuint result;

            if (Environment.Is64BitProcess)
            {
                Assert.True(NumberHelper<nuint>.TryCreate<nint>(unchecked((nint)0x0000000000000000), out result));
                Assert.Equal(unchecked((nuint)0x0000000000000000), result);

                Assert.True(NumberHelper<nuint>.TryCreate<nint>(unchecked((nint)0x0000000000000001), out result));
                Assert.Equal(unchecked((nuint)0x0000000000000001), result);

                Assert.True(NumberHelper<nuint>.TryCreate<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF), out result));
                Assert.Equal(unchecked((nuint)0x7FFFFFFFFFFFFFFF), result);

                Assert.False(NumberHelper<nuint>.TryCreate<nint>(unchecked((nint)0x8000000000000000), out result));
                Assert.Equal(unchecked((nuint)0x0000000000000000), result);

                Assert.False(NumberHelper<nuint>.TryCreate<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF), out result));
                Assert.Equal(unchecked((nuint)0x0000000000000000), result);
            }
            else
            {
                Assert.True(NumberHelper<nuint>.TryCreate<nint>((nint)0x00000000, out result));
                Assert.Equal((nuint)0x00000000, result);

                Assert.True(NumberHelper<nuint>.TryCreate<nint>((nint)0x00000001, out result));
                Assert.Equal((nuint)0x00000001, result);

                Assert.True(NumberHelper<nuint>.TryCreate<nint>((nint)0x7FFFFFFF, out result));
                Assert.Equal((nuint)0x7FFFFFFF, result);

                Assert.False(NumberHelper<nuint>.TryCreate<nint>(unchecked((nint)0x80000000), out result));
                Assert.Equal((nuint)0x00000000, result);

                Assert.False(NumberHelper<nuint>.TryCreate<nint>(unchecked((nint)0xFFFFFFFF), out result));
                Assert.Equal((nuint)0x00000000, result);
            }
        }

        [Fact]
        public static void TryCreateFromSByteTest()
        {
            nuint result;

            Assert.True(NumberHelper<nuint>.TryCreate<sbyte>(0x00, out result));
            Assert.Equal((nuint)0x00000000, result);

            Assert.True(NumberHelper<nuint>.TryCreate<sbyte>(0x01, out result));
            Assert.Equal((nuint)0x00000001, result);

            Assert.True(NumberHelper<nuint>.TryCreate<sbyte>(0x7F, out result));
            Assert.Equal((nuint)0x0000007F, result);

            Assert.False(NumberHelper<nuint>.TryCreate<sbyte>(unchecked((sbyte)0x80), out result));
            Assert.Equal((nuint)0x00000000, result);

            Assert.False(NumberHelper<nuint>.TryCreate<sbyte>(unchecked((sbyte)0xFF), out result));
            Assert.Equal((nuint)0x00000000, result);
        }

        [Fact]
        public static void TryCreateFromUInt16Test()
        {
            nuint result;

            Assert.True(NumberHelper<nuint>.TryCreate<ushort>(0x0000, out result));
            Assert.Equal((nuint)0x00000000, result);

            Assert.True(NumberHelper<nuint>.TryCreate<ushort>(0x0001, out result));
            Assert.Equal((nuint)0x00000001, result);

            Assert.True(NumberHelper<nuint>.TryCreate<ushort>(0x7FFF, out result));
            Assert.Equal((nuint)0x00007FFF, result);

            Assert.True(NumberHelper<nuint>.TryCreate<ushort>(0x8000, out result));
            Assert.Equal((nuint)0x00008000, result);

            Assert.True(NumberHelper<nuint>.TryCreate<ushort>(0xFFFF, out result));
            Assert.Equal((nuint)0x0000FFFF, result);
        }

        [Fact]
        public static void TryCreateFromUInt32Test()
        {
            nuint result;

            Assert.True(NumberHelper<nuint>.TryCreate<uint>(0x00000000, out result));
            Assert.Equal((nuint)0x00000000, result);

            Assert.True(NumberHelper<nuint>.TryCreate<uint>(0x00000001, out result));
            Assert.Equal((nuint)0x00000001, result);

            Assert.True(NumberHelper<nuint>.TryCreate<uint>(0x7FFFFFFF, out result));
            Assert.Equal((nuint)0x7FFFFFFF, result);

            Assert.True(NumberHelper<nuint>.TryCreate<uint>(0x80000000, out result));
            Assert.Equal((nuint)0x80000000, result);

            Assert.True(NumberHelper<nuint>.TryCreate<uint>(0xFFFFFFFF, out result));
            Assert.Equal((nuint)0xFFFFFFFF, result);
        }

        [Fact]
        public static void TryCreateFromUInt64Test()
        {
            nuint result;

            if (Environment.Is64BitProcess)
            {
                Assert.True(NumberHelper<nuint>.TryCreate<ulong>(0x0000000000000000, out result));
                Assert.Equal(unchecked((nuint)0x0000000000000000), result);

                Assert.True(NumberHelper<nuint>.TryCreate<ulong>(0x0000000000000001, out result));
                Assert.Equal(unchecked((nuint)0x00000000000000001), result);

                Assert.True(NumberHelper<nuint>.TryCreate<ulong>(0x7FFFFFFFFFFFFFFF, out result));
                Assert.Equal(unchecked((nuint)0x7FFFFFFFFFFFFFFF), result);

                Assert.True(NumberHelper<nuint>.TryCreate<ulong>(0x8000000000000000, out result));
                Assert.Equal(unchecked((nuint)0x8000000000000000), result);

                Assert.True(NumberHelper<nuint>.TryCreate<ulong>(0xFFFFFFFFFFFFFFFF, out result));
                Assert.Equal(unchecked((nuint)0xFFFFFFFFFFFFFFFF), result);
            }
            else
            {
                Assert.True(NumberHelper<nuint>.TryCreate<ulong>(0x0000000000000000, out result));
                Assert.Equal((nuint)0x00000000, result);

                Assert.True(NumberHelper<nuint>.TryCreate<ulong>(0x0000000000000001, out result));
                Assert.Equal((nuint)0x00000001, result);

                Assert.False(NumberHelper<nuint>.TryCreate<ulong>(0x7FFFFFFFFFFFFFFF, out result));
                Assert.Equal((nuint)0x00000000, result);

                Assert.False(NumberHelper<nuint>.TryCreate<ulong>(0x8000000000000000, out result));
                Assert.Equal((nuint)0x00000000, result);

                Assert.False(NumberHelper<nuint>.TryCreate<ulong>(0xFFFFFFFFFFFFFFFF, out result));
                Assert.Equal((nuint)0x00000000, result);
            }
        }

        [Fact]
        public static void TryCreateFromUIntPtrTest()
        {
            nuint result;

            if (Environment.Is64BitProcess)
            {
                Assert.True(NumberHelper<nuint>.TryCreate<nuint>(unchecked((nuint)0x0000000000000000), out result));
                Assert.Equal(unchecked((nuint)0x0000000000000000), result);

                Assert.True(NumberHelper<nuint>.TryCreate<nuint>(unchecked((nuint)0x0000000000000001), out result));
                Assert.Equal(unchecked((nuint)0x0000000000000001), result);

                Assert.True(NumberHelper<nuint>.TryCreate<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF), out result));
                Assert.Equal(unchecked((nuint)0x7FFFFFFFFFFFFFFF), result);

                Assert.True(NumberHelper<nuint>.TryCreate<nuint>(unchecked((nuint)0x8000000000000000), out result));
                Assert.Equal(unchecked((nuint)0x8000000000000000), result);

                Assert.True(NumberHelper<nuint>.TryCreate<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF), out result));
                Assert.Equal(unchecked((nuint)0xFFFFFFFFFFFFFFFF), result);
            }
            else
            {
                Assert.True(NumberHelper<nuint>.TryCreate<nuint>((nuint)0x00000000, out result));
                Assert.Equal((nuint)0x00000000, result);

                Assert.True(NumberHelper<nuint>.TryCreate<nuint>((nuint)0x00000001, out result));
                Assert.Equal((nuint)0x00000001, result);

                Assert.True(NumberHelper<nuint>.TryCreate<nuint>((nuint)0x7FFFFFFF, out result));
                Assert.Equal((nuint)0x7FFFFFFF, result);

                Assert.True(NumberHelper<nuint>.TryCreate<nuint>(unchecked((nuint)0x80000000), out result));
                Assert.Equal((nuint)0x80000000, result);

                Assert.True(NumberHelper<nuint>.TryCreate<nuint>(unchecked((nuint)0xFFFFFFFF), out result));
                Assert.Equal((nuint)0xFFFFFFFF, result);
            }
        }

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
                Assert.Equal(expected, NumberHelper<nuint>.Parse(value, style, provider));

                // Substitute default NumberFormatInfo
                Assert.True(NumberHelper<nuint>.TryParse(value, style, new NumberFormatInfo(), out result));
                Assert.Equal(expected, result);
                Assert.Equal(expected, NumberHelper<nuint>.Parse(value, style, new NumberFormatInfo()));
            }

            // Default style
            if (style == NumberStyles.Integer)
            {
                Assert.Equal(expected, ParsableHelper<nuint>.Parse(value, provider));
            }

            // Full overloads
            Assert.True(NumberHelper<nuint>.TryParse(value, style, provider, out result));
            Assert.Equal(expected, result);
            Assert.Equal(expected, NumberHelper<nuint>.Parse(value, style, provider));
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
                Assert.Throws(exceptionType, () => NumberHelper<nuint>.Parse(value, style, provider));

                // Substitute default NumberFormatInfo
                Assert.False(NumberHelper<nuint>.TryParse(value, style, new NumberFormatInfo(), out result));
                Assert.Equal(default(nuint), result);
                Assert.Throws(exceptionType, () => NumberHelper<nuint>.Parse(value, style, new NumberFormatInfo()));
            }

            // Default style
            if (style == NumberStyles.Integer)
            {
                Assert.Throws(exceptionType, () => ParsableHelper<nuint>.Parse(value, provider));
            }

            // Full overloads
            Assert.False(NumberHelper<nuint>.TryParse(value, style, provider, out result));
            Assert.Equal(default(nuint), result);
            Assert.Throws(exceptionType, () => NumberHelper<nuint>.Parse(value, style, provider));
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

            Assert.Equal(expected, NumberHelper<nuint>.Parse(value.AsSpan(offset, count), style, provider));

            Assert.True(NumberHelper<nuint>.TryParse(value.AsSpan(offset, count), style, provider, out result));
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

            Assert.Throws(exceptionType, () => NumberHelper<nuint>.Parse(value.AsSpan(), style, provider));

            Assert.False(NumberHelper<nuint>.TryParse(value.AsSpan(), style, provider, out result));
            Assert.Equal(default(nuint), result);
        }
    }
}
