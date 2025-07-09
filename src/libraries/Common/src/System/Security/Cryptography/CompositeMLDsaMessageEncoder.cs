// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Security.Cryptography;

namespace Internal.Cryptography
{
    internal static class CompositeMLDsaMessageEncoder
    {
        private static ReadOnlySpan<byte> MessageRepresentativePrefix => "CompositeAlgorithmSignatures2025"u8;

        // TODO move into managed CompositeMLDsa implementation
        // TODO the representative message will often be < 256 bytes so we should stackalloc with a callback
        internal static byte[] GetMessageRepresentative(
            CompositeMLDsaAlgorithm algorithm,
            ReadOnlySpan<byte> context,
            ReadOnlySpan<byte> r,
            ReadOnlySpan<byte> message)
        {
            Debug.Assert(r.Length is CompositeMLDsaAlgorithm.RandomizerSizeInBytes);

            // M' = Prefix || Domain || len(ctx) || ctx || r || PH( M )

            using (IncrementalHash hash = IncrementalHash.CreateHash(algorithm.HashAlgorithmName))
            {
                int length = checked(MessageRepresentativePrefix.Length +   // Prefix
                                     algorithm.DomainSeparator.Length +     // Domain
                                     1 +                                    // len(ctx)
                                     context.Length +                       // ctx
                                     r.Length +                             // r
#if NET
                                     hash.HashLengthInBytes);               // PH( M )
#else
                                     hash.GetHashLengthInBytes());          // PH( M )
#endif

                byte[] M_prime = new byte[length];

                int offset = 0;

                // Prefix
                MessageRepresentativePrefix.CopyTo(M_prime.AsSpan(offset, MessageRepresentativePrefix.Length));
                offset += MessageRepresentativePrefix.Length;

                // Domain
                algorithm.DomainSeparator.AsSpan().CopyTo(M_prime.AsSpan(offset, algorithm.DomainSeparator.Length));
                offset += algorithm.DomainSeparator.Length;

                // len(ctx)
                M_prime[offset] = checked((byte)context.Length);
                offset++;

                // ctx
                context.CopyTo(M_prime.AsSpan(offset, context.Length));
                offset += context.Length;

                // r
                r.CopyTo(M_prime.AsSpan(offset, r.Length));
                offset += r.Length;

                // PH( M )
                hash.AppendData(message);
#if NET
                hash.GetHashAndReset(M_prime.AsSpan(offset, hash.HashLengthInBytes));
                offset += hash.HashLengthInBytes;
#else
                byte[] hashBytes = hash.GetHashAndReset();
                hashBytes.CopyTo(M_prime.AsSpan(offset, hash.GetHashLengthInBytes()));
                offset += hash.GetHashLengthInBytes();
#endif

                Debug.Assert(offset == M_prime.Length);

                return M_prime;
            }
        }
    }
}
