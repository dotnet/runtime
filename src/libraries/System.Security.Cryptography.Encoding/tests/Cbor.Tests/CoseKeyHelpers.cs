// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;

namespace System.Formats.Cbor.Tests
{
    public static class CborCoseKeyHelpers
    {
        public static byte[] ExportCosePublicKey(ECDsa ecDsa, HashAlgorithmName hashAlgName)
        {
            ECParameters ecParams = ecDsa.ExportParameters(includePrivateParameters: false);
            using var writer = new CborWriter(CborConformanceLevel.Ctap2Canonical);
            WriteECParametersAsCosePublicKey(writer, ecParams, hashAlgName);
            return writer.GetEncoding();
        }

        public static (ECDsa ecDsa, HashAlgorithmName hashAlgName) ParseCosePublicKey(byte[] coseKey)
        {
            var reader = new CborReader(coseKey, CborConformanceLevel.Ctap2Canonical);
            (ECParameters ecParams, HashAlgorithmName hashAlgName) = ReadECParametersAsCosePublicKey(reader);
            return (ECDsa.Create(ecParams), hashAlgName);
        }

        public static void WriteECParametersAsCosePublicKey(CborWriter writer, ECParameters ecParams, HashAlgorithmName algorithmName)
        {
            Debug.Assert(writer.ConformanceLevel == CborConformanceLevel.Ctap2Canonical);

            if (ecParams.Q.X is null || ecParams.Q.Y is null)
            {
                throw new ArgumentException("does not specify a public key point.", nameof(ecParams));
            }

            // run these first to perform necessary validation
            (CoseKeyType kty, CoseCrvId crv) = MapECCurveToCoseKtyAndCrv(ecParams.Curve);
            CoseKeyAlgorithm alg = MapHashAlgorithmNameToCoseKeyAlg(algorithmName);

            // begin writing CBOR object
            writer.WriteStartMap();

            WriteCoseKeyLabel(writer, CoseKeyLabel.Kty);
            writer.WriteInt32((int)kty);

            WriteCoseKeyLabel(writer, CoseKeyLabel.Alg);
            writer.WriteInt32((int)alg);

            WriteCoseKeyLabel(writer, CoseKeyLabel.EcCrv);
            writer.WriteInt32((int)crv);

            WriteCoseKeyLabel(writer, CoseKeyLabel.EcX);
            writer.WriteByteString(ecParams.Q.X);

            WriteCoseKeyLabel(writer, CoseKeyLabel.EcY);
            writer.WriteByteString(ecParams.Q.Y);

            writer.WriteEndMap();

            static (CoseKeyType kty, CoseCrvId crv) MapECCurveToCoseKtyAndCrv(ECCurve curve)
            {
                if (!curve.IsNamed)
                {
                    throw new ArgumentException("Only named curves supported in EC COSE keys.", nameof(curve));
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
        }

        public static (ECParameters, HashAlgorithmName) ReadECParametersAsCosePublicKey(CborReader reader)
        {
            Debug.Assert(reader.ConformanceLevel == CborConformanceLevel.Ctap2Canonical);

            var ecParams = new ECParameters();

            int? length = reader.ReadStartMap();
            if (length != 5)
            {
                throw new FormatException("Unexpected number of elements in the CBOR cose key.");
            }

            // CTAP2 guarantees order of fields

            try
            {
                ReadCoseKeyLabel(reader, CoseKeyLabel.Kty);
                CoseKeyType kty = (CoseKeyType)reader.ReadInt32();

                ReadCoseKeyLabel(reader, CoseKeyLabel.Alg);
                CoseKeyAlgorithm alg = (CoseKeyAlgorithm)reader.ReadInt32();
                HashAlgorithmName algName = MapCoseKeyAlgToHashAlgorithmName(alg);

                ReadCoseKeyLabel(reader, CoseKeyLabel.EcCrv);
                CoseCrvId crv = (CoseCrvId)reader.ReadInt32();

                if (!IsValidKtyCrvCombination(kty, crv))
                {
                    throw new FormatException("Invalid kty/crv combination in COSE key.");
                }

                ecParams.Curve = MapCoseCrvToECCurve(crv);

                ReadCoseKeyLabel(reader, CoseKeyLabel.EcX);
                ecParams.Q.X = reader.ReadByteString();

                ReadCoseKeyLabel(reader, CoseKeyLabel.EcY);
                ecParams.Q.Y = reader.ReadByteString();

                reader.ReadEndMap();

                return (ecParams, algName);
            }
            catch (InvalidOperationException e)
            {
                throw new FormatException("Invalid COSE_key format in CBOR document", e);
            }

            static bool IsValidKtyCrvCombination(CoseKeyType kty, CoseCrvId crv)
            {
                switch (crv)
                {
                    case CoseCrvId.P256:
                    case CoseCrvId.P384:
                    case CoseCrvId.P521:
                        return kty == CoseKeyType.EC2;

                    case CoseCrvId.X255519:
                    case CoseCrvId.X448:
                    case CoseCrvId.Ed25519:
                    case CoseCrvId.Ed448:
                        return kty == CoseKeyType.OKP;

                    default:
                        return false;
                };
            }

            static ECCurve MapCoseCrvToECCurve(CoseCrvId crv)
            {
                return crv switch
                {
                    CoseCrvId.P256 => ECCurve.NamedCurves.nistP256,
                    CoseCrvId.P384 => ECCurve.NamedCurves.nistP384,
                    CoseCrvId.P521 => ECCurve.NamedCurves.nistP521,
                    //(CoseCrvId.X255519 or
                    //CoseCrvId.X448 or
                    //CoseCrvId.Ed25519 or
                    //CoseCrvId.Ed448) => throw new NotImplementedException("OKP type curves not implemented."),
                    _ => throw new FormatException("Unrecognized COSE crv value."),
                };
            }

            static HashAlgorithmName MapCoseKeyAlgToHashAlgorithmName(CoseKeyAlgorithm alg)
            {
                return alg switch
                {
                    CoseKeyAlgorithm.ES256 => HashAlgorithmName.SHA256,
                    CoseKeyAlgorithm.ES384 => HashAlgorithmName.SHA384,
                    CoseKeyAlgorithm.ES512 => HashAlgorithmName.SHA512,
                    _ => throw new FormatException("Unrecognized COSE alg value."),
                };
            }
        }

        private static void WriteCoseKeyLabel(CborWriter writer, CoseKeyLabel label)
        {
            writer.WriteInt32((int)label);
        }

        private static CoseKeyLabel ReadCoseKeyLabel(CborReader reader, CoseKeyLabel? expectedLabel = null)
        {
            var label = (CoseKeyLabel)reader.ReadInt32();
            if (label != expectedLabel)
            {
                throw new FormatException("Unexpected COSE key label.");
            }

            return label;
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
