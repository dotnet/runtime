// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Formats.Asn1;

namespace System.Security.Cryptography.X509Certificates
{
    internal sealed class RSAPkcs1X509SignatureGenerator : X509SignatureGenerator
    {
        private readonly RSA _key;

        internal RSAPkcs1X509SignatureGenerator(RSA key)
        {
            Debug.Assert(key != null);

            _key = key;
        }

        public override byte[] SignData(byte[] data, HashAlgorithmName hashAlgorithm)
        {
            return _key.SignData(data, hashAlgorithm, RSASignaturePadding.Pkcs1);
        }

        protected override PublicKey BuildPublicKey()
        {
            return BuildPublicKey(_key);
        }

        internal static PublicKey BuildPublicKey(RSA rsa)
        {
            Oid oid = Oids.RsaOid;
            ReadOnlySpan<byte> asnNull = new byte[] { 0x05, 0x00 };

            // The OID is being passed to everything here because that's what
            // X509Certificate2.PublicKey does.
            return new PublicKey(
                oid,
                // Encode the DER-NULL even though it is OPTIONAL, because everyone else does.
                //
                // This is due to one version of the ASN.1 not including OPTIONAL, and that was
                // the version that got predominately implemented for RSA. Now it's convention.
                new AsnEncodedData(oid, asnNull),
                new AsnEncodedData(oid, rsa.ExportRSAPublicKey(), skipCopy: true));
        }

        public override byte[] GetSignatureAlgorithmIdentifier(HashAlgorithmName hashAlgorithm)
        {
            string oid;

            if (hashAlgorithm == HashAlgorithmName.SHA256)
            {
                oid = Oids.RsaPkcs1Sha256;
            }
            else if (hashAlgorithm == HashAlgorithmName.SHA384)
            {
                oid = Oids.RsaPkcs1Sha384;
            }
            else if (hashAlgorithm == HashAlgorithmName.SHA512)
            {
                oid = Oids.RsaPkcs1Sha512;
            }
            else
            {
                throw new ArgumentOutOfRangeException(
                    nameof(hashAlgorithm),
                    hashAlgorithm,
                    SR.Format(SR.Cryptography_UnknownHashAlgorithm, hashAlgorithm.Name));
            }

            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
            writer.PushSequence();
            writer.WriteObjectIdentifier(oid);
            writer.WriteNull();
            writer.PopSequence();
            return writer.Encode();
        }
    }
}
