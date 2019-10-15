// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class DateTimeTests
    {
        [Fact]
        public static void ReadContentAsDateTime1()
        {
            var reader = Utils.CreateFragmentReader("<Root>0001-<![CDATA[01]]>-01T0<?a?>0:00:00<!-- Comment inbetween--></Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(new DateTime(1, 1, 1, 0, 0, 0), (DateTime)reader.ReadContentAs(typeof(DateTime), null));
        }

        [Fact]
        public static void ReadContentAsDateTime10()
        {
            var reader = Utils.CreateFragmentReader("<Root>  20<?a?>02-1<![CDATA[2]]>-3<!-- Comment inbetween-->0  </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(new DateTime(2002, 12, 30, 0, 0, 0), (DateTime)reader.ReadContentAs(typeof(DateTime), null));
        }

        [Fact]
        public static void ReadContentAsDateTime11()
        {
            var reader = Utils.CreateFragmentReader("<Root>  <![CDATA[2]]>00<?a?>2-1<!-- Comment inbetween-->2-30Z  </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(new DateTime(2002, 12, 30, 0, 0, 0, 0).Add(new TimeSpan(0, 0, 0)), (DateTime)reader.ReadContentAs(typeof(DateTime), null));
        }

        [Fact]
        public static void ReadContentAsDateTime12()
        {
            var reader = Utils.CreateFragmentReader("<Root>  <!-- Comment inbetween-->0002-01-01T00:00:00+00:00  </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(new DateTime(2, 1, 1, 0, 0, 0).Add(TimeZoneInfo.Local.GetUtcOffset(new DateTime(2, 1, 1))), (DateTime)reader.ReadContentAs(typeof(DateTime), null));
        }

        [Fact]
        public static void ReadContentAsDateTime13()
        {
            var reader = Utils.CreateFragmentReader("<Root>001-01-01T00:00:00+00:00</Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(DateTime), null));
        }

        [Fact]
        public static void ReadContentAsDateTime14()
        {
            var reader = Utils.CreateFragmentReader("<Root>99<?a?>99-12-31T12:59:59+14:00:00</Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(DateTime), null));
        }

        [Fact]
        public static void ReadContentAsDateTime15()
        {
            var reader = Utils.CreateFragmentReader("<Root>0<?a?></Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(DateTime), null));
        }

        [Fact]
        public static void ReadContentAsDateTime16()
        {
            var reader = Utils.CreateFragmentReader("<Root>  9<?a?>999 Z</Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(DateTime), null));
        }

        [Fact]
        public static void ReadContentAsDateTime17()
        {
            var reader = Utils.CreateFragmentReader("<Root> ABC<?a?>D  </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(DateTime), null));
        }

        [Fact]
        public static void ReadContentAsDateTime18()
        {
            var reader = Utils.CreateFragmentReader("<Root>yyy<?a?>y-MM-ddTHH:mm</Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(DateTime), null));
        }

        [Fact]
        public static void ReadContentAsDateTime19()
        {
            var reader = Utils.CreateFragmentReader("<Root>21<?a?>00-02-29T23:59:59.9999999+13:60  </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(DateTime), null));
        }

        [Fact]
        public static void ReadContentAsDateTime2()
        {
            var reader = Utils.CreateFragmentReader("<Root>99<!-- Comment inbetween-->99-1<?a?>2-31T1<![CDATA[2]]>:59:59</Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(new DateTime(9999, 12, 31, 12, 59, 59), (DateTime)reader.ReadContentAs(typeof(DateTime), null));
        }

        [Fact]
        public static void ReadContentAsDateTime20()
        {
            var reader = Utils.CreateFragmentReader("<Root>3  000-0<?a?>2-29T23:59:59.999999999999-13:60</Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(DateTime), null));
        }

        [Fact]
        public static void ReadContentAsDateTime21()
        {
            var reader = Utils.CreateFragmentReader("<Root>2002-12-33</Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(DateTime), null));
        }

        [Fact]
        public static void ReadContentAsDateTime22()
        {
            var reader = Utils.CreateFragmentReader("<Root  >2002-13-30  </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(DateTime), null));
        }

        [Fact]
        public static void ReadContentAsDateTime3()
        {
            var reader = Utils.CreateFragmentReader("<Root>  0<?a?>0:0<!-- Comment inbetween-->0:00+00:00   </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0, DateTimeKind.Utc).ToLocalTime(), (DateTime)reader.ReadContentAs(typeof(DateTime), null));
        }

        [Fact]
        public static void ReadContentAsDateTime4()
        {
            var reader = Utils.CreateFragmentReader("<Root>00<!-- Comment inbetween-->01</Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(new DateTime(1, 1, 1, 0, 0, 0), (DateTime)reader.ReadContentAs(typeof(DateTime), null));
        }

        [Fact]
        public static void ReadContentAsDateTime5()
        {
            var reader = Utils.CreateFragmentReader("<Root>  999<!-- Comment inbetween-->9  </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(new DateTime(9999, 1, 1, 0, 0, 0), (DateTime)reader.ReadContentAs(typeof(DateTime), null));
        }

        [Fact]
        public static void ReadContentAsDateTime6()
        {
            var reader = Utils.CreateFragmentReader("<Root>  <![CDATA[0]]>001Z  </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(new DateTime(1, 1, 1, 0, 0, 0, 0).Add(new TimeSpan(0, 0, 0)), (DateTime)reader.ReadContentAs(typeof(DateTime), null));
        }

        [Fact]
        public static void ReadContentAsDateTime7()
        {
            var reader = Utils.CreateFragmentReader("<Root><![CDATA[9]]>999Z</Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(new DateTime(9999, 1, 1, 0, 0, 0, 0).Add(new TimeSpan(0, 0, 0)), (DateTime)reader.ReadContentAs(typeof(DateTime), null));
        }

        [Fact]
        public static void ReadContentAsDateTime8()
        {
            var reader = Utils.CreateFragmentReader("<Root>   2000-0<![CDATA[2]]>-29T23:59:59.999<?a?>9999   </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(new DateTime(2000, 2, 29, 23, 59, 59).AddTicks(9999999), (DateTime)reader.ReadContentAs(typeof(DateTime), null));
        }

        [Fact]
        public static void ReadContentAsDateTime9()
        {
            var reader = Utils.CreateFragmentReader("<Root>  2<?a?>00<!-- Comment inbetween-->0-02-29T23:59:5<?a?>9-13:<![CDATA[60]]>    </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(new DateTime(2000, 2, 29, 23, 59, 59).Add(TimeZoneInfo.Local.GetUtcOffset(new DateTime(2000, 2, 29)) + new TimeSpan(14, 0, 0)), (DateTime)reader.ReadContentAs(typeof(DateTime), null));
        }

        [Fact]
        public static void ReadContentAsDateTimeIsOutOfRange1()
        {
            var reader = Utils.CreateFragmentReader(@"<doc>   99<!-- Comment inbetween-->99-1<![CDATA[2]]>-31T01:60:5<?a?>9.99<?a?>9999<![CDATA[4]]>9<?Zz?>-00<![CDATA[:]]>00   </doc>");
            reader.PositionOnElementNoDoctype("doc");
            if (!reader.MoveToAttribute("a"))
                reader.Read();
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(DateTime), null));
        }

        [Fact]
        public static void ReadContentAsDateTimeIsOutOfRange2()
        {
            var reader = Utils.CreateFragmentReader(@"<f a='2002-02-29T23:59:59.9999999999999+13:61'/>");
            reader.PositionOnElementNoDoctype("f");
            if (!reader.MoveToAttribute("a"))
                reader.Read();
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(DateTime), null));
        }

        [Fact]
        public static void ReadContentAsDateTimeIsOutOfRange3()
        {
            var reader = Utils.CreateFragmentReader(@"<f a='2002-02-29T23:59:59.9999999999999+13:61'/>");
            reader.PositionOnElementNoDoctype("f");
            if (!reader.MoveToAttribute("a"))
                reader.Read();
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadContentAsDateTimeIsOutOfRange4()
        {
            var reader = Utils.CreateFragmentReader(@"<doc>   99<!-- Comment inbetween-->99-1<![CDATA[2]]>-31T01:60:5<?a?>9.99<?a?>9999<![CDATA[4]]>9<?Zz?>-00<![CDATA[:]]>00   </doc>");
            reader.PositionOnElementNoDoctype("doc");
            if (!reader.MoveToAttribute("a"))
                reader.Read();
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadContentAsDateTimeIsOutOfRangeDateTimeOffset1()
        {
            var reader = Utils.CreateFragmentReader(@"<doc>   99<!-- Comment inbetween-->99-1<![CDATA[2]]>-31T01:60:5<?a?>9.99<?a?>9999<![CDATA[4]]>9<?Zz?>-00<![CDATA[:]]>00   </doc>");
            reader.PositionOnElementNoDoctype("doc");
            if (!reader.MoveToAttribute("a"))
                reader.Read();
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(DateTime), null));
        }

        [Fact]
        public static void ReadContentAsDateTimeIsOutOfRangeDateTimeOffset2()
        {
            var reader = Utils.CreateFragmentReader(@"<f a='2002-02-29T23:59:59.9999999999999+13:61'/>");
            reader.PositionOnElementNoDoctype("f");
            if (!reader.MoveToAttribute("a"))
                reader.Read();
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(DateTime), null));
        }

        [Fact]
        public static void ReadContentAsDateTimeWithWhitespace1()
        {
            var reader = Utils.CreateFragmentReader(@"<doc>   9999-12-31   </doc>");
            reader.PositionOnElementNonEmptyNoDoctype("doc");
            reader.Read();
            Assert.Equal(new DateTime(9999, 12, 31, 0, 0, 0), reader.ReadContentAs(typeof(DateTime), null));
        }

        [Fact]
        public static void ReadContentAsDateTimeWithWhitespace2()
        {
            var reader = Utils.CreateFragmentReader(@"<doc>   9999-12-31   </doc>");
            reader.PositionOnElementNonEmptyNoDoctype("doc");
            reader.Read();
            Assert.Equal(new DateTimeOffset(9999, 12, 31, 0, 0, 0, TimeZoneInfo.Local.GetUtcOffset(new DateTime(9999, 12, 31))).ToString(), reader.ReadContentAs(typeof(DateTimeOffset), null).ToString());
        }
    }
}
