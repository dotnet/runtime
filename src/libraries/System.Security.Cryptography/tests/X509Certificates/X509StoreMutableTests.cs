// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    [OuterLoop("Modifies system state")]
    [SkipOnPlatform(TestPlatforms.Browser, "Browser doesn't support X509Store")]
    public static partial class X509StoreMutableTests
    {
        [ConditionalFact(nameof(PermissionsAllowStoreWrite))]
        public static void AddToStoreTwice()
        {
            using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            using (var cert = new X509Certificate2(TestData.PfxData, TestData.PfxDataPassword, X509KeyStorageFlags.Exportable))
            using (var certOnly = new X509Certificate2(cert.RawData))
            {
                store.Open(OpenFlags.ReadWrite);

                // Defensive removal.
                store.Remove(certOnly);
                Assert.False(IsCertInStore(cert, store), "PfxData certificate was found on pre-condition");

                store.Add(cert);
                Assert.True(IsCertInStore(certOnly, store), "PfxData certificate was found after add");

                // No exception for duplicate item.
                store.Add(cert);

                // Cleanup
                store.Remove(certOnly);
            }
        }

        [ConditionalFact(nameof(PermissionsAllowStoreWrite))]
        public static void AddPrivateAfterPublic()
        {
            using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            using (var cert = new X509Certificate2(TestData.PfxData, TestData.PfxDataPassword, X509KeyStorageFlags.Exportable))
            using (var certOnly = new X509Certificate2(cert.RawData))
            {
                store.Open(OpenFlags.ReadWrite);

                // Defensive removal.
                store.Remove(certOnly);
                Assert.False(IsCertInStore(cert, store), "PfxData certificate was found on pre-condition");

                store.Add(certOnly);
                Assert.True(IsCertInStore(certOnly, store), "PfxData certificate was found after add");
                Assert.False(StoreHasPrivateKey(store, certOnly), "Store has a private key for PfxData after public-only add");

                // Add the private key
                store.Add(cert);
                Assert.True(StoreHasPrivateKey(store, certOnly), "Store has a private key for PfxData after PFX add");

                // Cleanup
                store.Remove(certOnly);
            }
        }

        [ConditionalFact(nameof(PermissionsAllowStoreWrite))]
        public static void AddPublicAfterPrivate()
        {
            using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            using (var cert = new X509Certificate2(TestData.PfxData, TestData.PfxDataPassword, X509KeyStorageFlags.Exportable))
            using (var certOnly = new X509Certificate2(cert.RawData))
            {
                store.Open(OpenFlags.ReadWrite);

                // Defensive removal.
                store.Remove(certOnly);
                Assert.False(IsCertInStore(cert, store), "PfxData certificate was found on pre-condition");

                // Add the private key
                store.Add(cert);
                Assert.True(IsCertInStore(certOnly, store), "PfxData certificate was found after add");
                Assert.True(StoreHasPrivateKey(store, certOnly), "Store has a private key for PfxData after PFX add");

                // Add the public key with no private key
                store.Add(certOnly);
                Assert.True(StoreHasPrivateKey(store, certOnly), "Store has a private key for PfxData after public-only add");

                // Cleanup
                store.Remove(certOnly);
            }
        }

        [ConditionalTheory(nameof(PermissionsAllowStoreWrite))]
        [InlineData(true)]
        [InlineData(false)]
        public static void VerifyRemove(bool withPrivateKey)
        {
            using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            using (var certWithPrivateKey = new X509Certificate2(TestData.PfxData, TestData.PfxDataPassword, X509KeyStorageFlags.Exportable))
            using (var certOnly = new X509Certificate2(certWithPrivateKey.RawData))
            {
                X509Certificate2 cert = withPrivateKey ? certWithPrivateKey : certOnly;
                store.Open(OpenFlags.ReadWrite);

                // Defensive removal.  Sort of circular, but it's the best we can do.
                store.Remove(cert);
                Assert.False(IsCertInStore(cert, store), "PfxData certificate was found on pre-condition");

                store.Add(cert);
                Assert.True(IsCertInStore(cert, store), "PfxData certificate was found after add");

                store.Remove(cert);
                Assert.False(IsCertInStore(cert, store), "PfxData certificate was found after remove");
            }
        }

        [ConditionalFact(nameof(PermissionsAllowStoreWrite))]
        public static void RemovePublicDeletesPrivateKey()
        {
            using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            using (var cert = new X509Certificate2(TestData.PfxData, TestData.PfxDataPassword, X509KeyStorageFlags.Exportable))
            using (var certOnly = new X509Certificate2(cert.RawData))
            {
                store.Open(OpenFlags.ReadWrite);

                // Defensive removal.
                store.Remove(cert);
                Assert.False(IsCertInStore(cert, store), "PfxData certificate was found on pre-condition");

                // Add the private key
                store.Add(cert);
                Assert.True(IsCertInStore(cert, store), "PfxData certificate was found after add");

                store.Remove(certOnly);
                Assert.False(IsCertInStore(cert, store), "PfxData certificate was found after remove");

                // Add back the public key only
                store.Add(certOnly);
                Assert.True(IsCertInStore(cert, store), "PfxData certificate was found after public-only add");
                Assert.False(StoreHasPrivateKey(store, cert), "Store has a private key for cert after public-only add");

                // Cleanup
                store.Remove(certOnly);
            }
        }

        private static bool StoreHasPrivateKey(X509Store store, X509Certificate2 forCert)
        {
            using (ImportedCollection coll = new ImportedCollection(store.Certificates))
            {
                foreach (X509Certificate2 storeCert in coll.Collection)
                {
                    if (forCert.Equals(storeCert))
                    {
                        return storeCert.HasPrivateKey;
                    }
                }
            }

            Assert.Fail($"Certificate ({forCert.Subject}) exists in the store");
            return false;
        }

        private static bool IsCertInStore(X509Certificate2 cert, X509Store store)
        {
            using (ImportedCollection coll = new ImportedCollection(store.Certificates))
            {
                foreach (X509Certificate2 storeCert in coll.Collection)
                {
                    if (cert.Equals(storeCert))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static int GetStoreCertificateCount(X509Store store)
        {
            using (var coll = new ImportedCollection(store.Certificates))
            {
                return coll.Collection.Count;
            }
        }
    }
}
