// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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

        internal static void WriteByteStringLength(ToBeSignedBuilder toBeSignedBuilder, ulong value)
        {
            const CborMajorType MajorType = CborMajorType.ByteString;
            CborInitialByte initialByte;

            if (value < (byte)CborAdditionalInfo.Additional8BitData)
            {
                initialByte = new CborInitialByte(MajorType, (CborAdditionalInfo)value);
                toBeSignedBuilder.AppendToBeSigned([initialByte.InitialByte]);
            }
            else if (value <= byte.MaxValue)
            {
                initialByte = new CborInitialByte(MajorType, CborAdditionalInfo.Additional8BitData);
                toBeSignedBuilder.AppendToBeSigned([initialByte.InitialByte, (byte)value]);
            }
            else if (value <= ushort.MaxValue)
            {
                initialByte = new CborInitialByte(MajorType, CborAdditionalInfo.Additional16BitData);
                Span<byte> buffer = stackalloc byte[1 + sizeof(ushort)];
                buffer[0] = initialByte.InitialByte;
                BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(1), (ushort)value);
                toBeSignedBuilder.AppendToBeSigned(buffer);
            }
            else if (value <= uint.MaxValue)
            {
                initialByte = new CborInitialByte(MajorType, CborAdditionalInfo.Additional32BitData);
                Span<byte> buffer = stackalloc byte[1 + sizeof(uint)];
                buffer[0] = initialByte.InitialByte;
                BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(1), (uint)value);
                toBeSignedBuilder.AppendToBeSigned(buffer);
            }
            else
            {
                initialByte = new CborInitialByte(MajorType, CborAdditionalInfo.Additional64BitData);
                Span<byte> buffer = stackalloc byte[1 + sizeof(ulong)];
                buffer[0] = initialByte.InitialByte;
                BinaryPrimitives.WriteUInt64BigEndian(buffer.Slice(1), value);
                toBeSignedBuilder.AppendToBeSigned(buffer);
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

        internal static int SignHash(CoseSigner signer, ReadOnlySpan<byte> toBeSigned, Span<byte> destination)
        {
            AsymmetricAlgorithm key = signer.Key;
            KeyType keyType = signer._keyType;

            switch (keyType)
            {
                case KeyType.ECDsa:
                    return SignHashWithECDsa((ECDsa)key, toBeSigned, destination);
                case KeyType.RSA:
                    Debug.Assert(signer.RSASignaturePadding != null);
                    return SignHashWithRSA((RSA)key, toBeSigned, signer.HashAlgorithm, signer.RSASignaturePadding, destination);
#pragma warning disable SYSLIB5006
                case KeyType.MLDsa:
                    // we ignore signer.HashAlgorithm
                    return SignDataWithMLDsa(((MLDsaAsymmetricAlgorithmWrapper)key).WrappedKey, toBeSigned, destination);
#pragma warning restore SYSLIB5006
                default:
                    Debug.Fail("Unknown key type");
                    throw new CryptographicException(SR.Format(SR.Sign1UnsupportedKey, keyType.ToString()));
            }
        }

        private static int SignHashWithECDsa(ECDsa key, ReadOnlySpan<byte> hash, Span<byte> destination)
        {
            if (!key.TrySignHash(hash, destination, out int bytesWritten))
            {
                Debug.Fail("TrySignData failed with a pre-calculated destination");
                throw new CryptographicException();
            }

            return bytesWritten;
        }

        private static int SignHashWithRSA(RSA key, ReadOnlySpan<byte> hash, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding, Span<byte> destination)
        {
            if (!key.TrySignHash(hash, destination, hashAlgorithm, padding, out int bytesWritten))
            {
                Debug.Fail("TrySignData failed with a pre-calculated destination");
                throw new CryptographicException();
            }

            return bytesWritten;
        }

        [Experimental(Experimentals.PostQuantumCryptographyDiagId)]
        private static int SignDataWithMLDsa(MLDsa key, ReadOnlySpan<byte> data, Span<byte> destination)
        {
            if (destination.Length != key.SignData(data, destination))
            {
                Debug.Fail($"SignData failed with a pre-calculated destination (pre-calculated: {destination.Length} != signature-size: {key.Algorithm.SignatureSizeInBytes})");
                throw new CryptographicException();
            }

            return destination.Length;
        }

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

            switch (keyType)
            {
                case KeyType.ECDsa:
                    return 2 * ((keySize + 7) / 8);
                case KeyType.RSA:
                    return (keySize + 7) / 8;
#pragma warning disable SYSLIB5006
                case KeyType.MLDsa:
                    return ((MLDsaAsymmetricAlgorithmWrapper)signer.Key).WrappedKey.Algorithm.SignatureSizeInBytes;
#pragma warning restore SYSLIB5006
                default:
                    Debug.Fail($"Unknown key type: {keyType}");
                    throw new CryptographicException(SR.Format(SR.Sign1UnsupportedKey, keyType.ToString()));
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
                    return null;
                }

                return (int)alg;
            }

            if (state == CborReaderState.TextString)
            {
                int alg = KnownCoseAlgorithms.FromString(reader.ReadTextString());

                if (reader.BytesRemaining != 0)
                {
                    return null;
                }

                return alg;
            }

            return null;
        }

        internal static HashAlgorithmName GetHashAlgorithmFromCoseAlgorithmAndKeyType(int algorithm, KeyType keyType, out RSASignaturePadding? padding)
        {
            switch (keyType)
            {
                case KeyType.ECDsa:
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
                case KeyType.RSA:
                {
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
#pragma warning disable SYSLIB5006
                case KeyType.MLDsa:
                {
                    padding = null;
                    if (algorithm != KnownCoseAlgorithms.MLDsa44 && algorithm != KnownCoseAlgorithms.MLDsa65 && algorithm != KnownCoseAlgorithms.MLDsa87)
                    {
                        throw new CryptographicException(SR.Format(SR.Sign1AlgDoesNotMatchWithTheOnesSupportedByTypeOfKey, algorithm, typeof(MLDsa)));
                    }

                    return default;
                }
#pragma warning restore SYSLIB5006
                default:
                {
                    Debug.Fail($"Unknown key type: {keyType}");
                    throw new CryptographicException();
                }
            }
        }

        internal static KeyType GetKeyType(AsymmetricAlgorithm key)
        {
            return key switch
            {
                ECDsa => KeyType.ECDsa,
                RSA => KeyType.RSA,
#pragma warning disable SYSLIB5006
                MLDsaAsymmetricAlgorithmWrapper => KeyType.MLDsa,
#pragma warning restore SYSLIB5006
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

        internal static void WriteSignature(Span<byte> buffer, ReadOnlySpan<byte> toBeSigned, CborWriter writer, CoseSigner signer)
        {
            int bytesWritten = SignHash(signer, toBeSigned, buffer);
            writer.WriteByteString(buffer.Slice(0, bytesWritten));
        }

#if NETSTANDARD2_0 || NETFRAMEWORK
        internal static void AppendData(this IncrementalHash hasher, ReadOnlySpan<byte> data)
        {
            hasher.AppendData(data.ToArray());
        }

        internal static bool TrySignHash(this ECDsa key, ReadOnlySpan<byte> hash, Span<byte> destination, out int bytesWritten)
        {
            byte[] signature = key.SignHash(hash.ToArray());

            if (destination.Length < signature.Length)
            {
                bytesWritten = 0;
                return false;
            }

            signature.CopyTo(destination);
            bytesWritten = signature.Length;
            return true;
        }

        internal static bool TrySignHash(this RSA key, ReadOnlySpan<byte> hash, Span<byte> destination, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding, out int bytesWritten)
        {
            byte[] signature = key.SignHash(hash.ToArray(), hashAlgorithm, padding);

            if (destination.Length < signature.Length)
            {
                bytesWritten = 0;
                return false;
            }

            signature.CopyTo(destination);
            bytesWritten = signature.Length;
            return true;
        }

        internal static bool VerifyHash(this RSA rsa, ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signature, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
        {
            return rsa.VerifyHash(hash.ToArray(), signature.ToArray(), hashAlgorithm, padding);
        }

        internal static bool VerifyHash(this ECDsa ecdsa, ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signature)
        {
            return ecdsa.VerifyHash(hash.ToArray(), signature.ToArray());
        }
#endif
    }
}
