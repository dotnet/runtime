// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml;
using Xunit;

namespace XmlDocumentTests.XmlAttributeCollectionTests
{
    public class RemoveAllTests
    {
        private XmlDocument CreateDocumentWithElement()
        {
            var doc = new XmlDocument();
            doc.AppendChild(doc.CreateElement("root"));
            return doc;
        }

        [Fact]
        public void RemoveAllRemovesAllAttributes()
        {
            XmlDocument doc = CreateDocumentWithElement();
            XmlElement element = doc.DocumentElement;
            element.Attributes.Append(doc.CreateAttribute("attr1"));
            element.Attributes.Append(doc.CreateAttribute("attr2"));
            element.Attributes.Append(doc.CreateAttribute("attr3"));

            XmlAttributeCollection target = element.Attributes;
            element.RemoveAll();

            Assert.Equal(0, target.Count);
        }
    }
}
