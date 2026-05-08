// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Xml.Schema;
using Xunit;

namespace System.Xml.Tests
{
    public class XmlConvertEndOfDayTests
    {
        [Theory]
        [InlineData("2007-04-05T24:00:00", XmlDateTimeSerializationMode.RoundtripKind, 2007, 4, 6, 0, 0, 0, DateTimeKind.Unspecified)]
        [InlineData("2000-01-01T24:00:00", XmlDateTimeSerializationMode.RoundtripKind, 2000, 1, 2, 0, 0, 0, DateTimeKind.Unspecified)]
        [InlineData("2000-12-31T24:00:00", XmlDateTimeSerializationMode.RoundtripKind, 2001, 1, 1, 0, 0, 0, DateTimeKind.Unspecified)]
        [InlineData("2000-02-28T24:00:00", XmlDateTimeSerializationMode.RoundtripKind, 2000, 2, 29, 0, 0, 0, DateTimeKind.Unspecified)] // leap year
        [InlineData("2001-02-28T24:00:00", XmlDateTimeSerializationMode.RoundtripKind, 2001, 3, 1, 0, 0, 0, DateTimeKind.Unspecified)] // non-leap year
        [InlineData("2007-04-05T24:00:00Z", XmlDateTimeSerializationMode.RoundtripKind, 2007, 4, 6, 0, 0, 0, DateTimeKind.Utc)]
        [InlineData("2007-04-05T24:00:00", XmlDateTimeSerializationMode.Utc, 2007, 4, 6, 0, 0, 0, DateTimeKind.Utc)]
        [InlineData("2007-04-05T24:00:00", XmlDateTimeSerializationMode.Unspecified, 2007, 4, 6, 0, 0, 0, DateTimeKind.Unspecified)]
        [InlineData("0001-01-01T24:00:00", XmlDateTimeSerializationMode.RoundtripKind, 1, 1, 2, 0, 0, 0, DateTimeKind.Unspecified)] // earliest possible date
        public static void ToDateTime_EndOfDay_Valid(string input, XmlDateTimeSerializationMode mode, int year, int month, int day, int hour, int minute, int second, DateTimeKind expectedKind)
        {
            DateTime result = XmlConvert.ToDateTime(input, mode);
            Assert.Equal(new DateTime(year, month, day, hour, minute, second, expectedKind), result);
        }

        [Fact]
        public static void ToDateTime_EndOfDay_Local_Valid()
        {
            DateTime result = XmlConvert.ToDateTime("2007-04-05T24:00:00", XmlDateTimeSerializationMode.Local);
            Assert.Equal(DateTimeKind.Local, result.Kind);
            Assert.Equal(0, result.Hour);
            Assert.Equal(0, result.Minute);
            Assert.Equal(0, result.Second);
        }

        [Theory]
        [InlineData("2007-04-05T24:00:00.0000000")]
        [InlineData("2007-04-05T24:00:00.0")]
        public static void ToDateTime_EndOfDay_ZeroFraction_Valid(string input)
        {
            DateTime result = XmlConvert.ToDateTime(input, XmlDateTimeSerializationMode.RoundtripKind);
            Assert.Equal(new DateTime(2007, 4, 6, 0, 0, 0, DateTimeKind.Unspecified), result);
        }

        [Theory]
        [InlineData("2007-04-05T24:01:00")]
        [InlineData("2007-04-05T24:00:01")]
        [InlineData("2007-04-05T24:00:00.0000001")]
        [InlineData("2007-04-05T24:59:59")]
        [InlineData("9999-12-31T24:00:00")]
        public static void ToDateTime_EndOfDay_Invalid(string input)
        {
            Assert.Throws<FormatException>(() => XmlConvert.ToDateTime(input, XmlDateTimeSerializationMode.RoundtripKind));
        }

        [Theory]
        [InlineData("2007-04-05T24:00:00", 2007, 4, 6, 0, 0, 0)]
        [InlineData("2000-01-01T24:00:00", 2000, 1, 2, 0, 0, 0)]
        [InlineData("2000-12-31T24:00:00", 2001, 1, 1, 0, 0, 0)]
        [InlineData("2000-02-28T24:00:00", 2000, 2, 29, 0, 0, 0)] // leap year
        [InlineData("2001-02-28T24:00:00", 2001, 3, 1, 0, 0, 0)] // non-leap year
        [InlineData("0001-01-01T24:00:00", 1, 1, 2, 0, 0, 0)] // earliest possible date
        public static void ToDateTimeOffset_EndOfDay_Valid(string input, int year, int month, int day, int hour, int minute, int second)
        {
            DateTimeOffset result = XmlConvert.ToDateTimeOffset(input);
            Assert.Equal(new DateTimeOffset(year, month, day, hour, minute, second, result.Offset), result);
        }

        [Theory]
        [InlineData("2007-04-05T24:00:00Z")]
        [InlineData("2007-04-05T24:00:00+05:30")]
        [InlineData("2007-04-05T24:00:00-08:00")]
        public static void ToDateTimeOffset_EndOfDay_WithTimeZone_Valid(string input)
        {
            DateTimeOffset result = XmlConvert.ToDateTimeOffset(input);
            Assert.Equal(6, result.Day);
            Assert.Equal(0, result.Hour);
        }

        [Theory]
        [InlineData("2007-04-05T24:01:00")]
        [InlineData("2007-04-05T24:00:01")]
        [InlineData("2007-04-05T24:00:00.0000001")]
        [InlineData("9999-12-31T24:00:00")]
        public static void ToDateTimeOffset_EndOfDay_Invalid(string input)
        {
            Assert.Throws<FormatException>(() => XmlConvert.ToDateTimeOffset(input));
        }

        [Theory]
        [InlineData("24:00:00")]
        [InlineData("24:00:00Z")]
        public static void ToDateTime_TimeOnly_EndOfDay_Valid(string input)
        {
            DateTime result = XmlConvert.ToDateTime(input, XmlDateTimeSerializationMode.RoundtripKind);
            Assert.Equal(0, result.Hour);
            Assert.Equal(0, result.Minute);
            Assert.Equal(0, result.Second);
        }

        [Theory]
        [InlineData("24:00:00+05:30")]
        [InlineData("24:00:00-08:00")]
        public static void ToDateTime_TimeOnly_EndOfDay_WithOffset_Valid(string input)
        {
            DateTime result = XmlConvert.ToDateTime(input, XmlDateTimeSerializationMode.RoundtripKind);
            Assert.Equal(0, result.Second);
        }

        [Theory]
        [InlineData("24:01:00")]
        [InlineData("24:00:01")]
        [InlineData("24:00:00.0000001")]
        public static void ToDateTime_TimeOnly_EndOfDay_Invalid(string input)
        {
            Assert.Throws<FormatException>(() => XmlConvert.ToDateTime(input, XmlDateTimeSerializationMode.RoundtripKind));
        }

        [Fact]
        public static void XmlSchemaValidation_DateTime_EndOfDay_Valid()
        {
            string xsd = @"<?xml version=""1.0"" encoding=""utf-8""?>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" targetNamespace=""endOfDate"" xmlns:tns=""endOfDate"" elementFormDefault=""qualified"">
  <xs:element name=""root"" type=""xs:dateTime""/>
</xs:schema>";

            string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<root xmlns=""endOfDate"">2007-04-05T24:00:00</root>";

            var settings = new XmlReaderSettings
            {
                ValidationType = ValidationType.Schema,
            };
            settings.Schemas.Add("endOfDate", XmlReader.Create(new StringReader(xsd)));

            using XmlReader reader = XmlReader.Create(new StringReader(xml), settings);
            var doc = new XmlDocument();
            doc.Load(reader);
        }

        [Fact]
        public static void XmlSchemaValidation_DateTime_EndOfDay_WithTimeZone_Valid()
        {
            string xsd = @"<?xml version=""1.0"" encoding=""utf-8""?>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" targetNamespace=""endOfDate"" xmlns:tns=""endOfDate"" elementFormDefault=""qualified"">
  <xs:element name=""root"" type=""xs:dateTime""/>
</xs:schema>";

            string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<root xmlns=""endOfDate"">2007-04-05T24:00:00Z</root>";

            var settings = new XmlReaderSettings
            {
                ValidationType = ValidationType.Schema,
            };
            settings.Schemas.Add("endOfDate", XmlReader.Create(new StringReader(xsd)));

            using XmlReader reader = XmlReader.Create(new StringReader(xml), settings);
            var doc = new XmlDocument();
            doc.Load(reader);
        }

        [Fact]
        public static void XmlSchemaValidation_Time_EndOfDay_Valid()
        {
            string xsd = @"<?xml version=""1.0"" encoding=""utf-8""?>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" targetNamespace=""endOfDate"" xmlns:tns=""endOfDate"" elementFormDefault=""qualified"">
  <xs:element name=""root"" type=""xs:time""/>
</xs:schema>";

            string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<root xmlns=""endOfDate"">24:00:00</root>";

            var settings = new XmlReaderSettings
            {
                ValidationType = ValidationType.Schema,
            };
            settings.Schemas.Add("endOfDate", XmlReader.Create(new StringReader(xsd)));

            using XmlReader reader = XmlReader.Create(new StringReader(xml), settings);
            var doc = new XmlDocument();
            doc.Load(reader);
        }

        [Fact]
        public static void XmlSchemaValidation_DateTime_EndOfDay_NonZeroMinute_Invalid()
        {
            string xsd = @"<?xml version=""1.0"" encoding=""utf-8""?>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" targetNamespace=""endOfDate"" xmlns:tns=""endOfDate"" elementFormDefault=""qualified"">
  <xs:element name=""root"" type=""xs:dateTime""/>
</xs:schema>";

            string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<root xmlns=""endOfDate"">2007-04-05T24:01:00</root>";

            var settings = new XmlReaderSettings
            {
                ValidationType = ValidationType.Schema,
            };
            settings.Schemas.Add("endOfDate", XmlReader.Create(new StringReader(xsd)));

            bool validationErrorOccurred = false;
            settings.ValidationEventHandler += (sender, e) =>
            {
                validationErrorOccurred = true;
            };

            using XmlReader reader = XmlReader.Create(new StringReader(xml), settings);
            var doc = new XmlDocument();
            doc.Load(reader);
            Assert.True(validationErrorOccurred);
        }

        [Theory]
        [InlineData("2007-04-05T00:00:00", 2007, 4, 5, 0, 0, 0)]
        [InlineData("2007-04-05T23:59:59", 2007, 4, 5, 23, 59, 59)]
        [InlineData("2007-04-05T12:30:45", 2007, 4, 5, 12, 30, 45)]
        [InlineData("2007-04-05T01:00:00", 2007, 4, 5, 1, 0, 0)]
        public static void ToDateTime_ExistingHours_StillWork(string input, int year, int month, int day, int hour, int minute, int second)
        {
            DateTime result = XmlConvert.ToDateTime(input, XmlDateTimeSerializationMode.RoundtripKind);
            Assert.Equal(new DateTime(year, month, day, hour, minute, second, DateTimeKind.Unspecified), result);
        }

        [Theory]
        [InlineData("2007-04-05T25:00:00")]
        [InlineData("2007-04-05T99:00:00")]
        public static void ToDateTime_HourAbove24_Invalid(string input)
        {
            Assert.Throws<FormatException>(() => XmlConvert.ToDateTime(input, XmlDateTimeSerializationMode.RoundtripKind));
        }
    }
}
