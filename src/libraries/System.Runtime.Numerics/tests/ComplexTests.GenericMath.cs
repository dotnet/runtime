// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
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

        //
        // IAdditionOperators
        //

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

        //
        // IAdditiveIdentity
        //

        [Fact]
        public static void AdditiveIdentityTest()
        {
            AssertBitwiseEqual(0.0, AdditiveIdentityHelper<Complex, Complex>.AdditiveIdentity);
        }

        //
        // IDecrementOperators
        //

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

        //
        // IDivisionOperators
        //

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

        //
        // IEqualityOperators
        //

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

        //
        // IIncrementOperators
        //

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

        //
        // IMultiplicativeIdentity
        //

        [Fact]
        public static void MultiplicativeIdentityTest()
        {
            AssertBitwiseEqual(1.0, MultiplicativeIdentityHelper<Complex, Complex>.MultiplicativeIdentity);
        }

        //
        // IMultiplyOperators
        //

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

        //
        // INumberBase
        //

        [Fact]
        public static void OneTest()
        {
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.One);
        }

        [Fact]
        public static void RadixTest()
        {
            Assert.Equal(2, NumberBaseHelper<Complex>.Radix);
        }

        [Fact]
        public static void ZeroTest()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<Complex>.Zero);
        }

        [Fact]
        public static void AbsTest()
        {
            AssertBitwiseEqual(double.PositiveInfinity, NumberBaseHelper<Complex>.Abs(double.NegativeInfinity));
            AssertBitwiseEqual(double.MaxValue, NumberBaseHelper<Complex>.Abs(double.MinValue));
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.Abs(-1.0));
            AssertBitwiseEqual(MinNormal, NumberBaseHelper<Complex>.Abs(-MinNormal));
            AssertBitwiseEqual(MaxSubnormal, NumberBaseHelper<Complex>.Abs(-MaxSubnormal));
            AssertBitwiseEqual(double.Epsilon, NumberBaseHelper<Complex>.Abs(-double.Epsilon));
            AssertBitwiseEqual(0.0, NumberBaseHelper<Complex>.Abs(-0.0));
            AssertBitwiseEqual(double.NaN, NumberBaseHelper<Complex>.Abs(double.NaN));
            AssertBitwiseEqual(0.0, NumberBaseHelper<Complex>.Abs(0.0));
            AssertBitwiseEqual(double.Epsilon, NumberBaseHelper<Complex>.Abs(double.Epsilon));
            AssertBitwiseEqual(MaxSubnormal, NumberBaseHelper<Complex>.Abs(MaxSubnormal));
            AssertBitwiseEqual(MinNormal, NumberBaseHelper<Complex>.Abs(MinNormal));
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.Abs(1.0));
            AssertBitwiseEqual(double.MaxValue, NumberBaseHelper<Complex>.Abs(double.MaxValue));
            AssertBitwiseEqual(double.PositiveInfinity, NumberBaseHelper<Complex>.Abs(double.PositiveInfinity));
        }

        [Fact]
        public static void CreateCheckedFromByteTest()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<Complex>.CreateChecked<byte>(0x00));
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.CreateChecked<byte>(0x01));
            AssertBitwiseEqual(127.0, NumberBaseHelper<Complex>.CreateChecked<byte>(0x7F));
            AssertBitwiseEqual(128.0, NumberBaseHelper<Complex>.CreateChecked<byte>(0x80));
            AssertBitwiseEqual(255.0, NumberBaseHelper<Complex>.CreateChecked<byte>(0xFF));
        }

        [Fact]
        public static void CreateCheckedFromCharTest()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<Complex>.CreateChecked<char>((char)0x0000));
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.CreateChecked<char>((char)0x0001));
            AssertBitwiseEqual(32767.0, NumberBaseHelper<Complex>.CreateChecked<char>((char)0x7FFF));
            AssertBitwiseEqual(32768.0, NumberBaseHelper<Complex>.CreateChecked<char>((char)0x8000));
            AssertBitwiseEqual(65535.0, NumberBaseHelper<Complex>.CreateChecked<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateCheckedFromDecimalTest()
        {
            AssertBitwiseEqual(-79228162514264337593543950335.0, NumberBaseHelper<Complex>.CreateChecked<decimal>(decimal.MinValue));
            AssertBitwiseEqual(-1.0, NumberBaseHelper<Complex>.CreateChecked<decimal>(-1.0m));
            AssertBitwiseEqual(-0.0, NumberBaseHelper<Complex>.CreateChecked<decimal>(-0.0m));
            AssertBitwiseEqual(+0.0, NumberBaseHelper<Complex>.CreateChecked<decimal>(+0.0m));
            AssertBitwiseEqual(+1.0, NumberBaseHelper<Complex>.CreateChecked<decimal>(+1.0m));
            AssertBitwiseEqual(+79228162514264337593543950335.0, NumberBaseHelper<Complex>.CreateChecked<decimal>(decimal.MaxValue));
        }

        [Fact]
        public static void CreateCheckedFromDoubleTest()
        {
            AssertBitwiseEqual(double.NegativeInfinity, NumberBaseHelper<Complex>.CreateChecked<double>(double.NegativeInfinity));
            AssertBitwiseEqual(double.MinValue, NumberBaseHelper<Complex>.CreateChecked<double>(double.MinValue));

            AssertBitwiseEqual(-1.0, NumberBaseHelper<Complex>.CreateChecked<double>(-1.0));

            AssertBitwiseEqual(-2.2250738585072014E-308, NumberBaseHelper<Complex>.CreateChecked<double>(-2.2250738585072014E-308));
            AssertBitwiseEqual(-2.2250738585072009E-308, NumberBaseHelper<Complex>.CreateChecked<double>(-2.2250738585072009E-308));
            AssertBitwiseEqual(-double.Epsilon, NumberBaseHelper<Complex>.CreateChecked<double>(-double.Epsilon));
            AssertBitwiseEqual(-0.0, NumberBaseHelper<Complex>.CreateChecked<double>(-0.0));

            AssertBitwiseEqual(+0.0, NumberBaseHelper<Complex>.CreateChecked<double>(+0.0));
            AssertBitwiseEqual(+double.Epsilon, NumberBaseHelper<Complex>.CreateChecked<double>(double.Epsilon));
            AssertBitwiseEqual(+2.2250738585072009E-308, NumberBaseHelper<Complex>.CreateChecked<double>(2.2250738585072009E-308));
            AssertBitwiseEqual(+2.2250738585072014E-308, NumberBaseHelper<Complex>.CreateChecked<double>(2.2250738585072014E-308));

            AssertBitwiseEqual(+1.0, NumberBaseHelper<Complex>.CreateChecked<double>(1.0));

            AssertBitwiseEqual(double.MaxValue, NumberBaseHelper<Complex>.CreateChecked<double>(double.MaxValue));
            AssertBitwiseEqual(double.PositiveInfinity, NumberBaseHelper<Complex>.CreateChecked<double>(double.PositiveInfinity));

            AssertBitwiseEqual(double.NaN, NumberBaseHelper<Complex>.CreateChecked<double>(double.NaN));
        }

        [Fact]
        public static void CreateCheckedFromHalfTest()
        {
            AssertBitwiseEqual(Half.NegativeInfinity, NumberBaseHelper<Complex>.CreateChecked<Half>(Half.NegativeInfinity));

            AssertBitwiseEqual(-65504.0, NumberBaseHelper<Complex>.CreateChecked<Half>(Half.MinValue));
            AssertBitwiseEqual(-1.0, NumberBaseHelper<Complex>.CreateChecked<Half>(Half.NegativeOne));

            AssertBitwiseEqual(-6.103515625E-05, NumberBaseHelper<Complex>.CreateChecked<Half>(-BitConverter.UInt16BitsToHalf(0x0400)));
            AssertBitwiseEqual(-6.097555160522461E-05, NumberBaseHelper<Complex>.CreateChecked<Half>(-BitConverter.UInt16BitsToHalf(0x03FF)));
            AssertBitwiseEqual(-5.960464477539063E-08, NumberBaseHelper<Complex>.CreateChecked<Half>(-Half.Epsilon));
            AssertBitwiseEqual(-0.0, NumberBaseHelper<Complex>.CreateChecked<Half>(Half.NegativeZero));

            AssertBitwiseEqual(+0.0, NumberBaseHelper<Complex>.CreateChecked<Half>(Half.Zero));
            AssertBitwiseEqual(+5.960464477539063E-08, NumberBaseHelper<Complex>.CreateChecked<Half>(Half.Epsilon));
            AssertBitwiseEqual(+6.097555160522461E-05, NumberBaseHelper<Complex>.CreateChecked<Half>(BitConverter.UInt16BitsToHalf(0x03FF)));
            AssertBitwiseEqual(+6.103515625E-05, NumberBaseHelper<Complex>.CreateChecked<Half>(BitConverter.UInt16BitsToHalf(0x0400)));

            AssertBitwiseEqual(+1.0, NumberBaseHelper<Complex>.CreateChecked<Half>(Half.One));
            AssertBitwiseEqual(+65504.0, NumberBaseHelper<Complex>.CreateChecked<Half>(Half.MaxValue));

            AssertBitwiseEqual(Half.PositiveInfinity, NumberBaseHelper<Complex>.CreateChecked<Half>(Half.PositiveInfinity));

            AssertBitwiseEqual(Half.NaN, NumberBaseHelper<Complex>.CreateChecked<Half>(Half.NaN));
        }

        [Fact]
        public static void CreateCheckedFromInt16Test()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<Complex>.CreateChecked<short>(0x0000));
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.CreateChecked<short>(0x0001));
            AssertBitwiseEqual(32767.0, NumberBaseHelper<Complex>.CreateChecked<short>(0x7FFF));
            AssertBitwiseEqual(-32768.0, NumberBaseHelper<Complex>.CreateChecked<short>(unchecked((short)0x8000)));
            AssertBitwiseEqual(-1.0, NumberBaseHelper<Complex>.CreateChecked<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromInt32Test()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<Complex>.CreateChecked<int>(0x00000000));
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.CreateChecked<int>(0x00000001));
            AssertBitwiseEqual(2147483647.0, NumberBaseHelper<Complex>.CreateChecked<int>(0x7FFFFFFF));
            AssertBitwiseEqual(-2147483648.0, NumberBaseHelper<Complex>.CreateChecked<int>(unchecked((int)0x80000000)));
            AssertBitwiseEqual(-1.0, NumberBaseHelper<Complex>.CreateChecked<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromInt64Test()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<Complex>.CreateChecked<long>(0x0000000000000000));
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.CreateChecked<long>(0x0000000000000001));
            AssertBitwiseEqual(9223372036854775807.0, NumberBaseHelper<Complex>.CreateChecked<long>(0x7FFFFFFFFFFFFFFF));
            AssertBitwiseEqual(-9223372036854775808.0, NumberBaseHelper<Complex>.CreateChecked<long>(unchecked(unchecked((long)0x8000000000000000))));
            AssertBitwiseEqual(-1.0, NumberBaseHelper<Complex>.CreateChecked<long>(unchecked(unchecked((long)0xFFFFFFFFFFFFFFFF))));
        }

        [Fact]
        public static void CreateCheckedFromInt128Test()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<Complex>.CreateChecked<Int128>(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000)));
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.CreateChecked<Int128>(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001)));
            AssertBitwiseEqual(170141183460469231731687303715884105727.0, NumberBaseHelper<Complex>.CreateChecked<Int128>(new Int128(0x7FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)));
            AssertBitwiseEqual(-170141183460469231731687303715884105728.0, NumberBaseHelper<Complex>.CreateChecked<Int128>(new Int128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000)));
            AssertBitwiseEqual(-1.0, NumberBaseHelper<Complex>.CreateChecked<Int128>(new Int128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)));
        }

        [Fact]
        public static void CreateCheckedFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual(0.0, NumberBaseHelper<Complex>.CreateChecked<nint>(unchecked((nint)0x0000000000000000)));
                AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.CreateChecked<nint>(unchecked((nint)0x0000000000000001)));
                AssertBitwiseEqual(9223372036854775807.0, NumberBaseHelper<Complex>.CreateChecked<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                AssertBitwiseEqual(-9223372036854775808.0, NumberBaseHelper<Complex>.CreateChecked<nint>(unchecked((nint)0x8000000000000000)));
                AssertBitwiseEqual(-1.0, NumberBaseHelper<Complex>.CreateChecked<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                AssertBitwiseEqual(0.0, NumberBaseHelper<Complex>.CreateChecked<nint>((nint)0x00000000));
                AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.CreateChecked<nint>((nint)0x00000001));
                AssertBitwiseEqual(2147483647.0, NumberBaseHelper<Complex>.CreateChecked<nint>((nint)0x7FFFFFFF));
                AssertBitwiseEqual(-2147483648.0, NumberBaseHelper<Complex>.CreateChecked<nint>(unchecked((nint)0x80000000)));
                AssertBitwiseEqual(-1.0, NumberBaseHelper<Complex>.CreateChecked<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateCheckedFromSByteTest()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<Complex>.CreateChecked<sbyte>(0x00));
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.CreateChecked<sbyte>(0x01));
            AssertBitwiseEqual(127.0, NumberBaseHelper<Complex>.CreateChecked<sbyte>(0x7F));
            AssertBitwiseEqual(-128.0, NumberBaseHelper<Complex>.CreateChecked<sbyte>(unchecked((sbyte)0x80)));
            AssertBitwiseEqual(-1.0, NumberBaseHelper<Complex>.CreateChecked<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateCheckedFromSingleTest()
        {
            AssertBitwiseEqual(float.NegativeInfinity, NumberBaseHelper<Complex>.CreateChecked<float>(float.NegativeInfinity));

            AssertBitwiseEqual(-3.4028234663852886E+38, NumberBaseHelper<Complex>.CreateChecked<float>(float.MinValue));
            AssertBitwiseEqual(-1.0, NumberBaseHelper<Complex>.CreateChecked<float>(-1.0f));

            AssertBitwiseEqual(-1.1754943508222875E-38, NumberBaseHelper<Complex>.CreateChecked<float>(-1.17549435E-38f));
            AssertBitwiseEqual(-1.1754942106924411E-38, NumberBaseHelper<Complex>.CreateChecked<float>(-1.17549421E-38f));
            AssertBitwiseEqual(-1.401298464324817E-45, NumberBaseHelper<Complex>.CreateChecked<float>(-float.Epsilon));
            AssertBitwiseEqual(-0.0, NumberBaseHelper<Complex>.CreateChecked<float>(-0.0f));

            AssertBitwiseEqual(+0.0, NumberBaseHelper<Complex>.CreateChecked<float>(+0.0f));
            AssertBitwiseEqual(+1.401298464324817E-45, NumberBaseHelper<Complex>.CreateChecked<float>(float.Epsilon));
            AssertBitwiseEqual(+1.1754942106924411E-38, NumberBaseHelper<Complex>.CreateChecked<float>(1.17549421E-38f));
            AssertBitwiseEqual(+1.1754943508222875E-38, NumberBaseHelper<Complex>.CreateChecked<float>(1.17549435E-38f));

            AssertBitwiseEqual(+1.0, NumberBaseHelper<Complex>.CreateChecked<float>(1.0f));
            AssertBitwiseEqual(+3.4028234663852886E+38, NumberBaseHelper<Complex>.CreateChecked<float>(float.MaxValue));

            AssertBitwiseEqual(float.PositiveInfinity, NumberBaseHelper<Complex>.CreateChecked<float>(float.PositiveInfinity));

            AssertBitwiseEqual(float.NaN, NumberBaseHelper<Complex>.CreateChecked<float>(float.NaN));
        }

        [Fact]
        public static void CreateCheckedFromUInt16Test()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<Complex>.CreateChecked<ushort>(0x0000));
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.CreateChecked<ushort>(0x0001));
            AssertBitwiseEqual(32767.0, NumberBaseHelper<Complex>.CreateChecked<ushort>(0x7FFF));
            AssertBitwiseEqual(32768.0, NumberBaseHelper<Complex>.CreateChecked<ushort>(0x8000));
            AssertBitwiseEqual(65535.0, NumberBaseHelper<Complex>.CreateChecked<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUInt32Test()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<Complex>.CreateChecked<uint>(0x00000000));
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.CreateChecked<uint>(0x00000001));
            AssertBitwiseEqual(2147483647.0, NumberBaseHelper<Complex>.CreateChecked<uint>(0x7FFFFFFF));
            AssertBitwiseEqual(2147483648.0, NumberBaseHelper<Complex>.CreateChecked<uint>(0x80000000));
            AssertBitwiseEqual(4294967295.0, NumberBaseHelper<Complex>.CreateChecked<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUInt64Test()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<Complex>.CreateChecked<ulong>(0x0000000000000000));
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.CreateChecked<ulong>(0x0000000000000001));
            AssertBitwiseEqual(9223372036854775807.0, NumberBaseHelper<Complex>.CreateChecked<ulong>(0x7FFFFFFFFFFFFFFF));
            AssertBitwiseEqual(9223372036854775808.0, NumberBaseHelper<Complex>.CreateChecked<ulong>(0x8000000000000000));
            AssertBitwiseEqual(18446744073709551615.0, NumberBaseHelper<Complex>.CreateChecked<ulong>(0xFFFFFFFFFFFFFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUInt128Test()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<Complex>.CreateChecked<UInt128>(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000)));
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.CreateChecked<UInt128>(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001)));
            AssertBitwiseEqual(170141183460469231731687303715884105727.0, NumberBaseHelper<Complex>.CreateChecked<UInt128>(new UInt128(0x7FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)));
            AssertBitwiseEqual(170141183460469231731687303715884105728.0, NumberBaseHelper<Complex>.CreateChecked<UInt128>(new UInt128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000)));
            AssertBitwiseEqual(340282366920938463463374607431768211455.0, NumberBaseHelper<Complex>.CreateChecked<UInt128>(new UInt128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)));
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/69795", TestRuntimes.Mono)]
        public static void CreateCheckedFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual(0.0, NumberBaseHelper<Complex>.CreateChecked<nuint>(unchecked((nuint)0x0000000000000000)));
                AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.CreateChecked<nuint>(unchecked((nuint)0x0000000000000001)));
                AssertBitwiseEqual(9223372036854775807.0, NumberBaseHelper<Complex>.CreateChecked<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));

                // https://github.com/dotnet/roslyn/issues/60714
                // AssertBitwiseEqual(9223372036854775808.0, NumberBaseHelper<Complex>.CreateChecked<nuint>(unchecked((nuint)0x8000000000000000)));
                // AssertBitwiseEqual(18446744073709551615.0,NumberBaseHelper<Complex>.CreateChecked<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                AssertBitwiseEqual(0.0, NumberBaseHelper<Complex>.CreateChecked<nuint>((nuint)0x00000000));
                AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.CreateChecked<nuint>((nuint)0x00000001));
                AssertBitwiseEqual(2147483647.0, NumberBaseHelper<Complex>.CreateChecked<nuint>((nuint)0x7FFFFFFF));

                // https://github.com/dotnet/roslyn/issues/60714
                // AssertBitwiseEqual(2147483648.0, NumberBaseHelper<Complex>.CreateChecked<nuint>((nuint)0x80000000));
                // AssertBitwiseEqual(4294967295.0, NumberBaseHelper<Complex>.CreateChecked<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateSaturatingFromByteTest()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<Complex>.CreateSaturating<byte>(0x00));
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.CreateSaturating<byte>(0x01));
            AssertBitwiseEqual(127.0, NumberBaseHelper<Complex>.CreateSaturating<byte>(0x7F));
            AssertBitwiseEqual(128.0, NumberBaseHelper<Complex>.CreateSaturating<byte>(0x80));
            AssertBitwiseEqual(255.0, NumberBaseHelper<Complex>.CreateSaturating<byte>(0xFF));
        }

        [Fact]
        public static void CreateSaturatingFromCharTest()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<Complex>.CreateSaturating<char>((char)0x0000));
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.CreateSaturating<char>((char)0x0001));
            AssertBitwiseEqual(32767.0, NumberBaseHelper<Complex>.CreateSaturating<char>((char)0x7FFF));
            AssertBitwiseEqual(32768.0, NumberBaseHelper<Complex>.CreateSaturating<char>((char)0x8000));
            AssertBitwiseEqual(65535.0, NumberBaseHelper<Complex>.CreateSaturating<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromDecimalTest()
        {
            AssertBitwiseEqual(-79228162514264337593543950335.0, NumberBaseHelper<Complex>.CreateSaturating<decimal>(decimal.MinValue));
            AssertBitwiseEqual(-1.0, NumberBaseHelper<Complex>.CreateSaturating<decimal>(-1.0m));
            AssertBitwiseEqual(-0.0, NumberBaseHelper<Complex>.CreateSaturating<decimal>(-0.0m));
            AssertBitwiseEqual(+0.0, NumberBaseHelper<Complex>.CreateSaturating<decimal>(+0.0m));
            AssertBitwiseEqual(+1.0, NumberBaseHelper<Complex>.CreateSaturating<decimal>(+1.0m));
            AssertBitwiseEqual(+79228162514264337593543950335.0, NumberBaseHelper<Complex>.CreateSaturating<decimal>(decimal.MaxValue));
        }

        [Fact]
        public static void CreateSaturatingFromDoubleTest()
        {
            AssertBitwiseEqual(double.NegativeInfinity, NumberBaseHelper<Complex>.CreateSaturating<double>(double.NegativeInfinity));
            AssertBitwiseEqual(double.MinValue, NumberBaseHelper<Complex>.CreateSaturating<double>(double.MinValue));

            AssertBitwiseEqual(-1.0, NumberBaseHelper<Complex>.CreateSaturating<double>(-1.0));

            AssertBitwiseEqual(-2.2250738585072014E-308, NumberBaseHelper<Complex>.CreateSaturating<double>(-2.2250738585072014E-308));
            AssertBitwiseEqual(-2.2250738585072009E-308, NumberBaseHelper<Complex>.CreateSaturating<double>(-2.2250738585072009E-308));
            AssertBitwiseEqual(-double.Epsilon, NumberBaseHelper<Complex>.CreateSaturating<double>(-double.Epsilon));
            AssertBitwiseEqual(-0.0, NumberBaseHelper<Complex>.CreateSaturating<double>(-0.0));

            AssertBitwiseEqual(+0.0, NumberBaseHelper<Complex>.CreateSaturating<double>(+0.0));
            AssertBitwiseEqual(+double.Epsilon, NumberBaseHelper<Complex>.CreateSaturating<double>(double.Epsilon));
            AssertBitwiseEqual(+2.2250738585072009E-308, NumberBaseHelper<Complex>.CreateSaturating<double>(2.2250738585072009E-308));
            AssertBitwiseEqual(+2.2250738585072014E-308, NumberBaseHelper<Complex>.CreateSaturating<double>(2.2250738585072014E-308));

            AssertBitwiseEqual(+1.0, NumberBaseHelper<Complex>.CreateSaturating<double>(1.0));

            AssertBitwiseEqual(double.MaxValue, NumberBaseHelper<Complex>.CreateSaturating<double>(double.MaxValue));
            AssertBitwiseEqual(double.PositiveInfinity, NumberBaseHelper<Complex>.CreateSaturating<double>(double.PositiveInfinity));

            AssertBitwiseEqual(double.NaN, NumberBaseHelper<Complex>.CreateSaturating<double>(double.NaN));
        }

        [Fact]
        public static void CreateSaturatingFromHalfTest()
        {
            AssertBitwiseEqual(Half.NegativeInfinity, NumberBaseHelper<Complex>.CreateSaturating<Half>(Half.NegativeInfinity));

            AssertBitwiseEqual(-65504.0, NumberBaseHelper<Complex>.CreateSaturating<Half>(Half.MinValue));
            AssertBitwiseEqual(-1.0, NumberBaseHelper<Complex>.CreateSaturating<Half>(Half.NegativeOne));

            AssertBitwiseEqual(-6.103515625E-05, NumberBaseHelper<Complex>.CreateSaturating<Half>(-BitConverter.UInt16BitsToHalf(0x0400)));
            AssertBitwiseEqual(-6.097555160522461E-05, NumberBaseHelper<Complex>.CreateSaturating<Half>(-BitConverter.UInt16BitsToHalf(0x03FF)));
            AssertBitwiseEqual(-5.960464477539063E-08, NumberBaseHelper<Complex>.CreateSaturating<Half>(-Half.Epsilon));
            AssertBitwiseEqual(-0.0, NumberBaseHelper<Complex>.CreateSaturating<Half>(Half.NegativeZero));

            AssertBitwiseEqual(+0.0, NumberBaseHelper<Complex>.CreateSaturating<Half>(Half.Zero));
            AssertBitwiseEqual(+5.960464477539063E-08, NumberBaseHelper<Complex>.CreateSaturating<Half>(Half.Epsilon));
            AssertBitwiseEqual(+6.097555160522461E-05, NumberBaseHelper<Complex>.CreateSaturating<Half>(BitConverter.UInt16BitsToHalf(0x03FF)));
            AssertBitwiseEqual(+6.103515625E-05, NumberBaseHelper<Complex>.CreateSaturating<Half>(BitConverter.UInt16BitsToHalf(0x0400)));

            AssertBitwiseEqual(+1.0, NumberBaseHelper<Complex>.CreateSaturating<Half>(Half.One));
            AssertBitwiseEqual(+65504.0, NumberBaseHelper<Complex>.CreateSaturating<Half>(Half.MaxValue));

            AssertBitwiseEqual(Half.PositiveInfinity, NumberBaseHelper<Complex>.CreateSaturating<Half>(Half.PositiveInfinity));

            AssertBitwiseEqual(Half.NaN, NumberBaseHelper<Complex>.CreateSaturating<Half>(Half.NaN));
        }

        [Fact]
        public static void CreateSaturatingFromInt16Test()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<Complex>.CreateSaturating<short>(0x0000));
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.CreateSaturating<short>(0x0001));
            AssertBitwiseEqual(32767.0, NumberBaseHelper<Complex>.CreateSaturating<short>(0x7FFF));
            AssertBitwiseEqual(-32768.0, NumberBaseHelper<Complex>.CreateSaturating<short>(unchecked((short)0x8000)));
            AssertBitwiseEqual(-1.0, NumberBaseHelper<Complex>.CreateSaturating<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromInt32Test()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<Complex>.CreateSaturating<int>(0x00000000));
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.CreateSaturating<int>(0x00000001));
            AssertBitwiseEqual(2147483647.0, NumberBaseHelper<Complex>.CreateSaturating<int>(0x7FFFFFFF));
            AssertBitwiseEqual(-2147483648.0, NumberBaseHelper<Complex>.CreateSaturating<int>(unchecked((int)0x80000000)));
            AssertBitwiseEqual(-1.0, NumberBaseHelper<Complex>.CreateSaturating<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromInt64Test()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<Complex>.CreateSaturating<long>(0x0000000000000000));
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.CreateSaturating<long>(0x0000000000000001));
            AssertBitwiseEqual(9223372036854775807.0, NumberBaseHelper<Complex>.CreateSaturating<long>(0x7FFFFFFFFFFFFFFF));
            AssertBitwiseEqual(-9223372036854775808.0, NumberBaseHelper<Complex>.CreateSaturating<long>(unchecked(unchecked((long)0x8000000000000000))));
            AssertBitwiseEqual(-1.0, NumberBaseHelper<Complex>.CreateSaturating<long>(unchecked(unchecked((long)0xFFFFFFFFFFFFFFFF))));
        }

        [Fact]
        public static void CreateSaturatingFromInt128Test()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<Complex>.CreateSaturating<Int128>(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000)));
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.CreateSaturating<Int128>(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001)));
            AssertBitwiseEqual(170141183460469231731687303715884105727.0, NumberBaseHelper<Complex>.CreateSaturating<Int128>(new Int128(0x7FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)));
            AssertBitwiseEqual(-170141183460469231731687303715884105728.0, NumberBaseHelper<Complex>.CreateSaturating<Int128>(new Int128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000)));
            AssertBitwiseEqual(-1.0, NumberBaseHelper<Complex>.CreateSaturating<Int128>(new Int128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual(0.0, NumberBaseHelper<Complex>.CreateSaturating<nint>(unchecked((nint)0x0000000000000000)));
                AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.CreateSaturating<nint>(unchecked((nint)0x0000000000000001)));
                AssertBitwiseEqual(9223372036854775807.0, NumberBaseHelper<Complex>.CreateSaturating<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                AssertBitwiseEqual(-9223372036854775808.0, NumberBaseHelper<Complex>.CreateSaturating<nint>(unchecked((nint)0x8000000000000000)));
                AssertBitwiseEqual(-1.0, NumberBaseHelper<Complex>.CreateSaturating<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                AssertBitwiseEqual(0.0, NumberBaseHelper<Complex>.CreateSaturating<nint>((nint)0x00000000));
                AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.CreateSaturating<nint>((nint)0x00000001));
                AssertBitwiseEqual(2147483647.0, NumberBaseHelper<Complex>.CreateSaturating<nint>((nint)0x7FFFFFFF));
                AssertBitwiseEqual(-2147483648.0, NumberBaseHelper<Complex>.CreateSaturating<nint>(unchecked((nint)0x80000000)));
                AssertBitwiseEqual(-1.0, NumberBaseHelper<Complex>.CreateSaturating<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateSaturatingFromSByteTest()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<Complex>.CreateSaturating<sbyte>(0x00));
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.CreateSaturating<sbyte>(0x01));
            AssertBitwiseEqual(127.0, NumberBaseHelper<Complex>.CreateSaturating<sbyte>(0x7F));
            AssertBitwiseEqual(-128.0, NumberBaseHelper<Complex>.CreateSaturating<sbyte>(unchecked((sbyte)0x80)));
            AssertBitwiseEqual(-1.0, NumberBaseHelper<Complex>.CreateSaturating<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateSaturatingFromSingleTest()
        {
            AssertBitwiseEqual(float.NegativeInfinity, NumberBaseHelper<Complex>.CreateSaturating<float>(float.NegativeInfinity));

            AssertBitwiseEqual(-3.4028234663852886E+38, NumberBaseHelper<Complex>.CreateSaturating<float>(float.MinValue));
            AssertBitwiseEqual(-1.0, NumberBaseHelper<Complex>.CreateSaturating<float>(-1.0f));

            AssertBitwiseEqual(-1.1754943508222875E-38, NumberBaseHelper<Complex>.CreateSaturating<float>(-1.17549435E-38f));
            AssertBitwiseEqual(-1.1754942106924411E-38, NumberBaseHelper<Complex>.CreateSaturating<float>(-1.17549421E-38f));
            AssertBitwiseEqual(-1.401298464324817E-45, NumberBaseHelper<Complex>.CreateSaturating<float>(-float.Epsilon));
            AssertBitwiseEqual(-0.0, NumberBaseHelper<Complex>.CreateSaturating<float>(-0.0f));

            AssertBitwiseEqual(+0.0, NumberBaseHelper<Complex>.CreateSaturating<float>(+0.0f));
            AssertBitwiseEqual(+1.401298464324817E-45, NumberBaseHelper<Complex>.CreateSaturating<float>(float.Epsilon));
            AssertBitwiseEqual(+1.1754942106924411E-38, NumberBaseHelper<Complex>.CreateSaturating<float>(1.17549421E-38f));
            AssertBitwiseEqual(+1.1754943508222875E-38, NumberBaseHelper<Complex>.CreateSaturating<float>(1.17549435E-38f));

            AssertBitwiseEqual(+1.0, NumberBaseHelper<Complex>.CreateSaturating<float>(1.0f));
            AssertBitwiseEqual(+3.4028234663852886E+38, NumberBaseHelper<Complex>.CreateSaturating<float>(float.MaxValue));

            AssertBitwiseEqual(float.PositiveInfinity, NumberBaseHelper<Complex>.CreateSaturating<float>(float.PositiveInfinity));

            AssertBitwiseEqual(float.NaN, NumberBaseHelper<Complex>.CreateSaturating<float>(float.NaN));
        }

        [Fact]
        public static void CreateSaturatingFromUInt16Test()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<Complex>.CreateSaturating<ushort>(0x0000));
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.CreateSaturating<ushort>(0x0001));
            AssertBitwiseEqual(32767.0, NumberBaseHelper<Complex>.CreateSaturating<ushort>(0x7FFF));
            AssertBitwiseEqual(32768.0, NumberBaseHelper<Complex>.CreateSaturating<ushort>(0x8000));
            AssertBitwiseEqual(65535.0, NumberBaseHelper<Complex>.CreateSaturating<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUInt32Test()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<Complex>.CreateSaturating<uint>(0x00000000));
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.CreateSaturating<uint>(0x00000001));
            AssertBitwiseEqual(2147483647.0, NumberBaseHelper<Complex>.CreateSaturating<uint>(0x7FFFFFFF));
            AssertBitwiseEqual(2147483648.0, NumberBaseHelper<Complex>.CreateSaturating<uint>(0x80000000));
            AssertBitwiseEqual(4294967295.0, NumberBaseHelper<Complex>.CreateSaturating<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUInt64Test()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<Complex>.CreateSaturating<ulong>(0x0000000000000000));
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.CreateSaturating<ulong>(0x0000000000000001));
            AssertBitwiseEqual(9223372036854775807.0, NumberBaseHelper<Complex>.CreateSaturating<ulong>(0x7FFFFFFFFFFFFFFF));
            AssertBitwiseEqual(9223372036854775808.0, NumberBaseHelper<Complex>.CreateSaturating<ulong>(0x8000000000000000));
            AssertBitwiseEqual(18446744073709551615.0, NumberBaseHelper<Complex>.CreateSaturating<ulong>(0xFFFFFFFFFFFFFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUInt128Test()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<Complex>.CreateSaturating<UInt128>(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000)));
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.CreateSaturating<UInt128>(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001)));
            AssertBitwiseEqual(170141183460469231731687303715884105727.0, NumberBaseHelper<Complex>.CreateSaturating<UInt128>(new UInt128(0x7FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)));
            AssertBitwiseEqual(170141183460469231731687303715884105728.0, NumberBaseHelper<Complex>.CreateSaturating<UInt128>(new UInt128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000)));
            AssertBitwiseEqual(340282366920938463463374607431768211455.0, NumberBaseHelper<Complex>.CreateSaturating<UInt128>(new UInt128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)));
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/69795", TestRuntimes.Mono)]
        public static void CreateSaturatingFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual(0.0, NumberBaseHelper<Complex>.CreateSaturating<nuint>(unchecked((nuint)0x0000000000000000)));
                AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.CreateSaturating<nuint>(unchecked((nuint)0x0000000000000001)));
                AssertBitwiseEqual(9223372036854775807.0, NumberBaseHelper<Complex>.CreateSaturating<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));

                // https://github.com/dotnet/roslyn/issues/60714
                // AssertBitwiseEqual(9223372036854775808.0, NumberBaseHelper<Complex>.CreateSaturating<nuint>(unchecked((nuint)0x8000000000000000)));
                // AssertBitwiseEqual(18446744073709551615.0, NumberBaseHelper<Complex>.CreateSaturating<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                AssertBitwiseEqual(0.0, NumberBaseHelper<Complex>.CreateSaturating<nuint>((nuint)0x00000000));
                AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.CreateSaturating<nuint>((nuint)0x00000001));
                AssertBitwiseEqual(2147483647.0, NumberBaseHelper<Complex>.CreateSaturating<nuint>((nuint)0x7FFFFFFF));

                // https://github.com/dotnet/roslyn/issues/60714
                // AssertBitwiseEqual(2147483648.0, NumberBaseHelper<Complex>.CreateSaturating<nuint>((nuint)0x80000000));
                // AssertBitwiseEqual(4294967295.0, NumberBaseHelper<Complex>.CreateSaturating<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateTruncatingFromByteTest()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<Complex>.CreateTruncating<byte>(0x00));
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.CreateTruncating<byte>(0x01));
            AssertBitwiseEqual(127.0, NumberBaseHelper<Complex>.CreateTruncating<byte>(0x7F));
            AssertBitwiseEqual(128.0, NumberBaseHelper<Complex>.CreateTruncating<byte>(0x80));
            AssertBitwiseEqual(255.0, NumberBaseHelper<Complex>.CreateTruncating<byte>(0xFF));
        }

        [Fact]
        public static void CreateTruncatingFromCharTest()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<Complex>.CreateTruncating<char>((char)0x0000));
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.CreateTruncating<char>((char)0x0001));
            AssertBitwiseEqual(32767.0, NumberBaseHelper<Complex>.CreateTruncating<char>((char)0x7FFF));
            AssertBitwiseEqual(32768.0, NumberBaseHelper<Complex>.CreateTruncating<char>((char)0x8000));
            AssertBitwiseEqual(65535.0, NumberBaseHelper<Complex>.CreateTruncating<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromDecimalTest()
        {
            AssertBitwiseEqual(-79228162514264337593543950335.0, NumberBaseHelper<Complex>.CreateTruncating<decimal>(decimal.MinValue));
            AssertBitwiseEqual(-1.0, NumberBaseHelper<Complex>.CreateTruncating<decimal>(-1.0m));
            AssertBitwiseEqual(-0.0, NumberBaseHelper<Complex>.CreateTruncating<decimal>(-0.0m));
            AssertBitwiseEqual(+0.0, NumberBaseHelper<Complex>.CreateTruncating<decimal>(+0.0m));
            AssertBitwiseEqual(+1.0, NumberBaseHelper<Complex>.CreateTruncating<decimal>(+1.0m));
            AssertBitwiseEqual(+79228162514264337593543950335.0, NumberBaseHelper<Complex>.CreateTruncating<decimal>(decimal.MaxValue));
        }

        [Fact]
        public static void CreateTruncatingFromDoubleTest()
        {
            AssertBitwiseEqual(double.NegativeInfinity, NumberBaseHelper<Complex>.CreateTruncating<double>(double.NegativeInfinity));
            AssertBitwiseEqual(double.MinValue, NumberBaseHelper<Complex>.CreateTruncating<double>(double.MinValue));

            AssertBitwiseEqual(-1.0, NumberBaseHelper<Complex>.CreateTruncating<double>(-1.0));

            AssertBitwiseEqual(-2.2250738585072014E-308, NumberBaseHelper<Complex>.CreateTruncating<double>(-2.2250738585072014E-308));
            AssertBitwiseEqual(-2.2250738585072009E-308, NumberBaseHelper<Complex>.CreateTruncating<double>(-2.2250738585072009E-308));
            AssertBitwiseEqual(-double.Epsilon, NumberBaseHelper<Complex>.CreateTruncating<double>(-double.Epsilon));
            AssertBitwiseEqual(-0.0, NumberBaseHelper<Complex>.CreateTruncating<double>(-0.0));

            AssertBitwiseEqual(+0.0, NumberBaseHelper<Complex>.CreateTruncating<double>(+0.0));
            AssertBitwiseEqual(+double.Epsilon, NumberBaseHelper<Complex>.CreateTruncating<double>(double.Epsilon));
            AssertBitwiseEqual(+2.2250738585072009E-308, NumberBaseHelper<Complex>.CreateTruncating<double>(2.2250738585072009E-308));
            AssertBitwiseEqual(+2.2250738585072014E-308, NumberBaseHelper<Complex>.CreateTruncating<double>(2.2250738585072014E-308));

            AssertBitwiseEqual(+1.0, NumberBaseHelper<Complex>.CreateTruncating<double>(1.0));

            AssertBitwiseEqual(double.MaxValue, NumberBaseHelper<Complex>.CreateTruncating<double>(double.MaxValue));
            AssertBitwiseEqual(double.PositiveInfinity, NumberBaseHelper<Complex>.CreateTruncating<double>(double.PositiveInfinity));

            AssertBitwiseEqual(double.NaN, NumberBaseHelper<Complex>.CreateTruncating<double>(double.NaN));
        }

        [Fact]
        public static void CreateTruncatingFromHalfTest()
        {
            AssertBitwiseEqual(Half.NegativeInfinity, NumberBaseHelper<Complex>.CreateTruncating<Half>(Half.NegativeInfinity));

            AssertBitwiseEqual(-65504.0, NumberBaseHelper<Complex>.CreateTruncating<Half>(Half.MinValue));
            AssertBitwiseEqual(-1.0, NumberBaseHelper<Complex>.CreateTruncating<Half>(Half.NegativeOne));

            AssertBitwiseEqual(-6.103515625E-05, NumberBaseHelper<Complex>.CreateTruncating<Half>(-BitConverter.UInt16BitsToHalf(0x0400)));
            AssertBitwiseEqual(-6.097555160522461E-05, NumberBaseHelper<Complex>.CreateTruncating<Half>(-BitConverter.UInt16BitsToHalf(0x03FF)));
            AssertBitwiseEqual(-5.960464477539063E-08, NumberBaseHelper<Complex>.CreateTruncating<Half>(-Half.Epsilon));
            AssertBitwiseEqual(-0.0, NumberBaseHelper<Complex>.CreateTruncating<Half>(Half.NegativeZero));

            AssertBitwiseEqual(+0.0, NumberBaseHelper<Complex>.CreateTruncating<Half>(Half.Zero));
            AssertBitwiseEqual(+5.960464477539063E-08, NumberBaseHelper<Complex>.CreateTruncating<Half>(Half.Epsilon));
            AssertBitwiseEqual(+6.097555160522461E-05, NumberBaseHelper<Complex>.CreateTruncating<Half>(BitConverter.UInt16BitsToHalf(0x03FF)));
            AssertBitwiseEqual(+6.103515625E-05, NumberBaseHelper<Complex>.CreateTruncating<Half>(BitConverter.UInt16BitsToHalf(0x0400)));

            AssertBitwiseEqual(+1.0, NumberBaseHelper<Complex>.CreateTruncating<Half>(Half.One));
            AssertBitwiseEqual(+65504.0, NumberBaseHelper<Complex>.CreateTruncating<Half>(Half.MaxValue));

            AssertBitwiseEqual(Half.PositiveInfinity, NumberBaseHelper<Complex>.CreateTruncating<Half>(Half.PositiveInfinity));

            AssertBitwiseEqual(Half.NaN, NumberBaseHelper<Complex>.CreateTruncating<Half>(Half.NaN));
        }

        [Fact]
        public static void CreateTruncatingFromInt16Test()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<Complex>.CreateTruncating<short>(0x0000));
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.CreateTruncating<short>(0x0001));
            AssertBitwiseEqual(32767.0, NumberBaseHelper<Complex>.CreateTruncating<short>(0x7FFF));
            AssertBitwiseEqual(-32768.0, NumberBaseHelper<Complex>.CreateTruncating<short>(unchecked((short)0x8000)));
            AssertBitwiseEqual(-1.0, NumberBaseHelper<Complex>.CreateTruncating<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateTruncatingFromInt32Test()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<Complex>.CreateTruncating<int>(0x00000000));
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.CreateTruncating<int>(0x00000001));
            AssertBitwiseEqual(2147483647.0, NumberBaseHelper<Complex>.CreateTruncating<int>(0x7FFFFFFF));
            AssertBitwiseEqual(-2147483648.0, NumberBaseHelper<Complex>.CreateTruncating<int>(unchecked((int)0x80000000)));
            AssertBitwiseEqual(-1.0, NumberBaseHelper<Complex>.CreateTruncating<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateTruncatingFromInt64Test()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<Complex>.CreateTruncating<long>(0x0000000000000000));
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.CreateTruncating<long>(0x0000000000000001));
            AssertBitwiseEqual(9223372036854775807.0, NumberBaseHelper<Complex>.CreateTruncating<long>(0x7FFFFFFFFFFFFFFF));
            AssertBitwiseEqual(-9223372036854775808.0, NumberBaseHelper<Complex>.CreateTruncating<long>(unchecked(unchecked((long)0x8000000000000000))));
            AssertBitwiseEqual(-1.0, NumberBaseHelper<Complex>.CreateTruncating<long>(unchecked(unchecked((long)0xFFFFFFFFFFFFFFFF))));
        }

        [Fact]
        public static void CreateTruncatingFromInt128Test()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<Complex>.CreateTruncating<Int128>(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000)));
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.CreateTruncating<Int128>(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001)));
            AssertBitwiseEqual(170141183460469231731687303715884105727.0, NumberBaseHelper<Complex>.CreateTruncating<Int128>(new Int128(0x7FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)));
            AssertBitwiseEqual(-170141183460469231731687303715884105728.0, NumberBaseHelper<Complex>.CreateTruncating<Int128>(new Int128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000)));
            AssertBitwiseEqual(-1.0, NumberBaseHelper<Complex>.CreateTruncating<Int128>(new Int128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)));
        }

        [Fact]
        public static void CreateTruncatingFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual(0.0, NumberBaseHelper<Complex>.CreateTruncating<nint>(unchecked((nint)0x0000000000000000)));
                AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.CreateTruncating<nint>(unchecked((nint)0x0000000000000001)));
                AssertBitwiseEqual(9223372036854775807.0, NumberBaseHelper<Complex>.CreateTruncating<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                AssertBitwiseEqual(-9223372036854775808.0, NumberBaseHelper<Complex>.CreateTruncating<nint>(unchecked((nint)0x8000000000000000)));
                AssertBitwiseEqual(-1.0, NumberBaseHelper<Complex>.CreateTruncating<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                AssertBitwiseEqual(0.0, NumberBaseHelper<Complex>.CreateTruncating<nint>((nint)0x00000000));
                AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.CreateTruncating<nint>((nint)0x00000001));
                AssertBitwiseEqual(2147483647.0, NumberBaseHelper<Complex>.CreateTruncating<nint>((nint)0x7FFFFFFF));
                AssertBitwiseEqual(-2147483648.0, NumberBaseHelper<Complex>.CreateTruncating<nint>(unchecked((nint)0x80000000)));
                AssertBitwiseEqual(-1.0, NumberBaseHelper<Complex>.CreateTruncating<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateTruncatingFromSByteTest()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<Complex>.CreateTruncating<sbyte>(0x00));
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.CreateTruncating<sbyte>(0x01));
            AssertBitwiseEqual(127.0, NumberBaseHelper<Complex>.CreateTruncating<sbyte>(0x7F));
            AssertBitwiseEqual(-128.0, NumberBaseHelper<Complex>.CreateTruncating<sbyte>(unchecked((sbyte)0x80)));
            AssertBitwiseEqual(-1.0, NumberBaseHelper<Complex>.CreateTruncating<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateTruncatingFromSingleTest()
        {
            AssertBitwiseEqual(float.NegativeInfinity, NumberBaseHelper<Complex>.CreateTruncating<float>(float.NegativeInfinity));

            AssertBitwiseEqual(-3.4028234663852886E+38, NumberBaseHelper<Complex>.CreateTruncating<float>(float.MinValue));
            AssertBitwiseEqual(-1.0, NumberBaseHelper<Complex>.CreateTruncating<float>(-1.0f));

            AssertBitwiseEqual(-1.1754943508222875E-38, NumberBaseHelper<Complex>.CreateTruncating<float>(-1.17549435E-38f));
            AssertBitwiseEqual(-1.1754942106924411E-38, NumberBaseHelper<Complex>.CreateTruncating<float>(-1.17549421E-38f));
            AssertBitwiseEqual(-1.401298464324817E-45, NumberBaseHelper<Complex>.CreateTruncating<float>(-float.Epsilon));
            AssertBitwiseEqual(-0.0, NumberBaseHelper<Complex>.CreateTruncating<float>(-0.0f));

            AssertBitwiseEqual(+0.0, NumberBaseHelper<Complex>.CreateTruncating<float>(+0.0f));
            AssertBitwiseEqual(+1.401298464324817E-45, NumberBaseHelper<Complex>.CreateTruncating<float>(float.Epsilon));
            AssertBitwiseEqual(+1.1754942106924411E-38, NumberBaseHelper<Complex>.CreateTruncating<float>(1.17549421E-38f));
            AssertBitwiseEqual(+1.1754943508222875E-38, NumberBaseHelper<Complex>.CreateTruncating<float>(1.17549435E-38f));

            AssertBitwiseEqual(+1.0, NumberBaseHelper<Complex>.CreateTruncating<float>(1.0f));
            AssertBitwiseEqual(+3.4028234663852886E+38, NumberBaseHelper<Complex>.CreateTruncating<float>(float.MaxValue));

            AssertBitwiseEqual(float.PositiveInfinity, NumberBaseHelper<Complex>.CreateTruncating<float>(float.PositiveInfinity));

            AssertBitwiseEqual(float.NaN, NumberBaseHelper<Complex>.CreateTruncating<float>(float.NaN));
        }

        [Fact]
        public static void CreateTruncatingFromUInt16Test()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<Complex>.CreateTruncating<ushort>(0x0000));
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.CreateTruncating<ushort>(0x0001));
            AssertBitwiseEqual(32767.0, NumberBaseHelper<Complex>.CreateTruncating<ushort>(0x7FFF));
            AssertBitwiseEqual(32768.0, NumberBaseHelper<Complex>.CreateTruncating<ushort>(0x8000));
            AssertBitwiseEqual(65535.0, NumberBaseHelper<Complex>.CreateTruncating<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt32Test()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<Complex>.CreateTruncating<uint>(0x00000000));
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.CreateTruncating<uint>(0x00000001));
            AssertBitwiseEqual(2147483647.0, NumberBaseHelper<Complex>.CreateTruncating<uint>(0x7FFFFFFF));
            AssertBitwiseEqual(2147483648.0, NumberBaseHelper<Complex>.CreateTruncating<uint>(0x80000000));
            AssertBitwiseEqual(4294967295.0, NumberBaseHelper<Complex>.CreateTruncating<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt64Test()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<Complex>.CreateTruncating<ulong>(0x0000000000000000));
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.CreateTruncating<ulong>(0x0000000000000001));
            AssertBitwiseEqual(9223372036854775807.0, NumberBaseHelper<Complex>.CreateTruncating<ulong>(0x7FFFFFFFFFFFFFFF));
            AssertBitwiseEqual(9223372036854775808.0, NumberBaseHelper<Complex>.CreateTruncating<ulong>(0x8000000000000000));
            AssertBitwiseEqual(18446744073709551615.0, NumberBaseHelper<Complex>.CreateTruncating<ulong>(0xFFFFFFFFFFFFFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt128Test()
        {
            AssertBitwiseEqual(0.0, NumberBaseHelper<Complex>.CreateTruncating<UInt128>(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000)));
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.CreateTruncating<UInt128>(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001)));
            AssertBitwiseEqual(170141183460469231731687303715884105727.0, NumberBaseHelper<Complex>.CreateTruncating<UInt128>(new UInt128(0x7FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)));
            AssertBitwiseEqual(170141183460469231731687303715884105728.0, NumberBaseHelper<Complex>.CreateTruncating<UInt128>(new UInt128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000)));
            AssertBitwiseEqual(340282366920938463463374607431768211455.0, NumberBaseHelper<Complex>.CreateTruncating<UInt128>(new UInt128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)));
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/69795", TestRuntimes.Mono)]
        public static void CreateTruncatingFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                AssertBitwiseEqual(0.0, NumberBaseHelper<Complex>.CreateTruncating<nuint>(unchecked((nuint)0x0000000000000000)));
                AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.CreateTruncating<nuint>(unchecked((nuint)0x0000000000000001)));
                AssertBitwiseEqual(9223372036854775807.0, NumberBaseHelper<Complex>.CreateTruncating<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));

                // https://github.com/dotnet/roslyn/issues/60714
                // AssertBitwiseEqual(9223372036854775808.0, NumberBaseHelper<Complex>.CreateTruncating<nuint>(unchecked((nuint)0x8000000000000000)));
                // AssertBitwiseEqual(18446744073709551615.0, NumberBaseHelper<Complex>.CreateTruncating<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                AssertBitwiseEqual(0.0, NumberBaseHelper<Complex>.CreateTruncating<nuint>((nuint)0x00000000));
                AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.CreateTruncating<nuint>((nuint)0x00000001));
                AssertBitwiseEqual(2147483647.0, NumberBaseHelper<Complex>.CreateTruncating<nuint>((nuint)0x7FFFFFFF));

                // https://github.com/dotnet/roslyn/issues/60714
                // AssertBitwiseEqual(2147483648.0, NumberBaseHelper<Complex>.CreateTruncating<nuint>((nuint)0x80000000));
                // AssertBitwiseEqual(4294967295.0, NumberBaseHelper<Complex>.CreateTruncating<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void IsCanonicalTest()
        {
            Assert.True(NumberBaseHelper<Complex>.IsCanonical(double.NegativeInfinity));
            Assert.True(NumberBaseHelper<Complex>.IsCanonical(double.MinValue));
            Assert.True(NumberBaseHelper<Complex>.IsCanonical(-1.0));
            Assert.True(NumberBaseHelper<Complex>.IsCanonical(-MinNormal));
            Assert.True(NumberBaseHelper<Complex>.IsCanonical(-MaxSubnormal));
            Assert.True(NumberBaseHelper<Complex>.IsCanonical(-double.Epsilon));
            Assert.True(NumberBaseHelper<Complex>.IsCanonical(-0.0));
            Assert.True(NumberBaseHelper<Complex>.IsCanonical(double.NaN));
            Assert.True(NumberBaseHelper<Complex>.IsCanonical(0.0));
            Assert.True(NumberBaseHelper<Complex>.IsCanonical(double.Epsilon));
            Assert.True(NumberBaseHelper<Complex>.IsCanonical(MaxSubnormal));
            Assert.True(NumberBaseHelper<Complex>.IsCanonical(MinNormal));
            Assert.True(NumberBaseHelper<Complex>.IsCanonical(1.0));
            Assert.True(NumberBaseHelper<Complex>.IsCanonical(double.MaxValue));
            Assert.True(NumberBaseHelper<Complex>.IsCanonical(double.PositiveInfinity));

            Assert.True(NumberBaseHelper<Complex>.IsCanonical(new Complex(-1.0, double.NegativeInfinity)));
            Assert.True(NumberBaseHelper<Complex>.IsCanonical(new Complex(-1.0, -1.0)));
            Assert.True(NumberBaseHelper<Complex>.IsCanonical(new Complex(-1.0, -0.0)));
            Assert.True(NumberBaseHelper<Complex>.IsCanonical(new Complex(-1.0, double.NaN)));
            Assert.True(NumberBaseHelper<Complex>.IsCanonical(new Complex(-1.0, 0.0)));
            Assert.True(NumberBaseHelper<Complex>.IsCanonical(new Complex(-1.0, 1.0)));
            Assert.True(NumberBaseHelper<Complex>.IsCanonical(new Complex(-1.0, double.PositiveInfinity)));

            Assert.True(NumberBaseHelper<Complex>.IsCanonical(new Complex(-0.0, double.NegativeInfinity)));
            Assert.True(NumberBaseHelper<Complex>.IsCanonical(new Complex(-0.0, -1.0)));
            Assert.True(NumberBaseHelper<Complex>.IsCanonical(new Complex(-0.0, -0.0)));
            Assert.True(NumberBaseHelper<Complex>.IsCanonical(new Complex(-0.0, double.NaN)));
            Assert.True(NumberBaseHelper<Complex>.IsCanonical(new Complex(-0.0, 0.0)));
            Assert.True(NumberBaseHelper<Complex>.IsCanonical(new Complex(-0.0, 1.0)));
            Assert.True(NumberBaseHelper<Complex>.IsCanonical(new Complex(-0.0, double.PositiveInfinity)));

            Assert.True(NumberBaseHelper<Complex>.IsCanonical(new Complex(double.NaN, double.NegativeInfinity)));
            Assert.True(NumberBaseHelper<Complex>.IsCanonical(new Complex(double.NaN, -1.0)));
            Assert.True(NumberBaseHelper<Complex>.IsCanonical(new Complex(double.NaN, -0.0)));
            Assert.True(NumberBaseHelper<Complex>.IsCanonical(new Complex(double.NaN, double.NaN)));
            Assert.True(NumberBaseHelper<Complex>.IsCanonical(new Complex(double.NaN, 0.0)));
            Assert.True(NumberBaseHelper<Complex>.IsCanonical(new Complex(double.NaN, 1.0)));
            Assert.True(NumberBaseHelper<Complex>.IsCanonical(new Complex(double.NaN, double.PositiveInfinity)));

            Assert.True(NumberBaseHelper<Complex>.IsCanonical(new Complex(0.0, double.NegativeInfinity)));
            Assert.True(NumberBaseHelper<Complex>.IsCanonical(new Complex(0.0, -1.0)));
            Assert.True(NumberBaseHelper<Complex>.IsCanonical(new Complex(0.0, -0.0)));
            Assert.True(NumberBaseHelper<Complex>.IsCanonical(new Complex(0.0, double.NaN)));
            Assert.True(NumberBaseHelper<Complex>.IsCanonical(new Complex(0.0, 0.0)));
            Assert.True(NumberBaseHelper<Complex>.IsCanonical(new Complex(0.0, 1.0)));
            Assert.True(NumberBaseHelper<Complex>.IsCanonical(new Complex(0.0, double.PositiveInfinity)));

            Assert.True(NumberBaseHelper<Complex>.IsCanonical(new Complex(1.0, double.NegativeInfinity)));
            Assert.True(NumberBaseHelper<Complex>.IsCanonical(new Complex(1.0, -1.0)));
            Assert.True(NumberBaseHelper<Complex>.IsCanonical(new Complex(1.0, -0.0)));
            Assert.True(NumberBaseHelper<Complex>.IsCanonical(new Complex(1.0, double.NaN)));
            Assert.True(NumberBaseHelper<Complex>.IsCanonical(new Complex(1.0, 0.0)));
            Assert.True(NumberBaseHelper<Complex>.IsCanonical(new Complex(1.0, 1.0)));
            Assert.True(NumberBaseHelper<Complex>.IsCanonical(new Complex(1.0, double.PositiveInfinity)));
        }

        [Fact]
        public static void IsComplexNumberTest()
        {
            Assert.False(NumberBaseHelper<Complex>.IsComplexNumber(double.NegativeInfinity));
            Assert.False(NumberBaseHelper<Complex>.IsComplexNumber(double.MinValue));
            Assert.False(NumberBaseHelper<Complex>.IsComplexNumber(-1.0));
            Assert.False(NumberBaseHelper<Complex>.IsComplexNumber(-MinNormal));
            Assert.False(NumberBaseHelper<Complex>.IsComplexNumber(-MaxSubnormal));
            Assert.False(NumberBaseHelper<Complex>.IsComplexNumber(-double.Epsilon));
            Assert.False(NumberBaseHelper<Complex>.IsComplexNumber(-0.0));
            Assert.False(NumberBaseHelper<Complex>.IsComplexNumber(double.NaN));
            Assert.False(NumberBaseHelper<Complex>.IsComplexNumber(0.0));
            Assert.False(NumberBaseHelper<Complex>.IsComplexNumber(double.Epsilon));
            Assert.False(NumberBaseHelper<Complex>.IsComplexNumber(MaxSubnormal));
            Assert.False(NumberBaseHelper<Complex>.IsComplexNumber(MinNormal));
            Assert.False(NumberBaseHelper<Complex>.IsComplexNumber(1.0));
            Assert.False(NumberBaseHelper<Complex>.IsComplexNumber(double.MaxValue));
            Assert.False(NumberBaseHelper<Complex>.IsComplexNumber(double.PositiveInfinity));

            Assert.True(NumberBaseHelper<Complex>.IsComplexNumber(new Complex(-1.0, double.NegativeInfinity)));
            Assert.True(NumberBaseHelper<Complex>.IsComplexNumber(new Complex(-1.0, -1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsComplexNumber(new Complex(-1.0, -0.0)));
            Assert.True(NumberBaseHelper<Complex>.IsComplexNumber(new Complex(-1.0, double.NaN)));
            Assert.False(NumberBaseHelper<Complex>.IsComplexNumber(new Complex(-1.0, 0.0)));
            Assert.True(NumberBaseHelper<Complex>.IsComplexNumber(new Complex(-1.0, 1.0)));
            Assert.True(NumberBaseHelper<Complex>.IsComplexNumber(new Complex(-1.0, double.PositiveInfinity)));

            Assert.False(NumberBaseHelper<Complex>.IsComplexNumber(new Complex(-0.0, double.NegativeInfinity)));
            Assert.False(NumberBaseHelper<Complex>.IsComplexNumber(new Complex(-0.0, -1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsComplexNumber(new Complex(-0.0, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsComplexNumber(new Complex(-0.0, double.NaN)));
            Assert.False(NumberBaseHelper<Complex>.IsComplexNumber(new Complex(-0.0, 0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsComplexNumber(new Complex(-0.0, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsComplexNumber(new Complex(-0.0, double.PositiveInfinity)));

            Assert.True(NumberBaseHelper<Complex>.IsComplexNumber(new Complex(double.NaN, double.NegativeInfinity)));
            Assert.True(NumberBaseHelper<Complex>.IsComplexNumber(new Complex(double.NaN, -1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsComplexNumber(new Complex(double.NaN, -0.0)));
            Assert.True(NumberBaseHelper<Complex>.IsComplexNumber(new Complex(double.NaN, double.NaN)));
            Assert.False(NumberBaseHelper<Complex>.IsComplexNumber(new Complex(double.NaN, 0.0)));
            Assert.True(NumberBaseHelper<Complex>.IsComplexNumber(new Complex(double.NaN, 1.0)));
            Assert.True(NumberBaseHelper<Complex>.IsComplexNumber(new Complex(double.NaN, double.PositiveInfinity)));

            Assert.False(NumberBaseHelper<Complex>.IsComplexNumber(new Complex(0.0, double.NegativeInfinity)));
            Assert.False(NumberBaseHelper<Complex>.IsComplexNumber(new Complex(0.0, -1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsComplexNumber(new Complex(0.0, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsComplexNumber(new Complex(0.0, double.NaN)));
            Assert.False(NumberBaseHelper<Complex>.IsComplexNumber(new Complex(0.0, 0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsComplexNumber(new Complex(0.0, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsComplexNumber(new Complex(0.0, double.PositiveInfinity)));

            Assert.True(NumberBaseHelper<Complex>.IsComplexNumber(new Complex(1.0, double.NegativeInfinity)));
            Assert.True(NumberBaseHelper<Complex>.IsComplexNumber(new Complex(1.0, -1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsComplexNumber(new Complex(1.0, -0.0)));
            Assert.True(NumberBaseHelper<Complex>.IsComplexNumber(new Complex(1.0, double.NaN)));
            Assert.False(NumberBaseHelper<Complex>.IsComplexNumber(new Complex(1.0, 0.0)));
            Assert.True(NumberBaseHelper<Complex>.IsComplexNumber(new Complex(1.0, 1.0)));
            Assert.True(NumberBaseHelper<Complex>.IsComplexNumber(new Complex(1.0, double.PositiveInfinity)));
        }

        [Fact]
        public static void IsEvenIntegerTest()
        {
            Assert.False(NumberBaseHelper<Complex>.IsEvenInteger(double.NegativeInfinity));
            Assert.True(NumberBaseHelper<Complex>.IsEvenInteger(double.MinValue));
            Assert.False(NumberBaseHelper<Complex>.IsEvenInteger(-1.0));
            Assert.False(NumberBaseHelper<Complex>.IsEvenInteger(-MinNormal));
            Assert.False(NumberBaseHelper<Complex>.IsEvenInteger(-MaxSubnormal));
            Assert.False(NumberBaseHelper<Complex>.IsEvenInteger(-double.Epsilon));
            Assert.True(NumberBaseHelper<Complex>.IsEvenInteger(-0.0));
            Assert.False(NumberBaseHelper<Complex>.IsEvenInteger(double.NaN));
            Assert.True(NumberBaseHelper<Complex>.IsEvenInteger(0.0));
            Assert.False(NumberBaseHelper<Complex>.IsEvenInteger(double.Epsilon));
            Assert.False(NumberBaseHelper<Complex>.IsEvenInteger(MaxSubnormal));
            Assert.False(NumberBaseHelper<Complex>.IsEvenInteger(MinNormal));
            Assert.False(NumberBaseHelper<Complex>.IsEvenInteger(1.0));
            Assert.True(NumberBaseHelper<Complex>.IsEvenInteger(double.MaxValue));
            Assert.False(NumberBaseHelper<Complex>.IsEvenInteger(double.PositiveInfinity));

            Assert.False(NumberBaseHelper<Complex>.IsEvenInteger(new Complex(-1.0, double.NegativeInfinity)));
            Assert.False(NumberBaseHelper<Complex>.IsEvenInteger(new Complex(-1.0, -1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsEvenInteger(new Complex(-1.0, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsEvenInteger(new Complex(-1.0, double.NaN)));
            Assert.False(NumberBaseHelper<Complex>.IsEvenInteger(new Complex(-1.0, 0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsEvenInteger(new Complex(-1.0, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsEvenInteger(new Complex(-1.0, double.PositiveInfinity)));

            Assert.False(NumberBaseHelper<Complex>.IsEvenInteger(new Complex(-0.0, double.NegativeInfinity)));
            Assert.False(NumberBaseHelper<Complex>.IsEvenInteger(new Complex(-0.0, -1.0)));
            Assert.True(NumberBaseHelper<Complex>.IsEvenInteger(new Complex(-0.0, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsEvenInteger(new Complex(-0.0, double.NaN)));
            Assert.True(NumberBaseHelper<Complex>.IsEvenInteger(new Complex(-0.0, 0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsEvenInteger(new Complex(-0.0, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsEvenInteger(new Complex(-0.0, double.PositiveInfinity)));

            Assert.False(NumberBaseHelper<Complex>.IsEvenInteger(new Complex(double.NaN, double.NegativeInfinity)));
            Assert.False(NumberBaseHelper<Complex>.IsEvenInteger(new Complex(double.NaN, -1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsEvenInteger(new Complex(double.NaN, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsEvenInteger(new Complex(double.NaN, double.NaN)));
            Assert.False(NumberBaseHelper<Complex>.IsEvenInteger(new Complex(double.NaN, 0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsEvenInteger(new Complex(double.NaN, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsEvenInteger(new Complex(double.NaN, double.PositiveInfinity)));

            Assert.False(NumberBaseHelper<Complex>.IsEvenInteger(new Complex(0.0, double.NegativeInfinity)));
            Assert.False(NumberBaseHelper<Complex>.IsEvenInteger(new Complex(0.0, -1.0)));
            Assert.True(NumberBaseHelper<Complex>.IsEvenInteger(new Complex(0.0, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsEvenInteger(new Complex(0.0, double.NaN)));
            Assert.True(NumberBaseHelper<Complex>.IsEvenInteger(new Complex(0.0, 0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsEvenInteger(new Complex(0.0, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsEvenInteger(new Complex(0.0, double.PositiveInfinity)));

            Assert.False(NumberBaseHelper<Complex>.IsEvenInteger(new Complex(1.0, double.NegativeInfinity)));
            Assert.False(NumberBaseHelper<Complex>.IsEvenInteger(new Complex(1.0, -1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsEvenInteger(new Complex(1.0, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsEvenInteger(new Complex(1.0, double.NaN)));
            Assert.False(NumberBaseHelper<Complex>.IsEvenInteger(new Complex(1.0, 0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsEvenInteger(new Complex(1.0, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsEvenInteger(new Complex(1.0, double.PositiveInfinity)));
        }

        [Fact]
        public static void IsFiniteTest()
        {
            Assert.False(NumberBaseHelper<Complex>.IsFinite(double.NegativeInfinity));
            Assert.True(NumberBaseHelper<Complex>.IsFinite(double.MinValue));
            Assert.True(NumberBaseHelper<Complex>.IsFinite(-1.0));
            Assert.True(NumberBaseHelper<Complex>.IsFinite(-MinNormal));
            Assert.True(NumberBaseHelper<Complex>.IsFinite(-MaxSubnormal));
            Assert.True(NumberBaseHelper<Complex>.IsFinite(-double.Epsilon));
            Assert.True(NumberBaseHelper<Complex>.IsFinite(-0.0));
            Assert.False(NumberBaseHelper<Complex>.IsFinite(double.NaN));
            Assert.True(NumberBaseHelper<Complex>.IsFinite(0.0));
            Assert.True(NumberBaseHelper<Complex>.IsFinite(double.Epsilon));
            Assert.True(NumberBaseHelper<Complex>.IsFinite(MaxSubnormal));
            Assert.True(NumberBaseHelper<Complex>.IsFinite(MinNormal));
            Assert.True(NumberBaseHelper<Complex>.IsFinite(1.0));
            Assert.True(NumberBaseHelper<Complex>.IsFinite(double.MaxValue));
            Assert.False(NumberBaseHelper<Complex>.IsFinite(double.PositiveInfinity));

            Assert.False(NumberBaseHelper<Complex>.IsFinite(new Complex(-1.0, double.NegativeInfinity)));
            Assert.True(NumberBaseHelper<Complex>.IsFinite(new Complex(-1.0, -1.0)));
            Assert.True(NumberBaseHelper<Complex>.IsFinite(new Complex(-1.0, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsFinite(new Complex(-1.0, double.NaN)));
            Assert.True(NumberBaseHelper<Complex>.IsFinite(new Complex(-1.0, 0.0)));
            Assert.True(NumberBaseHelper<Complex>.IsFinite(new Complex(-1.0, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsFinite(new Complex(-1.0, double.PositiveInfinity)));

            Assert.False(NumberBaseHelper<Complex>.IsFinite(new Complex(-0.0, double.NegativeInfinity)));
            Assert.True(NumberBaseHelper<Complex>.IsFinite(new Complex(-0.0, -1.0)));
            Assert.True(NumberBaseHelper<Complex>.IsFinite(new Complex(-0.0, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsFinite(new Complex(-0.0, double.NaN)));
            Assert.True(NumberBaseHelper<Complex>.IsFinite(new Complex(-0.0, 0.0)));
            Assert.True(NumberBaseHelper<Complex>.IsFinite(new Complex(-0.0, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsFinite(new Complex(-0.0, double.PositiveInfinity)));

            Assert.False(NumberBaseHelper<Complex>.IsFinite(new Complex(double.NaN, double.NegativeInfinity)));
            Assert.False(NumberBaseHelper<Complex>.IsFinite(new Complex(double.NaN, -1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsFinite(new Complex(double.NaN, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsFinite(new Complex(double.NaN, double.NaN)));
            Assert.False(NumberBaseHelper<Complex>.IsFinite(new Complex(double.NaN, 0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsFinite(new Complex(double.NaN, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsFinite(new Complex(double.NaN, double.PositiveInfinity)));

            Assert.False(NumberBaseHelper<Complex>.IsFinite(new Complex(0.0, double.NegativeInfinity)));
            Assert.True(NumberBaseHelper<Complex>.IsFinite(new Complex(0.0, -1.0)));
            Assert.True(NumberBaseHelper<Complex>.IsFinite(new Complex(0.0, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsFinite(new Complex(0.0, double.NaN)));
            Assert.True(NumberBaseHelper<Complex>.IsFinite(new Complex(0.0, 0.0)));
            Assert.True(NumberBaseHelper<Complex>.IsFinite(new Complex(0.0, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsFinite(new Complex(0.0, double.PositiveInfinity)));

            Assert.False(NumberBaseHelper<Complex>.IsFinite(new Complex(1.0, double.NegativeInfinity)));
            Assert.True(NumberBaseHelper<Complex>.IsFinite(new Complex(1.0, -1.0)));
            Assert.True(NumberBaseHelper<Complex>.IsFinite(new Complex(1.0, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsFinite(new Complex(1.0, double.NaN)));
            Assert.True(NumberBaseHelper<Complex>.IsFinite(new Complex(1.0, 0.0)));
            Assert.True(NumberBaseHelper<Complex>.IsFinite(new Complex(1.0, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsFinite(new Complex(1.0, double.PositiveInfinity)));
        }

        [Fact]
        public static void IsImaginaryNumberTest()
        {
            Assert.False(NumberBaseHelper<Complex>.IsImaginaryNumber(double.NegativeInfinity));
            Assert.False(NumberBaseHelper<Complex>.IsImaginaryNumber(double.MinValue));
            Assert.False(NumberBaseHelper<Complex>.IsImaginaryNumber(-1.0));
            Assert.False(NumberBaseHelper<Complex>.IsImaginaryNumber(-MinNormal));
            Assert.False(NumberBaseHelper<Complex>.IsImaginaryNumber(-MaxSubnormal));
            Assert.False(NumberBaseHelper<Complex>.IsImaginaryNumber(-double.Epsilon));
            Assert.True(NumberBaseHelper<Complex>.IsImaginaryNumber(-0.0));
            Assert.False(NumberBaseHelper<Complex>.IsImaginaryNumber(double.NaN));
            Assert.True(NumberBaseHelper<Complex>.IsImaginaryNumber(0.0));
            Assert.False(NumberBaseHelper<Complex>.IsImaginaryNumber(double.Epsilon));
            Assert.False(NumberBaseHelper<Complex>.IsImaginaryNumber(MaxSubnormal));
            Assert.False(NumberBaseHelper<Complex>.IsImaginaryNumber(MinNormal));
            Assert.False(NumberBaseHelper<Complex>.IsImaginaryNumber(1.0));
            Assert.False(NumberBaseHelper<Complex>.IsImaginaryNumber(double.MaxValue));
            Assert.False(NumberBaseHelper<Complex>.IsImaginaryNumber(double.PositiveInfinity));

            Assert.False(NumberBaseHelper<Complex>.IsImaginaryNumber(new Complex(-1.0, double.NegativeInfinity)));
            Assert.False(NumberBaseHelper<Complex>.IsImaginaryNumber(new Complex(-1.0, -1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsImaginaryNumber(new Complex(-1.0, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsImaginaryNumber(new Complex(-1.0, double.NaN)));
            Assert.False(NumberBaseHelper<Complex>.IsImaginaryNumber(new Complex(-1.0, 0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsImaginaryNumber(new Complex(-1.0, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsImaginaryNumber(new Complex(-1.0, double.PositiveInfinity)));

            Assert.True(NumberBaseHelper<Complex>.IsImaginaryNumber(new Complex(-0.0, double.NegativeInfinity)));
            Assert.True(NumberBaseHelper<Complex>.IsImaginaryNumber(new Complex(-0.0, -1.0)));
            Assert.True(NumberBaseHelper<Complex>.IsImaginaryNumber(new Complex(-0.0, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsImaginaryNumber(new Complex(-0.0, double.NaN)));
            Assert.True(NumberBaseHelper<Complex>.IsImaginaryNumber(new Complex(-0.0, 0.0)));
            Assert.True(NumberBaseHelper<Complex>.IsImaginaryNumber(new Complex(-0.0, 1.0)));
            Assert.True(NumberBaseHelper<Complex>.IsImaginaryNumber(new Complex(-0.0, double.PositiveInfinity)));

            Assert.False(NumberBaseHelper<Complex>.IsImaginaryNumber(new Complex(double.NaN, double.NegativeInfinity)));
            Assert.False(NumberBaseHelper<Complex>.IsImaginaryNumber(new Complex(double.NaN, -1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsImaginaryNumber(new Complex(double.NaN, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsImaginaryNumber(new Complex(double.NaN, double.NaN)));
            Assert.False(NumberBaseHelper<Complex>.IsImaginaryNumber(new Complex(double.NaN, 0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsImaginaryNumber(new Complex(double.NaN, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsImaginaryNumber(new Complex(double.NaN, double.PositiveInfinity)));

            Assert.True(NumberBaseHelper<Complex>.IsImaginaryNumber(new Complex(0.0, double.NegativeInfinity)));
            Assert.True(NumberBaseHelper<Complex>.IsImaginaryNumber(new Complex(0.0, -1.0)));
            Assert.True(NumberBaseHelper<Complex>.IsImaginaryNumber(new Complex(0.0, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsImaginaryNumber(new Complex(0.0, double.NaN)));
            Assert.True(NumberBaseHelper<Complex>.IsImaginaryNumber(new Complex(0.0, 0.0)));
            Assert.True(NumberBaseHelper<Complex>.IsImaginaryNumber(new Complex(0.0, 1.0)));
            Assert.True(NumberBaseHelper<Complex>.IsImaginaryNumber(new Complex(0.0, double.PositiveInfinity)));

            Assert.False(NumberBaseHelper<Complex>.IsImaginaryNumber(new Complex(1.0, double.NegativeInfinity)));
            Assert.False(NumberBaseHelper<Complex>.IsImaginaryNumber(new Complex(1.0, -1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsImaginaryNumber(new Complex(1.0, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsImaginaryNumber(new Complex(1.0, double.NaN)));
            Assert.False(NumberBaseHelper<Complex>.IsImaginaryNumber(new Complex(1.0, 0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsImaginaryNumber(new Complex(1.0, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsImaginaryNumber(new Complex(1.0, double.PositiveInfinity)));
        }

        [Fact]
        public static void IsInfinityTest()
        {
            Assert.True(NumberBaseHelper<Complex>.IsInfinity(double.NegativeInfinity));
            Assert.False(NumberBaseHelper<Complex>.IsInfinity(double.MinValue));
            Assert.False(NumberBaseHelper<Complex>.IsInfinity(-1.0));
            Assert.False(NumberBaseHelper<Complex>.IsInfinity(-MinNormal));
            Assert.False(NumberBaseHelper<Complex>.IsInfinity(-MaxSubnormal));
            Assert.False(NumberBaseHelper<Complex>.IsInfinity(-double.Epsilon));
            Assert.False(NumberBaseHelper<Complex>.IsInfinity(-0.0));
            Assert.False(NumberBaseHelper<Complex>.IsInfinity(double.NaN));
            Assert.False(NumberBaseHelper<Complex>.IsInfinity(0.0));
            Assert.False(NumberBaseHelper<Complex>.IsInfinity(double.Epsilon));
            Assert.False(NumberBaseHelper<Complex>.IsInfinity(MaxSubnormal));
            Assert.False(NumberBaseHelper<Complex>.IsInfinity(MinNormal));
            Assert.False(NumberBaseHelper<Complex>.IsInfinity(1.0));
            Assert.False(NumberBaseHelper<Complex>.IsInfinity(double.MaxValue));
            Assert.True(NumberBaseHelper<Complex>.IsInfinity(double.PositiveInfinity));

            Assert.True(NumberBaseHelper<Complex>.IsInfinity(new Complex(-1.0, double.NegativeInfinity)));
            Assert.False(NumberBaseHelper<Complex>.IsInfinity(new Complex(-1.0, -1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsInfinity(new Complex(-1.0, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsInfinity(new Complex(-1.0, double.NaN)));
            Assert.False(NumberBaseHelper<Complex>.IsInfinity(new Complex(-1.0, 0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsInfinity(new Complex(-1.0, 1.0)));
            Assert.True(NumberBaseHelper<Complex>.IsInfinity(new Complex(-1.0, double.PositiveInfinity)));

            Assert.True(NumberBaseHelper<Complex>.IsInfinity(new Complex(-0.0, double.NegativeInfinity)));
            Assert.False(NumberBaseHelper<Complex>.IsInfinity(new Complex(-0.0, -1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsInfinity(new Complex(-0.0, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsInfinity(new Complex(-0.0, double.NaN)));
            Assert.False(NumberBaseHelper<Complex>.IsInfinity(new Complex(-0.0, 0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsInfinity(new Complex(-0.0, 1.0)));
            Assert.True(NumberBaseHelper<Complex>.IsInfinity(new Complex(-0.0, double.PositiveInfinity)));

            Assert.True(NumberBaseHelper<Complex>.IsInfinity(new Complex(double.NaN, double.NegativeInfinity)));
            Assert.False(NumberBaseHelper<Complex>.IsInfinity(new Complex(double.NaN, -1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsInfinity(new Complex(double.NaN, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsInfinity(new Complex(double.NaN, double.NaN)));
            Assert.False(NumberBaseHelper<Complex>.IsInfinity(new Complex(double.NaN, 0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsInfinity(new Complex(double.NaN, 1.0)));
            Assert.True(NumberBaseHelper<Complex>.IsInfinity(new Complex(double.NaN, double.PositiveInfinity)));

            Assert.True(NumberBaseHelper<Complex>.IsInfinity(new Complex(0.0, double.NegativeInfinity)));
            Assert.False(NumberBaseHelper<Complex>.IsInfinity(new Complex(0.0, -1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsInfinity(new Complex(0.0, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsInfinity(new Complex(0.0, double.NaN)));
            Assert.False(NumberBaseHelper<Complex>.IsInfinity(new Complex(0.0, 0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsInfinity(new Complex(0.0, 1.0)));
            Assert.True(NumberBaseHelper<Complex>.IsInfinity(new Complex(0.0, double.PositiveInfinity)));

            Assert.True(NumberBaseHelper<Complex>.IsInfinity(new Complex(1.0, double.NegativeInfinity)));
            Assert.False(NumberBaseHelper<Complex>.IsInfinity(new Complex(1.0, -1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsInfinity(new Complex(1.0, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsInfinity(new Complex(1.0, double.NaN)));
            Assert.False(NumberBaseHelper<Complex>.IsInfinity(new Complex(1.0, 0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsInfinity(new Complex(1.0, 1.0)));
            Assert.True(NumberBaseHelper<Complex>.IsInfinity(new Complex(1.0, double.PositiveInfinity)));
        }

        [Fact]
        public static void IsIntegerTest()
        {
            Assert.False(NumberBaseHelper<Complex>.IsInteger(double.NegativeInfinity));
            Assert.True(NumberBaseHelper<Complex>.IsInteger(double.MinValue));
            Assert.True(NumberBaseHelper<Complex>.IsInteger(-1.0));
            Assert.False(NumberBaseHelper<Complex>.IsInteger(-MinNormal));
            Assert.False(NumberBaseHelper<Complex>.IsInteger(-MaxSubnormal));
            Assert.False(NumberBaseHelper<Complex>.IsInteger(-double.Epsilon));
            Assert.True(NumberBaseHelper<Complex>.IsInteger(-0.0));
            Assert.False(NumberBaseHelper<Complex>.IsInteger(double.NaN));
            Assert.True(NumberBaseHelper<Complex>.IsInteger(0.0));
            Assert.False(NumberBaseHelper<Complex>.IsInteger(double.Epsilon));
            Assert.False(NumberBaseHelper<Complex>.IsInteger(MaxSubnormal));
            Assert.False(NumberBaseHelper<Complex>.IsInteger(MinNormal));
            Assert.True(NumberBaseHelper<Complex>.IsInteger(1.0));
            Assert.True(NumberBaseHelper<Complex>.IsInteger(double.MaxValue));
            Assert.False(NumberBaseHelper<Complex>.IsInteger(double.PositiveInfinity));

            Assert.False(NumberBaseHelper<Complex>.IsInteger(new Complex(-1.0, double.NegativeInfinity)));
            Assert.False(NumberBaseHelper<Complex>.IsInteger(new Complex(-1.0, -1.0)));
            Assert.True(NumberBaseHelper<Complex>.IsInteger(new Complex(-1.0, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsInteger(new Complex(-1.0, double.NaN)));
            Assert.True(NumberBaseHelper<Complex>.IsInteger(new Complex(-1.0, 0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsInteger(new Complex(-1.0, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsInteger(new Complex(-1.0, double.PositiveInfinity)));

            Assert.False(NumberBaseHelper<Complex>.IsInteger(new Complex(-0.0, double.NegativeInfinity)));
            Assert.False(NumberBaseHelper<Complex>.IsInteger(new Complex(-0.0, -1.0)));
            Assert.True(NumberBaseHelper<Complex>.IsInteger(new Complex(-0.0, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsInteger(new Complex(-0.0, double.NaN)));
            Assert.True(NumberBaseHelper<Complex>.IsInteger(new Complex(-0.0, 0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsInteger(new Complex(-0.0, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsInteger(new Complex(-0.0, double.PositiveInfinity)));

            Assert.False(NumberBaseHelper<Complex>.IsInteger(new Complex(double.NaN, double.NegativeInfinity)));
            Assert.False(NumberBaseHelper<Complex>.IsInteger(new Complex(double.NaN, -1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsInteger(new Complex(double.NaN, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsInteger(new Complex(double.NaN, double.NaN)));
            Assert.False(NumberBaseHelper<Complex>.IsInteger(new Complex(double.NaN, 0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsInteger(new Complex(double.NaN, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsInteger(new Complex(double.NaN, double.PositiveInfinity)));

            Assert.False(NumberBaseHelper<Complex>.IsInteger(new Complex(0.0, double.NegativeInfinity)));
            Assert.False(NumberBaseHelper<Complex>.IsInteger(new Complex(0.0, -1.0)));
            Assert.True(NumberBaseHelper<Complex>.IsInteger(new Complex(0.0, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsInteger(new Complex(0.0, double.NaN)));
            Assert.True(NumberBaseHelper<Complex>.IsInteger(new Complex(0.0, 0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsInteger(new Complex(0.0, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsInteger(new Complex(0.0, double.PositiveInfinity)));

            Assert.False(NumberBaseHelper<Complex>.IsInteger(new Complex(1.0, double.NegativeInfinity)));
            Assert.False(NumberBaseHelper<Complex>.IsInteger(new Complex(1.0, -1.0)));
            Assert.True(NumberBaseHelper<Complex>.IsInteger(new Complex(1.0, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsInteger(new Complex(1.0, double.NaN)));
            Assert.True(NumberBaseHelper<Complex>.IsInteger(new Complex(1.0, 0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsInteger(new Complex(1.0, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsInteger(new Complex(1.0, double.PositiveInfinity)));
        }

        [Fact]
        public static void IsNaNTest()
        {
            Assert.False(NumberBaseHelper<Complex>.IsNaN(double.NegativeInfinity));
            Assert.False(NumberBaseHelper<Complex>.IsNaN(double.MinValue));
            Assert.False(NumberBaseHelper<Complex>.IsNaN(-1.0));
            Assert.False(NumberBaseHelper<Complex>.IsNaN(-MinNormal));
            Assert.False(NumberBaseHelper<Complex>.IsNaN(-MaxSubnormal));
            Assert.False(NumberBaseHelper<Complex>.IsNaN(-double.Epsilon));
            Assert.False(NumberBaseHelper<Complex>.IsNaN(-0.0));
            Assert.True(NumberBaseHelper<Complex>.IsNaN(double.NaN));
            Assert.False(NumberBaseHelper<Complex>.IsNaN(0.0));
            Assert.False(NumberBaseHelper<Complex>.IsNaN(double.Epsilon));
            Assert.False(NumberBaseHelper<Complex>.IsNaN(MaxSubnormal));
            Assert.False(NumberBaseHelper<Complex>.IsNaN(MinNormal));
            Assert.False(NumberBaseHelper<Complex>.IsNaN(1.0));
            Assert.False(NumberBaseHelper<Complex>.IsNaN(double.MaxValue));
            Assert.False(NumberBaseHelper<Complex>.IsNaN(double.PositiveInfinity));

            Assert.False(NumberBaseHelper<Complex>.IsNaN(new Complex(-1.0, double.NegativeInfinity)));
            Assert.False(NumberBaseHelper<Complex>.IsNaN(new Complex(-1.0, -1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsNaN(new Complex(-1.0, -0.0)));
            Assert.True(NumberBaseHelper<Complex>.IsNaN(new Complex(-1.0, double.NaN)));
            Assert.False(NumberBaseHelper<Complex>.IsNaN(new Complex(-1.0, 0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsNaN(new Complex(-1.0, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsNaN(new Complex(-1.0, double.PositiveInfinity)));

            Assert.False(NumberBaseHelper<Complex>.IsNaN(new Complex(-0.0, double.NegativeInfinity)));
            Assert.False(NumberBaseHelper<Complex>.IsNaN(new Complex(-0.0, -1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsNaN(new Complex(-0.0, -0.0)));
            Assert.True(NumberBaseHelper<Complex>.IsNaN(new Complex(-0.0, double.NaN)));
            Assert.False(NumberBaseHelper<Complex>.IsNaN(new Complex(-0.0, 0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsNaN(new Complex(-0.0, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsNaN(new Complex(-0.0, double.PositiveInfinity)));

            Assert.False(NumberBaseHelper<Complex>.IsNaN(new Complex(double.NaN, double.NegativeInfinity)));
            Assert.True(NumberBaseHelper<Complex>.IsNaN(new Complex(double.NaN, -1.0)));
            Assert.True(NumberBaseHelper<Complex>.IsNaN(new Complex(double.NaN, -0.0)));
            Assert.True(NumberBaseHelper<Complex>.IsNaN(new Complex(double.NaN, double.NaN)));
            Assert.True(NumberBaseHelper<Complex>.IsNaN(new Complex(double.NaN, 0.0)));
            Assert.True(NumberBaseHelper<Complex>.IsNaN(new Complex(double.NaN, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsNaN(new Complex(double.NaN, double.PositiveInfinity)));

            Assert.False(NumberBaseHelper<Complex>.IsNaN(new Complex(0.0, double.NegativeInfinity)));
            Assert.False(NumberBaseHelper<Complex>.IsNaN(new Complex(0.0, -1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsNaN(new Complex(0.0, -0.0)));
            Assert.True(NumberBaseHelper<Complex>.IsNaN(new Complex(0.0, double.NaN)));
            Assert.False(NumberBaseHelper<Complex>.IsNaN(new Complex(0.0, 0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsNaN(new Complex(0.0, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsNaN(new Complex(0.0, double.PositiveInfinity)));

            Assert.False(NumberBaseHelper<Complex>.IsNaN(new Complex(1.0, double.NegativeInfinity)));
            Assert.False(NumberBaseHelper<Complex>.IsNaN(new Complex(1.0, -1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsNaN(new Complex(1.0, -0.0)));
            Assert.True(NumberBaseHelper<Complex>.IsNaN(new Complex(1.0, double.NaN)));
            Assert.False(NumberBaseHelper<Complex>.IsNaN(new Complex(1.0, 0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsNaN(new Complex(1.0, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsNaN(new Complex(1.0, double.PositiveInfinity)));
        }

        [Fact]
        public static void IsNegativeTest()
        {
            Assert.True(NumberBaseHelper<Complex>.IsNegative(double.NegativeInfinity));
            Assert.True(NumberBaseHelper<Complex>.IsNegative(double.MinValue));
            Assert.True(NumberBaseHelper<Complex>.IsNegative(-1.0));
            Assert.True(NumberBaseHelper<Complex>.IsNegative(-MinNormal));
            Assert.True(NumberBaseHelper<Complex>.IsNegative(-MaxSubnormal));
            Assert.True(NumberBaseHelper<Complex>.IsNegative(-double.Epsilon));
            Assert.True(NumberBaseHelper<Complex>.IsNegative(-0.0));
            Assert.True(NumberBaseHelper<Complex>.IsNegative(double.NaN));
            Assert.False(NumberBaseHelper<Complex>.IsNegative(0.0));
            Assert.False(NumberBaseHelper<Complex>.IsNegative(double.Epsilon));
            Assert.False(NumberBaseHelper<Complex>.IsNegative(MaxSubnormal));
            Assert.False(NumberBaseHelper<Complex>.IsNegative(MinNormal));
            Assert.False(NumberBaseHelper<Complex>.IsNegative(1.0));
            Assert.False(NumberBaseHelper<Complex>.IsNegative(double.MaxValue));
            Assert.False(NumberBaseHelper<Complex>.IsNegative(double.PositiveInfinity));

            Assert.False(NumberBaseHelper<Complex>.IsNegative(new Complex(-1.0, double.NegativeInfinity)));
            Assert.False(NumberBaseHelper<Complex>.IsNegative(new Complex(-1.0, -1.0)));
            Assert.True(NumberBaseHelper<Complex>.IsNegative(new Complex(-1.0, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsNegative(new Complex(-1.0, double.NaN)));
            Assert.True(NumberBaseHelper<Complex>.IsNegative(new Complex(-1.0, 0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsNegative(new Complex(-1.0, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsNegative(new Complex(-1.0, double.PositiveInfinity)));

            Assert.False(NumberBaseHelper<Complex>.IsNegative(new Complex(-0.0, double.NegativeInfinity)));
            Assert.False(NumberBaseHelper<Complex>.IsNegative(new Complex(-0.0, -1.0)));
            Assert.True(NumberBaseHelper<Complex>.IsNegative(new Complex(-0.0, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsNegative(new Complex(-0.0, double.NaN)));
            Assert.True(NumberBaseHelper<Complex>.IsNegative(new Complex(-0.0, 0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsNegative(new Complex(-0.0, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsNegative(new Complex(-0.0, double.PositiveInfinity)));

            Assert.False(NumberBaseHelper<Complex>.IsNegative(new Complex(double.NaN, double.NegativeInfinity)));
            Assert.False(NumberBaseHelper<Complex>.IsNegative(new Complex(double.NaN, -1.0)));
            Assert.True(NumberBaseHelper<Complex>.IsNegative(new Complex(double.NaN, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsNegative(new Complex(double.NaN, double.NaN)));
            Assert.True(NumberBaseHelper<Complex>.IsNegative(new Complex(double.NaN, 0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsNegative(new Complex(double.NaN, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsNegative(new Complex(double.NaN, double.PositiveInfinity)));

            Assert.False(NumberBaseHelper<Complex>.IsNegative(new Complex(0.0, double.NegativeInfinity)));
            Assert.False(NumberBaseHelper<Complex>.IsNegative(new Complex(0.0, -1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsNegative(new Complex(0.0, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsNegative(new Complex(0.0, double.NaN)));
            Assert.False(NumberBaseHelper<Complex>.IsNegative(new Complex(0.0, 0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsNegative(new Complex(0.0, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsNegative(new Complex(0.0, double.PositiveInfinity)));

            Assert.False(NumberBaseHelper<Complex>.IsNegative(new Complex(1.0, double.NegativeInfinity)));
            Assert.False(NumberBaseHelper<Complex>.IsNegative(new Complex(1.0, -1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsNegative(new Complex(1.0, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsNegative(new Complex(1.0, double.NaN)));
            Assert.False(NumberBaseHelper<Complex>.IsNegative(new Complex(1.0, 0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsNegative(new Complex(1.0, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsNegative(new Complex(1.0, double.PositiveInfinity)));
        }

        [Fact]
        public static void IsNegativeInfinityTest()
        {
            Assert.True(NumberBaseHelper<Complex>.IsNegativeInfinity(double.NegativeInfinity));
            Assert.False(NumberBaseHelper<Complex>.IsNegativeInfinity(double.MinValue));
            Assert.False(NumberBaseHelper<Complex>.IsNegativeInfinity(-1.0));
            Assert.False(NumberBaseHelper<Complex>.IsNegativeInfinity(-MinNormal));
            Assert.False(NumberBaseHelper<Complex>.IsNegativeInfinity(-MaxSubnormal));
            Assert.False(NumberBaseHelper<Complex>.IsNegativeInfinity(-double.Epsilon));
            Assert.False(NumberBaseHelper<Complex>.IsNegativeInfinity(-0.0));
            Assert.False(NumberBaseHelper<Complex>.IsNegativeInfinity(double.NaN));
            Assert.False(NumberBaseHelper<Complex>.IsNegativeInfinity(0.0));
            Assert.False(NumberBaseHelper<Complex>.IsNegativeInfinity(double.Epsilon));
            Assert.False(NumberBaseHelper<Complex>.IsNegativeInfinity(MaxSubnormal));
            Assert.False(NumberBaseHelper<Complex>.IsNegativeInfinity(MinNormal));
            Assert.False(NumberBaseHelper<Complex>.IsNegativeInfinity(1.0));
            Assert.False(NumberBaseHelper<Complex>.IsNegativeInfinity(double.MaxValue));
            Assert.False(NumberBaseHelper<Complex>.IsNegativeInfinity(double.PositiveInfinity));

            Assert.False(NumberBaseHelper<Complex>.IsNegativeInfinity(new Complex(-1.0, double.NegativeInfinity)));
            Assert.False(NumberBaseHelper<Complex>.IsNegativeInfinity(new Complex(-1.0, -1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsNegativeInfinity(new Complex(-1.0, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsNegativeInfinity(new Complex(-1.0, double.NaN)));
            Assert.False(NumberBaseHelper<Complex>.IsNegativeInfinity(new Complex(-1.0, 0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsNegativeInfinity(new Complex(-1.0, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsNegativeInfinity(new Complex(-1.0, double.PositiveInfinity)));

            Assert.False(NumberBaseHelper<Complex>.IsNegativeInfinity(new Complex(-0.0, double.NegativeInfinity)));
            Assert.False(NumberBaseHelper<Complex>.IsNegativeInfinity(new Complex(-0.0, -1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsNegativeInfinity(new Complex(-0.0, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsNegativeInfinity(new Complex(-0.0, double.NaN)));
            Assert.False(NumberBaseHelper<Complex>.IsNegativeInfinity(new Complex(-0.0, 0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsNegativeInfinity(new Complex(-0.0, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsNegativeInfinity(new Complex(-0.0, double.PositiveInfinity)));

            Assert.False(NumberBaseHelper<Complex>.IsNegativeInfinity(new Complex(double.NaN, double.NegativeInfinity)));
            Assert.False(NumberBaseHelper<Complex>.IsNegativeInfinity(new Complex(double.NaN, -1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsNegativeInfinity(new Complex(double.NaN, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsNegativeInfinity(new Complex(double.NaN, double.NaN)));
            Assert.False(NumberBaseHelper<Complex>.IsNegativeInfinity(new Complex(double.NaN, 0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsNegativeInfinity(new Complex(double.NaN, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsNegativeInfinity(new Complex(double.NaN, double.PositiveInfinity)));

            Assert.False(NumberBaseHelper<Complex>.IsNegativeInfinity(new Complex(0.0, double.NegativeInfinity)));
            Assert.False(NumberBaseHelper<Complex>.IsNegativeInfinity(new Complex(0.0, -1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsNegativeInfinity(new Complex(0.0, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsNegativeInfinity(new Complex(0.0, double.NaN)));
            Assert.False(NumberBaseHelper<Complex>.IsNegativeInfinity(new Complex(0.0, 0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsNegativeInfinity(new Complex(0.0, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsNegativeInfinity(new Complex(0.0, double.PositiveInfinity)));

            Assert.False(NumberBaseHelper<Complex>.IsNegativeInfinity(new Complex(1.0, double.NegativeInfinity)));
            Assert.False(NumberBaseHelper<Complex>.IsNegativeInfinity(new Complex(1.0, -1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsNegativeInfinity(new Complex(1.0, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsNegativeInfinity(new Complex(1.0, double.NaN)));
            Assert.False(NumberBaseHelper<Complex>.IsNegativeInfinity(new Complex(1.0, 0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsNegativeInfinity(new Complex(1.0, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsNegativeInfinity(new Complex(1.0, double.PositiveInfinity)));
        }

        [Fact]
        public static void IsNormalTest()
        {
            Assert.False(NumberBaseHelper<Complex>.IsNormal(double.NegativeInfinity));
            Assert.True(NumberBaseHelper<Complex>.IsNormal(double.MinValue));
            Assert.True(NumberBaseHelper<Complex>.IsNormal(-1.0));
            Assert.True(NumberBaseHelper<Complex>.IsNormal(-MinNormal));
            Assert.False(NumberBaseHelper<Complex>.IsNormal(-MaxSubnormal));
            Assert.False(NumberBaseHelper<Complex>.IsNormal(-double.Epsilon));
            Assert.False(NumberBaseHelper<Complex>.IsNormal(-0.0));
            Assert.False(NumberBaseHelper<Complex>.IsNormal(double.NaN));
            Assert.False(NumberBaseHelper<Complex>.IsNormal(0.0));
            Assert.False(NumberBaseHelper<Complex>.IsNormal(double.Epsilon));
            Assert.False(NumberBaseHelper<Complex>.IsNormal(MaxSubnormal));
            Assert.True(NumberBaseHelper<Complex>.IsNormal(MinNormal));
            Assert.True(NumberBaseHelper<Complex>.IsNormal(1.0));
            Assert.True(NumberBaseHelper<Complex>.IsNormal(double.MaxValue));
            Assert.False(NumberBaseHelper<Complex>.IsNormal(double.PositiveInfinity));

            Assert.False(NumberBaseHelper<Complex>.IsNormal(new Complex(-1.0, double.NegativeInfinity)));
            Assert.True(NumberBaseHelper<Complex>.IsNormal(new Complex(-1.0, -1.0)));
            Assert.True(NumberBaseHelper<Complex>.IsNormal(new Complex(-1.0, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsNormal(new Complex(-1.0, double.NaN)));
            Assert.True(NumberBaseHelper<Complex>.IsNormal(new Complex(-1.0, 0.0)));
            Assert.True(NumberBaseHelper<Complex>.IsNormal(new Complex(-1.0, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsNormal(new Complex(-1.0, double.PositiveInfinity)));

            Assert.False(NumberBaseHelper<Complex>.IsNormal(new Complex(-0.0, double.NegativeInfinity)));
            Assert.False(NumberBaseHelper<Complex>.IsNormal(new Complex(-0.0, -1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsNormal(new Complex(-0.0, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsNormal(new Complex(-0.0, double.NaN)));
            Assert.False(NumberBaseHelper<Complex>.IsNormal(new Complex(-0.0, 0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsNormal(new Complex(-0.0, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsNormal(new Complex(-0.0, double.PositiveInfinity)));

            Assert.False(NumberBaseHelper<Complex>.IsNormal(new Complex(double.NaN, double.NegativeInfinity)));
            Assert.False(NumberBaseHelper<Complex>.IsNormal(new Complex(double.NaN, -1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsNormal(new Complex(double.NaN, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsNormal(new Complex(double.NaN, double.NaN)));
            Assert.False(NumberBaseHelper<Complex>.IsNormal(new Complex(double.NaN, 0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsNormal(new Complex(double.NaN, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsNormal(new Complex(double.NaN, double.PositiveInfinity)));

            Assert.False(NumberBaseHelper<Complex>.IsNormal(new Complex(0.0, double.NegativeInfinity)));
            Assert.False(NumberBaseHelper<Complex>.IsNormal(new Complex(0.0, -1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsNormal(new Complex(0.0, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsNormal(new Complex(0.0, double.NaN)));
            Assert.False(NumberBaseHelper<Complex>.IsNormal(new Complex(0.0, 0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsNormal(new Complex(0.0, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsNormal(new Complex(0.0, double.PositiveInfinity)));

            Assert.False(NumberBaseHelper<Complex>.IsNormal(new Complex(1.0, double.NegativeInfinity)));
            Assert.True(NumberBaseHelper<Complex>.IsNormal(new Complex(1.0, -1.0)));
            Assert.True(NumberBaseHelper<Complex>.IsNormal(new Complex(1.0, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsNormal(new Complex(1.0, double.NaN)));
            Assert.True(NumberBaseHelper<Complex>.IsNormal(new Complex(1.0, 0.0)));
            Assert.True(NumberBaseHelper<Complex>.IsNormal(new Complex(1.0, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsNormal(new Complex(1.0, double.PositiveInfinity)));
        }

        [Fact]
        public static void IsOddIntegerTest()
        {
            Assert.False(NumberBaseHelper<Complex>.IsOddInteger(double.NegativeInfinity));
            Assert.False(NumberBaseHelper<Complex>.IsOddInteger(double.MinValue));
            Assert.True(NumberBaseHelper<Complex>.IsOddInteger(-1.0));
            Assert.False(NumberBaseHelper<Complex>.IsOddInteger(-MinNormal));
            Assert.False(NumberBaseHelper<Complex>.IsOddInteger(-MaxSubnormal));
            Assert.False(NumberBaseHelper<Complex>.IsOddInteger(-double.Epsilon));
            Assert.False(NumberBaseHelper<Complex>.IsOddInteger(-0.0));
            Assert.False(NumberBaseHelper<Complex>.IsOddInteger(double.NaN));
            Assert.False(NumberBaseHelper<Complex>.IsOddInteger(0.0));
            Assert.False(NumberBaseHelper<Complex>.IsOddInteger(double.Epsilon));
            Assert.False(NumberBaseHelper<Complex>.IsOddInteger(MaxSubnormal));
            Assert.False(NumberBaseHelper<Complex>.IsOddInteger(MinNormal));
            Assert.True(NumberBaseHelper<Complex>.IsOddInteger(1.0));
            Assert.False(NumberBaseHelper<Complex>.IsOddInteger(double.MaxValue));
            Assert.False(NumberBaseHelper<Complex>.IsOddInteger(double.PositiveInfinity));

            Assert.False(NumberBaseHelper<Complex>.IsOddInteger(new Complex(-1.0, double.NegativeInfinity)));
            Assert.False(NumberBaseHelper<Complex>.IsOddInteger(new Complex(-1.0, -1.0)));
            Assert.True(NumberBaseHelper<Complex>.IsOddInteger(new Complex(-1.0, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsOddInteger(new Complex(-1.0, double.NaN)));
            Assert.True(NumberBaseHelper<Complex>.IsOddInteger(new Complex(-1.0, 0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsOddInteger(new Complex(-1.0, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsOddInteger(new Complex(-1.0, double.PositiveInfinity)));

            Assert.False(NumberBaseHelper<Complex>.IsOddInteger(new Complex(-0.0, double.NegativeInfinity)));
            Assert.False(NumberBaseHelper<Complex>.IsOddInteger(new Complex(-0.0, -1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsOddInteger(new Complex(-0.0, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsOddInteger(new Complex(-0.0, double.NaN)));
            Assert.False(NumberBaseHelper<Complex>.IsOddInteger(new Complex(-0.0, 0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsOddInteger(new Complex(-0.0, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsOddInteger(new Complex(-0.0, double.PositiveInfinity)));

            Assert.False(NumberBaseHelper<Complex>.IsOddInteger(new Complex(double.NaN, double.NegativeInfinity)));
            Assert.False(NumberBaseHelper<Complex>.IsOddInteger(new Complex(double.NaN, -1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsOddInteger(new Complex(double.NaN, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsOddInteger(new Complex(double.NaN, double.NaN)));
            Assert.False(NumberBaseHelper<Complex>.IsOddInteger(new Complex(double.NaN, 0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsOddInteger(new Complex(double.NaN, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsOddInteger(new Complex(double.NaN, double.PositiveInfinity)));

            Assert.False(NumberBaseHelper<Complex>.IsOddInteger(new Complex(0.0, double.NegativeInfinity)));
            Assert.False(NumberBaseHelper<Complex>.IsOddInteger(new Complex(0.0, -1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsOddInteger(new Complex(0.0, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsOddInteger(new Complex(0.0, double.NaN)));
            Assert.False(NumberBaseHelper<Complex>.IsOddInteger(new Complex(0.0, 0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsOddInteger(new Complex(0.0, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsOddInteger(new Complex(0.0, double.PositiveInfinity)));

            Assert.False(NumberBaseHelper<Complex>.IsOddInteger(new Complex(1.0, double.NegativeInfinity)));
            Assert.False(NumberBaseHelper<Complex>.IsOddInteger(new Complex(1.0, -1.0)));
            Assert.True(NumberBaseHelper<Complex>.IsOddInteger(new Complex(1.0, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsOddInteger(new Complex(1.0, double.NaN)));
            Assert.True(NumberBaseHelper<Complex>.IsOddInteger(new Complex(1.0, 0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsOddInteger(new Complex(1.0, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsOddInteger(new Complex(1.0, double.PositiveInfinity)));
        }

        [Fact]
        public static void IsPositiveTest()
        {
            Assert.False(NumberBaseHelper<Complex>.IsPositive(double.NegativeInfinity));
            Assert.False(NumberBaseHelper<Complex>.IsPositive(double.MinValue));
            Assert.False(NumberBaseHelper<Complex>.IsPositive(-1.0));
            Assert.False(NumberBaseHelper<Complex>.IsPositive(-MinNormal));
            Assert.False(NumberBaseHelper<Complex>.IsPositive(-MaxSubnormal));
            Assert.False(NumberBaseHelper<Complex>.IsPositive(-double.Epsilon));
            Assert.False(NumberBaseHelper<Complex>.IsPositive(-0.0));
            Assert.False(NumberBaseHelper<Complex>.IsPositive(double.NaN));
            Assert.True(NumberBaseHelper<Complex>.IsPositive(0.0));
            Assert.True(NumberBaseHelper<Complex>.IsPositive(double.Epsilon));
            Assert.True(NumberBaseHelper<Complex>.IsPositive(MaxSubnormal));
            Assert.True(NumberBaseHelper<Complex>.IsPositive(MinNormal));
            Assert.True(NumberBaseHelper<Complex>.IsPositive(1.0));
            Assert.True(NumberBaseHelper<Complex>.IsPositive(double.MaxValue));
            Assert.True(NumberBaseHelper<Complex>.IsPositive(double.PositiveInfinity));

            Assert.False(NumberBaseHelper<Complex>.IsPositive(new Complex(-1.0, double.NegativeInfinity)));
            Assert.False(NumberBaseHelper<Complex>.IsPositive(new Complex(-1.0, -1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsPositive(new Complex(-1.0, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsPositive(new Complex(-1.0, double.NaN)));
            Assert.False(NumberBaseHelper<Complex>.IsPositive(new Complex(-1.0, 0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsPositive(new Complex(-1.0, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsPositive(new Complex(-1.0, double.PositiveInfinity)));

            Assert.False(NumberBaseHelper<Complex>.IsPositive(new Complex(-0.0, double.NegativeInfinity)));
            Assert.False(NumberBaseHelper<Complex>.IsPositive(new Complex(-0.0, -1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsPositive(new Complex(-0.0, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsPositive(new Complex(-0.0, double.NaN)));
            Assert.False(NumberBaseHelper<Complex>.IsPositive(new Complex(-0.0, 0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsPositive(new Complex(-0.0, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsPositive(new Complex(-0.0, double.PositiveInfinity)));

            Assert.False(NumberBaseHelper<Complex>.IsPositive(new Complex(double.NaN, double.NegativeInfinity)));
            Assert.False(NumberBaseHelper<Complex>.IsPositive(new Complex(double.NaN, -1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsPositive(new Complex(double.NaN, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsPositive(new Complex(double.NaN, double.NaN)));
            Assert.False(NumberBaseHelper<Complex>.IsPositive(new Complex(double.NaN, 0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsPositive(new Complex(double.NaN, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsPositive(new Complex(double.NaN, double.PositiveInfinity)));

            Assert.False(NumberBaseHelper<Complex>.IsPositive(new Complex(0.0, double.NegativeInfinity)));
            Assert.False(NumberBaseHelper<Complex>.IsPositive(new Complex(0.0, -1.0)));
            Assert.True(NumberBaseHelper<Complex>.IsPositive(new Complex(0.0, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsPositive(new Complex(0.0, double.NaN)));
            Assert.True(NumberBaseHelper<Complex>.IsPositive(new Complex(0.0, 0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsPositive(new Complex(0.0, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsPositive(new Complex(0.0, double.PositiveInfinity)));

            Assert.False(NumberBaseHelper<Complex>.IsPositive(new Complex(1.0, double.NegativeInfinity)));
            Assert.False(NumberBaseHelper<Complex>.IsPositive(new Complex(1.0, -1.0)));
            Assert.True(NumberBaseHelper<Complex>.IsPositive(new Complex(1.0, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsPositive(new Complex(1.0, double.NaN)));
            Assert.True(NumberBaseHelper<Complex>.IsPositive(new Complex(1.0, 0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsPositive(new Complex(1.0, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsPositive(new Complex(1.0, double.PositiveInfinity)));
        }

        [Fact]
        public static void IsPositiveInfinityTest()
        {
            Assert.False(NumberBaseHelper<Complex>.IsPositiveInfinity(double.NegativeInfinity));
            Assert.False(NumberBaseHelper<Complex>.IsPositiveInfinity(double.MinValue));
            Assert.False(NumberBaseHelper<Complex>.IsPositiveInfinity(-1.0));
            Assert.False(NumberBaseHelper<Complex>.IsPositiveInfinity(-MinNormal));
            Assert.False(NumberBaseHelper<Complex>.IsPositiveInfinity(-MaxSubnormal));
            Assert.False(NumberBaseHelper<Complex>.IsPositiveInfinity(-double.Epsilon));
            Assert.False(NumberBaseHelper<Complex>.IsPositiveInfinity(-0.0));
            Assert.False(NumberBaseHelper<Complex>.IsPositiveInfinity(double.NaN));
            Assert.False(NumberBaseHelper<Complex>.IsPositiveInfinity(0.0));
            Assert.False(NumberBaseHelper<Complex>.IsPositiveInfinity(double.Epsilon));
            Assert.False(NumberBaseHelper<Complex>.IsPositiveInfinity(MaxSubnormal));
            Assert.False(NumberBaseHelper<Complex>.IsPositiveInfinity(MinNormal));
            Assert.False(NumberBaseHelper<Complex>.IsPositiveInfinity(1.0));
            Assert.False(NumberBaseHelper<Complex>.IsPositiveInfinity(double.MaxValue));
            Assert.True(NumberBaseHelper<Complex>.IsPositiveInfinity(double.PositiveInfinity));

            Assert.False(NumberBaseHelper<Complex>.IsPositiveInfinity(new Complex(-1.0, double.NegativeInfinity)));
            Assert.False(NumberBaseHelper<Complex>.IsPositiveInfinity(new Complex(-1.0, -1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsPositiveInfinity(new Complex(-1.0, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsPositiveInfinity(new Complex(-1.0, double.NaN)));
            Assert.False(NumberBaseHelper<Complex>.IsPositiveInfinity(new Complex(-1.0, 0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsPositiveInfinity(new Complex(-1.0, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsPositiveInfinity(new Complex(-1.0, double.PositiveInfinity)));

            Assert.False(NumberBaseHelper<Complex>.IsPositiveInfinity(new Complex(-0.0, double.NegativeInfinity)));
            Assert.False(NumberBaseHelper<Complex>.IsPositiveInfinity(new Complex(-0.0, -1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsPositiveInfinity(new Complex(-0.0, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsPositiveInfinity(new Complex(-0.0, double.NaN)));
            Assert.False(NumberBaseHelper<Complex>.IsPositiveInfinity(new Complex(-0.0, 0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsPositiveInfinity(new Complex(-0.0, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsPositiveInfinity(new Complex(-0.0, double.PositiveInfinity)));

            Assert.False(NumberBaseHelper<Complex>.IsPositiveInfinity(new Complex(double.NaN, double.NegativeInfinity)));
            Assert.False(NumberBaseHelper<Complex>.IsPositiveInfinity(new Complex(double.NaN, -1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsPositiveInfinity(new Complex(double.NaN, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsPositiveInfinity(new Complex(double.NaN, double.NaN)));
            Assert.False(NumberBaseHelper<Complex>.IsPositiveInfinity(new Complex(double.NaN, 0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsPositiveInfinity(new Complex(double.NaN, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsPositiveInfinity(new Complex(double.NaN, double.PositiveInfinity)));

            Assert.False(NumberBaseHelper<Complex>.IsPositiveInfinity(new Complex(0.0, double.NegativeInfinity)));
            Assert.False(NumberBaseHelper<Complex>.IsPositiveInfinity(new Complex(0.0, -1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsPositiveInfinity(new Complex(0.0, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsPositiveInfinity(new Complex(0.0, double.NaN)));
            Assert.False(NumberBaseHelper<Complex>.IsPositiveInfinity(new Complex(0.0, 0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsPositiveInfinity(new Complex(0.0, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsPositiveInfinity(new Complex(0.0, double.PositiveInfinity)));

            Assert.False(NumberBaseHelper<Complex>.IsPositiveInfinity(new Complex(1.0, double.NegativeInfinity)));
            Assert.False(NumberBaseHelper<Complex>.IsPositiveInfinity(new Complex(1.0, -1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsPositiveInfinity(new Complex(1.0, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsPositiveInfinity(new Complex(1.0, double.NaN)));
            Assert.False(NumberBaseHelper<Complex>.IsPositiveInfinity(new Complex(1.0, 0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsPositiveInfinity(new Complex(1.0, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsPositiveInfinity(new Complex(1.0, double.PositiveInfinity)));
        }

        [Fact]
        public static void IsRealNumberTest()
        {
            Assert.True(NumberBaseHelper<Complex>.IsRealNumber(double.NegativeInfinity));
            Assert.True(NumberBaseHelper<Complex>.IsRealNumber(double.MinValue));
            Assert.True(NumberBaseHelper<Complex>.IsRealNumber(-1.0));
            Assert.True(NumberBaseHelper<Complex>.IsRealNumber(-MinNormal));
            Assert.True(NumberBaseHelper<Complex>.IsRealNumber(-MaxSubnormal));
            Assert.True(NumberBaseHelper<Complex>.IsRealNumber(-double.Epsilon));
            Assert.True(NumberBaseHelper<Complex>.IsRealNumber(-0.0));
            Assert.False(NumberBaseHelper<Complex>.IsRealNumber(double.NaN));
            Assert.True(NumberBaseHelper<Complex>.IsRealNumber(0.0));
            Assert.True(NumberBaseHelper<Complex>.IsRealNumber(double.Epsilon));
            Assert.True(NumberBaseHelper<Complex>.IsRealNumber(MaxSubnormal));
            Assert.True(NumberBaseHelper<Complex>.IsRealNumber(MinNormal));
            Assert.True(NumberBaseHelper<Complex>.IsRealNumber(1.0));
            Assert.True(NumberBaseHelper<Complex>.IsRealNumber(double.MaxValue));
            Assert.True(NumberBaseHelper<Complex>.IsRealNumber(double.PositiveInfinity));

            Assert.False(NumberBaseHelper<Complex>.IsRealNumber(new Complex(-1.0, double.NegativeInfinity)));
            Assert.False(NumberBaseHelper<Complex>.IsRealNumber(new Complex(-1.0, -1.0)));
            Assert.True(NumberBaseHelper<Complex>.IsRealNumber(new Complex(-1.0, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsRealNumber(new Complex(-1.0, double.NaN)));
            Assert.True(NumberBaseHelper<Complex>.IsRealNumber(new Complex(-1.0, 0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsRealNumber(new Complex(-1.0, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsRealNumber(new Complex(-1.0, double.PositiveInfinity)));

            Assert.False(NumberBaseHelper<Complex>.IsRealNumber(new Complex(-0.0, double.NegativeInfinity)));
            Assert.False(NumberBaseHelper<Complex>.IsRealNumber(new Complex(-0.0, -1.0)));
            Assert.True(NumberBaseHelper<Complex>.IsRealNumber(new Complex(-0.0, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsRealNumber(new Complex(-0.0, double.NaN)));
            Assert.True(NumberBaseHelper<Complex>.IsRealNumber(new Complex(-0.0, 0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsRealNumber(new Complex(-0.0, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsRealNumber(new Complex(-0.0, double.PositiveInfinity)));

            Assert.False(NumberBaseHelper<Complex>.IsRealNumber(new Complex(double.NaN, double.NegativeInfinity)));
            Assert.False(NumberBaseHelper<Complex>.IsRealNumber(new Complex(double.NaN, -1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsRealNumber(new Complex(double.NaN, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsRealNumber(new Complex(double.NaN, double.NaN)));
            Assert.False(NumberBaseHelper<Complex>.IsRealNumber(new Complex(double.NaN, 0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsRealNumber(new Complex(double.NaN, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsRealNumber(new Complex(double.NaN, double.PositiveInfinity)));

            Assert.False(NumberBaseHelper<Complex>.IsRealNumber(new Complex(0.0, double.NegativeInfinity)));
            Assert.False(NumberBaseHelper<Complex>.IsRealNumber(new Complex(0.0, -1.0)));
            Assert.True(NumberBaseHelper<Complex>.IsRealNumber(new Complex(0.0, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsRealNumber(new Complex(0.0, double.NaN)));
            Assert.True(NumberBaseHelper<Complex>.IsRealNumber(new Complex(0.0, 0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsRealNumber(new Complex(0.0, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsRealNumber(new Complex(0.0, double.PositiveInfinity)));

            Assert.False(NumberBaseHelper<Complex>.IsRealNumber(new Complex(1.0, double.NegativeInfinity)));
            Assert.False(NumberBaseHelper<Complex>.IsRealNumber(new Complex(1.0, -1.0)));
            Assert.True(NumberBaseHelper<Complex>.IsRealNumber(new Complex(1.0, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsRealNumber(new Complex(1.0, double.NaN)));
            Assert.True(NumberBaseHelper<Complex>.IsRealNumber(new Complex(1.0, 0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsRealNumber(new Complex(1.0, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsRealNumber(new Complex(1.0, double.PositiveInfinity)));
        }

        [Fact]
        public static void IsSubnormalTest()
        {
            Assert.False(NumberBaseHelper<Complex>.IsSubnormal(double.NegativeInfinity));
            Assert.False(NumberBaseHelper<Complex>.IsSubnormal(double.MinValue));
            Assert.False(NumberBaseHelper<Complex>.IsSubnormal(-1.0));
            Assert.False(NumberBaseHelper<Complex>.IsSubnormal(-MinNormal));
            Assert.True(NumberBaseHelper<Complex>.IsSubnormal(-MaxSubnormal));
            Assert.True(NumberBaseHelper<Complex>.IsSubnormal(-double.Epsilon));
            Assert.False(NumberBaseHelper<Complex>.IsSubnormal(-0.0));
            Assert.False(NumberBaseHelper<Complex>.IsSubnormal(double.NaN));
            Assert.False(NumberBaseHelper<Complex>.IsSubnormal(0.0));
            Assert.True(NumberBaseHelper<Complex>.IsSubnormal(double.Epsilon));
            Assert.True(NumberBaseHelper<Complex>.IsSubnormal(MaxSubnormal));
            Assert.False(NumberBaseHelper<Complex>.IsSubnormal(MinNormal));
            Assert.False(NumberBaseHelper<Complex>.IsSubnormal(1.0));
            Assert.False(NumberBaseHelper<Complex>.IsSubnormal(double.MaxValue));
            Assert.False(NumberBaseHelper<Complex>.IsSubnormal(double.PositiveInfinity));

            Assert.False(NumberBaseHelper<Complex>.IsSubnormal(new Complex(-1.0, double.NegativeInfinity)));
            Assert.False(NumberBaseHelper<Complex>.IsSubnormal(new Complex(-1.0, -1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsSubnormal(new Complex(-1.0, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsSubnormal(new Complex(-1.0, double.NaN)));
            Assert.False(NumberBaseHelper<Complex>.IsSubnormal(new Complex(-1.0, 0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsSubnormal(new Complex(-1.0, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsSubnormal(new Complex(-1.0, double.PositiveInfinity)));

            Assert.False(NumberBaseHelper<Complex>.IsSubnormal(new Complex(-0.0, double.NegativeInfinity)));
            Assert.False(NumberBaseHelper<Complex>.IsSubnormal(new Complex(-0.0, -1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsSubnormal(new Complex(-0.0, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsSubnormal(new Complex(-0.0, double.NaN)));
            Assert.False(NumberBaseHelper<Complex>.IsSubnormal(new Complex(-0.0, 0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsSubnormal(new Complex(-0.0, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsSubnormal(new Complex(-0.0, double.PositiveInfinity)));

            Assert.False(NumberBaseHelper<Complex>.IsSubnormal(new Complex(double.NaN, double.NegativeInfinity)));
            Assert.False(NumberBaseHelper<Complex>.IsSubnormal(new Complex(double.NaN, -1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsSubnormal(new Complex(double.NaN, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsSubnormal(new Complex(double.NaN, double.NaN)));
            Assert.False(NumberBaseHelper<Complex>.IsSubnormal(new Complex(double.NaN, 0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsSubnormal(new Complex(double.NaN, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsSubnormal(new Complex(double.NaN, double.PositiveInfinity)));

            Assert.False(NumberBaseHelper<Complex>.IsSubnormal(new Complex(0.0, double.NegativeInfinity)));
            Assert.False(NumberBaseHelper<Complex>.IsSubnormal(new Complex(0.0, -1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsSubnormal(new Complex(0.0, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsSubnormal(new Complex(0.0, double.NaN)));
            Assert.False(NumberBaseHelper<Complex>.IsSubnormal(new Complex(0.0, 0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsSubnormal(new Complex(0.0, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsSubnormal(new Complex(0.0, double.PositiveInfinity)));

            Assert.False(NumberBaseHelper<Complex>.IsSubnormal(new Complex(1.0, double.NegativeInfinity)));
            Assert.False(NumberBaseHelper<Complex>.IsSubnormal(new Complex(1.0, -1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsSubnormal(new Complex(1.0, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsSubnormal(new Complex(1.0, double.NaN)));
            Assert.False(NumberBaseHelper<Complex>.IsSubnormal(new Complex(1.0, 0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsSubnormal(new Complex(1.0, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsSubnormal(new Complex(1.0, double.PositiveInfinity)));
        }

        [Fact]
        public static void IsZeroTest()
        {
            Assert.False(NumberBaseHelper<Complex>.IsZero(double.NegativeInfinity));
            Assert.False(NumberBaseHelper<Complex>.IsZero(double.MinValue));
            Assert.False(NumberBaseHelper<Complex>.IsZero(-1.0));
            Assert.False(NumberBaseHelper<Complex>.IsZero(-MinNormal));
            Assert.False(NumberBaseHelper<Complex>.IsZero(-MaxSubnormal));
            Assert.False(NumberBaseHelper<Complex>.IsZero(-double.Epsilon));
            Assert.True(NumberBaseHelper<Complex>.IsZero(-0.0));
            Assert.False(NumberBaseHelper<Complex>.IsZero(double.NaN));
            Assert.True(NumberBaseHelper<Complex>.IsZero(0.0));
            Assert.False(NumberBaseHelper<Complex>.IsZero(double.Epsilon));
            Assert.False(NumberBaseHelper<Complex>.IsZero(MaxSubnormal));
            Assert.False(NumberBaseHelper<Complex>.IsZero(MinNormal));
            Assert.False(NumberBaseHelper<Complex>.IsZero(1.0));
            Assert.False(NumberBaseHelper<Complex>.IsZero(double.MaxValue));
            Assert.False(NumberBaseHelper<Complex>.IsZero(double.PositiveInfinity));

            Assert.False(NumberBaseHelper<Complex>.IsZero(new Complex(-1.0, double.NegativeInfinity)));
            Assert.False(NumberBaseHelper<Complex>.IsZero(new Complex(-1.0, -1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsZero(new Complex(-1.0, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsZero(new Complex(-1.0, double.NaN)));
            Assert.False(NumberBaseHelper<Complex>.IsZero(new Complex(-1.0, 0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsZero(new Complex(-1.0, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsZero(new Complex(-1.0, double.PositiveInfinity)));

            Assert.False(NumberBaseHelper<Complex>.IsZero(new Complex(-0.0, double.NegativeInfinity)));
            Assert.False(NumberBaseHelper<Complex>.IsZero(new Complex(-0.0, -1.0)));
            Assert.True(NumberBaseHelper<Complex>.IsZero(new Complex(-0.0, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsZero(new Complex(-0.0, double.NaN)));
            Assert.True(NumberBaseHelper<Complex>.IsZero(new Complex(-0.0, 0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsZero(new Complex(-0.0, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsZero(new Complex(-0.0, double.PositiveInfinity)));

            Assert.False(NumberBaseHelper<Complex>.IsZero(new Complex(double.NaN, double.NegativeInfinity)));
            Assert.False(NumberBaseHelper<Complex>.IsZero(new Complex(double.NaN, -1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsZero(new Complex(double.NaN, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsZero(new Complex(double.NaN, double.NaN)));
            Assert.False(NumberBaseHelper<Complex>.IsZero(new Complex(double.NaN, 0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsZero(new Complex(double.NaN, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsZero(new Complex(double.NaN, double.PositiveInfinity)));

            Assert.False(NumberBaseHelper<Complex>.IsZero(new Complex(0.0, double.NegativeInfinity)));
            Assert.False(NumberBaseHelper<Complex>.IsZero(new Complex(0.0, -1.0)));
            Assert.True(NumberBaseHelper<Complex>.IsZero(new Complex(0.0, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsZero(new Complex(0.0, double.NaN)));
            Assert.True(NumberBaseHelper<Complex>.IsZero(new Complex(0.0, 0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsZero(new Complex(0.0, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsZero(new Complex(0.0, double.PositiveInfinity)));

            Assert.False(NumberBaseHelper<Complex>.IsZero(new Complex(1.0, double.NegativeInfinity)));
            Assert.False(NumberBaseHelper<Complex>.IsZero(new Complex(1.0, -1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsZero(new Complex(1.0, -0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsZero(new Complex(1.0, double.NaN)));
            Assert.False(NumberBaseHelper<Complex>.IsZero(new Complex(1.0, 0.0)));
            Assert.False(NumberBaseHelper<Complex>.IsZero(new Complex(1.0, 1.0)));
            Assert.False(NumberBaseHelper<Complex>.IsZero(new Complex(1.0, double.PositiveInfinity)));
        }

        [Fact]
        public static void MaxMagnitudeTest()
        {
            AssertBitwiseEqual(double.NegativeInfinity, NumberBaseHelper<Complex>.MaxMagnitude(double.NegativeInfinity, 1.0));
            AssertBitwiseEqual(double.MinValue, NumberBaseHelper<Complex>.MaxMagnitude(double.MinValue, 1.0));
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.MaxMagnitude(-1.0, 1.0));
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.MaxMagnitude(-MinNormal, 1.0));
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.MaxMagnitude(-MaxSubnormal, 1.0));
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.MaxMagnitude(-double.Epsilon, 1.0));
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.MaxMagnitude(-0.0, 1.0));
            AssertBitwiseEqual(double.NaN, NumberBaseHelper<Complex>.MaxMagnitude(double.NaN, 1.0));
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.MaxMagnitude(0.0, 1.0));
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.MaxMagnitude(double.Epsilon, 1.0));
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.MaxMagnitude(MaxSubnormal, 1.0));
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.MaxMagnitude(MinNormal, 1.0));
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.MaxMagnitude(1.0, 1.0));
            AssertBitwiseEqual(double.MaxValue, NumberBaseHelper<Complex>.MaxMagnitude(double.MaxValue, 1.0));
            AssertBitwiseEqual(double.PositiveInfinity, NumberBaseHelper<Complex>.MaxMagnitude(double.PositiveInfinity, 1.0));

            AssertBitwiseEqual(new Complex(-1.0, -1.0), NumberBaseHelper<Complex>.MaxMagnitude(new Complex(-1.0, -1.0), new Complex(-1.0, -1.0)));
            AssertBitwiseEqual(new Complex(-1.0, +1.0), NumberBaseHelper<Complex>.MaxMagnitude(new Complex(-1.0, -1.0), new Complex(-1.0, +1.0)));
            AssertBitwiseEqual(new Complex(+1.0, -1.0), NumberBaseHelper<Complex>.MaxMagnitude(new Complex(-1.0, -1.0), new Complex(+1.0, -1.0)));
            AssertBitwiseEqual(new Complex(+1.0, +1.0), NumberBaseHelper<Complex>.MaxMagnitude(new Complex(-1.0, -1.0), new Complex(+1.0, +1.0)));

            AssertBitwiseEqual(new Complex(-1.0, +1.0), NumberBaseHelper<Complex>.MaxMagnitude(new Complex(-1.0, +1.0), new Complex(-1.0, -1.0)));
            AssertBitwiseEqual(new Complex(-1.0, +1.0), NumberBaseHelper<Complex>.MaxMagnitude(new Complex(-1.0, +1.0), new Complex(-1.0, +1.0)));
            AssertBitwiseEqual(new Complex(+1.0, -1.0), NumberBaseHelper<Complex>.MaxMagnitude(new Complex(-1.0, +1.0), new Complex(+1.0, -1.0)));
            AssertBitwiseEqual(new Complex(+1.0, +1.0), NumberBaseHelper<Complex>.MaxMagnitude(new Complex(-1.0, +1.0), new Complex(+1.0, +1.0)));

            AssertBitwiseEqual(new Complex(+1.0, -1.0), NumberBaseHelper<Complex>.MaxMagnitude(new Complex(+1.0, -1.0), new Complex(-1.0, -1.0)));
            AssertBitwiseEqual(new Complex(+1.0, -1.0), NumberBaseHelper<Complex>.MaxMagnitude(new Complex(+1.0, -1.0), new Complex(-1.0, +1.0)));
            AssertBitwiseEqual(new Complex(+1.0, -1.0), NumberBaseHelper<Complex>.MaxMagnitude(new Complex(+1.0, -1.0), new Complex(+1.0, -1.0)));
            AssertBitwiseEqual(new Complex(+1.0, +1.0), NumberBaseHelper<Complex>.MaxMagnitude(new Complex(+1.0, -1.0), new Complex(+1.0, +1.0)));

            AssertBitwiseEqual(new Complex(+1.0, +1.0), NumberBaseHelper<Complex>.MaxMagnitude(new Complex(+1.0, +1.0), new Complex(-1.0, -1.0)));
            AssertBitwiseEqual(new Complex(+1.0, +1.0), NumberBaseHelper<Complex>.MaxMagnitude(new Complex(+1.0, +1.0), new Complex(-1.0, +1.0)));
            AssertBitwiseEqual(new Complex(+1.0, +1.0), NumberBaseHelper<Complex>.MaxMagnitude(new Complex(+1.0, +1.0), new Complex(+1.0, -1.0)));
            AssertBitwiseEqual(new Complex(+1.0, +1.0), NumberBaseHelper<Complex>.MaxMagnitude(new Complex(+1.0, +1.0), new Complex(+1.0, +1.0)));
        }

        [Fact]
        public static void MaxMagnitudeNumberTest()
        {
            AssertBitwiseEqual(double.NegativeInfinity, NumberBaseHelper<Complex>.MaxMagnitudeNumber(double.NegativeInfinity, 1.0));
            AssertBitwiseEqual(double.MinValue, NumberBaseHelper<Complex>.MaxMagnitudeNumber(double.MinValue, 1.0));
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.MaxMagnitudeNumber(-1.0, 1.0));
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.MaxMagnitudeNumber(-MinNormal, 1.0));
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.MaxMagnitudeNumber(-MaxSubnormal, 1.0));
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.MaxMagnitudeNumber(-double.Epsilon, 1.0));
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.MaxMagnitudeNumber(-0.0, 1.0));
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.MaxMagnitudeNumber(double.NaN, 1.0));
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.MaxMagnitudeNumber(0.0, 1.0));
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.MaxMagnitudeNumber(double.Epsilon, 1.0));
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.MaxMagnitudeNumber(MaxSubnormal, 1.0));
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.MaxMagnitudeNumber(MinNormal, 1.0));
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.MaxMagnitudeNumber(1.0, 1.0));
            AssertBitwiseEqual(double.MaxValue, NumberBaseHelper<Complex>.MaxMagnitudeNumber(double.MaxValue, 1.0));
            AssertBitwiseEqual(double.PositiveInfinity, NumberBaseHelper<Complex>.MaxMagnitudeNumber(double.PositiveInfinity, 1.0));

            AssertBitwiseEqual(new Complex(-1.0, -1.0), NumberBaseHelper<Complex>.MaxMagnitudeNumber(new Complex(-1.0, -1.0), new Complex(-1.0, -1.0)));
            AssertBitwiseEqual(new Complex(-1.0, +1.0), NumberBaseHelper<Complex>.MaxMagnitudeNumber(new Complex(-1.0, -1.0), new Complex(-1.0, +1.0)));
            AssertBitwiseEqual(new Complex(+1.0, -1.0), NumberBaseHelper<Complex>.MaxMagnitudeNumber(new Complex(-1.0, -1.0), new Complex(+1.0, -1.0)));
            AssertBitwiseEqual(new Complex(+1.0, +1.0), NumberBaseHelper<Complex>.MaxMagnitudeNumber(new Complex(-1.0, -1.0), new Complex(+1.0, +1.0)));

            AssertBitwiseEqual(new Complex(-1.0, +1.0), NumberBaseHelper<Complex>.MaxMagnitudeNumber(new Complex(-1.0, +1.0), new Complex(-1.0, -1.0)));
            AssertBitwiseEqual(new Complex(-1.0, +1.0), NumberBaseHelper<Complex>.MaxMagnitudeNumber(new Complex(-1.0, +1.0), new Complex(-1.0, +1.0)));
            AssertBitwiseEqual(new Complex(+1.0, -1.0), NumberBaseHelper<Complex>.MaxMagnitudeNumber(new Complex(-1.0, +1.0), new Complex(+1.0, -1.0)));
            AssertBitwiseEqual(new Complex(+1.0, +1.0), NumberBaseHelper<Complex>.MaxMagnitudeNumber(new Complex(-1.0, +1.0), new Complex(+1.0, +1.0)));

            AssertBitwiseEqual(new Complex(+1.0, -1.0), NumberBaseHelper<Complex>.MaxMagnitudeNumber(new Complex(+1.0, -1.0), new Complex(-1.0, -1.0)));
            AssertBitwiseEqual(new Complex(+1.0, -1.0), NumberBaseHelper<Complex>.MaxMagnitudeNumber(new Complex(+1.0, -1.0), new Complex(-1.0, +1.0)));
            AssertBitwiseEqual(new Complex(+1.0, -1.0), NumberBaseHelper<Complex>.MaxMagnitudeNumber(new Complex(+1.0, -1.0), new Complex(+1.0, -1.0)));
            AssertBitwiseEqual(new Complex(+1.0, +1.0), NumberBaseHelper<Complex>.MaxMagnitudeNumber(new Complex(+1.0, -1.0), new Complex(+1.0, +1.0)));

            AssertBitwiseEqual(new Complex(+1.0, +1.0), NumberBaseHelper<Complex>.MaxMagnitudeNumber(new Complex(+1.0, +1.0), new Complex(-1.0, -1.0)));
            AssertBitwiseEqual(new Complex(+1.0, +1.0), NumberBaseHelper<Complex>.MaxMagnitudeNumber(new Complex(+1.0, +1.0), new Complex(-1.0, +1.0)));
            AssertBitwiseEqual(new Complex(+1.0, +1.0), NumberBaseHelper<Complex>.MaxMagnitudeNumber(new Complex(+1.0, +1.0), new Complex(+1.0, -1.0)));
            AssertBitwiseEqual(new Complex(+1.0, +1.0), NumberBaseHelper<Complex>.MaxMagnitudeNumber(new Complex(+1.0, +1.0), new Complex(+1.0, +1.0)));
        }

        [Fact]
        public static void MinMagnitudeTest()
        {
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.MinMagnitude(double.NegativeInfinity, 1.0));
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.MinMagnitude(double.MinValue, 1.0));
            AssertBitwiseEqual(-1.0, NumberBaseHelper<Complex>.MinMagnitude(-1.0, 1.0));
            AssertBitwiseEqual(-MinNormal, NumberBaseHelper<Complex>.MinMagnitude(-MinNormal, 1.0));
            AssertBitwiseEqual(-MaxSubnormal, NumberBaseHelper<Complex>.MinMagnitude(-MaxSubnormal, 1.0));
            AssertBitwiseEqual(-double.Epsilon, NumberBaseHelper<Complex>.MinMagnitude(-double.Epsilon, 1.0));
            AssertBitwiseEqual(-0.0, NumberBaseHelper<Complex>.MinMagnitude(-0.0, 1.0));
            AssertBitwiseEqual(double.NaN, NumberBaseHelper<Complex>.MinMagnitude(double.NaN, 1.0));
            AssertBitwiseEqual(0.0, NumberBaseHelper<Complex>.MinMagnitude(0.0, 1.0));
            AssertBitwiseEqual(double.Epsilon, NumberBaseHelper<Complex>.MinMagnitude(double.Epsilon, 1.0));
            AssertBitwiseEqual(MaxSubnormal, NumberBaseHelper<Complex>.MinMagnitude(MaxSubnormal, 1.0));
            AssertBitwiseEqual(MinNormal, NumberBaseHelper<Complex>.MinMagnitude(MinNormal, 1.0));
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.MinMagnitude(1.0, 1.0));
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.MinMagnitude(double.MaxValue, 1.0));
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.MinMagnitude(double.PositiveInfinity, 1.0));

            AssertBitwiseEqual(new Complex(-1.0, -1.0), NumberBaseHelper<Complex>.MinMagnitude(new Complex(-1.0, -1.0), new Complex(-1.0, -1.0)));
            AssertBitwiseEqual(new Complex(-1.0, -1.0), NumberBaseHelper<Complex>.MinMagnitude(new Complex(-1.0, -1.0), new Complex(-1.0, +1.0)));
            AssertBitwiseEqual(new Complex(-1.0, -1.0), NumberBaseHelper<Complex>.MinMagnitude(new Complex(-1.0, -1.0), new Complex(+1.0, -1.0)));
            AssertBitwiseEqual(new Complex(-1.0, -1.0), NumberBaseHelper<Complex>.MinMagnitude(new Complex(-1.0, -1.0), new Complex(+1.0, +1.0)));

            AssertBitwiseEqual(new Complex(-1.0, -1.0), NumberBaseHelper<Complex>.MinMagnitude(new Complex(-1.0, +1.0), new Complex(-1.0, -1.0)));
            AssertBitwiseEqual(new Complex(-1.0, +1.0), NumberBaseHelper<Complex>.MinMagnitude(new Complex(-1.0, +1.0), new Complex(-1.0, +1.0)));
            AssertBitwiseEqual(new Complex(-1.0, +1.0), NumberBaseHelper<Complex>.MinMagnitude(new Complex(-1.0, +1.0), new Complex(+1.0, -1.0)));
            AssertBitwiseEqual(new Complex(-1.0, +1.0), NumberBaseHelper<Complex>.MinMagnitude(new Complex(-1.0, +1.0), new Complex(+1.0, +1.0)));

            AssertBitwiseEqual(new Complex(-1.0, -1.0), NumberBaseHelper<Complex>.MinMagnitude(new Complex(+1.0, -1.0), new Complex(-1.0, -1.0)));
            AssertBitwiseEqual(new Complex(-1.0, +1.0), NumberBaseHelper<Complex>.MinMagnitude(new Complex(+1.0, -1.0), new Complex(-1.0, +1.0)));
            AssertBitwiseEqual(new Complex(+1.0, -1.0), NumberBaseHelper<Complex>.MinMagnitude(new Complex(+1.0, -1.0), new Complex(+1.0, -1.0)));
            AssertBitwiseEqual(new Complex(+1.0, -1.0), NumberBaseHelper<Complex>.MinMagnitude(new Complex(+1.0, -1.0), new Complex(+1.0, +1.0)));

            AssertBitwiseEqual(new Complex(-1.0, -1.0), NumberBaseHelper<Complex>.MinMagnitude(new Complex(+1.0, +1.0), new Complex(-1.0, -1.0)));
            AssertBitwiseEqual(new Complex(-1.0, +1.0), NumberBaseHelper<Complex>.MinMagnitude(new Complex(+1.0, +1.0), new Complex(-1.0, +1.0)));
            AssertBitwiseEqual(new Complex(+1.0, -1.0), NumberBaseHelper<Complex>.MinMagnitude(new Complex(+1.0, +1.0), new Complex(+1.0, -1.0)));
            AssertBitwiseEqual(new Complex(+1.0, +1.0), NumberBaseHelper<Complex>.MinMagnitude(new Complex(+1.0, +1.0), new Complex(+1.0, +1.0)));
        }

        [Fact]
        public static void MinMagnitudeNumberTest()
        {
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.MinMagnitudeNumber(double.NegativeInfinity, 1.0));
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.MinMagnitudeNumber(double.MinValue, 1.0));
            AssertBitwiseEqual(-1.0, NumberBaseHelper<Complex>.MinMagnitudeNumber(-1.0, 1.0));
            AssertBitwiseEqual(-MinNormal, NumberBaseHelper<Complex>.MinMagnitudeNumber(-MinNormal, 1.0));
            AssertBitwiseEqual(-MaxSubnormal, NumberBaseHelper<Complex>.MinMagnitudeNumber(-MaxSubnormal, 1.0));
            AssertBitwiseEqual(-double.Epsilon, NumberBaseHelper<Complex>.MinMagnitudeNumber(-double.Epsilon, 1.0));
            AssertBitwiseEqual(-0.0, NumberBaseHelper<Complex>.MinMagnitudeNumber(-0.0, 1.0));
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.MinMagnitudeNumber(double.NaN, 1.0));
            AssertBitwiseEqual(0.0, NumberBaseHelper<Complex>.MinMagnitudeNumber(0.0, 1.0));
            AssertBitwiseEqual(double.Epsilon, NumberBaseHelper<Complex>.MinMagnitudeNumber(double.Epsilon, 1.0));
            AssertBitwiseEqual(MaxSubnormal, NumberBaseHelper<Complex>.MinMagnitudeNumber(MaxSubnormal, 1.0));
            AssertBitwiseEqual(MinNormal, NumberBaseHelper<Complex>.MinMagnitudeNumber(MinNormal, 1.0));
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.MinMagnitudeNumber(1.0, 1.0));
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.MinMagnitudeNumber(double.MaxValue, 1.0));
            AssertBitwiseEqual(1.0, NumberBaseHelper<Complex>.MinMagnitudeNumber(double.PositiveInfinity, 1.0));

            AssertBitwiseEqual(new Complex(-1.0, -1.0), NumberBaseHelper<Complex>.MinMagnitudeNumber(new Complex(-1.0, -1.0), new Complex(-1.0, -1.0)));
            AssertBitwiseEqual(new Complex(-1.0, -1.0), NumberBaseHelper<Complex>.MinMagnitudeNumber(new Complex(-1.0, -1.0), new Complex(-1.0, +1.0)));
            AssertBitwiseEqual(new Complex(-1.0, -1.0), NumberBaseHelper<Complex>.MinMagnitudeNumber(new Complex(-1.0, -1.0), new Complex(+1.0, -1.0)));
            AssertBitwiseEqual(new Complex(-1.0, -1.0), NumberBaseHelper<Complex>.MinMagnitudeNumber(new Complex(-1.0, -1.0), new Complex(+1.0, +1.0)));

            AssertBitwiseEqual(new Complex(-1.0, -1.0), NumberBaseHelper<Complex>.MinMagnitudeNumber(new Complex(-1.0, +1.0), new Complex(-1.0, -1.0)));
            AssertBitwiseEqual(new Complex(-1.0, +1.0), NumberBaseHelper<Complex>.MinMagnitudeNumber(new Complex(-1.0, +1.0), new Complex(-1.0, +1.0)));
            AssertBitwiseEqual(new Complex(-1.0, +1.0), NumberBaseHelper<Complex>.MinMagnitudeNumber(new Complex(-1.0, +1.0), new Complex(+1.0, -1.0)));
            AssertBitwiseEqual(new Complex(-1.0, +1.0), NumberBaseHelper<Complex>.MinMagnitudeNumber(new Complex(-1.0, +1.0), new Complex(+1.0, +1.0)));

            AssertBitwiseEqual(new Complex(-1.0, -1.0), NumberBaseHelper<Complex>.MinMagnitudeNumber(new Complex(+1.0, -1.0), new Complex(-1.0, -1.0)));
            AssertBitwiseEqual(new Complex(-1.0, +1.0), NumberBaseHelper<Complex>.MinMagnitudeNumber(new Complex(+1.0, -1.0), new Complex(-1.0, +1.0)));
            AssertBitwiseEqual(new Complex(+1.0, -1.0), NumberBaseHelper<Complex>.MinMagnitudeNumber(new Complex(+1.0, -1.0), new Complex(+1.0, -1.0)));
            AssertBitwiseEqual(new Complex(+1.0, -1.0), NumberBaseHelper<Complex>.MinMagnitudeNumber(new Complex(+1.0, -1.0), new Complex(+1.0, +1.0)));

            AssertBitwiseEqual(new Complex(-1.0, -1.0), NumberBaseHelper<Complex>.MinMagnitudeNumber(new Complex(+1.0, +1.0), new Complex(-1.0, -1.0)));
            AssertBitwiseEqual(new Complex(-1.0, +1.0), NumberBaseHelper<Complex>.MinMagnitudeNumber(new Complex(+1.0, +1.0), new Complex(-1.0, +1.0)));
            AssertBitwiseEqual(new Complex(+1.0, -1.0), NumberBaseHelper<Complex>.MinMagnitudeNumber(new Complex(+1.0, +1.0), new Complex(+1.0, -1.0)));
            AssertBitwiseEqual(new Complex(+1.0, +1.0), NumberBaseHelper<Complex>.MinMagnitudeNumber(new Complex(+1.0, +1.0), new Complex(+1.0, +1.0)));
        }

        //
        // INumberBase.TryConvertTo
        //

        [Fact]
        public static void TryConvertToCheckedByteTest()
        {
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateChecked<Complex>(0.0));
            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.CreateChecked<Complex>(1.0));
            Assert.Equal((byte)0x7F, NumberBaseHelper<byte>.CreateChecked<Complex>(127.0));
            Assert.Equal((byte)0x80, NumberBaseHelper<byte>.CreateChecked<Complex>(128.0));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateChecked<Complex>(255.0));
        }

        [Fact]
        public static void TryConvertToCheckedCharTest()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateChecked<Complex>(0.0));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateChecked<Complex>(1.0));
            Assert.Equal((char)0x7FFF, NumberBaseHelper<char>.CreateChecked<Complex>(32767.0));
            Assert.Equal((char)0x8000, NumberBaseHelper<char>.CreateChecked<Complex>(32768.0));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateChecked<Complex>(65535.0));
        }

        [Fact]
        public static void TryConvertToCheckedDecimalTest()
        {
            Assert.Equal(-79228162514264300000000000000.0m, NumberBaseHelper<decimal>.CreateChecked<Complex>(-79228162514264333195497439231.0));
            Assert.Equal(-1.0m, NumberBaseHelper<decimal>.CreateChecked<Complex>(-1.0));
            Assert.Equal(-0.0m, NumberBaseHelper<decimal>.CreateChecked<Complex>(-0.0));
            Assert.Equal(+0.0m, NumberBaseHelper<decimal>.CreateChecked<Complex>(+0.0));
            Assert.Equal(+1.0m, NumberBaseHelper<decimal>.CreateChecked<Complex>(+1.0));
            Assert.Equal(+79228162514264300000000000000.0m, NumberBaseHelper<decimal>.CreateChecked<Complex>(+79228162514264333195497439231.0));
        }

        [Fact]
        public static void TryConvertToCheckedDoubleTest()
        {
            Assert.Equal(double.NegativeInfinity, NumberBaseHelper<double>.CreateChecked<Complex>(double.NegativeInfinity));
            Assert.Equal(double.MinValue, NumberBaseHelper<double>.CreateChecked<Complex>(double.MinValue));

            Assert.Equal(-1.0, NumberBaseHelper<double>.CreateChecked<Complex>(-1.0));

            Assert.Equal(-2.2250738585072014E-308, NumberBaseHelper<double>.CreateChecked<Complex>(-2.2250738585072014E-308));
            Assert.Equal(-2.2250738585072009E-308, NumberBaseHelper<double>.CreateChecked<Complex>(-2.2250738585072009E-308));
            Assert.Equal(-double.Epsilon, NumberBaseHelper<double>.CreateChecked<Complex>(-double.Epsilon));
            Assert.Equal(-0.0, NumberBaseHelper<double>.CreateChecked<Complex>(-0.0));

            Assert.Equal(+0.0, NumberBaseHelper<double>.CreateChecked<Complex>(+0.0));
            Assert.Equal(double.Epsilon, NumberBaseHelper<double>.CreateChecked<Complex>(+double.Epsilon));
            Assert.Equal(2.2250738585072009E-308, NumberBaseHelper<double>.CreateChecked<Complex>(+2.2250738585072009E-308));
            Assert.Equal(2.2250738585072014E-308, NumberBaseHelper<double>.CreateChecked<Complex>(+2.2250738585072014E-308));

            Assert.Equal(1.0, NumberBaseHelper<double>.CreateChecked<Complex>(+1.0));

            Assert.Equal(double.MaxValue, NumberBaseHelper<double>.CreateChecked<Complex>(double.MaxValue));
            Assert.Equal(double.PositiveInfinity, NumberBaseHelper<double>.CreateChecked<Complex>(double.PositiveInfinity));

            Assert.Equal(double.NaN, NumberBaseHelper<double>.CreateChecked<Complex>(double.NaN));
        }

        [Fact]
        public static void TryConvertToCheckedHalfTest()
        {
            Assert.Equal(Half.NegativeInfinity, NumberBaseHelper<Half>.CreateChecked<Complex>(Half.NegativeInfinity));

            Assert.Equal(Half.MinValue, NumberBaseHelper<Half>.CreateChecked<Complex>(-65504.0));
            Assert.Equal(Half.NegativeOne, NumberBaseHelper<Half>.CreateChecked<Complex>(-1.0));

            Assert.Equal(-BitConverter.UInt16BitsToHalf(0x0400), NumberBaseHelper<Half>.CreateChecked<Complex>(-6.103515625E-05));
            Assert.Equal(-BitConverter.UInt16BitsToHalf(0x03FF), NumberBaseHelper<Half>.CreateChecked<Complex>(-6.097555160522461E-05));
            Assert.Equal(-Half.Epsilon, NumberBaseHelper<Half>.CreateChecked<Complex>(-5.960464477539063E-08));
            Assert.Equal(Half.NegativeZero, NumberBaseHelper<Half>.CreateChecked<Complex>(-0.0));

            Assert.Equal(Half.Zero, NumberBaseHelper<Half>.CreateChecked<Complex>(+0.0));
            Assert.Equal(Half.Epsilon, NumberBaseHelper<Half>.CreateChecked<Complex>(+5.960464477539063E-08));
            Assert.Equal(BitConverter.UInt16BitsToHalf(0x03FF), NumberBaseHelper<Half>.CreateChecked<Complex>(+6.097555160522461E-05));
            Assert.Equal(BitConverter.UInt16BitsToHalf(0x0400), NumberBaseHelper<Half>.CreateChecked<Complex>(+6.103515625E-05));

            Assert.Equal(Half.One, NumberBaseHelper<Half>.CreateChecked<Complex>(+1.0));
            Assert.Equal(Half.MaxValue, NumberBaseHelper<Half>.CreateChecked<Complex>(+65504.0));

            Assert.Equal(Half.PositiveInfinity, NumberBaseHelper<Half>.CreateChecked<Complex>(Half.PositiveInfinity));

            Assert.Equal(Half.NaN, NumberBaseHelper<Half>.CreateChecked<Complex>(Half.NaN));
        }

        [Fact]
        public static void TryConvertToCheckedInt16Test()
        {
            Assert.Equal(0x0000, NumberBaseHelper<short>.CreateChecked<Complex>(0.0));
            Assert.Equal(0x0001, NumberBaseHelper<short>.CreateChecked<Complex>(1.0));
            Assert.Equal(0x7FFF, NumberBaseHelper<short>.CreateChecked<Complex>(32767.0));
            Assert.Equal(unchecked((short)0x8000), NumberBaseHelper<short>.CreateChecked<Complex>(-32768.0));
            Assert.Equal(unchecked((short)0xFFFF), NumberBaseHelper<short>.CreateChecked<Complex>(-1.0));
        }

        [Fact]
        public static void TryConvertToCheckedInt32Test()
        {
            Assert.Equal(0x00000000, NumberBaseHelper<int>.CreateChecked<Complex>(0.0));
            Assert.Equal(0x00000001, NumberBaseHelper<int>.CreateChecked<Complex>(1.0));
            Assert.Equal(0x7FFFFFFF, NumberBaseHelper<int>.CreateChecked<Complex>(2147483647.0));
            Assert.Equal(unchecked((int)0x80000000), NumberBaseHelper<int>.CreateChecked<Complex>(-2147483648.0));
            Assert.Equal(unchecked((int)0xFFFFFFFF), NumberBaseHelper<int>.CreateChecked<Complex>(-1.0));
        }

        [Fact]
        public static void TryConvertToCheckedInt64Test()
        {
            Assert.Equal(0x0000_0000_0000_0000, NumberBaseHelper<long>.CreateChecked<Complex>(0.0));
            Assert.Equal(0x0000_0000_0000_0001, NumberBaseHelper<long>.CreateChecked<Complex>(1.0));
            Assert.Equal(0x7FFF_FFFF_FFFF_FC00, NumberBaseHelper<long>.CreateChecked<Complex>(+9223372036854774784.0));
            Assert.Equal(unchecked(unchecked((long)0x8000_0000_0000_0000)), NumberBaseHelper<long>.CreateChecked<Complex>(-9223372036854775808.0));
            Assert.Equal(unchecked(unchecked((long)0xFFFF_FFFF_FFFF_FFFF)), NumberBaseHelper<long>.CreateChecked<Complex>(-1.0));
        }

        [Fact]
        public static void TryConvertToCheckedInt128Test()
        {
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), NumberBaseHelper<Int128>.CreateChecked<Complex>(0.0));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), NumberBaseHelper<Int128>.CreateChecked<Complex>(1.0));
            Assert.Equal(new Int128(0x7FFF_FFFF_FFFF_FC00, 0x0000_0000_0000_0000), NumberBaseHelper<Int128>.CreateChecked<Complex>(+170141183460469212842221372237303250944.0));
            Assert.Equal(new Int128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000), NumberBaseHelper<Int128>.CreateChecked<Complex>(-170141183460469231731687303715884105728.0));
            Assert.Equal(new Int128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), NumberBaseHelper<Int128>.CreateChecked<Complex>(-1.0));
        }

        [Fact]
        public static void TryConvertToCheckedIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0x0000_0000_0000_0000), NumberBaseHelper<nint>.CreateChecked<Complex>(0.0));
                Assert.Equal(unchecked((nint)0x0000_0000_0000_0001), NumberBaseHelper<nint>.CreateChecked<Complex>(1.0));
                Assert.Equal(unchecked((nint)0x7FFF_FFFF_FFFF_FC00), NumberBaseHelper<nint>.CreateChecked<Complex>(+9223372036854774784.0));
                Assert.Equal(unchecked((nint)0x8000_0000_0000_0000), NumberBaseHelper<nint>.CreateChecked<Complex>(-9223372036854775808.0));
                Assert.Equal(unchecked((nint)0xFFFF_FFFF_FFFF_FFFF), NumberBaseHelper<nint>.CreateChecked<Complex>(-1.0));
            }
            else
            {
                Assert.Equal((nint)0x00000000, NumberBaseHelper<nint>.CreateChecked<Complex>(0.0));
                Assert.Equal((nint)0x00000001, NumberBaseHelper<nint>.CreateChecked<Complex>(1.0));
                Assert.Equal((nint)0x7FFFFFFF, NumberBaseHelper<nint>.CreateChecked<Complex>(2147483647.0));
                Assert.Equal(unchecked((nint)0x80000000), NumberBaseHelper<nint>.CreateChecked<Complex>(-2147483648.0));
                Assert.Equal(unchecked((nint)0xFFFFFFFF), NumberBaseHelper<nint>.CreateChecked<Complex>(-1.0));
            }
        }

        [Fact]
        public static void TryConvertToCheckedSByteTest()
        {
            Assert.Equal(0x00, NumberBaseHelper<sbyte>.CreateChecked<Complex>(0.0));
            Assert.Equal(0x01, NumberBaseHelper<sbyte>.CreateChecked<Complex>(1.0));
            Assert.Equal(0x7F, NumberBaseHelper<sbyte>.CreateChecked<Complex>(127.0));
            Assert.Equal(unchecked((sbyte)0x80), NumberBaseHelper<sbyte>.CreateChecked<Complex>(-128.0));
            Assert.Equal(unchecked((sbyte)0xFF), NumberBaseHelper<sbyte>.CreateChecked<Complex>(-1.0));
        }

        [Fact]
        public static void TryConvertToCheckedSingleTest()
        {
            Assert.Equal(float.NegativeInfinity, NumberBaseHelper<float>.CreateChecked<Complex>(float.NegativeInfinity));

            Assert.Equal(float.MinValue, NumberBaseHelper<float>.CreateChecked<Complex>(-3.4028234663852886E+38));
            Assert.Equal(-1.0f, NumberBaseHelper<float>.CreateChecked<Complex>(-1.0));

            Assert.Equal(-1.17549435E-38f, NumberBaseHelper<float>.CreateChecked<Complex>(-1.1754943508222875E-38));
            Assert.Equal(-1.17549421E-38f, NumberBaseHelper<float>.CreateChecked<Complex>(-1.1754942106924411E-38));
            Assert.Equal(-float.Epsilon, NumberBaseHelper<float>.CreateChecked<Complex>(-1.401298464324817E-45));
            Assert.Equal(-0.0f, NumberBaseHelper<float>.CreateChecked<Complex>(-0.0));

            Assert.Equal(+0.0f, NumberBaseHelper<float>.CreateChecked<Complex>(+0.0));
            Assert.Equal(float.Epsilon, NumberBaseHelper<float>.CreateChecked<Complex>(+1.401298464324817E-45));
            Assert.Equal(1.17549421E-38f, NumberBaseHelper<float>.CreateChecked<Complex>(+1.1754942106924411E-38));
            Assert.Equal(1.17549435E-38f, NumberBaseHelper<float>.CreateChecked<Complex>(+1.1754943508222875E-38));

            Assert.Equal(1.0f, NumberBaseHelper<float>.CreateChecked<Complex>(+1.0));
            Assert.Equal(float.MaxValue, NumberBaseHelper<float>.CreateChecked<Complex>(+3.4028234663852886E+38));

            Assert.Equal(float.PositiveInfinity, NumberBaseHelper<float>.CreateChecked<Complex>(float.PositiveInfinity));

            Assert.Equal(float.NaN, NumberBaseHelper<float>.CreateChecked<Complex>(float.NaN));
        }

        [Fact]
        public static void TryConvertToCheckedUInt16Test()
        {
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateChecked<Complex>(0.0));
            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.CreateChecked<Complex>(1.0));
            Assert.Equal((ushort)0x7FFF, NumberBaseHelper<ushort>.CreateChecked<Complex>(32767.0));
            Assert.Equal((ushort)0x8000, NumberBaseHelper<ushort>.CreateChecked<Complex>(32768.0));
            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateChecked<Complex>(65535.0));
        }

        [Fact]
        public static void TryConvertToCheckedUInt32Test()
        {
            Assert.Equal((uint)0x00000000, NumberBaseHelper<uint>.CreateChecked<Complex>(0.0));
            Assert.Equal((uint)0x00000001, NumberBaseHelper<uint>.CreateChecked<Complex>(1.0));
            Assert.Equal((uint)0x7FFFFFFF, NumberBaseHelper<uint>.CreateChecked<Complex>(2147483647.0));
            Assert.Equal((uint)0x80000000, NumberBaseHelper<uint>.CreateChecked<Complex>(2147483648.0));
            Assert.Equal((uint)0xFFFFFFFF, NumberBaseHelper<uint>.CreateChecked<Complex>(4294967295.0));
        }

        [Fact]
        public static void TryConvertToCheckedUInt64Test()
        {
            Assert.Equal((ulong)0x0000_0000_0000_0000, NumberBaseHelper<ulong>.CreateChecked<Complex>(0.0));
            Assert.Equal((ulong)0x0000_0000_0000_0001, NumberBaseHelper<ulong>.CreateChecked<Complex>(1.0));
            Assert.Equal((ulong)0x8000_0000_0000_0000, NumberBaseHelper<ulong>.CreateChecked<Complex>(9223372036854775808.0));
            Assert.Equal((ulong)0xFFFF_FFFF_FFFF_F800, NumberBaseHelper<ulong>.CreateChecked<Complex>(18446744073709549568.0));
        }

        [Fact]
        public static void TryConvertToCheckedUInt128Test()
        {
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), NumberBaseHelper<UInt128>.CreateChecked<Complex>(0.0));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), NumberBaseHelper<UInt128>.CreateChecked<Complex>(1.0));
            Assert.Equal(new UInt128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000), NumberBaseHelper<UInt128>.CreateChecked<Complex>(170141183460469231731687303715884105728.0));
            Assert.Equal(new UInt128(0xFFFF_FFFF_FFFF_F800, 0x0000_0000_0000_0000), NumberBaseHelper<UInt128>.CreateChecked<Complex>(340282366920938425684442744474606501888.0));
        }

        [Fact]
        [SkipOnMono("https://github.com/dotnet/runtime/issues/69794")]
        public static void TryConvertToCheckedUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nuint)0x0000_0000_0000_0000), NumberBaseHelper<nuint>.CreateChecked<Complex>(0.0));
                Assert.Equal(unchecked((nuint)0x0000_0000_0000_0001), NumberBaseHelper<nuint>.CreateChecked<Complex>(1.0));
                Assert.Equal(unchecked((nuint)0x8000_0000_0000_0000), NumberBaseHelper<nuint>.CreateChecked<Complex>(9223372036854775808.0));
                Assert.Equal(unchecked((nuint)0xFFFF_FFFF_FFFF_F800), NumberBaseHelper<nuint>.CreateChecked<Complex>(18446744073709549568.0));
            }
            else
            {
                Assert.Equal((nuint)0x0000_0000, NumberBaseHelper<nuint>.CreateChecked<Complex>(0.0));
                Assert.Equal((nuint)0x0000_0001, NumberBaseHelper<nuint>.CreateChecked<Complex>(1.0));
                Assert.Equal((nuint)0x8000_0000, NumberBaseHelper<nuint>.CreateChecked<Complex>(2147483648.0));
                Assert.Equal((nuint)0xFFFF_FFFF, NumberBaseHelper<nuint>.CreateChecked<Complex>(4294967295.0));
            }
        }

        [Fact]
        public static void TryConvertToSaturatingByteTest()
        {
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateSaturating<Complex>(0.0));
            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.CreateSaturating<Complex>(1.0));
            Assert.Equal((byte)0x7F, NumberBaseHelper<byte>.CreateSaturating<Complex>(127.0));
            Assert.Equal((byte)0x80, NumberBaseHelper<byte>.CreateSaturating<Complex>(128.0));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateSaturating<Complex>(255.0));
        }

        [Fact]
        public static void TryConvertToSaturatingCharTest()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateSaturating<Complex>(0.0));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateSaturating<Complex>(1.0));
            Assert.Equal((char)0x7FFF, NumberBaseHelper<char>.CreateSaturating<Complex>(32767.0));
            Assert.Equal((char)0x8000, NumberBaseHelper<char>.CreateSaturating<Complex>(32768.0));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateSaturating<Complex>(65535.0));
        }

        [Fact]
        public static void TryConvertToSaturatingDecimalTest()
        {
            Assert.Equal(decimal.MinValue, NumberBaseHelper<decimal>.CreateSaturating<Complex>(-79228162514264337593543950335.0));
            Assert.Equal(-1.0m, NumberBaseHelper<decimal>.CreateSaturating<Complex>(-1.0));
            Assert.Equal(-0.0m, NumberBaseHelper<decimal>.CreateSaturating<Complex>(-0.0));
            Assert.Equal(+0.0m, NumberBaseHelper<decimal>.CreateSaturating<Complex>(+0.0));
            Assert.Equal(+1.0m, NumberBaseHelper<decimal>.CreateSaturating<Complex>(+1.0));
            Assert.Equal(decimal.MaxValue, NumberBaseHelper<decimal>.CreateSaturating<Complex>(+79228162514264337593543950335.0));
        }

        [Fact]
        public static void TryConvertToSaturatingDoubleTest()
        {
            Assert.Equal(double.NegativeInfinity, NumberBaseHelper<double>.CreateSaturating<Complex>(double.NegativeInfinity));
            Assert.Equal(double.MinValue, NumberBaseHelper<double>.CreateSaturating<Complex>(double.MinValue));

            Assert.Equal(-1.0, NumberBaseHelper<double>.CreateSaturating<Complex>(-1.0));

            Assert.Equal(-2.2250738585072014E-308, NumberBaseHelper<double>.CreateSaturating<Complex>(-2.2250738585072014E-308));
            Assert.Equal(-2.2250738585072009E-308, NumberBaseHelper<double>.CreateSaturating<Complex>(-2.2250738585072009E-308));
            Assert.Equal(-double.Epsilon, NumberBaseHelper<double>.CreateSaturating<Complex>(-double.Epsilon));
            Assert.Equal(-0.0, NumberBaseHelper<double>.CreateSaturating<Complex>(-0.0));

            Assert.Equal(+0.0, NumberBaseHelper<double>.CreateSaturating<Complex>(+0.0));
            Assert.Equal(double.Epsilon, NumberBaseHelper<double>.CreateSaturating<Complex>(+double.Epsilon));
            Assert.Equal(2.2250738585072009E-308, NumberBaseHelper<double>.CreateSaturating<Complex>(+2.2250738585072009E-308));
            Assert.Equal(2.2250738585072014E-308, NumberBaseHelper<double>.CreateSaturating<Complex>(+2.2250738585072014E-308));

            Assert.Equal(1.0, NumberBaseHelper<double>.CreateSaturating<Complex>(+1.0));

            Assert.Equal(double.MaxValue, NumberBaseHelper<double>.CreateSaturating<Complex>(double.MaxValue));
            Assert.Equal(double.PositiveInfinity, NumberBaseHelper<double>.CreateSaturating<Complex>(double.PositiveInfinity));

            Assert.Equal(double.NaN, NumberBaseHelper<double>.CreateSaturating<Complex>(double.NaN));
        }

        [Fact]
        public static void TryConvertToSaturatingHalfTest()
        {
            Assert.Equal(Half.NegativeInfinity, NumberBaseHelper<Half>.CreateSaturating<Complex>(Half.NegativeInfinity));

            Assert.Equal(Half.MinValue, NumberBaseHelper<Half>.CreateSaturating<Complex>(-65504.0));
            Assert.Equal(Half.NegativeOne, NumberBaseHelper<Half>.CreateSaturating<Complex>(-1.0));

            Assert.Equal(-BitConverter.UInt16BitsToHalf(0x0400), NumberBaseHelper<Half>.CreateSaturating<Complex>(-6.103515625E-05));
            Assert.Equal(-BitConverter.UInt16BitsToHalf(0x03FF), NumberBaseHelper<Half>.CreateSaturating<Complex>(-6.097555160522461E-05));
            Assert.Equal(-Half.Epsilon, NumberBaseHelper<Half>.CreateSaturating<Complex>(-5.960464477539063E-08));
            Assert.Equal(Half.NegativeZero, NumberBaseHelper<Half>.CreateSaturating<Complex>(-0.0));

            Assert.Equal(Half.Zero, NumberBaseHelper<Half>.CreateSaturating<Complex>(+0.0));
            Assert.Equal(Half.Epsilon, NumberBaseHelper<Half>.CreateSaturating<Complex>(+5.960464477539063E-08));
            Assert.Equal(BitConverter.UInt16BitsToHalf(0x03FF), NumberBaseHelper<Half>.CreateSaturating<Complex>(+6.097555160522461E-05));
            Assert.Equal(BitConverter.UInt16BitsToHalf(0x0400), NumberBaseHelper<Half>.CreateSaturating<Complex>(+6.103515625E-05));

            Assert.Equal(Half.One, NumberBaseHelper<Half>.CreateSaturating<Complex>(+1.0));
            Assert.Equal(Half.MaxValue, NumberBaseHelper<Half>.CreateSaturating<Complex>(+65504.0));

            Assert.Equal(Half.PositiveInfinity, NumberBaseHelper<Half>.CreateSaturating<Complex>(Half.PositiveInfinity));

            Assert.Equal(Half.NaN, NumberBaseHelper<Half>.CreateSaturating<Complex>(Half.NaN));
        }

        [Fact]
        public static void TryConvertToSaturatingInt16Test()
        {
            Assert.Equal(0x0000, NumberBaseHelper<short>.CreateSaturating<Complex>(0.0));
            Assert.Equal(0x0001, NumberBaseHelper<short>.CreateSaturating<Complex>(1.0));
            Assert.Equal(0x7FFF, NumberBaseHelper<short>.CreateSaturating<Complex>(32767.0));
            Assert.Equal(unchecked((short)0x8000), NumberBaseHelper<short>.CreateSaturating<Complex>(-32768.0));
            Assert.Equal(unchecked((short)0xFFFF), NumberBaseHelper<short>.CreateSaturating<Complex>(-1.0));
        }

        [Fact]
        public static void TryConvertToSaturatingInt32Test()
        {
            Assert.Equal(0x00000000, NumberBaseHelper<int>.CreateSaturating<Complex>(0.0));
            Assert.Equal(0x00000001, NumberBaseHelper<int>.CreateSaturating<Complex>(1.0));
            Assert.Equal(0x7FFFFFFF, NumberBaseHelper<int>.CreateSaturating<Complex>(2147483647.0));
            Assert.Equal(unchecked((int)0x80000000), NumberBaseHelper<int>.CreateSaturating<Complex>(-2147483648.0));
            Assert.Equal(unchecked((int)0xFFFFFFFF), NumberBaseHelper<int>.CreateSaturating<Complex>(-1.0));
        }

        [Fact]
        public static void TryConvertToSaturatingInt64Test()
        {
            Assert.Equal(0x0000000000000000, NumberBaseHelper<long>.CreateSaturating<Complex>(0.0));
            Assert.Equal(0x0000000000000001, NumberBaseHelper<long>.CreateSaturating<Complex>(1.0));
            Assert.Equal(0x7FFFFFFFFFFFFFFF, NumberBaseHelper<long>.CreateSaturating<Complex>(9223372036854775807.0));
            Assert.Equal(unchecked(unchecked((long)0x8000000000000000)), NumberBaseHelper<long>.CreateSaturating<Complex>(-9223372036854775808.0));
            Assert.Equal(unchecked(unchecked((long)0xFFFFFFFFFFFFFFFF)), NumberBaseHelper<long>.CreateSaturating<Complex>(-1.0));
        }

        [Fact]
        public static void TryConvertToSaturatingInt128Test()
        {
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), NumberBaseHelper<Int128>.CreateSaturating<Complex>(0.0));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), NumberBaseHelper<Int128>.CreateSaturating<Complex>(1.0));
            Assert.Equal(new Int128(0x7FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), NumberBaseHelper<Int128>.CreateSaturating<Complex>(170141183460469231731687303715884105727.0));
            Assert.Equal(new Int128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000), NumberBaseHelper<Int128>.CreateSaturating<Complex>(-170141183460469231731687303715884105728.0));
            Assert.Equal(new Int128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), NumberBaseHelper<Int128>.CreateSaturating<Complex>(-1.0));
        }

        [Fact]
        public static void TryConvertToSaturatingIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0x0000000000000000), NumberBaseHelper<nint>.CreateSaturating<Complex>(0.0));
                Assert.Equal(unchecked((nint)0x0000000000000001), NumberBaseHelper<nint>.CreateSaturating<Complex>(1.0));
                Assert.Equal(unchecked((nint)0x7FFFFFFFFFFFFFFF), NumberBaseHelper<nint>.CreateSaturating<Complex>(9223372036854775807.0));
                Assert.Equal(unchecked((nint)0x8000000000000000), NumberBaseHelper<nint>.CreateSaturating<Complex>(-9223372036854775808.0));
                Assert.Equal(unchecked((nint)0xFFFFFFFFFFFFFFFF), NumberBaseHelper<nint>.CreateSaturating<Complex>(-1.0));
            }
            else
            {
                Assert.Equal((nint)0x00000000, NumberBaseHelper<nint>.CreateSaturating<Complex>(0.0));
                Assert.Equal((nint)0x00000001, NumberBaseHelper<nint>.CreateSaturating<Complex>(1.0));
                Assert.Equal((nint)0x7FFFFFFF, NumberBaseHelper<nint>.CreateSaturating<Complex>(2147483647.0));
                Assert.Equal(unchecked((nint)0x80000000), NumberBaseHelper<nint>.CreateSaturating<Complex>(-2147483648.0));
                Assert.Equal(unchecked((nint)0xFFFFFFFF), NumberBaseHelper<nint>.CreateSaturating<Complex>(-1.0));
            }
        }

        [Fact]
        public static void TryConvertToSaturatingSByteTest()
        {
            Assert.Equal(0x00, NumberBaseHelper<sbyte>.CreateSaturating<Complex>(0.0));
            Assert.Equal(0x01, NumberBaseHelper<sbyte>.CreateSaturating<Complex>(1.0));
            Assert.Equal(0x7F, NumberBaseHelper<sbyte>.CreateSaturating<Complex>(127.0));
            Assert.Equal(unchecked((sbyte)0x80), NumberBaseHelper<sbyte>.CreateSaturating<Complex>(-128.0));
            Assert.Equal(unchecked((sbyte)0xFF), NumberBaseHelper<sbyte>.CreateSaturating<Complex>(-1.0));
        }

        [Fact]
        public static void TryConvertToSaturatingSingleTest()
        {
            Assert.Equal(float.NegativeInfinity, NumberBaseHelper<float>.CreateSaturating<Complex>(float.NegativeInfinity));

            Assert.Equal(float.MinValue, NumberBaseHelper<float>.CreateSaturating<Complex>(-3.4028234663852886E+38));
            Assert.Equal(-1.0f, NumberBaseHelper<float>.CreateSaturating<Complex>(-1.0));

            Assert.Equal(-1.17549435E-38f, NumberBaseHelper<float>.CreateSaturating<Complex>(-1.1754943508222875E-38));
            Assert.Equal(-1.17549421E-38f, NumberBaseHelper<float>.CreateSaturating<Complex>(-1.1754942106924411E-38));
            Assert.Equal(-float.Epsilon, NumberBaseHelper<float>.CreateSaturating<Complex>(-1.401298464324817E-45));
            Assert.Equal(-0.0f, NumberBaseHelper<float>.CreateSaturating<Complex>(-0.0));

            Assert.Equal(+0.0f, NumberBaseHelper<float>.CreateSaturating<Complex>(+0.0));
            Assert.Equal(float.Epsilon, NumberBaseHelper<float>.CreateSaturating<Complex>(+1.401298464324817E-45));
            Assert.Equal(1.17549421E-38f, NumberBaseHelper<float>.CreateSaturating<Complex>(+1.1754942106924411E-38));
            Assert.Equal(1.17549435E-38f, NumberBaseHelper<float>.CreateSaturating<Complex>(+1.1754943508222875E-38));

            Assert.Equal(1.0f, NumberBaseHelper<float>.CreateSaturating<Complex>(+1.0));
            Assert.Equal(float.MaxValue, NumberBaseHelper<float>.CreateSaturating<Complex>(+3.4028234663852886E+38));

            Assert.Equal(float.PositiveInfinity, NumberBaseHelper<float>.CreateSaturating<Complex>(float.PositiveInfinity));

            Assert.Equal(float.NaN, NumberBaseHelper<float>.CreateSaturating<Complex>(float.NaN));
        }

        [Fact]
        public static void TryConvertToSaturatingUInt16Test()
        {
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateSaturating<Complex>(0.0));
            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.CreateSaturating<Complex>(1.0));
            Assert.Equal((ushort)0x7FFF, NumberBaseHelper<ushort>.CreateSaturating<Complex>(32767.0));
            Assert.Equal((ushort)0x8000, NumberBaseHelper<ushort>.CreateSaturating<Complex>(32768.0));
            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateSaturating<Complex>(65535.0));
        }

        [Fact]
        public static void TryConvertToSaturatingUInt32Test()
        {
            Assert.Equal((uint)0x00000000, NumberBaseHelper<uint>.CreateSaturating<Complex>(0.0));
            Assert.Equal((uint)0x00000001, NumberBaseHelper<uint>.CreateSaturating<Complex>(1.0));
            Assert.Equal((uint)0x7FFFFFFF, NumberBaseHelper<uint>.CreateSaturating<Complex>(2147483647.0));
            Assert.Equal((uint)0x80000000, NumberBaseHelper<uint>.CreateSaturating<Complex>(2147483648.0));
            Assert.Equal((uint)0xFFFFFFFF, NumberBaseHelper<uint>.CreateSaturating<Complex>(4294967295.0));
        }

        [Fact]
        public static void TryConvertToSaturatingUInt64Test()
        {
            Assert.Equal((ulong)0x0000000000000000, NumberBaseHelper<ulong>.CreateSaturating<Complex>(0.0));
            Assert.Equal((ulong)0x0000000000000001, NumberBaseHelper<ulong>.CreateSaturating<Complex>(1.0));
            Assert.Equal((ulong)0x8000000000000000, NumberBaseHelper<ulong>.CreateSaturating<Complex>(9223372036854775808.0));
            Assert.Equal((ulong)0xFFFFFFFFFFFFFFFF, NumberBaseHelper<ulong>.CreateSaturating<Complex>(18446744073709551615.0));
        }

        [Fact]
        public static void TryConvertToSaturatingUInt128Test()
        {
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), NumberBaseHelper<UInt128>.CreateSaturating<Complex>(0.0));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), NumberBaseHelper<UInt128>.CreateSaturating<Complex>(1.0));
            Assert.Equal(new UInt128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000), NumberBaseHelper<UInt128>.CreateSaturating<Complex>(170141183460469231731687303715884105728.0));
            Assert.Equal(new UInt128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), NumberBaseHelper<UInt128>.CreateSaturating<Complex>(340282366920938463463374607431768211455.0));
        }

        [Fact]
        public static void TryConvertToSaturatingUIntPtrTest()
        {
            // if (Environment.Is64BitProcess)
            // {
            //     Assert.Equal(unchecked((nuint)0x0000_0000_0000_0000), NumberBaseHelper<nuint>.CreateSaturating<Complex>(0.0));
            //     Assert.Equal(unchecked((nuint)0x0000_0000_0000_0001), NumberBaseHelper<nuint>.CreateSaturating<Complex>(1.0));
            //     Assert.Equal(unchecked((nuint)0x8000_0000_0000_0000), NumberBaseHelper<nuint>.CreateSaturating<Complex>(9223372036854775808.0));
            //     Assert.Equal(unchecked((nuint)0xFFFF_FFFF_FFFF_FFFF), NumberBaseHelper<nuint>.CreateSaturating<Complex>(18446744073709551615.0));
            // }
            // else
            // {
            //     Assert.Equal((nuint)0x0000_0000, NumberBaseHelper<nuint>.CreateSaturating<Complex>(0.0));
            //     Assert.Equal((nuint)0x0000_0001, NumberBaseHelper<nuint>.CreateSaturating<Complex>(1.0));
            //     Assert.Equal((nuint)0x8000_0000, NumberBaseHelper<nuint>.CreateSaturating<Complex>(2147483648.0));
            //     Assert.Equal((nuint)0xFFFF_FFFF, NumberBaseHelper<nuint>.CreateSaturating<Complex>(4294967295.0));
            // }
        }

        [Fact]
        public static void TryConvertToTruncatingByteTest()
        {
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.CreateTruncating<Complex>(0.0));
            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.CreateTruncating<Complex>(1.0));
            Assert.Equal((byte)0x7F, NumberBaseHelper<byte>.CreateTruncating<Complex>(127.0));
            Assert.Equal((byte)0x80, NumberBaseHelper<byte>.CreateTruncating<Complex>(128.0));
            Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateTruncating<Complex>(255.0));
        }

        [Fact]
        public static void TryConvertToTruncatingCharTest()
        {
            Assert.Equal((char)0x0000, NumberBaseHelper<char>.CreateTruncating<Complex>(0.0));
            Assert.Equal((char)0x0001, NumberBaseHelper<char>.CreateTruncating<Complex>(1.0));
            Assert.Equal((char)0x7FFF, NumberBaseHelper<char>.CreateTruncating<Complex>(32767.0));
            Assert.Equal((char)0x8000, NumberBaseHelper<char>.CreateTruncating<Complex>(32768.0));
            Assert.Equal((char)0xFFFF, NumberBaseHelper<char>.CreateTruncating<Complex>(65535.0));
        }

        [Fact]
        public static void TryConvertToTruncatingDecimalTest()
        {
            Assert.Equal(decimal.MinValue, NumberBaseHelper<decimal>.CreateTruncating<Complex>(-79228162514264337593543950335.0));
            Assert.Equal(-1.0m, NumberBaseHelper<decimal>.CreateTruncating<Complex>(-1.0));
            Assert.Equal(-0.0m, NumberBaseHelper<decimal>.CreateTruncating<Complex>(-0.0));
            Assert.Equal(+0.0m, NumberBaseHelper<decimal>.CreateTruncating<Complex>(+0.0));
            Assert.Equal(+1.0m, NumberBaseHelper<decimal>.CreateTruncating<Complex>(+1.0));
            Assert.Equal(decimal.MaxValue, NumberBaseHelper<decimal>.CreateTruncating<Complex>(+79228162514264337593543950335.0));
        }

        [Fact]
        public static void TryConvertToTruncatingDoubleTest()
        {
            Assert.Equal(double.NegativeInfinity, NumberBaseHelper<double>.CreateTruncating<Complex>(double.NegativeInfinity));
            Assert.Equal(double.MinValue, NumberBaseHelper<double>.CreateTruncating<Complex>(double.MinValue));

            Assert.Equal(-1.0, NumberBaseHelper<double>.CreateTruncating<Complex>(-1.0));

            Assert.Equal(-2.2250738585072014E-308, NumberBaseHelper<double>.CreateTruncating<Complex>(-2.2250738585072014E-308));
            Assert.Equal(-2.2250738585072009E-308, NumberBaseHelper<double>.CreateTruncating<Complex>(-2.2250738585072009E-308));
            Assert.Equal(-double.Epsilon, NumberBaseHelper<double>.CreateTruncating<Complex>(-double.Epsilon));
            Assert.Equal(-0.0, NumberBaseHelper<double>.CreateTruncating<Complex>(-0.0));

            Assert.Equal(+0.0, NumberBaseHelper<double>.CreateTruncating<Complex>(+0.0));
            Assert.Equal(double.Epsilon, NumberBaseHelper<double>.CreateTruncating<Complex>(+double.Epsilon));
            Assert.Equal(2.2250738585072009E-308, NumberBaseHelper<double>.CreateTruncating<Complex>(+2.2250738585072009E-308));
            Assert.Equal(2.2250738585072014E-308, NumberBaseHelper<double>.CreateTruncating<Complex>(+2.2250738585072014E-308));

            Assert.Equal(1.0, NumberBaseHelper<double>.CreateTruncating<Complex>(+1.0));

            Assert.Equal(double.MaxValue, NumberBaseHelper<double>.CreateTruncating<Complex>(double.MaxValue));
            Assert.Equal(double.PositiveInfinity, NumberBaseHelper<double>.CreateTruncating<Complex>(double.PositiveInfinity));

            Assert.Equal(double.NaN, NumberBaseHelper<double>.CreateTruncating<Complex>(double.NaN));
        }

        [Fact]
        public static void TryConvertToTruncatingHalfTest()
        {
            Assert.Equal(Half.NegativeInfinity, NumberBaseHelper<Half>.CreateTruncating<Complex>(Half.NegativeInfinity));

            Assert.Equal(Half.MinValue, NumberBaseHelper<Half>.CreateTruncating<Complex>(-65504.0));
            Assert.Equal(Half.NegativeOne, NumberBaseHelper<Half>.CreateTruncating<Complex>(-1.0));

            Assert.Equal(-BitConverter.UInt16BitsToHalf(0x0400), NumberBaseHelper<Half>.CreateTruncating<Complex>(-6.103515625E-05));
            Assert.Equal(-BitConverter.UInt16BitsToHalf(0x03FF), NumberBaseHelper<Half>.CreateTruncating<Complex>(-6.097555160522461E-05));
            Assert.Equal(-Half.Epsilon, NumberBaseHelper<Half>.CreateTruncating<Complex>(-5.960464477539063E-08));
            Assert.Equal(Half.NegativeZero, NumberBaseHelper<Half>.CreateTruncating<Complex>(-0.0));

            Assert.Equal(Half.Zero, NumberBaseHelper<Half>.CreateTruncating<Complex>(+0.0));
            Assert.Equal(Half.Epsilon, NumberBaseHelper<Half>.CreateTruncating<Complex>(+5.960464477539063E-08));
            Assert.Equal(BitConverter.UInt16BitsToHalf(0x03FF), NumberBaseHelper<Half>.CreateTruncating<Complex>(+6.097555160522461E-05));
            Assert.Equal(BitConverter.UInt16BitsToHalf(0x0400), NumberBaseHelper<Half>.CreateTruncating<Complex>(+6.103515625E-05));

            Assert.Equal(Half.One, NumberBaseHelper<Half>.CreateTruncating<Complex>(+1.0));
            Assert.Equal(Half.MaxValue, NumberBaseHelper<Half>.CreateTruncating<Complex>(+65504.0));

            Assert.Equal(Half.PositiveInfinity, NumberBaseHelper<Half>.CreateTruncating<Complex>(Half.PositiveInfinity));

            Assert.Equal(Half.NaN, NumberBaseHelper<Half>.CreateTruncating<Complex>(Half.NaN));
        }

        [Fact]
        public static void TryConvertToTruncatingInt16Test()
        {
            Assert.Equal(0x0000, NumberBaseHelper<short>.CreateTruncating<Complex>(0.0));
            Assert.Equal(0x0001, NumberBaseHelper<short>.CreateTruncating<Complex>(1.0));
            Assert.Equal(0x7FFF, NumberBaseHelper<short>.CreateTruncating<Complex>(32767.0));
            Assert.Equal(unchecked((short)0x8000), NumberBaseHelper<short>.CreateTruncating<Complex>(-32768.0));
            Assert.Equal(unchecked((short)0xFFFF), NumberBaseHelper<short>.CreateTruncating<Complex>(-1.0));
        }

        [Fact]
        public static void TryConvertToTruncatingInt32Test()
        {
            Assert.Equal(0x00000000, NumberBaseHelper<int>.CreateTruncating<Complex>(0.0));
            Assert.Equal(0x00000001, NumberBaseHelper<int>.CreateTruncating<Complex>(1.0));
            Assert.Equal(0x7FFFFFFF, NumberBaseHelper<int>.CreateTruncating<Complex>(2147483647.0));
            Assert.Equal(unchecked((int)0x80000000), NumberBaseHelper<int>.CreateTruncating<Complex>(-2147483648.0));
            Assert.Equal(unchecked((int)0xFFFFFFFF), NumberBaseHelper<int>.CreateTruncating<Complex>(-1.0));
        }

        [Fact]
        public static void TryConvertToTruncatingInt64Test()
        {
            Assert.Equal(0x0000000000000000, NumberBaseHelper<long>.CreateTruncating<Complex>(0.0));
            Assert.Equal(0x0000000000000001, NumberBaseHelper<long>.CreateTruncating<Complex>(1.0));
            Assert.Equal(unchecked(unchecked((long)0x8000000000000000)), NumberBaseHelper<long>.CreateTruncating<Complex>(-9223372036854775808.0));
            Assert.Equal(unchecked(unchecked((long)0xFFFFFFFFFFFFFFFF)), NumberBaseHelper<long>.CreateTruncating<Complex>(-1.0));
        }

        [Fact]
        public static void TryConvertToTruncatingInt128Test()
        {
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), NumberBaseHelper<Int128>.CreateTruncating<Complex>(0.0));
            Assert.Equal(new Int128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), NumberBaseHelper<Int128>.CreateTruncating<Complex>(1.0));
            Assert.Equal(new Int128(0x7FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), NumberBaseHelper<Int128>.CreateTruncating<Complex>(170141183460469231731687303715884105727.0));
            Assert.Equal(new Int128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000), NumberBaseHelper<Int128>.CreateTruncating<Complex>(-170141183460469231731687303715884105728.0));
            Assert.Equal(new Int128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), NumberBaseHelper<Int128>.CreateTruncating<Complex>(-1.0));
        }

        [Fact]
        public static void TryConvertToTruncatingIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(unchecked((nint)0x0000000000000000), NumberBaseHelper<nint>.CreateTruncating<Complex>(0.0));
                Assert.Equal(unchecked((nint)0x0000000000000001), NumberBaseHelper<nint>.CreateTruncating<Complex>(1.0));
                Assert.Equal(unchecked((nint)0x7FFFFFFFFFFFFFFF), NumberBaseHelper<nint>.CreateTruncating<Complex>(9223372036854775807.0));
                Assert.Equal(unchecked((nint)0x8000000000000000), NumberBaseHelper<nint>.CreateTruncating<Complex>(-9223372036854775808.0));
                Assert.Equal(unchecked((nint)0xFFFFFFFFFFFFFFFF), NumberBaseHelper<nint>.CreateTruncating<Complex>(-1.0));
            }
            else
            {
                Assert.Equal((nint)0x00000000, NumberBaseHelper<nint>.CreateTruncating<Complex>(0.0));
                Assert.Equal((nint)0x00000001, NumberBaseHelper<nint>.CreateTruncating<Complex>(1.0));
                Assert.Equal((nint)0x7FFFFFFF, NumberBaseHelper<nint>.CreateTruncating<Complex>(2147483647.0));
                Assert.Equal(unchecked((nint)0x80000000), NumberBaseHelper<nint>.CreateTruncating<Complex>(-2147483648.0));
                Assert.Equal(unchecked((nint)0xFFFFFFFF), NumberBaseHelper<nint>.CreateTruncating<Complex>(-1.0));
            }
        }

        [Fact]
        public static void TryConvertToTruncatingSByteTest()
        {
            Assert.Equal(0x00, NumberBaseHelper<sbyte>.CreateTruncating<Complex>(0.0));
            Assert.Equal(0x01, NumberBaseHelper<sbyte>.CreateTruncating<Complex>(1.0));
            Assert.Equal(0x7F, NumberBaseHelper<sbyte>.CreateTruncating<Complex>(127.0));
            Assert.Equal(unchecked((sbyte)0x80), NumberBaseHelper<sbyte>.CreateTruncating<Complex>(-128.0));
            Assert.Equal(unchecked((sbyte)0xFF), NumberBaseHelper<sbyte>.CreateTruncating<Complex>(-1.0));
        }

        [Fact]
        public static void TryConvertToTruncatingSingleTest()
        {
            Assert.Equal(float.NegativeInfinity, NumberBaseHelper<float>.CreateTruncating<Complex>(float.NegativeInfinity));

            Assert.Equal(float.MinValue, NumberBaseHelper<float>.CreateTruncating<Complex>(-3.4028234663852886E+38));
            Assert.Equal(-1.0f, NumberBaseHelper<float>.CreateTruncating<Complex>(-1.0));

            Assert.Equal(-1.17549435E-38f, NumberBaseHelper<float>.CreateTruncating<Complex>(-1.1754943508222875E-38));
            Assert.Equal(-1.17549421E-38f, NumberBaseHelper<float>.CreateTruncating<Complex>(-1.1754942106924411E-38));
            Assert.Equal(-float.Epsilon, NumberBaseHelper<float>.CreateTruncating<Complex>(-1.401298464324817E-45));
            Assert.Equal(-0.0f, NumberBaseHelper<float>.CreateTruncating<Complex>(-0.0));

            Assert.Equal(+0.0f, NumberBaseHelper<float>.CreateTruncating<Complex>(+0.0));
            Assert.Equal(float.Epsilon, NumberBaseHelper<float>.CreateTruncating<Complex>(+1.401298464324817E-45));
            Assert.Equal(1.17549421E-38f, NumberBaseHelper<float>.CreateTruncating<Complex>(+1.1754942106924411E-38));
            Assert.Equal(1.17549435E-38f, NumberBaseHelper<float>.CreateTruncating<Complex>(+1.1754943508222875E-38));

            Assert.Equal(1.0f, NumberBaseHelper<float>.CreateTruncating<Complex>(+1.0));
            Assert.Equal(float.MaxValue, NumberBaseHelper<float>.CreateTruncating<Complex>(+3.4028234663852886E+38));

            Assert.Equal(float.PositiveInfinity, NumberBaseHelper<float>.CreateTruncating<Complex>(float.PositiveInfinity));

            Assert.Equal(float.NaN, NumberBaseHelper<float>.CreateTruncating<Complex>(float.NaN));
        }

        [Fact]
        public static void TryConvertToTruncatingUInt16Test()
        {
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.CreateTruncating<Complex>(0.0));
            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.CreateTruncating<Complex>(1.0));
            Assert.Equal((ushort)0x7FFF, NumberBaseHelper<ushort>.CreateTruncating<Complex>(32767.0));
            Assert.Equal((ushort)0x8000, NumberBaseHelper<ushort>.CreateTruncating<Complex>(32768.0));
            Assert.Equal((ushort)0xFFFF, NumberBaseHelper<ushort>.CreateTruncating<Complex>(65535.0));
        }

        [Fact]
        public static void TryConvertToTruncatingUInt32Test()
        {
            Assert.Equal((uint)0x00000000, NumberBaseHelper<uint>.CreateTruncating<Complex>(0.0));
            Assert.Equal((uint)0x00000001, NumberBaseHelper<uint>.CreateTruncating<Complex>(1.0));
            Assert.Equal((uint)0x7FFFFFFF, NumberBaseHelper<uint>.CreateTruncating<Complex>(2147483647.0));
            Assert.Equal((uint)0x80000000, NumberBaseHelper<uint>.CreateTruncating<Complex>(2147483648.0));
            Assert.Equal((uint)0xFFFFFFFF, NumberBaseHelper<uint>.CreateTruncating<Complex>(4294967295.0));
        }

        [Fact]
        public static void TryConvertToTruncatingUInt64Test()
        {
            Assert.Equal((ulong)0x0000000000000000, NumberBaseHelper<ulong>.CreateTruncating<Complex>(0.0));
            Assert.Equal((ulong)0x0000000000000001, NumberBaseHelper<ulong>.CreateTruncating<Complex>(1.0));
            Assert.Equal((ulong)0x8000000000000000, NumberBaseHelper<ulong>.CreateTruncating<Complex>(9223372036854775808.0));
            Assert.Equal((ulong)0xFFFFFFFFFFFFFFFF, NumberBaseHelper<ulong>.CreateTruncating<Complex>(18446744073709551615.0));
        }

        [Fact]
        public static void TryConvertToTruncatingUInt128Test()
        {
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), NumberBaseHelper<UInt128>.CreateTruncating<Complex>(0.0));
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0001), NumberBaseHelper<UInt128>.CreateTruncating<Complex>(1.0));
            Assert.Equal(new UInt128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000), NumberBaseHelper<UInt128>.CreateTruncating<Complex>(170141183460469231731687303715884105728.0));
            Assert.Equal(new UInt128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), NumberBaseHelper<UInt128>.CreateTruncating<Complex>(340282366920938463463374607431768211455.0));
        }

        //
        // ISignedNumber
        //

        [Fact]
        public static void NegativeOneTest()
        {
            Assert.Equal(-1.0, SignedNumberHelper<Complex>.NegativeOne);
        }

        //
        // ISubtractionOperators
        //

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

        //
        // IUnaryNegationOperators
        //

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

        //
        // IUnaryPlusOperators
        //

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
