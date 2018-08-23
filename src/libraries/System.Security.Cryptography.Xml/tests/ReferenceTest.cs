// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information
//
// ReferenceTest.cs - Test Cases for Reference
//
// Author:
//	Sebastien Pouliot <sebastien@ximian.com>
//
// (C) 2002, 2003 Motus Technologies Inc. (http://www.motus.com)
// (C) 2004 Novell (http://www.novell.com)
//
// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using Xunit;

namespace System.Security.Cryptography.Xml.Tests
{

    public class ReferenceTest
    {

        [Fact]
        public void Ctor()
        {
            Reference reference = new Reference();
            Assert.Null(reference.Uri);
            Assert.NotNull(reference.TransformChain);
            Assert.Null(reference.Id);
            Assert.Null(reference.Type);
            Assert.Null(reference.Uri);
            Assert.Throws<NullReferenceException>(() => reference.GetXml());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("uri")]
        [InlineData("http://mysite.com/")]
        public void Ctor_Uri(string uri)
        {
            Reference reference = new Reference(uri);

            Assert.Equal("http://www.w3.org/2001/04/xmlenc#sha256", reference.DigestMethod);

            Assert.Null(reference.DigestValue);
            Assert.Null(reference.Id);
            Assert.Null(reference.Type);
            Assert.Equal(uri, reference.Uri);
            Assert.Throws<NullReferenceException>(() => reference.GetXml());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("abcdefghijklmnopqrstuvwxyz")]
        [InlineData("01234567890")]
        public void Ctor_Stream(string data)
        {
            using (MemoryStream memoryStream = data != null ? new MemoryStream(Encoding.UTF8.GetBytes(data)) : null)
            {
                Reference reference = new Reference(memoryStream);

                Assert.Equal("http://www.w3.org/2001/04/xmlenc#sha256", reference.DigestMethod);

                Assert.Null(reference.DigestValue);
                Assert.Null(reference.Id);
                Assert.Null(reference.Type);
                Assert.Null(reference.Uri);
                Assert.Throws<NullReferenceException>(() => reference.GetXml());
            }
        }

        [Fact]
        public void LoadC14NTransform()
        { 
            string test =
                @"<Reference xmlns=""http://www.w3.org/2000/09/xmldsig#""><Transforms><Transform Algorithm=""http://www.w3.org/TR/2001/REC-xml-c14n-20010315"" /></Transforms><DigestMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#sha1"" /><DigestValue>AAAAAAAAAAAAAAAAAAAAAAAAAAA=</DigestValue></Reference>";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(test);
            Reference reference = new Reference();
            reference.LoadXml(doc.DocumentElement);
            Assert.Equal(test, (reference.GetXml().OuterXml));
            Assert.Equal(1, reference.TransformChain.Count);
        }

        [Fact]
        public void LoadC14NWithCommentsTransforms()
        {
            Reference reference = new Reference();
            string test =
                "<Reference xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><Transforms><Transform Algorithm=\"http://www.w3.org/TR/2001/REC-xml-c14n-20010315#WithComments\" /></Transforms><DigestMethod Algorithm=\"http://www.w3.org/2000/09/xmldsig#sha1\" /><DigestValue>AAAAAAAAAAAAAAAAAAAAAAAAAAA=</DigestValue></Reference>";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(test);
            reference.LoadXml(doc.DocumentElement);
            Assert.Equal(test, (reference.GetXml().OuterXml));
            Assert.Equal(1, reference.TransformChain.Count);
        }

        [Fact]
        public void LoadEnvelopedSignatureTransforms()
        {
            Reference reference = new Reference();
            string test = "<Reference xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><Transforms><Transform Algorithm=\"http://www.w3.org/2000/09/xmldsig#enveloped-signature\" /></Transforms><DigestMethod Algorithm=\"http://www.w3.org/2000/09/xmldsig#sha1\" /><DigestValue>AAAAAAAAAAAAAAAAAAAAAAAAAAA=</DigestValue></Reference>";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(test);
            reference.LoadXml(doc.DocumentElement);
            Assert.Equal(test, (reference.GetXml().OuterXml));
            Assert.Equal(1, reference.TransformChain.Count);
        }

        [Fact]
        public void LoadXPathTransforms()
        {
            Reference reference = new Reference();
            // test1 (MS) is an XML equivalent to test2 (Mono)
            string test1 = "<Reference xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><Transforms><Transform Algorithm=\"http://www.w3.org/TR/1999/REC-xpath-19991116\"><XPath></XPath></Transform></Transforms><DigestMethod Algorithm=\"http://www.w3.org/2000/09/xmldsig#sha1\" /><DigestValue>AAAAAAAAAAAAAAAAAAAAAAAAAAA=</DigestValue></Reference>";
            string test2 = "<Reference xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><Transforms><Transform Algorithm=\"http://www.w3.org/TR/1999/REC-xpath-19991116\"><XPath /></Transform></Transforms><DigestMethod Algorithm=\"http://www.w3.org/2000/09/xmldsig#sha1\" /><DigestValue>AAAAAAAAAAAAAAAAAAAAAAAAAAA=</DigestValue></Reference> ";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(test1);
            reference.LoadXml(doc.DocumentElement);
            string result = (reference.GetXml().OuterXml);
            Assert.True(((test1 == result) || (test2 == result)), result);
            Assert.Equal(1, reference.TransformChain.Count);
        }

        [Fact]
        public void LoadXsltTransforms()
        {
            string test = "<Reference xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><Transforms>";
            test += "<Transform Algorithm=\"http://www.w3.org/TR/1999/REC-xslt-19991116\">";
            test += "<xsl:stylesheet xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\" xmlns=\"http://www.w3.org/TR/xhtml1/strict\" exclude-result-prefixes=\"foo\" version=\"1.0\">";
            test += "<xsl:output encoding=\"UTF-8\" indent=\"no\" method=\"xml\" />";
            test += "<xsl:template match=\"/\"><html><head><title>Notaries</title>";
            test += "</head><body><table><xsl:for-each select=\"Notaries/Notary\">";
            test += "<tr><th><xsl:value-of select=\"@name\" /></th></tr></xsl:for-each>";
            test += "</table></body></html></xsl:template></xsl:stylesheet></Transform>";
            test += "</Transforms><DigestMethod Algorithm=\"http://www.w3.org/2000/09/xmldsig#sha1\" /><DigestValue>AAAAAAAAAAAAAAAAAAAAAAAAAAA=</DigestValue></Reference>";
            Reference reference = new Reference();
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(test);
            reference.LoadXml(doc.DocumentElement);
            string result = reference.GetXml().OuterXml;
            Assert.Equal(test, result);
            Assert.Equal(1, reference.TransformChain.Count);
        }

        [Fact]
        public void LoadAllTransforms()
        {
            string test = "<Reference xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><Transforms><Transform Algorithm=\"http://www.w3.org/2000/09/xmldsig#base64\" /><Transform Algorithm=\"http://www.w3.org/TR/2001/REC-xml-c14n-20010315\" /><Transform Algorithm=\"http://www.w3.org/TR/2001/REC-xml-c14n-20010315#WithComments\" /><Transform Algorithm=\"http://www.w3.org/2000/09/xmldsig#enveloped-signature\" /><Transform Algorithm=\"http://www.w3.org/TR/1999/REC-xpath-19991116\"><XPath /></Transform>";
            test += "<Transform Algorithm=\"http://www.w3.org/TR/1999/REC-xslt-19991116\">";
            test += "<xsl:stylesheet xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\" xmlns=\"http://www.w3.org/TR/xhtml1/strict\" exclude-result-prefixes=\"foo\" version=\"1.0\">";
            test += "<xsl:output encoding=\"UTF-8\" indent=\"no\" method=\"xml\" />";
            test += "<xsl:template match=\"/\"><html><head><title>Notaries</title>";
            test += "</head><body><table><xsl:for-each select=\"Notaries/Notary\">";
            test += "<tr><th><xsl:value-of select=\"@name\" /></th></tr></xsl:for-each>";
            test += "</table></body></html></xsl:template></xsl:stylesheet></Transform>";
            test += "</Transforms><DigestMethod Algorithm=\"http://www.w3.org/2000/09/xmldsig#sha1\" /><DigestValue>AAAAAAAAAAAAAAAAAAAAAAAAAAA=</DigestValue></Reference>";
            Reference reference = new Reference();
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(test);
            reference.LoadXml(doc.DocumentElement);
            string result = reference.GetXml().OuterXml;
            Assert.Equal(test, result);
            Assert.Equal(6, reference.TransformChain.Count);
        }

        [Fact]
        public void LoadNoTransform()
        {
            Reference reference = new Reference();
            const string uri = "urn:1";
            string test = 
                $@"<Reference URI=""{ uri }"" xmlns=""http://www.w3.org/2000/09/xmldsig#""><DigestMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#sha1"" /><DigestValue>/Vvq6sXEVbtZC8GwNtLQnGOy/VI=</DigestValue></Reference>";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(test);
            reference.LoadXml(doc.DocumentElement);
            Assert.Equal(test, reference.GetXml().OuterXml);
            Assert.Equal(uri, reference.Uri);
            Assert.Equal(new byte[]{ 0xFD, 0x5B, 0xEA, 0xEA, 0xC5, 0xC4, 0x55, 0xBB, 0x59, 0x0B, 0xC1, 0xB0, 0x36, 0xD2, 0xD0, 0x9C, 0x63, 0xB2, 0xFD, 0x52 }, reference.DigestValue);
            Assert.Equal(0, reference.TransformChain.Count);
        }

        [Fact]
        public void AddAllTransforms()
        {
            Reference reference = new Reference();
            // adding an empty hash value
            byte[] hash = new byte[20];
            reference.DigestMethod = SignedXml.XmlDsigSHA1Url;
            reference.DigestValue = hash;
            XmlElement xel = reference.GetXml();
            // this is the minimal Reference (DigestValue)!
            Assert.NotNull(xel);

            reference.AddTransform(new XmlDsigBase64Transform());
            reference.AddTransform(new XmlDsigC14NTransform());
            reference.AddTransform(new XmlDsigC14NWithCommentsTransform());
            reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
            reference.AddTransform(new XmlDsigXPathTransform());
            reference.AddTransform(new XmlDsigXsltTransform());

            string test1 = @"<Reference xmlns=""http://www.w3.org/2000/09/xmldsig#""><Transforms><Transform Algorithm=""http://www.w3.org/2000/09/xmldsig#base64"" /><Transform Algorithm=""http://www.w3.org/TR/2001/REC-xml-c14n-20010315"" /><Transform Algorithm=""http://www.w3.org/TR/2001/REC-xml-c14n-20010315#WithComments"" /><Transform Algorithm=""http://www.w3.org/2000/09/xmldsig#enveloped-signature"" /><Transform Algorithm=""http://www.w3.org/TR/1999/REC-xpath-19991116""><XPath /></Transform><Transform Algorithm=""http://www.w3.org/TR/1999/REC-xslt-19991116"" /></Transforms><DigestMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#sha1"" /><DigestValue>AAAAAAAAAAAAAAAAAAAAAAAAAAA=</DigestValue></Reference>";
            string result = reference.GetXml().OuterXml;
            Assert.Equal(test1, result);
            // however this value cannot be loaded as it's missing some transform (xslt) parameters

            // can we add them again ?
            reference.AddTransform(new XmlDsigBase64Transform());
            reference.AddTransform(new XmlDsigC14NTransform());
            reference.AddTransform(new XmlDsigC14NWithCommentsTransform());
            reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
            reference.AddTransform(new XmlDsigXPathTransform());
            reference.AddTransform(new XmlDsigXsltTransform());

            // seems so ;-)
            Assert.Equal(12, reference.TransformChain.Count);
        }

        [Fact]
        public void Null()
        {
            Reference reference = new Reference();
            // null DigestMethod -> "" DigestMethod !!!
            reference.DigestMethod = null;
            Assert.Null(reference.DigestMethod);
        }

        [Fact]
        public void Bad1()
        {
            Reference reference = new Reference();
            reference.Uri = "#MyObjectId";
            // not enough info
            Assert.Throws< NullReferenceException>(()=> reference.GetXml());
        }

        [Fact]
        public void Bad2()
        {
            Reference reference = new Reference();
            // bad hash - there's no validation!
            reference.DigestMethod = "http://www.w3.org/2000/09/xmldsig#mono";
        }

        const string xml = @"<player bats=""left"" id=""10012"" throws=""right"">
	<!-- Here&apos;s a comment -->
	<name>Alfonso Soriano</name>
	<position>2B</position>
	<team>New York Yankees</team>
<dsig:Signature xmlns=""http://www.w3.org/2000/09/xmldsig#"" xmlns:dsig=""http://www.w3.org/2000/09/xmldsig#"">"
+ @"<dsig:SignedInfo><dsig:CanonicalizationMethod Algorithm=""http://www.w3.org/TR/2001/REC-xml-c14n-withcomments-20010315""/><dsig:SignatureMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#rsa-sha1""/>"
+ @"<dsig:Reference URI=""""><dsig:Transforms><dsig:Transform Algorithm=""http://www.w3.org/2000/09/xmldsig#enveloped-signature""/></dsig:Transforms><dsig:DigestMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#sha1""/><dsig:DigestValue>nDF2V/bzRd0VE3EwShWtsBzTEDc=</dsig:DigestValue></dsig:Reference></dsig:SignedInfo><dsig:SignatureValue>fbye4Xm//RPUTsLd1dwJPo0gPZYX6gVYCEB/gz2348EARNk/nCCch1fFfpuqAGMKg4ayVC0yWkUyE5V4QB33jaGlh9wuNQSjxs6TIvFwSsT+0ioDgVgFv0gVeasbyNL4rFEHuAWL8QKwDT9L6b2wUvJC90DmpBs9GMR2jTZIWlM=</dsig:SignatureValue><dsig:KeyInfo><dsig:X509Data><dsig:X509Certificate>MIIC0DCCAjmgAwIBAgIDD0JBMA0GCSqGSIb3DQEBBAUAMHwxCzAJBgNVBAYTAlVTMREwDwYDVQQIEwhOZXcgWW9yazERMA8GA1UEBxMITmV3IFlvcmsxGTAXBgNVBAoTEFBoYW9zIFRlY2hub2xvZ3kxFDASBgNVBAsTC0VuZ2luZWVyaW5nMRYwFAYDVQQDEw1UZXN0IENBIChSU0EpMB4XDTAyMDQyOTE5MTY0MFoXDTEyMDQyNjE5MTY0MFowgYAxCzAJBgNVBAYTAlVTMREwDwYDVQQIEwhOZXcgWW9yazERMA8GA1UEBxMITmV3IFlvcmsxGTAXBgNVBAoTEFBoYW9zIFRlY2hub2xvZ3kxFDASBgNVBAsTC0VuZ2luZWVyaW5nMRowGAYDVQQDExFUZXN0IENsaWVudCAoUlNBKTCBnzANBgkqhkiG9w0BAQEFAAOBjQAwgYkCgYEAgIb6nAB9oS/AI5jIj6WymvQhRxiMlE07G4abmMliYi5zWzvaFE2tnU+RZIBgtoXcgDEIU/vsLQut7nzCn9mHxC8JEaV4D4U91j64AyZakShqJw7qjJfqUxxPL0yJv2oFiouPDjGuJ9JPi0NrsZq+yfWfM54s4b9SNkcOIVMybZUCAwEAAaNbMFkwDAYDVR0TAQH/BAIwADAPBgNVHQ8BAf8EBQMDB9gAMBkGA1UdEQQSMBCBDnRlY2hAcGhhb3MuY29tMB0GA1UdDgQWBBQT58rBCxPmVLeZaYGRqVROnQlFbzANBgkqhkiG9w0BAQQFAAOBgQCxbCovFST25t+ryN1RipqozxJQcguKfeCwbfgBNobzcRvoW0kSIf7zi4mtQajDM0NfslFF51/dex5Rn64HmFFshSwSvQQMyf5Cfaqv2XQ60OXq6nAFG6WbHoge6RqfIez2MWDLoSB6plsjKtMmL3mcybBhROtX5GGuLx1NtfhNFQ==</dsig:X509Certificate><dsig:X509IssuerSerial><dsig:X509IssuerName>CN=Test CA (RSA),OU=Engineering,O=Phaos Technology,L=New York,ST=New York,C=US</dsig:X509IssuerName><dsig:X509SerialNumber>1000001</dsig:X509SerialNumber></dsig:X509IssuerSerial><dsig:X509SubjectName>CN=Test Client (RSA),OU=Engineering,O=Phaos Technology,L=New York,ST=New York,C=US</dsig:X509SubjectName><dsig:X509SKI>E+fKwQsT5lS3mWmBkalUTp0JRW8=</dsig:X509SKI></dsig:X509Data></dsig:KeyInfo></dsig:Signature></player>";

        [Fact]
        public void KeepDocument()
        {
            string result = @"<dsig:Reference URI="""" xmlns:dsig=""http://www.w3.org/2000/09/xmldsig#""><dsig:Transforms><dsig:Transform Algorithm=""http://www.w3.org/2000/09/xmldsig#enveloped-signature"" /></dsig:Transforms><dsig:DigestMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#sha1"" /><dsig:DigestValue>nDF2V/bzRd0VE3EwShWtsBzTEDc=</dsig:DigestValue></dsig:Reference>";

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            XmlElement org = (XmlElement)doc.SelectSingleNode("//*[local-name()='Reference']");
            Reference r = new Reference();
            r.LoadXml(org);
            XmlElement el = r.GetXml();
            Assert.Equal(doc, el.OwnerDocument);
            Assert.Equal(org, el);
            Assert.Equal(result, el.OuterXml);
        }
    }
}
