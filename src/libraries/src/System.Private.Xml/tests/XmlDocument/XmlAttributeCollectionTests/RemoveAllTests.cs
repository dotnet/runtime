// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
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
