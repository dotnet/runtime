// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.RemoteExecutor;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    [ConditionalClass(typeof(MLKem), nameof(MLKem.IsSupported))]
    public class MLKemImplementationTests : MLKemBaseTests
    {
        public override MLKem GenerateKey(MLKemAlgorithm algorithm)
        {
            return MLKem.GenerateKey(algorithm);
        }

        public override MLKem ImportPrivateSeed(MLKemAlgorithm algorithm, ReadOnlySpan<byte> seed)
        {
            return MLKem.ImportPrivateSeed(algorithm, seed);
        }

        public override MLKem ImportDecapsulationKey(MLKemAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            return MLKem.ImportDecapsulationKey(algorithm, source);
        }

        public override MLKem ImportEncapsulationKey(MLKemAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            return MLKem.ImportEncapsulationKey(algorithm, source);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void IsSupported_InitializesCrypto()
        {
            if (!MLKem.IsSupported)
            {
                throw new SkipTestException("Algorithm is not supported on current platform.");
            }

            // This ensures that ML-KEM is the first cryptographic algorithm touched in the process, which kicks off
            // the initialization of the crypto layer on some platforms. Running in a remote executor ensures no other
            // test has pre-initialized anything.
            RemoteExecutor.Invoke(static () =>
            {
                return MLKem.IsSupported ? RemoteExecutor.SuccessExitCode : 0;
            }).Dispose();
        }

        [Fact]
        public static void IsSupported_AgreesWithPlatform()
        {
            Assert.Equal(PlatformSupportsMLKem(), MLKem.IsSupported);
        }

        private static bool PlatformSupportsMLKem() =>
            PlatformDetection.IsOpenSsl3_5 ||
            PlatformDetection.IsWindows10Version27858OrGreater;
    }
}
