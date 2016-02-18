// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography.Rsa.Tests;
using Xunit;

namespace System.Security.Cryptography.Csp.Tests
{
    public class RSACryptoServiceProviderTests
    {
        const int PROV_RSA_FULL = 1;
        const int PROV_RSA_AES = 24;

        [Fact]
        public static void DefaultKeySize()
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                Assert.Equal(1024, rsa.KeySize);
            }
        }

        [Fact]
        public static void PublicOnly_DefaultKey()
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                // This will call the key into being, which should create a public/private pair,
                // therefore it should not be public-only.
                Assert.False(rsa.PublicOnly);
            }
        }

        [Fact]
        public static void PublicOnly_WithPrivateKey()
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportParameters(TestData.RSA1024Params);

                Assert.False(rsa.PublicOnly);
            }
        }

        [Fact]
        public static void PublicOnly_WithNoPrivate()
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                RSAParameters publicParams = new RSAParameters
                {
                    Modulus = TestData.RSA1024Params.Modulus,
                    Exponent = TestData.RSA1024Params.Exponent,
                };

                rsa.ImportParameters(publicParams);
                Assert.True(rsa.PublicOnly);
            }
        }

        [Fact]
        public static void CreateKey_LegacyProvider()
        {
            CspParameters cspParameters = new CspParameters(PROV_RSA_FULL);

            using (var rsa = new RSACryptoServiceProvider(cspParameters))
            {
                CspKeyContainerInfo containerInfo = rsa.CspKeyContainerInfo;
                Assert.Equal(PROV_RSA_FULL, containerInfo.ProviderType);
            }
        }

        [Fact]
        public static void CreateKey_LegacyProvider_RoundtripBlob()
        {
            const int KeySize = 512;

            CspParameters cspParameters = new CspParameters(PROV_RSA_FULL);
            byte[] blob;

            using (var rsa = new RSACryptoServiceProvider(KeySize, cspParameters))
            {
                CspKeyContainerInfo containerInfo = rsa.CspKeyContainerInfo;
                Assert.Equal(PROV_RSA_FULL, containerInfo.ProviderType);
                Assert.Equal(KeySize, rsa.KeySize);

                blob = rsa.ExportCspBlob(true);
            }

            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportCspBlob(blob);

                CspKeyContainerInfo containerInfo = rsa.CspKeyContainerInfo;

                // The provider information is not persisted in the blob
                Assert.Equal(PROV_RSA_AES, containerInfo.ProviderType);
                Assert.Equal(KeySize, rsa.KeySize);
            }
        }

        [Fact]
        public static void DefaultKey_Parameters()
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                CspKeyContainerInfo keyContainerInfo = rsa.CspKeyContainerInfo;

                Assert.NotNull(keyContainerInfo);
                Assert.Equal(PROV_RSA_AES, keyContainerInfo.ProviderType);

                // This shouldn't be localized, so it should be safe to test on all cultures
                Assert.Equal("Microsoft Enhanced RSA and AES Cryptographic Provider", keyContainerInfo.ProviderName);

                Assert.Null(keyContainerInfo.KeyContainerName);
                Assert.Equal(string.Empty, keyContainerInfo.UniqueKeyContainerName);

                Assert.False(keyContainerInfo.HardwareDevice, "HardwareDevice");
                Assert.False(keyContainerInfo.MachineKeyStore, "MachineKeyStore");
                Assert.False(keyContainerInfo.Protected, "Protected");
                Assert.False(keyContainerInfo.Removable, "Removable");

                // Ephemeral keys don't successfully request the exportable bit.
                Assert.ThrowsAny<CryptographicException>(() => keyContainerInfo.Exportable);

                Assert.True(keyContainerInfo.RandomlyGenerated, "RandomlyGenerated");

                Assert.Equal(KeyNumber.Exchange, keyContainerInfo.KeyNumber);
            }
        }

        [Fact]
        public static void DefaultKey_NotPersisted()
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                Assert.False(rsa.PersistKeyInCsp);
            }
        }

        [Fact]
        public static void NamedKey_DefaultProvider()
        {
            const int KeySize = 2048;

            CspParameters cspParameters = new CspParameters
            {
                KeyContainerName = Guid.NewGuid().ToString(),
            };

            using (new RsaKeyLifetime(cspParameters))
            {
                byte[] privateBlob;
                string uniqueKeyContainerName;

                using (var rsa = new RSACryptoServiceProvider(KeySize, cspParameters))
                {
                    Assert.True(rsa.PersistKeyInCsp, "rsa.PersistKeyInCsp");
                    Assert.Equal(cspParameters.KeyContainerName, rsa.CspKeyContainerInfo.KeyContainerName);

                    uniqueKeyContainerName = rsa.CspKeyContainerInfo.UniqueKeyContainerName;
                    Assert.NotNull(uniqueKeyContainerName);
                    Assert.NotEqual(string.Empty, uniqueKeyContainerName);

                    privateBlob = rsa.ExportCspBlob(true);
                    Assert.True(rsa.CspKeyContainerInfo.Exportable, "rsa.CspKeyContainerInfo.Exportable");
                }

                // Fail if the key didn't persist
                cspParameters.Flags |= CspProviderFlags.UseExistingKey;

                using (var rsa = new RSACryptoServiceProvider(cspParameters))
                {
                    Assert.True(rsa.PersistKeyInCsp);
                    Assert.Equal(KeySize, rsa.KeySize);

                    Assert.Equal(uniqueKeyContainerName, rsa.CspKeyContainerInfo.UniqueKeyContainerName);

                    byte[] blob2 = rsa.ExportCspBlob(true);
                    Assert.Equal(privateBlob, blob2);
                }
            }
        }

        [Fact]
        public static void NamedKey_AlternateProvider()
        {
            const int KeySize = 512;

            CspParameters cspParameters = new CspParameters(PROV_RSA_FULL)
            {
                KeyContainerName = Guid.NewGuid().ToString(),
            };

            using (new RsaKeyLifetime(cspParameters))
            {
                byte[] privateBlob;
                string uniqueKeyContainerName;

                using (var rsa = new RSACryptoServiceProvider(KeySize, cspParameters))
                {
                    Assert.True(rsa.PersistKeyInCsp);
                    Assert.Equal(PROV_RSA_FULL, rsa.CspKeyContainerInfo.ProviderType);

                    privateBlob = rsa.ExportCspBlob(true);

                    Assert.Equal(cspParameters.KeyContainerName, rsa.CspKeyContainerInfo.KeyContainerName);

                    uniqueKeyContainerName = rsa.CspKeyContainerInfo.UniqueKeyContainerName;
                    Assert.NotNull(uniqueKeyContainerName);
                    Assert.NotEqual(string.Empty, uniqueKeyContainerName);
                }

                // Fail if the key didn't persist
                cspParameters.Flags |= CspProviderFlags.UseExistingKey;

                using (var rsa = new RSACryptoServiceProvider(cspParameters))
                {
                    Assert.True(rsa.PersistKeyInCsp);
                    Assert.Equal(KeySize, rsa.KeySize);

                    // Since we're specifying the provider explicitly it should still match.
                    Assert.Equal(PROV_RSA_FULL, rsa.CspKeyContainerInfo.ProviderType);

                    Assert.Equal(uniqueKeyContainerName, rsa.CspKeyContainerInfo.UniqueKeyContainerName);

                    byte[] blob2 = rsa.ExportCspBlob(true);
                    Assert.Equal(privateBlob, blob2);
                }
            }
        }

        [Fact]
        public static void NonExportable_Ephemeral()
        {
            CspParameters cspParameters = new CspParameters
            {
                Flags = CspProviderFlags.UseNonExportableKey,
            };

            using (var rsa = new RSACryptoServiceProvider(cspParameters))
            {
                // Ephemeral keys don't successfully request the exportable bit.
                Assert.ThrowsAny<CryptographicException>(() => rsa.CspKeyContainerInfo.Exportable);

                Assert.Throws<CryptographicException>(() => rsa.ExportCspBlob(true));
                Assert.Throws<CryptographicException>(() => rsa.ExportParameters(true));
            }
        }

        [Fact]
        public static void NonExportable_Persisted()
        {
            CspParameters cspParameters = new CspParameters
            {
                KeyContainerName = Guid.NewGuid().ToString(),
                Flags = CspProviderFlags.UseNonExportableKey,
            };

            using (new RsaKeyLifetime(cspParameters))
            {
                using (var rsa = new RSACryptoServiceProvider(cspParameters))
                {
                    Assert.False(rsa.CspKeyContainerInfo.Exportable, "rsa.CspKeyContainerInfo.Exportable");

                    Assert.Throws<CryptographicException>(() => rsa.ExportCspBlob(true));
                    Assert.Throws<CryptographicException>(() => rsa.ExportParameters(true));
                }
            }
        }

        private sealed class RsaKeyLifetime : IDisposable
        {
            private readonly CspParameters _cspParameters;

            internal RsaKeyLifetime(CspParameters cspParameters)
            {
                const CspProviderFlags CopyableFlags =
                    CspProviderFlags.UseMachineKeyStore;

                _cspParameters = new CspParameters(
                    cspParameters.ProviderType,
                    cspParameters.ProviderName,
                    cspParameters.KeyContainerName)
                {
                    // If the test failed before creating the key, don't bother recreating it.
                    Flags = (cspParameters.Flags & CopyableFlags) | CspProviderFlags.UseExistingKey,
                };
            }

            public void Dispose()
            {
                try
                {
                    using (var rsa = new RSACryptoServiceProvider(_cspParameters))
                    {
                        // Delete the key at the end of this using
                        rsa.PersistKeyInCsp = false;
                    }
                }
                catch (CryptographicException)
                {
                }
            }
        }
    }
}
