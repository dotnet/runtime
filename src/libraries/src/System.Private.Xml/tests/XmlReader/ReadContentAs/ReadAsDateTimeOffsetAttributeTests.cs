// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class DateTimeOffsetAttributeTests
    {
        [Fact]
        public static void ReadContentAsDateTimeOffsetAttribute1()
        {
            var reader = Utils.CreateFragmentReader("<Root a='  0002-01-01T00:00:00+00:00  '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(new DateTimeOffset(new DateTime(2, 1, 1, 0, 0, 0, DateTimeKind.Utc)), (DateTimeOffset)reader.ReadContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadContentAsDateTimeOffsetAttribute10()
        {
            var reader = Utils.CreateFragmentReader("<Root a='0000Z'/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(DateTime), null));
        }

        [Fact]
        public static void ReadContentAsDateTimeOffsetAttribute11()
        {
            var reader = Utils.CreateFragmentReader("<Root a='  3000-02-29T23:59:59.999999999999-13:60  '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Throws<XmlException>(() => reader.ReadContentAsDateTimeOffset());
        }

        [Fact]
        public static void ReadContentAsDateTimeOffsetAttribute12()
        {
            var reader = Utils.CreateFragmentReader("<Root a='  0000Z'/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Throws<XmlException>(() => reader.ReadContentAsDateTimeOffset());
        }

        [Fact]
        public static void ReadContentAsDateTimeOffsetAttribute13()
        {
            var reader = Utils.CreateFragmentReader("<Root a='  0001-01-01T00:00:00+00:00  '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(new DateTimeOffset(1, 1, 1, 0, 0, 0, TimeSpan.FromHours(0)).ToString(), reader.ReadContentAs(typeof(DateTimeOffset), null).ToString());
        }

        [Fact]
        public static void ReadContentAsDateTimeOffsetAttribute14()
        {
            var reader = Utils.CreateFragmentReader("<Root a='9999-12-31T12:59:59-10:60'/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(new DateTimeOffset(9999, 12, 31, 12, 59, 59, TimeSpan.FromHours(-11)).ToString(), reader.ReadContentAs(typeof(DateTimeOffset), null).ToString());
        }

        [Fact]
        public static void ReadContentAsDateTimeOffsetAttribute15()
        {
            var reader = Utils.CreateFragmentReader("<Root a='  2000-02-29T23:59:59.999999999999+13:60  '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(new DateTimeOffset(2000, 3, 1, 0, 0, 0, TimeSpan.FromHours(14)).ToString(), reader.ReadContentAs(typeof(DateTimeOffset), null).ToString());
        }

        [Fact]
        public static void ReadContentAsDateTimeOffsetAttribute16()
        {
            var reader = Utils.CreateFragmentReader("<Root a='  2002-01-01T12:01:01Z'/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(new DateTimeOffset(2002, 1, 1, 12, 1, 1, TimeSpan.FromHours(0)).ToString(), reader.ReadContentAs(typeof(DateTimeOffset), null).ToString());
        }

        [Fact]
        public static void ReadContentAsDateTimeOffsetAttribute17()
        {
            var reader = Utils.CreateFragmentReader("<Root a='2004-02-29T23:59:59.99999999999z  '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(new DateTimeOffset(2004, 3, 1, 0, 0, 0, TimeSpan.FromHours(0)).ToString(), reader.ReadContentAs(typeof(DateTimeOffset), null).ToString());
        }

        [Fact]
        public static void ReadContentAsDateTimeOffsetAttribute18()
        {
            var reader = Utils.CreateFragmentReader("<Root a='0000Z'/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadContentAsDateTimeOffsetAttribute19()
        {
            var reader = Utils.CreateFragmentReader("<Root a='9999-12-31T12:59:60-11:00'/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadContentAsDateTimeOffsetAttribute2()
        {
            var reader = Utils.CreateFragmentReader("<Root a='9998-12-31T12:59:59-00:00'/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(new DateTimeOffset(new DateTime(9998, 12, 31, 12, 59, 59, DateTimeKind.Utc)), (DateTimeOffset)reader.ReadContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadContentAsDateTimeOffsetAttribute20()
        {
            var reader = Utils.CreateFragmentReader("<Root a='3000-02-29T23:59:59.999999999999-13:60'/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadContentAsDateTimeOffsetAttribute21()
        {
            var reader = Utils.CreateFragmentReader("<Root a='0001-01-01T00:00:00-15:00'/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Throws<ArgumentOutOfRangeException>(() => reader.ReadContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadContentAsDateTimeOffsetAttribute3()
        {
            var reader = Utils.CreateFragmentReader("<Root a='  2000-02-29T23:59:59+13:60  '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(new DateTimeOffset(2000, 2, 29, 23, 59, 59, new TimeSpan(14, 0, 0)), (DateTimeOffset)reader.ReadContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadContentAsDateTimeOffsetAttribute4()
        {
            var reader = Utils.CreateFragmentReader("<Root a='  0002-01-01T00:00:00+00:00  '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(new DateTimeOffset(new DateTime(2, 1, 1, 0, 0, 0, DateTimeKind.Utc)), reader.ReadContentAsDateTimeOffset());
        }

        [Fact]
        public static void ReadContentAsDateTimeOffsetAttribute5()
        {
            var reader = Utils.CreateFragmentReader("<Root a='9998-12-31T12:59:59-00:00'/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(new DateTimeOffset(new DateTime(9998, 12, 31, 12, 59, 59, DateTimeKind.Utc)), reader.ReadContentAsDateTimeOffset());
        }

        [Fact]
        public static void ReadContentAsDateTimeOffsetAttribute6()
        {
            var reader = Utils.CreateFragmentReader("<Root a='  2000-02-29T23:59:59+13:60  '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(new DateTimeOffset(2000, 2, 29, 23, 59, 59, new TimeSpan(14, 0, 0)), reader.ReadContentAsDateTimeOffset());
        }

        [Fact]
        public static void ReadContentAsDateTimeOffsetAttribute7()
        {
            var reader = Utils.CreateFragmentReader("<Root a='  0001-01-01T00:00:00-99:99z  '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(DateTime), null));
        }

        [Fact]
        public static void ReadContentAsDateTimeOffsetAttribute8()
        {
            var reader = Utils.CreateFragmentReader("<Root a='9999-31T12:59:60-11:00'/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(DateTime), null));
        }

        [Fact]
        public static void ReadContentAsDateTimeOffsetAttribute9()
        {
            var reader = Utils.CreateFragmentReader("<Root a='  3000-00-29T23:59:59.999999999999-13:60  '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(DateTime), null));
        }
    }
}
