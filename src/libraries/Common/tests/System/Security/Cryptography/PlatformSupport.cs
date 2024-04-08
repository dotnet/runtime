// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Xunit;

namespace Test.Cryptography
{
    internal static class PlatformSupport
    {
        private static readonly Dictionary<CngAlgorithm, bool> s_platformCryptoSupportedAlgorithms = new();

        private static bool PlatformCryptoProviderFunctional(CngAlgorithm algorithm)
        {
            // Use a full lock around a non-concurrent dictionary. We do not want the value factory for
            // ConcurrentDictionary to be executing simultaneously for the same algorithm.
            lock (s_platformCryptoSupportedAlgorithms)
            {
                if (s_platformCryptoSupportedAlgorithms.TryGetValue(algorithm, out bool supported))
                {
                    return supported;
                }

                supported = DetermineAlgorithmFunctional(algorithm);
                s_platformCryptoSupportedAlgorithms[algorithm] = supported;
                return supported;
            }

            static bool DetermineAlgorithmFunctional(CngAlgorithm algorithm)
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
                            algorithm,
                            $"{nameof(PlatformCryptoProviderFunctional)}{algorithm.Algorithm}Key",
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
            }
        }

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

        internal static bool PlatformCryptoProviderFunctionalP256 => PlatformCryptoProviderFunctional(CngAlgorithm.ECDsaP256);
        internal static bool PlatformCryptoProviderFunctionalP384 => PlatformCryptoProviderFunctional(CngAlgorithm.ECDsaP384);
        internal static bool PlatformCryptoProviderFunctionalRsa => PlatformCryptoProviderFunctional(CngAlgorithm.Rsa);
    }
}
