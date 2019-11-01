// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class ArrayTests
    {
        [Fact]
        public static void DeserializationOfTypedArraysByXmlReader1()
        {
            var reader = Utils.CreateFragmentReader("<a b='1  2 3 4'>1  2 3 4</a>");
            reader.PositionOnElement("a");
            reader.Read();
            var values = (int[])reader.ReadContentAs(typeof(int[]), null);

            Assert.Equal(4, values.Length);
            Assert.Equal(1, values[0]);
            Assert.Equal(2, values[1]);
            Assert.Equal(3, values[2]);
            Assert.Equal(4, values[3]);
        }

        [Fact]
        public static void DeserializationOfTypedArraysByXmlReader2()
        {
            var reader = Utils.CreateFragmentReader("<a b='1  2 3 4'>1  2 3 4</a>");
            reader.PositionOnElement("a");
            reader.MoveToAttribute("b");
            var values = (int[])reader.ReadContentAs(typeof(int[]), null);

            Assert.Equal(4, values.Length);
            Assert.Equal(1, values[0]);
            Assert.Equal(2, values[1]);
            Assert.Equal(3, values[2]);
            Assert.Equal(4, values[3]);
        }

        [Fact]
        public static void DeserializationOfTypedArraysByXmlReader3()
        {
            var reader = Utils.CreateFragmentReader("<a b='1  2 3 4'>1  2 3 4</a>");
            reader.PositionOnElement("a");
            reader.Read();
            var values = (object[])reader.ReadContentAs(typeof(string[]), null);

            Assert.Equal(4, values.Length);
            Assert.Equal("1", values[0]);
            Assert.Equal("2", values[1]);
            Assert.Equal("3", values[2]);
            Assert.Equal("4", values[3]);
        }

        [Fact]
        public static void DeserializationOfTypedArraysByXmlReader4()
        {
            var reader = Utils.CreateFragmentReader("<a b='1  2 3 4'>1  2 3 4</a>");
            reader.PositionOnElement("a");
            reader.MoveToAttribute("b");
            var values = (object[])reader.ReadContentAs(typeof(string[]), null);

            Assert.Equal(4, values.Length);
            Assert.Equal("1", values[0]);
            Assert.Equal("2", values[1]);
            Assert.Equal("3", values[2]);
            Assert.Equal("4", values[3]);
        }

        [Fact]
        public static void DeserializationOfTypedArraysByXmlReader5()
        {
            var reader = Utils.CreateFragmentReader("<a b='1  2 3 4'>1  2 3 4</a>");
            reader.PositionOnElement("a");
            reader.Read();
            var values = (object[])reader.ReadContentAs(typeof(string[]), null);

            Assert.Equal(4, values.Length);
            Assert.Equal("1", values[0]);
            Assert.Equal("2", values[1]);
            Assert.Equal("3", values[2]);
            Assert.Equal("4", values[3]);
        }

        [Fact]
        public static void DeserializationOfTypedArraysByXmlReader6()
        {
            var reader = Utils.CreateFragmentReader("<a b='1  2 3 4'>1  2 3 4</a>");
            reader.PositionOnElement("a");
            reader.MoveToAttribute("b");
            var values = (object[])reader.ReadContentAs(typeof(string[]), null);

            Assert.Equal(4, values.Length);
            Assert.Equal("1", values[0]);
            Assert.Equal("2", values[1]);
            Assert.Equal("3", values[2]);
            Assert.Equal("4", values[3]);
        }
    }
}
