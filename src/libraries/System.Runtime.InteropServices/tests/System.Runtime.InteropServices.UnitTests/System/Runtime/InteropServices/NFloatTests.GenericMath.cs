// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class NFloatTests_GenericMath
    {
        private static NFloat MinNormal
        {
            get
            {
                if (Environment.Is64BitProcess)
                {
                    return new NFloat(2.2250738585072014E-308);
                }
                else
                {
                    return new NFloat(1.17549435E-38f);
                }
            }
        }

        private static NFloat MaxSubnormal
        {
            get
            {
                if (Environment.Is64BitProcess)
                {
                    return new NFloat(2.2250738585072009E-308);
                }
                else
                {
                    return new NFloat(1.17549421E-38f);
                }
            }
        }

        private static NFloat NegativeOne
        {
            get
            {
                if (Environment.Is64BitProcess)
                {
                    return new NFloat(-1.0);
                }
                else
                {
                    return new NFloat(-1.0f);
                }
            }
        }

        private static NFloat NegativeTwo
        {
            get
            {
                if (Environment.Is64BitProcess)
                {
                    return new NFloat(-2.0);
                }
                else
                {
                    return new NFloat(-2.0f);
                }
            }
        }

        private static NFloat NegativeZero
        {
            get
            {
                if (Environment.Is64BitProcess)
                {
                    return new NFloat(-0.0);
                }
                else
                {
                    return new NFloat(-0.0f);
                }
            }
        }

        private static NFloat PositiveOne
        {
            get
            {
                if (Environment.Is64BitProcess)
                {
                    return new NFloat(1.0);
                }
                else
                {
                    return new NFloat(1.0f);
                }
            }
        }

        private static NFloat PositiveTwo
        {
            get
            {
                if (Environment.Is64BitProcess)
                {
                    return new NFloat(2.0);
                }
                else
                {
                    return new NFloat(2.0f);
                }
            }
        }

        private static NFloat PositiveZero
        {
            get
            {
                if (Environment.Is64BitProcess)
                {
                    return new NFloat(0.0);
                }
                else
                {
                    return new NFloat(0.0f);
                }
            }
        }

        private static void AssertBitwiseEqual(NFloat expected, NFloat actual)
        {
            if (Environment.Is64BitProcess)
            {
                ulong expectedBits = BitConverter.DoubleToUInt64Bits((double)expected);
                ulong actualBits = BitConverter.DoubleToUInt64Bits((double)actual);

                if (expectedBits == actualBits)
                {
                    return;
                }
            }
            else
            {
                uint expectedBits = BitConverter.SingleToUInt32Bits((float)expected);
                uint actualBits = BitConverter.SingleToUInt32Bits((float)actual);

                if (expectedBits == actualBits)
                {
                    return;
                }
            }

            if (NFloat.IsNaN(expected) && NFloat.IsNaN(actual))
            {
                return;
            }

            throw new Xunit.Sdk.EqualException(expected, actual);
        }

        [Fact]
        public static void AdditiveIdentityTest()
        {
            AssertBitwiseEqual(PositiveZero, AdditiveIdentityHelper<NFloat, NFloat>.AdditiveIdentity);
        }

        [Fact]
        public static void MinValueTest()
        {
            AssertBitwiseEqual(NFloat.MinValue, MinMaxValueHelper<NFloat>.MinValue);
        }

        [Fact]
        public static void MaxValueTest()
        {
            AssertBitwiseEqual(NFloat.MaxValue, MinMaxValueHelper<NFloat>.MaxValue);
        }

        [Fact]
        public static void MultiplicativeIdentityTest()
        {
            AssertBitwiseEqual(PositiveOne, MultiplicativeIdentityHelper<NFloat, NFloat>.MultiplicativeIdentity);
        }

        [Fact]
        public static void NegativeOneTest()
        {
            Assert.Equal(NegativeOne, SignedNumberHelper<NFloat>.NegativeOne);
        }

        [Fact]
        public static void OneTest()
        {
            AssertBitwiseEqual(PositiveOne, NumberBaseHelper<NFloat>.One);
        }

        [Fact]
        public static void ZeroTest()
        {
            AssertBitwiseEqual(PositiveZero, NumberBaseHelper<NFloat>.Zero);
        }

        [Fact]
        public static void op_AdditionTest()
        {
            AssertBitwiseEqual(NFloat.NegativeInfinity, AdditionOperatorsHelper<NFloat, NFloat, NFloat>.op_Addition(NFloat.NegativeInfinity, PositiveOne));
            AssertBitwiseEqual(NFloat.MinValue, AdditionOperatorsHelper<NFloat, NFloat, NFloat>.op_Addition(NFloat.MinValue, PositiveOne));
            AssertBitwiseEqual(PositiveZero, AdditionOperatorsHelper<NFloat, NFloat, NFloat>.op_Addition(NegativeOne, PositiveOne));
            AssertBitwiseEqual(PositiveOne, AdditionOperatorsHelper<NFloat, NFloat, NFloat>.op_Addition(-MinNormal, PositiveOne));
            AssertBitwiseEqual(PositiveOne, AdditionOperatorsHelper<NFloat, NFloat, NFloat>.op_Addition(-MaxSubnormal, PositiveOne));
            AssertBitwiseEqual(PositiveOne, AdditionOperatorsHelper<NFloat, NFloat, NFloat>.op_Addition(-NFloat.Epsilon, PositiveOne));
            AssertBitwiseEqual(PositiveOne, AdditionOperatorsHelper<NFloat, NFloat, NFloat>.op_Addition(NegativeZero, PositiveOne));
            AssertBitwiseEqual(NFloat.NaN, AdditionOperatorsHelper<NFloat, NFloat, NFloat>.op_Addition(NFloat.NaN, PositiveOne));
            AssertBitwiseEqual(PositiveOne, AdditionOperatorsHelper<NFloat, NFloat, NFloat>.op_Addition(PositiveZero, PositiveOne));
            AssertBitwiseEqual(PositiveOne, AdditionOperatorsHelper<NFloat, NFloat, NFloat>.op_Addition(NFloat.Epsilon, PositiveOne));
            AssertBitwiseEqual(PositiveOne, AdditionOperatorsHelper<NFloat, NFloat, NFloat>.op_Addition(MaxSubnormal, PositiveOne));
            AssertBitwiseEqual(PositiveOne, AdditionOperatorsHelper<NFloat, NFloat, NFloat>.op_Addition(MinNormal, PositiveOne));
            AssertBitwiseEqual(PositiveTwo, AdditionOperatorsHelper<NFloat, NFloat, NFloat>.op_Addition(PositiveOne, PositiveOne));
            AssertBitwiseEqual(NFloat.MaxValue, AdditionOperatorsHelper<NFloat, NFloat, NFloat>.op_Addition(NFloat.MaxValue, PositiveOne));
            AssertBitwiseEqual(NFloat.PositiveInfinity, AdditionOperatorsHelper<NFloat, NFloat, NFloat>.op_Addition(NFloat.PositiveInfinity, PositiveOne));
        }

        [Fact]
        public static void op_CheckedAdditionTest()
        {
            AssertBitwiseEqual(NFloat.NegativeInfinity, AdditionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedAddition(NFloat.NegativeInfinity, PositiveOne));
            AssertBitwiseEqual(NFloat.MinValue, AdditionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedAddition(NFloat.MinValue, PositiveOne));
            AssertBitwiseEqual(PositiveZero, AdditionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedAddition(NegativeOne, PositiveOne));
            AssertBitwiseEqual(PositiveOne, AdditionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedAddition(-MinNormal, PositiveOne));
            AssertBitwiseEqual(PositiveOne, AdditionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedAddition(-MaxSubnormal, PositiveOne));
            AssertBitwiseEqual(PositiveOne, AdditionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedAddition(-NFloat.Epsilon, PositiveOne));
            AssertBitwiseEqual(PositiveOne, AdditionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedAddition(NegativeZero, PositiveOne));
            AssertBitwiseEqual(NFloat.NaN, AdditionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedAddition(NFloat.NaN, PositiveOne));
            AssertBitwiseEqual(PositiveOne, AdditionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedAddition(PositiveZero, PositiveOne));
            AssertBitwiseEqual(PositiveOne, AdditionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedAddition(NFloat.Epsilon, PositiveOne));
            AssertBitwiseEqual(PositiveOne, AdditionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedAddition(MaxSubnormal, PositiveOne));
            AssertBitwiseEqual(PositiveOne, AdditionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedAddition(MinNormal, PositiveOne));
            AssertBitwiseEqual(PositiveTwo, AdditionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedAddition(PositiveOne, PositiveOne));
            AssertBitwiseEqual(NFloat.MaxValue, AdditionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedAddition(NFloat.MaxValue, PositiveOne));
            AssertBitwiseEqual(NFloat.PositiveInfinity, AdditionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedAddition(NFloat.PositiveInfinity, PositiveOne));
        }

        [Fact]
        public static void IsPow2Test()
        {
            Assert.False(BinaryNumberHelper<NFloat>.IsPow2(NFloat.NegativeInfinity));
            Assert.False(BinaryNumberHelper<NFloat>.IsPow2(NFloat.MinValue));
            Assert.False(BinaryNumberHelper<NFloat>.IsPow2(NegativeOne));
            Assert.False(BinaryNumberHelper<NFloat>.IsPow2(-MinNormal));
            Assert.False(BinaryNumberHelper<NFloat>.IsPow2(-MaxSubnormal));
            Assert.False(BinaryNumberHelper<NFloat>.IsPow2(-NFloat.Epsilon));
            Assert.False(BinaryNumberHelper<NFloat>.IsPow2(NegativeZero));
            Assert.False(BinaryNumberHelper<NFloat>.IsPow2(NFloat.NaN));
            Assert.False(BinaryNumberHelper<NFloat>.IsPow2(PositiveZero));
            Assert.False(BinaryNumberHelper<NFloat>.IsPow2(NFloat.Epsilon));
            Assert.False(BinaryNumberHelper<NFloat>.IsPow2(MaxSubnormal));
            Assert.True(BinaryNumberHelper<NFloat>.IsPow2(MinNormal));
            Assert.True(BinaryNumberHelper<NFloat>.IsPow2(PositiveOne));
            Assert.False(BinaryNumberHelper<NFloat>.IsPow2(NFloat.MaxValue));
            Assert.False(BinaryNumberHelper<NFloat>.IsPow2(NFloat.PositiveInfinity));
        }

        [Fact]
        public static void Log2Test()
        {
            AssertBitwiseEqual(NFloat.NaN, BinaryNumberHelper<NFloat>.Log2(NFloat.NegativeInfinity));
            AssertBitwiseEqual(NFloat.NaN, BinaryNumberHelper<NFloat>.Log2(NFloat.MinValue));
            AssertBitwiseEqual(NFloat.NaN, BinaryNumberHelper<NFloat>.Log2(NegativeOne));
            AssertBitwiseEqual(NFloat.NaN, BinaryNumberHelper<NFloat>.Log2(-MinNormal));
            AssertBitwiseEqual(NFloat.NaN, BinaryNumberHelper<NFloat>.Log2(-MaxSubnormal));
            AssertBitwiseEqual(NFloat.NaN, BinaryNumberHelper<NFloat>.Log2(-NFloat.Epsilon));
            AssertBitwiseEqual(NFloat.NegativeInfinity, BinaryNumberHelper<NFloat>.Log2(NegativeZero));
            AssertBitwiseEqual(NFloat.NaN, BinaryNumberHelper<NFloat>.Log2(NFloat.NaN));
            AssertBitwiseEqual(NFloat.NegativeInfinity, BinaryNumberHelper<NFloat>.Log2(PositiveZero));
            AssertBitwiseEqual(PositiveZero, BinaryNumberHelper<NFloat>.Log2(PositiveOne));
            AssertBitwiseEqual(NFloat.PositiveInfinity, BinaryNumberHelper<NFloat>.Log2(NFloat.PositiveInfinity));

            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual((NFloat)(-1074.0), BinaryNumberHelper<NFloat>.Log2(NFloat.Epsilon));
                AssertBitwiseEqual((NFloat)(-1022.0), BinaryNumberHelper<NFloat>.Log2(MaxSubnormal));
                AssertBitwiseEqual((NFloat)(-1022.0), BinaryNumberHelper<NFloat>.Log2(MinNormal));
                AssertBitwiseEqual((NFloat)1024.0, BinaryNumberHelper<NFloat>.Log2(NFloat.MaxValue));
            }
            else
            {
                AssertBitwiseEqual((NFloat)(-149.0f), BinaryNumberHelper<NFloat>.Log2(NFloat.Epsilon));
                AssertBitwiseEqual((NFloat)(-126.0f), BinaryNumberHelper<NFloat>.Log2(MaxSubnormal));
                AssertBitwiseEqual((NFloat)(-126.0f), BinaryNumberHelper<NFloat>.Log2(MinNormal));
                AssertBitwiseEqual((NFloat)128.0f, BinaryNumberHelper<NFloat>.Log2(NFloat.MaxValue));
            }
        }

        [Fact]
        public static void op_LessThanTest()
        {
            Assert.True(ComparisonOperatorsHelper<NFloat, NFloat>.op_LessThan(NFloat.NegativeInfinity, PositiveOne));
            Assert.True(ComparisonOperatorsHelper<NFloat, NFloat>.op_LessThan(NFloat.MinValue, PositiveOne));
            Assert.True(ComparisonOperatorsHelper<NFloat, NFloat>.op_LessThan(NegativeOne, PositiveOne));
            Assert.True(ComparisonOperatorsHelper<NFloat, NFloat>.op_LessThan(-MinNormal, PositiveOne));
            Assert.True(ComparisonOperatorsHelper<NFloat, NFloat>.op_LessThan(-MaxSubnormal, PositiveOne));
            Assert.True(ComparisonOperatorsHelper<NFloat, NFloat>.op_LessThan(-NFloat.Epsilon, PositiveOne));
            Assert.True(ComparisonOperatorsHelper<NFloat, NFloat>.op_LessThan(NegativeZero, PositiveOne));
            Assert.False(ComparisonOperatorsHelper<NFloat, NFloat>.op_LessThan(NFloat.NaN, PositiveOne));
            Assert.True(ComparisonOperatorsHelper<NFloat, NFloat>.op_LessThan(PositiveZero, PositiveOne));
            Assert.True(ComparisonOperatorsHelper<NFloat, NFloat>.op_LessThan(NFloat.Epsilon, PositiveOne));
            Assert.True(ComparisonOperatorsHelper<NFloat, NFloat>.op_LessThan(MaxSubnormal, PositiveOne));
            Assert.True(ComparisonOperatorsHelper<NFloat, NFloat>.op_LessThan(MinNormal, PositiveOne));
            Assert.False(ComparisonOperatorsHelper<NFloat, NFloat>.op_LessThan(PositiveOne, PositiveOne));
            Assert.False(ComparisonOperatorsHelper<NFloat, NFloat>.op_LessThan(NFloat.MaxValue, PositiveOne));
            Assert.False(ComparisonOperatorsHelper<NFloat, NFloat>.op_LessThan(NFloat.PositiveInfinity, PositiveOne));
        }

        [Fact]
        public static void op_LessThanOrEqualTest()
        {
            Assert.True(ComparisonOperatorsHelper<NFloat, NFloat>.op_LessThanOrEqual(NFloat.NegativeInfinity, PositiveOne));
            Assert.True(ComparisonOperatorsHelper<NFloat, NFloat>.op_LessThanOrEqual(NFloat.MinValue, PositiveOne));
            Assert.True(ComparisonOperatorsHelper<NFloat, NFloat>.op_LessThanOrEqual(NegativeOne, PositiveOne));
            Assert.True(ComparisonOperatorsHelper<NFloat, NFloat>.op_LessThanOrEqual(-MinNormal, PositiveOne));
            Assert.True(ComparisonOperatorsHelper<NFloat, NFloat>.op_LessThanOrEqual(-MaxSubnormal, PositiveOne));
            Assert.True(ComparisonOperatorsHelper<NFloat, NFloat>.op_LessThanOrEqual(-NFloat.Epsilon, PositiveOne));
            Assert.True(ComparisonOperatorsHelper<NFloat, NFloat>.op_LessThanOrEqual(NegativeZero, PositiveOne));
            Assert.False(ComparisonOperatorsHelper<NFloat, NFloat>.op_LessThanOrEqual(NFloat.NaN, PositiveOne));
            Assert.True(ComparisonOperatorsHelper<NFloat, NFloat>.op_LessThanOrEqual(PositiveZero, PositiveOne));
            Assert.True(ComparisonOperatorsHelper<NFloat, NFloat>.op_LessThanOrEqual(NFloat.Epsilon, PositiveOne));
            Assert.True(ComparisonOperatorsHelper<NFloat, NFloat>.op_LessThanOrEqual(MaxSubnormal, PositiveOne));
            Assert.True(ComparisonOperatorsHelper<NFloat, NFloat>.op_LessThanOrEqual(MinNormal, PositiveOne));
            Assert.True(ComparisonOperatorsHelper<NFloat, NFloat>.op_LessThanOrEqual(PositiveOne, PositiveOne));
            Assert.False(ComparisonOperatorsHelper<NFloat, NFloat>.op_LessThanOrEqual(NFloat.MaxValue, PositiveOne));
            Assert.False(ComparisonOperatorsHelper<NFloat, NFloat>.op_LessThanOrEqual(NFloat.PositiveInfinity, PositiveOne));
        }

        [Fact]
        public static void op_GreaterThanTest()
        {
            Assert.False(ComparisonOperatorsHelper<NFloat, NFloat>.op_GreaterThan(NFloat.NegativeInfinity, PositiveOne));
            Assert.False(ComparisonOperatorsHelper<NFloat, NFloat>.op_GreaterThan(NFloat.MinValue, PositiveOne));
            Assert.False(ComparisonOperatorsHelper<NFloat, NFloat>.op_GreaterThan(NegativeOne, PositiveOne));
            Assert.False(ComparisonOperatorsHelper<NFloat, NFloat>.op_GreaterThan(-MinNormal, PositiveOne));
            Assert.False(ComparisonOperatorsHelper<NFloat, NFloat>.op_GreaterThan(-MaxSubnormal, PositiveOne));
            Assert.False(ComparisonOperatorsHelper<NFloat, NFloat>.op_GreaterThan(-NFloat.Epsilon, PositiveOne));
            Assert.False(ComparisonOperatorsHelper<NFloat, NFloat>.op_GreaterThan(NegativeZero, PositiveOne));
            Assert.False(ComparisonOperatorsHelper<NFloat, NFloat>.op_GreaterThan(NFloat.NaN, PositiveOne));
            Assert.False(ComparisonOperatorsHelper<NFloat, NFloat>.op_GreaterThan(PositiveZero, PositiveOne));
            Assert.False(ComparisonOperatorsHelper<NFloat, NFloat>.op_GreaterThan(NFloat.Epsilon, PositiveOne));
            Assert.False(ComparisonOperatorsHelper<NFloat, NFloat>.op_GreaterThan(MaxSubnormal, PositiveOne));
            Assert.False(ComparisonOperatorsHelper<NFloat, NFloat>.op_GreaterThan(MinNormal, PositiveOne));
            Assert.False(ComparisonOperatorsHelper<NFloat, NFloat>.op_GreaterThan(PositiveOne, PositiveOne));
            Assert.True(ComparisonOperatorsHelper<NFloat, NFloat>.op_GreaterThan(NFloat.MaxValue, PositiveOne));
            Assert.True(ComparisonOperatorsHelper<NFloat, NFloat>.op_GreaterThan(NFloat.PositiveInfinity, PositiveOne));
        }

        [Fact]
        public static void op_GreaterThanOrEqualTest()
        {
            Assert.False(ComparisonOperatorsHelper<NFloat, NFloat>.op_GreaterThanOrEqual(NFloat.NegativeInfinity, PositiveOne));
            Assert.False(ComparisonOperatorsHelper<NFloat, NFloat>.op_GreaterThanOrEqual(NFloat.MinValue, PositiveOne));
            Assert.False(ComparisonOperatorsHelper<NFloat, NFloat>.op_GreaterThanOrEqual(NegativeOne, PositiveOne));
            Assert.False(ComparisonOperatorsHelper<NFloat, NFloat>.op_GreaterThanOrEqual(-MinNormal, PositiveOne));
            Assert.False(ComparisonOperatorsHelper<NFloat, NFloat>.op_GreaterThanOrEqual(-MaxSubnormal, PositiveOne));
            Assert.False(ComparisonOperatorsHelper<NFloat, NFloat>.op_GreaterThanOrEqual(-NFloat.Epsilon, PositiveOne));
            Assert.False(ComparisonOperatorsHelper<NFloat, NFloat>.op_GreaterThanOrEqual(NegativeZero, PositiveOne));
            Assert.False(ComparisonOperatorsHelper<NFloat, NFloat>.op_GreaterThanOrEqual(NFloat.NaN, PositiveOne));
            Assert.False(ComparisonOperatorsHelper<NFloat, NFloat>.op_GreaterThanOrEqual(PositiveZero, PositiveOne));
            Assert.False(ComparisonOperatorsHelper<NFloat, NFloat>.op_GreaterThanOrEqual(NFloat.Epsilon, PositiveOne));
            Assert.False(ComparisonOperatorsHelper<NFloat, NFloat>.op_GreaterThanOrEqual(MaxSubnormal, PositiveOne));
            Assert.False(ComparisonOperatorsHelper<NFloat, NFloat>.op_GreaterThanOrEqual(MinNormal, PositiveOne));
            Assert.True(ComparisonOperatorsHelper<NFloat, NFloat>.op_GreaterThanOrEqual(PositiveOne, PositiveOne));
            Assert.True(ComparisonOperatorsHelper<NFloat, NFloat>.op_GreaterThanOrEqual(NFloat.MaxValue, PositiveOne));
            Assert.True(ComparisonOperatorsHelper<NFloat, NFloat>.op_GreaterThanOrEqual(NFloat.PositiveInfinity, PositiveOne));
        }

        [Fact]
        public static void op_DecrementTest()
        {
            AssertBitwiseEqual(NFloat.NegativeInfinity, DecrementOperatorsHelper<NFloat>.op_Decrement(NFloat.NegativeInfinity));
            AssertBitwiseEqual(NFloat.MinValue, DecrementOperatorsHelper<NFloat>.op_Decrement(NFloat.MinValue));
            AssertBitwiseEqual(NegativeTwo, DecrementOperatorsHelper<NFloat>.op_Decrement(NegativeOne));
            AssertBitwiseEqual(NegativeOne, DecrementOperatorsHelper<NFloat>.op_Decrement(-MinNormal));
            AssertBitwiseEqual(NegativeOne, DecrementOperatorsHelper<NFloat>.op_Decrement(-MaxSubnormal));
            AssertBitwiseEqual(NegativeOne, DecrementOperatorsHelper<NFloat>.op_Decrement(-NFloat.Epsilon));
            AssertBitwiseEqual(NegativeOne, DecrementOperatorsHelper<NFloat>.op_Decrement(NegativeZero));
            AssertBitwiseEqual(NFloat.NaN, DecrementOperatorsHelper<NFloat>.op_Decrement(NFloat.NaN));
            AssertBitwiseEqual(NegativeOne, DecrementOperatorsHelper<NFloat>.op_Decrement(PositiveZero));
            AssertBitwiseEqual(NegativeOne, DecrementOperatorsHelper<NFloat>.op_Decrement(NFloat.Epsilon));
            AssertBitwiseEqual(NegativeOne, DecrementOperatorsHelper<NFloat>.op_Decrement(MaxSubnormal));
            AssertBitwiseEqual(NegativeOne, DecrementOperatorsHelper<NFloat>.op_Decrement(MinNormal));
            AssertBitwiseEqual(PositiveZero, DecrementOperatorsHelper<NFloat>.op_Decrement(PositiveOne));
            AssertBitwiseEqual(NFloat.MaxValue, DecrementOperatorsHelper<NFloat>.op_Decrement(NFloat.MaxValue));
            AssertBitwiseEqual(NFloat.PositiveInfinity, DecrementOperatorsHelper<NFloat>.op_Decrement(NFloat.PositiveInfinity));
        }

        [Fact]
        public static void op_CheckedDecrementTest()
        {
            AssertBitwiseEqual(NFloat.NegativeInfinity, DecrementOperatorsHelper<NFloat>.op_CheckedDecrement(NFloat.NegativeInfinity));
            AssertBitwiseEqual(NFloat.MinValue, DecrementOperatorsHelper<NFloat>.op_CheckedDecrement(NFloat.MinValue));
            AssertBitwiseEqual(NegativeTwo, DecrementOperatorsHelper<NFloat>.op_CheckedDecrement(NegativeOne));
            AssertBitwiseEqual(NegativeOne, DecrementOperatorsHelper<NFloat>.op_CheckedDecrement(-MinNormal));
            AssertBitwiseEqual(NegativeOne, DecrementOperatorsHelper<NFloat>.op_CheckedDecrement(-MaxSubnormal));
            AssertBitwiseEqual(NegativeOne, DecrementOperatorsHelper<NFloat>.op_CheckedDecrement(-NFloat.Epsilon));
            AssertBitwiseEqual(NegativeOne, DecrementOperatorsHelper<NFloat>.op_CheckedDecrement(NegativeZero));
            AssertBitwiseEqual(NFloat.NaN, DecrementOperatorsHelper<NFloat>.op_CheckedDecrement(NFloat.NaN));
            AssertBitwiseEqual(NegativeOne, DecrementOperatorsHelper<NFloat>.op_CheckedDecrement(PositiveZero));
            AssertBitwiseEqual(NegativeOne, DecrementOperatorsHelper<NFloat>.op_CheckedDecrement(NFloat.Epsilon));
            AssertBitwiseEqual(NegativeOne, DecrementOperatorsHelper<NFloat>.op_CheckedDecrement(MaxSubnormal));
            AssertBitwiseEqual(NegativeOne, DecrementOperatorsHelper<NFloat>.op_CheckedDecrement(MinNormal));
            AssertBitwiseEqual(PositiveZero, DecrementOperatorsHelper<NFloat>.op_CheckedDecrement(PositiveOne));
            AssertBitwiseEqual(NFloat.MaxValue, DecrementOperatorsHelper<NFloat>.op_CheckedDecrement(NFloat.MaxValue));
            AssertBitwiseEqual(NFloat.PositiveInfinity, DecrementOperatorsHelper<NFloat>.op_CheckedDecrement(NFloat.PositiveInfinity));
        }

        [Fact]
        public static void op_DivisionTest()
        {
            AssertBitwiseEqual(NFloat.NegativeInfinity, DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_Division(NFloat.NegativeInfinity, PositiveTwo));
            AssertBitwiseEqual((NFloat)(-0.5f), DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_Division(NegativeOne, PositiveTwo));
            AssertBitwiseEqual(NegativeZero, DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_Division(-NFloat.Epsilon, PositiveTwo));
            AssertBitwiseEqual(NegativeZero, DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_Division(NegativeZero, PositiveTwo));
            AssertBitwiseEqual(NFloat.NaN, DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_Division(NFloat.NaN, PositiveTwo));
            AssertBitwiseEqual(PositiveZero, DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_Division(PositiveZero, PositiveTwo));
            AssertBitwiseEqual(PositiveZero, DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_Division(NFloat.Epsilon, PositiveTwo));
            AssertBitwiseEqual((NFloat)0.5f, DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_Division(PositiveOne, PositiveTwo));
            AssertBitwiseEqual(NFloat.PositiveInfinity, DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_Division(NFloat.PositiveInfinity, PositiveTwo));

            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual((NFloat)(-8.9884656743115785E+307), DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_Division(NFloat.MinValue, PositiveTwo));
                AssertBitwiseEqual((NFloat)(-1.1125369292536007E-308), DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_Division(-MinNormal, PositiveTwo));
                AssertBitwiseEqual((NFloat)(-1.1125369292536007E-308), DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_Division(-MaxSubnormal, PositiveTwo));
                AssertBitwiseEqual((NFloat)1.1125369292536007E-308, DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_Division(MaxSubnormal, PositiveTwo));
                AssertBitwiseEqual((NFloat)1.1125369292536007E-308, DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_Division(MinNormal, PositiveTwo));
                AssertBitwiseEqual((NFloat)8.9884656743115785E+307, DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_Division(NFloat.MaxValue, PositiveTwo));
            }
            else
            {
                AssertBitwiseEqual((NFloat)(-1.70141173E+38f), DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_Division(NFloat.MinValue, PositiveTwo));
                AssertBitwiseEqual((NFloat)(-5.87747175E-39f), DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_Division(-MinNormal, PositiveTwo));
                AssertBitwiseEqual((NFloat)(-5.87747175E-39f), DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_Division(-MaxSubnormal, PositiveTwo));
                AssertBitwiseEqual((NFloat)5.87747175E-39f, DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_Division(MaxSubnormal, PositiveTwo));
                AssertBitwiseEqual((NFloat)5.87747175E-39f, DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_Division(MinNormal, PositiveTwo));
                AssertBitwiseEqual((NFloat)1.70141173E+38f, DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_Division(NFloat.MaxValue, PositiveTwo));
            }
        }

        [Fact]
        public static void op_CheckedDivisionTest()
        {
            AssertBitwiseEqual(NFloat.NegativeInfinity, DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedDivision(NFloat.NegativeInfinity, PositiveTwo));
            AssertBitwiseEqual((NFloat)(-0.5f), DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedDivision(NegativeOne, PositiveTwo));
            AssertBitwiseEqual(NegativeZero, DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedDivision(-NFloat.Epsilon, PositiveTwo));
            AssertBitwiseEqual(NegativeZero, DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedDivision(NegativeZero, PositiveTwo));
            AssertBitwiseEqual(NFloat.NaN, DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedDivision(NFloat.NaN, PositiveTwo));
            AssertBitwiseEqual(PositiveZero, DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedDivision(PositiveZero, PositiveTwo));
            AssertBitwiseEqual(PositiveZero, DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedDivision(NFloat.Epsilon, PositiveTwo));
            AssertBitwiseEqual((NFloat)0.5f, DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedDivision(PositiveOne, PositiveTwo));
            AssertBitwiseEqual(NFloat.PositiveInfinity, DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedDivision(NFloat.PositiveInfinity, PositiveTwo));

            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual((NFloat)(-8.9884656743115785E+307), DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedDivision(NFloat.MinValue, PositiveTwo));
                AssertBitwiseEqual((NFloat)(-1.1125369292536007E-308), DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedDivision(-MinNormal, PositiveTwo));
                AssertBitwiseEqual((NFloat)(-1.1125369292536007E-308), DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedDivision(-MaxSubnormal, PositiveTwo));
                AssertBitwiseEqual((NFloat)1.1125369292536007E-308, DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedDivision(MaxSubnormal, PositiveTwo));
                AssertBitwiseEqual((NFloat)1.1125369292536007E-308, DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedDivision(MinNormal, PositiveTwo));
                AssertBitwiseEqual((NFloat)8.9884656743115785E+307, DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedDivision(NFloat.MaxValue, PositiveTwo));
            }
            else
            {
                AssertBitwiseEqual((NFloat)(-1.70141173E+38f), DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedDivision(NFloat.MinValue, PositiveTwo));
                AssertBitwiseEqual((NFloat)(-5.87747175E-39f), DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedDivision(-MinNormal, PositiveTwo));
                AssertBitwiseEqual((NFloat)(-5.87747175E-39f), DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedDivision(-MaxSubnormal, PositiveTwo));
                AssertBitwiseEqual((NFloat)5.87747175E-39f, DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedDivision(MaxSubnormal, PositiveTwo));
                AssertBitwiseEqual((NFloat)5.87747175E-39f, DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedDivision(MinNormal, PositiveTwo));
                AssertBitwiseEqual((NFloat)1.70141173E+38f, DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedDivision(NFloat.MaxValue, PositiveTwo));
            }
        }

        [Fact]
        public static void op_EqualityTest()
        {
            Assert.False(EqualityOperatorsHelper<NFloat, NFloat>.op_Equality(NFloat.NegativeInfinity, PositiveOne));
            Assert.False(EqualityOperatorsHelper<NFloat, NFloat>.op_Equality(NFloat.MinValue, PositiveOne));
            Assert.False(EqualityOperatorsHelper<NFloat, NFloat>.op_Equality(NegativeOne, PositiveOne));
            Assert.False(EqualityOperatorsHelper<NFloat, NFloat>.op_Equality(-MinNormal, PositiveOne));
            Assert.False(EqualityOperatorsHelper<NFloat, NFloat>.op_Equality(-MaxSubnormal, PositiveOne));
            Assert.False(EqualityOperatorsHelper<NFloat, NFloat>.op_Equality(-NFloat.Epsilon, PositiveOne));
            Assert.False(EqualityOperatorsHelper<NFloat, NFloat>.op_Equality(NegativeZero, PositiveOne));
            Assert.False(EqualityOperatorsHelper<NFloat, NFloat>.op_Equality(NFloat.NaN, PositiveOne));
            Assert.False(EqualityOperatorsHelper<NFloat, NFloat>.op_Equality(PositiveZero, PositiveOne));
            Assert.False(EqualityOperatorsHelper<NFloat, NFloat>.op_Equality(NFloat.Epsilon, PositiveOne));
            Assert.False(EqualityOperatorsHelper<NFloat, NFloat>.op_Equality(MaxSubnormal, PositiveOne));
            Assert.False(EqualityOperatorsHelper<NFloat, NFloat>.op_Equality(MinNormal, PositiveOne));
            Assert.True(EqualityOperatorsHelper<NFloat, NFloat>.op_Equality(PositiveOne, PositiveOne));
            Assert.False(EqualityOperatorsHelper<NFloat, NFloat>.op_Equality(NFloat.MaxValue, PositiveOne));
            Assert.False(EqualityOperatorsHelper<NFloat, NFloat>.op_Equality(NFloat.PositiveInfinity, PositiveOne));
        }

        [Fact]
        public static void op_InequalityTest()
        {
            Assert.True(EqualityOperatorsHelper<NFloat, NFloat>.op_Inequality(NFloat.NegativeInfinity, PositiveOne));
            Assert.True(EqualityOperatorsHelper<NFloat, NFloat>.op_Inequality(NFloat.MinValue, PositiveOne));
            Assert.True(EqualityOperatorsHelper<NFloat, NFloat>.op_Inequality(NegativeOne, PositiveOne));
            Assert.True(EqualityOperatorsHelper<NFloat, NFloat>.op_Inequality(-MinNormal, PositiveOne));
            Assert.True(EqualityOperatorsHelper<NFloat, NFloat>.op_Inequality(-MaxSubnormal, PositiveOne));
            Assert.True(EqualityOperatorsHelper<NFloat, NFloat>.op_Inequality(-NFloat.Epsilon, PositiveOne));
            Assert.True(EqualityOperatorsHelper<NFloat, NFloat>.op_Inequality(NegativeZero, PositiveOne));
            Assert.True(EqualityOperatorsHelper<NFloat, NFloat>.op_Inequality(NFloat.NaN, PositiveOne));
            Assert.True(EqualityOperatorsHelper<NFloat, NFloat>.op_Inequality(PositiveZero, PositiveOne));
            Assert.True(EqualityOperatorsHelper<NFloat, NFloat>.op_Inequality(NFloat.Epsilon, PositiveOne));
            Assert.True(EqualityOperatorsHelper<NFloat, NFloat>.op_Inequality(MaxSubnormal, PositiveOne));
            Assert.True(EqualityOperatorsHelper<NFloat, NFloat>.op_Inequality(MinNormal, PositiveOne));
            Assert.False(EqualityOperatorsHelper<NFloat, NFloat>.op_Inequality(PositiveOne, PositiveOne));
            Assert.True(EqualityOperatorsHelper<NFloat, NFloat>.op_Inequality(NFloat.MaxValue, PositiveOne));
            Assert.True(EqualityOperatorsHelper<NFloat, NFloat>.op_Inequality(NFloat.PositiveInfinity, PositiveOne));
        }

        [Fact]
        public static void op_IncrementTest()
        {
            AssertBitwiseEqual(NFloat.NegativeInfinity, IncrementOperatorsHelper<NFloat>.op_Increment(NFloat.NegativeInfinity));
            AssertBitwiseEqual(NFloat.MinValue, IncrementOperatorsHelper<NFloat>.op_Increment(NFloat.MinValue));
            AssertBitwiseEqual(PositiveZero, IncrementOperatorsHelper<NFloat>.op_Increment(NegativeOne));
            AssertBitwiseEqual(PositiveOne, IncrementOperatorsHelper<NFloat>.op_Increment(-MinNormal));
            AssertBitwiseEqual(PositiveOne, IncrementOperatorsHelper<NFloat>.op_Increment(-MaxSubnormal));
            AssertBitwiseEqual(PositiveOne, IncrementOperatorsHelper<NFloat>.op_Increment(-NFloat.Epsilon));
            AssertBitwiseEqual(PositiveOne, IncrementOperatorsHelper<NFloat>.op_Increment(NegativeZero));
            AssertBitwiseEqual(NFloat.NaN, IncrementOperatorsHelper<NFloat>.op_Increment(NFloat.NaN));
            AssertBitwiseEqual(PositiveOne, IncrementOperatorsHelper<NFloat>.op_Increment(PositiveZero));
            AssertBitwiseEqual(PositiveOne, IncrementOperatorsHelper<NFloat>.op_Increment(NFloat.Epsilon));
            AssertBitwiseEqual(PositiveOne, IncrementOperatorsHelper<NFloat>.op_Increment(MaxSubnormal));
            AssertBitwiseEqual(PositiveOne, IncrementOperatorsHelper<NFloat>.op_Increment(MinNormal));
            AssertBitwiseEqual(PositiveTwo, IncrementOperatorsHelper<NFloat>.op_Increment(PositiveOne));
            AssertBitwiseEqual(NFloat.MaxValue, IncrementOperatorsHelper<NFloat>.op_Increment(NFloat.MaxValue));
            AssertBitwiseEqual(NFloat.PositiveInfinity, IncrementOperatorsHelper<NFloat>.op_Increment(NFloat.PositiveInfinity));
        }

        [Fact]
        public static void op_CheckedIncrementTest()
        {
            AssertBitwiseEqual(NFloat.NegativeInfinity, IncrementOperatorsHelper<NFloat>.op_CheckedIncrement(NFloat.NegativeInfinity));
            AssertBitwiseEqual(NFloat.MinValue, IncrementOperatorsHelper<NFloat>.op_CheckedIncrement(NFloat.MinValue));
            AssertBitwiseEqual(PositiveZero, IncrementOperatorsHelper<NFloat>.op_CheckedIncrement(NegativeOne));
            AssertBitwiseEqual(PositiveOne, IncrementOperatorsHelper<NFloat>.op_CheckedIncrement(-MinNormal));
            AssertBitwiseEqual(PositiveOne, IncrementOperatorsHelper<NFloat>.op_CheckedIncrement(-MaxSubnormal));
            AssertBitwiseEqual(PositiveOne, IncrementOperatorsHelper<NFloat>.op_CheckedIncrement(-NFloat.Epsilon));
            AssertBitwiseEqual(PositiveOne, IncrementOperatorsHelper<NFloat>.op_CheckedIncrement(NegativeZero));
            AssertBitwiseEqual(NFloat.NaN, IncrementOperatorsHelper<NFloat>.op_CheckedIncrement(NFloat.NaN));
            AssertBitwiseEqual(PositiveOne, IncrementOperatorsHelper<NFloat>.op_CheckedIncrement(PositiveZero));
            AssertBitwiseEqual(PositiveOne, IncrementOperatorsHelper<NFloat>.op_CheckedIncrement(NFloat.Epsilon));
            AssertBitwiseEqual(PositiveOne, IncrementOperatorsHelper<NFloat>.op_CheckedIncrement(MaxSubnormal));
            AssertBitwiseEqual(PositiveOne, IncrementOperatorsHelper<NFloat>.op_CheckedIncrement(MinNormal));
            AssertBitwiseEqual(PositiveTwo, IncrementOperatorsHelper<NFloat>.op_CheckedIncrement(PositiveOne));
            AssertBitwiseEqual(NFloat.MaxValue, IncrementOperatorsHelper<NFloat>.op_CheckedIncrement(NFloat.MaxValue));
            AssertBitwiseEqual(NFloat.PositiveInfinity, IncrementOperatorsHelper<NFloat>.op_CheckedIncrement(NFloat.PositiveInfinity));
        }

        [Fact]
        public static void op_ModulusTest()
        {
            AssertBitwiseEqual(NFloat.NaN, ModulusOperatorsHelper<NFloat, NFloat, NFloat>.op_Modulus(NFloat.NegativeInfinity, PositiveTwo));
            AssertBitwiseEqual(NegativeZero, ModulusOperatorsHelper<NFloat, NFloat, NFloat>.op_Modulus(NFloat.MinValue, PositiveTwo));
            AssertBitwiseEqual(NegativeOne, ModulusOperatorsHelper<NFloat, NFloat, NFloat>.op_Modulus(NegativeOne, PositiveTwo));
            AssertBitwiseEqual(-MinNormal, ModulusOperatorsHelper<NFloat, NFloat, NFloat>.op_Modulus(-MinNormal, PositiveTwo));
            AssertBitwiseEqual(-MaxSubnormal, ModulusOperatorsHelper<NFloat, NFloat, NFloat>.op_Modulus(-MaxSubnormal, PositiveTwo));
            AssertBitwiseEqual(-NFloat.Epsilon, ModulusOperatorsHelper<NFloat, NFloat, NFloat>.op_Modulus(-NFloat.Epsilon, PositiveTwo)); ;
            AssertBitwiseEqual(NegativeZero, ModulusOperatorsHelper<NFloat, NFloat, NFloat>.op_Modulus(NegativeZero, PositiveTwo));
            AssertBitwiseEqual(NFloat.NaN, ModulusOperatorsHelper<NFloat, NFloat, NFloat>.op_Modulus(NFloat.NaN, PositiveTwo));
            AssertBitwiseEqual(PositiveZero, ModulusOperatorsHelper<NFloat, NFloat, NFloat>.op_Modulus(PositiveZero, PositiveTwo));
            AssertBitwiseEqual(NFloat.Epsilon, ModulusOperatorsHelper<NFloat, NFloat, NFloat>.op_Modulus(NFloat.Epsilon, PositiveTwo));
            AssertBitwiseEqual(MaxSubnormal, ModulusOperatorsHelper<NFloat, NFloat, NFloat>.op_Modulus(MaxSubnormal, PositiveTwo));
            AssertBitwiseEqual(MinNormal, ModulusOperatorsHelper<NFloat, NFloat, NFloat>.op_Modulus(MinNormal, PositiveTwo));
            AssertBitwiseEqual(PositiveOne, ModulusOperatorsHelper<NFloat, NFloat, NFloat>.op_Modulus(PositiveOne, PositiveTwo));
            AssertBitwiseEqual(PositiveZero, ModulusOperatorsHelper<NFloat, NFloat, NFloat>.op_Modulus(NFloat.MaxValue, PositiveTwo));
            AssertBitwiseEqual(NFloat.NaN, ModulusOperatorsHelper<NFloat, NFloat, NFloat>.op_Modulus(NFloat.PositiveInfinity, PositiveTwo));
        }

        [Fact]
        public static void op_MultiplyTest()
        {
            AssertBitwiseEqual(NFloat.NegativeInfinity, MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_Multiply(NFloat.NegativeInfinity, PositiveTwo));
            AssertBitwiseEqual(NFloat.NegativeInfinity, MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_Multiply(NFloat.MinValue, PositiveTwo));
            AssertBitwiseEqual(NegativeTwo, MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_Multiply(NegativeOne, PositiveTwo));
            AssertBitwiseEqual(NegativeZero, MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_Multiply(NegativeZero, PositiveTwo));
            AssertBitwiseEqual(NFloat.NaN, MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_Multiply(NFloat.NaN, PositiveTwo));
            AssertBitwiseEqual(PositiveZero, MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_Multiply(PositiveZero, PositiveTwo));
            AssertBitwiseEqual(PositiveTwo, MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_Multiply(PositiveOne, PositiveTwo));
            AssertBitwiseEqual(NFloat.PositiveInfinity, MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_Multiply(NFloat.MaxValue, PositiveTwo));
            AssertBitwiseEqual(NFloat.PositiveInfinity, MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_Multiply(NFloat.PositiveInfinity, PositiveTwo));

            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual((NFloat)(-4.4501477170144028E-308), MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_Multiply(-MinNormal, PositiveTwo));
                AssertBitwiseEqual((NFloat)(-4.4501477170144018E-308), MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_Multiply(-MaxSubnormal, PositiveTwo));
                AssertBitwiseEqual((NFloat)(-9.8813129168249309E-324), MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_Multiply(-NFloat.Epsilon, PositiveTwo));
                AssertBitwiseEqual((NFloat)9.8813129168249309E-324, MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_Multiply(NFloat.Epsilon, PositiveTwo));
                AssertBitwiseEqual((NFloat)4.4501477170144018E-308, MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_Multiply(MaxSubnormal, PositiveTwo));
                AssertBitwiseEqual((NFloat)4.4501477170144028E-308, MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_Multiply(MinNormal, PositiveTwo));
            }
            else
            {
                AssertBitwiseEqual((NFloat)(-2.3509887E-38f), MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_Multiply(-MinNormal, PositiveTwo));
                AssertBitwiseEqual((NFloat)(-2.35098842E-38f), MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_Multiply(-MaxSubnormal, PositiveTwo));
                AssertBitwiseEqual((NFloat)(-2.80259693E-45f), MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_Multiply(-NFloat.Epsilon, PositiveTwo));
                AssertBitwiseEqual((NFloat)2.80259693E-45f, MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_Multiply(NFloat.Epsilon, PositiveTwo));
                AssertBitwiseEqual((NFloat)2.35098842E-38f, MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_Multiply(MaxSubnormal, PositiveTwo));
                AssertBitwiseEqual((NFloat)2.3509887E-38f, MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_Multiply(MinNormal, PositiveTwo));
            }
        }

        [Fact]
        public static void op_CheckedMultiplyTest()
        {
            AssertBitwiseEqual(NFloat.NegativeInfinity, MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedMultiply(NFloat.NegativeInfinity, PositiveTwo));
            AssertBitwiseEqual(NFloat.NegativeInfinity, MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedMultiply(NFloat.MinValue, PositiveTwo));
            AssertBitwiseEqual(NegativeTwo, MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedMultiply(NegativeOne, PositiveTwo));
            AssertBitwiseEqual(NegativeZero, MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedMultiply(NegativeZero, PositiveTwo));
            AssertBitwiseEqual(NFloat.NaN, MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedMultiply(NFloat.NaN, PositiveTwo));
            AssertBitwiseEqual(PositiveZero, MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedMultiply(PositiveZero, PositiveTwo));
            AssertBitwiseEqual(PositiveTwo, MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedMultiply(PositiveOne, PositiveTwo));
            AssertBitwiseEqual(NFloat.PositiveInfinity, MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedMultiply(NFloat.MaxValue, PositiveTwo));
            AssertBitwiseEqual(NFloat.PositiveInfinity, MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedMultiply(NFloat.PositiveInfinity, PositiveTwo));

            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual((NFloat)(-4.4501477170144028E-308), MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedMultiply(-MinNormal, PositiveTwo));
                AssertBitwiseEqual((NFloat)(-4.4501477170144018E-308), MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedMultiply(-MaxSubnormal, PositiveTwo));
                AssertBitwiseEqual((NFloat)(-9.8813129168249309E-324), MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedMultiply(-NFloat.Epsilon, PositiveTwo));
                AssertBitwiseEqual((NFloat)9.8813129168249309E-324, MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedMultiply(NFloat.Epsilon, PositiveTwo));
                AssertBitwiseEqual((NFloat)4.4501477170144018E-308, MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedMultiply(MaxSubnormal, PositiveTwo));
                AssertBitwiseEqual((NFloat)4.4501477170144028E-308, MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedMultiply(MinNormal, PositiveTwo));
            }
            else
            {
                AssertBitwiseEqual((NFloat)(-2.3509887E-38f), MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedMultiply(-MinNormal, PositiveTwo));
                AssertBitwiseEqual((NFloat)(-2.35098842E-38f), MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedMultiply(-MaxSubnormal, PositiveTwo));
                AssertBitwiseEqual((NFloat)(-2.80259693E-45f), MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedMultiply(-NFloat.Epsilon, PositiveTwo));
                AssertBitwiseEqual((NFloat)2.80259693E-45f, MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedMultiply(NFloat.Epsilon, PositiveTwo));
                AssertBitwiseEqual((NFloat)2.35098842E-38f, MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedMultiply(MaxSubnormal, PositiveTwo));
                AssertBitwiseEqual((NFloat)2.3509887E-38f, MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedMultiply(MinNormal, PositiveTwo));
            }
        }

        [Fact]
        public static void AbsTest()
        {
            AssertBitwiseEqual(NFloat.PositiveInfinity, NumberHelper<NFloat>.Abs(NFloat.NegativeInfinity));
            AssertBitwiseEqual(NFloat.MaxValue, NumberHelper<NFloat>.Abs(NFloat.MinValue));
            AssertBitwiseEqual(PositiveOne, NumberHelper<NFloat>.Abs(NegativeOne));
            AssertBitwiseEqual(MinNormal, NumberHelper<NFloat>.Abs(-MinNormal));
            AssertBitwiseEqual(MaxSubnormal, NumberHelper<NFloat>.Abs(-MaxSubnormal));
            AssertBitwiseEqual(NFloat.Epsilon, NumberHelper<NFloat>.Abs(-NFloat.Epsilon));
            AssertBitwiseEqual(PositiveZero, NumberHelper<NFloat>.Abs(NegativeZero));
            AssertBitwiseEqual(NFloat.NaN, NumberHelper<NFloat>.Abs(NFloat.NaN));
            AssertBitwiseEqual(PositiveZero, NumberHelper<NFloat>.Abs(PositiveZero));
            AssertBitwiseEqual(NFloat.Epsilon, NumberHelper<NFloat>.Abs(NFloat.Epsilon));
            AssertBitwiseEqual(MaxSubnormal, NumberHelper<NFloat>.Abs(MaxSubnormal));
            AssertBitwiseEqual(MinNormal, NumberHelper<NFloat>.Abs(MinNormal));
            AssertBitwiseEqual(PositiveOne, NumberHelper<NFloat>.Abs(PositiveOne));
            AssertBitwiseEqual(NFloat.MaxValue, NumberHelper<NFloat>.Abs(NFloat.MaxValue));
            AssertBitwiseEqual(NFloat.PositiveInfinity, NumberHelper<NFloat>.Abs(NFloat.PositiveInfinity));
        }

        [Fact]
        public static void ClampTest()
        {
            AssertBitwiseEqual(PositiveOne, NumberHelper<NFloat>.Clamp(NFloat.NegativeInfinity, PositiveOne, (NFloat)63.0f));
            AssertBitwiseEqual(PositiveOne, NumberHelper<NFloat>.Clamp(NFloat.MinValue, PositiveOne, (NFloat)63.0f));
            AssertBitwiseEqual(PositiveOne, NumberHelper<NFloat>.Clamp(NegativeOne, PositiveOne, (NFloat)63.0f));
            AssertBitwiseEqual(PositiveOne, NumberHelper<NFloat>.Clamp(-MinNormal, PositiveOne, (NFloat)63.0f));
            AssertBitwiseEqual(PositiveOne, NumberHelper<NFloat>.Clamp(-MaxSubnormal, PositiveOne, (NFloat)63.0f));
            AssertBitwiseEqual(PositiveOne, NumberHelper<NFloat>.Clamp(-NFloat.Epsilon, PositiveOne, (NFloat)63.0f));
            AssertBitwiseEqual(PositiveOne, NumberHelper<NFloat>.Clamp(NegativeZero, PositiveOne, (NFloat)63.0f));
            AssertBitwiseEqual(NFloat.NaN, NumberHelper<NFloat>.Clamp(NFloat.NaN, PositiveOne, (NFloat)63.0f));
            AssertBitwiseEqual(PositiveOne, NumberHelper<NFloat>.Clamp(PositiveZero, PositiveOne, (NFloat)63.0f));
            AssertBitwiseEqual(PositiveOne, NumberHelper<NFloat>.Clamp(NFloat.Epsilon, PositiveOne, (NFloat)63.0f));
            AssertBitwiseEqual(PositiveOne, NumberHelper<NFloat>.Clamp(MaxSubnormal, PositiveOne, (NFloat)63.0f));
            AssertBitwiseEqual(PositiveOne, NumberHelper<NFloat>.Clamp(MinNormal, PositiveOne, (NFloat)63.0f));
            AssertBitwiseEqual(PositiveOne, NumberHelper<NFloat>.Clamp(PositiveOne, PositiveOne, (NFloat)63.0f));
            AssertBitwiseEqual((NFloat)63.0f, NumberHelper<NFloat>.Clamp(NFloat.MaxValue, PositiveOne, (NFloat)63.0f));
            AssertBitwiseEqual((NFloat)63.0f, NumberHelper<NFloat>.Clamp(NFloat.PositiveInfinity, PositiveOne, (NFloat)63.0f));
        }

        [Fact]
        public static void CreateCheckedFromByteTest()
        {
            AssertBitwiseEqual(PositiveZero, NumberHelper<NFloat>.CreateChecked<byte>(0x00));
            AssertBitwiseEqual(PositiveOne, NumberHelper<NFloat>.CreateChecked<byte>(0x01));
            AssertBitwiseEqual((NFloat)127.0f, NumberHelper<NFloat>.CreateChecked<byte>(0x7F));
            AssertBitwiseEqual((NFloat)128.0f, NumberHelper<NFloat>.CreateChecked<byte>(0x80));
            AssertBitwiseEqual((NFloat)255.0f, NumberHelper<NFloat>.CreateChecked<byte>(0xFF));
        }

        [Fact]
        public static void CreateCheckedFromCharTest()
        {
            AssertBitwiseEqual(PositiveZero, NumberHelper<NFloat>.CreateChecked<char>((char)0x0000));
            AssertBitwiseEqual(PositiveOne, NumberHelper<NFloat>.CreateChecked<char>((char)0x0001));
            AssertBitwiseEqual((NFloat)32767.0f, NumberHelper<NFloat>.CreateChecked<char>((char)0x7FFF));
            AssertBitwiseEqual((NFloat)32768.0f, NumberHelper<NFloat>.CreateChecked<char>((char)0x8000));
            AssertBitwiseEqual((NFloat)65535.0f, NumberHelper<NFloat>.CreateChecked<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateCheckedFromInt16Test()
        {
            AssertBitwiseEqual(PositiveZero, NumberHelper<NFloat>.CreateChecked<short>(0x0000));
            AssertBitwiseEqual(PositiveOne, NumberHelper<NFloat>.CreateChecked<short>(0x0001));
            AssertBitwiseEqual((NFloat)32767.0f, NumberHelper<NFloat>.CreateChecked<short>(0x7FFF));
            AssertBitwiseEqual((NFloat)(-32768.0f), NumberHelper<NFloat>.CreateChecked<short>(unchecked((short)0x8000)));
            AssertBitwiseEqual(NegativeOne, NumberHelper<NFloat>.CreateChecked<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromInt32Test()
        {
            AssertBitwiseEqual(PositiveZero, NumberHelper<NFloat>.CreateChecked<int>(0x00000000));
            AssertBitwiseEqual(PositiveOne, NumberHelper<NFloat>.CreateChecked<int>(0x00000001));
            AssertBitwiseEqual(NegativeOne, NumberHelper<NFloat>.CreateChecked<int>(unchecked((int)0xFFFFFFFF)));

            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual((NFloat)2147483647.0, NumberHelper<NFloat>.CreateChecked<int>(0x7FFFFFFF));
                AssertBitwiseEqual((NFloat)(-2147483648.0), NumberHelper<NFloat>.CreateChecked<int>(unchecked((int)0x80000000)));
            }
            else
            {
                AssertBitwiseEqual((NFloat)2147483647.0f, NumberHelper<NFloat>.CreateChecked<int>(0x7FFFFFFF));
                AssertBitwiseEqual((NFloat)(-2147483648.0f), NumberHelper<NFloat>.CreateChecked<int>(unchecked((int)0x80000000)));
            }
        }

        [Fact]
        public static void CreateCheckedFromInt64Test()
        {
            AssertBitwiseEqual(PositiveZero, NumberHelper<NFloat>.CreateChecked<long>(0x0000000000000000));
            AssertBitwiseEqual(PositiveOne, NumberHelper<NFloat>.CreateChecked<long>(0x0000000000000001));
            AssertBitwiseEqual(NegativeOne, NumberHelper<NFloat>.CreateChecked<long>(unchecked(unchecked((long)0xFFFFFFFFFFFFFFFF))));

            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual((NFloat)9223372036854775807.0, NumberHelper<NFloat>.CreateChecked<long>(0x7FFFFFFFFFFFFFFF));
                AssertBitwiseEqual((NFloat)(-9223372036854775808.0), NumberHelper<NFloat>.CreateChecked<long>(unchecked(unchecked((long)0x8000000000000000))));
            }
            else
            {
                AssertBitwiseEqual((NFloat)9223372036854775807.0f, NumberHelper<NFloat>.CreateChecked<long>(0x7FFFFFFFFFFFFFFF));
                AssertBitwiseEqual((NFloat)(-9223372036854775808.0f), NumberHelper<NFloat>.CreateChecked<long>(unchecked(unchecked((long)0x8000000000000000))));
            }
        }

        [Fact]
        public static void CreateCheckedFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual(PositiveZero, NumberHelper<NFloat>.CreateChecked<nint>(unchecked((nint)0x0000000000000000)));
                AssertBitwiseEqual(PositiveOne, NumberHelper<NFloat>.CreateChecked<nint>(unchecked((nint)0x0000000000000001)));
                AssertBitwiseEqual((NFloat)9223372036854775807.0, NumberHelper<NFloat>.CreateChecked<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                AssertBitwiseEqual((NFloat)(-9223372036854775808.0), NumberHelper<NFloat>.CreateChecked<nint>(unchecked((nint)0x8000000000000000)));
                AssertBitwiseEqual(NegativeOne, NumberHelper<NFloat>.CreateChecked<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                AssertBitwiseEqual(PositiveZero, NumberHelper<NFloat>.CreateChecked<nint>((nint)0x00000000));
                AssertBitwiseEqual(PositiveOne, NumberHelper<NFloat>.CreateChecked<nint>((nint)0x00000001));
                AssertBitwiseEqual((NFloat)2147483647.0f, NumberHelper<NFloat>.CreateChecked<nint>((nint)0x7FFFFFFF));
                AssertBitwiseEqual((NFloat)(-2147483648.0f), NumberHelper<NFloat>.CreateChecked<nint>(unchecked((nint)0x80000000)));
                AssertBitwiseEqual(NegativeOne, NumberHelper<NFloat>.CreateChecked<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateCheckedFromSByteTest()
        {
            AssertBitwiseEqual(PositiveZero, NumberHelper<NFloat>.CreateChecked<sbyte>(0x00));
            AssertBitwiseEqual(PositiveOne, NumberHelper<NFloat>.CreateChecked<sbyte>(0x01));
            AssertBitwiseEqual((NFloat)127.0f, NumberHelper<NFloat>.CreateChecked<sbyte>(0x7F));
            AssertBitwiseEqual((NFloat)(-128.0f), NumberHelper<NFloat>.CreateChecked<sbyte>(unchecked((sbyte)0x80)));
            AssertBitwiseEqual(NegativeOne, NumberHelper<NFloat>.CreateChecked<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateCheckedFromUInt16Test()
        {
            AssertBitwiseEqual(PositiveZero, NumberHelper<NFloat>.CreateChecked<ushort>(0x0000));
            AssertBitwiseEqual(PositiveOne, NumberHelper<NFloat>.CreateChecked<ushort>(0x0001));
            AssertBitwiseEqual((NFloat)32767.0f, NumberHelper<NFloat>.CreateChecked<ushort>(0x7FFF));
            AssertBitwiseEqual((NFloat)32768.0f, NumberHelper<NFloat>.CreateChecked<ushort>(0x8000));
            AssertBitwiseEqual((NFloat)65535.0f, NumberHelper<NFloat>.CreateChecked<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUInt32Test()
        {
            AssertBitwiseEqual(PositiveZero, NumberHelper<NFloat>.CreateChecked<uint>(0x00000000));
            AssertBitwiseEqual(PositiveOne, NumberHelper<NFloat>.CreateChecked<uint>(0x00000001));

            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual((NFloat)2147483647.0, NumberHelper<NFloat>.CreateChecked<uint>(0x7FFFFFFF));
                AssertBitwiseEqual((NFloat)2147483648.0, NumberHelper<NFloat>.CreateChecked<uint>(0x80000000));
                AssertBitwiseEqual((NFloat)4294967295.0, NumberHelper<NFloat>.CreateChecked<uint>(0xFFFFFFFF));
            }
            else
            {
                AssertBitwiseEqual((NFloat)2147483647.0f, NumberHelper<NFloat>.CreateChecked<uint>(0x7FFFFFFF));
                AssertBitwiseEqual((NFloat)2147483648.0f, NumberHelper<NFloat>.CreateChecked<uint>(0x80000000));
                AssertBitwiseEqual((NFloat)4294967295.0f, NumberHelper<NFloat>.CreateChecked<uint>(0xFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateCheckedFromUInt64Test()
        {
            AssertBitwiseEqual(PositiveZero, NumberHelper<NFloat>.CreateChecked<ulong>(0x0000000000000000));
            AssertBitwiseEqual(PositiveOne, NumberHelper<NFloat>.CreateChecked<ulong>(0x0000000000000001));

            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual((NFloat)9223372036854775807.0, NumberHelper<NFloat>.CreateChecked<ulong>(0x7FFFFFFFFFFFFFFF));
                AssertBitwiseEqual((NFloat)9223372036854775808.0, NumberHelper<NFloat>.CreateChecked<ulong>(0x8000000000000000));
                AssertBitwiseEqual((NFloat)18446744073709551615.0, NumberHelper<NFloat>.CreateChecked<ulong>(0xFFFFFFFFFFFFFFFF));
            }
            else
            {
                AssertBitwiseEqual((NFloat)9223372036854775807.0f, NumberHelper<NFloat>.CreateChecked<ulong>(0x7FFFFFFFFFFFFFFF));
                AssertBitwiseEqual((NFloat)9223372036854775808.0f, NumberHelper<NFloat>.CreateChecked<ulong>(0x8000000000000000));
                AssertBitwiseEqual((NFloat)18446744073709551615.0f, NumberHelper<NFloat>.CreateChecked<ulong>(0xFFFFFFFFFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateCheckedFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual(PositiveZero, NumberHelper<NFloat>.CreateChecked<nuint>(unchecked((nuint)0x0000000000000000)));
                AssertBitwiseEqual(PositiveOne, NumberHelper<NFloat>.CreateChecked<nuint>(unchecked((nuint)0x0000000000000001)));
                AssertBitwiseEqual((NFloat)9223372036854775807.0, NumberHelper<NFloat>.CreateChecked<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));

                // https://github.com/dotnet/roslyn/issues/60714
                // AssertBitwiseEqual((NFloat)9223372036854775808.0, NumberHelper<NFloat>.CreateChecked<nuint>(unchecked((nuint)0x8000000000000000)));
                // AssertBitwiseEqual((NFloat)18446744073709551615.0,NumberHelper<NFloat>.CreateChecked<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                AssertBitwiseEqual(PositiveZero, NumberHelper<NFloat>.CreateChecked<nuint>((nuint)0x00000000));
                AssertBitwiseEqual(PositiveOne, NumberHelper<NFloat>.CreateChecked<nuint>((nuint)0x00000001));
                AssertBitwiseEqual((NFloat)2147483647.0f, NumberHelper<NFloat>.CreateChecked<nuint>((nuint)0x7FFFFFFF));

                // https://github.com/dotnet/roslyn/issues/60714
                // AssertBitwiseEqual((NFloat)2147483648.0f, NumberHelper<NFloat>.CreateChecked<nuint>((nuint)0x80000000));
                // AssertBitwiseEqual((NFloat)4294967295.0f, NumberHelper<NFloat>.CreateChecked<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateSaturatingFromByteTest()
        {
            AssertBitwiseEqual(PositiveZero, NumberHelper<NFloat>.CreateSaturating<byte>(0x00));
            AssertBitwiseEqual(PositiveOne, NumberHelper<NFloat>.CreateSaturating<byte>(0x01));
            AssertBitwiseEqual((NFloat)127.0f, NumberHelper<NFloat>.CreateSaturating<byte>(0x7F));
            AssertBitwiseEqual((NFloat)128.0f, NumberHelper<NFloat>.CreateSaturating<byte>(0x80));
            AssertBitwiseEqual((NFloat)255.0f, NumberHelper<NFloat>.CreateSaturating<byte>(0xFF));
        }

        [Fact]
        public static void CreateSaturatingFromCharTest()
        {
            AssertBitwiseEqual(PositiveZero, NumberHelper<NFloat>.CreateSaturating<char>((char)0x0000));
            AssertBitwiseEqual(PositiveOne, NumberHelper<NFloat>.CreateSaturating<char>((char)0x0001));
            AssertBitwiseEqual((NFloat)32767.0f, NumberHelper<NFloat>.CreateSaturating<char>((char)0x7FFF));
            AssertBitwiseEqual((NFloat)32768.0f, NumberHelper<NFloat>.CreateSaturating<char>((char)0x8000));
            AssertBitwiseEqual((NFloat)65535.0f, NumberHelper<NFloat>.CreateSaturating<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromInt16Test()
        {
            AssertBitwiseEqual(PositiveZero, NumberHelper<NFloat>.CreateSaturating<short>(0x0000));
            AssertBitwiseEqual(PositiveOne, NumberHelper<NFloat>.CreateSaturating<short>(0x0001));
            AssertBitwiseEqual((NFloat)32767.0f, NumberHelper<NFloat>.CreateSaturating<short>(0x7FFF));
            AssertBitwiseEqual((NFloat)(-32768.0f), NumberHelper<NFloat>.CreateSaturating<short>(unchecked((short)0x8000)));
            AssertBitwiseEqual(NegativeOne, NumberHelper<NFloat>.CreateSaturating<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromInt32Test()
        {
            AssertBitwiseEqual(PositiveZero, NumberHelper<NFloat>.CreateSaturating<int>(0x00000000));
            AssertBitwiseEqual(PositiveOne, NumberHelper<NFloat>.CreateSaturating<int>(0x00000001));
            AssertBitwiseEqual(NegativeOne, NumberHelper<NFloat>.CreateSaturating<int>(unchecked((int)0xFFFFFFFF)));

            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual((NFloat)2147483647.0, NumberHelper<NFloat>.CreateSaturating<int>(0x7FFFFFFF));
                AssertBitwiseEqual((NFloat)(-2147483648.0), NumberHelper<NFloat>.CreateSaturating<int>(unchecked((int)0x80000000)));
            }
            else
            {
                AssertBitwiseEqual((NFloat)2147483647.0f, NumberHelper<NFloat>.CreateSaturating<int>(0x7FFFFFFF));
                AssertBitwiseEqual((NFloat)(-2147483648.0f), NumberHelper<NFloat>.CreateSaturating<int>(unchecked((int)0x80000000)));
            }
        }

        [Fact]
        public static void CreateSaturatingFromInt64Test()
        {
            AssertBitwiseEqual(PositiveZero, NumberHelper<NFloat>.CreateSaturating<long>(0x0000000000000000));
            AssertBitwiseEqual(PositiveOne, NumberHelper<NFloat>.CreateSaturating<long>(0x0000000000000001));
            AssertBitwiseEqual(NegativeOne, NumberHelper<NFloat>.CreateSaturating<long>(unchecked(unchecked((long)0xFFFFFFFFFFFFFFFF))));

            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual((NFloat)9223372036854775807.0, NumberHelper<NFloat>.CreateSaturating<long>(0x7FFFFFFFFFFFFFFF));
                AssertBitwiseEqual((NFloat)(-9223372036854775808.0), NumberHelper<NFloat>.CreateSaturating<long>(unchecked(unchecked((long)0x8000000000000000))));
            }
            else
            {
                AssertBitwiseEqual((NFloat)9223372036854775807.0f, NumberHelper<NFloat>.CreateSaturating<long>(0x7FFFFFFFFFFFFFFF));
                AssertBitwiseEqual((NFloat)(-9223372036854775808.0f), NumberHelper<NFloat>.CreateSaturating<long>(unchecked(unchecked((long)0x8000000000000000))));
            }
        }

        [Fact]
        public static void CreateSaturatingFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual(PositiveZero, NumberHelper<NFloat>.CreateSaturating<nint>(unchecked((nint)0x0000000000000000)));
                AssertBitwiseEqual(PositiveOne, NumberHelper<NFloat>.CreateSaturating<nint>(unchecked((nint)0x0000000000000001)));
                AssertBitwiseEqual((NFloat)9223372036854775807.0, NumberHelper<NFloat>.CreateSaturating<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                AssertBitwiseEqual((NFloat)(-9223372036854775808.0), NumberHelper<NFloat>.CreateSaturating<nint>(unchecked((nint)0x8000000000000000)));
                AssertBitwiseEqual(NegativeOne, NumberHelper<NFloat>.CreateSaturating<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                AssertBitwiseEqual(PositiveZero, NumberHelper<NFloat>.CreateSaturating<nint>((nint)0x00000000));
                AssertBitwiseEqual(PositiveOne, NumberHelper<NFloat>.CreateSaturating<nint>((nint)0x00000001));
                AssertBitwiseEqual((NFloat)2147483647.0f, NumberHelper<NFloat>.CreateSaturating<nint>((nint)0x7FFFFFFF));
                AssertBitwiseEqual((NFloat)(-2147483648.0f), NumberHelper<NFloat>.CreateSaturating<nint>(unchecked((nint)0x80000000)));
                AssertBitwiseEqual(NegativeOne, NumberHelper<NFloat>.CreateSaturating<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateSaturatingFromSByteTest()
        {
            AssertBitwiseEqual(PositiveZero, NumberHelper<NFloat>.CreateSaturating<sbyte>(0x00));
            AssertBitwiseEqual(PositiveOne, NumberHelper<NFloat>.CreateSaturating<sbyte>(0x01));
            AssertBitwiseEqual((NFloat)127.0f, NumberHelper<NFloat>.CreateSaturating<sbyte>(0x7F));
            AssertBitwiseEqual((NFloat)(-128.0f), NumberHelper<NFloat>.CreateSaturating<sbyte>(unchecked((sbyte)0x80)));
            AssertBitwiseEqual(NegativeOne, NumberHelper<NFloat>.CreateSaturating<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateSaturatingFromUInt16Test()
        {
            AssertBitwiseEqual(PositiveZero, NumberHelper<NFloat>.CreateSaturating<ushort>(0x0000));
            AssertBitwiseEqual(PositiveOne, NumberHelper<NFloat>.CreateSaturating<ushort>(0x0001));
            AssertBitwiseEqual((NFloat)32767.0f, NumberHelper<NFloat>.CreateSaturating<ushort>(0x7FFF));
            AssertBitwiseEqual((NFloat)32768.0f, NumberHelper<NFloat>.CreateSaturating<ushort>(0x8000));
            AssertBitwiseEqual((NFloat)65535.0f, NumberHelper<NFloat>.CreateSaturating<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUInt32Test()
        {
            AssertBitwiseEqual(PositiveZero, NumberHelper<NFloat>.CreateSaturating<uint>(0x00000000));
            AssertBitwiseEqual(PositiveOne, NumberHelper<NFloat>.CreateSaturating<uint>(0x00000001));

            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual((NFloat)2147483647.0, NumberHelper<NFloat>.CreateSaturating<uint>(0x7FFFFFFF));
                AssertBitwiseEqual((NFloat)2147483648.0, NumberHelper<NFloat>.CreateSaturating<uint>(0x80000000));
                AssertBitwiseEqual((NFloat)4294967295.0, NumberHelper<NFloat>.CreateSaturating<uint>(0xFFFFFFFF));
            }
            else
            {
                AssertBitwiseEqual((NFloat)2147483647.0f, NumberHelper<NFloat>.CreateSaturating<uint>(0x7FFFFFFF));
                AssertBitwiseEqual((NFloat)2147483648.0f, NumberHelper<NFloat>.CreateSaturating<uint>(0x80000000));
                AssertBitwiseEqual((NFloat)4294967295.0f, NumberHelper<NFloat>.CreateSaturating<uint>(0xFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateSaturatingFromUInt64Test()
        {
            AssertBitwiseEqual(PositiveZero, NumberHelper<NFloat>.CreateSaturating<ulong>(0x0000000000000000));
            AssertBitwiseEqual(PositiveOne, NumberHelper<NFloat>.CreateSaturating<ulong>(0x0000000000000001));

            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual((NFloat)9223372036854775807.0, NumberHelper<NFloat>.CreateSaturating<ulong>(0x7FFFFFFFFFFFFFFF));
                AssertBitwiseEqual((NFloat)9223372036854775808.0, NumberHelper<NFloat>.CreateSaturating<ulong>(0x8000000000000000));
                AssertBitwiseEqual((NFloat)18446744073709551615.0, NumberHelper<NFloat>.CreateSaturating<ulong>(0xFFFFFFFFFFFFFFFF));
            }
            else
            {
                AssertBitwiseEqual((NFloat)9223372036854775807.0f, NumberHelper<NFloat>.CreateSaturating<ulong>(0x7FFFFFFFFFFFFFFF));
                AssertBitwiseEqual((NFloat)9223372036854775808.0f, NumberHelper<NFloat>.CreateSaturating<ulong>(0x8000000000000000));
                AssertBitwiseEqual((NFloat)18446744073709551615.0f, NumberHelper<NFloat>.CreateSaturating<ulong>(0xFFFFFFFFFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateSaturatingFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual(PositiveZero, NumberHelper<NFloat>.CreateSaturating<nuint>(unchecked((nuint)0x0000000000000000)));
                AssertBitwiseEqual(PositiveOne, NumberHelper<NFloat>.CreateSaturating<nuint>(unchecked((nuint)0x0000000000000001)));
                AssertBitwiseEqual((NFloat)9223372036854775807.0, NumberHelper<NFloat>.CreateSaturating<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));

                // https://github.com/dotnet/roslyn/issues/60714
                // AssertBitwiseEqual((NFloat)9223372036854775808.0, NumberHelper<NFloat>.CreateSaturating<nuint>(unchecked((nuint)0x8000000000000000)));
                // AssertBitwiseEqual((NFloat)18446744073709551615.0, NumberHelper<NFloat>.CreateSaturating<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                AssertBitwiseEqual(PositiveZero, NumberHelper<NFloat>.CreateSaturating<nuint>((nuint)0x00000000));
                AssertBitwiseEqual(PositiveOne, NumberHelper<NFloat>.CreateSaturating<nuint>((nuint)0x00000001));
                AssertBitwiseEqual((NFloat)2147483647.0f, NumberHelper<NFloat>.CreateSaturating<nuint>((nuint)0x7FFFFFFF));

                // https://github.com/dotnet/roslyn/issues/60714
                // AssertBitwiseEqual((NFloat)2147483648.0f, NumberHelper<NFloat>.CreateSaturating<nuint>((nuint)0x80000000));
                // AssertBitwiseEqual((NFloat)4294967295.0f, NumberHelper<NFloat>.CreateSaturating<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateTruncatingFromByteTest()
        {
            AssertBitwiseEqual(PositiveZero, NumberHelper<NFloat>.CreateTruncating<byte>(0x00));
            AssertBitwiseEqual(PositiveOne, NumberHelper<NFloat>.CreateTruncating<byte>(0x01));
            AssertBitwiseEqual((NFloat)127.0f, NumberHelper<NFloat>.CreateTruncating<byte>(0x7F));
            AssertBitwiseEqual((NFloat)128.0f, NumberHelper<NFloat>.CreateTruncating<byte>(0x80));
            AssertBitwiseEqual((NFloat)255.0f, NumberHelper<NFloat>.CreateTruncating<byte>(0xFF));
        }

        [Fact]
        public static void CreateTruncatingFromCharTest()
        {
            AssertBitwiseEqual(PositiveZero, NumberHelper<NFloat>.CreateTruncating<char>((char)0x0000));
            AssertBitwiseEqual(PositiveOne, NumberHelper<NFloat>.CreateTruncating<char>((char)0x0001));
            AssertBitwiseEqual((NFloat)32767.0f, NumberHelper<NFloat>.CreateTruncating<char>((char)0x7FFF));
            AssertBitwiseEqual((NFloat)32768.0f, NumberHelper<NFloat>.CreateTruncating<char>((char)0x8000));
            AssertBitwiseEqual((NFloat)65535.0f, NumberHelper<NFloat>.CreateTruncating<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromInt16Test()
        {
            AssertBitwiseEqual(PositiveZero, NumberHelper<NFloat>.CreateTruncating<short>(0x0000));
            AssertBitwiseEqual(PositiveOne, NumberHelper<NFloat>.CreateTruncating<short>(0x0001));
            AssertBitwiseEqual((NFloat)32767.0f, NumberHelper<NFloat>.CreateTruncating<short>(0x7FFF));
            AssertBitwiseEqual((NFloat)(-32768.0f), NumberHelper<NFloat>.CreateTruncating<short>(unchecked((short)0x8000)));
            AssertBitwiseEqual(NegativeOne, NumberHelper<NFloat>.CreateTruncating<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateTruncatingFromInt32Test()
        {
            AssertBitwiseEqual(PositiveZero, NumberHelper<NFloat>.CreateTruncating<int>(0x00000000));
            AssertBitwiseEqual(PositiveOne, NumberHelper<NFloat>.CreateTruncating<int>(0x00000001));
            AssertBitwiseEqual(NegativeOne, NumberHelper<NFloat>.CreateTruncating<int>(unchecked((int)0xFFFFFFFF)));

            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual((NFloat)2147483647.0, NumberHelper<NFloat>.CreateTruncating<int>(0x7FFFFFFF));
                AssertBitwiseEqual((NFloat)(-2147483648.0), NumberHelper<NFloat>.CreateTruncating<int>(unchecked((int)0x80000000)));
            }
            else
            {
                AssertBitwiseEqual((NFloat)2147483647.0f, NumberHelper<NFloat>.CreateTruncating<int>(0x7FFFFFFF));
                AssertBitwiseEqual((NFloat)(-2147483648.0f), NumberHelper<NFloat>.CreateTruncating<int>(unchecked((int)0x80000000)));
            }
        }

        [Fact]
        public static void CreateTruncatingFromInt64Test()
        {
            AssertBitwiseEqual(PositiveZero, NumberHelper<NFloat>.CreateTruncating<long>(0x0000000000000000));
            AssertBitwiseEqual(PositiveOne, NumberHelper<NFloat>.CreateTruncating<long>(0x0000000000000001));
            AssertBitwiseEqual(NegativeOne, NumberHelper<NFloat>.CreateTruncating<long>(unchecked(unchecked((long)0xFFFFFFFFFFFFFFFF))));

            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual((NFloat)9223372036854775807.0, NumberHelper<NFloat>.CreateTruncating<long>(0x7FFFFFFFFFFFFFFF));
                AssertBitwiseEqual((NFloat)(-9223372036854775808.0), NumberHelper<NFloat>.CreateTruncating<long>(unchecked(unchecked((long)0x8000000000000000))));
            }
            else
            {
                AssertBitwiseEqual((NFloat)9223372036854775807.0f, NumberHelper<NFloat>.CreateTruncating<long>(0x7FFFFFFFFFFFFFFF));
                AssertBitwiseEqual((NFloat)(-9223372036854775808.0f), NumberHelper<NFloat>.CreateTruncating<long>(unchecked(unchecked((long)0x8000000000000000))));
            }
        }

        [Fact]
        public static void CreateTruncatingFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual(PositiveZero, NumberHelper<NFloat>.CreateTruncating<nint>(unchecked((nint)0x0000000000000000)));
                AssertBitwiseEqual(PositiveOne, NumberHelper<NFloat>.CreateTruncating<nint>(unchecked((nint)0x0000000000000001)));
                AssertBitwiseEqual((NFloat)9223372036854775807.0, NumberHelper<NFloat>.CreateTruncating<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                AssertBitwiseEqual((NFloat)(-9223372036854775808.0), NumberHelper<NFloat>.CreateTruncating<nint>(unchecked((nint)0x8000000000000000)));
                AssertBitwiseEqual(NegativeOne, NumberHelper<NFloat>.CreateTruncating<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                AssertBitwiseEqual(PositiveZero, NumberHelper<NFloat>.CreateTruncating<nint>((nint)0x00000000));
                AssertBitwiseEqual(PositiveOne, NumberHelper<NFloat>.CreateTruncating<nint>((nint)0x00000001));
                AssertBitwiseEqual((NFloat)2147483647.0f, NumberHelper<NFloat>.CreateTruncating<nint>((nint)0x7FFFFFFF));
                AssertBitwiseEqual((NFloat)(-2147483648.0f), NumberHelper<NFloat>.CreateTruncating<nint>(unchecked((nint)0x80000000)));
                AssertBitwiseEqual(NegativeOne, NumberHelper<NFloat>.CreateTruncating<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateTruncatingFromSByteTest()
        {
            AssertBitwiseEqual(PositiveZero, NumberHelper<NFloat>.CreateTruncating<sbyte>(0x00));
            AssertBitwiseEqual(PositiveOne, NumberHelper<NFloat>.CreateTruncating<sbyte>(0x01));
            AssertBitwiseEqual((NFloat)127.0f, NumberHelper<NFloat>.CreateTruncating<sbyte>(0x7F));
            AssertBitwiseEqual((NFloat)(-128.0f), NumberHelper<NFloat>.CreateTruncating<sbyte>(unchecked((sbyte)0x80)));
            AssertBitwiseEqual(NegativeOne, NumberHelper<NFloat>.CreateTruncating<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateTruncatingFromUInt16Test()
        {
            AssertBitwiseEqual(PositiveZero, NumberHelper<NFloat>.CreateTruncating<ushort>(0x0000));
            AssertBitwiseEqual(PositiveOne, NumberHelper<NFloat>.CreateTruncating<ushort>(0x0001));
            AssertBitwiseEqual((NFloat)32767.0f, NumberHelper<NFloat>.CreateTruncating<ushort>(0x7FFF));
            AssertBitwiseEqual((NFloat)32768.0f, NumberHelper<NFloat>.CreateTruncating<ushort>(0x8000));
            AssertBitwiseEqual((NFloat)65535.0f, NumberHelper<NFloat>.CreateTruncating<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt32Test()
        {
            AssertBitwiseEqual(PositiveZero, NumberHelper<NFloat>.CreateTruncating<uint>(0x00000000));
            AssertBitwiseEqual(PositiveOne, NumberHelper<NFloat>.CreateTruncating<uint>(0x00000001));

            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual((NFloat)2147483647.0, NumberHelper<NFloat>.CreateTruncating<uint>(0x7FFFFFFF));
                AssertBitwiseEqual((NFloat)2147483648.0, NumberHelper<NFloat>.CreateTruncating<uint>(0x80000000));
                AssertBitwiseEqual((NFloat)4294967295.0, NumberHelper<NFloat>.CreateTruncating<uint>(0xFFFFFFFF));
            }
            else
            {
                AssertBitwiseEqual((NFloat)2147483647.0f, NumberHelper<NFloat>.CreateTruncating<uint>(0x7FFFFFFF));
                AssertBitwiseEqual((NFloat)2147483648.0f, NumberHelper<NFloat>.CreateTruncating<uint>(0x80000000));
                AssertBitwiseEqual((NFloat)4294967295.0f, NumberHelper<NFloat>.CreateTruncating<uint>(0xFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateTruncatingFromUInt64Test()
        {
            AssertBitwiseEqual(PositiveZero, NumberHelper<NFloat>.CreateTruncating<ulong>(0x0000000000000000));
            AssertBitwiseEqual(PositiveOne, NumberHelper<NFloat>.CreateTruncating<ulong>(0x0000000000000001));

            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual((NFloat)9223372036854775807.0, NumberHelper<NFloat>.CreateTruncating<ulong>(0x7FFFFFFFFFFFFFFF));
                AssertBitwiseEqual((NFloat)9223372036854775808.0, NumberHelper<NFloat>.CreateTruncating<ulong>(0x8000000000000000));
                AssertBitwiseEqual((NFloat)18446744073709551615.0, NumberHelper<NFloat>.CreateTruncating<ulong>(0xFFFFFFFFFFFFFFFF));
            }
            else
            {
                AssertBitwiseEqual((NFloat)9223372036854775807.0f, NumberHelper<NFloat>.CreateTruncating<ulong>(0x7FFFFFFFFFFFFFFF));
                AssertBitwiseEqual((NFloat)9223372036854775808.0f, NumberHelper<NFloat>.CreateTruncating<ulong>(0x8000000000000000));
                AssertBitwiseEqual((NFloat)18446744073709551615.0f, NumberHelper<NFloat>.CreateTruncating<ulong>(0xFFFFFFFFFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateTruncatingFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual(PositiveZero, NumberHelper<NFloat>.CreateTruncating<nuint>(unchecked((nuint)0x0000000000000000)));
                AssertBitwiseEqual(PositiveOne, NumberHelper<NFloat>.CreateTruncating<nuint>(unchecked((nuint)0x0000000000000001)));
                AssertBitwiseEqual((NFloat)9223372036854775807.0, NumberHelper<NFloat>.CreateTruncating<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));

                // https://github.com/dotnet/roslyn/issues/60714
                // AssertBitwiseEqual((NFloat)9223372036854775808.0, NumberHelper<NFloat>.CreateTruncating<nuint>(unchecked((nuint)0x8000000000000000)));
                // AssertBitwiseEqual((NFloat)18446744073709551615.0, NumberHelper<NFloat>.CreateTruncating<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                AssertBitwiseEqual(PositiveZero, NumberHelper<NFloat>.CreateTruncating<nuint>((nuint)0x00000000));
                AssertBitwiseEqual(PositiveOne, NumberHelper<NFloat>.CreateTruncating<nuint>((nuint)0x00000001));
                AssertBitwiseEqual((NFloat)2147483647.0f, NumberHelper<NFloat>.CreateTruncating<nuint>((nuint)0x7FFFFFFF));

                // https://github.com/dotnet/roslyn/issues/60714
                // AssertBitwiseEqual((NFloat)2147483648.0f, NumberHelper<NFloat>.CreateTruncating<nuint>((nuint)0x80000000));
                // AssertBitwiseEqual((NFloat)4294967295.0f, NumberHelper<NFloat>.CreateTruncating<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void MaxTest()
        {
            AssertBitwiseEqual(PositiveOne, NumberHelper<NFloat>.Max(NFloat.NegativeInfinity, PositiveOne));
            AssertBitwiseEqual(PositiveOne, NumberHelper<NFloat>.Max(NFloat.MinValue, PositiveOne));
            AssertBitwiseEqual(PositiveOne, NumberHelper<NFloat>.Max(NegativeOne, PositiveOne));
            AssertBitwiseEqual(PositiveOne, NumberHelper<NFloat>.Max(-MinNormal, PositiveOne));
            AssertBitwiseEqual(PositiveOne, NumberHelper<NFloat>.Max(-MaxSubnormal, PositiveOne));
            AssertBitwiseEqual(PositiveOne, NumberHelper<NFloat>.Max(-NFloat.Epsilon, PositiveOne));
            AssertBitwiseEqual(PositiveOne, NumberHelper<NFloat>.Max(NegativeZero, PositiveOne));
            AssertBitwiseEqual(NFloat.NaN, NumberHelper<NFloat>.Max(NFloat.NaN, PositiveOne));
            AssertBitwiseEqual(PositiveOne, NumberHelper<NFloat>.Max(PositiveZero, PositiveOne));
            AssertBitwiseEqual(PositiveOne, NumberHelper<NFloat>.Max(NFloat.Epsilon, PositiveOne));
            AssertBitwiseEqual(PositiveOne, NumberHelper<NFloat>.Max(MaxSubnormal, PositiveOne));
            AssertBitwiseEqual(PositiveOne, NumberHelper<NFloat>.Max(MinNormal, PositiveOne));
            AssertBitwiseEqual(PositiveOne, NumberHelper<NFloat>.Max(PositiveOne, PositiveOne));
            AssertBitwiseEqual(NFloat.MaxValue, NumberHelper<NFloat>.Max(NFloat.MaxValue, PositiveOne));
            AssertBitwiseEqual(NFloat.PositiveInfinity, NumberHelper<NFloat>.Max(NFloat.PositiveInfinity, PositiveOne));
        }

        [Fact]
        public static void MinTest()
        {
            AssertBitwiseEqual(NFloat.NegativeInfinity, NumberHelper<NFloat>.Min(NFloat.NegativeInfinity, PositiveOne));
            AssertBitwiseEqual(NFloat.MinValue, NumberHelper<NFloat>.Min(NFloat.MinValue, PositiveOne));
            AssertBitwiseEqual(NegativeOne, NumberHelper<NFloat>.Min(NegativeOne, PositiveOne));
            AssertBitwiseEqual(-MinNormal, NumberHelper<NFloat>.Min(-MinNormal, PositiveOne));
            AssertBitwiseEqual(-MaxSubnormal, NumberHelper<NFloat>.Min(-MaxSubnormal, PositiveOne));
            AssertBitwiseEqual(-NFloat.Epsilon, NumberHelper<NFloat>.Min(-NFloat.Epsilon, PositiveOne));
            AssertBitwiseEqual(NegativeZero, NumberHelper<NFloat>.Min(NegativeZero, PositiveOne));
            AssertBitwiseEqual(NFloat.NaN, NumberHelper<NFloat>.Min(NFloat.NaN, PositiveOne));
            AssertBitwiseEqual(PositiveZero, NumberHelper<NFloat>.Min(PositiveZero, PositiveOne));
            AssertBitwiseEqual(NFloat.Epsilon, NumberHelper<NFloat>.Min(NFloat.Epsilon, PositiveOne));
            AssertBitwiseEqual(MaxSubnormal, NumberHelper<NFloat>.Min(MaxSubnormal, PositiveOne));
            AssertBitwiseEqual(MinNormal, NumberHelper<NFloat>.Min(MinNormal, PositiveOne));
            AssertBitwiseEqual(PositiveOne, NumberHelper<NFloat>.Min(PositiveOne, PositiveOne));
            AssertBitwiseEqual(PositiveOne, NumberHelper<NFloat>.Min(NFloat.MaxValue, PositiveOne));
            AssertBitwiseEqual(PositiveOne, NumberHelper<NFloat>.Min(NFloat.PositiveInfinity, PositiveOne));
        }

        [Fact]
        public static void SignTest()
        {
            Assert.Equal(-1, NumberHelper<NFloat>.Sign(NFloat.NegativeInfinity));
            Assert.Equal(-1, NumberHelper<NFloat>.Sign(NFloat.MinValue));
            Assert.Equal(-1, NumberHelper<NFloat>.Sign(NegativeOne));
            Assert.Equal(-1, NumberHelper<NFloat>.Sign(-MinNormal));
            Assert.Equal(-1, NumberHelper<NFloat>.Sign(-MaxSubnormal));
            Assert.Equal(-1, NumberHelper<NFloat>.Sign(-NFloat.Epsilon));

            Assert.Equal(0, NumberHelper<NFloat>.Sign(NegativeZero));
            Assert.Equal(0, NumberHelper<NFloat>.Sign(PositiveZero));

            Assert.Equal(1, NumberHelper<NFloat>.Sign(NFloat.Epsilon));
            Assert.Equal(1, NumberHelper<NFloat>.Sign(MaxSubnormal));
            Assert.Equal(1, NumberHelper<NFloat>.Sign(MinNormal));
            Assert.Equal(1, NumberHelper<NFloat>.Sign(PositiveOne));
            Assert.Equal(1, NumberHelper<NFloat>.Sign(NFloat.MaxValue));
            Assert.Equal(1, NumberHelper<NFloat>.Sign(NFloat.PositiveInfinity));

            Assert.Throws<ArithmeticException>(() => NumberHelper<NFloat>.Sign(NFloat.NaN));
        }

        [Fact]
        public static void TryCreateFromByteTest()
        {
            NFloat result;

            Assert.True(NumberHelper<NFloat>.TryCreate<byte>(0x00, out result));
            Assert.Equal(PositiveZero, result);

            Assert.True(NumberHelper<NFloat>.TryCreate<byte>(0x01, out result));
            Assert.Equal(PositiveOne, result);

            Assert.True(NumberHelper<NFloat>.TryCreate<byte>(0x7F, out result));
            Assert.Equal((NFloat)127.0f, result);

            Assert.True(NumberHelper<NFloat>.TryCreate<byte>(0x80, out result));
            Assert.Equal((NFloat)128.0f, result);

            Assert.True(NumberHelper<NFloat>.TryCreate<byte>(0xFF, out result));
            Assert.Equal((NFloat)255.0f, result);
        }

        [Fact]
        public static void TryCreateFromCharTest()
        {
            NFloat result;

            Assert.True(NumberHelper<NFloat>.TryCreate<char>((char)0x0000, out result));
            Assert.Equal(PositiveZero, result);

            Assert.True(NumberHelper<NFloat>.TryCreate<char>((char)0x0001, out result));
            Assert.Equal(PositiveOne, result);

            Assert.True(NumberHelper<NFloat>.TryCreate<char>((char)0x7FFF, out result));
            Assert.Equal((NFloat)32767.0f, result);

            Assert.True(NumberHelper<NFloat>.TryCreate<char>((char)0x8000, out result));
            Assert.Equal((NFloat)32768.0f, result);

            Assert.True(NumberHelper<NFloat>.TryCreate<char>((char)0xFFFF, out result));
            Assert.Equal((NFloat)65535.0f, result);
        }

        [Fact]
        public static void TryCreateFromInt16Test()
        {
            NFloat result;

            Assert.True(NumberHelper<NFloat>.TryCreate<short>(0x0000, out result));
            Assert.Equal(PositiveZero, result);

            Assert.True(NumberHelper<NFloat>.TryCreate<short>(0x0001, out result));
            Assert.Equal(PositiveOne, result);

            Assert.True(NumberHelper<NFloat>.TryCreate<short>(0x7FFF, out result));
            Assert.Equal((NFloat)32767.0f, result);

            Assert.True(NumberHelper<NFloat>.TryCreate<short>(unchecked((short)0x8000), out result));
            Assert.Equal((NFloat)(-32768.0f), result);

            Assert.True(NumberHelper<NFloat>.TryCreate<short>(unchecked((short)0xFFFF), out result));
            Assert.Equal(NegativeOne, result);
        }

        [Fact]
        public static void TryCreateFromInt32Test()
        {
            NFloat result;

            Assert.True(NumberHelper<NFloat>.TryCreate<int>(0x00000000, out result));
            Assert.Equal(PositiveZero, result);

            Assert.True(NumberHelper<NFloat>.TryCreate<int>(0x00000001, out result));
            Assert.Equal(PositiveOne, result);

            Assert.True(NumberHelper<NFloat>.TryCreate<int>(unchecked((int)0xFFFFFFFF), out result));
            Assert.Equal(NegativeOne, result);

            if (Environment.Is64BitProcess)
            {
                Assert.True(NumberHelper<NFloat>.TryCreate<int>(0x7FFFFFFF, out result));
                Assert.Equal((NFloat)2147483647.0, result);

                Assert.True(NumberHelper<NFloat>.TryCreate<int>(unchecked((int)0x80000000), out result));
                Assert.Equal((NFloat)(-2147483648.0), result);
            }
            else
            {
                Assert.True(NumberHelper<NFloat>.TryCreate<int>(0x7FFFFFFF, out result));
                Assert.Equal((NFloat)2147483647.0f, result);

                Assert.True(NumberHelper<NFloat>.TryCreate<int>(unchecked((int)0x80000000), out result));
                Assert.Equal((NFloat)(-2147483648.0f), result);
            }
        }

        [Fact]
        public static void TryCreateFromInt64Test()
        {
            NFloat result;

            Assert.True(NumberHelper<NFloat>.TryCreate<long>(0x0000000000000000, out result));
            Assert.Equal(PositiveZero, result);

            Assert.True(NumberHelper<NFloat>.TryCreate<long>(0x0000000000000001, out result));
            Assert.Equal(PositiveOne, result);

            Assert.True(NumberHelper<NFloat>.TryCreate<long>(unchecked(unchecked((long)0xFFFFFFFFFFFFFFFF)), out result));
            Assert.Equal(NegativeOne, result);

            if (Environment.Is64BitProcess)
            {
                Assert.True(NumberHelper<NFloat>.TryCreate<long>(0x7FFFFFFFFFFFFFFF, out result));
                Assert.Equal((NFloat)9223372036854775807.0, result);

                Assert.True(NumberHelper<NFloat>.TryCreate<long>(unchecked(unchecked((long)0x8000000000000000)), out result));
                Assert.Equal((NFloat)(-9223372036854775808.0), result);
            }
            else
            {
                Assert.True(NumberHelper<NFloat>.TryCreate<long>(0x7FFFFFFFFFFFFFFF, out result));
                Assert.Equal((NFloat)9223372036854775807.0f, result);

                Assert.True(NumberHelper<NFloat>.TryCreate<long>(unchecked(unchecked((long)0x8000000000000000)), out result));
                Assert.Equal((NFloat)(-9223372036854775808.0f), result);
            }
        }

        [Fact]
        public static void TryCreateFromIntPtrTest()
        {
            NFloat result;

            if (Environment.Is64BitProcess)
            {
                Assert.True(NumberHelper<NFloat>.TryCreate<nint>(unchecked((nint)0x0000000000000000), out result));
                Assert.Equal(PositiveZero, result);

                Assert.True(NumberHelper<NFloat>.TryCreate<nint>(unchecked((nint)0x0000000000000001), out result));
                Assert.Equal(PositiveOne, result);

                Assert.True(NumberHelper<NFloat>.TryCreate<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF), out result));
                Assert.Equal((NFloat)9223372036854775807.0, result);

                Assert.True(NumberHelper<NFloat>.TryCreate<nint>(unchecked((nint)0x8000000000000000), out result));
                Assert.Equal((NFloat)(-9223372036854775808.0), result);

                Assert.True(NumberHelper<NFloat>.TryCreate<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF), out result));
                Assert.Equal(NegativeOne, result);
            }
            else
            {
                Assert.True(NumberHelper<NFloat>.TryCreate<nint>((nint)0x00000000, out result));
                Assert.Equal(PositiveZero, result);

                Assert.True(NumberHelper<NFloat>.TryCreate<nint>((nint)0x00000001, out result));
                Assert.Equal(PositiveOne, result);

                Assert.True(NumberHelper<NFloat>.TryCreate<nint>((nint)0x7FFFFFFF, out result));
                Assert.Equal((NFloat)2147483647.0f, result);

                Assert.True(NumberHelper<NFloat>.TryCreate<nint>(unchecked((nint)0x80000000), out result));
                Assert.Equal((NFloat)(-2147483648.0f), result);

                Assert.True(NumberHelper<NFloat>.TryCreate<nint>(unchecked((nint)0xFFFFFFFF), out result));
                Assert.Equal(NegativeOne, result);
            }
        }

        [Fact]
        public static void TryCreateFromSByteTest()
        {
            NFloat result;

            Assert.True(NumberHelper<NFloat>.TryCreate<sbyte>(0x00, out result));
            Assert.Equal(PositiveZero, result);

            Assert.True(NumberHelper<NFloat>.TryCreate<sbyte>(0x01, out result));
            Assert.Equal(PositiveOne, result);

            Assert.True(NumberHelper<NFloat>.TryCreate<sbyte>(0x7F, out result));
            Assert.Equal((NFloat)127.0f, result);

            Assert.True(NumberHelper<NFloat>.TryCreate<sbyte>(unchecked((sbyte)0x80), out result));
            Assert.Equal((NFloat)(-128.0f), result);

            Assert.True(NumberHelper<NFloat>.TryCreate<sbyte>(unchecked((sbyte)0xFF), out result));
            Assert.Equal(NegativeOne, result);
        }

        [Fact]
        public static void TryCreateFromUInt16Test()
        {
            NFloat result;

            Assert.True(NumberHelper<NFloat>.TryCreate<ushort>(0x0000, out result));
            Assert.Equal(PositiveZero, result);

            Assert.True(NumberHelper<NFloat>.TryCreate<ushort>(0x0001, out result));
            Assert.Equal(PositiveOne, result);

            Assert.True(NumberHelper<NFloat>.TryCreate<ushort>(0x7FFF, out result));
            Assert.Equal((NFloat)32767.0f, result);

            Assert.True(NumberHelper<NFloat>.TryCreate<ushort>(0x8000, out result));
            Assert.Equal((NFloat)32768.0f, result);

            Assert.True(NumberHelper<NFloat>.TryCreate<ushort>(0xFFFF, out result));
            Assert.Equal((NFloat)65535.0f, result);
        }

        [Fact]
        public static void TryCreateFromUInt32Test()
        {
            NFloat result;

            Assert.True(NumberHelper<NFloat>.TryCreate<uint>(0x00000000, out result));
            Assert.Equal(PositiveZero, result);

            Assert.True(NumberHelper<NFloat>.TryCreate<uint>(0x00000001, out result));
            Assert.Equal(PositiveOne, result);

            if (Environment.Is64BitProcess)
            {
                Assert.True(NumberHelper<NFloat>.TryCreate<uint>(0x7FFFFFFF, out result));
                Assert.Equal((NFloat)2147483647.0, result);

                Assert.True(NumberHelper<NFloat>.TryCreate<uint>(0x80000000, out result));
                Assert.Equal((NFloat)2147483648.0, result);

                Assert.True(NumberHelper<NFloat>.TryCreate<uint>(0xFFFFFFFF, out result));
                Assert.Equal((NFloat)4294967295.0, result);
            }
            else
            {
                Assert.True(NumberHelper<NFloat>.TryCreate<uint>(0x7FFFFFFF, out result));
                Assert.Equal((NFloat)2147483647.0f, result);

                Assert.True(NumberHelper<NFloat>.TryCreate<uint>(0x80000000, out result));
                Assert.Equal((NFloat)2147483648.0f, result);

                Assert.True(NumberHelper<NFloat>.TryCreate<uint>(0xFFFFFFFF, out result));
                Assert.Equal((NFloat)4294967295.0f, result);
            }
        }

        [Fact]
        public static void TryCreateFromUInt64Test()
        {
            NFloat result;

            Assert.True(NumberHelper<NFloat>.TryCreate<ulong>(0x0000000000000000, out result));
            Assert.Equal(PositiveZero, result);

            Assert.True(NumberHelper<NFloat>.TryCreate<ulong>(0x0000000000000001, out result));
            Assert.Equal(PositiveOne, result);

            if (Environment.Is64BitProcess)
            {
                Assert.True(NumberHelper<NFloat>.TryCreate<ulong>(0x7FFFFFFFFFFFFFFF, out result));
                Assert.Equal((NFloat)9223372036854775807.0, result);

                Assert.True(NumberHelper<NFloat>.TryCreate<ulong>(0x8000000000000000, out result));
                Assert.Equal((NFloat)9223372036854775808.0, result);

                Assert.True(NumberHelper<NFloat>.TryCreate<ulong>(0xFFFFFFFFFFFFFFFF, out result));
                Assert.Equal((NFloat)18446744073709551615.0, result);
            }
            else
            {
                Assert.True(NumberHelper<NFloat>.TryCreate<ulong>(0x7FFFFFFFFFFFFFFF, out result));
                Assert.Equal((NFloat)9223372036854775807.0f, result);

                Assert.True(NumberHelper<NFloat>.TryCreate<ulong>(0x8000000000000000, out result));
                Assert.Equal((NFloat)9223372036854775808.0f, result);

                Assert.True(NumberHelper<NFloat>.TryCreate<ulong>(0xFFFFFFFFFFFFFFFF, out result));
                Assert.Equal((NFloat)18446744073709551615.0f, result);
            }
        }

        [Fact]
        public static void TryCreateFromUIntPtrTest()
        {
            NFloat result;

            if (Environment.Is64BitProcess)
            {
                Assert.True(NumberHelper<NFloat>.TryCreate<nuint>(unchecked((nuint)0x0000000000000000), out result));
                Assert.Equal(PositiveZero, result);

                Assert.True(NumberHelper<NFloat>.TryCreate<nuint>(unchecked((nuint)0x0000000000000001), out result));
                Assert.Equal(PositiveOne, result);

                Assert.True(NumberHelper<NFloat>.TryCreate<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF), out result));
                Assert.Equal((NFloat)9223372036854775807.0, result);

                // https://github.com/dotnet/roslyn/issues/60714
                // Assert.True(NumberHelper<NFloat>.TryCreate<nuint>(unchecked((nuint)0x8000000000000000), out result));
                // Assert.Equal((NFloat)9223372036854775808.0, result);
                //
                // Assert.True(NumberHelper<NFloat>.TryCreate<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF), out result));
                // Assert.Equal((NFloat)18446744073709551615.0, result);
            }
            else
            {
                Assert.True(NumberHelper<NFloat>.TryCreate<nuint>((nuint)0x00000000, out result));
                Assert.Equal(PositiveZero, result);

                Assert.True(NumberHelper<NFloat>.TryCreate<nuint>((nuint)0x00000001, out result));
                Assert.Equal(PositiveOne, result);

                Assert.True(NumberHelper<NFloat>.TryCreate<nuint>((nuint)0x7FFFFFFF, out result));
                Assert.Equal((NFloat)2147483647.0f, result);

                // https://github.com/dotnet/roslyn/issues/60714
                // Assert.True(NumberHelper<NFloat>.TryCreate<nuint>(unchecked((nuint)0x80000000), out result));
                // Assert.Equal((NFloat)2147483648.0f, result);
                //
                // Assert.True(NumberHelper<NFloat>.TryCreate<nuint>(unchecked((nuint)0xFFFFFFFF), out result));
                // Assert.Equal((NFloat)4294967295.0f, result);
            }
        }

        [Fact]
        public static void GetExponentByteCountTest()
        {
            int expected = Environment.Is64BitProcess ? 2 : 1;

            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetExponentByteCount(NFloat.NegativeInfinity));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetExponentByteCount(NFloat.MinValue));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetExponentByteCount(NegativeOne));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetExponentByteCount(-MinNormal));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetExponentByteCount(-MaxSubnormal));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetExponentByteCount(-NFloat.Epsilon));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetExponentByteCount(NegativeZero));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetExponentByteCount(NFloat.NaN));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetExponentByteCount(PositiveZero));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetExponentByteCount(NFloat.Epsilon));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetExponentByteCount(MaxSubnormal));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetExponentByteCount(MinNormal));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetExponentByteCount(PositiveOne));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetExponentByteCount(NFloat.MaxValue));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetExponentByteCount(NFloat.PositiveInfinity));
        }

        [Fact]
        public static void GetExponentShortestBitLengthTest()
        {
            long expected = Environment.Is64BitProcess ? 11 : 8;

            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetExponentShortestBitLength(NFloat.NegativeInfinity));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetExponentShortestBitLength(-MinNormal));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetExponentShortestBitLength(-MaxSubnormal));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetExponentShortestBitLength(-NFloat.Epsilon));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetExponentShortestBitLength(NegativeZero));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetExponentShortestBitLength(NFloat.NaN));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetExponentShortestBitLength(PositiveZero));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetExponentShortestBitLength(NFloat.Epsilon));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetExponentShortestBitLength(MaxSubnormal));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetExponentShortestBitLength(MinNormal));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetExponentShortestBitLength(NFloat.PositiveInfinity));

            expected = Environment.Is64BitProcess ? 10 : 7;

            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetExponentShortestBitLength(NFloat.MinValue));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetExponentShortestBitLength(NFloat.MaxValue));

            expected = 0;

            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetExponentShortestBitLength(NegativeOne));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetExponentShortestBitLength(PositiveOne));
        }

        [Fact]
        public static void GetSignificandByteCountTest()
        {
            int expected = Environment.Is64BitProcess ? 8 : 4;

            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetSignificandByteCount(NFloat.NegativeInfinity));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetSignificandByteCount(NFloat.MinValue));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetSignificandByteCount(NegativeOne));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetSignificandByteCount(-MinNormal));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetSignificandByteCount(-MaxSubnormal));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetSignificandByteCount(-NFloat.Epsilon));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetSignificandByteCount(NegativeZero));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetSignificandByteCount(NFloat.NaN));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetSignificandByteCount(PositiveZero));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetSignificandByteCount(NFloat.Epsilon));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetSignificandByteCount(MaxSubnormal));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetSignificandByteCount(MinNormal));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetSignificandByteCount(PositiveOne));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetSignificandByteCount(NFloat.MaxValue));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetSignificandByteCount(NFloat.PositiveInfinity));
        }

        [Fact]
        public static void GetSignificandBitLengthTest()
        {
            long expected = Environment.Is64BitProcess ? 53 : 24;

            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetSignificandBitLength(NFloat.NegativeInfinity));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetSignificandBitLength(NFloat.MinValue));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetSignificandBitLength(NegativeOne));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetSignificandBitLength(-MinNormal));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetSignificandBitLength(-MaxSubnormal));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetSignificandBitLength(-NFloat.Epsilon));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetSignificandBitLength(NegativeZero));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetSignificandBitLength(NFloat.NaN));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetSignificandBitLength(PositiveZero));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetSignificandBitLength(NFloat.Epsilon));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetSignificandBitLength(MaxSubnormal));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetSignificandBitLength(MinNormal));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetSignificandBitLength(PositiveOne));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetSignificandBitLength(NFloat.MaxValue));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetSignificandBitLength(NFloat.PositiveInfinity));
        }

        [Fact]
        public static void TryWriteExponentLittleEndianTest()
        {
            if (Environment.Is64BitProcess)
            {
                Span<byte> destination = stackalloc byte[2];
                int bytesWritten = 0;

                Assert.True(FloatingPointHelper<NFloat>.TryWriteExponentLittleEndian(NFloat.NegativeInfinity, destination, out bytesWritten));
                Assert.Equal(2, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x04 }, destination.ToArray()); // +1024

                Assert.True(FloatingPointHelper<NFloat>.TryWriteExponentLittleEndian(NFloat.MinValue, destination, out bytesWritten));
                Assert.Equal(2, bytesWritten);
                Assert.Equal(new byte[] { 0xFF, 0x03 }, destination.ToArray()); // +1023

                Assert.True(FloatingPointHelper<NFloat>.TryWriteExponentLittleEndian(NegativeOne, destination, out bytesWritten));
                Assert.Equal(2, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x00 }, destination.ToArray()); // +0

                Assert.True(FloatingPointHelper<NFloat>.TryWriteExponentLittleEndian(-MinNormal, destination, out bytesWritten));
                Assert.Equal(2, bytesWritten);
                Assert.Equal(new byte[] { 0x02, 0xFC }, destination.ToArray()); // -1022

                Assert.True(FloatingPointHelper<NFloat>.TryWriteExponentLittleEndian(-MaxSubnormal, destination, out bytesWritten));
                Assert.Equal(2, bytesWritten);
                Assert.Equal(new byte[] { 0x01, 0xFC }, destination.ToArray()); // -1023

                Assert.True(FloatingPointHelper<NFloat>.TryWriteExponentLittleEndian(-NFloat.Epsilon, destination, out bytesWritten));
                Assert.Equal(2, bytesWritten);
                Assert.Equal(new byte[] { 0x01, 0xFC }, destination.ToArray()); // -1023

                Assert.True(FloatingPointHelper<NFloat>.TryWriteExponentLittleEndian(NegativeZero, destination, out bytesWritten));
                Assert.Equal(2, bytesWritten);
                Assert.Equal(new byte[] { 0x01, 0xFC }, destination.ToArray()); // -1023

                Assert.True(FloatingPointHelper<NFloat>.TryWriteExponentLittleEndian(NFloat.NaN, destination, out bytesWritten));
                Assert.Equal(2, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x04 }, destination.ToArray()); // +1024

                Assert.True(FloatingPointHelper<NFloat>.TryWriteExponentLittleEndian(PositiveZero, destination, out bytesWritten));
                Assert.Equal(2, bytesWritten);
                Assert.Equal(new byte[] { 0x01, 0xFC }, destination.ToArray()); // -1023

                Assert.True(FloatingPointHelper<NFloat>.TryWriteExponentLittleEndian(NFloat.Epsilon, destination, out bytesWritten));
                Assert.Equal(2, bytesWritten);
                Assert.Equal(new byte[] { 0x01, 0xFC }, destination.ToArray()); // -1023

                Assert.True(FloatingPointHelper<NFloat>.TryWriteExponentLittleEndian(MaxSubnormal, destination, out bytesWritten));
                Assert.Equal(2, bytesWritten);
                Assert.Equal(new byte[] { 0x01, 0xFC }, destination.ToArray()); // -1023

                Assert.True(FloatingPointHelper<NFloat>.TryWriteExponentLittleEndian(MinNormal, destination, out bytesWritten));
                Assert.Equal(2, bytesWritten);
                Assert.Equal(new byte[] { 0x02, 0xFC }, destination.ToArray()); // -1022

                Assert.True(FloatingPointHelper<NFloat>.TryWriteExponentLittleEndian(PositiveOne, destination, out bytesWritten));
                Assert.Equal(2, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x00 }, destination.ToArray()); // +0

                Assert.True(FloatingPointHelper<NFloat>.TryWriteExponentLittleEndian(NFloat.MaxValue, destination, out bytesWritten));
                Assert.Equal(2, bytesWritten);
                Assert.Equal(new byte[] { 0xFF, 0x03 }, destination.ToArray()); // +1023

                Assert.True(FloatingPointHelper<NFloat>.TryWriteExponentLittleEndian(NFloat.PositiveInfinity, destination, out bytesWritten));
                Assert.Equal(2, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x04 }, destination.ToArray()); // +1024

                Assert.False(FloatingPointHelper<NFloat>.TryWriteExponentLittleEndian(default, Span<byte>.Empty, out bytesWritten));
                Assert.Equal(0, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x04 }, destination.ToArray());
            }
            else
            {
                Span<byte> destination = stackalloc byte[1];
                int bytesWritten = 0;

                Assert.True(FloatingPointHelper<NFloat>.TryWriteExponentLittleEndian(NFloat.NegativeInfinity, destination, out bytesWritten));
                Assert.Equal(1, bytesWritten);
                Assert.Equal(new byte[] { 0x80 }, destination.ToArray()); // -128

                Assert.True(FloatingPointHelper<NFloat>.TryWriteExponentLittleEndian(NFloat.MinValue, destination, out bytesWritten));
                Assert.Equal(1, bytesWritten);
                Assert.Equal(new byte[] { 0x7F }, destination.ToArray()); // +127

                Assert.True(FloatingPointHelper<NFloat>.TryWriteExponentLittleEndian(NegativeOne, destination, out bytesWritten));
                Assert.Equal(1, bytesWritten);
                Assert.Equal(new byte[] { 0x00 }, destination.ToArray()); // +0

                Assert.True(FloatingPointHelper<NFloat>.TryWriteExponentLittleEndian(-MinNormal, destination, out bytesWritten));
                Assert.Equal(1, bytesWritten);
                Assert.Equal(new byte[] { 0x82 }, destination.ToArray()); // -126

                Assert.True(FloatingPointHelper<NFloat>.TryWriteExponentLittleEndian(-MaxSubnormal, destination, out bytesWritten));
                Assert.Equal(1, bytesWritten);
                Assert.Equal(new byte[] { 0x81 }, destination.ToArray()); // -127

                Assert.True(FloatingPointHelper<NFloat>.TryWriteExponentLittleEndian(-NFloat.Epsilon, destination, out bytesWritten));
                Assert.Equal(1, bytesWritten);
                Assert.Equal(new byte[] { 0x81 }, destination.ToArray()); // -127

                Assert.True(FloatingPointHelper<NFloat>.TryWriteExponentLittleEndian(NegativeZero, destination, out bytesWritten));
                Assert.Equal(1, bytesWritten);
                Assert.Equal(new byte[] { 0x81 }, destination.ToArray()); // -127

                Assert.True(FloatingPointHelper<NFloat>.TryWriteExponentLittleEndian(NFloat.NaN, destination, out bytesWritten));
                Assert.Equal(1, bytesWritten);
                Assert.Equal(new byte[] { 0x80 }, destination.ToArray()); // -128

                Assert.True(FloatingPointHelper<NFloat>.TryWriteExponentLittleEndian(PositiveZero, destination, out bytesWritten));
                Assert.Equal(1, bytesWritten);
                Assert.Equal(new byte[] { 0x81 }, destination.ToArray()); // -127

                Assert.True(FloatingPointHelper<NFloat>.TryWriteExponentLittleEndian(NFloat.Epsilon, destination, out bytesWritten));
                Assert.Equal(1, bytesWritten);
                Assert.Equal(new byte[] { 0x81 }, destination.ToArray()); // -127

                Assert.True(FloatingPointHelper<NFloat>.TryWriteExponentLittleEndian(MaxSubnormal, destination, out bytesWritten));
                Assert.Equal(1, bytesWritten);
                Assert.Equal(new byte[] { 0x81 }, destination.ToArray()); // -127

                Assert.True(FloatingPointHelper<NFloat>.TryWriteExponentLittleEndian(MinNormal, destination, out bytesWritten));
                Assert.Equal(1, bytesWritten);
                Assert.Equal(new byte[] { 0x82 }, destination.ToArray()); // -126

                Assert.True(FloatingPointHelper<NFloat>.TryWriteExponentLittleEndian(PositiveOne, destination, out bytesWritten));
                Assert.Equal(1, bytesWritten);
                Assert.Equal(new byte[] { 0x00 }, destination.ToArray()); // +0

                Assert.True(FloatingPointHelper<NFloat>.TryWriteExponentLittleEndian(NFloat.MaxValue, destination, out bytesWritten));
                Assert.Equal(1, bytesWritten);
                Assert.Equal(new byte[] { 0x7F }, destination.ToArray()); // +127

                Assert.True(FloatingPointHelper<NFloat>.TryWriteExponentLittleEndian(NFloat.PositiveInfinity, destination, out bytesWritten));
                Assert.Equal(1, bytesWritten);
                Assert.Equal(new byte[] { 0x80 }, destination.ToArray()); // -128

                Assert.False(FloatingPointHelper<NFloat>.TryWriteExponentLittleEndian(default, Span<byte>.Empty, out bytesWritten));
                Assert.Equal(0, bytesWritten);
                Assert.Equal(new byte[] { 0x80 }, destination.ToArray());
            }
        }

        [Fact]
        public static void TryWriteSignificandLittleEndianTest()
        {
            if (Environment.Is64BitProcess)
            {
                Span<byte> destination = stackalloc byte[8];
                int bytesWritten = 0;

                Assert.True(FloatingPointHelper<NFloat>.TryWriteSignificandLittleEndian(NFloat.NegativeInfinity, destination, out bytesWritten));
                Assert.Equal(8, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x10, 0x00 }, destination.ToArray());

                Assert.True(FloatingPointHelper<NFloat>.TryWriteSignificandLittleEndian(NFloat.MinValue, destination, out bytesWritten));
                Assert.Equal(8, bytesWritten);
                Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x1F, 0x00 }, destination.ToArray());

                Assert.True(FloatingPointHelper<NFloat>.TryWriteSignificandLittleEndian(NegativeOne, destination, out bytesWritten));
                Assert.Equal(8, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x10, 0x00 }, destination.ToArray());

                Assert.True(FloatingPointHelper<NFloat>.TryWriteSignificandLittleEndian(-MinNormal, destination, out bytesWritten));
                Assert.Equal(8, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x10, 0x00 }, destination.ToArray());

                Assert.True(FloatingPointHelper<NFloat>.TryWriteSignificandLittleEndian(-MaxSubnormal, destination, out bytesWritten));
                Assert.Equal(8, bytesWritten);
                Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x0F, 0x00 }, destination.ToArray());

                Assert.True(FloatingPointHelper<NFloat>.TryWriteSignificandLittleEndian(-NFloat.Epsilon, destination, out bytesWritten));
                Assert.Equal(8, bytesWritten);
                Assert.Equal(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

                Assert.True(FloatingPointHelper<NFloat>.TryWriteSignificandLittleEndian(NegativeZero, destination, out bytesWritten));
                Assert.Equal(8, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

                Assert.True(FloatingPointHelper<NFloat>.TryWriteSignificandLittleEndian(NFloat.NaN, destination, out bytesWritten));
                Assert.Equal(8, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x18, 0x00 }, destination.ToArray());

                Assert.True(FloatingPointHelper<NFloat>.TryWriteSignificandLittleEndian(PositiveZero, destination, out bytesWritten));
                Assert.Equal(8, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

                Assert.True(FloatingPointHelper<NFloat>.TryWriteSignificandLittleEndian(NFloat.Epsilon, destination, out bytesWritten));
                Assert.Equal(8, bytesWritten);
                Assert.Equal(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

                Assert.True(FloatingPointHelper<NFloat>.TryWriteSignificandLittleEndian(MaxSubnormal, destination, out bytesWritten));
                Assert.Equal(8, bytesWritten);
                Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x0F, 0x00 }, destination.ToArray());

                Assert.True(FloatingPointHelper<NFloat>.TryWriteSignificandLittleEndian(MinNormal, destination, out bytesWritten));
                Assert.Equal(8, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x10, 0x00 }, destination.ToArray());

                Assert.True(FloatingPointHelper<NFloat>.TryWriteSignificandLittleEndian(PositiveOne, destination, out bytesWritten));
                Assert.Equal(8, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x10, 0x00 }, destination.ToArray());

                Assert.True(FloatingPointHelper<NFloat>.TryWriteSignificandLittleEndian(NFloat.MaxValue, destination, out bytesWritten));
                Assert.Equal(8, bytesWritten);
                Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x1F, 0x00 }, destination.ToArray());

                Assert.True(FloatingPointHelper<NFloat>.TryWriteSignificandLittleEndian(NFloat.PositiveInfinity, destination, out bytesWritten));
                Assert.Equal(8, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x10, 0x00 }, destination.ToArray());

                Assert.False(FloatingPointHelper<NFloat>.TryWriteSignificandLittleEndian(default, Span<byte>.Empty, out bytesWritten));
                Assert.Equal(0, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x10, 0x00 }, destination.ToArray());
            }
            else
            {
                Span<byte> destination = stackalloc byte[4];
                int bytesWritten = 0;

                Assert.True(FloatingPointHelper<NFloat>.TryWriteSignificandLittleEndian(NFloat.NegativeInfinity, destination, out bytesWritten));
                Assert.Equal(4, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x00, 0x80, 0x00 }, destination.ToArray());

                Assert.True(FloatingPointHelper<NFloat>.TryWriteSignificandLittleEndian(NFloat.MinValue, destination, out bytesWritten));
                Assert.Equal(4, bytesWritten);
                Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0x00 }, destination.ToArray());

                Assert.True(FloatingPointHelper<NFloat>.TryWriteSignificandLittleEndian(NegativeOne, destination, out bytesWritten));
                Assert.Equal(4, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x00, 0x80, 0x00 }, destination.ToArray());

                Assert.True(FloatingPointHelper<NFloat>.TryWriteSignificandLittleEndian(-MinNormal, destination, out bytesWritten));
                Assert.Equal(4, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x00, 0x80, 0x00 }, destination.ToArray());

                Assert.True(FloatingPointHelper<NFloat>.TryWriteSignificandLittleEndian(-MaxSubnormal, destination, out bytesWritten));
                Assert.Equal(4, bytesWritten);
                Assert.Equal(new byte[] { 0xFF, 0xFF, 0x7F, 0x00 }, destination.ToArray());

                Assert.True(FloatingPointHelper<NFloat>.TryWriteSignificandLittleEndian(-NFloat.Epsilon, destination, out bytesWritten));
                Assert.Equal(4, bytesWritten);
                Assert.Equal(new byte[] { 0x01, 0x00, 0x00, 0x00 }, destination.ToArray());

                Assert.True(FloatingPointHelper<NFloat>.TryWriteSignificandLittleEndian(NegativeZero, destination, out bytesWritten));
                Assert.Equal(4, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

                Assert.True(FloatingPointHelper<NFloat>.TryWriteSignificandLittleEndian(NFloat.NaN, destination, out bytesWritten));
                Assert.Equal(4, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x00, 0xC0, 0x00 }, destination.ToArray());

                Assert.True(FloatingPointHelper<NFloat>.TryWriteSignificandLittleEndian(PositiveZero, destination, out bytesWritten));
                Assert.Equal(4, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

                Assert.True(FloatingPointHelper<NFloat>.TryWriteSignificandLittleEndian(NFloat.Epsilon, destination, out bytesWritten));
                Assert.Equal(4, bytesWritten);
                Assert.Equal(new byte[] { 0x01, 0x00, 0x00, 0x00 }, destination.ToArray());

                Assert.True(FloatingPointHelper<NFloat>.TryWriteSignificandLittleEndian(MaxSubnormal, destination, out bytesWritten));
                Assert.Equal(4, bytesWritten);
                Assert.Equal(new byte[] { 0xFF, 0xFF, 0x7F, 0x00 }, destination.ToArray());

                Assert.True(FloatingPointHelper<NFloat>.TryWriteSignificandLittleEndian(MinNormal, destination, out bytesWritten));
                Assert.Equal(4, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x00, 0x80, 0x00 }, destination.ToArray());

                Assert.True(FloatingPointHelper<NFloat>.TryWriteSignificandLittleEndian(PositiveOne, destination, out bytesWritten));
                Assert.Equal(4, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x00, 0x80, 0x00 }, destination.ToArray());

                Assert.True(FloatingPointHelper<NFloat>.TryWriteSignificandLittleEndian(NFloat.MaxValue, destination, out bytesWritten));
                Assert.Equal(4, bytesWritten);
                Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0x00 }, destination.ToArray());

                Assert.True(FloatingPointHelper<NFloat>.TryWriteSignificandLittleEndian(NFloat.PositiveInfinity, destination, out bytesWritten));
                Assert.Equal(4, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x00, 0x80, 0x00 }, destination.ToArray());

                Assert.False(FloatingPointHelper<NFloat>.TryWriteSignificandLittleEndian(default, Span<byte>.Empty, out bytesWritten));
                Assert.Equal(0, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x00, 0x80, 0x00 }, destination.ToArray());
            }
        }

        [Fact]
        public static void op_SubtractionTest()
        {
            AssertBitwiseEqual(NFloat.NegativeInfinity, SubtractionOperatorsHelper<NFloat, NFloat, NFloat>.op_Subtraction(NFloat.NegativeInfinity, PositiveOne));
            AssertBitwiseEqual(NFloat.MinValue, SubtractionOperatorsHelper<NFloat, NFloat, NFloat>.op_Subtraction(NFloat.MinValue, PositiveOne));
            AssertBitwiseEqual(NegativeTwo, SubtractionOperatorsHelper<NFloat, NFloat, NFloat>.op_Subtraction(NegativeOne, PositiveOne));
            AssertBitwiseEqual(NegativeOne, SubtractionOperatorsHelper<NFloat, NFloat, NFloat>.op_Subtraction(-MinNormal, PositiveOne));
            AssertBitwiseEqual(NegativeOne, SubtractionOperatorsHelper<NFloat, NFloat, NFloat>.op_Subtraction(-MaxSubnormal, PositiveOne));
            AssertBitwiseEqual(NegativeOne, SubtractionOperatorsHelper<NFloat, NFloat, NFloat>.op_Subtraction(-NFloat.Epsilon, PositiveOne));
            AssertBitwiseEqual(NegativeOne, SubtractionOperatorsHelper<NFloat, NFloat, NFloat>.op_Subtraction(NegativeZero, PositiveOne));
            AssertBitwiseEqual(NFloat.NaN, SubtractionOperatorsHelper<NFloat, NFloat, NFloat>.op_Subtraction(NFloat.NaN, PositiveOne));
            AssertBitwiseEqual(NegativeOne, SubtractionOperatorsHelper<NFloat, NFloat, NFloat>.op_Subtraction(PositiveZero, PositiveOne));
            AssertBitwiseEqual(NegativeOne, SubtractionOperatorsHelper<NFloat, NFloat, NFloat>.op_Subtraction(NFloat.Epsilon, PositiveOne));
            AssertBitwiseEqual(NegativeOne, SubtractionOperatorsHelper<NFloat, NFloat, NFloat>.op_Subtraction(MaxSubnormal, PositiveOne));
            AssertBitwiseEqual(NegativeOne, SubtractionOperatorsHelper<NFloat, NFloat, NFloat>.op_Subtraction(MinNormal, PositiveOne));
            AssertBitwiseEqual(PositiveZero, SubtractionOperatorsHelper<NFloat, NFloat, NFloat>.op_Subtraction(PositiveOne, PositiveOne));
            AssertBitwiseEqual(NFloat.MaxValue, SubtractionOperatorsHelper<NFloat, NFloat, NFloat>.op_Subtraction(NFloat.MaxValue, PositiveOne));
            AssertBitwiseEqual(NFloat.PositiveInfinity, SubtractionOperatorsHelper<NFloat, NFloat, NFloat>.op_Subtraction(NFloat.PositiveInfinity, PositiveOne));
        }

        [Fact]
        public static void op_CheckedSubtractionTest()
        {
            AssertBitwiseEqual(NFloat.NegativeInfinity, SubtractionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedSubtraction(NFloat.NegativeInfinity, PositiveOne));
            AssertBitwiseEqual(NFloat.MinValue, SubtractionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedSubtraction(NFloat.MinValue, PositiveOne));
            AssertBitwiseEqual(NegativeTwo, SubtractionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedSubtraction(NegativeOne, PositiveOne));
            AssertBitwiseEqual(NegativeOne, SubtractionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedSubtraction(-MinNormal, PositiveOne));
            AssertBitwiseEqual(NegativeOne, SubtractionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedSubtraction(-MaxSubnormal, PositiveOne));
            AssertBitwiseEqual(NegativeOne, SubtractionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedSubtraction(-NFloat.Epsilon, PositiveOne));
            AssertBitwiseEqual(NegativeOne, SubtractionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedSubtraction(NegativeZero, PositiveOne));
            AssertBitwiseEqual(NFloat.NaN, SubtractionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedSubtraction(NFloat.NaN, PositiveOne));
            AssertBitwiseEqual(NegativeOne, SubtractionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedSubtraction(PositiveZero, PositiveOne));
            AssertBitwiseEqual(NegativeOne, SubtractionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedSubtraction(NFloat.Epsilon, PositiveOne));
            AssertBitwiseEqual(NegativeOne, SubtractionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedSubtraction(MaxSubnormal, PositiveOne));
            AssertBitwiseEqual(NegativeOne, SubtractionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedSubtraction(MinNormal, PositiveOne));
            AssertBitwiseEqual(PositiveZero, SubtractionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedSubtraction(PositiveOne, PositiveOne));
            AssertBitwiseEqual(NFloat.MaxValue, SubtractionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedSubtraction(NFloat.MaxValue, PositiveOne));
            AssertBitwiseEqual(NFloat.PositiveInfinity, SubtractionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedSubtraction(NFloat.PositiveInfinity, PositiveOne));
        }

        [Fact]
        public static void op_UnaryNegationTest()
        {
            AssertBitwiseEqual(NFloat.PositiveInfinity, UnaryNegationOperatorsHelper<NFloat, NFloat>.op_UnaryNegation(NFloat.NegativeInfinity));
            AssertBitwiseEqual(NFloat.MaxValue, UnaryNegationOperatorsHelper<NFloat, NFloat>.op_UnaryNegation(NFloat.MinValue));
            AssertBitwiseEqual(PositiveOne, UnaryNegationOperatorsHelper<NFloat, NFloat>.op_UnaryNegation(NegativeOne));
            AssertBitwiseEqual(MinNormal, UnaryNegationOperatorsHelper<NFloat, NFloat>.op_UnaryNegation(-MinNormal));
            AssertBitwiseEqual(MaxSubnormal, UnaryNegationOperatorsHelper<NFloat, NFloat>.op_UnaryNegation(-MaxSubnormal));
            AssertBitwiseEqual(NFloat.Epsilon, UnaryNegationOperatorsHelper<NFloat, NFloat>.op_UnaryNegation(-NFloat.Epsilon));
            AssertBitwiseEqual(PositiveZero, UnaryNegationOperatorsHelper<NFloat, NFloat>.op_UnaryNegation(NegativeZero));
            AssertBitwiseEqual(NFloat.NaN, UnaryNegationOperatorsHelper<NFloat, NFloat>.op_UnaryNegation(NFloat.NaN));
            AssertBitwiseEqual(NegativeZero, UnaryNegationOperatorsHelper<NFloat, NFloat>.op_UnaryNegation(PositiveZero));
            AssertBitwiseEqual(-NFloat.Epsilon, UnaryNegationOperatorsHelper<NFloat, NFloat>.op_UnaryNegation(NFloat.Epsilon));
            AssertBitwiseEqual(-MaxSubnormal, UnaryNegationOperatorsHelper<NFloat, NFloat>.op_UnaryNegation(MaxSubnormal));
            AssertBitwiseEqual(-MinNormal, UnaryNegationOperatorsHelper<NFloat, NFloat>.op_UnaryNegation(MinNormal));
            AssertBitwiseEqual(NegativeOne, UnaryNegationOperatorsHelper<NFloat, NFloat>.op_UnaryNegation(PositiveOne));
            AssertBitwiseEqual(NFloat.MinValue, UnaryNegationOperatorsHelper<NFloat, NFloat>.op_UnaryNegation(NFloat.MaxValue));
            AssertBitwiseEqual(NFloat.NegativeInfinity, UnaryNegationOperatorsHelper<NFloat, NFloat>.op_UnaryNegation(NFloat.PositiveInfinity));
        }

        [Fact]
        public static void op_CheckedUnaryNegationTest()
        {
            AssertBitwiseEqual(NFloat.PositiveInfinity, UnaryNegationOperatorsHelper<NFloat, NFloat>.op_CheckedUnaryNegation(NFloat.NegativeInfinity));
            AssertBitwiseEqual(NFloat.MaxValue, UnaryNegationOperatorsHelper<NFloat, NFloat>.op_CheckedUnaryNegation(NFloat.MinValue));
            AssertBitwiseEqual(PositiveOne, UnaryNegationOperatorsHelper<NFloat, NFloat>.op_CheckedUnaryNegation(NegativeOne));
            AssertBitwiseEqual(MinNormal, UnaryNegationOperatorsHelper<NFloat, NFloat>.op_CheckedUnaryNegation(-MinNormal));
            AssertBitwiseEqual(MaxSubnormal, UnaryNegationOperatorsHelper<NFloat, NFloat>.op_CheckedUnaryNegation(-MaxSubnormal));
            AssertBitwiseEqual(NFloat.Epsilon, UnaryNegationOperatorsHelper<NFloat, NFloat>.op_CheckedUnaryNegation(-NFloat.Epsilon));
            AssertBitwiseEqual(PositiveZero, UnaryNegationOperatorsHelper<NFloat, NFloat>.op_CheckedUnaryNegation(NegativeZero));
            AssertBitwiseEqual(NFloat.NaN, UnaryNegationOperatorsHelper<NFloat, NFloat>.op_CheckedUnaryNegation(NFloat.NaN));
            AssertBitwiseEqual(NegativeZero, UnaryNegationOperatorsHelper<NFloat, NFloat>.op_CheckedUnaryNegation(PositiveZero));
            AssertBitwiseEqual(-NFloat.Epsilon, UnaryNegationOperatorsHelper<NFloat, NFloat>.op_CheckedUnaryNegation(NFloat.Epsilon));
            AssertBitwiseEqual(-MaxSubnormal, UnaryNegationOperatorsHelper<NFloat, NFloat>.op_CheckedUnaryNegation(MaxSubnormal));
            AssertBitwiseEqual(-MinNormal, UnaryNegationOperatorsHelper<NFloat, NFloat>.op_CheckedUnaryNegation(MinNormal));
            AssertBitwiseEqual(NegativeOne, UnaryNegationOperatorsHelper<NFloat, NFloat>.op_CheckedUnaryNegation(PositiveOne));
            AssertBitwiseEqual(NFloat.MinValue, UnaryNegationOperatorsHelper<NFloat, NFloat>.op_CheckedUnaryNegation(NFloat.MaxValue));
            AssertBitwiseEqual(NFloat.NegativeInfinity, UnaryNegationOperatorsHelper<NFloat, NFloat>.op_CheckedUnaryNegation(NFloat.PositiveInfinity));
        }

        [Fact]
        public static void op_UnaryPlusTest()
        {
            AssertBitwiseEqual(NFloat.NegativeInfinity, UnaryPlusOperatorsHelper<NFloat, NFloat>.op_UnaryPlus(NFloat.NegativeInfinity));
            AssertBitwiseEqual(NFloat.MinValue, UnaryPlusOperatorsHelper<NFloat, NFloat>.op_UnaryPlus(NFloat.MinValue));
            AssertBitwiseEqual(NegativeOne, UnaryPlusOperatorsHelper<NFloat, NFloat>.op_UnaryPlus(NegativeOne));
            AssertBitwiseEqual(-MinNormal, UnaryPlusOperatorsHelper<NFloat, NFloat>.op_UnaryPlus(-MinNormal));
            AssertBitwiseEqual(-MaxSubnormal, UnaryPlusOperatorsHelper<NFloat, NFloat>.op_UnaryPlus(-MaxSubnormal));
            AssertBitwiseEqual(-NFloat.Epsilon, UnaryPlusOperatorsHelper<NFloat, NFloat>.op_UnaryPlus(-NFloat.Epsilon));
            AssertBitwiseEqual(NegativeZero, UnaryPlusOperatorsHelper<NFloat, NFloat>.op_UnaryPlus(NegativeZero));
            AssertBitwiseEqual(NFloat.NaN, UnaryPlusOperatorsHelper<NFloat, NFloat>.op_UnaryPlus(NFloat.NaN));
            AssertBitwiseEqual(PositiveZero, UnaryPlusOperatorsHelper<NFloat, NFloat>.op_UnaryPlus(PositiveZero));
            AssertBitwiseEqual(NFloat.Epsilon, UnaryPlusOperatorsHelper<NFloat, NFloat>.op_UnaryPlus(NFloat.Epsilon));
            AssertBitwiseEqual(MaxSubnormal, UnaryPlusOperatorsHelper<NFloat, NFloat>.op_UnaryPlus(MaxSubnormal));
            AssertBitwiseEqual(MinNormal, UnaryPlusOperatorsHelper<NFloat, NFloat>.op_UnaryPlus(MinNormal));
            AssertBitwiseEqual(PositiveOne, UnaryPlusOperatorsHelper<NFloat, NFloat>.op_UnaryPlus(PositiveOne));
            AssertBitwiseEqual(NFloat.MaxValue, UnaryPlusOperatorsHelper<NFloat, NFloat>.op_UnaryPlus(NFloat.MaxValue));
            AssertBitwiseEqual(NFloat.PositiveInfinity, UnaryPlusOperatorsHelper<NFloat, NFloat>.op_UnaryPlus(NFloat.PositiveInfinity));
        }
    }
}
