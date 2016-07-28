// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class DateTimeAttributeTests
    {
        [Fact]
        public static void ReadContentAsDateTimeAttribute1()
        {
            var reader = Utils.CreateFragmentReader("<Root a='  0002-01-01T00:00:00+00:00  '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(new DateTime(2, 1, 1, 0, 0, 0).Add(TimeZoneInfo.Local.GetUtcOffset(new DateTime(2, 1, 1))), (DateTime)reader.ReadContentAs(typeof(DateTime), null));
        }

        [Fact]
        public static void ReadContentAsDateTimeAttribute2()
        {
            var reader = Utils.CreateFragmentReader("<Root a='9998-12-31T12:59:59-00:00'/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(new DateTime(9998, 12, 31, 12, 59, 59).Add(TimeZoneInfo.Local.GetUtcOffset(new DateTime(9998, 12, 31))), (DateTime)reader.ReadContentAs(typeof(DateTime), null));
        }

        [Fact]
        public static void ReadContentAsDateTimeAttribute3()
        {
            var reader = Utils.CreateFragmentReader("<Root a='  2000-02-29T23:59:59+13:60  '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(new DateTime(2000, 2, 29, 23, 59, 59).Add(TimeZoneInfo.Local.GetUtcOffset(new DateTime(2000, 2, 29)) - new TimeSpan(14, 0, 0)), (DateTime)reader.ReadContentAs(typeof(DateTime), null));
        }

        [Fact]
        public static void ReadContentAsDateTimeAttribute4()
        {
            var reader = Utils.CreateFragmentReader("<Root a='  00:00:00+00:00   '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0, DateTimeKind.Utc).ToLocalTime(), (DateTime)reader.ReadContentAs(typeof(DateTime), null));
        }

        [Fact]
        public static void ReadContentAsDateTimeAttribute5()
        {
            var reader = Utils.CreateFragmentReader("<Root a='9999-31T12:59:60-11:00'/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(DateTime), null));
        }

        [Fact]
        public static void ReadContentAsDateTimeAttribute6()
        {
            var reader = Utils.CreateFragmentReader("<Root a='  3000-00-29T23:59:59.999999999999-13:60  '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(DateTime), null));
        }

        [Fact]
        public static void ReadContentAsDateTimeAttribute7()
        {
            var reader = Utils.CreateFragmentReader("<Root a='0000Z'/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(DateTime), null));
        }

        [Fact]
        public static void ReadContentAsDateTimeAttribute8()
        {
            var reader = Utils.CreateFragmentReader("<Root a='  0001-01-01T00:00:00-99:99z  '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(DateTime), null));
        }
    }
}
