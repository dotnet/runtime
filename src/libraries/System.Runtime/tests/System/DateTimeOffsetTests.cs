// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using Xunit;

namespace System.Tests
{
    public static class DateTimeOffsetTests
    {
        [Fact]
        public static void MaxValue()
        {
            VerifyDateTimeOffset(DateTimeOffset.MaxValue, 9999, 12, 31, 23, 59, 59, 999, 999, TimeSpan.Zero, 900);
        }

        [Fact]
        public static void MinValue()
        {
            VerifyDateTimeOffset(DateTimeOffset.MinValue, 1, 1, 1, 0, 0, 0, 0, 0, TimeSpan.Zero);
        }

        [Fact]
        public static void Ctor_Empty()
        {
            VerifyDateTimeOffset(new DateTimeOffset(), 1, 1, 1, 0, 0, 0, 0, 0, TimeSpan.Zero);
            VerifyDateTimeOffset(default(DateTimeOffset), 1, 1, 1, 0, 0, 0, 0, 0, TimeSpan.Zero);
        }

        [Fact]
        public static void Ctor_DateTime()
        {
            var dateTimeOffset = new DateTimeOffset(new DateTime(2012, 6, 11, 0, 0, 0, 0, DateTimeKind.Utc));
            VerifyDateTimeOffset(dateTimeOffset, 2012, 6, 11, 0, 0, 0, 0, 0, TimeSpan.Zero);

            dateTimeOffset = new DateTimeOffset(new DateTime(1986, 8, 15, 10, 20, 5, 4, 3, DateTimeKind.Local));
            VerifyDateTimeOffset(dateTimeOffset, 1986, 8, 15, 10, 20, 5, 4, 3, null);

            DateTimeOffset today = new DateTimeOffset(DateTime.Today);
            DateTimeOffset now = DateTimeOffset.Now.Date;
            VerifyDateTimeOffset(today, now.Year, now.Month, now.Day, 0, 0, 0, 0, 0, now.Offset);

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
            AssertExtensions.Throws<ArgumentOutOfRangeException>("microsecond", () => new DateTimeOffset(new DateTime(min.Year, min.Month, min.Day, min.Hour, min.Minute, min.Second, min.Millisecond, min.Microsecond - 1, DateTimeKind.Utc)));

            // DateTime > DateTimeOffset.MaxValue
            DateTimeOffset max = DateTimeOffset.MaxValue;
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(new DateTime(max.Year + 1, max.Month, max.Day, max.Hour, max.Minute, max.Second, max.Millisecond, DateTimeKind.Utc)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(new DateTime(max.Year, max.Month + 1, max.Day, max.Hour, max.Minute, max.Second, max.Millisecond, DateTimeKind.Utc)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(new DateTime(max.Year, max.Month, max.Day + 1, max.Hour, max.Minute, max.Second, max.Millisecond, DateTimeKind.Utc)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(new DateTime(max.Year, max.Month, max.Day, max.Hour + 1, max.Minute, max.Second, max.Millisecond, DateTimeKind.Utc)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(new DateTime(max.Year, max.Month, max.Day, max.Hour, max.Minute + 1, max.Second, max.Millisecond, DateTimeKind.Utc)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(new DateTime(max.Year, max.Month, max.Day, max.Hour, max.Minute, max.Second + 1, max.Millisecond, DateTimeKind.Utc)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("millisecond", () => new DateTimeOffset(new DateTime(max.Year, max.Month, max.Day, max.Hour, max.Minute, max.Second, max.Millisecond + 1, DateTimeKind.Utc)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("microsecond", () => new DateTimeOffset(new DateTime(max.Year, max.Month, max.Day, max.Hour, max.Minute, max.Second, max.Millisecond, max.Microsecond + 1, DateTimeKind.Utc)));
        }

        [Fact]
        public static void Ctor_DateOnly_TimeOnly_TimeSpan()
        {
            var dateTimeOffset = new DateTimeOffset(DateOnly.MinValue, TimeOnly.MinValue, TimeSpan.FromHours(-14));
            VerifyDateTimeOffset(dateTimeOffset, 1, 1, 1, 0, 0, 0, 0, 0, TimeSpan.FromHours(-14));
            
            dateTimeOffset = new DateTimeOffset(DateOnly.MaxValue, TimeOnly.MaxValue, TimeSpan.FromHours(14));
            VerifyDateTimeOffset(dateTimeOffset, 9999, 12, 31, 23, 59, 59, 999, 999, TimeSpan.FromHours(14), 900);

            dateTimeOffset = new DateTimeOffset(new DateOnly(2012, 12, 31), new TimeOnly(13, 50, 10), TimeSpan.Zero);
            VerifyDateTimeOffset(dateTimeOffset, 2012, 12, 31, 13, 50, 10, 0, 0, TimeSpan.Zero);

            DateTimeOffset now = DateTimeOffset.Now;
            DateTimeOffset constructed = new DateTimeOffset(DateOnly.FromDateTime(now.DateTime), TimeOnly.FromDateTime(now.DateTime), now.Offset);
            Assert.Equal(now, constructed);
        }

        [Fact]
        public static void Ctor_DateTime_TimeSpan()
        {
            var dateTimeOffset = new DateTimeOffset(DateTime.MinValue, TimeSpan.FromHours(-14));
            VerifyDateTimeOffset(dateTimeOffset, 1, 1, 1, 0, 0, 0, 0, 0, TimeSpan.FromHours(-14));

            dateTimeOffset = new DateTimeOffset(DateTime.MaxValue, TimeSpan.FromHours(14));
            VerifyDateTimeOffset(dateTimeOffset, 9999, 12, 31, 23, 59, 59, 999, 999, TimeSpan.FromHours(14), 900);

            dateTimeOffset = new DateTimeOffset(new DateTime(2012, 12, 31, 13, 50, 10), TimeSpan.Zero);
            VerifyDateTimeOffset(dateTimeOffset, 2012, 12, 31, 13, 50, 10, 0, 0, TimeSpan.Zero);
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
            AssertExtensions.Throws<ArgumentException>("offset", () => new DateTimeOffset(DateTime.UtcNow, new TimeSpan(0, 0, 0, 0, 0, 3))); // TimeSpan is not whole minutes
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
            AssertExtensions.Throws<ArgumentOutOfRangeException>("microsecond", () => new DateTimeOffset(new DateTime(min.Year, min.Month, min.Day, min.Hour, min.Minute, min.Second, min.Millisecond, min.Microsecond - 1, DateTimeKind.Utc), TimeSpan.Zero));

            // DateTime > DateTimeOffset.MaxValue
            DateTimeOffset max = DateTimeOffset.MaxValue;
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(new DateTime(max.Year + 1, max.Month, max.Day, max.Hour, max.Minute, max.Second, max.Millisecond, DateTimeKind.Utc), TimeSpan.Zero));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(new DateTime(max.Year, max.Month + 1, max.Day, max.Hour, max.Minute, max.Second, max.Millisecond, DateTimeKind.Utc), TimeSpan.Zero));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(new DateTime(max.Year, max.Month, max.Day + 1, max.Hour, max.Minute, max.Second, max.Millisecond, DateTimeKind.Utc), TimeSpan.Zero));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(new DateTime(max.Year, max.Month, max.Day, max.Hour + 1, max.Minute, max.Second, max.Millisecond, DateTimeKind.Utc), TimeSpan.Zero));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(new DateTime(max.Year, max.Month, max.Day, max.Hour, max.Minute + 1, max.Second, max.Millisecond, DateTimeKind.Utc), TimeSpan.Zero));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(new DateTime(max.Year, max.Month, max.Day, max.Hour, max.Minute, max.Second + 1, max.Millisecond, DateTimeKind.Utc), TimeSpan.Zero));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("millisecond", () => new DateTimeOffset(new DateTime(max.Year, max.Month, max.Day, max.Hour, max.Minute, max.Second, max.Millisecond + 1, DateTimeKind.Utc), TimeSpan.Zero));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("microsecond", () => new DateTimeOffset(new DateTime(max.Year, max.Month, max.Day, max.Hour, max.Minute, max.Second, max.Millisecond, max.Microsecond + 1, DateTimeKind.Utc), TimeSpan.Zero));

            // Invalid offset
            AssertExtensions.Throws<ArgumentException>("offset", () => new DateTimeOffset(DateTime.Now, TimeSpan.FromTicks(1)));
            AssertExtensions.Throws<ArgumentException>("offset", () => new DateTimeOffset(DateTime.UtcNow, TimeSpan.FromTicks(1)));
        }

        [Fact]
        public static void Ctor_Long_TimeSpan()
        {
            var expected = new DateTime(1, 2, 3, 4, 5, 6, 7);
            var dateTimeOffset = new DateTimeOffset(expected.Ticks, TimeSpan.Zero);
            VerifyDateTimeOffset(dateTimeOffset, dateTimeOffset.Year, dateTimeOffset.Month, dateTimeOffset.Day, dateTimeOffset.Hour, dateTimeOffset.Minute, dateTimeOffset.Second, dateTimeOffset.Millisecond, dateTimeOffset.Microsecond, TimeSpan.Zero);
        }

        [Fact]
        public static void Ctor_Long_TimeSpan_Invalid()
        {
            AssertExtensions.Throws<ArgumentException>("offset", () => new DateTimeOffset(0, new TimeSpan(0, 0, 3))); // TimeSpan is not whole minutes
            AssertExtensions.Throws<ArgumentException>("offset", () => new DateTimeOffset(0, new TimeSpan(0, 0, -3))); // TimeSpan is not whole minutes
            AssertExtensions.Throws<ArgumentException>("offset", () => new DateTimeOffset(0, new TimeSpan(0, 0, 0, 0, 3))); // TimeSpan is not whole minutes
            AssertExtensions.Throws<ArgumentException>("offset", () => new DateTimeOffset(0, new TimeSpan(0, 0, 0, 0, -3))); // TimeSpan is not whole minutes
            AssertExtensions.Throws<ArgumentException>("offset", () => new DateTimeOffset(0, new TimeSpan(0, 0, 0, 0, 0, 3))); // TimeSpan is not whole minutes
            AssertExtensions.Throws<ArgumentException>("offset", () => new DateTimeOffset(0, new TimeSpan(0, 0, 0, 0, 0, -3))); // TimeSpan is not whole minutes

            AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => new DateTimeOffset(0, TimeSpan.FromHours(-15))); // TimeZone.Offset > 14
            AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => new DateTimeOffset(0, TimeSpan.FromHours(15))); // TimeZone.Offset < -14

            AssertExtensions.Throws<ArgumentOutOfRangeException>("ticks", () => new DateTimeOffset(DateTimeOffset.MinValue.Ticks - 1, TimeSpan.Zero)); // Ticks < DateTimeOffset.MinValue.Ticks
            AssertExtensions.Throws<ArgumentOutOfRangeException>("ticks", () => new DateTimeOffset(DateTimeOffset.MaxValue.Ticks + 1, TimeSpan.Zero)); // Ticks > DateTimeOffset.MaxValue.Ticks
        }

        [Fact]
        public static void Ctor_Int_Int_Int_Int_Int_Int_Int_Int_TimeSpan()
        {
            var dateTimeOffset = new DateTimeOffset(1973, 10, 6, 14, 30, 0, 500, 400, TimeSpan.Zero);
            VerifyDateTimeOffset(dateTimeOffset, 1973, 10, 6, 14, 30, 0, 500, 400, TimeSpan.Zero);
        }

        [Fact]
        public static void Ctor_Int_Int_Int_Int_Int_Int_Int_TimeSpan()
        {
            var dateTimeOffset = new DateTimeOffset(1973, 10, 6, 14, 30, 0, 500, TimeSpan.Zero);
            VerifyDateTimeOffset(dateTimeOffset, 1973, 10, 6, 14, 30, 0, 500, 0, TimeSpan.Zero);
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

            AssertExtensions.Throws<ArgumentOutOfRangeException>("microsecond", () => new DateTimeOffset(1, 1, 1, 1, 1, 1, 0, -1, TimeSpan.Zero)); // Microsecond < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("microsecond", () => new DateTimeOffset(1, 1, 1, 1, 1, 1, 0, 1000, TimeSpan.Zero)); // Microsecond > 999

            // DateTime < DateTimeOffset.MinValue
            DateTimeOffset min = DateTimeOffset.MinValue;
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(min.Year - 1, min.Month, min.Day, min.Hour, min.Minute, min.Second, min.Millisecond, TimeSpan.Zero));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(min.Year, min.Month - 1, min.Day, min.Hour, min.Minute, min.Second, min.Millisecond, TimeSpan.Zero));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(min.Year, min.Month, min.Day - 1, min.Hour, min.Minute, min.Second, min.Millisecond, TimeSpan.Zero));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(min.Year, min.Month, min.Day, min.Hour - 1, min.Minute, min.Second, min.Millisecond, TimeSpan.Zero));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(min.Year, min.Month, min.Day, min.Hour, min.Minute - 1, min.Second, min.Millisecond, TimeSpan.Zero));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(min.Year, min.Month, min.Day, min.Hour, min.Minute, min.Second - 1, min.Millisecond, TimeSpan.Zero));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("millisecond", () => new DateTimeOffset(min.Year, min.Month, min.Day, min.Hour, min.Minute, min.Second, min.Millisecond - 1, TimeSpan.Zero));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("microsecond", () => new DateTimeOffset(min.Year, min.Month, min.Day, min.Hour, min.Minute, min.Second, min.Millisecond, min.Microsecond - 1, TimeSpan.Zero));

            // DateTime > DateTimeOffset.MaxValue
            DateTimeOffset max = DateTimeOffset.MaxValue;
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(max.Year + 1, max.Month, max.Day, max.Hour, max.Minute, max.Second, max.Millisecond, TimeSpan.Zero));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(max.Year, max.Month + 1, max.Day + 1, max.Hour, max.Minute, max.Second, max.Millisecond, TimeSpan.Zero));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(max.Year, max.Month, max.Day + 1, max.Hour, max.Minute, max.Second, max.Millisecond, TimeSpan.Zero));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(max.Year, max.Month, max.Day, max.Hour + 1, max.Minute, max.Second, max.Millisecond, TimeSpan.Zero));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(max.Year, max.Month, max.Day, max.Hour, max.Minute + 1, max.Second, max.Millisecond, TimeSpan.Zero));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(max.Year, max.Month, max.Day, max.Hour, max.Minute, max.Second + 1, max.Millisecond, TimeSpan.Zero));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("millisecond", () => new DateTimeOffset(max.Year, max.Month, max.Day, max.Hour, max.Minute, max.Second, max.Millisecond + 1, TimeSpan.Zero));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("microsecond", () => new DateTimeOffset(max.Year, max.Month, max.Day, max.Hour, max.Minute, max.Second, max.Millisecond, max.Microsecond + 1, TimeSpan.Zero));
        }

        [Fact]
        public static void Ctor_Int_Int_Int_Int_Int_Int_TimeSpan()
        {
            var dateTimeOffset = new DateTimeOffset(1973, 10, 6, 14, 30, 0, TimeSpan.Zero);
            VerifyDateTimeOffset(dateTimeOffset, 1973, 10, 6, 14, 30, 0, 0, 0, TimeSpan.Zero);
        }

        [Fact]
        public static void Ctor_Int_Int_Int_Int_Int_Int_TimeSpan_Invalid()
        {
            AssertExtensions.Throws<ArgumentException>("offset", () => new DateTimeOffset(1, 1, 1, 1, 1, 1, new TimeSpan(0, 0, 3))); // TimeSpan is not whole minutes
            AssertExtensions.Throws<ArgumentException>("offset", () => new DateTimeOffset(1, 1, 1, 1, 1, 1, new TimeSpan(0, 0, -3))); // TimeSpan is not whole minutes
            AssertExtensions.Throws<ArgumentException>("offset", () => new DateTimeOffset(1, 1, 1, 1, 1, 1, new TimeSpan(0, 0, 0, 0, 3))); // TimeSpan is not whole minutes
            AssertExtensions.Throws<ArgumentException>("offset", () => new DateTimeOffset(1, 1, 1, 1, 1, 1, new TimeSpan(0, 0, 0, 0, -3))); // TimeSpan is not whole minutes
            AssertExtensions.Throws<ArgumentException>("offset", () => new DateTimeOffset(1, 1, 1, 1, 1, 1, new TimeSpan(0, 0, 0, 0, 0, 3))); // TimeSpan is not whole minutes
            AssertExtensions.Throws<ArgumentException>("offset", () => new DateTimeOffset(1, 1, 1, 1, 1, 1, new TimeSpan(0, 0, 0, 0, 0, -3))); // TimeSpan is not whole minutes

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

        [Theory]
        [InlineData(2022, 12, 31, 23, 59, 59)]
        [InlineData(2000, 1, 1, 12, 34, 59)]
        [InlineData(2005, 2, 3, 4, 4, 1)]
        public static void Deconstruct_DateOnly_TimeOnly_TimeSpan(int year, int month, int day, int hour, int minute, int second)
        {
            var date = new DateOnly(year, month, day);
            var time = new TimeOnly(hour, minute, second);

            var offset = TimeSpan.FromHours(10);
            var dateTimeOffset = new DateTimeOffset(date, time, offset);
            var (obtainedDate, obtainedTime, obtainedOffset) = dateTimeOffset;
            
            Assert.Equal(date, obtainedDate);
            Assert.Equal(time, obtainedTime);
            Assert.Equal(offset, obtainedOffset);
        }

        [Fact]
        public static void ImplicitCast_DateTime()
        {
            DateTime dateTime = new DateTime(2012, 6, 11, 0, 0, 0, 0, DateTimeKind.Utc);
            DateTimeOffset dateTimeOffset = dateTime;
            VerifyDateTimeOffset(dateTimeOffset, 2012, 6, 11, 0, 0, 0, 0, 0, TimeSpan.Zero);
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
            yield return new object[] { dateTimeOffset1, dateTimeOffset2, -new TimeSpan(185, 14, 47, 0) };
            yield return new object[] { dateTimeOffset1, dateTimeOffset3, -new TimeSpan(130, 10, 27, 0) };
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
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => DateTimeOffset.Now.AddYears(10001));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => DateTimeOffset.Now.AddYears(-10001));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => DateTimeOffset.MaxValue.AddYears(1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => DateTimeOffset.MinValue.AddYears(-1));
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

        public static IEnumerable<object[]> AddMicroseconds_TestData()
        {
            yield return new object[] { new DateTimeOffset(new DateTime(1986, 8, 15, 10, 20, 5, 70, 70, DateTimeKind.Utc)), 10, new DateTimeOffset(new DateTime(1986, 8, 15, 10, 20, 5, 70, 80, DateTimeKind.Utc)) };
            yield return new object[] { new DateTimeOffset(new DateTime(1986, 8, 15, 10, 20, 5, 70, 70, DateTimeKind.Utc)), 0, new DateTimeOffset(new DateTime(1986, 8, 15, 10, 20, 5, 70, 70, DateTimeKind.Utc)) };
            yield return new object[] { new DateTimeOffset(new DateTime(1986, 8, 15, 10, 20, 5, 70, 70, DateTimeKind.Utc)), -10, new DateTimeOffset(new DateTime(1986, 8, 15, 10, 20, 5, 70, 60, DateTimeKind.Utc)) };
        }

        [Theory]
        [MemberData(nameof(AddMicroseconds_TestData))]
        public static void AddMicroseconds(DateTimeOffset dateTimeOffset, double microseconds, DateTimeOffset expected)
        {
            Assert.Equal(expected, dateTimeOffset.AddMicroseconds(microseconds));
        }

        [Fact]
        public static void AddMicroseconds_NewDateOutOfRange_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => DateTimeOffset.MaxValue.AddMicroseconds(1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => DateTimeOffset.MinValue.AddMicroseconds(-1));
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

        public static IEnumerable<object[]> UnixTime_TestData()
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

        public static bool IsMinValueNegativeLocalOffset() => TimeZoneInfo.Local.GetUtcOffset(DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc)).Ticks < 0;

        [ConditionalFact(nameof(IsMinValueNegativeLocalOffset))]
        public static void ToLocalTime_MinValue()
        {
            DateTimeOffset dateTimeOffset = new DateTimeOffset(DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc));
            TimeSpan offset = TimeZoneInfo.Local.GetUtcOffset(dateTimeOffset);
            Assert.Equal(new DateTimeOffset(DateTime.MinValue, offset), dateTimeOffset.ToLocalTime());
        }

        public static bool IsMaxValuePositiveLocalOffset() => TimeZoneInfo.Local.GetUtcOffset(DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc)).Ticks > 0;

        [ConditionalFact(nameof(IsMaxValuePositiveLocalOffset))]
        public static void ToLocalTime_MaxValue()
        {
            DateTimeOffset dateTimeOffset = new DateTimeOffset(DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc));
            TimeSpan offset = TimeZoneInfo.Local.GetUtcOffset(dateTimeOffset);
            Assert.Equal(new DateTimeOffset(DateTime.MaxValue, offset), dateTimeOffset.ToLocalTime());
        }

        public static bool IsPacificTime() => TimeZoneInfo.Local.Id == "Pacific Standard Time";

        public static IEnumerable<object[]> ToLocalTime_Ambiguous_TestData()
        {
            yield return new object[] { new DateTimeOffset(2019, 11, 3, 1, 0, 0, new TimeSpan(-7, 0, 0)) };
            yield return new object[] { new DateTimeOffset(2019, 11, 3, 1, 0, 0, new TimeSpan(-8, 0, 0)) };
        }

        [ConditionalTheory(nameof(IsPacificTime))]
        [MemberData(nameof(ToLocalTime_Ambiguous_TestData))]
        public static void ToLocalTime_Ambiguous(DateTimeOffset dateTimeOffset)
        {
            Assert.True(dateTimeOffset.EqualsExact(dateTimeOffset.ToLocalTime()));
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
        public static void EqualsTest(DateTimeOffset dateTimeOffset1, object obj, bool expectedEquals, bool expectedEqualsExact)
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

        public static IEnumerable<object[]> StandardFormatSpecifiers() =>
            DateTimeTests.StandardFormatSpecifiers()
            .Where(a => !a[0].Equals("U")); // "U" isn't supported by DateTimeOffset

        [Theory]
        [MemberData(nameof(StandardFormatSpecifiers))]
        public static void ParseExact_ToStringThenParseExactRoundtrip_Success(string standardFormat)
        {
            var r = new Random(42);
            for (int i = 0; i < 200; i++) // test with a bunch of random dates
            {
                DateTimeOffset dt = new DateTimeOffset(
                    DateTimeOffset.MinValue.Ticks + (long)(r.NextDouble() * (DateTimeOffset.MaxValue.Ticks - DateTimeOffset.MinValue.Ticks)),
                    new TimeSpan(r.Next(-13, 13), r.Next(0, 60), 0));
                try
                {
                    string expected = dt.ToString(standardFormat);

                    Assert.Equal(expected, DateTimeOffset.ParseExact(expected, standardFormat, null).ToString(standardFormat));
                    Assert.Equal(expected, DateTimeOffset.ParseExact(expected, standardFormat, null, DateTimeStyles.None).ToString(standardFormat));
                    Assert.Equal(expected, DateTimeOffset.ParseExact(expected, new[] { standardFormat }, null, DateTimeStyles.None).ToString(standardFormat));
                    Assert.Equal(expected, DateTimeOffset.ParseExact(expected, new[] { standardFormat }, null, DateTimeStyles.AllowWhiteSpaces).ToString(standardFormat));

                    Assert.True(DateTimeOffset.TryParseExact(expected, standardFormat, null, DateTimeStyles.None, out DateTimeOffset actual));
                    Assert.Equal(expected, actual.ToString(standardFormat));
                    Assert.True(DateTimeOffset.TryParseExact(expected, new[] { standardFormat }, null, DateTimeStyles.None, out actual));
                    Assert.Equal(expected, actual.ToString(standardFormat));

                    // Should also parse with Parse, though may not round trip exactly
                    DateTimeOffset.Parse(expected);
                }
                catch (Exception e)
                {
                    throw new Exception(dt.DateTime.Ticks + ":" + dt.Offset.Ticks, e);
                }
            }
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

        [Theory]
        [MemberData(nameof(ParseExact_TestData_O))]
        public static void ParseExact_String_String_FormatProvider_DateTimeStyles_O(DateTimeOffset dt, string input)
        {
            string expectedString = dt.ToString("o");

            Assert.Equal(expectedString, DateTimeOffset.ParseExact(input, "o", null).ToString("o"));
            Assert.Equal(expectedString, DateTimeOffset.ParseExact(input, "o", null, DateTimeStyles.None).ToString("o"));

            const string Whitespace = " \t\r\n ";
            Assert.Equal(expectedString, DateTimeOffset.ParseExact(Whitespace + input, "o", null, DateTimeStyles.AllowLeadingWhite).ToString("o"));
            Assert.Equal(expectedString, DateTimeOffset.ParseExact(input + Whitespace, "o", null, DateTimeStyles.AllowTrailingWhite).ToString("o"));
            Assert.Equal(expectedString, DateTimeOffset.ParseExact(
                Whitespace +
                input +
                Whitespace, "o", null, DateTimeStyles.AllowLeadingWhite | DateTimeStyles.AllowTrailingWhite).ToString("o"));
            Assert.Equal(expectedString, DateTimeOffset.ParseExact(
                input.Substring(0, 27) +
                Whitespace +
                input.Substring(27), "o", null, DateTimeStyles.AllowInnerWhite).ToString("o"));
            Assert.Equal(expectedString, DateTimeOffset.ParseExact(
                Whitespace +
                input.Substring(0, 27) +
                Whitespace +
                input.Substring(27) +
                Whitespace, "o", null, DateTimeStyles.AllowWhiteSpaces).ToString("o"));
        }

        public static IEnumerable<object[]> ParseExact_TestData_O()
        {
            foreach (TimeSpan offset in new[] { TimeSpan.Zero, new TimeSpan(-1, 23, 0), new TimeSpan(7, 0, 0) })
            {
                var dto = new DateTimeOffset(new DateTime(1234567891234567891, DateTimeKind.Unspecified), offset);
                yield return new object[] { dto, dto.ToString("o") };

                yield return new object[] { DateTimeOffset.MinValue, DateTimeOffset.MinValue.ToString("o") };
                yield return new object[] { DateTimeOffset.MaxValue, DateTimeOffset.MaxValue.ToString("o") };
            }
        }

        [Theory]
        [MemberData(nameof(ParseExact_TestData_InvalidData_O))]
        public static void ParseExact_InvalidData_O(string invalidString)
        {
            Assert.Throws<FormatException>(() => DateTimeOffset.ParseExact(invalidString, "o", null));
            Assert.Throws<FormatException>(() => DateTimeOffset.ParseExact(invalidString, "o", null, DateTimeStyles.None));
            Assert.Throws<FormatException>(() => DateTimeOffset.ParseExact(invalidString, new string[] { "o" }, null, DateTimeStyles.None));
        }

        public static IEnumerable<object[]> ParseExact_TestData_InvalidData_O() =>
            DateTimeTests.ParseExact_TestData_InvalidData_O();

        [Theory]
        [MemberData(nameof(ParseExact_TestData_R))]
        public static void ParseExact_String_String_FormatProvider_DateTimeStyles_R(DateTimeOffset dt, string input)
        {
            Assert.Equal(dt.ToString("r"), DateTimeOffset.ParseExact(input, "r", null).ToString("r"));
            Assert.Equal(dt.ToString("r"), DateTimeOffset.ParseExact(input, "r", null, DateTimeStyles.None).ToString("r"));

            const string Whitespace = " \t\r\n ";
            Assert.Equal(dt.ToString("r"), DateTimeOffset.ParseExact(Whitespace + input, "r", null, DateTimeStyles.AllowLeadingWhite).ToString("r"));
            Assert.Equal(dt.ToString("r"), DateTimeOffset.ParseExact(input + Whitespace, "r", null, DateTimeStyles.AllowTrailingWhite).ToString("r"));
            Assert.Equal(dt.ToString("r"), DateTimeOffset.ParseExact(
                Whitespace +
                input +
                Whitespace, "r", null, DateTimeStyles.AllowLeadingWhite | DateTimeStyles.AllowTrailingWhite).ToString("r"));
            Assert.Equal(dt.ToString("r"), DateTimeOffset.ParseExact(
                input.Substring(0, 4) +
                Whitespace +
                input.Substring(4), "r", null, DateTimeStyles.AllowInnerWhite).ToString("r"));
            Assert.Equal(dt.ToString("r"), DateTimeOffset.ParseExact(
                Whitespace +
                input.Substring(0, 4) +
                Whitespace +
                input.Substring(4) +
                Whitespace, "r", null, DateTimeStyles.AllowWhiteSpaces).ToString("r"));
        }

        public static IEnumerable<object[]> ParseExact_TestData_R()
        {
            foreach (object[] dateTimeData in DateTimeTests.ParseExact_TestData_R())
            {
                yield return new object[] { new DateTimeOffset((DateTime)dateTimeData[0], TimeSpan.Zero), (string)dateTimeData[1] };
            }
        }

        [Theory]
        [MemberData(nameof(ParseExact_TestData_InvalidData_R))]
        public static void ParseExact_InvalidData_R(string invalidString)
        {
            Assert.Throws<FormatException>(() => DateTimeOffset.ParseExact(invalidString, "r", null));
            Assert.Throws<FormatException>(() => DateTimeOffset.ParseExact(invalidString, "r", null, DateTimeStyles.None));
            Assert.Throws<FormatException>(() => DateTimeOffset.ParseExact(invalidString, new string[] { "r" }, null, DateTimeStyles.None));
        }

        public static IEnumerable<object[]> ParseExact_TestData_InvalidData_R() =>
            DateTimeTests.ParseExact_TestData_InvalidData_R();

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

        private static void VerifyDateTimeOffset(DateTimeOffset dateTimeOffset, int year, int month, int day, int hour, int minute, int second, int millisecond, int microsecond, TimeSpan? offset, int nanosecond = 0)
        {
            Assert.Equal(year, dateTimeOffset.Year);
            Assert.Equal(month, dateTimeOffset.Month);
            Assert.Equal(day, dateTimeOffset.Day);
            Assert.Equal(hour, dateTimeOffset.Hour);
            Assert.Equal(minute, dateTimeOffset.Minute);
            Assert.Equal(second, dateTimeOffset.Second);
            Assert.Equal(millisecond, dateTimeOffset.Millisecond);
            Assert.Equal(microsecond, dateTimeOffset.Microsecond);
            Assert.Equal(nanosecond, dateTimeOffset.Nanosecond);

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
            VerifyDateTimeOffset(dateTimeOffset, 1, 1, 1, 0, 0, 0, 0, 0, TimeSpan.Zero);
        }


        [Fact]
        public static void Ctor_Calendar_TimeSpan_Microseconds()
        {
            var dateTimeOffset = new DateTimeOffset(1, 1, 1, 0, 0, 0, 0, 123, new GregorianCalendar(), TimeSpan.Zero);
            VerifyDateTimeOffset(dateTimeOffset, 1, 1, 1, 0, 0, 0, 0, 123, TimeSpan.Zero);
        }

        public static IEnumerable<object[]> ToString_MatchesExpected_MemberData()
        {
            // Randomly generated data on .NET Framework with:
            //     using System;
            //     using System.Globalization;
            //     class Program
            //     {
            //         static void Main()
            //         {
            //             var rand = new Random(1);
            //             var bytes = new byte[8];
            //             for (int i = 0; i < 100; )
            //             {
            //                 const string Formats = "dDfFgGmMoOrRstTuUyY";
            //                 string format = Formats[rand.Next(Formats.Length - 1)].ToString();
            //                 try
            //                 {
            //                     rand.NextBytes(bytes);
            //                     long seed1 = BitConverter.ToInt64(bytes, 0);
            //                     short seed2 = BitConverter.ToInt16(bytes, 0);
            //                     var dto = new DateTimeOffset(seed1, TimeSpan.FromSeconds(seed2));
            //                     if (format[0] is 'o' or 'O' or 'r' or 'R' or 'u' or 's')
            //                     {
            //                         Console.WriteLine($"yield return new object[] {{ new DateTimeOffset({seed1}, TimeSpan.FromSeconds({seed2})), \"{format}\", null, \"{dto.ToString(format)}\" }};");
            //                     }
            //                     Console.WriteLine($"yield return new object[] {{ new DateTimeOffset({seed1}, TimeSpan.FromSeconds({seed2})), \"{format}\", CultureInfo.InvariantCulture, \"{dto.ToString(format, CultureInfo.InvariantCulture)}\" }};");
            //                     i++;
            //                 }
            //                 catch { }
            //             }
            //         }
            //     }
            yield return new object[] { new DateTimeOffset(2866003186733990972, TimeSpan.FromSeconds(-15300)), "d", CultureInfo.InvariantCulture, "01/02/9083" };
            yield return new object[] { new DateTimeOffset(17724261114884476, TimeSpan.FromSeconds(-20100)), "o", null, "0057-03-02T04:35:11.4884476-05:35" };
            yield return new object[] { new DateTimeOffset(17724261114884476, TimeSpan.FromSeconds(-20100)), "o", CultureInfo.InvariantCulture, "0057-03-02T04:35:11.4884476-05:35" };
            yield return new object[] { new DateTimeOffset(1922396642931594516, TimeSpan.FromSeconds(21780)), "M", CultureInfo.InvariantCulture, "October 31" };
            yield return new object[] { new DateTimeOffset(483367606946453596, TimeSpan.FromSeconds(22620)), "G", CultureInfo.InvariantCulture, "09/25/1532 05:58:14" };
            yield return new object[] { new DateTimeOffset(69949591831359208, TimeSpan.FromSeconds(-20760)), "o", null, "0222-08-31T04:13:03.1359208-05:46" };
            yield return new object[] { new DateTimeOffset(69949591831359208, TimeSpan.FromSeconds(-20760)), "o", CultureInfo.InvariantCulture, "0222-08-31T04:13:03.1359208-05:46" };
            yield return new object[] { new DateTimeOffset(2558272663974397160, TimeSpan.FromSeconds(17640)), "o", null, "8107-11-05T17:33:17.4397160+04:54" };
            yield return new object[] { new DateTimeOffset(2558272663974397160, TimeSpan.FromSeconds(17640)), "o", CultureInfo.InvariantCulture, "8107-11-05T17:33:17.4397160+04:54" };
            yield return new object[] { new DateTimeOffset(1595228036159296076, TimeSpan.FromSeconds(-11700)), "g", CultureInfo.InvariantCulture, "01/29/5056 17:53" };
            yield return new object[] { new DateTimeOffset(1482818938952517812, TimeSpan.FromSeconds(-30540)), "r", null, "Mon, 13 Nov 4699 23:27:15 GMT" };
            yield return new object[] { new DateTimeOffset(1482818938952517812, TimeSpan.FromSeconds(-30540)), "r", CultureInfo.InvariantCulture, "Mon, 13 Nov 4699 23:27:15 GMT" };
            yield return new object[] { new DateTimeOffset(2374737620308081364, TimeSpan.FromSeconds(15060)), "y", CultureInfo.InvariantCulture, "7526 March" };
            yield return new object[] { new DateTimeOffset(1480473615374849012, TimeSpan.FromSeconds(-27660)), "g", CultureInfo.InvariantCulture, "06/08/4692 03:05" };
            yield return new object[] { new DateTimeOffset(559024831959587456, TimeSpan.FromSeconds(-9600)), "M", CultureInfo.InvariantCulture, "June 24" };
            yield return new object[] { new DateTimeOffset(1184653058942329036, TimeSpan.FromSeconds(24780)), "d", CultureInfo.InvariantCulture, "01/07/3755" };
            yield return new object[] { new DateTimeOffset(516669061683837048, TimeSpan.FromSeconds(30840)), "G", CultureInfo.InvariantCulture, "04/05/1638 14:22:48" };
            yield return new object[] { new DateTimeOffset(2258308651822418992, TimeSpan.FromSeconds(3120)), "T", CultureInfo.InvariantCulture, "03:53:02" };
            yield return new object[] { new DateTimeOffset(1383517800988996320, TimeSpan.FromSeconds(-3360)), "t", CultureInfo.InvariantCulture, "18:01" };
            yield return new object[] { new DateTimeOffset(728656139270861240, TimeSpan.FromSeconds(-20040)), "u", null, "2310-01-09 05:52:47Z" };
            yield return new object[] { new DateTimeOffset(728656139270861240, TimeSpan.FromSeconds(-20040)), "u", CultureInfo.InvariantCulture, "2310-01-09 05:52:47Z" };
            yield return new object[] { new DateTimeOffset(393200115812717732, TimeSpan.FromSeconds(-11100)), "M", CultureInfo.InvariantCulture, "January 01" };
            yield return new object[] { new DateTimeOffset(1359420562790649548, TimeSpan.FromSeconds(-28980)), "u", null, "4308-11-01 18:20:59Z" };
            yield return new object[] { new DateTimeOffset(1359420562790649548, TimeSpan.FromSeconds(-28980)), "u", CultureInfo.InvariantCulture, "4308-11-01 18:20:59Z" };
            yield return new object[] { new DateTimeOffset(1235254827956094012, TimeSpan.FromSeconds(-15300)), "u", null, "3915-05-16 06:21:35Z" };
            yield return new object[] { new DateTimeOffset(1235254827956094012, TimeSpan.FromSeconds(-15300)), "u", CultureInfo.InvariantCulture, "3915-05-16 06:21:35Z" };
            yield return new object[] { new DateTimeOffset(114477656718234848, TimeSpan.FromSeconds(-11040)), "f", CultureInfo.InvariantCulture, "Tuesday, 08 October 0363 06:54" };
            yield return new object[] { new DateTimeOffset(192057803897653116, TimeSpan.FromSeconds(18300)), "O", null, "0609-08-11T02:59:49.7653116+05:05" };
            yield return new object[] { new DateTimeOffset(192057803897653116, TimeSpan.FromSeconds(18300)), "O", CultureInfo.InvariantCulture, "0609-08-11T02:59:49.7653116+05:05" };
            yield return new object[] { new DateTimeOffset(546374288963361880, TimeSpan.FromSeconds(23640)), "G", CultureInfo.InvariantCulture, "05/23/1732 15:34:56" };
            yield return new object[] { new DateTimeOffset(270121790607980192, TimeSpan.FromSeconds(-2400)), "R", null, "Sun, 24 Dec 0856 23:44:20 GMT" };
            yield return new object[] { new DateTimeOffset(270121790607980192, TimeSpan.FromSeconds(-2400)), "R", CultureInfo.InvariantCulture, "Sun, 24 Dec 0856 23:44:20 GMT" };
            yield return new object[] { new DateTimeOffset(2305676294986132560, TimeSpan.FromSeconds(-5040)), "D", CultureInfo.InvariantCulture, "Thursday, 26 May 7307" };
            yield return new object[] { new DateTimeOffset(2532230820032055596, TimeSpan.FromSeconds(-30420)), "T", CultureInfo.InvariantCulture, "17:00:03" };
            yield return new object[] { new DateTimeOffset(1076978232703995584, TimeSpan.FromSeconds(20160)), "T", CultureInfo.InvariantCulture, "14:01:10" };
            yield return new object[] { new DateTimeOffset(1339659304181130620, TimeSpan.FromSeconds(25980)), "M", CultureInfo.InvariantCulture, "March 19" };
            yield return new object[] { new DateTimeOffset(2750426316766699852, TimeSpan.FromSeconds(-7860)), "t", CultureInfo.InvariantCulture, "19:01" };
            yield return new object[] { new DateTimeOffset(972505514843635232, TimeSpan.FromSeconds(-480)), "T", CultureInfo.InvariantCulture, "02:04:44" };
            yield return new object[] { new DateTimeOffset(2427049669893407744, TimeSpan.FromSeconds(-15360)), "R", null, "Sun, 06 Jan 7692 10:39:09 GMT" };
            yield return new object[] { new DateTimeOffset(2427049669893407744, TimeSpan.FromSeconds(-15360)), "R", CultureInfo.InvariantCulture, "Sun, 06 Jan 7692 10:39:09 GMT" };
            yield return new object[] { new DateTimeOffset(2167597301268758764, TimeSpan.FromSeconds(16620)), "u", null, "6869-11-03 23:31:46Z" };
            yield return new object[] { new DateTimeOffset(2167597301268758764, TimeSpan.FromSeconds(16620)), "u", CultureInfo.InvariantCulture, "6869-11-03 23:31:46Z" };
            yield return new object[] { new DateTimeOffset(2833512902056985060, TimeSpan.FromSeconds(-15900)), "u", null, "8980-01-18 00:08:25Z" };
            yield return new object[] { new DateTimeOffset(2833512902056985060, TimeSpan.FromSeconds(-15900)), "u", CultureInfo.InvariantCulture, "8980-01-18 00:08:25Z" };
            yield return new object[] { new DateTimeOffset(2053144072711223968, TimeSpan.FromSeconds(-17760)), "F", CultureInfo.InvariantCulture, "Sunday, 27 February 6507 03:47:51" };
            yield return new object[] { new DateTimeOffset(1245775652276323568, TimeSpan.FromSeconds(-15120)), "d", CultureInfo.InvariantCulture, "09/15/3948" };
            yield return new object[] { new DateTimeOffset(3067145896534734916, TimeSpan.FromSeconds(-1980)), "s", null, "9720-05-26T09:07:33" };
            yield return new object[] { new DateTimeOffset(3067145896534734916, TimeSpan.FromSeconds(-1980)), "s", CultureInfo.InvariantCulture, "9720-05-26T09:07:33" };
            yield return new object[] { new DateTimeOffset(1149266991238407556, TimeSpan.FromSeconds(-6780)), "m", CultureInfo.InvariantCulture, "November 19" };
            yield return new object[] { new DateTimeOffset(1749528579164719160, TimeSpan.FromSeconds(-14280)), "G", CultureInfo.InvariantCulture, "01/14/5545 08:05:16" };
            yield return new object[] { new DateTimeOffset(1375434758076332332, TimeSpan.FromSeconds(31020)), "R", null, "Sat, 01 Aug 4359 00:26:27 GMT" };
            yield return new object[] { new DateTimeOffset(1375434758076332332, TimeSpan.FromSeconds(31020)), "R", CultureInfo.InvariantCulture, "Sat, 01 Aug 4359 00:26:27 GMT" };
            yield return new object[] { new DateTimeOffset(28528038954117508, TimeSpan.FromSeconds(-22140)), "D", CultureInfo.InvariantCulture, "Sunday, 27 May 0091" };
            yield return new object[] { new DateTimeOffset(685194926724353912, TimeSpan.FromSeconds(3960)), "t", CultureInfo.InvariantCulture, "16:24" };
            yield return new object[] { new DateTimeOffset(2887804226685042448, TimeSpan.FromSeconds(-240)), "D", CultureInfo.InvariantCulture, "Sunday, 03 February 9152" };
            yield return new object[] { new DateTimeOffset(863118435880997388, TimeSpan.FromSeconds(4620)), "D", CultureInfo.InvariantCulture, "Wednesday, 12 February 2736" };
            yield return new object[] { new DateTimeOffset(1131353524374008364, TimeSpan.FromSeconds(-3540)), "y", CultureInfo.InvariantCulture, "3586 February" };
            yield return new object[] { new DateTimeOffset(315210523520920416, TimeSpan.FromSeconds(-5280)), "r", null, "Tue, 12 Nov 0999 01:20:32 GMT" };
            yield return new object[] { new DateTimeOffset(315210523520920416, TimeSpan.FromSeconds(-5280)), "r", CultureInfo.InvariantCulture, "Tue, 12 Nov 0999 01:20:32 GMT" };
            yield return new object[] { new DateTimeOffset(2230965644288159332, TimeSpan.FromSeconds(28260)), "D", CultureInfo.InvariantCulture, "Friday, 26 August 7070" };
            yield return new object[] { new DateTimeOffset(2347007386434452848, TimeSpan.FromSeconds(-17040)), "M", CultureInfo.InvariantCulture, "May 16" };
            yield return new object[] { new DateTimeOffset(3057726375364446024, TimeSpan.FromSeconds(-14520)), "T", CultureInfo.InvariantCulture, "03:45:36" };
            yield return new object[] { new DateTimeOffset(1346223178375097544, TimeSpan.FromSeconds(-4920)), "T", CultureInfo.InvariantCulture, "16:17:17" };
            yield return new object[] { new DateTimeOffset(2139495673968405100, TimeSpan.FromSeconds(10860)), "G", CultureInfo.InvariantCulture, "10/17/6780 03:23:16" };
            yield return new object[] { new DateTimeOffset(1393959301494092688, TimeSpan.FromSeconds(13200)), "r", null, "Fri, 13 Apr 4418 16:02:29 GMT" };
            yield return new object[] { new DateTimeOffset(1393959301494092688, TimeSpan.FromSeconds(13200)), "r", CultureInfo.InvariantCulture, "Fri, 13 Apr 4418 16:02:29 GMT" };
            yield return new object[] { new DateTimeOffset(2354387478841092052, TimeSpan.FromSeconds(26580)), "u", null, "7461-10-04 04:48:24Z" };
            yield return new object[] { new DateTimeOffset(2354387478841092052, TimeSpan.FromSeconds(26580)), "u", CultureInfo.InvariantCulture, "7461-10-04 04:48:24Z" };
            yield return new object[] { new DateTimeOffset(2695673889181138404, TimeSpan.FromSeconds(-540)), "T", CultureInfo.InvariantCulture, "22:15:18" };
            yield return new object[] { new DateTimeOffset(3129400016207346692, TimeSpan.FromSeconds(-1020)), "f", CultureInfo.InvariantCulture, "Tuesday, 04 September 9917 18:13" };
            yield return new object[] { new DateTimeOffset(268510768954133436, TimeSpan.FromSeconds(-13380)), "m", CultureInfo.InvariantCulture, "November 17" };
            yield return new object[] { new DateTimeOffset(1675324527268079456, TimeSpan.FromSeconds(10080)), "y", CultureInfo.InvariantCulture, "5309 November" };
            yield return new object[] { new DateTimeOffset(1226179294413194056, TimeSpan.FromSeconds(840)), "s", null, "3886-08-10T23:57:21" };
            yield return new object[] { new DateTimeOffset(1226179294413194056, TimeSpan.FromSeconds(840)), "s", CultureInfo.InvariantCulture, "3886-08-10T23:57:21" };
            yield return new object[] { new DateTimeOffset(1541746737990743828, TimeSpan.FromSeconds(14100)), "g", CultureInfo.InvariantCulture, "08/08/4886 02:16" };
            yield return new object[] { new DateTimeOffset(3014961136493232496, TimeSpan.FromSeconds(-32400)), "O", null, "9555-01-13T08:27:29.3232496-09:00" };
            yield return new object[] { new DateTimeOffset(3014961136493232496, TimeSpan.FromSeconds(-32400)), "O", CultureInfo.InvariantCulture, "9555-01-13T08:27:29.3232496-09:00" };
            yield return new object[] { new DateTimeOffset(93553939704192692, TimeSpan.FromSeconds(7860)), "m", CultureInfo.InvariantCulture, "June 18" };
            yield return new object[] { new DateTimeOffset(1474648860395527136, TimeSpan.FromSeconds(23520)), "s", null, "4673-12-23T12:20:39" };
            yield return new object[] { new DateTimeOffset(1474648860395527136, TimeSpan.FromSeconds(23520)), "s", CultureInfo.InvariantCulture, "4673-12-23T12:20:39" };
            yield return new object[] { new DateTimeOffset(1295864623067010816, TimeSpan.FromSeconds(-26880)), "M", CultureInfo.InvariantCulture, "June 08" };
            yield return new object[] { new DateTimeOffset(2739123151422226552, TimeSpan.FromSeconds(120)), "o", null, "8680-12-08T10:12:22.2226552+00:02" };
            yield return new object[] { new DateTimeOffset(2739123151422226552, TimeSpan.FromSeconds(120)), "o", CultureInfo.InvariantCulture, "8680-12-08T10:12:22.2226552+00:02" };
            yield return new object[] { new DateTimeOffset(1515671391938001628, TimeSpan.FromSeconds(-17700)), "o", null, "4803-12-22T07:06:33.8001628-04:55" };
            yield return new object[] { new DateTimeOffset(1515671391938001628, TimeSpan.FromSeconds(-17700)), "o", CultureInfo.InvariantCulture, "4803-12-22T07:06:33.8001628-04:55" };
            yield return new object[] { new DateTimeOffset(1675967728676687860, TimeSpan.FromSeconds(-12300)), "D", CultureInfo.InvariantCulture, "Monday, 07 December 5311" };
            yield return new object[] { new DateTimeOffset(2231850043577254012, TimeSpan.FromSeconds(-16260)), "O", null, "7073-06-14T18:32:37.7254012-04:31" };
            yield return new object[] { new DateTimeOffset(2231850043577254012, TimeSpan.FromSeconds(-16260)), "O", CultureInfo.InvariantCulture, "7073-06-14T18:32:37.7254012-04:31" };
            yield return new object[] { new DateTimeOffset(763201213144623488, TimeSpan.FromSeconds(-5760)), "y", CultureInfo.InvariantCulture, "2419 June" };
            yield return new object[] { new DateTimeOffset(1902743697611562976, TimeSpan.FromSeconds(8160)), "R", null, "Mon, 22 Jul 6030 13:20:01 GMT" };
            yield return new object[] { new DateTimeOffset(1902743697611562976, TimeSpan.FromSeconds(8160)), "R", CultureInfo.InvariantCulture, "Mon, 22 Jul 6030 13:20:01 GMT" };
            yield return new object[] { new DateTimeOffset(2170088908634528380, TimeSpan.FromSeconds(6780)), "M", CultureInfo.InvariantCulture, "September 26" };
            yield return new object[] { new DateTimeOffset(2945387119699905100, TimeSpan.FromSeconds(19020)), "u", null, "9334-07-24 15:35:49Z" };
            yield return new object[] { new DateTimeOffset(2945387119699905100, TimeSpan.FromSeconds(19020)), "u", CultureInfo.InvariantCulture, "9334-07-24 15:35:49Z" };
            yield return new object[] { new DateTimeOffset(2210427227472213120, TimeSpan.FromSeconds(13440)), "d", CultureInfo.InvariantCulture, "07/26/7005" };
            yield return new object[] { new DateTimeOffset(1172280359902648716, TimeSpan.FromSeconds(6540)), "r", null, "Wed, 23 Oct 3715 21:30:50 GMT" };
            yield return new object[] { new DateTimeOffset(1172280359902648716, TimeSpan.FromSeconds(6540)), "r", CultureInfo.InvariantCulture, "Wed, 23 Oct 3715 21:30:50 GMT" };
            yield return new object[] { new DateTimeOffset(193781915832174040, TimeSpan.FromSeconds(17880)), "G", CultureInfo.InvariantCulture, "01/27/0615 14:59:43" };
            yield return new object[] { new DateTimeOffset(1434340271653145524, TimeSpan.FromSeconds(19380)), "s", null, "4546-03-31T01:19:25" };
            yield return new object[] { new DateTimeOffset(1434340271653145524, TimeSpan.FromSeconds(19380)), "s", CultureInfo.InvariantCulture, "4546-03-31T01:19:25" };
            yield return new object[] { new DateTimeOffset(2232002475903745148, TimeSpan.FromSeconds(-900)), "R", null, "Mon, 08 Dec 7073 05:01:30 GMT" };
            yield return new object[] { new DateTimeOffset(2232002475903745148, TimeSpan.FromSeconds(-900)), "R", CultureInfo.InvariantCulture, "Mon, 08 Dec 7073 05:01:30 GMT" };
            yield return new object[] { new DateTimeOffset(75862097515405336, TimeSpan.FromSeconds(24600)), "r", null, "Wed, 26 May 0241 01:39:11 GMT" };
            yield return new object[] { new DateTimeOffset(75862097515405336, TimeSpan.FromSeconds(24600)), "r", CultureInfo.InvariantCulture, "Wed, 26 May 0241 01:39:11 GMT" };
            yield return new object[] { new DateTimeOffset(1311882990195785184, TimeSpan.FromSeconds(15840)), "s", null, "4158-03-12T02:10:19" };
            yield return new object[] { new DateTimeOffset(1311882990195785184, TimeSpan.FromSeconds(15840)), "s", CultureInfo.InvariantCulture, "4158-03-12T02:10:19" };
            yield return new object[] { new DateTimeOffset(2492654623258780576, TimeSpan.FromSeconds(9120)), "t", CultureInfo.InvariantCulture, "22:12" };
            yield return new object[] { new DateTimeOffset(3044591719812160368, TimeSpan.FromSeconds(-9360)), "O", null, "9648-12-05T00:13:01.2160368-02:36" };
            yield return new object[] { new DateTimeOffset(3044591719812160368, TimeSpan.FromSeconds(-9360)), "O", CultureInfo.InvariantCulture, "9648-12-05T00:13:01.2160368-02:36" };
            yield return new object[] { new DateTimeOffset(2782989619967598208, TimeSpan.FromSeconds(-24960)), "o", null, "8819-12-11T19:13:16.7598208-06:56" };
            yield return new object[] { new DateTimeOffset(2782989619967598208, TimeSpan.FromSeconds(-24960)), "o", CultureInfo.InvariantCulture, "8819-12-11T19:13:16.7598208-06:56" };
            yield return new object[] { new DateTimeOffset(2691678346098786600, TimeSpan.FromSeconds(16680)), "G", CultureInfo.InvariantCulture, "08/04/8530 10:56:49" };
            yield return new object[] { new DateTimeOffset(2863023557156129544, TimeSpan.FromSeconds(-13560)), "M", CultureInfo.InvariantCulture, "July 24" };
            yield return new object[] { new DateTimeOffset(2141892998746916860, TimeSpan.FromSeconds(-14340)), "o", null, "6788-05-22T19:44:34.6916860-03:59" };
            yield return new object[] { new DateTimeOffset(2141892998746916860, TimeSpan.FromSeconds(-14340)), "o", CultureInfo.InvariantCulture, "6788-05-22T19:44:34.6916860-03:59" };
            yield return new object[] { new DateTimeOffset(1647902714725003280, TimeSpan.FromSeconds(-4080)), "o", null, "5222-12-30T19:24:32.5003280-01:08" };
            yield return new object[] { new DateTimeOffset(1647902714725003280, TimeSpan.FromSeconds(-4080)), "o", CultureInfo.InvariantCulture, "5222-12-30T19:24:32.5003280-01:08" };
            yield return new object[] { new DateTimeOffset(3015783691236408744, TimeSpan.FromSeconds(-600)), "G", CultureInfo.InvariantCulture, "08/22/9557 09:12:03" };
            yield return new object[] { new DateTimeOffset(1723006204695438056, TimeSpan.FromSeconds(25320)), "m", CultureInfo.InvariantCulture, "December 28" };
            yield return new object[] { new DateTimeOffset(205672147887324044, TimeSpan.FromSeconds(-1140)), "m", CultureInfo.InvariantCulture, "October 01" };
            yield return new object[] { new DateTimeOffset(176841286150132116, TimeSpan.FromSeconds(4500)), "d", CultureInfo.InvariantCulture, "05/22/0561" };
            yield return new object[] { new DateTimeOffset(358056122991012336, TimeSpan.FromSeconds(27120)), "f", CultureInfo.InvariantCulture, "Wednesday, 21 August 1135 19:24" };
            yield return new object[] { new DateTimeOffset(504008756218880332, TimeSpan.FromSeconds(-7860)), "m", CultureInfo.InvariantCulture, "February 21" };
            yield return new object[] { new DateTimeOffset(2338734206946518640, TimeSpan.FromSeconds(9840)), "M", CultureInfo.InvariantCulture, "February 27" };
            yield return new object[] { new DateTimeOffset(2514061437370836652, TimeSpan.FromSeconds(9900)), "o", null, "7967-09-30T07:55:37.0836652+02:45" };
            yield return new object[] { new DateTimeOffset(2514061437370836652, TimeSpan.FromSeconds(9900)), "o", CultureInfo.InvariantCulture, "7967-09-30T07:55:37.0836652+02:45" };
            yield return new object[] { new DateTimeOffset(2910141711367555000, TimeSpan.FromSeconds(18360)), "r", null, "Tue, 15 Nov 9222 08:39:36 GMT" };
            yield return new object[] { new DateTimeOffset(2910141711367555000, TimeSpan.FromSeconds(18360)), "r", CultureInfo.InvariantCulture, "Tue, 15 Nov 9222 08:39:36 GMT" };
            yield return new object[] { new DateTimeOffset(2268815489190300608, TimeSpan.FromSeconds(-29760)), "M", CultureInfo.InvariantCulture, "August 04" };
            yield return new object[] { new DateTimeOffset(2270906825242135956, TimeSpan.FromSeconds(19860)), "T", CultureInfo.InvariantCulture, "09:08:44" };
            yield return new object[] { new DateTimeOffset(1257384048216439692, TimeSpan.FromSeconds(-1140)), "D", CultureInfo.InvariantCulture, "Saturday, 29 June 3985" };
            yield return new object[] { new DateTimeOffset(2905726883951199532, TimeSpan.FromSeconds(-15060)), "F", CultureInfo.InvariantCulture, "Tuesday, 18 November 9208 19:39:55" };
            yield return new object[] { new DateTimeOffset(1963041343129029388, TimeSpan.FromSeconds(-29940)), "r", null, "Sun, 19 Aug 6221 22:30:52 GMT" };
            yield return new object[] { new DateTimeOffset(1963041343129029388, TimeSpan.FromSeconds(-29940)), "r", CultureInfo.InvariantCulture, "Sun, 19 Aug 6221 22:30:52 GMT" };
            yield return new object[] { new DateTimeOffset(1344688163288837500, TimeSpan.FromSeconds(-4740)), "G", CultureInfo.InvariantCulture, "02/24/4262 00:58:48" };
            yield return new object[] { new DateTimeOffset(2904241358872885260, TimeSpan.FromSeconds(-18420)), "y", CultureInfo.InvariantCulture, "9204 March" };
            yield return new object[] { new DateTimeOffset(2226716916868971284, TimeSpan.FromSeconds(-1260)), "G", CultureInfo.InvariantCulture, "03/09/7057 15:41:26" };

            // Year patterns
            var enUS = new CultureInfo("en-US");
            var thTH = new CultureInfo("th-TH");
            yield return new object[] { new DateTimeOffset(new DateTime(1234, 5, 6)), "yy", enUS, "34" };
            if (PlatformDetection.IsNotInvariantGlobalization)
            {
                yield return new object[] { DateTimeOffset.MaxValue, "yy", thTH, "42" };
            }

            for (int i = 3; i < 20; i++)
            {
                yield return new object[] { new DateTimeOffset(new DateTime(1234, 5, 6)), new string('y', i), enUS, 1234.ToString("D" + i) };

                if (PlatformDetection.IsNotInvariantGlobalization)
                {
                    yield return new object[] { DateTimeOffset.MaxValue, new string('y', i), thTH, 10542.ToString("D" + i) };
                }
            }

            // Non-ASCII in format string
            yield return new object[] { new DateTimeOffset(2023, 04, 17, 10, 46, 12, TimeSpan.Zero), "HH\u202dmm", null, "10\u202d46" };
        }

        [Theory]
        [MemberData(nameof(ToString_MatchesExpected_MemberData))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/60562", TestPlatforms.Android | TestPlatforms.LinuxBionic)]
        public static void ToString_MatchesExpected(DateTimeOffset dateTimeOffset, string format, IFormatProvider provider, string expected)
        {
            if (provider == null)
            {
                Assert.Equal(expected, dateTimeOffset.ToString(format));
            }

            Assert.Equal(expected, dateTimeOffset.ToString(format, provider));
        }

        public static IEnumerable<object[]> ToString_WithCulture_MatchesExpected_MemberData()
        {
            yield return new object[] { new DateTimeOffset(636572516255571994, TimeSpan.FromHours(-5)), "M", new CultureInfo("fr-FR"), "21 mars" };
            yield return new object[] { new DateTimeOffset(636572516255571994, TimeSpan.FromHours(-5)), "Y", new CultureInfo("da-DK"), "marts 2018" };
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotInvariantGlobalization))]
        [MemberData(nameof(ToString_WithCulture_MatchesExpected_MemberData))]
        public static void ToString_WithCulture_MatchesExpected(DateTimeOffset dateTimeOffset, string format, CultureInfo culture, string expected)
        {
            Assert.Equal(expected, dateTimeOffset.ToString(format, culture));
        }

        [Fact]
        public static void ToString_ParseSpan_RoundtripsSuccessfully()
        {
            DateTimeOffset expected = DateTimeOffset.MaxValue;
            string expectedString = expected.ToString();

            Assert.Equal(expectedString, DateTimeOffset.Parse(expectedString.AsSpan()).ToString());
            Assert.Equal(expectedString, DateTimeOffset.Parse(expectedString.AsSpan(), null).ToString());
            Assert.Equal(expectedString, DateTimeOffset.Parse(expectedString.AsSpan(), null, DateTimeStyles.None).ToString());

            Assert.True(DateTimeOffset.TryParse(expectedString.AsSpan(), out DateTimeOffset actual));
            Assert.Equal(expectedString, actual.ToString());
            Assert.True(DateTimeOffset.TryParse(expectedString.AsSpan(), null, DateTimeStyles.None, out actual));
            Assert.Equal(expectedString, actual.ToString());
        }

        [Theory]
        [InlineData("r")]
        [InlineData("o")]
        public static void ToString_Slice_ParseSpan_RoundtripsSuccessfully(string roundtripFormat)
        {
            string expectedString = DateTimeOffset.UtcNow.ToString(roundtripFormat);
            ReadOnlySpan<char> expectedSpan = ("abcd" + expectedString + "1234").AsSpan("abcd".Length, expectedString.Length);

            Assert.Equal(expectedString, DateTimeOffset.Parse(expectedSpan).ToString(roundtripFormat));
            Assert.Equal(expectedString, DateTimeOffset.Parse(expectedSpan, null).ToString(roundtripFormat));
            Assert.Equal(expectedString, DateTimeOffset.Parse(expectedSpan, null, DateTimeStyles.None).ToString(roundtripFormat));

            Assert.True(DateTimeOffset.TryParse(expectedSpan, out DateTimeOffset actual));
            Assert.Equal(expectedString, actual.ToString(roundtripFormat));
            Assert.True(DateTimeOffset.TryParse(expectedSpan, null, DateTimeStyles.None, out actual));
            Assert.Equal(expectedString, actual.ToString(roundtripFormat));
        }

        [Fact]
        public static void ToString_ParseExactSpan_RoundtripsSuccessfully()
        {
            DateTimeOffset expected = DateTimeOffset.MaxValue;
            string expectedString = expected.ToString("u");

            Assert.Equal(expectedString, DateTimeOffset.ParseExact(expectedString, "u", null, DateTimeStyles.None).ToString("u"));
            Assert.Equal(expectedString, DateTimeOffset.ParseExact(expectedString, new[] { "u" }, null, DateTimeStyles.None).ToString("u"));

            Assert.True(DateTimeOffset.TryParseExact(expectedString, "u", null, DateTimeStyles.None, out DateTimeOffset actual));
            Assert.Equal(expectedString, actual.ToString("u"));
            Assert.True(DateTimeOffset.TryParseExact(expectedString, new[] { "u" }, null, DateTimeStyles.None, out actual));
            Assert.Equal(expectedString, actual.ToString("u"));
        }

        [Theory]
        [InlineData(5)]
        [InlineData(-5)]
        [InlineData(0)]
        [InlineData(14 * 60)]  // max offset
        [InlineData(-14 * 60)] // min offset
        public static void TotalNumberOfMinutesTest(int minutesCount)
        {
            DateTimeOffset dto = new DateTimeOffset(new DateTime(2022, 11, 12), TimeSpan.FromMinutes(minutesCount));
            Assert.Equal(minutesCount, dto.TotalOffsetMinutes);
            Assert.Equal(minutesCount, dto.Offset.TotalMinutes);
        }

        [Fact]
        public static void TotalNumberOfMinutesNowTest()
        {
            DateTimeOffset dto = DateTimeOffset.UtcNow;
            Assert.Equal(0, dto.TotalOffsetMinutes);

            dto = DateTimeOffset.Now;
            Assert.Equal(dto.Offset.TotalMinutes, dto.TotalOffsetMinutes);
        }

        [Fact]
        public static void TryFormat_ToString_EqualResults()
        {
            // UTF16
            {
                DateTimeOffset expected = DateTimeOffset.MaxValue;
                string expectedString = expected.ToString();

                // Just the right amount of space, succeeds
                Span<char> actual = new char[expectedString.Length];
                Assert.True(expected.TryFormat(actual, out int charsWritten));
                Assert.Equal(expectedString.Length, charsWritten);
                Assert.Equal<char>(expectedString.ToCharArray(), actual.ToArray());

                // Too little space, fails
                actual = new char[expectedString.Length - 1];
                Assert.False(expected.TryFormat(actual, out charsWritten));
                Assert.Equal(0, charsWritten);

                // More than enough space, succeeds
                actual = new char[expectedString.Length + 1];
                Assert.True(expected.TryFormat(actual, out charsWritten));
                Assert.Equal(expectedString.Length, charsWritten);
                Assert.Equal<char>(expectedString.ToCharArray(), actual.Slice(0, expectedString.Length).ToArray());
                Assert.Equal(0, actual[actual.Length - 1]);
            }

            // UTF8
            {
                DateTimeOffset expected = DateTimeOffset.MaxValue;
                string expectedString = expected.ToString();

                // Just the right amount of space, succeeds
                Span<byte> actual = new byte[Encoding.UTF8.GetByteCount(expectedString)];
                Assert.True(expected.TryFormat(actual, out int bytesWritten, default, null));
                Assert.Equal(actual.Length, bytesWritten);
                Assert.Equal(expectedString, Encoding.UTF8.GetString(actual));

                // Too little space, fails
                actual = new byte[Encoding.UTF8.GetByteCount(expectedString) - 1];
                Assert.False(expected.TryFormat(actual, out bytesWritten, default, null));
                Assert.Equal(0, bytesWritten);

                // More than enough space, succeeds
                actual = new byte[Encoding.UTF8.GetByteCount(expectedString) + 1];
                Assert.True(expected.TryFormat(actual, out bytesWritten, default, null));
                Assert.Equal(actual.Length - 1, bytesWritten);
                Assert.Equal(expectedString, Encoding.UTF8.GetString(actual.Slice(0, bytesWritten)));
                Assert.Equal(0, actual[actual.Length - 1]);
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotInvariantGlobalization))]
        [MemberData(nameof(ToString_MatchesExpected_MemberData))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/60562", TestPlatforms.Android | TestPlatforms.LinuxBionic)]
        public static void TryFormat_MatchesExpected(DateTimeOffset dateTimeOffset, string format, IFormatProvider provider, string expected)
        {
            // UTF16
            {
                var destination = new char[expected.Length];

                Assert.False(dateTimeOffset.TryFormat(destination.AsSpan(0, destination.Length - 1), out _, format, provider));

                Assert.True(dateTimeOffset.TryFormat(destination, out int charsWritten, format, provider));
                Assert.Equal(destination.Length, charsWritten);
                Assert.Equal(expected, new string(destination));
            }

            // UTF8
            {
                var destination = new byte[Encoding.UTF8.GetByteCount(expected)];

                Assert.False(dateTimeOffset.TryFormat(destination.AsSpan(0, destination.Length - 1), out _, format, provider));

                Assert.True(dateTimeOffset.TryFormat(destination, out int bytesWritten, format, provider));
                Assert.Equal(destination.Length, bytesWritten);
                Assert.Equal(expected, Encoding.UTF8.GetString(destination));
            }
        }

        [Fact]
        public static void UnixEpoch()
        {
            VerifyDateTimeOffset(DateTimeOffset.UnixEpoch, 1970, 1, 1, 0, 0, 0, 0, 0, TimeSpan.Zero);
        }
    }
}
