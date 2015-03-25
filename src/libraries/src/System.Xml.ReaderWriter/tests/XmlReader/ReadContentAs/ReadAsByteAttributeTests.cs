// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Xml;

namespace XMLTests.ReaderWriter.ReadContentTests
{
    public class ByteAttributeTests
    {
        [Fact]
        public static void ReadContentAsByteAttribute1()
        {
            var reader = Utils.CreateFragmentReader("<Root a='  false  '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(Byte), null));
        }

        [Fact]
        public static void ReadContentAsByteAttribute2()
        {
            var reader = Utils.CreateFragmentReader("<Root a='true'/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(Byte), null));
        }

        [Fact]
        public static void ReadContentAsByteAttribute3()
        {
            var reader = Utils.CreateFragmentReader("<Root a='  0  '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(Byte.MinValue, reader.ReadContentAs(typeof(Byte), null));
        }

        [Fact]
        public static void ReadContentAsByteAttribute4()
        {
            var reader = Utils.CreateFragmentReader("<Root a='  255  '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(Byte.MaxValue, reader.ReadContentAs(typeof(Byte), null));
        }

        [Fact]
        public static void ReadContentAsByteAttribute5()
        {
            var reader = Utils.CreateFragmentReader("<Root a='0'/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(Byte.MinValue, reader.ReadContentAs(typeof(Byte), null));
        }

        [Fact]
        public static void ReadContentAsByteAttribute6()
        {
            var reader = Utils.CreateFragmentReader("<Root a='255'/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(Byte.MaxValue, reader.ReadContentAs(typeof(Byte), null));
        }
    }
}