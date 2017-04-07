// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml;
using Xunit;

namespace System.Security.Cryptography.Xml.Tests
{
    public class EncryptionPropertyTest
    {
        private const string ValidXml = @"<EncryptionProperty xmlns=""http://www.w3.org/2001/04/xmlenc#"" Id=""ID_val"" Target=""Target_val"" />";
        private const string IDValue = "ID_val";
        private const string TargetValue = "Target_val";

        [Fact]
        public void DefaultConstructor()
        {
            EncryptionProperty encryptionProperty = new EncryptionProperty();
            Assert.Null(encryptionProperty.Id);
            Assert.Null(encryptionProperty.Target);
            Assert.Null(encryptionProperty.PropertyElement);
        }

        [Fact]
        public void ConstructorWithXmlElement_ValidEncryptionPropertyXml()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(ValidXml);
            EncryptionProperty encryptionProperty = new EncryptionProperty(doc.DocumentElement);
            Assert.NotNull(encryptionProperty.PropertyElement);
            Assert.Equal(doc.DocumentElement, encryptionProperty.PropertyElement);
        }

        [Fact]
        public void ConstructorWithXmlElement_Element_Null()
        {
            Assert.Throws<ArgumentNullException>(() => new EncryptionProperty(null));
        }

        [Fact]
        public void ConstructorWithXmlElement_InvalidXml()
        {
            string xml = "<a />";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            Assert.Throws<CryptographicException>(() => new EncryptionProperty(doc.DocumentElement));
        }

        [Fact]
        public void PropertyElement_Set_Null()
        {
            EncryptionProperty encryptionProperty = new EncryptionProperty();
            Assert.Throws<ArgumentNullException>(() => encryptionProperty.PropertyElement = null);
        }

        [Fact]
        public void PropertyElement_Set_InvalidXml()
        {
            string xml = "<a />";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            EncryptionProperty encryptionProperty = new EncryptionProperty();
            Assert.Throws<CryptographicException>(() => encryptionProperty.PropertyElement = doc.DocumentElement);
        }

        [Fact]
        public void PropertyElement_Set_ValidEncryptionPropertyXml()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(ValidXml);
            EncryptionProperty encryptionProperty = new EncryptionProperty();
            encryptionProperty.PropertyElement = doc.DocumentElement;
            Assert.Equal(doc.DocumentElement, encryptionProperty.PropertyElement);
        }

        [Fact]
        public void LoadXml_Null()
        {
            EncryptionProperty encryptionProperty = new EncryptionProperty();
            Assert.Throws<ArgumentNullException>(() => encryptionProperty.LoadXml(null));
        }

        [Fact]
        public void LoadXml_InvalidXml()
        {
            string xml = "<a />";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            EncryptionProperty encryptionProperty = new EncryptionProperty();
            Assert.Throws<CryptographicException>(() => encryptionProperty.LoadXml(doc.DocumentElement));
        }

        [Fact]
        public void LoadXml_ValidEncryptionPropertyXml()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(ValidXml);
            EncryptionProperty encryptionProperty = new EncryptionProperty();
            encryptionProperty.LoadXml(doc.DocumentElement);
            Assert.Equal(doc.DocumentElement, encryptionProperty.PropertyElement);
            Assert.Equal(IDValue, encryptionProperty.Id);
            Assert.Equal(TargetValue, encryptionProperty.Target);
        }

        [Fact]
        public void GetXml()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(ValidXml);
            EncryptionProperty encryptionProperty = new EncryptionProperty();
            encryptionProperty.PropertyElement = doc.DocumentElement;

            XmlElement output = encryptionProperty.GetXml();
            Assert.Equal(ValidXml, output.OuterXml);
        }

        [Fact]
        public void GetXml_Cached()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(ValidXml);
            EncryptionProperty encryptionProperty = new EncryptionProperty();
            encryptionProperty.LoadXml(doc.DocumentElement);

            XmlElement output = encryptionProperty.GetXml();
            Assert.Equal(ValidXml, output.OuterXml);
        }
    }
}
