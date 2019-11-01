// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Xml.Linq.Tests
{
    public static class XNodeAxesWithXName
    {
        [Fact]
        public static void NullXNameTestElement()
        {
            XDocument xDoc = TestData.GetDocumentWithContacts();
            XElement xElement = xDoc.Root.Element(null);
            Assert.Null(xElement);
        }

        [Fact]
        public static void NullXNameTestElements()
        {
            XDocument xDoc = TestData.GetDocumentWithContacts();
            XElement xElement = xDoc.Root.Element(null);
            IEnumerable<XNode> xNodeList = xDoc.Root.Elements(null);
            Assert.Equal(0, xNodeList.Count());
        }
    }

    public static class XAttributeAxesWithXName
    {
        [Fact]
        public static void NullXNameTestAttribute()
        {
            XDocument xDoc = TestData.GetDocumentWithContacts();
            XAttribute xAttrib = xDoc.Root.Attribute(null);
            Assert.Null(xAttrib);
        }

        [Fact]
        public static void NullXNameTestAttributes()
        {
            XDocument xDoc = TestData.GetDocumentWithContacts();
            IEnumerable<XAttribute> xAttrib = xDoc.Root.Attributes(null);
            Assert.Equal(0, xAttrib.Count());
        }

        [Fact]
        public static void InvalidXNameTest()
        {
            Assert.Throws<XmlException>(() => { TestData.GetDocumentWithContacts().Root.Attribute("*&^%_#@!"); });
            AssertExtensions.Throws<ArgumentException>(null, () => { TestData.GetDocumentWithContacts().Root.Attribute(""); });
            Assert.Throws<XmlException>(() => { TestData.GetDocumentWithContacts().Root.Attributes("*&^%_#@!"); });
            AssertExtensions.Throws<ArgumentException>(null, () => { TestData.GetDocumentWithContacts().Root.Attributes(""); });
        }
    }

    public static class IEnumerableAttributeAxesWithXName
    {
        [Fact]
        public static void NullXNameTest()
        {
            XDocument xDoc = TestData.GetDocumentWithContacts();
            Assert.Equal(0, xDoc.Root.Elements().Attributes(null).Count());
        }
    }
}
