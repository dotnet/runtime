// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Algorithms.Tests
{
    public partial class IncrementalHashTests
    {
        [Theory]
        [MemberData(nameof(GetHashAlgorithms))]
        public static void VerifyIncrementalHash_Span(HashAlgorithm referenceAlgorithm, HashAlgorithmName hashAlgorithm)
        {
            using (referenceAlgorithm)
            using (IncrementalHash incrementalHash = IncrementalHash.CreateHash(hashAlgorithm))
            {
                VerifyIncrementalResult_Span(referenceAlgorithm, incrementalHash);
            }
        }

        [Theory]
        [MemberData(nameof(GetHMACs))]
        public static void VerifyIncrementalHMAC_Span(HMAC referenceAlgorithm, HashAlgorithmName hashAlgorithm)
        {
            using (referenceAlgorithm)
            using (IncrementalHash incrementalHash = IncrementalHash.CreateHMAC(hashAlgorithm, s_hmacKey))
            {
                referenceAlgorithm.Key = s_hmacKey;
                VerifyIncrementalResult_Span(referenceAlgorithm, incrementalHash);
            }
        }

        private static void VerifyIncrementalResult_Span(HashAlgorithm referenceAlgorithm, IncrementalHash incrementalHash)
        {
            int referenceHashLength;
            byte[] referenceHash = new byte[1];
            while (!referenceAlgorithm.TryComputeHash(s_inputBytes, referenceHash, out referenceHashLength))
            {
                referenceHash = new byte[referenceHash.Length * 2];
            }

            const int StepA = 13;
            const int StepB = 7;
            int position = 0;

            while (position < s_inputBytes.Length - StepA)
            {
                incrementalHash.AppendData(new ReadOnlySpan<byte>(s_inputBytes, position, StepA));
                position += StepA;
            }

            incrementalHash.AppendData(new ReadOnlySpan<byte>(s_inputBytes, position, s_inputBytes.Length - position));

            byte[] incrementalA = new byte[referenceHashLength];
            int bytesWritten;
            Assert.True(incrementalHash.TryGetHashAndReset(incrementalA, out bytesWritten));
            Assert.Equal(referenceHashLength, bytesWritten);
            Assert.Equal<byte>(new Span<byte>(referenceHash, 0, referenceHashLength).ToArray(), new Span<byte>(incrementalA).Slice(0, bytesWritten).ToArray());

            // Now try again, verifying both immune to step size behaviors, and that GetHashAndReset resets.
            position = 0;

            while (position < s_inputBytes.Length - StepB)
            {
                incrementalHash.AppendData(new ReadOnlySpan<byte>(s_inputBytes, position, StepB));
                position += StepB;
            }

            incrementalHash.AppendData(new ReadOnlySpan<byte>(s_inputBytes, position, s_inputBytes.Length - position));

            byte[] incrementalB = new byte[referenceHashLength];
            Assert.True(incrementalHash.TryGetHashAndReset(incrementalB, out bytesWritten));
            Assert.Equal(referenceHashLength, bytesWritten);
            Assert.Equal<byte>(new Span<byte>(referenceHash, 0, referenceHashLength).ToArray(), incrementalB);
        }

        [Theory]
        [MemberData(nameof(GetHashAlgorithms))]
        public static void VerifyEmptyHash_Span(HashAlgorithm referenceAlgorithm, HashAlgorithmName hashAlgorithm)
        {
            using (referenceAlgorithm)
            using (IncrementalHash incrementalHash = IncrementalHash.CreateHash(hashAlgorithm))
            {
                for (int i = 0; i < 10; i++)
                {
                    incrementalHash.AppendData(ReadOnlySpan<byte>.Empty);
                }

                byte[] referenceHash = referenceAlgorithm.ComputeHash(Array.Empty<byte>());
                byte[] incrementalResult = new byte[referenceHash.Length];
                Assert.True(incrementalHash.TryGetHashAndReset(incrementalResult, out int bytesWritten));
                Assert.Equal(referenceHash.Length, bytesWritten);
                Assert.Equal(referenceHash, incrementalResult);
            }
        }

        [Theory]
        [MemberData(nameof(GetHMACs))]
        public static void VerifyEmptyHMAC_Span(HMAC referenceAlgorithm, HashAlgorithmName hashAlgorithm)
        {
            using (referenceAlgorithm)
            using (IncrementalHash incrementalHash = IncrementalHash.CreateHMAC(hashAlgorithm, s_hmacKey))
            {
                referenceAlgorithm.Key = s_hmacKey;

                for (int i = 0; i < 10; i++)
                {
                    incrementalHash.AppendData(ReadOnlySpan<byte>.Empty);
                }

                byte[] referenceHash = referenceAlgorithm.ComputeHash(Array.Empty<byte>());
                byte[] incrementalResult = new byte[referenceHash.Length];
                Assert.True(incrementalHash.TryGetHashAndReset(incrementalResult, out int bytesWritten));
                Assert.Equal(referenceHash.Length, bytesWritten);
                Assert.Equal(referenceHash, incrementalResult);
            }
        }

        [Theory]
        [MemberData(nameof(GetHashAlgorithms))]
        public static void VerifyTrivialHash_Span(HashAlgorithm referenceAlgorithm, HashAlgorithmName hashAlgorithm)
        {
            using (referenceAlgorithm)
            using (IncrementalHash incrementalHash = IncrementalHash.CreateHash(hashAlgorithm))
            {
                byte[] referenceHash = referenceAlgorithm.ComputeHash(Array.Empty<byte>());
                byte[] incrementalResult = new byte[referenceHash.Length];
                Assert.True(incrementalHash.TryGetHashAndReset(incrementalResult, out int bytesWritten));
                Assert.Equal(referenceHash.Length, bytesWritten);
                Assert.Equal(referenceHash, incrementalResult);
            }
        }

        [Theory]
        [MemberData(nameof(GetHMACs))]
        public static void VerifyTrivialHMAC_Span(HMAC referenceAlgorithm, HashAlgorithmName hashAlgorithm)
        {
            using (referenceAlgorithm)
            using (IncrementalHash incrementalHash = IncrementalHash.CreateHMAC(hashAlgorithm, s_hmacKey))
            {
                referenceAlgorithm.Key = s_hmacKey;

                byte[] referenceHash = referenceAlgorithm.ComputeHash(Array.Empty<byte>());
                byte[] incrementalResult = new byte[referenceHash.Length];
                Assert.True(incrementalHash.TryGetHashAndReset(incrementalResult, out int bytesWritten));
                Assert.Equal(referenceHash.Length, bytesWritten);
                Assert.Equal(referenceHash, incrementalResult);
            }
        }

        [Theory]
        [MemberData(nameof(GetHashAlgorithms))]
        public static void Dispose_HashAlgorithm_ThrowsException(HashAlgorithm referenceAlgorithm, HashAlgorithmName hashAlgorithm)
        {
            referenceAlgorithm.Dispose();
            var incrementalHash = IncrementalHash.CreateHash(hashAlgorithm);
            incrementalHash.Dispose();

            Assert.Throws<ObjectDisposedException>(() => incrementalHash.AppendData(new byte[1]));
            Assert.Throws<ObjectDisposedException>(() => incrementalHash.AppendData(new ReadOnlySpan<byte>(new byte[1])));

            Assert.Throws<ObjectDisposedException>(() => incrementalHash.GetHashAndReset());
            Assert.Throws<ObjectDisposedException>(() => incrementalHash.TryGetHashAndReset(new byte[1], out int _));
        }

        [Theory]
        [MemberData(nameof(GetHMACs))]
        public static void Dispose_HMAC_ThrowsException(HMAC referenceAlgorithm, HashAlgorithmName hashAlgorithm)
        {
            referenceAlgorithm.Dispose();
            var incrementalHash = IncrementalHash.CreateHMAC(hashAlgorithm, s_hmacKey);
            incrementalHash.Dispose();

            Assert.Throws<ObjectDisposedException>(() => incrementalHash.AppendData(new byte[1]));
            Assert.Throws<ObjectDisposedException>(() => incrementalHash.AppendData(new ReadOnlySpan<byte>(new byte[1])));

            Assert.Throws<ObjectDisposedException>(() => incrementalHash.GetHashAndReset());
            Assert.Throws<ObjectDisposedException>(() => incrementalHash.TryGetHashAndReset(new byte[1], out int _));
        }
    }
}
