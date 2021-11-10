// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Threading;
using System.Threading.Tasks;
using Test.IO.Streams;
using Xunit;

namespace System.IO.Hashing.Tests
{
    public static class NonCryptoHashBaseTests
    {
        [Fact]
        public static void ZeroLengthHashIsInvalid()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>(
                "hashLengthInBytes",
                () => new FlexibleAlgorithm(0));
        }

        [Fact]
        public static void NegativeHashLengthIsInvalid()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>(
                "hashLengthInBytes",
                () => new FlexibleAlgorithm(-1));
        }

        [Fact]
        public static void TryGetCurrentHash_TooSmall()
        {
            Span<byte> buf = stackalloc byte[7];
            FlexibleAlgorithm hash = new FlexibleAlgorithm(buf.Length + 1);

            Assert.True(hash.IsReset);
            hash.Append(buf);
            Assert.False(hash.IsReset);

            while (true)
            {
                Assert.False(hash.TryGetCurrentHash(buf, out int written));
                Assert.Equal(0, written);

                if (buf.IsEmpty)
                {
                    break;
                }

                buf = buf.Slice(1);
            }

            Assert.Equal(0, hash.GetCurrentHashCoreCallCount);
            Assert.False(hash.IsReset);
        }

        [Fact]
        public static void TryGetCurrentHash_TooBig_Succeeds()
        {
            Span<byte> buf = stackalloc byte[16];
            FlexibleAlgorithm hash = new FlexibleAlgorithm(buf.Length / 2);
            int i = 0;

            hash.Append(buf);

            while (buf.Length > hash.HashLengthInBytes)
            {
                Assert.Equal(i, hash.GetCurrentHashCoreCallCount);

                // TryGetCurrentHash in turn asserts that buf got Sliced down to HashLengthInBytes.
                Assert.True(hash.TryGetCurrentHash(buf, out int written));
                Assert.Equal(hash.HashLengthInBytes, written);

                buf = buf.Slice(1);
                i++;
            }

            Assert.Equal(i, hash.GetCurrentHashCoreCallCount);
            Assert.False(hash.IsReset);
        }

        [Fact]
        public static void GetCurrentHash_TooSmall()
        {
            byte[] buf = new byte[7];
            FlexibleAlgorithm hash = new FlexibleAlgorithm(buf.Length + 1);

            Assert.True(hash.IsReset);
            hash.Append(buf);
            Assert.False(hash.IsReset);

            for (int i = 0; i <= buf.Length; i++)
            {
                AssertExtensions.Throws<ArgumentException>(
                    "destination",
                    () => hash.GetCurrentHash(buf.AsSpan(i)));
            }

            Assert.False(hash.IsReset);
        }

        [Fact]
        public static void GetCurrentHash_TooBig_Succeeds()
        {
            Span<byte> buf = stackalloc byte[16];
            FlexibleAlgorithm hash = new FlexibleAlgorithm(buf.Length / 2);
            int i = 0;

            hash.Append(buf);

            while (buf.Length > hash.HashLengthInBytes)
            {
                Assert.Equal(i, hash.GetCurrentHashCoreCallCount);

                // GetCurrentHash in turn asserts that buf got Sliced down to HashLengthInBytes.
                int written = hash.GetCurrentHash(buf);
                Assert.Equal(hash.HashLengthInBytes, written);

                buf = buf.Slice(1);
                i++;
            }

            Assert.Equal(i, hash.GetCurrentHashCoreCallCount);
            Assert.False(hash.IsReset);
        }

        [Fact]
        public static void AllocatingGetCurrentHash_CorrectSize()
        {
            FlexibleAlgorithm hash = new FlexibleAlgorithm(12);
            byte[] ret = hash.GetCurrentHash();
            Assert.Equal(hash.HashLengthInBytes, ret.Length);
            Assert.Equal(1, hash.GetCurrentHashCoreCallCount);
        }

        [Fact]
        public static void DefaultGetHashAndResetDoesWhatItSays()
        {
            Span<byte> buf = stackalloc byte[16];
            FlexibleAlgorithm hash = new FlexibleAlgorithm(buf.Length);
            Assert.True(hash.IsReset);
            hash.Append(buf);
            Assert.False(hash.IsReset);
            Assert.Equal(0, hash.GetCurrentHashCoreCallCount);

            byte[] ret = hash.GetHashAndReset();
            Assert.True(hash.IsReset);
            Assert.Equal(1, hash.GetCurrentHashCoreCallCount);
            Assert.Equal(hash.HashLengthInBytes, ret.Length);

            hash.Append(ret);
            Assert.False(hash.IsReset);

            Assert.True(hash.TryGetHashAndReset(buf, out int written));
            Assert.True(hash.IsReset);
            Assert.Equal(2, hash.GetCurrentHashCoreCallCount);
            Assert.Equal(hash.HashLengthInBytes, written);

            hash.Append(ret);
            Assert.False(hash.IsReset);

            written = hash.GetHashAndReset(buf);
            Assert.True(hash.IsReset);
            Assert.Equal(3, hash.GetCurrentHashCoreCallCount);
            Assert.Equal(hash.HashLengthInBytes, written);
        }

        [Fact]
        public static void TryGetHashAndReset_TooSmall()
        {
            Span<byte> buf = stackalloc byte[7];
            FlexibleAlgorithm hash = new FlexibleAlgorithm(buf.Length + 1);

            Assert.True(hash.IsReset);
            hash.Append(buf);
            Assert.False(hash.IsReset);

            while (true)
            {
                Assert.False(hash.TryGetHashAndReset(buf, out int written));
                Assert.Equal(0, written);

                if (buf.IsEmpty)
                {
                    break;
                }

                buf = buf.Slice(1);
            }

            Assert.Equal(0, hash.GetCurrentHashCoreCallCount);
            Assert.False(hash.IsReset);
        }

        [Fact]
        public static void TryGetHashAndReset_TooBig_Succeeds()
        {
            Span<byte> buf = stackalloc byte[16];
            FlexibleAlgorithm hash = new FlexibleAlgorithm(buf.Length / 2);
            int i = 0;

            while (buf.Length > hash.HashLengthInBytes)
            {
                Assert.Equal(i, hash.GetCurrentHashCoreCallCount);

                hash.Append(buf);
                Assert.False(hash.IsReset);

                // TryGetCurrentHash in turn asserts that buf got Sliced down to HashLengthInBytes.
                Assert.True(hash.TryGetHashAndReset(buf, out int written));
                Assert.Equal(hash.HashLengthInBytes, written);
                Assert.True(hash.IsReset);

                buf = buf.Slice(1);
                i++;
            }

            Assert.Equal(i, hash.GetCurrentHashCoreCallCount);
            Assert.True(hash.IsReset);
        }

        [Fact]
        public static void GetHashAndReset_TooSmall()
        {
            byte[] buf = new byte[7];
            FlexibleAlgorithm hash = new FlexibleAlgorithm(buf.Length + 1);

            Assert.True(hash.IsReset);
            hash.Append(buf);
            Assert.False(hash.IsReset);

            for (int i = 0; i <= buf.Length; i++)
            {
                AssertExtensions.Throws<ArgumentException>(
                    "destination",
                    () => hash.GetHashAndReset(buf.AsSpan(i)));
            }

            Assert.Equal(0, hash.GetCurrentHashCoreCallCount);
            Assert.False(hash.IsReset);
        }

        [Fact]
        public static void GetHashAndReset_TooBig_Succeeds()
        {
            Span<byte> buf = stackalloc byte[16];
            FlexibleAlgorithm hash = new FlexibleAlgorithm(buf.Length / 2);
            int i = 0;

            while (buf.Length > hash.HashLengthInBytes)
            {
                Assert.Equal(i, hash.GetCurrentHashCoreCallCount);

                hash.Append(buf);
                Assert.False(hash.IsReset);

                Assert.True(hash.TryGetHashAndReset(buf, out int written));
                Assert.Equal(hash.HashLengthInBytes, written);
                Assert.True(hash.IsReset);

                buf = buf.Slice(1);
                i++;
            }

            Assert.Equal(i, hash.GetCurrentHashCoreCallCount);
            Assert.True(hash.IsReset);
        }

        [Fact]
        public static void AllocatingGetHashAndReset_CorrectSize()
        {
            FlexibleAlgorithm hash = new FlexibleAlgorithm(12);
            byte[] ret = hash.GetHashAndReset();
            Assert.Equal(hash.HashLengthInBytes, ret.Length);
            Assert.Equal(1, hash.GetCurrentHashCoreCallCount);
        }

        [Fact]
        public static void OverriddenTryGetHashAndReset_TooSmall()
        {
            Span<byte> buf = stackalloc byte[7];
            var hash = new FlexibleAlgorithmOverride(buf.Length + 1);

            Assert.True(hash.IsReset);
            hash.Append(buf);
            Assert.False(hash.IsReset);

            while (true)
            {
                Assert.False(hash.TryGetHashAndReset(buf, out int written));
                Assert.Equal(0, written);

                if (buf.IsEmpty)
                {
                    break;
                }

                buf = buf.Slice(1);
            }

            Assert.Equal(0, hash.GetCurrentHashCoreCallCount);
            Assert.Equal(0, hash.GetHashAndResetCoreCallCount);
            Assert.False(hash.IsReset);
        }

        [Fact]
        public static void OverriddenTryGetHashAndReset_TooBig_Succeeds()
        {
            Span<byte> buf = stackalloc byte[16];
            var hash = new FlexibleAlgorithmOverride(buf.Length / 2);
            int i = 0;

            while (buf.Length > hash.HashLengthInBytes)
            {
                Assert.Equal(0, hash.GetCurrentHashCoreCallCount);
                Assert.Equal(i, hash.GetHashAndResetCoreCallCount);

                hash.Append(buf);
                Assert.False(hash.IsReset);

                // TryGetCurrentHash in turn asserts that buf got Sliced down to HashLengthInBytes.
                Assert.True(hash.TryGetHashAndReset(buf, out int written));
                Assert.Equal(hash.HashLengthInBytes, written);
                Assert.True(hash.IsReset);

                buf = buf.Slice(1);
                i++;
            }

            Assert.Equal(0, hash.GetCurrentHashCoreCallCount);
            Assert.Equal(i, hash.GetHashAndResetCoreCallCount);
            Assert.True(hash.IsReset);
        }

        [Fact]
        public static void OverriddenGetHashAndReset_TooSmall()
        {
            byte[] buf = new byte[7];
            var hash = new FlexibleAlgorithmOverride(buf.Length + 1);

            Assert.True(hash.IsReset);
            hash.Append(buf);
            Assert.False(hash.IsReset);

            for (int i = 0; i <= buf.Length; i++)
            {
                AssertExtensions.Throws<ArgumentException>(
                    "destination",
                    () => hash.GetHashAndReset(buf.AsSpan(i)));
            }

            Assert.Equal(0, hash.GetCurrentHashCoreCallCount);
            Assert.Equal(0, hash.GetHashAndResetCoreCallCount);
            Assert.False(hash.IsReset);
        }

        [Fact]
        public static void OverriddenGetHashAndReset_TooBig_Succeeds()
        {
            Span<byte> buf = stackalloc byte[16];
            var hash = new FlexibleAlgorithmOverride(buf.Length / 2);
            int i = 0;

            while (buf.Length > hash.HashLengthInBytes)
            {
                Assert.Equal(0, hash.GetCurrentHashCoreCallCount);
                Assert.Equal(i, hash.GetHashAndResetCoreCallCount);

                hash.Append(buf);
                Assert.False(hash.IsReset);

                Assert.True(hash.TryGetHashAndReset(buf, out int written));
                Assert.Equal(hash.HashLengthInBytes, written);
                Assert.True(hash.IsReset);

                buf = buf.Slice(1);
                i++;
            }

            Assert.Equal(0, hash.GetCurrentHashCoreCallCount);
            Assert.Equal(i, hash.GetHashAndResetCoreCallCount);
            Assert.True(hash.IsReset);
        }

        [Fact]
        public static void OverriddenAllocatingGetHashAndReset_CorrectSize()
        {
            var hash = new FlexibleAlgorithmOverride(12);
            byte[] ret = hash.GetHashAndReset();
            Assert.Equal(hash.HashLengthInBytes, ret.Length);
            Assert.Equal(0, hash.GetCurrentHashCoreCallCount);
            Assert.Equal(1, hash.GetHashAndResetCoreCallCount);
        }

        [Fact]
        public static void AppendNullArrayThrows()
        {
            NonCryptographicHashAlgorithm hash = new FlexibleAlgorithm(5);
            AssertExtensions.Throws<ArgumentNullException>("source", () => hash.Append((byte[])null));
        }

        [Fact]
        public static void AppendNullStreamThrows()
        {
            NonCryptographicHashAlgorithm hash = new FlexibleAlgorithm(5);
            AssertExtensions.Throws<ArgumentNullException>("stream", () => hash.Append((Stream)null));
        }

        [Fact]
        public static void AppendAsyncNullStreamThrows_OutsideTask()
        {
            NonCryptographicHashAlgorithm hash = new FlexibleAlgorithm(5);
            AssertExtensions.Throws<ArgumentNullException>("stream", () => hash.AppendAsync(null));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(511)]
        [InlineData(4097)]
        [InlineData(1048581)]
        public static void AppendStreamSanityTest(int streamSize)
        {
            NonCryptographicHashAlgorithm hash = new CountingAlgorithm();

            using (PositionValueStream stream = new PositionValueStream(streamSize))
            {
                hash.Append(stream);
            }

            Span<byte> val = stackalloc byte[sizeof(int)];
            hash.GetHashAndReset(val);
            int res = BinaryPrimitives.ReadInt32LittleEndian(val);
            Assert.Equal(streamSize, res);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(511)]
        [InlineData(4097)]
        [InlineData(1048581)]
        public static async Task AppendStreamAsyncSanityTest(int streamSize)
        {
            NonCryptographicHashAlgorithm hash = new CountingAlgorithm();

            using (PositionValueStream stream = new PositionValueStream(streamSize))
            {
                await hash.AppendAsync(stream);
            }

            byte[] val = new byte[sizeof(int)];
            hash.GetHashAndReset(val);
            int res = BinaryPrimitives.ReadInt32LittleEndian(val);
            Assert.Equal(streamSize, res);
        }

        [Fact]
        public static void AppendStreamAsyncSupportsCancellation()
        {
            NonCryptographicHashAlgorithm hash = new CountingAlgorithm();

            using (PositionValueStream stream = new PositionValueStream(21))
            using (CancellationTokenSource cts = new CancellationTokenSource())
            {
                cts.Cancel();

                Task task = hash.AppendAsync(stream, cts.Token);
                Assert.True(task.IsCompleted);
                Assert.True(task.IsCanceled);
            }

            byte[] val = new byte[sizeof(int)];
            hash.GetHashAndReset(val);
            int res = BinaryPrimitives.ReadInt32LittleEndian(val);
            Assert.Equal(0, res);
        }

        [Fact]
        public static void GetHashCode_NotSupported()
        {
            NonCryptographicHashAlgorithm hash = new CountingAlgorithm();
            Assert.Throws<NotSupportedException>(() => hash.GetHashCode());
        }
        private sealed class CountingAlgorithm : NonCryptographicHashAlgorithm
        {
            private int _count;

            public CountingAlgorithm()
                : base(sizeof(int))
            {
            }

            public override void Append(ReadOnlySpan<byte> source)
            {
                _count += source.Length;
            }

            public override void Reset()
            {
                _count = 0;
            }

            protected override void GetCurrentHashCore(Span<byte> destination)
            {
                BinaryPrimitives.WriteInt32LittleEndian(destination, _count);
            }
        }

        private sealed class FlexibleAlgorithm : NonCryptographicHashAlgorithm
        {
            public bool IsReset { get; private set; }
            public int GetCurrentHashCoreCallCount { get; private set; }

            public FlexibleAlgorithm(int hashLengthInBytes)
                : base(hashLengthInBytes)
            {
                Reset();
            }

            public override void Append(ReadOnlySpan<byte> source)
            {
                if (source.Length > 0)
                {
                    IsReset = false;
                }
            }

            public override void Reset()
            {
                IsReset = true;
            }

            protected override void GetCurrentHashCore(Span<byte> destination)
            {
                Assert.Equal(HashLengthInBytes, destination.Length);
                destination.Fill((byte)GetCurrentHashCoreCallCount);
                GetCurrentHashCoreCallCount++;
            }
        }

        private sealed class FlexibleAlgorithmOverride : NonCryptographicHashAlgorithm
        {
            public bool IsReset { get; private set; }
            public int GetCurrentHashCoreCallCount { get; private set; }
            public int GetHashAndResetCoreCallCount { get; private set; }

            public FlexibleAlgorithmOverride(int hashLengthInBytes)
                : base(hashLengthInBytes)
            {
                Reset();
            }

            public override void Append(ReadOnlySpan<byte> source)
            {
                if (source.Length > 0)
                {
                    IsReset = false;
                }
            }

            public override void Reset()
            {
                IsReset = true;
            }

            protected override void GetCurrentHashCore(Span<byte> destination)
            {
                Assert.Equal(HashLengthInBytes, destination.Length);
                destination.Fill(0xFE);
                GetCurrentHashCoreCallCount++;
            }

            protected override void GetHashAndResetCore(Span<byte> destination)
            {
                Assert.Equal(HashLengthInBytes, destination.Length);
                destination.Fill(0xFE);
                Reset();
                GetHashAndResetCoreCallCount++;
            }
        }
    }
}
