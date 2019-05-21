// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    /// <summary>
    /// Tests that apply to the filesystem/cache portions of the X509 infrastructure on Unix implementations.
    /// </summary>
    [Collection("X509Filesystem")]
    public static class X509FilesystemTests
    {
        private static bool RunManualTests { get; } = TestEnvironmentConfiguration.RunManualTests;

        [OuterLoop]
        // This test is a bit too flaky to be on in the normal run, even for OuterLoop.
        // It can fail due to networking problems, and due to the filesystem interactions it doesn't
        // have strong isolation from other tests (even in different processes).
        [ConditionalFact(nameof(RunManualTests))]
        public static void VerifyCrlCache()
        {
            string crlDirectory = PersistedFiles.GetUserFeatureDirectory("cryptography", "crls");
            string crlFile = Path.Combine(crlDirectory,MicrosoftDotComRootCrlFilename);

            Directory.CreateDirectory(crlDirectory);
            File.Delete(crlFile);

            using (var microsoftDotComIssuer = new X509Certificate2(TestData.MicrosoftDotComIssuerBytes))
            using (var microsoftDotComRoot = new X509Certificate2(TestData.MicrosoftDotComRootBytes))
            using (var unrelated = new X509Certificate2(TestData.DssCer))
            using (var chainHolder = new ChainHolder())
            {
                X509Chain chain = chainHolder.Chain;

                chain.ChainPolicy.ExtraStore.Add(unrelated);
                chain.ChainPolicy.ExtraStore.Add(microsoftDotComRoot);
                
                // The very start of the CRL period.
                chain.ChainPolicy.VerificationTime = new DateTime(2015, 6, 17, 0, 0, 0, DateTimeKind.Utc);
                chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EndCertificateOnly;
                chain.ChainPolicy.VerificationFlags |= X509VerificationFlags.AllowUnknownCertificateAuthority;

                bool valid = chain.Build(microsoftDotComIssuer);
                Assert.True(valid, "Precondition: Chain builds with no revocation checks");

                int initialErrorCount = chain.ChainStatus.Length;
                Assert.InRange(initialErrorCount, 0, 1);

                if (initialErrorCount > 0)
                {
                    Assert.Equal(X509ChainStatusFlags.UntrustedRoot, chain.ChainStatus[0].Status);
                }

                chainHolder.DisposeChainElements();

                chain.ChainPolicy.RevocationMode = X509RevocationMode.Offline;

                valid = chain.Build(microsoftDotComIssuer);
                Assert.False(valid, "Chain should not build validly");

                Assert.Equal(initialErrorCount + 1, chain.ChainStatus.Length);
                Assert.Equal(X509ChainStatusFlags.RevocationStatusUnknown, chain.ChainStatus[0].Status);

                File.WriteAllText(crlFile, MicrosoftDotComRootCrlPem, Encoding.ASCII);

                chainHolder.DisposeChainElements();

                valid = chain.Build(microsoftDotComIssuer);
                Assert.True(valid, "Chain should build validly now");
                Assert.Equal(initialErrorCount, chain.ChainStatus.Length);
            }
        }

        [Fact]
        public static void X509Store_OpenExisting_Fails()
        {
            RunX509StoreTest(
                (store, storeDirectory) =>
                {
                    // Since the directory was explicitly deleted already, this should fail.
                    Assert.Throws<CryptographicException>(
                        () => store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly));
                });
        }

        [Fact]
        private static void X509Store_AddReadOnly()
        {
            RunX509StoreTest(
                (store, storeDirectory) =>
                {
                    using (var cert = new X509Certificate2(TestData.MsCertificate))
                    {
                        store.Open(OpenFlags.ReadOnly);

                        // Adding a certificate when the store is ReadOnly should fail:
                        Assert.Throws<CryptographicException>(() => store.Add(cert));

                        // Since we haven't done anything yet, we shouldn't have polluted the hard drive.
                        Assert.False(Directory.Exists(storeDirectory), "Directory.Exists(storeDirectory)");
                    }
                });
        }

        [Fact]
        private static void X509Store_AddClosed()
        {
            RunX509StoreTest(
                (store, storeDirectory) =>
                {
                    using (var cert = new X509Certificate2(TestData.MsCertificate))
                    {
                        // Adding a certificate when the store is closed should fail:
                        Assert.Throws<CryptographicException>(() => store.Add(cert));

                        // Since we haven't done anything yet, we shouldn't have polluted the hard drive.
                        Assert.False(Directory.Exists(storeDirectory), "Directory.Exists(storeDirectory)");
                    }
                });
        }

        [Fact]
        [OuterLoop(/* Alters user/machine state */)]
        private static void X509Store_AddOne()
        {
            RunX509StoreTest(
                (store, storeDirectory) =>
                {
                    using (var cert = new X509Certificate2(TestData.MsCertificate))
                    {
                        store.Open(OpenFlags.ReadWrite);

                        store.Add(cert);
                        Assert.True(Directory.Exists(storeDirectory), "Directory.Exists(storeDirectory)");
                        Assert.Equal(1, Directory.GetFiles(storeDirectory).Length);

                        using (var coll = new ImportedCollection(store.Certificates))
                        {
                            X509Certificate2Collection storeCerts = coll.Collection;

                            Assert.Equal(1, storeCerts.Count);

                            using (X509Certificate2 storeCert = storeCerts[0])
                            {
                                Assert.Equal(cert, storeCert);
                                Assert.NotSame(cert, storeCert);
                            }
                        }
                    }
                });
        }

        [Fact]
        [OuterLoop(/* Alters user/machine state */)]
        private static void X509Store_AddOneAfterUpgrade()
        {
            RunX509StoreTest(
                (store, storeDirectory) =>
                {
                    using (var cert = new X509Certificate2(TestData.MsCertificate))
                    {
                        store.Open(OpenFlags.ReadOnly);

                        // Adding a certificate when the store is ReadOnly should fail:
                        Assert.Throws<CryptographicException>(() => store.Add(cert));

                        // Since we haven't done anything yet, we shouldn't have polluted the hard drive.
                        Assert.False(Directory.Exists(storeDirectory), "Directory.Exists(storeDirectory)");

                        // Calling Open on an open store changes the access rights:
                        store.Open(OpenFlags.ReadWrite);

                        store.Add(cert);
                        Assert.True(Directory.Exists(storeDirectory), "Directory.Exists(storeDirectory)");
                        Assert.Equal(1, Directory.GetFiles(storeDirectory).Length);

                        using (var coll = new ImportedCollection(store.Certificates))
                        {
                            X509Certificate2Collection storeCerts = coll.Collection;

                            Assert.Equal(1, storeCerts.Count);

                            using (X509Certificate2 storeCert = storeCerts[0])
                            {
                                Assert.Equal(cert, storeCert);
                                Assert.NotSame(cert, storeCert);
                            }
                        }
                    }
                });
        }

        [Fact]
        [OuterLoop(/* Alters user/machine state */)]
        private static void X509Store_DowngradePermissions()
        {
            RunX509StoreTest(
                (store, storeDirectory) =>
                {
                    using (var certA = new X509Certificate2(TestData.MsCertificate))
                    using (var certB = new X509Certificate2(TestData.DssCer))
                    {
                        store.Open(OpenFlags.ReadWrite);
                        
                        // Ensure that ReadWrite took effect.
                        store.Add(certA);

                        store.Open(OpenFlags.ReadOnly);

                        // Adding a certificate when the store is ReadOnly should fail:
                        Assert.Throws<CryptographicException>(() => store.Add(certB));
                    }
                });
        }

        [Fact]
        [OuterLoop(/* Alters user/machine state */)]
        private static void X509Store_AddAfterDispose()
        {
            RunX509StoreTest(
                (store, storeDirectory) =>
                {
                    using (var certA = new X509Certificate2(TestData.MsCertificate))
                    using (var certB = new X509Certificate2(TestData.DssCer))
                    {
                        store.Open(OpenFlags.ReadWrite);

                        store.Add(certA);

                        // Dispose returns the store to the pre-opened state.
                        store.Dispose();

                        // Adding a certificate when the store is closed should fail:
                        Assert.Throws<CryptographicException>(() => store.Add(certB));
                    }
                });
        }

        [Fact]
        [OuterLoop(/* Alters user/machine state */)]
        private static void X509Store_AddAndClear()
        {
            RunX509StoreTest(
                (store, storeDirectory) =>
                {
                    using (var cert = new X509Certificate2(TestData.MsCertificate))
                    {
                        store.Open(OpenFlags.ReadWrite);

                        store.Add(cert);
                        Assert.True(Directory.Exists(storeDirectory), "Directory.Exists(storeDirectory)");
                        Assert.Equal(1, Directory.GetFiles(storeDirectory).Length);

                        store.Remove(cert);

                        // The directory should still exist.
                        Assert.True(Directory.Exists(storeDirectory), "Store Directory Still Exists");
                        Assert.Equal(0, Directory.GetFiles(storeDirectory).Length);
                    }
                });
        }

        [Fact]
        [OuterLoop(/* Alters user/machine state */)]
        private static void X509Store_AddDuplicate()
        {
            RunX509StoreTest(
                (store, storeDirectory) =>
                {
                    using (var cert = new X509Certificate2(TestData.MsCertificate))
                    using (var certClone = new X509Certificate2(cert.RawData))
                    {
                        store.Open(OpenFlags.ReadWrite);

                        store.Add(cert);
                        Assert.True(Directory.Exists(storeDirectory), "Directory.Exists(storeDirectory)");
                        Assert.Equal(1, Directory.GetFiles(storeDirectory).Length);

                        store.Add(certClone);
                        Assert.Equal(1, Directory.GetFiles(storeDirectory).Length);
                    }
                });
        }

        [Fact]
        [OuterLoop(/* Alters user/machine state */)]
        private static void X509Store_AddTwo()
        {
            RunX509StoreTest(
                (store, storeDirectory) =>
                {
                    using (var certA = new X509Certificate2(TestData.MsCertificate))
                    using (var certB = new X509Certificate2(TestData.DssCer))
                    {
                        store.Open(OpenFlags.ReadWrite);

                        store.Add(certA);
                        store.Add(certB);
                        Assert.True(Directory.Exists(storeDirectory), "Directory.Exists(storeDirectory)");
                        Assert.Equal(2, Directory.GetFiles(storeDirectory).Length);

                        X509Certificate2Collection storeCerts = store.Certificates;
                        Assert.Equal(2, storeCerts.Count);

                        X509Certificate2[] expectedCerts = { certA, certB };

                        foreach (X509Certificate2 storeCert in storeCerts)
                        {
                            Assert.Contains(storeCert, expectedCerts);
                            storeCert.Dispose();
                        }
                    }
                });
        }

        [Fact]
        [OuterLoop(/* Alters user/machine state */)]
        private static void X509Store_AddTwo_UpgradePrivateKey()
        {
            RunX509StoreTest(
                (store, storeDirectory) =>
                {
                    using (var certAPrivate = new X509Certificate2(TestData.PfxData, TestData.PfxDataPassword))
                    using (var certAPublic = new X509Certificate2(certAPrivate.RawData))
                    using (var certB = new X509Certificate2(TestData.DssCer))
                    {
                        store.Open(OpenFlags.ReadWrite);

                        store.Add(certAPublic);
                        store.Add(certB);
                        Assert.True(Directory.Exists(storeDirectory), "Directory.Exists(storeDirectory)");

                        string[] storeFiles = Directory.GetFiles(storeDirectory);
                        Assert.Equal(2, storeFiles.Length);

                        X509Certificate2Collection storeCerts = store.Certificates;
                        Assert.Equal(2, storeCerts.Count);

                        X509Certificate2[] expectedCerts = { certAPublic, certB };

                        foreach (X509Certificate2 storeCert in storeCerts)
                        {
                            Assert.False(storeCert.HasPrivateKey, "storeCert.HasPrivateKey (before)");
                            storeCert.Dispose();
                        }

                        store.Add(certAPrivate);
                        // It replaces the existing file, the names should be unaffected.
                        Assert.Equal(storeFiles, Directory.GetFiles(storeDirectory));

                        storeCerts = store.Certificates;
                        Assert.Equal(2, storeCerts.Count);

                        bool foundCertA = false;

                        foreach (X509Certificate2 storeCert in storeCerts)
                        {
                            // The public instance and private instance are .Equal
                            if (storeCert.Equals(certAPublic))
                            {
                                Assert.True(storeCert.HasPrivateKey, "storeCert.HasPrivateKey (affected cert)");
                                foundCertA = true;
                            }
                            else
                            {
                                Assert.False(storeCert.HasPrivateKey, "storeCert.HasPrivateKey (other cert)");
                            }

                            Assert.Contains(storeCert, expectedCerts);
                            storeCert.Dispose();
                        }

                        Assert.True(foundCertA, "foundCertA");
                    }
                });
        }

        [Fact]
        [OuterLoop(/* Alters user/machine state */)]
        private static void X509Store_AddTwo_UpgradePrivateKey_NoDowngrade()
        {
            RunX509StoreTest(
                (store, storeDirectory) =>
                {
                    using (var certAPrivate = new X509Certificate2(TestData.PfxData, TestData.PfxDataPassword))
                    using (var certAPublic = new X509Certificate2(certAPrivate.RawData))
                    using (var certB = new X509Certificate2(TestData.DssCer))
                    {
                        store.Open(OpenFlags.ReadWrite);

                        store.Add(certAPublic);
                        store.Add(certB);
                        Assert.True(Directory.Exists(storeDirectory), "Directory.Exists(storeDirectory)");

                        X509Certificate2Collection storeCerts = store.Certificates;
                        Assert.Equal(2, storeCerts.Count);

                        X509Certificate2[] expectedCerts = { certAPublic, certB };

                        foreach (X509Certificate2 storeCert in storeCerts)
                        {
                            Assert.False(storeCert.HasPrivateKey, "storeCert.HasPrivateKey (before)");
                            Assert.Contains(storeCert, expectedCerts);
                            storeCert.Dispose();
                        }

                        // Add the private (checked in X509Store_AddTwo_UpgradePrivateKey)
                        store.Add(certAPrivate);
                        // Then add the public again, which shouldn't do anything.
                        store.Add(certAPublic);

                        storeCerts = store.Certificates;
                        Assert.Equal(2, storeCerts.Count);

                        bool foundCertA = false;

                        foreach (X509Certificate2 storeCert in storeCerts)
                        {
                            if (storeCert.Equals(certAPublic))
                            {
                                Assert.True(storeCert.HasPrivateKey, "storeCert.HasPrivateKey (affected cert)");
                                foundCertA = true;
                            }
                            else
                            {
                                Assert.False(storeCert.HasPrivateKey, "storeCert.HasPrivateKey (other cert)");
                            }

                            Assert.Contains(storeCert, expectedCerts);
                            storeCert.Dispose();
                        }
                        
                        Assert.True(foundCertA, "foundCertA");
                    }
                });
        }

        [Fact]
        [OuterLoop(/* Alters user/machine state */)]
        private static void X509Store_DistinctCollections()
        {
            RunX509StoreTest(
                (store, storeDirectory) =>
                {
                    using (var certA = new X509Certificate2(TestData.MsCertificate))
                    using (var certB = new X509Certificate2(TestData.DssCer))
                    {
                        store.Open(OpenFlags.ReadWrite);

                        store.Add(certA);
                        store.Add(certB);
                        Assert.True(Directory.Exists(storeDirectory), "Directory.Exists(storeDirectory)");
                        Assert.Equal(2, Directory.GetFiles(storeDirectory).Length);

                        X509Certificate2Collection storeCertsA = store.Certificates;
                        X509Certificate2Collection storeCertsB = store.Certificates;

                        Assert.NotSame(storeCertsA, storeCertsB);
                        Assert.Equal(storeCertsA.Count, storeCertsB.Count);

                        foreach (X509Certificate2 collACert in storeCertsA)
                        {
                            int bIndex = storeCertsB.IndexOf(collACert);
                            Assert.InRange(bIndex, 0, storeCertsB.Count);

                            X509Certificate2 collBCert = storeCertsB[bIndex];
                            // Equal is implied by IndexOf working.
                            Assert.NotSame(collACert, collBCert);

                            storeCertsB.RemoveAt(bIndex);

                            collACert.Dispose();
                            collBCert.Dispose();
                        }
                    }
                });
        }

        [Fact]
        [OuterLoop(/* Alters user/machine state */)]
        private static void X509Store_Add4_Remove1()
        {
            RunX509StoreTest(
                (store, storeDirectory) =>
                {
                    using (var certA = new X509Certificate2(TestData.MsCertificate))
                    using (var certB = new X509Certificate2(TestData.DssCer))
                    using (var certBClone = new X509Certificate2(certB.RawData))
                    using (var certC = new X509Certificate2(TestData.ECDsa256Certificate))
                    using (var certD = new X509Certificate2(TestData.MicrosoftDotComRootBytes))
                    {
                        store.Open(OpenFlags.ReadWrite);
                        
                        store.Add(certA);
                        store.Add(certB);
                        store.Add(certC);
                        store.Add(certD);

                        Assert.True(Directory.Exists(storeDirectory), "Directory.Exists(storeDirectory)");
                        Assert.Equal(4, Directory.GetFiles(storeDirectory).Length);

                        X509Certificate2[] expectedCerts = { certA, certB, certC, certD };
                        X509Certificate2Collection storeCerts = store.Certificates;
                        Assert.Equal(4, storeCerts.Count);

                        foreach (X509Certificate2 storeCert in storeCerts)
                        {
                            Assert.Contains(storeCert, expectedCerts);
                            storeCert.Dispose();
                        }

                        store.Remove(certBClone);
                        Assert.Equal(3, Directory.GetFiles(storeDirectory).Length);

                        expectedCerts = new[] { certA, certC, certD };
                        storeCerts = store.Certificates;
                        Assert.Equal(3, storeCerts.Count);

                        foreach (X509Certificate2 storeCert in storeCerts)
                        {
                            Assert.Contains(storeCert, expectedCerts);
                            storeCert.Dispose();
                        }
                    }
                });
        }

        [Theory]
        [OuterLoop(/* Alters user/machine state */)]
        [InlineData(false)]
        [InlineData(true)]
        private static void X509Store_MultipleObjects(bool matchCase)
        {
            RunX509StoreTest(
                (store, storeDirectory) =>
                {
                    using (var certA = new X509Certificate2(TestData.MsCertificate))
                    using (var certB = new X509Certificate2(TestData.DssCer))
                    using (var certC = new X509Certificate2(TestData.ECDsa256Certificate))
                    using (var certD = new X509Certificate2(TestData.MicrosoftDotComRootBytes))
                    {
                        store.Open(OpenFlags.ReadWrite);

                        store.Add(certA);
                        store.Add(certB);
                        Assert.True(Directory.Exists(storeDirectory), "Directory.Exists(storeDirectory)");

                        string newName = store.Name;

                        if (!matchCase)
                        {
                            newName = newName.ToUpperInvariant();
                            Assert.NotEqual(store.Name, newName);
                        }

                        using (X509Store storeClone = new X509Store(newName, store.Location))
                        {
                            storeClone.Open(OpenFlags.ReadWrite);
                            AssertEqualContents(store, storeClone);

                            store.Add(certC);

                            // The object was added to store, but should show up in both objects
                            // after re-reading the Certificates property
                            AssertEqualContents(store, storeClone);

                            // Now add one to storeClone to prove bidirectionality.
                            storeClone.Add(certD);
                            AssertEqualContents(store, storeClone);
                        }
                    }
                });
        }

        [Fact]
        [OuterLoop( /* Alters user/machine state */)]
        private static void X509Store_FiltersDuplicateOnLoad()
        {
            RunX509StoreTest(
                (store, storeDirectory) =>
                {
                    using (var certA = new X509Certificate2(TestData.MsCertificate))
                    {
                        store.Open(OpenFlags.ReadWrite);

                        store.Add(certA);

                        // Emulate a race condition of parallel adds with the following flow
                        // AdderA: Notice [thumbprint].pfx is available, create it (0 bytes)
                        // AdderB: Notice [thumbprint].pfx already exists, but can't be read, move to [thumbprint].1.pfx
                        // AdderA: finish write
                        // AdderB: finish write

                        string[] files = Directory.GetFiles(storeDirectory, "*.pfx");
                        Assert.Equal(1, files.Length);

                        string srcFile = files[0];
                        string baseName = Path.GetFileNameWithoutExtension(srcFile);
                        string destFile = Path.Combine(storeDirectory, srcFile + ".1.pfx");
                        File.Copy(srcFile, destFile);

                        using (var coll = new ImportedCollection(store.Certificates))
                        {
                            Assert.Equal(1, coll.Collection.Count);
                            Assert.Equal(certA, coll.Collection[0]);
                        }

                        // Also check that remove removes both files.

                        store.Remove(certA);

                        string[] filesAfter = Directory.GetFiles(storeDirectory, "*.pfx");
                        Assert.Equal(0, filesAfter.Length);
                    }
                });
        }

        private static void AssertEqualContents(X509Store storeA, X509Store storeB)
        {
            Assert.NotSame(storeA, storeB);

            using (var storeATracker = new ImportedCollection(storeA.Certificates))
            using (var storeBTracker = new ImportedCollection(storeB.Certificates))
            {
                X509Certificate2Collection storeACerts = storeATracker.Collection;
                X509Certificate2Collection storeBCerts = storeBTracker.Collection;

                Assert.Equal(storeACerts.OfType<X509Certificate2>(), storeBCerts.OfType<X509Certificate2>());
            }
        }

        private static void RunX509StoreTest(Action<X509Store, string> testAction)
        {
            string certStoresFeaturePath = PersistedFiles.GetUserFeatureDirectory("cryptography", "x509stores");
            string storeName = "TestStore" + Guid.NewGuid().ToString("N");
            string storeDirectory = Path.Combine(certStoresFeaturePath, storeName.ToLowerInvariant());

            if (Directory.Exists(storeDirectory))
            {
                Directory.Delete(storeDirectory, true);
            }

            try
            {
                using (X509Store store = new X509Store(storeName, StoreLocation.CurrentUser))
                {
                    testAction(store, storeDirectory);
                }
            }
            finally
            {
                try
                {
                    if (Directory.Exists(storeDirectory))
                    {
                        Directory.Delete(storeDirectory, true);
                    }
                }
                catch
                {
                    // Don't allow any (additional?) I/O errors to propagate.
                }
            }
        }

        // `openssl crl -in [MicrosoftDotComRootCrlPem] -noout -hash`.crl
        private const string MicrosoftDotComRootCrlFilename = "b204d74a.crl";

        // This CRL was downloaded 2015-08-31 20:31 PDT
        // It is valid from Jun 17 00:00:00 2015 GMT to Sep 30 23:59:59 2015 GMT
        private const string MicrosoftDotComRootCrlPem =
            @"-----BEGIN X509 CRL-----
MIICETCB+jANBgkqhkiG9w0BAQUFADCByjELMAkGA1UEBhMCVVMxFzAVBgNVBAoT
DlZlcmlTaWduLCBJbmMuMR8wHQYDVQQLExZWZXJpU2lnbiBUcnVzdCBOZXR3b3Jr
MTowOAYDVQQLEzEoYykgMjAwNiBWZXJpU2lnbiwgSW5jLiAtIEZvciBhdXRob3Jp
emVkIHVzZSBvbmx5MUUwQwYDVQQDEzxWZXJpU2lnbiBDbGFzcyAzIFB1YmxpYyBQ
cmltYXJ5IENlcnRpZmljYXRpb24gQXV0aG9yaXR5IC0gRzUXDTE1MDYxNzAwMDAw
MFoXDTE1MDkzMDIzNTk1OVowDQYJKoZIhvcNAQEFBQADggEBAFxqobObEqKNSAe+
A9cHCYI7sw+Vc8HuE7E+VZc6ni3a2UHiprYuXDsvD18+cyv/nFSLpLqLmExZrsf/
dzH8GH2HgBTt5aO/nX08EBrDgcjHo9b0VI6ZuOOaEeS0NsRh28Jupfn1Xwcsbdw9
nVh1OaExpHwxgg7pJr4pXzaAjbl3b4QfCPyTd5aaOQOEmqvJtRrMwCna4qQ3p4r6
QYe19/pXqK9my7lSmH1vZ0CmNvQeNPmnx+YmFXYTBgap+Xi2cs6GX/qI04CDzjWi
sm6L0+S1Zx2wMhiYOi0JvrRizf+rIyKkDbPMoYEyXZqcCwSnv6mJQY81vmKRKU5N
WKo2mLw=
-----END X509 CRL-----";
    }
}
