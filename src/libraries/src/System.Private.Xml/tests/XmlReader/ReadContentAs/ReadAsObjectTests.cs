// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class ObjectTests
    {
        [Fact]
        public static void ReadContentAsObject1()
        {
            var reader = Utils.CreateFragmentReader("<Root>-005.5</Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal("-005.5", reader.ReadContentAsObject());
        }

        [Fact]
        public static void ReadContentAsObject10()
        {
            var reader = Utils.CreateFragmentReader("<Root>9999.9</Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal("9999.9", reader.ReadContentAs(typeof(object), null));
        }

        [Fact]
        public static void ReadContentAsObject11()
        {
            var reader = Utils.CreateFragmentReader("<Root>44.44</Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal("44.44", reader.ReadContentAs(typeof(object), null));
        }

        [Fact]
        public static void ReadContentAsObject12()
        {
            var reader = Utils.CreateFragmentReader("<Root>44.55</Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal("44.55", reader.ReadContentAs(typeof(object), null));
        }

        [Fact]
        public static void ReadContentAsObject2()
        {
            var reader = Utils.CreateFragmentReader("<Root>0001</Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal("0001", reader.ReadContentAsObject());
        }

        [Fact]
        public static void ReadContentAsObject3()
        {
            var reader = Utils.CreateFragmentReader("<Root>0</Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal("0", reader.ReadContentAsObject());
        }

        [Fact]
        public static void ReadContentAsObject4()
        {
            var reader = Utils.CreateFragmentReader("<Root>9999.9</Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal("9999.9", reader.ReadContentAsObject());
        }

        [Fact]
        public static void ReadContentAsObject5()
        {
            var reader = Utils.CreateFragmentReader("<Root>44.44</Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal("44.44", reader.ReadContentAsObject());
        }

        [Fact]
        public static void ReadContentAsObject6()
        {
            var reader = Utils.CreateFragmentReader("<Root>44.55</Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal("44.55", reader.ReadContentAsObject());
        }

        [Fact]
        public static void ReadContentAsObject7()
        {
            var reader = Utils.CreateFragmentReader("<Root>-005.5</Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal("-005.5", reader.ReadContentAs(typeof(object), null));
        }

        [Fact]
        public static void ReadContentAsObject8()
        {
            var reader = Utils.CreateFragmentReader("<Root>0001</Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal("0001", reader.ReadContentAs(typeof(object), null));
        }

        [Fact]
        public static void ReadContentAsObject9()
        {
            var reader = Utils.CreateFragmentReader("<Root>0</Root>");
            reader.PositionOnElement("Root");
            reader.Read();
            Assert.Equal("0", reader.ReadContentAs(typeof(object), null));
        }
    }
}
