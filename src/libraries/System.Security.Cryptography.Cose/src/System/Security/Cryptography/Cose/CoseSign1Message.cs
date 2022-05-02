// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Formats.Cbor;
using System.IO;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace System.Security.Cryptography.Cose
{
    public sealed class CoseSign1Message : CoseMessage
    {
        private const string SigStructureCoxtextSign1 = "Signature1";
        private const int Sign1ArrayLegth = 4;
        private byte[]? _toBeSignedHash;

        internal CoseSign1Message(CoseHeaderMap protectedHeader, CoseHeaderMap unprotectedHeader, byte[]? content, byte[] signature, byte[] protectedHeaderAsBstr)
            : base(protectedHeader, unprotectedHeader, content, signature, protectedHeaderAsBstr) { }

        [UnsupportedOSPlatform("browser")]
        public static byte[] Sign(byte[] content!!, AsymmetricAlgorithm key!!, HashAlgorithmName hashAlgorithm, CoseHeaderMap? protectedHeaders = null, CoseHeaderMap? unprotectedHeaders = null, bool isDetached = false)
            => SignCore(content.AsSpan(), null, key, hashAlgorithm, GetKeyType(key), protectedHeaders, unprotectedHeaders, isDetached);

        [UnsupportedOSPlatform("browser")]
        public static byte[] Sign(ReadOnlySpan<byte> content, AsymmetricAlgorithm key!!, HashAlgorithmName hashAlgorithm, CoseHeaderMap? protectedHeaders = null, CoseHeaderMap? unprotectedHeaders = null, bool isDetached = false)
            => SignCore(content, null, key, hashAlgorithm, GetKeyType(key), protectedHeaders, unprotectedHeaders, isDetached);

        [UnsupportedOSPlatform("browser")]
        public static byte[] Sign(Stream detachedContent!!, AsymmetricAlgorithm key!!, HashAlgorithmName hashAlgorithm, CoseHeaderMap? protectedHeaders = null, CoseHeaderMap? unprotectedHeaders = null)
        {
            if (!detachedContent.CanRead)
            {
                throw new ArgumentException(SR.Sign1ArgumentStreamNotReadable, nameof(detachedContent));
            }

            if (!detachedContent.CanSeek)
            {
                throw new ArgumentException(SR.Sign1ArgumentStreamNotSeekable, nameof(detachedContent));
            }

            return SignCore(default, detachedContent, key, hashAlgorithm, GetKeyType(key), protectedHeaders, unprotectedHeaders, isDetached: true);
        }

        [UnsupportedOSPlatform("browser")]
        internal static byte[] SignCore(
            ReadOnlySpan<byte> contentBytes,
            Stream? contentStream,
            AsymmetricAlgorithm key,
            HashAlgorithmName hashAlgorithm,
            KeyType keyType,
            CoseHeaderMap? protectedHeaders,
            CoseHeaderMap? unprotectedHeaders,
            bool isDetached)
        {
            Debug.Assert(contentStream == null || (isDetached == true && contentBytes.Length == 0));

            ValidateBeforeSign(protectedHeaders, unprotectedHeaders, keyType, hashAlgorithm, out int? algHeaderValueToSlip);

            int contentLength;
            if (contentStream != null)
            {
                contentLength = 0;
            }
            else
            {
                contentLength = contentBytes.Length;
            }

            int expectedSize = ComputeEncodedSize(protectedHeaders, unprotectedHeaders, algHeaderValueToSlip, contentLength, isDetached, key.KeySize, keyType);
            var buffer = new byte[expectedSize];

            int bytesWritten = CreateCoseSign1Message(contentBytes, contentStream, buffer, key, hashAlgorithm, protectedHeaders, unprotectedHeaders, isDetached, algHeaderValueToSlip, keyType);
            Debug.Assert(expectedSize == bytesWritten);

            return buffer;
        }

        [UnsupportedOSPlatform("browser")]
        public static Task<byte[]> SignAsync(
            Stream detachedContent!!,
            AsymmetricAlgorithm key!!,
            HashAlgorithmName hashAlgorithm,
            CoseHeaderMap? protectedHeaders = null,
            CoseHeaderMap? unprotectedHeaders = null,
            CancellationToken cancellationToken = default)
        {
            if (!detachedContent.CanRead)
            {
                throw new ArgumentException(SR.Sign1ArgumentStreamNotReadable, nameof(detachedContent));
            }

            if (!detachedContent.CanSeek)
            {
                throw new ArgumentException(SR.Sign1ArgumentStreamNotSeekable, nameof(detachedContent));
            }

            KeyType keyType = GetKeyType(key);
            ValidateBeforeSign(protectedHeaders, unprotectedHeaders, keyType, hashAlgorithm, out int? algHeaderValueToSlip);

            int expectedSize = ComputeEncodedSize(protectedHeaders, unprotectedHeaders, algHeaderValueToSlip, contentLength: 0, isDetached: true, key.KeySize, keyType);
            return SignAsyncCore(expectedSize, detachedContent, key, hashAlgorithm, keyType, protectedHeaders, unprotectedHeaders, cancellationToken, algHeaderValueToSlip);
        }

        [UnsupportedOSPlatform("browser")]
        private static async Task<byte[]> SignAsyncCore(
            int expectedSize,
            Stream content,
            AsymmetricAlgorithm key,
            HashAlgorithmName hashAlgorithm,
            KeyType keyType,
            CoseHeaderMap? protectedHeaders,
            CoseHeaderMap? unprotectedHeaders,
            CancellationToken cancellationToken,
            int? algHeaderValueToSlip)
        {
            byte[] buffer = new byte[expectedSize];
            int bytesWritten = await CreateCoseSign1MessageAsync(content, buffer, key, hashAlgorithm, protectedHeaders, unprotectedHeaders, cancellationToken, algHeaderValueToSlip, keyType).ConfigureAwait(false);

            Debug.Assert(buffer.Length == bytesWritten);
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

            bytesWritten = CreateCoseSign1Message(content, null, destination, key, hashAlgorithm, protectedHeaders, unprotectedHeaders, isDetached, algHeaderValueToSlip, keyType);
            Debug.Assert(expectedSize == bytesWritten);

            return true;
        }

        internal static void ValidateBeforeSign(CoseHeaderMap? protectedHeaders, CoseHeaderMap? unprotectedHeaders, KeyType keyType, HashAlgorithmName hashAlgorithm, out int? algHeaderValueToSlip)
        {
            ThrowIfDuplicateLabels(protectedHeaders, unprotectedHeaders);
            algHeaderValueToSlip = ValidateOrSlipAlgorithmHeader(protectedHeaders, unprotectedHeaders, keyType, hashAlgorithm);
        }

        [UnsupportedOSPlatform("browser")]
        private static int CreateCoseSign1Message(
            ReadOnlySpan<byte> contentBytes,
            Stream? contentStream,
            Span<byte> buffer,
            AsymmetricAlgorithm key,
            HashAlgorithmName hashAlgorithm,
            CoseHeaderMap? protectedHeaders,
            CoseHeaderMap? unprotectedHeaders,
            bool isDetached,
            int? algHeaderValueToSlip,
            KeyType keyType)
        {
            var writer = new CborWriter();
            writer.WriteTag(Sign1Tag);
            writer.WriteStartArray(Sign1ArrayLegth);

            int protectedMapBytesWritten = WriteHeaderMap(buffer, writer, protectedHeaders, isProtected: true, algHeaderValueToSlip);
            // We're going to use the encoded protected headers again after this step (for the toBeSigned construction),
            // so don't overwrite them yet.
            WriteHeaderMap(buffer.Slice(protectedMapBytesWritten), writer, unprotectedHeaders, isProtected: false, null);

            if (contentStream != null)
            {
                WriteContent(writer, default, isDetached: true);
            }
            else
            {
                WriteContent(writer, contentBytes, isDetached);
            }

            using (IncrementalHash hasher = IncrementalHash.CreateHash(hashAlgorithm))
            {
                AppendToBeSigned(buffer, hasher, SigStructureCoxtextSign1, buffer.Slice(0, protectedMapBytesWritten), contentBytes, contentStream, hashAlgorithm);
                WriteSignature(buffer, hasher, writer, key, keyType, hashAlgorithm);
            }

            writer.WriteEndArray();
            return writer.Encode(buffer);
        }

        [UnsupportedOSPlatform("browser")]
        private static async Task<int> CreateCoseSign1MessageAsync(
            Stream content,
            byte[] buffer,
            AsymmetricAlgorithm key,
            HashAlgorithmName hashAlgorithm,
            CoseHeaderMap? protectedHeaders,
            CoseHeaderMap? unprotectedHeaders,
            CancellationToken cancellationToken,
            int? algHeaderValueToSlip,
            KeyType keyType)
        {
            var writer = new CborWriter();
            writer.WriteTag(Sign1Tag);
            writer.WriteStartArray(Sign1ArrayLegth);

            int protectedMapBytesWritten = WriteHeaderMap(buffer, writer, protectedHeaders, isProtected: true, algHeaderValueToSlip);
            // We're going to use the encoded protected headers again after this step (for the toBeSigned construction),
            // so don't overwrite them yet.
            WriteHeaderMap(buffer.AsSpan(protectedMapBytesWritten), writer, unprotectedHeaders, isProtected: false, null);
            WriteContent(writer, default, isDetached: true);

            using (IncrementalHash hasher = IncrementalHash.CreateHash(hashAlgorithm))
            {
                await AppendToBeSignedAsync(buffer, hasher, SigStructureCoxtextSign1, buffer.AsMemory(0, protectedMapBytesWritten), content, hashAlgorithm, cancellationToken).ConfigureAwait(false);
                WriteSignature(buffer, hasher, writer, key, keyType, hashAlgorithm);
            }

            writer.WriteEndArray();
            return writer.Encode(buffer);
        }

        private static int WriteHeaderMap(Span<byte> buffer, CborWriter writer, CoseHeaderMap? headerMap, bool isProtected, int? algHeaderValueToSlip)
        {
            int bytesWritten = CoseHeaderMap.Encode(headerMap, buffer, mustReturnEmptyBstrIfEmpty: isProtected, algHeaderValueToSlip);
            ReadOnlySpan<byte> encodedValue = buffer.Slice(0, bytesWritten);

            if (isProtected)
            {
                writer.WriteByteString(encodedValue);
            }
            else
            {
                writer.WriteEncodedValue(encodedValue);
            }

            return bytesWritten;
        }

        private static void WriteContent(CborWriter writer, ReadOnlySpan<byte> content, bool isDetached)
        {
            if (isDetached)
            {
                writer.WriteNull();
            }
            else
            {
                writer.WriteByteString(content);
            }
        }

        [UnsupportedOSPlatform("browser")]
        private static void WriteSignature(Span<byte> buffer, IncrementalHash hasher, CborWriter writer, AsymmetricAlgorithm key, KeyType keyType, HashAlgorithmName hashAlgorithm)
        {
            int bytesWritten;

            if (keyType == KeyType.ECDsa)
            {
                bytesWritten = CoseHelpers.SignHashWithECDsa((ECDsa)key, hasher, buffer);
            }
            else
            {
                Debug.Assert(keyType == KeyType.RSA);
                bytesWritten = CoseHelpers.SignHashWithRSA((RSA)key, hasher, hashAlgorithm, buffer);
            }

            writer.WriteByteString(buffer.Slice(0, bytesWritten));
        }

        [UnsupportedOSPlatform("browser")]
        public bool Verify(AsymmetricAlgorithm key!!)
        {
            if (_content == null)
            {
                throw new CryptographicException(SR.Sign1VerifyContentWasDetached);
            }

            return VerifyCore(key, _content, null, GetKeyType(key));
        }

        [UnsupportedOSPlatform("browser")]
        public bool Verify(AsymmetricAlgorithm key!!, byte[] content!!)
        {
            if (_content != null)
            {
                throw new CryptographicException(SR.Sign1VerifyContentWasEmbedded);
            }

            return VerifyCore(key, content, null, GetKeyType(key));
        }

        [UnsupportedOSPlatform("browser")]
        public bool Verify(AsymmetricAlgorithm key!!, ReadOnlySpan<byte> content)
        {
            if (_content != null)
            {
                throw new CryptographicException(SR.Sign1VerifyContentWasEmbedded);
            }

            return VerifyCore(key, content, null, GetKeyType(key));
        }

        [UnsupportedOSPlatform("browser")]
        public bool Verify(AsymmetricAlgorithm key!!, Stream detachedContent!!)
        {
            if (_content != null)
            {
                throw new CryptographicException(SR.Sign1VerifyContentWasEmbedded);
            }

            if (!detachedContent.CanRead)
            {
                throw new ArgumentException(SR.Sign1ArgumentStreamNotReadable, nameof(detachedContent));
            }

            if (!detachedContent.CanSeek)
            {
                throw new ArgumentException(SR.Sign1ArgumentStreamNotSeekable, nameof(detachedContent));
            }

            return VerifyCore(key, default, detachedContent, GetKeyType(key));
        }

        [UnsupportedOSPlatform("browser")]
        private bool VerifyCore(AsymmetricAlgorithm key, ReadOnlySpan<byte> contentBytes, Stream? contentStream, KeyType keyType)
        {
            Debug.Assert(contentStream == null || contentBytes.Length == 0);

            ThrowIfUnsupportedHeaders();
            ReadOnlyMemory<byte> encodedAlg = GetCoseAlgorithmFromProtectedHeaders(ProtectedHeaders);

            int? nullableAlg = DecodeCoseAlgorithmHeader(encodedAlg);
            if (nullableAlg == null)
            {
                throw new CryptographicException(SR.Sign1VerifyAlgHeaderWasIncorrect);
            }

            HashAlgorithmName hashAlgorithm = GetHashAlgorithmFromCoseAlgorithmAndKeyType(nullableAlg.Value, keyType);

            if (_toBeSignedHash != null)
            {
                // toBeSigned shouldn't be cached if the message has detached content
                // since the passed-in content can be different in each call.
                Debug.Assert(_content != null);
                return VerifyHash(key, hashAlgorithm, _toBeSignedHash, keyType);
            }

            using (IncrementalHash hasher = IncrementalHash.CreateHash(hashAlgorithm))
            {
                int bufferLength = ComputeToBeSignedEncodedSize(SigStructureCoxtextSign1, _protectedHeaderAsBstr, ReadOnlySpan<byte>.Empty);
                byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferLength);

                try
                {
                    AppendToBeSigned(buffer, hasher, SigStructureCoxtextSign1, _protectedHeaderAsBstr, contentBytes, contentStream, hashAlgorithm);
                    return VerifyHash(key, hasher, hashAlgorithm, keyType);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
                }
            }
        }

        [UnsupportedOSPlatform("browser")]
        public Task<bool> VerifyAsync(AsymmetricAlgorithm key!!, Stream detachedContent!!, CancellationToken cancellationToken = default)
        {
            if (_content != null)
            {
                throw new CryptographicException(SR.Sign1VerifyContentWasEmbedded);
            }

            if (!detachedContent.CanRead)
            {
                throw new ArgumentException(SR.Sign1ArgumentStreamNotReadable, nameof(detachedContent));
            }

            if (!detachedContent.CanSeek)
            {
                throw new ArgumentException(SR.Sign1ArgumentStreamNotSeekable, nameof(detachedContent));
            }

            return VerifyAsyncCore(key, detachedContent, GetKeyType(key), cancellationToken);
        }

        [UnsupportedOSPlatform("browser")]
        private async Task<bool> VerifyAsyncCore(AsymmetricAlgorithm key, Stream content, KeyType keyType, CancellationToken cancellationToken)
        {
            ThrowIfUnsupportedHeaders();

            ReadOnlyMemory<byte> encodedAlg = GetCoseAlgorithmFromProtectedHeaders(ProtectedHeaders);

            int? nullableAlg = DecodeCoseAlgorithmHeader(encodedAlg);
            if (nullableAlg == null)
            {
                throw new CryptographicException(SR.Sign1VerifyAlgHeaderWasIncorrect);
            }

            HashAlgorithmName hashAlgorithm = GetHashAlgorithmFromCoseAlgorithmAndKeyType(nullableAlg.Value, keyType);

            using (IncrementalHash hasher = IncrementalHash.CreateHash(hashAlgorithm))
            {
                int bufferLength = ComputeToBeSignedEncodedSize(SigStructureCoxtextSign1, _protectedHeaderAsBstr, ReadOnlySpan<byte>.Empty);
                byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferLength);

                await AppendToBeSignedAsync(buffer, hasher, SigStructureCoxtextSign1, _protectedHeaderAsBstr, content, hashAlgorithm, cancellationToken).ConfigureAwait(false);
                bool retVal = VerifyHash(key, hasher, hashAlgorithm, keyType);

                ArrayPool<byte>.Shared.Return(buffer, clearArray: true);

                return retVal;
            }
        }

        [UnsupportedOSPlatform("browser")]
        private bool VerifyHash(AsymmetricAlgorithm key, IncrementalHash hasher, HashAlgorithmName hashAlgorithm, KeyType keyType)
        {
#if NETSTANDARD2_0 || NETFRAMEWORK
            byte[] hash = hasher.GetHashAndReset();
            if (_content != null)
            {
                _toBeSignedHash = hash;
            }
#else
            Span<byte> hash = stackalloc byte[0];
            if (_content != null)
            {
                hash = _toBeSignedHash = hasher.GetHashAndReset();
            }
            else
            {
                Debug.Assert(hasher.HashLengthInBytes <= 512 / 8); // largest hash we can get (SHA512).
                hash = stackalloc byte[hasher.HashLengthInBytes];
                hasher.GetHashAndReset(hash);
            }
#endif
            if (keyType == KeyType.ECDsa)
            {
                var ecdsa = (ECDsa)key;
                return ecdsa.VerifyHash(hash, _signature);
            }
            else
            {
                Debug.Assert(keyType == KeyType.RSA);
                var rsa = (RSA)key;
                return rsa.VerifyHash(hash, _signature, hashAlgorithm, RSASignaturePadding.Pss);
            }
        }

        [UnsupportedOSPlatform("browser")]
        private bool VerifyHash(AsymmetricAlgorithm key, HashAlgorithmName hashAlgorithm, byte[] toBeSignedHash, KeyType keyType)
        {
            if (keyType == KeyType.ECDsa)
            {
                var ecdsa = (ECDsa)key;
                return ecdsa.VerifyHash(toBeSignedHash, _signature);
            }
            else
            {
                Debug.Assert(keyType == KeyType.RSA);
                var rsa = (RSA)key;
                return rsa.VerifyHash(toBeSignedHash, _signature, hashAlgorithm, RSASignaturePadding.Pss);
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
