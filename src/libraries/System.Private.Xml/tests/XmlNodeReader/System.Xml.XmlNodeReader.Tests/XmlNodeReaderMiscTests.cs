// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Xml.Tests
{
    public class XmlNodeReaderMiscTests
    {
        [Fact]
        public void NodeReaderCloseWithEmptyXml()
        {
            var nodeReader = new XmlNodeReader(new XmlDocument());
            nodeReader.Close();
            Assert.Equal(ReadState.Closed, nodeReader.ReadState);
        }

        [Fact]
        public void NodeReaderSkipWithSimpleXml()
        {
            XmlNodeReader nodeReader = NodeReaderTestHelper.CreateNodeReader("<root atri='val'><child /></root>");
            Assert.True(nodeReader.Read());
            nodeReader.Skip();
            Assert.True(nodeReader.EOF);
            Assert.Equal(ReadState.EndOfFile, nodeReader.ReadState);
            Assert.Equal(XmlNodeType.None, nodeReader.NodeType);
        }

        [Fact]
        public void NodeReaderLookupNamespaceWithEmptyXml()
        {
            var xmlDoc = new XmlDocument();
            var nodeReader = new XmlNodeReader(xmlDoc);
            Assert.Null(nodeReader.LookupNamespace(string.Empty));            
        }

        [Fact]
        public void NodeReaderLookupNamespaceWithSimpleXml()
        {
            XmlNodeReader nodeReader = NodeReaderTestHelper.CreateNodeReader("<root></root>");
            nodeReader.Read();
            var namespaceResolver = nodeReader as IXmlNamespaceResolver;
            Assert.Equal(null, namespaceResolver.LookupNamespace("prefix"));
            Assert.Collection(namespaceResolver.GetNamespacesInScope(XmlNamespaceScope.All)
                , kv => Assert.Equal(kv.Key, "xml"));
            Assert.Empty(namespaceResolver.GetNamespacesInScope(XmlNamespaceScope.Local));
        }

        [Fact]
        public void NodeReaderLookupNamespaceWithValidXml()
        {
            string xml = "<book xmlns:bk='urn:samples'> " +
                   "<title>Pride And Prejudice</title>" +
                   "<bk:genre>novel</bk:genre>" +
                   "</book>";
            XmlNodeReader nodeReader = NodeReaderTestHelper.CreateNodeReader(xml);
            Assert.Equal(string.Empty, nodeReader.LocalName);
            Assert.Equal(string.Empty, nodeReader.Name);
            Assert.Equal(string.Empty, nodeReader.NamespaceURI);
            Assert.Equal(string.Empty, nodeReader.Prefix);
            var namespaceResolver = nodeReader as IXmlNamespaceResolver;
            Assert.Equal(string.Empty, namespaceResolver.LookupNamespace(string.Empty));

            Assert.True(nodeReader.Read());
            Assert.True(nodeReader.Read());
            Assert.True(nodeReader.Read());
            Assert.Equal(null, nodeReader.LookupNamespace(string.Empty));
            namespaceResolver = nodeReader as IXmlNamespaceResolver;
            Assert.Equal(string.Empty, namespaceResolver.LookupNamespace(string.Empty));

            Assert.True(nodeReader.Read());
            Assert.True(nodeReader.Read());
            Assert.Equal("genre", nodeReader.LocalName);
            Assert.Equal("bk:genre", nodeReader.Name);
            Assert.Equal("urn:samples", nodeReader.LookupNamespace(nodeReader.Prefix));
            Assert.Equal("bk", nodeReader.Prefix);

            namespaceResolver = nodeReader as IXmlNamespaceResolver;
            Assert.Equal("bk", namespaceResolver.LookupPrefix("urn:samples"));
            Assert.Equal("urn:samples", namespaceResolver.LookupNamespace("bk"));
            IDictionary<string, string> namespaces = namespaceResolver.GetNamespacesInScope(XmlNamespaceScope.All);
            Assert.True(namespaces.ContainsKey("bk") && namespaces["bk"].Equals("urn:samples"));
            Assert.True(namespaces.ContainsKey("xml"));
        }    
    }
}
