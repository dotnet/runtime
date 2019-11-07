// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class DateTimeOffsetElementContentTests
    {
        [Fact]
        public static void ReadElementContentAsDateTimeOffset100()
        {
            var reader = Utils.CreateFragmentReader("<Root><![CDATA[00]]>01-01-01<?a?>T00:00:0<!-- Comment inbetween-->0+00:00</Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(new DateTimeOffset(1, 1, 1, 0, 0, 0, TimeSpan.FromHours(0)).ToString(), reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", "").ToString());
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset101()
        {
            var reader = Utils.CreateFragmentReader("<Root>   0001-01-01T00<?a?>:00:00<!-- Comment inbetween-->-1<!-- Comment inbetween-->3:<![CDATA[60]]>   </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(new DateTimeOffset(1, 1, 1, 0, 0, 0, TimeSpan.FromHours(-14)).ToString(), reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", "").ToString());
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset102()
        {
            var reader = Utils.CreateFragmentReader("<Root>0<!-- Comment inbetween-->01-0<?a?>1-01T00:00:00+00:00</Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", ""));
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset103()
        {
            var reader = Utils.CreateFragmentReader("<Root><![CDATA[2<?a?>002-12-33]]></Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", ""));
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset104()
        {
            var reader = Utils.CreateFragmentReader("<Root>0001-01-<!-- Comment inbetween-->01T00:0<?a?>0:00-14:01</Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<ArgumentOutOfRangeException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", ""));
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset105()
        {
            var reader = Utils.CreateFragmentReader("<Root>9999-<!-- Comment inbetween-->12-3<?a?>1T1<![CDATA[2]]>:59:59+15:00</Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<ArgumentOutOfRangeException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", ""));
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset106()
        {
            var reader = Utils.CreateFragmentReader("<Root>999<!-- Comment inbetween-->9-1<![CDATA[2]]>-31T1<![CDATA[2]]>:59:6<?a?>0-1<?a?>1:00</Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", ""));
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset107()
        {
            var reader = Utils.CreateFragmentReader("<Root><![CDATA[2]]>0</Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", ""));
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset108()
        {
            var reader = Utils.CreateFragmentReader("<Root>99<!-- Comment inbetween-->9<![CDATA[z]]></Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", ""));
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset109()
        {
            var reader = Utils.CreateFragmentReader("<Root><![CDATA[z]]></Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", ""));
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset11()
        {
            var reader = Utils.CreateFragmentReader("<Root>  <![CDATA[2]]>00<?a?>2-1<!-- Comment inbetween-->2-30Z  </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(new DateTimeOffset(new DateTime(2002, 12, 30, 0, 0, 0, 0, DateTimeKind.Utc)), (DateTimeOffset)reader.ReadElementContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset110()
        {
            var reader = Utils.CreateFragmentReader("<Root><![CDATA[Z]]></Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", ""));
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset111()
        {
            var reader = Utils.CreateFragmentReader("<Root><![CDATA[2]]>100-02-29T23:59:59.9999999+13:60</Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", ""));
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset112()
        {
            var reader = Utils.CreateFragmentReader("<Root>300<!-- Comment inbetween-->0-0<![CDATA[2]]>-29T23:59:<?a?>59.999999999999-13:60</Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", ""));
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset113()
        {
            var reader = Utils.CreateFragmentReader("<Root>21<!-- Comment inbetween-->00-02-29T<![CDATA[2]]>3:59:59.<?a?>999999999999Z</Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", ""));
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset114()
        {
            var reader = Utils.CreateFragmentReader("<Root>3<!-- Comment inbetween-->000-02<!-- Comment inbetween-->-29T<?a?>23:59:59.999<![CDATA[2]]>99999999z</Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", ""));
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset115()
        {
            var reader = Utils.CreateFragmentReader("<Root><![CDATA[2]]>0<!-- Comment inbetween-->0<?a?><![CDATA[2]]>-13-30</Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", ""));
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset12()
        {
            var reader = Utils.CreateFragmentReader("<Root>  <!-- Comment inbetween-->0002-01-01T00:00:00+00:00  </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(new DateTimeOffset(new DateTime(2, 1, 1, 0, 0, 0, DateTimeKind.Utc)), (DateTimeOffset)reader.ReadElementContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset13()
        {
            var reader = Utils.CreateFragmentReader("<Root>99<!-- Comment inbetween-->99-1<?a?>2-31T1<![CDATA[2]]>:59:59</Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(new DateTimeOffset(new DateTime(9999, 12, 31, 12, 59, 59, DateTimeKind.Local)), (DateTimeOffset)reader.ReadElementContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset14()
        {
            var reader = Utils.CreateFragmentReader("<Root>  999<!-- Comment inbetween-->9  </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(new DateTimeOffset(new DateTime(9999, 1, 1, 0, 0, 0, DateTimeKind.Local)), (DateTimeOffset)reader.ReadElementContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset15()
        {
            var reader = Utils.CreateFragmentReader("<Root>  <![CDATA[0]]>001Z  </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)), (DateTimeOffset)reader.ReadElementContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset16()
        {
            var reader = Utils.CreateFragmentReader("<Root><![CDATA[9]]>999Z</Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(new DateTimeOffset(new DateTime(9999, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)), (DateTimeOffset)reader.ReadElementContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset17()
        {
            var reader = Utils.CreateFragmentReader("<Root>   2000-0<![CDATA[2]]>-29T23:59:59.999<?a?>9999   </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(new DateTimeOffset(new DateTime(2000, 2, 29, 23, 59, 59, DateTimeKind.Local).AddTicks(9999999)), (DateTimeOffset)reader.ReadElementContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset18()
        {
            var reader = Utils.CreateFragmentReader("<Root>  2<?a?>00<!-- Comment inbetween-->0-02-29T23:59:5<?a?>9-13:<![CDATA[60]]>    </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(new DateTimeOffset(2000, 2, 29, 23, 59, 59, new TimeSpan(-14, 0, 0)), (DateTimeOffset)reader.ReadElementContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset2()
        {
            var reader = Utils.CreateFragmentReader("<Root>  20<?a?>02-1<![CDATA[2]]>-3<!-- Comment inbetween-->0  </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(new DateTimeOffset(new DateTime(2002, 12, 30, 0, 0, 0, DateTimeKind.Local)), (DateTimeOffset)reader.ReadElementContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset31()
        {
            var reader = Utils.CreateFragmentReader("<Root>0001-<![CDATA[0<!-- Comment inbetween-->1]]>-01T0<?a?>0:00:00</Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset32()
        {
            var reader = Utils.CreateFragmentReader("<Root>  20<?9?>02-1<![CDATA[2]]>-3<!-- Comment inbetween-->0  </Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset33()
        {
            var reader = Utils.CreateFragmentReader("<Root>  000<!-- Comment inbetween-->1-01-01T00:00:00-14:00z  </Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset34()
        {
            var reader = Utils.CreateFragmentReader("<Root>9999-1<![CDATA[0<!-- Comment inbetween-->1]]>-31T12:59:59+14:00z</Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset35()
        {
            var reader = Utils.CreateFragmentReader("<Root>9999-12-31T12:59:60-11:00</Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset36()
        {
            var reader = Utils.CreateFragmentReader("<Root> 0  </Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset37()
        {
            var reader = Utils.CreateFragmentReader("<Root> 9999 Z </Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset38()
        {
            var reader = Utils.CreateFragmentReader("<Root>ABCD</Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset39()
        {
            var reader = Utils.CreateFragmentReader("<Root>  yyyy-MM-ddTHH:mm  </Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset40()
        {
            var reader = Utils.CreateFragmentReader("<Root>2100-02-29T23:59:59.9999999+13:60</Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset41()
        {
            var reader = Utils.CreateFragmentReader("<Root> 3000-02-29T23:59:59.999999999999 -13:60  </Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset42()
        {
            var reader = Utils.CreateFragmentReader("<Root>  2002-12-33  </Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset43()
        {
            var reader = Utils.CreateFragmentReader("<Root>  <!-- Comment inbetween-->0002-01-01T00:00:00+00:00  </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(new DateTimeOffset(new DateTime(2, 1, 1, 0, 0, 0, DateTimeKind.Utc)), reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset44()
        {
            var reader = Utils.CreateFragmentReader("<Root>   2000-0<![CDATA[2]]>-29T23:59:59.999<?a?>9999   </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(new DateTimeOffset(new DateTime(2000, 2, 29, 23, 59, 59, DateTimeKind.Local).AddTicks(9999999)), reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset45()
        {
            var reader = Utils.CreateFragmentReader("<Root>  2<?a?>00<!-- Comment inbetween-->0-02-29T23:59:5<?a?>9-13:<![CDATA[60]]>    </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(new DateTimeOffset(2000, 2, 29, 23, 59, 59, new TimeSpan(-14, 0, 0)), reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset46()
        {
            var reader = Utils.CreateFragmentReader("<Root>  999<!-- Comment inbetween-->9  </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(new DateTimeOffset(new DateTime(9999, 1, 1, 0, 0, 0, DateTimeKind.Local)), reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset47()
        {
            var reader = Utils.CreateFragmentReader("<Root>  20<?a?>02-1<![CDATA[2]]>-3<!-- Comment inbetween-->0  </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(new DateTimeOffset(new DateTime(2002, 12, 30, 0, 0, 0, DateTimeKind.Local)), reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset48()
        {
            var reader = Utils.CreateFragmentReader("<Root>  <![CDATA[2]]>00<?a?>2-1<!-- Comment inbetween-->2-30Z  </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(new DateTimeOffset(new DateTime(2002, 12, 30, 0, 0, 0, 0, DateTimeKind.Utc)), reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset49()
        {
            var reader = Utils.CreateFragmentReader("<Root>  <![CDATA[0]]>001Z  </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)), reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset50()
        {
            var reader = Utils.CreateFragmentReader("<Root>99<!-- Comment inbetween-->99-1<?a?>2-31T1<![CDATA[2]]>:59:59</Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(new DateTimeOffset(new DateTime(9999, 12, 31, 12, 59, 59, DateTimeKind.Local)), reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset51()
        {
            var reader = Utils.CreateFragmentReader("<Root><![CDATA[9]]>999Z</Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(new DateTimeOffset(new DateTime(9999, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)), reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset52()
        {
            var reader = Utils.CreateFragmentReader("<Root>0001-01-01T00:00:00-14:01Z</Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", ""));
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset53()
        {
            var reader = Utils.CreateFragmentReader("<Root>001-01-01T00:00:00+00:00</Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", ""));
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset54()
        {
            var reader = Utils.CreateFragmentReader("<Root>2002-12-33</Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", ""));
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset55()
        {
            var reader = Utils.CreateFragmentReader("<Root>999<?9?>9-12-31T12:59:59+15:00Z</Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", ""));
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset56()
        {
            var reader = Utils.CreateFragmentReader("<Root>9999-12-31T12:59:60-11:00</Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", ""));
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset57()
        {
            var reader = Utils.CreateFragmentReader("<Root>0</Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", ""));
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset58()
        {
            var reader = Utils.CreateFragmentReader("<Root><![CDATA[9<!-- Comment inbetween-->]]>999Z</Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", ""));
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset59()
        {
            var reader = Utils.CreateFragmentReader("<Root>ABCD</Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", ""));
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset60()
        {
            var reader = Utils.CreateFragmentReader("<Root>yyyy-MM-ddTHH:mm</Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", ""));
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset61()
        {
            var reader = Utils.CreateFragmentReader("<Root>2100-02-29T23:59:59.9999999+13:60</Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", ""));
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset62()
        {
            var reader = Utils.CreateFragmentReader("<Root>3000-02-29T23:59:59.999999999999-13:60</Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", ""));
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset63()
        {
            var reader = Utils.CreateFragmentReader("<Root>2100-02-29T23:59:5<![CDATA[9]]>.999999999999Z</Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", ""));
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset64()
        {
            var reader = Utils.CreateFragmentReader("<Root>3000-02-29T2<?9?>3:59:59.99<![CDATA[9]]><?a?>99<!-- Comment inbetween-->9999999999z</Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", ""));
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset65()
        {
            var reader = Utils.CreateFragmentReader("<Root>2002-13-30</Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", ""));
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset66()
        {
            var reader = Utils.CreateFragmentReader("<Root>   200<!-- Comment inbetween-->2-<![CDATA[12]]>-3<?a?>0Z   </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(new DateTimeOffset(2002, 12, 30, 0, 0, 0, TimeSpan.FromHours(0)).ToString(), reader.ReadElementContentAs(typeof(DateTimeOffset), null).ToString());
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset67()
        {
            var reader = Utils.CreateFragmentReader("<Root><![CDATA[00]]>01-01-01<?a?>T00:00:0<!-- Comment inbetween-->0+00:00</Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(new DateTimeOffset(1, 1, 1, 0, 0, 0, TimeSpan.FromHours(0)).ToString(), reader.ReadElementContentAs(typeof(DateTimeOffset), null).ToString());
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset68()
        {
            var reader = Utils.CreateFragmentReader("<Root>   0001-01-01T00<?a?>:00:00<!-- Comment inbetween-->-1<!-- Comment inbetween-->3:<![CDATA[60]]>   </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(new DateTimeOffset(1, 1, 1, 0, 0, 0, TimeSpan.FromHours(-14)).ToString(), reader.ReadElementContentAs(typeof(DateTimeOffset), null).ToString());
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset69()
        {
            var reader = Utils.CreateFragmentReader("<Root><![CDATA[9]]>99<?a?>9-12-31T12:<!-- Comment inbetween-->5<![CDATA[9]]>:5<![CDATA[9]]>+14:00</Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(new DateTimeOffset(9999, 12, 31, 12, 59, 59, TimeSpan.FromHours(+14)).ToString(), reader.ReadElementContentAs(typeof(DateTimeOffset), null).ToString());
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset70()
        {
            var reader = Utils.CreateFragmentReader("<Root>  9<![CDATA[9]]>99-1<?a?>2-31T1<!-- Comment inbetween-->2:59:59-10:60     </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(new DateTimeOffset(9999, 12, 31, 12, 59, 59, TimeSpan.FromHours(-11)).ToString(), reader.ReadElementContentAs(typeof(DateTimeOffset), null).ToString());
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset71()
        {
            var reader = Utils.CreateFragmentReader("<Root>  20<!-- Comment inbetween-->05 </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(new DateTimeOffset(2005, 1, 1, 0, 0, 0, TimeZoneInfo.Local.GetUtcOffset(new DateTime(2005, 1, 1))).ToString(), reader.ReadElementContentAs(typeof(DateTimeOffset), null).ToString());
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset72()
        {
            var reader = Utils.CreateFragmentReader("<Root> 9<![CDATA[9]]>9<!-- Comment inbetween-->9<?a?> </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(new DateTimeOffset(9999, 1, 1, 0, 0, 0, TimeZoneInfo.Local.GetUtcOffset(new DateTime(9999, 1, 1))).ToString(), reader.ReadElementContentAs(typeof(DateTimeOffset), null).ToString());
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset73()
        {
            var reader = Utils.CreateFragmentReader("<Root> 0<?a?>0<!-- Comment inbetween-->01Z </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(new DateTimeOffset(1, 1, 1, 0, 0, 0, TimeSpan.FromHours(0)).ToString(), reader.ReadElementContentAs(typeof(DateTimeOffset), null).ToString());
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset74()
        {
            var reader = Utils.CreateFragmentReader("<Root>99<!-- Comment inbetween-->9<![CDATA[9]]>Z<?a?></Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(new DateTimeOffset(9999, 1, 1, 0, 0, 0, TimeSpan.FromHours(0)).ToString(), reader.ReadElementContentAs(typeof(DateTimeOffset), null).ToString());
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset75()
        {
            var reader = Utils.CreateFragmentReader("<Root>2<!-- Comment inbetween-->000-02-29T23:5<?a?>9:5<![CDATA[9]]>.999999<![CDATA[9]]>+13:60  </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(new DateTimeOffset(2000, 2, 29, 23, 59, 59, TimeSpan.FromHours(14)).ToString(), reader.ReadElementContentAs(typeof(DateTimeOffset), null).ToString());
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset76()
        {
            var reader = Utils.CreateFragmentReader("<Root>    20<?a?>00-02-29T23:59:59.999999999999-<!-- Comment inbetween-->13:60</Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(new DateTimeOffset(2000, 3, 1, 0, 0, 0, TimeSpan.FromHours(-14)).ToString(), reader.ReadElementContentAs(typeof(DateTimeOffset), null).ToString());
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset77()
        {
            var reader = Utils.CreateFragmentReader("<Root>2<?a?>00<!-- Comment inbetween-->2-12-3<![CDATA[0]]></Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(new DateTimeOffset(2002, 12, 30, 0, 0, 0, TimeZoneInfo.Local.GetUtcOffset(new DateTime(2002, 12, 30))).ToString(), reader.ReadElementContentAs(typeof(DateTimeOffset), null).ToString());
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset78()
        {
            var reader = Utils.CreateFragmentReader("<Root>0001-<![CDATA[0<!-- Comment inbetween-->1]]>-01T0<?a?>0:00:00</Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset79()
        {
            var reader = Utils.CreateFragmentReader("<Root>  20<?9?>02-1<![CDATA[2]]>-<?a?>3<!-- Comment inbetween-->0  </Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset80()
        {
            var reader = Utils.CreateFragmentReader("<Root><![CDATA[2]]>01-01-01T00<?a?>:0<!-- Comment inbetween-->0:00+00:00</Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset81()
        {
            var reader = Utils.CreateFragmentReader("<Root><![CDATA[2]]>00<?a?>1-01-01T00:<!-- Comment inbetween-->00:00-14:01</Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<ArgumentOutOfRangeException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset82()
        {
            var reader = Utils.CreateFragmentReader("<Root><!-- Comment inbetween-->99<?a?>99-12-31T12:59:59+14:0<![CDATA[2]]></Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<ArgumentOutOfRangeException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset83()
        {
            var reader = Utils.CreateFragmentReader("<Root>9<?a?>999-1<!-- Comment inbetween-->2-31T12:<![CDATA[2]]>9:60-11:00</Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset84()
        {
            var reader = Utils.CreateFragmentReader("<Root><?a?>0<![CDATA[2]]><!-- Comment inbetween--></Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset85()
        {
            var reader = Utils.CreateFragmentReader("<Root>9<?a?>9<!-- Comment inbetween-->9<![CDATA[z]]></Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset86()
        {
            var reader = Utils.CreateFragmentReader("<Root><![CDATA[z<!-- Comment inbetween-->]]></Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset87()
        {
            var reader = Utils.CreateFragmentReader("<Root><![CDATA[<!-- Comment inbetween-->Z]]></Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset88()
        {
            var reader = Utils.CreateFragmentReader("<Root>21<!-- Comment inbetween-->00-02-<![CDATA[2]]>9T23:59:59.9999999+13:60</Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset89()
        {
            var reader = Utils.CreateFragmentReader("<Root>30<!-- Comment inbetween-->00-02-29T<![CDATA[2]]>3:59:59.999999999999-13:60</Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset90()
        {
            var reader = Utils.CreateFragmentReader("<Root> 9<![CDATA[9]]>9<!-- Comment inbetween-->9<?a?> </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(new DateTimeOffset(9999, 1, 1, 0, 0, 0, TimeZoneInfo.Local.GetUtcOffset(new DateTime(9999, 1, 1))).ToString(), reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", "").ToString());
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset91()
        {
            var reader = Utils.CreateFragmentReader("<Root><![CDATA[9]]>99<?a?>9-12-31T12:<!-- Comment inbetween-->5<![CDATA[9]]>:5<![CDATA[9]]>+14:00</Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(new DateTimeOffset(9999, 12, 31, 12, 59, 59, TimeSpan.FromHours(+14)).ToString(), reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", "").ToString());
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset92()
        {
            var reader = Utils.CreateFragmentReader("<Root>  9<![CDATA[9]]>99-1<?a?>2-31T1<!-- Comment inbetween-->2:59:59-10:60     </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(new DateTimeOffset(9999, 12, 31, 12, 59, 59, TimeSpan.FromHours(-11)).ToString(), reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", "").ToString());
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset93()
        {
            var reader = Utils.CreateFragmentReader("<Root>  20<!-- Comment inbetween-->05 </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(new DateTimeOffset(2005, 1, 1, 0, 0, 0, TimeZoneInfo.Local.GetUtcOffset(new DateTime(2005, 1, 1))).ToString(), reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", "").ToString());
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset94()
        {
            var reader = Utils.CreateFragmentReader("<Root>2<?a?>00<!-- Comment inbetween-->2-12-3<![CDATA[0]]></Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(new DateTimeOffset(2002, 12, 30, 0, 0, 0, TimeZoneInfo.Local.GetUtcOffset(new DateTime(2002, 12, 30))).ToString(), reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", "").ToString());
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset95()
        {
            var reader = Utils.CreateFragmentReader("<Root> 0<?a?>0<!-- Comment inbetween-->01Z </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(new DateTimeOffset(1, 1, 1, 0, 0, 0, TimeSpan.FromHours(0)).ToString(), reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", "").ToString());
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset96()
        {
            var reader = Utils.CreateFragmentReader("<Root>99<!-- Comment inbetween-->9<![CDATA[9]]>Z<?a?></Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(new DateTimeOffset(9999, 1, 1, 0, 0, 0, TimeSpan.FromHours(0)).ToString(), reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", "").ToString());
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset97()
        {
            var reader = Utils.CreateFragmentReader("<Root>2<!-- Comment inbetween-->000-02-29T23:5<?a?>9:5<![CDATA[9]]>.999999<![CDATA[9]]>+13:60  </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(new DateTimeOffset(2000, 2, 29, 23, 59, 59, TimeSpan.FromHours(14)).ToString(), reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", "").ToString());
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset98()
        {
            var reader = Utils.CreateFragmentReader("<Root>    20<?a?>00-02-29T23:59:59.999999999999-<!-- Comment inbetween-->13:60</Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(new DateTimeOffset(2000, 3, 1, 0, 0, 0, TimeSpan.FromHours(-14)).ToString(), reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", "").ToString());
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffset99()
        {
            var reader = Utils.CreateFragmentReader("<Root>   200<!-- Comment inbetween-->2-<![CDATA[12]]>-3<?a?>0Z   </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(new DateTimeOffset(2002, 12, 30, 0, 0, 0, TimeSpan.FromHours(0)).ToString(), reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", "").ToString());
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffsetnull1()
        {
            var reader = Utils.CreateFragmentReader("<Root>999</Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<ArgumentNullException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, null, ""));
            Assert.Throws<ArgumentNullException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, null, ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffsetnull2()
        {
            var reader = Utils.CreateFragmentReader("<Root>9999</Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<ArgumentNullException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, null, ""));
            Assert.Throws<ArgumentNullException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, null, ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffsetnull3()
        {
            var reader = Utils.CreateFragmentReader("<Root>9999</Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<ArgumentNullException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", null));
            Assert.Throws<ArgumentNullException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", null));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffsetnull4()
        {
            var reader = Utils.CreateFragmentReader("<Root>999</Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<ArgumentNullException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", null));
            Assert.Throws<ArgumentNullException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", null));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffsetnull5()
        {
            var reader = Utils.CreateFragmentReader("<Root>999</Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<ArgumentNullException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, null, ""));
            Assert.Throws<ArgumentNullException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, null, ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffsetnull6()
        {
            var reader = Utils.CreateFragmentReader("<Root>9999</Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<ArgumentNullException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, null, ""));
            Assert.Throws<ArgumentNullException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, null, ""));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffsetnull7()
        {
            var reader = Utils.CreateFragmentReader("<Root>999</Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<ArgumentNullException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", null));
            Assert.Throws<ArgumentNullException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", null));
        }

        [Fact]
        public static void ReadElementContentAsDateTimeOffsetnull8()
        {
            var reader = Utils.CreateFragmentReader("<Root>9999</Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<ArgumentNullException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", null));
            Assert.Throws<ArgumentNullException>(() => reader.ReadElementContentAs(typeof(DateTimeOffset), null, "Root", null));
        }
    }
}
