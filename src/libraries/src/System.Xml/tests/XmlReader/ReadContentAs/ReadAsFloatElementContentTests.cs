// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class FloatElementContentTests
    {
        [Fact]
        public static void ReadElementContentAsFloat1()
        {
            var reader = Utils.CreateFragmentReader("<Root a=' b '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(float), null));
        }

        [Fact]
        public static void ReadElementContentAsFloat10()
        {
            var reader = Utils.CreateFragmentReader("<Root> -0<!-- Comment inbetween-->05.145<?a?><![CDATA[6]]> </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(-5.1456F, reader.ReadElementContentAs(typeof(float), null));
        }

        [Fact]
        public static void ReadElementContentAsFloat11()
        {
            var reader = Utils.CreateFragmentReader("<Root a=' b '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Throws<XmlException>(() => reader.ReadContentAsFloat());
        }

        [Fact]
        public static void ReadElementContentAsFloat12()
        {
            var reader = Utils.CreateFragmentReader("<Root> <?a?>00<!-- Comment inbetween-->9<![CDATA[9]]>.<![CDATA[9]]>99999</Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(99.999999F, reader.ReadElementContentAs(typeof(float), null));
        }

        [Fact]
        public static void ReadElementContentAsFloat13()
        {
            var reader = Utils.CreateFragmentReader("<Root>-5<![CDATA[6]]>.4<?a?>444<!-- Comment inbetween-->455<![CDATA[6]]> </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(-56.44444556F, reader.ReadElementContentAs(typeof(float), null));
        }

        [Fact]
        public static void ReadElementContentAsFloat14()
        {
            var reader = Utils.CreateFragmentReader("<Root>  5<![CDATA[6]]>.455555<!-- Comment inbetween--><![CDATA[6]]>44<?a?>  </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(56.455555644F, reader.ReadElementContentAs(typeof(float), null));
        }

        [Fact]
        public static void ReadElementContentAsFloat15()
        {
            var reader = Utils.CreateFragmentReader("<Root> -000123<?a?>45<!-- Comment inbetween--><![CDATA[6]]> </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(-123456F, reader.ReadElementContentAs(typeof(float), null));
        }

        [Fact]
        public static void ReadElementContentAsFloat16()
        {
            var reader = Utils.CreateFragmentReader("<Root> <![CDATA[9]]>999<!-- Comment inbetween-->9.444<?a?>5<![CDATA[6]]> </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(99999.44456F, reader.ReadElementContentAs(typeof(float), null));
        }

        [Fact]
        public static void ReadElementContentAsFloat17()
        {
            var reader = Utils.CreateFragmentReader("<Root> true </Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(float), null));
        }

        [Fact]
        public static void ReadElementContentAsFloat18()
        {
            var reader = Utils.CreateFragmentReader("<Root> true </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(float), null));
        }

        [Fact]
        public static void ReadElementContentAsFloat2()
        {
            var reader = Utils.CreateFragmentReader("<Root> -0<!-- Comment inbetween-->05.145<?a?><![CDATA[6]]> </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(-5.1456F, reader.ReadElementContentAsFloat());
        }

        [Fact]
        public static void ReadElementContentAsFloat3()
        {
            var reader = Utils.CreateFragmentReader("<Root> <?a?>00<!-- Comment inbetween-->9<![CDATA[9]]>.<![CDATA[9]]>99999</Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(99.999999F, reader.ReadElementContentAsFloat());
        }

        [Fact]
        public static void ReadElementContentAsFloat4()
        {
            var reader = Utils.CreateFragmentReader("<Root>-5<![CDATA[6]]>.4<?a?>444<!-- Comment inbetween-->455<![CDATA[6]]> </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(-56.44444556F, reader.ReadElementContentAsFloat());
        }

        [Fact]
        public static void ReadElementContentAsFloat5()
        {
            var reader = Utils.CreateFragmentReader("<Root>  5<![CDATA[6]]>.455555<!-- Comment inbetween--><![CDATA[6]]>44<?a?>  </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(56.455555644F, reader.ReadElementContentAsFloat());
        }

        [Fact]
        public static void ReadElementContentAsFloat6()
        {
            var reader = Utils.CreateFragmentReader("<Root> -000123<?a?>45<!-- Comment inbetween--><![CDATA[6]]> </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(-123456F, reader.ReadElementContentAsFloat());
        }

        [Fact]
        public static void ReadElementContentAsFloat7()
        {
            var reader = Utils.CreateFragmentReader("<Root> <![CDATA[9]]>999<!-- Comment inbetween-->9.444<?a?>5<![CDATA[6]]> </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(99999.44456F, reader.ReadElementContentAsFloat());
        }

        [Fact]
        public static void ReadElementContentAsFloat8()
        {
            var reader = Utils.CreateFragmentReader("<Root> true </Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAsFloat());
        }

        [Fact]
        public static void ReadElementContentAsFloat9()
        {
            var reader = Utils.CreateFragmentReader("<Root> true </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Throws<XmlException>(() => reader.ReadContentAsFloat());
        }
    }
}
