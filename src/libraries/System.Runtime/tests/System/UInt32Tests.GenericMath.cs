// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Xunit;

namespace System.Tests
{
    public class UInt32Tests_GenericMath
    {
        [Fact]
        public static void AdditiveIdentityTest()
        {
            Assert.Equal((uint)0x00000000, AdditiveIdentityHelper<uint, uint>.AdditiveIdentity);
        }

        [Fact]
        public static void MinValueTest()
        {
            Assert.Equal((uint)0x00000000, MinMaxValueHelper<uint>.MinValue);
        }

        [Fact]
        public static void MaxValueTest()
        {
            Assert.Equal((uint)0xFFFFFFFF, MinMaxValueHelper<uint>.MaxValue);
        }

        [Fact]
        public static void MultiplicativeIdentityTest()
        {
            Assert.Equal((uint)0x00000001, MultiplicativeIdentityHelper<uint, uint>.MultiplicativeIdentity);
        }

        [Fact]
        public static void OneTest()
        {
            Assert.Equal((uint)0x00000001, NumberBaseHelper<uint>.One);
        }

        [Fact]
        public static void ZeroTest()
        {
            Assert.Equal((uint)0x00000000, NumberBaseHelper<uint>.Zero);
        }

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
        public static void GetShortestBitLengthTest()
        {
            Assert.Equal(0x00, BinaryIntegerHelper<uint>.GetShortestBitLength((uint)0x00000000));
            Assert.Equal(0x01, BinaryIntegerHelper<uint>.GetShortestBitLength((uint)0x00000001));
            Assert.Equal(0x1F, BinaryIntegerHelper<uint>.GetShortestBitLength((uint)0x7FFFFFFF));
            Assert.Equal(0x20, BinaryIntegerHelper<uint>.GetShortestBitLength((uint)0x80000000));
            Assert.Equal(0x20, BinaryIntegerHelper<uint>.GetShortestBitLength((uint)0xFFFFFFFF));
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

        [Fact]
        public static void op_LessThanTest()
        {
            Assert.True(ComparisonOperatorsHelper<uint, uint>.op_LessThan((uint)0x00000000, 1));
            Assert.False(ComparisonOperatorsHelper<uint, uint>.op_LessThan((uint)0x00000001, 1));
            Assert.False(ComparisonOperatorsHelper<uint, uint>.op_LessThan((uint)0x7FFFFFFF, 1));
            Assert.False(ComparisonOperatorsHelper<uint, uint>.op_LessThan((uint)0x80000000, 1));
            Assert.False(ComparisonOperatorsHelper<uint, uint>.op_LessThan((uint)0xFFFFFFFF, 1));
        }

        [Fact]
        public static void op_LessThanOrEqualTest()
        {
            Assert.True(ComparisonOperatorsHelper<uint, uint>.op_LessThanOrEqual((uint)0x00000000, 1));
            Assert.True(ComparisonOperatorsHelper<uint, uint>.op_LessThanOrEqual((uint)0x00000001, 1));
            Assert.False(ComparisonOperatorsHelper<uint, uint>.op_LessThanOrEqual((uint)0x7FFFFFFF, 1));
            Assert.False(ComparisonOperatorsHelper<uint, uint>.op_LessThanOrEqual((uint)0x80000000, 1));
            Assert.False(ComparisonOperatorsHelper<uint, uint>.op_LessThanOrEqual((uint)0xFFFFFFFF, 1));
        }

        [Fact]
        public static void op_GreaterThanTest()
        {
            Assert.False(ComparisonOperatorsHelper<uint, uint>.op_GreaterThan((uint)0x00000000, 1));
            Assert.False(ComparisonOperatorsHelper<uint, uint>.op_GreaterThan((uint)0x00000001, 1));
            Assert.True(ComparisonOperatorsHelper<uint, uint>.op_GreaterThan((uint)0x7FFFFFFF, 1));
            Assert.True(ComparisonOperatorsHelper<uint, uint>.op_GreaterThan((uint)0x80000000, 1));
            Assert.True(ComparisonOperatorsHelper<uint, uint>.op_GreaterThan((uint)0xFFFFFFFF, 1));
        }

        [Fact]
        public static void op_GreaterThanOrEqualTest()
        {
            Assert.False(ComparisonOperatorsHelper<uint, uint>.op_GreaterThanOrEqual((uint)0x00000000, 1));
            Assert.True(ComparisonOperatorsHelper<uint, uint>.op_GreaterThanOrEqual((uint)0x00000001, 1));
            Assert.True(ComparisonOperatorsHelper<uint, uint>.op_GreaterThanOrEqual((uint)0x7FFFFFFF, 1));
            Assert.True(ComparisonOperatorsHelper<uint, uint>.op_GreaterThanOrEqual((uint)0x80000000, 1));
            Assert.True(ComparisonOperatorsHelper<uint, uint>.op_GreaterThanOrEqual((uint)0xFFFFFFFF, 1));
        }

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

        [Fact]
        public static void op_DivisionTest()
        {
            Assert.Equal((uint)0x00000000, DivisionOperatorsHelper<uint, uint, uint>.op_Division((uint)0x00000000, 2));
            Assert.Equal((uint)0x00000000, DivisionOperatorsHelper<uint, uint, uint>.op_Division((uint)0x00000001, 2));
            Assert.Equal((uint)0x3FFFFFFF, DivisionOperatorsHelper<uint, uint, uint>.op_Division((uint)0x7FFFFFFF, 2));
            Assert.Equal((uint)0x40000000, DivisionOperatorsHelper<uint, uint, uint>.op_Division((uint)0x80000000, 2));
            Assert.Equal((uint)0x7FFFFFFF, DivisionOperatorsHelper<uint, uint, uint>.op_Division((uint)0xFFFFFFFF, 2));

            Assert.Throws<DivideByZeroException>(() => DivisionOperatorsHelper<uint, uint, uint>.op_Division((uint)0x00000001, 0));
        }

        [Fact]
        public static void op_CheckedDivisionTest()
        {
            Assert.Equal((uint)0x00000000, DivisionOperatorsHelper<uint, uint, uint>.op_CheckedDivision((uint)0x00000000, 2));
            Assert.Equal((uint)0x00000000, DivisionOperatorsHelper<uint, uint, uint>.op_CheckedDivision((uint)0x00000001, 2));
            Assert.Equal((uint)0x3FFFFFFF, DivisionOperatorsHelper<uint, uint, uint>.op_CheckedDivision((uint)0x7FFFFFFF, 2));
            Assert.Equal((uint)0x40000000, DivisionOperatorsHelper<uint, uint, uint>.op_CheckedDivision((uint)0x80000000, 2));
            Assert.Equal((uint)0x7FFFFFFF, DivisionOperatorsHelper<uint, uint, uint>.op_CheckedDivision((uint)0xFFFFFFFF, 2));

            Assert.Throws<DivideByZeroException>(() => DivisionOperatorsHelper<uint, uint, uint>.op_CheckedDivision((uint)0x00000001, 0));
        }

        [Fact]
        public static void op_EqualityTest()
        {
            Assert.False(EqualityOperatorsHelper<uint, uint>.op_Equality((uint)0x00000000, 1));
            Assert.True(EqualityOperatorsHelper<uint, uint>.op_Equality((uint)0x00000001, 1));
            Assert.False(EqualityOperatorsHelper<uint, uint>.op_Equality((uint)0x7FFFFFFF, 1));
            Assert.False(EqualityOperatorsHelper<uint, uint>.op_Equality((uint)0x80000000, 1));
            Assert.False(EqualityOperatorsHelper<uint, uint>.op_Equality((uint)0xFFFFFFFF, 1));
        }

        [Fact]
        public static void op_InequalityTest()
        {
            Assert.True(EqualityOperatorsHelper<uint, uint>.op_Inequality((uint)0x00000000, 1));
            Assert.False(EqualityOperatorsHelper<uint, uint>.op_Inequality((uint)0x00000001, 1));
            Assert.True(EqualityOperatorsHelper<uint, uint>.op_Inequality((uint)0x7FFFFFFF, 1));
            Assert.True(EqualityOperatorsHelper<uint, uint>.op_Inequality((uint)0x80000000, 1));
            Assert.True(EqualityOperatorsHelper<uint, uint>.op_Inequality((uint)0xFFFFFFFF, 1));
        }

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

        [Fact]
        public static void AbsTest()
        {
            Assert.Equal((uint)0x00000000, NumberHelper<uint>.Abs((uint)0x00000000));
            Assert.Equal((uint)0x00000001, NumberHelper<uint>.Abs((uint)0x00000001));
            Assert.Equal((uint)0x7FFFFFFF, NumberHelper<uint>.Abs((uint)0x7FFFFFFF));
            Assert.Equal((uint)0x80000000, NumberHelper<uint>.Abs((uint)0x80000000));
            Assert.Equal((uint)0xFFFFFFFF, NumberHelper<uint>.Abs((uint)0xFFFFFFFF));
        }

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
        public static void CreateCheckedFromByteTest()
        {
            Assert.Equal((uint)0x00000000, NumberHelper<uint>.CreateChecked<byte>(0x00));
            Assert.Equal((uint)0x00000001, NumberHelper<uint>.CreateChecked<byte>(0x01));
            Assert.Equal((uint)0x0000007F, NumberHelper<uint>.CreateChecked<byte>(0x7F));
            Assert.Equal((uint)0x00000080, NumberHelper<uint>.CreateChecked<byte>(0x80));
            Assert.Equal((uint)0x000000FF, NumberHelper<uint>.CreateChecked<byte>(0xFF));
        }

        [Fact]
        public static void CreateCheckedFromCharTest()
        {
            Assert.Equal((uint)0x00000000, NumberHelper<uint>.CreateChecked<char>((char)0x0000));
            Assert.Equal((uint)0x00000001, NumberHelper<uint>.CreateChecked<char>((char)0x0001));
            Assert.Equal((uint)0x00007FFF, NumberHelper<uint>.CreateChecked<char>((char)0x7FFF));
            Assert.Equal((uint)0x00008000, NumberHelper<uint>.CreateChecked<char>((char)0x8000));
            Assert.Equal((uint)0x0000FFFF, NumberHelper<uint>.CreateChecked<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateCheckedFromInt16Test()
        {
            Assert.Equal((uint)0x00000000, NumberHelper<uint>.CreateChecked<short>(0x0000));
            Assert.Equal((uint)0x00000001, NumberHelper<uint>.CreateChecked<short>(0x0001));
            Assert.Equal((uint)0x00007FFF, NumberHelper<uint>.CreateChecked<short>(0x7FFF));
            Assert.Throws<OverflowException>(() => NumberHelper<uint>.CreateChecked<short>(unchecked((short)0x8000)));
            Assert.Throws<OverflowException>(() => NumberHelper<uint>.CreateChecked<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromInt32Test()
        {
            Assert.Equal((uint)0x00000000, NumberHelper<uint>.CreateChecked<int>(0x00000000));
            Assert.Equal((uint)0x00000001, NumberHelper<uint>.CreateChecked<int>(0x00000001));
            Assert.Equal((uint)0x7FFFFFFF, NumberHelper<uint>.CreateChecked<int>(0x7FFFFFFF));
            Assert.Throws<OverflowException>(() => NumberHelper<uint>.CreateChecked<int>(unchecked((int)0x80000000)));
            Assert.Throws<OverflowException>(() => NumberHelper<uint>.CreateChecked<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromInt64Test()
        {
            Assert.Equal((uint)0x00000000, NumberHelper<uint>.CreateChecked<long>(0x0000000000000000));
            Assert.Equal((uint)0x00000001, NumberHelper<uint>.CreateChecked<long>(0x0000000000000001));
            Assert.Throws<OverflowException>(() => NumberHelper<uint>.CreateChecked<long>(0x7FFFFFFFFFFFFFFF));
            Assert.Throws<OverflowException>(() => NumberHelper<uint>.CreateChecked<long>(unchecked((long)0x8000000000000000)));
            Assert.Throws<OverflowException>(() => NumberHelper<uint>.CreateChecked<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((uint)0x00000000, NumberHelper<uint>.CreateChecked<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal((uint)0x00000001, NumberHelper<uint>.CreateChecked<nint>(unchecked((nint)0x0000000000000001)));
                Assert.Throws<OverflowException>(() => NumberHelper<uint>.CreateChecked<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Throws<OverflowException>(() => NumberHelper<uint>.CreateChecked<nint>(unchecked((nint)0x8000000000000000)));
                Assert.Throws<OverflowException>(() => NumberHelper<uint>.CreateChecked<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((uint)0x00000000, NumberHelper<uint>.CreateChecked<nint>((nint)0x00000000));
                Assert.Equal((uint)0x00000001, NumberHelper<uint>.CreateChecked<nint>((nint)0x00000001));
                Assert.Equal((uint)0x7FFFFFFF, NumberHelper<uint>.CreateChecked<nint>((nint)0x7FFFFFFF));
                Assert.Throws<OverflowException>(() => NumberHelper<uint>.CreateChecked<nint>(unchecked((nint)0x80000000)));
                Assert.Throws<OverflowException>(() => NumberHelper<uint>.CreateChecked<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateCheckedFromSByteTest()
        {
            Assert.Equal((uint)0x00000000, NumberHelper<uint>.CreateChecked<sbyte>(0x00));
            Assert.Equal((uint)0x00000001, NumberHelper<uint>.CreateChecked<sbyte>(0x01));
            Assert.Equal((uint)0x0000007F, NumberHelper<uint>.CreateChecked<sbyte>(0x7F));
            Assert.Throws<OverflowException>(() => NumberHelper<uint>.CreateChecked<sbyte>(unchecked((sbyte)0x80)));
            Assert.Throws<OverflowException>(() => NumberHelper<uint>.CreateChecked<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateCheckedFromUInt16Test()
        {
            Assert.Equal((uint)0x00000000, NumberHelper<uint>.CreateChecked<ushort>(0x0000));
            Assert.Equal((uint)0x00000001, NumberHelper<uint>.CreateChecked<ushort>(0x0001));
            Assert.Equal((uint)0x00007FFF, NumberHelper<uint>.CreateChecked<ushort>(0x7FFF));
            Assert.Equal((uint)0x00008000, NumberHelper<uint>.CreateChecked<ushort>(0x8000));
            Assert.Equal((uint)0x0000FFFF, NumberHelper<uint>.CreateChecked<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUInt32Test()
        {
            Assert.Equal((uint)0x00000000, NumberHelper<uint>.CreateChecked<uint>(0x00000000));
            Assert.Equal((uint)0x00000001, NumberHelper<uint>.CreateChecked<uint>(0x00000001));
            Assert.Equal((uint)0x7FFFFFFF, NumberHelper<uint>.CreateChecked<uint>(0x7FFFFFFF));
            Assert.Equal((uint)0x80000000, NumberHelper<uint>.CreateChecked<uint>(0x80000000));
            Assert.Equal((uint)0xFFFFFFFF, NumberHelper<uint>.CreateChecked<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUInt64Test()
        {
            Assert.Equal((uint)0x00000000, NumberHelper<uint>.CreateChecked<ulong>(0x0000000000000000));
            Assert.Equal((uint)0x00000001, NumberHelper<uint>.CreateChecked<ulong>(0x0000000000000001));
            Assert.Throws<OverflowException>(() => NumberHelper<uint>.CreateChecked<ulong>(0x7FFFFFFFFFFFFFFF));
            Assert.Throws<OverflowException>(() => NumberHelper<uint>.CreateChecked<ulong>(0x8000000000000000));
            Assert.Throws<OverflowException>(() => NumberHelper<uint>.CreateChecked<ulong>(0xFFFFFFFFFFFFFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((uint)0x00000000, NumberHelper<uint>.CreateChecked<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal((uint)0x00000001, NumberHelper<uint>.CreateChecked<nuint>(unchecked((nuint)0x0000000000000001)));
                Assert.Throws<OverflowException>(() => NumberHelper<uint>.CreateChecked<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Throws<OverflowException>(() => NumberHelper<uint>.CreateChecked<nuint>(unchecked((nuint)0x8000000000000000)));
                Assert.Throws<OverflowException>(() => NumberHelper<uint>.CreateChecked<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((uint)0x00000000, NumberHelper<uint>.CreateChecked<nuint>((nuint)0x00000000));
                Assert.Equal((uint)0x00000001, NumberHelper<uint>.CreateChecked<nuint>((nuint)0x00000001));
                Assert.Equal((uint)0x7FFFFFFF, NumberHelper<uint>.CreateChecked<nuint>((nuint)0x7FFFFFFF));
                Assert.Equal((uint)0x80000000, NumberHelper<uint>.CreateChecked<nuint>((nuint)0x80000000));
                Assert.Equal((uint)0xFFFFFFFF, NumberHelper<uint>.CreateChecked<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateSaturatingFromByteTest()
        {
            Assert.Equal((uint)0x00000000, NumberHelper<uint>.CreateSaturating<byte>(0x00));
            Assert.Equal((uint)0x00000001, NumberHelper<uint>.CreateSaturating<byte>(0x01));
            Assert.Equal((uint)0x0000007F, NumberHelper<uint>.CreateSaturating<byte>(0x7F));
            Assert.Equal((uint)0x00000080, NumberHelper<uint>.CreateSaturating<byte>(0x80));
            Assert.Equal((uint)0x000000FF, NumberHelper<uint>.CreateSaturating<byte>(0xFF));
        }

        [Fact]
        public static void CreateSaturatingFromCharTest()
        {
            Assert.Equal((uint)0x00000000, NumberHelper<uint>.CreateSaturating<char>((char)0x0000));
            Assert.Equal((uint)0x00000001, NumberHelper<uint>.CreateSaturating<char>((char)0x0001));
            Assert.Equal((uint)0x00007FFF, NumberHelper<uint>.CreateSaturating<char>((char)0x7FFF));
            Assert.Equal((uint)0x00008000, NumberHelper<uint>.CreateSaturating<char>((char)0x8000));
            Assert.Equal((uint)0x0000FFFF, NumberHelper<uint>.CreateSaturating<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromInt16Test()
        {
            Assert.Equal((uint)0x00000000, NumberHelper<uint>.CreateSaturating<short>(0x0000));
            Assert.Equal((uint)0x00000001, NumberHelper<uint>.CreateSaturating<short>(0x0001));
            Assert.Equal((uint)0x00007FFF, NumberHelper<uint>.CreateSaturating<short>(0x7FFF));
            Assert.Equal((uint)0x00000000, NumberHelper<uint>.CreateSaturating<short>(unchecked((short)0x8000)));
            Assert.Equal((uint)0x00000000, NumberHelper<uint>.CreateSaturating<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromInt32Test()
        {
            Assert.Equal((uint)0x00000000, NumberHelper<uint>.CreateSaturating<int>(0x00000000));
            Assert.Equal((uint)0x00000001, NumberHelper<uint>.CreateSaturating<int>(0x00000001));
            Assert.Equal((uint)0x7FFFFFFF, NumberHelper<uint>.CreateSaturating<int>(0x7FFFFFFF));
            Assert.Equal((uint)0x00000000, NumberHelper<uint>.CreateSaturating<int>(unchecked((int)0x80000000)));
            Assert.Equal((uint)0x00000000, NumberHelper<uint>.CreateSaturating<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromInt64Test()
        {
            Assert.Equal((uint)0x00000000, NumberHelper<uint>.CreateSaturating<long>(0x0000000000000000));
            Assert.Equal((uint)0x00000001, NumberHelper<uint>.CreateSaturating<long>(0x0000000000000001));
            Assert.Equal((uint)0xFFFFFFFF, NumberHelper<uint>.CreateSaturating<long>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal((uint)0x00000000, NumberHelper<uint>.CreateSaturating<long>(unchecked((long)0x8000000000000000)));
            Assert.Equal((uint)0x00000000, NumberHelper<uint>.CreateSaturating<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((uint)0x00000000, NumberHelper<uint>.CreateSaturating<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal((uint)0x00000001, NumberHelper<uint>.CreateSaturating<nint>(unchecked((nint)0x0000000000000001)));
                Assert.Equal((uint)0xFFFFFFFF, NumberHelper<uint>.CreateSaturating<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal((uint)0x00000000, NumberHelper<uint>.CreateSaturating<nint>(unchecked((nint)0x8000000000000000)));
                Assert.Equal((uint)0x00000000, NumberHelper<uint>.CreateSaturating<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((uint)0x00000000, NumberHelper<uint>.CreateSaturating<nint>((nint)0x00000000));
                Assert.Equal((uint)0x00000001, NumberHelper<uint>.CreateSaturating<nint>((nint)0x00000001));
                Assert.Equal((uint)0x7FFFFFFF, NumberHelper<uint>.CreateSaturating<nint>((nint)0x7FFFFFFF));
                Assert.Equal((uint)0x00000000, NumberHelper<uint>.CreateSaturating<nint>(unchecked((nint)0x80000000)));
                Assert.Equal((uint)0x00000000, NumberHelper<uint>.CreateSaturating<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateSaturatingFromSByteTest()
        {
            Assert.Equal((uint)0x00000000, NumberHelper<uint>.CreateSaturating<sbyte>(0x00));
            Assert.Equal((uint)0x00000001, NumberHelper<uint>.CreateSaturating<sbyte>(0x01));
            Assert.Equal((uint)0x0000007F, NumberHelper<uint>.CreateSaturating<sbyte>(0x7F));
            Assert.Equal((uint)0x00000000, NumberHelper<uint>.CreateSaturating<sbyte>(unchecked((sbyte)0x80)));
            Assert.Equal((uint)0x00000000, NumberHelper<uint>.CreateSaturating<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateSaturatingFromUInt16Test()
        {
            Assert.Equal((uint)0x00000000, NumberHelper<uint>.CreateSaturating<ushort>(0x0000));
            Assert.Equal((uint)0x00000001, NumberHelper<uint>.CreateSaturating<ushort>(0x0001));
            Assert.Equal((uint)0x00007FFF, NumberHelper<uint>.CreateSaturating<ushort>(0x7FFF));
            Assert.Equal((uint)0x00008000, NumberHelper<uint>.CreateSaturating<ushort>(0x8000));
            Assert.Equal((uint)0x0000FFFF, NumberHelper<uint>.CreateSaturating<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUInt32Test()
        {
            Assert.Equal((uint)0x00000000, NumberHelper<uint>.CreateSaturating<uint>(0x00000000));
            Assert.Equal((uint)0x00000001, NumberHelper<uint>.CreateSaturating<uint>(0x00000001));
            Assert.Equal((uint)0x7FFFFFFF, NumberHelper<uint>.CreateSaturating<uint>(0x7FFFFFFF));
            Assert.Equal((uint)0x80000000, NumberHelper<uint>.CreateSaturating<uint>(0x80000000));
            Assert.Equal((uint)0xFFFFFFFF, NumberHelper<uint>.CreateSaturating<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUInt64Test()
        {
            Assert.Equal((uint)0x00000000, NumberHelper<uint>.CreateSaturating<ulong>(0x0000000000000000));
            Assert.Equal((uint)0x00000001, NumberHelper<uint>.CreateSaturating<ulong>(0x0000000000000001));
            Assert.Equal((uint)0xFFFFFFFF, NumberHelper<uint>.CreateSaturating<ulong>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal((uint)0xFFFFFFFF, NumberHelper<uint>.CreateSaturating<ulong>(0x8000000000000000));
            Assert.Equal((uint)0xFFFFFFFF, NumberHelper<uint>.CreateSaturating<ulong>(0xFFFFFFFFFFFFFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((uint)0x00000000, NumberHelper<uint>.CreateSaturating<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal((uint)0x00000001, NumberHelper<uint>.CreateSaturating<nuint>(unchecked((nuint)0x0000000000000001)));
                Assert.Equal((uint)0xFFFFFFFF, NumberHelper<uint>.CreateSaturating<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal((uint)0xFFFFFFFF, NumberHelper<uint>.CreateSaturating<nuint>(unchecked((nuint)0x8000000000000000)));
                Assert.Equal((uint)0xFFFFFFFF, NumberHelper<uint>.CreateSaturating<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((uint)0x00000000, NumberHelper<uint>.CreateSaturating<nuint>((nuint)0x00000000));
                Assert.Equal((uint)0x00000001, NumberHelper<uint>.CreateSaturating<nuint>((nuint)0x00000001));
                Assert.Equal((uint)0x7FFFFFFF, NumberHelper<uint>.CreateSaturating<nuint>((nuint)0x7FFFFFFF));
                Assert.Equal((uint)0x80000000, NumberHelper<uint>.CreateSaturating<nuint>((nuint)0x80000000));
                Assert.Equal((uint)0xFFFFFFFF, NumberHelper<uint>.CreateSaturating<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateTruncatingFromByteTest()
        {
            Assert.Equal((uint)0x00000000, NumberHelper<uint>.CreateTruncating<byte>(0x00));
            Assert.Equal((uint)0x00000001, NumberHelper<uint>.CreateTruncating<byte>(0x01));
            Assert.Equal((uint)0x0000007F, NumberHelper<uint>.CreateTruncating<byte>(0x7F));
            Assert.Equal((uint)0x00000080, NumberHelper<uint>.CreateTruncating<byte>(0x80));
            Assert.Equal((uint)0x000000FF, NumberHelper<uint>.CreateTruncating<byte>(0xFF));
        }

        [Fact]
        public static void CreateTruncatingFromCharTest()
        {
            Assert.Equal((uint)0x00000000, NumberHelper<uint>.CreateTruncating<char>((char)0x0000));
            Assert.Equal((uint)0x00000001, NumberHelper<uint>.CreateTruncating<char>((char)0x0001));
            Assert.Equal((uint)0x00007FFF, NumberHelper<uint>.CreateTruncating<char>((char)0x7FFF));
            Assert.Equal((uint)0x00008000, NumberHelper<uint>.CreateTruncating<char>((char)0x8000));
            Assert.Equal((uint)0x0000FFFF, NumberHelper<uint>.CreateTruncating<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromInt16Test()
        {
            Assert.Equal((uint)0x00000000, NumberHelper<uint>.CreateTruncating<short>(0x0000));
            Assert.Equal((uint)0x00000001, NumberHelper<uint>.CreateTruncating<short>(0x0001));
            Assert.Equal((uint)0x00007FFF, NumberHelper<uint>.CreateTruncating<short>(0x7FFF));
            Assert.Equal((uint)0xFFFF8000, NumberHelper<uint>.CreateTruncating<short>(unchecked((short)0x8000)));
            Assert.Equal((uint)0xFFFFFFFF, NumberHelper<uint>.CreateTruncating<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateTruncatingFromInt32Test()
        {
            Assert.Equal((uint)0x00000000, NumberHelper<uint>.CreateTruncating<int>(0x00000000));
            Assert.Equal((uint)0x00000001, NumberHelper<uint>.CreateTruncating<int>(0x00000001));
            Assert.Equal((uint)0x7FFFFFFF, NumberHelper<uint>.CreateTruncating<int>(0x7FFFFFFF));
            Assert.Equal((uint)0x80000000, NumberHelper<uint>.CreateTruncating<int>(unchecked((int)0x80000000)));
            Assert.Equal((uint)0xFFFFFFFF, NumberHelper<uint>.CreateTruncating<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateTruncatingFromInt64Test()
        {
            Assert.Equal((uint)0x00000000, NumberHelper<uint>.CreateTruncating<long>(0x0000000000000000));
            Assert.Equal((uint)0x00000001, NumberHelper<uint>.CreateTruncating<long>(0x0000000000000001));
            Assert.Equal((uint)0xFFFFFFFF, NumberHelper<uint>.CreateTruncating<long>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal((uint)0x00000000, NumberHelper<uint>.CreateTruncating<long>(unchecked((long)0x8000000000000000)));
            Assert.Equal((uint)0xFFFFFFFF, NumberHelper<uint>.CreateTruncating<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void CreateTruncatingFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((uint)0x00000000, NumberHelper<uint>.CreateTruncating<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal((uint)0x00000001, NumberHelper<uint>.CreateTruncating<nint>(unchecked((nint)0x0000000000000001)));
                Assert.Equal((uint)0xFFFFFFFF, NumberHelper<uint>.CreateTruncating<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal((uint)0x00000000, NumberHelper<uint>.CreateTruncating<nint>(unchecked((nint)0x8000000000000000)));
                Assert.Equal((uint)0xFFFFFFFF, NumberHelper<uint>.CreateTruncating<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((uint)0x00000000, NumberHelper<uint>.CreateTruncating<nint>((nint)0x00000000));
                Assert.Equal((uint)0x00000001, NumberHelper<uint>.CreateTruncating<nint>((nint)0x00000001));
                Assert.Equal((uint)0x7FFFFFFF, NumberHelper<uint>.CreateTruncating<nint>((nint)0x7FFFFFFF));
                Assert.Equal((uint)0x80000000, NumberHelper<uint>.CreateTruncating<nint>(unchecked((nint)0x80000000)));
                Assert.Equal((uint)0xFFFFFFFF, NumberHelper<uint>.CreateTruncating<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateTruncatingFromSByteTest()
        {
            Assert.Equal((uint)0x00000000, NumberHelper<uint>.CreateTruncating<sbyte>(0x00));
            Assert.Equal((uint)0x00000001, NumberHelper<uint>.CreateTruncating<sbyte>(0x01));
            Assert.Equal((uint)0x0000007F, NumberHelper<uint>.CreateTruncating<sbyte>(0x7F));
            Assert.Equal((uint)0xFFFFFF80, NumberHelper<uint>.CreateTruncating<sbyte>(unchecked((sbyte)0x80)));
            Assert.Equal((uint)0xFFFFFFFF, NumberHelper<uint>.CreateTruncating<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateTruncatingFromUInt16Test()
        {
            Assert.Equal((uint)0x00000000, NumberHelper<uint>.CreateTruncating<ushort>(0x0000));
            Assert.Equal((uint)0x00000001, NumberHelper<uint>.CreateTruncating<ushort>(0x0001));
            Assert.Equal((uint)0x00007FFF, NumberHelper<uint>.CreateTruncating<ushort>(0x7FFF));
            Assert.Equal((uint)0x00008000, NumberHelper<uint>.CreateTruncating<ushort>(0x8000));
            Assert.Equal((uint)0x0000FFFF, NumberHelper<uint>.CreateTruncating<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt32Test()
        {
            Assert.Equal((uint)0x00000000, NumberHelper<uint>.CreateTruncating<uint>(0x00000000));
            Assert.Equal((uint)0x00000001, NumberHelper<uint>.CreateTruncating<uint>(0x00000001));
            Assert.Equal((uint)0x7FFFFFFF, NumberHelper<uint>.CreateTruncating<uint>(0x7FFFFFFF));
            Assert.Equal((uint)0x80000000, NumberHelper<uint>.CreateTruncating<uint>(0x80000000));
            Assert.Equal((uint)0xFFFFFFFF, NumberHelper<uint>.CreateTruncating<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt64Test()
        {
            Assert.Equal((uint)0x00000000, NumberHelper<uint>.CreateTruncating<ulong>(0x0000000000000000));
            Assert.Equal((uint)0x00000001, NumberHelper<uint>.CreateTruncating<ulong>(0x0000000000000001));
            Assert.Equal((uint)0xFFFFFFFF, NumberHelper<uint>.CreateTruncating<ulong>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal((uint)0x00000000, NumberHelper<uint>.CreateTruncating<ulong>(0x8000000000000000));
            Assert.Equal((uint)0xFFFFFFFF, NumberHelper<uint>.CreateTruncating<ulong>(0xFFFFFFFFFFFFFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((uint)0x00000000, NumberHelper<uint>.CreateTruncating<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal((uint)0x00000001, NumberHelper<uint>.CreateTruncating<nuint>(unchecked((nuint)0x0000000000000001)));
                Assert.Equal((uint)0xFFFFFFFF, NumberHelper<uint>.CreateTruncating<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal((uint)0x00000000, NumberHelper<uint>.CreateTruncating<nuint>(unchecked((nuint)0x8000000000000000)));
                Assert.Equal((uint)0xFFFFFFFF, NumberHelper<uint>.CreateTruncating<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((uint)0x00000000, NumberHelper<uint>.CreateTruncating<nuint>((nuint)0x00000000));
                Assert.Equal((uint)0x00000001, NumberHelper<uint>.CreateTruncating<nuint>((nuint)0x00000001));
                Assert.Equal((uint)0x7FFFFFFF, NumberHelper<uint>.CreateTruncating<nuint>((nuint)0x7FFFFFFF));
                Assert.Equal((uint)0x80000000, NumberHelper<uint>.CreateTruncating<nuint>((nuint)0x80000000));
                Assert.Equal((uint)0xFFFFFFFF, NumberHelper<uint>.CreateTruncating<nuint>((nuint)0xFFFFFFFF));
            }
        }

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
        public static void MaxTest()
        {
            Assert.Equal((uint)0x00000001, NumberHelper<uint>.Max((uint)0x00000000, 1));
            Assert.Equal((uint)0x00000001, NumberHelper<uint>.Max((uint)0x00000001, 1));
            Assert.Equal((uint)0x7FFFFFFF, NumberHelper<uint>.Max((uint)0x7FFFFFFF, 1));
            Assert.Equal((uint)0x80000000, NumberHelper<uint>.Max((uint)0x80000000, 1));
            Assert.Equal((uint)0xFFFFFFFF, NumberHelper<uint>.Max((uint)0xFFFFFFFF, 1));
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
        public static void SignTest()
        {
            Assert.Equal(0, NumberHelper<uint>.Sign((uint)0x00000000));
            Assert.Equal(1, NumberHelper<uint>.Sign((uint)0x00000001));
            Assert.Equal(1, NumberHelper<uint>.Sign((uint)0x7FFFFFFF));
            Assert.Equal(1, NumberHelper<uint>.Sign((uint)0x80000000));
            Assert.Equal(1, NumberHelper<uint>.Sign((uint)0xFFFFFFFF));
        }

        [Fact]
        public static void TryCreateFromByteTest()
        {
            uint result;

            Assert.True(NumberHelper<uint>.TryCreate<byte>(0x00, out result));
            Assert.Equal((uint)0x00000000, result);

            Assert.True(NumberHelper<uint>.TryCreate<byte>(0x01, out result));
            Assert.Equal((uint)0x00000001, result);

            Assert.True(NumberHelper<uint>.TryCreate<byte>(0x7F, out result));
            Assert.Equal((uint)0x0000007F, result);

            Assert.True(NumberHelper<uint>.TryCreate<byte>(0x80, out result));
            Assert.Equal((uint)0x00000080, result);

            Assert.True(NumberHelper<uint>.TryCreate<byte>(0xFF, out result));
            Assert.Equal((uint)0x000000FF, result);
        }

        [Fact]
        public static void TryCreateFromCharTest()
        {
            uint result;

            Assert.True(NumberHelper<uint>.TryCreate<char>((char)0x0000, out result));
            Assert.Equal((uint)0x00000000, result);

            Assert.True(NumberHelper<uint>.TryCreate<char>((char)0x0001, out result));
            Assert.Equal((uint)0x00000001, result);

            Assert.True(NumberHelper<uint>.TryCreate<char>((char)0x7FFF, out result));
            Assert.Equal((uint)0x00007FFF, result);

            Assert.True(NumberHelper<uint>.TryCreate<char>((char)0x8000, out result));
            Assert.Equal((uint)0x00008000, result);

            Assert.True(NumberHelper<uint>.TryCreate<char>((char)0xFFFF, out result));
            Assert.Equal((uint)0x0000FFFF, result);
        }

        [Fact]
        public static void TryCreateFromInt16Test()
        {
            uint result;

            Assert.True(NumberHelper<uint>.TryCreate<short>(0x0000, out result));
            Assert.Equal((uint)0x00000000, result);

            Assert.True(NumberHelper<uint>.TryCreate<short>(0x0001, out result));
            Assert.Equal((uint)0x00000001, result);

            Assert.True(NumberHelper<uint>.TryCreate<short>(0x7FFF, out result));
            Assert.Equal((uint)0x00007FFF, result);

            Assert.False(NumberHelper<uint>.TryCreate<short>(unchecked((short)0x8000), out result));
            Assert.Equal((uint)0x00000000, result);

            Assert.False(NumberHelper<uint>.TryCreate<short>(unchecked((short)0xFFFF), out result));
            Assert.Equal((uint)0x00000000, result);
        }

        [Fact]
        public static void TryCreateFromInt32Test()
        {
            uint result;

            Assert.True(NumberHelper<uint>.TryCreate<int>(0x00000000, out result));
            Assert.Equal((uint)0x00000000, result);

            Assert.True(NumberHelper<uint>.TryCreate<int>(0x00000001, out result));
            Assert.Equal((uint)0x00000001, result);

            Assert.True(NumberHelper<uint>.TryCreate<int>(0x7FFFFFFF, out result));
            Assert.Equal((uint)0x7FFFFFFF, result);

            Assert.False(NumberHelper<uint>.TryCreate<int>(unchecked((int)0x80000000), out result));
            Assert.Equal((uint)0x00000000, result);

            Assert.False(NumberHelper<uint>.TryCreate<int>(unchecked((int)0xFFFFFFFF), out result));
            Assert.Equal((uint)0x00000000, result);
        }

        [Fact]
        public static void TryCreateFromInt64Test()
        {
            uint result;

            Assert.True(NumberHelper<uint>.TryCreate<long>(0x0000000000000000, out result));
            Assert.Equal((uint)0x00000000, result);

            Assert.True(NumberHelper<uint>.TryCreate<long>(0x0000000000000001, out result));
            Assert.Equal((uint)0x00000001, result);

            Assert.False(NumberHelper<uint>.TryCreate<long>(0x7FFFFFFFFFFFFFFF, out result));
            Assert.Equal((uint)0x00000000, result);

            Assert.False(NumberHelper<uint>.TryCreate<long>(unchecked((long)0x8000000000000000), out result));
            Assert.Equal((uint)0x00000000, result);

            Assert.False(NumberHelper<uint>.TryCreate<long>(unchecked((long)0xFFFFFFFFFFFFFFFF), out result));
            Assert.Equal((uint)0x00000000, result);
        }

        [Fact]
        public static void TryCreateFromIntPtrTest()
        {
            uint result;

            if (Environment.Is64BitProcess)
            {
                Assert.True(NumberHelper<uint>.TryCreate<nint>(unchecked((nint)0x0000000000000000), out result));
                Assert.Equal((uint)0x00000000, result);

                Assert.True(NumberHelper<uint>.TryCreate<nint>(unchecked((nint)0x0000000000000001), out result));
                Assert.Equal((uint)0x00000001, result);

                Assert.False(NumberHelper<uint>.TryCreate<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF), out result));
                Assert.Equal((uint)0x00000000, result);

                Assert.False(NumberHelper<uint>.TryCreate<nint>(unchecked((nint)0x8000000000000000), out result));
                Assert.Equal((uint)0x00000000, result);

                Assert.False(NumberHelper<uint>.TryCreate<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF), out result));
                Assert.Equal((uint)0x00000000, result);
            }
            else
            {
                Assert.True(NumberHelper<uint>.TryCreate<nint>((nint)0x00000000, out result));
                Assert.Equal((uint)0x00000000, result);

                Assert.True(NumberHelper<uint>.TryCreate<nint>((nint)0x00000001, out result));
                Assert.Equal((uint)0x00000001, result);

                Assert.True(NumberHelper<uint>.TryCreate<nint>((nint)0x7FFFFFFF, out result));
                Assert.Equal((uint)0x7FFFFFFF, result);

                Assert.False(NumberHelper<uint>.TryCreate<nint>(unchecked((nint)0x80000000), out result));
                Assert.Equal((uint)0x00000000, result);

                Assert.False(NumberHelper<uint>.TryCreate<nint>(unchecked((nint)0xFFFFFFFF), out result));
                Assert.Equal((uint)0x00000000, result);
            }
        }

        [Fact]
        public static void TryCreateFromSByteTest()
        {
            uint result;

            Assert.True(NumberHelper<uint>.TryCreate<sbyte>(0x00, out result));
            Assert.Equal((uint)0x00000000, result);

            Assert.True(NumberHelper<uint>.TryCreate<sbyte>(0x01, out result));
            Assert.Equal((uint)0x00000001, result);

            Assert.True(NumberHelper<uint>.TryCreate<sbyte>(0x7F, out result));
            Assert.Equal((uint)0x0000007F, result);

            Assert.False(NumberHelper<uint>.TryCreate<sbyte>(unchecked((sbyte)0x80), out result));
            Assert.Equal((uint)0x00000000, result);

            Assert.False(NumberHelper<uint>.TryCreate<sbyte>(unchecked((sbyte)0xFF), out result));
            Assert.Equal((uint)0x00000000, result);
        }

        [Fact]
        public static void TryCreateFromUInt16Test()
        {
            uint result;

            Assert.True(NumberHelper<uint>.TryCreate<ushort>(0x0000, out result));
            Assert.Equal((uint)0x00000000, result);

            Assert.True(NumberHelper<uint>.TryCreate<ushort>(0x0001, out result));
            Assert.Equal((uint)0x00000001, result);

            Assert.True(NumberHelper<uint>.TryCreate<ushort>(0x7FFF, out result));
            Assert.Equal((uint)0x00007FFF, result);

            Assert.True(NumberHelper<uint>.TryCreate<ushort>(0x8000, out result));
            Assert.Equal((uint)0x00008000, result);

            Assert.True(NumberHelper<uint>.TryCreate<ushort>(0xFFFF, out result));
            Assert.Equal((uint)0x0000FFFF, result);
        }

        [Fact]
        public static void TryCreateFromUInt32Test()
        {
            uint result;

            Assert.True(NumberHelper<uint>.TryCreate<uint>(0x00000000, out result));
            Assert.Equal((uint)0x00000000, result);

            Assert.True(NumberHelper<uint>.TryCreate<uint>(0x00000001, out result));
            Assert.Equal((uint)0x00000001, result);

            Assert.True(NumberHelper<uint>.TryCreate<uint>(0x7FFFFFFF, out result));
            Assert.Equal((uint)0x7FFFFFFF, result);

            Assert.True(NumberHelper<uint>.TryCreate<uint>(0x80000000, out result));
            Assert.Equal((uint)0x80000000, result);

            Assert.True(NumberHelper<uint>.TryCreate<uint>(0xFFFFFFFF, out result));
            Assert.Equal((uint)0xFFFFFFFF, result);
        }

        [Fact]
        public static void TryCreateFromUInt64Test()
        {
            uint result;

            Assert.True(NumberHelper<uint>.TryCreate<ulong>(0x0000000000000000, out result));
            Assert.Equal((uint)0x00000000, result);

            Assert.True(NumberHelper<uint>.TryCreate<ulong>(0x0000000000000001, out result));
            Assert.Equal((uint)0x00000001, result);

            Assert.False(NumberHelper<uint>.TryCreate<ulong>(0x7FFFFFFFFFFFFFFF, out result));
            Assert.Equal((uint)0x00000000, result);

            Assert.False(NumberHelper<uint>.TryCreate<ulong>(0x8000000000000000, out result));
            Assert.Equal((uint)0x00000000, result);

            Assert.False(NumberHelper<uint>.TryCreate<ulong>(0xFFFFFFFFFFFFFFFF, out result));
            Assert.Equal((uint)0x00000000, result);
        }

        [Fact]
        public static void TryCreateFromUIntPtrTest()
        {
            uint result;

            if (Environment.Is64BitProcess)
            {
                Assert.True(NumberHelper<uint>.TryCreate<nuint>(unchecked((nuint)0x0000000000000000), out result));
                Assert.Equal((uint)0x00000000, result);

                Assert.True(NumberHelper<uint>.TryCreate<nuint>(unchecked((nuint)0x0000000000000001), out result));
                Assert.Equal((uint)0x00000001, result);

                Assert.False(NumberHelper<uint>.TryCreate<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF), out result));
                Assert.Equal((uint)0x00000000, result);

                Assert.False(NumberHelper<uint>.TryCreate<nuint>(unchecked((nuint)0x8000000000000000), out result));
                Assert.Equal((uint)0x00000000, result);

                Assert.False(NumberHelper<uint>.TryCreate<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF), out result));
                Assert.Equal((uint)0x00000000, result);
            }
            else
            {
                Assert.True(NumberHelper<uint>.TryCreate<nuint>((nuint)0x00000000, out result));
                Assert.Equal((uint)0x00000000, result);

                Assert.True(NumberHelper<uint>.TryCreate<nuint>((nuint)0x00000001, out result));
                Assert.Equal((uint)0x00000001, result);

                Assert.True(NumberHelper<uint>.TryCreate<nuint>((nuint)0x7FFFFFFF, out result));
                Assert.Equal((uint)0x7FFFFFFF, result);

                Assert.True(NumberHelper<uint>.TryCreate<nuint>(unchecked((nuint)0x80000000), out result));
                Assert.Equal((uint)0x80000000, result);

                Assert.True(NumberHelper<uint>.TryCreate<nuint>(unchecked((nuint)0xFFFFFFFF), out result));
                Assert.Equal((uint)0xFFFFFFFF, result);
            }
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

        [Fact]
        public static void op_LeftShiftTest()
        {
            Assert.Equal((uint)0x00000000, ShiftOperatorsHelper<uint, uint>.op_LeftShift((uint)0x00000000, 1));
            Assert.Equal((uint)0x00000002, ShiftOperatorsHelper<uint, uint>.op_LeftShift((uint)0x00000001, 1));
            Assert.Equal((uint)0xFFFFFFFE, ShiftOperatorsHelper<uint, uint>.op_LeftShift((uint)0x7FFFFFFF, 1));
            Assert.Equal((uint)0x00000000, ShiftOperatorsHelper<uint, uint>.op_LeftShift((uint)0x80000000, 1));
            Assert.Equal((uint)0xFFFFFFFE, ShiftOperatorsHelper<uint, uint>.op_LeftShift((uint)0xFFFFFFFF, 1));
        }

        [Fact]
        public static void op_RightShiftTest()
        {
            Assert.Equal((uint)0x00000000, ShiftOperatorsHelper<uint, uint>.op_RightShift((uint)0x00000000, 1));
            Assert.Equal((uint)0x00000000, ShiftOperatorsHelper<uint, uint>.op_RightShift((uint)0x00000001, 1));
            Assert.Equal((uint)0x3FFFFFFF, ShiftOperatorsHelper<uint, uint>.op_RightShift((uint)0x7FFFFFFF, 1));
            Assert.Equal((uint)0x40000000, ShiftOperatorsHelper<uint, uint>.op_RightShift((uint)0x80000000, 1));
            Assert.Equal((uint)0x7FFFFFFF, ShiftOperatorsHelper<uint, uint>.op_RightShift((uint)0xFFFFFFFF, 1));
        }

        [Fact]
        public static void op_UnsignedRightShiftTest()
        {
            Assert.Equal((uint)0x00000000, ShiftOperatorsHelper<uint, uint>.op_UnsignedRightShift((uint)0x00000000, 1));
            Assert.Equal((uint)0x00000000, ShiftOperatorsHelper<uint, uint>.op_UnsignedRightShift((uint)0x00000001, 1));
            Assert.Equal((uint)0x3FFFFFFF, ShiftOperatorsHelper<uint, uint>.op_UnsignedRightShift((uint)0x7FFFFFFF, 1));
            Assert.Equal((uint)0x40000000, ShiftOperatorsHelper<uint, uint>.op_UnsignedRightShift((uint)0x80000000, 1));
            Assert.Equal((uint)0x7FFFFFFF, ShiftOperatorsHelper<uint, uint>.op_UnsignedRightShift((uint)0xFFFFFFFF, 1));
        }

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

        [Fact]
        public static void op_UnaryPlusTest()
        {
            Assert.Equal((uint)0x00000000, UnaryPlusOperatorsHelper<uint, uint>.op_UnaryPlus((uint)0x00000000));
            Assert.Equal((uint)0x00000001, UnaryPlusOperatorsHelper<uint, uint>.op_UnaryPlus((uint)0x00000001));
            Assert.Equal((uint)0x7FFFFFFF, UnaryPlusOperatorsHelper<uint, uint>.op_UnaryPlus((uint)0x7FFFFFFF));
            Assert.Equal((uint)0x80000000, UnaryPlusOperatorsHelper<uint, uint>.op_UnaryPlus((uint)0x80000000));
            Assert.Equal((uint)0xFFFFFFFF, UnaryPlusOperatorsHelper<uint, uint>.op_UnaryPlus((uint)0xFFFFFFFF));
        }

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
                Assert.Equal(expected, NumberHelper<uint>.Parse(value, style, provider));

                // Substitute default NumberFormatInfo
                Assert.True(NumberHelper<uint>.TryParse(value, style, new NumberFormatInfo(), out result));
                Assert.Equal(expected, result);
                Assert.Equal(expected, NumberHelper<uint>.Parse(value, style, new NumberFormatInfo()));
            }

            // Default style
            if (style == NumberStyles.Integer)
            {
                Assert.Equal(expected, ParsableHelper<uint>.Parse(value, provider));
            }

            // Full overloads
            Assert.True(NumberHelper<uint>.TryParse(value, style, provider, out result));
            Assert.Equal(expected, result);
            Assert.Equal(expected, NumberHelper<uint>.Parse(value, style, provider));
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
                Assert.Throws(exceptionType, () => NumberHelper<uint>.Parse(value, style, provider));

                // Substitute default NumberFormatInfo
                Assert.False(NumberHelper<uint>.TryParse(value, style, new NumberFormatInfo(), out result));
                Assert.Equal(default(uint), result);
                Assert.Throws(exceptionType, () => NumberHelper<uint>.Parse(value, style, new NumberFormatInfo()));
            }

            // Default style
            if (style == NumberStyles.Integer)
            {
                Assert.Throws(exceptionType, () => ParsableHelper<uint>.Parse(value, provider));
            }

            // Full overloads
            Assert.False(NumberHelper<uint>.TryParse(value, style, provider, out result));
            Assert.Equal(default(uint), result);
            Assert.Throws(exceptionType, () => NumberHelper<uint>.Parse(value, style, provider));
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

            Assert.Equal(expected, NumberHelper<uint>.Parse(value.AsSpan(offset, count), style, provider));

            Assert.True(NumberHelper<uint>.TryParse(value.AsSpan(offset, count), style, provider, out result));
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

            Assert.Throws(exceptionType, () => NumberHelper<uint>.Parse(value.AsSpan(), style, provider));

            Assert.False(NumberHelper<uint>.TryParse(value.AsSpan(), style, provider, out result));
            Assert.Equal(default(uint), result);
        }
    }
}
