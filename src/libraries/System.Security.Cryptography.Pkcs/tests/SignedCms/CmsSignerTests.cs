// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
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
#endif
    }
}
