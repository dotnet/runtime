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
        private const string PlatformCryptoProvider = "Microsoft Platform Crypto Provider";
        private const string SoftwareKeyStorageProvider = "Microsoft Software Key Storage Provider";

        private static readonly Dictionary<string, Dictionary<CngAlgorithm, bool>> s_providerSupportedAlgorithms = new();

        private static bool CngProviderFunctional(string provider, CngAlgorithm algorithm)
        {
            // Use a full lock around a non-concurrent dictionary. We do not want the value factory for
            // ConcurrentDictionary to be executing simultaneously for the same algorithm.
            lock (s_providerSupportedAlgorithms)
            {
                Dictionary<CngAlgorithm, bool> supportedAlgorithms;

                if (!s_providerSupportedAlgorithms.TryGetValue(provider, out supportedAlgorithms))
                {
                    s_providerSupportedAlgorithms[provider] = supportedAlgorithms = new();
                }

                if (supportedAlgorithms.TryGetValue(algorithm, out bool supported))
                {
                    return supported;
                }

                supported = DetermineAlgorithmFunctional(provider, algorithm);
                supportedAlgorithms[algorithm] = supported;
                return supported;
            }

            static bool DetermineAlgorithmFunctional(string provider, CngAlgorithm algorithm)
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
                            $"{nameof(CngProviderFunctional)}{provider.Replace(" ", "")}{algorithm.Algorithm}Key",
                        new CngKeyCreationParameters
                        {
                            Provider = new CngProvider(provider),
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

#if NET
        internal static readonly bool IsAndroidVersionAtLeast31 = OperatingSystem.IsAndroidVersionAtLeast(31);
#else
        internal static readonly bool IsAndroidVersionAtLeast31 = false;
#endif

        internal static bool PlatformCryptoProviderFunctionalP256 => CngProviderFunctional(PlatformCryptoProvider, CngAlgorithm.ECDsaP256);
        internal static bool PlatformCryptoProviderFunctionalP384 => CngProviderFunctional(PlatformCryptoProvider, CngAlgorithm.ECDsaP384);
        internal static bool PlatformCryptoProviderFunctionalRsa => CngProviderFunctional(PlatformCryptoProvider, CngAlgorithm.Rsa);
        internal static bool SoftwareKeyStorageProviderFunctionalP256 => CngProviderFunctional(SoftwareKeyStorageProvider, CngAlgorithm.ECDsaP256);
    }
}
