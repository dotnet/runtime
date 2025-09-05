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
        // As of .Net 8.0, CultureInfo.CurrentCulture.DateTimeFormat.Calendar.TwoDigitYearMax is 2049 for invariant and en-US cultures
        [InlineData("2 Jun 50 09:39 GMT", 1950, 6, 2, 9, 39, 0)]
        [InlineData("2 Jun 49 09:39 GMT", 2049, 6, 2, 9, 39, 0)]
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
        [InlineData("Mon, 2 Jun 2003 09:39:21 +0000", 2003, 6, 2, 9, 39, 21)]
        [InlineData("Mon, 2 Jun 2003 09:39:21 -0000", 2003, 6, 2, 9, 39, 21)]
        [InlineData("Mon, 2 Jun 2003 09:39:21 UT", 2003, 6, 2, 9, 39, 21)]
        [InlineData("Mon, 02 Jun 03 09:39:21 +0000", 2003, 6, 2, 9, 39, 21)]
        [InlineData("Mon, 02 Jun 03 09:39:21 -0000", 2003, 6, 2, 9, 39, 21)]
        [InlineData("Mon, 02 Jun 03 09:39:21 UT", 2003, 6, 2, 9, 39, 21)]
        // As of .Net 8.0, CultureInfo.CurrentCulture.DateTimeFormat.Calendar.TwoDigitYearMax is 2049 for invariant and en-US cultures
        [InlineData("02 Jun 50 09:39:21 +0000", 1950, 6, 2, 9, 39, 21)]
        [InlineData("02 Jun 50 09:39:21 -0000", 1950, 6, 2, 9, 39, 21)]
        [InlineData("02 Jun 49 09:39:21 UT", 2049, 6, 2, 9, 39, 21)]
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
    }
}
