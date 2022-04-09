// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Formats.Cbor;
using System.Runtime.Versioning;

namespace System.Security.Cryptography.Cose
{
    public sealed class CoseSign1Message : CoseMessage
    {
        private const string SigStructureCoxtextSign1 = "Signature1";
        private const int Sign1ArrayLegth = 4;
        private byte[]? _toBeSigned;

        internal CoseSign1Message(CoseHeaderMap protectedHeader, CoseHeaderMap unprotectedHeader, byte[]? content, byte[] signature, byte[] protectedHeaderAsBstr)
            : base(protectedHeader, unprotectedHeader, content, signature, protectedHeaderAsBstr) { }

        [UnsupportedOSPlatform("browser")]
        public static byte[] Sign(byte[] content!!, AsymmetricAlgorithm key!!, HashAlgorithmName hashAlgorithm, CoseHeaderMap? protectedHeaders = null, CoseHeaderMap? unprotectedHeaders = null, bool isDetached = false)
            => SignCore(content.AsSpan(), key, hashAlgorithm, GetKeyType(key), protectedHeaders, unprotectedHeaders, isDetached);

        [UnsupportedOSPlatform("browser")]
        public static byte[] Sign(ReadOnlySpan<byte> content, AsymmetricAlgorithm key!!, HashAlgorithmName hashAlgorithm, CoseHeaderMap? protectedHeaders = null, CoseHeaderMap? unprotectedHeaders = null, bool isDetached = false)
            => SignCore(content, key, hashAlgorithm, GetKeyType(key), protectedHeaders, unprotectedHeaders, isDetached);

        [UnsupportedOSPlatform("browser")]
        internal static byte[] SignCore(ReadOnlySpan<byte> content, AsymmetricAlgorithm key, HashAlgorithmName hashAlgorithm, KeyType keyType, CoseHeaderMap? protectedHeaders, CoseHeaderMap? unprotectedHeaders, bool isDetached)
        {
            ValidateBeforeSign(protectedHeaders, unprotectedHeaders, keyType, hashAlgorithm, out int? algHeaderValueToSlip);

            int expectedSize = ComputeEncodedSize(protectedHeaders, unprotectedHeaders, algHeaderValueToSlip, content.Length, isDetached, key.KeySize, keyType);
            byte[] buffer = new byte[expectedSize];

            int bytesWritten = CreateCoseSign1Message(content, buffer, key, hashAlgorithm, protectedHeaders, unprotectedHeaders, isDetached, algHeaderValueToSlip, keyType);
            Debug.Assert(expectedSize == bytesWritten);

            return buffer;
        }

        [UnsupportedOSPlatform("browser")]
        public static bool TrySign(
            ReadOnlySpan<byte> content,
            Span<byte> destination,
            AsymmetricAlgorithm key!!,
            HashAlgorithmName hashAlgorithm,
            out int bytesWritten,
            CoseHeaderMap? protectedHeaders = null,
            CoseHeaderMap? unprotectedHeaders = null,
            bool isDetached = false)
        {
            KeyType keyType = GetKeyType(key);
            ValidateBeforeSign(protectedHeaders, unprotectedHeaders, keyType, hashAlgorithm, out int? algHeaderValueToSlip);

            int expectedSize = ComputeEncodedSize(protectedHeaders, unprotectedHeaders, algHeaderValueToSlip, content.Length, isDetached, key.KeySize, keyType);
            if (expectedSize > destination.Length)
            {
                bytesWritten = 0;
                return false;
            }

            bytesWritten = CreateCoseSign1Message(content, destination, key, hashAlgorithm, protectedHeaders, unprotectedHeaders, isDetached, algHeaderValueToSlip, keyType);
            Debug.Assert(expectedSize == bytesWritten);

            return true;
        }

        internal static KeyType GetKeyType(AsymmetricAlgorithm key)
        {
            return key switch
            {
                ECDsa => KeyType.ECDsa,
                RSA => KeyType.RSA,
                _ => throw new CryptographicException(SR.Format(SR.Sign1UnsupportedKey, key.GetType()))
            };
        }

        internal static void ValidateBeforeSign(CoseHeaderMap? protectedHeaders, CoseHeaderMap? unprotectedHeaders, KeyType keyType, HashAlgorithmName hashAlgorithm, out int? algHeaderValueToSlip)
        {
            ThrowIfDuplicateLabels(protectedHeaders, unprotectedHeaders);
            algHeaderValueToSlip = ValidateOrSlipAlgorithmHeader(protectedHeaders, unprotectedHeaders, keyType, hashAlgorithm);
        }

        [UnsupportedOSPlatform("browser")]
        internal static int CreateCoseSign1Message(ReadOnlySpan<byte> content, Span<byte> buffer, AsymmetricAlgorithm key, HashAlgorithmName hashAlgorithm, CoseHeaderMap? protectedHeaders, CoseHeaderMap? unprotectedHeaders, bool isDetached, int? algHeaderValueToSlip, KeyType keyType)
        {
            var writer = new CborWriter();
            writer.WriteTag(Sign1Tag);
            writer.WriteStartArray(Sign1ArrayLegth);

            int protectedMapBytesWritten = CoseHeaderMap.Encode(protectedHeaders, buffer, true, algHeaderValueToSlip);
            ReadOnlySpan<byte> encodedProtectedHeaders = buffer.Slice(0, protectedMapBytesWritten);
            // We're going to use the encoded protected headers again after this step (for the toBeSigned construction),
            // so don't overwrite them yet.
            writer.WriteByteString(encodedProtectedHeaders);

            int unprotectedMapBytesWritten = CoseHeaderMap.Encode(unprotectedHeaders, buffer.Slice(protectedMapBytesWritten));
            ReadOnlySpan<byte> encodedUnprotectedHeaders = buffer.Slice(protectedMapBytesWritten, unprotectedMapBytesWritten);
            writer.WriteEncodedValue(encodedUnprotectedHeaders);

            if (isDetached)
            {
                writer.WriteNull();
            }
            else
            {
                writer.WriteByteString(content);
            }

            int expectedToBeSignedSize = ComputeToBeSignedEncodedSize(SigStructureCoxtextSign1, encodedProtectedHeaders, content);

            Span<byte> toBeSignedBuffer = buffer;
            byte[]? rentedToBeSignedBuffer = null;
            int signatureBytesWritten;

            // It is possible for toBeSigned to be bigger than the COSE message length that we used to determine the size of our buffer.
            // we rent a bigger buffer if that's the case.
            if (buffer.Length < expectedToBeSignedSize)
            {
                rentedToBeSignedBuffer = ArrayPool<byte>.Shared.Rent(expectedToBeSignedSize);
                toBeSignedBuffer = rentedToBeSignedBuffer;
            }

            try
            {
                int toBeSignedBytesWritten = CreateToBeSigned(SigStructureCoxtextSign1, encodedProtectedHeaders, content, toBeSignedBuffer);
                ReadOnlySpan<byte> encodedToBeSigned = buffer.Slice(0, toBeSignedBytesWritten);

                if (keyType == KeyType.ECDsa)
                {
                    signatureBytesWritten = SignWithECDsa((ECDsa)key, encodedToBeSigned, hashAlgorithm, buffer);
                }
                else
                {
                    Debug.Assert(keyType == KeyType.RSA);
                    signatureBytesWritten = SignWithRSA((RSA)key, encodedToBeSigned, hashAlgorithm, buffer);
                }
            }
            finally
            {
                if (rentedToBeSignedBuffer != null)
                {
                    ArrayPool<byte>.Shared.Return(rentedToBeSignedBuffer, clearArray: true);
                }
            }

            writer.WriteByteString(buffer.Slice(0, signatureBytesWritten));

            writer.WriteEndArray();

            return writer.Encode(buffer);
        }

        [UnsupportedOSPlatform("browser")]
        private static int SignWithECDsa(ECDsa key, ReadOnlySpan<byte> data, HashAlgorithmName hashAlgorithm, Span<byte> destination)
        {
#if NETSTANDARD2_0 || NETFRAMEWORK
            byte[] signature = key.SignData(data.ToArray(), hashAlgorithm);
            signature.CopyTo(destination);
            return signature.Length;
#else
            if (!key.TrySignData(data, destination, hashAlgorithm, out int bytesWritten))
            {
                Debug.Fail("TrySignData failed with a pre-calculated destination");
                throw new CryptographicException();
            }

            return bytesWritten;
#endif
        }

        [UnsupportedOSPlatform("browser")]
        private static int SignWithRSA(RSA key, ReadOnlySpan<byte> data, HashAlgorithmName hashAlgorithm, Span<byte> destination)
        {
#if NETSTANDARD2_0 || NETFRAMEWORK
            byte[] signature = key.SignData(data.ToArray(), hashAlgorithm, RSASignaturePadding.Pss);
            signature.CopyTo(destination);
            return signature.Length;
#else
            if (!key.TrySignData(data, destination, hashAlgorithm, RSASignaturePadding.Pss, out int bytesWritten))
            {
                Debug.Fail("TrySignData failed with a pre-calculated destination");
                throw new CryptographicException();
            }

            return bytesWritten;
#endif
        }

        [UnsupportedOSPlatform("browser")]
        public bool Verify(AsymmetricAlgorithm key!!)
        {
            if (_content == null)
            {
                throw new CryptographicException(SR.Sign1VerifyContentWasDetached);
            }

            return VerifyCore(key, _content);
        }

        [UnsupportedOSPlatform("browser")]
        public bool Verify(AsymmetricAlgorithm key!!, byte[] content!!)
        {
            if (_content != null)
            {
                throw new CryptographicException(SR.Sign1VerifyContentWasEmbedded);
            }

            return VerifyCore(key, content);
        }

        [UnsupportedOSPlatform("browser")]
        public bool Verify(AsymmetricAlgorithm key, ReadOnlySpan<byte> content)
        {
            if (_content != null)
            {
                throw new CryptographicException(SR.Sign1VerifyContentWasEmbedded);
            }

            return VerifyCore(key, content);
        }

        [UnsupportedOSPlatform("browser")]
        private bool VerifyCore(AsymmetricAlgorithm key, ReadOnlySpan<byte> content)
        {
            if (key is ECDsa ecdsa)
            {
                return VerifyECDsa(ecdsa, content);
            }
            else if (key is RSA rsa)
            {
                return VerifyRSA(rsa, content);
            }
            else
            {
                throw new CryptographicException(SR.Format(SR.Sign1UnsupportedKey, key.GetType()));
            }
        }

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

            if (_content == null)
            {
                // Never cache toBeSigned if the message has detached content since the passed-in content can be different in each call.
                toBeSigned = CreateToBeSignedForVerify(content);
            }
            else if (_toBeSigned == null)
            {
                toBeSigned = _toBeSigned = CreateToBeSignedForVerify(content);
            }
            else
            {
                toBeSigned = _toBeSigned;
            }

            byte[] CreateToBeSignedForVerify(ReadOnlySpan<byte> content)
            {
                byte[] rentedbuffer = ArrayPool<byte>.Shared.Rent(ComputeToBeSignedEncodedSize(SigStructureCoxtextSign1, _protectedHeaderAsBstr, content));
                try
                {
                    Span<byte> buffer = rentedbuffer;
                    int bytesWritten = CreateToBeSigned(SigStructureCoxtextSign1, _protectedHeaderAsBstr, content, buffer);
                    return buffer.Slice(0, bytesWritten).ToArray();
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(rentedbuffer, clearArray: true);
                }
            }
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

        private static int ComputeEncodedSize(CoseHeaderMap? protectedHeaders, CoseHeaderMap? unprotectedHeaders, int? algHeaderValueToSlip, int contentLength, bool isDetached, int keySize, KeyType keyType)
        {
            // tag + array(4) + encoded protected header map + unprotected header map + content + signature.
            const int SizeOfTag = 1;
            const int SizeOfNull = 1;

            int encodedSize = SizeOfTag + SizeOfArrayOfFour +
                CoseHelpers.GetByteStringEncodedSize(CoseHeaderMap.ComputeEncodedSize(protectedHeaders, algHeaderValueToSlip)) +
                CoseHeaderMap.ComputeEncodedSize(unprotectedHeaders);

            if (isDetached)
            {
                encodedSize += SizeOfNull;
            }
            else
            {
                encodedSize += CoseHelpers.GetByteStringEncodedSize(contentLength);
            }

            int signatureSize;
            if (keyType == KeyType.ECDsa)
            {
                signatureSize = 2 * ((keySize + 7) / 8);
            }
            else // RSA
            {
                Debug.Assert(keyType == KeyType.RSA);
                signatureSize = (keySize + 7) / 8;
            }

            encodedSize += CoseHelpers.GetByteStringEncodedSize(signatureSize);

            return encodedSize;
        }
    }
}
