// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace System.Tests
{
    public static partial class TimeSpanTests
    {
        [Fact]
        public static void MaxValue()
        {
            VerifyTimeSpan(TimeSpan.MaxValue, 10675199, 2, 48, 5, 477);
        }

        [Fact]
        public static void MinValue()
        {
            VerifyTimeSpan(TimeSpan.MinValue, -10675199, -2, -48, -5, -477);
        }

        [Fact]
        public static void Zero()
        {
            VerifyTimeSpan(TimeSpan.Zero, 0, 0, 0, 0, 0);
        }

        [Fact]
        public static void Ctor_Empty()
        {
            VerifyTimeSpan(new TimeSpan(), 0, 0, 0, 0, 0);
            VerifyTimeSpan(default(TimeSpan), 0, 0, 0, 0, 0);
        }

        [Fact]
        public static void Ctor_Long()
        {
            VerifyTimeSpan(new TimeSpan(999999999999999999), 1157407, 9, 46, 39, 999);
        }

        [Fact]
        public static void Ctor_Int_Int_Int()
        {
            var timeSpan = new TimeSpan(10, 9, 8);
            VerifyTimeSpan(timeSpan, 0, 10, 9, 8, 0);
        }

        [Fact]
        public static void Ctor_Int_Int_Int_Invalid()
        {
            Assert.Throws<ArgumentOutOfRangeException>(null, () => new TimeSpan((int)TimeSpan.MinValue.TotalHours - 1, 0, 0)); // TimeSpan < TimeSpan.MinValue
            Assert.Throws<ArgumentOutOfRangeException>(null, () => new TimeSpan((int)TimeSpan.MaxValue.TotalHours + 1, 0, 0)); // TimeSpan > TimeSpan.MaxValue
        }

        [Fact]
        public static void Ctor_Int_Int_Int_Int_Int()
        {
            var timeSpan = new TimeSpan(10, 9, 8, 7, 6);
            VerifyTimeSpan(timeSpan, 10, 9, 8, 7, 6);
        }

        [Fact]
        public static void Ctor_Int_Int_Int_Int_Int_Invalid()
        {
            // TimeSpan > TimeSpan.MinValue
            TimeSpan min = TimeSpan.MinValue;
            Assert.Throws<ArgumentOutOfRangeException>(null, () => new TimeSpan(min.Days - 1, min.Hours, min.Minutes, min.Seconds, min.Milliseconds));
            Assert.Throws<ArgumentOutOfRangeException>(null, () => new TimeSpan(min.Days, min.Hours - 1, min.Minutes, min.Seconds, min.Milliseconds));
            Assert.Throws<ArgumentOutOfRangeException>(null, () => new TimeSpan(min.Days, min.Hours, min.Minutes - 1, min.Seconds, min.Milliseconds));
            Assert.Throws<ArgumentOutOfRangeException>(null, () => new TimeSpan(min.Days, min.Hours, min.Minutes, min.Seconds - 1, min.Milliseconds));
            Assert.Throws<ArgumentOutOfRangeException>(null, () => new TimeSpan(min.Days, min.Hours, min.Minutes, min.Seconds, min.Milliseconds - 1));

            // TimeSpan > TimeSpan.MaxValue
            TimeSpan max = TimeSpan.MaxValue;
            Assert.Throws<ArgumentOutOfRangeException>(null, () => new TimeSpan(max.Days + 1, max.Hours, max.Minutes, max.Seconds, max.Milliseconds));
            Assert.Throws<ArgumentOutOfRangeException>(null, () => new TimeSpan(max.Days, max.Hours + 1, max.Minutes, max.Seconds, max.Milliseconds));
            Assert.Throws<ArgumentOutOfRangeException>(null, () => new TimeSpan(max.Days, max.Hours, max.Minutes + 1, max.Seconds, max.Milliseconds));
            Assert.Throws<ArgumentOutOfRangeException>(null, () => new TimeSpan(max.Days, max.Hours, max.Minutes, max.Seconds + 1, max.Milliseconds));
            Assert.Throws<ArgumentOutOfRangeException>(null, () => new TimeSpan(max.Days, max.Hours, max.Minutes, max.Seconds, max.Milliseconds + 1));
        }

        public static IEnumerable<object[]> Total_Days_Hours_Minutes_Seconds_Milliseconds_TestData()
        {
            yield return new object[] { new TimeSpan(0, 0, 0, 0, 0), 0.0, 0.0, 0.0, 0.0, 0.0 };
            yield return new object[] { new TimeSpan(0, 0, 0, 0, 500), 0.5 / 60.0 / 60.0 / 24.0, 0.5 / 60.0 / 60.0, 0.5 / 60.0, 0.5, 500.0 };
            yield return new object[] { new TimeSpan(0, 1, 0, 0, 0), 1 / 24.0, 1, 60, 3600, 3600000 };
            yield return new object[] { new TimeSpan(1, 0, 0, 0, 0), 1, 24, 1440, 86400, 86400000 };
            yield return new object[] { new TimeSpan(1, 1, 0, 0, 0), 25.0 / 24.0, 25, 1500, 90000, 90000000 };
        }

        [Theory]
        [MemberData(nameof(Total_Days_Hours_Minutes_Seconds_Milliseconds_TestData))]
        public static void Total_Days_Hours_Minutes_Seconds_Milliseconds(TimeSpan timeSpan, double expectedDays, double expectedHours, double expectedMinutes, double expectedSeconds, double expectedMilliseconds)
        {
            // Use ToString() to prevent any rounding errors when comparing
            Assert.Equal(expectedDays.ToString(), timeSpan.TotalDays.ToString());
            Assert.Equal(expectedHours, timeSpan.TotalHours);
            Assert.Equal(expectedMinutes, timeSpan.TotalMinutes);
            Assert.Equal(expectedSeconds, timeSpan.TotalSeconds);
            Assert.Equal(expectedMilliseconds, timeSpan.TotalMilliseconds);
        }

        [Fact]
        public static void TotalMilliseconds_Invalid()
        {
            long maxMilliseconds = long.MaxValue / 10000;
            long minMilliseconds = long.MinValue / 10000;
            Assert.Equal(maxMilliseconds, TimeSpan.MaxValue.TotalMilliseconds);
            Assert.Equal(minMilliseconds, TimeSpan.MinValue.TotalMilliseconds);
        }

        public static IEnumerable<object[]> Add_TestData()
        {
            yield return new object[] { new TimeSpan(0, 0, 0), new TimeSpan(1, 2, 3), new TimeSpan(1, 2, 3) };
            yield return new object[] { new TimeSpan(1, 2, 3), new TimeSpan(4, 5, 6), new TimeSpan(5, 7, 9) };
            yield return new object[] { new TimeSpan(1, 2, 3), new TimeSpan(-4, -5, -6), new TimeSpan(-3, -3, -3) };

            yield return new object[] { new TimeSpan(1, 2, 3, 4, 5), new TimeSpan(1, 2, 3), new TimeSpan(1, 3, 5, 7, 5) };
            yield return new object[] { new TimeSpan(1, 2, 3, 4, 5), new TimeSpan(10, 12, 13, 14, 15), new TimeSpan(11, 14, 16, 18, 20) };
            yield return new object[] { new TimeSpan(10000), new TimeSpan(200000), new TimeSpan(210000) };
        }

        [Theory]
        [MemberData(nameof(Add_TestData))]
        public static void Add(TimeSpan timeSpan1, TimeSpan timeSpan2, TimeSpan expected)
        {
            Assert.Equal(expected, timeSpan1.Add(timeSpan2));
            Assert.Equal(expected, timeSpan1 + timeSpan2);
        }

        [Fact]
        public static void Add_Invalid()
        {
            Assert.Throws<OverflowException>(() => TimeSpan.MaxValue.Add(new TimeSpan(1))); // Result > TimeSpan.MaxValue
            Assert.Throws<OverflowException>(() => TimeSpan.MinValue.Add(new TimeSpan(-1))); // Result < TimeSpan.MinValue

            Assert.Throws<OverflowException>(() => TimeSpan.MaxValue + new TimeSpan(1)); // Result > TimeSpan.MaxValue
            Assert.Throws<OverflowException>(() => TimeSpan.MinValue + new TimeSpan(-1)); // Result < TimeSpan.MinValue
        }

        public static IEnumerable<object[]> CompareTo_TestData()
        {
            yield return new object[] { new TimeSpan(10000), new TimeSpan(10000), 0 };
            yield return new object[] { new TimeSpan(20000), new TimeSpan(10000), 1 };
            yield return new object[] { new TimeSpan(10000), new TimeSpan(20000), -1 };

            yield return new object[] { new TimeSpan(1, 2, 3), new TimeSpan(1, 2, 3), 0 };
            yield return new object[] { new TimeSpan(1, 2, 3), new TimeSpan(1, 2, 4), -1 };
            yield return new object[] { new TimeSpan(1, 2, 3), new TimeSpan(1, 2, 2), 1 };
            yield return new object[] { new TimeSpan(1, 2, 3), new TimeSpan(1, 3, 3), -1 };
            yield return new object[] { new TimeSpan(1, 2, 3), new TimeSpan(1, 1, 3), 1 };
            yield return new object[] { new TimeSpan(1, 2, 3), new TimeSpan(2, 2, 3), -1 };
            yield return new object[] { new TimeSpan(1, 2, 3), new TimeSpan(0, 2, 3), 1 };

            yield return new object[] { new TimeSpan(1, 2, 3, 4), new TimeSpan(1, 2, 3, 4), 0 };
            yield return new object[] { new TimeSpan(1, 2, 3, 4), new TimeSpan(1, 2, 3, 5), -1 };
            yield return new object[] { new TimeSpan(1, 2, 3, 4), new TimeSpan(1, 2, 3, 3), 1 };
            yield return new object[] { new TimeSpan(1, 2, 3, 4), new TimeSpan(1, 2, 4, 4), -1 };
            yield return new object[] { new TimeSpan(1, 2, 3, 4), new TimeSpan(1, 2, 2, 4), 1 };
            yield return new object[] { new TimeSpan(1, 2, 3, 4), new TimeSpan(1, 3, 3, 4), -1 };
            yield return new object[] { new TimeSpan(1, 2, 3, 4), new TimeSpan(1, 1, 3, 4), 1 };
            yield return new object[] { new TimeSpan(1, 2, 3, 4), new TimeSpan(2, 2, 3, 4), -1 };
            yield return new object[] { new TimeSpan(1, 2, 3, 4), new TimeSpan(0, 2, 3, 4), 1 };

            yield return new object[] { new TimeSpan(1, 2, 3, 4, 5), new TimeSpan(1, 2, 3, 4, 5), 0 };
            yield return new object[] { new TimeSpan(1, 2, 3, 4, 5), new TimeSpan(1, 2, 3, 4, 6), -1 };
            yield return new object[] { new TimeSpan(1, 2, 3, 4, 5), new TimeSpan(1, 2, 3, 4, 4), 1 };
            yield return new object[] { new TimeSpan(1, 2, 3, 4, 5), new TimeSpan(1, 2, 3, 5, 5), -1 };
            yield return new object[] { new TimeSpan(1, 2, 3, 4, 5), new TimeSpan(1, 2, 3, 3, 5), 1 };
            yield return new object[] { new TimeSpan(1, 2, 3, 4, 5), new TimeSpan(1, 2, 4, 4, 5), -1 };
            yield return new object[] { new TimeSpan(1, 2, 3, 4, 5), new TimeSpan(1, 2, 2, 4, 5), 1 };
            yield return new object[] { new TimeSpan(1, 2, 3, 4, 5), new TimeSpan(1, 3, 3, 4, 5), -1 };
            yield return new object[] { new TimeSpan(1, 2, 3, 4, 5), new TimeSpan(1, 1, 3, 4, 5), 1 };
            yield return new object[] { new TimeSpan(1, 2, 3, 4, 5), new TimeSpan(2, 2, 3, 4, 5), -1 };
            yield return new object[] { new TimeSpan(1, 2, 3, 4, 5), new TimeSpan(0, 2, 3, 4, 5), 1 };

            yield return new object[] { new TimeSpan(10000), null, 1 };
        }

        [Theory]
        [MemberData(nameof(CompareTo_TestData))]
        public static void CompareTo(TimeSpan timeSpan1, object obj, int expected)
        {
            if (obj is TimeSpan)
            {
                TimeSpan timeSpan2 = (TimeSpan)obj;
                Assert.Equal(expected, Math.Sign(timeSpan1.CompareTo(timeSpan2)));
                Assert.Equal(expected, Math.Sign(TimeSpan.Compare(timeSpan1, timeSpan2)));

                if (expected >= 0)
                {
                    Assert.True(timeSpan1 >= timeSpan2);
                    Assert.False(timeSpan1 < timeSpan2);
                }
                if (expected > 0)
                {
                    Assert.True(timeSpan1 > timeSpan2);
                    Assert.False(timeSpan1 <= timeSpan2);
                }
                if (expected <= 0)
                {
                    Assert.True(timeSpan1 <= timeSpan2);
                    Assert.False(timeSpan1 > timeSpan2);
                }
                if (expected < 0)
                {
                    Assert.True(timeSpan1 < timeSpan2);
                    Assert.False(timeSpan1 >= timeSpan2);
                }
            }
            IComparable comparable = timeSpan1;
            Assert.Equal(expected, Math.Sign(comparable.CompareTo(obj)));
        }

        [Fact]
        public static void CompareTo_ObjectNotTimeSpan_ThrowsArgumentException()
        {
            IComparable comparable = new TimeSpan(10000);
            Assert.Throws<ArgumentException>(null, () => comparable.CompareTo("10000")); // Obj is not a time span
        }

        public static IEnumerable<object[]> Duration_TestData()
        {
            yield return new object[] { new TimeSpan(0, 0, 0), new TimeSpan(0, 0, 0) };
            yield return new object[] { new TimeSpan(1, 2, 3), new TimeSpan(1, 2, 3) };
            yield return new object[] { new TimeSpan(-1, -2, -3), new TimeSpan(1, 2, 3) };
            yield return new object[] { new TimeSpan(12345), new TimeSpan(12345) };
            yield return new object[] { new TimeSpan(-12345), new TimeSpan(12345) };
        }

        [Theory]
        [MemberData(nameof(Duration_TestData))]
        public static void Duration(TimeSpan timeSpan, TimeSpan expected)
        {
            Assert.Equal(expected, timeSpan.Duration());
        }

        [Fact]
        public static void Duration_Invalid()
        {
            Assert.Throws<OverflowException>(() => TimeSpan.MinValue.Duration()); // TimeSpan.Ticks == TimeSpan.MinValue.Ticks
            Assert.Throws<OverflowException>(() => new TimeSpan(TimeSpan.MinValue.Ticks).Duration()); // TimeSpan.Ticks == TimeSpan.MinValue.Ticks
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { new TimeSpan(0, 0, 0), new TimeSpan(0, 0, 0), true };

            yield return new object[] { new TimeSpan(1, 2, 3), new TimeSpan(1, 2, 3), true };
            yield return new object[] { new TimeSpan(1, 2, 3), new TimeSpan(1, 2, 4), false };
            yield return new object[] { new TimeSpan(1, 2, 3), new TimeSpan(1, 3, 3), false };
            yield return new object[] { new TimeSpan(1, 2, 3), new TimeSpan(2, 2, 3), false };
            yield return new object[] { new TimeSpan(1, 2, 3), new TimeSpan(0, 1, 2, 3), true };
            yield return new object[] { new TimeSpan(1, 2, 3), new TimeSpan(0, 1, 2, 3, 0), true };

            yield return new object[] { new TimeSpan(1, 2, 3, 4), new TimeSpan(1, 2, 3, 4), true };
            yield return new object[] { new TimeSpan(1, 2, 3, 4), new TimeSpan(1, 2, 3, 5), false };
            yield return new object[] { new TimeSpan(1, 2, 3, 4), new TimeSpan(1, 2, 4, 4), false };
            yield return new object[] { new TimeSpan(1, 2, 3, 4), new TimeSpan(1, 3, 3, 4), false };
            yield return new object[] { new TimeSpan(1, 2, 3, 4), new TimeSpan(2, 2, 3, 4), false };
            yield return new object[] { new TimeSpan(1, 2, 3, 4, 5), new TimeSpan(2, 3, 4), false };

            yield return new object[] { new TimeSpan(1, 2, 3, 4, 5), new TimeSpan(1, 2, 3, 4, 5), true };
            yield return new object[] { new TimeSpan(1, 2, 3, 4, 5), new TimeSpan(1, 2, 3, 4, 6), false };
            yield return new object[] { new TimeSpan(1, 2, 3, 4, 5), new TimeSpan(1, 2, 3, 5, 5), false };
            yield return new object[] { new TimeSpan(1, 2, 3, 4, 5), new TimeSpan(1, 2, 4, 4, 5), false };
            yield return new object[] { new TimeSpan(1, 2, 3, 4, 5), new TimeSpan(1, 3, 3, 4, 5), false };
            yield return new object[] { new TimeSpan(1, 2, 3, 4, 5), new TimeSpan(2, 2, 3, 4, 5), false };

            yield return new object[] { new TimeSpan(1, 2, 3, 4, 5), new TimeSpan(1, 2, 3, 4), false };
            yield return new object[] { new TimeSpan(1, 2, 3, 4, 5), new TimeSpan(2, 2, 3), false };

            yield return new object[] { new TimeSpan(10000), new TimeSpan(10000), true };
            yield return new object[] { new TimeSpan(10000), new TimeSpan(20000), false };

            yield return new object[] { new TimeSpan(10000), "20000", false };
            yield return new object[] { new TimeSpan(10000), null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public static void Equals(TimeSpan timeSpan1, object obj, bool expected)
        {
            if (obj is TimeSpan)
            {
                TimeSpan timeSpan2 = (TimeSpan)obj;
                Assert.Equal(expected, TimeSpan.Equals(timeSpan1, timeSpan2));
                Assert.Equal(expected, timeSpan1.Equals(timeSpan2));
                Assert.Equal(expected, timeSpan1 == timeSpan2);
                Assert.Equal(!expected, timeSpan1 != timeSpan2);

                Assert.Equal(expected, timeSpan1.GetHashCode().Equals(timeSpan2.GetHashCode()));
            }
            Assert.Equal(expected, timeSpan1.Equals(obj));
        }

        public static IEnumerable<object[]> FromDays_TestData()
        {
            yield return new object[] { 100.5, new TimeSpan(100, 12, 0, 0) };
            yield return new object[] { 2.5, new TimeSpan(2, 12, 0, 0) };
            yield return new object[] { 1.0, new TimeSpan(1, 0, 0, 0) };
            yield return new object[] { 0.0, new TimeSpan(0, 0, 0, 0) };
            yield return new object[] { -1.0, new TimeSpan(-1, 0, 0, 0) };
            yield return new object[] { -2.5, new TimeSpan(-2, -12, 0, 0) };
            yield return new object[] { -100.5, new TimeSpan(-100, -12, 0, 0) };
        }

        [Theory]
        [MemberData(nameof(FromDays_TestData))]
        public static void FromDays(double value, TimeSpan expected)
        {
            Assert.Equal(expected, TimeSpan.FromDays(value));
        }

        [Fact]
        public static void FromDays_Invalid()
        {
            double maxDays = long.MaxValue / (TimeSpan.TicksPerMillisecond / 1000.0 / 60.0 / 60.0 / 24.0);

            Assert.Throws<OverflowException>(() => TimeSpan.FromDays(double.PositiveInfinity)); // Value is positive infinity
            Assert.Throws<OverflowException>(() => TimeSpan.FromDays(double.NegativeInfinity)); // Value is positive infinity

            Assert.Throws<OverflowException>(() => TimeSpan.FromDays(maxDays)); // Value > TimeSpan.MaxValue
            Assert.Throws<OverflowException>(() => TimeSpan.FromDays(-maxDays)); // Value < TimeSpan.MinValue

            Assert.Throws<ArgumentException>(null, () => TimeSpan.FromMinutes(double.NaN)); // Value is NaN
        }

        public static IEnumerable<object[]> FromHours_TestData()
        {
            yield return new object[] { 100.5, new TimeSpan(4, 4, 30, 0) };
            yield return new object[] { 2.5, new TimeSpan(2, 30, 0) };
            yield return new object[] { 1.0, new TimeSpan(1, 0, 0) };
            yield return new object[] { 0.0, new TimeSpan(0, 0, 0) };
            yield return new object[] { -1.0, new TimeSpan(-1, 0, 0) };
            yield return new object[] { -2.5, new TimeSpan(-2, -30, 0) };
            yield return new object[] { -100.5, new TimeSpan(-4, -4, -30, 0) };
        }

        [Theory]
        [MemberData(nameof(FromHours_TestData))]
        public static void FromHours(double value, TimeSpan expected)
        {
            Assert.Equal(expected, TimeSpan.FromHours(value));
        }

        [Fact]
        public static void FromHours_Invalid()
        {
            double maxHours = long.MaxValue / (TimeSpan.TicksPerMillisecond / 1000.0 / 60.0 / 60.0);

            Assert.Throws<OverflowException>(() => TimeSpan.FromHours(double.PositiveInfinity)); // Value is positive infinity
            Assert.Throws<OverflowException>(() => TimeSpan.FromHours(double.NegativeInfinity)); // Value is positive infinity

            Assert.Throws<OverflowException>(() => TimeSpan.FromHours(maxHours)); // Value > TimeSpan.MaxValue
            Assert.Throws<OverflowException>(() => TimeSpan.FromHours(-maxHours)); // Value < TimeSpan.MinValue

            Assert.Throws<ArgumentException>(null, () => TimeSpan.FromMinutes(double.NaN)); // Value is NaN
        }

        public static IEnumerable<object[]> FromMinutes_TestData()
        {
            yield return new object[] { 100.5, new TimeSpan(1, 40, 30) };
            yield return new object[] { 2.5, new TimeSpan(0, 2, 30) };
            yield return new object[] { 1.0, new TimeSpan(0, 1, 0) };
            yield return new object[] { 0.0, new TimeSpan(0, 0, 0) };
            yield return new object[] { -1.0, new TimeSpan(0, -1, 0) };
            yield return new object[] { -2.5, new TimeSpan(0, -2, -30) };
            yield return new object[] { -100.5, new TimeSpan(-1, -40, -30) };
        }

        [Theory]
        [MemberData(nameof(FromMinutes_TestData))]
        public static void FromMinutes(double value, TimeSpan expected)
        {
            Assert.Equal(expected, TimeSpan.FromMinutes(value));
        }

        [Fact]
        public static void FromMinutes_Invalid()
        {
            double maxMinutes = long.MaxValue / (TimeSpan.TicksPerMillisecond / 1000.0 / 60.0);

            Assert.Throws<OverflowException>(() => TimeSpan.FromMinutes(double.PositiveInfinity)); // Value is positive infinity
            Assert.Throws<OverflowException>(() => TimeSpan.FromMinutes(double.NegativeInfinity)); // Value is positive infinity

            Assert.Throws<OverflowException>(() => TimeSpan.FromMinutes(maxMinutes)); // Value > TimeSpan.MaxValue
            Assert.Throws<OverflowException>(() => TimeSpan.FromMinutes(-maxMinutes)); // Value < TimeSpan.MinValue

            Assert.Throws<ArgumentException>(null, () => TimeSpan.FromMinutes(double.NaN)); // Value is NaN
        }

        public static IEnumerable<object[]> FromSeconds_TestData()
        {
            yield return new object[] { 100.5, new TimeSpan(0, 0, 1, 40, 500) };
            yield return new object[] { 2.5, new TimeSpan(0, 0, 0, 2, 500) };
            yield return new object[] { 1.0, new TimeSpan(0, 0, 0, 1, 0) };
            yield return new object[] { 0.0, new TimeSpan(0, 0, 0, 0, 0) };
            yield return new object[] { -1.0, new TimeSpan(0, 0, 0, -1, 0) };
            yield return new object[] { -2.5, new TimeSpan(0, 0, 0, -2, -500) };
            yield return new object[] { -100.5, new TimeSpan(0, 0, -1, -40, -500) };
        }

        [Theory]
        [MemberData(nameof(FromSeconds_TestData))]
        public static void FromSeconds(double value, TimeSpan expected)
        {
            Assert.Equal(expected, TimeSpan.FromSeconds(value));
        }

        [Fact]
        public static void FromSeconds_Invalid()
        {
            double maxSeconds = long.MaxValue / (TimeSpan.TicksPerMillisecond / 1000.0);

            Assert.Throws<OverflowException>(() => TimeSpan.FromSeconds(double.PositiveInfinity)); // Value is positive infinity
            Assert.Throws<OverflowException>(() => TimeSpan.FromSeconds(double.NegativeInfinity)); // Value is positive infinity

            Assert.Throws<OverflowException>(() => TimeSpan.FromSeconds(maxSeconds)); // Value > TimeSpan.MaxValue
            Assert.Throws<OverflowException>(() => TimeSpan.FromSeconds(-maxSeconds)); // Value < TimeSpan.MinValue

            Assert.Throws<ArgumentException>(null, () => TimeSpan.FromSeconds(double.NaN)); // Value is NaN
        }

        public static IEnumerable<object[]> FromMilliseconds_TestData()
        {
            yield return new object[] { 1500.5, new TimeSpan(0, 0, 0, 1, 501) };
            yield return new object[] { 2.5, new TimeSpan(0, 0, 0, 0, 3) };
            yield return new object[] { 1.0, new TimeSpan(0, 0, 0, 0, 1) };
            yield return new object[] { 0.0, new TimeSpan(0, 0, 0, 0, 0) };
            yield return new object[] { -1.0, new TimeSpan(0, 0, 0, 0, -1) };
            yield return new object[] { -2.5, new TimeSpan(0, 0, 0, 0, -3) };
            yield return new object[] { -1500.5, new TimeSpan(0, 0, 0, -1, -501) };
        }

        [Theory]
        [MemberData(nameof(FromMilliseconds_TestData))]
        public static void FromMilliseconds(double value, TimeSpan expected)
        {
            Assert.Equal(expected, TimeSpan.FromMilliseconds(value));
        }

        [Fact]
        public static void FromMilliseconds_Invalid()
        {
            double maxMilliseconds = long.MaxValue / TimeSpan.TicksPerMillisecond;

            Assert.Throws<OverflowException>(() => TimeSpan.FromMilliseconds(double.PositiveInfinity)); // Value is positive infinity
            Assert.Throws<OverflowException>(() => TimeSpan.FromMilliseconds(double.NegativeInfinity)); // Value is positive infinity

            Assert.Throws<OverflowException>(() => TimeSpan.FromMilliseconds(maxMilliseconds)); // Value > TimeSpan.MaxValue
            Assert.Throws<OverflowException>(() => TimeSpan.FromMilliseconds(-maxMilliseconds)); // Value < TimeSpan.MinValue

            Assert.Throws<ArgumentException>(null, () => TimeSpan.FromMilliseconds(double.NaN)); // Value is NaN
        }

        public static IEnumerable<object[]> FromTicks_TestData()
        {
            yield return new object[] { TimeSpan.TicksPerMillisecond, new TimeSpan(0, 0, 0, 0, 1) };
            yield return new object[] { TimeSpan.TicksPerSecond, new TimeSpan(0, 0, 0, 1, 0) };
            yield return new object[] { TimeSpan.TicksPerMinute, new TimeSpan(0, 0, 1, 0, 0) };
            yield return new object[] { TimeSpan.TicksPerHour, new TimeSpan(0, 1, 0, 0, 0) };
            yield return new object[] { TimeSpan.TicksPerDay, new TimeSpan(1, 0, 0, 0, 0) };
            yield return new object[] { 1.0, new TimeSpan(1) };
            yield return new object[] { 0.0, new TimeSpan(0, 0, 0) };
            yield return new object[] { -1.0, new TimeSpan(-1) };
            yield return new object[] { -TimeSpan.TicksPerMillisecond, new TimeSpan(0, 0, 0, 0, -1) };
            yield return new object[] { -TimeSpan.TicksPerSecond, new TimeSpan(0, 0, 0, -1, 0) };
            yield return new object[] { -TimeSpan.TicksPerMinute, new TimeSpan(0, 0, -1, 0, 0) };
            yield return new object[] { -TimeSpan.TicksPerHour, new TimeSpan(0, -1, 0, 0, 0) };
            yield return new object[] { -TimeSpan.TicksPerDay, new TimeSpan(-1, 0, 0, 0, 0) };
        }

        [Theory]
        [MemberData(nameof(FromTicks_TestData))]
        public static void FromTicks(long value, TimeSpan expected)
        {
            Assert.Equal(expected, TimeSpan.FromTicks(value));
        }

        public static IEnumerable<object[]> Negate_TestData()
        {
            yield return new object[] { new TimeSpan(0, 0, 0), new TimeSpan(0, 0, 0) };
            yield return new object[] { new TimeSpan(1, 2, 3), new TimeSpan(-1, -2, -3) };
            yield return new object[] { new TimeSpan(-1, -2, -3), new TimeSpan(1, 2, 3) };
            yield return new object[] { new TimeSpan(12345), new TimeSpan(-12345) };
            yield return new object[] { new TimeSpan(-12345), new TimeSpan(12345) };
        }

        [Theory]
        [MemberData(nameof(Negate_TestData))]
        public static void Negate(TimeSpan timeSpan, TimeSpan expected)
        {
            Assert.Equal(expected, timeSpan.Negate());
            Assert.Equal(expected, -timeSpan);
        }

        [Fact]
        public static void Negate_Invalid()
        {
            Assert.Throws<OverflowException>(() => TimeSpan.MinValue.Negate()); // TimeSpan.MinValue cannot be negated
            Assert.Throws<OverflowException>(() => -TimeSpan.MinValue); // TimeSpan.MinValue cannot be negated
        }

        public static IEnumerable<object[]> Parse_Valid_TestData()
        {
            yield return new object[] { "       12:24:02", null, new TimeSpan(0, 12, 24, 2, 0) };

            yield return new object[] { "12:24", null, new TimeSpan(0, 12, 24, 0, 0) };
            yield return new object[] { "12:24:02", null, new TimeSpan(0, 12, 24, 2, 0) };
            yield return new object[] { "1.12:24:02", null, new TimeSpan(1, 12, 24, 2, 0) };
            yield return new object[] { "1:12:24:02", null, new TimeSpan(1, 12, 24, 2, 0) };
            yield return new object[] { "1.12:24:02.999", null, new TimeSpan(1, 12, 24, 2, 999) };

            yield return new object[] { "-12:24:02", null, new TimeSpan(0, -12, -24, -2, 0) };
            yield return new object[] { "-1.12:24:02.999", null, new TimeSpan(-1, -12, -24, -2, -999) };

            // Croatia uses ',' in place of '.'
            CultureInfo croatianCulture = new CultureInfo("hr-HR");
            yield return new object[] { "6:12:14:45,348", croatianCulture, new TimeSpan(6, 12, 14, 45, 348) };
        }

        [Theory]
        [MemberData(nameof(Parse_Valid_TestData))]
        public static void Parse(string input, IFormatProvider provider, TimeSpan expected)
        {
            TimeSpan result;
            if (provider == null)
            {
                Assert.True(TimeSpan.TryParse(input, out result));
                Assert.Equal(expected, result);

                Assert.Equal(expected, TimeSpan.Parse(input));
            }
            Assert.True(TimeSpan.TryParse(input, provider, out result));
            Assert.Equal(expected, result);

            Assert.Equal(expected, TimeSpan.Parse(input, provider));
        }

        public static IEnumerable<object[]> Parse_Invalid_TestData()
        {
            yield return new object[] { null, null, typeof(ArgumentNullException) };

            yield return new object[] { "", null, typeof(FormatException) };
            yield return new object[] { "-", null, typeof(FormatException) };
            yield return new object[] { "garbage", null, typeof(FormatException) };
            yield return new object[] { "12/12/12", null, typeof(FormatException) };

            yield return new object[] { "1:1:1.99999999", null, typeof(OverflowException) };

            // Croatia uses ',' in place of '.'
            CultureInfo croatianCulture = new CultureInfo("hr-HR");
            yield return new object[] { "6:12:14:45.3448", croatianCulture, typeof(FormatException) };
        }

        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_Invalid(string input, IFormatProvider provider, Type exceptionType)
        {
            TimeSpan result;
            if (provider == null)
            {
                Assert.False(TimeSpan.TryParse(input, out result));
                Assert.Equal(TimeSpan.Zero, result);

                Assert.Throws(exceptionType, () => TimeSpan.Parse(input));
            }
            Assert.False(TimeSpan.TryParse(input, provider, out result));
            Assert.Equal(TimeSpan.Zero, result);

            Assert.Throws(exceptionType, () => TimeSpan.Parse(input, provider));
        }

        public static IEnumerable<object[]> ParseExact_Valid_TestData()
        {
            // Standard timespan formats 'c', 'g', 'G'
            yield return new object[] { "12:24:02", "c", new TimeSpan(0, 12, 24, 2) };
            yield return new object[] { "1.12:24:02", "c", new TimeSpan(1, 12, 24, 2) };
            yield return new object[] { "-01.07:45:16.999", "c", new TimeSpan(1, 7, 45, 16, 999).Negate() };
            yield return new object[] { "12:24:02", "g", new TimeSpan(0, 12, 24, 2) };
            yield return new object[] { "1:12:24:02", "g", new TimeSpan(1, 12, 24, 2) };
            yield return new object[] { "-01:07:45:16.999", "g", new TimeSpan(1, 7, 45, 16, 999).Negate() };
            yield return new object[] { "1:12:24:02.243", "G", new TimeSpan(1, 12, 24, 2, 243) };
            yield return new object[] { "-01:07:45:16.999", "G", new TimeSpan(1, 7, 45, 16, 999).Negate() };

            // Custom timespan formats
            yield return new object[] { "12.23:32:43", @"dd\.h\:m\:s", new TimeSpan(12, 23, 32, 43) };
            yield return new object[] { "012.23:32:43.893", @"ddd\.h\:m\:s\.fff", new TimeSpan(12, 23, 32, 43, 893) };
            yield return new object[] { "12.05:02:03", @"d\.hh\:mm\:ss", new TimeSpan(12, 5, 2, 3) };
            yield return new object[] { "12:34 minutes", @"mm\:ss\ \m\i\n\u\t\e\s", new TimeSpan(0, 12, 34) };
        }

        [Theory]
        [MemberData(nameof(ParseExact_Valid_TestData))]
        public static void ParseExact(string input, string format, TimeSpan expected)
        {
            TimeSpan result;
            Assert.Equal(expected, TimeSpan.ParseExact(input, format, new CultureInfo("en-US")));

            Assert.True(TimeSpan.TryParseExact(input, format, new CultureInfo("en-US"), out result));
            Assert.Equal(expected, result);

            // TimeSpanStyles is interpreted only for custom formats
            if (format != "c" && format != "g" && format != "G")
            {
                Assert.Equal(expected.Negate(), TimeSpan.ParseExact(input, format, new CultureInfo("en-US"), TimeSpanStyles.AssumeNegative));

                Assert.True(TimeSpan.TryParseExact(input, format, new CultureInfo("en-US"), TimeSpanStyles.AssumeNegative, out result));
                Assert.Equal(expected.Negate(), result);
            }
        }

        public static IEnumerable<object[]> ParseExact_Invalid_TestData()
        {
            yield return new object[] { null, "c", typeof(ArgumentNullException) };

            yield return new object[] { "", "c", typeof(FormatException) };
            yield return new object[] { "-", "c", typeof(FormatException) };
            yield return new object[] { "garbage", "c", typeof(FormatException) };

            // Standard timespan formats 'c', 'g', 'G'
            yield return new object[] { "24:24:02", "c", typeof(OverflowException) };
            yield return new object[] { "1:12:24:02", "c", typeof(FormatException) };
            yield return new object[] { "12:61:02", "g", typeof(OverflowException) };
            yield return new object[] { "1.12:24:02", "g", typeof(FormatException) };
            yield return new object[] { "1:07:45:16.99999999", "G", typeof(OverflowException) };
            yield return new object[] { "1:12:24:02", "G", typeof(FormatException) };

            // Custom timespan formats
            yield return new object[] { "12.35:32:43", @"dd\.h\:m\:s", typeof(OverflowException) };
            yield return new object[] { "12.5:2:3", @"d\.hh\:mm\:ss", typeof(FormatException) };
            yield return new object[] { "12.5:2", @"d\.hh\:mm\:ss", typeof(FormatException) };
        }

        [Theory]
        [MemberData(nameof(ParseExact_Invalid_TestData))]
        public static void ParseExactTest_Invalid(string input, string format, Type exceptionType)
        {
            Assert.Throws(exceptionType, () => TimeSpan.ParseExact(input, format, new CultureInfo("en-US")));

            TimeSpan result;
            Assert.False(TimeSpan.TryParseExact(input, format, new CultureInfo("en-US"), out result));
            Assert.Equal(TimeSpan.Zero, result);
        }

        public static IEnumerable<object[]> Subtract_TestData()
        {
            yield return new object[] { new TimeSpan(0, 0, 0), new TimeSpan(1, 2, 3), new TimeSpan(-1, -2, -3) };
            yield return new object[] { new TimeSpan(1, 2, 3), new TimeSpan(4, 5, 6), new TimeSpan(-3, -3, -3) };
            yield return new object[] { new TimeSpan(1, 2, 3), new TimeSpan(-4, -5, -6), new TimeSpan(5, 7, 9) };

            yield return new object[] { new TimeSpan(1, 2, 3, 4, 5), new TimeSpan(1, 2, 3), new TimeSpan(1, 1, 1, 1, 5) };
            yield return new object[] { new TimeSpan(10, 11, 12, 13, 14), new TimeSpan(1, 2, 3, 4, 5), new TimeSpan(9, 9, 9, 9, 9) };
            yield return new object[] { new TimeSpan(200000), new TimeSpan(10000), new TimeSpan(190000) };
        }

        [Theory]
        [MemberData(nameof(Subtract_TestData))]
        public static void Subtract(TimeSpan ts1, TimeSpan ts2, TimeSpan expected)
        {
            Assert.Equal(expected, ts1.Subtract(ts2));
            Assert.Equal(expected, ts1 - ts2);
        }

        [Fact]
        public static void Subtract_Invalid()
        {
            Assert.Throws<OverflowException>(() => TimeSpan.MaxValue.Subtract(new TimeSpan(-1))); // Result > TimeSpan.MaxValue
            Assert.Throws<OverflowException>(() => TimeSpan.MinValue.Subtract(new TimeSpan(1))); // Result < TimeSpan.MinValue

            Assert.Throws<OverflowException>(() => TimeSpan.MaxValue - new TimeSpan(-1)); // Result > TimeSpan.MaxValue
            Assert.Throws<OverflowException>(() => TimeSpan.MinValue - new TimeSpan(1)); // Result < TimeSpan.MinValue
        }

        [Fact]
        public static void ToStringTest()
        {
            var timeSpan1 = new TimeSpan(1, 2, 3);
            var timeSpan2 = new TimeSpan(1, 2, 3);
            var timeSpan3 = new TimeSpan(1, 2, 4);

            var timeSpan4 = new TimeSpan(1, 2, 3, 4);
            var timeSpan5 = new TimeSpan(1, 2, 3, 4);
            var timeSpan6 = new TimeSpan(1, 2, 3, 5);

            var timeSpan7 = new TimeSpan(1, 2, 3, 4, 5);
            var timeSpan8 = new TimeSpan(1, 2, 3, 4, 5);
            var timeSpan9 = new TimeSpan(1, 2, 3, 4, 6);

            Assert.Equal(timeSpan1.ToString(), timeSpan2.ToString());
            Assert.Equal(timeSpan1.ToString("c"), timeSpan2.ToString("c"));
            Assert.Equal(timeSpan1.ToString("c", null), timeSpan2.ToString("c", null));
            Assert.NotEqual(timeSpan1.ToString(), timeSpan3.ToString());
            Assert.NotEqual(timeSpan1.ToString(), timeSpan4.ToString());
            Assert.NotEqual(timeSpan1.ToString(), timeSpan7.ToString());

            Assert.Equal(timeSpan4.ToString(), timeSpan5.ToString());
            Assert.Equal(timeSpan4.ToString("c"), timeSpan5.ToString("c"));
            Assert.Equal(timeSpan4.ToString("c", null), timeSpan5.ToString("c", null));
            Assert.NotEqual(timeSpan4.ToString(), timeSpan6.ToString());
            Assert.NotEqual(timeSpan4.ToString(), timeSpan7.ToString());

            Assert.Equal(timeSpan7.ToString(), timeSpan8.ToString());
            Assert.Equal(timeSpan7.ToString("c"), timeSpan8.ToString("c"));
            Assert.Equal(timeSpan7.ToString("c", null), timeSpan8.ToString("c", null));
            Assert.NotEqual(timeSpan7.ToString(), timeSpan9.ToString());
        }

        [Fact]
        public static void ToString_InvalidFormat_ThrowsFormatException()
        {
            var timeSpan = new TimeSpan();
            Assert.Throws<FormatException>(() => timeSpan.ToString("y")); // Invalid format
            Assert.Throws<FormatException>(() => timeSpan.ToString("cc")); // Invalid format
        }

        private static void VerifyTimeSpan(TimeSpan timeSpan, int days, int hours, int minutes, int seconds, int milliseconds)
        {
            Assert.Equal(days, timeSpan.Days);
            Assert.Equal(hours, timeSpan.Hours);
            Assert.Equal(minutes, timeSpan.Minutes);
            Assert.Equal(seconds, timeSpan.Seconds);
            Assert.Equal(milliseconds, timeSpan.Milliseconds);

            Assert.Equal(timeSpan, +timeSpan);
        }
    }
}
