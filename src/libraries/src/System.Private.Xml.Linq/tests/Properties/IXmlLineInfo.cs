// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using XmlCoreTest.Common;
using Xunit;

namespace System.Xml.Linq.Tests
{
    public class ILineInfoTests
    {
        [Fact]
        public void CastToInterface()
        {
            XDocument doc = new XDocument();
            Assert.IsAssignableFrom<IXmlLineInfo>(doc);
            Assert.IsAssignableFrom<IXmlLineInfo>(doc.CreateReader());
        }

        [Fact]
        public void BaseUriInitial()
        {
            string fileName = Path.Combine("TestData", "XLinq", "config.xml");
            XDocument doc = XDocument.Load(FilePathUtil.getStream(fileName));
            Assert.Equal(doc.CreateReader().BaseURI, doc.BaseUri);

            XElement elem = XElement.Load(FilePathUtil.getStream(fileName));
            Assert.Equal(elem.CreateReader().BaseURI, elem.BaseUri);
        }

        [Fact]
        public void AllNodesTests()
        {
            string fileName = Path.Combine("TestData", "XLinq", "IXmlLineInfoTests", "company-data.xml");
            using (XmlReader r = XmlReader.Create(FilePathUtil.getStream(fileName), new XmlReaderSettings() { DtdProcessing = DtdProcessing.Ignore }))
            {
                XDocument doc = XDocument.Load(r, LoadOptions.SetBaseUri);
                foreach (XNode node in doc.DescendantNodes())
                {
                    using (XmlReader testReader = node.CreateReader())
                    {
                        Assert.Equal(testReader.BaseURI, node.BaseUri);
                    }
                }
            }
        }
    }
}
