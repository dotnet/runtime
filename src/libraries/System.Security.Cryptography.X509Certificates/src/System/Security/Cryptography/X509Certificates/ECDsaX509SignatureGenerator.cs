// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Numerics;
using System.Security.Cryptography.Asn1;
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

            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.DER))
            {
                writer.PushSequence();
                writer.WriteObjectIdentifier(oid);
                writer.PopSequence();
                return writer.Encode();
            }
        }

        public override byte[] SignData(byte[] data, HashAlgorithmName hashAlgorithm)
        {
            byte[] ieeeFormat = _key.SignData(data, hashAlgorithm);
            return AsymmetricAlgorithmHelpers.ConvertIeee1363ToDer(ieeeFormat);
        }

        protected override PublicKey BuildPublicKey()
        {
            ECParameters ecParameters = _key.ExportParameters(false);

            if (!ecParameters.Curve.IsNamed)
            {
                throw new InvalidOperationException(SR.Cryptography_ECC_NamedCurvesOnly);
            }

            string curveOid = ecParameters.Curve.Oid.Value;
            byte[] curveOidEncoded;

            if (string.IsNullOrEmpty(curveOid))
            {
                string friendlyName = ecParameters.Curve.Oid.FriendlyName;

                // Translate the three curves that were supported Windows 7-8.1, but emit no Oid.Value;
                // otherwise just wash the friendly name back through Oid to see if we can get a value.
                switch (friendlyName)
                {
                    case "nistP256":
                        curveOid = Oids.secp256r1;
                        break;
                    case "nistP384":
                        curveOid = Oids.secp384r1;
                        break;
                    case "nistP521":
                        curveOid = Oids.secp521r1;
                        break;
                    default:
                        curveOid = new Oid(friendlyName).Value;
                        break;
                }
            }

            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.DER))
            {
                writer.WriteObjectIdentifier(curveOid);
                curveOidEncoded = writer.Encode();
            }

            Debug.Assert(ecParameters.Q.X.Length == ecParameters.Q.Y.Length);
            byte[] uncompressedPoint = new byte[1 + ecParameters.Q.X.Length + ecParameters.Q.Y.Length];

            // Uncompressed point (0x04)
            uncompressedPoint[0] = 0x04;
            
            Buffer.BlockCopy(ecParameters.Q.X, 0, uncompressedPoint, 1, ecParameters.Q.X.Length);
            Buffer.BlockCopy(ecParameters.Q.Y, 0, uncompressedPoint, 1 + ecParameters.Q.X.Length, ecParameters.Q.Y.Length);

            Oid ecPublicKey = new Oid(Oids.EcPublicKey);
            
            return new PublicKey(
                ecPublicKey,
                new AsnEncodedData(ecPublicKey, curveOidEncoded),
                new AsnEncodedData(ecPublicKey, uncompressedPoint));
        }
    }
}
