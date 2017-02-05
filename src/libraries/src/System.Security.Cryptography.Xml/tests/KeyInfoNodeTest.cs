//
// KeyInfoNodeTest.cs - Test Cases for KeyInfoNode
//
// Author:
//	Sebastien Pouliot (spouliot@motus.com)
//
// (C) 2002, 2003 Motus Technologies Inc. (http://www.motus.com)
//
// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

using System.Xml;
using Xunit;

namespace System.Security.Cryptography.Xml.Tests
{

    public class KeyInfoNodeTest
    {

        [Fact]
        public void NewKeyNode()
        {
            string test = "<Test></Test>";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(test);

            KeyInfoNode node1 = new KeyInfoNode();
            node1.Value = doc.DocumentElement;
            XmlElement xel = node1.GetXml();

            KeyInfoNode node2 = new KeyInfoNode(node1.Value);
            node2.LoadXml(xel);

            Assert.Equal((node1.GetXml().OuterXml), (node2.GetXml().OuterXml));
        }

        [Fact]
        public void ImportKeyNode()
        {
            // Note: KeyValue is a valid KeyNode
            string value = "<KeyName xmlns=\"http://www.w3.org/2000/09/xmldsig#\">Mono::</KeyName>";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(value);

            KeyInfoNode node1 = new KeyInfoNode();
            node1.LoadXml(doc.DocumentElement);

            string s = (node1.GetXml().OuterXml);
            Assert.Equal(value, s);
        }

        // well there's no invalid value - unless you read the doc ;-)
        [Fact]
        public void InvalidKeyNode()
        {
            string bad = "<Test></Test>";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(bad);

            KeyInfoNode node1 = new KeyInfoNode();
            // LAMESPEC: No ArgumentNullException is thrown if value == null
            node1.LoadXml(null);
            Assert.Null(node1.Value);
        }
    }
}
