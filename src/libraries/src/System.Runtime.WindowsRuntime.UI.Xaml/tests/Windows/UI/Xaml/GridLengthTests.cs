// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Xunit;

namespace Windows.UI.Xaml.Tests
{
    public class GridLengthTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var gridLength = new GridLength();
            Assert.Equal(GridUnitType.Auto, gridLength.GridUnitType);
            Assert.False(gridLength.IsAbsolute);
            Assert.True(gridLength.IsAuto);
            Assert.False(gridLength.IsStar);
            Assert.Equal(1, gridLength.Value);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(10)]
        [InlineData(float.MaxValue)]
        public void Ctor_Pixels(double pixels)
        {
            var gridLength = new GridLength(pixels);
            Assert.Equal(GridUnitType.Pixel, gridLength.GridUnitType);
            Assert.True(gridLength.IsAbsolute);
            Assert.False(gridLength.IsAuto);
            Assert.False(gridLength.IsStar);
            Assert.Equal(pixels, gridLength.Value);
        }

        [Theory]
        [InlineData(0, GridUnitType.Auto, 1)]
        [InlineData(0, GridUnitType.Pixel, 0)]
        [InlineData(0, GridUnitType.Star, 0)]
        [InlineData(10, GridUnitType.Pixel, 10)]
        [InlineData(float.MaxValue, GridUnitType.Star, float.MaxValue)]
        public void Ctor_Value_UnitType(double value, GridUnitType unitType, double expectedValue)
        {
            var gridLength = new GridLength(value, unitType);
            Assert.Equal(unitType, gridLength.GridUnitType);
            Assert.Equal(unitType == GridUnitType.Pixel, gridLength.IsAbsolute);
            Assert.Equal(unitType == GridUnitType.Auto, gridLength.IsAuto);
            Assert.Equal(unitType == GridUnitType.Star, gridLength.IsStar);
            Assert.Equal(expectedValue, gridLength.Value);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(double.NaN)]
        [InlineData(double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity)]
        public void Ctor_InvalidValue_ThrowsArgumentException(double value)
        {
            AssertExtensions.Throws<ArgumentException>("value", () => new GridLength(value));
            AssertExtensions.Throws<ArgumentException>("value", () => new GridLength(value, GridUnitType.Pixel));
        }

        [Theory]
        [InlineData(GridUnitType.Auto - 1)]
        [InlineData(GridUnitType.Star + 1)]
        public void Ctor_InvalidUnitType_ThrowsArgumentException(GridUnitType unitType)
        {
            AssertExtensions.Throws<ArgumentException>("type", () => new GridLength(1, unitType));
        }

        [Fact]
        public void Auto_Get_ReturnsExpected()
        {
            GridLength gridLength = GridLength.Auto;
            Assert.Equal(GridUnitType.Auto, gridLength.GridUnitType);
            Assert.False(gridLength.IsAbsolute);
            Assert.True(gridLength.IsAuto);
            Assert.False(gridLength.IsStar);
            Assert.Equal(1, gridLength.Value);
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { new GridLength(10, GridUnitType.Pixel), new GridLength(10, GridUnitType.Pixel), true };
            yield return new object[] { new GridLength(10, GridUnitType.Pixel), new GridLength(11, GridUnitType.Pixel), false };
            yield return new object[] { new GridLength(10, GridUnitType.Pixel), new GridLength(10, GridUnitType.Auto), false };
            yield return new object[] { new GridLength(10, GridUnitType.Pixel), new GridLength(10, GridUnitType.Star), false };
            yield return new object[] { new GridLength(10, GridUnitType.Auto), GridLength.Auto, true };

            yield return new object[] { new GridLength(10, GridUnitType.Pixel), new object(), false };
            yield return new object[] { new GridLength(10, GridUnitType.Pixel), null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Object_ReturnsExpected(GridLength gridLength, object other, bool expected)
        {
            Assert.Equal(expected, gridLength.Equals(other));
            if (other is GridLength otherGridLength)
            {
                Assert.Equal(expected, gridLength.Equals(otherGridLength));
                Assert.Equal(expected, gridLength == otherGridLength);
                Assert.Equal(!expected, gridLength != otherGridLength);
                Assert.Equal(expected, gridLength.GetHashCode().Equals(otherGridLength.GetHashCode()));
            }
        }

        public static IEnumerable<object[]> ToString_TestData()
        {
            yield return new object[] { GridLength.Auto, "Auto" };
            yield return new object[] { new GridLength(10, GridUnitType.Pixel), "10" };
            yield return new object[] { new GridLength(10, GridUnitType.Star), "10*" };
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public void ToString_Invoke_ReturnsExpected(GridLength gridLength, string expected)
        {
            Assert.Equal(expected, gridLength.ToString());
        }
    }
}