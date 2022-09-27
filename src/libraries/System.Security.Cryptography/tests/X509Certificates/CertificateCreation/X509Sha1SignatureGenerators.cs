// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Test.Cryptography;

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
        private readonly X509SignatureGenerator _realRsaGenerator;

        internal RSASha1PssSignatureGenerator(RSA rsa)
        {
            _realRsaGenerator = CreateForRSA(rsa, RSASignaturePadding.Pss);
        }

        protected override PublicKey BuildPublicKey() => _realRsaGenerator.PublicKey;

        public override byte[] GetSignatureAlgorithmIdentifier(HashAlgorithmName hashAlgorithm)
        {
            if (hashAlgorithm == HashAlgorithmName.SHA1)
                return "300D06092A864886F70D01010A3000".HexToByteArray();

            throw new InvalidOperationException();
        }

        public override byte[] SignData(byte[] data, HashAlgorithmName hashAlgorithm) =>
            _realRsaGenerator.SignData(data, hashAlgorithm);
    }
}
