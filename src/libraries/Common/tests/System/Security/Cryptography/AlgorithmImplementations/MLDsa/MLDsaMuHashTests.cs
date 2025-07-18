// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
                Assert.Equal(0, muHash.DisposeCallCount);
                muHash.Dispose();
                Assert.Equal(1, muHash.DisposeCallCount);
                muHash.Dispose();
                Assert.Equal(1, muHash.DisposeCallCount);
            }

            Assert.Equal(1, muHash.DisposeCallCount);
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
            internal Action<bool> DisposeHook { get; set; } = _ => { };
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
                DisposeHook(disposing);
            }

            protected override void GetCurrentHashCore(Span<byte> destination)
            {
                GetCurrentHashCoreCallCount++;
                GetCurrentHashCoreHook(destination);
            }

            protected override void GetHashAndResetCore(Span<byte> destination)
            {
                GetCurrentHashCoreCallCount++;
                GetCurrentHashCoreHook(destination);
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
