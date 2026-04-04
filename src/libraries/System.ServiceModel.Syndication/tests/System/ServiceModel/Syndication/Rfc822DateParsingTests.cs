// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Xml;
using Xunit;
using System.Globalization;

namespace System.ServiceModel.Syndication.Tests
{
    public class Rfc822DateParsingTests
    {
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]   // NetFx requires double digit dates and four digit years
        [Theory]
        [InlineData("Mon, 2 Jun 2003 09:39:21 GMT", 2003, 6, 2, 9, 39, 21)]
        [InlineData("2 Jun 2003 09:39:21 GMT", 2003, 6, 2, 9, 39, 21)]
        [InlineData("02 Jun 03 09:39:21 GMT", 2003, 6, 2, 9, 39, 21)]
        [InlineData("Mon, 2 Jun 03 09:39:21 GMT", 2003, 6, 2, 9, 39, 21)]
        [InlineData("2 Jun 03 09:39 GMT", 2003, 6, 2, 9, 39, 0)]
        [InlineData("02 Jun 03 09:39 GMT", 2003, 6, 2, 9, 39, 0)]
        [InlineData("Mon, 2 Jun 03 09:39 GMT", 2003, 6, 2, 9, 39, 0)]
        [InlineData("Mon, 02 Jun 03 09:39 GMT", 2003, 6, 2, 9, 39, 0)]
        public void Rss20ItemFormatter_Read_SingleDigitDay_And_TwoDigitYear(string pubDate, int year, int month, int day, int hour, int minute, int second)
        {
            string xml = $"<item><pubDate>{pubDate}</pubDate></item>";
            using var stringReader = new StringReader(xml);
            using var reader = XmlReader.Create(stringReader);
            var formatter = new Rss20ItemFormatter();
            formatter.ReadFrom(reader);
            DateTimeOffset expected = new DateTimeOffset(year, month, day, hour, minute, second, TimeSpan.Zero);
            Assert.Equal(expected, formatter.Item.PublishDate);
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]   // NetFx requires double digit dates and four digit years
        [Theory]
        [InlineData("Mon, 02 Jun 03 09:39:21 +0000", 2003, 6, 2, 9, 39, 21)]
        [InlineData("Mon, 02 Jun 03 09:39:21 -0000", 2003, 6, 2, 9, 39, 21)]
        [InlineData("Mon, 02 Jun 03 09:39:21 UT", 2003, 6, 2, 9, 39, 21)]
        public void Rss20ItemFormatter_Read_SingleDigitDay_NormalizedTimeZones(string pubDate, int year, int month, int day, int hour, int minute, int second)
        {
            string xml = $"<item><pubDate>{pubDate}</pubDate></item>";
            using var stringReader = new StringReader(xml);
            using var reader = XmlReader.Create(stringReader);
            var formatter = new Rss20ItemFormatter();
            formatter.ReadFrom(reader);
            DateTimeOffset expectedUtc = new DateTimeOffset(year, month, day, hour, minute, second, TimeSpan.Zero);
            Assert.Equal(expectedUtc, formatter.Item.PublishDate);
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]   // NetFx requires double digit dates and four digit years
        [Theory]
        [InlineData("2 Jun {year} 09:39 GMT", 6, 2, 9, 39, 0)]
        [InlineData("02 Jun {year} 09:39:21 +0000", 6, 2, 9, 39, 21)]
        [InlineData("02 Jun {year} 09:39:21 -0000", 6, 2, 9, 39, 21)]
        [InlineData("02 Jun {year} 09:39:21 UT", 6, 2, 9, 39, 21)]
        public void Rss20ItemFormatter_Read_SingleDigitDay_And_TwoDigitYear_Max(string pubDate, int month, int day, int hour, int minute, int second)
        {
            // As of .Net 8.0, CultureInfo.CurrentCulture.DateTimeFormat.Calendar.TwoDigitYearMax is 2049 for invariant and en-US cultures
            var maxDate = CultureInfo.CurrentCulture.DateTimeFormat.Calendar.TwoDigitYearMax % 100;

            // Test under/at the 2-digit year max threshold
            var underDate = pubDate.Replace("{year}", maxDate.ToString());
            string xml = $"<item><pubDate>{underDate}</pubDate></item>";
            using var stringReader = new StringReader(xml);
            using var reader = XmlReader.Create(stringReader);
            var formatter = new Rss20ItemFormatter();
            formatter.ReadFrom(reader);
            DateTimeOffset expectedUnder = new DateTimeOffset(2000 + maxDate, month, day, hour, minute, second, TimeSpan.Zero);
            Assert.Equal(expectedUnder, formatter.Item.PublishDate);

            // Test over the 2-digit year max threshold
            var overDate = pubDate.Replace("{year}", (maxDate + 1).ToString());
            xml = $"<item><pubDate>{overDate}</pubDate></item>";
            using var stringReaderOver = new StringReader(xml);
            using var readerOver = XmlReader.Create(stringReaderOver);
            formatter = new Rss20ItemFormatter();
            formatter.ReadFrom(readerOver);
            DateTimeOffset expectedOver = new DateTimeOffset(1900 + maxDate + 1, month, day, hour, minute, second, TimeSpan.Zero);
            Assert.Equal(expectedOver, formatter.Item.PublishDate);
        }
    }
}
