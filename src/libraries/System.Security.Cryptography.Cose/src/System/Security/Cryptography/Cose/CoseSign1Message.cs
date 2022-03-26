// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Formats.Cbor;
using System.Runtime.Versioning;

namespace System.Security.Cryptography.Cose
{
    public sealed class CoseSign1Message : CoseMessage
    {
        private const string SigStructureCoxtextSign1 = "Signature1";
        private const int Sign1ArrayLegth = 4;

        internal CoseSign1Message(CoseHeaderMap protectedHeader, CoseHeaderMap unprotectedHeader, byte[]? content, byte[] signature, byte[] protectedHeaderAsBstr)
            : base(protectedHeader, unprotectedHeader, content, signature, protectedHeaderAsBstr) { }

        [UnsupportedOSPlatform("browser")]
        public static byte[] Sign(byte[] content!!, ECDsa key!!, HashAlgorithmName hashAlgorithm, bool isDetached = false)
        {
            byte[] encodedProtectedHeader = CreateEncodedProtectedHeader(KeyType.ECDsa, hashAlgorithm);
            byte[] toBeSigned = CreateToBeSigned(SigStructureCoxtextSign1, encodedProtectedHeader, content);
            byte[] signature = SignWithECDsa(key, toBeSigned, hashAlgorithm);
            return SignCore(encodedProtectedHeader, GetEmptyCborMap(), signature, content, isDetached);
        }

        [UnsupportedOSPlatform("browser")]
        public static byte[] Sign(byte[] content!!, RSA key!!, HashAlgorithmName hashAlgorithm, bool isDetached = false)
        {
            byte[] encodedProtectedHeader = CreateEncodedProtectedHeader(KeyType.RSA, hashAlgorithm);
            byte[] toBeSigned = CreateToBeSigned(SigStructureCoxtextSign1, encodedProtectedHeader, content);
            byte[] signature = SignWithRSA(key, toBeSigned, hashAlgorithm);
            return SignCore(encodedProtectedHeader, GetEmptyCborMap(), signature, content, isDetached);
        }

        [UnsupportedOSPlatform("browser")]
        public static byte[] Sign(byte[] content!!, AsymmetricAlgorithm key!!, HashAlgorithmName hashAlgorithm, CoseHeaderMap? protectedHeaders = null, CoseHeaderMap? unprotectedHeaders = null, bool isDetached = false)
        {
            KeyType keyType = key switch
            {
                ECDsa => KeyType.ECDsa,
                RSA => KeyType.RSA,
                _ => throw new CryptographicException(SR.Format(SR.Sign1SignUnsupportedKey, key.GetType()))
            };

            ThrowIfDuplicateLabels(protectedHeaders, unprotectedHeaders);

            int? algHeaderValueToSlip = ValidateOrSlipAlgorithmHeader(protectedHeaders, unprotectedHeaders, keyType, hashAlgorithm);

            byte[] encodedProtetedHeaders = CoseHeaderMap.Encode(protectedHeaders, mustReturnEmptyBstrIfEmpty: true, algHeaderValueToSlip);
            byte[] toBeSigned = CreateToBeSigned(SigStructureCoxtextSign1, encodedProtetedHeaders, content);

            byte[] signature;
            if (keyType == KeyType.ECDsa)
            {
                signature = SignWithECDsa((ECDsa)key, toBeSigned, hashAlgorithm);
            }
            else
            {
                signature = SignWithRSA((RSA)key, toBeSigned, hashAlgorithm);
            }

            return SignCore(encodedProtetedHeaders, CoseHeaderMap.Encode(unprotectedHeaders), signature, content, isDetached);
        }

        private static byte[] SignCore(
            ReadOnlySpan<byte> encodedProtectedHeader,
            ReadOnlySpan<byte> encodedUnprotectedHeader,
            ReadOnlySpan<byte> signature,
            ReadOnlySpan<byte> content,
            bool isDetached)
        {
            var writer = new CborWriter();
            writer.WriteTag(Sign1Tag);
            writer.WriteStartArray(Sign1ArrayLegth);
            writer.WriteByteString(encodedProtectedHeader);
            writer.WriteEncodedValue(encodedUnprotectedHeader);

            if (isDetached)
            {
                writer.WriteNull();
            }
            else
            {
                writer.WriteByteString(content);
            }

            writer.WriteByteString(signature);
            writer.WriteEndArray();

            return writer.Encode();
        }

        [UnsupportedOSPlatform("browser")]
        private static byte[] SignWithECDsa(ECDsa key, byte[] data, HashAlgorithmName hashAlgorithm)
            => key.SignData(data, hashAlgorithm);

        [UnsupportedOSPlatform("browser")]
        private static byte[] SignWithRSA(RSA key, byte[] data, HashAlgorithmName hashAlgorithm)
            => key.SignData(data, hashAlgorithm, RSASignaturePadding.Pss);

        internal static byte[] CreateEncodedProtectedHeader(KeyType algType, HashAlgorithmName hashAlgorithm)
        {
            var writer = new CborWriter();
            writer.WriteStartMap(1);
            writer.WriteInt32(KnownHeaders.Alg);
            writer.WriteInt32(GetCoseAlgorithmHeaderFromKeyTypeAndHashAlgorithm(algType, hashAlgorithm));
            writer.WriteEndMap();
            return writer.Encode();
        }

        private static byte[] GetEmptyCborMap()
        {
            var writer = new CborWriter();
            writer.WriteStartMap(0);
            writer.WriteEndMap();
            return writer.Encode();
        }

        [UnsupportedOSPlatform("browser")]
        public bool Verify(ECDsa key) => VerifyECDsa(key, _content ?? throw new CryptographicException(SR.Sign1VerifyContentWasDetached));

        [UnsupportedOSPlatform("browser")]
        public bool Verify(RSA key) => VerifyRSA(key, _content ?? throw new CryptographicException(SR.Sign1VerifyContentWasDetached));

        [UnsupportedOSPlatform("browser")]
        public bool Verify(ECDsa key, ReadOnlySpan<byte> content) => VerifyECDsa(key, _content == null ? content : throw new CryptographicException(SR.Sign1VerifyContentWasEmbedded));

        [UnsupportedOSPlatform("browser")]
        public bool Verify(RSA key, ReadOnlySpan<byte> content) =>  VerifyRSA(key, _content == null ? content : throw new CryptographicException(SR.Sign1VerifyContentWasEmbedded));

        [UnsupportedOSPlatform("browser")]
        private bool VerifyECDsa(ECDsa key, ReadOnlySpan<byte> content)
        {
            PrepareForVerify(content, out int alg, out byte[] toBeSigned);
            HashAlgorithmName hashAlgorithm = GetHashAlgorithmFromCoseAlgorithmAndKeyType(alg, KeyType.ECDsa);
            return key.VerifyData(toBeSigned, _signature, hashAlgorithm);
        }

        [UnsupportedOSPlatform("browser")]
        private bool VerifyRSA(RSA key, ReadOnlySpan<byte> content)
        {
            PrepareForVerify(content, out int alg, out byte[] toBeSigned);
            HashAlgorithmName hashAlgorithm = GetHashAlgorithmFromCoseAlgorithmAndKeyType(alg, KeyType.RSA);
            return key.VerifyData(toBeSigned, _signature, hashAlgorithm, RSASignaturePadding.Pss);
        }

        private void PrepareForVerify(ReadOnlySpan<byte> content, out int alg, out byte[] toBeSigned)
        {
            ThrowIfUnsupportedHeaders();

            ReadOnlyMemory<byte> encodedAlg = GetCoseAlgorithmFromProtectedHeaders(ProtectedHeaders);

            int? nullableAlg = DecodeCoseAlgorithmHeader(encodedAlg);
            if (nullableAlg == null)
            {
                throw new CryptographicException(SR.Sign1VerifyAlgHeaderWasIncorrect);
            }

            alg = nullableAlg.Value;
            toBeSigned = CreateToBeSigned(SigStructureCoxtextSign1, _protectedHeaderAsBstr, content);
        }

        private static ReadOnlyMemory<byte> GetCoseAlgorithmFromProtectedHeaders(CoseHeaderMap protectedHeaders)
        {
            // https://datatracker.ietf.org/doc/html/rfc8152#section-3.1 alg:
            // This parameter MUST be authenticated where the ability to do so exists.
            // This authentication can be done either by placing the header in the protected header bucket or as part of the externally supplied data.
            if (!protectedHeaders.TryGetEncodedValue(CoseHeaderLabel.Algorithm, out ReadOnlyMemory<byte> encodedAlg))
            {
                throw new CryptographicException(SR.Sign1VerifyAlgIsRequired);
            }

            return encodedAlg;
        }

        // If we Validate: The caller did specify a COSE Algorithm, we will make sure it matches the specified key and hash algorithm.
        // If we Slip: The caller did not specify a COSE Algorithm, we will write the header for them, rather than throw.
        private static int? ValidateOrSlipAlgorithmHeader(
            CoseHeaderMap? protectedHeaders,
            CoseHeaderMap? unprotectedHeaders,
            KeyType keyType,
            HashAlgorithmName hashAlgorithm)
        {
            int algHeaderValue = GetCoseAlgorithmHeaderFromKeyTypeAndHashAlgorithm(keyType, hashAlgorithm);

            if (protectedHeaders != null && protectedHeaders.TryGetEncodedValue(CoseHeaderLabel.Algorithm, out ReadOnlyMemory<byte> encodedAlg))
            {
                ValidateAlgorithmHeader(encodedAlg, algHeaderValue, keyType, hashAlgorithm);
                return null;
            }

            if (unprotectedHeaders != null && unprotectedHeaders.TryGetEncodedValue(CoseHeaderLabel.Algorithm, out _))
            {
                throw new CryptographicException(SR.Sign1SignAlgMustBeProtected);
            }

            return algHeaderValue;

            static void ValidateAlgorithmHeader(ReadOnlyMemory<byte> encodedAlg, int expectedAlg, KeyType keyType, HashAlgorithmName hashAlgorithm)
            {
                int? alg = DecodeCoseAlgorithmHeader(encodedAlg);
                Debug.Assert(alg.HasValue, "Algorithm (alg) is a known header and should have been validated in Set[Encoded]Value()");

                if (expectedAlg != alg.Value)
                {
                    throw new CryptographicException(SR.Format(SR.Sign1SignCoseAlgorithDoesNotMatchSpecifiedKeyAndHashAlgorithm, alg.Value, keyType.ToString(), hashAlgorithm.Name));
                }
            }
        }

        private static int? DecodeCoseAlgorithmHeader(ReadOnlyMemory<byte> encodedAlg)
        {
            var reader = new CborReader(encodedAlg);
            CborReaderState state = reader.PeekState();

            if (state == CborReaderState.UnsignedInteger)
            {
                KnownCoseAlgorithms.ThrowUnsignedIntegerNotSupported(reader.ReadUInt64());
            }
            else if (state == CborReaderState.NegativeInteger)
            {
                ulong cborNegativeIntRepresentation = reader.ReadCborNegativeIntegerRepresentation();

                if (cborNegativeIntRepresentation > long.MaxValue)
                {
                    KnownCoseAlgorithms.ThrowCborNegativeIntegerNotSupported(cborNegativeIntRepresentation);
                }

                long alg = checked(-1L - (long)cborNegativeIntRepresentation);
                KnownCoseAlgorithms.ThrowIfNotSupported(alg);

                if (reader.BytesRemaining != 0)
                {
                    throw new CryptographicException(SR.Sign1VerifyAlgHeaderWasIncorrect);
                }

                return (int)alg;
            }

            if (state == CborReaderState.TextString)
            {
                int alg = KnownCoseAlgorithms.FromString(reader.ReadTextString());

                if (reader.BytesRemaining != 0)
                {
                    throw new CryptographicException(SR.Sign1VerifyAlgHeaderWasIncorrect);
                }

                return alg;
            }

            return null;
        }

        private static HashAlgorithmName GetHashAlgorithmFromCoseAlgorithmAndKeyType(int algorithm, KeyType keyType)
        {
            if (keyType == KeyType.ECDsa)
            {
                return algorithm switch
                {
                    KnownCoseAlgorithms.ES256 => HashAlgorithmName.SHA256,
                    KnownCoseAlgorithms.ES384 => HashAlgorithmName.SHA384,
                    KnownCoseAlgorithms.ES512 => HashAlgorithmName.SHA512,
                    _ => throw new CryptographicException(SR.Format(SR.Sign1AlgDoesNotMatchWithTheOnesSupportedByTypeOfKey, algorithm, typeof(ECDsa)))
                };
            }
            else
            {
                Debug.Assert(keyType == KeyType.RSA);
                return algorithm switch
                {
                    KnownCoseAlgorithms.PS256 => HashAlgorithmName.SHA256,
                    KnownCoseAlgorithms.PS384 => HashAlgorithmName.SHA384,
                    KnownCoseAlgorithms.PS512 => HashAlgorithmName.SHA512,
                    _ => throw new CryptographicException(SR.Format(SR.Sign1AlgDoesNotMatchWithTheOnesSupportedByTypeOfKey, algorithm, typeof(RSA)))
                };
            }
        }

        private static int GetCoseAlgorithmHeaderFromKeyTypeAndHashAlgorithm(KeyType keyType, HashAlgorithmName hashAlgorithm)
            => keyType switch
            {
                KeyType.ECDsa => hashAlgorithm.Name switch
                {
                    nameof(HashAlgorithmName.SHA256) => KnownCoseAlgorithms.ES256,
                    nameof(HashAlgorithmName.SHA384) => KnownCoseAlgorithms.ES384,
                    nameof(HashAlgorithmName.SHA512) => KnownCoseAlgorithms.ES512,
                    _ => throw new CryptographicException(SR.Format(SR.Sign1SignUnsupportedHashAlgorithm, hashAlgorithm.Name))
                },
                _ => hashAlgorithm.Name switch // KeyType.RSA
                {
                    nameof(HashAlgorithmName.SHA256) => KnownCoseAlgorithms.PS256,
                    nameof(HashAlgorithmName.SHA384) => KnownCoseAlgorithms.PS384,
                    nameof(HashAlgorithmName.SHA512) => KnownCoseAlgorithms.PS512,
                    _ => throw new CryptographicException(SR.Format(SR.Sign1SignUnsupportedHashAlgorithm, hashAlgorithm.Name))
                },
            };

        private void ThrowIfUnsupportedHeaders()
        {
            if (ProtectedHeaders.TryGetEncodedValue(CoseHeaderLabel.Critical, out _) ||
                ProtectedHeaders.TryGetEncodedValue(CoseHeaderLabel.CounterSignature, out _))
            {
                throw new NotSupportedException(SR.Sign1VerifyCriticalAndCounterSignNotSupported);
            }

            if (UnprotectedHeaders.TryGetEncodedValue(CoseHeaderLabel.Critical, out _) ||
                UnprotectedHeaders.TryGetEncodedValue(CoseHeaderLabel.CounterSignature, out _))
            {
                throw new NotSupportedException(SR.Sign1VerifyCriticalAndCounterSignNotSupported);
            }
        }
    }
}
