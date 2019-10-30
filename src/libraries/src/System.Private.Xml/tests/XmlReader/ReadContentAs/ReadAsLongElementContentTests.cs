// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class LongElementContentTests
    {
        [Fact]
        public static void ReadElementContentAsLong1()
        {
            var reader = Utils.CreateFragmentReader("<Root a=' 4.678 '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(long), null));
        }

        [Fact]
        public static void ReadElementContentAsLong10()
        {
            var reader = Utils.CreateFragmentReader("<Root a=' 4.678 '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Throws<XmlException>(() => reader.ReadContentAsLong());
        }

        [Fact]
        public static void ReadElementContentAsLong11()
        {
            var reader = Utils.CreateFragmentReader("<Root>  <![CDATA[0]]>0<?a?>0<!-- Comment inbetween-->1<!-- Comment inbetween--></Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(1L, reader.ReadElementContentAs(typeof(long), null));
        }

        [Fact]
        public static void ReadElementContentAsLong12()
        {
            var reader = Utils.CreateFragmentReader("<Root><?a?><![CDATA[0]]><!-- Comment inbetween-->  </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(0L, reader.ReadElementContentAs(typeof(long), null));
        }

        [Fact]
        public static void ReadElementContentAsLong13()
        {
            var reader = Utils.CreateFragmentReader("<Root>99<?a?>99<!-- Comment inbetween-->99</Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(999999L, reader.ReadElementContentAs(typeof(long), null));
        }

        [Fact]
        public static void ReadElementContentAsLong14()
        {
            var reader = Utils.CreateFragmentReader("<Root> -<?a?>4<!-- Comment inbetween-->4 </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(-44L, reader.ReadElementContentAs(typeof(long), null));
        }

        [Fact]
        public static void ReadElementContentAsLong15()
        {
            var reader = Utils.CreateFragmentReader("<Root> -<?a?><!-- Comment inbetween-->00045<![CDATA[5]]> </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(-455L, reader.ReadElementContentAs(typeof(long), null));
        }

        [Fact]
        public static void ReadElementContentAsLong16()
        {
            var reader = Utils.CreateFragmentReader("<Root> -45.5 </Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(long), null));
        }

        [Fact]
        public static void ReadElementContentAsLong17()
        {
            var reader = Utils.CreateFragmentReader("<Root> -45.5 </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(long), null));
        }

        [Fact]
        public static void ReadElementContentAsLong18()
        {
            var reader = Utils.CreateFragmentReader("<Root>  <?a?>-0<!-- Comment inbetween-->0<![CDATA[5]]>  </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(-5L, reader.ReadElementContentAs(typeof(long), null));
        }

        [Fact]
        public static void ReadElementContentAsLong2()
        {
            var reader = Utils.CreateFragmentReader("<Root>  <?a?>-0<!-- Comment inbetween-->0<![CDATA[5]]>  </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(-5L, reader.ReadElementContentAsLong());
        }

        [Fact]
        public static void ReadElementContentAsLong3()
        {
            var reader = Utils.CreateFragmentReader("<Root>  <![CDATA[0]]>0<?a?>0<!-- Comment inbetween-->1<!-- Comment inbetween--></Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(1L, reader.ReadElementContentAsLong());
        }

        [Fact]
        public static void ReadElementContentAsLong4()
        {
            var reader = Utils.CreateFragmentReader("<Root><?a?><![CDATA[0]]><!-- Comment inbetween-->  </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(0L, reader.ReadElementContentAsLong());
        }

        [Fact]
        public static void ReadElementContentAsLong5()
        {
            var reader = Utils.CreateFragmentReader("<Root>99<?a?>99<!-- Comment inbetween-->99</Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(999999L, reader.ReadElementContentAsLong());
        }

        [Fact]
        public static void ReadElementContentAsLong6()
        {
            var reader = Utils.CreateFragmentReader("<Root> -<?a?>4<!-- Comment inbetween-->4 </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(-44L, reader.ReadElementContentAsLong());
        }

        [Fact]
        public static void ReadElementContentAsLong7()
        {
            var reader = Utils.CreateFragmentReader("<Root> -<?a?><!-- Comment inbetween-->00045<![CDATA[5]]> </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(-455L, reader.ReadElementContentAsLong());
        }

        [Fact]
        public static void ReadElementContentAsLong8()
        {
            var reader = Utils.CreateFragmentReader("<Root> -45.5 </Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAsLong());
        }

        [Fact]
        public static void ReadElementContentAsLong9()
        {
            var reader = Utils.CreateFragmentReader("<Root> -45.5 </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Throws<XmlException>(() => reader.ReadContentAsLong());
        }
    }
}
