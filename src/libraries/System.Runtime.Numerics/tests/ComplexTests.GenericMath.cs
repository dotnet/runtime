// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Numerics.Tests
{
    public class ComplexTests_GenericMath
    {
        private static Complex MinNormal => 2.2250738585072014E-308;

        private static Complex MaxSubnormal => 2.2250738585072009E-308;

        private static void AssertBitwiseEqual(double expected, double actual)
        {
            ulong expectedBits = BitConverter.DoubleToUInt64Bits(expected);
            ulong actualBits = BitConverter.DoubleToUInt64Bits(actual);

            if (expectedBits == actualBits)
            {
                return;
            }

            if (Complex.IsNaN(expected) && Complex.IsNaN(actual))
            {
                return;
            }

            throw new Xunit.Sdk.EqualException(expected, actual);
        }

        private static void AssertBitwiseEqual(Complex expected, Complex actual)
        {
            AssertBitwiseEqual(expected.Real, actual.Real);
            AssertBitwiseEqual(expected.Imaginary, actual.Imaginary);
        }

        [Fact]
        public static void AdditiveIdentityTest()
        {
            AssertBitwiseEqual(0.0, AdditiveIdentityHelper<Complex, Complex>.AdditiveIdentity);
        }

        [Fact]
        public static void MultiplicativeIdentityTest()
        {
            AssertBitwiseEqual(1.0, MultiplicativeIdentityHelper<Complex, Complex>.MultiplicativeIdentity);
        }

        [Fact]
        public static void NegativeOneTest()
        {
            Assert.Equal(-1.0, SignedNumberHelper<Complex>.NegativeOne);
        }

        [Fact]
        public static void OneTest()
        {
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.One);
        }

        [Fact]
        public static void ZeroTest()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<Complex>.Zero);
        }

        [Fact]
        public static void op_AdditionTest()
        {
            AssertBitwiseEqual(0.0, AdditionOperatorsHelper<Complex, Complex, Complex>.op_Addition(-1.0, 1.0));
            AssertBitwiseEqual(1.0, AdditionOperatorsHelper<Complex, Complex, Complex>.op_Addition(-MinNormal, 1.0));
            AssertBitwiseEqual(1.0, AdditionOperatorsHelper<Complex, Complex, Complex>.op_Addition(-MaxSubnormal, 1.0));
            AssertBitwiseEqual(1.0, AdditionOperatorsHelper<Complex, Complex, Complex>.op_Addition(-0.0, 1.0));
            AssertBitwiseEqual(Complex.NaN, AdditionOperatorsHelper<Complex, Complex, Complex>.op_Addition(Complex.NaN, 1.0));
            AssertBitwiseEqual(1.0, AdditionOperatorsHelper<Complex, Complex, Complex>.op_Addition(0.0, 1.0));
            AssertBitwiseEqual(1.0, AdditionOperatorsHelper<Complex, Complex, Complex>.op_Addition(MaxSubnormal, 1.0));
            AssertBitwiseEqual(1.0, AdditionOperatorsHelper<Complex, Complex, Complex>.op_Addition(MinNormal, 1.0));
            AssertBitwiseEqual(2.0, AdditionOperatorsHelper<Complex, Complex, Complex>.op_Addition(1.0, 1.0));
        }

        [Fact]
        public static void op_CheckedAdditionTest()
        {
            AssertBitwiseEqual(0.0, AdditionOperatorsHelper<Complex, Complex, Complex>.op_CheckedAddition(-1.0, 1.0));
            AssertBitwiseEqual(1.0, AdditionOperatorsHelper<Complex, Complex, Complex>.op_CheckedAddition(-MinNormal, 1.0));
            AssertBitwiseEqual(1.0, AdditionOperatorsHelper<Complex, Complex, Complex>.op_CheckedAddition(-MaxSubnormal, 1.0));
            AssertBitwiseEqual(1.0, AdditionOperatorsHelper<Complex, Complex, Complex>.op_CheckedAddition(-0.0, 1.0));
            AssertBitwiseEqual(Complex.NaN, AdditionOperatorsHelper<Complex, Complex, Complex>.op_CheckedAddition(Complex.NaN, 1.0));
            AssertBitwiseEqual(1.0, AdditionOperatorsHelper<Complex, Complex, Complex>.op_CheckedAddition(0.0, 1.0));
            AssertBitwiseEqual(1.0, AdditionOperatorsHelper<Complex, Complex, Complex>.op_CheckedAddition(MaxSubnormal, 1.0));
            AssertBitwiseEqual(1.0, AdditionOperatorsHelper<Complex, Complex, Complex>.op_CheckedAddition(MinNormal, 1.0));
            AssertBitwiseEqual(2.0, AdditionOperatorsHelper<Complex, Complex, Complex>.op_CheckedAddition(1.0, 1.0));
        }

        [Fact]
        public static void op_DecrementTest()
        {
            AssertBitwiseEqual(-2.0, DecrementOperatorsHelper<Complex>.op_Decrement(-1.0));
            AssertBitwiseEqual(new Complex(-1.0, -0.0), DecrementOperatorsHelper<Complex>.op_Decrement(-MinNormal));
            AssertBitwiseEqual(new Complex(-1.0, -0.0), DecrementOperatorsHelper<Complex>.op_Decrement(-MaxSubnormal));
            AssertBitwiseEqual(-1.0, DecrementOperatorsHelper<Complex>.op_Decrement(-0.0));
            AssertBitwiseEqual(Complex.NaN, DecrementOperatorsHelper<Complex>.op_Decrement(Complex.NaN));
            AssertBitwiseEqual(-1.0, DecrementOperatorsHelper<Complex>.op_Decrement(0.0));
            AssertBitwiseEqual(-1.0, DecrementOperatorsHelper<Complex>.op_Decrement(MaxSubnormal));
            AssertBitwiseEqual(-1.0, DecrementOperatorsHelper<Complex>.op_Decrement(MinNormal));
            AssertBitwiseEqual(0.0, DecrementOperatorsHelper<Complex>.op_Decrement(1.0));
        }

        [Fact]
        public static void op_CheckedDecrementTest()
        {
            AssertBitwiseEqual(-2.0, DecrementOperatorsHelper<Complex>.op_CheckedDecrement(-1.0));
            AssertBitwiseEqual(new Complex(-1.0, -0.0), DecrementOperatorsHelper<Complex>.op_CheckedDecrement(-MinNormal));
            AssertBitwiseEqual(new Complex(-1.0, -0.0), DecrementOperatorsHelper<Complex>.op_CheckedDecrement(-MaxSubnormal));
            AssertBitwiseEqual(-1.0, DecrementOperatorsHelper<Complex>.op_CheckedDecrement(-0.0));
            AssertBitwiseEqual(Complex.NaN, DecrementOperatorsHelper<Complex>.op_CheckedDecrement(Complex.NaN));
            AssertBitwiseEqual(-1.0, DecrementOperatorsHelper<Complex>.op_CheckedDecrement(0.0));
            AssertBitwiseEqual(-1.0, DecrementOperatorsHelper<Complex>.op_CheckedDecrement(MaxSubnormal));
            AssertBitwiseEqual(-1.0, DecrementOperatorsHelper<Complex>.op_CheckedDecrement(MinNormal));
            AssertBitwiseEqual(0.0, DecrementOperatorsHelper<Complex>.op_CheckedDecrement(1.0));
        }

        [Fact]
        public static void op_DivisionTest()
        {
            AssertBitwiseEqual(-0.5, DivisionOperatorsHelper<Complex, Complex, Complex>.op_Division(-1.0, 2.0));
            AssertBitwiseEqual(-1.1125369292536007E-308, DivisionOperatorsHelper<Complex, Complex, Complex>.op_Division(-MinNormal, 2.0));
            AssertBitwiseEqual(-1.1125369292536007E-308, DivisionOperatorsHelper<Complex, Complex, Complex>.op_Division(-MaxSubnormal, 2.0));
            AssertBitwiseEqual(0.0, DivisionOperatorsHelper<Complex, Complex, Complex>.op_Division(-0.0, 2.0));
            AssertBitwiseEqual(Complex.NaN, DivisionOperatorsHelper<Complex, Complex, Complex>.op_Division(Complex.NaN, 2.0));
            AssertBitwiseEqual(0.0, DivisionOperatorsHelper<Complex, Complex, Complex>.op_Division(0.0, 2.0));
            AssertBitwiseEqual(1.1125369292536007E-308, DivisionOperatorsHelper<Complex, Complex, Complex>.op_Division(MaxSubnormal, 2.0));
            AssertBitwiseEqual(1.1125369292536007E-308, DivisionOperatorsHelper<Complex, Complex, Complex>.op_Division(MinNormal, 2.0));
            AssertBitwiseEqual(0.5, DivisionOperatorsHelper<Complex, Complex, Complex>.op_Division(1.0, 2.0));
        }

        [Fact]
        public static void op_CheckedDivisionTest()
        {
            AssertBitwiseEqual(-0.5, DivisionOperatorsHelper<Complex, Complex, Complex>.op_CheckedDivision(-1.0, 2.0));
            AssertBitwiseEqual(-1.1125369292536007E-308, DivisionOperatorsHelper<Complex, Complex, Complex>.op_CheckedDivision(-MinNormal, 2.0));
            AssertBitwiseEqual(-1.1125369292536007E-308, DivisionOperatorsHelper<Complex, Complex, Complex>.op_CheckedDivision(-MaxSubnormal, 2.0));
            AssertBitwiseEqual(0.0, DivisionOperatorsHelper<Complex, Complex, Complex>.op_CheckedDivision(-0.0, 2.0));
            AssertBitwiseEqual(Complex.NaN, DivisionOperatorsHelper<Complex, Complex, Complex>.op_CheckedDivision(Complex.NaN, 2.0));
            AssertBitwiseEqual(0.0, DivisionOperatorsHelper<Complex, Complex, Complex>.op_CheckedDivision(0.0, 2.0));
            AssertBitwiseEqual(1.1125369292536007E-308, DivisionOperatorsHelper<Complex, Complex, Complex>.op_CheckedDivision(MaxSubnormal, 2.0));
            AssertBitwiseEqual(1.1125369292536007E-308, DivisionOperatorsHelper<Complex, Complex, Complex>.op_CheckedDivision(MinNormal, 2.0));
            AssertBitwiseEqual(0.5, DivisionOperatorsHelper<Complex, Complex, Complex>.op_CheckedDivision(1.0, 2.0));
        }

        [Fact]
        public static void op_EqualityTest()
        {
            Assert.False(EqualityOperatorsHelper<Complex, Complex>.op_Equality(-1.0, 1.0));
            Assert.False(EqualityOperatorsHelper<Complex, Complex>.op_Equality(-MinNormal, 1.0));
            Assert.False(EqualityOperatorsHelper<Complex, Complex>.op_Equality(-MaxSubnormal, 1.0));
            Assert.False(EqualityOperatorsHelper<Complex, Complex>.op_Equality(-0.0, 1.0));
            Assert.False(EqualityOperatorsHelper<Complex, Complex>.op_Equality(Complex.NaN, 1.0));
            Assert.False(EqualityOperatorsHelper<Complex, Complex>.op_Equality(0.0, 1.0));
            Assert.False(EqualityOperatorsHelper<Complex, Complex>.op_Equality(MaxSubnormal, 1.0));
            Assert.False(EqualityOperatorsHelper<Complex, Complex>.op_Equality(MinNormal, 1.0));
            Assert.True(EqualityOperatorsHelper<Complex, Complex>.op_Equality(1.0, 1.0));
        }

        [Fact]
        public static void op_InequalityTest()
        {
            Assert.True(EqualityOperatorsHelper<Complex, Complex>.op_Inequality(-1.0, 1.0));
            Assert.True(EqualityOperatorsHelper<Complex, Complex>.op_Inequality(-MinNormal, 1.0));
            Assert.True(EqualityOperatorsHelper<Complex, Complex>.op_Inequality(-MaxSubnormal, 1.0));
            Assert.True(EqualityOperatorsHelper<Complex, Complex>.op_Inequality(-0.0, 1.0));
            Assert.True(EqualityOperatorsHelper<Complex, Complex>.op_Inequality(Complex.NaN, 1.0));
            Assert.True(EqualityOperatorsHelper<Complex, Complex>.op_Inequality(0.0, 1.0));
            Assert.True(EqualityOperatorsHelper<Complex, Complex>.op_Inequality(MaxSubnormal, 1.0));
            Assert.True(EqualityOperatorsHelper<Complex, Complex>.op_Inequality(MinNormal, 1.0));
            Assert.False(EqualityOperatorsHelper<Complex, Complex>.op_Inequality(1.0, 1.0));
        }

        [Fact]
        public static void op_IncrementTest()
        {
            AssertBitwiseEqual(0.0, IncrementOperatorsHelper<Complex>.op_Increment(-1.0));
            AssertBitwiseEqual(1.0, IncrementOperatorsHelper<Complex>.op_Increment(-MinNormal));
            AssertBitwiseEqual(1.0, IncrementOperatorsHelper<Complex>.op_Increment(-MaxSubnormal));
            AssertBitwiseEqual(1.0, IncrementOperatorsHelper<Complex>.op_Increment(-0.0));
            AssertBitwiseEqual(Complex.NaN, IncrementOperatorsHelper<Complex>.op_Increment(Complex.NaN));
            AssertBitwiseEqual(1.0, IncrementOperatorsHelper<Complex>.op_Increment(0.0));
            AssertBitwiseEqual(1.0, IncrementOperatorsHelper<Complex>.op_Increment(MaxSubnormal));
            AssertBitwiseEqual(1.0, IncrementOperatorsHelper<Complex>.op_Increment(MinNormal));
            AssertBitwiseEqual(2.0, IncrementOperatorsHelper<Complex>.op_Increment(1.0));
        }

        [Fact]
        public static void op_CheckedIncrementTest()
        {
            AssertBitwiseEqual(0.0, IncrementOperatorsHelper<Complex>.op_CheckedIncrement(-1.0));
            AssertBitwiseEqual(1.0, IncrementOperatorsHelper<Complex>.op_CheckedIncrement(-MinNormal));
            AssertBitwiseEqual(1.0, IncrementOperatorsHelper<Complex>.op_CheckedIncrement(-MaxSubnormal));
            AssertBitwiseEqual(1.0, IncrementOperatorsHelper<Complex>.op_CheckedIncrement(-0.0));
            AssertBitwiseEqual(Complex.NaN, IncrementOperatorsHelper<Complex>.op_CheckedIncrement(Complex.NaN));
            AssertBitwiseEqual(1.0, IncrementOperatorsHelper<Complex>.op_CheckedIncrement(0.0));
            AssertBitwiseEqual(1.0, IncrementOperatorsHelper<Complex>.op_CheckedIncrement(MaxSubnormal));
            AssertBitwiseEqual(1.0, IncrementOperatorsHelper<Complex>.op_CheckedIncrement(MinNormal));
            AssertBitwiseEqual(2.0, IncrementOperatorsHelper<Complex>.op_CheckedIncrement(1.0));
        }

        [Fact]
        public static void op_MultiplyTest()
        {
            AssertBitwiseEqual(-2.0, MultiplyOperatorsHelper<Complex, Complex, Complex>.op_Multiply(-1.0, 2.0));
            AssertBitwiseEqual(new Complex(-4.4501477170144028E-308, -0.0), MultiplyOperatorsHelper<Complex, Complex, Complex>.op_Multiply(-MinNormal, 2.0));
            AssertBitwiseEqual(new Complex(-4.4501477170144018E-308, -0.0), MultiplyOperatorsHelper<Complex, Complex, Complex>.op_Multiply(-MaxSubnormal, 2.0));
            AssertBitwiseEqual(-0.0, MultiplyOperatorsHelper<Complex, Complex, Complex>.op_Multiply(-0.0, 2.0));
            AssertBitwiseEqual(Complex.NaN, MultiplyOperatorsHelper<Complex, Complex, Complex>.op_Multiply(Complex.NaN, 2.0));
            AssertBitwiseEqual(0.0, MultiplyOperatorsHelper<Complex, Complex, Complex>.op_Multiply(0.0, 2.0));
            AssertBitwiseEqual(4.4501477170144018E-308, MultiplyOperatorsHelper<Complex, Complex, Complex>.op_Multiply(MaxSubnormal, 2.0));
            AssertBitwiseEqual(4.4501477170144028E-308, MultiplyOperatorsHelper<Complex, Complex, Complex>.op_Multiply(MinNormal, 2.0));
            AssertBitwiseEqual(2.0, MultiplyOperatorsHelper<Complex, Complex, Complex>.op_Multiply(1.0, 2.0));
        }

        [Fact]
        public static void op_CheckedMultiplyTest()
        {
            AssertBitwiseEqual(-2.0, MultiplyOperatorsHelper<Complex, Complex, Complex>.op_CheckedMultiply(-1.0, 2.0));
            AssertBitwiseEqual(new Complex(-4.4501477170144028E-308, -0.0), MultiplyOperatorsHelper<Complex, Complex, Complex>.op_CheckedMultiply(-MinNormal, 2.0));
            AssertBitwiseEqual(new Complex(-4.4501477170144018E-308, -0.0), MultiplyOperatorsHelper<Complex, Complex, Complex>.op_CheckedMultiply(-MaxSubnormal, 2.0));
            AssertBitwiseEqual(-0.0, MultiplyOperatorsHelper<Complex, Complex, Complex>.op_CheckedMultiply(-0.0, 2.0));
            AssertBitwiseEqual(Complex.NaN, MultiplyOperatorsHelper<Complex, Complex, Complex>.op_CheckedMultiply(Complex.NaN, 2.0));
            AssertBitwiseEqual(0.0, MultiplyOperatorsHelper<Complex, Complex, Complex>.op_CheckedMultiply(0.0, 2.0));
            AssertBitwiseEqual(4.4501477170144018E-308, MultiplyOperatorsHelper<Complex, Complex, Complex>.op_CheckedMultiply(MaxSubnormal, 2.0));
            AssertBitwiseEqual(4.4501477170144028E-308, MultiplyOperatorsHelper<Complex, Complex, Complex>.op_CheckedMultiply(MinNormal, 2.0));
            AssertBitwiseEqual(2.0, MultiplyOperatorsHelper<Complex, Complex, Complex>.op_CheckedMultiply(1.0, 2.0));
        }

        [Fact]
        public static void op_SubtractionTest()
        {
            AssertBitwiseEqual(-2.0, SubtractionOperatorsHelper<Complex, Complex, Complex>.op_Subtraction(-1.0, 1.0));
            AssertBitwiseEqual(new Complex(-1.0, -0.0), SubtractionOperatorsHelper<Complex, Complex, Complex>.op_Subtraction(-MinNormal, 1.0));
            AssertBitwiseEqual(new Complex(-1.0, -0.0), SubtractionOperatorsHelper<Complex, Complex, Complex>.op_Subtraction(-MaxSubnormal, 1.0));
            AssertBitwiseEqual(-1.0, SubtractionOperatorsHelper<Complex, Complex, Complex>.op_Subtraction(-0.0, 1.0));
            AssertBitwiseEqual(Complex.NaN, SubtractionOperatorsHelper<Complex, Complex, Complex>.op_Subtraction(Complex.NaN, 1.0));
            AssertBitwiseEqual(-1.0, SubtractionOperatorsHelper<Complex, Complex, Complex>.op_Subtraction(0.0, 1.0));
            AssertBitwiseEqual(-1.0, SubtractionOperatorsHelper<Complex, Complex, Complex>.op_Subtraction(MaxSubnormal, 1.0));
            AssertBitwiseEqual(-1.0, SubtractionOperatorsHelper<Complex, Complex, Complex>.op_Subtraction(MinNormal, 1.0));
            AssertBitwiseEqual(0.0, SubtractionOperatorsHelper<Complex, Complex, Complex>.op_Subtraction(1.0, 1.0));
        }

        [Fact]
        public static void op_CheckedSubtractionTest()
        {
            AssertBitwiseEqual(-2.0, SubtractionOperatorsHelper<Complex, Complex, Complex>.op_CheckedSubtraction(-1.0, 1.0));
            AssertBitwiseEqual(new Complex(-1.0, -0.0), SubtractionOperatorsHelper<Complex, Complex, Complex>.op_CheckedSubtraction(-MinNormal, 1.0));
            AssertBitwiseEqual(new Complex(-1.0, -0.0), SubtractionOperatorsHelper<Complex, Complex, Complex>.op_CheckedSubtraction(-MaxSubnormal, 1.0));
            AssertBitwiseEqual(-1.0, SubtractionOperatorsHelper<Complex, Complex, Complex>.op_CheckedSubtraction(-0.0, 1.0));
            AssertBitwiseEqual(Complex.NaN, SubtractionOperatorsHelper<Complex, Complex, Complex>.op_CheckedSubtraction(Complex.NaN, 1.0));
            AssertBitwiseEqual(-1.0, SubtractionOperatorsHelper<Complex, Complex, Complex>.op_CheckedSubtraction(0.0, 1.0));
            AssertBitwiseEqual(-1.0, SubtractionOperatorsHelper<Complex, Complex, Complex>.op_CheckedSubtraction(MaxSubnormal, 1.0));
            AssertBitwiseEqual(-1.0, SubtractionOperatorsHelper<Complex, Complex, Complex>.op_CheckedSubtraction(MinNormal, 1.0));
            AssertBitwiseEqual(0.0, SubtractionOperatorsHelper<Complex, Complex, Complex>.op_CheckedSubtraction(1.0, 1.0));
        }

        [Fact]
        public static void op_UnaryNegationTest()
        {
            AssertBitwiseEqual(new Complex(1.0, -0.0), UnaryNegationOperatorsHelper<Complex, Complex>.op_UnaryNegation(-1.0));
            AssertBitwiseEqual(MinNormal, UnaryNegationOperatorsHelper<Complex, Complex>.op_UnaryNegation(-MinNormal));
            AssertBitwiseEqual(MaxSubnormal, UnaryNegationOperatorsHelper<Complex, Complex>.op_UnaryNegation(-MaxSubnormal));
            AssertBitwiseEqual(new Complex(0.0, -0.0), UnaryNegationOperatorsHelper<Complex, Complex>.op_UnaryNegation(-0.0));
            AssertBitwiseEqual(Complex.NaN, UnaryNegationOperatorsHelper<Complex, Complex>.op_UnaryNegation(Complex.NaN));
            AssertBitwiseEqual(new Complex(-0.0, -0.0), UnaryNegationOperatorsHelper<Complex, Complex>.op_UnaryNegation(0.0));
            AssertBitwiseEqual(-MaxSubnormal, UnaryNegationOperatorsHelper<Complex, Complex>.op_UnaryNegation(MaxSubnormal));
            AssertBitwiseEqual(-MinNormal, UnaryNegationOperatorsHelper<Complex, Complex>.op_UnaryNegation(MinNormal));
            AssertBitwiseEqual(new Complex(-1.0, -0.0), UnaryNegationOperatorsHelper<Complex, Complex>.op_UnaryNegation(1.0));
        }

        [Fact]
        public static void op_CheckedUnaryNegationTest()
        {
            AssertBitwiseEqual(new Complex(1.0, -0.0), UnaryNegationOperatorsHelper<Complex, Complex>.op_CheckedUnaryNegation(-1.0));
            AssertBitwiseEqual(MinNormal, UnaryNegationOperatorsHelper<Complex, Complex>.op_CheckedUnaryNegation(-MinNormal));
            AssertBitwiseEqual(MaxSubnormal, UnaryNegationOperatorsHelper<Complex, Complex>.op_CheckedUnaryNegation(-MaxSubnormal));
            AssertBitwiseEqual(new Complex(0.0, -0.0), UnaryNegationOperatorsHelper<Complex, Complex>.op_CheckedUnaryNegation(-0.0));
            AssertBitwiseEqual(Complex.NaN, UnaryNegationOperatorsHelper<Complex, Complex>.op_CheckedUnaryNegation(Complex.NaN));
            AssertBitwiseEqual(new Complex(-0.0, -0.0), UnaryNegationOperatorsHelper<Complex, Complex>.op_CheckedUnaryNegation(0.0));
            AssertBitwiseEqual(-MaxSubnormal, UnaryNegationOperatorsHelper<Complex, Complex>.op_CheckedUnaryNegation(MaxSubnormal));
            AssertBitwiseEqual(-MinNormal, UnaryNegationOperatorsHelper<Complex, Complex>.op_CheckedUnaryNegation(MinNormal));
            AssertBitwiseEqual(new Complex(-1.0, -0.0), UnaryNegationOperatorsHelper<Complex, Complex>.op_CheckedUnaryNegation(1.0));
        }

        [Fact]
        public static void op_UnaryPlusTest()
        {
            AssertBitwiseEqual(-1.0, UnaryPlusOperatorsHelper<Complex, Complex>.op_UnaryPlus(-1.0));
            AssertBitwiseEqual(-MinNormal, UnaryPlusOperatorsHelper<Complex, Complex>.op_UnaryPlus(-MinNormal));
            AssertBitwiseEqual(-MaxSubnormal, UnaryPlusOperatorsHelper<Complex, Complex>.op_UnaryPlus(-MaxSubnormal));
            AssertBitwiseEqual(-0.0, UnaryPlusOperatorsHelper<Complex, Complex>.op_UnaryPlus(-0.0));
            AssertBitwiseEqual(Complex.NaN, UnaryPlusOperatorsHelper<Complex, Complex>.op_UnaryPlus(Complex.NaN));
            AssertBitwiseEqual(0.0, UnaryPlusOperatorsHelper<Complex, Complex>.op_UnaryPlus(0.0));
            AssertBitwiseEqual(MaxSubnormal, UnaryPlusOperatorsHelper<Complex, Complex>.op_UnaryPlus(MaxSubnormal));
            AssertBitwiseEqual(MinNormal, UnaryPlusOperatorsHelper<Complex, Complex>.op_UnaryPlus(MinNormal));
            AssertBitwiseEqual(1.0, UnaryPlusOperatorsHelper<Complex, Complex>.op_UnaryPlus(1.0));
        }
    }
}
