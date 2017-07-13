// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace Windows.UI.Xaml.Media.Animation.Tests
{
    public class RepeatBehaviorTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var repeatBehaviour = new RepeatBehavior();
            Assert.True(repeatBehaviour.HasCount);
            Assert.Equal(0, repeatBehaviour.Count);

            Assert.False(repeatBehaviour.HasDuration);
            Assert.Equal(TimeSpan.Zero, repeatBehaviour.Duration);

            Assert.Equal(RepeatBehaviorType.Count, repeatBehaviour.Type);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(10)]
        [InlineData(double.MaxValue)]
        public void Ctor_Count(double count)
        {
            var repeatBehaviour = new RepeatBehavior(count);
            Assert.True(repeatBehaviour.HasCount);
            Assert.Equal(count, repeatBehaviour.Count);

            Assert.False(repeatBehaviour.HasDuration);
            Assert.Equal(TimeSpan.Zero, repeatBehaviour.Duration);

            Assert.Equal(RepeatBehaviorType.Count, repeatBehaviour.Type);
            Assert.Equal(count.GetHashCode(), repeatBehaviour.GetHashCode());
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(double.NaN)]
        [InlineData(double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity)]
        public void Ctor_InvalidCount_ThrowsArgumentOutOfRangeException(double count)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => new RepeatBehavior(count));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public void Ctor_TimeSpan(int seconds)
        {
            var repeatBehaviour = new RepeatBehavior(TimeSpan.FromSeconds(seconds));
            Assert.False(repeatBehaviour.HasCount);
            Assert.Equal(0, repeatBehaviour.Count);

            Assert.True(repeatBehaviour.HasDuration);
            Assert.Equal(TimeSpan.FromSeconds(seconds), repeatBehaviour.Duration);

            Assert.Equal(RepeatBehaviorType.Duration, repeatBehaviour.Type);
            Assert.Equal(TimeSpan.FromSeconds(seconds).GetHashCode(), repeatBehaviour.GetHashCode());
        }

        [Fact]
        public void Ctor_NegativeTimeSpan_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("duration", () => new RepeatBehavior(TimeSpan.FromTicks(-1)));
        }

        [Fact]
        public void Forever_Get_ReturnsExpected()
        {
            RepeatBehavior forever = RepeatBehavior.Forever;
            Assert.False(forever.HasCount);
            Assert.Equal(0, forever.Count);

            Assert.False(forever.HasDuration);
            Assert.Equal(TimeSpan.Zero, forever.Duration);

            Assert.Equal(RepeatBehaviorType.Forever, forever.Type);

            Assert.Equal(int.MaxValue - 42, forever.GetHashCode());
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(10)]
        [InlineData(double.MaxValue)]
        [InlineData(double.NaN)]
        [InlineData(double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity)]
        public void Count_Set_GetReturnsExpected(double value)
        {
            var repeatBehaviour = new RepeatBehavior(TimeSpan.MaxValue) { Count = value };
            Assert.False(repeatBehaviour.HasCount);
            Assert.Equal(value, repeatBehaviour.Count);

            // Although we set a count, the type is unchanged.
            Assert.Equal(RepeatBehaviorType.Duration, repeatBehaviour.Type);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(-1)]
        public void Duration_Set_GetReturnsExpected(int seconds)
        {
            var repeatBehaviour = new RepeatBehavior(1) { Duration = TimeSpan.FromSeconds(seconds) };
            Assert.False(repeatBehaviour.HasDuration);
            Assert.Equal(TimeSpan.FromSeconds(seconds), repeatBehaviour.Duration);

            // Although we set a duration, the type is unchanged.
            Assert.Equal(RepeatBehaviorType.Count, repeatBehaviour.Type);
        }

        [Theory]
        [InlineData(RepeatBehaviorType.Count - 1)]
        [InlineData(RepeatBehaviorType.Count)]
        [InlineData(RepeatBehaviorType.Duration)]
        [InlineData(RepeatBehaviorType.Forever)]
        [InlineData(RepeatBehaviorType.Forever + 1)]
        public void Type_Set_GetReturnsExpected(RepeatBehaviorType type)
        {
            var repeatBehaviour = new RepeatBehavior(1) { Type = type };
            Assert.Equal(type == RepeatBehaviorType.Count, repeatBehaviour.HasCount);
            Assert.Equal(type == RepeatBehaviorType.Duration, repeatBehaviour.HasDuration);
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { new RepeatBehavior(2), new RepeatBehavior(2), true };
            yield return new object[] { new RepeatBehavior(2), new RepeatBehavior(1), false };
            yield return new object[] { new RepeatBehavior(2), RepeatBehavior.Forever, false };
            yield return new object[] { new RepeatBehavior(2), new RepeatBehavior(TimeSpan.FromSeconds(2)), false };

            yield return new object[] { new RepeatBehavior(TimeSpan.FromSeconds(2)), new RepeatBehavior(TimeSpan.FromSeconds(2)), true };
            yield return new object[] { new RepeatBehavior(TimeSpan.FromSeconds(2)), new RepeatBehavior(TimeSpan.FromSeconds(1)), false };
            yield return new object[] { new RepeatBehavior(TimeSpan.FromSeconds(2)), RepeatBehavior.Forever, false };
            yield return new object[] { new RepeatBehavior(TimeSpan.FromSeconds(2)), new RepeatBehavior(2), false };

            yield return new object[] { RepeatBehavior.Forever, RepeatBehavior.Forever, true };
            yield return new object[] { RepeatBehavior.Forever, new RepeatBehavior(TimeSpan.FromSeconds(2)), false };
            yield return new object[] { RepeatBehavior.Forever, new RepeatBehavior(2), false };

            yield return new object[] { new RepeatBehavior { Type = RepeatBehaviorType.Count - 1 }, new RepeatBehavior { Type = RepeatBehaviorType.Count - 1 }, false };
            yield return new object[] { new RepeatBehavior { Type = RepeatBehaviorType.Forever + 1 }, new RepeatBehavior { Type = RepeatBehaviorType.Count + 1 }, false };
            yield return new object[] { new RepeatBehavior(TimeSpan.FromSeconds(2)), new object(), false };
            yield return new object[] { new RepeatBehavior(TimeSpan.FromSeconds(2)), null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Object_ReturnsExpected(RepeatBehavior repeatBehaviour, object other, bool expected)
        {
            Assert.Equal(expected, repeatBehaviour.Equals(other));
            if (other is RepeatBehavior otherRepeatBehaviour)
            {
                Assert.Equal(expected, RepeatBehavior.Equals(repeatBehaviour, otherRepeatBehaviour));
                Assert.Equal(expected, repeatBehaviour.Equals(otherRepeatBehaviour));
                Assert.Equal(expected, repeatBehaviour == otherRepeatBehaviour);
                Assert.Equal(!expected, repeatBehaviour != otherRepeatBehaviour);

                if (repeatBehaviour.Type >= RepeatBehaviorType.Count && repeatBehaviour.Type <= RepeatBehaviorType.Forever)
                {
                    Assert.Equal(expected, repeatBehaviour.GetHashCode().Equals(otherRepeatBehaviour.GetHashCode()));
                }
                else if (repeatBehaviour.Type == otherRepeatBehaviour.Type)
                {
                    Assert.Equal(repeatBehaviour.GetHashCode(), otherRepeatBehaviour.GetHashCode());
                }
            }
        }

        public static IEnumerable<object[]> ToString_TestData()
        {
            yield return new object[] { RepeatBehavior.Forever, null, null, "Forever" };
            yield return new object[] { RepeatBehavior.Forever, "InvalidFormat", CultureInfo.CurrentCulture, "Forever" };

            yield return new object[] { new RepeatBehavior(TimeSpan.FromSeconds(2)), null, null, TimeSpan.FromSeconds(2).ToString() };
            yield return new object[] { new RepeatBehavior(TimeSpan.FromSeconds(2)), "InvalidFormat", CultureInfo.CurrentCulture, TimeSpan.FromSeconds(2).ToString() };

            var culture = new CultureInfo("en-US");
            culture.NumberFormat.NumberDecimalSeparator = "|";
            yield return new object[] { new RepeatBehavior(2.2), "abc", culture, "abcx" };
            yield return new object[] { new RepeatBehavior(2.2), "N4", culture, "2|2000x" };
            yield return new object[] { new RepeatBehavior(2.2), null, culture, "2|2x" };

            yield return new object[] { new RepeatBehavior(2.2), null, null, $"{2.2.ToString()}x" };
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public void ToString_Invoke_ReturnsExpected(RepeatBehavior keyTime, string format, IFormatProvider formatProvider, string expected)
        {
            if (format == null)
            {
                if (formatProvider == null)
                {
                    Assert.Equal(expected, keyTime.ToString());
                }

                Assert.Equal(expected, keyTime.ToString(formatProvider));
            }

            Assert.Equal(expected, ((IFormattable)keyTime).ToString(format, formatProvider));
        }
    }
}