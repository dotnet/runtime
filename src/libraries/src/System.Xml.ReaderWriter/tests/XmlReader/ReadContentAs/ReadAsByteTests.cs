// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Xml;

namespace XMLTests.ReaderWriter.ReadContentTests
{
    public class ByteTests
    {
        [Fact]
        public static void ReadContentAsByte1()
        {
            var reader = Utils.CreateFragmentReader("<Root>  2<!-- Comment inbetween--><![CDATA[5]]>5<?a?>   </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(Byte.MaxValue, reader.ReadContentAs(typeof(Byte), null));
        }

        [Fact]
        public static void ReadContentAsByte2()
        {
            var reader = Utils.CreateFragmentReader("<Root> <![CDATA[0]]><?a?><!-- Comment inbetween--> </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(Byte.MinValue, reader.ReadContentAs(typeof(Byte), null));
        }

        [Fact]
        public static void ReadContentAsByte3()
        {
            var reader = Utils.CreateFragmentReader("<Root> <?a?><![CDATA[2]]>5<!-- Comment inbetween-->5 </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(Byte.MaxValue, reader.ReadContentAs(typeof(Byte), null));
        }

        [Fact]
        public static void ReadContentAsByte4()
        {
            var reader = Utils.CreateFragmentReader("<Root>  <![CDATA[false]]><!-- Comment inbetween-->  <?a?></Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(Byte), null));
        }

        [Fact]
        public static void ReadContentAsByte5()
        {
            var reader = Utils.CreateFragmentReader("<Root><!-- Comment inbetween--><![CDATA[true]]></Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(Byte), null));
        }

        [Fact]
        public static void ReadContentAsByte6()
        {
            var reader = Utils.CreateFragmentReader("<Root>  <!-- Comment inbetween--><![CDATA[0]]>   </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(Byte.MinValue, reader.ReadContentAs(typeof(Byte), null));
        }
    }
}