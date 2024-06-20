// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    public abstract partial class X509CertificateLoaderPkcs12Tests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void VerifyPreserveKeyName(bool preserveName)
        {
            Pkcs12LoaderLimits loaderLimits = new Pkcs12LoaderLimits
            {
                PreserveKeyName = preserveName,
            };

            string keyName = Guid.NewGuid().ToString("D");
            byte[] pfx = MakeAttributeTest(keyName: keyName, friendlyName: "Non-preserved");

            X509Certificate2 cert = LoadPfxNoFile(
                pfx,
                keyStorageFlags: X509KeyStorageFlags.DefaultKeySet,
                loaderLimits: loaderLimits);

            using (cert)
            {
                using (RSA key = cert.GetRSAPrivateKey())
                {
                    CngKey cngKey = Assert.IsType<RSACng>(key).Key;

                    if (preserveName)
                    {
                        Assert.Equal(keyName, cngKey.KeyName);
                    }
                    else
                    {
                        Assert.NotEqual(keyName, cngKey.KeyName);
                    }
                }

                // Alias was not preserved
                Assert.Empty(cert.FriendlyName);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void VerifyPreserveAlias(bool preserveAlias)
        {
            Pkcs12LoaderLimits loaderLimits = new Pkcs12LoaderLimits
            {
                PreserveCertificateAlias = preserveAlias,
            };

            string keyName = Guid.NewGuid().ToString("D");
            string alias = Guid.NewGuid().ToString("D");
            byte[] pfx = MakeAttributeTest(keyName: keyName, friendlyName: alias);

            X509Certificate2 cert = LoadPfxNoFile(
                pfx,
                keyStorageFlags: X509KeyStorageFlags.DefaultKeySet,
                loaderLimits: loaderLimits);

            using (cert)
            {
                if (preserveAlias)
                {
                    Assert.Equal(alias, cert.FriendlyName);
                }
                else
                {
                    Assert.Empty(cert.FriendlyName);
                }

                using (RSA key = cert.GetRSAPrivateKey())
                {
                    CngKey cngKey = Assert.IsType<RSACng>(key).Key;

                    // Key name was not preserved
                    Assert.NotEqual(keyName, cngKey.KeyName);
                }
            }
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void VerifyPreservePreserveProvider(bool preserveProvider, bool preserveName)
        {
            // This test forces a key creation with CAPI, and verifies that
            // PreserveStorageProvider keeps the key in CAPI.  Additionally,
            // it shows that PreserveKeyName and PreserveStorageProvider are independent.
            Pkcs12LoaderLimits loaderLimits = new Pkcs12LoaderLimits
            {
                PreserveKeyName = preserveName,
                PreserveStorageProvider = preserveProvider,
            };

            string keyName = Guid.NewGuid().ToString("D");
            string alias = Guid.NewGuid().ToString("D");
            byte[] pfx = MakeAttributeTest(keyName: keyName, friendlyName: alias, useCapi: true);

            X509Certificate2 cert = LoadPfxNoFile(
                pfx,
                keyStorageFlags: X509KeyStorageFlags.DefaultKeySet,
                loaderLimits: loaderLimits);

            using (cert)
            {
                using (RSA key = cert.GetRSAPrivateKey())
                {
                    CngKey cngKey = Assert.IsType<RSACng>(key).Key;

                    if (preserveName)
                    {
                        Assert.Equal(keyName, cngKey.KeyName);
                    }
                    else
                    {
                        Assert.NotEqual(keyName, cngKey.KeyName);
                    }

                    const string CapiProvider = "Microsoft Enhanced RSA and AES Cryptographic Provider";

                    if (preserveProvider)
                    {
                        Assert.Equal(CapiProvider, cngKey.Provider.Provider);
                    }
                    else
                    {
                        Assert.NotEqual(CapiProvider, cngKey.Provider.Provider);
                    }
                }

                // Alias is not preserved
                Assert.Empty(cert.FriendlyName);
            }
        }

        private static byte[] MakeAttributeTest(
            string? keyName = null,
            string? friendlyName = null,
            bool useCapi = false,
            [CallerMemberName] string testName = null)
        {
            CngKey cngKey = null;
            RSACryptoServiceProvider rsaCsp = null;

            try
            {
                RSA key;

                if (keyName is not null)
                {
                    if (useCapi)
                    {
                        CspParameters cspParameters = new CspParameters(24)
                        {
                            KeyContainerName = keyName,
                        };

                        rsaCsp = new RSACryptoServiceProvider(2048, cspParameters);
                        key = rsaCsp;
                    }
                    else
                    {
                        CngKeyCreationParameters cngParams = new CngKeyCreationParameters
                        {
                            ExportPolicy = CngExportPolicies.AllowPlaintextExport,
                        };

                        cngKey = CngKey.Create(CngAlgorithm.Rsa, keyName, cngParams);
                        key = new RSACng(cngKey);
                    }
                }
                else
                {
                    key = RSA.Create(2048);
                }

                CertificateRequest req = new CertificateRequest(
                    $"CN={testName}-{keyName}-{friendlyName}",
                    key,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                DateTimeOffset now = DateTimeOffset.UtcNow;

                using (X509Certificate2 cert = req.CreateSelfSigned(now.AddMinutes(-5), now.AddMinutes(5)))
                {
                    if (friendlyName is not null)
                    {
                        cert.FriendlyName = friendlyName;
                    }

                    return cert.Export(X509ContentType.Pfx);
                }
            }
            finally
            {
                cngKey?.Delete();

                if (rsaCsp is not null)
                {
                    rsaCsp.PersistKeyInCsp = false;
                    rsaCsp.Dispose();
                }
            }
        }
    }
}
