// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class StringAttributeTests
    {
        [Fact]
        public static void ReadContentAsStringAttribute1()
        {
            var reader = Utils.CreateFragmentReader("<Root a='-56.455'/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal("-56.455", reader.ReadContentAs(typeof(string), null));
        }

        [Fact]
        public static void ReadContentAsStringAttribute10()
        {
            var reader = Utils.CreateFragmentReader("<Root a='0'/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal("0", reader.ReadContentAs(typeof(string), null));
        }

        [Fact]
        public static void ReadContentAsStringAttribute11()
        {
            var reader = Utils.CreateFragmentReader("<Root a='0099.99'/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal("0099.99", reader.ReadContentAs(typeof(string), null));
        }

        [Fact]
        public static void ReadContentAsStringAttribute12()
        {
            var reader = Utils.CreateFragmentReader("<Root a='-56.44'/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal("-56.44", reader.ReadContentAs(typeof(string), null));
        }

        [Fact]
        public static void ReadContentAsStringAttribute2()
        {
            var reader = Utils.CreateFragmentReader("<Root a='-000123456'/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal("-000123456", reader.ReadContentAs(typeof(string), null));
        }

        [Fact]
        public static void ReadContentAsStringAttribute3()
        {
            var reader = Utils.CreateFragmentReader("<Root a='99999.44456'/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal("99999.44456", reader.ReadContentAs(typeof(string), null));
        }

        [Fact]
        public static void ReadContentAsStringAttribute4()
        {
            var reader = Utils.CreateFragmentReader("<Root a='0'/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal("0", reader.ReadContentAsString());
        }

        [Fact]
        public static void ReadContentAsStringAttribute5()
        {
            var reader = Utils.CreateFragmentReader("<Root a='0099.99'/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal("0099.99", reader.ReadContentAsString());
        }

        [Fact]
        public static void ReadContentAsStringAttribute6()
        {
            var reader = Utils.CreateFragmentReader("<Root a='-56.44'/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal("-56.44", reader.ReadContentAsString());
        }

        [Fact]
        public static void ReadContentAsStringAttribute7()
        {
            var reader = Utils.CreateFragmentReader("<Root a='-56.455'/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal("-56.455", reader.ReadContentAsString());
        }

        [Fact]
        public static void ReadContentAsStringAttribute8()
        {
            var reader = Utils.CreateFragmentReader("<Root a='-000123456'/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal("-000123456", reader.ReadContentAsString());
        }

        [Fact]
        public static void ReadContentAsStringAttribute9()
        {
            var reader = Utils.CreateFragmentReader("<Root a='99999.44456'/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal("99999.44456", reader.ReadContentAsString());
        }
    }
}
