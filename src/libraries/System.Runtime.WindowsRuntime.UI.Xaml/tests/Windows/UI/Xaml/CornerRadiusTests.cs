// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;

namespace Windows.UI.Xaml.Tests
{
    public class CornerRadiusTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var cornerRadius = new CornerRadius();
            Assert.Equal(0, cornerRadius.TopLeft);
            Assert.Equal(0, cornerRadius.TopRight);
            Assert.Equal(0, cornerRadius.BottomRight);
            Assert.Equal(0, cornerRadius.BottomLeft);
        }

        public static IEnumerable<object[]> ValidDoubles_TestData()
        {
            yield return new object[] { 0 };
            yield return new object[] { 1 };
            yield return new object[] { double.MaxValue };
            yield return new object[] { double.PositiveInfinity };
        }

        [Theory]
        [MemberData(nameof(ValidDoubles_TestData))]
        public void Ctor_UniformRadius(double uniformRadius)
        {
            var cornerRadius = new CornerRadius(uniformRadius);
            Assert.Equal(uniformRadius, cornerRadius.TopLeft);
            Assert.Equal(uniformRadius, cornerRadius.TopRight);
            Assert.Equal(uniformRadius, cornerRadius.BottomRight);
            Assert.Equal(uniformRadius, cornerRadius.BottomLeft);
        }

        private static List<double> InvalidDoubles { get; } = new List<double> { -1, double.NaN, double.NegativeInfinity };

        public static IEnumerable<object[]> InvalidDoubles_TestData() => InvalidDoubles.Select(f => new object[] { f });

        [Theory]
        [MemberData(nameof(InvalidDoubles_TestData))]
        public void Ctor_InvalidUniformRadius_ThrowsArgumentException(double uniformRadius)
        {
            AssertExtensions.Throws<ArgumentException>(null, () => new CornerRadius(uniformRadius));
        }

        [Theory]
        [InlineData(0, 0, 0, 0)]
        [InlineData(1, 2, 3, 4)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity)]
        [InlineData(double.MaxValue, double.MaxValue, double.MaxValue, double.MaxValue)]
        public void Ctor_TopLeft_TopRight_BottomRight_BottomLeft(double topLeft, double topRight, double bottomRight, double bottomLeft)
        {
            var cornerRadius = new CornerRadius(topLeft, topRight, bottomRight, bottomLeft);
            Assert.Equal(topLeft, cornerRadius.TopLeft);
            Assert.Equal(topRight, cornerRadius.TopRight);
            Assert.Equal(bottomRight, cornerRadius.BottomRight);
            Assert.Equal(bottomLeft, cornerRadius.BottomLeft);
        }

        public static IEnumerable<object[]> Ctor_InvalidValues_TestData()
        {
            foreach (double invalidValue in InvalidDoubles)
            {
                yield return new object[] { invalidValue, 0, 0, 0 };
                yield return new object[] { 0, invalidValue, 0, 0 };
                yield return new object[] { 0, 0, invalidValue, 0 };
                yield return new object[] { 0, 0, 0, invalidValue };
            }
        }

        [Theory]
        [MemberData(nameof(Ctor_InvalidValues_TestData))]
        public void Ctor_InvalidValues_ThrowsArgumentException(double topLeft, double topRight, double bottomRight, double bottomLeft)
        {
            AssertExtensions.Throws<ArgumentException>(null, () => new CornerRadius(topLeft, topRight, bottomRight, bottomLeft));
        }

        [Theory]
        [MemberData(nameof(ValidDoubles_TestData))]
        public void TopLeft_SetValid_GetReturnsExpected(double value)
        {
            var cornerRadius = new CornerRadius { TopLeft = value };
            Assert.Equal(value, cornerRadius.TopLeft);
        }

        [Theory]
        [MemberData(nameof(InvalidDoubles_TestData))]
        public void TopLeft_SetInvalid_ThrowsArgumentException(double value)
        {
            var cornerRadius = new CornerRadius();
            AssertExtensions.Throws<ArgumentException>(null, () => cornerRadius.TopLeft = value);
        }

        [Theory]
        [MemberData(nameof(ValidDoubles_TestData))]
        public void TopRight_SetValid_GetReturnsExpected(double value)
        {
            var cornerRadius = new CornerRadius { TopRight = value };
            Assert.Equal(value, cornerRadius.TopRight);
        }

        [Theory]
        [MemberData(nameof(InvalidDoubles_TestData))]
        public void TopRight_SetInvalid_ThrowsArgumentException(double value)
        {
            var cornerRadius = new CornerRadius();
            AssertExtensions.Throws<ArgumentException>(null, () => cornerRadius.TopRight = value);
        }

        [Theory]
        [MemberData(nameof(ValidDoubles_TestData))]
        public void BottomRight_SetValid_GetReturnsExpected(double value)
        {
            var cornerRadius = new CornerRadius { BottomRight = value };
            Assert.Equal(value, cornerRadius.BottomRight);
        }

        [Theory]
        [MemberData(nameof(InvalidDoubles_TestData))]
        public void BottomRight_SetInvalid_ThrowsArgumentException(double value)
        {
            var cornerRadius = new CornerRadius();
            AssertExtensions.Throws<ArgumentException>(null, () => cornerRadius.BottomRight = value);
        }

        [Theory]
        [MemberData(nameof(ValidDoubles_TestData))]
        public void BottomLeft_SetValid_GetReturnsExpected(double value)
        {
            var cornerRadius = new CornerRadius { BottomLeft = value };
            Assert.Equal(value, cornerRadius.BottomLeft);
        }

        [Theory]
        [MemberData(nameof(InvalidDoubles_TestData))]
        public void BottomLeft_SetInvalid_ThrowsArgumentException(double value)
        {
            var cornerRadius = new CornerRadius();
            AssertExtensions.Throws<ArgumentException>(null, () => cornerRadius.BottomLeft = value);
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { new CornerRadius(1, 2, 3, 4), new CornerRadius(1, 2, 3, 4), true };
            yield return new object[] { new CornerRadius(1, 2, 3, 4), new CornerRadius(2, 2, 3, 4), false };
            yield return new object[] { new CornerRadius(1, 2, 3, 4), new CornerRadius(1, 3, 3, 4), false };
            yield return new object[] { new CornerRadius(1, 2, 3, 4), new CornerRadius(1, 2, 4, 4), false };
            yield return new object[] { new CornerRadius(1, 2, 3, 4), new CornerRadius(1, 2, 3, 5), false };

            yield return new object[] { new CornerRadius(1, 2, 3, 4), new object(), false };
            yield return new object[] { new CornerRadius(1, 2, 3, 4), null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Object_ReturnsExpected(CornerRadius cornerRadius, object other, bool expected)
        {
            Assert.Equal(expected, cornerRadius.Equals(other));
            if (other is CornerRadius otherCornerRadius)
            {
                Assert.Equal(expected, cornerRadius.Equals(otherCornerRadius));
                Assert.Equal(expected, cornerRadius == otherCornerRadius);
                Assert.Equal(!expected, cornerRadius != otherCornerRadius);
                Assert.Equal(expected, cornerRadius.GetHashCode().Equals(otherCornerRadius.GetHashCode()));
            }
        }

        [Fact]
        public void ToString_Invoke_ReturnsExpected()
        {
            var cornerRadius = new CornerRadius(1, 2.2, 3, 4);
            Assert.Equal("1,2.2,3,4", cornerRadius.ToString());
        }

        [Fact]
        public void ToString_NaN_ReturnsAuto()
        {
            CornerRadius cornerRadius = new FakeCornerRadius
            {
                TopLeft = double.NaN,
                TopRight = double.NaN,
                BottomRight = double.NaN,
                BottomLeft = double.NaN
            }.ToActual();
            Assert.Equal("Auto,Auto,Auto,Auto", cornerRadius.ToString());
        }

        public struct FakeCornerRadius
        {
            public double TopRight;
            public double TopLeft;
            public double BottomRight;
            public double BottomLeft;

            public CornerRadius ToActual()
            {
                CornerRadiusWrapper wrapper = default(CornerRadiusWrapper);
                wrapper.Fake = this;
                return wrapper.Actual;
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct CornerRadiusWrapper
        {
            [FieldOffset(0)] public CornerRadius Actual;
            [FieldOffset(0)] public FakeCornerRadius Fake;
        }
    }
}