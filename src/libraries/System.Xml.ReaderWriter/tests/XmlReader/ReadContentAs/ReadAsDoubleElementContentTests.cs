// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Xml;

namespace XMLTests.ReaderWriter.ReadContentTests
{
    public class DoubleElementContentTests
    {
        [Fact]
        public static void ReadElementContentAsDouble1()
        {
            var reader = Utils.CreateFragmentReader("<Root a=' b '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(Double), null));
        }

        [Fact]
        public static void ReadElementContentAsDouble10()
        {
            var reader = Utils.CreateFragmentReader("<Root> true </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Throws<XmlException>(() => reader.ReadContentAsDouble());
        }

        [Fact]
        public static void ReadElementContentAsDouble11()
        {
            var reader = Utils.CreateFragmentReader("<Root a=' b '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Throws<XmlException>(() => reader.ReadContentAsDouble());
        }

        [Fact]
        public static void ReadElementContentAsDouble12()
        {
            var reader = Utils.CreateFragmentReader("<Root> <?a?>0<![CDATA[0]]>0<!-- Comment inbetween-->1</Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(1d, reader.ReadElementContentAs(typeof(Double), null));
        }

        [Fact]
        public static void ReadElementContentAsDouble13()
        {
            var reader = Utils.CreateFragmentReader("<Root> <![CDATA[0]]> </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(0d, reader.ReadElementContentAs(typeof(Double), null));
        }

        [Fact]
        public static void ReadElementContentAsDouble14()
        {
            var reader = Utils.CreateFragmentReader("<Root><![CDATA[9]]>9<?a?>9<!-- Comment inbetween-->9.<![CDATA[9]]></Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(9999.9d, reader.ReadElementContentAs(typeof(Double), null));
        }

        [Fact]
        public static void ReadElementContentAsDouble15()
        {
            var reader = Utils.CreateFragmentReader("<Root>  <!-- Comment inbetween-->44.<?a?>44  </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(44.44d, reader.ReadElementContentAs(typeof(Double), null));
        }

        [Fact]
        public static void ReadElementContentAsDouble16()
        {
            var reader = Utils.CreateFragmentReader("<Root> 44<?a?>.55<!-- Comment inbetween--> </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(44.55d, reader.ReadElementContentAs(typeof(Double), null));
        }

        [Fact]
        public static void ReadElementContentAsDouble17()
        {
            var reader = Utils.CreateFragmentReader("<Root> true </Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(Double), null));
        }

        [Fact]
        public static void ReadElementContentAsDouble18()
        {
            var reader = Utils.CreateFragmentReader("<Root> true </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(Double), null));
        }

        [Fact]
        public static void ReadElementContentAsDouble2()
        {
            var reader = Utils.CreateFragmentReader("<Root> <![CDATA[-0]]><?a?>0<!-- Comment inbetween-->5.5 </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(-5.5d, reader.ReadElementContentAsDouble());
        }

        [Fact]
        public static void ReadElementContentAsDouble3()
        {
            var reader = Utils.CreateFragmentReader("<Root> <?a?>0<![CDATA[0]]>0<!-- Comment inbetween-->1</Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(1d, reader.ReadElementContentAsDouble());
        }

        [Fact]
        public static void ReadElementContentAsDouble4()
        {
            var reader = Utils.CreateFragmentReader("<Root> <![CDATA[0]]> </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(0d, reader.ReadElementContentAsDouble());
        }

        [Fact]
        public static void ReadElementContentAsDouble5()
        {
            var reader = Utils.CreateFragmentReader("<Root><![CDATA[9]]>9<?a?>9<!-- Comment inbetween-->9.<![CDATA[9]]></Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(9999.9d, reader.ReadElementContentAsDouble());
        }

        [Fact]
        public static void ReadElementContentAsDouble6()
        {
            var reader = Utils.CreateFragmentReader("<Root>  <!-- Comment inbetween-->44.<?a?>44  </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(44.44d, reader.ReadElementContentAsDouble());
        }

        [Fact]
        public static void ReadElementContentAsDouble7()
        {
            var reader = Utils.CreateFragmentReader("<Root> 44<?a?>.55<!-- Comment inbetween--> </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(44.55d, reader.ReadElementContentAsDouble());
        }

        [Fact]
        public static void ReadElementContentAsDouble8()
        {
            var reader = Utils.CreateFragmentReader("<Root> true </Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAsDouble());
        }

        [Fact]
        public static void ReadElementContentAsDouble9()
        {
            var reader = Utils.CreateFragmentReader("<Root> <![CDATA[-0]]><?a?>0<!-- Comment inbetween-->5.5 </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(-5.5d, reader.ReadElementContentAs(typeof(Double), null));
        }
    }
}