// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class InsertBeforeTests
    {
        private XmlDocument CreateDocumentWithElement()
        {
            var doc = new XmlDocument();
            doc.AppendChild(doc.CreateElement("root"));
            return doc;
        }

        [Fact]
        public void InsertBeforeWithSameRefAttrKeepsOrderIntactAndReturnsTheArgument()
        {
            XmlDocument doc = CreateDocumentWithElement();
            XmlElement element = doc.DocumentElement;
            XmlAttribute attr1, attr2, attr3;
            attr1 = element.Attributes.Append(doc.CreateAttribute("attr1"));
            attr2 = element.Attributes.Append(doc.CreateAttribute("attr2"));
            attr3 = element.Attributes.Append(doc.CreateAttribute("attr3"));

            XmlAttributeCollection target = element.Attributes;
            XmlAttribute result = target.InsertBefore(attr2, attr2);

            Assert.Equal(3, target.Count);
            Assert.Same(attr1, target[0]);
            Assert.Same(attr2, target[1]);
            Assert.Same(attr3, target[2]);
            Assert.Same(attr2, result);
        }

        [Fact]
        public void InsertBeforeWithNullRefAttrAddsToTheEnd()
        {
            XmlDocument doc = CreateDocumentWithElement();
            XmlElement element = doc.DocumentElement;
            XmlAttribute attr1, attr2, attr3;
            attr1 = element.Attributes.Append(doc.CreateAttribute("attr1"));
            attr2 = element.Attributes.Append(doc.CreateAttribute("attr2"));
            attr3 = doc.CreateAttribute("attr3");

            XmlAttributeCollection target = element.Attributes;
            target.InsertBefore(attr3, null);

            Assert.Equal(3, target.Count);
            Assert.Same(attr1, target[0]);
            Assert.Same(attr2, target[1]);
            Assert.Same(attr3, target[2]);
        }

        [Fact]
        public void InsertBeforeWithRefAttrWithAnotherOwnerElementThrows()
        {
            XmlDocument doc = CreateDocumentWithElement();
            XmlElement element = doc.DocumentElement;
            XmlAttribute newAttr = doc.CreateAttribute("newAttr");
            XmlElement anotherElement = doc.CreateElement("anotherElement");
            XmlAttribute anotherOwnerElementAttr = anotherElement.SetAttributeNode("anotherOwnerElementAttr", string.Empty);

            XmlAttributeCollection target = element.Attributes;
            AssertExtensions.Throws<ArgumentException>(null, () => target.InsertBefore(newAttr, anotherOwnerElementAttr));
        }

        [Fact]
        public void InsertBeforeWithAttrWithAnotherOwnerDocumentThrows()
        {
            XmlDocument doc = CreateDocumentWithElement();
            XmlElement element = doc.DocumentElement;
            XmlAttribute existingAttr = doc.CreateAttribute("existingAttr");
            element.Attributes.Append(existingAttr);
            XmlAttribute anotherOwnerDocumentAttr = new XmlDocument().CreateAttribute("anotherOwnerDocumentAttr");

            XmlAttributeCollection target = element.Attributes;
            AssertExtensions.Throws<ArgumentException>(null, () => target.InsertBefore(anotherOwnerDocumentAttr, existingAttr));
        }

        [Fact]
        public void InsertBeforeDetachesAttrFromCurrentOwnerElement()
        {
            const string attributeName = "movingAttr";
            XmlDocument doc = CreateDocumentWithElement();
            XmlElement element = doc.DocumentElement;
            XmlAttribute attr = element.Attributes.Append(doc.CreateAttribute(attributeName));
            // assert on implicitly set preconditions 
            Assert.Same(element, attr.OwnerElement);
            Assert.True(element.HasAttribute(attributeName));

            XmlElement destinationElement = doc.CreateElement("anotherElement");
            XmlAttribute refAttr = destinationElement.Attributes.Append(doc.CreateAttribute("anotherAttr"));
            XmlAttributeCollection target = destinationElement.Attributes;
            target.InsertBefore(attr, refAttr);

            Assert.Same(destinationElement, attr.OwnerElement);
            Assert.False(element.HasAttribute(attributeName));
        }

        [Fact]
        public void InsertBeforeCanInsertBeforeTheFirst()
        {
            XmlDocument doc = CreateDocumentWithElement();
            XmlElement element = doc.DocumentElement;
            XmlAttribute refAttr = element.Attributes.Append(doc.CreateAttribute("attr1", "some:uri1"));
            element.Attributes.Append(doc.CreateAttribute("attr2", "some:uri2"));
            element.Attributes.Append(doc.CreateAttribute("attr3", "some:uri3"));
            XmlAttribute newAttr = doc.CreateAttribute("newAttr");

            XmlAttributeCollection target = element.Attributes;
            target.InsertBefore(newAttr, refAttr);

            Assert.Equal(4, target.Count);
            Assert.Same(newAttr, target[0]);
            Assert.Same(refAttr, target[1]);
        }

        [Fact]
        public void InsertBeforeCanInsertBeforeTheLast()
        {
            XmlDocument doc = CreateDocumentWithElement();
            XmlElement element = doc.DocumentElement;
            element.Attributes.Append(doc.CreateAttribute("attr1", "some:uri1"));
            element.Attributes.Append(doc.CreateAttribute("attr2", "some:uri2"));
            XmlAttribute refAttr = element.Attributes.Append(doc.CreateAttribute("attr3", "some:uri3"));
            XmlAttribute newAttr = doc.CreateAttribute("newAttr");

            XmlAttributeCollection target = element.Attributes;
            target.InsertBefore(newAttr, refAttr);

            Assert.Equal(4, target.Count);
            Assert.Same(newAttr, target[2]);
            Assert.Same(refAttr, target[3]);
        }

        [Fact]
        public void InsertBeforeCanInsertInTheMiddle()
        {
            XmlDocument doc = CreateDocumentWithElement();
            XmlElement element = doc.DocumentElement;
            element.Attributes.Append(doc.CreateAttribute("attr1", "some:uri1"));
            XmlAttribute refAttr = element.Attributes.Append(doc.CreateAttribute("attr2", "some:uri2"));
            element.Attributes.Append(doc.CreateAttribute("attr3", "some:uri3"));
            XmlAttribute newAttr = doc.CreateAttribute("newAttr");

            XmlAttributeCollection target = element.Attributes;
            target.InsertBefore(newAttr, refAttr);

            Assert.Equal(4, target.Count);
            Assert.Same(newAttr, target[1]);
            Assert.Same(refAttr, target[2]);
        }

        [Fact]
        public void InsertBeforeRemovesDupAttrAfterTheRef()
        {
            const string attributeName = "existingAttr";
            const string attributeUri = "some:existingUri";
            XmlDocument doc = CreateDocumentWithElement();
            XmlElement element = doc.DocumentElement;
            XmlAttribute refAttr = element.Attributes.Append(doc.CreateAttribute("attr1", "some:uri1"));
            element.Attributes.Append(doc.CreateAttribute(attributeName, attributeUri)); //dup
            XmlAttribute anotherAttr = element.Attributes.Append(doc.CreateAttribute("attr2", "some:uri2"));
            XmlAttribute newAttr = doc.CreateAttribute(attributeName, attributeUri);

            XmlAttributeCollection target = element.Attributes;
            target.InsertBefore(newAttr, refAttr);

            Assert.Equal(3, target.Count);
            Assert.Same(newAttr, target[0]);
            Assert.Same(refAttr, target[1]);
            Assert.Same(anotherAttr, target[2]);
        }

        [Fact]
        public void InsertBeforeRemovesDupAttrBeforeTheRef()
        {
            const string attributeName = "existingAttr";
            const string attributeUri = "some:existingUri";
            XmlDocument doc = CreateDocumentWithElement();
            XmlElement element = doc.DocumentElement;
            XmlAttribute anotherAttr = element.Attributes.Append(doc.CreateAttribute("attr1", "some:uri1"));
            element.Attributes.Append(doc.CreateAttribute(attributeName, attributeUri)); //dup
            XmlAttribute refAttr = element.Attributes.Append(doc.CreateAttribute("attr2", "some:uri2"));
            XmlAttribute newAttr = doc.CreateAttribute(attributeName, attributeUri);

            XmlAttributeCollection target = element.Attributes;
            target.InsertBefore(newAttr, refAttr);

            Assert.Equal(3, target.Count);
            Assert.Same(anotherAttr, target[0]);
            Assert.Same(newAttr, target[1]);
            Assert.Same(refAttr, target[2]);
        }

        [Fact]
        public void InsertBeforeRemovesDupRefAttr()
        {
            const string attributeName = "existingAttr";
            const string attributeUri = "some:existingUri";
            XmlDocument doc = CreateDocumentWithElement();
            XmlElement element = doc.DocumentElement;
            XmlAttribute refAttr = element.Attributes.Append(doc.CreateAttribute(attributeName, attributeUri)); //dup
            XmlAttribute anotherAttr1 = element.Attributes.Append(doc.CreateAttribute("attr1", "some:uri1"));
            XmlAttribute anotherAttr2 = element.Attributes.Append(doc.CreateAttribute("attr2", "some:uri2"));
            XmlAttribute newAttr = doc.CreateAttribute(attributeName, attributeUri);

            XmlAttributeCollection target = element.Attributes;
            target.InsertBefore(newAttr, refAttr);

            Assert.Equal(3, target.Count);
            Assert.Same(newAttr, target[0]);
            Assert.Same(anotherAttr1, target[1]);
            Assert.Same(anotherAttr2, target[2]);
        }

        [Fact]
        public void InsertBeforeReturnsInsertedAttr()
        {
            XmlDocument doc = CreateDocumentWithElement();
            XmlElement element = doc.DocumentElement;
            XmlAttribute refAttr = element.Attributes.Append(doc.CreateAttribute("attr1", "some:uri1"));
            XmlAttribute newAttr = doc.CreateAttribute("attr2", "some:uri2");

            XmlAttributeCollection target = element.Attributes;
            Assert.Same(newAttr, target.InsertBefore(newAttr, refAttr));
        }
    }
}
