// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class ByteElementContentTests
    {
        [Fact]
        public static void ReadElementContentAsByte1()
        {
            var reader = Utils.CreateFragmentReader("<Root>  True   </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(byte), null));
        }

        [Fact]
        public static void ReadElementContentAsByte2()
        {
            var reader = Utils.CreateFragmentReader("<Root a=' False '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(byte), null));
        }

        [Fact]
        public static void ReadElementContentAsByte3()
        {
            var reader = Utils.CreateFragmentReader("<Root> <![CDATA[0]]><?a?><!-- Comment inbetween--> </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(byte.MinValue, reader.ReadElementContentAs(typeof(byte), null));
        }

        [Fact]
        public static void ReadElementContentAsByte4()
        {
            var reader = Utils.CreateFragmentReader("<Root> <?a?><![CDATA[2]]>5<!-- Comment inbetween-->5 </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(byte.MaxValue, reader.ReadElementContentAs(typeof(byte), null));
        }

        [Fact]
        public static void ReadElementContentAsByte5()
        {
            var reader = Utils.CreateFragmentReader("<Root>  <![CDATA[false]]><!-- Comment inbetween-->  <?a?></Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(byte), null));
        }

        [Fact]
        public static void ReadElementContentAsByte6()
        {
            var reader = Utils.CreateFragmentReader("<Root><!-- Comment inbetween--><![CDATA[true]]></Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(byte), null));
        }

        [Fact]
        public static void ReadElementContentAsByte7()
        {
            var reader = Utils.CreateFragmentReader("<Root>  <!-- Comment inbetween--><![CDATA[0]]>   </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(byte.MinValue, reader.ReadElementContentAs(typeof(byte), null));
        }

        [Fact]
        public static void ReadElementContentAsByte8()
        {
            var reader = Utils.CreateFragmentReader("<Root>  2<!-- Comment inbetween--><![CDATA[5]]>5<?a?>   </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal(byte.MaxValue, reader.ReadElementContentAs(typeof(byte), null));
        }

        [Fact]
        public static void ReadElementContentAsByte9()
        {
            var reader = Utils.CreateFragmentReader("<Root>  True   </Root>");
            reader.PositionOnElement("Root");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(byte), null));
        }
    }
}
