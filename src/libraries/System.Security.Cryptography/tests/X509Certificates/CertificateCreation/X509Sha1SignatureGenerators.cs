// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.Asn1;
using Test.Cryptography;
#if NET10_0_OR_GREATER
using System.Formats.Asn1;
#endif

namespace System.Security.Cryptography.X509Certificates.Tests.CertificateCreation
{
    internal sealed class ECDsaSha1SignatureGenerator : X509SignatureGenerator
    {
        private readonly X509SignatureGenerator _realGenerator;

        internal ECDsaSha1SignatureGenerator(ECDsa ecdsa)
        {
            _realGenerator = CreateForECDsa(ecdsa);
        }

        protected override PublicKey BuildPublicKey() => _realGenerator.PublicKey;

        public override byte[] GetSignatureAlgorithmIdentifier(HashAlgorithmName hashAlgorithm)
        {
            if (hashAlgorithm == HashAlgorithmName.SHA1)
                return "300906072A8648CE3D0401".HexToByteArray();

            throw new InvalidOperationException();
        }

        public override byte[] SignData(byte[] data, HashAlgorithmName hashAlgorithm) =>
            _realGenerator.SignData(data, hashAlgorithm);
    }

    internal sealed class RSASha1Pkcs1SignatureGenerator : X509SignatureGenerator
    {
        private readonly X509SignatureGenerator _realRsaGenerator;

        internal RSASha1Pkcs1SignatureGenerator(RSA rsa)
        {
            _realRsaGenerator = CreateForRSA(rsa, RSASignaturePadding.Pkcs1);
        }

        protected override PublicKey BuildPublicKey() => _realRsaGenerator.PublicKey;

        public override byte[] GetSignatureAlgorithmIdentifier(HashAlgorithmName hashAlgorithm)
        {
            if (hashAlgorithm == HashAlgorithmName.SHA1)
                return "300D06092A864886F70D0101050500".HexToByteArray();

            throw new InvalidOperationException();
        }

        public override byte[] SignData(byte[] data, HashAlgorithmName hashAlgorithm) =>
            _realRsaGenerator.SignData(data, hashAlgorithm);
    }

    internal sealed class RSASha1PssSignatureGenerator : X509SignatureGenerator
    {
        private readonly RSASignaturePadding _signaturePadding;
        private readonly X509SignatureGenerator _realRsaGenerator;

        internal RSASha1PssSignatureGenerator(RSA rsa, RSASignaturePadding signaturePadding)
        {
            _signaturePadding = signaturePadding;
            _realRsaGenerator = CreateForRSA(rsa, signaturePadding);
        }

        protected override PublicKey BuildPublicKey() => _realRsaGenerator.PublicKey;

        public override byte[] GetSignatureAlgorithmIdentifier(HashAlgorithmName hashAlgorithm)
        {
            if (hashAlgorithm == HashAlgorithmName.SHA1)
            {
                if (_signaturePadding.PssSaltLength == RSASignaturePadding.PssSaltLengthIsHashLength)
                {
                    // sha1WithRSAEncryption with RSASSA-PSS parameters
                    return "300D06092A864886F70D01010A3000".HexToByteArray();
                }
                else if (_signaturePadding.PssSaltLength == 1)
                {
                    return "303506092a864886f70d01010a3028a009300706052b0e03021aa116301406092a864886f70d010108300706052b0e03021aa203020101".HexToByteArray();
                }
                else if (_signaturePadding.PssSaltLength == RSASignaturePadding.PssSaltLengthMax)
                {
                    // Salt length is 234
                    return "303606092a864886f70d01010a3029a009300706052b0e03021aa116301406092a864886f70d010108300706052b0e03021aa204020200ea".HexToByteArray();
                }
            }
            throw new InvalidOperationException();
        }

        public override byte[] SignData(byte[] data, HashAlgorithmName hashAlgorithm) =>
            _realRsaGenerator.SignData(data, hashAlgorithm);
    }
}
