// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class NFloatTests
    {
        [Fact]
        public void Ctor_Empty()
        {
            NFloat value = new NFloat();
            Assert.Equal(0, value.Value);
        }

        [Fact]
        public void Ctor_Float()
        {
            NFloat value = new NFloat(42.0f);
            Assert.Equal(42.0, value.Value);
        }

        [Fact]
        public void Ctor_Double()
        {
            NFloat value = new NFloat(42.0);
            Assert.Equal(42.0, value.Value);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.Is32BitProcess))]
        public void Ctor_Double_OutOfRange()
        {
            NFloat value = new NFloat(double.MaxValue);
            Assert.Equal((double)(float)double.MaxValue, value.Value);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.Is64BitProcess))]
        public void Ctor_Double_LargeValue()
        {
            NFloat value = new NFloat(double.MaxValue);
            Assert.Equal(double.MaxValue, value.Value);
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

        [Fact]
        public unsafe void Size()
        {
            int size = PlatformDetection.Is32BitProcess ? 4 : 8;
#pragma warning disable xUnit2000 // The value under test here is the sizeof expression
            Assert.Equal(size, sizeof(NFloat));
#pragma warning restore xUnit2000
            Assert.Equal(size, Marshal.SizeOf<NFloat>());
        }
    }
}
