// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class IntegerAttributeTests
    {
        [Fact]
        public static void ReadContentAsIntAttribute1()
        {
            var reader = Utils.CreateFragmentReader("<Root a='  9999'/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(9999, reader.ReadContentAs(typeof(int), null));
        }

        [Fact]
        public static void ReadContentAsIntAttribute10()
        {
            var reader = Utils.CreateFragmentReader("<Root a='56455'/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(56455, reader.ReadContentAsInt());
        }

        [Fact]
        public static void ReadContentAsIntAttribute11()
        {
            var reader = Utils.CreateFragmentReader("<Root a=' -000123456 '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(-123456, reader.ReadContentAsInt());
        }

        [Fact]
        public static void ReadContentAsIntAttribute12()
        {
            var reader = Utils.CreateFragmentReader("<Root a=' 000099999 '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(99999, reader.ReadContentAsInt());
        }

        [Fact]
        public static void ReadContentAsIntAttribute2()
        {
            var reader = Utils.CreateFragmentReader("<Root a='  0  '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(0, reader.ReadContentAs(typeof(int), null));
        }

        [Fact]
        public static void ReadContentAsIntAttribute3()
        {
            var reader = Utils.CreateFragmentReader("<Root a='-5644 '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(-5644, reader.ReadContentAs(typeof(int), null));
        }

        [Fact]
        public static void ReadContentAsIntAttribute4()
        {
            var reader = Utils.CreateFragmentReader("<Root a='56455'/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(56455, reader.ReadContentAs(typeof(int), null));
        }

        [Fact]
        public static void ReadContentAsIntAttribute5()
        {
            var reader = Utils.CreateFragmentReader("<Root a=' -000123456 '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(-123456, reader.ReadContentAs(typeof(int), null));
        }

        [Fact]
        public static void ReadContentAsIntAttribute6()
        {
            var reader = Utils.CreateFragmentReader("<Root a=' 000099999 '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(99999, reader.ReadContentAs(typeof(int), null));
        }

        [Fact]
        public static void ReadContentAsIntAttribute7()
        {
            var reader = Utils.CreateFragmentReader("<Root a='  0  '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(0, reader.ReadContentAsInt());
        }

        [Fact]
        public static void ReadContentAsIntAttribute8()
        {
            var reader = Utils.CreateFragmentReader("<Root a='  9999'/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(9999, reader.ReadContentAsInt());
        }

        [Fact]
        public static void ReadContentAsIntAttribute9()
        {
            var reader = Utils.CreateFragmentReader("<Root a='-5644 '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(-5644, reader.ReadContentAsInt());
        }
    }
}
