// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    internal interface IKmacStatic
    {
        internal static abstract string HashAlgorithmName { get; }
        internal static abstract bool IsSupported { get; }
        internal static abstract bool IsXof { get; }
    }

    internal static class KmacStatic<TKmac> where TKmac : IKmacStatic
    {
        // KMAC-256 with a 512-bit capacity is the biggest "typical" use of KMAC (See 8.4.2 from SP-800-185)
        private const int MaxStackKmacSize = 64;

        internal static bool Verify(
            ReadOnlySpan<byte> key,
            ReadOnlySpan<byte> source,
            ReadOnlySpan<byte> hash,
            ReadOnlySpan<byte> customizationString)
        {
            ThrowIfEmptyHash(hash);
            ThrowIfNotSupported();

            Span<byte> hashBuffer = stackalloc byte[MaxStackKmacSize];

            if (hash.Length > MaxStackKmacSize)
            {
                hashBuffer = new byte[hash.Length];
            }
            else
            {
                hashBuffer = hashBuffer.Slice(0, hash.Length);
            }

            unsafe
            {
                fixed (byte* pHashBuffer = hashBuffer)
                {
                    HashProviderDispenser.OneShotHashProvider.KmacData(
                        TKmac.HashAlgorithmName,
                        key,
                        source,
                        hashBuffer,
                        customizationString,
                        TKmac.IsXof);

                    bool result = CryptographicOperations.FixedTimeEquals(hashBuffer, hash);
                    CryptographicOperations.ZeroMemory(hashBuffer);
                    return result;
                }
            }
        }

        internal static bool Verify(byte[] key, byte[] source, byte[] hash, byte[]? customizationString)
        {
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(hash);

            return Verify(
                new ReadOnlySpan<byte>(key),
                new ReadOnlySpan<byte>(source),
                new ReadOnlySpan<byte>(hash),
                new ReadOnlySpan<byte>(customizationString)); // null to empty conversion is expected.
        }

        private static void ThrowIfEmptyHash(ReadOnlySpan<byte> hash)
        {
            if (hash.IsEmpty)
                throw new ArgumentException(SR.Argument_HashEmpty, nameof(hash));
        }

        private static void ThrowIfNotSupported()
        {
            if (!TKmac.IsSupported)
                throw new PlatformNotSupportedException();
        }
    }
}
