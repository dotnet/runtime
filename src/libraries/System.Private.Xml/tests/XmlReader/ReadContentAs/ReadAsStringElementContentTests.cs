// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class StringElementContentTests
    {
        [Fact]
        public static void ReadElementContentAsString1()
        {
            var reader = Utils.CreateFragmentReader("<Root>-005.5</Root>");
            reader.PositionOnElement("Root");
            Assert.Equal("-005.5", reader.ReadElementContentAsString());
        }

        [Fact]
        public static void ReadElementContentAsString10()
        {
            var reader = Utils.CreateFragmentReader("<Root>44.44</Root>");
            reader.PositionOnElement("Root");
            Assert.Equal("44.44", reader.ReadElementContentAs(typeof(string), null));
        }

        [Fact]
        public static void ReadElementContentAsString11()
        {
            var reader = Utils.CreateFragmentReader("<Root>44.55</Root>");
            reader.PositionOnElement("Root");
            Assert.Equal("44.55", reader.ReadElementContentAs(typeof(string), null));
        }

        [Fact]
        public static void ReadElementContentAsString12()
        {
            var reader = Utils.CreateFragmentReader("<Root>-005.5</Root>");
            reader.PositionOnElement("Root");
            Assert.Equal("-005.5", reader.ReadElementContentAs(typeof(string), null));
        }

        [Fact]
        public static void ReadElementContentAsString2()
        {
            var reader = Utils.CreateFragmentReader("<Root>0001</Root>");
            reader.PositionOnElement("Root");
            Assert.Equal("0001", reader.ReadElementContentAsString());
        }

        [Fact]
        public static void ReadElementContentAsString3()
        {
            var reader = Utils.CreateFragmentReader("<Root>0</Root>");
            reader.PositionOnElement("Root");
            Assert.Equal("0", reader.ReadElementContentAsString());
        }

        [Fact]
        public static void ReadElementContentAsString4()
        {
            var reader = Utils.CreateFragmentReader("<Root>9999.9</Root>");
            reader.PositionOnElement("Root");
            Assert.Equal("9999.9", reader.ReadElementContentAsString());
        }

        [Fact]
        public static void ReadElementContentAsString5()
        {
            var reader = Utils.CreateFragmentReader("<Root>44.44</Root>");
            reader.PositionOnElement("Root");
            Assert.Equal("44.44", reader.ReadElementContentAsString());
        }

        [Fact]
        public static void ReadElementContentAsString6()
        {
            var reader = Utils.CreateFragmentReader("<Root>44.55</Root>");
            reader.PositionOnElement("Root");
            Assert.Equal("44.55", reader.ReadElementContentAsString());
        }

        [Fact]
        public static void ReadElementContentAsString7()
        {
            var reader = Utils.CreateFragmentReader("<Root>0001</Root>");
            reader.PositionOnElement("Root");
            Assert.Equal("0001", reader.ReadElementContentAs(typeof(string), null));
        }

        [Fact]
        public static void ReadElementContentAsString8()
        {
            var reader = Utils.CreateFragmentReader("<Root>0</Root>");
            reader.PositionOnElement("Root");
            Assert.Equal("0", reader.ReadElementContentAs(typeof(string), null));
        }

        [Fact]
        public static void ReadElementContentAsString9()
        {
            var reader = Utils.CreateFragmentReader("<Root>9999.9</Root>");
            reader.PositionOnElement("Root");
            Assert.Equal("9999.9", reader.ReadElementContentAs(typeof(string), null));
        }
    }
}
