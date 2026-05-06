// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Internal.Cryptography
{
    internal static partial class Helpers
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe ref readonly byte GetNonNullPinnableReference(ReadOnlySpan<byte> buffer)
        {
            // Based on the internal implementation from MemoryMarshal.
            return ref buffer.Length != 0 ? ref MemoryMarshal.GetReference(buffer) : ref Unsafe.AsRef<byte>((void*)1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe ref byte GetNonNullPinnableReference(Span<byte> buffer)
        {
            // Based on the internal implementation from MemoryMarshal.
            return ref buffer.Length != 0 ? ref MemoryMarshal.GetReference(buffer) : ref Unsafe.AsRef<byte>((void*)1);
        }

        internal static ReadOnlyMemory<byte> DecodeOctetStringAsMemory(ReadOnlyMemory<byte> encodedOctetString)
        {
            try
            {
                ReadOnlySpan<byte> input = encodedOctetString.Span;

                if (AsnDecoder.TryReadPrimitiveOctetString(
                    input,
                    AsnEncodingRules.BER,
                    out ReadOnlySpan<byte> primitive,
                    out int consumed))
                {
                    if (consumed != input.Length)
                    {
                        throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                    }

                    if (input.Overlaps(primitive, out int offset))
                    {
                        return encodedOctetString.Slice(offset, primitive.Length);
                    }

                    Debug.Fail("input.Overlaps(primitive) failed after TryReadPrimitiveOctetString succeeded");
                }

                byte[] ret = AsnDecoder.ReadOctetString(input, AsnEncodingRules.BER, out consumed);

                if (consumed != input.Length)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }

                return ret;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

#if !NET
        // TODO change to an extension property when https://github.com/dotnet/runtime/issues/115949 is resolved
        internal static int GetHashLengthInBytes(this IncrementalHash hash)
        {
            HashAlgorithmName hashAlgorithmName = hash.AlgorithmName;

            if (hash.AlgorithmName == HashAlgorithmName.SHA1)
            {
                return 160 / 8;
            }
            else if (hashAlgorithmName == HashAlgorithmName.SHA256)
            {
                return 256 / 8;
            }
            else if (hashAlgorithmName == HashAlgorithmName.SHA384)
            {
                return 384 / 8;
            }
            else if (hashAlgorithmName == HashAlgorithmName.SHA512)
            {
                return 512 / 8;
            }
            else if (hashAlgorithmName == HashAlgorithmName.MD5)
            {
                return 128 / 8;
            }
            else
            {
                Debug.Fail($"Unexpected hash algorithm: {hashAlgorithmName}");
                throw new CryptographicException();
            }
        }

        extension (RandomNumberGenerator)
        {
            internal static unsafe void Fill(Span<byte> buffer)
            {
                if (buffer.Length > 0)
                {
                    fixed (byte* pbBuffer = buffer)
                    {
                        Interop.BCrypt.NTSTATUS status = Interop.BCrypt.BCryptGenRandom(IntPtr.Zero, pbBuffer, buffer.Length, Interop.BCrypt.BCRYPT_USE_SYSTEM_PREFERRED_RNG);
                        if (status != Interop.BCrypt.NTSTATUS.STATUS_SUCCESS)
                            throw Interop.BCrypt.CreateCryptographicException(status);
                    }
                }
            }
        }
#endif
    }
}
