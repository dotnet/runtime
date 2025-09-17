// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    internal interface IHMACStatic
    {
        internal static abstract int HashSizeInBytes { get; }
        internal static abstract string HashAlgorithmName { get; }
    }

    // This class acts as a single implementation of the HMAC classes that the public APIs defer to.
    // The public APIs call these methods directly, so they need to behave as if they were public,
    // including parameter validation and async behavior.
    internal static class HMACStatic<THMAC> where THMAC : IHMACStatic
    {
        internal static bool Verify(ReadOnlySpan<byte> key, ReadOnlySpan<byte> source, ReadOnlySpan<byte> hash)
        {
            if (hash.Length != THMAC.HashSizeInBytes)
                throw new ArgumentException(SR.Format(SR.Argument_HashImprecise, THMAC.HashSizeInBytes), nameof(hash));

            Span<byte> mac = stackalloc byte[THMAC.HashSizeInBytes];
            int written = HashProviderDispenser.OneShotHashProvider.MacData(THMAC.HashAlgorithmName, key, source, mac);
            Debug.Assert(written == THMAC.HashSizeInBytes);

            bool result = CryptographicOperations.FixedTimeEquals(mac, hash);
            CryptographicOperations.ZeroMemory(mac);
            return result;
        }

        internal static byte[] HashData(byte[] key, byte[] source)
        {
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(source);

            return HashData(new ReadOnlySpan<byte>(key), new ReadOnlySpan<byte>(source));
        }

        internal static byte[] HashData(ReadOnlySpan<byte> key, ReadOnlySpan<byte> source)
        {
            byte[] buffer = new byte[THMAC.HashSizeInBytes];

            int written = HashData(key, source, buffer.AsSpan());
            Debug.Assert(written == buffer.Length);

            return buffer;
        }

        internal static int HashData(ReadOnlySpan<byte> key, ReadOnlySpan<byte> source, Span<byte> destination)
        {
            if (!TryHashData(key, source, destination, out int bytesWritten))
            {
                throw new ArgumentException(SR.Argument_DestinationTooShort, nameof(destination));
            }

            return bytesWritten;
        }

        internal static bool TryHashData(ReadOnlySpan<byte> key, ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length < THMAC.HashSizeInBytes)
            {
                bytesWritten = 0;
                return false;
            }

            bytesWritten = HashProviderDispenser.OneShotHashProvider.MacData(THMAC.HashAlgorithmName, key, source, destination);
            Debug.Assert(bytesWritten == THMAC.HashSizeInBytes);

            return true;
        }

        internal static int HashData(ReadOnlySpan<byte> key, Stream source, Span<byte> destination)
        {
            ArgumentNullException.ThrowIfNull(source);

            if (destination.Length < THMAC.HashSizeInBytes)
                throw new ArgumentException(SR.Argument_DestinationTooShort, nameof(destination));

            if (!source.CanRead)
                throw new ArgumentException(SR.Argument_StreamNotReadable, nameof(source));

            return LiteHashProvider.HmacStream(THMAC.HashAlgorithmName, key, source, destination);
        }

        internal static byte[] HashData(ReadOnlySpan<byte> key, Stream source)
        {
            ArgumentNullException.ThrowIfNull(source);

            if (!source.CanRead)
                throw new ArgumentException(SR.Argument_StreamNotReadable, nameof(source));

            return LiteHashProvider.HmacStream(THMAC.HashAlgorithmName, THMAC.HashSizeInBytes, key, source);
        }

        internal static byte[] HashData(byte[] key, Stream source)
        {
            ArgumentNullException.ThrowIfNull(key);

            return HashData(new ReadOnlySpan<byte>(key), source);
        }

        internal static ValueTask<byte[]> HashDataAsync(
            ReadOnlyMemory<byte> key,
            Stream source,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(source);

            if (!source.CanRead)
                throw new ArgumentException(SR.Argument_StreamNotReadable, nameof(source));

            return LiteHashProvider.HmacStreamAsync(THMAC.HashAlgorithmName, key.Span, source, cancellationToken);
        }

        internal static ValueTask<byte[]> HashDataAsync(byte[] key, Stream source, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(key);

            return HashDataAsync(new ReadOnlyMemory<byte>(key), source, cancellationToken);
        }

        internal static ValueTask<int> HashDataAsync(
            ReadOnlyMemory<byte> key,
            Stream source,
            Memory<byte> destination,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(source);

            if (destination.Length < THMAC.HashSizeInBytes)
                throw new ArgumentException(SR.Argument_DestinationTooShort, nameof(destination));

            if (!source.CanRead)
                throw new ArgumentException(SR.Argument_StreamNotReadable, nameof(source));

            return LiteHashProvider.HmacStreamAsync(
                THMAC.HashAlgorithmName,
                key.Span,
                source,
                destination,
                cancellationToken);
        }

        internal static bool Verify(byte[] key, byte[] source, byte[] hash)
        {
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(hash);

            return Verify(new ReadOnlySpan<byte>(key), new ReadOnlySpan<byte>(source), new ReadOnlySpan<byte>(hash));
        }

        internal static bool Verify(ReadOnlySpan<byte> key, Stream source, ReadOnlySpan<byte> hash)
        {
            ArgumentNullException.ThrowIfNull(source);

            if (hash.Length != THMAC.HashSizeInBytes)
                throw new ArgumentException(SR.Format(SR.Argument_HashImprecise, THMAC.HashSizeInBytes), nameof(hash));

            if (!source.CanRead)
                throw new ArgumentException(SR.Argument_StreamNotReadable, nameof(source));

            Span<byte> mac = stackalloc byte[THMAC.HashSizeInBytes];
            int written = LiteHashProvider.HmacStream(THMAC.HashAlgorithmName, key, source, mac);
            Debug.Assert(written == THMAC.HashSizeInBytes);

            bool result = CryptographicOperations.FixedTimeEquals(mac, hash);
            CryptographicOperations.ZeroMemory(mac);
            return result;
        }

        internal static bool Verify(byte[] key, Stream source, byte[] hash)
        {
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(hash);
            // source parameter check is done in called overload.

            return Verify(new ReadOnlySpan<byte>(key), source, new ReadOnlySpan<byte>(hash));
        }

        internal static ValueTask<bool> VerifyAsync(
            ReadOnlyMemory<byte> key,
            Stream source,
            ReadOnlyMemory<byte> hash,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(source);

            if (hash.Length != THMAC.HashSizeInBytes)
                throw new ArgumentException(SR.Format(SR.Argument_HashImprecise, THMAC.HashSizeInBytes), nameof(hash));

            if (!source.CanRead)
                throw new ArgumentException(SR.Argument_StreamNotReadable, nameof(source));

            return VerifyAsyncInner(key, source, hash, cancellationToken);

            static async ValueTask<bool> VerifyAsyncInner(
                ReadOnlyMemory<byte> key,
                Stream source,
                ReadOnlyMemory<byte> hash,
                CancellationToken cancellationToken)
            {
                byte[] mac = new byte[THMAC.HashSizeInBytes];

                using (PinAndClear.Track(mac))
                {
                    int written = await LiteHashProvider.HmacStreamAsync(
                        THMAC.HashAlgorithmName,
                        key.Span,
                        source,
                        mac,
                        cancellationToken).ConfigureAwait(false);

                    Debug.Assert(written == THMAC.HashSizeInBytes);
                    return CryptographicOperations.FixedTimeEquals(mac, hash.Span);
                }
            }
        }

        internal static ValueTask<bool> VerifyAsync(
            byte[] key,
            Stream source,
            byte[] hash,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(hash);
            // source parameter check is done in called overload.

            return VerifyAsync(new ReadOnlyMemory<byte>(key), source, new ReadOnlyMemory<byte>(hash), cancellationToken);
        }
    }
}
