// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.RemoteExecutor;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    public static partial class MLKemTests
    {
        public static bool IsNotSupported => !MLKem.IsSupported;

        [Fact]
        public static void IsSupported_AgreesWithPlatform()
        {
            Assert.Equal(PlatformSupportsMLKem(), MLKem.IsSupported);
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

        private static bool PlatformSupportsMLKem()
        {
            if (PlatformDetection.IsOpenSslSupported && PlatformDetection.OpenSslVersion >= new Version(3, 5))
            {
                return true;
            }

            return false;
        }
    }
}
