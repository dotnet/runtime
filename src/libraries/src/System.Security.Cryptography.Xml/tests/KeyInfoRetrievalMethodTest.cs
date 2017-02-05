//
// KeyInfoRetrievalMethodTest.cs - Test Cases for KeyInfoRetrievalMethod
//
// Author:
//	Sebastien Pouliot  <sebastien@ximian.com>
//
// (C) 2002, 2003 Motus Technologies Inc. (http://www.motus.com)
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
//
// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

using System.Xml;
using Xunit;

namespace System.Security.Cryptography.Xml.Tests
{

    public class KeyInfoRetrievalMethodTest
    {

        [Fact]
        public void TestNewEmptyKeyNode()
        {
            KeyInfoRetrievalMethod uri1 = new KeyInfoRetrievalMethod();
            Assert.Equal("<RetrievalMethod xmlns=\"http://www.w3.org/2000/09/xmldsig#\" />", (uri1.GetXml().OuterXml));
        }

        [Fact]
        public void TestNewKeyNode()
        {
            string uri = "http://www.go-mono.com/";
            KeyInfoRetrievalMethod uri1 = new KeyInfoRetrievalMethod();
            uri1.Uri = uri;
            XmlElement xel = uri1.GetXml();

            KeyInfoRetrievalMethod uri2 = new KeyInfoRetrievalMethod(uri1.Uri);
            uri2.LoadXml(xel);

            Assert.Equal((uri1.GetXml().OuterXml), (uri2.GetXml().OuterXml));
            Assert.Equal(uri, uri1.Uri);
        }

        [Fact]
        public void TestImportKeyNode()
        {
            string value = "<RetrievalMethod URI=\"http://www.go-mono.com/\" xmlns=\"http://www.w3.org/2000/09/xmldsig#\" />";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(value);

            KeyInfoRetrievalMethod uri1 = new KeyInfoRetrievalMethod();
            uri1.LoadXml(doc.DocumentElement);

            // verify that proper XML is generated (equals to original)
            string s = (uri1.GetXml().OuterXml);
            Assert.Equal(value, s);

            // verify that property is parsed correctly
            Assert.Equal("http://www.go-mono.com/", uri1.Uri);
        }

        [Fact]
        public void InvalidKeyNode1()
        {
            KeyInfoRetrievalMethod uri1 = new KeyInfoRetrievalMethod();
            Assert.Throws<ArgumentNullException>(() => uri1.LoadXml(null));
        }

        [Fact]
        public void InvalidKeyNode2()
        {
            string bad = "<Test></Test>";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(bad);

            KeyInfoRetrievalMethod uri1 = new KeyInfoRetrievalMethod();
            // no exception is thrown
            uri1.LoadXml(doc.DocumentElement);
            AssertCrypto.AssertXmlEquals("invalid", "<RetrievalMethod xmlns=\"http://www.w3.org/2000/09/xmldsig#\" />", (uri1.GetXml().OuterXml));
        }
    }
}
