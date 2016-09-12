// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class FloatAttributeTests
    {
        [Fact]
        public static void ReadContentAsFloatAttribute1()
        {
            var reader = Utils.CreateFragmentReader("<Root a='56.455555644'/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(56.455555644F, reader.ReadContentAsFloat());
        }

        [Fact]
        public static void ReadContentAsFloatAttribute10()
        {
            var reader = Utils.CreateFragmentReader("<Root a=' -000123456 '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(-123456F, reader.ReadContentAsFloat());
        }

        [Fact]
        public static void ReadContentAsFloatAttribute11()
        {
            var reader = Utils.CreateFragmentReader("<Root a=' 99999.44456 '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(99999.44456F, reader.ReadContentAsFloat());
        }

        [Fact]
        public static void ReadContentAsFloatAttribute12()
        {
            var reader = Utils.CreateFragmentReader("<Root a=' 0099.999999'/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(99.999999F, reader.ReadContentAs(typeof(float), null));
        }

        [Fact]
        public static void ReadContentAsFloatAttribute2()
        {
            var reader = Utils.CreateFragmentReader("<Root a=' -005.1456 '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(-5.1456F, reader.ReadContentAs(typeof(float), null));
        }

        [Fact]
        public static void ReadContentAsFloatAttribute3()
        {
            var reader = Utils.CreateFragmentReader("<Root a='-56.44444556 '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(-56.44444556F, reader.ReadContentAs(typeof(float), null));
        }

        [Fact]
        public static void ReadContentAsFloatAttribute4()
        {
            var reader = Utils.CreateFragmentReader("<Root a='56.455555644'/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(56.455555644F, reader.ReadContentAs(typeof(float), null));
        }

        [Fact]
        public static void ReadContentAsFloatAttribute5()
        {
            var reader = Utils.CreateFragmentReader("<Root a=' -000123456 '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(-123456F, reader.ReadContentAs(typeof(float), null));
        }

        [Fact]
        public static void ReadContentAsFloatAttribute6()
        {
            var reader = Utils.CreateFragmentReader("<Root a=' 99999.44456 '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(99999.44456F, reader.ReadContentAs(typeof(float), null));
        }

        [Fact]
        public static void ReadContentAsFloatAttribute7()
        {
            var reader = Utils.CreateFragmentReader("<Root a=' -005.1456 '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(-5.1456F, reader.ReadContentAsFloat());
        }

        [Fact]
        public static void ReadContentAsFloatAttribute8()
        {
            var reader = Utils.CreateFragmentReader("<Root a=' 0099.999999'/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(99.999999F, reader.ReadContentAsFloat());
        }

        [Fact]
        public static void ReadContentAsFloatAttribute9()
        {
            var reader = Utils.CreateFragmentReader("<Root a='-56.44444556 '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(-56.44444556F, reader.ReadContentAsFloat());
        }
    }
}
