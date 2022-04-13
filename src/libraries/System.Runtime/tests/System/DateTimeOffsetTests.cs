// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
            //                     long seed1 = BitConverter.ToInt64(bytes, 0);
            //                     short seed2 = BitConverter.ToInt16(bytes, 0);
            //                     var dto = new DateTimeOffset(seed1, TimeSpan.FromSeconds(seed2));
            //                     Console.WriteLine($"yield return new object[] {{ new DateTimeOffset({seed1}, TimeSpan.FromSeconds({seed2})), \"{format}\", null, \"{dto.ToString(format)}\" }};");
            //                     i++;
            //                 }
            //                 catch { }
            //             }
            //         }
            //     }
            yield return new object[] { new DateTimeOffset(2900400428644841236, TimeSpan.FromSeconds(14100)), "R", null, "Thu, 02 Jan 9192 18:39:24 GMT" };
            yield return new object[] { new DateTimeOffset(1274262903994885572, TimeSpan.FromSeconds(-23100)), "R", null, "Fri, 24 Dec 4038 14:11:39 GMT" };
            yield return new object[] { new DateTimeOffset(1228646631256163984, TimeSpan.FromSeconds(1680)), "R", null, "Tue, 05 Jun 3894 16:37:25 GMT" };
            yield return new object[] { new DateTimeOffset(2075652349868341848, TimeSpan.FromSeconds(31320)), "O", null, "6578-06-25T09:43:06.8341848+08:42" };
            yield return new object[] { new DateTimeOffset(2552829549675618692, TimeSpan.FromSeconds(8580)), "O", null, "8090-08-05T19:56:07.5618692+02:23" };
            yield return new object[] { new DateTimeOffset(2125715934081389968, TimeSpan.FromSeconds(5520)), "o", null, "6737-02-16T08:50:08.1389968+01:32" };
            yield return new object[] { new DateTimeOffset(98868902986124576, TimeSpan.FromSeconds(3360)), "O", null, "0314-04-22T14:24:58.6124576+00:56" };
            yield return new object[] { new DateTimeOffset(2013251697288306712, TimeSpan.FromSeconds(24600)), "O", null, "6380-09-28T10:15:28.8306712+06:50" };
            yield return new object[] { new DateTimeOffset(1037147523427228640, TimeSpan.FromSeconds(23520)), "o", null, "3287-08-04T05:25:42.7228640+06:32" };
            yield return new object[] { new DateTimeOffset(2177752403904984188, TimeSpan.FromSeconds(29820)), "r", null, "Mon, 09 Jan 6902 10:02:50 GMT" };
            yield return new object[] { new DateTimeOffset(832166470435218996, TimeSpan.FromSeconds(9780)), "r", null, "Fri, 12 Jan 2638 12:34:23 GMT" };
            yield return new object[] { new DateTimeOffset(651609783134768240, TimeSpan.FromSeconds(-13200)), "o", null, "2065-11-13T23:45:13.4768240-03:40" };
            yield return new object[] { new DateTimeOffset(1436056534611459616, TimeSpan.FromSeconds(-480)), "o", null, "4551-09-07T11:17:41.1459616-00:08" };
            yield return new object[] { new DateTimeOffset(1028124353647668124, TimeSpan.FromSeconds(-5220)), "o", null, "3258-12-30T17:49:24.7668124-01:27" };
            yield return new object[] { new DateTimeOffset(815446183072290676, TimeSpan.FromSeconds(-10380)), "o", null, "2585-01-17T10:51:47.2290676-02:53" };
            yield return new object[] { new DateTimeOffset(2091185090181553120, TimeSpan.FromSeconds(23520)), "r", null, "Fri, 14 Sep 6627 20:11:38 GMT" };
            yield return new object[] { new DateTimeOffset(2668855365894778960, TimeSpan.FromSeconds(-20400)), "R", null, "Mon, 08 Apr 8458 04:56:29 GMT" };
            yield return new object[] { new DateTimeOffset(1640160533452759488, TimeSpan.FromSeconds(8640)), "O", null, "5198-06-18T22:49:05.2759488+02:24" };
            yield return new object[] { new DateTimeOffset(2958665748788957224, TimeSpan.FromSeconds(20520)), "o", null, "9376-08-21T15:41:18.8957224+05:42" };
            yield return new object[] { new DateTimeOffset(2902544657562766092, TimeSpan.FromSeconds(-29940)), "R", null, "Tue, 20 Oct 9198 00:48:16 GMT" };
            yield return new object[] { new DateTimeOffset(2847595389168931696, TimeSpan.FromSeconds(21360)), "R", null, "Thu, 02 Sep 9024 17:59:16 GMT" };
            yield return new object[] { new DateTimeOffset(2010196475667096780, TimeSpan.FromSeconds(9420)), "r", null, "Sat, 23 Jan 6371 04:22:26 GMT" };
            yield return new object[] { new DateTimeOffset(613442997756722832, TimeSpan.FromSeconds(32400)), "O", null, "1944-12-04T11:16:15.6722832+09:00" };
            yield return new object[] { new DateTimeOffset(921560296274801912, TimeSpan.FromSeconds(13560)), "r", null, "Wed, 23 Apr 2921 13:21:07 GMT" };
            yield return new object[] { new DateTimeOffset(1990689515682669052, TimeSpan.FromSeconds(8700)), "o", null, "6309-03-31T18:59:28.2669052+02:25" };
            yield return new object[] { new DateTimeOffset(620638066929852080, TimeSpan.FromSeconds(24240)), "O", null, "1967-09-23T02:18:12.9852080+06:44" };
            yield return new object[] { new DateTimeOffset(327248350932775524, TimeSpan.FromSeconds(12900)), "O", null, "1038-01-04T15:58:13.2775524+03:35" };
            yield return new object[] { new DateTimeOffset(1370257845275318012, TimeSpan.FromSeconds(-10500)), "O", null, "4343-03-06T13:55:27.5318012-02:55" };
            yield return new object[] { new DateTimeOffset(1239382730779209800, TimeSpan.FromSeconds(12360)), "O", null, "3928-06-13T18:04:37.9209800+03:26" };
            yield return new object[] { new DateTimeOffset(2935667013803687040, TimeSpan.FromSeconds(-17280)), "o", null, "9303-10-05T17:56:20.3687040-04:48" };
            yield return new object[] { new DateTimeOffset(2101626275971711700, TimeSpan.FromSeconds(-15660)), "R", null, "Tue, 16 Oct 6660 00:00:57 GMT" };
            yield return new object[] { new DateTimeOffset(1417918072364232412, TimeSpan.FromSeconds(28380)), "o", null, "4494-03-15T21:07:16.4232412+07:53" };
            yield return new object[] { new DateTimeOffset(962535844977970944, TimeSpan.FromSeconds(3840)), "r", null, "Thu, 27 Feb 3051 01:44:17 GMT" };
            yield return new object[] { new DateTimeOffset(2576630638913059544, TimeSpan.FromSeconds(-1320)), "R", null, "Tue, 07 Jan 8166 09:40:11 GMT" };
            yield return new object[] { new DateTimeOffset(991481917233718112, TimeSpan.FromSeconds(25440)), "r", null, "Thu, 19 Nov 3142 05:18:03 GMT" };
            yield return new object[] { new DateTimeOffset(230115425073485984, TimeSpan.FromSeconds(-10080)), "o", null, "0730-03-18T07:08:27.3485984-02:48" };
            yield return new object[] { new DateTimeOffset(1289946780226617584, TimeSpan.FromSeconds(240)), "r", null, "Sat, 04 Sep 4088 22:56:22 GMT" };
            yield return new object[] { new DateTimeOffset(3119563990129685280, TimeSpan.FromSeconds(-19680)), "R", null, "Sun, 04 Jul 9886 16:44:52 GMT" };
            yield return new object[] { new DateTimeOffset(1167612095351481672, TimeSpan.FromSeconds(-6840)), "r", null, "Thu, 06 Jan 3701 23:12:55 GMT" };
            yield return new object[] { new DateTimeOffset(1617181518122280616, TimeSpan.FromSeconds(26280)), "O", null, "5125-08-24T20:50:12.2280616+07:18" };

            // Year patterns
            var enUS = new CultureInfo("en-US");
            var thTH = new CultureInfo("th-TH");
            yield return new object[] { new DateTimeOffset(new DateTime(1234, 5, 6)), "yy", enUS, "34" };
            if (PlatformDetection.IsNotInvariantGlobalization)
                yield return new object[] { DateTimeOffset.MaxValue, "yy", thTH, "42" };

            for (int i = 3; i < 20; i++)
            {
                yield return new object[] { new DateTimeOffset(new DateTime(1234, 5, 6)), new string('y', i), enUS, 1234.ToString("D" + i) };

                if (PlatformDetection.IsNotInvariantGlobalization)
                    yield return new object[] { DateTimeOffset.MaxValue, new string('y', i), thTH, 10542.ToString("D" + i) };
            }
        }

        [Theory]
        [MemberData(nameof(ToString_MatchesExpected_MemberData))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/60562", TestPlatforms.Android)]
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

        [Fact]
        public static void TryFormat_ToString_EqualResults()
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

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotInvariantGlobalization))]
        [MemberData(nameof(ToString_MatchesExpected_MemberData))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/60562", TestPlatforms.Android)]
        public static void TryFormat_MatchesExpected(DateTimeOffset dateTimeOffset, string format, IFormatProvider provider, string expected)
        {
            var destination = new char[expected.Length];

            Assert.False(dateTimeOffset.TryFormat(destination.AsSpan(0, destination.Length - 1), out _, format, provider));

            Assert.True(dateTimeOffset.TryFormat(destination, out int charsWritten, format, provider));
            Assert.Equal(destination.Length, charsWritten);
            Assert.Equal(expected, new string(destination));
        }

        [Fact]
        public static void UnixEpoch()
        {
            VerifyDateTimeOffset(DateTimeOffset.UnixEpoch, 1970, 1, 1, 0, 0, 0, 0, 0, TimeSpan.Zero);
        }
    }
}
