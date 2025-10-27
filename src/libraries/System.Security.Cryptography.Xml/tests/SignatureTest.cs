// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// SignatureTest.cs - Test Cases for SignedXml
//
// Author:
//  Sebastien Pouliot <sebastien@ximian.com>
//
// (C) 2002, 2003 Motus Technologies Inc. (http://www.motus.com)
// Copyright (C) 2004-2005 Novell, Inc (http://www.novell.com)

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

        [Fact]
        public void SignatureProperties()
        {
            Signature sig = new Signature();
            Assert.Null(sig.Id);
            Assert.NotNull(sig.ObjectList);
            Assert.Equal(0, sig.ObjectList.Count);
            Assert.NotNull(sig.KeyInfo);  // Auto-initialized
            Assert.Equal(0, sig.KeyInfo.Count);
            Assert.Null(sig.SignatureValue);
            Assert.Null(sig.SignedInfo);
        }

        [Fact]
        public void SignatureIdProperty()
        {
            Signature sig = new Signature();
            sig.Id = "sig-id";
            Assert.Equal("sig-id", sig.Id);
        }

        [Fact]
        public void SignatureWithKeyInfo()
        {
            Signature sig = new Signature();
            sig.SignedInfo = new SignedInfo();
            sig.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigC14NTransformUrl;
            sig.SignedInfo.SignatureMethod = SignedXml.XmlDsigRSASHA256Url;
            
            // Add a reference (required for GetXml)
            Reference reference = new Reference();
            reference.DigestMethod = SignedXml.XmlDsigSHA256Url;
            reference.DigestValue = new byte[] { 1, 2, 3, 4 };
            sig.SignedInfo.AddReference(reference);
            
            sig.SignatureValue = new byte[] { 1, 2, 3, 4 };
            sig.KeyInfo.AddClause(new KeyInfoName("TestKey"));

            XmlElement xml = sig.GetXml();
            Assert.NotNull(xml);
            Assert.Equal("Signature", xml.LocalName);
        }

        [Fact]
        public void SignatureWithObjects()
        {
            Signature sig = new Signature();
            sig.SignedInfo = new SignedInfo();
            sig.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigC14NTransformUrl;
            sig.SignedInfo.SignatureMethod = SignedXml.XmlDsigRSASHA256Url;

            // Add a reference (required for GetXml)
            Reference reference = new Reference();
            reference.DigestMethod = SignedXml.XmlDsigSHA256Url;
            reference.DigestValue = new byte[] { 1, 2, 3, 4 };
            sig.SignedInfo.AddReference(reference);

            sig.SignatureValue = new byte[] { 1, 2, 3, 4 };

            DataObject obj1 = new DataObject();
            obj1.Id = "obj1";
            sig.AddObject(obj1);

            DataObject obj2 = new DataObject();
            obj2.Id = "obj2";
            sig.AddObject(obj2);

            Assert.Equal(2, sig.ObjectList.Count);
            XmlElement xml = sig.GetXml();
            Assert.NotNull(xml);
        }

        [Fact]
        public void SignatureLoadXmlWithId()
        {
            string xml = @"<Signature Id=""sig1"" xmlns=""http://www.w3.org/2000/09/xmldsig#"">
                <SignedInfo>
                    <CanonicalizationMethod Algorithm=""http://www.w3.org/TR/2001/REC-xml-c14n-20010315"" />
                    <SignatureMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#rsa-sha256"" />
                </SignedInfo>
                <SignatureValue>AQIDBA==</SignatureValue>
            </Signature>";

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            Signature sig = new Signature();
            sig.LoadXml(doc.DocumentElement);
            Assert.Equal("sig1", sig.Id);
        }

        [Fact]
        public void SignatureLoadXmlWithKeyInfo()
        {
            string xml = @"<Signature xmlns=""http://www.w3.org/2000/09/xmldsig#"">
                <SignedInfo>
                    <CanonicalizationMethod Algorithm=""http://www.w3.org/TR/2001/REC-xml-c14n-20010315"" />
                    <SignatureMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#rsa-sha256"" />
                </SignedInfo>
                <SignatureValue>AQIDBA==</SignatureValue>
                <KeyInfo>
                    <KeyName>MyKey</KeyName>
                </KeyInfo>
            </Signature>";

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            Signature sig = new Signature();
            sig.LoadXml(doc.DocumentElement);
            Assert.NotNull(sig.KeyInfo);
            Assert.Equal(1, sig.KeyInfo.Count);
        }

        [Fact]
        public void SignatureLoadXmlWithObjects()
        {
            string xml = @"<Signature xmlns=""http://www.w3.org/2000/09/xmldsig#"">
                <SignedInfo>
                    <CanonicalizationMethod Algorithm=""http://www.w3.org/TR/2001/REC-xml-c14n-20010315"" />
                    <SignatureMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#rsa-sha256"" />
                </SignedInfo>
                <SignatureValue>AQIDBA==</SignatureValue>
                <Object Id=""obj1""><test>data</test></Object>
                <Object Id=""obj2""><test>data2</test></Object>
            </Signature>";

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            Signature sig = new Signature();
            sig.LoadXml(doc.DocumentElement);
            Assert.Equal(2, sig.ObjectList.Count);
        }

        [Fact]
        public void Signature_AddObject()
        {
            Signature sig = new Signature();
            DataObject obj = new DataObject();
            obj.Id = "obj1";
            
            sig.AddObject(obj);
            Assert.Equal(1, sig.ObjectList.Count);
            Assert.Same(obj, sig.ObjectList[0]);
        }

        [Fact]
        public void Signature_ObjectList_NotNull()
        {
            Signature sig = new Signature();
            Assert.NotNull(sig.ObjectList);
            Assert.Equal(0, sig.ObjectList.Count);
        }

        [Fact]
        public void Signature_LoadXml_Null()
        {
            Signature sig = new Signature();
            Assert.Throws<ArgumentNullException>(() => sig.LoadXml(null));
        }

        [Fact]
        public void Signature_GetXml_SignatureValueNull()
        {
            Signature sig = new Signature();
            sig.SignedInfo = new SignedInfo();
            sig.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigC14NTransformUrl;
            sig.SignedInfo.SignatureMethod = SignedXml.XmlDsigRSASHA256Url;
            Reference r = new Reference();
            r.DigestMethod = SignedXml.XmlDsigSHA256Url;
            r.DigestValue = new byte[32];
            sig.SignedInfo.AddReference(r);
            
            Assert.Throws<CryptographicException>(() => sig.GetXml());
        }

        [Fact]
        public void Signature_LoadXml_EmptySignature()
        {
            string xml = @"<Signature xmlns=""http://www.w3.org/2000/09/xmldsig#"" />";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            Signature sig = new Signature();
            Assert.Throws<CryptographicException>(() => sig.LoadXml(doc.DocumentElement));
        }

        [Fact]
        public void Signature_LoadXml_MissingSignedInfo()
        {
            string xml = @"<Signature xmlns=""http://www.w3.org/2000/09/xmldsig#"">
                <SignatureValue>AQIDBA==</SignatureValue>
            </Signature>";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            Signature sig = new Signature();
            Assert.Throws<CryptographicException>(() => sig.LoadXml(doc.DocumentElement));
        }

        [Fact]
        public void Signature_LoadXml_MissingSignatureValue()
        {
            string xml = @"<Signature xmlns=""http://www.w3.org/2000/09/xmldsig#"">
                <SignedInfo>
                    <CanonicalizationMethod Algorithm=""http://www.w3.org/TR/2001/REC-xml-c14n-20010315"" />
                    <SignatureMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#rsa-sha256"" />
                </SignedInfo>
            </Signature>";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            Signature sig = new Signature();
            Assert.Throws<CryptographicException>(() => sig.LoadXml(doc.DocumentElement));
        }
    }
}
