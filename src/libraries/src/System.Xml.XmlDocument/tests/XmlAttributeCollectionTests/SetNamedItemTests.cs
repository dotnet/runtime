// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Xml;
using Xunit;

namespace XmlDocumentTests.XmlAttributeCollectionTests
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
            Assert.Throws<ArgumentException>(() => target.SetNamedItem(element));
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
