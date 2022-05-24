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

        [Fact]
        public static void TryCreateFromByteTest()
        {
            Complex result;

            Assert.True(NumberBaseHelper<Complex>.TryCreate<byte>(0x00, out result));
            Assert.Equal(0.0, result);

            Assert.True(NumberBaseHelper<Complex>.TryCreate<byte>(0x01, out result));
            Assert.Equal(1.0, result);

            Assert.True(NumberBaseHelper<Complex>.TryCreate<byte>(0x7F, out result));
            Assert.Equal(127.0, result);

            Assert.True(NumberBaseHelper<Complex>.TryCreate<byte>(0x80, out result));
            Assert.Equal(128.0, result);

            Assert.True(NumberBaseHelper<Complex>.TryCreate<byte>(0xFF, out result));
            Assert.Equal(255.0, result);
        }

        [Fact]
        public static void TryCreateFromCharTest()
        {
            Complex result;

            Assert.True(NumberBaseHelper<Complex>.TryCreate<char>((char)0x0000, out result));
            Assert.Equal(0.0, result);

            Assert.True(NumberBaseHelper<Complex>.TryCreate<char>((char)0x0001, out result));
            Assert.Equal(1.0, result);

            Assert.True(NumberBaseHelper<Complex>.TryCreate<char>((char)0x7FFF, out result));
            Assert.Equal(32767.0, result);

            Assert.True(NumberBaseHelper<Complex>.TryCreate<char>((char)0x8000, out result));
            Assert.Equal(32768.0, result);

            Assert.True(NumberBaseHelper<Complex>.TryCreate<char>((char)0xFFFF, out result));
            Assert.Equal(65535.0, result);
        }

        [Fact]
        public static void TryCreateFromInt16Test()
        {
            Complex result;

            Assert.True(NumberBaseHelper<Complex>.TryCreate<short>(0x0000, out result));
            Assert.Equal(0.0, result);

            Assert.True(NumberBaseHelper<Complex>.TryCreate<short>(0x0001, out result));
            Assert.Equal(1.0, result);

            Assert.True(NumberBaseHelper<Complex>.TryCreate<short>(0x7FFF, out result));
            Assert.Equal(32767.0, result);

            Assert.True(NumberBaseHelper<Complex>.TryCreate<short>(unchecked((short)0x8000), out result));
            Assert.Equal(-32768.0, result);

            Assert.True(NumberBaseHelper<Complex>.TryCreate<short>(unchecked((short)0xFFFF), out result));
            Assert.Equal(-1.0, result);
        }

        [Fact]
        public static void TryCreateFromInt32Test()
        {
            Complex result;

            Assert.True(NumberBaseHelper<Complex>.TryCreate<int>(0x00000000, out result));
            Assert.Equal(0.0, result);

            Assert.True(NumberBaseHelper<Complex>.TryCreate<int>(0x00000001, out result));
            Assert.Equal(1.0, result);

            Assert.True(NumberBaseHelper<Complex>.TryCreate<int>(0x7FFFFFFF, out result));
            Assert.Equal(2147483647.0, result);

            Assert.True(NumberBaseHelper<Complex>.TryCreate<int>(unchecked((int)0x80000000), out result));
            Assert.Equal(-2147483648.0, result);

            Assert.True(NumberBaseHelper<Complex>.TryCreate<int>(unchecked((int)0xFFFFFFFF), out result));
            Assert.Equal(-1.0, result);
        }

        [Fact]
        public static void TryCreateFromInt64Test()
        {
            Complex result;

            Assert.True(NumberBaseHelper<Complex>.TryCreate<long>(0x0000000000000000, out result));
            Assert.Equal(0.0, result);

            Assert.True(NumberBaseHelper<Complex>.TryCreate<long>(0x0000000000000001, out result));
            Assert.Equal(1.0, result);

            Assert.True(NumberBaseHelper<Complex>.TryCreate<long>(0x7FFFFFFFFFFFFFFF, out result));
            Assert.Equal(9223372036854775807.0, result);

            Assert.True(NumberBaseHelper<Complex>.TryCreate<long>(unchecked(unchecked((long)0x8000000000000000)), out result));
            Assert.Equal(-9223372036854775808.0, result);

            Assert.True(NumberBaseHelper<Complex>.TryCreate<long>(unchecked(unchecked((long)0xFFFFFFFFFFFFFFFF)), out result));
            Assert.Equal(-1.0, result);
        }

        [Fact]
        public static void TryCreateFromIntPtrTest()
        {
            Complex result;

            if (Environment.Is64BitProcess)
            {
                Assert.True(NumberBaseHelper<Complex>.TryCreate<nint>(unchecked((nint)0x0000000000000000), out result));
                Assert.Equal(0.0, result);

                Assert.True(NumberBaseHelper<Complex>.TryCreate<nint>(unchecked((nint)0x0000000000000001), out result));
                Assert.Equal(1.0, result);

                Assert.True(NumberBaseHelper<Complex>.TryCreate<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF), out result));
                Assert.Equal(9223372036854775807.0, result);

                Assert.True(NumberBaseHelper<Complex>.TryCreate<nint>(unchecked((nint)0x8000000000000000), out result));
                Assert.Equal(-9223372036854775808.0, result);

                Assert.True(NumberBaseHelper<Complex>.TryCreate<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF), out result));
                Assert.Equal(-1.0, result);
            }
            else
            {
                Assert.True(NumberBaseHelper<Complex>.TryCreate<nint>((nint)0x00000000, out result));
                Assert.Equal(0.0, result);

                Assert.True(NumberBaseHelper<Complex>.TryCreate<nint>((nint)0x00000001, out result));
                Assert.Equal(1.0, result);

                Assert.True(NumberBaseHelper<Complex>.TryCreate<nint>((nint)0x7FFFFFFF, out result));
                Assert.Equal(2147483647.0, result);

                Assert.True(NumberBaseHelper<Complex>.TryCreate<nint>(unchecked((nint)0x80000000), out result));
                Assert.Equal(-2147483648.0, result);

                Assert.True(NumberBaseHelper<Complex>.TryCreate<nint>(unchecked((nint)0xFFFFFFFF), out result));
                Assert.Equal(-1.0, result);
            }
        }

        [Fact]
        public static void TryCreateFromSByteTest()
        {
            Complex result;

            Assert.True(NumberBaseHelper<Complex>.TryCreate<sbyte>(0x00, out result));
            Assert.Equal(0.0, result);

            Assert.True(NumberBaseHelper<Complex>.TryCreate<sbyte>(0x01, out result));
            Assert.Equal(1.0, result);

            Assert.True(NumberBaseHelper<Complex>.TryCreate<sbyte>(0x7F, out result));
            Assert.Equal(127.0, result);

            Assert.True(NumberBaseHelper<Complex>.TryCreate<sbyte>(unchecked((sbyte)0x80), out result));
            Assert.Equal(-128.0, result);

            Assert.True(NumberBaseHelper<Complex>.TryCreate<sbyte>(unchecked((sbyte)0xFF), out result));
            Assert.Equal(-1.0, result);
        }

        [Fact]
        public static void TryCreateFromUInt16Test()
        {
            Complex result;

            Assert.True(NumberBaseHelper<Complex>.TryCreate<ushort>(0x0000, out result));
            Assert.Equal(0.0, result);

            Assert.True(NumberBaseHelper<Complex>.TryCreate<ushort>(0x0001, out result));
            Assert.Equal(1.0, result);

            Assert.True(NumberBaseHelper<Complex>.TryCreate<ushort>(0x7FFF, out result));
            Assert.Equal(32767.0, result);

            Assert.True(NumberBaseHelper<Complex>.TryCreate<ushort>(0x8000, out result));
            Assert.Equal(32768.0, result);

            Assert.True(NumberBaseHelper<Complex>.TryCreate<ushort>(0xFFFF, out result));
            Assert.Equal(65535.0, result);
        }

        [Fact]
        public static void TryCreateFromUInt32Test()
        {
            Complex result;

            Assert.True(NumberBaseHelper<Complex>.TryCreate<uint>(0x00000000, out result));
            Assert.Equal(0.0, result);

            Assert.True(NumberBaseHelper<Complex>.TryCreate<uint>(0x00000001, out result));
            Assert.Equal(1.0, result);

            Assert.True(NumberBaseHelper<Complex>.TryCreate<uint>(0x7FFFFFFF, out result));
            Assert.Equal(2147483647.0, result);

            Assert.True(NumberBaseHelper<Complex>.TryCreate<uint>(0x80000000, out result));
            Assert.Equal(2147483648.0, result);

            Assert.True(NumberBaseHelper<Complex>.TryCreate<uint>(0xFFFFFFFF, out result));
            Assert.Equal(4294967295.0, result);
        }

        [Fact]
        public static void TryCreateFromUInt64Test()
        {
            Complex result;

            Assert.True(NumberBaseHelper<Complex>.TryCreate<ulong>(0x0000000000000000, out result));
            Assert.Equal(0.0, result);

            Assert.True(NumberBaseHelper<Complex>.TryCreate<ulong>(0x0000000000000001, out result));
            Assert.Equal(1.0, result);

            Assert.True(NumberBaseHelper<Complex>.TryCreate<ulong>(0x7FFFFFFFFFFFFFFF, out result));
            Assert.Equal(9223372036854775807.0, result);

            Assert.True(NumberBaseHelper<Complex>.TryCreate<ulong>(0x8000000000000000, out result));
            Assert.Equal(9223372036854775808.0, result);

            Assert.True(NumberBaseHelper<Complex>.TryCreate<ulong>(0xFFFFFFFFFFFFFFFF, out result));
            Assert.Equal(18446744073709551615.0, result);
        }

        [Fact]
        public static void TryCreateFromUIntPtrTest()
        {
            Complex result;

            if (Environment.Is64BitProcess)
            {
                Assert.True(NumberBaseHelper<Complex>.TryCreate<nuint>(unchecked((nuint)0x0000000000000000), out result));
                Assert.Equal(0.0, result);

                Assert.True(NumberBaseHelper<Complex>.TryCreate<nuint>(unchecked((nuint)0x0000000000000001), out result));
                Assert.Equal(1.0, result);

                Assert.True(NumberBaseHelper<Complex>.TryCreate<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF), out result));
                Assert.Equal(9223372036854775807.0, result);

                // https://github.com/dotnet/roslyn/issues/60714
                // Assert.True(NumberBaseHelper<Complex>.TryCreate<nuint>(unchecked((nuint)0x8000000000000000), out result));
                // Assert.Equal(9223372036854775808.0, result);
                //
                // Assert.True(NumberBaseHelper<Complex>.TryCreate<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF), out result));
                // Assert.Equal(18446744073709551615.0, result);
            }
            else
            {
                Assert.True(NumberBaseHelper<Complex>.TryCreate<nuint>((nuint)0x00000000, out result));
                Assert.Equal(0.0, result);

                Assert.True(NumberBaseHelper<Complex>.TryCreate<nuint>((nuint)0x00000001, out result));
                Assert.Equal(1.0, result);

                Assert.True(NumberBaseHelper<Complex>.TryCreate<nuint>((nuint)0x7FFFFFFF, out result));
                Assert.Equal(2147483647.0, result);

                // https://github.com/dotnet/roslyn/issues/60714
                // Assert.True(NumberBaseHelper<Complex>.TryCreate<nuint>(unchecked((nuint)0x80000000), out result));
                // Assert.Equal(2147483648.0, result);
                //
                // Assert.True(NumberBaseHelper<Complex>.TryCreate<nuint>(unchecked((nuint)0xFFFFFFFF), out result));
                // Assert.Equal(4294967295.0, result);
            }
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
