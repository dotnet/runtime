// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information
//
// Unit tests for XmlDecryptionTransform
//
// Author:
//	Sebastien Pouliot  <sebastien@ximian.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
//
// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Xml;
using Xunit;

namespace System.Security.Cryptography.Xml.Tests
{

    public class UnprotectedXmlDecryptionTransform : XmlDecryptionTransform
    {
        public bool UnprotectedIsTargetElement(XmlElement inputElement, string idValue)
        {
            return base.IsTargetElement(inputElement, idValue);
        }

        public XmlNodeList UnprotectedGetInnerXml()
        {
            return base.GetInnerXml();
        }
    }

    public class XmlDecryptionTransformTest
    {
        private UnprotectedXmlDecryptionTransform transform;

        public XmlDecryptionTransformTest()
        {
            transform = new UnprotectedXmlDecryptionTransform();
        }

        [Fact]
        public void IsTargetElement_XmlElementNull()
        {
            Assert.False(transform.UnprotectedIsTargetElement(null, "value"));
        }

        [Fact]
        public void IsTargetElement_StringNull()
        {
            XmlDocument doc = new XmlDocument();
            Assert.False(transform.UnprotectedIsTargetElement(doc.DocumentElement, null));
        }

        [Theory]
        [InlineData("<a id=\"1\" />", "1", true)]
        [InlineData("<a ID=\"1\" />", "1", true)]
        [InlineData("<a Id=\"1\" />", "1", true)]
        [InlineData("<a iD=\"1\" />", "1", false)]
        [InlineData("<a id=\"1\" />", "2", false)]
        public void IsTargetElement_ValidXml(string xml, string id, bool expectedResult)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            Assert.Equal(expectedResult, transform.UnprotectedIsTargetElement(doc.DocumentElement, id));
        }

        [Fact]
        public void AddExceptUri_Null()
        {
            Assert.Throws<ArgumentNullException>(() => transform.AddExceptUri(null));
        }

        [Fact]
        public void EncryptedXml_NotNull()
        {
            Assert.NotNull(transform.EncryptedXml);
        }

        [Fact]
        public void InputTypes()
        {
            Type[] inputTypes = transform.InputTypes;

            Assert.Equal(2, inputTypes.Length);
            Assert.Contains(typeof(Stream), inputTypes);
            Assert.Contains(typeof(XmlDocument), inputTypes);
        }

        [Fact]
        public void OutputTypes()
        {
            Type[] outputTypes = transform.OutputTypes;

            Assert.Equal(1, outputTypes.Length);
            Assert.Contains(typeof(XmlDocument), outputTypes);
        }

        [Fact]
        public void LoadInnerXml_XmlNull()
        {
            Assert.Throws<CryptographicException>(() => transform.LoadInnerXml(null));
        }

        [Fact]
        public void LoadInnerXml_XmlNoExcept()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<a />");

            Assert.Throws<CryptographicException>(() => transform.LoadInnerXml(doc.ChildNodes));

            Assert.Null(transform.UnprotectedGetInnerXml());
        }

        [Fact]
        public void LoadInnerXml_XmlNoUriForExcept()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(@"<dcrpt:Except xmlns:dcrpt=""http://www.w3.org/2002/07/decrypt#""/>");

            Assert.Throws<CryptographicException>(() => transform.LoadInnerXml(doc.ChildNodes));
        }

        [Fact]
        public void LoadInnerXml_XmlValidUriForExcept()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(@"<dcrpt:Except URI=""#item1"" xmlns:dcrpt=""http://www.w3.org/2002/07/decrypt#""/>");

            transform.LoadInnerXml(doc.ChildNodes);

            Assert.NotNull(transform.UnprotectedGetInnerXml());
        }

        [Fact]
        public void LoadStreamInput_CorrectXml()
        {
            XmlDocument doc = new XmlDocument();
            string xml = "<root><a /><b /></root>";
            doc.LoadXml(xml);

            using (MemoryStream memoryStream = new MemoryStream())
            using (StreamWriter streamWriter = new StreamWriter(memoryStream, Text.Encoding.Unicode))
            {
                streamWriter.Write(xml);
                streamWriter.Flush();
                memoryStream.Position = 0;

                transform.LoadInput(memoryStream);
                XmlDocument output = (XmlDocument)transform.GetOutput();

                Assert.Equal(xml, output.OuterXml);
            }
        }

        [Fact]
        public void GetOutput_WrongType()
        {
            XmlDocument doc = new XmlDocument();
            string xml = "<test />";
            doc.LoadXml(xml);

            transform.LoadInput(doc);
            AssertExtensions.Throws<ArgumentException>("type", () => transform.GetOutput(typeof(string)));
        }

        [Fact]
        public void GetOutput_XmlNoEncryptedData()
        {
            XmlDocument doc = new XmlDocument();
            string xml = "<test />";
            doc.LoadXml(xml);

            transform.LoadInput(doc);
            XmlDocument transformedDocument = (XmlDocument)transform.GetOutput(typeof(XmlDocument));

            Assert.Equal(xml, transformedDocument.OuterXml);
        }

        [Fact]

        public void GetOutput_XmlWithEncryptedData()
        {
            XmlDocument doc = new XmlDocument();
            string xml = "<root><a /><b /><c>To Be Encrypted</c><d /></root>";
            doc.LoadXml(xml);

            XmlDocument transformedDocument = GetTransformedOutput(doc, "c");

            Assert.NotNull(transformedDocument);
            Assert.Equal(xml, transformedDocument.OuterXml);
        }

        [Fact]
        public void GetOutput_XmlWithEncryptedDataInRoot()
        {
            XmlDocument doc = new XmlDocument();
            string xml = "<root>To Be Encrypted</root>";
            doc.LoadXml(xml);

            XmlDocument transformedDocument = GetTransformedOutput(doc, "//root");

            Assert.NotNull(transformedDocument);
            Assert.Equal(xml, transformedDocument.OuterXml);
        }

        [Fact]
        public void GetOutput_XmlWithEncryptedDataAndExcept()
        {
            XmlDocument doc = new XmlDocument();
            string xml = "<root><a /><b /><c>To Be Encrypted</c><d /></root>";
            doc.LoadXml(xml);

            transform.AddExceptUri("#_notfound");
            transform.AddExceptUri("#_0");
            XmlDocument transformedDocument = GetTransformedOutput(doc, "c");

            XmlNamespaceManager xmlNamespaceManager = new XmlNamespaceManager(doc.NameTable);
            xmlNamespaceManager.AddNamespace("enc", EncryptedXml.XmlEncNamespaceUrl);
            Assert.NotNull(transformedDocument.DocumentElement.SelectSingleNode("//enc:EncryptedData", xmlNamespaceManager));
            Assert.NotEqual(xml, transformedDocument.OuterXml);
        }

        private XmlDocument GetTransformedOutput(XmlDocument doc, string nodeToEncrypt)
        {
            using (var aesAlgo = Aes.Create())
            {
                var encryptedXml = new EncryptedXml();
                encryptedXml.AddKeyNameMapping("aes", aesAlgo);
                XmlElement elementToEncrypt = (XmlElement)doc.DocumentElement.SelectSingleNode(nodeToEncrypt);
                EncryptedData encryptedData = encryptedXml.Encrypt(elementToEncrypt, "aes");
                EncryptedXml.ReplaceElement(elementToEncrypt, encryptedData, false);

                XmlNamespaceManager xmlNamespaceManager = new XmlNamespaceManager(doc.NameTable);
                xmlNamespaceManager.AddNamespace("enc", EncryptedXml.XmlEncNamespaceUrl);
                XmlElement encryptedNode = (XmlElement)doc.DocumentElement.SelectSingleNode("//enc:EncryptedData", xmlNamespaceManager);
                encryptedNode.SetAttribute("ID", "#_0");

                transform.LoadInput(doc);
                transform.EncryptedXml = encryptedXml;
                XmlDocument transformedDocument = (XmlDocument)transform.GetOutput();

                transform.EncryptedXml = null;

                return transformedDocument;
            }
        }
    }
}

