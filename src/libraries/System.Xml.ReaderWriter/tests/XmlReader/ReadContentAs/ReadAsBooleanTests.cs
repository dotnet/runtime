// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Xml;

namespace XMLTests.ReaderWriter.ReadContentTests
{
    public class BooleanTests
    {
        [Fact]
        public static void ReadContentAsBoolean1()
        {
            var reader = Utils.CreateFragmentReader("<Root> <?a?><!-- Comment inbetween--><![CDATA[0]]> </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(false, reader.ReadContentAsBoolean());
        }

        [Fact]
        public static void ReadContentAsBoolean10()
        {
            var reader = Utils.CreateFragmentReader("<Root> t<!-- Comment inbetween-->ru<?a?>e </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(true, reader.ReadContentAs(typeof(bool), null));
        }

        [Fact]
        public static void ReadContentAsBoolean11()
        {
            var reader = Utils.CreateFragmentReader("<Root>  <?a?><!-- Comment inbetween-->0   </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(false, reader.ReadContentAs(typeof(bool), null));
        }

        [Fact]
        public static void ReadContentAsBoolean12()
        {
            var reader = Utils.CreateFragmentReader("<Root><![CDATA[1]]><?a?></Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(true, reader.ReadContentAs(typeof(bool), null));
        }

        [Fact]
        public static void ReadContentAsBoolean2()
        {
            var reader = Utils.CreateFragmentReader("<Root> <!-- Comment inbetween--><![CDATA[1]]><?a?> </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(true, reader.ReadContentAsBoolean());
        }

        [Fact]
        public static void ReadContentAsBoolean3()
        {
            var reader = Utils.CreateFragmentReader("<Root> f<!-- Comment inbetween-->a<?a?>lse<!-- Comment inbetween--> </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(false, reader.ReadContentAsBoolean());
        }

        [Fact]
        public static void ReadContentAsBoolean4()
        {
            var reader = Utils.CreateFragmentReader("<Root> t<!-- Comment inbetween-->ru<?a?>e </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(true, reader.ReadContentAsBoolean());
        }

        [Fact]
        public static void ReadContentAsBoolean5()
        {
            var reader = Utils.CreateFragmentReader("<Root>  <?a?><!-- Comment inbetween-->0   </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(false, reader.ReadContentAsBoolean());
        }

        [Fact]
        public static void ReadContentAsBoolean6()
        {
            var reader = Utils.CreateFragmentReader("<Root><![CDATA[1]]><?a?></Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(true, reader.ReadContentAsBoolean());
        }

        [Fact]
        public static void ReadContentAsBoolean7()
        {
            var reader = Utils.CreateFragmentReader("<Root> <?a?><!-- Comment inbetween--><![CDATA[0]]> </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(false, reader.ReadContentAs(typeof(bool), null));
        }

        [Fact]
        public static void ReadContentAsBoolean8()
        {
            var reader = Utils.CreateFragmentReader("<Root> <!-- Comment inbetween--><![CDATA[1]]><?a?> </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(true, reader.ReadContentAs(typeof(bool), null));
        }

        [Fact]
        public static void ReadContentAsBoolean9()
        {
            var reader = Utils.CreateFragmentReader("<Root> f<!-- Comment inbetween-->a<?a?>lse<!-- Comment inbetween--> </Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal(false, reader.ReadContentAs(typeof(bool), null));
        }
    }
}