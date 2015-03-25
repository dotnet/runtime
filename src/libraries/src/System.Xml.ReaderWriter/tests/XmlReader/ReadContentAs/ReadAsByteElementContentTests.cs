// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Xml;

namespace XMLTests.ReaderWriter.ReadContentTests
{
    public class ByteElementContentTests
    {
        [Fact]
        public static void ReadElementContentAsByte1()
        {
            var reader = Utils.CreateFragmentReader("<Root>  True   </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(Byte), null));
        }

        [Fact]
        public static void ReadElementContentAsByte2()
        {
            var reader = Utils.CreateFragmentReader("<Root a=' False '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(Byte), null));
        }

        [Fact]
        public static void ReadElementContentAsByte3()
        {
            var reader = Utils.CreateFragmentReader("<Root> <![CDATA[0]]><?a?><!-- Comment inbetween--> </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(Byte.MinValue, reader.ReadElementContentAs(typeof(Byte), null));
        }

        [Fact]
        public static void ReadElementContentAsByte4()
        {
            var reader = Utils.CreateFragmentReader("<Root> <?a?><![CDATA[2]]>5<!-- Comment inbetween-->5 </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(Byte.MaxValue, reader.ReadElementContentAs(typeof(Byte), null));
        }

        [Fact]
        public static void ReadElementContentAsByte5()
        {
            var reader = Utils.CreateFragmentReader("<Root>  <![CDATA[false]]><!-- Comment inbetween-->  <?a?></Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(Byte), null));
        }

        [Fact]
        public static void ReadElementContentAsByte6()
        {
            var reader = Utils.CreateFragmentReader("<Root><!-- Comment inbetween--><![CDATA[true]]></Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(Byte), null));
        }

        [Fact]
        public static void ReadElementContentAsByte7()
        {
            var reader = Utils.CreateFragmentReader("<Root>  <!-- Comment inbetween--><![CDATA[0]]>   </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(Byte.MinValue, reader.ReadElementContentAs(typeof(Byte), null));
        }

        [Fact]
        public static void ReadElementContentAsByte8()
        {
            var reader = Utils.CreateFragmentReader("<Root>  2<!-- Comment inbetween--><![CDATA[5]]>5<?a?>   </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(Byte.MaxValue, reader.ReadElementContentAs(typeof(Byte), null));
        }

        [Fact]
        public static void ReadElementContentAsByte9()
        {
            var reader = Utils.CreateFragmentReader("<Root>  True   </Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(Byte), null));
        }
    }
}