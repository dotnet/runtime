// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Xunit;

namespace System.Tests
{
    public class Int16Tests_GenericMath
    {
        [Fact]
        public static void AdditiveIdentityTest()
        {
            Assert.Equal((short)0x0000, AdditiveIdentityHelper<short, short>.AdditiveIdentity);
        }

        [Fact]
        public static void MinValueTest()
        {
            Assert.Equal(unchecked((short)0x8000), MinMaxValueHelper<short>.MinValue);
        }

        [Fact]
        public static void MaxValueTest()
        {
            Assert.Equal((short)0x7FFF, MinMaxValueHelper<short>.MaxValue);
        }

        [Fact]
        public static void MultiplicativeIdentityTest()
        {
            Assert.Equal((short)0x0001, MultiplicativeIdentityHelper<short, short>.MultiplicativeIdentity);
        }

        [Fact]
        public static void NegativeOneTest()
        {
            Assert.Equal(unchecked((short)0xFFFF), SignedNumberHelper<short>.NegativeOne);
        }

        [Fact]
        public static void OneTest()
        {
            Assert.Equal((short)0x0001, NumberBaseHelper<short>.One);
        }

        [Fact]
        public static void ZeroTest()
        {
            Assert.Equal((short)0x0000, NumberBaseHelper<short>.Zero);
        }

        [Fact]
        public static void op_AdditionTest()
        {
            Assert.Equal((short)0x0001, AdditionOperatorsHelper<short, short, short>.op_Addition((short)0x0000, (short)1));
            Assert.Equal((short)0x0002, AdditionOperatorsHelper<short, short, short>.op_Addition((short)0x0001, (short)1));
            Assert.Equal(unchecked((short)0x8000), AdditionOperatorsHelper<short, short, short>.op_Addition((short)0x7FFF, (short)1));
            Assert.Equal(unchecked((short)0x8001), AdditionOperatorsHelper<short, short, short>.op_Addition(unchecked((short)0x8000), (short)1));
            Assert.Equal((short)0x0000, AdditionOperatorsHelper<short, short, short>.op_Addition(unchecked((short)0xFFFF), (short)1));
        }

        [Fact]
        public static void op_CheckedAdditionTest()
        {
            Assert.Equal((short)0x0001, AdditionOperatorsHelper<short, short, short>.op_CheckedAddition((short)0x0000, (short)1));
            Assert.Equal((short)0x0002, AdditionOperatorsHelper<short, short, short>.op_CheckedAddition((short)0x0001, (short)1));
            Assert.Equal(unchecked((short)0x8001), AdditionOperatorsHelper<short, short, short>.op_CheckedAddition(unchecked((short)0x8000), (short)1));
            Assert.Equal((short)0x0000, AdditionOperatorsHelper<short, short, short>.op_CheckedAddition(unchecked((short)0xFFFF), (short)1));

            Assert.Throws<OverflowException>(() => AdditionOperatorsHelper<short, short, short>.op_CheckedAddition((short)0x7FFF, (short)1));
        }

        [Fact]
        public static void LeadingZeroCountTest()
        {
            Assert.Equal((short)0x0010, BinaryIntegerHelper<short>.LeadingZeroCount((short)0x0000));
            Assert.Equal((short)0x000F, BinaryIntegerHelper<short>.LeadingZeroCount((short)0x0001));
            Assert.Equal((short)0x0001, BinaryIntegerHelper<short>.LeadingZeroCount((short)0x7FFF));
            Assert.Equal((short)0x0000, BinaryIntegerHelper<short>.LeadingZeroCount(unchecked((short)0x8000)));
            Assert.Equal((short)0x0000, BinaryIntegerHelper<short>.LeadingZeroCount(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void PopCountTest()
        {
            Assert.Equal((short)0x0000, BinaryIntegerHelper<short>.PopCount((short)0x0000));
            Assert.Equal((short)0x0001, BinaryIntegerHelper<short>.PopCount((short)0x0001));
            Assert.Equal((short)0x000F, BinaryIntegerHelper<short>.PopCount((short)0x7FFF));
            Assert.Equal((short)0x0001, BinaryIntegerHelper<short>.PopCount(unchecked((short)0x8000)));
            Assert.Equal((short)0x0010, BinaryIntegerHelper<short>.PopCount(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void RotateLeftTest()
        {
            Assert.Equal((short)0x0000, BinaryIntegerHelper<short>.RotateLeft((short)0x0000, 1));
            Assert.Equal((short)0x0002, BinaryIntegerHelper<short>.RotateLeft((short)0x0001, 1));
            Assert.Equal(unchecked((short)0xFFFE), BinaryIntegerHelper<short>.RotateLeft((short)0x7FFF, 1));
            Assert.Equal((short)0x0001, BinaryIntegerHelper<short>.RotateLeft(unchecked((short)0x8000), 1));
            Assert.Equal(unchecked((short)0xFFFF), BinaryIntegerHelper<short>.RotateLeft(unchecked((short)0xFFFF), 1));
        }

        [Fact]
        public static void RotateRightTest()
        {
            Assert.Equal((short)0x0000, BinaryIntegerHelper<short>.RotateRight((short)0x0000, 1));
            Assert.Equal(unchecked((short)0x8000), BinaryIntegerHelper<short>.RotateRight((short)0x0001, 1));
            Assert.Equal(unchecked((short)0xBFFF), BinaryIntegerHelper<short>.RotateRight((short)0x7FFF, 1));
            Assert.Equal((short)0x4000, BinaryIntegerHelper<short>.RotateRight(unchecked((short)0x8000), 1));
            Assert.Equal(unchecked((short)0xFFFF), BinaryIntegerHelper<short>.RotateRight(unchecked((short)0xFFFF), 1));
        }

        [Fact]
        public static void TrailingZeroCountTest()
        {
            Assert.Equal((short)0x0010, BinaryIntegerHelper<short>.TrailingZeroCount((short)0x0000));
            Assert.Equal((short)0x0000, BinaryIntegerHelper<short>.TrailingZeroCount((short)0x0001));
            Assert.Equal((short)0x0000, BinaryIntegerHelper<short>.TrailingZeroCount((short)0x7FFF));
            Assert.Equal((short)0x000F, BinaryIntegerHelper<short>.TrailingZeroCount(unchecked((short)0x8000)));
            Assert.Equal((short)0x0000, BinaryIntegerHelper<short>.TrailingZeroCount(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void GetShortestBitLengthTest()
        {
            Assert.Equal(0x00, BinaryIntegerHelper<short>.GetShortestBitLength((short)0x0000));
            Assert.Equal(0x01, BinaryIntegerHelper<short>.GetShortestBitLength((short)0x0001));
            Assert.Equal(0x0F, BinaryIntegerHelper<short>.GetShortestBitLength((short)0x7FFF));
            Assert.Equal(0x10, BinaryIntegerHelper<short>.GetShortestBitLength(unchecked((short)0x8000)));
            Assert.Equal(0x01, BinaryIntegerHelper<short>.GetShortestBitLength(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void IsPow2Test()
        {
            Assert.False(BinaryNumberHelper<short>.IsPow2((short)0x0000));
            Assert.True(BinaryNumberHelper<short>.IsPow2((short)0x0001));
            Assert.False(BinaryNumberHelper<short>.IsPow2((short)0x7FFF));
            Assert.False(BinaryNumberHelper<short>.IsPow2(unchecked((short)0x8000)));
            Assert.False(BinaryNumberHelper<short>.IsPow2(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void Log2Test()
        {
            Assert.Equal((short)0x0000, BinaryNumberHelper<short>.Log2((short)0x0000));
            Assert.Equal((short)0x0000, BinaryNumberHelper<short>.Log2((short)0x0001));
            Assert.Equal((short)0x000E, BinaryNumberHelper<short>.Log2((short)0x7FFF));
            Assert.Throws<ArgumentOutOfRangeException>(() => BinaryNumberHelper<short>.Log2(unchecked((short)0x8000)));
            Assert.Throws<ArgumentOutOfRangeException>(() => BinaryNumberHelper<short>.Log2(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void op_BitwiseAndTest()
        {
            Assert.Equal((short)0x0000, BitwiseOperatorsHelper<short, short, short>.op_BitwiseAnd((short)0x0000, (short)1));
            Assert.Equal((short)0x0001, BitwiseOperatorsHelper<short, short, short>.op_BitwiseAnd((short)0x0001, (short)1));
            Assert.Equal((short)0x0001, BitwiseOperatorsHelper<short, short, short>.op_BitwiseAnd((short)0x7FFF, (short)1));
            Assert.Equal((short)0x0000, BitwiseOperatorsHelper<short, short, short>.op_BitwiseAnd(unchecked((short)0x8000), (short)1));
            Assert.Equal((short)0x0001, BitwiseOperatorsHelper<short, short, short>.op_BitwiseAnd(unchecked((short)0xFFFF), (short)1));
        }

        [Fact]
        public static void op_BitwiseOrTest()
        {
            Assert.Equal((short)0x0001, BitwiseOperatorsHelper<short, short, short>.op_BitwiseOr((short)0x0000, (short)1));
            Assert.Equal((short)0x0001, BitwiseOperatorsHelper<short, short, short>.op_BitwiseOr((short)0x0001, (short)1));
            Assert.Equal((short)0x7FFF, BitwiseOperatorsHelper<short, short, short>.op_BitwiseOr((short)0x7FFF, (short)1));
            Assert.Equal(unchecked((short)0x8001), BitwiseOperatorsHelper<short, short, short>.op_BitwiseOr(unchecked((short)0x8000), (short)1));
            Assert.Equal(unchecked((short)0xFFFF), BitwiseOperatorsHelper<short, short, short>.op_BitwiseOr(unchecked((short)0xFFFF), (short)1));
        }

        [Fact]
        public static void op_ExclusiveOrTest()
        {
            Assert.Equal((short)0x0001, BitwiseOperatorsHelper<short, short, short>.op_ExclusiveOr((short)0x0000, (short)1));
            Assert.Equal((short)0x0000, BitwiseOperatorsHelper<short, short, short>.op_ExclusiveOr((short)0x0001, (short)1));
            Assert.Equal((short)0x7FFE, BitwiseOperatorsHelper<short, short, short>.op_ExclusiveOr((short)0x7FFF, (short)1));
            Assert.Equal(unchecked((short)0x8001), BitwiseOperatorsHelper<short, short, short>.op_ExclusiveOr(unchecked((short)0x8000), (short)1));
            Assert.Equal(unchecked((short)0xFFFE), BitwiseOperatorsHelper<short, short, short>.op_ExclusiveOr(unchecked((short)0xFFFF), (short)1));
        }

        [Fact]
        public static void op_OnesComplementTest()
        {
            Assert.Equal(unchecked((short)0xFFFF), BitwiseOperatorsHelper<short, short, short>.op_OnesComplement((short)0x0000));
            Assert.Equal(unchecked((short)0xFFFE), BitwiseOperatorsHelper<short, short, short>.op_OnesComplement((short)0x0001));
            Assert.Equal(unchecked((short)0x8000), BitwiseOperatorsHelper<short, short, short>.op_OnesComplement((short)0x7FFF));
            Assert.Equal((short)0x7FFF, BitwiseOperatorsHelper<short, short, short>.op_OnesComplement(unchecked((short)0x8000)));
            Assert.Equal((short)0x0000, BitwiseOperatorsHelper<short, short, short>.op_OnesComplement(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void op_LessThanTest()
        {
            Assert.True(ComparisonOperatorsHelper<short, short>.op_LessThan((short)0x0000, (short)1));
            Assert.False(ComparisonOperatorsHelper<short, short>.op_LessThan((short)0x0001, (short)1));
            Assert.False(ComparisonOperatorsHelper<short, short>.op_LessThan((short)0x7FFF, (short)1));
            Assert.True(ComparisonOperatorsHelper<short, short>.op_LessThan(unchecked((short)0x8000), (short)1));
            Assert.True(ComparisonOperatorsHelper<short, short>.op_LessThan(unchecked((short)0xFFFF), (short)1));
        }

        [Fact]
        public static void op_LessThanOrEqualTest()
        {
            Assert.True(ComparisonOperatorsHelper<short, short>.op_LessThanOrEqual((short)0x0000, (short)1));
            Assert.True(ComparisonOperatorsHelper<short, short>.op_LessThanOrEqual((short)0x0001, (short)1));
            Assert.False(ComparisonOperatorsHelper<short, short>.op_LessThanOrEqual((short)0x7FFF, (short)1));
            Assert.True(ComparisonOperatorsHelper<short, short>.op_LessThanOrEqual(unchecked((short)0x8000), (short)1));
            Assert.True(ComparisonOperatorsHelper<short, short>.op_LessThanOrEqual(unchecked((short)0xFFFF), (short)1));
        }

        [Fact]
        public static void op_GreaterThanTest()
        {
            Assert.False(ComparisonOperatorsHelper<short, short>.op_GreaterThan((short)0x0000, (short)1));
            Assert.False(ComparisonOperatorsHelper<short, short>.op_GreaterThan((short)0x0001, (short)1));
            Assert.True(ComparisonOperatorsHelper<short, short>.op_GreaterThan((short)0x7FFF, (short)1));
            Assert.False(ComparisonOperatorsHelper<short, short>.op_GreaterThan(unchecked((short)0x8000), (short)1));
            Assert.False(ComparisonOperatorsHelper<short, short>.op_GreaterThan(unchecked((short)0xFFFF), (short)1));
        }

        [Fact]
        public static void op_GreaterThanOrEqualTest()
        {
            Assert.False(ComparisonOperatorsHelper<short, short>.op_GreaterThanOrEqual((short)0x0000, (short)1));
            Assert.True(ComparisonOperatorsHelper<short, short>.op_GreaterThanOrEqual((short)0x0001, (short)1));
            Assert.True(ComparisonOperatorsHelper<short, short>.op_GreaterThanOrEqual((short)0x7FFF, (short)1));
            Assert.False(ComparisonOperatorsHelper<short, short>.op_GreaterThanOrEqual(unchecked((short)0x8000), (short)1));
            Assert.False(ComparisonOperatorsHelper<short, short>.op_GreaterThanOrEqual(unchecked((short)0xFFFF), (short)1));
        }

        [Fact]
        public static void op_DecrementTest()
        {
            Assert.Equal(unchecked((short)0xFFFF), DecrementOperatorsHelper<short>.op_Decrement((short)0x0000));
            Assert.Equal((short)0x0000, DecrementOperatorsHelper<short>.op_Decrement((short)0x0001));
            Assert.Equal((short)0x7FFE, DecrementOperatorsHelper<short>.op_Decrement((short)0x7FFF));
            Assert.Equal((short)0x7FFF, DecrementOperatorsHelper<short>.op_Decrement(unchecked((short)0x8000)));
            Assert.Equal(unchecked((short)0xFFFE), DecrementOperatorsHelper<short>.op_Decrement(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void op_CheckedDecrementTest()
        {
            Assert.Equal(unchecked((short)0xFFFF), DecrementOperatorsHelper<short>.op_CheckedDecrement((short)0x0000));
            Assert.Equal((short)0x0000, DecrementOperatorsHelper<short>.op_CheckedDecrement((short)0x0001));
            Assert.Equal((short)0x7FFE, DecrementOperatorsHelper<short>.op_CheckedDecrement((short)0x7FFF));
            Assert.Equal(unchecked((short)0xFFFE), DecrementOperatorsHelper<short>.op_CheckedDecrement(unchecked((short)0xFFFF)));

            Assert.Throws<OverflowException>(() => DecrementOperatorsHelper<short>.op_CheckedDecrement(unchecked((short)0x8000)));
        }

        [Fact]
        public static void op_DivisionTest()
        {
            Assert.Equal((short)0x0000, DivisionOperatorsHelper<short, short, short>.op_Division((short)0x0000, (short)2));
            Assert.Equal((short)0x0000, DivisionOperatorsHelper<short, short, short>.op_Division((short)0x0001, (short)2));
            Assert.Equal((short)0x3FFF, DivisionOperatorsHelper<short, short, short>.op_Division((short)0x7FFF, (short)2));
            Assert.Equal(unchecked((short)0xC000), DivisionOperatorsHelper<short, short, short>.op_Division(unchecked((short)0x8000), (short)2));
            Assert.Equal((short)0x0000, DivisionOperatorsHelper<short, short, short>.op_Division(unchecked((short)0xFFFF), (short)2));

            Assert.Throws<DivideByZeroException>(() => DivisionOperatorsHelper<short, short, short>.op_Division((short)0x0001, (short)0));
        }

        [Fact]
        public static void op_CheckedDivisionTest()
        {
            Assert.Equal((short)0x0000, DivisionOperatorsHelper<short, short, short>.op_CheckedDivision((short)0x0000, (short)2));
            Assert.Equal((short)0x0000, DivisionOperatorsHelper<short, short, short>.op_CheckedDivision((short)0x0001, (short)2));
            Assert.Equal((short)0x3FFF, DivisionOperatorsHelper<short, short, short>.op_CheckedDivision((short)0x7FFF, (short)2));
            Assert.Equal(unchecked((short)0xC000), DivisionOperatorsHelper<short, short, short>.op_CheckedDivision(unchecked((short)0x8000), (short)2));
            Assert.Equal((short)0x0000, DivisionOperatorsHelper<short, short, short>.op_CheckedDivision(unchecked((short)0xFFFF), (short)2));

            Assert.Throws<DivideByZeroException>(() => DivisionOperatorsHelper<short, short, short>.op_CheckedDivision((short)0x0001, (short)0));
        }

        [Fact]
        public static void op_EqualityTest()
        {
            Assert.False(EqualityOperatorsHelper<short, short>.op_Equality((short)0x0000, (short)1));
            Assert.True(EqualityOperatorsHelper<short, short>.op_Equality((short)0x0001, (short)1));
            Assert.False(EqualityOperatorsHelper<short, short>.op_Equality((short)0x7FFF, (short)1));
            Assert.False(EqualityOperatorsHelper<short, short>.op_Equality(unchecked((short)0x8000), (short)1));
            Assert.False(EqualityOperatorsHelper<short, short>.op_Equality(unchecked((short)0xFFFF), (short)1));
        }

        [Fact]
        public static void op_InequalityTest()
        {
            Assert.True(EqualityOperatorsHelper<short, short>.op_Inequality((short)0x0000, (short)1));
            Assert.False(EqualityOperatorsHelper<short, short>.op_Inequality((short)0x0001, (short)1));
            Assert.True(EqualityOperatorsHelper<short, short>.op_Inequality((short)0x7FFF, (short)1));
            Assert.True(EqualityOperatorsHelper<short, short>.op_Inequality(unchecked((short)0x8000), (short)1));
            Assert.True(EqualityOperatorsHelper<short, short>.op_Inequality(unchecked((short)0xFFFF), (short)1));
        }

        [Fact]
        public static void op_IncrementTest()
        {
            Assert.Equal((short)0x0001, IncrementOperatorsHelper<short>.op_Increment((short)0x0000));
            Assert.Equal((short)0x0002, IncrementOperatorsHelper<short>.op_Increment((short)0x0001));
            Assert.Equal(unchecked((short)0x8000), IncrementOperatorsHelper<short>.op_Increment((short)0x7FFF));
            Assert.Equal(unchecked((short)0x8001), IncrementOperatorsHelper<short>.op_Increment(unchecked((short)0x8000)));
            Assert.Equal((short)0x0000, IncrementOperatorsHelper<short>.op_Increment(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void op_CheckedIncrementTest()
        {
            Assert.Equal((short)0x0001, IncrementOperatorsHelper<short>.op_CheckedIncrement((short)0x0000));
            Assert.Equal((short)0x0002, IncrementOperatorsHelper<short>.op_CheckedIncrement((short)0x0001));
            Assert.Equal(unchecked((short)0x8001), IncrementOperatorsHelper<short>.op_CheckedIncrement(unchecked((short)0x8000)));
            Assert.Equal((short)0x0000, IncrementOperatorsHelper<short>.op_CheckedIncrement(unchecked((short)0xFFFF)));

            Assert.Throws<OverflowException>(() => IncrementOperatorsHelper<short>.op_CheckedIncrement((short)0x7FFF));
        }

        [Fact]
        public static void op_ModulusTest()
        {
            Assert.Equal((short)0x0000, ModulusOperatorsHelper<short, short, short>.op_Modulus((short)0x0000, (short)2));
            Assert.Equal((short)0x0001, ModulusOperatorsHelper<short, short, short>.op_Modulus((short)0x0001, (short)2));
            Assert.Equal((short)0x0001, ModulusOperatorsHelper<short, short, short>.op_Modulus((short)0x7FFF, (short)2));
            Assert.Equal((short)0x0000, ModulusOperatorsHelper<short, short, short>.op_Modulus(unchecked((short)0x8000), (short)2));
            Assert.Equal(unchecked((short)0xFFFF), ModulusOperatorsHelper<short, short, short>.op_Modulus(unchecked((short)0xFFFF), (short)2));

            Assert.Throws<DivideByZeroException>(() => ModulusOperatorsHelper<short, short, short>.op_Modulus((short)0x0001, (short)0));
        }

        [Fact]
        public static void op_MultiplyTest()
        {
            Assert.Equal((short)0x0000, MultiplyOperatorsHelper<short, short, short>.op_Multiply((short)0x0000, (short)2));
            Assert.Equal((short)0x0002, MultiplyOperatorsHelper<short, short, short>.op_Multiply((short)0x0001, (short)2));
            Assert.Equal(unchecked((short)0xFFFE), MultiplyOperatorsHelper<short, short, short>.op_Multiply((short)0x7FFF, (short)2));
            Assert.Equal((short)0x0000, MultiplyOperatorsHelper<short, short, short>.op_Multiply(unchecked((short)0x8000), (short)2));
            Assert.Equal(unchecked((short)0xFFFE), MultiplyOperatorsHelper<short, short, short>.op_Multiply(unchecked((short)0xFFFF), (short)2));
        }

        [Fact]
        public static void op_CheckedMultiplyTest()
        {
            Assert.Equal((short)0x0000, MultiplyOperatorsHelper<short, short, short>.op_CheckedMultiply((short)0x0000, (short)2));
            Assert.Equal((short)0x0002, MultiplyOperatorsHelper<short, short, short>.op_CheckedMultiply((short)0x0001, (short)2));
            Assert.Equal(unchecked((short)0xFFFE), MultiplyOperatorsHelper<short, short, short>.op_CheckedMultiply(unchecked((short)0xFFFF), (short)2));

            Assert.Throws<OverflowException>(() => MultiplyOperatorsHelper<short, short, short>.op_CheckedMultiply((short)0x7FFF, (short)2));
            Assert.Throws<OverflowException>(() => MultiplyOperatorsHelper<short, short, short>.op_CheckedMultiply(unchecked((short)0x8000), (short)2));
        }

        [Fact]
        public static void AbsTest()
        {
            Assert.Equal((short)0x0000, NumberHelper<short>.Abs((short)0x0000));
            Assert.Equal((short)0x0001, NumberHelper<short>.Abs((short)0x0001));
            Assert.Equal((short)0x7FFF, NumberHelper<short>.Abs((short)0x7FFF));
            Assert.Throws<OverflowException>(() => NumberHelper<short>.Abs(unchecked((short)0x8000)));
            Assert.Equal((short)0x0001, NumberHelper<short>.Abs(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void ClampTest()
        {
            Assert.Equal((short)0x0000, NumberHelper<short>.Clamp((short)0x0000, unchecked((short)0xFFC0), (short)0x003F));
            Assert.Equal((short)0x0001, NumberHelper<short>.Clamp((short)0x0001, unchecked((short)0xFFC0), (short)0x003F));
            Assert.Equal((short)0x003F, NumberHelper<short>.Clamp((short)0x7FFF, unchecked((short)0xFFC0), (short)0x003F));
            Assert.Equal(unchecked((short)0xFFC0), NumberHelper<short>.Clamp(unchecked((short)0x8000), unchecked((short)0xFFC0), (short)0x003F));
            Assert.Equal(unchecked((short)0xFFFF), NumberHelper<short>.Clamp(unchecked((short)0xFFFF), unchecked((short)0xFFC0), (short)0x003F));
        }

        [Fact]
        public static void CreateCheckedFromByteTest()
        {
            Assert.Equal((short)0x0000, NumberHelper<short>.CreateChecked<byte>(0x00));
            Assert.Equal((short)0x0001, NumberHelper<short>.CreateChecked<byte>(0x01));
            Assert.Equal((short)0x007F, NumberHelper<short>.CreateChecked<byte>(0x7F));
            Assert.Equal((short)0x0080, NumberHelper<short>.CreateChecked<byte>(0x80));
            Assert.Equal((short)0x00FF, NumberHelper<short>.CreateChecked<byte>(0xFF));
        }

        [Fact]
        public static void CreateCheckedFromCharTest()
        {
            Assert.Equal((short)0x0000, NumberHelper<short>.CreateChecked<char>((char)0x0000));
            Assert.Equal((short)0x0001, NumberHelper<short>.CreateChecked<char>((char)0x0001));
            Assert.Equal((short)0x7FFF, NumberHelper<short>.CreateChecked<char>((char)0x7FFF));
            Assert.Throws<OverflowException>(() => NumberHelper<short>.CreateChecked<char>((char)0x8000));
            Assert.Throws<OverflowException>(() => NumberHelper<short>.CreateChecked<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateCheckedFromInt16Test()
        {
            Assert.Equal((short)0x0000, NumberHelper<short>.CreateChecked<short>(0x0000));
            Assert.Equal((short)0x0001, NumberHelper<short>.CreateChecked<short>(0x0001));
            Assert.Equal((short)0x7FFF, NumberHelper<short>.CreateChecked<short>(0x7FFF));
            Assert.Equal(unchecked((short)0x8000), NumberHelper<short>.CreateChecked<short>(unchecked((short)0x8000)));
            Assert.Equal(unchecked((short)0xFFFF), NumberHelper<short>.CreateChecked<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromInt32Test()
        {
            Assert.Equal((short)0x0000, NumberHelper<short>.CreateChecked<int>(0x00000000));
            Assert.Equal((short)0x0001, NumberHelper<short>.CreateChecked<int>(0x00000001));
            Assert.Throws<OverflowException>(() => NumberHelper<short>.CreateChecked<int>(0x7FFFFFFF));
            Assert.Throws<OverflowException>(() => NumberHelper<short>.CreateChecked<int>(unchecked((int)0x80000000)));
            Assert.Equal(unchecked((short)0xFFFF), NumberHelper<short>.CreateChecked<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromInt64Test()
        {
            Assert.Equal((short)0x0000, NumberHelper<short>.CreateChecked<long>(0x0000000000000000));
            Assert.Equal((short)0x0001, NumberHelper<short>.CreateChecked<long>(0x0000000000000001));
            Assert.Throws<OverflowException>(() => NumberHelper<short>.CreateChecked<long>(0x7FFFFFFFFFFFFFFF));
            Assert.Throws<OverflowException>(() => NumberHelper<short>.CreateChecked<long>(unchecked((long)0x8000000000000000)));
            Assert.Equal(unchecked((short)0xFFFF), NumberHelper<short>.CreateChecked<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((short)0x0000, NumberHelper<short>.CreateChecked<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal((short)0x0001, NumberHelper<short>.CreateChecked<nint>(unchecked((nint)0x0000000000000001)));
                Assert.Throws<OverflowException>(() => NumberHelper<short>.CreateChecked<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Throws<OverflowException>(() => NumberHelper<short>.CreateChecked<nint>(unchecked((nint)0x8000000000000000)));
                Assert.Equal(unchecked((short)0xFFFF), NumberHelper<short>.CreateChecked<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((short)0x0000, NumberHelper<short>.CreateChecked<nint>((nint)0x00000000));
                Assert.Equal((short)0x0001, NumberHelper<short>.CreateChecked<nint>((nint)0x00000001));
                Assert.Throws<OverflowException>(() => NumberHelper<short>.CreateChecked<nint>((nint)0x7FFFFFFF));
                Assert.Throws<OverflowException>(() => NumberHelper<short>.CreateChecked<nint>(unchecked((nint)0x80000000)));
                Assert.Equal(unchecked((short)0xFFFF), NumberHelper<short>.CreateChecked<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateCheckedFromSByteTest()
        {
            Assert.Equal((short)0x0000, NumberHelper<short>.CreateChecked<sbyte>(0x00));
            Assert.Equal((short)0x0001, NumberHelper<short>.CreateChecked<sbyte>(0x01));
            Assert.Equal((short)0x007F, NumberHelper<short>.CreateChecked<sbyte>(0x7F));
            Assert.Equal(unchecked((short)0xFF80), NumberHelper<short>.CreateChecked<sbyte>(unchecked((sbyte)0x80)));
            Assert.Equal(unchecked((short)0xFFFF), NumberHelper<short>.CreateChecked<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateCheckedFromUInt16Test()
        {
            Assert.Equal((short)0x0000, NumberHelper<short>.CreateChecked<ushort>(0x0000));
            Assert.Equal((short)0x0001, NumberHelper<short>.CreateChecked<ushort>(0x0001));
            Assert.Equal((short)0x7FFF, NumberHelper<short>.CreateChecked<ushort>(0x7FFF));
            Assert.Throws<OverflowException>(() => NumberHelper<short>.CreateChecked<ushort>(0x8000));
            Assert.Throws<OverflowException>(() => NumberHelper<short>.CreateChecked<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUInt32Test()
        {
            Assert.Equal((short)0x0000, NumberHelper<short>.CreateChecked<uint>(0x00000000));
            Assert.Equal((short)0x0001, NumberHelper<short>.CreateChecked<uint>(0x00000001));
            Assert.Throws<OverflowException>(() => NumberHelper<short>.CreateChecked<uint>(0x7FFFFFFF));
            Assert.Throws<OverflowException>(() => NumberHelper<short>.CreateChecked<uint>(0x80000000));
            Assert.Throws<OverflowException>(() => NumberHelper<short>.CreateChecked<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUInt64Test()
        {
            Assert.Equal((short)0x0000, NumberHelper<short>.CreateChecked<ulong>(0x0000000000000000));
            Assert.Equal((short)0x0001, NumberHelper<short>.CreateChecked<ulong>(0x0000000000000001));
            Assert.Throws<OverflowException>(() => NumberHelper<short>.CreateChecked<ulong>(0x7FFFFFFFFFFFFFFF));
            Assert.Throws<OverflowException>(() => NumberHelper<short>.CreateChecked<ulong>(0x8000000000000000));
            Assert.Throws<OverflowException>(() => NumberHelper<short>.CreateChecked<ulong>(0xFFFFFFFFFFFFFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((short)0x0000, NumberHelper<short>.CreateChecked<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal((short)0x0001, NumberHelper<short>.CreateChecked<nuint>(unchecked((nuint)0x0000000000000001)));
                Assert.Throws<OverflowException>(() => NumberHelper<short>.CreateChecked<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Throws<OverflowException>(() => NumberHelper<short>.CreateChecked<nuint>(unchecked((nuint)0x8000000000000000)));
                Assert.Throws<OverflowException>(() => NumberHelper<short>.CreateChecked<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((short)0x0000, NumberHelper<short>.CreateChecked<nuint>((nuint)0x00000000));
                Assert.Equal((short)0x0001, NumberHelper<short>.CreateChecked<nuint>((nuint)0x00000001));
                Assert.Throws<OverflowException>(() => NumberHelper<short>.CreateChecked<nuint>((nuint)0x7FFFFFFF));
                Assert.Throws<OverflowException>(() => NumberHelper<short>.CreateChecked<nuint>((nuint)0x80000000));
                Assert.Throws<OverflowException>(() => NumberHelper<short>.CreateChecked<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateSaturatingFromByteTest()
        {
            Assert.Equal((short)0x0000, NumberHelper<short>.CreateSaturating<byte>(0x00));
            Assert.Equal((short)0x0001, NumberHelper<short>.CreateSaturating<byte>(0x01));
            Assert.Equal((short)0x007F, NumberHelper<short>.CreateSaturating<byte>(0x7F));
            Assert.Equal((short)0x0080, NumberHelper<short>.CreateSaturating<byte>(0x80));
            Assert.Equal((short)0x00FF, NumberHelper<short>.CreateSaturating<byte>(0xFF));
        }

        [Fact]
        public static void CreateSaturatingFromCharTest()
        {
            Assert.Equal((short)0x0000, NumberHelper<short>.CreateSaturating<char>((char)0x0000));
            Assert.Equal((short)0x0001, NumberHelper<short>.CreateSaturating<char>((char)0x0001));
            Assert.Equal((short)0x7FFF, NumberHelper<short>.CreateSaturating<char>((char)0x7FFF));
            Assert.Equal((short)0x7FFF, NumberHelper<short>.CreateSaturating<char>((char)0x8000));
            Assert.Equal((short)0x7FFF, NumberHelper<short>.CreateSaturating<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromInt16Test()
        {
            Assert.Equal((short)0x0000, NumberHelper<short>.CreateSaturating<short>(0x0000));
            Assert.Equal((short)0x0001, NumberHelper<short>.CreateSaturating<short>(0x0001));
            Assert.Equal((short)0x7FFF, NumberHelper<short>.CreateSaturating<short>(0x7FFF));
            Assert.Equal(unchecked((short)0x8000), NumberHelper<short>.CreateSaturating<short>(unchecked((short)0x8000)));
            Assert.Equal(unchecked((short)0xFFFF), NumberHelper<short>.CreateSaturating<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromInt32Test()
        {
            Assert.Equal((short)0x0000, NumberHelper<short>.CreateSaturating<int>(0x00000000));
            Assert.Equal((short)0x0001, NumberHelper<short>.CreateSaturating<int>(0x00000001));
            Assert.Equal((short)0x7FFF, NumberHelper<short>.CreateSaturating<int>(0x7FFFFFFF));
            Assert.Equal(unchecked((short)0x8000), NumberHelper<short>.CreateSaturating<int>(unchecked((int)0x80000000)));
            Assert.Equal(unchecked((short)0xFFFF), NumberHelper<short>.CreateSaturating<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromInt64Test()
        {
            Assert.Equal((short)0x0000, NumberHelper<short>.CreateSaturating<long>(0x0000000000000000));
            Assert.Equal((short)0x0001, NumberHelper<short>.CreateSaturating<long>(0x0000000000000001));
            Assert.Equal((short)0x7FFF, NumberHelper<short>.CreateSaturating<long>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal(unchecked((short)0x8000), NumberHelper<short>.CreateSaturating<long>(unchecked((long)0x8000000000000000)));
            Assert.Equal(unchecked((short)0xFFFF), NumberHelper<short>.CreateSaturating<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((short)0x0000, NumberHelper<short>.CreateSaturating<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal((short)0x0001, NumberHelper<short>.CreateSaturating<nint>(unchecked((nint)0x0000000000000001)));
                Assert.Equal((short)0x7FFF, NumberHelper<short>.CreateSaturating<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((short)0x8000), NumberHelper<short>.CreateSaturating<nint>(unchecked((nint)0x8000000000000000)));
                Assert.Equal(unchecked((short)0xFFFF), NumberHelper<short>.CreateSaturating<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((short)0x0000, NumberHelper<short>.CreateSaturating<nint>((nint)0x00000000));
                Assert.Equal((short)0x0001, NumberHelper<short>.CreateSaturating<nint>((nint)0x00000001));
                Assert.Equal((short)0x7FFF, NumberHelper<short>.CreateSaturating<nint>((nint)0x7FFFFFFF));
                Assert.Equal(unchecked((short)0x8000), NumberHelper<short>.CreateSaturating<nint>(unchecked((nint)0x80000000)));
                Assert.Equal(unchecked((short)0xFFFF), NumberHelper<short>.CreateSaturating<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateSaturatingFromSByteTest()
        {
            Assert.Equal((short)0x0000, NumberHelper<short>.CreateSaturating<sbyte>(0x00));
            Assert.Equal((short)0x0001, NumberHelper<short>.CreateSaturating<sbyte>(0x01));
            Assert.Equal((short)0x007F, NumberHelper<short>.CreateSaturating<sbyte>(0x7F));
            Assert.Equal(unchecked((short)0xFF80), NumberHelper<short>.CreateSaturating<sbyte>(unchecked((sbyte)0x80)));
            Assert.Equal(unchecked((short)0xFFFF), NumberHelper<short>.CreateSaturating<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateSaturatingFromUInt16Test()
        {
            Assert.Equal((short)0x0000, NumberHelper<short>.CreateSaturating<ushort>(0x0000));
            Assert.Equal((short)0x0001, NumberHelper<short>.CreateSaturating<ushort>(0x0001));
            Assert.Equal((short)0x7FFF, NumberHelper<short>.CreateSaturating<ushort>(0x7FFF));
            Assert.Equal((short)0x7FFF, NumberHelper<short>.CreateSaturating<ushort>(0x8000));
            Assert.Equal((short)0x7FFF, NumberHelper<short>.CreateSaturating<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUInt32Test()
        {
            Assert.Equal((short)0x0000, NumberHelper<short>.CreateSaturating<uint>(0x00000000));
            Assert.Equal((short)0x0001, NumberHelper<short>.CreateSaturating<uint>(0x00000001));
            Assert.Equal((short)0x7FFF, NumberHelper<short>.CreateSaturating<uint>(0x7FFFFFFF));
            Assert.Equal((short)0x7FFF, NumberHelper<short>.CreateSaturating<uint>(0x80000000));
            Assert.Equal((short)0x7FFF, NumberHelper<short>.CreateSaturating<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUInt64Test()
        {
            Assert.Equal((short)0x0000, NumberHelper<short>.CreateSaturating<ulong>(0x0000000000000000));
            Assert.Equal((short)0x0001, NumberHelper<short>.CreateSaturating<ulong>(0x0000000000000001));
            Assert.Equal((short)0x7FFF, NumberHelper<short>.CreateSaturating<ulong>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal((short)0x7FFF, NumberHelper<short>.CreateSaturating<ulong>(0x8000000000000000));
            Assert.Equal((short)0x7FFF, NumberHelper<short>.CreateSaturating<ulong>(0xFFFFFFFFFFFFFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((short)0x0000, NumberHelper<short>.CreateSaturating<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal((short)0x0001, NumberHelper<short>.CreateSaturating<nuint>(unchecked((nuint)0x0000000000000001)));
                Assert.Equal((short)0x7FFF, NumberHelper<short>.CreateSaturating<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal((short)0x7FFF, NumberHelper<short>.CreateSaturating<nuint>(unchecked((nuint)0x8000000000000000)));
                Assert.Equal((short)0x7FFF, NumberHelper<short>.CreateSaturating<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((short)0x0000, NumberHelper<short>.CreateSaturating<nuint>((nuint)0x00000000));
                Assert.Equal((short)0x0001, NumberHelper<short>.CreateSaturating<nuint>((nuint)0x00000001));
                Assert.Equal((short)0x7FFF, NumberHelper<short>.CreateSaturating<nuint>((nuint)0x7FFFFFFF));
                Assert.Equal((short)0x7FFF, NumberHelper<short>.CreateSaturating<nuint>((nuint)0x80000000));
                Assert.Equal((short)0x7FFF, NumberHelper<short>.CreateSaturating<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateTruncatingFromByteTest()
        {
            Assert.Equal((short)0x0000, NumberHelper<short>.CreateTruncating<byte>(0x00));
            Assert.Equal((short)0x0001, NumberHelper<short>.CreateTruncating<byte>(0x01));
            Assert.Equal((short)0x007F, NumberHelper<short>.CreateTruncating<byte>(0x7F));
            Assert.Equal((short)0x0080, NumberHelper<short>.CreateTruncating<byte>(0x80));
            Assert.Equal((short)0x00FF, NumberHelper<short>.CreateTruncating<byte>(0xFF));
        }

        [Fact]
        public static void CreateTruncatingFromCharTest()
        {
            Assert.Equal((short)0x0000, NumberHelper<short>.CreateTruncating<char>((char)0x0000));
            Assert.Equal((short)0x0001, NumberHelper<short>.CreateTruncating<char>((char)0x0001));
            Assert.Equal((short)0x7FFF, NumberHelper<short>.CreateTruncating<char>((char)0x7FFF));
            Assert.Equal(unchecked((short)0x8000), NumberHelper<short>.CreateTruncating<char>((char)0x8000));
            Assert.Equal(unchecked((short)0xFFFF), NumberHelper<short>.CreateTruncating<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromInt16Test()
        {
            Assert.Equal((short)0x0000, NumberHelper<short>.CreateTruncating<short>(0x0000));
            Assert.Equal((short)0x0001, NumberHelper<short>.CreateTruncating<short>(0x0001));
            Assert.Equal((short)0x7FFF, NumberHelper<short>.CreateTruncating<short>(0x7FFF));
            Assert.Equal(unchecked((short)0x8000), NumberHelper<short>.CreateTruncating<short>(unchecked((short)0x8000)));
            Assert.Equal(unchecked((short)0xFFFF), NumberHelper<short>.CreateTruncating<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateTruncatingFromInt32Test()
        {
            Assert.Equal((short)0x0000, NumberHelper<short>.CreateTruncating<int>(0x00000000));
            Assert.Equal((short)0x0001, NumberHelper<short>.CreateTruncating<int>(0x00000001));
            Assert.Equal(unchecked((short)0xFFFF), NumberHelper<short>.CreateTruncating<int>(0x7FFFFFFF));
            Assert.Equal((short)0x0000, NumberHelper<short>.CreateTruncating<int>(unchecked((int)0x80000000)));
            Assert.Equal(unchecked((short)0xFFFF), NumberHelper<short>.CreateTruncating<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateTruncatingFromInt64Test()
        {
            Assert.Equal((short)0x0000, NumberHelper<short>.CreateTruncating<long>(0x0000000000000000));
            Assert.Equal((short)0x0001, NumberHelper<short>.CreateTruncating<long>(0x0000000000000001));
            Assert.Equal(unchecked((short)0xFFFF), NumberHelper<short>.CreateTruncating<long>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal((short)0x0000, NumberHelper<short>.CreateTruncating<long>(unchecked((long)0x8000000000000000)));
            Assert.Equal(unchecked((short)0xFFFF), NumberHelper<short>.CreateTruncating<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void CreateTruncatingFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((short)0x0000, NumberHelper<short>.CreateTruncating<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal((short)0x0001, NumberHelper<short>.CreateTruncating<nint>(unchecked((nint)0x0000000000000001)));
                Assert.Equal(unchecked((short)0xFFFF), NumberHelper<short>.CreateTruncating<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal((short)0x0000, NumberHelper<short>.CreateTruncating<nint>(unchecked((nint)0x8000000000000000)));
                Assert.Equal(unchecked((short)0xFFFF), NumberHelper<short>.CreateTruncating<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((short)0x0000, NumberHelper<short>.CreateTruncating<nint>((nint)0x00000000));
                Assert.Equal((short)0x0001, NumberHelper<short>.CreateTruncating<nint>((nint)0x00000001));
                Assert.Equal(unchecked((short)0xFFFF), NumberHelper<short>.CreateTruncating<nint>((nint)0x7FFFFFFF));
                Assert.Equal((short)0x0000, NumberHelper<short>.CreateTruncating<nint>(unchecked((nint)0x80000000)));
                Assert.Equal(unchecked((short)0xFFFF), NumberHelper<short>.CreateTruncating<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateTruncatingFromSByteTest()
        {
            Assert.Equal((short)0x0000, NumberHelper<short>.CreateTruncating<sbyte>(0x00));
            Assert.Equal((short)0x0001, NumberHelper<short>.CreateTruncating<sbyte>(0x01));
            Assert.Equal((short)0x007F, NumberHelper<short>.CreateTruncating<sbyte>(0x7F));
            Assert.Equal(unchecked((short)0xFF80), NumberHelper<short>.CreateTruncating<sbyte>(unchecked((sbyte)0x80)));
            Assert.Equal(unchecked((short)0xFFFF), NumberHelper<short>.CreateTruncating<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateTruncatingFromUInt16Test()
        {
            Assert.Equal((short)0x0000, NumberHelper<short>.CreateTruncating<ushort>(0x0000));
            Assert.Equal((short)0x0001, NumberHelper<short>.CreateTruncating<ushort>(0x0001));
            Assert.Equal((short)0x7FFF, NumberHelper<short>.CreateTruncating<ushort>(0x7FFF));
            Assert.Equal(unchecked((short)0x8000), NumberHelper<short>.CreateTruncating<ushort>(0x8000));
            Assert.Equal(unchecked((short)0xFFFF), NumberHelper<short>.CreateTruncating<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt32Test()
        {
            Assert.Equal((short)0x0000, NumberHelper<short>.CreateTruncating<uint>(0x00000000));
            Assert.Equal((short)0x0001, NumberHelper<short>.CreateTruncating<uint>(0x00000001));
            Assert.Equal(unchecked((short)0xFFFF), NumberHelper<short>.CreateTruncating<uint>(0x7FFFFFFF));
            Assert.Equal((short)0x0000, NumberHelper<short>.CreateTruncating<uint>(0x80000000));
            Assert.Equal(unchecked((short)0xFFFF), NumberHelper<short>.CreateTruncating<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt64Test()
        {
            Assert.Equal((short)0x0000, NumberHelper<short>.CreateTruncating<ulong>(0x0000000000000000));
            Assert.Equal((short)0x0001, NumberHelper<short>.CreateTruncating<ulong>(0x0000000000000001));
            Assert.Equal(unchecked((short)0xFFFF), NumberHelper<short>.CreateTruncating<ulong>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal((short)0x0000, NumberHelper<short>.CreateTruncating<ulong>(0x8000000000000000));
            Assert.Equal(unchecked((short)0xFFFF), NumberHelper<short>.CreateTruncating<ulong>(0xFFFFFFFFFFFFFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((short)0x0000, NumberHelper<short>.CreateTruncating<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal((short)0x0001, NumberHelper<short>.CreateTruncating<nuint>(unchecked((nuint)0x0000000000000001)));
                Assert.Equal(unchecked((short)0xFFFF), NumberHelper<short>.CreateTruncating<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal((short)0x0000, NumberHelper<short>.CreateTruncating<nuint>(unchecked((nuint)0x8000000000000000)));
                Assert.Equal(unchecked((short)0xFFFF), NumberHelper<short>.CreateTruncating<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((short)0x0000, NumberHelper<short>.CreateTruncating<nuint>((nuint)0x00000000));
                Assert.Equal((short)0x0001, NumberHelper<short>.CreateTruncating<nuint>((nuint)0x00000001));
                Assert.Equal(unchecked((short)0xFFFF), NumberHelper<short>.CreateTruncating<nuint>((nuint)0x7FFFFFFF));
                Assert.Equal((short)0x0000, NumberHelper<short>.CreateTruncating<nuint>((nuint)0x80000000));
                Assert.Equal(unchecked((short)0xFFFF), NumberHelper<short>.CreateTruncating<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void DivRemTest()
        {
            Assert.Equal(((short)0x0000, (short)0x0000), BinaryIntegerHelper<short>.DivRem((short)0x0000, (short)2));
            Assert.Equal(((short)0x0000, (short)0x0001), BinaryIntegerHelper<short>.DivRem((short)0x0001, (short)2));
            Assert.Equal(((short)0x3FFF, (short)0x0001), BinaryIntegerHelper<short>.DivRem((short)0x7FFF, (short)2));
            Assert.Equal((unchecked((short)0xC000), (short)0x0000), BinaryIntegerHelper<short>.DivRem(unchecked((short)0x8000), (short)2));
            Assert.Equal(((short)0x0000, unchecked((short)0xFFFF)), BinaryIntegerHelper<short>.DivRem(unchecked((short)0xFFFF), (short)2));
        }

        [Fact]
        public static void MaxTest()
        {
            Assert.Equal((short)0x0001, NumberHelper<short>.Max((short)0x0000, (short)1));
            Assert.Equal((short)0x0001, NumberHelper<short>.Max((short)0x0001, (short)1));
            Assert.Equal((short)0x7FFF, NumberHelper<short>.Max((short)0x7FFF, (short)1));
            Assert.Equal((short)0x0001, NumberHelper<short>.Max(unchecked((short)0x8000), (short)1));
            Assert.Equal((short)0x0001, NumberHelper<short>.Max(unchecked((short)0xFFFF), (short)1));
        }

        [Fact]
        public static void MinTest()
        {
            Assert.Equal((short)0x0000, NumberHelper<short>.Min((short)0x0000, (short)1));
            Assert.Equal((short)0x0001, NumberHelper<short>.Min((short)0x0001, (short)1));
            Assert.Equal((short)0x0001, NumberHelper<short>.Min((short)0x7FFF, (short)1));
            Assert.Equal(unchecked((short)0x8000), NumberHelper<short>.Min(unchecked((short)0x8000), (short)1));
            Assert.Equal(unchecked((short)0xFFFF), NumberHelper<short>.Min(unchecked((short)0xFFFF), (short)1));
        }

        [Fact]
        public static void SignTest()
        {
            Assert.Equal(0, NumberHelper<short>.Sign((short)0x0000));
            Assert.Equal(1, NumberHelper<short>.Sign((short)0x0001));
            Assert.Equal(1, NumberHelper<short>.Sign((short)0x7FFF));
            Assert.Equal(-1, NumberHelper<short>.Sign(unchecked((short)0x8000)));
            Assert.Equal(-1, NumberHelper<short>.Sign(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void TryCreateFromByteTest()
        {
            short result;

            Assert.True(NumberHelper<short>.TryCreate<byte>(0x00, out result));
            Assert.Equal((short)0x0000, result);

            Assert.True(NumberHelper<short>.TryCreate<byte>(0x01, out result));
            Assert.Equal((short)0x0001, result);

            Assert.True(NumberHelper<short>.TryCreate<byte>(0x7F, out result));
            Assert.Equal((short)0x007F, result);

            Assert.True(NumberHelper<short>.TryCreate<byte>(0x80, out result));
            Assert.Equal((short)0x0080, result);

            Assert.True(NumberHelper<short>.TryCreate<byte>(0xFF, out result));
            Assert.Equal((short)0x00FF, result);
        }

        [Fact]
        public static void TryCreateFromCharTest()
        {
            short result;

            Assert.True(NumberHelper<short>.TryCreate<char>((char)0x0000, out result));
            Assert.Equal((short)0x0000, result);

            Assert.True(NumberHelper<short>.TryCreate<char>((char)0x0001, out result));
            Assert.Equal((short)0x0001, result);

            Assert.True(NumberHelper<short>.TryCreate<char>((char)0x7FFF, out result));
            Assert.Equal((short)0x7FFF, result);

            Assert.False(NumberHelper<short>.TryCreate<char>((char)0x8000, out result));
            Assert.Equal((short)0x0000, result);

            Assert.False(NumberHelper<short>.TryCreate<char>((char)0xFFFF, out result));
            Assert.Equal((short)0x0000, result);
        }

        [Fact]
        public static void TryCreateFromInt16Test()
        {
            short result;

            Assert.True(NumberHelper<short>.TryCreate<short>(0x0000, out result));
            Assert.Equal((short)0x0000, result);

            Assert.True(NumberHelper<short>.TryCreate<short>(0x0001, out result));
            Assert.Equal((short)0x0001, result);

            Assert.True(NumberHelper<short>.TryCreate<short>(0x7FFF, out result));
            Assert.Equal((short)0x7FFF, result);

            Assert.True(NumberHelper<short>.TryCreate<short>(unchecked((short)0x8000), out result));
            Assert.Equal(unchecked((short)0x8000), result);

            Assert.True(NumberHelper<short>.TryCreate<short>(unchecked((short)0xFFFF), out result));
            Assert.Equal(unchecked((short)0xFFFF), result);
        }

        [Fact]
        public static void TryCreateFromInt32Test()
        {
            short result;

            Assert.True(NumberHelper<short>.TryCreate<int>(0x00000000, out result));
            Assert.Equal((short)0x0000, result);

            Assert.True(NumberHelper<short>.TryCreate<int>(0x00000001, out result));
            Assert.Equal((short)0x0001, result);

            Assert.False(NumberHelper<short>.TryCreate<int>(0x7FFFFFFF, out result));
            Assert.Equal((short)0x0000, result);

            Assert.False(NumberHelper<short>.TryCreate<int>(unchecked((int)0x80000000), out result));
            Assert.Equal((short)0x0000, result);

            Assert.True(NumberHelper<short>.TryCreate<int>(unchecked((int)0xFFFFFFFF), out result));
            Assert.Equal(unchecked((short)0xFFFF), result);
        }

        [Fact]
        public static void TryCreateFromInt64Test()
        {
            short result;

            Assert.True(NumberHelper<short>.TryCreate<long>(0x0000000000000000, out result));
            Assert.Equal((short)0x0000, result);

            Assert.True(NumberHelper<short>.TryCreate<long>(0x0000000000000001, out result));
            Assert.Equal((short)0x0001, result);

            Assert.False(NumberHelper<short>.TryCreate<long>(0x7FFFFFFFFFFFFFFF, out result));
            Assert.Equal((short)0x0000, result);

            Assert.False(NumberHelper<short>.TryCreate<long>(unchecked((long)0x8000000000000000), out result));
            Assert.Equal((short)0x0000, result);

            Assert.True(NumberHelper<short>.TryCreate<long>(unchecked((long)0xFFFFFFFFFFFFFFFF), out result));
            Assert.Equal(unchecked((short)0xFFFF), result);
        }

        [Fact]
        public static void TryCreateFromIntPtrTest()
        {
            short result;

            if (Environment.Is64BitProcess)
            {
                Assert.True(NumberHelper<short>.TryCreate<nint>(unchecked((nint)0x0000000000000000), out result));
                Assert.Equal((short)0x0000, result);

                Assert.True(NumberHelper<short>.TryCreate<nint>(unchecked((nint)0x0000000000000001), out result));
                Assert.Equal((short)0x0001, result);

                Assert.False(NumberHelper<short>.TryCreate<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF), out result));
                Assert.Equal((short)0x0000, result);

                Assert.False(NumberHelper<short>.TryCreate<nint>(unchecked((nint)0x8000000000000000), out result));
                Assert.Equal((short)0x0000, result);

                Assert.True(NumberHelper<short>.TryCreate<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF), out result));
                Assert.Equal(unchecked((short)0xFFFF), result);
            }
            else
            {
                Assert.True(NumberHelper<short>.TryCreate<nint>((nint)0x00000000, out result));
                Assert.Equal((short)0x0000, result);

                Assert.True(NumberHelper<short>.TryCreate<nint>((nint)0x00000001, out result));
                Assert.Equal((short)0x0001, result);

                Assert.False(NumberHelper<short>.TryCreate<nint>((nint)0x7FFFFFFF, out result));
                Assert.Equal((short)0x0000, result);

                Assert.False(NumberHelper<short>.TryCreate<nint>(unchecked((nint)0x80000000), out result));
                Assert.Equal((short)0x0000, result);

                Assert.True(NumberHelper<short>.TryCreate<nint>(unchecked((nint)0xFFFFFFFF), out result));
                Assert.Equal(unchecked((short)0xFFFF), result);
            }
        }

        [Fact]
        public static void TryCreateFromSByteTest()
        {
            short result;

            Assert.True(NumberHelper<short>.TryCreate<sbyte>(0x00, out result));
            Assert.Equal((short)0x0000, result);

            Assert.True(NumberHelper<short>.TryCreate<sbyte>(0x01, out result));
            Assert.Equal((short)0x0001, result);

            Assert.True(NumberHelper<short>.TryCreate<sbyte>(0x7F, out result));
            Assert.Equal((short)0x007F, result);

            Assert.True(NumberHelper<short>.TryCreate<sbyte>(unchecked((sbyte)0x80), out result));
            Assert.Equal(unchecked((short)0xFF80), result);

            Assert.True(NumberHelper<short>.TryCreate<sbyte>(unchecked((sbyte)0xFF), out result));
            Assert.Equal(unchecked((short)0xFFFF), result);
        }

        [Fact]
        public static void TryCreateFromUInt16Test()
        {
            short result;

            Assert.True(NumberHelper<short>.TryCreate<ushort>(0x0000, out result));
            Assert.Equal((short)0x0000, result);

            Assert.True(NumberHelper<short>.TryCreate<ushort>(0x0001, out result));
            Assert.Equal((short)0x0001, result);

            Assert.True(NumberHelper<short>.TryCreate<ushort>(0x7FFF, out result));
            Assert.Equal((short)0x7FFF, result);

            Assert.False(NumberHelper<short>.TryCreate<ushort>(0x8000, out result));
            Assert.Equal((short)0x0000, result);

            Assert.False(NumberHelper<short>.TryCreate<ushort>(0xFFFF, out result));
            Assert.Equal((short)0x0000, result);
        }

        [Fact]
        public static void TryCreateFromUInt32Test()
        {
            short result;

            Assert.True(NumberHelper<short>.TryCreate<uint>(0x00000000, out result));
            Assert.Equal((short)0x0000, result);

            Assert.True(NumberHelper<short>.TryCreate<uint>(0x00000001, out result));
            Assert.Equal((short)0x0001, result);

            Assert.False(NumberHelper<short>.TryCreate<uint>(0x7FFFFFFF, out result));
            Assert.Equal((short)0x0000, result);

            Assert.False(NumberHelper<short>.TryCreate<uint>(0x80000000, out result));
            Assert.Equal((short)0x0000, result);

            Assert.False(NumberHelper<short>.TryCreate<uint>(0xFFFFFFFF, out result));
            Assert.Equal((short)0x0000, result);
        }

        [Fact]
        public static void TryCreateFromUInt64Test()
        {
            short result;

            Assert.True(NumberHelper<short>.TryCreate<ulong>(0x0000000000000000, out result));
            Assert.Equal((short)0x0000, result);

            Assert.True(NumberHelper<short>.TryCreate<ulong>(0x0000000000000001, out result));
            Assert.Equal((short)0x0001, result);

            Assert.False(NumberHelper<short>.TryCreate<ulong>(0x7FFFFFFFFFFFFFFF, out result));
            Assert.Equal((short)0x0000, result);

            Assert.False(NumberHelper<short>.TryCreate<ulong>(0x8000000000000000, out result));
            Assert.Equal((short)0x0000, result);

            Assert.False(NumberHelper<short>.TryCreate<ulong>(0xFFFFFFFFFFFFFFFF, out result));
            Assert.Equal((short)0x0000, result);
        }

        [Fact]
        public static void TryCreateFromUIntPtrTest()
        {
            short result;

            if (Environment.Is64BitProcess)
            {
                Assert.True(NumberHelper<short>.TryCreate<nuint>(unchecked((nuint)0x0000000000000000), out result));
                Assert.Equal((short)0x0000, result);

                Assert.True(NumberHelper<short>.TryCreate<nuint>(unchecked((nuint)0x0000000000000001), out result));
                Assert.Equal((short)0x0001, result);

                Assert.False(NumberHelper<short>.TryCreate<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF), out result));
                Assert.Equal((short)0x0000, result);

                Assert.False(NumberHelper<short>.TryCreate<nuint>(unchecked((nuint)0x8000000000000000), out result));
                Assert.Equal((short)0x0000, result);

                Assert.False(NumberHelper<short>.TryCreate<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF), out result));
                Assert.Equal((short)0x0000, result);
            }
            else
            {
                Assert.True(NumberHelper<short>.TryCreate<nuint>((nuint)0x00000000, out result));
                Assert.Equal((short)0x0000, result);

                Assert.True(NumberHelper<short>.TryCreate<nuint>((nuint)0x00000001, out result));
                Assert.Equal((short)0x0001, result);

                Assert.False(NumberHelper<short>.TryCreate<nuint>((nuint)0x7FFFFFFF, out result));
                Assert.Equal((short)0x0000, result);

                Assert.False(NumberHelper<short>.TryCreate<nuint>(unchecked((nuint)0x80000000), out result));
                Assert.Equal((short)0x0000, result);

                Assert.False(NumberHelper<short>.TryCreate<nuint>(unchecked((nuint)0xFFFFFFFF), out result));
                Assert.Equal((short)0x0000, result);
            }
        }

        [Fact]
        public static void GetByteCountTest()
        {
            Assert.Equal(2, BinaryIntegerHelper<short>.GetByteCount((short)0x0000));
            Assert.Equal(2, BinaryIntegerHelper<short>.GetByteCount((short)0x0001));
            Assert.Equal(2, BinaryIntegerHelper<short>.GetByteCount((short)0x7FFF));
            Assert.Equal(2, BinaryIntegerHelper<short>.GetByteCount(unchecked((short)0x8000)));
            Assert.Equal(2, BinaryIntegerHelper<short>.GetByteCount(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void TryWriteLittleEndianTest()
        {
            Span<byte> destination = stackalloc byte[2];
            int bytesWritten = 0;

            Assert.True(BinaryIntegerHelper<short>.TryWriteLittleEndian((short)0x0000, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x00 }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<short>.TryWriteLittleEndian((short)0x0001, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0x01, 0x00 }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<short>.TryWriteLittleEndian((short)0x7FFF, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0x7F }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<short>.TryWriteLittleEndian(unchecked((short)0x8000), destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x80 }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<short>.TryWriteLittleEndian(unchecked((short)0xFFFF), destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0xFF }, destination.ToArray());

            Assert.False(BinaryIntegerHelper<short>.TryWriteLittleEndian(default, Span<byte>.Empty, out bytesWritten));
            Assert.Equal(0, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0xFF }, destination.ToArray());
        }

        [Fact]
        public static void op_LeftShiftTest()
        {
            Assert.Equal((short)0x0000, ShiftOperatorsHelper<short, short>.op_LeftShift((short)0x0000, 1));
            Assert.Equal((short)0x0002, ShiftOperatorsHelper<short, short>.op_LeftShift((short)0x0001, 1));
            Assert.Equal(unchecked((short)0xFFFE), ShiftOperatorsHelper<short, short>.op_LeftShift((short)0x7FFF, 1));
            Assert.Equal((short)0x0000, ShiftOperatorsHelper<short, short>.op_LeftShift(unchecked((short)0x8000), 1));
            Assert.Equal(unchecked((short)0xFFFE), ShiftOperatorsHelper<short, short>.op_LeftShift(unchecked((short)0xFFFF), 1));
        }

        [Fact]
        public static void op_RightShiftTest()
        {
            Assert.Equal((short)0x0000, ShiftOperatorsHelper<short, short>.op_RightShift((short)0x0000, 1));
            Assert.Equal((short)0x0000, ShiftOperatorsHelper<short, short>.op_RightShift((short)0x0001, 1));
            Assert.Equal((short)0x3FFF, ShiftOperatorsHelper<short, short>.op_RightShift((short)0x7FFF, 1));
            Assert.Equal(unchecked((short)0xC000), ShiftOperatorsHelper<short, short>.op_RightShift(unchecked((short)0x8000), 1));
            Assert.Equal(unchecked((short)0xFFFF), ShiftOperatorsHelper<short, short>.op_RightShift(unchecked((short)0xFFFF), 1));
        }

        [Fact]
        public static void op_UnsignedRightShiftTest()
        {
            Assert.Equal((short)0x0000, ShiftOperatorsHelper<short, short>.op_UnsignedRightShift((short)0x0000, 1));
            Assert.Equal((short)0x0000, ShiftOperatorsHelper<short, short>.op_UnsignedRightShift((short)0x0001, 1));
            Assert.Equal((short)0x3FFF, ShiftOperatorsHelper<short, short>.op_UnsignedRightShift((short)0x7FFF, 1));
            Assert.Equal((short)0x4000, ShiftOperatorsHelper<short, short>.op_UnsignedRightShift(unchecked((short)0x8000), 1));
            Assert.Equal((short)0x7FFF, ShiftOperatorsHelper<short, short>.op_UnsignedRightShift(unchecked((short)0xFFFF), 1));
        }

        [Fact]
        public static void op_SubtractionTest()
        {
            Assert.Equal(unchecked((short)0xFFFF), SubtractionOperatorsHelper<short, short, short>.op_Subtraction((short)0x0000, (short)1));
            Assert.Equal((short)0x0000, SubtractionOperatorsHelper<short, short, short>.op_Subtraction((short)0x0001, (short)1));
            Assert.Equal((short)0x7FFE, SubtractionOperatorsHelper<short, short, short>.op_Subtraction((short)0x7FFF, (short)1));
            Assert.Equal((short)0x7FFF, SubtractionOperatorsHelper<short, short, short>.op_Subtraction(unchecked((short)0x8000), (short)1));
            Assert.Equal(unchecked((short)0xFFFE), SubtractionOperatorsHelper<short, short, short>.op_Subtraction(unchecked((short)0xFFFF), (short)1));
        }

        [Fact]
        public static void op_CheckedSubtractionTest()
        {
            Assert.Equal(unchecked((short)0xFFFF), SubtractionOperatorsHelper<short, short, short>.op_CheckedSubtraction((short)0x0000, (short)1));
            Assert.Equal((short)0x0000, SubtractionOperatorsHelper<short, short, short>.op_CheckedSubtraction((short)0x0001, (short)1));
            Assert.Equal((short)0x7FFE, SubtractionOperatorsHelper<short, short, short>.op_CheckedSubtraction((short)0x7FFF, (short)1));
            Assert.Equal(unchecked((short)0xFFFE), SubtractionOperatorsHelper<short, short, short>.op_CheckedSubtraction(unchecked((short)0xFFFF), (short)1));

            Assert.Throws<OverflowException>(() => SubtractionOperatorsHelper<short, short, short>.op_CheckedSubtraction(unchecked((short)0x8000), (short)1));
        }

        [Fact]
        public static void op_UnaryNegationTest()
        {
            Assert.Equal((short)0x0000, UnaryNegationOperatorsHelper<short, short>.op_UnaryNegation((short)0x0000));
            Assert.Equal(unchecked((short)0xFFFF), UnaryNegationOperatorsHelper<short, short>.op_UnaryNegation((short)0x0001));
            Assert.Equal(unchecked((short)0x8001), UnaryNegationOperatorsHelper<short, short>.op_UnaryNegation((short)0x7FFF));
            Assert.Equal(unchecked((short)0x8000), UnaryNegationOperatorsHelper<short, short>.op_UnaryNegation(unchecked((short)0x8000)));
            Assert.Equal((short)0x0001, UnaryNegationOperatorsHelper<short, short>.op_UnaryNegation(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void op_CheckedUnaryNegationTest()
        {
            Assert.Equal((short)0x0000, UnaryNegationOperatorsHelper<short, short>.op_CheckedUnaryNegation((short)0x0000));
            Assert.Equal(unchecked((short)0xFFFF), UnaryNegationOperatorsHelper<short, short>.op_CheckedUnaryNegation((short)0x0001));
            Assert.Equal(unchecked((short)0x8001), UnaryNegationOperatorsHelper<short, short>.op_CheckedUnaryNegation((short)0x7FFF));
            Assert.Equal((short)0x0001, UnaryNegationOperatorsHelper<short, short>.op_CheckedUnaryNegation(unchecked((short)0xFFFF)));

            Assert.Throws<OverflowException>(() => UnaryNegationOperatorsHelper<short, short>.op_CheckedUnaryNegation(unchecked((short)0x8000)));
        }

        [Fact]
        public static void op_UnaryPlusTest()
        {
            Assert.Equal((short)0x0000, UnaryPlusOperatorsHelper<short, short>.op_UnaryPlus((short)0x0000));
            Assert.Equal((short)0x0001, UnaryPlusOperatorsHelper<short, short>.op_UnaryPlus((short)0x0001));
            Assert.Equal((short)0x7FFF, UnaryPlusOperatorsHelper<short, short>.op_UnaryPlus((short)0x7FFF));
            Assert.Equal(unchecked((short)0x8000), UnaryPlusOperatorsHelper<short, short>.op_UnaryPlus(unchecked((short)0x8000)));
            Assert.Equal(unchecked((short)0xFFFF), UnaryPlusOperatorsHelper<short, short>.op_UnaryPlus(unchecked((short)0xFFFF)));
        }

        [Theory]
        [MemberData(nameof(Int16Tests.Parse_Valid_TestData), MemberType = typeof(Int16Tests))]
        public static void ParseValidStringTest(string value, NumberStyles style, IFormatProvider provider, short expected)
        {
            short result;

            // Default style and provider
            if ((style == NumberStyles.Integer) && (provider is null))
            {
                Assert.True(ParsableHelper<short>.TryParse(value, provider, out result));
                Assert.Equal(expected, result);
                Assert.Equal(expected, ParsableHelper<short>.Parse(value, provider));
            }

            // Default provider
            if (provider is null)
            {
                Assert.Equal(expected, NumberHelper<short>.Parse(value, style, provider));

                // Substitute default NumberFormatInfo
                Assert.True(NumberHelper<short>.TryParse(value, style, new NumberFormatInfo(), out result));
                Assert.Equal(expected, result);
                Assert.Equal(expected, NumberHelper<short>.Parse(value, style, new NumberFormatInfo()));
            }

            // Default style
            if (style == NumberStyles.Integer)
            {
                Assert.Equal(expected, ParsableHelper<short>.Parse(value, provider));
            }

            // Full overloads
            Assert.True(NumberHelper<short>.TryParse(value, style, provider, out result));
            Assert.Equal(expected, result);
            Assert.Equal(expected, NumberHelper<short>.Parse(value, style, provider));
        }

        [Theory]
        [MemberData(nameof(Int16Tests.Parse_Invalid_TestData), MemberType = typeof(Int16Tests))]
        public static void ParseInvalidStringTest(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            short result;

            // Default style and provider
            if ((style == NumberStyles.Integer) && (provider is null))
            {
                Assert.False(ParsableHelper<short>.TryParse(value, provider, out result));
                Assert.Equal(default(short), result);
                Assert.Throws(exceptionType, () => ParsableHelper<short>.Parse(value, provider));
            }

            // Default provider
            if (provider is null)
            {
                Assert.Throws(exceptionType, () => NumberHelper<short>.Parse(value, style, provider));

                // Substitute default NumberFormatInfo
                Assert.False(NumberHelper<short>.TryParse(value, style, new NumberFormatInfo(), out result));
                Assert.Equal(default(short), result);
                Assert.Throws(exceptionType, () => NumberHelper<short>.Parse(value, style, new NumberFormatInfo()));
            }

            // Default style
            if (style == NumberStyles.Integer)
            {
                Assert.Throws(exceptionType, () => ParsableHelper<short>.Parse(value, provider));
            }

            // Full overloads
            Assert.False(NumberHelper<short>.TryParse(value, style, provider, out result));
            Assert.Equal(default(short), result);
            Assert.Throws(exceptionType, () => NumberHelper<short>.Parse(value, style, provider));
        }

        [Theory]
        [MemberData(nameof(Int16Tests.Parse_ValidWithOffsetCount_TestData), MemberType = typeof(Int16Tests))]
        public static void ParseValidSpanTest(string value, int offset, int count, NumberStyles style, IFormatProvider provider, short expected)
        {
            short result;

            // Default style and provider
            if ((style == NumberStyles.Integer) && (provider is null))
            {
                Assert.True(SpanParsableHelper<short>.TryParse(value.AsSpan(offset, count), provider, out result));
                Assert.Equal(expected, result);
            }

            Assert.Equal(expected, NumberHelper<short>.Parse(value.AsSpan(offset, count), style, provider));

            Assert.True(NumberHelper<short>.TryParse(value.AsSpan(offset, count), style, provider, out result));
            Assert.Equal(expected, result);
        }

        [Theory]
        [MemberData(nameof(Int16Tests.Parse_Invalid_TestData), MemberType = typeof(Int16Tests))]
        public static void ParseInvalidSpanTest(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            if (value is null)
            {
                return;
            }

            short result;

            // Default style and provider
            if ((style == NumberStyles.Integer) && (provider is null))
            {
                Assert.False(SpanParsableHelper<short>.TryParse(value.AsSpan(), provider, out result));
                Assert.Equal(default(short), result);
            }

            Assert.Throws(exceptionType, () => NumberHelper<short>.Parse(value.AsSpan(), style, provider));

            Assert.False(NumberHelper<short>.TryParse(value.AsSpan(), style, provider, out result));
            Assert.Equal(default(short), result);
        }
    }
}
