// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    public static class MLDsaMuHashTests
    {
        [Fact]
        public static void KeyRequired()
        {
            AssertExtensions.Throws<ArgumentNullException>("key", () => new MLDsaMuHashTestImplementation(null));
        }

        [Fact]
        public static void KeyPropertyIsTransparent()
        {
            using (MLDsaTestImplementation key = MLDsaTestImplementation.CreateNoOp(MLDsaAlgorithm.MLDsa87))
            using (MLDsaMuHashTestImplementation muHash = new MLDsaMuHashTestImplementation(key))
            {
                Assert.Same(key, muHash.ExposedKey);
            }
        }

        [Fact]
        public static void DisposeOnce()
        {
            MLDsaMuHashTestImplementation muHash;

            using (MLDsaTestImplementation key = MLDsaTestImplementation.CreateNoOp(MLDsaAlgorithm.MLDsa87))
            using (muHash = new MLDsaMuHashTestImplementation(key))
            {
                muHash.DisposeHook = disposing => AssertExtensions.TrueExpression(disposing);

                Assert.Equal(0, muHash.DisposeCallCount);
                muHash.Dispose();
                Assert.Equal(1, muHash.DisposeCallCount);
                muHash.Dispose();
                Assert.Equal(1, muHash.DisposeCallCount);
            }

            Assert.Equal(1, muHash.DisposeCallCount);
        }

        [Fact]
        public static void UseAfterDispose()
        {
            using (MLDsaTestImplementation key = MLDsaTestImplementation.CreateNoOp(MLDsaAlgorithm.MLDsa87))
            using (MLDsaMuHash muHash = new MLDsaMuHashTestImplementation(key))
            using (Stream stream = new MemoryStream())
            {
                byte[] hash = new byte[muHash.HashLengthInBytes];
                byte[] signature = new byte[key.Algorithm.SignatureSizeInBytes];
                muHash.Dispose();

                AssertExtensions.Throws<ObjectDisposedException>(() => _ = muHash.HashLengthInBytes);
                AssertExtensions.Throws<ObjectDisposedException>(() => muHash.AppendData(Array.Empty<byte>()));
                AssertExtensions.Throws<ObjectDisposedException>(() => muHash.AppendData(ReadOnlySpan<byte>.Empty));
                AssertExtensions.Throws<ObjectDisposedException>(() => muHash.AppendData(stream));
                AssertExtensions.Throws<ObjectDisposedException>(() => muHash.AppendDataAsync(stream));
                AssertExtensions.Throws<ObjectDisposedException>(() => muHash.Clone());
                AssertExtensions.Throws<ObjectDisposedException>(() => muHash.GetCurrentHash());
                AssertExtensions.Throws<ObjectDisposedException>(() => muHash.GetCurrentHash(hash));
                AssertExtensions.Throws<ObjectDisposedException>(() => muHash.GetHashAndReset());
                AssertExtensions.Throws<ObjectDisposedException>(() => muHash.GetHashAndReset(hash));
                AssertExtensions.Throws<ObjectDisposedException>(() => muHash.Reset());
                AssertExtensions.Throws<ObjectDisposedException>(() => muHash.SignAndReset());
                AssertExtensions.Throws<ObjectDisposedException>(() => muHash.SignAndReset(signature));
                AssertExtensions.Throws<ObjectDisposedException>(() => muHash.VerifyAndReset(signature));
                AssertExtensions.Throws<ObjectDisposedException>(() => muHash.VerifyAndReset(new ReadOnlySpan<byte>(signature)));
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static void NullArgumentValidation(bool disposeFirst)
        {
            using (MLDsaTestImplementation key = MLDsaTestImplementation.CreateNoOp(MLDsaAlgorithm.MLDsa87))
            using (MLDsaMuHash muHash = new MLDsaMuHashTestImplementation(key))
            {
                if (disposeFirst)
                {
                    // Test that argument validation exceptions take precedence over ObjectDisposedException
                    muHash.Dispose();
                }

                AssertExtensions.Throws<ArgumentNullException>("data", () => muHash.AppendData((byte[])null));
                AssertExtensions.Throws<ArgumentNullException>("stream", () => muHash.AppendData((Stream)null));
                AssertExtensions.Throws<ArgumentNullException>("stream", () => muHash.AppendDataAsync(null));
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static void NonReadableStreamValidation(bool disposeFirst)
        {
            using (MLDsaTestImplementation key = MLDsaTestImplementation.CreateNoOp(MLDsaAlgorithm.MLDsa87))
            using (MLDsaMuHash muHash = new MLDsaMuHashTestImplementation(key))
            {
                if (disposeFirst)
                {
                    // Test that argument validation exceptions take precedence over ObjectDisposedException
                    muHash.Dispose();
                }

                AssertExtensions.Throws<ArgumentException>("stream", () => muHash.AppendData(UntouchableStream.Instance));
                AssertExtensions.Throws<ArgumentException>("stream", () => muHash.AppendDataAsync(UntouchableStream.Instance));
            }
        }

        [Fact]
        public static void KeyInaccessibleAfterDispose()
        {
            using (MLDsaTestImplementation key = MLDsaTestImplementation.CreateNoOp(MLDsaAlgorithm.MLDsa87))
            using (MLDsaMuHashTestImplementation muHash = new MLDsaMuHashTestImplementation(key))
            {
                Assert.NotNull(muHash.ExposedKey);
                muHash.Dispose();
                AssertExtensions.Throws<ObjectDisposedException>(() => _ = muHash.ExposedKey);
            }
        }

        [Theory]
        [MemberData(nameof(MLDsaTestsData.AllMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public static void HashSize_IsCorrect(MLDsaAlgorithm algorithm)
        {
            using (MLDsaTestImplementation key = MLDsaTestImplementation.CreateNoOp(algorithm))
            using (MLDsaMuHash muHash = new MLDsaMuHashTestImplementation(key))
            {
                Assert.Equal(64, muHash.HashLengthInBytes);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static void SignAndReset_ValidationAfterDispose(bool disposeFirst)
        {
            using (MLDsaTestImplementation key = MLDsaTestImplementation.CreateNoOp(MLDsaAlgorithm.MLDsa87))
            using (MLDsaMuHash muHash = new MLDsaMuHashTestImplementation(key))
            {
                if (disposeFirst)
                {
                    muHash.Dispose();
                    AssertExtensions.Throws<ObjectDisposedException>(() => muHash.SignAndReset(Array.Empty<byte>()));
                }
                else
                {
                    AssertExtensions.Throws<ArgumentException>("destination", () => muHash.SignAndReset(Array.Empty<byte>()));
                }
            }
        }

        [Fact]
        public static void CloneMustClone()
        {
            using (MLDsaTestImplementation key = MLDsaTestImplementation.CreateNoOp(MLDsaAlgorithm.MLDsa87))
            using (MLDsaMuHashTestImplementation muHash = new MLDsaMuHashTestImplementation(key))
            {
                muHash.CloneHook = () => muHash;
                AssertExtensions.Throws<CryptographicException>(() => muHash.Clone());
                Assert.Equal(1, muHash.CloneCoreCallCount);

                muHash.CloneHook = () => new MLDsaMuHashTestImplementation(key);
                Assert.NotSame(muHash, muHash.Clone());
                Assert.Equal(2, muHash.CloneCoreCallCount);
            }
        }

        [Fact]
        public static void ResetCallsGetHashAndReset()
        {
            using (MLDsaTestImplementation key = MLDsaTestImplementation.CreateNoOp(MLDsaAlgorithm.MLDsa87))
            using (MLDsaMuHashTestImplementation muHash = new MLDsaMuHashTestImplementation(key))
            {
                muHash.GetHashAndResetCoreHook = destination => destination.Clear();
                muHash.ResetCoreHook = null;
                muHash.Reset();

                Assert.Equal(1, muHash.ResetCoreCallCount);
                Assert.Equal(1, muHash.GetHashAndResetCoreCallCount);
            }
        }

        [Fact]
        public static void OverriddenResetDoesNotCallGetHashAndReset()
        {
            using (MLDsaTestImplementation key = MLDsaTestImplementation.CreateNoOp(MLDsaAlgorithm.MLDsa87))
            using (MLDsaMuHashTestImplementation muHash = new MLDsaMuHashTestImplementation(key))
            {
                muHash.ResetCoreHook = () => { };
                muHash.Reset();

                Assert.Equal(1, muHash.ResetCoreCallCount);
                Assert.Equal(0, muHash.GetHashAndResetCoreCallCount);
            }
        }

        [Fact]
        public static async Task AppendDataSkipsEmpty()
        {
            using (MLDsaTestImplementation key = MLDsaTestImplementation.CreateNoOp(MLDsaAlgorithm.MLDsa87))
            using (MLDsaMuHashTestImplementation muHash = new MLDsaMuHashTestImplementation(key))
            using (Stream stream = new MemoryStream())
            {
                muHash.AppendData(Array.Empty<byte>());
                muHash.AppendData(ReadOnlySpan<byte>.Empty);
                muHash.AppendData(stream);
                await muHash.AppendDataAsync(stream);

                Assert.Equal(0, muHash.AppendDataCoreCallCount);
            }
        }

        [Fact]
        public static void AppendDataStraightPassthrough()
        {
            using (MLDsaTestImplementation key = MLDsaTestImplementation.CreateNoOp(MLDsaAlgorithm.MLDsa87))
            using (MLDsaMuHashTestImplementation muHash = new MLDsaMuHashTestImplementation(key))
            {
                byte[] data = new byte[10];
                muHash.AppendDataCoreHook = span => AssertExtensions.Same(data, span);

                muHash.AppendData(data);
                Assert.Equal(1, muHash.AppendDataCoreCallCount);
                muHash.AppendData(new ReadOnlySpan<byte>(data));
                Assert.Equal(2, muHash.AppendDataCoreCallCount);
            }
        }

        [Fact]
        public static void AppendDataStream()
        {
            byte[] data = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
            List<byte> list = new();

            using (MLDsaTestImplementation key = MLDsaTestImplementation.CreateNoOp(MLDsaAlgorithm.MLDsa87))
            using (MLDsaMuHashTestImplementation muHash = new MLDsaMuHashTestImplementation(key))
            using (MemoryStream stream = new MemoryStream(data))
            {
                muHash.AppendDataCoreHook =
                    span =>
                    {
                        for (int i = 0; i < span.Length; i++)
                        {
                            list.Add(span[i]);
                        }
                    };

                muHash.AppendData(stream);
                Assert.Equal(data, list);
            }
        }

        [Fact]
        public static async Task AppendDataStreamAsync()
        {
            byte[] data = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
            List<byte> list = new();

            using (MLDsaTestImplementation key = MLDsaTestImplementation.CreateNoOp(MLDsaAlgorithm.MLDsa87))
            using (MLDsaMuHashTestImplementation muHash = new MLDsaMuHashTestImplementation(key))
            using (MemoryStream stream = new MemoryStream(data))
            {
                muHash.AppendDataCoreHook =
                    span =>
                    {
                        for (int i = 0; i < span.Length; i++)
                        {
                            list.Add(span[i]);
                        }
                    };

                await muHash.AppendDataAsync(stream);
                Assert.Equal(data, list);
            }
        }

        [Fact]
        public static void GetHashAndReset_Alloc()
        {
            using (MLDsaTestImplementation key = MLDsaTestImplementation.CreateNoOp(MLDsaAlgorithm.MLDsa87))
            using (MLDsaMuHashTestImplementation muHash = new MLDsaMuHashTestImplementation(key))
            {
                muHash.GetHashAndResetCoreHook = destination => destination.Fill(0x0B);
                byte[] ret = muHash.GetHashAndReset();
                Assert.Equal(muHash.HashLengthInBytes, ret.Length);
                AssertExtensions.FilledWith((byte)0x0B, ret);
            }
        }

        [Fact]
        public static void GetHashAndReset_Span()
        {
            using (MLDsaTestImplementation key = MLDsaTestImplementation.CreateNoOp(MLDsaAlgorithm.MLDsa87))
            using (MLDsaMuHashTestImplementation muHash = new MLDsaMuHashTestImplementation(key))
            {
                byte[] buffer = new byte[120];
                int target = muHash.HashLengthInBytes;

                AssertExtensions.Throws<ArgumentException>(
                    "destination",
                    () => muHash.GetHashAndReset(buffer.AsSpan(0, target - 1)));

                AssertExtensions.Throws<ArgumentException>(
                    "destination",
                    () => muHash.GetHashAndReset(buffer.AsSpan(0, target + 1)));

                int offset = 10;
                Span<byte> mu = buffer.AsSpan(offset, target);
                buffer.AsSpan().Fill(0xFF);

                muHash.GetHashAndResetCoreHook = destination => destination.Fill(0x0B);
                muHash.GetHashAndReset(mu);

                AssertExtensions.FilledWith((byte)0x0B, mu);
                AssertExtensions.FilledWith((byte)0xFF, buffer.AsSpan(0, offset));
                AssertExtensions.FilledWith((byte)0xFF, buffer.AsSpan(offset + target));
            }
        }

        [Fact]
        public static void GetCurrentHash_Alloc()
        {
            using (MLDsaTestImplementation key = MLDsaTestImplementation.CreateNoOp(MLDsaAlgorithm.MLDsa87))
            using (MLDsaMuHashTestImplementation muHash = new MLDsaMuHashTestImplementation(key))
            {
                muHash.GetCurrentHashCoreHook = destination => destination.Fill(0x0B);
                byte[] ret = muHash.GetCurrentHash();
                Assert.Equal(muHash.HashLengthInBytes, ret.Length);
                AssertExtensions.FilledWith((byte)0x0B, ret);
            }
        }

        [Fact]
        public static void GetCurrentHash_Span()
        {
            using (MLDsaTestImplementation key = MLDsaTestImplementation.CreateNoOp(MLDsaAlgorithm.MLDsa87))
            using (MLDsaMuHashTestImplementation muHash = new MLDsaMuHashTestImplementation(key))
            {
                byte[] buffer = new byte[120];
                int target = muHash.HashLengthInBytes;

                AssertExtensions.Throws<ArgumentException>(
                    "destination",
                    () => muHash.GetCurrentHash(buffer.AsSpan(0, target - 1)));

                AssertExtensions.Throws<ArgumentException>(
                    "destination",
                    () => muHash.GetCurrentHash(buffer.AsSpan(0, target + 1)));

                int offset = 10;
                Span<byte> mu = buffer.AsSpan(offset, target);
                buffer.AsSpan().Fill(0xFF);

                muHash.GetCurrentHashCoreHook = destination => destination.Fill(0x0B);
                muHash.GetCurrentHash(mu);

                AssertExtensions.FilledWith((byte)0x0B, mu);
                AssertExtensions.FilledWith((byte)0xFF, buffer.AsSpan(0, offset));
                AssertExtensions.FilledWith((byte)0xFF, buffer.AsSpan(offset + target));
            }
        }

        [Fact]
        public static void SignAndReset_Alloc()
        {
            using (MLDsaTestImplementation key = MLDsaTestImplementation.CreateOverriddenCoreMethodsFail(MLDsaAlgorithm.MLDsa87))
            using (MLDsaMuHashTestImplementation muHash = new MLDsaMuHashTestImplementation(key))
            {
                key.SignExternalMuHook = (mu, destination) => destination.Fill(0xB0);
                muHash.GetHashAndResetCoreHook = destination => destination.Fill(0x0B);
                byte[] ret = muHash.SignAndReset();

                Assert.Equal(key.Algorithm.SignatureSizeInBytes, ret.Length);
                AssertExtensions.FilledWith((byte)0xB0, ret);
            }
        }

        [Fact]
        public static void SignAndReset_Span()
        {
            using (MLDsaTestImplementation key = MLDsaTestImplementation.CreateOverriddenCoreMethodsFail(MLDsaAlgorithm.MLDsa87))
            using (MLDsaMuHashTestImplementation muHash = new MLDsaMuHashTestImplementation(key))
            {
                int offset = 10;
                int target = key.Algorithm.SignatureSizeInBytes;
                byte[] buffer = new byte[target + 3 * offset];
                buffer.AsSpan().Fill(0xDD);

                // GetHashAndResetCoreHook isn't set here, so we see that argument validation
                // prevents it from getting called.

                AssertExtensions.Throws<ArgumentException>(
                    "destination",
                    () => muHash.SignAndReset(buffer.AsSpan(offset, target - 1)));

                AssertExtensions.Throws<ArgumentException>(
                    "destination",
                    () => muHash.SignAndReset(buffer.AsSpan(offset, target + 1)));

                key.SignExternalMuHook = (mu, destination) => destination.Fill(0xB0);
                muHash.GetHashAndResetCoreHook = destination => destination.Fill(0x0B);

                Span<byte> sig = buffer.AsSpan(offset, target);
                muHash.SignAndReset(sig);

                AssertExtensions.FilledWith((byte)0xB0, sig);
                AssertExtensions.FilledWith((byte)0xDD, buffer.AsSpan(0, offset));
                AssertExtensions.FilledWith((byte)0xDD, buffer.AsSpan(offset + target));
            }
        }

        [Fact]
        public static void VerifyAndReset()
        {
            using (MLDsaTestImplementation key = MLDsaTestImplementation.CreateOverriddenCoreMethodsFail(MLDsaAlgorithm.MLDsa87))
            using (MLDsaMuHashTestImplementation muHash = new MLDsaMuHashTestImplementation(key))
            {
                // VerifyCore isn't actually called when the signature is too short, but that's not this test's job to validate.
                // So, set it up at the beginning.
                key.VerifyExternalMuHook = (mu, signature) => signature.IndexOf(mu) == signature.Length - mu.Length;
                muHash.GetHashAndResetCoreHook = destination => destination.Fill(0x0B);

                byte[] longSig = new byte[key.Algorithm.SignatureSizeInBytes + 1];
                byte[] shortSig = new byte[key.Algorithm.SignatureSizeInBytes - 1];
                byte[] correctSig = new byte[key.Algorithm.SignatureSizeInBytes];

                longSig.AsSpan(longSig.Length - 64).Fill(0x0B);
                shortSig.AsSpan(longSig.Length - 64).Fill(0x0B);
                correctSig.AsSpan(correctSig.Length - 64).Fill(0x0B);

                // MLDsaMuHash isn't verifying the signature length, so every call calls GetHashAndReset.

                AssertExtensions.FalseExpression(muHash.VerifyAndReset(longSig));
                Assert.Equal(1, muHash.GetHashAndResetCoreCallCount);
                AssertExtensions.FalseExpression(muHash.VerifyAndReset(new ReadOnlySpan<byte>(longSig)));
                Assert.Equal(2, muHash.GetHashAndResetCoreCallCount);

                AssertExtensions.FalseExpression(muHash.VerifyAndReset(shortSig));
                Assert.Equal(3, muHash.GetHashAndResetCoreCallCount);
                AssertExtensions.FalseExpression(muHash.VerifyAndReset(new ReadOnlySpan<byte>(shortSig)));
                Assert.Equal(4, muHash.GetHashAndResetCoreCallCount);

                AssertExtensions.TrueExpression(muHash.VerifyAndReset(correctSig));
                Assert.Equal(5, muHash.GetHashAndResetCoreCallCount);
                AssertExtensions.TrueExpression(muHash.VerifyAndReset(new ReadOnlySpan<byte>(correctSig)));
                Assert.Equal(6, muHash.GetHashAndResetCoreCallCount);
            }
        }

        private sealed class MLDsaMuHashTestImplementation : MLDsaMuHash
        {
            internal delegate void AppendDataDelegate(ReadOnlySpan<byte> data);
            internal delegate void GetHashDelegate(Span<byte> destination);

            internal int AppendDataCoreCallCount { get; private set; }
            internal int CloneCoreCallCount { get; private set; }
            internal int DisposeCallCount { get; private set; }
            internal int GetCurrentHashCoreCallCount { get; private set; }
            internal int GetHashAndResetCoreCallCount { get; private set; }
            internal int ResetCoreCallCount { get; private set; }

            internal AppendDataDelegate AppendDataCoreHook { get; set; } = _ => Assert.Fail("AppendDataCore called unexpectedly");
            internal Func<MLDsaMuHash> CloneHook { get; set; } = () => { Assert.Fail("CloneCore called unexpectedly"); return null!; };
            internal Action<bool> DisposeHook { get; set; } = null;
            internal GetHashDelegate GetCurrentHashCoreHook { get; set; } = _ => Assert.Fail("GetCurrentHashCore called unexpectedly");
            internal GetHashDelegate GetHashAndResetCoreHook { get; set; } = _ => Assert.Fail("GetHashAndResetCore called unexpectedly");
            internal Action ResetCoreHook { get; set; } = () => Assert.Fail("ResetCore called unexpectedly");

            internal MLDsaMuHashTestImplementation(MLDsa key)
                : base(key)
            {
            }

            internal MLDsa ExposedKey => Key;

            protected override void AppendDataCore(ReadOnlySpan<byte> data)
            {
                AppendDataCoreCallCount++;
                AppendDataCoreHook(data);
            }

            protected override MLDsaMuHash CloneCore()
            {
                CloneCoreCallCount++;
                return CloneHook();
            }

            protected override void Dispose(bool disposing)
            {
                DisposeCallCount++;

                if (DisposeHook is null)
                {
                    base.Dispose(disposing);
                }
                else
                {
                    DisposeHook(disposing);
                }
            }

            protected override void GetCurrentHashCore(Span<byte> destination)
            {
                GetCurrentHashCoreCallCount++;
                GetCurrentHashCoreHook(destination);
            }

            protected override void GetHashAndResetCore(Span<byte> destination)
            {
                GetHashAndResetCoreCallCount++;
                GetHashAndResetCoreHook(destination);
            }

            protected override void ResetCore()
            {
                ResetCoreCallCount++;

                if (ResetCoreHook is null)
                {
                    base.ResetCore();
                }
                else
                {
                    ResetCoreHook();
                }
            }
        }
    }
}
