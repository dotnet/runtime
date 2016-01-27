// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class IndexerTests
    {
        private XmlDocument CreateDocumentWithElement()
        {
            var doc = new XmlDocument();
            doc.AppendChild(doc.CreateElement("root"));
            return doc;
        }

        [Fact]
        public void IndexerByIndexReturnsAttribute()
        {
            XmlDocument doc = CreateDocumentWithElement();
            XmlElement element = doc.DocumentElement;
            XmlAttribute attr1, attr2, attr3;
            attr1 = element.Attributes.Append(doc.CreateAttribute("attr1"));
            attr2 = element.Attributes.Append(doc.CreateAttribute("attr2"));
            attr3 = element.Attributes.Append(doc.CreateAttribute("attr3"));

            XmlAttributeCollection target = element.Attributes;
            // can return first, middle or last attribute
            Assert.Same(attr1, target[0]);
            Assert.Same(attr2, target[1]);
            Assert.Same(attr3, target[2]);
        }

        [Fact]
        public void IndexerByIndexWithOutOfRangeIndexThrows()
        {
            XmlDocument doc = CreateDocumentWithElement();
            XmlElement element = doc.DocumentElement;
            element.Attributes.Append(doc.CreateAttribute("attr"));

            XmlAttributeCollection target = element.Attributes;
            Assert.Throws<IndexOutOfRangeException>(() => target[1]);
        }

        [Fact]
        public void IndexerByLocalNameReturnsAttribute()
        {
            const string attrName1 = "attr1", attrName2 = "attr2", attrName3 = "attr3";
            const string attrUri = "some:uri";
            XmlDocument doc = CreateDocumentWithElement();
            XmlElement element = doc.DocumentElement;
            XmlAttribute attr1, attr2, attr3;
            attr1 = element.Attributes.Append(doc.CreateAttribute(attrName1, attrUri));
            attr2 = element.Attributes.Append(doc.CreateAttribute(attrName2, attrUri));
            attr3 = element.Attributes.Append(doc.CreateAttribute(attrName3, attrUri));

            XmlAttributeCollection target = element.Attributes;
            // can find first, middle or last attribute
            Assert.Same(attr1, target[attrName1]); 
            Assert.Same(attr2, target[attrName2]);
            Assert.Same(attr3, target[attrName3]);
        }

        [Fact]
        public void IndexerByLocalNameReturnsFirstMatch()
        {
            const string attrName = "attr";
            const string attrUri1 = "some:uri1", attrUri2 = "some:uri2", attrUri3 = "some:uri3";
            XmlDocument doc = CreateDocumentWithElement();
            XmlElement element = doc.DocumentElement;
            element.Attributes.Append(doc.CreateAttribute("someAttr", attrUri1));
            XmlAttribute expectedAttr = element.Attributes.Append(doc.CreateAttribute(attrName, attrUri2));
            element.Attributes.Append(doc.CreateAttribute(attrName, attrUri3));

            XmlAttributeCollection target = element.Attributes;
            // finds first match
            Assert.Same(expectedAttr, target[attrName]);
        }

        [Fact]
        public void IndexerByLocalNameIsCaseSensitive()
        {
            const string attrName1 = "attr", attrName2 = "ATTR", attrName3 = "Attr";
            const string attrUri = "some:uri";
            XmlDocument doc = CreateDocumentWithElement();
            XmlElement element = doc.DocumentElement;
            element.Attributes.Append(doc.CreateAttribute(attrName1, attrUri));
            XmlAttribute expectedAttr = element.Attributes.Append(doc.CreateAttribute(attrName2, attrUri));
            element.Attributes.Append(doc.CreateAttribute(attrName3, attrUri));

            XmlAttributeCollection target = element.Attributes;
            Assert.Same(expectedAttr, target[attrName2]);
        }

        [Fact]
        public void IndexerByLocalNameWithNonExistentNameReturnsNull()
        {
            XmlDocument doc = CreateDocumentWithElement();
            XmlElement element = doc.DocumentElement;
            element.Attributes.Append(doc.CreateAttribute("attr1"));
            element.Attributes.Append(doc.CreateAttribute("attr2"));
            element.Attributes.Append(doc.CreateAttribute("attr3"));

            XmlAttributeCollection target = element.Attributes;
            Assert.Null(target["anotherAttr"]);
        }

        [Fact]
        public void IndexerByNameReturnsAttribute()
        {
            const string attrName1 = "attr", attrName2 = "anotherAttr", attrName3 = "attr";
            const string attrUri1 = "some:uri", attrUri2 = "some:uri", attrUri3 = "some:anotherUri";
            XmlDocument doc = CreateDocumentWithElement();
            XmlElement element = doc.DocumentElement;
            XmlAttribute attr1, attr2, attr3;
            attr1 = element.Attributes.Append(doc.CreateAttribute(attrName1, attrUri1));
            attr2 = element.Attributes.Append(doc.CreateAttribute(attrName2, attrUri2));
            attr3 = element.Attributes.Append(doc.CreateAttribute(attrName3, attrUri3));

            XmlAttributeCollection target = element.Attributes;
            // can find first, middle or last attribute
            Assert.Same(attr1, target[attrName1, attrUri1]);
            Assert.Same(attr2, target[attrName2, attrUri2]);
            Assert.Same(attr3, target[attrName3, attrUri3]);
        }

        [Fact]
        public void IndexerByNameIsCaseSensitive()
        {
            const string attrName1 = "attr", attrName2 = "ATTR", attrName3 = "Attr";
            const string attrUri1 = "some:uri", attrUri2 = "SOME:URI", attrUri3 = "Some:Uri";
            XmlDocument doc = CreateDocumentWithElement();
            XmlElement element = doc.DocumentElement;
            element.Attributes.Append(doc.CreateAttribute(attrName1, attrUri1));
            XmlAttribute expectedAttr = element.Attributes.Append(doc.CreateAttribute(attrName2, attrUri2));
            element.Attributes.Append(doc.CreateAttribute(attrName3, attrUri3));

            XmlAttributeCollection target = element.Attributes;
            Assert.Same(expectedAttr, target[attrName2, attrUri2]);
        }

        [Fact]
        public void IndexerByNameWithNonExistentNameReturnsNull()
        {
            XmlDocument doc = CreateDocumentWithElement();
            XmlElement element = doc.DocumentElement;
            element.Attributes.Append(doc.CreateAttribute("attr1", "some:uri1"));
            element.Attributes.Append(doc.CreateAttribute("attr2", "some:uri2"));
            element.Attributes.Append(doc.CreateAttribute("attr3", "some:uri3"));

            XmlAttributeCollection target = element.Attributes;
            Assert.Null(target["anotherAttr", "another:uri"]);
        }
    }
}
