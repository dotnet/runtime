// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using Xunit;

namespace Windows.UI.Xaml.Tests
{
    public class ThicknessTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var thickness = new Thickness();
            Assert.Equal(0, thickness.Left);
            Assert.Equal(0, thickness.Top);
            Assert.Equal(0, thickness.Right);
            Assert.Equal(0, thickness.Bottom);
        }

        public static IEnumerable<object[]> Values_TestData()
        {
            yield return new object[] { -1 };
            yield return new object[] { 0 };
            yield return new object[] { 1 };
            yield return new object[] { double.MaxValue };
            yield return new object[] { double.NaN };
            yield return new object[] { double.PositiveInfinity };
            yield return new object[] { double.NegativeInfinity };
        }

        [Theory]
        [MemberData(nameof(Values_TestData))]
        public void Ctor_Default(double uniformLength)
        {
            var thickness = new Thickness(uniformLength);
            Assert.Equal(uniformLength, thickness.Left);
            Assert.Equal(uniformLength, thickness.Top);
            Assert.Equal(uniformLength, thickness.Right);
            Assert.Equal(uniformLength, thickness.Bottom);
        }

        [Theory]
        [InlineData(-1, -2, -3, -4)]
        [InlineData(0, 0, 0, 0)]
        [InlineData(1, 2, 3, 4)]
        [InlineData(double.NaN, double.NaN, double.NaN, double.NaN)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity)]
        public void Ctor_Left_Top_Right_Bottom(double left, double top, double right, double bottom)
        {
            var thickness = new Thickness(left, top, right, bottom);
            Assert.Equal(left, thickness.Left);
            Assert.Equal(top, thickness.Top);
            Assert.Equal(right, thickness.Right);
            Assert.Equal(bottom, thickness.Bottom);
        }

        [Theory]
        [MemberData(nameof(Values_TestData))]
        public void Left_Set_GetReturnsExpected(double value)
        {
            var thickness = new Thickness { Left = value };
            Assert.Equal(value, thickness.Left);
        }

        [Theory]
        [MemberData(nameof(Values_TestData))]
        public void Top_Set_GetReturnsExpected(double value)
        {
            var thickness = new Thickness { Top = value };
            Assert.Equal(value, thickness.Top);
        }

        [Theory]
        [MemberData(nameof(Values_TestData))]
        public void Right_Set_GetReturnsExpected(double value)
        {
            var thickness = new Thickness { Right = value };
            Assert.Equal(value, thickness.Right);
        }

        [Theory]
        [MemberData(nameof(Values_TestData))]
        public void Bottom_Set_GetReturnsExpected(double value)
        {
            var cornerRadius = new Thickness { Bottom = value };
            Assert.Equal(value, cornerRadius.Bottom);
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { new Thickness(1, 2, 3, 4), new Thickness(1, 2, 3, 4), true };
            yield return new object[] { new Thickness(1, 2, 3, 4), new Thickness(2, 2, 3, 4), false };
            yield return new object[] { new Thickness(1, 2, 3, 4), new Thickness(1, 3, 3, 4), false };
            yield return new object[] { new Thickness(1, 2, 3, 4), new Thickness(1, 2, 4, 4), false };
            yield return new object[] { new Thickness(1, 2, 3, 4), new Thickness(1, 2, 3, 5), false };

            yield return new object[] { new Thickness(1, 2, 3, 4), new object(), false };
            yield return new object[] { new Thickness(1, 2, 3, 4), null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Object_ReturnsExpected(Thickness thickness, object other, bool expected)
        {
            Assert.Equal(expected, thickness.Equals(other));
            if (other is Thickness otherThickness)
            {
                Assert.Equal(expected, thickness.Equals(otherThickness));
                Assert.Equal(expected, thickness == otherThickness);
                Assert.Equal(!expected, thickness != otherThickness);
                Assert.Equal(expected, thickness.GetHashCode().Equals(otherThickness.GetHashCode()));
            }
        }

        [Fact]
        public void ToString_Invoke_ReturnsExpected()
        {
            var thickness = new Thickness(1, 2.2, 3, 4);
            Assert.Equal("1,2.2,3,4", thickness.ToString());
        }

        [Fact]
        public void ToString_NaN_ReturnsAuto()
        {
            Thickness thickness = new FakeThickness
            {
                Left = double.NaN,
                Top = double.NaN,
                Right = double.NaN,
                Bottom = double.NaN
            }.ToActual();
            Assert.Equal("Auto,Auto,Auto,Auto", thickness.ToString());
        }

        public struct FakeThickness
        {
            public double Left;
            public double Top;
            public double Right;
            public double Bottom;

            public Thickness ToActual()
            {
                ThicknessWrapper wrapper = default(ThicknessWrapper);
                wrapper.Fake = this;
                return wrapper.Actual;
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct ThicknessWrapper
        {
            [FieldOffset(0)] public Thickness Actual;
            [FieldOffset(0)] public FakeThickness Fake;
        }
    }
}