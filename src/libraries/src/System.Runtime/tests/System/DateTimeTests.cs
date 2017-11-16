// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Xunit;

namespace System.Tests
{
    public static partial class DateTimeTests
    {
        [Fact]
        public static void MaxValue()
        {
            VerifyDateTime(DateTime.MaxValue, 9999, 12, 31, 23, 59, 59, 999, DateTimeKind.Unspecified);
        }

        [Fact]
        public static void MinValue()
        {
            VerifyDateTime(DateTime.MinValue, 1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified);
        }

        [Fact]
        public static void Ctor_Long()
        {
            VerifyDateTime(new DateTime(999999999999999999), 3169, 11, 16, 9, 46, 39, 999, DateTimeKind.Unspecified);
        }

        [Fact]
        public static void Ctor_Long_InvalidTicks_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("ticks", () => new DateTime(DateTime.MinValue.Ticks - 1)); // Ticks < DateTime.MinValue.Ticks
            AssertExtensions.Throws<ArgumentOutOfRangeException>("ticks", () => new DateTime(DateTime.MaxValue.Ticks + 1)); // Ticks > DateTime.MaxValue.Ticks
        }

        [Fact]
        public static void Ctor_Long_DateTimeKind()
        {
            VerifyDateTime(new DateTime(999999999999999999, DateTimeKind.Utc), 3169, 11, 16, 9, 46, 39, 999, DateTimeKind.Utc);
        }

        [Fact]
        public static void Ctor_Long_DateTimeKind_Invalid()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("ticks", () => new DateTime(DateTime.MinValue.Ticks - 1, DateTimeKind.Utc)); // Ticks < DateTime.MinValue.Ticks
            AssertExtensions.Throws<ArgumentOutOfRangeException>("ticks", () => new DateTime(DateTime.MaxValue.Ticks + 1, DateTimeKind.Utc)); // Ticks > DateTime.MaxValue.Ticks

            AssertExtensions.Throws<ArgumentException>("kind", () => new DateTime(0, DateTimeKind.Unspecified - 1)); // Invalid date time kind
            AssertExtensions.Throws<ArgumentException>("kind", () => new DateTime(0, DateTimeKind.Local + 1)); // Invalid date time kind
        }

        [Fact]
        public static void Ctor_Int_Int_Int()
        {
            var dateTime = new DateTime(2012, 6, 11);
            VerifyDateTime(dateTime, 2012, 6, 11, 0, 0, 0, 0, DateTimeKind.Unspecified);
        }

        [Fact]
        public static void Ctor_Int_Int_Int_Invalid()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(0, 1, 1)); // Year < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(10000, 1, 1)); // Year > 9999

            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 0, 1)); // Month < 1
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 13, 1)); // Month > 12

            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, 0)); // Day < 1
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, 32)); // Day > days in month
        }

        [Fact]
        public static void Ctor_Int_Int_Int_Int_Int_Int()
        {
            var dateTime = new DateTime(2012, 12, 31, 13, 50, 10);
            VerifyDateTime(dateTime, 2012, 12, 31, 13, 50, 10, 0, DateTimeKind.Unspecified);
        }

        [Fact]
        public static void Ctor_Int_Int_Int_Int_Int_Int_Invalid()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(0, 1, 1, 1, 1, 1)); // Year < 1
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(10000, 1, 1, 1, 1, 1)); // Year > 9999

            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 0, 1, 1, 1, 1)); // Month < 1
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 13, 1, 1, 1, 1)); // Month > 12

            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, 0, 1, 1, 1)); // Day < 1
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, 32, 1, 1, 1)); // Day > days in month

            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, 1, -1, 1, 1)); // Hour < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, 1, 24, 1, 1)); // Hour > 23

            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, 1, 1, -1, 1)); // Minute < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, 1, 1, 60, 1)); // Minute > 59

            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, 1, 1, 1, -1)); // Second < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, 1, 1, 1, 60)); // Second > 59
        }

        [Fact]
        public static void Ctor_Int_Int_Int_Int_Int_Int_Int_DateTimeKind()
        {
            var dateTime = new DateTime(1986, 8, 15, 10, 20, 5, DateTimeKind.Local);
            VerifyDateTime(dateTime, 1986, 8, 15, 10, 20, 5, 0, DateTimeKind.Local);
        }

        [Fact]
        public static void Ctor_Int_Int_Int_Int_Int_Int_DateTimeKind_Invalid()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(0, 1, 1, 1, 1, 1, DateTimeKind.Utc)); // Year < 1
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(10000, 1, 1, 1, 1, 1, DateTimeKind.Utc)); // Year > 9999

            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 0, 1, 1, 1, 1, DateTimeKind.Utc)); // Month < 1
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 13, 1, 1, 1, 1, DateTimeKind.Utc)); // Month > 12

            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, 0, 1, 1, 1, DateTimeKind.Utc)); // Day < 1
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, 32, 1, 1, 1, DateTimeKind.Utc)); // Day > days in month

            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, 1, -1, 1, 1, DateTimeKind.Utc)); // Hour < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, 1, 24, 1, 1, DateTimeKind.Utc)); // Hour > 23

            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, 1, 1, -1, 1, DateTimeKind.Utc)); // Minute < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, 1, 1, 60, 1, DateTimeKind.Utc)); // Minute > 59

            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, 1, 1, 1, -1, DateTimeKind.Utc)); // Second < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, 1, 1, 1, 60, DateTimeKind.Utc)); // Second > 59

            AssertExtensions.Throws<ArgumentException>("kind", () => new DateTime(1, 1, 1, 1, 1, 1, DateTimeKind.Unspecified - 1)); // Invalid date time kind
            AssertExtensions.Throws<ArgumentException>("kind", () => new DateTime(1, 1, 1, 1, 1, 1, DateTimeKind.Local + 1)); // Invalid date time kind
        }

        [Fact]
        public static void Ctor_Int_Int_Int_Int_Int_Int_Int()
        {
            var dateTime = new DateTime(1973, 10, 6, 14, 30, 0, 500);
            VerifyDateTime(dateTime, 1973, 10, 6, 14, 30, 0, 500, DateTimeKind.Unspecified);
        }

        [Fact]
        public static void Ctor_Int_Int_Int_Int_Int_Int_Int_Invalid()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(0, 1, 1, 1, 1, 1, 1)); // Year < 1
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(10000, 1, 1, 1, 1, 1, 1)); // Year > 9999

            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 0, 1, 1, 1, 1, 1)); // Month < 1
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 13, 1, 1, 1, 1, 1)); // Month > 12

            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, 0, 1, 1, 1, 1)); // Day < 1
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, 32, 1, 1, 1, 1)); // Day > days in month

            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, 1, -1, 1, 1, 1)); // Hour < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, 1, 24, 1, 1, 1)); // Hour > 23

            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, 1, 1, -1, 1, 1)); // Minute < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, 1, 1, 60, 1, 1)); // Minute > 59

            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, 1, 1, 1, -1, 1)); // Second < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, 1, 1, 1, 60, 1)); // Second > 59

            AssertExtensions.Throws<ArgumentOutOfRangeException>("millisecond", () => new DateTime(1, 1, 1, 1, 1, 1, -1)); // Milisecond < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("millisecond", () => new DateTime(1, 1, 1, 1, 1, 1, 1000)); // Millisecond > 999
        }

        [Fact]
        public static void Ctor_Int_Int_Int_Int_Int_Int_Int_Int_DateTimeKind()
        {
            var dateTime = new DateTime(1986, 8, 15, 10, 20, 5, 600, DateTimeKind.Local);
            VerifyDateTime(dateTime, 1986, 8, 15, 10, 20, 5, 600, DateTimeKind.Local);
        }

        [Fact]
        public static void Ctor_Int_Int_Int_Int_Int_Int_Int_DateTimeKind_Invalid()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(0, 1, 1, 1, 1, 1, 1, DateTimeKind.Utc)); // Year < 1
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(10000, 1, 1, 1, 1, 1, 1, DateTimeKind.Utc)); // Year > 9999

            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 0, 1, 1, 1, 1, 1, DateTimeKind.Utc)); // Month < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 13, 1, 1, 1, 1, 1, DateTimeKind.Utc)); // Month > 12

            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, 0, 1, 1, 1, 1, DateTimeKind.Utc)); // Day < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, 32, 1, 1, 1, 1, DateTimeKind.Utc)); // Day > days in month

            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, 1, -1, 1, 1, 1, DateTimeKind.Utc)); // Hour < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, 1, 24, 1, 1, 1, DateTimeKind.Utc)); // Hour > 23

            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, 1, 1, -1, 1, 1, DateTimeKind.Utc)); // Minute < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, 1, 1, 60, 1, 1, DateTimeKind.Utc)); // Minute > 59

            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, 1, 1, 1, -1, 1, DateTimeKind.Utc)); // Second < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, 1, 1, 1, 60, 1, DateTimeKind.Utc)); // Second > 59

            AssertExtensions.Throws<ArgumentOutOfRangeException>("millisecond", () => new DateTime(1, 1, 1, 1, 1, 1, -1, DateTimeKind.Utc)); // Millisecond < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("millisecond", () => new DateTime(1, 1, 1, 1, 1, 1, 1000, DateTimeKind.Utc)); // Millisecond > 999

            AssertExtensions.Throws<ArgumentException>("kind", () => new DateTime(1, 1, 1, 1, 1, 1, 1, DateTimeKind.Unspecified - 1)); // Invalid date time kind
            AssertExtensions.Throws<ArgumentException>("kind", () => new DateTime(1, 1, 1, 1, 1, 1, 1, DateTimeKind.Local + 1)); // Invalid date time kind
        }

        [Theory]
        [InlineData(2004, true)]
        [InlineData(2005, false)]
        public static void IsLeapYear(int year, bool expected)
        {
            Assert.Equal(expected, DateTime.IsLeapYear(year));
        }

        [Fact]
        public static void IsLeapYear_InvalidYear_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("year", () => DateTime.IsLeapYear(0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("year", () => DateTime.IsLeapYear(10000));
        }

        public static IEnumerable<object[]> Add_TimeSpan_TestData()
        {
            yield return new object[] { new DateTime(1000), new TimeSpan(10), new DateTime(1010) };
            yield return new object[] { new DateTime(1000), TimeSpan.Zero, new DateTime(1000) };
            yield return new object[] { new DateTime(1000), new TimeSpan(-10), new DateTime(990) };
        }

        [Theory]
        [MemberData(nameof(Add_TimeSpan_TestData))]
        public static void Add_TimeSpan(DateTime dateTime, TimeSpan timeSpan, DateTime expected)
        {
            Assert.Equal(expected, dateTime.Add(timeSpan));
        }

        [Fact]
        public static void Add_TimeSpan_NewDateOutOfRange_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => DateTime.MinValue.Add(TimeSpan.FromTicks(-1)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => DateTime.MaxValue.Add(TimeSpan.FromTicks(11)));
        }

        public static IEnumerable<object[]> AddYears_TestData()
        {
            yield return new object[] { new DateTime(1986, 8, 15, 10, 20, 5, 70), 10, new DateTime(1996, 8, 15, 10, 20, 5, 70) };
            yield return new object[] { new DateTime(1986, 8, 15, 10, 20, 5, 70), 0, new DateTime(1986, 8, 15, 10, 20, 5, 70) };
            yield return new object[] { new DateTime(1986, 8, 15, 10, 20, 5, 70), -10, new DateTime(1976, 8, 15, 10, 20, 5, 70) };
        }

        [Theory]
        [MemberData(nameof(AddYears_TestData))]
        public static void AddYears(DateTime dateTime, int years, DateTime expected)
        {
            Assert.Equal(expected, dateTime.AddYears(years));
        }

        [Fact]
        public static void AddYears_NewDateOutOfRange_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("years", () => DateTime.Now.AddYears(10001));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("years", () => DateTime.Now.AddYears(-10001));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("months", () => DateTime.MaxValue.AddYears(1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("months", () => DateTime.MinValue.AddYears(-1));
        }

        public static IEnumerable<object[]> AddMonths_TestData()
        {
            yield return new object[] { new DateTime(1986, 8, 15, 10, 20, 5, 70), 2, new DateTime(1986, 10, 15, 10, 20, 5, 70) };
            yield return new object[] { new DateTime(1986, 8, 15, 10, 20, 5, 70), 0, new DateTime(1986, 8, 15, 10, 20, 5, 70) };
            yield return new object[] { new DateTime(1986, 8, 15, 10, 20, 5, 70), -2, new DateTime(1986, 6, 15, 10, 20, 5, 70) };
        }

        [Theory]
        [MemberData(nameof(AddMonths_TestData))]
        public static void AddMonths(DateTime dateTime, int months, DateTime expected)
        {
            Assert.Equal(expected, dateTime.AddMonths(months));
        }

        [Fact]
        public static void AddMonths_NewDateOutOfRange_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("months", () => DateTime.Now.AddMonths(120001));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("months", () => DateTime.Now.AddMonths(-120001));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("months", () => DateTime.MaxValue.AddMonths(1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("months", () => DateTime.MinValue.AddMonths(-1));
        }

        public static IEnumerable<object[]> AddDays_TestData()
        {
            yield return new object[] { new DateTime(1986, 8, 15, 10, 20, 5, 70), 2, new DateTime(1986, 8, 17, 10, 20, 5, 70) };
            yield return new object[] { new DateTime(1986, 8, 15, 10, 20, 5, 70), 0, new DateTime(1986, 8, 15, 10, 20, 5, 70) };
            yield return new object[] { new DateTime(1986, 8, 15, 10, 20, 5, 70), -2, new DateTime(1986, 8, 13, 10, 20, 5, 70) };
        }

        [Theory]
        [MemberData(nameof(AddDays_TestData))]
        public static void AddDays(DateTime dateTime, double days, DateTime expected)
        {
            Assert.Equal(expected, dateTime.AddDays(days));
        }

        [Fact]
        public static void AddDays_NewDateOutOfRange_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => DateTime.MaxValue.AddDays(1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => DateTime.MinValue.AddDays(-1));
        }

        public static IEnumerable<object[]> AddHours_TestData()
        {
            yield return new object[] { new DateTime(1986, 8, 15, 10, 20, 5, 70), 3, new DateTime(1986, 8, 15, 13, 20, 5, 70) };
            yield return new object[] { new DateTime(1986, 8, 15, 10, 20, 5, 70), 0, new DateTime(1986, 8, 15, 10, 20, 5, 70) };
            yield return new object[] { new DateTime(1986, 8, 15, 10, 20, 5, 70), -3, new DateTime(1986, 8, 15, 7, 20, 5, 70) };
        }

        [Theory]
        [MemberData(nameof(AddHours_TestData))]
        public static void AddHours(DateTime dateTime, double hours, DateTime expected)
        {
            Assert.Equal(expected, dateTime.AddHours(hours));
        }

        [Fact]
        public static void AddHours_NewDateOutOfRange_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => DateTime.MaxValue.AddHours(1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => DateTime.MinValue.AddHours(-1));
        }

        public static IEnumerable<object[]> AddMinutes_TestData()
        {
            yield return new object[] { new DateTime(1986, 8, 15, 10, 20, 5, 70), 5, new DateTime(1986, 8, 15, 10, 25, 5, 70) };
            yield return new object[] { new DateTime(1986, 8, 15, 10, 20, 5, 70), 0, new DateTime(1986, 8, 15, 10, 20, 5, 70) };
            yield return new object[] { new DateTime(1986, 8, 15, 10, 20, 5, 70), -5, new DateTime(1986, 8, 15, 10, 15, 5, 70) };
        }

        [Theory]
        [MemberData(nameof(AddMinutes_TestData))]
        public static void AddMinutes(DateTime dateTime, double minutes, DateTime expected)
        {
            Assert.Equal(expected, dateTime.AddMinutes(minutes));
        }

        [Fact]
        public static void AddMinutes_NewDateOutOfRange_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => DateTime.MaxValue.AddMinutes(1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => DateTime.MinValue.AddMinutes(-1));
        }

        public static IEnumerable<object[]> AddSeconds_TestData()
        {
            yield return new object[] { new DateTime(1986, 8, 15, 10, 20, 5, 70), 30, new DateTime(1986, 8, 15, 10, 20, 35, 70) };
            yield return new object[] { new DateTime(1986, 8, 15, 10, 20, 5, 70), 0, new DateTime(1986, 8, 15, 10, 20, 5, 70) };
            yield return new object[] { new DateTime(1986, 8, 15, 10, 20, 5, 70), -3, new DateTime(1986, 8, 15, 10, 20, 2, 70) };
        }

        [Theory]
        [MemberData(nameof(AddSeconds_TestData))]
        public static void AddSeconds(DateTime dateTime, double seconds, DateTime expected)
        {
            Assert.Equal(expected, dateTime.AddSeconds(seconds));
        }

        [Fact]
        public static void AddSeconds_NewDateOutOfRange_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => DateTime.MaxValue.AddSeconds(1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => DateTime.MinValue.AddSeconds(-1));
        }

        public static IEnumerable<object[]> AddMilliseconds_TestData()
        {
            yield return new object[] { new DateTime(1986, 8, 15, 10, 20, 5, 70), 10, new DateTime(1986, 8, 15, 10, 20, 5, 80) };
            yield return new object[] { new DateTime(1986, 8, 15, 10, 20, 5, 70), 0, new DateTime(1986, 8, 15, 10, 20, 5, 70) };
            yield return new object[] { new DateTime(1986, 8, 15, 10, 20, 5, 70), -10, new DateTime(1986, 8, 15, 10, 20, 5, 60) };
        }

        [Theory]
        [MemberData(nameof(AddMilliseconds_TestData))]
        public static void AddMilliseconds(DateTime dateTime, double milliseconds, DateTime expected)
        {
            Assert.Equal(expected, dateTime.AddMilliseconds(milliseconds));
        }

        [Fact]
        public static void AddMilliseconds_NewDateOutOfRange_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => DateTime.MaxValue.AddMilliseconds(1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => DateTime.MinValue.AddMilliseconds(-1));
        }

        public static IEnumerable<object[]> AddTicks_TestData()
        {
            yield return new object[] { new DateTime(1000), 10, new DateTime(1010) };
            yield return new object[] { new DateTime(1000), 0, new DateTime(1000) };
            yield return new object[] { new DateTime(1000), -10, new DateTime(990) };
        }

        [Theory]
        [MemberData(nameof(AddTicks_TestData))]
        public static void AddTicks(DateTime dateTime, long ticks, DateTime expected)
        {
            Assert.Equal(expected, dateTime.AddTicks(ticks));
        }

        [Fact]
        public static void AddTicks_NewDateOutOfRange_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => DateTime.MaxValue.AddTicks(1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => DateTime.MinValue.AddTicks(-1));
        }

        [Fact]
        public static void DayOfWeekTest()
        {
            var dateTime = new DateTime(2012, 6, 18);
            Assert.Equal(DayOfWeek.Monday, dateTime.DayOfWeek);
        }

        [Fact]
        public static void DayOfYear()
        {
            var dateTime = new DateTime(2012, 6, 18);
            Assert.Equal(170, dateTime.DayOfYear);
        }

        [Fact]
        public static void TimeOfDay()
        {
            var dateTime = new DateTime(2012, 6, 18, 10, 5, 1, 0);
            TimeSpan ts = dateTime.TimeOfDay;

            DateTime newDate = dateTime.Subtract(ts);
            Assert.Equal(new DateTime(2012, 6, 18, 0, 0, 0, 0).Ticks, newDate.Ticks);
            Assert.Equal(dateTime.Ticks, newDate.Add(ts).Ticks);
        }

        [Fact]
        public static void Today()
        {
            DateTime today = DateTime.Today;
            DateTime now = DateTime.Now;
            VerifyDateTime(today, now.Year, now.Month, now.Day, 0, 0, 0, 0, DateTimeKind.Local);

            today = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, DateTimeKind.Utc);
            Assert.Equal(DateTimeKind.Utc, today.Kind);
            Assert.False(today.IsDaylightSavingTime());
        }

        [Fact]
        public static void Conversion()
        {
            DateTime today = DateTime.Today;
            long dateTimeRaw = today.ToBinary();
            Assert.Equal(today, DateTime.FromBinary(dateTimeRaw));

            dateTimeRaw = today.ToUniversalTime().ToBinary();
            Assert.Equal(today.ToUniversalTime(), DateTime.FromBinary(dateTimeRaw));

            dateTimeRaw = today.ToFileTime();
            Assert.Equal(today, DateTime.FromFileTime(dateTimeRaw));

            dateTimeRaw = today.ToFileTimeUtc();
            Assert.Equal(today, DateTime.FromFileTimeUtc(dateTimeRaw).ToLocalTime());
        }

        public static IEnumerable<object[]> Subtract_TimeSpan_TestData()
        {
            var dateTime = new DateTime(2012, 6, 18, 10, 5, 1, 0, DateTimeKind.Utc);

            yield return new object[] { dateTime, new TimeSpan(10, 5, 1), new DateTime(2012, 6, 18, 0, 0, 0, 0, DateTimeKind.Utc) };
            yield return new object[] { dateTime, new TimeSpan(-10, -5, -1), new DateTime(2012, 6, 18, 20, 10, 2, 0, DateTimeKind.Utc) };
        }

        [Theory]
        [MemberData(nameof(Subtract_TimeSpan_TestData))]
        public static void Subtract_TimeSpan(DateTime dateTime, TimeSpan timeSpan, DateTime expected)
        {
            Assert.Equal(expected, dateTime - timeSpan);
            Assert.Equal(expected, dateTime.Subtract(timeSpan));
        }

        public static IEnumerable<object[]> Subtract_DateTime_TestData()
        {
            var dateTime1 = new DateTime(1996, 6, 3, 22, 15, 0, DateTimeKind.Utc);
            var dateTime2 = new DateTime(1996, 12, 6, 13, 2, 0, DateTimeKind.Utc);
            var dateTime3 = new DateTime(1996, 10, 12, 8, 42, 0, DateTimeKind.Utc);

            yield return new object[] { dateTime2, dateTime1, new TimeSpan(185, 14, 47, 0) };
            yield return new object[] { dateTime1, dateTime2, new TimeSpan(-185, -14, -47, 0) };
            yield return new object[] { dateTime1, dateTime2, new TimeSpan(-185, -14, -47, 0) };
        }

        [Theory]
        [MemberData(nameof(Subtract_DateTime_TestData))]
        public static void Subtract_DateTime(DateTime dateTime1, DateTime dateTime2, TimeSpan expected)
        {
            Assert.Equal(expected, dateTime1 - dateTime2);
            Assert.Equal(expected, dateTime1.Subtract(dateTime2));
        }

        [Fact]
        public static void Subtract_DateTime_Invalid()
        {
            DateTime date1 = DateTime.MinValue.ToLocalTime();
            Assert.Throws<ArgumentOutOfRangeException>(() => date1.Subtract(new TimeSpan(365, 0, 0, 0)));

            DateTime date2 = DateTime.MaxValue.ToLocalTime();
            Assert.Throws<ArgumentOutOfRangeException>(() => date2.Subtract(new TimeSpan(-365, 0, 0, 0)));
        }

        [Fact]
        public static void Parse_String()
        {
            DateTime expected = DateTime.MaxValue;
            string expectedString = expected.ToString();

            DateTime result = DateTime.Parse(expectedString);
            Assert.Equal(expectedString, result.ToString());
        }

        [Fact]
        public static void Parse_String_FormatProvider()
        {
            DateTime expected = DateTime.MaxValue;
            string expectedString = expected.ToString();

            DateTime result = DateTime.Parse(expectedString, null);
            Assert.Equal(expectedString, result.ToString());
        }

        [Fact]
        public static void Parse_String_FormatProvider_DateTimeStyles()
        {
            DateTime expected = DateTime.MaxValue;
            string expectedString = expected.ToString();

            DateTime result = DateTime.Parse(expectedString, null, DateTimeStyles.None);
            Assert.Equal(expectedString, result.ToString());
        }

        [Fact]
        public static void Parse_Japanese()
        {
            var expected = new DateTime(2012, 12, 21, 10, 8, 6);
            var cultureInfo = new CultureInfo("ja-JP");

            string expectedString = string.Format(cultureInfo, "{0}", expected);
            Assert.Equal(expected, DateTime.Parse(expectedString, cultureInfo));
        }

        [Fact]
        public static void Parse_InvalidArguments_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("s", () => DateTime.Parse(null));
            AssertExtensions.Throws<ArgumentNullException>("s", () => DateTime.Parse(null, new MyFormatter()));
            AssertExtensions.Throws<ArgumentNullException>("s", () => DateTime.Parse((string)null, new MyFormatter(), DateTimeStyles.NoCurrentDateDefault));

            Assert.Throws<FormatException>(() => DateTime.Parse(""));
            Assert.Throws<FormatException>(() => DateTime.Parse("", new MyFormatter()));
            Assert.Throws<FormatException>(() => DateTime.Parse("", new MyFormatter(), DateTimeStyles.NoCurrentDateDefault));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public static void TryParse_NullOrEmptyString_ReturnsFalse(string input)
        {
            Assert.False(DateTime.TryParse(input, out DateTime result));
            Assert.False(DateTime.TryParse(input, new MyFormatter(), DateTimeStyles.None, out result));
        }

        [Fact]
        public static void ParseExact_InvalidArguments_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("s", () => DateTime.ParseExact(null, "d", new MyFormatter()));
            AssertExtensions.Throws<ArgumentNullException>("s", () => DateTime.ParseExact((string)null, "d", new MyFormatter(), DateTimeStyles.None));
            AssertExtensions.Throws<ArgumentNullException>("s", () => DateTime.ParseExact((string)null, new[] { "d" }, new MyFormatter(), DateTimeStyles.NoCurrentDateDefault));

            Assert.Throws<FormatException>(() => DateTime.ParseExact("", "d", new MyFormatter()));
            Assert.Throws<FormatException>(() => DateTime.ParseExact("", "d", new MyFormatter(), DateTimeStyles.None));
            Assert.Throws<FormatException>(() => DateTime.ParseExact("", new[] { "d" }, new MyFormatter(), DateTimeStyles.NoCurrentDateDefault));

            AssertExtensions.Throws<ArgumentNullException>("format", () => DateTime.ParseExact("123", null, new MyFormatter()));
            AssertExtensions.Throws<ArgumentNullException>("format", () => DateTime.ParseExact("123", (string)null, new MyFormatter(), DateTimeStyles.None));
            AssertExtensions.Throws<ArgumentNullException>("formats", () => DateTime.ParseExact("123", (string[])null, new MyFormatter(), DateTimeStyles.NoCurrentDateDefault));

            Assert.Throws<FormatException>(() => DateTime.ParseExact("123", "", new MyFormatter()));
            Assert.Throws<FormatException>(() => DateTime.ParseExact("123", "", new MyFormatter(), DateTimeStyles.None));
            Assert.Throws<FormatException>(() => DateTime.ParseExact("123", new string[0], new MyFormatter(), DateTimeStyles.NoCurrentDateDefault));
            Assert.Throws<FormatException>(() => DateTime.ParseExact("123", new string[] { null }, new MyFormatter(), DateTimeStyles.NoCurrentDateDefault));
            Assert.Throws<FormatException>(() => DateTime.ParseExact("123", new[] { "" }, new MyFormatter(), DateTimeStyles.NoCurrentDateDefault));
        }

        [Fact]
        public static void TryParseExact_InvalidArguments_ReturnsFalse()
        {
            Assert.False(DateTime.TryParseExact((string)null, "d", new MyFormatter(), DateTimeStyles.None, out DateTime result));
            Assert.False(DateTime.TryParseExact((string)null, new[] { "d" }, new MyFormatter(), DateTimeStyles.None, out result));

            Assert.False(DateTime.TryParseExact("", "d", new MyFormatter(), DateTimeStyles.None, out result));
            Assert.False(DateTime.TryParseExact("", new[] { "d" }, new MyFormatter(), DateTimeStyles.None, out result));

            Assert.False(DateTime.TryParseExact("abc", (string)null, new MyFormatter(), DateTimeStyles.None, out result));
            Assert.False(DateTime.TryParseExact("abc", (string[])null, new MyFormatter(), DateTimeStyles.None, out result));

            Assert.False(DateTime.TryParseExact("abc", "", new MyFormatter(), DateTimeStyles.None, out result));
            Assert.False(DateTime.TryParseExact("abc", new string[0], new MyFormatter(), DateTimeStyles.None, out result));
            Assert.False(DateTime.TryParseExact("abc", new string[] { null }, new MyFormatter(), DateTimeStyles.None, out result));
            Assert.False(DateTime.TryParseExact("abc", new[] { "" }, new MyFormatter(), DateTimeStyles.None, out result));
            Assert.False(DateTime.TryParseExact("abc", new[] { "" }, new MyFormatter(), DateTimeStyles.None, out result));
        }

        [Fact]
        public static void TryParse_String()
        {
            DateTime expected = DateTime.MaxValue;
            string expectedString = expected.ToString("g");

            DateTime result;
            Assert.True(DateTime.TryParse(expectedString, out result));
            Assert.Equal(expectedString, result.ToString("g"));
        }

        [Fact]
        public static void TryParse_String_FormatProvider_DateTimeStyles_U()
        {
            DateTime expected = DateTime.MaxValue;
            string expectedString = expected.ToString("u");

            DateTime result;
            Assert.True(DateTime.TryParse(expectedString, null, DateTimeStyles.AdjustToUniversal, out result));
            Assert.Equal(expectedString, result.ToString("u"));
        }

        [Fact]
        public static void TryParse_String_FormatProvider_DateTimeStyles_G()
        {
            DateTime expected = DateTime.MaxValue;
            string expectedString = expected.ToString("g");

            DateTime result;
            Assert.True(DateTime.TryParse(expectedString, null, DateTimeStyles.AdjustToUniversal, out result));
            Assert.Equal(expectedString, result.ToString("g"));
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "The full .NET framework has a bug and incorrectly parses this date")]
        public static void TryParse_TimeDesignators_NetCore()
        {
            DateTime result;
            Assert.True(DateTime.TryParse("4/21 5am", new CultureInfo("en-US"), DateTimeStyles.None, out result));
            Assert.Equal(4, result.Month);
            Assert.Equal(21, result.Day);
            Assert.Equal(5, result.Hour);

            Assert.True(DateTime.TryParse("4/21 5pm", new CultureInfo("en-US"), DateTimeStyles.None, out result));
            Assert.Equal(4, result.Month);
            Assert.Equal(21, result.Day);
            Assert.Equal(17, result.Hour);
        }

        [Fact]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework, "The coreclr fixed a bug where the .NET framework incorrectly parses this date")]
        public static void TryParse_TimeDesignators_Netfx()
        {
            DateTime result;
            Assert.True(DateTime.TryParse("4/21 5am", new CultureInfo("en-US"), DateTimeStyles.None, out result));
            Assert.Equal(DateTime.Now.Month, result.Month);
            Assert.Equal(DateTime.Now.Day, result.Day);
            Assert.Equal(4, result.Hour);
            Assert.Equal(0, result.Minute);
            Assert.Equal(0, result.Second);

            Assert.True(DateTime.TryParse("4/21 5pm", new CultureInfo("en-US"), DateTimeStyles.None, out result));
            Assert.Equal(DateTime.Now.Month, result.Month);
            Assert.Equal(DateTime.Now.Day, result.Day);
            Assert.Equal(16, result.Hour);
            Assert.Equal(0, result.Minute);
            Assert.Equal(0, result.Second);
        }

        public static IEnumerable<object[]> StandardFormatSpecifiers()
        {
            yield return new object[] { "d" };
            yield return new object[] { "D" };
            yield return new object[] { "f" };
            yield return new object[] { "F" };
            yield return new object[] { "g" };
            yield return new object[] { "G" };
            yield return new object[] { "m" };
            yield return new object[] { "M" };
            yield return new object[] { "o" };
            yield return new object[] { "O" };
            yield return new object[] { "r" };
            yield return new object[] { "R" };
            yield return new object[] { "s" };
            yield return new object[] { "t" };
            yield return new object[] { "T" };
            yield return new object[] { "u" };
            yield return new object[] { "U" };
            yield return new object[] { "y" };
            yield return new object[] { "Y" };
        }

        [Theory]
        [MemberData(nameof(StandardFormatSpecifiers))]
        public static void ParseExact_ToStringThenParseExactRoundtrip_Success(string standardFormat)
        {
            DateTime dt = DateTime.Now;
            string expected = dt.ToString(standardFormat);

            Assert.Equal(expected, DateTime.ParseExact(expected, standardFormat, null).ToString(standardFormat));
            Assert.Equal(expected, DateTime.ParseExact(expected, standardFormat, null, DateTimeStyles.None).ToString(standardFormat));
            Assert.Equal(expected, DateTime.ParseExact(expected, new[] { standardFormat }, null, DateTimeStyles.None).ToString(standardFormat));

            Assert.True(DateTime.TryParseExact(expected, standardFormat, null, DateTimeStyles.None, out DateTime actual));
            Assert.Equal(expected, actual.ToString(standardFormat));
            Assert.True(DateTime.TryParseExact(expected, new[] { standardFormat }, null, DateTimeStyles.None, out actual));
            Assert.Equal(expected, actual.ToString(standardFormat));

            // Should also parse with Parse, though may not round trip exactly
            DateTime.Parse(expected);
        }

        public static IEnumerable<object[]> InvalidFormatSpecifierRoundtripPairs()
        {
            yield return new object[] { "d", "f" };
            yield return new object[] { "o", "r" };
            yield return new object[] { "u", "y" };
        }

        [Theory]
        [MemberData(nameof(InvalidFormatSpecifierRoundtripPairs))]
        public static void ParseExact_ToStringThenParseExact_RoundtripWithOtherFormat_Fails(string toStringFormat, string parseFormat)
        {
            DateTime dt = DateTime.Now;
            string expected = dt.ToString(toStringFormat);

            Assert.Throws<FormatException>(() => DateTime.ParseExact(expected, parseFormat, null));
            Assert.Throws<FormatException>(() => DateTime.ParseExact(expected, parseFormat, null, DateTimeStyles.None));
            Assert.Throws<FormatException>(() => DateTime.ParseExact(expected, new[] { parseFormat }, null, DateTimeStyles.None));

            Assert.False(DateTime.TryParseExact(expected, parseFormat, null, DateTimeStyles.None, out DateTime result));
            Assert.False(DateTime.TryParseExact(expected, new[] { parseFormat }, null, DateTimeStyles.None, out result));
        }

        public static IEnumerable<object[]> Format_String_TestData_O()
        {
            yield return new object[] { DateTime.MaxValue, "9999-12-31T23:59:59.9999999" };
            yield return new object[] { DateTime.MinValue, "0001-01-01T00:00:00.0000000" };
            yield return new object[] { new DateTime(1906, 8, 15, 7, 24, 5, 300), "1906-08-15T07:24:05.3000000" };
        }

        [Theory] 
        [MemberData(nameof(Format_String_TestData_R))]
        public static void ParseExact_String_String_FormatProvider_DateTimeStyles_R(DateTime dt, string expected)
        {
            string actual = dt.ToString("r");
            Assert.Equal(expected, actual);

            DateTime result = DateTime.ParseExact(actual, "r", null, DateTimeStyles.None);
            Assert.Equal(expected, result.ToString("r"));
        }

        public static IEnumerable<object[]> Format_String_TestData_R()
        {
            yield return new object[] { DateTime.MaxValue, "Fri, 31 Dec 9999 23:59:59 GMT" };
            yield return new object[] { DateTime.MinValue, "Mon, 01 Jan 0001 00:00:00 GMT" };
            yield return new object[] { new DateTime(1906, 8, 15, 7, 24, 5, 300), "Wed, 15 Aug 1906 07:24:05 GMT" };
        }

        [Fact]
        public static void ParseExact_String_String_FormatProvider_DateTimeStyles_CustomFormatProvider()
        {
            var formatter = new MyFormatter();
            string dateBefore = DateTime.Now.ToString();

            DateTime dateAfter = DateTime.ParseExact(dateBefore, "G", formatter, DateTimeStyles.AdjustToUniversal);
            Assert.Equal(dateBefore, dateAfter.ToString());
        }

        [Fact]
        public static void ParseExact_String_StringArray_FormatProvider_DateTimeStyles()
        {
            DateTime expected = DateTime.MaxValue;
            string expectedString = expected.ToString("g");

            var formats = new string[] { "g" };
            DateTime result = DateTime.ParseExact(expectedString, formats, null, DateTimeStyles.AdjustToUniversal);
            Assert.Equal(expectedString, result.ToString("g"));
        }

        [Fact]
        public static void TryParseExact_String_String_FormatProvider_DateTimeStyles_NullFormatProvider()
        {
            DateTime expected = DateTime.MaxValue;
            string expectedString = expected.ToString("g");

            DateTime resulted;
            Assert.True(DateTime.TryParseExact(expectedString, "g", null, DateTimeStyles.AdjustToUniversal, out resulted));
            Assert.Equal(expectedString, resulted.ToString("g"));
        }

        [Fact]
        public static void TryParseExact_String_StringArray_FormatProvider_DateTimeStyles()
        {
            DateTime expected = DateTime.MaxValue;
            string expectedString = expected.ToString("g");

            var formats = new string[] { "g" };
            DateTime result;
            Assert.True(DateTime.TryParseExact(expectedString, formats, null, DateTimeStyles.AdjustToUniversal, out result));
            Assert.Equal(expectedString, result.ToString("g"));
        }

        public static void ParseExact_EscapedSingleQuotes()
        {
            var formatInfo = DateTimeFormatInfo.GetInstance(new CultureInfo("mt-MT"));
            const string format = @"dddd, d' ta\' 'MMMM yyyy";

            DateTime expected = new DateTime(1999, 2, 28, 17, 00, 01);
            string formatted = expected.ToString(format, formatInfo);
            DateTime actual = DateTime.ParseExact(formatted, format, formatInfo);

            Assert.Equal(expected.Date, actual.Date);
        }

        [Theory]
        [InlineData("fi-FI")]
        [InlineData("nb-NO")]
        [InlineData("nb-SJ")]
        [InlineData("sr-Cyrl-XK")]
        [InlineData("sr-Latn-ME")]
        [InlineData("sr-Latn-RS")]
        [InlineData("sr-Latn-XK")]
        public static void Parse_SpecialCultures(string cultureName)
        {
            // Test DateTime parsing with cultures which has the date separator and time separator are same
            CultureInfo cultureInfo;
            try
            {
                cultureInfo = new CultureInfo(cultureName);
            }
            catch (CultureNotFoundException)
            {
                // Ignore un-supported culture in current platform
                return;
            }

            var dateTime = new DateTime(2015, 11, 20, 11, 49, 50);
            string dateString = dateTime.ToString(cultureInfo.DateTimeFormat.ShortDatePattern, cultureInfo);

            DateTime parsedDate;
            Assert.True(DateTime.TryParse(dateString, cultureInfo, DateTimeStyles.None, out parsedDate));
            if (cultureInfo.DateTimeFormat.ShortDatePattern.Contains("yyyy") || HasDifferentDateTimeSeparators(cultureInfo.DateTimeFormat))
            {
                Assert.Equal(dateTime.Date, parsedDate);
            }
            else
            {
                // When the date separator and time separator are the same, DateTime.TryParse cannot 
                // tell the difference between a short date like dd.MM.yy and a short time
                // like HH.mm.ss. So it assumes that if it gets 03.04.11, that must be a time
                // and uses the current date to construct the date time.
                DateTime now = DateTime.Now;
                Assert.Equal(new DateTime(now.Year, now.Month, now.Day, dateTime.Day, dateTime.Month, dateTime.Year % 100), parsedDate);
            }

            dateString = dateTime.ToString(cultureInfo.DateTimeFormat.LongDatePattern, cultureInfo);
            Assert.True(DateTime.TryParse(dateString, cultureInfo, DateTimeStyles.None, out parsedDate));
            Assert.Equal(dateTime.Date, parsedDate);

            dateString = dateTime.ToString(cultureInfo.DateTimeFormat.FullDateTimePattern, cultureInfo);
            Assert.True(DateTime.TryParse(dateString, cultureInfo, DateTimeStyles.None, out parsedDate));
            Assert.Equal(dateTime, parsedDate);

            dateString = dateTime.ToString(cultureInfo.DateTimeFormat.LongTimePattern, cultureInfo);
            Assert.True(DateTime.TryParse(dateString, cultureInfo, DateTimeStyles.None, out parsedDate));
            Assert.Equal(dateTime.TimeOfDay, parsedDate.TimeOfDay);
        }

        private static bool HasDifferentDateTimeSeparators(DateTimeFormatInfo dateTimeFormat)
        {
            // Since .NET Core doesn't expose DateTimeFormatInfo DateSeparator and TimeSeparator properties,
            // this method gets the separators using DateTime.ToString by passing in the invariant separators.
            // The invariant separators will then get turned into the culture's separators by ToString,
            // which are then compared.

            var dateTime = new DateTime(2015, 11, 24, 17, 57, 29);
            string separators = dateTime.ToString("/@:", dateTimeFormat);

            int delimiterIndex = separators.IndexOf('@');
            string dateSeparator = separators.Substring(0, delimiterIndex);
            string timeSeparator = separators.Substring(delimiterIndex + 1);
            return dateSeparator != timeSeparator;
        }

        [Fact]
        public static void GetDateTimeFormats()
        {
            var allStandardFormats = new char[]
            {
            'd', 'D', 'f', 'F', 'g', 'G',
            'm', 'M', 'o', 'O', 'r', 'R',
            's', 't', 'T', 'u', 'U', 'y', 'Y',
            };

            var dateTime = new DateTime(2009, 7, 28, 5, 23, 15);
            var formats = new List<string>();

            foreach (char format in allStandardFormats)
            {
                string[] dates = dateTime.GetDateTimeFormats(format);

                Assert.True(dates.Length > 0);

                DateTime parsedDate;
                Assert.True(DateTime.TryParseExact(dates[0], format.ToString(), CultureInfo.CurrentCulture, DateTimeStyles.None, out parsedDate));

                formats.AddRange(dates);
            }

            List<string> actualFormats = dateTime.GetDateTimeFormats().ToList();
            Assert.Equal(formats.OrderBy(t => t), actualFormats.OrderBy(t => t));

            actualFormats = dateTime.GetDateTimeFormats(CultureInfo.CurrentCulture).ToList();
            Assert.Equal(formats.OrderBy(t => t), actualFormats.OrderBy(t => t));
        }

        [Fact]
        public static void GetDateTimeFormats_FormatSpecifier_InvalidFormat()
        {
            var dateTime = new DateTime(2009, 7, 28, 5, 23, 15);
            Assert.Throws<FormatException>(() => dateTime.GetDateTimeFormats('x')); // No such format
        }

        private static void VerifyDateTime(DateTime dateTime, int year, int month, int day, int hour, int minute, int second, int millisecond, DateTimeKind kind)
        {
            Assert.Equal(year, dateTime.Year);
            Assert.Equal(month, dateTime.Month);
            Assert.Equal(day, dateTime.Day);
            Assert.Equal(hour, dateTime.Hour);
            Assert.Equal(minute, dateTime.Minute);
            Assert.Equal(second, dateTime.Second);
            Assert.Equal(millisecond, dateTime.Millisecond);

            Assert.Equal(kind, dateTime.Kind);
        }

        private class MyFormatter : IFormatProvider
        {
            public object GetFormat(Type formatType)
            {
                return typeof(IFormatProvider) == formatType ? this : null;
            }
        }

        [Fact]
        public static void InvalidDateTimeStyles()
        {
            string strDateTime = "Thursday, August 31, 2006 1:14";
            string[] formats = new string[] { "f" };
            IFormatProvider provider = new CultureInfo("en-US");
            DateTimeStyles style = DateTimeStyles.AssumeLocal | DateTimeStyles.AssumeUniversal;
            AssertExtensions.Throws<ArgumentException>("style", () => DateTime.ParseExact(strDateTime, formats, provider, style));
        }

        public static IEnumerable<object[]> Parse_ValidInput_Suceeds_MemberData()
        {
            yield return new object[] { "1234 12", CultureInfo.InvariantCulture, new DateTime(1234, 12, 1, 0, 0, 0) };
            yield return new object[] { "12 1234", CultureInfo.InvariantCulture, new DateTime(1234, 12, 1, 0, 0, 0) };
            yield return new object[] { "12 1234 11", CultureInfo.InvariantCulture, new DateTime(1234, 12, 11, 0, 0, 0) };
            yield return new object[] { "1234 12 13", CultureInfo.InvariantCulture, new DateTime(1234, 12, 13, 0, 0, 0) };
            yield return new object[] { "12 13 1234", CultureInfo.InvariantCulture, new DateTime(1234, 12, 13, 0, 0, 0) };
            yield return new object[] { "1 1 1", CultureInfo.InvariantCulture, new DateTime(2001, 1, 1, 0, 0, 0) };
            yield return new object[] { "2 2 2Z", CultureInfo.InvariantCulture, TimeZoneInfo.ConvertTimeFromUtc(new DateTime(2002, 2, 2, 0, 0, 0, DateTimeKind.Utc), TimeZoneInfo.Local) };
            yield return new object[] { "#10/10/2095#\0", CultureInfo.InvariantCulture, new DateTime(2095, 10, 10, 0, 0, 0) };

            DateTime today = DateTime.Today;
            var hebrewCulture = new CultureInfo("he-IL");
            hebrewCulture.DateTimeFormat.Calendar = new HebrewCalendar();
            yield return new object[] { today.ToString(hebrewCulture), hebrewCulture, today };

            var mongolianCulture = new CultureInfo("mn-MN");
            yield return new object[] { today.ToString(mongolianCulture), mongolianCulture, today };
        }

        [Theory]
        [MemberData(nameof(Parse_ValidInput_Suceeds_MemberData))]
        public static void Parse_ValidInput_Suceeds(string input, CultureInfo culture, DateTime? expected)
        {
            Assert.Equal(expected, DateTime.Parse(input, culture));
        }

        public static IEnumerable<object[]> ParseExact_ValidInput_Succeeds_MemberData()
        {
            yield return new object[] { "9", "%d", CultureInfo.InvariantCulture, DateTimeStyles.None, new DateTime(DateTime.Now.Year, 1, 9, 0, 0, 0) };
            yield return new object[] { "15", "dd", CultureInfo.InvariantCulture, DateTimeStyles.None, new DateTime(DateTime.Now.Year, 1, 15, 0, 0, 0) };

            yield return new object[] { "9", "%M", CultureInfo.InvariantCulture, DateTimeStyles.None, new DateTime(DateTime.Now.Year, 9, 1, 0, 0, 0) };
            yield return new object[] { "09", "MM", CultureInfo.InvariantCulture, DateTimeStyles.None, new DateTime(DateTime.Now.Year, 9, 1, 0, 0, 0) };
            yield return new object[] { "Sep", "MMM", CultureInfo.InvariantCulture, DateTimeStyles.None, new DateTime(DateTime.Now.Year, 9, 1, 0, 0, 0) };
            yield return new object[] { "September", "MMMM", CultureInfo.InvariantCulture, DateTimeStyles.None, new DateTime(DateTime.Now.Year, 9, 1, 0, 0, 0) };

            yield return new object[] { "1", "%y", CultureInfo.InvariantCulture, DateTimeStyles.None, new DateTime(2001, 1, 1, 0, 0, 0) };
            yield return new object[] { "01", "yy", CultureInfo.InvariantCulture, DateTimeStyles.None, new DateTime(2001, 1, 1, 0, 0, 0) };
            yield return new object[] { "2001", "yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, new DateTime(2001, 1, 1, 0, 0, 0) };

            yield return new object[] { "3", "%H", CultureInfo.InvariantCulture, DateTimeStyles.None, DateTime.Today + TimeSpan.FromHours(3) };
            yield return new object[] { "03", "HH", CultureInfo.InvariantCulture, DateTimeStyles.None, DateTime.Today + TimeSpan.FromHours(3) };

            yield return new object[] { "3A", "ht", CultureInfo.InvariantCulture, DateTimeStyles.None, DateTime.Today + TimeSpan.FromHours(3) };
            yield return new object[] { "03A", "hht", CultureInfo.InvariantCulture, DateTimeStyles.None, DateTime.Today + TimeSpan.FromHours(3) };
            yield return new object[] { "3P", "ht", CultureInfo.InvariantCulture, DateTimeStyles.None, DateTime.Today + TimeSpan.FromHours(12 + 3) };
            yield return new object[] { "03P", "hht", CultureInfo.InvariantCulture, DateTimeStyles.None, DateTime.Today + TimeSpan.FromHours(12 + 3) };

            yield return new object[] { "2017-10-11 01:23:45Z", "u", CultureInfo.InvariantCulture, DateTimeStyles.None, new DateTime(2017, 10, 11, 1, 23, 45) };
            yield return new object[] { "9/8/2017 10:11:12 AM", "M/d/yyyy HH':'mm':'ss tt", CultureInfo.InvariantCulture, DateTimeStyles.None, new DateTime(2017, 9, 8, 10, 11, 12) };
            yield return new object[] { "9/8/2017 20:11:12 PM", "M/d/yyyy HH':'mm':'ss tt", CultureInfo.InvariantCulture, DateTimeStyles.None, new DateTime(2017, 9, 8, 20, 11, 12) };
            yield return new object[] { "Fri, 08 Sep 2017 11:18:19 -0000", "ddd, d MMM yyyy H:m:s zzz", new CultureInfo("en-US"), DateTimeStyles.AllowInnerWhite, new DateTime(2017, 9, 8, 11, 18, 19, DateTimeKind.Utc) };
            yield return new object[] { "1234-05-06T07:00:00.8Z", "yyyy-MM-dd'T'HH:mm:ss.FFF'Z'", CultureInfo.InvariantCulture, DateTimeStyles.None, new DateTime(1234, 5, 6, 7, 0, 0, 800) };
            yield return new object[] { "1234-05-06T07:00:00Z", "yyyy-MM-dd'T'HH:mm:ss.FFF'Z'", CultureInfo.InvariantCulture, DateTimeStyles.None, new DateTime(1234, 5, 6, 7, 0, 0, 0) };
            yield return new object[] { "1234-05-06T07:00:00Z", "yyyy-MM-dd'T'HH:mm:ssFFF'Z'", CultureInfo.InvariantCulture, DateTimeStyles.None, new DateTime(1234, 5, 6, 7, 0, 0, 0) };
            yield return new object[] { "1234-05-06T07:00:00Z", "yyyy-MM-dd'T'HH:mm:ssFFF'Z'", CultureInfo.InvariantCulture, DateTimeStyles.None, new DateTime(1234, 5, 6, 7, 0, 0, 0) };
            yield return new object[] { "1234-05-06T07:00:00Z", "yyyy-MM-dd'T'HH:mm:ssFFFZ", CultureInfo.InvariantCulture, DateTimeStyles.None, TimeZoneInfo.ConvertTimeFromUtc(new DateTime(1234, 5, 6, 7, 0, 0, DateTimeKind.Utc), TimeZoneInfo.Local) };
            yield return new object[] { "1234-05-06T07:00:00GMT", "yyyy-MM-dd'T'HH:mm:ssFFFZ", CultureInfo.InvariantCulture, DateTimeStyles.None, TimeZoneInfo.ConvertTimeFromUtc(new DateTime(1234, 5, 6, 7, 0, 0, DateTimeKind.Utc), TimeZoneInfo.Local) };

            var hebrewCulture = new CultureInfo("he-IL");
            hebrewCulture.DateTimeFormat.Calendar = new HebrewCalendar();
            DateTime today = DateTime.Today;
            foreach (string pattern in hebrewCulture.DateTimeFormat.GetAllDateTimePatterns())
            {
                yield return new object[] { today.ToString(pattern, hebrewCulture), pattern, hebrewCulture, DateTimeStyles.None, null };
            }
        }

        [Theory]
        [MemberData(nameof(ParseExact_ValidInput_Succeeds_MemberData))]
        public static void ParseExact_ValidInput_Succeeds(string input, string format, CultureInfo culture, DateTimeStyles style, DateTime? expected)
        {
            DateTime result1 = DateTime.ParseExact(input, format, culture, style);
            DateTime result2 = DateTime.ParseExact(input, new[] { format }, culture, style);

            Assert.True(DateTime.TryParseExact(input, format, culture, style, out DateTime result3));
            Assert.True(DateTime.TryParseExact(input, new[] { format }, culture, style, out DateTime result4));

            Assert.Equal(result1, result2);
            Assert.Equal(result1, result3);
            Assert.Equal(result1, result4);

            if (expected != null) // some inputs don't roundtrip well
            {
                // Normalize values to make comparison easier
                if (expected.Value.Kind != DateTimeKind.Utc)
                {
                    expected = expected.Value.ToUniversalTime();
                }
                if (result1.Kind != DateTimeKind.Utc)
                {
                    result1 = result1.ToUniversalTime();
                }

                Assert.Equal(expected, result1);
            }
        }

        public static IEnumerable<object[]> ParseExact_InvalidInputs_Fail_MemberData()
        {
            yield return new object[] { "6/28/2004 13:00:00 AM", "M/d/yyyy HH':'mm':'ss tt", CultureInfo.InvariantCulture, DateTimeStyles.None };
            yield return new object[] { "6/28/2004 03:00:00 PM", "M/d/yyyy HH':'mm':'ss tt", CultureInfo.InvariantCulture, DateTimeStyles.None };

            yield return new object[] { "1", "dd", CultureInfo.InvariantCulture, DateTimeStyles.None };
            yield return new object[] { "99", "dd", CultureInfo.InvariantCulture, DateTimeStyles.None };
            yield return new object[] { "123", "dd", CultureInfo.InvariantCulture, DateTimeStyles.None };

            yield return new object[] { "1", "mm", CultureInfo.InvariantCulture, DateTimeStyles.None };
            yield return new object[] { "99", "mm", CultureInfo.InvariantCulture, DateTimeStyles.None };
            yield return new object[] { "123", "mm", CultureInfo.InvariantCulture, DateTimeStyles.None };

            yield return new object[] { "1", "ss", CultureInfo.InvariantCulture, DateTimeStyles.None };
            yield return new object[] { "99", "ss", CultureInfo.InvariantCulture, DateTimeStyles.None };
            yield return new object[] { "123", "ss", CultureInfo.InvariantCulture, DateTimeStyles.None };

            yield return new object[] { "1", "MM", CultureInfo.InvariantCulture, DateTimeStyles.None };
            yield return new object[] { "99", "MM", CultureInfo.InvariantCulture, DateTimeStyles.None };
            yield return new object[] { "Fep", "MMM", CultureInfo.InvariantCulture, DateTimeStyles.None };
            yield return new object[] { "Jantember", "MMMM", CultureInfo.InvariantCulture, DateTimeStyles.None };

            yield return new object[] { "123", "YY", CultureInfo.InvariantCulture, DateTimeStyles.None };
            yield return new object[] { "12345", "YYYY", CultureInfo.InvariantCulture, DateTimeStyles.None };

            yield return new object[] { "1", "HH", CultureInfo.InvariantCulture, DateTimeStyles.None };
            yield return new object[] { "99", "HH", CultureInfo.InvariantCulture, DateTimeStyles.None };
            yield return new object[] { "123", "HH", CultureInfo.InvariantCulture, DateTimeStyles.None };

            yield return new object[] { "1", "hh", CultureInfo.InvariantCulture, DateTimeStyles.None };
            yield return new object[] { "99", "hh", CultureInfo.InvariantCulture, DateTimeStyles.None };
            yield return new object[] { "123", "hh", CultureInfo.InvariantCulture, DateTimeStyles.None };

            yield return new object[] { "1", "ff", CultureInfo.InvariantCulture, DateTimeStyles.None };
            yield return new object[] { "123", "ff", CultureInfo.InvariantCulture, DateTimeStyles.None };
            yield return new object[] { "123456", "fffff", CultureInfo.InvariantCulture, DateTimeStyles.None };
            yield return new object[] { "1234", "fffff", CultureInfo.InvariantCulture, DateTimeStyles.None };

            yield return new object[] { "AM", "t", CultureInfo.InvariantCulture, DateTimeStyles.None };
            yield return new object[] { "PM", "t", CultureInfo.InvariantCulture, DateTimeStyles.None };
            yield return new object[] { "PM", "ttt", CultureInfo.InvariantCulture, DateTimeStyles.None };
            yield return new object[] { "AAM", "tt", CultureInfo.InvariantCulture, DateTimeStyles.None };
            yield return new object[] { "CM", "tt", CultureInfo.InvariantCulture, DateTimeStyles.None };
        }

        [Theory]
        [MemberData(nameof(ParseExact_InvalidInputs_Fail_MemberData))]
        public static void ParseExact_InvalidInputs_Fail(string input, string format, CultureInfo culture, DateTimeStyles style)
        {
            Assert.Throws<FormatException>(() => DateTime.ParseExact(input, format, culture, style));
            Assert.Throws<FormatException>(() => DateTime.ParseExact(input, new[] { format }, culture, style));

            Assert.False(DateTime.TryParseExact(input, format, culture, style, out DateTime result));
            Assert.False(DateTime.TryParseExact(input, new[] { format }, culture, style, out result));
        }
    }
}
