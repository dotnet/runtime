// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text;

namespace System.Security.Cryptography
{
    internal sealed partial class SP800108HmacCounterKdfImplementationManaged
    {
        public SP800108HmacCounterKdfImplementationManaged(ReadOnlySpan<byte> key, HashAlgorithmName hashAlgorithm)
        {
            _key = key.ToArray();
            _hashAlgorithm = hashAlgorithm;
        }

        internal static unsafe void DeriveBytesOneShot(
            ReadOnlySpan<byte> key,
            HashAlgorithmName hashAlgorithm,
            ReadOnlySpan<byte> label,
            ReadOnlySpan<byte> context,
            Span<byte> destination)
        {
            if (destination.IsEmpty)
            {
                return;
            }

            // IncrementalHash needs an array of the correct size, so we can't rent for the key.
            byte[] keyBuffer = new byte[key.Length];
            byte[] labelBuffer = CryptoPool.Rent(label.Length);
            byte[] contextBuffer = CryptoPool.Rent(context.Length);

            // Fixed to prevent GC moves.
            fixed (byte* pKeyBuffer = keyBuffer)
            {
                try
                {
                    key.CopyTo(keyBuffer);
                    label.CopyTo(labelBuffer);
                    context.CopyTo(contextBuffer);

                    DeriveBytesOneShot(
                        keyBuffer,
                        hashAlgorithm,
                        labelBuffer,
                        label.Length,
                        contextBuffer,
                        context.Length,
                        destination);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(keyBuffer);
                    CryptoPool.Return(labelBuffer, clearSize: label.Length);
                    CryptoPool.Return(contextBuffer, clearSize: context.Length);
                }
            }
        }

        internal static void DeriveBytesOneShot(
            byte[] key,
            HashAlgorithmName hashAlgorithm,
            byte[] label,
            byte[] context,
            Span<byte> destination)
        {
            DeriveBytesOneShot(key, hashAlgorithm, label, label.Length, context, context.Length, destination);
        }

         internal static unsafe void DeriveBytesOneShot(
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

            // The netstandard implementation needs arrays for all inputs, so always rent and don't perform
            // the UTF8 encoding on the stack since that will just end up renting again anyway.

            Encoding utf8ThrowingEncoding = Utf8DataEncoding.ThrowingUtf8Encoding;
            byte[] labelBuffer = CryptoPool.Rent(utf8ThrowingEncoding.GetMaxByteCount(label.Length));
            byte[] contextBuffer = CryptoPool.Rent(utf8ThrowingEncoding.GetMaxByteCount(context.Length));
            int labelWritten = 0;
            int contextWritten = 0;

            byte[] keyBuffer = new byte[key.Length];

            fixed (byte* pKeyBuffer = keyBuffer)
            {
                try
                {
                    labelWritten = utf8ThrowingEncoding.GetBytes(label, labelBuffer);
                    contextWritten = utf8ThrowingEncoding.GetBytes(context, contextBuffer);
                    key.CopyTo(keyBuffer);

                    DeriveBytesOneShot(
                        keyBuffer,
                        hashAlgorithm,
                        labelBuffer,
                        labelWritten,
                        contextBuffer,
                        contextWritten,
                        destination);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(keyBuffer);
                    CryptoPool.Return(labelBuffer, labelWritten);
                    CryptoPool.Return(contextBuffer, contextWritten);
                }
            }
        }

        private static void DeriveBytesOneShot(
            byte[] key,
            HashAlgorithmName hashAlgorithm,
            byte[] label,
            int labelLength,
            byte[] context,
            int contextLength,
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
                    // We use this rented buffer for three things. The first two uints for i and L, and last byte
                    // for the zero separator. So this is
                    // uint(L) | uint(i) | byte(0)
                    // This lets us use a single rent for the L, i, and 0x00.
                    const int LOffset = 0;
                    const int LLength = sizeof(uint);
                    const int IOffset = LLength + LOffset;
                    const int ILength = sizeof(uint);
                    const int ZeroOffset = IOffset + ILength;
                    const int ZeroLength = sizeof(byte);
                    const int RentSize = ZeroOffset + ZeroLength;

                    byte[]? rentedBuffer = null;

                    try
                    {
                        rentedBuffer = CryptoPool.Rent(RentSize);

                        WriteUInt32BigEndian((uint)destination.Length * 8U, rentedBuffer.AsSpan(LOffset, LLength));
                        rentedBuffer[ZeroOffset] = 0;

                        for (uint i = 1; !destination.IsEmpty; i++)
                        {
                            WriteUInt32BigEndian(i, rentedBuffer.AsSpan(IOffset, ILength));
                            hash.AppendData(rentedBuffer, IOffset, ILength);
                            hash.AppendData(label, 0, labelLength);
                            hash.AppendData(rentedBuffer, ZeroOffset, ZeroLength);
                            hash.AppendData(context, 0, contextLength);
                            hash.AppendData(rentedBuffer, LOffset, LLength);

                            byte[] hmac = hash.GetHashAndReset();
                            int needed = Math.Min(destination.Length, hmac.Length);
                            hmac.AsSpan(0, needed).CopyTo(destination);
                            destination = destination.Slice(needed);

                            // Best effort to zero out the key material.
                            CryptographicOperations.ZeroMemory(hmac);
                        }
                    }
                    finally
                    {
                        if (rentedBuffer is not null)
                        {
                            CryptoPool.Return(rentedBuffer, clearSize: RentSize);
                        }
                    }
                }
            }
        }

        private static void WriteUInt32BigEndian(uint value, Span<byte> destination)
        {
            Debug.Assert(destination.Length == sizeof(uint));
            destination[0] = (byte)(value >> 24);
            destination[1] = (byte)(value >> 16);
            destination[2] = (byte)(value >> 8);
            destination[3] = (byte)(value);
        }
    }
}
