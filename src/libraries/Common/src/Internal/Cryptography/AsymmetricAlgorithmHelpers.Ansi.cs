// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Security.Cryptography;

namespace Internal.Cryptography
{
    internal static partial class AsymmetricAlgorithmHelpers
    {
        // Encodes a EC key as an uncompressed set of concatenated scalars,
        // optionally including the private key. To omit the private parameter,
        // "d" must have a length of zero.
        public static (Range PublicKey, Range? PrivateKey) EncodeToUncompressedAnsiX963Key(
            ReadOnlySpan<byte> x,
            ReadOnlySpan<byte> y,
            ReadOnlySpan<byte> d,
            Span<byte> destination)
        {
            const byte UncompressedKeyPrefix = 0x04;
            if (x.Length != y.Length || (d.Length > 0 && d.Length != y.Length))
                throw new CryptographicException(SR.Cryptography_NotValidPublicOrPrivateKey);

            int size = 1 + x.Length + y.Length + d.Length; // 0x04 || X || Y { || D }

            if (destination.Length < size)
            {
                Debug.Fail("destination.Length < size");
                throw new CryptographicException();
            }

            destination[0] = UncompressedKeyPrefix;
            x.CopyTo(destination.Slice(1));
            y.CopyTo(destination.Slice(1 + x.Length));
            Range publicKey = 0..(1 + x.Length + y.Length);
            Range? privateKey;

            if (d.Length > 0)
            {
                d.CopyTo(destination[publicKey.End..]);
                privateKey = 0..size;
            }
            else
            {
                privateKey = null;
            }

            return (publicKey, privateKey);
        }
    }
}
