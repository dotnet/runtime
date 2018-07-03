// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class DecimalTests
    {
        [Fact]
        public static void ReadContentAsDecimal1()
        {
            var reader = Utils.CreateFragmentReader("<Root>44<?a?>.44</Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(44.44m, reader.ReadContentAs(typeof(decimal), null));
        }

        [Fact]
        public static void ReadContentAsDecimal10()
        {
            var reader = Utils.CreateFragmentReader("<Root>  00<!-- Comment inbetween-->01<?a?></Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(1m, reader.ReadContentAs(typeof(decimal), null));
        }

        [Fact]
        public static void ReadContentAsDecimal11()
        {
            var reader = Utils.CreateFragmentReader("<Root> <?a?>0  </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(0m, reader.ReadContentAs(typeof(decimal), null));
        }

        [Fact]
        public static void ReadContentAsDecimal12()
        {
            var reader = Utils.CreateFragmentReader("<Root> 9<![CDATA[9]]>99.9 </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(9999.9m, reader.ReadContentAs(typeof(decimal), null));
        }

        [Fact]
        public static void ReadContentAsDecimal2()
        {
            var reader = Utils.CreateFragmentReader("<Root>  -0<?a?>0<!-- Comment inbetween-->5.<![CDATA[5]]> </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(-5.5m, reader.ReadContentAsDecimal());
        }

        [Fact]
        public static void ReadContentAsDecimal3()
        {
            var reader = Utils.CreateFragmentReader("<Root>  00<!-- Comment inbetween-->01<?a?></Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(1m, reader.ReadContentAsDecimal());
        }

        [Fact]
        public static void ReadContentAsDecimal4()
        {
            var reader = Utils.CreateFragmentReader("<Root> <?a?>0  </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(0m, reader.ReadContentAsDecimal());
        }

        [Fact]
        public static void ReadContentAsDecimal5()
        {
            var reader = Utils.CreateFragmentReader("<Root> 9<![CDATA[9]]>99.9 </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(9999.9m, reader.ReadContentAsDecimal());
        }

        [Fact]
        public static void ReadContentAsDecimal6()
        {
            var reader = Utils.CreateFragmentReader("<Root>44<?a?>.44</Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(44.44m, reader.ReadContentAsDecimal());
        }

        [Fact]
        public static void ReadContentAsDecimal7()
        {
            var reader = Utils.CreateFragmentReader("<Root>  4<?a?>4.5<!-- Comment inbetween-->5  </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(44.55m, reader.ReadContentAsDecimal());
        }

        [Fact]
        public static void ReadContentAsDecimal8()
        {
            var reader = Utils.CreateFragmentReader("<Root>  4<?a?>4.5<!-- Comment inbetween-->5  </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(44.55m, reader.ReadContentAs(typeof(decimal), null));
        }

        [Fact]
        public static void ReadContentAsDecimal9()
        {
            var reader = Utils.CreateFragmentReader("<Root>  -0<?a?>0<!-- Comment inbetween-->5.<![CDATA[5]]> </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(-5.5m, reader.ReadContentAs(typeof(decimal), null));
        }
    }
}
