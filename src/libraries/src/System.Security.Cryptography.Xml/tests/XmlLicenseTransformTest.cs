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
        private UnprotectedXmlLicenseTransform transform;

        public XmlLicenseTransformTest()
        {
            transform = new UnprotectedXmlLicenseTransform();
        }

        [Fact] // ctor ()
        public void Constructor1()
        {
            Assert.Equal("urn:mpeg:mpeg21:2003:01-REL-R-NS:licenseTransform",
                transform.Algorithm);
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
            Assert.Throws<ArgumentException>(() => transform.GetOutput(typeof(string)));
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
    }
}

