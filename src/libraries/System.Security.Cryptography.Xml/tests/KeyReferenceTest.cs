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
    }
}
