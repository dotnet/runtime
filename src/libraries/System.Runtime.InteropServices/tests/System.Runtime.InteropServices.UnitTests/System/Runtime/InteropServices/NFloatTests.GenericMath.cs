// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Tests;
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

        private static NFloat One
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

        private static NFloat Two
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

        private static NFloat Zero
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

        //
        // IAdditionOperators
        //

        [Fact]
        public static void op_AdditionTest()
        {
            AssertBitwiseEqual(NFloat.NegativeInfinity, AdditionOperatorsHelper<NFloat, NFloat, NFloat>.op_Addition(NFloat.NegativeInfinity, One));
            AssertBitwiseEqual(NFloat.MinValue, AdditionOperatorsHelper<NFloat, NFloat, NFloat>.op_Addition(NFloat.MinValue, One));
            AssertBitwiseEqual(Zero, AdditionOperatorsHelper<NFloat, NFloat, NFloat>.op_Addition(NegativeOne, One));
            AssertBitwiseEqual(One, AdditionOperatorsHelper<NFloat, NFloat, NFloat>.op_Addition(-MinNormal, One));
            AssertBitwiseEqual(One, AdditionOperatorsHelper<NFloat, NFloat, NFloat>.op_Addition(-MaxSubnormal, One));
            AssertBitwiseEqual(One, AdditionOperatorsHelper<NFloat, NFloat, NFloat>.op_Addition(-NFloat.Epsilon, One));
            AssertBitwiseEqual(One, AdditionOperatorsHelper<NFloat, NFloat, NFloat>.op_Addition(NegativeZero, One));
            AssertBitwiseEqual(NFloat.NaN, AdditionOperatorsHelper<NFloat, NFloat, NFloat>.op_Addition(NFloat.NaN, One));
            AssertBitwiseEqual(One, AdditionOperatorsHelper<NFloat, NFloat, NFloat>.op_Addition(Zero, One));
            AssertBitwiseEqual(One, AdditionOperatorsHelper<NFloat, NFloat, NFloat>.op_Addition(NFloat.Epsilon, One));
            AssertBitwiseEqual(One, AdditionOperatorsHelper<NFloat, NFloat, NFloat>.op_Addition(MaxSubnormal, One));
            AssertBitwiseEqual(One, AdditionOperatorsHelper<NFloat, NFloat, NFloat>.op_Addition(MinNormal, One));
            AssertBitwiseEqual(Two, AdditionOperatorsHelper<NFloat, NFloat, NFloat>.op_Addition(One, One));
            AssertBitwiseEqual(NFloat.MaxValue, AdditionOperatorsHelper<NFloat, NFloat, NFloat>.op_Addition(NFloat.MaxValue, One));
            AssertBitwiseEqual(NFloat.PositiveInfinity, AdditionOperatorsHelper<NFloat, NFloat, NFloat>.op_Addition(NFloat.PositiveInfinity, One));
        }

        [Fact]
        public static void op_CheckedAdditionTest()
        {
            AssertBitwiseEqual(NFloat.NegativeInfinity, AdditionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedAddition(NFloat.NegativeInfinity, One));
            AssertBitwiseEqual(NFloat.MinValue, AdditionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedAddition(NFloat.MinValue, One));
            AssertBitwiseEqual(Zero, AdditionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedAddition(NegativeOne, One));
            AssertBitwiseEqual(One, AdditionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedAddition(-MinNormal, One));
            AssertBitwiseEqual(One, AdditionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedAddition(-MaxSubnormal, One));
            AssertBitwiseEqual(One, AdditionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedAddition(-NFloat.Epsilon, One));
            AssertBitwiseEqual(One, AdditionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedAddition(NegativeZero, One));
            AssertBitwiseEqual(NFloat.NaN, AdditionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedAddition(NFloat.NaN, One));
            AssertBitwiseEqual(One, AdditionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedAddition(Zero, One));
            AssertBitwiseEqual(One, AdditionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedAddition(NFloat.Epsilon, One));
            AssertBitwiseEqual(One, AdditionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedAddition(MaxSubnormal, One));
            AssertBitwiseEqual(One, AdditionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedAddition(MinNormal, One));
            AssertBitwiseEqual(Two, AdditionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedAddition(One, One));
            AssertBitwiseEqual(NFloat.MaxValue, AdditionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedAddition(NFloat.MaxValue, One));
            AssertBitwiseEqual(NFloat.PositiveInfinity, AdditionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedAddition(NFloat.PositiveInfinity, One));
        }

        //
        // IAdditiveIdentity
        //

        [Fact]
        public static void AdditiveIdentityTest()
        {
            AssertBitwiseEqual(Zero, AdditiveIdentityHelper<NFloat, NFloat>.AdditiveIdentity);
        }

        //
        // IBinaryNumber
        //

        [Fact]
        public static void AllBitsSetTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(0xFFFF_FFFF_FFFF_FFFF, BitConverter.DoubleToUInt64Bits((double)BinaryNumberHelper<NFloat>.AllBitsSet));
                Assert.Equal(0UL, ~BitConverter.DoubleToUInt64Bits((double)BinaryNumberHelper<NFloat>.AllBitsSet));
            }
            else
            {
                Assert.Equal(0xFFFF_FFFF, BitConverter.SingleToUInt32Bits((float)BinaryNumberHelper<NFloat>.AllBitsSet));
                Assert.Equal(0U, ~BitConverter.SingleToUInt32Bits((float)BinaryNumberHelper<NFloat>.AllBitsSet));
            }
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
            Assert.False(BinaryNumberHelper<NFloat>.IsPow2(Zero));
            Assert.False(BinaryNumberHelper<NFloat>.IsPow2(NFloat.Epsilon));
            Assert.False(BinaryNumberHelper<NFloat>.IsPow2(MaxSubnormal));
            Assert.True(BinaryNumberHelper<NFloat>.IsPow2(MinNormal));
            Assert.True(BinaryNumberHelper<NFloat>.IsPow2(One));
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
            AssertBitwiseEqual(NFloat.NegativeInfinity, BinaryNumberHelper<NFloat>.Log2(Zero));
            AssertBitwiseEqual(Zero, BinaryNumberHelper<NFloat>.Log2(One));
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

        //
        // IComparisonOperators
        //

        [Fact]
        public static void op_GreaterThanTest()
        {
            Assert.False(ComparisonOperatorsHelper<NFloat, NFloat, bool>.op_GreaterThan(NFloat.NegativeInfinity, One));
            Assert.False(ComparisonOperatorsHelper<NFloat, NFloat, bool>.op_GreaterThan(NFloat.MinValue, One));
            Assert.False(ComparisonOperatorsHelper<NFloat, NFloat, bool>.op_GreaterThan(NegativeOne, One));
            Assert.False(ComparisonOperatorsHelper<NFloat, NFloat, bool>.op_GreaterThan(-MinNormal, One));
            Assert.False(ComparisonOperatorsHelper<NFloat, NFloat, bool>.op_GreaterThan(-MaxSubnormal, One));
            Assert.False(ComparisonOperatorsHelper<NFloat, NFloat, bool>.op_GreaterThan(-NFloat.Epsilon, One));
            Assert.False(ComparisonOperatorsHelper<NFloat, NFloat, bool>.op_GreaterThan(NegativeZero, One));
            Assert.False(ComparisonOperatorsHelper<NFloat, NFloat, bool>.op_GreaterThan(NFloat.NaN, One));
            Assert.False(ComparisonOperatorsHelper<NFloat, NFloat, bool>.op_GreaterThan(Zero, One));
            Assert.False(ComparisonOperatorsHelper<NFloat, NFloat, bool>.op_GreaterThan(NFloat.Epsilon, One));
            Assert.False(ComparisonOperatorsHelper<NFloat, NFloat, bool>.op_GreaterThan(MaxSubnormal, One));
            Assert.False(ComparisonOperatorsHelper<NFloat, NFloat, bool>.op_GreaterThan(MinNormal, One));
            Assert.False(ComparisonOperatorsHelper<NFloat, NFloat, bool>.op_GreaterThan(One, One));
            Assert.True(ComparisonOperatorsHelper<NFloat, NFloat, bool>.op_GreaterThan(NFloat.MaxValue, One));
            Assert.True(ComparisonOperatorsHelper<NFloat, NFloat, bool>.op_GreaterThan(NFloat.PositiveInfinity, One));
        }

        [Fact]
        public static void op_GreaterThanOrEqualTest()
        {
            Assert.False(ComparisonOperatorsHelper<NFloat, NFloat, bool>.op_GreaterThanOrEqual(NFloat.NegativeInfinity, One));
            Assert.False(ComparisonOperatorsHelper<NFloat, NFloat, bool>.op_GreaterThanOrEqual(NFloat.MinValue, One));
            Assert.False(ComparisonOperatorsHelper<NFloat, NFloat, bool>.op_GreaterThanOrEqual(NegativeOne, One));
            Assert.False(ComparisonOperatorsHelper<NFloat, NFloat, bool>.op_GreaterThanOrEqual(-MinNormal, One));
            Assert.False(ComparisonOperatorsHelper<NFloat, NFloat, bool>.op_GreaterThanOrEqual(-MaxSubnormal, One));
            Assert.False(ComparisonOperatorsHelper<NFloat, NFloat, bool>.op_GreaterThanOrEqual(-NFloat.Epsilon, One));
            Assert.False(ComparisonOperatorsHelper<NFloat, NFloat, bool>.op_GreaterThanOrEqual(NegativeZero, One));
            Assert.False(ComparisonOperatorsHelper<NFloat, NFloat, bool>.op_GreaterThanOrEqual(NFloat.NaN, One));
            Assert.False(ComparisonOperatorsHelper<NFloat, NFloat, bool>.op_GreaterThanOrEqual(Zero, One));
            Assert.False(ComparisonOperatorsHelper<NFloat, NFloat, bool>.op_GreaterThanOrEqual(NFloat.Epsilon, One));
            Assert.False(ComparisonOperatorsHelper<NFloat, NFloat, bool>.op_GreaterThanOrEqual(MaxSubnormal, One));
            Assert.False(ComparisonOperatorsHelper<NFloat, NFloat, bool>.op_GreaterThanOrEqual(MinNormal, One));
            Assert.True(ComparisonOperatorsHelper<NFloat, NFloat, bool>.op_GreaterThanOrEqual(One, One));
            Assert.True(ComparisonOperatorsHelper<NFloat, NFloat, bool>.op_GreaterThanOrEqual(NFloat.MaxValue, One));
            Assert.True(ComparisonOperatorsHelper<NFloat, NFloat, bool>.op_GreaterThanOrEqual(NFloat.PositiveInfinity, One));
        }

        [Fact]
        public static void op_LessThanTest()
        {
            Assert.True(ComparisonOperatorsHelper<NFloat, NFloat, bool>.op_LessThan(NFloat.NegativeInfinity, One));
            Assert.True(ComparisonOperatorsHelper<NFloat, NFloat, bool>.op_LessThan(NFloat.MinValue, One));
            Assert.True(ComparisonOperatorsHelper<NFloat, NFloat, bool>.op_LessThan(NegativeOne, One));
            Assert.True(ComparisonOperatorsHelper<NFloat, NFloat, bool>.op_LessThan(-MinNormal, One));
            Assert.True(ComparisonOperatorsHelper<NFloat, NFloat, bool>.op_LessThan(-MaxSubnormal, One));
            Assert.True(ComparisonOperatorsHelper<NFloat, NFloat, bool>.op_LessThan(-NFloat.Epsilon, One));
            Assert.True(ComparisonOperatorsHelper<NFloat, NFloat, bool>.op_LessThan(NegativeZero, One));
            Assert.False(ComparisonOperatorsHelper<NFloat, NFloat, bool>.op_LessThan(NFloat.NaN, One));
            Assert.True(ComparisonOperatorsHelper<NFloat, NFloat, bool>.op_LessThan(Zero, One));
            Assert.True(ComparisonOperatorsHelper<NFloat, NFloat, bool>.op_LessThan(NFloat.Epsilon, One));
            Assert.True(ComparisonOperatorsHelper<NFloat, NFloat, bool>.op_LessThan(MaxSubnormal, One));
            Assert.True(ComparisonOperatorsHelper<NFloat, NFloat, bool>.op_LessThan(MinNormal, One));
            Assert.False(ComparisonOperatorsHelper<NFloat, NFloat, bool>.op_LessThan(One, One));
            Assert.False(ComparisonOperatorsHelper<NFloat, NFloat, bool>.op_LessThan(NFloat.MaxValue, One));
            Assert.False(ComparisonOperatorsHelper<NFloat, NFloat, bool>.op_LessThan(NFloat.PositiveInfinity, One));
        }

        [Fact]
        public static void op_LessThanOrEqualTest()
        {
            Assert.True(ComparisonOperatorsHelper<NFloat, NFloat, bool>.op_LessThanOrEqual(NFloat.NegativeInfinity, One));
            Assert.True(ComparisonOperatorsHelper<NFloat, NFloat, bool>.op_LessThanOrEqual(NFloat.MinValue, One));
            Assert.True(ComparisonOperatorsHelper<NFloat, NFloat, bool>.op_LessThanOrEqual(NegativeOne, One));
            Assert.True(ComparisonOperatorsHelper<NFloat, NFloat, bool>.op_LessThanOrEqual(-MinNormal, One));
            Assert.True(ComparisonOperatorsHelper<NFloat, NFloat, bool>.op_LessThanOrEqual(-MaxSubnormal, One));
            Assert.True(ComparisonOperatorsHelper<NFloat, NFloat, bool>.op_LessThanOrEqual(-NFloat.Epsilon, One));
            Assert.True(ComparisonOperatorsHelper<NFloat, NFloat, bool>.op_LessThanOrEqual(NegativeZero, One));
            Assert.False(ComparisonOperatorsHelper<NFloat, NFloat, bool>.op_LessThanOrEqual(NFloat.NaN, One));
            Assert.True(ComparisonOperatorsHelper<NFloat, NFloat, bool>.op_LessThanOrEqual(Zero, One));
            Assert.True(ComparisonOperatorsHelper<NFloat, NFloat, bool>.op_LessThanOrEqual(NFloat.Epsilon, One));
            Assert.True(ComparisonOperatorsHelper<NFloat, NFloat, bool>.op_LessThanOrEqual(MaxSubnormal, One));
            Assert.True(ComparisonOperatorsHelper<NFloat, NFloat, bool>.op_LessThanOrEqual(MinNormal, One));
            Assert.True(ComparisonOperatorsHelper<NFloat, NFloat, bool>.op_LessThanOrEqual(One, One));
            Assert.False(ComparisonOperatorsHelper<NFloat, NFloat, bool>.op_LessThanOrEqual(NFloat.MaxValue, One));
            Assert.False(ComparisonOperatorsHelper<NFloat, NFloat, bool>.op_LessThanOrEqual(NFloat.PositiveInfinity, One));
        }

        //
        // IDecrementOperators
        //

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
            AssertBitwiseEqual(NegativeOne, DecrementOperatorsHelper<NFloat>.op_Decrement(Zero));
            AssertBitwiseEqual(NegativeOne, DecrementOperatorsHelper<NFloat>.op_Decrement(NFloat.Epsilon));
            AssertBitwiseEqual(NegativeOne, DecrementOperatorsHelper<NFloat>.op_Decrement(MaxSubnormal));
            AssertBitwiseEqual(NegativeOne, DecrementOperatorsHelper<NFloat>.op_Decrement(MinNormal));
            AssertBitwiseEqual(Zero, DecrementOperatorsHelper<NFloat>.op_Decrement(One));
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
            AssertBitwiseEqual(NegativeOne, DecrementOperatorsHelper<NFloat>.op_CheckedDecrement(Zero));
            AssertBitwiseEqual(NegativeOne, DecrementOperatorsHelper<NFloat>.op_CheckedDecrement(NFloat.Epsilon));
            AssertBitwiseEqual(NegativeOne, DecrementOperatorsHelper<NFloat>.op_CheckedDecrement(MaxSubnormal));
            AssertBitwiseEqual(NegativeOne, DecrementOperatorsHelper<NFloat>.op_CheckedDecrement(MinNormal));
            AssertBitwiseEqual(Zero, DecrementOperatorsHelper<NFloat>.op_CheckedDecrement(One));
            AssertBitwiseEqual(NFloat.MaxValue, DecrementOperatorsHelper<NFloat>.op_CheckedDecrement(NFloat.MaxValue));
            AssertBitwiseEqual(NFloat.PositiveInfinity, DecrementOperatorsHelper<NFloat>.op_CheckedDecrement(NFloat.PositiveInfinity));
        }

        //
        // IDivisionOperators
        //

        [Fact]
        public static void op_DivisionTest()
        {
            AssertBitwiseEqual(NFloat.NegativeInfinity, DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_Division(NFloat.NegativeInfinity, Two));
            AssertBitwiseEqual((NFloat)(-0.5f), DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_Division(NegativeOne, Two));
            AssertBitwiseEqual(NegativeZero, DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_Division(-NFloat.Epsilon, Two));
            AssertBitwiseEqual(NegativeZero, DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_Division(NegativeZero, Two));
            AssertBitwiseEqual(NFloat.NaN, DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_Division(NFloat.NaN, Two));
            AssertBitwiseEqual(Zero, DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_Division(Zero, Two));
            AssertBitwiseEqual(Zero, DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_Division(NFloat.Epsilon, Two));
            AssertBitwiseEqual((NFloat)0.5f, DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_Division(One, Two));
            AssertBitwiseEqual(NFloat.PositiveInfinity, DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_Division(NFloat.PositiveInfinity, Two));

            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual((NFloat)(-8.9884656743115785E+307), DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_Division(NFloat.MinValue, Two));
                AssertBitwiseEqual((NFloat)(-1.1125369292536007E-308), DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_Division(-MinNormal, Two));
                AssertBitwiseEqual((NFloat)(-1.1125369292536007E-308), DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_Division(-MaxSubnormal, Two));
                AssertBitwiseEqual((NFloat)1.1125369292536007E-308, DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_Division(MaxSubnormal, Two));
                AssertBitwiseEqual((NFloat)1.1125369292536007E-308, DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_Division(MinNormal, Two));
                AssertBitwiseEqual((NFloat)8.9884656743115785E+307, DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_Division(NFloat.MaxValue, Two));
            }
            else
            {
                AssertBitwiseEqual((NFloat)(-1.70141173E+38f), DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_Division(NFloat.MinValue, Two));
                AssertBitwiseEqual((NFloat)(-5.87747175E-39f), DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_Division(-MinNormal, Two));
                AssertBitwiseEqual((NFloat)(-5.87747175E-39f), DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_Division(-MaxSubnormal, Two));
                AssertBitwiseEqual((NFloat)5.87747175E-39f, DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_Division(MaxSubnormal, Two));
                AssertBitwiseEqual((NFloat)5.87747175E-39f, DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_Division(MinNormal, Two));
                AssertBitwiseEqual((NFloat)1.70141173E+38f, DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_Division(NFloat.MaxValue, Two));
            }
        }

        [Fact]
        public static void op_CheckedDivisionTest()
        {
            AssertBitwiseEqual(NFloat.NegativeInfinity, DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedDivision(NFloat.NegativeInfinity, Two));
            AssertBitwiseEqual((NFloat)(-0.5f), DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedDivision(NegativeOne, Two));
            AssertBitwiseEqual(NegativeZero, DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedDivision(-NFloat.Epsilon, Two));
            AssertBitwiseEqual(NegativeZero, DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedDivision(NegativeZero, Two));
            AssertBitwiseEqual(NFloat.NaN, DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedDivision(NFloat.NaN, Two));
            AssertBitwiseEqual(Zero, DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedDivision(Zero, Two));
            AssertBitwiseEqual(Zero, DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedDivision(NFloat.Epsilon, Two));
            AssertBitwiseEqual((NFloat)0.5f, DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedDivision(One, Two));
            AssertBitwiseEqual(NFloat.PositiveInfinity, DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedDivision(NFloat.PositiveInfinity, Two));

            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual((NFloat)(-8.9884656743115785E+307), DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedDivision(NFloat.MinValue, Two));
                AssertBitwiseEqual((NFloat)(-1.1125369292536007E-308), DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedDivision(-MinNormal, Two));
                AssertBitwiseEqual((NFloat)(-1.1125369292536007E-308), DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedDivision(-MaxSubnormal, Two));
                AssertBitwiseEqual((NFloat)1.1125369292536007E-308, DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedDivision(MaxSubnormal, Two));
                AssertBitwiseEqual((NFloat)1.1125369292536007E-308, DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedDivision(MinNormal, Two));
                AssertBitwiseEqual((NFloat)8.9884656743115785E+307, DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedDivision(NFloat.MaxValue, Two));
            }
            else
            {
                AssertBitwiseEqual((NFloat)(-1.70141173E+38f), DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedDivision(NFloat.MinValue, Two));
                AssertBitwiseEqual((NFloat)(-5.87747175E-39f), DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedDivision(-MinNormal, Two));
                AssertBitwiseEqual((NFloat)(-5.87747175E-39f), DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedDivision(-MaxSubnormal, Two));
                AssertBitwiseEqual((NFloat)5.87747175E-39f, DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedDivision(MaxSubnormal, Two));
                AssertBitwiseEqual((NFloat)5.87747175E-39f, DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedDivision(MinNormal, Two));
                AssertBitwiseEqual((NFloat)1.70141173E+38f, DivisionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedDivision(NFloat.MaxValue, Two));
            }
        }

        //
        // IEqualityOperators
        //

        [Fact]
        public static void op_EqualityTest()
        {
            Assert.False(EqualityOperatorsHelper<NFloat, NFloat, bool>.op_Equality(NFloat.NegativeInfinity, One));
            Assert.False(EqualityOperatorsHelper<NFloat, NFloat, bool>.op_Equality(NFloat.MinValue, One));
            Assert.False(EqualityOperatorsHelper<NFloat, NFloat, bool>.op_Equality(NegativeOne, One));
            Assert.False(EqualityOperatorsHelper<NFloat, NFloat, bool>.op_Equality(-MinNormal, One));
            Assert.False(EqualityOperatorsHelper<NFloat, NFloat, bool>.op_Equality(-MaxSubnormal, One));
            Assert.False(EqualityOperatorsHelper<NFloat, NFloat, bool>.op_Equality(-NFloat.Epsilon, One));
            Assert.False(EqualityOperatorsHelper<NFloat, NFloat, bool>.op_Equality(NegativeZero, One));
            Assert.False(EqualityOperatorsHelper<NFloat, NFloat, bool>.op_Equality(NFloat.NaN, One));
            Assert.False(EqualityOperatorsHelper<NFloat, NFloat, bool>.op_Equality(Zero, One));
            Assert.False(EqualityOperatorsHelper<NFloat, NFloat, bool>.op_Equality(NFloat.Epsilon, One));
            Assert.False(EqualityOperatorsHelper<NFloat, NFloat, bool>.op_Equality(MaxSubnormal, One));
            Assert.False(EqualityOperatorsHelper<NFloat, NFloat, bool>.op_Equality(MinNormal, One));
            Assert.True(EqualityOperatorsHelper<NFloat, NFloat, bool>.op_Equality(One, One));
            Assert.False(EqualityOperatorsHelper<NFloat, NFloat, bool>.op_Equality(NFloat.MaxValue, One));
            Assert.False(EqualityOperatorsHelper<NFloat, NFloat, bool>.op_Equality(NFloat.PositiveInfinity, One));
        }

        [Fact]
        public static void op_InequalityTest()
        {
            Assert.True(EqualityOperatorsHelper<NFloat, NFloat, bool>.op_Inequality(NFloat.NegativeInfinity, One));
            Assert.True(EqualityOperatorsHelper<NFloat, NFloat, bool>.op_Inequality(NFloat.MinValue, One));
            Assert.True(EqualityOperatorsHelper<NFloat, NFloat, bool>.op_Inequality(NegativeOne, One));
            Assert.True(EqualityOperatorsHelper<NFloat, NFloat, bool>.op_Inequality(-MinNormal, One));
            Assert.True(EqualityOperatorsHelper<NFloat, NFloat, bool>.op_Inequality(-MaxSubnormal, One));
            Assert.True(EqualityOperatorsHelper<NFloat, NFloat, bool>.op_Inequality(-NFloat.Epsilon, One));
            Assert.True(EqualityOperatorsHelper<NFloat, NFloat, bool>.op_Inequality(NegativeZero, One));
            Assert.True(EqualityOperatorsHelper<NFloat, NFloat, bool>.op_Inequality(NFloat.NaN, One));
            Assert.True(EqualityOperatorsHelper<NFloat, NFloat, bool>.op_Inequality(Zero, One));
            Assert.True(EqualityOperatorsHelper<NFloat, NFloat, bool>.op_Inequality(NFloat.Epsilon, One));
            Assert.True(EqualityOperatorsHelper<NFloat, NFloat, bool>.op_Inequality(MaxSubnormal, One));
            Assert.True(EqualityOperatorsHelper<NFloat, NFloat, bool>.op_Inequality(MinNormal, One));
            Assert.False(EqualityOperatorsHelper<NFloat, NFloat, bool>.op_Inequality(One, One));
            Assert.True(EqualityOperatorsHelper<NFloat, NFloat, bool>.op_Inequality(NFloat.MaxValue, One));
            Assert.True(EqualityOperatorsHelper<NFloat, NFloat, bool>.op_Inequality(NFloat.PositiveInfinity, One));
        }

        //
        // IFloatingPoint
        //

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
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetExponentByteCount(Zero));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetExponentByteCount(NFloat.Epsilon));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetExponentByteCount(MaxSubnormal));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetExponentByteCount(MinNormal));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetExponentByteCount(One));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetExponentByteCount(NFloat.MaxValue));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetExponentByteCount(NFloat.PositiveInfinity));
        }

        [Fact]
        public static void GetExponentShortestBitLengthTest()
        {
            int expected = Environment.Is64BitProcess ? 11 : 8;

            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetExponentShortestBitLength(NFloat.NegativeInfinity));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetExponentShortestBitLength(-MinNormal));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetExponentShortestBitLength(-MaxSubnormal));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetExponentShortestBitLength(-NFloat.Epsilon));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetExponentShortestBitLength(NegativeZero));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetExponentShortestBitLength(NFloat.NaN));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetExponentShortestBitLength(Zero));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetExponentShortestBitLength(NFloat.Epsilon));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetExponentShortestBitLength(MaxSubnormal));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetExponentShortestBitLength(MinNormal));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetExponentShortestBitLength(NFloat.PositiveInfinity));

            expected = Environment.Is64BitProcess ? 10 : 7;

            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetExponentShortestBitLength(NFloat.MinValue));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetExponentShortestBitLength(NFloat.MaxValue));

            expected = 0;

            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetExponentShortestBitLength(NegativeOne));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetExponentShortestBitLength(One));
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
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetSignificandByteCount(Zero));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetSignificandByteCount(NFloat.Epsilon));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetSignificandByteCount(MaxSubnormal));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetSignificandByteCount(MinNormal));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetSignificandByteCount(One));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetSignificandByteCount(NFloat.MaxValue));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetSignificandByteCount(NFloat.PositiveInfinity));
        }

        [Fact]
        public static void GetSignificandBitLengthTest()
        {
            int expected = Environment.Is64BitProcess ? 53 : 24;

            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetSignificandBitLength(NFloat.NegativeInfinity));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetSignificandBitLength(NFloat.MinValue));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetSignificandBitLength(NegativeOne));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetSignificandBitLength(-MinNormal));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetSignificandBitLength(-MaxSubnormal));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetSignificandBitLength(-NFloat.Epsilon));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetSignificandBitLength(NegativeZero));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetSignificandBitLength(NFloat.NaN));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetSignificandBitLength(Zero));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetSignificandBitLength(NFloat.Epsilon));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetSignificandBitLength(MaxSubnormal));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetSignificandBitLength(MinNormal));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetSignificandBitLength(One));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetSignificandBitLength(NFloat.MaxValue));
            Assert.Equal(expected, FloatingPointHelper<NFloat>.GetSignificandBitLength(NFloat.PositiveInfinity));
        }

        [Fact]
        public static void TryWriteExponentBigEndianTest()
        {
            if (Environment.Is64BitProcess)
            {
                Span<byte> destination = stackalloc byte[2];
                int bytesWritten = 0;

                Assert.True(FloatingPointHelper<NFloat>.TryWriteExponentBigEndian(NFloat.NegativeInfinity, destination, out bytesWritten));
                Assert.Equal(2, bytesWritten);
                Assert.Equal(new byte[] { 0x04, 0x00 }, destination.ToArray()); // +1024

                Assert.True(FloatingPointHelper<NFloat>.TryWriteExponentBigEndian(NFloat.MinValue, destination, out bytesWritten));
                Assert.Equal(2, bytesWritten);
                Assert.Equal(new byte[] { 0x03, 0xFF }, destination.ToArray()); // +1023

                Assert.True(FloatingPointHelper<NFloat>.TryWriteExponentBigEndian(NegativeOne, destination, out bytesWritten));
                Assert.Equal(2, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x00 }, destination.ToArray()); // +0

                Assert.True(FloatingPointHelper<NFloat>.TryWriteExponentBigEndian(-MinNormal, destination, out bytesWritten));
                Assert.Equal(2, bytesWritten);
                Assert.Equal(new byte[] { 0xFC, 0x02 }, destination.ToArray()); // -1022

                Assert.True(FloatingPointHelper<NFloat>.TryWriteExponentBigEndian(-MaxSubnormal, destination, out bytesWritten));
                Assert.Equal(2, bytesWritten);
                Assert.Equal(new byte[] { 0xFC, 0x01 }, destination.ToArray()); // -1023

                Assert.True(FloatingPointHelper<NFloat>.TryWriteExponentBigEndian(-NFloat.Epsilon, destination, out bytesWritten));
                Assert.Equal(2, bytesWritten);
                Assert.Equal(new byte[] { 0xFC, 0x01 }, destination.ToArray()); // -1023

                Assert.True(FloatingPointHelper<NFloat>.TryWriteExponentBigEndian(NegativeZero, destination, out bytesWritten));
                Assert.Equal(2, bytesWritten);
                Assert.Equal(new byte[] { 0xFC, 0x01 }, destination.ToArray()); // -1023

                Assert.True(FloatingPointHelper<NFloat>.TryWriteExponentBigEndian(NFloat.NaN, destination, out bytesWritten));
                Assert.Equal(2, bytesWritten);
                Assert.Equal(new byte[] { 0x04, 0x00 }, destination.ToArray()); // +1024

                Assert.True(FloatingPointHelper<NFloat>.TryWriteExponentBigEndian(Zero, destination, out bytesWritten));
                Assert.Equal(2, bytesWritten);
                Assert.Equal(new byte[] { 0xFC, 0x01 }, destination.ToArray()); // -1023

                Assert.True(FloatingPointHelper<NFloat>.TryWriteExponentBigEndian(NFloat.Epsilon, destination, out bytesWritten));
                Assert.Equal(2, bytesWritten);
                Assert.Equal(new byte[] { 0xFC, 0x01 }, destination.ToArray()); // -1023

                Assert.True(FloatingPointHelper<NFloat>.TryWriteExponentBigEndian(MaxSubnormal, destination, out bytesWritten));
                Assert.Equal(2, bytesWritten);
                Assert.Equal(new byte[] { 0xFC, 0x01 }, destination.ToArray()); // -1023

                Assert.True(FloatingPointHelper<NFloat>.TryWriteExponentBigEndian(MinNormal, destination, out bytesWritten));
                Assert.Equal(2, bytesWritten);
                Assert.Equal(new byte[] { 0xFC, 0x02 }, destination.ToArray()); // -1022

                Assert.True(FloatingPointHelper<NFloat>.TryWriteExponentBigEndian(One, destination, out bytesWritten));
                Assert.Equal(2, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x00 }, destination.ToArray()); // +0

                Assert.True(FloatingPointHelper<NFloat>.TryWriteExponentBigEndian(NFloat.MaxValue, destination, out bytesWritten));
                Assert.Equal(2, bytesWritten);
                Assert.Equal(new byte[] { 0x03, 0xFF }, destination.ToArray()); // +1023

                Assert.True(FloatingPointHelper<NFloat>.TryWriteExponentBigEndian(NFloat.PositiveInfinity, destination, out bytesWritten));
                Assert.Equal(2, bytesWritten);
                Assert.Equal(new byte[] { 0x04, 0x00 }, destination.ToArray()); // +1024

                Assert.False(FloatingPointHelper<NFloat>.TryWriteExponentBigEndian(default, Span<byte>.Empty, out bytesWritten));
                Assert.Equal(0, bytesWritten);
                Assert.Equal(new byte[] { 0x04, 0x00 }, destination.ToArray());
            }
            else
            {
                Span<byte> destination = stackalloc byte[1];
                int bytesWritten = 0;

                Assert.True(FloatingPointHelper<NFloat>.TryWriteExponentBigEndian(NFloat.NegativeInfinity, destination, out bytesWritten));
                Assert.Equal(1, bytesWritten);
                Assert.Equal(new byte[] { 0x80 }, destination.ToArray()); // -128

                Assert.True(FloatingPointHelper<NFloat>.TryWriteExponentBigEndian(NFloat.MinValue, destination, out bytesWritten));
                Assert.Equal(1, bytesWritten);
                Assert.Equal(new byte[] { 0x7F }, destination.ToArray()); // +127

                Assert.True(FloatingPointHelper<NFloat>.TryWriteExponentBigEndian(NegativeOne, destination, out bytesWritten));
                Assert.Equal(1, bytesWritten);
                Assert.Equal(new byte[] { 0x00 }, destination.ToArray()); // +0

                Assert.True(FloatingPointHelper<NFloat>.TryWriteExponentBigEndian(-MinNormal, destination, out bytesWritten));
                Assert.Equal(1, bytesWritten);
                Assert.Equal(new byte[] { 0x82 }, destination.ToArray()); // -126

                Assert.True(FloatingPointHelper<NFloat>.TryWriteExponentBigEndian(-MaxSubnormal, destination, out bytesWritten));
                Assert.Equal(1, bytesWritten);
                Assert.Equal(new byte[] { 0x81 }, destination.ToArray()); // -127

                Assert.True(FloatingPointHelper<NFloat>.TryWriteExponentBigEndian(-NFloat.Epsilon, destination, out bytesWritten));
                Assert.Equal(1, bytesWritten);
                Assert.Equal(new byte[] { 0x81 }, destination.ToArray()); // -127

                Assert.True(FloatingPointHelper<NFloat>.TryWriteExponentBigEndian(NegativeZero, destination, out bytesWritten));
                Assert.Equal(1, bytesWritten);
                Assert.Equal(new byte[] { 0x81 }, destination.ToArray()); // -127

                Assert.True(FloatingPointHelper<NFloat>.TryWriteExponentBigEndian(NFloat.NaN, destination, out bytesWritten));
                Assert.Equal(1, bytesWritten);
                Assert.Equal(new byte[] { 0x80 }, destination.ToArray()); // -128

                Assert.True(FloatingPointHelper<NFloat>.TryWriteExponentBigEndian(Zero, destination, out bytesWritten));
                Assert.Equal(1, bytesWritten);
                Assert.Equal(new byte[] { 0x81 }, destination.ToArray()); // -127

                Assert.True(FloatingPointHelper<NFloat>.TryWriteExponentBigEndian(NFloat.Epsilon, destination, out bytesWritten));
                Assert.Equal(1, bytesWritten);
                Assert.Equal(new byte[] { 0x81 }, destination.ToArray()); // -127

                Assert.True(FloatingPointHelper<NFloat>.TryWriteExponentBigEndian(MaxSubnormal, destination, out bytesWritten));
                Assert.Equal(1, bytesWritten);
                Assert.Equal(new byte[] { 0x81 }, destination.ToArray()); // -127

                Assert.True(FloatingPointHelper<NFloat>.TryWriteExponentBigEndian(MinNormal, destination, out bytesWritten));
                Assert.Equal(1, bytesWritten);
                Assert.Equal(new byte[] { 0x82 }, destination.ToArray()); // -126

                Assert.True(FloatingPointHelper<NFloat>.TryWriteExponentBigEndian(One, destination, out bytesWritten));
                Assert.Equal(1, bytesWritten);
                Assert.Equal(new byte[] { 0x00 }, destination.ToArray()); // +0

                Assert.True(FloatingPointHelper<NFloat>.TryWriteExponentBigEndian(NFloat.MaxValue, destination, out bytesWritten));
                Assert.Equal(1, bytesWritten);
                Assert.Equal(new byte[] { 0x7F }, destination.ToArray()); // +127

                Assert.True(FloatingPointHelper<NFloat>.TryWriteExponentBigEndian(NFloat.PositiveInfinity, destination, out bytesWritten));
                Assert.Equal(1, bytesWritten);
                Assert.Equal(new byte[] { 0x80 }, destination.ToArray()); // -128

                Assert.False(FloatingPointHelper<NFloat>.TryWriteExponentBigEndian(default, Span<byte>.Empty, out bytesWritten));
                Assert.Equal(0, bytesWritten);
                Assert.Equal(new byte[] { 0x80 }, destination.ToArray());
            }
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

                Assert.True(FloatingPointHelper<NFloat>.TryWriteExponentLittleEndian(Zero, destination, out bytesWritten));
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

                Assert.True(FloatingPointHelper<NFloat>.TryWriteExponentLittleEndian(One, destination, out bytesWritten));
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

                Assert.True(FloatingPointHelper<NFloat>.TryWriteExponentLittleEndian(Zero, destination, out bytesWritten));
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

                Assert.True(FloatingPointHelper<NFloat>.TryWriteExponentLittleEndian(One, destination, out bytesWritten));
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
        public static void TryWriteSignificandBigEndianTest()
        {
            if (Environment.Is64BitProcess)
            {
                Span<byte> destination = stackalloc byte[8];
                int bytesWritten = 0;

                Assert.True(FloatingPointHelper<NFloat>.TryWriteSignificandBigEndian(NFloat.NegativeInfinity, destination, out bytesWritten));
                Assert.Equal(8, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

                Assert.True(FloatingPointHelper<NFloat>.TryWriteSignificandBigEndian(NFloat.MinValue, destination, out bytesWritten));
                Assert.Equal(8, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x1F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, destination.ToArray());

                Assert.True(FloatingPointHelper<NFloat>.TryWriteSignificandBigEndian(NegativeOne, destination, out bytesWritten));
                Assert.Equal(8, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

                Assert.True(FloatingPointHelper<NFloat>.TryWriteSignificandBigEndian(-MinNormal, destination, out bytesWritten));
                Assert.Equal(8, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

                Assert.True(FloatingPointHelper<NFloat>.TryWriteSignificandBigEndian(-MaxSubnormal, destination, out bytesWritten));
                Assert.Equal(8, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x0F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, destination.ToArray());

                Assert.True(FloatingPointHelper<NFloat>.TryWriteSignificandBigEndian(-NFloat.Epsilon, destination, out bytesWritten));
                Assert.Equal(8, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, destination.ToArray());

                Assert.True(FloatingPointHelper<NFloat>.TryWriteSignificandBigEndian(NegativeZero, destination, out bytesWritten));
                Assert.Equal(8, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

                Assert.True(FloatingPointHelper<NFloat>.TryWriteSignificandBigEndian(NFloat.NaN, destination, out bytesWritten));
                Assert.Equal(8, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x18, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

                Assert.True(FloatingPointHelper<NFloat>.TryWriteSignificandBigEndian(Zero, destination, out bytesWritten));
                Assert.Equal(8, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

                Assert.True(FloatingPointHelper<NFloat>.TryWriteSignificandBigEndian(NFloat.Epsilon, destination, out bytesWritten));
                Assert.Equal(8, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, destination.ToArray());

                Assert.True(FloatingPointHelper<NFloat>.TryWriteSignificandBigEndian(MaxSubnormal, destination, out bytesWritten));
                Assert.Equal(8, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x0F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, destination.ToArray());

                Assert.True(FloatingPointHelper<NFloat>.TryWriteSignificandBigEndian(MinNormal, destination, out bytesWritten));
                Assert.Equal(8, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

                Assert.True(FloatingPointHelper<NFloat>.TryWriteSignificandBigEndian(One, destination, out bytesWritten));
                Assert.Equal(8, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

                Assert.True(FloatingPointHelper<NFloat>.TryWriteSignificandBigEndian(NFloat.MaxValue, destination, out bytesWritten));
                Assert.Equal(8, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x1F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, destination.ToArray());

                Assert.True(FloatingPointHelper<NFloat>.TryWriteSignificandBigEndian(NFloat.PositiveInfinity, destination, out bytesWritten));
                Assert.Equal(8, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

                Assert.False(FloatingPointHelper<NFloat>.TryWriteSignificandBigEndian(default, Span<byte>.Empty, out bytesWritten));
                Assert.Equal(0, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());
            }
            else
            {
                Span<byte> destination = stackalloc byte[4];
                int bytesWritten = 0;

                Assert.True(FloatingPointHelper<NFloat>.TryWriteSignificandBigEndian(NFloat.NegativeInfinity, destination, out bytesWritten));
                Assert.Equal(4, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x80, 0x00, 0x00 }, destination.ToArray());

                Assert.True(FloatingPointHelper<NFloat>.TryWriteSignificandBigEndian(NFloat.MinValue, destination, out bytesWritten));
                Assert.Equal(4, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0xFF, 0xFF, 0xFF }, destination.ToArray());

                Assert.True(FloatingPointHelper<NFloat>.TryWriteSignificandBigEndian(NegativeOne, destination, out bytesWritten));
                Assert.Equal(4, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x80, 0x00, 0x00 }, destination.ToArray());

                Assert.True(FloatingPointHelper<NFloat>.TryWriteSignificandBigEndian(-MinNormal, destination, out bytesWritten));
                Assert.Equal(4, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x80, 0x00, 0x00 }, destination.ToArray());

                Assert.True(FloatingPointHelper<NFloat>.TryWriteSignificandBigEndian(-MaxSubnormal, destination, out bytesWritten));
                Assert.Equal(4, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x7F, 0xFF, 0xFF }, destination.ToArray());

                Assert.True(FloatingPointHelper<NFloat>.TryWriteSignificandBigEndian(-NFloat.Epsilon, destination, out bytesWritten));
                Assert.Equal(4, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x01 }, destination.ToArray());

                Assert.True(FloatingPointHelper<NFloat>.TryWriteSignificandBigEndian(NegativeZero, destination, out bytesWritten));
                Assert.Equal(4, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

                Assert.True(FloatingPointHelper<NFloat>.TryWriteSignificandBigEndian(NFloat.NaN, destination, out bytesWritten));
                Assert.Equal(4, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0xC0, 0x00, 0x00 }, destination.ToArray());

                Assert.True(FloatingPointHelper<NFloat>.TryWriteSignificandBigEndian(Zero, destination, out bytesWritten));
                Assert.Equal(4, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

                Assert.True(FloatingPointHelper<NFloat>.TryWriteSignificandBigEndian(NFloat.Epsilon, destination, out bytesWritten));
                Assert.Equal(4, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x01 }, destination.ToArray());

                Assert.True(FloatingPointHelper<NFloat>.TryWriteSignificandBigEndian(MaxSubnormal, destination, out bytesWritten));
                Assert.Equal(4, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x7F, 0xFF, 0xFF }, destination.ToArray());

                Assert.True(FloatingPointHelper<NFloat>.TryWriteSignificandBigEndian(MinNormal, destination, out bytesWritten));
                Assert.Equal(4, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x80, 0x00, 0x00 }, destination.ToArray());

                Assert.True(FloatingPointHelper<NFloat>.TryWriteSignificandBigEndian(One, destination, out bytesWritten));
                Assert.Equal(4, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x80, 0x00, 0x00 }, destination.ToArray());

                Assert.True(FloatingPointHelper<NFloat>.TryWriteSignificandBigEndian(NFloat.MaxValue, destination, out bytesWritten));
                Assert.Equal(4, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0xFF, 0xFF, 0xFF }, destination.ToArray());

                Assert.True(FloatingPointHelper<NFloat>.TryWriteSignificandBigEndian(NFloat.PositiveInfinity, destination, out bytesWritten));
                Assert.Equal(4, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x80, 0x00, 0x00 }, destination.ToArray());

                Assert.False(FloatingPointHelper<NFloat>.TryWriteSignificandBigEndian(default, Span<byte>.Empty, out bytesWritten));
                Assert.Equal(0, bytesWritten);
                Assert.Equal(new byte[] { 0x00, 0x80, 0x00, 0x00 }, destination.ToArray());
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

                Assert.True(FloatingPointHelper<NFloat>.TryWriteSignificandLittleEndian(Zero, destination, out bytesWritten));
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

                Assert.True(FloatingPointHelper<NFloat>.TryWriteSignificandLittleEndian(One, destination, out bytesWritten));
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

                Assert.True(FloatingPointHelper<NFloat>.TryWriteSignificandLittleEndian(Zero, destination, out bytesWritten));
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

                Assert.True(FloatingPointHelper<NFloat>.TryWriteSignificandLittleEndian(One, destination, out bytesWritten));
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

        //
        // IIncrementOperators
        //

        [Fact]
        public static void op_IncrementTest()
        {
            AssertBitwiseEqual(NFloat.NegativeInfinity, IncrementOperatorsHelper<NFloat>.op_Increment(NFloat.NegativeInfinity));
            AssertBitwiseEqual(NFloat.MinValue, IncrementOperatorsHelper<NFloat>.op_Increment(NFloat.MinValue));
            AssertBitwiseEqual(Zero, IncrementOperatorsHelper<NFloat>.op_Increment(NegativeOne));
            AssertBitwiseEqual(One, IncrementOperatorsHelper<NFloat>.op_Increment(-MinNormal));
            AssertBitwiseEqual(One, IncrementOperatorsHelper<NFloat>.op_Increment(-MaxSubnormal));
            AssertBitwiseEqual(One, IncrementOperatorsHelper<NFloat>.op_Increment(-NFloat.Epsilon));
            AssertBitwiseEqual(One, IncrementOperatorsHelper<NFloat>.op_Increment(NegativeZero));
            AssertBitwiseEqual(NFloat.NaN, IncrementOperatorsHelper<NFloat>.op_Increment(NFloat.NaN));
            AssertBitwiseEqual(One, IncrementOperatorsHelper<NFloat>.op_Increment(Zero));
            AssertBitwiseEqual(One, IncrementOperatorsHelper<NFloat>.op_Increment(NFloat.Epsilon));
            AssertBitwiseEqual(One, IncrementOperatorsHelper<NFloat>.op_Increment(MaxSubnormal));
            AssertBitwiseEqual(One, IncrementOperatorsHelper<NFloat>.op_Increment(MinNormal));
            AssertBitwiseEqual(Two, IncrementOperatorsHelper<NFloat>.op_Increment(One));
            AssertBitwiseEqual(NFloat.MaxValue, IncrementOperatorsHelper<NFloat>.op_Increment(NFloat.MaxValue));
            AssertBitwiseEqual(NFloat.PositiveInfinity, IncrementOperatorsHelper<NFloat>.op_Increment(NFloat.PositiveInfinity));
        }

        [Fact]
        public static void op_CheckedIncrementTest()
        {
            AssertBitwiseEqual(NFloat.NegativeInfinity, IncrementOperatorsHelper<NFloat>.op_CheckedIncrement(NFloat.NegativeInfinity));
            AssertBitwiseEqual(NFloat.MinValue, IncrementOperatorsHelper<NFloat>.op_CheckedIncrement(NFloat.MinValue));
            AssertBitwiseEqual(Zero, IncrementOperatorsHelper<NFloat>.op_CheckedIncrement(NegativeOne));
            AssertBitwiseEqual(One, IncrementOperatorsHelper<NFloat>.op_CheckedIncrement(-MinNormal));
            AssertBitwiseEqual(One, IncrementOperatorsHelper<NFloat>.op_CheckedIncrement(-MaxSubnormal));
            AssertBitwiseEqual(One, IncrementOperatorsHelper<NFloat>.op_CheckedIncrement(-NFloat.Epsilon));
            AssertBitwiseEqual(One, IncrementOperatorsHelper<NFloat>.op_CheckedIncrement(NegativeZero));
            AssertBitwiseEqual(NFloat.NaN, IncrementOperatorsHelper<NFloat>.op_CheckedIncrement(NFloat.NaN));
            AssertBitwiseEqual(One, IncrementOperatorsHelper<NFloat>.op_CheckedIncrement(Zero));
            AssertBitwiseEqual(One, IncrementOperatorsHelper<NFloat>.op_CheckedIncrement(NFloat.Epsilon));
            AssertBitwiseEqual(One, IncrementOperatorsHelper<NFloat>.op_CheckedIncrement(MaxSubnormal));
            AssertBitwiseEqual(One, IncrementOperatorsHelper<NFloat>.op_CheckedIncrement(MinNormal));
            AssertBitwiseEqual(Two, IncrementOperatorsHelper<NFloat>.op_CheckedIncrement(One));
            AssertBitwiseEqual(NFloat.MaxValue, IncrementOperatorsHelper<NFloat>.op_CheckedIncrement(NFloat.MaxValue));
            AssertBitwiseEqual(NFloat.PositiveInfinity, IncrementOperatorsHelper<NFloat>.op_CheckedIncrement(NFloat.PositiveInfinity));
        }

        //
        // IMinMaxValue
        //

        [Fact]
        public static void MaxValueTest()
        {
            AssertBitwiseEqual(NFloat.MaxValue, MinMaxValueHelper<NFloat>.MaxValue);
        }

        [Fact]
        public static void MinValueTest()
        {
            AssertBitwiseEqual(NFloat.MinValue, MinMaxValueHelper<NFloat>.MinValue);
        }

        //
        // IModulusOperators
        //

        [Fact]
        public static void op_ModulusTest()
        {
            AssertBitwiseEqual(NFloat.NaN, ModulusOperatorsHelper<NFloat, NFloat, NFloat>.op_Modulus(NFloat.NegativeInfinity, Two));
            AssertBitwiseEqual(NegativeZero, ModulusOperatorsHelper<NFloat, NFloat, NFloat>.op_Modulus(NFloat.MinValue, Two));
            AssertBitwiseEqual(NegativeOne, ModulusOperatorsHelper<NFloat, NFloat, NFloat>.op_Modulus(NegativeOne, Two));
            AssertBitwiseEqual(-MinNormal, ModulusOperatorsHelper<NFloat, NFloat, NFloat>.op_Modulus(-MinNormal, Two));
            AssertBitwiseEqual(-MaxSubnormal, ModulusOperatorsHelper<NFloat, NFloat, NFloat>.op_Modulus(-MaxSubnormal, Two));
            AssertBitwiseEqual(-NFloat.Epsilon, ModulusOperatorsHelper<NFloat, NFloat, NFloat>.op_Modulus(-NFloat.Epsilon, Two)); ;
            AssertBitwiseEqual(NegativeZero, ModulusOperatorsHelper<NFloat, NFloat, NFloat>.op_Modulus(NegativeZero, Two));
            AssertBitwiseEqual(NFloat.NaN, ModulusOperatorsHelper<NFloat, NFloat, NFloat>.op_Modulus(NFloat.NaN, Two));
            AssertBitwiseEqual(Zero, ModulusOperatorsHelper<NFloat, NFloat, NFloat>.op_Modulus(Zero, Two));
            AssertBitwiseEqual(NFloat.Epsilon, ModulusOperatorsHelper<NFloat, NFloat, NFloat>.op_Modulus(NFloat.Epsilon, Two));
            AssertBitwiseEqual(MaxSubnormal, ModulusOperatorsHelper<NFloat, NFloat, NFloat>.op_Modulus(MaxSubnormal, Two));
            AssertBitwiseEqual(MinNormal, ModulusOperatorsHelper<NFloat, NFloat, NFloat>.op_Modulus(MinNormal, Two));
            AssertBitwiseEqual(One, ModulusOperatorsHelper<NFloat, NFloat, NFloat>.op_Modulus(One, Two));
            AssertBitwiseEqual(Zero, ModulusOperatorsHelper<NFloat, NFloat, NFloat>.op_Modulus(NFloat.MaxValue, Two));
            AssertBitwiseEqual(NFloat.NaN, ModulusOperatorsHelper<NFloat, NFloat, NFloat>.op_Modulus(NFloat.PositiveInfinity, Two));
        }

        //
        // IMultiplicativeIdentity
        //

        [Fact]
        public static void MultiplicativeIdentityTest()
        {
            AssertBitwiseEqual(One, MultiplicativeIdentityHelper<NFloat, NFloat>.MultiplicativeIdentity);
        }

        //
        // IMultiplyOperators
        //

        [Fact]
        public static void op_MultiplyTest()
        {
            AssertBitwiseEqual(NFloat.NegativeInfinity, MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_Multiply(NFloat.NegativeInfinity, Two));
            AssertBitwiseEqual(NFloat.NegativeInfinity, MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_Multiply(NFloat.MinValue, Two));
            AssertBitwiseEqual(NegativeTwo, MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_Multiply(NegativeOne, Two));
            AssertBitwiseEqual(NegativeZero, MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_Multiply(NegativeZero, Two));
            AssertBitwiseEqual(NFloat.NaN, MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_Multiply(NFloat.NaN, Two));
            AssertBitwiseEqual(Zero, MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_Multiply(Zero, Two));
            AssertBitwiseEqual(Two, MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_Multiply(One, Two));
            AssertBitwiseEqual(NFloat.PositiveInfinity, MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_Multiply(NFloat.MaxValue, Two));
            AssertBitwiseEqual(NFloat.PositiveInfinity, MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_Multiply(NFloat.PositiveInfinity, Two));

            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual((NFloat)(-4.4501477170144028E-308), MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_Multiply(-MinNormal, Two));
                AssertBitwiseEqual((NFloat)(-4.4501477170144018E-308), MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_Multiply(-MaxSubnormal, Two));
                AssertBitwiseEqual((NFloat)(-9.8813129168249309E-324), MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_Multiply(-NFloat.Epsilon, Two));
                AssertBitwiseEqual((NFloat)9.8813129168249309E-324, MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_Multiply(NFloat.Epsilon, Two));
                AssertBitwiseEqual((NFloat)4.4501477170144018E-308, MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_Multiply(MaxSubnormal, Two));
                AssertBitwiseEqual((NFloat)4.4501477170144028E-308, MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_Multiply(MinNormal, Two));
            }
            else
            {
                AssertBitwiseEqual((NFloat)(-2.3509887E-38f), MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_Multiply(-MinNormal, Two));
                AssertBitwiseEqual((NFloat)(-2.35098842E-38f), MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_Multiply(-MaxSubnormal, Two));
                AssertBitwiseEqual((NFloat)(-2.80259693E-45f), MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_Multiply(-NFloat.Epsilon, Two));
                AssertBitwiseEqual((NFloat)2.80259693E-45f, MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_Multiply(NFloat.Epsilon, Two));
                AssertBitwiseEqual((NFloat)2.35098842E-38f, MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_Multiply(MaxSubnormal, Two));
                AssertBitwiseEqual((NFloat)2.3509887E-38f, MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_Multiply(MinNormal, Two));
            }
        }

        [Fact]
        public static void op_CheckedMultiplyTest()
        {
            AssertBitwiseEqual(NFloat.NegativeInfinity, MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedMultiply(NFloat.NegativeInfinity, Two));
            AssertBitwiseEqual(NFloat.NegativeInfinity, MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedMultiply(NFloat.MinValue, Two));
            AssertBitwiseEqual(NegativeTwo, MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedMultiply(NegativeOne, Two));
            AssertBitwiseEqual(NegativeZero, MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedMultiply(NegativeZero, Two));
            AssertBitwiseEqual(NFloat.NaN, MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedMultiply(NFloat.NaN, Two));
            AssertBitwiseEqual(Zero, MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedMultiply(Zero, Two));
            AssertBitwiseEqual(Two, MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedMultiply(One, Two));
            AssertBitwiseEqual(NFloat.PositiveInfinity, MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedMultiply(NFloat.MaxValue, Two));
            AssertBitwiseEqual(NFloat.PositiveInfinity, MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedMultiply(NFloat.PositiveInfinity, Two));

            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual((NFloat)(-4.4501477170144028E-308), MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedMultiply(-MinNormal, Two));
                AssertBitwiseEqual((NFloat)(-4.4501477170144018E-308), MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedMultiply(-MaxSubnormal, Two));
                AssertBitwiseEqual((NFloat)(-9.8813129168249309E-324), MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedMultiply(-NFloat.Epsilon, Two));
                AssertBitwiseEqual((NFloat)9.8813129168249309E-324, MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedMultiply(NFloat.Epsilon, Two));
                AssertBitwiseEqual((NFloat)4.4501477170144018E-308, MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedMultiply(MaxSubnormal, Two));
                AssertBitwiseEqual((NFloat)4.4501477170144028E-308, MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedMultiply(MinNormal, Two));
            }
            else
            {
                AssertBitwiseEqual((NFloat)(-2.3509887E-38f), MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedMultiply(-MinNormal, Two));
                AssertBitwiseEqual((NFloat)(-2.35098842E-38f), MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedMultiply(-MaxSubnormal, Two));
                AssertBitwiseEqual((NFloat)(-2.80259693E-45f), MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedMultiply(-NFloat.Epsilon, Two));
                AssertBitwiseEqual((NFloat)2.80259693E-45f, MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedMultiply(NFloat.Epsilon, Two));
                AssertBitwiseEqual((NFloat)2.35098842E-38f, MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedMultiply(MaxSubnormal, Two));
                AssertBitwiseEqual((NFloat)2.3509887E-38f, MultiplyOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedMultiply(MinNormal, Two));
            }
        }

        //
        // INumber
        //

        [Fact]
        public static void ClampTest()
        {
            AssertBitwiseEqual(One, NumberHelper<NFloat>.Clamp(NFloat.NegativeInfinity, One, (NFloat)63.0f));
            AssertBitwiseEqual(One, NumberHelper<NFloat>.Clamp(NFloat.MinValue, One, (NFloat)63.0f));
            AssertBitwiseEqual(One, NumberHelper<NFloat>.Clamp(NegativeOne, One, (NFloat)63.0f));
            AssertBitwiseEqual(One, NumberHelper<NFloat>.Clamp(-MinNormal, One, (NFloat)63.0f));
            AssertBitwiseEqual(One, NumberHelper<NFloat>.Clamp(-MaxSubnormal, One, (NFloat)63.0f));
            AssertBitwiseEqual(One, NumberHelper<NFloat>.Clamp(-NFloat.Epsilon, One, (NFloat)63.0f));
            AssertBitwiseEqual(One, NumberHelper<NFloat>.Clamp(NegativeZero, One, (NFloat)63.0f));
            AssertBitwiseEqual(NFloat.NaN, NumberHelper<NFloat>.Clamp(NFloat.NaN, One, (NFloat)63.0f));
            AssertBitwiseEqual(One, NumberHelper<NFloat>.Clamp(Zero, One, (NFloat)63.0f));
            AssertBitwiseEqual(One, NumberHelper<NFloat>.Clamp(NFloat.Epsilon, One, (NFloat)63.0f));
            AssertBitwiseEqual(One, NumberHelper<NFloat>.Clamp(MaxSubnormal, One, (NFloat)63.0f));
            AssertBitwiseEqual(One, NumberHelper<NFloat>.Clamp(MinNormal, One, (NFloat)63.0f));
            AssertBitwiseEqual(One, NumberHelper<NFloat>.Clamp(One, One, (NFloat)63.0f));
            AssertBitwiseEqual((NFloat)63.0f, NumberHelper<NFloat>.Clamp(NFloat.MaxValue, One, (NFloat)63.0f));
            AssertBitwiseEqual((NFloat)63.0f, NumberHelper<NFloat>.Clamp(NFloat.PositiveInfinity, One, (NFloat)63.0f));
        }

        [Fact]
        public static void MaxTest()
        {
            AssertBitwiseEqual(One, NumberHelper<NFloat>.Max(NFloat.NegativeInfinity, One));
            AssertBitwiseEqual(One, NumberHelper<NFloat>.Max(NFloat.MinValue, One));
            AssertBitwiseEqual(One, NumberHelper<NFloat>.Max(NegativeOne, One));
            AssertBitwiseEqual(One, NumberHelper<NFloat>.Max(-MinNormal, One));
            AssertBitwiseEqual(One, NumberHelper<NFloat>.Max(-MaxSubnormal, One));
            AssertBitwiseEqual(One, NumberHelper<NFloat>.Max(-NFloat.Epsilon, One));
            AssertBitwiseEqual(One, NumberHelper<NFloat>.Max(NegativeZero, One));
            AssertBitwiseEqual(NFloat.NaN, NumberHelper<NFloat>.Max(NFloat.NaN, One));
            AssertBitwiseEqual(One, NumberHelper<NFloat>.Max(Zero, One));
            AssertBitwiseEqual(One, NumberHelper<NFloat>.Max(NFloat.Epsilon, One));
            AssertBitwiseEqual(One, NumberHelper<NFloat>.Max(MaxSubnormal, One));
            AssertBitwiseEqual(One, NumberHelper<NFloat>.Max(MinNormal, One));
            AssertBitwiseEqual(One, NumberHelper<NFloat>.Max(One, One));
            AssertBitwiseEqual(NFloat.MaxValue, NumberHelper<NFloat>.Max(NFloat.MaxValue, One));
            AssertBitwiseEqual(NFloat.PositiveInfinity, NumberHelper<NFloat>.Max(NFloat.PositiveInfinity, One));
        }

        [Fact]
        public static void MaxNumberTest()
        {
            AssertBitwiseEqual(One, NumberHelper<NFloat>.MaxNumber(NFloat.NegativeInfinity, One));
            AssertBitwiseEqual(One, NumberHelper<NFloat>.MaxNumber(NFloat.MinValue, One));
            AssertBitwiseEqual(One, NumberHelper<NFloat>.MaxNumber(NegativeOne, One));
            AssertBitwiseEqual(One, NumberHelper<NFloat>.MaxNumber(-MinNormal, One));
            AssertBitwiseEqual(One, NumberHelper<NFloat>.MaxNumber(-MaxSubnormal, One));
            AssertBitwiseEqual(One, NumberHelper<NFloat>.MaxNumber(-NFloat.Epsilon, One));
            AssertBitwiseEqual(One, NumberHelper<NFloat>.MaxNumber(NegativeZero, One));
            AssertBitwiseEqual(One, NumberHelper<NFloat>.MaxNumber(NFloat.NaN, One));
            AssertBitwiseEqual(One, NumberHelper<NFloat>.MaxNumber(Zero, One));
            AssertBitwiseEqual(One, NumberHelper<NFloat>.MaxNumber(NFloat.Epsilon, One));
            AssertBitwiseEqual(One, NumberHelper<NFloat>.MaxNumber(MaxSubnormal, One));
            AssertBitwiseEqual(One, NumberHelper<NFloat>.MaxNumber(MinNormal, One));
            AssertBitwiseEqual(One, NumberHelper<NFloat>.MaxNumber(One, One));
            AssertBitwiseEqual(NFloat.MaxValue, NumberHelper<NFloat>.MaxNumber(NFloat.MaxValue, One));
            AssertBitwiseEqual(NFloat.PositiveInfinity, NumberHelper<NFloat>.MaxNumber(NFloat.PositiveInfinity, One));
        }

        [Fact]
        public static void MinTest()
        {
            AssertBitwiseEqual(NFloat.NegativeInfinity, NumberHelper<NFloat>.Min(NFloat.NegativeInfinity, One));
            AssertBitwiseEqual(NFloat.MinValue, NumberHelper<NFloat>.Min(NFloat.MinValue, One));
            AssertBitwiseEqual(NegativeOne, NumberHelper<NFloat>.Min(NegativeOne, One));
            AssertBitwiseEqual(-MinNormal, NumberHelper<NFloat>.Min(-MinNormal, One));
            AssertBitwiseEqual(-MaxSubnormal, NumberHelper<NFloat>.Min(-MaxSubnormal, One));
            AssertBitwiseEqual(-NFloat.Epsilon, NumberHelper<NFloat>.Min(-NFloat.Epsilon, One));
            AssertBitwiseEqual(NegativeZero, NumberHelper<NFloat>.Min(NegativeZero, One));
            AssertBitwiseEqual(NFloat.NaN, NumberHelper<NFloat>.Min(NFloat.NaN, One));
            AssertBitwiseEqual(Zero, NumberHelper<NFloat>.Min(Zero, One));
            AssertBitwiseEqual(NFloat.Epsilon, NumberHelper<NFloat>.Min(NFloat.Epsilon, One));
            AssertBitwiseEqual(MaxSubnormal, NumberHelper<NFloat>.Min(MaxSubnormal, One));
            AssertBitwiseEqual(MinNormal, NumberHelper<NFloat>.Min(MinNormal, One));
            AssertBitwiseEqual(One, NumberHelper<NFloat>.Min(One, One));
            AssertBitwiseEqual(One, NumberHelper<NFloat>.Min(NFloat.MaxValue, One));
            AssertBitwiseEqual(One, NumberHelper<NFloat>.Min(NFloat.PositiveInfinity, One));
        }

        [Fact]
        public static void MinNumberTest()
        {
            AssertBitwiseEqual(NFloat.NegativeInfinity, NumberHelper<NFloat>.MinNumber(NFloat.NegativeInfinity, One));
            AssertBitwiseEqual(NFloat.MinValue, NumberHelper<NFloat>.MinNumber(NFloat.MinValue, One));
            AssertBitwiseEqual(NegativeOne, NumberHelper<NFloat>.MinNumber(NegativeOne, One));
            AssertBitwiseEqual(-MinNormal, NumberHelper<NFloat>.MinNumber(-MinNormal, One));
            AssertBitwiseEqual(-MaxSubnormal, NumberHelper<NFloat>.MinNumber(-MaxSubnormal, One));
            AssertBitwiseEqual(-NFloat.Epsilon, NumberHelper<NFloat>.MinNumber(-NFloat.Epsilon, One));
            AssertBitwiseEqual(NegativeZero, NumberHelper<NFloat>.MinNumber(NegativeZero, One));
            AssertBitwiseEqual(One, NumberHelper<NFloat>.MinNumber(NFloat.NaN, One));
            AssertBitwiseEqual(Zero, NumberHelper<NFloat>.MinNumber(Zero, One));
            AssertBitwiseEqual(NFloat.Epsilon, NumberHelper<NFloat>.MinNumber(NFloat.Epsilon, One));
            AssertBitwiseEqual(MaxSubnormal, NumberHelper<NFloat>.MinNumber(MaxSubnormal, One));
            AssertBitwiseEqual(MinNormal, NumberHelper<NFloat>.MinNumber(MinNormal, One));
            AssertBitwiseEqual(One, NumberHelper<NFloat>.MinNumber(One, One));
            AssertBitwiseEqual(One, NumberHelper<NFloat>.MinNumber(NFloat.MaxValue, One));
            AssertBitwiseEqual(One, NumberHelper<NFloat>.MinNumber(NFloat.PositiveInfinity, One));
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
            Assert.Equal(0, NumberHelper<NFloat>.Sign(Zero));

            Assert.Equal(1, NumberHelper<NFloat>.Sign(NFloat.Epsilon));
            Assert.Equal(1, NumberHelper<NFloat>.Sign(MaxSubnormal));
            Assert.Equal(1, NumberHelper<NFloat>.Sign(MinNormal));
            Assert.Equal(1, NumberHelper<NFloat>.Sign(One));
            Assert.Equal(1, NumberHelper<NFloat>.Sign(NFloat.MaxValue));
            Assert.Equal(1, NumberHelper<NFloat>.Sign(NFloat.PositiveInfinity));

            Assert.Throws<ArithmeticException>(() => NumberHelper<NFloat>.Sign(NFloat.NaN));
        }

        //
        // INumberBase
        //

        [Fact]
        public static void OneTest()
        {
            AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.One);
        }

        [Fact]
        public static void RadixTest()
        {
            Assert.Equal(2, NumberBaseHelper<NFloat>.Radix);
        }

        [Fact]
        public static void ZeroTest()
        {
            AssertBitwiseEqual(Zero, NumberBaseHelper<NFloat>.Zero);
        }

        [Fact]
        public static void AbsTest()
        {
            AssertBitwiseEqual(NFloat.PositiveInfinity, NumberBaseHelper<NFloat>.Abs(NFloat.NegativeInfinity));
            AssertBitwiseEqual(NFloat.MaxValue, NumberBaseHelper<NFloat>.Abs(NFloat.MinValue));
            AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.Abs(NegativeOne));
            AssertBitwiseEqual(MinNormal, NumberBaseHelper<NFloat>.Abs(-MinNormal));
            AssertBitwiseEqual(MaxSubnormal, NumberBaseHelper<NFloat>.Abs(-MaxSubnormal));
            AssertBitwiseEqual(NFloat.Epsilon, NumberBaseHelper<NFloat>.Abs(-NFloat.Epsilon));
            AssertBitwiseEqual(Zero, NumberBaseHelper<NFloat>.Abs(NegativeZero));
            AssertBitwiseEqual(NFloat.NaN, NumberBaseHelper<NFloat>.Abs(NFloat.NaN));
            AssertBitwiseEqual(Zero, NumberBaseHelper<NFloat>.Abs(Zero));
            AssertBitwiseEqual(NFloat.Epsilon, NumberBaseHelper<NFloat>.Abs(NFloat.Epsilon));
            AssertBitwiseEqual(MaxSubnormal, NumberBaseHelper<NFloat>.Abs(MaxSubnormal));
            AssertBitwiseEqual(MinNormal, NumberBaseHelper<NFloat>.Abs(MinNormal));
            AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.Abs(One));
            AssertBitwiseEqual(NFloat.MaxValue, NumberBaseHelper<NFloat>.Abs(NFloat.MaxValue));
            AssertBitwiseEqual(NFloat.PositiveInfinity, NumberBaseHelper<NFloat>.Abs(NFloat.PositiveInfinity));
        }

        [Fact]
        public static void CreateCheckedFromByteTest()
        {
            AssertBitwiseEqual(Zero, NumberBaseHelper<NFloat>.CreateChecked<byte>(0x00));
            AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.CreateChecked<byte>(0x01));
            AssertBitwiseEqual((NFloat)127.0f, NumberBaseHelper<NFloat>.CreateChecked<byte>(0x7F));
            AssertBitwiseEqual((NFloat)128.0f, NumberBaseHelper<NFloat>.CreateChecked<byte>(0x80));
            AssertBitwiseEqual((NFloat)255.0f, NumberBaseHelper<NFloat>.CreateChecked<byte>(0xFF));
        }

        [Fact]
        public static void CreateCheckedFromCharTest()
        {
            AssertBitwiseEqual(Zero, NumberBaseHelper<NFloat>.CreateChecked<char>((char)0x0000));
            AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.CreateChecked<char>((char)0x0001));
            AssertBitwiseEqual((NFloat)32767.0f, NumberBaseHelper<NFloat>.CreateChecked<char>((char)0x7FFF));
            AssertBitwiseEqual((NFloat)32768.0f, NumberBaseHelper<NFloat>.CreateChecked<char>((char)0x8000));
            AssertBitwiseEqual((NFloat)65535.0f, NumberBaseHelper<NFloat>.CreateChecked<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateCheckedFromDecimalTest()
        {
            AssertBitwiseEqual(-1.0f, NumberBaseHelper<NFloat>.CreateChecked<decimal>(-1.0m));
            AssertBitwiseEqual(-0.0f, NumberBaseHelper<NFloat>.CreateChecked<decimal>(-0.0m));
            AssertBitwiseEqual(+0.0f, NumberBaseHelper<NFloat>.CreateChecked<decimal>(+0.0m));
            AssertBitwiseEqual(+1.0f, NumberBaseHelper<NFloat>.CreateChecked<decimal>(+1.0m));

            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual((NFloat)(-79228162514264337593543950335.0), NumberBaseHelper<NFloat>.CreateChecked<decimal>(decimal.MinValue));
                AssertBitwiseEqual((NFloat)(+79228162514264337593543950335.0), NumberBaseHelper<NFloat>.CreateChecked<decimal>(decimal.MaxValue));
            }
            else
            {
                AssertBitwiseEqual(-79228162514264337593543950335.0f, NumberBaseHelper<NFloat>.CreateChecked<decimal>(decimal.MinValue));
                AssertBitwiseEqual(+79228162514264337593543950335.0f, NumberBaseHelper<NFloat>.CreateChecked<decimal>(decimal.MaxValue));
            }
        }

        [Fact]
        public static void CreateCheckedFromDoubleTest()
        {
            AssertBitwiseEqual(NFloat.NegativeInfinity, NumberBaseHelper<NFloat>.CreateChecked<double>(double.NegativeInfinity));

            AssertBitwiseEqual(-1.0f, NumberBaseHelper<NFloat>.CreateChecked<double>(-1.0));
            AssertBitwiseEqual(-0.0f, NumberBaseHelper<NFloat>.CreateChecked<double>(-0.0));
            AssertBitwiseEqual(+0.0f, NumberBaseHelper<NFloat>.CreateChecked<double>(+0.0));
            AssertBitwiseEqual(+1.0f, NumberBaseHelper<NFloat>.CreateChecked<double>(1.0));

            AssertBitwiseEqual(NFloat.PositiveInfinity, NumberBaseHelper<NFloat>.CreateChecked<double>(double.PositiveInfinity));

            AssertBitwiseEqual(NFloat.NaN, NumberBaseHelper<NFloat>.CreateChecked<double>(double.NaN));

            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual(NFloat.MinValue, NumberBaseHelper<NFloat>.CreateChecked<double>(double.MinValue));
                AssertBitwiseEqual(-MinNormal, NumberBaseHelper<NFloat>.CreateChecked<double>(-2.2250738585072014E-308));
                AssertBitwiseEqual(-MaxSubnormal, NumberBaseHelper<NFloat>.CreateChecked<double>(-2.2250738585072009E-308));
                AssertBitwiseEqual(-NFloat.Epsilon, NumberBaseHelper<NFloat>.CreateChecked<double>(-double.Epsilon));

                AssertBitwiseEqual(+NFloat.Epsilon, NumberBaseHelper<NFloat>.CreateChecked<double>(double.Epsilon));
                AssertBitwiseEqual(+MaxSubnormal, NumberBaseHelper<NFloat>.CreateChecked<double>(2.2250738585072009E-308));
                AssertBitwiseEqual(+MinNormal, NumberBaseHelper<NFloat>.CreateChecked<double>(2.2250738585072014E-308));
                AssertBitwiseEqual(NFloat.MaxValue, NumberBaseHelper<NFloat>.CreateChecked<double>(double.MaxValue));
            }
            else
            {
                AssertBitwiseEqual(NFloat.NegativeInfinity, NumberBaseHelper<NFloat>.CreateChecked<double>(double.MinValue));
                AssertBitwiseEqual(-0.0f, NumberBaseHelper<NFloat>.CreateChecked<double>(-2.2250738585072014E-308));
                AssertBitwiseEqual(-0.0f, NumberBaseHelper<NFloat>.CreateChecked<double>(-2.2250738585072009E-308));
                AssertBitwiseEqual(-0.0f, NumberBaseHelper<NFloat>.CreateChecked<double>(-double.Epsilon));

                AssertBitwiseEqual(+0.0f, NumberBaseHelper<NFloat>.CreateChecked<double>(double.Epsilon));
                AssertBitwiseEqual(+0.0f, NumberBaseHelper<NFloat>.CreateChecked<double>(2.2250738585072009E-308));
                AssertBitwiseEqual(+0.0f, NumberBaseHelper<NFloat>.CreateChecked<double>(2.2250738585072014E-308));
                AssertBitwiseEqual(NFloat.PositiveInfinity, NumberBaseHelper<NFloat>.CreateChecked<double>(double.MaxValue));
            }
        }

        [Fact]
        public static void CreateCheckedFromHalfTest()
        {
            AssertBitwiseEqual(NFloat.NegativeInfinity, NumberBaseHelper<NFloat>.CreateChecked<Half>(Half.NegativeInfinity));

            AssertBitwiseEqual(-65504.0f, NumberBaseHelper<NFloat>.CreateChecked<Half>(Half.MinValue));
            AssertBitwiseEqual(-1.0f, NumberBaseHelper<NFloat>.CreateChecked<Half>(Half.NegativeOne));
            AssertBitwiseEqual(-0.0f, NumberBaseHelper<NFloat>.CreateChecked<Half>(Half.NegativeZero));

            AssertBitwiseEqual(+0.0f, NumberBaseHelper<NFloat>.CreateChecked<Half>(Half.Zero));
            AssertBitwiseEqual(+1.0f, NumberBaseHelper<NFloat>.CreateChecked<Half>(Half.One));
            AssertBitwiseEqual(+65504.0f, NumberBaseHelper<NFloat>.CreateChecked<Half>(Half.MaxValue));

            AssertBitwiseEqual(NFloat.PositiveInfinity, NumberBaseHelper<NFloat>.CreateChecked<Half>(Half.PositiveInfinity));

            AssertBitwiseEqual(NFloat.NaN, NumberBaseHelper<NFloat>.CreateChecked<Half>(Half.NaN));

            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual((NFloat)(-6.103515625E-05), NumberBaseHelper<NFloat>.CreateChecked<Half>(-BitConverter.UInt16BitsToHalf(0x0400)));
                AssertBitwiseEqual((NFloat)(-6.097555160522461E-05), NumberBaseHelper<NFloat>.CreateChecked<Half>(-BitConverter.UInt16BitsToHalf(0x03FF)));
                AssertBitwiseEqual((NFloat)(-5.960464477539063E-08), NumberBaseHelper<NFloat>.CreateChecked<Half>(-Half.Epsilon));

                AssertBitwiseEqual((NFloat)(+5.960464477539063E-08), NumberBaseHelper<NFloat>.CreateChecked<Half>(Half.Epsilon));
                AssertBitwiseEqual((NFloat)(+6.097555160522461E-05), NumberBaseHelper<NFloat>.CreateChecked<Half>(BitConverter.UInt16BitsToHalf(0x03FF)));
                AssertBitwiseEqual((NFloat)(+6.103515625E-05), NumberBaseHelper<NFloat>.CreateChecked<Half>(BitConverter.UInt16BitsToHalf(0x0400)));
            }
            else
            {
                AssertBitwiseEqual(-6.1035156E-05f, NumberBaseHelper<NFloat>.CreateChecked<Half>(-BitConverter.UInt16BitsToHalf(0x0400)));
                AssertBitwiseEqual(-6.097555E-05f, NumberBaseHelper<NFloat>.CreateChecked<Half>(-BitConverter.UInt16BitsToHalf(0x03FF)));
                AssertBitwiseEqual(-5.9604645E-08f, NumberBaseHelper<NFloat>.CreateChecked<Half>(-Half.Epsilon));

                AssertBitwiseEqual(+5.9604645E-08f, NumberBaseHelper<NFloat>.CreateChecked<Half>(Half.Epsilon));
                AssertBitwiseEqual(+6.097555E-05f, NumberBaseHelper<NFloat>.CreateChecked<Half>(BitConverter.UInt16BitsToHalf(0x03FF)));
                AssertBitwiseEqual(+6.1035156E-05f, NumberBaseHelper<NFloat>.CreateChecked<Half>(BitConverter.UInt16BitsToHalf(0x0400)));
            }
        }

        [Fact]
        public static void CreateCheckedFromInt16Test()
        {
            AssertBitwiseEqual(Zero, NumberBaseHelper<NFloat>.CreateChecked<short>(0x0000));
            AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.CreateChecked<short>(0x0001));
            AssertBitwiseEqual((NFloat)32767.0f, NumberBaseHelper<NFloat>.CreateChecked<short>(0x7FFF));
            AssertBitwiseEqual((NFloat)(-32768.0f), NumberBaseHelper<NFloat>.CreateChecked<short>(unchecked((short)0x8000)));
            AssertBitwiseEqual(NegativeOne, NumberBaseHelper<NFloat>.CreateChecked<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromInt32Test()
        {
            AssertBitwiseEqual(Zero, NumberBaseHelper<NFloat>.CreateChecked<int>(0x00000000));
            AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.CreateChecked<int>(0x00000001));
            AssertBitwiseEqual(NegativeOne, NumberBaseHelper<NFloat>.CreateChecked<int>(unchecked((int)0xFFFFFFFF)));

            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual((NFloat)2147483647.0, NumberBaseHelper<NFloat>.CreateChecked<int>(0x7FFFFFFF));
                AssertBitwiseEqual((NFloat)(-2147483648.0), NumberBaseHelper<NFloat>.CreateChecked<int>(unchecked((int)0x80000000)));
            }
            else
            {
                AssertBitwiseEqual((NFloat)2147483647.0f, NumberBaseHelper<NFloat>.CreateChecked<int>(0x7FFFFFFF));
                AssertBitwiseEqual((NFloat)(-2147483648.0f), NumberBaseHelper<NFloat>.CreateChecked<int>(unchecked((int)0x80000000)));
            }
        }

        [Fact]
        public static void CreateCheckedFromInt64Test()
        {
            AssertBitwiseEqual(Zero, NumberBaseHelper<NFloat>.CreateChecked<long>(0x0000000000000000));
            AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.CreateChecked<long>(0x0000000000000001));
            AssertBitwiseEqual(NegativeOne, NumberBaseHelper<NFloat>.CreateChecked<long>(unchecked(unchecked((long)0xFFFFFFFFFFFFFFFF))));

            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual((NFloat)9223372036854775807.0, NumberBaseHelper<NFloat>.CreateChecked<long>(0x7FFFFFFFFFFFFFFF));
                AssertBitwiseEqual((NFloat)(-9223372036854775808.0), NumberBaseHelper<NFloat>.CreateChecked<long>(unchecked(unchecked((long)0x8000000000000000))));
            }
            else
            {
                AssertBitwiseEqual((NFloat)9223372036854775807.0f, NumberBaseHelper<NFloat>.CreateChecked<long>(0x7FFFFFFFFFFFFFFF));
                AssertBitwiseEqual((NFloat)(-9223372036854775808.0f), NumberBaseHelper<NFloat>.CreateChecked<long>(unchecked(unchecked((long)0x8000000000000000))));
            }
        }

        [Fact]
        public static void CreateCheckedFromInt128Test()
        {
            AssertBitwiseEqual(+0.0f, NumberBaseHelper<NFloat>.CreateChecked<Int128>(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000)));
            AssertBitwiseEqual(+1.0f, NumberBaseHelper<NFloat>.CreateChecked<Int128>(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001)));
            AssertBitwiseEqual(-1.0f, NumberBaseHelper<NFloat>.CreateChecked<Int128>(new Int128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)));

            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual((NFloat)(+170141183460469231731687303715884105727.0), NumberBaseHelper<NFloat>.CreateChecked<Int128>(new Int128(0x7FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)));
                AssertBitwiseEqual((NFloat)(-170141183460469231731687303715884105728.0), NumberBaseHelper<NFloat>.CreateChecked<Int128>(new Int128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000)));
            }
            else
            {
                AssertBitwiseEqual(+170141183460469231731687303715884105727.0f, NumberBaseHelper<NFloat>.CreateChecked<Int128>(new Int128(0x7FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)));
                AssertBitwiseEqual(-170141183460469231731687303715884105728.0f, NumberBaseHelper<NFloat>.CreateChecked<Int128>(new Int128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000)));
            }
        }

        [Fact]
        public static void CreateCheckedFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual(Zero, NumberBaseHelper<NFloat>.CreateChecked<nint>(unchecked((nint)0x0000000000000000)));
                AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.CreateChecked<nint>(unchecked((nint)0x0000000000000001)));
                AssertBitwiseEqual((NFloat)9223372036854775807.0, NumberBaseHelper<NFloat>.CreateChecked<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                AssertBitwiseEqual((NFloat)(-9223372036854775808.0), NumberBaseHelper<NFloat>.CreateChecked<nint>(unchecked((nint)0x8000000000000000)));
                AssertBitwiseEqual(NegativeOne, NumberBaseHelper<NFloat>.CreateChecked<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                AssertBitwiseEqual(Zero, NumberBaseHelper<NFloat>.CreateChecked<nint>((nint)0x00000000));
                AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.CreateChecked<nint>((nint)0x00000001));
                AssertBitwiseEqual((NFloat)2147483647.0f, NumberBaseHelper<NFloat>.CreateChecked<nint>((nint)0x7FFFFFFF));
                AssertBitwiseEqual((NFloat)(-2147483648.0f), NumberBaseHelper<NFloat>.CreateChecked<nint>(unchecked((nint)0x80000000)));
                AssertBitwiseEqual(NegativeOne, NumberBaseHelper<NFloat>.CreateChecked<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateCheckedFromNFloatTest()
        {
            AssertBitwiseEqual(NFloat.NegativeInfinity, NumberBaseHelper<NFloat>.CreateChecked<NFloat>(NFloat.NegativeInfinity));

            AssertBitwiseEqual(-1.0f, NumberBaseHelper<NFloat>.CreateChecked<NFloat>(-1.0f));
            AssertBitwiseEqual(-0.0f, NumberBaseHelper<NFloat>.CreateChecked<NFloat>(-0.0f));

            AssertBitwiseEqual(+0.0f, NumberBaseHelper<NFloat>.CreateChecked<NFloat>(+0.0f));
            AssertBitwiseEqual(+1.0f, NumberBaseHelper<NFloat>.CreateChecked<NFloat>(1.0f));

            AssertBitwiseEqual(NFloat.MinValue, NumberBaseHelper<NFloat>.CreateChecked<NFloat>(NFloat.MinValue));

            AssertBitwiseEqual(-MinNormal, NumberBaseHelper<NFloat>.CreateChecked<NFloat>((NFloat)(-MinNormal)));
            AssertBitwiseEqual(-MaxSubnormal, NumberBaseHelper<NFloat>.CreateChecked<NFloat>((NFloat)(-MaxSubnormal)));
            AssertBitwiseEqual(-NFloat.Epsilon, NumberBaseHelper<NFloat>.CreateChecked<NFloat>(-NFloat.Epsilon));

            AssertBitwiseEqual(+NFloat.Epsilon, NumberBaseHelper<NFloat>.CreateChecked<NFloat>(NFloat.Epsilon));
            AssertBitwiseEqual(+MaxSubnormal, NumberBaseHelper<NFloat>.CreateChecked<NFloat>((NFloat)MaxSubnormal));
            AssertBitwiseEqual(+MinNormal, NumberBaseHelper<NFloat>.CreateChecked<NFloat>((NFloat)MinNormal));

            AssertBitwiseEqual(NFloat.MaxValue, NumberBaseHelper<NFloat>.CreateChecked<NFloat>(NFloat.MaxValue));

            AssertBitwiseEqual(NFloat.PositiveInfinity, NumberBaseHelper<NFloat>.CreateChecked<NFloat>(NFloat.PositiveInfinity));

            AssertBitwiseEqual(NFloat.NaN, NumberBaseHelper<NFloat>.CreateChecked<NFloat>(NFloat.NaN));
        }

        [Fact]
        public static void CreateCheckedFromSByteTest()
        {
            AssertBitwiseEqual(Zero, NumberBaseHelper<NFloat>.CreateChecked<sbyte>(0x00));
            AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.CreateChecked<sbyte>(0x01));
            AssertBitwiseEqual((NFloat)127.0f, NumberBaseHelper<NFloat>.CreateChecked<sbyte>(0x7F));
            AssertBitwiseEqual((NFloat)(-128.0f), NumberBaseHelper<NFloat>.CreateChecked<sbyte>(unchecked((sbyte)0x80)));
            AssertBitwiseEqual(NegativeOne, NumberBaseHelper<NFloat>.CreateChecked<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateCheckedFromSingleTest()
        {
            AssertBitwiseEqual(NFloat.NegativeInfinity, NumberBaseHelper<NFloat>.CreateChecked<float>(float.NegativeInfinity));

            AssertBitwiseEqual(-3.4028234663852886E+38f, NumberBaseHelper<NFloat>.CreateChecked<float>(float.MinValue));
            AssertBitwiseEqual(-1.0f, NumberBaseHelper<NFloat>.CreateChecked<float>(-1.0f));

            AssertBitwiseEqual(-1.1754943508222875E-38f, NumberBaseHelper<NFloat>.CreateChecked<float>(-1.17549435E-38f));
            AssertBitwiseEqual(-1.1754942106924411E-38f, NumberBaseHelper<NFloat>.CreateChecked<float>(-1.17549421E-38f));
            AssertBitwiseEqual(-1.401298464324817E-45f, NumberBaseHelper<NFloat>.CreateChecked<float>(-float.Epsilon));
            AssertBitwiseEqual(-0.0f, NumberBaseHelper<NFloat>.CreateChecked<float>(-0.0f));

            AssertBitwiseEqual(+0.0f, NumberBaseHelper<NFloat>.CreateChecked<float>(+0.0f));
            AssertBitwiseEqual(+1.401298464324817E-45f, NumberBaseHelper<NFloat>.CreateChecked<float>(float.Epsilon));
            AssertBitwiseEqual(+1.1754942106924411E-38f, NumberBaseHelper<NFloat>.CreateChecked<float>(1.17549421E-38f));
            AssertBitwiseEqual(+1.1754943508222875E-38f, NumberBaseHelper<NFloat>.CreateChecked<float>(1.17549435E-38f));

            AssertBitwiseEqual(+1.0f, NumberBaseHelper<NFloat>.CreateChecked<float>(1.0f));
            AssertBitwiseEqual(+3.4028234663852886E+38f, NumberBaseHelper<NFloat>.CreateChecked<float>(float.MaxValue));

            AssertBitwiseEqual(NFloat.PositiveInfinity, NumberBaseHelper<NFloat>.CreateChecked<float>(float.PositiveInfinity));

            AssertBitwiseEqual(NFloat.NaN, NumberBaseHelper<NFloat>.CreateChecked<float>(float.NaN));
        }

        [Fact]
        public static void CreateCheckedFromUInt16Test()
        {
            AssertBitwiseEqual(Zero, NumberBaseHelper<NFloat>.CreateChecked<ushort>(0x0000));
            AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.CreateChecked<ushort>(0x0001));
            AssertBitwiseEqual((NFloat)32767.0f, NumberBaseHelper<NFloat>.CreateChecked<ushort>(0x7FFF));
            AssertBitwiseEqual((NFloat)32768.0f, NumberBaseHelper<NFloat>.CreateChecked<ushort>(0x8000));
            AssertBitwiseEqual((NFloat)65535.0f, NumberBaseHelper<NFloat>.CreateChecked<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUInt32Test()
        {
            AssertBitwiseEqual(Zero, NumberBaseHelper<NFloat>.CreateChecked<uint>(0x00000000));
            AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.CreateChecked<uint>(0x00000001));

            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual((NFloat)2147483647.0, NumberBaseHelper<NFloat>.CreateChecked<uint>(0x7FFFFFFF));
                AssertBitwiseEqual((NFloat)2147483648.0, NumberBaseHelper<NFloat>.CreateChecked<uint>(0x80000000));
                AssertBitwiseEqual((NFloat)4294967295.0, NumberBaseHelper<NFloat>.CreateChecked<uint>(0xFFFFFFFF));
            }
            else
            {
                AssertBitwiseEqual((NFloat)2147483647.0f, NumberBaseHelper<NFloat>.CreateChecked<uint>(0x7FFFFFFF));
                AssertBitwiseEqual((NFloat)2147483648.0f, NumberBaseHelper<NFloat>.CreateChecked<uint>(0x80000000));
                AssertBitwiseEqual((NFloat)4294967295.0f, NumberBaseHelper<NFloat>.CreateChecked<uint>(0xFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateCheckedFromUInt64Test()
        {
            AssertBitwiseEqual(Zero, NumberBaseHelper<NFloat>.CreateChecked<ulong>(0x0000000000000000));
            AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.CreateChecked<ulong>(0x0000000000000001));

            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual((NFloat)9223372036854775807.0, NumberBaseHelper<NFloat>.CreateChecked<ulong>(0x7FFFFFFFFFFFFFFF));
                AssertBitwiseEqual((NFloat)9223372036854775808.0, NumberBaseHelper<NFloat>.CreateChecked<ulong>(0x8000000000000000));
                AssertBitwiseEqual((NFloat)18446744073709551615.0, NumberBaseHelper<NFloat>.CreateChecked<ulong>(0xFFFFFFFFFFFFFFFF));
            }
            else
            {
                AssertBitwiseEqual((NFloat)9223372036854775807.0f, NumberBaseHelper<NFloat>.CreateChecked<ulong>(0x7FFFFFFFFFFFFFFF));
                AssertBitwiseEqual((NFloat)9223372036854775808.0f, NumberBaseHelper<NFloat>.CreateChecked<ulong>(0x8000000000000000));
                AssertBitwiseEqual((NFloat)18446744073709551615.0f, NumberBaseHelper<NFloat>.CreateChecked<ulong>(0xFFFFFFFFFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateCheckedFromUInt128Test()
        {
            AssertBitwiseEqual(0.0f, NumberBaseHelper<NFloat>.CreateChecked<UInt128>(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000)));
            AssertBitwiseEqual(1.0f, NumberBaseHelper<NFloat>.CreateChecked<UInt128>(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001)));

            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual((NFloat)(170141183460469231731687303715884105727.0), NumberBaseHelper<NFloat>.CreateChecked<UInt128>(new UInt128(0x7FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)));
                AssertBitwiseEqual((NFloat)(170141183460469231731687303715884105728.0), NumberBaseHelper<NFloat>.CreateChecked<UInt128>(new UInt128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000)));
                AssertBitwiseEqual((NFloat)(340282366920938463463374607431768211455.0), NumberBaseHelper<NFloat>.CreateChecked<UInt128>(new UInt128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)));
            }
            else
            {
                AssertBitwiseEqual(170141183460469231731687303715884105727.0f, NumberBaseHelper<NFloat>.CreateChecked<UInt128>(new UInt128(0x7FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)));
                AssertBitwiseEqual(170141183460469231731687303715884105728.0f, NumberBaseHelper<NFloat>.CreateChecked<UInt128>(new UInt128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000)));
                AssertBitwiseEqual(float.PositiveInfinity, NumberBaseHelper<NFloat>.CreateChecked<UInt128>(new UInt128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)));
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/69795", TestRuntimes.Mono)]
        public static void CreateCheckedFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual(Zero, NumberBaseHelper<NFloat>.CreateChecked<nuint>(unchecked((nuint)0x0000000000000000)));
                AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.CreateChecked<nuint>(unchecked((nuint)0x0000000000000001)));
                AssertBitwiseEqual((NFloat)9223372036854775807.0, NumberBaseHelper<NFloat>.CreateChecked<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));

                // https://github.com/dotnet/roslyn/issues/60714
                // AssertBitwiseEqual((NFloat)9223372036854775808.0, NumberBaseHelper<NFloat>.CreateChecked<nuint>(unchecked((nuint)0x8000000000000000)));
                // AssertBitwiseEqual((NFloat)18446744073709551615.0,NumberBaseHelper<NFloat>.CreateChecked<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                AssertBitwiseEqual(Zero, NumberBaseHelper<NFloat>.CreateChecked<nuint>((nuint)0x00000000));
                AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.CreateChecked<nuint>((nuint)0x00000001));
                AssertBitwiseEqual((NFloat)2147483647.0f, NumberBaseHelper<NFloat>.CreateChecked<nuint>((nuint)0x7FFFFFFF));

                // https://github.com/dotnet/roslyn/issues/60714
                // AssertBitwiseEqual((NFloat)2147483648.0f, NumberBaseHelper<NFloat>.CreateChecked<nuint>((nuint)0x80000000));
                // AssertBitwiseEqual((NFloat)4294967295.0f, NumberBaseHelper<NFloat>.CreateChecked<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateSaturatingFromByteTest()
        {
            AssertBitwiseEqual(Zero, NumberBaseHelper<NFloat>.CreateSaturating<byte>(0x00));
            AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.CreateSaturating<byte>(0x01));
            AssertBitwiseEqual((NFloat)127.0f, NumberBaseHelper<NFloat>.CreateSaturating<byte>(0x7F));
            AssertBitwiseEqual((NFloat)128.0f, NumberBaseHelper<NFloat>.CreateSaturating<byte>(0x80));
            AssertBitwiseEqual((NFloat)255.0f, NumberBaseHelper<NFloat>.CreateSaturating<byte>(0xFF));
        }

        [Fact]
        public static void CreateSaturatingFromCharTest()
        {
            AssertBitwiseEqual(Zero, NumberBaseHelper<NFloat>.CreateSaturating<char>((char)0x0000));
            AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.CreateSaturating<char>((char)0x0001));
            AssertBitwiseEqual((NFloat)32767.0f, NumberBaseHelper<NFloat>.CreateSaturating<char>((char)0x7FFF));
            AssertBitwiseEqual((NFloat)32768.0f, NumberBaseHelper<NFloat>.CreateSaturating<char>((char)0x8000));
            AssertBitwiseEqual((NFloat)65535.0f, NumberBaseHelper<NFloat>.CreateSaturating<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromDecimalTest()
        {
            AssertBitwiseEqual(-1.0f, NumberBaseHelper<NFloat>.CreateSaturating<decimal>(-1.0m));
            AssertBitwiseEqual(-0.0f, NumberBaseHelper<NFloat>.CreateSaturating<decimal>(-0.0m));
            AssertBitwiseEqual(+0.0f, NumberBaseHelper<NFloat>.CreateSaturating<decimal>(+0.0m));
            AssertBitwiseEqual(+1.0f, NumberBaseHelper<NFloat>.CreateSaturating<decimal>(+1.0m));

            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual((NFloat)(-79228162514264337593543950335.0), NumberBaseHelper<NFloat>.CreateSaturating<decimal>(decimal.MinValue));
                AssertBitwiseEqual((NFloat)(+79228162514264337593543950335.0), NumberBaseHelper<NFloat>.CreateSaturating<decimal>(decimal.MaxValue));
            }
            else
            {
                AssertBitwiseEqual(-79228162514264337593543950335.0f, NumberBaseHelper<NFloat>.CreateSaturating<decimal>(decimal.MinValue));
                AssertBitwiseEqual(+79228162514264337593543950335.0f, NumberBaseHelper<NFloat>.CreateSaturating<decimal>(decimal.MaxValue));
            }
        }

        [Fact]
        public static void CreateSaturatingFromDoubleTest()
        {
            AssertBitwiseEqual(NFloat.NegativeInfinity, NumberBaseHelper<NFloat>.CreateSaturating<double>(double.NegativeInfinity));

            AssertBitwiseEqual(-1.0f, NumberBaseHelper<NFloat>.CreateSaturating<double>(-1.0));
            AssertBitwiseEqual(-0.0f, NumberBaseHelper<NFloat>.CreateSaturating<double>(-0.0));
            AssertBitwiseEqual(+0.0f, NumberBaseHelper<NFloat>.CreateSaturating<double>(+0.0));
            AssertBitwiseEqual(+1.0f, NumberBaseHelper<NFloat>.CreateSaturating<double>(+1.0));

            AssertBitwiseEqual(NFloat.PositiveInfinity, NumberBaseHelper<NFloat>.CreateSaturating<double>(double.PositiveInfinity));

            AssertBitwiseEqual(NFloat.NaN, NumberBaseHelper<NFloat>.CreateSaturating<double>(double.NaN));

            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual(NFloat.MinValue, NumberBaseHelper<NFloat>.CreateSaturating<double>(double.MinValue));
                AssertBitwiseEqual(-MinNormal, NumberBaseHelper<NFloat>.CreateSaturating<double>(-2.2250738585072014E-308));
                AssertBitwiseEqual(-MaxSubnormal, NumberBaseHelper<NFloat>.CreateSaturating<double>(-2.2250738585072009E-308));
                AssertBitwiseEqual(-NFloat.Epsilon, NumberBaseHelper<NFloat>.CreateSaturating<double>(-double.Epsilon));

                AssertBitwiseEqual(+NFloat.Epsilon, NumberBaseHelper<NFloat>.CreateSaturating<double>(double.Epsilon));
                AssertBitwiseEqual(+MaxSubnormal, NumberBaseHelper<NFloat>.CreateSaturating<double>(2.2250738585072009E-308));
                AssertBitwiseEqual(+MinNormal, NumberBaseHelper<NFloat>.CreateSaturating<double>(2.2250738585072014E-308));
                AssertBitwiseEqual(NFloat.MaxValue, NumberBaseHelper<NFloat>.CreateSaturating<double>(double.MaxValue));
            }
            else
            {
                AssertBitwiseEqual(NFloat.NegativeInfinity, NumberBaseHelper<NFloat>.CreateSaturating<double>(double.MinValue));
                AssertBitwiseEqual(-0.0f, NumberBaseHelper<NFloat>.CreateSaturating<double>(-2.2250738585072014E-308));
                AssertBitwiseEqual(-0.0f, NumberBaseHelper<NFloat>.CreateSaturating<double>(-2.2250738585072009E-308));
                AssertBitwiseEqual(-0.0f, NumberBaseHelper<NFloat>.CreateSaturating<double>(-double.Epsilon));

                AssertBitwiseEqual(+0.0f, NumberBaseHelper<NFloat>.CreateSaturating<double>(double.Epsilon));
                AssertBitwiseEqual(+0.0f, NumberBaseHelper<NFloat>.CreateSaturating<double>(2.2250738585072009E-308));
                AssertBitwiseEqual(+0.0f, NumberBaseHelper<NFloat>.CreateSaturating<double>(2.2250738585072014E-308));
                AssertBitwiseEqual(NFloat.PositiveInfinity, NumberBaseHelper<NFloat>.CreateSaturating<double>(double.MaxValue));
            }
        }

        [Fact]
        public static void CreateSaturatingFromHalfTest()
        {
            AssertBitwiseEqual(NFloat.NegativeInfinity, NumberBaseHelper<NFloat>.CreateSaturating<Half>(Half.NegativeInfinity));

            AssertBitwiseEqual(-65504.0f, NumberBaseHelper<NFloat>.CreateSaturating<Half>(Half.MinValue));
            AssertBitwiseEqual(-1.0f, NumberBaseHelper<NFloat>.CreateSaturating<Half>(Half.NegativeOne));
            AssertBitwiseEqual(-0.0f, NumberBaseHelper<NFloat>.CreateSaturating<Half>(Half.NegativeZero));

            AssertBitwiseEqual(+0.0f, NumberBaseHelper<NFloat>.CreateSaturating<Half>(Half.Zero));
            AssertBitwiseEqual(+1.0f, NumberBaseHelper<NFloat>.CreateSaturating<Half>(Half.One));
            AssertBitwiseEqual(+65504.0f, NumberBaseHelper<NFloat>.CreateSaturating<Half>(Half.MaxValue));

            AssertBitwiseEqual(NFloat.PositiveInfinity, NumberBaseHelper<NFloat>.CreateSaturating<Half>(Half.PositiveInfinity));

            AssertBitwiseEqual(NFloat.NaN, NumberBaseHelper<NFloat>.CreateSaturating<Half>(Half.NaN));

            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual((NFloat)(-6.103515625E-05), NumberBaseHelper<NFloat>.CreateSaturating<Half>(-BitConverter.UInt16BitsToHalf(0x0400)));
                AssertBitwiseEqual((NFloat)(-6.097555160522461E-05), NumberBaseHelper<NFloat>.CreateSaturating<Half>(-BitConverter.UInt16BitsToHalf(0x03FF)));
                AssertBitwiseEqual((NFloat)(-5.960464477539063E-08), NumberBaseHelper<NFloat>.CreateSaturating<Half>(-Half.Epsilon));

                AssertBitwiseEqual((NFloat)(+5.960464477539063E-08), NumberBaseHelper<NFloat>.CreateSaturating<Half>(Half.Epsilon));
                AssertBitwiseEqual((NFloat)(+6.097555160522461E-05), NumberBaseHelper<NFloat>.CreateSaturating<Half>(BitConverter.UInt16BitsToHalf(0x03FF)));
                AssertBitwiseEqual((NFloat)(+6.103515625E-05), NumberBaseHelper<NFloat>.CreateSaturating<Half>(BitConverter.UInt16BitsToHalf(0x0400)));
            }
            else
            {
                AssertBitwiseEqual(-6.1035156E-05f, NumberBaseHelper<NFloat>.CreateSaturating<Half>(-BitConverter.UInt16BitsToHalf(0x0400)));
                AssertBitwiseEqual(-6.097555E-05f, NumberBaseHelper<NFloat>.CreateSaturating<Half>(-BitConverter.UInt16BitsToHalf(0x03FF)));
                AssertBitwiseEqual(-5.9604645E-08f, NumberBaseHelper<NFloat>.CreateSaturating<Half>(-Half.Epsilon));

                AssertBitwiseEqual(+5.9604645E-08f, NumberBaseHelper<NFloat>.CreateSaturating<Half>(Half.Epsilon));
                AssertBitwiseEqual(+6.097555E-05f, NumberBaseHelper<NFloat>.CreateSaturating<Half>(BitConverter.UInt16BitsToHalf(0x03FF)));
                AssertBitwiseEqual(+6.1035156E-05f, NumberBaseHelper<NFloat>.CreateSaturating<Half>(BitConverter.UInt16BitsToHalf(0x0400)));
            }
        }

        [Fact]
        public static void CreateSaturatingFromInt16Test()
        {
            AssertBitwiseEqual(Zero, NumberBaseHelper<NFloat>.CreateSaturating<short>(0x0000));
            AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.CreateSaturating<short>(0x0001));
            AssertBitwiseEqual((NFloat)32767.0f, NumberBaseHelper<NFloat>.CreateSaturating<short>(0x7FFF));
            AssertBitwiseEqual((NFloat)(-32768.0f), NumberBaseHelper<NFloat>.CreateSaturating<short>(unchecked((short)0x8000)));
            AssertBitwiseEqual(NegativeOne, NumberBaseHelper<NFloat>.CreateSaturating<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromInt32Test()
        {
            AssertBitwiseEqual(Zero, NumberBaseHelper<NFloat>.CreateSaturating<int>(0x00000000));
            AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.CreateSaturating<int>(0x00000001));
            AssertBitwiseEqual(NegativeOne, NumberBaseHelper<NFloat>.CreateSaturating<int>(unchecked((int)0xFFFFFFFF)));

            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual((NFloat)2147483647.0, NumberBaseHelper<NFloat>.CreateSaturating<int>(0x7FFFFFFF));
                AssertBitwiseEqual((NFloat)(-2147483648.0), NumberBaseHelper<NFloat>.CreateSaturating<int>(unchecked((int)0x80000000)));
            }
            else
            {
                AssertBitwiseEqual((NFloat)2147483647.0f, NumberBaseHelper<NFloat>.CreateSaturating<int>(0x7FFFFFFF));
                AssertBitwiseEqual((NFloat)(-2147483648.0f), NumberBaseHelper<NFloat>.CreateSaturating<int>(unchecked((int)0x80000000)));
            }
        }

        [Fact]
        public static void CreateSaturatingFromInt64Test()
        {
            AssertBitwiseEqual(Zero, NumberBaseHelper<NFloat>.CreateSaturating<long>(0x0000000000000000));
            AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.CreateSaturating<long>(0x0000000000000001));
            AssertBitwiseEqual(NegativeOne, NumberBaseHelper<NFloat>.CreateSaturating<long>(unchecked(unchecked((long)0xFFFFFFFFFFFFFFFF))));

            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual((NFloat)9223372036854775807.0, NumberBaseHelper<NFloat>.CreateSaturating<long>(0x7FFFFFFFFFFFFFFF));
                AssertBitwiseEqual((NFloat)(-9223372036854775808.0), NumberBaseHelper<NFloat>.CreateSaturating<long>(unchecked(unchecked((long)0x8000000000000000))));
            }
            else
            {
                AssertBitwiseEqual((NFloat)9223372036854775807.0f, NumberBaseHelper<NFloat>.CreateSaturating<long>(0x7FFFFFFFFFFFFFFF));
                AssertBitwiseEqual((NFloat)(-9223372036854775808.0f), NumberBaseHelper<NFloat>.CreateSaturating<long>(unchecked(unchecked((long)0x8000000000000000))));
            }
        }

        [Fact]
        public static void CreateSaturatingFromInt128Test()
        {
            AssertBitwiseEqual(+0.0f, NumberBaseHelper<NFloat>.CreateSaturating<Int128>(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000)));
            AssertBitwiseEqual(+1.0f, NumberBaseHelper<NFloat>.CreateSaturating<Int128>(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001)));
            AssertBitwiseEqual(-1.0f, NumberBaseHelper<NFloat>.CreateSaturating<Int128>(new Int128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)));

            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual((NFloat)(+170141183460469231731687303715884105727.0), NumberBaseHelper<NFloat>.CreateSaturating<Int128>(new Int128(0x7FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)));
                AssertBitwiseEqual((NFloat)(-170141183460469231731687303715884105728.0), NumberBaseHelper<NFloat>.CreateSaturating<Int128>(new Int128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000)));
            }
            else
            {
                AssertBitwiseEqual(+170141183460469231731687303715884105727.0f, NumberBaseHelper<NFloat>.CreateSaturating<Int128>(new Int128(0x7FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)));
                AssertBitwiseEqual(-170141183460469231731687303715884105728.0f, NumberBaseHelper<NFloat>.CreateSaturating<Int128>(new Int128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000)));
            }
        }

        [Fact]
        public static void CreateSaturatingFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual(Zero, NumberBaseHelper<NFloat>.CreateSaturating<nint>(unchecked((nint)0x0000000000000000)));
                AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.CreateSaturating<nint>(unchecked((nint)0x0000000000000001)));
                AssertBitwiseEqual((NFloat)9223372036854775807.0, NumberBaseHelper<NFloat>.CreateSaturating<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                AssertBitwiseEqual((NFloat)(-9223372036854775808.0), NumberBaseHelper<NFloat>.CreateSaturating<nint>(unchecked((nint)0x8000000000000000)));
                AssertBitwiseEqual(NegativeOne, NumberBaseHelper<NFloat>.CreateSaturating<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                AssertBitwiseEqual(Zero, NumberBaseHelper<NFloat>.CreateSaturating<nint>((nint)0x00000000));
                AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.CreateSaturating<nint>((nint)0x00000001));
                AssertBitwiseEqual((NFloat)2147483647.0f, NumberBaseHelper<NFloat>.CreateSaturating<nint>((nint)0x7FFFFFFF));
                AssertBitwiseEqual((NFloat)(-2147483648.0f), NumberBaseHelper<NFloat>.CreateSaturating<nint>(unchecked((nint)0x80000000)));
                AssertBitwiseEqual(NegativeOne, NumberBaseHelper<NFloat>.CreateSaturating<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateSaturatingFromNFloatTest()
        {
            AssertBitwiseEqual(NFloat.NegativeInfinity, NumberBaseHelper<NFloat>.CreateSaturating<NFloat>(NFloat.NegativeInfinity));

            AssertBitwiseEqual(-1.0f, NumberBaseHelper<NFloat>.CreateSaturating<NFloat>(-1.0f));
            AssertBitwiseEqual(-0.0f, NumberBaseHelper<NFloat>.CreateSaturating<NFloat>(-0.0f));

            AssertBitwiseEqual(+0.0f, NumberBaseHelper<NFloat>.CreateSaturating<NFloat>(+0.0f));
            AssertBitwiseEqual(+1.0f, NumberBaseHelper<NFloat>.CreateSaturating<NFloat>(1.0f));

            AssertBitwiseEqual(NFloat.MinValue, NumberBaseHelper<NFloat>.CreateSaturating<NFloat>(NFloat.MinValue));

            AssertBitwiseEqual(-MinNormal, NumberBaseHelper<NFloat>.CreateSaturating<NFloat>((NFloat)(-MinNormal)));
            AssertBitwiseEqual(-MaxSubnormal, NumberBaseHelper<NFloat>.CreateSaturating<NFloat>((NFloat)(-MaxSubnormal)));
            AssertBitwiseEqual(-NFloat.Epsilon, NumberBaseHelper<NFloat>.CreateSaturating<NFloat>(-NFloat.Epsilon));

            AssertBitwiseEqual(+NFloat.Epsilon, NumberBaseHelper<NFloat>.CreateSaturating<NFloat>(NFloat.Epsilon));
            AssertBitwiseEqual(+MaxSubnormal, NumberBaseHelper<NFloat>.CreateSaturating<NFloat>((NFloat)MaxSubnormal));
            AssertBitwiseEqual(+MinNormal, NumberBaseHelper<NFloat>.CreateSaturating<NFloat>((NFloat)MinNormal));

            AssertBitwiseEqual(NFloat.MaxValue, NumberBaseHelper<NFloat>.CreateSaturating<NFloat>(NFloat.MaxValue));

            AssertBitwiseEqual(NFloat.PositiveInfinity, NumberBaseHelper<NFloat>.CreateSaturating<NFloat>(NFloat.PositiveInfinity));

            AssertBitwiseEqual(NFloat.NaN, NumberBaseHelper<NFloat>.CreateSaturating<NFloat>(NFloat.NaN));
        }

        [Fact]
        public static void CreateSaturatingFromSByteTest()
        {
            AssertBitwiseEqual(Zero, NumberBaseHelper<NFloat>.CreateSaturating<sbyte>(0x00));
            AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.CreateSaturating<sbyte>(0x01));
            AssertBitwiseEqual((NFloat)127.0f, NumberBaseHelper<NFloat>.CreateSaturating<sbyte>(0x7F));
            AssertBitwiseEqual((NFloat)(-128.0f), NumberBaseHelper<NFloat>.CreateSaturating<sbyte>(unchecked((sbyte)0x80)));
            AssertBitwiseEqual(NegativeOne, NumberBaseHelper<NFloat>.CreateSaturating<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateSaturatingFromSingleTest()
        {
            AssertBitwiseEqual(NFloat.NegativeInfinity, NumberBaseHelper<NFloat>.CreateSaturating<float>(float.NegativeInfinity));

            AssertBitwiseEqual(-3.4028234663852886E+38f, NumberBaseHelper<NFloat>.CreateSaturating<float>(float.MinValue));
            AssertBitwiseEqual(-1.0f, NumberBaseHelper<NFloat>.CreateSaturating<float>(-1.0f));

            AssertBitwiseEqual(-1.1754943508222875E-38f, NumberBaseHelper<NFloat>.CreateSaturating<float>(-1.17549435E-38f));
            AssertBitwiseEqual(-1.1754942106924411E-38f, NumberBaseHelper<NFloat>.CreateSaturating<float>(-1.17549421E-38f));
            AssertBitwiseEqual(-1.401298464324817E-45f, NumberBaseHelper<NFloat>.CreateSaturating<float>(-float.Epsilon));
            AssertBitwiseEqual(-0.0f, NumberBaseHelper<NFloat>.CreateSaturating<float>(-0.0f));

            AssertBitwiseEqual(+0.0f, NumberBaseHelper<NFloat>.CreateSaturating<float>(+0.0f));
            AssertBitwiseEqual(+1.401298464324817E-45f, NumberBaseHelper<NFloat>.CreateSaturating<float>(float.Epsilon));
            AssertBitwiseEqual(+1.1754942106924411E-38f, NumberBaseHelper<NFloat>.CreateSaturating<float>(1.17549421E-38f));
            AssertBitwiseEqual(+1.1754943508222875E-38f, NumberBaseHelper<NFloat>.CreateSaturating<float>(1.17549435E-38f));

            AssertBitwiseEqual(+1.0f, NumberBaseHelper<NFloat>.CreateSaturating<float>(1.0f));
            AssertBitwiseEqual(+3.4028234663852886E+38f, NumberBaseHelper<NFloat>.CreateSaturating<float>(float.MaxValue));

            AssertBitwiseEqual(NFloat.PositiveInfinity, NumberBaseHelper<NFloat>.CreateSaturating<float>(float.PositiveInfinity));

            AssertBitwiseEqual(NFloat.NaN, NumberBaseHelper<NFloat>.CreateSaturating<float>(float.NaN));
        }

        [Fact]
        public static void CreateSaturatingFromUInt16Test()
        {
            AssertBitwiseEqual(Zero, NumberBaseHelper<NFloat>.CreateSaturating<ushort>(0x0000));
            AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.CreateSaturating<ushort>(0x0001));
            AssertBitwiseEqual((NFloat)32767.0f, NumberBaseHelper<NFloat>.CreateSaturating<ushort>(0x7FFF));
            AssertBitwiseEqual((NFloat)32768.0f, NumberBaseHelper<NFloat>.CreateSaturating<ushort>(0x8000));
            AssertBitwiseEqual((NFloat)65535.0f, NumberBaseHelper<NFloat>.CreateSaturating<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUInt32Test()
        {
            AssertBitwiseEqual(Zero, NumberBaseHelper<NFloat>.CreateSaturating<uint>(0x00000000));
            AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.CreateSaturating<uint>(0x00000001));

            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual((NFloat)2147483647.0, NumberBaseHelper<NFloat>.CreateSaturating<uint>(0x7FFFFFFF));
                AssertBitwiseEqual((NFloat)2147483648.0, NumberBaseHelper<NFloat>.CreateSaturating<uint>(0x80000000));
                AssertBitwiseEqual((NFloat)4294967295.0, NumberBaseHelper<NFloat>.CreateSaturating<uint>(0xFFFFFFFF));
            }
            else
            {
                AssertBitwiseEqual((NFloat)2147483647.0f, NumberBaseHelper<NFloat>.CreateSaturating<uint>(0x7FFFFFFF));
                AssertBitwiseEqual((NFloat)2147483648.0f, NumberBaseHelper<NFloat>.CreateSaturating<uint>(0x80000000));
                AssertBitwiseEqual((NFloat)4294967295.0f, NumberBaseHelper<NFloat>.CreateSaturating<uint>(0xFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateSaturatingFromUInt64Test()
        {
            AssertBitwiseEqual(Zero, NumberBaseHelper<NFloat>.CreateSaturating<ulong>(0x0000000000000000));
            AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.CreateSaturating<ulong>(0x0000000000000001));

            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual((NFloat)9223372036854775807.0, NumberBaseHelper<NFloat>.CreateSaturating<ulong>(0x7FFFFFFFFFFFFFFF));
                AssertBitwiseEqual((NFloat)9223372036854775808.0, NumberBaseHelper<NFloat>.CreateSaturating<ulong>(0x8000000000000000));
                AssertBitwiseEqual((NFloat)18446744073709551615.0, NumberBaseHelper<NFloat>.CreateSaturating<ulong>(0xFFFFFFFFFFFFFFFF));
            }
            else
            {
                AssertBitwiseEqual((NFloat)9223372036854775807.0f, NumberBaseHelper<NFloat>.CreateSaturating<ulong>(0x7FFFFFFFFFFFFFFF));
                AssertBitwiseEqual((NFloat)9223372036854775808.0f, NumberBaseHelper<NFloat>.CreateSaturating<ulong>(0x8000000000000000));
                AssertBitwiseEqual((NFloat)18446744073709551615.0f, NumberBaseHelper<NFloat>.CreateSaturating<ulong>(0xFFFFFFFFFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateSaturatingFromUInt128Test()
        {
            AssertBitwiseEqual(0.0f, NumberBaseHelper<NFloat>.CreateSaturating<UInt128>(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000)));
            AssertBitwiseEqual(1.0f, NumberBaseHelper<NFloat>.CreateSaturating<UInt128>(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001)));

            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual((NFloat)(170141183460469231731687303715884105727.0), NumberBaseHelper<NFloat>.CreateSaturating<UInt128>(new UInt128(0x7FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)));
                AssertBitwiseEqual((NFloat)(170141183460469231731687303715884105728.0), NumberBaseHelper<NFloat>.CreateSaturating<UInt128>(new UInt128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000)));
                AssertBitwiseEqual((NFloat)(340282366920938463463374607431768211455.0), NumberBaseHelper<NFloat>.CreateSaturating<UInt128>(new UInt128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)));
            }
            else
            {
                AssertBitwiseEqual(170141183460469231731687303715884105727.0f, NumberBaseHelper<NFloat>.CreateSaturating<UInt128>(new UInt128(0x7FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)));
                AssertBitwiseEqual(170141183460469231731687303715884105728.0f, NumberBaseHelper<NFloat>.CreateSaturating<UInt128>(new UInt128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000)));
                AssertBitwiseEqual(float.PositiveInfinity, NumberBaseHelper<NFloat>.CreateSaturating<UInt128>(new UInt128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)));
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/69795", TestRuntimes.Mono)]
        public static void CreateSaturatingFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual(Zero, NumberBaseHelper<NFloat>.CreateSaturating<nuint>(unchecked((nuint)0x0000000000000000)));
                AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.CreateSaturating<nuint>(unchecked((nuint)0x0000000000000001)));
                AssertBitwiseEqual((NFloat)9223372036854775807.0, NumberBaseHelper<NFloat>.CreateSaturating<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));

                // https://github.com/dotnet/roslyn/issues/60714
                // AssertBitwiseEqual((NFloat)9223372036854775808.0, NumberBaseHelper<NFloat>.CreateSaturating<nuint>(unchecked((nuint)0x8000000000000000)));
                // AssertBitwiseEqual((NFloat)18446744073709551615.0, NumberBaseHelper<NFloat>.CreateSaturating<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                AssertBitwiseEqual(Zero, NumberBaseHelper<NFloat>.CreateSaturating<nuint>((nuint)0x00000000));
                AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.CreateSaturating<nuint>((nuint)0x00000001));
                AssertBitwiseEqual((NFloat)2147483647.0f, NumberBaseHelper<NFloat>.CreateSaturating<nuint>((nuint)0x7FFFFFFF));

                // https://github.com/dotnet/roslyn/issues/60714
                // AssertBitwiseEqual((NFloat)2147483648.0f, NumberBaseHelper<NFloat>.CreateSaturating<nuint>((nuint)0x80000000));
                // AssertBitwiseEqual((NFloat)4294967295.0f, NumberBaseHelper<NFloat>.CreateSaturating<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateTruncatingFromByteTest()
        {
            AssertBitwiseEqual(Zero, NumberBaseHelper<NFloat>.CreateTruncating<byte>(0x00));
            AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.CreateTruncating<byte>(0x01));
            AssertBitwiseEqual((NFloat)127.0f, NumberBaseHelper<NFloat>.CreateTruncating<byte>(0x7F));
            AssertBitwiseEqual((NFloat)128.0f, NumberBaseHelper<NFloat>.CreateTruncating<byte>(0x80));
            AssertBitwiseEqual((NFloat)255.0f, NumberBaseHelper<NFloat>.CreateTruncating<byte>(0xFF));
        }

        [Fact]
        public static void CreateTruncatingFromCharTest()
        {
            AssertBitwiseEqual(Zero, NumberBaseHelper<NFloat>.CreateTruncating<char>((char)0x0000));
            AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.CreateTruncating<char>((char)0x0001));
            AssertBitwiseEqual((NFloat)32767.0f, NumberBaseHelper<NFloat>.CreateTruncating<char>((char)0x7FFF));
            AssertBitwiseEqual((NFloat)32768.0f, NumberBaseHelper<NFloat>.CreateTruncating<char>((char)0x8000));
            AssertBitwiseEqual((NFloat)65535.0f, NumberBaseHelper<NFloat>.CreateTruncating<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromDecimalTest()
        {
            AssertBitwiseEqual(-1.0f, NumberBaseHelper<NFloat>.CreateTruncating<decimal>(-1.0m));
            AssertBitwiseEqual(-0.0f, NumberBaseHelper<NFloat>.CreateTruncating<decimal>(-0.0m));
            AssertBitwiseEqual(+0.0f, NumberBaseHelper<NFloat>.CreateTruncating<decimal>(+0.0m));
            AssertBitwiseEqual(+1.0f, NumberBaseHelper<NFloat>.CreateTruncating<decimal>(+1.0m));

            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual((NFloat)(-79228162514264337593543950335.0), NumberBaseHelper<NFloat>.CreateTruncating<decimal>(decimal.MinValue));
                AssertBitwiseEqual((NFloat)(+79228162514264337593543950335.0), NumberBaseHelper<NFloat>.CreateTruncating<decimal>(decimal.MaxValue));
            }
            else
            {
                AssertBitwiseEqual(-79228162514264337593543950335.0f, NumberBaseHelper<NFloat>.CreateTruncating<decimal>(decimal.MinValue));
                AssertBitwiseEqual(+79228162514264337593543950335.0f, NumberBaseHelper<NFloat>.CreateTruncating<decimal>(decimal.MaxValue));
            }
        }

        [Fact]
        public static void CreateTruncatingFromDoubleTest()
        {
            AssertBitwiseEqual(NFloat.NegativeInfinity, NumberBaseHelper<NFloat>.CreateTruncating<double>(double.NegativeInfinity));

            AssertBitwiseEqual(-1.0f, NumberBaseHelper<NFloat>.CreateTruncating<double>(-1.0));
            AssertBitwiseEqual(-0.0f, NumberBaseHelper<NFloat>.CreateTruncating<double>(-0.0));
            AssertBitwiseEqual(+0.0f, NumberBaseHelper<NFloat>.CreateTruncating<double>(+0.0));
            AssertBitwiseEqual(+1.0f, NumberBaseHelper<NFloat>.CreateTruncating<double>(1.0));

            AssertBitwiseEqual(NFloat.PositiveInfinity, NumberBaseHelper<NFloat>.CreateTruncating<double>(double.PositiveInfinity));

            AssertBitwiseEqual(NFloat.NaN, NumberBaseHelper<NFloat>.CreateTruncating<double>(double.NaN));

            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual(NFloat.MinValue, NumberBaseHelper<NFloat>.CreateTruncating<double>(double.MinValue));
                AssertBitwiseEqual(-MinNormal, NumberBaseHelper<NFloat>.CreateTruncating<double>(-2.2250738585072014E-308));
                AssertBitwiseEqual(-MaxSubnormal, NumberBaseHelper<NFloat>.CreateTruncating<double>(-2.2250738585072009E-308));
                AssertBitwiseEqual(-NFloat.Epsilon, NumberBaseHelper<NFloat>.CreateTruncating<double>(-double.Epsilon));

                AssertBitwiseEqual(+NFloat.Epsilon, NumberBaseHelper<NFloat>.CreateTruncating<double>(double.Epsilon));
                AssertBitwiseEqual(+MaxSubnormal, NumberBaseHelper<NFloat>.CreateTruncating<double>(2.2250738585072009E-308));
                AssertBitwiseEqual(+MinNormal, NumberBaseHelper<NFloat>.CreateTruncating<double>(2.2250738585072014E-308));
                AssertBitwiseEqual(NFloat.MaxValue, NumberBaseHelper<NFloat>.CreateTruncating<double>(double.MaxValue));
            }
            else
            {
                AssertBitwiseEqual(NFloat.NegativeInfinity, NumberBaseHelper<NFloat>.CreateTruncating<double>(double.MinValue));
                AssertBitwiseEqual(-0.0f, NumberBaseHelper<NFloat>.CreateTruncating<double>(-2.2250738585072014E-308));
                AssertBitwiseEqual(-0.0f, NumberBaseHelper<NFloat>.CreateTruncating<double>(-2.2250738585072009E-308));
                AssertBitwiseEqual(-0.0f, NumberBaseHelper<NFloat>.CreateTruncating<double>(-double.Epsilon));

                AssertBitwiseEqual(+0.0f, NumberBaseHelper<NFloat>.CreateTruncating<double>(double.Epsilon));
                AssertBitwiseEqual(+0.0f, NumberBaseHelper<NFloat>.CreateTruncating<double>(2.2250738585072009E-308));
                AssertBitwiseEqual(+0.0f, NumberBaseHelper<NFloat>.CreateTruncating<double>(2.2250738585072014E-308));
                AssertBitwiseEqual(NFloat.PositiveInfinity, NumberBaseHelper<NFloat>.CreateTruncating<double>(double.MaxValue));
            }
        }

        [Fact]
        public static void CreateTruncatingFromHalfTest()
        {
            AssertBitwiseEqual(NFloat.NegativeInfinity, NumberBaseHelper<NFloat>.CreateTruncating<Half>(Half.NegativeInfinity));

            AssertBitwiseEqual(-65504.0f, NumberBaseHelper<NFloat>.CreateTruncating<Half>(Half.MinValue));
            AssertBitwiseEqual(-1.0f, NumberBaseHelper<NFloat>.CreateTruncating<Half>(Half.NegativeOne));
            AssertBitwiseEqual(-0.0f, NumberBaseHelper<NFloat>.CreateTruncating<Half>(Half.NegativeZero));

            AssertBitwiseEqual(+0.0f, NumberBaseHelper<NFloat>.CreateTruncating<Half>(Half.Zero));
            AssertBitwiseEqual(+1.0f, NumberBaseHelper<NFloat>.CreateTruncating<Half>(Half.One));
            AssertBitwiseEqual(+65504.0f, NumberBaseHelper<NFloat>.CreateTruncating<Half>(Half.MaxValue));

            AssertBitwiseEqual(NFloat.PositiveInfinity, NumberBaseHelper<NFloat>.CreateTruncating<Half>(Half.PositiveInfinity));

            AssertBitwiseEqual(NFloat.NaN, NumberBaseHelper<NFloat>.CreateTruncating<Half>(Half.NaN));

            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual((NFloat)(-6.103515625E-05), NumberBaseHelper<NFloat>.CreateTruncating<Half>(-BitConverter.UInt16BitsToHalf(0x0400)));
                AssertBitwiseEqual((NFloat)(-6.097555160522461E-05), NumberBaseHelper<NFloat>.CreateTruncating<Half>(-BitConverter.UInt16BitsToHalf(0x03FF)));
                AssertBitwiseEqual((NFloat)(-5.960464477539063E-08), NumberBaseHelper<NFloat>.CreateTruncating<Half>(-Half.Epsilon));

                AssertBitwiseEqual((NFloat)(+5.960464477539063E-08), NumberBaseHelper<NFloat>.CreateTruncating<Half>(Half.Epsilon));
                AssertBitwiseEqual((NFloat)(+6.097555160522461E-05), NumberBaseHelper<NFloat>.CreateTruncating<Half>(BitConverter.UInt16BitsToHalf(0x03FF)));
                AssertBitwiseEqual((NFloat)(+6.103515625E-05), NumberBaseHelper<NFloat>.CreateTruncating<Half>(BitConverter.UInt16BitsToHalf(0x0400)));
            }
            else
            {
                AssertBitwiseEqual(-6.1035156E-05f, NumberBaseHelper<NFloat>.CreateTruncating<Half>(-BitConverter.UInt16BitsToHalf(0x0400)));
                AssertBitwiseEqual(-6.097555E-05f, NumberBaseHelper<NFloat>.CreateTruncating<Half>(-BitConverter.UInt16BitsToHalf(0x03FF)));
                AssertBitwiseEqual(-5.9604645E-08f, NumberBaseHelper<NFloat>.CreateTruncating<Half>(-Half.Epsilon));

                AssertBitwiseEqual(+5.9604645E-08f, NumberBaseHelper<NFloat>.CreateTruncating<Half>(Half.Epsilon));
                AssertBitwiseEqual(+6.097555E-05f, NumberBaseHelper<NFloat>.CreateTruncating<Half>(BitConverter.UInt16BitsToHalf(0x03FF)));
                AssertBitwiseEqual(+6.1035156E-05f, NumberBaseHelper<NFloat>.CreateTruncating<Half>(BitConverter.UInt16BitsToHalf(0x0400)));
            }
        }

        [Fact]
        public static void CreateTruncatingFromInt16Test()
        {
            AssertBitwiseEqual(Zero, NumberBaseHelper<NFloat>.CreateTruncating<short>(0x0000));
            AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.CreateTruncating<short>(0x0001));
            AssertBitwiseEqual((NFloat)32767.0f, NumberBaseHelper<NFloat>.CreateTruncating<short>(0x7FFF));
            AssertBitwiseEqual((NFloat)(-32768.0f), NumberBaseHelper<NFloat>.CreateTruncating<short>(unchecked((short)0x8000)));
            AssertBitwiseEqual(NegativeOne, NumberBaseHelper<NFloat>.CreateTruncating<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateTruncatingFromInt32Test()
        {
            AssertBitwiseEqual(Zero, NumberBaseHelper<NFloat>.CreateTruncating<int>(0x00000000));
            AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.CreateTruncating<int>(0x00000001));
            AssertBitwiseEqual(NegativeOne, NumberBaseHelper<NFloat>.CreateTruncating<int>(unchecked((int)0xFFFFFFFF)));

            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual((NFloat)2147483647.0, NumberBaseHelper<NFloat>.CreateTruncating<int>(0x7FFFFFFF));
                AssertBitwiseEqual((NFloat)(-2147483648.0), NumberBaseHelper<NFloat>.CreateTruncating<int>(unchecked((int)0x80000000)));
            }
            else
            {
                AssertBitwiseEqual((NFloat)2147483647.0f, NumberBaseHelper<NFloat>.CreateTruncating<int>(0x7FFFFFFF));
                AssertBitwiseEqual((NFloat)(-2147483648.0f), NumberBaseHelper<NFloat>.CreateTruncating<int>(unchecked((int)0x80000000)));
            }
        }

        [Fact]
        public static void CreateTruncatingFromInt64Test()
        {
            AssertBitwiseEqual(Zero, NumberBaseHelper<NFloat>.CreateTruncating<long>(0x0000000000000000));
            AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.CreateTruncating<long>(0x0000000000000001));
            AssertBitwiseEqual(NegativeOne, NumberBaseHelper<NFloat>.CreateTruncating<long>(unchecked(unchecked((long)0xFFFFFFFFFFFFFFFF))));

            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual((NFloat)9223372036854775807.0, NumberBaseHelper<NFloat>.CreateTruncating<long>(0x7FFFFFFFFFFFFFFF));
                AssertBitwiseEqual((NFloat)(-9223372036854775808.0), NumberBaseHelper<NFloat>.CreateTruncating<long>(unchecked(unchecked((long)0x8000000000000000))));
            }
            else
            {
                AssertBitwiseEqual((NFloat)9223372036854775807.0f, NumberBaseHelper<NFloat>.CreateTruncating<long>(0x7FFFFFFFFFFFFFFF));
                AssertBitwiseEqual((NFloat)(-9223372036854775808.0f), NumberBaseHelper<NFloat>.CreateTruncating<long>(unchecked(unchecked((long)0x8000000000000000))));
            }
        }

        [Fact]
        public static void CreateTruncatingFromInt128Test()
        {
            AssertBitwiseEqual(+0.0f, NumberBaseHelper<NFloat>.CreateTruncating<Int128>(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000)));
            AssertBitwiseEqual(+1.0f, NumberBaseHelper<NFloat>.CreateTruncating<Int128>(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001)));
            AssertBitwiseEqual(-1.0f, NumberBaseHelper<NFloat>.CreateTruncating<Int128>(new Int128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)));

            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual((NFloat)(+170141183460469231731687303715884105727.0), NumberBaseHelper<NFloat>.CreateTruncating<Int128>(new Int128(0x7FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)));
                AssertBitwiseEqual((NFloat)(-170141183460469231731687303715884105728.0), NumberBaseHelper<NFloat>.CreateTruncating<Int128>(new Int128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000)));
            }
            else
            {
                AssertBitwiseEqual(+170141183460469231731687303715884105727.0f, NumberBaseHelper<NFloat>.CreateTruncating<Int128>(new Int128(0x7FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)));
                AssertBitwiseEqual(-170141183460469231731687303715884105728.0f, NumberBaseHelper<NFloat>.CreateTruncating<Int128>(new Int128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000)));
            }
        }

        [Fact]
        public static void CreateTruncatingFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual(Zero, NumberBaseHelper<NFloat>.CreateTruncating<nint>(unchecked((nint)0x0000000000000000)));
                AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.CreateTruncating<nint>(unchecked((nint)0x0000000000000001)));
                AssertBitwiseEqual((NFloat)9223372036854775807.0, NumberBaseHelper<NFloat>.CreateTruncating<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                AssertBitwiseEqual((NFloat)(-9223372036854775808.0), NumberBaseHelper<NFloat>.CreateTruncating<nint>(unchecked((nint)0x8000000000000000)));
                AssertBitwiseEqual(NegativeOne, NumberBaseHelper<NFloat>.CreateTruncating<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                AssertBitwiseEqual(Zero, NumberBaseHelper<NFloat>.CreateTruncating<nint>((nint)0x00000000));
                AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.CreateTruncating<nint>((nint)0x00000001));
                AssertBitwiseEqual((NFloat)2147483647.0f, NumberBaseHelper<NFloat>.CreateTruncating<nint>((nint)0x7FFFFFFF));
                AssertBitwiseEqual((NFloat)(-2147483648.0f), NumberBaseHelper<NFloat>.CreateTruncating<nint>(unchecked((nint)0x80000000)));
                AssertBitwiseEqual(NegativeOne, NumberBaseHelper<NFloat>.CreateTruncating<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateTruncatingFromNFloatTest()
        {
            AssertBitwiseEqual(NFloat.NegativeInfinity, NumberBaseHelper<NFloat>.CreateTruncating<NFloat>(NFloat.NegativeInfinity));

            AssertBitwiseEqual(-1.0f, NumberBaseHelper<NFloat>.CreateTruncating<NFloat>(-1.0f));
            AssertBitwiseEqual(-0.0f, NumberBaseHelper<NFloat>.CreateTruncating<NFloat>(-0.0f));

            AssertBitwiseEqual(+0.0f, NumberBaseHelper<NFloat>.CreateTruncating<NFloat>(+0.0f));
            AssertBitwiseEqual(+1.0f, NumberBaseHelper<NFloat>.CreateTruncating<NFloat>(1.0f));

            AssertBitwiseEqual(NFloat.MinValue, NumberBaseHelper<NFloat>.CreateTruncating<NFloat>(NFloat.MinValue));

            AssertBitwiseEqual(-MinNormal, NumberBaseHelper<NFloat>.CreateTruncating<NFloat>((NFloat)(-MinNormal)));
            AssertBitwiseEqual(-MaxSubnormal, NumberBaseHelper<NFloat>.CreateTruncating<NFloat>((NFloat)(-MaxSubnormal)));
            AssertBitwiseEqual(-NFloat.Epsilon, NumberBaseHelper<NFloat>.CreateTruncating<NFloat>(-NFloat.Epsilon));

            AssertBitwiseEqual(+NFloat.Epsilon, NumberBaseHelper<NFloat>.CreateTruncating<NFloat>(NFloat.Epsilon));
            AssertBitwiseEqual(+MaxSubnormal, NumberBaseHelper<NFloat>.CreateTruncating<NFloat>((NFloat)MaxSubnormal));
            AssertBitwiseEqual(+MinNormal, NumberBaseHelper<NFloat>.CreateTruncating<NFloat>((NFloat)MinNormal));

            AssertBitwiseEqual(NFloat.MaxValue, NumberBaseHelper<NFloat>.CreateTruncating<NFloat>(NFloat.MaxValue));

            AssertBitwiseEqual(NFloat.PositiveInfinity, NumberBaseHelper<NFloat>.CreateTruncating<NFloat>(NFloat.PositiveInfinity));

            AssertBitwiseEqual(NFloat.NaN, NumberBaseHelper<NFloat>.CreateTruncating<NFloat>(NFloat.NaN));
        }

        [Fact]
        public static void CreateTruncatingFromSByteTest()
        {
            AssertBitwiseEqual(Zero, NumberBaseHelper<NFloat>.CreateTruncating<sbyte>(0x00));
            AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.CreateTruncating<sbyte>(0x01));
            AssertBitwiseEqual((NFloat)127.0f, NumberBaseHelper<NFloat>.CreateTruncating<sbyte>(0x7F));
            AssertBitwiseEqual((NFloat)(-128.0f), NumberBaseHelper<NFloat>.CreateTruncating<sbyte>(unchecked((sbyte)0x80)));
            AssertBitwiseEqual(NegativeOne, NumberBaseHelper<NFloat>.CreateTruncating<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateTruncatingFromSingleTest()
        {
            AssertBitwiseEqual(NFloat.NegativeInfinity, NumberBaseHelper<NFloat>.CreateTruncating<float>(float.NegativeInfinity));

            AssertBitwiseEqual(-3.4028234663852886E+38f, NumberBaseHelper<NFloat>.CreateTruncating<float>(float.MinValue));
            AssertBitwiseEqual(-1.0f, NumberBaseHelper<NFloat>.CreateTruncating<float>(-1.0f));

            AssertBitwiseEqual(-1.1754943508222875E-38f, NumberBaseHelper<NFloat>.CreateTruncating<float>(-1.17549435E-38f));
            AssertBitwiseEqual(-1.1754942106924411E-38f, NumberBaseHelper<NFloat>.CreateTruncating<float>(-1.17549421E-38f));
            AssertBitwiseEqual(-1.401298464324817E-45f, NumberBaseHelper<NFloat>.CreateTruncating<float>(-float.Epsilon));
            AssertBitwiseEqual(-0.0f, NumberBaseHelper<NFloat>.CreateTruncating<float>(-0.0f));

            AssertBitwiseEqual(+0.0f, NumberBaseHelper<NFloat>.CreateTruncating<float>(+0.0f));
            AssertBitwiseEqual(+1.401298464324817E-45f, NumberBaseHelper<NFloat>.CreateTruncating<float>(float.Epsilon));
            AssertBitwiseEqual(+1.1754942106924411E-38f, NumberBaseHelper<NFloat>.CreateTruncating<float>(1.17549421E-38f));
            AssertBitwiseEqual(+1.1754943508222875E-38f, NumberBaseHelper<NFloat>.CreateTruncating<float>(1.17549435E-38f));

            AssertBitwiseEqual(+1.0f, NumberBaseHelper<NFloat>.CreateTruncating<float>(1.0f));
            AssertBitwiseEqual(+3.4028234663852886E+38f, NumberBaseHelper<NFloat>.CreateTruncating<float>(float.MaxValue));

            AssertBitwiseEqual(NFloat.PositiveInfinity, NumberBaseHelper<NFloat>.CreateTruncating<float>(float.PositiveInfinity));

            AssertBitwiseEqual(NFloat.NaN, NumberBaseHelper<NFloat>.CreateTruncating<float>(float.NaN));
        }

        [Fact]
        public static void CreateTruncatingFromUInt16Test()
        {
            AssertBitwiseEqual(Zero, NumberBaseHelper<NFloat>.CreateTruncating<ushort>(0x0000));
            AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.CreateTruncating<ushort>(0x0001));
            AssertBitwiseEqual((NFloat)32767.0f, NumberBaseHelper<NFloat>.CreateTruncating<ushort>(0x7FFF));
            AssertBitwiseEqual((NFloat)32768.0f, NumberBaseHelper<NFloat>.CreateTruncating<ushort>(0x8000));
            AssertBitwiseEqual((NFloat)65535.0f, NumberBaseHelper<NFloat>.CreateTruncating<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt32Test()
        {
            AssertBitwiseEqual(Zero, NumberBaseHelper<NFloat>.CreateTruncating<uint>(0x00000000));
            AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.CreateTruncating<uint>(0x00000001));

            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual((NFloat)2147483647.0, NumberBaseHelper<NFloat>.CreateTruncating<uint>(0x7FFFFFFF));
                AssertBitwiseEqual((NFloat)2147483648.0, NumberBaseHelper<NFloat>.CreateTruncating<uint>(0x80000000));
                AssertBitwiseEqual((NFloat)4294967295.0, NumberBaseHelper<NFloat>.CreateTruncating<uint>(0xFFFFFFFF));
            }
            else
            {
                AssertBitwiseEqual((NFloat)2147483647.0f, NumberBaseHelper<NFloat>.CreateTruncating<uint>(0x7FFFFFFF));
                AssertBitwiseEqual((NFloat)2147483648.0f, NumberBaseHelper<NFloat>.CreateTruncating<uint>(0x80000000));
                AssertBitwiseEqual((NFloat)4294967295.0f, NumberBaseHelper<NFloat>.CreateTruncating<uint>(0xFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateTruncatingFromUInt64Test()
        {
            AssertBitwiseEqual(Zero, NumberBaseHelper<NFloat>.CreateTruncating<ulong>(0x0000000000000000));
            AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.CreateTruncating<ulong>(0x0000000000000001));

            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual((NFloat)9223372036854775807.0, NumberBaseHelper<NFloat>.CreateTruncating<ulong>(0x7FFFFFFFFFFFFFFF));
                AssertBitwiseEqual((NFloat)9223372036854775808.0, NumberBaseHelper<NFloat>.CreateTruncating<ulong>(0x8000000000000000));
                AssertBitwiseEqual((NFloat)18446744073709551615.0, NumberBaseHelper<NFloat>.CreateTruncating<ulong>(0xFFFFFFFFFFFFFFFF));
            }
            else
            {
                AssertBitwiseEqual((NFloat)9223372036854775807.0f, NumberBaseHelper<NFloat>.CreateTruncating<ulong>(0x7FFFFFFFFFFFFFFF));
                AssertBitwiseEqual((NFloat)9223372036854775808.0f, NumberBaseHelper<NFloat>.CreateTruncating<ulong>(0x8000000000000000));
                AssertBitwiseEqual((NFloat)18446744073709551615.0f, NumberBaseHelper<NFloat>.CreateTruncating<ulong>(0xFFFFFFFFFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateTruncatingFromUInt128Test()
        {
            AssertBitwiseEqual(0.0f, NumberBaseHelper<NFloat>.CreateTruncating<UInt128>(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000)));
            AssertBitwiseEqual(1.0f, NumberBaseHelper<NFloat>.CreateTruncating<UInt128>(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001)));

            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual((NFloat)(170141183460469231731687303715884105727.0), NumberBaseHelper<NFloat>.CreateTruncating<UInt128>(new UInt128(0x7FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)));
                AssertBitwiseEqual((NFloat)(170141183460469231731687303715884105728.0), NumberBaseHelper<NFloat>.CreateTruncating<UInt128>(new UInt128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000)));
                AssertBitwiseEqual((NFloat)(340282366920938463463374607431768211455.0), NumberBaseHelper<NFloat>.CreateTruncating<UInt128>(new UInt128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)));
            }
            else
            {
                AssertBitwiseEqual(170141183460469231731687303715884105727.0f, NumberBaseHelper<NFloat>.CreateTruncating<UInt128>(new UInt128(0x7FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)));
                AssertBitwiseEqual(170141183460469231731687303715884105728.0f, NumberBaseHelper<NFloat>.CreateTruncating<UInt128>(new UInt128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000)));
                AssertBitwiseEqual(float.PositiveInfinity, NumberBaseHelper<NFloat>.CreateTruncating<UInt128>(new UInt128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)));
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/69795", TestRuntimes.Mono)]
        public static void CreateTruncatingFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual(Zero, NumberBaseHelper<NFloat>.CreateTruncating<nuint>(unchecked((nuint)0x0000000000000000)));
                AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.CreateTruncating<nuint>(unchecked((nuint)0x0000000000000001)));
                AssertBitwiseEqual((NFloat)9223372036854775807.0, NumberBaseHelper<NFloat>.CreateTruncating<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));

                // https://github.com/dotnet/roslyn/issues/60714
                // AssertBitwiseEqual((NFloat)9223372036854775808.0, NumberBaseHelper<NFloat>.CreateTruncating<nuint>(unchecked((nuint)0x8000000000000000)));
                // AssertBitwiseEqual((NFloat)18446744073709551615.0, NumberBaseHelper<NFloat>.CreateTruncating<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                AssertBitwiseEqual(Zero, NumberBaseHelper<NFloat>.CreateTruncating<nuint>((nuint)0x00000000));
                AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.CreateTruncating<nuint>((nuint)0x00000001));
                AssertBitwiseEqual((NFloat)2147483647.0f, NumberBaseHelper<NFloat>.CreateTruncating<nuint>((nuint)0x7FFFFFFF));

                // https://github.com/dotnet/roslyn/issues/60714
                // AssertBitwiseEqual((NFloat)2147483648.0f, NumberBaseHelper<NFloat>.CreateTruncating<nuint>((nuint)0x80000000));
                // AssertBitwiseEqual((NFloat)4294967295.0f, NumberBaseHelper<NFloat>.CreateTruncating<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void IsCanonicalTest()
        {
            Assert.True(NumberBaseHelper<NFloat>.IsCanonical(NFloat.NegativeInfinity));
            Assert.True(NumberBaseHelper<NFloat>.IsCanonical(NFloat.MinValue));
            Assert.True(NumberBaseHelper<NFloat>.IsCanonical(NegativeOne));
            Assert.True(NumberBaseHelper<NFloat>.IsCanonical(-MinNormal));
            Assert.True(NumberBaseHelper<NFloat>.IsCanonical(-MaxSubnormal));
            Assert.True(NumberBaseHelper<NFloat>.IsCanonical(-NFloat.Epsilon));
            Assert.True(NumberBaseHelper<NFloat>.IsCanonical(NegativeZero));
            Assert.True(NumberBaseHelper<NFloat>.IsCanonical(NFloat.NaN));
            Assert.True(NumberBaseHelper<NFloat>.IsCanonical(Zero));
            Assert.True(NumberBaseHelper<NFloat>.IsCanonical(NFloat.Epsilon));
            Assert.True(NumberBaseHelper<NFloat>.IsCanonical(MaxSubnormal));
            Assert.True(NumberBaseHelper<NFloat>.IsCanonical(MinNormal));
            Assert.True(NumberBaseHelper<NFloat>.IsCanonical(One));
            Assert.True(NumberBaseHelper<NFloat>.IsCanonical(NFloat.MaxValue));
            Assert.True(NumberBaseHelper<NFloat>.IsCanonical(NFloat.PositiveInfinity));
        }

        [Fact]
        public static void IsComplexNumberTest()
        {
            Assert.False(NumberBaseHelper<NFloat>.IsComplexNumber(NFloat.NegativeInfinity));
            Assert.False(NumberBaseHelper<NFloat>.IsComplexNumber(NFloat.MinValue));
            Assert.False(NumberBaseHelper<NFloat>.IsComplexNumber(NegativeOne));
            Assert.False(NumberBaseHelper<NFloat>.IsComplexNumber(-MinNormal));
            Assert.False(NumberBaseHelper<NFloat>.IsComplexNumber(-MaxSubnormal));
            Assert.False(NumberBaseHelper<NFloat>.IsComplexNumber(-NFloat.Epsilon));
            Assert.False(NumberBaseHelper<NFloat>.IsComplexNumber(NegativeZero));
            Assert.False(NumberBaseHelper<NFloat>.IsComplexNumber(NFloat.NaN));
            Assert.False(NumberBaseHelper<NFloat>.IsComplexNumber(Zero));
            Assert.False(NumberBaseHelper<NFloat>.IsComplexNumber(NFloat.Epsilon));
            Assert.False(NumberBaseHelper<NFloat>.IsComplexNumber(MaxSubnormal));
            Assert.False(NumberBaseHelper<NFloat>.IsComplexNumber(MinNormal));
            Assert.False(NumberBaseHelper<NFloat>.IsComplexNumber(One));
            Assert.False(NumberBaseHelper<NFloat>.IsComplexNumber(NFloat.MaxValue));
            Assert.False(NumberBaseHelper<NFloat>.IsComplexNumber(NFloat.PositiveInfinity));
        }

        [Fact]
        public static void IsEvenIntegerTest()
        {
            Assert.False(NumberBaseHelper<NFloat>.IsEvenInteger(NFloat.NegativeInfinity));
            Assert.True(NumberBaseHelper<NFloat>.IsEvenInteger(NFloat.MinValue));
            Assert.False(NumberBaseHelper<NFloat>.IsEvenInteger(NegativeOne));
            Assert.False(NumberBaseHelper<NFloat>.IsEvenInteger(-MinNormal));
            Assert.False(NumberBaseHelper<NFloat>.IsEvenInteger(-MaxSubnormal));
            Assert.False(NumberBaseHelper<NFloat>.IsEvenInteger(-NFloat.Epsilon));
            Assert.True(NumberBaseHelper<NFloat>.IsEvenInteger(NegativeZero));
            Assert.False(NumberBaseHelper<NFloat>.IsEvenInteger(NFloat.NaN));
            Assert.True(NumberBaseHelper<NFloat>.IsEvenInteger(Zero));
            Assert.False(NumberBaseHelper<NFloat>.IsEvenInteger(NFloat.Epsilon));
            Assert.False(NumberBaseHelper<NFloat>.IsEvenInteger(MaxSubnormal));
            Assert.False(NumberBaseHelper<NFloat>.IsEvenInteger(MinNormal));
            Assert.False(NumberBaseHelper<NFloat>.IsEvenInteger(One));
            Assert.True(NumberBaseHelper<NFloat>.IsEvenInteger(NFloat.MaxValue));
            Assert.False(NumberBaseHelper<NFloat>.IsEvenInteger(NFloat.PositiveInfinity));
        }

        [Fact]
        public static void IsFiniteTest()
        {
            Assert.False(NumberBaseHelper<NFloat>.IsFinite(NFloat.NegativeInfinity));
            Assert.True(NumberBaseHelper<NFloat>.IsFinite(NFloat.MinValue));
            Assert.True(NumberBaseHelper<NFloat>.IsFinite(NegativeOne));
            Assert.True(NumberBaseHelper<NFloat>.IsFinite(-MinNormal));
            Assert.True(NumberBaseHelper<NFloat>.IsFinite(-MaxSubnormal));
            Assert.True(NumberBaseHelper<NFloat>.IsFinite(-NFloat.Epsilon));
            Assert.True(NumberBaseHelper<NFloat>.IsFinite(NegativeZero));
            Assert.False(NumberBaseHelper<NFloat>.IsFinite(NFloat.NaN));
            Assert.True(NumberBaseHelper<NFloat>.IsFinite(Zero));
            Assert.True(NumberBaseHelper<NFloat>.IsFinite(NFloat.Epsilon));
            Assert.True(NumberBaseHelper<NFloat>.IsFinite(MaxSubnormal));
            Assert.True(NumberBaseHelper<NFloat>.IsFinite(MinNormal));
            Assert.True(NumberBaseHelper<NFloat>.IsFinite(One));
            Assert.True(NumberBaseHelper<NFloat>.IsFinite(NFloat.MaxValue));
            Assert.False(NumberBaseHelper<NFloat>.IsFinite(NFloat.PositiveInfinity));
        }

        [Fact]
        public static void IsImaginaryNumberTest()
        {
            Assert.False(NumberBaseHelper<NFloat>.IsImaginaryNumber(NFloat.NegativeInfinity));
            Assert.False(NumberBaseHelper<NFloat>.IsImaginaryNumber(NFloat.MinValue));
            Assert.False(NumberBaseHelper<NFloat>.IsImaginaryNumber(NegativeOne));
            Assert.False(NumberBaseHelper<NFloat>.IsImaginaryNumber(-MinNormal));
            Assert.False(NumberBaseHelper<NFloat>.IsImaginaryNumber(-MaxSubnormal));
            Assert.False(NumberBaseHelper<NFloat>.IsImaginaryNumber(-NFloat.Epsilon));
            Assert.False(NumberBaseHelper<NFloat>.IsImaginaryNumber(NegativeZero));
            Assert.False(NumberBaseHelper<NFloat>.IsImaginaryNumber(NFloat.NaN));
            Assert.False(NumberBaseHelper<NFloat>.IsImaginaryNumber(Zero));
            Assert.False(NumberBaseHelper<NFloat>.IsImaginaryNumber(NFloat.Epsilon));
            Assert.False(NumberBaseHelper<NFloat>.IsImaginaryNumber(MaxSubnormal));
            Assert.False(NumberBaseHelper<NFloat>.IsImaginaryNumber(MinNormal));
            Assert.False(NumberBaseHelper<NFloat>.IsImaginaryNumber(One));
            Assert.False(NumberBaseHelper<NFloat>.IsImaginaryNumber(NFloat.MaxValue));
            Assert.False(NumberBaseHelper<NFloat>.IsImaginaryNumber(NFloat.PositiveInfinity));
        }

        [Fact]
        public static void IsInfinityTest()
        {
            Assert.True(NumberBaseHelper<NFloat>.IsInfinity(NFloat.NegativeInfinity));
            Assert.False(NumberBaseHelper<NFloat>.IsInfinity(NFloat.MinValue));
            Assert.False(NumberBaseHelper<NFloat>.IsInfinity(NegativeOne));
            Assert.False(NumberBaseHelper<NFloat>.IsInfinity(-MinNormal));
            Assert.False(NumberBaseHelper<NFloat>.IsInfinity(-MaxSubnormal));
            Assert.False(NumberBaseHelper<NFloat>.IsInfinity(-NFloat.Epsilon));
            Assert.False(NumberBaseHelper<NFloat>.IsInfinity(NegativeZero));
            Assert.False(NumberBaseHelper<NFloat>.IsInfinity(NFloat.NaN));
            Assert.False(NumberBaseHelper<NFloat>.IsInfinity(Zero));
            Assert.False(NumberBaseHelper<NFloat>.IsInfinity(NFloat.Epsilon));
            Assert.False(NumberBaseHelper<NFloat>.IsInfinity(MaxSubnormal));
            Assert.False(NumberBaseHelper<NFloat>.IsInfinity(MinNormal));
            Assert.False(NumberBaseHelper<NFloat>.IsInfinity(One));
            Assert.False(NumberBaseHelper<NFloat>.IsInfinity(NFloat.MaxValue));
            Assert.True(NumberBaseHelper<NFloat>.IsInfinity(NFloat.PositiveInfinity));
        }

        [Fact]
        public static void IsIntegerTest()
        {
            Assert.False(NumberBaseHelper<NFloat>.IsInteger(NFloat.NegativeInfinity));
            Assert.True(NumberBaseHelper<NFloat>.IsInteger(NFloat.MinValue));
            Assert.True(NumberBaseHelper<NFloat>.IsInteger(NegativeOne));
            Assert.False(NumberBaseHelper<NFloat>.IsInteger(-MinNormal));
            Assert.False(NumberBaseHelper<NFloat>.IsInteger(-MaxSubnormal));
            Assert.False(NumberBaseHelper<NFloat>.IsInteger(-NFloat.Epsilon));
            Assert.True(NumberBaseHelper<NFloat>.IsInteger(NegativeZero));
            Assert.False(NumberBaseHelper<NFloat>.IsInteger(NFloat.NaN));
            Assert.True(NumberBaseHelper<NFloat>.IsInteger(Zero));
            Assert.False(NumberBaseHelper<NFloat>.IsInteger(NFloat.Epsilon));
            Assert.False(NumberBaseHelper<NFloat>.IsInteger(MaxSubnormal));
            Assert.False(NumberBaseHelper<NFloat>.IsInteger(MinNormal));
            Assert.True(NumberBaseHelper<NFloat>.IsInteger(One));
            Assert.True(NumberBaseHelper<NFloat>.IsInteger(NFloat.MaxValue));
            Assert.False(NumberBaseHelper<NFloat>.IsInteger(NFloat.PositiveInfinity));
        }

        [Fact]
        public static void IsNaNTest()
        {
            Assert.False(NumberBaseHelper<NFloat>.IsNaN(NFloat.NegativeInfinity));
            Assert.False(NumberBaseHelper<NFloat>.IsNaN(NFloat.MinValue));
            Assert.False(NumberBaseHelper<NFloat>.IsNaN(NegativeOne));
            Assert.False(NumberBaseHelper<NFloat>.IsNaN(-MinNormal));
            Assert.False(NumberBaseHelper<NFloat>.IsNaN(-MaxSubnormal));
            Assert.False(NumberBaseHelper<NFloat>.IsNaN(-NFloat.Epsilon));
            Assert.False(NumberBaseHelper<NFloat>.IsNaN(NegativeZero));
            Assert.True(NumberBaseHelper<NFloat>.IsNaN(NFloat.NaN));
            Assert.False(NumberBaseHelper<NFloat>.IsNaN(Zero));
            Assert.False(NumberBaseHelper<NFloat>.IsNaN(NFloat.Epsilon));
            Assert.False(NumberBaseHelper<NFloat>.IsNaN(MaxSubnormal));
            Assert.False(NumberBaseHelper<NFloat>.IsNaN(MinNormal));
            Assert.False(NumberBaseHelper<NFloat>.IsNaN(One));
            Assert.False(NumberBaseHelper<NFloat>.IsNaN(NFloat.MaxValue));
            Assert.False(NumberBaseHelper<NFloat>.IsNaN(NFloat.PositiveInfinity));
        }

        [Fact]
        public static void IsNegativeTest()
        {
            Assert.True(NumberBaseHelper<NFloat>.IsNegative(NFloat.NegativeInfinity));
            Assert.True(NumberBaseHelper<NFloat>.IsNegative(NFloat.MinValue));
            Assert.True(NumberBaseHelper<NFloat>.IsNegative(NegativeOne));
            Assert.True(NumberBaseHelper<NFloat>.IsNegative(-MinNormal));
            Assert.True(NumberBaseHelper<NFloat>.IsNegative(-MaxSubnormal));
            Assert.True(NumberBaseHelper<NFloat>.IsNegative(-NFloat.Epsilon));
            Assert.True(NumberBaseHelper<NFloat>.IsNegative(NegativeZero));
            Assert.True(NumberBaseHelper<NFloat>.IsNegative(NFloat.NaN));
            Assert.False(NumberBaseHelper<NFloat>.IsNegative(Zero));
            Assert.False(NumberBaseHelper<NFloat>.IsNegative(NFloat.Epsilon));
            Assert.False(NumberBaseHelper<NFloat>.IsNegative(MaxSubnormal));
            Assert.False(NumberBaseHelper<NFloat>.IsNegative(MinNormal));
            Assert.False(NumberBaseHelper<NFloat>.IsNegative(One));
            Assert.False(NumberBaseHelper<NFloat>.IsNegative(NFloat.MaxValue));
            Assert.False(NumberBaseHelper<NFloat>.IsNegative(NFloat.PositiveInfinity));
        }

        [Fact]
        public static void IsNegativeInfinityTest()
        {
            Assert.True(NumberBaseHelper<NFloat>.IsNegativeInfinity(NFloat.NegativeInfinity));
            Assert.False(NumberBaseHelper<NFloat>.IsNegativeInfinity(NFloat.MinValue));
            Assert.False(NumberBaseHelper<NFloat>.IsNegativeInfinity(NegativeOne));
            Assert.False(NumberBaseHelper<NFloat>.IsNegativeInfinity(-MinNormal));
            Assert.False(NumberBaseHelper<NFloat>.IsNegativeInfinity(-MaxSubnormal));
            Assert.False(NumberBaseHelper<NFloat>.IsNegativeInfinity(-NFloat.Epsilon));
            Assert.False(NumberBaseHelper<NFloat>.IsNegativeInfinity(NegativeZero));
            Assert.False(NumberBaseHelper<NFloat>.IsNegativeInfinity(NFloat.NaN));
            Assert.False(NumberBaseHelper<NFloat>.IsNegativeInfinity(Zero));
            Assert.False(NumberBaseHelper<NFloat>.IsNegativeInfinity(NFloat.Epsilon));
            Assert.False(NumberBaseHelper<NFloat>.IsNegativeInfinity(MaxSubnormal));
            Assert.False(NumberBaseHelper<NFloat>.IsNegativeInfinity(MinNormal));
            Assert.False(NumberBaseHelper<NFloat>.IsNegativeInfinity(One));
            Assert.False(NumberBaseHelper<NFloat>.IsNegativeInfinity(NFloat.MaxValue));
            Assert.False(NumberBaseHelper<NFloat>.IsNegativeInfinity(NFloat.PositiveInfinity));
        }

        [Fact]
        public static void IsNormalTest()
        {
            Assert.False(NumberBaseHelper<NFloat>.IsNormal(NFloat.NegativeInfinity));
            Assert.True(NumberBaseHelper<NFloat>.IsNormal(NFloat.MinValue));
            Assert.True(NumberBaseHelper<NFloat>.IsNormal(NegativeOne));
            Assert.True(NumberBaseHelper<NFloat>.IsNormal(-MinNormal));
            Assert.False(NumberBaseHelper<NFloat>.IsNormal(-MaxSubnormal));
            Assert.False(NumberBaseHelper<NFloat>.IsNormal(-NFloat.Epsilon));
            Assert.False(NumberBaseHelper<NFloat>.IsNormal(NegativeZero));
            Assert.False(NumberBaseHelper<NFloat>.IsNormal(NFloat.NaN));
            Assert.False(NumberBaseHelper<NFloat>.IsNormal(Zero));
            Assert.False(NumberBaseHelper<NFloat>.IsNormal(NFloat.Epsilon));
            Assert.False(NumberBaseHelper<NFloat>.IsNormal(MaxSubnormal));
            Assert.True(NumberBaseHelper<NFloat>.IsNormal(MinNormal));
            Assert.True(NumberBaseHelper<NFloat>.IsNormal(One));
            Assert.True(NumberBaseHelper<NFloat>.IsNormal(NFloat.MaxValue));
            Assert.False(NumberBaseHelper<NFloat>.IsNormal(NFloat.PositiveInfinity));
        }

        [Fact]
        public static void IsOddIntegerTest()
        {
            Assert.False(NumberBaseHelper<NFloat>.IsOddInteger(NFloat.NegativeInfinity));
            Assert.False(NumberBaseHelper<NFloat>.IsOddInteger(NFloat.MinValue));
            Assert.True(NumberBaseHelper<NFloat>.IsOddInteger(NegativeOne));
            Assert.False(NumberBaseHelper<NFloat>.IsOddInteger(-MinNormal));
            Assert.False(NumberBaseHelper<NFloat>.IsOddInteger(-MaxSubnormal));
            Assert.False(NumberBaseHelper<NFloat>.IsOddInteger(-NFloat.Epsilon));
            Assert.False(NumberBaseHelper<NFloat>.IsOddInteger(NegativeZero));
            Assert.False(NumberBaseHelper<NFloat>.IsOddInteger(NFloat.NaN));
            Assert.False(NumberBaseHelper<NFloat>.IsOddInteger(Zero));
            Assert.False(NumberBaseHelper<NFloat>.IsOddInteger(NFloat.Epsilon));
            Assert.False(NumberBaseHelper<NFloat>.IsOddInteger(MaxSubnormal));
            Assert.False(NumberBaseHelper<NFloat>.IsOddInteger(MinNormal));
            Assert.True(NumberBaseHelper<NFloat>.IsOddInteger(One));
            Assert.False(NumberBaseHelper<NFloat>.IsOddInteger(NFloat.MaxValue));
            Assert.False(NumberBaseHelper<NFloat>.IsOddInteger(NFloat.PositiveInfinity));
        }

        [Fact]
        public static void IsPositiveTest()
        {
            Assert.False(NumberBaseHelper<NFloat>.IsPositive(NFloat.NegativeInfinity));
            Assert.False(NumberBaseHelper<NFloat>.IsPositive(NFloat.MinValue));
            Assert.False(NumberBaseHelper<NFloat>.IsPositive(NegativeOne));
            Assert.False(NumberBaseHelper<NFloat>.IsPositive(-MinNormal));
            Assert.False(NumberBaseHelper<NFloat>.IsPositive(-MaxSubnormal));
            Assert.False(NumberBaseHelper<NFloat>.IsPositive(-NFloat.Epsilon));
            Assert.False(NumberBaseHelper<NFloat>.IsPositive(NegativeZero));
            Assert.False(NumberBaseHelper<NFloat>.IsPositive(NFloat.NaN));
            Assert.True(NumberBaseHelper<NFloat>.IsPositive(Zero));
            Assert.True(NumberBaseHelper<NFloat>.IsPositive(NFloat.Epsilon));
            Assert.True(NumberBaseHelper<NFloat>.IsPositive(MaxSubnormal));
            Assert.True(NumberBaseHelper<NFloat>.IsPositive(MinNormal));
            Assert.True(NumberBaseHelper<NFloat>.IsPositive(One));
            Assert.True(NumberBaseHelper<NFloat>.IsPositive(NFloat.MaxValue));
            Assert.True(NumberBaseHelper<NFloat>.IsPositive(NFloat.PositiveInfinity));
        }

        [Fact]
        public static void IsPositiveInfinityTest()
        {
            Assert.False(NumberBaseHelper<NFloat>.IsPositiveInfinity(NFloat.NegativeInfinity));
            Assert.False(NumberBaseHelper<NFloat>.IsPositiveInfinity(NFloat.MinValue));
            Assert.False(NumberBaseHelper<NFloat>.IsPositiveInfinity(NegativeOne));
            Assert.False(NumberBaseHelper<NFloat>.IsPositiveInfinity(-MinNormal));
            Assert.False(NumberBaseHelper<NFloat>.IsPositiveInfinity(-MaxSubnormal));
            Assert.False(NumberBaseHelper<NFloat>.IsPositiveInfinity(-NFloat.Epsilon));
            Assert.False(NumberBaseHelper<NFloat>.IsPositiveInfinity(NegativeZero));
            Assert.False(NumberBaseHelper<NFloat>.IsPositiveInfinity(NFloat.NaN));
            Assert.False(NumberBaseHelper<NFloat>.IsPositiveInfinity(Zero));
            Assert.False(NumberBaseHelper<NFloat>.IsPositiveInfinity(NFloat.Epsilon));
            Assert.False(NumberBaseHelper<NFloat>.IsPositiveInfinity(MaxSubnormal));
            Assert.False(NumberBaseHelper<NFloat>.IsPositiveInfinity(MinNormal));
            Assert.False(NumberBaseHelper<NFloat>.IsPositiveInfinity(One));
            Assert.False(NumberBaseHelper<NFloat>.IsPositiveInfinity(NFloat.MaxValue));
            Assert.True(NumberBaseHelper<NFloat>.IsPositiveInfinity(NFloat.PositiveInfinity));
        }

        [Fact]
        public static void IsRealNumberTest()
        {
            Assert.True(NumberBaseHelper<NFloat>.IsRealNumber(NFloat.NegativeInfinity));
            Assert.True(NumberBaseHelper<NFloat>.IsRealNumber(NFloat.MinValue));
            Assert.True(NumberBaseHelper<NFloat>.IsRealNumber(NegativeOne));
            Assert.True(NumberBaseHelper<NFloat>.IsRealNumber(-MinNormal));
            Assert.True(NumberBaseHelper<NFloat>.IsRealNumber(-MaxSubnormal));
            Assert.True(NumberBaseHelper<NFloat>.IsRealNumber(-NFloat.Epsilon));
            Assert.True(NumberBaseHelper<NFloat>.IsRealNumber(NegativeZero));
            Assert.False(NumberBaseHelper<NFloat>.IsRealNumber(NFloat.NaN));
            Assert.True(NumberBaseHelper<NFloat>.IsRealNumber(Zero));
            Assert.True(NumberBaseHelper<NFloat>.IsRealNumber(NFloat.Epsilon));
            Assert.True(NumberBaseHelper<NFloat>.IsRealNumber(MaxSubnormal));
            Assert.True(NumberBaseHelper<NFloat>.IsRealNumber(MinNormal));
            Assert.True(NumberBaseHelper<NFloat>.IsRealNumber(One));
            Assert.True(NumberBaseHelper<NFloat>.IsRealNumber(NFloat.MaxValue));
            Assert.True(NumberBaseHelper<NFloat>.IsRealNumber(NFloat.PositiveInfinity));
        }

        [Fact]
        public static void IsSubnormalTest()
        {
            Assert.False(NumberBaseHelper<NFloat>.IsSubnormal(NFloat.NegativeInfinity));
            Assert.False(NumberBaseHelper<NFloat>.IsSubnormal(NFloat.MinValue));
            Assert.False(NumberBaseHelper<NFloat>.IsSubnormal(NegativeOne));
            Assert.False(NumberBaseHelper<NFloat>.IsSubnormal(-MinNormal));
            Assert.True(NumberBaseHelper<NFloat>.IsSubnormal(-MaxSubnormal));
            Assert.True(NumberBaseHelper<NFloat>.IsSubnormal(-NFloat.Epsilon));
            Assert.False(NumberBaseHelper<NFloat>.IsSubnormal(NegativeZero));
            Assert.False(NumberBaseHelper<NFloat>.IsSubnormal(NFloat.NaN));
            Assert.False(NumberBaseHelper<NFloat>.IsSubnormal(Zero));
            Assert.True(NumberBaseHelper<NFloat>.IsSubnormal(NFloat.Epsilon));
            Assert.True(NumberBaseHelper<NFloat>.IsSubnormal(MaxSubnormal));
            Assert.False(NumberBaseHelper<NFloat>.IsSubnormal(MinNormal));
            Assert.False(NumberBaseHelper<NFloat>.IsSubnormal(One));
            Assert.False(NumberBaseHelper<NFloat>.IsSubnormal(NFloat.MaxValue));
            Assert.False(NumberBaseHelper<NFloat>.IsSubnormal(NFloat.PositiveInfinity));
        }

        [Fact]
        public static void IsZeroTest()
        {
            Assert.False(NumberBaseHelper<NFloat>.IsZero(NFloat.NegativeInfinity));
            Assert.False(NumberBaseHelper<NFloat>.IsZero(NFloat.MinValue));
            Assert.False(NumberBaseHelper<NFloat>.IsZero(NegativeOne));
            Assert.False(NumberBaseHelper<NFloat>.IsZero(-MinNormal));
            Assert.False(NumberBaseHelper<NFloat>.IsZero(-MaxSubnormal));
            Assert.False(NumberBaseHelper<NFloat>.IsZero(-NFloat.Epsilon));
            Assert.True(NumberBaseHelper<NFloat>.IsZero(NegativeZero));
            Assert.False(NumberBaseHelper<NFloat>.IsZero(NFloat.NaN));
            Assert.True(NumberBaseHelper<NFloat>.IsZero(Zero));
            Assert.False(NumberBaseHelper<NFloat>.IsZero(NFloat.Epsilon));
            Assert.False(NumberBaseHelper<NFloat>.IsZero(MaxSubnormal));
            Assert.False(NumberBaseHelper<NFloat>.IsZero(MinNormal));
            Assert.False(NumberBaseHelper<NFloat>.IsZero(One));
            Assert.False(NumberBaseHelper<NFloat>.IsZero(NFloat.MaxValue));
            Assert.False(NumberBaseHelper<NFloat>.IsZero(NFloat.PositiveInfinity));
        }

        [Fact]
        public static void MaxMagnitudeTest()
        {
            AssertBitwiseEqual(NFloat.NegativeInfinity, NumberBaseHelper<NFloat>.MaxMagnitude(NFloat.NegativeInfinity, One));
            AssertBitwiseEqual(NFloat.MinValue, NumberBaseHelper<NFloat>.MaxMagnitude(NFloat.MinValue, One));
            AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.MaxMagnitude(NegativeOne, One));
            AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.MaxMagnitude(-MinNormal, One));
            AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.MaxMagnitude(-MaxSubnormal, One));
            AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.MaxMagnitude(-NFloat.Epsilon, One));
            AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.MaxMagnitude(NegativeZero, One));
            AssertBitwiseEqual(NFloat.NaN, NumberBaseHelper<NFloat>.MaxMagnitude(NFloat.NaN, One));
            AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.MaxMagnitude(Zero, One));
            AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.MaxMagnitude(NFloat.Epsilon, One));
            AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.MaxMagnitude(MaxSubnormal, One));
            AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.MaxMagnitude(MinNormal, One));
            AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.MaxMagnitude(One, One));
            AssertBitwiseEqual(NFloat.MaxValue, NumberBaseHelper<NFloat>.MaxMagnitude(NFloat.MaxValue, One));
            AssertBitwiseEqual(NFloat.PositiveInfinity, NumberBaseHelper<NFloat>.MaxMagnitude(NFloat.PositiveInfinity, One));
        }

        [Fact]
        public static void MaxMagnitudeNumberTest()
        {
            AssertBitwiseEqual(NFloat.NegativeInfinity, NumberBaseHelper<NFloat>.MaxMagnitudeNumber(NFloat.NegativeInfinity, One));
            AssertBitwiseEqual(NFloat.MinValue, NumberBaseHelper<NFloat>.MaxMagnitudeNumber(NFloat.MinValue, One));
            AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.MaxMagnitudeNumber(NegativeOne, One));
            AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.MaxMagnitudeNumber(-MinNormal, One));
            AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.MaxMagnitudeNumber(-MaxSubnormal, One));
            AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.MaxMagnitudeNumber(-NFloat.Epsilon, One));
            AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.MaxMagnitudeNumber(NegativeZero, One));
            AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.MaxMagnitudeNumber(NFloat.NaN, One));
            AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.MaxMagnitudeNumber(Zero, One));
            AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.MaxMagnitudeNumber(NFloat.Epsilon, One));
            AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.MaxMagnitudeNumber(MaxSubnormal, One));
            AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.MaxMagnitudeNumber(MinNormal, One));
            AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.MaxMagnitudeNumber(One, One));
            AssertBitwiseEqual(NFloat.MaxValue, NumberBaseHelper<NFloat>.MaxMagnitudeNumber(NFloat.MaxValue, One));
            AssertBitwiseEqual(NFloat.PositiveInfinity, NumberBaseHelper<NFloat>.MaxMagnitudeNumber(NFloat.PositiveInfinity, One));
        }

        [Fact]
        public static void MinMagnitudeTest()
        {
            AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.MinMagnitude(NFloat.NegativeInfinity, One));
            AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.MinMagnitude(NFloat.MinValue, One));
            AssertBitwiseEqual(NegativeOne, NumberBaseHelper<NFloat>.MinMagnitude(NegativeOne, One));
            AssertBitwiseEqual(-MinNormal, NumberBaseHelper<NFloat>.MinMagnitude(-MinNormal, One));
            AssertBitwiseEqual(-MaxSubnormal, NumberBaseHelper<NFloat>.MinMagnitude(-MaxSubnormal, One));
            AssertBitwiseEqual(-NFloat.Epsilon, NumberBaseHelper<NFloat>.MinMagnitude(-NFloat.Epsilon, One));
            AssertBitwiseEqual(NegativeZero, NumberBaseHelper<NFloat>.MinMagnitude(NegativeZero, One));
            AssertBitwiseEqual(NFloat.NaN, NumberBaseHelper<NFloat>.MinMagnitude(NFloat.NaN, One));
            AssertBitwiseEqual(Zero, NumberBaseHelper<NFloat>.MinMagnitude(Zero, One));
            AssertBitwiseEqual(NFloat.Epsilon, NumberBaseHelper<NFloat>.MinMagnitude(NFloat.Epsilon, One));
            AssertBitwiseEqual(MaxSubnormal, NumberBaseHelper<NFloat>.MinMagnitude(MaxSubnormal, One));
            AssertBitwiseEqual(MinNormal, NumberBaseHelper<NFloat>.MinMagnitude(MinNormal, One));
            AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.MinMagnitude(One, One));
            AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.MinMagnitude(NFloat.MaxValue, One));
            AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.MinMagnitude(NFloat.PositiveInfinity, One));
        }

        [Fact]
        public static void MinMagnitudeNumberTest()
        {
            AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.MinMagnitudeNumber(NFloat.NegativeInfinity, One));
            AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.MinMagnitudeNumber(NFloat.MinValue, One));
            AssertBitwiseEqual(NegativeOne, NumberBaseHelper<NFloat>.MinMagnitudeNumber(NegativeOne, One));
            AssertBitwiseEqual(-MinNormal, NumberBaseHelper<NFloat>.MinMagnitudeNumber(-MinNormal, One));
            AssertBitwiseEqual(-MaxSubnormal, NumberBaseHelper<NFloat>.MinMagnitudeNumber(-MaxSubnormal, One));
            AssertBitwiseEqual(-NFloat.Epsilon, NumberBaseHelper<NFloat>.MinMagnitudeNumber(-NFloat.Epsilon, One));
            AssertBitwiseEqual(NegativeZero, NumberBaseHelper<NFloat>.MinMagnitudeNumber(NegativeZero, One));
            AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.MinMagnitudeNumber(NFloat.NaN, One));
            AssertBitwiseEqual(Zero, NumberBaseHelper<NFloat>.MinMagnitudeNumber(Zero, One));
            AssertBitwiseEqual(NFloat.Epsilon, NumberBaseHelper<NFloat>.MinMagnitudeNumber(NFloat.Epsilon, One));
            AssertBitwiseEqual(MaxSubnormal, NumberBaseHelper<NFloat>.MinMagnitudeNumber(MaxSubnormal, One));
            AssertBitwiseEqual(MinNormal, NumberBaseHelper<NFloat>.MinMagnitudeNumber(MinNormal, One));
            AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.MinMagnitudeNumber(One, One));
            AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.MinMagnitudeNumber(NFloat.MaxValue, One));
            AssertBitwiseEqual(One, NumberBaseHelper<NFloat>.MinMagnitudeNumber(NFloat.PositiveInfinity, One));
        }

        //
        // ISignedNumber
        //

        [Fact]
        public static void NegativeOneTest()
        {
            Assert.Equal(NegativeOne, SignedNumberHelper<NFloat>.NegativeOne);
        }

        //
        // ISubtractionOperators
        //

        [Fact]
        public static void op_SubtractionTest()
        {
            AssertBitwiseEqual(NFloat.NegativeInfinity, SubtractionOperatorsHelper<NFloat, NFloat, NFloat>.op_Subtraction(NFloat.NegativeInfinity, One));
            AssertBitwiseEqual(NFloat.MinValue, SubtractionOperatorsHelper<NFloat, NFloat, NFloat>.op_Subtraction(NFloat.MinValue, One));
            AssertBitwiseEqual(NegativeTwo, SubtractionOperatorsHelper<NFloat, NFloat, NFloat>.op_Subtraction(NegativeOne, One));
            AssertBitwiseEqual(NegativeOne, SubtractionOperatorsHelper<NFloat, NFloat, NFloat>.op_Subtraction(-MinNormal, One));
            AssertBitwiseEqual(NegativeOne, SubtractionOperatorsHelper<NFloat, NFloat, NFloat>.op_Subtraction(-MaxSubnormal, One));
            AssertBitwiseEqual(NegativeOne, SubtractionOperatorsHelper<NFloat, NFloat, NFloat>.op_Subtraction(-NFloat.Epsilon, One));
            AssertBitwiseEqual(NegativeOne, SubtractionOperatorsHelper<NFloat, NFloat, NFloat>.op_Subtraction(NegativeZero, One));
            AssertBitwiseEqual(NFloat.NaN, SubtractionOperatorsHelper<NFloat, NFloat, NFloat>.op_Subtraction(NFloat.NaN, One));
            AssertBitwiseEqual(NegativeOne, SubtractionOperatorsHelper<NFloat, NFloat, NFloat>.op_Subtraction(Zero, One));
            AssertBitwiseEqual(NegativeOne, SubtractionOperatorsHelper<NFloat, NFloat, NFloat>.op_Subtraction(NFloat.Epsilon, One));
            AssertBitwiseEqual(NegativeOne, SubtractionOperatorsHelper<NFloat, NFloat, NFloat>.op_Subtraction(MaxSubnormal, One));
            AssertBitwiseEqual(NegativeOne, SubtractionOperatorsHelper<NFloat, NFloat, NFloat>.op_Subtraction(MinNormal, One));
            AssertBitwiseEqual(Zero, SubtractionOperatorsHelper<NFloat, NFloat, NFloat>.op_Subtraction(One, One));
            AssertBitwiseEqual(NFloat.MaxValue, SubtractionOperatorsHelper<NFloat, NFloat, NFloat>.op_Subtraction(NFloat.MaxValue, One));
            AssertBitwiseEqual(NFloat.PositiveInfinity, SubtractionOperatorsHelper<NFloat, NFloat, NFloat>.op_Subtraction(NFloat.PositiveInfinity, One));
        }

        [Fact]
        public static void op_CheckedSubtractionTest()
        {
            AssertBitwiseEqual(NFloat.NegativeInfinity, SubtractionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedSubtraction(NFloat.NegativeInfinity, One));
            AssertBitwiseEqual(NFloat.MinValue, SubtractionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedSubtraction(NFloat.MinValue, One));
            AssertBitwiseEqual(NegativeTwo, SubtractionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedSubtraction(NegativeOne, One));
            AssertBitwiseEqual(NegativeOne, SubtractionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedSubtraction(-MinNormal, One));
            AssertBitwiseEqual(NegativeOne, SubtractionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedSubtraction(-MaxSubnormal, One));
            AssertBitwiseEqual(NegativeOne, SubtractionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedSubtraction(-NFloat.Epsilon, One));
            AssertBitwiseEqual(NegativeOne, SubtractionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedSubtraction(NegativeZero, One));
            AssertBitwiseEqual(NFloat.NaN, SubtractionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedSubtraction(NFloat.NaN, One));
            AssertBitwiseEqual(NegativeOne, SubtractionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedSubtraction(Zero, One));
            AssertBitwiseEqual(NegativeOne, SubtractionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedSubtraction(NFloat.Epsilon, One));
            AssertBitwiseEqual(NegativeOne, SubtractionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedSubtraction(MaxSubnormal, One));
            AssertBitwiseEqual(NegativeOne, SubtractionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedSubtraction(MinNormal, One));
            AssertBitwiseEqual(Zero, SubtractionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedSubtraction(One, One));
            AssertBitwiseEqual(NFloat.MaxValue, SubtractionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedSubtraction(NFloat.MaxValue, One));
            AssertBitwiseEqual(NFloat.PositiveInfinity, SubtractionOperatorsHelper<NFloat, NFloat, NFloat>.op_CheckedSubtraction(NFloat.PositiveInfinity, One));
        }

        //
        // IUnaryNegationOperators
        //

        [Fact]
        public static void op_UnaryNegationTest()
        {
            AssertBitwiseEqual(NFloat.PositiveInfinity, UnaryNegationOperatorsHelper<NFloat, NFloat>.op_UnaryNegation(NFloat.NegativeInfinity));
            AssertBitwiseEqual(NFloat.MaxValue, UnaryNegationOperatorsHelper<NFloat, NFloat>.op_UnaryNegation(NFloat.MinValue));
            AssertBitwiseEqual(One, UnaryNegationOperatorsHelper<NFloat, NFloat>.op_UnaryNegation(NegativeOne));
            AssertBitwiseEqual(MinNormal, UnaryNegationOperatorsHelper<NFloat, NFloat>.op_UnaryNegation(-MinNormal));
            AssertBitwiseEqual(MaxSubnormal, UnaryNegationOperatorsHelper<NFloat, NFloat>.op_UnaryNegation(-MaxSubnormal));
            AssertBitwiseEqual(NFloat.Epsilon, UnaryNegationOperatorsHelper<NFloat, NFloat>.op_UnaryNegation(-NFloat.Epsilon));
            AssertBitwiseEqual(Zero, UnaryNegationOperatorsHelper<NFloat, NFloat>.op_UnaryNegation(NegativeZero));
            AssertBitwiseEqual(NFloat.NaN, UnaryNegationOperatorsHelper<NFloat, NFloat>.op_UnaryNegation(NFloat.NaN));
            AssertBitwiseEqual(NegativeZero, UnaryNegationOperatorsHelper<NFloat, NFloat>.op_UnaryNegation(Zero));
            AssertBitwiseEqual(-NFloat.Epsilon, UnaryNegationOperatorsHelper<NFloat, NFloat>.op_UnaryNegation(NFloat.Epsilon));
            AssertBitwiseEqual(-MaxSubnormal, UnaryNegationOperatorsHelper<NFloat, NFloat>.op_UnaryNegation(MaxSubnormal));
            AssertBitwiseEqual(-MinNormal, UnaryNegationOperatorsHelper<NFloat, NFloat>.op_UnaryNegation(MinNormal));
            AssertBitwiseEqual(NegativeOne, UnaryNegationOperatorsHelper<NFloat, NFloat>.op_UnaryNegation(One));
            AssertBitwiseEqual(NFloat.MinValue, UnaryNegationOperatorsHelper<NFloat, NFloat>.op_UnaryNegation(NFloat.MaxValue));
            AssertBitwiseEqual(NFloat.NegativeInfinity, UnaryNegationOperatorsHelper<NFloat, NFloat>.op_UnaryNegation(NFloat.PositiveInfinity));
        }

        [Fact]
        public static void op_CheckedUnaryNegationTest()
        {
            AssertBitwiseEqual(NFloat.PositiveInfinity, UnaryNegationOperatorsHelper<NFloat, NFloat>.op_CheckedUnaryNegation(NFloat.NegativeInfinity));
            AssertBitwiseEqual(NFloat.MaxValue, UnaryNegationOperatorsHelper<NFloat, NFloat>.op_CheckedUnaryNegation(NFloat.MinValue));
            AssertBitwiseEqual(One, UnaryNegationOperatorsHelper<NFloat, NFloat>.op_CheckedUnaryNegation(NegativeOne));
            AssertBitwiseEqual(MinNormal, UnaryNegationOperatorsHelper<NFloat, NFloat>.op_CheckedUnaryNegation(-MinNormal));
            AssertBitwiseEqual(MaxSubnormal, UnaryNegationOperatorsHelper<NFloat, NFloat>.op_CheckedUnaryNegation(-MaxSubnormal));
            AssertBitwiseEqual(NFloat.Epsilon, UnaryNegationOperatorsHelper<NFloat, NFloat>.op_CheckedUnaryNegation(-NFloat.Epsilon));
            AssertBitwiseEqual(Zero, UnaryNegationOperatorsHelper<NFloat, NFloat>.op_CheckedUnaryNegation(NegativeZero));
            AssertBitwiseEqual(NFloat.NaN, UnaryNegationOperatorsHelper<NFloat, NFloat>.op_CheckedUnaryNegation(NFloat.NaN));
            AssertBitwiseEqual(NegativeZero, UnaryNegationOperatorsHelper<NFloat, NFloat>.op_CheckedUnaryNegation(Zero));
            AssertBitwiseEqual(-NFloat.Epsilon, UnaryNegationOperatorsHelper<NFloat, NFloat>.op_CheckedUnaryNegation(NFloat.Epsilon));
            AssertBitwiseEqual(-MaxSubnormal, UnaryNegationOperatorsHelper<NFloat, NFloat>.op_CheckedUnaryNegation(MaxSubnormal));
            AssertBitwiseEqual(-MinNormal, UnaryNegationOperatorsHelper<NFloat, NFloat>.op_CheckedUnaryNegation(MinNormal));
            AssertBitwiseEqual(NegativeOne, UnaryNegationOperatorsHelper<NFloat, NFloat>.op_CheckedUnaryNegation(One));
            AssertBitwiseEqual(NFloat.MinValue, UnaryNegationOperatorsHelper<NFloat, NFloat>.op_CheckedUnaryNegation(NFloat.MaxValue));
            AssertBitwiseEqual(NFloat.NegativeInfinity, UnaryNegationOperatorsHelper<NFloat, NFloat>.op_CheckedUnaryNegation(NFloat.PositiveInfinity));
        }

        //
        // IUnaryPlusOperators
        //

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
            AssertBitwiseEqual(Zero, UnaryPlusOperatorsHelper<NFloat, NFloat>.op_UnaryPlus(Zero));
            AssertBitwiseEqual(NFloat.Epsilon, UnaryPlusOperatorsHelper<NFloat, NFloat>.op_UnaryPlus(NFloat.Epsilon));
            AssertBitwiseEqual(MaxSubnormal, UnaryPlusOperatorsHelper<NFloat, NFloat>.op_UnaryPlus(MaxSubnormal));
            AssertBitwiseEqual(MinNormal, UnaryPlusOperatorsHelper<NFloat, NFloat>.op_UnaryPlus(MinNormal));
            AssertBitwiseEqual(One, UnaryPlusOperatorsHelper<NFloat, NFloat>.op_UnaryPlus(One));
            AssertBitwiseEqual(NFloat.MaxValue, UnaryPlusOperatorsHelper<NFloat, NFloat>.op_UnaryPlus(NFloat.MaxValue));
            AssertBitwiseEqual(NFloat.PositiveInfinity, UnaryPlusOperatorsHelper<NFloat, NFloat>.op_UnaryPlus(NFloat.PositiveInfinity));
        }
    }
}
