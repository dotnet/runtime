// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Xunit;

namespace System.Tests
{
    public static partial class DateTimeOffsetTests
    {
        [Fact]
        public static void MaxValue()
        {
            VerifyDateTimeOffset(DateTimeOffset.MaxValue, 9999, 12, 31, 23, 59, 59, 999, TimeSpan.Zero);
        }

        [Fact]
        public static void MinValue()
        {
            VerifyDateTimeOffset(DateTimeOffset.MinValue, 1, 1, 1, 0, 0, 0, 0, TimeSpan.Zero);
        }

        [Fact]
        public static void Ctor_Empty()
        {
            VerifyDateTimeOffset(new DateTimeOffset(), 1, 1, 1, 0, 0, 0, 0, TimeSpan.Zero);
            VerifyDateTimeOffset(default(DateTimeOffset), 1, 1, 1, 0, 0, 0, 0, TimeSpan.Zero);
        }

        [Fact]
        public static void Ctor_DateTime()
        {
            var dateTimeOffset = new DateTimeOffset(new DateTime(2012, 6, 11, 0, 0, 0, 0, DateTimeKind.Utc));
            VerifyDateTimeOffset(dateTimeOffset, 2012, 6, 11, 0, 0, 0, 0, TimeSpan.Zero);

            dateTimeOffset = new DateTimeOffset(new DateTime(1986, 8, 15, 10, 20, 5, 4, DateTimeKind.Local));
            VerifyDateTimeOffset(dateTimeOffset, 1986, 8, 15, 10, 20, 5, 4, null);

            DateTimeOffset today = new DateTimeOffset(DateTime.Today);
            DateTimeOffset now = DateTimeOffset.Now.Date;
            VerifyDateTimeOffset(today, now.Year, now.Month, now.Day, 0, 0, 0, 0, now.Offset);

            today = new DateTimeOffset(new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, DateTimeKind.Utc));
            Assert.Equal(TimeSpan.Zero, today.Offset);
            Assert.False(today.UtcDateTime.IsDaylightSavingTime());
        }

        [Fact]
        public static void Ctor_DateTime_Invalid()
        {
            // DateTime < DateTimeOffset.MinValue
            DateTimeOffset min = DateTimeOffset.MinValue;
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(new DateTime(min.Year - 1, min.Day, min.Hour, min.Minute, min.Second, min.Millisecond, DateTimeKind.Utc)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(new DateTime(min.Year, min.Month - 1, min.Day, min.Hour, min.Minute, min.Second, min.Millisecond, DateTimeKind.Utc)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(new DateTime(min.Year, min.Month, min.Day - 1, min.Hour, min.Minute, min.Second, min.Millisecond, DateTimeKind.Utc)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(new DateTime(min.Year, min.Month, min.Day, min.Hour - 1, min.Minute, min.Second, min.Millisecond, DateTimeKind.Utc)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(new DateTime(min.Year, min.Month, min.Day, min.Hour, min.Minute - 1, min.Second, min.Millisecond, DateTimeKind.Utc)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(new DateTime(min.Year, min.Month, min.Day, min.Hour, min.Minute, min.Second - 1, min.Millisecond, DateTimeKind.Utc)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("millisecond", () => new DateTimeOffset(new DateTime(min.Year, min.Month, min.Day, min.Hour, min.Minute, min.Second, min.Millisecond - 1, DateTimeKind.Utc)));

            // DateTime > DateTimeOffset.MaxValue
            DateTimeOffset max = DateTimeOffset.MaxValue;
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(new DateTime(max.Year + 1, max.Month, max.Day, max.Hour, max.Minute, max.Second, max.Millisecond, DateTimeKind.Utc)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(new DateTime(max.Year, max.Month + 1, max.Day, max.Hour, max.Minute, max.Second, max.Millisecond, DateTimeKind.Utc)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(new DateTime(max.Year, max.Month, max.Day + 1, max.Hour, max.Minute, max.Second, max.Millisecond, DateTimeKind.Utc)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(new DateTime(max.Year, max.Month, max.Day, max.Hour + 1, max.Minute, max.Second, max.Millisecond, DateTimeKind.Utc)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(new DateTime(max.Year, max.Month, max.Day, max.Hour, max.Minute + 1, max.Second, max.Millisecond, DateTimeKind.Utc)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(new DateTime(max.Year, max.Month, max.Day, max.Hour, max.Minute, max.Second + 1, max.Millisecond, DateTimeKind.Utc)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("millisecond", () => new DateTimeOffset(new DateTime(max.Year, max.Month, max.Day, max.Hour, max.Minute, max.Second, max.Millisecond + 1, DateTimeKind.Utc)));
        }

        [Fact]
        public static void Ctor_DateTime_TimeSpan()
        {
            var dateTimeOffset = new DateTimeOffset(DateTime.MinValue, TimeSpan.FromHours(-14));
            VerifyDateTimeOffset(dateTimeOffset, 1, 1, 1, 0, 0, 0, 0, TimeSpan.FromHours(-14));

            dateTimeOffset = new DateTimeOffset(DateTime.MaxValue, TimeSpan.FromHours(14));
            VerifyDateTimeOffset(dateTimeOffset, 9999, 12, 31, 23, 59, 59, 999, TimeSpan.FromHours(14));

            dateTimeOffset = new DateTimeOffset(new DateTime(2012, 12, 31, 13, 50, 10), TimeSpan.Zero);
            VerifyDateTimeOffset(dateTimeOffset, 2012, 12, 31, 13, 50, 10, 0, TimeSpan.Zero);
        }

        [Fact]
        public static void Ctor_DateTime_TimeSpan_Invalid()
        {
            AssertExtensions.Throws<ArgumentException>("offset", () => new DateTimeOffset(DateTime.Now, TimeSpan.FromHours(15))); // Local time and non timezone timespan
            AssertExtensions.Throws<ArgumentException>("offset", () => new DateTimeOffset(DateTime.Now, TimeSpan.FromHours(-15))); // Local time and non timezone timespan

            AssertExtensions.Throws<ArgumentException>("offset", () => new DateTimeOffset(DateTime.UtcNow, TimeSpan.FromHours(1))); // Local time and non zero timespan

            AssertExtensions.Throws<ArgumentException>("offset", () => new DateTimeOffset(DateTime.UtcNow, new TimeSpan(0, 0, 3))); // TimeSpan is not whole minutes
            AssertExtensions.Throws<ArgumentException>("offset", () => new DateTimeOffset(DateTime.UtcNow, new TimeSpan(0, 0, -3))); // TimeSpan is not whole minutes
            AssertExtensions.Throws<ArgumentException>("offset", () => new DateTimeOffset(DateTime.UtcNow, new TimeSpan(0, 0, 0, 0, 3))); // TimeSpan is not whole minutes
            AssertExtensions.Throws<ArgumentException>("offset", () => new DateTimeOffset(DateTime.UtcNow, new TimeSpan(0, 0, 0, 0, -3))); // TimeSpan is not whole minutes

            // DateTime < DateTimeOffset.MinValue
            DateTimeOffset min = DateTimeOffset.MinValue;
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(new DateTime(min.Year - 1, min.Day, min.Hour, min.Minute, min.Second, min.Millisecond, DateTimeKind.Utc), TimeSpan.Zero));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(new DateTime(min.Year, min.Month - 1, min.Day, min.Hour, min.Minute, min.Second, min.Millisecond, DateTimeKind.Utc), TimeSpan.Zero));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(new DateTime(min.Year, min.Month, min.Day - 1, min.Hour, min.Minute, min.Second, min.Millisecond, DateTimeKind.Utc), TimeSpan.Zero));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(new DateTime(min.Year, min.Month, min.Day, min.Hour - 1, min.Minute, min.Second, min.Millisecond, DateTimeKind.Utc), TimeSpan.Zero));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(new DateTime(min.Year, min.Month, min.Day, min.Hour, min.Minute - 1, min.Second, min.Millisecond, DateTimeKind.Utc), TimeSpan.Zero));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(new DateTime(min.Year, min.Month, min.Day, min.Hour, min.Minute, min.Second - 1, min.Millisecond, DateTimeKind.Utc), TimeSpan.Zero));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("millisecond", () => new DateTimeOffset(new DateTime(min.Year, min.Month, min.Day, min.Hour, min.Minute, min.Second, min.Millisecond - 1, DateTimeKind.Utc), TimeSpan.Zero));

            // DateTime > DateTimeOffset.MaxValue
            DateTimeOffset max = DateTimeOffset.MaxValue;
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(new DateTime(max.Year + 1, max.Month, max.Day, max.Hour, max.Minute, max.Second, max.Millisecond, DateTimeKind.Utc), TimeSpan.Zero));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(new DateTime(max.Year, max.Month + 1, max.Day, max.Hour, max.Minute, max.Second, max.Millisecond, DateTimeKind.Utc), TimeSpan.Zero));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(new DateTime(max.Year, max.Month, max.Day + 1, max.Hour, max.Minute, max.Second, max.Millisecond, DateTimeKind.Utc), TimeSpan.Zero));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(new DateTime(max.Year, max.Month, max.Day, max.Hour + 1, max.Minute, max.Second, max.Millisecond, DateTimeKind.Utc), TimeSpan.Zero));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(new DateTime(max.Year, max.Month, max.Day, max.Hour, max.Minute + 1, max.Second, max.Millisecond, DateTimeKind.Utc), TimeSpan.Zero));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(new DateTime(max.Year, max.Month, max.Day, max.Hour, max.Minute, max.Second + 1, max.Millisecond, DateTimeKind.Utc), TimeSpan.Zero));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("millisecond", () => new DateTimeOffset(new DateTime(max.Year, max.Month, max.Day, max.Hour, max.Minute, max.Second, max.Millisecond + 1, DateTimeKind.Utc), TimeSpan.Zero));

            // Invalid offset
            AssertExtensions.Throws<ArgumentException>("offset", () => new DateTimeOffset(DateTime.Now, TimeSpan.FromTicks(1)));
            AssertExtensions.Throws<ArgumentException>("offset", () => new DateTimeOffset(DateTime.UtcNow, TimeSpan.FromTicks(1)));
        }

        [Fact]
        public static void Ctor_Long_TimeSpan()
        {
            var expected = new DateTime(1, 2, 3, 4, 5, 6, 7);
            var dateTimeOffset = new DateTimeOffset(expected.Ticks, TimeSpan.Zero);
            VerifyDateTimeOffset(dateTimeOffset, dateTimeOffset.Year, dateTimeOffset.Month, dateTimeOffset.Day, dateTimeOffset.Hour, dateTimeOffset.Minute, dateTimeOffset.Second, dateTimeOffset.Millisecond, TimeSpan.Zero);
        }

        [Fact]
        public static void Ctor_Long_TimeSpan_Invalid()
        {
            AssertExtensions.Throws<ArgumentException>("offset", () => new DateTimeOffset(0, new TimeSpan(0, 0, 3))); // TimeSpan is not whole minutes
            AssertExtensions.Throws<ArgumentException>("offset", () => new DateTimeOffset(0, new TimeSpan(0, 0, -3))); // TimeSpan is not whole minutes
            AssertExtensions.Throws<ArgumentException>("offset", () => new DateTimeOffset(0, new TimeSpan(0, 0, 0, 0, 3))); // TimeSpan is not whole minutes
            AssertExtensions.Throws<ArgumentException>("offset", () => new DateTimeOffset(0, new TimeSpan(0, 0, 0, 0, -3))); // TimeSpan is not whole minutes

            AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => new DateTimeOffset(0, TimeSpan.FromHours(-15))); // TimeZone.Offset > 14
            AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => new DateTimeOffset(0, TimeSpan.FromHours(15))); // TimeZone.Offset < -14

            AssertExtensions.Throws<ArgumentOutOfRangeException>("ticks", () => new DateTimeOffset(DateTimeOffset.MinValue.Ticks - 1, TimeSpan.Zero)); // Ticks < DateTimeOffset.MinValue.Ticks
            AssertExtensions.Throws<ArgumentOutOfRangeException>("ticks", () => new DateTimeOffset(DateTimeOffset.MaxValue.Ticks + 1, TimeSpan.Zero)); // Ticks > DateTimeOffset.MaxValue.Ticks
        }

        [Fact]
        public static void Ctor_Int_Int_Int_Int_Int_Int_Int_TimeSpan()
        {
            var dateTimeOffset = new DateTimeOffset(1973, 10, 6, 14, 30, 0, 500, TimeSpan.Zero);
            VerifyDateTimeOffset(dateTimeOffset, 1973, 10, 6, 14, 30, 0, 500, TimeSpan.Zero);
        }

        [Fact]
        public static void Ctor_Int_Int_Int_Int_Int_Int_Int_TimeSpan_Invalid()
        {
            AssertExtensions.Throws<ArgumentException>("offset", () => new DateTimeOffset(1, 1, 1, 1, 1, 1, 1, new TimeSpan(0, 0, 3))); // TimeSpan is not whole minutes
            AssertExtensions.Throws<ArgumentException>("offset", () => new DateTimeOffset(1, 1, 1, 1, 1, 1, 1, new TimeSpan(0, 0, -3))); // TimeSpan is not whole minutes
            AssertExtensions.Throws<ArgumentException>("offset", () => new DateTimeOffset(1, 1, 1, 1, 1, 1, 1, new TimeSpan(0, 0, 0, 0, 3))); // TimeSpan is not whole minutes
            AssertExtensions.Throws<ArgumentException>("offset", () => new DateTimeOffset(1, 1, 1, 1, 1, 1, 1, new TimeSpan(0, 0, 0, 0, -3))); // TimeSpan is not whole minutes

            AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => new DateTimeOffset(1, 1, 1, 1, 1, 1, 1, TimeSpan.FromHours(-15))); // TimeZone.Offset > 14
            AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => new DateTimeOffset(1, 1, 1, 1, 1, 1, 1, TimeSpan.FromHours(15))); // TimeZone.Offset < -14

            // Invalid DateTime
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(0, 1, 1, 1, 1, 1, 1, TimeSpan.Zero)); // Year < 1
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(10000, 1, 1, 1, 1, 1, 1, TimeSpan.Zero)); // Year > 9999

            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(1, 0, 1, 1, 1, 1, 1, TimeSpan.Zero)); // Month < 1
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(1, 13, 1, 1, 1, 1, 1, TimeSpan.Zero)); // Motnh > 23

            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(1, 1, 0, 1, 1, 1, 1, TimeSpan.Zero)); // Day < 1
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(1, 1, 32, 1, 1, 1, 1, TimeSpan.Zero)); // Day > days in month

            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(1, 1, 1, -1, 1, 1, 1, TimeSpan.Zero)); // Hour < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(1, 1, 1, 24, 1, 1, 1, TimeSpan.Zero)); // Hour > 23

            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(1, 1, 1, 1, -1, 1, 1, TimeSpan.Zero)); // Minute < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(1, 1, 1, 1, 60, 1, 1, TimeSpan.Zero)); // Minute > 59

            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(1, 1, 1, 1, 1, -1, 1, TimeSpan.Zero)); // Second < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(1, 1, 1, 1, 1, 60, 1, TimeSpan.Zero)); // Second > 59

            AssertExtensions.Throws<ArgumentOutOfRangeException>("millisecond", () => new DateTimeOffset(1, 1, 1, 1, 1, 1, -1, TimeSpan.Zero)); // Millisecond < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("millisecond", () => new DateTimeOffset(1, 1, 1, 1, 1, 1, 1000, TimeSpan.Zero)); // Millisecond > 999

            // DateTime < DateTimeOffset.MinValue
            DateTimeOffset min = DateTimeOffset.MinValue;
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(min.Year - 1, min.Month, min.Day, min.Hour, min.Minute, min.Second, min.Millisecond, TimeSpan.Zero));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(min.Year, min.Month - 1, min.Day, min.Hour, min.Minute, min.Second, min.Millisecond, TimeSpan.Zero));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(min.Year, min.Month, min.Day - 1, min.Hour, min.Minute, min.Second, min.Millisecond, TimeSpan.Zero));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(min.Year, min.Month, min.Day, min.Hour - 1, min.Minute, min.Second, min.Millisecond, TimeSpan.Zero));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(min.Year, min.Month, min.Day, min.Hour, min.Minute - 1, min.Second, min.Millisecond, TimeSpan.Zero));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(min.Year, min.Month, min.Day, min.Hour, min.Minute, min.Second - 1, min.Millisecond, TimeSpan.Zero));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("millisecond", () => new DateTimeOffset(min.Year, min.Month, min.Day, min.Hour, min.Minute, min.Second, min.Millisecond - 1, TimeSpan.Zero));

            // DateTime > DateTimeOffset.MaxValue
            DateTimeOffset max = DateTimeOffset.MaxValue;
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(max.Year + 1, max.Month, max.Day, max.Hour, max.Minute, max.Second, max.Millisecond, TimeSpan.Zero));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(max.Year, max.Month + 1, max.Day + 1, max.Hour, max.Minute, max.Second, max.Millisecond, TimeSpan.Zero));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(max.Year, max.Month, max.Day + 1, max.Hour, max.Minute, max.Second, max.Millisecond, TimeSpan.Zero));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(max.Year, max.Month, max.Day, max.Hour + 1, max.Minute, max.Second, max.Millisecond, TimeSpan.Zero));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(max.Year, max.Month, max.Day, max.Hour, max.Minute + 1, max.Second, max.Millisecond, TimeSpan.Zero));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(max.Year, max.Month, max.Day, max.Hour, max.Minute, max.Second + 1, max.Millisecond, TimeSpan.Zero));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("millisecond", () => new DateTimeOffset(max.Year, max.Month, max.Day, max.Hour, max.Minute, max.Second, max.Millisecond + 1, TimeSpan.Zero));
        }

        [Fact]
        public static void Ctor_Int_Int_Int_Int_Int_Int_TimeSpan()
        {
            var dateTimeOffset = new DateTimeOffset(1973, 10, 6, 14, 30, 0, TimeSpan.Zero);
            VerifyDateTimeOffset(dateTimeOffset, 1973, 10, 6, 14, 30, 0, 0, TimeSpan.Zero);
        }

        [Fact]
        public static void Ctor_Int_Int_Int_Int_Int_Int_TimeSpan_Invalid()
        {
            AssertExtensions.Throws<ArgumentException>("offset", () => new DateTimeOffset(1, 1, 1, 1, 1, 1, new TimeSpan(0, 0, 3))); // TimeSpan is not whole minutes
            AssertExtensions.Throws<ArgumentException>("offset", () => new DateTimeOffset(1, 1, 1, 1, 1, 1, new TimeSpan(0, 0, -3))); // TimeSpan is not whole minutes
            AssertExtensions.Throws<ArgumentException>("offset", () => new DateTimeOffset(1, 1, 1, 1, 1, 1, new TimeSpan(0, 0, 0, 0, 3))); // TimeSpan is not whole minutes
            AssertExtensions.Throws<ArgumentException>("offset", () => new DateTimeOffset(1, 1, 1, 1, 1, 1, new TimeSpan(0, 0, 0, 0, -3))); // TimeSpan is not whole minutes

            AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => new DateTimeOffset(1, 1, 1, 1, 1, 1, TimeSpan.FromHours(-15))); // TimeZone.Offset > 14
            AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => new DateTimeOffset(1, 1, 1, 1, 1, 1, TimeSpan.FromHours(15))); // TimeZone.Offset < -14

            // Invalid DateTime
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(0, 1, 1, 1, 1, 1, TimeSpan.Zero)); // Year < 1
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(10000, 1, 1, 1, 1, 1, TimeSpan.Zero)); // Year > 9999

            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(1, 0, 1, 1, 1, 1, TimeSpan.Zero)); // Month < 1
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(1, 13, 1, 1, 1, 1, TimeSpan.Zero)); // Month > 23

            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(1, 1, 0, 1, 1, 1, TimeSpan.Zero)); // Day < 1
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(1, 1, 32, 1, 1, 1, TimeSpan.Zero)); // Day > days in month

            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(1, 1, 1, -1, 1, 1, TimeSpan.Zero)); // Hour < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(1, 1, 1, 24, 1, 1, TimeSpan.Zero)); // Hour > 23

            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(1, 1, 1, 1, -1, 1, TimeSpan.Zero)); // Minute < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(1, 1, 1, 1, 60, 1, TimeSpan.Zero)); // Minute > 59

            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(1, 1, 1, 1, 1, -1, TimeSpan.Zero)); // Second < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(1, 1, 1, 1, 1, 60, TimeSpan.Zero)); // Second > 59

            // DateTime < DateTimeOffset.MinValue
            DateTimeOffset min = DateTimeOffset.MinValue;
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(min.Year - 1, min.Month, min.Day, min.Hour, min.Minute, min.Second, TimeSpan.Zero));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(min.Year, min.Month - 1, min.Day, min.Hour, min.Minute, min.Second, TimeSpan.Zero));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(min.Year, min.Month, min.Day - 1, min.Hour, min.Minute, min.Second, TimeSpan.Zero));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(min.Year, min.Month, min.Day, min.Hour - 1, min.Minute, min.Second, TimeSpan.Zero));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(min.Year, min.Month, min.Day, min.Hour, min.Minute - 1, min.Second, TimeSpan.Zero));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(min.Year, min.Month, min.Day, min.Hour, min.Minute, min.Second - 1, TimeSpan.Zero));

            // DateTime > DateTimeOffset.MaxValue
            DateTimeOffset max = DateTimeOffset.MaxValue;
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(max.Year + 1, max.Month, max.Day, max.Hour, max.Minute, max.Second, TimeSpan.Zero));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(max.Year, max.Month + 1, max.Day + 1, max.Hour, max.Minute, max.Second, TimeSpan.Zero));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(max.Year, max.Month, max.Day + 1, max.Hour, max.Minute, max.Second, TimeSpan.Zero));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(max.Year, max.Month, max.Day, max.Hour + 1, max.Minute, max.Second, TimeSpan.Zero));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(max.Year, max.Month, max.Day, max.Hour, max.Minute + 1, max.Second, TimeSpan.Zero));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(max.Year, max.Month, max.Day, max.Hour, max.Minute, max.Second + 1, TimeSpan.Zero));
        }

        [Fact]
        public static void ImplicitCast_DateTime()
        {
            DateTime dateTime = new DateTime(2012, 6, 11, 0, 0, 0, 0, DateTimeKind.Utc);
            DateTimeOffset dateTimeOffset = dateTime;
            VerifyDateTimeOffset(dateTimeOffset, 2012, 6, 11, 0, 0, 0, 0, TimeSpan.Zero);
        }

        [Fact]
        public static void AddSubtract_TimeSpan()
        {
            var dateTimeOffset = new DateTimeOffset(new DateTime(2012, 6, 18, 10, 5, 1, 0, DateTimeKind.Utc));
            TimeSpan timeSpan = dateTimeOffset.TimeOfDay;

            DateTimeOffset newDate = dateTimeOffset.Subtract(timeSpan);
            Assert.Equal(new DateTimeOffset(new DateTime(2012, 6, 18, 0, 0, 0, 0, DateTimeKind.Utc)).Ticks, newDate.Ticks);
            Assert.Equal(dateTimeOffset.Ticks, newDate.Add(timeSpan).Ticks);
        }

        public static IEnumerable<object[]> Subtract_TimeSpan_TestData()
        {
            var dateTimeOffset = new DateTimeOffset(new DateTime(2012, 6, 18, 10, 5, 1, 0, DateTimeKind.Utc));

            yield return new object[] { dateTimeOffset, new TimeSpan(10, 5, 1), new DateTimeOffset(new DateTime(2012, 6, 18, 0, 0, 0, 0, DateTimeKind.Utc)) };
            yield return new object[] { dateTimeOffset, new TimeSpan(-10, -5, -1), new DateTimeOffset(new DateTime(2012, 6, 18, 20, 10, 2, 0, DateTimeKind.Utc)) };
        }

        [Theory]
        [MemberData(nameof(Subtract_TimeSpan_TestData))]
        public static void Subtract_TimeSpan(DateTimeOffset dt, TimeSpan ts, DateTimeOffset expected)
        {
            Assert.Equal(expected, dt - ts);
            Assert.Equal(expected, dt.Subtract(ts));
        }

        public static IEnumerable<object[]> Subtract_DateTimeOffset_TestData()
        {
            var dateTimeOffset1 = new DateTimeOffset(new DateTime(1996, 6, 3, 22, 15, 0, DateTimeKind.Utc));
            var dateTimeOffset2 = new DateTimeOffset(new DateTime(1996, 12, 6, 13, 2, 0, DateTimeKind.Utc));
            var dateTimeOffset3 = new DateTimeOffset(new DateTime(1996, 10, 12, 8, 42, 0, DateTimeKind.Utc));

            yield return new object[] { dateTimeOffset2, dateTimeOffset1, new TimeSpan(185, 14, 47, 0) };
            yield return new object[] { dateTimeOffset1, dateTimeOffset2, new TimeSpan(-185, -14, -47, 0) };
            yield return new object[] { dateTimeOffset1, dateTimeOffset2, new TimeSpan(-185, -14, -47, 0) };
        }

        [Theory]
        [MemberData(nameof(Subtract_DateTimeOffset_TestData))]
        public static void Subtract_DateTimeOffset(DateTimeOffset dt1, DateTimeOffset dt2, TimeSpan expected)
        {
            Assert.Equal(expected, dt1 - dt2);
            Assert.Equal(expected, dt1.Subtract(dt2));
        }

        public static IEnumerable<object[]> Add_TimeSpan_TestData()
        {
            yield return new object[] { new DateTimeOffset(new DateTime(1000, DateTimeKind.Utc)), new TimeSpan(10), new DateTimeOffset(new DateTime(1010, DateTimeKind.Utc)) };
            yield return new object[] { new DateTimeOffset(new DateTime(1000, DateTimeKind.Utc)), TimeSpan.Zero, new DateTimeOffset(new DateTime(1000, DateTimeKind.Utc)) };
            yield return new object[] { new DateTimeOffset(new DateTime(1000, DateTimeKind.Utc)), new TimeSpan(-10), new DateTimeOffset(new DateTime(990, DateTimeKind.Utc)) };
        }

        [Theory]
        [MemberData(nameof(Add_TimeSpan_TestData))]
        public static void Add_TimeSpan(DateTimeOffset dateTimeOffset, TimeSpan timeSpan, DateTimeOffset expected)
        {
            Assert.Equal(expected, dateTimeOffset.Add(timeSpan));
            Assert.Equal(expected, dateTimeOffset + timeSpan);
        }

        [Fact]
        public static void Add_TimeSpan_NewDateOutOfRange_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => DateTimeOffset.MinValue.Add(TimeSpan.FromTicks(-1)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => DateTimeOffset.MaxValue.Add(TimeSpan.FromTicks(11)));
        }

        public static IEnumerable<object[]> AddYears_TestData()
        {
            yield return new object[] { new DateTimeOffset(new DateTime(1986, 8, 15, 10, 20, 5, 70, DateTimeKind.Utc)), 10, new DateTimeOffset(new DateTime(1996, 8, 15, 10, 20, 5, 70, DateTimeKind.Utc)) };
            yield return new object[] { new DateTimeOffset(new DateTime(1986, 8, 15, 10, 20, 5, 70, DateTimeKind.Utc)), 0, new DateTimeOffset(new DateTime(1986, 8, 15, 10, 20, 5, 70, DateTimeKind.Utc)) };
            yield return new object[] { new DateTimeOffset(new DateTime(1986, 8, 15, 10, 20, 5, 70, DateTimeKind.Utc)), -10, new DateTimeOffset(new DateTime(1976, 8, 15, 10, 20, 5, 70, DateTimeKind.Utc)) };
        }

        [Theory]
        [MemberData(nameof(AddYears_TestData))]
        public static void AddYears(DateTimeOffset dateTimeOffset, int years, DateTimeOffset expected)
        {
            Assert.Equal(expected, dateTimeOffset.AddYears(years));
        }

        [Fact]
        public static void AddYears_NewDateOutOfRange_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("years", () => DateTimeOffset.Now.AddYears(10001));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("years", () => DateTimeOffset.Now.AddYears(-10001));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("months", () => DateTimeOffset.MaxValue.AddYears(1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("months", () => DateTimeOffset.MinValue.AddYears(-1));
        }

        public static IEnumerable<object[]> AddMonths_TestData()
        {
            yield return new object[] { new DateTimeOffset(new DateTime(1986, 8, 15, 10, 20, 5, 70, DateTimeKind.Utc)), 2, new DateTimeOffset(new DateTime(1986, 10, 15, 10, 20, 5, 70, DateTimeKind.Utc)) };
            yield return new object[] { new DateTimeOffset(new DateTime(1986, 8, 15, 10, 20, 5, 70, DateTimeKind.Utc)), 0, new DateTimeOffset(new DateTime(1986, 8, 15, 10, 20, 5, 70, DateTimeKind.Utc)) };
            yield return new object[] { new DateTimeOffset(new DateTime(1986, 8, 15, 10, 20, 5, 70, DateTimeKind.Utc)), -2, new DateTimeOffset(new DateTime(1986, 6, 15, 10, 20, 5, 70, DateTimeKind.Utc)) };
        }

        [Theory]
        [MemberData(nameof(AddMonths_TestData))]
        public static void AddMonths(DateTimeOffset dateTimeOffset, int months, DateTimeOffset expected)
        {
            Assert.Equal(expected, dateTimeOffset.AddMonths(months));
        }

        [Fact]
        public static void AddMonths_NewDateOutOfRange_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("months", () => DateTimeOffset.Now.AddMonths(120001));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("months", () => DateTimeOffset.Now.AddMonths(-120001));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("months", () => DateTimeOffset.MaxValue.AddMonths(1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("months", () => DateTimeOffset.MinValue.AddMonths(-1));
        }

        public static IEnumerable<object[]> AddDays_TestData()
        {
            yield return new object[] { new DateTimeOffset(new DateTime(1986, 8, 15, 10, 20, 5, 70, DateTimeKind.Utc)), 2, new DateTimeOffset(new DateTime(1986, 8, 17, 10, 20, 5, 70, DateTimeKind.Utc)) };
            yield return new object[] { new DateTimeOffset(new DateTime(1986, 8, 15, 10, 20, 5, 70, DateTimeKind.Utc)), 0, new DateTimeOffset(new DateTime(1986, 8, 15, 10, 20, 5, 70, DateTimeKind.Utc)) };
            yield return new object[] { new DateTimeOffset(new DateTime(1986, 8, 15, 10, 20, 5, 70, DateTimeKind.Utc)), -2, new DateTimeOffset(new DateTime(1986, 8, 13, 10, 20, 5, 70, DateTimeKind.Utc)) };
        }

        [Theory]
        [MemberData(nameof(AddDays_TestData))]
        public static void AddDays(DateTimeOffset dateTimeOffset, double days, DateTimeOffset expected)
        {
            Assert.Equal(expected, dateTimeOffset.AddDays(days));
        }

        [Fact]
        public static void AddDays_NewDateOutOfRange_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => DateTimeOffset.MaxValue.AddDays(1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => DateTimeOffset.MinValue.AddDays(-1));
        }

        public static IEnumerable<object[]> AddHours_TestData()
        {
            yield return new object[] { new DateTimeOffset(new DateTime(1986, 8, 15, 10, 20, 5, 70, DateTimeKind.Utc)), 3, new DateTimeOffset(new DateTime(1986, 8, 15, 13, 20, 5, 70, DateTimeKind.Utc)) };
            yield return new object[] { new DateTimeOffset(new DateTime(1986, 8, 15, 10, 20, 5, 70, DateTimeKind.Utc)), 0, new DateTimeOffset(new DateTime(1986, 8, 15, 10, 20, 5, 70, DateTimeKind.Utc)) };
            yield return new object[] { new DateTimeOffset(new DateTime(1986, 8, 15, 10, 20, 5, 70, DateTimeKind.Utc)), -3, new DateTimeOffset(new DateTime(1986, 8, 15, 7, 20, 5, 70, DateTimeKind.Utc)) };
        }

        [Theory]
        [MemberData(nameof(AddHours_TestData))]
        public static void AddHours(DateTimeOffset dateTimeOffset, double hours, DateTimeOffset expected)
        {
            Assert.Equal(expected, dateTimeOffset.AddHours(hours));
        }

        [Fact]
        public static void AddHours_NewDateOutOfRange_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => DateTimeOffset.MaxValue.AddHours(1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => DateTimeOffset.MinValue.AddHours(-1));
        }

        public static IEnumerable<object[]> AddMinutes_TestData()
        {
            yield return new object[] { new DateTimeOffset(new DateTime(1986, 8, 15, 10, 20, 5, 70, DateTimeKind.Utc)), 5, new DateTimeOffset(new DateTime(1986, 8, 15, 10, 25, 5, 70, DateTimeKind.Utc)) };
            yield return new object[] { new DateTimeOffset(new DateTime(1986, 8, 15, 10, 20, 5, 70, DateTimeKind.Utc)), 0, new DateTimeOffset(new DateTime(1986, 8, 15, 10, 20, 5, 70, DateTimeKind.Utc)) };
            yield return new object[] { new DateTimeOffset(new DateTime(1986, 8, 15, 10, 20, 5, 70, DateTimeKind.Utc)), -5, new DateTimeOffset(new DateTime(1986, 8, 15, 10, 15, 5, 70, DateTimeKind.Utc)) };
        }

        [Theory]
        [MemberData(nameof(AddMinutes_TestData))]
        public static void AddMinutes(DateTimeOffset dateTimeOffset, double minutes, DateTimeOffset expected)
        {
            Assert.Equal(expected, dateTimeOffset.AddMinutes(minutes));
        }

        [Fact]
        public static void AddMinutes_NewDateOutOfRange_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => DateTimeOffset.MaxValue.AddMinutes(1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => DateTimeOffset.MinValue.AddMinutes(-1));
        }

        public static IEnumerable<object[]> AddSeconds_TestData()
        {
            yield return new object[] { new DateTimeOffset(new DateTime(1986, 8, 15, 10, 20, 5, 70, DateTimeKind.Utc)), 30, new DateTimeOffset(new DateTime(1986, 8, 15, 10, 20, 35, 70, DateTimeKind.Utc)) };
            yield return new object[] { new DateTimeOffset(new DateTime(1986, 8, 15, 10, 20, 5, 70, DateTimeKind.Utc)), 0, new DateTimeOffset(new DateTime(1986, 8, 15, 10, 20, 5, 70, DateTimeKind.Utc)) };
            yield return new object[] { new DateTimeOffset(new DateTime(1986, 8, 15, 10, 20, 5, 70, DateTimeKind.Utc)), -3, new DateTimeOffset(new DateTime(1986, 8, 15, 10, 20, 2, 70, DateTimeKind.Utc)) };
        }

        [Theory]
        [MemberData(nameof(AddSeconds_TestData))]
        public static void AddSeconds(DateTimeOffset dateTimeOffset, double seconds, DateTimeOffset expected)
        {
            Assert.Equal(expected, dateTimeOffset.AddSeconds(seconds));
        }

        [Fact]
        public static void AddSeconds_NewDateOutOfRange_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => DateTimeOffset.MaxValue.AddSeconds(1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => DateTimeOffset.MinValue.AddSeconds(-1));
        }

        public static IEnumerable<object[]> AddMilliseconds_TestData()
        {
            yield return new object[] { new DateTimeOffset(new DateTime(1986, 8, 15, 10, 20, 5, 70, DateTimeKind.Utc)), 10, new DateTimeOffset(new DateTime(1986, 8, 15, 10, 20, 5, 80, DateTimeKind.Utc)) };
            yield return new object[] { new DateTimeOffset(new DateTime(1986, 8, 15, 10, 20, 5, 70, DateTimeKind.Utc)), 0, new DateTimeOffset(new DateTime(1986, 8, 15, 10, 20, 5, 70, DateTimeKind.Utc)) };
            yield return new object[] { new DateTimeOffset(new DateTime(1986, 8, 15, 10, 20, 5, 70, DateTimeKind.Utc)), -10, new DateTimeOffset(new DateTime(1986, 8, 15, 10, 20, 5, 60, DateTimeKind.Utc)) };
        }

        [Theory]
        [MemberData(nameof(AddMilliseconds_TestData))]
        public static void AddMilliseconds(DateTimeOffset dateTimeOffset, double milliseconds, DateTimeOffset expected)
        {
            Assert.Equal(expected, dateTimeOffset.AddMilliseconds(milliseconds));
        }

        [Fact]
        public static void AddMilliseconds_NewDateOutOfRange_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => DateTimeOffset.MaxValue.AddMilliseconds(1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => DateTimeOffset.MinValue.AddMilliseconds(-1));
        }

        public static IEnumerable<object[]> AddTicks_TestData()
        {
            yield return new object[] { new DateTimeOffset(new DateTime(1000, DateTimeKind.Utc)), 10, new DateTimeOffset(new DateTime(1010, DateTimeKind.Utc)) };
            yield return new object[] { new DateTimeOffset(new DateTime(1000, DateTimeKind.Utc)), 0, new DateTimeOffset(new DateTime(1000, DateTimeKind.Utc)) };
            yield return new object[] { new DateTimeOffset(new DateTime(1000, DateTimeKind.Utc)), -10, new DateTimeOffset(new DateTime(990, DateTimeKind.Utc)) };
        }

        [Theory]
        [MemberData(nameof(AddTicks_TestData))]
        public static void AddTicks(DateTimeOffset dateTimeOffset, long ticks, DateTimeOffset expected)
        {
            Assert.Equal(expected, dateTimeOffset.AddTicks(ticks));
        }

        [Fact]
        public static void AddTicks_NewDateOutOfRange_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => DateTimeOffset.MaxValue.AddTicks(1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => DateTimeOffset.MinValue.AddTicks(-1));
        }

        [Fact]
        public static void ToFromFileTime()
        {
            var today = new DateTimeOffset(DateTime.Today);

            long dateTimeRaw = today.ToFileTime();
            Assert.Equal(today, DateTimeOffset.FromFileTime(dateTimeRaw));
        }

        [Fact]
        public static void UtcDateTime()
        {
            DateTime now = DateTime.Now;
            var dateTimeOffset = new DateTimeOffset(now);
            Assert.Equal(DateTime.Today, dateTimeOffset.Date);
            Assert.Equal(now, dateTimeOffset.DateTime);
            Assert.Equal(now.ToUniversalTime(), dateTimeOffset.UtcDateTime);
        }

        [Fact]
        public static void UtcNow()
        {
            DateTimeOffset start = DateTimeOffset.UtcNow;
            Assert.True(
                SpinWait.SpinUntil(() => DateTimeOffset.UtcNow > start, TimeSpan.FromSeconds(30)),
                "Expected UtcNow to changes");
        }

        [Fact]
        public static void DayOfYear()
        {
            DateTimeOffset dateTimeOffset = new DateTimeOffset();
            Assert.Equal(dateTimeOffset.DateTime.DayOfYear, dateTimeOffset.DayOfYear);
        }

        [Fact]
        public static void DayOfWeekTest()
        {
            DateTimeOffset dateTimeOffset = new DateTimeOffset();
            Assert.Equal(dateTimeOffset.DateTime.DayOfWeek, dateTimeOffset.DayOfWeek);
        }

        [Fact]
        public static void TimeOfDay()
        {
            DateTimeOffset dateTimeOffset = new DateTimeOffset();
            Assert.Equal(dateTimeOffset.DateTime.TimeOfDay, dateTimeOffset.TimeOfDay);
        }

        private static IEnumerable<object[]> UnixTime_TestData()
        {
            yield return new object[] { TestTime.FromMilliseconds(DateTimeOffset.MinValue, -62135596800000) };
            yield return new object[] { TestTime.FromMilliseconds(DateTimeOffset.MaxValue, 253402300799999) };
            yield return new object[] { TestTime.FromMilliseconds(new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero), 0) };
            yield return new object[] { TestTime.FromMilliseconds(new DateTimeOffset(2014, 6, 13, 17, 21, 50, TimeSpan.Zero), 1402680110000) };
            yield return new object[] { TestTime.FromMilliseconds(new DateTimeOffset(2830, 12, 15, 1, 23, 45, TimeSpan.Zero), 27169089825000) };
            yield return new object[] { TestTime.FromMilliseconds(new DateTimeOffset(2830, 12, 15, 1, 23, 45, 399, TimeSpan.Zero), 27169089825399) };
            yield return new object[] { TestTime.FromMilliseconds(new DateTimeOffset(9999, 12, 30, 23, 24, 25, TimeSpan.Zero), 253402212265000) };
            yield return new object[] { TestTime.FromMilliseconds(new DateTimeOffset(1907, 7, 7, 7, 7, 7, TimeSpan.Zero), -1971967973000) };
            yield return new object[] { TestTime.FromMilliseconds(new DateTimeOffset(1907, 7, 7, 7, 7, 7, 1, TimeSpan.Zero), -1971967972999) };
            yield return new object[] { TestTime.FromMilliseconds(new DateTimeOffset(1907, 7, 7, 7, 7, 7, 777, TimeSpan.Zero), -1971967972223) };
            yield return new object[] { TestTime.FromMilliseconds(new DateTimeOffset(601636288270011234, TimeSpan.Zero), -1971967972999) };
        }

        [Theory]
        [MemberData(nameof(UnixTime_TestData))]
        public static void ToUnixTimeMilliseconds(TestTime test)
        {
            long expectedMilliseconds = test.UnixTimeMilliseconds;
            long actualMilliseconds = test.DateTimeOffset.ToUnixTimeMilliseconds();
            Assert.Equal(expectedMilliseconds, actualMilliseconds);
        }

        [Theory]
        [MemberData(nameof(UnixTime_TestData))]
        public static void ToUnixTimeMilliseconds_RoundTrip(TestTime test)
        {
            long unixTimeMilliseconds = test.DateTimeOffset.ToUnixTimeMilliseconds();
            FromUnixTimeMilliseconds(TestTime.FromMilliseconds(test.DateTimeOffset, unixTimeMilliseconds));
        }

        [Theory]
        [MemberData(nameof(UnixTime_TestData))]
        public static void ToUnixTimeSeconds(TestTime test)
        {
            long expectedSeconds = test.UnixTimeSeconds;
            long actualSeconds = test.DateTimeOffset.ToUnixTimeSeconds();
            Assert.Equal(expectedSeconds, actualSeconds);
        }

        [Theory]
        [MemberData(nameof(UnixTime_TestData))]
        public static void ToUnixTimeSeconds_RoundTrip(TestTime test)
        {
            long unixTimeSeconds = test.DateTimeOffset.ToUnixTimeSeconds();
            FromUnixTimeSeconds(TestTime.FromSeconds(test.DateTimeOffset, unixTimeSeconds));
        }

        [Theory]
        [MemberData(nameof(UnixTime_TestData))]
        public static void FromUnixTimeMilliseconds(TestTime test)
        {
            // Only assert that expected == actual up to millisecond precision for conversion from milliseconds
            long expectedTicks = (test.DateTimeOffset.UtcTicks / TimeSpan.TicksPerMillisecond) * TimeSpan.TicksPerMillisecond;
            long actualTicks = DateTimeOffset.FromUnixTimeMilliseconds(test.UnixTimeMilliseconds).UtcTicks;
            Assert.Equal(expectedTicks, actualTicks);
        }

        [Fact]
        public static void FromUnixTimeMilliseconds_Invalid()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("milliseconds", () => DateTimeOffset.FromUnixTimeMilliseconds(-62135596800001)); // Milliseconds < DateTimeOffset.MinValue
            AssertExtensions.Throws<ArgumentOutOfRangeException>("milliseconds", () => DateTimeOffset.FromUnixTimeMilliseconds(253402300800000)); // Milliseconds > DateTimeOffset.MaxValue

            AssertExtensions.Throws<ArgumentOutOfRangeException>("milliseconds", () => DateTimeOffset.FromUnixTimeMilliseconds(long.MinValue)); // Milliseconds < DateTimeOffset.MinValue
            AssertExtensions.Throws<ArgumentOutOfRangeException>("milliseconds", () => DateTimeOffset.FromUnixTimeMilliseconds(long.MaxValue)); // Milliseconds > DateTimeOffset.MaxValue
        }

        [Theory]
        [MemberData(nameof(UnixTime_TestData))]
        public static void FromUnixTimeSeconds(TestTime test)
        {
            // Only assert that expected == actual up to second precision for conversion from seconds
            long expectedTicks = (test.DateTimeOffset.UtcTicks / TimeSpan.TicksPerSecond) * TimeSpan.TicksPerSecond;
            long actualTicks = DateTimeOffset.FromUnixTimeSeconds(test.UnixTimeSeconds).UtcTicks;
            Assert.Equal(expectedTicks, actualTicks);
        }

        [Fact]
        public static void FromUnixTimeSeconds_Invalid()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("seconds", () => DateTimeOffset.FromUnixTimeSeconds(-62135596801));// Seconds < DateTimeOffset.MinValue
            AssertExtensions.Throws<ArgumentOutOfRangeException>("seconds", () => DateTimeOffset.FromUnixTimeSeconds(253402300800)); // Seconds > DateTimeOffset.MaxValue

            AssertExtensions.Throws<ArgumentOutOfRangeException>("seconds", () => DateTimeOffset.FromUnixTimeSeconds(long.MinValue)); // Seconds < DateTimeOffset.MinValue
            AssertExtensions.Throws<ArgumentOutOfRangeException>("seconds", () => DateTimeOffset.FromUnixTimeSeconds(long.MaxValue)); // Seconds < DateTimeOffset.MinValue
        }

        [Theory]
        [MemberData(nameof(UnixTime_TestData))]
        public static void FromUnixTimeMilliseconds_RoundTrip(TestTime test)
        {
            DateTimeOffset dateTime = DateTimeOffset.FromUnixTimeMilliseconds(test.UnixTimeMilliseconds);
            ToUnixTimeMilliseconds(TestTime.FromMilliseconds(dateTime, test.UnixTimeMilliseconds));
        }

        [Theory]
        [MemberData(nameof(UnixTime_TestData))]
        public static void FromUnixTimeSeconds_RoundTrip(TestTime test)
        {
            DateTimeOffset dateTime = DateTimeOffset.FromUnixTimeSeconds(test.UnixTimeSeconds);
            ToUnixTimeSeconds(TestTime.FromSeconds(dateTime, test.UnixTimeSeconds));
        }

        [Fact]
        public static void ToLocalTime()
        {
            DateTimeOffset dateTimeOffset = new DateTimeOffset(new DateTime(1000, DateTimeKind.Utc));
            Assert.Equal(new DateTimeOffset(dateTimeOffset.UtcDateTime.ToLocalTime()), dateTimeOffset.ToLocalTime());
        }
        
        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { DateTimeOffset.MinValue, DateTimeOffset.MinValue, true, true };
            yield return new object[] { DateTimeOffset.MinValue, DateTimeOffset.MaxValue, false, false };

            yield return new object[] { DateTimeOffset.Now, new object(), false, false };
            yield return new object[] { DateTimeOffset.Now, null, false, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public static void Equals(DateTimeOffset dateTimeOffset1, object obj, bool expectedEquals, bool expectedEqualsExact)
        {
            Assert.Equal(expectedEquals, dateTimeOffset1.Equals(obj));
            if (obj is DateTimeOffset)
            {
                DateTimeOffset dateTimeOffset2 = (DateTimeOffset)obj;
                Assert.Equal(expectedEquals, dateTimeOffset1.Equals(dateTimeOffset2));
                Assert.Equal(expectedEquals, DateTimeOffset.Equals(dateTimeOffset1, dateTimeOffset2));

                Assert.Equal(expectedEquals, dateTimeOffset1.GetHashCode().Equals(dateTimeOffset2.GetHashCode()));
                Assert.Equal(expectedEqualsExact, dateTimeOffset1.EqualsExact(dateTimeOffset2));

                Assert.Equal(expectedEquals, dateTimeOffset1 == dateTimeOffset2);
                Assert.Equal(!expectedEquals, dateTimeOffset1 != dateTimeOffset2);
            }
        }

        public static IEnumerable<object[]> Compare_TestData()
        {
            yield return new object[] { new DateTimeOffset(new DateTime(1000, DateTimeKind.Utc)), new DateTimeOffset(new DateTime(1000, DateTimeKind.Utc)), 0 };
            yield return new object[] { new DateTimeOffset(new DateTime(1000, DateTimeKind.Utc)), new DateTimeOffset(new DateTime(1001, DateTimeKind.Utc)), -1 };
            yield return new object[] { new DateTimeOffset(new DateTime(1000, DateTimeKind.Utc)), new DateTimeOffset(new DateTime(999, DateTimeKind.Utc)), 1 };
        }

        [Theory]
        [MemberData(nameof(Compare_TestData))]
        public static void Compare(DateTimeOffset dateTimeOffset1, DateTimeOffset dateTimeOffset2, int expected)
        {
            Assert.Equal(expected, Math.Sign(dateTimeOffset1.CompareTo(dateTimeOffset2)));
            Assert.Equal(expected, Math.Sign(DateTimeOffset.Compare(dateTimeOffset1, dateTimeOffset2)));

            IComparable comparable = dateTimeOffset1;
            Assert.Equal(expected, Math.Sign(comparable.CompareTo(dateTimeOffset2)));

            if (expected > 0)
            {
                Assert.True(dateTimeOffset1 > dateTimeOffset2);
                Assert.Equal(expected >= 0, dateTimeOffset1 >= dateTimeOffset2);
                Assert.False(dateTimeOffset1 < dateTimeOffset2);
                Assert.Equal(expected == 0, dateTimeOffset1 <= dateTimeOffset2);
            }
            else if (expected < 0)
            {
                Assert.False(dateTimeOffset1 > dateTimeOffset2);
                Assert.Equal(expected == 0, dateTimeOffset1 >= dateTimeOffset2);
                Assert.True(dateTimeOffset1 < dateTimeOffset2);
                Assert.Equal(expected <= 0, dateTimeOffset1 <= dateTimeOffset2);
            }
            else if (expected == 0)
            {
                Assert.False(dateTimeOffset1 > dateTimeOffset2);
                Assert.True(dateTimeOffset1 >= dateTimeOffset2);
                Assert.False(dateTimeOffset1 < dateTimeOffset2);
                Assert.True(dateTimeOffset1 <= dateTimeOffset2);
            }
        }

        [Fact]
        public static void Parse_String()
        {
            DateTimeOffset expected = DateTimeOffset.MaxValue;
            string expectedString = expected.ToString();

            DateTimeOffset result = DateTimeOffset.Parse(expectedString);
            Assert.Equal(expectedString, result.ToString());
        }

        [Fact]
        public static void Parse_String_FormatProvider()
        {
            DateTimeOffset expected = DateTimeOffset.MaxValue;
            string expectedString = expected.ToString();

            DateTimeOffset result = DateTimeOffset.Parse(expectedString, null);
            Assert.Equal(expectedString, result.ToString((IFormatProvider)null));
        }

        [Fact]
        public static void Parse_String_FormatProvider_DateTimeStyles()
        {
            DateTimeOffset expected = DateTimeOffset.MaxValue;
            string expectedString = expected.ToString();

            DateTimeOffset result = DateTimeOffset.Parse(expectedString, null, DateTimeStyles.None);
            Assert.Equal(expectedString, result.ToString());
        }

        [Fact]
        public static void Parse_Japanese()
        {
            var expected = new DateTimeOffset(new DateTime(2012, 12, 21, 10, 8, 6));
            var cultureInfo = new CultureInfo("ja-JP");

            string expectedString = string.Format(cultureInfo, "{0}", expected);
            Assert.Equal(expected, DateTimeOffset.Parse(expectedString, cultureInfo));
        }

        [Fact]
        public static void TryParse_String()
        {
            DateTimeOffset expected = DateTimeOffset.MaxValue;
            string expectedString = expected.ToString("u");

            DateTimeOffset result;
            Assert.True(DateTimeOffset.TryParse(expectedString, out result));
            Assert.Equal(expectedString, result.ToString("u"));
        }

        [Fact]
        public static void TryParse_String_FormatProvider_DateTimeStyles_U()
        {
            DateTimeOffset expected = DateTimeOffset.MaxValue;
            string expectedString = expected.ToString("u");

            DateTimeOffset result;
            Assert.True(DateTimeOffset.TryParse(expectedString, null, DateTimeStyles.None, out result));
            Assert.Equal(expectedString, result.ToString("u"));
        }

        [Fact]
        public static void TryParse_String_FormatProvider_DateTimeStyles_G()
        {
            DateTimeOffset expected = DateTimeOffset.MaxValue;
            string expectedString = expected.ToString("g");

            DateTimeOffset result;
            Assert.True(DateTimeOffset.TryParse(expectedString, null, DateTimeStyles.AssumeUniversal, out result));
            Assert.Equal(expectedString, result.ToString("g"));
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "The full .NET framework has a bug and incorrectly parses this date")]
        public static void TryParse_TimeDesignators_NetCore()
        {
            DateTimeOffset result;
            Assert.True(DateTimeOffset.TryParse("4/21 5am", new CultureInfo("en-US"), DateTimeStyles.None, out result));
            Assert.Equal(4, result.Month);
            Assert.Equal(21, result.Day);
            Assert.Equal(5, result.Hour);
            Assert.Equal(0, result.Minute);
            Assert.Equal(0, result.Second);

            Assert.True(DateTimeOffset.TryParse("4/21 5pm", new CultureInfo("en-US"), DateTimeStyles.None, out result));
            Assert.Equal(4, result.Month);
            Assert.Equal(21, result.Day);
            Assert.Equal(17, result.Hour);
            Assert.Equal(0, result.Minute);
            Assert.Equal(0, result.Second);
        }

        [Fact]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework, "The coreclr fixed a bug where the .NET framework incorrectly parses this date")]
        public static void TryParse_TimeDesignators_Netfx()
        {
            DateTimeOffset result;
            Assert.True(DateTimeOffset.TryParse("4/21 5am", new CultureInfo("en-US"), DateTimeStyles.None, out result));
            Assert.Equal(DateTimeOffset.Now.Month, result.Month);
            Assert.Equal(DateTimeOffset.Now.Day, result.Day);
            Assert.Equal(4, result.Hour);
            Assert.Equal(0, result.Minute);
            Assert.Equal(0, result.Second);

            Assert.True(DateTimeOffset.TryParse("4/21 5pm", new CultureInfo("en-US"), DateTimeStyles.None, out result));
            Assert.Equal(DateTimeOffset.Now.Month, result.Month);
            Assert.Equal(DateTimeOffset.Now.Day, result.Day);
            Assert.Equal(16, result.Hour);
            Assert.Equal(0, result.Minute);
            Assert.Equal(0, result.Second);
        }

        [Fact]
        public static void ParseExact_String_String_FormatProvider()
        {
            DateTimeOffset expected = DateTimeOffset.MaxValue;
            string expectedString = expected.ToString("u");

            DateTimeOffset result = DateTimeOffset.ParseExact(expectedString, "u", null);
            Assert.Equal(expectedString, result.ToString("u"));
        }

        [Fact]
        public static void ParseExact_String_String_FormatProvider_DateTimeStyles_U()
        {
            DateTimeOffset expected = DateTimeOffset.MaxValue;
            string expectedString = expected.ToString("u");

            DateTimeOffset result = DateTimeOffset.ParseExact(expectedString, "u", null, DateTimeStyles.None);
            Assert.Equal(expectedString, result.ToString("u"));
        }

        [Fact]
        public static void ParseExact_String_String_FormatProvider_DateTimeStyles_G()
        {
            DateTimeOffset expected = DateTimeOffset.MaxValue;
            string expectedString = expected.ToString("g");

            DateTimeOffset result = DateTimeOffset.ParseExact(expectedString, "g", null, DateTimeStyles.AssumeUniversal);
            Assert.Equal(expectedString, result.ToString("g"));
        }

        [Theory, MemberData(nameof(Format_String_TestData_O))]
        public static void ParseExact_String_String_FormatProvider_DateTimeStyles_O(DateTimeOffset dt, string expected)
        {
            string actual = dt.ToString("o");
            Assert.Equal(expected, actual);

            DateTimeOffset result = DateTimeOffset.ParseExact(actual, "o", null, DateTimeStyles.None);
            Assert.Equal(expected, result.ToString("o"));
        }

        public static IEnumerable<object[]> Format_String_TestData_O()
        {
            yield return new object[] { DateTimeOffset.MaxValue, "9999-12-31T23:59:59.9999999+00:00" };
            yield return new object[] { DateTimeOffset.MinValue, "0001-01-01T00:00:00.0000000+00:00" };
            yield return new object[] { new DateTimeOffset(1906, 8, 15, 7, 24, 5, 300, new TimeSpan(0, 0, 0)), "1906-08-15T07:24:05.3000000+00:00" };
            yield return new object[] { new DateTimeOffset(1906, 8, 15, 7, 24, 5, 300, new TimeSpan(7, 30, 0)), "1906-08-15T07:24:05.3000000+07:30" };
        }

        [Theory, MemberData(nameof(Format_String_TestData_R))]
        public static void ParseExact_String_String_FormatProvider_DateTimeStyles_R(DateTimeOffset dt, string expected)
        {
            string actual = dt.ToString("r");
            Assert.Equal(expected, actual);

            DateTimeOffset result = DateTimeOffset.ParseExact(actual, "r", null, DateTimeStyles.None);
            Assert.Equal(expected, result.ToString("r"));
        }

        public static IEnumerable<object[]> Format_String_TestData_R()
        {
            yield return new object[] { DateTimeOffset.MaxValue, "Fri, 31 Dec 9999 23:59:59 GMT" };
            yield return new object[] { DateTimeOffset.MinValue, "Mon, 01 Jan 0001 00:00:00 GMT" };
            yield return new object[] { new DateTimeOffset(1906, 8, 15, 7, 24, 5, 300, new TimeSpan(0, 0, 0)), "Wed, 15 Aug 1906 07:24:05 GMT" };
            yield return new object[] { new DateTimeOffset(1906, 8, 15, 7, 24, 5, 300, new TimeSpan(7, 30, 0)), "Tue, 14 Aug 1906 23:54:05 GMT" };
        }

        [Fact]
        public static void ParseExact_String_String_FormatProvider_DateTimeStyles_R()
        {
            DateTimeOffset expected = DateTimeOffset.MaxValue;
            string expectedString = expected.ToString("r");

            DateTimeOffset result = DateTimeOffset.ParseExact(expectedString, "r", null, DateTimeStyles.None);
            Assert.Equal(expectedString, result.ToString("r"));
        }

        [Fact]
        public static void ParseExact_String_String_FormatProvider_DateTimeStyles_CustomFormatProvider()
        {
            var formatter = new MyFormatter();
            string dateBefore = DateTime.Now.ToString();

            DateTimeOffset dateAfter = DateTimeOffset.ParseExact(dateBefore, "G", formatter, DateTimeStyles.AssumeUniversal);
            Assert.Equal(dateBefore, dateAfter.DateTime.ToString());
        }

        [Fact]
        public static void ParseExact_String_StringArray_FormatProvider_DateTimeStyles()
        {
            DateTimeOffset expected = DateTimeOffset.MaxValue;
            string expectedString = expected.ToString("g");

            var formats = new string[] { "g" };
            DateTimeOffset result = DateTimeOffset.ParseExact(expectedString, formats, null, DateTimeStyles.AssumeUniversal);
            Assert.Equal(expectedString, result.ToString("g"));
        }

        [Fact]
        public static void TryParseExact_String_String_FormatProvider_DateTimeStyles_NullFormatProvider()
        {
            DateTimeOffset expected = DateTimeOffset.MaxValue;
            string expectedString = expected.ToString("g");

            DateTimeOffset resulted;
            Assert.True(DateTimeOffset.TryParseExact(expectedString, "g", null, DateTimeStyles.AssumeUniversal, out resulted));
            Assert.Equal(expectedString, resulted.ToString("g"));
        }

        [Fact]
        public static void TryParseExact_String_StringArray_FormatProvider_DateTimeStyles()
        {
            DateTimeOffset expected = DateTimeOffset.MaxValue;
            string expectedString = expected.ToString("g");

            var formats = new string[] { "g" };
            DateTimeOffset result;
            Assert.True(DateTimeOffset.TryParseExact(expectedString, formats, null, DateTimeStyles.AssumeUniversal, out result));
            Assert.Equal(expectedString, result.ToString("g"));
        }

        [Theory]
        [InlineData(~(DateTimeStyles.AllowLeadingWhite | DateTimeStyles.AllowTrailingWhite | DateTimeStyles.AllowInnerWhite | DateTimeStyles.NoCurrentDateDefault | DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeLocal | DateTimeStyles.AssumeUniversal | DateTimeStyles.RoundtripKind))]
        [InlineData(DateTimeStyles.NoCurrentDateDefault)]
        public static void Parse_InvalidDateTimeStyle_ThrowsArgumentException(DateTimeStyles style)
        {
            AssertExtensions.Throws<ArgumentException>("styles", () => DateTimeOffset.Parse("06/08/1990", null, style));
            AssertExtensions.Throws<ArgumentException>("styles", () => DateTimeOffset.ParseExact("06/08/1990", "Y", null, style));

            DateTimeOffset dateTimeOffset = default(DateTimeOffset);
            AssertExtensions.Throws<ArgumentException>("styles", () => DateTimeOffset.TryParse("06/08/1990", null, style, out dateTimeOffset));
            Assert.Equal(default(DateTimeOffset), dateTimeOffset);

            AssertExtensions.Throws<ArgumentException>("styles", () => DateTimeOffset.TryParseExact("06/08/1990", "Y", null, style, out dateTimeOffset));
            Assert.Equal(default(DateTimeOffset), dateTimeOffset);
        }

        private static void VerifyDateTimeOffset(DateTimeOffset dateTimeOffset, int year, int month, int day, int hour, int minute, int second, int millisecond, TimeSpan? offset)
        {
            Assert.Equal(year, dateTimeOffset.Year);
            Assert.Equal(month, dateTimeOffset.Month);
            Assert.Equal(day, dateTimeOffset.Day);
            Assert.Equal(hour, dateTimeOffset.Hour);
            Assert.Equal(minute, dateTimeOffset.Minute);
            Assert.Equal(second, dateTimeOffset.Second);
            Assert.Equal(millisecond, dateTimeOffset.Millisecond);

            if (offset.HasValue)
            {
                Assert.Equal(offset.Value, dateTimeOffset.Offset);
            }
        }

        private class MyFormatter : IFormatProvider
        {
            public object GetFormat(Type formatType)
            {
                return typeof(IFormatProvider) == formatType ? this : null;
            }
        }

        public class TestTime
        {
            private TestTime(DateTimeOffset dateTimeOffset, long unixTimeMilliseconds, long unixTimeSeconds)
            {
                DateTimeOffset = dateTimeOffset;
                UnixTimeMilliseconds = unixTimeMilliseconds;
                UnixTimeSeconds = unixTimeSeconds;
            }

            public static TestTime FromMilliseconds(DateTimeOffset dateTimeOffset, long unixTimeMilliseconds)
            {
                long unixTimeSeconds = unixTimeMilliseconds / 1000;

                // Always round UnixTimeSeconds down toward 1/1/0001 00:00:00
                // (this happens automatically for unixTimeMilliseconds > 0)
                bool hasSubSecondPrecision = unixTimeMilliseconds % 1000 != 0;
                if (unixTimeMilliseconds < 0 && hasSubSecondPrecision)
                {
                    --unixTimeSeconds;
                }

                return new TestTime(dateTimeOffset, unixTimeMilliseconds, unixTimeSeconds);
            }

            public static TestTime FromSeconds(DateTimeOffset dateTimeOffset, long unixTimeSeconds)
            {
                return new TestTime(dateTimeOffset, unixTimeSeconds * 1000, unixTimeSeconds);
            }

            public DateTimeOffset DateTimeOffset { get; private set; }
            public long UnixTimeMilliseconds { get; private set; }
            public long UnixTimeSeconds { get; private set; }
        }

        [Fact]
        public static void Ctor_Calendar_TimeSpan()
        {
            var dateTimeOffset = new DateTimeOffset(1, 1, 1, 0, 0, 0, 0, new GregorianCalendar(),TimeSpan.Zero);
            VerifyDateTimeOffset(dateTimeOffset, 1, 1, 1, 0, 0, 0, 0, TimeSpan.Zero);
        }
    }
}
