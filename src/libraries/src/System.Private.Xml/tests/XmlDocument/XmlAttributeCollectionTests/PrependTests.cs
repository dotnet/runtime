// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class PrependTests
    {
        private XmlDocument CreateDocumentWithElement()
        {
            var doc = new XmlDocument();
            doc.AppendChild(doc.CreateElement("root"));
            return doc;
        }

        [Fact]
        public void PrependWithAttrWithAnotherOwnerDocumentThrows()
        {
            XmlDocument doc = CreateDocumentWithElement();
            XmlAttribute anotherDocumentAttr = new XmlDocument().CreateAttribute("attr");
            XmlAttributeCollection target = doc.DocumentElement.Attributes;
            AssertExtensions.Throws<ArgumentException>(null, () => target.Prepend(anotherDocumentAttr));
        }

        [Fact]
        public void PrependDetachesAttrFromCurrentOwnerElement()
        {
            const string attributeName = "movingAttr";
            XmlDocument doc = CreateDocumentWithElement();
            XmlElement element = doc.DocumentElement;
            XmlAttribute attr = element.Attributes.Append(doc.CreateAttribute(attributeName));
            // assert on implicitly set preconditions 
            Assert.Same(element, attr.OwnerElement);
            Assert.True(element.HasAttribute(attributeName));

            XmlElement destinationElement = doc.CreateElement("anotherElement");
            XmlAttributeCollection target = destinationElement.Attributes;
            target.Prepend(attr);

            Assert.Same(destinationElement, attr.OwnerElement);
            Assert.False(element.HasAttribute(attributeName));
        }

        [Fact]
        public void PrependRemovesExistingAttribute()
        {
            const string attributeName = "existingAttr";
            const string attributeUri = "existingUri";
            XmlDocument doc = CreateDocumentWithElement();
            XmlElement element = doc.DocumentElement;
            XmlAttribute anotherAttr = element.Attributes.Append(doc.CreateAttribute("anotherAttribute"));
            XmlAttribute existingAttr = element.Attributes.Append(doc.CreateAttribute(attributeName, attributeUri));
            // assert on implicitly set preconditions
            Assert.Same(anotherAttr, element.Attributes[0]);

            var newAttr = doc.CreateAttribute(attributeName, attributeUri);
            XmlAttributeCollection target = element.Attributes;
            target.Prepend(newAttr);

            Assert.Equal(2, target.Count);
            Assert.Same(newAttr, target[0]);
            Assert.Same(anotherAttr, target[1]);
        }

        [Fact]
        public void PrependInsertsAtPosition0()
        {
            const string attributeName = "newAttr";
            XmlDocument doc = CreateDocumentWithElement();
            XmlElement element = doc.DocumentElement;
            element.Attributes.Append(doc.CreateAttribute("anotherAttribute"));

            var newAttr = doc.CreateAttribute(attributeName);
            XmlAttributeCollection target = element.Attributes;
            target.Prepend(newAttr);

            Assert.Same(newAttr, target[0]);
        }

        [Fact]
        public void PrependReturnsAddedAttribute()
        {
            XmlDocument doc = CreateDocumentWithElement();
            XmlElement element = doc.DocumentElement;
            var newAttr = doc.CreateAttribute("attr");

            XmlAttributeCollection target = element.Attributes;
            var result = target.Prepend(newAttr);

            Assert.Same(newAttr, result);
        }

    }
}
