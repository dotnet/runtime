// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
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
                oid = Oids.ECDsaSha256;
            }
            else if (hashAlgorithm == HashAlgorithmName.SHA384)
            {
                oid = Oids.ECDsaSha384;
            }
            else if (hashAlgorithm == HashAlgorithmName.SHA512)
            {
                oid = Oids.ECDsaSha512;
            }
            else
            {
                throw new ArgumentOutOfRangeException(
                    nameof(hashAlgorithm),
                    hashAlgorithm,
                    SR.Format(SR.Cryptography_UnknownHashAlgorithm, hashAlgorithm.Name));
            }

            return DerEncoder.ConstructSequence(DerEncoder.SegmentedEncodeOid(oid));
        }

        public override byte[] SignData(byte[] data, HashAlgorithmName hashAlgorithm)
        {
            byte[] ieeeFormat = _key.SignData(data, hashAlgorithm);

            Debug.Assert(ieeeFormat.Length % 2 == 0);
            int segmentLength = ieeeFormat.Length / 2;

            return DerEncoder.ConstructSequence(
                DerEncoder.SegmentedEncodeUnsignedInteger(new ReadOnlySpan<byte>(ieeeFormat, 0, segmentLength)),
                DerEncoder.SegmentedEncodeUnsignedInteger(new ReadOnlySpan<byte>(ieeeFormat, segmentLength, segmentLength)));
        }

        protected override PublicKey BuildPublicKey()
        {
            ECParameters ecParameters = _key.ExportParameters(false);

            if (!ecParameters.Curve.IsNamed)
            {
                throw new InvalidOperationException(SR.Cryptography_ECC_NamedCurvesOnly);
            }

            string curveOid = ecParameters.Curve.Oid.Value;

            if (string.IsNullOrEmpty(curveOid))
            {
                string friendlyName = ecParameters.Curve.Oid.FriendlyName;

                // Translate the three curves that were supported Windows 7-8.1, but emit no Oid.Value;
                // otherwise just wash the friendly name back through Oid to see if we can get a value.
                switch (friendlyName)
                {
                    case "nistP256":
                        curveOid = Oids.EccCurveSecp256r1;
                        break;
                    case "nistP384":
                        curveOid = Oids.EccCurveSecp384r1;
                        break;
                    case "nistP521":
                        curveOid = Oids.EccCurveSecp521r1;
                        break;
                    default:
                        curveOid = new Oid(friendlyName).Value;
                        break;
                }
            }

            Debug.Assert(ecParameters.Q.X.Length == ecParameters.Q.Y.Length);
            byte[] uncompressedPoint = new byte[1 + ecParameters.Q.X.Length + ecParameters.Q.Y.Length];

            // Uncompressed point (0x04)
            uncompressedPoint[0] = 0x04;
            
            Buffer.BlockCopy(ecParameters.Q.X, 0, uncompressedPoint, 1, ecParameters.Q.X.Length);
            Buffer.BlockCopy(ecParameters.Q.Y, 0, uncompressedPoint, 1 + ecParameters.Q.X.Length, ecParameters.Q.Y.Length);

            Oid ecPublicKey = new Oid(Oids.Ecc);
            
            return new PublicKey(
                ecPublicKey,
                new AsnEncodedData(ecPublicKey, DerEncoder.EncodeOid(curveOid)),
                new AsnEncodedData(ecPublicKey, uncompressedPoint));
        }
    }
}
