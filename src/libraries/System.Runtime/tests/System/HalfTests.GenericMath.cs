// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Xunit;

namespace System.Tests
{
    public class HalfTests_GenericMath
    {
        private static Half MinNormal => BitConverter.UInt16BitsToHalf(0x0400);

        private static Half MaxSubnormal => BitConverter.UInt16BitsToHalf(0x03FF);

        private static Half NegativeOne => BitConverter.UInt16BitsToHalf(0xBC00);

        private static Half NegativeTwo => BitConverter.UInt16BitsToHalf(0xC000);

        private static Half NegativeZero => BitConverter.UInt16BitsToHalf(0x8000);

        private static Half PositiveOne => BitConverter.UInt16BitsToHalf(0x3C00);

        private static Half PositiveTwo => BitConverter.UInt16BitsToHalf(0x4000);

        private static Half PositiveZero => BitConverter.UInt16BitsToHalf(0x0000);

        private static void AssertBitwiseEqual(Half expected, Half actual)
        {
            ushort expectedBits = BitConverter.HalfToUInt16Bits(expected);
            ushort actualBits = BitConverter.HalfToUInt16Bits(actual);

            if (expectedBits == actualBits)
            {
                return;
            }

            if (Half.IsNaN(expected) && Half.IsNaN(actual))
            {
                return;
            }

            throw new Xunit.Sdk.EqualException(expected, actual);
        }

        [Fact]
        public static void AdditiveIdentityTest()
        {
            AssertBitwiseEqual(PositiveZero, AdditiveIdentityHelper<Half, Half>.AdditiveIdentity);
        }

        [Fact]
        public static void MinValueTest()
        {
            AssertBitwiseEqual(Half.MinValue, MinMaxValueHelper<Half>.MinValue);
        }

        [Fact]
        public static void MaxValueTest()
        {
            AssertBitwiseEqual(Half.MaxValue, MinMaxValueHelper<Half>.MaxValue);
        }

        [Fact]
        public static void MultiplicativeIdentityTest()
        {
            AssertBitwiseEqual(PositiveOne, MultiplicativeIdentityHelper<Half, Half>.MultiplicativeIdentity);
        }

        [Fact]
        public static void NegativeOneTest()
        {
            Assert.Equal(NegativeOne, SignedNumberHelper<Half>.NegativeOne);
        }

        [Fact]
        public static void OneTest()
        {
            AssertBitwiseEqual(PositiveOne, NumberBaseHelper<Half>.One);
        }

        [Fact]
        public static void ZeroTest()
        {
            AssertBitwiseEqual(PositiveZero, NumberBaseHelper<Half>.Zero);
        }

        [Fact]
        public static void op_AdditionTest()
        {
            AssertBitwiseEqual(Half.NegativeInfinity, AdditionOperatorsHelper<Half, Half, Half>.op_Addition(Half.NegativeInfinity, PositiveOne));
            AssertBitwiseEqual(Half.MinValue, AdditionOperatorsHelper<Half, Half, Half>.op_Addition(Half.MinValue, PositiveOne));
            AssertBitwiseEqual(PositiveZero, AdditionOperatorsHelper<Half, Half, Half>.op_Addition(NegativeOne, PositiveOne));
            AssertBitwiseEqual(PositiveOne, AdditionOperatorsHelper<Half, Half, Half>.op_Addition(-MinNormal, PositiveOne));
            AssertBitwiseEqual(PositiveOne, AdditionOperatorsHelper<Half, Half, Half>.op_Addition(-MaxSubnormal, PositiveOne));
            AssertBitwiseEqual(PositiveOne, AdditionOperatorsHelper<Half, Half, Half>.op_Addition(-Half.Epsilon, PositiveOne));
            AssertBitwiseEqual(PositiveOne, AdditionOperatorsHelper<Half, Half, Half>.op_Addition(NegativeZero, PositiveOne));
            AssertBitwiseEqual(Half.NaN, AdditionOperatorsHelper<Half, Half, Half>.op_Addition(Half.NaN, PositiveOne));
            AssertBitwiseEqual(PositiveOne, AdditionOperatorsHelper<Half, Half, Half>.op_Addition(PositiveZero, PositiveOne));
            AssertBitwiseEqual(PositiveOne, AdditionOperatorsHelper<Half, Half, Half>.op_Addition(Half.Epsilon, PositiveOne));
            AssertBitwiseEqual(PositiveOne, AdditionOperatorsHelper<Half, Half, Half>.op_Addition(MaxSubnormal, PositiveOne));
            AssertBitwiseEqual(PositiveOne, AdditionOperatorsHelper<Half, Half, Half>.op_Addition(MinNormal, PositiveOne));
            AssertBitwiseEqual(PositiveTwo, AdditionOperatorsHelper<Half, Half, Half>.op_Addition(PositiveOne, PositiveOne));
            AssertBitwiseEqual(Half.MaxValue, AdditionOperatorsHelper<Half, Half, Half>.op_Addition(Half.MaxValue, PositiveOne));
            AssertBitwiseEqual(Half.PositiveInfinity, AdditionOperatorsHelper<Half, Half, Half>.op_Addition(Half.PositiveInfinity, PositiveOne));
        }

        [Fact]
        public static void op_CheckedAdditionTest()
        {
            AssertBitwiseEqual(Half.NegativeInfinity, AdditionOperatorsHelper<Half, Half, Half>.op_CheckedAddition(Half.NegativeInfinity, PositiveOne));
            AssertBitwiseEqual(Half.MinValue, AdditionOperatorsHelper<Half, Half, Half>.op_CheckedAddition(Half.MinValue, PositiveOne));
            AssertBitwiseEqual(PositiveZero, AdditionOperatorsHelper<Half, Half, Half>.op_CheckedAddition(NegativeOne, PositiveOne));
            AssertBitwiseEqual(PositiveOne, AdditionOperatorsHelper<Half, Half, Half>.op_CheckedAddition(-MinNormal, PositiveOne));
            AssertBitwiseEqual(PositiveOne, AdditionOperatorsHelper<Half, Half, Half>.op_CheckedAddition(-MaxSubnormal, PositiveOne));
            AssertBitwiseEqual(PositiveOne, AdditionOperatorsHelper<Half, Half, Half>.op_CheckedAddition(-Half.Epsilon, PositiveOne));
            AssertBitwiseEqual(PositiveOne, AdditionOperatorsHelper<Half, Half, Half>.op_CheckedAddition(NegativeZero, PositiveOne));
            AssertBitwiseEqual(Half.NaN, AdditionOperatorsHelper<Half, Half, Half>.op_CheckedAddition(Half.NaN, PositiveOne));
            AssertBitwiseEqual(PositiveOne, AdditionOperatorsHelper<Half, Half, Half>.op_CheckedAddition(PositiveZero, PositiveOne));
            AssertBitwiseEqual(PositiveOne, AdditionOperatorsHelper<Half, Half, Half>.op_CheckedAddition(Half.Epsilon, PositiveOne));
            AssertBitwiseEqual(PositiveOne, AdditionOperatorsHelper<Half, Half, Half>.op_CheckedAddition(MaxSubnormal, PositiveOne));
            AssertBitwiseEqual(PositiveOne, AdditionOperatorsHelper<Half, Half, Half>.op_CheckedAddition(MinNormal, PositiveOne));
            AssertBitwiseEqual(PositiveTwo, AdditionOperatorsHelper<Half, Half, Half>.op_CheckedAddition(PositiveOne, PositiveOne));
            AssertBitwiseEqual(Half.MaxValue, AdditionOperatorsHelper<Half, Half, Half>.op_CheckedAddition(Half.MaxValue, PositiveOne));
            AssertBitwiseEqual(Half.PositiveInfinity, AdditionOperatorsHelper<Half, Half, Half>.op_CheckedAddition(Half.PositiveInfinity, PositiveOne));
        }

        [Fact]
        public static void IsPow2Test()
        {
            Assert.False(BinaryNumberHelper<Half>.IsPow2(Half.NegativeInfinity));
            Assert.False(BinaryNumberHelper<Half>.IsPow2(Half.MinValue));
            Assert.False(BinaryNumberHelper<Half>.IsPow2(NegativeOne));
            Assert.False(BinaryNumberHelper<Half>.IsPow2(-MinNormal));
            Assert.False(BinaryNumberHelper<Half>.IsPow2(-MaxSubnormal));
            Assert.False(BinaryNumberHelper<Half>.IsPow2(-Half.Epsilon));
            Assert.False(BinaryNumberHelper<Half>.IsPow2(NegativeZero));
            Assert.False(BinaryNumberHelper<Half>.IsPow2(Half.NaN));
            Assert.False(BinaryNumberHelper<Half>.IsPow2(PositiveZero));
            Assert.False(BinaryNumberHelper<Half>.IsPow2(Half.Epsilon));
            Assert.False(BinaryNumberHelper<Half>.IsPow2(MaxSubnormal));
            Assert.True(BinaryNumberHelper<Half>.IsPow2(MinNormal));
            Assert.True(BinaryNumberHelper<Half>.IsPow2(PositiveOne));
            Assert.False(BinaryNumberHelper<Half>.IsPow2(Half.MaxValue));
            Assert.False(BinaryNumberHelper<Half>.IsPow2(Half.PositiveInfinity));
        }

        [Fact]
        public static void Log2Test()
        {
            AssertBitwiseEqual(Half.NaN, BinaryNumberHelper<Half>.Log2(Half.NegativeInfinity));
            AssertBitwiseEqual(Half.NaN, BinaryNumberHelper<Half>.Log2(Half.MinValue));
            AssertBitwiseEqual(Half.NaN, BinaryNumberHelper<Half>.Log2(NegativeOne));
            AssertBitwiseEqual(Half.NaN, BinaryNumberHelper<Half>.Log2(-MinNormal));
            AssertBitwiseEqual(Half.NaN, BinaryNumberHelper<Half>.Log2(-MaxSubnormal));
            AssertBitwiseEqual(Half.NaN, BinaryNumberHelper<Half>.Log2(-Half.Epsilon));
            AssertBitwiseEqual(Half.NegativeInfinity, BinaryNumberHelper<Half>.Log2(NegativeZero));
            AssertBitwiseEqual(Half.NaN, BinaryNumberHelper<Half>.Log2(Half.NaN));
            AssertBitwiseEqual(Half.NegativeInfinity, BinaryNumberHelper<Half>.Log2(PositiveZero));
            AssertBitwiseEqual((Half)(-24.0f), BinaryNumberHelper<Half>.Log2(Half.Epsilon));
            AssertBitwiseEqual((Half)(-14.0f), BinaryNumberHelper<Half>.Log2(MaxSubnormal));
            AssertBitwiseEqual((Half)(-14.0f), BinaryNumberHelper<Half>.Log2(MinNormal));
            AssertBitwiseEqual(PositiveZero, BinaryNumberHelper<Half>.Log2(PositiveOne));
            AssertBitwiseEqual((Half)16.0f, BinaryNumberHelper<Half>.Log2(Half.MaxValue));
            AssertBitwiseEqual(Half.PositiveInfinity, BinaryNumberHelper<Half>.Log2(Half.PositiveInfinity));
        }

        [Fact]
        public static void op_LessThanTest()
        {
            Assert.True(ComparisonOperatorsHelper<Half, Half>.op_LessThan(Half.NegativeInfinity, PositiveOne));
            Assert.True(ComparisonOperatorsHelper<Half, Half>.op_LessThan(Half.MinValue, PositiveOne));
            Assert.True(ComparisonOperatorsHelper<Half, Half>.op_LessThan(NegativeOne, PositiveOne));
            Assert.True(ComparisonOperatorsHelper<Half, Half>.op_LessThan(-MinNormal, PositiveOne));
            Assert.True(ComparisonOperatorsHelper<Half, Half>.op_LessThan(-MaxSubnormal, PositiveOne));
            Assert.True(ComparisonOperatorsHelper<Half, Half>.op_LessThan(-Half.Epsilon, PositiveOne));
            Assert.True(ComparisonOperatorsHelper<Half, Half>.op_LessThan(NegativeZero, PositiveOne));
            Assert.False(ComparisonOperatorsHelper<Half, Half>.op_LessThan(Half.NaN, PositiveOne));
            Assert.True(ComparisonOperatorsHelper<Half, Half>.op_LessThan(PositiveZero, PositiveOne));
            Assert.True(ComparisonOperatorsHelper<Half, Half>.op_LessThan(Half.Epsilon, PositiveOne));
            Assert.True(ComparisonOperatorsHelper<Half, Half>.op_LessThan(MaxSubnormal, PositiveOne));
            Assert.True(ComparisonOperatorsHelper<Half, Half>.op_LessThan(MinNormal, PositiveOne));
            Assert.False(ComparisonOperatorsHelper<Half, Half>.op_LessThan(PositiveOne, PositiveOne));
            Assert.False(ComparisonOperatorsHelper<Half, Half>.op_LessThan(Half.MaxValue, PositiveOne));
            Assert.False(ComparisonOperatorsHelper<Half, Half>.op_LessThan(Half.PositiveInfinity, PositiveOne));
        }

        [Fact]
        public static void op_LessThanOrEqualTest()
        {
            Assert.True(ComparisonOperatorsHelper<Half, Half>.op_LessThanOrEqual(Half.NegativeInfinity, PositiveOne));
            Assert.True(ComparisonOperatorsHelper<Half, Half>.op_LessThanOrEqual(Half.MinValue, PositiveOne));
            Assert.True(ComparisonOperatorsHelper<Half, Half>.op_LessThanOrEqual(NegativeOne, PositiveOne));
            Assert.True(ComparisonOperatorsHelper<Half, Half>.op_LessThanOrEqual(-MinNormal, PositiveOne));
            Assert.True(ComparisonOperatorsHelper<Half, Half>.op_LessThanOrEqual(-MaxSubnormal, PositiveOne));
            Assert.True(ComparisonOperatorsHelper<Half, Half>.op_LessThanOrEqual(-Half.Epsilon, PositiveOne));
            Assert.True(ComparisonOperatorsHelper<Half, Half>.op_LessThanOrEqual(NegativeZero, PositiveOne));
            Assert.False(ComparisonOperatorsHelper<Half, Half>.op_LessThanOrEqual(Half.NaN, PositiveOne));
            Assert.True(ComparisonOperatorsHelper<Half, Half>.op_LessThanOrEqual(PositiveZero, PositiveOne));
            Assert.True(ComparisonOperatorsHelper<Half, Half>.op_LessThanOrEqual(Half.Epsilon, PositiveOne));
            Assert.True(ComparisonOperatorsHelper<Half, Half>.op_LessThanOrEqual(MaxSubnormal, PositiveOne));
            Assert.True(ComparisonOperatorsHelper<Half, Half>.op_LessThanOrEqual(MinNormal, PositiveOne));
            Assert.True(ComparisonOperatorsHelper<Half, Half>.op_LessThanOrEqual(PositiveOne, PositiveOne));
            Assert.False(ComparisonOperatorsHelper<Half, Half>.op_LessThanOrEqual(Half.MaxValue, PositiveOne));
            Assert.False(ComparisonOperatorsHelper<Half, Half>.op_LessThanOrEqual(Half.PositiveInfinity, PositiveOne));
        }

        [Fact]
        public static void op_GreaterThanTest()
        {
            Assert.False(ComparisonOperatorsHelper<Half, Half>.op_GreaterThan(Half.NegativeInfinity, PositiveOne));
            Assert.False(ComparisonOperatorsHelper<Half, Half>.op_GreaterThan(Half.MinValue, PositiveOne));
            Assert.False(ComparisonOperatorsHelper<Half, Half>.op_GreaterThan(NegativeOne, PositiveOne));
            Assert.False(ComparisonOperatorsHelper<Half, Half>.op_GreaterThan(-MinNormal, PositiveOne));
            Assert.False(ComparisonOperatorsHelper<Half, Half>.op_GreaterThan(-MaxSubnormal, PositiveOne));
            Assert.False(ComparisonOperatorsHelper<Half, Half>.op_GreaterThan(-Half.Epsilon, PositiveOne));
            Assert.False(ComparisonOperatorsHelper<Half, Half>.op_GreaterThan(NegativeZero, PositiveOne));
            Assert.False(ComparisonOperatorsHelper<Half, Half>.op_GreaterThan(Half.NaN, PositiveOne));
            Assert.False(ComparisonOperatorsHelper<Half, Half>.op_GreaterThan(PositiveZero, PositiveOne));
            Assert.False(ComparisonOperatorsHelper<Half, Half>.op_GreaterThan(Half.Epsilon, PositiveOne));
            Assert.False(ComparisonOperatorsHelper<Half, Half>.op_GreaterThan(MaxSubnormal, PositiveOne));
            Assert.False(ComparisonOperatorsHelper<Half, Half>.op_GreaterThan(MinNormal, PositiveOne));
            Assert.False(ComparisonOperatorsHelper<Half, Half>.op_GreaterThan(PositiveOne, PositiveOne));
            Assert.True(ComparisonOperatorsHelper<Half, Half>.op_GreaterThan(Half.MaxValue, PositiveOne));
            Assert.True(ComparisonOperatorsHelper<Half, Half>.op_GreaterThan(Half.PositiveInfinity, PositiveOne));
        }

        [Fact]
        public static void op_GreaterThanOrEqualTest()
        {
            Assert.False(ComparisonOperatorsHelper<Half, Half>.op_GreaterThanOrEqual(Half.NegativeInfinity, PositiveOne));
            Assert.False(ComparisonOperatorsHelper<Half, Half>.op_GreaterThanOrEqual(Half.MinValue, PositiveOne));
            Assert.False(ComparisonOperatorsHelper<Half, Half>.op_GreaterThanOrEqual(NegativeOne, PositiveOne));
            Assert.False(ComparisonOperatorsHelper<Half, Half>.op_GreaterThanOrEqual(-MinNormal, PositiveOne));
            Assert.False(ComparisonOperatorsHelper<Half, Half>.op_GreaterThanOrEqual(-MaxSubnormal, PositiveOne));
            Assert.False(ComparisonOperatorsHelper<Half, Half>.op_GreaterThanOrEqual(-Half.Epsilon, PositiveOne));
            Assert.False(ComparisonOperatorsHelper<Half, Half>.op_GreaterThanOrEqual(NegativeZero, PositiveOne));
            Assert.False(ComparisonOperatorsHelper<Half, Half>.op_GreaterThanOrEqual(Half.NaN, PositiveOne));
            Assert.False(ComparisonOperatorsHelper<Half, Half>.op_GreaterThanOrEqual(PositiveZero, PositiveOne));
            Assert.False(ComparisonOperatorsHelper<Half, Half>.op_GreaterThanOrEqual(Half.Epsilon, PositiveOne));
            Assert.False(ComparisonOperatorsHelper<Half, Half>.op_GreaterThanOrEqual(MaxSubnormal, PositiveOne));
            Assert.False(ComparisonOperatorsHelper<Half, Half>.op_GreaterThanOrEqual(MinNormal, PositiveOne));
            Assert.True(ComparisonOperatorsHelper<Half, Half>.op_GreaterThanOrEqual(PositiveOne, PositiveOne));
            Assert.True(ComparisonOperatorsHelper<Half, Half>.op_GreaterThanOrEqual(Half.MaxValue, PositiveOne));
            Assert.True(ComparisonOperatorsHelper<Half, Half>.op_GreaterThanOrEqual(Half.PositiveInfinity, PositiveOne));
        }

        [Fact]
        public static void op_DecrementTest()
        {
            AssertBitwiseEqual(Half.NegativeInfinity, DecrementOperatorsHelper<Half>.op_Decrement(Half.NegativeInfinity));
            AssertBitwiseEqual(Half.MinValue, DecrementOperatorsHelper<Half>.op_Decrement(Half.MinValue));
            AssertBitwiseEqual(NegativeTwo, DecrementOperatorsHelper<Half>.op_Decrement(NegativeOne));
            AssertBitwiseEqual(NegativeOne, DecrementOperatorsHelper<Half>.op_Decrement(-MinNormal));
            AssertBitwiseEqual(NegativeOne, DecrementOperatorsHelper<Half>.op_Decrement(-MaxSubnormal));
            AssertBitwiseEqual(NegativeOne, DecrementOperatorsHelper<Half>.op_Decrement(-Half.Epsilon));
            AssertBitwiseEqual(NegativeOne, DecrementOperatorsHelper<Half>.op_Decrement(NegativeZero));
            AssertBitwiseEqual(Half.NaN, DecrementOperatorsHelper<Half>.op_Decrement(Half.NaN));
            AssertBitwiseEqual(NegativeOne, DecrementOperatorsHelper<Half>.op_Decrement(PositiveZero));
            AssertBitwiseEqual(NegativeOne, DecrementOperatorsHelper<Half>.op_Decrement(Half.Epsilon));
            AssertBitwiseEqual(NegativeOne, DecrementOperatorsHelper<Half>.op_Decrement(MaxSubnormal));
            AssertBitwiseEqual(NegativeOne, DecrementOperatorsHelper<Half>.op_Decrement(MinNormal));
            AssertBitwiseEqual(PositiveZero, DecrementOperatorsHelper<Half>.op_Decrement(PositiveOne));
            AssertBitwiseEqual(Half.MaxValue, DecrementOperatorsHelper<Half>.op_Decrement(Half.MaxValue));
            AssertBitwiseEqual(Half.PositiveInfinity, DecrementOperatorsHelper<Half>.op_Decrement(Half.PositiveInfinity));
        }

        [Fact]
        public static void op_CheckedDecrementTest()
        {
            AssertBitwiseEqual(Half.NegativeInfinity, DecrementOperatorsHelper<Half>.op_CheckedDecrement(Half.NegativeInfinity));
            AssertBitwiseEqual(Half.MinValue, DecrementOperatorsHelper<Half>.op_CheckedDecrement(Half.MinValue));
            AssertBitwiseEqual(NegativeTwo, DecrementOperatorsHelper<Half>.op_CheckedDecrement(NegativeOne));
            AssertBitwiseEqual(NegativeOne, DecrementOperatorsHelper<Half>.op_CheckedDecrement(-MinNormal));
            AssertBitwiseEqual(NegativeOne, DecrementOperatorsHelper<Half>.op_CheckedDecrement(-MaxSubnormal));
            AssertBitwiseEqual(NegativeOne, DecrementOperatorsHelper<Half>.op_CheckedDecrement(-Half.Epsilon));
            AssertBitwiseEqual(NegativeOne, DecrementOperatorsHelper<Half>.op_CheckedDecrement(NegativeZero));
            AssertBitwiseEqual(Half.NaN, DecrementOperatorsHelper<Half>.op_CheckedDecrement(Half.NaN));
            AssertBitwiseEqual(NegativeOne, DecrementOperatorsHelper<Half>.op_CheckedDecrement(PositiveZero));
            AssertBitwiseEqual(NegativeOne, DecrementOperatorsHelper<Half>.op_CheckedDecrement(Half.Epsilon));
            AssertBitwiseEqual(NegativeOne, DecrementOperatorsHelper<Half>.op_CheckedDecrement(MaxSubnormal));
            AssertBitwiseEqual(NegativeOne, DecrementOperatorsHelper<Half>.op_CheckedDecrement(MinNormal));
            AssertBitwiseEqual(PositiveZero, DecrementOperatorsHelper<Half>.op_CheckedDecrement(PositiveOne));
            AssertBitwiseEqual(Half.MaxValue, DecrementOperatorsHelper<Half>.op_CheckedDecrement(Half.MaxValue));
            AssertBitwiseEqual(Half.PositiveInfinity, DecrementOperatorsHelper<Half>.op_CheckedDecrement(Half.PositiveInfinity));
        }

        [Fact]
        public static void op_DivisionTest()
        {
            AssertBitwiseEqual(Half.NegativeInfinity, DivisionOperatorsHelper<Half, Half, Half>.op_Division(Half.NegativeInfinity, PositiveTwo));
            AssertBitwiseEqual((Half)(-32750.0f), DivisionOperatorsHelper<Half, Half, Half>.op_Division(Half.MinValue, PositiveTwo));
            AssertBitwiseEqual((Half)(-0.5f), DivisionOperatorsHelper<Half, Half, Half>.op_Division(NegativeOne, PositiveTwo));
            AssertBitwiseEqual((Half)(-3.05E-05f), DivisionOperatorsHelper<Half, Half, Half>.op_Division(-MinNormal, PositiveTwo));
            AssertBitwiseEqual((Half)(-3.05E-05f), DivisionOperatorsHelper<Half, Half, Half>.op_Division(-MaxSubnormal, PositiveTwo));
            AssertBitwiseEqual(NegativeZero, DivisionOperatorsHelper<Half, Half, Half>.op_Division(-Half.Epsilon, PositiveTwo));
            AssertBitwiseEqual(NegativeZero, DivisionOperatorsHelper<Half, Half, Half>.op_Division(NegativeZero, PositiveTwo));
            AssertBitwiseEqual(Half.NaN, DivisionOperatorsHelper<Half, Half, Half>.op_Division(Half.NaN, PositiveTwo));
            AssertBitwiseEqual(PositiveZero, DivisionOperatorsHelper<Half, Half, Half>.op_Division(PositiveZero, PositiveTwo));
            AssertBitwiseEqual(PositiveZero, DivisionOperatorsHelper<Half, Half, Half>.op_Division(Half.Epsilon, PositiveTwo));
            AssertBitwiseEqual((Half)3.05E-05f, DivisionOperatorsHelper<Half, Half, Half>.op_Division(MaxSubnormal, PositiveTwo));
            AssertBitwiseEqual((Half)3.05E-05f, DivisionOperatorsHelper<Half, Half, Half>.op_Division(MinNormal, PositiveTwo));
            AssertBitwiseEqual((Half)0.5f, DivisionOperatorsHelper<Half, Half, Half>.op_Division(PositiveOne, PositiveTwo));
            AssertBitwiseEqual((Half)32750.0f, DivisionOperatorsHelper<Half, Half, Half>.op_Division(Half.MaxValue, PositiveTwo));
            AssertBitwiseEqual(Half.PositiveInfinity, DivisionOperatorsHelper<Half, Half, Half>.op_Division(Half.PositiveInfinity, PositiveTwo));
        }

        [Fact]
        public static void op_CheckedDivisionTest()
        {
            AssertBitwiseEqual(Half.NegativeInfinity, DivisionOperatorsHelper<Half, Half, Half>.op_CheckedDivision(Half.NegativeInfinity, PositiveTwo));
            AssertBitwiseEqual((Half)(-32750.0f), DivisionOperatorsHelper<Half, Half, Half>.op_CheckedDivision(Half.MinValue, PositiveTwo));
            AssertBitwiseEqual((Half)(-0.5f), DivisionOperatorsHelper<Half, Half, Half>.op_CheckedDivision(NegativeOne, PositiveTwo));
            AssertBitwiseEqual((Half)(-3.05E-05f), DivisionOperatorsHelper<Half, Half, Half>.op_CheckedDivision(-MinNormal, PositiveTwo));
            AssertBitwiseEqual((Half)(-3.05E-05f), DivisionOperatorsHelper<Half, Half, Half>.op_CheckedDivision(-MaxSubnormal, PositiveTwo));
            AssertBitwiseEqual(NegativeZero, DivisionOperatorsHelper<Half, Half, Half>.op_CheckedDivision(-Half.Epsilon, PositiveTwo));
            AssertBitwiseEqual(NegativeZero, DivisionOperatorsHelper<Half, Half, Half>.op_CheckedDivision(NegativeZero, PositiveTwo));
            AssertBitwiseEqual(Half.NaN, DivisionOperatorsHelper<Half, Half, Half>.op_CheckedDivision(Half.NaN, PositiveTwo));
            AssertBitwiseEqual(PositiveZero, DivisionOperatorsHelper<Half, Half, Half>.op_CheckedDivision(PositiveZero, PositiveTwo));
            AssertBitwiseEqual(PositiveZero, DivisionOperatorsHelper<Half, Half, Half>.op_CheckedDivision(Half.Epsilon, PositiveTwo));
            AssertBitwiseEqual((Half)3.05E-05f, DivisionOperatorsHelper<Half, Half, Half>.op_CheckedDivision(MaxSubnormal, PositiveTwo));
            AssertBitwiseEqual((Half)3.05E-05f, DivisionOperatorsHelper<Half, Half, Half>.op_CheckedDivision(MinNormal, PositiveTwo));
            AssertBitwiseEqual((Half)0.5f, DivisionOperatorsHelper<Half, Half, Half>.op_CheckedDivision(PositiveOne, PositiveTwo));
            AssertBitwiseEqual((Half)32750.0f, DivisionOperatorsHelper<Half, Half, Half>.op_CheckedDivision(Half.MaxValue, PositiveTwo));
            AssertBitwiseEqual(Half.PositiveInfinity, DivisionOperatorsHelper<Half, Half, Half>.op_CheckedDivision(Half.PositiveInfinity, PositiveTwo));
        }

        [Fact]
        public static void op_EqualityTest()
        {
            Assert.False(EqualityOperatorsHelper<Half, Half>.op_Equality(Half.NegativeInfinity, PositiveOne));
            Assert.False(EqualityOperatorsHelper<Half, Half>.op_Equality(Half.MinValue, PositiveOne));
            Assert.False(EqualityOperatorsHelper<Half, Half>.op_Equality(NegativeOne, PositiveOne));
            Assert.False(EqualityOperatorsHelper<Half, Half>.op_Equality(-MinNormal, PositiveOne));
            Assert.False(EqualityOperatorsHelper<Half, Half>.op_Equality(-MaxSubnormal, PositiveOne));
            Assert.False(EqualityOperatorsHelper<Half, Half>.op_Equality(-Half.Epsilon, PositiveOne));
            Assert.False(EqualityOperatorsHelper<Half, Half>.op_Equality(NegativeZero, PositiveOne));
            Assert.False(EqualityOperatorsHelper<Half, Half>.op_Equality(Half.NaN, PositiveOne));
            Assert.False(EqualityOperatorsHelper<Half, Half>.op_Equality(PositiveZero, PositiveOne));
            Assert.False(EqualityOperatorsHelper<Half, Half>.op_Equality(Half.Epsilon, PositiveOne));
            Assert.False(EqualityOperatorsHelper<Half, Half>.op_Equality(MaxSubnormal, PositiveOne));
            Assert.False(EqualityOperatorsHelper<Half, Half>.op_Equality(MinNormal, PositiveOne));
            Assert.True(EqualityOperatorsHelper<Half, Half>.op_Equality(PositiveOne, PositiveOne));
            Assert.False(EqualityOperatorsHelper<Half, Half>.op_Equality(Half.MaxValue, PositiveOne));
            Assert.False(EqualityOperatorsHelper<Half, Half>.op_Equality(Half.PositiveInfinity, PositiveOne));
        }

        [Fact]
        public static void op_InequalityTest()
        {
            Assert.True(EqualityOperatorsHelper<Half, Half>.op_Inequality(Half.NegativeInfinity, PositiveOne));
            Assert.True(EqualityOperatorsHelper<Half, Half>.op_Inequality(Half.MinValue, PositiveOne));
            Assert.True(EqualityOperatorsHelper<Half, Half>.op_Inequality(NegativeOne, PositiveOne));
            Assert.True(EqualityOperatorsHelper<Half, Half>.op_Inequality(-MinNormal, PositiveOne));
            Assert.True(EqualityOperatorsHelper<Half, Half>.op_Inequality(-MaxSubnormal, PositiveOne));
            Assert.True(EqualityOperatorsHelper<Half, Half>.op_Inequality(-Half.Epsilon, PositiveOne));
            Assert.True(EqualityOperatorsHelper<Half, Half>.op_Inequality(NegativeZero, PositiveOne));
            Assert.True(EqualityOperatorsHelper<Half, Half>.op_Inequality(Half.NaN, PositiveOne));
            Assert.True(EqualityOperatorsHelper<Half, Half>.op_Inequality(PositiveZero, PositiveOne));
            Assert.True(EqualityOperatorsHelper<Half, Half>.op_Inequality(Half.Epsilon, PositiveOne));
            Assert.True(EqualityOperatorsHelper<Half, Half>.op_Inequality(MaxSubnormal, PositiveOne));
            Assert.True(EqualityOperatorsHelper<Half, Half>.op_Inequality(MinNormal, PositiveOne));
            Assert.False(EqualityOperatorsHelper<Half, Half>.op_Inequality(PositiveOne, PositiveOne));
            Assert.True(EqualityOperatorsHelper<Half, Half>.op_Inequality(Half.MaxValue, PositiveOne));
            Assert.True(EqualityOperatorsHelper<Half, Half>.op_Inequality(Half.PositiveInfinity, PositiveOne));
        }

        [Fact]
        public static void op_IncrementTest()
        {
            AssertBitwiseEqual(Half.NegativeInfinity, IncrementOperatorsHelper<Half>.op_Increment(Half.NegativeInfinity));
            AssertBitwiseEqual(Half.MinValue, IncrementOperatorsHelper<Half>.op_Increment(Half.MinValue));
            AssertBitwiseEqual(PositiveZero, IncrementOperatorsHelper<Half>.op_Increment(NegativeOne));
            AssertBitwiseEqual(PositiveOne, IncrementOperatorsHelper<Half>.op_Increment(-MinNormal));
            AssertBitwiseEqual(PositiveOne, IncrementOperatorsHelper<Half>.op_Increment(-MaxSubnormal));
            AssertBitwiseEqual(PositiveOne, IncrementOperatorsHelper<Half>.op_Increment(-Half.Epsilon));
            AssertBitwiseEqual(PositiveOne, IncrementOperatorsHelper<Half>.op_Increment(NegativeZero));
            AssertBitwiseEqual(Half.NaN, IncrementOperatorsHelper<Half>.op_Increment(Half.NaN));
            AssertBitwiseEqual(PositiveOne, IncrementOperatorsHelper<Half>.op_Increment(PositiveZero));
            AssertBitwiseEqual(PositiveOne, IncrementOperatorsHelper<Half>.op_Increment(Half.Epsilon));
            AssertBitwiseEqual(PositiveOne, IncrementOperatorsHelper<Half>.op_Increment(MaxSubnormal));
            AssertBitwiseEqual(PositiveOne, IncrementOperatorsHelper<Half>.op_Increment(MinNormal));
            AssertBitwiseEqual(PositiveTwo, IncrementOperatorsHelper<Half>.op_Increment(PositiveOne));
            AssertBitwiseEqual(Half.MaxValue, IncrementOperatorsHelper<Half>.op_Increment(Half.MaxValue));
            AssertBitwiseEqual(Half.PositiveInfinity, IncrementOperatorsHelper<Half>.op_Increment(Half.PositiveInfinity));
        }

        [Fact]
        public static void op_CheckedIncrementTest()
        {
            AssertBitwiseEqual(Half.NegativeInfinity, IncrementOperatorsHelper<Half>.op_CheckedIncrement(Half.NegativeInfinity));
            AssertBitwiseEqual(Half.MinValue, IncrementOperatorsHelper<Half>.op_CheckedIncrement(Half.MinValue));
            AssertBitwiseEqual(PositiveZero, IncrementOperatorsHelper<Half>.op_CheckedIncrement(NegativeOne));
            AssertBitwiseEqual(PositiveOne, IncrementOperatorsHelper<Half>.op_CheckedIncrement(-MinNormal));
            AssertBitwiseEqual(PositiveOne, IncrementOperatorsHelper<Half>.op_CheckedIncrement(-MaxSubnormal));
            AssertBitwiseEqual(PositiveOne, IncrementOperatorsHelper<Half>.op_CheckedIncrement(-Half.Epsilon));
            AssertBitwiseEqual(PositiveOne, IncrementOperatorsHelper<Half>.op_CheckedIncrement(NegativeZero));
            AssertBitwiseEqual(Half.NaN, IncrementOperatorsHelper<Half>.op_CheckedIncrement(Half.NaN));
            AssertBitwiseEqual(PositiveOne, IncrementOperatorsHelper<Half>.op_CheckedIncrement(PositiveZero));
            AssertBitwiseEqual(PositiveOne, IncrementOperatorsHelper<Half>.op_CheckedIncrement(Half.Epsilon));
            AssertBitwiseEqual(PositiveOne, IncrementOperatorsHelper<Half>.op_CheckedIncrement(MaxSubnormal));
            AssertBitwiseEqual(PositiveOne, IncrementOperatorsHelper<Half>.op_CheckedIncrement(MinNormal));
            AssertBitwiseEqual(PositiveTwo, IncrementOperatorsHelper<Half>.op_CheckedIncrement(PositiveOne));
            AssertBitwiseEqual(Half.MaxValue, IncrementOperatorsHelper<Half>.op_CheckedIncrement(Half.MaxValue));
            AssertBitwiseEqual(Half.PositiveInfinity, IncrementOperatorsHelper<Half>.op_CheckedIncrement(Half.PositiveInfinity));
        }

        [Fact]
        public static void op_ModulusTest()
        {
            AssertBitwiseEqual(Half.NaN, ModulusOperatorsHelper<Half, Half, Half>.op_Modulus(Half.NegativeInfinity, PositiveTwo));

            // https://github.com/dotnet/runtime/issues/67993
            // AssertBitwiseEqual(NegativeZero, ModulusOperatorsHelper<Half, Half, Half>.op_Modulus(Half.MinValue, PositiveTwo));

            AssertBitwiseEqual(NegativeOne, ModulusOperatorsHelper<Half, Half, Half>.op_Modulus(NegativeOne, PositiveTwo));
            AssertBitwiseEqual(-MinNormal, ModulusOperatorsHelper<Half, Half, Half>.op_Modulus(-MinNormal, PositiveTwo));
            AssertBitwiseEqual(-MaxSubnormal, ModulusOperatorsHelper<Half, Half, Half>.op_Modulus(-MaxSubnormal, PositiveTwo));
            AssertBitwiseEqual(-Half.Epsilon, ModulusOperatorsHelper<Half, Half, Half>.op_Modulus(-Half.Epsilon, PositiveTwo)); ;

            // https://github.com/dotnet/runtime/issues/67993
            // AssertBitwiseEqual(NegativeZero, ModulusOperatorsHelper<Half, Half, Half>.op_Modulus(NegativeZero, PositiveTwo));

            AssertBitwiseEqual(Half.NaN, ModulusOperatorsHelper<Half, Half, Half>.op_Modulus(Half.NaN, PositiveTwo));
            AssertBitwiseEqual(PositiveZero, ModulusOperatorsHelper<Half, Half, Half>.op_Modulus(PositiveZero, PositiveTwo));
            AssertBitwiseEqual(Half.Epsilon, ModulusOperatorsHelper<Half, Half, Half>.op_Modulus(Half.Epsilon, PositiveTwo));
            AssertBitwiseEqual(MaxSubnormal, ModulusOperatorsHelper<Half, Half, Half>.op_Modulus(MaxSubnormal, PositiveTwo));
            AssertBitwiseEqual(MinNormal, ModulusOperatorsHelper<Half, Half, Half>.op_Modulus(MinNormal, PositiveTwo));
            AssertBitwiseEqual(PositiveOne, ModulusOperatorsHelper<Half, Half, Half>.op_Modulus(PositiveOne, PositiveTwo));
            AssertBitwiseEqual(PositiveZero, ModulusOperatorsHelper<Half, Half, Half>.op_Modulus(Half.MaxValue, PositiveTwo));
            AssertBitwiseEqual(Half.NaN, ModulusOperatorsHelper<Half, Half, Half>.op_Modulus(Half.PositiveInfinity, PositiveTwo));
        }

        [Fact]
        public static void op_MultiplyTest()
        {
            AssertBitwiseEqual(Half.NegativeInfinity, MultiplyOperatorsHelper<Half, Half, Half>.op_Multiply(Half.NegativeInfinity, PositiveTwo));
            AssertBitwiseEqual(Half.NegativeInfinity, MultiplyOperatorsHelper<Half, Half, Half>.op_Multiply(Half.MinValue, PositiveTwo));
            AssertBitwiseEqual(NegativeTwo, MultiplyOperatorsHelper<Half, Half, Half>.op_Multiply(NegativeOne, PositiveTwo));
            AssertBitwiseEqual((Half)(-0.0001221f), MultiplyOperatorsHelper<Half, Half, Half>.op_Multiply(-MinNormal, PositiveTwo));
            AssertBitwiseEqual((Half)(-0.00012195f), MultiplyOperatorsHelper<Half, Half, Half>.op_Multiply(-MaxSubnormal, PositiveTwo));
            AssertBitwiseEqual((Half)(-1E-07f), MultiplyOperatorsHelper<Half, Half, Half>.op_Multiply(-Half.Epsilon, PositiveTwo));
            AssertBitwiseEqual(NegativeZero, MultiplyOperatorsHelper<Half, Half, Half>.op_Multiply(NegativeZero, PositiveTwo));
            AssertBitwiseEqual(Half.NaN, MultiplyOperatorsHelper<Half, Half, Half>.op_Multiply(Half.NaN, PositiveTwo));
            AssertBitwiseEqual(PositiveZero, MultiplyOperatorsHelper<Half, Half, Half>.op_Multiply(PositiveZero, PositiveTwo));
            AssertBitwiseEqual((Half)1E-07f, MultiplyOperatorsHelper<Half, Half, Half>.op_Multiply(Half.Epsilon, PositiveTwo));
            AssertBitwiseEqual((Half)0.00012195f, MultiplyOperatorsHelper<Half, Half, Half>.op_Multiply(MaxSubnormal, PositiveTwo));
            AssertBitwiseEqual((Half)0.0001221f, MultiplyOperatorsHelper<Half, Half, Half>.op_Multiply(MinNormal, PositiveTwo));
            AssertBitwiseEqual(PositiveTwo, MultiplyOperatorsHelper<Half, Half, Half>.op_Multiply(PositiveOne, PositiveTwo));
            AssertBitwiseEqual(Half.PositiveInfinity, MultiplyOperatorsHelper<Half, Half, Half>.op_Multiply(Half.MaxValue, PositiveTwo));
            AssertBitwiseEqual(Half.PositiveInfinity, MultiplyOperatorsHelper<Half, Half, Half>.op_Multiply(Half.PositiveInfinity, PositiveTwo));
        }

        [Fact]
        public static void op_CheckedMultiplyTest()
        {
            AssertBitwiseEqual(Half.NegativeInfinity, MultiplyOperatorsHelper<Half, Half, Half>.op_CheckedMultiply(Half.NegativeInfinity, PositiveTwo));
            AssertBitwiseEqual(Half.NegativeInfinity, MultiplyOperatorsHelper<Half, Half, Half>.op_CheckedMultiply(Half.MinValue, PositiveTwo));
            AssertBitwiseEqual(NegativeTwo, MultiplyOperatorsHelper<Half, Half, Half>.op_CheckedMultiply(NegativeOne, PositiveTwo));
            AssertBitwiseEqual((Half)(-0.0001221f), MultiplyOperatorsHelper<Half, Half, Half>.op_CheckedMultiply(-MinNormal, PositiveTwo));
            AssertBitwiseEqual((Half)(-0.00012195f), MultiplyOperatorsHelper<Half, Half, Half>.op_CheckedMultiply(-MaxSubnormal, PositiveTwo));
            AssertBitwiseEqual((Half)(-1E-07f), MultiplyOperatorsHelper<Half, Half, Half>.op_CheckedMultiply(-Half.Epsilon, PositiveTwo));
            AssertBitwiseEqual(NegativeZero, MultiplyOperatorsHelper<Half, Half, Half>.op_CheckedMultiply(NegativeZero, PositiveTwo));
            AssertBitwiseEqual(Half.NaN, MultiplyOperatorsHelper<Half, Half, Half>.op_CheckedMultiply(Half.NaN, PositiveTwo));
            AssertBitwiseEqual(PositiveZero, MultiplyOperatorsHelper<Half, Half, Half>.op_CheckedMultiply(PositiveZero, PositiveTwo));
            AssertBitwiseEqual((Half)1E-07f, MultiplyOperatorsHelper<Half, Half, Half>.op_CheckedMultiply(Half.Epsilon, PositiveTwo));
            AssertBitwiseEqual((Half)0.00012195f, MultiplyOperatorsHelper<Half, Half, Half>.op_CheckedMultiply(MaxSubnormal, PositiveTwo));
            AssertBitwiseEqual((Half)0.0001221f, MultiplyOperatorsHelper<Half, Half, Half>.op_CheckedMultiply(MinNormal, PositiveTwo));
            AssertBitwiseEqual(PositiveTwo, MultiplyOperatorsHelper<Half, Half, Half>.op_CheckedMultiply(PositiveOne, PositiveTwo));
            AssertBitwiseEqual(Half.PositiveInfinity, MultiplyOperatorsHelper<Half, Half, Half>.op_CheckedMultiply(Half.MaxValue, PositiveTwo));
            AssertBitwiseEqual(Half.PositiveInfinity, MultiplyOperatorsHelper<Half, Half, Half>.op_CheckedMultiply(Half.PositiveInfinity, PositiveTwo));
        }

        [Fact]
        public static void AbsTest()
        {
            AssertBitwiseEqual(Half.PositiveInfinity, NumberHelper<Half>.Abs(Half.NegativeInfinity));
            AssertBitwiseEqual(Half.MaxValue, NumberHelper<Half>.Abs(Half.MinValue));
            AssertBitwiseEqual(PositiveOne, NumberHelper<Half>.Abs(NegativeOne));
            AssertBitwiseEqual(MinNormal, NumberHelper<Half>.Abs(-MinNormal));
            AssertBitwiseEqual(MaxSubnormal, NumberHelper<Half>.Abs(-MaxSubnormal));
            AssertBitwiseEqual(Half.Epsilon, NumberHelper<Half>.Abs(-Half.Epsilon));
            AssertBitwiseEqual(PositiveZero, NumberHelper<Half>.Abs(NegativeZero));
            AssertBitwiseEqual(Half.NaN, NumberHelper<Half>.Abs(Half.NaN));
            AssertBitwiseEqual(PositiveZero, NumberHelper<Half>.Abs(PositiveZero));
            AssertBitwiseEqual(Half.Epsilon, NumberHelper<Half>.Abs(Half.Epsilon));
            AssertBitwiseEqual(MaxSubnormal, NumberHelper<Half>.Abs(MaxSubnormal));
            AssertBitwiseEqual(MinNormal, NumberHelper<Half>.Abs(MinNormal));
            AssertBitwiseEqual(PositiveOne, NumberHelper<Half>.Abs(PositiveOne));
            AssertBitwiseEqual(Half.MaxValue, NumberHelper<Half>.Abs(Half.MaxValue));
            AssertBitwiseEqual(Half.PositiveInfinity, NumberHelper<Half>.Abs(Half.PositiveInfinity));
        }

        [Fact]
        public static void ClampTest()
        {
            AssertBitwiseEqual(PositiveOne, NumberHelper<Half>.Clamp(Half.NegativeInfinity, PositiveOne, (Half)63.0f));
            AssertBitwiseEqual(PositiveOne, NumberHelper<Half>.Clamp(Half.MinValue, PositiveOne, (Half)63.0f));
            AssertBitwiseEqual(PositiveOne, NumberHelper<Half>.Clamp(NegativeOne, PositiveOne, (Half)63.0f));
            AssertBitwiseEqual(PositiveOne, NumberHelper<Half>.Clamp(-MinNormal, PositiveOne, (Half)63.0f));
            AssertBitwiseEqual(PositiveOne, NumberHelper<Half>.Clamp(-MaxSubnormal, PositiveOne, (Half)63.0f));
            AssertBitwiseEqual(PositiveOne, NumberHelper<Half>.Clamp(-Half.Epsilon, PositiveOne, (Half)63.0f));
            AssertBitwiseEqual(PositiveOne, NumberHelper<Half>.Clamp(NegativeZero, PositiveOne, (Half)63.0f));
            AssertBitwiseEqual(Half.NaN, NumberHelper<Half>.Clamp(Half.NaN, PositiveOne, (Half)63.0f));
            AssertBitwiseEqual(PositiveOne, NumberHelper<Half>.Clamp(PositiveZero, PositiveOne, (Half)63.0f));
            AssertBitwiseEqual(PositiveOne, NumberHelper<Half>.Clamp(Half.Epsilon, PositiveOne, (Half)63.0f));
            AssertBitwiseEqual(PositiveOne, NumberHelper<Half>.Clamp(MaxSubnormal, PositiveOne, (Half)63.0f));
            AssertBitwiseEqual(PositiveOne, NumberHelper<Half>.Clamp(MinNormal, PositiveOne, (Half)63.0f));
            AssertBitwiseEqual(PositiveOne, NumberHelper<Half>.Clamp(PositiveOne, PositiveOne, (Half)63.0f));
            AssertBitwiseEqual((Half)63.0f, NumberHelper<Half>.Clamp(Half.MaxValue, PositiveOne, (Half)63.0f));
            AssertBitwiseEqual((Half)63.0f, NumberHelper<Half>.Clamp(Half.PositiveInfinity, PositiveOne, (Half)63.0f));
        }

        [Fact]
        public static void CreateCheckedFromByteTest()
        {
            AssertBitwiseEqual(PositiveZero, NumberHelper<Half>.CreateChecked<byte>(0x00));
            AssertBitwiseEqual(PositiveOne, NumberHelper<Half>.CreateChecked<byte>(0x01));
            AssertBitwiseEqual((Half)127.0f, NumberHelper<Half>.CreateChecked<byte>(0x7F));
            AssertBitwiseEqual((Half)128.0f, NumberHelper<Half>.CreateChecked<byte>(0x80));
            AssertBitwiseEqual((Half)255.0f, NumberHelper<Half>.CreateChecked<byte>(0xFF));
        }

        [Fact]
        public static void CreateCheckedFromCharTest()
        {
            AssertBitwiseEqual(PositiveZero, NumberHelper<Half>.CreateChecked<char>((char)0x0000));
            AssertBitwiseEqual(PositiveOne, NumberHelper<Half>.CreateChecked<char>((char)0x0001));
            AssertBitwiseEqual((Half)32767.0f, NumberHelper<Half>.CreateChecked<char>((char)0x7FFF));
            AssertBitwiseEqual((Half)32768.0f, NumberHelper<Half>.CreateChecked<char>((char)0x8000));
            AssertBitwiseEqual((Half)65535.0f, NumberHelper<Half>.CreateChecked<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateCheckedFromInt16Test()
        {
            AssertBitwiseEqual(PositiveZero, NumberHelper<Half>.CreateChecked<short>(0x0000));
            AssertBitwiseEqual(PositiveOne, NumberHelper<Half>.CreateChecked<short>(0x0001));
            AssertBitwiseEqual((Half)32767.0f, NumberHelper<Half>.CreateChecked<short>(0x7FFF));
            AssertBitwiseEqual((Half)(-32768.0f), NumberHelper<Half>.CreateChecked<short>(unchecked((short)0x8000)));
            AssertBitwiseEqual(NegativeOne, NumberHelper<Half>.CreateChecked<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromInt32Test()
        {
            AssertBitwiseEqual(PositiveZero, NumberHelper<Half>.CreateChecked<int>(0x00000000));
            AssertBitwiseEqual(PositiveOne, NumberHelper<Half>.CreateChecked<int>(0x00000001));
            AssertBitwiseEqual((Half)2147483647.0f, NumberHelper<Half>.CreateChecked<int>(0x7FFFFFFF));
            AssertBitwiseEqual((Half)(-2147483648.0f), NumberHelper<Half>.CreateChecked<int>(unchecked((int)0x80000000)));
            AssertBitwiseEqual(NegativeOne, NumberHelper<Half>.CreateChecked<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromInt64Test()
        {
            AssertBitwiseEqual(PositiveZero, NumberHelper<Half>.CreateChecked<long>(0x0000000000000000));
            AssertBitwiseEqual(PositiveOne, NumberHelper<Half>.CreateChecked<long>(0x0000000000000001));
            AssertBitwiseEqual((Half)9223372036854775807.0f, NumberHelper<Half>.CreateChecked<long>(0x7FFFFFFFFFFFFFFF));
            AssertBitwiseEqual((Half)(-9223372036854775808.0f), NumberHelper<Half>.CreateChecked<long>(unchecked(unchecked((long)0x8000000000000000))));
            AssertBitwiseEqual(NegativeOne, NumberHelper<Half>.CreateChecked<long>(unchecked(unchecked((long)0xFFFFFFFFFFFFFFFF))));
        }

        [Fact]
        public static void CreateCheckedFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual(PositiveZero, NumberHelper<Half>.CreateChecked<nint>(unchecked((nint)0x0000000000000000)));
                AssertBitwiseEqual(PositiveOne, NumberHelper<Half>.CreateChecked<nint>(unchecked((nint)0x0000000000000001)));
                AssertBitwiseEqual((Half)9223372036854775807.0f, NumberHelper<Half>.CreateChecked<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                AssertBitwiseEqual((Half)(-9223372036854775808.0f), NumberHelper<Half>.CreateChecked<nint>(unchecked((nint)0x8000000000000000)));
                AssertBitwiseEqual(NegativeOne, NumberHelper<Half>.CreateChecked<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                AssertBitwiseEqual(PositiveZero, NumberHelper<Half>.CreateChecked<nint>((nint)0x00000000));
                AssertBitwiseEqual(PositiveOne, NumberHelper<Half>.CreateChecked<nint>((nint)0x00000001));
                AssertBitwiseEqual((Half)2147483647.0f, NumberHelper<Half>.CreateChecked<nint>((nint)0x7FFFFFFF));
                AssertBitwiseEqual((Half)(-2147483648.0f), NumberHelper<Half>.CreateChecked<nint>(unchecked((nint)0x80000000)));
                AssertBitwiseEqual(NegativeOne, NumberHelper<Half>.CreateChecked<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateCheckedFromSByteTest()
        {
            AssertBitwiseEqual(PositiveZero, NumberHelper<Half>.CreateChecked<sbyte>(0x00));
            AssertBitwiseEqual(PositiveOne, NumberHelper<Half>.CreateChecked<sbyte>(0x01));
            AssertBitwiseEqual((Half)127.0f, NumberHelper<Half>.CreateChecked<sbyte>(0x7F));
            AssertBitwiseEqual((Half)(-128.0f), NumberHelper<Half>.CreateChecked<sbyte>(unchecked((sbyte)0x80)));
            AssertBitwiseEqual(NegativeOne, NumberHelper<Half>.CreateChecked<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateCheckedFromUInt16Test()
        {
            AssertBitwiseEqual(PositiveZero, NumberHelper<Half>.CreateChecked<ushort>(0x0000));
            AssertBitwiseEqual(PositiveOne, NumberHelper<Half>.CreateChecked<ushort>(0x0001));
            AssertBitwiseEqual((Half)32767.0f, NumberHelper<Half>.CreateChecked<ushort>(0x7FFF));
            AssertBitwiseEqual((Half)32768.0f, NumberHelper<Half>.CreateChecked<ushort>(0x8000));
            AssertBitwiseEqual((Half)65535.0f, NumberHelper<Half>.CreateChecked<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUInt32Test()
        {
            AssertBitwiseEqual(PositiveZero, NumberHelper<Half>.CreateChecked<uint>(0x00000000));
            AssertBitwiseEqual(PositiveOne, NumberHelper<Half>.CreateChecked<uint>(0x00000001));
            AssertBitwiseEqual((Half)2147483647.0f, NumberHelper<Half>.CreateChecked<uint>(0x7FFFFFFF));
            AssertBitwiseEqual((Half)2147483648.0f, NumberHelper<Half>.CreateChecked<uint>(0x80000000));
            AssertBitwiseEqual((Half)4294967295.0f, NumberHelper<Half>.CreateChecked<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUInt64Test()
        {
            AssertBitwiseEqual(PositiveZero, NumberHelper<Half>.CreateChecked<ulong>(0x0000000000000000));
            AssertBitwiseEqual(PositiveOne, NumberHelper<Half>.CreateChecked<ulong>(0x0000000000000001));
            AssertBitwiseEqual((Half)9223372036854775807.0f, NumberHelper<Half>.CreateChecked<ulong>(0x7FFFFFFFFFFFFFFF));
            AssertBitwiseEqual((Half)9223372036854775808.0f, NumberHelper<Half>.CreateChecked<ulong>(0x8000000000000000));
            AssertBitwiseEqual((Half)18446744073709551615.0f, NumberHelper<Half>.CreateChecked<ulong>(0xFFFFFFFFFFFFFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual(PositiveZero, NumberHelper<Half>.CreateChecked<nuint>(unchecked((nuint)0x0000000000000000)));
                AssertBitwiseEqual(PositiveOne, NumberHelper<Half>.CreateChecked<nuint>(unchecked((nuint)0x0000000000000001)));
                AssertBitwiseEqual((Half)9223372036854775807.0f, NumberHelper<Half>.CreateChecked<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));

                // https://github.com/dotnet/roslyn/issues/60714
                // AssertBitwiseEqual((Half)9223372036854775808.0f, NumberHelper<Half>.CreateChecked<nuint>(unchecked((nuint)0x8000000000000000)));
                // AssertBitwiseEqual((Half)18446744073709551615.0f,NumberHelper<Half>.CreateChecked<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                AssertBitwiseEqual(PositiveZero, NumberHelper<Half>.CreateChecked<nuint>((nuint)0x00000000));
                AssertBitwiseEqual(PositiveOne, NumberHelper<Half>.CreateChecked<nuint>((nuint)0x00000001));
                AssertBitwiseEqual((Half)2147483647.0f, NumberHelper<Half>.CreateChecked<nuint>((nuint)0x7FFFFFFF));

                // https://github.com/dotnet/roslyn/issues/60714
                // AssertBitwiseEqual((Half)2147483648.0f, NumberHelper<Half>.CreateChecked<nuint>((nuint)0x80000000));
                // AssertBitwiseEqual((Half)4294967295.0f, NumberHelper<Half>.CreateChecked<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateSaturatingFromByteTest()
        {
            AssertBitwiseEqual(PositiveZero, NumberHelper<Half>.CreateSaturating<byte>(0x00));
            AssertBitwiseEqual(PositiveOne, NumberHelper<Half>.CreateSaturating<byte>(0x01));
            AssertBitwiseEqual((Half)127.0f, NumberHelper<Half>.CreateSaturating<byte>(0x7F));
            AssertBitwiseEqual((Half)128.0f, NumberHelper<Half>.CreateSaturating<byte>(0x80));
            AssertBitwiseEqual((Half)255.0f, NumberHelper<Half>.CreateSaturating<byte>(0xFF));
        }

        [Fact]
        public static void CreateSaturatingFromCharTest()
        {
            AssertBitwiseEqual(PositiveZero, NumberHelper<Half>.CreateSaturating<char>((char)0x0000));
            AssertBitwiseEqual(PositiveOne, NumberHelper<Half>.CreateSaturating<char>((char)0x0001));
            AssertBitwiseEqual((Half)32767.0f, NumberHelper<Half>.CreateSaturating<char>((char)0x7FFF));
            AssertBitwiseEqual((Half)32768.0f, NumberHelper<Half>.CreateSaturating<char>((char)0x8000));
            AssertBitwiseEqual((Half)65535.0f, NumberHelper<Half>.CreateSaturating<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromInt16Test()
        {
            AssertBitwiseEqual(PositiveZero, NumberHelper<Half>.CreateSaturating<short>(0x0000));
            AssertBitwiseEqual(PositiveOne, NumberHelper<Half>.CreateSaturating<short>(0x0001));
            AssertBitwiseEqual((Half)32767.0f, NumberHelper<Half>.CreateSaturating<short>(0x7FFF));
            AssertBitwiseEqual((Half)(-32768.0f), NumberHelper<Half>.CreateSaturating<short>(unchecked((short)0x8000)));
            AssertBitwiseEqual(NegativeOne, NumberHelper<Half>.CreateSaturating<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromInt32Test()
        {
            AssertBitwiseEqual(PositiveZero, NumberHelper<Half>.CreateSaturating<int>(0x00000000));
            AssertBitwiseEqual(PositiveOne, NumberHelper<Half>.CreateSaturating<int>(0x00000001));
            AssertBitwiseEqual((Half)2147483647.0f, NumberHelper<Half>.CreateSaturating<int>(0x7FFFFFFF));
            AssertBitwiseEqual((Half)(-2147483648.0f), NumberHelper<Half>.CreateSaturating<int>(unchecked((int)0x80000000)));
            AssertBitwiseEqual(NegativeOne, NumberHelper<Half>.CreateSaturating<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromInt64Test()
        {
            AssertBitwiseEqual(PositiveZero, NumberHelper<Half>.CreateSaturating<long>(0x0000000000000000));
            AssertBitwiseEqual(PositiveOne, NumberHelper<Half>.CreateSaturating<long>(0x0000000000000001));
            AssertBitwiseEqual((Half)9223372036854775807.0f, NumberHelper<Half>.CreateSaturating<long>(0x7FFFFFFFFFFFFFFF));
            AssertBitwiseEqual((Half)(-9223372036854775808.0f), NumberHelper<Half>.CreateSaturating<long>(unchecked(unchecked((long)0x8000000000000000))));
            AssertBitwiseEqual(NegativeOne, NumberHelper<Half>.CreateSaturating<long>(unchecked(unchecked((long)0xFFFFFFFFFFFFFFFF))));
        }

        [Fact]
        public static void CreateSaturatingFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual(PositiveZero, NumberHelper<Half>.CreateSaturating<nint>(unchecked((nint)0x0000000000000000)));
                AssertBitwiseEqual(PositiveOne, NumberHelper<Half>.CreateSaturating<nint>(unchecked((nint)0x0000000000000001)));
                AssertBitwiseEqual((Half)9223372036854775807.0f, NumberHelper<Half>.CreateSaturating<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                AssertBitwiseEqual((Half)(-9223372036854775808.0f), NumberHelper<Half>.CreateSaturating<nint>(unchecked((nint)0x8000000000000000)));
                AssertBitwiseEqual(NegativeOne, NumberHelper<Half>.CreateSaturating<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                AssertBitwiseEqual(PositiveZero, NumberHelper<Half>.CreateSaturating<nint>((nint)0x00000000));
                AssertBitwiseEqual(PositiveOne, NumberHelper<Half>.CreateSaturating<nint>((nint)0x00000001));
                AssertBitwiseEqual((Half)2147483647.0f, NumberHelper<Half>.CreateSaturating<nint>((nint)0x7FFFFFFF));
                AssertBitwiseEqual((Half)(-2147483648.0f), NumberHelper<Half>.CreateSaturating<nint>(unchecked((nint)0x80000000)));
                AssertBitwiseEqual(NegativeOne, NumberHelper<Half>.CreateSaturating<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateSaturatingFromSByteTest()
        {
            AssertBitwiseEqual(PositiveZero, NumberHelper<Half>.CreateSaturating<sbyte>(0x00));
            AssertBitwiseEqual(PositiveOne, NumberHelper<Half>.CreateSaturating<sbyte>(0x01));
            AssertBitwiseEqual((Half)127.0f, NumberHelper<Half>.CreateSaturating<sbyte>(0x7F));
            AssertBitwiseEqual((Half)(-128.0f), NumberHelper<Half>.CreateSaturating<sbyte>(unchecked((sbyte)0x80)));
            AssertBitwiseEqual(NegativeOne, NumberHelper<Half>.CreateSaturating<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateSaturatingFromUInt16Test()
        {
            AssertBitwiseEqual(PositiveZero, NumberHelper<Half>.CreateSaturating<ushort>(0x0000));
            AssertBitwiseEqual(PositiveOne, NumberHelper<Half>.CreateSaturating<ushort>(0x0001));
            AssertBitwiseEqual((Half)32767.0f, NumberHelper<Half>.CreateSaturating<ushort>(0x7FFF));
            AssertBitwiseEqual((Half)32768.0f, NumberHelper<Half>.CreateSaturating<ushort>(0x8000));
            AssertBitwiseEqual((Half)65535.0f, NumberHelper<Half>.CreateSaturating<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUInt32Test()
        {
            AssertBitwiseEqual(PositiveZero, NumberHelper<Half>.CreateSaturating<uint>(0x00000000));
            AssertBitwiseEqual(PositiveOne, NumberHelper<Half>.CreateSaturating<uint>(0x00000001));
            AssertBitwiseEqual((Half)2147483647.0f, NumberHelper<Half>.CreateSaturating<uint>(0x7FFFFFFF));
            AssertBitwiseEqual((Half)2147483648.0f, NumberHelper<Half>.CreateSaturating<uint>(0x80000000));
            AssertBitwiseEqual((Half)4294967295.0f, NumberHelper<Half>.CreateSaturating<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUInt64Test()
        {
            AssertBitwiseEqual(PositiveZero, NumberHelper<Half>.CreateSaturating<ulong>(0x0000000000000000));
            AssertBitwiseEqual(PositiveOne, NumberHelper<Half>.CreateSaturating<ulong>(0x0000000000000001));
            AssertBitwiseEqual((Half)9223372036854775807.0f, NumberHelper<Half>.CreateSaturating<ulong>(0x7FFFFFFFFFFFFFFF));
            AssertBitwiseEqual((Half)9223372036854775808.0f, NumberHelper<Half>.CreateSaturating<ulong>(0x8000000000000000));
            AssertBitwiseEqual((Half)18446744073709551615.0f, NumberHelper<Half>.CreateSaturating<ulong>(0xFFFFFFFFFFFFFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual(PositiveZero, NumberHelper<Half>.CreateSaturating<nuint>(unchecked((nuint)0x0000000000000000)));
                AssertBitwiseEqual(PositiveOne, NumberHelper<Half>.CreateSaturating<nuint>(unchecked((nuint)0x0000000000000001)));
                AssertBitwiseEqual((Half)9223372036854775807.0f, NumberHelper<Half>.CreateSaturating<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));

                // https://github.com/dotnet/roslyn/issues/60714
                // AssertBitwiseEqual((Half)9223372036854775808.0f, NumberHelper<Half>.CreateSaturating<nuint>(unchecked((nuint)0x8000000000000000)));
                // AssertBitwiseEqual((Half)18446744073709551615.0f, NumberHelper<Half>.CreateSaturating<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                AssertBitwiseEqual(PositiveZero, NumberHelper<Half>.CreateSaturating<nuint>((nuint)0x00000000));
                AssertBitwiseEqual(PositiveOne, NumberHelper<Half>.CreateSaturating<nuint>((nuint)0x00000001));
                AssertBitwiseEqual((Half)2147483647.0f, NumberHelper<Half>.CreateSaturating<nuint>((nuint)0x7FFFFFFF));

                // https://github.com/dotnet/roslyn/issues/60714
                // AssertBitwiseEqual((Half)2147483648.0f, NumberHelper<Half>.CreateSaturating<nuint>((nuint)0x80000000));
                // AssertBitwiseEqual((Half)4294967295.0f, NumberHelper<Half>.CreateSaturating<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateTruncatingFromByteTest()
        {
            AssertBitwiseEqual(PositiveZero, NumberHelper<Half>.CreateTruncating<byte>(0x00));
            AssertBitwiseEqual(PositiveOne, NumberHelper<Half>.CreateTruncating<byte>(0x01));
            AssertBitwiseEqual((Half)127.0f, NumberHelper<Half>.CreateTruncating<byte>(0x7F));
            AssertBitwiseEqual((Half)128.0f, NumberHelper<Half>.CreateTruncating<byte>(0x80));
            AssertBitwiseEqual((Half)255.0f, NumberHelper<Half>.CreateTruncating<byte>(0xFF));
        }

        [Fact]
        public static void CreateTruncatingFromCharTest()
        {
            AssertBitwiseEqual(PositiveZero, NumberHelper<Half>.CreateTruncating<char>((char)0x0000));
            AssertBitwiseEqual(PositiveOne, NumberHelper<Half>.CreateTruncating<char>((char)0x0001));
            AssertBitwiseEqual((Half)32767.0f, NumberHelper<Half>.CreateTruncating<char>((char)0x7FFF));
            AssertBitwiseEqual((Half)32768.0f, NumberHelper<Half>.CreateTruncating<char>((char)0x8000));
            AssertBitwiseEqual((Half)65535.0f, NumberHelper<Half>.CreateTruncating<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromInt16Test()
        {
            AssertBitwiseEqual(PositiveZero, NumberHelper<Half>.CreateTruncating<short>(0x0000));
            AssertBitwiseEqual(PositiveOne, NumberHelper<Half>.CreateTruncating<short>(0x0001));
            AssertBitwiseEqual((Half)32767.0f, NumberHelper<Half>.CreateTruncating<short>(0x7FFF));
            AssertBitwiseEqual((Half)(-32768.0f), NumberHelper<Half>.CreateTruncating<short>(unchecked((short)0x8000)));
            AssertBitwiseEqual(NegativeOne, NumberHelper<Half>.CreateTruncating<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateTruncatingFromInt32Test()
        {
            AssertBitwiseEqual(PositiveZero, NumberHelper<Half>.CreateTruncating<int>(0x00000000));
            AssertBitwiseEqual(PositiveOne, NumberHelper<Half>.CreateTruncating<int>(0x00000001));
            AssertBitwiseEqual((Half)2147483647.0f, NumberHelper<Half>.CreateTruncating<int>(0x7FFFFFFF));
            AssertBitwiseEqual((Half)(-2147483648.0f), NumberHelper<Half>.CreateTruncating<int>(unchecked((int)0x80000000)));
            AssertBitwiseEqual(NegativeOne, NumberHelper<Half>.CreateTruncating<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateTruncatingFromInt64Test()
        {
            AssertBitwiseEqual(PositiveZero, NumberHelper<Half>.CreateTruncating<long>(0x0000000000000000));
            AssertBitwiseEqual(PositiveOne, NumberHelper<Half>.CreateTruncating<long>(0x0000000000000001));
            AssertBitwiseEqual((Half)9223372036854775807.0f, NumberHelper<Half>.CreateTruncating<long>(0x7FFFFFFFFFFFFFFF));
            AssertBitwiseEqual((Half)(-9223372036854775808.0f), NumberHelper<Half>.CreateTruncating<long>(unchecked(unchecked((long)0x8000000000000000))));
            AssertBitwiseEqual(NegativeOne, NumberHelper<Half>.CreateTruncating<long>(unchecked(unchecked((long)0xFFFFFFFFFFFFFFFF))));
        }

        [Fact]
        public static void CreateTruncatingFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual(PositiveZero, NumberHelper<Half>.CreateTruncating<nint>(unchecked((nint)0x0000000000000000)));
                AssertBitwiseEqual(PositiveOne, NumberHelper<Half>.CreateTruncating<nint>(unchecked((nint)0x0000000000000001)));
                AssertBitwiseEqual((Half)9223372036854775807.0f, NumberHelper<Half>.CreateTruncating<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                AssertBitwiseEqual((Half)(-9223372036854775808.0f), NumberHelper<Half>.CreateTruncating<nint>(unchecked((nint)0x8000000000000000)));
                AssertBitwiseEqual(NegativeOne, NumberHelper<Half>.CreateTruncating<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                AssertBitwiseEqual(PositiveZero, NumberHelper<Half>.CreateTruncating<nint>((nint)0x00000000));
                AssertBitwiseEqual(PositiveOne, NumberHelper<Half>.CreateTruncating<nint>((nint)0x00000001));
                AssertBitwiseEqual((Half)2147483647.0f, NumberHelper<Half>.CreateTruncating<nint>((nint)0x7FFFFFFF));
                AssertBitwiseEqual((Half)(-2147483648.0f), NumberHelper<Half>.CreateTruncating<nint>(unchecked((nint)0x80000000)));
                AssertBitwiseEqual(NegativeOne, NumberHelper<Half>.CreateTruncating<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateTruncatingFromSByteTest()
        {
            AssertBitwiseEqual(PositiveZero, NumberHelper<Half>.CreateTruncating<sbyte>(0x00));
            AssertBitwiseEqual(PositiveOne, NumberHelper<Half>.CreateTruncating<sbyte>(0x01));
            AssertBitwiseEqual((Half)127.0f, NumberHelper<Half>.CreateTruncating<sbyte>(0x7F));
            AssertBitwiseEqual((Half)(-128.0f), NumberHelper<Half>.CreateTruncating<sbyte>(unchecked((sbyte)0x80)));
            AssertBitwiseEqual(NegativeOne, NumberHelper<Half>.CreateTruncating<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateTruncatingFromUInt16Test()
        {
            AssertBitwiseEqual(PositiveZero, NumberHelper<Half>.CreateTruncating<ushort>(0x0000));
            AssertBitwiseEqual(PositiveOne, NumberHelper<Half>.CreateTruncating<ushort>(0x0001));
            AssertBitwiseEqual((Half)32767.0f, NumberHelper<Half>.CreateTruncating<ushort>(0x7FFF));
            AssertBitwiseEqual((Half)32768.0f, NumberHelper<Half>.CreateTruncating<ushort>(0x8000));
            AssertBitwiseEqual((Half)65535.0f, NumberHelper<Half>.CreateTruncating<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt32Test()
        {
            AssertBitwiseEqual(PositiveZero, NumberHelper<Half>.CreateTruncating<uint>(0x00000000));
            AssertBitwiseEqual(PositiveOne, NumberHelper<Half>.CreateTruncating<uint>(0x00000001));
            AssertBitwiseEqual((Half)2147483647.0f, NumberHelper<Half>.CreateTruncating<uint>(0x7FFFFFFF));
            AssertBitwiseEqual((Half)2147483648.0f, NumberHelper<Half>.CreateTruncating<uint>(0x80000000));
            AssertBitwiseEqual((Half)4294967295.0f, NumberHelper<Half>.CreateTruncating<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt64Test()
        {
            AssertBitwiseEqual(PositiveZero, NumberHelper<Half>.CreateTruncating<ulong>(0x0000000000000000));
            AssertBitwiseEqual(PositiveOne, NumberHelper<Half>.CreateTruncating<ulong>(0x0000000000000001));
            AssertBitwiseEqual((Half)9223372036854775807.0f, NumberHelper<Half>.CreateTruncating<ulong>(0x7FFFFFFFFFFFFFFF));
            AssertBitwiseEqual((Half)9223372036854775808.0f, NumberHelper<Half>.CreateTruncating<ulong>(0x8000000000000000));
            AssertBitwiseEqual((Half)18446744073709551615.0f, NumberHelper<Half>.CreateTruncating<ulong>(0xFFFFFFFFFFFFFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual(PositiveZero, NumberHelper<Half>.CreateTruncating<nuint>(unchecked((nuint)0x0000000000000000)));
                AssertBitwiseEqual(PositiveOne, NumberHelper<Half>.CreateTruncating<nuint>(unchecked((nuint)0x0000000000000001)));
                AssertBitwiseEqual((Half)9223372036854775807.0f, NumberHelper<Half>.CreateTruncating<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));

                // https://github.com/dotnet/roslyn/issues/60714
                // AssertBitwiseEqual((Half)9223372036854775808.0f, NumberHelper<Half>.CreateTruncating<nuint>(unchecked((nuint)0x8000000000000000)));
                // AssertBitwiseEqual((Half)18446744073709551615.0f, NumberHelper<Half>.CreateTruncating<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                AssertBitwiseEqual(PositiveZero, NumberHelper<Half>.CreateTruncating<nuint>((nuint)0x00000000));
                AssertBitwiseEqual(PositiveOne, NumberHelper<Half>.CreateTruncating<nuint>((nuint)0x00000001));
                AssertBitwiseEqual((Half)2147483647.0f, NumberHelper<Half>.CreateTruncating<nuint>((nuint)0x7FFFFFFF));

                // https://github.com/dotnet/roslyn/issues/60714
                // AssertBitwiseEqual((Half)2147483648.0f, NumberHelper<Half>.CreateTruncating<nuint>((nuint)0x80000000));
                // AssertBitwiseEqual((Half)4294967295.0f, NumberHelper<Half>.CreateTruncating<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void MaxTest()
        {
            AssertBitwiseEqual(PositiveOne, NumberHelper<Half>.Max(Half.NegativeInfinity, PositiveOne));
            AssertBitwiseEqual(PositiveOne, NumberHelper<Half>.Max(Half.MinValue, PositiveOne));
            AssertBitwiseEqual(PositiveOne, NumberHelper<Half>.Max(NegativeOne, PositiveOne));
            AssertBitwiseEqual(PositiveOne, NumberHelper<Half>.Max(-MinNormal, PositiveOne));
            AssertBitwiseEqual(PositiveOne, NumberHelper<Half>.Max(-MaxSubnormal, PositiveOne));
            AssertBitwiseEqual(PositiveOne, NumberHelper<Half>.Max(-Half.Epsilon, PositiveOne));
            AssertBitwiseEqual(PositiveOne, NumberHelper<Half>.Max(NegativeZero, PositiveOne));
            AssertBitwiseEqual(Half.NaN, NumberHelper<Half>.Max(Half.NaN, PositiveOne));
            AssertBitwiseEqual(PositiveOne, NumberHelper<Half>.Max(PositiveZero, PositiveOne));
            AssertBitwiseEqual(PositiveOne, NumberHelper<Half>.Max(Half.Epsilon, PositiveOne));
            AssertBitwiseEqual(PositiveOne, NumberHelper<Half>.Max(MaxSubnormal, PositiveOne));
            AssertBitwiseEqual(PositiveOne, NumberHelper<Half>.Max(MinNormal, PositiveOne));
            AssertBitwiseEqual(PositiveOne, NumberHelper<Half>.Max(PositiveOne, PositiveOne));
            AssertBitwiseEqual(Half.MaxValue, NumberHelper<Half>.Max(Half.MaxValue, PositiveOne));
            AssertBitwiseEqual(Half.PositiveInfinity, NumberHelper<Half>.Max(Half.PositiveInfinity, PositiveOne));
        }

        [Fact]
        public static void MinTest()
        {
            AssertBitwiseEqual(Half.NegativeInfinity, NumberHelper<Half>.Min(Half.NegativeInfinity, PositiveOne));
            AssertBitwiseEqual(Half.MinValue, NumberHelper<Half>.Min(Half.MinValue, PositiveOne));
            AssertBitwiseEqual(NegativeOne, NumberHelper<Half>.Min(NegativeOne, PositiveOne));
            AssertBitwiseEqual(-MinNormal, NumberHelper<Half>.Min(-MinNormal, PositiveOne));
            AssertBitwiseEqual(-MaxSubnormal, NumberHelper<Half>.Min(-MaxSubnormal, PositiveOne));
            AssertBitwiseEqual(-Half.Epsilon, NumberHelper<Half>.Min(-Half.Epsilon, PositiveOne));
            AssertBitwiseEqual(NegativeZero, NumberHelper<Half>.Min(NegativeZero, PositiveOne));
            AssertBitwiseEqual(Half.NaN, NumberHelper<Half>.Min(Half.NaN, PositiveOne));
            AssertBitwiseEqual(PositiveZero, NumberHelper<Half>.Min(PositiveZero, PositiveOne));
            AssertBitwiseEqual(Half.Epsilon, NumberHelper<Half>.Min(Half.Epsilon, PositiveOne));
            AssertBitwiseEqual(MaxSubnormal, NumberHelper<Half>.Min(MaxSubnormal, PositiveOne));
            AssertBitwiseEqual(MinNormal, NumberHelper<Half>.Min(MinNormal, PositiveOne));
            AssertBitwiseEqual(PositiveOne, NumberHelper<Half>.Min(PositiveOne, PositiveOne));
            AssertBitwiseEqual(PositiveOne, NumberHelper<Half>.Min(Half.MaxValue, PositiveOne));
            AssertBitwiseEqual(PositiveOne, NumberHelper<Half>.Min(Half.PositiveInfinity, PositiveOne));
        }

        [Fact]
        public static void SignTest()
        {
            Assert.Equal(-1, NumberHelper<Half>.Sign(Half.NegativeInfinity));
            Assert.Equal(-1, NumberHelper<Half>.Sign(Half.MinValue));
            Assert.Equal(-1, NumberHelper<Half>.Sign(NegativeOne));
            Assert.Equal(-1, NumberHelper<Half>.Sign(-MinNormal));
            Assert.Equal(-1, NumberHelper<Half>.Sign(-MaxSubnormal));
            Assert.Equal(-1, NumberHelper<Half>.Sign(-Half.Epsilon));

            Assert.Equal(0, NumberHelper<Half>.Sign(NegativeZero));
            Assert.Equal(0, NumberHelper<Half>.Sign(PositiveZero));

            Assert.Equal(1, NumberHelper<Half>.Sign(Half.Epsilon));
            Assert.Equal(1, NumberHelper<Half>.Sign(MaxSubnormal));
            Assert.Equal(1, NumberHelper<Half>.Sign(MinNormal));
            Assert.Equal(1, NumberHelper<Half>.Sign(PositiveOne));
            Assert.Equal(1, NumberHelper<Half>.Sign(Half.MaxValue));
            Assert.Equal(1, NumberHelper<Half>.Sign(Half.PositiveInfinity));

            Assert.Throws<ArithmeticException>(() => NumberHelper<Half>.Sign(Half.NaN));
        }

        [Fact]
        public static void TryCreateFromByteTest()
        {
            Half result;

            Assert.True(NumberHelper<Half>.TryCreate<byte>(0x00, out result));
            Assert.Equal(PositiveZero, result);

            Assert.True(NumberHelper<Half>.TryCreate<byte>(0x01, out result));
            Assert.Equal(PositiveOne, result);

            Assert.True(NumberHelper<Half>.TryCreate<byte>(0x7F, out result));
            Assert.Equal((Half)127.0f, result);

            Assert.True(NumberHelper<Half>.TryCreate<byte>(0x80, out result));
            Assert.Equal((Half)128.0f, result);

            Assert.True(NumberHelper<Half>.TryCreate<byte>(0xFF, out result));
            Assert.Equal((Half)255.0f, result);
        }

        [Fact]
        public static void TryCreateFromCharTest()
        {
            Half result;

            Assert.True(NumberHelper<Half>.TryCreate<char>((char)0x0000, out result));
            Assert.Equal(PositiveZero, result);

            Assert.True(NumberHelper<Half>.TryCreate<char>((char)0x0001, out result));
            Assert.Equal(PositiveOne, result);

            Assert.True(NumberHelper<Half>.TryCreate<char>((char)0x7FFF, out result));
            Assert.Equal((Half)32767.0f, result);

            Assert.True(NumberHelper<Half>.TryCreate<char>((char)0x8000, out result));
            Assert.Equal((Half)32768.0f, result);

            Assert.True(NumberHelper<Half>.TryCreate<char>((char)0xFFFF, out result));
            Assert.Equal((Half)65535.0f, result);
        }

        [Fact]
        public static void TryCreateFromInt16Test()
        {
            Half result;

            Assert.True(NumberHelper<Half>.TryCreate<short>(0x0000, out result));
            Assert.Equal(PositiveZero, result);

            Assert.True(NumberHelper<Half>.TryCreate<short>(0x0001, out result));
            Assert.Equal(PositiveOne, result);

            Assert.True(NumberHelper<Half>.TryCreate<short>(0x7FFF, out result));
            Assert.Equal((Half)32767.0f, result);

            Assert.True(NumberHelper<Half>.TryCreate<short>(unchecked((short)0x8000), out result));
            Assert.Equal((Half)(-32768.0f), result);

            Assert.True(NumberHelper<Half>.TryCreate<short>(unchecked((short)0xFFFF), out result));
            Assert.Equal(NegativeOne, result);
        }

        [Fact]
        public static void TryCreateFromInt32Test()
        {
            Half result;

            Assert.True(NumberHelper<Half>.TryCreate<int>(0x00000000, out result));
            Assert.Equal(PositiveZero, result);

            Assert.True(NumberHelper<Half>.TryCreate<int>(0x00000001, out result));
            Assert.Equal(PositiveOne, result);

            Assert.True(NumberHelper<Half>.TryCreate<int>(0x7FFFFFFF, out result));
            Assert.Equal((Half)2147483647.0f, result);

            Assert.True(NumberHelper<Half>.TryCreate<int>(unchecked((int)0x80000000), out result));
            Assert.Equal((Half)(-2147483648.0f), result);

            Assert.True(NumberHelper<Half>.TryCreate<int>(unchecked((int)0xFFFFFFFF), out result));
            Assert.Equal(NegativeOne, result);
        }

        [Fact]
        public static void TryCreateFromInt64Test()
        {
            Half result;

            Assert.True(NumberHelper<Half>.TryCreate<long>(0x0000000000000000, out result));
            Assert.Equal(PositiveZero, result);

            Assert.True(NumberHelper<Half>.TryCreate<long>(0x0000000000000001, out result));
            Assert.Equal(PositiveOne, result);

            Assert.True(NumberHelper<Half>.TryCreate<long>(0x7FFFFFFFFFFFFFFF, out result));
            Assert.Equal((Half)9223372036854775807.0f, result);

            Assert.True(NumberHelper<Half>.TryCreate<long>(unchecked(unchecked((long)0x8000000000000000)), out result));
            Assert.Equal((Half)(-9223372036854775808.0f), result);

            Assert.True(NumberHelper<Half>.TryCreate<long>(unchecked(unchecked((long)0xFFFFFFFFFFFFFFFF)), out result));
            Assert.Equal(NegativeOne, result);
        }

        [Fact]
        public static void TryCreateFromIntPtrTest()
        {
            Half result;

            if (Environment.Is64BitProcess)
            {
                Assert.True(NumberHelper<Half>.TryCreate<nint>(unchecked((nint)0x0000000000000000), out result));
                Assert.Equal(PositiveZero, result);

                Assert.True(NumberHelper<Half>.TryCreate<nint>(unchecked((nint)0x0000000000000001), out result));
                Assert.Equal(PositiveOne, result);

                Assert.True(NumberHelper<Half>.TryCreate<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF), out result));
                Assert.Equal((Half)9223372036854775807.0f, result);

                Assert.True(NumberHelper<Half>.TryCreate<nint>(unchecked((nint)0x8000000000000000), out result));
                Assert.Equal((Half)(-9223372036854775808.0f), result);

                Assert.True(NumberHelper<Half>.TryCreate<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF), out result));
                Assert.Equal(NegativeOne, result);
            }
            else
            {
                Assert.True(NumberHelper<Half>.TryCreate<nint>((nint)0x00000000, out result));
                Assert.Equal(PositiveZero, result);

                Assert.True(NumberHelper<Half>.TryCreate<nint>((nint)0x00000001, out result));
                Assert.Equal(PositiveOne, result);

                Assert.True(NumberHelper<Half>.TryCreate<nint>((nint)0x7FFFFFFF, out result));
                Assert.Equal((Half)2147483647.0f, result);

                Assert.True(NumberHelper<Half>.TryCreate<nint>(unchecked((nint)0x80000000), out result));
                Assert.Equal((Half)(-2147483648.0f), result);

                Assert.True(NumberHelper<Half>.TryCreate<nint>(unchecked((nint)0xFFFFFFFF), out result));
                Assert.Equal(NegativeOne, result);
            }
        }

        [Fact]
        public static void TryCreateFromSByteTest()
        {
            Half result;

            Assert.True(NumberHelper<Half>.TryCreate<sbyte>(0x00, out result));
            Assert.Equal(PositiveZero, result);

            Assert.True(NumberHelper<Half>.TryCreate<sbyte>(0x01, out result));
            Assert.Equal(PositiveOne, result);

            Assert.True(NumberHelper<Half>.TryCreate<sbyte>(0x7F, out result));
            Assert.Equal((Half)127.0f, result);

            Assert.True(NumberHelper<Half>.TryCreate<sbyte>(unchecked((sbyte)0x80), out result));
            Assert.Equal((Half)(-128.0f), result);

            Assert.True(NumberHelper<Half>.TryCreate<sbyte>(unchecked((sbyte)0xFF), out result));
            Assert.Equal(NegativeOne, result);
        }

        [Fact]
        public static void TryCreateFromUInt16Test()
        {
            Half result;

            Assert.True(NumberHelper<Half>.TryCreate<ushort>(0x0000, out result));
            Assert.Equal(PositiveZero, result);

            Assert.True(NumberHelper<Half>.TryCreate<ushort>(0x0001, out result));
            Assert.Equal(PositiveOne, result);

            Assert.True(NumberHelper<Half>.TryCreate<ushort>(0x7FFF, out result));
            Assert.Equal((Half)32767.0f, result);

            Assert.True(NumberHelper<Half>.TryCreate<ushort>(0x8000, out result));
            Assert.Equal((Half)32768.0f, result);

            Assert.True(NumberHelper<Half>.TryCreate<ushort>(0xFFFF, out result));
            Assert.Equal((Half)65535.0f, result);
        }

        [Fact]
        public static void TryCreateFromUInt32Test()
        {
            Half result;

            Assert.True(NumberHelper<Half>.TryCreate<uint>(0x00000000, out result));
            Assert.Equal(PositiveZero, result);

            Assert.True(NumberHelper<Half>.TryCreate<uint>(0x00000001, out result));
            Assert.Equal(PositiveOne, result);

            Assert.True(NumberHelper<Half>.TryCreate<uint>(0x7FFFFFFF, out result));
            Assert.Equal((Half)2147483647.0f, result);

            Assert.True(NumberHelper<Half>.TryCreate<uint>(0x80000000, out result));
            Assert.Equal((Half)2147483648.0f, result);

            Assert.True(NumberHelper<Half>.TryCreate<uint>(0xFFFFFFFF, out result));
            Assert.Equal((Half)4294967295.0f, result);
        }

        [Fact]
        public static void TryCreateFromUInt64Test()
        {
            Half result;

            Assert.True(NumberHelper<Half>.TryCreate<ulong>(0x0000000000000000, out result));
            Assert.Equal(PositiveZero, result);

            Assert.True(NumberHelper<Half>.TryCreate<ulong>(0x0000000000000001, out result));
            Assert.Equal(PositiveOne, result);

            Assert.True(NumberHelper<Half>.TryCreate<ulong>(0x7FFFFFFFFFFFFFFF, out result));
            Assert.Equal((Half)9223372036854775807.0f, result);

            Assert.True(NumberHelper<Half>.TryCreate<ulong>(0x8000000000000000, out result));
            Assert.Equal((Half)9223372036854775808.0f, result);

            Assert.True(NumberHelper<Half>.TryCreate<ulong>(0xFFFFFFFFFFFFFFFF, out result));
            Assert.Equal((Half)18446744073709551615.0f, result);
        }

        [Fact]
        public static void TryCreateFromUIntPtrTest()
        {
            Half result;

            if (Environment.Is64BitProcess)
            {
                Assert.True(NumberHelper<Half>.TryCreate<nuint>(unchecked((nuint)0x0000000000000000), out result));
                Assert.Equal(PositiveZero, result);

                Assert.True(NumberHelper<Half>.TryCreate<nuint>(unchecked((nuint)0x0000000000000001), out result));
                Assert.Equal(PositiveOne, result);

                Assert.True(NumberHelper<Half>.TryCreate<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF), out result));
                Assert.Equal((Half)9223372036854775807.0f, result);

                // https://github.com/dotnet/roslyn/issues/60714
                // Assert.True(NumberHelper<Half>.TryCreate<nuint>(unchecked((nuint)0x8000000000000000), out result));
                // Assert.Equal((Half)9223372036854775808.0f, result);
                //
                // Assert.True(NumberHelper<Half>.TryCreate<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF), out result));
                // Assert.Equal((Half)18446744073709551615.0f, result);
            }
            else
            {
                Assert.True(NumberHelper<Half>.TryCreate<nuint>((nuint)0x00000000, out result));
                Assert.Equal(PositiveZero, result);

                Assert.True(NumberHelper<Half>.TryCreate<nuint>((nuint)0x00000001, out result));
                Assert.Equal(PositiveOne, result);

                Assert.True(NumberHelper<Half>.TryCreate<nuint>((nuint)0x7FFFFFFF, out result));
                Assert.Equal((Half)2147483647.0f, result);

                // https://github.com/dotnet/roslyn/issues/60714
                // Assert.True(NumberHelper<Half>.TryCreate<nuint>(unchecked((nuint)0x80000000), out result));
                // Assert.Equal((Half)2147483648.0f, result);
                //
                // Assert.True(NumberHelper<Half>.TryCreate<nuint>(unchecked((nuint)0xFFFFFFFF), out result));
                // Assert.Equal((Half)4294967295.0f, result);
            }
        }

        [Fact]
        public static void op_SubtractionTest()
        {
            AssertBitwiseEqual(Half.NegativeInfinity, SubtractionOperatorsHelper<Half, Half, Half>.op_Subtraction(Half.NegativeInfinity, PositiveOne));
            AssertBitwiseEqual(Half.MinValue, SubtractionOperatorsHelper<Half, Half, Half>.op_Subtraction(Half.MinValue, PositiveOne));
            AssertBitwiseEqual(NegativeTwo, SubtractionOperatorsHelper<Half, Half, Half>.op_Subtraction(NegativeOne, PositiveOne));
            AssertBitwiseEqual(NegativeOne, SubtractionOperatorsHelper<Half, Half, Half>.op_Subtraction(-MinNormal, PositiveOne));
            AssertBitwiseEqual(NegativeOne, SubtractionOperatorsHelper<Half, Half, Half>.op_Subtraction(-MaxSubnormal, PositiveOne));
            AssertBitwiseEqual(NegativeOne, SubtractionOperatorsHelper<Half, Half, Half>.op_Subtraction(-Half.Epsilon, PositiveOne));
            AssertBitwiseEqual(NegativeOne, SubtractionOperatorsHelper<Half, Half, Half>.op_Subtraction(NegativeZero, PositiveOne));
            AssertBitwiseEqual(Half.NaN, SubtractionOperatorsHelper<Half, Half, Half>.op_Subtraction(Half.NaN, PositiveOne));
            AssertBitwiseEqual(NegativeOne, SubtractionOperatorsHelper<Half, Half, Half>.op_Subtraction(PositiveZero, PositiveOne));
            AssertBitwiseEqual(NegativeOne, SubtractionOperatorsHelper<Half, Half, Half>.op_Subtraction(Half.Epsilon, PositiveOne));
            AssertBitwiseEqual(NegativeOne, SubtractionOperatorsHelper<Half, Half, Half>.op_Subtraction(MaxSubnormal, PositiveOne));
            AssertBitwiseEqual(NegativeOne, SubtractionOperatorsHelper<Half, Half, Half>.op_Subtraction(MinNormal, PositiveOne));
            AssertBitwiseEqual(PositiveZero, SubtractionOperatorsHelper<Half, Half, Half>.op_Subtraction(PositiveOne, PositiveOne));
            AssertBitwiseEqual(Half.MaxValue, SubtractionOperatorsHelper<Half, Half, Half>.op_Subtraction(Half.MaxValue, PositiveOne));
            AssertBitwiseEqual(Half.PositiveInfinity, SubtractionOperatorsHelper<Half, Half, Half>.op_Subtraction(Half.PositiveInfinity, PositiveOne));
        }

        [Fact]
        public static void op_CheckedSubtractionTest()
        {
            AssertBitwiseEqual(Half.NegativeInfinity, SubtractionOperatorsHelper<Half, Half, Half>.op_CheckedSubtraction(Half.NegativeInfinity, PositiveOne));
            AssertBitwiseEqual(Half.MinValue, SubtractionOperatorsHelper<Half, Half, Half>.op_CheckedSubtraction(Half.MinValue, PositiveOne));
            AssertBitwiseEqual(NegativeTwo, SubtractionOperatorsHelper<Half, Half, Half>.op_CheckedSubtraction(NegativeOne, PositiveOne));
            AssertBitwiseEqual(NegativeOne, SubtractionOperatorsHelper<Half, Half, Half>.op_CheckedSubtraction(-MinNormal, PositiveOne));
            AssertBitwiseEqual(NegativeOne, SubtractionOperatorsHelper<Half, Half, Half>.op_CheckedSubtraction(-MaxSubnormal, PositiveOne));
            AssertBitwiseEqual(NegativeOne, SubtractionOperatorsHelper<Half, Half, Half>.op_CheckedSubtraction(-Half.Epsilon, PositiveOne));
            AssertBitwiseEqual(NegativeOne, SubtractionOperatorsHelper<Half, Half, Half>.op_CheckedSubtraction(NegativeZero, PositiveOne));
            AssertBitwiseEqual(Half.NaN, SubtractionOperatorsHelper<Half, Half, Half>.op_CheckedSubtraction(Half.NaN, PositiveOne));
            AssertBitwiseEqual(NegativeOne, SubtractionOperatorsHelper<Half, Half, Half>.op_CheckedSubtraction(PositiveZero, PositiveOne));
            AssertBitwiseEqual(NegativeOne, SubtractionOperatorsHelper<Half, Half, Half>.op_CheckedSubtraction(Half.Epsilon, PositiveOne));
            AssertBitwiseEqual(NegativeOne, SubtractionOperatorsHelper<Half, Half, Half>.op_CheckedSubtraction(MaxSubnormal, PositiveOne));
            AssertBitwiseEqual(NegativeOne, SubtractionOperatorsHelper<Half, Half, Half>.op_CheckedSubtraction(MinNormal, PositiveOne));
            AssertBitwiseEqual(PositiveZero, SubtractionOperatorsHelper<Half, Half, Half>.op_CheckedSubtraction(PositiveOne, PositiveOne));
            AssertBitwiseEqual(Half.MaxValue, SubtractionOperatorsHelper<Half, Half, Half>.op_CheckedSubtraction(Half.MaxValue, PositiveOne));
            AssertBitwiseEqual(Half.PositiveInfinity, SubtractionOperatorsHelper<Half, Half, Half>.op_CheckedSubtraction(Half.PositiveInfinity, PositiveOne));
        }

        [Fact]
        public static void op_UnaryNegationTest()
        {
            AssertBitwiseEqual(Half.PositiveInfinity, UnaryNegationOperatorsHelper<Half, Half>.op_UnaryNegation(Half.NegativeInfinity));
            AssertBitwiseEqual(Half.MaxValue, UnaryNegationOperatorsHelper<Half, Half>.op_UnaryNegation(Half.MinValue));
            AssertBitwiseEqual(PositiveOne, UnaryNegationOperatorsHelper<Half, Half>.op_UnaryNegation(NegativeOne));
            AssertBitwiseEqual(MinNormal, UnaryNegationOperatorsHelper<Half, Half>.op_UnaryNegation(-MinNormal));
            AssertBitwiseEqual(MaxSubnormal, UnaryNegationOperatorsHelper<Half, Half>.op_UnaryNegation(-MaxSubnormal));
            AssertBitwiseEqual(Half.Epsilon, UnaryNegationOperatorsHelper<Half, Half>.op_UnaryNegation(-Half.Epsilon));
            AssertBitwiseEqual(PositiveZero, UnaryNegationOperatorsHelper<Half, Half>.op_UnaryNegation(NegativeZero));
            AssertBitwiseEqual(Half.NaN, UnaryNegationOperatorsHelper<Half, Half>.op_UnaryNegation(Half.NaN));
            AssertBitwiseEqual(NegativeZero, UnaryNegationOperatorsHelper<Half, Half>.op_UnaryNegation(PositiveZero));
            AssertBitwiseEqual(-Half.Epsilon, UnaryNegationOperatorsHelper<Half, Half>.op_UnaryNegation(Half.Epsilon));
            AssertBitwiseEqual(-MaxSubnormal, UnaryNegationOperatorsHelper<Half, Half>.op_UnaryNegation(MaxSubnormal));
            AssertBitwiseEqual(-MinNormal, UnaryNegationOperatorsHelper<Half, Half>.op_UnaryNegation(MinNormal));
            AssertBitwiseEqual(NegativeOne, UnaryNegationOperatorsHelper<Half, Half>.op_UnaryNegation(PositiveOne));
            AssertBitwiseEqual(Half.MinValue, UnaryNegationOperatorsHelper<Half, Half>.op_UnaryNegation(Half.MaxValue));
            AssertBitwiseEqual(Half.NegativeInfinity, UnaryNegationOperatorsHelper<Half, Half>.op_UnaryNegation(Half.PositiveInfinity));
        }

        [Fact]
        public static void op_CheckedUnaryNegationTest()
        {
            AssertBitwiseEqual(Half.PositiveInfinity, UnaryNegationOperatorsHelper<Half, Half>.op_CheckedUnaryNegation(Half.NegativeInfinity));
            AssertBitwiseEqual(Half.MaxValue, UnaryNegationOperatorsHelper<Half, Half>.op_CheckedUnaryNegation(Half.MinValue));
            AssertBitwiseEqual(PositiveOne, UnaryNegationOperatorsHelper<Half, Half>.op_CheckedUnaryNegation(NegativeOne));
            AssertBitwiseEqual(MinNormal, UnaryNegationOperatorsHelper<Half, Half>.op_CheckedUnaryNegation(-MinNormal));
            AssertBitwiseEqual(MaxSubnormal, UnaryNegationOperatorsHelper<Half, Half>.op_CheckedUnaryNegation(-MaxSubnormal));
            AssertBitwiseEqual(Half.Epsilon, UnaryNegationOperatorsHelper<Half, Half>.op_CheckedUnaryNegation(-Half.Epsilon));
            AssertBitwiseEqual(PositiveZero, UnaryNegationOperatorsHelper<Half, Half>.op_CheckedUnaryNegation(NegativeZero));
            AssertBitwiseEqual(Half.NaN, UnaryNegationOperatorsHelper<Half, Half>.op_CheckedUnaryNegation(Half.NaN));
            AssertBitwiseEqual(NegativeZero, UnaryNegationOperatorsHelper<Half, Half>.op_CheckedUnaryNegation(PositiveZero));
            AssertBitwiseEqual(-Half.Epsilon, UnaryNegationOperatorsHelper<Half, Half>.op_CheckedUnaryNegation(Half.Epsilon));
            AssertBitwiseEqual(-MaxSubnormal, UnaryNegationOperatorsHelper<Half, Half>.op_CheckedUnaryNegation(MaxSubnormal));
            AssertBitwiseEqual(-MinNormal, UnaryNegationOperatorsHelper<Half, Half>.op_CheckedUnaryNegation(MinNormal));
            AssertBitwiseEqual(NegativeOne, UnaryNegationOperatorsHelper<Half, Half>.op_CheckedUnaryNegation(PositiveOne));
            AssertBitwiseEqual(Half.MinValue, UnaryNegationOperatorsHelper<Half, Half>.op_CheckedUnaryNegation(Half.MaxValue));
            AssertBitwiseEqual(Half.NegativeInfinity, UnaryNegationOperatorsHelper<Half, Half>.op_CheckedUnaryNegation(Half.PositiveInfinity));
        }

        [Fact]
        public static void op_UnaryPlusTest()
        {
            AssertBitwiseEqual(Half.NegativeInfinity, UnaryPlusOperatorsHelper<Half, Half>.op_UnaryPlus(Half.NegativeInfinity));
            AssertBitwiseEqual(Half.MinValue, UnaryPlusOperatorsHelper<Half, Half>.op_UnaryPlus(Half.MinValue));
            AssertBitwiseEqual(NegativeOne, UnaryPlusOperatorsHelper<Half, Half>.op_UnaryPlus(NegativeOne));
            AssertBitwiseEqual(-MinNormal, UnaryPlusOperatorsHelper<Half, Half>.op_UnaryPlus(-MinNormal));
            AssertBitwiseEqual(-MaxSubnormal, UnaryPlusOperatorsHelper<Half, Half>.op_UnaryPlus(-MaxSubnormal));
            AssertBitwiseEqual(-Half.Epsilon, UnaryPlusOperatorsHelper<Half, Half>.op_UnaryPlus(-Half.Epsilon));
            AssertBitwiseEqual(NegativeZero, UnaryPlusOperatorsHelper<Half, Half>.op_UnaryPlus(NegativeZero));
            AssertBitwiseEqual(Half.NaN, UnaryPlusOperatorsHelper<Half, Half>.op_UnaryPlus(Half.NaN));
            AssertBitwiseEqual(PositiveZero, UnaryPlusOperatorsHelper<Half, Half>.op_UnaryPlus(PositiveZero));
            AssertBitwiseEqual(Half.Epsilon, UnaryPlusOperatorsHelper<Half, Half>.op_UnaryPlus(Half.Epsilon));
            AssertBitwiseEqual(MaxSubnormal, UnaryPlusOperatorsHelper<Half, Half>.op_UnaryPlus(MaxSubnormal));
            AssertBitwiseEqual(MinNormal, UnaryPlusOperatorsHelper<Half, Half>.op_UnaryPlus(MinNormal));
            AssertBitwiseEqual(PositiveOne, UnaryPlusOperatorsHelper<Half, Half>.op_UnaryPlus(PositiveOne));
            AssertBitwiseEqual(Half.MaxValue, UnaryPlusOperatorsHelper<Half, Half>.op_UnaryPlus(Half.MaxValue));
            AssertBitwiseEqual(Half.PositiveInfinity, UnaryPlusOperatorsHelper<Half, Half>.op_UnaryPlus(Half.PositiveInfinity));
        }

        [Theory]
        [MemberData(nameof(HalfTests.Parse_Valid_TestData), MemberType = typeof(HalfTests))]
        public static void ParseValidStringTest(string value, NumberStyles style, IFormatProvider provider, Half expected)
        {
            bool isDefaultProvider = provider == null || provider == NumberFormatInfo.CurrentInfo;
            Half result;
            if ((style & ~(NumberStyles.Float | NumberStyles.AllowThousands)) == 0 && style != NumberStyles.None)
            {
                // Use Parse(string) or Parse(string, IFormatProvider)
                if (isDefaultProvider)
                {
                    Assert.True(NumberHelper<Half>.TryParse(value, null, out result));
                    Assert.Equal(expected, result);

                    Assert.Equal(expected, NumberHelper<Half>.Parse(value, null));
                }

                Assert.Equal(expected, NumberHelper<Half>.Parse(value, provider));
            }

            // Use Parse(string, NumberStyles, IFormatProvider)
            Assert.True(NumberHelper<Half>.TryParse(value, style, provider, out result));
            Assert.Equal(expected, result);

            Assert.Equal(expected, NumberHelper<Half>.Parse(value, style, provider));

            if (isDefaultProvider)
            {
                // Use Parse(string, NumberStyles) or Parse(string, NumberStyles, IFormatProvider)
                Assert.True(NumberHelper<Half>.TryParse(value, style, NumberFormatInfo.CurrentInfo, out result));
                Assert.Equal(expected, result);

                Assert.Equal(expected, NumberHelper<Half>.Parse(value, style, null));
                Assert.Equal(expected, NumberHelper<Half>.Parse(value, style, NumberFormatInfo.CurrentInfo));
            }
        }

        [Theory]
        [MemberData(nameof(HalfTests.Parse_Invalid_TestData), MemberType = typeof(HalfTests))]
        public static void ParseInvalidStringTest(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            bool isDefaultProvider = provider == null || provider == NumberFormatInfo.CurrentInfo;
            Half result;
            if ((style & ~(NumberStyles.Float | NumberStyles.AllowThousands)) == 0 && style != NumberStyles.None && (style & NumberStyles.AllowLeadingWhite) == (style & NumberStyles.AllowTrailingWhite))
            {
                // Use Parse(string) or Parse(string, IFormatProvider)
                if (isDefaultProvider)
                {
                    Assert.False(NumberHelper<Half>.TryParse(value, null, out result));
                    Assert.Equal(default(Half), result);

                    Assert.Throws(exceptionType, () => NumberHelper<Half>.Parse(value, null));
                }

                Assert.Throws(exceptionType, () => NumberHelper<Half>.Parse(value, provider));
            }

            // Use Parse(string, NumberStyles, IFormatProvider)
            Assert.False(NumberHelper<Half>.TryParse(value, style, provider, out result));
            Assert.Equal(default(Half), result);

            Assert.Throws(exceptionType, () => NumberHelper<Half>.Parse(value, style, provider));

            if (isDefaultProvider)
            {
                // Use Parse(string, NumberStyles) or Parse(string, NumberStyles, IFormatProvider)
                Assert.False(NumberHelper<Half>.TryParse(value, style, NumberFormatInfo.CurrentInfo, out result));
                Assert.Equal(default(Half), result);

                Assert.Throws(exceptionType, () => NumberHelper<Half>.Parse(value, style, null));
                Assert.Throws(exceptionType, () => NumberHelper<Half>.Parse(value, style, NumberFormatInfo.CurrentInfo));
            }
        }

        [Theory]
        [MemberData(nameof(HalfTests.Parse_ValidWithOffsetCount_TestData), MemberType = typeof(HalfTests))]
        public static void ParseValidSpanTest(string value, int offset, int count, NumberStyles style, IFormatProvider provider, Half expected)
        {
            bool isDefaultProvider = provider == null || provider == NumberFormatInfo.CurrentInfo;
            Half result;
            if ((style & ~(NumberStyles.Float | NumberStyles.AllowThousands)) == 0 && style != NumberStyles.None)
            {
                // Use Parse(string) or Parse(string, IFormatProvider)
                if (isDefaultProvider)
                {
                    Assert.True(NumberHelper<Half>.TryParse(value.AsSpan(offset, count), null, out result));
                    Assert.Equal(expected, result);

                    Assert.Equal(expected, NumberHelper<Half>.Parse(value.AsSpan(offset, count), null));
                }

                Assert.Equal(expected, NumberHelper<Half>.Parse(value.AsSpan(offset, count), provider: provider));
            }

            Assert.Equal(expected, NumberHelper<Half>.Parse(value.AsSpan(offset, count), style, provider));

            Assert.True(NumberHelper<Half>.TryParse(value.AsSpan(offset, count), style, provider, out result));
            Assert.Equal(expected, result);
        }

        [Theory]
        [MemberData(nameof(HalfTests.Parse_Invalid_TestData), MemberType = typeof(HalfTests))]
        public static void ParseInvalidSpanTest(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            if (value != null)
            {
                Assert.Throws(exceptionType, () => NumberHelper<Half>.Parse(value.AsSpan(), style, provider));

                Assert.False(NumberHelper<Half>.TryParse(value.AsSpan(), style, provider, out Half result));
                Assert.Equal((Half)0, result);
            }
        }
    }
}
