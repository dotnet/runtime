// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class DoubleAttributeTests
    {
        [Fact]
        public static void ReadContentAsDoubleAttribute1()
        {
            var reader = Utils.CreateFragmentReader("<Root a='  0  '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(0d, reader.ReadContentAs(typeof(double), null));
        }

        [Fact]
        public static void ReadContentAsDoubleAttribute10()
        {
            var reader = Utils.CreateFragmentReader("<Root a=' -000123456 '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(-123456d, reader.ReadContentAsDouble());
        }

        [Fact]
        public static void ReadContentAsDoubleAttribute11()
        {
            var reader = Utils.CreateFragmentReader("<Root a=' 99999.44456 '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(99999.44456d, reader.ReadContentAsDouble());
        }

        [Fact]
        public static void ReadContentAsDoubleAttribute12()
        {
            var reader = Utils.CreateFragmentReader("<Root a='  0099.99'/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(99.99d, reader.ReadContentAs(typeof(double), null));
        }

        [Fact]
        public static void ReadContentAsDoubleAttribute2()
        {
            var reader = Utils.CreateFragmentReader("<Root a='-56.44  '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(-56.44d, reader.ReadContentAs(typeof(double), null));
        }

        [Fact]
        public static void ReadContentAsDoubleAttribute3()
        {
            var reader = Utils.CreateFragmentReader("<Root a='-56.455'/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(-56.455d, reader.ReadContentAs(typeof(double), null));
        }

        [Fact]
        public static void ReadContentAsDoubleAttribute4()
        {
            var reader = Utils.CreateFragmentReader("<Root a=' -000123456 '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(-123456d, reader.ReadContentAs(typeof(double), null));
        }

        [Fact]
        public static void ReadContentAsDoubleAttribute5()
        {
            var reader = Utils.CreateFragmentReader("<Root a=' 99999.44456 '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(99999.44456d, reader.ReadContentAs(typeof(double), null));
        }

        [Fact]
        public static void ReadContentAsDoubleAttribute6()
        {
            var reader = Utils.CreateFragmentReader("<Root a='  0  '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(0d, reader.ReadContentAsDouble());
        }

        [Fact]
        public static void ReadContentAsDoubleAttribute7()
        {
            var reader = Utils.CreateFragmentReader("<Root a='  0099.99'/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(99.99d, reader.ReadContentAsDouble());
        }

        [Fact]
        public static void ReadContentAsDoubleAttribute8()
        {
            var reader = Utils.CreateFragmentReader("<Root a='-56.44  '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(-56.44d, reader.ReadContentAsDouble());
        }

        [Fact]
        public static void ReadContentAsDoubleAttribute9()
        {
            var reader = Utils.CreateFragmentReader("<Root a='-56.455'/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(-56.455d, reader.ReadContentAsDouble());
        }
    }
}
