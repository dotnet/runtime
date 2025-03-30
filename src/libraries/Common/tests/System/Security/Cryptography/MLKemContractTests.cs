// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;
using Xunit.Sdk;

namespace System.Security.Cryptography.Tests
{
    public static class MLKemContractTests
    {
        [Fact]
        public static void Constructor_ThrowsForNullAlgorithm()
        {
            AssertExtensions.Throws<ArgumentNullException>("algorithm", static () => new MLKemContract(null));
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void Constructor_SetsAlgorithmProperty(MLKemAlgorithm algorithm)
        {
            using MLKemContract kem = new(algorithm);
            Assert.Equal(algorithm, kem.Algorithm);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(7)]
        public static void Dispose_OnDisposing(int disposeCalls)
        {
            int count = 0;
            MLKemContract kem = new(MLKemAlgorithm.MLKem512)
            {
                OnDispose = (bool disposing) =>
                {
                    count++;
                    AssertExtensions.TrueExpression(disposing);
                }
            };

            for (int i = 0; i < disposeCalls; i++)
            {
                kem.Dispose();
            }

            Assert.Equal(1, count);
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void Encapsulate_Exact_WrongCiphertextLength(MLKemAlgorithm algorithm)
        {
            using MLKemContract kem = new(algorithm);

            // Ciphertext too small
            AssertExtensions.Throws<ArgumentException>("ciphertext", () => kem.Encapsulate(
                new byte[algorithm.CiphertextSizeInBytes - 1],
                new byte[algorithm.SharedSecretSizeInBytes]));

            // Ciphertext too large
            AssertExtensions.Throws<ArgumentException>("ciphertext", () => kem.Encapsulate(
                new byte[algorithm.CiphertextSizeInBytes + 1],
                new byte[algorithm.SharedSecretSizeInBytes]));
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void Encapsulate_Exact_WrongSharedSecretLength(MLKemAlgorithm algorithm)
        {
            using MLKemContract kem = new(algorithm);

            // Shared secret too small
            AssertExtensions.Throws<ArgumentException>("sharedSecret", () => kem.Encapsulate(
                new byte[algorithm.CiphertextSizeInBytes],
                new byte[algorithm.SharedSecretSizeInBytes - 1]));

            // Shared secret too large
            AssertExtensions.Throws<ArgumentException>("sharedSecret", () => kem.Encapsulate(
                new byte[algorithm.CiphertextSizeInBytes],
                new byte[algorithm.SharedSecretSizeInBytes + 1]));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void Encapsulate_Exact_OverlappingBuffers(bool partial)
        {
            using MLKemContract kem = new(MLKemAlgorithm.MLKem512);
            byte[] buffer = new byte[MLKemAlgorithm.MLKem512.CiphertextSizeInBytes];

            Assert.Throws<CryptographicException>(() =>
            {
                Span<byte> sharedSecret = buffer.AsSpan(partial ? 1 : 0, MLKemAlgorithm.MLKem512.SharedSecretSizeInBytes);
                Span<byte> ciphertext = buffer.AsSpan(0, MLKemAlgorithm.MLKem512.CiphertextSizeInBytes);
                kem.Encapsulate(ciphertext, sharedSecret);
            });
        }

        [Fact]
        public static void Encapsulate_Exact_Disposed()
        {
            using MLKemContract kem = new(MLKemAlgorithm.MLKem512);
            kem.Dispose();
            Assert.Throws<ObjectDisposedException>(() => kem.Encapsulate(
                new byte[kem.Algorithm.CiphertextSizeInBytes],
                new byte[kem.Algorithm.SharedSecretSizeInBytes]));
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void Encapsulate_Exact_DistinctBuffers_Works(MLKemAlgorithm algorithm)
        {
            byte[] ciphertextBuffer = new byte[algorithm.CiphertextSizeInBytes];
            byte[] sharedSecretBuffer = new byte[algorithm.SharedSecretSizeInBytes];
            using MLKemContract kem = new(algorithm)
            {
                OnEncapsulateCore = (Span<byte> ciphertext, Span<byte> sharedSecret) =>
                {
                    AssertSameBuffer(ciphertext, ciphertextBuffer);
                    AssertSameBuffer(sharedSecret, sharedSecretBuffer);
                }
            };

            kem.Encapsulate(ciphertextBuffer, sharedSecretBuffer);
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void Encapsulate_Exact_SameNonOverlappingBuffer_Works(MLKemAlgorithm algorithm)
        {
            byte[] buffer = new byte[algorithm.CiphertextSizeInBytes + algorithm.SharedSecretSizeInBytes];
            Memory<byte> ciphertextBuffer = buffer.AsMemory(0, algorithm.CiphertextSizeInBytes);
            Memory<byte> sharedSecretBuffer = buffer.AsMemory(algorithm.CiphertextSizeInBytes, algorithm.SharedSecretSizeInBytes);
            using MLKemContract kem = new(algorithm)
            {
                OnEncapsulateCore = (Span<byte> ciphertext, Span<byte> sharedSecret) =>
                {
                    AssertSameBuffer(ciphertext, ciphertextBuffer.Span);
                    AssertSameBuffer(sharedSecret, sharedSecretBuffer.Span);
                }
            };

            kem.Encapsulate(ciphertextBuffer.Span, sharedSecretBuffer.Span);
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void Encapsulate_Written_DistinctBufferSameLength_Works(MLKemAlgorithm algorithm)
        {
            byte[] ciphertextBuffer = new byte[algorithm.CiphertextSizeInBytes];
            byte[] sharedSecretBuffer = new byte[algorithm.SharedSecretSizeInBytes];
            using MLKemContract kem = new(algorithm)
            {
                OnEncapsulateCore = (Span<byte> ciphertext, Span<byte> sharedSecret) =>
                {
                    AssertSameBuffer(ciphertext, ciphertextBuffer);
                    AssertSameBuffer(sharedSecret, sharedSecretBuffer);
                }
            };

            kem.Encapsulate(
                ciphertextBuffer,
                sharedSecretBuffer,
                out int ciphertextBytesWritten,
                out int sharedSecretBytesWritten);

            Assert.Equal(algorithm.CiphertextSizeInBytes, ciphertextBytesWritten);
            Assert.Equal(algorithm.SharedSecretSizeInBytes, sharedSecretBytesWritten);
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void Encapsulate_Written_DistinctBufferOversized_Works(MLKemAlgorithm algorithm)
        {
            byte[] ciphertextBuffer = new byte[algorithm.CiphertextSizeInBytes + 42];
            byte[] sharedSecretBuffer = new byte[algorithm.SharedSecretSizeInBytes + 42];
            using MLKemContract kem = new(algorithm)
            {
                OnEncapsulateCore = (Span<byte> ciphertext, Span<byte> sharedSecret) =>
                {
                    AssertSameBuffer(ciphertext, ciphertextBuffer.AsSpan(0, algorithm.CiphertextSizeInBytes));
                    AssertSameBuffer(sharedSecret, sharedSecretBuffer.AsSpan(0, algorithm.SharedSecretSizeInBytes));
                }
            };

            kem.Encapsulate(
                ciphertextBuffer,
                sharedSecretBuffer,
                out int ciphertextBytesWritten,
                out int sharedSecretBytesWritten);

            Assert.Equal(algorithm.CiphertextSizeInBytes, ciphertextBytesWritten);
            Assert.Equal(algorithm.SharedSecretSizeInBytes, sharedSecretBytesWritten);
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void Encapsulate_Written_UndersizedCiphertextBuffer(MLKemAlgorithm algorithm)
        {
            byte[] ciphertextBuffer = new byte[algorithm.CiphertextSizeInBytes - 1];
            byte[] sharedSecretBuffer = new byte[algorithm.SharedSecretSizeInBytes];
            using MLKemContract kem = new(algorithm);

            Assert.Throws<ArgumentException>("ciphertext", () => kem.Encapsulate(
                ciphertextBuffer,
                sharedSecretBuffer,
                out _,
                out _));
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void Encapsulate_Written_UndersizedSharedSecretBuffer(MLKemAlgorithm algorithm)
        {
            byte[] ciphertextBuffer = new byte[algorithm.CiphertextSizeInBytes];
            byte[] sharedSecretBuffer = new byte[algorithm.SharedSecretSizeInBytes - 1];
            using MLKemContract kem = new(algorithm);

            Assert.Throws<ArgumentException>("sharedSecret", () => kem.Encapsulate(
                ciphertextBuffer,
                sharedSecretBuffer,
                out _,
                out _));
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void Encapsulate_Written_Overlaps_WhenTrimmed_Works(MLKemAlgorithm algorithm)
        {
            // sharedSecret does overlap ciphertext in this test. However, the part that overlaps will never
            // be written to because it is trimmed to the exact size, which ends up with the buffers beside each
            // other, which should work.
            byte[] buffer = new byte[algorithm.SharedSecretSizeInBytes + algorithm.CiphertextSizeInBytes + 10];
            Memory<byte> sharedSecretBuffer = buffer.AsMemory(0, algorithm.SharedSecretSizeInBytes + 10);
            Memory<byte> ciphertextBuffer = buffer.AsMemory(algorithm.SharedSecretSizeInBytes);
            AssertExtensions.TrueExpression(sharedSecretBuffer.Span.Overlaps(ciphertextBuffer.Span));

            using MLKemContract kem = new(algorithm)
            {
                OnEncapsulateCore = (Span<byte> ciphertext, Span<byte> sharedSecret) =>
                {
                    AssertSameBuffer(ciphertext, ciphertextBuffer.Span.Slice(0, algorithm.CiphertextSizeInBytes));
                    AssertSameBuffer(sharedSecret, sharedSecretBuffer.Span.Slice(0, algorithm.SharedSecretSizeInBytes));
                    AssertExtensions.FalseExpression(ciphertext.Overlaps(sharedSecret));
                }
            };

            kem.Encapsulate(
                ciphertextBuffer.Span,
                sharedSecretBuffer.Span,
                out int ciphertextWritten,
                out int sharedSecretWritten);

            Assert.Equal(algorithm.CiphertextSizeInBytes, ciphertextWritten);
            Assert.Equal(algorithm.SharedSecretSizeInBytes, sharedSecretWritten);
        }

        [Fact]
        public static void Encapsulate_Written_Disposed()
        {
            MLKemContract kem = new(MLKemAlgorithm.MLKem512);
            kem.Dispose();
            Assert.Throws<ObjectDisposedException>(() => kem.Encapsulate(
                new byte[kem.Algorithm.CiphertextSizeInBytes],
                new byte[kem.Algorithm.SharedSecretSizeInBytes],
                out _,
                out _));
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void Encapsulate_Allocated(MLKemAlgorithm algorithm)
        {
            using MLKemContract kem = new(algorithm)
            {
                OnEncapsulateCore = (Span<byte> ciphertext, Span<byte> sharedSecret) =>
                {
                    ciphertext.Fill(0xCA);
                    sharedSecret.Fill(0xFE);
                }
            };

            byte[] ciphertext = kem.Encapsulate(out byte[] sharedSecret);

            Assert.Equal(algorithm.CiphertextSizeInBytes, ciphertext.Length);
            Assert.Equal(algorithm.SharedSecretSizeInBytes, sharedSecret.Length);
            AssertExtensions.FilledWith<byte>(0xCA, ciphertext);
            AssertExtensions.FilledWith<byte>(0xFE, sharedSecret);
        }

        [Fact]
        public static void Encapsulate_Allocated_Disposed()
        {
            MLKemContract kem = new(MLKemAlgorithm.MLKem512);
            kem.Dispose();
            Assert.Throws<ObjectDisposedException>(() => kem.Encapsulate(out _));
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void Decapsulate_Exact_WrongCiphertextLength(MLKemAlgorithm algorithm)
        {
            using MLKemContract kem = new(algorithm);

            // Ciphertext too small
            AssertExtensions.Throws<ArgumentException>("ciphertext", () => kem.Decapsulate(
                new byte[algorithm.CiphertextSizeInBytes - 1],
                new byte[algorithm.SharedSecretSizeInBytes]));

            // Ciphertext too large
            AssertExtensions.Throws<ArgumentException>("ciphertext", () => kem.Decapsulate(
                new byte[algorithm.CiphertextSizeInBytes + 1],
                new byte[algorithm.SharedSecretSizeInBytes]));
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void Decapsulate_Exact_WrongSharedSecretLength(MLKemAlgorithm algorithm)
        {
            using MLKemContract kem = new(algorithm);

            // Shared secret too small
            AssertExtensions.Throws<ArgumentException>("sharedSecret", () => kem.Decapsulate(
                new byte[algorithm.CiphertextSizeInBytes],
                new byte[algorithm.SharedSecretSizeInBytes - 1]));

            // Shared secret too large
            AssertExtensions.Throws<ArgumentException>("sharedSecret", () => kem.Decapsulate(
                new byte[algorithm.CiphertextSizeInBytes],
                new byte[algorithm.SharedSecretSizeInBytes + 1]));
        }

        [Fact]
        public static void Decapsulate_Exact_Disposed()
        {
            MLKemContract kem = new(MLKemAlgorithm.MLKem512);
            kem.Dispose();
            Assert.Throws<ObjectDisposedException>(() => kem.Encapsulate(out _));
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void Decapsulate_Exact_SeparateBuffers_Works(MLKemAlgorithm algorithm)
        {
            byte[] ciphertextBuffer = new byte[algorithm.CiphertextSizeInBytes];
            byte[] sharedSecretBuffer = new byte[algorithm.SharedSecretSizeInBytes];

            using MLKemContract kem = new(algorithm)
            {
                OnDecapsulateCore = (ReadOnlySpan<byte> ciphertext, Span<byte> sharedSecret) =>
                {
                    AssertSameBuffer(ciphertextBuffer, ciphertext);
                    AssertSameBuffer(sharedSecretBuffer, sharedSecret);
                }
            };

            kem.Decapsulate(ciphertextBuffer, sharedSecretBuffer);
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void Decapsulate_Exact_OverlappingBuffers_Works(MLKemAlgorithm algorithm)
        {
            byte[] buffer = new byte[Math.Max(algorithm.CiphertextSizeInBytes, algorithm.SharedSecretSizeInBytes)];
            Memory<byte> ciphertextBuffer = buffer.AsMemory(0, algorithm.CiphertextSizeInBytes);
            Memory<byte> sharedSecretBuffer = buffer.AsMemory(0, algorithm.SharedSecretSizeInBytes);

            using MLKemContract kem = new(algorithm)
            {
                OnDecapsulateCore = (ReadOnlySpan<byte> ciphertext, Span<byte> sharedSecret) =>
                {
                    AssertSameBuffer(ciphertextBuffer.Span, ciphertext);
                    AssertSameBuffer(sharedSecretBuffer.Span, sharedSecret);
                }
            };

            kem.Decapsulate(ciphertextBuffer.Span, sharedSecretBuffer.Span);
        }

        private static void AssertSameBuffer(ReadOnlySpan<byte> buffer1, ReadOnlySpan<byte> buffer2)
        {
            if (buffer1.Length != buffer2.Length)
            {
                Assert.Fail("Expected buffers to have same length. " +
                    $"The first buffer's length {buffer1.Length} does not match the second buffer's length {buffer2.Length}.");
            }

            if (!buffer1.Overlaps(buffer2, out int offset) || offset != 0)
            {
                Assert.Fail("Expected buffers to be the same memory location, but were not.");
            }

        }
    }

    internal sealed class MLKemContract : MLKem
    {
        internal DecapsulateCoreCallback OnDecapsulateCore { get; set; }
        internal EncapsulateCoreCallback OnEncapsulateCore { get; set; }
        internal ExportKeyCoreCallback OnExportPrivateSeedCore { get; set; }
        internal ExportKeyCoreCallback OnExportEncapsulationKeyCore { get; set; }
        internal ExportKeyCoreCallback OnExportDecapsulationKeyCore { get; set; }
        internal Action<bool> OnDispose { get; set; } = (bool disposing) => { };

        private int DecapsulateCoreCount { get; set; }
        private int EncapsulateCoreCount { get; set; }
        private int ExportPrivateSeedCoreCount { get; set; }
        private int ExportEncapsulationKeyCoreCount { get; set; }
        private int ExportDecapsulationKeyCoreCount { get; set; }

        private bool _disposed;

        public MLKemContract(MLKemAlgorithm algorithm) : base(algorithm)
        {
        }

        protected override void DecapsulateCore(ReadOnlySpan<byte> ciphertext, Span<byte> sharedSecret)
        {
            DecapsulateCoreCount++;
            GetCallback(OnDecapsulateCore)(ciphertext, sharedSecret);
        }

        protected override void EncapsulateCore(Span<byte> ciphertext, Span<byte> sharedSecret)
        {
            EncapsulateCoreCount++;
            GetCallback(OnEncapsulateCore)(ciphertext, sharedSecret);
        }

        protected override void ExportPrivateSeedCore(Span<byte> destination)
        {
            ExportPrivateSeedCoreCount++;
            GetCallback(OnExportPrivateSeedCore)(destination);
        }

        protected override void ExportDecapsulationKeyCore(Span<byte> destination)
        {
            ExportDecapsulationKeyCoreCount++;
            GetCallback(OnExportDecapsulationKeyCore)(destination);
        }

        protected override void ExportEncapsulationKeyCore(Span<byte> destination)
        {
            ExportEncapsulationKeyCoreCount++;
            GetCallback(OnExportEncapsulationKeyCore)(destination);
        }

        protected override void Dispose(bool disposing)
        {
            GetCallback(OnDispose)(disposing);
            VerifyCalledOnDispose();
            _disposed = true;
        }

        private void VerifyCalledOnDispose()
        {
            if (OnDecapsulateCore is not null && DecapsulateCoreCount == 0)
            {
                Assert.Fail($"Expected call to {nameof(DecapsulateCore)}.");
            }
            if (OnEncapsulateCore is not null && EncapsulateCoreCount == 0)
            {
                Assert.Fail($"Expected call to {nameof(EncapsulateCore)}.");
            }
            if (OnExportPrivateSeedCore is not null && ExportPrivateSeedCoreCount == 0)
            {
                Assert.Fail($"Expected call to {nameof(ExportPrivateSeedCore)}.");
            }
            if (OnExportDecapsulationKeyCore is not null && ExportDecapsulationKeyCoreCount == 0)
            {
                Assert.Fail($"Expected call to {nameof(ExportDecapsulationKeyCore)}.");
            }
            if (OnExportEncapsulationKeyCore is not null && ExportEncapsulationKeyCoreCount == 0)
            {
                Assert.Fail($"Expected call to {nameof(ExportEncapsulationKeyCore)}.");
            }
        }

        internal delegate void DecapsulateCoreCallback(ReadOnlySpan<byte> ciphertext, Span<byte> sharedSecret);
        internal delegate void EncapsulateCoreCallback(Span<byte> ciphertext, Span<byte> sharedSecret);
        internal delegate void ExportKeyCoreCallback(Span<byte> destination);

        private T GetCallback<T>(T callback, [CallerMemberNameAttribute]string caller = null) where T : Delegate
        {
            if (_disposed)
            {
                Assert.Fail($"Unexpected call to ${caller} after Dispose.");
            }

            return callback ?? throw new XunitException($"Unexpected call to {caller}.");
        }
    }
}
