// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Formats.Cbor;
using System.Numerics;
using System.Runtime.CompilerServices;
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

        internal static CoseAlgorithm? DecodeCoseAlgorithmHeader(ReadOnlyMemory<byte> encodedAlg)
        {
            var reader = new CborReader(encodedAlg);
            CborReaderState state = reader.PeekState();

            if (state == CborReaderState.UnsignedInteger)
            {
                ThrowUnsignedIntegerNotSupported(reader.ReadUInt64());
            }
            else if (state == CborReaderState.NegativeInteger)
            {
                ulong cborNegativeIntRepresentation = reader.ReadCborNegativeIntegerRepresentation();

                if (cborNegativeIntRepresentation > long.MaxValue)
                {
                    ThrowCborNegativeIntegerNotSupported(cborNegativeIntRepresentation);
                }

                long alg = checked(-1L - (long)cborNegativeIntRepresentation);
                CoseAlgorithm coseAlg = CoseKey.CoseAlgorithmFromInt64(alg);

                if (reader.BytesRemaining != 0)
                {
                    return null;
                }

                return coseAlg;
            }

            if (state == CborReaderState.TextString)
            {
                CoseAlgorithm alg = CoseKey.CoseAlgorithmFromString(reader.ReadTextString());

                if (reader.BytesRemaining != 0)
                {
                    return null;
                }

                return alg;
            }

            return null;

            static void ThrowUnsignedIntegerNotSupported(ulong alg) // All algorithm valid values are negatives.
                => throw new CryptographicException(SR.Format(SR.Sign1UnknownCoseAlgorithm, alg));

            static void ThrowCborNegativeIntegerNotSupported(ulong alg) // Cbor Negative Integer Representation is too big.
                => throw new CryptographicException(SR.Format(SR.Sign1UnknownCoseAlgorithm, BigInteger.MinusOne - new BigInteger(alg)));
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

        internal static int WriteHeaderMap(Span<byte> buffer, CborWriter writer, CoseHeaderMap? headerMap, bool isProtected, CoseAlgorithm? algHeaderValueToSlip)
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
            int bytesWritten = signer.CoseKey.Sign(toBeSigned, buffer);
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
