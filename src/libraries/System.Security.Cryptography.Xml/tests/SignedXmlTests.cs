// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml;
using Xunit;

namespace System.Security.Cryptography.Xml.Tests
{
    public class SignedXmlTests
    {
        [Fact]
        public void Constructor_Document_Null()
        {
            Assert.Throws<ArgumentNullException>(
                () => new SignedXml((XmlDocument) null)
            );
        }

        [Fact]
        public void Constructor_XmlElement_Null()
        {
            Assert.Throws<ArgumentNullException>(
                () => new SignedXml((XmlElement) null)
            );
        }

        [Fact]
        public void Constructor_NoArgs()
        {
            SignedXml signedXml = new SignedXml();

            // TODO: Expand this
            Assert.NotNull(signedXml.EncryptedXml);

            Assert.Equal(0, signedXml.KeyInfo.Count);
            Assert.Equal(null, signedXml.KeyInfo.Id);

            // TODO: Expand
            Assert.NotNull(signedXml.Signature);
            Assert.NotNull(signedXml.Signature.SignedInfo);

            Assert.Equal(signedXml.SafeCanonicalizationMethods,
                new []
                {
                    SignedXml.XmlDsigC14NTransformUrl,
                    SignedXml.XmlDsigC14NWithCommentsTransformUrl,
                    SignedXml.XmlDsigExcC14NTransformUrl,
                    SignedXml.XmlDsigExcC14NWithCommentsTransformUrl
                });
            Assert.NotNull(signedXml.SignatureFormatValidator);

            Assert.Null(signedXml.SignatureLength);
            Assert.Null(signedXml.SignatureMethod);
            Assert.Null(signedXml.SignatureValue);
        }
    }
}
