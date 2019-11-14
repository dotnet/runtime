// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class DoubleTests
    {
        [Fact]
        public static void ReadContentAsDouble1()
        {
            var reader = Utils.CreateFragmentReader("<Root>  <!-- Comment inbetween-->44.<?a?>44  </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(44.44d, reader.ReadContentAsDouble());
        }

        [Fact]
        public static void ReadContentAsDouble10()
        {
            var reader = Utils.CreateFragmentReader("<Root><![CDATA[9]]>9<?a?>9<!-- Comment inbetween-->9.<![CDATA[9]]></Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(9999.9d, reader.ReadContentAs(typeof(double), null));
        }

        [Fact]
        public static void ReadContentAsDouble11()
        {
            var reader = Utils.CreateFragmentReader("<Root>  <!-- Comment inbetween-->44.<?a?>44  </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(44.44d, reader.ReadContentAs(typeof(double), null));
        }

        [Fact]
        public static void ReadContentAsDouble12()
        {
            var reader = Utils.CreateFragmentReader("<Root> 44<?a?>.55<!-- Comment inbetween--> </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(44.55d, reader.ReadContentAs(typeof(double), null));
        }

        [Fact]
        public static void ReadContentAsDouble2()
        {
            var reader = Utils.CreateFragmentReader("<Root> <![CDATA[-0]]><?a?>0<!-- Comment inbetween-->5.5 </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(-5.5d, reader.ReadContentAsDouble());
        }

        [Fact]
        public static void ReadContentAsDouble3()
        {
            var reader = Utils.CreateFragmentReader("<Root> <?a?>0<![CDATA[0]]>0<!-- Comment inbetween-->1</Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(1d, reader.ReadContentAsDouble());
        }

        [Fact]
        public static void ReadContentAsDouble4()
        {
            var reader = Utils.CreateFragmentReader("<Root> <![CDATA[0]]> </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(0d, reader.ReadContentAsDouble());
        }

        [Fact]
        public static void ReadContentAsDouble5()
        {
            var reader = Utils.CreateFragmentReader("<Root><![CDATA[9]]>9<?a?>9<!-- Comment inbetween-->9.<![CDATA[9]]></Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(9999.9d, reader.ReadContentAsDouble());
        }

        [Fact]
        public static void ReadContentAsDouble6()
        {
            var reader = Utils.CreateFragmentReader("<Root> 44<?a?>.55<!-- Comment inbetween--> </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(44.55d, reader.ReadContentAsDouble());
        }

        [Fact]
        public static void ReadContentAsDouble7()
        {
            var reader = Utils.CreateFragmentReader("<Root> <![CDATA[-0]]><?a?>0<!-- Comment inbetween-->5.5 </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(-5.5d, reader.ReadContentAs(typeof(double), null));
        }

        [Fact]
        public static void ReadContentAsDouble8()
        {
            var reader = Utils.CreateFragmentReader("<Root> <?a?>0<![CDATA[0]]>0<!-- Comment inbetween-->1</Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(1d, reader.ReadContentAs(typeof(double), null));
        }

        [Fact]
        public static void ReadContentAsDouble9()
        {
            var reader = Utils.CreateFragmentReader("<Root> <![CDATA[0]]> </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(0d, reader.ReadContentAs(typeof(double), null));
        }
    }
}
