// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class ObjectAttributeTests
    {
        [Fact]
        public static void ReadContentAsObjectAttribute1()
        {
            var reader = Utils.CreateFragmentReader("<Root a='-56.455'/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal("-56.455", reader.ReadContentAsObject());
        }

        [Fact]
        public static void ReadContentAsObjectAttribute10()
        {
            var reader = Utils.CreateFragmentReader("<Root a='-56.44'/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal("-56.44", reader.ReadContentAs(typeof(object), null));
        }

        [Fact]
        public static void ReadContentAsObjectAttribute11()
        {
            var reader = Utils.CreateFragmentReader("<Root a='-56.455'/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal("-56.455", reader.ReadContentAs(typeof(object), null));
        }

        [Fact]
        public static void ReadContentAsObjectAttribute2()
        {
            var reader = Utils.CreateFragmentReader("<Root a='0'/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal("0", reader.ReadContentAs(typeof(object), null));
        }

        [Fact]
        public static void ReadContentAsObjectAttribute3()
        {
            var reader = Utils.CreateFragmentReader("<Root a='99999.44456'/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal("99999.44456", reader.ReadContentAs(typeof(object), null));
        }

        [Fact]
        public static void ReadContentAsObjectAttribute4()
        {
            var reader = Utils.CreateFragmentReader("<Root a='0'/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal("0", reader.ReadContentAsObject());
        }

        [Fact]
        public static void ReadContentAsObjectAttribute5()
        {
            var reader = Utils.CreateFragmentReader("<Root a='0099.99'/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal("0099.99", reader.ReadContentAsObject());
        }

        [Fact]
        public static void ReadContentAsObjectAttribute6()
        {
            var reader = Utils.CreateFragmentReader("<Root a='-56.44'/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal("-56.44", reader.ReadContentAsObject());
        }

        [Fact]
        public static void ReadContentAsObjectAttribute7()
        {
            var reader = Utils.CreateFragmentReader("<Root a='-000123456'/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal("-000123456", reader.ReadContentAsObject());
        }

        [Fact]
        public static void ReadContentAsObjectAttribute8()
        {
            var reader = Utils.CreateFragmentReader("<Root a='99999.44456'/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal("99999.44456", reader.ReadContentAsObject());
        }

        [Fact]
        public static void ReadContentAsObjectAttribute9()
        {
            var reader = Utils.CreateFragmentReader("<Root a='0099.99'/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal("0099.99", reader.ReadContentAs(typeof(object), null));
        }

        [Fact]
        public static void ReadContentAsObjectgAttribute1()
        {
            var reader = Utils.CreateFragmentReader("<Root a='-000123456'/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal("-000123456", reader.ReadContentAs(typeof(object), null));
        }
    }
}
