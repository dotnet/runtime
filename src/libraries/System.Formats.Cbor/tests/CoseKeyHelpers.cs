// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Security.Cryptography;

// Provides a reference implementation for serializing ECDsa public keys to the COSE_Key format
// according to https://tools.ietf.org/html/rfc8152#section-8.1

namespace System.Formats.Cbor.Tests
{
    public static class CborCoseKeyHelpers
    {
        public static byte[] ExportECDsaPublicKey(ECDsa ecDsa, HashAlgorithmName? hashAlgName)
        {
            ECParameters ecParams = ecDsa.ExportParameters(includePrivateParameters: false);
            var writer = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
            WriteECParametersAsCosePublicKey(writer, ecParams, hashAlgName);
            return writer.Encode();
        }

        public static (ECDsa ecDsa, HashAlgorithmName? hashAlgName) ParseECDsaPublicKey(byte[] coseKey)
        {
            var reader = new CborReader(coseKey, CborConformanceMode.Ctap2Canonical);
            (ECParameters ecParams, HashAlgorithmName? hashAlgName) = ReadECParametersAsCosePublicKey(reader);
            return (ECDsa.Create(ecParams), hashAlgName);
        }

        private static void WriteECParametersAsCosePublicKey(CborWriter writer, ECParameters ecParams, HashAlgorithmName? algorithmName)
        {
            Debug.Assert(writer.ConformanceMode == CborConformanceMode.Ctap2Canonical && writer.ConvertIndefiniteLengthEncodings);

            if (ecParams.Q.X is null || ecParams.Q.Y is null)
            {
                throw new ArgumentException("does not specify a public key point.", nameof(ecParams));
            }

            // run these first to perform necessary validation
            (CoseKeyType kty, CoseCrvId crv) = MapECCurveToCoseKtyAndCrv(ecParams.Curve);
            CoseKeyAlgorithm? alg = (algorithmName != null) ? MapHashAlgorithmNameToCoseKeyAlg(algorithmName.Value) : (CoseKeyAlgorithm?)null;

            // Begin writing a CBOR object
            writer.WriteStartMap(definiteLength: null);

            // NB labels should be sorted according to CTAP2 canonical encoding rules.
            // While the CborWriter will attempt to sort the encodings on its own,
            // it is generally more efficient if keys are written in sorted order to begin with.

            WriteCoseKeyLabel(writer, CoseKeyLabel.Kty);
            writer.WriteInt32((int)kty);

            if (alg != null)
            {
                WriteCoseKeyLabel(writer, CoseKeyLabel.Alg);
                writer.WriteInt32((int)alg);
            }

            WriteCoseKeyLabel(writer, CoseKeyLabel.EcCrv);
            writer.WriteInt32((int)crv);

            WriteCoseKeyLabel(writer, CoseKeyLabel.EcX);
            writer.WriteByteString(ecParams.Q.X);

            WriteCoseKeyLabel(writer, CoseKeyLabel.EcY);
            writer.WriteByteString(ecParams.Q.Y);

            writer.WriteEndMap();

            static (CoseKeyType, CoseCrvId) MapECCurveToCoseKtyAndCrv(ECCurve curve)
            {
                if (!curve.IsNamed)
                {
                    throw new ArgumentException("EC COSE keys only support named curves.", nameof(curve));
                }

                if (MatchesOid(ECCurve.NamedCurves.nistP256))
                {
                    return (CoseKeyType.EC2, CoseCrvId.P256);
                }

                if (MatchesOid(ECCurve.NamedCurves.nistP384))
                {
                    return (CoseKeyType.EC2, CoseCrvId.P384);
                }

                if (MatchesOid(ECCurve.NamedCurves.nistP521))
                {
                    return (CoseKeyType.EC2, CoseCrvId.P521);
                }

                throw new ArgumentException("Unrecognized named curve", curve.Oid.Value);

                bool MatchesOid(ECCurve namedCurve) => curve.Oid.Value == namedCurve.Oid.Value;
            }

            static CoseKeyAlgorithm MapHashAlgorithmNameToCoseKeyAlg(HashAlgorithmName name)
            {
                if (MatchesName(HashAlgorithmName.SHA256))
                {
                    return CoseKeyAlgorithm.ES256;
                }

                if (MatchesName(HashAlgorithmName.SHA384))
                {
                    return CoseKeyAlgorithm.ES384;
                }

                if (MatchesName(HashAlgorithmName.SHA512))
                {
                    return CoseKeyAlgorithm.ES512;
                }

                throw new ArgumentException("Unrecognized hash algorithm name.", nameof(HashAlgorithmName));

                bool MatchesName(HashAlgorithmName candidate) => name.Name == candidate.Name;
            }

            static void WriteCoseKeyLabel(CborWriter writer, CoseKeyLabel label)
            {
                writer.WriteInt32((int)label);
            }
        }

        private static (ECParameters, HashAlgorithmName?) ReadECParametersAsCosePublicKey(CborReader reader)
        {
            Debug.Assert(reader.ConformanceMode == CborConformanceMode.Ctap2Canonical);

            // CTAP2 conformance mode requires that fields are sorted by key encoding.
            // We take advantage of this by reading keys in that order.
            // NB1. COSE labels are not sorted according to canonical integer ordering,
            //      negative labels must always follow positive labels. 
            // NB2. Any unrecognized keys will result in the reader failing.
            // NB3. in order to support optional fields, we need to store the latest read label.
            CoseKeyLabel? latestReadLabel = null;

            int? remainingKeys = reader.ReadStartMap();
            Debug.Assert(remainingKeys != null); // guaranteed by CTAP2 conformance

            try
            {
                var ecParams = new ECParameters();

                ReadCoseKeyLabel(CoseKeyLabel.Kty);
                CoseKeyType kty = (CoseKeyType)reader.ReadInt32();

                HashAlgorithmName? algName = null;
                if (TryReadCoseKeyLabel(CoseKeyLabel.Alg))
                {
                    CoseKeyAlgorithm alg = (CoseKeyAlgorithm)reader.ReadInt32();
                    algName = MapCoseKeyAlgToHashAlgorithmName(alg);
                }

                if (TryReadCoseKeyLabel(CoseKeyLabel.KeyOps))
                {
                    // No-op, simply tolerate potential key_ops labels
                    reader.SkipValue();
                }

                ReadCoseKeyLabel(CoseKeyLabel.EcCrv);
                CoseCrvId crv = (CoseCrvId)reader.ReadInt32();

                if (IsValidKtyCrvCombination(kty, crv))
                {
                    ecParams.Curve = MapCoseCrvToECCurve(crv);
                }
                else
                {
                    throw new CborContentException("Invalid kty/crv combination in COSE key.");
                }

                ReadCoseKeyLabel(CoseKeyLabel.EcX);
                ecParams.Q.X = reader.ReadByteString();

                ReadCoseKeyLabel(CoseKeyLabel.EcY);
                ecParams.Q.Y = reader.ReadByteString();

                if (TryReadCoseKeyLabel(CoseKeyLabel.EcD))
                {
                    throw new CborContentException("COSE key encodes a private key.");
                }

                if (remainingKeys > 0)
                {
                    throw new CborContentException("COSE_key contains unrecognized trailing data.");
                }

                reader.ReadEndMap();

                return (ecParams, algName);
            }
            catch (InvalidOperationException e)
            {
                throw new CborContentException("Invalid COSE_key format in CBOR document", e);
            }

            static bool IsValidKtyCrvCombination(CoseKeyType kty, CoseCrvId crv)
            {
                return (kty, crv) switch
                {
                    (CoseKeyType.EC2, CoseCrvId.P256 or CoseCrvId.P384 or CoseCrvId.P521) => true,
                    (CoseKeyType.OKP, CoseCrvId.X255519 or CoseCrvId.X448 or CoseCrvId.Ed25519 or CoseCrvId.Ed448) => true,
                    _ => false,
                };
                ;
            }

            static ECCurve MapCoseCrvToECCurve(CoseCrvId crv)
            {
                return crv switch
                {
                    CoseCrvId.P256 => ECCurve.NamedCurves.nistP256,
                    CoseCrvId.P384 => ECCurve.NamedCurves.nistP384,
                    CoseCrvId.P521 => ECCurve.NamedCurves.nistP521,
                    CoseCrvId.X255519 or
                    CoseCrvId.X448 or
                    CoseCrvId.Ed25519 or
                    CoseCrvId.Ed448 => throw new NotImplementedException("OKP type curves not implemented."),
                    _ => throw new CborContentException("Unrecognized COSE crv value."),
                };
            }

            static HashAlgorithmName MapCoseKeyAlgToHashAlgorithmName(CoseKeyAlgorithm alg)
            {
                return alg switch
                {
                    CoseKeyAlgorithm.ES256 => HashAlgorithmName.SHA256,
                    CoseKeyAlgorithm.ES384 => HashAlgorithmName.SHA384,
                    CoseKeyAlgorithm.ES512 => HashAlgorithmName.SHA512,
                    _ => throw new CborContentException("Unrecognized COSE alg value."),
                };
            }

            // Handles optional labels
            bool TryReadCoseKeyLabel(CoseKeyLabel expectedLabel)
            {
                // The `currentLabel` parameter can hold a label that
                // was read when handling a previous optional field.
                // We only need to read the next label if uninhabited.
                if (latestReadLabel == null)
                {
                    // check that we have not reached the end of the COSE key object
                    if (remainingKeys == 0)
                    {
                        return false;
                    }

                    latestReadLabel = (CoseKeyLabel)reader.ReadInt32();
                }

                if (expectedLabel != latestReadLabel.Value)
                {
                    return false;
                }

                // read was successful, vacate the `currentLabel` parameter to advance reads.
                latestReadLabel = null;
                remainingKeys--;
                return true;
            }

            // Handles required labels
            void ReadCoseKeyLabel(CoseKeyLabel expectedLabel)
            {
                if (!TryReadCoseKeyLabel(expectedLabel))
                {
                    throw new CborContentException("Unexpected COSE key label.");
                }
            }
        }

        private enum CoseKeyLabel : int
        {
            // cf. https://tools.ietf.org/html/rfc8152#section-7.1 table 3
            Kty = 1,
            Kid = 2,
            Alg = 3,
            KeyOps = 4,
            BaseIv = 5,

            // cf. https://tools.ietf.org/html/rfc8152#section-13.1.1 table 23
            EcCrv = -1,
            EcX = -2,
            EcY = -3,
            EcD = -4,
        };

        private enum CoseCrvId : int
        {
            // cf. https://tools.ietf.org/html/rfc8152#section-13.1 table 22
            P256 = 1,
            P384 = 2,
            P521 = 3,
            X255519 = 4,
            X448 = 5,
            Ed25519 = 6,
            Ed448 = 7,
        }

        private enum CoseKeyType : int
        {
            // cf. https://tools.ietf.org/html/rfc8152#section-13 table 21
            OKP = 1,
            EC2 = 2,

            Symmetric = 4,
            Reserved = 0,
        }

        private enum CoseKeyAlgorithm : int
        {
            // cf. https://tools.ietf.org/html/rfc8152#section-8.1 table 5
            ES256 = -7,
            ES384 = -35,
            ES512 = -36,
        }
    }
}
