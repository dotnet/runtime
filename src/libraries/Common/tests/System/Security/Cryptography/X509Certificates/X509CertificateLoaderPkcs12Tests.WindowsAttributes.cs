// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    public abstract partial class X509CertificateLoaderPkcs12Tests
    {
        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void VerifyPreserveKeyName(bool preserveName, bool machineKey)
        {
            Pkcs12LoaderLimits loaderLimits = new Pkcs12LoaderLimits
            {
                PreserveKeyName = preserveName,
            };

            string keyName = Guid.NewGuid().ToString("D");
            byte[] pfx = MakeAttributeTest(keyName: keyName, friendlyName: "Non-preserved", machineKey: machineKey);

            X509Certificate2 cert = LoadPfxNoFile(
                pfx,
                keyStorageFlags: X509KeyStorageFlags.DefaultKeySet,
                loaderLimits: loaderLimits);

            using (cert)
            {
                using (RSA key = cert.GetRSAPrivateKey())
                using (CngKey cngKey = Assert.IsType<RSACng>(key).Key)
                {
                    if (preserveName)
                    {
                        Assert.Equal(keyName, cngKey.KeyName);
                    }
                    else
                    {
                        Assert.NotEqual(keyName, cngKey.KeyName);
                    }

                    // MachineKey is preserved irrespective of PreserveKeyName
                    Assert.Equal(machineKey, cngKey.IsMachineKey);
                }

                // Alias was not preserved
                Assert.Empty(cert.FriendlyName);
            }
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void VerifyPreserveAlias(bool preserveAlias, bool machineKey)
        {
            Pkcs12LoaderLimits loaderLimits = new Pkcs12LoaderLimits
            {
                PreserveCertificateAlias = preserveAlias,
            };

            string keyName = Guid.NewGuid().ToString("D");
            string alias = Guid.NewGuid().ToString("D");
            byte[] pfx = MakeAttributeTest(keyName: keyName, friendlyName: alias, machineKey: machineKey);

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
                using (CngKey cngKey = Assert.IsType<RSACng>(key).Key)
                {
                    // Key name was not preserved
                    Assert.NotEqual(keyName, cngKey.KeyName);

                    // MachineKey is preserved irrespective of PreserveCertificateAlias
                    Assert.Equal(machineKey, cngKey.IsMachineKey);
                }
            }
        }

        [Theory]
        [InlineData(true, true, true)]
        [InlineData(true, true, false)]
        [InlineData(true, false, true)]
        [InlineData(true, false, false)]
        [InlineData(false, true, true)]
        [InlineData(false, true, false)]
        [InlineData(false, false, true)]
        [InlineData(false, false, false)]
        public void VerifyPreserveProvider(bool preserveProvider, bool preserveName, bool machineKey)
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
            byte[] pfx = MakeAttributeTest(keyName: keyName, friendlyName: alias, useCapi: true, machineKey: machineKey);

            X509Certificate2 cert = LoadPfxNoFile(
                pfx,
                keyStorageFlags: X509KeyStorageFlags.DefaultKeySet,
                loaderLimits: loaderLimits);

            using (cert)
            {
                using (RSA key = cert.GetRSAPrivateKey())
                using (CngKey cngKey = Assert.IsType<RSACng>(key).Key)
                {
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

                    // MachineKey is preserved irrespective of PreserveKeyName or PreserveStorageProvider
                    Assert.Equal(machineKey, cngKey.IsMachineKey);
                }

                // Alias is not preserved
                Assert.Empty(cert.FriendlyName);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void VerifyNamesWithDuplicateAttributes(bool noLimits)
        {
            // This test mainly shows that when duplicate attributes are present contents
            // processed by our filter and processed directly by PFXImportCertStore come up
            // with the same answer.

            Pkcs12LoaderLimits limits = Pkcs12LoaderLimits.DangerousNoLimits;

            // DangerousNoLimits is tested by reference, by cloning the object we
            // use a functional equivalent using the work-limiting and attribute-filtering
            // loader.
            if (!noLimits)
            {
                limits = new Pkcs12LoaderLimits(limits);
            }

            X509Certificate2 cert = LoadPfxNoFile(
                TestData.DuplicateAttributesPfx,
                TestData.PlaceholderPw,
                X509KeyStorageFlags.DefaultKeySet,
                loaderLimits: limits);

            using (cert)
            {
                Assert.Equal("Certificate 1", cert.GetNameInfo(X509NameType.SimpleName, false));
                Assert.True(cert.HasPrivateKey, "cert.HasPrivateKey");
                Assert.Equal("Microsoft Enhanced RSA and AES Cryptographic Provider", cert.FriendlyName);

                using (RSA key = cert.GetRSAPrivateKey())
                using (CngKey cngKey = Assert.IsType<RSACng>(key).Key)
                {
                    Assert.Equal("Microsoft Enhanced RSA and AES Cryptographic Provider", cngKey.Provider.Provider);
                    Assert.True(cngKey.IsMachineKey, "cngKey.IsMachineKey");

                    // If keyname000 gets taken, we'll get a random key name on import.  What's important is that we
                    // don't pick the second entry: keyname001.
                    Assert.NotEqual("keyname001", cngKey.KeyName);
                }
            }
        }

        private static byte[] MakeAttributeTest(
            string? keyName = null,
            string? friendlyName = null,
            bool useCapi = false,
            bool machineKey = false,
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
                            Flags = machineKey ? CspProviderFlags.UseMachineKeyStore : CspProviderFlags.NoFlags,
                        };

                        rsaCsp = new RSACryptoServiceProvider(2048, cspParameters);
                        key = rsaCsp;
                    }
                    else
                    {
                        CngKeyCreationParameters cngParams = new CngKeyCreationParameters
                        {
                            ExportPolicy = CngExportPolicies.AllowPlaintextExport,
                            KeyCreationOptions = machineKey ? CngKeyCreationOptions.MachineKey : CngKeyCreationOptions.None,
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
