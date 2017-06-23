// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class AppendTests
    {
        private XmlDocument CreateDocumentWithElement()
        {
            var doc = new XmlDocument();
            doc.AppendChild(doc.CreateElement("root"));
            return doc;
        }

        [Fact]
        public void AppendWithAttrWithAnotherOwnerDocumentThrows()
        {
            XmlDocument doc = CreateDocumentWithElement();
            XmlAttribute anotherDocumentAttr = new XmlDocument().CreateAttribute("attr");
            XmlAttributeCollection target = doc.DocumentElement.Attributes;
            AssertExtensions.Throws<ArgumentException>(null, () => target.Append(anotherDocumentAttr));
        }

        [Fact]
        public void AppendDetachesAttrFromCurrentOwnerElement()
        {
            const string attributeName = "movingAttr";
            XmlDocument doc = CreateDocumentWithElement();
            XmlElement element = doc.DocumentElement;
            XmlAttribute attr = element.Attributes.Append(doc.CreateAttribute(attributeName));

            XmlElement destinationElement = doc.CreateElement("anotherElement");
            XmlAttributeCollection target = destinationElement.Attributes;
            target.Append(attr);

            Assert.Same(destinationElement, attr.OwnerElement);
            Assert.False(element.HasAttribute(attributeName));
        }

        [Fact]
        public void AppendRemovesExistingAttribute()
        {
            const string attributeName = "existingAttr";
            const string attributeUri = "existingUri";
            XmlDocument doc = CreateDocumentWithElement();
            XmlElement element = doc.DocumentElement;
            XmlAttribute existingAttr = element.Attributes.Append(doc.CreateAttribute(attributeName, attributeUri));
            XmlAttribute anotherAttr = element.Attributes.Append(doc.CreateAttribute("anotherAttribute"));

            var newAttr = doc.CreateAttribute(attributeName, attributeUri);
            XmlAttributeCollection target = element.Attributes;
            target.Append(newAttr);

            Assert.Equal(2, target.Count);
            Assert.Same(anotherAttr, target[0]);
            Assert.Same(newAttr, target[1]);
        }

        [Fact]
        public void AppendsInsertsInTheEnd()
        {
            const string attributeName = "newAttr";
            XmlDocument doc = CreateDocumentWithElement();
            XmlElement element = doc.DocumentElement;
            element.Attributes.Append(doc.CreateAttribute("anotherAttribute1"));
            element.Attributes.Append(doc.CreateAttribute("anotherAttribute2"));
            var newAttr = doc.CreateAttribute(attributeName);

            XmlAttributeCollection target = element.Attributes;
            target.Append(newAttr);

            Assert.Same(newAttr, target[2]);
        }

        [Fact]
        public void AppendReturnsAddedAttribute()
        {
            XmlDocument doc = CreateDocumentWithElement();
            XmlElement element = doc.DocumentElement;
            var newAttr = doc.CreateAttribute("attr");

            XmlAttributeCollection target = element.Attributes;
            var result = target.Append(newAttr);

            Assert.Same(newAttr, result);
        }
    }
}
