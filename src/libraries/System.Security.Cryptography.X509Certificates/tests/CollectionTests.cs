// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    public static class CollectionTests
    {
        [Fact]
        public static void X509Certificate2CollectionEnumerator()
        {
            using (X509Certificate2 c1 = new X509Certificate2())
            using (X509Certificate2 c2 = new X509Certificate2())
            using (X509Certificate2 c3 = new X509Certificate2())
            {
                X509Certificate2Collection cc = new X509Certificate2Collection(new X509Certificate2[] { c1, c2, c3 });
                X509Certificate2Enumerator e = cc.GetEnumerator();
                object ignored;

                for (int i = 0; i < 2; i++)
                {
                    // Not started
                    Assert.Throws<InvalidOperationException>(() => ignored = e.Current);

                    Assert.True(e.MoveNext());
                    Assert.Same(c1, e.Current);

                    Assert.True(e.MoveNext());
                    Assert.Same(c2, e.Current);

                    Assert.True(e.MoveNext());
                    Assert.Same(c3, e.Current);

                    Assert.False(e.MoveNext());
                    Assert.False(e.MoveNext());
                    Assert.False(e.MoveNext());
                    Assert.False(e.MoveNext());
                    Assert.False(e.MoveNext());

                    // ended.
                    Assert.Throws<InvalidOperationException>(() => ignored = e.Current);

                    e.Reset();
                }
            }
        }

        [Fact]
        public static void X509CertificateCollectionEnumerator()
        {
            using (X509Certificate2 c1 = new X509Certificate2())
            using (X509Certificate2 c2 = new X509Certificate2())
            using (X509Certificate2 c3 = new X509Certificate2())
            {
                X509CertificateCollection cc = new X509CertificateCollection(new X509Certificate[] { c1, c2, c3 });
                X509CertificateCollection.X509CertificateEnumerator e = cc.GetEnumerator();
                object ignored;

                for (int i = 0; i < 2; i++)
                {
                    // Not started
                    Assert.Throws<InvalidOperationException>(() => ignored = e.Current);

                    Assert.True(e.MoveNext());
                    Assert.Same(c1, e.Current);

                    Assert.True(e.MoveNext());
                    Assert.Same(c2, e.Current);

                    Assert.True(e.MoveNext());
                    Assert.Same(c3, e.Current);

                    Assert.False(e.MoveNext());
                    Assert.False(e.MoveNext());
                    Assert.False(e.MoveNext());
                    Assert.False(e.MoveNext());
                    Assert.False(e.MoveNext());

                    // ended.
                    Assert.Throws<InvalidOperationException>(() => ignored = e.Current);

                    e.Reset();
                }
            }
        }

        [Fact]
        public static void X509CertificateCollectionThrowsArgumentNullException()
        {
            using (X509Certificate certificate = new X509Certificate())
            {
                X509CertificateCollection collection = new X509CertificateCollection { certificate };

                Assert.Throws<ArgumentNullException>(() => collection[0] = null);
                Assert.Throws<ArgumentNullException>(() => collection.Add(null));
                Assert.Throws<ArgumentNullException>(() => collection.AddRange((X509Certificate[])null));
                Assert.Throws<ArgumentNullException>(() => collection.AddRange((X509CertificateCollection)null));
                Assert.Throws<ArgumentNullException>(() => collection.CopyTo(null, 0));
                Assert.Throws<ArgumentNullException>(() => collection.Insert(0, null));
                Assert.Throws<ArgumentNullException>(() => collection.Remove(null));

                IList ilist = (IList)collection;
                Assert.Throws<ArgumentNullException>(() => ilist[0] = null);
                Assert.Throws<ArgumentNullException>(() => ilist.Add(null));
                Assert.Throws<ArgumentNullException>(() => ilist.CopyTo(null, 0));
                Assert.Throws<ArgumentNullException>(() => ilist.Insert(0, null));
                Assert.Throws<ArgumentNullException>(() => ilist.Remove(null));
            }

            Assert.Throws<ArgumentNullException>(() => new X509CertificateCollection.X509CertificateEnumerator(null));
        }

        [Fact]
        public static void X509Certificate2CollectionThrowsArgumentNullException()
        {
            using (X509Certificate2 certificate = new X509Certificate2())
            {
                X509Certificate2Collection collection = new X509Certificate2Collection { certificate };

                Assert.Throws<ArgumentNullException>(() => collection[0] = null);
                Assert.Throws<ArgumentNullException>(() => collection.Add((X509Certificate)null));
                Assert.Throws<ArgumentNullException>(() => collection.Add((X509Certificate2)null));
                Assert.Throws<ArgumentNullException>(() => collection.AddRange((X509Certificate[])null));
                Assert.Throws<ArgumentNullException>(() => collection.AddRange((X509CertificateCollection)null));
                Assert.Throws<ArgumentNullException>(() => collection.AddRange((X509Certificate2[])null));
                Assert.Throws<ArgumentNullException>(() => collection.AddRange((X509Certificate2Collection)null));

                // Note: X509CertificateCollection.Contains does not throw, but X509Certificate2Collection.Contains does throw.
                Assert.Throws<ArgumentNullException>(() => collection.Contains((X509Certificate2)null));

                Assert.Throws<ArgumentNullException>(() => collection.CopyTo(null, 0));
                Assert.Throws<ArgumentNullException>(() => collection.Insert(0, (X509Certificate)null));
                Assert.Throws<ArgumentNullException>(() => collection.Insert(0, (X509Certificate2)null));
                Assert.Throws<ArgumentNullException>(() => collection.Remove((X509Certificate)null));
                Assert.Throws<ArgumentNullException>(() => collection.Remove((X509Certificate2)null));
                Assert.Throws<ArgumentNullException>(() => collection.RemoveRange((X509Certificate2[])null));
                Assert.Throws<ArgumentNullException>(() => collection.RemoveRange((X509Certificate2Collection)null));

                Assert.Throws<ArgumentNullException>(() => collection.Import((byte[])null));
                Assert.Throws<ArgumentNullException>(() => collection.Import((string)null));

                IList ilist = (IList)collection;
                Assert.Throws<ArgumentNullException>(() => ilist[0] = null);
                Assert.Throws<ArgumentNullException>(() => ilist.Add(null));
                Assert.Throws<ArgumentNullException>(() => ilist.CopyTo(null, 0));
                Assert.Throws<ArgumentNullException>(() => ilist.Insert(0, null));
                Assert.Throws<ArgumentNullException>(() => ilist.Remove(null));
            }
        }

        [Fact]
        public static void X509CertificateCollectionThrowsArgumentOutOfRangeException()
        {
            using (X509Certificate certificate = new X509Certificate())
            {
                X509CertificateCollection collection = new X509CertificateCollection { certificate };

                Assert.Throws<ArgumentOutOfRangeException>(() => collection[-1]);
                Assert.Throws<ArgumentOutOfRangeException>(() => collection[collection.Count]);
                Assert.Throws<ArgumentOutOfRangeException>(() => collection[-1] = certificate);
                Assert.Throws<ArgumentOutOfRangeException>(() => collection[collection.Count] = certificate);
                Assert.Throws<ArgumentOutOfRangeException>(() => collection.Insert(-1, certificate));
                Assert.Throws<ArgumentOutOfRangeException>(() => collection.Insert(collection.Count + 1, certificate));
                Assert.Throws<ArgumentOutOfRangeException>(() => collection.RemoveAt(-1));
                Assert.Throws<ArgumentOutOfRangeException>(() => collection.RemoveAt(collection.Count));

                IList ilist = (IList)collection;
                Assert.Throws<ArgumentOutOfRangeException>(() => ilist[-1]);
                Assert.Throws<ArgumentOutOfRangeException>(() => ilist[collection.Count]);
                Assert.Throws<ArgumentOutOfRangeException>(() => ilist[-1] = certificate);
                Assert.Throws<ArgumentOutOfRangeException>(() => ilist[collection.Count] = certificate);
                Assert.Throws<ArgumentOutOfRangeException>(() => ilist.Insert(-1, certificate));
                Assert.Throws<ArgumentOutOfRangeException>(() => ilist.Insert(collection.Count + 1, certificate));
                Assert.Throws<ArgumentOutOfRangeException>(() => ilist.RemoveAt(-1));
                Assert.Throws<ArgumentOutOfRangeException>(() => ilist.RemoveAt(collection.Count));
            }
        }

        [Fact]
        public static void X509Certificate2CollectionThrowsArgumentOutOfRangeException()
        {
            using (X509Certificate2 certificate = new X509Certificate2())
            {
                X509Certificate2Collection collection = new X509Certificate2Collection { certificate };

                Assert.Throws<ArgumentOutOfRangeException>(() => collection[-1]);
                Assert.Throws<ArgumentOutOfRangeException>(() => collection[collection.Count]);
                Assert.Throws<ArgumentOutOfRangeException>(() => collection[-1] = certificate);
                Assert.Throws<ArgumentOutOfRangeException>(() => collection[collection.Count] = certificate);
                Assert.Throws<ArgumentOutOfRangeException>(() => collection.Insert(-1, certificate));
                Assert.Throws<ArgumentOutOfRangeException>(() => collection.Insert(collection.Count + 1, certificate));
                Assert.Throws<ArgumentOutOfRangeException>(() => collection.RemoveAt(-1));
                Assert.Throws<ArgumentOutOfRangeException>(() => collection.RemoveAt(collection.Count));

                IList ilist = (IList)collection;
                Assert.Throws<ArgumentOutOfRangeException>(() => ilist[-1]);
                Assert.Throws<ArgumentOutOfRangeException>(() => ilist[collection.Count]);
                Assert.Throws<ArgumentOutOfRangeException>(() => ilist[-1] = certificate);
                Assert.Throws<ArgumentOutOfRangeException>(() => ilist[collection.Count] = certificate);
                Assert.Throws<ArgumentOutOfRangeException>(() => ilist.Insert(-1, certificate));
                Assert.Throws<ArgumentOutOfRangeException>(() => ilist.Insert(collection.Count + 1, certificate));
                Assert.Throws<ArgumentOutOfRangeException>(() => ilist.RemoveAt(-1));
                Assert.Throws<ArgumentOutOfRangeException>(() => ilist.RemoveAt(collection.Count));
            }
        }

        [Fact]
        public static void X509CertificateCollectionEnumeratorModification()
        {
            using (X509Certificate c1 = new X509Certificate())
            using (X509Certificate c2 = new X509Certificate())
            using (X509Certificate c3 = new X509Certificate())
            {
                X509CertificateCollection cc = new X509CertificateCollection(new X509Certificate[] { c1, c2, c3 });
                X509CertificateCollection.X509CertificateEnumerator e = cc.GetEnumerator();

                cc.Add(c1);

                // Collection changed.
                Assert.Throws<InvalidOperationException>(() => e.MoveNext());
                Assert.Throws<InvalidOperationException>(() => e.Reset());
            }
        }

        [Fact]
        public static void X509Certificate2CollectionEnumeratorModification()
        {
            using (X509Certificate2 c1 = new X509Certificate2())
            using (X509Certificate2 c2 = new X509Certificate2())
            using (X509Certificate2 c3 = new X509Certificate2())
            {
                X509Certificate2Collection cc = new X509Certificate2Collection(new X509Certificate2[] { c1, c2, c3 });
                X509Certificate2Enumerator e = cc.GetEnumerator();

                cc.Add(c1);

                // Collection changed.
                Assert.Throws<InvalidOperationException>(() => e.MoveNext());
                Assert.Throws<InvalidOperationException>(() => e.Reset());
            }
        }

        [Fact]
        public static void X509CertificateCollectionAdd()
        {
            using (X509Certificate2 c1 = new X509Certificate2())
            using (X509Certificate2 c2 = new X509Certificate2())
            {
                X509CertificateCollection cc = new X509CertificateCollection();
                int idx = cc.Add(c1);
                Assert.Equal(0, idx);

                idx = cc.Add(c2);
                Assert.Equal(1, idx);

                Assert.Throws<ArgumentNullException>(() => cc.Add(null));
            }
        }

        [Fact]
        public static void X509CertificateCollectionAsIList()
        {
            using (X509Certificate2 c1 = new X509Certificate2())
            using (X509Certificate2 c2 = new X509Certificate2())
            {
                X509CertificateCollection cc = new X509CertificateCollection();
                cc.Add(c1);
                cc.Add(c2);

                IList il = cc;
                Assert.Throws<ArgumentNullException>(() => il[0] = null);

                string bogus = "Bogus";
                Assert.Throws<ArgumentException>(() => il[0] = bogus);
                Assert.Throws<ArgumentException>(() => il.Add(bogus));
                Assert.Throws<ArgumentException>(() => il.Insert(0, bogus));
            }
        }

        [Fact]
        public static void AddDoesNotClone()
        {
            using (X509Certificate2 c1 = new X509Certificate2())
            {
                X509Certificate2Collection coll = new X509Certificate2Collection();
                coll.Add(c1);

                Assert.Same(c1, coll[0]);
            }
        }

        [Fact]
        public static void ImportNull()
        {
            X509Certificate2Collection cc2 = new X509Certificate2Collection();
            Assert.Throws<ArgumentNullException>(() => cc2.Import((byte[])null));
            Assert.Throws<ArgumentNullException>(() => cc2.Import((String)null));
        }

        [Fact]
        public static void ImportPfx()
        {
            using (var pfxCer = new X509Certificate2(TestData.PfxData, TestData.PfxDataPassword))
            {
                X509Certificate2Collection cc2 = new X509Certificate2Collection();
                cc2.Import(TestData.PfxData, TestData.PfxDataPassword, X509KeyStorageFlags.DefaultKeySet);
                int count = cc2.Count;
                Assert.Equal(1, count);

                using (X509Certificate2 c = cc2[0])
                {
                    // pfxCer was loaded directly, cc2[0] was Imported, two distinct copies.
                    Assert.NotSame(pfxCer, c);

                    Assert.Equal(pfxCer, c);
                    Assert.Equal(pfxCer.Thumbprint, c.Thumbprint);
                }
            }
        }

        [Fact]
        public static void ImportFullChainPfx()
        {
            X509Certificate2Collection certs = new X509Certificate2Collection();
            certs.Import(Path.Combine("TestData", "test.pfx"), "test", X509KeyStorageFlags.DefaultKeySet);
            int count = certs.Count;
            Assert.Equal(3, count);

            // Verify that the read ordering is consistent across the platforms
            string[] expectedSubjects =
            {
                "MS Passport Test Sub CA",
                "MS Passport Test Root CA",
                "test.local",
            };

            string[] actualSubjects = certs.OfType<X509Certificate2>().
                Select(cert => cert.GetNameInfo(X509NameType.SimpleName, false)).
                ToArray();

            Assert.Equal(expectedSubjects, actualSubjects);
            
            // And verify that we have private keys when we expect them
            bool[] expectedHasPrivateKeys =
            {
                false,
                false,
                true,
            };

            bool[] actualHasPrivateKeys = certs.OfType<X509Certificate2>().
                Select(cert => cert.HasPrivateKey).
                ToArray();

            Assert.Equal(expectedHasPrivateKeys, actualHasPrivateKeys);
        }

        [Fact]
        [ActiveIssue(1993, PlatformID.AnyUnix)]
        public static void ImportStoreSavedAsCerData()
        {
            using (var pfxCer = new X509Certificate2(TestData.PfxData, TestData.PfxDataPassword))
            {
                X509Certificate2Collection cc2 = new X509Certificate2Collection();
                cc2.Import(TestData.StoreSavedAsCerData);
                int count = cc2.Count;
                Assert.Equal(1, count);

                using (X509Certificate2 c = cc2[0])
                {
                    // pfxCer was loaded directly, cc2[0] was Imported, two distinct copies.
                    Assert.NotSame(pfxCer, c);

                    Assert.Equal(pfxCer, c);
                    Assert.Equal(pfxCer.Thumbprint, c.Thumbprint);
                }
            }
        }

        [Fact]
        [ActiveIssue(1993, PlatformID.AnyUnix)]
        public static void ImportStoreSavedAsSerializedCerData()
        {
            using (var pfxCer = new X509Certificate2(TestData.PfxData, TestData.PfxDataPassword))
            {
                X509Certificate2Collection cc2 = new X509Certificate2Collection();
                cc2.Import(TestData.StoreSavedAsSerializedCerData);
                int count = cc2.Count;
                Assert.Equal(1, count);

                using (X509Certificate2 c = cc2[0])
                {
                    // pfxCer was loaded directly, cc2[0] was Imported, two distinct copies.
                    Assert.NotSame(pfxCer, c);

                    Assert.Equal(pfxCer, c);
                    Assert.Equal(pfxCer.Thumbprint, c.Thumbprint);
                }
            }
        }

        [Fact]
        [ActiveIssue(1993, PlatformID.AnyUnix)]
        public static void ImportStoreSavedAsSerializedStoreData()
        {
            using (var msCer = new X509Certificate2(TestData.MsCertificate))
            using (var pfxCer = new X509Certificate2(TestData.PfxData, TestData.PfxDataPassword))
            {

                X509Certificate2Collection cc2 = new X509Certificate2Collection();
                cc2.Import(TestData.StoreSavedAsSerializedStoreData);
                int count = cc2.Count;
                Assert.Equal(2, count);

                X509Certificate2[] cs = cc2.ToArray().OrderBy(c => c.Subject).ToArray();

                Assert.NotSame(msCer, cs[0]);
                Assert.Equal(msCer, cs[0]);
                Assert.Equal(msCer.Thumbprint, cs[0].Thumbprint);

                Assert.NotSame(pfxCer, cs[1]);
                Assert.Equal(pfxCer, cs[1]);
                Assert.Equal(pfxCer.Thumbprint, cs[1].Thumbprint);
            }
        }

        [Fact]
        [ActiveIssue(1993, PlatformID.AnyUnix)]
        public static void ImportStoreSavedAsPfxData()
        {
            using (var msCer = new X509Certificate2(TestData.MsCertificate))
            using (var pfxCer = new X509Certificate2(TestData.PfxData, TestData.PfxDataPassword))
            {
                X509Certificate2Collection cc2 = new X509Certificate2Collection();
                cc2.Import(TestData.StoreSavedAsPfxData);
                int count = cc2.Count;
                Assert.Equal(2, count);

                X509Certificate2[] cs = cc2.ToArray().OrderBy(c => c.Subject).ToArray();
                Assert.NotSame(msCer, cs[0]);
                Assert.Equal(msCer, cs[0]);
                Assert.Equal(msCer.Thumbprint, cs[0].Thumbprint);

                Assert.NotSame(pfxCer, cs[1]);
                Assert.Equal(pfxCer, cs[1]);
                Assert.Equal(pfxCer.Thumbprint, cs[1].Thumbprint);
            }
        }

        [Fact]
        public static void ImportFromFileTests()
        {
            using (var pfxCer = new X509Certificate2(TestData.PfxData, TestData.PfxDataPassword))
            {
                X509Certificate2Collection cc2 = new X509Certificate2Collection();
                cc2.Import(Path.Combine("TestData" ,"My.pfx"), TestData.PfxDataPassword, X509KeyStorageFlags.DefaultKeySet);
                int count = cc2.Count;
                Assert.Equal(1, count);

                using (X509Certificate2 c = cc2[0])
                {
                    // pfxCer was loaded directly, cc2[0] was Imported, two distinct copies.
                    Assert.NotSame(pfxCer, c);

                    Assert.Equal(pfxCer, c);
                    Assert.Equal(pfxCer.Thumbprint, c.Thumbprint);
                }
            }
        }

        [Fact]
        [ActiveIssue(2745, PlatformID.AnyUnix)]
        public static void ImportMultiplePrivateKeysPfx()
        {
            var collection = new X509Certificate2Collection();
            collection.Import(TestData.MultiPrivateKeyPfx);

            Assert.Equal(2, collection.Count);

            foreach (X509Certificate2 cert in collection)
            {
                Assert.True(cert.HasPrivateKey, "cert.HasPrivateKey");
            }
        }

        [Fact]
        [ActiveIssue(1993, PlatformID.AnyUnix)]
        public static void ExportCert()
        {
            TestExportSingleCert(X509ContentType.Cert);
        }

        [Fact]
        [ActiveIssue(1993, PlatformID.AnyUnix)]
        public static void ExportSerializedCert()
        {
            TestExportSingleCert(X509ContentType.SerializedCert);
        }

        [Fact]
        [ActiveIssue(1993, PlatformID.AnyUnix)]
        public static void ExportSerializedStore()
        {
            TestExportStore(X509ContentType.SerializedStore);
        }

        [Fact]
        [ActiveIssue(1993, PlatformID.AnyUnix)]
        public static void ExportPkcs7()
        {
            TestExportStore(X509ContentType.Pkcs7);
        }

        [Fact]
        public static void X509CertificateCollectionSyncRoot()
        {
            var cc = new X509CertificateCollection();
            Assert.NotNull(((ICollection)cc).SyncRoot);
            Assert.Same(((ICollection)cc).SyncRoot, ((ICollection)cc).SyncRoot);
        }

        [Fact]
        public static void ExportEmpty_Cert()
        {
            var collection = new X509Certificate2Collection();
            byte[] exported = collection.Export(X509ContentType.Cert);

            Assert.Null(exported);
        }

        [Fact]
        [ActiveIssue(2746, PlatformID.AnyUnix)]
        public static void ExportEmpty_Pkcs12()
        {
            var collection = new X509Certificate2Collection();
            byte[] exported = collection.Export(X509ContentType.Pkcs12);

            // The empty PFX is legal, the answer won't be null.
            Assert.NotNull(exported);
        }

        [Fact]
        public static void ImportEmpty_Pkcs12()
        {
            var collection = new X509Certificate2Collection();

            collection.Import(TestData.EmptyPfx);

            Assert.Equal(0, collection.Count);
        }

        [Fact]
        public static void ExportUnrelatedPfx()
        {
            // Export multiple certificates which are not part of any kind of certificate chain.
            // Nothing in the PKCS12 structure requires they're related, but it might be an underlying
            // assumption of the provider.
            using (var cert1 = new X509Certificate2(TestData.MsCertificate))
            using (var cert2 = new X509Certificate2(TestData.ComplexNameInfoCert))
            using (var cert3 = new X509Certificate2(TestData.CertWithPolicies))
            {
                var collection = new X509Certificate2Collection
                {
                    cert1,
                    cert2,
                    cert3,
                };

                byte[] exported = collection.Export(X509ContentType.Pkcs12);

                var importedCollection = new X509Certificate2Collection();
                importedCollection.Import(exported);

                // Verify that the two collections contain the same certificates,
                // but the order isn't really a factor.
                Assert.Equal(collection.Count, importedCollection.Count);

                // Compare just the subject names first, because it's the easiest thing to read out of the failure message.
                string[] subjects = new string[collection.Count];
                string[] importedSubjects = new string[collection.Count];

                for (int i = 0; i < collection.Count; i++)
                {
                    subjects[i] = collection[i].GetNameInfo(X509NameType.SimpleName, false);
                    importedSubjects[i] = importedCollection[i].GetNameInfo(X509NameType.SimpleName, false);
                }

                Assert.Equal(subjects, importedSubjects);

                // But, really, the collections should be equivalent
                // (after being coerced to IEnumerable<X509Certificate2>)
                Assert.Equal(collection.OfType<X509Certificate2>(), importedCollection.OfType<X509Certificate2>());
            }
        }

        [Fact]
        public static void MultipleImport()
        {
            var collection = new X509Certificate2Collection();

            collection.Import(Path.Combine("TestData", "DummyTcpServer.pfx"), null, default(X509KeyStorageFlags));
            collection.Import(TestData.PfxData, TestData.PfxDataPassword, default(X509KeyStorageFlags));

            Assert.Equal(3, collection.Count);
        }

        [Fact]
        [ActiveIssue(2743, PlatformID.AnyUnix)]
        public static void ExportMultiplePrivateKeys()
        {
            var collection = new X509Certificate2Collection();

            collection.Import(Path.Combine("TestData", "DummyTcpServer.pfx"), null, X509KeyStorageFlags.Exportable);
            collection.Import(TestData.PfxData, TestData.PfxDataPassword, X509KeyStorageFlags.Exportable);

            // Pre-condition, we have multiple private keys
            int originalPrivateKeyCount = collection.OfType<X509Certificate2>().Count(c => c.HasPrivateKey);
            Assert.Equal(2, originalPrivateKeyCount);

            // Export, re-import.
            byte[] exported;

            try
            {
                exported = collection.Export(X509ContentType.Pkcs12);
            }
            catch (PlatformNotSupportedException)
            {
                // [ActiveIssue(2743, PlatformID.AnyUnix)]
                // Our Unix builds can't export more than one private key in a single PFX, so this is
                // their exit point.
                //
                // If Windows gets here, or any exception other than PlatformNotSupportedException is raised,
                // let that fail the test.
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    throw;
                }

                return;
            }

            // As the other half of issue 2743, if we make it this far we better be Windows (or remove the catch
            // above)
            Assert.True(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), "RuntimeInformation.IsOSPlatform(OSPlatform.Windows)");

            var importedCollection = new X509Certificate2Collection();
            importedCollection.Import(exported);

            Assert.Equal(collection.Count, importedCollection.Count);

            int importedPrivateKeyCount = importedCollection.OfType<X509Certificate2>().Count(c => c.HasPrivateKey);
            Assert.Equal(originalPrivateKeyCount, importedPrivateKeyCount);
        }

        [Fact]
        public static void X509CertificateCollectionCopyTo()
        {
            using (X509Certificate2 c1 = new X509Certificate2())
            using (X509Certificate2 c2 = new X509Certificate2())
            using (X509Certificate2 c3 = new X509Certificate2())
            {
                X509CertificateCollection cc = new X509CertificateCollection(new X509Certificate[] { c1, c2, c3 });

                X509Certificate[] array1 = new X509Certificate[cc.Count];
                cc.CopyTo(array1, 0);

                Assert.Same(c1, array1[0]);
                Assert.Same(c2, array1[1]);
                Assert.Same(c3, array1[2]);

                X509Certificate[] array2 = new X509Certificate[cc.Count];
                ((ICollection)cc).CopyTo(array2, 0);

                Assert.Same(c1, array2[0]);
                Assert.Same(c2, array2[1]);
                Assert.Same(c3, array2[2]);
            }
        }

        [Fact]
        public static void X509Certificate2CollectionCopyTo()
        {
            using (X509Certificate2 c1 = new X509Certificate2())
            using (X509Certificate2 c2 = new X509Certificate2())
            using (X509Certificate2 c3 = new X509Certificate2())
            {
                X509Certificate2Collection cc = new X509Certificate2Collection(new X509Certificate2[] { c1, c2, c3 });

                X509Certificate2[] array1 = new X509Certificate2[cc.Count];
                cc.CopyTo(array1, 0);

                Assert.Same(c1, array1[0]);
                Assert.Same(c2, array1[1]);
                Assert.Same(c3, array1[2]);

                X509Certificate2[] array2 = new X509Certificate2[cc.Count];
                ((ICollection)cc).CopyTo(array2, 0);

                Assert.Same(c1, array2[0]);
                Assert.Same(c2, array2[1]);
                Assert.Same(c3, array2[2]);
            }
        }

        private static void TestExportSingleCert(X509ContentType ct)
        {
            using (var msCer = new X509Certificate2(TestData.MsCertificate))
            using (var pfxCer = new X509Certificate2(TestData.PfxData, TestData.PfxDataPassword))
            {
                X509Certificate2Collection cc = new X509Certificate2Collection(new X509Certificate2[] { msCer, pfxCer });

                byte[] blob = cc.Export(ct);

                Assert.Equal(ct, X509Certificate2.GetCertContentType(blob));

                X509Certificate2Collection cc2 = new X509Certificate2Collection();
                cc2.Import(blob);
                int count = cc2.Count;
                Assert.Equal(1, count);

                using (X509Certificate2 c = cc2[0])
                {
                    Assert.NotSame(msCer, c);
                    Assert.NotSame(pfxCer, c);

                    Assert.True(msCer.Equals(c) || pfxCer.Equals(c));
                }
            }
        }

        private static void TestExportStore(X509ContentType ct)
        {
            using (var msCer = new X509Certificate2(TestData.MsCertificate))
            using (var pfxCer = new X509Certificate2(TestData.PfxData, TestData.PfxDataPassword))
            {
                X509Certificate2Collection cc = new X509Certificate2Collection(new X509Certificate2[] { msCer, pfxCer });

                byte[] blob = cc.Export(ct);

                Assert.Equal(ct, X509Certificate2.GetCertContentType(blob));

                X509Certificate2Collection cc2 = new X509Certificate2Collection();
                cc2.Import(blob);
                int count = cc2.Count;
                Assert.Equal(2, count);

                X509Certificate2[] cs = cc2.ToArray().OrderBy(c => c.Subject).ToArray();

                using (X509Certificate2 first = cs[0])
                {
                    Assert.NotSame(msCer, first);
                    Assert.Equal(msCer, first);
                }

                using (X509Certificate2 second = cs[1])
                {
                    Assert.NotSame(pfxCer, second);
                    Assert.Equal(pfxCer, second);
                }
            }
        }

        private static X509Certificate2[] ToArray(this X509Certificate2Collection col)
        {
            X509Certificate2[] array = new X509Certificate2[col.Count];
            for (int i = 0; i < col.Count; i++)
            {
                array[i] = col[i];
            }
            return array;
        }
    }
}
