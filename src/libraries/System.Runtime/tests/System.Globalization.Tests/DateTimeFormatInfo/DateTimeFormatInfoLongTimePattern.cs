// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoLongTimePattern
    {
        [Fact]
        public void LongTimePattern_GetInvariantInfo_ReturnsExpected()
        {
            Assert.Equal("HH:mm:ss", DateTimeFormatInfo.InvariantInfo.LongTimePattern);
        }

        public static IEnumerable<object[]> LongTimePattern_Set_TestData()
        {
            yield return new object[] { string.Empty };
            yield return new object[] { "garbage" };
            yield return new object[] { "dddd, dd MMMM yyyy HH:mm:ss" };
            yield return new object[] { "HH" };
            yield return new object[] { "T" };
            yield return new object[] { "HH:mm:ss dddd, dd MMMM yyyy" };
            yield return new object[] { "HH:mm:ss" };
        }

        [Theory]
        [MemberData(nameof(LongTimePattern_Set_TestData))]
        public void LongTimePattern_Set_GetReturnsExpected(string value)
        {
            var format = new DateTimeFormatInfo();
            format.LongTimePattern = value;
            Assert.Equal(value, format.LongTimePattern);
        }

        [Fact]
        public void LongTimePattern_Set_InvalidatesDerivedPatterns()
        {
            const string Pattern = "#$";
            var format = new DateTimeFormatInfo();
            var d = DateTimeOffset.Now;
            d.ToString("F", format); // FullDateTimePattern
            d.ToString("G", format); // GeneralLongTimePattern
            d.ToString(format); // DateTimeOffsetPattern
            format.LongTimePattern = Pattern;
            Assert.Contains(Pattern, d.ToString("F", format));
            Assert.Contains(Pattern, d.ToString("G", format));
            Assert.Contains(Pattern, d.ToString(format));
        }

        [Fact]
        public void LongTimePattern_SetNullValue_ThrowsArgumentNullException()
        {
            var format = new DateTimeFormatInfo();
            AssertExtensions.Throws<ArgumentNullException>("value", () => format.LongTimePattern = null);
        }

        [Fact]
        public void LongTimePattern_SetReadOnly_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => DateTimeFormatInfo.InvariantInfo.LongTimePattern = "HH:mm:ss");
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsIcuGlobalization))]
        public void LongTimePattern_CheckReadingTimeFormatWithSingleQuotes_ICU()
        {
            // Usually fr-CA long time format has a single quotes e.g. "HH 'h' mm 'min' ss 's'".
            // Ensuring when reading such formats from ICU we'll not eat the spaces after the single quotes.
            string longTimeFormat = CultureInfo.GetCultureInfo("fr-CA").DateTimeFormat.LongTimePattern;
            int startIndex = 0;

            while ((startIndex = longTimeFormat.IndexOf('\'', startIndex)) >= 0 && startIndex < longTimeFormat.Length - 1)
            {
                // We have the opening single quote, find the closing one.
                startIndex++;
                if ((startIndex = longTimeFormat.IndexOf('\'', startIndex)) > 0 && startIndex < longTimeFormat.Length - 1)
                {
                    Assert.Equal(' ', longTimeFormat[++startIndex]);
                }
                else
                {
                    break; // done.
                }
            }
        }

        [Fact]
        public void LongTimePattern_VerifyTimePatterns()
        {
            Assert.All(CultureInfo.GetCultures(CultureTypes.AllCultures), culture => {
                if (DateTimeFormatInfoData.HasBadIcuTimePatterns(culture))
                {
                    return;
                }
                var pattern = culture.DateTimeFormat.LongTimePattern;
                bool use24Hour = false;
                bool use12Hour = false;
                bool useAMPM = false;
                for (var i = 0; i < pattern.Length; i++)
                {
                    switch (pattern[i])
                    {
                        case 'H': use24Hour = true; break;
                        case 'h': use12Hour = true; break;
                        case 't': useAMPM = true; break;
                        case '\\': i++; break;
                        case '\'':
                            i++;
                            for (; i < pattern.Length; i++)
                            {
                                var c = pattern[i];
                                if (c == '\'') break;
                                if (c == '\\') i++;
                            }
                            break;
                    }
                }
                Assert.True((use24Hour || useAMPM) && (use12Hour ^ use24Hour), $"Bad long time pattern for culture {culture.Name}: '{pattern}'");
            });
        }

        [Fact]
        public void LongTimePattern_CheckTimeFormatWithSpaces()
        {
            var date = DateTime.Today + TimeSpan.FromHours(15) + TimeSpan.FromMinutes(15);
            var culture = new CultureInfo("en-US");
            string formattedDate = date.ToString("t", culture);
            bool containsSpace = formattedDate.Contains(' ');
            bool containsNoBreakSpace = formattedDate.Contains('\u00A0');
            bool containsNarrowNoBreakSpace = formattedDate.Contains('\u202F');

            Assert.True(containsSpace || containsNoBreakSpace || containsNarrowNoBreakSpace,
                $"Formatted date string '{formattedDate}' does not contain any of the specified spaces.");
        }
    }
}
