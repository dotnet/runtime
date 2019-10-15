// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class FloatTests
    {
        [Fact]
        public static void ReadContentAsFloat1()
        {
            var reader = Utils.CreateFragmentReader("<Root> -0<!-- Comment inbetween-->05.145<?a?><![CDATA[6]]> </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(-5.1456F, reader.ReadContentAsFloat());
        }

        [Fact]
        public static void ReadContentAsFloat10()
        {
            var reader = Utils.CreateFragmentReader("<Root>  5<![CDATA[6]]>.455555<!-- Comment inbetween--><![CDATA[6]]>44<?a?>  </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(56.455555644F, reader.ReadContentAs(typeof(float), null));
        }

        [Fact]
        public static void ReadContentAsFloat11()
        {
            var reader = Utils.CreateFragmentReader("<Root> -000123<?a?>45<!-- Comment inbetween--><![CDATA[6]]> </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(-123456F, reader.ReadContentAs(typeof(float), null));
        }

        [Fact]
        public static void ReadContentAsFloat12()
        {
            var reader = Utils.CreateFragmentReader("<Root> <![CDATA[9]]>999<!-- Comment inbetween-->9.444<?a?>5<![CDATA[6]]> </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(99999.44456F, reader.ReadContentAs(typeof(float), null));
        }

        [Fact]
        public static void ReadContentAsFloat2()
        {
            var reader = Utils.CreateFragmentReader("<Root> <?a?>00<!-- Comment inbetween-->9<![CDATA[9]]>.<![CDATA[9]]>99999</Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(99.999999F, reader.ReadContentAsFloat());
        }

        [Fact]
        public static void ReadContentAsFloat3()
        {
            var reader = Utils.CreateFragmentReader("<Root>-5<![CDATA[6]]>.4<?a?>444<!-- Comment inbetween-->455<![CDATA[6]]> </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(-56.44444556F, reader.ReadContentAsFloat());
        }

        [Fact]
        public static void ReadContentAsFloat4()
        {
            var reader = Utils.CreateFragmentReader("<Root>  5<![CDATA[6]]>.455555<!-- Comment inbetween--><![CDATA[6]]>44<?a?>  </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(56.455555644F, reader.ReadContentAsFloat());
        }

        [Fact]
        public static void ReadContentAsFloat5()
        {
            var reader = Utils.CreateFragmentReader("<Root> -000123<?a?>45<!-- Comment inbetween--><![CDATA[6]]> </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(-123456F, reader.ReadContentAsFloat());
        }

        [Fact]
        public static void ReadContentAsFloat6()
        {
            var reader = Utils.CreateFragmentReader("<Root> <![CDATA[9]]>999<!-- Comment inbetween-->9.444<?a?>5<![CDATA[6]]> </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(99999.44456F, reader.ReadContentAsFloat());
        }

        [Fact]
        public static void ReadContentAsFloat7()
        {
            var reader = Utils.CreateFragmentReader("<Root> -0<!-- Comment inbetween-->05.145<?a?><![CDATA[6]]> </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(-5.1456F, reader.ReadContentAs(typeof(float), null));
        }

        [Fact]
        public static void ReadContentAsFloat8()
        {
            var reader = Utils.CreateFragmentReader("<Root> <?a?>00<!-- Comment inbetween-->9<![CDATA[9]]>.<![CDATA[9]]>99999</Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(99.999999F, reader.ReadContentAs(typeof(float), null));
        }

        [Fact]
        public static void ReadContentAsFloat9()
        {
            var reader = Utils.CreateFragmentReader("<Root>-5<![CDATA[6]]>.4<?a?>444<!-- Comment inbetween-->455<![CDATA[6]]> </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(-56.44444556F, reader.ReadContentAs(typeof(float), null));
        }
    }
}
