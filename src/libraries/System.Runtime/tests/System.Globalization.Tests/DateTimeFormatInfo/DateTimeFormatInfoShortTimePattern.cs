// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoShortTimePattern
    {
        [Fact]
        public void ShortTimePattern_GetInvariantInfo_ReturnsExpected()
        {
            Assert.Equal("HH:mm", DateTimeFormatInfo.InvariantInfo.ShortTimePattern);
        }

        public static IEnumerable<object[]> ShortTimePattern_Set_TestData()
        {
            yield return new object[] { string.Empty };
            yield return new object[] { "garbage" };
            yield return new object[] { "dddd, dd MMMM yyyy HH:mm:ss" };
            yield return new object[] { "HH:mm" };
            yield return new object[] { "t" };
        }

        [Theory]
        [MemberData(nameof(ShortTimePattern_Set_TestData))]
        public void ShortTimePattern_Set_GetReturnsExpected(string value)
        {
            var format = new DateTimeFormatInfo();
            format.ShortTimePattern = value;
            Assert.Equal(value, format.ShortTimePattern);
        }

        [Fact]
        public void ShortTimePattern_Set_InvalidatesDerivedPattern()
        {
            const string Pattern = "#$";
            var format = new DateTimeFormatInfo();
            var d = DateTimeOffset.Now;
            d.ToString("g", format); // GeneralShortTimePattern
            format.ShortTimePattern = Pattern;
            Assert.Contains(Pattern, d.ToString("g", format));
        }

        [Fact]
        public void ShortTimePattern_SetNull_ThrowsArgumentNullException()
        {
            var format = new DateTimeFormatInfo();
            AssertExtensions.Throws<ArgumentNullException>("value", () => format.ShortTimePattern = null);
        }

        [Fact]
        public void ShortTimePattern_SetReadOnly_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => DateTimeFormatInfo.InvariantInfo.ShortTimePattern = "HH:mm");
        }

        [Fact]
        public void ShortTimePattern_VerifyTimePatterns()
        {
            Assert.All(CultureInfo.GetCultures(CultureTypes.AllCultures), culture => {
                if (DateTimeFormatInfoData.HasBadIcuTimePatterns(culture))
                {
                    return;
                }
                var pattern = culture.DateTimeFormat.ShortTimePattern;
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
                Assert.True((use24Hour || useAMPM) && (use12Hour ^ use24Hour), $"Bad short time pattern for culture {culture.Name}: '{pattern}'");
            });
        }

        [Fact]
        public void ShortTimePattern_CheckTimeFormatWithSpaces()
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
