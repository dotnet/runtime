// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Tests
{
    public class CharTests_GenericMath
    {
        //
        // IAdditionOperators
        //

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

        //
        // IAdditiveIdentity
        //

        [Fact]
        public static void AdditiveIdentityTest()
        {
            Assert.Equal((char)0x0000, AdditiveIdentityHelper<char, char>.AdditiveIdentity);
        }

        //
        // IBinaryInteger
        //

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
        public static void GetByteCountTest()
        {
            Assert.Equal(2, BinaryIntegerHelper<char>.GetByteCount((char)0x0000));
            Assert.Equal(2, BinaryIntegerHelper<char>.GetByteCount((char)0x0001));
            Assert.Equal(2, BinaryIntegerHelper<char>.GetByteCount((char)0x7FFF));
            Assert.Equal(2, BinaryIntegerHelper<char>.GetByteCount((char)0x8000));
            Assert.Equal(2, BinaryIntegerHelper<char>.GetByteCount((char)0xFFFF));
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
        public static void TryWriteBigEndianTest()
        {
            Span<byte> destination = stackalloc byte[2];
            int bytesWritten = 0;

            Assert.True(BinaryIntegerHelper<char>.TryWriteBigEndian((char)0x0000, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x00 }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<char>.TryWriteBigEndian((char)0x0001, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x01 }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<char>.TryWriteBigEndian((char)0x7FFF, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0x7F, 0xFF }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<char>.TryWriteBigEndian((char)0x8000, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0x80, 0x00 }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<char>.TryWriteBigEndian((char)0xFFFF, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0xFF }, destination.ToArray());

            Assert.False(BinaryIntegerHelper<char>.TryWriteBigEndian(default, Span<byte>.Empty, out bytesWritten));
            Assert.Equal(0, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0xFF }, destination.ToArray());
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

        //
        // IBinaryNumber
        //

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

        //
        // IBitwiseOperators
        //

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

        //
        // IComparisonOperators
        //

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

        //
        // IDecrementOperators
        //

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

        //
        // IDivisionOperators
        //

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

        //
        // IEqualityOperators
        //

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

        //
        // IIncrementOperators
        //

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

        //
        // IMinMaxValue
        //

        [Fact]
        public static void MaxValueTest()
        {
            Assert.Equal((char)0xFFFF, MinMaxValueHelper<char>.MaxValue);
        }

        [Fact]
        public static void MinValueTest()
        {
            Assert.Equal((char)0x0000, MinMaxValueHelper<char>.MinValue);
        }

        //
        // IModulusOperators
        //

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

        //
        // IMultiplicativeIdentity
        //

        [Fact]
        public static void MultiplicativeIdentityTest()
        {
            Assert.Equal((char)0x0001, MultiplicativeIdentityHelper<char, char>.MultiplicativeIdentity);
        }

        //
        // IMultiplyOperators
        //

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

        //
        // INumber
        //

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
        public static void MaxTest()
        {
            Assert.Equal((char)0x0001, NumberHelper<char>.Max((char)0x0000, (char)1));
            Assert.Equal((char)0x0001, NumberHelper<char>.Max((char)0x0001, (char)1));
            Assert.Equal((char)0x7FFF, NumberHelper<char>.Max((char)0x7FFF, (char)1));
            Assert.Equal((char)0x8000, NumberHelper<char>.Max((char)0x8000, (char)1));
            Assert.Equal((char)0xFFFF, NumberHelper<char>.Max((char)0xFFFF, (char)1));
        }

        [Fact]
        public static void MaxNumberTest()
        {
            Assert.Equal((char)0x0001, NumberHelper<char>.MaxNumber((char)0x0000, (char)1));
            Assert.Equal((char)0x0001, NumberHelper<char>.MaxNumber((char)0x0001, (char)1));
            Assert.Equal((char)0x7FFF, NumberHelper<char>.MaxNumber((char)0x7FFF, (char)1));
            Assert.Equal((char)0x8000, NumberHelper<char>.MaxNumber((char)0x8000, (char)1));
            Assert.Equal((char)0xFFFF, NumberHelper<char>.MaxNumber((char)0xFFFF, (char)1));
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
        public static void MinNumberTest()
        {
            Assert.Equal((char)0x0000, NumberHelper<char>.MinNumber((char)0x0000, (char)1));
            Assert.Equal((char)0x0001, NumberHelper<char>.MinNumber((char)0x0001, (char)1));
            Assert.Equal((char)0x0001, NumberHelper<char>.MinNumber((char)0x7FFF, (char)1));
            Assert.Equal((char)0x0001, NumberHelper<char>.MinNumber((char)0x8000, (char)1));
            Assert.Equal((char)0x0001, NumberHelper<char>.MinNumber((char)0xFFFF, (char)1));
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

        //
        // INumberBase
        //

        [Fact]
        public static void OneTest()
        {
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.One);
        }

        [Fact]
        public static void RadixTest()
        {
            Assert.Equal(2, NumberBaseHelper<char>.Radix);
        }

        [Fact]
        public static void ZeroTest()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.Zero);
        }

        [Fact]
        public static void AbsTest()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.Abs((char)0x0000));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.Abs((char)0x0001));
            Assert.Equal((char)0x7FFF, NumberBaseHelper<char>.Abs((char)0x7FFF));
            Assert.Equal((char)0x8000, NumberBaseHelper<char>.Abs((char)0x8000));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.Abs((char)0xFFFF));
        }

        [Fact]
        public static void CreateCheckedFromByteTest()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateChecked<byte>(0x00));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateChecked<byte>(0x01));
            Assert.Equal((char)0x007F, NumberBaseHelper<char>.CreateChecked<byte>(0x7F));
            Assert.Equal((char)0x0080, NumberBaseHelper<char>.CreateChecked<byte>(0x80));
            Assert.Equal((char)0x00FF, NumberBaseHelper<char>.CreateChecked<byte>(0xFF));
        }

        [Fact]
        public static void CreateCheckedFromCharTest()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateChecked<char>((char)0x0000));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateChecked<char>((char)0x0001));
            Assert.Equal((char)0x7FFF, NumberBaseHelper<char>.CreateChecked<char>((char)0x7FFF));
            Assert.Equal((char)0x8000, NumberBaseHelper<char>.CreateChecked<char>((char)0x8000));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateChecked<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateCheckedFromInt16Test()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateChecked<short>(0x0000));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateChecked<short>(0x0001));
            Assert.Equal((char)0x7FFF, NumberBaseHelper<char>.CreateChecked<short>(0x7FFF));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<short>(unchecked((short)0x8000)));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromInt32Test()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateChecked<int>(0x00000000));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateChecked<int>(0x00000001));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<int>(0x7FFFFFFF));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<int>(unchecked((int)0x80000000)));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromInt64Test()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateChecked<long>(0x0000000000000000));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateChecked<long>(0x0000000000000001));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<long>(0x7FFFFFFFFFFFFFFF));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<long>(unchecked((long)0x8000000000000000)));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateChecked<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateChecked<nint>(unchecked((nint)0x0000000000000001)));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<nint>(unchecked((nint)0x8000000000000000)));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateChecked<nint>((nint)0x00000000));
                Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateChecked<nint>((nint)0x00000001));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<nint>((nint)0x7FFFFFFF));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<nint>(unchecked((nint)0x80000000)));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateCheckedFromSByteTest()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateChecked<sbyte>(0x00));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateChecked<sbyte>(0x01));
            Assert.Equal((char)0x007F, NumberBaseHelper<char>.CreateChecked<sbyte>(0x7F));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<sbyte>(unchecked((sbyte)0x80)));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateCheckedFromUInt16Test()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateChecked<ushort>(0x0000));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateChecked<ushort>(0x0001));
            Assert.Equal((char)0x7FFF, NumberBaseHelper<char>.CreateChecked<ushort>(0x7FFF));
            Assert.Equal((char)0x8000, NumberBaseHelper<char>.CreateChecked<ushort>(0x8000));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateChecked<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUInt32Test()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateChecked<uint>(0x00000000));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateChecked<uint>(0x00000001));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<uint>(0x7FFFFFFF));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<uint>(0x80000000));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUInt64Test()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateChecked<ulong>(0x0000000000000000));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateChecked<ulong>(0x0000000000000001));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<ulong>(0x7FFFFFFFFFFFFFFF));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<ulong>(0x8000000000000000));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<ulong>(0xFFFFFFFFFFFFFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateChecked<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateChecked<nuint>(unchecked((nuint)0x0000000000000001)));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<nuint>(unchecked((nuint)0x8000000000000000)));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateChecked<nuint>((nuint)0x00000000));
                Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateChecked<nuint>((nuint)0x00000001));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<nuint>((nuint)0x7FFFFFFF));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<nuint>((nuint)0x80000000));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<char>.CreateChecked<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateSaturatingFromByteTest()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<byte>(0x00));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateSaturating<byte>(0x01));
            Assert.Equal((char)0x007F, NumberBaseHelper<char>.CreateSaturating<byte>(0x7F));
            Assert.Equal((char)0x0080, NumberBaseHelper<char>.CreateSaturating<byte>(0x80));
            Assert.Equal((char)0x00FF, NumberBaseHelper<char>.CreateSaturating<byte>(0xFF));
        }

        [Fact]
        public static void CreateSaturatingFromCharTest()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<char>((char)0x0000));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateSaturating<char>((char)0x0001));
            Assert.Equal((char)0x7FFF, NumberBaseHelper<char>.CreateSaturating<char>((char)0x7FFF));
            Assert.Equal((char)0x8000, NumberBaseHelper<char>.CreateSaturating<char>((char)0x8000));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateSaturating<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromInt16Test()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<short>(0x0000));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateSaturating<short>(0x0001));
            Assert.Equal((char)0x7FFF, NumberBaseHelper<char>.CreateSaturating<short>(0x7FFF));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<short>(unchecked((short)0x8000)));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromInt32Test()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<int>(0x00000000));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateSaturating<int>(0x00000001));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateSaturating<int>(0x7FFFFFFF));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<int>(unchecked((int)0x80000000)));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromInt64Test()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<long>(0x0000000000000000));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateSaturating<long>(0x0000000000000001));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateSaturating<long>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<long>(unchecked((long)0x8000000000000000)));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateSaturating<nint>(unchecked((nint)0x0000000000000001)));
                Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateSaturating<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<nint>(unchecked((nint)0x8000000000000000)));
                Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<nint>((nint)0x00000000));
                Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateSaturating<nint>((nint)0x00000001));
                Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateSaturating<nint>((nint)0x7FFFFFFF));
                Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<nint>(unchecked((nint)0x80000000)));
                Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateSaturatingFromSByteTest()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<sbyte>(0x00));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateSaturating<sbyte>(0x01));
            Assert.Equal((char)0x007F, NumberBaseHelper<char>.CreateSaturating<sbyte>(0x7F));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<sbyte>(unchecked((sbyte)0x80)));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateSaturatingFromUInt16Test()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<ushort>(0x0000));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateSaturating<ushort>(0x0001));
            Assert.Equal((char)0x7FFF, NumberBaseHelper<char>.CreateSaturating<ushort>(0x7FFF));
            Assert.Equal((char)0x8000, NumberBaseHelper<char>.CreateSaturating<ushort>(0x8000));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateSaturating<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUInt32Test()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<uint>(0x00000000));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateSaturating<uint>(0x00000001));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateSaturating<uint>(0x7FFFFFFF));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateSaturating<uint>(0x80000000));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateSaturating<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUInt64Test()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<ulong>(0x0000000000000000));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateSaturating<ulong>(0x0000000000000001));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateSaturating<ulong>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateSaturating<ulong>(0x8000000000000000));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateSaturating<ulong>(0xFFFFFFFFFFFFFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateSaturating<nuint>(unchecked((nuint)0x0000000000000001)));
                Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateSaturating<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateSaturating<nuint>(unchecked((nuint)0x8000000000000000)));
                Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateSaturating<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<nuint>((nuint)0x00000000));
                Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateSaturating<nuint>((nuint)0x00000001));
                Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateSaturating<nuint>((nuint)0x7FFFFFFF));
                Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateSaturating<nuint>((nuint)0x80000000));
                Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateSaturating<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateTruncatingFromByteTest()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<byte>(0x00));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateTruncating<byte>(0x01));
            Assert.Equal((char)0x007F, NumberBaseHelper<char>.CreateTruncating<byte>(0x7F));
            Assert.Equal((char)0x0080, NumberBaseHelper<char>.CreateTruncating<byte>(0x80));
            Assert.Equal((char)0x00FF, NumberBaseHelper<char>.CreateTruncating<byte>(0xFF));
        }

        [Fact]
        public static void CreateTruncatingFromCharTest()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<char>((char)0x0000));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateTruncating<char>((char)0x0001));
            Assert.Equal((char)0x7FFF, NumberBaseHelper<char>.CreateTruncating<char>((char)0x7FFF));
            Assert.Equal((char)0x8000, NumberBaseHelper<char>.CreateTruncating<char>((char)0x8000));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateTruncating<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromInt16Test()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<short>(0x0000));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateTruncating<short>(0x0001));
            Assert.Equal((char)0x7FFF, NumberBaseHelper<char>.CreateTruncating<short>(0x7FFF));
            Assert.Equal((char)0x8000, NumberBaseHelper<char>.CreateTruncating<short>(unchecked((short)0x8000)));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateTruncating<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateTruncatingFromInt32Test()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<int>(0x00000000));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateTruncating<int>(0x00000001));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateTruncating<int>(0x7FFFFFFF));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<int>(unchecked((int)0x80000000)));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateTruncating<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateTruncatingFromInt64Test()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<long>(0x0000000000000000));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateTruncating<long>(0x0000000000000001));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateTruncating<long>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<long>(unchecked((long)0x8000000000000000)));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateTruncating<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void CreateTruncatingFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateTruncating<nint>(unchecked((nint)0x0000000000000001)));
                Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateTruncating<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<nint>(unchecked((nint)0x8000000000000000)));
                Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateTruncating<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<nint>((nint)0x00000000));
                Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateTruncating<nint>((nint)0x00000001));
                Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateTruncating<nint>((nint)0x7FFFFFFF));
                Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<nint>(unchecked((nint)0x80000000)));
                Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateTruncating<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateTruncatingFromSByteTest()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<sbyte>(0x00));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateTruncating<sbyte>(0x01));
            Assert.Equal((char)0x007F, NumberBaseHelper<char>.CreateTruncating<sbyte>(0x7F));
            Assert.Equal((char)0xFF80, NumberBaseHelper<char>.CreateTruncating<sbyte>(unchecked((sbyte)0x80)));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateTruncating<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateTruncatingFromUInt16Test()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<ushort>(0x0000));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateTruncating<ushort>(0x0001));
            Assert.Equal((char)0x7FFF, NumberBaseHelper<char>.CreateTruncating<ushort>(0x7FFF));
            Assert.Equal((char)0x8000, NumberBaseHelper<char>.CreateTruncating<ushort>(0x8000));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateTruncating<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt32Test()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<uint>(0x00000000));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateTruncating<uint>(0x00000001));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateTruncating<uint>(0x7FFFFFFF));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<uint>(0x80000000));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateTruncating<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt64Test()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<ulong>(0x0000000000000000));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateTruncating<ulong>(0x0000000000000001));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateTruncating<ulong>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<ulong>(0x8000000000000000));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateTruncating<ulong>(0xFFFFFFFFFFFFFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateTruncating<nuint>(unchecked((nuint)0x0000000000000001)));
                Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateTruncating<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<nuint>(unchecked((nuint)0x8000000000000000)));
                Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateTruncating<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<nuint>((nuint)0x00000000));
                Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateTruncating<nuint>((nuint)0x00000001));
                Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateTruncating<nuint>((nuint)0x7FFFFFFF));
                Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<nuint>((nuint)0x80000000));
                Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateTruncating<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void IsCanonicalTest()
        {
            Assert.True(NumberBaseHelper<char>.IsCanonical((char)0x0000));
            Assert.True(NumberBaseHelper<char>.IsCanonical((char)0x0001));
            Assert.True(NumberBaseHelper<char>.IsCanonical((char)0x7FFF));
            Assert.True(NumberBaseHelper<char>.IsCanonical((char)0x8000));
            Assert.True(NumberBaseHelper<char>.IsCanonical((char)0xFFFF));
        }

        [Fact]
        public static void IsComplexNumberTest()
        {
            Assert.False(NumberBaseHelper<char>.IsComplexNumber((char)0x0000));
            Assert.False(NumberBaseHelper<char>.IsComplexNumber((char)0x0001));
            Assert.False(NumberBaseHelper<char>.IsComplexNumber((char)0x7FFF));
            Assert.False(NumberBaseHelper<char>.IsComplexNumber((char)0x8000));
            Assert.False(NumberBaseHelper<char>.IsComplexNumber((char)0xFFFF));
        }

        [Fact]
        public static void IsEvenIntegerTest()
        {
            Assert.True(NumberBaseHelper<char>.IsEvenInteger((char)0x0000));
            Assert.False(NumberBaseHelper<char>.IsEvenInteger((char)0x0001));
            Assert.False(NumberBaseHelper<char>.IsEvenInteger((char)0x7FFF));
            Assert.True(NumberBaseHelper<char>.IsEvenInteger((char)0x8000));
            Assert.False(NumberBaseHelper<char>.IsEvenInteger((char)0xFFFF));
        }

        [Fact]
        public static void IsFiniteTest()
        {
            Assert.True(NumberBaseHelper<char>.IsFinite((char)0x0000));
            Assert.True(NumberBaseHelper<char>.IsFinite((char)0x0001));
            Assert.True(NumberBaseHelper<char>.IsFinite((char)0x7FFF));
            Assert.True(NumberBaseHelper<char>.IsFinite((char)0x8000));
            Assert.True(NumberBaseHelper<char>.IsFinite((char)0xFFFF));
        }

        [Fact]
        public static void IsImaginaryNumberTest()
        {
            Assert.False(NumberBaseHelper<char>.IsImaginaryNumber((char)0x0000));
            Assert.False(NumberBaseHelper<char>.IsImaginaryNumber((char)0x0001));
            Assert.False(NumberBaseHelper<char>.IsImaginaryNumber((char)0x7FFF));
            Assert.False(NumberBaseHelper<char>.IsImaginaryNumber((char)0x8000));
            Assert.False(NumberBaseHelper<char>.IsImaginaryNumber((char)0xFFFF));
        }

        [Fact]
        public static void IsInfinityTest()
        {
            Assert.False(NumberBaseHelper<char>.IsInfinity((char)0x0000));
            Assert.False(NumberBaseHelper<char>.IsInfinity((char)0x0001));
            Assert.False(NumberBaseHelper<char>.IsInfinity((char)0x7FFF));
            Assert.False(NumberBaseHelper<char>.IsInfinity((char)0x8000));
            Assert.False(NumberBaseHelper<char>.IsInfinity((char)0xFFFF));
        }

        [Fact]
        public static void IsIntegerTest()
        {
            Assert.True(NumberBaseHelper<char>.IsInteger((char)0x0000));
            Assert.True(NumberBaseHelper<char>.IsInteger((char)0x0001));
            Assert.True(NumberBaseHelper<char>.IsInteger((char)0x7FFF));
            Assert.True(NumberBaseHelper<char>.IsInteger((char)0x8000));
            Assert.True(NumberBaseHelper<char>.IsInteger((char)0xFFFF));
        }

        [Fact]
        public static void IsNaNTest()
        {
            Assert.False(NumberBaseHelper<char>.IsNaN((char)0x0000));
            Assert.False(NumberBaseHelper<char>.IsNaN((char)0x0001));
            Assert.False(NumberBaseHelper<char>.IsNaN((char)0x7FFF));
            Assert.False(NumberBaseHelper<char>.IsNaN((char)0x8000));
            Assert.False(NumberBaseHelper<char>.IsNaN((char)0xFFFF));
        }

        [Fact]
        public static void IsNegativeTest()
        {
            Assert.False(NumberBaseHelper<char>.IsNegative((char)0x0000));
            Assert.False(NumberBaseHelper<char>.IsNegative((char)0x0001));
            Assert.False(NumberBaseHelper<char>.IsNegative((char)0x7FFF));
            Assert.False(NumberBaseHelper<char>.IsNegative((char)0x8000));
            Assert.False(NumberBaseHelper<char>.IsNegative((char)0xFFFF));
        }

        [Fact]
        public static void IsNegativeInfinityTest()
        {
            Assert.False(NumberBaseHelper<char>.IsNegativeInfinity((char)0x0000));
            Assert.False(NumberBaseHelper<char>.IsNegativeInfinity((char)0x0001));
            Assert.False(NumberBaseHelper<char>.IsNegativeInfinity((char)0x7FFF));
            Assert.False(NumberBaseHelper<char>.IsNegativeInfinity((char)0x8000));
            Assert.False(NumberBaseHelper<char>.IsNegativeInfinity((char)0xFFFF));
        }

        [Fact]
        public static void IsNormalTest()
        {
            Assert.False(NumberBaseHelper<char>.IsNormal((char)0x0000));
            Assert.True(NumberBaseHelper<char>.IsNormal((char)0x0001));
            Assert.True(NumberBaseHelper<char>.IsNormal((char)0x7FFF));
            Assert.True(NumberBaseHelper<char>.IsNormal((char)0x8000));
            Assert.True(NumberBaseHelper<char>.IsNormal((char)0xFFFF));
        }

        [Fact]
        public static void IsOddIntegerTest()
        {
            Assert.False(NumberBaseHelper<char>.IsOddInteger((char)0x0000));
            Assert.True(NumberBaseHelper<char>.IsOddInteger((char)0x0001));
            Assert.True(NumberBaseHelper<char>.IsOddInteger((char)0x7FFF));
            Assert.False(NumberBaseHelper<char>.IsOddInteger((char)0x8000));
            Assert.True(NumberBaseHelper<char>.IsOddInteger((char)0xFFFF));
        }

        [Fact]
        public static void IsPositiveTest()
        {
            Assert.True(NumberBaseHelper<char>.IsPositive((char)0x0000));
            Assert.True(NumberBaseHelper<char>.IsPositive((char)0x0001));
            Assert.True(NumberBaseHelper<char>.IsPositive((char)0x7FFF));
            Assert.True(NumberBaseHelper<char>.IsPositive((char)0x8000));
            Assert.True(NumberBaseHelper<char>.IsPositive((char)0xFFFF));
        }

        [Fact]
        public static void IsPositiveInfinityTest()
        {
            Assert.False(NumberBaseHelper<char>.IsPositiveInfinity((char)0x0000));
            Assert.False(NumberBaseHelper<char>.IsPositiveInfinity((char)0x0001));
            Assert.False(NumberBaseHelper<char>.IsPositiveInfinity((char)0x7FFF));
            Assert.False(NumberBaseHelper<char>.IsPositiveInfinity((char)0x8000));
            Assert.False(NumberBaseHelper<char>.IsPositiveInfinity((char)0xFFFF));
        }

        [Fact]
        public static void IsRealNumberTest()
        {
            Assert.True(NumberBaseHelper<char>.IsRealNumber((char)0x0000));
            Assert.True(NumberBaseHelper<char>.IsRealNumber((char)0x0001));
            Assert.True(NumberBaseHelper<char>.IsRealNumber((char)0x7FFF));
            Assert.True(NumberBaseHelper<char>.IsRealNumber((char)0x8000));
            Assert.True(NumberBaseHelper<char>.IsRealNumber((char)0xFFFF));
        }

        [Fact]
        public static void IsSubnormalTest()
        {
            Assert.False(NumberBaseHelper<char>.IsSubnormal((char)0x0000));
            Assert.False(NumberBaseHelper<char>.IsSubnormal((char)0x0001));
            Assert.False(NumberBaseHelper<char>.IsSubnormal((char)0x7FFF));
            Assert.False(NumberBaseHelper<char>.IsSubnormal((char)0x8000));
            Assert.False(NumberBaseHelper<char>.IsSubnormal((char)0xFFFF));
        }

        [Fact]
        public static void IsZeroTest()
        {
            Assert.True(NumberBaseHelper<char>.IsZero((char)0x0000));
            Assert.False(NumberBaseHelper<char>.IsZero((char)0x0001));
            Assert.False(NumberBaseHelper<char>.IsZero((char)0x7FFF));
            Assert.False(NumberBaseHelper<char>.IsZero((char)0x8000));
            Assert.False(NumberBaseHelper<char>.IsZero((char)0xFFFF));
        }

        [Fact]
        public static void MinMagnitudeMagnitude()
        {
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.MaxMagnitude((char)0x0000, (char)1));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.MaxMagnitude((char)0x0001, (char)1));
            Assert.Equal((char)0x7FFF, NumberBaseHelper<char>.MaxMagnitude((char)0x7FFF, (char)1));
            Assert.Equal((char)0x8000, NumberBaseHelper<char>.MaxMagnitude((char)0x8000, (char)1));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.MaxMagnitude((char)0xFFFF, (char)1));
        }

        [Fact]
        public static void MaxMagnitudeNumberTest()
        {
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.MaxMagnitudeNumber((char)0x0000, (char)1));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.MaxMagnitudeNumber((char)0x0001, (char)1));
            Assert.Equal((char)0x7FFF, NumberBaseHelper<char>.MaxMagnitudeNumber((char)0x7FFF, (char)1));
            Assert.Equal((char)0x8000, NumberBaseHelper<char>.MaxMagnitudeNumber((char)0x8000, (char)1));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.MaxMagnitudeNumber((char)0xFFFF, (char)1));
        }

        [Fact]
        public static void MinMagnitudeTest()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.MinMagnitude((char)0x0000, (char)1));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.MinMagnitude((char)0x0001, (char)1));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.MinMagnitude((char)0x7FFF, (char)1));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.MinMagnitude((char)0x8000, (char)1));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.MinMagnitude((char)0xFFFF, (char)1));
        }

        [Fact]
        public static void MinMagnitudeNumberTest()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.MinMagnitudeNumber((char)0x0000, (char)1));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.MinMagnitudeNumber((char)0x0001, (char)1));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.MinMagnitudeNumber((char)0x7FFF, (char)1));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.MinMagnitudeNumber((char)0x8000, (char)1));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.MinMagnitudeNumber((char)0xFFFF, (char)1));
        }

        [Fact]
        public static void TryCreateFromByteTest()
        {
            char result;

            Assert.True(NumberBaseHelper<char>.TryCreate<byte>(0x00, out result));
            Assert.Equal((char)0x0000, result);

            Assert.True(NumberBaseHelper<char>.TryCreate<byte>(0x01, out result));
            Assert.Equal((char)0x0001, result);

            Assert.True(NumberBaseHelper<char>.TryCreate<byte>(0x7F, out result));
            Assert.Equal((char)0x007F, result);

            Assert.True(NumberBaseHelper<char>.TryCreate<byte>(0x80, out result));
            Assert.Equal((char)0x0080, result);

            Assert.True(NumberBaseHelper<char>.TryCreate<byte>(0xFF, out result));
            Assert.Equal((char)0x00FF, result);
        }

        [Fact]
        public static void TryCreateFromCharTest()
        {
            char result;

            Assert.True(NumberBaseHelper<char>.TryCreate<char>((char)0x0000, out result));
            Assert.Equal((char)0x0000, result);

            Assert.True(NumberBaseHelper<char>.TryCreate<char>((char)0x0001, out result));
            Assert.Equal((char)0x0001, result);

            Assert.True(NumberBaseHelper<char>.TryCreate<char>((char)0x7FFF, out result));
            Assert.Equal((char)0x7FFF, result);

            Assert.True(NumberBaseHelper<char>.TryCreate<char>((char)0x8000, out result));
            Assert.Equal((char)0x8000, result);

            Assert.True(NumberBaseHelper<char>.TryCreate<char>((char)0xFFFF, out result));
            Assert.Equal((char)0xFFFF, result);
        }

        [Fact]
        public static void TryCreateFromInt16Test()
        {
            char result;

            Assert.True(NumberBaseHelper<char>.TryCreate<short>(0x0000, out result));
            Assert.Equal((char)0x0000, result);

            Assert.True(NumberBaseHelper<char>.TryCreate<short>(0x0001, out result));
            Assert.Equal((char)0x0001, result);

            Assert.True(NumberBaseHelper<char>.TryCreate<short>(0x7FFF, out result));
            Assert.Equal((char)0x7FFF, result);

            Assert.False(NumberBaseHelper<char>.TryCreate<short>(unchecked((short)0x8000), out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(NumberBaseHelper<char>.TryCreate<short>(unchecked((short)0xFFFF), out result));
            Assert.Equal((char)0x0000, result);
        }

        [Fact]
        public static void TryCreateFromInt32Test()
        {
            char result;

            Assert.True(NumberBaseHelper<char>.TryCreate<int>(0x00000000, out result));
            Assert.Equal((char)0x0000, result);

            Assert.True(NumberBaseHelper<char>.TryCreate<int>(0x00000001, out result));
            Assert.Equal((char)0x0001, result);

            Assert.False(NumberBaseHelper<char>.TryCreate<int>(0x7FFFFFFF, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(NumberBaseHelper<char>.TryCreate<int>(unchecked((int)0x80000000), out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(NumberBaseHelper<char>.TryCreate<int>(unchecked((int)0xFFFFFFFF), out result));
            Assert.Equal((char)0x0000, result);
        }

        [Fact]
        public static void TryCreateFromInt64Test()
        {
            char result;

            Assert.True(NumberBaseHelper<char>.TryCreate<long>(0x0000000000000000, out result));
            Assert.Equal((char)0x0000, result);

            Assert.True(NumberBaseHelper<char>.TryCreate<long>(0x0000000000000001, out result));
            Assert.Equal((char)0x0001, result);

            Assert.False(NumberBaseHelper<char>.TryCreate<long>(0x7FFFFFFFFFFFFFFF, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(NumberBaseHelper<char>.TryCreate<long>(unchecked((long)0x8000000000000000), out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(NumberBaseHelper<char>.TryCreate<long>(unchecked((long)0xFFFFFFFFFFFFFFFF), out result));
            Assert.Equal((char)0x0000, result);
        }

        [Fact]
        public static void TryCreateFromIntPtrTest()
        {
            char result;

            if (Environment.Is64BitProcess)
            {
                Assert.True(NumberBaseHelper<char>.TryCreate<nint>(unchecked((nint)0x0000000000000000), out result));
                Assert.Equal((char)0x0000, result);

                Assert.True(NumberBaseHelper<char>.TryCreate<nint>(unchecked((nint)0x0000000000000001), out result));
                Assert.Equal((char)0x0001, result);

                Assert.False(NumberBaseHelper<char>.TryCreate<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF), out result));
                Assert.Equal((char)0x0000, result);

                Assert.False(NumberBaseHelper<char>.TryCreate<nint>(unchecked((nint)0x8000000000000000), out result));
                Assert.Equal((char)0x0000, result);

                Assert.False(NumberBaseHelper<char>.TryCreate<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF), out result));
                Assert.Equal((char)0x0000, result);
            }
            else
            {
                Assert.True(NumberBaseHelper<char>.TryCreate<nint>((nint)0x00000000, out result));
                Assert.Equal((char)0x0000, result);

                Assert.True(NumberBaseHelper<char>.TryCreate<nint>((nint)0x00000001, out result));
                Assert.Equal((char)0x0001, result);

                Assert.False(NumberBaseHelper<char>.TryCreate<nint>((nint)0x7FFFFFFF, out result));
                Assert.Equal((char)0x0000, result);

                Assert.False(NumberBaseHelper<char>.TryCreate<nint>(unchecked((nint)0x80000000), out result));
                Assert.Equal((char)0x0000, result);

                Assert.False(NumberBaseHelper<char>.TryCreate<nint>(unchecked((nint)0xFFFFFFFF), out result));
                Assert.Equal((char)0x0000, result);
            }
        }

        [Fact]
        public static void TryCreateFromSByteTest()
        {
            char result;

            Assert.True(NumberBaseHelper<char>.TryCreate<sbyte>(0x00, out result));
            Assert.Equal((char)0x0000, result);

            Assert.True(NumberBaseHelper<char>.TryCreate<sbyte>(0x01, out result));
            Assert.Equal((char)0x0001, result);

            Assert.True(NumberBaseHelper<char>.TryCreate<sbyte>(0x7F, out result));
            Assert.Equal((char)0x007F, result);

            Assert.False(NumberBaseHelper<char>.TryCreate<sbyte>(unchecked((sbyte)0x80), out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(NumberBaseHelper<char>.TryCreate<sbyte>(unchecked((sbyte)0xFF), out result));
            Assert.Equal((char)0x0000, result);
        }

        [Fact]
        public static void TryCreateFromUInt16Test()
        {
            char result;

            Assert.True(NumberBaseHelper<char>.TryCreate<ushort>(0x0000, out result));
            Assert.Equal((char)0x0000, result);

            Assert.True(NumberBaseHelper<char>.TryCreate<ushort>(0x0001, out result));
            Assert.Equal((char)0x0001, result);

            Assert.True(NumberBaseHelper<char>.TryCreate<ushort>(0x7FFF, out result));
            Assert.Equal((char)0x7FFF, result);

            Assert.True(NumberBaseHelper<char>.TryCreate<ushort>(0x8000, out result));
            Assert.Equal((char)0x8000, result);

            Assert.True(NumberBaseHelper<char>.TryCreate<ushort>(0xFFFF, out result));
            Assert.Equal((char)0xFFFF, result);
        }

        [Fact]
        public static void TryCreateFromUInt32Test()
        {
            char result;

            Assert.True(NumberBaseHelper<char>.TryCreate<uint>(0x00000000, out result));
            Assert.Equal((char)0x0000, result);

            Assert.True(NumberBaseHelper<char>.TryCreate<uint>(0x00000001, out result));
            Assert.Equal((char)0x0001, result);

            Assert.False(NumberBaseHelper<char>.TryCreate<uint>(0x7FFFFFFF, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(NumberBaseHelper<char>.TryCreate<uint>(0x80000000, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(NumberBaseHelper<char>.TryCreate<uint>(0xFFFFFFFF, out result));
            Assert.Equal((char)0x0000, result);
        }

        [Fact]
        public static void TryCreateFromUInt64Test()
        {
            char result;

            Assert.True(NumberBaseHelper<char>.TryCreate<ulong>(0x0000000000000000, out result));
            Assert.Equal((char)0x0000, result);

            Assert.True(NumberBaseHelper<char>.TryCreate<ulong>(0x0000000000000001, out result));
            Assert.Equal((char)0x0001, result);

            Assert.False(NumberBaseHelper<char>.TryCreate<ulong>(0x7FFFFFFFFFFFFFFF, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(NumberBaseHelper<char>.TryCreate<ulong>(0x8000000000000000, out result));
            Assert.Equal((char)0x0000, result);

            Assert.False(NumberBaseHelper<char>.TryCreate<ulong>(0xFFFFFFFFFFFFFFFF, out result));
            Assert.Equal((char)0x0000, result);
        }

        [Fact]
        public static void TryCreateFromUIntPtrTest()
        {
            char result;

            if (Environment.Is64BitProcess)
            {
                Assert.True(NumberBaseHelper<char>.TryCreate<nuint>(unchecked((nuint)0x0000000000000000), out result));
                Assert.Equal((char)0x0000, result);

                Assert.True(NumberBaseHelper<char>.TryCreate<nuint>(unchecked((nuint)0x0000000000000001), out result));
                Assert.Equal((char)0x0001, result);

                Assert.False(NumberBaseHelper<char>.TryCreate<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF), out result));
                Assert.Equal((char)0x0000, result);

                Assert.False(NumberBaseHelper<char>.TryCreate<nuint>(unchecked((nuint)0x8000000000000000), out result));
                Assert.Equal((char)0x0000, result);

                Assert.False(NumberBaseHelper<char>.TryCreate<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF), out result));
                Assert.Equal((char)0x0000, result);
            }
            else
            {
                Assert.True(NumberBaseHelper<char>.TryCreate<nuint>((nuint)0x00000000, out result));
                Assert.Equal((char)0x0000, result);

                Assert.True(NumberBaseHelper<char>.TryCreate<nuint>((nuint)0x00000001, out result));
                Assert.Equal((char)0x0001, result);

                Assert.False(NumberBaseHelper<char>.TryCreate<nuint>((nuint)0x7FFFFFFF, out result));
                Assert.Equal((char)0x0000, result);

                Assert.False(NumberBaseHelper<char>.TryCreate<nuint>(unchecked((nuint)0x80000000), out result));
                Assert.Equal((char)0x0000, result);

                Assert.False(NumberBaseHelper<char>.TryCreate<nuint>(unchecked((nuint)0xFFFFFFFF), out result));
                Assert.Equal((char)0x0000, result);
            }
        }

        //
        // IShiftOperators
        //

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
        public static void op_UnsignedRightShiftTest()
        {
            Assert.Equal((char)0x0000, ShiftOperatorsHelper<char, char>.op_UnsignedRightShift((char)0x0000, 1));
            Assert.Equal((char)0x0000, ShiftOperatorsHelper<char, char>.op_UnsignedRightShift((char)0x0001, 1));
            Assert.Equal((char)0x3FFF, ShiftOperatorsHelper<char, char>.op_UnsignedRightShift((char)0x7FFF, 1));
            Assert.Equal((char)0x4000, ShiftOperatorsHelper<char, char>.op_UnsignedRightShift((char)0x8000, 1));
            Assert.Equal((char)0x7FFF, ShiftOperatorsHelper<char, char>.op_UnsignedRightShift((char)0xFFFF, 1));
        }

        //
        // ISubtractionOperators
        //

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

        //
        // IUnaryNegationOperators
        //

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

        //
        // IUnaryPlusOperators
        //

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
