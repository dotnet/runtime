// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;

namespace System.Security.Cryptography
{
    internal sealed partial class SP800108HmacCounterKdfImplementationManaged
    {
        private const int CharToBytesStackBufferSize = 256;

        public SP800108HmacCounterKdfImplementationManaged(ReadOnlySpan<byte> key, HashAlgorithmName hashAlgorithm)
        {
            // Use the POH if we can so the key doesn't get moved around by the GC.
            _key = GC.AllocateArray<byte>(key.Length, pinned: true);
            key.CopyTo(_key);
            _hashAlgorithm = hashAlgorithm;
        }

        internal static void DeriveBytesOneShot(
            ReadOnlySpan<byte> key,
            HashAlgorithmName hashAlgorithm,
            ReadOnlySpan<byte> label,
            ReadOnlySpan<byte> context,
            Span<byte> destination)
        {
            if (destination.Length == 0)
            {
                return;
            }

            Debug.Assert(destination.Length <= 0x1FFFFFFF);

            // Do everything as checked. Over/underflows are never expected.
            checked
            {
                // The KDF is defined as K(i) := PRF (KI, [i]2 || Label || 0x00 || Context || [L]2)
                // We know L is already less than 0x1FFFFFFF. h = ceil(L / h) where H is the hash length in bits.
                // So we don't expect i to overflow.
                using (IncrementalHash hash = IncrementalHash.CreateHMAC(hashAlgorithm, key))
                {
                    Span<byte> iBuffer = stackalloc byte[sizeof(uint)];
                    Span<byte> lBuffer = stackalloc byte[sizeof(uint)];
                    ReadOnlySpan<byte> zero = [0];
                    Span<byte> hmacBuffer = stackalloc byte[512 / 8]; // Largest HMAC supported is SHA512
                    int hmacBufferWritten = 0;

                    BinaryPrimitives.WriteUInt32BigEndian(lBuffer, (uint)destination.Length * 8U);

                    for (uint i = 1; !destination.IsEmpty; i++)
                    {
                        BinaryPrimitives.WriteUInt32BigEndian(iBuffer, i);
                        hash.AppendData(iBuffer);
                        hash.AppendData(label);
                        hash.AppendData(zero);
                        hash.AppendData(context);
                        hash.AppendData(lBuffer);

                        if (destination.Length >= hash.HashLengthInBytes)
                        {
                            int written = hash.GetHashAndReset(destination);
                            destination = destination.Slice(written);
                        }
                        else
                        {
                            hmacBufferWritten = hash.GetHashAndReset(hmacBuffer);
                            Debug.Assert(hmacBufferWritten > destination.Length);
                            hmacBuffer.Slice(0, destination.Length).CopyTo(destination);
                            destination = default;
                        }
                    }

                    // Get derived key material off the stack, if any.
                    CryptographicOperations.ZeroMemory(hmacBuffer.Slice(0, hmacBufferWritten));
                }
            }
        }

         internal static void DeriveBytesOneShot(
            ReadOnlySpan<byte> key,
            HashAlgorithmName hashAlgorithm,
            ReadOnlySpan<char> label,
            ReadOnlySpan<char> context,
            Span<byte> destination)
        {
            if (destination.Length == 0)
            {
                return;
            }

            using (Utf8DataEncoding labelData = new Utf8DataEncoding(label, stackalloc byte[CharToBytesStackBufferSize]))
            using (Utf8DataEncoding contextData = new Utf8DataEncoding(context, stackalloc byte[CharToBytesStackBufferSize]))
            {
                DeriveBytesOneShot(key, hashAlgorithm, labelData.Utf8Bytes, contextData.Utf8Bytes, destination);
            }
        }
    }
}
