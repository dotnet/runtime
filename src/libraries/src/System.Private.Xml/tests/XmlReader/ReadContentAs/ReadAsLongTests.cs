// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class LongTests
    {
        [Fact]
        public static void ReadContentAsLong1()
        {
            var reader = Utils.CreateFragmentReader("<Root>  <?a?>-0<!-- Comment inbetween-->0<![CDATA[5]]>  </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(-5L, reader.ReadContentAsLong());
        }

        [Fact]
        public static void ReadContentAsLong10()
        {
            var reader = Utils.CreateFragmentReader("<Root>99<?a?>99<!-- Comment inbetween-->99</Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(999999L, reader.ReadContentAs(typeof(long), null));
        }

        [Fact]
        public static void ReadContentAsLong11()
        {
            var reader = Utils.CreateFragmentReader("<Root> -<?a?>4<!-- Comment inbetween-->4 </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(-44L, reader.ReadContentAs(typeof(long), null));
        }

        [Fact]
        public static void ReadContentAsLong12()
        {
            var reader = Utils.CreateFragmentReader("<Root> -<?a?><!-- Comment inbetween-->00045<![CDATA[5]]> </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(-455L, reader.ReadContentAs(typeof(long), null));
        }

        [Fact]
        public static void ReadContentAsLong2()
        {
            var reader = Utils.CreateFragmentReader("<Root>  <![CDATA[0]]>0<?a?>0<!-- Comment inbetween-->1<!-- Comment inbetween--></Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(1L, reader.ReadContentAsLong());
        }

        [Fact]
        public static void ReadContentAsLong3()
        {
            var reader = Utils.CreateFragmentReader("<Root><?a?><![CDATA[0]]><!-- Comment inbetween-->  </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(0L, reader.ReadContentAsLong());
        }

        [Fact]
        public static void ReadContentAsLong4()
        {
            var reader = Utils.CreateFragmentReader("<Root>99<?a?>99<!-- Comment inbetween-->99</Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(999999L, reader.ReadContentAsLong());
        }

        [Fact]
        public static void ReadContentAsLong5()
        {
            var reader = Utils.CreateFragmentReader("<Root> -<?a?>4<!-- Comment inbetween-->4 </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(-44L, reader.ReadContentAsLong());
        }

        [Fact]
        public static void ReadContentAsLong6()
        {
            var reader = Utils.CreateFragmentReader("<Root> -<?a?><!-- Comment inbetween-->00045<![CDATA[5]]> </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(-455L, reader.ReadContentAsLong());
        }

        [Fact]
        public static void ReadContentAsLong7()
        {
            var reader = Utils.CreateFragmentReader("<Root>  <?a?>-0<!-- Comment inbetween-->0<![CDATA[5]]>  </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(-5L, reader.ReadContentAs(typeof(long), null));
        }

        [Fact]
        public static void ReadContentAsLong8()
        {
            var reader = Utils.CreateFragmentReader("<Root>  <![CDATA[0]]>0<?a?>0<!-- Comment inbetween-->1<!-- Comment inbetween--></Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(1L, reader.ReadContentAs(typeof(long), null));
        }

        [Fact]
        public static void ReadContentAsLong9()
        {
            var reader = Utils.CreateFragmentReader("<Root><?a?><![CDATA[0]]><!-- Comment inbetween-->  </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(0L, reader.ReadContentAs(typeof(long), null));
        }
    }
}
