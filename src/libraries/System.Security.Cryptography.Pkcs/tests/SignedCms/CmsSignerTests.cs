// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Security.Cryptography.SLHDsa.Tests;
using Xunit;

namespace System.Security.Cryptography.Pkcs.Tests
{
    public static partial class CmsSignerTests
    {
        [Theory]
        [InlineData((SubjectIdentifierType)0)]
        [InlineData((SubjectIdentifierType)4)]
        [InlineData((SubjectIdentifierType)(-1))]
        public static void SignerIdentifierType_InvalidValues(SubjectIdentifierType invalidType)
        {
            CmsSigner signer = new CmsSigner();

            AssertExtensions.Throws<ArgumentException>(
                expectedParamName: null,
                () => signer.SignerIdentifierType = invalidType);
        }

#if NET
        [Fact]
        public static void SignaturePadding_InvalidValue()
        {
            RSASignaturePaddingMode badMode = (RSASignaturePaddingMode)(-1);

            // Currently we support all RSASignaturePaddings. However we want to make sure we fail properly
            // if an unsupported one is added later, so construct a bogus padding.
            RSASignaturePadding badPadding = (RSASignaturePadding)typeof(RSASignaturePadding)
                .GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, new Type[] { typeof(RSASignaturePaddingMode)})
                .Invoke(new object[] { badMode });

            // Test setter
            CmsSigner signer = new CmsSigner();
            AssertExtensions.Throws<ArgumentException>("value", () => signer.SignaturePadding = badPadding);

            // Test ctor
            AssertExtensions.Throws<ArgumentException>("signaturePadding", () => new CmsSigner(
                SubjectIdentifierType.IssuerAndSerialNumber,
                certificate: null,
                privateKey: null,
                badPadding));
        }

        [Fact]
        public static void SignaturePadding_Null()
        {
            CmsSigner signer = new CmsSigner();
            signer.SignaturePadding = null; // Assert.NoThrow

            _ = new CmsSigner(
                SubjectIdentifierType.IssuerAndSerialNumber,
                certificate: null,
                privateKey: null,
                signaturePadding: null); // Assert.NoThrow
        }

        [Fact]
        public static void HasPrivateKey_IsCorrect()
        {
            CmsSigner signer = new CmsSigner();
            AssertExtensions.FalseExpression(signer.HasPrivateKey);

            using (RSA rsa = RSA.Create())
            {
                // Create signer with RSA key
                signer = new CmsSigner(SubjectIdentifierType.IssuerAndSerialNumber, null, rsa);
                AssertExtensions.TrueExpression(signer.HasPrivateKey);
                Assert.NotNull(signer.PrivateKey);
                Assert.Equal(rsa.SignatureAlgorithm, signer.PrivateKey.SignatureAlgorithm);

                signer.PrivateKey = null;
                AssertExtensions.FalseExpression(signer.HasPrivateKey);

                if (SlhDsa.IsSupported)
                {
                    using SlhDsa slhDsa =
                        SlhDsa.ImportSlhDsaSecretKey(
                            SlhDsaAlgorithm.SlhDsaSha2_128s,
                            SlhDsaTestData.IetfSlhDsaSha2_128sPrivateKeyValue);

                    // Create signer with SlhDsa key
                    signer = new CmsSigner(SubjectIdentifierType.IssuerAndSerialNumber, null, slhDsa);
                    AssertExtensions.TrueExpression(signer.HasPrivateKey);
                    Assert.Null(signer.PrivateKey); // SlhDsa does not expose the private key

                    // Change private key to RSA key
                    signer.PrivateKey = rsa;
                    AssertExtensions.TrueExpression(signer.HasPrivateKey);
                    Assert.NotNull(signer.PrivateKey);
                    Assert.Equal(rsa.SignatureAlgorithm, signer.PrivateKey.SignatureAlgorithm);
                }
            }
        }
#endif
    }
}
