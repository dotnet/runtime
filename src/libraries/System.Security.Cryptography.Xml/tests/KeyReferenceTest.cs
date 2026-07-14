// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml;
using Xunit;

namespace System.Security.Cryptography.Xml.Tests
{
    public class KeyReferenceTest
    {
        [Fact]
        public void Constructor_Default()
        {
            KeyReference keyRef = new KeyReference();
            Assert.NotNull(keyRef);
        }

        [Fact]
        public void Constructor_WithUri()
        {
            string uri = "#EncryptedKey1";
            KeyReference keyRef = new KeyReference(uri);
            Assert.Equal(uri, keyRef.Uri);
        }

        [Fact]
        public void Constructor_WithUriAndTransformChain()
        {
            string uri = "#EncryptedKey1";
            TransformChain tc = new TransformChain();
            tc.Add(new XmlDsigBase64Transform());
            
            KeyReference keyRef = new KeyReference(uri, tc);
            Assert.Equal(uri, keyRef.Uri);
            Assert.NotNull(keyRef.TransformChain);
        }

        [Fact]
        public void GetXml_ReturnsValidXml()
        {
            KeyReference keyRef = new KeyReference("#key1");
            XmlElement element = keyRef.GetXml();
            Assert.NotNull(element);
            Assert.Equal("KeyReference", element.LocalName);
        }

        [Fact]
        public void LoadXml_ValidXml()
        {
            string xml = @"<KeyReference URI=""#key1"" xmlns=""http://www.w3.org/2001/04/xmlenc#"" />";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            KeyReference keyRef = new KeyReference();
            keyRef.LoadXml(doc.DocumentElement);
            Assert.Equal("#key1", keyRef.Uri);
        }

        [Fact]
        public void LoadXml_WithTransforms()
        {
            string xml = @"<KeyReference URI=""#key1"" xmlns=""http://www.w3.org/2001/04/xmlenc#"">
                <Transforms>
                    <Transform Algorithm=""http://www.w3.org/2001/10/xml-exc-c14n#"" />
                </Transforms>
            </KeyReference>";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            KeyReference keyRef = new KeyReference();
            keyRef.LoadXml(doc.DocumentElement);
            Assert.Equal("#key1", keyRef.Uri);
            Assert.NotNull(keyRef.TransformChain);
            Assert.Equal(1, keyRef.TransformChain.Count);
        }

        [Fact]
        public void ReferenceType_IsKeyReference()
        {
            KeyReference keyRef = new KeyReference();
            XmlElement element = keyRef.GetXml();
            Assert.Equal("KeyReference", element.LocalName);
        }
    }
}
