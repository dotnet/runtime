// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Security.Cryptography.SLHDsa.Tests
{
    public class SlhDsaPlatformTests
    {
        [Fact]
        public static void IsSupported_AgreesWithPlatform()
        {
            Assert.Equal(PlatformSupportsSlhDsa(), SlhDsa.IsSupported);
        }

        private static bool PlatformSupportsSlhDsa() => PlatformDetection.IsOpenSsl3_5;

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void IsSupported_InitializesCrypto()
        {
            string arg = SlhDsa.IsSupported ? "1" : "0";

            // This ensures that SLH-DSA is the first cryptographic algorithm touched in the process, which kicks off
            // the initialization of the crypto layer on some platforms. Running in a remote executor ensures no other
            // test has pre-initialized anything.
            RemoteExecutor.Invoke(static (string isSupportedStr) =>
            {
                bool isSupported = isSupportedStr == "1";
                return SlhDsa.IsSupported == isSupported ? RemoteExecutor.SuccessExitCode : 0;
            }, arg).Dispose();
        }
    }
}
