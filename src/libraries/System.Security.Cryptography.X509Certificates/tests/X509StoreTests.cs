// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NETCOREAPP
#define HAVE_STORE_ISOPEN
#endif

using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.DotNet.RemoteExecutor;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    public class X509StoreTests : FileCleanupTestBase
    {
        [Fact]
        public static void OpenMyStore()
        {
            using (X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.ReadOnly);
                Assert.Equal("My", store.Name);
            }
        }

        [Fact]
        public static void Constructor_DefaultStoreName()
        {
            using (X509Store store = new X509Store(StoreLocation.CurrentUser))
            {
                Assert.Equal("MY", store.Name);
            }
        }

#if HAVE_STORE_ISOPEN
        [Fact]
        public static void Constructor_IsNotOpen()
        {
            using (X509Store store = new X509Store(StoreLocation.CurrentUser))
            {
                Assert.False(store.IsOpen);
            }
        }
#endif

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

        [PlatformSpecific(TestPlatforms.Windows | TestPlatforms.OSX)] // Not supported via OpenSSL
        [Fact]
        public static void Constructor_StoreHandle()
        {
            using (X509Store store1 = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                store1.Open(OpenFlags.ReadOnly);
                bool hadCerts;

                using (var coll = new ImportedCollection(store1.Certificates))
                {
                    // Use >1 instead of >0 in case the one is an ephemeral accident.
                    hadCerts = coll.Collection.Count > 1;
                    Assert.True(coll.Collection.Count >= 0);
                }

                using (X509Store store2 = new X509Store(store1.StoreHandle))
                {
                    using (var coll = new ImportedCollection(store2.Certificates))
                    {
                        if (hadCerts)
                        {
                            // Use InRange here instead of True >= 0 so that the error message
                            // is different, and we can diagnose a bit of what state we might have been in.
                            Assert.InRange(coll.Collection.Count, 1, int.MaxValue);
                        }
                        else
                        {
                            Assert.True(coll.Collection.Count >= 0);
                        }
                    }
                }
            }
        }

        [PlatformSpecific(PlatformSupport.OpenSSL)] // API not supported via OpenSSL
        [Fact]
        public static void Constructor_StoreHandle_OpenSSL()
        {
            using (X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.ReadOnly);
                Assert.Equal(IntPtr.Zero, store.StoreHandle);
            }

            Assert.Throws<PlatformNotSupportedException>(() => new X509Chain(IntPtr.Zero));
        }

#if HAVE_STORE_ISOPEN
        [Fact]
        public static void Constructor_OpenFlags()
        {
            using (X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser, OpenFlags.ReadOnly))
            {
                Assert.True(store.IsOpen);
            }
        }

        [Fact]
        public static void Constructor_OpenFlags_StoreName()
        {
            using (X509Store store = new X509Store("My", StoreLocation.CurrentUser, OpenFlags.ReadOnly))
            {
                Assert.True(store.IsOpen);
            }
        }

        [Fact]
        public static void Constructor_OpenFlags_OpenAnyway()
        {
            using (X509Store store = new X509Store("My", StoreLocation.CurrentUser, OpenFlags.ReadOnly))
            {
                store.Open(OpenFlags.ReadOnly);
                Assert.True(store.IsOpen);
            }
        }

        [Fact]
        public static void Constructor_OpenFlags_NonExistingStoreName_Throws()
        {
            Assert.ThrowsAny<CryptographicException>(() =>
                new X509Store(new Guid().ToString("D"), StoreLocation.CurrentUser, OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly)
            );
        }
#endif

        [PlatformSpecific(TestPlatforms.Windows | TestPlatforms.OSX)] // StoreHandle not supported via OpenSSL
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
        public static void OpenNotExistent()
        {
            using (X509Store store = new X509Store(Guid.NewGuid().ToString("N"), StoreLocation.CurrentUser))
            {
                Assert.ThrowsAny<CryptographicException>(() => store.Open(OpenFlags.OpenExistingOnly));
            }
        }

#if HAVE_STORE_ISOPEN
        [Fact]
        public static void Open_IsOpenTrue()
        {
            using (X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.ReadOnly);
                Assert.True(store.IsOpen);
            }
        }

        [Fact]
        public static void Dispose_IsOpenFalse()
        {
            X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);
            store.Dispose();
            Assert.False(store.IsOpen);
        }

        [Fact]
        public static void ReOpen_IsOpenTrue()
        {
            X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);
            store.Close();
            store.Open(OpenFlags.ReadOnly);
            Assert.True(store.IsOpen);
        }
#endif

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
        public static void AddDisposedThrowsCryptographicException()
        {
            using (X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            using (X509Certificate2 cert = new X509Certificate2(TestData.MsCertificate))
            {
                store.Open(OpenFlags.ReadWrite);

                cert.Dispose();
                Assert.Throws<CryptographicException>(() => store.Add(cert));
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

        [Fact]
        public static void RemoveReadOnlyNonExistingDoesNotThrow()
        {
            using (X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            using (X509Certificate2 cert = new X509Certificate2(TestData.MsCertificate))
            {
                store.Open(OpenFlags.ReadOnly);
                store.Remove(cert);
            }
        }

        [Fact]
        public static void RemoveDisposedIsIgnored()
        {
            using (X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            using (X509Certificate2 cert = new X509Certificate2(TestData.MsCertificate))
            {
                store.Open(OpenFlags.ReadWrite);
                cert.Dispose();
                store.Remove(cert);
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
         * See issue https://github.com/dotnet/runtime/issues/19030
         * and https://github.com/dotnet/runtime/issues/18792

        [Fact]
        public static void TestAddAndRemove() {}

        [Fact]
        public static void TestAddRangeAndRemoveRange() {}
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
        [PlatformSpecific(TestPlatforms.Windows | TestPlatforms.OSX)]
        public static void OpenMachineMyStore_Supported()
        {
            using (X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine))
            {
                store.Open(OpenFlags.ReadOnly);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix & ~TestPlatforms.OSX)]
        public static void OpenMachineMyStore_NotSupported()
        {
            using (X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine))
            {
                Exception e = Assert.Throws<CryptographicException>(() => store.Open(OpenFlags.ReadOnly));
                Assert.NotNull(e.InnerException);
                Assert.IsType<PlatformNotSupportedException>(e.InnerException);
            }
        }

        [Theory]
        [PlatformSpecific(TestPlatforms.AnyUnix & ~TestPlatforms.OSX)]
        [SkipOnPlatform(PlatformSupport.MobileAppleCrypto, "Root certificate store is not accessible")]
        [InlineData(OpenFlags.ReadOnly, false)]
        [InlineData(OpenFlags.MaxAllowed, false)]
        [InlineData(OpenFlags.ReadWrite, true)]
        public static void OpenMachineRootStore_Permissions(OpenFlags permissions, bool shouldThrow)
        {
            using (X509Store store = new X509Store(StoreName.Root, StoreLocation.LocalMachine))
            {
                if (shouldThrow)
                {
                    Exception e = Assert.Throws<CryptographicException>(() => store.Open(permissions));
                    Assert.NotNull(e.InnerException);
                    Assert.IsType<PlatformNotSupportedException>(e.InnerException);
                }
                else
                {
                    // Assert.DoesNotThrow
                    store.Open(permissions);
                }
            }
        }

        [Fact]
        [SkipOnPlatform(PlatformSupport.MobileAppleCrypto, "Root certificate store is not accessible")]
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

        [Theory]
        [PlatformSpecific(TestPlatforms.Windows | TestPlatforms.OSX)]
        [InlineData(StoreLocation.CurrentUser, true)]
        [InlineData(StoreLocation.LocalMachine, true)]
        [InlineData(StoreLocation.CurrentUser, false)]
        [InlineData(StoreLocation.LocalMachine, false)]
        public static void EnumerateDisallowedStore(StoreLocation location, bool useEnum)
        {
            X509Store store = useEnum
                ? new X509Store(StoreName.Disallowed, location)
                // Non-normative casing, proving that we aren't case-sensitive (Windows isn't)
                : new X509Store("disallowed", location);

            using (store)
            {
                store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);

                using (var storeCerts = new ImportedCollection(store.Certificates))
                {
                    // That's all.  We enumerated it.
                    // There might not even be data in it.
                }
            }
        }

        [Theory]
        [PlatformSpecific(TestPlatforms.AnyUnix & ~TestPlatforms.OSX)]
        [InlineData(false, OpenFlags.ReadOnly)]
        [InlineData(true, OpenFlags.ReadOnly)]
        [InlineData(false, OpenFlags.ReadWrite)]
        [InlineData(true, OpenFlags.ReadWrite)]
        [InlineData(false, OpenFlags.MaxAllowed)]
        [InlineData(true, OpenFlags.MaxAllowed)]
        public static void UnixCannotOpenMachineDisallowedStore(bool useEnum, OpenFlags openFlags)
        {
            X509Store store = useEnum
                ? new X509Store(StoreName.Disallowed, StoreLocation.LocalMachine)
                // Non-normative casing, proving that we aren't case-sensitive (Windows isn't)
                : new X509Store("disallowed", StoreLocation.LocalMachine);

            using (store)
            {
                Exception e = Assert.Throws<CryptographicException>(() => store.Open(openFlags));
                Assert.NotNull(e.InnerException);
                Assert.IsType<PlatformNotSupportedException>(e.InnerException);
                Assert.Equal(e.Message, e.InnerException.Message);
            }
        }

        [Theory]
        [PlatformSpecific(TestPlatforms.AnyUnix & ~TestPlatforms.OSX)]
        [InlineData(false, OpenFlags.ReadOnly)]
        [InlineData(true, OpenFlags.ReadOnly)]
        [InlineData(false, OpenFlags.ReadWrite)]
        [InlineData(true, OpenFlags.ReadWrite)]
        [InlineData(false, OpenFlags.MaxAllowed)]
        [InlineData(true, OpenFlags.MaxAllowed)]
        public static void UnixCannotModifyDisallowedStore(bool useEnum, OpenFlags openFlags)
        {
            X509Store store = useEnum
                ? new X509Store(StoreName.Disallowed, StoreLocation.CurrentUser)
                // Non-normative casing, proving that we aren't case-sensitive (Windows isn't)
                : new X509Store("disallowed", StoreLocation.CurrentUser);

            using (store)
            using (X509Certificate2 cert = new X509Certificate2(TestData.Rsa384CertificatePemBytes))
            {
                store.Open(openFlags);
                Exception e = Assert.Throws<CryptographicException>(() => store.Add(cert));

                if (openFlags == OpenFlags.ReadOnly)
                {
                    Assert.Null(e.InnerException);
                }
                else
                {
                    Assert.NotNull(e.InnerException);
                    Assert.IsType<PlatformNotSupportedException>(e.InnerException);
                    Assert.Equal(e.Message, e.InnerException.Message);
                }

                Assert.Equal(0, store.Certificates.Count);
            }
        }
#if Unix
        [ConditionalFact(nameof(NotRunningAsRootAndRemoteExecutorSupported))] // root can read '2.pem'
        [PlatformSpecific(TestPlatforms.Linux)] // Windows/OSX doesn't use SSL_CERT_{DIR,FILE}.
        private void X509Store_MachineStoreLoadSkipsInvalidFiles()
        {
            // We create a folder for our machine store and use it by setting SSL_CERT_{DIR,FILE}.
            // In the store we'll add some invalid files, but we start and finish with a valid file.
            // This is to account for the order in which the store is populated.
            string sslCertDir = GetTestFilePath();
            Directory.CreateDirectory(sslCertDir);

            // Valid file.
            File.WriteAllBytes(Path.Combine(sslCertDir, "0.pem"), TestData.SelfSigned1PemBytes);

            // File with invalid content.
            File.WriteAllText(Path.Combine(sslCertDir, "1.pem"), "This is not a valid cert");

            // File which is not readable by the current user.
            string unreadableFileName = Path.Combine(sslCertDir, "2.pem");
            File.WriteAllBytes(unreadableFileName, TestData.SelfSigned2PemBytes);
            Assert.Equal(0, chmod(unreadableFileName, 0));

            // Valid file.
            File.WriteAllBytes(Path.Combine(sslCertDir, "3.pem"), TestData.SelfSigned3PemBytes);

            var psi = new ProcessStartInfo();
            psi.Environment.Add("SSL_CERT_DIR", sslCertDir);
            psi.Environment.Add("SSL_CERT_FILE", "/nonexisting");
            RemoteExecutor.Invoke(() =>
            {
                using (var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine))
                {
                    store.Open(OpenFlags.OpenExistingOnly);

                    // Check nr of certificates in store.
                    Assert.Equal(2, store.Certificates.Count);
                }
            }, new RemoteInvokeOptions { StartInfo = psi }).Dispose();
        }

        [DllImport("libc")]
        private static extern int chmod(string path, int mode);
        [DllImport("libc")]
        private static extern uint geteuid();

        public static bool NotRunningAsRootAndRemoteExecutorSupported => geteuid() != 0 && RemoteExecutor.IsSupported;
#endif
    }
}
