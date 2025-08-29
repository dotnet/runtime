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
        private static readonly Lazy<bool> s_lazyIsRC2Supported = new Lazy<bool>(() =>
        {
            if (PlatformDetection.IsAndroid)
            {
                return false;
            }

            if (PlatformDetection.IsLinux)
            {
                try
                {
                    using (RC2 rc2 = RC2.Create())
                    using (rc2.CreateEncryptor())
                    {
                    }

                    return true;
                }
                catch (PlatformNotSupportedException)
                {
                    return false;
                }
            }

            return true;
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

        private static bool CheckIfRsaPssSupported()
        {
            if (PlatformDetection.IsBrowser)
            {
                // Browser doesn't support PSS or RSA at all.
                return false;
            }

            using (RSA rsa = RSA.Create())
            {
                try
                {
                    rsa.SignData(Array.Empty<byte>(), HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
                }
                catch (CryptographicException)
                {
                    return false;
                }
            }

            return true;
        }

        // Platforms that use Apple Cryptography
        internal const TestPlatforms AppleCrypto = TestPlatforms.OSX | TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst;
        internal const TestPlatforms MobileAppleCrypto = TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst;

        // Platforms that support OpenSSL - all Unix except OSX/iOS/tvOS/MacCatalyst, Android, and Browser
        internal const TestPlatforms OpenSSL = TestPlatforms.AnyUnix & ~(AppleCrypto | TestPlatforms.Android | TestPlatforms.Browser);

        // Whether or not the current platform supports RC2
        internal static bool IsRC2Supported => s_lazyIsRC2Supported.Value;

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

        private static bool? s_isRsaPssSupported;

        /// <summary>
        /// Checks if the platform supports RSA-PSS signatures.
        /// This value is not suitable to check if RSA-PSS is supported in cert chains - see CertificateRequestChainTests.PlatformSupportsPss.
        /// </summary>
        internal static bool IsRsaPssSupported => s_isRsaPssSupported ??= CheckIfRsaPssSupported();

        internal static bool IsPqcMLKemX509Supported
        {
            get
            {
#if NETFRAMEWORK
                return false;
#else
#pragma warning disable SYSLIB5006 // PQC is experimental
                return MLKem.IsSupported && !RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
#pragma warning restore SYSLIB5006
#endif
            }
        }
    }
}
