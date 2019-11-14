// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class IntegerElementContentTests
    {
        [Fact]
        public static void ReadElementContentAsInt1()
        {
            var reader = Utils.CreateFragmentReader(@"<doc a='b'>9999</doc>");
            reader.PositionOnElementNonEmptyNoDoctype("doc");
            Assert.Equal(new DateTime(9999, 1, 1, 0, 0, 0), reader.ReadElementContentAs(typeof(DateTime), null));
        }

        [Fact]
        public static void ReadElementContentAsInt10()
        {
            var reader = Utils.CreateFragmentReader("<Root>-4<!-- Comment inbetween-->4</Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(-44, reader.ReadElementContentAsInt());
        }

        [Fact]
        public static void ReadElementContentAsInt11()
        {
            var reader = Utils.CreateFragmentReader("<Root> -<!-- Comment inbetween-->000<?a?>455 </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(-455, reader.ReadElementContentAsInt());
        }

        [Fact]
        public static void ReadElementContentAsInt12()
        {
            var reader = Utils.CreateFragmentReader("<Root> -45.5 </Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAsInt());
        }

        [Fact]
        public static void ReadElementContentAsInt13()
        {
            var reader = Utils.CreateFragmentReader("<Root> -45.5 </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Throws<XmlException>(() => reader.ReadContentAsInt());
        }

        [Fact]
        public static void ReadElementContentAsInt14()
        {
            var reader = Utils.CreateFragmentReader("<Root a=' 4.678'/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Throws<XmlException>(() => reader.ReadContentAsInt());
        }

        [Fact]
        public static void ReadElementContentAsInt15()
        {
            var reader = Utils.CreateFragmentReader("<Root>  -<![CDATA[0]]>0<!-- Comment inbetween-->5<?a?>  </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(-5, reader.ReadElementContentAs(typeof(int), null));
        }

        [Fact]
        public static void ReadElementContentAsInt16()
        {
            var reader = Utils.CreateFragmentReader("<Root>  <!-- Comment inbetween-->0<?a?>00<![CDATA[1]]></Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(1, reader.ReadElementContentAs(typeof(int), null));
        }

        [Fact]
        public static void ReadElementContentAsInt17()
        {
            var reader = Utils.CreateFragmentReader("<Root><![CDATA[0]]> <!-- Comment inbetween--> </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(0, reader.ReadElementContentAs(typeof(int), null));
        }

        [Fact]
        public static void ReadElementContentAsInt18()
        {
            var reader = Utils.CreateFragmentReader("<Root> 9<![CDATA[9]]>99<?a?>9<!-- Comment inbetween--><![CDATA[9]]> </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(999999, reader.ReadElementContentAs(typeof(int), null));
        }

        [Fact]
        public static void ReadElementContentAsInt19()
        {
            var reader = Utils.CreateFragmentReader("<Root>-4<!-- Comment inbetween-->4</Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(-44, reader.ReadElementContentAs(typeof(int), null));
        }

        [Fact]
        public static void ReadElementContentAsInt2()
        {
            var reader = Utils.CreateFragmentReader(@"<doc a='b'>0001z</doc>");
            reader.PositionOnElementNonEmptyNoDoctype("doc");
            Assert.Equal(new DateTime(1, 1, 1, 0, 0, 0, 0).Add(new TimeSpan(0, 0, 0)), reader.ReadElementContentAs(typeof(DateTime), null));
        }

        [Fact]
        public static void ReadElementContentAsInt20()
        {
            var reader = Utils.CreateFragmentReader("<Root> -<!-- Comment inbetween-->000<?a?>455 </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(-455, reader.ReadElementContentAs(typeof(int), null));
        }

        [Fact]
        public static void ReadElementContentAsInt21()
        {
            var reader = Utils.CreateFragmentReader("<Root> -45.5 </Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(int), null));
        }

        [Fact]
        public static void ReadElementContentAsInt22()
        {
            var reader = Utils.CreateFragmentReader("<Root> -45.5 </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(int), null));
        }

        [Fact]
        public static void ReadElementContentAsInt23()
        {
            var reader = Utils.CreateFragmentReader(@"<doc a='b'>9999</doc>");
            reader.PositionOnElementNonEmptyNoDoctype("doc");
            Assert.Equal(new DateTimeOffset(9999, 1, 1, 0, 0, 0, TimeZoneInfo.Local.GetUtcOffset(new DateTime(9999, 1, 1))).ToString(), reader.ReadElementContentAs(typeof(DateTimeOffset), null).ToString());
        }

        [Fact]
        public static void ReadElementContentAsInt24()
        {
            var reader = Utils.CreateFragmentReader(@"<doc a='b'>0001z</doc>");
            reader.PositionOnElementNonEmptyNoDoctype("doc");
            Assert.Equal(new DateTimeOffset(1, 1, 1, 0, 0, 0, TimeSpan.FromHours(0)).ToString(), reader.ReadElementContentAs(typeof(DateTimeOffset), null).ToString());
        }

        [Fact]
        public static void ReadElementContentAsInt3()
        {
            var reader = Utils.CreateFragmentReader(@"<doc a='b'>0001z</doc>");
            reader.PositionOnElementNonEmptyNoDoctype("doc");
            Assert.Equal(new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)), reader.ReadElementContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadElementContentAsInt4()
        {
            var reader = Utils.CreateFragmentReader(@"<doc a='b'>9999</doc>");
            reader.PositionOnElementNonEmptyNoDoctype("doc");
            Assert.Equal(new DateTimeOffset(new DateTime(9999, 1, 1, 0, 0, 0, DateTimeKind.Local)), reader.ReadElementContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadElementContentAsInt5()
        {
            var reader = Utils.CreateFragmentReader("<Root a=' 4.678'/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(int), null));
        }

        [Fact]
        public static void ReadElementContentAsInt6()
        {
            var reader = Utils.CreateFragmentReader("<Root>  -<![CDATA[0]]>0<!-- Comment inbetween-->5<?a?>  </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(-5, reader.ReadElementContentAsInt());
        }

        [Fact]
        public static void ReadElementContentAsInt7()
        {
            var reader = Utils.CreateFragmentReader("<Root>  <!-- Comment inbetween-->0<?a?>00<![CDATA[1]]></Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(1, reader.ReadElementContentAsInt());
        }

        [Fact]
        public static void ReadElementContentAsInt8()
        {
            var reader = Utils.CreateFragmentReader("<Root><![CDATA[0]]> <!-- Comment inbetween--> </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(0, reader.ReadElementContentAsInt());
        }

        [Fact]
        public static void ReadElementContentAsInt9()
        {
            var reader = Utils.CreateFragmentReader("<Root> 9<![CDATA[9]]>99<?a?>9<!-- Comment inbetween--><![CDATA[9]]> </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(999999, reader.ReadElementContentAsInt());
        }
    }
}
