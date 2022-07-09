// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

#pragma warning disable xUnit1025 // reporting duplicate test cases due to not distinguishing 0.0 from -0.0, NaN from -NaN

namespace System.Runtime.InteropServices.Tests
{
    public class NFloatTests
    {
        // The tests themselves will take CrossPlatformMachineEpsilon## and adjust it according to the expected result
        // so that the delta used for comparison will compare the most significant digits and ignore
        // any digits that are outside the single or double precision range (6-9 or 15-17 digits).
        //
        // For example, a test with an expect result in the format of 0.xxxxxxxxx will use
        // CrossPlatformMachineEpsilon## for the variance, while an expected result in the format of 0.0xxxxxxxxx
        // will use CrossPlatformMachineEpsilon## / 10 and expected result in the format of x.xxxxxx will
        // use CrossPlatformMachineEpsilon## * 10.

        // binary32 (float) has a machine epsilon of 2^-23 (approx. 1.19e-07). However, this
        // is slightly too accurate when writing tests meant to run against libm implementations
        // for various platforms. 2^-21 (approx. 4.76e-07) seems to be as accurate as we can get.
        private const float CrossPlatformMachineEpsilon32 = 4.76837158e-07f;

        // binary64 (double) has a machine epsilon of 2^-52 (approx. 2.22e-16). However, this
        // is slightly too accurate when writing tests meant to run against libm implementations
        // for various platforms. 2^-50 (approx. 8.88e-16) seems to be as accurate as we can get.
        private const double CrossPlatformMachineEpsilon64 = 8.8817841970012523e-16;

        [Fact]
        public void Ctor_Empty()
        {
            NFloat result = new NFloat();
            Assert.Equal(0, result.Value);
        }

        [Fact]
        public void Ctor_Float()
        {
            NFloat result = new NFloat(42.0f);
            Assert.Equal(42.0, result.Value);
        }

        [Fact]
        public void Ctor_Double()
        {
            NFloat result = new NFloat(42.0);
            Assert.Equal(42.0, result.Value);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.Is32BitProcess))]
        public void Ctor_Double_OutOfRange()
        {
            NFloat result = new NFloat(double.MaxValue);
            Assert.Equal(float.PositiveInfinity, result.Value);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.Is64BitProcess))]
        public void Ctor_Double_LargeValue()
        {
            NFloat result = new NFloat(double.MaxValue);
            Assert.Equal(double.MaxValue, result.Value);
        }

        [Fact]
        public void Epsilon()
        {
            NFloat result = NFloat.Epsilon;

            if (Environment.Is64BitProcess)
            {
                Assert.Equal(double.Epsilon, result.Value);
            }
            else
            {
                Assert.Equal(float.Epsilon, result.Value);
            }
        }

        [Fact]
        public void MaxValue()
        {
            NFloat result = NFloat.MaxValue;

            if (Environment.Is64BitProcess)
            {
                Assert.Equal(double.MaxValue, result.Value);
            }
            else
            {
                Assert.Equal(float.MaxValue, result.Value);
            }
        }

        [Fact]
        public void MinValue()
        {
            NFloat result = NFloat.MinValue;

            if (Environment.Is64BitProcess)
            {
                Assert.Equal(double.MinValue, result.Value);
            }
            else
            {
                Assert.Equal(float.MinValue, result.Value);
            }
        }

        [Fact]
        public void NaN()
        {
            NFloat result = NFloat.NaN;
            Assert.True(double.IsNaN(result.Value));
        }

        [Fact]
        public void NegativeInfinity()
        {
            NFloat result = NFloat.NegativeInfinity;

            if (Environment.Is64BitProcess)
            {
                Assert.Equal(double.NegativeInfinity, result.Value);
            }
            else
            {
                Assert.Equal(float.NegativeInfinity, result.Value);
            }
        }

        [Fact]
        public void PositiveInfinity()
        {
            NFloat result = NFloat.PositiveInfinity;

            if (Environment.Is64BitProcess)
            {
                Assert.Equal(double.PositiveInfinity, result.Value);
            }
            else
            {
                Assert.Equal(float.PositiveInfinity, result.Value);
            }
        }

        [Fact]
        public unsafe void Size()
        {
            int size = PlatformDetection.Is32BitProcess ? 4 : 8;
#pragma warning disable xUnit2000 // The value under test here is the sizeof expression
            Assert.Equal(size, sizeof(NFloat));
#pragma warning restore xUnit2000
            Assert.Equal(size, Marshal.SizeOf<NFloat>());
        }

        [Theory]
        [InlineData(-4567.0f)]
        [InlineData(-4567.89101f)]
        [InlineData(0.0f)]
        [InlineData(4567.0f)]
        [InlineData(4567.89101f)]
        public static void op_UnaryPlus(float value)
        {
            NFloat result = +(new NFloat(value));
            Assert.Equal(+value, result.Value);
        }

        [Theory]
        [InlineData(-4567.0f)]
        [InlineData(-4567.89101f)]
        [InlineData(0.0f)]
        [InlineData(4567.0f)]
        [InlineData(4567.89101f)]
        public static void op_UnaryNegation(float value)
        {
            NFloat result = -(new NFloat(value));
            Assert.Equal(-value, result.Value);
        }

        [Theory]
        [InlineData(-4567.0f)]
        [InlineData(-4567.89101f)]
        [InlineData(0.0f)]
        [InlineData(4567.0f)]
        [InlineData(4567.89101f)]
        public static void op_Decrement(float value)
        {
            NFloat result = new NFloat(value);
            --result;

            if (Environment.Is64BitProcess)
            {
                Assert.Equal((double)value - 1, result.Value);
            }
            else
            {
                Assert.Equal(value - 1, result.Value);
            }
        }

        [Theory]
        [InlineData(-4567.0f)]
        [InlineData(-4567.89101f)]
        [InlineData(0.0f)]
        [InlineData(4567.0f)]
        [InlineData(4567.89101f)]
        public static void op_Increment(float value)
        {
            NFloat result = new NFloat(value);
            ++result;

            if (Environment.Is64BitProcess)
            {
                Assert.Equal((double)value + 1, result.Value);
            }
            else
            {
                Assert.Equal(value + 1, result.Value);
            }
        }

        [Theory]
        [InlineData(-4567.0f, 3.14f)]
        [InlineData(-4567.89101f, 3.14569f)]
        [InlineData(0.0f, 3.14f)]
        [InlineData(4567.0f, -3.14f)]
        [InlineData(4567.89101f, -3.14569f)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/65557", typeof(PlatformDetection), nameof(PlatformDetection.IsAndroid), nameof(PlatformDetection.Is32BitProcess))]
        public static void op_Addition(float left, float right)
        {
            NFloat result = new NFloat(left) + new NFloat(right);

            if (Environment.Is64BitProcess)
            {
                Assert.Equal((double)left + right, result.Value);
            }
            else
            {
                Assert.Equal(left + right, result.Value);
            }
        }

        [Theory]
        [InlineData(-4567.0f, 3.14f)]
        [InlineData(-4567.89101f, 3.14569f)]
        [InlineData(0.0f, 3.14f)]
        [InlineData(4567.0f, -3.14f)]
        [InlineData(4567.89101f, -3.14569f)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/65557", typeof(PlatformDetection), nameof(PlatformDetection.IsAndroid), nameof(PlatformDetection.Is32BitProcess))]
        public static void op_Subtraction(float left, float right)
        {
            NFloat result = new NFloat(left) - new NFloat(right);

            if (Environment.Is64BitProcess)
            {
                Assert.Equal((double)left - right, result.Value);
            }
            else
            {
                Assert.Equal(left - right, result.Value);
            }
        }

        [Theory]
        [InlineData(-4567.0f, 3.14f)]
        [InlineData(-4567.89101f, 3.14569f)]
        [InlineData(0.0f, 3.14f)]
        [InlineData(4567.0f, -3.14f)]
        [InlineData(4567.89101f, -3.14569f)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/65557", typeof(PlatformDetection), nameof(PlatformDetection.IsAndroid), nameof(PlatformDetection.Is32BitProcess))]
        public static void op_Multiply(float left, float right)
        {
            NFloat result = new NFloat(left) * new NFloat(right);

            if (Environment.Is64BitProcess)
            {
                Assert.Equal((double)left * right, result.Value);
            }
            else
            {
                Assert.Equal(left * right, result.Value);
            }
        }

        [Theory]
        [InlineData(-4567.0f, 3.14f)]
        [InlineData(-4567.89101f, 3.14569f)]
        [InlineData(0.0f, 3.14f)]
        [InlineData(4567.0f, -3.14f)]
        [InlineData(4567.89101f, -3.14569f)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/65557", typeof(PlatformDetection), nameof(PlatformDetection.IsAndroid), nameof(PlatformDetection.Is32BitProcess))]
        public static void op_Division(float left, float right)
        {
            NFloat result = new NFloat(left) / new NFloat(right);

            if (Environment.Is64BitProcess)
            {
                Assert.Equal((double)left / right, result.Value);
            }
            else
            {
                Assert.Equal(left / right, result.Value);
            }
        }

        [Theory]
        [InlineData(-4567.0f, 3.14f)]
        [InlineData(-4567.89101f, 3.14569f)]
        [InlineData(0.0f, 3.14f)]
        [InlineData(4567.0f, -3.14f)]
        [InlineData(4567.89101f, -3.14569f)]
        public static void op_Modulus(float left, float right)
        {
            NFloat result = new NFloat(left) % new NFloat(right);

            if (Environment.Is64BitProcess)
            {
                Assert.Equal((double)left % right, result.Value);
            }
            else
            {
                Assert.Equal(left % right, result.Value);
            }
        }

        [Theory]
        [InlineData(789.0f, 789.0f)]
        [InlineData(789.0f, -789.0f)]
        [InlineData(789.0f, 0.0f)]
        [InlineData(789.0f, 1000.0f)]
        public void op_Equality(float left, float right)
        {
            bool result = new NFloat(left) == new NFloat(right);
            Assert.Equal(left == right, result);
        }

        [Theory]
        [InlineData(789.0f, 789.0f)]
        [InlineData(789.0f, -789.0f)]
        [InlineData(789.0f, 0.0f)]
        [InlineData(789.0f, 1000.0f)]
        public void op_Inequality(float left, float right)
        {
            bool result = new NFloat(left) != new NFloat(right);
            Assert.Equal(left != right, result);
        }

        [Theory]
        [InlineData(789.0f, 789.0f)]
        [InlineData(789.0f, -789.0f)]
        [InlineData(789.0f, 0.0f)]
        [InlineData(789.0f, 1000.0f)]
        public void op_GreaterThan(float left, float right)
        {
            bool result = new NFloat(left) > new NFloat(right);
            Assert.Equal(left > right, result);
        }

        [Theory]
        [InlineData(789.0f, 789.0f)]
        [InlineData(789.0f, -789.0f)]
        [InlineData(789.0f, 0.0f)]
        [InlineData(789.0f, 1000.0f)]
        public void op_GreaterThanOrEqual(float left, float right)
        {
            bool result = new NFloat(left) >= new NFloat(right);
            Assert.Equal(left >= right, result);
        }

        [Theory]
        [InlineData(789.0f, 789.0f)]
        [InlineData(789.0f, -789.0f)]
        [InlineData(789.0f, 0.0f)]
        [InlineData(789.0f, 1000.0f)]
        public void op_LessThan(float left, float right)
        {
            bool result = new NFloat(left) < new NFloat(right);
            Assert.Equal(left < right, result);
        }

        [Theory]
        [InlineData(789.0f, 789.0f)]
        [InlineData(789.0f, -789.0f)]
        [InlineData(789.0f, 0.0f)]
        [InlineData(789.0f, 1000.0f)]
        public void op_LessThanOrEqual(float left, float right)
        {
            bool result = new NFloat(left) <= new NFloat(right);
            Assert.Equal(left <= right, result);
        }

        [Theory]
        [InlineData(-4567.0f)]
        [InlineData(-4567.89101f)]
        [InlineData(0.0f)]
        [InlineData(4567.0f)]
        [InlineData(4567.89101f)]
        public void DoubleToNFloat(float value)
        {
            NFloat result = (NFloat)(double)value;

            if (Environment.Is64BitProcess)
            {
                Assert.Equal(value, result.Value);
            }
            else
            {
                Assert.Equal((float)value, result.Value);
            }
        }

        [Theory]
        [InlineData(-4567.0f)]
        [InlineData(-4567.89101f)]
        [InlineData(0.0f)]
        [InlineData(4567.0f)]
        [InlineData(4567.89101f)]
        public void NFloatToByte(float value)
        {
            byte result = (byte)new NFloat(value);
            Assert.Equal((byte)value, result);
        }

        [Theory]
        [InlineData(-4567.0f)]
        [InlineData(-4567.89101f)]
        [InlineData(0.0f)]
        [InlineData(4567.0f)]
        [InlineData(4567.89101f)]
        public void NFloatToChar(float value)
        {
            char result = (char)new NFloat(value);
            Assert.Equal((char)value, result);
        }

        [Theory]
        [InlineData(-4567.0f)]
        [InlineData(-4567.89101f)]
        [InlineData(0.0f)]
        [InlineData(4567.0f)]
        [InlineData(4567.89101f)]
        public void NFloatToDecimal(float value)
        {
            decimal result = (decimal)new NFloat(value);

            if (Environment.Is64BitProcess)
            {
                Assert.Equal((decimal)(double)value, result);
            }
            else
            {
                Assert.Equal((decimal)value, result);
            }
        }

        [Theory]
        [InlineData(-4567.0f)]
        [InlineData(-4567.89101f)]
        [InlineData(0.0f)]
        [InlineData(4567.0f)]
        [InlineData(4567.89101f)]
        public void NFloatToInt16(float value)
        {
            short result = (short)new NFloat(value);
            Assert.Equal((short)value, result);
        }

        [Theory]
        [InlineData(-4567.0f)]
        [InlineData(-4567.89101f)]
        [InlineData(0.0f)]
        [InlineData(4567.0f)]
        [InlineData(4567.89101f)]
        public void NFloatToInt32(float value)
        {
            int result = (int)new NFloat(value);
            Assert.Equal((int)value, result);
        }

        [Theory]
        [InlineData(-4567.0f)]
        [InlineData(-4567.89101f)]
        [InlineData(0.0f)]
        [InlineData(4567.0f)]
        [InlineData(4567.89101f)]
        public void NFloatToInt64(float value)
        {
            long result = (long)new NFloat(value);
            Assert.Equal((long)value, result);
        }

        [Theory]
        [InlineData(-4567.0f, Skip = "https://github.com/dotnet/runtime/issues/64386")]
        [InlineData(-4567.89101f, Skip = "https://github.com/dotnet/runtime/issues/64386")]
        [InlineData(0.0f)]
        [InlineData(4567.0f)]
        [InlineData(4567.89101f)]
        public void NFloatToIntPtr(float value)
        {
            nint result = (nint)new NFloat(value);
            Assert.Equal((nint)value, result);
        }

        [Theory]
        [InlineData(-4567.0f)]
        [InlineData(-4567.89101f)]
        [InlineData(0.0f)]
        [InlineData(4567.0f)]
        [InlineData(4567.89101f)]
        public void NFloatToSByte(float value)
        {
            sbyte result = (sbyte)new NFloat(value);
            Assert.Equal((sbyte)value, result);
        }

        [Theory]
        [InlineData(-4567.0f)]
        [InlineData(-4567.89101f)]
        [InlineData(0.0f)]
        [InlineData(4567.0f)]
        [InlineData(4567.89101f)]
        public void NFloatToSingle(float value)
        {
            float result = (float)new NFloat(value);
            Assert.Equal(value, result);
        }

        [Theory]
        [InlineData(-4567.0f)]
        [InlineData(-4567.89101f)]
        [InlineData(0.0f)]
        [InlineData(4567.0f)]
        [InlineData(4567.89101f)]
        public void NFloatToUInt16(float value)
        {
            ushort result = (ushort)new NFloat(value);
            Assert.Equal((ushort)value, result);
        }

        [Theory]
        [InlineData(-4567.0f)]
        [InlineData(-4567.89101f)]
        [InlineData(0.0f)]
        [InlineData(4567.0f)]
        [InlineData(4567.89101f)]
        public void NFloatToUInt32(float value)
        {
            uint result = (uint)new NFloat(value);
            Assert.Equal((uint)value, result);
        }

        [Theory]
        [InlineData(-4567.0f)]
        [InlineData(-4567.89101f)]
        [InlineData(0.0f)]
        [InlineData(4567.0f)]
        [InlineData(4567.89101f)]
        public void NFloatToUInt64(float value)
        {
            ulong result = (ulong)new NFloat(value);
            Assert.Equal((ulong)value, result);
        }

        [Theory]
        [InlineData(-4567.0f)]
        [InlineData(-4567.89101f)]
        [InlineData(0.0f)]
        [InlineData(4567.0f)]
        [InlineData(4567.89101f)]
        public void NFloatToUIntPtr(float value)
        {
            nuint result = (nuint)new NFloat(value);
            Assert.Equal((nuint)value, result);
        }

        [Theory]
        [InlineData((byte)0)]
        [InlineData((byte)5)]
        [InlineData((byte)42)]
        [InlineData((byte)127)]
        [InlineData((byte)255)]
        public void ByteToNFloat(byte value)
        {
            NFloat result = value;
            Assert.Equal(value, result.Value);
        }

        [Theory]
        [InlineData('A')]
        [InlineData('B')]
        [InlineData('C')]
        [InlineData('D')]
        [InlineData('E')]
        public void CharToNFloat(char value)
        {
            NFloat result = value;
            Assert.Equal(value, result.Value);
        }

        [Theory]
        [InlineData((short)-255)]
        [InlineData((short)-127)]
        [InlineData((short)0)]
        [InlineData((short)127)]
        [InlineData((short)255)]
        public void Int16ToNFloat(short value)
        {
            NFloat result = value;
            Assert.Equal(value, result.Value);
        }

        [Theory]
        [InlineData(-255)]
        [InlineData(-127)]
        [InlineData(0)]
        [InlineData(127)]
        [InlineData(255)]
        public void Int32ToNFloat(int value)
        {
            NFloat result = value;
            Assert.Equal(value, result.Value);
        }

        [Theory]
        [InlineData((long)-255)]
        [InlineData((long)-127)]
        [InlineData((long)0)]
        [InlineData((long)127)]
        [InlineData((long)255)]
        public void Int64ToNFloat(long value)
        {
            NFloat result = value;
            Assert.Equal(value, result.Value);
        }

        [Theory]
        [InlineData((int)-255)]
        [InlineData((int)-127)]
        [InlineData((int)0)]
        [InlineData((int)127)]
        [InlineData((int)255)]
        public void IntPtrToNFloat(int value)
        {
            NFloat result = (nint)value;
            Assert.Equal(value, result.Value);
        }

        [Theory]
        [InlineData((sbyte)-127)]
        [InlineData((sbyte)-63)]
        [InlineData((sbyte)0)]
        [InlineData((sbyte)63)]
        [InlineData((sbyte)127)]
        public void SByteToNFloat(sbyte value)
        {
            NFloat result = value;
            Assert.Equal(value, result.Value);
        }

        [Theory]
        [InlineData(-4567.0f)]
        [InlineData(-4567.89101f)]
        [InlineData(0.0f)]
        [InlineData(4567.0f)]
        [InlineData(4567.89101f)]
        public void SingleToNFloat(float value)
        {
            NFloat result = value;
            Assert.Equal(value, result.Value);
        }

        [Theory]
        [InlineData((ushort)0)]
        [InlineData((ushort)5)]
        [InlineData((ushort)42)]
        [InlineData((ushort)127)]
        [InlineData((ushort)255)]
        public void UInt16ToNFloat(ushort value)
        {
            NFloat result = value;
            Assert.Equal(value, result.Value);
        }

        [Theory]
        [InlineData((uint)0)]
        [InlineData((uint)5)]
        [InlineData((uint)42)]
        [InlineData((uint)127)]
        [InlineData((uint)255)]
        public void UInt32ToNFloat(uint value)
        {
            NFloat result = value;
            Assert.Equal(value, result.Value);
        }

        [Theory]
        [InlineData((ulong)0)]
        [InlineData((ulong)5)]
        [InlineData((ulong)42)]
        [InlineData((ulong)127)]
        [InlineData((ulong)255)]
        public void UInt64ToNFloat(ulong value)
        {
            NFloat result = value;
            Assert.Equal(value, result.Value);
        }

        [Theory]
        [InlineData((uint)0)]
        [InlineData((uint)5)]
        [InlineData((uint)42)]
        [InlineData((uint)127)]
        [InlineData((uint)255)]
        public void UIntPtrToNFloat(uint value)
        {
            NFloat result = (nuint)value;
            Assert.Equal(value, result.Value);
        }

        [Theory]
        [InlineData(-4567.0f)]
        [InlineData(-4567.89101f)]
        [InlineData(0.0f)]
        [InlineData(4567.0f)]
        [InlineData(4567.89101f)]
        public void NFloatToDouble(float value)
        {
            double result = new NFloat(value);
            Assert.Equal(value, result);
        }

        [Theory]
        [InlineData(-4567.0f)]
        [InlineData(-4567.89101f)]
        [InlineData(0.0f)]
        [InlineData(4567.0f)]
        [InlineData(4567.89101f)]
        [InlineData(float.Epsilon)]
        [InlineData(float.NegativeInfinity)]
        [InlineData(float.PositiveInfinity)]
        [InlineData(float.NaN)]
        public void IsFinite(float value)
        {
            bool result = NFloat.IsFinite(value);
            Assert.Equal(float.IsFinite(value), result);
        }

        [Theory]
        [InlineData(-4567.0f)]
        [InlineData(-4567.89101f)]
        [InlineData(0.0f)]
        [InlineData(4567.0f)]
        [InlineData(4567.89101f)]
        [InlineData(float.Epsilon)]
        [InlineData(float.NegativeInfinity)]
        [InlineData(float.PositiveInfinity)]
        [InlineData(float.NaN)]
        public void IsInfinity(float value)
        {
            bool result = NFloat.IsInfinity(value);
            Assert.Equal(float.IsInfinity(value), result);
        }

        [Theory]
        [InlineData(-4567.0f)]
        [InlineData(-4567.89101f)]
        [InlineData(0.0f)]
        [InlineData(4567.0f)]
        [InlineData(4567.89101f)]
        [InlineData(float.Epsilon)]
        [InlineData(float.NegativeInfinity)]
        [InlineData(float.PositiveInfinity)]
        [InlineData(float.NaN)]
        public void IsNaN(float value)
        {
            bool result = NFloat.IsNaN(value);
            Assert.Equal(float.IsNaN(value), result);
        }

        [Theory]
        [InlineData(-4567.0f)]
        [InlineData(-4567.89101f)]
        [InlineData(0.0f)]
        [InlineData(4567.0f)]
        [InlineData(4567.89101f)]
        [InlineData(float.Epsilon)]
        [InlineData(float.NegativeInfinity)]
        [InlineData(float.PositiveInfinity)]
        [InlineData(float.NaN)]
        public void IsNegative(float value)
        {
            bool result = NFloat.IsNegative(value);
            Assert.Equal(float.IsNegative(value), result);
        }

        [Theory]
        [InlineData(-4567.0f)]
        [InlineData(-4567.89101f)]
        [InlineData(0.0f)]
        [InlineData(4567.0f)]
        [InlineData(4567.89101f)]
        [InlineData(float.Epsilon)]
        [InlineData(float.NegativeInfinity)]
        [InlineData(float.PositiveInfinity)]
        [InlineData(float.NaN)]
        public void IsNegativeInfinity(float value)
        {
            bool result = NFloat.IsNegativeInfinity(value);
            Assert.Equal(float.IsNegativeInfinity(value), result);
        }

        [Theory]
        [InlineData(-4567.0f)]
        [InlineData(-4567.89101f)]
        [InlineData(0.0f)]
        [InlineData(4567.0f)]
        [InlineData(4567.89101f)]
        [InlineData(float.Epsilon)]
        [InlineData(float.NegativeInfinity)]
        [InlineData(float.PositiveInfinity)]
        [InlineData(float.NaN)]
        public void IsNormal(float value)
        {
            bool result = NFloat.IsNormal(value);

            if (Environment.Is64BitProcess)
            {
                Assert.Equal(double.IsNormal(value), result);
            }
            else
            {
                Assert.Equal(float.IsNormal(value), result);
            }
        }

        [Theory]
        [InlineData(-4567.0f)]
        [InlineData(-4567.89101f)]
        [InlineData(0.0f)]
        [InlineData(4567.0f)]
        [InlineData(4567.89101f)]
        [InlineData(float.Epsilon)]
        [InlineData(float.NegativeInfinity)]
        [InlineData(float.PositiveInfinity)]
        [InlineData(float.NaN)]
        public void IsPositiveInfinity(float value)
        {
            bool result = NFloat.IsPositiveInfinity(value);
            Assert.Equal(float.IsPositiveInfinity(value), result);
        }

        [Theory]
        [InlineData(-4567.0f)]
        [InlineData(-4567.89101f)]
        [InlineData(0.0f)]
        [InlineData(4567.0f)]
        [InlineData(4567.89101f)]
        [InlineData(float.Epsilon)]
        [InlineData(float.NegativeInfinity)]
        [InlineData(float.PositiveInfinity)]
        [InlineData(float.NaN)]
        public void IsSubnormal(float value)
        {
            bool result = NFloat.IsSubnormal(value);

            if (Environment.Is64BitProcess)
            {
                Assert.Equal(double.IsSubnormal(value), result);
            }
            else
            {
                Assert.Equal(float.IsSubnormal(value), result);
            }
        }

        public static IEnumerable<object[]> EqualsData()
        {
            yield return new object[] { new NFloat(789.0f), new NFloat(789.0f), true };
            yield return new object[] { new NFloat(789.0f), new NFloat(-789.0f), false };
            yield return new object[] { new NFloat(789.0f), new NFloat(0.0f), false };
            yield return new object[] { new NFloat(789.0f), 789.0f, false };
            yield return new object[] { new NFloat(789.0f), "789.0", false };
        }

        [Theory]
        [MemberData(nameof(EqualsData))]
        public void EqualsTest(NFloat f1, object obj, bool expected)
        {
            if (obj is NFloat f2)
            {
                Assert.Equal(expected, f1.Equals((object)f2));
                Assert.Equal(expected, f1.Equals(f2));
                Assert.Equal(expected, f1.GetHashCode().Equals(f2.GetHashCode()));
            }
            Assert.Equal(expected, f1.Equals(obj));
        }

        [Fact]
        public void NaNEqualsTest()
        {
            NFloat f1 = new NFloat(float.NaN);
            NFloat f2 = new NFloat(float.NaN);
            Assert.Equal(f1.Value.Equals(f2.Value), f1.Equals(f2));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is64BitProcess))]
        [InlineData(-4567.0f)]
        [InlineData(-4567.89101f)]
        [InlineData(0.0f)]
        [InlineData(4567.0f)]
        [InlineData(4567.89101f)]
        [InlineData(float.NaN)]
        public static void ToStringTest64(float value)
        {
            NFloat nfloat = new NFloat(value);

            Assert.Equal(((double)value).ToString(), nfloat.ToString());
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is32BitProcess))]
        [InlineData(-4567.0f)]
        [InlineData(-4567.89101f)]
        [InlineData(0.0f)]
        [InlineData(4567.0f)]
        [InlineData(4567.89101f)]
        [InlineData(float.NaN)]
        public static void ToStringTest32(float value)
        {
            NFloat nfloat = new NFloat(value);

            Assert.Equal(value.ToString(), nfloat.ToString());
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is32BitProcess))]
        [InlineData(float.NegativeInfinity, float.PositiveInfinity, float.PositiveInfinity)]
        [InlineData(float.PositiveInfinity, float.NegativeInfinity, float.PositiveInfinity)]
        [InlineData(float.MinValue, float.MaxValue, float.MaxValue)]
        [InlineData(float.MaxValue, float.MinValue, float.MaxValue)]
        [InlineData(float.NaN, float.NaN, float.NaN)]
        [InlineData(float.NaN, 1.0f, 1.0f)]
        [InlineData(1.0f, float.NaN, 1.0f)]
        [InlineData(float.PositiveInfinity, float.NaN, float.PositiveInfinity)]
        [InlineData(float.NegativeInfinity, float.NaN, float.NegativeInfinity)]
        [InlineData(float.NaN, float.PositiveInfinity, float.PositiveInfinity)]
        [InlineData(float.NaN, float.NegativeInfinity, float.NegativeInfinity)]
        [InlineData(-0.0f, 0.0f, 0.0f)]
        [InlineData(0.0f, -0.0f, 0.0f)]
        [InlineData(2.0f, -3.0f, -3.0f)]
        [InlineData(-3.0f, 2.0f, -3.0f)]
        [InlineData(3.0f, -2.0f, 3.0f)]
        [InlineData(-2.0f, 3.0f, 3.0f)]
        public static void MaxMagnitudeNumberTest32(float x, float y, float expectedResult)
        {
            AssertExtensions.Equal(expectedResult, (float)NFloat.MaxMagnitudeNumber(x, y), 0.0f);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is32BitProcess))]
        [InlineData(float.NegativeInfinity, float.PositiveInfinity, float.PositiveInfinity)]
        [InlineData(float.PositiveInfinity, float.NegativeInfinity, float.PositiveInfinity)]
        [InlineData(float.MinValue, float.MaxValue, float.MaxValue)]
        [InlineData(float.MaxValue, float.MinValue, float.MaxValue)]
        [InlineData(float.NaN, float.NaN, float.NaN)]
        [InlineData(float.NaN, 1.0f, 1.0f)]
        [InlineData(1.0f, float.NaN, 1.0f)]
        [InlineData(float.PositiveInfinity, float.NaN, float.PositiveInfinity)]
        [InlineData(float.NegativeInfinity, float.NaN, float.NegativeInfinity)]
        [InlineData(float.NaN, float.PositiveInfinity, float.PositiveInfinity)]
        [InlineData(float.NaN, float.NegativeInfinity, float.NegativeInfinity)]
        [InlineData(-0.0f, 0.0f, 0.0f)]
        [InlineData(0.0f, -0.0f, 0.0f)]
        [InlineData(2.0f, -3.0f, 2.0f)]
        [InlineData(-3.0f, 2.0f, 2.0f)]
        [InlineData(3.0f, -2.0f, 3.0f)]
        [InlineData(-2.0f, 3.0f, 3.0f)]
        public static void MaxNumberTest32(float x, float y, float expectedResult)
        {
            AssertExtensions.Equal(expectedResult, (float)NFloat.MaxNumber(x, y), 0.0f);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is32BitProcess))]
        [InlineData(float.NegativeInfinity, float.PositiveInfinity, float.NegativeInfinity)]
        [InlineData(float.PositiveInfinity, float.NegativeInfinity, float.NegativeInfinity)]
        [InlineData(float.MinValue, float.MaxValue, float.MinValue)]
        [InlineData(float.MaxValue, float.MinValue, float.MinValue)]
        [InlineData(float.NaN, float.NaN, float.NaN)]
        [InlineData(float.NaN, 1.0f, 1.0f)]
        [InlineData(1.0f, float.NaN, 1.0f)]
        [InlineData(float.PositiveInfinity, float.NaN, float.PositiveInfinity)]
        [InlineData(float.NegativeInfinity, float.NaN, float.NegativeInfinity)]
        [InlineData(float.NaN, float.PositiveInfinity, float.PositiveInfinity)]
        [InlineData(float.NaN, float.NegativeInfinity, float.NegativeInfinity)]
        [InlineData(-0.0f, 0.0f, -0.0f)]
        [InlineData(0.0f, -0.0f, -0.0f)]
        [InlineData(2.0f, -3.0f, 2.0f)]
        [InlineData(-3.0f, 2.0f, 2.0f)]
        [InlineData(3.0f, -2.0f, -2.0f)]
        [InlineData(-2.0f, 3.0f, -2.0f)]
        public static void MinMagnitudeNumberTest32(float x, float y, float expectedResult)
        {
            AssertExtensions.Equal(expectedResult, (float)NFloat.MinMagnitudeNumber(x, y), 0.0f);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is32BitProcess))]
        [InlineData(float.NegativeInfinity, float.PositiveInfinity, float.NegativeInfinity)]
        [InlineData(float.PositiveInfinity, float.NegativeInfinity, float.NegativeInfinity)]
        [InlineData(float.MinValue, float.MaxValue, float.MinValue)]
        [InlineData(float.MaxValue, float.MinValue, float.MinValue)]
        [InlineData(float.NaN, float.NaN, float.NaN)]
        [InlineData(float.NaN, 1.0f, 1.0f)]
        [InlineData(1.0f, float.NaN, 1.0f)]
        [InlineData(float.PositiveInfinity, float.NaN, float.PositiveInfinity)]
        [InlineData(float.NegativeInfinity, float.NaN, float.NegativeInfinity)]
        [InlineData(float.NaN, float.PositiveInfinity, float.PositiveInfinity)]
        [InlineData(float.NaN, float.NegativeInfinity, float.NegativeInfinity)]
        [InlineData(-0.0f, 0.0f, -0.0f)]
        [InlineData(0.0f, -0.0f, -0.0f)]
        [InlineData(2.0f, -3.0f, -3.0f)]
        [InlineData(-3.0f, 2.0f, -3.0f)]
        [InlineData(3.0f, -2.0f, -2.0f)]
        [InlineData(-2.0f, 3.0f, -2.0f)]
        public static void MinNumberTest32(float x, float y, float expectedResult)
        {
            AssertExtensions.Equal(expectedResult, (float)NFloat.MinNumber(x, y), 0.0f);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is64BitProcess))]
        [InlineData(double.NegativeInfinity, double.PositiveInfinity, double.PositiveInfinity)]
        [InlineData(double.PositiveInfinity, double.NegativeInfinity, double.PositiveInfinity)]
        [InlineData(double.MinValue, double.MaxValue, double.MaxValue)]
        [InlineData(double.MaxValue, double.MinValue, double.MaxValue)]
        [InlineData(double.NaN, double.NaN, double.NaN)]
        [InlineData(double.NaN, 1.0, 1.0)]
        [InlineData(1.0, double.NaN, 1.0)]
        [InlineData(double.PositiveInfinity, double.NaN, double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity, double.NaN, double.NegativeInfinity)]
        [InlineData(double.NaN, double.PositiveInfinity, double.PositiveInfinity)]
        [InlineData(double.NaN, double.NegativeInfinity, double.NegativeInfinity)]
        [InlineData(-0.0, 0.0, 0.0)]
        [InlineData(0.0, -0.0, 0.0)]
        [InlineData(2.0, -3.0, -3.0)]
        [InlineData(-3.0, 2.0, -3.0)]
        [InlineData(3.0, -2.0, 3.0)]
        [InlineData(-2.0, 3.0, 3.0)]
        public static void MaxMagnitudeNumberTest64(double x, double y, double expectedResult)
        {
            AssertExtensions.Equal(expectedResult, NFloat.MaxMagnitudeNumber((NFloat)x, (NFloat)y), 0.0);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is64BitProcess))]
        [InlineData(double.NegativeInfinity, double.PositiveInfinity, double.PositiveInfinity)]
        [InlineData(double.PositiveInfinity, double.NegativeInfinity, double.PositiveInfinity)]
        [InlineData(double.MinValue, double.MaxValue, double.MaxValue)]
        [InlineData(double.MaxValue, double.MinValue, double.MaxValue)]
        [InlineData(double.NaN, double.NaN, double.NaN)]
        [InlineData(double.NaN, 1.0, 1.0)]
        [InlineData(1.0, double.NaN, 1.0)]
        [InlineData(double.PositiveInfinity, double.NaN, double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity, double.NaN, double.NegativeInfinity)]
        [InlineData(double.NaN, double.PositiveInfinity, double.PositiveInfinity)]
        [InlineData(double.NaN, double.NegativeInfinity, double.NegativeInfinity)]
        [InlineData(-0.0, 0.0, 0.0)]
        [InlineData(0.0, -0.0, 0.0)]
        [InlineData(2.0, -3.0, 2.0)]
        [InlineData(-3.0, 2.0, 2.0)]
        [InlineData(3.0, -2.0, 3.0)]
        [InlineData(-2.0, 3.0, 3.0)]
        public static void MaxNumberTest64(double x, double y, double expectedResult)
        {
            AssertExtensions.Equal(expectedResult, NFloat.MaxNumber((NFloat)x, (NFloat)y), 0.0);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is64BitProcess))]
        [InlineData(double.NegativeInfinity, double.PositiveInfinity, double.NegativeInfinity)]
        [InlineData(double.PositiveInfinity, double.NegativeInfinity, double.NegativeInfinity)]
        [InlineData(double.MinValue, double.MaxValue, double.MinValue)]
        [InlineData(double.MaxValue, double.MinValue, double.MinValue)]
        [InlineData(double.NaN, double.NaN, double.NaN)]
        [InlineData(double.NaN, 1.0, 1.0)]
        [InlineData(1.0, double.NaN, 1.0)]
        [InlineData(double.PositiveInfinity, double.NaN, double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity, double.NaN, double.NegativeInfinity)]
        [InlineData(double.NaN, double.PositiveInfinity, double.PositiveInfinity)]
        [InlineData(double.NaN, double.NegativeInfinity, double.NegativeInfinity)]
        [InlineData(-0.0, 0.0, -0.0)]
        [InlineData(0.0, -0.0, -0.0)]
        [InlineData(2.0, -3.0, 2.0)]
        [InlineData(-3.0, 2.0, 2.0)]
        [InlineData(3.0, -2.0, -2.0)]
        [InlineData(-2.0, 3.0, -2.0)]
        public static void MinMagnitudeNumberTest64(double x, double y, double expectedResult)
        {
            AssertExtensions.Equal(expectedResult, NFloat.MinMagnitudeNumber((NFloat)x, (NFloat)y), 0.0);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is64BitProcess))]
        [InlineData(double.NegativeInfinity, double.PositiveInfinity, double.NegativeInfinity)]
        [InlineData(double.PositiveInfinity, double.NegativeInfinity, double.NegativeInfinity)]
        [InlineData(double.MinValue, double.MaxValue, double.MinValue)]
        [InlineData(double.MaxValue, double.MinValue, double.MinValue)]
        [InlineData(double.NaN, double.NaN, double.NaN)]
        [InlineData(double.NaN, 1.0, 1.0)]
        [InlineData(1.0, double.NaN, 1.0)]
        [InlineData(double.PositiveInfinity, double.NaN, double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity, double.NaN, double.NegativeInfinity)]
        [InlineData(double.NaN, double.PositiveInfinity, double.PositiveInfinity)]
        [InlineData(double.NaN, double.NegativeInfinity, double.NegativeInfinity)]
        [InlineData(-0.0, 0.0, -0.0)]
        [InlineData(0.0, -0.0, -0.0)]
        [InlineData(2.0, -3.0, -3.0)]
        [InlineData(-3.0, 2.0, -3.0)]
        [InlineData(3.0, -2.0, -2.0)]
        [InlineData(-2.0, 3.0, -2.0)]
        public static void MinNumberTest64(double x, double y, double expectedResult)
        {
            AssertExtensions.Equal(expectedResult, NFloat.MinNumber((NFloat)x, (NFloat)y), 0.0);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is32BitProcess))]
        [InlineData( float.NegativeInfinity, -1.0f,                   0.0f)]
        [InlineData(-3.14159265f,            -0.956786082f,           CrossPlatformMachineEpsilon32)]        // value: -(pi)
        [InlineData(-2.71828183f,            -0.934011964f,           CrossPlatformMachineEpsilon32)]        // value: -(e)
        [InlineData(-2.30258509f,            -0.9f,                   CrossPlatformMachineEpsilon32)]        // value: -(ln(10))
        [InlineData(-1.57079633f,            -0.792120424f,           CrossPlatformMachineEpsilon32)]        // value: -(pi / 2)
        [InlineData(-1.44269504f,            -0.763709912f,           CrossPlatformMachineEpsilon32)]        // value: -(log2(e))
        [InlineData(-1.41421356f,            -0.756883266f,           CrossPlatformMachineEpsilon32)]        // value: -(sqrt(2))
        [InlineData(-1.12837917f,            -0.676442736f,           CrossPlatformMachineEpsilon32)]        // value: -(2 / sqrt(pi))
        [InlineData(-1.0f,                   -0.632120559f,           CrossPlatformMachineEpsilon32)]
        [InlineData(-0.785398163f,           -0.544061872f,           CrossPlatformMachineEpsilon32)]        // value: -(pi / 4)
        [InlineData(-0.707106781f,           -0.506931309f,           CrossPlatformMachineEpsilon32)]        // value: -(1 / sqrt(2))
        [InlineData(-0.693147181f,           -0.5f,                   CrossPlatformMachineEpsilon32)]        // value: -(ln(2))
        [InlineData(-0.636619772f,           -0.470922192f,           CrossPlatformMachineEpsilon32)]        // value: -(2 / pi)
        [InlineData(-0.434294482f,           -0.352278515f,           CrossPlatformMachineEpsilon32)]        // value: -(log10(e))
        [InlineData(-0.318309886f,           -0.272622651f,           CrossPlatformMachineEpsilon32)]        // value: -(1 / pi)
        [InlineData(-0.0f,                    0.0f,                   0.0f)]
        [InlineData( float.NaN,               float.NaN,              0.0f)]
        [InlineData( 0.0f,                    0.0f,                   0.0f)]
        [InlineData( 0.318309886f,            0.374802227f,           CrossPlatformMachineEpsilon32)]        // value:  (1 / pi)
        [InlineData( 0.434294482f,            0.543873444f,           CrossPlatformMachineEpsilon32)]        // value:  (log10(e))
        [InlineData( 0.636619772f,            0.890081165f,           CrossPlatformMachineEpsilon32)]        // value:  (2 / pi)
        [InlineData( 0.693147181f,            1.0f,                   CrossPlatformMachineEpsilon32 * 10)]   // value:  (ln(2))
        [InlineData( 0.707106781f,            1.02811498f,            CrossPlatformMachineEpsilon32 * 10)]   // value:  (1 / sqrt(2))
        [InlineData( 0.785398163f,            1.19328005f,            CrossPlatformMachineEpsilon32 * 10)]   // value:  (pi / 4)
        [InlineData( 1.0f,                    1.71828183f,            CrossPlatformMachineEpsilon32 * 10)]
        [InlineData( 1.12837917f,             2.09064302f,            CrossPlatformMachineEpsilon32 * 10)]   // value:  (2 / sqrt(pi))
        [InlineData( 1.41421356f,             3.11325038f,            CrossPlatformMachineEpsilon32 * 10)]   // value:  (sqrt(2))
        [InlineData( 1.44269504f,             3.23208611f,            CrossPlatformMachineEpsilon32 * 10)]   // value:  (log2(e))
        [InlineData( 1.57079633f,             3.81047738f,            CrossPlatformMachineEpsilon32 * 10)]   // value:  (pi / 2)
        [InlineData( 2.30258509f,             9.0f,                   CrossPlatformMachineEpsilon32 * 10)]   // value:  (ln(10))
        [InlineData( 2.71828183f,             14.1542622f,            CrossPlatformMachineEpsilon32 * 100)]  // value:  (e)
        [InlineData( 3.14159265f,             22.1406926f,            CrossPlatformMachineEpsilon32 * 100)]  // value:  (pi)
        [InlineData( float.PositiveInfinity,  float.PositiveInfinity, 0.0f)]
        public static void ExpM1Test32(float value, float expectedResult, float allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, (float)NFloat.ExpM1(value), allowedVariance);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is32BitProcess))]
        [InlineData( float.NegativeInfinity, 0.0f,                   0.0f)]
        [InlineData(-3.14159265f,            0.113314732f,           CrossPlatformMachineEpsilon32)]        // value: -(pi)
        [InlineData(-2.71828183f,            0.151955223f,           CrossPlatformMachineEpsilon32)]        // value: -(e)
        [InlineData(-2.30258509f,            0.202699566f,           CrossPlatformMachineEpsilon32)]        // value: -(ln(10))
        [InlineData(-1.57079633f,            0.336622537f,           CrossPlatformMachineEpsilon32)]        // value: -(pi / 2)
        [InlineData(-1.44269504f,            0.367879441f,           CrossPlatformMachineEpsilon32)]        // value: -(log2(e))
        [InlineData(-1.41421356f,            0.375214227f,           CrossPlatformMachineEpsilon32)]        // value: -(sqrt(2))
        [InlineData(-1.12837917f,            0.457429347f,           CrossPlatformMachineEpsilon32)]        // value: -(2 / sqrt(pi))
        [InlineData(-1.0f,                   0.5f,                   CrossPlatformMachineEpsilon32)]
        [InlineData(-0.785398163f,           0.580191810f,           CrossPlatformMachineEpsilon32)]        // value: -(pi / 4)
        [InlineData(-0.707106781f,           0.612547327f,           CrossPlatformMachineEpsilon32)]        // value: -(1 / sqrt(2))
        [InlineData(-0.693147181f,           0.618503138f,           CrossPlatformMachineEpsilon32)]        // value: -(ln(2))
        [InlineData(-0.636619772f,           0.643218242f,           CrossPlatformMachineEpsilon32)]        // value: -(2 / pi)
        [InlineData(-0.434294482f,           0.740055574f,           CrossPlatformMachineEpsilon32)]        // value: -(log10(e))
        [InlineData(-0.318309886f,           0.802008879f,           CrossPlatformMachineEpsilon32)]        // value: -(1 / pi)
        [InlineData(-0.0f,                   1.0f,                   0.0f)]
        [InlineData( float.NaN,              float.NaN,              0.0f)]
        [InlineData( 0.0f,                   1.0f,                   0.0f)]
        [InlineData( 0.318309886f,           1.24686899f,            CrossPlatformMachineEpsilon32 * 10)]   // value:  (1 / pi)
        [InlineData( 0.434294482f,           1.35124987f,            CrossPlatformMachineEpsilon32 * 10)]   // value:  (log10(e))
        [InlineData( 0.636619772f,           1.55468228f,            CrossPlatformMachineEpsilon32 * 10)]   // value:  (2 / pi)
        [InlineData( 0.693147181f,           1.61680667f,            CrossPlatformMachineEpsilon32 * 10)]   // value:  (ln(2))
        [InlineData( 0.707106781f,           1.63252692f,            CrossPlatformMachineEpsilon32 * 10)]   // value:  (1 / sqrt(2))
        [InlineData( 0.785398163f,           1.72356793f,            CrossPlatformMachineEpsilon32 * 10)]   // value:  (pi / 4)
        [InlineData( 1.0f,                   2.0,                    CrossPlatformMachineEpsilon32 * 10)]
        [InlineData( 1.12837917f,            2.18612996f,            CrossPlatformMachineEpsilon32 * 10)]   // value:  (2 / sqrt(pi))
        [InlineData( 1.41421356f,            2.66514414f,            CrossPlatformMachineEpsilon32 * 10)]   // value:  (sqrt(2))
        [InlineData( 1.44269504f,            2.71828183f,            CrossPlatformMachineEpsilon32 * 10)]   // value:  (log2(e))
        [InlineData( 1.57079633f,            2.97068642f,            CrossPlatformMachineEpsilon32 * 10)]   // value:  (pi / 2)
        [InlineData( 2.30258509f,            4.93340967f,            CrossPlatformMachineEpsilon32 * 10)]   // value:  (ln(10))
        [InlineData( 2.71828183f,            6.58088599f,            CrossPlatformMachineEpsilon32 * 10)]   // value:  (e)
        [InlineData( 3.14159265f,            8.82497783f,            CrossPlatformMachineEpsilon32 * 10)]   // value:  (pi)
        [InlineData( float.PositiveInfinity, float.PositiveInfinity, 0.0f)]
        public static void Exp2Test32(float value, float expectedResult, float allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, (float)NFloat.Exp2(value), allowedVariance);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is32BitProcess))]
        [InlineData( float.NegativeInfinity, -1.0f,                   0.0f)]
        [InlineData(-3.14159265f,            -0.886685268f,           CrossPlatformMachineEpsilon32)]        // value: -(pi)
        [InlineData(-2.71828183f,            -0.848044777f,           CrossPlatformMachineEpsilon32)]        // value: -(e)
        [InlineData(-2.30258509f,            -0.797300434f,           CrossPlatformMachineEpsilon32)]        // value: -(ln(10))
        [InlineData(-1.57079633f,            -0.663377463f,           CrossPlatformMachineEpsilon32)]        // value: -(pi / 2)
        [InlineData(-1.44269504f,            -0.632120559f,           CrossPlatformMachineEpsilon32)]        // value: -(log2(e))
        [InlineData(-1.41421356f,            -0.624785773f,           CrossPlatformMachineEpsilon32)]        // value: -(sqrt(2))
        [InlineData(-1.12837917f,            -0.542570653f,           CrossPlatformMachineEpsilon32)]        // value: -(2 / sqrt(pi))
        [InlineData(-1.0f,                   -0.5f,                   CrossPlatformMachineEpsilon32)]
        [InlineData(-0.785398163f,           -0.419808190f,           CrossPlatformMachineEpsilon32)]        // value: -(pi / 4)
        [InlineData(-0.707106781f,           -0.387452673f,           CrossPlatformMachineEpsilon32)]        // value: -(1 / sqrt(2))
        [InlineData(-0.693147181f,           -0.381496862f,           CrossPlatformMachineEpsilon32)]        // value: -(ln(2))
        [InlineData(-0.636619772f,           -0.356781758f,           CrossPlatformMachineEpsilon32)]        // value: -(2 / pi)
        [InlineData(-0.434294482f,           -0.259944426f,           CrossPlatformMachineEpsilon32)]        // value: -(log10(e))
        [InlineData(-0.318309886f,           -0.197991121f,           CrossPlatformMachineEpsilon32)]        // value: -(1 / pi)
        [InlineData(-0.0f,                    0.0f,                   0.0f)]
        [InlineData( float.NaN,               float.NaN,              0.0f)]
        [InlineData( 0.0f,                    0.0f,                   0.0f)]
        [InlineData( 0.318309886f,            0.246868989f,           CrossPlatformMachineEpsilon32)]        // value:  (1 / pi)
        [InlineData( 0.434294482f,            0.351249873f,           CrossPlatformMachineEpsilon32)]        // value:  (log10(e))
        [InlineData( 0.636619772f,            0.554682275f,           CrossPlatformMachineEpsilon32)]        // value:  (2 / pi)
        [InlineData( 0.693147181f,            0.616806672f,           CrossPlatformMachineEpsilon32)]        // value:  (ln(2))
        [InlineData( 0.707106781f,            0.632526919f,           CrossPlatformMachineEpsilon32)]        // value:  (1 / sqrt(2))
        [InlineData( 0.785398163f,            0.723567934f,           CrossPlatformMachineEpsilon32)]        // value:  (pi / 4)
        [InlineData( 1.0f,                    1.0f,                   CrossPlatformMachineEpsilon32 * 10)]
        [InlineData( 1.12837917f,             1.18612996f,            CrossPlatformMachineEpsilon32 * 10)]   // value:  (2 / sqrt(pi))
        [InlineData( 1.41421356f,             1.66514414f,            CrossPlatformMachineEpsilon32 * 10)]   // value:  (sqrt(2))
        [InlineData( 1.44269504f,             1.71828183f,            CrossPlatformMachineEpsilon32 * 10)]   // value:  (log2(e))
        [InlineData( 1.57079633f,             1.97068642f,            CrossPlatformMachineEpsilon32 * 10)]   // value:  (pi / 2)
        [InlineData( 2.30258509f,             3.93340967f,            CrossPlatformMachineEpsilon32 * 10)]   // value:  (ln(10))
        [InlineData( 2.71828183f,             5.58088599f,            CrossPlatformMachineEpsilon32 * 10)]   // value:  (e)
        [InlineData( 3.14159265f,             7.82497783f,            CrossPlatformMachineEpsilon32 * 10)]   // value:  (pi)
        [InlineData( float.PositiveInfinity,  float.PositiveInfinity, 0.0f)]
        public static void Exp2M1Test32(float value, float expectedResult, float allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, (float)NFloat.Exp2M1(value), allowedVariance);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is32BitProcess))]
        [InlineData( float.NegativeInfinity, 0.0,                    0.0f)]
        [InlineData(-3.14159265f,            0.000721784159f,        CrossPlatformMachineEpsilon32 / 1000)]  // value: -(pi)
        [InlineData(-2.71828183f,            0.00191301410f,         CrossPlatformMachineEpsilon32 / 100)]   // value: -(e)
        [InlineData(-2.30258509f,            0.00498212830f,         CrossPlatformMachineEpsilon32 / 100)]   // value: -(ln(10))
        [InlineData(-1.57079633f,            0.0268660410f,          CrossPlatformMachineEpsilon32 / 10)]    // value: -(pi / 2)
        [InlineData(-1.44269504f,            0.0360831928f,          CrossPlatformMachineEpsilon32 / 10)]    // value: -(log2(e))
        [InlineData(-1.41421356f,            0.0385288847f,          CrossPlatformMachineEpsilon32 / 10)]    // value: -(sqrt(2))
        [InlineData(-1.12837917f,            0.0744082059f,          CrossPlatformMachineEpsilon32 / 10)]    // value: -(2 / sqrt(pi))
        [InlineData(-1.0f,                   0.1f,                   CrossPlatformMachineEpsilon32)]
        [InlineData(-0.785398163f,           0.163908636f,           CrossPlatformMachineEpsilon32)]         // value: -(pi / 4)
        [InlineData(-0.707106781f,           0.196287760f,           CrossPlatformMachineEpsilon32)]         // value: -(1 / sqrt(2))
        [InlineData(-0.693147181f,           0.202699566f,           CrossPlatformMachineEpsilon32)]         // value: -(ln(2))
        [InlineData(-0.636619772f,           0.230876765f,           CrossPlatformMachineEpsilon32)]         // value: -(2 / pi)
        [InlineData(-0.434294482f,           0.367879441f,           CrossPlatformMachineEpsilon32)]         // value: -(log10(e))
        [InlineData(-0.318309886f,           0.480496373f,           CrossPlatformMachineEpsilon32)]         // value: -(1 / pi)
        [InlineData(-0.0f,                   1.0f,                   0.0f)]
        [InlineData( float.NaN,              float.NaN,              0.0f)]
        [InlineData( 0.0f,                   1.0f,                   0.0f)]
        [InlineData( 0.318309886f,           2.08118116f,            CrossPlatformMachineEpsilon32 * 10)]    // value:  (1 / pi)
        [InlineData( 0.434294482f,           2.71828183f,            CrossPlatformMachineEpsilon32 * 10)]    // value:  (log10(e))
        [InlineData( 0.636619772f,           4.33131503f,            CrossPlatformMachineEpsilon32 * 10)]    // value:  (2 / pi)
        [InlineData( 0.693147181f,           4.93340967f,            CrossPlatformMachineEpsilon32 * 10)]    // value:  (ln(2))
        [InlineData( 0.707106781f,           5.09456117f,            CrossPlatformMachineEpsilon32 * 10)]    // value:  (1 / sqrt(2))
        [InlineData( 0.785398163f,           6.10095980f,            CrossPlatformMachineEpsilon32 * 10)]    // value:  (pi / 4)
        [InlineData( 1.0f,                   10.0f,                  CrossPlatformMachineEpsilon32 * 100)]
        [InlineData( 1.12837917f,            13.4393779f,            CrossPlatformMachineEpsilon32 * 100)]   // value:  (2 / sqrt(pi))
        [InlineData( 1.41421356f,            25.9545535f,            CrossPlatformMachineEpsilon32 * 100)]   // value:  (sqrt(2))
        [InlineData( 1.44269504f,            27.7137338f,            CrossPlatformMachineEpsilon32 * 100)]   // value:  (log2(e))
        [InlineData( 1.57079633f,            37.2217105f,            CrossPlatformMachineEpsilon32 * 100)]   // value:  (pi / 2)
        [InlineData( 2.30258509f,            200.717432f,            CrossPlatformMachineEpsilon32 * 1000)]  // value:  (ln(10))
        [InlineData( 2.71828183f,            522.735300f,            CrossPlatformMachineEpsilon32 * 1000)]  // value:  (e)
        [InlineData( 3.14159265f,            1385.45573f,            CrossPlatformMachineEpsilon32 * 10000)] // value:  (pi)
        [InlineData( float.PositiveInfinity, float.PositiveInfinity, 0.0f)]
        public static void Exp10Test32(float value, float expectedResult, float allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, (float)NFloat.Exp10(value), allowedVariance);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is32BitProcess))]
        [InlineData( float.NegativeInfinity, -1.0f,                   0.0f)]
        [InlineData(-3.14159265f,            -0.999278216f,           CrossPlatformMachineEpsilon32)]         // value: -(pi)
        [InlineData(-2.71828183f,            -0.998086986f,           CrossPlatformMachineEpsilon32)]         // value: -(e)
        [InlineData(-2.30258509f,            -0.995017872f,           CrossPlatformMachineEpsilon32)]         // value: -(ln(10))
        [InlineData(-1.57079633f,            -0.973133959f,           CrossPlatformMachineEpsilon32)]         // value: -(pi / 2)
        [InlineData(-1.44269504f,            -0.963916807f,           CrossPlatformMachineEpsilon32)]         // value: -(log2(e))
        [InlineData(-1.41421356f,            -0.961471115f,           CrossPlatformMachineEpsilon32)]         // value: -(sqrt(2))
        [InlineData(-1.12837917f,            -0.925591794f,           CrossPlatformMachineEpsilon32)]         // value: -(2 / sqrt(pi))
        [InlineData(-1.0f,                   -0.9f,                   CrossPlatformMachineEpsilon32)]
        [InlineData(-0.785398163f,           -0.836091364f,           CrossPlatformMachineEpsilon32)]         // value: -(pi / 4)
        [InlineData(-0.707106781f,           -0.803712240f,           CrossPlatformMachineEpsilon32)]         // value: -(1 / sqrt(2))
        [InlineData(-0.693147181f,           -0.797300434f,           CrossPlatformMachineEpsilon32)]         // value: -(ln(2))
        [InlineData(-0.636619772f,           -0.769123235f,           CrossPlatformMachineEpsilon32)]         // value: -(2 / pi)
        [InlineData(-0.434294482f,           -0.632120559f,           CrossPlatformMachineEpsilon32)]         // value: -(log10(e))
        [InlineData(-0.318309886f,           -0.519503627f,           CrossPlatformMachineEpsilon32)]         // value: -(1 / pi)
        [InlineData(-0.0f,                    0.0f,                   0.0f)]
        [InlineData( float.NaN,               float.NaN,              0.0f)]
        [InlineData( 0.0f,                    0.0f,                   0.0f)]
        [InlineData( 0.318309886f,            1.08118116f,            CrossPlatformMachineEpsilon32 * 10)]    // value:  (1 / pi)
        [InlineData( 0.434294482f,            1.71828183f,            CrossPlatformMachineEpsilon32 * 10)]    // value:  (log10(e))
        [InlineData( 0.636619772f,            3.33131503f,            CrossPlatformMachineEpsilon32 * 10)]    // value:  (2 / pi)
        [InlineData( 0.693147181f,            3.93340967f,            CrossPlatformMachineEpsilon32 * 10)]    // value:  (ln(2))
        [InlineData( 0.707106781f,            4.09456117f,            CrossPlatformMachineEpsilon32 * 10)]    // value:  (1 / sqrt(2))
        [InlineData( 0.785398163f,            5.10095980f,            CrossPlatformMachineEpsilon32 * 10)]    // value:  (pi / 4)
        [InlineData( 1.0f,                    9.0,                    CrossPlatformMachineEpsilon32 * 10)]
        [InlineData( 1.12837917f,             12.4393779f,            CrossPlatformMachineEpsilon32 * 100)]   // value:  (2 / sqrt(pi))
        [InlineData( 1.41421356f,             24.9545535f,            CrossPlatformMachineEpsilon32 * 100)]   // value:  (sqrt(2))
        [InlineData( 1.44269504f,             26.7137338f,            CrossPlatformMachineEpsilon32 * 100)]   // value:  (log2(e))
        [InlineData( 1.57079633f,             36.2217105f,            CrossPlatformMachineEpsilon32 * 100)]   // value:  (pi / 2)
        [InlineData( 2.30258509f,             199.717432f,            CrossPlatformMachineEpsilon32 * 1000)]  // value:  (ln(10))
        [InlineData( 2.71828183f,             521.735300f,            CrossPlatformMachineEpsilon32 * 1000)]  // value:  (e)
        [InlineData( 3.14159265f,             1384.45573f,            CrossPlatformMachineEpsilon32 * 10000)] // value:  (pi)
        [InlineData( float.PositiveInfinity,  float.PositiveInfinity, 0.0)]
        public static void Exp10M1Test32(float value, float expectedResult, float allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, (float)NFloat.Exp10M1(value), allowedVariance);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is32BitProcess))]
        [InlineData( float.NegativeInfinity,  float.NaN,              0.0f)]
        [InlineData(-3.14159265f,             float.NaN,              0.0f)]                              //                              value: -(pi)
        [InlineData(-2.71828183f,             float.NaN,              0.0f)]                              //                              value: -(e)
        [InlineData(-1.41421356f,             float.NaN,              0.0f)]                              //                              value: -(sqrt(2))
        [InlineData( float.NaN,               float.NaN,              0.0f)]
        [InlineData(-1.0f,                    float.NegativeInfinity, 0.0f)]
        [InlineData(-0.956786082f,           -3.14159265f,            CrossPlatformMachineEpsilon32 * 10)]  // expected: -(pi)
        [InlineData(-0.934011964f,           -2.71828183f,            CrossPlatformMachineEpsilon32 * 10)]  // expected: -(e)
        [InlineData(-0.9f,                   -2.30258509f,            CrossPlatformMachineEpsilon32 * 10)]  // expected: -(ln(10))
        [InlineData(-0.792120424f,           -1.57079633f,            CrossPlatformMachineEpsilon32 * 10)]  // expected: -(pi / 2)
        [InlineData(-0.763709912f,           -1.44269504f,            CrossPlatformMachineEpsilon32 * 10)]  // expected: -(log2(e))
        [InlineData(-0.756883266f,           -1.41421356f,            CrossPlatformMachineEpsilon32 * 10)]  // expected: -(sqrt(2))
        [InlineData(-0.676442736f,           -1.12837917f,            CrossPlatformMachineEpsilon32 * 10)]  // expected: -(2 / sqrt(pi))
        [InlineData(-0.632120559f,           -1.0f,                   CrossPlatformMachineEpsilon32 * 10)]
        [InlineData(-0.544061872f,           -0.785398163f,           CrossPlatformMachineEpsilon32)]       // expected: -(pi / 4)
        [InlineData(-0.506931309f,           -0.707106781f,           CrossPlatformMachineEpsilon32)]       // expected: -(1 / sqrt(2))
        [InlineData(-0.5f,                   -0.693147181f,           CrossPlatformMachineEpsilon32)]       // expected: -(ln(2))
        [InlineData(-0.470922192f,           -0.636619772f,           CrossPlatformMachineEpsilon32)]       // expected: -(2 / pi)
        [InlineData(-0.0f,                    0.0f,                   0.0f)]
        [InlineData( 0.0f,                    0.0f,                   0.0f)]
        [InlineData( 0.374802227f,            0.318309886f,           CrossPlatformMachineEpsilon32)]       // expected:  (1 / pi)
        [InlineData( 0.543873444f,            0.434294482f,           CrossPlatformMachineEpsilon32)]       // expected:  (log10(e))
        [InlineData( 0.890081165f,            0.636619772f,           CrossPlatformMachineEpsilon32)]       // expected:  (2 / pi)
        [InlineData( 1.0f,                    0.693147181f,           CrossPlatformMachineEpsilon32)]       // expected:  (ln(2))
        [InlineData( 1.02811498f,             0.707106781f,           CrossPlatformMachineEpsilon32)]       // expected:  (1 / sqrt(2))
        [InlineData( 1.19328005f,             0.785398163f,           CrossPlatformMachineEpsilon32)]       // expected:  (pi / 4)
        [InlineData( 1.71828183f,             1.0f,                   CrossPlatformMachineEpsilon32 * 10)]
        [InlineData( 2.09064302f,             1.12837917f,            CrossPlatformMachineEpsilon32 * 10)]  // expected:  (2 / sqrt(pi))
        [InlineData( 3.11325038f,             1.41421356f,            CrossPlatformMachineEpsilon32 * 10)]  // expected:  (sqrt(2))
        [InlineData( 3.23208611f,             1.44269504f,            CrossPlatformMachineEpsilon32 * 10)]  // expected:  (log2(e))
        [InlineData( 3.81047738f,             1.57079633f,            CrossPlatformMachineEpsilon32 * 10)]  // expected:  (pi / 2)
        [InlineData( 9.0f,                    2.30258509f,            CrossPlatformMachineEpsilon32 * 10)]  // expected:  (ln(10))
        [InlineData( 14.1542622f,             2.71828183f,            CrossPlatformMachineEpsilon32 * 10)]  // expected:  (e)
        [InlineData( 22.1406926f,             3.14159265f,            CrossPlatformMachineEpsilon32 * 10)]  // expected:  (pi)
        [InlineData( float.PositiveInfinity,  float.PositiveInfinity, 0.0f)]
        public static void LogP1Test32(float value, float expectedResult, float allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, (float)NFloat.LogP1(value), allowedVariance);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is32BitProcess))]
        [InlineData( float.NegativeInfinity,  float.NaN,              0.0f)]
        [InlineData( float.NaN,               float.NaN,              0.0f)]
        [InlineData(-1.0f,                    float.NegativeInfinity, 0.0f)]
        [InlineData(-0.886685268f,           -3.14159265f,            CrossPlatformMachineEpsilon32 * 10)]  // expected: -(pi)
        [InlineData(-0.848044777f,           -2.71828183f,            CrossPlatformMachineEpsilon32 * 10)]  // expected: -(e)
        [InlineData(-0.797300434f,           -2.30258509f,            CrossPlatformMachineEpsilon32 * 10)]  // expected: -(ln(10))
        [InlineData(-0.663377463f,           -1.57079633f,            CrossPlatformMachineEpsilon32 * 10)]  // expected: -(pi / 2)
        [InlineData(-0.632120559f,           -1.44269504f,            CrossPlatformMachineEpsilon32 * 10)]  // expected: -(log2(e))
        [InlineData(-0.624785773f,           -1.41421356f,            CrossPlatformMachineEpsilon32 * 10)]  // expected: -(sqrt(2))
        [InlineData(-0.542570653f,           -1.12837917f,            CrossPlatformMachineEpsilon32 * 10)]  // expected: -(2 / sqrt(pi))
        [InlineData(-0.5f,                   -1.0f,                   CrossPlatformMachineEpsilon32 * 10)]
        [InlineData(-0.419808190f,           -0.785398163f,           CrossPlatformMachineEpsilon32)]       // expected: -(pi / 4)
        [InlineData(-0.387452673f,           -0.707106781f,           CrossPlatformMachineEpsilon32)]       // expected: -(1 / sqrt(2))
        [InlineData(-0.381496862f,           -0.693147181f,           CrossPlatformMachineEpsilon32)]       // expected: -(ln(2))
        [InlineData(-0.356781758f,           -0.636619772f,           CrossPlatformMachineEpsilon32)]       // expected: -(2 / pi)
        [InlineData(-0.259944426f,           -0.434294482f,           CrossPlatformMachineEpsilon32)]       // expected: -(log10(e))
        [InlineData(-0.197991121f,           -0.318309886f,           CrossPlatformMachineEpsilon32)]       // expected: -(1 / pi)
        [InlineData(-0.0f,                    0.0f,                   0.0f)]
        [InlineData( 0.0f,                    0.0f,                   0.0f)]
        [InlineData( 0.246868989f,            0.318309886f,           CrossPlatformMachineEpsilon32)]       // expected:  (1 / pi)
        [InlineData( 0.351249873f,            0.434294482f,           CrossPlatformMachineEpsilon32)]       // expected:  (log10(e))
        [InlineData( 0.554682275f,            0.636619772f,           CrossPlatformMachineEpsilon32)]       // expected:  (2 / pi)
        [InlineData( 0.616806672f,            0.693147181f,           CrossPlatformMachineEpsilon32)]       // expected:  (ln(2))
        [InlineData( 0.632526919f,            0.707106781f,           CrossPlatformMachineEpsilon32)]       // expected:  (1 / sqrt(2))
        [InlineData( 0.723567934f,            0.785398163f,           CrossPlatformMachineEpsilon32)]       // expected:  (pi / 4)
        [InlineData( 1.0f,                    1.0f,                   CrossPlatformMachineEpsilon32 * 10)]
        [InlineData( 1.18612996f,             1.12837917f,            CrossPlatformMachineEpsilon32 * 10)]  // expected:  (2 / sqrt(pi))
        [InlineData( 1.66514414f,             1.41421356f,            CrossPlatformMachineEpsilon32 * 10)]  // expected:  (sqrt(2))
        [InlineData( 1.71828183f,             1.44269504f,            CrossPlatformMachineEpsilon32 * 10)]  // expected:  (log2(e))
        [InlineData( 1.97068642f,             1.57079633f,            CrossPlatformMachineEpsilon32 * 10)]  // expected:  (pi / 2)
        [InlineData( 3.93340967f,             2.30258509f,            CrossPlatformMachineEpsilon32 * 10)]  // expected:  (ln(10))
        [InlineData( 5.58088599f,             2.71828183f,            CrossPlatformMachineEpsilon32 * 10)]  // expected:  (e)
        [InlineData( 7.82497783f,             3.14159265f,            CrossPlatformMachineEpsilon32 * 10)]  // expected:  (pi)
        [InlineData( float.PositiveInfinity,  float.PositiveInfinity, 0.0f)]
        public static void Log2P1Test32(float value, float expectedResult, float allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, (float)NFloat.Log2P1(value), allowedVariance);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is32BitProcess))]
        [InlineData( float.NegativeInfinity,  float.NaN,              0.0f)]
        [InlineData(-3.14159265f,             float.NaN,              0.0f)]                              //                              value: -(pi)
        [InlineData(-2.71828183f,             float.NaN,              0.0f)]                              //                              value: -(e)
        [InlineData(-1.41421356f,             float.NaN,              0.0f)]                              //                              value: -(sqrt(2))
        [InlineData( float.NaN,               float.NaN,              0.0f)]
        [InlineData(-1.0f,                    float.NegativeInfinity, 0.0f)]
        [InlineData(-0.998086986f,           -2.71828183f,            CrossPlatformMachineEpsilon32 * 10)]  // expected: -(e)
        [InlineData(-0.995017872f,           -2.30258509f,            CrossPlatformMachineEpsilon32 * 10)]  // expected: -(ln(10))
        [InlineData(-0.973133959f,           -1.57079633f,            CrossPlatformMachineEpsilon32 * 10)]  // expected: -(pi / 2)
        [InlineData(-0.963916807f,           -1.44269504f,            CrossPlatformMachineEpsilon32 * 10)]  // expected: -(log2(e))
        [InlineData(-0.961471115f,           -1.41421356f,            CrossPlatformMachineEpsilon32 * 10)]  // expected: -(sqrt(2))
        [InlineData(-0.925591794f,           -1.12837917f,            CrossPlatformMachineEpsilon32 * 10)]  // expected: -(2 / sqrt(pi))
        [InlineData(-0.9f,                   -1.0f,                   CrossPlatformMachineEpsilon32 * 10)]
        [InlineData(-0.836091364f,           -0.785398163f,           CrossPlatformMachineEpsilon32)]       // expected: -(pi / 4)
        [InlineData(-0.803712240f,           -0.707106781f,           CrossPlatformMachineEpsilon32)]       // expected: -(1 / sqrt(2))
        [InlineData(-0.797300434f,           -0.693147181f,           CrossPlatformMachineEpsilon32)]       // expected: -(ln(2))
        [InlineData(-0.769123235f,           -0.636619772f,           CrossPlatformMachineEpsilon32)]       // expected: -(2 / pi)
        [InlineData(-0.632120559f,           -0.434294482f,           CrossPlatformMachineEpsilon32)]       // expected: -(log10(e))
        [InlineData(-0.519503627f,           -0.318309886f,           CrossPlatformMachineEpsilon32)]       // expected: -(1 / pi)
        [InlineData(-0.0f,                    0.0f,                   0.0f)]
        [InlineData( 0.0f,                    0.0f,                   0.0f)]
        [InlineData( 1.08118116f,             0.318309886f,           CrossPlatformMachineEpsilon32)]       // expected:  (1 / pi)
        [InlineData( 1.71828183f,             0.434294482f,           CrossPlatformMachineEpsilon32)]       // expected:  (log10(e))        value: (e)
        [InlineData( 3.33131503f,             0.636619772f,           CrossPlatformMachineEpsilon32)]       // expected:  (2 / pi)
        [InlineData( 3.93340967f,             0.693147181f,           CrossPlatformMachineEpsilon32)]       // expected:  (ln(2))
        [InlineData( 4.09456117f,             0.707106781f,           CrossPlatformMachineEpsilon32)]       // expected:  (1 / sqrt(2))
        [InlineData( 5.10095980f,             0.785398163f,           CrossPlatformMachineEpsilon32)]       // expected:  (pi / 4)
        [InlineData( 9.0f,                    1.0f,                   CrossPlatformMachineEpsilon32 * 10)]
        [InlineData( 12.4393779f,             1.12837917f,            CrossPlatformMachineEpsilon32 * 10)]  // expected:  (2 / sqrt(pi))
        [InlineData( 24.9545535f,             1.41421356f,            CrossPlatformMachineEpsilon32 * 10)]  // expected:  (sqrt(2))
        [InlineData( 26.7137338f,             1.44269504f,            CrossPlatformMachineEpsilon32 * 10)]  // expected:  (log2(e))
        [InlineData( 36.2217105f,             1.57079633f,            CrossPlatformMachineEpsilon32 * 10)]  // expected:  (pi / 2)
        [InlineData( 199.717432f,             2.30258509f,            CrossPlatformMachineEpsilon32 * 10)]  // expected:  (ln(10))
        [InlineData( 521.735300f,             2.71828183f,            CrossPlatformMachineEpsilon32 * 10)]  // expected:  (e)
        [InlineData( 1384.45573f,             3.14159265f,            CrossPlatformMachineEpsilon32 * 10)]  // expected:  (pi)
        [InlineData( float.PositiveInfinity,  float.PositiveInfinity, 0.0f)]
        public static void Log10P1Test32(float value, float expectedResult, float allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, (float)NFloat.Log10P1(value), allowedVariance);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is32BitProcess))]
        [InlineData(float.NaN,              float.NaN,              float.NaN,              0.0f)]
        [InlineData(float.NaN,              0.0f,                   float.NaN,              0.0f)]
        [InlineData(float.NaN,              1.0f,                   float.NaN,              0.0f)]
        [InlineData(float.NaN,              2.71828183f,            float.NaN,              0.0f)]
        [InlineData(float.NaN,              10.0f,                  float.NaN,              0.0f)]
        [InlineData(0.0f,                   0.0f,                   0.0f,                   0.0f)]
        [InlineData(0.0f,                   1.0f,                   1.0f,                   0.0f)]
        [InlineData(0.0f,                   1.57079633f,            1.57079633f,            0.0f)]
        [InlineData(0.0f,                   2.0f,                   2.0f,                   0.0f)]
        [InlineData(0.0f,                   2.71828183f,            2.71828183f,            0.0f)]
        [InlineData(0.0f,                   3.0f,                   3.0f,                   0.0f)]
        [InlineData(0.0f,                   10.0f,                  10.0f,                  0.0f)]
        [InlineData(1.0f,                   1.0f,                   1.41421356f,            CrossPlatformMachineEpsilon32 * 10)]
        [InlineData(2.71828183f,            0.318309886f,           2.73685536f,            CrossPlatformMachineEpsilon32 * 10)]   // x: (e)   y: (1 / pi)
        [InlineData(2.71828183f,            0.434294482f,           2.75275640f,            CrossPlatformMachineEpsilon32 * 10)]   // x: (e)   y: (log10(e))
        [InlineData(2.71828183f,            0.636619772f,           2.79183467f,            CrossPlatformMachineEpsilon32 * 10)]   // x: (e)   y: (2 / pi)
        [InlineData(2.71828183f,            0.693147181f,           2.80526454f,            CrossPlatformMachineEpsilon32 * 10)]   // x: (e)   y: (ln(2))
        [InlineData(2.71828183f,            0.707106781f,           2.80874636f,            CrossPlatformMachineEpsilon32 * 10)]   // x: (e)   y: (1 / sqrt(2))
        [InlineData(2.71828183f,            0.785398163f,           2.82947104f,            CrossPlatformMachineEpsilon32 * 10)]   // x: (e)   y: (pi / 4)
        [InlineData(2.71828183f,            1.0f,                   2.89638673f,            CrossPlatformMachineEpsilon32 * 10)]   // x: (e)
        [InlineData(2.71828183f,            1.12837917f,            2.94317781f,            CrossPlatformMachineEpsilon32 * 10)]   // x: (e)   y: (2 / sqrt(pi))
        [InlineData(2.71828183f,            1.41421356f,            3.06415667f,            CrossPlatformMachineEpsilon32 * 10)]   // x: (e)   y: (sqrt(2))
        [InlineData(2.71828183f,            1.44269504f,            3.07740558f,            CrossPlatformMachineEpsilon32 * 10)]   // x: (e)   y: (log2(e))
        [InlineData(2.71828183f,            1.57079633f,            3.13949951f,            CrossPlatformMachineEpsilon32 * 10)]   // x: (e)   y: (pi / 2)
        [InlineData(2.71828183f,            2.30258509f,            3.56243656f,            CrossPlatformMachineEpsilon32 * 10)]   // x: (e)   y: (ln(10))
        [InlineData(2.71828183f,            2.71828183f,            3.84423103f,            CrossPlatformMachineEpsilon32 * 10)]   // x: (e)   y: (e)
        [InlineData(2.71828183f,            3.14159265f,            4.15435440f,            CrossPlatformMachineEpsilon32 * 10)]   // x: (e)   y: (pi)
        [InlineData(10.0f,                  0.318309886f,           10.0050648f,            CrossPlatformMachineEpsilon32 * 100)]  //          y: (1 / pi)
        [InlineData(10.0f,                  0.434294482f,           10.0094261f,            CrossPlatformMachineEpsilon32 * 100)]  //          y: (log10(e))
        [InlineData(10.0f,                  0.636619772f,           10.0202437f,            CrossPlatformMachineEpsilon32 * 100)]  //          y: (2 / pi)
        [InlineData(10.0f,                  0.693147181f,           10.0239939f,            CrossPlatformMachineEpsilon32 * 100)]  //          y: (ln(2))
        [InlineData(10.0f,                  0.707106781f,           10.0249688f,            CrossPlatformMachineEpsilon32 * 100)]  //          y: (1 / sqrt(2))
        [InlineData(10.0f,                  0.785398163f,           10.0307951f,            CrossPlatformMachineEpsilon32 * 100)]  //          y: (pi / 4)
        [InlineData(10.0f,                  1.0f,                   10.0498756f,            CrossPlatformMachineEpsilon32 * 100)]  //
        [InlineData(10.0f,                  1.12837917f,            10.0634606f,            CrossPlatformMachineEpsilon32 * 100)]  //          y: (2 / sqrt(pi))
        [InlineData(10.0f,                  1.41421356f,            10.0995049f,            CrossPlatformMachineEpsilon32 * 100)]  //          y: (sqrt(2))
        [InlineData(10.0f,                  1.44269504f,            10.1035325f,            CrossPlatformMachineEpsilon32 * 100)]  //          y: (log2(e))
        [InlineData(10.0f,                  1.57079633f,            10.1226183f,            CrossPlatformMachineEpsilon32 * 100)]  //          y: (pi / 2)
        [InlineData(10.0f,                  2.30258509f,            10.2616713f,            CrossPlatformMachineEpsilon32 * 100)]  //          y: (ln(10))
        [InlineData(10.0f,                  2.71828183f,            10.3628691f,            CrossPlatformMachineEpsilon32 * 100)]  //          y: (e)
        [InlineData(10.0f,                  3.14159265f,            10.4818703f,            CrossPlatformMachineEpsilon32 * 100)]  //          y: (pi)
        [InlineData(float.PositiveInfinity, float.NaN,              float.PositiveInfinity, 0.0f)]
        [InlineData(float.PositiveInfinity, 0.0f,                   float.PositiveInfinity, 0.0f)]
        [InlineData(float.PositiveInfinity, 1.0f,                   float.PositiveInfinity, 0.0f)]
        [InlineData(float.PositiveInfinity, 2.71828183f,            float.PositiveInfinity, 0.0f)]
        [InlineData(float.PositiveInfinity, 10.0f,                  float.PositiveInfinity, 0.0f)]
        [InlineData(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity, 0.0f)]
        public static void Hypot32(float x, float y, float expectedResult, float allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, NFloat.Hypot(-x, -y), allowedVariance);
            AssertExtensions.Equal(expectedResult, NFloat.Hypot(-x, +y), allowedVariance);
            AssertExtensions.Equal(expectedResult, NFloat.Hypot(+x, -y), allowedVariance);
            AssertExtensions.Equal(expectedResult, NFloat.Hypot(+x, +y), allowedVariance);

            AssertExtensions.Equal(expectedResult, NFloat.Hypot(-y, -x), allowedVariance);
            AssertExtensions.Equal(expectedResult, NFloat.Hypot(-y, +x), allowedVariance);
            AssertExtensions.Equal(expectedResult, NFloat.Hypot(+y, -x), allowedVariance);
            AssertExtensions.Equal(expectedResult, NFloat.Hypot(+y, +x), allowedVariance);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is32BitProcess))]
        [InlineData( float.NegativeInfinity, -5, -0.0f,                   0.0f)]
        [InlineData( float.NegativeInfinity, -4,  float.NaN,              0.0f)]
        [InlineData( float.NegativeInfinity, -3, -0.0f,                   0.0f)]
        [InlineData( float.NegativeInfinity, -2,  float.NaN,              0.0f)]
        [InlineData( float.NegativeInfinity, -1, -0.0f,                   0.0f)]
        [InlineData( float.NegativeInfinity,  0,  float.NaN,              0.0f)]
        [InlineData( float.NegativeInfinity,  1,  float.NegativeInfinity, 0.0f)]
        [InlineData( float.NegativeInfinity,  2,  float.NaN,              0.0f)]
        [InlineData( float.NegativeInfinity,  3,  float.NegativeInfinity, 0.0f)]
        [InlineData( float.NegativeInfinity,  4,  float.NaN,              0.0f)]
        [InlineData( float.NegativeInfinity,  5,  float.NegativeInfinity, 0.0f)]
        [InlineData(-2.71828183f,            -5, -0.81873075f,            CrossPlatformMachineEpsilon32 * 10)]
        [InlineData(-2.71828183f,            -4,  float.NaN,              0.0f)]
        [InlineData(-2.71828183f,            -3, -0.71653131f,            CrossPlatformMachineEpsilon32 * 10)]
        [InlineData(-2.71828183f,            -2,  float.NaN,              0.0f)]
        [InlineData(-2.71828183f,            -1, -0.36787944f,            CrossPlatformMachineEpsilon32 * 10)]
        [InlineData(-2.71828183f,             0,  float.NaN,              0.0f)]
        [InlineData(-2.71828183f,             1, -2.71828183f,            CrossPlatformMachineEpsilon32 * 10)]
        [InlineData(-2.71828183f,             2,  float.NaN,              0.0f)]
        [InlineData(-2.71828183f,             3, -1.39561243f,            CrossPlatformMachineEpsilon32 * 10)]
        [InlineData(-2.71828183f,             4,  float.NaN,              0.0f)]
        [InlineData(-2.71828183f,             5, -1.22140276f,            CrossPlatformMachineEpsilon32 * 10)]
        [InlineData(-1.0f,                   -5, -1.0f,                   0.0f)]
        [InlineData(-1.0f,                   -4,  float.NaN,              0.0f)]
        [InlineData(-1.0f,                   -3, -1.0f,                   0.0f)]
        [InlineData(-1.0f,                   -2,  float.NaN,              0.0f)]
        [InlineData(-1.0f,                   -1, -1.0f,                   0.0f)]
        [InlineData(-1.0f,                    0,  float.NaN,              0.0f)]
        [InlineData(-1.0f,                    1, -1.0f,                   0.0f)]
        [InlineData(-1.0f,                    2,  float.NaN,              0.0f)]
        [InlineData(-1.0f,                    3, -1.0f,                   0.0f)]
        [InlineData(-1.0f,                    4,  float.NaN,              0.0f)]
        [InlineData(-1.0f,                    5, -1.0f,                   0.0f)]
        [InlineData(-0.0f,                   -5,  float.NegativeInfinity, 0.0f)]
        [InlineData(-0.0f,                   -4,  float.PositiveInfinity, 0.0f)]
        [InlineData(-0.0f,                   -3,  float.NegativeInfinity, 0.0f)]
        [InlineData(-0.0f,                   -2,  float.PositiveInfinity, 0.0f)]
        [InlineData(-0.0f,                   -1,  float.NegativeInfinity, 0.0f)]
        [InlineData(-0.0f,                    0,  float.NaN,              0.0f)]
        [InlineData(-0.0f,                    1, -0.0f,                   0.0f)]
        [InlineData(-0.0f,                    2,  0.0f,                   0.0f)]
        [InlineData(-0.0f,                    3, -0.0f,                   0.0f)]
        [InlineData(-0.0f,                    4,  0.0f,                   0.0f)]
        [InlineData(-0.0f,                    5, -0.0f,                   0.0f)]
        [InlineData( float.NaN,              -5,  float.NaN,              0.0f)]
        [InlineData( float.NaN,              -4,  float.NaN,              0.0f)]
        [InlineData( float.NaN,              -3,  float.NaN,              0.0f)]
        [InlineData( float.NaN,              -2,  float.NaN,              0.0f)]
        [InlineData( float.NaN,              -1,  float.NaN,              0.0f)]
        [InlineData( float.NaN,               0,  float.NaN,              0.0f)]
        [InlineData( float.NaN,               1,  float.NaN,              0.0f)]
        [InlineData( float.NaN,               2,  float.NaN,              0.0f)]
        [InlineData( float.NaN,               3,  float.NaN,              0.0f)]
        [InlineData( float.NaN,               4,  float.NaN,              0.0f)]
        [InlineData( float.NaN,               5,  float.NaN,              0.0f)]
        [InlineData( 0.0f,                   -5,  float.PositiveInfinity, 0.0f)]
        [InlineData( 0.0f,                   -4,  float.PositiveInfinity, 0.0f)]
        [InlineData( 0.0f,                   -3,  float.PositiveInfinity, 0.0f)]
        [InlineData( 0.0f,                   -2,  float.PositiveInfinity, 0.0f)]
        [InlineData( 0.0f,                   -1,  float.PositiveInfinity, 0.0f)]
        [InlineData( 0.0f,                    0,  float.NaN,              0.0f)]
        [InlineData( 0.0f,                    1,  0.0f,                   0.0f)]
        [InlineData( 0.0f,                    2,  0.0f,                   0.0f)]
        [InlineData( 0.0f,                    3,  0.0f,                   0.0f)]
        [InlineData( 0.0f,                    4,  0.0f,                   0.0f)]
        [InlineData( 0.0f,                    5,  0.0f,                   0.0f)]
        [InlineData( 1.0f,                   -5,  1.0f,                   0.0f)]
        [InlineData( 1.0f,                   -4,  1.0f,                   0.0f)]
        [InlineData( 1.0f,                   -3,  1.0f,                   0.0f)]
        [InlineData( 1.0f,                   -2,  1.0f,                   0.0f)]
        [InlineData( 1.0f,                   -1,  1.0f,                   0.0f)]
        [InlineData( 1.0f,                    0,  float.NaN,              0.0f)]
        [InlineData( 1.0f,                    1,  1.0f,                   0.0f)]
        [InlineData( 1.0f,                    2,  1.0f,                   0.0f)]
        [InlineData( 1.0f,                    3,  1.0f,                   0.0f)]
        [InlineData( 1.0f,                    4,  1.0f,                   0.0f)]
        [InlineData( 1.0f,                    5,  1.0f,                   0.0f)]
        [InlineData( 2.71828183f,            -5,  0.81873075f,            CrossPlatformMachineEpsilon32 * 10)]
        [InlineData( 2.71828183f,            -4,  0.77880078f,            CrossPlatformMachineEpsilon32 * 10)]
        [InlineData( 2.71828183f,            -3,  0.71653131f,            CrossPlatformMachineEpsilon32 * 10)]
        [InlineData( 2.71828183f,            -2,  0.60653066f,            CrossPlatformMachineEpsilon32 * 10)]
        [InlineData( 2.71828183f,            -1,  0.36787944f,            CrossPlatformMachineEpsilon32 * 10)]
        [InlineData( 2.71828183f,             0,  float.NaN,              0.0f)]
        [InlineData( 2.71828183f,             1,  2.71828183f,            0.0f)]
        [InlineData( 2.71828183f,             2,  1.64872127f,            CrossPlatformMachineEpsilon32 * 10)]
        [InlineData( 2.71828183f,             3,  1.39561243f,            CrossPlatformMachineEpsilon32 * 10)]
        [InlineData( 2.71828183f,             4,  1.28402542f,            CrossPlatformMachineEpsilon32 * 10)]
        [InlineData( 2.71828183f,             5,  1.22140276f,            CrossPlatformMachineEpsilon32 * 10)]
        [InlineData( float.PositiveInfinity, -5,  0.0f,                   0.0f)]
        [InlineData( float.PositiveInfinity, -4,  0.0f,                   0.0f)]
        [InlineData( float.PositiveInfinity, -3,  0.0f,                   0.0f)]
        [InlineData( float.PositiveInfinity, -2,  0.0f,                   0.0f)]
        [InlineData( float.PositiveInfinity, -1,  0.0f,                   0.0f)]
        [InlineData( float.PositiveInfinity,  0,  float.NaN,              0.0f)]
        [InlineData( float.PositiveInfinity,  1,  float.PositiveInfinity, 0.0f)]
        [InlineData( float.PositiveInfinity,  2,  float.PositiveInfinity, 0.0f)]
        [InlineData( float.PositiveInfinity,  3,  float.PositiveInfinity, 0.0f)]
        [InlineData( float.PositiveInfinity,  4,  float.PositiveInfinity, 0.0f)]
        [InlineData( float.PositiveInfinity,  5,  float.PositiveInfinity, 0.0f)]
        public static void RootN32(float x, int n, float expectedResult, float allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, NFloat.RootN(x, n), allowedVariance);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is32BitProcess))]
        [InlineData( float.NaN,    float.NaN,    0.0f)]
        [InlineData( 1.0f,         0.0f,         0.0f)]
        [InlineData( 0.540302306f, 0.318309886f, CrossPlatformMachineEpsilon32)]
        [InlineData( 0.204957194f, 0.434294482f, CrossPlatformMachineEpsilon32)]
        [InlineData( 0.0f,         0.5f,         CrossPlatformMachineEpsilon32)] // This should be exact, but has an issue on WASM/Unix
        [InlineData(-0.416146837f, 0.636619772f, CrossPlatformMachineEpsilon32)]
        [InlineData(-0.570233249f, 0.693147181f, CrossPlatformMachineEpsilon32)]
        [InlineData(-0.605699867f, 0.707106781f, CrossPlatformMachineEpsilon32)]
        [InlineData(-0.781211892f, 0.785398163f, CrossPlatformMachineEpsilon32)]
        [InlineData(-1.0f,         1.0f,         CrossPlatformMachineEpsilon32)] // This should be exact, but has an issue on WASM/Unix
        [InlineData(-0.919764995f, 0.871620833f, CrossPlatformMachineEpsilon32)]
        [InlineData(-0.266255342f, 0.585786438f, CrossPlatformMachineEpsilon32)]
        [InlineData(-0.179057946f, 0.557304959f, CrossPlatformMachineEpsilon32)]
        [InlineData( 0.220584041f, 0.429203673f, CrossPlatformMachineEpsilon32)]
        [InlineData( 0.581195664f, 0.302585093f, CrossPlatformMachineEpsilon32)]
        [InlineData(-0.633255651f, 0.718281828f, CrossPlatformMachineEpsilon32)]
        [InlineData(-0.902685362f, 0.858407346f, CrossPlatformMachineEpsilon32)]
        public static void AcosPiTest32(float value, float expectedResult, float allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, (float)NFloat.AcosPi(value), allowedVariance);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is32BitProcess))]
        [InlineData( float.NaN,     float.NaN,    0.0f)]
        [InlineData( 0.0f,          0.0f,         0.0f)]
        [InlineData( 0.841470985f,  0.318309886f, CrossPlatformMachineEpsilon32)]
        [InlineData( 0.978770938f,  0.434294482f, CrossPlatformMachineEpsilon32)]
        [InlineData( 1.0f,          0.5f,         CrossPlatformMachineEpsilon32)] // This should be exact, but has an issue on WASM/Unix
        [InlineData( 0.909297427f,  0.363380228f, CrossPlatformMachineEpsilon32)]
        [InlineData( 0.821482831f,  0.306852819f, CrossPlatformMachineEpsilon32)]
        [InlineData( 0.795693202f,  0.292893219f, CrossPlatformMachineEpsilon32)]
        [InlineData( 0.624265953f,  0.214601837f, CrossPlatformMachineEpsilon32)]
        [InlineData(-0.392469559f, -0.128379167f, CrossPlatformMachineEpsilon32)]
        [InlineData(-0.963902533f, -0.414213562f, CrossPlatformMachineEpsilon32)]
        [InlineData(-0.983838529f, -0.442695041f, CrossPlatformMachineEpsilon32)]
        [InlineData(-0.975367972f, -0.429203673f, CrossPlatformMachineEpsilon32)]
        [InlineData( 0.813763848f,  0.302585093f, CrossPlatformMachineEpsilon32)]
        [InlineData( 0.773942685f,  0.281718172f, CrossPlatformMachineEpsilon32)]
        [InlineData(-0.430301217f, -0.141592654f, CrossPlatformMachineEpsilon32)]
        public static void AsinPiTest32(float value, float expectedResult, float allowedVariance)
        {
            AssertExtensions.Equal(-expectedResult, (float)NFloat.AsinPi(-value), allowedVariance);
            AssertExtensions.Equal(+expectedResult, (float)NFloat.AsinPi(+value), allowedVariance);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is32BitProcess))]
        [InlineData( float.NaN,               float.NaN,               float.NaN,    0.0f)]
        [InlineData( 0.0f,                   -1.0f,                    1.0f,         CrossPlatformMachineEpsilon32)]    // y: sinpi(0)              x:  cospi(1)            ; This should be exact, but has an issue on WASM/Unix
        [InlineData( 0.0f,                   -0.0f,                    1.0f,         CrossPlatformMachineEpsilon32)]    // y: sinpi(0)              x: -cospi(0.5)          ; This should be exact, but has an issue on WASM/Unix
        [InlineData( 0.0f,                    0.0f,                    0.0f,         0.0f)]                             // y: sinpi(0)              x:  cospi(0.5)
        [InlineData( 0.0f,                    1.0f,                    0.0f,         0.0f)]                             // y: sinpi(0)              x:  cospi(0)
        [InlineData( 0.841470985f,            0.540302306f,            0.318309886f, CrossPlatformMachineEpsilon32)]    // y: sinpi(1 / pi)         x:  cospi(1 / pi)
        [InlineData( 0.978770938f,            0.204957194f,            0.434294482f, CrossPlatformMachineEpsilon32)]    // y: sinpi(log10(e))       x:  cospi(log10(e))
        [InlineData( 1.0f,                   -0.0f,                    0.5f,         CrossPlatformMachineEpsilon32)]    // y: sinpi(0.5)            x: -cospi(0.5)          ; This should be exact, but has an issue on WASM/Unix
        [InlineData( 1.0f,                    0.0f,                    0.5f,         CrossPlatformMachineEpsilon32)]    // y: sinpi(0.5)            x:  cospi(0.5)          ; This should be exact, but has an issue on WASM/Unix
        [InlineData( 0.909297427f,           -0.416146837f,            0.636619772f, CrossPlatformMachineEpsilon32)]    // y: sinpi(2 / pi)         x:  cospi(2 / pi)
        [InlineData( 0.821482831f,           -0.570233249f,            0.693147181f, CrossPlatformMachineEpsilon32)]    // y: sinpi(ln(2))          x:  cospi(ln(2))
        [InlineData( 0.795693202f,           -0.605699867f,            0.707106781f, CrossPlatformMachineEpsilon32)]    // y: sinpi(1 / sqrt(2))    x:  cospi(1 / sqrt(2))
        [InlineData( 0.624265953f,           -0.781211892f,            0.785398163f, CrossPlatformMachineEpsilon32)]    // y: sinpi(pi / 4)         x:  cospi(pi / 4)
        [InlineData(-0.392469559f,           -0.919764995f,           -0.871620833f, CrossPlatformMachineEpsilon32)]    // y: sinpi(2 / sqrt(pi))   x:  cospi(2 / sqrt(pi))
        [InlineData(-0.963902533f,           -0.266255342f,           -0.585786438f, CrossPlatformMachineEpsilon32)]    // y: sinpi(sqrt(2))        x:  cospi(sqrt(2))
        [InlineData(-0.983838529f,           -0.179057946f,           -0.557304959f, CrossPlatformMachineEpsilon32)]    // y: sinpi(log2(e))        x:  cospi(log2(e))
        [InlineData(-0.975367972f,            0.220584041f,           -0.429203673f, CrossPlatformMachineEpsilon32)]    // y: sinpi(pi / 2)         x:  cospi(pi / 2)
        [InlineData( 0.813763848f,            0.581195664f,            0.302585093f, CrossPlatformMachineEpsilon32)]    // y: sinpi(ln(10))         x:  cospi(ln(10))
        [InlineData( 0.773942685f,           -0.633255651f,            0.718281828f, CrossPlatformMachineEpsilon32)]    // y: sinpi(e)              x:  cospi(e)
        [InlineData(-0.430301217f,           -0.902685362f,           -0.858407346f, CrossPlatformMachineEpsilon32)]    // y: sinpi(pi)             x:  cospi(pi)
        [InlineData( 1.0f,                    float.NegativeInfinity,  1.0f,         CrossPlatformMachineEpsilon32)]    // y: sinpi(0.5)                                    ; This should be exact, but has an issue on WASM/Unix
        [InlineData( 1.0f,                    float.PositiveInfinity,  0.0f,         0.0f)]                             // y: sinpi(0.5)
        [InlineData( float.PositiveInfinity, -1.0f,                    0.5f,         CrossPlatformMachineEpsilon32)]    //                          x:  cospi(1)            ; This should be exact, but has an issue on WASM/Unix
        [InlineData( float.PositiveInfinity,  1.0f,                    0.5f,         CrossPlatformMachineEpsilon32)]    //                          x:  cospi(0)            ; This should be exact, but has an issue on WASM/Unix
        [InlineData( float.PositiveInfinity,  float.NegativeInfinity,  0.75f,        CrossPlatformMachineEpsilon32)]    //                                                  ; This should be exact, but has an issue on WASM/Unix
        [InlineData( float.PositiveInfinity,  float.PositiveInfinity,  0.25f,        CrossPlatformMachineEpsilon32)]    //                                                  ; This should be exact, but has an issue on WASM/Unix
        public static void Atan2PiTest32(float y, float x, float expectedResult, float allowedVariance)
        {
            AssertExtensions.Equal(-expectedResult, (float)NFloat.Atan2Pi(-y, +x), allowedVariance);
            AssertExtensions.Equal(+expectedResult, (float)NFloat.Atan2Pi(+y, +x), allowedVariance);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is32BitProcess))]
        [InlineData( float.NaN,               float.NaN,    0.0f)]
        [InlineData( 0.0f,                    0.0f,         0.0f)]
        [InlineData( 1.55740773f,             0.318309886f, CrossPlatformMachineEpsilon32)]
        [InlineData( 4.77548954f,             0.434294482f, CrossPlatformMachineEpsilon32)]
        [InlineData( float.PositiveInfinity,  0.5f,         CrossPlatformMachineEpsilon32)] // This should be exact, but has an issue on WASM/Unix
        [InlineData(-2.18503986f,            -0.363380228f, CrossPlatformMachineEpsilon32)]
        [InlineData(-1.44060844f,            -0.306852819f, CrossPlatformMachineEpsilon32)]
        [InlineData(-1.31367571f,            -0.292893219f, CrossPlatformMachineEpsilon32)]
        [InlineData(-0.79909940f,            -0.214601837f, CrossPlatformMachineEpsilon32)]
        [InlineData( 0.42670634f,             0.128379167f, CrossPlatformMachineEpsilon32)]
        [InlineData( 3.62021857f,             0.414213562f, CrossPlatformMachineEpsilon32)]
        [InlineData( 5.49452594f,             0.442695041f, CrossPlatformMachineEpsilon32)]
        [InlineData(-4.42175222f,            -0.429203673f, CrossPlatformMachineEpsilon32)]
        [InlineData( 1.40015471f,             0.302585093f, CrossPlatformMachineEpsilon32)]
        [InlineData(-1.22216467f,            -0.281718172f, CrossPlatformMachineEpsilon32)]
        [InlineData( 0.476690146f,            0.141592654f, CrossPlatformMachineEpsilon32)]
        public static void AtanPiTest32(float value, float expectedResult, float allowedVariance)
        {
            AssertExtensions.Equal(-expectedResult, (float)NFloat.AtanPi(-value), allowedVariance);
            AssertExtensions.Equal(+expectedResult, (float)NFloat.AtanPi(+value), allowedVariance);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is32BitProcess))]
        [InlineData(float.NaN,               float.NaN,    0.0f)]
        [InlineData(0.0f,                    1.0f,         0.0f)]
        [InlineData(0.318309886f,            0.540302306f, CrossPlatformMachineEpsilon32)]       // value:  (1 / pi)
        [InlineData(0.434294482f,            0.204957194f, CrossPlatformMachineEpsilon32)]       // value:  (log10(e))
        [InlineData(0.5f,                    0.0f,         0.0f)]
        [InlineData(0.636619772f,           -0.416146837f, CrossPlatformMachineEpsilon32)]       // value:  (2 / pi)
        [InlineData(0.693147181f,           -0.570233249f, CrossPlatformMachineEpsilon32)]       // value:  (ln(2))
        [InlineData(0.707106781f,           -0.605699867f, CrossPlatformMachineEpsilon32)]       // value:  (1 / sqrt(2))
        [InlineData(0.785398163f,           -0.781211892f, CrossPlatformMachineEpsilon32)]       // value:  (pi / 4)
        [InlineData(1.0f,                   -1.0f,         0.0f)]
        [InlineData(1.12837917f,            -0.919764995f, CrossPlatformMachineEpsilon32)]       // value:  (2 / sqrt(pi))
        [InlineData(1.41421356f,            -0.266255342f, CrossPlatformMachineEpsilon32)]       // value:  (sqrt(2))
        [InlineData(1.44269504f,            -0.179057946f, CrossPlatformMachineEpsilon32)]       // value:  (log2(e))
        [InlineData(1.5f,                    0.0f,         0.0f)]
        [InlineData(1.57079633f,             0.220584041f, CrossPlatformMachineEpsilon32)]       // value:  (pi / 2)
        [InlineData(2.0f,                    1.0f,         0.0f)]
        [InlineData(2.30258509f,             0.581195664f, CrossPlatformMachineEpsilon32)]       // value:  (ln(10))
        [InlineData(2.5f,                    0.0f,         0.0f)]
        [InlineData(2.71828183f,            -0.633255651f, CrossPlatformMachineEpsilon32)]       // value:  (e)
        [InlineData(3.0f,                   -1.0f,         0.0f)]
        [InlineData(3.14159265f,            -0.902685362f, CrossPlatformMachineEpsilon32)]       // value:  (pi)
        [InlineData(3.5f,                    0.0f,         0.0f)]
        [InlineData(float.PositiveInfinity,  float.NaN,    0.0f)]
        public static void CosPiTest32(float value, float expectedResult, float allowedVariance)
        {
            AssertExtensions.Equal(+expectedResult, (float)NFloat.CosPi(-value), allowedVariance);
            AssertExtensions.Equal(+expectedResult, (float)NFloat.CosPi(+value), allowedVariance);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is32BitProcess))]
        [InlineData(float.NaN,               float.NaN,    0.0f)]
        [InlineData(0.0f,                    0.0f,         0.0f)]
        [InlineData(0.318309886f,            0.841470985f, CrossPlatformMachineEpsilon32)]       // value:  (1 / pi)
        [InlineData(0.434294482f,            0.978770938f, CrossPlatformMachineEpsilon32)]       // value:  (log10(e))
        [InlineData(0.5f,                    1.0f,         0.0f)]
        [InlineData(0.636619772f,            0.909297427f, CrossPlatformMachineEpsilon32)]       // value:  (2 / pi)
        [InlineData(0.693147181f,            0.821482831f, CrossPlatformMachineEpsilon32)]       // value:  (ln(2))
        [InlineData(0.707106781f,            0.795693202f, CrossPlatformMachineEpsilon32)]       // value:  (1 / sqrt(2))
        [InlineData(0.785398163f,            0.624265953f, CrossPlatformMachineEpsilon32)]       // value:  (pi / 4)
        [InlineData(1.0f,                    0.0f,         0.0f)]
        [InlineData(1.12837917f,            -0.392469559f, CrossPlatformMachineEpsilon32)]       // value:  (2 / sqrt(pi))
        [InlineData(1.41421356f,            -0.963902533f, CrossPlatformMachineEpsilon32)]       // value:  (sqrt(2))
        [InlineData(1.44269504f,            -0.983838529f, CrossPlatformMachineEpsilon32)]       // value:  (log2(e))
        [InlineData(1.5f,                   -1.0f,         0.0f)]
        [InlineData(1.57079633f,            -0.975367972f, CrossPlatformMachineEpsilon32)]       // value:  (pi / 2)
        [InlineData(2.0f,                    0.0f,         0.0f)]
        [InlineData(2.30258509f,             0.813763848f, CrossPlatformMachineEpsilon32)]       // value:  (ln(10))
        [InlineData(2.5f,                    1.0f,         0.0f)]
        [InlineData(2.71828183f,             0.773942685f, CrossPlatformMachineEpsilon32)]       // value:  (e)
        [InlineData(3.0f,                    0.0f,         0.0f)]
        [InlineData(3.14159265f,            -0.430301217f, CrossPlatformMachineEpsilon32)]       // value:  (pi)
        [InlineData(3.5f,                   -1.0f,         0.0f)]
        [InlineData(float.PositiveInfinity,  float.NaN,    0.0f)]
        public static void SinPiTest32(float value, float expectedResult, float allowedVariance)
        {
            AssertExtensions.Equal(-expectedResult, (float)NFloat.SinPi(-value), allowedVariance);
            AssertExtensions.Equal(+expectedResult, (float)NFloat.SinPi(+value), allowedVariance);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is32BitProcess))]
        [InlineData(float.NaN,               float.NaN,              0.0f)]
        [InlineData(0.0f,                    0.0f,                   0.0f)]
        [InlineData(0.318309886f,            1.55740772f,            CrossPlatformMachineEpsilon32 * 10)]  // value:  (1 / pi)
        [InlineData(0.434294482f,            4.77548954f,            CrossPlatformMachineEpsilon32 * 10)]  // value:  (log10(e))
        [InlineData(0.5f,                    float.PositiveInfinity, 0.0f)]
        [InlineData(0.636619772f,           -2.18503986f,            CrossPlatformMachineEpsilon32 * 10)]  // value:  (2 / pi)
        [InlineData(0.693147181f,           -1.44060844f,            CrossPlatformMachineEpsilon32 * 10)]  // value:  (ln(2))
        [InlineData(0.707106781f,           -1.31367571f,            CrossPlatformMachineEpsilon32 * 10)]  // value:  (1 / sqrt(2))
        [InlineData(0.785398163f,           -0.799099398f,           CrossPlatformMachineEpsilon32)]       // value:  (pi / 4)
        [InlineData(1.0f,                   -0.0f,                   0.0f)]
        [InlineData(1.12837917f,             0.426706344f,           CrossPlatformMachineEpsilon32)]       // value:  (2 / sqrt(pi))
        [InlineData(1.41421356f,             3.62021857f,            CrossPlatformMachineEpsilon32 * 10)]  // value:  (sqrt(2))
        [InlineData(1.44269504f,             5.49452594f,            CrossPlatformMachineEpsilon32 * 10)]  // value:  (log2(e))
        [InlineData(1.5f,                    float.NegativeInfinity, 0.0f)]
        [InlineData(1.57079633f,            -4.42175222f,            CrossPlatformMachineEpsilon32 * 10)]  // value:  (pi / 2)
        [InlineData(2.0f,                    0.0f,                   0.0f)]
        [InlineData(2.30258509f,             1.40015471f,            CrossPlatformMachineEpsilon32 * 10)]  // value:  (ln(10))
        [InlineData(2.5f,                    float.PositiveInfinity, 0.0f)]
        [InlineData(2.71828183f,            -1.22216467f,            CrossPlatformMachineEpsilon32 * 10)]  // value:  (e)
        [InlineData(3.0f,                   -0.0f,                   0.0f)]
        [InlineData(3.14159265f,             0.476690146f,           CrossPlatformMachineEpsilon32)]       // value:  (pi)
        [InlineData(3.5f,                    float.NegativeInfinity, 0.0f)]
        [InlineData(float.PositiveInfinity,  float.NaN,              0.0f)]
        public static void TanPiTest32(float value, float expectedResult, float allowedVariance)
        {
            AssertExtensions.Equal(-expectedResult, (float)NFloat.TanPi(-value), allowedVariance);
            AssertExtensions.Equal(+expectedResult, (float)NFloat.TanPi(+value), allowedVariance);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is64BitProcess))]
        [InlineData( double.NegativeInfinity, -1.0,                     0.0)]
        [InlineData(-3.1415926535897932,      -0.95678608173622775,     CrossPlatformMachineEpsilon64)]        // value: -(pi)
        [InlineData(-2.7182818284590452,      -0.93401196415468746,     CrossPlatformMachineEpsilon64)]        // value: -(e)
        [InlineData(-2.3025850929940457,      -0.9,                     CrossPlatformMachineEpsilon64)]        // value: -(ln(10))
        [InlineData(-1.5707963267948966,      -0.79212042364923809,     CrossPlatformMachineEpsilon64)]        // value: -(pi / 2)
        [InlineData(-1.4426950408889634,      -0.76370991165547730,     CrossPlatformMachineEpsilon64)]        // value: -(log2(e))
        [InlineData(-1.4142135623730950,      -0.75688326556578579,     CrossPlatformMachineEpsilon64)]        // value: -(sqrt(2))
        [InlineData(-1.1283791670955126,      -0.67644273609692890,     CrossPlatformMachineEpsilon64)]        // value: -(2 / sqrt(pi))
        [InlineData(-1.0,                     -0.63212055882855768,     CrossPlatformMachineEpsilon64)]
        [InlineData(-0.78539816339744831,     -0.54406187223400376,     CrossPlatformMachineEpsilon64)]        // value: -(pi / 4)
        [InlineData(-0.70710678118654752,     -0.50693130860476021,     CrossPlatformMachineEpsilon64)]        // value: -(1 / sqrt(2))
        [InlineData(-0.69314718055994531,     -0.5,                     CrossPlatformMachineEpsilon64)]        // value: -(ln(2))
        [InlineData(-0.63661977236758134,     -0.47092219173226465,     CrossPlatformMachineEpsilon64)]        // value: -(2 / pi)
        [InlineData(-0.43429448190325183,     -0.35227851485819935,     CrossPlatformMachineEpsilon64)]        // value: -(log10(e))
        [InlineData(-0.31830988618379067,     -0.27262265070478353,     CrossPlatformMachineEpsilon64)]        // value: -(1 / pi)
        [InlineData(-0.0,                      0.0,                     0.0)]
        [InlineData( double.NaN,               double.NaN,              0.0)]
        [InlineData( 0.0,                      0.0,                     0.0)]
        [InlineData( 0.31830988618379067,      0.37480222743935863,     CrossPlatformMachineEpsilon64)]        // value:  (1 / pi)
        [InlineData( 0.43429448190325183,      0.54387344397118114,     CrossPlatformMachineEpsilon64)]        // value:  (log10(e))
        [InlineData( 0.63661977236758134,      0.89008116457222198,     CrossPlatformMachineEpsilon64)]        // value:  (2 / pi)
        [InlineData( 0.69314718055994531,      1.0,                     CrossPlatformMachineEpsilon64 * 10)]   // value:  (ln(2))
        [InlineData( 0.70710678118654752,      1.0281149816474725,      CrossPlatformMachineEpsilon64 * 10)]   // value:  (1 / sqrt(2))
        [InlineData( 0.78539816339744831,      1.1932800507380155,      CrossPlatformMachineEpsilon64 * 10)]   // value:  (pi / 4)
        [InlineData( 1.0,                      1.7182818284590452,      CrossPlatformMachineEpsilon64 * 10)]
        [InlineData( 1.1283791670955126,       2.0906430223107976,      CrossPlatformMachineEpsilon64 * 10)]   // value:  (2 / sqrt(pi))
        [InlineData( 1.4142135623730950,       3.1132503787829275,      CrossPlatformMachineEpsilon64 * 10)]   // value:  (sqrt(2))
        [InlineData( 1.4426950408889634,       3.2320861065570819,      CrossPlatformMachineEpsilon64 * 10)]   // value:  (log2(e))
        [InlineData( 1.5707963267948966,       3.8104773809653517,      CrossPlatformMachineEpsilon64 * 10)]   // value:  (pi / 2)
        [InlineData( 2.3025850929940457,       9.0,                     CrossPlatformMachineEpsilon64 * 10)]   // value:  (ln(10))
        [InlineData( 2.7182818284590452,       14.154262241479264,      CrossPlatformMachineEpsilon64 * 100)]  // value:  (e)
        [InlineData( 3.1415926535897932,       22.140692632779269,      CrossPlatformMachineEpsilon64 * 100)]  // value:  (pi)
        [InlineData( double.PositiveInfinity,  double.PositiveInfinity, 0.0)]
        public static void ExpM1Test64(double value, double expectedResult, double allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, NFloat.ExpM1((NFloat)value), allowedVariance);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is64BitProcess))]
        [InlineData( double.NegativeInfinity, 0.0,                     0.0)]
        [InlineData(-3.1415926535897932,      0.11331473229676087,     CrossPlatformMachineEpsilon64)]        // value: -(pi)
        [InlineData(-2.7182818284590452,      0.15195522325791297,     CrossPlatformMachineEpsilon64)]        // value: -(e)
        [InlineData(-2.3025850929940457,      0.20269956628651730,     CrossPlatformMachineEpsilon64)]        // value: -(ln(10))
        [InlineData(-1.5707963267948966,      0.33662253682241906,     CrossPlatformMachineEpsilon64)]        // value: -(pi / 2)
        [InlineData(-1.4426950408889634,      0.36787944117144232,     CrossPlatformMachineEpsilon64)]        // value: -(log2(e))
        [InlineData(-1.4142135623730950,      0.37521422724648177,     CrossPlatformMachineEpsilon64)]        // value: -(sqrt(2))
        [InlineData(-1.1283791670955126,      0.45742934732229695,     CrossPlatformMachineEpsilon64)]        // value: -(2 / sqrt(pi))
        [InlineData(-1.0,                     0.5,                     CrossPlatformMachineEpsilon64)]
        [InlineData(-0.78539816339744831,     0.58019181037172444,     CrossPlatformMachineEpsilon64)]        // value: -(pi / 4)
        [InlineData(-0.70710678118654752,     0.61254732653606592,     CrossPlatformMachineEpsilon64)]        // value: -(1 / sqrt(2))
        [InlineData(-0.69314718055994531,     0.61850313780157598,     CrossPlatformMachineEpsilon64)]        // value: -(ln(2))
        [InlineData(-0.63661977236758134,     0.64321824193300488,     CrossPlatformMachineEpsilon64)]        // value: -(2 / pi)
        [InlineData(-0.43429448190325183,     0.74005557395545179,     CrossPlatformMachineEpsilon64)]        // value: -(log10(e))
        [InlineData(-0.31830988618379067,     0.80200887896145195,     CrossPlatformMachineEpsilon64)]        // value: -(1 / pi)
        [InlineData(-0.0,                     1.0,                     0.0)]
        [InlineData( double.NaN,              double.NaN,              0.0)]
        [InlineData( 0.0,                     1.0,                     0.0)]
        [InlineData( 0.31830988618379067,     1.2468689889006383,      CrossPlatformMachineEpsilon64 * 10)]   // value:  (1 / pi)
        [InlineData( 0.43429448190325183,     1.3512498725672672,      CrossPlatformMachineEpsilon64 * 10)]   // value:  (log10(e))
        [InlineData( 0.63661977236758134,     1.5546822754821001,      CrossPlatformMachineEpsilon64 * 10)]   // value:  (2 / pi)
        [InlineData( 0.69314718055994531,     1.6168066722416747,      CrossPlatformMachineEpsilon64 * 10)]   // value:  (ln(2))
        [InlineData( 0.70710678118654752,     1.6325269194381528,      CrossPlatformMachineEpsilon64 * 10)]   // value:  (1 / sqrt(2))
        [InlineData( 0.78539816339744831,     1.7235679341273495,      CrossPlatformMachineEpsilon64 * 10)]   // value:  (pi / 4)
        [InlineData( 1.0,                     2.0,                     CrossPlatformMachineEpsilon64 * 10)]
        [InlineData( 1.1283791670955126,      2.1861299583286618,      CrossPlatformMachineEpsilon64 * 10)]   // value:  (2 / sqrt(pi))
        [InlineData( 1.4142135623730950,      2.6651441426902252,      CrossPlatformMachineEpsilon64 * 10)]   // value:  (sqrt(2))
        [InlineData( 1.4426950408889634,      2.7182818284590452,      CrossPlatformMachineEpsilon64 * 10)]   // value:  (log2(e))
        [InlineData( 1.5707963267948966,      2.9706864235520193,      CrossPlatformMachineEpsilon64 * 10)]   // value:  (pi / 2)
        [InlineData( 2.3025850929940457,      4.9334096679145963,      CrossPlatformMachineEpsilon64 * 10)]   // value:  (ln(10))
        [InlineData( 2.7182818284590452,      6.5808859910179210,      CrossPlatformMachineEpsilon64 * 10)]   // value:  (e)
        [InlineData( 3.1415926535897932,      8.8249778270762876,      CrossPlatformMachineEpsilon64 * 10)]   // value:  (pi)
        [InlineData( double.PositiveInfinity, double.PositiveInfinity, 0.0)]
        public static void Exp2Test64(double value, double expectedResult, double allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, NFloat.Exp2((NFloat)value), allowedVariance);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is64BitProcess))]
        [InlineData( double.NegativeInfinity, -1.0,                     0.0)]
        [InlineData(-3.1415926535897932,      -0.88668526770323913,     CrossPlatformMachineEpsilon64)]        // value: -(pi)
        [InlineData(-2.7182818284590452,      -0.84804477674208703,     CrossPlatformMachineEpsilon64)]        // value: -(e)
        [InlineData(-2.3025850929940457,      -0.79730043371348270,     CrossPlatformMachineEpsilon64)]        // value: -(ln(10))
        [InlineData(-1.5707963267948966,      -0.66337746317758094,     CrossPlatformMachineEpsilon64)]        // value: -(pi / 2)
        [InlineData(-1.4426950408889634,      -0.63212055882855768,     CrossPlatformMachineEpsilon64)]        // value: -(log2(e))
        [InlineData(-1.4142135623730950,      -0.62478577275351823,     CrossPlatformMachineEpsilon64)]        // value: -(sqrt(2))
        [InlineData(-1.1283791670955126,      -0.54257065267770305,     CrossPlatformMachineEpsilon64)]        // value: -(2 / sqrt(pi))
        [InlineData(-1.0,                     -0.5,                     CrossPlatformMachineEpsilon64)]
        [InlineData(-0.78539816339744831,     -0.41980818962827556,     CrossPlatformMachineEpsilon64)]        // value: -(pi / 4)
        [InlineData(-0.70710678118654752,     -0.38745267346393408,     CrossPlatformMachineEpsilon64)]        // value: -(1 / sqrt(2))
        [InlineData(-0.69314718055994531,     -0.38149686219842402,     CrossPlatformMachineEpsilon64)]        // value: -(ln(2))
        [InlineData(-0.63661977236758134,     -0.35678175806699512,     CrossPlatformMachineEpsilon64)]        // value: -(2 / pi)
        [InlineData(-0.43429448190325183,     -0.25994442604454821,     CrossPlatformMachineEpsilon64)]        // value: -(log10(e))
        [InlineData(-0.31830988618379067,     -0.19799112103854805,     CrossPlatformMachineEpsilon64)]        // value: -(1 / pi)
        [InlineData(-0.0,                      0.0,                     0.0)]
        [InlineData( double.NaN,               double.NaN,              0.0)]
        [InlineData( 0.0,                      0.0,                     0.0)]
        [InlineData( 0.31830988618379067,      0.24686898890063831,     CrossPlatformMachineEpsilon64)]        // value:  (1 / pi)
        [InlineData( 0.43429448190325183,      0.35124987256726717,     CrossPlatformMachineEpsilon64)]        // value:  (log10(e))
        [InlineData( 0.63661977236758134,      0.55468227548210009,     CrossPlatformMachineEpsilon64)]        // value:  (2 / pi)
        [InlineData( 0.69314718055994531,      0.61680667224167466,     CrossPlatformMachineEpsilon64)]        // value:  (ln(2))
        [InlineData( 0.70710678118654752,      0.63252691943815284,     CrossPlatformMachineEpsilon64)]        // value:  (1 / sqrt(2))
        [InlineData( 0.78539816339744831,      0.72356793412734949,     CrossPlatformMachineEpsilon64)]        // value:  (pi / 4)
        [InlineData( 1.0,                      1.0,                     CrossPlatformMachineEpsilon64 * 10)]
        [InlineData( 1.1283791670955126,       1.1861299583286618,      CrossPlatformMachineEpsilon64 * 10)]   // value:  (2 / sqrt(pi))
        [InlineData( 1.4142135623730950,       1.6651441426902252,      CrossPlatformMachineEpsilon64 * 10)]   // value:  (sqrt(2))
        [InlineData( 1.4426950408889634,       1.7182818284590452,      CrossPlatformMachineEpsilon64 * 10)]   // value:  (log2(e))
        [InlineData( 1.5707963267948966,       1.9706864235520193,      CrossPlatformMachineEpsilon64 * 10)]   // value:  (pi / 2)
        [InlineData( 2.3025850929940457,       3.9334096679145963,      CrossPlatformMachineEpsilon64 * 10)]   // value:  (ln(10))
        [InlineData( 2.7182818284590452,       5.5808859910179210,      CrossPlatformMachineEpsilon64 * 10)]   // value:  (e)
        [InlineData( 3.1415926535897932,       7.8249778270762876,      CrossPlatformMachineEpsilon64 * 10)]   // value:  (pi)
        [InlineData( double.PositiveInfinity,  double.PositiveInfinity, 0.0)]
        public static void Exp2M1Test64(double value, double expectedResult, double allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, NFloat.Exp2M1((NFloat)value), allowedVariance);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is64BitProcess))]
        [InlineData( double.NegativeInfinity,  0.0,                     0.0)]
        [InlineData(-3.1415926535897932,       0.00072178415907472774,  CrossPlatformMachineEpsilon64 / 1000)]  // value: -(pi)
        [InlineData(-2.7182818284590452,       0.0019130141022243176,   CrossPlatformMachineEpsilon64 / 100)]   // value: -(e)
        [InlineData(-2.3025850929940457,       0.0049821282964407206,   CrossPlatformMachineEpsilon64 / 100)]   // value: -(ln(10))
        [InlineData(-1.5707963267948966,       0.026866041001136132,    CrossPlatformMachineEpsilon64 / 10)]    // value: -(pi / 2)
        [InlineData(-1.4426950408889634,       0.036083192820787210,    CrossPlatformMachineEpsilon64 / 10)]    // value: -(log2(e))
        [InlineData(-1.4142135623730950,       0.038528884700322026,    CrossPlatformMachineEpsilon64 / 10)]    // value: -(sqrt(2))
        [InlineData(-1.1283791670955126,       0.074408205860642723,    CrossPlatformMachineEpsilon64 / 10)]    // value: -(2 / sqrt(pi))
        [InlineData(-1.0,                      0.1,                     CrossPlatformMachineEpsilon64)]
        [InlineData(-0.78539816339744831,      0.16390863613957665,     CrossPlatformMachineEpsilon64)]         // value: -(pi / 4)
        [InlineData(-0.70710678118654752,      0.19628775993505562,     CrossPlatformMachineEpsilon64)]         // value: -(1 / sqrt(2))
        [InlineData(-0.69314718055994531,      0.20269956628651730,     CrossPlatformMachineEpsilon64)]         // value: -(ln(2))
        [InlineData(-0.63661977236758134,      0.23087676451600055,     CrossPlatformMachineEpsilon64)]         // value: -(2 / pi)
        [InlineData(-0.43429448190325183,      0.36787944117144232,     CrossPlatformMachineEpsilon64)]         // value: -(log10(e))
        [InlineData(-0.31830988618379067,      0.48049637305186868,     CrossPlatformMachineEpsilon64)]         // value: -(1 / pi)
        [InlineData(-0.0,                      1.0,                     0.0)]
        [InlineData( double.NaN,               double.NaN,              0.0)]
        [InlineData( 0.0,                      1.0,                     0.0)]
        [InlineData( 0.31830988618379067,      2.0811811619898573,      CrossPlatformMachineEpsilon64 * 10)]    // value:  (1 / pi)
        [InlineData( 0.43429448190325183,      2.7182818284590452,      CrossPlatformMachineEpsilon64 * 10)]    // value:  (log10(e))
        [InlineData( 0.63661977236758134,      4.3313150290214525,      CrossPlatformMachineEpsilon64 * 10)]    // value:  (2 / pi)
        [InlineData( 0.69314718055994531,      4.9334096679145963,      CrossPlatformMachineEpsilon64 * 10)]    // value:  (ln(2))
        [InlineData( 0.70710678118654752,      5.0945611704512962,      CrossPlatformMachineEpsilon64 * 10)]    // value:  (1 / sqrt(2))
        [InlineData( 0.78539816339744831,      6.1009598002416937,      CrossPlatformMachineEpsilon64 * 10)]    // value:  (pi / 4)
        [InlineData( 1.0,                      10.0,                    CrossPlatformMachineEpsilon64 * 100)]
        [InlineData( 1.1283791670955126,       13.439377934644401,      CrossPlatformMachineEpsilon64 * 100)]   // value:  (2 / sqrt(pi))
        [InlineData( 1.4142135623730950,       25.954553519470081,      CrossPlatformMachineEpsilon64 * 100)]   // value:  (sqrt(2))
        [InlineData( 1.4426950408889634,       27.713733786437790,      CrossPlatformMachineEpsilon64 * 100)]   // value:  (log2(e))
        [InlineData( 1.5707963267948966,       37.221710484165167,      CrossPlatformMachineEpsilon64 * 100)]   // value:  (pi / 2)
        [InlineData( 2.3025850929940457,       200.71743249053009,      CrossPlatformMachineEpsilon64 * 1000)]  // value:  (ln(10))
        [InlineData( 2.7182818284590452,       522.73529967043665,      CrossPlatformMachineEpsilon64 * 1000)]  // value:  (e)
        [InlineData( 3.1415926535897932,       1385.4557313670111,      CrossPlatformMachineEpsilon64 * 10000)] // value:  (pi)
        [InlineData( double.PositiveInfinity,  double.PositiveInfinity, 0.0)]
        public static void Exp10Test64(double value, double expectedResult, double allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, NFloat.Exp10((NFloat)value), allowedVariance);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is64BitProcess))]
        [InlineData( double.NegativeInfinity, -1.0,                     0.0)]
        [InlineData(-3.1415926535897932,      -0.99927821584092527,     CrossPlatformMachineEpsilon64)]         // value: -(pi)
        [InlineData(-2.7182818284590452,      -0.99808698589777568,     CrossPlatformMachineEpsilon64)]         // value: -(e)
        [InlineData(-2.3025850929940457,      -0.99501787170355928,     CrossPlatformMachineEpsilon64)]         // value: -(ln(10))
        [InlineData(-1.5707963267948966,      -0.97313395899886387,     CrossPlatformMachineEpsilon64)]         // value: -(pi / 2)
        [InlineData(-1.4426950408889634,      -0.96391680717921279,     CrossPlatformMachineEpsilon64)]         // value: -(log2(e))
        [InlineData(-1.4142135623730950,      -0.96147111529967797,     CrossPlatformMachineEpsilon64)]         // value: -(sqrt(2))
        [InlineData(-1.1283791670955126,      -0.92559179413935728,     CrossPlatformMachineEpsilon64)]         // value: -(2 / sqrt(pi))
        [InlineData(-1.0,                     -0.9,                     CrossPlatformMachineEpsilon64)]
        [InlineData(-0.78539816339744831,     -0.83609136386042335,     CrossPlatformMachineEpsilon64)]         // value: -(pi / 4)
        [InlineData(-0.70710678118654752,     -0.80371224006494438,     CrossPlatformMachineEpsilon64)]         // value: -(1 / sqrt(2))
        [InlineData(-0.69314718055994531,     -0.79730043371348270,     CrossPlatformMachineEpsilon64)]         // value: -(ln(2))
        [InlineData(-0.63661977236758134,     -0.76912323548399945,     CrossPlatformMachineEpsilon64)]         // value: -(2 / pi)
        [InlineData(-0.43429448190325183,     -0.63212055882855768,     CrossPlatformMachineEpsilon64)]         // value: -(log10(e))
        [InlineData(-0.31830988618379067,     -0.51950362694813132,     CrossPlatformMachineEpsilon64)]         // value: -(1 / pi)
        [InlineData(-0.0,                      0.0,                     0.0)]
        [InlineData( double.NaN,               double.NaN,              0.0)]
        [InlineData( 0.0,                      0.0,                     0.0)]
        [InlineData( 0.31830988618379067,      1.0811811619898573,      CrossPlatformMachineEpsilon64 * 10)]    // value:  (1 / pi)
        [InlineData( 0.43429448190325183,      1.7182818284590452,      CrossPlatformMachineEpsilon64 * 10)]    // value:  (log10(e))
        [InlineData( 0.63661977236758134,      3.3313150290214525,      CrossPlatformMachineEpsilon64 * 10)]    // value:  (2 / pi)
        [InlineData( 0.69314718055994531,      3.9334096679145963,      CrossPlatformMachineEpsilon64 * 10)]    // value:  (ln(2))
        [InlineData( 0.70710678118654752,      4.0945611704512962,      CrossPlatformMachineEpsilon64 * 10)]    // value:  (1 / sqrt(2))
        [InlineData( 0.78539816339744831,      5.1009598002416937,      CrossPlatformMachineEpsilon64 * 10)]    // value:  (pi / 4)
        [InlineData( 1.0,                      9.0,                     CrossPlatformMachineEpsilon64 * 10)]
        [InlineData( 1.1283791670955126,       12.439377934644401,      CrossPlatformMachineEpsilon64 * 100)]   // value:  (2 / sqrt(pi))
        [InlineData( 1.4142135623730950,       24.954553519470081,      CrossPlatformMachineEpsilon64 * 100)]   // value:  (sqrt(2))
        [InlineData( 1.4426950408889634,       26.713733786437790,      CrossPlatformMachineEpsilon64 * 100)]   // value:  (log2(e))
        [InlineData( 1.5707963267948966,       36.221710484165167,      CrossPlatformMachineEpsilon64 * 100)]   // value:  (pi / 2)
        [InlineData( 2.3025850929940457,       199.71743249053009,      CrossPlatformMachineEpsilon64 * 1000)]  // value:  (ln(10))
        [InlineData( 2.7182818284590452,       521.73529967043665,      CrossPlatformMachineEpsilon64 * 1000)]  // value:  (e)
        [InlineData( 3.1415926535897932,       1384.4557313670111,      CrossPlatformMachineEpsilon64 * 10000)] // value:  (pi)
        [InlineData( double.PositiveInfinity,  double.PositiveInfinity, 0.0)]
        public static void Exp10M1Test64(double value, double expectedResult, double allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, NFloat.Exp10M1((NFloat)value), allowedVariance);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is64BitProcess))]
        [InlineData( double.NegativeInfinity, double.NaN,               0.0)]
        [InlineData(-3.1415926535897932,      double.NaN,               0.0)]                               //                              value: -(pi)
        [InlineData(-2.7182818284590452,      double.NaN,               0.0)]                               //                              value: -(e)
        [InlineData(-1.4142135623730950,      double.NaN,               0.0)]                               //                              value: -(sqrt(2))
        [InlineData( double.NaN,              double.NaN,               0.0)]
        [InlineData(-1.0,                     double.NegativeInfinity,  0.0)]
        [InlineData(-0.95678608173622775,    -3.1415926535897932,       CrossPlatformMachineEpsilon64 * 10)]  // expected: -(pi)
        [InlineData(-0.93401196415468746,    -2.7182818284590452,       CrossPlatformMachineEpsilon64 * 10)]  // expected: -(e)
        [InlineData(-0.9,                    -2.3025850929940457,       CrossPlatformMachineEpsilon64 * 10)]  // expected: -(ln(10))
        [InlineData(-0.79212042364923809,    -1.5707963267948966,       CrossPlatformMachineEpsilon64 * 10)]  // expected: -(pi / 2)
        [InlineData(-0.76370991165547730,    -1.4426950408889634,       CrossPlatformMachineEpsilon64 * 10)]  // expected: -(log2(e))
        [InlineData(-0.75688326556578579,    -1.4142135623730950,       CrossPlatformMachineEpsilon64 * 10)]  // expected: -(sqrt(2))
        [InlineData(-0.67644273609692890,    -1.1283791670955126,       CrossPlatformMachineEpsilon64 * 10)]  // expected: -(2 / sqrt(pi))
        [InlineData(-0.63212055882855768,    -1.0,                      CrossPlatformMachineEpsilon64 * 10)]
        [InlineData(-0.54406187223400376,    -0.78539816339744831,      CrossPlatformMachineEpsilon64)]       // expected: -(pi / 4)
        [InlineData(-0.50693130860476021,    -0.70710678118654752,      CrossPlatformMachineEpsilon64)]       // expected: -(1 / sqrt(2))
        [InlineData(-0.5,                    -0.69314718055994531,      CrossPlatformMachineEpsilon64)]       // expected: -(ln(2))
        [InlineData(-0.47092219173226465,    -0.63661977236758134,      CrossPlatformMachineEpsilon64)]       // expected: -(2 / pi)
        [InlineData(-0.0,                     0.0,                      0.0)]
        [InlineData( 0.0,                     0.0,                      0.0)]
        [InlineData( 0.37480222743935863,     0.31830988618379067,      CrossPlatformMachineEpsilon64)]       // expected:  (1 / pi)
        [InlineData( 0.54387344397118114,     0.43429448190325183,      CrossPlatformMachineEpsilon64)]       // expected:  (log10(e))
        [InlineData( 0.89008116457222198,     0.63661977236758134,      CrossPlatformMachineEpsilon64)]       // expected:  (2 / pi)
        [InlineData( 1.0,                     0.69314718055994531,      CrossPlatformMachineEpsilon64)]       // expected:  (ln(2))
        [InlineData( 1.0281149816474725,      0.70710678118654752,      CrossPlatformMachineEpsilon64)]       // expected:  (1 / sqrt(2))
        [InlineData( 1.1932800507380155,      0.78539816339744831,      CrossPlatformMachineEpsilon64)]       // expected:  (pi / 4)
        [InlineData( 1.7182818284590452,      1.0,                      CrossPlatformMachineEpsilon64 * 10)]  //                              value: (e)
        [InlineData( 2.0906430223107976,      1.1283791670955126,       CrossPlatformMachineEpsilon64 * 10)]  // expected:  (2 / sqrt(pi))
        [InlineData( 3.1132503787829275,      1.4142135623730950,       CrossPlatformMachineEpsilon64 * 10)]  // expected:  (sqrt(2))
        [InlineData( 3.2320861065570819,      1.4426950408889634,       CrossPlatformMachineEpsilon64 * 10)]  // expected:  (log2(e))
        [InlineData( 3.8104773809653517,      1.5707963267948966,       CrossPlatformMachineEpsilon64 * 10)]  // expected:  (pi / 2)
        [InlineData( 9.0,                     2.3025850929940457,       CrossPlatformMachineEpsilon64 * 10)]  // expected:  (ln(10))
        [InlineData( 14.154262241479264,      2.7182818284590452,       CrossPlatformMachineEpsilon64 * 10)]  // expected:  (e)
        [InlineData( 22.140692632779269,      3.1415926535897932,       CrossPlatformMachineEpsilon64 * 10)]  // expected:  (pi)
        [InlineData( double.PositiveInfinity,  double.PositiveInfinity, 0.0)]
        public static void LogP1Test64(double value, double expectedResult, double allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, NFloat.LogP1((NFloat)value), allowedVariance);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is64BitProcess))]
        [InlineData( double.NegativeInfinity,  double.NaN,              0.0)]
        [InlineData( double.NaN,               double.NaN,              0.0)]
        [InlineData(-1.0,                      double.NegativeInfinity, 0.0)]
        [InlineData(-0.88668526770323913,     -3.1415926535897932,      CrossPlatformMachineEpsilon64 * 10)]  // expected: -(pi)
        [InlineData(-0.84804477674208703,     -2.7182818284590452,      CrossPlatformMachineEpsilon64 * 10)]  // expected: -(e)
        [InlineData(-0.79730043371348270,     -2.3025850929940457,      CrossPlatformMachineEpsilon64 * 10)]  // expected: -(ln(10))
        [InlineData(-0.66337746317758094,     -1.5707963267948966,      CrossPlatformMachineEpsilon64 * 10)]  // expected: -(pi / 2)
        [InlineData(-0.63212055882855768,     -1.4426950408889634,      CrossPlatformMachineEpsilon64 * 10)]  // expected: -(log2(e))
        [InlineData(-0.62478577275351823,     -1.4142135623730950,      CrossPlatformMachineEpsilon64 * 10)]  // expected: -(sqrt(2))
        [InlineData(-0.54257065267770305,     -1.1283791670955126,      CrossPlatformMachineEpsilon64 * 10)]  // expected: -(2 / sqrt(pi))
        [InlineData(-0.5,                     -1.0,                     CrossPlatformMachineEpsilon64 * 10)]
        [InlineData(-0.41980818962827556,     -0.78539816339744831,     CrossPlatformMachineEpsilon64)]       // expected: -(pi / 4)
        [InlineData(-0.38745267346393408,     -0.70710678118654752,     CrossPlatformMachineEpsilon64)]       // expected: -(1 / sqrt(2))
        [InlineData(-0.38149686219842402,     -0.69314718055994531,     CrossPlatformMachineEpsilon64)]       // expected: -(ln(2))
        [InlineData(-0.35678175806699512,     -0.63661977236758134,     CrossPlatformMachineEpsilon64)]       // expected: -(2 / pi)
        [InlineData(-0.25994442604454821,     -0.43429448190325183,     CrossPlatformMachineEpsilon64)]       // expected: -(log10(e))
        [InlineData(-0.19799112103854805,     -0.31830988618379067,     CrossPlatformMachineEpsilon64)]       // expected: -(1 / pi)
        [InlineData(-0.0,                      0.0,                     0.0)]
        [InlineData( 0.0,                      0.0,                     0.0)]
        [InlineData( 0.24686898890063831,      0.31830988618379067,     CrossPlatformMachineEpsilon64)]       // expected:  (1 / pi)
        [InlineData( 0.35124987256726717,      0.43429448190325183,     CrossPlatformMachineEpsilon64)]       // expected:  (log10(e))
        [InlineData( 0.55468227548210009,      0.63661977236758134,     CrossPlatformMachineEpsilon64)]       // expected:  (2 / pi)
        [InlineData( 0.61680667224167466,      0.69314718055994531,     CrossPlatformMachineEpsilon64)]       // expected:  (ln(2))
        [InlineData( 0.63252691943815284,      0.70710678118654752,     CrossPlatformMachineEpsilon64)]       // expected:  (1 / sqrt(2))
        [InlineData( 0.72356793412734949,      0.78539816339744831,     CrossPlatformMachineEpsilon64)]       // expected:  (pi / 4)
        [InlineData( 1.0,                      1.0,                     CrossPlatformMachineEpsilon64 * 10)]  //                              value: (e)
        [InlineData( 1.1861299583286618,       1.1283791670955126,      CrossPlatformMachineEpsilon64 * 10)]  // expected:  (2 / sqrt(pi))
        [InlineData( 1.6651441426902252,       1.4142135623730950,      CrossPlatformMachineEpsilon64 * 10)]  // expected:  (sqrt(2))
        [InlineData( 1.7182818284590452,       1.4426950408889634,      CrossPlatformMachineEpsilon64 * 10)]  // expected:  (log2(e))
        [InlineData( 1.9706864235520193,       1.5707963267948966,      CrossPlatformMachineEpsilon64 * 10)]  // expected:  (pi / 2)
        [InlineData( 3.9334096679145963,       2.3025850929940457,      CrossPlatformMachineEpsilon64 * 10)]  // expected:  (ln(10))
        [InlineData( 5.5808859910179210,       2.7182818284590452,      CrossPlatformMachineEpsilon64 * 10)]  // expected:  (e)
        [InlineData( 7.8249778270762876,       3.1415926535897932,      CrossPlatformMachineEpsilon64 * 10)]  // expected:  (pi)
        [InlineData( double.PositiveInfinity,  double.PositiveInfinity, 0.0)]
        public static void Log2P1Test64(double value, double expectedResult, double allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, NFloat.Log2P1((NFloat)value), allowedVariance);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is64BitProcess))]
        [InlineData( double.NegativeInfinity,  double.NaN,              0.0)]
        [InlineData(-3.1415926535897932,       double.NaN,              0.0)]                               //                              value: -(pi)
        [InlineData(-2.7182818284590452,       double.NaN,              0.0)]                               //                              value: -(e)
        [InlineData(-1.4142135623730950,       double.NaN,              0.0)]                               //                              value: -(sqrt(2))
        [InlineData( double.NaN,               double.NaN,              0.0)]
        [InlineData(-1.0,                      double.NegativeInfinity, 0.0)]
        [InlineData(-0.99808698589777568,     -2.7182818284590452,      CrossPlatformMachineEpsilon64 * 10)]  // expected: -(e)
        [InlineData(-0.99501787170355928,     -2.3025850929940457,      CrossPlatformMachineEpsilon64 * 10)]  // expected: -(ln(10))
        [InlineData(-0.97313395899886387,     -1.5707963267948966,      CrossPlatformMachineEpsilon64 * 10)]  // expected: -(pi / 2)
        [InlineData(-0.96391680717921279,     -1.4426950408889634,      CrossPlatformMachineEpsilon64 * 10)]  // expected: -(log2(e))
        [InlineData(-0.96147111529967797,     -1.4142135623730950,      CrossPlatformMachineEpsilon64 * 10)]  // expected: -(sqrt(2))
        [InlineData(-0.92559179413935728,     -1.1283791670955126,      CrossPlatformMachineEpsilon64 * 10)]  // expected: -(2 / sqrt(pi))
        [InlineData(-0.9,                     -1.0,                     CrossPlatformMachineEpsilon64 * 10)]
        [InlineData(-0.83609136386042335,     -0.78539816339744831,     CrossPlatformMachineEpsilon64)]       // expected: -(pi / 4)
        [InlineData(-0.80371224006494438,     -0.70710678118654752,     CrossPlatformMachineEpsilon64)]       // expected: -(1 / sqrt(2))
        [InlineData(-0.79730043371348270,     -0.69314718055994531,     CrossPlatformMachineEpsilon64)]       // expected: -(ln(2))
        [InlineData(-0.76912323548399945,     -0.63661977236758134,     CrossPlatformMachineEpsilon64)]       // expected: -(2 / pi)
        [InlineData(-0.63212055882855768,     -0.43429448190325183,     CrossPlatformMachineEpsilon64)]       // expected: -(log10(e))
        [InlineData(-0.51950362694813132,     -0.31830988618379067,     CrossPlatformMachineEpsilon64)]       // expected: -(1 / pi)
        [InlineData(-0.0,                      0.0,                     0.0)]
        [InlineData( 0.0,                      0.0,                     0.0)]
        [InlineData( 1.0811811619898573,       0.31830988618379067,     CrossPlatformMachineEpsilon64)]       // expected:  (1 / pi)
        [InlineData( 1.7182818284590452,       0.43429448190325183,     CrossPlatformMachineEpsilon64)]       // expected:  (log10(e))        value: (e)
        [InlineData( 3.3313150290214525,       0.63661977236758134,     CrossPlatformMachineEpsilon64)]       // expected:  (2 / pi)
        [InlineData( 3.9334096679145963,       0.69314718055994531,     CrossPlatformMachineEpsilon64)]       // expected:  (ln(2))
        [InlineData( 4.0945611704512962,       0.70710678118654752,     CrossPlatformMachineEpsilon64)]       // expected:  (1 / sqrt(2))
        [InlineData( 5.1009598002416937,       0.78539816339744831,     CrossPlatformMachineEpsilon64)]       // expected:  (pi / 4)
        [InlineData( 9.0,                      1.0,                     CrossPlatformMachineEpsilon64 * 10)]
        [InlineData( 12.439377934644401,       1.1283791670955126,      CrossPlatformMachineEpsilon64 * 10)]  // expected:  (2 / sqrt(pi))
        [InlineData( 24.954553519470081,       1.4142135623730950,      CrossPlatformMachineEpsilon64 * 10)]  // expected:  (sqrt(2))
        [InlineData( 26.713733786437790,       1.4426950408889634,      CrossPlatformMachineEpsilon64 * 10)]  // expected:  (log2(e))
        [InlineData( 36.221710484165167,       1.5707963267948966,      CrossPlatformMachineEpsilon64 * 10)]  // expected:  (pi / 2)
        [InlineData( 199.71743249053009,       2.3025850929940457,      CrossPlatformMachineEpsilon64 * 10)]  // expected:  (ln(10))
        [InlineData( 521.73529967043665,       2.7182818284590452,      CrossPlatformMachineEpsilon64 * 10)]  // expected:  (e)
        [InlineData( 1384.4557313670111,       3.1415926535897932,      CrossPlatformMachineEpsilon64 * 10)]  // expected:  (pi)
        [InlineData( double.PositiveInfinity,  double.PositiveInfinity, 0.0)]
        public static void Log10P1Test64(double value, double expectedResult, double allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, NFloat.Log10P1((NFloat)value), allowedVariance);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is64BitProcess))]
        [InlineData(double.NaN,              double.NaN,              double.NaN,              0.0)]
        [InlineData(double.NaN,              0.0f,                    double.NaN,              0.0)]
        [InlineData(double.NaN,              1.0f,                    double.NaN,              0.0)]
        [InlineData(double.NaN,              2.7182818284590452,      double.NaN,              0.0)]
        [InlineData(double.NaN,              10.0,                    double.NaN,              0.0)]
        [InlineData(0.0,                     0.0,                     0.0,                     0.0)]
        [InlineData(0.0,                     1.0,                     1.0,                     0.0)]
        [InlineData(0.0,                     1.5707963267948966,      1.5707963267948966,      0.0)]
        [InlineData(0.0,                     2.0,                     2.0,                     0.0)]
        [InlineData(0.0,                     2.7182818284590452,      2.7182818284590452,      0.0)]
        [InlineData(0.0,                     3.0,                     3.0,                     0.0)]
        [InlineData(0.0,                     10.0,                    10.0,                    0.0)]
        [InlineData(1.0,                     1.0,                     1.4142135623730950,      CrossPlatformMachineEpsilon64 * 10)]
        [InlineData(2.7182818284590452,      0.31830988618379067,     2.7368553638387594,      CrossPlatformMachineEpsilon64 * 10)]   // x: (e)   y: (1 / pi)
        [InlineData(2.7182818284590452,      0.43429448190325183,     2.7527563996732919,      CrossPlatformMachineEpsilon64 * 10)]   // x: (e)   y: (log10(e))
        [InlineData(2.7182818284590452,      0.63661977236758134,     2.7918346715914253,      CrossPlatformMachineEpsilon64 * 10)]   // x: (e)   y: (2 / pi)
        [InlineData(2.7182818284590452,      0.69314718055994531,     2.8052645352709344,      CrossPlatformMachineEpsilon64 * 10)]   // x: (e)   y: (ln(2))
        [InlineData(2.7182818284590452,      0.70710678118654752,     2.8087463571726533,      CrossPlatformMachineEpsilon64 * 10)]   // x: (e)   y: (1 / sqrt(2))
        [InlineData(2.7182818284590452,      0.78539816339744831,     2.8294710413783590,      CrossPlatformMachineEpsilon64 * 10)]   // x: (e)   y: (pi / 4)
        [InlineData(2.7182818284590452,      1.0,                     2.8963867315900082,      CrossPlatformMachineEpsilon64 * 10)]   // x: (e)
        [InlineData(2.7182818284590452,      1.1283791670955126,      2.9431778138036127,      CrossPlatformMachineEpsilon64 * 10)]   // x: (e)   y: (2 / sqrt(pi))
        [InlineData(2.7182818284590452,      1.4142135623730950,      3.0641566701020120,      CrossPlatformMachineEpsilon64 * 10)]   // x: (e)   y: (sqrt(2))
        [InlineData(2.7182818284590452,      1.4426950408889634,      3.0774055761202907,      CrossPlatformMachineEpsilon64 * 10)]   // x: (e)   y: (log2(e))
        [InlineData(2.7182818284590452,      1.5707963267948966,      3.1394995141268918,      CrossPlatformMachineEpsilon64 * 10)]   // x: (e)   y: (pi / 2)
        [InlineData(2.7182818284590452,      2.3025850929940457,      3.5624365551415857,      CrossPlatformMachineEpsilon64 * 10)]   // x: (e)   y: (ln(10))
        [InlineData(2.7182818284590452,      2.7182818284590452,      3.8442310281591168,      CrossPlatformMachineEpsilon64 * 10)]   // x: (e)   y: (e)
        [InlineData(2.7182818284590452,      3.1415926535897932,      4.1543544023133136,      CrossPlatformMachineEpsilon64 * 10)]   // x: (e)   y: (pi)
        [InlineData(10.0,                    0.31830988618379067,     10.005064776584025,      CrossPlatformMachineEpsilon64 * 100)]  //          y: (1 / pi)
        [InlineData(10.0,                    0.43429448190325183,     10.009426142242702,      CrossPlatformMachineEpsilon64 * 100)]  //          y: (log10(e))
        [InlineData(10.0,                    0.63661977236758134,     10.020243746265325,      CrossPlatformMachineEpsilon64 * 100)]  //          y: (2 / pi)
        [InlineData(10.0,                    0.69314718055994531,     10.023993865417028,      CrossPlatformMachineEpsilon64 * 100)]  //          y: (ln(2))
        [InlineData(10.0,                    0.70710678118654752,     10.024968827881711,      CrossPlatformMachineEpsilon64 * 100)]  //          y: (1 / sqrt(2))
        [InlineData(10.0,                    0.78539816339744831,     10.030795096853892,      CrossPlatformMachineEpsilon64 * 100)]  //          y: (pi / 4)
        [InlineData(10.0,                    1.0,                     10.049875621120890,      CrossPlatformMachineEpsilon64 * 100)]  //
        [InlineData(10.0,                    1.1283791670955126,      10.063460614755501,      CrossPlatformMachineEpsilon64 * 100)]  //          y: (2 / sqrt(pi))
        [InlineData(10.0,                    1.4142135623730950,      10.099504938362078,      CrossPlatformMachineEpsilon64 * 100)]  //          y: (sqrt(2))
        [InlineData(10.0,                    1.4426950408889634,      10.103532500121213,      CrossPlatformMachineEpsilon64 * 100)]  //          y: (log2(e))
        [InlineData(10.0,                    1.5707963267948966,      10.122618292728040,      CrossPlatformMachineEpsilon64 * 100)]  //          y: (pi / 2)
        [InlineData(10.0,                    2.3025850929940457,      10.261671311754163,      CrossPlatformMachineEpsilon64 * 100)]  //          y: (ln(10))
        [InlineData(10.0,                    2.7182818284590452,      10.362869105558106,      CrossPlatformMachineEpsilon64 * 100)]  //          y: (e)
        [InlineData(10.0,                    3.1415926535897932,      10.481870272097884,      CrossPlatformMachineEpsilon64 * 100)]  //          y: (pi)
        [InlineData(double.PositiveInfinity, double.NaN,              double.PositiveInfinity, 0.0)]
        [InlineData(double.PositiveInfinity, 0.0,                     double.PositiveInfinity, 0.0)]
        [InlineData(double.PositiveInfinity, 1.0,                     double.PositiveInfinity, 0.0)]
        [InlineData(double.PositiveInfinity, 2.7182818284590452,      double.PositiveInfinity, 0.0)]
        [InlineData(double.PositiveInfinity, 10.0,                    double.PositiveInfinity, 0.0)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity, 0.0)]
        public static void Hypot64(double x, double y, double expectedResult, double allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, NFloat.Hypot((NFloat)(-x), (NFloat)(-y)), allowedVariance);
            AssertExtensions.Equal(expectedResult, NFloat.Hypot((NFloat)(-x), (NFloat)(+y)), allowedVariance);
            AssertExtensions.Equal(expectedResult, NFloat.Hypot((NFloat)(+x), (NFloat)(-y)), allowedVariance);
            AssertExtensions.Equal(expectedResult, NFloat.Hypot((NFloat)(+x), (NFloat)(+y)), allowedVariance);

            AssertExtensions.Equal(expectedResult, NFloat.Hypot((NFloat)(-y), (NFloat)(-x)), allowedVariance);
            AssertExtensions.Equal(expectedResult, NFloat.Hypot((NFloat)(-y), (NFloat)(+x)), allowedVariance);
            AssertExtensions.Equal(expectedResult, NFloat.Hypot((NFloat)(+y), (NFloat)(-x)), allowedVariance);
            AssertExtensions.Equal(expectedResult, NFloat.Hypot((NFloat)(+y), (NFloat)(+x)), allowedVariance);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is64BitProcess))]
        [InlineData( double.NegativeInfinity, -5, -0.0,                     0.0)]
        [InlineData( double.NegativeInfinity, -4,  double.NaN,              0.0)]
        [InlineData( double.NegativeInfinity, -3, -0.0,                     0.0)]
        [InlineData( double.NegativeInfinity, -2,  double.NaN,              0.0)]
        [InlineData( double.NegativeInfinity, -1, -0.0,                     0.0)]
        [InlineData( double.NegativeInfinity,  0,  double.NaN,              0.0)]
        [InlineData( double.NegativeInfinity,  1,  double.NegativeInfinity, 0.0)]
        [InlineData( double.NegativeInfinity,  2,  double.NaN,              0.0)]
        [InlineData( double.NegativeInfinity,  3,  double.NegativeInfinity, 0.0)]
        [InlineData( double.NegativeInfinity,  4,  double.NaN,              0.0)]
        [InlineData( double.NegativeInfinity,  5,  double.NegativeInfinity, 0.0)]
        [InlineData(-2.7182818284590452,      -5, -0.8187307530779819,      CrossPlatformMachineEpsilon64 * 10)]
        [InlineData(-2.7182818284590452,      -4,  double.NaN,              0.0)]
        [InlineData(-2.7182818284590452,      -3, -0.7165313105737893,      CrossPlatformMachineEpsilon64 * 10)]
        [InlineData(-2.7182818284590452,      -2,  double.NaN,              0.0)]
        [InlineData(-2.7182818284590452,      -1, -0.3678794411714423,      CrossPlatformMachineEpsilon64 * 10)]
        [InlineData(-2.7182818284590452,       0,  double.NaN,              0.0)]
        [InlineData(-2.7182818284590452,       1, -2.7182818284590452,      CrossPlatformMachineEpsilon64 * 10)]
        [InlineData(-2.7182818284590452,       2,  double.NaN,              0.0)]
        [InlineData(-2.7182818284590452,       3, -1.3956124250860895,      CrossPlatformMachineEpsilon64 * 10)]
        [InlineData(-2.7182818284590452,       4,  double.NaN,              0.0)]
        [InlineData(-2.7182818284590452,       5, -1.2214027581601698,      CrossPlatformMachineEpsilon64 * 10)]
        [InlineData(-1.0,                     -5, -1.0,                     0.0)]
        [InlineData(-1.0,                     -4,  double.NaN,              0.0)]
        [InlineData(-1.0,                     -3, -1.0,                     0.0)]
        [InlineData(-1.0,                     -2,  double.NaN,              0.0)]
        [InlineData(-1.0,                     -1, -1.0,                     0.0)]
        [InlineData(-1.0,                      0,  double.NaN,              0.0)]
        [InlineData(-1.0,                      1, -1.0,                     0.0)]
        [InlineData(-1.0,                      2,  double.NaN,              0.0)]
        [InlineData(-1.0,                      3, -1.0,                     0.0)]
        [InlineData(-1.0,                      4,  double.NaN,              0.0)]
        [InlineData(-1.0,                      5, -1.0,                     0.0)]
        [InlineData(-0.0,                     -5,  double.NegativeInfinity, 0.0)]
        [InlineData(-0.0,                     -4,  double.PositiveInfinity, 0.0)]
        [InlineData(-0.0,                     -3,  double.NegativeInfinity, 0.0)]
        [InlineData(-0.0,                     -2,  double.PositiveInfinity, 0.0)]
        [InlineData(-0.0,                     -1,  double.NegativeInfinity, 0.0)]
        [InlineData(-0.0,                      0,  double.NaN,              0.0)]
        [InlineData(-0.0,                      1, -0.0,                     0.0)]
        [InlineData(-0.0,                      2,  0.0,                     0.0)]
        [InlineData(-0.0,                      3, -0.0,                     0.0)]
        [InlineData(-0.0,                      4,  0.0,                     0.0)]
        [InlineData(-0.0,                      5, -0.0,                     0.0)]
        [InlineData( double.NaN,              -5,  double.NaN,              0.0)]
        [InlineData( double.NaN,              -4,  double.NaN,              0.0)]
        [InlineData( double.NaN,              -3,  double.NaN,              0.0)]
        [InlineData( double.NaN,              -2,  double.NaN,              0.0)]
        [InlineData( double.NaN,              -1,  double.NaN,              0.0)]
        [InlineData( double.NaN,               0,  double.NaN,              0.0)]
        [InlineData( double.NaN,               1,  double.NaN,              0.0)]
        [InlineData( double.NaN,               2,  double.NaN,              0.0)]
        [InlineData( double.NaN,               3,  double.NaN,              0.0)]
        [InlineData( double.NaN,               4,  double.NaN,              0.0)]
        [InlineData( double.NaN,               5,  double.NaN,              0.0)]
        [InlineData( 0.0,                     -5,  double.PositiveInfinity, 0.0)]
        [InlineData( 0.0,                     -4,  double.PositiveInfinity, 0.0)]
        [InlineData( 0.0,                     -3,  double.PositiveInfinity, 0.0)]
        [InlineData( 0.0,                     -2,  double.PositiveInfinity, 0.0)]
        [InlineData( 0.0,                     -1,  double.PositiveInfinity, 0.0)]
        [InlineData( 0.0,                      0,  double.NaN,              0.0)]
        [InlineData( 0.0,                      1,  0.0,                     0.0)]
        [InlineData( 0.0,                      2,  0.0,                     0.0)]
        [InlineData( 0.0,                      3,  0.0,                     0.0)]
        [InlineData( 0.0,                      4,  0.0,                     0.0)]
        [InlineData( 0.0,                      5,  0.0,                     0.0)]
        [InlineData( 1.0,                     -5,  1.0,                     0.0)]
        [InlineData( 1.0,                     -4,  1.0,                     0.0)]
        [InlineData( 1.0,                     -3,  1.0,                     0.0)]
        [InlineData( 1.0,                     -2,  1.0,                     0.0)]
        [InlineData( 1.0,                     -1,  1.0,                     0.0)]
        [InlineData( 1.0,                      0,  double.NaN,              0.0)]
        [InlineData( 1.0,                      1,  1.0,                     0.0)]
        [InlineData( 1.0,                      2,  1.0,                     0.0)]
        [InlineData( 1.0,                      3,  1.0,                     0.0)]
        [InlineData( 1.0,                      4,  1.0,                     0.0)]
        [InlineData( 1.0,                      5,  1.0,                     0.0)]
        [InlineData( 2.7182818284590452,      -5,  0.8187307530779819,      CrossPlatformMachineEpsilon64 * 10)]
        [InlineData( 2.7182818284590452,      -4,  0.7788007830714049,      CrossPlatformMachineEpsilon64 * 10)]
        [InlineData( 2.7182818284590452,      -3,  0.7165313105737893,      CrossPlatformMachineEpsilon64 * 10)]
        [InlineData( 2.7182818284590452,      -2,  0.6065306597126334,      CrossPlatformMachineEpsilon64 * 10)]
        [InlineData( 2.7182818284590452,      -1,  0.3678794411714423,      CrossPlatformMachineEpsilon64 * 10)]
        [InlineData( 2.7182818284590452,       0,  double.NaN,              0.0)]
        [InlineData( 2.7182818284590452,       1,  2.7182818284590452,      0.0)]
        [InlineData( 2.7182818284590452,       2,  1.6487212707001281,      CrossPlatformMachineEpsilon64 * 10)]
        [InlineData( 2.7182818284590452,       3,  1.3956124250860895,      CrossPlatformMachineEpsilon64 * 10)]
        [InlineData( 2.7182818284590452,       4,  1.2840254166877415,      CrossPlatformMachineEpsilon64 * 10)]
        [InlineData( 2.7182818284590452,       5,  1.2214027581601698,      CrossPlatformMachineEpsilon64 * 10)]
        [InlineData( double.PositiveInfinity, -5,  0.0f,                    0.0)]
        [InlineData( double.PositiveInfinity, -4,  0.0f,                    0.0)]
        [InlineData( double.PositiveInfinity, -3,  0.0f,                    0.0)]
        [InlineData( double.PositiveInfinity, -2,  0.0f,                    0.0)]
        [InlineData( double.PositiveInfinity, -1,  0.0f,                    0.0)]
        [InlineData( double.PositiveInfinity,  0,  double.NaN,              0.0)]
        [InlineData( double.PositiveInfinity,  1,  double.PositiveInfinity, 0.0)]
        [InlineData( double.PositiveInfinity,  2,  double.PositiveInfinity, 0.0)]
        [InlineData( double.PositiveInfinity,  3,  double.PositiveInfinity, 0.0)]
        [InlineData( double.PositiveInfinity,  4,  double.PositiveInfinity, 0.0)]
        [InlineData( double.PositiveInfinity,  5,  double.PositiveInfinity, 0.0)]
        public static void RootN64(double x, int n, double expectedResult, double allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, NFloat.RootN((NFloat)x, n), allowedVariance);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is64BitProcess))]
        [InlineData( double.NaN,          double.NaN,          0.0)]
        [InlineData( 1.0,                 0.0,                 0.0)]
        [InlineData( 0.54030230586813972, 0.31830988618379067, CrossPlatformMachineEpsilon64)]
        [InlineData( 0.20495719432643395, 0.43429448190325183, CrossPlatformMachineEpsilon64)]
        [InlineData( 0.0,                 0.5,                 0.0)]
        [InlineData(-0.41614683654714239, 0.63661977236758134, CrossPlatformMachineEpsilon64)]
        [InlineData(-0.57023324876887755, 0.69314718055994531, CrossPlatformMachineEpsilon64)]
        [InlineData(-0.60569986707881343, 0.70710678118654752, CrossPlatformMachineEpsilon64)]
        [InlineData(-0.78121189211048819, 0.78539816339744831, CrossPlatformMachineEpsilon64)]
        [InlineData(-1.0,                 1.0,                 0.0)]
        [InlineData(-0.91976499476851874, 0.87162083290448743, CrossPlatformMachineEpsilon64)]
        [InlineData(-0.26625534204141549, 0.58578643762690495, CrossPlatformMachineEpsilon64)]
        [InlineData(-0.17905794598427576, 0.55730495911103659, CrossPlatformMachineEpsilon64)]
        [InlineData( 0.22058404074969809, 0.42920367320510338, CrossPlatformMachineEpsilon64)]
        [InlineData( 0.58119566361426737, 0.30258509299404568, CrossPlatformMachineEpsilon64)]
        [InlineData(-0.63325565131482003, 0.71828182845904523, CrossPlatformMachineEpsilon64)]
        [InlineData(-0.90268536193307107, 0.85840734641020676, CrossPlatformMachineEpsilon64)]
        public static void AcosPiTest64(double value, double expectedResult, double allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, NFloat.AcosPi((NFloat)(value)), allowedVariance);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is64BitProcess))]
        [InlineData( double.NaN,           double.NaN,          0.0)]
        [InlineData( 0.0,                  0.0,                 0.0)]
        [InlineData( 0.84147098480789651,  0.31830988618379067, CrossPlatformMachineEpsilon64)]
        [InlineData( 0.97877093770393305,  0.43429448190325183, CrossPlatformMachineEpsilon64)]
        [InlineData( 1.0,                  0.5,                 0.0)]
        [InlineData( 0.90929742682568170,  0.36338022763241866, CrossPlatformMachineEpsilon64)]
        [InlineData( 0.82148283122563883,  0.30685281944005469, CrossPlatformMachineEpsilon64)]
        [InlineData( 0.79569320156748087,  0.29289321881345248, CrossPlatformMachineEpsilon64)]
        [InlineData( 0.62426595263969903,  0.21460183660255169, CrossPlatformMachineEpsilon64)]
        [InlineData(-0.39246955856278420, -0.12837916709551257, CrossPlatformMachineEpsilon64)]
        [InlineData(-0.96390253284987733, -0.41421356237309505, CrossPlatformMachineEpsilon64)]
        [InlineData(-0.98383852942436249, -0.44269504088896341, CrossPlatformMachineEpsilon64)]
        [InlineData(-0.97536797208363139, -0.42920367320510338, CrossPlatformMachineEpsilon64)]
        [InlineData( 0.81376384817462330,  0.30258509299404568, CrossPlatformMachineEpsilon64)]
        [InlineData( 0.77394268526670828,  0.28171817154095476, CrossPlatformMachineEpsilon64)]
        [InlineData(-0.43030121700009227, -0.14159265358979324, CrossPlatformMachineEpsilon64)]
        public static void AsinPiTest64(double value, double expectedResult, double allowedVariance)
        {
            AssertExtensions.Equal(-expectedResult, NFloat.AsinPi((NFloat)(-value)), allowedVariance);
            AssertExtensions.Equal(+expectedResult, NFloat.AsinPi((NFloat)(+value)), allowedVariance);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is64BitProcess))]
        [InlineData( double.NaN,               double.NaN,              double.NaN,           0.0)]
        [InlineData( 0.0,                     -1.0,                      1.0,                 CrossPlatformMachineEpsilon64)]   // y: sinpi(0)              x:  cospi(1)            ; This should be exact, but has an issue on WASM/Unix
        [InlineData( 0.0,                     -0.0,                      1.0,                 CrossPlatformMachineEpsilon64)]   // y: sinpi(0)              x: -cospi(0.5)          ; This should be exact, but has an issue on WASM/Unix
        [InlineData( 0.0,                      0.0,                      0.0,                 0.0)]                             // y: sinpi(0)              x:  cospi(0.5)
        [InlineData( 0.0,                      1.0,                      0.0,                 0.0)]                             // y: sinpi(0)              x:  cospi(0)
        [InlineData( 0.84147098480789651,      0.54030230586813972,      0.31830988618379067, CrossPlatformMachineEpsilon64)]   // y: sinpi(1 / pi)         x:  cospi(1 / pi)
        [InlineData( 0.97877093770393305,      0.20495719432643395,      0.43429448190325183, CrossPlatformMachineEpsilon64)]   // y: sinpi(log10(e))       x:  cospi(log10(e))
        [InlineData( 1.0,                     -0.0,                      0.5,                 CrossPlatformMachineEpsilon64)]   // y: sinpi(0.5)            x: -cospi(0.5)          ; This should be exact, but has an issue on WASM/Unix
        [InlineData( 1.0,                      0.0,                      0.5,                 CrossPlatformMachineEpsilon64)]   // y: sinpi(0.5)            x:  cospi(0.5)          ; This should be exact, but has an issue on WASM/Unix
        [InlineData( 0.90929742682568170,     -0.41614683654714239,      0.63661977236758134, CrossPlatformMachineEpsilon64)]   // y: sinpi(2 / pi)         x:  cospi(2 / pi)
        [InlineData( 0.82148283122563883,     -0.57023324876887755,      0.69314718055994531, CrossPlatformMachineEpsilon64)]   // y: sinpi(ln(2))          x:  cospi(ln(2))
        [InlineData( 0.79569320156748087,     -0.60569986707881343,      0.70710678118654752, CrossPlatformMachineEpsilon64)]   // y: sinpi(1 / sqrt(2))    x:  cospi(1 / sqrt(2))
        [InlineData( 0.62426595263969903,     -0.78121189211048819,      0.78539816339744831, CrossPlatformMachineEpsilon64)]   // y: sinpi(pi / 4)         x:  cospi(pi / 4)
        [InlineData(-0.39246955856278420,     -0.91976499476851874,     -0.87162083290448743, CrossPlatformMachineEpsilon64)]   // y: sinpi(2 / sqrt(pi))   x:  cospi(2 / sqrt(pi))
        [InlineData(-0.96390253284987733,     -0.26625534204141549,     -0.58578643762690495, CrossPlatformMachineEpsilon64)]   // y: sinpi(sqrt(2))        x:  cospi(sqrt(2))
        [InlineData(-0.98383852942436249,     -0.17905794598427576,     -0.55730495911103659, CrossPlatformMachineEpsilon64)]   // y: sinpi(log2(e))        x:  cospi(log2(e))
        [InlineData(-0.97536797208363139,      0.22058404074969809,     -0.42920367320510338, CrossPlatformMachineEpsilon64)]   // y: sinpi(pi / 2)         x:  cospi(pi / 2)
        [InlineData( 0.81376384817462330,      0.58119566361426737,      0.30258509299404568, CrossPlatformMachineEpsilon64)]   // y: sinpi(ln(10))         x:  cospi(ln(10))
        [InlineData( 0.77394268526670828,     -0.63325565131482003,      0.71828182845904524, CrossPlatformMachineEpsilon64)]   // y: sinpi(e)              x:  cospi(e)
        [InlineData(-0.43030121700009227,     -0.90268536193307107,     -0.85840734641020676, CrossPlatformMachineEpsilon64)]   // y: sinpi(pi)             x:  cospi(pi)
        [InlineData( 1.0,                      double.NegativeInfinity,  1.0,                 CrossPlatformMachineEpsilon64)]   // y: sinpi(0.5)                                    ; This should be exact, but has an issue on WASM/Unix
        [InlineData( 1.0,                      double.PositiveInfinity,  0.0,                 0.0)]                             // y: sinpi(0.5)
        [InlineData( double.PositiveInfinity, -1.0,                      0.5,                 CrossPlatformMachineEpsilon64)]   //                          x:  cospi(1)            ; This should be exact, but has an issue on WASM/Unix
        [InlineData( double.PositiveInfinity,  1.0,                      0.5,                 CrossPlatformMachineEpsilon64)]   //                          x:  cospi(0)            ; This should be exact, but has an issue on WASM/Unix
        [InlineData( double.PositiveInfinity,  double.NegativeInfinity,  0.75,                CrossPlatformMachineEpsilon64)]   //                                                  ; This should be exact, but has an issue on WASM/Unix
        [InlineData( double.PositiveInfinity,  double.PositiveInfinity,  0.25,                CrossPlatformMachineEpsilon64)]   //                                                  ; This should be exact, but has an issue on WASM/Unix
        public static void Atan2PiTest(double y, double x, double expectedResult, double allowedVariance)
        {
            AssertExtensions.Equal(-expectedResult, NFloat.Atan2Pi((NFloat)(-y), (NFloat)(+x)), allowedVariance);
            AssertExtensions.Equal(+expectedResult, NFloat.Atan2Pi((NFloat)(+y), (NFloat)(+x)), allowedVariance);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is64BitProcess))]
        [InlineData( double.NaN,               double.NaN,          0.0)]
        [InlineData( 0.0,                      0.0,                 0.0)]
        [InlineData( 1.5574077246549022,       0.31830988618379067, CrossPlatformMachineEpsilon64)]
        [InlineData( 4.7754895402454188,       0.43429448190325183, CrossPlatformMachineEpsilon64)]
        [InlineData( double.PositiveInfinity,  0.5,                 0.0)]
        [InlineData(-2.1850398632615190,      -0.36338022763241866, CrossPlatformMachineEpsilon64)]
        [InlineData(-1.4406084404920341,      -0.30685281944005469, CrossPlatformMachineEpsilon64)]
        [InlineData(-1.3136757077477542,      -0.29289321881345248, CrossPlatformMachineEpsilon64)]
        [InlineData(-0.79909939792801821,     -0.21460183660255169, CrossPlatformMachineEpsilon64)]
        [InlineData( 0.42670634433261806,      0.12837916709551257, CrossPlatformMachineEpsilon64)]
        [InlineData( 3.6202185671074506,       0.41421356237309505, CrossPlatformMachineEpsilon64)]
        [InlineData( 5.4945259425167300,       0.44269504088896341, CrossPlatformMachineEpsilon64)]
        [InlineData(-4.4217522209161288,      -0.42920367320510338, CrossPlatformMachineEpsilon64)]
        [InlineData( 1.4001547140150527,       0.30258509299404568, CrossPlatformMachineEpsilon64)]
        [InlineData(-1.2221646718190066,      -0.28171817154095476, CrossPlatformMachineEpsilon64)]
        [InlineData( 0.47669014603118892,      0.14159265358979324, CrossPlatformMachineEpsilon64)]
        public static void AtanPiTest64(double value, double expectedResult, double allowedVariance)
        {
            AssertExtensions.Equal(-expectedResult, NFloat.AtanPi((NFloat)(-value)), allowedVariance);
            AssertExtensions.Equal(+expectedResult, NFloat.AtanPi((NFloat)(+value)), allowedVariance);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is64BitProcess))]
        [InlineData(double.NaN,               double.NaN,          0.0)]
        [InlineData(0.0,                      1.0,                 0.0)]
        [InlineData(0.31830988618379067,      0.54030230586813972, CrossPlatformMachineEpsilon64)]       // value:  (1 / pi)
        [InlineData(0.43429448190325183,      0.20495719432643395, CrossPlatformMachineEpsilon64)]       // value:  (log10(e))
        [InlineData(0.5,                      0.0,                 0.0)]
        [InlineData(0.63661977236758134,     -0.41614683654714239, CrossPlatformMachineEpsilon64)]       // value:  (2 / pi)
        [InlineData(0.69314718055994531,     -0.57023324876887755, CrossPlatformMachineEpsilon64)]       // value:  (ln(2))
        [InlineData(0.70710678118654752,     -0.60569986707881343, CrossPlatformMachineEpsilon64)]       // value:  (1 / sqrt(2))
        [InlineData(0.78539816339744831,     -0.78121189211048819, CrossPlatformMachineEpsilon64)]       // value:  (pi / 4)
        [InlineData(1.0,                     -1.0,                 0.0)]
        [InlineData(1.1283791670955126,      -0.91976499476851874, CrossPlatformMachineEpsilon64)]       // value:  (2 / sqrt(pi))
        [InlineData(1.4142135623730950,      -0.26625534204141549, CrossPlatformMachineEpsilon64)]       // value:  (sqrt(2))
        [InlineData(1.4426950408889634,      -0.17905794598427576, CrossPlatformMachineEpsilon64)]       // value:  (log2(e))
        [InlineData(1.5,                      0.0,                 0.0)]
        [InlineData(1.5707963267948966,       0.22058404074969809, CrossPlatformMachineEpsilon64)]       // value:  (pi / 2)
        [InlineData(2.0,                      1.0,                 0.0)]
        [InlineData(2.3025850929940457,       0.58119566361426737, CrossPlatformMachineEpsilon64)]       // value:  (ln(10))
        [InlineData(2.5,                      0.0,                 0.0)]
        [InlineData(2.7182818284590452,      -0.63325565131482003, CrossPlatformMachineEpsilon64)]       // value:  (e)
        [InlineData(3.0,                     -1.0,                 0.0)]
        [InlineData(3.1415926535897932,      -0.90268536193307107, CrossPlatformMachineEpsilon64)]       // value:  (pi)
        [InlineData(3.5,                      0.0,                 0.0)]
        [InlineData(double.PositiveInfinity,  double.NaN,          0.0)]
        public static void CosPiTest64(double value, double expectedResult, double allowedVariance)
        {
            AssertExtensions.Equal(+expectedResult, NFloat.CosPi((NFloat)(-value)), allowedVariance);
            AssertExtensions.Equal(+expectedResult, NFloat.CosPi((NFloat)(+value)), allowedVariance);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is64BitProcess))]
        [InlineData(double.NaN,               double.NaN,          0.0)]
        [InlineData(0.0,                      0.0,                 0.0)]
        [InlineData(0.31830988618379067,      0.84147098480789651, CrossPlatformMachineEpsilon64)]       // value:  (1 / pi)
        [InlineData(0.43429448190325183,      0.97877093770393305, CrossPlatformMachineEpsilon64)]       // value:  (log10(e))
        [InlineData(0.5,                      1.0,                 0.0)]
        [InlineData(0.63661977236758134,      0.90929742682568170, CrossPlatformMachineEpsilon64)]       // value:  (2 / pi)
        [InlineData(0.69314718055994531,      0.82148283122563883, CrossPlatformMachineEpsilon64)]       // value:  (ln(2))
        [InlineData(0.70710678118654752,      0.79569320156748087, CrossPlatformMachineEpsilon64)]       // value:  (1 / sqrt(2))
        [InlineData(0.78539816339744831,      0.62426595263969903, CrossPlatformMachineEpsilon64)]       // value:  (pi / 4)
        [InlineData(1.0,                      0.0,                 0.0)]
        [InlineData(1.1283791670955126,      -0.39246955856278420, CrossPlatformMachineEpsilon64)]       // value:  (2 / sqrt(pi))
        [InlineData(1.4142135623730950,      -0.96390253284987733, CrossPlatformMachineEpsilon64)]       // value:  (sqrt(2))
        [InlineData(1.4426950408889634,      -0.98383852942436249, CrossPlatformMachineEpsilon64)]       // value:  (log2(e))
        [InlineData(1.5,                     -1.0,                 0.0)]
        [InlineData(1.5707963267948966,      -0.97536797208363139, CrossPlatformMachineEpsilon64)]       // value:  (pi / 2)
        [InlineData(2.0,                      0.0,                 0.0)]
        [InlineData(2.3025850929940457,       0.81376384817462330, CrossPlatformMachineEpsilon64)]       // value:  (ln(10))
        [InlineData(2.5,                      1.0,                 0.0)]
        [InlineData(2.7182818284590452,       0.77394268526670828, CrossPlatformMachineEpsilon64)]       // value:  (e)
        [InlineData(3.0,                      0.0,                 0.0)]
        [InlineData(3.1415926535897932,      -0.43030121700009227, CrossPlatformMachineEpsilon64)]       // value:  (pi)
        [InlineData(3.5,                     -1.0,                 0.0)]
        [InlineData(double.PositiveInfinity,  double.NaN,          0.0)]
        public static void SinPiTest64(double value, double expectedResult, double allowedVariance)
        {
            AssertExtensions.Equal(-expectedResult, NFloat.SinPi((NFloat)(-value)), allowedVariance);
            AssertExtensions.Equal(+expectedResult, NFloat.SinPi((NFloat)(+value)), allowedVariance);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is64BitProcess))]
        [InlineData(double.NaN,               double.NaN,              0.0)]
        [InlineData(0.0,                      0.0,                     0.0)]
        [InlineData(0.31830988618379067,      1.5574077246549022,      CrossPlatformMachineEpsilon64 * 10)]  // value:  (1 / pi)
        [InlineData(0.43429448190325183,      4.7754895402454188,      CrossPlatformMachineEpsilon64 * 10)]  // value:  (log10(e))
        [InlineData(0.5,                      double.PositiveInfinity, 0.0)]
        [InlineData(0.63661977236758134,     -2.1850398632615190,      CrossPlatformMachineEpsilon64 * 10)]  // value:  (2 / pi)
        [InlineData(0.69314718055994531,     -1.4406084404920341,      CrossPlatformMachineEpsilon64 * 10)]  // value:  (ln(2))
        [InlineData(0.70710678118654752,     -1.3136757077477542,      CrossPlatformMachineEpsilon64 * 10)]  // value:  (1 / sqrt(2))
        [InlineData(0.78539816339744831,     -0.79909939792801821,     CrossPlatformMachineEpsilon64)]       // value:  (pi / 4)
        [InlineData(1.0,                     -0.0,                     0.0)]
        [InlineData(1.1283791670955126,       0.42670634433261806,     CrossPlatformMachineEpsilon64)]       // value:  (2 / sqrt(pi))
        [InlineData(1.4142135623730950,       3.6202185671074506,      CrossPlatformMachineEpsilon64 * 10)]  // value:  (sqrt(2))
        [InlineData(1.4426950408889634,       5.4945259425167300,      CrossPlatformMachineEpsilon64 * 10)]  // value:  (log2(e))
        [InlineData(1.5,                      double.NegativeInfinity, 0.0)]
        [InlineData(1.5707963267948966,      -4.4217522209161288,      CrossPlatformMachineEpsilon64 * 10)]  // value:  (pi / 2)
        [InlineData(2.0,                      0.0,                     0.0)]
        [InlineData(2.3025850929940457,       1.4001547140150527,      CrossPlatformMachineEpsilon64 * 10)]  // value:  (ln(10))
        [InlineData(2.5,                      double.PositiveInfinity, 0.0)]
        [InlineData(2.7182818284590452,      -1.2221646718190066,      CrossPlatformMachineEpsilon64 * 10)]  // value:  (e)
        [InlineData(3.0,                     -0.0,                     0.0)]
        [InlineData(3.1415926535897932,       0.47669014603118892,     CrossPlatformMachineEpsilon64)]       // value:  (pi)
        [InlineData(3.5,                      double.NegativeInfinity, 0.0)]
        [InlineData(double.PositiveInfinity,  double.NaN,              0.0)]
        public static void TanPiTest64(double value, double expectedResult, double allowedVariance)
        {
            AssertExtensions.Equal(-expectedResult, NFloat.TanPi((NFloat)(-value)), allowedVariance);
            AssertExtensions.Equal(+expectedResult, NFloat.TanPi((NFloat)(+value)), allowedVariance);
        }
    }
}
