// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Xunit;

namespace System.Tests
{
    public class DecimalTests_GenericMath
    {
        //
        // IAdditionOperators
        //

        [Fact]
        public static void op_AdditionTest()
        {
            Assert.Equal(-79228162514264337593543950334.0m, AdditionOperatorsHelper<decimal, decimal, decimal>.op_Addition(decimal.MinValue, 1.0m));
            Assert.Equal(0.0m, AdditionOperatorsHelper<decimal, decimal, decimal>.op_Addition(-1.0m, 1.0m));
            Assert.Equal(1.0m, AdditionOperatorsHelper<decimal, decimal, decimal>.op_Addition(-0.0m, 1.0m));
            Assert.Equal(1.0m, AdditionOperatorsHelper<decimal, decimal, decimal>.op_Addition(0.0m, 1.0m));
            Assert.Equal(2.0m, AdditionOperatorsHelper<decimal, decimal, decimal>.op_Addition(1.0m, 1.0m));

            Assert.Throws<OverflowException>(() => AdditionOperatorsHelper<decimal, decimal, decimal>.op_Addition(decimal.MaxValue, 1.0m));
        }

        [Fact]
        public static void op_CheckedAdditionTest()
        {
            Assert.Equal(-79228162514264337593543950334.0m, AdditionOperatorsHelper<decimal, decimal, decimal>.op_CheckedAddition(decimal.MinValue, 1.0m));
            Assert.Equal(0.0m, AdditionOperatorsHelper<decimal, decimal, decimal>.op_CheckedAddition(-1.0m, 1.0m));
            Assert.Equal(1.0m, AdditionOperatorsHelper<decimal, decimal, decimal>.op_CheckedAddition(-0.0m, 1.0m));
            Assert.Equal(1.0m, AdditionOperatorsHelper<decimal, decimal, decimal>.op_CheckedAddition(0.0m, 1.0m));
            Assert.Equal(2.0m, AdditionOperatorsHelper<decimal, decimal, decimal>.op_CheckedAddition(1.0m, 1.0m));

            Assert.Throws<OverflowException>(() => AdditionOperatorsHelper<decimal, decimal, decimal>.op_CheckedAddition(decimal.MaxValue, 1.0m));
        }

        //
        // IAdditiveIdentity
        //

        [Fact]
        public static void AdditiveIdentityTest()
        {
            Assert.Equal(0.0m, AdditiveIdentityHelper<decimal, decimal>.AdditiveIdentity);
        }

        //
        // IComparisonOperators
        //

        [Fact]
        public static void op_GreaterThanTest()
        {
            Assert.False(ComparisonOperatorsHelper<decimal, decimal>.op_GreaterThan(decimal.MinValue, 1.0m));
            Assert.False(ComparisonOperatorsHelper<decimal, decimal>.op_GreaterThan(-1.0m, 1.0m));
            Assert.False(ComparisonOperatorsHelper<decimal, decimal>.op_GreaterThan(-0.0m, 1.0m));
            Assert.False(ComparisonOperatorsHelper<decimal, decimal>.op_GreaterThan(0.0m, 1.0m));
            Assert.False(ComparisonOperatorsHelper<decimal, decimal>.op_GreaterThan(1.0m, 1.0m));
            Assert.True(ComparisonOperatorsHelper<decimal, decimal>.op_GreaterThan(decimal.MaxValue, 1.0m));
        }

        [Fact]
        public static void op_GreaterThanOrEqualTest()
        {
            Assert.False(ComparisonOperatorsHelper<decimal, decimal>.op_GreaterThanOrEqual(decimal.MinValue, 1.0m));
            Assert.False(ComparisonOperatorsHelper<decimal, decimal>.op_GreaterThanOrEqual(-1.0m, 1.0m));
            Assert.False(ComparisonOperatorsHelper<decimal, decimal>.op_GreaterThanOrEqual(-0.0m, 1.0m));
            Assert.False(ComparisonOperatorsHelper<decimal, decimal>.op_GreaterThanOrEqual(0.0m, 1.0m));
            Assert.True(ComparisonOperatorsHelper<decimal, decimal>.op_GreaterThanOrEqual(1.0m, 1.0m));
            Assert.True(ComparisonOperatorsHelper<decimal, decimal>.op_GreaterThanOrEqual(decimal.MaxValue, 1.0m));
        }

        [Fact]
        public static void op_LessThanTest()
        {
            Assert.True(ComparisonOperatorsHelper<decimal, decimal>.op_LessThan(decimal.MinValue, 1.0m));
            Assert.True(ComparisonOperatorsHelper<decimal, decimal>.op_LessThan(-1.0m, 1.0m));
            Assert.True(ComparisonOperatorsHelper<decimal, decimal>.op_LessThan(-0.0m, 1.0m));
            Assert.True(ComparisonOperatorsHelper<decimal, decimal>.op_LessThan(0.0m, 1.0m));
            Assert.False(ComparisonOperatorsHelper<decimal, decimal>.op_LessThan(1.0m, 1.0m));
            Assert.False(ComparisonOperatorsHelper<decimal, decimal>.op_LessThan(decimal.MaxValue, 1.0m));
        }

        [Fact]
        public static void op_LessThanOrEqualTest()
        {
            Assert.True(ComparisonOperatorsHelper<decimal, decimal>.op_LessThanOrEqual(decimal.MinValue, 1.0m));
            Assert.True(ComparisonOperatorsHelper<decimal, decimal>.op_LessThanOrEqual(-1.0m, 1.0m));
            Assert.True(ComparisonOperatorsHelper<decimal, decimal>.op_LessThanOrEqual(-0.0m, 1.0m));
            Assert.True(ComparisonOperatorsHelper<decimal, decimal>.op_LessThanOrEqual(0.0m, 1.0m));
            Assert.True(ComparisonOperatorsHelper<decimal, decimal>.op_LessThanOrEqual(1.0m, 1.0m));
            Assert.False(ComparisonOperatorsHelper<decimal, decimal>.op_LessThanOrEqual(decimal.MaxValue, 1.0m));
        }

        //
        // IDecrementOperators
        //

        [Fact]
        public static void op_DecrementTest()
        {
            Assert.Equal(-2.0m, DecrementOperatorsHelper<decimal>.op_Decrement(-1.0m));
            Assert.Equal(-1.0m, DecrementOperatorsHelper<decimal>.op_Decrement(-0.0m));
            Assert.Equal(-1.0m, DecrementOperatorsHelper<decimal>.op_Decrement(0.0m));
            Assert.Equal(0.0m, DecrementOperatorsHelper<decimal>.op_Decrement(1.0m));
            Assert.Equal(79228162514264337593543950334.0m, DecrementOperatorsHelper<decimal>.op_Decrement(decimal.MaxValue));

            Assert.Throws<OverflowException>(() => DecrementOperatorsHelper<decimal>.op_Decrement(decimal.MinValue));
        }

        [Fact]
        public static void op_CheckedDecrementTest()
        {
            Assert.Equal(-2.0m, DecrementOperatorsHelper<decimal>.op_CheckedDecrement(-1.0m));
            Assert.Equal(-1.0m, DecrementOperatorsHelper<decimal>.op_CheckedDecrement(-0.0m));
            Assert.Equal(-1.0m, DecrementOperatorsHelper<decimal>.op_CheckedDecrement(0.0m));
            Assert.Equal(0.0m, DecrementOperatorsHelper<decimal>.op_CheckedDecrement(1.0m));
            Assert.Equal(79228162514264337593543950334.0m, DecrementOperatorsHelper<decimal>.op_CheckedDecrement(decimal.MaxValue));

            Assert.Throws<OverflowException>(() => DecrementOperatorsHelper<decimal>.op_CheckedDecrement(decimal.MinValue));
        }

        //
        // IDivisionOperators
        //

        [Fact]
        public static void op_DivisionTest()
        {
            Assert.Equal(-39614081257132168796771975168.0m, DivisionOperatorsHelper<decimal, decimal, decimal>.op_Division(decimal.MinValue, 2.0m));
            Assert.Equal(-0.5m, DivisionOperatorsHelper<decimal, decimal, decimal>.op_Division(-1.0m, 2.0m));
            Assert.Equal(-0.0m, DivisionOperatorsHelper<decimal, decimal, decimal>.op_Division(-0.0m, 2.0m));
            Assert.Equal(0.0m, DivisionOperatorsHelper<decimal, decimal, decimal>.op_Division(0.0m, 2.0m));
            Assert.Equal(0.5m, DivisionOperatorsHelper<decimal, decimal, decimal>.op_Division(1.0m, 2.0m));
            Assert.Equal(39614081257132168796771975168.0m, DivisionOperatorsHelper<decimal, decimal, decimal>.op_Division(decimal.MaxValue, 2.0m));
        }

        [Fact]
        public static void op_CheckedDivisionTest()
        {
            Assert.Equal(-39614081257132168796771975168.0m, DivisionOperatorsHelper<decimal, decimal, decimal>.op_CheckedDivision(decimal.MinValue, 2.0m));
            Assert.Equal(-0.5m, DivisionOperatorsHelper<decimal, decimal, decimal>.op_CheckedDivision(-1.0m, 2.0m));
            Assert.Equal(-0.0m, DivisionOperatorsHelper<decimal, decimal, decimal>.op_CheckedDivision(-0.0m, 2.0m));
            Assert.Equal(0.0m, DivisionOperatorsHelper<decimal, decimal, decimal>.op_CheckedDivision(0.0m, 2.0m));
            Assert.Equal(0.5m, DivisionOperatorsHelper<decimal, decimal, decimal>.op_CheckedDivision(1.0m, 2.0m));
            Assert.Equal(39614081257132168796771975168.0m, DivisionOperatorsHelper<decimal, decimal, decimal>.op_CheckedDivision(decimal.MaxValue, 2.0m));
        }

        //
        // IEqualityOperators
        //

        [Fact]
        public static void op_EqualityTest()
        {
            Assert.False(EqualityOperatorsHelper<decimal, decimal>.op_Equality(decimal.MinValue, 1.0m));
            Assert.False(EqualityOperatorsHelper<decimal, decimal>.op_Equality(-1.0m, 1.0m));
            Assert.False(EqualityOperatorsHelper<decimal, decimal>.op_Equality(-0.0m, 1.0m));
            Assert.False(EqualityOperatorsHelper<decimal, decimal>.op_Equality(0.0m, 1.0m));
            Assert.True(EqualityOperatorsHelper<decimal, decimal>.op_Equality(1.0m, 1.0m));
            Assert.False(EqualityOperatorsHelper<decimal, decimal>.op_Equality(decimal.MaxValue, 1.0m));
        }

        [Fact]
        public static void op_InequalityTest()
        {
            Assert.True(EqualityOperatorsHelper<decimal, decimal>.op_Inequality(decimal.MinValue, 1.0m));
            Assert.True(EqualityOperatorsHelper<decimal, decimal>.op_Inequality(-1.0m, 1.0m));
            Assert.True(EqualityOperatorsHelper<decimal, decimal>.op_Inequality(-0.0m, 1.0m));
            Assert.True(EqualityOperatorsHelper<decimal, decimal>.op_Inequality(0.0m, 1.0m));
            Assert.False(EqualityOperatorsHelper<decimal, decimal>.op_Inequality(1.0m, 1.0m));
            Assert.True(EqualityOperatorsHelper<decimal, decimal>.op_Inequality(decimal.MaxValue, 1.0m));
        }

        //
        // IFloatingPoint
        //

        [Fact]
        public static void GetExponentByteCountTest()
        {
            Assert.Equal(1, FloatingPointHelper<decimal>.GetExponentByteCount(decimal.MinValue));
            Assert.Equal(1, FloatingPointHelper<decimal>.GetExponentByteCount(-1.0m));
            Assert.Equal(1, FloatingPointHelper<decimal>.GetExponentByteCount(-0.0m));
            Assert.Equal(1, FloatingPointHelper<decimal>.GetExponentByteCount(0.0m));
            Assert.Equal(1, FloatingPointHelper<decimal>.GetExponentByteCount(1.0m));
            Assert.Equal(1, FloatingPointHelper<decimal>.GetExponentByteCount(decimal.MaxValue));
        }

        [Fact]
        public static void GetExponentShortestBitLengthTest()
        {
            Assert.Equal(7, FloatingPointHelper<decimal>.GetExponentShortestBitLength(decimal.MinValue));
            Assert.Equal(7, FloatingPointHelper<decimal>.GetExponentShortestBitLength(-1.0m));
            Assert.Equal(7, FloatingPointHelper<decimal>.GetExponentShortestBitLength(-0.0m));
            Assert.Equal(7, FloatingPointHelper<decimal>.GetExponentShortestBitLength(0.0m));
            Assert.Equal(7, FloatingPointHelper<decimal>.GetExponentShortestBitLength(1.0m));
            Assert.Equal(7, FloatingPointHelper<decimal>.GetExponentShortestBitLength(decimal.MaxValue));
        }

        [Fact]
        public static void GetSignificandByteCountTest()
        {
            Assert.Equal(12, FloatingPointHelper<decimal>.GetSignificandByteCount(decimal.MinValue));
            Assert.Equal(12, FloatingPointHelper<decimal>.GetSignificandByteCount(-1.0m));
            Assert.Equal(12, FloatingPointHelper<decimal>.GetSignificandByteCount(-0.0m));
            Assert.Equal(12, FloatingPointHelper<decimal>.GetSignificandByteCount(0.0m));
            Assert.Equal(12, FloatingPointHelper<decimal>.GetSignificandByteCount(1.0m));
            Assert.Equal(12, FloatingPointHelper<decimal>.GetSignificandByteCount(decimal.MaxValue));
        }

        [Fact]
        public static void GetSignificandBitLengthTest()
        {
            Assert.Equal(96, FloatingPointHelper<decimal>.GetSignificandBitLength(decimal.MinValue));
            Assert.Equal(96, FloatingPointHelper<decimal>.GetSignificandBitLength(-1.0m));
            Assert.Equal(96, FloatingPointHelper<decimal>.GetSignificandBitLength(-0.0m));
            Assert.Equal(96, FloatingPointHelper<decimal>.GetSignificandBitLength(0.0m));
            Assert.Equal(96, FloatingPointHelper<decimal>.GetSignificandBitLength(1.0m));
            Assert.Equal(96, FloatingPointHelper<decimal>.GetSignificandBitLength(decimal.MaxValue));
        }

        [Fact]
        public static void TryWriteExponentBigEndianTest()
        {
            Span<byte> destination = stackalloc byte[1];
            int bytesWritten = 0;

            Assert.True(FloatingPointHelper<decimal>.TryWriteExponentBigEndian(decimal.MinValue, destination, out bytesWritten));
            Assert.Equal(1, bytesWritten);
            Assert.Equal(new byte[] { 0x5F }, destination.ToArray()); // +95

            Assert.True(FloatingPointHelper<decimal>.TryWriteExponentBigEndian(-1.0m, destination, out bytesWritten));
            Assert.Equal(1, bytesWritten);
            Assert.Equal(new byte[] { 0x5E }, destination.ToArray()); // +94

            Assert.True(FloatingPointHelper<decimal>.TryWriteExponentBigEndian(-0.0m, destination, out bytesWritten));
            Assert.Equal(1, bytesWritten);
            Assert.Equal(new byte[] { 0x5E }, destination.ToArray()); // +94

            Assert.True(FloatingPointHelper<decimal>.TryWriteExponentBigEndian(0.0m, destination, out bytesWritten));
            Assert.Equal(1, bytesWritten);
            Assert.Equal(new byte[] { 0x5E }, destination.ToArray()); // +94

            Assert.True(FloatingPointHelper<decimal>.TryWriteExponentBigEndian(1.0m, destination, out bytesWritten));
            Assert.Equal(1, bytesWritten);
            Assert.Equal(new byte[] { 0x5E }, destination.ToArray()); // +94

            Assert.True(FloatingPointHelper<decimal>.TryWriteExponentBigEndian(decimal.MaxValue, destination, out bytesWritten));
            Assert.Equal(1, bytesWritten);
            Assert.Equal(new byte[] { 0x5F }, destination.ToArray()); // +95

            Assert.False(FloatingPointHelper<decimal>.TryWriteExponentBigEndian(default, Span<byte>.Empty, out bytesWritten));
            Assert.Equal(0, bytesWritten);
            Assert.Equal(new byte[] { 0x5F }, destination.ToArray());
        }

        [Fact]
        public static void TryWriteExponentLittleEndianTest()
        {
            Span<byte> destination = stackalloc byte[1];
            int bytesWritten = 0;

            Assert.True(FloatingPointHelper<decimal>.TryWriteExponentLittleEndian(decimal.MinValue, destination, out bytesWritten));
            Assert.Equal(1, bytesWritten);
            Assert.Equal(new byte[] { 0x5F }, destination.ToArray()); // +95

            Assert.True(FloatingPointHelper<decimal>.TryWriteExponentLittleEndian(-1.0m, destination, out bytesWritten));
            Assert.Equal(1, bytesWritten);
            Assert.Equal(new byte[] { 0x5E }, destination.ToArray()); // +94

            Assert.True(FloatingPointHelper<decimal>.TryWriteExponentLittleEndian(-0.0m, destination, out bytesWritten));
            Assert.Equal(1, bytesWritten);
            Assert.Equal(new byte[] { 0x5E }, destination.ToArray()); // +94

            Assert.True(FloatingPointHelper<decimal>.TryWriteExponentLittleEndian(0.0m, destination, out bytesWritten));
            Assert.Equal(1, bytesWritten);
            Assert.Equal(new byte[] { 0x5E }, destination.ToArray()); // +94

            Assert.True(FloatingPointHelper<decimal>.TryWriteExponentLittleEndian(1.0m, destination, out bytesWritten));
            Assert.Equal(1, bytesWritten);
            Assert.Equal(new byte[] { 0x5E }, destination.ToArray()); // +94

            Assert.True(FloatingPointHelper<decimal>.TryWriteExponentLittleEndian(decimal.MaxValue, destination, out bytesWritten));
            Assert.Equal(1, bytesWritten);
            Assert.Equal(new byte[] { 0x5F }, destination.ToArray()); // +95

            Assert.False(FloatingPointHelper<decimal>.TryWriteExponentLittleEndian(default, Span<byte>.Empty, out bytesWritten));
            Assert.Equal(0, bytesWritten);
            Assert.Equal(new byte[] { 0x5F }, destination.ToArray());
        }

        [Fact]
        public static void TryWriteSignificandBigEndianTest()
        {
            Span<byte> destination = stackalloc byte[12];
            int bytesWritten = 0;

            Assert.True(FloatingPointHelper<decimal>.TryWriteSignificandBigEndian(decimal.MinValue, destination, out bytesWritten));
            Assert.Equal(12, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, destination.ToArray());

            Assert.True(FloatingPointHelper<decimal>.TryWriteSignificandBigEndian(-1.0m, destination, out bytesWritten));
            Assert.Equal(12, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0A }, destination.ToArray());

            Assert.True(FloatingPointHelper<decimal>.TryWriteSignificandBigEndian(-0.0m, destination, out bytesWritten));
            Assert.Equal(12, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

            Assert.True(FloatingPointHelper<decimal>.TryWriteSignificandBigEndian(0.0m, destination, out bytesWritten));
            Assert.Equal(12, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

            Assert.True(FloatingPointHelper<decimal>.TryWriteSignificandBigEndian(1.0m, destination, out bytesWritten));
            Assert.Equal(12, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0A }, destination.ToArray());

            Assert.True(FloatingPointHelper<decimal>.TryWriteSignificandBigEndian(decimal.MaxValue, destination, out bytesWritten));
            Assert.Equal(12, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, destination.ToArray());

            Assert.False(FloatingPointHelper<decimal>.TryWriteSignificandBigEndian(default, Span<byte>.Empty, out bytesWritten));
            Assert.Equal(0, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, destination.ToArray());
        }

        [Fact]
        public static void TryWriteSignificandLittleEndianTest()
        {
            Span<byte> destination = stackalloc byte[12];
            int bytesWritten = 0;

            Assert.True(FloatingPointHelper<decimal>.TryWriteSignificandLittleEndian(decimal.MinValue, destination, out bytesWritten));
            Assert.Equal(12, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, destination.ToArray());

            Assert.True(FloatingPointHelper<decimal>.TryWriteSignificandLittleEndian(-1.0m, destination, out bytesWritten));
            Assert.Equal(12, bytesWritten);
            Assert.Equal(new byte[] { 0x0A, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

            Assert.True(FloatingPointHelper<decimal>.TryWriteSignificandLittleEndian(-0.0m, destination, out bytesWritten));
            Assert.Equal(12, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

            Assert.True(FloatingPointHelper<decimal>.TryWriteSignificandLittleEndian(0.0m, destination, out bytesWritten));
            Assert.Equal(12, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

            Assert.True(FloatingPointHelper<decimal>.TryWriteSignificandLittleEndian(1.0m, destination, out bytesWritten));
            Assert.Equal(12, bytesWritten);
            Assert.Equal(new byte[] { 0x0A, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

            Assert.True(FloatingPointHelper<decimal>.TryWriteSignificandLittleEndian(decimal.MaxValue, destination, out bytesWritten));
            Assert.Equal(12, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, destination.ToArray());

            Assert.False(FloatingPointHelper<decimal>.TryWriteSignificandLittleEndian(default, Span<byte>.Empty, out bytesWritten));
            Assert.Equal(0, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, destination.ToArray());
        }

        //
        // IIncrementOperators
        //

        [Fact]
        public static void op_IncrementTest()
        {
            Assert.Equal(-79228162514264337593543950334.0m, IncrementOperatorsHelper<decimal>.op_Increment(decimal.MinValue));
            Assert.Equal(0.0m, IncrementOperatorsHelper<decimal>.op_Increment(-1.0m));
            Assert.Equal(1.0m, IncrementOperatorsHelper<decimal>.op_Increment(-0.0m));
            Assert.Equal(1.0m, IncrementOperatorsHelper<decimal>.op_Increment(0.0m));
            Assert.Equal(2.0m, IncrementOperatorsHelper<decimal>.op_Increment(1.0m));

            Assert.Throws<OverflowException>(() => IncrementOperatorsHelper<decimal>.op_Increment(decimal.MaxValue));
        }

        [Fact]
        public static void op_CheckedIncrementTest()
        {
            Assert.Equal(-79228162514264337593543950334.0m, IncrementOperatorsHelper<decimal>.op_CheckedIncrement(decimal.MinValue));
            Assert.Equal(0.0m, IncrementOperatorsHelper<decimal>.op_CheckedIncrement(-1.0m));
            Assert.Equal(1.0m, IncrementOperatorsHelper<decimal>.op_CheckedIncrement(-0.0m));
            Assert.Equal(1.0m, IncrementOperatorsHelper<decimal>.op_CheckedIncrement(0.0m));
            Assert.Equal(2.0m, IncrementOperatorsHelper<decimal>.op_CheckedIncrement(1.0m));

            Assert.Throws<OverflowException>(() => IncrementOperatorsHelper<decimal>.op_CheckedIncrement(decimal.MaxValue));
        }

        //
        // IMinMaxValue
        //

        [Fact]
        public static void MaxValueTest()
        {
            Assert.Equal(decimal.MaxValue, MinMaxValueHelper<decimal>.MaxValue);
        }

        [Fact]
        public static void MinValueTest()
        {
            Assert.Equal(decimal.MinValue, MinMaxValueHelper<decimal>.MinValue);
        }

        //
        // IModulusOperators
        //

        [Fact]
        public static void op_ModulusTest()
        {
            Assert.Equal(-1.0m, ModulusOperatorsHelper<decimal, decimal, decimal>.op_Modulus(decimal.MinValue, 2.0m));
            Assert.Equal(-1.0m, ModulusOperatorsHelper<decimal, decimal, decimal>.op_Modulus(-1.0m, 2.0m));
            Assert.Equal(-0.0m, ModulusOperatorsHelper<decimal, decimal, decimal>.op_Modulus(-0.0m, 2.0m));
            Assert.Equal(0.0m, ModulusOperatorsHelper<decimal, decimal, decimal>.op_Modulus(0.0m, 2.0m));
            Assert.Equal(1.0m, ModulusOperatorsHelper<decimal, decimal, decimal>.op_Modulus(1.0m, 2.0m));
            Assert.Equal(1.0m, ModulusOperatorsHelper<decimal, decimal, decimal>.op_Modulus(decimal.MaxValue, 2.0m));
        }

        //
        // IMultiplicativeIdentity
        //

        [Fact]
        public static void MultiplicativeIdentityTest()
        {
            Assert.Equal(1.0m, MultiplicativeIdentityHelper<decimal, decimal>.MultiplicativeIdentity);
        }

        //
        // IMultiplyOperators
        //

        [Fact]
        public static void op_MultiplyTest()
        {
            Assert.Equal(-2.0m, MultiplyOperatorsHelper<decimal, decimal, decimal>.op_Multiply(-1.0m, 2.0m));
            Assert.Equal(-0.0m, MultiplyOperatorsHelper<decimal, decimal, decimal>.op_Multiply(-0.0m, 2.0m));
            Assert.Equal(0.0m, MultiplyOperatorsHelper<decimal, decimal, decimal>.op_Multiply(0.0m, 2.0m));
            Assert.Equal(2.0m, MultiplyOperatorsHelper<decimal, decimal, decimal>.op_Multiply(1.0m, 2.0m));

            Assert.Throws<OverflowException>(() => MultiplyOperatorsHelper<decimal, decimal, decimal>.op_Multiply(decimal.MinValue, 2.0m));
            Assert.Throws<OverflowException>(() => MultiplyOperatorsHelper<decimal, decimal, decimal>.op_Multiply(decimal.MaxValue, 2.0m));
        }

        [Fact]
        public static void op_CheckedMultiplyTest()
        {
            Assert.Equal(-2.0m, MultiplyOperatorsHelper<decimal, decimal, decimal>.op_CheckedMultiply(-1.0m, 2.0m));
            Assert.Equal(-0.0m, MultiplyOperatorsHelper<decimal, decimal, decimal>.op_CheckedMultiply(-0.0m, 2.0m));
            Assert.Equal(0.0m, MultiplyOperatorsHelper<decimal, decimal, decimal>.op_CheckedMultiply(0.0m, 2.0m));
            Assert.Equal(2.0m, MultiplyOperatorsHelper<decimal, decimal, decimal>.op_CheckedMultiply(1.0m, 2.0m));

            Assert.Throws<OverflowException>(() => MultiplyOperatorsHelper<decimal, decimal, decimal>.op_CheckedMultiply(decimal.MinValue, 2.0m));
            Assert.Throws<OverflowException>(() => MultiplyOperatorsHelper<decimal, decimal, decimal>.op_CheckedMultiply(decimal.MaxValue, 2.0m));
        }

        //
        // INumber
        //

        [Fact]
        public static void ClampTest()
        {
            Assert.Equal(1.0m, NumberHelper<decimal>.Clamp(decimal.MinValue, 1.0m, 63.0m));
            Assert.Equal(1.0m, NumberHelper<decimal>.Clamp(-1.0m, 1.0m, 63.0m));
            Assert.Equal(1.0m, NumberHelper<decimal>.Clamp(-0.0m, 1.0m, 63.0m));
            Assert.Equal(1.0m, NumberHelper<decimal>.Clamp(0.0m, 1.0m, 63.0m));
            Assert.Equal(1.0m, NumberHelper<decimal>.Clamp(1.0m, 1.0m, 63.0m));
            Assert.Equal(63.0m, NumberHelper<decimal>.Clamp(decimal.MaxValue, 1.0m, 63.0m));
        }

        [Fact]
        public static void MaxTest()
        {
            Assert.Equal(1.0m, NumberHelper<decimal>.Max(decimal.MinValue, 1.0m));
            Assert.Equal(1.0m, NumberHelper<decimal>.Max(-1.0m, 1.0m));
            Assert.Equal(1.0m, NumberHelper<decimal>.Max(-0.0m, 1.0m));
            Assert.Equal(1.0m, NumberHelper<decimal>.Max(0.0m, 1.0m));
            Assert.Equal(1.0m, NumberHelper<decimal>.Max(1.0m, 1.0m));
            Assert.Equal(decimal.MaxValue, NumberHelper<decimal>.Max(decimal.MaxValue, 1.0m));
        }

        [Fact]
        public static void MaxNumberTest()
        {
            Assert.Equal(1.0m, NumberHelper<decimal>.MaxNumber(decimal.MinValue, 1.0m));
            Assert.Equal(1.0m, NumberHelper<decimal>.MaxNumber(-1.0m, 1.0m));
            Assert.Equal(1.0m, NumberHelper<decimal>.MaxNumber(-0.0m, 1.0m));
            Assert.Equal(1.0m, NumberHelper<decimal>.MaxNumber(0.0m, 1.0m));
            Assert.Equal(1.0m, NumberHelper<decimal>.MaxNumber(1.0m, 1.0m));
            Assert.Equal(decimal.MaxValue, NumberHelper<decimal>.MaxNumber(decimal.MaxValue, 1.0m));
        }

        [Fact]
        public static void MinTest()
        {
            Assert.Equal(decimal.MinValue, NumberHelper<decimal>.Min(decimal.MinValue, 1.0m));
            Assert.Equal(-1.0m, NumberHelper<decimal>.Min(-1.0m, 1.0m));
            Assert.Equal(-0.0m, NumberHelper<decimal>.Min(-0.0m, 1.0m));
            Assert.Equal(0.0m, NumberHelper<decimal>.Min(0.0m, 1.0m));
            Assert.Equal(1.0m, NumberHelper<decimal>.Min(1.0m, 1.0m));
            Assert.Equal(1.0m, NumberHelper<decimal>.Min(decimal.MaxValue, 1.0m));
        }

        [Fact]
        public static void MinNumberTest()
        {
            Assert.Equal(decimal.MinValue, NumberHelper<decimal>.MinNumber(decimal.MinValue, 1.0m));
            Assert.Equal(-1.0m, NumberHelper<decimal>.MinNumber(-1.0m, 1.0m));
            Assert.Equal(-0.0m, NumberHelper<decimal>.MinNumber(-0.0m, 1.0m));
            Assert.Equal(0.0m, NumberHelper<decimal>.MinNumber(0.0m, 1.0m));
            Assert.Equal(1.0m, NumberHelper<decimal>.MinNumber(1.0m, 1.0m));
            Assert.Equal(1.0m, NumberHelper<decimal>.MinNumber(decimal.MaxValue, 1.0m));
        }

        [Fact]
        public static void SignTest()
        {
            Assert.Equal(-1, NumberHelper<decimal>.Sign(decimal.MinValue));
            Assert.Equal(-1, NumberHelper<decimal>.Sign(-1.0m));

            Assert.Equal(0, NumberHelper<decimal>.Sign(-0.0m));
            Assert.Equal(0, NumberHelper<decimal>.Sign(0.0m));

            Assert.Equal(1, NumberHelper<decimal>.Sign(1.0m));
            Assert.Equal(1, NumberHelper<decimal>.Sign(decimal.MaxValue));
        }

        //
        // INumberBase
        //

        [Fact]
        public static void OneTest()
        {
            Assert.Equal(1.0m, NumberBaseHelper<decimal>.One);
        }

        [Fact]
        public static void ZeroTest()
        {
            Assert.Equal(0.0m, NumberBaseHelper<decimal>.Zero);
        }

        [Fact]
        public static void AbsTest()
        {
            Assert.Equal(decimal.MaxValue, NumberBaseHelper<decimal>.Abs(decimal.MinValue));
            Assert.Equal(1.0m, NumberBaseHelper<decimal>.Abs(-1.0m));
            Assert.Equal(0.0m, NumberBaseHelper<decimal>.Abs(-0.0m));
            Assert.Equal(0.0m, NumberBaseHelper<decimal>.Abs(0.0m));
            Assert.Equal(1.0m, NumberBaseHelper<decimal>.Abs(1.0m));
            Assert.Equal(decimal.MaxValue, NumberBaseHelper<decimal>.Abs(decimal.MaxValue));
        }

        [Fact]
        public static void CreateCheckedFromByteTest()
        {
            Assert.Equal(0.0m, NumberBaseHelper<decimal>.CreateChecked<byte>(0x00));
            Assert.Equal(1.0m, NumberBaseHelper<decimal>.CreateChecked<byte>(0x01));
            Assert.Equal(127.0m, NumberBaseHelper<decimal>.CreateChecked<byte>(0x7F));
            Assert.Equal(128.0m, NumberBaseHelper<decimal>.CreateChecked<byte>(0x80));
            Assert.Equal(255.0m, NumberBaseHelper<decimal>.CreateChecked<byte>(0xFF));
        }

        [Fact]
        public static void CreateCheckedFromCharTest()
        {
            Assert.Equal(0.0m, NumberBaseHelper<decimal>.CreateChecked<char>((char)0x0000));
            Assert.Equal(1.0m, NumberBaseHelper<decimal>.CreateChecked<char>((char)0x0001));
            Assert.Equal(32767.0m, NumberBaseHelper<decimal>.CreateChecked<char>((char)0x7FFF));
            Assert.Equal(32768.0m, NumberBaseHelper<decimal>.CreateChecked<char>((char)0x8000));
            Assert.Equal(65535.0m, NumberBaseHelper<decimal>.CreateChecked<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateCheckedFromInt16Test()
        {
            Assert.Equal(0.0m, NumberBaseHelper<decimal>.CreateChecked<short>(0x0000));
            Assert.Equal(1.0m, NumberBaseHelper<decimal>.CreateChecked<short>(0x0001));
            Assert.Equal(32767.0m, NumberBaseHelper<decimal>.CreateChecked<short>(0x7FFF));
            Assert.Equal(-32768.0m, NumberBaseHelper<decimal>.CreateChecked<short>(unchecked((short)0x8000)));
            Assert.Equal(-1.0m, NumberBaseHelper<decimal>.CreateChecked<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromInt32Test()
        {
            Assert.Equal(0.0m, NumberBaseHelper<decimal>.CreateChecked<int>(0x00000000));
            Assert.Equal(1.0m, NumberBaseHelper<decimal>.CreateChecked<int>(0x00000001));
            Assert.Equal(2147483647.0m, NumberBaseHelper<decimal>.CreateChecked<int>(0x7FFFFFFF));
            Assert.Equal(-2147483648.0m, NumberBaseHelper<decimal>.CreateChecked<int>(unchecked((int)0x80000000)));
            Assert.Equal(-1.0m, NumberBaseHelper<decimal>.CreateChecked<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromInt64Test()
        {
            Assert.Equal(0.0m, NumberBaseHelper<decimal>.CreateChecked<long>(0x0000000000000000));
            Assert.Equal(1.0m, NumberBaseHelper<decimal>.CreateChecked<long>(0x0000000000000001));
            Assert.Equal(9223372036854775807.0m, NumberBaseHelper<decimal>.CreateChecked<long>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal(-9223372036854775808.0m, NumberBaseHelper<decimal>.CreateChecked<long>(unchecked(unchecked((long)0x8000000000000000))));
            Assert.Equal(-1.0m, NumberBaseHelper<decimal>.CreateChecked<long>(unchecked(unchecked((long)0xFFFFFFFFFFFFFFFF))));
        }

        [Fact]
        public static void CreateCheckedFromInt128Test()
        {
            Assert.Equal(0.0m, NumberBaseHelper<decimal>.CreateChecked<Int128>(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000)));
            Assert.Equal(1.0m, NumberBaseHelper<decimal>.CreateChecked<Int128>(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001)));
            Assert.Equal(-1.0m, NumberBaseHelper<decimal>.CreateChecked<Int128>(new Int128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<decimal>.CreateChecked<Int128>(new Int128(0x7FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<decimal>.CreateChecked<Int128>(new Int128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000)));
        }

        [Fact]
        public static void CreateCheckedFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(0.0m, NumberBaseHelper<decimal>.CreateChecked<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal(1.0m, NumberBaseHelper<decimal>.CreateChecked<nint>(unchecked((nint)0x0000000000000001)));
                Assert.Equal(9223372036854775807.0m, NumberBaseHelper<decimal>.CreateChecked<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(-9223372036854775808.0m, NumberBaseHelper<decimal>.CreateChecked<nint>(unchecked((nint)0x8000000000000000)));
                Assert.Equal(-1.0m, NumberBaseHelper<decimal>.CreateChecked<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal(0.0m, NumberBaseHelper<decimal>.CreateChecked<nint>((nint)0x00000000));
                Assert.Equal(1.0m, NumberBaseHelper<decimal>.CreateChecked<nint>((nint)0x00000001));
                Assert.Equal(2147483647.0m, NumberBaseHelper<decimal>.CreateChecked<nint>((nint)0x7FFFFFFF));
                Assert.Equal(-2147483648.0m, NumberBaseHelper<decimal>.CreateChecked<nint>(unchecked((nint)0x80000000)));
                Assert.Equal(-1.0m, NumberBaseHelper<decimal>.CreateChecked<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateCheckedFromSByteTest()
        {
            Assert.Equal(0.0m, NumberBaseHelper<decimal>.CreateChecked<sbyte>(0x00));
            Assert.Equal(1.0m, NumberBaseHelper<decimal>.CreateChecked<sbyte>(0x01));
            Assert.Equal(127.0m, NumberBaseHelper<decimal>.CreateChecked<sbyte>(0x7F));
            Assert.Equal(-128.0m, NumberBaseHelper<decimal>.CreateChecked<sbyte>(unchecked((sbyte)0x80)));
            Assert.Equal(-1.0m, NumberBaseHelper<decimal>.CreateChecked<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateCheckedFromUInt16Test()
        {
            Assert.Equal(0.0m, NumberBaseHelper<decimal>.CreateChecked<ushort>(0x0000));
            Assert.Equal(1.0m, NumberBaseHelper<decimal>.CreateChecked<ushort>(0x0001));
            Assert.Equal(32767.0m, NumberBaseHelper<decimal>.CreateChecked<ushort>(0x7FFF));
            Assert.Equal(32768.0m, NumberBaseHelper<decimal>.CreateChecked<ushort>(0x8000));
            Assert.Equal(65535.0m, NumberBaseHelper<decimal>.CreateChecked<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUInt32Test()
        {
            Assert.Equal(0.0m, NumberBaseHelper<decimal>.CreateChecked<uint>(0x00000000));
            Assert.Equal(1.0m, NumberBaseHelper<decimal>.CreateChecked<uint>(0x00000001));
            Assert.Equal(2147483647.0m, NumberBaseHelper<decimal>.CreateChecked<uint>(0x7FFFFFFF));
            Assert.Equal(2147483648.0m, NumberBaseHelper<decimal>.CreateChecked<uint>(0x80000000));
            Assert.Equal(4294967295.0m, NumberBaseHelper<decimal>.CreateChecked<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUInt64Test()
        {
            Assert.Equal(0.0m, NumberBaseHelper<decimal>.CreateChecked<ulong>(0x0000000000000000));
            Assert.Equal(1.0m, NumberBaseHelper<decimal>.CreateChecked<ulong>(0x0000000000000001));
            Assert.Equal(9223372036854775807.0m, NumberBaseHelper<decimal>.CreateChecked<ulong>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal(9223372036854775808.0m, NumberBaseHelper<decimal>.CreateChecked<ulong>(0x8000000000000000));
            Assert.Equal(18446744073709551615.0m, NumberBaseHelper<decimal>.CreateChecked<ulong>(0xFFFFFFFFFFFFFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUInt128Test()
        {
            Assert.Equal(0.0m, NumberBaseHelper<decimal>.CreateChecked<UInt128>(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000)));
            Assert.Equal(1.0m, NumberBaseHelper<decimal>.CreateChecked<UInt128>(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001)));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<decimal>.CreateChecked<UInt128>(new UInt128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<decimal>.CreateChecked<UInt128>(new UInt128(0x7FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<decimal>.CreateChecked<UInt128>(new UInt128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000)));
        }

        [Fact]
        public static void CreateCheckedFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(0.0m, NumberBaseHelper<decimal>.CreateChecked<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal(1.0m, NumberBaseHelper<decimal>.CreateChecked<nuint>(unchecked((nuint)0x0000000000000001)));
                Assert.Equal(9223372036854775807.0m, NumberBaseHelper<decimal>.CreateChecked<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(9223372036854775808.0m, NumberBaseHelper<decimal>.CreateChecked<nuint>(unchecked((nuint)0x8000000000000000)));
                Assert.Equal(18446744073709551615.0m, NumberBaseHelper<decimal>.CreateChecked<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal(0.0m, NumberBaseHelper<decimal>.CreateChecked<nuint>((nuint)0x00000000));
                Assert.Equal(1.0m, NumberBaseHelper<decimal>.CreateChecked<nuint>((nuint)0x00000001));
                Assert.Equal(2147483647.0m, NumberBaseHelper<decimal>.CreateChecked<nuint>((nuint)0x7FFFFFFF));
                Assert.Equal(2147483648.0m, NumberBaseHelper<decimal>.CreateChecked<nuint>((nuint)0x80000000));
                Assert.Equal(4294967295.0m, NumberBaseHelper<decimal>.CreateChecked<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateSaturatingFromByteTest()
        {
            Assert.Equal(0.0m, NumberBaseHelper<decimal>.CreateSaturating<byte>(0x00));
            Assert.Equal(1.0m, NumberBaseHelper<decimal>.CreateSaturating<byte>(0x01));
            Assert.Equal(127.0m, NumberBaseHelper<decimal>.CreateSaturating<byte>(0x7F));
            Assert.Equal(128.0m, NumberBaseHelper<decimal>.CreateSaturating<byte>(0x80));
            Assert.Equal(255.0m, NumberBaseHelper<decimal>.CreateSaturating<byte>(0xFF));
        }

        [Fact]
        public static void CreateSaturatingFromCharTest()
        {
            Assert.Equal(0.0m, NumberBaseHelper<decimal>.CreateSaturating<char>((char)0x0000));
            Assert.Equal(1.0m, NumberBaseHelper<decimal>.CreateSaturating<char>((char)0x0001));
            Assert.Equal(32767.0m, NumberBaseHelper<decimal>.CreateSaturating<char>((char)0x7FFF));
            Assert.Equal(32768.0m, NumberBaseHelper<decimal>.CreateSaturating<char>((char)0x8000));
            Assert.Equal(65535.0m, NumberBaseHelper<decimal>.CreateSaturating<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromInt16Test()
        {
            Assert.Equal(0.0m, NumberBaseHelper<decimal>.CreateSaturating<short>(0x0000));
            Assert.Equal(1.0m, NumberBaseHelper<decimal>.CreateSaturating<short>(0x0001));
            Assert.Equal(32767.0m, NumberBaseHelper<decimal>.CreateSaturating<short>(0x7FFF));
            Assert.Equal(-32768.0m, NumberBaseHelper<decimal>.CreateSaturating<short>(unchecked((short)0x8000)));
            Assert.Equal(-1.0m, NumberBaseHelper<decimal>.CreateSaturating<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromInt32Test()
        {
            Assert.Equal(0.0m, NumberBaseHelper<decimal>.CreateSaturating<int>(0x00000000));
            Assert.Equal(1.0m, NumberBaseHelper<decimal>.CreateSaturating<int>(0x00000001));
            Assert.Equal(2147483647.0m, NumberBaseHelper<decimal>.CreateSaturating<int>(0x7FFFFFFF));
            Assert.Equal(-2147483648.0m, NumberBaseHelper<decimal>.CreateSaturating<int>(unchecked((int)0x80000000)));
            Assert.Equal(-1.0m, NumberBaseHelper<decimal>.CreateSaturating<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromInt64Test()
        {
            Assert.Equal(0.0m, NumberBaseHelper<decimal>.CreateSaturating<long>(0x0000000000000000));
            Assert.Equal(1.0m, NumberBaseHelper<decimal>.CreateSaturating<long>(0x0000000000000001));
            Assert.Equal(9223372036854775807.0m, NumberBaseHelper<decimal>.CreateSaturating<long>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal(-9223372036854775808.0m, NumberBaseHelper<decimal>.CreateSaturating<long>(unchecked(unchecked((long)0x8000000000000000))));
            Assert.Equal(-1.0m, NumberBaseHelper<decimal>.CreateSaturating<long>(unchecked(unchecked((long)0xFFFFFFFFFFFFFFFF))));
        }

        [Fact]
        public static void CreateSaturatingFromInt128Test()
        {
            Assert.Equal(0.0m, NumberBaseHelper<decimal>.CreateSaturating<Int128>(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000)));

            Assert.Equal(+1.0m, NumberBaseHelper<decimal>.CreateSaturating<Int128>(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001)));
            Assert.Equal(-1.0m, NumberBaseHelper<decimal>.CreateSaturating<Int128>(new Int128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)));

            Assert.Equal(decimal.MaxValue, NumberBaseHelper<decimal>.CreateSaturating<Int128>(new Int128(0x7FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)));
            Assert.Equal(decimal.MinValue, NumberBaseHelper<decimal>.CreateSaturating<Int128>(new Int128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000)));
        }

        [Fact]
        public static void CreateSaturatingFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(0.0m, NumberBaseHelper<decimal>.CreateSaturating<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal(1.0m, NumberBaseHelper<decimal>.CreateSaturating<nint>(unchecked((nint)0x0000000000000001)));
                Assert.Equal(9223372036854775807.0m, NumberBaseHelper<decimal>.CreateSaturating<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(-9223372036854775808.0m, NumberBaseHelper<decimal>.CreateSaturating<nint>(unchecked((nint)0x8000000000000000)));
                Assert.Equal(-1.0m, NumberBaseHelper<decimal>.CreateSaturating<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal(0.0m, NumberBaseHelper<decimal>.CreateSaturating<nint>((nint)0x00000000));
                Assert.Equal(1.0m, NumberBaseHelper<decimal>.CreateSaturating<nint>((nint)0x00000001));
                Assert.Equal(2147483647.0m, NumberBaseHelper<decimal>.CreateSaturating<nint>((nint)0x7FFFFFFF));
                Assert.Equal(-2147483648.0m, NumberBaseHelper<decimal>.CreateSaturating<nint>(unchecked((nint)0x80000000)));
                Assert.Equal(-1.0m, NumberBaseHelper<decimal>.CreateSaturating<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateSaturatingFromSByteTest()
        {
            Assert.Equal(0.0m, NumberBaseHelper<decimal>.CreateSaturating<sbyte>(0x00));
            Assert.Equal(1.0m, NumberBaseHelper<decimal>.CreateSaturating<sbyte>(0x01));
            Assert.Equal(127.0m, NumberBaseHelper<decimal>.CreateSaturating<sbyte>(0x7F));
            Assert.Equal(-128.0m, NumberBaseHelper<decimal>.CreateSaturating<sbyte>(unchecked((sbyte)0x80)));
            Assert.Equal(-1.0m, NumberBaseHelper<decimal>.CreateSaturating<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateSaturatingFromUInt16Test()
        {
            Assert.Equal(0.0m, NumberBaseHelper<decimal>.CreateSaturating<ushort>(0x0000));
            Assert.Equal(1.0m, NumberBaseHelper<decimal>.CreateSaturating<ushort>(0x0001));
            Assert.Equal(32767.0m, NumberBaseHelper<decimal>.CreateSaturating<ushort>(0x7FFF));
            Assert.Equal(32768.0m, NumberBaseHelper<decimal>.CreateSaturating<ushort>(0x8000));
            Assert.Equal(65535.0m, NumberBaseHelper<decimal>.CreateSaturating<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUInt32Test()
        {
            Assert.Equal(0.0m, NumberBaseHelper<decimal>.CreateSaturating<uint>(0x00000000));
            Assert.Equal(1.0m, NumberBaseHelper<decimal>.CreateSaturating<uint>(0x00000001));
            Assert.Equal(2147483647.0m, NumberBaseHelper<decimal>.CreateSaturating<uint>(0x7FFFFFFF));
            Assert.Equal(2147483648.0m, NumberBaseHelper<decimal>.CreateSaturating<uint>(0x80000000));
            Assert.Equal(4294967295.0m, NumberBaseHelper<decimal>.CreateSaturating<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUInt64Test()
        {
            Assert.Equal(0.0m, NumberBaseHelper<decimal>.CreateSaturating<ulong>(0x0000000000000000));
            Assert.Equal(1.0m, NumberBaseHelper<decimal>.CreateSaturating<ulong>(0x0000000000000001));
            Assert.Equal(9223372036854775807.0m, NumberBaseHelper<decimal>.CreateSaturating<ulong>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal(9223372036854775808.0m, NumberBaseHelper<decimal>.CreateSaturating<ulong>(0x8000000000000000));
            Assert.Equal(18446744073709551615.0m, NumberBaseHelper<decimal>.CreateSaturating<ulong>(0xFFFFFFFFFFFFFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUInt128Test()
        {
            Assert.Equal(0.0m, NumberBaseHelper<decimal>.CreateSaturating<UInt128>(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000)));

            Assert.Equal(+1.0m, NumberBaseHelper<decimal>.CreateSaturating<UInt128>(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001)));
            Assert.Equal(decimal.MaxValue, NumberBaseHelper<decimal>.CreateSaturating<UInt128>(new UInt128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)));

            Assert.Equal(decimal.MaxValue, NumberBaseHelper<decimal>.CreateSaturating<UInt128>(new UInt128(0x7FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)));
            Assert.Equal(decimal.MaxValue, NumberBaseHelper<decimal>.CreateSaturating<UInt128>(new UInt128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000)));
        }

        [Fact]
        public static void CreateSaturatingFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(0.0m, NumberBaseHelper<decimal>.CreateSaturating<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal(1.0m, NumberBaseHelper<decimal>.CreateSaturating<nuint>(unchecked((nuint)0x0000000000000001)));
                Assert.Equal(9223372036854775807.0m, NumberBaseHelper<decimal>.CreateSaturating<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(9223372036854775808.0m, NumberBaseHelper<decimal>.CreateSaturating<nuint>(unchecked((nuint)0x8000000000000000)));
                Assert.Equal(18446744073709551615.0m, NumberBaseHelper<decimal>.CreateSaturating<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal(0.0m, NumberBaseHelper<decimal>.CreateSaturating<nuint>((nuint)0x00000000));
                Assert.Equal(1.0m, NumberBaseHelper<decimal>.CreateSaturating<nuint>((nuint)0x00000001));
                Assert.Equal(2147483647.0m, NumberBaseHelper<decimal>.CreateSaturating<nuint>((nuint)0x7FFFFFFF));
                Assert.Equal(2147483648.0m, NumberBaseHelper<decimal>.CreateSaturating<nuint>((nuint)0x80000000));
                Assert.Equal(4294967295.0m, NumberBaseHelper<decimal>.CreateSaturating<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateTruncatingFromByteTest()
        {
            Assert.Equal(0.0m, NumberBaseHelper<decimal>.CreateTruncating<byte>(0x00));
            Assert.Equal(1.0m, NumberBaseHelper<decimal>.CreateTruncating<byte>(0x01));
            Assert.Equal(127.0m, NumberBaseHelper<decimal>.CreateTruncating<byte>(0x7F));
            Assert.Equal(128.0m, NumberBaseHelper<decimal>.CreateTruncating<byte>(0x80));
            Assert.Equal(255.0m, NumberBaseHelper<decimal>.CreateTruncating<byte>(0xFF));
        }

        [Fact]
        public static void CreateTruncatingFromCharTest()
        {
            Assert.Equal(0.0m, NumberBaseHelper<decimal>.CreateTruncating<char>((char)0x0000));
            Assert.Equal(1.0m, NumberBaseHelper<decimal>.CreateTruncating<char>((char)0x0001));
            Assert.Equal(32767.0m, NumberBaseHelper<decimal>.CreateTruncating<char>((char)0x7FFF));
            Assert.Equal(32768.0m, NumberBaseHelper<decimal>.CreateTruncating<char>((char)0x8000));
            Assert.Equal(65535.0m, NumberBaseHelper<decimal>.CreateTruncating<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromInt16Test()
        {
            Assert.Equal(0.0m, NumberBaseHelper<decimal>.CreateTruncating<short>(0x0000));
            Assert.Equal(1.0m, NumberBaseHelper<decimal>.CreateTruncating<short>(0x0001));
            Assert.Equal(32767.0m, NumberBaseHelper<decimal>.CreateTruncating<short>(0x7FFF));
            Assert.Equal(-32768.0m, NumberBaseHelper<decimal>.CreateTruncating<short>(unchecked((short)0x8000)));
            Assert.Equal(-1.0m, NumberBaseHelper<decimal>.CreateTruncating<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateTruncatingFromInt32Test()
        {
            Assert.Equal(0.0m, NumberBaseHelper<decimal>.CreateTruncating<int>(0x00000000));
            Assert.Equal(1.0m, NumberBaseHelper<decimal>.CreateTruncating<int>(0x00000001));
            Assert.Equal(2147483647.0m, NumberBaseHelper<decimal>.CreateTruncating<int>(0x7FFFFFFF));
            Assert.Equal(-2147483648.0m, NumberBaseHelper<decimal>.CreateTruncating<int>(unchecked((int)0x80000000)));
            Assert.Equal(-1.0m, NumberBaseHelper<decimal>.CreateTruncating<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateTruncatingFromInt64Test()
        {
            Assert.Equal(0.0m, NumberBaseHelper<decimal>.CreateTruncating<long>(0x0000000000000000));
            Assert.Equal(1.0m, NumberBaseHelper<decimal>.CreateTruncating<long>(0x0000000000000001));
            Assert.Equal(9223372036854775807.0m, NumberBaseHelper<decimal>.CreateTruncating<long>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal(-9223372036854775808.0m, NumberBaseHelper<decimal>.CreateTruncating<long>(unchecked(unchecked((long)0x8000000000000000))));
            Assert.Equal(-1.0m, NumberBaseHelper<decimal>.CreateTruncating<long>(unchecked(unchecked((long)0xFFFFFFFFFFFFFFFF))));
        }

        [Fact]
        public static void CreateTruncatingFromInt128Test()
        {
            Assert.Equal(0.0m, NumberBaseHelper<decimal>.CreateTruncating<Int128>(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000)));

            Assert.Equal(+1.0m, NumberBaseHelper<decimal>.CreateTruncating<Int128>(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001)));
            Assert.Equal(-1.0m, NumberBaseHelper<decimal>.CreateTruncating<Int128>(new Int128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)));

            Assert.Equal(decimal.MaxValue, NumberBaseHelper<decimal>.CreateTruncating<Int128>(new Int128(0x7FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)));
            Assert.Equal(0.0m, NumberBaseHelper<decimal>.CreateTruncating<Int128>(new Int128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000)));
        }

        [Fact]
        public static void CreateTruncatingFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(0.0m, NumberBaseHelper<decimal>.CreateTruncating<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal(1.0m, NumberBaseHelper<decimal>.CreateTruncating<nint>(unchecked((nint)0x0000000000000001)));
                Assert.Equal(9223372036854775807.0m, NumberBaseHelper<decimal>.CreateTruncating<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(-9223372036854775808.0m, NumberBaseHelper<decimal>.CreateTruncating<nint>(unchecked((nint)0x8000000000000000)));
                Assert.Equal(-1.0m, NumberBaseHelper<decimal>.CreateTruncating<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal(0.0m, NumberBaseHelper<decimal>.CreateTruncating<nint>((nint)0x00000000));
                Assert.Equal(1.0m, NumberBaseHelper<decimal>.CreateTruncating<nint>((nint)0x00000001));
                Assert.Equal(2147483647.0m, NumberBaseHelper<decimal>.CreateTruncating<nint>((nint)0x7FFFFFFF));
                Assert.Equal(-2147483648.0m, NumberBaseHelper<decimal>.CreateTruncating<nint>(unchecked((nint)0x80000000)));
                Assert.Equal(-1.0m, NumberBaseHelper<decimal>.CreateTruncating<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateTruncatingFromSByteTest()
        {
            Assert.Equal(0.0m, NumberBaseHelper<decimal>.CreateTruncating<sbyte>(0x00));
            Assert.Equal(1.0m, NumberBaseHelper<decimal>.CreateTruncating<sbyte>(0x01));
            Assert.Equal(127.0m, NumberBaseHelper<decimal>.CreateTruncating<sbyte>(0x7F));
            Assert.Equal(-128.0m, NumberBaseHelper<decimal>.CreateTruncating<sbyte>(unchecked((sbyte)0x80)));
            Assert.Equal(-1.0m, NumberBaseHelper<decimal>.CreateTruncating<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateTruncatingFromUInt16Test()
        {
            Assert.Equal(0.0m, NumberBaseHelper<decimal>.CreateTruncating<ushort>(0x0000));
            Assert.Equal(1.0m, NumberBaseHelper<decimal>.CreateTruncating<ushort>(0x0001));
            Assert.Equal(32767.0m, NumberBaseHelper<decimal>.CreateTruncating<ushort>(0x7FFF));
            Assert.Equal(32768.0m, NumberBaseHelper<decimal>.CreateTruncating<ushort>(0x8000));
            Assert.Equal(65535.0m, NumberBaseHelper<decimal>.CreateTruncating<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt32Test()
        {
            Assert.Equal(0.0m, NumberBaseHelper<decimal>.CreateTruncating<uint>(0x00000000));
            Assert.Equal(1.0m, NumberBaseHelper<decimal>.CreateTruncating<uint>(0x00000001));
            Assert.Equal(2147483647.0m, NumberBaseHelper<decimal>.CreateTruncating<uint>(0x7FFFFFFF));
            Assert.Equal(2147483648.0m, NumberBaseHelper<decimal>.CreateTruncating<uint>(0x80000000));
            Assert.Equal(4294967295.0m, NumberBaseHelper<decimal>.CreateTruncating<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt64Test()
        {
            Assert.Equal(0.0m, NumberBaseHelper<decimal>.CreateTruncating<ulong>(0x0000000000000000));
            Assert.Equal(1.0m, NumberBaseHelper<decimal>.CreateTruncating<ulong>(0x0000000000000001));
            Assert.Equal(9223372036854775807.0m, NumberBaseHelper<decimal>.CreateTruncating<ulong>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal(9223372036854775808.0m, NumberBaseHelper<decimal>.CreateTruncating<ulong>(0x8000000000000000));
            Assert.Equal(18446744073709551615.0m, NumberBaseHelper<decimal>.CreateTruncating<ulong>(0xFFFFFFFFFFFFFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt128Test()
        {
            Assert.Equal(0.0m, NumberBaseHelper<decimal>.CreateTruncating<UInt128>(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000)));

            Assert.Equal(+1.0m, NumberBaseHelper<decimal>.CreateTruncating<UInt128>(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001)));
            Assert.Equal(decimal.MaxValue, NumberBaseHelper<decimal>.CreateTruncating<UInt128>(new UInt128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)));

            Assert.Equal(decimal.MaxValue, NumberBaseHelper<decimal>.CreateTruncating<UInt128>(new UInt128(0x7FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)));
            Assert.Equal(0.0m, NumberBaseHelper<decimal>.CreateTruncating<UInt128>(new UInt128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000)));
        }

        [Fact]
        public static void CreateTruncatingFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(0.0m, NumberBaseHelper<decimal>.CreateTruncating<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal(1.0m, NumberBaseHelper<decimal>.CreateTruncating<nuint>(unchecked((nuint)0x0000000000000001)));
                Assert.Equal(9223372036854775807.0m, NumberBaseHelper<decimal>.CreateTruncating<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(9223372036854775808.0m, NumberBaseHelper<decimal>.CreateTruncating<nuint>(unchecked((nuint)0x8000000000000000)));
                Assert.Equal(18446744073709551615.0m, NumberBaseHelper<decimal>.CreateTruncating<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal(0.0m, NumberBaseHelper<decimal>.CreateTruncating<nuint>((nuint)0x00000000));
                Assert.Equal(1.0m, NumberBaseHelper<decimal>.CreateTruncating<nuint>((nuint)0x00000001));
                Assert.Equal(2147483647.0m, NumberBaseHelper<decimal>.CreateTruncating<nuint>((nuint)0x7FFFFFFF));
                Assert.Equal(2147483648.0m, NumberBaseHelper<decimal>.CreateTruncating<nuint>((nuint)0x80000000));
                Assert.Equal(4294967295.0m, NumberBaseHelper<decimal>.CreateTruncating<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void IsFiniteTest()
        {
            Assert.True(NumberBaseHelper<decimal>.IsFinite(decimal.MinValue));
            Assert.True(NumberBaseHelper<decimal>.IsFinite(-1.0m));
            Assert.True(NumberBaseHelper<decimal>.IsFinite(-0.0m));
            Assert.True(NumberBaseHelper<decimal>.IsFinite(0.0m));
            Assert.True(NumberBaseHelper<decimal>.IsFinite(1.0m));
            Assert.True(NumberBaseHelper<decimal>.IsFinite(decimal.MaxValue));
        }

        [Fact]
        public static void IsInfinityTest()
        {
            Assert.False(NumberBaseHelper<decimal>.IsInfinity(decimal.MinValue));
            Assert.False(NumberBaseHelper<decimal>.IsInfinity(-1.0m));
            Assert.False(NumberBaseHelper<decimal>.IsInfinity(-0.0m));
            Assert.False(NumberBaseHelper<decimal>.IsInfinity(0.0m));
            Assert.False(NumberBaseHelper<decimal>.IsInfinity(1.0m));
            Assert.False(NumberBaseHelper<decimal>.IsInfinity(decimal.MaxValue));
        }

        [Fact]
        public static void IsNaNTest()
        {
            Assert.False(NumberBaseHelper<decimal>.IsNaN(decimal.MinValue));
            Assert.False(NumberBaseHelper<decimal>.IsNaN(-1.0m));
            Assert.False(NumberBaseHelper<decimal>.IsNaN(-0.0m));
            Assert.False(NumberBaseHelper<decimal>.IsNaN(0.0m));
            Assert.False(NumberBaseHelper<decimal>.IsNaN(1.0m));
            Assert.False(NumberBaseHelper<decimal>.IsNaN(decimal.MaxValue));
        }

        [Fact]
        public static void IsNegativeTest()
        {
            Assert.True(NumberBaseHelper<decimal>.IsNegative(decimal.MinValue));
            Assert.True(NumberBaseHelper<decimal>.IsNegative(-1.0m));
            Assert.True(NumberBaseHelper<decimal>.IsNegative(-0.0m));
            Assert.False(NumberBaseHelper<decimal>.IsNegative(0.0m));
            Assert.False(NumberBaseHelper<decimal>.IsNegative(1.0m));
            Assert.False(NumberBaseHelper<decimal>.IsNegative(decimal.MaxValue));
        }

        [Fact]
        public static void IsNegativeInfinityTest()
        {
            Assert.False(NumberBaseHelper<decimal>.IsNegativeInfinity(decimal.MinValue));
            Assert.False(NumberBaseHelper<decimal>.IsNegativeInfinity(-1.0m));
            Assert.False(NumberBaseHelper<decimal>.IsNegativeInfinity(-0.0m));
            Assert.False(NumberBaseHelper<decimal>.IsNegativeInfinity(0.0m));
            Assert.False(NumberBaseHelper<decimal>.IsNegativeInfinity(1.0m));
            Assert.False(NumberBaseHelper<decimal>.IsNegativeInfinity(decimal.MaxValue));
        }

        [Fact]
        public static void IsNormalTest()
        {
            Assert.True(NumberBaseHelper<decimal>.IsNormal(decimal.MinValue));
            Assert.True(NumberBaseHelper<decimal>.IsNormal(-1.0m));
            Assert.False(NumberBaseHelper<decimal>.IsNormal(-0.0m));
            Assert.False(NumberBaseHelper<decimal>.IsNormal(0.0m));
            Assert.True(NumberBaseHelper<decimal>.IsNormal(1.0m));
            Assert.True(NumberBaseHelper<decimal>.IsNormal(decimal.MaxValue));
        }

        [Fact]
        public static void IsPositiveInfinityTest()
        {
            Assert.False(NumberBaseHelper<decimal>.IsPositiveInfinity(decimal.MinValue));
            Assert.False(NumberBaseHelper<decimal>.IsPositiveInfinity(-1.0m));
            Assert.False(NumberBaseHelper<decimal>.IsPositiveInfinity(-0.0m));
            Assert.False(NumberBaseHelper<decimal>.IsPositiveInfinity(0.0m));
            Assert.False(NumberBaseHelper<decimal>.IsPositiveInfinity(1.0m));
            Assert.False(NumberBaseHelper<decimal>.IsPositiveInfinity(decimal.MaxValue));
        }

        [Fact]
        public static void IsSubnormalTest()
        {
            Assert.False(NumberBaseHelper<decimal>.IsSubnormal(decimal.MinValue));
            Assert.False(NumberBaseHelper<decimal>.IsSubnormal(-1.0m));
            Assert.False(NumberBaseHelper<decimal>.IsSubnormal(-0.0m));
            Assert.False(NumberBaseHelper<decimal>.IsSubnormal(0.0m));
            Assert.False(NumberBaseHelper<decimal>.IsSubnormal(1.0m));
            Assert.False(NumberBaseHelper<decimal>.IsSubnormal(decimal.MaxValue));
        }

        [Fact]
        public static void MaxMagnitudeTest()
        {
            Assert.Equal(decimal.MinValue, NumberBaseHelper<decimal>.MaxMagnitude(decimal.MinValue, 1.0m));
            Assert.Equal(1.0m, NumberBaseHelper<decimal>.MaxMagnitude(-1.0m, 1.0m));
            Assert.Equal(1.0m, NumberBaseHelper<decimal>.MaxMagnitude(-0.0m, 1.0m));
            Assert.Equal(1.0m, NumberBaseHelper<decimal>.MaxMagnitude(0.0m, 1.0m));
            Assert.Equal(1.0m, NumberBaseHelper<decimal>.MaxMagnitude(1.0m, 1.0m));
            Assert.Equal(decimal.MaxValue, NumberBaseHelper<decimal>.MaxMagnitude(decimal.MaxValue, 1.0m));
        }

        [Fact]
        public static void MaxMagnitudeNumberTest()
        {
            Assert.Equal(decimal.MinValue, NumberBaseHelper<decimal>.MaxMagnitudeNumber(decimal.MinValue, 1.0m));
            Assert.Equal(1.0m, NumberBaseHelper<decimal>.MaxMagnitudeNumber(-1.0m, 1.0m));
            Assert.Equal(1.0m, NumberBaseHelper<decimal>.MaxMagnitudeNumber(-0.0m, 1.0m));
            Assert.Equal(1.0m, NumberBaseHelper<decimal>.MaxMagnitudeNumber(0.0m, 1.0m));
            Assert.Equal(1.0m, NumberBaseHelper<decimal>.MaxMagnitudeNumber(1.0m, 1.0m));
            Assert.Equal(decimal.MaxValue, NumberBaseHelper<decimal>.MaxMagnitudeNumber(decimal.MaxValue, 1.0m));
        }

        [Fact]
        public static void MinMagnitudeTest()
        {
            Assert.Equal(1.0m, NumberBaseHelper<decimal>.MinMagnitude(decimal.MinValue, 1.0m));
            Assert.Equal(-1.0m, NumberBaseHelper<decimal>.MinMagnitude(-1.0m, 1.0m));
            Assert.Equal(-0.0m, NumberBaseHelper<decimal>.MinMagnitude(-0.0m, 1.0m));
            Assert.Equal(0.0m, NumberBaseHelper<decimal>.MinMagnitude(0.0m, 1.0m));
            Assert.Equal(1.0m, NumberBaseHelper<decimal>.MinMagnitude(1.0m, 1.0m));
            Assert.Equal(1.0m, NumberBaseHelper<decimal>.MinMagnitude(decimal.MaxValue, 1.0m));
        }

        [Fact]
        public static void MinMagnitudeNumberTest()
        {
            Assert.Equal(1.0m, NumberBaseHelper<decimal>.MinMagnitudeNumber(decimal.MinValue, 1.0m));
            Assert.Equal(-1.0m, NumberBaseHelper<decimal>.MinMagnitudeNumber(-1.0m, 1.0m));
            Assert.Equal(-0.0m, NumberBaseHelper<decimal>.MinMagnitudeNumber(-0.0m, 1.0m));
            Assert.Equal(0.0m, NumberBaseHelper<decimal>.MinMagnitudeNumber(0.0m, 1.0m));
            Assert.Equal(1.0m, NumberBaseHelper<decimal>.MinMagnitudeNumber(1.0m, 1.0m));
            Assert.Equal(1.0m, NumberBaseHelper<decimal>.MinMagnitudeNumber(decimal.MaxValue, 1.0m));
        }

        [Fact]
        public static void TryCreateFromByteTest()
        {
            decimal result;

            Assert.True(NumberBaseHelper<decimal>.TryCreate<byte>(0x00, out result));
            Assert.Equal(0.0m, result);

            Assert.True(NumberBaseHelper<decimal>.TryCreate<byte>(0x01, out result));
            Assert.Equal(1.0m, result);

            Assert.True(NumberBaseHelper<decimal>.TryCreate<byte>(0x7F, out result));
            Assert.Equal(127.0m, result);

            Assert.True(NumberBaseHelper<decimal>.TryCreate<byte>(0x80, out result));
            Assert.Equal(128.0m, result);

            Assert.True(NumberBaseHelper<decimal>.TryCreate<byte>(0xFF, out result));
            Assert.Equal(255.0m, result);
        }

        [Fact]
        public static void TryCreateFromCharTest()
        {
            decimal result;

            Assert.True(NumberBaseHelper<decimal>.TryCreate<char>((char)0x0000, out result));
            Assert.Equal(0.0m, result);

            Assert.True(NumberBaseHelper<decimal>.TryCreate<char>((char)0x0001, out result));
            Assert.Equal(1.0m, result);

            Assert.True(NumberBaseHelper<decimal>.TryCreate<char>((char)0x7FFF, out result));
            Assert.Equal(32767.0m, result);

            Assert.True(NumberBaseHelper<decimal>.TryCreate<char>((char)0x8000, out result));
            Assert.Equal(32768.0m, result);

            Assert.True(NumberBaseHelper<decimal>.TryCreate<char>((char)0xFFFF, out result));
            Assert.Equal(65535.0m, result);
        }

        [Fact]
        public static void TryCreateFromInt16Test()
        {
            decimal result;

            Assert.True(NumberBaseHelper<decimal>.TryCreate<short>(0x0000, out result));
            Assert.Equal(0.0m, result);

            Assert.True(NumberBaseHelper<decimal>.TryCreate<short>(0x0001, out result));
            Assert.Equal(1.0m, result);

            Assert.True(NumberBaseHelper<decimal>.TryCreate<short>(0x7FFF, out result));
            Assert.Equal(32767.0m, result);

            Assert.True(NumberBaseHelper<decimal>.TryCreate<short>(unchecked((short)0x8000), out result));
            Assert.Equal(-32768.0m, result);

            Assert.True(NumberBaseHelper<decimal>.TryCreate<short>(unchecked((short)0xFFFF), out result));
            Assert.Equal(-1.0m, result);
        }

        [Fact]
        public static void TryCreateFromInt32Test()
        {
            decimal result;

            Assert.True(NumberBaseHelper<decimal>.TryCreate<int>(0x00000000, out result));
            Assert.Equal(0.0m, result);

            Assert.True(NumberBaseHelper<decimal>.TryCreate<int>(0x00000001, out result));
            Assert.Equal(1.0m, result);

            Assert.True(NumberBaseHelper<decimal>.TryCreate<int>(0x7FFFFFFF, out result));
            Assert.Equal(2147483647.0m, result);

            Assert.True(NumberBaseHelper<decimal>.TryCreate<int>(unchecked((int)0x80000000), out result));
            Assert.Equal(-2147483648.0m, result);

            Assert.True(NumberBaseHelper<decimal>.TryCreate<int>(unchecked((int)0xFFFFFFFF), out result));
            Assert.Equal(-1.0m, result);
        }

        [Fact]
        public static void TryCreateFromInt64Test()
        {
            decimal result;

            Assert.True(NumberBaseHelper<decimal>.TryCreate<long>(0x0000000000000000, out result));
            Assert.Equal(0.0m, result);

            Assert.True(NumberBaseHelper<decimal>.TryCreate<long>(0x0000000000000001, out result));
            Assert.Equal(1.0m, result);

            Assert.True(NumberBaseHelper<decimal>.TryCreate<long>(0x7FFFFFFFFFFFFFFF, out result));
            Assert.Equal(9223372036854775807.0m, result);

            Assert.True(NumberBaseHelper<decimal>.TryCreate<long>(unchecked(unchecked((long)0x8000000000000000)), out result));
            Assert.Equal(-9223372036854775808.0m, result);

            Assert.True(NumberBaseHelper<decimal>.TryCreate<long>(unchecked(unchecked((long)0xFFFFFFFFFFFFFFFF)), out result));
            Assert.Equal(-1.0m, result);
        }

        [Fact]
        public static void TryCreateFromInt128Test()
        {
            decimal result;

            Assert.True(NumberBaseHelper<decimal>.TryCreate<Int128>(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), out result));
            Assert.Equal(0.0m, result);

            Assert.True(NumberBaseHelper<decimal>.TryCreate<Int128>(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), out result));
            Assert.Equal(1.0m, result);                         

            Assert.True(NumberBaseHelper<decimal>.TryCreate<Int128>(new Int128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), out result));
            Assert.Equal(-1.0m, result);

            Assert.False(NumberBaseHelper<decimal>.TryCreate<Int128>(new Int128(0x7FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), out result));
            Assert.Equal(0.0m, result);

            Assert.False(NumberBaseHelper<decimal>.TryCreate<Int128>(new Int128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000), out result));
            Assert.Equal(0.0m, result);
        }

        [Fact]
        public static void TryCreateFromIntPtrTest()
        {
            decimal result;

            if (Environment.Is64BitProcess)
            {
                Assert.True(NumberBaseHelper<decimal>.TryCreate<nint>(unchecked((nint)0x0000000000000000), out result));
                Assert.Equal(0.0m, result);

                Assert.True(NumberBaseHelper<decimal>.TryCreate<nint>(unchecked((nint)0x0000000000000001), out result));
                Assert.Equal(1.0m, result);

                Assert.True(NumberBaseHelper<decimal>.TryCreate<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF), out result));
                Assert.Equal(9223372036854775807.0m, result);

                Assert.True(NumberBaseHelper<decimal>.TryCreate<nint>(unchecked((nint)0x8000000000000000), out result));
                Assert.Equal(-9223372036854775808.0m, result);

                Assert.True(NumberBaseHelper<decimal>.TryCreate<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF), out result));
                Assert.Equal(-1.0m, result);
            }
            else
            {
                Assert.True(NumberBaseHelper<decimal>.TryCreate<nint>((nint)0x00000000, out result));
                Assert.Equal(0.0m, result);

                Assert.True(NumberBaseHelper<decimal>.TryCreate<nint>((nint)0x00000001, out result));
                Assert.Equal(1.0m, result);

                Assert.True(NumberBaseHelper<decimal>.TryCreate<nint>((nint)0x7FFFFFFF, out result));
                Assert.Equal(2147483647.0m, result);

                Assert.True(NumberBaseHelper<decimal>.TryCreate<nint>(unchecked((nint)0x80000000), out result));
                Assert.Equal(-2147483648.0m, result);

                Assert.True(NumberBaseHelper<decimal>.TryCreate<nint>(unchecked((nint)0xFFFFFFFF), out result));
                Assert.Equal(-1.0m, result);
            }
        }

        [Fact]
        public static void TryCreateFromSByteTest()
        {
            decimal result;

            Assert.True(NumberBaseHelper<decimal>.TryCreate<sbyte>(0x00, out result));
            Assert.Equal(0.0m, result);

            Assert.True(NumberBaseHelper<decimal>.TryCreate<sbyte>(0x01, out result));
            Assert.Equal(1.0m, result);

            Assert.True(NumberBaseHelper<decimal>.TryCreate<sbyte>(0x7F, out result));
            Assert.Equal(127.0m, result);

            Assert.True(NumberBaseHelper<decimal>.TryCreate<sbyte>(unchecked((sbyte)0x80), out result));
            Assert.Equal(-128.0m, result);

            Assert.True(NumberBaseHelper<decimal>.TryCreate<sbyte>(unchecked((sbyte)0xFF), out result));
            Assert.Equal(-1.0m, result);
        }

        [Fact]
        public static void TryCreateFromUInt16Test()
        {
            decimal result;

            Assert.True(NumberBaseHelper<decimal>.TryCreate<ushort>(0x0000, out result));
            Assert.Equal(0.0m, result);

            Assert.True(NumberBaseHelper<decimal>.TryCreate<ushort>(0x0001, out result));
            Assert.Equal(1.0m, result);

            Assert.True(NumberBaseHelper<decimal>.TryCreate<ushort>(0x7FFF, out result));
            Assert.Equal(32767.0m, result);

            Assert.True(NumberBaseHelper<decimal>.TryCreate<ushort>(0x8000, out result));
            Assert.Equal(32768.0m, result);

            Assert.True(NumberBaseHelper<decimal>.TryCreate<ushort>(0xFFFF, out result));
            Assert.Equal(65535.0m, result);
        }

        [Fact]
        public static void TryCreateFromUInt32Test()
        {
            decimal result;

            Assert.True(NumberBaseHelper<decimal>.TryCreate<uint>(0x00000000, out result));
            Assert.Equal(0.0m, result);

            Assert.True(NumberBaseHelper<decimal>.TryCreate<uint>(0x00000001, out result));
            Assert.Equal(1.0m, result);

            Assert.True(NumberBaseHelper<decimal>.TryCreate<uint>(0x7FFFFFFF, out result));
            Assert.Equal(2147483647.0m, result);

            Assert.True(NumberBaseHelper<decimal>.TryCreate<uint>(0x80000000, out result));
            Assert.Equal(2147483648.0m, result);

            Assert.True(NumberBaseHelper<decimal>.TryCreate<uint>(0xFFFFFFFF, out result));
            Assert.Equal(4294967295.0m, result);
        }

        [Fact]
        public static void TryCreateFromUInt64Test()
        {
            decimal result;

            Assert.True(NumberBaseHelper<decimal>.TryCreate<ulong>(0x0000000000000000, out result));
            Assert.Equal(0.0m, result);

            Assert.True(NumberBaseHelper<decimal>.TryCreate<ulong>(0x0000000000000001, out result));
            Assert.Equal(1.0m, result);

            Assert.True(NumberBaseHelper<decimal>.TryCreate<ulong>(0x7FFFFFFFFFFFFFFF, out result));
            Assert.Equal(9223372036854775807.0m, result);

            Assert.True(NumberBaseHelper<decimal>.TryCreate<ulong>(0x8000000000000000, out result));
            Assert.Equal(9223372036854775808.0m, result);

            Assert.True(NumberBaseHelper<decimal>.TryCreate<ulong>(0xFFFFFFFFFFFFFFFF, out result));
            Assert.Equal(18446744073709551615.0m, result);
        }

        [Fact]
        public static void TryCreateFromUInt128Test()
        {
            decimal result;

            Assert.True(NumberBaseHelper<decimal>.TryCreate<UInt128>(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), out result));
            Assert.Equal(0.0m, result);

            Assert.True(NumberBaseHelper<decimal>.TryCreate<UInt128>(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), out result));
            Assert.Equal(1.0m, result);

            Assert.False(NumberBaseHelper<decimal>.TryCreate<UInt128>(new UInt128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), out result));
            Assert.Equal(0.0m, result);

            Assert.False(NumberBaseHelper<decimal>.TryCreate<UInt128>(new UInt128(0x7FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), out result));
            Assert.Equal(0.0m, result);

            Assert.False(NumberBaseHelper<decimal>.TryCreate<UInt128>(new UInt128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000), out result));
            Assert.Equal(0.0m, result);
        }

        [Fact]
        public static void TryCreateFromUIntPtrTest()
        {
            decimal result;

            if (Environment.Is64BitProcess)
            {
                Assert.True(NumberBaseHelper<decimal>.TryCreate<nuint>(unchecked((nuint)0x0000000000000000), out result));
                Assert.Equal(0.0m, result);

                Assert.True(NumberBaseHelper<decimal>.TryCreate<nuint>(unchecked((nuint)0x0000000000000001), out result));
                Assert.Equal(1.0m, result);

                Assert.True(NumberBaseHelper<decimal>.TryCreate<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF), out result));
                Assert.Equal(9223372036854775807.0m, result);

                Assert.True(NumberBaseHelper<decimal>.TryCreate<nuint>(unchecked((nuint)0x8000000000000000), out result));
                Assert.Equal(9223372036854775808.0m, result);

                Assert.True(NumberBaseHelper<decimal>.TryCreate<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF), out result));
                Assert.Equal(18446744073709551615.0m, result);
            }
            else
            {
                Assert.True(NumberBaseHelper<decimal>.TryCreate<nuint>((nuint)0x00000000, out result));
                Assert.Equal(0.0m, result);

                Assert.True(NumberBaseHelper<decimal>.TryCreate<nuint>((nuint)0x00000001, out result));
                Assert.Equal(1.0m, result);

                Assert.True(NumberBaseHelper<decimal>.TryCreate<nuint>((nuint)0x7FFFFFFF, out result));
                Assert.Equal(2147483647.0m, result);

                Assert.True(NumberBaseHelper<decimal>.TryCreate<nuint>(unchecked((nuint)0x80000000), out result));
                Assert.Equal(2147483648.0m, result);

                Assert.True(NumberBaseHelper<decimal>.TryCreate<nuint>(unchecked((nuint)0xFFFFFFFF), out result));
                Assert.Equal(4294967295.0m, result);
            }
        }

        //
        // ISignedNumber
        //

        [Fact]
        public static void NegativeOneTest()
        {
            Assert.Equal(-1.0m, SignedNumberHelper<decimal>.NegativeOne);
        }

        //
        // ISubtractionOperators
        //

        [Fact]
        public static void op_SubtractionTest()
        {
            Assert.Equal(-2.0m, SubtractionOperatorsHelper<decimal, decimal, decimal>.op_Subtraction(-1.0m, 1.0m));
            Assert.Equal(-1.0m, SubtractionOperatorsHelper<decimal, decimal, decimal>.op_Subtraction(-0.0m, 1.0m));
            Assert.Equal(-1.0m, SubtractionOperatorsHelper<decimal, decimal, decimal>.op_Subtraction(0.0m, 1.0m));
            Assert.Equal(0.0m, SubtractionOperatorsHelper<decimal, decimal, decimal>.op_Subtraction(1.0m, 1.0m));
            Assert.Equal(79228162514264337593543950334.0m, SubtractionOperatorsHelper<decimal, decimal, decimal>.op_Subtraction(decimal.MaxValue, 1.0m));

            Assert.Throws<OverflowException>(() => SubtractionOperatorsHelper<decimal, decimal, decimal>.op_Subtraction(decimal.MinValue, 1.0m));
        }

        [Fact]
        public static void op_CheckedSubtractionTest()
        {
            Assert.Equal(-2.0m, SubtractionOperatorsHelper<decimal, decimal, decimal>.op_CheckedSubtraction(-1.0m, 1.0m));
            Assert.Equal(-1.0m, SubtractionOperatorsHelper<decimal, decimal, decimal>.op_CheckedSubtraction(-0.0m, 1.0m));
            Assert.Equal(-1.0m, SubtractionOperatorsHelper<decimal, decimal, decimal>.op_CheckedSubtraction(0.0m, 1.0m));
            Assert.Equal(0.0m, SubtractionOperatorsHelper<decimal, decimal, decimal>.op_CheckedSubtraction(1.0m, 1.0m));
            Assert.Equal(79228162514264337593543950334.0m, SubtractionOperatorsHelper<decimal, decimal, decimal>.op_CheckedSubtraction(decimal.MaxValue, 1.0m));

            Assert.Throws<OverflowException>(() => SubtractionOperatorsHelper<decimal, decimal, decimal>.op_CheckedSubtraction(decimal.MinValue, 1.0m));
        }

        //
        // IUnaryNegationOperators
        //

        [Fact]
        public static void op_UnaryNegationTest()
        {
            Assert.Equal(decimal.MaxValue, UnaryNegationOperatorsHelper<decimal, decimal>.op_UnaryNegation(decimal.MinValue));
            Assert.Equal(1.0m, UnaryNegationOperatorsHelper<decimal, decimal>.op_UnaryNegation(-1.0m));
            Assert.Equal(0.0m, UnaryNegationOperatorsHelper<decimal, decimal>.op_UnaryNegation(-0.0m));
            Assert.Equal(-0.0m, UnaryNegationOperatorsHelper<decimal, decimal>.op_UnaryNegation(0.0m));
            Assert.Equal(-1.0m, UnaryNegationOperatorsHelper<decimal, decimal>.op_UnaryNegation(1.0m));
            Assert.Equal(decimal.MinValue, UnaryNegationOperatorsHelper<decimal, decimal>.op_UnaryNegation(decimal.MaxValue));
        }

        [Fact]
        public static void op_CheckedUnaryNegationTest()
        {
            Assert.Equal(decimal.MaxValue, UnaryNegationOperatorsHelper<decimal, decimal>.op_CheckedUnaryNegation(decimal.MinValue));
            Assert.Equal(1.0m, UnaryNegationOperatorsHelper<decimal, decimal>.op_CheckedUnaryNegation(-1.0m));
            Assert.Equal(0.0m, UnaryNegationOperatorsHelper<decimal, decimal>.op_CheckedUnaryNegation(-0.0m));
            Assert.Equal(-0.0m, UnaryNegationOperatorsHelper<decimal, decimal>.op_CheckedUnaryNegation(0.0m));
            Assert.Equal(-1.0m, UnaryNegationOperatorsHelper<decimal, decimal>.op_CheckedUnaryNegation(1.0m));
            Assert.Equal(decimal.MinValue, UnaryNegationOperatorsHelper<decimal, decimal>.op_CheckedUnaryNegation(decimal.MaxValue));
        }

        //
        // IUnaryPlusOperators
        //

        [Fact]
        public static void op_UnaryPlusTest()
        {
            Assert.Equal(decimal.MinValue, UnaryPlusOperatorsHelper<decimal, decimal>.op_UnaryPlus(decimal.MinValue));
            Assert.Equal(-1.0m, UnaryPlusOperatorsHelper<decimal, decimal>.op_UnaryPlus(-1.0m));
            Assert.Equal(-0.0m, UnaryPlusOperatorsHelper<decimal, decimal>.op_UnaryPlus(-0.0m));
            Assert.Equal(0.0m, UnaryPlusOperatorsHelper<decimal, decimal>.op_UnaryPlus(0.0m));
            Assert.Equal(1.0m, UnaryPlusOperatorsHelper<decimal, decimal>.op_UnaryPlus(1.0m));
            Assert.Equal(decimal.MaxValue, UnaryPlusOperatorsHelper<decimal, decimal>.op_UnaryPlus(decimal.MaxValue));
        }

        //
        // IParsable and ISpanParsable
        //

        [Theory]
        [MemberData(nameof(DecimalTests.Parse_Valid_TestData), MemberType = typeof(DecimalTests))]
        public static void ParseValidStringTest(string value, NumberStyles style, IFormatProvider provider, decimal expected)
        {
            bool isDefaultProvider = provider == null || provider == NumberFormatInfo.CurrentInfo;
            decimal result;
            if ((style & ~(NumberStyles.Float | NumberStyles.AllowThousands)) == 0 && style != NumberStyles.None)
            {
                // Use Parse(string) or Parse(string, IFormatProvider)
                if (isDefaultProvider)
                {
                    Assert.True(ParsableHelper<decimal>.TryParse(value, null, out result));
                    Assert.Equal(expected, result);

                    Assert.Equal(expected, ParsableHelper<decimal>.Parse(value, null));
                }

                Assert.Equal(expected, ParsableHelper<decimal>.Parse(value, provider));
            }

            // Use Parse(string, NumberStyles, IFormatProvider)
            Assert.True(NumberBaseHelper<decimal>.TryParse(value, style, provider, out result));
            Assert.Equal(expected, result);

            Assert.Equal(expected, NumberBaseHelper<decimal>.Parse(value, style, provider));

            if (isDefaultProvider)
            {
                // Use Parse(string, NumberStyles) or Parse(string, NumberStyles, IFormatProvider)
                Assert.True(NumberBaseHelper<decimal>.TryParse(value, style, NumberFormatInfo.CurrentInfo, out result));
                Assert.Equal(expected, result);

                Assert.Equal(expected, NumberBaseHelper<decimal>.Parse(value, style, null));
                Assert.Equal(expected, NumberBaseHelper<decimal>.Parse(value, style, NumberFormatInfo.CurrentInfo));
            }
        }

        [Theory]
        [MemberData(nameof(DecimalTests.Parse_Invalid_TestData), MemberType = typeof(DecimalTests))]
        public static void ParseInvalidStringTest(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            bool isDefaultProvider = provider == null || provider == NumberFormatInfo.CurrentInfo;
            decimal result;
            if ((style & ~(NumberStyles.Float | NumberStyles.AllowThousands)) == 0 && style != NumberStyles.None && (style & NumberStyles.AllowLeadingWhite) == (style & NumberStyles.AllowTrailingWhite))
            {
                // Use Parse(string) or Parse(string, IFormatProvider)
                if (isDefaultProvider)
                {
                    Assert.False(ParsableHelper<decimal>.TryParse(value, null, out result));
                    Assert.Equal(default(decimal), result);

                    Assert.Throws(exceptionType, () => ParsableHelper<decimal>.Parse(value, null));
                }

                Assert.Throws(exceptionType, () => ParsableHelper<decimal>.Parse(value, provider));
            }

            // Use Parse(string, NumberStyles, IFormatProvider)
            Assert.False(NumberBaseHelper<decimal>.TryParse(value, style, provider, out result));
            Assert.Equal(default(decimal), result);

            Assert.Throws(exceptionType, () => NumberBaseHelper<decimal>.Parse(value, style, provider));

            if (isDefaultProvider)
            {
                // Use Parse(string, NumberStyles) or Parse(string, NumberStyles, IFormatProvider)
                Assert.False(NumberBaseHelper<decimal>.TryParse(value, style, NumberFormatInfo.CurrentInfo, out result));
                Assert.Equal(default(decimal), result);

                Assert.Throws(exceptionType, () => NumberBaseHelper<decimal>.Parse(value, style, null));
                Assert.Throws(exceptionType, () => NumberBaseHelper<decimal>.Parse(value, style, NumberFormatInfo.CurrentInfo));
            }
        }

        [Theory]
        [MemberData(nameof(DecimalTests.Parse_ValidWithOffsetCount_TestData), MemberType = typeof(DecimalTests))]
        public static void ParseValidSpanTest(string value, int offset, int count, NumberStyles style, IFormatProvider provider, decimal expected)
        {
            bool isDefaultProvider = provider == null || provider == NumberFormatInfo.CurrentInfo;
            decimal result;
            if ((style & ~(NumberStyles.Float | NumberStyles.AllowThousands)) == 0 && style != NumberStyles.None)
            {
                // Use Parse(string) or Parse(string, IFormatProvider)
                if (isDefaultProvider)
                {
                    Assert.True(SpanParsableHelper<decimal>.TryParse(value.AsSpan(offset, count), null, out result));
                    Assert.Equal(expected, result);

                    Assert.Equal(expected, SpanParsableHelper<decimal>.Parse(value.AsSpan(offset, count), null));
                }

                Assert.Equal(expected, SpanParsableHelper<decimal>.Parse(value.AsSpan(offset, count), provider: provider));
            }

            Assert.Equal(expected, NumberBaseHelper<decimal>.Parse(value.AsSpan(offset, count), style, provider));

            Assert.True(NumberBaseHelper<decimal>.TryParse(value.AsSpan(offset, count), style, provider, out result));
            Assert.Equal(expected, result);
        }

        [Theory]
        [MemberData(nameof(DecimalTests.Parse_Invalid_TestData), MemberType = typeof(DecimalTests))]
        public static void ParseInvalidSpanTest(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            if (value != null)
            {
                Assert.Throws(exceptionType, () => NumberBaseHelper<decimal>.Parse(value.AsSpan(), style, provider));

                Assert.False(NumberBaseHelper<decimal>.TryParse(value.AsSpan(), style, provider, out decimal result));
                Assert.Equal(0, result);
            }
        }
    }
}
