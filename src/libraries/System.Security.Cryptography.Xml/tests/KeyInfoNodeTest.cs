// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// KeyInfoNodeTest.cs - Test Cases for KeyInfoNode
//
// Author:
//  Sebastien Pouliot (spouliot@motus.com)
//
// (C) 2002, 2003 Motus Technologies Inc. (http://www.motus.com)

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
            // No ArgumentNullException is thrown if value == null
            node1.LoadXml(null);
            Assert.Null(node1.Value);
        }

        [Fact]
        public void Constructor_Empty()
        {
            KeyInfoNode node = new KeyInfoNode();
            Assert.Null(node.Value);
        }

        [Fact]
        public void Constructor_WithElement()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<test>data</test>");
            
            KeyInfoNode node = new KeyInfoNode(doc.DocumentElement);
            Assert.NotNull(node.Value);
            Assert.Equal("test", node.Value.LocalName);
        }

        [Fact]
        public void Value_SetNull()
        {
            KeyInfoNode node = new KeyInfoNode();
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<test />");
            node.Value = doc.DocumentElement;
            
            node.Value = null;
            Assert.Null(node.Value);
        }

        [Fact]
        public void GetXml_NullValue()
        {
            KeyInfoNode node = new KeyInfoNode();
            // Throws InvalidOperationException, not returns null
            Assert.Throws<InvalidOperationException>(() => node.GetXml());
        }

        [Fact]
        public void LoadXml_ValidElement()
        {
            string xml = "<CustomElement xmlns=\"http://custom.ns\"><Data>test</Data></CustomElement>";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            KeyInfoNode node = new KeyInfoNode();
            node.LoadXml(doc.DocumentElement);
            Assert.NotNull(node.Value);
            Assert.Equal("CustomElement", node.Value.LocalName);
        }
    }
}
