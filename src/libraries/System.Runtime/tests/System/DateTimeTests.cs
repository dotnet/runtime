// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Tests
{
    public class DateTimeTests
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
        public static void Ctor_Long_DateTimeKind()
        {
            VerifyDateTime(new DateTime(999999999999999999, DateTimeKind.Utc), 3169, 11, 16, 9, 46, 39, 999, DateTimeKind.Utc);
        }

        public static IEnumerable<object[]> Ctor_InvalidTicks_TestData()
        {
            yield return new object[] { DateTime.MinValue.Ticks - 1 };
            yield return new object[] { DateTime.MaxValue.Ticks + 1 };
        }

        [Theory]
        [MemberData(nameof(Ctor_InvalidTicks_TestData))]
        public void Ctor_InvalidTicks_ThrowsArgumentOutOfRangeException(long ticks)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("ticks", () => new DateTime(ticks));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("ticks", () => new DateTime(ticks, DateTimeKind.Utc));
        }

        [Fact]
        public void Ctor_Int_Int_Int()
        {
            var dateTime = new DateTime(2012, 6, 11);
            VerifyDateTime(dateTime, 2012, 6, 11, 0, 0, 0, 0, DateTimeKind.Unspecified);
        }

        [Fact]
        public void Ctor_Int_Int_Int_Calendar()
        {
            var dateTime = new DateTime(2012, 6, 11, new GregorianCalendar());
            VerifyDateTime(dateTime, 2012, 6, 11, 0, 0, 0, 0, DateTimeKind.Unspecified);
        }

        [Fact]
        public void Ctor_Int_Int_Int_Int_Int_Int()
        {
            var dateTime = new DateTime(2012, 12, 31, 13, 50, 10);
            VerifyDateTime(dateTime, 2012, 12, 31, 13, 50, 10, 0, DateTimeKind.Unspecified);
        }

        [Fact]
        public void Ctor_Int_Int_Int_Int_Int_Int_DateTimeKind()
        {
            var dateTime = new DateTime(1986, 8, 15, 10, 20, 5, DateTimeKind.Local);
            VerifyDateTime(dateTime, 1986, 8, 15, 10, 20, 5, 0, DateTimeKind.Local);
        }

        [Fact]
        public void Ctor_Int_Int_Int_Int_Int_Int_Calendar()
        {
            var dateTime = new DateTime(2012, 12, 31, 13, 50, 10, new GregorianCalendar());
            VerifyDateTime(dateTime, 2012, 12, 31, 13, 50, 10, 0, DateTimeKind.Unspecified);
        }

        public static IEnumerable<object[]> Ctor_Int_Int_Int_Int_Int_Int_Int_Int_TestData()
        {
            yield return new object[] { 1986, 8, 15, 10, 20, 5, 600 };
            yield return new object[] { 1986, 2, 28, 10, 20, 5, 600 };
            yield return new object[] { 1986, 12, 31, 10, 20, 5, 600 };
            yield return new object[] { 2000, 2, 28, 10, 20, 5, 600 };
            yield return new object[] { 2000, 2, 29, 10, 20, 5, 600 };
            yield return new object[] { 2000, 12, 31, 10, 20, 5, 600 };
            yield return new object[] { 1900, 2, 28, 10, 20, 5, 600 };
            yield return new object[] { 1900, 12, 31, 10, 20, 5, 600 };
        }

        [Theory]
        [MemberData(nameof(Ctor_Int_Int_Int_Int_Int_Int_Int_Int_TestData))]
        public void Ctor_Int_Int_Int_Int_Int_Int_Int(int year, int month, int day, int hour, int minute, int second, int millisecond)
        {
            var dateTime = new DateTime(year, month, day, hour, minute, second, millisecond);
            VerifyDateTime(dateTime, year, month, day, hour, minute, second, millisecond, DateTimeKind.Unspecified);
        }

        [Theory]
        [MemberData(nameof(Ctor_Int_Int_Int_Int_Int_Int_Int_Int_TestData))]
        public void Ctor_Int_Int_Int_Int_Int_Int_Int_Calendar(int year, int month, int day, int hour, int minute, int second, int millisecond)
        {
            var dateTime = new DateTime(year, month, day, hour, minute, second, millisecond, new GregorianCalendar());
            VerifyDateTime(dateTime, year, month, day, hour, minute, second, millisecond, DateTimeKind.Unspecified);
        }

        [Theory]
        [MemberData(nameof(Ctor_Int_Int_Int_Int_Int_Int_Int_Int_TestData))]
        public void Ctor_Int_Int_Int_Int_Int_Int_Int_Int_DateTimeKind(int year, int month, int day, int hour, int minute, int second, int millisecond)
        {
            var dateTime = new DateTime(year, month, day, hour, minute, second, millisecond, DateTimeKind.Local);
            VerifyDateTime(dateTime, year, month, day, hour, minute, second, millisecond, DateTimeKind.Local);
        }

        [Theory]
        [MemberData(nameof(Ctor_Int_Int_Int_Int_Int_Int_Int_Int_TestData))]
        public void Ctor_Int_Int_Int_Int_Int_Int_Int_Int_Calendar_DateTimeKind(int year, int month, int day, int hour, int minute, int second, int millisecond)
        {
            var dateTime = new DateTime(year, month, day, hour, minute, second, millisecond, new GregorianCalendar(), DateTimeKind.Local);
            VerifyDateTime(dateTime, year, month, day, hour, minute, second, millisecond, DateTimeKind.Local);
        }

        public static IEnumerable<object[]> Ctor_Int_Int_Int_Int_Int_Int_Int_Int_Int_TestData()
        {
            yield return new object[] { 1986, 8, 15, 10, 20, 5, 600, 300 };
            yield return new object[] { 1986, 2, 28, 10, 20, 5, 600, 300 };
            yield return new object[] { 1986, 12, 31, 10, 20, 5, 600, 300 };
            yield return new object[] { 2000, 2, 28, 10, 20, 5, 600, 300 };
            yield return new object[] { 2000, 2, 29, 10, 20, 5, 600, 300 };
            yield return new object[] { 2000, 12, 31, 10, 20, 5, 600, 300 };
            yield return new object[] { 1900, 2, 28, 10, 20, 5, 600, 300 };
            yield return new object[] { 1900, 12, 31, 10, 20, 5, 600, 300 };
        }

        [Theory]
        [MemberData(nameof(Ctor_Int_Int_Int_Int_Int_Int_Int_Int_Int_TestData))]
        public void Ctor_Int_Int_Int_Int_Int_Int_Int_Int(int year, int month, int day, int hour, int minute, int second, int millisecond, int microsecond)
        {
            var dateTime = new DateTime(year, month, day, hour, minute, second, millisecond, microsecond);
            VerifyDateTime(dateTime, year, month, day, hour, minute, second, millisecond, microsecond, DateTimeKind.Unspecified);
        }

        [Theory]
        [MemberData(nameof(Ctor_Int_Int_Int_Int_Int_Int_Int_Int_Int_TestData))]
        public void Ctor_Int_Int_Int_Int_Int_Int_Int_Int_Calendar(int year, int month, int day, int hour, int minute, int second, int millisecond, int microsecond)
        {
            var dateTime = new DateTime(year, month, day, hour, minute, second, millisecond, microsecond, new GregorianCalendar());
            VerifyDateTime(dateTime, year, month, day, hour, minute, second, millisecond, microsecond, DateTimeKind.Unspecified);
        }

        [Theory]
        [MemberData(nameof(Ctor_Int_Int_Int_Int_Int_Int_Int_Int_Int_TestData))]
        public void Ctor_Int_Int_Int_Int_Int_Int_Int_Int_Int_DateTimeKind(int year, int month, int day, int hour, int minute, int second, int millisecond, int microsecond)
        {
            var dateTime = new DateTime(year, month, day, hour, minute, second, millisecond, microsecond, DateTimeKind.Local);
            VerifyDateTime(dateTime, year, month, day, hour, minute, second, millisecond, microsecond, DateTimeKind.Local);
        }

        [Theory]
        [MemberData(nameof(Ctor_Int_Int_Int_Int_Int_Int_Int_Int_Int_TestData))]
        public void Ctor_Int_Int_Int_Int_Int_Int_Int_Int_Int_Calendar_DateTimeKind(int year, int month, int day, int hour, int minute, int second, int millisecond, int microsecond)
        {
            var dateTime = new DateTime(year, month, day, hour, minute, second, millisecond, microsecond, new GregorianCalendar(), DateTimeKind.Local);
            VerifyDateTime(dateTime, year, month, day, hour, minute, second, millisecond, microsecond, DateTimeKind.Local);
        }

        public static IEnumerable<object[]> Ctor_Int_Int_Int_Int_Int_Int_Int_Int_Int_WithNanoseconds_TestData()
        {
            yield return new object[] { 1986, 8, 15, 10, 20, 5, 600, 300, 0 };
            yield return new object[] { 1986, 2, 28, 10, 20, 5, 600, 300, 100 };
            yield return new object[] { 1986, 12, 31, 10, 20, 5, 600, 300, 200 };
            yield return new object[] { 2000, 2, 28, 10, 20, 5, 600, 300, 300 };
            yield return new object[] { 2000, 2, 29, 10, 20, 5, 600, 300, 400 };
            yield return new object[] { 2000, 12, 31, 10, 20, 5, 600, 300, 500 };
            yield return new object[] { 1900, 2, 28, 10, 20, 5, 600, 300, 600 };
            yield return new object[] { 1900, 12, 31, 10, 20, 5, 600, 300, 900 };
        }

        [Theory]
        [MemberData(nameof(Ctor_Int_Int_Int_Int_Int_Int_Int_Int_Int_WithNanoseconds_TestData))]
        public void Ctor_Int_Int_Int_Int_Int_Int_Int_Int_Int_WithNanoseconds(int year, int month, int day, int hour, int minute, int second, int millisecond, int microsecond, int nanosecond)
        {
            var dateTime = new DateTime(year, month, day, hour, minute, second, millisecond, microsecond);
            dateTime = dateTime.AddTicks(nanosecond / 100);
            VerifyDateTime(dateTime, year, month, day, hour, minute, second, millisecond, microsecond, nanosecond, DateTimeKind.Unspecified);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(10000)]
        public void Ctor_InvalidYear_ThrowsArgumentOutOfRangeException(int year)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(year, 1, 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(year, 1, 1, new GregorianCalendar()));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(year, 1, 1, 1, 1, 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(year, 1, 1, 1, 1, 1, DateTimeKind.Utc));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(year, 1, 1, 1, 1, 1, new GregorianCalendar()));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(year, 1, 1, 1, 1, 1, 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(year, 1, 1, 1, 1, 1, 1, DateTimeKind.Utc));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(year, 1, 1, 1, 1, 1, 1, new GregorianCalendar()));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(year, 1, 1, 1, 1, 1, 1, new GregorianCalendar(), DateTimeKind.Utc));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(year, 1, 1, 1, 1, 1, 1, 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(year, 1, 1, 1, 1, 1, 1, 1, DateTimeKind.Utc));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(year, 1, 1, 1, 1, 1, 1, 1, new GregorianCalendar()));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(year, 1, 1, 1, 1, 1, 1, 1, new GregorianCalendar(), DateTimeKind.Utc));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(13)]
        public void Ctor_InvalidMonth_ThrowsArgumentOutOfRangeException(int month)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, month, 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, month, 1, new GregorianCalendar()));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, month, 1, 1, 1, 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, month, 1, 1, 1, 1, DateTimeKind.Utc));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, month, 1, 1, 1, 1, new GregorianCalendar()));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, month, 1, 1, 1, 1, 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, month, 1, 1, 1, 1, 1, DateTimeKind.Utc));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, month, 1, 1, 1, 1, 1, new GregorianCalendar()));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, month, 1, 1, 1, 1, 1, new GregorianCalendar(), DateTimeKind.Utc));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, month, 1, 1, 1, 1, 1, 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, month, 1, 1, 1, 1, 1, 1, DateTimeKind.Utc));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, month, 1, 1, 1, 1, 1, 1, new GregorianCalendar()));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, month, 1, 1, 1, 1, 1, 1, new GregorianCalendar(), DateTimeKind.Utc));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(32)]
        public void Ctor_InvalidDay_ThrowsArgumentOutOfRangeException(int day)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, day));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, day, new GregorianCalendar()));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, day, 1, 1, 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, day, 1, 1, 1, DateTimeKind.Utc));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, day, 1, 1, 1, new GregorianCalendar()));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, day, 1, 1, 1, 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, day, 1, 1, 1, 1, DateTimeKind.Utc));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, day, 1, 1, 1, 1, new GregorianCalendar()));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, day, 1, 1, 1, 1, new GregorianCalendar(), DateTimeKind.Utc));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, day, 1, 1, 1, 1, 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, day, 1, 1, 1, 1, 1, DateTimeKind.Utc));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, day, 1, 1, 1, 1, 1, new GregorianCalendar()));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, day, 1, 1, 1, 1, 1, new GregorianCalendar(), DateTimeKind.Utc));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(24)]
        public void Ctor_InvalidHour_ThrowsArgumentOutOfRangeException(int hour)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, 1, hour, 1, 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, 1, hour, 1, 1, DateTimeKind.Utc));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, 1, hour, 1, 1, new GregorianCalendar()));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, 1, hour, 1, 1, 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, 1, hour, 1, 1, 1, DateTimeKind.Utc));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, 1, hour, 1, 1, 1, new GregorianCalendar()));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, 1, hour, 1, 1, 1, new GregorianCalendar(), DateTimeKind.Utc));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, 1, hour, 1, 1, 1, 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, 1, hour, 1, 1, 1, 1, DateTimeKind.Utc));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, 1, hour, 1, 1, 1, 1, new GregorianCalendar()));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, 1, hour, 1, 1, 1, 1, new GregorianCalendar(), DateTimeKind.Utc));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(60)]
        public void Ctor_InvalidMinute_ThrowsArgumentOutOfRangeException(int minute)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, 1, 1, minute, 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, 1, 1, minute, 1, DateTimeKind.Utc));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, 1, 1, minute, 1, new GregorianCalendar()));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, 1, 1, minute, 1, 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, 1, 1, minute, 1, 1, DateTimeKind.Utc));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, 1, 1, minute, 1, 1, new GregorianCalendar()));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, 1, 1, minute, 1, 1, new GregorianCalendar(), DateTimeKind.Utc));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, 1, 1, minute, 1, 1, 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, 1, 1, minute, 1, 1, 1, DateTimeKind.Utc));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, 1, 1, minute, 1, 1, 1, new GregorianCalendar()));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, 1, 1, minute, 1, 1, 1, new GregorianCalendar(), DateTimeKind.Utc));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(60)]
        public void Ctor_InvalidSecond_ThrowsArgumentOutOfRangeException(int second)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, 1, 1, 1, second));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, 1, 1, 1, second, DateTimeKind.Utc));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, 1, 1, 1, second, new GregorianCalendar()));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, 1, 1, 1, second, 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, 1, 1, 1, second, 1, DateTimeKind.Utc));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, 1, 1, 1, second, 1, new GregorianCalendar(), DateTimeKind.Utc));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, 1, 1, 1, second, 1, 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, 1, 1, 1, second, 1, 1, DateTimeKind.Utc));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTime(1, 1, 1, 1, 1, second, 1, 1, new GregorianCalendar(), DateTimeKind.Utc));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(1000)]
        public void Ctor_InvalidMillisecond_ThrowsArgumentOutOfRangeException(int millisecond)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("millisecond", () => new DateTime(1, 1, 1, 1, 1, 1, millisecond));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("millisecond", () => new DateTime(1, 1, 1, 1, 1, 1, millisecond, DateTimeKind.Utc));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("millisecond", () => new DateTime(1, 1, 1, 1, 1, 1, millisecond, new GregorianCalendar()));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("millisecond", () => new DateTime(1, 1, 1, 1, 1, 1, millisecond, new GregorianCalendar(), DateTimeKind.Utc));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("millisecond", () => new DateTime(1, 1, 1, 1, 1, 1, millisecond, 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("millisecond", () => new DateTime(1, 1, 1, 1, 1, 1, millisecond, 1, DateTimeKind.Utc));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("millisecond", () => new DateTime(1, 1, 1, 1, 1, 1, millisecond, 1, new GregorianCalendar()));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("millisecond", () => new DateTime(1, 1, 1, 1, 1, 1, millisecond, 1, new GregorianCalendar(), DateTimeKind.Utc));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(1000)]
        public void Ctor_InvalidMicrosecond_ThrowsArgumentOutOfRangeException(int microsecond)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("microsecond", () => new DateTime(1, 1, 1, 1, 1, 1, 1, microsecond));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("microsecond", () => new DateTime(1, 1, 1, 1, 1, 1, 1, microsecond, DateTimeKind.Utc));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("microsecond", () => new DateTime(1, 1, 1, 1, 1, 1, 1, microsecond, new GregorianCalendar()));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("microsecond", () => new DateTime(1, 1, 1, 1, 1, 1, 1, microsecond, new GregorianCalendar(), DateTimeKind.Utc));
        }

        [Theory]
        [InlineData(DateTimeKind.Unspecified - 1)]
        [InlineData(DateTimeKind.Local + 1)]
        public void Ctor_InvalidDateTimeKind_ThrowsArgumentException(DateTimeKind kind)
        {
            AssertExtensions.Throws<ArgumentException>("kind", () => new DateTime(0, kind));
            AssertExtensions.Throws<ArgumentException>("kind", () => new DateTime(1, 1, 1, 1, 1, 1, kind));
            AssertExtensions.Throws<ArgumentException>("kind", () => new DateTime(1, 1, 1, 1, 1, 1, 1, kind));
            AssertExtensions.Throws<ArgumentException>("kind", () => new DateTime(1, 1, 1, 1, 1, 1, 1, new GregorianCalendar(), kind));
            AssertExtensions.Throws<ArgumentException>("kind", () => new DateTime(1, 1, 1, 1, 1, 1, 1, 1, kind));
            AssertExtensions.Throws<ArgumentException>("kind", () => new DateTime(1, 1, 1, 1, 1, 1, 1, 1, new GregorianCalendar(), kind));
        }

        [Fact]
        public void Ctor_NullCalendar_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("calendar", () => new DateTime(1, 1, 1, null));
            AssertExtensions.Throws<ArgumentNullException>("calendar", () => new DateTime(1, 1, 1, 1, 1, 1, null));
            AssertExtensions.Throws<ArgumentNullException>("calendar", () => new DateTime(1, 1, 1, 1, 1, 1, 1, null));
            AssertExtensions.Throws<ArgumentNullException>("calendar", () => new DateTime(1, 1, 1, 1, 1, 1, 1, null, DateTimeKind.Local));
            AssertExtensions.Throws<ArgumentNullException>("calendar", () => new DateTime(1, 1, 1, 1, 1, 1, 1, 1, null));
            AssertExtensions.Throws<ArgumentNullException>("calendar", () => new DateTime(1, 1, 1, 1, 1, 1, 1, 1, null, DateTimeKind.Local));
        }

        [Theory]
        [InlineData(2004, 1, 31)]
        [InlineData(2004, 2, 29)]
        [InlineData(2004, 3, 31)]
        [InlineData(2004, 4, 30)]
        [InlineData(2004, 5, 31)]
        [InlineData(2004, 6, 30)]
        [InlineData(2004, 7, 31)]
        [InlineData(2004, 8, 31)]
        [InlineData(2004, 9, 30)]
        [InlineData(2004, 10, 31)]
        [InlineData(2004, 11, 30)]
        [InlineData(2004, 12, 31)]
        [InlineData(2005, 1, 31)]
        [InlineData(2005, 2, 28)]
        [InlineData(2005, 3, 31)]
        [InlineData(2005, 4, 30)]
        [InlineData(2005, 5, 31)]
        [InlineData(2005, 6, 30)]
        [InlineData(2005, 7, 31)]
        [InlineData(2005, 8, 31)]
        [InlineData(2005, 9, 30)]
        [InlineData(2005, 10, 31)]
        [InlineData(2005, 11, 30)]
        [InlineData(2005, 12, 31)]
        public void DaysInMonth_Invoke_ReturnsExpected(int year, int month, int expected)
        {
            Assert.Equal(expected, DateTime.DaysInMonth(year, month));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(13)]
        public void DaysInMonth_InvalidMonth_ThrowsArgumentOutOfRangeException(int month)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("month", () => DateTime.DaysInMonth(1, month));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(10000)]
        public void DaysInMonth_InvalidYear_ThrowsArgumentOutOfRangeException(int year)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("year", () => DateTime.DaysInMonth(year, 1));
        }

        [Theory]
        [InlineData(2004, true)]
        [InlineData(2000, true)]
        [InlineData(1900, false)]
        [InlineData(2005, false)]
        public void IsLeapYear_Invoke_ReturnsExpected(int year, bool expected)
        {
            Assert.Equal(expected, DateTime.IsLeapYear(year));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(10000)]
        public void IsLeapYear_InvalidYear_ThrowsArgumentOutOfRangeException(int year)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("year", () => DateTime.IsLeapYear(year));
        }

        public static IEnumerable<object[]> IsDaylightSavingTime_TestData()
        {
            yield return new object[] { new DateTime(2018, 11, 24, 0, 0, 0, DateTimeKind.Utc), false };
            yield return new object[] { DateTime.MinValue, false };
            yield return new object[] { DateTime.MaxValue, false };
        }

        [Theory]
        [MemberData(nameof(IsDaylightSavingTime_TestData))]
        public void IsDaylightSavingTime_Invoke_ReturnsExpected(DateTime date, bool expected)
        {
            Assert.Equal(expected, date.IsDaylightSavingTime());
        }

        public static IEnumerable<object[]> Add_TimeSpan_TestData()
        {
            yield return new object[] { new DateTime(1000), new TimeSpan(10), new DateTime(1010) };
            yield return new object[] { new DateTime(1000), TimeSpan.Zero, new DateTime(1000) };
            yield return new object[] { new DateTime(1000), new TimeSpan(-10), new DateTime(990) };
        }

        [Theory]
        [MemberData(nameof(Add_TimeSpan_TestData))]
        public void Add_TimeSpan_ReturnsExpected(DateTime dateTime, TimeSpan timeSpan, DateTime expected)
        {
            Assert.Equal(expected, dateTime.Add(timeSpan));
            Assert.Equal(expected, dateTime + timeSpan);
        }

        public static IEnumerable<object[]> Add_TimeSpanOutOfRange_TestData()
        {
            yield return new object[] { DateTime.Now, TimeSpan.MaxValue };
            yield return new object[] { DateTime.Now, TimeSpan.MinValue };
            yield return new object[] { DateTime.MaxValue, new TimeSpan(1) };
            yield return new object[] { DateTime.MinValue, new TimeSpan(-1) };
        }

        [Theory]
        [MemberData(nameof(Add_TimeSpanOutOfRange_TestData))]
        public void Add_TimeSpan_NewDateOutOfRange_ThrowsArgumentOutOfRangeException(DateTime date, TimeSpan value)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => date.Add(value));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("t", () => date + value);
        }

        public static IEnumerable<object[]> AddYears_TestData()
        {
            yield return new object[] { new DateTime(1986, 8, 15, 10, 20, 5, 70), 10, new DateTime(1996, 8, 15, 10, 20, 5, 70) };
            yield return new object[] { new DateTime(1986, 8, 15, 10, 20, 5, 70), 0, new DateTime(1986, 8, 15, 10, 20, 5, 70) };
            yield return new object[] { new DateTime(1986, 8, 15, 10, 20, 5, 70), -10, new DateTime(1976, 8, 15, 10, 20, 5, 70) };
        }

        [Theory]
        [MemberData(nameof(AddYears_TestData))]
        public void AddYears_Invoke_ReturnsExpected(DateTime dateTime, int years, DateTime expected)
        {
            Assert.Equal(expected, dateTime.AddYears(years));
        }

        public static IEnumerable<object[]> AddYears_OutOfRange_TestData()
        {
            yield return new object[] { DateTime.Now, 10001 };
            yield return new object[] { DateTime.Now, -10001 };
            yield return new object[] { DateTime.MaxValue, 1 };
            yield return new object[] { DateTime.MinValue, -1 };
        }

        [Theory]
        [MemberData(nameof(AddYears_OutOfRange_TestData))]
        public static void AddYears_NewDateOutOfRange_ThrowsArgumentOutOfRangeException(DateTime date, int years)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => date.AddYears(years));
        }

        public static IEnumerable<object[]> AddMonths_TestData()
        {
            yield return new object[] { new DateTime(1986, 8, 15, 10, 20, 5, 70), 2, new DateTime(1986, 10, 15, 10, 20, 5, 70) };
            yield return new object[] { new DateTime(1986, 8, 31, 10, 20, 5, 70), 1, new DateTime(1986, 9, 30, 10, 20, 5, 70) };
            yield return new object[] { new DateTime(1986, 9, 30, 10, 20, 5, 70), 1, new DateTime(1986, 10, 30, 10, 20, 5, 70) };
            yield return new object[] { new DateTime(1986, 8, 15, 10, 20, 5, 70), 0, new DateTime(1986, 8, 15, 10, 20, 5, 70) };
            yield return new object[] { new DateTime(1986, 8, 15, 10, 20, 5, 70), -2, new DateTime(1986, 6, 15, 10, 20, 5, 70) };
            yield return new object[] { new DateTime(1900, 2, 28, 10, 20, 5, 70), 1, new DateTime(1900, 3, 28, 10, 20, 5, 70) };
            yield return new object[] { new DateTime(1900, 1, 31, 10, 20, 5, 70), 1, new DateTime(1900, 2, 28, 10, 20, 5, 70) };
            yield return new object[] { new DateTime(2000, 1, 31, 10, 20, 5, 70), 1, new DateTime(2000, 2, 29, 10, 20, 5, 70) };
            yield return new object[] { new DateTime(2000, 2, 29, 10, 20, 5, 70), 1, new DateTime(2000, 3, 29, 10, 20, 5, 70) };
            yield return new object[] { new DateTime(2004, 1, 31, 10, 20, 5, 70), 1, new DateTime(2004, 2, 29, 10, 20, 5, 70) };
            yield return new object[] { new DateTime(2004, 2, 29, 10, 20, 5, 70), 1, new DateTime(2004, 3, 29, 10, 20, 5, 70) };
            yield return new object[] { new DateTime(2000, 12, 31), 1, new DateTime(2001, 1, 31) };
        }

        [Theory]
        [MemberData(nameof(AddMonths_TestData))]
        public void AddMonths_Invoke_ReturnsExpected(DateTime dateTime, int months, DateTime expected)
        {
            Assert.Equal(expected, dateTime.AddMonths(months));
        }

        public static IEnumerable<object[]> AddMonths_OutOfRange_TestData()
        {
            yield return new object[] { DateTime.Now, 120001 };
            yield return new object[] { DateTime.Now, -120001 };
            yield return new object[] { DateTime.MaxValue, 1 };
            yield return new object[] { DateTime.MinValue, -1 };
        }

        [Theory]
        [MemberData(nameof(AddMonths_OutOfRange_TestData))]
        public void AddMonths_NewDateOutOfRange_ThrowsArgumentOutOfRangeException(DateTime date, int months)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("months", () => date.AddMonths(months));
        }

        public static IEnumerable<object[]> AddDays_TestData()
        {
            yield return new object[] { new DateTime(1986, 8, 15, 10, 20, 5, 70), 2, new DateTime(1986, 8, 17, 10, 20, 5, 70) };
            yield return new object[] { new DateTime(1986, 8, 15, 10, 20, 5, 70), 2, new DateTime(1986, 8, 17, 10, 20, 5, 70) };
            yield return new object[] { new DateTime(1986, 8, 15, 10, 20, 5, 70), 0, new DateTime(1986, 8, 15, 10, 20, 5, 70) };
            yield return new object[] { new DateTime(1986, 8, 15, 10, 20, 5, 70), -2, new DateTime(1986, 8, 13, 10, 20, 5, 70) };
        }

        [Theory]
        [MemberData(nameof(AddDays_TestData))]
        public void AddDays_Invoke_ReturnsExpected(DateTime dateTime, double days, DateTime expected)
        {
            Assert.Equal(expected, dateTime.AddDays(days));
        }

        public static IEnumerable<object[]> AddDays_OutOfRange_TestData()
        {
            yield return new object[] { DateTime.MaxValue, 1 };
            yield return new object[] { DateTime.MinValue, -1 };
            yield return new object[] { DateTime.Now, double.MaxValue };
            yield return new object[] { DateTime.Now, double.MinValue };
        }

        [Theory]
        [MemberData(nameof(AddDays_OutOfRange_TestData))]
        public void AddDays_NewDateOutOfRange_ThrowsArgumentOutOfRangeException(DateTime date, double days)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => date.AddDays(days));
        }

        public static IEnumerable<object[]> AddHours_TestData()
        {
            yield return new object[] { new DateTime(1986, 8, 15, 10, 20, 5, 70), 3, new DateTime(1986, 8, 15, 13, 20, 5, 70) };
            yield return new object[] { new DateTime(1986, 8, 15, 10, 20, 5, 70), 0, new DateTime(1986, 8, 15, 10, 20, 5, 70) };
            yield return new object[] { new DateTime(1986, 8, 15, 10, 20, 5, 70), -3, new DateTime(1986, 8, 15, 7, 20, 5, 70) };
        }

        [Theory]
        [MemberData(nameof(AddHours_TestData))]
        public void AddHours_Invoke_RetunsExpected(DateTime dateTime, double hours, DateTime expected)
        {
            Assert.Equal(expected, dateTime.AddHours(hours));
        }

        public static IEnumerable<object[]> AddHours_OutOfRange_TestData()
        {
            yield return new object[] { DateTime.MaxValue, 1 };
            yield return new object[] { DateTime.MinValue, -1 };
            yield return new object[] { DateTime.Now, double.MaxValue };
            yield return new object[] { DateTime.Now, double.MinValue };
        }

        [Theory]
        [MemberData(nameof(AddHours_OutOfRange_TestData))]
        public void AddHours_NewDateOutOfRange_ThrowsArgumentOutOfRangeException(DateTime date, double hours)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => date.AddHours(hours));
        }

        public static IEnumerable<object[]> AddMinutes_TestData()
        {
            yield return new object[] { new DateTime(1986, 8, 15, 10, 20, 5, 70), 5, new DateTime(1986, 8, 15, 10, 25, 5, 70) };
            yield return new object[] { new DateTime(1986, 8, 15, 10, 20, 5, 70), 0, new DateTime(1986, 8, 15, 10, 20, 5, 70) };
            yield return new object[] { new DateTime(1986, 8, 15, 10, 20, 5, 70), -5, new DateTime(1986, 8, 15, 10, 15, 5, 70) };
        }

        [Theory]
        [MemberData(nameof(AddMinutes_TestData))]
        public void AddMinutes_Invoke_ReturnsExpected(DateTime dateTime, double minutes, DateTime expected)
        {
            Assert.Equal(expected, dateTime.AddMinutes(minutes));
        }

        public static IEnumerable<object[]> AddMinutes_OutOfRange_TestData()
        {
            yield return new object[] { DateTime.MaxValue, 1 };
            yield return new object[] { DateTime.MinValue, -1 };
            yield return new object[] { DateTime.Now, double.MaxValue };
            yield return new object[] { DateTime.Now, double.MinValue };
        }

        [Theory]
        [MemberData(nameof(AddMinutes_OutOfRange_TestData))]

        public void AddMinutes_NewDateOutOfRange_ThrowsArgumentOutOfRangeException(DateTime date, double minutes)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => date.AddMinutes(minutes));
        }

        public static IEnumerable<object[]> AddSeconds_TestData()
        {
            yield return new object[] { new DateTime(1986, 8, 15, 10, 20, 5, 70), 30, new DateTime(1986, 8, 15, 10, 20, 35, 70) };
            yield return new object[] { new DateTime(1986, 8, 15, 10, 20, 5, 70), 0, new DateTime(1986, 8, 15, 10, 20, 5, 70) };
            yield return new object[] { new DateTime(1986, 8, 15, 10, 20, 5, 70), -3, new DateTime(1986, 8, 15, 10, 20, 2, 70) };
        }

        [Theory]
        [MemberData(nameof(AddSeconds_TestData))]
        public void AddSeconds_Invoke_ReturnsExpected(DateTime dateTime, double seconds, DateTime expected)
        {
            Assert.Equal(expected, dateTime.AddSeconds(seconds));
        }

        public static IEnumerable<object[]> AddSeconds_OutOfRange_TestData()
        {
            yield return new object[] { DateTime.MaxValue, 1 };
            yield return new object[] { DateTime.MinValue, -1 };
            yield return new object[] { DateTime.Now, double.MaxValue };
            yield return new object[] { DateTime.Now, double.MinValue };
        }

        [Theory]
        [MemberData(nameof(AddSeconds_OutOfRange_TestData))]
        public void AddSeconds_NewDateOutOfRange_ThrowsArgumentOutOfRangeException(DateTime date, double seconds)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => date.AddSeconds(seconds));
        }

        public static IEnumerable<object[]> AddMilliseconds_TestData()
        {
            yield return new object[] { new DateTime(1986, 8, 15, 10, 20, 5, 70), 10, new DateTime(1986, 8, 15, 10, 20, 5, 80) };
            yield return new object[] { new DateTime(1986, 8, 15, 10, 20, 5, 70), 0, new DateTime(1986, 8, 15, 10, 20, 5, 70) };
            yield return new object[] { new DateTime(1986, 8, 15, 10, 20, 5, 70), -10, new DateTime(1986, 8, 15, 10, 20, 5, 60) };
        }

        [Theory]
        [MemberData(nameof(AddMilliseconds_TestData))]
        public void AddMilliseconds_Invoke_ReturnsExpected(DateTime dateTime, double milliseconds, DateTime expected)
        {
            Assert.Equal(expected, dateTime.AddMilliseconds(milliseconds));
        }

        public static IEnumerable<object[]> AddMillseconds_OutOfRange_TestData()
        {
            yield return new object[] { DateTime.MaxValue, 1 };
            yield return new object[] { DateTime.MinValue, -1 };
            yield return new object[] { DateTime.Now, double.MaxValue };
            yield return new object[] { DateTime.Now, double.MinValue };
        }

        [Theory]
        [MemberData(nameof(AddMillseconds_OutOfRange_TestData))]
        public void AddMilliseconds_NewDateOutOfRange_ThrowsArgumentOutOfRangeException(DateTime date, double milliseconds)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => date.AddMilliseconds(milliseconds));
        }

        public static IEnumerable<object[]> AddMicroseconds_TestData()
        {
            yield return new object[] { new DateTime(1986, 8, 15, 10, 20, 5, 70, 70), 10, new DateTime(1986, 8, 15, 10, 20, 5, 70, 80) };
            yield return new object[] { new DateTime(1986, 8, 15, 10, 20, 5, 70, 70), 0, new DateTime(1986, 8, 15, 10, 20, 5, 70, 70) };
            yield return new object[] { new DateTime(1986, 8, 15, 10, 20, 5, 70, 70), -10, new DateTime(1986, 8, 15, 10, 20, 5, 70, 60) };
        }

        [Theory]
        [MemberData(nameof(AddMicroseconds_TestData))]
        public void AddMicroseconds_Invoke_ReturnsExpected(DateTime dateTime, double microseconds, DateTime expected)
        {
            Assert.Equal(expected, dateTime.AddMicroseconds(microseconds));
        }

        public static IEnumerable<object[]> AddMicroseconds_OutOfRange_TestData()
        {
            yield return new object[] { DateTime.MaxValue, 1 };
            yield return new object[] { DateTime.MinValue, -1 };
            yield return new object[] { DateTime.Now, double.MaxValue };
            yield return new object[] { DateTime.Now, double.MinValue };
        }

        [Theory]
        [MemberData(nameof(AddMicroseconds_OutOfRange_TestData))]
        public void AddMicroseconds_NewDateOutOfRange_ThrowsArgumentOutOfRangeException(DateTime date, double microseconds)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => date.AddMicroseconds(microseconds));
        }

        public static IEnumerable<object[]> AddTicks_TestData()
        {
            yield return new object[] { new DateTime(1000), 10, new DateTime(1010) };
            yield return new object[] { new DateTime(1000), 0, new DateTime(1000) };
            yield return new object[] { new DateTime(1000), -10, new DateTime(990) };
        }

        [Theory]
        [MemberData(nameof(AddTicks_TestData))]
        public void AddTicks_Invoke_ReturnsExpected(DateTime dateTime, long ticks, DateTime expected)
        {
            Assert.Equal(expected, dateTime.AddTicks(ticks));
        }

        public static IEnumerable<object[]> AddTicks_OutOfRange_TestData()
        {
            yield return new object[] { DateTime.MaxValue, 1 };
            yield return new object[] { DateTime.MinValue, -1 };
            yield return new object[] { DateTime.Now, long.MaxValue };
            yield return new object[] { DateTime.Now, long.MinValue };
        }

        [Theory]
        [MemberData(nameof(AddTicks_OutOfRange_TestData))]

        public void AddTicks_NewDateOutOfRange_ThrowsArgumentOutOfRangeException(DateTime date, long ticks)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => date.AddTicks(ticks));
        }

        public static IEnumerable<object[]> CompareTo_TestData()
        {
            yield return new object[] { new DateTime(10), new DateTime(10), 0 };
            yield return new object[] { new DateTime(10), new DateTime(11), -1 };
            yield return new object[] { new DateTime(10), new DateTime(9), 1 };
            yield return new object[] { new DateTime(10), null, 1 };
        }

        [Theory]
        [MemberData(nameof(CompareTo_TestData))]
        public void CompareTo_Invoke_ReturnsExpected(DateTime date, object other, int expected)
        {
            if (other is DateTime otherDate)
            {
                Assert.Equal(expected, date.CompareTo(otherDate));
                Assert.Equal(expected, DateTime.Compare(date, otherDate));

                Assert.Equal(expected > 0, date > otherDate);
                Assert.Equal(expected >= 0, date >= otherDate);
                Assert.Equal(expected < 0, date < otherDate);
                Assert.Equal(expected <= 0, date <= otherDate);
            }

            Assert.Equal(expected, date.CompareTo(other));
        }

        [Fact]
        public void CompareTo_NotDateTime_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => DateTime.Now.CompareTo(new object()));
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { new DateTime(10), new DateTime(10), true };
            yield return new object[] { new DateTime(10), new DateTime(11), false };
            yield return new object[] { new DateTime(10), new DateTime(9), false };
            yield return new object[] { new DateTime(10), new object(), false };
            yield return new object[] { new DateTime(10), null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Invoke_ReturnsExpected(DateTime date, object other, bool expected)
        {
            if (other is DateTime otherDate)
            {
                Assert.Equal(expected, date.Equals(otherDate));
                Assert.Equal(expected, DateTime.Equals(date, otherDate));
                Assert.Equal(expected, date.GetHashCode().Equals(otherDate.GetHashCode()));

                Assert.Equal(expected, date == otherDate);
                Assert.Equal(!expected, date != otherDate);
            }

            Assert.Equal(expected, date.Equals(other));
        }

        [Fact]
        public void DayOfWeek_Get_ReturnsExpected()
        {
            var dateTime = new DateTime(2012, 6, 18);
            Assert.Equal(DayOfWeek.Monday, dateTime.DayOfWeek);
        }

        [Fact]
        public void DayOfYear_Get_ReturnsExpected()
        {
            var dateTime = new DateTime(2012, 6, 18);
            Assert.Equal(170, dateTime.DayOfYear);
        }

        [Fact]
        public void TimeOfDay_Get_ReturnsExpected()
        {
            var dateTime = new DateTime(2012, 6, 18, 10, 5, 1, 0);
            TimeSpan ts = dateTime.TimeOfDay;

            DateTime newDate = dateTime.Subtract(ts);
            Assert.Equal(new DateTime(2012, 6, 18, 0, 0, 0, 0).Ticks, newDate.Ticks);
            Assert.Equal(dateTime.Ticks, newDate.Add(ts).Ticks);
        }

        [Fact]
        public void Today_Get_ReturnsExpected()
        {
            DateTime today = DateTime.Today;
            DateTime now = DateTime.Now;
            VerifyDateTime(today, now.Year, now.Month, now.Day, 0, 0, 0, 0, DateTimeKind.Local);

            today = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, DateTimeKind.Utc);
            Assert.Equal(DateTimeKind.Utc, today.Kind);
            Assert.False(today.IsDaylightSavingTime());
        }

        public static IEnumerable<object[]> Subtract_TimeSpan_TestData()
        {
            var dateTime = new DateTime(2012, 6, 18, 10, 5, 1, 0, DateTimeKind.Utc);

            yield return new object[] { dateTime, new TimeSpan(10, 5, 1), new DateTime(2012, 6, 18, 0, 0, 0, 0, DateTimeKind.Utc) };
            yield return new object[] { dateTime, new TimeSpan(-10, -5, -1), new DateTime(2012, 6, 18, 20, 10, 2, 0, DateTimeKind.Utc) };
        }

        [Theory]
        [MemberData(nameof(Subtract_TimeSpan_TestData))]
        public void Subtract_TimeSpan_ReturnsExpected(DateTime dateTime, TimeSpan timeSpan, DateTime expected)
        {
            Assert.Equal(expected, dateTime.Subtract(timeSpan));
            Assert.Equal(expected, dateTime - timeSpan);
        }

        public static IEnumerable<object[]> Subtract_OutOfRangeTimeSpan_TestData()
        {
            yield return new object[] { DateTime.Now, TimeSpan.MinValue };
            yield return new object[] { DateTime.Now, TimeSpan.MaxValue };
            yield return new object[] { DateTime.MaxValue, new TimeSpan(-1) };
            yield return new object[] { DateTime.MinValue, new TimeSpan(1) };
        }

        [Theory]
        [MemberData(nameof(Subtract_OutOfRangeTimeSpan_TestData))]
        public static void Subtract_OutOfRangeTimeSpan_ThrowsArgumentOutOfRangeException(DateTime date, TimeSpan value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => date.Subtract(value));
            Assert.Throws<ArgumentOutOfRangeException>(() => date - value);
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
        public void Subtract_DateTime_ReturnsExpected(DateTime dateTime1, DateTime dateTime2, TimeSpan expected)
        {
            Assert.Equal(expected, dateTime1.Subtract(dateTime2));
            Assert.Equal(expected, dateTime1 - dateTime2);
        }

        public static IEnumerable<object[]> ToOADate_TestData()
        {
            yield return new object[] { new DateTime(1), 0 };
            yield return new object[] { new DateTime((long)10000 * 1000 * 60 * 60 * 24 - 1), 1 };
            yield return new object[] { new DateTime(100, 1, 1), -657434 };
            yield return new object[] { new DateTime(1889, 11, 24, 23, 59, 59, 999).AddTicks(1), -3687 };
            yield return new object[] { new DateTime(1889, 11, 24, 17, 57, 30, 12), -3688.74826402778 };
            yield return new object[] { new DateTime(1889, 11, 24).AddTicks(1), -3688 };
            yield return new object[] { new DateTime(1899, 12, 30), 0 };
            yield return new object[] { new DateTime(2018, 11, 24), 43428 };
            yield return new object[] { new DateTime(2018, 11, 24, 17, 57, 30, 12), 43428.74826 };
            yield return new object[] { new DateTime(2018, 11, 24, 23, 59, 59, 999).AddTicks(1), 43429 };
            yield return new object[] { DateTime.MinValue, 0 };
            yield return new object[] { DateTime.MaxValue, 2958466 };
        }

        [Theory]
        [MemberData(nameof(ToOADate_TestData))]
        public void ToOADate_Invoke_ReturnsExpected(DateTime date, double expected)
        {
            Assert.Equal(expected, date.ToOADate(), 5);
        }

        public static IEnumerable<object[]> ToOADate_Overflow_TestData()
        {
            yield return new object[] { new DateTime((long)10000 * 1000 * 60 * 60 * 24) };
            yield return new object[] { new DateTime(100, 1, 1).AddTicks(-1) };
        }

        [Theory]
        [MemberData(nameof(ToOADate_Overflow_TestData))]
        public void ToOADate_SmallDate_ThrowsOverflowException(DateTime date)
        {
            Assert.Throws<OverflowException>(() => date.ToOADate());
        }

        public static IEnumerable<object[]> FromOADate_TestData()
        {
            yield return new object[] { -1.5, new DateTime(1899, 12, 29, 12, 0, 0) };
            yield return new object[] { -1, new DateTime(1899, 12, 29) };
            yield return new object[] { 0, new DateTime(1899, 12, 30) };
            yield return new object[] { 1, new DateTime(1899, 12, 31) };
            yield return new object[] { 1.5, new DateTime(1899, 12, 31, 12, 0, 0) };
            yield return new object[] { -657434.99999999, new DateTime(100, 1, 1, 23, 59, 59, 999) };
            yield return new object[] { -657434.9999999999, new DateTime(99, 12, 31) };
            yield return new object[] { 2958465.999999994, new DateTime(9999, 12, 31, 23, 59, 59, 999) };
        }

        [Theory]
        [MemberData(nameof(FromOADate_TestData))]
        public void FromOADate_Invoke_ReturnsExpected(double value, DateTime expected)
        {
            DateTime actual = DateTime.FromOADate(value);
            Assert.Equal(expected, actual);
            Assert.Equal(DateTimeKind.Unspecified, actual.Kind);
        }

        [Theory]
        [InlineData(-657435)]
        [InlineData(2958466)]
        [InlineData(-657434.99999999995)]
        [InlineData(2958465.999999995)]
        [InlineData(double.NaN)]
        public void FromOADate_InvalidValue_ThrowsArgumentException(double value)
        {
            AssertExtensions.Throws<ArgumentException>(null, () => DateTime.FromOADate(value));
        }

        public static IEnumerable<object[]> ToBinary_TestData()
        {
            const long Ticks = 123456789101112;

            DateTime local = new DateTime(Ticks, DateTimeKind.Local);
            TimeSpan offset = TimeZoneInfo.Local.GetUtcOffset(local);
            long localTicks = local.Ticks - offset.Ticks;
            if (localTicks < 0)
            {
                localTicks |= 0x4000000000000000;
            }

            yield return new object[] { new DateTime(Ticks, DateTimeKind.Utc), Ticks | ((long)DateTimeKind.Utc << 62) };
            yield return new object[] { new DateTime(Ticks, DateTimeKind.Unspecified), Ticks | (( long)DateTimeKind.Unspecified << 62) };
            yield return new object[] { local, localTicks | ((long)DateTimeKind.Local << 62) };

            yield return new object[] { DateTime.MaxValue, 3155378975999999999 };
            yield return new object[] { DateTime.MinValue, 0 };
        }

        [Theory]
        [MemberData(nameof(ToBinary_TestData))]
        public void ToBinary_Invoke_ReturnsExpected(DateTime date, long expected)
        {
            Assert.Equal(expected, date.ToBinary());
        }

        public static IEnumerable<object[]> FromBinary_TestData()
        {
            yield return new object[] { new DateTime(2018, 12, 24, 17, 34, 30, 12) };
            yield return new object[] { new DateTime(2018, 12, 24, 17, 34, 30, 12, DateTimeKind.Local) };
            yield return new object[] { DateTime.Today };
            yield return new object[] { DateTime.MinValue };
            yield return new object[] { DateTime.MaxValue };
        }

        [Theory]
        [MemberData(nameof(FromBinary_TestData))]
        public void FromBinary_Invoke_ReturnsExpected(DateTime date)
        {
            Assert.Equal(date, DateTime.FromBinary(date.ToBinary()));
        }

        [Theory]
        [InlineData(3155378976000000000)]
        [InlineData(long.MaxValue)]
        [InlineData(3155378976000000000 | ((long)DateTimeKind.Utc << 62))]
        public void FromBinary_OutOfRangeTicks_ThrowsArgumentException(long dateData)
        {
            AssertExtensions.Throws<ArgumentException>("dateData", () => DateTime.FromBinary(dateData));
        }

        public static IEnumerable<object[]> ToFileTime_TestData()
        {
            yield return new object[] { new DateTime(1601, 1, 1, 0, 0, 0, DateTimeKind.Utc) };
            yield return new object[] { new DateTime(1601, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddTicks(1) };

            yield return new object[] { new DateTime(2018, 12, 24, 0, 0, 0, DateTimeKind.Utc) };
            yield return new object[] { new DateTime(2018, 11, 24, 17, 57, 30, 12, DateTimeKind.Utc) };

            yield return new object[] { new DateTime(2018, 12, 24, 0, 0, 0, DateTimeKind.Local) };
            yield return new object[] { new DateTime(2018, 11, 24, 17, 57, 30, 12, DateTimeKind.Local) };
        }

        [Theory]
        [MemberData(nameof(ToFileTime_TestData))]
        public void ToFileTime_Invoke_ReturnsExpected(DateTime date)
        {
            long fileTime = date.ToFileTime();
            DateTime fromFileTime = date.Kind == DateTimeKind.Utc ? DateTime.FromFileTimeUtc(fileTime) : DateTime.FromFileTime(fileTime);
            Assert.Equal(date, fromFileTime);
        }

        public static IEnumerable<object[]> ToFileTime_Overflow_TestData()
        {
            yield return new object[] { DateTime.MinValue };
            yield return new object[] { new DateTime(1600, 12, 31) };
        }

        [Theory]
        [MemberData(nameof(ToFileTime_Overflow_TestData))]
        public void ToFileTime_SmallDate_ThrowsArgumentOutOfRangeException(DateTime date)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => date.ToFileTime());
        }

        public static IEnumerable<object[]> FromFileTime_TestData()
        {
            yield return new object[] { 0, new DateTime(1601, 1, 1) };
            yield return new object[] { 2650467743999999999, DateTime.MaxValue };
            yield return new object[] { 131875558500120000, new DateTime(2018, 11, 24, 17, 57, 30, 12) };
        }

        [Theory]
        [MemberData(nameof(FromFileTime_TestData))]
        public void FromFileTime_Invoke_ReturnsExpected(long fileTime, DateTime expected)
        {
            DateTime actual = DateTime.FromFileTime(fileTime);
            Assert.Equal(expected.ToLocalTime(), actual);
            Assert.Equal(DateTimeKind.Local, actual.Kind);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(2650467744000000000)]
        public void FromFileTime_OutOfRange_ThrowsArgumentOutOfRangeException(long fileTime)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("fileTime", () => DateTime.FromFileTime(fileTime));
        }

        public static IEnumerable<object[]> ToFileTimeUtc_TestData()
        {
            DateTime local = new DateTime(2018, 12, 24, 0, 0, 0, DateTimeKind.Local);
            DateTime localToUtc = TimeZoneInfo.ConvertTimeToUtc(local);

            yield return new object[] { new DateTime(1601, 1, 1), 0 };
            yield return new object[] { new DateTime(1601, 1, 1).AddTicks(1), 1 };
            yield return new object[] { new DateTime(2018, 12, 24), 131900832000000000 };
            yield return new object[] { local, localToUtc.ToFileTimeUtc() };
            yield return new object[] { new DateTime(2018, 11, 24, 17, 57, 30, 12), 131875558500120000 };
            yield return new object[] { DateTime.MaxValue, 2650467743999999999 };
        }

        [Theory]
        [MemberData(nameof(ToFileTimeUtc_TestData))]
        public void ToFileTimeUtc_Invoke_ReturnsExpected(DateTime date, long expected)
        {
            Assert.Equal(expected, date.ToFileTimeUtc());
        }

        public static IEnumerable<object[]> ToFileTimeUtc_Overflow_TestData()
        {
            yield return new object[] { DateTime.MinValue };
            yield return new object[] { new DateTime(1600, 12, 31) };
        }

        [Theory]
        [MemberData(nameof(ToFileTimeUtc_Overflow_TestData))]
        public void ToFileTimeUtc_SmallDate_ThrowsArgumentOutOfRangeException(DateTime date)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => date.ToFileTimeUtc());
        }

        public static IEnumerable<object[]> FromFileTimeUtc_TestData()
        {
            yield return new object[] { 0, new DateTime(1601, 1, 1) };
            yield return new object[] { 2650467743999999999, DateTime.MaxValue };
            yield return new object[] { 131875558500120000, new DateTime(2018, 11, 24, 17, 57, 30, 12) };
        }

        [Theory]
        [MemberData(nameof(FromFileTimeUtc_TestData))]
        public void FromFileTimeUtc_Invoke_ReturnsExpected(long fileTime, DateTime expected)
        {
            DateTime actual = DateTime.FromFileTimeUtc(fileTime);
            Assert.Equal(expected, actual);
            Assert.Equal(DateTimeKind.Utc, actual.Kind);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(2650467744000000000)]
        public void FromFileTimeUtc_OutOfRange_ThrowsArgumentOutOfRangeException(long fileTime)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("fileTime", () => DateTime.FromFileTimeUtc(fileTime));
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

        private static bool IsNotOSXOrBrowser => !PlatformDetection.IsOSXLike && !PlatformDetection.IsBrowser;

        [ConditionalTheory(nameof(IsNotOSXOrBrowser))]
        [InlineData("ar")]
        [InlineData("ar-EG")]
        [InlineData("ar-IQ")]
        [InlineData("ar-SA")]
        [InlineData("ar-YE")]
        public static void DateTimeParsingWithBiDiCultureTest(string cultureName)
        {
            DateTime dt = new DateTime(2021, 11, 30, 14, 30, 40);
            CultureInfo ci = CultureInfo.GetCultureInfo(cultureName);
            string formatted = dt.ToString("d", ci);
            Assert.Equal(dt.Date, DateTime.Parse(formatted, ci));
            formatted = dt.ToString("g", ci);
            DateTime parsed = DateTime.Parse(formatted, ci);
            Assert.Equal(dt.Date, parsed.Date);
            Assert.Equal(dt.Hour, parsed.Hour);
            Assert.Equal(dt.Minute, parsed.Minute);
        }

        [Fact]
        public static void DateTimeParsingWithSpaceTimeSeparators()
        {
            DateTime dt = new DateTime(2021, 11, 30, 14, 30, 40);
            CultureInfo ci = CultureInfo.GetCultureInfo("en-US");
            // It is possible we find some cultures use such formats. dz-BT is example of that
            string formatted = dt.ToString("yyyy/MM/dd hh mm tt", ci);
            DateTime parsed = DateTime.Parse(formatted, ci);
            Assert.Equal(dt.Hour, parsed.Hour);
            Assert.Equal(dt.Minute, parsed.Minute);

            formatted = dt.ToString("yyyy/MM/dd hh mm ss tt", ci);
            parsed = DateTime.Parse(formatted, ci);
            Assert.Equal(dt, parsed);
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

            Assert.Throws<FormatException>(() => DateTime.Parse("2020-5-7T09:37:00.0000000-07:00c"));
            Assert.Throws<FormatException>(() => DateTime.Parse("2020-5-7T09:37:00.0000000-07:00c", new MyFormatter()));
            Assert.Throws<FormatException>(() => DateTime.Parse("2020-5-7T09:37:00.0000000-07:00c", new MyFormatter(), DateTimeStyles.NoCurrentDateDefault));

            Assert.Throws<FormatException>(() => DateTime.Parse("2020-5-7T09:37:00.0000000+00:00#"));
            Assert.Throws<FormatException>(() => DateTime.Parse("2020-5-7T09:37:00.0000000+00:00#", new MyFormatter()));
            Assert.Throws<FormatException>(() => DateTime.Parse("2020-5-7T09:37:00.0000000+00:00#", new MyFormatter(), DateTimeStyles.NoCurrentDateDefault));

            Assert.Throws<FormatException>(() => DateTime.Parse("2020-5-7T09:37:00.0000000+00:00#\0"));
            Assert.Throws<FormatException>(() => DateTime.Parse("2020-5-7T09:37:00.0000000+00:00#\0", new MyFormatter()));
            Assert.Throws<FormatException>(() => DateTime.Parse("2020-5-7T09:37:00.0000000+00:00#\0", new MyFormatter(), DateTimeStyles.NoCurrentDateDefault));
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
            var r = new Random(42);
            for (int i = 0; i < 200; i++) // test with a bunch of random dates
            {
                DateTime dt = new DateTime(DateTime.MinValue.Ticks + (long)(r.NextDouble() * (DateTime.MaxValue.Ticks - DateTime.MinValue.Ticks)), DateTimeKind.Unspecified);
                string expected = dt.ToString(standardFormat);

                Assert.Equal(expected, DateTime.ParseExact(expected, standardFormat, null).ToString(standardFormat));
                Assert.Equal(expected, DateTime.ParseExact(expected, standardFormat, null, DateTimeStyles.None).ToString(standardFormat));
                Assert.Equal(expected, DateTime.ParseExact(expected, new[] { standardFormat }, null, DateTimeStyles.None).ToString(standardFormat));
                Assert.Equal(expected, DateTime.ParseExact(expected, new[] { standardFormat }, null, DateTimeStyles.AllowWhiteSpaces).ToString(standardFormat));

                Assert.True(DateTime.TryParseExact(expected, standardFormat, null, DateTimeStyles.None, out DateTime actual));
                Assert.Equal(expected, actual.ToString(standardFormat));
                Assert.True(DateTime.TryParseExact(expected, new[] { standardFormat }, null, DateTimeStyles.None, out actual));
                Assert.Equal(expected, actual.ToString(standardFormat));

                // Should also parse with Parse, though may not round trip exactly
                DateTime.Parse(expected);
            }
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

        [Theory]
        [MemberData(nameof(ParseExact_TestData_R))]
        public static void ParseExact_String_String_FormatProvider_DateTimeStyles_R(DateTime dt, string input)
        {
            Assert.Equal(DateTimeKind.Unspecified, DateTime.ParseExact(input, "r", null).Kind);

            Assert.Equal(dt.ToString("r"), DateTime.ParseExact(input, "r", null).ToString("r"));
            Assert.Equal(dt.ToString("r"), DateTime.ParseExact(input, "r", null, DateTimeStyles.None).ToString("r"));

            const string Whitespace = " \t\r\n ";
            Assert.Equal(dt.ToString("r"), DateTime.ParseExact(Whitespace + input, "r", null, DateTimeStyles.AllowLeadingWhite).ToString("r"));
            Assert.Equal(dt.ToString("r"), DateTime.ParseExact(input + Whitespace, "r", null, DateTimeStyles.AllowTrailingWhite).ToString("r"));
            Assert.Equal(dt.ToString("r"), DateTime.ParseExact(
                Whitespace +
                input +
                Whitespace, "r", null, DateTimeStyles.AllowLeadingWhite | DateTimeStyles.AllowTrailingWhite).ToString("r"));
            Assert.Equal(dt.ToString("r"), DateTime.ParseExact(
                input.Substring(0, 4) +
                Whitespace +
                input.Substring(4), "r", null, DateTimeStyles.AllowInnerWhite).ToString("r"));
            Assert.Equal(dt.ToString("r"), DateTime.ParseExact(
                Whitespace +
                input.Substring(0, 4) +
                Whitespace +
                input.Substring(4) +
                Whitespace, "r", null, DateTimeStyles.AllowWhiteSpaces).ToString("r"));
        }

        public static IEnumerable<object[]> ParseExact_TestData_R()
        {
            // Lowest, highest, and random DateTime in lower, upper, and normal casing
            var pairs = new (DateTime, string)[]
            {
                (DateTime.MaxValue, "Fri, 31 Dec 9999 23:59:59"),
                (DateTime.MinValue, "Mon, 01 Jan 0001 00:00:00"),
                (new DateTime(1906, 8, 15, 7, 24, 5, 300), "Wed, 15 Aug 1906 07:24:05"),
            };
            foreach ((DateTime, string) pair in pairs)
            {
                yield return new object[] { pair.Item1, pair.Item2 + " GMT" };
                yield return new object[] { pair.Item1, pair.Item2.ToLowerInvariant() + " GMT" };
                yield return new object[] { pair.Item1, pair.Item2.ToUpperInvariant() + " GMT" };
            }

            // All months
            DateTime dt = DateTime.UtcNow;
            for (int i = 0; i < 12; i++)
            {
                dt = dt.AddMonths(1);
                yield return new object[] { dt, dt.ToString("R") };
            }

            // All days
            for (int i = 0; i < 7; i++)
            {
                dt = dt.AddDays(1);
                yield return new object[] { dt, dt.ToString("R") };
            }
        }

        [Theory]
        [MemberData(nameof(ParseExact_TestData_InvalidData_R))]
        public static void ParseExact_InvalidData_R(string invalidString)
        {
            Assert.Throws<FormatException>(() => DateTime.ParseExact(invalidString, "r", null));
            Assert.Throws<FormatException>(() => DateTime.ParseExact(invalidString, "r", null, DateTimeStyles.None));
            Assert.Throws<FormatException>(() => DateTime.ParseExact(invalidString, new string[] { "r" }, null, DateTimeStyles.None));
        }

        public static IEnumerable<object[]> ParseExact_TestData_InvalidData_R()
        {
            yield return new object[] { "Thu, 15 Aug 1906 07:24:05 GMT" }; // invalid day of week
            yield return new object[] { "Ste, 15 Aug 1906 07:24:05 GMT" }; // invalid day of week
            yield return new object[] { "We, 15 Aug 1906 07:24:05 GMT" }; // too short day of week
            yield return new object[] { "Wedn, 15 Aug 1906 07:24:05 GMT" }; // too long day of week

            yield return new object[] { "Wed, 32 Aug 1906 07:24:05 GMT" }; // too large day
            yield return new object[] { "Wed, -1 Aug 1906 07:24:05 GMT" }; // too small day

            yield return new object[] { "Wed, 15 Au 1906 07:24:05 GMT" }; // too small month
            yield return new object[] { "Wed, 15 August 1906 07:24:05 GMT" }; // too large month

            yield return new object[] { "Wed, 15 Aug -1 07:24:05 GMT" }; // too small year
            yield return new object[] { "Wed, 15 Aug 10000 07:24:05 GMT" }; // too large year

            yield return new object[] { "Wed, 15 Aug 1906 24:24:05 GMT" }; // too large hour
            yield return new object[] { "Wed, 15 Aug 1906 07:60:05 GMT" }; // too large minute
            yield return new object[] { "Wed, 15 Aug 1906 07:24:60 GMT" }; // too large second

            yield return new object[] { "Wed, 15 Aug 1906 07:24:05 STE" }; // invalid timezone
            yield return new object[] { "Wed, 15 Aug 1906 07:24:05 GM" }; // too short timezone
            yield return new object[] { "Wed, 15 Aug 1906 07:24:05 GMTT" }; // too long timezone
            yield return new object[] { "Wed, 15 Aug 1906 07:24:05 gmt" }; // wrong casing
            yield return new object[] { "Wed, 15 Aug 1906 07:24:05 Z" }; // zulu invalid
            yield return new object[] { "Wed, 15 Aug 1906 07:24:05 UTC" }; // UTC invalid

            yield return new object[] { " Wed, 15 Aug 1906 07:24:05 GMT" }; // whitespace before
            yield return new object[] { "Wed, 15 Aug 1906 07:24:05 GMT " }; // whitespace after
            yield return new object[] { "Wed, 15 Aug 1906  07:24:05 GMT" }; // extra whitespace middle
            yield return new object[] { "Wed, 15 Aug 1906 07: 24:05 GMT" }; // extra whitespace middle

            yield return new object[] { "Wed,\t15 Aug 1906 07:24:05 GMT" }; // wrong whitespace for first space
            yield return new object[] { "Wed, 15\tAug 1906 07:24:05 GMT" }; // wrong whitespace for second space
            yield return new object[] { "Wed, 15 Aug\t1906 07:24:05 GMT" }; // wrong whitespace for third space
            yield return new object[] { "Wed, 15 Aug 1906\t07:24:05 GMT" }; // wrong whitespace for fourth space
            yield return new object[] { "Wed, 15 Aug 1906 07:24:05\tGMT" }; // wrong whitespace for fifth space
            yield return new object[] { "Wed; 15 Aug 1906 07:24:05 GMT" }; // wrong comma
            yield return new object[] { "Wed\x642C 15 Aug 1906 07:24:05 GMT" }; // wrong comma
            yield return new object[] { "Wed, 15 Aug 1906 07;24:05 GMT" }; // wrong first colon
            yield return new object[] { "Wed, 15 Aug 1906 07:24;05 GMT" }; // wrong second colon

            yield return new object[] { "\x2057ed, 15 Aug 1906 07:24:05 GMT" }; // invalid characters to validate ASCII checks on day of week
            yield return new object[] { "W\x5765d, 15 Aug 1906 07:24:05 GMT" }; // invalid characters to validate ASCII checks on day of week
            yield return new object[] { "We\x6564, 15 Aug 1906 07:24:05 GMT" }; // invalid characters to validate ASCII checks on day of week

            yield return new object[] { "Wed, 15 \x2041ug 1906 07:24:05 GMT" }; // invalid characters to validate ASCII checks on month
            yield return new object[] { "Wed, 15 A\x4175g 1906 07:24:05 GMT" }; // invalid characters to validate ASCII checks on month
            yield return new object[] { "Wed, 15 Au\x7567 1906 07:24:05 GMT" }; // invalid characters to validate ASCII checks on month

            yield return new object[] { "Wed, 15 Aug 1906 07:24:05 \x2047MT" }; // invalid characters to validate ASCII checks on GMT
            yield return new object[] { "Wed, 15 Aug 1906 07:24:05 G\x474DT" }; // invalid characters to validate ASCII checks on GMT
            yield return new object[] { "Wed, 15 Aug 1906 07:24:05 GM\x4D54" }; // invalid characters to validate ASCII checks on GMT

            yield return new object[] { "Wed, A5 Aug 1906 07:24:05 GMT" }; // invalid digits
            yield return new object[] { "Wed, 1A Aug 1906 07:24:05 GMT" }; // invalid digits
            yield return new object[] { "Wed, 15 Aug A906 07:24:05 GMT" }; // invalid digits
            yield return new object[] { "Wed, 15 Aug 1A06 07:24:05 GMT" }; // invalid digits
            yield return new object[] { "Wed, 15 Aug 19A6 07:24:05 GMT" }; // invalid digits
            yield return new object[] { "Wed, 15 Aug 190A 07:24:05 GMT" }; // invalid digits
            yield return new object[] { "Wed, 15 Aug 1906 A7:24:05 GMT" }; // invalid digits
            yield return new object[] { "Wed, 15 Aug 1906 0A:24:05 GMT" }; // invalid digits
            yield return new object[] { "Wed, 15 Aug 1906 07:A4:05 GMT" }; // invalid digits
            yield return new object[] { "Wed, 15 Aug 1906 07:2A:05 GMT" }; // invalid digits
            yield return new object[] { "Wed, 15 Aug 1906 07:24:A5 GMT" }; // invalid digits
            yield return new object[] { "Wed, 15 Aug 1906 07:24:0A GMT" }; // invalid digits
        }

        [Theory]
        [MemberData(nameof(ParseExact_TestData_O))]
        public static void ParseExact_String_String_FormatProvider_DateTimeStyles_O(DateTime dt, string input)
        {
            string expectedString;
            if (input.Length == 27) // no timezone
            {
                Assert.Equal(DateTimeKind.Unspecified, DateTime.ParseExact(input, "o", null).Kind);
                expectedString = dt.ToString("o");
            }
            else // "Z" or +/- offset
            {
                Assert.Equal(DateTimeKind.Local, DateTime.ParseExact(input, "o", null).Kind);
                expectedString = dt.ToLocalTime().ToString("o");
            }

            Assert.Equal(expectedString, DateTime.ParseExact(input, "o", null).ToString("o"));
            Assert.Equal(expectedString, DateTime.ParseExact(input, "o", null, DateTimeStyles.None).ToString("o"));

            const string Whitespace = " \t\r\n ";
            Assert.Equal(expectedString, DateTime.ParseExact(Whitespace + input, "o", null, DateTimeStyles.AllowLeadingWhite).ToString("o"));
            Assert.Equal(expectedString, DateTime.ParseExact(input + Whitespace, "o", null, DateTimeStyles.AllowTrailingWhite).ToString("o"));
            Assert.Equal(expectedString, DateTime.ParseExact(
                Whitespace +
                input +
                Whitespace, "o", null, DateTimeStyles.AllowLeadingWhite | DateTimeStyles.AllowTrailingWhite).ToString("o"));
            Assert.Equal(expectedString, DateTime.ParseExact(
                input.Substring(0, 27) +
                Whitespace +
                input.Substring(27), "o", null, DateTimeStyles.AllowInnerWhite).ToString("o"));
            Assert.Equal(expectedString, DateTime.ParseExact(
                Whitespace +
                input.Substring(0, 27) +
                Whitespace +
                input.Substring(27) +
                Whitespace, "o", null, DateTimeStyles.AllowWhiteSpaces).ToString("o"));
        }

        public static IEnumerable<object[]> ParseExact_TestData_O()
        {
            // Arbitrary DateTime in each of Unspecified, Utc, and Local kinds.
            foreach (DateTimeKind kind in new[] { DateTimeKind.Unspecified, DateTimeKind.Utc, DateTimeKind.Local })
            {
                var dt = new DateTime(1234567891234567891, kind);
                yield return new object[] { dt, dt.ToString("o") };
            }

            // Min and max in each of Unspecified, Utc, and Local kinds.
            foreach (DateTime dt in new[] { DateTime.MinValue, DateTime.MaxValue })
            {
                yield return new object[] { dt, dt.ToString("o") };
                yield return new object[] { dt.ToUniversalTime(), dt.ToUniversalTime().ToString("o") };
                yield return new object[] { dt.ToLocalTime(), dt.ToLocalTime().ToString("o") };
            }

            // 1-digit offset hour is accepted due to legacy/compat
            yield return new object[] { new DateTime(636664076235238523, DateTimeKind.Utc), "2018-07-05T18:36:43.5238523+1:23" };
        }

        [Theory]
        [MemberData(nameof(ParseExact_TestData_InvalidData_O))]
        public static void ParseExact_InvalidData_O(string invalidString)
        {
            Assert.Throws<FormatException>(() => DateTime.ParseExact(invalidString, "o", null));
            Assert.Throws<FormatException>(() => DateTime.ParseExact(invalidString, "o", null, DateTimeStyles.None));
            Assert.Throws<FormatException>(() => DateTime.ParseExact(invalidString, new string[] { "o" }, null, DateTimeStyles.None));
        }

        public static IEnumerable<object[]> ParseExact_TestData_InvalidData_O()
        {
            yield return new object[] { " 2018-07-05T18:36:43.5238523" }; // whitespace before
            yield return new object[] { " 2018-07-05T18:36:43.5238523Z" }; // whitespace before
            yield return new object[] { " 2018-07-05T18:36:43.5238523+00:00" }; // whitespace before
            yield return new object[] { "2018-07-05T18:36:43.5238523 " }; // whitespace after
            yield return new object[] { "2018-07-05T18:36:43.5238523Z " }; // whitespace after
            yield return new object[] { "2018-07-05T18:36:43.5238523+00:00 " }; // whitespace after
            yield return new object[] { "2018-07-05T18:36:43.5238523 Z" }; // whitespace inside
            yield return new object[] { "2018-07-05T18:36:43.5238523 +00:00" }; // whitespace inside

            yield return new object[] { "201-07-05T18:36:43.5238523" }; // too short year
            yield return new object[] { "20181-07-05T18:36:43.5238523" }; // too long year
            yield return new object[] { "2018-7-05T18:36:43.5238523" }; // too short month
            yield return new object[] { "2018-017-05T18:36:43.5238523" }; // too long month
            yield return new object[] { "2018-07-5T18:36:43.5238523" }; // too short day
            yield return new object[] { "2018-07-015T18:36:43.5238523" }; // too long day
            yield return new object[] { "2018-07-05T018:36:43.5238523" }; // too long hour
            yield return new object[] { "2018-07-05T8:36:43.5238523" }; // too short hour
            yield return new object[] { "2018-07-05T18:6:43.5238523" }; // too short minute
            yield return new object[] { "2018-07-05T18:036:43.5238523" }; // too long minute
            yield return new object[] { "2018-07-05T18:06:3.5238523" }; // too short second
            yield return new object[] { "2018-07-05T18:36:043.5238523" }; // too long second
            yield return new object[] { "2018-07-05T18:06:03.238523" }; // too short fraction
            yield return new object[] { "2018-07-05T18:36:43.15238523" }; // too long fraction
            yield return new object[] { "2018-07-05T18:36:43.5238523+001:00" }; // too long offset hour
            yield return new object[] { "2018-07-05T18:36:43.5238523+01:0" }; // too short offset minute
            yield return new object[] { "2018-07-05T18:36:43.5238523+01:000" }; // too long offset minute

            yield return new object[] { "2018=07-05T18:36:43.5238523" }; // invalid first hyphen
            yield return new object[] { "2018-07=05T18:36:43.5238523" }; // invalid second hyphen
            yield return new object[] { "2018-07-05A18:36:43.5238523" }; // invalid T
            yield return new object[] { "2018-07-05T18;36:43.5238523" }; // invalid first colon
            yield return new object[] { "2018-07-05T18:36;43.5238523" }; // invalid second colon
            yield return new object[] { "2018-07-05T18:36:43,5238523" }; // invalid period
            yield return new object[] { "2018-07-05T18:36:43.5238523,00:00" }; // invalid +/-/Z
            yield return new object[] { "2018-07-05T18:36:43.5238523+00;00" }; // invalid third colon
            yield return new object[] { "2018-07-05T18:36:43.5238523+1;00" }; // invalid colon with 1-digit offset hour

            yield return new object[] { "a018-07-05T18:36:43.5238523" }; // invalid digits
            yield return new object[] { "2a18-07-05T18:36:43.5238523" }; // invalid digits
            yield return new object[] { "20a8-07-05T18:36:43.5238523" }; // invalid digits
            yield return new object[] { "201a-07-05T18:36:43.5238523" }; // invalid digits
            yield return new object[] { "2018-a7-05T18:36:43.5238523" }; // invalid digits
            yield return new object[] { "2018-0a-05T18:36:43.5238523" }; // invalid digits
            yield return new object[] { "2018-07-a5T18:36:43.5238523" }; // invalid digits
            yield return new object[] { "2018-07-0aT18:36:43.5238523" }; // invalid digits
            yield return new object[] { "2018-07-05Ta8:36:43.5238523" }; // invalid digits
            yield return new object[] { "2018-07-05T1a:36:43.5238523" }; // invalid digits
            yield return new object[] { "2018-07-05T18:a6:43.5238523" }; // invalid digits
            yield return new object[] { "2018-07-05T18:3a:43.5238523" }; // invalid digits
            yield return new object[] { "2018-07-05T18:36:a3.5238523" }; // invalid digits
            yield return new object[] { "2018-07-05T18:36:4a.5238523" }; // invalid digits
            yield return new object[] { "2018-07-05T18:36:43.a238523" }; // invalid digits
            yield return new object[] { "2018-07-05T18:36:43.5a38523" }; // invalid digits
            yield return new object[] { "2018-07-05T18:36:43.52a8523" }; // invalid digits
            yield return new object[] { "2018-07-05T18:36:43.523a523" }; // invalid digits
            yield return new object[] { "2018-07-05T18:36:43.5238a23" }; // invalid digits
            yield return new object[] { "2018-07-05T18:36:43.52385a3" }; // invalid digits
            yield return new object[] { "2018-07-05T18:36:43.523852a" }; // invalid digits
            yield return new object[] { "2018-07-05T18:36:43.5238523+a0:00" }; // invalid digits
            yield return new object[] { "2018-07-05T18:36:43.5238523+0a:00" }; // invalid digits
            yield return new object[] { "2018-07-05T18:36:43.5238523+00:a0" }; // invalid digits
            yield return new object[] { "2018-07-05T18:36:43.5238523+00:0a" }; // invalid digits
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

        [Fact]
        // Regression test for https://github.com/dotnet/runtime/issues/9565
        public static void TryParseExact_EmptyAMPMDesignator()
        {
            var englishCulture = new CultureInfo("en-US");
            englishCulture.DateTimeFormat.AMDesignator = "";
            englishCulture.DateTimeFormat.PMDesignator = "";
            Assert.False(DateTime.TryParseExact(" ", "%t", englishCulture, DateTimeStyles.None, out _));
        }

        [Fact]
        public static void ParseExact_EscapedSingleQuotes()
        {
            DateTimeFormatInfo formatInfo;
            if (PlatformDetection.IsBrowser)
            {
                formatInfo = DateTimeFormatInfo.GetInstance(new CultureInfo("id-ID"));
            }
            else
            {
                formatInfo = DateTimeFormatInfo.GetInstance(new CultureInfo("mt-MT"));
            }
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

        private static void VerifyDateTime(DateTime dateTime, int year, int month, int day, int hour, int minute,
            int second, int millisecond, int microsecond, DateTimeKind kind)
        {
            VerifyDateTime(dateTime, year, month, day, hour, minute, second, millisecond, kind);
            Assert.Equal(microsecond, dateTime.Microsecond);
        }

        private static void VerifyDateTime(DateTime dateTime, int year, int month, int day, int hour, int minute,
            int second, int millisecond, int microsecond, int nanosecond, DateTimeKind kind)
        {
            VerifyDateTime(dateTime, year, month, day, hour, minute, second, millisecond, microsecond, kind);
            Assert.Equal(nanosecond, dateTime.Nanosecond);
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

        [Fact]
        public static void TestTryParseAtBoundaries()
        {
            Assert.True(DateTime.TryParse("9999-12-31T23:59:59.9999999", out var maxDateTime),
                        "DateTime parsing expected to succeed at the boundary DateTime.MaxValue");
            Assert.Equal(DateTime.MaxValue, maxDateTime);

            Assert.False(DateTime.TryParse("9999-12-31T23:59:59.999999999Z", out var dateTime),
              "DateTime parsing expected to throw with any dates greater than DateTime.MaxValue");
        }

        public static IEnumerable<object[]> Parse_ValidInput_Succeeds_MemberData()
        {
            yield return new object[] { "1234 12", CultureInfo.InvariantCulture, new DateTime(1234, 12, 1, 0, 0, 0) };
            yield return new object[] { "12 1234", CultureInfo.InvariantCulture, new DateTime(1234, 12, 1, 0, 0, 0) };
            yield return new object[] { "12 1234 11", CultureInfo.InvariantCulture, new DateTime(1234, 12, 11, 0, 0, 0) };
            yield return new object[] { "1234 12 13", CultureInfo.InvariantCulture, new DateTime(1234, 12, 13, 0, 0, 0) };
            yield return new object[] { "12 13 1234", CultureInfo.InvariantCulture, new DateTime(1234, 12, 13, 0, 0, 0) };
            yield return new object[] { "1 1 1", CultureInfo.InvariantCulture, new DateTime(2001, 1, 1, 0, 0, 0) };
            yield return new object[] { "2 2 2Z", CultureInfo.InvariantCulture, TimeZoneInfo.ConvertTimeFromUtc(new DateTime(2002, 2, 2, 0, 0, 0, DateTimeKind.Utc), TimeZoneInfo.Local) };
            yield return new object[] { "#10/10/2095#\0", CultureInfo.InvariantCulture, new DateTime(2095, 10, 10, 0, 0, 0) };

            yield return new object[] { "2020-5-7T09:37:00.0000000+00:00\0", CultureInfo.InvariantCulture, TimeZoneInfo.ConvertTimeFromUtc(new DateTime(2020, 5, 7, 9, 37, 0, DateTimeKind.Utc), TimeZoneInfo.Local) };
            yield return new object[] { "#2020-5-7T09:37:00.0000000+00:00#", CultureInfo.InvariantCulture, TimeZoneInfo.ConvertTimeFromUtc(new DateTime(2020, 5, 7, 9, 37, 0, DateTimeKind.Utc), TimeZoneInfo.Local) };
            yield return new object[] { "#2020-5-7T09:37:00.0000000+00:00#\0", CultureInfo.InvariantCulture, TimeZoneInfo.ConvertTimeFromUtc(new DateTime(2020, 5, 7, 9, 37, 0, DateTimeKind.Utc), TimeZoneInfo.Local) };
            yield return new object[] { "2020-5-7T09:37:00.0000000+00:00", CultureInfo.InvariantCulture, TimeZoneInfo.ConvertTimeFromUtc(new DateTime(2020, 5, 7, 9, 37, 0, DateTimeKind.Utc), TimeZoneInfo.Local) };

            if (PlatformDetection.IsNotInvariantGlobalization)
            {
                DateTime today = DateTime.Today;
                var hebrewCulture = new CultureInfo("he-IL");
                hebrewCulture.DateTimeFormat.Calendar = new HebrewCalendar();
                yield return new object[] { today.ToString(hebrewCulture), hebrewCulture, today };

                CultureInfo culture;
                if (PlatformDetection.IsBrowser)
                {
                    culture = new CultureInfo("pl-PL");
                }
                else
                {
                    culture = new CultureInfo("mn-MN");
                }
                yield return new object[] { today.ToString(culture), culture, today };
            }
        }

        [Theory]
        [MemberData(nameof(Parse_ValidInput_Succeeds_MemberData))]
        public static void Parse_ValidInput_Succeeds(string input, CultureInfo culture, DateTime? expected)
        {
            Assert.Equal(expected, DateTime.Parse(input, culture));
        }

        public static IEnumerable<object[]> ParseExact_ValidInput_Succeeds_MemberData()
        {
            foreach (DateTimeStyles style in new[] { DateTimeStyles.None, DateTimeStyles.AllowWhiteSpaces })
            {
                yield return new object[] { "9", "%d", CultureInfo.InvariantCulture, style, new DateTime(DateTime.Now.Year, 1, 9, 0, 0, 0) };
                yield return new object[] { "15", "dd", CultureInfo.InvariantCulture, style, new DateTime(DateTime.Now.Year, 1, 15, 0, 0, 0) };

                yield return new object[] { "9", "%M", CultureInfo.InvariantCulture, style, new DateTime(DateTime.Now.Year, 9, 1, 0, 0, 0) };
                yield return new object[] { "09", "MM", CultureInfo.InvariantCulture, style, new DateTime(DateTime.Now.Year, 9, 1, 0, 0, 0) };
                yield return new object[] { "Sep", "MMM", CultureInfo.InvariantCulture, style, new DateTime(DateTime.Now.Year, 9, 1, 0, 0, 0) };
                yield return new object[] { "September", "MMMM", CultureInfo.InvariantCulture, style, new DateTime(DateTime.Now.Year, 9, 1, 0, 0, 0) };

                yield return new object[] { "1", "%y", CultureInfo.InvariantCulture, style, new DateTime(2001, 1, 1, 0, 0, 0) };
                yield return new object[] { "01", "yy", CultureInfo.InvariantCulture, style, new DateTime(2001, 1, 1, 0, 0, 0) };
                yield return new object[] { "2001", "yyyy", CultureInfo.InvariantCulture, style, new DateTime(2001, 1, 1, 0, 0, 0) };

                yield return new object[] { "3", "%H", CultureInfo.InvariantCulture, style, DateTime.Today + TimeSpan.FromHours(3) };
                yield return new object[] { "03", "HH", CultureInfo.InvariantCulture, style, DateTime.Today + TimeSpan.FromHours(3) };

                yield return new object[] { "3A", "ht", CultureInfo.InvariantCulture, style, DateTime.Today + TimeSpan.FromHours(3) };
                yield return new object[] { "03A", "hht", CultureInfo.InvariantCulture, style, DateTime.Today + TimeSpan.FromHours(3) };
                yield return new object[] { "3P", "ht", CultureInfo.InvariantCulture, style, DateTime.Today + TimeSpan.FromHours(12 + 3) };
                yield return new object[] { "03P", "hht", CultureInfo.InvariantCulture, style, DateTime.Today + TimeSpan.FromHours(12 + 3) };

                yield return new object[] { "2017-10-11 01:23:45Z", "u", CultureInfo.InvariantCulture, style, new DateTime(2017, 10, 11, 1, 23, 45) };
                yield return new object[] { "9/8/2017 10:11:12 AM", "M/d/yyyy HH':'mm':'ss tt", CultureInfo.InvariantCulture, style, new DateTime(2017, 9, 8, 10, 11, 12) };
                yield return new object[] { "9/8/2017 20:11:12 PM", "M/d/yyyy HH':'mm':'ss tt", CultureInfo.InvariantCulture, style, new DateTime(2017, 9, 8, 20, 11, 12) };
                yield return new object[] { "Fri, 08 Sep 2017 11:18:19 -0000", "ddd, d MMM yyyy H:m:s zzz", new CultureInfo("en-US"), DateTimeStyles.AllowInnerWhite, new DateTime(2017, 9, 8, 11, 18, 19, DateTimeKind.Utc) };
                yield return new object[] { "1234-05-06T07:00:00.8Z", "yyyy-MM-dd'T'HH:mm:ss.FFF'Z'", CultureInfo.InvariantCulture, style, new DateTime(1234, 5, 6, 7, 0, 0, 800) };
                yield return new object[] { "1234-05-06T07:00:00Z", "yyyy-MM-dd'T'HH:mm:ss.FFF'Z'", CultureInfo.InvariantCulture, style, new DateTime(1234, 5, 6, 7, 0, 0, 0) };
                yield return new object[] { "1234-05-06T07:00:00Z", "yyyy-MM-dd'T'HH:mm:ssFFF'Z'", CultureInfo.InvariantCulture, style, new DateTime(1234, 5, 6, 7, 0, 0, 0) };
                yield return new object[] { "1234-05-06T07:00:00Z", "yyyy-MM-dd'T'HH:mm:ssFFF'Z'", CultureInfo.InvariantCulture, style, new DateTime(1234, 5, 6, 7, 0, 0, 0) };
                yield return new object[] { "1234-05-06T07:00:00Z", "yyyy-MM-dd'T'HH:mm:ssFFFZ", CultureInfo.InvariantCulture, style, TimeZoneInfo.ConvertTimeFromUtc(new DateTime(1234, 5, 6, 7, 0, 0, DateTimeKind.Utc), TimeZoneInfo.Local) };
                yield return new object[] { "1234-05-06T07:00:00GMT", "yyyy-MM-dd'T'HH:mm:ssFFFZ", CultureInfo.InvariantCulture, style, TimeZoneInfo.ConvertTimeFromUtc(new DateTime(1234, 5, 6, 7, 0, 0, DateTimeKind.Utc), TimeZoneInfo.Local) };
            }

            yield return new object[] { "9", "\"  \"%d", CultureInfo.InvariantCulture, DateTimeStyles.AllowLeadingWhite, new DateTime(DateTime.Now.Year, 1, 9, 0, 0, 0) };
            yield return new object[] { "15", "\' \'dd", CultureInfo.InvariantCulture, DateTimeStyles.AllowLeadingWhite, new DateTime(DateTime.Now.Year, 1, 15, 0, 0, 0) };

            yield return new object[] { "9", "\"  \"%M", CultureInfo.InvariantCulture, DateTimeStyles.AllowLeadingWhite, new DateTime(DateTime.Now.Year, 9, 1, 0, 0, 0) };
            yield return new object[] { "09", "\" \"MM", CultureInfo.InvariantCulture, DateTimeStyles.AllowLeadingWhite, new DateTime(DateTime.Now.Year, 9, 1, 0, 0, 0) };
            yield return new object[] { "Sep", "\"  \"MMM", CultureInfo.InvariantCulture, DateTimeStyles.AllowLeadingWhite, new DateTime(DateTime.Now.Year, 9, 1, 0, 0, 0) };
            yield return new object[] { "September", "\' \'MMMM", CultureInfo.InvariantCulture, DateTimeStyles.AllowLeadingWhite, new DateTime(DateTime.Now.Year, 9, 1, 0, 0, 0) };

            yield return new object[] { "1", "\' \'%y", CultureInfo.InvariantCulture, DateTimeStyles.AllowLeadingWhite, new DateTime(2001, 1, 1, 0, 0, 0) };
            yield return new object[] { "01", "\"  \"yy", CultureInfo.InvariantCulture, DateTimeStyles.AllowLeadingWhite, new DateTime(2001, 1, 1, 0, 0, 0) };
            yield return new object[] { "2001", "\" \"yyyy", CultureInfo.InvariantCulture, DateTimeStyles.AllowLeadingWhite, new DateTime(2001, 1, 1, 0, 0, 0) };

            yield return new object[] { "3", "\"  \"%H", CultureInfo.InvariantCulture, DateTimeStyles.AllowLeadingWhite, DateTime.Today + TimeSpan.FromHours(3) };
            yield return new object[] { "03", "\" \"HH", CultureInfo.InvariantCulture, DateTimeStyles.AllowLeadingWhite, DateTime.Today + TimeSpan.FromHours(3) };

            yield return new object[] { "3A", "\"  \"ht", CultureInfo.InvariantCulture, DateTimeStyles.AllowLeadingWhite, DateTime.Today + TimeSpan.FromHours(3) };
            yield return new object[] { "03A", "\" \"hht", CultureInfo.InvariantCulture, DateTimeStyles.AllowLeadingWhite, DateTime.Today + TimeSpan.FromHours(3) };
            yield return new object[] { "3P", "\'  \'ht", CultureInfo.InvariantCulture, DateTimeStyles.AllowLeadingWhite, DateTime.Today + TimeSpan.FromHours(12 + 3) };
            yield return new object[] { "03P", "\" \"hht", CultureInfo.InvariantCulture, DateTimeStyles.AllowLeadingWhite, DateTime.Today + TimeSpan.FromHours(12 + 3) };

            yield return new object[] { "2017-10-11 01:23:45Z", "u", CultureInfo.InvariantCulture, DateTimeStyles.AllowLeadingWhite, new DateTime(2017, 10, 11, 1, 23, 45) };
            yield return new object[] { "9/8/2017 10:11:12 AM", "\'  \'M/d/yyyy HH':'mm':'ss tt", CultureInfo.InvariantCulture, DateTimeStyles.AllowLeadingWhite, new DateTime(2017, 9, 8, 10, 11, 12) };
            yield return new object[] { "9/8/2017 20:11:12 PM", "\" \"M/d/yyyy HH':'mm':'ss tt", CultureInfo.InvariantCulture, DateTimeStyles.AllowLeadingWhite, new DateTime(2017, 9, 8, 20, 11, 12) };
            yield return new object[] { "1234-05-06T07:00:00.8Z", "\" \"yyyy-MM-dd'T'HH:mm:ss.FFF'Z'", CultureInfo.InvariantCulture, DateTimeStyles.AllowLeadingWhite, new DateTime(1234, 5, 6, 7, 0, 0, 800) };
            yield return new object[] { "1234-05-06T07:00:00Z", "\"  \"yyyy-MM-dd'T'HH:mm:ss.FFF'Z'", CultureInfo.InvariantCulture, DateTimeStyles.AllowLeadingWhite, new DateTime(1234, 5, 6, 7, 0, 0, 0) };
            yield return new object[] { "1234-05-06T07:00:00Z", "\' \'yyyy-MM-dd'T'HH:mm:ssFFF'Z'", CultureInfo.InvariantCulture, DateTimeStyles.AllowLeadingWhite, new DateTime(1234, 5, 6, 7, 0, 0, 0) };
            yield return new object[] { "1234-05-06T07:00:00Z", "\'  \'yyyy-MM-dd'T'HH:mm:ssFFF'Z'", CultureInfo.InvariantCulture, DateTimeStyles.AllowLeadingWhite, new DateTime(1234, 5, 6, 7, 0, 0, 0) };
            yield return new object[] { "1234-05-06T07:00:00Z", "\" \"yyyy-MM-dd'T'HH:mm:ssFFFZ", CultureInfo.InvariantCulture, DateTimeStyles.AllowLeadingWhite, TimeZoneInfo.ConvertTimeFromUtc(new DateTime(1234, 5, 6, 7, 0, 0, DateTimeKind.Utc), TimeZoneInfo.Local) };
            yield return new object[] { "1234-05-06T07:00:00GMT", "\"  \"yyyy-MM-dd'T'HH:mm:ssFFFZ", CultureInfo.InvariantCulture, DateTimeStyles.AllowLeadingWhite, TimeZoneInfo.ConvertTimeFromUtc(new DateTime(1234, 5, 6, 7, 0, 0, DateTimeKind.Utc), TimeZoneInfo.Local) };


            yield return new object[] { "9", "%d\"  \"", CultureInfo.InvariantCulture, DateTimeStyles.AllowTrailingWhite, new DateTime(DateTime.Now.Year, 1, 9, 0, 0, 0) };
            yield return new object[] { "15", "dd\' \'", CultureInfo.InvariantCulture, DateTimeStyles.AllowTrailingWhite, new DateTime(DateTime.Now.Year, 1, 15, 0, 0, 0) };

            yield return new object[] { "9", "%M\"  \"", CultureInfo.InvariantCulture, DateTimeStyles.AllowTrailingWhite, new DateTime(DateTime.Now.Year, 9, 1, 0, 0, 0) };
            yield return new object[] { "09", "MM\" \"", CultureInfo.InvariantCulture, DateTimeStyles.AllowTrailingWhite, new DateTime(DateTime.Now.Year, 9, 1, 0, 0, 0) };
            yield return new object[] { "Sep", "MMM\"  \"", CultureInfo.InvariantCulture, DateTimeStyles.AllowTrailingWhite, new DateTime(DateTime.Now.Year, 9, 1, 0, 0, 0) };
            yield return new object[] { "September", "MMMM\' \'", CultureInfo.InvariantCulture, DateTimeStyles.AllowTrailingWhite, new DateTime(DateTime.Now.Year, 9, 1, 0, 0, 0) };

            yield return new object[] { "1", "%y\' \'", CultureInfo.InvariantCulture, DateTimeStyles.AllowTrailingWhite, new DateTime(2001, 1, 1, 0, 0, 0) };
            yield return new object[] { "01", "yy\"  \"", CultureInfo.InvariantCulture, DateTimeStyles.AllowTrailingWhite, new DateTime(2001, 1, 1, 0, 0, 0) };
            yield return new object[] { "2001", "yyyy\" \"", CultureInfo.InvariantCulture, DateTimeStyles.AllowTrailingWhite, new DateTime(2001, 1, 1, 0, 0, 0) };

            yield return new object[] { "3", "%H\"  \"", CultureInfo.InvariantCulture, DateTimeStyles.AllowTrailingWhite, DateTime.Today + TimeSpan.FromHours(3) };
            yield return new object[] { "03", "HH\" \"", CultureInfo.InvariantCulture, DateTimeStyles.AllowTrailingWhite, DateTime.Today + TimeSpan.FromHours(3) };

            yield return new object[] { "3A", "ht\"  \"", CultureInfo.InvariantCulture, DateTimeStyles.AllowTrailingWhite, DateTime.Today + TimeSpan.FromHours(3) };
            yield return new object[] { "03A", "hht\" \"", CultureInfo.InvariantCulture, DateTimeStyles.AllowTrailingWhite, DateTime.Today + TimeSpan.FromHours(3) };
            yield return new object[] { "3P", "ht\'  \'", CultureInfo.InvariantCulture, DateTimeStyles.AllowTrailingWhite, DateTime.Today + TimeSpan.FromHours(12 + 3) };
            yield return new object[] { "03P", "hht\" \"", CultureInfo.InvariantCulture, DateTimeStyles.AllowTrailingWhite, DateTime.Today + TimeSpan.FromHours(12 + 3) };

            yield return new object[] { "2017-10-11 01:23:45Z", "u", CultureInfo.InvariantCulture, DateTimeStyles.AllowTrailingWhite, new DateTime(2017, 10, 11, 1, 23, 45) };
            yield return new object[] { "9/8/2017 10:11:12 AM", "M/d/yyyy HH':'mm':'ss tt\'  \'", CultureInfo.InvariantCulture, DateTimeStyles.AllowTrailingWhite, new DateTime(2017, 9, 8, 10, 11, 12) };
            yield return new object[] { "9/8/2017 20:11:12 PM", "M/d/yyyy HH':'mm':'ss tt\" \"", CultureInfo.InvariantCulture, DateTimeStyles.AllowTrailingWhite, new DateTime(2017, 9, 8, 20, 11, 12) };
            yield return new object[] { "1234-05-06T07:00:00.8Z", "yyyy-MM-dd'T'HH:mm:ss.FFF'Z'\" \"", CultureInfo.InvariantCulture, DateTimeStyles.AllowTrailingWhite, new DateTime(1234, 5, 6, 7, 0, 0, 800) };
            yield return new object[] { "1234-05-06T07:00:00Z", "yyyy-MM-dd'T'HH:mm:ss.FFF'Z'\"  \"", CultureInfo.InvariantCulture, DateTimeStyles.AllowTrailingWhite, new DateTime(1234, 5, 6, 7, 0, 0, 0) };
            yield return new object[] { "1234-05-06T07:00:00Z", "yyyy-MM-dd'T'HH:mm:ssFFF'Z'\' \'", CultureInfo.InvariantCulture, DateTimeStyles.AllowTrailingWhite, new DateTime(1234, 5, 6, 7, 0, 0, 0) };
            yield return new object[] { "1234-05-06T07:00:00Z", "yyyy-MM-dd'T'HH:mm:ssFFF'Z'\'  \'", CultureInfo.InvariantCulture, DateTimeStyles.AllowTrailingWhite, new DateTime(1234, 5, 6, 7, 0, 0, 0) };
            yield return new object[] { "1234-05-06T07:00:00Z", "yyyy-MM-dd'T'HH:mm:ssFFFZ\" \"", CultureInfo.InvariantCulture, DateTimeStyles.AllowTrailingWhite, TimeZoneInfo.ConvertTimeFromUtc(new DateTime(1234, 5, 6, 7, 0, 0, DateTimeKind.Utc), TimeZoneInfo.Local) };
            yield return new object[] { "1234-05-06T07:00:00GMT", "yyyy-MM-dd'T'HH:mm:ssFFFZ\"  \"", CultureInfo.InvariantCulture, DateTimeStyles.AllowTrailingWhite, TimeZoneInfo.ConvertTimeFromUtc(new DateTime(1234, 5, 6, 7, 0, 0, DateTimeKind.Utc), TimeZoneInfo.Local) };

            yield return new object[] { "9/8/2017 10:11:12 AM                                          ", "M/d/yyyy HH':'mm':'ss tt\'  \'", CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, new DateTime(2017, 9, 8, 10, 11, 12) };
            yield return new object[] { "9/8/2017 10:11:12 AM       ", "M/d/yyyy HH':'mm':'ss tt\'  \'", CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, new DateTime(2017, 9, 8, 10, 11, 12) };
            yield return new object[] { "9/ 8    /2017    10:11:12 AM       ", "M/d/yyyy HH':'mm':'ss tt\'  \'", CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, new DateTime(2017, 9, 8, 10, 11, 12) };
            yield return new object[] { "   9   /8/2017       10:11:12 AM", "M/d/yyyy HH':'mm':'ss tt\'  \'", CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, new DateTime(2017, 9, 8, 10, 11, 12) };
            yield return new object[] { "9/8/2017 10 : 11 : 12 AM", "M/d/yyyy HH':'mm':'ss tt\'  \'", CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, new DateTime(2017, 9, 8, 10, 11, 12) };
            yield return new object[] { " 9 / 8 / 2017    10 : 11 : 12 AM", "M/d/yyyy HH':'mm':'ss tt\'  \'", CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, new DateTime(2017, 9, 8, 10, 11, 12) };
            yield return new object[] { "   9   /   8   /   2017    10  :   11  :   12  AM", "M/d/yyyy HH':'mm':'ss tt\'  \'", CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, new DateTime(2017, 9, 8, 10, 11, 12) };

            if (PlatformDetection.IsNotInvariantGlobalization)
            {
                var hebrewCulture = new CultureInfo("he-IL");
                hebrewCulture.DateTimeFormat.Calendar = new HebrewCalendar();
                DateTime today = DateTime.Today;
                foreach (string pattern in hebrewCulture.DateTimeFormat.GetAllDateTimePatterns())
                {
                    yield return new object[] { today.ToString(pattern, hebrewCulture), pattern, hebrewCulture, DateTimeStyles.None, null };
                }
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

        public static IEnumerable<object[]> ToString_MatchesExpected_MemberData()
        {
            // Randomly generated data on .NET Framework with:
            //     using System;
            //     class Program
            //     {
            //         static void Main()
            //         {
            //             var rand = new Random(42);
            //             var bytes = new byte[8];
            //             int i = 0;
            //             while (i < 40)
            //             {
            //                 DateTimeKind kind = rand.Next(2) == 0 ? DateTimeKind.Utc : DateTimeKind.Unspecified;
            //                 string format;
            //                 switch (rand.Next(4))
            //                 {
            //                     case 0: format = "o"; break;
            //                     case 1: format = "O"; break;
            //                     case 2: format = "r"; break;
            //                     default: format = "R"; break;
            //                 }
            //
            //                 try
            //                 {
            //                     rand.NextBytes(bytes);
            //                     long seed = BitConverter.ToInt64(bytes, 0);
            //                     var dt = new DateTime(seed, kind);
            //                     Console.WriteLine($"yield return new object[] {{ new DateTime({seed}, DateTimeKind.{kind}), \"{format}\", null, \"{dt.ToString(format)}\" }};");
            //                     i++;
            //                 }
            //                 catch { }
            //             }
            //         }
            //}
            yield return new object[] { new DateTime(2688006240964947440, DateTimeKind.Utc), "O", null, "8518-12-15T08:01:36.4947440Z" };
            yield return new object[] { new DateTime(2461197105169450509, DateTimeKind.Utc), "r", null, "Sun, 23 Mar 7800 18:15:16 GMT" };
            yield return new object[] { new DateTime(71363981510699949, DateTimeKind.Unspecified), "R", null, "Fri, 23 Feb 0227 04:49:11 GMT" };
            yield return new object[] { new DateTime(1678426538898407093, DateTimeKind.Unspecified), "R", null, "Fri, 22 Sep 5319 07:24:49 GMT" };
            yield return new object[] { new DateTime(2689041307785948711, DateTimeKind.Utc), "o", null, "8522-03-27T07:52:58.5948711Z" };
            yield return new object[] { new DateTime(996610247053299209, DateTimeKind.Unspecified), "r", null, "Thu, 19 Feb 3159 01:58:25 GMT" };
            yield return new object[] { new DateTime(3105391438361510074, DateTimeKind.Unspecified), "R", null, "Fri, 06 Aug 9841 01:17:16 GMT" };
            yield return new object[] { new DateTime(946433487657072106, DateTimeKind.Utc), "R", null, "Mon, 17 Feb 3000 03:06:05 GMT" };
            yield return new object[] { new DateTime(2521748413631767931, DateTimeKind.Unspecified), "R", null, "Sat, 08 Feb 7992 07:02:43 GMT" };
            yield return new object[] { new DateTime(49349519375012969, DateTimeKind.Utc), "R", null, "Fri, 20 May 0157 11:58:57 GMT" };
            yield return new object[] { new DateTime(796677276139881359, DateTimeKind.Utc), "o", null, "2525-07-28T04:20:13.9881359Z" };
            yield return new object[] { new DateTime(3022911536338429542, DateTimeKind.Unspecified), "R", null, "Mon, 24 Mar 9580 04:53:53 GMT" };
            yield return new object[] { new DateTime(1144652135553351618, DateTimeKind.Utc), "R", null, "Tue, 04 Apr 3628 20:39:15 GMT" };
            yield return new object[] { new DateTime(2570858096011770291, DateTimeKind.Unspecified), "o", null, "8147-09-23T04:53:21.1770291" };
            yield return new object[] { new DateTime(15695724649124585, DateTimeKind.Unspecified), "R", null, "Tue, 27 Sep 0050 08:21:04 GMT" };
            yield return new object[] { new DateTime(1503933934291527034, DateTimeKind.Unspecified), "O", null, "4766-10-12T06:37:09.1527034" };
            yield return new object[] { new DateTime(2688603665097410101, DateTimeKind.Unspecified), "r", null, "Tue, 05 Nov 8520 19:08:29 GMT" };
            yield return new object[] { new DateTime(1310336900529542610, DateTimeKind.Unspecified), "r", null, "Tue, 17 Apr 4153 15:14:12 GMT" };
            yield return new object[] { new DateTime(2313720085584182693, DateTimeKind.Unspecified), "O", null, "7332-11-20T18:22:38.4182693" };
            yield return new object[] { new DateTime(2291958603891779335, DateTimeKind.Unspecified), "o", null, "7263-12-05T20:46:29.1779335" };
            yield return new object[] { new DateTime(262036413643976979, DateTimeKind.Unspecified), "o", null, "0831-05-12T21:16:04.3976979" };
            yield return new object[] { new DateTime(684781207384421044, DateTimeKind.Utc), "O", null, "2170-12-26T20:12:18.4421044Z" };
            yield return new object[] { new DateTime(1444462249169683325, DateTimeKind.Utc), "r", null, "Mon, 27 Apr 4578 07:21:56 GMT" };
            yield return new object[] { new DateTime(1155518137384061537, DateTimeKind.Unspecified), "r", null, "Sun, 10 Sep 3662 06:02:18 GMT" };
            yield return new object[] { new DateTime(2333390479532380569, DateTimeKind.Unspecified), "O", null, "7395-03-22T10:12:33.2380569" };
            yield return new object[] { new DateTime(2217528014591554502, DateTimeKind.Unspecified), "R", null, "Sat, 26 Jan 7028 08:24:19 GMT" };
            yield return new object[] { new DateTime(2764551324904480205, DateTimeKind.Utc), "O", null, "8761-07-08T04:21:30.4480205Z" };
            yield return new object[] { new DateTime(2880903932678729712, DateTimeKind.Utc), "O", null, "9130-03-23T13:14:27.8729712Z" };
            yield return new object[] { new DateTime(507699902578704433, DateTimeKind.Utc), "O", null, "1609-11-02T15:04:17.8704433Z" };
            yield return new object[] { new DateTime(2429953022324426129, DateTimeKind.Utc), "O", null, "7701-03-20T15:03:52.4426129Z" };
            yield return new object[] { new DateTime(603147512164908366, DateTimeKind.Unspecified), "O", null, "1912-04-20T09:33:36.4908366" };
            yield return new object[] { new DateTime(2900400428644841236, DateTimeKind.Utc), "R", null, "Thu, 02 Jan 9192 22:34:24 GMT" };
            yield return new object[] { new DateTime(1710845568474490805, DateTimeKind.Utc), "O", null, "5422-06-16T08:00:47.4490805Z" };
            yield return new object[] { new DateTime(2988999715803714268, DateTimeKind.Utc), "r", null, "Sun, 06 Oct 9472 09:53:00 GMT" };
            yield return new object[] { new DateTime(1068133489112689365, DateTimeKind.Utc), "r", null, "Wed, 12 Oct 3385 14:41:51 GMT" };
            yield return new object[] { new DateTime(798784044525059284, DateTimeKind.Unspecified), "R", null, "Mon, 31 Mar 2532 13:40:52 GMT" };
            yield return new object[] { new DateTime(2561736813034040593, DateTimeKind.Utc), "O", null, "8118-10-28T03:55:03.4040593Z" };
            yield return new object[] { new DateTime(1677975383149674547, DateTimeKind.Utc), "o", null, "5318-04-18T03:18:34.9674547Z" };
            yield return new object[] { new DateTime(1101778442151366156, DateTimeKind.Utc), "O", null, "3492-05-25T12:43:35.1366156Z" };
            yield return new object[] { new DateTime(221550163152616218, DateTimeKind.Utc), "r", null, "Sun, 25 Jan 0703 19:11:55 GMT" };

            // Year patterns
            if (PlatformDetection.IsNotInvariantGlobalization)
            {
                var enUS = new CultureInfo("en-US");
                var thTH = new CultureInfo("th-TH");
                yield return new object[] { new DateTime(1234, 5, 6), "yy", enUS, "34" };
                yield return new object[] { DateTime.MaxValue, "yy", thTH, "42" };
                for (int i = 3; i < 20; i++)
                {
                    yield return new object[] { new DateTime(1234, 5, 6), new string('y', i), enUS, 1234.ToString("D" + i) };
                    yield return new object[] { DateTime.MaxValue, new string('y', i), thTH, 10542.ToString("D" + i) };
                }
            }
            else
            {
                var invariant = new CultureInfo("");
                yield return new object[] { new DateTime(1234, 5, 6), "yy", invariant, "34" };

                for (int i = 3; i < 20; i++)
                {
                    yield return new object[] { new DateTime(1234, 5, 6), new string('y', i), invariant, 1234.ToString("D" + i) };
                }
            }
        }

        [Theory]
        [MemberData(nameof(Parse_ValidInput_Succeeds_MemberData))]
        public static void Parse_Span_ValidInput_Succeeds(string input, CultureInfo culture, DateTime? expected)
        {
            Assert.Equal(expected, DateTime.Parse(input.AsSpan(), culture));
        }

        [Theory]
        [MemberData(nameof(ParseExact_ValidInput_Succeeds_MemberData))]
        public static void ParseExact_Span_ValidInput_Succeeds(string input, string format, CultureInfo culture, DateTimeStyles style, DateTime? expected)
        {
            DateTime result1 = DateTime.ParseExact(input.AsSpan(), format, culture, style);
            DateTime result2 = DateTime.ParseExact(input.AsSpan(), new[] { format }, culture, style);

            Assert.True(DateTime.TryParseExact(input.AsSpan(), format, culture, style, out DateTime result3));
            Assert.True(DateTime.TryParseExact(input.AsSpan(), new[] { format }, culture, style, out DateTime result4));

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

        [Theory]
        [MemberData(nameof(ParseExact_InvalidInputs_Fail_MemberData))]
        public static void ParseExact_Span_InvalidInputs_Fail(string input, string format, CultureInfo culture, DateTimeStyles style)
        {
            Assert.Throws<FormatException>(() => DateTime.ParseExact(input.AsSpan(), format, culture, style));
            Assert.Throws<FormatException>(() => DateTime.ParseExact(input.AsSpan(), new[] { format }, culture, style));

            Assert.False(DateTime.TryParseExact(input.AsSpan(), format, culture, style, out DateTime result));
            Assert.False(DateTime.TryParseExact(input.AsSpan(), new[] { format }, culture, style, out result));
        }

        [Theory]
        [MemberData(nameof(ToString_MatchesExpected_MemberData))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/60562", TestPlatforms.Android)]
        public void ToString_Invoke_ReturnsExpected(DateTime dateTime, string format, IFormatProvider provider, string expected)
        {
            if (provider == null)
            {
                Assert.Equal(expected, dateTime.ToString(format));
            }

            Assert.Equal(expected, dateTime.ToString(format, provider));
        }

        [Fact]
        public void ToLongDateString_Invoke_ReturnsExpected()
        {
            DateTime date = DateTime.Now;
            Assert.Equal(date.ToString("D"), date.ToLongDateString());
        }

        [Fact]
        public void ToLongTimeString_Invoke_ReturnsExpected()
        {
            DateTime date = DateTime.Now;
            Assert.Equal(date.ToString("T"), date.ToLongTimeString());
        }

        [Fact]
        public void ToShortDateString_Invoke_ReturnsExpected()
        {
            DateTime date = DateTime.Now;
            Assert.Equal(date.ToString("d"), date.ToShortDateString());
        }

        [Fact]
        public void ToShortTimeString_Invoke_ReturnsExpected()
        {
            DateTime date = DateTime.Now;
            Assert.Equal(date.ToString("t"), date.ToShortTimeString());
        }

        [Fact]
        public void GetTypeCode_Invoke_ReturnsExpected()
        {
            Assert.Equal(TypeCode.DateTime, DateTime.Now.GetTypeCode());
        }

        [Fact]
        public void ToBoolean_Invoke_ThrowsInvalidCastException()
        {
            Assert.Throws<InvalidCastException>(() => ((IConvertible)DateTime.Now).ToBoolean(null));
        }

        [Fact]
        public void ToChar_Invoke_ThrowsInvalidCastException()
        {
            Assert.Throws<InvalidCastException>(() => ((IConvertible)DateTime.Now).ToChar(null));
        }

        [Fact]
        public void ToSByte_Invoke_ThrowsInvalidCastException()
        {
            Assert.Throws<InvalidCastException>(() => ((IConvertible)DateTime.Now).ToSByte(null));
        }

        [Fact]
        public void ToByte_Invoke_ThrowsInvalidCastException()
        {
            Assert.Throws<InvalidCastException>(() => ((IConvertible)DateTime.Now).ToByte(null));
        }

        [Fact]
        public void ToInt16_Invoke_ThrowsInvalidCastException()
        {
            Assert.Throws<InvalidCastException>(() => ((IConvertible)DateTime.Now).ToInt16(null));
        }

        [Fact]
        public void ToUInt16_Invoke_ThrowsInvalidCastException()
        {
            Assert.Throws<InvalidCastException>(() => ((IConvertible)DateTime.Now).ToUInt16(null));
        }

        [Fact]
        public void ToInt32_Invoke_ThrowsInvalidCastException()
        {
            Assert.Throws<InvalidCastException>(() => ((IConvertible)DateTime.Now).ToInt32(null));
        }

        [Fact]
        public void ToUInt32_Invoke_ThrowsInvalidCastException()
        {
            Assert.Throws<InvalidCastException>(() => ((IConvertible)DateTime.Now).ToUInt32(null));
        }

        [Fact]
        public void ToInt64_Invoke_ThrowsInvalidCastException()
        {
            Assert.Throws<InvalidCastException>(() => ((IConvertible)DateTime.Now).ToInt64(null));
        }

        [Fact]
        public void ToUInt64_Invoke_ThrowsInvalidCastException()
        {
            Assert.Throws<InvalidCastException>(() => ((IConvertible)DateTime.Now).ToUInt64(null));
        }

        [Fact]
        public void ToSingle_Invoke_ThrowsInvalidCastException()
        {
            Assert.Throws<InvalidCastException>(() => ((IConvertible)DateTime.Now).ToSingle(null));
        }

        [Fact]
        public void ToDouble_Invoke_ThrowsInvalidCastException()
        {
            Assert.Throws<InvalidCastException>(() => ((IConvertible)DateTime.Now).ToDouble(null));
        }

        [Fact]
        public void ToDecimal_Invoke_ThrowsInvalidCastException()
        {
            Assert.Throws<InvalidCastException>(() => ((IConvertible)DateTime.Now).ToDecimal(null));
        }

        [Fact]
        public void ToDateTime_Invoke_ReturnsExpected()
        {
            DateTime date = DateTime.Now;
            Assert.Equal(date, ((IConvertible)date).ToDateTime(null));
        }

        [Fact]
        public void ToType_DateTime_ReturnsExpected()
        {
            DateTime date = DateTime.Now;
            Assert.Equal(date, ((IConvertible)date).ToType(typeof(DateTime), null));
        }

        [Fact]
        public void GetObjectData_Invoke_ReturnsExpected()
        {
            ISerializable serializable = new DateTime(10, DateTimeKind.Utc);
            SerializationInfo info = new SerializationInfo(typeof(DateTime), new FormatterConverter());

            serializable.GetObjectData(info, new StreamingContext());
            Assert.Equal(10, info.GetInt64("ticks"));
            Assert.Equal(4611686018427387914, info.GetInt64("dateData"));
        }

        [Fact]
        public void GetObjectData_NullInfo_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("info", () => ((ISerializable)DateTime.Now).GetObjectData(null, new StreamingContext()));
        }

        [Fact]
        public void TestRoundTrippingDateTimeAndFileTime()
        {
            // This test ensure the round tripping of DateTime with the system file time.
            // It is important to have this working on systems supporting leap seconds as the conversion wouldn't be simple
            // conversion but involve some OS calls to ensure the right conversion is happening.

            DateTime now = DateTime.UtcNow;
            long fileTime = now.ToFileTimeUtc();
            DateTime roundTrippedDateTime = DateTime.FromFileTimeUtc(fileTime);
            Assert.Equal(now, roundTrippedDateTime);

            now = DateTime.Now;
            fileTime = now.ToFileTime();
            roundTrippedDateTime = DateTime.FromFileTime(fileTime);
            Assert.Equal(now, roundTrippedDateTime);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void TestTimeSynchronizationWithTheSystem()
        {
            // The reported time by the framework should be synchronized with the OS.
            // There shouldn't be any shift by more than one second, otherwise there is something wrong.
            // This test is useful when running on a system supporting leap seconds to ensure when the system
            // has leap seconds, the framework reported time will still be synchronized.

            SYSTEMTIME st;
            SYSTEMTIME st1;

            GetSystemTime(out st);
            DateTime dt = DateTime.UtcNow;
            GetSystemTime(out st1);

            DateTime systemDateTimeNow1  = new DateTime(st.wYear, st.wMonth, st.wDay, st.wHour, st.wMinute, st.wSecond, st.wMillisecond, DateTimeKind.Utc);
            DateTime systemDateTimeNow2  = new DateTime(st1.wYear, st1.wMonth, st1.wDay, st1.wHour, st1.wMinute, st1.wSecond, st1.wMillisecond, DateTimeKind.Utc);

            // Usually GetSystemTime and DateTime.UtcNow calls doesn't take one second to execute, if this is not the case then
            // the thread was sleeping for awhile and we cannot test reliably on that case.

            TimeSpan diff = systemDateTimeNow2 - systemDateTimeNow1;
            if (diff < TimeSpan.FromSeconds(1))
            {
                diff = dt - systemDateTimeNow1;
                Assert.True(diff < TimeSpan.FromSeconds(1), $"Reported DateTime.UtcNow {dt} is shifted by more than one second then the system time {systemDateTimeNow1}");
            }
        }

        [DllImport("Kernel32.dll")]
        internal static extern void GetSystemTime(out SYSTEMTIME lpSystemTime);

        [StructLayout(LayoutKind.Sequential)]
        internal struct SYSTEMTIME
        {
            internal ushort wYear;
            internal ushort wMonth;
            internal ushort wDayOfWeek;
            internal ushort wDay;
            internal ushort wHour;
            internal ushort wMinute;
            internal ushort wSecond;
            internal ushort wMillisecond;
        }

        [Theory]
        [MemberData(nameof(StandardFormatSpecifiers))]
        public static void TryFormat_MatchesToString(string format)
        {
            DateTime dt = DateTime.UtcNow;
            string expected = dt.ToString(format);

            // Just the right length, succeeds
            Span<char> dest = new char[expected.Length];
            Assert.True(dt.TryFormat(dest, out int charsWritten, format));
            Assert.Equal(expected.Length, charsWritten);
            Assert.Equal<char>(expected.ToCharArray(), dest.ToArray());

            // Too short, fails
            dest = new char[expected.Length - 1];
            Assert.False(dt.TryFormat(dest, out charsWritten, format));
            Assert.Equal(0, charsWritten);

            // Longer than needed, succeeds
            dest = new char[expected.Length + 1];
            Assert.True(dt.TryFormat(dest, out charsWritten, format));
            Assert.Equal(expected.Length, charsWritten);
            Assert.Equal<char>(expected.ToCharArray(), dest.Slice(0, expected.Length).ToArray());
            Assert.Equal(0, dest[dest.Length - 1]);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotInvariantGlobalization))]
        [MemberData(nameof(ToString_MatchesExpected_MemberData))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/60562", TestPlatforms.Android)]
        public static void TryFormat_MatchesExpected(DateTime dateTime, string format, IFormatProvider provider, string expected)
        {
            var destination = new char[expected.Length];

            Assert.False(dateTime.TryFormat(destination.AsSpan(0, destination.Length - 1), out _, format, provider));

            Assert.True(dateTime.TryFormat(destination, out int charsWritten, format, provider));
            Assert.Equal(destination.Length, charsWritten);
            Assert.Equal(expected, new string(destination));
        }

        [Fact]
        public static void UnixEpoch()
        {
            VerifyDateTime(DateTime.UnixEpoch, 1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        }
    }
}
