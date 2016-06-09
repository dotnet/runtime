// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class BooleanElementContentTests
    {
        [Fact]
        public static void ReadElementContentAsBoolean1()
        {
            var reader = Utils.CreateFragmentReader("<Root>  True   </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(bool), null));
        }

        [Fact]
        public static void ReadElementContentAsBoolean10()
        {
            var reader = Utils.CreateFragmentReader("<Root>  True   </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Throws<XmlException>(() => reader.ReadContentAsBoolean());
        }

        [Fact]
        public static void ReadElementContentAsBoolean11()
        {
            var reader = Utils.CreateFragmentReader("<Root> <?a?><!-- Comment inbetween--><![CDATA[0]]> </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(false, reader.ReadElementContentAs(typeof(bool), null));
        }

        [Fact]
        public static void ReadElementContentAsBoolean12()
        {
            var reader = Utils.CreateFragmentReader("<Root a=' False '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Throws<XmlException>(() => reader.ReadContentAsBoolean());
        }

        [Fact]
        public static void ReadElementContentAsBoolean13()
        {
            var reader = Utils.CreateFragmentReader("<Root> <!-- Comment inbetween--><![CDATA[1]]><?a?> </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(true, reader.ReadElementContentAs(typeof(bool), null));
        }

        [Fact]
        public static void ReadElementContentAsBoolean14()
        {
            var reader = Utils.CreateFragmentReader("<Root> f<!-- Comment inbetween-->a<?a?>lse<!-- Comment inbetween--> </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(false, reader.ReadElementContentAs(typeof(bool), null));
        }

        [Fact]
        public static void ReadElementContentAsBoolean15()
        {
            var reader = Utils.CreateFragmentReader("<Root> t<!-- Comment inbetween-->ru<?a?>e </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(true, reader.ReadElementContentAs(typeof(bool), null));
        }

        [Fact]
        public static void ReadElementContentAsBoolean16()
        {
            var reader = Utils.CreateFragmentReader("<Root>  <?a?><!-- Comment inbetween-->0   </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(false, reader.ReadElementContentAs(typeof(bool), null));
        }

        [Fact]
        public static void ReadElementContentAsBoolean17()
        {
            var reader = Utils.CreateFragmentReader("<Root><![CDATA[1]]><?a?></Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(true, reader.ReadElementContentAs(typeof(bool), null));
        }

        [Fact]
        public static void ReadElementContentAsBoolean18()
        {
            var reader = Utils.CreateFragmentReader("<Root>  True   </Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(bool), null));
        }

        [Fact]
        public static void ReadElementContentAsBoolean2()
        {
            var reader = Utils.CreateFragmentReader("<Root a=' False '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(bool), null));
        }

        [Fact]
        public static void ReadElementContentAsBoolean3()
        {
            var reader = Utils.CreateFragmentReader("<Root> <?a?><!-- Comment inbetween--><![CDATA[0]]> </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(false, reader.ReadElementContentAsBoolean());
        }

        [Fact]
        public static void ReadElementContentAsBoolean4()
        {
            var reader = Utils.CreateFragmentReader("<Root> <!-- Comment inbetween--><![CDATA[1]]><?a?> </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(true, reader.ReadElementContentAsBoolean());
        }

        [Fact]
        public static void ReadElementContentAsBoolean5()
        {
            var reader = Utils.CreateFragmentReader("<Root> f<!-- Comment inbetween-->a<?a?>lse<!-- Comment inbetween--> </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(false, reader.ReadElementContentAsBoolean());
        }

        [Fact]
        public static void ReadElementContentAsBoolean6()
        {
            var reader = Utils.CreateFragmentReader("<Root> t<!-- Comment inbetween-->ru<?a?>e </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(true, reader.ReadElementContentAsBoolean());
        }

        [Fact]
        public static void ReadElementContentAsBoolean7()
        {
            var reader = Utils.CreateFragmentReader("<Root>  <?a?><!-- Comment inbetween-->0   </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(false, reader.ReadElementContentAsBoolean());
        }

        [Fact]
        public static void ReadElementContentAsBoolean8()
        {
            var reader = Utils.CreateFragmentReader("<Root><![CDATA[1]]><?a?></Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(true, reader.ReadElementContentAsBoolean());
        }

        [Fact]
        public static void ReadElementContentAsBoolean9()
        {
            var reader = Utils.CreateFragmentReader("<Root>  True   </Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAsBoolean());
        }
    }
}
