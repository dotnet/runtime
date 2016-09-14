// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
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
