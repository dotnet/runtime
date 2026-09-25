// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.Tests
{
    [ConditionalClass(typeof(X25519DiffieHellman), nameof(X25519DiffieHellman.IsSupported))]
    public sealed class X25519DiffieHellmanImplementationTests : X25519DiffieHellmanBaseTests
    {
        public override X25519DiffieHellman GenerateKey() => X25519DiffieHellman.GenerateKey();

        public override X25519DiffieHellman ImportPrivateKey(ReadOnlySpan<byte> source) =>
            X25519DiffieHellman.ImportPrivateKey(source);

        public override X25519DiffieHellman ImportPublicKey(ReadOnlySpan<byte> source) =>
            X25519DiffieHellman.ImportPublicKey(source);
    }

    public static class X25519DiffieHellmanImplementationSupportedTests
    {
        [Fact]
        public static void IsSupported_AgreesWithPlatform()
        {
#if NET
            if (OperatingSystem.IsAndroid() && !OperatingSystem.IsAndroidVersionAtLeast(33))
            {
                // Android documents that XDH is available in API Level 33 or higher. However, Android's primary
                // cryptographic provider, Conscrypt, can be updated out-of-band from Android itself. API level 33 is
                // when it was first documented but Android may make it available in previous versions.
                // So if the Android version is API level 32 or lower, any support level is acceptable. For 33+ it is
                // required.
                return;
            }
#endif

            bool expectedSupported =
                PlatformDetection.IsWindows10OrLater ||
                PlatformDetection.IsApplePlatform ||
#if NET
                OperatingSystem.IsAndroidVersionAtLeast(33) ||
#endif
                PlatformDetection.IsOpenSslSupported; // X25519 is in OpenSSL 1.1.0 and .NET's floor is 1.1.1.

            Assert.Equal(expectedSupported, X25519DiffieHellman.IsSupported);
        }
    }
}
