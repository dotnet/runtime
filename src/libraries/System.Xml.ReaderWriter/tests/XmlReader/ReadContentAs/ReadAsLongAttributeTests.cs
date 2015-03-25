// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Xml;

namespace XMLTests.ReaderWriter.ReadContentTests
{
    public class LongAttributeTests
    {
        [Fact]
        public static void ReadContentAsLongAttribute1()
        {
            var reader = Utils.CreateFragmentReader("<Root a=' 0 '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(0L, reader.ReadContentAs(typeof(long), null));
        }

        [Fact]
        public static void ReadContentAsLongAttribute10()
        {
            var reader = Utils.CreateFragmentReader("<Root a='56455'/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(56455L, reader.ReadContentAsLong());
        }

        [Fact]
        public static void ReadContentAsLongAttribute11()
        {
            var reader = Utils.CreateFragmentReader("<Root a=' -000123456 '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(-123456L, reader.ReadContentAsLong());
        }

        [Fact]
        public static void ReadContentAsLongAttribute12()
        {
            var reader = Utils.CreateFragmentReader("<Root a=' 000099999 '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(99999L, reader.ReadContentAsLong());
        }

        [Fact]
        public static void ReadContentAsLongAttribute2()
        {
            var reader = Utils.CreateFragmentReader("<Root a=' 9999'/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(9999L, reader.ReadContentAs(typeof(long), null));
        }

        [Fact]
        public static void ReadContentAsLongAttribute3()
        {
            var reader = Utils.CreateFragmentReader("<Root a='-5644 '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(-5644L, reader.ReadContentAs(typeof(long), null));
        }

        [Fact]
        public static void ReadContentAsLongAttribute4()
        {
            var reader = Utils.CreateFragmentReader("<Root a='56455'/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(56455L, reader.ReadContentAs(typeof(long), null));
        }

        [Fact]
        public static void ReadContentAsLongAttribute5()
        {
            var reader = Utils.CreateFragmentReader("<Root a=' -000123456 '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(-123456L, reader.ReadContentAs(typeof(long), null));
        }

        [Fact]
        public static void ReadContentAsLongAttribute6()
        {
            var reader = Utils.CreateFragmentReader("<Root a=' 000099999 '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(99999L, reader.ReadContentAs(typeof(long), null));
        }

        [Fact]
        public static void ReadContentAsLongAttribute7()
        {
            var reader = Utils.CreateFragmentReader("<Root a=' 0 '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(0L, reader.ReadContentAsLong());
        }

        [Fact]
        public static void ReadContentAsLongAttribute8()
        {
            var reader = Utils.CreateFragmentReader("<Root a=' 9999'/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(9999L, reader.ReadContentAsLong());
        }

        [Fact]
        public static void ReadContentAsLongAttribute9()
        {
            var reader = Utils.CreateFragmentReader("<Root a='-5644 '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(-5644L, reader.ReadContentAsLong());
        }
    }
}