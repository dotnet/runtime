// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.Security.Cryptography
{
    internal static partial class LiteHashProvider
    {
        internal static void XofStream(string hashAlgorithmId, Stream source, Span<byte> destination)
        {
            LiteXof hash = CreateXof(hashAlgorithmId);
            int written = ProcessStream(hash, source, destination);
            Debug.Assert(written == destination.Length);
        }

        internal static byte[] XofStream(string hashAlgorithmId, int outputLength, Stream source)
        {
            byte[] result = new byte[outputLength];
            LiteXof hash = CreateXof(hashAlgorithmId);
            int written = ProcessStream(hash, source, result);
            Debug.Assert(written == outputLength);
            return result;
        }

        internal static ValueTask XofStreamAsync(
            string hashAlgorithmId,
            Stream source,
            Memory<byte> destination,
            CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return ValueTask.FromCanceled(cancellationToken);
            }

            LiteXof hash = CreateXof(hashAlgorithmId);
            return ProcessStreamXofAsync(hash, source, destination, cancellationToken);
        }

        internal static ValueTask<byte[]> XofStreamAsync(
            string hashAlgorithmId,
            int outputLength,
            Stream source,
            CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return ValueTask.FromCanceled<byte[]>(cancellationToken);
            }

            LiteXof hash = CreateXof(hashAlgorithmId);
            return ProcessStreamAsync(hash, outputLength, source, cancellationToken);
        }

        // This takes ownership of the hash parameter and disposes of it when done.
        private static async ValueTask ProcessStreamXofAsync<T>(
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

                    hash.Finalize(destination.Span);
                }
                finally
                {
                    CryptoPool.Return(rented, clearSize: maxRead);
                }
            }
        }
    }
}
