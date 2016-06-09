// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class StringTests
    {
        [Fact]
        public static void ReadContentAsString1()
        {
            var reader = Utils.CreateFragmentReader("<Root>44.55</Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal("44.55", reader.ReadContentAs(typeof(String), null));
        }

        [Fact]
        public static void ReadContentAsString10()
        {
            var reader = Utils.CreateFragmentReader("<Root>0</Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal("0", reader.ReadContentAs(typeof(String), null));
        }

        [Fact]
        public static void ReadContentAsString11()
        {
            var reader = Utils.CreateFragmentReader("<Root>9999.9</Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal("9999.9", reader.ReadContentAs(typeof(String), null));
        }

        [Fact]
        public static void ReadContentAsString12()
        {
            var reader = Utils.CreateFragmentReader("<Root>44.44</Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal("44.44", reader.ReadContentAs(typeof(String), null));
        }

        [Fact]
        public static void ReadContentAsString2()
        {
            var reader = Utils.CreateFragmentReader("<Root>-005.5</Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal("-005.5", reader.ReadContentAsString());
        }

        [Fact]
        public static void ReadContentAsString3()
        {
            var reader = Utils.CreateFragmentReader("<Root>0001</Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal("0001", reader.ReadContentAsString());
        }

        [Fact]
        public static void ReadContentAsString4()
        {
            var reader = Utils.CreateFragmentReader("<Root>0</Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal("0", reader.ReadContentAsString());
        }

        [Fact]
        public static void ReadContentAsString5()
        {
            var reader = Utils.CreateFragmentReader("<Root>9999.9</Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal("9999.9", reader.ReadContentAsString());
        }

        [Fact]
        public static void ReadContentAsString6()
        {
            var reader = Utils.CreateFragmentReader("<Root>44.44</Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal("44.44", reader.ReadContentAsString());
        }

        [Fact]
        public static void ReadContentAsString7()
        {
            var reader = Utils.CreateFragmentReader("<Root>44.55</Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal("44.55", reader.ReadContentAsString());
        }

        [Fact]
        public static void ReadContentAsString8()
        {
            var reader = Utils.CreateFragmentReader("<Root>-005.5</Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal("-005.5", reader.ReadContentAs(typeof(String), null));
        }

        [Fact]
        public static void ReadContentAsString9()
        {
            var reader = Utils.CreateFragmentReader("<Root>0001</Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal("0001", reader.ReadContentAs(typeof(String), null));
        }
    }
}
