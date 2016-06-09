// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class DecimalAttributeTests
    {
        [Fact]
        public static void ReadContentAsDecimalAttribute1()
        {
            var reader = Utils.CreateFragmentReader("<Root a='  0099.99  '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(99.99m, reader.ReadContentAs(typeof(Decimal), null));
        }

        [Fact]
        public static void ReadContentAsDecimalAttribute10()
        {
            var reader = Utils.CreateFragmentReader("<Root a='-000123456  '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(-123456m, reader.ReadContentAsDecimal());
        }

        [Fact]
        public static void ReadContentAsDecimalAttribute11()
        {
            var reader = Utils.CreateFragmentReader("<Root a='99999.44456'/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(99999.44456m, reader.ReadContentAsDecimal());
        }

        [Fact]
        public static void ReadContentAsDecimalAttribute12()
        {
            var reader = Utils.CreateFragmentReader("<Root a=' 0 '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(0m, reader.ReadContentAs(typeof(Decimal), null));
        }

        [Fact]
        public static void ReadContentAsDecimalAttribute2()
        {
            var reader = Utils.CreateFragmentReader("<Root a=' -56.44 '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(-56.44m, reader.ReadContentAs(typeof(Decimal), null));
        }

        [Fact]
        public static void ReadContentAsDecimalAttribute3()
        {
            var reader = Utils.CreateFragmentReader("<Root a='  -56.455'/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(-56.455m, reader.ReadContentAs(typeof(Decimal), null));
        }

        [Fact]
        public static void ReadContentAsDecimalAttribute4()
        {
            var reader = Utils.CreateFragmentReader("<Root a='-000123456  '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(-123456m, reader.ReadContentAs(typeof(Decimal), null));
        }

        [Fact]
        public static void ReadContentAsDecimalAttribute5()
        {
            var reader = Utils.CreateFragmentReader("<Root a='99999.44456'/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(99999.44456m, reader.ReadContentAs(typeof(Decimal), null));
        }

        [Fact]
        public static void ReadContentAsDecimalAttribute6()
        {
            var reader = Utils.CreateFragmentReader("<Root a=' 0 '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(0m, reader.ReadContentAsDecimal());
        }

        [Fact]
        public static void ReadContentAsDecimalAttribute7()
        {
            var reader = Utils.CreateFragmentReader("<Root a='  0099.99  '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(99.99m, reader.ReadContentAsDecimal());
        }

        [Fact]
        public static void ReadContentAsDecimalAttribute8()
        {
            var reader = Utils.CreateFragmentReader("<Root a=' -56.44 '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(-56.44m, reader.ReadContentAsDecimal());
        }

        [Fact]
        public static void ReadContentAsDecimalAttribute9()
        {
            var reader = Utils.CreateFragmentReader("<Root a='  -56.455'/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal(-56.455m, reader.ReadContentAsDecimal());
        }
    }
}
