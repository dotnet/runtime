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
    }
}
