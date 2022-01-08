// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
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
        public static byte[] Sign(byte[] content, ECDsa key, HashAlgorithmName hashAlgorithm, bool isDetached = false)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            byte[] encodedProtectedHeader = CreateEncodedProtectedHeader(KeyType.ECDsa, hashAlgorithm);
            byte[] toBeSigned = CreateToBeSigned(SigStructureCoxtextSign1, encodedProtectedHeader, content);
            byte[] signature = SignWithECDsa(key, toBeSigned, hashAlgorithm);
            return SignCore(encodedProtectedHeader, GetEmptyCborMap(), signature, content, isDetached);
        }

        [UnsupportedOSPlatform("browser")]
        public static byte[] Sign(byte[] content, RSA key, HashAlgorithmName hashAlgorithm, bool isDetached = false)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            byte[] encodedProtectedHeader = CreateEncodedProtectedHeader(KeyType.RSA, hashAlgorithm);
            byte[] toBeSigned = CreateToBeSigned(SigStructureCoxtextSign1, encodedProtectedHeader, content);
            byte[] signature = SignWithRSA(key, toBeSigned, hashAlgorithm);
            return SignCore(encodedProtectedHeader, GetEmptyCborMap(), signature, content, isDetached);
        }

        [UnsupportedOSPlatform("browser")]
        public static byte[] Sign(byte[] content, CoseHeaderMap protectedHeaders, CoseHeaderMap unprotectedHeaders, AsymmetricAlgorithm key, bool isDetached = false)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (protectedHeaders == null)
            {
                throw new ArgumentNullException(nameof(protectedHeaders));
            }

            if (unprotectedHeaders == null)
            {
                throw new ArgumentNullException(nameof(unprotectedHeaders));
            }

            KeyType keyType = key switch
            {
                ECDsa => KeyType.ECDsa,
                RSA => KeyType.RSA,
                _ => throw new CryptographicException(SR.Format(SR.Sign1SignUnsupportedKey, key.GetType()))
            };

            ThrowIfDuplicateLabels(protectedHeaders, unprotectedHeaders);

            ReadOnlyMemory<byte> encodedAlg = GetCoseAlgorithmFromBuckets(protectedHeaders, unprotectedHeaders);

            int? algorithmHeader = DecodeCoseAlgorithmHeader(encodedAlg);
            Debug.Assert(algorithmHeader.HasValue, "Algorithm (alg) is a known header and should have been validated in Set[Encoded]Value()");

            HashAlgorithmName hashAlgorithm = GetHashAlgorithmFromCoseAlgorithmAndKeyType(algorithmHeader.Value, keyType);

            byte[] encodedProtetedHeaders = protectedHeaders.Encode(mustReturnEmptyBstrIfEmpty: true);
            byte[] toBeSigned = CreateToBeSigned(SigStructureCoxtextSign1, encodedProtetedHeaders, content);

            byte[] signature = keyType == KeyType.ECDsa ?
                SignWithECDsa((ECDsa)key, toBeSigned, hashAlgorithm) :
                SignWithRSA((RSA)key, toBeSigned, hashAlgorithm);

            return SignCore(encodedProtetedHeaders, unprotectedHeaders.Encode(), signature, content, isDetached);
        }

        // Validate duplicate labels https://datatracker.ietf.org/doc/html/rfc8152#section-3.
        internal static void ThrowIfDuplicateLabels(CoseHeaderMap @protected, CoseHeaderMap unprotected)
        {
            foreach ((CoseHeaderLabel Label, ReadOnlyMemory<byte>) header in @protected)
            {
                if (unprotected.TryGetEncodedValue(header.Label, out _))
                {
                    throw new CryptographicException(SR.Sign1SignHeaderDuplicateLabels);
                }
            }
        }

        private static byte[] SignCore(ReadOnlySpan<byte> encodedProtectedHeader, ReadOnlySpan<byte> encodedUnprotectedHeader, ReadOnlySpan<byte> signature, ReadOnlySpan<byte> content, bool isDetached)
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
            ReadOnlyMemory<byte> encodedAlg = GetCoseAlgorithmFromBuckets(ProtectedHeader, UnprotectedHeader);
            alg = DecodeCoseAlgorithmHeader(encodedAlg) ?? throw new CryptographicException(SR.Sign1VerifyAlgHeaderWasIncorrect);
            toBeSigned = CreateToBeSigned(SigStructureCoxtextSign1, _protectedHeaderAsBstr, content);
        }

        private static ReadOnlyMemory<byte> GetCoseAlgorithmFromBuckets(CoseHeaderMap protectedHeaders, CoseHeaderMap unprotectedHeaders)
        {
            // https://datatracker.ietf.org/doc/html/rfc8152#section-3.1 alg:
            // This parameter MUST be authenticated where the ability to do so exists.
            // This authentication can be done either by placing the header in the protected header bucket or as part of the externally supplied data.
            // Example of an Algorithm header placed in the unprotected bucket https://github.com/cose-wg/Examples/blob/master/sign1-tests/sign-pass-01.json
            CoseHeaderLabel label = CoseHeaderLabel.Algorithm;
            if (!protectedHeaders.TryGetEncodedValue(label, out ReadOnlyMemory<byte> encodedAlg))
            {
                if (!unprotectedHeaders.TryGetEncodedValue(label, out encodedAlg))
                {
                    throw new CryptographicException(SR.Sign1SignAlgIsRequired);
                }
            }

            return encodedAlg;
        }

        private static int? DecodeCoseAlgorithmHeader(ReadOnlyMemory<byte> encodedAlg)
        {
            var reader = new CborReader(encodedAlg);
            CborReaderState state = reader.PeekState();

            if (state == CborReaderState.NegativeInteger || state == CborReaderState.UnsignedInteger)
            {
                int alg = reader.ReadInt32();
                KnownCoseAlgorithms.ThrowIfNotSupported(alg);
                return reader.BytesRemaining == 0 ? alg : throw new CryptographicException(SR.Sign1VerifyAlgHeaderWasIncorrect);
            }

            if (state == CborReaderState.TextString)
            {
                int alg = KnownCoseAlgorithms.FromString(reader.ReadTextString());
                return reader.BytesRemaining == 0 ? alg : throw new CryptographicException(SR.Sign1VerifyAlgHeaderWasIncorrect);
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
                    KnownHashAlgorithms.SHA256 => KnownCoseAlgorithms.ES256,
                    KnownHashAlgorithms.SHA384 => KnownCoseAlgorithms.ES384,
                    KnownHashAlgorithms.SHA512 => KnownCoseAlgorithms.ES512,
                    _ => throw new CryptographicException(SR.Format(SR.Sign1SignUnsupportedHashAlgorithm, hashAlgorithm.Name))
                },
                _ => hashAlgorithm.Name switch // KeyType.RSA
                {
                    KnownHashAlgorithms.SHA256 => KnownCoseAlgorithms.PS256,
                    KnownHashAlgorithms.SHA384 => KnownCoseAlgorithms.PS384,
                    KnownHashAlgorithms.SHA512 => KnownCoseAlgorithms.PS512,
                    _ => throw new CryptographicException(SR.Format(SR.Sign1SignUnsupportedHashAlgorithm, hashAlgorithm.Name))
                },
            };
    }
}
