// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Xml;

namespace XMLTests.ReaderWriter.ReadContentTests
{
    public class BooleanAttributeTests
    {
        [Fact]
        public static void ReadContentAsBooleanAttribute1()
        {
            var reader = Utils.CreateFragmentReader("<Root a='0'/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(false, reader.ReadContentAsBoolean());
        }

        [Fact]
        public static void ReadContentAsBooleanAttribute10()
        {
            var reader = Utils.CreateFragmentReader("<Root a='  1  '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(true, reader.ReadContentAsBoolean());
        }

        [Fact]
        public static void ReadContentAsBooleanAttribute11()
        {
            var reader = Utils.CreateFragmentReader("<Root a='0'/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(false, reader.ReadContentAs(typeof(bool), null));
        }

        [Fact]
        public static void ReadContentAsBooleanAttribute12()
        {
            var reader = Utils.CreateFragmentReader("<Root a=' 1 '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(true, reader.ReadContentAs(typeof(bool), null));
        }

        [Fact]
        public static void ReadContentAsBooleanAttribute2()
        {
            var reader = Utils.CreateFragmentReader("<Root a=' false '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(false, reader.ReadContentAs(typeof(bool), null));
        }

        [Fact]
        public static void ReadContentAsBooleanAttribute3()
        {
            var reader = Utils.CreateFragmentReader("<Root a=' true '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(true, reader.ReadContentAs(typeof(bool), null));
        }

        [Fact]
        public static void ReadContentAsBooleanAttribute4()
        {
            var reader = Utils.CreateFragmentReader("<Root a='  0  '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(false, reader.ReadContentAs(typeof(bool), null));
        }

        [Fact]
        public static void ReadContentAsBooleanAttribute5()
        {
            var reader = Utils.CreateFragmentReader("<Root a='  1  '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(true, reader.ReadContentAs(typeof(bool), null));
        }

        [Fact]
        public static void ReadContentAsBooleanAttribute6()
        {
            var reader = Utils.CreateFragmentReader("<Root a=' 1 '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(true, reader.ReadContentAsBoolean());
        }

        [Fact]
        public static void ReadContentAsBooleanAttribute7()
        {
            var reader = Utils.CreateFragmentReader("<Root a=' false '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(false, reader.ReadContentAsBoolean());
        }

        [Fact]
        public static void ReadContentAsBooleanAttribute8()
        {
            var reader = Utils.CreateFragmentReader("<Root a=' true '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(true, reader.ReadContentAsBoolean());
        }

        [Fact]
        public static void ReadContentAsBooleanAttribute9()
        {
            var reader = Utils.CreateFragmentReader("<Root a='  0  '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(false, reader.ReadContentAsBoolean());
        }
    }
}