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

        private static bool PlatformSupportsMLDsa() => PlatformDetection.IsOpenSsl3_5;

        [Fact]
        public static void DisposeIsCalledOnImplementation()
        {
            MLDsaTestImplementation mldsa = MLDsaTestImplementation.CreateNoOp(MLDsaAlgorithm.MLDsa44);
            int numberOfTimesDisposeCalled = 0;
            mldsa.DisposeHook = (disposing) =>
            {
                numberOfTimesDisposeCalled++;
            };

            Assert.Equal(0, numberOfTimesDisposeCalled);
            mldsa.Dispose();
            Assert.Equal(1, numberOfTimesDisposeCalled);
            mldsa.Dispose();
            Assert.Equal(1, numberOfTimesDisposeCalled);
        }
    }
}
