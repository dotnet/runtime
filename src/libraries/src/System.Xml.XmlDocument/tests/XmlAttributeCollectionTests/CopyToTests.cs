// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class CopyToTests
    {
        private XmlDocument CreateDocumentWithElement()
        {
            var doc = new XmlDocument();
            doc.AppendChild(doc.CreateElement("root"));
            return doc;
        }

        [Fact]
        public void CopyToCopiesClonedAttributes()
        {
            XmlDocument doc = CreateDocumentWithElement();
            XmlElement element = doc.DocumentElement;
            XmlAttribute attr1, attr2;
            attr1 = element.Attributes.Append(doc.CreateAttribute("attr1"));
            attr2 = element.Attributes.Append(doc.CreateAttribute("attr2"));
            XmlAttribute[] destinationArray = new XmlAttribute[2];

            XmlAttributeCollection target = element.Attributes;
            target.CopyTo(destinationArray, 0);

            Assert.NotNull(destinationArray[0]);
            Assert.NotSame(attr1, destinationArray[0]);
            Assert.NotSame(element, destinationArray[0].OwnerElement);
            Assert.Equal("attr1", destinationArray[0].LocalName);
            Assert.NotNull(destinationArray[1]);
            Assert.NotSame(attr2, destinationArray[1]);
            Assert.NotSame(element, destinationArray[1].OwnerElement);
            Assert.Equal("attr2", destinationArray[1].LocalName);
        }

        [Fact]
        public void CopyToCopiesClonedAttributesAtSpecifiedLocation()
        {
            XmlDocument doc = CreateDocumentWithElement();
            XmlElement element = doc.DocumentElement;
            XmlAttribute attr1, attr2;
            attr1 = element.Attributes.Append(doc.CreateAttribute("attr1"));
            attr2 = element.Attributes.Append(doc.CreateAttribute("attr2"));
            XmlAttribute[] destinationArray = new XmlAttribute[4];

            XmlAttributeCollection target = element.Attributes;
            target.CopyTo(destinationArray, 2);

            Assert.Null(destinationArray[0]);
            Assert.Null(destinationArray[1]);
            Assert.NotNull(destinationArray[2]);
            Assert.NotNull(destinationArray[3]);
        }
    }
}
