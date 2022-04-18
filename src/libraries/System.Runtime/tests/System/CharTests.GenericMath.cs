// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Tests
{
    public class CharTests_GenericMath
    {
        [Fact]
        public static void AdditiveIdentityTest()
        {
            Assert.Equal((char)0x0000, AdditiveIdentityHelper<char, char>.AdditiveIdentity);
        }

        [Fact]
        public static void MinValueTest()
        {
            Assert.Equal((char)0x0000, MinMaxValueHelper<char>.MinValue);
        }

        [Fact]
        public static void MaxValueTest()
        {
            Assert.Equal((char)0xFFFF, MinMaxValueHelper<char>.MaxValue);
        }

        [Fact]
        public static void MultiplicativeIdentityTest()
        {
            Assert.Equal((char)0x0001, MultiplicativeIdentityHelper<char, char>.MultiplicativeIdentity);
        }

        [Fact]
        public static void OneTest()
        {
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.One);
        }

        [Fact]
        public static void ZeroTest()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.Zero);
        }

        [Fact]
        public static void op_AdditionTest()
        {
            Assert.Equal((char)0x0001, AdditionOperatorsHelper<char, char, char>.op_Addition((char)0x0000, (char)1));
            Assert.Equal((char)0x0002, AdditionOperatorsHelper<char, char, char>.op_Addition((char)0x0001, (char)1));
            Assert.Equal((char)0x8000, AdditionOperatorsHelper<char, char, char>.op_Addition((char)0x7FFF, (char)1));
            Assert.Equal((char)0x8001, AdditionOperatorsHelper<char, char, char>.op_Addition((char)0x8000, (char)1));
            Assert.Equal((char)0x0000, AdditionOperatorsHelper<char, char, char>.op_Addition((char)0xFFFF, (char)1));
        }

        [Fact]
        public static void op_CheckedAdditionTest()
        {
            Assert.Equal((char)0x0001, AdditionOperatorsHelper<char, char, char>.op_CheckedAddition((char)0x0000, (char)1));
            Assert.Equal((char)0x0002, AdditionOperatorsHelper<char, char, char>.op_CheckedAddition((char)0x0001, (char)1));
            Assert.Equal((char)0x8000, AdditionOperatorsHelper<char, char, char>.op_CheckedAddition((char)0x7FFF, (char)1));
            Assert.Equal((char)0x8001, AdditionOperatorsHelper<char, char, char>.op_CheckedAddition((char)0x8000, (char)1));

            Assert.Throws<OverflowException>(() => AdditionOperatorsHelper<char, char, char>.op_CheckedAddition((char)0xFFFF, (char)1));
        }

        [Fact]
        public static void LeadingZeroCountTest()
        {
            Assert.Equal((char)0x0010, BinaryIntegerHelper<char>.LeadingZeroCount((char)0x0000));
            Assert.Equal((char)0x000F, BinaryIntegerHelper<char>.LeadingZeroCount((char)0x0001));
            Assert.Equal((char)0x0001, BinaryIntegerHelper<char>.LeadingZeroCount((char)0x7FFF));
            Assert.Equal((char)0x0000, BinaryIntegerHelper<char>.LeadingZeroCount((char)0x8000));
            Assert.Equal((char)0x0000, BinaryIntegerHelper<char>.LeadingZeroCount((char)0xFFFF));
        }

        [Fact]
        public static void PopCountTest()
        {
            Assert.Equal((char)0x0000, BinaryIntegerHelper<char>.PopCount((char)0x0000));
            Assert.Equal((char)0x0001, BinaryIntegerHelper<char>.PopCount((char)0x0001));
            Assert.Equal((char)0x000F, BinaryIntegerHelper<char>.PopCount((char)0x7FFF));
            Assert.Equal((char)0x0001, BinaryIntegerHelper<char>.PopCount((char)0x8000));
            Assert.Equal((char)0x0010, BinaryIntegerHelper<char>.PopCount((char)0xFFFF));
        }

        [Fact]
        public static void RotateLeftTest()
        {
            Assert.Equal((char)0x0000, BinaryIntegerHelper<char>.RotateLeft((char)0x0000, 1));
            Assert.Equal((char)0x0002, BinaryIntegerHelper<char>.RotateLeft((char)0x0001, 1));
            Assert.Equal((char)0xFFFE, BinaryIntegerHelper<char>.RotateLeft((char)0x7FFF, 1));
            Assert.Equal((char)0x0001, BinaryIntegerHelper<char>.RotateLeft((char)0x8000, 1));
            Assert.Equal((char)0xFFFF, BinaryIntegerHelper<char>.RotateLeft((char)0xFFFF, 1));
        }

        [Fact]
        public static void RotateRightTest()
        {
            Assert.Equal((char)0x0000, BinaryIntegerHelper<char>.RotateRight((char)0x0000, 1));
            Assert.Equal((char)0x8000, BinaryIntegerHelper<char>.RotateRight((char)0x0001, 1));
            Assert.Equal((char)0xBFFF, BinaryIntegerHelper<char>.RotateRight((char)0x7FFF, 1));
            Assert.Equal((char)0x4000, BinaryIntegerHelper<char>.RotateRight((char)0x8000, 1));
            Assert.Equal((char)0xFFFF, BinaryIntegerHelper<char>.RotateRight((char)0xFFFF, 1));
        }

        [Fact]
        public static void TrailingZeroCountTest()
        {
            Assert.Equal((char)0x0010, BinaryIntegerHelper<char>.TrailingZeroCount((char)0x0000));
            Assert.Equal((char)0x0000, BinaryIntegerHelper<char>.TrailingZeroCount((char)0x0001));
            Assert.Equal((char)0x0000, BinaryIntegerHelper<char>.TrailingZeroCount((char)0x7FFF));
            Assert.Equal((char)0x000F, BinaryIntegerHelper<char>.TrailingZeroCount((char)0x8000));
            Assert.Equal((char)0x0000, BinaryIntegerHelper<char>.TrailingZeroCount((char)0xFFFF));
        }

        [Fact]
        public static void GetShortestBitLengthTest()
        {
            Assert.Equal(0x00, BinaryIntegerHelper<char>.GetShortestBitLength((char)0x0000));
            Assert.Equal(0x01, BinaryIntegerHelper<char>.GetShortestBitLength((char)0x0001));
            Assert.Equal(0x0F, BinaryIntegerHelper<char>.GetShortestBitLength((char)0x7FFF));
            Assert.Equal(0x10, BinaryIntegerHelper<char>.GetShortestBitLength((char)0x8000));
            Assert.Equal(0x10, BinaryIntegerHelper<char>.GetShortestBitLength((char)0xFFFF));
        }

        [Fact]
        public static void IsPow2Test()
        {
            Assert.False(BinaryNumberHelper<char>.IsPow2((char)0x0000));
            Assert.True(BinaryNumberHelper<char>.IsPow2((char)0x0001));
            Assert.False(BinaryNumberHelper<char>.IsPow2((char)0x7FFF));
            Assert.True(BinaryNumberHelper<char>.IsPow2((char)0x8000));
            Assert.False(BinaryNumberHelper<char>.IsPow2((char)0xFFFF));
        }

        [Fact]
        public static void Log2Test()
        {
            Assert.Equal((char)0x0000, BinaryNumberHelper<char>.Log2((char)0x0000));
            Assert.Equal((char)0x0000, BinaryNumberHelper<char>.Log2((char)0x0001));
            Assert.Equal((char)0x000E, BinaryNumberHelper<char>.Log2((char)0x7FFF));
            Assert.Equal((char)0x000F, BinaryNumberHelper<char>.Log2((char)0x8000));
            Assert.Equal((char)0x000F, BinaryNumberHelper<char>.Log2((char)0xFFFF));
        }

        [Fact]
        public static void op_BitwiseAndTest()
        {
            Assert.Equal((char)0x0000, BitwiseOperatorsHelper<char, char, char>.op_BitwiseAnd((char)0x0000, (char)1));
            Assert.Equal((char)0x0001, BitwiseOperatorsHelper<char, char, char>.op_BitwiseAnd((char)0x0001, (char)1));
            Assert.Equal((char)0x0001, BitwiseOperatorsHelper<char, char, char>.op_BitwiseAnd((char)0x7FFF, (char)1));
            Assert.Equal((char)0x0000, BitwiseOperatorsHelper<char, char, char>.op_BitwiseAnd((char)0x8000, (char)1));
            Assert.Equal((char)0x0001, BitwiseOperatorsHelper<char, char, char>.op_BitwiseAnd((char)0xFFFF, (char)1));
        }

        [Fact]
        public static void op_BitwiseOrTest()
        {
            Assert.Equal((char)0x0001, BitwiseOperatorsHelper<char, char, char>.op_BitwiseOr((char)0x0000, (char)1));
            Assert.Equal((char)0x0001, BitwiseOperatorsHelper<char, char, char>.op_BitwiseOr((char)0x0001, (char)1));
            Assert.Equal((char)0x7FFF, BitwiseOperatorsHelper<char, char, char>.op_BitwiseOr((char)0x7FFF, (char)1));
            Assert.Equal((char)0x8001, BitwiseOperatorsHelper<char, char, char>.op_BitwiseOr((char)0x8000, (char)1));
            Assert.Equal((char)0xFFFF, BitwiseOperatorsHelper<char, char, char>.op_BitwiseOr((char)0xFFFF, (char)1));
        }

        [Fact]
        public static void op_ExclusiveOrTest()
        {
            Assert.Equal((char)0x0001, BitwiseOperatorsHelper<char, char, char>.op_ExclusiveOr((char)0x0000, (char)1));
            Assert.Equal((char)0x0000, BitwiseOperatorsHelper<char, char, char>.op_ExclusiveOr((char)0x0001, (char)1));
            Assert.Equal((char)0x7FFE, BitwiseOperatorsHelper<char, char, char>.op_ExclusiveOr((char)0x7FFF, (char)1));
            Assert.Equal((char)0x8001, BitwiseOperatorsHelper<char, char, char>.op_ExclusiveOr((char)0x8000, (char)1));
            Assert.Equal((char)0xFFFE, BitwiseOperatorsHelper<char, char, char>.op_ExclusiveOr((char)0xFFFF, (char)1));
        }

        [Fact]
        public static void op_OnesComplementTest()
        {
            Assert.Equal((char)0xFFFF, BitwiseOperatorsHelper<char, char, char>.op_OnesComplement((char)0x0000));
            Assert.Equal((char)0xFFFE, BitwiseOperatorsHelper<char, char, char>.op_OnesComplement((char)0x0001));
            Assert.Equal((char)0x8000, BitwiseOperatorsHelper<char, char, char>.op_OnesComplement((char)0x7FFF));
            Assert.Equal((char)0x7FFF, BitwiseOperatorsHelper<char, char, char>.op_OnesComplement((char)0x8000));
            Assert.Equal((char)0x0000, BitwiseOperatorsHelper<char, char, char>.op_OnesComplement((char)0xFFFF));
        }

        [Fact]
        public static void op_LessThanTest()
        {
            Assert.True(ComparisonOperatorsHelper<char, char>.op_LessThan((char)0x0000, (char)1));
            Assert.False(ComparisonOperatorsHelper<char, char>.op_LessThan((char)0x0001, (char)1));
            Assert.False(ComparisonOperatorsHelper<char, char>.op_LessThan((char)0x7FFF, (char)1));
            Assert.False(ComparisonOperatorsHelper<char, char>.op_LessThan((char)0x8000, (char)1));
            Assert.False(ComparisonOperatorsHelper<char, char>.op_LessThan((char)0xFFFF, (char)1));
        }

        [Fact]
        public static void op_LessThanOrEqualTest()
        {
            Assert.True(ComparisonOperatorsHelper<char, char>.op_LessThanOrEqual((char)0x0000, (char)1));
            Assert.True(ComparisonOperatorsHelper<char, char>.op_LessThanOrEqual((char)0x0001, (char)1));
            Assert.False(ComparisonOperatorsHelper<char, char>.op_LessThanOrEqual((char)0x7FFF, (char)1));
            Assert.False(ComparisonOperatorsHelper<char, char>.op_LessThanOrEqual((char)0x8000, (char)1));
            Assert.False(ComparisonOperatorsHelper<char, char>.op_LessThanOrEqual((char)0xFFFF, (char)1));
        }

        [Fact]
        public static void op_GreaterThanTest()
        {
            Assert.False(ComparisonOperatorsHelper<char, char>.op_GreaterThan((char)0x0000, (char)1));
            Assert.False(ComparisonOperatorsHelper<char, char>.op_GreaterThan((char)0x0001, (char)1));
            Assert.True(ComparisonOperatorsHelper<char, char>.op_GreaterThan((char)0x7FFF, (char)1));
            Assert.True(ComparisonOperatorsHelper<char, char>.op_GreaterThan((char)0x8000, (char)1));
            Assert.True(ComparisonOperatorsHelper<char, char>.op_GreaterThan((char)0xFFFF, (char)1));
        }

        [Fact]
        public static void op_GreaterThanOrEqualTest()
        {
            Assert.False(ComparisonOperatorsHelper<char, char>.op_GreaterThanOrEqual((char)0x0000, (char)1));
            Assert.True(ComparisonOperatorsHelper<char, char>.op_GreaterThanOrEqual((char)0x0001, (char)1));
            Assert.True(ComparisonOperatorsHelper<char, char>.op_GreaterThanOrEqual((char)0x7FFF, (char)1));
            Assert.True(ComparisonOperatorsHelper<char, char>.op_GreaterThanOrEqual((char)0x8000, (char)1));
            Assert.True(ComparisonOperatorsHelper<char, char>.op_GreaterThanOrEqual((char)0xFFFF, (char)1));
        }

        [Fact]
        public static void op_DecrementTest()
        {
            Assert.Equal((char)0xFFFF, DecrementOperatorsHelper<char>.op_Decrement((char)0x0000));
            Assert.Equal((char)0x0000, DecrementOperatorsHelper<char>.op_Decrement((char)0x0001));
            Assert.Equal((char)0x7FFE, DecrementOperatorsHelper<char>.op_Decrement((char)0x7FFF));
            Assert.Equal((char)0x7FFF, DecrementOperatorsHelper<char>.op_Decrement((char)0x8000));
            Assert.Equal((char)0xFFFE, DecrementOperatorsHelper<char>.op_Decrement((char)0xFFFF));
        }

        [Fact]
        public static void op_CheckedDecrementTest()
        {
            Assert.Equal((char)0x0000, DecrementOperatorsHelper<char>.op_CheckedDecrement((char)0x0001));
            Assert.Equal((char)0x7FFE, DecrementOperatorsHelper<char>.op_CheckedDecrement((char)0x7FFF));
            Assert.Equal((char)0x7FFF, DecrementOperatorsHelper<char>.op_CheckedDecrement((char)0x8000));
            Assert.Equal((char)0xFFFE, DecrementOperatorsHelper<char>.op_CheckedDecrement((char)0xFFFF));

            Assert.Throws<OverflowException>(() => DecrementOperatorsHelper<char>.op_CheckedDecrement((char)0x0000));
        }

        [Fact]
        public static void op_DivisionTest()
        {
            Assert.Equal((char)0x0000, DivisionOperatorsHelper<char, char, char>.op_Division((char)0x0000, (char)2));
            Assert.Equal((char)0x0000, DivisionOperatorsHelper<char, char, char>.op_Division((char)0x0001, (char)2));
            Assert.Equal((char)0x3FFF, DivisionOperatorsHelper<char, char, char>.op_Division((char)0x7FFF, (char)2));
            Assert.Equal((char)0x4000, DivisionOperatorsHelper<char, char, char>.op_Division((char)0x8000, (char)2));
            Assert.Equal((char)0x7FFF, DivisionOperatorsHelper<char, char, char>.op_Division((char)0xFFFF, (char)2));

            Assert.Throws<DivideByZeroException>(() => DivisionOperatorsHelper<char, char, char>.op_Division((char)0x0001, (char)0));
        }

        [Fact]
        public static void op_CheckedDivisionTest()
        {
            Assert.Equal((char)0x0000, DivisionOperatorsHelper<char, char, char>.op_CheckedDivision((char)0x0000, (char)2));
            Assert.Equal((char)0x0000, DivisionOperatorsHelper<char, char, char>.op_CheckedDivision((char)0x0001, (char)2));
            Assert.Equal((char)0x3FFF, DivisionOperatorsHelper<char, char, char>.op_CheckedDivision((char)0x7FFF, (char)2));
            Assert.Equal((char)0x4000, DivisionOperatorsHelper<char, char, char>.op_CheckedDivision((char)0x8000, (char)2));
            Assert.Equal((char)0x7FFF, DivisionOperatorsHelper<char, char, char>.op_CheckedDivision((char)0xFFFF, (char)2));

            Assert.Throws<DivideByZeroException>(() => DivisionOperatorsHelper<char, char, char>.op_CheckedDivision((char)0x0001, (char)0));
        }

        [Fact]
        public static void op_EqualityTest()
        {
            Assert.False(EqualityOperatorsHelper<char, char>.op_Equality((char)0x0000, (char)1));
            Assert.True(EqualityOperatorsHelper<char, char>.op_Equality((char)0x0001, (char)1));
            Assert.False(EqualityOperatorsHelper<char, char>.op_Equality((char)0x7FFF, (char)1));
            Assert.False(EqualityOperatorsHelper<char, char>.op_Equality((char)0x8000, (char)1));
            Assert.False(EqualityOperatorsHelper<char, char>.op_Equality((char)0xFFFF, (char)1));
        }

        [Fact]
        public static void op_InequalityTest()
        {
            Assert.True(EqualityOperatorsHelper<char, char>.op_Inequality((char)0x0000, (char)1));
            Assert.False(EqualityOperatorsHelper<char, char>.op_Inequality((char)0x0001, (char)1));
            Assert.True(EqualityOperatorsHelper<char, char>.op_Inequality((char)0x7FFF, (char)1));
            Assert.True(EqualityOperatorsHelper<char, char>.op_Inequality((char)0x8000, (char)1));
            Assert.True(EqualityOperatorsHelper<char, char>.op_Inequality((char)0xFFFF, (char)1));
        }

        [Fact]
        public static void op_IncrementTest()
        {
            Assert.Equal((char)0x0001, IncrementOperatorsHelper<char>.op_Increment((char)0x0000));
            Assert.Equal((char)0x0002, IncrementOperatorsHelper<char>.op_Increment((char)0x0001));
            Assert.Equal((char)0x8000, IncrementOperatorsHelper<char>.op_Increment((char)0x7FFF));
            Assert.Equal((char)0x8001, IncrementOperatorsHelper<char>.op_Increment((char)0x8000));
            Assert.Equal((char)0x0000, IncrementOperatorsHelper<char>.op_Increment((char)0xFFFF));
        }

        [Fact]
        public static void op_CheckedIncrementTest()
        {
            Assert.Equal((char)0x0001, IncrementOperatorsHelper<char>.op_CheckedIncrement((char)0x0000));
            Assert.Equal((char)0x0002, IncrementOperatorsHelper<char>.op_CheckedIncrement((char)0x0001));
            Assert.Equal((char)0x8000, IncrementOperatorsHelper<char>.op_CheckedIncrement((char)0x7FFF));
            Assert.Equal((char)0x8001, IncrementOperatorsHelper<char>.op_CheckedIncrement((char)0x8000));

            Assert.Throws<OverflowException>(() => IncrementOperatorsHelper<char>.op_CheckedIncrement((char)0xFFFF));
        }

        [Fact]
        public static void op_ModulusTest()
        {
            Assert.Equal((char)0x0000, ModulusOperatorsHelper<char, char, char>.op_Modulus((char)0x0000, (char)2));
            Assert.Equal((char)0x0001, ModulusOperatorsHelper<char, char, char>.op_Modulus((char)0x0001, (char)2));
            Assert.Equal((char)0x0001, ModulusOperatorsHelper<char, char, char>.op_Modulus((char)0x7FFF, (char)2));
            Assert.Equal((char)0x0000, ModulusOperatorsHelper<char, char, char>.op_Modulus((char)0x8000, (char)2));
            Assert.Equal((char)0x0001, ModulusOperatorsHelper<char, char, char>.op_Modulus((char)0xFFFF, (char)2));

            Assert.Throws<DivideByZeroException>(() => ModulusOperatorsHelper<char, char, char>.op_Modulus((char)0x0001, (char)0));
        }

        [Fact]
        public static void op_MultiplyTest()
        {
            Assert.Equal((char)0x0000, MultiplyOperatorsHelper<char, char, char>.op_Multiply((char)0x0000, (char)2));
            Assert.Equal((char)0x0002, MultiplyOperatorsHelper<char, char, char>.op_Multiply((char)0x0001, (char)2));
            Assert.Equal((char)0xFFFE, MultiplyOperatorsHelper<char, char, char>.op_Multiply((char)0x7FFF, (char)2));
            Assert.Equal((char)0x0000, MultiplyOperatorsHelper<char, char, char>.op_Multiply((char)0x8000, (char)2));
            Assert.Equal((char)0xFFFE, MultiplyOperatorsHelper<char, char, char>.op_Multiply((char)0xFFFF, (char)2));
        }

        [Fact]
        public static void op_CheckedMultiplyTest()
        {
            Assert.Equal((char)0x0000, MultiplyOperatorsHelper<char, char, char>.op_CheckedMultiply((char)0x0000, (char)2));
            Assert.Equal((char)0x0002, MultiplyOperatorsHelper<char, char, char>.op_CheckedMultiply((char)0x0001, (char)2));
            Assert.Equal((char)0xFFFE, MultiplyOperatorsHelper<char, char, char>.op_CheckedMultiply((char)0x7FFF, (char)2));

            Assert.Throws<OverflowException>(() => MultiplyOperatorsHelper<char, char, char>.op_CheckedMultiply((char)0x8000, (char)2));
            Assert.Throws<OverflowException>(() => MultiplyOperatorsHelper<char, char, char>.op_CheckedMultiply((char)0xFFFF, (char)2));
        }

        [Fact]
        public static void AbsTest()
        {
            Assert.Equal((char)0x0000, NumberHelper<char>.Abs((char)0x0000));
            Assert.Equal((char)0x0001, NumberHelper<char>.Abs((char)0x0001));
            Assert.Equal((char)0x7FFF, NumberHelper<char>.Abs((char)0x7FFF));
            Assert.Equal((char)0x8000, NumberHelper<char>.Abs((char)0x8000));
            Assert.Equal((char)0xFFFF, NumberHelper<char>.Abs((char)0xFFFF));
        }

        [Fact]
        public static void ClampTest()
        {
            Assert.Equal((char)0x0001, NumberHelper<char>.Clamp((char)0x0000, (char)0x0001, (char)0x003F));
            Assert.Equal((char)0x0001, NumberHelper<char>.Clamp((char)0x0001, (char)0x0001, (char)0x003F));
            Assert.Equal((char)0x003F, NumberHelper<char>.Clamp((char)0x7FFF, (char)0x0001, (char)0x003F));
            Assert.Equal((char)0x003F, NumberHelper<char>.Clamp((char)0x8000, (char)0x0001, (char)0x003F));
            Assert.Equal((char)0x003F, NumberHelper<char>.Clamp((char)0xFFFF, (char)0x0001, (char)0x003F));
        }

        [Fact]
        public static void CreateCheckedFromByteTest()
        {
            Assert.Equal((char)0x0000, NumberHelper<char>.CreateChecked<byte>(0x00));
            Assert.Equal((char)0x0001, NumberHelper<char>.CreateChecked<byte>(0x01));
            Assert.Equal((char)0x007F, NumberHelper<char>.CreateChecked<byte>(0x7F));
            Assert.Equal((char)0x0080, NumberHelper<char>.CreateChecked<byte>(0x80));
            Assert.Equal((char)0x00FF, NumberHelper<char>.CreateChecked<byte>(0xFF));
        }

        [Fact]
        public static void CreateCheckedFromCharTest()
        {
            Assert.Equal((char)0x0000, NumberHelper<char>.CreateChecked<char>((char)0x0000));
            Assert.Equal((char)0x0001, NumberHelper<char>.CreateChecked<char>((char)0x0001));
            Assert.Equal((char)0x7FFF, NumberHelper<char>.CreateChecked<char>((char)0x7FFF));
            Assert.Equal((char)0x8000, NumberHelper<char>.CreateChecked<char>((char)0x8000));
            Assert.Equal((char)0xFFFF, NumberHelper<char>.CreateChecked<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateCheckedFromInt16Test()
        {
            Assert.Equal((char)0x0000, NumberHelper<char>.CreateChecked<short>(0x0000));
            Assert.Equal((char)0x0001, NumberHelper<char>.CreateChecked<short>(0x0001));
            Assert.Equal((char)0x7FFF, NumberHelper<char>.CreateChecked<short>(0x7FFF));
            Assert.Throws<OverflowException>(() => NumberHelper<char>.CreateChecked<short>(unchecked((short)0x8000)));
            Assert.Throws<OverflowException>(() => NumberHelper<char>.CreateChecked<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromInt32Test()
        {
            Assert.Equal((char)0x0000, NumberHelper<char>.CreateChecked<int>(0x00000000));
            Assert.Equal((char)0x0001, NumberHelper<char>.CreateChecked<int>(0x00000001));
            Assert.Throws<OverflowException>(() => NumberHelper<char>.CreateChecked<int>(0x7FFFFFFF));
            Assert.Throws<OverflowException>(() => NumberHelper<char>.CreateChecked<int>(unchecked((int)0x80000000)));
            Assert.Throws<OverflowException>(() => NumberHelper<char>.CreateChecked<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromInt64Test()
        {
            Assert.Equal((char)0x0000, NumberHelper<char>.CreateChecked<long>(0x0000000000000000));
            Assert.Equal((char)0x0001, NumberHelper<char>.CreateChecked<long>(0x0000000000000001));
            Assert.Throws<OverflowException>(() => NumberHelper<char>.CreateChecked<long>(0x7FFFFFFFFFFFFFFF));
            Assert.Throws<OverflowException>(() => NumberHelper<char>.CreateChecked<long>(unchecked((long)0x8000000000000000)));
            Assert.Throws<OverflowException>(() => NumberHelper<char>.CreateChecked<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((char)0x0000, NumberHelper<char>.CreateChecked<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal((char)0x0001, NumberHelper<char>.CreateChecked<nint>(unchecked((nint)0x0000000000000001)));
                Assert.Throws<OverflowException>(() => NumberHelper<char>.CreateChecked<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Throws<OverflowException>(() => NumberHelper<char>.CreateChecked<nint>(unchecked((nint)0x8000000000000000)));
                Assert.Throws<OverflowException>(() => NumberHelper<char>.CreateChecked<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((char)0x0000, NumberHelper<char>.CreateChecked<nint>((nint)0x00000000));
                Assert.Equal((char)0x0001, NumberHelper<char>.CreateChecked<nint>((nint)0x00000001));
                Assert.Throws<OverflowException>(() => NumberHelper<char>.CreateChecked<nint>((nint)0x7FFFFFFF));
                Assert.Throws<OverflowException>(() => NumberHelper<char>.CreateChecked<nint>(unchecked((nint)0x80000000)));
                Assert.Throws<OverflowException>(() => NumberHelper<char>.CreateChecked<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateCheckedFromSByteTest()
        {
            Assert.Equal((char)0x0000, NumberHelper<char>.CreateChecked<sbyte>(0x00));
            Assert.Equal((char)0x0001, NumberHelper<char>.CreateChecked<sbyte>(0x01));
            Assert.Equal((char)0x007F, NumberHelper<char>.CreateChecked<sbyte>(0x7F));
            Assert.Throws<OverflowException>(() => NumberHelper<char>.CreateChecked<sbyte>(unchecked((sbyte)0x80)));
            Assert.Throws<OverflowException>(() => NumberHelper<char>.CreateChecked<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateCheckedFromUInt16Test()
        {
            Assert.Equal((char)0x0000, NumberHelper<char>.CreateChecked<ushort>(0x0000));
            Assert.Equal((char)0x0001, NumberHelper<char>.CreateChecked<ushort>(0x0001));
            Assert.Equal((char)0x7FFF, NumberHelper<char>.CreateChecked<ushort>(0x7FFF));
            Assert.Equal((char)0x8000, NumberHelper<char>.CreateChecked<ushort>(0x8000));
            Assert.Equal((char)0xFFFF, NumberHelper<char>.CreateChecked<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUInt32Test()
        {
            Assert.Equal((char)0x0000, NumberHelper<char>.CreateChecked<uint>(0x00000000));
            Assert.Equal((char)0x0001, NumberHelper<char>.CreateChecked<uint>(0x00000001));
            Assert.Throws<OverflowException>(() => NumberHelper<char>.CreateChecked<uint>(0x7FFFFFFF));
            Assert.Throws<OverflowException>(() => NumberHelper<char>.CreateChecked<uint>(0x80000000));
            Assert.Throws<OverflowException>(() => NumberHelper<char>.CreateChecked<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUInt64Test()
        {
            Assert.Equal((char)0x0000, NumberHelper<char>.CreateChecked<ulong>(0x0000000000000000));
            Assert.Equal((char)0x0001, NumberHelper<char>.CreateChecked<ulong>(0x0000000000000001));
            Assert.Throws<OverflowException>(() => NumberHelper<char>.CreateChecked<ulong>(0x7FFFFFFFFFFFFFFF));
            Assert.Throws<OverflowException>(() => NumberHelper<char>.CreateChecked<ulong>(0x8000000000000000));
            Assert.Throws<OverflowException>(() => NumberHelper<char>.CreateChecked<ulong>(0xFFFFFFFFFFFFFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((char)0x0000, NumberHelper<char>.CreateChecked<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal((char)0x0001, NumberHelper<char>.CreateChecked<nuint>(unchecked((nuint)0x0000000000000001)));
                Assert.Throws<OverflowException>(() => NumberHelper<char>.CreateChecked<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Throws<OverflowException>(() => NumberHelper<char>.CreateChecked<nuint>(unchecked((nuint)0x8000000000000000)));
                Assert.Throws<OverflowException>(() => NumberHelper<char>.CreateChecked<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((char)0x0000, NumberHelper<char>.CreateChecked<nuint>((nuint)0x00000000));
                Assert.Equal((char)0x0001, NumberHelper<char>.CreateChecked<nuint>((nuint)0x00000001));
                Assert.Throws<OverflowException>(() => NumberHelper<char>.CreateChecked<nuint>((nuint)0x7FFFFFFF));
                Assert.Throws<OverflowException>(() => NumberHelper<char>.CreateChecked<nuint>((nuint)0x80000000));
                Assert.Throws<OverflowException>(() => NumberHelper<char>.CreateChecked<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateSaturatingFromByteTest()
        {
            Assert.Equal((char)0x0000, NumberHelper<char>.CreateSaturating<byte>(0x00));
            Assert.Equal((char)0x0001, NumberHelper<char>.CreateSaturating<byte>(0x01));
            Assert.Equal((char)0x007F, NumberHelper<char>.CreateSaturating<byte>(0x7F));
            Assert.Equal((char)0x0080, NumberHelper<char>.CreateSaturating<byte>(0x80));
            Assert.Equal((char)0x00FF, NumberHelper<char>.CreateSaturating<byte>(0xFF));
        }

        [Fact]
        public static void CreateSaturatingFromCharTest()
        {
            Assert.Equal((char)0x0000, NumberHelper<char>.CreateSaturating<char>((char)0x0000));
            Assert.Equal((char)0x0001, NumberHelper<char>.CreateSaturating<char>((char)0x0001));
            Assert.Equal((char)0x7FFF, NumberHelper<char>.CreateSaturating<char>((char)0x7FFF));
            Assert.Equal((char)0x8000, NumberHelper<char>.CreateSaturating<char>((char)0x8000));
            Assert.Equal((char)0xFFFF, NumberHelper<char>.CreateSaturating<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromInt16Test()
        {
            Assert.Equal((char)0x0000, NumberHelper<char>.CreateSaturating<short>(0x0000));
            Assert.Equal((char)0x0001, NumberHelper<char>.CreateSaturating<short>(0x0001));
            Assert.Equal((char)0x7FFF, NumberHelper<char>.CreateSaturating<short>(0x7FFF));
            Assert.Equal((char)0x0000, NumberHelper<char>.CreateSaturating<short>(unchecked((short)0x8000)));
            Assert.Equal((char)0x0000, NumberHelper<char>.CreateSaturating<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromInt32Test()
        {
            Assert.Equal((char)0x0000, NumberHelper<char>.CreateSaturating<int>(0x00000000));
            Assert.Equal((char)0x0001, NumberHelper<char>.CreateSaturating<int>(0x00000001));
            Assert.Equal((char)0xFFFF, NumberHelper<char>.CreateSaturating<int>(0x7FFFFFFF));
            Assert.Equal((char)0x0000, NumberHelper<char>.CreateSaturating<int>(unchecked((int)0x80000000)));
            Assert.Equal((char)0x0000, NumberHelper<char>.CreateSaturating<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromInt64Test()
        {
            Assert.Equal((char)0x0000, NumberHelper<char>.CreateSaturating<long>(0x0000000000000000));
            Assert.Equal((char)0x0001, NumberHelper<char>.CreateSaturating<long>(0x0000000000000001));
            Assert.Equal((char)0xFFFF, NumberHelper<char>.CreateSaturating<long>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal((char)0x0000, NumberHelper<char>.CreateSaturating<long>(unchecked((long)0x8000000000000000)));
            Assert.Equal((char)0x0000, NumberHelper<char>.CreateSaturating<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((char)0x0000, NumberHelper<char>.CreateSaturating<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal((char)0x0001, NumberHelper<char>.CreateSaturating<nint>(unchecked((nint)0x0000000000000001)));
                Assert.Equal((char)0xFFFF, NumberHelper<char>.CreateSaturating<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal((char)0x0000, NumberHelper<char>.CreateSaturating<nint>(unchecked((nint)0x8000000000000000)));
                Assert.Equal((char)0x0000, NumberHelper<char>.CreateSaturating<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((char)0x0000, NumberHelper<char>.CreateSaturating<nint>((nint)0x00000000));
                Assert.Equal((char)0x0001, NumberHelper<char>.CreateSaturating<nint>((nint)0x00000001));
                Assert.Equal((char)0xFFFF, NumberHelper<char>.CreateSaturating<nint>((nint)0x7FFFFFFF));
                Assert.Equal((char)0x0000, NumberHelper<char>.CreateSaturating<nint>(unchecked((nint)0x80000000)));
                Assert.Equal((char)0x0000, NumberHelper<char>.CreateSaturating<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateSaturatingFromSByteTest()
        {
            Assert.Equal((char)0x0000, NumberHelper<char>.CreateSaturating<sbyte>(0x00));
            Assert.Equal((char)0x0001, NumberHelper<char>.CreateSaturating<sbyte>(0x01));
            Assert.Equal((char)0x007F, NumberHelper<char>.CreateSaturating<sbyte>(0x7F));
            Assert.Equal((char)0x0000, NumberHelper<char>.CreateSaturating<sbyte>(unchecked((sbyte)0x80)));
            Assert.Equal((char)0x0000, NumberHelper<char>.CreateSaturating<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateSaturatingFromUInt16Test()
        {
            Assert.Equal((char)0x0000, NumberHelper<char>.CreateSaturating<ushort>(0x0000));
            Assert.Equal((char)0x0001, NumberHelper<char>.CreateSaturating<ushort>(0x0001));
            Assert.Equal((char)0x7FFF, NumberHelper<char>.CreateSaturating<ushort>(0x7FFF));
            Assert.Equal((char)0x8000, NumberHelper<char>.CreateSaturating<ushort>(0x8000));
            Assert.Equal((char)0xFFFF, NumberHelper<char>.CreateSaturating<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUInt32Test()
        {
            Assert.Equal((char)0x0000, NumberHelper<char>.CreateSaturating<uint>(0x00000000));
            Assert.Equal((char)0x0001, NumberHelper<char>.CreateSaturating<uint>(0x00000001));
            Assert.Equal((char)0xFFFF, NumberHelper<char>.CreateSaturating<uint>(0x7FFFFFFF));
            Assert.Equal((char)0xFFFF, NumberHelper<char>.CreateSaturating<uint>(0x80000000));
            Assert.Equal((char)0xFFFF, NumberHelper<char>.CreateSaturating<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUInt64Test()
        {
            Assert.Equal((char)0x0000, NumberHelper<char>.CreateSaturating<ulong>(0x0000000000000000));
            Assert.Equal((char)0x0001, NumberHelper<char>.CreateSaturating<ulong>(0x0000000000000001));
            Assert.Equal((char)0xFFFF, NumberHelper<char>.CreateSaturating<ulong>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal((char)0xFFFF, NumberHelper<char>.CreateSaturating<ulong>(0x8000000000000000));
            Assert.Equal((char)0xFFFF, NumberHelper<char>.CreateSaturating<ulong>(0xFFFFFFFFFFFFFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((char)0x0000, NumberHelper<char>.CreateSaturating<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal((char)0x0001, NumberHelper<char>.CreateSaturating<nuint>(unchecked((nuint)0x0000000000000001)));
                Assert.Equal((char)0xFFFF, NumberHelper<char>.CreateSaturating<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal((char)0xFFFF, NumberHelper<char>.CreateSaturating<nuint>(unchecked((nuint)0x8000000000000000)));
                Assert.Equal((char)0xFFFF, NumberHelper<char>.CreateSaturating<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((char)0x0000, NumberHelper<char>.CreateSaturating<nuint>((nuint)0x00000000));
                Assert.Equal((char)0x0001, NumberHelper<char>.CreateSaturating<nuint>((nuint)0x00000001));
                Assert.Equal((char)0xFFFF, NumberHelper<char>.CreateSaturating<nuint>((nuint)0x7FFFFFFF));
                Assert.Equal((char)0xFFFF, NumberHelper<char>.CreateSaturating<nuint>((nuint)0x80000000));
                Assert.Equal((char)0xFFFF, NumberHelper<char>.CreateSaturating<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateTruncatingFromByteTest()
        {
            Assert.Equal((char)0x0000, NumberHelper<char>.CreateTruncating<byte>(0x00));
            Assert.Equal((char)0x0001, NumberHelper<char>.CreateTruncating<byte>(0x01));
            Assert.Equal((char)0x007F, NumberHelper<char>.CreateTruncating<byte>(0x7F));
            Assert.Equal((char)0x0080, NumberHelper<char>.CreateTruncating<byte>(0x80));
            Assert.Equal((char)0x00FF, NumberHelper<char>.CreateTruncating<byte>(0xFF));
        }

        [Fact]
        public static void CreateTruncatingFromCharTest()
        {
            Assert.Equal((char)0x0000, NumberHelper<char>.CreateTruncating<char>((char)0x0000));
            Assert.Equal((char)0x0001, NumberHelper<char>.CreateTruncating<char>((char)0x0001));
            Assert.Equal((char)0x7FFF, NumberHelper<char>.CreateTruncating<char>((char)0x7FFF));
            Assert.Equal((char)0x8000, NumberHelper<char>.CreateTruncating<char>((char)0x8000));
            Assert.Equal((char)0xFFFF, NumberHelper<char>.CreateTruncating<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromInt16Test()
        {
            Assert.Equal((char)0x0000, NumberHelper<char>.CreateTruncating<short>(0x0000));
            Assert.Equal((char)0x0001, NumberHelper<char>.CreateTruncating<short>(0x0001));
            Assert.Equal((char)0x7FFF, NumberHelper<char>.CreateTruncating<short>(0x7FFF));
            Assert.Equal((char)0x8000, NumberHelper<char>.CreateTruncating<short>(unchecked((short)0x8000)));
            Assert.Equal((char)0xFFFF, NumberHelper<char>.CreateTruncating<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateTruncatingFromInt32Test()
        {
            Assert.Equal((char)0x0000, NumberHelper<char>.CreateTruncating<int>(0x00000000));
            Assert.Equal((char)0x0001, NumberHelper<char>.CreateTruncating<int>(0x00000001));
            Assert.Equal((char)0xFFFF, NumberHelper<char>.CreateTruncating<int>(0x7FFFFFFF));
            Assert.Equal((char)0x0000, NumberHelper<char>.CreateTruncating<int>(unchecked((int)0x80000000)));
            Assert.Equal((char)0xFFFF, NumberHelper<char>.CreateTruncating<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateTruncatingFromInt64Test()
        {
            Assert.Equal((char)0x0000, NumberHelper<char>.CreateTruncating<long>(0x0000000000000000));
            Assert.Equal((char)0x0001, NumberHelper<char>.CreateTruncating<long>(0x0000000000000001));
            Assert.Equal((char)0xFFFF, NumberHelper<char>.CreateTruncating<long>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal((char)0x0000, NumberHelper<char>.CreateTruncating<long>(unchecked((long)0x8000000000000000)));
            Assert.Equal((char)0xFFFF, NumberHelper<char>.CreateTruncating<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void CreateTruncatingFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((char)0x0000, NumberHelper<char>.CreateTruncating<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal((char)0x0001, NumberHelper<char>.CreateTruncating<nint>(unchecked((nint)0x0000000000000001)));
                Assert.Equal((char)0xFFFF, NumberHelper<char>.CreateTruncating<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal((char)0x0000, NumberHelper<char>.CreateTruncating<nint>(unchecked((nint)0x8000000000000000)));
                Assert.Equal((char)0xFFFF, NumberHelper<char>.CreateTruncating<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((char)0x0000, NumberHelper<char>.CreateTruncating<nint>((nint)0x00000000));
                Assert.Equal((char)0x0001, NumberHelper<char>.CreateTruncating<nint>((nint)0x00000001));
                Assert.Equal((char)0xFFFF, NumberHelper<char>.CreateTruncating<nint>((nint)0x7FFFFFFF));
                Assert.Equal((char)0x0000, NumberHelper<char>.CreateTruncating<nint>(unchecked((nint)0x80000000)));
                Assert.Equal((char)0xFFFF, NumberHelper<char>.CreateTruncating<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateTruncatingFromSByteTest()
        {
            Assert.Equal((char)0x0000, NumberHelper<char>.CreateTruncating<sbyte>(0x00));
            Assert.Equal((char)0x0001, NumberHelper<char>.CreateTruncating<sbyte>(0x01));
            Assert.Equal((char)0x007F, NumberHelper<char>.CreateTruncating<sbyte>(0x7F));
            Assert.Equal((char)0xFF80, NumberHelper<char>.CreateTruncating<sbyte>(unchecked((sbyte)0x80)));
            Assert.Equal((char)0xFFFF, NumberHelper<char>.CreateTruncating<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateTruncatingFromUInt16Test()
        {
            Assert.Equal((char)0x0000, NumberHelper<char>.CreateTruncating<ushort>(0x0000));
            Assert.Equal((char)0x0001, NumberHelper<char>.CreateTruncating<ushort>(0x0001));
            Assert.Equal((char)0x7FFF, NumberHelper<char>.CreateTruncating<ushort>(0x7FFF));
            Assert.Equal((char)0x8000, NumberHelper<char>.CreateTruncating<ushort>(0x8000));
            Assert.Equal((char)0xFFFF, NumberHelper<char>.CreateTruncating<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt32Test()
        {
            Assert.Equal((char)0x0000, NumberHelper<char>.CreateTruncating<uint>(0x00000000));
            Assert.Equal((char)0x0001, NumberHelper<char>.CreateTruncating<uint>(0x00000001));
            Assert.Equal((char)0xFFFF, NumberHelper<char>.CreateTruncating<uint>(0x7FFFFFFF));
            Assert.Equal((char)0x0000, NumberHelper<char>.CreateTruncating<uint>(0x80000000));
            Assert.Equal((char)0xFFFF, NumberHelper<char>.CreateTruncating<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt64Test()
        {
            Assert.Equal((char)0x0000, NumberHelper<char>.CreateTruncating<ulong>(0x0000000000000000));
            Assert.Equal((char)0x0001, NumberHelper<char>.CreateTruncating<ulong>(0x0000000000000001));
            Assert.Equal((char)0xFFFF, NumberHelper<char>.CreateTruncating<ulong>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal((char)0x0000, NumberHelper<char>.CreateTruncating<ulong>(0x8000000000000000));
            Assert.Equal((char)0xFFFF, NumberHelper<char>.CreateTruncating<ulong>(0xFFFFFFFFFFFFFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((char)0x0000, NumberHelper<char>.CreateTruncating<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal((char)0x0001, NumberHelper<char>.CreateTruncating<nuint>(unchecked((nuint)0x0000000000000001)));
                Assert.Equal((char)0xFFFF, NumberHelper<char>.CreateTruncating<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal((char)0x0000, NumberHelper<char>.CreateTruncating<nuint>(unchecked((nuint)0x8000000000000000)));
                Assert.Equal((char)0xFFFF, NumberHelper<char>.CreateTruncating<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((char)0x0000, NumberHelper<char>.CreateTruncating<nuint>((nuint)0x00000000));
                Assert.Equal((char)0x0001, NumberHelper<char>.CreateTruncating<nuint>((nuint)0x00000001));
                Assert.Equal((char)0xFFFF, NumberHelper<char>.CreateTruncating<nuint>((nuint)0x7FFFFFFF));
                Assert.Equal((char)0x0000, NumberHelper<char>.CreateTruncating<nuint>((nuint)0x80000000));
                Assert.Equal((char)0xFFFF, NumberHelper<char>.CreateTruncating<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void DivRemTest()
        {
            Assert.Equal(((char)0x0000, (char)0x0000), BinaryIntegerHelper<char>.DivRem((char)0x0000, (char)2));
            Assert.Equal(((char)0x0000, (char)0x0001), BinaryIntegerHelper<char>.DivRem((char)0x0001, (char)2));
            Assert.Equal(((char)0x3FFF, (char)0x0001), BinaryIntegerHelper<char>.DivRem((char)0x7FFF, (char)2));
            Assert.Equal(((char)0x4000, (char)0x0000), BinaryIntegerHelper<char>.DivRem((char)0x8000, (char)2));
            Assert.Equal(((char)0x7FFF, (char)0x0001), BinaryIntegerHelper<char>.DivRem((char)0xFFFF, (char)2));
        }

        [Fact]
        public static void MaxTest()
        {
            Assert.Equal((char)0x0001, NumberHelper<char>.Max((char)0x0000, (char)1));
            Assert.Equal((char)0x0001, NumberHelper<char>.Max((char)0x0001, (char)1));
            Assert.Equal((char)0x7FFF, NumberHelper<char>.Max((char)0x7FFF, (char)1));
            Assert.Equal((char)0x8000, NumberHelper<char>.Max((char)0x8000, (char)1));
            Assert.Equal((char)0xFFFF, NumberHelper<char>.Max((char)0xFFFF, (char)1));
        }

        [Fact]
        public static void MinTest()
        {
            Assert.Equal((char)0x0000, NumberHelper<char>.Min((char)0x0000, (char)1));
            Assert.Equal((char)0x0001, NumberHelper<char>.Min((char)0x0001, (char)1));
            Assert.Equal((char)0x0001, NumberHelper<char>.Min((char)0x7FFF, (char)1));
            Assert.Equal((char)0x0001, NumberHelper<char>.Min((char)0x8000, (char)1));
            Assert.Equal((char)0x0001, NumberHelper<char>.Min((char)0xFFFF, (char)1));
        }

        [Fact]
        public static void SignTest()
        {
            Assert.Equal(0, NumberHelper<char>.Sign((char)0x0000));
            Assert.Equal(1, NumberHelper<char>.Sign((char)0x0001));
            Assert.Equal(1, NumberHelper<char>.Sign((char)0x7FFF));
            Assert.Equal(1, NumberHelper<char>.Sign((char)0x8000));
            Assert.Equal(1, NumberHelper<char>.Sign((char)0xFFFF));
        }

        [Fact]
        public static void TryCreateFromByteTest()
        {
            char result;

            Assert.True(NumberHelper<char>.TryCreate<byte>(0x00, out result));
            Assert.Equal((char)0x0000, result);

            Assert.True(NumberHelper<char>.TryCreate<byte>(0x01, out result));
            Assert.Equal((char)0x0001, result);

            Assert.True(NumberHelper<char>.TryCreate<byte>(0x7F, out result));
            Assert.Equal((char)0x007F, result);

            Assert.True(NumberHelper<char>.TryCreate<byte>(0x80, out result));
            Assert.Equal((char)0x0080, result);

            Assert.True(NumberHelper<char>.TryCreate<byte>(0xFF, out result));
            Assert.Equal((char)0x00FF, result);
        }

        [Fact]
        public static void TryCreateFromCharTest()
        {
            char result;

            Assert.True(NumberHelper<char>.TryCreate<char>((char)0x0000, out result));
            Assert.Equal((char)0x0000, result);

            Assert.True(NumberHelper<char>.TryCreate<char>((char)0x0001, out result));
            Assert.Equal((char)0x0001, result);

            Assert.True(NumberHelper<char>.TryCreate<char>((char)0x7FFF, out result));
            Assert.Equal((char)0x7FFF, result);

            Assert.True(NumberHelper<char>.TryCreate<char>((char)0x8000, out result));
            Assert.Equal((char)0x8000, result);

            Assert.True(NumberHelper<char>.TryCreate<char>((char)0xFFFF, out result));
            Assert.Equal((char)0xFFFF, result);
        }

        [Fact]
        public static void TryCreateFromInt16Test()
        {
            char result;

            Assert.True(NumberHelper<char>.TryCreate<short>(0x0000, out result));
            Assert.Equal((char)0x0000, result);

            Assert.True(NumberHelper<char>.TryCreate<short>(0x0001, out result));
            Assert.Equal((char)0x0001, result);

            Assert.True(NumberHelper<char>.TryCreate<short>(0x7FFF, out result));
            Assert.Equal((char)0x7FFF, result);

            Assert.False(NumberHelper<char>.TryCreate<short>(unchecked((short)0x8000), out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(NumberHelper<char>.TryCreate<short>(unchecked((short)0xFFFF), out result));
            Assert.Equal((char)0x0000, result);
        }

        [Fact]
        public static void TryCreateFromInt32Test()
        {
            char result;

            Assert.True(NumberHelper<char>.TryCreate<int>(0x00000000, out result));
            Assert.Equal((char)0x0000, result);

            Assert.True(NumberHelper<char>.TryCreate<int>(0x00000001, out result));
            Assert.Equal((char)0x0001, result);

            Assert.False(NumberHelper<char>.TryCreate<int>(0x7FFFFFFF, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(NumberHelper<char>.TryCreate<int>(unchecked((int)0x80000000), out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(NumberHelper<char>.TryCreate<int>(unchecked((int)0xFFFFFFFF), out result));
            Assert.Equal((char)0x0000, result);
        }

        [Fact]
        public static void TryCreateFromInt64Test()
        {
            char result;

            Assert.True(NumberHelper<char>.TryCreate<long>(0x0000000000000000, out result));
            Assert.Equal((char)0x0000, result);

            Assert.True(NumberHelper<char>.TryCreate<long>(0x0000000000000001, out result));
            Assert.Equal((char)0x0001, result);

            Assert.False(NumberHelper<char>.TryCreate<long>(0x7FFFFFFFFFFFFFFF, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(NumberHelper<char>.TryCreate<long>(unchecked((long)0x8000000000000000), out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(NumberHelper<char>.TryCreate<long>(unchecked((long)0xFFFFFFFFFFFFFFFF), out result));
            Assert.Equal((char)0x0000, result);
        }

        [Fact]
        public static void TryCreateFromIntPtrTest()
        {
            char result;

            if (Environment.Is64BitProcess)
            {
                Assert.True(NumberHelper<char>.TryCreate<nint>(unchecked((nint)0x0000000000000000), out result));
                Assert.Equal((char)0x0000, result);

                Assert.True(NumberHelper<char>.TryCreate<nint>(unchecked((nint)0x0000000000000001), out result));
                Assert.Equal((char)0x0001, result);

                Assert.False(NumberHelper<char>.TryCreate<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF), out result));
                Assert.Equal((char)0x0000, result);

                Assert.False(NumberHelper<char>.TryCreate<nint>(unchecked((nint)0x8000000000000000), out result));
                Assert.Equal((char)0x0000, result);

                Assert.False(NumberHelper<char>.TryCreate<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF), out result));
                Assert.Equal((char)0x0000, result);
            }
            else
            {
                Assert.True(NumberHelper<char>.TryCreate<nint>((nint)0x00000000, out result));
                Assert.Equal((char)0x0000, result);

                Assert.True(NumberHelper<char>.TryCreate<nint>((nint)0x00000001, out result));
                Assert.Equal((char)0x0001, result);

                Assert.False(NumberHelper<char>.TryCreate<nint>((nint)0x7FFFFFFF, out result));
                Assert.Equal((char)0x0000, result);

                Assert.False(NumberHelper<char>.TryCreate<nint>(unchecked((nint)0x80000000), out result));
                Assert.Equal((char)0x0000, result);

                Assert.False(NumberHelper<char>.TryCreate<nint>(unchecked((nint)0xFFFFFFFF), out result));
                Assert.Equal((char)0x0000, result);
            }
        }

        [Fact]
        public static void TryCreateFromSByteTest()
        {
            char result;

            Assert.True(NumberHelper<char>.TryCreate<sbyte>(0x00, out result));
            Assert.Equal((char)0x0000, result);

            Assert.True(NumberHelper<char>.TryCreate<sbyte>(0x01, out result));
            Assert.Equal((char)0x0001, result);

            Assert.True(NumberHelper<char>.TryCreate<sbyte>(0x7F, out result));
            Assert.Equal((char)0x007F, result);

            Assert.False(NumberHelper<char>.TryCreate<sbyte>(unchecked((sbyte)0x80), out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(NumberHelper<char>.TryCreate<sbyte>(unchecked((sbyte)0xFF), out result));
            Assert.Equal((char)0x0000, result);
        }

        [Fact]
        public static void TryCreateFromUInt16Test()
        {
            char result;

            Assert.True(NumberHelper<char>.TryCreate<ushort>(0x0000, out result));
            Assert.Equal((char)0x0000, result);

            Assert.True(NumberHelper<char>.TryCreate<ushort>(0x0001, out result));
            Assert.Equal((char)0x0001, result);

            Assert.True(NumberHelper<char>.TryCreate<ushort>(0x7FFF, out result));
            Assert.Equal((char)0x7FFF, result);

            Assert.True(NumberHelper<char>.TryCreate<ushort>(0x8000, out result));
            Assert.Equal((char)0x8000, result);

            Assert.True(NumberHelper<char>.TryCreate<ushort>(0xFFFF, out result));
            Assert.Equal((char)0xFFFF, result);
        }

        [Fact]
        public static void TryCreateFromUInt32Test()
        {
            char result;

            Assert.True(NumberHelper<char>.TryCreate<uint>(0x00000000, out result));
            Assert.Equal((char)0x0000, result);

            Assert.True(NumberHelper<char>.TryCreate<uint>(0x00000001, out result));
            Assert.Equal((char)0x0001, result);

            Assert.False(NumberHelper<char>.TryCreate<uint>(0x7FFFFFFF, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(NumberHelper<char>.TryCreate<uint>(0x80000000, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(NumberHelper<char>.TryCreate<uint>(0xFFFFFFFF, out result));
            Assert.Equal((char)0x0000, result);
        }

        [Fact]
        public static void TryCreateFromUInt64Test()
        {
            char result;

            Assert.True(NumberHelper<char>.TryCreate<ulong>(0x0000000000000000, out result));
            Assert.Equal((char)0x0000, result);

            Assert.True(NumberHelper<char>.TryCreate<ulong>(0x0000000000000001, out result));
            Assert.Equal((char)0x0001, result);

            Assert.False(NumberHelper<char>.TryCreate<ulong>(0x7FFFFFFFFFFFFFFF, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(NumberHelper<char>.TryCreate<ulong>(0x8000000000000000, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(NumberHelper<char>.TryCreate<ulong>(0xFFFFFFFFFFFFFFFF, out result));
            Assert.Equal((char)0x0000, result);
        }

        [Fact]
        public static void TryCreateFromUIntPtrTest()
        {
            char result;

            if (Environment.Is64BitProcess)
            {
                Assert.True(NumberHelper<char>.TryCreate<nuint>(unchecked((nuint)0x0000000000000000), out result));
                Assert.Equal((char)0x0000, result);

                Assert.True(NumberHelper<char>.TryCreate<nuint>(unchecked((nuint)0x0000000000000001), out result));
                Assert.Equal((char)0x0001, result);

                Assert.False(NumberHelper<char>.TryCreate<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF), out result));
                Assert.Equal((char)0x0000, result);

                Assert.False(NumberHelper<char>.TryCreate<nuint>(unchecked((nuint)0x8000000000000000), out result));
                Assert.Equal((char)0x0000, result);

                Assert.False(NumberHelper<char>.TryCreate<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF), out result));
                Assert.Equal((char)0x0000, result);
            }
            else
            {
                Assert.True(NumberHelper<char>.TryCreate<nuint>((nuint)0x00000000, out result));
                Assert.Equal((char)0x0000, result);

                Assert.True(NumberHelper<char>.TryCreate<nuint>((nuint)0x00000001, out result));
                Assert.Equal((char)0x0001, result);

                Assert.False(NumberHelper<char>.TryCreate<nuint>((nuint)0x7FFFFFFF, out result));
                Assert.Equal((char)0x0000, result);

                Assert.False(NumberHelper<char>.TryCreate<nuint>(unchecked((nuint)0x80000000), out result));
                Assert.Equal((char)0x0000, result);

                Assert.False(NumberHelper<char>.TryCreate<nuint>(unchecked((nuint)0xFFFFFFFF), out result));
                Assert.Equal((char)0x0000, result);
            }
        }

        [Fact]
        public static void GetByteCountTest()
        {
            Assert.Equal(2, BinaryIntegerHelper<char>.GetByteCount((char)0x0000));
            Assert.Equal(2, BinaryIntegerHelper<char>.GetByteCount((char)0x0001));
            Assert.Equal(2, BinaryIntegerHelper<char>.GetByteCount((char)0x7FFF));
            Assert.Equal(2, BinaryIntegerHelper<char>.GetByteCount((char)0x8000));
            Assert.Equal(2, BinaryIntegerHelper<char>.GetByteCount((char)0xFFFF));
        }

        [Fact]
        public static void TryWriteLittleEndianTest()
        {
            Span<byte> destination = stackalloc byte[2];
            int bytesWritten = 0;

            Assert.True(BinaryIntegerHelper<char>.TryWriteLittleEndian((char)0x0000, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x00 }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<char>.TryWriteLittleEndian((char)0x0001, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0x01, 0x00 }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<char>.TryWriteLittleEndian((char)0x7FFF, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0x7F }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<char>.TryWriteLittleEndian((char)0x8000, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x80 }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<char>.TryWriteLittleEndian((char)0xFFFF, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0xFF }, destination.ToArray());

            Assert.False(BinaryIntegerHelper<char>.TryWriteLittleEndian(default, Span<byte>.Empty, out bytesWritten));
            Assert.Equal(0, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0xFF }, destination.ToArray());
        }

        [Fact]
        public static void op_LeftShiftTest()
        {
            Assert.Equal((char)0x0000, ShiftOperatorsHelper<char, char>.op_LeftShift((char)0x0000, 1));
            Assert.Equal((char)0x0002, ShiftOperatorsHelper<char, char>.op_LeftShift((char)0x0001, 1));
            Assert.Equal((char)0xFFFE, ShiftOperatorsHelper<char, char>.op_LeftShift((char)0x7FFF, 1));
            Assert.Equal((char)0x0000, ShiftOperatorsHelper<char, char>.op_LeftShift((char)0x8000, 1));
            Assert.Equal((char)0xFFFE, ShiftOperatorsHelper<char, char>.op_LeftShift((char)0xFFFF, 1));
        }

        [Fact]
        public static void op_RightShiftTest()
        {
            Assert.Equal((char)0x0000, ShiftOperatorsHelper<char, char>.op_RightShift((char)0x0000, 1));
            Assert.Equal((char)0x0000, ShiftOperatorsHelper<char, char>.op_RightShift((char)0x0001, 1));
            Assert.Equal((char)0x3FFF, ShiftOperatorsHelper<char, char>.op_RightShift((char)0x7FFF, 1));
            Assert.Equal((char)0x4000, ShiftOperatorsHelper<char, char>.op_RightShift((char)0x8000, 1));
            Assert.Equal((char)0x7FFF, ShiftOperatorsHelper<char, char>.op_RightShift((char)0xFFFF, 1));
        }

        [Fact]
        public static void op_SubtractionTest()
        {
            Assert.Equal((char)0xFFFF, SubtractionOperatorsHelper<char, char, char>.op_Subtraction((char)0x0000, (char)1));
            Assert.Equal((char)0x0000, SubtractionOperatorsHelper<char, char, char>.op_Subtraction((char)0x0001, (char)1));
            Assert.Equal((char)0x7FFE, SubtractionOperatorsHelper<char, char, char>.op_Subtraction((char)0x7FFF, (char)1));
            Assert.Equal((char)0x7FFF, SubtractionOperatorsHelper<char, char, char>.op_Subtraction((char)0x8000, (char)1));
            Assert.Equal((char)0xFFFE, SubtractionOperatorsHelper<char, char, char>.op_Subtraction((char)0xFFFF, (char)1));
        }

        [Fact]
        public static void op_CheckedSubtractionTest()
        {
            Assert.Equal((char)0x0000, SubtractionOperatorsHelper<char, char, char>.op_CheckedSubtraction((char)0x0001, (char)1));
            Assert.Equal((char)0x7FFE, SubtractionOperatorsHelper<char, char, char>.op_CheckedSubtraction((char)0x7FFF, (char)1));
            Assert.Equal((char)0x7FFF, SubtractionOperatorsHelper<char, char, char>.op_CheckedSubtraction((char)0x8000, (char)1));
            Assert.Equal((char)0xFFFE, SubtractionOperatorsHelper<char, char, char>.op_CheckedSubtraction((char)0xFFFF, (char)1));

            Assert.Throws<OverflowException>(() => SubtractionOperatorsHelper<char, char, char>.op_CheckedSubtraction((char)0x0000, (char)1));
        }

        [Fact]
        public static void op_UnaryNegationTest()
        {
            Assert.Equal((char)0x0000, UnaryNegationOperatorsHelper<char, char>.op_UnaryNegation((char)0x0000));
            Assert.Equal((char)0xFFFF, UnaryNegationOperatorsHelper<char, char>.op_UnaryNegation((char)0x0001));
            Assert.Equal((char)0x8001, UnaryNegationOperatorsHelper<char, char>.op_UnaryNegation((char)0x7FFF));
            Assert.Equal((char)0x8000, UnaryNegationOperatorsHelper<char, char>.op_UnaryNegation((char)0x8000));
            Assert.Equal((char)0x0001, UnaryNegationOperatorsHelper<char, char>.op_UnaryNegation((char)0xFFFF));
        }

        [Fact]
        public static void op_CheckedUnaryNegationTest()
        {
            Assert.Equal((char)0x0000, UnaryNegationOperatorsHelper<char, char>.op_CheckedUnaryNegation((char)0x0000));

            Assert.Throws<OverflowException>(() => UnaryNegationOperatorsHelper<char, char>.op_CheckedUnaryNegation((char)0x0001));
            Assert.Throws<OverflowException>(() => UnaryNegationOperatorsHelper<char, char>.op_CheckedUnaryNegation((char)0x7FFF));
            Assert.Throws<OverflowException>(() => UnaryNegationOperatorsHelper<char, char>.op_CheckedUnaryNegation((char)0x8000));
            Assert.Throws<OverflowException>(() => UnaryNegationOperatorsHelper<char, char>.op_CheckedUnaryNegation((char)0xFFFF));
        }

        [Fact]
        public static void op_UnaryPlusTest()
        {
            Assert.Equal((char)0x0000, UnaryPlusOperatorsHelper<char, char>.op_UnaryPlus((char)0x0000));
            Assert.Equal((char)0x0001, UnaryPlusOperatorsHelper<char, char>.op_UnaryPlus((char)0x0001));
            Assert.Equal((char)0x7FFF, UnaryPlusOperatorsHelper<char, char>.op_UnaryPlus((char)0x7FFF));
            Assert.Equal((char)0x8000, UnaryPlusOperatorsHelper<char, char>.op_UnaryPlus((char)0x8000));
            Assert.Equal((char)0xFFFF, UnaryPlusOperatorsHelper<char, char>.op_UnaryPlus((char)0xFFFF));
        }
    }
}
