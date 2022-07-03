// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Tests
{
    public class DoubleTests_GenericMath
    {
        internal const double MinNormal = 2.2250738585072014E-308;

        internal const double MaxSubnormal = 2.2250738585072009E-308;

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

        //
        // IAdditionOperators
        //

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

        //
        // IAdditiveIdentity
        //

        [Fact]
        public static void AdditiveIdentityTest()
        {
            AssertBitwiseEqual(0.0, AdditiveIdentityHelper<double, double>.AdditiveIdentity);
        }

        //
        // IBinaryNumber
        //

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

        //
        // IComparisonOperators
        //

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

        //
        // IDecrementOperators
        //

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

        //
        // IDivisionOperators
        //

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

        //
        // IEqualityOperators
        //

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

        //
        // IFloatingPoint
        //

        [Fact]
        public static void GetExponentByteCountTest()
        {
            Assert.Equal(2, FloatingPointHelper<double>.GetExponentByteCount(double.NegativeInfinity));
            Assert.Equal(2, FloatingPointHelper<double>.GetExponentByteCount(double.MinValue));
            Assert.Equal(2, FloatingPointHelper<double>.GetExponentByteCount(-1.0));
            Assert.Equal(2, FloatingPointHelper<double>.GetExponentByteCount(-MinNormal));
            Assert.Equal(2, FloatingPointHelper<double>.GetExponentByteCount(-MaxSubnormal));
            Assert.Equal(2, FloatingPointHelper<double>.GetExponentByteCount(-double.Epsilon));
            Assert.Equal(2, FloatingPointHelper<double>.GetExponentByteCount(-0.0));
            Assert.Equal(2, FloatingPointHelper<double>.GetExponentByteCount(double.NaN));
            Assert.Equal(2, FloatingPointHelper<double>.GetExponentByteCount(0.0));
            Assert.Equal(2, FloatingPointHelper<double>.GetExponentByteCount(double.Epsilon));
            Assert.Equal(2, FloatingPointHelper<double>.GetExponentByteCount(MaxSubnormal));
            Assert.Equal(2, FloatingPointHelper<double>.GetExponentByteCount(MinNormal));
            Assert.Equal(2, FloatingPointHelper<double>.GetExponentByteCount(1.0));
            Assert.Equal(2, FloatingPointHelper<double>.GetExponentByteCount(double.MaxValue));
            Assert.Equal(2, FloatingPointHelper<double>.GetExponentByteCount(double.PositiveInfinity));
        }

        [Fact]
        public static void GetExponentShortestBitLengthTest()
        {
            Assert.Equal(11, FloatingPointHelper<double>.GetExponentShortestBitLength(double.NegativeInfinity));
            Assert.Equal(10, FloatingPointHelper<double>.GetExponentShortestBitLength(double.MinValue));
            Assert.Equal(0, FloatingPointHelper<double>.GetExponentShortestBitLength(-1.0));
            Assert.Equal(11, FloatingPointHelper<double>.GetExponentShortestBitLength(-MinNormal));
            Assert.Equal(11, FloatingPointHelper<double>.GetExponentShortestBitLength(-MaxSubnormal));
            Assert.Equal(11, FloatingPointHelper<double>.GetExponentShortestBitLength(-double.Epsilon));
            Assert.Equal(11, FloatingPointHelper<double>.GetExponentShortestBitLength(-0.0));
            Assert.Equal(11, FloatingPointHelper<double>.GetExponentShortestBitLength(double.NaN));
            Assert.Equal(11, FloatingPointHelper<double>.GetExponentShortestBitLength(0.0));
            Assert.Equal(11, FloatingPointHelper<double>.GetExponentShortestBitLength(double.Epsilon));
            Assert.Equal(11, FloatingPointHelper<double>.GetExponentShortestBitLength(MaxSubnormal));
            Assert.Equal(11, FloatingPointHelper<double>.GetExponentShortestBitLength(MinNormal));
            Assert.Equal(0, FloatingPointHelper<double>.GetExponentShortestBitLength(1.0));
            Assert.Equal(10, FloatingPointHelper<double>.GetExponentShortestBitLength(double.MaxValue));
            Assert.Equal(11, FloatingPointHelper<double>.GetExponentShortestBitLength(double.PositiveInfinity));
        }

        [Fact]
        public static void GetSignificandByteCountTest()
        {
            Assert.Equal(8, FloatingPointHelper<double>.GetSignificandByteCount(double.NegativeInfinity));
            Assert.Equal(8, FloatingPointHelper<double>.GetSignificandByteCount(double.MinValue));
            Assert.Equal(8, FloatingPointHelper<double>.GetSignificandByteCount(-1.0));
            Assert.Equal(8, FloatingPointHelper<double>.GetSignificandByteCount(-MinNormal));
            Assert.Equal(8, FloatingPointHelper<double>.GetSignificandByteCount(-MaxSubnormal));
            Assert.Equal(8, FloatingPointHelper<double>.GetSignificandByteCount(-double.Epsilon));
            Assert.Equal(8, FloatingPointHelper<double>.GetSignificandByteCount(-0.0));
            Assert.Equal(8, FloatingPointHelper<double>.GetSignificandByteCount(double.NaN));
            Assert.Equal(8, FloatingPointHelper<double>.GetSignificandByteCount(0.0));
            Assert.Equal(8, FloatingPointHelper<double>.GetSignificandByteCount(double.Epsilon));
            Assert.Equal(8, FloatingPointHelper<double>.GetSignificandByteCount(MaxSubnormal));
            Assert.Equal(8, FloatingPointHelper<double>.GetSignificandByteCount(MinNormal));
            Assert.Equal(8, FloatingPointHelper<double>.GetSignificandByteCount(1.0));
            Assert.Equal(8, FloatingPointHelper<double>.GetSignificandByteCount(double.MaxValue));
            Assert.Equal(8, FloatingPointHelper<double>.GetSignificandByteCount(double.PositiveInfinity));
        }

        [Fact]
        public static void GetSignificandBitLengthTest()
        {
            Assert.Equal(53, FloatingPointHelper<double>.GetSignificandBitLength(double.NegativeInfinity));
            Assert.Equal(53, FloatingPointHelper<double>.GetSignificandBitLength(double.MinValue));
            Assert.Equal(53, FloatingPointHelper<double>.GetSignificandBitLength(-1.0));
            Assert.Equal(53, FloatingPointHelper<double>.GetSignificandBitLength(-MinNormal));
            Assert.Equal(53, FloatingPointHelper<double>.GetSignificandBitLength(-MaxSubnormal));
            Assert.Equal(53, FloatingPointHelper<double>.GetSignificandBitLength(-double.Epsilon));
            Assert.Equal(53, FloatingPointHelper<double>.GetSignificandBitLength(-0.0));
            Assert.Equal(53, FloatingPointHelper<double>.GetSignificandBitLength(double.NaN));
            Assert.Equal(53, FloatingPointHelper<double>.GetSignificandBitLength(0.0));
            Assert.Equal(53, FloatingPointHelper<double>.GetSignificandBitLength(double.Epsilon));
            Assert.Equal(53, FloatingPointHelper<double>.GetSignificandBitLength(MaxSubnormal));
            Assert.Equal(53, FloatingPointHelper<double>.GetSignificandBitLength(MinNormal));
            Assert.Equal(53, FloatingPointHelper<double>.GetSignificandBitLength(1.0));
            Assert.Equal(53, FloatingPointHelper<double>.GetSignificandBitLength(double.MaxValue));
            Assert.Equal(53, FloatingPointHelper<double>.GetSignificandBitLength(double.PositiveInfinity));
        }

        [Fact]
        public static void TryWriteExponentBigEndianTest()
        {
            Span<byte> destination = stackalloc byte[2];
            int bytesWritten = 0;

            Assert.True(FloatingPointHelper<double>.TryWriteExponentBigEndian(double.NegativeInfinity, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0x04, 0x00 }, destination.ToArray()); // +1024

            Assert.True(FloatingPointHelper<double>.TryWriteExponentBigEndian(double.MinValue, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0x03, 0xFF }, destination.ToArray()); // +1023

            Assert.True(FloatingPointHelper<double>.TryWriteExponentBigEndian(-1.0, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x00 }, destination.ToArray()); // +0

            Assert.True(FloatingPointHelper<double>.TryWriteExponentBigEndian(-MinNormal, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0xFC, 0x02 }, destination.ToArray()); // -1022

            Assert.True(FloatingPointHelper<double>.TryWriteExponentBigEndian(-MaxSubnormal, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0xFC, 0x01 }, destination.ToArray()); // -1023

            Assert.True(FloatingPointHelper<double>.TryWriteExponentBigEndian(-double.Epsilon, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0xFC, 0x01 }, destination.ToArray()); // -1023

            Assert.True(FloatingPointHelper<double>.TryWriteExponentBigEndian(-0.0, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0xFC, 0x01 }, destination.ToArray()); // -1023

            Assert.True(FloatingPointHelper<double>.TryWriteExponentBigEndian(double.NaN, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0x04, 0x00 }, destination.ToArray()); // +1024

            Assert.True(FloatingPointHelper<double>.TryWriteExponentBigEndian(0.0, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0xFC, 0x01 }, destination.ToArray()); // -1023

            Assert.True(FloatingPointHelper<double>.TryWriteExponentBigEndian(double.Epsilon, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0xFC, 0x01 }, destination.ToArray()); // -1023

            Assert.True(FloatingPointHelper<double>.TryWriteExponentBigEndian(MaxSubnormal, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0xFC, 0x01 }, destination.ToArray()); // -1023

            Assert.True(FloatingPointHelper<double>.TryWriteExponentBigEndian(MinNormal, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0xFC, 0x02 }, destination.ToArray()); // -1022

            Assert.True(FloatingPointHelper<double>.TryWriteExponentBigEndian(1.0, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x00 }, destination.ToArray()); // +0

            Assert.True(FloatingPointHelper<double>.TryWriteExponentBigEndian(double.MaxValue, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0x03, 0xff }, destination.ToArray()); // +1023

            Assert.True(FloatingPointHelper<double>.TryWriteExponentBigEndian(double.PositiveInfinity, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0x04, 0x00 }, destination.ToArray()); // +1024

            Assert.False(FloatingPointHelper<double>.TryWriteExponentBigEndian(default, Span<byte>.Empty, out bytesWritten));
            Assert.Equal(0, bytesWritten);
            Assert.Equal(new byte[] { 0x04, 0x00 }, destination.ToArray());
        }

        [Fact]
        public static void TryWriteExponentLittleEndianTest()
        {
            Span<byte> destination = stackalloc byte[2];
            int bytesWritten = 0;

            Assert.True(FloatingPointHelper<double>.TryWriteExponentLittleEndian(double.NegativeInfinity, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x04 }, destination.ToArray()); // +1024

            Assert.True(FloatingPointHelper<double>.TryWriteExponentLittleEndian(double.MinValue, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0x03 }, destination.ToArray()); // +1023

            Assert.True(FloatingPointHelper<double>.TryWriteExponentLittleEndian(-1.0, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x00 }, destination.ToArray()); // +0

            Assert.True(FloatingPointHelper<double>.TryWriteExponentLittleEndian(-MinNormal, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0x02, 0xFC }, destination.ToArray()); // -1022

            Assert.True(FloatingPointHelper<double>.TryWriteExponentLittleEndian(-MaxSubnormal, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0x01, 0xFC }, destination.ToArray()); // -1023

            Assert.True(FloatingPointHelper<double>.TryWriteExponentLittleEndian(-double.Epsilon, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0x01, 0xFC }, destination.ToArray()); // -1023

            Assert.True(FloatingPointHelper<double>.TryWriteExponentLittleEndian(-0.0, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0x01, 0xFC }, destination.ToArray()); // -1023

            Assert.True(FloatingPointHelper<double>.TryWriteExponentLittleEndian(double.NaN, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x04 }, destination.ToArray()); // +1024

            Assert.True(FloatingPointHelper<double>.TryWriteExponentLittleEndian(0.0, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0x01, 0xFC }, destination.ToArray()); // -1023

            Assert.True(FloatingPointHelper<double>.TryWriteExponentLittleEndian(double.Epsilon, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0x01, 0xFC }, destination.ToArray()); // -1023

            Assert.True(FloatingPointHelper<double>.TryWriteExponentLittleEndian(MaxSubnormal, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0x01, 0xFC }, destination.ToArray()); // -1023

            Assert.True(FloatingPointHelper<double>.TryWriteExponentLittleEndian(MinNormal, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0x02, 0xFC }, destination.ToArray()); // -1022

            Assert.True(FloatingPointHelper<double>.TryWriteExponentLittleEndian(1.0, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x00 }, destination.ToArray()); // +0

            Assert.True(FloatingPointHelper<double>.TryWriteExponentLittleEndian(double.MaxValue, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0x03 }, destination.ToArray()); // +1023

            Assert.True(FloatingPointHelper<double>.TryWriteExponentLittleEndian(double.PositiveInfinity, destination, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x04 }, destination.ToArray()); // +1024

            Assert.False(FloatingPointHelper<double>.TryWriteExponentLittleEndian(default, Span<byte>.Empty, out bytesWritten));
            Assert.Equal(0, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x04 }, destination.ToArray());
        }

        [Fact]
        public static void TryWriteSignificandBigEndianTest()
        {
            Span<byte> destination = stackalloc byte[8];
            int bytesWritten = 0;

            Assert.True(FloatingPointHelper<double>.TryWriteSignificandBigEndian(double.NegativeInfinity, destination, out bytesWritten));
            Assert.Equal(8, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

            Assert.True(FloatingPointHelper<double>.TryWriteSignificandBigEndian(double.MinValue, destination, out bytesWritten));
            Assert.Equal(8, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x1F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, destination.ToArray());

            Assert.True(FloatingPointHelper<double>.TryWriteSignificandBigEndian(-1.0, destination, out bytesWritten));
            Assert.Equal(8, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

            Assert.True(FloatingPointHelper<double>.TryWriteSignificandBigEndian(-MinNormal, destination, out bytesWritten));
            Assert.Equal(8, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

            Assert.True(FloatingPointHelper<double>.TryWriteSignificandBigEndian(-MaxSubnormal, destination, out bytesWritten));
            Assert.Equal(8, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x0F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, destination.ToArray());

            Assert.True(FloatingPointHelper<double>.TryWriteSignificandBigEndian(-double.Epsilon, destination, out bytesWritten));
            Assert.Equal(8, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, destination.ToArray());

            Assert.True(FloatingPointHelper<double>.TryWriteSignificandBigEndian(-0.0, destination, out bytesWritten));
            Assert.Equal(8, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

            Assert.True(FloatingPointHelper<double>.TryWriteSignificandBigEndian(double.NaN, destination, out bytesWritten));
            Assert.Equal(8, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x18, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

            Assert.True(FloatingPointHelper<double>.TryWriteSignificandBigEndian(0.0, destination, out bytesWritten));
            Assert.Equal(8, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

            Assert.True(FloatingPointHelper<double>.TryWriteSignificandBigEndian(double.Epsilon, destination, out bytesWritten));
            Assert.Equal(8, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, destination.ToArray());

            Assert.True(FloatingPointHelper<double>.TryWriteSignificandBigEndian(MaxSubnormal, destination, out bytesWritten));
            Assert.Equal(8, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x0F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, destination.ToArray());

            Assert.True(FloatingPointHelper<double>.TryWriteSignificandBigEndian(MinNormal, destination, out bytesWritten));
            Assert.Equal(8, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

            Assert.True(FloatingPointHelper<double>.TryWriteSignificandBigEndian(1.0, destination, out bytesWritten));
            Assert.Equal(8, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

            Assert.True(FloatingPointHelper<double>.TryWriteSignificandBigEndian(double.MaxValue, destination, out bytesWritten));
            Assert.Equal(8, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x1F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, destination.ToArray());

            Assert.True(FloatingPointHelper<double>.TryWriteSignificandBigEndian(double.PositiveInfinity, destination, out bytesWritten));
            Assert.Equal(8, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

            Assert.False(FloatingPointHelper<double>.TryWriteSignificandBigEndian(default, Span<byte>.Empty, out bytesWritten));
            Assert.Equal(0, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());
        }

        [Fact]
        public static void TryWriteSignificandLittleEndianTest()
        {
            Span<byte> destination = stackalloc byte[8];
            int bytesWritten = 0;

            Assert.True(FloatingPointHelper<double>.TryWriteSignificandLittleEndian(double.NegativeInfinity, destination, out bytesWritten));
            Assert.Equal(8, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x10, 0x00 }, destination.ToArray());

            Assert.True(FloatingPointHelper<double>.TryWriteSignificandLittleEndian(double.MinValue, destination, out bytesWritten));
            Assert.Equal(8, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x1F, 0x00 }, destination.ToArray());

            Assert.True(FloatingPointHelper<double>.TryWriteSignificandLittleEndian(-1.0, destination, out bytesWritten));
            Assert.Equal(8, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x10, 0x00 }, destination.ToArray());

            Assert.True(FloatingPointHelper<double>.TryWriteSignificandLittleEndian(-MinNormal, destination, out bytesWritten));
            Assert.Equal(8, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x10, 0x00 }, destination.ToArray());

            Assert.True(FloatingPointHelper<double>.TryWriteSignificandLittleEndian(-MaxSubnormal, destination, out bytesWritten));
            Assert.Equal(8, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x0F, 0x00 }, destination.ToArray());

            Assert.True(FloatingPointHelper<double>.TryWriteSignificandLittleEndian(-double.Epsilon, destination, out bytesWritten));
            Assert.Equal(8, bytesWritten);
            Assert.Equal(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

            Assert.True(FloatingPointHelper<double>.TryWriteSignificandLittleEndian(-0.0, destination, out bytesWritten));
            Assert.Equal(8, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

            Assert.True(FloatingPointHelper<double>.TryWriteSignificandLittleEndian(double.NaN, destination, out bytesWritten));
            Assert.Equal(8, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x18, 0x00 }, destination.ToArray());

            Assert.True(FloatingPointHelper<double>.TryWriteSignificandLittleEndian(0.0, destination, out bytesWritten));
            Assert.Equal(8, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

            Assert.True(FloatingPointHelper<double>.TryWriteSignificandLittleEndian(double.Epsilon, destination, out bytesWritten));
            Assert.Equal(8, bytesWritten);
            Assert.Equal(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

            Assert.True(FloatingPointHelper<double>.TryWriteSignificandLittleEndian(MaxSubnormal, destination, out bytesWritten));
            Assert.Equal(8, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x0F, 0x00 }, destination.ToArray());

            Assert.True(FloatingPointHelper<double>.TryWriteSignificandLittleEndian(MinNormal, destination, out bytesWritten));
            Assert.Equal(8, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x10, 0x00 }, destination.ToArray());

            Assert.True(FloatingPointHelper<double>.TryWriteSignificandLittleEndian(1.0, destination, out bytesWritten));
            Assert.Equal(8, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x10, 0x00 }, destination.ToArray());

            Assert.True(FloatingPointHelper<double>.TryWriteSignificandLittleEndian(double.MaxValue, destination, out bytesWritten));
            Assert.Equal(8, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x1F, 0x00 }, destination.ToArray());

            Assert.True(FloatingPointHelper<double>.TryWriteSignificandLittleEndian(double.PositiveInfinity, destination, out bytesWritten));
            Assert.Equal(8, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x10, 0x00 }, destination.ToArray());

            Assert.False(FloatingPointHelper<double>.TryWriteSignificandLittleEndian(default, Span<byte>.Empty, out bytesWritten));
            Assert.Equal(0, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x10, 0x00 }, destination.ToArray());
        }

        //
        // IIncrementOperators
        //

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

        //
        // IMinMaxValue
        //

        [Fact]
        public static void MaxValueTest()
        {
            AssertBitwiseEqual(double.MaxValue, MinMaxValueHelper<double>.MaxValue);
        }

        [Fact]
        public static void MinValueTest()
        {
            AssertBitwiseEqual(double.MinValue, MinMaxValueHelper<double>.MinValue);
        }

        //
        // IModulusOperators
        //

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

        //
        // IMultiplicativeIdentity
        //

        [Fact]
        public static void MultiplicativeIdentityTest()
        {
            AssertBitwiseEqual(1.0, MultiplicativeIdentityHelper<double, double>.MultiplicativeIdentity);
        }

        //
        // IMultiplyOperators
        //

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

        //
        // INumber
        //

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
        public static void MaxNumberTest()
        {
            AssertBitwiseEqual(1.0, NumberHelper<double>.MaxNumber(double.NegativeInfinity, 1.0));
            AssertBitwiseEqual(1.0, NumberHelper<double>.MaxNumber(double.MinValue, 1.0));
            AssertBitwiseEqual(1.0, NumberHelper<double>.MaxNumber(-1.0, 1.0));
            AssertBitwiseEqual(1.0, NumberHelper<double>.MaxNumber(-MinNormal, 1.0));
            AssertBitwiseEqual(1.0, NumberHelper<double>.MaxNumber(-MaxSubnormal, 1.0));
            AssertBitwiseEqual(1.0, NumberHelper<double>.MaxNumber(-double.Epsilon, 1.0));
            AssertBitwiseEqual(1.0, NumberHelper<double>.MaxNumber(-0.0, 1.0));
            AssertBitwiseEqual(1.0, NumberHelper<double>.MaxNumber(double.NaN, 1.0));
            AssertBitwiseEqual(1.0, NumberHelper<double>.MaxNumber(0.0, 1.0));
            AssertBitwiseEqual(1.0, NumberHelper<double>.MaxNumber(double.Epsilon, 1.0));
            AssertBitwiseEqual(1.0, NumberHelper<double>.MaxNumber(MaxSubnormal, 1.0));
            AssertBitwiseEqual(1.0, NumberHelper<double>.MaxNumber(MinNormal, 1.0));
            AssertBitwiseEqual(1.0, NumberHelper<double>.MaxNumber(1.0, 1.0));
            AssertBitwiseEqual(double.MaxValue, NumberHelper<double>.MaxNumber(double.MaxValue, 1.0));
            AssertBitwiseEqual(double.PositiveInfinity, NumberHelper<double>.MaxNumber(double.PositiveInfinity, 1.0));
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
        public static void MinNumberTest()
        {
            AssertBitwiseEqual(double.NegativeInfinity, NumberHelper<double>.MinNumber(double.NegativeInfinity, 1.0));
            AssertBitwiseEqual(double.MinValue, NumberHelper<double>.MinNumber(double.MinValue, 1.0));
            AssertBitwiseEqual(-1.0, NumberHelper<double>.MinNumber(-1.0, 1.0));
            AssertBitwiseEqual(-MinNormal, NumberHelper<double>.MinNumber(-MinNormal, 1.0));
            AssertBitwiseEqual(-MaxSubnormal, NumberHelper<double>.MinNumber(-MaxSubnormal, 1.0));
            AssertBitwiseEqual(-double.Epsilon, NumberHelper<double>.MinNumber(-double.Epsilon, 1.0));
            AssertBitwiseEqual(-0.0, NumberHelper<double>.MinNumber(-0.0, 1.0));
            AssertBitwiseEqual(1.0, NumberHelper<double>.MinNumber(double.NaN, 1.0));
            AssertBitwiseEqual(0.0, NumberHelper<double>.MinNumber(0.0, 1.0));
            AssertBitwiseEqual(double.Epsilon, NumberHelper<double>.MinNumber(double.Epsilon, 1.0));
            AssertBitwiseEqual(MaxSubnormal, NumberHelper<double>.MinNumber(MaxSubnormal, 1.0));
            AssertBitwiseEqual(MinNormal, NumberHelper<double>.MinNumber(MinNormal, 1.0));
            AssertBitwiseEqual(1.0, NumberHelper<double>.MinNumber(1.0, 1.0));
            AssertBitwiseEqual(1.0, NumberHelper<double>.MinNumber(double.MaxValue, 1.0));
            AssertBitwiseEqual(1.0, NumberHelper<double>.MinNumber(double.PositiveInfinity, 1.0));
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

        //
        // INumberBase
        //

        [Fact]
        public static void OneTest()
        {
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.One);
        }

        [Fact]
        public static void RadixTest()
        {
            Assert.Equal(2, NumberBaseHelper<double>.Radix);
        }

        [Fact]
        public static void ZeroTest()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<double>.Zero);
        }

        [Fact]
        public static void AbsTest()
        {
            AssertBitwiseEqual(double.PositiveInfinity, NumberBaseHelper<double>.Abs(double.NegativeInfinity));
            AssertBitwiseEqual(double.MaxValue, NumberBaseHelper<double>.Abs(double.MinValue));
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.Abs(-1.0));
            AssertBitwiseEqual(MinNormal, NumberBaseHelper<double>.Abs(-MinNormal));
            AssertBitwiseEqual(MaxSubnormal, NumberBaseHelper<double>.Abs(-MaxSubnormal));
            AssertBitwiseEqual(double.Epsilon, NumberBaseHelper<double>.Abs(-double.Epsilon));
            AssertBitwiseEqual(0.0, NumberBaseHelper<double>.Abs(-0.0));
            AssertBitwiseEqual(double.NaN, NumberBaseHelper<double>.Abs(double.NaN));
            AssertBitwiseEqual(0.0, NumberBaseHelper<double>.Abs(0.0));
            AssertBitwiseEqual(double.Epsilon, NumberBaseHelper<double>.Abs(double.Epsilon));
            AssertBitwiseEqual(MaxSubnormal, NumberBaseHelper<double>.Abs(MaxSubnormal));
            AssertBitwiseEqual(MinNormal, NumberBaseHelper<double>.Abs(MinNormal));
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.Abs(1.0));
            AssertBitwiseEqual(double.MaxValue, NumberBaseHelper<double>.Abs(double.MaxValue));
            AssertBitwiseEqual(double.PositiveInfinity, NumberBaseHelper<double>.Abs(double.PositiveInfinity));
        }

        [Fact]
        public static void CreateCheckedFromByteTest()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<double>.CreateChecked<byte>(0x00));
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.CreateChecked<byte>(0x01));
            AssertBitwiseEqual(127.0, NumberBaseHelper<double>.CreateChecked<byte>(0x7F));
            AssertBitwiseEqual(128.0, NumberBaseHelper<double>.CreateChecked<byte>(0x80));
            AssertBitwiseEqual(255.0, NumberBaseHelper<double>.CreateChecked<byte>(0xFF));
        }

        [Fact]
        public static void CreateCheckedFromCharTest()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<double>.CreateChecked<char>((char)0x0000));
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.CreateChecked<char>((char)0x0001));
            AssertBitwiseEqual(32767.0, NumberBaseHelper<double>.CreateChecked<char>((char)0x7FFF));
            AssertBitwiseEqual(32768.0, NumberBaseHelper<double>.CreateChecked<char>((char)0x8000));
            AssertBitwiseEqual(65535.0, NumberBaseHelper<double>.CreateChecked<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateCheckedFromDecimalTest()
        {
            AssertBitwiseEqual(-79228162514264337593543950335.0, NumberBaseHelper<double>.CreateChecked<decimal>(decimal.MinValue));
            AssertBitwiseEqual(-1.0, NumberBaseHelper<double>.CreateChecked<decimal>(-1.0m));
            AssertBitwiseEqual(-0.0, NumberBaseHelper<double>.CreateChecked<decimal>(-0.0m));
            AssertBitwiseEqual(+0.0, NumberBaseHelper<double>.CreateChecked<decimal>(+0.0m));
            AssertBitwiseEqual(+1.0, NumberBaseHelper<double>.CreateChecked<decimal>(+1.0m));
            AssertBitwiseEqual(+79228162514264337593543950335.0, NumberBaseHelper<double>.CreateChecked<decimal>(decimal.MaxValue));   
        }

        [Fact]
        public static void CreateCheckedFromDoubleTest()
        {
            AssertBitwiseEqual(double.NegativeInfinity, NumberBaseHelper<double>.CreateChecked<double>(double.NegativeInfinity));
            AssertBitwiseEqual(double.MinValue, NumberBaseHelper<double>.CreateChecked<double>(double.MinValue));

            AssertBitwiseEqual(-1.0, NumberBaseHelper<double>.CreateChecked<double>(-1.0));

            AssertBitwiseEqual(-MinNormal, NumberBaseHelper<double>.CreateChecked<double>(-MinNormal));
            AssertBitwiseEqual(-MaxSubnormal, NumberBaseHelper<double>.CreateChecked<double>(-MaxSubnormal));
            AssertBitwiseEqual(-double.Epsilon, NumberBaseHelper<double>.CreateChecked<double>(-double.Epsilon));
            AssertBitwiseEqual(-0.0, NumberBaseHelper<double>.CreateChecked<double>(-0.0));

            AssertBitwiseEqual(+0.0, NumberBaseHelper<double>.CreateChecked<double>(+0.0));
            AssertBitwiseEqual(+double.Epsilon, NumberBaseHelper<double>.CreateChecked<double>(double.Epsilon));
            AssertBitwiseEqual(+MaxSubnormal, NumberBaseHelper<double>.CreateChecked<double>(MaxSubnormal));
            AssertBitwiseEqual(+MinNormal, NumberBaseHelper<double>.CreateChecked<double>(MinNormal));

            AssertBitwiseEqual(+1.0, NumberBaseHelper<double>.CreateChecked<double>(1.0));

            AssertBitwiseEqual(double.MaxValue, NumberBaseHelper<double>.CreateChecked<double>(double.MaxValue));
            AssertBitwiseEqual(double.PositiveInfinity, NumberBaseHelper<double>.CreateChecked<double>(double.PositiveInfinity));

            AssertBitwiseEqual(double.NaN, NumberBaseHelper<double>.CreateChecked<double>(double.NaN));
        }

        [Fact]
        public static void CreateCheckedFromHalfTest()
        {
            AssertBitwiseEqual(double.NegativeInfinity, NumberBaseHelper<double>.CreateChecked<Half>(Half.NegativeInfinity));

            AssertBitwiseEqual(-65504.0, NumberBaseHelper<double>.CreateChecked<Half>(Half.MinValue));
            AssertBitwiseEqual(-1.0, NumberBaseHelper<double>.CreateChecked<Half>(Half.NegativeOne));

            AssertBitwiseEqual(-6.103515625E-05, NumberBaseHelper<double>.CreateChecked<Half>(-HalfTests_GenericMath.MinNormal));
            AssertBitwiseEqual(-6.097555160522461E-05, NumberBaseHelper<double>.CreateChecked<Half>(-HalfTests_GenericMath.MaxSubnormal));
            AssertBitwiseEqual(-5.960464477539063E-08, NumberBaseHelper<double>.CreateChecked<Half>(-Half.Epsilon));
            AssertBitwiseEqual(-0.0, NumberBaseHelper<double>.CreateChecked<Half>(Half.NegativeZero));

            AssertBitwiseEqual(+0.0, NumberBaseHelper<double>.CreateChecked<Half>(Half.Zero));
            AssertBitwiseEqual(+5.960464477539063E-08, NumberBaseHelper<double>.CreateChecked<Half>(Half.Epsilon));
            AssertBitwiseEqual(+6.097555160522461E-05, NumberBaseHelper<double>.CreateChecked<Half>(HalfTests_GenericMath.MaxSubnormal));
            AssertBitwiseEqual(+6.103515625E-05, NumberBaseHelper<double>.CreateChecked<Half>(HalfTests_GenericMath.MinNormal));

            AssertBitwiseEqual(+1.0, NumberBaseHelper<double>.CreateChecked<Half>(Half.One));
            AssertBitwiseEqual(+65504.0, NumberBaseHelper<double>.CreateChecked<Half>(Half.MaxValue));

            AssertBitwiseEqual(double.PositiveInfinity, NumberBaseHelper<double>.CreateChecked<Half>(Half.PositiveInfinity));

            AssertBitwiseEqual(double.NaN, NumberBaseHelper<double>.CreateChecked<Half>(Half.NaN));
        }

        [Fact]
        public static void CreateCheckedFromInt16Test()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<double>.CreateChecked<short>(0x0000));
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.CreateChecked<short>(0x0001));
            AssertBitwiseEqual(32767.0, NumberBaseHelper<double>.CreateChecked<short>(0x7FFF));
            AssertBitwiseEqual(-32768.0, NumberBaseHelper<double>.CreateChecked<short>(unchecked((short)0x8000)));
            AssertBitwiseEqual(-1.0, NumberBaseHelper<double>.CreateChecked<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromInt32Test()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<double>.CreateChecked<int>(0x00000000));
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.CreateChecked<int>(0x00000001));
            AssertBitwiseEqual(2147483647.0, NumberBaseHelper<double>.CreateChecked<int>(0x7FFFFFFF));
            AssertBitwiseEqual(-2147483648.0, NumberBaseHelper<double>.CreateChecked<int>(unchecked((int)0x80000000)));
            AssertBitwiseEqual(-1.0, NumberBaseHelper<double>.CreateChecked<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromInt64Test()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<double>.CreateChecked<long>(0x0000000000000000));
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.CreateChecked<long>(0x0000000000000001));
            AssertBitwiseEqual(9223372036854775807.0, NumberBaseHelper<double>.CreateChecked<long>(0x7FFFFFFFFFFFFFFF));
            AssertBitwiseEqual(-9223372036854775808.0, NumberBaseHelper<double>.CreateChecked<long>(unchecked(unchecked((long)0x8000000000000000))));
            AssertBitwiseEqual(-1.0, NumberBaseHelper<double>.CreateChecked<long>(unchecked(unchecked((long)0xFFFFFFFFFFFFFFFF))));
        }

        [Fact]
        public static void CreateCheckedFromInt128Test()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<double>.CreateChecked<Int128>(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000)));
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.CreateChecked<Int128>(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001)));
            AssertBitwiseEqual(170141183460469231731687303715884105727.0, NumberBaseHelper<double>.CreateChecked<Int128>(new Int128(0x7FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)));
            AssertBitwiseEqual(-170141183460469231731687303715884105728.0, NumberBaseHelper<double>.CreateChecked<Int128>(new Int128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000)));
            AssertBitwiseEqual(-1.0, NumberBaseHelper<double>.CreateChecked<Int128>(new Int128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)));
        }

        [Fact]
        public static void CreateCheckedFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual(0.0, NumberBaseHelper<double>.CreateChecked<nint>(unchecked((nint)0x0000000000000000)));
                AssertBitwiseEqual(1.0, NumberBaseHelper<double>.CreateChecked<nint>(unchecked((nint)0x0000000000000001)));
                AssertBitwiseEqual(9223372036854775807.0, NumberBaseHelper<double>.CreateChecked<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                AssertBitwiseEqual(-9223372036854775808.0, NumberBaseHelper<double>.CreateChecked<nint>(unchecked((nint)0x8000000000000000)));
                AssertBitwiseEqual(-1.0, NumberBaseHelper<double>.CreateChecked<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                AssertBitwiseEqual(0.0, NumberBaseHelper<double>.CreateChecked<nint>((nint)0x00000000));
                AssertBitwiseEqual(1.0, NumberBaseHelper<double>.CreateChecked<nint>((nint)0x00000001));
                AssertBitwiseEqual(2147483647.0, NumberBaseHelper<double>.CreateChecked<nint>((nint)0x7FFFFFFF));
                AssertBitwiseEqual(-2147483648.0, NumberBaseHelper<double>.CreateChecked<nint>(unchecked((nint)0x80000000)));
                AssertBitwiseEqual(-1.0, NumberBaseHelper<double>.CreateChecked<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateCheckedFromNFloatTest()
        {
            AssertBitwiseEqual(double.NegativeInfinity, NumberBaseHelper<double>.CreateChecked<NFloat>(NFloat.NegativeInfinity));

            AssertBitwiseEqual(-1.0, NumberBaseHelper<double>.CreateChecked<NFloat>(-1.0f));
            AssertBitwiseEqual(-0.0, NumberBaseHelper<double>.CreateChecked<NFloat>(-0.0f));

            AssertBitwiseEqual(+0.0, NumberBaseHelper<double>.CreateChecked<NFloat>(+0.0f));
            AssertBitwiseEqual(+1.0, NumberBaseHelper<double>.CreateChecked<NFloat>(1.0f));

            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual(double.MinValue, NumberBaseHelper<double>.CreateChecked<NFloat>(NFloat.MinValue));

                AssertBitwiseEqual(-MinNormal, NumberBaseHelper<double>.CreateChecked<NFloat>((NFloat)(-MinNormal)));
                AssertBitwiseEqual(-MaxSubnormal, NumberBaseHelper<double>.CreateChecked<NFloat>((NFloat)(-MaxSubnormal)));
                AssertBitwiseEqual(-double.Epsilon, NumberBaseHelper<double>.CreateChecked<NFloat>(-NFloat.Epsilon));

                AssertBitwiseEqual(+double.Epsilon, NumberBaseHelper<double>.CreateChecked<NFloat>(NFloat.Epsilon));
                AssertBitwiseEqual(+MaxSubnormal, NumberBaseHelper<double>.CreateChecked<NFloat>((NFloat)MaxSubnormal));
                AssertBitwiseEqual(+MinNormal, NumberBaseHelper<double>.CreateChecked<NFloat>((NFloat)MinNormal));

                AssertBitwiseEqual(double.MaxValue, NumberBaseHelper<double>.CreateChecked<NFloat>(NFloat.MaxValue));
            }
            else
            {
                AssertBitwiseEqual(-3.4028234663852886E+38, NumberBaseHelper<double>.CreateChecked<NFloat>(NFloat.MinValue));
                AssertBitwiseEqual(-1.1754943508222875E-38, NumberBaseHelper<double>.CreateChecked<NFloat>(-SingleTests_GenericMath.MinNormal));
                AssertBitwiseEqual(-1.1754942106924411E-38, NumberBaseHelper<double>.CreateChecked<NFloat>(-SingleTests_GenericMath.MaxSubnormal));
                AssertBitwiseEqual(-1.401298464324817E-45, NumberBaseHelper<double>.CreateChecked<NFloat>(-NFloat.Epsilon));

                AssertBitwiseEqual(+1.401298464324817E-45, NumberBaseHelper<double>.CreateChecked<NFloat>(NFloat.Epsilon));
                AssertBitwiseEqual(+1.1754942106924411E-38, NumberBaseHelper<double>.CreateChecked<NFloat>(SingleTests_GenericMath.MaxSubnormal));
                AssertBitwiseEqual(+1.1754943508222875E-38, NumberBaseHelper<double>.CreateChecked<NFloat>(SingleTests_GenericMath.MinNormal));
                AssertBitwiseEqual(+3.4028234663852886E+38, NumberBaseHelper<double>.CreateChecked<NFloat>(NFloat.MaxValue));
            }

            AssertBitwiseEqual(double.PositiveInfinity, NumberBaseHelper<double>.CreateChecked<NFloat>(NFloat.PositiveInfinity));

            AssertBitwiseEqual(double.NaN, NumberBaseHelper<double>.CreateChecked<NFloat>(NFloat.NaN));
        }

        [Fact]
        public static void CreateCheckedFromSByteTest()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<double>.CreateChecked<sbyte>(0x00));
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.CreateChecked<sbyte>(0x01));
            AssertBitwiseEqual(127.0, NumberBaseHelper<double>.CreateChecked<sbyte>(0x7F));
            AssertBitwiseEqual(-128.0, NumberBaseHelper<double>.CreateChecked<sbyte>(unchecked((sbyte)0x80)));
            AssertBitwiseEqual(-1.0, NumberBaseHelper<double>.CreateChecked<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateCheckedFromSingleTest()
        {
            AssertBitwiseEqual(double.NegativeInfinity, NumberBaseHelper<double>.CreateChecked<float>(float.NegativeInfinity));

            AssertBitwiseEqual(-3.4028234663852886E+38, NumberBaseHelper<double>.CreateChecked<float>(float.MinValue));
            AssertBitwiseEqual(-1.0, NumberBaseHelper<double>.CreateChecked<float>(-1.0f));

            AssertBitwiseEqual(-1.1754943508222875E-38, NumberBaseHelper<double>.CreateChecked<float>(-SingleTests_GenericMath.MinNormal));
            AssertBitwiseEqual(-1.1754942106924411E-38, NumberBaseHelper<double>.CreateChecked<float>(-SingleTests_GenericMath.MaxSubnormal));
            AssertBitwiseEqual(-1.401298464324817E-45, NumberBaseHelper<double>.CreateChecked<float>(-float.Epsilon));
            AssertBitwiseEqual(-0.0, NumberBaseHelper<double>.CreateChecked<float>(-0.0f));

            AssertBitwiseEqual(+0.0, NumberBaseHelper<double>.CreateChecked<float>(+0.0f));
            AssertBitwiseEqual(+1.401298464324817E-45, NumberBaseHelper<double>.CreateChecked<float>(float.Epsilon));
            AssertBitwiseEqual(+1.1754942106924411E-38, NumberBaseHelper<double>.CreateChecked<float>(SingleTests_GenericMath.MaxSubnormal));
            AssertBitwiseEqual(+1.1754943508222875E-38, NumberBaseHelper<double>.CreateChecked<float>(SingleTests_GenericMath.MinNormal));

            AssertBitwiseEqual(+1.0, NumberBaseHelper<double>.CreateChecked<float>(1.0f));
            AssertBitwiseEqual(+3.4028234663852886E+38, NumberBaseHelper<double>.CreateChecked<float>(float.MaxValue));

            AssertBitwiseEqual(double.PositiveInfinity, NumberBaseHelper<double>.CreateChecked<float>(float.PositiveInfinity));

            AssertBitwiseEqual(double.NaN, NumberBaseHelper<double>.CreateChecked<float>(float.NaN));
        }

        [Fact]
        public static void CreateCheckedFromUInt16Test()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<double>.CreateChecked<ushort>(0x0000));
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.CreateChecked<ushort>(0x0001));
            AssertBitwiseEqual(32767.0, NumberBaseHelper<double>.CreateChecked<ushort>(0x7FFF));
            AssertBitwiseEqual(32768.0, NumberBaseHelper<double>.CreateChecked<ushort>(0x8000));
            AssertBitwiseEqual(65535.0, NumberBaseHelper<double>.CreateChecked<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUInt32Test()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<double>.CreateChecked<uint>(0x00000000));
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.CreateChecked<uint>(0x00000001));
            AssertBitwiseEqual(2147483647.0, NumberBaseHelper<double>.CreateChecked<uint>(0x7FFFFFFF));
            AssertBitwiseEqual(2147483648.0, NumberBaseHelper<double>.CreateChecked<uint>(0x80000000));
            AssertBitwiseEqual(4294967295.0, NumberBaseHelper<double>.CreateChecked<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUInt64Test()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<double>.CreateChecked<ulong>(0x0000000000000000));
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.CreateChecked<ulong>(0x0000000000000001));
            AssertBitwiseEqual(9223372036854775807.0, NumberBaseHelper<double>.CreateChecked<ulong>(0x7FFFFFFFFFFFFFFF));
            AssertBitwiseEqual(9223372036854775808.0, NumberBaseHelper<double>.CreateChecked<ulong>(0x8000000000000000));
            AssertBitwiseEqual(18446744073709551615.0, NumberBaseHelper<double>.CreateChecked<ulong>(0xFFFFFFFFFFFFFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUInt128Test()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<double>.CreateChecked<UInt128>(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000)));
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.CreateChecked<UInt128>(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001)));
            AssertBitwiseEqual(170141183460469231731687303715884105727.0, NumberBaseHelper<double>.CreateChecked<UInt128>(new UInt128(0x7FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)));
            AssertBitwiseEqual(170141183460469231731687303715884105728.0, NumberBaseHelper<double>.CreateChecked<UInt128>(new UInt128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000)));
            AssertBitwiseEqual(340282366920938463463374607431768211455.0, NumberBaseHelper<double>.CreateChecked<UInt128>(new UInt128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)));
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/69795", TestRuntimes.Mono)]
        public static void CreateCheckedFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual(0.0, NumberBaseHelper<double>.CreateChecked<nuint>(unchecked((nuint)0x0000000000000000)));
                AssertBitwiseEqual(1.0, NumberBaseHelper<double>.CreateChecked<nuint>(unchecked((nuint)0x0000000000000001)));
                AssertBitwiseEqual(9223372036854775807.0, NumberBaseHelper<double>.CreateChecked<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));

                // https://github.com/dotnet/roslyn/issues/60714
                // AssertBitwiseEqual(9223372036854775808.0, NumberBaseHelper<double>.CreateChecked<nuint>(unchecked((nuint)0x8000000000000000)));
                // AssertBitwiseEqual(18446744073709551615.0,NumberBaseHelper<double>.CreateChecked<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                AssertBitwiseEqual(0.0, NumberBaseHelper<double>.CreateChecked<nuint>((nuint)0x00000000));
                AssertBitwiseEqual(1.0, NumberBaseHelper<double>.CreateChecked<nuint>((nuint)0x00000001));
                AssertBitwiseEqual(2147483647.0, NumberBaseHelper<double>.CreateChecked<nuint>((nuint)0x7FFFFFFF));

                // https://github.com/dotnet/roslyn/issues/60714
                // AssertBitwiseEqual(2147483648.0, NumberBaseHelper<double>.CreateChecked<nuint>((nuint)0x80000000));
                // AssertBitwiseEqual(4294967295.0, NumberBaseHelper<double>.CreateChecked<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateSaturatingFromByteTest()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<double>.CreateSaturating<byte>(0x00));
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.CreateSaturating<byte>(0x01));
            AssertBitwiseEqual(127.0, NumberBaseHelper<double>.CreateSaturating<byte>(0x7F));
            AssertBitwiseEqual(128.0, NumberBaseHelper<double>.CreateSaturating<byte>(0x80));
            AssertBitwiseEqual(255.0, NumberBaseHelper<double>.CreateSaturating<byte>(0xFF));
        }

        [Fact]
        public static void CreateSaturatingFromCharTest()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<double>.CreateSaturating<char>((char)0x0000));
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.CreateSaturating<char>((char)0x0001));
            AssertBitwiseEqual(32767.0, NumberBaseHelper<double>.CreateSaturating<char>((char)0x7FFF));
            AssertBitwiseEqual(32768.0, NumberBaseHelper<double>.CreateSaturating<char>((char)0x8000));
            AssertBitwiseEqual(65535.0, NumberBaseHelper<double>.CreateSaturating<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromDecimalTest()
        {
            AssertBitwiseEqual(-79228162514264337593543950335.0, NumberBaseHelper<double>.CreateSaturating<decimal>(decimal.MinValue));
            AssertBitwiseEqual(-1.0, NumberBaseHelper<double>.CreateSaturating<decimal>(-1.0m));
            AssertBitwiseEqual(-0.0, NumberBaseHelper<double>.CreateSaturating<decimal>(-0.0m));
            AssertBitwiseEqual(+0.0, NumberBaseHelper<double>.CreateSaturating<decimal>(+0.0m));
            AssertBitwiseEqual(+1.0, NumberBaseHelper<double>.CreateSaturating<decimal>(+1.0m));
            AssertBitwiseEqual(+79228162514264337593543950335.0, NumberBaseHelper<double>.CreateSaturating<decimal>(decimal.MaxValue));
        }

        [Fact]
        public static void CreateSaturatingFromDoubleTest()
        {
            AssertBitwiseEqual(double.NegativeInfinity, NumberBaseHelper<double>.CreateSaturating<double>(double.NegativeInfinity));
            AssertBitwiseEqual(double.MinValue, NumberBaseHelper<double>.CreateSaturating<double>(double.MinValue));

            AssertBitwiseEqual(-1.0, NumberBaseHelper<double>.CreateSaturating<double>(-1.0));

            AssertBitwiseEqual(-MinNormal, NumberBaseHelper<double>.CreateSaturating<double>(-MinNormal));
            AssertBitwiseEqual(-MaxSubnormal, NumberBaseHelper<double>.CreateSaturating<double>(-MaxSubnormal));
            AssertBitwiseEqual(-double.Epsilon, NumberBaseHelper<double>.CreateSaturating<double>(-double.Epsilon));
            AssertBitwiseEqual(-0.0, NumberBaseHelper<double>.CreateSaturating<double>(-0.0));

            AssertBitwiseEqual(+0.0, NumberBaseHelper<double>.CreateSaturating<double>(+0.0));
            AssertBitwiseEqual(+double.Epsilon, NumberBaseHelper<double>.CreateSaturating<double>(double.Epsilon));
            AssertBitwiseEqual(+MaxSubnormal, NumberBaseHelper<double>.CreateSaturating<double>(MaxSubnormal));
            AssertBitwiseEqual(+MinNormal, NumberBaseHelper<double>.CreateSaturating<double>(MinNormal));

            AssertBitwiseEqual(+1.0, NumberBaseHelper<double>.CreateSaturating<double>(1.0));

            AssertBitwiseEqual(double.MaxValue, NumberBaseHelper<double>.CreateSaturating<double>(double.MaxValue));
            AssertBitwiseEqual(double.PositiveInfinity, NumberBaseHelper<double>.CreateSaturating<double>(double.PositiveInfinity));

            AssertBitwiseEqual(double.NaN, NumberBaseHelper<double>.CreateSaturating<double>(double.NaN));
        }

        [Fact]
        public static void CreateSaturatingFromHalfTest()
        {
            AssertBitwiseEqual(double.NegativeInfinity, NumberBaseHelper<double>.CreateSaturating<Half>(Half.NegativeInfinity));

            AssertBitwiseEqual(-65504.0, NumberBaseHelper<double>.CreateSaturating<Half>(Half.MinValue));
            AssertBitwiseEqual(-1.0, NumberBaseHelper<double>.CreateSaturating<Half>(Half.NegativeOne));

            AssertBitwiseEqual(-6.103515625E-05, NumberBaseHelper<double>.CreateSaturating<Half>(-HalfTests_GenericMath.MinNormal));
            AssertBitwiseEqual(-6.097555160522461E-05, NumberBaseHelper<double>.CreateSaturating<Half>(-HalfTests_GenericMath.MaxSubnormal));
            AssertBitwiseEqual(-5.960464477539063E-08, NumberBaseHelper<double>.CreateSaturating<Half>(-Half.Epsilon));
            AssertBitwiseEqual(-0.0, NumberBaseHelper<double>.CreateSaturating<Half>(Half.NegativeZero));

            AssertBitwiseEqual(+0.0, NumberBaseHelper<double>.CreateSaturating<Half>(Half.Zero));
            AssertBitwiseEqual(+5.960464477539063E-08, NumberBaseHelper<double>.CreateSaturating<Half>(Half.Epsilon));
            AssertBitwiseEqual(+6.097555160522461E-05, NumberBaseHelper<double>.CreateSaturating<Half>(HalfTests_GenericMath.MaxSubnormal));
            AssertBitwiseEqual(+6.103515625E-05, NumberBaseHelper<double>.CreateSaturating<Half>(HalfTests_GenericMath.MinNormal));

            AssertBitwiseEqual(+1.0, NumberBaseHelper<double>.CreateSaturating<Half>(Half.One));
            AssertBitwiseEqual(+65504.0, NumberBaseHelper<double>.CreateSaturating<Half>(Half.MaxValue));

            AssertBitwiseEqual(double.PositiveInfinity, NumberBaseHelper<double>.CreateSaturating<Half>(Half.PositiveInfinity));

            AssertBitwiseEqual(double.NaN, NumberBaseHelper<double>.CreateSaturating<Half>(Half.NaN));
        }

        [Fact]
        public static void CreateSaturatingFromInt16Test()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<double>.CreateSaturating<short>(0x0000));
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.CreateSaturating<short>(0x0001));
            AssertBitwiseEqual(32767.0, NumberBaseHelper<double>.CreateSaturating<short>(0x7FFF));
            AssertBitwiseEqual(-32768.0, NumberBaseHelper<double>.CreateSaturating<short>(unchecked((short)0x8000)));
            AssertBitwiseEqual(-1.0, NumberBaseHelper<double>.CreateSaturating<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromInt32Test()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<double>.CreateSaturating<int>(0x00000000));
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.CreateSaturating<int>(0x00000001));
            AssertBitwiseEqual(2147483647.0, NumberBaseHelper<double>.CreateSaturating<int>(0x7FFFFFFF));
            AssertBitwiseEqual(-2147483648.0, NumberBaseHelper<double>.CreateSaturating<int>(unchecked((int)0x80000000)));
            AssertBitwiseEqual(-1.0, NumberBaseHelper<double>.CreateSaturating<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromInt64Test()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<double>.CreateSaturating<long>(0x0000000000000000));
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.CreateSaturating<long>(0x0000000000000001));
            AssertBitwiseEqual(9223372036854775807.0, NumberBaseHelper<double>.CreateSaturating<long>(0x7FFFFFFFFFFFFFFF));
            AssertBitwiseEqual(-9223372036854775808.0, NumberBaseHelper<double>.CreateSaturating<long>(unchecked(unchecked((long)0x8000000000000000))));
            AssertBitwiseEqual(-1.0, NumberBaseHelper<double>.CreateSaturating<long>(unchecked(unchecked((long)0xFFFFFFFFFFFFFFFF))));
        }

        [Fact]
        public static void CreateSaturatingFromInt128Test()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<double>.CreateSaturating<Int128>(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000)));
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.CreateSaturating<Int128>(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001)));
            AssertBitwiseEqual(170141183460469231731687303715884105727.0, NumberBaseHelper<double>.CreateSaturating<Int128>(new Int128(0x7FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)));
            AssertBitwiseEqual(-170141183460469231731687303715884105728.0, NumberBaseHelper<double>.CreateSaturating<Int128>(new Int128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000)));
            AssertBitwiseEqual(-1.0, NumberBaseHelper<double>.CreateSaturating<Int128>(new Int128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual(0.0, NumberBaseHelper<double>.CreateSaturating<nint>(unchecked((nint)0x0000000000000000)));
                AssertBitwiseEqual(1.0, NumberBaseHelper<double>.CreateSaturating<nint>(unchecked((nint)0x0000000000000001)));
                AssertBitwiseEqual(9223372036854775807.0, NumberBaseHelper<double>.CreateSaturating<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                AssertBitwiseEqual(-9223372036854775808.0, NumberBaseHelper<double>.CreateSaturating<nint>(unchecked((nint)0x8000000000000000)));
                AssertBitwiseEqual(-1.0, NumberBaseHelper<double>.CreateSaturating<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                AssertBitwiseEqual(0.0, NumberBaseHelper<double>.CreateSaturating<nint>((nint)0x00000000));
                AssertBitwiseEqual(1.0, NumberBaseHelper<double>.CreateSaturating<nint>((nint)0x00000001));
                AssertBitwiseEqual(2147483647.0, NumberBaseHelper<double>.CreateSaturating<nint>((nint)0x7FFFFFFF));
                AssertBitwiseEqual(-2147483648.0, NumberBaseHelper<double>.CreateSaturating<nint>(unchecked((nint)0x80000000)));
                AssertBitwiseEqual(-1.0, NumberBaseHelper<double>.CreateSaturating<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateSaturatingFromNFloatTest()
        {
            AssertBitwiseEqual(double.NegativeInfinity, NumberBaseHelper<double>.CreateSaturating<NFloat>(NFloat.NegativeInfinity));

            AssertBitwiseEqual(-1.0, NumberBaseHelper<double>.CreateSaturating<NFloat>(-1.0f));
            AssertBitwiseEqual(-0.0, NumberBaseHelper<double>.CreateSaturating<NFloat>(-0.0f));

            AssertBitwiseEqual(+0.0, NumberBaseHelper<double>.CreateSaturating<NFloat>(+0.0f));
            AssertBitwiseEqual(+1.0, NumberBaseHelper<double>.CreateSaturating<NFloat>(1.0f));

            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual(double.MinValue, NumberBaseHelper<double>.CreateSaturating<NFloat>(NFloat.MinValue));

                AssertBitwiseEqual(-MinNormal, NumberBaseHelper<double>.CreateSaturating<NFloat>((NFloat)(-MinNormal)));
                AssertBitwiseEqual(-MaxSubnormal, NumberBaseHelper<double>.CreateSaturating<NFloat>((NFloat)(-MaxSubnormal)));
                AssertBitwiseEqual(-double.Epsilon, NumberBaseHelper<double>.CreateSaturating<NFloat>(-NFloat.Epsilon));

                AssertBitwiseEqual(+double.Epsilon, NumberBaseHelper<double>.CreateSaturating<NFloat>(NFloat.Epsilon));
                AssertBitwiseEqual(+MaxSubnormal, NumberBaseHelper<double>.CreateSaturating<NFloat>((NFloat)MaxSubnormal));
                AssertBitwiseEqual(+MinNormal, NumberBaseHelper<double>.CreateSaturating<NFloat>((NFloat)MinNormal));

                AssertBitwiseEqual(double.MaxValue, NumberBaseHelper<double>.CreateSaturating<NFloat>(NFloat.MaxValue));
            }
            else
            {
                AssertBitwiseEqual(-3.4028234663852886E+38, NumberBaseHelper<double>.CreateSaturating<NFloat>(NFloat.MinValue));
                AssertBitwiseEqual(-1.1754943508222875E-38, NumberBaseHelper<double>.CreateSaturating<NFloat>(-SingleTests_GenericMath.MinNormal));
                AssertBitwiseEqual(-1.1754942106924411E-38, NumberBaseHelper<double>.CreateSaturating<NFloat>(-SingleTests_GenericMath.MaxSubnormal));
                AssertBitwiseEqual(-1.401298464324817E-45, NumberBaseHelper<double>.CreateSaturating<NFloat>(-NFloat.Epsilon));

                AssertBitwiseEqual(+1.401298464324817E-45, NumberBaseHelper<double>.CreateSaturating<NFloat>(NFloat.Epsilon));
                AssertBitwiseEqual(+1.1754942106924411E-38, NumberBaseHelper<double>.CreateSaturating<NFloat>(SingleTests_GenericMath.MaxSubnormal));
                AssertBitwiseEqual(+1.1754943508222875E-38, NumberBaseHelper<double>.CreateSaturating<NFloat>(SingleTests_GenericMath.MinNormal));
                AssertBitwiseEqual(+3.4028234663852886E+38, NumberBaseHelper<double>.CreateSaturating<NFloat>(NFloat.MaxValue));
            }

            AssertBitwiseEqual(double.PositiveInfinity, NumberBaseHelper<double>.CreateSaturating<NFloat>(NFloat.PositiveInfinity));

            AssertBitwiseEqual(double.NaN, NumberBaseHelper<double>.CreateSaturating<NFloat>(NFloat.NaN));
        }

        [Fact]
        public static void CreateSaturatingFromSByteTest()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<double>.CreateSaturating<sbyte>(0x00));
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.CreateSaturating<sbyte>(0x01));
            AssertBitwiseEqual(127.0, NumberBaseHelper<double>.CreateSaturating<sbyte>(0x7F));
            AssertBitwiseEqual(-128.0, NumberBaseHelper<double>.CreateSaturating<sbyte>(unchecked((sbyte)0x80)));
            AssertBitwiseEqual(-1.0, NumberBaseHelper<double>.CreateSaturating<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateSaturatingFromSingleTest()
        {
            AssertBitwiseEqual(double.NegativeInfinity, NumberBaseHelper<double>.CreateSaturating<float>(float.NegativeInfinity));

            AssertBitwiseEqual(-3.4028234663852886E+38, NumberBaseHelper<double>.CreateSaturating<float>(float.MinValue));
            AssertBitwiseEqual(-1.0, NumberBaseHelper<double>.CreateSaturating<float>(-1.0f));

            AssertBitwiseEqual(-1.1754943508222875E-38, NumberBaseHelper<double>.CreateSaturating<float>(-SingleTests_GenericMath.MinNormal));
            AssertBitwiseEqual(-1.1754942106924411E-38, NumberBaseHelper<double>.CreateSaturating<float>(-SingleTests_GenericMath.MaxSubnormal));
            AssertBitwiseEqual(-1.401298464324817E-45, NumberBaseHelper<double>.CreateSaturating<float>(-float.Epsilon));
            AssertBitwiseEqual(-0.0, NumberBaseHelper<double>.CreateSaturating<float>(-0.0f));

            AssertBitwiseEqual(+0.0, NumberBaseHelper<double>.CreateSaturating<float>(+0.0f));
            AssertBitwiseEqual(+1.401298464324817E-45, NumberBaseHelper<double>.CreateSaturating<float>(float.Epsilon));
            AssertBitwiseEqual(+1.1754942106924411E-38, NumberBaseHelper<double>.CreateSaturating<float>(SingleTests_GenericMath.MaxSubnormal));
            AssertBitwiseEqual(+1.1754943508222875E-38, NumberBaseHelper<double>.CreateSaturating<float>(SingleTests_GenericMath.MinNormal));

            AssertBitwiseEqual(+1.0, NumberBaseHelper<double>.CreateSaturating<float>(1.0f));
            AssertBitwiseEqual(+3.4028234663852886E+38, NumberBaseHelper<double>.CreateSaturating<float>(float.MaxValue));

            AssertBitwiseEqual(double.PositiveInfinity, NumberBaseHelper<double>.CreateSaturating<float>(float.PositiveInfinity));

            AssertBitwiseEqual(double.NaN, NumberBaseHelper<double>.CreateSaturating<float>(float.NaN));
        }

        [Fact]
        public static void CreateSaturatingFromUInt16Test()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<double>.CreateSaturating<ushort>(0x0000));
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.CreateSaturating<ushort>(0x0001));
            AssertBitwiseEqual(32767.0, NumberBaseHelper<double>.CreateSaturating<ushort>(0x7FFF));
            AssertBitwiseEqual(32768.0, NumberBaseHelper<double>.CreateSaturating<ushort>(0x8000));
            AssertBitwiseEqual(65535.0, NumberBaseHelper<double>.CreateSaturating<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUInt32Test()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<double>.CreateSaturating<uint>(0x00000000));
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.CreateSaturating<uint>(0x00000001));
            AssertBitwiseEqual(2147483647.0, NumberBaseHelper<double>.CreateSaturating<uint>(0x7FFFFFFF));
            AssertBitwiseEqual(2147483648.0, NumberBaseHelper<double>.CreateSaturating<uint>(0x80000000));
            AssertBitwiseEqual(4294967295.0, NumberBaseHelper<double>.CreateSaturating<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUInt64Test()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<double>.CreateSaturating<ulong>(0x0000000000000000));
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.CreateSaturating<ulong>(0x0000000000000001));
            AssertBitwiseEqual(9223372036854775807.0, NumberBaseHelper<double>.CreateSaturating<ulong>(0x7FFFFFFFFFFFFFFF));
            AssertBitwiseEqual(9223372036854775808.0, NumberBaseHelper<double>.CreateSaturating<ulong>(0x8000000000000000));
            AssertBitwiseEqual(18446744073709551615.0, NumberBaseHelper<double>.CreateSaturating<ulong>(0xFFFFFFFFFFFFFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUInt128Test()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<double>.CreateSaturating<UInt128>(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000)));
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.CreateSaturating<UInt128>(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001)));
            AssertBitwiseEqual(170141183460469231731687303715884105727.0, NumberBaseHelper<double>.CreateSaturating<UInt128>(new UInt128(0x7FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)));
            AssertBitwiseEqual(170141183460469231731687303715884105728.0, NumberBaseHelper<double>.CreateSaturating<UInt128>(new UInt128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000)));
            AssertBitwiseEqual(340282366920938463463374607431768211455.0, NumberBaseHelper<double>.CreateSaturating<UInt128>(new UInt128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)));
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/69795", TestRuntimes.Mono)]
        public static void CreateSaturatingFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual(0.0, NumberBaseHelper<double>.CreateSaturating<nuint>(unchecked((nuint)0x0000000000000000)));
                AssertBitwiseEqual(1.0, NumberBaseHelper<double>.CreateSaturating<nuint>(unchecked((nuint)0x0000000000000001)));
                AssertBitwiseEqual(9223372036854775807.0, NumberBaseHelper<double>.CreateSaturating<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));

                // https://github.com/dotnet/roslyn/issues/60714
                // AssertBitwiseEqual(9223372036854775808.0, NumberBaseHelper<double>.CreateSaturating<nuint>(unchecked((nuint)0x8000000000000000)));
                // AssertBitwiseEqual(18446744073709551615.0, NumberBaseHelper<double>.CreateSaturating<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                AssertBitwiseEqual(0.0, NumberBaseHelper<double>.CreateSaturating<nuint>((nuint)0x00000000));
                AssertBitwiseEqual(1.0, NumberBaseHelper<double>.CreateSaturating<nuint>((nuint)0x00000001));
                AssertBitwiseEqual(2147483647.0, NumberBaseHelper<double>.CreateSaturating<nuint>((nuint)0x7FFFFFFF));

                // https://github.com/dotnet/roslyn/issues/60714
                // AssertBitwiseEqual(2147483648.0, NumberBaseHelper<double>.CreateSaturating<nuint>((nuint)0x80000000));
                // AssertBitwiseEqual(4294967295.0, NumberBaseHelper<double>.CreateSaturating<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateTruncatingFromByteTest()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<double>.CreateTruncating<byte>(0x00));
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.CreateTruncating<byte>(0x01));
            AssertBitwiseEqual(127.0, NumberBaseHelper<double>.CreateTruncating<byte>(0x7F));
            AssertBitwiseEqual(128.0, NumberBaseHelper<double>.CreateTruncating<byte>(0x80));
            AssertBitwiseEqual(255.0, NumberBaseHelper<double>.CreateTruncating<byte>(0xFF));
        }

        [Fact]
        public static void CreateTruncatingFromCharTest()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<double>.CreateTruncating<char>((char)0x0000));
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.CreateTruncating<char>((char)0x0001));
            AssertBitwiseEqual(32767.0, NumberBaseHelper<double>.CreateTruncating<char>((char)0x7FFF));
            AssertBitwiseEqual(32768.0, NumberBaseHelper<double>.CreateTruncating<char>((char)0x8000));
            AssertBitwiseEqual(65535.0, NumberBaseHelper<double>.CreateTruncating<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromDecimalTest()
        {
            AssertBitwiseEqual(-79228162514264337593543950335.0, NumberBaseHelper<double>.CreateTruncating<decimal>(decimal.MinValue));
            AssertBitwiseEqual(-1.0, NumberBaseHelper<double>.CreateTruncating<decimal>(-1.0m));
            AssertBitwiseEqual(-0.0, NumberBaseHelper<double>.CreateTruncating<decimal>(-0.0m));
            AssertBitwiseEqual(+0.0, NumberBaseHelper<double>.CreateTruncating<decimal>(+0.0m));
            AssertBitwiseEqual(+1.0, NumberBaseHelper<double>.CreateTruncating<decimal>(+1.0m));
            AssertBitwiseEqual(+79228162514264337593543950335.0, NumberBaseHelper<double>.CreateTruncating<decimal>(decimal.MaxValue));
        }

        [Fact]
        public static void CreateTruncatingFromDoubleTest()
        {
            AssertBitwiseEqual(double.NegativeInfinity, NumberBaseHelper<double>.CreateTruncating<double>(double.NegativeInfinity));
            AssertBitwiseEqual(double.MinValue, NumberBaseHelper<double>.CreateTruncating<double>(double.MinValue));

            AssertBitwiseEqual(-1.0, NumberBaseHelper<double>.CreateTruncating<double>(-1.0));

            AssertBitwiseEqual(-MinNormal, NumberBaseHelper<double>.CreateTruncating<double>(-MinNormal));
            AssertBitwiseEqual(-MaxSubnormal, NumberBaseHelper<double>.CreateTruncating<double>(-MaxSubnormal));
            AssertBitwiseEqual(-double.Epsilon, NumberBaseHelper<double>.CreateTruncating<double>(-double.Epsilon));
            AssertBitwiseEqual(-0.0, NumberBaseHelper<double>.CreateTruncating<double>(-0.0));

            AssertBitwiseEqual(+0.0, NumberBaseHelper<double>.CreateTruncating<double>(+0.0));
            AssertBitwiseEqual(+double.Epsilon, NumberBaseHelper<double>.CreateTruncating<double>(double.Epsilon));
            AssertBitwiseEqual(+MaxSubnormal, NumberBaseHelper<double>.CreateTruncating<double>(MaxSubnormal));
            AssertBitwiseEqual(+MinNormal, NumberBaseHelper<double>.CreateTruncating<double>(MinNormal));

            AssertBitwiseEqual(+1.0, NumberBaseHelper<double>.CreateTruncating<double>(1.0));

            AssertBitwiseEqual(double.MaxValue, NumberBaseHelper<double>.CreateTruncating<double>(double.MaxValue));
            AssertBitwiseEqual(double.PositiveInfinity, NumberBaseHelper<double>.CreateTruncating<double>(double.PositiveInfinity));

            AssertBitwiseEqual(double.NaN, NumberBaseHelper<double>.CreateTruncating<double>(double.NaN));
        }

        [Fact]
        public static void CreateTruncatingFromHalfTest()
        {
            AssertBitwiseEqual(double.NegativeInfinity, NumberBaseHelper<double>.CreateTruncating<Half>(Half.NegativeInfinity));

            AssertBitwiseEqual(-65504.0, NumberBaseHelper<double>.CreateTruncating<Half>(Half.MinValue));
            AssertBitwiseEqual(-1.0, NumberBaseHelper<double>.CreateTruncating<Half>(Half.NegativeOne));

            AssertBitwiseEqual(-6.103515625E-05, NumberBaseHelper<double>.CreateTruncating<Half>(-HalfTests_GenericMath.MinNormal));
            AssertBitwiseEqual(-6.097555160522461E-05, NumberBaseHelper<double>.CreateTruncating<Half>(-HalfTests_GenericMath.MaxSubnormal));
            AssertBitwiseEqual(-5.960464477539063E-08, NumberBaseHelper<double>.CreateTruncating<Half>(-Half.Epsilon));
            AssertBitwiseEqual(-0.0, NumberBaseHelper<double>.CreateTruncating<Half>(Half.NegativeZero));

            AssertBitwiseEqual(+0.0, NumberBaseHelper<double>.CreateTruncating<Half>(Half.Zero));
            AssertBitwiseEqual(+5.960464477539063E-08, NumberBaseHelper<double>.CreateTruncating<Half>(Half.Epsilon));
            AssertBitwiseEqual(+6.097555160522461E-05, NumberBaseHelper<double>.CreateTruncating<Half>(HalfTests_GenericMath.MaxSubnormal));
            AssertBitwiseEqual(+6.103515625E-05, NumberBaseHelper<double>.CreateTruncating<Half>(HalfTests_GenericMath.MinNormal));

            AssertBitwiseEqual(+1.0, NumberBaseHelper<double>.CreateTruncating<Half>(Half.One));
            AssertBitwiseEqual(+65504.0, NumberBaseHelper<double>.CreateTruncating<Half>(Half.MaxValue));

            AssertBitwiseEqual(double.PositiveInfinity, NumberBaseHelper<double>.CreateTruncating<Half>(Half.PositiveInfinity));

            AssertBitwiseEqual(double.NaN, NumberBaseHelper<double>.CreateTruncating<Half>(Half.NaN));
        }

        [Fact]
        public static void CreateTruncatingFromInt16Test()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<double>.CreateTruncating<short>(0x0000));
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.CreateTruncating<short>(0x0001));
            AssertBitwiseEqual(32767.0, NumberBaseHelper<double>.CreateTruncating<short>(0x7FFF));
            AssertBitwiseEqual(-32768.0, NumberBaseHelper<double>.CreateTruncating<short>(unchecked((short)0x8000)));
            AssertBitwiseEqual(-1.0, NumberBaseHelper<double>.CreateTruncating<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateTruncatingFromInt32Test()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<double>.CreateTruncating<int>(0x00000000));
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.CreateTruncating<int>(0x00000001));
            AssertBitwiseEqual(2147483647.0, NumberBaseHelper<double>.CreateTruncating<int>(0x7FFFFFFF));
            AssertBitwiseEqual(-2147483648.0, NumberBaseHelper<double>.CreateTruncating<int>(unchecked((int)0x80000000)));
            AssertBitwiseEqual(-1.0, NumberBaseHelper<double>.CreateTruncating<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateTruncatingFromInt64Test()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<double>.CreateTruncating<long>(0x0000000000000000));
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.CreateTruncating<long>(0x0000000000000001));
            AssertBitwiseEqual(9223372036854775807.0, NumberBaseHelper<double>.CreateTruncating<long>(0x7FFFFFFFFFFFFFFF));
            AssertBitwiseEqual(-9223372036854775808.0, NumberBaseHelper<double>.CreateTruncating<long>(unchecked(unchecked((long)0x8000000000000000))));
            AssertBitwiseEqual(-1.0, NumberBaseHelper<double>.CreateTruncating<long>(unchecked(unchecked((long)0xFFFFFFFFFFFFFFFF))));
        }

        [Fact]
        public static void CreateTruncatingFromInt128Test()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<double>.CreateTruncating<Int128>(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000)));
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.CreateTruncating<Int128>(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001)));
            AssertBitwiseEqual(170141183460469231731687303715884105727.0, NumberBaseHelper<double>.CreateTruncating<Int128>(new Int128(0x7FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)));
            AssertBitwiseEqual(-170141183460469231731687303715884105728.0, NumberBaseHelper<double>.CreateTruncating<Int128>(new Int128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000)));
            AssertBitwiseEqual(-1.0, NumberBaseHelper<double>.CreateTruncating<Int128>(new Int128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)));
        }

        [Fact]
        public static void CreateTruncatingFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual(0.0, NumberBaseHelper<double>.CreateTruncating<nint>(unchecked((nint)0x0000000000000000)));
                AssertBitwiseEqual(1.0, NumberBaseHelper<double>.CreateTruncating<nint>(unchecked((nint)0x0000000000000001)));
                AssertBitwiseEqual(9223372036854775807.0, NumberBaseHelper<double>.CreateTruncating<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                AssertBitwiseEqual(-9223372036854775808.0, NumberBaseHelper<double>.CreateTruncating<nint>(unchecked((nint)0x8000000000000000)));
                AssertBitwiseEqual(-1.0, NumberBaseHelper<double>.CreateTruncating<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                AssertBitwiseEqual(0.0, NumberBaseHelper<double>.CreateTruncating<nint>((nint)0x00000000));
                AssertBitwiseEqual(1.0, NumberBaseHelper<double>.CreateTruncating<nint>((nint)0x00000001));
                AssertBitwiseEqual(2147483647.0, NumberBaseHelper<double>.CreateTruncating<nint>((nint)0x7FFFFFFF));
                AssertBitwiseEqual(-2147483648.0, NumberBaseHelper<double>.CreateTruncating<nint>(unchecked((nint)0x80000000)));
                AssertBitwiseEqual(-1.0, NumberBaseHelper<double>.CreateTruncating<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateTruncatingFromNFloatTest()
        {
            AssertBitwiseEqual(double.NegativeInfinity, NumberBaseHelper<double>.CreateTruncating<NFloat>(NFloat.NegativeInfinity));

            AssertBitwiseEqual(-1.0, NumberBaseHelper<double>.CreateTruncating<NFloat>(-1.0f));
            AssertBitwiseEqual(-0.0, NumberBaseHelper<double>.CreateTruncating<NFloat>(-0.0f));

            AssertBitwiseEqual(+0.0, NumberBaseHelper<double>.CreateTruncating<NFloat>(+0.0f));
            AssertBitwiseEqual(+1.0, NumberBaseHelper<double>.CreateTruncating<NFloat>(1.0f));

            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual(double.MinValue, NumberBaseHelper<double>.CreateTruncating<NFloat>(NFloat.MinValue));

                AssertBitwiseEqual(-MinNormal, NumberBaseHelper<double>.CreateTruncating<NFloat>((NFloat)(-MinNormal)));
                AssertBitwiseEqual(-MaxSubnormal, NumberBaseHelper<double>.CreateTruncating<NFloat>((NFloat)(-MaxSubnormal)));
                AssertBitwiseEqual(-double.Epsilon, NumberBaseHelper<double>.CreateTruncating<NFloat>(-NFloat.Epsilon));

                AssertBitwiseEqual(+double.Epsilon, NumberBaseHelper<double>.CreateTruncating<NFloat>(NFloat.Epsilon));
                AssertBitwiseEqual(+MaxSubnormal, NumberBaseHelper<double>.CreateTruncating<NFloat>((NFloat)MaxSubnormal));
                AssertBitwiseEqual(+MinNormal, NumberBaseHelper<double>.CreateTruncating<NFloat>((NFloat)MinNormal));

                AssertBitwiseEqual(double.MaxValue, NumberBaseHelper<double>.CreateTruncating<NFloat>(NFloat.MaxValue));
            }
            else
            {
                AssertBitwiseEqual(-3.4028234663852886E+38, NumberBaseHelper<double>.CreateTruncating<NFloat>(NFloat.MinValue));
                AssertBitwiseEqual(-1.1754943508222875E-38, NumberBaseHelper<double>.CreateTruncating<NFloat>(-SingleTests_GenericMath.MinNormal));
                AssertBitwiseEqual(-1.1754942106924411E-38, NumberBaseHelper<double>.CreateTruncating<NFloat>(-SingleTests_GenericMath.MaxSubnormal));
                AssertBitwiseEqual(-1.401298464324817E-45, NumberBaseHelper<double>.CreateTruncating<NFloat>(-NFloat.Epsilon));

                AssertBitwiseEqual(+1.401298464324817E-45, NumberBaseHelper<double>.CreateTruncating<NFloat>(NFloat.Epsilon));
                AssertBitwiseEqual(+1.1754942106924411E-38, NumberBaseHelper<double>.CreateTruncating<NFloat>(SingleTests_GenericMath.MaxSubnormal));
                AssertBitwiseEqual(+1.1754943508222875E-38, NumberBaseHelper<double>.CreateTruncating<NFloat>(SingleTests_GenericMath.MinNormal));
                AssertBitwiseEqual(+3.4028234663852886E+38, NumberBaseHelper<double>.CreateTruncating<NFloat>(NFloat.MaxValue));
            }

            AssertBitwiseEqual(double.PositiveInfinity, NumberBaseHelper<double>.CreateTruncating<NFloat>(NFloat.PositiveInfinity));

            AssertBitwiseEqual(double.NaN, NumberBaseHelper<double>.CreateTruncating<NFloat>(NFloat.NaN));
        }

        [Fact]
        public static void CreateTruncatingFromSByteTest()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<double>.CreateTruncating<sbyte>(0x00));
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.CreateTruncating<sbyte>(0x01));
            AssertBitwiseEqual(127.0, NumberBaseHelper<double>.CreateTruncating<sbyte>(0x7F));
            AssertBitwiseEqual(-128.0, NumberBaseHelper<double>.CreateTruncating<sbyte>(unchecked((sbyte)0x80)));
            AssertBitwiseEqual(-1.0, NumberBaseHelper<double>.CreateTruncating<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateTruncatingFromSingleTest()
        {
            AssertBitwiseEqual(double.NegativeInfinity, NumberBaseHelper<double>.CreateTruncating<float>(float.NegativeInfinity));

            AssertBitwiseEqual(-3.4028234663852886E+38, NumberBaseHelper<double>.CreateTruncating<float>(float.MinValue));
            AssertBitwiseEqual(-1.0, NumberBaseHelper<double>.CreateTruncating<float>(-1.0f));

            AssertBitwiseEqual(-1.1754943508222875E-38, NumberBaseHelper<double>.CreateTruncating<float>(-SingleTests_GenericMath.MinNormal));
            AssertBitwiseEqual(-1.1754942106924411E-38, NumberBaseHelper<double>.CreateTruncating<float>(-SingleTests_GenericMath.MaxSubnormal));
            AssertBitwiseEqual(-1.401298464324817E-45, NumberBaseHelper<double>.CreateTruncating<float>(-float.Epsilon));
            AssertBitwiseEqual(-0.0, NumberBaseHelper<double>.CreateTruncating<float>(-0.0f));

            AssertBitwiseEqual(+0.0, NumberBaseHelper<double>.CreateTruncating<float>(+0.0f));
            AssertBitwiseEqual(+1.401298464324817E-45, NumberBaseHelper<double>.CreateTruncating<float>(float.Epsilon));
            AssertBitwiseEqual(+1.1754942106924411E-38, NumberBaseHelper<double>.CreateTruncating<float>(SingleTests_GenericMath.MaxSubnormal));
            AssertBitwiseEqual(+1.1754943508222875E-38, NumberBaseHelper<double>.CreateTruncating<float>(SingleTests_GenericMath.MinNormal));

            AssertBitwiseEqual(+1.0, NumberBaseHelper<double>.CreateTruncating<float>(1.0f));
            AssertBitwiseEqual(+3.4028234663852886E+38, NumberBaseHelper<double>.CreateTruncating<float>(float.MaxValue));

            AssertBitwiseEqual(double.PositiveInfinity, NumberBaseHelper<double>.CreateTruncating<float>(float.PositiveInfinity));

            AssertBitwiseEqual(double.NaN, NumberBaseHelper<double>.CreateTruncating<float>(float.NaN));
        }

        [Fact]
        public static void CreateTruncatingFromUInt16Test()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<double>.CreateTruncating<ushort>(0x0000));
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.CreateTruncating<ushort>(0x0001));
            AssertBitwiseEqual(32767.0, NumberBaseHelper<double>.CreateTruncating<ushort>(0x7FFF));
            AssertBitwiseEqual(32768.0, NumberBaseHelper<double>.CreateTruncating<ushort>(0x8000));
            AssertBitwiseEqual(65535.0, NumberBaseHelper<double>.CreateTruncating<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt32Test()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<double>.CreateTruncating<uint>(0x00000000));
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.CreateTruncating<uint>(0x00000001));
            AssertBitwiseEqual(2147483647.0, NumberBaseHelper<double>.CreateTruncating<uint>(0x7FFFFFFF));
            AssertBitwiseEqual(2147483648.0, NumberBaseHelper<double>.CreateTruncating<uint>(0x80000000));
            AssertBitwiseEqual(4294967295.0, NumberBaseHelper<double>.CreateTruncating<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt64Test()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<double>.CreateTruncating<ulong>(0x0000000000000000));
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.CreateTruncating<ulong>(0x0000000000000001));
            AssertBitwiseEqual(9223372036854775807.0, NumberBaseHelper<double>.CreateTruncating<ulong>(0x7FFFFFFFFFFFFFFF));
            AssertBitwiseEqual(9223372036854775808.0, NumberBaseHelper<double>.CreateTruncating<ulong>(0x8000000000000000));
            AssertBitwiseEqual(18446744073709551615.0, NumberBaseHelper<double>.CreateTruncating<ulong>(0xFFFFFFFFFFFFFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt128Test()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<double>.CreateTruncating<UInt128>(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000)));
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.CreateTruncating<UInt128>(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001)));
            AssertBitwiseEqual(170141183460469231731687303715884105727.0, NumberBaseHelper<double>.CreateTruncating<UInt128>(new UInt128(0x7FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)));
            AssertBitwiseEqual(170141183460469231731687303715884105728.0, NumberBaseHelper<double>.CreateTruncating<UInt128>(new UInt128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000)));
            AssertBitwiseEqual(340282366920938463463374607431768211455.0, NumberBaseHelper<double>.CreateTruncating<UInt128>(new UInt128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)));
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/69795", TestRuntimes.Mono)]
        public static void CreateTruncatingFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual(0.0, NumberBaseHelper<double>.CreateTruncating<nuint>(unchecked((nuint)0x0000000000000000)));
                AssertBitwiseEqual(1.0, NumberBaseHelper<double>.CreateTruncating<nuint>(unchecked((nuint)0x0000000000000001)));
                AssertBitwiseEqual(9223372036854775807.0, NumberBaseHelper<double>.CreateTruncating<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));

                // https://github.com/dotnet/roslyn/issues/60714
                // AssertBitwiseEqual(9223372036854775808.0, NumberBaseHelper<double>.CreateTruncating<nuint>(unchecked((nuint)0x8000000000000000)));
                // AssertBitwiseEqual(18446744073709551615.0, NumberBaseHelper<double>.CreateTruncating<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                AssertBitwiseEqual(0.0, NumberBaseHelper<double>.CreateTruncating<nuint>((nuint)0x00000000));
                AssertBitwiseEqual(1.0, NumberBaseHelper<double>.CreateTruncating<nuint>((nuint)0x00000001));
                AssertBitwiseEqual(2147483647.0, NumberBaseHelper<double>.CreateTruncating<nuint>((nuint)0x7FFFFFFF));

                // https://github.com/dotnet/roslyn/issues/60714
                // AssertBitwiseEqual(2147483648.0, NumberBaseHelper<double>.CreateTruncating<nuint>((nuint)0x80000000));
                // AssertBitwiseEqual(4294967295.0, NumberBaseHelper<double>.CreateTruncating<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void IsCanonicalTest()
        {
            Assert.True(NumberBaseHelper<double>.IsCanonical(double.NegativeInfinity));
            Assert.True(NumberBaseHelper<double>.IsCanonical(double.MinValue));
            Assert.True(NumberBaseHelper<double>.IsCanonical(-1.0));
            Assert.True(NumberBaseHelper<double>.IsCanonical(-MinNormal));
            Assert.True(NumberBaseHelper<double>.IsCanonical(-MaxSubnormal));
            Assert.True(NumberBaseHelper<double>.IsCanonical(-double.Epsilon));
            Assert.True(NumberBaseHelper<double>.IsCanonical(-0.0));
            Assert.True(NumberBaseHelper<double>.IsCanonical(double.NaN));
            Assert.True(NumberBaseHelper<double>.IsCanonical(0.0));
            Assert.True(NumberBaseHelper<double>.IsCanonical(double.Epsilon));
            Assert.True(NumberBaseHelper<double>.IsCanonical(MaxSubnormal));
            Assert.True(NumberBaseHelper<double>.IsCanonical(MinNormal));
            Assert.True(NumberBaseHelper<double>.IsCanonical(1.0));
            Assert.True(NumberBaseHelper<double>.IsCanonical(double.MaxValue));
            Assert.True(NumberBaseHelper<double>.IsCanonical(double.PositiveInfinity));
        }

        [Fact]
        public static void IsComplexNumberTest()
        {
            Assert.False(NumberBaseHelper<double>.IsComplexNumber(double.NegativeInfinity));
            Assert.False(NumberBaseHelper<double>.IsComplexNumber(double.MinValue));
            Assert.False(NumberBaseHelper<double>.IsComplexNumber(-1.0));
            Assert.False(NumberBaseHelper<double>.IsComplexNumber(-MinNormal));
            Assert.False(NumberBaseHelper<double>.IsComplexNumber(-MaxSubnormal));
            Assert.False(NumberBaseHelper<double>.IsComplexNumber(-double.Epsilon));
            Assert.False(NumberBaseHelper<double>.IsComplexNumber(-0.0));
            Assert.False(NumberBaseHelper<double>.IsComplexNumber(double.NaN));
            Assert.False(NumberBaseHelper<double>.IsComplexNumber(0.0));
            Assert.False(NumberBaseHelper<double>.IsComplexNumber(double.Epsilon));
            Assert.False(NumberBaseHelper<double>.IsComplexNumber(MaxSubnormal));
            Assert.False(NumberBaseHelper<double>.IsComplexNumber(MinNormal));
            Assert.False(NumberBaseHelper<double>.IsComplexNumber(1.0));
            Assert.False(NumberBaseHelper<double>.IsComplexNumber(double.MaxValue));
            Assert.False(NumberBaseHelper<double>.IsComplexNumber(double.PositiveInfinity));
        }

        [Fact]
        public static void IsEvenIntegerTest()
        {
            Assert.False(NumberBaseHelper<double>.IsEvenInteger(double.NegativeInfinity));
            Assert.True(NumberBaseHelper<double>.IsEvenInteger(double.MinValue));
            Assert.False(NumberBaseHelper<double>.IsEvenInteger(-1.0));
            Assert.False(NumberBaseHelper<double>.IsEvenInteger(-MinNormal));
            Assert.False(NumberBaseHelper<double>.IsEvenInteger(-MaxSubnormal));
            Assert.False(NumberBaseHelper<double>.IsEvenInteger(-double.Epsilon));
            Assert.True(NumberBaseHelper<double>.IsEvenInteger(-0.0));
            Assert.False(NumberBaseHelper<double>.IsEvenInteger(double.NaN));
            Assert.True(NumberBaseHelper<double>.IsEvenInteger(0.0));
            Assert.False(NumberBaseHelper<double>.IsEvenInteger(double.Epsilon));
            Assert.False(NumberBaseHelper<double>.IsEvenInteger(MaxSubnormal));
            Assert.False(NumberBaseHelper<double>.IsEvenInteger(MinNormal));
            Assert.False(NumberBaseHelper<double>.IsEvenInteger(1.0));
            Assert.True(NumberBaseHelper<double>.IsEvenInteger(double.MaxValue));
            Assert.False(NumberBaseHelper<double>.IsEvenInteger(double.PositiveInfinity));
        }

        [Fact]
        public static void IsFiniteTest()
        {
            Assert.False(NumberBaseHelper<double>.IsFinite(double.NegativeInfinity));
            Assert.True(NumberBaseHelper<double>.IsFinite(double.MinValue));
            Assert.True(NumberBaseHelper<double>.IsFinite(-1.0));
            Assert.True(NumberBaseHelper<double>.IsFinite(-MinNormal));
            Assert.True(NumberBaseHelper<double>.IsFinite(-MaxSubnormal));
            Assert.True(NumberBaseHelper<double>.IsFinite(-double.Epsilon));
            Assert.True(NumberBaseHelper<double>.IsFinite(-0.0));
            Assert.False(NumberBaseHelper<double>.IsFinite(double.NaN));
            Assert.True(NumberBaseHelper<double>.IsFinite(0.0));
            Assert.True(NumberBaseHelper<double>.IsFinite(double.Epsilon));
            Assert.True(NumberBaseHelper<double>.IsFinite(MaxSubnormal));
            Assert.True(NumberBaseHelper<double>.IsFinite(MinNormal));
            Assert.True(NumberBaseHelper<double>.IsFinite(1.0));
            Assert.True(NumberBaseHelper<double>.IsFinite(double.MaxValue));
            Assert.False(NumberBaseHelper<double>.IsFinite(double.PositiveInfinity));
        }

        [Fact]
        public static void IsImaginaryNumberTest()
        {
            Assert.False(NumberBaseHelper<double>.IsImaginaryNumber(double.NegativeInfinity));
            Assert.False(NumberBaseHelper<double>.IsImaginaryNumber(double.MinValue));
            Assert.False(NumberBaseHelper<double>.IsImaginaryNumber(-1.0));
            Assert.False(NumberBaseHelper<double>.IsImaginaryNumber(-MinNormal));
            Assert.False(NumberBaseHelper<double>.IsImaginaryNumber(-MaxSubnormal));
            Assert.False(NumberBaseHelper<double>.IsImaginaryNumber(-double.Epsilon));
            Assert.False(NumberBaseHelper<double>.IsImaginaryNumber(-0.0));
            Assert.False(NumberBaseHelper<double>.IsImaginaryNumber(double.NaN));
            Assert.False(NumberBaseHelper<double>.IsImaginaryNumber(0.0));
            Assert.False(NumberBaseHelper<double>.IsImaginaryNumber(double.Epsilon));
            Assert.False(NumberBaseHelper<double>.IsImaginaryNumber(MaxSubnormal));
            Assert.False(NumberBaseHelper<double>.IsImaginaryNumber(MinNormal));
            Assert.False(NumberBaseHelper<double>.IsImaginaryNumber(1.0));
            Assert.False(NumberBaseHelper<double>.IsImaginaryNumber(double.MaxValue));
            Assert.False(NumberBaseHelper<double>.IsImaginaryNumber(double.PositiveInfinity));
        }

        [Fact]
        public static void IsInfinityTest()
        {
            Assert.True(NumberBaseHelper<double>.IsInfinity(double.NegativeInfinity));
            Assert.False(NumberBaseHelper<double>.IsInfinity(double.MinValue));
            Assert.False(NumberBaseHelper<double>.IsInfinity(-1.0));
            Assert.False(NumberBaseHelper<double>.IsInfinity(-MinNormal));
            Assert.False(NumberBaseHelper<double>.IsInfinity(-MaxSubnormal));
            Assert.False(NumberBaseHelper<double>.IsInfinity(-double.Epsilon));
            Assert.False(NumberBaseHelper<double>.IsInfinity(-0.0));
            Assert.False(NumberBaseHelper<double>.IsInfinity(double.NaN));
            Assert.False(NumberBaseHelper<double>.IsInfinity(0.0));
            Assert.False(NumberBaseHelper<double>.IsInfinity(double.Epsilon));
            Assert.False(NumberBaseHelper<double>.IsInfinity(MaxSubnormal));
            Assert.False(NumberBaseHelper<double>.IsInfinity(MinNormal));
            Assert.False(NumberBaseHelper<double>.IsInfinity(1.0));
            Assert.False(NumberBaseHelper<double>.IsInfinity(double.MaxValue));
            Assert.True(NumberBaseHelper<double>.IsInfinity(double.PositiveInfinity));
        }

        [Fact]
        public static void IsIntegerTest()
        {
            Assert.False(NumberBaseHelper<double>.IsInteger(double.NegativeInfinity));
            Assert.True(NumberBaseHelper<double>.IsInteger(double.MinValue));
            Assert.True(NumberBaseHelper<double>.IsInteger(-1.0));
            Assert.False(NumberBaseHelper<double>.IsInteger(-MinNormal));
            Assert.False(NumberBaseHelper<double>.IsInteger(-MaxSubnormal));
            Assert.False(NumberBaseHelper<double>.IsInteger(-double.Epsilon));
            Assert.True(NumberBaseHelper<double>.IsInteger(-0.0));
            Assert.False(NumberBaseHelper<double>.IsInteger(double.NaN));
            Assert.True(NumberBaseHelper<double>.IsInteger(0.0));
            Assert.False(NumberBaseHelper<double>.IsInteger(double.Epsilon));
            Assert.False(NumberBaseHelper<double>.IsInteger(MaxSubnormal));
            Assert.False(NumberBaseHelper<double>.IsInteger(MinNormal));
            Assert.True(NumberBaseHelper<double>.IsInteger(1.0));
            Assert.True(NumberBaseHelper<double>.IsInteger(double.MaxValue));
            Assert.False(NumberBaseHelper<double>.IsInteger(double.PositiveInfinity));
        }

        [Fact]
        public static void IsNaNTest()
        {
            Assert.False(NumberBaseHelper<double>.IsNaN(double.NegativeInfinity));
            Assert.False(NumberBaseHelper<double>.IsNaN(double.MinValue));
            Assert.False(NumberBaseHelper<double>.IsNaN(-1.0));
            Assert.False(NumberBaseHelper<double>.IsNaN(-MinNormal));
            Assert.False(NumberBaseHelper<double>.IsNaN(-MaxSubnormal));
            Assert.False(NumberBaseHelper<double>.IsNaN(-double.Epsilon));
            Assert.False(NumberBaseHelper<double>.IsNaN(-0.0));
            Assert.True(NumberBaseHelper<double>.IsNaN(double.NaN));
            Assert.False(NumberBaseHelper<double>.IsNaN(0.0));
            Assert.False(NumberBaseHelper<double>.IsNaN(double.Epsilon));
            Assert.False(NumberBaseHelper<double>.IsNaN(MaxSubnormal));
            Assert.False(NumberBaseHelper<double>.IsNaN(MinNormal));
            Assert.False(NumberBaseHelper<double>.IsNaN(1.0));
            Assert.False(NumberBaseHelper<double>.IsNaN(double.MaxValue));
            Assert.False(NumberBaseHelper<double>.IsNaN(double.PositiveInfinity));
        }

        [Fact]
        public static void IsNegativeTest()
        {
            Assert.True(NumberBaseHelper<double>.IsNegative(double.NegativeInfinity));
            Assert.True(NumberBaseHelper<double>.IsNegative(double.MinValue));
            Assert.True(NumberBaseHelper<double>.IsNegative(-1.0));
            Assert.True(NumberBaseHelper<double>.IsNegative(-MinNormal));
            Assert.True(NumberBaseHelper<double>.IsNegative(-MaxSubnormal));
            Assert.True(NumberBaseHelper<double>.IsNegative(-double.Epsilon));
            Assert.True(NumberBaseHelper<double>.IsNegative(-0.0));
            Assert.True(NumberBaseHelper<double>.IsNegative(double.NaN));
            Assert.False(NumberBaseHelper<double>.IsNegative(0.0));
            Assert.False(NumberBaseHelper<double>.IsNegative(double.Epsilon));
            Assert.False(NumberBaseHelper<double>.IsNegative(MaxSubnormal));
            Assert.False(NumberBaseHelper<double>.IsNegative(MinNormal));
            Assert.False(NumberBaseHelper<double>.IsNegative(1.0));
            Assert.False(NumberBaseHelper<double>.IsNegative(double.MaxValue));
            Assert.False(NumberBaseHelper<double>.IsNegative(double.PositiveInfinity));
        }

        [Fact]
        public static void IsNegativeInfinityTest()
        {
            Assert.True(NumberBaseHelper<double>.IsNegativeInfinity(double.NegativeInfinity));
            Assert.False(NumberBaseHelper<double>.IsNegativeInfinity(double.MinValue));
            Assert.False(NumberBaseHelper<double>.IsNegativeInfinity(-1.0));
            Assert.False(NumberBaseHelper<double>.IsNegativeInfinity(-MinNormal));
            Assert.False(NumberBaseHelper<double>.IsNegativeInfinity(-MaxSubnormal));
            Assert.False(NumberBaseHelper<double>.IsNegativeInfinity(-double.Epsilon));
            Assert.False(NumberBaseHelper<double>.IsNegativeInfinity(-0.0));
            Assert.False(NumberBaseHelper<double>.IsNegativeInfinity(double.NaN));
            Assert.False(NumberBaseHelper<double>.IsNegativeInfinity(0.0));
            Assert.False(NumberBaseHelper<double>.IsNegativeInfinity(double.Epsilon));
            Assert.False(NumberBaseHelper<double>.IsNegativeInfinity(MaxSubnormal));
            Assert.False(NumberBaseHelper<double>.IsNegativeInfinity(MinNormal));
            Assert.False(NumberBaseHelper<double>.IsNegativeInfinity(1.0));
            Assert.False(NumberBaseHelper<double>.IsNegativeInfinity(double.MaxValue));
            Assert.False(NumberBaseHelper<double>.IsNegativeInfinity(double.PositiveInfinity));
        }

        [Fact]
        public static void IsNormalTest()
        {
            Assert.False(NumberBaseHelper<double>.IsNormal(double.NegativeInfinity));
            Assert.True(NumberBaseHelper<double>.IsNormal(double.MinValue));
            Assert.True(NumberBaseHelper<double>.IsNormal(-1.0));
            Assert.True(NumberBaseHelper<double>.IsNormal(-MinNormal));
            Assert.False(NumberBaseHelper<double>.IsNormal(-MaxSubnormal));
            Assert.False(NumberBaseHelper<double>.IsNormal(-double.Epsilon));
            Assert.False(NumberBaseHelper<double>.IsNormal(-0.0));
            Assert.False(NumberBaseHelper<double>.IsNormal(double.NaN));
            Assert.False(NumberBaseHelper<double>.IsNormal(0.0));
            Assert.False(NumberBaseHelper<double>.IsNormal(double.Epsilon));
            Assert.False(NumberBaseHelper<double>.IsNormal(MaxSubnormal));
            Assert.True(NumberBaseHelper<double>.IsNormal(MinNormal));
            Assert.True(NumberBaseHelper<double>.IsNormal(1.0));
            Assert.True(NumberBaseHelper<double>.IsNormal(double.MaxValue));
            Assert.False(NumberBaseHelper<double>.IsNormal(double.PositiveInfinity));
        }

        [Fact]
        public static void IsOddIntegerTest()
        {
            Assert.False(NumberBaseHelper<double>.IsOddInteger(double.NegativeInfinity));
            Assert.False(NumberBaseHelper<double>.IsOddInteger(double.MinValue));
            Assert.True(NumberBaseHelper<double>.IsOddInteger(-1.0));
            Assert.False(NumberBaseHelper<double>.IsOddInteger(-MinNormal));
            Assert.False(NumberBaseHelper<double>.IsOddInteger(-MaxSubnormal));
            Assert.False(NumberBaseHelper<double>.IsOddInteger(-double.Epsilon));
            Assert.False(NumberBaseHelper<double>.IsOddInteger(-0.0));
            Assert.False(NumberBaseHelper<double>.IsOddInteger(double.NaN));
            Assert.False(NumberBaseHelper<double>.IsOddInteger(0.0));
            Assert.False(NumberBaseHelper<double>.IsOddInteger(double.Epsilon));
            Assert.False(NumberBaseHelper<double>.IsOddInteger(MaxSubnormal));
            Assert.False(NumberBaseHelper<double>.IsOddInteger(MinNormal));
            Assert.True(NumberBaseHelper<double>.IsOddInteger(1.0));
            Assert.False(NumberBaseHelper<double>.IsOddInteger(double.MaxValue));
            Assert.False(NumberBaseHelper<double>.IsOddInteger(double.PositiveInfinity));
        }

        [Fact]
        public static void IsPositiveTest()
        {
            Assert.False(NumberBaseHelper<double>.IsPositive(double.NegativeInfinity));
            Assert.False(NumberBaseHelper<double>.IsPositive(double.MinValue));
            Assert.False(NumberBaseHelper<double>.IsPositive(-1.0));
            Assert.False(NumberBaseHelper<double>.IsPositive(-MinNormal));
            Assert.False(NumberBaseHelper<double>.IsPositive(-MaxSubnormal));
            Assert.False(NumberBaseHelper<double>.IsPositive(-double.Epsilon));
            Assert.False(NumberBaseHelper<double>.IsPositive(-0.0));
            Assert.False(NumberBaseHelper<double>.IsPositive(double.NaN));
            Assert.True(NumberBaseHelper<double>.IsPositive(0.0));
            Assert.True(NumberBaseHelper<double>.IsPositive(double.Epsilon));
            Assert.True(NumberBaseHelper<double>.IsPositive(MaxSubnormal));
            Assert.True(NumberBaseHelper<double>.IsPositive(MinNormal));
            Assert.True(NumberBaseHelper<double>.IsPositive(1.0));
            Assert.True(NumberBaseHelper<double>.IsPositive(double.MaxValue));
            Assert.True(NumberBaseHelper<double>.IsPositive(double.PositiveInfinity));
        }

        [Fact]
        public static void IsPositiveInfinityTest()
        {
            Assert.False(NumberBaseHelper<double>.IsPositiveInfinity(double.NegativeInfinity));
            Assert.False(NumberBaseHelper<double>.IsPositiveInfinity(double.MinValue));
            Assert.False(NumberBaseHelper<double>.IsPositiveInfinity(-1.0));
            Assert.False(NumberBaseHelper<double>.IsPositiveInfinity(-MinNormal));
            Assert.False(NumberBaseHelper<double>.IsPositiveInfinity(-MaxSubnormal));
            Assert.False(NumberBaseHelper<double>.IsPositiveInfinity(-double.Epsilon));
            Assert.False(NumberBaseHelper<double>.IsPositiveInfinity(-0.0));
            Assert.False(NumberBaseHelper<double>.IsPositiveInfinity(double.NaN));
            Assert.False(NumberBaseHelper<double>.IsPositiveInfinity(0.0));
            Assert.False(NumberBaseHelper<double>.IsPositiveInfinity(double.Epsilon));
            Assert.False(NumberBaseHelper<double>.IsPositiveInfinity(MaxSubnormal));
            Assert.False(NumberBaseHelper<double>.IsPositiveInfinity(MinNormal));
            Assert.False(NumberBaseHelper<double>.IsPositiveInfinity(1.0));
            Assert.False(NumberBaseHelper<double>.IsPositiveInfinity(double.MaxValue));
            Assert.True(NumberBaseHelper<double>.IsPositiveInfinity(double.PositiveInfinity));
        }

        [Fact]
        public static void IsRealNumberTest()
        {
            Assert.True(NumberBaseHelper<double>.IsRealNumber(double.NegativeInfinity));
            Assert.True(NumberBaseHelper<double>.IsRealNumber(double.MinValue));
            Assert.True(NumberBaseHelper<double>.IsRealNumber(-1.0));
            Assert.True(NumberBaseHelper<double>.IsRealNumber(-MinNormal));
            Assert.True(NumberBaseHelper<double>.IsRealNumber(-MaxSubnormal));
            Assert.True(NumberBaseHelper<double>.IsRealNumber(-double.Epsilon));
            Assert.True(NumberBaseHelper<double>.IsRealNumber(-0.0));
            Assert.False(NumberBaseHelper<double>.IsRealNumber(double.NaN));
            Assert.True(NumberBaseHelper<double>.IsRealNumber(0.0));
            Assert.True(NumberBaseHelper<double>.IsRealNumber(double.Epsilon));
            Assert.True(NumberBaseHelper<double>.IsRealNumber(MaxSubnormal));
            Assert.True(NumberBaseHelper<double>.IsRealNumber(MinNormal));
            Assert.True(NumberBaseHelper<double>.IsRealNumber(1.0));
            Assert.True(NumberBaseHelper<double>.IsRealNumber(double.MaxValue));
            Assert.True(NumberBaseHelper<double>.IsRealNumber(double.PositiveInfinity));
        }

        [Fact]
        public static void IsSubnormalTest()
        {
            Assert.False(NumberBaseHelper<double>.IsSubnormal(double.NegativeInfinity));
            Assert.False(NumberBaseHelper<double>.IsSubnormal(double.MinValue));
            Assert.False(NumberBaseHelper<double>.IsSubnormal(-1.0));
            Assert.False(NumberBaseHelper<double>.IsSubnormal(-MinNormal));
            Assert.True(NumberBaseHelper<double>.IsSubnormal(-MaxSubnormal));
            Assert.True(NumberBaseHelper<double>.IsSubnormal(-double.Epsilon));
            Assert.False(NumberBaseHelper<double>.IsSubnormal(-0.0));
            Assert.False(NumberBaseHelper<double>.IsSubnormal(double.NaN));
            Assert.False(NumberBaseHelper<double>.IsSubnormal(0.0));
            Assert.True(NumberBaseHelper<double>.IsSubnormal(double.Epsilon));
            Assert.True(NumberBaseHelper<double>.IsSubnormal(MaxSubnormal));
            Assert.False(NumberBaseHelper<double>.IsSubnormal(MinNormal));
            Assert.False(NumberBaseHelper<double>.IsSubnormal(1.0));
            Assert.False(NumberBaseHelper<double>.IsSubnormal(double.MaxValue));
            Assert.False(NumberBaseHelper<double>.IsSubnormal(double.PositiveInfinity));
        }

        [Fact]
        public static void IsZeroTest()
        {
            Assert.False(NumberBaseHelper<double>.IsZero(double.NegativeInfinity));
            Assert.False(NumberBaseHelper<double>.IsZero(double.MinValue));
            Assert.False(NumberBaseHelper<double>.IsZero(-1.0));
            Assert.False(NumberBaseHelper<double>.IsZero(-MinNormal));
            Assert.False(NumberBaseHelper<double>.IsZero(-MaxSubnormal));
            Assert.False(NumberBaseHelper<double>.IsZero(-double.Epsilon));
            Assert.True(NumberBaseHelper<double>.IsZero(-0.0));
            Assert.False(NumberBaseHelper<double>.IsZero(double.NaN));
            Assert.True(NumberBaseHelper<double>.IsZero(0.0));
            Assert.False(NumberBaseHelper<double>.IsZero(double.Epsilon));
            Assert.False(NumberBaseHelper<double>.IsZero(MaxSubnormal));
            Assert.False(NumberBaseHelper<double>.IsZero(MinNormal));
            Assert.False(NumberBaseHelper<double>.IsZero(1.0));
            Assert.False(NumberBaseHelper<double>.IsZero(double.MaxValue));
            Assert.False(NumberBaseHelper<double>.IsZero(double.PositiveInfinity));
        }

        [Fact]
        public static void MaxMagnitudeTest()
        {
            AssertBitwiseEqual(double.NegativeInfinity, NumberBaseHelper<double>.MaxMagnitude(double.NegativeInfinity, 1.0));
            AssertBitwiseEqual(double.MinValue, NumberBaseHelper<double>.MaxMagnitude(double.MinValue, 1.0));
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.MaxMagnitude(-1.0, 1.0));
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.MaxMagnitude(-MinNormal, 1.0));
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.MaxMagnitude(-MaxSubnormal, 1.0));
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.MaxMagnitude(-double.Epsilon, 1.0));
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.MaxMagnitude(-0.0, 1.0));
            AssertBitwiseEqual(double.NaN, NumberBaseHelper<double>.MaxMagnitude(double.NaN, 1.0));
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.MaxMagnitude(0.0, 1.0));
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.MaxMagnitude(double.Epsilon, 1.0));
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.MaxMagnitude(MaxSubnormal, 1.0));
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.MaxMagnitude(MinNormal, 1.0));
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.MaxMagnitude(1.0, 1.0));
            AssertBitwiseEqual(double.MaxValue, NumberBaseHelper<double>.MaxMagnitude(double.MaxValue, 1.0));
            AssertBitwiseEqual(double.PositiveInfinity, NumberBaseHelper<double>.MaxMagnitude(double.PositiveInfinity, 1.0));
        }

        [Fact]
        public static void MaxMagnitudeNumberTest()
        {
            AssertBitwiseEqual(double.NegativeInfinity, NumberBaseHelper<double>.MaxMagnitudeNumber(double.NegativeInfinity, 1.0));
            AssertBitwiseEqual(double.MinValue, NumberBaseHelper<double>.MaxMagnitudeNumber(double.MinValue, 1.0));
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.MaxMagnitudeNumber(-1.0, 1.0));
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.MaxMagnitudeNumber(-MinNormal, 1.0));
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.MaxMagnitudeNumber(-MaxSubnormal, 1.0));
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.MaxMagnitudeNumber(-double.Epsilon, 1.0));
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.MaxMagnitudeNumber(-0.0, 1.0));
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.MaxMagnitudeNumber(double.NaN, 1.0));
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.MaxMagnitudeNumber(0.0, 1.0));
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.MaxMagnitudeNumber(double.Epsilon, 1.0));
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.MaxMagnitudeNumber(MaxSubnormal, 1.0));
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.MaxMagnitudeNumber(MinNormal, 1.0));
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.MaxMagnitudeNumber(1.0, 1.0));
            AssertBitwiseEqual(double.MaxValue, NumberBaseHelper<double>.MaxMagnitudeNumber(double.MaxValue, 1.0));
            AssertBitwiseEqual(double.PositiveInfinity, NumberBaseHelper<double>.MaxMagnitudeNumber(double.PositiveInfinity, 1.0));
        }

        [Fact]
        public static void MinMagnitudeTest()
        {
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.MinMagnitude(double.NegativeInfinity, 1.0));
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.MinMagnitude(double.MinValue, 1.0));
            AssertBitwiseEqual(-1.0, NumberBaseHelper<double>.MinMagnitude(-1.0, 1.0));
            AssertBitwiseEqual(-MinNormal, NumberBaseHelper<double>.MinMagnitude(-MinNormal, 1.0));
            AssertBitwiseEqual(-MaxSubnormal, NumberBaseHelper<double>.MinMagnitude(-MaxSubnormal, 1.0));
            AssertBitwiseEqual(-double.Epsilon, NumberBaseHelper<double>.MinMagnitude(-double.Epsilon, 1.0));
            AssertBitwiseEqual(-0.0, NumberBaseHelper<double>.MinMagnitude(-0.0, 1.0));
            AssertBitwiseEqual(double.NaN, NumberBaseHelper<double>.MinMagnitude(double.NaN, 1.0));
            AssertBitwiseEqual(0.0, NumberBaseHelper<double>.MinMagnitude(0.0, 1.0));
            AssertBitwiseEqual(double.Epsilon, NumberBaseHelper<double>.MinMagnitude(double.Epsilon, 1.0));
            AssertBitwiseEqual(MaxSubnormal, NumberBaseHelper<double>.MinMagnitude(MaxSubnormal, 1.0));
            AssertBitwiseEqual(MinNormal, NumberBaseHelper<double>.MinMagnitude(MinNormal, 1.0));
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.MinMagnitude(1.0, 1.0));
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.MinMagnitude(double.MaxValue, 1.0));
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.MinMagnitude(double.PositiveInfinity, 1.0));
        }

        [Fact]
        public static void MinMagnitudeNumberTest()
        {
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.MinMagnitudeNumber(double.NegativeInfinity, 1.0));
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.MinMagnitudeNumber(double.MinValue, 1.0));
            AssertBitwiseEqual(-1.0, NumberBaseHelper<double>.MinMagnitudeNumber(-1.0, 1.0));
            AssertBitwiseEqual(-MinNormal, NumberBaseHelper<double>.MinMagnitudeNumber(-MinNormal, 1.0));
            AssertBitwiseEqual(-MaxSubnormal, NumberBaseHelper<double>.MinMagnitudeNumber(-MaxSubnormal, 1.0));
            AssertBitwiseEqual(-double.Epsilon, NumberBaseHelper<double>.MinMagnitudeNumber(-double.Epsilon, 1.0));
            AssertBitwiseEqual(-0.0, NumberBaseHelper<double>.MinMagnitudeNumber(-0.0, 1.0));
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.MinMagnitudeNumber(double.NaN, 1.0));
            AssertBitwiseEqual(0.0, NumberBaseHelper<double>.MinMagnitudeNumber(0.0, 1.0));
            AssertBitwiseEqual(double.Epsilon, NumberBaseHelper<double>.MinMagnitudeNumber(double.Epsilon, 1.0));
            AssertBitwiseEqual(MaxSubnormal, NumberBaseHelper<double>.MinMagnitudeNumber(MaxSubnormal, 1.0));
            AssertBitwiseEqual(MinNormal, NumberBaseHelper<double>.MinMagnitudeNumber(MinNormal, 1.0));
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.MinMagnitudeNumber(1.0, 1.0));
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.MinMagnitudeNumber(double.MaxValue, 1.0));
            AssertBitwiseEqual(1.0, NumberBaseHelper<double>.MinMagnitudeNumber(double.PositiveInfinity, 1.0));
        }

        //
        // ISignedNumber
        //

        [Fact]
        public static void NegativeOneTest()
        {
            Assert.Equal(-1.0, SignedNumberHelper<double>.NegativeOne);
        }

        //
        // ISubtractionOperators
        //

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

        //
        // IUnaryNegationOperators
        //

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

        //
        // IUnaryPlusOperators
        //

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

        //
        // IParsable and ISpanParsable
        //

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
                    Assert.True(ParsableHelper<double>.TryParse(value, null, out result));
                    Assert.Equal(expected, result);

                    Assert.Equal(expected, ParsableHelper<double>.Parse(value, null));
                }

                Assert.Equal(expected, ParsableHelper<double>.Parse(value, provider));
            }

            // Use Parse(string, NumberStyles, IFormatProvider)
            Assert.True(NumberBaseHelper<double>.TryParse(value, style, provider, out result));
            Assert.Equal(expected, result);

            Assert.Equal(expected, NumberBaseHelper<double>.Parse(value, style, provider));

            if (isDefaultProvider)
            {
                // Use Parse(string, NumberStyles) or Parse(string, NumberStyles, IFormatProvider)
                Assert.True(NumberBaseHelper<double>.TryParse(value, style, NumberFormatInfo.CurrentInfo, out result));
                Assert.Equal(expected, result);

                Assert.Equal(expected, NumberBaseHelper<double>.Parse(value, style, null));
                Assert.Equal(expected, NumberBaseHelper<double>.Parse(value, style, NumberFormatInfo.CurrentInfo));
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
                    Assert.False(ParsableHelper<double>.TryParse(value, null, out result));
                    Assert.Equal(default(double), result);

                    Assert.Throws(exceptionType, () => ParsableHelper<double>.Parse(value, null));
                }

                Assert.Throws(exceptionType, () => ParsableHelper<double>.Parse(value, provider));
            }

            // Use Parse(string, NumberStyles, IFormatProvider)
            Assert.False(NumberBaseHelper<double>.TryParse(value, style, provider, out result));
            Assert.Equal(default(double), result);

            Assert.Throws(exceptionType, () => NumberBaseHelper<double>.Parse(value, style, provider));

            if (isDefaultProvider)
            {
                // Use Parse(string, NumberStyles) or Parse(string, NumberStyles, IFormatProvider)
                Assert.False(NumberBaseHelper<double>.TryParse(value, style, NumberFormatInfo.CurrentInfo, out result));
                Assert.Equal(default(double), result);

                Assert.Throws(exceptionType, () => NumberBaseHelper<double>.Parse(value, style, null));
                Assert.Throws(exceptionType, () => NumberBaseHelper<double>.Parse(value, style, NumberFormatInfo.CurrentInfo));
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
                    Assert.True(SpanParsableHelper<double>.TryParse(value.AsSpan(offset, count), null, out result));
                    Assert.Equal(expected, result);

                    Assert.Equal(expected, SpanParsableHelper<double>.Parse(value.AsSpan(offset, count), null));
                }

                Assert.Equal(expected, SpanParsableHelper<double>.Parse(value.AsSpan(offset, count), provider: provider));
            }

            Assert.Equal(expected, NumberBaseHelper<double>.Parse(value.AsSpan(offset, count), style, provider));

            Assert.True(NumberBaseHelper<double>.TryParse(value.AsSpan(offset, count), style, provider, out result));
            Assert.Equal(expected, result);
        }

        [Theory]
        [MemberData(nameof(DoubleTests.Parse_Invalid_TestData), MemberType = typeof(DoubleTests))]
        public static void ParseInvalidSpanTest(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            if (value != null)
            {
                Assert.Throws(exceptionType, () => NumberBaseHelper<double>.Parse(value.AsSpan(), style, provider));

                Assert.False(NumberBaseHelper<double>.TryParse(value.AsSpan(), style, provider, out double result));
                Assert.Equal(0, result);
            }
        }
    }
}
