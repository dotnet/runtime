// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class IntegerTests
    {
        [Fact]
        public static void ReadContentAsInt1()
        {
            var reader = Utils.CreateFragmentReader(@"<doc a='b'>9999</doc>");
            reader.PositionOnElementNonEmptyNoDoctype("doc");
            reader.Read();
            Assert.Equal(new DateTime(9999, 1, 1, 0, 0, 0), reader.ReadContentAs(typeof(DateTime), null));
        }

        [Fact]
        public static void ReadContentAsInt10()
        {
            var reader = Utils.CreateFragmentReader("<Root> -<!-- Comment inbetween-->000<?a?>455 </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(-455, reader.ReadContentAsInt());
        }

        [Fact]
        public static void ReadContentAsInt11()
        {
            var reader = Utils.CreateFragmentReader("<Root>  -<![CDATA[0]]>0<!-- Comment inbetween-->5<?a?>  </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(-5, reader.ReadContentAs(typeof(int), null));
        }

        [Fact]
        public static void ReadContentAsInt12()
        {
            var reader = Utils.CreateFragmentReader("<Root>  <!-- Comment inbetween-->0<?a?>00<![CDATA[1]]></Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(1, reader.ReadContentAs(typeof(int), null));
        }

        [Fact]
        public static void ReadContentAsInt13()
        {
            var reader = Utils.CreateFragmentReader("<Root><![CDATA[0]]> <!-- Comment inbetween--> </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(0, reader.ReadContentAs(typeof(int), null));
        }

        [Fact]
        public static void ReadContentAsInt14()
        {
            var reader = Utils.CreateFragmentReader("<Root> 9<![CDATA[9]]>99<?a?>9<!-- Comment inbetween--><![CDATA[9]]> </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(999999, reader.ReadContentAs(typeof(int), null));
        }

        [Fact]
        public static void ReadContentAsInt15()
        {
            var reader = Utils.CreateFragmentReader("<Root>-4<!-- Comment inbetween-->4</Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(-44, reader.ReadContentAs(typeof(int), null));
        }

        [Fact]
        public static void ReadContentAsInt16()
        {
            var reader = Utils.CreateFragmentReader("<Root> -<!-- Comment inbetween-->000<?a?>455 </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(-455, reader.ReadContentAs(typeof(int), null));
        }

        [Fact]
        public static void ReadContentAsInt17()
        {
            var reader = Utils.CreateFragmentReader(@"<doc a='b'>9999</doc>");
            reader.PositionOnElementNonEmptyNoDoctype("doc");
            reader.Read();
            Assert.Equal(new DateTimeOffset(9999, 1, 1, 0, 0, 0, TimeZoneInfo.Local.GetUtcOffset(new DateTime(9999, 1, 1))).ToString(), reader.ReadContentAs(typeof(DateTimeOffset), null).ToString());
        }

        [Fact]
        public static void ReadContentAsInt18()
        {
            var reader = Utils.CreateFragmentReader(@"<doc a='b'>0001z</doc>");
            reader.PositionOnElementNonEmptyNoDoctype("doc");
            reader.Read();
            Assert.Equal(new DateTimeOffset(1, 1, 1, 0, 0, 0, TimeSpan.FromHours(0)).ToString(), reader.ReadContentAs(typeof(DateTimeOffset), null).ToString());
        }

        [Fact]
        public static void ReadContentAsInt2()
        {
            var reader = Utils.CreateFragmentReader(@"<doc a='b'>0001z</doc>");
            reader.PositionOnElementNonEmptyNoDoctype("doc");
            reader.Read();
            Assert.Equal(new DateTime(1, 1, 1, 0, 0, 0, 0).Add(new TimeSpan(0, 0, 0)), reader.ReadContentAs(typeof(DateTime), null));
        }

        [Fact]
        public static void ReadContentAsInt3()
        {
            var reader = Utils.CreateFragmentReader(@"<doc a='b'>0001z</doc>");
            reader.PositionOnElementNonEmptyNoDoctype("doc");
            reader.Read();
            Assert.Equal(new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)), reader.ReadContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadContentAsInt4()
        {
            var reader = Utils.CreateFragmentReader(@"<doc a='b'>9999</doc>");
            reader.PositionOnElementNonEmptyNoDoctype("doc");
            reader.Read();
            Assert.Equal(new DateTimeOffset(new DateTime(9999, 1, 1, 0, 0, 0, DateTimeKind.Local)), reader.ReadContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadContentAsInt5()
        {
            var reader = Utils.CreateFragmentReader("<Root>  -<![CDATA[0]]>0<!-- Comment inbetween-->5<?a?>  </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(-5, reader.ReadContentAsInt());
        }

        [Fact]
        public static void ReadContentAsInt6()
        {
            var reader = Utils.CreateFragmentReader("<Root>  <!-- Comment inbetween-->0<?a?>00<![CDATA[1]]></Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(1, reader.ReadContentAsInt());
        }

        [Fact]
        public static void ReadContentAsInt7()
        {
            var reader = Utils.CreateFragmentReader("<Root><![CDATA[0]]> <!-- Comment inbetween--> </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(0, reader.ReadContentAsInt());
        }

        [Fact]
        public static void ReadContentAsInt8()
        {
            var reader = Utils.CreateFragmentReader("<Root> 9<![CDATA[9]]>99<?a?>9<!-- Comment inbetween--><![CDATA[9]]> </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(999999, reader.ReadContentAsInt());
        }

        [Fact]
        public static void ReadContentAsInt9()
        {
            var reader = Utils.CreateFragmentReader("<Root>-4<!-- Comment inbetween-->4</Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(-44, reader.ReadContentAsInt());
        }
    }
}
