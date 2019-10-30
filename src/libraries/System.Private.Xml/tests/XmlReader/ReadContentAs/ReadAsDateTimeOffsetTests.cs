// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class DateTimeOffsetTests
    {
        [Fact]
        public static void ReadContentAsDateTimeOffset1()
        {
            var reader = Utils.CreateFragmentReader("<Root>  <![CDATA[0]]>001Z  </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)), (DateTimeOffset)reader.ReadContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadContentAsDateTimeOffset10()
        {
            var reader = Utils.CreateFragmentReader("<Root><![CDATA[9]]>999Z</Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(new DateTimeOffset(new DateTime(9999, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)), reader.ReadContentAsDateTimeOffset());
        }

        [Fact]
        public static void ReadContentAsDateTimeOffset11()
        {
            var reader = Utils.CreateFragmentReader("<Root>   2000-0<![CDATA[2]]>-29T23:59:59.999<?a?>9999   </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(new DateTimeOffset(new DateTime(2000, 2, 29, 23, 59, 59, DateTimeKind.Local).AddTicks(9999999)), reader.ReadContentAsDateTimeOffset());
        }

        [Fact]
        public static void ReadContentAsDateTimeOffset12()
        {
            var reader = Utils.CreateFragmentReader("<Root>  2<?a?>00<!-- Comment inbetween-->0-02-29T23:59:5<?a?>9-13:<![CDATA[60]]>    </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(new DateTimeOffset(2000, 2, 29, 23, 59, 59, new TimeSpan(-14, 0, 0)), reader.ReadContentAsDateTimeOffset());
        }

        [Fact]
        public static void ReadContentAsDateTimeOffset13()
        {
            var reader = Utils.CreateFragmentReader("<Root><![CDATA[9]]>999Z</Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(new DateTimeOffset(new DateTime(9999, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)), (DateTimeOffset)reader.ReadContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadContentAsDateTimeOffset14()
        {
            var reader = Utils.CreateFragmentReader("<Root>  20<?a?>02-1<![CDATA[2]]>-3<!-- Comment inbetween-->0  </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(new DateTimeOffset(new DateTime(2002, 12, 30, 0, 0, 0, DateTimeKind.Local)), (DateTimeOffset)reader.ReadContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadContentAsDateTimeOffset15()
        {
            var reader = Utils.CreateFragmentReader("<Root>  <![CDATA[2]]>00<?a?>2-1<!-- Comment inbetween-->2-30Z  </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(new DateTimeOffset(new DateTime(2002, 12, 30, 0, 0, 0, 0, DateTimeKind.Utc)), (DateTimeOffset)reader.ReadContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadContentAsDateTimeOffset16()
        {
            var reader = Utils.CreateFragmentReader("<Root>  <!-- Comment inbetween-->0002-01-01T00:00:00+00:00  </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(new DateTimeOffset(new DateTime(2, 1, 1, 0, 0, 0, DateTimeKind.Utc)), (DateTimeOffset)reader.ReadContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadContentAsDateTimeOffset17()
        {
            var reader = Utils.CreateFragmentReader("<Root>99<!-- Comment inbetween-->99-1<?a?>2-31T1<![CDATA[2]]>:59:59</Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(new DateTimeOffset(new DateTime(9999, 12, 31, 12, 59, 59, DateTimeKind.Local)), (DateTimeOffset)reader.ReadContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadContentAsDateTimeOffset18()
        {
            var reader = Utils.CreateFragmentReader("<Root>  999<!-- Comment inbetween-->9  </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(new DateTimeOffset(new DateTime(9999, 1, 1, 0, 0, 0, DateTimeKind.Local)), (DateTimeOffset)reader.ReadContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadContentAsDateTimeOffset19()
        {
            var reader = Utils.CreateFragmentReader("<Root>001-01-01T00:00:00+00:00</Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadContentAsDateTimeOffset2()
        {
            var reader = Utils.CreateFragmentReader("<Root>   2000-0<![CDATA[2]]>-29T23:59:59.999<?a?>9999   </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(new DateTimeOffset(new DateTime(2000, 2, 29, 23, 59, 59, DateTimeKind.Local).AddTicks(9999999)), (DateTimeOffset)reader.ReadContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadContentAsDateTimeOffset20()
        {
            var reader = Utils.CreateFragmentReader("<Root>99<?a?>99-12-31T12:59:59+14:00:00</Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadContentAsDateTimeOffset21()
        {
            var reader = Utils.CreateFragmentReader("<Root>0<?a?></Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadContentAsDateTimeOffset22()
        {
            var reader = Utils.CreateFragmentReader("<Root>  9<?a?>999 Z</Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadContentAsDateTimeOffset23()
        {
            var reader = Utils.CreateFragmentReader("<Root> ABC<?a?>D  </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadContentAsDateTimeOffset24()
        {
            var reader = Utils.CreateFragmentReader("<Root>yyy<?a?>y-MM-ddTHH:mm</Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadContentAsDateTimeOffset25()
        {
            var reader = Utils.CreateFragmentReader("<Root>21<?a?>00-02-29T23:59:59.9999999+13:60  </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadContentAsDateTimeOffset26()
        {
            var reader = Utils.CreateFragmentReader("<Root>3  000-0<?a?>2-29T23:59:59.999999999999-13:60</Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadContentAsDateTimeOffset27()
        {
            var reader = Utils.CreateFragmentReader("<Root>  200<?a?>2-12-33</Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Throws<XmlException>(() => reader.ReadContentAsDateTimeOffset());
        }

        [Fact]
        public static void ReadContentAsDateTimeOffset28()
        {
            var reader = Utils.CreateFragmentReader("<Root>20<?a?>0<![CDATA[2<?a?>]]>-13-30  </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Throws<XmlException>(() => reader.ReadContentAsDateTimeOffset());
        }

        [Fact]
        public static void ReadContentAsDateTimeOffset29()
        {
            var reader = Utils.CreateFragmentReader("<Root>   001-01-01<?a?>T00:00:00+00:00   </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Throws<XmlException>(() => reader.ReadContentAsDateTimeOffset());
        }

        [Fact]
        public static void ReadContentAsDateTimeOffset3()
        {
            var reader = Utils.CreateFragmentReader("<Root>  2<?a?>00<!-- Comment inbetween-->0-02-29T23:59:5<?a?>9-13:<![CDATA[60]]>    </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(new DateTimeOffset(2000, 2, 29, 23, 59, 59, new TimeSpan(-14, 0, 0)), (DateTimeOffset)reader.ReadContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadContentAsDateTimeOffset30()
        {
            var reader = Utils.CreateFragmentReader("<Root>9999-1<![CDATA[2]]>-31T12:59:59+14:00:00</Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Throws<XmlException>(() => reader.ReadContentAsDateTimeOffset());
        }

        [Fact]
        public static void ReadContentAsDateTimeOffset31()
        {
            var reader = Utils.CreateFragmentReader("<Root><?a?>0<!-- Comment inbetween--></Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Throws<XmlException>(() => reader.ReadContentAsDateTimeOffset());
        }

        [Fact]
        public static void ReadContentAsDateTimeOffset32()
        {
            var reader = Utils.CreateFragmentReader("<Root>  9<?a?>9<!-- Comment inbetween-->99  Z  </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Throws<XmlException>(() => reader.ReadContentAsDateTimeOffset());
        }

        [Fact]
        public static void ReadContentAsDateTimeOffset33()
        {
            var reader = Utils.CreateFragmentReader("<Root>A<?a?>B<!-- Comment inbetween-->CD</Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Throws<XmlException>(() => reader.ReadContentAsDateTimeOffset());
        }

        [Fact]
        public static void ReadContentAsDateTimeOffset34()
        {
            var reader = Utils.CreateFragmentReader("<Root>yyyy-MM-ddTHH:mm</Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Throws<XmlException>(() => reader.ReadContentAsDateTimeOffset());
        }

        [Fact]
        public static void ReadContentAsDateTimeOffset35()
        {
            var reader = Utils.CreateFragmentReader("<Root>   21<?a?>00<!-- Comment inbetween-->-02-29T23:59:59.9999999+13:60  </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Throws<XmlException>(() => reader.ReadContentAsDateTimeOffset());
        }

        [Fact]
        public static void ReadContentAsDateTimeOffset36()
        {
            var reader = Utils.CreateFragmentReader("<Root>3000-<?a?>02-29T<!-- Comment inbetween-->23:59:59.999999999999-13:60</Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Throws<XmlException>(() => reader.ReadContentAsDateTimeOffset());
        }

        [Fact]
        public static void ReadContentAsDateTimeOffset37()
        {
            var reader = Utils.CreateFragmentReader("<Root>2002-12-33</Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadContentAsDateTimeOffset38()
        {
            var reader = Utils.CreateFragmentReader("<Root  >2002-13-30  </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadContentAsDateTimeOffset39()
        {
            var reader = Utils.CreateFragmentReader("<Root>   200<!-- Comment inbetween-->2-<![CDATA[12]]>-3<?a?>0Z   </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(new DateTimeOffset(2002, 12, 30, 0, 0, 0, TimeSpan.FromHours(0)).ToString(), reader.ReadContentAs(typeof(DateTimeOffset), null).ToString());
        }

        [Fact]
        public static void ReadContentAsDateTimeOffset4()
        {
            var reader = Utils.CreateFragmentReader("<Root>  20<?a?>02-1<![CDATA[2]]>-3<!-- Comment inbetween-->0  </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(new DateTimeOffset(new DateTime(2002, 12, 30, 0, 0, 0, DateTimeKind.Local)), reader.ReadContentAsDateTimeOffset());
        }

        [Fact]
        public static void ReadContentAsDateTimeOffset40()
        {
            var reader = Utils.CreateFragmentReader("<Root><![CDATA[00]]>01-01-01<?a?>T00:00:0<!-- Comment inbetween-->0+00:00</Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(new DateTimeOffset(1, 1, 1, 0, 0, 0, TimeSpan.FromHours(0)).ToString(), reader.ReadContentAs(typeof(DateTimeOffset), null).ToString());
        }

        [Fact]
        public static void ReadContentAsDateTimeOffset41()
        {
            var reader = Utils.CreateFragmentReader("<Root><![CDATA[9]]>99<?a?>9-12-31T12:<!-- Comment inbetween-->5<![CDATA[9]]>:5<![CDATA[9]]>+14:00</Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(new DateTimeOffset(9999, 12, 31, 12, 59, 59, TimeSpan.FromHours(+14)).ToString(), reader.ReadContentAs(typeof(DateTimeOffset), null).ToString());
        }

        [Fact]
        public static void ReadContentAsDateTimeOffset42()
        {
            var reader = Utils.CreateFragmentReader("<Root>  9<![CDATA[9]]>99-1<?a?>2-31T1<!-- Comment inbetween-->2:59:59-10:60     </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(new DateTimeOffset(9999, 12, 31, 12, 59, 59, TimeSpan.FromHours(-11)).ToString(), reader.ReadContentAs(typeof(DateTimeOffset), null).ToString());
        }

        [Fact]
        public static void ReadContentAsDateTimeOffset43()
        {
            var reader = Utils.CreateFragmentReader("<Root>  20<!-- Comment inbetween-->05 </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(new DateTimeOffset(2005, 1, 1, 0, 0, 0, TimeZoneInfo.Local.GetUtcOffset(new DateTime(2005, 1, 1))).ToString(), reader.ReadContentAs(typeof(DateTimeOffset), null).ToString());
        }

        [Fact]
        public static void ReadContentAsDateTimeOffset44()
        {
            var reader = Utils.CreateFragmentReader("<Root> 9<![CDATA[9]]>9<!-- Comment inbetween-->9<?a?> </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(new DateTimeOffset(9999, 1, 1, 0, 0, 0, TimeZoneInfo.Local.GetUtcOffset(new DateTime(9999, 1, 1))).ToString(), reader.ReadContentAs(typeof(DateTimeOffset), null).ToString());
        }

        [Fact]
        public static void ReadContentAsDateTimeOffset45()
        {
            var reader = Utils.CreateFragmentReader("<Root> 0<?a?>0<!-- Comment inbetween-->01Z </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(new DateTimeOffset(1, 1, 1, 0, 0, 0, TimeSpan.FromHours(0)).ToString(), reader.ReadContentAs(typeof(DateTimeOffset), null).ToString());
        }

        [Fact]
        public static void ReadContentAsDateTimeOffset46()
        {
            var reader = Utils.CreateFragmentReader("<Root>99<!-- Comment inbetween-->9<![CDATA[9]]>Z<?a?></Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(new DateTimeOffset(9999, 1, 1, 0, 0, 0, TimeSpan.FromHours(0)).ToString(), reader.ReadContentAs(typeof(DateTimeOffset), null).ToString());
        }

        [Fact]
        public static void ReadContentAsDateTimeOffset47()
        {
            var reader = Utils.CreateFragmentReader("<Root>2<!-- Comment inbetween-->000-02-29T23:5<?a?>9:5<![CDATA[9]]>.999999<![CDATA[9]]>+13:60  </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(new DateTimeOffset(2000, 2, 29, 23, 59, 59, TimeSpan.FromHours(14)).ToString(), reader.ReadContentAs(typeof(DateTimeOffset), null).ToString());
        }

        [Fact]
        public static void ReadContentAsDateTimeOffset48()
        {
            var reader = Utils.CreateFragmentReader("<Root>    20<?a?>00-02-29T23:59:59.999999999999-<!-- Comment inbetween-->13:60</Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(new DateTimeOffset(2000, 3, 1, 0, 0, 0, TimeSpan.FromHours(-14)).ToString(), reader.ReadContentAs(typeof(DateTimeOffset), null).ToString());
        }

        [Fact]
        public static void ReadContentAsDateTimeOffset49()
        {
            var reader = Utils.CreateFragmentReader("<Root>   0001-01-01T00<?a?>:00:00<!-- Comment inbetween-->-1<!-- Comment inbetween-->3:<![CDATA[60]]>   </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(new DateTimeOffset(1, 1, 1, 0, 0, 0, TimeSpan.FromHours(-14)).ToString(), reader.ReadContentAs(typeof(DateTimeOffset), null).ToString());
        }

        [Fact]
        public static void ReadContentAsDateTimeOffset5()
        {
            var reader = Utils.CreateFragmentReader("<Root>  <![CDATA[2]]>00<?a?>2-1<!-- Comment inbetween-->2-30Z  </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(new DateTimeOffset(new DateTime(2002, 12, 30, 0, 0, 0, 0, DateTimeKind.Utc)), reader.ReadContentAsDateTimeOffset());
        }

        [Fact]
        public static void ReadContentAsDateTimeOffset50()
        {
            var reader = Utils.CreateFragmentReader("<Root>2<?a?>00<!-- Comment inbetween-->2-12-3<![CDATA[0]]></Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(new DateTimeOffset(2002, 12, 30, 0, 0, 0, TimeZoneInfo.Local.GetUtcOffset(new DateTime(2002, 12, 30))).ToString(), reader.ReadContentAs(typeof(DateTimeOffset), null).ToString());
        }

        [Fact]
        public static void ReadContentAsDateTimeOffset51()
        {
            var reader = Utils.CreateFragmentReader("<Root>  20<?9?>02-1<![CDATA[2]]>-3<!-- Comment inbetween-->0  </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadContentAsDateTimeOffset52()
        {
            var reader = Utils.CreateFragmentReader("<Root>9999-12-31T12:5<?a?>9:59+14:00:00</Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadContentAsDateTimeOffset53()
        {
            var reader = Utils.CreateFragmentReader("<Root>0<?a?></Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadContentAsDateTimeOffset54()
        {
            var reader = Utils.CreateFragmentReader("<Root>99<?a?>9Z</Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadContentAsDateTimeOffset55()
        {
            var reader = Utils.CreateFragmentReader("<Root><![CDATA[z<!-- Comment inbetween-->]]></Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadContentAsDateTimeOffset56()
        {
            var reader = Utils.CreateFragmentReader("<Root><![CDATA[Z<!-- Comment inbetween-->]]></Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadContentAsDateTimeOffset57()
        {
            var reader = Utils.CreateFragmentReader("<Root>21<!-- Comment inbetween-->00-02-29T23:59:59.9999999+13:60</Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadContentAsDateTimeOffset58()
        {
            var reader = Utils.CreateFragmentReader("<Root>3000-02-29T23:59:<!-- Comment inbetween-->9.999999999999-13:60</Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadContentAsDateTimeOffset59()
        {
            var reader = Utils.CreateFragmentReader("<Root>001-01-01T0<?a?>0:00:00<!-- Comment inbetween-->+00:00</Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadContentAsDateTimeOffset6()
        {
            var reader = Utils.CreateFragmentReader("<Root>  <!-- Comment inbetween-->0002-01-01T00:00:00+00:00  </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(new DateTimeOffset(new DateTime(2, 1, 1, 0, 0, 0, DateTimeKind.Utc)), reader.ReadContentAsDateTimeOffset());
        }

        [Fact]
        public static void ReadContentAsDateTimeOffset60()
        {
            var reader = Utils.CreateFragmentReader("<Root>0001-<![CDATA[0<!-- Comment inbetween-->1]]>-01T0<?a?>0:00:00</Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadContentAsDateTimeOffset7()
        {
            var reader = Utils.CreateFragmentReader("<Root>99<!-- Comment inbetween-->99-1<?a?>2-31T1<![CDATA[2]]>:59:59</Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(new DateTimeOffset(new DateTime(9999, 12, 31, 12, 59, 59, DateTimeKind.Local)), reader.ReadContentAsDateTimeOffset());
        }

        [Fact]
        public static void ReadContentAsDateTimeOffset8()
        {
            var reader = Utils.CreateFragmentReader("<Root>  999<!-- Comment inbetween-->9  </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(new DateTimeOffset(new DateTime(9999, 1, 1, 0, 0, 0, DateTimeKind.Local)), reader.ReadContentAsDateTimeOffset());
        }

        [Fact]
        public static void ReadContentAsDateTimeOffset9()
        {
            var reader = Utils.CreateFragmentReader("<Root>  <![CDATA[0]]>001Z  </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)), reader.ReadContentAsDateTimeOffset());
        }
    }
}
