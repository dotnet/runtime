// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml;
using Xunit;

namespace XmlDocumentTests.XmlAttributeCollectionTests
{
    public class RemoveAtTests
    {
        private XmlDocument CreateDocumentWithElement()
        {
            var doc = new XmlDocument();
            doc.AppendChild(doc.CreateElement("root"));
            return doc;
        }

        [Fact]
        public void RemoveAtWithNegativeIndexReturnsNull()
        {
            XmlDocument doc = CreateDocumentWithElement();
            XmlElement element = doc.DocumentElement;
            XmlAttribute attr1, attr2, attr3;
            attr1 = element.Attributes.Append(doc.CreateAttribute("attr1"));
            attr2 = element.Attributes.Append(doc.CreateAttribute("attr2"));
            attr3 = element.Attributes.Append(doc.CreateAttribute("attr3"));

            XmlAttributeCollection target = element.Attributes;
            Assert.Null(target.RemoveAt(-1));
        }

        [Fact]
        public void RemoveAtWithInvalidIndexReturnsNull()
        {
            XmlDocument doc = CreateDocumentWithElement();
            XmlElement element = doc.DocumentElement;
            XmlAttribute attr1, attr2, attr3;
            attr1 = element.Attributes.Append(doc.CreateAttribute("attr1"));
            attr2 = element.Attributes.Append(doc.CreateAttribute("attr2"));
            attr3 = element.Attributes.Append(doc.CreateAttribute("attr3"));

            XmlAttributeCollection target = element.Attributes;
            Assert.Null(target.RemoveAt(3));
        }

        [Fact]
        public void RemoveAtDeletesAttr()
        {
            XmlDocument doc = CreateDocumentWithElement();
            XmlElement element = doc.DocumentElement;
            XmlAttribute attr1, attr2;
            attr1 = element.Attributes.Append(doc.CreateAttribute("attr1"));
            element.Attributes.Append(doc.CreateAttribute("attr2"));
            attr2 = element.Attributes.Append(doc.CreateAttribute("attr3"));

            XmlAttributeCollection target = element.Attributes;
            target.RemoveAt(1);

            Assert.Equal(2, target.Count);
            Assert.Same(attr1, target[0]);
            Assert.Same(attr2, target[1]);
        }

        [Fact]
        public void RemoveAtReturnsDeletedAttr()
        {
            XmlDocument doc = CreateDocumentWithElement();
            XmlElement element = doc.DocumentElement;
            XmlAttribute attr1, attr2, attr3;
            attr1 = element.Attributes.Append(doc.CreateAttribute("attr1"));
            attr2 = element.Attributes.Append(doc.CreateAttribute("attr2"));
            attr3 = element.Attributes.Append(doc.CreateAttribute("attr3"));

            XmlAttributeCollection target = element.Attributes;
            var actualAttr = target.RemoveAt(1);

            Assert.Same(attr2, actualAttr);
        }
    }
}
