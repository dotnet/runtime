// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    [OuterLoop("Modifies system state")]
    [PlatformSpecific(TestPlatforms.OSX)]
    public static class X509StoreMutableTests_OSX
    {
        public static bool PermissionsAllowStoreWrite { get; } = TestPermissions();

        private static bool TestPermissions()
        {
            try
            {
                AddToStore_Exportable();
            }
            catch (CryptographicException e)
            {
                const int errSecWrPerm = -61;

                if (e.HResult == errSecWrPerm)
                {
                    return false;
                }
            }
            catch
            {
            }

            return true;
        }

        [ConditionalFact(nameof(PermissionsAllowStoreWrite))]
        public static void PersistKeySet_OSX()
        {
            using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            using (var cert = new X509Certificate2(TestData.PfxData, TestData.PfxDataPassword, X509KeyStorageFlags.DefaultKeySet))
            {
                store.Open(OpenFlags.ReadWrite);

                // Defensive removal.
                store.Remove(cert);

                Assert.False(IsCertInStore(cert, store), "PtxData certificate was found on pre-condition");

                // Opening this as persisted has now added it to login.keychain, aka CU\My.
                using (var persistedCert = new X509Certificate2(TestData.PfxData, TestData.PfxDataPassword, X509KeyStorageFlags.PersistKeySet))
                {
                    Assert.True(IsCertInStore(cert, store), "PtxData certificate was found upon PersistKeySet import");
                }

                // And ensure it didn't get removed when the certificate got disposed.
                Assert.True(IsCertInStore(cert, store), "PtxData certificate was found after PersistKeySet Dispose");

                // Cleanup.
                store.Remove(cert);
            }
        }

        [ConditionalFact(nameof(PermissionsAllowStoreWrite))]
        public static void AddToStore_NonExportable_OSX()
        {
            using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            using (var cert = new X509Certificate2(TestData.PfxData, TestData.PfxDataPassword, X509KeyStorageFlags.DefaultKeySet))
            {
                store.Open(OpenFlags.ReadWrite);

                int countBefore = GetStoreCertificateCount(store);

                // Because this has to export the key from the temporary keychain to the permanent one,
                // a non-exportable PFX load will fail.
                Assert.ThrowsAny<CryptographicException>(() => store.Add(cert));

                int countAfter = GetStoreCertificateCount(store);

                Assert.Equal(countBefore, countAfter);
            }
        }

        [ConditionalFact(nameof(PermissionsAllowStoreWrite))]
        public static void AddToStore_Exportable()
        {
            using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            using (var cert = new X509Certificate2(TestData.PfxData, TestData.PfxDataPassword, X509KeyStorageFlags.Exportable))
            using (var certOnly = new X509Certificate2(cert.RawData))
            {
                store.Open(OpenFlags.ReadWrite);

                // Defensive removal.
                store.Remove(certOnly);
                Assert.False(IsCertInStore(cert, store), "PtxData certificate was found on pre-condition");

                store.Add(cert);
                Assert.True(IsCertInStore(certOnly, store), "PtxData certificate was found after add");

                // Cleanup
                store.Remove(certOnly);
            }
        }

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
                Assert.False(IsCertInStore(cert, store), "PtxData certificate was found on pre-condition");

                store.Add(cert);
                Assert.True(IsCertInStore(certOnly, store), "PtxData certificate was found after add");

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
                Assert.False(IsCertInStore(cert, store), "PtxData certificate was found on pre-condition");

                store.Add(certOnly);
                Assert.True(IsCertInStore(certOnly, store), "PtxData certificate was found after add");
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
                Assert.False(IsCertInStore(cert, store), "PtxData certificate was found on pre-condition");

                // Add the private key
                store.Add(cert);
                Assert.True(IsCertInStore(certOnly, store), "PtxData certificate was found after add");
                Assert.True(StoreHasPrivateKey(store, certOnly), "Store has a private key for PfxData after PFX add");

                // Add the public key with no private key
                store.Add(certOnly);
                Assert.True(StoreHasPrivateKey(store, certOnly), "Store has a private key for PfxData after public-only add");

                // Cleanup
                store.Remove(certOnly);
            }
        }

        [ConditionalFact(nameof(PermissionsAllowStoreWrite))]
        public static void VerifyRemove()
        {
            using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            using (var cert = new X509Certificate2(TestData.PfxData, TestData.PfxDataPassword, X509KeyStorageFlags.Exportable))
            {
                store.Open(OpenFlags.ReadWrite);

                // Defensive removal.  Sort of circular, but it's the best we can do.
                store.Remove(cert);
                Assert.False(IsCertInStore(cert, store), "PtxData certificate was found on pre-condition");

                store.Add(cert);
                Assert.True(IsCertInStore(cert, store), "PtxData certificate was found after add");

                store.Remove(cert);
                Assert.False(IsCertInStore(cert, store), "PtxData certificate was found after remove");
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
                Assert.False(IsCertInStore(cert, store), "PtxData certificate was found on pre-condition");

                // Add the private key
                store.Add(cert);
                Assert.True(IsCertInStore(cert, store), "PtxData certificate was found after add");

                store.Remove(certOnly);
                Assert.False(IsCertInStore(cert, store), "PtxData certificate was found after remove");

                // Add back the public key only
                store.Add(certOnly);
                Assert.True(IsCertInStore(cert, store), "PtxData certificate was found after public-only add");
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

            Assert.True(false, $"Certificate ({forCert.Subject}) exists in the store");
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
