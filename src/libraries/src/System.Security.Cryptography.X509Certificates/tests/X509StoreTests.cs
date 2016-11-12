// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    public class X509StoreTests
    {
        [Fact]
        public static void OpenMyStore()
        {
            using (X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.ReadOnly);
            }
        }

#if netstandard17
        [Fact]
        public static void Constructor_DefaultStoreName()
        {
            using (X509Store store = new X509Store(StoreLocation.CurrentUser))
            {
                Assert.Equal("My", store.Name);
            }
        }

        [Fact]
        public static void Constructor_DefaultStoreLocation()
        {
            using (X509Store store = new X509Store(StoreName.My))
            {
                Assert.Equal(StoreLocation.CurrentUser, store.Location);
            }

            using (X509Store store = new X509Store("My"))
            {
                Assert.Equal(StoreLocation.CurrentUser, store.Location);
            }
        }

        [PlatformSpecific(TestPlatforms.Windows)]
        [Fact]
        public static void Constructor_StoreHandle()
        {
            using (X509Store store1 = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                store1.Open(OpenFlags.ReadOnly);
                int certCount1;

                using (var coll = new ImportedCollection(store1.Certificates))
                {
                    certCount1 = coll.Collection.Count;
                    Assert.True(certCount1 >= 0);
                }

                using (X509Store store2 = new X509Store(store1.StoreHandle))
                {
                    using (var coll = new ImportedCollection(store2.Certificates))
                    {
                        int certCount2 = coll.Collection.Count;
                        Assert.Equal(certCount1, certCount2);
                    }
                }
            }
        }

        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [Fact]
        public static void Constructor_StoreHandle_Unix()
        {
            using (X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.ReadOnly);
                Assert.Equal(IntPtr.Zero, store.StoreHandle);
            }

            Assert.Throws<PlatformNotSupportedException>(() => new X509Chain(IntPtr.Zero));
        }

        [PlatformSpecific(TestPlatforms.Windows)]
        [Fact]
        public static void TestDispose()
        {
            X509Store store;
            using (store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.ReadOnly);
                Assert.NotEqual(IntPtr.Zero, store.StoreHandle);
            }

            Assert.Throws<CryptographicException>(() => store.StoreHandle);
        }
#endif

        [Fact]
        public static void ReadMyCertificates()
        {
            using (X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.ReadOnly);

                using (var coll = new ImportedCollection(store.Certificates))
                {
                    int certCount = coll.Collection.Count;

                    // This assert is just so certCount appears to be used, the test really
                    // is that store.get_Certificates didn't throw.
                    Assert.True(certCount >= 0);
                }
            }
        }

        [Fact]
        public static void OpenNotExistant()
        {
            using (X509Store store = new X509Store(Guid.NewGuid().ToString("N"), StoreLocation.CurrentUser))
            {
                Assert.ThrowsAny<CryptographicException>(() => store.Open(OpenFlags.OpenExistingOnly));
            }
        }

        [Fact]
        public static void AddReadOnlyThrows()
        {
            using (X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            using (X509Certificate2 cert = new X509Certificate2(TestData.MsCertificate))
            {
                store.Open(OpenFlags.ReadOnly);

                using (var coll = new ImportedCollection(store.Certificates))
                {
                    // Add only throws when it has to do work.  If, for some reason, this certificate
                    // is already present in the CurrentUser\My store, we can't really test this
                    // functionality.
                    if (!coll.Collection.Contains(cert))
                    {
                        Assert.ThrowsAny<CryptographicException>(() => store.Add(cert));
                    }
                }
            }
        }

        [Fact]
        public static void AddReadOnlyThrowsWhenCertificateExists()
        {
            using (X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.ReadOnly);

                X509Certificate2 toAdd = null;

                // Look through the certificates to find one with no private key to call add on.
                // (The private key restriction is so that in the event of an "accidental success"
                // that no potential permissions would be modified)
                using (var coll = new ImportedCollection(store.Certificates))
                {
                    foreach (X509Certificate2 cert in coll.Collection)
                    {
                        if (!cert.HasPrivateKey)
                        {
                            toAdd = cert;
                            break;
                        }
                    }

                    if (toAdd != null)
                    {
                        Assert.ThrowsAny<CryptographicException>(() => store.Add(toAdd));
                    }
                }
            }
        }

        [Fact]
        public static void RemoveReadOnlyThrowsWhenFound()
        {
            // This test is unfortunate, in that it will mostly never test.
            // In order to do so it would have to open the store ReadWrite, put in a known value,
            // and call Remove on a ReadOnly copy.
            //
            // Just calling Remove on the first item found could also work (when the store isn't empty),
            // but if it fails the cost is too high.
            //
            // So what's the purpose of this test, you ask? To record why we're not unit testing it.
            // And someone could test it manually if they wanted.
            using (X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            using (X509Certificate2 cert = new X509Certificate2(TestData.MsCertificate))
            {
                store.Open(OpenFlags.ReadOnly);

                using (var coll = new ImportedCollection(store.Certificates))
                {
                    if (coll.Collection.Contains(cert))
                    {
                        Assert.ThrowsAny<CryptographicException>(() => store.Remove(cert));
                    }
                }
            }
        }

        /* Placeholder information for these tests until they can be written to run reliably.
         * Currently such tests would create physical files (Unix) and\or certificates (Windows)
         * which can collide with other running tests that use the same cert, or from a
         * test suite running more than once at the same time on the same machine.
         * Ideally, we use a GUID-named store to aoiv collitions with proper cleanup on Unix and Windows
         * and\or have lower testing hooks or use Microsoft Fakes Framework to redirect
         * and encapsulate the actual storage logic so it can be tested, along with mock exceptions
         * to verify exception handling.
         * See issue https://github.com/dotnet/corefx/issues/12833
         * and https://github.com/dotnet/corefx/issues/12223

        [Fact]
        public static void TestAddAndRemove() {}

#if netstandard17
        [Fact]
        public static void TestAddRangeAndRemoveRange() {}
#endif
        */

        [Fact]
        public static void EnumerateClosedIsEmpty()
        {
            using (X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                int count = store.Certificates.Count;
                Assert.Equal(0, count);
            }
        }

        [Fact]
        public static void AddClosedThrows()
        {
            using (X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            using (X509Certificate2 cert = new X509Certificate2(TestData.MsCertificate))
            {
                Assert.ThrowsAny<CryptographicException>(() => store.Add(cert));
            }
        }

        [Fact]
        public static void RemoveClosedThrows()
        {
            using (X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            using (X509Certificate2 cert = new X509Certificate2(TestData.MsCertificate))
            {
                Assert.ThrowsAny<CryptographicException>(() => store.Remove(cert));
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public static void OpenMachineMyStore_Supported()
        {
            using (X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine))
            {
                store.Open(OpenFlags.ReadOnly);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public static void OpenMachineMyStore_NotSupported()
        {
            using (X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine))
            {
                Assert.Throws<PlatformNotSupportedException>(() => store.Open(OpenFlags.ReadOnly));
            }
        }

        [Theory]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [InlineData(OpenFlags.ReadOnly, false)]
        [InlineData(OpenFlags.MaxAllowed, false)]
        [InlineData(OpenFlags.ReadWrite, true)]
        public static void OpenMachineRootStore_Permissions(OpenFlags permissions, bool shouldThrow)
        {
            using (X509Store store = new X509Store(StoreName.Root, StoreLocation.LocalMachine))
            {
                if (shouldThrow)
                {
                    Assert.Throws<PlatformNotSupportedException>(() => store.Open(permissions));
                }
                else
                {
                    // Assert.DoesNotThrow
                    store.Open(permissions);
                }
            }
        }

        [Fact]
        public static void MachineRootStore_NonEmpty()
        {
            // This test will fail on systems where the administrator has gone out of their
            // way to prune the trusted CA list down below this threshold.
            //
            // As of 2016-01-25, Ubuntu 14.04 has 169, and CentOS 7.1 has 175, so that'd be
            // quite a lot of pruning.
            //
            // And as of 2016-01-29 we understand the Homebrew-installed root store, with 180.
            const int MinimumThreshold = 5;

            using (X509Store store = new X509Store(StoreName.Root, StoreLocation.LocalMachine))
            {
                store.Open(OpenFlags.ReadOnly);

                using (var storeCerts = new ImportedCollection(store.Certificates))
                {
                    int certCount = storeCerts.Collection.Count;
                    Assert.InRange(certCount, MinimumThreshold, int.MaxValue);
                }
            }
        }
    }
}
