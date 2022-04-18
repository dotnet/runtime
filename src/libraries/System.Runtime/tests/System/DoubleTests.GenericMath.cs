// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Xunit;

namespace System.Tests
{
    public class DoubleTests_GenericMath
    {
        private const double MinNormal = 2.2250738585072014E-308;

        private const double MaxSubnormal = 2.2250738585072009E-308;

        private static void AssertBitwiseEqual(double expected, double actual)
        {
            ulong expectedBits = BitConverter.DoubleToUInt64Bits(expected);
            ulong actualBits = BitConverter.DoubleToUInt64Bits(actual);

            if (expectedBits == actualBits)
            {
                return;
            }

            if (double.IsNaN(expected) && double.IsNaN(actual))
            {
                return;
            }

            throw new Xunit.Sdk.EqualException(expected, actual);
        }

        [Fact]
        public static void AdditiveIdentityTest()
        {
            AssertBitwiseEqual(0.0, AdditiveIdentityHelper<double, double>.AdditiveIdentity);
        }

        [Fact]
        public static void MinValueTest()
        {
            AssertBitwiseEqual(double.MinValue, MinMaxValueHelper<double>.MinValue);
        }

        [Fact]
        public static void MaxValueTest()
        {
            AssertBitwiseEqual(double.MaxValue, MinMaxValueHelper<double>.MaxValue);
        }

        [Fact]
        public static void MultiplicativeIdentityTest()
        {
            AssertBitwiseEqual(1.0, MultiplicativeIdentityHelper<double, double>.MultiplicativeIdentity);
        }

        [Fact]
        public static void NegativeOneTest()
        {
            Assert.Equal(-1.0, SignedNumberHelper<double>.NegativeOne);
        }

        [Fact]
        public static void OneTest()
        {
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.One);
        }

        [Fact]
        public static void ZeroTest()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<double>.Zero);
        }

        [Fact]
        public static void op_AdditionTest()
        {
            AssertBitwiseEqual(double.NegativeInfinity, AdditionOperatorsHelper<double, double, double>.op_Addition(double.NegativeInfinity, 1.0));
            AssertBitwiseEqual(double.MinValue, AdditionOperatorsHelper<double, double, double>.op_Addition(double.MinValue, 1.0));
            AssertBitwiseEqual(0.0, AdditionOperatorsHelper<double, double, double>.op_Addition(-1.0, 1.0));
            AssertBitwiseEqual(1.0, AdditionOperatorsHelper<double, double, double>.op_Addition(-MinNormal, 1.0));
            AssertBitwiseEqual(1.0, AdditionOperatorsHelper<double, double, double>.op_Addition(-MaxSubnormal, 1.0));
            AssertBitwiseEqual(1.0, AdditionOperatorsHelper<double, double, double>.op_Addition(-double.Epsilon, 1.0));
            AssertBitwiseEqual(1.0, AdditionOperatorsHelper<double, double, double>.op_Addition(-0.0, 1.0));
            AssertBitwiseEqual(double.NaN, AdditionOperatorsHelper<double, double, double>.op_Addition(double.NaN, 1.0));
            AssertBitwiseEqual(1.0, AdditionOperatorsHelper<double, double, double>.op_Addition(0.0, 1.0));
            AssertBitwiseEqual(1.0, AdditionOperatorsHelper<double, double, double>.op_Addition(double.Epsilon, 1.0));
            AssertBitwiseEqual(1.0, AdditionOperatorsHelper<double, double, double>.op_Addition(MaxSubnormal, 1.0));
            AssertBitwiseEqual(1.0, AdditionOperatorsHelper<double, double, double>.op_Addition(MinNormal, 1.0));
            AssertBitwiseEqual(2.0, AdditionOperatorsHelper<double, double, double>.op_Addition(1.0, 1.0));
            AssertBitwiseEqual(double.MaxValue, AdditionOperatorsHelper<double, double, double>.op_Addition(double.MaxValue, 1.0));
            AssertBitwiseEqual(double.PositiveInfinity, AdditionOperatorsHelper<double, double, double>.op_Addition(double.PositiveInfinity, 1.0));
        }

        [Fact]
        public static void op_CheckedAdditionTest()
        {
            AssertBitwiseEqual(double.NegativeInfinity, AdditionOperatorsHelper<double, double, double>.op_CheckedAddition(double.NegativeInfinity, 1.0));
            AssertBitwiseEqual(double.MinValue, AdditionOperatorsHelper<double, double, double>.op_CheckedAddition(double.MinValue, 1.0));
            AssertBitwiseEqual(0.0, AdditionOperatorsHelper<double, double, double>.op_CheckedAddition(-1.0, 1.0));
            AssertBitwiseEqual(1.0, AdditionOperatorsHelper<double, double, double>.op_CheckedAddition(-MinNormal, 1.0));
            AssertBitwiseEqual(1.0, AdditionOperatorsHelper<double, double, double>.op_CheckedAddition(-MaxSubnormal, 1.0));
            AssertBitwiseEqual(1.0, AdditionOperatorsHelper<double, double, double>.op_CheckedAddition(-double.Epsilon, 1.0));
            AssertBitwiseEqual(1.0, AdditionOperatorsHelper<double, double, double>.op_CheckedAddition(-0.0, 1.0));
            AssertBitwiseEqual(double.NaN, AdditionOperatorsHelper<double, double, double>.op_CheckedAddition(double.NaN, 1.0));
            AssertBitwiseEqual(1.0, AdditionOperatorsHelper<double, double, double>.op_CheckedAddition(0.0, 1.0));
            AssertBitwiseEqual(1.0, AdditionOperatorsHelper<double, double, double>.op_CheckedAddition(double.Epsilon, 1.0));
            AssertBitwiseEqual(1.0, AdditionOperatorsHelper<double, double, double>.op_CheckedAddition(MaxSubnormal, 1.0));
            AssertBitwiseEqual(1.0, AdditionOperatorsHelper<double, double, double>.op_CheckedAddition(MinNormal, 1.0));
            AssertBitwiseEqual(2.0, AdditionOperatorsHelper<double, double, double>.op_CheckedAddition(1.0, 1.0));
            AssertBitwiseEqual(double.MaxValue, AdditionOperatorsHelper<double, double, double>.op_CheckedAddition(double.MaxValue, 1.0));
            AssertBitwiseEqual(double.PositiveInfinity, AdditionOperatorsHelper<double, double, double>.op_CheckedAddition(double.PositiveInfinity, 1.0));
        }

        [Fact]
        public static void IsPow2Test()
        {
            Assert.False(BinaryNumberHelper<double>.IsPow2(double.NegativeInfinity));
            Assert.False(BinaryNumberHelper<double>.IsPow2(double.MinValue));
            Assert.False(BinaryNumberHelper<double>.IsPow2(-1.0));
            Assert.False(BinaryNumberHelper<double>.IsPow2(-MinNormal));
            Assert.False(BinaryNumberHelper<double>.IsPow2(-MaxSubnormal));
            Assert.False(BinaryNumberHelper<double>.IsPow2(-double.Epsilon));
            Assert.False(BinaryNumberHelper<double>.IsPow2(-0.0));
            Assert.False(BinaryNumberHelper<double>.IsPow2(double.NaN));
            Assert.False(BinaryNumberHelper<double>.IsPow2(0.0));
            Assert.False(BinaryNumberHelper<double>.IsPow2(double.Epsilon));
            Assert.False(BinaryNumberHelper<double>.IsPow2(MaxSubnormal));
            Assert.True(BinaryNumberHelper<double>.IsPow2(MinNormal));
            Assert.True(BinaryNumberHelper<double>.IsPow2(1.0));
            Assert.False(BinaryNumberHelper<double>.IsPow2(double.MaxValue));
            Assert.False(BinaryNumberHelper<double>.IsPow2(double.PositiveInfinity));
        }

        [Fact]
        public static void Log2Test()
        {
            AssertBitwiseEqual(double.NaN, BinaryNumberHelper<double>.Log2(double.NegativeInfinity));
            AssertBitwiseEqual(double.NaN, BinaryNumberHelper<double>.Log2(double.MinValue));
            AssertBitwiseEqual(double.NaN, BinaryNumberHelper<double>.Log2(-1.0));
            AssertBitwiseEqual(double.NaN, BinaryNumberHelper<double>.Log2(-MinNormal));
            AssertBitwiseEqual(double.NaN, BinaryNumberHelper<double>.Log2(-MaxSubnormal));
            AssertBitwiseEqual(double.NaN, BinaryNumberHelper<double>.Log2(-double.Epsilon));
            AssertBitwiseEqual(double.NegativeInfinity, BinaryNumberHelper<double>.Log2(-0.0));
            AssertBitwiseEqual(double.NaN, BinaryNumberHelper<double>.Log2(double.NaN));
            AssertBitwiseEqual(double.NegativeInfinity, BinaryNumberHelper<double>.Log2(0.0));
            AssertBitwiseEqual(-1074.0, BinaryNumberHelper<double>.Log2(double.Epsilon));
            AssertBitwiseEqual(-1022.0, BinaryNumberHelper<double>.Log2(MaxSubnormal));
            AssertBitwiseEqual(-1022.0, BinaryNumberHelper<double>.Log2(MinNormal));
            AssertBitwiseEqual(0.0, BinaryNumberHelper<double>.Log2(1.0));
            AssertBitwiseEqual(1024.0, BinaryNumberHelper<double>.Log2(double.MaxValue));
            AssertBitwiseEqual(double.PositiveInfinity, BinaryNumberHelper<double>.Log2(double.PositiveInfinity));
        }

        [Fact]
        public static void op_LessThanTest()
        {
            Assert.True(ComparisonOperatorsHelper<double, double>.op_LessThan(double.NegativeInfinity, 1.0));
            Assert.True(ComparisonOperatorsHelper<double, double>.op_LessThan(double.MinValue, 1.0));
            Assert.True(ComparisonOperatorsHelper<double, double>.op_LessThan(-1.0, 1.0));
            Assert.True(ComparisonOperatorsHelper<double, double>.op_LessThan(-MinNormal, 1.0));
            Assert.True(ComparisonOperatorsHelper<double, double>.op_LessThan(-MaxSubnormal, 1.0));
            Assert.True(ComparisonOperatorsHelper<double, double>.op_LessThan(-double.Epsilon, 1.0));
            Assert.True(ComparisonOperatorsHelper<double, double>.op_LessThan(-0.0, 1.0));
            Assert.False(ComparisonOperatorsHelper<double, double>.op_LessThan(double.NaN, 1.0));
            Assert.True(ComparisonOperatorsHelper<double, double>.op_LessThan(0.0, 1.0));
            Assert.True(ComparisonOperatorsHelper<double, double>.op_LessThan(double.Epsilon, 1.0));
            Assert.True(ComparisonOperatorsHelper<double, double>.op_LessThan(MaxSubnormal, 1.0));
            Assert.True(ComparisonOperatorsHelper<double, double>.op_LessThan(MinNormal, 1.0));
            Assert.False(ComparisonOperatorsHelper<double, double>.op_LessThan(1.0, 1.0));
            Assert.False(ComparisonOperatorsHelper<double, double>.op_LessThan(double.MaxValue, 1.0));
            Assert.False(ComparisonOperatorsHelper<double, double>.op_LessThan(double.PositiveInfinity, 1.0));
        }

        [Fact]
        public static void op_LessThanOrEqualTest()
        {
            Assert.True(ComparisonOperatorsHelper<double, double>.op_LessThanOrEqual(double.NegativeInfinity, 1.0));
            Assert.True(ComparisonOperatorsHelper<double, double>.op_LessThanOrEqual(double.MinValue, 1.0));
            Assert.True(ComparisonOperatorsHelper<double, double>.op_LessThanOrEqual(-1.0, 1.0));
            Assert.True(ComparisonOperatorsHelper<double, double>.op_LessThanOrEqual(-MinNormal, 1.0));
            Assert.True(ComparisonOperatorsHelper<double, double>.op_LessThanOrEqual(-MaxSubnormal, 1.0));
            Assert.True(ComparisonOperatorsHelper<double, double>.op_LessThanOrEqual(-double.Epsilon, 1.0));
            Assert.True(ComparisonOperatorsHelper<double, double>.op_LessThanOrEqual(-0.0, 1.0));
            Assert.False(ComparisonOperatorsHelper<double, double>.op_LessThanOrEqual(double.NaN, 1.0));
            Assert.True(ComparisonOperatorsHelper<double, double>.op_LessThanOrEqual(0.0, 1.0));
            Assert.True(ComparisonOperatorsHelper<double, double>.op_LessThanOrEqual(double.Epsilon, 1.0));
            Assert.True(ComparisonOperatorsHelper<double, double>.op_LessThanOrEqual(MaxSubnormal, 1.0));
            Assert.True(ComparisonOperatorsHelper<double, double>.op_LessThanOrEqual(MinNormal, 1.0));
            Assert.True(ComparisonOperatorsHelper<double, double>.op_LessThanOrEqual(1.0, 1.0));
            Assert.False(ComparisonOperatorsHelper<double, double>.op_LessThanOrEqual(double.MaxValue, 1.0));
            Assert.False(ComparisonOperatorsHelper<double, double>.op_LessThanOrEqual(double.PositiveInfinity, 1.0));
        }

        [Fact]
        public static void op_GreaterThanTest()
        {
            Assert.False(ComparisonOperatorsHelper<double, double>.op_GreaterThan(double.NegativeInfinity, 1.0));
            Assert.False(ComparisonOperatorsHelper<double, double>.op_GreaterThan(double.MinValue, 1.0));
            Assert.False(ComparisonOperatorsHelper<double, double>.op_GreaterThan(-1.0, 1.0));
            Assert.False(ComparisonOperatorsHelper<double, double>.op_GreaterThan(-MinNormal, 1.0));
            Assert.False(ComparisonOperatorsHelper<double, double>.op_GreaterThan(-MaxSubnormal, 1.0));
            Assert.False(ComparisonOperatorsHelper<double, double>.op_GreaterThan(-double.Epsilon, 1.0));
            Assert.False(ComparisonOperatorsHelper<double, double>.op_GreaterThan(-0.0, 1.0));
            Assert.False(ComparisonOperatorsHelper<double, double>.op_GreaterThan(double.NaN, 1.0));
            Assert.False(ComparisonOperatorsHelper<double, double>.op_GreaterThan(0.0, 1.0));
            Assert.False(ComparisonOperatorsHelper<double, double>.op_GreaterThan(double.Epsilon, 1.0));
            Assert.False(ComparisonOperatorsHelper<double, double>.op_GreaterThan(MaxSubnormal, 1.0));
            Assert.False(ComparisonOperatorsHelper<double, double>.op_GreaterThan(MinNormal, 1.0));
            Assert.False(ComparisonOperatorsHelper<double, double>.op_GreaterThan(1.0, 1.0));
            Assert.True(ComparisonOperatorsHelper<double, double>.op_GreaterThan(double.MaxValue, 1.0));
            Assert.True(ComparisonOperatorsHelper<double, double>.op_GreaterThan(double.PositiveInfinity, 1.0));
        }

        [Fact]
        public static void op_GreaterThanOrEqualTest()
        {
            Assert.False(ComparisonOperatorsHelper<double, double>.op_GreaterThanOrEqual(double.NegativeInfinity, 1.0));
            Assert.False(ComparisonOperatorsHelper<double, double>.op_GreaterThanOrEqual(double.MinValue, 1.0));
            Assert.False(ComparisonOperatorsHelper<double, double>.op_GreaterThanOrEqual(-1.0, 1.0));
            Assert.False(ComparisonOperatorsHelper<double, double>.op_GreaterThanOrEqual(-MinNormal, 1.0));
            Assert.False(ComparisonOperatorsHelper<double, double>.op_GreaterThanOrEqual(-MaxSubnormal, 1.0));
            Assert.False(ComparisonOperatorsHelper<double, double>.op_GreaterThanOrEqual(-double.Epsilon, 1.0));
            Assert.False(ComparisonOperatorsHelper<double, double>.op_GreaterThanOrEqual(-0.0, 1.0));
            Assert.False(ComparisonOperatorsHelper<double, double>.op_GreaterThanOrEqual(double.NaN, 1.0));
            Assert.False(ComparisonOperatorsHelper<double, double>.op_GreaterThanOrEqual(0.0, 1.0));
            Assert.False(ComparisonOperatorsHelper<double, double>.op_GreaterThanOrEqual(double.Epsilon, 1.0));
            Assert.False(ComparisonOperatorsHelper<double, double>.op_GreaterThanOrEqual(MaxSubnormal, 1.0));
            Assert.False(ComparisonOperatorsHelper<double, double>.op_GreaterThanOrEqual(MinNormal, 1.0));
            Assert.True(ComparisonOperatorsHelper<double, double>.op_GreaterThanOrEqual(1.0, 1.0));
            Assert.True(ComparisonOperatorsHelper<double, double>.op_GreaterThanOrEqual(double.MaxValue, 1.0));
            Assert.True(ComparisonOperatorsHelper<double, double>.op_GreaterThanOrEqual(double.PositiveInfinity, 1.0));
        }

        [Fact]
        public static void op_DecrementTest()
        {
            AssertBitwiseEqual(double.NegativeInfinity, DecrementOperatorsHelper<double>.op_Decrement(double.NegativeInfinity));
            AssertBitwiseEqual(double.MinValue, DecrementOperatorsHelper<double>.op_Decrement(double.MinValue));
            AssertBitwiseEqual(-2.0, DecrementOperatorsHelper<double>.op_Decrement(-1.0));
            AssertBitwiseEqual(-1.0, DecrementOperatorsHelper<double>.op_Decrement(-MinNormal));
            AssertBitwiseEqual(-1.0, DecrementOperatorsHelper<double>.op_Decrement(-MaxSubnormal));
            AssertBitwiseEqual(-1.0, DecrementOperatorsHelper<double>.op_Decrement(-double.Epsilon));
            AssertBitwiseEqual(-1.0, DecrementOperatorsHelper<double>.op_Decrement(-0.0));
            AssertBitwiseEqual(double.NaN, DecrementOperatorsHelper<double>.op_Decrement(double.NaN));
            AssertBitwiseEqual(-1.0, DecrementOperatorsHelper<double>.op_Decrement(0.0));
            AssertBitwiseEqual(-1.0, DecrementOperatorsHelper<double>.op_Decrement(double.Epsilon));
            AssertBitwiseEqual(-1.0, DecrementOperatorsHelper<double>.op_Decrement(MaxSubnormal));
            AssertBitwiseEqual(-1.0, DecrementOperatorsHelper<double>.op_Decrement(MinNormal));
            AssertBitwiseEqual(0.0, DecrementOperatorsHelper<double>.op_Decrement(1.0));
            AssertBitwiseEqual(double.MaxValue, DecrementOperatorsHelper<double>.op_Decrement(double.MaxValue));
            AssertBitwiseEqual(double.PositiveInfinity, DecrementOperatorsHelper<double>.op_Decrement(double.PositiveInfinity));
        }

        [Fact]
        public static void op_CheckedDecrementTest()
        {
            AssertBitwiseEqual(double.NegativeInfinity, DecrementOperatorsHelper<double>.op_CheckedDecrement(double.NegativeInfinity));
            AssertBitwiseEqual(double.MinValue, DecrementOperatorsHelper<double>.op_CheckedDecrement(double.MinValue));
            AssertBitwiseEqual(-2.0, DecrementOperatorsHelper<double>.op_CheckedDecrement(-1.0));
            AssertBitwiseEqual(-1.0, DecrementOperatorsHelper<double>.op_CheckedDecrement(-MinNormal));
            AssertBitwiseEqual(-1.0, DecrementOperatorsHelper<double>.op_CheckedDecrement(-MaxSubnormal));
            AssertBitwiseEqual(-1.0, DecrementOperatorsHelper<double>.op_CheckedDecrement(-double.Epsilon));
            AssertBitwiseEqual(-1.0, DecrementOperatorsHelper<double>.op_CheckedDecrement(-0.0));
            AssertBitwiseEqual(double.NaN, DecrementOperatorsHelper<double>.op_CheckedDecrement(double.NaN));
            AssertBitwiseEqual(-1.0, DecrementOperatorsHelper<double>.op_CheckedDecrement(0.0));
            AssertBitwiseEqual(-1.0, DecrementOperatorsHelper<double>.op_CheckedDecrement(double.Epsilon));
            AssertBitwiseEqual(-1.0, DecrementOperatorsHelper<double>.op_CheckedDecrement(MaxSubnormal));
            AssertBitwiseEqual(-1.0, DecrementOperatorsHelper<double>.op_CheckedDecrement(MinNormal));
            AssertBitwiseEqual(0.0, DecrementOperatorsHelper<double>.op_CheckedDecrement(1.0));
            AssertBitwiseEqual(double.MaxValue, DecrementOperatorsHelper<double>.op_CheckedDecrement(double.MaxValue));
            AssertBitwiseEqual(double.PositiveInfinity, DecrementOperatorsHelper<double>.op_CheckedDecrement(double.PositiveInfinity));
        }

        [Fact]
        public static void op_DivisionTest()
        {
            AssertBitwiseEqual(double.NegativeInfinity, DivisionOperatorsHelper<double, double, double>.op_Division(double.NegativeInfinity, 2.0));
            AssertBitwiseEqual(-8.9884656743115785E+307, DivisionOperatorsHelper<double, double, double>.op_Division(double.MinValue, 2.0));
            AssertBitwiseEqual(-0.5, DivisionOperatorsHelper<double, double, double>.op_Division(-1.0, 2.0));
            AssertBitwiseEqual(-1.1125369292536007E-308, DivisionOperatorsHelper<double, double, double>.op_Division(-MinNormal, 2.0));
            AssertBitwiseEqual(-1.1125369292536007E-308, DivisionOperatorsHelper<double, double, double>.op_Division(-MaxSubnormal, 2.0));
            AssertBitwiseEqual(-0.0, DivisionOperatorsHelper<double, double, double>.op_Division(-double.Epsilon, 2.0));
            AssertBitwiseEqual(-0.0, DivisionOperatorsHelper<double, double, double>.op_Division(-0.0, 2.0));
            AssertBitwiseEqual(double.NaN, DivisionOperatorsHelper<double, double, double>.op_Division(double.NaN, 2.0));
            AssertBitwiseEqual(0.0, DivisionOperatorsHelper<double, double, double>.op_Division(0.0, 2.0));
            AssertBitwiseEqual(0.0, DivisionOperatorsHelper<double, double, double>.op_Division(double.Epsilon, 2.0));
            AssertBitwiseEqual(1.1125369292536007E-308, DivisionOperatorsHelper<double, double, double>.op_Division(MaxSubnormal, 2.0));
            AssertBitwiseEqual(1.1125369292536007E-308, DivisionOperatorsHelper<double, double, double>.op_Division(MinNormal, 2.0));
            AssertBitwiseEqual(0.5, DivisionOperatorsHelper<double, double, double>.op_Division(1.0, 2.0));
            AssertBitwiseEqual(8.9884656743115785E+307, DivisionOperatorsHelper<double, double, double>.op_Division(double.MaxValue, 2.0));
            AssertBitwiseEqual(double.PositiveInfinity, DivisionOperatorsHelper<double, double, double>.op_Division(double.PositiveInfinity, 2.0));
        }

        [Fact]
        public static void op_CheckedDivisionTest()
        {
            AssertBitwiseEqual(double.NegativeInfinity, DivisionOperatorsHelper<double, double, double>.op_CheckedDivision(double.NegativeInfinity, 2.0));
            AssertBitwiseEqual(-8.9884656743115785E+307, DivisionOperatorsHelper<double, double, double>.op_CheckedDivision(double.MinValue, 2.0));
            AssertBitwiseEqual(-0.5, DivisionOperatorsHelper<double, double, double>.op_CheckedDivision(-1.0, 2.0));
            AssertBitwiseEqual(-1.1125369292536007E-308, DivisionOperatorsHelper<double, double, double>.op_CheckedDivision(-MinNormal, 2.0));
            AssertBitwiseEqual(-1.1125369292536007E-308, DivisionOperatorsHelper<double, double, double>.op_CheckedDivision(-MaxSubnormal, 2.0));
            AssertBitwiseEqual(-0.0, DivisionOperatorsHelper<double, double, double>.op_CheckedDivision(-double.Epsilon, 2.0));
            AssertBitwiseEqual(-0.0, DivisionOperatorsHelper<double, double, double>.op_CheckedDivision(-0.0, 2.0));
            AssertBitwiseEqual(double.NaN, DivisionOperatorsHelper<double, double, double>.op_CheckedDivision(double.NaN, 2.0));
            AssertBitwiseEqual(0.0, DivisionOperatorsHelper<double, double, double>.op_CheckedDivision(0.0, 2.0));
            AssertBitwiseEqual(0.0, DivisionOperatorsHelper<double, double, double>.op_CheckedDivision(double.Epsilon, 2.0));
            AssertBitwiseEqual(1.1125369292536007E-308, DivisionOperatorsHelper<double, double, double>.op_CheckedDivision(MaxSubnormal, 2.0));
            AssertBitwiseEqual(1.1125369292536007E-308, DivisionOperatorsHelper<double, double, double>.op_CheckedDivision(MinNormal, 2.0));
            AssertBitwiseEqual(0.5, DivisionOperatorsHelper<double, double, double>.op_CheckedDivision(1.0, 2.0));
            AssertBitwiseEqual(8.9884656743115785E+307, DivisionOperatorsHelper<double, double, double>.op_CheckedDivision(double.MaxValue, 2.0));
            AssertBitwiseEqual(double.PositiveInfinity, DivisionOperatorsHelper<double, double, double>.op_CheckedDivision(double.PositiveInfinity, 2.0));
        }

        [Fact]
        public static void op_EqualityTest()
        {
            Assert.False(EqualityOperatorsHelper<double, double>.op_Equality(double.NegativeInfinity, 1.0));
            Assert.False(EqualityOperatorsHelper<double, double>.op_Equality(double.MinValue, 1.0));
            Assert.False(EqualityOperatorsHelper<double, double>.op_Equality(-1.0, 1.0));
            Assert.False(EqualityOperatorsHelper<double, double>.op_Equality(-MinNormal, 1.0));
            Assert.False(EqualityOperatorsHelper<double, double>.op_Equality(-MaxSubnormal, 1.0));
            Assert.False(EqualityOperatorsHelper<double, double>.op_Equality(-double.Epsilon, 1.0));
            Assert.False(EqualityOperatorsHelper<double, double>.op_Equality(-0.0, 1.0));
            Assert.False(EqualityOperatorsHelper<double, double>.op_Equality(double.NaN, 1.0));
            Assert.False(EqualityOperatorsHelper<double, double>.op_Equality(0.0, 1.0));
            Assert.False(EqualityOperatorsHelper<double, double>.op_Equality(double.Epsilon, 1.0));
            Assert.False(EqualityOperatorsHelper<double, double>.op_Equality(MaxSubnormal, 1.0));
            Assert.False(EqualityOperatorsHelper<double, double>.op_Equality(MinNormal, 1.0));
            Assert.True(EqualityOperatorsHelper<double, double>.op_Equality(1.0, 1.0));
            Assert.False(EqualityOperatorsHelper<double, double>.op_Equality(double.MaxValue, 1.0));
            Assert.False(EqualityOperatorsHelper<double, double>.op_Equality(double.PositiveInfinity, 1.0));
        }

        [Fact]
        public static void op_InequalityTest()
        {
            Assert.True(EqualityOperatorsHelper<double, double>.op_Inequality(double.NegativeInfinity, 1.0));
            Assert.True(EqualityOperatorsHelper<double, double>.op_Inequality(double.MinValue, 1.0));
            Assert.True(EqualityOperatorsHelper<double, double>.op_Inequality(-1.0, 1.0));
            Assert.True(EqualityOperatorsHelper<double, double>.op_Inequality(-MinNormal, 1.0));
            Assert.True(EqualityOperatorsHelper<double, double>.op_Inequality(-MaxSubnormal, 1.0));
            Assert.True(EqualityOperatorsHelper<double, double>.op_Inequality(-double.Epsilon, 1.0));
            Assert.True(EqualityOperatorsHelper<double, double>.op_Inequality(-0.0, 1.0));
            Assert.True(EqualityOperatorsHelper<double, double>.op_Inequality(double.NaN, 1.0));
            Assert.True(EqualityOperatorsHelper<double, double>.op_Inequality(0.0, 1.0));
            Assert.True(EqualityOperatorsHelper<double, double>.op_Inequality(double.Epsilon, 1.0));
            Assert.True(EqualityOperatorsHelper<double, double>.op_Inequality(MaxSubnormal, 1.0));
            Assert.True(EqualityOperatorsHelper<double, double>.op_Inequality(MinNormal, 1.0));
            Assert.False(EqualityOperatorsHelper<double, double>.op_Inequality(1.0, 1.0));
            Assert.True(EqualityOperatorsHelper<double, double>.op_Inequality(double.MaxValue, 1.0));
            Assert.True(EqualityOperatorsHelper<double, double>.op_Inequality(double.PositiveInfinity, 1.0));
        }

        [Fact]
        public static void op_IncrementTest()
        {
            AssertBitwiseEqual(double.NegativeInfinity, IncrementOperatorsHelper<double>.op_Increment(double.NegativeInfinity));
            AssertBitwiseEqual(double.MinValue, IncrementOperatorsHelper<double>.op_Increment(double.MinValue));
            AssertBitwiseEqual(0.0, IncrementOperatorsHelper<double>.op_Increment(-1.0));
            AssertBitwiseEqual(1.0, IncrementOperatorsHelper<double>.op_Increment(-MinNormal));
            AssertBitwiseEqual(1.0, IncrementOperatorsHelper<double>.op_Increment(-MaxSubnormal));
            AssertBitwiseEqual(1.0, IncrementOperatorsHelper<double>.op_Increment(-double.Epsilon));
            AssertBitwiseEqual(1.0, IncrementOperatorsHelper<double>.op_Increment(-0.0));
            AssertBitwiseEqual(double.NaN, IncrementOperatorsHelper<double>.op_Increment(double.NaN));
            AssertBitwiseEqual(1.0, IncrementOperatorsHelper<double>.op_Increment(0.0));
            AssertBitwiseEqual(1.0, IncrementOperatorsHelper<double>.op_Increment(double.Epsilon));
            AssertBitwiseEqual(1.0, IncrementOperatorsHelper<double>.op_Increment(MaxSubnormal));
            AssertBitwiseEqual(1.0, IncrementOperatorsHelper<double>.op_Increment(MinNormal));
            AssertBitwiseEqual(2.0, IncrementOperatorsHelper<double>.op_Increment(1.0));
            AssertBitwiseEqual(double.MaxValue, IncrementOperatorsHelper<double>.op_Increment(double.MaxValue));
            AssertBitwiseEqual(double.PositiveInfinity, IncrementOperatorsHelper<double>.op_Increment(double.PositiveInfinity));
        }

        [Fact]
        public static void op_CheckedIncrementTest()
        {
            AssertBitwiseEqual(double.NegativeInfinity, IncrementOperatorsHelper<double>.op_CheckedIncrement(double.NegativeInfinity));
            AssertBitwiseEqual(double.MinValue, IncrementOperatorsHelper<double>.op_CheckedIncrement(double.MinValue));
            AssertBitwiseEqual(0.0, IncrementOperatorsHelper<double>.op_CheckedIncrement(-1.0));
            AssertBitwiseEqual(1.0, IncrementOperatorsHelper<double>.op_CheckedIncrement(-MinNormal));
            AssertBitwiseEqual(1.0, IncrementOperatorsHelper<double>.op_CheckedIncrement(-MaxSubnormal));
            AssertBitwiseEqual(1.0, IncrementOperatorsHelper<double>.op_CheckedIncrement(-double.Epsilon));
            AssertBitwiseEqual(1.0, IncrementOperatorsHelper<double>.op_CheckedIncrement(-0.0));
            AssertBitwiseEqual(double.NaN, IncrementOperatorsHelper<double>.op_CheckedIncrement(double.NaN));
            AssertBitwiseEqual(1.0, IncrementOperatorsHelper<double>.op_CheckedIncrement(0.0));
            AssertBitwiseEqual(1.0, IncrementOperatorsHelper<double>.op_CheckedIncrement(double.Epsilon));
            AssertBitwiseEqual(1.0, IncrementOperatorsHelper<double>.op_CheckedIncrement(MaxSubnormal));
            AssertBitwiseEqual(1.0, IncrementOperatorsHelper<double>.op_CheckedIncrement(MinNormal));
            AssertBitwiseEqual(2.0, IncrementOperatorsHelper<double>.op_CheckedIncrement(1.0));
            AssertBitwiseEqual(double.MaxValue, IncrementOperatorsHelper<double>.op_CheckedIncrement(double.MaxValue));
            AssertBitwiseEqual(double.PositiveInfinity, IncrementOperatorsHelper<double>.op_CheckedIncrement(double.PositiveInfinity));
        }

        [Fact]
        public static void op_ModulusTest()
        {
            AssertBitwiseEqual(double.NaN, ModulusOperatorsHelper<double, double, double>.op_Modulus(double.NegativeInfinity, 2.0));
            AssertBitwiseEqual(-0.0, ModulusOperatorsHelper<double, double, double>.op_Modulus(double.MinValue, 2.0));
            AssertBitwiseEqual(-1.0, ModulusOperatorsHelper<double, double, double>.op_Modulus(-1.0, 2.0));
            AssertBitwiseEqual(-MinNormal, ModulusOperatorsHelper<double, double, double>.op_Modulus(-MinNormal, 2.0));
            AssertBitwiseEqual(-MaxSubnormal, ModulusOperatorsHelper<double, double, double>.op_Modulus(-MaxSubnormal, 2.0));
            AssertBitwiseEqual(-double.Epsilon, ModulusOperatorsHelper<double, double, double>.op_Modulus(-double.Epsilon, 2.0)); ;
            AssertBitwiseEqual(-0.0, ModulusOperatorsHelper<double, double, double>.op_Modulus(-0.0, 2.0));
            AssertBitwiseEqual(double.NaN, ModulusOperatorsHelper<double, double, double>.op_Modulus(double.NaN, 2.0));
            AssertBitwiseEqual(0.0, ModulusOperatorsHelper<double, double, double>.op_Modulus(0.0, 2.0));
            AssertBitwiseEqual(double.Epsilon, ModulusOperatorsHelper<double, double, double>.op_Modulus(double.Epsilon, 2.0));
            AssertBitwiseEqual(MaxSubnormal, ModulusOperatorsHelper<double, double, double>.op_Modulus(MaxSubnormal, 2.0));
            AssertBitwiseEqual(MinNormal, ModulusOperatorsHelper<double, double, double>.op_Modulus(MinNormal, 2.0));
            AssertBitwiseEqual(1.0, ModulusOperatorsHelper<double, double, double>.op_Modulus(1.0, 2.0));
            AssertBitwiseEqual(0.0, ModulusOperatorsHelper<double, double, double>.op_Modulus(double.MaxValue, 2.0));
            AssertBitwiseEqual(double.NaN, ModulusOperatorsHelper<double, double, double>.op_Modulus(double.PositiveInfinity, 2.0));
        }

        [Fact]
        public static void op_MultiplyTest()
        {
            AssertBitwiseEqual(double.NegativeInfinity, MultiplyOperatorsHelper<double, double, double>.op_Multiply(double.NegativeInfinity, 2.0));
            AssertBitwiseEqual(double.NegativeInfinity, MultiplyOperatorsHelper<double, double, double>.op_Multiply(double.MinValue, 2.0));
            AssertBitwiseEqual(-2.0, MultiplyOperatorsHelper<double, double, double>.op_Multiply(-1.0, 2.0));
            AssertBitwiseEqual(-4.4501477170144028E-308, MultiplyOperatorsHelper<double, double, double>.op_Multiply(-MinNormal, 2.0));
            AssertBitwiseEqual(-4.4501477170144018E-308, MultiplyOperatorsHelper<double, double, double>.op_Multiply(-MaxSubnormal, 2.0));
            AssertBitwiseEqual(-9.8813129168249309E-324, MultiplyOperatorsHelper<double, double, double>.op_Multiply(-double.Epsilon, 2.0));
            AssertBitwiseEqual(-0.0, MultiplyOperatorsHelper<double, double, double>.op_Multiply(-0.0, 2.0));
            AssertBitwiseEqual(double.NaN, MultiplyOperatorsHelper<double, double, double>.op_Multiply(double.NaN, 2.0));
            AssertBitwiseEqual(0.0, MultiplyOperatorsHelper<double, double, double>.op_Multiply(0.0, 2.0));
            AssertBitwiseEqual(9.8813129168249309E-324, MultiplyOperatorsHelper<double, double, double>.op_Multiply(double.Epsilon, 2.0));
            AssertBitwiseEqual(4.4501477170144018E-308, MultiplyOperatorsHelper<double, double, double>.op_Multiply(MaxSubnormal, 2.0));
            AssertBitwiseEqual(4.4501477170144028E-308, MultiplyOperatorsHelper<double, double, double>.op_Multiply(MinNormal, 2.0));
            AssertBitwiseEqual(2.0, MultiplyOperatorsHelper<double, double, double>.op_Multiply(1.0, 2.0));
            AssertBitwiseEqual(double.PositiveInfinity, MultiplyOperatorsHelper<double, double, double>.op_Multiply(double.MaxValue, 2.0));
            AssertBitwiseEqual(double.PositiveInfinity, MultiplyOperatorsHelper<double, double, double>.op_Multiply(double.PositiveInfinity, 2.0));
        }

        [Fact]
        public static void op_CheckedMultiplyTest()
        {
            AssertBitwiseEqual(double.NegativeInfinity, MultiplyOperatorsHelper<double, double, double>.op_CheckedMultiply(double.NegativeInfinity, 2.0));
            AssertBitwiseEqual(double.NegativeInfinity, MultiplyOperatorsHelper<double, double, double>.op_CheckedMultiply(double.MinValue, 2.0));
            AssertBitwiseEqual(-2.0, MultiplyOperatorsHelper<double, double, double>.op_CheckedMultiply(-1.0, 2.0));
            AssertBitwiseEqual(-4.4501477170144028E-308, MultiplyOperatorsHelper<double, double, double>.op_CheckedMultiply(-MinNormal, 2.0));
            AssertBitwiseEqual(-4.4501477170144018E-308, MultiplyOperatorsHelper<double, double, double>.op_CheckedMultiply(-MaxSubnormal, 2.0));
            AssertBitwiseEqual(-9.8813129168249309E-324, MultiplyOperatorsHelper<double, double, double>.op_CheckedMultiply(-double.Epsilon, 2.0));
            AssertBitwiseEqual(-0.0, MultiplyOperatorsHelper<double, double, double>.op_CheckedMultiply(-0.0, 2.0));
            AssertBitwiseEqual(double.NaN, MultiplyOperatorsHelper<double, double, double>.op_CheckedMultiply(double.NaN, 2.0));
            AssertBitwiseEqual(0.0, MultiplyOperatorsHelper<double, double, double>.op_CheckedMultiply(0.0, 2.0));
            AssertBitwiseEqual(9.8813129168249309E-324, MultiplyOperatorsHelper<double, double, double>.op_CheckedMultiply(double.Epsilon, 2.0));
            AssertBitwiseEqual(4.4501477170144018E-308, MultiplyOperatorsHelper<double, double, double>.op_CheckedMultiply(MaxSubnormal, 2.0));
            AssertBitwiseEqual(4.4501477170144028E-308, MultiplyOperatorsHelper<double, double, double>.op_CheckedMultiply(MinNormal, 2.0));
            AssertBitwiseEqual(2.0, MultiplyOperatorsHelper<double, double, double>.op_CheckedMultiply(1.0, 2.0));
            AssertBitwiseEqual(double.PositiveInfinity, MultiplyOperatorsHelper<double, double, double>.op_CheckedMultiply(double.MaxValue, 2.0));
            AssertBitwiseEqual(double.PositiveInfinity, MultiplyOperatorsHelper<double, double, double>.op_CheckedMultiply(double.PositiveInfinity, 2.0));
        }

        [Fact]
        public static void AbsTest()
        {
            AssertBitwiseEqual(double.PositiveInfinity, NumberHelper<double>.Abs(double.NegativeInfinity));
            AssertBitwiseEqual(double.MaxValue, NumberHelper<double>.Abs(double.MinValue));
            AssertBitwiseEqual(1.0, NumberHelper<double>.Abs(-1.0));
            AssertBitwiseEqual(MinNormal, NumberHelper<double>.Abs(-MinNormal));
            AssertBitwiseEqual(MaxSubnormal, NumberHelper<double>.Abs(-MaxSubnormal));
            AssertBitwiseEqual(double.Epsilon, NumberHelper<double>.Abs(-double.Epsilon));
            AssertBitwiseEqual(0.0, NumberHelper<double>.Abs(-0.0));
            AssertBitwiseEqual(double.NaN, NumberHelper<double>.Abs(double.NaN));
            AssertBitwiseEqual(0.0, NumberHelper<double>.Abs(0.0));
            AssertBitwiseEqual(double.Epsilon, NumberHelper<double>.Abs(double.Epsilon));
            AssertBitwiseEqual(MaxSubnormal, NumberHelper<double>.Abs(MaxSubnormal));
            AssertBitwiseEqual(MinNormal, NumberHelper<double>.Abs(MinNormal));
            AssertBitwiseEqual(1.0, NumberHelper<double>.Abs(1.0));
            AssertBitwiseEqual(double.MaxValue, NumberHelper<double>.Abs(double.MaxValue));
            AssertBitwiseEqual(double.PositiveInfinity, NumberHelper<double>.Abs(double.PositiveInfinity));
        }

        [Fact]
        public static void ClampTest()
        {
            AssertBitwiseEqual(1.0, NumberHelper<double>.Clamp(double.NegativeInfinity, 1.0, 63.0));
            AssertBitwiseEqual(1.0, NumberHelper<double>.Clamp(double.MinValue, 1.0, 63.0));
            AssertBitwiseEqual(1.0, NumberHelper<double>.Clamp(-1.0, 1.0, 63.0));
            AssertBitwiseEqual(1.0, NumberHelper<double>.Clamp(-MinNormal, 1.0, 63.0));
            AssertBitwiseEqual(1.0, NumberHelper<double>.Clamp(-MaxSubnormal, 1.0, 63.0));
            AssertBitwiseEqual(1.0, NumberHelper<double>.Clamp(-double.Epsilon, 1.0, 63.0));
            AssertBitwiseEqual(1.0, NumberHelper<double>.Clamp(-0.0, 1.0, 63.0));
            AssertBitwiseEqual(double.NaN, NumberHelper<double>.Clamp(double.NaN, 1.0, 63.0));
            AssertBitwiseEqual(1.0, NumberHelper<double>.Clamp(0.0, 1.0, 63.0));
            AssertBitwiseEqual(1.0, NumberHelper<double>.Clamp(double.Epsilon, 1.0, 63.0));
            AssertBitwiseEqual(1.0, NumberHelper<double>.Clamp(MaxSubnormal, 1.0, 63.0));
            AssertBitwiseEqual(1.0, NumberHelper<double>.Clamp(MinNormal, 1.0, 63.0));
            AssertBitwiseEqual(1.0, NumberHelper<double>.Clamp(1.0, 1.0, 63.0));
            AssertBitwiseEqual(63.0, NumberHelper<double>.Clamp(double.MaxValue, 1.0, 63.0));
            AssertBitwiseEqual(63.0, NumberHelper<double>.Clamp(double.PositiveInfinity, 1.0, 63.0));
        }

        [Fact]
        public static void CreateCheckedFromByteTest()
        {
            AssertBitwiseEqual(0.0, NumberHelper<double>.CreateChecked<byte>(0x00));
            AssertBitwiseEqual(1.0, NumberHelper<double>.CreateChecked<byte>(0x01));
            AssertBitwiseEqual(127.0, NumberHelper<double>.CreateChecked<byte>(0x7F));
            AssertBitwiseEqual(128.0, NumberHelper<double>.CreateChecked<byte>(0x80));
            AssertBitwiseEqual(255.0, NumberHelper<double>.CreateChecked<byte>(0xFF));
        }

        [Fact]
        public static void CreateCheckedFromCharTest()
        {
            AssertBitwiseEqual(0.0, NumberHelper<double>.CreateChecked<char>((char)0x0000));
            AssertBitwiseEqual(1.0, NumberHelper<double>.CreateChecked<char>((char)0x0001));
            AssertBitwiseEqual(32767.0, NumberHelper<double>.CreateChecked<char>((char)0x7FFF));
            AssertBitwiseEqual(32768.0, NumberHelper<double>.CreateChecked<char>((char)0x8000));
            AssertBitwiseEqual(65535.0, NumberHelper<double>.CreateChecked<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateCheckedFromInt16Test()
        {
            AssertBitwiseEqual(0.0, NumberHelper<double>.CreateChecked<short>(0x0000));
            AssertBitwiseEqual(1.0, NumberHelper<double>.CreateChecked<short>(0x0001));
            AssertBitwiseEqual(32767.0, NumberHelper<double>.CreateChecked<short>(0x7FFF));
            AssertBitwiseEqual(-32768.0, NumberHelper<double>.CreateChecked<short>(unchecked((short)0x8000)));
            AssertBitwiseEqual(-1.0, NumberHelper<double>.CreateChecked<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromInt32Test()
        {
            AssertBitwiseEqual(0.0, NumberHelper<double>.CreateChecked<int>(0x00000000));
            AssertBitwiseEqual(1.0, NumberHelper<double>.CreateChecked<int>(0x00000001));
            AssertBitwiseEqual(2147483647.0, NumberHelper<double>.CreateChecked<int>(0x7FFFFFFF));
            AssertBitwiseEqual(-2147483648.0, NumberHelper<double>.CreateChecked<int>(unchecked((int)0x80000000)));
            AssertBitwiseEqual(-1.0, NumberHelper<double>.CreateChecked<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromInt64Test()
        {
            AssertBitwiseEqual(0.0, NumberHelper<double>.CreateChecked<long>(0x0000000000000000));
            AssertBitwiseEqual(1.0, NumberHelper<double>.CreateChecked<long>(0x0000000000000001));
            AssertBitwiseEqual(9223372036854775807.0, NumberHelper<double>.CreateChecked<long>(0x7FFFFFFFFFFFFFFF));
            AssertBitwiseEqual(-9223372036854775808.0, NumberHelper<double>.CreateChecked<long>(unchecked(unchecked((long)0x8000000000000000))));
            AssertBitwiseEqual(-1.0, NumberHelper<double>.CreateChecked<long>(unchecked(unchecked((long)0xFFFFFFFFFFFFFFFF))));
        }

        [Fact]
        public static void CreateCheckedFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual(0.0, NumberHelper<double>.CreateChecked<nint>(unchecked((nint)0x0000000000000000)));
                AssertBitwiseEqual(1.0, NumberHelper<double>.CreateChecked<nint>(unchecked((nint)0x0000000000000001)));
                AssertBitwiseEqual(9223372036854775807.0, NumberHelper<double>.CreateChecked<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                AssertBitwiseEqual(-9223372036854775808.0, NumberHelper<double>.CreateChecked<nint>(unchecked((nint)0x8000000000000000)));
                AssertBitwiseEqual(-1.0, NumberHelper<double>.CreateChecked<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                AssertBitwiseEqual(0.0, NumberHelper<double>.CreateChecked<nint>((nint)0x00000000));
                AssertBitwiseEqual(1.0, NumberHelper<double>.CreateChecked<nint>((nint)0x00000001));
                AssertBitwiseEqual(2147483647.0, NumberHelper<double>.CreateChecked<nint>((nint)0x7FFFFFFF));
                AssertBitwiseEqual(-2147483648.0, NumberHelper<double>.CreateChecked<nint>(unchecked((nint)0x80000000)));
                AssertBitwiseEqual(-1.0, NumberHelper<double>.CreateChecked<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateCheckedFromSByteTest()
        {
            AssertBitwiseEqual(0.0, NumberHelper<double>.CreateChecked<sbyte>(0x00));
            AssertBitwiseEqual(1.0, NumberHelper<double>.CreateChecked<sbyte>(0x01));
            AssertBitwiseEqual(127.0, NumberHelper<double>.CreateChecked<sbyte>(0x7F));
            AssertBitwiseEqual(-128.0, NumberHelper<double>.CreateChecked<sbyte>(unchecked((sbyte)0x80)));
            AssertBitwiseEqual(-1.0, NumberHelper<double>.CreateChecked<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateCheckedFromUInt16Test()
        {
            AssertBitwiseEqual(0.0, NumberHelper<double>.CreateChecked<ushort>(0x0000));
            AssertBitwiseEqual(1.0, NumberHelper<double>.CreateChecked<ushort>(0x0001));
            AssertBitwiseEqual(32767.0, NumberHelper<double>.CreateChecked<ushort>(0x7FFF));
            AssertBitwiseEqual(32768.0, NumberHelper<double>.CreateChecked<ushort>(0x8000));
            AssertBitwiseEqual(65535.0, NumberHelper<double>.CreateChecked<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUInt32Test()
        {
            AssertBitwiseEqual(0.0, NumberHelper<double>.CreateChecked<uint>(0x00000000));
            AssertBitwiseEqual(1.0, NumberHelper<double>.CreateChecked<uint>(0x00000001));
            AssertBitwiseEqual(2147483647.0, NumberHelper<double>.CreateChecked<uint>(0x7FFFFFFF));
            AssertBitwiseEqual(2147483648.0, NumberHelper<double>.CreateChecked<uint>(0x80000000));
            AssertBitwiseEqual(4294967295.0, NumberHelper<double>.CreateChecked<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUInt64Test()
        {
            AssertBitwiseEqual(0.0, NumberHelper<double>.CreateChecked<ulong>(0x0000000000000000));
            AssertBitwiseEqual(1.0, NumberHelper<double>.CreateChecked<ulong>(0x0000000000000001));
            AssertBitwiseEqual(9223372036854775807.0, NumberHelper<double>.CreateChecked<ulong>(0x7FFFFFFFFFFFFFFF));
            AssertBitwiseEqual(9223372036854775808.0, NumberHelper<double>.CreateChecked<ulong>(0x8000000000000000));
            AssertBitwiseEqual(18446744073709551615.0, NumberHelper<double>.CreateChecked<ulong>(0xFFFFFFFFFFFFFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual(0.0, NumberHelper<double>.CreateChecked<nuint>(unchecked((nuint)0x0000000000000000)));
                AssertBitwiseEqual(1.0, NumberHelper<double>.CreateChecked<nuint>(unchecked((nuint)0x0000000000000001)));
                AssertBitwiseEqual(9223372036854775807.0, NumberHelper<double>.CreateChecked<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));

                // https://github.com/dotnet/roslyn/issues/60714
                // AssertBitwiseEqual(9223372036854775808.0, NumberHelper<double>.CreateChecked<nuint>(unchecked((nuint)0x8000000000000000)));
                // AssertBitwiseEqual(18446744073709551615.0,NumberHelper<double>.CreateChecked<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                AssertBitwiseEqual(0.0, NumberHelper<double>.CreateChecked<nuint>((nuint)0x00000000));
                AssertBitwiseEqual(1.0, NumberHelper<double>.CreateChecked<nuint>((nuint)0x00000001));
                AssertBitwiseEqual(2147483647.0, NumberHelper<double>.CreateChecked<nuint>((nuint)0x7FFFFFFF));

                // https://github.com/dotnet/roslyn/issues/60714
                // AssertBitwiseEqual(2147483648.0, NumberHelper<double>.CreateChecked<nuint>((nuint)0x80000000));
                // AssertBitwiseEqual(4294967295.0, NumberHelper<double>.CreateChecked<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateSaturatingFromByteTest()
        {
            AssertBitwiseEqual(0.0, NumberHelper<double>.CreateSaturating<byte>(0x00));
            AssertBitwiseEqual(1.0, NumberHelper<double>.CreateSaturating<byte>(0x01));
            AssertBitwiseEqual(127.0, NumberHelper<double>.CreateSaturating<byte>(0x7F));
            AssertBitwiseEqual(128.0, NumberHelper<double>.CreateSaturating<byte>(0x80));
            AssertBitwiseEqual(255.0, NumberHelper<double>.CreateSaturating<byte>(0xFF));
        }

        [Fact]
        public static void CreateSaturatingFromCharTest()
        {
            AssertBitwiseEqual(0.0, NumberHelper<double>.CreateSaturating<char>((char)0x0000));
            AssertBitwiseEqual(1.0, NumberHelper<double>.CreateSaturating<char>((char)0x0001));
            AssertBitwiseEqual(32767.0, NumberHelper<double>.CreateSaturating<char>((char)0x7FFF));
            AssertBitwiseEqual(32768.0, NumberHelper<double>.CreateSaturating<char>((char)0x8000));
            AssertBitwiseEqual(65535.0, NumberHelper<double>.CreateSaturating<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromInt16Test()
        {
            AssertBitwiseEqual(0.0, NumberHelper<double>.CreateSaturating<short>(0x0000));
            AssertBitwiseEqual(1.0, NumberHelper<double>.CreateSaturating<short>(0x0001));
            AssertBitwiseEqual(32767.0, NumberHelper<double>.CreateSaturating<short>(0x7FFF));
            AssertBitwiseEqual(-32768.0, NumberHelper<double>.CreateSaturating<short>(unchecked((short)0x8000)));
            AssertBitwiseEqual(-1.0, NumberHelper<double>.CreateSaturating<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromInt32Test()
        {
            AssertBitwiseEqual(0.0, NumberHelper<double>.CreateSaturating<int>(0x00000000));
            AssertBitwiseEqual(1.0, NumberHelper<double>.CreateSaturating<int>(0x00000001));
            AssertBitwiseEqual(2147483647.0, NumberHelper<double>.CreateSaturating<int>(0x7FFFFFFF));
            AssertBitwiseEqual(-2147483648.0, NumberHelper<double>.CreateSaturating<int>(unchecked((int)0x80000000)));
            AssertBitwiseEqual(-1.0, NumberHelper<double>.CreateSaturating<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromInt64Test()
        {
            AssertBitwiseEqual(0.0, NumberHelper<double>.CreateSaturating<long>(0x0000000000000000));
            AssertBitwiseEqual(1.0, NumberHelper<double>.CreateSaturating<long>(0x0000000000000001));
            AssertBitwiseEqual(9223372036854775807.0, NumberHelper<double>.CreateSaturating<long>(0x7FFFFFFFFFFFFFFF));
            AssertBitwiseEqual(-9223372036854775808.0, NumberHelper<double>.CreateSaturating<long>(unchecked(unchecked((long)0x8000000000000000))));
            AssertBitwiseEqual(-1.0, NumberHelper<double>.CreateSaturating<long>(unchecked(unchecked((long)0xFFFFFFFFFFFFFFFF))));
        }

        [Fact]
        public static void CreateSaturatingFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual(0.0, NumberHelper<double>.CreateSaturating<nint>(unchecked((nint)0x0000000000000000)));
                AssertBitwiseEqual(1.0, NumberHelper<double>.CreateSaturating<nint>(unchecked((nint)0x0000000000000001)));
                AssertBitwiseEqual(9223372036854775807.0, NumberHelper<double>.CreateSaturating<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                AssertBitwiseEqual(-9223372036854775808.0, NumberHelper<double>.CreateSaturating<nint>(unchecked((nint)0x8000000000000000)));
                AssertBitwiseEqual(-1.0, NumberHelper<double>.CreateSaturating<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                AssertBitwiseEqual(0.0, NumberHelper<double>.CreateSaturating<nint>((nint)0x00000000));
                AssertBitwiseEqual(1.0, NumberHelper<double>.CreateSaturating<nint>((nint)0x00000001));
                AssertBitwiseEqual(2147483647.0, NumberHelper<double>.CreateSaturating<nint>((nint)0x7FFFFFFF));
                AssertBitwiseEqual(-2147483648.0, NumberHelper<double>.CreateSaturating<nint>(unchecked((nint)0x80000000)));
                AssertBitwiseEqual(-1.0, NumberHelper<double>.CreateSaturating<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateSaturatingFromSByteTest()
        {
            AssertBitwiseEqual(0.0, NumberHelper<double>.CreateSaturating<sbyte>(0x00));
            AssertBitwiseEqual(1.0, NumberHelper<double>.CreateSaturating<sbyte>(0x01));
            AssertBitwiseEqual(127.0, NumberHelper<double>.CreateSaturating<sbyte>(0x7F));
            AssertBitwiseEqual(-128.0, NumberHelper<double>.CreateSaturating<sbyte>(unchecked((sbyte)0x80)));
            AssertBitwiseEqual(-1.0, NumberHelper<double>.CreateSaturating<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateSaturatingFromUInt16Test()
        {
            AssertBitwiseEqual(0.0, NumberHelper<double>.CreateSaturating<ushort>(0x0000));
            AssertBitwiseEqual(1.0, NumberHelper<double>.CreateSaturating<ushort>(0x0001));
            AssertBitwiseEqual(32767.0, NumberHelper<double>.CreateSaturating<ushort>(0x7FFF));
            AssertBitwiseEqual(32768.0, NumberHelper<double>.CreateSaturating<ushort>(0x8000));
            AssertBitwiseEqual(65535.0, NumberHelper<double>.CreateSaturating<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUInt32Test()
        {
            AssertBitwiseEqual(0.0, NumberHelper<double>.CreateSaturating<uint>(0x00000000));
            AssertBitwiseEqual(1.0, NumberHelper<double>.CreateSaturating<uint>(0x00000001));
            AssertBitwiseEqual(2147483647.0, NumberHelper<double>.CreateSaturating<uint>(0x7FFFFFFF));
            AssertBitwiseEqual(2147483648.0, NumberHelper<double>.CreateSaturating<uint>(0x80000000));
            AssertBitwiseEqual(4294967295.0, NumberHelper<double>.CreateSaturating<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUInt64Test()
        {
            AssertBitwiseEqual(0.0, NumberHelper<double>.CreateSaturating<ulong>(0x0000000000000000));
            AssertBitwiseEqual(1.0, NumberHelper<double>.CreateSaturating<ulong>(0x0000000000000001));
            AssertBitwiseEqual(9223372036854775807.0, NumberHelper<double>.CreateSaturating<ulong>(0x7FFFFFFFFFFFFFFF));
            AssertBitwiseEqual(9223372036854775808.0, NumberHelper<double>.CreateSaturating<ulong>(0x8000000000000000));
            AssertBitwiseEqual(18446744073709551615.0, NumberHelper<double>.CreateSaturating<ulong>(0xFFFFFFFFFFFFFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual(0.0, NumberHelper<double>.CreateSaturating<nuint>(unchecked((nuint)0x0000000000000000)));
                AssertBitwiseEqual(1.0, NumberHelper<double>.CreateSaturating<nuint>(unchecked((nuint)0x0000000000000001)));
                AssertBitwiseEqual(9223372036854775807.0, NumberHelper<double>.CreateSaturating<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));

                // https://github.com/dotnet/roslyn/issues/60714
                // AssertBitwiseEqual(9223372036854775808.0, NumberHelper<double>.CreateSaturating<nuint>(unchecked((nuint)0x8000000000000000)));
                // AssertBitwiseEqual(18446744073709551615.0, NumberHelper<double>.CreateSaturating<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                AssertBitwiseEqual(0.0, NumberHelper<double>.CreateSaturating<nuint>((nuint)0x00000000));
                AssertBitwiseEqual(1.0, NumberHelper<double>.CreateSaturating<nuint>((nuint)0x00000001));
                AssertBitwiseEqual(2147483647.0, NumberHelper<double>.CreateSaturating<nuint>((nuint)0x7FFFFFFF));

                // https://github.com/dotnet/roslyn/issues/60714
                // AssertBitwiseEqual(2147483648.0, NumberHelper<double>.CreateSaturating<nuint>((nuint)0x80000000));
                // AssertBitwiseEqual(4294967295.0, NumberHelper<double>.CreateSaturating<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateTruncatingFromByteTest()
        {
            AssertBitwiseEqual(0.0, NumberHelper<double>.CreateTruncating<byte>(0x00));
            AssertBitwiseEqual(1.0, NumberHelper<double>.CreateTruncating<byte>(0x01));
            AssertBitwiseEqual(127.0, NumberHelper<double>.CreateTruncating<byte>(0x7F));
            AssertBitwiseEqual(128.0, NumberHelper<double>.CreateTruncating<byte>(0x80));
            AssertBitwiseEqual(255.0, NumberHelper<double>.CreateTruncating<byte>(0xFF));
        }

        [Fact]
        public static void CreateTruncatingFromCharTest()
        {
            AssertBitwiseEqual(0.0, NumberHelper<double>.CreateTruncating<char>((char)0x0000));
            AssertBitwiseEqual(1.0, NumberHelper<double>.CreateTruncating<char>((char)0x0001));
            AssertBitwiseEqual(32767.0, NumberHelper<double>.CreateTruncating<char>((char)0x7FFF));
            AssertBitwiseEqual(32768.0, NumberHelper<double>.CreateTruncating<char>((char)0x8000));
            AssertBitwiseEqual(65535.0, NumberHelper<double>.CreateTruncating<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromInt16Test()
        {
            AssertBitwiseEqual(0.0, NumberHelper<double>.CreateTruncating<short>(0x0000));
            AssertBitwiseEqual(1.0, NumberHelper<double>.CreateTruncating<short>(0x0001));
            AssertBitwiseEqual(32767.0, NumberHelper<double>.CreateTruncating<short>(0x7FFF));
            AssertBitwiseEqual(-32768.0, NumberHelper<double>.CreateTruncating<short>(unchecked((short)0x8000)));
            AssertBitwiseEqual(-1.0, NumberHelper<double>.CreateTruncating<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateTruncatingFromInt32Test()
        {
            AssertBitwiseEqual(0.0, NumberHelper<double>.CreateTruncating<int>(0x00000000));
            AssertBitwiseEqual(1.0, NumberHelper<double>.CreateTruncating<int>(0x00000001));
            AssertBitwiseEqual(2147483647.0, NumberHelper<double>.CreateTruncating<int>(0x7FFFFFFF));
            AssertBitwiseEqual(-2147483648.0, NumberHelper<double>.CreateTruncating<int>(unchecked((int)0x80000000)));
            AssertBitwiseEqual(-1.0, NumberHelper<double>.CreateTruncating<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateTruncatingFromInt64Test()
        {
            AssertBitwiseEqual(0.0, NumberHelper<double>.CreateTruncating<long>(0x0000000000000000));
            AssertBitwiseEqual(1.0, NumberHelper<double>.CreateTruncating<long>(0x0000000000000001));
            AssertBitwiseEqual(9223372036854775807.0, NumberHelper<double>.CreateTruncating<long>(0x7FFFFFFFFFFFFFFF));
            AssertBitwiseEqual(-9223372036854775808.0, NumberHelper<double>.CreateTruncating<long>(unchecked(unchecked((long)0x8000000000000000))));
            AssertBitwiseEqual(-1.0, NumberHelper<double>.CreateTruncating<long>(unchecked(unchecked((long)0xFFFFFFFFFFFFFFFF))));
        }

        [Fact]
        public static void CreateTruncatingFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual(0.0, NumberHelper<double>.CreateTruncating<nint>(unchecked((nint)0x0000000000000000)));
                AssertBitwiseEqual(1.0, NumberHelper<double>.CreateTruncating<nint>(unchecked((nint)0x0000000000000001)));
                AssertBitwiseEqual(9223372036854775807.0, NumberHelper<double>.CreateTruncating<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                AssertBitwiseEqual(-9223372036854775808.0, NumberHelper<double>.CreateTruncating<nint>(unchecked((nint)0x8000000000000000)));
                AssertBitwiseEqual(-1.0, NumberHelper<double>.CreateTruncating<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                AssertBitwiseEqual(0.0, NumberHelper<double>.CreateTruncating<nint>((nint)0x00000000));
                AssertBitwiseEqual(1.0, NumberHelper<double>.CreateTruncating<nint>((nint)0x00000001));
                AssertBitwiseEqual(2147483647.0, NumberHelper<double>.CreateTruncating<nint>((nint)0x7FFFFFFF));
                AssertBitwiseEqual(-2147483648.0, NumberHelper<double>.CreateTruncating<nint>(unchecked((nint)0x80000000)));
                AssertBitwiseEqual(-1.0, NumberHelper<double>.CreateTruncating<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateTruncatingFromSByteTest()
        {
            AssertBitwiseEqual(0.0, NumberHelper<double>.CreateTruncating<sbyte>(0x00));
            AssertBitwiseEqual(1.0, NumberHelper<double>.CreateTruncating<sbyte>(0x01));
            AssertBitwiseEqual(127.0, NumberHelper<double>.CreateTruncating<sbyte>(0x7F));
            AssertBitwiseEqual(-128.0, NumberHelper<double>.CreateTruncating<sbyte>(unchecked((sbyte)0x80)));
            AssertBitwiseEqual(-1.0, NumberHelper<double>.CreateTruncating<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateTruncatingFromUInt16Test()
        {
            AssertBitwiseEqual(0.0, NumberHelper<double>.CreateTruncating<ushort>(0x0000));
            AssertBitwiseEqual(1.0, NumberHelper<double>.CreateTruncating<ushort>(0x0001));
            AssertBitwiseEqual(32767.0, NumberHelper<double>.CreateTruncating<ushort>(0x7FFF));
            AssertBitwiseEqual(32768.0, NumberHelper<double>.CreateTruncating<ushort>(0x8000));
            AssertBitwiseEqual(65535.0, NumberHelper<double>.CreateTruncating<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt32Test()
        {
            AssertBitwiseEqual(0.0, NumberHelper<double>.CreateTruncating<uint>(0x00000000));
            AssertBitwiseEqual(1.0, NumberHelper<double>.CreateTruncating<uint>(0x00000001));
            AssertBitwiseEqual(2147483647.0, NumberHelper<double>.CreateTruncating<uint>(0x7FFFFFFF));
            AssertBitwiseEqual(2147483648.0, NumberHelper<double>.CreateTruncating<uint>(0x80000000));
            AssertBitwiseEqual(4294967295.0, NumberHelper<double>.CreateTruncating<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt64Test()
        {
            AssertBitwiseEqual(0.0, NumberHelper<double>.CreateTruncating<ulong>(0x0000000000000000));
            AssertBitwiseEqual(1.0, NumberHelper<double>.CreateTruncating<ulong>(0x0000000000000001));
            AssertBitwiseEqual(9223372036854775807.0, NumberHelper<double>.CreateTruncating<ulong>(0x7FFFFFFFFFFFFFFF));
            AssertBitwiseEqual(9223372036854775808.0, NumberHelper<double>.CreateTruncating<ulong>(0x8000000000000000));
            AssertBitwiseEqual(18446744073709551615.0, NumberHelper<double>.CreateTruncating<ulong>(0xFFFFFFFFFFFFFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual(0.0, NumberHelper<double>.CreateTruncating<nuint>(unchecked((nuint)0x0000000000000000)));
                AssertBitwiseEqual(1.0, NumberHelper<double>.CreateTruncating<nuint>(unchecked((nuint)0x0000000000000001)));
                AssertBitwiseEqual(9223372036854775807.0, NumberHelper<double>.CreateTruncating<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));

                // https://github.com/dotnet/roslyn/issues/60714
                // AssertBitwiseEqual(9223372036854775808.0, NumberHelper<double>.CreateTruncating<nuint>(unchecked((nuint)0x8000000000000000)));
                // AssertBitwiseEqual(18446744073709551615.0, NumberHelper<double>.CreateTruncating<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                AssertBitwiseEqual(0.0, NumberHelper<double>.CreateTruncating<nuint>((nuint)0x00000000));
                AssertBitwiseEqual(1.0, NumberHelper<double>.CreateTruncating<nuint>((nuint)0x00000001));
                AssertBitwiseEqual(2147483647.0, NumberHelper<double>.CreateTruncating<nuint>((nuint)0x7FFFFFFF));

                // https://github.com/dotnet/roslyn/issues/60714
                // AssertBitwiseEqual(2147483648.0, NumberHelper<double>.CreateTruncating<nuint>((nuint)0x80000000));
                // AssertBitwiseEqual(4294967295.0, NumberHelper<double>.CreateTruncating<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void MaxTest()
        {
            AssertBitwiseEqual(1.0, NumberHelper<double>.Max(double.NegativeInfinity, 1.0));
            AssertBitwiseEqual(1.0, NumberHelper<double>.Max(double.MinValue, 1.0));
            AssertBitwiseEqual(1.0, NumberHelper<double>.Max(-1.0, 1.0));
            AssertBitwiseEqual(1.0, NumberHelper<double>.Max(-MinNormal, 1.0));
            AssertBitwiseEqual(1.0, NumberHelper<double>.Max(-MaxSubnormal, 1.0));
            AssertBitwiseEqual(1.0, NumberHelper<double>.Max(-double.Epsilon, 1.0));
            AssertBitwiseEqual(1.0, NumberHelper<double>.Max(-0.0, 1.0));
            AssertBitwiseEqual(double.NaN, NumberHelper<double>.Max(double.NaN, 1.0));
            AssertBitwiseEqual(1.0, NumberHelper<double>.Max(0.0, 1.0));
            AssertBitwiseEqual(1.0, NumberHelper<double>.Max(double.Epsilon, 1.0));
            AssertBitwiseEqual(1.0, NumberHelper<double>.Max(MaxSubnormal, 1.0));
            AssertBitwiseEqual(1.0, NumberHelper<double>.Max(MinNormal, 1.0));
            AssertBitwiseEqual(1.0, NumberHelper<double>.Max(1.0, 1.0));
            AssertBitwiseEqual(double.MaxValue, NumberHelper<double>.Max(double.MaxValue, 1.0));
            AssertBitwiseEqual(double.PositiveInfinity, NumberHelper<double>.Max(double.PositiveInfinity, 1.0));
        }

        [Fact]
        public static void MinTest()
        {
            AssertBitwiseEqual(double.NegativeInfinity, NumberHelper<double>.Min(double.NegativeInfinity, 1.0));
            AssertBitwiseEqual(double.MinValue, NumberHelper<double>.Min(double.MinValue, 1.0));
            AssertBitwiseEqual(-1.0, NumberHelper<double>.Min(-1.0, 1.0));
            AssertBitwiseEqual(-MinNormal, NumberHelper<double>.Min(-MinNormal, 1.0));
            AssertBitwiseEqual(-MaxSubnormal, NumberHelper<double>.Min(-MaxSubnormal, 1.0));
            AssertBitwiseEqual(-double.Epsilon, NumberHelper<double>.Min(-double.Epsilon, 1.0));
            AssertBitwiseEqual(-0.0, NumberHelper<double>.Min(-0.0, 1.0));
            AssertBitwiseEqual(double.NaN, NumberHelper<double>.Min(double.NaN, 1.0));
            AssertBitwiseEqual(0.0, NumberHelper<double>.Min(0.0, 1.0));
            AssertBitwiseEqual(double.Epsilon, NumberHelper<double>.Min(double.Epsilon, 1.0));
            AssertBitwiseEqual(MaxSubnormal, NumberHelper<double>.Min(MaxSubnormal, 1.0));
            AssertBitwiseEqual(MinNormal, NumberHelper<double>.Min(MinNormal, 1.0));
            AssertBitwiseEqual(1.0, NumberHelper<double>.Min(1.0, 1.0));
            AssertBitwiseEqual(1.0, NumberHelper<double>.Min(double.MaxValue, 1.0));
            AssertBitwiseEqual(1.0, NumberHelper<double>.Min(double.PositiveInfinity, 1.0));
        }

        [Fact]
        public static void SignTest()
        {
            Assert.Equal(-1, NumberHelper<double>.Sign(double.NegativeInfinity));
            Assert.Equal(-1, NumberHelper<double>.Sign(double.MinValue));
            Assert.Equal(-1, NumberHelper<double>.Sign(-1.0));
            Assert.Equal(-1, NumberHelper<double>.Sign(-MinNormal));
            Assert.Equal(-1, NumberHelper<double>.Sign(-MaxSubnormal));
            Assert.Equal(-1, NumberHelper<double>.Sign(-double.Epsilon));

            Assert.Equal(0, NumberHelper<double>.Sign(-0.0));
            Assert.Equal(0, NumberHelper<double>.Sign(0.0));

            Assert.Equal(1, NumberHelper<double>.Sign(double.Epsilon));
            Assert.Equal(1, NumberHelper<double>.Sign(MaxSubnormal));
            Assert.Equal(1, NumberHelper<double>.Sign(MinNormal));
            Assert.Equal(1, NumberHelper<double>.Sign(1.0));
            Assert.Equal(1, NumberHelper<double>.Sign(double.MaxValue));
            Assert.Equal(1, NumberHelper<double>.Sign(double.PositiveInfinity));

            Assert.Throws<ArithmeticException>(() => NumberHelper<double>.Sign(double.NaN));
        }

        [Fact]
        public static void TryCreateFromByteTest()
        {
            double result;

            Assert.True(NumberHelper<double>.TryCreate<byte>(0x00, out result));
            Assert.Equal(0.0, result);

            Assert.True(NumberHelper<double>.TryCreate<byte>(0x01, out result));
            Assert.Equal(1.0, result);

            Assert.True(NumberHelper<double>.TryCreate<byte>(0x7F, out result));
            Assert.Equal(127.0, result);

            Assert.True(NumberHelper<double>.TryCreate<byte>(0x80, out result));
            Assert.Equal(128.0, result);

            Assert.True(NumberHelper<double>.TryCreate<byte>(0xFF, out result));
            Assert.Equal(255.0, result);
        }

        [Fact]
        public static void TryCreateFromCharTest()
        {
            double result;

            Assert.True(NumberHelper<double>.TryCreate<char>((char)0x0000, out result));
            Assert.Equal(0.0, result);

            Assert.True(NumberHelper<double>.TryCreate<char>((char)0x0001, out result));
            Assert.Equal(1.0, result);

            Assert.True(NumberHelper<double>.TryCreate<char>((char)0x7FFF, out result));
            Assert.Equal(32767.0, result);

            Assert.True(NumberHelper<double>.TryCreate<char>((char)0x8000, out result));
            Assert.Equal(32768.0, result);

            Assert.True(NumberHelper<double>.TryCreate<char>((char)0xFFFF, out result));
            Assert.Equal(65535.0, result);
        }

        [Fact]
        public static void TryCreateFromInt16Test()
        {
            double result;

            Assert.True(NumberHelper<double>.TryCreate<short>(0x0000, out result));
            Assert.Equal(0.0, result);

            Assert.True(NumberHelper<double>.TryCreate<short>(0x0001, out result));
            Assert.Equal(1.0, result);

            Assert.True(NumberHelper<double>.TryCreate<short>(0x7FFF, out result));
            Assert.Equal(32767.0, result);

            Assert.True(NumberHelper<double>.TryCreate<short>(unchecked((short)0x8000), out result));
            Assert.Equal(-32768.0, result);

            Assert.True(NumberHelper<double>.TryCreate<short>(unchecked((short)0xFFFF), out result));
            Assert.Equal(-1.0, result);
        }

        [Fact]
        public static void TryCreateFromInt32Test()
        {
            double result;

            Assert.True(NumberHelper<double>.TryCreate<int>(0x00000000, out result));
            Assert.Equal(0.0, result);

            Assert.True(NumberHelper<double>.TryCreate<int>(0x00000001, out result));
            Assert.Equal(1.0, result);

            Assert.True(NumberHelper<double>.TryCreate<int>(0x7FFFFFFF, out result));
            Assert.Equal(2147483647.0, result);

            Assert.True(NumberHelper<double>.TryCreate<int>(unchecked((int)0x80000000), out result));
            Assert.Equal(-2147483648.0, result);

            Assert.True(NumberHelper<double>.TryCreate<int>(unchecked((int)0xFFFFFFFF), out result));
            Assert.Equal(-1.0, result);
        }

        [Fact]
        public static void TryCreateFromInt64Test()
        {
            double result;

            Assert.True(NumberHelper<double>.TryCreate<long>(0x0000000000000000, out result));
            Assert.Equal(0.0, result);

            Assert.True(NumberHelper<double>.TryCreate<long>(0x0000000000000001, out result));
            Assert.Equal(1.0, result);

            Assert.True(NumberHelper<double>.TryCreate<long>(0x7FFFFFFFFFFFFFFF, out result));
            Assert.Equal(9223372036854775807.0, result);

            Assert.True(NumberHelper<double>.TryCreate<long>(unchecked(unchecked((long)0x8000000000000000)), out result));
            Assert.Equal(-9223372036854775808.0, result);

            Assert.True(NumberHelper<double>.TryCreate<long>(unchecked(unchecked((long)0xFFFFFFFFFFFFFFFF)), out result));
            Assert.Equal(-1.0, result);
        }

        [Fact]
        public static void TryCreateFromIntPtrTest()
        {
            double result;

            if (Environment.Is64BitProcess)
            {
                Assert.True(NumberHelper<double>.TryCreate<nint>(unchecked((nint)0x0000000000000000), out result));
                Assert.Equal(0.0, result);

                Assert.True(NumberHelper<double>.TryCreate<nint>(unchecked((nint)0x0000000000000001), out result));
                Assert.Equal(1.0, result);

                Assert.True(NumberHelper<double>.TryCreate<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF), out result));
                Assert.Equal(9223372036854775807.0, result);

                Assert.True(NumberHelper<double>.TryCreate<nint>(unchecked((nint)0x8000000000000000), out result));
                Assert.Equal(-9223372036854775808.0, result);

                Assert.True(NumberHelper<double>.TryCreate<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF), out result));
                Assert.Equal(-1.0, result);
            }
            else
            {
                Assert.True(NumberHelper<double>.TryCreate<nint>((nint)0x00000000, out result));
                Assert.Equal(0.0, result);

                Assert.True(NumberHelper<double>.TryCreate<nint>((nint)0x00000001, out result));
                Assert.Equal(1.0, result);

                Assert.True(NumberHelper<double>.TryCreate<nint>((nint)0x7FFFFFFF, out result));
                Assert.Equal(2147483647.0, result);

                Assert.True(NumberHelper<double>.TryCreate<nint>(unchecked((nint)0x80000000), out result));
                Assert.Equal(-2147483648.0, result);

                Assert.True(NumberHelper<double>.TryCreate<nint>(unchecked((nint)0xFFFFFFFF), out result));
                Assert.Equal(-1.0, result);
            }
        }

        [Fact]
        public static void TryCreateFromSByteTest()
        {
            double result;

            Assert.True(NumberHelper<double>.TryCreate<sbyte>(0x00, out result));
            Assert.Equal(0.0, result);

            Assert.True(NumberHelper<double>.TryCreate<sbyte>(0x01, out result));
            Assert.Equal(1.0, result);

            Assert.True(NumberHelper<double>.TryCreate<sbyte>(0x7F, out result));
            Assert.Equal(127.0, result);

            Assert.True(NumberHelper<double>.TryCreate<sbyte>(unchecked((sbyte)0x80), out result));
            Assert.Equal(-128.0, result);

            Assert.True(NumberHelper<double>.TryCreate<sbyte>(unchecked((sbyte)0xFF), out result));
            Assert.Equal(-1.0, result);
        }

        [Fact]
        public static void TryCreateFromUInt16Test()
        {
            double result;

            Assert.True(NumberHelper<double>.TryCreate<ushort>(0x0000, out result));
            Assert.Equal(0.0, result);

            Assert.True(NumberHelper<double>.TryCreate<ushort>(0x0001, out result));
            Assert.Equal(1.0, result);

            Assert.True(NumberHelper<double>.TryCreate<ushort>(0x7FFF, out result));
            Assert.Equal(32767.0, result);

            Assert.True(NumberHelper<double>.TryCreate<ushort>(0x8000, out result));
            Assert.Equal(32768.0, result);

            Assert.True(NumberHelper<double>.TryCreate<ushort>(0xFFFF, out result));
            Assert.Equal(65535.0, result);
        }

        [Fact]
        public static void TryCreateFromUInt32Test()
        {
            double result;

            Assert.True(NumberHelper<double>.TryCreate<uint>(0x00000000, out result));
            Assert.Equal(0.0, result);

            Assert.True(NumberHelper<double>.TryCreate<uint>(0x00000001, out result));
            Assert.Equal(1.0, result);

            Assert.True(NumberHelper<double>.TryCreate<uint>(0x7FFFFFFF, out result));
            Assert.Equal(2147483647.0, result);

            Assert.True(NumberHelper<double>.TryCreate<uint>(0x80000000, out result));
            Assert.Equal(2147483648.0, result);

            Assert.True(NumberHelper<double>.TryCreate<uint>(0xFFFFFFFF, out result));
            Assert.Equal(4294967295.0, result);
        }

        [Fact]
        public static void TryCreateFromUInt64Test()
        {
            double result;

            Assert.True(NumberHelper<double>.TryCreate<ulong>(0x0000000000000000, out result));
            Assert.Equal(0.0, result);

            Assert.True(NumberHelper<double>.TryCreate<ulong>(0x0000000000000001, out result));
            Assert.Equal(1.0, result);

            Assert.True(NumberHelper<double>.TryCreate<ulong>(0x7FFFFFFFFFFFFFFF, out result));
            Assert.Equal(9223372036854775807.0, result);

            Assert.True(NumberHelper<double>.TryCreate<ulong>(0x8000000000000000, out result));
            Assert.Equal(9223372036854775808.0, result);

            Assert.True(NumberHelper<double>.TryCreate<ulong>(0xFFFFFFFFFFFFFFFF, out result));
            Assert.Equal(18446744073709551615.0, result);
        }

        [Fact]
        public static void TryCreateFromUIntPtrTest()
        {
            double result;

            if (Environment.Is64BitProcess)
            {
                Assert.True(NumberHelper<double>.TryCreate<nuint>(unchecked((nuint)0x0000000000000000), out result));
                Assert.Equal(0.0, result);

                Assert.True(NumberHelper<double>.TryCreate<nuint>(unchecked((nuint)0x0000000000000001), out result));
                Assert.Equal(1.0, result);

                Assert.True(NumberHelper<double>.TryCreate<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF), out result));
                Assert.Equal(9223372036854775807.0, result);

                // https://github.com/dotnet/roslyn/issues/60714
                // Assert.True(NumberHelper<double>.TryCreate<nuint>(unchecked((nuint)0x8000000000000000), out result));
                // Assert.Equal(9223372036854775808.0, result);
                //
                // Assert.True(NumberHelper<double>.TryCreate<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF), out result));
                // Assert.Equal(18446744073709551615.0, result);
            }
            else
            {
                Assert.True(NumberHelper<double>.TryCreate<nuint>((nuint)0x00000000, out result));
                Assert.Equal(0.0, result);

                Assert.True(NumberHelper<double>.TryCreate<nuint>((nuint)0x00000001, out result));
                Assert.Equal(1.0, result);

                Assert.True(NumberHelper<double>.TryCreate<nuint>((nuint)0x7FFFFFFF, out result));
                Assert.Equal(2147483647.0, result);

                // https://github.com/dotnet/roslyn/issues/60714
                // Assert.True(NumberHelper<double>.TryCreate<nuint>(unchecked((nuint)0x80000000), out result));
                // Assert.Equal(2147483648.0, result);
                //
                // Assert.True(NumberHelper<double>.TryCreate<nuint>(unchecked((nuint)0xFFFFFFFF), out result));
                // Assert.Equal(4294967295.0, result);
            }
        }

        [Fact]
        public static void op_SubtractionTest()
        {
            AssertBitwiseEqual(double.NegativeInfinity, SubtractionOperatorsHelper<double, double, double>.op_Subtraction(double.NegativeInfinity, 1.0));
            AssertBitwiseEqual(double.MinValue, SubtractionOperatorsHelper<double, double, double>.op_Subtraction(double.MinValue, 1.0));
            AssertBitwiseEqual(-2.0, SubtractionOperatorsHelper<double, double, double>.op_Subtraction(-1.0, 1.0));
            AssertBitwiseEqual(-1.0, SubtractionOperatorsHelper<double, double, double>.op_Subtraction(-MinNormal, 1.0));
            AssertBitwiseEqual(-1.0, SubtractionOperatorsHelper<double, double, double>.op_Subtraction(-MaxSubnormal, 1.0));
            AssertBitwiseEqual(-1.0, SubtractionOperatorsHelper<double, double, double>.op_Subtraction(-double.Epsilon, 1.0));
            AssertBitwiseEqual(-1.0, SubtractionOperatorsHelper<double, double, double>.op_Subtraction(-0.0, 1.0));
            AssertBitwiseEqual(double.NaN, SubtractionOperatorsHelper<double, double, double>.op_Subtraction(double.NaN, 1.0));
            AssertBitwiseEqual(-1.0, SubtractionOperatorsHelper<double, double, double>.op_Subtraction(0.0, 1.0));
            AssertBitwiseEqual(-1.0, SubtractionOperatorsHelper<double, double, double>.op_Subtraction(double.Epsilon, 1.0));
            AssertBitwiseEqual(-1.0, SubtractionOperatorsHelper<double, double, double>.op_Subtraction(MaxSubnormal, 1.0));
            AssertBitwiseEqual(-1.0, SubtractionOperatorsHelper<double, double, double>.op_Subtraction(MinNormal, 1.0));
            AssertBitwiseEqual(0.0, SubtractionOperatorsHelper<double, double, double>.op_Subtraction(1.0, 1.0));
            AssertBitwiseEqual(double.MaxValue, SubtractionOperatorsHelper<double, double, double>.op_Subtraction(double.MaxValue, 1.0));
            AssertBitwiseEqual(double.PositiveInfinity, SubtractionOperatorsHelper<double, double, double>.op_Subtraction(double.PositiveInfinity, 1.0));
        }

        [Fact]
        public static void op_CheckedSubtractionTest()
        {
            AssertBitwiseEqual(double.NegativeInfinity, SubtractionOperatorsHelper<double, double, double>.op_CheckedSubtraction(double.NegativeInfinity, 1.0));
            AssertBitwiseEqual(double.MinValue, SubtractionOperatorsHelper<double, double, double>.op_CheckedSubtraction(double.MinValue, 1.0));
            AssertBitwiseEqual(-2.0, SubtractionOperatorsHelper<double, double, double>.op_CheckedSubtraction(-1.0, 1.0));
            AssertBitwiseEqual(-1.0, SubtractionOperatorsHelper<double, double, double>.op_CheckedSubtraction(-MinNormal, 1.0));
            AssertBitwiseEqual(-1.0, SubtractionOperatorsHelper<double, double, double>.op_CheckedSubtraction(-MaxSubnormal, 1.0));
            AssertBitwiseEqual(-1.0, SubtractionOperatorsHelper<double, double, double>.op_CheckedSubtraction(-double.Epsilon, 1.0));
            AssertBitwiseEqual(-1.0, SubtractionOperatorsHelper<double, double, double>.op_CheckedSubtraction(-0.0, 1.0));
            AssertBitwiseEqual(double.NaN, SubtractionOperatorsHelper<double, double, double>.op_CheckedSubtraction(double.NaN, 1.0));
            AssertBitwiseEqual(-1.0, SubtractionOperatorsHelper<double, double, double>.op_CheckedSubtraction(0.0, 1.0));
            AssertBitwiseEqual(-1.0, SubtractionOperatorsHelper<double, double, double>.op_CheckedSubtraction(double.Epsilon, 1.0));
            AssertBitwiseEqual(-1.0, SubtractionOperatorsHelper<double, double, double>.op_CheckedSubtraction(MaxSubnormal, 1.0));
            AssertBitwiseEqual(-1.0, SubtractionOperatorsHelper<double, double, double>.op_CheckedSubtraction(MinNormal, 1.0));
            AssertBitwiseEqual(0.0, SubtractionOperatorsHelper<double, double, double>.op_CheckedSubtraction(1.0, 1.0));
            AssertBitwiseEqual(double.MaxValue, SubtractionOperatorsHelper<double, double, double>.op_CheckedSubtraction(double.MaxValue, 1.0));
            AssertBitwiseEqual(double.PositiveInfinity, SubtractionOperatorsHelper<double, double, double>.op_CheckedSubtraction(double.PositiveInfinity, 1.0));
        }

        [Fact]
        public static void op_UnaryNegationTest()
        {
            AssertBitwiseEqual(double.PositiveInfinity, UnaryNegationOperatorsHelper<double, double>.op_UnaryNegation(double.NegativeInfinity));
            AssertBitwiseEqual(double.MaxValue, UnaryNegationOperatorsHelper<double, double>.op_UnaryNegation(double.MinValue));
            AssertBitwiseEqual(1.0, UnaryNegationOperatorsHelper<double, double>.op_UnaryNegation(-1.0));
            AssertBitwiseEqual(MinNormal, UnaryNegationOperatorsHelper<double, double>.op_UnaryNegation(-MinNormal));
            AssertBitwiseEqual(MaxSubnormal, UnaryNegationOperatorsHelper<double, double>.op_UnaryNegation(-MaxSubnormal));
            AssertBitwiseEqual(double.Epsilon, UnaryNegationOperatorsHelper<double, double>.op_UnaryNegation(-double.Epsilon));
            AssertBitwiseEqual(0.0, UnaryNegationOperatorsHelper<double, double>.op_UnaryNegation(-0.0));
            AssertBitwiseEqual(double.NaN, UnaryNegationOperatorsHelper<double, double>.op_UnaryNegation(double.NaN));
            AssertBitwiseEqual(-0.0, UnaryNegationOperatorsHelper<double, double>.op_UnaryNegation(0.0));
            AssertBitwiseEqual(-double.Epsilon, UnaryNegationOperatorsHelper<double, double>.op_UnaryNegation(double.Epsilon));
            AssertBitwiseEqual(-MaxSubnormal, UnaryNegationOperatorsHelper<double, double>.op_UnaryNegation(MaxSubnormal));
            AssertBitwiseEqual(-MinNormal, UnaryNegationOperatorsHelper<double, double>.op_UnaryNegation(MinNormal));
            AssertBitwiseEqual(-1.0, UnaryNegationOperatorsHelper<double, double>.op_UnaryNegation(1.0));
            AssertBitwiseEqual(double.MinValue, UnaryNegationOperatorsHelper<double, double>.op_UnaryNegation(double.MaxValue));
            AssertBitwiseEqual(double.NegativeInfinity, UnaryNegationOperatorsHelper<double, double>.op_UnaryNegation(double.PositiveInfinity));
        }

        [Fact]
        public static void op_CheckedUnaryNegationTest()
        {
            AssertBitwiseEqual(double.PositiveInfinity, UnaryNegationOperatorsHelper<double, double>.op_CheckedUnaryNegation(double.NegativeInfinity));
            AssertBitwiseEqual(double.MaxValue, UnaryNegationOperatorsHelper<double, double>.op_CheckedUnaryNegation(double.MinValue));
            AssertBitwiseEqual(1.0, UnaryNegationOperatorsHelper<double, double>.op_CheckedUnaryNegation(-1.0));
            AssertBitwiseEqual(MinNormal, UnaryNegationOperatorsHelper<double, double>.op_CheckedUnaryNegation(-MinNormal));
            AssertBitwiseEqual(MaxSubnormal, UnaryNegationOperatorsHelper<double, double>.op_CheckedUnaryNegation(-MaxSubnormal));
            AssertBitwiseEqual(double.Epsilon, UnaryNegationOperatorsHelper<double, double>.op_CheckedUnaryNegation(-double.Epsilon));
            AssertBitwiseEqual(0.0, UnaryNegationOperatorsHelper<double, double>.op_CheckedUnaryNegation(-0.0));
            AssertBitwiseEqual(double.NaN, UnaryNegationOperatorsHelper<double, double>.op_CheckedUnaryNegation(double.NaN));
            AssertBitwiseEqual(-0.0, UnaryNegationOperatorsHelper<double, double>.op_CheckedUnaryNegation(0.0));
            AssertBitwiseEqual(-double.Epsilon, UnaryNegationOperatorsHelper<double, double>.op_CheckedUnaryNegation(double.Epsilon));
            AssertBitwiseEqual(-MaxSubnormal, UnaryNegationOperatorsHelper<double, double>.op_CheckedUnaryNegation(MaxSubnormal));
            AssertBitwiseEqual(-MinNormal, UnaryNegationOperatorsHelper<double, double>.op_CheckedUnaryNegation(MinNormal));
            AssertBitwiseEqual(-1.0, UnaryNegationOperatorsHelper<double, double>.op_CheckedUnaryNegation(1.0));
            AssertBitwiseEqual(double.MinValue, UnaryNegationOperatorsHelper<double, double>.op_CheckedUnaryNegation(double.MaxValue));
            AssertBitwiseEqual(double.NegativeInfinity, UnaryNegationOperatorsHelper<double, double>.op_CheckedUnaryNegation(double.PositiveInfinity));
        }

        [Fact]
        public static void op_UnaryPlusTest()
        {
            AssertBitwiseEqual(double.NegativeInfinity, UnaryPlusOperatorsHelper<double, double>.op_UnaryPlus(double.NegativeInfinity));
            AssertBitwiseEqual(double.MinValue, UnaryPlusOperatorsHelper<double, double>.op_UnaryPlus(double.MinValue));
            AssertBitwiseEqual(-1.0, UnaryPlusOperatorsHelper<double, double>.op_UnaryPlus(-1.0));
            AssertBitwiseEqual(-MinNormal, UnaryPlusOperatorsHelper<double, double>.op_UnaryPlus(-MinNormal));
            AssertBitwiseEqual(-MaxSubnormal, UnaryPlusOperatorsHelper<double, double>.op_UnaryPlus(-MaxSubnormal));
            AssertBitwiseEqual(-double.Epsilon, UnaryPlusOperatorsHelper<double, double>.op_UnaryPlus(-double.Epsilon));
            AssertBitwiseEqual(-0.0, UnaryPlusOperatorsHelper<double, double>.op_UnaryPlus(-0.0));
            AssertBitwiseEqual(double.NaN, UnaryPlusOperatorsHelper<double, double>.op_UnaryPlus(double.NaN));
            AssertBitwiseEqual(0.0, UnaryPlusOperatorsHelper<double, double>.op_UnaryPlus(0.0));
            AssertBitwiseEqual(double.Epsilon, UnaryPlusOperatorsHelper<double, double>.op_UnaryPlus(double.Epsilon));
            AssertBitwiseEqual(MaxSubnormal, UnaryPlusOperatorsHelper<double, double>.op_UnaryPlus(MaxSubnormal));
            AssertBitwiseEqual(MinNormal, UnaryPlusOperatorsHelper<double, double>.op_UnaryPlus(MinNormal));
            AssertBitwiseEqual(1.0, UnaryPlusOperatorsHelper<double, double>.op_UnaryPlus(1.0));
            AssertBitwiseEqual(double.MaxValue, UnaryPlusOperatorsHelper<double, double>.op_UnaryPlus(double.MaxValue));
            AssertBitwiseEqual(double.PositiveInfinity, UnaryPlusOperatorsHelper<double, double>.op_UnaryPlus(double.PositiveInfinity));
        }

        [Theory]
        [MemberData(nameof(DoubleTests.Parse_Valid_TestData), MemberType = typeof(DoubleTests))]
        public static void ParseValidStringTest(string value, NumberStyles style, IFormatProvider provider, double expected)
        {
            bool isDefaultProvider = provider == null || provider == NumberFormatInfo.CurrentInfo;
            double result;
            if ((style & ~(NumberStyles.Float | NumberStyles.AllowThousands)) == 0 && style != NumberStyles.None)
            {
                // Use Parse(string) or Parse(string, IFormatProvider)
                if (isDefaultProvider)
                {
                    Assert.True(NumberHelper<double>.TryParse(value, null, out result));
                    Assert.Equal(expected, result);

                    Assert.Equal(expected, NumberHelper<double>.Parse(value, null));
                }

                Assert.Equal(expected, NumberHelper<double>.Parse(value, provider));
            }

            // Use Parse(string, NumberStyles, IFormatProvider)
            Assert.True(NumberHelper<double>.TryParse(value, style, provider, out result));
            Assert.Equal(expected, result);

            Assert.Equal(expected, NumberHelper<double>.Parse(value, style, provider));

            if (isDefaultProvider)
            {
                // Use Parse(string, NumberStyles) or Parse(string, NumberStyles, IFormatProvider)
                Assert.True(NumberHelper<double>.TryParse(value, style, NumberFormatInfo.CurrentInfo, out result));
                Assert.Equal(expected, result);

                Assert.Equal(expected, NumberHelper<double>.Parse(value, style, null));
                Assert.Equal(expected, NumberHelper<double>.Parse(value, style, NumberFormatInfo.CurrentInfo));
            }
        }

        [Theory]
        [MemberData(nameof(DoubleTests.Parse_Invalid_TestData), MemberType = typeof(DoubleTests))]
        public static void ParseInvalidStringTest(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            bool isDefaultProvider = provider == null || provider == NumberFormatInfo.CurrentInfo;
            double result;
            if ((style & ~(NumberStyles.Float | NumberStyles.AllowThousands)) == 0 && style != NumberStyles.None && (style & NumberStyles.AllowLeadingWhite) == (style & NumberStyles.AllowTrailingWhite))
            {
                // Use Parse(string) or Parse(string, IFormatProvider)
                if (isDefaultProvider)
                {
                    Assert.False(NumberHelper<double>.TryParse(value, null, out result));
                    Assert.Equal(default(double), result);

                    Assert.Throws(exceptionType, () => NumberHelper<double>.Parse(value, null));
                }

                Assert.Throws(exceptionType, () => NumberHelper<double>.Parse(value, provider));
            }

            // Use Parse(string, NumberStyles, IFormatProvider)
            Assert.False(NumberHelper<double>.TryParse(value, style, provider, out result));
            Assert.Equal(default(double), result);

            Assert.Throws(exceptionType, () => NumberHelper<double>.Parse(value, style, provider));

            if (isDefaultProvider)
            {
                // Use Parse(string, NumberStyles) or Parse(string, NumberStyles, IFormatProvider)
                Assert.False(NumberHelper<double>.TryParse(value, style, NumberFormatInfo.CurrentInfo, out result));
                Assert.Equal(default(double), result);

                Assert.Throws(exceptionType, () => NumberHelper<double>.Parse(value, style, null));
                Assert.Throws(exceptionType, () => NumberHelper<double>.Parse(value, style, NumberFormatInfo.CurrentInfo));
            }
        }

        [Theory]
        [MemberData(nameof(DoubleTests.Parse_ValidWithOffsetCount_TestData), MemberType = typeof(DoubleTests))]
        public static void ParseValidSpanTest(string value, int offset, int count, NumberStyles style, IFormatProvider provider, double expected)
        {
            bool isDefaultProvider = provider == null || provider == NumberFormatInfo.CurrentInfo;
            double result;
            if ((style & ~(NumberStyles.Float | NumberStyles.AllowThousands)) == 0 && style != NumberStyles.None)
            {
                // Use Parse(string) or Parse(string, IFormatProvider)
                if (isDefaultProvider)
                {
                    Assert.True(NumberHelper<double>.TryParse(value.AsSpan(offset, count), null, out result));
                    Assert.Equal(expected, result);

                    Assert.Equal(expected, NumberHelper<double>.Parse(value.AsSpan(offset, count), null));
                }

                Assert.Equal(expected, NumberHelper<double>.Parse(value.AsSpan(offset, count), provider: provider));
            }

            Assert.Equal(expected, NumberHelper<double>.Parse(value.AsSpan(offset, count), style, provider));

            Assert.True(NumberHelper<double>.TryParse(value.AsSpan(offset, count), style, provider, out result));
            Assert.Equal(expected, result);
        }

        [Theory]
        [MemberData(nameof(DoubleTests.Parse_Invalid_TestData), MemberType = typeof(DoubleTests))]
        public static void ParseInvalidSpanTest(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            if (value != null)
            {
                Assert.Throws(exceptionType, () => NumberHelper<double>.Parse(value.AsSpan(), style, provider));

                Assert.False(NumberHelper<double>.TryParse(value.AsSpan(), style, provider, out double result));
                Assert.Equal(0, result);
            }
        }
    }
}
