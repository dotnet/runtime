// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    public class IncrementalHashTests
    {
        // Some arbitrarily chosen OID segments
        private static readonly byte[] s_hmacKey = { 2, 5, 29, 54, 1, 2, 84, 113, 54, 91, 1, 1, 2, 5, 29, 10, };
        private static readonly byte[] s_inputBytes = ByteUtils.RepeatByte(0xA5, 512);

        public static IEnumerable<object[]> GetHashAlgorithms()
        {
            if (PlatformDetection.IsNotBrowser)
            {
                // MD5 is not supported on Browser
                yield return new object[] { MD5.Create(), HashAlgorithmName.MD5 };
            }

            yield return new object[] { SHA1.Create(), HashAlgorithmName.SHA1 };
            yield return new object[] { SHA256.Create(), HashAlgorithmName.SHA256 };
            yield return new object[] { SHA384.Create(), HashAlgorithmName.SHA384 };
            yield return new object[] { SHA512.Create(), HashAlgorithmName.SHA512 };
        }

        public static IEnumerable<object[]> GetHMACs()
        {
            if (!PlatformDetection.IsBrowser)
            {
                yield return new object[] { new HMACMD5(), HashAlgorithmName.MD5 };
            }
            yield return new object[] { new HMACSHA1(), HashAlgorithmName.SHA1 };
            yield return new object[] { new HMACSHA256(), HashAlgorithmName.SHA256 };
            yield return new object[] { new HMACSHA384(), HashAlgorithmName.SHA384 };
            yield return new object[] { new HMACSHA512(), HashAlgorithmName.SHA512 };
        }

        [Fact]
        public static void InvalidArguments_Throw()
        {
            AssertExtensions.Throws<ArgumentNullException>("hashAlgorithm", () => IncrementalHash.CreateHash(new HashAlgorithmName(null)));
            AssertExtensions.Throws<ArgumentException>("hashAlgorithm", () => IncrementalHash.CreateHash(new HashAlgorithmName("")));

            if (PlatformDetection.IsNotBrowser)
            {
                // HMAC is not supported on Browser
                AssertExtensions.Throws<ArgumentNullException>("hashAlgorithm", () => IncrementalHash.CreateHMAC(new HashAlgorithmName(null), new byte[1]));
                AssertExtensions.Throws<ArgumentException>("hashAlgorithm", () => IncrementalHash.CreateHMAC(new HashAlgorithmName(""), new byte[1]));

                AssertExtensions.Throws<ArgumentNullException>("key", () => IncrementalHash.CreateHMAC(HashAlgorithmName.SHA512, null));
            }

            using (IncrementalHash incrementalHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA512))
            {
                AssertExtensions.Throws<ArgumentNullException>("data", () => incrementalHash.AppendData(null));
                AssertExtensions.Throws<ArgumentNullException>("data", () => incrementalHash.AppendData(null, 0, 0));

                AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => incrementalHash.AppendData(new byte[1], -1, 1));

                AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => incrementalHash.AppendData(new byte[1], 0, -1));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => incrementalHash.AppendData(new byte[1], 0, 2));

                Assert.Throws<ArgumentException>(() => incrementalHash.AppendData(new byte[2], 1, 2));
            }
        }

        [Theory]
        [MemberData(nameof(GetHashAlgorithms))]
        public static void VerifyIncrementalHash(HashAlgorithm referenceAlgorithm, HashAlgorithmName hashAlgorithm)
        {
            using (referenceAlgorithm)
            using (IncrementalHash incrementalHash = IncrementalHash.CreateHash(hashAlgorithm))
            {
                Assert.Equal(hashAlgorithm, incrementalHash.AlgorithmName);
                VerifyIncrementalResult(referenceAlgorithm, incrementalHash);
            }
        }

        [Theory]
        [MemberData(nameof(GetHMACs))]
        public static void VerifyIncrementalHMAC(HMAC referenceAlgorithm, HashAlgorithmName hashAlgorithm)
        {
            using (referenceAlgorithm)
            using (IncrementalHash incrementalHash = IncrementalHash.CreateHMAC(hashAlgorithm, s_hmacKey))
            {
                referenceAlgorithm.Key = s_hmacKey;

                VerifyIncrementalResult(referenceAlgorithm, incrementalHash);
            }
        }

        [Theory]
        [MemberData(nameof(GetHMACs))]
        public static void VerifyIncrementalHMAC_SpanKey(HMAC referenceAlgorithm, HashAlgorithmName hashAlgorithm)
        {
            using (referenceAlgorithm)
            using (IncrementalHash incrementalHash = IncrementalHash.CreateHMAC(hashAlgorithm, new ReadOnlySpan<byte>(s_hmacKey)))
            {
                referenceAlgorithm.Key = s_hmacKey;

                VerifyIncrementalResult(referenceAlgorithm, incrementalHash);
            }
        }

        private static void VerifyIncrementalResult(HashAlgorithm referenceAlgorithm, IncrementalHash incrementalHash)
        {
            byte[] referenceHash = referenceAlgorithm.ComputeHash(s_inputBytes);
            const int StepA = 13;
            const int StepB = 7;

            int position = 0;

            while (position < s_inputBytes.Length - StepA)
            {
                incrementalHash.AppendData(s_inputBytes, position, StepA);
                position += StepA;
            }

            incrementalHash.AppendData(s_inputBytes, position, s_inputBytes.Length - position);

            byte[] incrementalA = incrementalHash.GetHashAndReset();
            Assert.Equal(referenceHash, incrementalA);

            // Now try again, verifying both immune to step size behaviors, and that GetHashAndReset resets.
            position = 0;

            while (position < s_inputBytes.Length - StepB)
            {
                incrementalHash.AppendData(s_inputBytes, position, StepB);
                position += StepB;
            }

            incrementalHash.AppendData(s_inputBytes, position, s_inputBytes.Length - position);

            byte[] incrementalB = incrementalHash.GetHashAndReset();
            Assert.Equal(referenceHash, incrementalB);
        }

        [Theory]
        [MemberData(nameof(GetHashAlgorithms))]
        public static void VerifyEmptyHash(HashAlgorithm referenceAlgorithm, HashAlgorithmName hashAlgorithm)
        {
            using (referenceAlgorithm)
            using (IncrementalHash incrementalHash = IncrementalHash.CreateHash(hashAlgorithm))
            {
                for (int i = 0; i < 10; i++)
                {
                    incrementalHash.AppendData(Array.Empty<byte>());
                }

                byte[] referenceHash = referenceAlgorithm.ComputeHash(Array.Empty<byte>());
                byte[] incrementalResult = incrementalHash.GetHashAndReset();

                Assert.Equal(referenceHash, incrementalResult);
            }
        }

        [Theory]
        [MemberData(nameof(GetHMACs))]
        public static void VerifyEmptyHMAC(HMAC referenceAlgorithm, HashAlgorithmName hashAlgorithm)
        {
            using (referenceAlgorithm)
            using (IncrementalHash incrementalHash = IncrementalHash.CreateHMAC(hashAlgorithm, s_hmacKey))
            {
                referenceAlgorithm.Key = s_hmacKey;

                for (int i = 0; i < 10; i++)
                {
                    incrementalHash.AppendData(Array.Empty<byte>());
                }

                byte[] referenceHash = referenceAlgorithm.ComputeHash(Array.Empty<byte>());
                byte[] incrementalResult = incrementalHash.GetHashAndReset();

                Assert.Equal(referenceHash, incrementalResult);
            }
        }

        [Theory]
        [MemberData(nameof(GetHashAlgorithms))]
        public static void VerifyTrivialHash(HashAlgorithm referenceAlgorithm, HashAlgorithmName hashAlgorithm)
        {
            using (referenceAlgorithm)
            using (IncrementalHash incrementalHash = IncrementalHash.CreateHash(hashAlgorithm))
            {
                byte[] referenceHash = referenceAlgorithm.ComputeHash(Array.Empty<byte>());
                byte[] incrementalResult = incrementalHash.GetHashAndReset();

                Assert.Equal(referenceHash, incrementalResult);
            }
        }

        [Theory]
        [MemberData(nameof(GetHMACs))]
        public static void VerifyTrivialHMAC(HMAC referenceAlgorithm, HashAlgorithmName hashAlgorithm)
        {
            using (referenceAlgorithm)
            using (IncrementalHash incrementalHash = IncrementalHash.CreateHMAC(hashAlgorithm, s_hmacKey))
            {
                referenceAlgorithm.Key = s_hmacKey;

                byte[] referenceHash = referenceAlgorithm.ComputeHash(Array.Empty<byte>());
                byte[] incrementalResult = incrementalHash.GetHashAndReset();

                Assert.Equal(referenceHash, incrementalResult);
            }
        }

        [Fact]
        public static void AppendDataAfterHashClose()
        {
            using (IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
            {
                byte[] firstHash = hash.GetHashAndReset();

                hash.AppendData(Array.Empty<byte>());
                byte[] secondHash = hash.GetHashAndReset();

                Assert.Equal(firstHash, secondHash);
            }
        }

        [Fact]
        public static void AppendDataAfterHMACClose()
        {
            using (IncrementalHash hash = IncrementalHash.CreateHMAC(HashAlgorithmName.SHA256, s_hmacKey))
            {
                byte[] firstHash = hash.GetHashAndReset();

                hash.AppendData(Array.Empty<byte>());
                byte[] secondHash = hash.GetHashAndReset();

                Assert.Equal(firstHash, secondHash);
            }
        }

        [Fact]
        public static void GetHashTwice()
        {
            using (IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
            {
                byte[] firstHash = hash.GetHashAndReset();
                byte[] secondHash = hash.GetHashAndReset();

                Assert.Equal(firstHash, secondHash);
            }
        }

        [Fact]
        public static void GetHMACTwice()
        {
            using (IncrementalHash hash = IncrementalHash.CreateHMAC(HashAlgorithmName.SHA256, s_hmacKey))
            {
                byte[] firstHash = hash.GetHashAndReset();
                byte[] secondHash = hash.GetHashAndReset();

                Assert.Equal(firstHash, secondHash);
            }
        }

        [Fact]
        public static void ModifyAfterHashDispose()
        {
            using (IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
            {
                hash.Dispose();
                Assert.Throws<ObjectDisposedException>(() => hash.AppendData(Array.Empty<byte>()));
                Assert.Throws<ObjectDisposedException>(() => hash.GetHashAndReset());
            }
        }

        [Fact]
        public static void ModifyAfterHMACDispose()
        {
            using (IncrementalHash hash = IncrementalHash.CreateHMAC(HashAlgorithmName.SHA256, s_hmacKey))
            {
                hash.Dispose();
                Assert.Throws<ObjectDisposedException>(() => hash.AppendData(Array.Empty<byte>()));
                Assert.Throws<ObjectDisposedException>(() => hash.GetHashAndReset());
            }
        }

        [Fact]
        public static void UnknownDigestAlgorithm()
        {
            Assert.ThrowsAny<CryptographicException>(
                () => IncrementalHash.CreateHash(new HashAlgorithmName("SHA0")));
        }

        [Fact]
        public static void UnknownHmacAlgorithm()
        {
            Assert.ThrowsAny<CryptographicException>(
                () => IncrementalHash.CreateHMAC(new HashAlgorithmName("SHA0"), Array.Empty<byte>()));
        }

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

            byte[] tmpDest = new byte[1];

            Assert.Throws<ObjectDisposedException>(() => incrementalHash.AppendData(tmpDest));
            Assert.Throws<ObjectDisposedException>(() => incrementalHash.AppendData(tmpDest, 0, 0));
            Assert.Throws<ObjectDisposedException>(() => incrementalHash.AppendData(new ReadOnlySpan<byte>(tmpDest)));

            Assert.Throws<ObjectDisposedException>(() => incrementalHash.GetHashAndReset());
            Assert.Throws<ObjectDisposedException>(() => incrementalHash.GetHashAndReset(tmpDest));
            Assert.Throws<ObjectDisposedException>(() => incrementalHash.TryGetHashAndReset(tmpDest, out int _));

            Assert.Throws<ObjectDisposedException>(() => incrementalHash.GetCurrentHash());
            Assert.Throws<ObjectDisposedException>(() => incrementalHash.GetCurrentHash(tmpDest));
            Assert.Throws<ObjectDisposedException>(() => incrementalHash.TryGetCurrentHash(tmpDest, out int _));
        }

        [Theory]
        [MemberData(nameof(GetHMACs))]
        public static void Dispose_HMAC_ThrowsException(HMAC referenceAlgorithm, HashAlgorithmName hashAlgorithm)
        {
            referenceAlgorithm.Dispose();
            var incrementalHash = IncrementalHash.CreateHMAC(hashAlgorithm, s_hmacKey);
            incrementalHash.Dispose();

            byte[] tmpDest = new byte[1];

            Assert.Throws<ObjectDisposedException>(() => incrementalHash.AppendData(tmpDest));
            Assert.Throws<ObjectDisposedException>(() => incrementalHash.AppendData(tmpDest, 0, 0));
            Assert.Throws<ObjectDisposedException>(() => incrementalHash.AppendData(new ReadOnlySpan<byte>(tmpDest)));

            Assert.Throws<ObjectDisposedException>(() => incrementalHash.GetHashAndReset());
            Assert.Throws<ObjectDisposedException>(() => incrementalHash.GetHashAndReset(tmpDest));
            Assert.Throws<ObjectDisposedException>(() => incrementalHash.TryGetHashAndReset(tmpDest, out int _));

            Assert.Throws<ObjectDisposedException>(() => incrementalHash.GetCurrentHash());
            Assert.Throws<ObjectDisposedException>(() => incrementalHash.GetCurrentHash(tmpDest));
            Assert.Throws<ObjectDisposedException>(() => incrementalHash.TryGetCurrentHash(tmpDest, out int _));
        }

        [Theory]
        [MemberData(nameof(GetHashAlgorithms))]
        public static void VerifyGetCurrentHash_Digest(HashAlgorithm referenceAlgorithm, HashAlgorithmName hashAlgorithm)
        {
            referenceAlgorithm.Dispose();

            using (IncrementalHash single = IncrementalHash.CreateHash(hashAlgorithm))
            using (IncrementalHash accumulated = IncrementalHash.CreateHash(hashAlgorithm))
            {
                VerifyGetCurrentHash(single, accumulated);
            }
        }

        [Theory]
        [SkipOnPlatform(TestPlatforms.Android, "Android doesn't support cloning the current state for HMAC, so it doesn't support GetCurrentHash.")]
        [MemberData(nameof(GetHMACs))]
        public static void VerifyGetCurrentHash_HMAC(HMAC referenceAlgorithm, HashAlgorithmName hashAlgorithm)
        {
            referenceAlgorithm.Dispose();

            using (IncrementalHash single = IncrementalHash.CreateHMAC(hashAlgorithm, s_hmacKey))
            using (IncrementalHash accumulated = IncrementalHash.CreateHMAC(hashAlgorithm, s_hmacKey))
            {
                VerifyGetCurrentHash(single, accumulated);
            }
        }

        [Theory]
        [SkipOnPlatform(TestPlatforms.Android, "Android doesn't support cloning the current state for HMAC, so it doesn't support GetCurrentHash.")]
        [MemberData(nameof(GetHMACs))]
        public static void VerifyBounds_GetCurrentHash_HMAC(HMAC referenceAlgorithm, HashAlgorithmName hashAlgorithm)
        {
            using (referenceAlgorithm)
            using (IncrementalHash incremental = IncrementalHash.CreateHMAC(hashAlgorithm, s_hmacKey))
            {
                referenceAlgorithm.Key = s_hmacKey;
                byte[] comparison = referenceAlgorithm.ComputeHash(Array.Empty<byte>());

                VerifyBounds(
                    comparison,
                    incremental,
                    inc => inc.GetCurrentHash(),
                    (inc, dest) => inc.GetCurrentHash(dest),
                    (IncrementalHash inc, Span<byte> dest, out int bytesWritten) =>
                        inc.TryGetCurrentHash(dest, out bytesWritten));
            }
        }

        [Theory]
        [MemberData(nameof(GetHMACs))]
        public static void VerifyBounds_GetHashAndReset_HMAC(HMAC referenceAlgorithm, HashAlgorithmName hashAlgorithm)
        {
            using (referenceAlgorithm)
            using (IncrementalHash incremental = IncrementalHash.CreateHMAC(hashAlgorithm, s_hmacKey))
            {
                referenceAlgorithm.Key = s_hmacKey;
                byte[] comparison = referenceAlgorithm.ComputeHash(Array.Empty<byte>());

                VerifyBounds(
                    comparison,
                    incremental,
                    inc => inc.GetHashAndReset(),
                    (inc, dest) => inc.GetHashAndReset(dest),
                    (IncrementalHash inc, Span<byte> dest, out int bytesWritten) =>
                        inc.TryGetHashAndReset(dest, out bytesWritten));
            }
        }

        [Theory]
        [MemberData(nameof(GetHashAlgorithms))]
        public static void VerifyBounds_GetCurrentHash_Hash(HashAlgorithm referenceAlgorithm, HashAlgorithmName hashAlgorithm)
        {
            using (referenceAlgorithm)
            using (IncrementalHash incremental = IncrementalHash.CreateHash(hashAlgorithm))
            {
                byte[] comparison = referenceAlgorithm.ComputeHash(Array.Empty<byte>());

                VerifyBounds(
                    comparison,
                    incremental,
                    inc => inc.GetCurrentHash(),
                    (inc, dest) => inc.GetCurrentHash(dest),
                    (IncrementalHash inc, Span<byte> dest, out int bytesWritten) =>
                        inc.TryGetCurrentHash(dest, out bytesWritten));
            }
        }

        [Theory]
        [MemberData(nameof(GetHashAlgorithms))]
        public static void VerifyBounds_GetHashAndReset_Hash(HashAlgorithm referenceAlgorithm, HashAlgorithmName hashAlgorithm)
        {
            using (referenceAlgorithm)
            using (IncrementalHash incremental = IncrementalHash.CreateHash(hashAlgorithm))
            {
                byte[] comparison = referenceAlgorithm.ComputeHash(Array.Empty<byte>());

                VerifyBounds(
                    comparison,
                    incremental,
                    inc => inc.GetHashAndReset(),
                    (inc, dest) => inc.GetHashAndReset(dest),
                    (IncrementalHash inc, Span<byte> dest, out int bytesWritten) =>
                        inc.TryGetHashAndReset(dest, out bytesWritten));
            }
        }

        private static void VerifyGetCurrentHash(IncrementalHash single, IncrementalHash accumulated)
        {
            Span<byte> buf = stackalloc byte[2048];
            Span<byte> fullDigest = stackalloc byte[512 / 8];
            Span<byte> curDigest = stackalloc byte[fullDigest.Length];
            SequenceFill(buf);

            int count = 0;
            const int Step = 13;
            int writtenA;
            int writtenB;

            while (count + Step < buf.Length)
            {
                // Accumulate only the current slice
                accumulated.AppendData(buf.Slice(count, Step));

                // The comparison needs the whole thing, since we're
                // comparing GetHashAndReset vs GetCurrentHash.
                count += Step;
                single.AppendData(buf.Slice(0, count));

                writtenA = single.GetHashAndReset(fullDigest);
                writtenB = accumulated.GetCurrentHash(curDigest);

                Assert.Equal(
                    fullDigest.Slice(0, writtenA).ByteArrayToHex(),
                    curDigest.Slice(0, writtenB).ByteArrayToHex());
            }

            accumulated.AppendData(buf.Slice(count));
            single.AppendData(buf);

            writtenA = single.GetHashAndReset(fullDigest);

            // Drain/reset accumulated with this last call
            writtenB = accumulated.GetHashAndReset(curDigest);

            Assert.Equal(
                fullDigest.Slice(0, writtenA).ByteArrayToHex(),
                curDigest.Slice(0, writtenB).ByteArrayToHex());
        }

        private delegate int SpanWriter(
            IncrementalHash incremental,
            Span<byte> destination);

        private delegate bool TrySpanWriter(
            IncrementalHash incremental,
            Span<byte> destination,
            out int bytesWritten);

        private static void VerifyBounds(
            byte[] comparison,
            IncrementalHash incremental,
            Func<IncrementalHash, byte[]> oneShot,
            SpanWriter spanWriter,
            TrySpanWriter trySpanWriter)
        {
            string comparisonHex = comparison.ByteArrayToHex();
            Span<byte> dest = stackalloc byte[512 / 8 + 1];

            // Empty
            Assert.False(trySpanWriter(incremental, Span<byte>.Empty, out int written));
            Assert.Equal(0, written);

            AssertExtensions.Throws<ArgumentException>(
                "destination",
                () => spanWriter(incremental, Array.Empty<byte>()));

            // HashLengthInBytes - 1
            Assert.False(
                trySpanWriter(incremental, comparison.AsSpan(0, comparison.Length - 1), out written));
            Assert.Equal(0, written);

            AssertExtensions.Throws<ArgumentException>(
                "destination",
                () => spanWriter(incremental, comparison.AsSpan(0, comparison.Length - 1)));

            // Ensure comparison wasn't overwritten
            Assert.Equal(comparisonHex, comparison.ByteArrayToHex());

            // > HashLengthInBytes
            Assert.True(trySpanWriter(incremental, dest, out written));
            Assert.Equal(comparison.Length, written);
            Assert.Equal(incremental.HashLengthInBytes, written);
            Assert.Equal(comparisonHex, dest.Slice(0, written).ByteArrayToHex());

            dest.Clear();
            written = spanWriter(incremental, dest);
            Assert.Equal(comparison.Length, written);
            Assert.Equal(incremental.HashLengthInBytes, written);
            Assert.Equal(comparisonHex, dest.Slice(0, written).ByteArrayToHex());

            // == HashLengthInBytes
            dest = dest.Slice(0, written);
            dest.Clear();

            Assert.True(trySpanWriter(incremental, dest, out written));
            Assert.Equal(comparison.Length, written);
            Assert.Equal(incremental.HashLengthInBytes, written);
            Assert.Equal(comparisonHex, dest.Slice(0, written).ByteArrayToHex());

            dest.Clear();
            written = spanWriter(incremental, dest);
            Assert.Equal(comparison.Length, written);
            Assert.Equal(incremental.HashLengthInBytes, written);
            Assert.Equal(comparisonHex, dest.Slice(0, written).ByteArrayToHex());

            byte[] returned = oneShot(incremental);
            Assert.Equal(comparison.Length, returned.Length);
            Assert.Equal(incremental.HashLengthInBytes, returned.Length);
            Assert.Equal(comparisonHex, returned.ByteArrayToHex());
        }

        private static void SequenceFill(Span<byte> span)
        {
            for (int i = 0; i < span.Length; i++)
            {
                span[i] = (byte)i;
            }
        }
    }
}
