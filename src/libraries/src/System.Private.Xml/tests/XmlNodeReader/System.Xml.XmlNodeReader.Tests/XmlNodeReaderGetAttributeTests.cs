// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class XmlNodeReaderGetAttributeTests
    {
        [Fact]
        public void NodeReaderGetAttributeWithEmptyXml()
        {
            var xmlDocument = new XmlDocument();
            var nodeReader = new XmlNodeReader(xmlDocument);
            Assert.Null(nodeReader.GetAttribute(string.Empty));
            Assert.Null(nodeReader.GetAttribute(string.Empty, string.Empty));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                nodeReader.GetAttribute(2);
            });
        }

        [Fact]
        public void NodeReaderGetAttributeWithValidXml()
        {
            string xml = "<root attr='val'><child /></root>";
            XmlNodeReader nodeReader = NodeReaderTestHelper.CreateNodeReader(xml);
            Assert.True(nodeReader.Read());
            Assert.Equal("val", nodeReader.GetAttribute("attr"));
            Assert.Equal("val", nodeReader.GetAttribute("attr", string.Empty));
            Assert.Equal("val", nodeReader.GetAttribute("attr", null));
            Assert.Equal("val", nodeReader.GetAttribute(0));            
        }
    }
}
