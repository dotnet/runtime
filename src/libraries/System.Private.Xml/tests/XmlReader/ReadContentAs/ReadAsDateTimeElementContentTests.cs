// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class DateTimeElementContentTests
    {
        [Fact]
        public static void ReadElementContentAsDateTime1()
        {
            var reader = Utils.CreateFragmentReader("<Root><![CDATA[9]]>999Z</Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(new DateTime(9999, 1, 1, 0, 0, 0, 0).Add(new TimeSpan(0, 0, 0)), (DateTime)reader.ReadElementContentAs(typeof(DateTime), null));
        }

        [Fact]
        public static void ReadElementContentAsDateTime10()
        {
            var reader = Utils.CreateFragmentReader("<Root>  20<?a?>02-1<![CDATA[2]]>-3<!-- Comment inbetween-->0  </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(new DateTime(2002, 12, 30, 0, 0, 0), (DateTime)reader.ReadElementContentAs(typeof(DateTime), null));
        }

        [Fact]
        public static void ReadElementContentAsDateTime11()
        {
            var reader = Utils.CreateFragmentReader("<Root>   2000-0<![CDATA[2]]>-29T23:59:59.999<?a?>9999   </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(new DateTime(2000, 2, 29, 23, 59, 59).AddTicks(9999999), (DateTime)reader.ReadElementContentAs(typeof(DateTime), null));
        }

        [Fact]
        public static void ReadElementContentAsDateTime12()
        {
            var reader = Utils.CreateFragmentReader("<Root>  2<?a?>00<!-- Comment inbetween-->0-02-29T23:59:5<?a?>9-13:<![CDATA[60]]>    </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(new DateTime(2000, 2, 29, 23, 59, 59).Add(TimeZoneInfo.Local.GetUtcOffset(new DateTime(2000, 2, 29)) + new TimeSpan(14, 0, 0)), (DateTime)reader.ReadElementContentAs(typeof(DateTime), null));
        }

        [Fact]
        public static void ReadElementContentAsDateTime13()
        {
            var reader = Utils.CreateFragmentReader("<Root>  2002-12-33  </Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTime), null));
        }

        [Fact]
        public static void ReadElementContentAsDateTime14()
        {
            var reader = Utils.CreateFragmentReader("<Root>0001-<![CDATA[0<!-- Comment inbetween-->1]]>-01T0<?a?>0:00:00</Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTime), null));
        }

        [Fact]
        public static void ReadElementContentAsDateTime15()
        {
            var reader = Utils.CreateFragmentReader("<Root>  20<?9?>02-1<![CDATA[2]]>-3<!-- Comment inbetween-->0  </Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTime), null));
        }

        [Fact]
        public static void ReadElementContentAsDateTime16()
        {
            var reader = Utils.CreateFragmentReader("<Root>  000<!-- Comment inbetween-->1-01-01T00:00:00-14:00z  </Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTime), null));
        }

        [Fact]
        public static void ReadElementContentAsDateTime17()
        {
            var reader = Utils.CreateFragmentReader("<Root>9999-1<![CDATA[0<!-- Comment inbetween-->1]]>-31T12:59:59+14:00z</Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTime), null));
        }

        [Fact]
        public static void ReadElementContentAsDateTime18()
        {
            var reader = Utils.CreateFragmentReader("<Root>9999-12-31T12:59:60-11:00</Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTime), null));
        }

        [Fact]
        public static void ReadElementContentAsDateTime19()
        {
            var reader = Utils.CreateFragmentReader("<Root> 0  </Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTime), null));
        }

        [Fact]
        public static void ReadElementContentAsDateTime2()
        {
            var reader = Utils.CreateFragmentReader("<Root>  <![CDATA[2]]>00<?a?>2-1<!-- Comment inbetween-->2-30Z  </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(new DateTime(2002, 12, 30, 0, 0, 0, 0).Add(new TimeSpan(0, 0, 0)), (DateTime)reader.ReadElementContentAs(typeof(DateTime), null));
        }

        [Fact]
        public static void ReadElementContentAsDateTime20()
        {
            var reader = Utils.CreateFragmentReader("<Root> 9999 Z </Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTime), null));
        }

        [Fact]
        public static void ReadElementContentAsDateTime21()
        {
            var reader = Utils.CreateFragmentReader("<Root>ABCD</Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTime), null));
        }

        [Fact]
        public static void ReadElementContentAsDateTime22()
        {
            var reader = Utils.CreateFragmentReader("<Root>  yyyy-MM-ddTHH:mm  </Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTime), null));
        }

        [Fact]
        public static void ReadElementContentAsDateTime23()
        {
            var reader = Utils.CreateFragmentReader("<Root>2100-02-29T23:59:59.9999999+13:60</Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTime), null));
        }

        [Fact]
        public static void ReadElementContentAsDateTime24()
        {
            var reader = Utils.CreateFragmentReader("<Root> 3000-02-29T23:59:59.999999999999 -13:60  </Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTime), null));
        }

        [Fact]
        public static void ReadElementContentAsDateTime25()
        {
            var reader = Utils.CreateFragmentReader("<Root>  <![CDATA[2]]>00<?a?>2-1<!-- Comment inbetween-->2-30Z  </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(new DateTime(2002, 12, 30, 0, 0, 0, 0).Add(new TimeSpan(0, 0, 0)), reader.ReadElementContentAs(typeof(DateTime), null, "Root", ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTime26()
        {
            var reader = Utils.CreateFragmentReader("<Root><![CDATA[9]]>999Z</Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(new DateTime(9999, 1, 1, 0, 0, 0, 0).Add(new TimeSpan(0, 0, 0)), reader.ReadElementContentAs(typeof(DateTime), null, "Root", ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTime27()
        {
            var reader = Utils.CreateFragmentReader("<Root>  2<?a?>00<!-- Comment inbetween-->0-02-29T23:59:5<?a?>9-13:<![CDATA[60]]>    </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(new DateTime(2000, 2, 29, 23, 59, 59).Add(TimeZoneInfo.Local.GetUtcOffset(new DateTime(2000, 2, 29)) + new TimeSpan(14, 0, 0)), reader.ReadElementContentAs(typeof(DateTime), null, "Root", ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTime28()
        {
            var reader = Utils.CreateFragmentReader("<Root>   2000-0<![CDATA[2]]>-29T23:59:59.999<?a?>9999   </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(new DateTime(2000, 2, 29, 23, 59, 59).AddTicks(9999999), reader.ReadElementContentAs(typeof(DateTime), null, "Root", ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTime29()
        {
            var reader = Utils.CreateFragmentReader("<Root>  20<?a?>02-1<![CDATA[2]]>-3<!-- Comment inbetween-->0  </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(new DateTime(2002, 12, 30, 0, 0, 0), reader.ReadElementContentAs(typeof(DateTime), null, "Root", ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTime3()
        {
            var reader = Utils.CreateFragmentReader("<Root>  <!-- Comment inbetween-->0002-01-01T00:00:00+00:00  </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(new DateTime(2, 1, 1, 0, 0, 0).Add(TimeZoneInfo.Local.GetUtcOffset(new DateTime(2, 1, 1))), (DateTime)reader.ReadElementContentAs(typeof(DateTime), null));
        }

        [Fact]
        public static void ReadElementContentAsDateTime30()
        {
            var reader = Utils.CreateFragmentReader("<Root>  <!-- Comment inbetween-->0002-01-01T00:00:00+00:00  </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(new DateTime(2, 1, 1, 0, 0, 0).Add(TimeZoneInfo.Local.GetUtcOffset(new DateTime(2, 1, 1))), reader.ReadElementContentAs(typeof(DateTime), null, "Root", ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTime31()
        {
            var reader = Utils.CreateFragmentReader("<Root>0001-<![CDATA[01]]>-01T0<?a?>0:00:00<!-- Comment inbetween--></Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(new DateTime(1, 1, 1, 0, 0, 0), reader.ReadElementContentAs(typeof(DateTime), null, "Root", ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTime32()
        {
            var reader = Utils.CreateFragmentReader("<Root>99<!-- Comment inbetween-->99-1<?a?>2-31T1<![CDATA[2]]>:59:59</Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(new DateTime(9999, 12, 31, 12, 59, 59), reader.ReadElementContentAs(typeof(DateTime), null, "Root", ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTime33()
        {
            var reader = Utils.CreateFragmentReader("<Root>  0<?a?>0:0<!-- Comment inbetween-->0:00+00:00   </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0, DateTimeKind.Utc).ToLocalTime(), reader.ReadElementContentAs(typeof(DateTime), null, "Root", ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTime34()
        {
            var reader = Utils.CreateFragmentReader("<Root>00<!-- Comment inbetween-->01</Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(new DateTime(1, 1, 1, 0, 0, 0), reader.ReadElementContentAs(typeof(DateTime), null, "Root", ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTime35()
        {
            var reader = Utils.CreateFragmentReader("<Root>  999<!-- Comment inbetween-->9  </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(new DateTime(9999, 1, 1, 0, 0, 0), reader.ReadElementContentAs(typeof(DateTime), null, "Root", ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTime36()
        {
            var reader = Utils.CreateFragmentReader("<Root>  <![CDATA[0]]>001Z  </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(new DateTime(1, 1, 1, 0, 0, 0, 0).Add(new TimeSpan(0, 0, 0)), reader.ReadElementContentAs(typeof(DateTime), null, "Root", ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTime37()
        {
            var reader = Utils.CreateFragmentReader("<Root>2100-02-29T23:59:59.9999999+13:60</Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTime), null, "Root", ""));
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTime), null, "Root", ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTime38()
        {
            var reader = Utils.CreateFragmentReader("<Root>001-01-01T00:00:00+00:00</Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTime), null, "Root", ""));
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTime), null, "Root", ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTime39()
        {
            var reader = Utils.CreateFragmentReader("<Root>0001-01-01T00:00:00-14:01Z</Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTime), null, "Root", ""));
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTime), null, "Root", ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTime4()
        {
            var reader = Utils.CreateFragmentReader("<Root>0001-<![CDATA[01]]>-01T0<?a?>0:00:00<!-- Comment inbetween--></Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(new DateTime(1, 1, 1, 0, 0, 0), (DateTime)reader.ReadElementContentAs(typeof(DateTime), null));
        }

        [Fact]
        public static void ReadElementContentAsDateTime40()
        {
            var reader = Utils.CreateFragmentReader("<Root>999<?9?>9-12-31T12:59:59+15:00Z</Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTime), null, "Root", ""));
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTime), null, "Root", ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTime41()
        {
            var reader = Utils.CreateFragmentReader("<Root>9999-12-31T12:59:60-11:00</Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTime), null, "Root", ""));
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTime), null, "Root", ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTime42()
        {
            var reader = Utils.CreateFragmentReader("<Root>0</Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTime), null, "Root", ""));
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTime), null, "Root", ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTime43()
        {
            var reader = Utils.CreateFragmentReader("<Root><![CDATA[9<!-- Comment inbetween-->]]>999Z</Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTime), null, "Root", ""));
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTime), null, "Root", ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTime44()
        {
            var reader = Utils.CreateFragmentReader("<Root>ABCD</Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTime), null, "Root", ""));
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTime), null, "Root", ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTime45()
        {
            var reader = Utils.CreateFragmentReader("<Root>yyyy-MM-ddTHH:mm</Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTime), null, "Root", ""));
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTime), null, "Root", ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTime46()
        {
            var reader = Utils.CreateFragmentReader("<Root>2002-12-33</Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTime), null, "Root", ""));
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTime), null, "Root", ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTime47()
        {
            var reader = Utils.CreateFragmentReader("<Root>3000-02-29T23:59:59.999999999999-13:60</Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTime), null, "Root", ""));
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTime), null, "Root", ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTime48()
        {
            var reader = Utils.CreateFragmentReader("<Root>2100-02-29T23:59:5<![CDATA[9]]>.999999999999Z</Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTime), null, "Root", ""));
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTime), null, "Root", ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTime49()
        {
            var reader = Utils.CreateFragmentReader("<Root>3000-02-29T2<?9?>3:59:59.99<![CDATA[9]]><?a?>99<!-- Comment inbetween-->9999999999z</Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTime), null, "Root", ""));
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTime), null, "Root", ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTime5()
        {
            var reader = Utils.CreateFragmentReader("<Root>99<!-- Comment inbetween-->99-1<?a?>2-31T1<![CDATA[2]]>:59:59</Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(new DateTime(9999, 12, 31, 12, 59, 59), (DateTime)reader.ReadElementContentAs(typeof(DateTime), null));
        }

        [Fact]
        public static void ReadElementContentAsDateTime50()
        {
            var reader = Utils.CreateFragmentReader("<Root>2002-13-30</Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTime), null, "Root", ""));
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTime), null, "Root", ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTime6()
        {
            var reader = Utils.CreateFragmentReader("<Root>  0<?a?>0:0<!-- Comment inbetween-->0:00+00:00   </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0, DateTimeKind.Utc).ToLocalTime(), (DateTime)reader.ReadElementContentAs(typeof(DateTime), null));
        }

        [Fact]
        public static void ReadElementContentAsDateTime7()
        {
            var reader = Utils.CreateFragmentReader("<Root>00<!-- Comment inbetween-->01</Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(new DateTime(1, 1, 1, 0, 0, 0), (DateTime)reader.ReadElementContentAs(typeof(DateTime), null));
        }

        [Fact]
        public static void ReadElementContentAsDateTime8()
        {
            var reader = Utils.CreateFragmentReader("<Root>  999<!-- Comment inbetween-->9  </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(new DateTime(9999, 1, 1, 0, 0, 0), (DateTime)reader.ReadElementContentAs(typeof(DateTime), null));
        }

        [Fact]
        public static void ReadElementContentAsDateTime9()
        {
            var reader = Utils.CreateFragmentReader("<Root>  <![CDATA[0]]>001Z  </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(new DateTime(1, 1, 1, 0, 0, 0, 0).Add(new TimeSpan(0, 0, 0)), (DateTime)reader.ReadElementContentAs(typeof(DateTime), null));
        }

        [Fact]
        public static void ReadElementContentAsDateTimenull1()
        {
            var reader = Utils.CreateFragmentReader("<Root>999</Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<ArgumentNullException>(() => reader.ReadElementContentAs(typeof(DateTime), null, null, ""));
            Assert.Throws<ArgumentNullException>(() => reader.ReadElementContentAs(typeof(DateTime), null, null, ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTimenull2()
        {
            var reader = Utils.CreateFragmentReader("<Root>9999</Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<ArgumentNullException>(() => reader.ReadElementContentAs(typeof(DateTime), null, null, ""));
            Assert.Throws<ArgumentNullException>(() => reader.ReadElementContentAs(typeof(DateTime), null, null, ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTimenull3()
        {
            var reader = Utils.CreateFragmentReader("<Root>9999</Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<ArgumentNullException>(() => reader.ReadElementContentAs(typeof(DateTime), null, "Root", null));
            Assert.Throws<ArgumentNullException>(() => reader.ReadElementContentAs(typeof(DateTime), null, "Root", null));
        }

        [Fact]
        public static void ReadElementContentAsDateTimenull4()
        {
            var reader = Utils.CreateFragmentReader("<Root>999</Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<ArgumentNullException>(() => reader.ReadElementContentAs(typeof(DateTime), null, "Root", null));
            Assert.Throws<ArgumentNullException>(() => reader.ReadElementContentAs(typeof(DateTime), null, "Root", null));
        }
    }
}
