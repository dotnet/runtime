// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class SetNamedItemTests
    {
        private XmlDocument CreateDocumentWithElement()
        {
            var doc = new XmlDocument();
            doc.AppendChild(doc.CreateElement("root"));
            return doc;
        }

        [Fact]
        public void SetNamedItemWithNullReturnsNull()
        {
            XmlDocument doc = CreateDocumentWithElement();
            XmlElement element = doc.DocumentElement;
            XmlAttributeCollection target = element.Attributes;
            Assert.Null(target.SetNamedItem(null));
        }

        [Fact]
        public void SetNamedItemWithNonAttributeThrows()
        {
            XmlDocument doc = CreateDocumentWithElement();
            XmlElement element = doc.DocumentElement;
            XmlAttributeCollection target = element.Attributes;
            AssertExtensions.Throws<ArgumentException>(null, () => target.SetNamedItem(element));
        }

        [Fact]
        public void SetNamedItemWithNewNameAppendsAndReturnsAdded()
        {
            XmlDocument doc = CreateDocumentWithElement();
            XmlElement element = doc.DocumentElement;
            element.Attributes.Append(doc.CreateAttribute("attr", "anotherUri"));
            element.Attributes.Append(doc.CreateAttribute("anotherAttr", "uri"));
            XmlAttribute expectedAttr = doc.CreateAttribute("attr", "uri");

            XmlAttributeCollection target = element.Attributes;
            XmlNode actualResult = target.SetNamedItem(expectedAttr);

            Assert.Equal(3, target.Count);
            Assert.Same(expectedAttr, target[2]);
            Assert.Same(expectedAttr, actualResult);
        }

        [Fact]
        public void SetNamedItemWithExistingNameReplacesAndReturnsRemoved()
        {
            XmlDocument doc = CreateDocumentWithElement();
            XmlElement element = doc.DocumentElement;
            element.Attributes.Append(doc.CreateAttribute("attr", "anotherUri"));
            XmlAttribute expectedRemoved = doc.CreateAttribute("attr", "uri");
            element.Attributes.Append(expectedRemoved);
            element.Attributes.Append(doc.CreateAttribute("anotherAttr", "uri"));
            XmlAttribute expectedAdded = doc.CreateAttribute("attr", "uri");

            XmlAttributeCollection target = element.Attributes;
            XmlNode actualResult = target.SetNamedItem(expectedAdded);

            Assert.Equal(3, target.Count);
            Assert.Same(expectedAdded, target[1]);
            Assert.Same(expectedRemoved, actualResult);
        }
    }
}
