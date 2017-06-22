// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Xunit;

namespace Windows.UI.Xaml.Media.Animation.Tests
{
    public class KeyTimeTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var keyTime = new KeyTime();
            Assert.Equal(TimeSpan.Zero, keyTime.TimeSpan);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public void FromTimeSpan_ValidTimeSpan_ReturnsExpected(int seconds)
        {
            KeyTime keyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(seconds));
            Assert.Equal(TimeSpan.FromSeconds(seconds), keyTime.TimeSpan);
        }

        [Fact]
        public void FromTimeSpan_NegativeTimeSpan_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("timeSpan", () => KeyTime.FromTimeSpan(TimeSpan.FromTicks(-1)));
        }

        [Fact]
        public void Operator_ValidTimeSpan_ReturnsExpected()
        {
            KeyTime keyTime = TimeSpan.FromSeconds(2);
            Assert.Equal(TimeSpan.FromSeconds(2), keyTime);
        }

        [Fact]
        public void Operator_NegativeTimeSpan_ThrowsArgumentOutOfRangeException()
        {
            TimeSpan timeSpan = TimeSpan.FromTicks(-1);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("timeSpan", () => (KeyTime)timeSpan);
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { KeyTime.FromTimeSpan(TimeSpan.FromSeconds(2)), KeyTime.FromTimeSpan(TimeSpan.FromSeconds(2)), true };
            yield return new object[] { KeyTime.FromTimeSpan(TimeSpan.FromSeconds(2)), KeyTime.FromTimeSpan(TimeSpan.FromSeconds(1)), false };

            yield return new object[] { KeyTime.FromTimeSpan(TimeSpan.FromSeconds(2)), new object(), false };
            yield return new object[] { KeyTime.FromTimeSpan(TimeSpan.FromSeconds(2)), null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Object_ReturnsExpected(KeyTime keyTime, object other, bool expected)
        {
            Assert.Equal(expected, keyTime.Equals(other));
            if (other is KeyTime otherKeyTime)
            {
                Assert.Equal(expected, keyTime.Equals(otherKeyTime));
                Assert.Equal(expected, keyTime == otherKeyTime);
                Assert.Equal(!expected, keyTime != otherKeyTime);
                Assert.Equal(expected, keyTime.GetHashCode().Equals(otherKeyTime.GetHashCode()));
            }
        }

        public static IEnumerable<object[]> ToString_TestData()
        {
            yield return new object[] { new KeyTime(), TimeSpan.Zero.ToString() };
            yield return new object[] { KeyTime.FromTimeSpan(TimeSpan.FromSeconds(2)), TimeSpan.FromSeconds(2).ToString() };
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public void ToString_Invoke_ReturnsExpected(KeyTime keyTime, string expected)
        {
            Assert.Equal(expected, keyTime.ToString());
        }
    }
}