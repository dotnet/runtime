// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Xunit;

namespace System.Tests
{
    public class TimeSpanTests
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
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new TimeSpan((int)TimeSpan.MinValue.TotalHours - 1, 0, 0)); // TimeSpan < TimeSpan.MinValue
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new TimeSpan((int)TimeSpan.MaxValue.TotalHours + 1, 0, 0)); // TimeSpan > TimeSpan.MaxValue
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
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new TimeSpan(min.Days - 1, min.Hours, min.Minutes, min.Seconds, min.Milliseconds));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new TimeSpan(min.Days, min.Hours - 1, min.Minutes, min.Seconds, min.Milliseconds));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new TimeSpan(min.Days, min.Hours, min.Minutes - 1, min.Seconds, min.Milliseconds));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new TimeSpan(min.Days, min.Hours, min.Minutes, min.Seconds - 1, min.Milliseconds));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new TimeSpan(min.Days, min.Hours, min.Minutes, min.Seconds, min.Milliseconds - 1));

            // TimeSpan > TimeSpan.MaxValue
            TimeSpan max = TimeSpan.MaxValue;
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new TimeSpan(max.Days + 1, max.Hours, max.Minutes, max.Seconds, max.Milliseconds));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new TimeSpan(max.Days, max.Hours + 1, max.Minutes, max.Seconds, max.Milliseconds));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new TimeSpan(max.Days, max.Hours, max.Minutes + 1, max.Seconds, max.Milliseconds));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new TimeSpan(max.Days, max.Hours, max.Minutes, max.Seconds + 1, max.Milliseconds));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new TimeSpan(max.Days, max.Hours, max.Minutes, max.Seconds, max.Milliseconds + 1));
        }

        [Fact]
        public static void Ctor_Int_Int_Int_Int_Int_Int()
        {
            var timeSpan = new TimeSpan(10, 9, 8, 7, 6, 5);
            VerifyTimeSpan(timeSpan, 10, 9, 8, 7, 6, 5);
        }

        [Fact]
        public static void Ctor_Int_Int_Int_Int_Int_Int_Invalid()
        {
            // TimeSpan > TimeSpan.MinValue
            TimeSpan min = TimeSpan.MinValue;
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new TimeSpan(min.Days - 1, min.Hours, min.Minutes, min.Seconds, min.Milliseconds, min.Microseconds));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new TimeSpan(min.Days, min.Hours - 1, min.Minutes, min.Seconds, min.Milliseconds, min.Microseconds));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new TimeSpan(min.Days, min.Hours, min.Minutes - 1, min.Seconds, min.Milliseconds, min.Microseconds));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new TimeSpan(min.Days, min.Hours, min.Minutes, min.Seconds - 1, min.Milliseconds, min.Microseconds));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new TimeSpan(min.Days, min.Hours, min.Minutes, min.Seconds, min.Milliseconds, min.Microseconds - 1));

            // TimeSpan > TimeSpan.MaxValue
            TimeSpan max = TimeSpan.MaxValue;
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new TimeSpan(max.Days + 1, max.Hours, max.Minutes, max.Seconds, max.Milliseconds, max.Microseconds));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new TimeSpan(max.Days, max.Hours + 1, max.Minutes, max.Seconds, max.Milliseconds, max.Microseconds));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new TimeSpan(max.Days, max.Hours, max.Minutes + 1, max.Seconds, max.Milliseconds, max.Microseconds));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new TimeSpan(max.Days, max.Hours, max.Minutes, max.Seconds + 1, max.Milliseconds, max.Microseconds));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new TimeSpan(max.Days, max.Hours, max.Minutes, max.Seconds, max.Milliseconds, max.Microseconds + 1));
        }

        [Theory]
        [InlineData(100)]
        [InlineData(300)]
        [InlineData(900)]
        public static void Ctor_Int_Int_Int_Int_Int_Int_WithNanosecond(int nanoseconds)
        {
            var timeSpan = new TimeSpan(10, 9, 8, 7, 6, 5);
            timeSpan = new TimeSpan(timeSpan.Ticks + nanoseconds / 100);

            VerifyTimeSpan(timeSpan, 10, 9, 8, 7, 6, 5, nanoseconds);
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
            Assert.Equal(expectedDays.ToString("G15"), timeSpan.TotalDays.ToString("G15"));
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

        [Fact]
        public static void FromMinMaxValue_DoesNotThrow()
        {
            TimeSpan maxTimeSpan = TimeSpan.FromDays(TimeSpan.MaxValue.TotalDays);
            TimeSpan minTimeSpan = TimeSpan.FromDays(TimeSpan.MinValue.TotalDays);

            Assert.Equal(TimeSpan.MaxValue, maxTimeSpan);
            Assert.Equal(TimeSpan.MinValue, minTimeSpan);
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
            AssertExtensions.Throws<ArgumentException>(null, () => comparable.CompareTo("10000")); // Obj is not a time span
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
        public static void EqualsTest(TimeSpan timeSpan1, object obj, bool expected)
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

#region FromX_int_overloads
        [Fact]
        public static void FromDays_Int_Overload1()
        {
            Assert.Equal(new TimeSpan(1, 0, 0, 0), TimeSpan.FromDays(1));
        }
        [Fact]
        public static void FromDays_Int_Overload2()
        {
            var expectedaaa = new TimeSpan(1, 2, 3, 4, 5, 6);
            var actual = TimeSpan.FromDays(1, 2, 3, 4, 5, 6);
            Assert.Equal(expectedaaa, actual);
        }
        [Fact]
        public static void FromHours_Int_Overload1()
        {
            Assert.Equal(new TimeSpan(1, 0, 0), TimeSpan.FromHours(1));
        }
        [Fact]
        public static void FromHours_Int_Overload2()
        {
            var expecteda = new TimeSpan(0, 1, 2, 3, 4, 5);
            var actual = TimeSpan.FromHours(1, 2, 3, 4, 5);
            Assert.Equal(expecteda, actual);
        }
        [Fact]
        public static void FromMinutes_Int_Overload1()
        {
            Assert.Equal(new TimeSpan(0, 1, 0), TimeSpan.FromMinutes(1));
        }
        [Fact]
        public static void FromMinutes_Int_Overload2()
        {
            var expected = new TimeSpan(0, 0, 1, 2, 3, 4);
            var actual = TimeSpan.FromMinutes(1, 2, 3, 4);
            Assert.Equal(expected, actual);
        }
        [Fact]
        public static void FromSeconds_Int_Overload1()
        {
            Assert.Equal(new TimeSpan(0, 0, 1), TimeSpan.FromSeconds(1));
        }
        [Fact]
        public static void FromSeconds_Int_Overload2()
        {
            var expected = new TimeSpan(0, 0, 0, 1, 2, 3);
            var actualaaa = TimeSpan.FromSeconds(1, 2, 3);
            Assert.Equal(expected, actualaaa);
        }
        [Fact]
        public static void FromMilliseconds_Int_Overload()
        {
            var expected = new TimeSpan(0, 0, 0, 0, 1, 2);
            var actuala = TimeSpan.FromMilliseconds(1, 2);
            Assert.Equal(expected, actuala);
        }
        [Fact]
        public static void FromMicroseconds_Int_Overload()
        {
            Assert.Equal(new TimeSpan(0, 0, 0, 0, 0, 1), TimeSpan.FromMicroseconds(1));
        }

        [Fact]
        public static void FromSeconds_Int_ShouldGiveResultWithPrecision()
        {
            // Given example of problem with double in: https://github.com/dotnet/runtime/issues/93890#issue-1957706751
            Assert.Equal(new TimeSpan(0, 0, 0, 101, 832), TimeSpan.FromSeconds(101, 832));
        }

        [Fact]
        public static void FromDays_Int_ShouldConstructMaxValueAproximation()
        {
            var expected = TimeSpan.MaxValue;
            var actual = TimeSpan.FromDays(expected.Days, expected.Hours, expected.Minutes, expected.Seconds, expected.Milliseconds, expected.Microseconds);
            // Should be within TicksPerMicrosecond (10) ticks of expected
            var diffTicks = (expected - actual).Ticks;
            Assert.True(Math.Abs(diffTicks) < 10, $"Diff ticks was {diffTicks}");
        }
        [Fact]
        public static void FromDays_Int_ShouldConstructMinValueAproximation()
        {
            var expected = TimeSpan.MinValue;
            var actual = TimeSpan.FromDays(expected.Days, expected.Hours, expected.Minutes, expected.Seconds, expected.Milliseconds, expected.Microseconds);
            // Should be within TicksPerMicrosecond (10) ticks of expected
            var diffTicks = (actual - expected).Ticks;
            Assert.True(Math.Abs(diffTicks) < 10, $"Diff ticks was {diffTicks}");
        }

        // Consts copied from internal const in TimeSpan
        // Max and Min are symmetrical
        private const long maxMicroseconds = 922_337_203_685_477_580;
        private const long maxMilliseconds = 922_337_203_685_477;
        private const long maxSeconds = 922_337_203_685;
        private const long maxMinutes = 15_372_286_728;
        private const int maxHours = 256_204_778;
        private const int maxDays = 10_675_199;
        public static IEnumerable<object[]> FromDays_Int_ShouldOverflowOrUnderflow_Data()
        {
            long[] individualMaxValues = [ maxDays, maxHours, maxMinutes, maxSeconds, maxMilliseconds, maxMicroseconds ];
            // Each possibility for individual property to overflow or underflow
            for (var i = 0; i < individualMaxValues.Length; i++)
            {
                var iVal = individualMaxValues[i] + 1;
               object[] resultPos = [ 0, 0, 0, 0, 0, 0 ];
               resultPos[i] = iVal;
               yield return resultPos;
               object[] resultNeg = [ 0, 0, 0, 0, 0, 0 ];
               resultNeg[i] = -iVal;
               yield return resultNeg;
            }
            // Each possibility for 2 properties to overflow or underflow
            // while neither of them individually overflow or underflow
            for (var i = 0; i < individualMaxValues.Length; i++)
            {
                for (var j = i + 1; j < individualMaxValues.Length; j++)
                {
                    var iVal = individualMaxValues[i];
                    var jVal = individualMaxValues[j];
                    object[] resultPos = [ 0, 0, 0, 0, 0, 0 ];
                    resultPos[i] = iVal;
                    resultPos[j] = jVal;
                    yield return resultPos;
                    object[] resultNeg = [ 0, 0, 0, 0, 0, 0 ];
                    resultNeg[i] = -iVal;
                    resultNeg[j] = -jVal;
                    yield return resultNeg;
                }
            }
        }
        [Theory]
        [MemberData(nameof(FromDays_Int_ShouldOverflowOrUnderflow_Data))]
        public static void FromDays_Int_ShouldOverflowOrUnderflow(int days, int hours, long minutes, long seconds, long milliseconds, long microseconds)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => TimeSpan.FromDays(days, hours, minutes, seconds, milliseconds, microseconds));
        }

        public static IEnumerable<object[]> FromDays_Int_ShouldNotOverflow_WhenOverflowingParamIsCounteredByOppositeSignParam_Data()
        {
            long[] individualMaxValues = [ maxDays, maxHours, maxMinutes, maxSeconds, maxMilliseconds, maxMicroseconds ];
            for (var i = 0; i < individualMaxValues.Length; i++)
            {
                for (var j = 0; j < individualMaxValues.Length; j++)
                {
                    if (i == j)
                    {
                        continue;
                    }
                    var iVal = individualMaxValues[i] + 1;
                    var jVal = individualMaxValues[j] + 1;
                    object[] result = [ 0, 0, 0, 0, 0, 0 ];
                    result[i] = iVal;
                    result[j] = -jVal;
                    yield return result;
                }
            }
        }
        [Theory]
        [MemberData(nameof(FromDays_Int_ShouldNotOverflow_WhenOverflowingParamIsCounteredByOppositeSignParam_Data))]
        public static void FromDays_Int_ShouldNotOverflow_WhenOverflowingParamIsCounteredByOppositeSignParam(int days, int hours, long minutes, long seconds, long milliseconds, long microseconds)
        {
            var actual = TimeSpan.FromDays(days, hours, minutes, seconds, milliseconds, microseconds);
            // 2 individually overflowing or underflowing params with opposite sign should end up close to TimeSpan.FromDays(0)
            // This is an implementation detail of the chosen test data, but a nice sanity check of expected result
            Assert.True(actual > TimeSpan.FromDays(-1));
            Assert.True(actual < TimeSpan.FromDays(1));
        }
        [Fact]
        public static void FromDays_Int_ShouldOverflow_WhenAllParamsIsMaxValidValue()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => TimeSpan.FromDays(maxDays, maxHours, maxMinutes, maxSeconds, maxMilliseconds, maxMicroseconds));
        }
        [Fact]
        public static void FromDays_Int_ShouldUnderflow_WhenAllParamsIsMinValidValue()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => TimeSpan.FromDays(-maxDays, -maxHours, -maxMinutes, -maxSeconds, -maxMilliseconds, -maxMicroseconds));
        }
        [Fact]
        public static void FromDays_Int_ShouldOverflow_WhenIntermediateCalculationCouldOverflowBackIntoValidRange()
        {
            // Given example of problematic day count in comment in abandoned pr https://github.com/dotnet/runtime/pull/95779/files#r1439772903
            Assert.Throws<ArgumentOutOfRangeException>(() => TimeSpan.FromDays(1067519900));
        }
        [Fact]
        public static void FromDays_Int_ShouldOverflow_WhenParamIsTypeMaxValue_All()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => TimeSpan.FromDays(int.MaxValue, int.MaxValue, long.MaxValue, long.MaxValue, long.MaxValue, long.MaxValue));
        }
        [Fact]
        public static void FromDays_Int_ShouldOverflow_WhenParamIsTypeMaxValue_Days()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => TimeSpan.FromDays(int.MaxValue, 0, 0, 0, 0, 0));
        }
        [Fact]
        public static void FromDays_Int_ShouldOverflow_WhenParamIsTypeMaxValue_Hours()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => TimeSpan.FromDays(0, int.MaxValue, 0, 0, 0, 0));
        }
        [Fact]
        public static void FromDays_Int_ShouldOverflow_WhenParamIsTypeMaxValue_Minutes()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => TimeSpan.FromDays(0, 0, long.MaxValue, 0, 0, 0));
        }
        [Fact]
        public static void FromDays_Int_ShouldOverflow_WhenParamIsTypeMaxValue_Seconds()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => TimeSpan.FromDays(0, 0, 0, long.MaxValue, 0, 0));
        }
        [Fact]
        public static void FromDays_Int_ShouldOverflow_WhenParamIsTypeMaxValue_Milliseconds()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => TimeSpan.FromDays(0, 0, 0, 0, long.MaxValue, 0));
        }
        [Fact]
        public static void FromDays_Int_ShouldOverflow_WhenParamIsTypeMaxValue_Microseconds()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => TimeSpan.FromDays(0, 0, 0, 0, 0, long.MaxValue));
        }
#endregion

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

            AssertExtensions.Throws<ArgumentException>(null, () => TimeSpan.FromMinutes(double.NaN)); // Value is NaN
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

            AssertExtensions.Throws<ArgumentException>(null, () => TimeSpan.FromMinutes(double.NaN)); // Value is NaN
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

            AssertExtensions.Throws<ArgumentException>(null, () => TimeSpan.FromMinutes(double.NaN)); // Value is NaN
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

            AssertExtensions.Throws<ArgumentException>(null, () => TimeSpan.FromSeconds(double.NaN)); // Value is NaN
        }

        public static IEnumerable<object[]> FromMilliseconds_TestData_NetCore()
        {
            yield return new object[] { 1500.5, new TimeSpan(15005000) };
            yield return new object[] { 2.5, new TimeSpan(25000) };
            yield return new object[] { 1.0, new TimeSpan(10000) };
            yield return new object[] { 0.0, new TimeSpan(0) };
            yield return new object[] { -1.0, new TimeSpan(-10000) };
            yield return new object[] { -2.5, new TimeSpan(-25000) };
            yield return new object[] { -1500.5, new TimeSpan(-15005000) };
        }

        [Theory]
        [MemberData(nameof(FromMilliseconds_TestData_NetCore))]
        public static void FromMilliseconds_Netcore(double value, TimeSpan expected)
        {
            Assert.Equal(expected, TimeSpan.FromMilliseconds(value));
        }

        public static IEnumerable<object[]> FromMilliseconds_TestData_Desktop()
        {
            yield return new object[] { 1500.5, new TimeSpan(15010000) };
            yield return new object[] { 2.5, new TimeSpan(30000) };
            yield return new object[] { 1.0, new TimeSpan(10000) };
            yield return new object[] { 0.0, new TimeSpan(0) };
            yield return new object[] { -1.0, new TimeSpan(-10000) };
            yield return new object[] { -2.5, new TimeSpan(-30000) };
            yield return new object[] { -1500.5, new TimeSpan(-15010000) };
        }

        [Fact]
        public static void FromMilliseconds_Invalid()
        {
            double maxMilliseconds = (double)TimeSpan.MaxValue.Ticks / (double)TimeSpan.TicksPerMillisecond + 1;

            Assert.Throws<OverflowException>(() => TimeSpan.FromMilliseconds(double.PositiveInfinity)); // Value is positive infinity
            Assert.Throws<OverflowException>(() => TimeSpan.FromMilliseconds(double.NegativeInfinity)); // Value is positive infinity

            Assert.Throws<OverflowException>(() => TimeSpan.FromMilliseconds(maxMilliseconds)); // Value > TimeSpan.MaxValue
            Assert.Throws<OverflowException>(() => TimeSpan.FromMilliseconds(-maxMilliseconds)); // Value < TimeSpan.MinValue

            AssertExtensions.Throws<ArgumentException>(null, () => TimeSpan.FromMilliseconds(double.NaN)); // Value is NaN
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

        public static IEnumerable<object[]> TotalSeconds_ExactRepresentation_TestData()
        {
            yield return new object[] { new TimeSpan(0, 0, 0, 0, 0) };
            yield return new object[] { new TimeSpan(0, 0, 0, 1, 0) };
            yield return new object[] { new TimeSpan(0, 0, 0, 1, 100) };
            yield return new object[] { new TimeSpan(0, 0, 0, 0, -100) };
            yield return new object[] { new TimeSpan(0, 0, 0, 0, 34967800) };
        }

        [Theory]
        [MemberData(nameof(TotalSeconds_ExactRepresentation_TestData))]
        public static void TotalSeconds_ExactRepresentation(TimeSpan value)
        {
            Assert.Equal(value, TimeSpan.FromSeconds(value.TotalSeconds));
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
            // Space is trimmed before and after
            yield return new object[] { "       12:24:02", null, new TimeSpan(0, 12, 24, 2, 0) };
            yield return new object[] { "12:24:02      ", null, new TimeSpan(0, 12, 24, 2, 0) };
            yield return new object[] { "     12:24:02      ", null, new TimeSpan(0, 12, 24, 2, 0) };

            // Positive and negative 0 are both valid
            yield return new object[] { "0", null, new TimeSpan(0, 0, 0, 0, 0) };

            // HH:MM
            yield return new object[] { "12:24", null, new TimeSpan(0, 12, 24, 0, 0) };

            // HH:MM:SS
            yield return new object[] { "12:24:02", null, new TimeSpan(0, 12, 24, 2, 0) };

            // DD.HH:MM
            yield return new object[] { "12.03:04", null, new TimeSpan(12, 3, 4, 0, 0) };

            // HH:MM:SS.FF
            yield return new object[] { "12:24:02.01", CultureInfo.InvariantCulture, new TimeSpan(0, 12, 24, 2, 10) };

            // HH:MM:SS.FF w/ varying length zero prefixes on the fraction
            yield return new object[] { "1:1:1.0", CultureInfo.InvariantCulture, new TimeSpan(1, 1, 1) };
            yield return new object[] { "1:1:1.0000000", CultureInfo.InvariantCulture, new TimeSpan(1, 1, 1) };
            yield return new object[] { "1:1:1.1", CultureInfo.InvariantCulture, new TimeSpan(0, 1, 1, 1, 100) };
            yield return new object[] { "1:1:1.01", CultureInfo.InvariantCulture, new TimeSpan(0, 1, 1, 1, 10) };
            yield return new object[] { "1:1:1.001", CultureInfo.InvariantCulture, new TimeSpan(0, 1, 1, 1, 1) };
            yield return new object[] { "1:1:1.0001", CultureInfo.InvariantCulture, new TimeSpan(36610001000) };
            yield return new object[] { "1:1:1.00001", CultureInfo.InvariantCulture, new TimeSpan(36610000100) };
            yield return new object[] { "1:1:1.000001", CultureInfo.InvariantCulture, new TimeSpan(36610000010) };
            yield return new object[] { "1:1:1.0000001", CultureInfo.InvariantCulture, new TimeSpan(36610000001) };

            // DD.HH:MM:SS
            yield return new object[] { "1.12:24:02", null, new TimeSpan(1, 12, 24, 2, 0) };

            // DD:HH:MM:SS
            yield return new object[] { "1:12:24:02", null, new TimeSpan(1, 12, 24, 2, 0) };

            // DD.HH:MM:.FF
            yield return new object[] { "01.23:45:.67", CultureInfo.InvariantCulture, new TimeSpan(1, 23, 45, 0, 670) };

            // DD.HH.MM:SS.FFF
            yield return new object[] { "1.12:24:02.999", CultureInfo.InvariantCulture, new TimeSpan(1, 12, 24, 2, 999) };

            // HH:MM::.FF w/ varying length zero prefixes on the fraction
            yield return new object[] { "1:1:.1", CultureInfo.InvariantCulture, new TimeSpan(36601000000) };
            yield return new object[] { "1:1:.01", CultureInfo.InvariantCulture, new TimeSpan(36600100000) };
            yield return new object[] { "1:1:.001", CultureInfo.InvariantCulture, new TimeSpan(36600010000) };
            yield return new object[] { "1:1:.0001", CultureInfo.InvariantCulture, new TimeSpan(36600001000) };
            yield return new object[] { "1:1:.00001", CultureInfo.InvariantCulture, new TimeSpan(36600000100) };
            yield return new object[] { "1:1:.000001", CultureInfo.InvariantCulture, new TimeSpan(36600000010) };
            yield return new object[] { "1:1:.0000001", CultureInfo.InvariantCulture, new TimeSpan(36600000001) };

            // Just below overflow on various components
            yield return new object[] { "10675199", null, new TimeSpan(9223371936000000000) };
            yield return new object[] { "10675199:00:00", null, new TimeSpan(9223371936000000000) };
            yield return new object[] { "10675199:02:00:00", null, new TimeSpan(9223372008000000000) };
            yield return new object[] { "10675199:02:48:00", null, new TimeSpan(9223372036800000000) };
            yield return new object[] { "10675199:02:48:05", null, new TimeSpan(9223372036850000000) };
            yield return new object[] { "10675199:02:48:05.4775", CultureInfo.InvariantCulture, new TimeSpan(9223372036854775000) };
            yield return new object[] { "00:00:59", null, new TimeSpan(0, 0, 59) };
            yield return new object[] { "00:59:00", null, new TimeSpan(0, 59, 0) };
            yield return new object[] { "23:00:00", null, new TimeSpan(23, 0, 0) };
            yield return new object[] { "24:00:00", null, new TimeSpan(24, 0, 0, 0) };

            if (PlatformDetection.IsNotInvariantGlobalization)
            {
                // Croatia uses ',' in place of '.'
                CultureInfo croatianCulture = new CultureInfo("hr-HR");
                yield return new object[] { "6:12:14:45,348", croatianCulture, new TimeSpan(6, 12, 14, 45, 348) };
            }
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

            // Also negate
            if (!char.IsWhiteSpace(input[0]))
            {
                Assert.Equal(-expected, TimeSpan.Parse("-" + input, provider));
                Assert.True(TimeSpan.TryParse("-" + input, provider, out result));
                Assert.Equal(-expected, result);
            }
        }

        public static IEnumerable<object[]> Parse_Invalid_TestData()
        {
            // FormatExceptions
            yield return new object[] { null, null, typeof(ArgumentNullException) }; // null input
            yield return new object[] { "", null, typeof(FormatException) }; // empty input
            yield return new object[] { "-", null, typeof(FormatException) }; // invalid sole separator
            yield return new object[] { "garbage", null, typeof(FormatException) }; // garbage input
            yield return new object[] { "12/12/12", null, typeof(FormatException) }; // unexpected separators
            yield return new object[] { "00:", null, typeof(FormatException) }; // missing number at end
            yield return new object[] { "00:00:-01", null, typeof(FormatException) }; // misplaced negative
            yield return new object[] { "\012:34:56", null, typeof(FormatException) }; // null char at front
            yield return new object[] { "1\02:34:56", null, typeof(FormatException) }; // null char in HH
            yield return new object[] { "12\0:34:56", null, typeof(FormatException) }; // null char at end of component
            yield return new object[] { "00:00::00", null, typeof(FormatException) }; // duplicated separator
            yield return new object[] { "00:00:00:", null, typeof(FormatException) }; // extra separator at end
            yield return new object[] { "00:00:00:00:00:00:00:00", null, typeof(FormatException) }; // too many components

            if (PlatformDetection.IsNotInvariantGlobalization)
                yield return new object[] { "6:12:14:45.3448", new CultureInfo("hr-HR"), typeof(FormatException) }; // culture that uses ',' rather than '.'

            // OverflowExceptions
            yield return new object[] { "1:1:1.99999999", null, typeof(OverflowException) }; // overflowing fraction

            yield return new object[] { "2147483647", null, typeof(OverflowException) }; // overflowing value == int.MaxValue
            yield return new object[] { "2147483648", null, typeof(OverflowException) }; // overflowing value == int.MaxValue + 1
            yield return new object[] { "10675200", null, typeof(OverflowException) }; // overflowing number of days
            yield return new object[] { "10675200:00:00", null, typeof(OverflowException) }; // overflowing number of hours
            yield return new object[] { "10675199:03:00:00", null, typeof(OverflowException) }; // overflowing number of days + hours
            yield return new object[] { "10675199:02:49:00", null, typeof(OverflowException) }; // overflowing number of days + hours + minutes
            yield return new object[] { "10675199:02:48:06", null, typeof(OverflowException) }; // overflowing number of days + hours + minutes + seconds
            yield return new object[] { "-10675199:02:48:06", null, typeof(OverflowException) }; // negative overflowing d + h + m + s
            yield return new object[] { "10675199:02:48:05.4776", CultureInfo.InvariantCulture, typeof(OverflowException) }; // overflowing days + hours + minutes + seconds + fraction
            yield return new object[] { "-10675199:02:48:05.4776", CultureInfo.InvariantCulture, typeof(OverflowException) }; // negative overflowing d + h + m + s +f
            yield return new object[] { "00:00:60", null, typeof(OverflowException) }; // overflowing seconds
            yield return new object[] { "00:60:00", null, typeof(OverflowException) }; // overflowing minutes
            yield return new object[] { "24:00", null, typeof(OverflowException) }; // overflowing hours
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
            foreach (string constFormat in new[] { "c", "t", "T" }) // "t" and "T" are the same as "c"
            {
                yield return new object[] { "12:24:02", constFormat, new TimeSpan(0, 12, 24, 2) }; // HH:MM:SS
                yield return new object[] { "1.12:24:02", constFormat, new TimeSpan(1, 12, 24, 2) }; // DD.HH:MM:SS
                yield return new object[] { "-01.07:45:16.999", constFormat, -new TimeSpan(1, 7, 45, 16, 999) }; // -DD.HH:MM:SS.FFF
            }

            // "g"
            yield return new object[] { "12", "g", new TimeSpan(12, 0, 0, 0) }; // days
            yield return new object[] { "-12", "g", new TimeSpan(-12, 0, 0, 0) }; // negative days
            yield return new object[] { "12:34", "g", new TimeSpan(12, 34, 00) }; // HH:MM
            yield return new object[] { "-12:34", "g", -new TimeSpan(12, 34, 00) }; // -HH:MM
            yield return new object[] { "1:2:.3", "g", new TimeSpan(0, 1, 2, 0, 300) }; // HH:MM:.FF
            yield return new object[] { "-1:2:.3", "g", -new TimeSpan(0, 1, 2, 0, 300) }; // -HH:MM:.FF
            yield return new object[] { "12:24:02", "g", new TimeSpan(0, 12, 24, 2) }; // HH:MM:SS
            yield return new object[] { "12:24:02.123", "g", new TimeSpan(0, 12, 24, 2, 123) }; // HH:MM:SS.FFF
            yield return new object[] { "-12:24:02.123", "g", -new TimeSpan(0, 12, 24, 2, 123) }; // -HH:MM:SS.FFF
            yield return new object[] { "1:2:3:.4", "g", new TimeSpan(1, 2, 3, 0, 400) }; // DD:HH:MM:.FF
            yield return new object[] { "-1:2:3:.4", "g", -new TimeSpan(1, 2, 3, 0, 400) }; // -DD:HH:MM:.FF
            yield return new object[] { "1:12:24:02", "g", new TimeSpan(1, 12, 24, 2) }; // DD:HH:MM:SS
            yield return new object[] { "-01:07:45:16.999", "g", -new TimeSpan(1, 7, 45, 16, 999) }; // -DD:HH:MM:SS.FFF

            // "G"
            yield return new object[] { "1:12:24:02.243", "G", new TimeSpan(1, 12, 24, 2, 243) }; // DD:MM:HH:SS.FFF
            yield return new object[] { "-01:07:45:16.999", "G", -new TimeSpan(1, 7, 45, 16, 999) }; // -DD:MM:HH:SS.FFF

            // Custom timespan formats
            yield return new object[] { "12.23:32:43", @"dd\.h\:m\:s", new TimeSpan(12, 23, 32, 43) };
            yield return new object[] { "012.23:32:43.893", @"ddd\.h\:m\:s\.fff", new TimeSpan(12, 23, 32, 43, 893) };
            yield return new object[] { "12.05:02:03", @"d\.hh\:mm\:ss", new TimeSpan(12, 5, 2, 3) };
            yield return new object[] { "12:34 minutes", @"mm\:ss\ \m\i\n\u\t\e\s", new TimeSpan(0, 12, 34) };
            yield return new object[] { "12:34 minutes", @"mm\:ss\ ""minutes""", new TimeSpan(0, 12, 34) };
            yield return new object[] { "12:34 minutes", @"mm\:ss\ 'minutes'", new TimeSpan(0, 12, 34) };
            yield return new object[] { "678", "fff", new TimeSpan(0, 0, 0, 0, 678) };
            yield return new object[] { "678", "FFF", new TimeSpan(0, 0, 0, 0, 678) };
            yield return new object[] { "3", "%d", new TimeSpan(3, 0, 0, 0, 0) };
            yield return new object[] { "3", "%h", new TimeSpan(3, 0, 0) };
            yield return new object[] { "3", "%m", new TimeSpan(0, 3, 0) };
            yield return new object[] { "3", "%s", new TimeSpan(0, 0, 3) };
            yield return new object[] { "3", "%f", new TimeSpan(0, 0, 0, 0, 300) };
            yield return new object[] { "3", "%F", new TimeSpan(0, 0, 0, 0, 300) };
        }

        [Theory]
        [MemberData(nameof(ParseExact_Valid_TestData))]
        public static void ParseExact(string input, string format, TimeSpan expected)
        {
            TimeSpan result;
            Assert.Equal(expected, TimeSpan.ParseExact(input, format, new CultureInfo("en-US")));
            Assert.Equal(expected, TimeSpan.ParseExact(input, format, new CultureInfo("en-US"), TimeSpanStyles.None));
            Assert.Equal(expected, TimeSpan.ParseExact(input, new[] { format }, new CultureInfo("en-US")));
            Assert.Equal(expected, TimeSpan.ParseExact(input, new[] { format }, new CultureInfo("en-US"), TimeSpanStyles.None));

            Assert.True(TimeSpan.TryParseExact(input, format, new CultureInfo("en-US"), out result));
            Assert.Equal(expected, result);

            Assert.True(TimeSpan.TryParseExact(input, format, new CultureInfo("en-US"), TimeSpanStyles.None, out result));
            Assert.Equal(expected, result);

            Assert.True(TimeSpan.TryParseExact(input, new[] { format }, new CultureInfo("en-US"), out result));
            Assert.Equal(expected, result);

            Assert.True(TimeSpan.TryParseExact(input, new[] { format }, new CultureInfo("en-US"), TimeSpanStyles.None, out result));
            Assert.Equal(expected, result);

            if (format != "c" && format != "t" && format != "T" && format != "g" && format != "G")
            {
                // TimeSpanStyles is interpreted only for custom formats
                Assert.Equal(expected.Negate(), TimeSpan.ParseExact(input, format, new CultureInfo("en-US"), TimeSpanStyles.AssumeNegative));
                Assert.Equal(expected.Negate(), TimeSpan.ParseExact(input, new string[] { format }, new CultureInfo("en-US"), TimeSpanStyles.AssumeNegative));

                Assert.True(TimeSpan.TryParseExact(input, format, new CultureInfo("en-US"), TimeSpanStyles.AssumeNegative, out result));
                Assert.Equal(expected.Negate(), result);

                Assert.True(TimeSpan.TryParseExact(input, new string[] { format }, new CultureInfo("en-US"), TimeSpanStyles.AssumeNegative, out result));
                Assert.Equal(expected.Negate(), result);
            }
            else
            {
                // Inputs that can be parsed in standard formats with ParseExact should also be parsable with Parse
                Assert.Equal(expected, TimeSpan.Parse(input, CultureInfo.InvariantCulture));

                Assert.True(TimeSpan.TryParse(input, CultureInfo.InvariantCulture, out result));
                Assert.Equal(expected, result);
            }
        }

        public static IEnumerable<object[]> ParseExact_Invalid_TestData()
        {
            yield return new object[] { null, "c", typeof(ArgumentNullException) };
            yield return new object[] { "00:00:00", null, typeof(ArgumentNullException) };
            yield return new object[] { "", "c", typeof(FormatException) };
            yield return new object[] { "-", "c", typeof(FormatException) };
            yield return new object[] { "garbage", "c", typeof(FormatException) };

            // Standard timespan formats 'c', 'g', 'G'
            yield return new object[] { "24:24:02", "c", typeof(OverflowException) };
            yield return new object[] { "1:60:02", "c", typeof(OverflowException) };
            yield return new object[] { "1:59:60", "c", typeof(OverflowException) };
            yield return new object[] { "1.24:59:02", "c", typeof(OverflowException) };
            yield return new object[] { "1.2:60:02", "c", typeof(OverflowException) };
            yield return new object[] { "1?59:02", "c", typeof(FormatException) };
            yield return new object[] { "1:59?02", "c", typeof(FormatException) };
            yield return new object[] { "1:59:02?123", "c", typeof(FormatException) };
            yield return new object[] { "1:12:24:02", "c", typeof(FormatException) };
            yield return new object[] { "12:61:02", "g", typeof(OverflowException) };
            yield return new object[] { "1.12:24:02", "g", typeof(FormatException) };
            yield return new object[] { "1:07:45:16.99999999", "G", typeof(OverflowException) };
            yield return new object[] { "1:12:24:02", "G", typeof(FormatException) };

            // Custom timespan formats
            yield return new object[] { "12.35:32:43", @"dd\.h\:m\:s", typeof(OverflowException) };
            yield return new object[] { "12.5:2:3", @"d\.hh\:mm\:ss", typeof(FormatException) };
            yield return new object[] { "12.5:2", @"d\.hh\:mm\:ss", typeof(FormatException) };
            yield return new object[] { "678", @"ffff", typeof(FormatException) };
            yield return new object[] { "00000012", @"FFFFFFFF", typeof(FormatException) };
            yield return new object[] { "12:034:56", @"hh\mm\ss", typeof(FormatException) };
            yield return new object[] { "12:34:056", @"hh\mm\ss", typeof(FormatException) };
            yield return new object[] { "12:34 minutes", @"mm\:ss\ ""minutes", typeof(FormatException) };
            yield return new object[] { "12:34 minutes", @"mm\:ss\ 'minutes", typeof(FormatException) };
            yield return new object[] { "12:34 mints", @"mm\:ss\ ""minutes""", typeof(FormatException) };
            yield return new object[] { "12:34 mints", @"mm\:ss\ 'minutes'", typeof(FormatException) };
            yield return new object[] { "1", @"d%", typeof(FormatException) };
            yield return new object[] { "1", @"%%d", typeof(FormatException) };
            yield return new object[] { "12:34:56", @"hhh\:mm\:ss", typeof(FormatException) };
            yield return new object[] { "12:34:56", @"hh\:hh\:ss", typeof(FormatException) };
            yield return new object[] { "123:34:56", @"hh\:mm\:ss", typeof(FormatException) };
            yield return new object[] { "12:34:56", @"hh\:mmm\:ss", typeof(FormatException) };
            yield return new object[] { "12:34:56", @"hh\:mm\:mm", typeof(FormatException) };
            yield return new object[] { "12:345:56", @"hh\:mm\:ss", typeof(FormatException) };
            yield return new object[] { "12:34:56", @"hh\:mm\:sss", typeof(FormatException) };
            yield return new object[] { "12:34:56", @"hh\:ss\:ss", typeof(FormatException) };
            yield return new object[] { "12:45", @"ff:ff", typeof(FormatException) };
            yield return new object[] { "000000123", @"ddddddddd", typeof(FormatException) };
            yield return new object[] { "12:34:56", @"dd:dd:hh", typeof(FormatException) };
            yield return new object[] { "123:45", @"dd:hh", typeof(FormatException) };
            yield return new object[] { "12:34", @"dd:vv", typeof(FormatException) };
            yield return new object[] { "00:00:00", "", typeof(FormatException) };
            yield return new object[] { "12.5:2", @"V", typeof(FormatException) };
        }

        [Theory]
        [MemberData(nameof(ParseExact_Invalid_TestData))]
        public static void ParseExactTest_Invalid(string input, string format, Type exceptionType)
        {
            Assert.Throws(exceptionType, () => TimeSpan.ParseExact(input, format, new CultureInfo("en-US")));
            Assert.Throws(exceptionType, () => TimeSpan.ParseExact(input, format, new CultureInfo("en-US"), TimeSpanStyles.None));

            Type exceptionTypeMultiple = exceptionType == typeof(OverflowException) || string.IsNullOrEmpty(format) ? typeof(FormatException) : exceptionType;
            Assert.Throws(exceptionTypeMultiple, () => TimeSpan.ParseExact(input, new string[] { format }, new CultureInfo("en-US")));
            Assert.Throws(exceptionTypeMultiple, () => TimeSpan.ParseExact(input, new string[] { format }, new CultureInfo("en-US"), TimeSpanStyles.None));

            TimeSpan result;
            Assert.False(TimeSpan.TryParseExact(input, format, new CultureInfo("en-US"), out result));
            Assert.Equal(TimeSpan.Zero, result);

            Assert.False(TimeSpan.TryParseExact(input, format, new CultureInfo("en-US"), TimeSpanStyles.None, out result));
            Assert.Equal(TimeSpan.Zero, result);

            Assert.False(TimeSpan.TryParseExact(input, new[] { format }, new CultureInfo("en-US"), out result));
            Assert.Equal(TimeSpan.Zero, result);

            Assert.False(TimeSpan.TryParseExact(input, new[] { format }, new CultureInfo("en-US"), TimeSpanStyles.None, out result));
            Assert.Equal(TimeSpan.Zero, result);
        }

        [Fact]
        public static void ParseExactMultiple_InvalidNullEmptyFormats()
        {
            TimeSpan result;

            AssertExtensions.Throws<ArgumentNullException>("formats", () => TimeSpan.ParseExact("12:34:56", (string[])null, null));
            Assert.False(TimeSpan.TryParseExact("12:34:56", (string[])null, null, out result));

            Assert.Throws<FormatException>(() => TimeSpan.ParseExact("12:34:56", new string[0], null));
            Assert.False(TimeSpan.TryParseExact("12:34:56", new string[0], null, out result));
        }

        public static IEnumerable<object[]> ParseExact_InvalidStyles_TestData()
        {
            yield return new object[] { TimeSpanStyles.None - 1 };
            yield return new object[] { TimeSpanStyles.AssumeNegative + 1 };
        }

        [Theory]
        [MemberData(nameof(ParseExact_InvalidStyles_TestData))]
        public void ParseExact_InvalidStyles_ThrowsArgumentException(TimeSpanStyles styles)
        {
            TimeSpan result;

            string inputString = "00:00:00";
            AssertExtensions.Throws<ArgumentException>("styles", () => TimeSpan.ParseExact(inputString, "s", new CultureInfo("en-US"), styles));
            AssertExtensions.Throws<ArgumentException>("styles", () => TimeSpan.ParseExact(inputString, new string[] { "s" }, new CultureInfo("en-US"), styles));
            AssertExtensions.Throws<ArgumentException>("styles", () => TimeSpan.TryParseExact(inputString, "s", new CultureInfo("en-US"), styles, out result));
            AssertExtensions.Throws<ArgumentException>("styles", () => TimeSpan.TryParseExact(inputString, new string[] { "s" }, new CultureInfo("en-US"), styles, out result));
        }

        public static IEnumerable<object[]> Parse_ValidWithOffsetCount_TestData()
        {
            foreach (object[] inputs in Parse_Valid_TestData())
            {
                yield return new object[] { inputs[0], 0, ((string)inputs[0]).Length, inputs[1], inputs[2] };
            }

            yield return new object[] { "     12:24:02      ", 5, 8, null, new TimeSpan(0, 12, 24, 2, 0) };
            yield return new object[] { "     12:24:02      ", 6, 7, null, new TimeSpan(0, 2, 24, 2, 0) };
            yield return new object[] { "     12:24:02      ", 6, 6, null, new TimeSpan(0, 2, 24, 0, 0) };
            yield return new object[] { "12:24:02.01", 0, 8, CultureInfo.InvariantCulture, new TimeSpan(0, 12, 24, 2, 0) };
            yield return new object[] { "1:1:1.00000001", 0, 7, CultureInfo.InvariantCulture, new TimeSpan(1, 1, 1) };
            yield return new object[] { "1:1:.00000001", 0, 6, CultureInfo.InvariantCulture, new TimeSpan(36600000000) };
            yield return new object[] { "24:00:00", 1, 7, null, new TimeSpan(4, 0, 0) };
        }

        [Theory]
        [MemberData(nameof(Parse_ValidWithOffsetCount_TestData))]
        public static void Parse_Span(string inputString, int offset, int count, IFormatProvider provider, TimeSpan expected)
        {
            ReadOnlySpan<char> input = inputString.AsSpan(offset, count);
            TimeSpan result;

            // Default provider.
            if (provider == null)
            {
                Assert.True(TimeSpan.TryParse(input, out result));
                Assert.Equal(expected, result);
            }

            Assert.Equal(expected, TimeSpan.Parse(input, provider));
            Assert.True(TimeSpan.TryParse(input, provider, out result));
            Assert.Equal(expected, result);

            // Also negate
            if (!char.IsWhiteSpace(input[0]))
            {
                input = ("-" + inputString.Substring(offset, count)).AsSpan();
                expected = -expected;

                Assert.Equal(expected, TimeSpan.Parse(input, provider));
                Assert.True(TimeSpan.TryParse(input, provider, out result));
                Assert.Equal(expected, result);
            }
        }

        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_Span_Invalid(string inputString, IFormatProvider provider, Type exceptionType)
        {
            if (inputString != null)
            {
                Assert.Throws(exceptionType, () => TimeSpan.Parse(inputString.AsSpan(), provider));
                Assert.False(TimeSpan.TryParse(inputString.AsSpan(), provider, out TimeSpan result));
                Assert.Equal(TimeSpan.Zero, result);
            }
        }

        [Theory]
        [MemberData(nameof(ParseExact_Valid_TestData))]
        public static void ParseExact_Span_Valid(string inputString, string format, TimeSpan expected)
        {
            ReadOnlySpan<char> input = inputString.AsSpan();

            TimeSpan result;
            Assert.Equal(expected, TimeSpan.ParseExact(input, format, new CultureInfo("en-US")));
            Assert.Equal(expected, TimeSpan.ParseExact(input, format, new CultureInfo("en-US"), TimeSpanStyles.None));
            Assert.Equal(expected, TimeSpan.ParseExact(input, new[] { format }, new CultureInfo("en-US")));
            Assert.Equal(expected, TimeSpan.ParseExact(input, new[] { format }, new CultureInfo("en-US"), TimeSpanStyles.None));

            Assert.True(TimeSpan.TryParseExact(input, format, new CultureInfo("en-US"), out result));
            Assert.Equal(expected, result);

            Assert.True(TimeSpan.TryParseExact(input, format, new CultureInfo("en-US"), TimeSpanStyles.None, out result));
            Assert.Equal(expected, result);

            Assert.True(TimeSpan.TryParseExact(input, new[] { format }, new CultureInfo("en-US"), out result));
            Assert.Equal(expected, result);

            Assert.True(TimeSpan.TryParseExact(input, new[] { format }, new CultureInfo("en-US"), TimeSpanStyles.None, out result));
            Assert.Equal(expected, result);

            if (format != "c" && format != "t" && format != "T" && format != "g" && format != "G")
            {
                // TimeSpanStyles is interpreted only for custom formats
                Assert.Equal(expected.Negate(), TimeSpan.ParseExact(input, format, new CultureInfo("en-US"), TimeSpanStyles.AssumeNegative));

                Assert.True(TimeSpan.TryParseExact(input, format, new CultureInfo("en-US"), TimeSpanStyles.AssumeNegative, out result));
                Assert.Equal(expected.Negate(), result);
            }
            else
            {
                // Inputs that can be parsed in standard formats with ParseExact should also be parsable with Parse
                Assert.Equal(expected, TimeSpan.Parse(input, CultureInfo.InvariantCulture));

                Assert.True(TimeSpan.TryParse(input, CultureInfo.InvariantCulture, out result));
                Assert.Equal(expected, result);
            }
        }

        [Theory]
        [MemberData(nameof(ParseExact_Invalid_TestData))]
        public static void ParseExactTest_Span_Invalid(string inputString, string format, Type exceptionType)
        {
            if (inputString != null && format != null)
            {
                Assert.Throws(exceptionType, () => TimeSpan.ParseExact(inputString.AsSpan(), format, new CultureInfo("en-US")));

                TimeSpan result;
                Assert.False(TimeSpan.TryParseExact(inputString.AsSpan(), format, new CultureInfo("en-US"), out result));
                Assert.Equal(TimeSpan.Zero, result);

                Assert.False(TimeSpan.TryParseExact(inputString.AsSpan(), format, new CultureInfo("en-US"), TimeSpanStyles.None, out result));
                Assert.Equal(TimeSpan.Zero, result);

                Assert.False(TimeSpan.TryParseExact(inputString.AsSpan(), new[] { format }, new CultureInfo("en-US"), out result));
                Assert.Equal(TimeSpan.Zero, result);

                Assert.False(TimeSpan.TryParseExact(inputString.AsSpan(), new[] { format }, new CultureInfo("en-US"), TimeSpanStyles.None, out result));
                Assert.Equal(TimeSpan.Zero, result);
            }
        }

        [Fact]
        public static void ParseExactMultiple_Span_InvalidNullEmptyFormats()
        {
            TimeSpan result;

            AssertExtensions.Throws<ArgumentNullException>("formats", () => TimeSpan.ParseExact("12:34:56".AsSpan(), (string[])null, null));
            Assert.False(TimeSpan.TryParseExact("12:34:56".AsSpan(), (string[])null, null, out result));

            Assert.Throws<FormatException>(() => TimeSpan.ParseExact("12:34:56".AsSpan(), new string[0], null));
            Assert.False(TimeSpan.TryParseExact("12:34:56".AsSpan(), new string[0], null, out result));
        }

        [Theory]
        [MemberData(nameof(ParseExact_InvalidStyles_TestData))]
        public void ParseExact_InvalidStylesSpan_ThrowsArgumentException(TimeSpanStyles styles)
        {
            TimeSpan result;

            string inputString = "00:00:00";
            AssertExtensions.Throws<ArgumentException>("styles", () => TimeSpan.ParseExact(inputString.AsSpan(), "s", new CultureInfo("en-US"), styles));
            AssertExtensions.Throws<ArgumentException>("styles", () => TimeSpan.ParseExact(inputString.AsSpan(), new string[] { "s" }, new CultureInfo("en-US"), styles));
            AssertExtensions.Throws<ArgumentException>("styles", () => TimeSpan.TryParseExact(inputString.AsSpan(), "s", new CultureInfo("en-US"), styles, out result));
            AssertExtensions.Throws<ArgumentException>("styles", () => TimeSpan.TryParseExact(inputString.AsSpan(), new string[] { "s" }, new CultureInfo("en-US"), styles, out result));
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

        public static IEnumerable<object[]> ToString_TestData()
        {
            CultureInfo invariantInfo = CultureInfo.InvariantCulture;
            CultureInfo commaSeparatorInfo = new CultureInfo("fr-FR");
            commaSeparatorInfo.NumberFormat.NegativeSign = "!@#!@#"; // validate this is ignored

            var input = new TimeSpan(123456789101112);
            yield return new object[] { input, "%d", invariantInfo, "142" };
            yield return new object[] { input, "dd", invariantInfo, "142" };
            yield return new object[] { input, "%h", invariantInfo, "21" };
            yield return new object[] { input, "hh", invariantInfo, "21" };
            yield return new object[] { input, "%m", invariantInfo, "21" };
            yield return new object[] { input, "mm", invariantInfo, "21" };
            yield return new object[] { input, "%s", invariantInfo, "18" };
            yield return new object[] { input, "ss", invariantInfo, "18" };
            yield return new object[] { input, "%f", invariantInfo, "9" };
            yield return new object[] { input, "ff", invariantInfo, "91" };
            yield return new object[] { input, "fff", invariantInfo, "910" };
            yield return new object[] { input, "ffff", invariantInfo, "9101" };
            yield return new object[] { input, "fffff", invariantInfo, "91011" };
            yield return new object[] { input, "ffffff", invariantInfo, "910111" };
            yield return new object[] { input, "fffffff", invariantInfo, "9101112" };
            yield return new object[] { input, "%F", invariantInfo, "9" };
            yield return new object[] { input, "FF", invariantInfo, "91" };
            yield return new object[] { input, "FFF", invariantInfo, "91" };
            yield return new object[] { input, "FFFF", invariantInfo, "9101" };
            yield return new object[] { input, "FFFFF", invariantInfo, "91011" };
            yield return new object[] { input, "FFFFFF", invariantInfo, "910111" };
            yield return new object[] { input, "FFFFFFF", invariantInfo, "9101112" };
            yield return new object[] { input, "dd\\.ss", invariantInfo, "142.18" };
            yield return new object[] { input, "dd\\.ss", commaSeparatorInfo, "142.18" };
            yield return new object[] { input, "dddddd\\.ss", invariantInfo, "000142.18" };

            // constant/invariant format
            CultureInfo[] cultureInfos = PlatformDetection.IsInvariantGlobalization ? new[] { (CultureInfo)null } : new[] { null, invariantInfo, commaSeparatorInfo };
            foreach (CultureInfo info in cultureInfos) // validate that culture is ignored
            {
                foreach (string constFormat in new[] { null, "c", "t", "T" })
                {
                    yield return new object[] { input, constFormat, info, "142.21:21:18.9101112" };
                    yield return new object[] { TimeSpan.Zero, constFormat, info, "00:00:00" };
                    yield return new object[] { new TimeSpan(1), constFormat, info, "00:00:00.0000001" };
                    yield return new object[] { new TimeSpan(-1), constFormat, info, "-00:00:00.0000001" };
                    yield return new object[] { TimeSpan.MaxValue, constFormat, info, "10675199.02:48:05.4775807" };
                    yield return new object[] { TimeSpan.MinValue, constFormat, info, "-10675199.02:48:05.4775808" };
                    yield return new object[] { new TimeSpan(1, 2, 3), constFormat, info, "01:02:03" };
                    yield return new object[] { -new TimeSpan(1, 2, 3), constFormat, info, "-01:02:03" };
                    yield return new object[] { new TimeSpan(12, 34, 56), constFormat, info, "12:34:56" };
                    yield return new object[] { new TimeSpan(12, 34, 56, 23), constFormat, info, "13.10:56:23" };
                    yield return new object[] { new TimeSpan(12, 34, 56, 23, 45), constFormat, info, "13.10:56:23.0450000" };
                    yield return new object[] { new TimeSpan(0, 23, 59, 59, 999), constFormat, info, "23:59:59.9990000" };
                }
            }

            // general short format, invariant culture
            yield return new object[] { input, "g", invariantInfo, "142:21:21:18.9101112" };
            yield return new object[] { TimeSpan.Zero, "g", invariantInfo, "0:00:00" };
            yield return new object[] { new TimeSpan(1), "g", invariantInfo, "0:00:00.0000001" };
            yield return new object[] { new TimeSpan(-1), "g", invariantInfo, "-0:00:00.0000001" };
            yield return new object[] { TimeSpan.MaxValue, "g", invariantInfo, "10675199:2:48:05.4775807" };
            yield return new object[] { TimeSpan.MinValue, "g", invariantInfo, "-10675199:2:48:05.4775808" };
            yield return new object[] { new TimeSpan(1, 2, 3), "g", invariantInfo, "1:02:03" };
            yield return new object[] { -new TimeSpan(1, 2, 3), "g", invariantInfo, "-1:02:03" };
            yield return new object[] { new TimeSpan(12, 34, 56), "g", invariantInfo, "12:34:56" };
            yield return new object[] { new TimeSpan(12, 34, 56, 23), "g", invariantInfo, "13:10:56:23" };
            yield return new object[] { new TimeSpan(12, 34, 56, 23, 45), "g", invariantInfo, "13:10:56:23.045" };
            yield return new object[] { new TimeSpan(0, 23, 59, 59, 999), "g", invariantInfo, "23:59:59.999" };

            if (PlatformDetection.IsNotInvariantGlobalization)
            {
                // general short format, NumberDecimalSeparator used
                yield return new object[] { input, "g", commaSeparatorInfo, "142:21:21:18,9101112" };
                yield return new object[] { TimeSpan.Zero, "g", commaSeparatorInfo, "0:00:00" };
                yield return new object[] { new TimeSpan(1), "g", commaSeparatorInfo, "0:00:00,0000001" };
                yield return new object[] { new TimeSpan(-1), "g", commaSeparatorInfo, "-0:00:00,0000001" };
                yield return new object[] { TimeSpan.MaxValue, "g", commaSeparatorInfo, "10675199:2:48:05,4775807" };
                yield return new object[] { TimeSpan.MinValue, "g", commaSeparatorInfo, "-10675199:2:48:05,4775808" };
                yield return new object[] { new TimeSpan(1, 2, 3), "g", commaSeparatorInfo, "1:02:03" };
                yield return new object[] { -new TimeSpan(1, 2, 3), "g", commaSeparatorInfo, "-1:02:03" };
                yield return new object[] { new TimeSpan(12, 34, 56), "g", commaSeparatorInfo, "12:34:56" };
                yield return new object[] { new TimeSpan(12, 34, 56, 23), "g", commaSeparatorInfo, "13:10:56:23" };
                yield return new object[] { new TimeSpan(12, 34, 56, 23, 45), "g", commaSeparatorInfo, "13:10:56:23,045" };
                yield return new object[] { new TimeSpan(0, 23, 59, 59, 999), "g", commaSeparatorInfo, "23:59:59,999" };
            }

            // general long format, invariant culture
            yield return new object[] { input, "G", invariantInfo, "142:21:21:18.9101112" };
            yield return new object[] { TimeSpan.Zero, "G", invariantInfo, "0:00:00:00.0000000" };
            yield return new object[] { new TimeSpan(1), "G", invariantInfo, "0:00:00:00.0000001" };
            yield return new object[] { new TimeSpan(-1), "G", invariantInfo, "-0:00:00:00.0000001" };
            yield return new object[] { TimeSpan.MaxValue, "G", invariantInfo, "10675199:02:48:05.4775807" };
            yield return new object[] { TimeSpan.MinValue, "G", invariantInfo, "-10675199:02:48:05.4775808" };
            yield return new object[] { new TimeSpan(1, 2, 3), "G", invariantInfo, "0:01:02:03.0000000" };
            yield return new object[] { -new TimeSpan(1, 2, 3), "G", invariantInfo, "-0:01:02:03.0000000" };
            yield return new object[] { new TimeSpan(12, 34, 56), "G", invariantInfo, "0:12:34:56.0000000" };
            yield return new object[] { new TimeSpan(12, 34, 56, 23), "G", invariantInfo, "13:10:56:23.0000000" };
            yield return new object[] { new TimeSpan(12, 34, 56, 23, 45), "G", invariantInfo, "13:10:56:23.0450000" };
            yield return new object[] { new TimeSpan(0, 23, 59, 59, 999), "G", invariantInfo, "0:23:59:59.9990000" };

            if (PlatformDetection.IsNotInvariantGlobalization)
            {
                // general long format, NumberDecimalSeparator used
                yield return new object[] { input, "G", commaSeparatorInfo, "142:21:21:18,9101112" };
                yield return new object[] { TimeSpan.Zero, "G", commaSeparatorInfo, "0:00:00:00,0000000" };
                yield return new object[] { new TimeSpan(1), "G", commaSeparatorInfo, "0:00:00:00,0000001" };
                yield return new object[] { new TimeSpan(-1), "G", commaSeparatorInfo, "-0:00:00:00,0000001" };
                yield return new object[] { TimeSpan.MaxValue, "G", commaSeparatorInfo, "10675199:02:48:05,4775807" };
                yield return new object[] { TimeSpan.MinValue, "G", commaSeparatorInfo, "-10675199:02:48:05,4775808" };
                yield return new object[] { new TimeSpan(1, 2, 3), "G", commaSeparatorInfo, "0:01:02:03,0000000" };
                yield return new object[] { -new TimeSpan(1, 2, 3), "G", commaSeparatorInfo, "-0:01:02:03,0000000" };
                yield return new object[] { new TimeSpan(12, 34, 56), "G", commaSeparatorInfo, "0:12:34:56,0000000" };
                yield return new object[] { new TimeSpan(12, 34, 56, 23), "G", commaSeparatorInfo, "13:10:56:23,0000000" };
                yield return new object[] { new TimeSpan(12, 34, 56, 23, 45), "G", commaSeparatorInfo, "13:10:56:23,0450000" };
                yield return new object[] { new TimeSpan(0, 23, 59, 59, 999), "G", commaSeparatorInfo, "0:23:59:59,9990000" };
            }
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public static void ToString_Valid(TimeSpan input, string format, CultureInfo info, string expected)
        {
            Assert.Equal(expected, input.ToString(format, info));
            if (info == null)
            {
                Assert.Equal(expected, input.ToString(format));
                if (format == null)
                {
                    Assert.Equal(expected, input.ToString());
                }
            }
        }

        public static IEnumerable<object[]> ToString_InvalidFormat_TestData()
        {
            yield return new object[] { "y" };
            yield return new object[] { "cc" };
            yield return new object[] { "F" };
            yield return new object[] { "C" };
        }

        [Theory]
        [MemberData(nameof(ToString_InvalidFormat_TestData))]
        public void ToString_InvalidFormat_ThrowsFormatException(string invalidFormat)
        {
            Assert.Throws<FormatException>(() => new TimeSpan().ToString(invalidFormat));
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

        private static void VerifyTimeSpan(TimeSpan timeSpan, int days, int hours, int minutes, int seconds,
            int milliseconds, int microseconds)
        {
            VerifyTimeSpan(timeSpan, days, hours, minutes, seconds, milliseconds);
            Assert.Equal(microseconds, timeSpan.Microseconds);
        }

        private static void VerifyTimeSpan(TimeSpan timeSpan, int days, int hours, int minutes, int seconds,
            int milliseconds, int microseconds, int nanoseconds)
        {
            VerifyTimeSpan(timeSpan, days, hours, minutes, seconds, milliseconds, microseconds);
            Assert.Equal(nanoseconds, timeSpan.Nanoseconds);
        }

        [Theory]
        [MemberData(nameof(CompareTo_TestData))]
        public static void CompareTo_Object(TimeSpan timeSpan1, object obj, int expected)
        {
            Assert.Equal(expected, Math.Sign(timeSpan1.CompareTo(obj)));
        }

        public static IEnumerable<object[]> MultiplicationTestData()
        {
            yield return new object[] {new TimeSpan(2, 30, 0), 2.0, new TimeSpan(5, 0, 0)};
            yield return new object[] {new TimeSpan(14, 2, 30, 0), 192.0, TimeSpan.FromDays(2708)};
            yield return new object[] {TimeSpan.FromDays(366), Math.PI, new TimeSpan(993446995288779)};
            yield return new object[] {TimeSpan.FromDays(366), -Math.E, new TimeSpan(-859585952922633)};
            yield return new object[] {TimeSpan.FromDays(29.530587981), 13.0, TimeSpan.FromDays(29.530587981 * 13.0) };
            yield return new object[] {TimeSpan.FromDays(-29.530587981), -12.0, TimeSpan.FromDays(-29.530587981 * -12.0) };
            yield return new object[] {TimeSpan.FromDays(-29.530587981), 0.0, TimeSpan.Zero};
            yield return new object[] {TimeSpan.MaxValue, 0.5, TimeSpan.FromTicks((long)(long.MaxValue * 0.5))};
        }

        // ParseDifferentLengthFractionWithLeadingZerosData mainly testing the behavior we have fixed in net core
        // which is the way we normalize the parsed fraction and possibly rounding it.
        public static IEnumerable<object[]> ParseDifferentLengthFractionWithLeadingZerosData()
        {
            yield return new object[] {"00:00:00.00000001",   new TimeSpan(0)};
            yield return new object[] {"00:00:00.00000005",   new TimeSpan(1)};
            yield return new object[] {"00:00:00.09999999",   new TimeSpan(1_000_000)};
            yield return new object[] {"00:00:00.0268435455", new TimeSpan(268435)};
            yield return new object[] {"00:00:00.01",         new TimeSpan(1_00_000)};
            yield return new object[] {"0:00:00.01000000",    new TimeSpan(100_000)};
            yield return new object[] {"0:00:00.010000000",   new TimeSpan(100_000)};
            yield return new object[] {"0:00:00.0123456",     new TimeSpan(123456)};
            yield return new object[] {"0:00:00.00123456",    new TimeSpan(12346)};
            yield return new object[] {"0:00:00.00000098",    new TimeSpan(10)};
            yield return new object[] {"0:00:00.00000099",    new TimeSpan(10)};
        }

        [Theory, MemberData(nameof(ParseDifferentLengthFractionWithLeadingZerosData))]
        public static void ParseDifferentLengthFractionWithLeadingZeros(string input, TimeSpan expected)
        {
            Assert.Equal(expected, TimeSpan.Parse(input, CultureInfo.InvariantCulture));
            Assert.Equal(expected, TimeSpan.ParseExact(input, "g", CultureInfo.InvariantCulture));
        }

        [Theory, MemberData(nameof(MultiplicationTestData))]
        public static void Multiplication(TimeSpan timeSpan, double factor, TimeSpan expected)
        {
            Assert.Equal(expected, timeSpan * factor);
            Assert.Equal(expected, factor * timeSpan);
        }

        [Fact]
        public static void OverflowingMultiplication()
        {
            Assert.Throws<OverflowException>(() => TimeSpan.MaxValue * 1.000000001);
            Assert.Throws<OverflowException>(() => -1.000000001 * TimeSpan.MaxValue);
        }

        [Fact]
        public static void NaNMultiplication()
        {
            AssertExtensions.Throws<ArgumentException>("factor", () => TimeSpan.FromDays(1) * double.NaN);
            AssertExtensions.Throws<ArgumentException>("factor", () => double.NaN * TimeSpan.FromDays(1));
        }

        [Theory, MemberData(nameof(MultiplicationTestData))]
        public static void Division(TimeSpan timeSpan, double factor, TimeSpan expected)
        {
            Assert.Equal(factor, expected / timeSpan, 14);
            double divisor = 1.0 / factor;
            Assert.Equal(expected, timeSpan / divisor);
        }

        [Fact]
        public static void DivideByZero()
        {
            Assert.Throws<OverflowException>(() => TimeSpan.FromDays(1) / 0);
            Assert.Throws<OverflowException>(() => TimeSpan.FromDays(-1) / 0);
            Assert.Throws<OverflowException>(() => TimeSpan.Zero / 0);
            Assert.Equal(double.PositiveInfinity, TimeSpan.FromDays(1) / TimeSpan.Zero);
            Assert.Equal(double.NegativeInfinity, TimeSpan.FromDays(-1) / TimeSpan.Zero);
            Assert.True(double.IsNaN(TimeSpan.Zero / TimeSpan.Zero));
        }

        [Fact]
        public static void NaNDivision()
        {
            AssertExtensions.Throws<ArgumentException>("divisor", () => TimeSpan.FromDays(1) / double.NaN);
        }

        [Theory, MemberData(nameof(MultiplicationTestData))]
        public static void NamedMultiplication(TimeSpan timeSpan, double factor, TimeSpan expected)
        {
            Assert.Equal(expected, timeSpan.Multiply(factor));
        }

        [Fact]
        public static void NamedOverflowingMultiplication()
        {
            Assert.Throws<OverflowException>(() => TimeSpan.MaxValue.Multiply(1.000000001));
        }

        [Fact]
        public static void NamedNaNMultiplication()
        {
            AssertExtensions.Throws<ArgumentException>("factor", () => TimeSpan.FromDays(1).Multiply(double.NaN));
        }

        [Theory, MemberData(nameof(MultiplicationTestData))]
        public static void NamedDivision(TimeSpan timeSpan, double factor, TimeSpan expected)
        {
            Assert.Equal(factor, expected.Divide(timeSpan), 14);
            double divisor = 1.0 / factor;
            Assert.Equal(expected, timeSpan.Divide(divisor));
        }

        [Fact]
        public static void NamedDivideByZero()
        {
            Assert.Throws<OverflowException>(() => TimeSpan.FromDays(1).Divide(0));
            Assert.Throws<OverflowException>(() => TimeSpan.FromDays(-1).Divide(0));
            Assert.Throws<OverflowException>(() => TimeSpan.Zero.Divide(0));
            Assert.Equal(double.PositiveInfinity, TimeSpan.FromDays(1).Divide(TimeSpan.Zero));
            Assert.Equal(double.NegativeInfinity, TimeSpan.FromDays(-1).Divide(TimeSpan.Zero));
            Assert.True(double.IsNaN(TimeSpan.Zero.Divide(TimeSpan.Zero)));
        }

        [Fact]
        public static void NamedNaNDivision()
        {
            AssertExtensions.Throws<ArgumentException>("divisor", () => TimeSpan.FromDays(1).Divide(double.NaN));
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public static void TryFormat_Valid(TimeSpan input, string format, CultureInfo info, string expected)
        {
            // UTF16
            {
                int charsWritten;
                Span<char> dst;

                dst = new char[expected.Length - 1];
                Assert.False(input.TryFormat(dst, out charsWritten, format, info));
                Assert.Equal(0, charsWritten);

                dst = new char[expected.Length];
                Assert.True(input.TryFormat(dst, out charsWritten, format, info));
                Assert.Equal(expected.Length, charsWritten);
                Assert.Equal(expected, new string(dst));

                dst = new char[expected.Length + 1];
                Assert.True(input.TryFormat(dst, out charsWritten, format, info));
                Assert.Equal(expected.Length, charsWritten);
                Assert.Equal(expected, new string(dst.Slice(0, dst.Length - 1)));
                Assert.Equal(0, dst[dst.Length - 1]);
            }

            // UTF8
            {
                int bytesWritten;
                Span<byte> dst;

                dst = new byte[expected.Length - 1];
                Assert.False(input.TryFormat(dst, out bytesWritten, format, info));
                Assert.Equal(0, bytesWritten);

                dst = new byte[expected.Length];
                Assert.True(input.TryFormat(dst, out bytesWritten, format, info));
                Assert.Equal(expected.Length, bytesWritten);
                Assert.Equal(expected, Encoding.UTF8.GetString(dst));

                dst = new byte[expected.Length + 1];
                Assert.True(input.TryFormat(dst, out bytesWritten, format, info));
                Assert.Equal(expected.Length, bytesWritten);
                Assert.Equal(expected, Encoding.UTF8.GetString(dst.Slice(0, dst.Length - 1)));
                Assert.Equal(0, dst[dst.Length - 1]);
            }
        }

        [Theory]
        [MemberData(nameof(ToString_InvalidFormat_TestData))]
        public void TryFormat_InvalidFormat_ThrowsFormatException(string invalidFormat)
        {
            Assert.Throws<FormatException>(() => new TimeSpan().TryFormat(new char[1], out int charsWritten, invalidFormat, null));
            Assert.Throws<FormatException>(() => ((IUtf8SpanFormattable)new TimeSpan()).TryFormat(new byte[1], out int bytesWritten, invalidFormat, null));
        }

        [Fact]
        public static void ConvertToTimeSpanPrecisionTest()
        {
            Assert.Equal(12345, TimeSpan.FromMilliseconds(1.23456).Ticks);
            Assert.Equal(12345, TimeSpan.FromMilliseconds(1.234567).Ticks);

            Assert.Equal(12345600, TimeSpan.FromSeconds(1.23456).Ticks);

            Assert.Equal(1.23456 * 60 * 10_000_000, TimeSpan.FromMinutes(1.23456).Ticks);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(100, 10)]
        [InlineData(1_000, 100)]
        public static void TestTotalMicroseconds(long ticks, double totalMicroseconds)
        {
            var timeSpan = new TimeSpan(ticks);
            Assert.Equal(totalMicroseconds, timeSpan.TotalMicroseconds);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(100, 10_000)]
        [InlineData(1_000, 100_000)]
        public static void TestTotalNanoseconds(long ticks, double totalNanoseconds)
        {
            var timeSpan = new TimeSpan(ticks);
            Assert.Equal(totalNanoseconds, timeSpan.TotalNanoseconds);
        }
    }
}
