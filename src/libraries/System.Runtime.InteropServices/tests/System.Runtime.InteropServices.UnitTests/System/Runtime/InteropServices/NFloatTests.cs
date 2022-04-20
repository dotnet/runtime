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
    }
}
