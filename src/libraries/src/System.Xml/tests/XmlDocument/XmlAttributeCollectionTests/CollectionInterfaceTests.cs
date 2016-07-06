// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using Xunit;

namespace System.Xml.Tests
{
    public class CollectionInterfaceTests
    {
        private XmlDocument CreateDocumentWithElement()
        {
            var doc = new XmlDocument();
            doc.AppendChild(doc.CreateElement("root"));
            return doc;
        }

        [Fact]
        public void CopyToCopiesReferences()
        {
            XmlDocument doc = CreateDocumentWithElement();
            XmlElement element = doc.DocumentElement;
            XmlAttribute attr1, attr2, attr3;
            attr1 = element.Attributes.Append(doc.CreateAttribute("attr1"));
            attr2 = element.Attributes.Append(doc.CreateAttribute("attr2"));
            attr3 = element.Attributes.Append(doc.CreateAttribute("attr3"));
            XmlAttribute[] destinationArray = new XmlAttribute[3];

            ICollection target = element.Attributes;
            target.CopyTo(destinationArray, 0);

            Assert.Same(attr1, destinationArray[0]);
            Assert.Same(attr2, destinationArray[1]);
            Assert.Same(attr3, destinationArray[2]);
        }

        [Fact]
        public void CopyToCopiesReferencesAtSpecifiedIndex()
        {
            XmlDocument doc = CreateDocumentWithElement();
            XmlElement element = doc.DocumentElement;
            XmlAttribute attr1, attr2, attr3;
            attr1 = element.Attributes.Append(doc.CreateAttribute("attr1"));
            attr2 = element.Attributes.Append(doc.CreateAttribute("attr2"));
            attr3 = element.Attributes.Append(doc.CreateAttribute("attr3"));
            XmlAttribute[] destinationArray = new XmlAttribute[5];

            ICollection target = element.Attributes;
            target.CopyTo(destinationArray, 2);

            Assert.Null(destinationArray[0]);
            Assert.Null(destinationArray[1]);
            Assert.Same(attr1, destinationArray[2]);
            Assert.Same(attr2, destinationArray[3]);
            Assert.Same(attr3, destinationArray[4]);
        }

        [Fact]
        public void IsSyncronizedGetsFalse()
        {
            XmlDocument doc = CreateDocumentWithElement();
            XmlElement element = doc.DocumentElement;
            ICollection target = element.Attributes;
            Assert.False(target.IsSynchronized);
        }

        [Fact]
        public void SyncRootGetsObject()
        {
            XmlDocument doc = CreateDocumentWithElement();
            XmlElement element = doc.DocumentElement;
            ICollection target = element.Attributes;
            Assert.NotNull(target.SyncRoot);
        }

        [Fact]
        public void SyncRootGetsSameObject()
        {
            XmlDocument doc = CreateDocumentWithElement();
            XmlElement element = doc.DocumentElement;
            ICollection target = element.Attributes;
            var syncRoot1 = target.SyncRoot;
            Assert.Same(syncRoot1, target.SyncRoot);
        }

        [Fact]
        public void CountGetsCount()
        {
            XmlDocument doc = CreateDocumentWithElement();
            XmlElement element = doc.DocumentElement;
            element.Attributes.Append(doc.CreateAttribute("attr1"));
            element.Attributes.Append(doc.CreateAttribute("attr2"));
            element.Attributes.Append(doc.CreateAttribute("attr3"));
            ICollection target = element.Attributes;
            Assert.Equal(3, target.Count);
        }
    }
}
