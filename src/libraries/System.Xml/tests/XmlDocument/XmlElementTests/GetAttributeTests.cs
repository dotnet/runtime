// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class GetAttributeTests
    {
        [Fact]
        public static void GetOneAttribute()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<elem1 child1=\"\" child2=\"duu\" child3=\"e1;e2;\" child4=\"a1\" child5=\"goody\"> text node two e1; text node three </elem1>");

            Assert.Equal("duu", xmlDocument.DocumentElement.GetAttribute("child2"));
        }

        [Fact]
        public static void GetEmptyAttribute()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<elem1 child1=\"\" child2=\"duu\" child3=\"e1;e2;\" child4=\"a1\" child5=\"goody\"> text node two e1; text node three </elem1>");

            Assert.Equal(string.Empty, xmlDocument.DocumentElement.GetAttribute("child1"));
        }

        [Fact]
        public static void WrongName()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<elem1 child1=\"\" child2=\"duu\" child3=\"e1;e2;\" child4=\"a1\" child5=\"goody\"> text node two e1; text node three </elem1>");

            Assert.Equal(string.Empty, xmlDocument.DocumentElement.GetAttribute("child7"));
        }

        [Fact]
        public static void WrongNamespace()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<elem1 child1=\"\" child2=\"duu\" child3=\"e1;e2;\" child4=\"a1\" child5=\"goody\"> text node two e1; text node three </elem1>");

            Assert.Equal(string.Empty, xmlDocument.DocumentElement.GetAttribute("child1", "ns2"));
        }

        [Fact]
        public static void SetAttributeAndGetIt()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<elem1 attr1=\"attr1\">This is a test</elem1>");

            xmlDocument.DocumentElement.SetAttribute("attr2", "test");

            Assert.Equal("test", xmlDocument.DocumentElement.GetAttribute("attr2"));
        }

        [Fact]
        public static void RemoveAnAttributeAndGetIt()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<elem1 attr1=\"attr1\">This is a test</elem1>");

            xmlDocument.DocumentElement.RemoveAttribute("attr1");

            Assert.Equal(string.Empty, xmlDocument.DocumentElement.GetAttribute("attr1"));
        }

        [Fact]
        public static void RemoveFirstOfThreeAttributeAndGetIt()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<elem1 attr1=\"attr1\" attr2=\"bar\" attr3=\"foo\">This is a test</elem1>");

            xmlDocument.DocumentElement.RemoveAttribute("attr1");

            Assert.Equal(string.Empty, xmlDocument.DocumentElement.GetAttribute("attr1"));
        }

        [Fact]
        public static void SetAttributeThatExistsAndGetIt()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<elem1 attr1=\"attr1\">This is a test</elem1>");

            xmlDocument.DocumentElement.SetAttribute("attr1", "test");

            Assert.Equal("test", xmlDocument.DocumentElement.GetAttribute("attr1"));
        }
    }
}
