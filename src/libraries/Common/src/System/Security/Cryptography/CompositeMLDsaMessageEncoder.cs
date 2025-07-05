// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Security.Cryptography;

namespace Internal.Cryptography
{
    internal sealed class CompositeMLDsaMessageEncoder : IDisposable
    {
        private static readonly byte[] MessageRepresentativePrefix = "CompositeAlgorithmSignatures2025"u8.ToArray();

        private readonly byte[] _bytes;

        private IncrementalHash _hash;

        public CompositeMLDsaMessageEncoder(CompositeMLDsaAlgorithm algorithm, ReadOnlySpan<byte> context, ReadOnlySpan<byte> r)
        {
            // M' = Prefix || Domain || len(ctx) || ctx || r || PH( M )

            int hashLength = Helpers.HashLength(algorithm.HashAlgorithmName);
            int length = checked(MessageRepresentativePrefix.Length +   // Prefix
                                 algorithm.DomainSeparator.Length +     // Domain
                                 1 +                                    // len(ctx)
                                 context.Length +                       // ctx
                                 32 +                                   // r
                                 hashLength);                           // PH( M )

            _bytes = new byte[length];
            Span<byte> M_prime = _bytes;
            M_prime.Clear();

            int offset = 0;

            // Prefix
            MessageRepresentativePrefix.AsSpan().CopyTo(M_prime.Slice(offset, MessageRepresentativePrefix.Length));
            offset += MessageRepresentativePrefix.Length;

            // Domain
            algorithm.DomainSeparator.AsSpan().CopyTo(M_prime.Slice(offset, algorithm.DomainSeparator.Length));
            offset += algorithm.DomainSeparator.Length;

            // len(ctx)
            M_prime[offset] = checked((byte)context.Length);
            offset++;

            // ctx
            context.CopyTo(M_prime.Slice(offset, context.Length));
            offset += context.Length;

            // r
            r.CopyTo(M_prime.Slice(offset, 32));
            offset += 32;

            Debug.Assert(offset + hashLength == _bytes.Length);

            _hash = IncrementalHash.CreateHash(algorithm.HashAlgorithmName);
        }

        public void AppendData(ReadOnlySpan<byte> data)
        {
            ThrowIfDisposed();

            _hash.AppendData(data);
        }

        public ReadOnlySpan<byte> GetMessageRepresentativeAndDispose()
        {
            ThrowIfDisposed();

            // PH( M )

#if NET
            _hash.GetHashAndReset(_bytes.AsSpan(_bytes.Length - _hash.HashLengthInBytes));
#else
            byte[] hashBytes = _hash.GetHashAndReset();
            hashBytes.CopyTo(_bytes.AsSpan(_bytes.Length - hashBytes.Length));
#endif
            return _bytes;
        }

        public void Dispose()
        {
            _hash?.Dispose();
            _hash = null!;
        }

        private void ThrowIfDisposed()
        {
#if NET
            ObjectDisposedException.ThrowIf(_hash is null, nameof(CompositeMLDsaMessageEncoder));
#else
            if (_hash is null)
            {
                throw new ObjectDisposedException(nameof(CompositeMLDsaMessageEncoder));
            }
#endif
        }
    }
}
