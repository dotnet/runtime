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
        public void Ctor_Int()
        {
            NFloat value = new NFloat(42.0f);
            Assert.Equal(42.0, value.Value);
        }

        [Fact]
        public void Ctor_NInt()
        {
            NFloat value = new NFloat(42.0);
            Assert.Equal(42.0, value.Value);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.Is32BitProcess))]
        public void Ctor_NInt_OutOfRange()
        {
            Assert.Throws<OverflowException>(() => new NFloat(double.MaxValue));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.Is64BitProcess))]
        public void Ctor_NInt_LargeValue()
        {
            NFloat value = new NFloat(double.MaxValue);
            Assert.Equal(double.MaxValue, value.Value);
        }

        [Theory]
        [InlineData(789.0f, 789.0f, true)]
        [InlineData(789.0f, -789.0f, false)]
        [InlineData(789.0f, 0.0f, false)]
        [InlineData(float.NaN, float.NaN, true)]
        [InlineData(789.0f, 789.0, false)]
        [InlineData(789.0f, "789", false)]
        public static void EqualsTest(float f1, object obj, bool expected)
        {
            if (obj is float f)
            {
                NFloat f2 = new NFloat(f);
                Assert.Equal(expected, new NFloat(f1).Equals(f2));
                Assert.Equal(expected, new NFloat(f1).GetHashCode().Equals(f2.GetHashCode()));
            }
            Assert.Equal(expected, new NFloat(f1).Equals(obj));
        }

        [Theory]
        [InlineData(-4567.0f, "-4567")]
        [InlineData(-4567.89101f, "-4567.891")]
        [InlineData(0.0f, "0")]
        [InlineData(4567.0f, "4567")]
        [InlineData(4567.89101f, "4567.891")]

        [InlineData(float.NaN, "NaN")]
        public static void ToStringTest(float value, string expected)
        {
            NFloat nfloat = new NFloat(value);

            Assert.Equal(expected, nfloat.ToString());
        }
    }
}
