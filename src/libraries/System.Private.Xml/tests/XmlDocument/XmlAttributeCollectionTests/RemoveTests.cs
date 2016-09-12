// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class RemoveTests
    {
        private XmlDocument CreateDocumentWithElement()
        {
            var doc = new XmlDocument();
            doc.AppendChild(doc.CreateElement("root"));
            return doc;
        }

        [Fact]
        public void RemoveCanDeleteFirstAttr()
        {
            XmlDocument doc = CreateDocumentWithElement();
            XmlElement element = doc.DocumentElement;
            XmlAttribute attr1, attr2, attr3;
            attr1 = element.Attributes.Append(doc.CreateAttribute("attr1"));
            attr2 = element.Attributes.Append(doc.CreateAttribute("attr2"));
            attr3 = element.Attributes.Append(doc.CreateAttribute("attr3"));

            XmlAttributeCollection target = element.Attributes;
            target.Remove(attr1);

            Assert.Equal(2, target.Count);
            Assert.Same(attr2, target[0]);
            Assert.Same(attr3, target[1]);
        }

        [Fact]
        public void RemoveCanDeleteMiddleAttr()
        {
            XmlDocument doc = CreateDocumentWithElement();
            XmlElement element = doc.DocumentElement;
            XmlAttribute attr1, attr2, attr3;
            attr1 = element.Attributes.Append(doc.CreateAttribute("attr1"));
            attr2 = element.Attributes.Append(doc.CreateAttribute("attr2"));
            attr3 = element.Attributes.Append(doc.CreateAttribute("attr3"));

            XmlAttributeCollection target = element.Attributes;
            target.Remove(attr2);

            Assert.Equal(2, target.Count);
            Assert.Same(attr1, target[0]);
            Assert.Same(attr3, target[1]);
        }

        [Fact]
        public void RemoveCanDeleteLastAttr()
        {
            XmlDocument doc = CreateDocumentWithElement();
            XmlElement element = doc.DocumentElement;
            XmlAttribute attr1, attr2, attr3;
            attr1 = element.Attributes.Append(doc.CreateAttribute("attr1"));
            attr2 = element.Attributes.Append(doc.CreateAttribute("attr2"));
            attr3 = element.Attributes.Append(doc.CreateAttribute("attr3"));

            XmlAttributeCollection target = element.Attributes;
            target.Remove(attr3);

            Assert.Equal(2, target.Count);
            Assert.Same(attr1, target[0]);
            Assert.Same(attr2, target[1]);
        }

        [Fact]
        public void RemoveWhenNotFoundAttrReturnsNull()
        {
            XmlDocument doc = CreateDocumentWithElement();
            XmlElement element = doc.DocumentElement;
            XmlAttribute attr1, attr2, attr3;
            attr1 = element.Attributes.Append(doc.CreateAttribute("attr1"));
            attr2 = element.Attributes.Append(doc.CreateAttribute("attr2"));
            attr3 = element.Attributes.Append(doc.CreateAttribute("attr3"));

            XmlAttributeCollection target = element.Attributes;
            Assert.Null(target.Remove(doc.CreateAttribute("anotherAttr")));
        }

        [Fact]
        public void RemoveReturnsRemovedAttr()
        {
            XmlDocument doc = CreateDocumentWithElement();
            XmlElement element = doc.DocumentElement;
            XmlAttribute attr1, attr2, attr3;
            attr1 = element.Attributes.Append(doc.CreateAttribute("attr1"));
            attr2 = element.Attributes.Append(doc.CreateAttribute("attr2"));
            attr3 = element.Attributes.Append(doc.CreateAttribute("attr3"));

            XmlAttributeCollection target = element.Attributes;
            var actualAttr = target.Remove(attr2);
            Assert.Same(attr2, actualAttr);
        }
    }
}
