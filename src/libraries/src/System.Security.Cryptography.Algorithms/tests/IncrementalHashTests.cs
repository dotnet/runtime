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
        // Some arbitrarily chosen OID segments
        private static readonly byte[] s_hmacKey = { 2, 5, 29, 54, 1, 2, 84, 113, 54, 91, 1, 1, 2, 5, 29, 10, };
        private static readonly byte[] s_inputBytes = ByteUtils.RepeatByte(0xA5, 512);

        public static IEnumerable<object[]> GetHashAlgorithms()
        {
            return new[]
            {
                new object[] { MD5.Create(), HashAlgorithmName.MD5 },
                new object[] { SHA1.Create(), HashAlgorithmName.SHA1 },
                new object[] { SHA256.Create(), HashAlgorithmName.SHA256 },
                new object[] { SHA384.Create(), HashAlgorithmName.SHA384 },
                new object[] { SHA512.Create(), HashAlgorithmName.SHA512 },
            };
        }

        public static IEnumerable<object[]> GetHMACs()
        {
            return new[]
            {
                new object[] { new HMACMD5(), HashAlgorithmName.MD5 },
                new object[] { new HMACSHA1(), HashAlgorithmName.SHA1 },
                new object[] { new HMACSHA256(), HashAlgorithmName.SHA256 },
                new object[] { new HMACSHA384(), HashAlgorithmName.SHA384 },
                new object[] { new HMACSHA512(), HashAlgorithmName.SHA512 },
            };
        }

        [Fact]
        public static void InvalidArguments_Throw()
        {
            AssertExtensions.Throws<ArgumentException>("hashAlgorithm", () => IncrementalHash.CreateHash(new HashAlgorithmName(null)));
            AssertExtensions.Throws<ArgumentException>("hashAlgorithm", () => IncrementalHash.CreateHash(new HashAlgorithmName("")));

            AssertExtensions.Throws<ArgumentException>("hashAlgorithm", () => IncrementalHash.CreateHMAC(new HashAlgorithmName(null), new byte[1]));
            AssertExtensions.Throws<ArgumentException>("hashAlgorithm", () => IncrementalHash.CreateHMAC(new HashAlgorithmName(""), new byte[1]));

            AssertExtensions.Throws<ArgumentNullException>("key", () => IncrementalHash.CreateHMAC(HashAlgorithmName.SHA512, null));

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
    }
}
