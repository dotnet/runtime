// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Xunit;

namespace Test.Cryptography
{
    internal static class PlatformSupport
    {
        private static Lazy<bool> s_lazyPlatformCryptoProviderFunctional = new Lazy<bool>(static () =>
        {
#if !NETFRAMEWORK
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return false;
            }
#endif

            CngKey key = null;

            try
            {
                key = CngKey.Create(
                    CngAlgorithm.ECDsaP256,
                    $"{nameof(PlatformCryptoProviderFunctional)}Key",
                    new CngKeyCreationParameters
                    {
                        Provider = new CngProvider("Microsoft Platform Crypto Provider"),
                        KeyCreationOptions = CngKeyCreationOptions.OverwriteExistingKey,
                    });

                return true;
            }
            catch (CryptographicException)
            {
                return false;
            }
            finally
            {
                key?.Delete();
            }
        });

        // Platforms that use Apple Cryptography
        internal const TestPlatforms AppleCrypto = TestPlatforms.OSX | TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst;
        internal const TestPlatforms MobileAppleCrypto = TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst;

        // Platforms that support OpenSSL - all Unix except OSX/iOS/tvOS/MacCatalyst, Android, and Browser
        internal const TestPlatforms OpenSSL = TestPlatforms.AnyUnix & ~(AppleCrypto | TestPlatforms.Android | TestPlatforms.Browser);

        // Whether or not the current platform supports RC2
        internal static readonly bool IsRC2Supported = !PlatformDetection.IsAndroid;

#if NETCOREAPP
        internal static readonly bool IsAndroidVersionAtLeast31 = OperatingSystem.IsAndroidVersionAtLeast(31);
#else
        internal static readonly bool IsAndroidVersionAtLeast31 = false;
#endif

        internal static bool PlatformCryptoProviderFunctional => s_lazyPlatformCryptoProviderFunctional.Value;

    }
}
