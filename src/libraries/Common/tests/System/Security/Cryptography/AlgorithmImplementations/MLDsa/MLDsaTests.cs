// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.DotNet.XUnitExtensions;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    public static class MLDsaTests
    {
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void IsSupported_InitializesCrypto()
        {
            string arg = MLDsa.IsSupported ? "1" : "0";

            // This ensures that ML-DSA is the first cryptographic algorithm touched in the process, which kicks off
            // the initialization of the crypto layer on some platforms. Running in a remote executor ensures no other
            // test has pre-initialized anything.
            RemoteExecutor.Invoke(static (string isSupportedStr) =>
            {
                bool isSupported = isSupportedStr == "1";
                return MLDsa.IsSupported == isSupported ? RemoteExecutor.SuccessExitCode : 0;
            }, arg).Dispose();
        }

        [Fact]
        public static void IsSupported_AgreesWithPlatform()
        {
            Assert.Equal(PlatformSupportsMLDsa(), MLDsa.IsSupported);
        }

        private static bool PlatformSupportsMLDsa()
            => PlatformDetection.IsOpenSslSupported && PlatformDetection.OpenSslVersion >= new Version(3, 5);

        [Fact]
        public static void DisposeIsCalledOnImplementation()
        {
            DisposeCallsCountingMLDsa mldsa = new DisposeCallsCountingMLDsa(MLDsaAlgorithm.MLDsa44);

            Assert.Equal(0, mldsa.NumberOfTimesDisposeCalled);
            mldsa.Dispose();
            Assert.Equal(1, mldsa.NumberOfTimesDisposeCalled);
            mldsa.Dispose();
            Assert.Equal(1, mldsa.NumberOfTimesDisposeCalled);
        }

        private class DisposeCallsCountingMLDsa : MLDsa
        {
            public DisposeCallsCountingMLDsa(MLDsaAlgorithm algorithm) : base(algorithm)
            {
            }

            internal int NumberOfTimesDisposeCalled { get; private set; } = 0;
            protected override void ExportMLDsaPrivateSeedCore(Span<byte> destination) => throw new NotImplementedException();
            protected override void ExportMLDsaPublicKeyCore(Span<byte> destination) => throw new NotImplementedException();
            protected override void ExportMLDsaSecretKeyCore(Span<byte> destination) => throw new NotImplementedException();
            protected override void SignDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, Span<byte> destination) => throw new NotImplementedException();
            protected override bool VerifyDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, ReadOnlySpan<byte> signature) => throw new NotImplementedException();
            protected override void Dispose(bool disposing)
            {
                NumberOfTimesDisposeCalled++;
                base.Dispose(disposing);
            }
        }
    }
}
