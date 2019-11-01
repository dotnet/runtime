// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Xunit;
using Xunit.Extensions;

namespace System.Security.Cryptography.Xml.Tests
{
    public class CipherDataTests
    {
        [Fact]
        public void Constructor_Empty()
        {
            CipherData cipherData = new CipherData();

            Assert.Null(cipherData.CipherReference);
            Assert.Null(cipherData.CipherValue);
            Assert.Throws<CryptographicException>(() => cipherData.GetXml());
        }

        [Fact]
        public void Constructor_CipherValue_Null()
        {
            Assert.Throws<ArgumentNullException>(() => new CipherData((byte[])null));
        }

        [Theory]
        [InlineData(new byte[0])]
        [InlineData(new byte[] { 1, 2, 3 })]
        public void Constructor_CipherValue(byte[] cipherValue)
        {
            CipherData cipherData = new CipherData(cipherValue);

            Assert.Equal(cipherValue, cipherData.CipherValue);
            Assert.Null(cipherData.CipherReference);

            XmlElement xmlElement = cipherData.GetXml();
            Assert.NotNull(xmlElement);
            Assert.Equal(
                $"<CipherData xmlns=\"http://www.w3.org/2001/04/xmlenc#\"><CipherValue>{Convert.ToBase64String(cipherValue)}</CipherValue></CipherData>",
                xmlElement.OuterXml);
        }

        [Fact]
        public void Constructor_CipherReference_Null()
        {
            Assert.Throws<ArgumentNullException>(() => new CipherData((CipherReference)null));
        }

        [Theory]
        [MemberData(nameof(Constructor_CipherReference_Source))]
        public void Constructor_CipherReference(CipherReference cipherReference)
        {
            CipherData cipherData = new CipherData(cipherReference);

            Assert.Null(cipherData.CipherValue);
            Assert.Equal(cipherReference, cipherData.CipherReference);

            XmlElement xmlElement = cipherData.GetXml();
            Assert.NotNull(xmlElement);
            if (cipherReference.Uri != string.Empty)
            {
                Assert.Equal(
                    $"<CipherData xmlns=\"http://www.w3.org/2001/04/xmlenc#\"><CipherReference URI=\"{cipherReference.Uri}\" /></CipherData>",
                    xmlElement.OuterXml);
            }
            else
            {
                Assert.Equal(
                    "<CipherData xmlns=\"http://www.w3.org/2001/04/xmlenc#\"><CipherReference /></CipherData>",
                    xmlElement.OuterXml);
            }
        }

        public static IEnumerable<object[]> Constructor_CipherReference_Source()
        {
            return new object[][]
            {
                new [] { new CipherReference() },
                new [] { new CipherReference("http://dummy.urionly.io") },
                new [] { new CipherReference("http://dummy.uri.transform.io", new TransformChain()) },
            };
        }

        [Fact]
        public void CipherReference_Null()
        {
            CipherData cipherData = new CipherData();
            Assert.Throws<ArgumentNullException>(() => cipherData.CipherReference = null);
        }

        [Fact]
        public void CipherReference_CipherValueSet()
        {
            CipherData cipherData = new CipherData(new byte[0]);
            Assert.Throws<CryptographicException>(() => cipherData.CipherReference = new CipherReference());
        }

        [Fact]
        public void CipherValue_Null()
        {
            CipherData cipherData = new CipherData(new CipherReference());
            Assert.Throws<ArgumentNullException>(() => cipherData.CipherValue = null);
        }

        [Fact]
        public void CipherValue_CipherReferenceSet()
        {
            CipherData cipherData = new CipherData(new CipherReference());
            Assert.Throws<CryptographicException>(() => cipherData.CipherValue = new byte[0]);
        }

        [Fact]
        public void LoadXml_Null()
        {
            CipherData cipherData = new CipherData();
            Assert.Throws<ArgumentNullException>(() => cipherData.LoadXml(null));
        }

        [Fact]
        public void LoadXml_NoValueOrReference()
        {
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root/>");

            CipherData cipherData = new CipherData();

            Assert.Throws<CryptographicException>(() => cipherData.LoadXml(xmlDocument.DocumentElement));
        }

        [Theory]
        [MemberData(nameof(LoadXml_CipherValue_Source))]
        public void LoadXml_CipherValue(XmlElement xmlElement, byte[] cipherValue)
        {
            CipherData cipherData = new CipherData();
            cipherData.LoadXml(xmlElement);

            Assert.Equal(cipherValue, cipherData.CipherValue);
            Assert.Null(cipherData.CipherReference);

            XmlElement gotXmlElement = cipherData.GetXml();
            Assert.NotNull(gotXmlElement);
            Assert.Equal(xmlElement.OuterXml, gotXmlElement.OuterXml);
        }

        public static IEnumerable<object[]> LoadXml_CipherValue_Source()
        {
            return new[]
            {
                ToCipherDataTestCase("<root xmlns:enc='{0}'><enc:CipherValue>{1}</enc:CipherValue></root>", new byte[0]),
                ToCipherDataTestCase("<root xmlns:enc='{0}'><enc:CipherValue>{1}</enc:CipherValue></root>", new byte[] { 5, 6, 7 }),
                ToCipherDataTestCase("<root xmlns='{0}'><CipherValue>{1}</CipherValue></root>", new byte[0]),
            };
        }

        public static object[] ToCipherDataTestCase(string xml, byte[] cipherData)
        {
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(string.Format(xml, EncryptedXml.XmlEncNamespaceUrl, Convert.ToBase64String(cipherData)));
            return new object[] { xmlDocument.DocumentElement, cipherData };
        }

        [Theory]
        [MemberData(nameof(LoadXml_CipherReference_Source))]
        public void LoadXml_CipherReference(XmlElement xmlElement, string uri)
        {
            CipherData cipherData = new CipherData();
            cipherData.LoadXml(xmlElement);

            Assert.Equal(uri, cipherData.CipherReference.Uri);
            Assert.Null(cipherData.CipherValue);

            XmlElement gotXmlElement = cipherData.GetXml();
            Assert.NotNull(gotXmlElement);
            Assert.Equal(xmlElement.OuterXml, gotXmlElement.OuterXml);
        }

        public static IEnumerable<object[]> LoadXml_CipherReference_Source()
        {
            return new[]
            {
                ToCipherReferenceXmlElement("<root xmlns:enc='{0}'><enc:CipherReference URI=\"{1}\" /></root>", "http://dummy.io"),
                ToCipherReferenceXmlElement("<root xmlns:enc='{0}'><enc:CipherReference URI=\"{1}\" /></root>", "https://encrypted.dummy.io"),
                ToCipherReferenceXmlElement("<root xmlns='{0}'><CipherReference URI=\"{1}\" /></root>", "ftp://wtf.org"),
            };
        }

        public static object[] ToCipherReferenceXmlElement(string xml, string uri)
        {
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(string.Format(xml, EncryptedXml.XmlEncNamespaceUrl, uri));
            return new object[] { xmlDocument.DocumentElement, uri };
        }
    }
}
