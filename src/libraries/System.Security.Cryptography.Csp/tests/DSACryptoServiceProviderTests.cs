// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.Dsa.Tests;
using Xunit;

namespace System.Security.Cryptography.Csp.Tests
{
    public class DSACryptoServiceProviderTests
    {
        const int PROV_DSS_DH = 13;

        public static bool SupportsKeyGeneration => DSAFactory.SupportsKeyGeneration;

        [Fact]
        public static void DefaultKeySize()
        {
            using (var dsa = new DSACryptoServiceProvider())
            {
                Assert.Equal(1024, dsa.KeySize);
            }
        }

        [Fact]
        public static void PublicOnly_DefaultKey()
        {
            using (var dsa = new DSACryptoServiceProvider())
            {
                // This will call the key into being, which should create a public/private pair,
                // therefore it should not be public-only.
                Assert.False(dsa.PublicOnly);
            }
        }

        [Fact]
        public static void PublicOnly_WithPrivateKey()
        {
            using (var dsa = new DSACryptoServiceProvider())
            {
                dsa.ImportParameters(DSATestData.GetDSA1024Params());

                Assert.False(dsa.PublicOnly);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // No support for CspParameters on Unix
        public static void CreateKey()
        {
            CspParameters cspParameters = new CspParameters(PROV_DSS_DH);

            using (var dsa = new DSACryptoServiceProvider(cspParameters))
            {
                CspKeyContainerInfo containerInfo = dsa.CspKeyContainerInfo;
                Assert.Equal(PROV_DSS_DH, containerInfo.ProviderType);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // No support for CspParameters on Unix
        public static void CreateKey_RoundtripBlob()
        {
            const int KeySize = 512;

            CspParameters cspParameters = new CspParameters(PROV_DSS_DH);
            byte[] blob;

            using (var dsa = new DSACryptoServiceProvider(KeySize, cspParameters))
            {
                CspKeyContainerInfo containerInfo = dsa.CspKeyContainerInfo;
                Assert.Equal(PROV_DSS_DH, containerInfo.ProviderType);
                Assert.Equal(KeySize, dsa.KeySize);

                blob = dsa.ExportCspBlob(true);
            }

            using (var dsa = new DSACryptoServiceProvider())
            {
                dsa.ImportCspBlob(blob);

                CspKeyContainerInfo containerInfo = dsa.CspKeyContainerInfo;

                // The provider information is not persisted in the blob
                Assert.Equal(PROV_DSS_DH, containerInfo.ProviderType);
                Assert.Equal(KeySize, dsa.KeySize);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // No support for CspKeyContainerInfo on Unix
        public static void DefaultKey_Parameters()
        {
            using (var dsa = new DSACryptoServiceProvider())
            {
                CspKeyContainerInfo keyContainerInfo = dsa.CspKeyContainerInfo;

                Assert.NotNull(keyContainerInfo);
                Assert.Equal(PROV_DSS_DH, keyContainerInfo.ProviderType);

                // This shouldn't be localized, so it should be safe to test on all cultures
                Assert.Equal("Microsoft Enhanced DSS and Diffie-Hellman Cryptographic Provider", keyContainerInfo.ProviderName);

                Assert.Null(keyContainerInfo.KeyContainerName);
                Assert.Equal(string.Empty, keyContainerInfo.UniqueKeyContainerName);

                Assert.False(keyContainerInfo.HardwareDevice, "HardwareDevice");
                Assert.False(keyContainerInfo.MachineKeyStore, "MachineKeyStore");
                Assert.False(keyContainerInfo.Protected, "Protected");
                Assert.False(keyContainerInfo.Removable, "Removable");

                // Ephemeral keys don't successfully request the exportable bit.
                Assert.ThrowsAny<CryptographicException>(() => keyContainerInfo.Exportable);

                Assert.True(keyContainerInfo.RandomlyGenerated, "RandomlyGenerated");

                Assert.Equal(KeyNumber.Signature, keyContainerInfo.KeyNumber);
            }
        }

        [Fact]
        public static void DefaultKey_NotPersisted()
        {
            using (var dsa = new DSACryptoServiceProvider())
            {
                Assert.False(dsa.PersistKeyInCsp);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // No support for CspParameters on Unix
        public static void NamedKey_DefaultProvider()
        {
            const int KeySize = 1024;

            CspParameters cspParameters = new CspParameters
            {
                ProviderType = PROV_DSS_DH,
                KeyContainerName = Guid.NewGuid().ToString(),
            };

            using (new DsaKeyLifetime(cspParameters))
            {
                byte[] privateBlob;
                string uniqueKeyContainerName;

                using (var dsa = new DSACryptoServiceProvider(KeySize, cspParameters))
                {
                    Assert.True(dsa.PersistKeyInCsp, "dsa.PersistKeyInCsp");
                    Assert.Equal(cspParameters.KeyContainerName, dsa.CspKeyContainerInfo.KeyContainerName);

                    uniqueKeyContainerName = dsa.CspKeyContainerInfo.UniqueKeyContainerName;
                    Assert.NotNull(uniqueKeyContainerName);
                    Assert.NotEqual(string.Empty, uniqueKeyContainerName);

                    privateBlob = dsa.ExportCspBlob(true);
                    Assert.True(dsa.CspKeyContainerInfo.Exportable, "dsa.CspKeyContainerInfo.Exportable");
                }

                // Fail if the key didn't persist
                cspParameters.Flags |= CspProviderFlags.UseExistingKey;

                using (var dsa = new DSACryptoServiceProvider(cspParameters))
                {
                    Assert.True(dsa.PersistKeyInCsp);
                    Assert.Equal(KeySize, dsa.KeySize);

                    Assert.Equal(uniqueKeyContainerName, dsa.CspKeyContainerInfo.UniqueKeyContainerName);

                    byte[] blob2 = dsa.ExportCspBlob(true);
                    Assert.Equal(privateBlob, blob2);
                }
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // No support for CspParameters on Unix
        public static void NonExportable_Ephemeral()
        {
            CspParameters cspParameters = new CspParameters
            {
                ProviderType = PROV_DSS_DH,
                Flags = CspProviderFlags.UseNonExportableKey,
            };

            using (var dsa = new DSACryptoServiceProvider(cspParameters))
            {
                // Ephemeral keys don't successfully request the exportable bit.
                Assert.ThrowsAny<CryptographicException>(() => dsa.CspKeyContainerInfo.Exportable);

                Assert.ThrowsAny<CryptographicException>(() => dsa.ExportCspBlob(true));
                Assert.ThrowsAny<CryptographicException>(() => dsa.ExportParameters(true));
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // No support for CspParameters on Unix
        public static void NonExportable_Persisted()
        {
            CspParameters cspParameters = new CspParameters
            {
                ProviderType = PROV_DSS_DH,
                KeyContainerName = Guid.NewGuid().ToString(),
                Flags = CspProviderFlags.UseNonExportableKey,
            };

            using (new DsaKeyLifetime(cspParameters))
            {
                using (var dsa = new DSACryptoServiceProvider(cspParameters))
                {
                    Assert.False(dsa.CspKeyContainerInfo.Exportable, "dsa.CspKeyContainerInfo.Exportable");

                    Assert.ThrowsAny<CryptographicException>(() => dsa.ExportCspBlob(true));
                    Assert.ThrowsAny<CryptographicException>(() => dsa.ExportParameters(true));
                }
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public static void Ctor_UseCspParameter_Throws_Unix()
        {
            var cspParameters = new CspParameters();
            Assert.Throws<PlatformNotSupportedException>(() => new DSACryptoServiceProvider(cspParameters));
            Assert.Throws<PlatformNotSupportedException>(() => new DSACryptoServiceProvider(0, cspParameters));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public static void CspKeyContainerInfo_Throws_Unix()
        {

            using (var dsa = new DSACryptoServiceProvider())
            {
                Assert.Throws<PlatformNotSupportedException>(() => (dsa.CspKeyContainerInfo));
            }
        }

        [Fact]
        public static void ImportParameters_KeyTooBig_Throws()
        {
            using (var dsa = new DSACryptoServiceProvider())
            {
                // Verify that the Unix shims throws the same exception as Windows when large keys imported
                Assert.ThrowsAny<CryptographicException>(() => dsa.ImportParameters(DSATestData.GetDSA2048Params()));
            }
        }

        [ConditionalFact(nameof(SupportsKeyGeneration))]
        public static void VerifyHash_InvalidHashAlgorithm_Throws()
        {
            byte[] hashVal = SHA1.HashData(DSATestData.HelloBytes);

            using (var dsa = new DSACryptoServiceProvider())
            {
                byte[] signVal = dsa.SignData(DSATestData.HelloBytes);
                Assert.ThrowsAny<CryptographicException>(() => dsa.VerifyHash(hashVal, "SHA256", signVal));
            }
        }

        [ConditionalFact(nameof(SupportsKeyGeneration))]
        public static void SignHash_DefaultAlgorithm_Success()
        {
            byte[] hashVal = SHA1.HashData(DSATestData.HelloBytes);

            using (var dsa = new DSACryptoServiceProvider())
            {
                byte[] signVal = dsa.SignHash(hashVal, null);
                Assert.True(dsa.VerifyHash(hashVal, null, signVal));
            }
        }

        [ConditionalFact(nameof(SupportsKeyGeneration))]
        public static void SignHash_InvalidHashAlgorithm_Throws()
        {
            byte[] hashVal = SHA256.HashData(DSATestData.HelloBytes);

            using (var dsa = new DSACryptoServiceProvider())
            {
                Assert.ThrowsAny<CryptographicException>(() => dsa.SignHash(hashVal, "SHA256"));
            }
        }

        [ConditionalFact(nameof(SupportsKeyGeneration))]
        public static void VerifyHash_DefaultAlgorithm_Success()
        {
            byte[] hashVal = SHA1.HashData(DSATestData.HelloBytes);

            using (var dsa = new DSACryptoServiceProvider())
            {
                byte[] signVal = dsa.SignData(DSATestData.HelloBytes);
                Assert.True(dsa.VerifyHash(hashVal, null, signVal));
            }
        }

        [ConditionalFact(nameof(SupportsKeyGeneration))]
        public static void VerifyHash_CaseInsensitive_Success()
        {
            byte[] hashVal = SHA1.HashData(DSATestData.HelloBytes);

            using (var dsa = new DSACryptoServiceProvider())
            {
                byte[] signVal = dsa.SignData(DSATestData.HelloBytes, new HashAlgorithmName("SHA1"));
                Assert.True(dsa.VerifyHash(hashVal, "SHA1", signVal));

                signVal = dsa.SignData(DSATestData.HelloBytes, new HashAlgorithmName("SHA1")); // lowercase would fail here
                Assert.True(dsa.VerifyHash(hashVal, "sha1", signVal));
            }
        }

        [Fact]
        public static void SignData_CaseInsensitive_Throws()
        {
            using (var dsa = new DSACryptoServiceProvider())
            {
                Assert.ThrowsAny<CryptographicException>(() => dsa.SignData(DSATestData.HelloBytes, new HashAlgorithmName("sha1")));
            }
        }

        [Fact]
        public static void SignData_InvalidHashAlgorithm_Throws()
        {
            using (var dsa = new DSACryptoServiceProvider())
            {
                Assert.ThrowsAny<CryptographicException>(() => dsa.SignData(DSATestData.HelloBytes, HashAlgorithmName.SHA256));
                Assert.ThrowsAny<CryptographicException>(() => dsa.SignData(new System.IO.MemoryStream(), HashAlgorithmName.SHA256));
                Assert.ThrowsAny<CryptographicException>(() => dsa.SignData(DSATestData.HelloBytes, 0, DSATestData.HelloBytes.Length, HashAlgorithmName.SHA256));
            }
        }

        [ConditionalFact(nameof(SupportsKeyGeneration))]
        public static void VerifyData_InvalidHashAlgorithm_Throws()
        {
            using (var dsa = new DSACryptoServiceProvider())
            {
                byte[] signVal = dsa.SignData(DSATestData.HelloBytes);

                Assert.ThrowsAny<CryptographicException>(() => dsa.VerifyData(DSATestData.HelloBytes, signVal, HashAlgorithmName.SHA256));
                Assert.ThrowsAny<CryptographicException>(() => dsa.VerifyData(DSATestData.HelloBytes, 0, DSATestData.HelloBytes.Length, signVal, HashAlgorithmName.SHA256));
            }
        }

        [Fact]
        public static void SignatureAlgorithm_Success()
        {
            using (var dsa = new DSACryptoServiceProvider())
            {
                Assert.Equal("http://www.w3.org/2000/09/xmldsig#dsa-sha1", dsa.SignatureAlgorithm);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)] // Only Unix has _impl shim pattern
        public static void TestShimOverloads_Unix()
        {
            ShimHelpers.VerifyAllBaseMembersOverloaded(typeof(DSACryptoServiceProvider));
        }

        private sealed class DsaKeyLifetime : IDisposable
        {
            private readonly CspParameters _cspParameters;

            internal DsaKeyLifetime(CspParameters cspParameters)
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
                    using (var dsa = new DSACryptoServiceProvider(_cspParameters))
                    {
                        // Delete the key at the end of this using
                        dsa.PersistKeyInCsp = false;
                    }
                }
                catch (CryptographicException)
                {
                }
            }
        }
    }
}
