//
// SignatureTest.cs - Test Cases for SignedXml
//
// Author:
//	Sebastien Pouliot <sebastien@ximian.com>
//
// (C) 2002, 2003 Motus Technologies Inc. (http://www.motus.com)
// Copyright (C) 2004-2005 Novell, Inc (http://www.novell.com)
//
// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

using System.Xml;
using Xunit;

namespace System.Security.Cryptography.Xml.Tests
{

    public class SignatureTest
    {

        protected Signature signature;

        public SignatureTest()
        {
            signature = new Signature();
        }

        [Fact]
        public void Signature1()
        {
            // empty - missing SignedInfo
            Assert.Throws<CryptographicException>(() => signature.GetXml());
        }

        [Fact]
        public void Signature2()
        {
            SignedInfo info = new SignedInfo();
            signature.SignedInfo = info;
            info.SignatureMethod = "http://www.w3.org/2000/09/xmldsig#dsa-sha1";
            signature.SignatureValue = new byte[128];
            Assert.Throws<CryptographicException>(() => signature.GetXml());
        }

        [Fact]
        public void Load()
        {
            string expected = "<Signature xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><SignedInfo><CanonicalizationMethod Algorithm=\"http://www.w3.org/TR/2001/REC-xml-c14n-20010315\" /><SignatureMethod Algorithm=\"http://www.w3.org/2000/09/xmldsig#rsa-sha1\" /><Reference URI=\"#MyObjectId\"><DigestMethod Algorithm=\"http://www.w3.org/2000/09/xmldsig#sha1\" /><DigestValue>/Vvq6sXEVbtZC8GwNtLQnGOy/VI=</DigestValue></Reference></SignedInfo><SignatureValue>A6XuE8Cy9iOffRXaW9b0+dUcMUJQnlmwLsiqtQnADbCtZXnXAaeJ6nGnQ4Mm0IGi0AJc7/2CoJReXl7iW4hltmFguG1e3nl0VxCyCTHKGOCo1u8R3K+B1rTaenFbSxs42EM7/D9KETsPlzfYfis36yM3PqatiCUOsoMsAiMGzlc=</SignatureValue><KeyInfo><KeyValue xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><RSAKeyValue><Modulus>tI8QYIpbG/m6JLyvP+S3X8mzcaAIayxomyTimSh9UCpEucRnGvLw0P73uStNpiF7wltTZA1HEsv+Ha39dY/0j/Wiy3RAodGDRNuKQao1wu34aNybZ673brbsbHFUfw/o7nlKD2xO84fbajBZmKtBBDy63NHt+QL+grSrREPfCTM=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue></KeyValue></KeyInfo><Object Id=\"MyObjectId\"><MyElement xmlns=\"samples\">This is some text</MyElement></Object></Signature>";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(expected);
            signature.LoadXml(doc.DocumentElement);
            string result = signature.GetXml().OuterXml;
            AssertCrypto.AssertXmlEquals("Load", expected, result);
        }

        [Fact]
        public void LoadXmlMalformed1()
        {
            SignedXml s = new SignedXml();
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<root/>");
            Assert.Throws<CryptographicException>(() => s.LoadXml(doc.DocumentElement));
        }

        [Fact]
        public void LoadXmlMalformed2()
        {
            SignedXml s = new SignedXml();
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<ds:Signature xmlns:ds='http://www.w3.org/2000/09/xmldsig#'><foo/><bar/></ds:Signature>");
            Assert.Throws<CryptographicException>(() => s.LoadXml(doc.DocumentElement));
        }
    }
}
