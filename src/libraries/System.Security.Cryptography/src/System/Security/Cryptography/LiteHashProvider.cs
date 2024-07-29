// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Security.Cryptography
{
    internal static partial class LiteHashProvider
    {
        internal static int HashStream(string hashAlgorithmId, Stream source, Span<byte> destination)
        {
            LiteHash hash = CreateHash(hashAlgorithmId);
            return ProcessStream(hash, source, destination);
        }

        internal static byte[] HashStream(string hashAlgorithmId, int hashSizeInBytes, Stream source)
        {
            byte[] result = new byte[hashSizeInBytes];
            LiteHash hash = CreateHash(hashAlgorithmId);
            int written = ProcessStream(hash, source, result);
            Debug.Assert(written == hashSizeInBytes);
            return result;
        }

        internal static ValueTask<int> HashStreamAsync(
            string hashAlgorithmId,
            Stream source,
            Memory<byte> destination,
            CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return ValueTask.FromCanceled<int>(cancellationToken);
            }

            LiteHash hash = CreateHash(hashAlgorithmId);
            return ProcessStreamAsync(hash, source, destination, cancellationToken);
        }

        internal static ValueTask<byte[]> HashStreamAsync(
            string hashAlgorithmId,
            Stream source,
            CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return ValueTask.FromCanceled<byte[]>(cancellationToken);
            }

            LiteHash hash = CreateHash(hashAlgorithmId);
            return ProcessStreamAsync(hash, hash.HashSizeInBytes, source, cancellationToken);
        }

        internal static int HmacStream(
            string hashAlgorithmId,
            ReadOnlySpan<byte> key,
            Stream source,
            Span<byte> destination)
        {
            LiteHmac hash = CreateHmac(hashAlgorithmId, key);
            return ProcessStream(hash, source, destination);
        }

        internal static byte[] HmacStream(
            string hashAlgorithmId,
            int hashSizeInBytes,
            ReadOnlySpan<byte> key,
            Stream source)
        {
            byte[] result = new byte[hashSizeInBytes];
            LiteHmac hash = CreateHmac(hashAlgorithmId, key);
            int written = ProcessStream(hash, source, result);
            Debug.Assert(written == hashSizeInBytes);
            return result;
        }

        internal static ValueTask<int> HmacStreamAsync(
            string hashAlgorithmId,
            ReadOnlySpan<byte> key,
            Stream source,
            Memory<byte> destination,
            CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return ValueTask.FromCanceled<int>(cancellationToken);
            }

            LiteHmac hash = CreateHmac(hashAlgorithmId, key);
            return ProcessStreamAsync(hash, source, destination, cancellationToken);
        }

        internal static ValueTask<byte[]> HmacStreamAsync(
            string hashAlgorithmId,
            ReadOnlySpan<byte> key,
            Stream source,
            CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return ValueTask.FromCanceled<byte[]>(cancellationToken);
            }

            LiteHmac hash = CreateHmac(hashAlgorithmId, key);
            return ProcessStreamAsync(hash, hash.HashSizeInBytes, source, cancellationToken);
        }

        /// This takes ownership of the hash parameter and disposes of it when done.
        private static int ProcessStream<T>(T hash, Stream source, Span<byte> destination) where T : ILiteHash
        {
            using (hash)
            {
                byte[] rented = CryptoPool.Rent(4096);

                int maxRead = 0;
                int read;

                try
                {
                    while ((read = source.Read(rented)) > 0)
                    {
                        maxRead = Math.Max(maxRead, read);
                        hash.Append(rented.AsSpan(0, read));
                    }

                    return hash.Finalize(destination);
                }
                finally
                {
                    CryptoPool.Return(rented, clearSize: maxRead);
                }
            }
        }

        /// This takes ownership of the hash parameter and disposes of it when done.
        private static async ValueTask<int> ProcessStreamAsync<T>(
            T hash,
            Stream source,
            Memory<byte> destination,
            CancellationToken cancellationToken) where T : ILiteHash
        {
            using (hash)
            {
                byte[] rented = CryptoPool.Rent(4096);

                int maxRead = 0;
                int read;

                try
                {
                    while ((read = await source.ReadAsync(rented, cancellationToken).ConfigureAwait(false)) > 0)
                    {
                        maxRead = Math.Max(maxRead, read);
                        hash.Append(rented.AsSpan(0, read));
                    }

                    return hash.Finalize(destination.Span);
                }
                finally
                {
                    CryptoPool.Return(rented, clearSize: maxRead);
                }
            }
        }

        // This takes ownership of the hash parameter and disposes of it when done.
        private static async ValueTask<byte[]> ProcessStreamAsync<T>(
            T hash,
            int outputLength,
            Stream source,
            CancellationToken cancellationToken) where T : ILiteHash
        {
            byte[] result = new byte[outputLength];
            int written = await ProcessStreamAsync(hash, source, result, cancellationToken).ConfigureAwait(false);

            Debug.Assert(written == result.Length);
            return result;
        }
    }

    internal interface ILiteHash : IDisposable
    {
        int HashSizeInBytes { get; }

        void Append(ReadOnlySpan<byte> data);
        int Finalize(Span<byte> destination);
    }
}
