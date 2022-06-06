// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Formats.Cbor;
using System.Runtime.Versioning;
using System.Text;

namespace System.Security.Cryptography.Cose
{
    internal static class CoseHelpers
    {
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

        [UnsupportedOSPlatform("browser")]
        internal static int SignHashWithECDsa(ECDsa key, IncrementalHash hasher, Span<byte> destination)
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

        [UnsupportedOSPlatform("browser")]
        internal static int SignHashWithRSA(RSA key, IncrementalHash hasher, HashAlgorithmName hashAlgorithm, Span<byte> destination)
        {
#if NETSTANDARD2_0 || NETFRAMEWORK
            byte[] signature = key.SignHash(hasher.GetHashAndReset(), hashAlgorithm, RSASignaturePadding.Pss);
            signature.CopyTo(destination);
            return signature.Length;
#else
            Debug.Assert(hasher.HashLengthInBytes <= 512 / 8); // largest hash we can get (SHA512).
            Span<byte> hash = stackalloc byte[hasher.HashLengthInBytes];
            hasher.GetHashAndReset(hash);

            if (!key.TrySignHash(hash, destination, hashAlgorithm, RSASignaturePadding.Pss, out int bytesWritten))
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
    }
}
