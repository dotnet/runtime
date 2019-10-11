// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography
{
    internal static class EccKeyFormatHelper
    {
        // This is the same limit that OpenSSL 1.0.2-1.1.1 has,
        // there's no real point reading anything bigger than this (for now).
        private const int MaxFieldBitSize = 661;

        private static readonly string[] s_validOids =
        {
            Oids.EcPublicKey,
        };

        internal static void ReadSubjectPublicKeyInfo(
            ReadOnlySpan<byte> source,
            out int bytesRead,
            out ECParameters key)
        {
            KeyFormatHelper.ReadSubjectPublicKeyInfo<ECParameters>(
                s_validOids,
                source,
                FromECPublicKey,
                out bytesRead,
                out key);
        }

        internal static ReadOnlyMemory<byte> ReadSubjectPublicKeyInfo(
             ReadOnlyMemory<byte> source,
             out int bytesRead)
        {
            return KeyFormatHelper.ReadSubjectPublicKeyInfo(
                s_validOids,
                source,
                out bytesRead);
        }

        internal static void ReadEncryptedPkcs8(
            ReadOnlySpan<byte> source,
            ReadOnlySpan<char> password,
            out int bytesRead,
            out ECParameters key)
        {
            KeyFormatHelper.ReadEncryptedPkcs8<ECParameters>(
                s_validOids,
                source,
                password,
                FromECPrivateKey,
                out bytesRead,
                out key);
        }

        internal static void ReadEncryptedPkcs8(
            ReadOnlySpan<byte> source,
            ReadOnlySpan<byte> passwordBytes,
            out int bytesRead,
            out ECParameters key)
        {
            KeyFormatHelper.ReadEncryptedPkcs8<ECParameters>(
                s_validOids,
                source,
                passwordBytes,
                FromECPrivateKey,
                out bytesRead,
                out key);
        }

        internal static unsafe ECParameters FromECPrivateKey(ReadOnlySpan<byte> key, out int bytesRead)
        {
            fixed (byte* ptr = &MemoryMarshal.GetReference(key))
            {
                using (MemoryManager<byte> manager = new PointerMemoryManager<byte>(ptr, key.Length))
                {
                    AsnReader reader = new AsnReader(manager.Memory, AsnEncodingRules.BER);
                    AlgorithmIdentifierAsn algId = default;
                    ReadOnlyMemory<byte> firstValue = reader.PeekEncodedValue();
                    FromECPrivateKey(firstValue, algId, out ECParameters ret);
                    bytesRead = firstValue.Length;
                    return ret;
                }
            }
        }

        internal static void FromECPrivateKey(
            ReadOnlyMemory<byte> keyData,
            in AlgorithmIdentifierAsn algId,
            out ECParameters ret)
        {
            ECPrivateKey key = ECPrivateKey.Decode(keyData, AsnEncodingRules.BER);

            ValidateParameters(key.Parameters, algId);

            if (key.Version != 1)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            // Implementation limitation
            if (key.PublicKey == null)
            {
                throw new CryptographicException(SR.Cryptography_NotValidPublicOrPrivateKey);
            }

            ReadOnlySpan<byte> publicKeyBytes = key.PublicKey.Value.Span;

            if (publicKeyBytes.Length == 0)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            // Implementation limitation
            // 04 (Uncompressed ECPoint) is almost always used.
            if (publicKeyBytes[0] != 0x04)
            {
                throw new CryptographicException(SR.Cryptography_NotValidPublicOrPrivateKey);
            }

            // https://www.secg.org/sec1-v2.pdf, 2.3.4, #3 (M has length 2 * CEIL(log2(q)/8) + 1)
            if (publicKeyBytes.Length != 2 * key.PrivateKey.Length + 1)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            ECDomainParameters domainParameters;

            if (key.Parameters != null)
            {
                domainParameters = key.Parameters.Value;
            }
            else
            {
                domainParameters = ECDomainParameters.Decode(algId.Parameters.Value, AsnEncodingRules.DER);
            }

            ret = new ECParameters
            {
                Curve = GetCurve(domainParameters),
                Q =
                {
                    X = publicKeyBytes.Slice(1, key.PrivateKey.Length).ToArray(),
                    Y = publicKeyBytes.Slice(1 + key.PrivateKey.Length).ToArray(),
                },
                D = key.PrivateKey.ToArray(),
            };

            ret.Validate();
        }

        internal static void FromECPublicKey(
            ReadOnlyMemory<byte> key,
            in AlgorithmIdentifierAsn algId,
            out ECParameters ret)
        {
            if (algId.Parameters == null)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            ReadOnlySpan<byte> publicKeyBytes = key.Span;

            if (publicKeyBytes.Length == 0)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            // Implementation limitation.
            // 04 (Uncompressed ECPoint) is almost always used.
            if (publicKeyBytes[0] != 0x04)
            {
                throw new CryptographicException(SR.Cryptography_NotValidPublicOrPrivateKey);
            }

            // https://www.secg.org/sec1-v2.pdf, 2.3.4, #3 (M has length 2 * CEIL(log2(q)/8) + 1)
            if ((publicKeyBytes.Length & 0x01) != 1)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            int fieldWidth = publicKeyBytes.Length / 2;

            ECDomainParameters domainParameters = ECDomainParameters.Decode(
                algId.Parameters.Value,
                AsnEncodingRules.DER);

            ret = new ECParameters
            {
                Curve = GetCurve(domainParameters),
                Q =
                {
                    X = publicKeyBytes.Slice(1, fieldWidth).ToArray(),
                    Y = publicKeyBytes.Slice(1 + fieldWidth).ToArray(),
                },
            };

            ret.Validate();
        }

        private static void ValidateParameters(ECDomainParameters? keyParameters, in AlgorithmIdentifierAsn algId)
        {
            // At least one is required
            if (keyParameters == null && algId.Parameters == null)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            // If they are both specified they must match.
            if (keyParameters != null && algId.Parameters != null)
            {
                ReadOnlySpan<byte> algIdParameters = algId.Parameters.Value.Span;

                // X.509 SubjectPublicKeyInfo specifies DER encoding.
                // RFC 5915 specifies DER encoding for EC Private Keys.
                // So we can compare as DER.
                using (AsnWriter writer = new AsnWriter(AsnEncodingRules.DER))
                {
                    keyParameters.Value.Encode(writer);

                    if (!writer.ValueEquals(algIdParameters))
                    {
                        throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                    }
                }
            }
        }

        private static ECCurve GetCurve(ECDomainParameters domainParameters)
        {
            if (domainParameters.Specified.HasValue)
            {
                return GetSpecifiedECCurve(domainParameters.Specified.Value);
            }

            if (domainParameters.Named == null)
            {
                throw new CryptographicException(SR.Cryptography_ECC_NamedCurvesOnly);
            }

            Oid curveOid = domainParameters.Named;

            switch (curveOid.Value)
            {
                case Oids.secp256r1:
                    curveOid = new Oid(Oids.secp256r1, nameof(ECCurve.NamedCurves.nistP256));
                    break;
                case Oids.secp384r1:
                    curveOid = new Oid(Oids.secp384r1, nameof(ECCurve.NamedCurves.nistP384));
                    break;
                case Oids.secp521r1:
                    curveOid = new Oid(Oids.secp521r1, nameof(ECCurve.NamedCurves.nistP521));
                    break;
            }

            return ECCurve.CreateFromOid(curveOid);
        }

        private static ECCurve GetSpecifiedECCurve(SpecifiedECDomain specifiedParameters)
        {
            // sec1-v2 C.3:
            //
            // Versions 1, 2, and 3 are defined.
            // 1 is just data, 2 and 3 mean that a seed is required (with different reasons for why,
            // but they're human-reasons, not technical ones).
            if (specifiedParameters.Version < 1 || specifiedParameters.Version > 3)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            if (specifiedParameters.Version > 1 && !specifiedParameters.Curve.Seed.HasValue)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            byte[] primeOrPoly;
            bool prime;

            switch (specifiedParameters.FieldID.FieldType)
            {
                case Oids.EcPrimeField:
                    prime = true;
                    AsnReader primeReader = new AsnReader(specifiedParameters.FieldID.Parameters, AsnEncodingRules.BER);
                    ReadOnlySpan<byte> primeValue = primeReader.ReadIntegerBytes().Span;

                    if (primeValue[0] == 0)
                    {
                        primeValue = primeValue.Slice(1);
                    }

                    if (primeValue.Length > (MaxFieldBitSize / 8))
                    {
                        throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                    }

                    primeOrPoly = primeValue.ToArray();
                    break;
                case Oids.EcChar2Field:
                    prime = false;
                    AsnReader char2Reader = new AsnReader(specifiedParameters.FieldID.Parameters, AsnEncodingRules.BER);
                    AsnReader innerReader = char2Reader.ReadSequence();
                    char2Reader.ThrowIfNotEmpty();

                    // Characteristic-two ::= SEQUENCE
                    // {
                    //     m INTEGER, -- Field size
                    //     basis CHARACTERISTIC-TWO.&id({BasisTypes}),
                    //     parameters CHARACTERISTIC-TWO.&Type({BasisTypes}{@basis})
                    // }

                    if (!innerReader.TryReadInt32(out int m) || m > MaxFieldBitSize || m < 0)
                    {
                        throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                    }

                    int k1;
                    int k2 = -1;
                    int k3 = -1;

                    switch (innerReader.ReadObjectIdentifierAsString())
                    {
                        case Oids.EcChar2TrinomialBasis:
                            // Trinomial ::= INTEGER
                            if (!innerReader.TryReadInt32(out k1) || k1 >= m || k1 < 1)
                            {
                                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                            }

                            break;
                        case Oids.EcChar2PentanomialBasis:
                            // Pentanomial ::= SEQUENCE
                            // {
                            //     k1 INTEGER, -- k1 > 0
                            //     k2 INTEGER, -- k2 > k1
                            //     k3 INTEGER -- k3 > k2
                            // }
                            AsnReader pentanomialReader = innerReader.ReadSequence();

                            if (!pentanomialReader.TryReadInt32(out k1) ||
                                !pentanomialReader.TryReadInt32(out k2) ||
                                !pentanomialReader.TryReadInt32(out k3) ||
                                k1 < 1 ||
                                k2 <= k1 ||
                                k3 <= k2 ||
                                k3 >= m)
                            {
                                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                            }

                            pentanomialReader.ThrowIfNotEmpty();

                            break;
                        default:
                            throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                    }

                    innerReader.ThrowIfNotEmpty();

                    BitArray poly = new BitArray(m + 1);
                    poly.Set(m, true);
                    poly.Set(k1, true);
                    poly.Set(0, true);

                    if (k2 > 0)
                    {
                        poly.Set(k2, true);
                        poly.Set(k3, true);
                    }

                    primeOrPoly = new byte[(m + 7) / 8];
                    poly.CopyTo(primeOrPoly, 0);
                    Array.Reverse(primeOrPoly);
                    break;
                default:
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            ECCurve curve;

            if (prime)
            {
                curve = new ECCurve
                {
                    CurveType = ECCurve.ECCurveType.PrimeShortWeierstrass,
                    Prime = primeOrPoly,
                };
            }
            else
            {
                curve = new ECCurve
                {
                    CurveType = ECCurve.ECCurveType.Characteristic2,
                    Polynomial = primeOrPoly,
                };
            }

            curve.A = specifiedParameters.Curve.A.ToUnsignedIntegerBytes(primeOrPoly.Length);
            curve.B = specifiedParameters.Curve.B.ToUnsignedIntegerBytes(primeOrPoly.Length);
            curve.Order = specifiedParameters.Order.ToUnsignedIntegerBytes(primeOrPoly.Length);

            ReadOnlySpan<byte> baseSpan = specifiedParameters.Base.Span;

            // We only understand the uncompressed point encoding, but that's almost always what's used.
            if (baseSpan[0] != 0x04 || baseSpan.Length != 2 * primeOrPoly.Length + 1)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            curve.G.X = baseSpan.Slice(1, primeOrPoly.Length).ToArray();
            curve.G.Y = baseSpan.Slice(1 + primeOrPoly.Length).ToArray();

            if (specifiedParameters.Cofactor.HasValue)
            {
                curve.Cofactor = specifiedParameters.Cofactor.Value.ToUnsignedIntegerBytes();
            }

            return curve;
        }

        internal static AsnWriter WriteSubjectPublicKeyInfo(ECParameters ecParameters)
        {
            ecParameters.Validate();

            // Since the public key format for EC keys is not ASN.1,
            // write the SPKI structure manually.

            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);

            // SubjectPublicKeyInfo
            writer.PushSequence();

            // algorithm
            WriteAlgorithmIdentifier(ecParameters, writer);

            // subjectPublicKey
            WriteUncompressedPublicKey(ecParameters, writer);

            writer.PopSequence();
            return writer;
        }

        private static AsnWriter WriteAlgorithmIdentifier(in ECParameters ecParameters)
        {
            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
            WriteAlgorithmIdentifier(ecParameters, writer);
            return writer;
        }

        private static void WriteAlgorithmIdentifier(in ECParameters ecParameters, AsnWriter writer)
        {
            writer.PushSequence();

            writer.WriteObjectIdentifier(Oids.EcPublicKey);
            WriteEcParameters(ecParameters, writer);

            writer.PopSequence();
        }

        internal static AsnWriter WritePkcs8PrivateKey(ECParameters ecParameters)
        {
            ecParameters.Validate();

            if (ecParameters.D == null)
            {
                throw new CryptographicException(SR.Cryptography_CSP_NoPrivateKey);
            }

            // Don't need the domain parameters because they're contained in the algId.
            using (AsnWriter ecPrivateKey = WriteEcPrivateKey(ecParameters, includeDomainParameters: false))
            using (AsnWriter algorithmIdentifier = WriteAlgorithmIdentifier(ecParameters))
            {
                return KeyFormatHelper.WritePkcs8(algorithmIdentifier, ecPrivateKey);
            }
        }

        private static void WriteEcParameters(ECParameters ecParameters, AsnWriter writer)
        {
            if (ecParameters.Curve.IsNamed)
            {
                Oid oid = ecParameters.Curve.Oid;

                // On Windows the FriendlyName is populated in places where the Value mightn't be.
                if (string.IsNullOrEmpty(oid.Value))
                {
                    oid = Oid.FromFriendlyName(oid.FriendlyName, OidGroup.All);
                }

                writer.WriteObjectIdentifier(oid.Value);
            }
            else if (ecParameters.Curve.IsExplicit)
            {
                Debug.Assert(ecParameters.Curve.IsPrime || ecParameters.Curve.IsCharacteristic2);
                WriteSpecifiedECDomain(ecParameters, writer);
            }
            else
            {
                throw new CryptographicException(
                    SR.Format(SR.Cryptography_CurveNotSupported, ecParameters.Curve.CurveType.ToString()));
            }
        }

        private static void WriteSpecifiedECDomain(ECParameters ecParameters, AsnWriter writer)
        {
            int m;
            int k1;
            int k2;
            int k3;
            m = k1 = k2 = k3 = -1;

            if (ecParameters.Curve.IsCharacteristic2)
            {
                DetermineChar2Parameters(ecParameters, ref m, ref k1, ref k2, ref k3);
            }

            // SpecifiedECDomain
            writer.PushSequence();
            {
                // version
                // We don't know if the seed (if present) is verifiably random (2).
                // We also don't know if the base point is verifiably random (3).
                // So just be version 1.
                writer.WriteInteger(1);

                // fieldId
                writer.PushSequence();
                {
                    if (ecParameters.Curve.IsPrime)
                    {
                        writer.WriteObjectIdentifier(Oids.EcPrimeField);
                        writer.WriteIntegerUnsigned(ecParameters.Curve.Prime);
                    }
                    else
                    {
                        Debug.Assert(ecParameters.Curve.IsCharacteristic2);

                        // id
                        writer.WriteObjectIdentifier(Oids.EcChar2Field);

                        // Parameters (Characteristic-two)
                        writer.PushSequence();
                        {
                            // m
                            writer.WriteInteger(m);

                            if (k3 > 0)
                            {
                                writer.WriteObjectIdentifier(Oids.EcChar2PentanomialBasis);

                                writer.PushSequence();
                                {
                                    writer.WriteInteger(k1);
                                    writer.WriteInteger(k2);
                                    writer.WriteInteger(k3);

                                    writer.PopSequence();
                                }
                            }
                            else
                            {
                                Debug.Assert(k2 < 0);
                                Debug.Assert(k1 > 0);

                                writer.WriteObjectIdentifier(Oids.EcChar2TrinomialBasis);
                                writer.WriteInteger(k1);
                            }

                            writer.PopSequence();
                        }
                    }

                    writer.PopSequence();
                }

                // curve
                WriteCurve(ecParameters.Curve, writer);

                // base
                WriteUncompressedBasePoint(ecParameters, writer);

                // order
                writer.WriteIntegerUnsigned(ecParameters.Curve.Order);

                // cofactor
                if (ecParameters.Curve.Cofactor != null)
                {
                    writer.WriteIntegerUnsigned(ecParameters.Curve.Cofactor);
                }

                // hash is omitted.

                writer.PopSequence();
            }
        }

        private static void DetermineChar2Parameters(
            in ECParameters ecParameters,
            ref int m,
            ref int k1,
            ref int k2,
            ref int k3)
        {
            byte[] polynomial = ecParameters.Curve.Polynomial;
            int lastIndex = polynomial.Length - 1;

            // The most significant byte needs a set bit, and the least significant bit must be set.
            if (polynomial[0] == 0 || (polynomial[lastIndex] & 1) != 1)
            {
                throw new CryptographicException(SR.Cryptography_InvalidECCharacteristic2Curve);
            }

            for (int localBitIndex = 7; localBitIndex >= 0; localBitIndex--)
            {
                int test = 1 << localBitIndex;

                if ((polynomial[0] & test) == test)
                {
                    m = checked(8 * lastIndex + localBitIndex);
                }
            }

            // Find the other set bits. Since we've already found m and 0, there is either
            // one remaining (trinomial) or 3 (pentanomial).
            for (int inverseIndex = 0; inverseIndex < polynomial.Length; inverseIndex++)
            {
                int forwardIndex = lastIndex - inverseIndex;
                byte val = polynomial[forwardIndex];

                for (int localBitIndex = 0; localBitIndex < 8; localBitIndex++)
                {
                    int test = 1 << localBitIndex;

                    if ((val & test) == test)
                    {
                        int bitIndex = 8 * inverseIndex + localBitIndex;

                        if (bitIndex == 0)
                        {
                            // The bottom bit is always set, it's not considered a parameter.
                        }
                        else if (bitIndex == m)
                        {
                            break;
                        }
                        else if (k1 < 0)
                        {
                            k1 = bitIndex;
                        }
                        else if (k2 < 0)
                        {
                            k2 = bitIndex;
                        }
                        else if (k3 < 0)
                        {
                            k3 = bitIndex;
                        }
                        else
                        {
                            // More than pentanomial.
                            throw new CryptographicException(SR.Cryptography_InvalidECCharacteristic2Curve);
                        }
                    }
                }
            }

            if (k3 > 0)
            {
                // Pentanomial
            }
            else if (k2 > 0)
            {
                // There is no quatranomial
                throw new CryptographicException(SR.Cryptography_InvalidECCharacteristic2Curve);
            }
            else if (k1 > 0)
            {
                // Trinomial
            }
            else
            {
                // No smaller bases exist
                throw new CryptographicException(SR.Cryptography_InvalidECCharacteristic2Curve);
            }
        }

        private static void WriteCurve(in ECCurve curve, AsnWriter writer)
        {
            writer.PushSequence();
            WriteFieldElement(curve.A, writer);
            WriteFieldElement(curve.B, writer);

            if (curve.Seed != null)
            {
                writer.WriteBitString(curve.Seed);
            }

            writer.PopSequence();
        }

        private static void WriteFieldElement(byte[] fieldElement, AsnWriter writer)
        {
            int start = 0;

            while (start < fieldElement.Length - 1 && fieldElement[start] == 0)
            {
                start++;
            }

            writer.WriteOctetString(fieldElement.AsSpan(start));
        }

        private static void WriteUncompressedBasePoint(in ECParameters ecParameters, AsnWriter writer)
        {
            int basePointLength = ecParameters.Curve.G.X.Length * 2 + 1;
            byte[] tmp = CryptoPool.Rent(basePointLength);
            tmp[0] = 0x04;
            ecParameters.Curve.G.X.CopyTo(tmp.AsSpan(1));
            ecParameters.Curve.G.Y.CopyTo(tmp.AsSpan(1 + ecParameters.Curve.G.X.Length));
            writer.WriteOctetString(tmp.AsSpan(0, basePointLength));
            // Non-sensitive data.
            CryptoPool.Return(tmp, clearSize: 0);
        }

        private static void WriteUncompressedPublicKey(in ECParameters ecParameters, AsnWriter writer)
        {
            int publicKeyLength = ecParameters.Q.X.Length * 2 + 1;

            writer.WriteBitString(
                publicKeyLength,
                ecParameters.Q,
                (publicKeyBytes, point) =>
                {
                    publicKeyBytes[0] = 0x04;
                    point.X.AsSpan().CopyTo(publicKeyBytes.Slice(1));
                    point.Y.AsSpan().CopyTo(publicKeyBytes.Slice(1 + point.X.Length));
                });
        }

        internal static AsnWriter WriteECPrivateKey(in ECParameters ecParameters)
        {
            return WriteEcPrivateKey(ecParameters, includeDomainParameters: true);
        }

        private static AsnWriter WriteEcPrivateKey(in ECParameters ecParameters, bool includeDomainParameters)
        {
            bool returning = false;
            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);

            try
            {
                // ECPrivateKey
                writer.PushSequence();

                // version 1
                writer.WriteInteger(1);

                // privateKey
                writer.WriteOctetString(ecParameters.D);

                // domainParameters
                if (includeDomainParameters)
                {
                    Asn1Tag explicit0 = new Asn1Tag(TagClass.ContextSpecific, 0, isConstructed: true);
                    writer.PushSequence(explicit0);

                    WriteEcParameters(ecParameters, writer);

                    writer.PopSequence(explicit0);
                }

                // publicKey
                {
                    Asn1Tag explicit1 = new Asn1Tag(TagClass.ContextSpecific, 1, isConstructed: true);
                    writer.PushSequence(explicit1);

                    WriteUncompressedPublicKey(ecParameters, writer);

                    writer.PopSequence(explicit1);
                }

                writer.PopSequence();
                returning = true;
                return writer;
            }
            finally
            {
                if (!returning)
                {
                    writer.Dispose();
                }
            }
        }
    }
}
