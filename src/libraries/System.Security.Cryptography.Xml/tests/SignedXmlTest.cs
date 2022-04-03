// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// SignedXmlTest.cs - Test Cases for SignedXml
//
// Author:
//  Sebastien Pouliot  <sebastien@ximian.com>
//
// (C) 2002, 2003 Motus Technologies Inc. (http://www.motus.com)
// Copyright (C) 2004-2005, 2008 Novell, Inc (http://www.novell.com)

using System.Globalization;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Xml.Tests
{

    public class SignedXmlEx : SignedXml
    {

        // required to test protected GetPublicKey in SignedXml
        public AsymmetricAlgorithm PublicGetPublicKey()
        {
            return base.GetPublicKey();
        }
    }

    public class SignedXmlTest
    {

        private const string signature = "<Signature xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><SignedInfo><CanonicalizationMethod Algorithm=\"http://www.w3.org/TR/2001/REC-xml-c14n-20010315\" /><SignatureMethod Algorithm=\"http://www.w3.org/2000/09/xmldsig#rsa-sha1\" /><Reference URI=\"#MyObjectId\"><DigestMethod Algorithm=\"http://www.w3.org/2000/09/xmldsig#sha1\" /><DigestValue>CTnnhjxUQHJmD+t1MjVXrOW+MCA=</DigestValue></Reference></SignedInfo><SignatureValue>dbFt6Zw3vR+Xh7LbM/vuifyFA7gPh/NlDM2Glz/SJBsveISieuTBpZlk/zavAeuXR/Nu0Ztt4OP4tCOg09a2RNlrTP0dhkeEfL1jTzpnVaLHuQbCiwOWCgbRif7Xt7N12FuiHYb3BltP/YyXS4E12NxlGlqnDiFA1v/mkK5+C1o=</SignatureValue><KeyInfo><KeyValue xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><RSAKeyValue><Modulus>hEfTJNa2idz2u+fSYDDG4Lx/xuk4aBbvOPVNqgc1l9Y8t7Pt+ZyF+kkF3uUl8Y0700BFGAsprnhwrWENK+PGdtvM5796ZKxCCa0ooKkofiT4355HqK26hpV8dvj38vq/rkJe1jHZgkTKa+c/0vjcYZOI/RT/IZv9JfXxVWLuLxk=</Modulus><Exponent>EQ==</Exponent></RSAKeyValue></KeyValue></KeyInfo><Object Id=\"MyObjectId\" xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><ObjectListTag xmlns=\"\" /></Object></Signature>";

        [Fact]
        public void StaticValues()
        {
            Assert.Equal("http://www.w3.org/TR/2001/REC-xml-c14n-20010315", SignedXml.XmlDsigCanonicalizationUrl);
            Assert.Equal("http://www.w3.org/TR/2001/REC-xml-c14n-20010315#WithComments", SignedXml.XmlDsigCanonicalizationWithCommentsUrl);
            Assert.Equal("http://www.w3.org/2000/09/xmldsig#dsa-sha1", SignedXml.XmlDsigDSAUrl);
            Assert.Equal("http://www.w3.org/2000/09/xmldsig#hmac-sha1", SignedXml.XmlDsigHMACSHA1Url);
            Assert.Equal("http://www.w3.org/2000/09/xmldsig#minimal", SignedXml.XmlDsigMinimalCanonicalizationUrl);
            Assert.Equal("http://www.w3.org/2000/09/xmldsig#", SignedXml.XmlDsigNamespaceUrl);
            Assert.Equal("http://www.w3.org/2000/09/xmldsig#rsa-sha1", SignedXml.XmlDsigRSASHA1Url);
            Assert.Equal("http://www.w3.org/2000/09/xmldsig#sha1", SignedXml.XmlDsigSHA1Url);
        }

        [Fact]
        public void Constructor_Empty()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(signature);
            XmlNodeList xnl = doc.GetElementsByTagName("Signature", SignedXml.XmlDsigNamespaceUrl);
            XmlElement xel = (XmlElement)xnl[0];

            SignedXml sx = new SignedXml(doc);
            sx.LoadXml(xel);
            Assert.True(sx.CheckSignature(), "CheckSignature");
        }

        [Fact]
        public void Constructor_XmlDocument()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(signature);
            XmlNodeList xnl = doc.GetElementsByTagName("Signature", SignedXml.XmlDsigNamespaceUrl);
            XmlElement xel = (XmlElement)xnl[0];

            SignedXml sx = new SignedXml(doc);
            sx.LoadXml(doc.DocumentElement);
            Assert.True(sx.CheckSignature());
        }

        [Fact]
        public void Constructor_XmlDocument_Null()
        {
            XmlDocument doc = null;
            Assert.Throws<ArgumentNullException>(() => new SignedXml(doc));
        }

        [Fact]
        public void Constructor_XmlElement()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(signature);
            XmlNodeList xnl = doc.GetElementsByTagName("Signature", SignedXml.XmlDsigNamespaceUrl);
            XmlElement xel = (XmlElement)xnl[0];

            SignedXml sx = new SignedXml(doc.DocumentElement);
            sx.LoadXml(xel);
            Assert.True(sx.CheckSignature(), "CheckSignature");
        }

        [Fact]
        public void Constructor_XmlElement_WithoutLoadXml()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(signature);
            XmlNodeList xnl = doc.GetElementsByTagName("Signature", SignedXml.XmlDsigNamespaceUrl);
            XmlElement xel = (XmlElement)xnl[0];

            SignedXml sx = new SignedXml(doc.DocumentElement);
            Assert.True(!sx.CheckSignature(), "!CheckSignature");
            // SignedXml (XmlElement) != SignedXml () + LoadXml (XmlElement)
        }

        [Fact]
        public void Constructor_XmlElement_Null()
        {
            XmlElement xel = null;
            Assert.Throws<ArgumentNullException>(() => new SignedXml(xel));
        }

        // sample from MSDN (url)
        public SignedXml MSDNSample()
        {
            // Create example data to sign.
            XmlDocument document = new XmlDocument();
            XmlNode node = document.CreateNode(XmlNodeType.Element, "", "MyElement", "samples");
            node.InnerText = "This is some text";
            document.AppendChild(node);

            // Create the SignedXml message.
            SignedXml signedXml = new SignedXml();

            // Create a data object to hold the data to sign.
            DataObject dataObject = new DataObject();
            dataObject.Data = document.ChildNodes;
            dataObject.Id = "MyObjectId";

            // Add the data object to the signature.
            signedXml.AddObject(dataObject);

            // Create a reference to be able to package everything into the
            // message.
            Reference reference = new Reference();
            reference.Uri = "#MyObjectId";

            // Add it to the message.
            signedXml.AddReference(reference);

            return signedXml;
        }

        [Fact]
        public void SignatureMethodMismatch()
        {
            SignedXml signedXml = MSDNSample();

            RSA key = RSA.Create();
            signedXml.SigningKey = key;
            signedXml.SignedInfo.SignatureMethod = SignedXml.XmlDsigHMACSHA1Url;

            // Add a KeyInfo.
            KeyInfo keyInfo = new KeyInfo();
            keyInfo.AddClause(new RSAKeyValue(key));
            signedXml.KeyInfo = keyInfo;

            Assert.NotNull(signedXml.SignatureMethod);
            // Compute the signature - causes unsupported algorithm by the key.
            Assert.Throws<CryptographicException>(() => signedXml.ComputeSignature());
        }

        [Fact]
        public void AsymmetricRSASignature()
        {
            SignedXml signedXml = MSDNSample();

            RSA key = RSA.Create();
            signedXml.SigningKey = key;

            // Add a KeyInfo.
            KeyInfo keyInfo = new KeyInfo();
            keyInfo.AddClause(new RSAKeyValue(key));
            signedXml.KeyInfo = keyInfo;

            Assert.Equal(1, signedXml.KeyInfo.Count);
            Assert.Null(signedXml.SignatureLength);
            Assert.Null(signedXml.SignatureMethod);
            Assert.Null(signedXml.SignatureValue);
            Assert.Null(signedXml.SigningKeyName);

            // Compute the signature.
            signedXml.ComputeSignature();

            Assert.Null(signedXml.SigningKeyName);

            Assert.Equal("http://www.w3.org/2001/04/xmldsig-more#rsa-sha256", signedXml.SignatureMethod);

            Assert.Equal(key.KeySize / 8, signedXml.SignatureValue.Length);
            Assert.Null(signedXml.SigningKeyName);

            // Get the XML representation of the signature.
            XmlElement xmlSignature = signedXml.GetXml();

            // we must reload the signature or it won't work
            // MS framework throw a "malformed element"
            SignedXml vrfy = new SignedXml();
            vrfy.LoadXml(xmlSignature);

            // assert that we can verify our own signature
            Assert.True(vrfy.CheckSignature(), "RSA-Compute/Verify");
        }

        // The same as MSDNSample(), but adding a few attributes
        public SignedXml MSDNSampleMixedCaseAttributes()
        {
            // Create example data to sign.
            XmlDocument document = new XmlDocument();
            XmlNode node = document.CreateNode(XmlNodeType.Element, "", "MyElement", "samples");
            node.InnerText = "This is some text";
            XmlAttribute a1 = document.CreateAttribute("Aa");
            XmlAttribute a2 = document.CreateAttribute("Bb");
            XmlAttribute a3 = document.CreateAttribute("aa");
            XmlAttribute a4 = document.CreateAttribute("bb");
            a1.Value = "one";
            a2.Value = "two";
            a3.Value = "three";
            a4.Value = "four";
            node.Attributes.Append(a1);
            node.Attributes.Append(a2);
            node.Attributes.Append(a3);
            node.Attributes.Append(a4);
            document.AppendChild(node);

            // Create the SignedXml message.
            SignedXml signedXml = new SignedXml();

            // Create a data object to hold the data to sign.
            DataObject dataObject = new DataObject();
            dataObject.Data = document.ChildNodes;
            dataObject.Id = "MyObjectId";

            // Add the data object to the signature.
            signedXml.AddObject(dataObject);

            // Create a reference to be able to package everything into the
            // message.
            Reference reference = new Reference();
            reference.Uri = "#MyObjectId";

            // Add it to the message.
            signedXml.AddReference(reference);

            return signedXml;
        }

        public string AsymmetricRSAMixedCaseAttributes()
        {
            SignedXml signedXml = MSDNSampleMixedCaseAttributes();

            RSA key = RSA.Create();
            signedXml.SigningKey = key;

            // Add a KeyInfo.
            KeyInfo keyInfo = new KeyInfo();
            keyInfo.AddClause(new RSAKeyValue(key));
            signedXml.KeyInfo = keyInfo;

            // Compute the signature.
            signedXml.ComputeSignature();

            // Get the XML representation of the signature.
            XmlElement xmlSignature = signedXml.GetXml();

            StringWriter sw = new StringWriter();
            XmlWriter w = new XmlTextWriter(sw);
            xmlSignature.WriteTo(w);
            return sw.ToString();
        }

        // Example output from Windows for AsymmetricRSAMixedCaseAttributes()
        private const string AsymmetricRSAMixedCaseAttributesResult = "<Signature xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><SignedInfo><CanonicalizationMethod Algorithm=\"http://www.w3.org/TR/2001/REC-xml-c14n-20010315\" /><SignatureMethod Algorithm=\"http://www.w3.org/2000/09/xmldsig#rsa-sha1\" /><Reference URI=\"#MyObjectId\"><DigestMethod Algorithm=\"http://www.w3.org/2000/09/xmldsig#sha1\" /><DigestValue>0j1xLsePFtuRHfXEnVdTSLWtAm4=</DigestValue></Reference></SignedInfo><SignatureValue>hmrEBgns5Xx14aDhzqOyIh0qLNMUldtW8+fNPcvtD/2KtEhNZQGctnhs90CRa1NZ08TqzW2pUaEwmqvMAtF4v8KtWzC/zTuc1jH6nxQvQSQo0ABhuXdu7/hknZkXJ4yKBbdgbKjAsKfULwbWrP/PacLPoYfCO+wXSrt+wLMTTWU=</SignatureValue><KeyInfo><KeyValue><RSAKeyValue><Modulus>4h/rHDr54r6SZWk2IPCeHX7N+wR1za0VBLshuS6tq3RSWap4PY2BM8VdbKH2T9RzyZoiHufjng+1seUx430iMsXisOLUkPP+yGtMQOSZ3CQHAa+IYA+fplXipixI0rV1J1wJNXQm3HxXQqKWpIv5fkwBtj8o2k6CWMgPNgFCnxc=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue></KeyValue></KeyInfo><Object Id=\"MyObjectId\"><MyElement Aa=\"one\" Bb=\"two\" aa=\"three\" bb=\"four\" xmlns=\"samples\">This is some text</MyElement></Object></Signature>";

        [Fact]
        public void AsymmetricRSAMixedCaseAttributesVerifyWindows()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(AsymmetricRSAMixedCaseAttributesResult);

            SignedXml v1 = new SignedXml();
            v1.LoadXml(doc.DocumentElement);
            Assert.True(v1.CheckSignature());
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/20575", TestPlatforms.OSX)]
        [SkipOnPlatform(PlatformSupport.MobileAppleCrypto, "DSA is not available")]
        public void AsymmetricDSASignature()
        {
            SignedXml signedXml = MSDNSample();

            DSA key = DSA.Create();
            signedXml.SigningKey = key;

            // Add a KeyInfo.
            KeyInfo keyInfo = new KeyInfo();
            keyInfo.AddClause(new DSAKeyValue(key));
            signedXml.KeyInfo = keyInfo;

            Assert.Equal(1, signedXml.KeyInfo.Count);
            Assert.Null(signedXml.SignatureLength);
            Assert.Null(signedXml.SignatureMethod);
            Assert.Null(signedXml.SignatureValue);
            Assert.Null(signedXml.SigningKeyName);

            // Compute the signature.
            signedXml.ComputeSignature();

            Assert.Null(signedXml.SignatureLength);
            Assert.Equal(SignedXml.XmlDsigDSAUrl, signedXml.SignatureMethod);
            Assert.InRange(signedXml.SignatureValue.Length, low: 40, high: int.MaxValue);
            Assert.Null(signedXml.SigningKeyName);

            // Get the XML representation of the signature.
            XmlElement xmlSignature = signedXml.GetXml();

            // we must reload the signature or it won't work
            // MS framework throw a "malformed element"
            SignedXml vrfy = new SignedXml();
            vrfy.LoadXml(xmlSignature);

            // assert that we can verify our own signature
            Assert.True(vrfy.CheckSignature(), "DSA-Compute/Verify");
        }

        [Fact]
        public void SymmetricHMACSHA1Signature()
        {
            SignedXml signedXml = MSDNSample();

            // Compute the signature.
            byte[] secretkey = Encoding.Default.GetBytes("password");
            HMACSHA1 hmac = new HMACSHA1(secretkey);
            Assert.Equal(0, signedXml.KeyInfo.Count);
            Assert.Null(signedXml.SignatureLength);
            Assert.Null(signedXml.SignatureMethod);
            Assert.Null(signedXml.SignatureValue);
            Assert.Null(signedXml.SigningKeyName);

            signedXml.ComputeSignature(hmac);

            Assert.Equal(0, signedXml.KeyInfo.Count);
            Assert.Null(signedXml.SignatureLength);
            Assert.Equal(SignedXml.XmlDsigHMACSHA1Url, signedXml.SignatureMethod);
            Assert.Equal(20, signedXml.SignatureValue.Length);
            Assert.Null(signedXml.SigningKeyName);

            // Get the XML representation of the signature.
            XmlElement xmlSignature = signedXml.GetXml();

            // we must reload the signature or it won't work
            // MS framework throw a "malformed element"
            SignedXml vrfy = new SignedXml();
            vrfy.LoadXml(xmlSignature);

            // assert that we can verify our own signature
            Assert.True(vrfy.CheckSignature(hmac), "HMACSHA1-Compute/Verify");
        }

        // Using empty constructor
        // The two other constructors don't seems to apply in verifying signatures
        [Fact]
        public void AsymmetricRSAVerify()
        {
            string value = "<Signature xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><SignedInfo><CanonicalizationMethod Algorithm=\"http://www.w3.org/TR/2001/REC-xml-c14n-20010315\" /><SignatureMethod Algorithm=\"http://www.w3.org/2000/09/xmldsig#rsa-sha1\" /><Reference URI=\"#MyObjectId\"><DigestMethod Algorithm=\"http://www.w3.org/2000/09/xmldsig#sha1\" /><DigestValue>/Vvq6sXEVbtZC8GwNtLQnGOy/VI=</DigestValue></Reference></SignedInfo><SignatureValue>A6XuE8Cy9iOffRXaW9b0+dUcMUJQnlmwLsiqtQnADbCtZXnXAaeJ6nGnQ4Mm0IGi0AJc7/2CoJReXl7iW4hltmFguG1e3nl0VxCyCTHKGOCo1u8R3K+B1rTaenFbSxs42EM7/D9KETsPlzfYfis36yM3PqatiCUOsoMsAiMGzlc=</SignatureValue><KeyInfo><KeyValue xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><RSAKeyValue><Modulus>tI8QYIpbG/m6JLyvP+S3X8mzcaAIayxomyTimSh9UCpEucRnGvLw0P73uStNpiF7wltTZA1HEsv+Ha39dY/0j/Wiy3RAodGDRNuKQao1wu34aNybZ673brbsbHFUfw/o7nlKD2xO84fbajBZmKtBBDy63NHt+QL+grSrREPfCTM=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue></KeyValue></KeyInfo><Object Id=\"MyObjectId\"><MyElement xmlns=\"samples\">This is some text</MyElement></Object></Signature>";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(value);

            SignedXml v1 = new SignedXml();
            v1.LoadXml(doc.DocumentElement);
            Assert.True(v1.CheckSignature(), "RSA-CheckSignature()");

            SignedXml v2 = new SignedXml();
            v2.LoadXml(doc.DocumentElement);
            AsymmetricAlgorithm key = null;
            bool vrfy = v2.CheckSignatureReturningKey(out key);
            Assert.True(vrfy, "RSA-CheckSignatureReturningKey()");

            SignedXml v3 = new SignedXml();
            v3.LoadXml(doc.DocumentElement);
            Assert.True(v3.CheckSignature(key), "RSA-CheckSignature(key)");
        }

        // Using empty constructor
        // The two other constructors don't seems to apply in verifying signatures
        [Fact]
        [SkipOnPlatform(PlatformSupport.MobileAppleCrypto, "DSA is not available")]
        public void AsymmetricDSAVerify()
        {
            string value = "<Signature xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><SignedInfo><CanonicalizationMethod Algorithm=\"http://www.w3.org/TR/2001/REC-xml-c14n-20010315\" /><SignatureMethod Algorithm=\"http://www.w3.org/2000/09/xmldsig#dsa-sha1\" /><Reference URI=\"#MyObjectId\"><DigestMethod Algorithm=\"http://www.w3.org/2000/09/xmldsig#sha1\" /><DigestValue>/Vvq6sXEVbtZC8GwNtLQnGOy/VI=</DigestValue></Reference></SignedInfo><SignatureValue>BYz/qRGjGsN1yMFPxWa3awUZm1y4I/IxOQroMxkOteRGgk1HIwhRYw==</SignatureValue><KeyInfo><KeyValue xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><DSAKeyValue><P>iglVaZ+LsSL8Y0aDXmFMBwva3xHqIypr3l/LtqBH9ziV2Sh1M4JVasAiKqytWIWt/s/Uk8Ckf2tO2Ww1vsNi1NL+Kg9T7FE52sn380/rF0miwGkZeidzm74OWhykb3J+wCTXaIwOzAWI1yN7FoeoN7wzF12jjlSXAXeqPMlViqk=</P><Q>u4sowiJMHilNRojtdmIuQY2YnB8=</Q><G>SdnN7d+wn1n+HH4Hr8MIryIRYgcXdbZ5TH7jAnuWc1koqRc1AZfcYAZ6RDf+orx6Lzn055FTFiN+1NHQfGUtXJCWW0zz0FVV1NJux7WRj8vGTldjJ5ef0oCenkpwDjcIxWsZgVobve4GPoyN1sAc1scnkJB59oupibklmF4y72A=</G><Y>XejzS8Z51yfl0zbYnxSYYbHqreSLjNCoGPB/KjM1TOyV5sMjz0StKtGrFWryTWc7EgvFY7kUth4e04VKf9HbK8z/FifHTXj8+Tszbjzw8GfInnBwLN+vJgbpnjtypmiI5Bm2nLiRbfkdAHP+OrKtr/EauM9GQfYuaxm3/Vj8B84=</Y><J>vGwGg9wqwwWP9xsoPoXu6kHArJtadiNKe9azBiUx5Ob883gd5wlKfEcGuKkBmBySGbgwxyOsIBovd9Kk48hF01ymfQzAAuHR0EdJECSsTsTTKVTLQNBU32O+PRbLYpv4E8kt6rNL83JLJCBY</J><Seed>sqzn8J6fd2gtEyq6YOqiUSHgPE8=</Seed><PgenCounter>sQ==</PgenCounter></DSAKeyValue></KeyValue></KeyInfo><Object Id=\"MyObjectId\"><MyElement xmlns=\"samples\">This is some text</MyElement></Object></Signature>";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(value);

            SignedXml v1 = new SignedXml();
            v1.LoadXml(doc.DocumentElement);
            Assert.True(v1.CheckSignature(), "DSA-CheckSignature()");

            SignedXml v2 = new SignedXml();
            v2.LoadXml(doc.DocumentElement);
            AsymmetricAlgorithm key = null;
            bool vrfy = v2.CheckSignatureReturningKey(out key);
            Assert.True(vrfy, "DSA-CheckSignatureReturningKey()");

            SignedXml v3 = new SignedXml();
            v3.LoadXml(doc.DocumentElement);
            Assert.True(v3.CheckSignature(key), "DSA-CheckSignature(key)");
        }

        [Fact]
        public void SymmetricHMACSHA1Verify()
        {
            string value = "<Signature xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><SignedInfo><CanonicalizationMethod Algorithm=\"http://www.w3.org/TR/2001/REC-xml-c14n-20010315\" /><SignatureMethod Algorithm=\"http://www.w3.org/2000/09/xmldsig#hmac-sha1\" /><Reference URI=\"#MyObjectId\"><DigestMethod Algorithm=\"http://www.w3.org/2000/09/xmldsig#sha1\" /><DigestValue>/Vvq6sXEVbtZC8GwNtLQnGOy/VI=</DigestValue></Reference></SignedInfo><SignatureValue>e2RxYr5yGbvTqZLCFcgA2RAC0yE=</SignatureValue><Object Id=\"MyObjectId\"><MyElement xmlns=\"samples\">This is some text</MyElement></Object></Signature>";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(value);

            SignedXml v1 = new SignedXml();
            v1.LoadXml(doc.DocumentElement);

            byte[] secretkey = Encoding.Default.GetBytes("password");
            HMACSHA1 hmac = new HMACSHA1(secretkey);

            Assert.True(v1.CheckSignature(hmac), "HMACSHA1-CheckSignature(key)");
        }

        [Fact]
        // adapted from http://bugzilla.ximian.com/show_bug.cgi?id=52084
        public void GetIdElement()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(signature);

            SignedXml v1 = new SignedXml();
            v1.LoadXml(doc.DocumentElement);
            Assert.True(v1.CheckSignature(), "CheckSignature");

            XmlElement xel = v1.GetIdElement(doc, "MyObjectId");
            Assert.True(xel.InnerXml.StartsWith("<ObjectListTag"), "GetIdElement");
        }

        [Fact]
        public void GetPublicKey()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(signature);

            SignedXmlEx sxe = new SignedXmlEx();
            sxe.LoadXml(doc.DocumentElement);

            AsymmetricAlgorithm aa1 = sxe.PublicGetPublicKey();
            Assert.True((aa1 is RSA), "First Public Key is RSA");

            AsymmetricAlgorithm aa2 = sxe.PublicGetPublicKey();
            Assert.Null(aa2);
        }

        [Fact]
        public void AddObject_Null()
        {
            SignedXml sx = new SignedXml();
            sx.AddObject(null);
        }

        [Fact]
        public void AddReference_Null()
        {
            SignedXml sx = new SignedXml();
            Assert.Throws<ArgumentNullException>(() => sx.AddReference(null));
        }

        [Fact]
        public void GetXml_WithoutInfo()
        {
            SignedXml sx = new SignedXml();
            Assert.Throws<CryptographicException>(() => sx.GetXml());
        }

        [Fact]
        public void LoadXml_Null()
        {
            SignedXml sx = new SignedXml();
            Assert.Throws<ArgumentNullException>(() => sx.LoadXml(null));
        }

        [Fact]
        public void SigningKeyName()
        {
            SignedXmlEx sxe = new SignedXmlEx();
            Assert.Null(sxe.SigningKeyName);
            sxe.SigningKeyName = "mono";
            Assert.Equal("mono", sxe.SigningKeyName);
        }

        [Fact]
        public void CheckSignatureEmptySafe()
        {
            SignedXml sx;
            KeyInfoClause kic;
            KeyInfo ki;

            // empty keyinfo passes...
            sx = new SignedXml();
            sx.KeyInfo = new KeyInfo();
            Assert.True(!sx.CheckSignature());

            // with empty KeyInfoName
            kic = new KeyInfoName();
            ki = new KeyInfo();
            ki.AddClause(kic);
            sx.KeyInfo = ki;
            Assert.True(!sx.CheckSignature());
        }

        [Fact]
        public void CheckSignatureEmpty()
        {
            SignedXml sx = new SignedXml();
            Assert.True(!sx.CheckSignature());
        }

        [Fact]
        public void ComputeSignatureNoSigningKey()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<foo/>");
            SignedXml signedXml = new SignedXml(doc);

            Reference reference = new Reference();
            reference.Uri = "";

            XmlDsigEnvelopedSignatureTransform env = new XmlDsigEnvelopedSignatureTransform();
            reference.AddTransform(env);
            signedXml.AddReference(reference);

            Assert.Throws<CryptographicException>(() => signedXml.ComputeSignature());
        }

        [Fact]
        [SkipOnPlatform(PlatformSupport.MobileAppleCrypto, "DSA is not available")]
        public void ComputeSignatureMissingReferencedObject()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<foo/>");
            SignedXml signedXml = new SignedXml(doc);
            DSA key = DSA.Create();
            signedXml.SigningKey = key;

            Reference reference = new Reference();
            reference.Uri = "#bleh";

            XmlDsigEnvelopedSignatureTransform env = new XmlDsigEnvelopedSignatureTransform();
            reference.AddTransform(env);
            signedXml.AddReference(reference);

            Assert.Throws<CryptographicException>(() => signedXml.ComputeSignature());
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/20429")]
        [Fact]
        public void DataReferenceToNonDataObject()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<foo Id='id:1'/>");
            SignedXml signedXml = new SignedXml(doc);
            DSA key = DSA.Create();
            signedXml.SigningKey = key;

            Reference reference = new Reference();
            reference.Uri = "#id:1";

            XmlDsigC14NTransform t = new XmlDsigC14NTransform();
            reference.AddTransform(t);
            signedXml.AddReference(reference);

            signedXml.ComputeSignature();
        }

        [Fact]
        public void SignElementWithoutPrefixedNamespace()
        {
            string input = "<Action xmlns='urn:foo'>http://tempuri.org/IFoo/Echo</Action>";
            string expected = @"<Signature xmlns=""http://www.w3.org/2000/09/xmldsig#""><SignedInfo><CanonicalizationMethod Algorithm=""http://www.w3.org/TR/2001/REC-xml-c14n-20010315"" /><SignatureMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#hmac-sha1"" /><Reference URI=""#_1""><Transforms><Transform Algorithm=""http://www.w3.org/TR/2001/REC-xml-c14n-20010315"" /></Transforms><DigestMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#sha1"" /><DigestValue>zdFEZB8rNzvpNG/gFEJBWk/M5Nk=</DigestValue></Reference></SignedInfo><SignatureValue>+OyGVzrHjmKSDWLNyvgx8pjbPfM=</SignatureValue><Object Id=""_1""><Action xmlns=""urn:foo"">http://tempuri.org/IFoo/Echo</Action></Object></Signature>";

            byte[] key = Convert.FromBase64String(
                "1W5EigVnbnRjGLbg99ElieOmuUgYO+KcwMJtE35SAGI=");
            Transform t = new XmlDsigC14NTransform();
            Assert.Equal(expected, SignWithHMACSHA1(input, key, t));
            // The second result should be still identical.
            Assert.Equal(expected, SignWithHMACSHA1(input, key, t));
        }

        [Fact]
        public void SignElementWithPrefixedNamespace()
        {
            string input = "<a:Action xmlns:a='urn:foo'>http://tempuri.org/IFoo/Echo</a:Action>";
            string expected = @"<Signature xmlns=""http://www.w3.org/2000/09/xmldsig#""><SignedInfo><CanonicalizationMethod Algorithm=""http://www.w3.org/TR/2001/REC-xml-c14n-20010315"" /><SignatureMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#hmac-sha1"" /><Reference URI=""#_1""><Transforms><Transform Algorithm=""http://www.w3.org/TR/2001/REC-xml-c14n-20010315"" /></Transforms><DigestMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#sha1"" /><DigestValue>6i5FlqkEfJOdUaOMCK7xn0I0HDg=</DigestValue></Reference></SignedInfo><SignatureValue>tASp+e2A0xqcm02jKg5TGqlhKpI=</SignatureValue><Object Id=""_1""><a:Action xmlns:a=""urn:foo"">http://tempuri.org/IFoo/Echo</a:Action></Object></Signature>";

            byte[] key = Convert.FromBase64String(
                "1W5EigVnbnRjGLbg99ElieOmuUgYO+KcwMJtE35SAGI=");
            Assert.Equal(SignWithHMACSHA1(input, key), expected);
        }

        string SignWithHMACSHA1(string input, byte[] key)
        {
            return SignWithHMACSHA1(input, key, new XmlDsigC14NTransform());
        }

        string SignWithHMACSHA1(string input, byte[] key, Transform transform)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(input);
            SignedXml sxml = new SignedXml(doc);

            HMACSHA1 keyhash = new HMACSHA1(key);
            DataObject d = new DataObject();
            //d.Data = doc.SelectNodes ("//*[local-name()='Body']/*");
            d.Data = doc.SelectNodes("//*[local-name()='Action']");
            d.Id = "_1";
            sxml.AddObject(d);
            Reference r = new Reference("#_1");
            r.AddTransform(transform);
            r.DigestMethod = SignedXml.XmlDsigSHA1Url;
            sxml.SignedInfo.AddReference(r);
            sxml.ComputeSignature(keyhash);
            StringWriter sw = new StringWriter();
            XmlWriter w = new XmlTextWriter(sw);
            sxml.GetXml().WriteTo(w);
            w.Close();
            return sw.ToString();
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/20429")]
        [Fact]
        public void GetIdElement_Null()
        {
            SignedXml sign = new SignedXml();
            Assert.Null(sign.GetIdElement(null, "value"));
            Assert.Null(sign.GetIdElement(new XmlDocument(), null));
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51370", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public void DigestValue_CRLF()
        {
            XmlDocument doc = CreateSomeXml("\r\n");
            XmlDsigExcC14NTransform transform = new XmlDsigExcC14NTransform();
            transform.LoadInput(doc);
            Stream s = (Stream)transform.GetOutput();
            string output = Stream2String(s);
            Assert.Equal("<person>&#xD;\n  <birthplace>Brussels</birthplace>&#xD;\n</person>", output);

            s.Position = 0;

            byte[] digest;
            using (HashAlgorithm hash = SHA1.Create())
            {
                digest = hash.ComputeHash(s);
            }

            Assert.Equal("IKbfdK2/DMfXyezCf5QggVCXfk8=", Convert.ToBase64String(digest));

            X509Certificate2 cert = new X509Certificate2(_pkcs12, "mono");
            SignedXml signedXml = new SignedXml(doc);
            signedXml.SigningKey = cert.GetRSAPrivateKey();
            signedXml.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigExcC14NTransformUrl;
            signedXml.SignedInfo.SignatureMethod = SignedXml.XmlDsigRSASHA1Url;

            Reference reference = new Reference();
            reference.DigestMethod = SignedXml.XmlDsigSHA1Url;
            reference.Uri = "";

            XmlDsigEnvelopedSignatureTransform env = new XmlDsigEnvelopedSignatureTransform();
            reference.AddTransform(env);
            signedXml.AddReference(reference);

            KeyInfo keyInfo = new KeyInfo();
            KeyInfoX509Data x509KeyInfo = new KeyInfoX509Data();
            x509KeyInfo.AddCertificate(new X509Certificate2(_cert));
            x509KeyInfo.AddCertificate(cert);
            keyInfo.AddClause(x509KeyInfo);
            signedXml.KeyInfo = keyInfo;

            signedXml.ComputeSignature();

            digest = reference.DigestValue;
            Assert.Equal("e3dsi1xK8FAx1vsug7J203JbEAU=", Convert.ToBase64String(digest));

            Assert.Equal("<SignedInfo xmlns=\"http://www.w3.org/2000/09/xmldsig#\">"
                + "<CanonicalizationMethod Algorithm=\"http://www.w3.org/2001/10/xml-exc-c14n#\" />"
                + "<SignatureMethod Algorithm=\"http://www.w3.org/2000/09/xmldsig#rsa-sha1\" />"
                + "<Reference URI=\"\">"
                + "<Transforms>"
                + "<Transform Algorithm=\"http://www.w3.org/2000/09/xmldsig#enveloped-signature\" />"
                + "</Transforms>"
                + "<DigestMethod Algorithm=\"http://www.w3.org/2000/09/xmldsig#sha1\" />"
                + "<DigestValue>e3dsi1xK8FAx1vsug7J203JbEAU=</DigestValue>"
                + "</Reference>"
                + "</SignedInfo>", signedXml.SignedInfo.GetXml().OuterXml);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51370", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public void DigestValue_LF()
        {
            XmlDocument doc = CreateSomeXml("\n");
            XmlDsigExcC14NTransform transform = new XmlDsigExcC14NTransform();
            transform.LoadInput(doc);
            Stream s = (Stream)transform.GetOutput();
            string output = Stream2String(s);
            Assert.Equal("<person>\n  <birthplace>Brussels</birthplace>\n</person>", output);

            s.Position = 0;

            byte[] digest;
            using (HashAlgorithm hash = SHA1.Create())
            {
                digest = hash.ComputeHash(s);
            }

            Assert.Equal("e3dsi1xK8FAx1vsug7J203JbEAU=", Convert.ToBase64String(digest));

            X509Certificate2 cert = new X509Certificate2(_pkcs12, "mono");
            SignedXml signedXml = new SignedXml(doc);
            signedXml.SigningKey = cert.GetRSAPrivateKey();
            signedXml.SignedInfo.SignatureMethod = SignedXml.XmlDsigRSASHA1Url;
            signedXml.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigExcC14NTransformUrl;

            Reference reference = new Reference();
            reference.DigestMethod = SignedXml.XmlDsigSHA1Url;
            reference.Uri = "";

            XmlDsigEnvelopedSignatureTransform env = new XmlDsigEnvelopedSignatureTransform();
            reference.AddTransform(env);
            signedXml.AddReference(reference);

            KeyInfo keyInfo = new KeyInfo();
            KeyInfoX509Data x509KeyInfo = new KeyInfoX509Data();
            x509KeyInfo.AddCertificate(new X509Certificate2(_cert));
            x509KeyInfo.AddCertificate(cert);
            keyInfo.AddClause(x509KeyInfo);
            signedXml.KeyInfo = keyInfo;

            signedXml.ComputeSignature();

            digest = reference.DigestValue;
            Assert.Equal("e3dsi1xK8FAx1vsug7J203JbEAU=", Convert.ToBase64String(digest));

            Assert.Equal("<SignedInfo xmlns=\"http://www.w3.org/2000/09/xmldsig#\">"
                + "<CanonicalizationMethod Algorithm=\"http://www.w3.org/2001/10/xml-exc-c14n#\" />"
                + "<SignatureMethod Algorithm=\"http://www.w3.org/2000/09/xmldsig#rsa-sha1\" />"
                + "<Reference URI=\"\">"
                + "<Transforms>"
                + "<Transform Algorithm=\"http://www.w3.org/2000/09/xmldsig#enveloped-signature\" />"
                + "</Transforms>"
                + "<DigestMethod Algorithm=\"http://www.w3.org/2000/09/xmldsig#sha1\" />"
                + "<DigestValue>e3dsi1xK8FAx1vsug7J203JbEAU=</DigestValue>"
                + "</Reference>"
                + "</SignedInfo>", signedXml.SignedInfo.GetXml().OuterXml);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51370", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public void SignedXML_CRLF_Invalid()
        {
            X509Certificate2 cert = new X509Certificate2(_pkcs12, "mono");

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(string.Format(CultureInfo.InvariantCulture,
                "<person>{0}" +
                "  <birthplace>Brussels</birthplace>{0}" +
                "<Signature xmlns=\"http://www.w3.org/2000/09/xmldsig#\">" +
                "<SignedInfo>" +
                "<CanonicalizationMethod Algorithm=\"http://www.w3.org/2001/10/xml-exc-c14n#\" />" +
                "<SignatureMethod Algorithm=\"http://www.w3.org/2000/09/xmldsig#rsa-sha1\" />" +
                "<Reference URI=\"\">" +
                "<Transforms>" +
                "<Transform Algorithm=\"http://www.w3.org/2000/09/xmldsig#enveloped-signature\" />" +
                "</Transforms>" +
                "<DigestMethod Algorithm=\"http://www.w3.org/2000/09/xmldsig#sha1\" />" +
                "<DigestValue>IKbfdK2/DMfXyezCf5QggVCXfk8=</DigestValue>" +
                "</Reference>" +
                "</SignedInfo>" +
                "<SignatureValue>" +
                "JuSd68PyARsZqGKSo5xX5yYHDuu6whHEhoXqxxFmGeEdvkKY2bgroWJ1ZTGHGr" +
                "VI7mtG3h0w1ibOKdltm9j4lZaZWo87CAJiJ2syeLbMyIVSw6OyZEsiFF/VqLKK" +
                "4T4AO6q7HYsC55zJrOvL1j9IIr8zBnJfvBdKckf0lczYbXc=" +
                "</SignatureValue>" +
                "<KeyInfo>" +
                "<X509Data>" +
                "<X509Certificate>" +
                "MIIBozCCAQygAwIBAgIQHc+8iURSTUarmg4trmrnGTANBgkqhkiG9w0BAQUFAD" +
                "ARMQ8wDQYDVQQDEwZOb3ZlbGwwIBcNMDYwOTIxMDcyNjUxWhgPMjA5MDAxMjEw" +
                "ODI2NTFaMA8xDTALBgNVBAMTBE1vbm8wgZ0wDQYJKoZIhvcNAQEBBQADgYsAMI" +
                "GHAoGBAJhFB1KHv2WzsHqih9Mvm3KffEOSMv+sh1mPW3sWI/95VOOVqJnhemMM" +
                "s82phSbNZeoPHny4btbykbRRaRQv94rtIM6geJR1e2c5mfJWtHSq3EYQarHC68" +
                "cAZvCAmQZGa1eQRNRqcTSKX8yfqH0SouIE9ohJtpiluNe+Xgk5fKv3AgERMA0G" +
                "CSqGSIb3DQEBBQUAA4GBAE6pqSgK8QKRHSh6YvYs9oRh1n8iREco7QmZCFj7UB" +
                "kn/QgJ9mKsT8o12VnYHqBCEwBNaT1ay3z/SR4/Z383zuu4Y6xxjqOqnM6gtwUV" +
                "u5/0hvz+ThtuTjItG6Ny5JkLZZQt/XbI5kg920t9jq3vbHBMuX2HxivwQe5sug" +
                "jPaTEY" +
                "</X509Certificate>" +
                "<X509Certificate>" +
                "MIIBpTCCAQ6gAwIBAgIQXo6Lr3rrSkW4xmNPRbHMbjANBgkqhkiG9w0BAQUFAD" +
                "ARMQ8wDQYDVQQDEwZOb3ZlbGwwIBcNMDYwOTIxMDcxNDE4WhgPMjA5MDAxMjEw" +
                "ODE0MThaMBExDzANBgNVBAMTBk1pZ3VlbDCBnTANBgkqhkiG9w0BAQEFAAOBiw" +
                "AwgYcCgYEArCkeSZ6U3U3Fm2qSuQsM7xvvsSzZGQLPDUHFQ/BZxA7LiGRfXbmO" +
                "yPkkYRYItXdy0yDl/8rAjelaL8jQ4me6Uexyeq+5xEgHn9VbNJny5apGNi4kF1" +
                "8DR5DK9Zme9d6icusgW8krv3//5SVE8ao7X5qrIOGS825eCJL73YWbxKkCAREw" +
                "DQYJKoZIhvcNAQEFBQADgYEASqBgYTkIJpDO28ZEXnF5Q/G3xDR/MxhdcrCISJ" +
                "tDbuGVZzK+xhFhiYD5Q1NiGhD4oDIVJPwKmZH4L3YP96iSh6RdtO27V05ET/X5" +
                "yWMKdeIsq6r9jXXv7NaWTmvNfMLKLNgEBCJ00+wN0u4xHUC7yCJc0KNQ3fjDLU" +
                "AT1oaVjWI=" +
                "</X509Certificate>" +
                "</X509Data>" +
                "</KeyInfo>" +
                "</Signature>" +
                "</person>", "\r\n"));

            SignedXml signedXml = new SignedXml(doc);
            XmlNodeList nodeList = doc.GetElementsByTagName("Signature");
            signedXml.LoadXml((XmlElement)nodeList[0]);
            Assert.True(!signedXml.CheckSignature(), "#2");
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51370", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public void SignedXML_CRLF_Valid()
        {
            X509Certificate2 cert = new X509Certificate2(_pkcs12, "mono");

            XmlDocument doc = CreateSignedXml(cert, SignedXml.XmlDsigExcC14NTransformUrl, "\r\n");
            Assert.Equal(string.Format(CultureInfo.InvariantCulture, "<person>{0}" +
                "  <birthplace>Brussels</birthplace>{0}" +
                "<Signature xmlns=\"http://www.w3.org/2000/09/xmldsig#\">" +
                "<SignedInfo>" +
                "<CanonicalizationMethod Algorithm=\"http://www.w3.org/2001/10/xml-exc-c14n#\" />" +
                "<SignatureMethod Algorithm=\"http://www.w3.org/2000/09/xmldsig#rsa-sha1\" />" +
                "<Reference URI=\"\">" +
                "<Transforms>" +
                "<Transform Algorithm=\"http://www.w3.org/2000/09/xmldsig#enveloped-signature\" />" +
                "</Transforms>" +
                "<DigestMethod Algorithm=\"http://www.w3.org/2000/09/xmldsig#sha1\" />" +
                "<DigestValue>e3dsi1xK8FAx1vsug7J203JbEAU=</DigestValue>" +
                "</Reference>" +
                "</SignedInfo>" +
                "<SignatureValue>" +
                "X29nbkOR/Xk3KwsEpEvpDOqfI6/NTtiewIxNqKMrPCoM0HLawK5HKsCw3lL07C" +
                "8SwqvoXJL9VS05gsSia85YCB8NPDeHuHY3CPGT7DVpgeHFA0oefMnOi8IAqKD2" +
                "nx29A222u5OmwbDO0qFqbtsgvIFiP5YJg04cwmnqs+eL+WA=" +
                "</SignatureValue>" +
                "<KeyInfo>" +
                "<X509Data>" +
                "<X509Certificate>" +
                "MIIBozCCAQygAwIBAgIQHc+8iURSTUarmg4trmrnGTANBgkqhkiG9w0BAQUFAD" +
                "ARMQ8wDQYDVQQDEwZOb3ZlbGwwIBcNMDYwOTIxMDcyNjUxWhgPMjA5MDAxMjEw" +
                "ODI2NTFaMA8xDTALBgNVBAMTBE1vbm8wgZ0wDQYJKoZIhvcNAQEBBQADgYsAMI" +
                "GHAoGBAJhFB1KHv2WzsHqih9Mvm3KffEOSMv+sh1mPW3sWI/95VOOVqJnhemMM" +
                "s82phSbNZeoPHny4btbykbRRaRQv94rtIM6geJR1e2c5mfJWtHSq3EYQarHC68" +
                "cAZvCAmQZGa1eQRNRqcTSKX8yfqH0SouIE9ohJtpiluNe+Xgk5fKv3AgERMA0G" +
                "CSqGSIb3DQEBBQUAA4GBAE6pqSgK8QKRHSh6YvYs9oRh1n8iREco7QmZCFj7UB" +
                "kn/QgJ9mKsT8o12VnYHqBCEwBNaT1ay3z/SR4/Z383zuu4Y6xxjqOqnM6gtwUV" +
                "u5/0hvz+ThtuTjItG6Ny5JkLZZQt/XbI5kg920t9jq3vbHBMuX2HxivwQe5sug" +
                "jPaTEY" +
                "</X509Certificate>" +
                "<X509Certificate>" +
                "MIIBpTCCAQ6gAwIBAgIQXo6Lr3rrSkW4xmNPRbHMbjANBgkqhkiG9w0BAQUFAD" +
                "ARMQ8wDQYDVQQDEwZOb3ZlbGwwIBcNMDYwOTIxMDcxNDE4WhgPMjA5MDAxMjEw" +
                "ODE0MThaMBExDzANBgNVBAMTBk1pZ3VlbDCBnTANBgkqhkiG9w0BAQEFAAOBiw" +
                "AwgYcCgYEArCkeSZ6U3U3Fm2qSuQsM7xvvsSzZGQLPDUHFQ/BZxA7LiGRfXbmO" +
                "yPkkYRYItXdy0yDl/8rAjelaL8jQ4me6Uexyeq+5xEgHn9VbNJny5apGNi4kF1" +
                "8DR5DK9Zme9d6icusgW8krv3//5SVE8ao7X5qrIOGS825eCJL73YWbxKkCAREw" +
                "DQYJKoZIhvcNAQEFBQADgYEASqBgYTkIJpDO28ZEXnF5Q/G3xDR/MxhdcrCISJ" +
                "tDbuGVZzK+xhFhiYD5Q1NiGhD4oDIVJPwKmZH4L3YP96iSh6RdtO27V05ET/X5" +
                "yWMKdeIsq6r9jXXv7NaWTmvNfMLKLNgEBCJ00+wN0u4xHUC7yCJc0KNQ3fjDLU" +
                "AT1oaVjWI=" +
                "</X509Certificate>" +
                "</X509Data>" +
                "</KeyInfo>" +
                "</Signature>" +
                "</person>", "\r\n"), doc.OuterXml);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51370", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public void SignedXML_LF_Valid()
        {
            X509Certificate2 cert = new X509Certificate2(_pkcs12, "mono");

            XmlDocument doc = CreateSignedXml(cert, SignedXml.XmlDsigExcC14NTransformUrl, "\n");
            Assert.Equal(string.Format(CultureInfo.InvariantCulture, "<person>{0}" +
                "  <birthplace>Brussels</birthplace>{0}" +
                "<Signature xmlns=\"http://www.w3.org/2000/09/xmldsig#\">" +
                "<SignedInfo>" +
                "<CanonicalizationMethod Algorithm=\"http://www.w3.org/2001/10/xml-exc-c14n#\" />" +
                "<SignatureMethod Algorithm=\"http://www.w3.org/2000/09/xmldsig#rsa-sha1\" />" +
                "<Reference URI=\"\">" +
                "<Transforms>" +
                "<Transform Algorithm=\"http://www.w3.org/2000/09/xmldsig#enveloped-signature\" />" +
                "</Transforms>" +
                "<DigestMethod Algorithm=\"http://www.w3.org/2000/09/xmldsig#sha1\" />" +
                "<DigestValue>e3dsi1xK8FAx1vsug7J203JbEAU=</DigestValue>" +
                "</Reference>" +
                "</SignedInfo>" +
                "<SignatureValue>" +
                "X29nbkOR/Xk3KwsEpEvpDOqfI6/NTtiewIxNqKMrPCoM0HLawK5HKsCw3lL07C" +
                "8SwqvoXJL9VS05gsSia85YCB8NPDeHuHY3CPGT7DVpgeHFA0oefMnOi8IAqKD2" +
                "nx29A222u5OmwbDO0qFqbtsgvIFiP5YJg04cwmnqs+eL+WA=" +
                "</SignatureValue>" +
                "<KeyInfo>" +
                "<X509Data>" +
                "<X509Certificate>" +
                "MIIBozCCAQygAwIBAgIQHc+8iURSTUarmg4trmrnGTANBgkqhkiG9w0BAQUFAD" +
                "ARMQ8wDQYDVQQDEwZOb3ZlbGwwIBcNMDYwOTIxMDcyNjUxWhgPMjA5MDAxMjEw" +
                "ODI2NTFaMA8xDTALBgNVBAMTBE1vbm8wgZ0wDQYJKoZIhvcNAQEBBQADgYsAMI" +
                "GHAoGBAJhFB1KHv2WzsHqih9Mvm3KffEOSMv+sh1mPW3sWI/95VOOVqJnhemMM" +
                "s82phSbNZeoPHny4btbykbRRaRQv94rtIM6geJR1e2c5mfJWtHSq3EYQarHC68" +
                "cAZvCAmQZGa1eQRNRqcTSKX8yfqH0SouIE9ohJtpiluNe+Xgk5fKv3AgERMA0G" +
                "CSqGSIb3DQEBBQUAA4GBAE6pqSgK8QKRHSh6YvYs9oRh1n8iREco7QmZCFj7UB" +
                "kn/QgJ9mKsT8o12VnYHqBCEwBNaT1ay3z/SR4/Z383zuu4Y6xxjqOqnM6gtwUV" +
                "u5/0hvz+ThtuTjItG6Ny5JkLZZQt/XbI5kg920t9jq3vbHBMuX2HxivwQe5sug" +
                "jPaTEY" +
                "</X509Certificate>" +
                "<X509Certificate>" +
                "MIIBpTCCAQ6gAwIBAgIQXo6Lr3rrSkW4xmNPRbHMbjANBgkqhkiG9w0BAQUFAD" +
                "ARMQ8wDQYDVQQDEwZOb3ZlbGwwIBcNMDYwOTIxMDcxNDE4WhgPMjA5MDAxMjEw" +
                "ODE0MThaMBExDzANBgNVBAMTBk1pZ3VlbDCBnTANBgkqhkiG9w0BAQEFAAOBiw" +
                "AwgYcCgYEArCkeSZ6U3U3Fm2qSuQsM7xvvsSzZGQLPDUHFQ/BZxA7LiGRfXbmO" +
                "yPkkYRYItXdy0yDl/8rAjelaL8jQ4me6Uexyeq+5xEgHn9VbNJny5apGNi4kF1" +
                "8DR5DK9Zme9d6icusgW8krv3//5SVE8ao7X5qrIOGS825eCJL73YWbxKkCAREw" +
                "DQYJKoZIhvcNAQEFBQADgYEASqBgYTkIJpDO28ZEXnF5Q/G3xDR/MxhdcrCISJ" +
                "tDbuGVZzK+xhFhiYD5Q1NiGhD4oDIVJPwKmZH4L3YP96iSh6RdtO27V05ET/X5" +
                "yWMKdeIsq6r9jXXv7NaWTmvNfMLKLNgEBCJ00+wN0u4xHUC7yCJc0KNQ3fjDLU" +
                "AT1oaVjWI=" +
                "</X509Certificate>" +
                "</X509Data>" +
                "</KeyInfo>" +
                "</Signature>" +
                "</person>", "\n"), doc.OuterXml);
        }

        [Fact] // part of bug #79454
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51370", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public void MultipleX509Certificates()
        {
            XmlDocument doc = null;
            X509Certificate2 cert = new X509Certificate2(_pkcs12, "mono");

            doc = CreateSignedXml(cert, SignedXml.XmlDsigExcC14NTransformUrl, "\n");
            Assert.True(VerifySignedXml(doc), "#1");

            doc = CreateSignedXml(cert, SignedXml.XmlDsigExcC14NWithCommentsTransformUrl, "\n");
            Assert.True(VerifySignedXml(doc), "#2");

            doc = CreateSignedXml(cert, SignedXml.XmlDsigCanonicalizationUrl, "\n");
            Assert.True(VerifySignedXml(doc), "#3");

            doc = CreateSignedXml(cert, SignedXml.XmlDsigCanonicalizationWithCommentsUrl, "\n");
            Assert.True(VerifySignedXml(doc), "#4");
        }

        // creates a signed XML document with two certificates in the X509Data
        // element, with the second being the one that should be used to verify
        // the signature
        static XmlDocument CreateSignedXml(X509Certificate2 cert, string canonicalizationMethod, string lineFeed)
        {
            XmlDocument doc = CreateSomeXml(lineFeed);

            SignedXml signedXml = new SignedXml(doc);
            signedXml.SigningKey = cert.GetRSAPrivateKey();
            signedXml.SignedInfo.CanonicalizationMethod = canonicalizationMethod;
            signedXml.SignedInfo.SignatureMethod = SignedXml.XmlDsigRSASHA1Url;

            Reference reference = new Reference();
            reference.DigestMethod = SignedXml.XmlDsigSHA1Url;
            reference.Uri = "";

            XmlDsigEnvelopedSignatureTransform env = new XmlDsigEnvelopedSignatureTransform();
            reference.AddTransform(env);
            signedXml.AddReference(reference);

            KeyInfo keyInfo = new KeyInfo();
            KeyInfoX509Data x509KeyInfo = new KeyInfoX509Data();
            x509KeyInfo.AddCertificate(new X509Certificate2(_cert));
            x509KeyInfo.AddCertificate(cert);
            keyInfo.AddClause(x509KeyInfo);
            signedXml.KeyInfo = keyInfo;

            signedXml.ComputeSignature();
            XmlElement xmlDigitalSignature = signedXml.GetXml();

            doc.DocumentElement.AppendChild(doc.ImportNode(xmlDigitalSignature, true));
            return doc;
        }

        static bool VerifySignedXml(XmlDocument signedDoc)
        {
            SignedXml signedXml = new SignedXml(signedDoc);
            XmlNodeList nodeList = signedDoc.GetElementsByTagName("Signature");
            signedXml.LoadXml((XmlElement)nodeList[0]);
            return signedXml.CheckSignature();
        }

        static XmlDocument CreateSomeXml(string lineFeed)
        {
            StringWriter sw = new StringWriter();
            sw.NewLine = lineFeed;
            XmlTextWriter xtw = new XmlTextWriter(sw);
            xtw.Formatting = Formatting.Indented;
            xtw.WriteStartElement("person");
            xtw.WriteElementString("birthplace", "Brussels");
            xtw.WriteEndElement();
            xtw.Flush();

            XmlDocument doc = new XmlDocument();
            doc.PreserveWhitespace = true;
            doc.Load(new StringReader(sw.ToString()));
            return doc;
        }

        string Stream2String(Stream s)
        {
            StringBuilder sb = new StringBuilder();
            int b = s.ReadByte();
            while (b != -1)
            {
                sb.Append(Convert.ToChar(b));
                b = s.ReadByte();
            }
            return sb.ToString();
        }

        private static byte[] _cert = new byte[] {
            0x30, 0x82, 0x01, 0xa3, 0x30, 0x82, 0x01, 0x0c, 0xa0, 0x03, 0x02,
            0x01, 0x02, 0x02, 0x10, 0x1d, 0xcf, 0xbc, 0x89, 0x44, 0x52, 0x4d,
            0x46, 0xab, 0x9a, 0x0e, 0x2d, 0xae, 0x6a, 0xe7, 0x19, 0x30, 0x0d,
            0x06, 0x09, 0x2a, 0x86, 0x48, 0x86, 0xf7, 0x0d, 0x01, 0x01, 0x05,
            0x05, 0x00, 0x30, 0x11, 0x31, 0x0f, 0x30, 0x0d, 0x06, 0x03, 0x55,
            0x04, 0x03, 0x13, 0x06, 0x4e, 0x6f, 0x76, 0x65, 0x6c, 0x6c, 0x30,
            0x20, 0x17, 0x0d, 0x30, 0x36, 0x30, 0x39, 0x32, 0x31, 0x30, 0x37,
            0x32, 0x36, 0x35, 0x31, 0x5a, 0x18, 0x0f, 0x32, 0x30, 0x39, 0x30,
            0x30, 0x31, 0x32, 0x31, 0x30, 0x38, 0x32, 0x36, 0x35, 0x31, 0x5a,
            0x30, 0x0f, 0x31, 0x0d, 0x30, 0x0b, 0x06, 0x03, 0x55, 0x04, 0x03,
            0x13, 0x04, 0x4d, 0x6f, 0x6e, 0x6f, 0x30, 0x81, 0x9d, 0x30, 0x0d,
            0x06, 0x09, 0x2a, 0x86, 0x48, 0x86, 0xf7, 0x0d, 0x01, 0x01, 0x01,
            0x05, 0x00, 0x03, 0x81, 0x8b, 0x00, 0x30, 0x81, 0x87, 0x02, 0x81,
            0x81, 0x00, 0x98, 0x45, 0x07, 0x52, 0x87, 0xbf, 0x65, 0xb3, 0xb0,
            0x7a, 0xa2, 0x87, 0xd3, 0x2f, 0x9b, 0x72, 0x9f, 0x7c, 0x43, 0x92,
            0x32, 0xff, 0xac, 0x87, 0x59, 0x8f, 0x5b, 0x7b, 0x16, 0x23, 0xff,
            0x79, 0x54, 0xe3, 0x95, 0xa8, 0x99, 0xe1, 0x7a, 0x63, 0x0c, 0xb3,
            0xcd, 0xa9, 0x85, 0x26, 0xcd, 0x65, 0xea, 0x0f, 0x1e, 0x7c, 0xb8,
            0x6e, 0xd6, 0xf2, 0x91, 0xb4, 0x51, 0x69, 0x14, 0x2f, 0xf7, 0x8a,
            0xed, 0x20, 0xce, 0xa0, 0x78, 0x94, 0x75, 0x7b, 0x67, 0x39, 0x99,
            0xf2, 0x56, 0xb4, 0x74, 0xaa, 0xdc, 0x46, 0x10, 0x6a, 0xb1, 0xc2,
            0xeb, 0xc7, 0x00, 0x66, 0xf0, 0x80, 0x99, 0x06, 0x46, 0x6b, 0x57,
            0x90, 0x44, 0xd4, 0x6a, 0x71, 0x34, 0x8a, 0x5f, 0xcc, 0x9f, 0xa8,
            0x7d, 0x12, 0xa2, 0xe2, 0x04, 0xf6, 0x88, 0x49, 0xb6, 0x98, 0xa5,
            0xb8, 0xd7, 0xbe, 0x5e, 0x09, 0x39, 0x7c, 0xab, 0xf7, 0x02, 0x01,
            0x11, 0x30, 0x0d, 0x06, 0x09, 0x2a, 0x86, 0x48, 0x86, 0xf7, 0x0d,
            0x01, 0x01, 0x05, 0x05, 0x00, 0x03, 0x81, 0x81, 0x00, 0x4e, 0xa9,
            0xa9, 0x28, 0x0a, 0xf1, 0x02, 0x91, 0x1d, 0x28, 0x7a, 0x62, 0xf6,
            0x2c, 0xf6, 0x84, 0x61, 0xd6, 0x7f, 0x22, 0x44, 0x47, 0x28, 0xed,
            0x09, 0x99, 0x08, 0x58, 0xfb, 0x50, 0x19, 0x27, 0xfd, 0x08, 0x09,
            0xf6, 0x62, 0xac, 0x4f, 0xca, 0x35, 0xd9, 0x59, 0xd8, 0x1e, 0xa0,
            0x42, 0x13, 0x00, 0x4d, 0x69, 0x3d, 0x5a, 0xcb, 0x7c, 0xff, 0x49,
            0x1e, 0x3f, 0x67, 0x7f, 0x37, 0xce, 0xeb, 0xb8, 0x63, 0xac, 0x71,
            0x8e, 0xa3, 0xaa, 0x9c, 0xce, 0xa0, 0xb7, 0x05, 0x15, 0xbb, 0x9f,
            0xf4, 0x86, 0xfc, 0xfe, 0x4e, 0x1b, 0x6e, 0x4e, 0x32, 0x2d, 0x1b,
            0xa3, 0x72, 0xe4, 0x99, 0x0b, 0x65, 0x94, 0x2d, 0xfd, 0x76, 0xc8,
            0xe6, 0x48, 0x3d, 0xdb, 0x4b, 0x7d, 0x8e, 0xad, 0xef, 0x6c, 0x70,
            0x4c, 0xb9, 0x7d, 0x87, 0xc6, 0x2b, 0xf0, 0x41, 0xee, 0x6c, 0xba,
            0x08, 0xcf, 0x69, 0x31, 0x18 };

        private static byte[] _pkcs12 = new byte[] {
            0x30, 0x82, 0x05, 0x8d, 0x02, 0x01, 0x03, 0x30, 0x82, 0x05, 0x47,
            0x06, 0x09, 0x2a, 0x86, 0x48, 0x86, 0xf7, 0x0d, 0x01, 0x07, 0x01,
            0xa0, 0x82, 0x05, 0x38, 0x04, 0x82, 0x05, 0x34, 0x30, 0x82, 0x05,
            0x30, 0x30, 0x82, 0x02, 0x3f, 0x06, 0x09, 0x2a, 0x86, 0x48, 0x86,
            0xf7, 0x0d, 0x01, 0x07, 0x06, 0xa0, 0x82, 0x02, 0x30, 0x30, 0x82,
            0x02, 0x2c, 0x02, 0x01, 0x00, 0x30, 0x82, 0x02, 0x25, 0x06, 0x09,
            0x2a, 0x86, 0x48, 0x86, 0xf7, 0x0d, 0x01, 0x07, 0x01, 0x30, 0x1c,
            0x06, 0x0a, 0x2a, 0x86, 0x48, 0x86, 0xf7, 0x0d, 0x01, 0x0c, 0x01,
            0x03, 0x30, 0x0e, 0x04, 0x08, 0x6e, 0x0a, 0x50, 0x20, 0xc3, 0x11,
            0x49, 0x07, 0x02, 0x02, 0x07, 0xd0, 0x80, 0x82, 0x01, 0xf8, 0x74,
            0x40, 0x07, 0x44, 0x6b, 0x80, 0x46, 0xe1, 0x4e, 0x65, 0x5e, 0xf2,
            0xf6, 0x38, 0x90, 0xd1, 0x75, 0x24, 0xd9, 0x72, 0x92, 0x5b, 0x4a,
            0xb9, 0x9e, 0xbd, 0xab, 0xe2, 0xb8, 0x91, 0xc9, 0x48, 0x14, 0x88,
            0x61, 0x7d, 0x06, 0xf9, 0x24, 0x80, 0xb5, 0x36, 0xaf, 0xfe, 0xc0,
            0x59, 0x00, 0x39, 0x3f, 0x78, 0xc0, 0x57, 0xea, 0x1e, 0xcb, 0x29,
            0xa4, 0x5f, 0xba, 0x4b, 0xd9, 0xca, 0x95, 0xab, 0x55, 0x4a, 0x11,
            0x1a, 0xf8, 0xe9, 0xd4, 0xc0, 0x08, 0x55, 0xfb, 0x69, 0x09, 0x0d,
            0x5b, 0xed, 0x02, 0xcc, 0x55, 0xfe, 0x05, 0x2e, 0x45, 0xa7, 0x8d,
            0x63, 0x9a, 0xda, 0x6c, 0xc7, 0xe1, 0xcb, 0x5c, 0xa7, 0xd9, 0x9b,
            0x4a, 0xfb, 0x7d, 0x31, 0xe5, 0x89, 0x3e, 0xf2, 0x32, 0xc9, 0x78,
            0xd0, 0x66, 0x1e, 0x38, 0xc7, 0xbf, 0x41, 0xf9, 0xe7, 0xbd, 0xce,
            0x8b, 0xc3, 0x14, 0x19, 0x4b, 0xfa, 0x3a, 0xa2, 0x1f, 0xb0, 0xd4,
            0xfa, 0x33, 0x39, 0x12, 0xd9, 0x36, 0x7f, 0x7e, 0xf0, 0xc4, 0xdc,
            0xf0, 0xb5, 0x7a, 0x50, 0x2c, 0x99, 0x9d, 0x02, 0x40, 0xec, 0x6a,
            0x23, 0x83, 0x16, 0xec, 0x8f, 0x58, 0x14, 0xa0, 0xa0, 0x9c, 0xa0,
            0xe1, 0xd0, 0x6f, 0x54, 0x1a, 0x10, 0x47, 0x69, 0x6b, 0x55, 0x7f,
            0x67, 0x7d, 0xb8, 0x38, 0xa0, 0x40, 0x99, 0x13, 0xe8, 0x15, 0x73,
            0x8d, 0x18, 0x86, 0x29, 0x74, 0xec, 0x66, 0xa3, 0xb8, 0x14, 0x10,
            0x61, 0xef, 0xa5, 0x79, 0x89, 0x01, 0xaa, 0xf2, 0x1f, 0x0c, 0xdd,
            0x0d, 0x8c, 0xbb, 0x7a, 0x4e, 0x0f, 0x47, 0x91, 0x37, 0xa3, 0x8a,
            0x43, 0x0f, 0xeb, 0xc7, 0x9b, 0x8d, 0xaf, 0x39, 0xdf, 0x23, 0x1c,
            0xa4, 0xf7, 0x66, 0x1c, 0x61, 0x42, 0x24, 0x9a, 0x0a, 0x3a, 0x31,
            0x9c, 0x51, 0xa2, 0x30, 0xbe, 0x85, 0xa6, 0xe8, 0x18, 0xfa, 0x8b,
            0xff, 0xdd, 0xdc, 0x34, 0x46, 0x4f, 0x15, 0xde, 0xdb, 0xc4, 0xeb,
            0x62, 0x3b, 0x7c, 0x25, 0x1a, 0x13, 0x8b, 0xda, 0x3b, 0x59, 0x2a,
            0xb8, 0x50, 0xe3, 0x9f, 0x76, 0xfc, 0xe8, 0x00, 0xfc, 0xf7, 0xba,
            0xd2, 0x45, 0x92, 0x14, 0xb5, 0xe2, 0x93, 0x41, 0x09, 0xea, 0x5b,
            0x5e, 0xda, 0x66, 0x92, 0xd1, 0x93, 0x7a, 0xc0, 0xe1, 0x2f, 0xed,
            0x29, 0x78, 0x80, 0xff, 0x79, 0x0e, 0xda, 0x78, 0x7e, 0x71, 0xa4,
            0x31, 0x2f, 0xe9, 0x48, 0xab, 0xc9, 0x40, 0x7d, 0x63, 0x06, 0xd6,
            0xb5, 0x2b, 0x49, 0xba, 0x43, 0x56, 0x69, 0xc5, 0xc2, 0x85, 0x37,
            0xdb, 0xe7, 0x39, 0x87, 0x8d, 0x14, 0x15, 0x55, 0x76, 0x3f, 0x70,
            0xf6, 0xd7, 0x80, 0x82, 0x48, 0x02, 0x64, 0xe1, 0x73, 0x1a, 0xd9,
            0x35, 0x1a, 0x43, 0xf3, 0xde, 0xd4, 0x00, 0x9d, 0x49, 0x2b, 0xc6,
            0x66, 0x19, 0x3e, 0xb8, 0xcc, 0x43, 0xcc, 0xa8, 0x12, 0xa4, 0xad,
            0xcd, 0xe2, 0xe6, 0xb3, 0xdd, 0x7e, 0x80, 0x50, 0xc0, 0xb4, 0x0c,
            0x4c, 0xd2, 0x31, 0xf3, 0xf8, 0x49, 0x31, 0xbe, 0xf2, 0x7d, 0x60,
            0x38, 0xe0, 0x60, 0xdf, 0x7b, 0x58, 0xe0, 0xf9, 0x6e, 0x68, 0x79,
            0x33, 0xb2, 0x2a, 0x53, 0x4c, 0x5a, 0x9d, 0xb3, 0x81, 0x4b, 0x19,
            0x21, 0xe2, 0x3a, 0x42, 0x07, 0x25, 0x5a, 0xee, 0x1f, 0x5d, 0xa2,
            0xca, 0xf7, 0x2f, 0x3c, 0x9b, 0xb0, 0xbc, 0xe7, 0xaf, 0x8c, 0x2f,
            0x52, 0x43, 0x79, 0x94, 0xb0, 0xee, 0xc4, 0x53, 0x09, 0xc0, 0xc9,
            0x21, 0x39, 0x64, 0x82, 0xc3, 0x54, 0xb8, 0x65, 0xf8, 0xdc, 0xb3,
            0xdf, 0x4d, 0xc4, 0x63, 0x59, 0x14, 0x37, 0xd6, 0xba, 0xa3, 0x98,
            0xda, 0x99, 0x02, 0xdd, 0x7a, 0x87, 0x3e, 0x34, 0xb5, 0x4b, 0x0a,
            0xb4, 0x2d, 0xea, 0x19, 0x24, 0xd1, 0xc2, 0x9f, 0x30, 0x82, 0x02,
            0xe9, 0x06, 0x09, 0x2a, 0x86, 0x48, 0x86, 0xf7, 0x0d, 0x01, 0x07,
            0x01, 0xa0, 0x82, 0x02, 0xda, 0x04, 0x82, 0x02, 0xd6, 0x30, 0x82,
            0x02, 0xd2, 0x30, 0x82, 0x02, 0xce, 0x06, 0x0b, 0x2a, 0x86, 0x48,
            0x86, 0xf7, 0x0d, 0x01, 0x0c, 0x0a, 0x01, 0x02, 0xa0, 0x82, 0x02,
            0xa6, 0x30, 0x82, 0x02, 0xa2, 0x30, 0x1c, 0x06, 0x0a, 0x2a, 0x86,
            0x48, 0x86, 0xf7, 0x0d, 0x01, 0x0c, 0x01, 0x03, 0x30, 0x0e, 0x04,
            0x08, 0xe0, 0x21, 0x4f, 0x90, 0x7d, 0x86, 0x72, 0xc7, 0x02, 0x02,
            0x07, 0xd0, 0x04, 0x82, 0x02, 0x80, 0x92, 0xac, 0xe8, 0x52, 0xa6,
            0x3e, 0xed, 0x3d, 0xbc, 0x28, 0x5f, 0xb9, 0x45, 0x76, 0x27, 0x95,
            0xf8, 0x6a, 0xc5, 0x17, 0x97, 0x46, 0x58, 0xe9, 0x15, 0x7c, 0x68,
            0x62, 0x67, 0xb5, 0x2f, 0x1b, 0x64, 0x27, 0x9d, 0xfd, 0x67, 0x66,
            0x42, 0x21, 0x5c, 0xf4, 0x64, 0x37, 0xcc, 0xc0, 0x04, 0x01, 0x91,
            0x6c, 0x6b, 0x84, 0x96, 0xae, 0x04, 0xfe, 0xcc, 0x88, 0x6a, 0x84,
            0xd7, 0x59, 0x28, 0x78, 0xc9, 0xb4, 0xf6, 0x4d, 0x86, 0x8d, 0x59,
            0xc6, 0x74, 0x30, 0xca, 0x2f, 0x0a, 0xa7, 0x66, 0x99, 0xf4, 0x8f,
            0x44, 0x6d, 0x97, 0x3c, 0xd6, 0xdb, 0xd6, 0x31, 0x8c, 0xf7, 0x75,
            0xd9, 0x0b, 0xf5, 0xd2, 0x27, 0x80, 0x81, 0x28, 0x0f, 0x6b, 0x8b,
            0x45, 0x11, 0x08, 0x1d, 0x06, 0x31, 0x4d, 0x98, 0x68, 0xc9, 0x09,
            0x9b, 0x51, 0x84, 0x81, 0x74, 0x76, 0x57, 0x63, 0xb5, 0x38, 0xc8,
            0xe1, 0x96, 0xe4, 0xcd, 0xd4, 0xe8, 0xf8, 0x26, 0x88, 0x88, 0xaa,
            0xdf, 0x1b, 0xc6, 0x37, 0xb8, 0xc4, 0xe1, 0xcb, 0xc0, 0x71, 0x3d,
            0xd6, 0xd7, 0x8b, 0xc6, 0xec, 0x5f, 0x42, 0x86, 0xb0, 0x8d, 0x1c,
            0x49, 0xb9, 0xc6, 0x96, 0x11, 0xa5, 0xd6, 0xd2, 0xc0, 0x18, 0xca,
            0xe7, 0xf6, 0x93, 0xb4, 0xf5, 0x7a, 0xe4, 0xec, 0xa2, 0x90, 0xf8,
            0xef, 0x66, 0x0f, 0xa8, 0x52, 0x0c, 0x3f, 0x85, 0x4a, 0x76, 0x3a,
            0xb8, 0x5a, 0x2d, 0x03, 0x5d, 0x99, 0x70, 0xbb, 0x02, 0x1c, 0x77,
            0x43, 0x12, 0xd9, 0x1f, 0x7c, 0x6f, 0x69, 0x15, 0x17, 0x30, 0x51,
            0x7d, 0x53, 0xc2, 0x06, 0xe0, 0xd2, 0x31, 0x17, 0x2a, 0x98, 0xe3,
            0xe0, 0x20, 0xfb, 0x01, 0xfd, 0xd1, 0x1b, 0x50, 0x00, 0xad, 0x1d,
            0xff, 0xa1, 0xae, 0xd6, 0xac, 0x38, 0x8b, 0x71, 0x28, 0x44, 0x66,
            0x8c, 0xb6, 0x34, 0xc5, 0x86, 0xc9, 0x34, 0xda, 0x6c, 0x2a, 0xef,
            0x69, 0x3c, 0xb7, 0xbd, 0xa5, 0x05, 0x3c, 0x7c, 0xfb, 0x0c, 0x2d,
            0x49, 0x09, 0xdb, 0x91, 0x3b, 0x41, 0x2a, 0xe4, 0xfa, 0x4a, 0xc2,
            0xea, 0x9e, 0x6f, 0xc3, 0x46, 0x2a, 0x77, 0x83, 0x4e, 0x22, 0x01,
            0xfb, 0x0c, 0x2d, 0x5a, 0xcf, 0x8d, 0xa7, 0x55, 0x24, 0x7c, 0xda,
            0x9e, 0xd8, 0xbc, 0xf6, 0x81, 0x63, 0x8a, 0x36, 0xd0, 0x13, 0x74,
            0x30, 0x4d, 0xd8, 0x4e, 0xa6, 0x81, 0x71, 0x71, 0xff, 0x9f, 0xf3,
            0x8d, 0x75, 0xad, 0x6b, 0x93, 0x93, 0x8c, 0xf8, 0x7d, 0xa6, 0x62,
            0x9d, 0xf7, 0x86, 0x6f, 0xcb, 0x5b, 0x6f, 0xe5, 0xee, 0xcd, 0xb0,
            0xb2, 0xfd, 0x96, 0x2c, 0xde, 0xa0, 0xcf, 0x46, 0x8c, 0x66, 0x0e,
            0xf9, 0xa3, 0xdb, 0xfa, 0x8f, 0x1b, 0x54, 0x9d, 0x13, 0x13, 0x6b,
            0x97, 0x43, 0x97, 0x64, 0xec, 0x2a, 0xc5, 0xc0, 0x26, 0xab, 0xea,
            0x37, 0xd6, 0xcb, 0xb9, 0x83, 0x18, 0x53, 0x5a, 0xcd, 0x28, 0xb3,
            0x3b, 0x9c, 0x13, 0xaa, 0x78, 0x6c, 0xcf, 0xe9, 0x75, 0x7c, 0x80,
            0x04, 0x05, 0x52, 0xda, 0x13, 0x41, 0xb0, 0x27, 0x0f, 0x82, 0xa3,
            0x81, 0xd8, 0xf7, 0xdc, 0x61, 0xbb, 0x98, 0x32, 0x5a, 0x88, 0xbf,
            0x49, 0xc1, 0x76, 0x83, 0xcd, 0xc4, 0xb4, 0xca, 0x8d, 0x36, 0x88,
            0xee, 0xdb, 0xc5, 0xf4, 0x13, 0x28, 0x4d, 0xae, 0x7a, 0x31, 0x3e,
            0x77, 0x19, 0xab, 0x11, 0x15, 0x29, 0xd4, 0xcf, 0xb4, 0x73, 0x36,
            0x92, 0x1e, 0x4e, 0x5d, 0x35, 0x57, 0x84, 0x45, 0x9d, 0x05, 0x3c,
            0x44, 0x86, 0x08, 0x0b, 0x90, 0x29, 0xf9, 0xe6, 0x48, 0xaf, 0xf4,
            0x62, 0xd2, 0x4d, 0x32, 0x1a, 0xe9, 0xbf, 0x3a, 0x7b, 0x25, 0x4a,
            0x03, 0xfb, 0x40, 0x1d, 0x71, 0x2c, 0x10, 0x54, 0xdc, 0xbf, 0xf4,
            0x50, 0x85, 0x15, 0x11, 0xb1, 0x2d, 0x03, 0x2c, 0xe4, 0x8a, 0xce,
            0xec, 0x6e, 0x46, 0x06, 0x13, 0x3c, 0x97, 0x8d, 0xdd, 0xf6, 0x1e,
            0x62, 0xb4, 0x8d, 0xfa, 0x2c, 0x86, 0x87, 0x64, 0x5e, 0xec, 0xc8,
            0x84, 0xd1, 0x3d, 0xc5, 0x76, 0x4a, 0x31, 0xd3, 0xdb, 0x34, 0x6e,
            0x8a, 0x49, 0xd6, 0x38, 0xbb, 0x05, 0xe9, 0x4d, 0xf1, 0xde, 0x3e,
            0xa4, 0x47, 0xdd, 0xe8, 0xa8, 0xf1, 0xba, 0x55, 0xce, 0xca, 0x5b,
            0x57, 0xd7, 0xc8, 0x9f, 0x09, 0xa3, 0x8e, 0x58, 0x83, 0x21, 0x0a,
            0x6e, 0xd3, 0x70, 0x9c, 0xb9, 0x7c, 0x52, 0x98, 0x53, 0xcb, 0xda,
            0x9d, 0xaf, 0xb7, 0x4b, 0xf7, 0x48, 0x91, 0x7e, 0x78, 0x20, 0x19,
            0xe3, 0x41, 0x9d, 0xc8, 0x68, 0x11, 0xfb, 0x5f, 0x6b, 0xc8, 0x09,
            0x74, 0xcb, 0x76, 0x08, 0xbc, 0x28, 0x63, 0x57, 0x04, 0xb0, 0x80,
            0xd1, 0x53, 0x60, 0x50, 0x44, 0xba, 0x80, 0x48, 0x5e, 0x0e, 0x9a,
            0xe5, 0x64, 0x26, 0x7a, 0x88, 0xb9, 0xc6, 0x33, 0x31, 0x15, 0x30,
            0x13, 0x06, 0x09, 0x2a, 0x86, 0x48, 0x86, 0xf7, 0x0d, 0x01, 0x09,
            0x15, 0x31, 0x06, 0x04, 0x04, 0x01, 0x00, 0x00, 0x00, 0x30, 0x3d,
            0x30, 0x21, 0x30, 0x09, 0x06, 0x05, 0x2b, 0x0e, 0x03, 0x02, 0x1a,
            0x05, 0x00, 0x04, 0x14, 0x32, 0x55, 0x07, 0xa2, 0x67, 0xf3, 0x76,
            0x4d, 0x0b, 0x6f, 0xa4, 0xa0, 0x7b, 0xce, 0x2f, 0xc5, 0xff, 0xbe,
            0x3e, 0x38, 0x04, 0x14, 0x52, 0xf8, 0xb3, 0xeb, 0xc3, 0xda, 0x79,
            0xfa, 0x75, 0x89, 0x67, 0x33, 0x01, 0xd0, 0xb0, 0x13, 0xfa, 0x11,
            0x94, 0xac, 0x02, 0x02, 0x07, 0xd0 };

        public SignedXml SignHMAC(string uri, KeyedHashAlgorithm mac, bool expectedToVerify)
        {
            string input = "<foo/>";

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(input);

            SignedXml sig = new SignedXml(doc);
            Reference r = new Reference("");
            r.AddTransform(new XmlDsigEnvelopedSignatureTransform());
            r.DigestMethod = uri;
            sig.AddReference(r);

            sig.ComputeSignature(mac);
            doc.DocumentElement.AppendChild(doc.ImportNode(sig.GetXml(), true));
            // doc.Save(System.Console.Out);

            sig.LoadXml(doc.DocumentElement["Signature"]);
            Assert.Equal(expectedToVerify, sig.CheckSignature(mac));
            return sig;
        }

        private static byte[] emptyHmacKey = new byte[0];
        private static byte[] badKey = new byte[3] { 1, 2, 3 };

        public static object[][] HmacKeys = new object[][] {
            new object[] { new byte[0] },
            new object[] { new byte[5] { 1, 2, 3, 4, 10 } }
        };

        private const string moreHmacMD5 = "http://www.w3.org/2001/04/xmldsig-more#hmac-md5";
        private const string more256 = "http://www.w3.org/2001/04/xmldsig-more#hmac-sha256";
        private const string more384 = "http://www.w3.org/2001/04/xmldsig-more#hmac-sha384";
        private const string more512 = "http://www.w3.org/2001/04/xmldsig-more#hmac-sha512";
        private const string moreripe = "http://www.w3.org/2001/04/xmldsig-more#hmac-ripemd160";

        [Theory, MemberData(nameof(HmacKeys))]
        public void SignHMAC_SHA256(byte[] hmackey)
        {
            var hmac = new HMACSHA256(hmackey);
            Assert.Equal(hmackey, hmac.Key);

            SignedXml sign = SignHMAC(EncryptedXml.XmlEncSHA256Url, hmac, true);
            Assert.Equal(more256, sign.SignatureMethod);
        }

        [Theory, MemberData(nameof(HmacKeys))]
        public void SignHMAC_SHA256_Bad(byte[] hmackey)
        {
            SignedXml sign = SignHMAC(more256, new HMACSHA256(hmackey), false);
            Assert.Equal(more256, sign.SignatureMethod);
        }

        [Fact]
        public void VerifyHMAC_SHA256()
        {
            string xml = "<?xml version=\"1.0\" encoding=\"Windows-1252\"?><foo><Signature xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><SignedInfo><CanonicalizationMethod Algorithm=\"http://www.w3.org/TR/2001/REC-xml-c14n-20010315\" /><SignatureMethod Algorithm=\"http://www.w3.org/2001/04/xmldsig-more#hmac-sha256\" /><Reference URI=\"\"><Transforms><Transform Algorithm=\"http://www.w3.org/2000/09/xmldsig#enveloped-signature\" /></Transforms><DigestMethod Algorithm=\"http://www.w3.org/2001/04/xmlenc#sha256\" /><DigestValue>sKG2hDPEHiPrzpd3QA8BZ0eMzMbSEPPMh9QqXgkP7Cs=</DigestValue></Reference></SignedInfo><SignatureValue>Faad3KInJdIpaGcBn5e04Zv080u45fSjAKqrgevdWQw=</SignatureValue></Signature></foo>";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            SignedXml sign = new SignedXml(doc);
            sign.LoadXml(doc.DocumentElement["Signature"]);

            // https://github.com/dotnet/runtime/issues/21236
            if (!PlatformDetection.IsNetFramework)
            {
                Assert.False(sign.CheckSignature(new HMACSHA256(badKey)));
            }

            Assert.True(sign.CheckSignature(new HMACSHA256(emptyHmacKey)));
        }

        [Theory, MemberData(nameof(HmacKeys))]
        public void SignHMAC_SHA512(byte[] hmackey)
        {
            var hmac = new HMACSHA512(hmackey);
            Assert.Equal(hmackey, hmac.Key);

            SignedXml sign = SignHMAC(EncryptedXml.XmlEncSHA512Url, hmac, true);
            Assert.Equal(more512, sign.SignatureMethod);
        }

        [Theory, MemberData(nameof(HmacKeys))]
        public void SignHMAC_SHA512_Bad(byte[] hmackey)
        {
            SignedXml sign = SignHMAC(more512, new HMACSHA512(hmackey), false);
            Assert.Equal(more512, sign.SignatureMethod);
        }

        [Fact]
        public void VerifyHMAC_SHA512()
        {
            string xml = "<?xml version=\"1.0\" encoding=\"Windows-1252\"?><foo><Signature xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><SignedInfo><CanonicalizationMethod Algorithm=\"http://www.w3.org/TR/2001/REC-xml-c14n-20010315\" /><SignatureMethod Algorithm=\"http://www.w3.org/2001/04/xmldsig-more#hmac-sha512\" /><Reference URI=\"\"><Transforms><Transform Algorithm=\"http://www.w3.org/2000/09/xmldsig#enveloped-signature\" /></Transforms><DigestMethod Algorithm=\"http://www.w3.org/2001/04/xmlenc#sha512\" /><DigestValue>2dvMkpTUE8Z76ResJG9pwPpVJNYo7t6s2L02V81xUVJ0oF8yJ7v8CTojjuL76s0iVdBxAOhP80Ambd1YaSkwSw==</DigestValue></Reference></SignedInfo><SignatureValue>wFenihRlm1x5/n0cifdX/TDOYqlbg2oVIbD/gyrAc0Q2hiIUTtwfBIMY5rQhKcErSz6YNoIl8RBQBmww/0rv5g==</SignatureValue></Signature></foo>";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            SignedXml sign = new SignedXml(doc);
            sign.LoadXml(doc.DocumentElement["Signature"]);

            // https://github.com/dotnet/runtime/issues/21236
            if (!PlatformDetection.IsNetFramework)
            {
                Assert.False(sign.CheckSignature(new HMACSHA512(badKey)));
            }

            Assert.True(sign.CheckSignature(new HMACSHA512(emptyHmacKey)));
        }

        [Theory, MemberData(nameof(HmacKeys))]
        public void SignHMAC_SHA384(byte[] hmackey)
        {
            var hmac = new HMACSHA384(hmackey);
            Assert.Equal(hmackey, hmac.Key);

            // works as long as the string can be used by CryptoConfig to create
            // an instance of the required hash algorithm
            SignedXml sign = SignHMAC("SHA384", hmac, true);
            Assert.Equal(more384, sign.SignatureMethod);
        }

        [Theory, MemberData(nameof(HmacKeys))]
        public void SignHMAC_SHA384_Bad(byte[] hmackey)
        {
            // we can't verity the signature if the URI is used
            SignedXml sign = SignHMAC(more384, new HMACSHA384(hmackey), false);
            Assert.Equal(more384, sign.SignatureMethod);
        }

        [Fact]
        public void VerifyHMAC_SHA384()
        {
            string xml = "<?xml version=\"1.0\" encoding=\"Windows-1252\"?><foo><Signature xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><SignedInfo><CanonicalizationMethod Algorithm=\"http://www.w3.org/TR/2001/REC-xml-c14n-20010315\" /><SignatureMethod Algorithm=\"http://www.w3.org/2001/04/xmldsig-more#hmac-sha384\" /><Reference URI=\"\"><Transforms><Transform Algorithm=\"http://www.w3.org/2000/09/xmldsig#enveloped-signature\" /></Transforms><DigestMethod Algorithm=\"SHA384\" /><DigestValue>kH9C0LeZocNVXhjfzpz00M5fc3WJf0QU8gxK4I7pAN7HN602yHo8yYDSlG14b5YS</DigestValue></Reference></SignedInfo><SignatureValue>LgydOfhv8nqpFLFPC+xg3ZnjC8D+V3mpzxv6GOdH1HDdw1r+LH/BFM2U7dntxgf0</SignatureValue></Signature></foo>";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            SignedXml sign = new SignedXml(doc);
            sign.LoadXml(doc.DocumentElement["Signature"]);

            // https://github.com/dotnet/runtime/issues/21236
            if (!PlatformDetection.IsNetFramework)
            {
                Assert.False(sign.CheckSignature(new HMACSHA384(badKey)));
            }

            Assert.True(sign.CheckSignature(new HMACSHA384(emptyHmacKey)));
        }

        [Theory, MemberData(nameof(HmacKeys))]
        public void SignHMAC_MD5(byte[] hmackey)
        {
            var hmac = new HMACMD5(hmackey);
            Assert.Equal(hmackey, hmac.Key);

            // works as long as the string can be used by CryptoConfig to create
            // an instance of the required hash algorithm
            SignedXml sign = SignHMAC("MD5", hmac, true);
            Assert.Equal(moreHmacMD5, sign.SignatureMethod);
        }

        [Theory, MemberData(nameof(HmacKeys))]
        public void SignHMAC_MD5_Bad(byte[] hmackey)
        {
            // we can't verity the signature if the URI is used
            SignedXml sign = SignHMAC(moreHmacMD5, new HMACMD5(hmackey), false);
            Assert.Equal(moreHmacMD5, sign.SignatureMethod);
        }

        [Fact]
        public void VerifyHMAC_MD5()
        {
            string xml = @"<?xml version=""1.0"" encoding=""Windows-1252""?><foo><Signature xmlns=""http://www.w3.org/2000/09/xmldsig#""><SignedInfo><CanonicalizationMethod Algorithm=""http://www.w3.org/TR/2001/REC-xml-c14n-20010315"" /><SignatureMethod Algorithm=""http://www.w3.org/2001/04/xmldsig-more#hmac-md5"" /><Reference URI=""""><Transforms><Transform Algorithm=""http://www.w3.org/2000/09/xmldsig#enveloped-signature"" /></Transforms><DigestMethod Algorithm=""MD5"" /><DigestValue>TH7ysbozJWVIWh/1K5bP1w==</DigestValue></Reference></SignedInfo><SignatureValue>tJ6m5YVu1jN1WgKWv3AXFQ==</SignatureValue></Signature></foo>";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            SignedXml sign = new SignedXml(doc);
            sign.LoadXml(doc.DocumentElement["Signature"]);

            // https://github.com/dotnet/runtime/issues/21236
            if (!PlatformDetection.IsNetFramework)
            {
                Assert.False(sign.CheckSignature(new HMACMD5(badKey)));
            }

            Assert.True(sign.CheckSignature(new HMACMD5(emptyHmacKey)));
        }

        // CVE-2009-0217
        // * a 0-length signature is the worse case - it accepts anything
        // * between 1-7 bits length are considered invalid (not a multiple of 8)
        // * a 8 bits signature would have one chance, out of 256, to be valid
        // * and so on... until we hit (output-length / 2) or 80 bits (see ERRATUM)

        static bool erratum = true; // xmldsig erratum for CVE-2009-0217

        static SignedXml GetSignedXml(string xml)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            SignedXml sign = new SignedXml(doc);
            sign.LoadXml(doc.DocumentElement);
            return sign;
        }

        static void CheckErratum(SignedXml signed, KeyedHashAlgorithm hmac, string message)
        {
            if (erratum)
            {
                Assert.Throws<CryptographicException>(() => signed.CheckSignature(hmac));
            }
            else
            {
                Assert.True(signed.CheckSignature(hmac), message);
            }
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/20429")]
        [Fact]
        public void HmacMustBeMultipleOfEightBits()
        {
            static void hmacMustBeMultipleOfEightBits(int bits)
            {
                string xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Signature xmlns=""http://www.w3.org/2000/09/xmldsig#"">
  <SignedInfo>
    <CanonicalizationMethod Algorithm=""http://www.w3.org/TR/2001/REC-xml-c14n-20010315"" />
    <SignatureMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#hmac-sha1"" >
      <HMACOutputLength>{0}</HMACOutputLength>
    </SignatureMethod>
    <Reference URI=""#object"">
      <DigestMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#sha1"" />
      <DigestValue>nz4GS0NbH2SrWlD/4fX313CoTzc=</DigestValue>
    </Reference>
  </SignedInfo>
  <SignatureValue>
    gA==
  </SignatureValue>
  <Object Id=""object"">some other text</Object>
</Signature>
";
                SignedXml sign = GetSignedXml(string.Format(xml, bits));
                // only multiple of 8 bits are supported
                sign.CheckSignature(new HMACSHA1(Encoding.ASCII.GetBytes("secret")));
            }

            for (int i = 1; i < 160; i++)
            {
                // The .NET framework only supports multiple of 8 bits
                if (i % 8 != 0)
                {
                    Assert.Throws<CryptographicException>(() => hmacMustBeMultipleOfEightBits(i));
                }
            }
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/20429")]
        [Fact]
        public void VerifyHMAC_ZeroLength()
        {
            string xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Signature xmlns=""http://www.w3.org/2000/09/xmldsig#"">
  <SignedInfo>
    <CanonicalizationMethod Algorithm=""http://www.w3.org/TR/2001/REC-xml-c14n-20010315"" />
    <SignatureMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#hmac-sha1"" >
      <HMACOutputLength>0</HMACOutputLength>
    </SignatureMethod>
    <Reference URI=""#object"">
      <DigestMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#sha1"" />
      <DigestValue>nz4GS0NbH2SrWlD/4fX313CoTzc=</DigestValue>
    </Reference>
  </SignedInfo>
  <SignatureValue>
  </SignatureValue>
  <Object Id=""object"">some other text</Object>
</Signature>
";
            SignedXml sign = GetSignedXml(xml);

            CheckErratum(sign, new HMACSHA1(Encoding.ASCII.GetBytes("no clue")), "1");
            CheckErratum(sign, new HMACSHA1(Encoding.ASCII.GetBytes("")), "2");
            CheckErratum(sign, new HMACSHA1(Encoding.ASCII.GetBytes("oops")), "3");
            CheckErratum(sign, new HMACSHA1(Encoding.ASCII.GetBytes("secret")), "4");
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/20429")]
        [Fact]
        public void VerifyHMAC_SmallerThanMinimumLength()
        {
            // 72 is a multiple of 8 but smaller than the minimum of 80 bits
            string xml = @"<Signature xmlns=""http://www.w3.org/2000/09/xmldsig#""><SignedInfo><CanonicalizationMethod Algorithm=""http://www.w3.org/TR/2001/REC-xml-c14n-20010315"" /><SignatureMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#hmac-sha1""><HMACOutputLength>72</HMACOutputLength></SignatureMethod><Reference URI=""#object""><DigestMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#sha1"" /><DigestValue>nz4GS0NbH2SrWlD/4fX313CoTzc=</DigestValue></Reference></SignedInfo><SignatureValue>2dimB+P5Aw5K</SignatureValue><Object Id=""object"">some other text</Object></Signature>";
            SignedXml sign = GetSignedXml(xml);
            CheckErratum(sign, new HMACSHA1(Encoding.ASCII.GetBytes("secret")), "72");
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/20429")]
        [Fact]
        public void VerifyHMAC_MinimumLength()
        {
            // 80 bits is the minimum (and the half-size of HMACSHA1)
            string xml = @"<Signature xmlns=""http://www.w3.org/2000/09/xmldsig#""><SignedInfo><CanonicalizationMethod Algorithm=""http://www.w3.org/TR/2001/REC-xml-c14n-20010315"" /><SignatureMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#hmac-sha1""><HMACOutputLength>80</HMACOutputLength></SignatureMethod><Reference URI=""#object""><DigestMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#sha1"" /><DigestValue>nz4GS0NbH2SrWlD/4fX313CoTzc=</DigestValue></Reference></SignedInfo><SignatureValue>jVQPtLj61zNYjw==</SignatureValue><Object Id=""object"">some other text</Object></Signature>";
            SignedXml sign = GetSignedXml(xml);
            Assert.True(sign.CheckSignature(new HMACSHA1(Encoding.ASCII.GetBytes("secret"))));
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/20429")]
        [Fact]
        public void VerifyHMAC_SmallerHalfLength()
        {
            // 80bits is smaller than the half-size of HMACSHA256
            string xml = @"<Signature xmlns=""http://www.w3.org/2000/09/xmldsig#""><SignedInfo><CanonicalizationMethod Algorithm=""http://www.w3.org/TR/2001/REC-xml-c14n-20010315"" /><SignatureMethod Algorithm=""http://www.w3.org/2001/04/xmldsig-more#hmac-sha256""><HMACOutputLength>80</HMACOutputLength></SignatureMethod><Reference URI=""#object""><DigestMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#sha1"" /><DigestValue>nz4GS0NbH2SrWlD/4fX313CoTzc=</DigestValue></Reference></SignedInfo><SignatureValue>vPtw7zKVV/JwQg==</SignatureValue><Object Id=""object"">some other text</Object></Signature>";
            SignedXml sign = GetSignedXml(xml);
            CheckErratum(sign, new HMACSHA256(Encoding.ASCII.GetBytes("secret")), "80");
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/20429")]
        [Fact]
        public void VerifyHMAC_HalfLength()
        {
            // 128 is the half-size of HMACSHA256
            string xml = @"<Signature xmlns=""http://www.w3.org/2000/09/xmldsig#""><SignedInfo><CanonicalizationMethod Algorithm=""http://www.w3.org/TR/2001/REC-xml-c14n-20010315"" /><SignatureMethod Algorithm=""http://www.w3.org/2001/04/xmldsig-more#hmac-sha256""><HMACOutputLength>128</HMACOutputLength></SignatureMethod><Reference URI=""#object""><DigestMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#sha1"" /><DigestValue>nz4GS0NbH2SrWlD/4fX313CoTzc=</DigestValue></Reference></SignedInfo><SignatureValue>aegpvkAwOL8gN/CjSnW6qw==</SignatureValue><Object Id=""object"">some other text</Object></Signature>";
            SignedXml sign = GetSignedXml(xml);
            Assert.True(sign.CheckSignature(new HMACSHA256(Encoding.ASCII.GetBytes("secret"))));
        }

        [Fact]
        public void VerifyHMAC_FullLength()
        {
            string xml = @"<Signature xmlns=""http://www.w3.org/2000/09/xmldsig#""><SignedInfo><CanonicalizationMethod Algorithm=""http://www.w3.org/TR/2001/REC-xml-c14n-20010315"" /><SignatureMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#hmac-sha1"" /><Reference URI=""#object""><DigestMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#sha1"" /><DigestValue>7/XTsHaBSOnJ/jXD5v0zL6VKYsk=</DigestValue></Reference></SignedInfo><SignatureValue>a0goL9esBUKPqtFYgpp2KST4huk=</SignatureValue><Object Id=""object"">some text</Object></Signature>";
            SignedXml sign = GetSignedXml(xml);
            Assert.True(sign.CheckSignature(new HMACSHA1(Encoding.ASCII.GetBytes("secret"))));
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/20429")]
        [Fact]
        public void VerifyHMAC_HMACOutputLength_Signature_Mismatch()
        {
            string xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Signature xmlns=""http://www.w3.org/2000/09/xmldsig#"">
  <SignedInfo>
    <CanonicalizationMethod Algorithm=""http://www.w3.org/TR/2001/REC-xml-c14n-20010315"" />
    <SignatureMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#hmac-sha1"" >
      <HMACOutputLength>80</HMACOutputLength>
    </SignatureMethod>
    <Reference URI=""#object"">
      <DigestMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#sha1"" />
      <DigestValue>nz4GS0NbH2SrWlD/4fX313CoTzc=</DigestValue>
    </Reference>
  </SignedInfo>
  <SignatureValue>
  </SignatureValue>
  <Object Id=""object"">some other text</Object>
</Signature>
";
            SignedXml sign = GetSignedXml(xml);
            Assert.Throws<CryptographicException>(() => sign.CheckSignature(new HMACSHA1(Encoding.ASCII.GetBytes("no clue"))));
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/20429")]
        [Fact]
        public void VerifyHMAC_HMACOutputLength_Invalid()
        {
            string xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Signature xmlns=""http://www.w3.org/2000/09/xmldsig#"">
  <SignedInfo>
    <CanonicalizationMethod Algorithm=""http://www.w3.org/TR/2001/REC-xml-c14n-20010315"" />
    <SignatureMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#hmac-sha1"" >
      <HMACOutputLength>I wish this was not a string property</HMACOutputLength>
    </SignatureMethod>
    <Reference URI=""#object"">
      <DigestMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#sha1"" />
      <DigestValue>nz4GS0NbH2SrWlD/4fX313CoTzc=</DigestValue>
    </Reference>
  </SignedInfo>
  <SignatureValue>
  </SignatureValue>
  <Object Id=""object"">some other text</Object>
</Signature>
";
            SignedXml sign = GetSignedXml(xml);
            Assert.Throws<FormatException>(() => sign.CheckSignature(new HMACSHA1(Encoding.ASCII.GetBytes("no clue"))));
        }

        [Fact]
        public void CoreFxSignedXmlUsesSha256ByDefault()
        {
            const string expectedSignatureMethod = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";
            const string expectedDigestMethod = "http://www.w3.org/2001/04/xmlenc#sha256";
            ValidateSignedXmlDefaultHashAlgorithms(expectedSignatureMethod, expectedDigestMethod);
        }

        private void ValidateSignedXmlDefaultHashAlgorithms(string expectedSignatureMethod, string expectedDigestMethod)
        {
            const string xml = @"<?xml version=""1.0""?>
<example>
<test>some text node</test>
</example>";

            var doc = new XmlDocument();
            doc.PreserveWhitespace = true;
            doc.LoadXml(xml);

            using (RSA key = RSA.Create())
            {
                var sxml = new SignedXml(doc)
                {
                    SigningKey = key
                };

                Assert.Null(sxml.SignedInfo.SignatureMethod);

                var reference = new Reference();
                Assert.Equal(expectedDigestMethod, reference.DigestMethod);

                reference.Uri = "";
                reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
                sxml.AddReference(reference);
                sxml.ComputeSignature();

                XmlElement dsig = sxml.GetXml();
                XPathNavigator xp = dsig.CreateNavigator();

                XmlNamespaceManager nsMgr = new XmlNamespaceManager(xp.NameTable);
                nsMgr.AddNamespace("ds", "http://www.w3.org/2000/09/xmldsig#");

                Assert.Equal(expectedSignatureMethod,
                    xp.SelectSingleNode("/ds:SignedInfo/ds:SignatureMethod/@Algorithm", nsMgr)?.Value);

                Assert.Equal(expectedDigestMethod,
                    xp.SelectSingleNode("/ds:SignedInfo/ds:Reference/ds:DigestMethod/@Algorithm", nsMgr)?.Value);
            }
        }

        // To reduce running time, the test data is a pre-calculated string. For anyone that want to
        // make adjustments to it, this is the small program that was used to generate the data.

        //void Main()
        //{
        //    var xml = "<root><x ID=\"a\"/><x ID=\"b\"><x ID=\"c\"><x ID=\"y\"/></x></x></root>";

        //    var xd = new XmlDocument();
        //    xd.LoadXml(xml);

        //    Sign(xd, "c", "y");
        //    Sign(xd, "b", "b");
        //    Sign(xd, "a", "a");
        //    Sign(xd, "", "");

        //    Console.WriteLine(xd);
        //}

        //void Sign(XmlDocument xd, string id, string signaturePlacement)
        //{
        //    var sx = new SignedXml(xd);
        //    var key = RSA.Create();
        //    sx.SigningKey = key;

        //    if (!string.IsNullOrEmpty(id)) id = "#" + id;
        //    var reference = new Reference(id);
        //    reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
        //    reference.AddTransform(new XmlDsigExcC14NTransform());
        //    sx.AddReference(reference);

        //    sx.ComputeSignature();
        //    sx.KeyInfo.AddClause(new RSAKeyValue(key));

        //    var node = string.IsNullOrEmpty(signaturePlacement) ? xd.DocumentElement :
        //        xd.SelectSingleNode("//x[@ID=\'" + signaturePlacement + "']");

        //    var signatureElement = sx.GetXml();

        //    node.AppendChild(sx.GetXml());
        //}

        // Note that signatures were created/added in an order so that all should validate.
        private const string multipleSignaturesXml =
            @"<root>
               <x ID=""a"">
                  <Signature xmlns=""http://www.w3.org/2000/09/xmldsig#"">
                     <SignedInfo>
                        <CanonicalizationMethod Algorithm=""http://www.w3.org/TR/2001/REC-xml-c14n-20010315"" />
                        <SignatureMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#rsa-sha1"" />
                        <Reference URI=""#a"">
                           <Transforms>
                              <Transform Algorithm=""http://www.w3.org/2000/09/xmldsig#enveloped-signature"" />
                              <Transform Algorithm=""http://www.w3.org/2001/10/xml-exc-c14n#"" />
                           </Transforms>
                           <DigestMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#sha1"" />
                           <DigestValue>2US57VqBEH0lqyIYKTxbq2deDTA=</DigestValue>
                        </Reference>
                     </SignedInfo>
                     <SignatureValue>VMdecll8TJ89oLmTOpkA12NgzZeO5AZCei+7649C9tB4ca8kJd0J3VAcRKtYeu+5A1oGmgDRS1icGf3TxuRKqHH2kTwGLZbmIEKf75n7lpz1ReBYqFMc/DW45x42MlerhGkPEdnO7Ucwykdd38gSqZJcfYENtwq7xUcTZIedKi4=</SignatureValue>
                     <KeyInfo>
                        <KeyValue>
                           <RSAKeyValue>
                              <Modulus>muFUHuh9LsUbbz8awq3p/RPlltaZFV0DxhofwqBS5zWhqJ/I5/0F2UVi+8XXQ37TFkBh5wpm/HwJC+Uh9t17l7CpdgiasGiN9G1i1gaSwaNsj2SnwCmBl/AICuFVp6i/UC+v77dXaBhTnH0lhD2a/+fbUomJAxSyQhqfpH3SLgk=</Modulus>
                              <Exponent>AQAB</Exponent>
                           </RSAKeyValue>
                        </KeyValue>
                     </KeyInfo>
                  </Signature>
               </x>
               <x ID=""b"">
                  <x ID=""c"">
                     <x ID=""y"">
                        <Signature xmlns=""http://www.w3.org/2000/09/xmldsig#"">
                           <SignedInfo>
                              <CanonicalizationMethod Algorithm=""http://www.w3.org/TR/2001/REC-xml-c14n-20010315"" />
                              <SignatureMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#rsa-sha1"" />
                              <Reference URI=""#c"">
                                 <Transforms>
                                    <Transform Algorithm=""http://www.w3.org/2000/09/xmldsig#enveloped-signature"" />
                                    <Transform Algorithm=""http://www.w3.org/2001/10/xml-exc-c14n#"" />
                                 </Transforms>
                                 <DigestMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#sha1"" />
                                 <DigestValue>NGqdYOU+AMF8pwX09mfN7GfG9lA=</DigestValue>
                              </Reference>
                           </SignedInfo>
                           <SignatureValue>YypMN7Cu6cDdMilxDV78dgTUtyNjY1iZn4rtzGQzCTPBFJHGNr75oZMg9vRX9nnpnNc3xHWbJyxTZ8uuXfVvPSjCTVjYeuMpe+11lz3qkQCmw+B9nypQTgXWz3zNrN0wNSTm1TzowWrte0vaJSWA9bgOFvmn9YG2GEfS69DSzOY=</SignatureValue>
                           <KeyInfo>
                              <KeyValue>
                                 <RSAKeyValue>
                                    <Modulus>xhqQCNyUSmaKKAhR+YhZjmrtK/vaG+S4AUwotC7u2B5f4e9OIye7PcN74k1G4K0cY5hzqeZUTKHCRBuxgDPT6IifA4MIeiKfyql20GlLNkEO/xAR9wrFgIBRWk9sgU7Nfhe8+W/AjY9+RlPPZXBdOVyAacse4KY4XY5z2GgYQHU=</Modulus>
                                    <Exponent>AQAB</Exponent>
                                 </RSAKeyValue>
                              </KeyValue>
                           </KeyInfo>
                        </Signature>
                     </x>
                  </x>
                  <Signature xmlns=""http://www.w3.org/2000/09/xmldsig#"">
                     <SignedInfo>
                        <CanonicalizationMethod Algorithm=""http://www.w3.org/TR/2001/REC-xml-c14n-20010315"" />
                        <SignatureMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#rsa-sha1"" />
                        <Reference URI=""#b"">
                           <Transforms>
                              <Transform Algorithm=""http://www.w3.org/2000/09/xmldsig#enveloped-signature"" />
                              <Transform Algorithm=""http://www.w3.org/2001/10/xml-exc-c14n#"" />
                           </Transforms>
                           <DigestMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#sha1"" />
                           <DigestValue>A9fDiGJkjH9TuFkydEBjIJMkMzU=</DigestValue>
                        </Reference>
                     </SignedInfo>
                     <SignatureValue>bdZtoQ2jqEVB3ifJ+lpVkKhtJYw8/WXhua3+O3ubRueYmmHVE26hFrg5y+Hz/D/YFVXtzU2dc6YypOYBLsdcRdI1JDqBN7UevmFe1NsW5YaBj4whmm3bTswMcqL6dQCXur0iq4LljVB8mhQP6nl27IajnIV+VOmkNNha+qzJfuE=</SignatureValue>
                     <KeyInfo>
                        <KeyValue>
                           <RSAKeyValue>
                              <Modulus>y1a1hMeJ7nkLr5SS5lkRvFV5dnejBnu7hjiXKYdN/YZKbaxVpmuPguJVqqrrXE52RigIcM//EYvfYvV0rhIr6PN+mwm2m4ZuZjAvCrxvYU3G/ZGHtF5LYt0RaZVMOLG4xqFXlQpwlEXw39UN1PreZi6XEV7Jjszd/VLdtIykW5U=</Modulus>
                              <Exponent>AQAB</Exponent>
                           </RSAKeyValue>
                        </KeyValue>
                     </KeyInfo>
                  </Signature>
               </x>
               <Signature xmlns=""http://www.w3.org/2000/09/xmldsig#"">
                  <SignedInfo>
                     <CanonicalizationMethod Algorithm=""http://www.w3.org/TR/2001/REC-xml-c14n-20010315"" />
                     <SignatureMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#rsa-sha1"" />
                     <Reference URI="""">
                        <Transforms>
                           <Transform Algorithm=""http://www.w3.org/2000/09/xmldsig#enveloped-signature"" />
                           <Transform Algorithm=""http://www.w3.org/2001/10/xml-exc-c14n#"" />
                        </Transforms>
                        <DigestMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#sha1"" />
                        <DigestValue>knyXmtlEae1LcoEEx52tq9yYr50=</DigestValue>
                     </Reference>
                  </SignedInfo>
                  <SignatureValue>Bk4BUVLgQ47zZ1mUrnzeRZfQor0C9GhrtF03AXF5Z7Iq4KfpAgD+R9BJhR6+5Fodr5O2v1v1OhzYxL8aBgM1bdrtTZZI02JmaWSUv5/Af2ZybXCec2hKReJ94omO/8vaq3kBdovfT9G0WfXBzlD0URy+7WZBi+YJ5FPtx1vroLM=</SignatureValue>
                  <KeyInfo>
                     <KeyValue>
                        <RSAKeyValue>
                           <Modulus>sa9jTiNt9nsavccQO9gZjUKRF1qgKGOY4tojYjv+C7VLYBDfrpXDWdYcsBQV0DJCD+CH4IJ069lMTJBQ5sHoc1pHxCqywgoMpMTbHrXt0PHvz6P7Bd77KgNfbsCnV62g098r/y8n8APRdp1G5zZFPAltOah8kj485cp2BRpQTmE=</Modulus>
                           <Exponent>AQAB</Exponent>
                        </RSAKeyValue>
                     </KeyValue>
                  </KeyInfo>
               </Signature>
            </root>";

        private SignedXml CreateSubjectForMultipleEnvelopedSignatures(string xml, string signatureParent)
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);

            var subject = new SignedXml(doc);

            XmlNode parentNode = string.IsNullOrEmpty(signatureParent) ? doc.DocumentElement : doc.SelectSingleNode("//x[@ID='" + signatureParent + "']");
            XmlElement signatureElement = parentNode["Signature"];

            subject.LoadXml(signatureElement);

            return subject;
        }

        [Theory]
        [InlineData("a"/*, 1*/)]
        [InlineData("b"/*, 2*/)]
        [InlineData("y"/*, 1*/)]
        [InlineData(""/* , 4*/)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "SignedXml has been failing validation on nested signatures all the time with .NET Framework and .NET (Core) up to .NET 6. This test was added together with a fix for .NET 7.")]
        public void CheckSignatureMultipleEnvelopedSignatures(string signatureParent/*, int expectedPosition*/)
        {
            SignedXml subject = CreateSubjectForMultipleEnvelopedSignatures(multipleSignaturesXml, signatureParent);

            // When debugging this test, it might make sense to validate the actual signature
            // position rather than the external-visible behaviour. The test relies on private
            // reflection so I don't think it belongs in the normal test run. Uncomment these
            // lines  and the expectedPosition parameter to enable the validation

            //var transform = (XmlDsigEnvelopedSignatureTransform)((Reference)subject.Signature.SignedInfo.References[0]).TransformChain[0];
            //var signaturePositionField = typeof(XmlDsigEnvelopedSignatureTransform).GetField("_signaturePosition", Reflection.BindingFlags.NonPublic | Reflection.BindingFlags.Instance);
            //var actualPosition = (int)signaturePositionField.GetValue(transform);
            //Assert.Equal(expectedPosition, actualPosition);

            Assert.True(subject.CheckSignature(), "Multiple signatures, validating " + signatureParent);
        }

        [Theory]
        [InlineData("a", "a", false)]
        [InlineData("a", "b", true)]
        [InlineData("b", "b", false)]
        [InlineData("b", "c", false)]
        [InlineData("y", "b", true)]
        [InlineData("y", "c", false)]
        [InlineData("y", "y", false)]
        [InlineData("", "a", false)]
        [InlineData("", "b", false)]
        [InlineData("", "c", false)]
        [InlineData("", "y", false)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "SignedXml has been failing validation on nested signatures all the time with .NET Framework and .NET (Core) up to .NET 6. This test was added together with a fix for .NET 7.")]
        public void CheckSignatureDetectsTamperedDataOnMultipleEnvelopedSignatures(
            string signatureParent, string tamperNode, bool expected)
        {
            var tampered = multipleSignaturesXml.Replace($"ID=\"{tamperNode}\"", $"ID=\"{tamperNode}\" Hackerz=\"true\"");

            SignedXml subject = CreateSubjectForMultipleEnvelopedSignatures(tampered, signatureParent);

            Assert.Equal(expected, subject.CheckSignature());
        }

        [Theory]
        [InlineData("a", "b")]
        [InlineData("a", "nonexisting")]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "SignedXml has been failing validation on nested signatures all the time with .NET Framework and .NET (Core) up to .NET 6. This test was added together with a fix for .NET 7.")]
        public void CheckSignatureHandlesIncorrectOrTamperedReferenceWithMultipleEnvelopedSignatures(
            string signatureParent, string newReference)
        {
            var tampered = multipleSignaturesXml.Replace($"URI=\"#{signatureParent}", $"URI=\"#{newReference}");

            SignedXml subject = CreateSubjectForMultipleEnvelopedSignatures(tampered, signatureParent);

            Assert.False(subject.CheckSignature());
        }
    }
}
