// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Xunit;

namespace Windows.UI.Xaml.Tests
{
    public class DurationTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var duration = new Duration();
            Assert.False(duration.HasTimeSpan);
            Assert.Throws<InvalidOperationException>(() => duration.TimeSpan);
        }

        [Fact]
        public void Ctor_TimeSpan()
        {
            var duration = new Duration(TimeSpan.FromSeconds(2));
            Assert.True(duration.HasTimeSpan);
            Assert.Equal(TimeSpan.FromSeconds(2), duration.TimeSpan);
        }

        [Fact]
        public void Operator_Duration_ReturnsExpected()
        {
            Duration duration = TimeSpan.FromSeconds(2);
            Assert.True(duration.HasTimeSpan);
            Assert.Equal(TimeSpan.FromSeconds(2), duration.TimeSpan);
        }

        [Fact]
        public void Operator_UnaryPlus_ReturnsExpected()
        {
            Duration duration = +new Duration(TimeSpan.FromSeconds(2));
            Assert.True(duration.HasTimeSpan);
            Assert.Equal(TimeSpan.FromSeconds(2), duration.TimeSpan);
        }

        [Fact]
        public void Automatic_Get_ReturnsExpected()
        {
            Duration duration = Duration.Automatic;
            Assert.False(duration.HasTimeSpan);
            Assert.Throws<InvalidOperationException>(() => duration.TimeSpan);
        }

        [Fact]
        public void Forever_Get_ReturnsExpected()
        {
            Duration duration = Duration.Forever;
            Assert.False(duration.HasTimeSpan);
            Assert.Throws<InvalidOperationException>(() => duration.TimeSpan);
        }

        public static IEnumerable<object[]> Add_TestData()
        {
            yield return new object[] { Duration.Automatic, Duration.Automatic, Duration.Automatic };
            yield return new object[] { Duration.Automatic, Duration.Forever, Duration.Automatic };
            yield return new object[] { Duration.Forever, Duration.Forever, Duration.Forever };

            yield return new object[] { Duration.Automatic, new Duration(TimeSpan.FromSeconds(2)), Duration.Automatic };
            yield return new object[] { Duration.Forever, new Duration(TimeSpan.FromSeconds(2)), Duration.Forever };

            yield return new object[] { new Duration(TimeSpan.FromSeconds(1)), new Duration(TimeSpan.FromSeconds(2)), new Duration(TimeSpan.FromSeconds(3)) };
        }

        [Theory]
        [MemberData(nameof(Add_TestData))]
        public void Add_Durations_ReturnsExpected(Duration duration1, Duration duration2, Duration expected)
        {
            Assert.Equal(expected, duration1.Add(duration2));
            Assert.Equal(expected, duration1 + duration2);

            Assert.Equal(expected, duration2.Add(duration1));
            Assert.Equal(expected, duration2 + duration1);
        }

        public static IEnumerable<object[]> Subtract_TestData()
        {
            yield return new object[] { Duration.Automatic, Duration.Automatic, Duration.Automatic };
            yield return new object[] { Duration.Automatic, Duration.Forever, Duration.Automatic };
            yield return new object[] { Duration.Forever, Duration.Automatic, Duration.Automatic };
            yield return new object[] { Duration.Forever, Duration.Forever, Duration.Automatic };

            yield return new object[] { Duration.Automatic, new Duration(TimeSpan.FromSeconds(2)), Duration.Automatic };
            yield return new object[] { Duration.Forever, new Duration(TimeSpan.FromSeconds(2)), Duration.Forever };

            yield return new object[] { new Duration(TimeSpan.FromSeconds(3)), new Duration(TimeSpan.FromSeconds(2)), new Duration(TimeSpan.FromSeconds(1)) };
        }

        [Theory]
        [MemberData(nameof(Subtract_TestData))]
        public void Subtract_Durations_ReturnsExpected(Duration duration1, Duration duration2, Duration expected)
        {
            Assert.Equal(expected, duration1.Subtract(duration2));
            Assert.Equal(expected, duration1 - duration2);
        }

        public static IEnumerable<object[]> Compare_TestData()
        {
            yield return new object[] { new Duration(TimeSpan.FromSeconds(1)), new Duration(TimeSpan.FromSeconds(2)), -1 };
            yield return new object[] { new Duration(TimeSpan.FromSeconds(2)), new Duration(TimeSpan.FromSeconds(1)), 1 };
            yield return new object[] { new Duration(TimeSpan.FromSeconds(1)), new Duration(TimeSpan.FromSeconds(1)), 0 };
            yield return new object[] { new Duration(TimeSpan.FromSeconds(1)), Duration.Automatic, 1 };
            yield return new object[] { new Duration(TimeSpan.FromSeconds(2)), Duration.Forever, -1 };

            yield return new object[] { Duration.Forever, Duration.Forever, 0 };
            yield return new object[] { Duration.Forever, new Duration(TimeSpan.FromSeconds(2)), 1 };
            yield return new object[] { Duration.Forever, Duration.Automatic, 1 };

            yield return new object[] { Duration.Automatic, Duration.Automatic, 0 };
            yield return new object[] { Duration.Automatic, new Duration(TimeSpan.FromSeconds(2)), -1 };
            yield return new object[] { Duration.Automatic, Duration.Forever, -1 };
        }

        [Theory]
        [MemberData(nameof(Compare_TestData))]
        public void Compare_TestData(Duration duration1, Duration duration2, int expected)
        {
            bool bothOrNoneAutomatic = (duration1 == Duration.Automatic) == (duration2 == Duration.Automatic);

            Assert.Equal(expected, Duration.Compare(duration1, duration2));

            Assert.Equal(expected <= 0 && bothOrNoneAutomatic, duration1 <= duration2);
            Assert.Equal(expected < 0 && bothOrNoneAutomatic, duration1 < duration2);

            Assert.Equal(expected == 0 && bothOrNoneAutomatic, duration1 == duration2);
            Assert.Equal(expected != 0, duration1 != duration2);

            Assert.Equal(expected >= 0 && bothOrNoneAutomatic, duration1 >= duration2);
            Assert.Equal(expected > 0 && bothOrNoneAutomatic, duration1 > duration2);
        }
        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { new Duration(), new object(), -1 };
            yield return new object[] { new Duration(), null, -1 };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        [MemberData(nameof(Compare_TestData))]
        public void Equals_Object_ReturnsExpected(Duration duration, object other, int expected)
        {
            Assert.Equal(expected == 0, duration.Equals(other));
            if (other is Duration otherDuration)
            {
                Assert.Equal(expected == 0, Duration.Equals(duration, otherDuration));
                Assert.Equal(expected == 0, duration.Equals(otherDuration));
                Assert.Equal(expected == 0, duration.GetHashCode().Equals(otherDuration.GetHashCode()));
            }
        }

        public static IEnumerable<object[]> ToString_TestData()
        {
            yield return new object[] { Duration.Automatic, "Automatic" };
            yield return new object[] { Duration.Forever, "Forever" };
            yield return new object[] { new Duration(TimeSpan.FromSeconds(2)), TimeSpan.FromSeconds(2).ToString() };
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public void ToString_Invoke_ReturnsExpected(Duration duration, string expected)
        {
            Assert.Equal(expected, duration.ToString());
        }
    }
}