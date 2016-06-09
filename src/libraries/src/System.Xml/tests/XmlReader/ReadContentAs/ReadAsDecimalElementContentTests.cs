// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class DecimalElementContentTests
    {
        [Fact]
        public static void ReadElementContentAsDecimal1()
        {
            var reader = Utils.CreateFragmentReader("<Root a='b'/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(Decimal), null));
        }

        [Fact]
        public static void ReadElementContentAsDecimal10()
        {
            var reader = Utils.CreateFragmentReader("<Root a='b'/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Throws<XmlException>(() => reader.ReadContentAsDecimal());
        }

        [Fact]
        public static void ReadElementContentAsDecimal11()
        {
            var reader = Utils.CreateFragmentReader("<Root>true </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(Decimal), null));
        }

        [Fact]
        public static void ReadElementContentAsDecimal12()
        {
            var reader = Utils.CreateFragmentReader("<Root>  -0<?a?>0<!-- Comment inbetween-->5.<![CDATA[5]]> </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(-5.5m, reader.ReadElementContentAs(typeof(Decimal), null));
        }

        [Fact]
        public static void ReadElementContentAsDecimal13()
        {
            var reader = Utils.CreateFragmentReader("<Root>  00<!-- Comment inbetween-->01<?a?></Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(1m, reader.ReadElementContentAs(typeof(Decimal), null));
        }

        [Fact]
        public static void ReadElementContentAsDecimal14()
        {
            var reader = Utils.CreateFragmentReader("<Root> <?a?>0  </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(0m, reader.ReadElementContentAs(typeof(Decimal), null));
        }

        [Fact]
        public static void ReadElementContentAsDecimal15()
        {
            var reader = Utils.CreateFragmentReader("<Root> 9<![CDATA[9]]>99.9 </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(9999.9m, reader.ReadElementContentAs(typeof(Decimal), null));
        }

        [Fact]
        public static void ReadElementContentAsDecimal16()
        {
            var reader = Utils.CreateFragmentReader("<Root>44<?a?>.44</Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(44.44m, reader.ReadElementContentAs(typeof(Decimal), null));
        }

        [Fact]
        public static void ReadElementContentAsDecimal17()
        {
            var reader = Utils.CreateFragmentReader("<Root>  4<?a?>4.5<!-- Comment inbetween-->5  </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(44.55m, reader.ReadElementContentAs(typeof(Decimal), null));
        }

        [Fact]
        public static void ReadElementContentAsDecimal18()
        {
            var reader = Utils.CreateFragmentReader("<Root>true </Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(Decimal), null));
        }

        [Fact]
        public static void ReadElementContentAsDecimal2()
        {
            var reader = Utils.CreateFragmentReader("<Root>  -0<?a?>0<!-- Comment inbetween-->5.<![CDATA[5]]> </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(-5.5m, reader.ReadElementContentAsDecimal());
        }

        [Fact]
        public static void ReadElementContentAsDecimal3()
        {
            var reader = Utils.CreateFragmentReader("<Root>  00<!-- Comment inbetween-->01<?a?></Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(1m, reader.ReadElementContentAsDecimal());
        }

        [Fact]
        public static void ReadElementContentAsDecimal4()
        {
            var reader = Utils.CreateFragmentReader("<Root> <?a?>0  </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(0m, reader.ReadElementContentAsDecimal());
        }

        [Fact]
        public static void ReadElementContentAsDecimal5()
        {
            var reader = Utils.CreateFragmentReader("<Root> 9<![CDATA[9]]>99.9 </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(9999.9m, reader.ReadElementContentAsDecimal());
        }

        [Fact]
        public static void ReadElementContentAsDecimal6()
        {
            var reader = Utils.CreateFragmentReader("<Root>44<?a?>.44</Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(44.44m, reader.ReadElementContentAsDecimal());
        }

        [Fact]
        public static void ReadElementContentAsDecimal7()
        {
            var reader = Utils.CreateFragmentReader("<Root>  4<?a?>4.5<!-- Comment inbetween-->5  </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(44.55m, reader.ReadElementContentAsDecimal());
        }

        [Fact]
        public static void ReadElementContentAsDecimal8()
        {
            var reader = Utils.CreateFragmentReader("<Root>true </Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAsDecimal());
        }

        [Fact]
        public static void ReadElementContentAsDecimal9()
        {
            var reader = Utils.CreateFragmentReader("<Root>true </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Throws<XmlException>(() => reader.ReadContentAsDecimal());
        }
    }
}
