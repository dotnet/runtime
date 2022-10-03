// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Formats.Cbor;
using System.Text;

namespace System.Security.Cryptography.Cose
{
    internal static class CoseHelpers
    {
        internal const int SizeOfNull = 1;
        internal const int SizeOfArrayOfLessThan24 = 1;

        private static readonly UTF8Encoding s_utf8EncodingStrict = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

        internal static int GetByteStringEncodedSize(int bstrLength)
        {
            return GetIntegerEncodedSize(bstrLength) + bstrLength;
        }

        internal static int GetTextStringEncodedSize(string value)
        {
            int strEncodedLength = s_utf8EncodingStrict.GetByteCount(value);
            return GetIntegerEncodedSize(strEncodedLength) + strEncodedLength;
        }

        internal static int GetIntegerEncodedSize(long value)
        {
            if (value < 0)
            {
                ulong unsignedRepresentation = (value == long.MinValue) ? (ulong)long.MaxValue : (ulong)(-value) - 1;
                return GetIntegerEncodedSize(unsignedRepresentation);
            }
            else
            {
                return GetIntegerEncodedSize((ulong)value);
            }
        }

        internal static void WriteByteStringLength(IncrementalHash hasher, ulong value)
        {
            const CborMajorType MajorType = CborMajorType.ByteString;
            CborInitialByte initialByte;

            if (value < (byte)CborAdditionalInfo.Additional8BitData)
            {
                initialByte = new CborInitialByte(MajorType, (CborAdditionalInfo)value);
                hasher.AppendData(stackalloc byte[] { initialByte.InitialByte });
            }
            else if (value <= byte.MaxValue)
            {
                initialByte = new CborInitialByte(MajorType, CborAdditionalInfo.Additional8BitData);
                hasher.AppendData(stackalloc byte[] { initialByte.InitialByte, (byte)value });
            }
            else if (value <= ushort.MaxValue)
            {
                initialByte = new CborInitialByte(MajorType, CborAdditionalInfo.Additional16BitData);
                Span<byte> buffer = stackalloc byte[1 + sizeof(ushort)];
                buffer[0] = initialByte.InitialByte;
                BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(1), (ushort)value);
                hasher.AppendData(buffer);
            }
            else if (value <= uint.MaxValue)
            {
                initialByte = new CborInitialByte(MajorType, CborAdditionalInfo.Additional32BitData);
                Span<byte> buffer = stackalloc byte[1 + sizeof(uint)];
                buffer[0] = initialByte.InitialByte;
                BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(1), (uint)value);
                hasher.AppendData(buffer);
            }
            else
            {
                initialByte = new CborInitialByte(MajorType, CborAdditionalInfo.Additional64BitData);
                Span<byte> buffer = stackalloc byte[1 + sizeof(ulong)];
                buffer[0] = initialByte.InitialByte;
                BinaryPrimitives.WriteUInt64BigEndian(buffer.Slice(1), value);
                hasher.AppendData(buffer);
            }
        }

        internal static int GetIntegerEncodedSize(ulong value)
        {
            if (value < 24)
            {
                return 1;
            }
            else if (value <= byte.MaxValue)
            {
                return 1 + sizeof(byte);
            }
            else if (value <= ushort.MaxValue)
            {
                return 1 + sizeof(ushort);
            }
            else if (value <= uint.MaxValue)
            {
                return 1 + sizeof(uint);
            }
            else
            {
                return 1 + sizeof(ulong);
            }
        }

        internal static int SignHash(CoseSigner signer, IncrementalHash hasher, Span<byte> destination)
        {
            AsymmetricAlgorithm key = signer.Key;
            KeyType keyType = signer._keyType;

            if (keyType == KeyType.ECDsa)
            {
                return SignHashWithECDsa((ECDsa)key, hasher, destination);
            }
            else
            {
                Debug.Assert(keyType == KeyType.RSA);
                Debug.Assert(signer.RSASignaturePadding != null);
                return SignHashWithRSA((RSA)key, hasher, signer.HashAlgorithm, signer.RSASignaturePadding, destination);
            }
        }

        private static int SignHashWithECDsa(ECDsa key, IncrementalHash hasher, Span<byte> destination)
        {
#if NETSTANDARD2_0 || NETFRAMEWORK
            byte[] signature = key.SignHash(hasher.GetHashAndReset());
            signature.CopyTo(destination);
            return signature.Length;
#else
            Debug.Assert(hasher.HashLengthInBytes <= 512 / 8); // largest hash we can get (SHA512).
            Span<byte> hash = stackalloc byte[hasher.HashLengthInBytes];
            hasher.GetHashAndReset(hash);

            if (!key.TrySignHash(hash, destination, out int bytesWritten))
            {
                Debug.Fail("TrySignData failed with a pre-calculated destination");
                throw new CryptographicException();
            }

            return bytesWritten;
#endif
        }

        private static int SignHashWithRSA(RSA key, IncrementalHash hasher, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding, Span<byte> destination)
        {
#if NETSTANDARD2_0 || NETFRAMEWORK
            byte[] signature = key.SignHash(hasher.GetHashAndReset(), hashAlgorithm, padding);
            signature.CopyTo(destination);
            return signature.Length;
#else
            Debug.Assert(hasher.HashLengthInBytes <= 512 / 8); // largest hash we can get (SHA512).
            Span<byte> hash = stackalloc byte[hasher.HashLengthInBytes];
            hasher.GetHashAndReset(hash);

            if (!key.TrySignHash(hash, destination, hashAlgorithm, padding, out int bytesWritten))
            {
                Debug.Fail("TrySignData failed with a pre-calculated destination");
                throw new CryptographicException();
            }

            return bytesWritten;
#endif
        }

#if NETSTANDARD2_0 || NETFRAMEWORK
        internal static void AppendData(this IncrementalHash hasher, ReadOnlySpan<byte> data)
        {
            hasher.AppendData(data.ToArray());
        }
#endif

        internal static int GetCoseSignEncodedLengthMinusSignature(bool isTagged, int sizeOfCborTag, int encodedProtectedHeadersLength, CoseHeaderMap unprotectedHeaders, byte[]? content)
        {
            int retVal = 0;

            if (isTagged)
            {
                retVal += sizeOfCborTag;
            }

            retVal += SizeOfArrayOfLessThan24;

            retVal += GetByteStringEncodedSize(encodedProtectedHeadersLength);
            retVal += CoseHeaderMap.ComputeEncodedSize(unprotectedHeaders);

            if (content is null)
            {
                retVal += SizeOfNull;
            }
            else
            {
                retVal += GetByteStringEncodedSize(content.Length);
            }

            return retVal;
        }

        internal static int ComputeSignatureSize(CoseSigner signer)
        {
            int keySize = signer.Key.KeySize;
            KeyType keyType = signer._keyType;

            if (keyType == KeyType.ECDsa)
            {
                return 2 * ((keySize + 7) / 8);
            }
            else // RSA
            {
                Debug.Assert(keyType == KeyType.RSA);
                return (keySize + 7) / 8;
            }
        }

        internal static int? DecodeCoseAlgorithmHeader(ReadOnlyMemory<byte> encodedAlg)
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

        internal static HashAlgorithmName GetHashAlgorithmFromCoseAlgorithmAndKeyType(int algorithm, KeyType keyType, out RSASignaturePadding? padding)
        {
            if (keyType == KeyType.ECDsa)
            {
                padding = null;
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
                HashAlgorithmName hashAlgorithm = algorithm switch
                {
                    KnownCoseAlgorithms.PS256 or KnownCoseAlgorithms.RS256 => HashAlgorithmName.SHA256,
                    KnownCoseAlgorithms.PS384 or KnownCoseAlgorithms.RS384 => HashAlgorithmName.SHA384,
                    KnownCoseAlgorithms.PS512 or KnownCoseAlgorithms.RS512 => HashAlgorithmName.SHA512,
                    _ => throw new CryptographicException(SR.Format(SR.Sign1AlgDoesNotMatchWithTheOnesSupportedByTypeOfKey, algorithm, typeof(RSA)))
                };

                if (algorithm <= KnownCoseAlgorithms.RS256)
                {
                    Debug.Assert(algorithm >= KnownCoseAlgorithms.RS512);
                    padding = RSASignaturePadding.Pkcs1;
                }
                else
                {
                    Debug.Assert(algorithm >= KnownCoseAlgorithms.PS512 && algorithm <= KnownCoseAlgorithms.PS256);
                    padding = RSASignaturePadding.Pss;
                }

                return hashAlgorithm;
            }
        }

        internal static KeyType GetKeyType(AsymmetricAlgorithm key)
        {
            return key switch
            {
                ECDsa => KeyType.ECDsa,
                RSA => KeyType.RSA,
                _ => throw new ArgumentException(SR.Format(SR.Sign1UnsupportedKey, key.GetType()), nameof(key))
            };
        }

        internal static ReadOnlyMemory<byte> GetCoseAlgorithmFromProtectedHeaders(CoseHeaderMap protectedHeaders)
        {
            // https://datatracker.ietf.org/doc/html/rfc8152#section-3.1 alg:
            // This parameter MUST be authenticated where the ability to do so exists.
            // This authentication can be done either by placing the header in the protected header bucket or as part of the externally supplied data.
            if (!protectedHeaders.TryGetValue(CoseHeaderLabel.Algorithm, out CoseHeaderValue value))
            {
                throw new CryptographicException(SR.Sign1VerifyAlgIsRequired);
            }

            return value.EncodedValue;
        }

        internal static int WriteHeaderMap(Span<byte> buffer, CborWriter writer, CoseHeaderMap? headerMap, bool isProtected, int? algHeaderValueToSlip)
        {
            int bytesWritten = CoseHeaderMap.Encode(headerMap, buffer, isProtected, algHeaderValueToSlip);
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

        internal static void WriteContent(CborWriter writer, ReadOnlySpan<byte> content, bool isDetached)
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

        internal static void WriteSignature(Span<byte> buffer, IncrementalHash hasher, CborWriter writer, CoseSigner signer)
        {
            int bytesWritten = SignHash(signer, hasher, buffer);
            writer.WriteByteString(buffer.Slice(0, bytesWritten));
        }
    }
}
