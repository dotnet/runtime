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

            return VerifyCore(
                key,
                source,
                hash,
                customizationString,
                static (key, source, hash, customizationString, buffer) =>
                {
                    HashProviderDispenser.OneShotHashProvider.KmacData(
                        TKmac.HashAlgorithmName,
                        key,
                        source,
                        buffer,
                        customizationString,
                        TKmac.IsXof);
                });
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

        internal static bool Verify(
            ReadOnlySpan<byte> key,
            Stream source,
            ReadOnlySpan<byte> hash,
            ReadOnlySpan<byte> customizationString)
        {
            ArgumentNullException.ThrowIfNull(source);
            ThrowIfEmptyHash(hash);
            ThrowIfStreamUnreadable(source);
            ThrowIfNotSupported();

            return VerifyCore(
                key,
                source,
                hash,
                customizationString,
                static (key, source, hash, customizationString, buffer) =>
                {
                    LiteHashProvider.KmacStream(
                        TKmac.HashAlgorithmName,
                        key,
                        customizationString,
                        source,
                        TKmac.IsXof,
                        buffer);
                });
        }

        internal static bool Verify(byte[] key, Stream source, byte[] hash, byte[]? customizationString)
        {
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(hash);

            return Verify(
                new ReadOnlySpan<byte>(key),
                source,
                new ReadOnlySpan<byte>(hash),
                new ReadOnlySpan<byte>(customizationString)); // null to empty conversion is expected.
        }

        internal static ValueTask<bool> VerifyAsync(
            ReadOnlyMemory<byte> key,
            Stream source,
            ReadOnlyMemory<byte> hash,
            ReadOnlyMemory<byte> customizationString,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(source);
            ThrowIfEmptyHash(hash);
            ThrowIfStreamUnreadable(source);
            ThrowIfNotSupported();

            return VerifyAsyncInner(key, source, hash, customizationString, cancellationToken);

            static async ValueTask<bool> VerifyAsyncInner(
                ReadOnlyMemory<byte> key,
                Stream source,
                ReadOnlyMemory<byte> hash,
                ReadOnlyMemory<byte> customizationString,
                CancellationToken cancellationToken)
            {
                byte[] mac = new byte[hash.Length];

                using (PinAndClear.Track(mac))
                {
                    await LiteHashProvider.KmacStreamAsync(
                        TKmac.HashAlgorithmName,
                        key.Span,
                        source,
                        TKmac.IsXof,
                        mac,
                        customizationString.Span,
                        cancellationToken).ConfigureAwait(false);

                    return CryptographicOperations.FixedTimeEquals(mac, hash.Span);
                }
            }
        }

        internal static ValueTask<bool> VerifyAsync(
            byte[] key,
            Stream source,
            byte[] hash,
            byte[]? customizationString,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(hash);

            return VerifyAsync(
                new ReadOnlyMemory<byte>(key),
                source,
                new ReadOnlyMemory<byte>(hash),
                new ReadOnlyMemory<byte>(customizationString), // null to empty conversion is expected.
                cancellationToken);
        }

        private static bool VerifyCore<TSource>(
            ReadOnlySpan<byte> key,
            TSource source,
            ReadOnlySpan<byte> hash,
            ReadOnlySpan<byte> customizationString,
            Action<ReadOnlySpan<byte>, TSource, ReadOnlySpan<byte>, ReadOnlySpan<byte>, Span<byte>> callback)
            where TSource : allows ref struct
        {
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
                    callback(key, source, hash, customizationString, hashBuffer);
                    bool result = CryptographicOperations.FixedTimeEquals(hashBuffer, hash);
                    CryptographicOperations.ZeroMemory(hashBuffer);
                    return result;
                }
            }
        }

        private static void ThrowIfEmptyHash(ReadOnlySpan<byte> hash)
        {
            if (hash.IsEmpty)
                throw new ArgumentException(SR.Argument_HashEmpty, nameof(hash));
        }

        private static void ThrowIfEmptyHash(ReadOnlyMemory<byte> hash)
        {
            if (hash.IsEmpty)
                throw new ArgumentException(SR.Argument_HashEmpty, nameof(hash));
        }

        private static void ThrowIfStreamUnreadable(Stream source)
        {
            if (!source.CanRead)
                throw new ArgumentException(SR.Argument_StreamNotReadable, nameof(source));
        }

        private static void ThrowIfNotSupported()
        {
            if (!TKmac.IsSupported)
                throw new PlatformNotSupportedException();
        }
    }
}
