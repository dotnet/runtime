// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Security.Cryptography.X509Certificates.Tests;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    [PlatformSpecific(TestPlatforms.Linux)]
    public static class OpenSslProviderAvailabilityTests
    {
        // Loads only the base provider, which has no ciphers, digests, MACs, KDFs or key exchange, so fetching one
        // fails as unsupported.
        private const string BaseProviderOnlyConfig = """
            openssl_conf = openssl_init

            [openssl_init]
            providers = provider_sect

            [provider_sect]
            base = base_sect

            [base_sect]
            activate = 1
            """;

        // Under this configuration, Azure Linux's OpenSSL keeps its SymCrypt provider active, and in FIPS mode OpenSSL on
        // RHEL, its rebuilds and Fedora keeps its FIPS provider active, whose algorithms vary by version.
        public static bool CanLoadOnlyBaseProvider =>
            RemoteExecutor.IsSupported && PlatformDetection.IsOpenSsl3 && PlatformDetection.IsNotSymCryptOpenSsl &&
            PlatformDetection.IsNotOpenSslFipsModeRequested;

        [ConditionalFact(typeof(OpenSslProviderAvailabilityTests), nameof(CanLoadOnlyBaseProvider))]
        public static void IsSupported_NoProviderHasAlgorithm_ReturnsFalse()
        {
            RunWithBaseProviderOnly(static () =>
            {
                Assert.False(X25519DiffieHellman.IsSupported, nameof(X25519DiffieHellman));
                Assert.False(Kmac128.IsSupported, nameof(Kmac128));
                Assert.False(Kmac256.IsSupported, nameof(Kmac256));
                Assert.False(KmacXof128.IsSupported, nameof(KmacXof128));
                Assert.False(KmacXof256.IsSupported, nameof(KmacXof256));
            });
        }

        [ConditionalFact(typeof(OpenSslProviderAvailabilityTests), nameof(CanLoadOnlyBaseProvider))]
        public static void Kdf_NoProviderHasAlgorithm_ThrowsCryptographicException()
        {
            // With no KDF from OpenSSL, HKDF and SP800108HmacCounterKdf use their managed implementations, which need
            // HMAC, which no provider has either.
            RunWithBaseProviderOnly(static () =>
            {
                Assert.ThrowsAny<CryptographicException>(() => HKDF.DeriveKey(HashAlgorithmName.SHA256, new byte[16], 8));
                Assert.ThrowsAny<CryptographicException>(
                    () => SP800108HmacCounterKdf.DeriveBytes(new byte[16], HashAlgorithmName.SHA256, "label", "context", 8));
            });
        }

        private static void RunWithBaseProviderOnly(Action test)
        {
            using (TempFileHolder config = new TempFileHolder(BaseProviderOnlyConfig))
            {
                ProcessStartInfo psi = new ProcessStartInfo { Environment = { { "OPENSSL_CONF", config.FilePath } } };
                RemoteExecutor.Invoke(test, new RemoteInvokeOptions { StartInfo = psi }).Dispose();
            }
        }
    }
}
