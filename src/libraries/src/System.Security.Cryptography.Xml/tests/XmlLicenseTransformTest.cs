//
// XmlLicenseTransformTest.cs - Test Cases for XmlLicenseTransform
//
// Author:
//  original:
//	Sebastien Pouliot <sebastien@ximian.com>
//	Aleksey Sanin (aleksey@aleksey.com)
//  this file:
//	Gert Driesen <drieseng@users.sourceforge.net>
//
// (C) 2003 Aleksey Sanin (aleksey@aleksey.com)
// (C) 2004 Novell (http://www.novell.com)
// (C) 2008 Gert Driesen
//
// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Xml;
using Xunit;

namespace System.Security.Cryptography.Xml.Tests
{
    public class UnprotectedXmlLicenseTransform : XmlLicenseTransform
    {
        public XmlNodeList UnprotectedGetInnerXml()
        {
            return base.GetInnerXml();
        }
    }

    public class DummyDecryptor : IRelDecryptor
    {
        public string ContentToReturn { get; set; }

        public Stream Decrypt(EncryptionMethod encryptionMethod, KeyInfo keyInfo, Stream toDecrypt)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(ContentToReturn);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
    }

    public class XmlLicenseTransformTest
    {
        public const string LicenseTransformNsUrl = "urn:mpeg:mpeg21:2003:01-REL-R-NS";
        public const string LicenseTransformUrl = LicenseTransformNsUrl + ":licenseTransform";
        private UnprotectedXmlLicenseTransform transform;

        public XmlLicenseTransformTest()
        {
            transform = new UnprotectedXmlLicenseTransform();
        }

        [Fact] // ctor ()
        public void Constructor1()
        {
            Assert.Equal(LicenseTransformUrl, transform.Algorithm);
            Assert.Null(transform.Decryptor);

            Type[] input = transform.InputTypes;
            Assert.Equal(1, input.Length);
            Assert.Equal(typeof(XmlDocument), input[0]);

            Type[] output = transform.OutputTypes;
            Assert.Equal(1, output.Length);
            Assert.Equal(typeof(XmlDocument), output[0]);
        }

        [Fact]
        public void InputTypes()
        {
            // property does not return a clone
            transform.InputTypes[0] = null;
            Assert.Null(transform.InputTypes[0]);

            // it's not a static array
            transform = new UnprotectedXmlLicenseTransform();
            Assert.NotNull(transform.InputTypes[0]);
        }

        [Fact]
        public void GetInnerXml()
        {
            XmlNodeList xnl = transform.UnprotectedGetInnerXml();
            Assert.Null(xnl);
        }

        [Fact]
        public void OutputTypes()
        {
            // property does not return a clone
            transform.OutputTypes[0] = null;
            Assert.Null(transform.OutputTypes[0]);

            // it's not a static array
            transform = new UnprotectedXmlLicenseTransform();
            Assert.NotNull(transform.OutputTypes[0]);
        }

        [Fact]
        public void Context_Null()
        {
            XmlDocument doc = GetDocumentFromResource("System.Security.Cryptography.Xml.Tests.XmlLicenseSample.xml");

            Assert.Throws<CryptographicException>(() => transform.LoadInput(doc));
        }

        [Fact]
        public void NoLicenseXml()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<root />");
            transform.Context = doc.DocumentElement;

            Assert.Throws<CryptographicException>(() => transform.LoadInput(doc));
        }

        [Fact]
        public void Decryptor_Null()
        {
            XmlDocument doc = GetDocumentFromResource("System.Security.Cryptography.Xml.Tests.XmlLicenseSample.xml");

            XmlNamespaceManager namespaceManager = new XmlNamespaceManager(doc.NameTable);
            namespaceManager.AddNamespace("r", "urn:mpeg:mpeg21:2003:01-REL-R-NS");
            transform.Context = doc.DocumentElement.SelectSingleNode("//r:issuer[1]", namespaceManager) as XmlElement;

            Assert.Throws<CryptographicException>(() => transform.LoadInput(doc));
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "https://github.com/dotnet/corefx/issues/19410")]
        public void ValidLicense()
        {
            XmlDocument doc = GetDocumentFromResource("System.Security.Cryptography.Xml.Tests.XmlLicenseSample.xml");

            XmlNamespaceManager namespaceManager = new XmlNamespaceManager(doc.NameTable);
            namespaceManager.AddNamespace("r", "urn:mpeg:mpeg21:2003:01-REL-R-NS");
            transform.Context = doc.DocumentElement.SelectSingleNode("//r:issuer[1]", namespaceManager) as XmlElement;
            DummyDecryptor decryptor = new DummyDecryptor { ContentToReturn = "Encrypted Content" };
            transform.Decryptor = decryptor;
            transform.LoadInput(doc);
            XmlDocument output = transform.GetOutput(typeof(XmlDocument)) as XmlDocument;

            string decodedXml = @"<r:license xmlns:r=""urn:mpeg:mpeg21:2003:01-REL-R-NS"" licenseId=""{00000000-0000-0000-0000-123456789012}"">";
            decodedXml += "<r:title>Test License</r:title><r:grant>Encrypted Content</r:grant>";
            decodedXml += "<r:issuer><r:details><r:timeOfIssue>2017-01-71T00:00:00Z</r:timeOfIssue></r:details></r:issuer></r:license>";
            Assert.NotNull(output);
            Assert.Equal(decodedXml, output.OuterXml);
        }

        [Fact]
        public void GetOutput_InvalidType()
        {
            AssertExtensions.Throws<ArgumentException>("type", () => transform.GetOutput(typeof(string)));
        }
		
        [Fact]
        public static void ItDecryptsLicense()
        {
            using (var key = RSA.Create())
            {
                string expected;
                string encryptedLicenseWithGrants = GenerateLicenseXmlWithEncryptedGrants(key, out expected);

                Assert.Contains("hello", expected);
                Assert.DoesNotContain("hello", encryptedLicenseWithGrants);

                XmlNamespaceManager nsManager;
                XmlDocument toDecrypt = LoadXmlWithLicenseNs(encryptedLicenseWithGrants, out nsManager);

                var decryptor = new XmlLicenseEncryptedRef();
                var transform = new XmlLicenseTransform()
                {
                    Decryptor = decryptor,
                    Context = FindLicenseTransformContext(toDecrypt, nsManager)
                };

                decryptor.AddAsymmetricKey(key);

                // Context is the input for this transform, argument is always ignored
                transform.LoadInput(null);

                XmlDocument decryptedDoc = transform.GetOutput() as XmlDocument;
                Assert.NotNull(decryptedDoc);
                string decrypted = decryptedDoc.OuterXml;
                Assert.Equal(expected, decrypted);
            }
        }

        private XmlDocument GetDocumentFromResource(string resourceName)
        {
            XmlDocument doc = new XmlDocument();
            using (Stream stream = TestHelpers.LoadResourceStream(resourceName))
            using (StreamReader streamReader = new StreamReader(stream))
            {
                string originalXml = streamReader.ReadToEnd();
                doc.LoadXml(originalXml);
            }

            return doc;
        }

        private static string GenerateLicenseXmlWithEncryptedGrants(RSA key, out string plainTextLicense)
        {
            plainTextLicense = @"<r:license xmlns:r=""urn:mpeg:mpeg21:2003:01-REL-R-NS"">
    <r:title>Test License</r:title>
    <r:grant>
        <r:forAll varName=""licensor"" />
        <r:forAll varName=""property"" />
        <r:forAll varName=""p0"">
            <r:propertyPossessor>
                <r:propertyAbstract varRef=""property"" />
            </r:propertyPossessor>
        </r:forAll>
        <r:keyHolder varRef=""licensor"" />
        <r:issue />
        <r:grant>
            <r:principal varRef=""p0"" />
            <x:bar xmlns:x=""urn:foo"" />
            <r:digitalResource>
                <testItem>hello</testItem>
            </r:digitalResource>
            <renderer xmlns=""urn:mpeg:mpeg21:2003:01-REL-MX-NS"">
                <mx:wildcard xmlns:mx=""urn:mpeg:mpeg21:2003:01-REL-MX-NS"">
                    <r:anXmlExpression>some-xpath-expression</r:anXmlExpression>
                </mx:wildcard>
                <mx:wildcard xmlns:mx=""urn:mpeg:mpeg21:2003:01-REL-MX-NS"">
                    <r:anXmlExpression>some-other-xpath-expression</r:anXmlExpression>
                </mx:wildcard>
            </renderer>
        </r:grant>
        <validityIntervalFloating xmlns=""urn:mpeg:mpeg21:2003:01-REL-SX-NS"">
            <sx:duration xmlns:sx=""urn:mpeg:mpeg21:2003:01-REL-SX-NS"">P2D</sx:duration>
        </validityIntervalFloating>
    </r:grant>
    <r:grant>
        <r:possessProperty />
        <emailName xmlns=""urn:mpeg:mpeg21:2003:01-REL-SX-NS"">test@test</emailName>
    </r:grant>
    <r:issuer xmlns:r=""urn:mpeg:mpeg21:2003:01-REL-R-NS"">
        <r:details>
            <r:timeOfIssue>2099-11-11T11:11:11Z</r:timeOfIssue>
        </r:details>
    </r:issuer>
</r:license>".Replace("\r\n", "\n");

            XmlNamespaceManager nsManager;
            XmlDocument doc = LoadXmlWithLicenseNs(plainTextLicense, out nsManager);

            EncryptLicense(FindLicenseTransformContext(doc, nsManager), key);

            return doc.OuterXml;
        }

        private static XmlElement FindLicenseTransformContext(XmlDocument doc, XmlNamespaceManager nsManager)
        {
            XmlNodeList issuerList = doc.SelectNodes("//r:issuer", nsManager);
            return issuerList[0] as XmlElement;
        }

        private static XmlDocument LoadXmlWithLicenseNs(string xml, out XmlNamespaceManager nsManager)
        {
            XmlDocument doc = new XmlDocument();
            doc.PreserveWhitespace = true;
            nsManager = new XmlNamespaceManager(doc.NameTable);
            nsManager.AddNamespace("r", LicenseTransformNsUrl);
            doc.LoadXml(xml);
            return doc;
        }

        private static void EncryptGrant(XmlNode grant, RSA key, XmlNamespaceManager nsMgr)
        {
            using (var ms = new MemoryStream())
            using (var sw = new StreamWriter(ms))
            {
                sw.Write(grant.InnerXml);
                sw.Flush();
                ms.Position = 0;

                KeyInfo keyInfo;
                EncryptionMethod encryptionMethod;
                CipherData cipherData;
                XmlLicenseEncryptedRef.Encrypt(ms, key, out keyInfo, out encryptionMethod, out cipherData);
                grant.RemoveAll();
                XmlDocument doc = grant.OwnerDocument;
                XmlElement encryptedGrant = doc.CreateElement("encryptedGrant", LicenseTransformNsUrl);
                grant.AppendChild(encryptedGrant);

                encryptedGrant.AppendChild(doc.ImportNode(keyInfo.GetXml(), true));
                encryptedGrant.AppendChild(doc.ImportNode(encryptionMethod.GetXml(), true));
                encryptedGrant.AppendChild(doc.ImportNode(cipherData.GetXml(), true));
            }
        }

        private static void EncryptLicense(XmlElement context, RSA key)
        {
            XmlDocument doc = context.OwnerDocument;

            var nsMgr = new XmlNamespaceManager(doc.NameTable);
            nsMgr.AddNamespace("dsig", SignedXml.XmlDsigNamespaceUrl);
            nsMgr.AddNamespace("enc", EncryptedXml.XmlEncNamespaceUrl);
            nsMgr.AddNamespace("r", LicenseTransformNsUrl);

            XmlElement currentIssuerContext = context.SelectSingleNode("ancestor-or-self::r:issuer[1]", nsMgr) as XmlElement;
            Assert.NotEqual(currentIssuerContext, null);

            XmlElement signatureNode = currentIssuerContext.SelectSingleNode("descendant-or-self::dsig:Signature[1]", nsMgr) as XmlElement;
            if (signatureNode != null)
            {
                signatureNode.ParentNode.RemoveChild(signatureNode);
            }

            XmlElement currentLicenseContext = currentIssuerContext.SelectSingleNode("ancestor-or-self::r:license[1]", nsMgr) as XmlElement;
            Assert.NotEqual(currentLicenseContext, null);

            XmlNodeList issuerList = currentLicenseContext.SelectNodes("descendant-or-self::r:license[1]/r:issuer", nsMgr);
            for (int i = 0; i < issuerList.Count; i++)
            {
                XmlNode issuer = issuerList[i];
                if (issuer == currentIssuerContext)
                {
                    continue;
                }

                if (issuer.LocalName == "issuer"
                    && issuer.NamespaceURI == LicenseTransformNsUrl)
                {
                    issuer.ParentNode.RemoveChild(issuer);
                }
            }

            XmlNodeList encryptedGrantList = currentLicenseContext.SelectNodes("/r:license/r:grant", nsMgr);

            for (int i = 0; i < encryptedGrantList.Count; i++)
            {
                EncryptGrant(encryptedGrantList[i], key, nsMgr);
            }
        }
    }
}

