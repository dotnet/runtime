// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Runtime.InteropServices;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography
{
    /// <summary>
    /// Represents the public and private key of the specified elliptic curve.
    /// </summary>
    public struct ECParameters
    {
        /// <summary>
        /// Public point.
        /// </summary>
        public ECPoint Q;

        /// <summary>
        /// Private Key. Not always present.
        /// </summary>
        public byte[] D;

        /// <summary>
        /// The Curve.
        /// </summary>
        public ECCurve Curve;

        /// <summary>
        /// Validate the current object.
        /// </summary>
        /// <exception cref="CryptographicException">
        ///     if the key or curve parameters are not valid for the current CurveType.
        /// </exception>
        public void Validate()
        {
            bool hasErrors = false;

            if (Q.X == null ||
                Q.Y == null ||
                Q.X.Length != Q.Y.Length)
            {
                hasErrors = true;
            }

            if (!hasErrors)
            {
                if (Curve.IsExplicit)
                {
                    // Explicit curves require D length to match Curve.Order
                    hasErrors = (D != null && (D.Length != Curve.Order.Length));
                }
                else if (Curve.IsNamed)
                {
                    // Named curves require D length to match Q.X and Q.Y
                    hasErrors = (D != null && (D.Length != Q.X.Length));
                }
            }

            if (hasErrors)
            {
                throw new CryptographicException(SR.Cryptography_InvalidCurveKeyParameters);
            }

            Curve.Validate();
        }

        internal static unsafe ECParameters FromECPrivateKey(ReadOnlySpan<byte> key, out int bytesRead)
        {
            fixed (byte* ptr = &MemoryMarshal.GetReference(key))
            {
                using (MemoryManager<byte> manager = new PointerMemoryManager<byte>(ptr, key.Length))
                {
                    ECPrivateKey parsedKey =
                        AsnSerializer.Deserialize<ECPrivateKey>(manager.Memory, AsnEncodingRules.BER, out bytesRead);

                    ECParameters ret;
                    AlgorithmIdentifierAsn algId = default;
                    FromECPrivateKey(parsedKey, algId, out ret);
                    return ret;
                }
            }
        }

        internal static void FromECPrivateKey(
            in ECPrivateKey key,
            in AlgorithmIdentifierAsn algId,
            out ECParameters ret)
        {
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
                domainParameters = AsnSerializer.Deserialize<ECDomainParameters>(
                    algId.Parameters.Value,
                    AsnEncodingRules.DER);
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
            in ReadOnlyMemory<byte> key,
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

            ECDomainParameters domainParameters = AsnSerializer.Deserialize<ECDomainParameters>(
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
                byte[] verify = ArrayPool<byte>.Shared.Rent(algIdParameters.Length);

                try
                {
                    // X.509 SubjectPublicKeyInfo specifies DER encoding.
                    // RFC 5915 specifies DER encoding for EC Private Keys.
                    // So we can compare as DER.
                    using (AsnWriter writer = AsnSerializer.Serialize(keyParameters.Value, AsnEncodingRules.DER))
                    {
                        if (!writer.TryEncode(verify, out int written) ||
                            written != algIdParameters.Length ||
                            !algIdParameters.SequenceEqual(verify.AsSpan(0, written)))
                        {
                            throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                        }
                    }
                }
                finally
                {
                    // verify contains public information and does not need to be cleared.
                    ArrayPool<byte>.Shared.Return(verify);
                }
            }
        }

        private static ECCurve GetCurve(in ECDomainParameters domainParameters)
        {
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

        internal AsnWriter WriteSubjectPublicKeyInfo()
        {
            Validate();

            if (!Curve.IsNamed)
            {
                throw new CryptographicException(SR.Cryptography_ECC_NamedCurvesOnly);
            }

            // Since the public key format for EC keys is not ASN.1,
            // write the SPKI structure manually.

            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);

            // SubjectPublicKeyInfo
            writer.PushSequence();

            // algorithm
            WriteAlgorithmIdentifier(writer);

            // subjectPublicKey
            WriteUncompressedPublicKey(writer);

            writer.PopSequence();
            return writer;
        }

        private AsnWriter WriteAlgorithmIdentifier()
        {
            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
            WriteAlgorithmIdentifier(writer);
            return writer;
        }

        private void WriteAlgorithmIdentifier(AsnWriter writer)
        {
            writer.PushSequence();

            writer.WriteObjectIdentifier(Oids.EcPublicKey);
            WriteEcParameters(writer);

            writer.PopSequence();
        }

        internal AsnWriter WritePkcs8PrivateKey()
        {
            Validate();

            if (D == null)
            {
                throw new CryptographicException(SR.Cryptography_CSP_NoPrivateKey);
            }

            if (!Curve.IsNamed)
            {
                throw new CryptographicException(SR.Cryptography_ECC_NamedCurvesOnly);
            }

            // Don't need the domain parameters because they're contained in the algId.
            using (AsnWriter ecPrivateKey = WriteEcPrivateKey(includeDomainParameters: false))
            using (AsnWriter algorithmIdentifier = WriteAlgorithmIdentifier())
            {
                return KeyFormatHelper.WritePkcs8(algorithmIdentifier, ecPrivateKey);
            }
        }

        private void WriteEcParameters(AsnWriter writer)
        {
            if (Curve.IsNamed)
            {
                Oid oid = Curve.Oid;

                // On Windows the FriendlyName is populated in places where the Value mightn't be.
                if (string.IsNullOrEmpty(oid.Value))
                {
                    oid = Oid.FromFriendlyName(oid.FriendlyName, OidGroup.All);
                }

                writer.WriteObjectIdentifier(oid.Value);
            }
            else
            {
                throw new CryptographicException(SR.Cryptography_ECC_NamedCurvesOnly);
            }
        }

        private void WriteUncompressedPublicKey(AsnWriter writer)
        {
            int publicKeyLength = Q.X.Length * 2 + 1;
            Span<byte> publicKeyBytes = stackalloc byte[0];
            byte[] publicKeyRented = null;

            if (publicKeyLength < 256)
            {
                publicKeyBytes = stackalloc byte[publicKeyLength];
            }
            else
            {
                publicKeyRented = ArrayPool<byte>.Shared.Rent(publicKeyLength);
                publicKeyBytes = publicKeyRented.AsSpan(0, publicKeyLength);
            }

            try
            {
                publicKeyBytes[0] = 0x04;
                Q.X.AsSpan().CopyTo(publicKeyBytes.Slice(1));
                Q.Y.AsSpan().CopyTo(publicKeyBytes.Slice(1 + Q.X.Length));

                writer.WriteBitString(publicKeyBytes);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(publicKeyBytes);

                if (publicKeyRented != null)
                {
                    ArrayPool<byte>.Shared.Return(publicKeyRented);
                }
            }
        }

        internal AsnWriter WriteECPrivateKey()
        {
            return WriteEcPrivateKey(includeDomainParameters: true);
        }

        private AsnWriter WriteEcPrivateKey(bool includeDomainParameters)
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
                writer.WriteOctetString(D);

                // domainParameters
                if (includeDomainParameters)
                {
                    Asn1Tag explicit0 = new Asn1Tag(TagClass.ContextSpecific, 0, isConstructed: true);
                    writer.PushSequence(explicit0);

                    WriteEcParameters(writer);

                    writer.PopSequence(explicit0);
                }

                // publicKey
                {
                    Asn1Tag explicit1 = new Asn1Tag(TagClass.ContextSpecific, 1, isConstructed: true);
                    writer.PushSequence(explicit1);

                    WriteUncompressedPublicKey(writer);

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
