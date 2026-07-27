// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Formats.Asn1;
using System.Security.Cryptography.Asn1;
using Xunit;
using Xunit.Sdk;

namespace System.Security.Cryptography.Tests
{
    internal static class Pkcs8TestHelpers
    {
        internal static void AssertEncryptedPkcs8PrivateKeyContents(PbeParameters pbeParameters, ReadOnlyMemory<byte> contents)
        {
            EncryptedPrivateKeyInfoAsn epki = EncryptedPrivateKeyInfoAsn.Decode(contents, AsnEncodingRules.BER);
            AlgorithmIdentifierAsn algorithmIdentifier = epki.EncryptionAlgorithm;

            if (pbeParameters.EncryptionAlgorithm == PbeEncryptionAlgorithm.TripleDes3KeyPkcs12)
            {
                // pbeWithSHA1And3-KeyTripleDES-CBC
                Assert.Equal("1.2.840.113549.1.12.1.3", algorithmIdentifier.Algorithm);
                ValuePBEParameter.Decode(
                    algorithmIdentifier.Parameters.Value.Span,
                    AsnEncodingRules.BER,
                    out ValuePBEParameter pbeParameterAsn);

                Assert.Equal(pbeParameters.IterationCount, pbeParameterAsn.IterationCount);
            }
            else
            {
                Assert.Equal("1.2.840.113549.1.5.13", algorithmIdentifier.Algorithm); // PBES2
                ValuePBES2Params.Decode(
                    algorithmIdentifier.Parameters.Value.Span,
                    AsnEncodingRules.BER,
                    out ValuePBES2Params pbes2Params);
                Assert.Equal("1.2.840.113549.1.5.12", pbes2Params.KeyDerivationFunc.Algorithm); // PBKDF2
                ValuePbkdf2Params.Decode(
                    pbes2Params.KeyDerivationFunc.Parameters,
                    AsnEncodingRules.BER,
                    out ValuePbkdf2Params pbkdf2Params);
                string expectedEncryptionOid = pbeParameters.EncryptionAlgorithm switch
                {
                    PbeEncryptionAlgorithm.Aes128Cbc => "2.16.840.1.101.3.4.1.2",
                    PbeEncryptionAlgorithm.Aes192Cbc => "2.16.840.1.101.3.4.1.22",
                    PbeEncryptionAlgorithm.Aes256Cbc => "2.16.840.1.101.3.4.1.42",
                    _ => throw new CryptographicException(),
                };

                Assert.Equal(pbeParameters.IterationCount, pbkdf2Params.IterationCount);
                Assert.Equal(pbeParameters.HashAlgorithm, GetHashAlgorithmFromPbkdf2Params(pbkdf2Params));
                Assert.Equal(expectedEncryptionOid, pbes2Params.EncryptionScheme.Algorithm);
            }
        }

        private static HashAlgorithmName GetHashAlgorithmFromPbkdf2Params(in ValuePbkdf2Params pbkdf2Params)
        {
            return pbkdf2Params.Prf.Algorithm switch
            {
                "1.2.840.113549.2.7" => HashAlgorithmName.SHA1,
                "1.2.840.113549.2.9" => HashAlgorithmName.SHA256,
                "1.2.840.113549.2.10" => HashAlgorithmName.SHA384,
                "1.2.840.113549.2.11" => HashAlgorithmName.SHA512,
                string other => throw new XunitException($"Unknown hash algorithm OID '{other}'."),
            };
        }
    }
}
