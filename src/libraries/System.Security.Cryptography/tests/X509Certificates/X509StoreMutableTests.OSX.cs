// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    [PlatformSpecific(TestPlatforms.OSX)]
    public static partial class X509StoreMutableTests
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
                const int errSecInteractionNotAllowed = -25308;
                const int kPOSIXErrorBase = 100000;

                switch (e.HResult)
                {
                    case errSecInteractionNotAllowed:
                        Console.WriteLine("Run 'security unlock-keychain' to make tests runable.");
                        return false;
                    case kPOSIXErrorBase:
                        // kPOSIXErrorBase is returned for "unknown error from a subsystem",
                        // which seems to happen on writes from SSH sessions even if the keychain
                        // was unlocked.
                        Console.WriteLine("Writing precondition failed with kPOSIXErrorBase, skipping tests.");
                        return false;
                    case errSecWrPerm:
                        Console.WriteLine("Writing precondition failed with permission denied, skipping tests.");
                        return false;
                }

                Console.WriteLine($"Precondition test failed with unknown code {e.HResult}, running anyways.");
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
        public static void CustomStore_ReadWrite()
        {
            using (var store = new X509Store("CustomKeyChain_CoreFX", StoreLocation.CurrentUser))
            using (new TemporaryX509Store(store))
            using (var cert = new X509Certificate2(TestData.PfxData, TestData.PfxDataPassword, X509KeyStorageFlags.Exportable))
            using (var certOnly = new X509Certificate2(cert.RawData))
            {
                store.Open(OpenFlags.ReadWrite);

                // Defensive removal.
                store.Remove(certOnly);
                Assert.False(IsCertInStore(cert, store), "PfxData certificate was found on pre-condition");

                store.Add(cert);
                Assert.True(IsCertInStore(certOnly, store), "PfxData certificate was found after add");

                // Cleanup
                store.Remove(certOnly);
            }
        }

        [ConditionalFact(nameof(PermissionsAllowStoreWrite))]
        public static void CustomStore_ReadOnly()
        {
            using (var store = new X509Store("CustomKeyChain_CoreFX", StoreLocation.CurrentUser))
            using (new TemporaryX509Store(store))
            using (var cert = new X509Certificate2(TestData.PfxData, TestData.PfxDataPassword, X509KeyStorageFlags.Exportable))
            using (var certOnly = new X509Certificate2(cert.RawData))
            {
                store.Open(OpenFlags.ReadOnly);
                Assert.ThrowsAny<CryptographicException>(() => store.Add(certOnly));
            }
        }

        [ConditionalFact(nameof(PermissionsAllowStoreWrite))]
        public static void CustomStore_OpenExistingOnly()
        {
            using (var store = new X509Store("CustomKeyChain_CoreFX_" + Guid.NewGuid().ToString(), StoreLocation.CurrentUser))
            using (new TemporaryX509Store(store))
            {
                Assert.ThrowsAny<CryptographicException>(() => store.Open(OpenFlags.OpenExistingOnly));
            }
        }

        [ConditionalFact(nameof(PermissionsAllowStoreWrite))]
        public static void CustomStore_CaseInsensitive()
        {
            using (var store1 = new X509Store("CustomKeyChain_CoreFX", StoreLocation.CurrentUser))
            using (new TemporaryX509Store(store1))
            using (var store2 = new X509Store("customkeychain_CoreFX", StoreLocation.CurrentUser))
            using (var cert = new X509Certificate2(TestData.PfxData, TestData.PfxDataPassword, X509KeyStorageFlags.Exportable))
            using (var certOnly = new X509Certificate2(cert.RawData))
            {
                store1.Open(OpenFlags.ReadWrite);
                store2.Open(OpenFlags.ReadOnly);

                // Defensive removal.
                store1.Remove(certOnly);
                Assert.False(IsCertInStore(cert, store1), "PfxData certificate was found on pre-condition");

                store1.Add(cert);
                Assert.True(IsCertInStore(certOnly, store1), "PfxData certificate was found after add");
                Assert.True(IsCertInStore(certOnly, store2), "PfxData certificate was found after add (second store)");

                // Cleanup
                store1.Remove(certOnly);
            }
        }

        [ConditionalFact(nameof(PermissionsAllowStoreWrite))]
        public static void CustomStore_InvalidFileName()
        {
            using (var store = new X509Store("../corefx", StoreLocation.CurrentUser))
                Assert.ThrowsAny<CryptographicException>(() => store.Open(OpenFlags.ReadWrite));
        }

        private class TemporaryX509Store : IDisposable
        {
            private X509Store _store;

            public TemporaryX509Store(X509Store store)
            {
                _store = store;
            }

            public void Dispose()
            {
                if (_store.IsOpen)
                    Interop.AppleCrypto.SecKeychainDelete(_store.StoreHandle, throwOnError: false);
            }
        }
    }
}
