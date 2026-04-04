// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Formats.Asn1;
using Internal.Cryptography;

namespace System.Security.Cryptography.X509Certificates
{
    internal sealed class MLDsaX509SignatureGenerator : X509SignatureGenerator
    {
        private readonly MLDsa _key;

        internal MLDsaX509SignatureGenerator(MLDsa key)
        {
            Debug.Assert(key != null);

            _key = key;
        }

        public override byte[] GetSignatureAlgorithmIdentifier(HashAlgorithmName hashAlgorithm)
        {
            // Ignore the hashAlgorithm parameter.
            // This generator only supports ML-DSA "Pure" signatures, but the overall design of
            // CertificateRequest makes it easy for a hashAlgorithm value to get here.

            const int InitialCapacity = 16;

            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER, InitialCapacity);
            writer.PushSequence();
            writer.WriteObjectIdentifier(_key.Algorithm.Oid);
            writer.PopSequence();

            Debug.Assert(writer.GetEncodedLength() <= InitialCapacity);
            return writer.Encode();
        }

        public override byte[] SignData(byte[] data, HashAlgorithmName hashAlgorithm)
        {
            ArgumentNullException.ThrowIfNull(data);

            // Ignore the hashAlgorithm parameter.
            // This generator only supports ML-DSA "Pure" signatures, but the overall design of
            // CertificateRequest makes it easy for a hashAlgorithm value to get here.

            return _key.SignData(data);
        }

        protected override PublicKey BuildPublicKey()
        {
            Oid oid = new Oid(_key.Algorithm.Oid, null);
            byte[] pkBytes = _key.ExportMLDsaPublicKey();

            return new PublicKey(
                oid,
                null,
                new AsnEncodedData(oid, pkBytes, skipCopy: true));
        }
    }
}
