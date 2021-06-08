// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace System.Tests
{
    public class DateOnlyTests
    {
        [Fact]
        public static void MinMaxValuesTest()
        {
            DateOnly date = DateOnly.MinValue;
            Assert.Equal(0, date.DayNumber);
            Assert.Equal(1, date.Year);
            Assert.Equal(1, date.Month);
            Assert.Equal(1, date.Day);

            date = DateOnly.MaxValue;
            Assert.Equal(3652058, date.DayNumber);
            Assert.Equal(9999, date.Year);
            Assert.Equal(12, date.Month);
            Assert.Equal(31, date.Day);
        }

        public static IEnumerable<object[]> Constructor_TestData()
        {
            yield return new object[] { 1, 1, 1, null };
            yield return new object[] { 9999, 12, 31, null };
            yield return new object[] { 2001, 4, 7, null };
            yield return new object[] { 1, 1, 1, new HijriCalendar() };
            yield return new object[] { 1, 1, 1, new JapaneseCalendar() };
        }

        [Theory]
        [MemberData(nameof(Constructor_TestData))]
        public static void ConstructorsTest(int year, int month, int day, Calendar calendar)
        {
            if (calendar == null)
            {
                DateOnly dateOnly = new DateOnly(year, month, day);
                Assert.Equal(year, dateOnly.Year);
                Assert.Equal(month, dateOnly.Month);
                Assert.Equal(day, dateOnly.Day);
            }
            else
            {
                DateTime dt = calendar.ToDateTime(year, month, day, 0, 0, 0, 0);
                DateOnly dateOnly = new DateOnly(year, month, day, calendar);
                Assert.Equal(dt.Year, dateOnly.Year);
                Assert.Equal(dt.Month, dateOnly.Month);
                Assert.Equal(dt.Day, dateOnly.Day);
            }
        }

        [Fact]
        public static void ConstructorsNegativeCasesTest()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new DateOnly(10000, 1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new DateOnly(-2021, 1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new DateOnly(2020, 13, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new DateOnly(2020, -1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new DateOnly(2020, 0, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new DateOnly(2020, 1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new DateOnly(2020, 1, 32));
            Assert.Throws<ArgumentOutOfRangeException>(() => new DateOnly(2020, 1, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new DateOnly(2003, 2, 29));
        }

        [Fact]
        public static void FromDayNumberTest()
        {
            DateOnly dateOnly = DateOnly.FromDayNumber(DateOnly.MinValue.DayNumber);
            Assert.Equal(1, dateOnly.Year);
            Assert.Equal(1, dateOnly.Month);
            Assert.Equal(1, dateOnly.Day);
            Assert.Equal(DateOnly.MinValue.DayNumber, dateOnly.DayNumber);

            dateOnly = DateOnly.FromDayNumber(DateOnly.MaxValue.DayNumber);
            Assert.Equal(9999, dateOnly.Year);
            Assert.Equal(12, dateOnly.Month);
            Assert.Equal(31, dateOnly.Day);
            Assert.Equal(DateOnly.MaxValue.DayNumber, dateOnly.DayNumber);

            DateTime dt = DateTime.Today;
            int dayNumber = (int) (dt.Ticks / TimeSpan.TicksPerDay);
            dateOnly = DateOnly.FromDayNumber(dayNumber);
            Assert.Equal(dt.Year, dateOnly.Year);
            Assert.Equal(dt.Month, dateOnly.Month);
            Assert.Equal(dt.Day, dateOnly.Day);
            Assert.Equal(dayNumber, dateOnly.DayNumber);

            Assert.Throws<ArgumentOutOfRangeException>(() => DateOnly.FromDayNumber(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => DateOnly.FromDayNumber(DateOnly.MaxValue.DayNumber + 1));
        }

        [Fact]
        public static void DayOfWeekAndDayOfYearTest()
        {
            DateTime dt = DateTime.Today;
            DateOnly dateOnly = DateOnly.FromDayNumber((int) (dt.Ticks / TimeSpan.TicksPerDay));
            Assert.Equal(dt.DayOfWeek, dateOnly.DayOfWeek);
            Assert.Equal(dt.DayOfYear, dateOnly.DayOfYear);
        }


        [Fact]
        public static void AddDaysTest()
        {
            DateOnly dateOnly = DateOnly.MinValue.AddDays(1);
            Assert.Equal(1, dateOnly.DayNumber);
            dateOnly = dateOnly.AddDays(1);
            Assert.Equal(2, dateOnly.DayNumber);
            dateOnly = dateOnly.AddDays(100);
            Assert.Equal(102, dateOnly.DayNumber);

            dateOnly = DateOnly.MaxValue.AddDays(-1);
            Assert.Equal(DateOnly.MaxValue.DayNumber - 1, dateOnly.DayNumber);
            dateOnly = dateOnly.AddDays(-1);
            Assert.Equal(DateOnly.MaxValue.DayNumber - 2, dateOnly.DayNumber);
            dateOnly = dateOnly.AddDays(-100);
            Assert.Equal(DateOnly.MaxValue.DayNumber - 102, dateOnly.DayNumber);

            Assert.Throws<ArgumentOutOfRangeException>(() => DateOnly.MinValue.AddDays(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => DateOnly.MaxValue.AddDays(1));
        }

        [Fact]
        public static void AddMonthsTest()
        {
            DateOnly dateOnly = new DateOnly(2021, 1, 31);
            for (int i = 1; i < 12; i++)
            {
                Assert.Equal(i, dateOnly.Month);
                dateOnly = dateOnly.AddMonths(1);
            }

            for (int i = 12; i > 1; i--)
            {
                Assert.Equal(i, dateOnly.Month);
                dateOnly = dateOnly.AddMonths(-1);
            }

            DateTime dt = DateTime.Today;
            dateOnly = DateOnly.FromDayNumber((int) (dt.Ticks / TimeSpan.TicksPerDay));

            Assert.Equal(dt.Year, dateOnly.Year);
            Assert.Equal(dt.Month, dateOnly.Month);
            Assert.Equal(dt.Day, dateOnly.Day);

            dt = dt.AddMonths(1);
            dateOnly = dateOnly.AddMonths(1);
            Assert.Equal(dt.Month, dateOnly.Month);

            dt = dt.AddMonths(50);
            dateOnly = dateOnly.AddMonths(50);
            Assert.Equal(dt.Month, dateOnly.Month);

            dt = dt.AddMonths(-150);
            dateOnly = dateOnly.AddMonths(-150);
            Assert.Equal(dt.Month, dateOnly.Month);

            Assert.Throws<ArgumentOutOfRangeException>(() => DateOnly.MinValue.AddMonths(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => DateOnly.MaxValue.AddMonths(1));
        }

        [Fact]
        public static void AddYearsTest()
        {
            DateOnly dateOnly = new DateOnly(2021, 1, 31);
            for (int i = 2021; i < 2040; i++)
            {
                Assert.Equal(i, dateOnly.Year);
                dateOnly = dateOnly.AddYears(1);
            }

            for (int i = dateOnly.Year; i > 2020; i--)
            {
                Assert.Equal(i, dateOnly.Year);
                dateOnly = dateOnly.AddYears(-1);
            }

            DateTime dt = DateTime.Today;
            dateOnly = DateOnly.FromDayNumber((int) (dt.Ticks / TimeSpan.TicksPerDay));

            Assert.Equal(dt.Year, dateOnly.Year);
            Assert.Equal(dt.Month, dateOnly.Month);
            Assert.Equal(dt.Day, dateOnly.Day);

            dt = dt.AddYears(1);
            dateOnly = dateOnly.AddYears(1);
            Assert.Equal(dt.Year, dateOnly.Year);

            dt = dt.AddYears(50);
            dateOnly = dateOnly.AddYears(50);
            Assert.Equal(dt.Year, dateOnly.Year);

            dt = dt.AddYears(-150);
            dateOnly = dateOnly.AddYears(-150);
            Assert.Equal(dt.Year, dateOnly.Year);

            Assert.Throws<ArgumentOutOfRangeException>(() => DateOnly.MinValue.AddYears(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => DateOnly.MaxValue.AddYears(1));
        }

        [Fact]
        public static void OperatorsTest()
        {
            Assert.True(DateOnly.MinValue != DateOnly.MaxValue);
            Assert.True(DateOnly.MinValue < DateOnly.MaxValue);
            Assert.True(DateOnly.MinValue <= DateOnly.MaxValue);
            Assert.True(DateOnly.MaxValue > DateOnly.MinValue);
            Assert.True(DateOnly.MaxValue >= DateOnly.MinValue);

            DateOnly dateOnly1 = new DateOnly(2021, 10, 10);
            DateOnly dateOnly2 = new DateOnly(2021, 10, 11);
            DateOnly dateOnly3 = new DateOnly(2021, 10, 10);

            Assert.True(dateOnly1 == dateOnly3);
            Assert.True(dateOnly1 >= dateOnly3);
            Assert.True(dateOnly1 <= dateOnly3);
            Assert.True(dateOnly1 != dateOnly2);
            Assert.True(dateOnly1 < dateOnly2);
            Assert.True(dateOnly1 <= dateOnly2);
            Assert.True(dateOnly2 > dateOnly1);
            Assert.True(dateOnly2 >= dateOnly1);
        }

        [Fact]
        public static void DateTimeConversionTest()
        {
            DateTime dt = DateTime.Today;
            DateOnly dateOnly = DateOnly.FromDateTime(dt);
            Assert.Equal(dt.Year, dateOnly.Year);
            Assert.Equal(dt.Month, dateOnly.Month);
            Assert.Equal(dt.Day, dateOnly.Day);

            dt = dateOnly.ToDateTime(new TimeOnly(1, 10, 20));
            Assert.Equal(dateOnly.Year, dt.Year);
            Assert.Equal(dateOnly.Month, dt.Month);
            Assert.Equal(dateOnly.Day, dt.Day);

            Assert.Equal(1, dt.Hour);
            Assert.Equal(10, dt.Minute);
            Assert.Equal(20, dt.Second);
            Assert.Equal(DateTimeKind.Unspecified, dt.Kind);


            dt = dateOnly.ToDateTime(new TimeOnly(23, 59, 59), DateTimeKind.Utc);
            Assert.Equal(dateOnly.Year, dt.Year);
            Assert.Equal(dateOnly.Month, dt.Month);
            Assert.Equal(dateOnly.Day, dt.Day);

            Assert.Equal(23, dt.Hour);
            Assert.Equal(59, dt.Minute);
            Assert.Equal(59, dt.Second);
            Assert.Equal(DateTimeKind.Utc, dt.Kind);

            dt = dateOnly.ToDateTime(new TimeOnly(23, 59, 59), DateTimeKind.Local);
            Assert.Equal(DateTimeKind.Local, dt.Kind);

            dateOnly = DateOnly.FromDateTime(dt);
            Assert.Equal(dt.Year, dateOnly.Year);
            Assert.Equal(dt.Month, dateOnly.Month);
            Assert.Equal(dt.Day, dateOnly.Day);
        }

        [Fact]
        public static void ComparisonsTest()
        {
            DateOnly dateOnly1 = DateOnly.FromDateTime(DateTime.Today);
            DateOnly dateOnly2 = DateOnly.FromDateTime(DateTime.Today);
            DateOnly dateOnly3 = dateOnly1.AddYears(-10);

            Assert.Equal(0, dateOnly1.CompareTo(dateOnly2));
            Assert.True(dateOnly1.Equals(dateOnly2));
            Assert.True(dateOnly1.Equals((object)dateOnly2));
            Assert.Equal(0, dateOnly2.CompareTo(dateOnly1));
            Assert.True(dateOnly2.Equals(dateOnly1));
            Assert.True(dateOnly2.Equals((object)dateOnly1));
            Assert.Equal(1, dateOnly1.CompareTo(dateOnly3));
            Assert.False(dateOnly1.Equals(dateOnly3));
            Assert.False(dateOnly1.Equals((object)dateOnly3));
            Assert.Equal(-1, dateOnly3.CompareTo(dateOnly1));
            Assert.False(dateOnly3.Equals(dateOnly1));
            Assert.False(dateOnly3.Equals((object)dateOnly1));

            Assert.Equal(0, dateOnly1.CompareTo((object)dateOnly2));
            Assert.Equal(0, dateOnly2.CompareTo((object)dateOnly1));
            Assert.Equal(1, dateOnly1.CompareTo((object)dateOnly3));
            Assert.Equal(-1, dateOnly3.CompareTo((object)dateOnly1));

            Assert.Equal(1, dateOnly1.CompareTo(null));

            Assert.Throws<ArgumentException>(() => dateOnly1.CompareTo(new object()));
            Assert.False(dateOnly3.Equals(new object()));
        }

        [Fact]
        public static void GetHashCodeTest()
        {
            Assert.Equal(DateOnly.MinValue.DayNumber, DateOnly.MinValue.GetHashCode());
            Assert.Equal(DateOnly.MaxValue.DayNumber, DateOnly.MaxValue.GetHashCode());
            DateOnly dateOnly = DateOnly.FromDateTime(DateTime.Today);
            Assert.Equal(dateOnly.DayNumber, dateOnly.GetHashCode());
        }

        // Arabic cultures uses zero width characters in the date formatting which cause a problem with the DateTime parsing in general.
        // We still test these cultures parsing but with ParseExact instead.
        internal static bool IsNotArabicCulture => !CultureInfo.CurrentCulture.Name.StartsWith("ar", StringComparison.OrdinalIgnoreCase);

        [ConditionalFact(nameof(IsNotArabicCulture))]
        public static void BasicFormatParseTest()
        {
            DateOnly dateOnly = DateOnly.FromDateTime(DateTime.Today);
            string s = dateOnly.ToString();
            DateOnly parsedDateOnly = DateOnly.Parse(s);
            Assert.True(DateOnly.TryParse(s, out DateOnly parsedDateOnly1));
            Assert.Equal(dateOnly, parsedDateOnly);
            Assert.Equal(dateOnly, parsedDateOnly1);
            parsedDateOnly = DateOnly.Parse(s.AsSpan());
            Assert.True(DateOnly.TryParse(s.AsSpan(), out parsedDateOnly1));
            Assert.Equal(dateOnly, parsedDateOnly);
            Assert.Equal(dateOnly, parsedDateOnly1);

            s = dateOnly.ToString(CultureInfo.InvariantCulture);
            parsedDateOnly = DateOnly.Parse(s, CultureInfo.InvariantCulture);
            Assert.True(DateOnly.TryParse(s.AsSpan(), CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDateOnly1));
            Assert.Equal(dateOnly, parsedDateOnly);
            Assert.Equal(dateOnly, parsedDateOnly1);
            parsedDateOnly = DateOnly.Parse(s.AsSpan(), CultureInfo.InvariantCulture);
            Assert.True(DateOnly.TryParse(s.AsSpan(), CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDateOnly1));
            Assert.Equal(dateOnly, parsedDateOnly);
            Assert.Equal(dateOnly, parsedDateOnly1);

            Assert.False(DateOnly.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out parsedDateOnly1));
            Assert.Throws<ArgumentException>(() => DateOnly.Parse(s, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal));
            Assert.False(DateOnly.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out parsedDateOnly1));
            Assert.Throws<ArgumentException>(() => DateOnly.Parse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal));
            Assert.False(DateOnly.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out parsedDateOnly1));
            Assert.Throws<ArgumentException>(() => DateOnly.Parse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal));
            Assert.False(DateOnly.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.NoCurrentDateDefault, out parsedDateOnly1));
            Assert.Throws<ArgumentException>(() => DateOnly.Parse(s, CultureInfo.InvariantCulture, DateTimeStyles.NoCurrentDateDefault));

            s = "     " + s + "     ";
            parsedDateOnly = DateOnly.Parse(s, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces);
            Assert.Equal(dateOnly, parsedDateOnly);
            parsedDateOnly = DateOnly.Parse(s.AsSpan(), CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces);
            Assert.Equal(dateOnly, parsedDateOnly);
        }

        [ConditionalFact(nameof(IsNotArabicCulture))]
        public static void FormatParseTest()
        {
            string[] patterns = new string[] { CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern, CultureInfo.CurrentCulture.DateTimeFormat.LongDatePattern, "d", "D", "o", "r" };

            DateOnly dateOnly = DateOnly.FromDateTime(DateTime.Today);

            foreach (string format in patterns)
            {
                string formattedDate = dateOnly.ToString(format);
                DateOnly parsedDateOnly = DateOnly.Parse(formattedDate);
                Assert.True(DateOnly.TryParse(formattedDate, out DateOnly parsedDateOnly1));
                Assert.Equal(dateOnly, parsedDateOnly);
                Assert.Equal(dateOnly, parsedDateOnly1);
                parsedDateOnly = DateOnly.Parse(formattedDate.AsSpan());
                Assert.True(DateOnly.TryParse(formattedDate.AsSpan(), out parsedDateOnly1));
                Assert.Equal(dateOnly, parsedDateOnly);
                Assert.Equal(dateOnly, parsedDateOnly1);

                parsedDateOnly = DateOnly.Parse(formattedDate, CultureInfo.CurrentCulture);
                Assert.True(DateOnly.TryParse(formattedDate, CultureInfo.CurrentCulture, DateTimeStyles.None, out parsedDateOnly1));
                Assert.Equal(dateOnly, parsedDateOnly);
                Assert.Equal(dateOnly, parsedDateOnly1);
                parsedDateOnly = DateOnly.Parse(formattedDate.AsSpan(), CultureInfo.CurrentCulture);
                Assert.True(DateOnly.TryParse(formattedDate.AsSpan(), CultureInfo.CurrentCulture, DateTimeStyles.None, out parsedDateOnly1));
                Assert.Equal(dateOnly, parsedDateOnly);
                Assert.Equal(dateOnly, parsedDateOnly1);

                parsedDateOnly = DateOnly.ParseExact(formattedDate, format);
                Assert.True(DateOnly.TryParseExact(formattedDate, format, out parsedDateOnly1));
                Assert.Equal(dateOnly, parsedDateOnly);
                Assert.Equal(dateOnly, parsedDateOnly1);
                parsedDateOnly = DateOnly.ParseExact(formattedDate.AsSpan(), format.AsSpan());
                Assert.True(DateOnly.TryParseExact(formattedDate.AsSpan(), format.AsSpan(), out parsedDateOnly1));
                Assert.Equal(dateOnly, parsedDateOnly);
                Assert.Equal(dateOnly, parsedDateOnly1);

                parsedDateOnly = DateOnly.ParseExact(formattedDate, format, CultureInfo.CurrentCulture);
                Assert.True(DateOnly.TryParseExact(formattedDate, format, CultureInfo.CurrentCulture, DateTimeStyles.None, out parsedDateOnly1));
                Assert.Equal(dateOnly, parsedDateOnly);
                Assert.Equal(dateOnly, parsedDateOnly1);
                parsedDateOnly = DateOnly.ParseExact(formattedDate.AsSpan(), format.AsSpan(), CultureInfo.CurrentCulture);
                Assert.True(DateOnly.TryParseExact(formattedDate.AsSpan(), format.AsSpan(), CultureInfo.CurrentCulture, DateTimeStyles.None, out parsedDateOnly1));
                Assert.Equal(dateOnly, parsedDateOnly);
                Assert.Equal(dateOnly, parsedDateOnly1);

                parsedDateOnly = DateOnly.ParseExact(formattedDate, patterns);
                Assert.True(DateOnly.TryParseExact(formattedDate, patterns, out parsedDateOnly1));
                Assert.Equal(dateOnly, parsedDateOnly);
                Assert.Equal(dateOnly, parsedDateOnly1);
                parsedDateOnly = DateOnly.ParseExact(formattedDate.AsSpan(), patterns);
                Assert.True(DateOnly.TryParseExact(formattedDate.AsSpan(), patterns, out parsedDateOnly1));
                Assert.Equal(dateOnly, parsedDateOnly);
                Assert.Equal(dateOnly, parsedDateOnly1);

                parsedDateOnly = DateOnly.ParseExact(formattedDate, patterns, CultureInfo.CurrentCulture);
                Assert.True(DateOnly.TryParseExact(formattedDate, patterns, CultureInfo.CurrentCulture, DateTimeStyles.None, out parsedDateOnly1));
                Assert.Equal(dateOnly, parsedDateOnly);
                Assert.Equal(dateOnly, parsedDateOnly1);
                parsedDateOnly = DateOnly.ParseExact(formattedDate.AsSpan(), patterns, CultureInfo.CurrentCulture);
                Assert.True(DateOnly.TryParseExact(formattedDate.AsSpan(), patterns, CultureInfo.CurrentCulture, DateTimeStyles.None, out parsedDateOnly1));
                Assert.Equal(dateOnly, parsedDateOnly);
                Assert.Equal(dateOnly, parsedDateOnly1);
            }
        }

        [Fact]
        public static void OAndRFormatsTest()
        {
            DateOnly dateOnly = DateOnly.FromDateTime(DateTime.Today);
            string formattedDate = dateOnly.ToString("o");
            Assert.Equal(10, formattedDate.Length);
            Assert.Equal('-', formattedDate[4]);
            Assert.Equal('-', formattedDate[7]);
            DateOnly parsedDateOnly = DateOnly.Parse(formattedDate);
            Assert.Equal(dateOnly, parsedDateOnly);
            parsedDateOnly = DateOnly.Parse(formattedDate.AsSpan());
            Assert.Equal(dateOnly, parsedDateOnly);
            parsedDateOnly = DateOnly.ParseExact(formattedDate, "O");
            Assert.Equal(dateOnly, parsedDateOnly);
            parsedDateOnly = DateOnly.ParseExact(formattedDate.AsSpan(), "O".AsSpan());
            Assert.Equal(dateOnly, parsedDateOnly);

            formattedDate = dateOnly.ToString("r");
            Assert.Equal(16, formattedDate.Length);
            Assert.Equal(',', formattedDate[3]);
            Assert.Equal(' ', formattedDate[4]);
            Assert.Equal(' ', formattedDate[7]);
            Assert.Equal(' ', formattedDate[11]);
            parsedDateOnly = DateOnly.Parse(formattedDate);
            Assert.Equal(dateOnly, parsedDateOnly);
            parsedDateOnly = DateOnly.Parse(formattedDate.AsSpan());
            Assert.Equal(dateOnly, parsedDateOnly);
            parsedDateOnly = DateOnly.ParseExact(formattedDate, "R");
            Assert.Equal(dateOnly, parsedDateOnly);
            parsedDateOnly = DateOnly.ParseExact(formattedDate.AsSpan(), "R".AsSpan());
            Assert.Equal(dateOnly, parsedDateOnly);
        }

        [Fact]
        public static void InvalidFormatsTest()
        {
            DateTime dt = DateTime.Now;
            string formatted = dt.ToString();
            Assert.Throws<FormatException>(() => DateOnly.Parse(formatted));
            Assert.Throws<FormatException>(() => DateOnly.Parse(formatted.AsSpan()));
            Assert.False(DateOnly.TryParse(formatted, out DateOnly dateOnly));
            Assert.False(DateOnly.TryParse(formatted.AsSpan(), out dateOnly));
            formatted = dt.ToString("t");
            Assert.Throws<FormatException>(() => DateOnly.Parse(formatted));
            Assert.Throws<FormatException>(() => DateOnly.Parse(formatted.AsSpan()));
            Assert.False(DateOnly.TryParse(formatted, out dateOnly));
            Assert.False(DateOnly.TryParse(formatted.AsSpan(), out dateOnly));
        }

        [Fact]
        public static void CustomFormattingTest()
        {
            DateOnly dateOnly = DateOnly.FromDateTime(DateTime.Today);
            string format = "dd, ddd 'dash' MMMM \"dash\" yyyy";
            string formatted = dateOnly.ToString(format);
            DateOnly parsedDateOnly = DateOnly.ParseExact(formatted, format);
            Assert.Equal(dateOnly, parsedDateOnly);
            parsedDateOnly = DateOnly.ParseExact(formatted.AsSpan(), format.AsSpan());
            Assert.Equal(dateOnly, parsedDateOnly);

            Assert.Throws<FormatException>(() => dateOnly.ToString("dd-MM-yyyy hh"));
            Assert.Throws<FormatException>(() => dateOnly.ToString("dd-MM-yyyy m"));
            Assert.Throws<FormatException>(() => dateOnly.ToString("dd-MM-yyyy s"));
            Assert.Throws<FormatException>(() => dateOnly.ToString("dd-MM-yyyy z"));
        }

        [Fact]
        public static void AllCulturesTest()
        {
            DateOnly dateOnly = DateOnly.FromDateTime(DateTime.Today);
            foreach (CultureInfo ci in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
            {
                string formatted = dateOnly.ToString("d", ci);
                DateOnly parsedDateOnly = DateOnly.ParseExact(formatted, "d", ci);
                Assert.Equal(dateOnly, parsedDateOnly);

                formatted = dateOnly.ToString("D", ci);
                parsedDateOnly = DateOnly.ParseExact(formatted, "D", ci);
                Assert.Equal(dateOnly, parsedDateOnly);
            }
        }

        [Fact]
        public static void TryFormatTest()
        {
            Span<char> buffer = stackalloc char[100];
            DateOnly dateOnly = DateOnly.FromDateTime(DateTime.Today);

            Assert.True(dateOnly.TryFormat(buffer, out int charsWritten));
            Assert.True(dateOnly.TryFormat(buffer, out charsWritten, "o"));
            Assert.Equal(10, charsWritten);
            Assert.True(dateOnly.TryFormat(buffer, out charsWritten, "R"));
            Assert.Equal(16, charsWritten);
            Assert.False(dateOnly.TryFormat(buffer.Slice(0, 3), out charsWritten));
            Assert.False(dateOnly.TryFormat(buffer.Slice(0, 3), out charsWritten, "r"));
            Assert.False(dateOnly.TryFormat(buffer.Slice(0, 3), out charsWritten, "O"));
        }
    }
}
