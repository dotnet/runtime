// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class ObjectElementContentTests
    {
        [Fact]
        public static void ReadElementContentAsObject1()
        {
            var reader = Utils.CreateFragmentReader("<Root>-005.5</Root>");
            reader.PositionOnElement("Root");
            Assert.Equal("-005.5", reader.ReadElementContentAsObject());
        }

        [Fact]
        public static void ReadElementContentAsObject10()
        {
            var reader = Utils.CreateFragmentReader("<Root>9999.9</Root>");
            reader.PositionOnElement("Root");
            Assert.Equal("9999.9", reader.ReadElementContentAs(typeof(object), null));
        }

        [Fact]
        public static void ReadElementContentAsObject11()
        {
            var reader = Utils.CreateFragmentReader("<Root>44.44</Root>");
            reader.PositionOnElement("Root");
            Assert.Equal("44.44", reader.ReadElementContentAs(typeof(object), null));
        }

        [Fact]
        public static void ReadElementContentAsObject12()
        {
            var reader = Utils.CreateFragmentReader("<Root>44.55</Root>");
            reader.PositionOnElement("Root");
            Assert.Equal("44.55", reader.ReadElementContentAs(typeof(object), null));
        }

        [Fact]
        public static void ReadElementContentAsObject2()
        {
            var reader = Utils.CreateFragmentReader("<Root>0001</Root>");
            reader.PositionOnElement("Root");
            Assert.Equal("0001", reader.ReadElementContentAsObject());
        }

        [Fact]
        public static void ReadElementContentAsObject3()
        {
            var reader = Utils.CreateFragmentReader("<Root>0</Root>");
            reader.PositionOnElement("Root");
            Assert.Equal("0", reader.ReadElementContentAsObject());
        }

        [Fact]
        public static void ReadElementContentAsObject4()
        {
            var reader = Utils.CreateFragmentReader("<Root>9999.9</Root>");
            reader.PositionOnElement("Root");
            Assert.Equal("9999.9", reader.ReadElementContentAsObject());
        }

        [Fact]
        public static void ReadElementContentAsObject5()
        {
            var reader = Utils.CreateFragmentReader("<Root>44.44</Root>");
            reader.PositionOnElement("Root");
            Assert.Equal("44.44", reader.ReadElementContentAsObject());
        }

        [Fact]
        public static void ReadElementContentAsObject6()
        {
            var reader = Utils.CreateFragmentReader("<Root>44.55</Root>");
            reader.PositionOnElement("Root");
            Assert.Equal("44.55", reader.ReadElementContentAsObject());
        }

        [Fact]
        public static void ReadElementContentAsObject7()
        {
            var reader = Utils.CreateFragmentReader("<Root>-005.5</Root>");
            reader.PositionOnElement("Root");
            Assert.Equal("-005.5", reader.ReadElementContentAs(typeof(object), null));
        }

        [Fact]
        public static void ReadElementContentAsObject8()
        {
            var reader = Utils.CreateFragmentReader("<Root>0001</Root>");
            reader.PositionOnElement("Root");
            Assert.Equal("0001", reader.ReadElementContentAs(typeof(object), null));
        }

        [Fact]
        public static void ReadElementContentAsObject9()
        {
            var reader = Utils.CreateFragmentReader("<Root>0</Root>");
            reader.PositionOnElement("Root");
            Assert.Equal("0", reader.ReadElementContentAs(typeof(object), null));
        }
    }
}
