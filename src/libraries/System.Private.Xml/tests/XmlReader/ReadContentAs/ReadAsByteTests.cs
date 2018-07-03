// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class ByteTests
    {
        [Fact]
        public static void ReadContentAsByte1()
        {
            var reader = Utils.CreateFragmentReader("<Root>  2<!-- Comment inbetween--><![CDATA[5]]>5<?a?>   </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(byte.MaxValue, reader.ReadContentAs(typeof(byte), null));
        }

        [Fact]
        public static void ReadContentAsByte2()
        {
            var reader = Utils.CreateFragmentReader("<Root> <![CDATA[0]]><?a?><!-- Comment inbetween--> </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(byte.MinValue, reader.ReadContentAs(typeof(byte), null));
        }

        [Fact]
        public static void ReadContentAsByte3()
        {
            var reader = Utils.CreateFragmentReader("<Root> <?a?><![CDATA[2]]>5<!-- Comment inbetween-->5 </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(byte.MaxValue, reader.ReadContentAs(typeof(byte), null));
        }

        [Fact]
        public static void ReadContentAsByte4()
        {
            var reader = Utils.CreateFragmentReader("<Root>  <![CDATA[false]]><!-- Comment inbetween-->  <?a?></Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(byte), null));
        }

        [Fact]
        public static void ReadContentAsByte5()
        {
            var reader = Utils.CreateFragmentReader("<Root><!-- Comment inbetween--><![CDATA[true]]></Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(byte), null));
        }

        [Fact]
        public static void ReadContentAsByte6()
        {
            var reader = Utils.CreateFragmentReader("<Root>  <!-- Comment inbetween--><![CDATA[0]]>   </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(byte.MinValue, reader.ReadContentAs(typeof(byte), null));
        }
    }
}
