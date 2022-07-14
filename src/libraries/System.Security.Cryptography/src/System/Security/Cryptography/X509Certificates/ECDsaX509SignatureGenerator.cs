// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Formats.Asn1;
using Internal.Cryptography;

namespace System.Security.Cryptography.X509Certificates
{
    internal sealed class ECDsaX509SignatureGenerator : X509SignatureGenerator
    {
        private readonly ECDsa _key;

        internal ECDsaX509SignatureGenerator(ECDsa key)
        {
            Debug.Assert(key != null);

            _key = key;
        }

        public override byte[] GetSignatureAlgorithmIdentifier(HashAlgorithmName hashAlgorithm)
        {
            string oid;

            if (hashAlgorithm == HashAlgorithmName.SHA256)
            {
                oid = Oids.ECDsaWithSha256;
            }
            else if (hashAlgorithm == HashAlgorithmName.SHA384)
            {
                oid = Oids.ECDsaWithSha384;
            }
            else if (hashAlgorithm == HashAlgorithmName.SHA512)
            {
                oid = Oids.ECDsaWithSha512;
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
            writer.PopSequence();
            return writer.Encode();
        }

        public override byte[] SignData(byte[] data, HashAlgorithmName hashAlgorithm)
        {
            return _key.SignData(data, hashAlgorithm, DSASignatureFormat.Rfc3279DerSequence);
        }

        protected override PublicKey BuildPublicKey()
        {
            ECParameters ecParameters = _key.ExportParameters(false);

            if (!ecParameters.Curve.IsNamed)
            {
                throw new InvalidOperationException(SR.Cryptography_ECC_NamedCurvesOnly);
            }

            string? curveOid = ecParameters.Curve.Oid.Value;
            byte[] curveOidEncoded;

            if (string.IsNullOrEmpty(curveOid))
            {
                string friendlyName = ecParameters.Curve.Oid.FriendlyName!;

                // Translate the three curves that were supported Windows 7-8.1, but emit no Oid.Value;
                // otherwise just wash the friendly name back through Oid to see if we can get a value.
                curveOid = friendlyName switch
                {
                    "nistP256" => Oids.secp256r1,
                    "nistP384" => Oids.secp384r1,
                    "nistP521" => Oids.secp521r1,
                    _ => new Oid(friendlyName).Value,
                };
            }

            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
            writer.WriteObjectIdentifier(curveOid!);
            curveOidEncoded = writer.Encode();

            Debug.Assert(ecParameters.Q.X!.Length == ecParameters.Q.Y!.Length);
            byte[] uncompressedPoint = new byte[1 + ecParameters.Q.X.Length + ecParameters.Q.Y.Length];

            // Uncompressed point (0x04)
            uncompressedPoint[0] = 0x04;

            Buffer.BlockCopy(ecParameters.Q.X, 0, uncompressedPoint, 1, ecParameters.Q.X.Length);
            Buffer.BlockCopy(ecParameters.Q.Y, 0, uncompressedPoint, 1 + ecParameters.Q.X.Length, ecParameters.Q.Y.Length);

            Oid ecPublicKey = Oids.EcPublicKeyOid;

            return new PublicKey(
                ecPublicKey,
                new AsnEncodedData(ecPublicKey, curveOidEncoded, skipCopy: true),
                new AsnEncodedData(ecPublicKey, uncompressedPoint, skipCopy: true));
        }
    }
}
