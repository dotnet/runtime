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
        private static readonly RSAParameters s_rsa384Parameters = new RSAParameters
        {
            Modulus = new byte[]
            {
                0xDA, 0xCC, 0x22, 0xD8, 0x6E, 0x67, 0x15, 0x75,
                0x03, 0x2E, 0x31, 0xF2, 0x06, 0xDC, 0xFC, 0x19,
                0x2C, 0x65, 0xE2, 0xD5, 0x10, 0x89, 0xE5, 0x11,
                0x2D, 0x09, 0x6F, 0x28, 0x82, 0xAF, 0xDB, 0x5B,
                0x78, 0xCD, 0xB6, 0x57, 0x2F, 0xD2, 0xF6, 0x1D,
                0xB3, 0x90, 0x47, 0x22, 0x32, 0xE3, 0xD9, 0xF5,
            },
            Exponent = new byte[]
            {
                0x01, 0x00, 0x01,
            },
            D = new byte[]
            {
                0x7A, 0x59, 0xBD, 0x02, 0x9A, 0x7A, 0x3A, 0x9D,
                0x7C, 0x71, 0xD0, 0xAC, 0x2E, 0xFA, 0x54, 0x5F,
                0x1F, 0x5C, 0xBA, 0x43, 0xBB, 0x43, 0xE1, 0x3B,
                0x78, 0x77, 0xAF, 0x82, 0xEF, 0xEB, 0x40, 0xC3,
                0x8D, 0x1E, 0xCD, 0x73, 0x7F, 0x5B, 0xF9, 0xC8,
                0x96, 0x92, 0xB2, 0x9C, 0x87, 0x5E, 0xD6, 0xE1,
            },
            P = new byte[]
            {
                0xFA, 0xDB, 0xD7, 0xF8, 0xA1, 0x8B, 0x3A, 0x75,
                0xA4, 0xF6, 0xDF, 0xAE, 0xE3, 0x42, 0x6F, 0xD0,
                0xFF, 0x8B, 0xAC, 0x74, 0xB6, 0x72, 0x2D, 0xEF,
            },
            DP = new byte[]
            {
                0x24, 0xFF, 0xBB, 0xD0, 0xDD, 0xF2, 0xAD, 0x02,
                0xA0, 0xFC, 0x10, 0x6D, 0xB8, 0xF3, 0x19, 0x8E,
                0xD7, 0xC2, 0x00, 0x03, 0x8E, 0xCD, 0x34, 0x5D,
            },
            Q = new byte[]
            {
                0xDF, 0x48, 0x14, 0x4A, 0x6D, 0x88, 0xA7, 0x80,
                0x14, 0x4F, 0xCE, 0xA6, 0x6B, 0xDC, 0xDA, 0x50,
                0xD6, 0x07, 0x1C, 0x54, 0xE5, 0xD0, 0xDA, 0x5B,
            },
            DQ = new byte[]
            {
                0x85, 0xDF, 0x73, 0xBB, 0x04, 0x5D, 0x91, 0x00,
                0x6C, 0x2D, 0x45, 0x9B, 0xE6, 0xC4, 0x2E, 0x69,
                0x95, 0x4A, 0x02, 0x24, 0xAC, 0xFE, 0x42, 0x4D,
            },
            InverseQ = new byte[]
            {
                0x1A, 0x3A, 0x76, 0x9C, 0x21, 0x26, 0x2B, 0x84,
                0xCA, 0x9C, 0xA9, 0x62, 0x0F, 0x98, 0xD2, 0xF4,
                0x3E, 0xAC, 0xCC, 0xD4, 0x87, 0x9A, 0x6F, 0xFD,
            },
        };

        private static readonly Dictionary<CngAlgorithm, bool> s_platformCryptoSupportedAlgorithms = new();

        private static readonly Lazy<bool> s_lazyIsRSA384Supported = new Lazy<bool>(() =>
        {
            // Linux and Apple are known to support RSA-384, so return true without checking.
            if (PlatformDetection.IsLinux || PlatformDetection.IsApplePlatform)
            {
                return true;
            }

            RSA rsa = RSA.Create();

            try
            {
                rsa.ImportParameters(s_rsa384Parameters);
                return true;
            }
            catch (CryptographicException)
            {
                return false;
            }
            finally
            {
                rsa.Dispose();
            }
        });

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
                try
                {
                    using CngKeyWrapper key = CngKeyWrapper.CreateMicrosoftPlatformCryptoProvider(
                            algorithm,
                            keySuffix: $"{algorithm.Algorithm}Key");

                    return true;
                }
                catch (CryptographicException)
                {
                    return false;
                }
            }
        }

        private static bool CheckIfVbsAvailable()
        {
#if !NETFRAMEWORK
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return false;
            }
#endif

            try
            {
                const CngKeyCreationOptions RequireVbs = (CngKeyCreationOptions)0x00020000;
#if !NETFRAMEWORK
                Assert.Equal(CngKeyCreationOptions.RequireVbs, RequireVbs);
#endif

                using CngKeyWrapper key = CngKeyWrapper.CreateMicrosoftSoftwareKeyStorageProvider(
                        CngAlgorithm.ECDsaP256,
                        RequireVbs,
                        keySuffix: $"{CngAlgorithm.ECDsaP256.Algorithm}Key");

                return true;
            }
            catch (CryptographicException)
            {
                return false;
            }
        }

        // Platforms that use Apple Cryptography
        internal const TestPlatforms AppleCrypto = TestPlatforms.OSX | TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst;
        internal const TestPlatforms MobileAppleCrypto = TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst;

        // Platforms that support OpenSSL - all Unix except OSX/iOS/tvOS/MacCatalyst, Android, and Browser
        internal const TestPlatforms OpenSSL = TestPlatforms.AnyUnix & ~(AppleCrypto | TestPlatforms.Android | TestPlatforms.Browser);

        // Whether or not the current platform supports RC2
        internal static readonly bool IsRC2Supported = !PlatformDetection.IsAndroid;

        internal static bool IsRSA384Supported => s_lazyIsRSA384Supported.Value;

#if NET
        internal static readonly bool IsAndroidVersionAtLeast31 = OperatingSystem.IsAndroidVersionAtLeast(31);
#else
        internal static readonly bool IsAndroidVersionAtLeast31 = false;
#endif

        internal static bool PlatformCryptoProviderFunctionalP256 => PlatformCryptoProviderFunctional(CngAlgorithm.ECDsaP256);
        internal static bool PlatformCryptoProviderFunctionalP384 => PlatformCryptoProviderFunctional(CngAlgorithm.ECDsaP384);
        internal static bool PlatformCryptoProviderFunctionalRsa => PlatformCryptoProviderFunctional(CngAlgorithm.Rsa);

        private static bool? s_isVbsAvailable;
        internal static bool IsVbsAvailable => s_isVbsAvailable ??= CheckIfVbsAvailable();
    }
}
