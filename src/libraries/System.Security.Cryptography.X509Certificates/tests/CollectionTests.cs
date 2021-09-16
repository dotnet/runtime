// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    public static class CollectionTests
    {
        [Fact]
        public static void X509CertificateCollectionsProperties()
        {
            IList ilist = new X509CertificateCollection();
            Assert.False(ilist.IsSynchronized);
            Assert.False(ilist.IsFixedSize);
            Assert.False(ilist.IsReadOnly);

            ilist = new X509Certificate2Collection();
            Assert.False(ilist.IsSynchronized);
            Assert.False(ilist.IsFixedSize);
            Assert.False(ilist.IsReadOnly);
        }

        [Fact]
        public static void X509CertificateCollectionConstructors()
        {
            using (X509Certificate c1 = new X509Certificate())
            using (X509Certificate c2 = new X509Certificate())
            using (X509Certificate c3 = new X509Certificate())
            {
                X509CertificateCollection cc = new X509CertificateCollection(new X509Certificate[] { c1, c2, c3 });
                Assert.Equal(3, cc.Count);
                Assert.Same(c1, cc[0]);
                Assert.Same(c2, cc[1]);
                Assert.Same(c3, cc[2]);

                X509CertificateCollection cc2 = new X509CertificateCollection(cc);
                Assert.Equal(3, cc2.Count);
                Assert.Same(c1, cc2[0]);
                Assert.Same(c2, cc2[1]);
                Assert.Same(c3, cc2[2]);

                Assert.Throws<ArgumentNullException>(() => new X509CertificateCollection(new X509Certificate[] { c1, c2, null, c3 }));
            }
        }

        [Fact]
        public static void X509Certificate2CollectionConstructors()
        {
            using (X509Certificate2 c1 = new X509Certificate2())
            using (X509Certificate2 c2 = new X509Certificate2())
            using (X509Certificate2 c3 = new X509Certificate2())
            {
                X509Certificate2Collection cc = new X509Certificate2Collection(new X509Certificate2[] { c1, c2, c3 });
                Assert.Equal(3, cc.Count);
                Assert.Same(c1, cc[0]);
                Assert.Same(c2, cc[1]);
                Assert.Same(c3, cc[2]);

                X509Certificate2Collection cc2 = new X509Certificate2Collection(cc);
                Assert.Equal(3, cc2.Count);
                Assert.Same(c1, cc2[0]);
                Assert.Same(c2, cc2[1]);
                Assert.Same(c3, cc2[2]);

                Assert.Throws<ArgumentNullException>(() => new X509Certificate2Collection(new X509Certificate2[] { c1, c2, null, c3 }));

                using (X509Certificate c4 = new X509Certificate())
                {
                    X509Certificate2Collection collection = new X509Certificate2Collection { c1, c2, c3 };
                    ((IList)collection).Add(c4); // Add non-X509Certificate2 object

                    Assert.Throws<InvalidCastException>(() => new X509Certificate2Collection(collection));
                }
            }
        }

        [Fact]
        public static void X509Certificate2CollectionEnumerator()
        {
            using (X509Certificate2 c1 = new X509Certificate2())
            using (X509Certificate2 c2 = new X509Certificate2())
            using (X509Certificate2 c3 = new X509Certificate2())
            {
                X509Certificate2Collection cc = new X509Certificate2Collection(new X509Certificate2[] { c1, c2, c3 });
                object ignored;

                X509Certificate2Enumerator e = cc.GetEnumerator();
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

                    // Ended
                    Assert.Throws<InvalidOperationException>(() => ignored = e.Current);

                    e.Reset();
                }

                IEnumerator e2 = cc.GetEnumerator();
                TestNonGenericEnumerator(e2, c1, c2, c3);

                IEnumerator e3 = ((IEnumerable)cc).GetEnumerator();
                TestNonGenericEnumerator(e3, c1, c2, c3);
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
                object ignored;

                X509CertificateCollection.X509CertificateEnumerator e = cc.GetEnumerator();
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

                    // Ended
                    Assert.Throws<InvalidOperationException>(() => ignored = e.Current);

                    e.Reset();
                }

                IEnumerator e2 = cc.GetEnumerator();
                TestNonGenericEnumerator(e2, c1, c2, c3);

                IEnumerator e3 = ((IEnumerable)cc).GetEnumerator();
                TestNonGenericEnumerator(e3, c1, c2, c3);
            }
        }

        private static void TestNonGenericEnumerator(IEnumerator e, object c1, object c2, object c3)
        {
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

                // Ended
                Assert.Throws<InvalidOperationException>(() => ignored = e.Current);

                e.Reset();
            }
        }

        [Fact]
        public static void X509CertificateCollectionThrowsArgumentNullException()
        {
            using (X509Certificate certificate = new X509Certificate())
            {
                Assert.Throws<ArgumentNullException>(() => new X509CertificateCollection((X509Certificate[])null));
                Assert.Throws<ArgumentNullException>(() => new X509CertificateCollection((X509CertificateCollection)null));

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

            AssertExtensions.Throws<ArgumentNullException, NullReferenceException>(
                () => new X509CertificateCollection.X509CertificateEnumerator(null));
        }

        [Fact]
        public static void X509Certificate2CollectionThrowsArgumentNullException()
        {
            using (X509Certificate2 certificate = new X509Certificate2())
            {
                Assert.Throws<ArgumentNullException>(() => new X509Certificate2Collection((X509Certificate2[])null));
                Assert.Throws<ArgumentNullException>(() => new X509Certificate2Collection((X509Certificate2Collection)null));

                X509Certificate2Collection collection = new X509Certificate2Collection { certificate };

                Assert.Throws<ArgumentNullException>(() => collection[0] = null);
                Assert.Throws<ArgumentNullException>(() => collection.Add((X509Certificate)null));
                Assert.Throws<ArgumentNullException>(() => collection.Add((X509Certificate2)null));
                Assert.Throws<ArgumentNullException>(() => collection.AddRange((X509Certificate[])null));
                Assert.Throws<ArgumentNullException>(() => collection.AddRange((X509CertificateCollection)null));
                Assert.Throws<ArgumentNullException>(() => collection.AddRange((X509Certificate2[])null));
                Assert.Throws<ArgumentNullException>(() => collection.AddRange((X509Certificate2Collection)null));
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
        public static void X509CertificateCollectionContains()
        {
            using (X509Certificate c1 = new X509Certificate())
            using (X509Certificate c2 = new X509Certificate())
            using (X509Certificate c3 = new X509Certificate())
            {
                X509CertificateCollection collection = new X509CertificateCollection(new X509Certificate[] { c1, c2, c3 });

                Assert.True(collection.Contains(c1));
                Assert.True(collection.Contains(c2));
                Assert.True(collection.Contains(c3));
                Assert.False(collection.Contains(null));

                IList ilist = (IList)collection;
                Assert.True(ilist.Contains(c1));
                Assert.True(ilist.Contains(c2));
                Assert.True(ilist.Contains(c3));
                Assert.False(ilist.Contains(null));
                Assert.False(ilist.Contains("Bogus"));
            }
        }

        [Fact]
        public static void X509Certificate2CollectionContains()
        {
            using (X509Certificate2 c1 = new X509Certificate2())
            using (X509Certificate2 c2 = new X509Certificate2())
            using (X509Certificate2 c3 = new X509Certificate2())
            {
                X509Certificate2Collection collection = new X509Certificate2Collection(new X509Certificate2[] { c1, c2, c3 });

                Assert.True(collection.Contains(c1));
                Assert.True(collection.Contains(c2));
                Assert.True(collection.Contains(c3));

                // Note: X509Certificate2Collection.Contains used to throw ArgumentNullException, but it
                // has been deliberately changed to no longer throw to match the behavior of
                // X509CertificateCollection.Contains and the IList.Contains implementation, which do not
                // throw.
                Assert.False(collection.Contains(null));

                IList ilist = (IList)collection;
                Assert.True(ilist.Contains(c1));
                Assert.True(ilist.Contains(c2));
                Assert.True(ilist.Contains(c3));
                Assert.False(ilist.Contains(null));
                Assert.False(ilist.Contains("Bogus"));
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
                Assert.Same(c1, cc[0]);

                idx = cc.Add(c2);
                Assert.Equal(1, idx);
                Assert.Same(c2, cc[1]);

                Assert.Throws<ArgumentNullException>(() => cc.Add(null));


                IList il = new X509CertificateCollection();
                idx = il.Add(c1);
                Assert.Equal(0, idx);
                Assert.Same(c1, il[0]);

                idx = il.Add(c2);
                Assert.Equal(1, idx);
                Assert.Same(c2, il[1]);

                Assert.Throws<ArgumentNullException>(() => il.Add(null));
            }
        }

        [Fact]
        public static void X509CertificateCollectionAsIList()
        {
            using (X509Certificate2 c1 = new X509Certificate2())
            using (X509Certificate2 c2 = new X509Certificate2())
            {
                IList il = new X509CertificateCollection();
                il.Add(c1);
                il.Add(c2);

                Assert.Throws<ArgumentNullException>(() => il[0] = null);
            }
        }

        [Fact]
        // On .NET Framework, list is untyped so it allows arbitrary types in it
        public static void X509CertificateCollectionAsIListBogusEntry()
        {
            using (X509Certificate2 c = new X509Certificate2())
            {
                IList il = new X509CertificateCollection();
                il.Add(c);

                string bogus = "Bogus";

                AssertExtensions.Throws<ArgumentException>("value", () => il[0] = bogus);
                AssertExtensions.Throws<ArgumentException>("value", () => il.Add(bogus));
                AssertExtensions.Throws<ArgumentException>("value", () => il.Remove(bogus));
                AssertExtensions.Throws<ArgumentException>("value", () => il.Insert(0, bogus));
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
        public static void ImportStoreSavedAsCerData()
        {
            using (var pfxCer = new X509Certificate2(TestData.PfxData, TestData.PfxDataPassword))
            {
                using (ImportedCollection ic = Cert.Import(TestData.StoreSavedAsCerData))
                {
                    X509Certificate2Collection cc2 = ic.Collection;
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
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]  // StoreSavedAsSerializedCerData not supported on Unix
        public static void ImportStoreSavedAsSerializedCerData_Windows()
        {
            using (var pfxCer = new X509Certificate2(TestData.PfxData, TestData.PfxDataPassword, Cert.EphemeralIfPossible))
            {
                using (ImportedCollection ic = Cert.Import(TestData.StoreSavedAsSerializedCerData))
                {
                    X509Certificate2Collection cc2 = ic.Collection;
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
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // StoreSavedAsSerializedCerData not supported on Unix
        public static void ImportStoreSavedAsSerializedCerData_Unix()
        {
            X509Certificate2Collection cc2 = new X509Certificate2Collection();
            Assert.ThrowsAny<CryptographicException>(() => cc2.Import(TestData.StoreSavedAsSerializedCerData));
            Assert.Equal(0, cc2.Count);
        }

        [Theory]
        [MemberData(nameof(StorageFlags))]
        [PlatformSpecific(TestPlatforms.Windows)]  // StoreSavedAsSerializedStoreData not supported on Unix
        public static void ImportStoreSavedAsSerializedStoreData_Windows(X509KeyStorageFlags keyStorageFlags)
        {
            using (var msCer = new X509Certificate2(TestData.MsCertificate))
            using (var pfxCer = new X509Certificate2(TestData.PfxData, TestData.PfxDataPassword, keyStorageFlags))
            using (ImportedCollection ic = Cert.Import(TestData.StoreSavedAsSerializedStoreData))
            {

                X509Certificate2Collection cc2 = ic.Collection;
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
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // StoreSavedAsSerializedStoreData not supported on Unix
        public static void ImportStoreSavedAsSerializedStoreData_Unix()
        {
            X509Certificate2Collection cc2 = new X509Certificate2Collection();
            Assert.ThrowsAny<CryptographicException>(() => cc2.Import(TestData.StoreSavedAsSerializedStoreData));
            Assert.Equal(0, cc2.Count);
        }

        [Fact]
        [SkipOnPlatform(PlatformSupport.MobileAppleCrypto, "PKCS#7 import is not available")]
        public static void ImportStoreSavedAsPfxData()
        {
            using (var msCer = new X509Certificate2(TestData.MsCertificate))
            using (var pfxCer = new X509Certificate2(TestData.PfxData, TestData.PfxDataPassword, Cert.EphemeralIfPossible))
            using (ImportedCollection ic = Cert.Import(TestData.StoreSavedAsPfxData))
            {
                X509Certificate2Collection cc2 = ic.Collection;
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
        public static void ImportInvalidData()
        {
            X509Certificate2Collection cc2 = new X509Certificate2Collection();
            Assert.ThrowsAny<CryptographicException>(() => cc2.Import(new byte[] { 0, 1, 1, 2, 3, 5, 8, 13, 21 }));
        }

        [Theory]
        [MemberData(nameof(StorageFlags))]
        public static void ImportFromFileTests(X509KeyStorageFlags storageFlags)
        {
            using (var pfxCer = new X509Certificate2(TestData.PfxData, TestData.PfxDataPassword, storageFlags))
            {
                using (ImportedCollection ic = Cert.Import(TestFiles.PfxFile, TestData.PfxDataPassword, storageFlags))
                {
                    X509Certificate2Collection cc2 = ic.Collection;
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
        }

        [Fact]
        public static void ImportMultiplePrivateKeysPfx()
        {
            using (ImportedCollection ic = Cert.Import(TestData.MultiPrivateKeyPfx))
            {
                X509Certificate2Collection collection = ic.Collection;

                Assert.Equal(2, collection.Count);

                foreach (X509Certificate2 cert in collection)
                {
                    Assert.True(cert.HasPrivateKey, "cert.HasPrivateKey");
                }
            }
        }

        [Fact]
        public static void ExportCert()
        {
            TestExportSingleCert(X509ContentType.Cert);
        }

        [Fact]
        public static void ExportCert_SecureString()
        {
            TestExportSingleCert_SecureStringPassword(X509ContentType.Cert);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]  // SerializedCert not supported on Unix
        public static void ExportSerializedCert_Windows()
        {
            TestExportSingleCert(X509ContentType.SerializedCert);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // SerializedCert not supported on Unix
        public static void ExportSerializedCert_Unix()
        {
            using (var msCer = new X509Certificate2(TestData.MsCertificate))
            using (var ecdsa256Cer = new X509Certificate2(TestData.ECDsa256Certificate))
            {
                X509Certificate2Collection cc = new X509Certificate2Collection(new[] { msCer, ecdsa256Cer });
                Assert.Throws<PlatformNotSupportedException>(() => cc.Export(X509ContentType.SerializedCert));
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]  // SerializedStore not supported on Unix
        public static void ExportSerializedStore_Windows()
        {
            TestExportStore(X509ContentType.SerializedStore);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // SerializedStore not supported on Unix
        public static void ExportSerializedStore_Unix()
        {
            using (var msCer = new X509Certificate2(TestData.MsCertificate))
            using (var ecdsa256Cer = new X509Certificate2(TestData.ECDsa256Certificate))
            {
                X509Certificate2Collection cc = new X509Certificate2Collection(new[] { msCer, ecdsa256Cer });
                Assert.Throws<PlatformNotSupportedException>(() => cc.Export(X509ContentType.SerializedStore));
            }
        }

        [Fact]
        [SkipOnPlatform(PlatformSupport.MobileAppleCrypto, "PKCS#7 export is not available")]
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
        public static void ExportEmpty_Pkcs12()
        {
            var collection = new X509Certificate2Collection();
            byte[] exported = collection.Export(X509ContentType.Pkcs12);

            // The empty PFX is legal, the answer won't be null.
            Assert.NotNull(exported);
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

                using (ImportedCollection ic = Cert.Import(exported))
                {
                    X509Certificate2Collection importedCollection = ic.Collection;

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
                    Assert.Equal(collection, importedCollection);
                }
            }
        }

        [Fact]
        public static void MultipleImport()
        {
            var collection = new X509Certificate2Collection();
            try
            {
                collection.Import(TestFiles.DummyTcpServerPfxFile, (string)null, Cert.EphemeralIfPossible);
                collection.Import(TestData.PfxData, TestData.PfxDataPassword, Cert.EphemeralIfPossible);
                Assert.Equal(3, collection.Count);
            }
            finally
            {
                foreach (X509Certificate2 cert in collection)
                {
                    cert.Dispose();
                }
            }
        }

        [Fact]
        public static void ExportMultiplePrivateKeys()
        {
            var collection = new X509Certificate2Collection();

            try
            {
                collection.Import(TestFiles.DummyTcpServerPfxFile, (string)null, X509KeyStorageFlags.Exportable | Cert.EphemeralIfPossible);
                collection.Import(TestData.PfxData, TestData.PfxDataPassword, X509KeyStorageFlags.Exportable | Cert.EphemeralIfPossible);

                // Pre-condition, we have multiple private keys
                int originalPrivateKeyCount = collection.Count(c => c.HasPrivateKey);
                Assert.Equal(2, originalPrivateKeyCount);

                byte[] exported = collection.Export(X509ContentType.Pkcs12);

                using (ImportedCollection ic = Cert.Import(exported))
                {
                    X509Certificate2Collection importedCollection = ic.Collection;

                    Assert.Equal(collection.Count, importedCollection.Count);

                    int importedPrivateKeyCount = importedCollection.Count(c => c.HasPrivateKey);
                    Assert.Equal(originalPrivateKeyCount, importedPrivateKeyCount);
                }
            }
            finally
            {
                foreach (X509Certificate2 cert in collection)
                {
                    cert.Dispose();
                }
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.MacCatalyst | TestPlatforms.tvOS, "The PKCS#12 Exportable flag is not supported on iOS/MacCatalyst/tvOS")]
        public static void CanAddMultipleCertsWithSinglePrivateKey()
        {
            using (var oneWithKey = new X509Certificate2(TestData.PfxData, TestData.PfxDataPassword, X509KeyStorageFlags.Exportable | Cert.EphemeralIfPossible))
            using (var twoWithoutKey = new X509Certificate2(TestData.ComplexNameInfoCert))
            {
                Assert.True(oneWithKey.HasPrivateKey);

                var col = new X509Certificate2Collection
                {
                    oneWithKey,
                    twoWithoutKey,
                };

                Assert.Equal(1, col.Count(x => x.HasPrivateKey));
                Assert.Equal(2, col.Count);

                byte[] buffer = col.Export(X509ContentType.Pfx);

                using (ImportedCollection newCollection = Cert.Import(buffer))
                {
                    Assert.Equal(2, newCollection.Collection.Count);
                }
            }
        }

        [Fact]
        public static void X509CertificateCollectionCopyTo()
        {
            using (X509Certificate c1 = new X509Certificate())
            using (X509Certificate c2 = new X509Certificate())
            using (X509Certificate c3 = new X509Certificate())
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
        [ActiveIssue("https://github.com/dotnet/runtime/issues/57506", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoRuntime), nameof(PlatformDetection.IsMariner))]
        public static void X509ChainElementCollection_CopyTo_NonZeroLowerBound_ThrowsIndexOutOfRangeException()
        {
            using (var microsoftDotCom = new X509Certificate2(TestData.MicrosoftDotComSslCertBytes))
            using (var microsoftDotComIssuer = new X509Certificate2(TestData.MicrosoftDotComIssuerBytes))
            using (var microsoftDotComRoot = new X509Certificate2(TestData.MicrosoftDotComRootBytes))
            using (var chainHolder = new ChainHolder())
            {
                X509Chain chain = chainHolder.Chain;

                chain.ChainPolicy.ExtraStore.Add(microsoftDotComRoot);
                chain.ChainPolicy.ExtraStore.Add(microsoftDotComIssuer);
                chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllFlags;

                // Halfway between microsoftDotCom's NotBefore and NotAfter
                // This isn't a boundary condition test.
                chain.ChainPolicy.VerificationTime = new DateTime(2021, 02, 26, 12, 01, 01, DateTimeKind.Local);

                bool valid = chain.Build(microsoftDotCom);
                Assert.True(valid, "Precondition: Chain built validly");

                ICollection collection = chain.ChainElements;
                Array array = Array.CreateInstance(typeof(object), new int[] { 10 }, new int[] { 10 });
                Assert.Throws<IndexOutOfRangeException>(() => collection.CopyTo(array, 0));
            }
        }

        [Fact]
        public static void X509ExtensionCollection_CopyTo_NonZeroLowerBound_ThrowsIndexOutOfRangeException()
        {
            using (X509Certificate2 cert = new X509Certificate2(TestData.PfxData, TestData.PfxDataPassword, Cert.EphemeralIfPossible))
            {
                ICollection collection = cert.Extensions;
                Array array = Array.CreateInstance(typeof(object), new int[] { 10 }, new int[] { 10 });
                Assert.Throws<IndexOutOfRangeException>(() => collection.CopyTo(array, 0));
            }
        }

        [Fact]
        public static void X509CertificateCollectionIndexOf()
        {
            using (X509Certificate2 c1 = new X509Certificate2())
            using (X509Certificate2 c2 = new X509Certificate2(TestData.PfxData, TestData.PfxDataPassword, Cert.EphemeralIfPossible))
            {
                X509CertificateCollection cc = new X509CertificateCollection(new X509Certificate[] { c1, c2 });
                Assert.Equal(0, cc.IndexOf(c1));
                Assert.Equal(1, cc.IndexOf(c2));

                IList il = cc;
                Assert.Equal(0, il.IndexOf(c1));
                Assert.Equal(1, il.IndexOf(c2));
            }
        }

        [Fact]
        public static void X509CertificateCollectionRemove()
        {
            using (X509Certificate2 c1 = new X509Certificate2())
            using (X509Certificate2 c2 = new X509Certificate2(TestData.PfxData, TestData.PfxDataPassword, Cert.EphemeralIfPossible))
            {
                X509CertificateCollection cc = new X509CertificateCollection(new X509Certificate[] { c1, c2 });

                cc.Remove(c1);
                Assert.Equal(1, cc.Count);
                Assert.Same(c2, cc[0]);

                cc.Remove(c2);
                Assert.Equal(0, cc.Count);

                AssertExtensions.Throws<ArgumentException>(null, () => cc.Remove(c2));

                IList il = new X509CertificateCollection(new X509Certificate[] { c1, c2 });

                il.Remove(c1);
                Assert.Equal(1, il.Count);
                Assert.Same(c2, il[0]);

                il.Remove(c2);
                Assert.Equal(0, il.Count);

                AssertExtensions.Throws<ArgumentException>(null, () => il.Remove(c2));
            }
        }

        [Fact]
        public static void X509CertificateCollectionRemoveAt()
        {
            using (X509Certificate c1 = new X509Certificate())
            using (X509Certificate c2 = new X509Certificate())
            using (X509Certificate c3 = new X509Certificate())
            {
                X509CertificateCollection cc = new X509CertificateCollection(new X509Certificate[] { c1, c2, c3 });

                cc.RemoveAt(0);
                Assert.Equal(2, cc.Count);
                Assert.Same(c2, cc[0]);
                Assert.Same(c3, cc[1]);

                cc.RemoveAt(1);
                Assert.Equal(1, cc.Count);
                Assert.Same(c2, cc[0]);

                cc.RemoveAt(0);
                Assert.Equal(0, cc.Count);


                IList il = new X509CertificateCollection(new X509Certificate[] { c1, c2, c3 });

                il.RemoveAt(0);
                Assert.Equal(2, il.Count);
                Assert.Same(c2, il[0]);
                Assert.Same(c3, il[1]);

                il.RemoveAt(1);
                Assert.Equal(1, il.Count);
                Assert.Same(c2, il[0]);

                il.RemoveAt(0);
                Assert.Equal(0, il.Count);
            }
        }

        [Fact]
        public static void X509Certificate2CollectionRemoveRangeArray()
        {
            using (X509Certificate2 c1 = new X509Certificate2(TestData.MsCertificate))
            using (X509Certificate2 c2 = new X509Certificate2(TestData.DssCer))
            using (X509Certificate2 c1Clone = new X509Certificate2(TestData.MsCertificate))
            {
                X509Certificate2[] array = new X509Certificate2[] { c1, c2 };

                X509Certificate2Collection cc = new X509Certificate2Collection(array);
                cc.RemoveRange(array);
                Assert.Equal(0, cc.Count);

                cc = new X509Certificate2Collection(array);
                cc.RemoveRange(new X509Certificate2[] { c2, c1 });
                Assert.Equal(0, cc.Count);

                cc = new X509Certificate2Collection(array);
                cc.RemoveRange(new X509Certificate2[] { c1 });
                Assert.Equal(1, cc.Count);
                Assert.Same(c2, cc[0]);

                cc = new X509Certificate2Collection(array);
                Assert.Throws<ArgumentNullException>(() => cc.RemoveRange(new X509Certificate2[] { c1, c2, null }));
                Assert.Equal(2, cc.Count);
                Assert.Same(c1, cc[0]);
                Assert.Same(c2, cc[1]);

                cc = new X509Certificate2Collection(array);
                Assert.Throws<ArgumentNullException>(() => cc.RemoveRange(new X509Certificate2[] { c1, null, c2 }));
                Assert.Equal(2, cc.Count);
                Assert.Same(c2, cc[0]);
                Assert.Same(c1, cc[1]);

                // Remove c1Clone (success)
                // Remove c1 (exception)
                // Add c1Clone back
                // End state: { c1, c2 } => { c2, c1Clone }
                cc = new X509Certificate2Collection(array);
                AssertExtensions.Throws<ArgumentException>(null, () => cc.RemoveRange(new X509Certificate2[] { c1Clone, c1, c2 }));
                Assert.Equal(2, cc.Count);
                Assert.Same(c2, cc[0]);
                Assert.Same(c1Clone, cc[1]);
            }
        }

        [Fact]
        public static void X509Certificate2CollectionRemoveRangeCollection()
        {
            using (X509Certificate2 c1 = new X509Certificate2(TestData.MsCertificate))
            using (X509Certificate2 c2 = new X509Certificate2(TestData.DssCer))
            using (X509Certificate2 c1Clone = new X509Certificate2(TestData.MsCertificate))
            using (X509Certificate c3 = new X509Certificate())
            {
                X509Certificate2[] array = new X509Certificate2[] { c1, c2 };

                X509Certificate2Collection cc = new X509Certificate2Collection(array);
                cc.RemoveRange(new X509Certificate2Collection { c1, c2 });
                Assert.Equal(0, cc.Count);

                cc = new X509Certificate2Collection(array);
                cc.RemoveRange(new X509Certificate2Collection { c2, c1 });
                Assert.Equal(0, cc.Count);

                cc = new X509Certificate2Collection(array);
                cc.RemoveRange(new X509Certificate2Collection { c1 });
                Assert.Equal(1, cc.Count);
                Assert.Same(c2, cc[0]);

                cc = new X509Certificate2Collection(array);
                X509Certificate2Collection collection = new X509Certificate2Collection();
                collection.Add(c1);
                collection.Add(c2);
                ((IList)collection).Add(c3); // Add non-X509Certificate2 object
                Assert.Throws<InvalidCastException>(() => cc.RemoveRange(collection));
                Assert.Equal(2, cc.Count);
                Assert.Same(c1, cc[0]);
                Assert.Same(c2, cc[1]);

                cc = new X509Certificate2Collection(array);
                collection = new X509Certificate2Collection();
                collection.Add(c1);
                ((IList)collection).Add(c3); // Add non-X509Certificate2 object
                collection.Add(c2);
                Assert.Throws<InvalidCastException>(() => cc.RemoveRange(collection));
                Assert.Equal(2, cc.Count);
                Assert.Same(c2, cc[0]);
                Assert.Same(c1, cc[1]);

                // Remove c1Clone (success)
                // Remove c1 (exception)
                // Add c1Clone back
                // End state: { c1, c2 } => { c2, c1Clone }
                cc = new X509Certificate2Collection(array);
                collection = new X509Certificate2Collection
                {
                    c1Clone,
                    c1,
                    c2,
                };
                AssertExtensions.Throws<ArgumentException>(null, () => cc.RemoveRange(collection));
                Assert.Equal(2, cc.Count);
                Assert.Same(c2, cc[0]);
                Assert.Same(c1Clone, cc[1]);
            }
        }

        [Fact]
        public static void X509CertificateCollectionIndexer()
        {
            using (X509Certificate c1 = new X509Certificate())
            using (X509Certificate c2 = new X509Certificate())
            using (X509Certificate c3 = new X509Certificate())
            {
                X509CertificateCollection cc = new X509CertificateCollection(new X509Certificate[] { c1, c2, c3 });
                cc[0] = c3;
                cc[1] = c2;
                cc[2] = c1;
                Assert.Same(c3, cc[0]);
                Assert.Same(c2, cc[1]);
                Assert.Same(c1, cc[2]);

                IList il = cc;
                il[0] = c1;
                il[1] = c2;
                il[2] = c3;
                Assert.Same(c1, il[0]);
                Assert.Same(c2, il[1]);
                Assert.Same(c3, il[2]);
            }
        }

        [Fact]
        public static void X509Certificate2CollectionIndexer()
        {
            using (X509Certificate2 c1 = new X509Certificate2())
            using (X509Certificate2 c2 = new X509Certificate2())
            using (X509Certificate2 c3 = new X509Certificate2())
            {
                X509Certificate2Collection cc = new X509Certificate2Collection(new X509Certificate2[] { c1, c2, c3 });
                cc[0] = c3;
                cc[1] = c2;
                cc[2] = c1;
                Assert.Same(c3, cc[0]);
                Assert.Same(c2, cc[1]);
                Assert.Same(c1, cc[2]);

                IList il = cc;
                il[0] = c1;
                il[1] = c2;
                il[2] = c3;
                Assert.Same(c1, il[0]);
                Assert.Same(c2, il[1]);
                Assert.Same(c3, il[2]);
            }
        }

        [Fact]
        public static void X509CertificateCollectionInsertAndClear()
        {
            using (X509Certificate c1 = new X509Certificate())
            using (X509Certificate c2 = new X509Certificate())
            using (X509Certificate c3 = new X509Certificate())
            {
                X509CertificateCollection cc = new X509CertificateCollection();
                cc.Insert(0, c1);
                cc.Insert(1, c2);
                cc.Insert(2, c3);
                Assert.Equal(3, cc.Count);
                Assert.Same(c1, cc[0]);
                Assert.Same(c2, cc[1]);
                Assert.Same(c3, cc[2]);

                cc.Clear();
                Assert.Equal(0, cc.Count);

                cc.Add(c1);
                cc.Add(c3);
                Assert.Equal(2, cc.Count);
                Assert.Same(c1, cc[0]);
                Assert.Same(c3, cc[1]);

                cc.Insert(1, c2);
                Assert.Equal(3, cc.Count);
                Assert.Same(c1, cc[0]);
                Assert.Same(c2, cc[1]);
                Assert.Same(c3, cc[2]);

                cc.Clear();
                Assert.Equal(0, cc.Count);

                IList il = cc;
                il.Insert(0, c1);
                il.Insert(1, c2);
                il.Insert(2, c3);
                Assert.Equal(3, il.Count);
                Assert.Same(c1, il[0]);
                Assert.Same(c2, il[1]);
                Assert.Same(c3, il[2]);

                il.Clear();
                Assert.Equal(0, il.Count);

                il.Add(c1);
                il.Add(c3);
                Assert.Equal(2, il.Count);
                Assert.Same(c1, il[0]);
                Assert.Same(c3, il[1]);

                il.Insert(1, c2);
                Assert.Equal(3, il.Count);
                Assert.Same(c1, il[0]);
                Assert.Same(c2, il[1]);
                Assert.Same(c3, il[2]);

                il.Clear();
                Assert.Equal(0, il.Count);
            }
        }

        [Fact]
        public static void X509Certificate2CollectionInsert()
        {
            using (X509Certificate2 c1 = new X509Certificate2())
            using (X509Certificate2 c2 = new X509Certificate2())
            using (X509Certificate2 c3 = new X509Certificate2())
            {
                X509Certificate2Collection cc = new X509Certificate2Collection();
                cc.Insert(0, c3);
                cc.Insert(0, c2);
                cc.Insert(0, c1);

                Assert.Equal(3, cc.Count);
                Assert.Same(c1, cc[0]);
                Assert.Same(c2, cc[1]);
                Assert.Same(c3, cc[2]);
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

        [Fact]
        public static void X509CertificateCollectionGetHashCode()
        {
            using (X509Certificate c1 = new X509Certificate())
            using (X509Certificate c2 = new X509Certificate())
            using (X509Certificate c3 = new X509Certificate())
            {
                X509CertificateCollection cc = new X509CertificateCollection(new X509Certificate[] { c1, c2, c3 });

                int expected = c1.GetHashCode() + c2.GetHashCode() + c3.GetHashCode();
                Assert.Equal(expected, cc.GetHashCode());
            }
        }

        [Fact]
        public static void X509Certificate2CollectionGetHashCode()
        {
            using (X509Certificate2 c1 = new X509Certificate2())
            using (X509Certificate2 c2 = new X509Certificate2())
            using (X509Certificate2 c3 = new X509Certificate2())
            {
                X509Certificate2Collection cc = new X509Certificate2Collection(new X509Certificate2[] { c1, c2, c3 });

                int expected = c1.GetHashCode() + c2.GetHashCode() + c3.GetHashCode();
                Assert.Equal(expected, cc.GetHashCode());
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/57506", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoRuntime), nameof(PlatformDetection.IsMariner))]
        public static void X509ChainElementCollection_IndexerVsEnumerator()
        {
            using (var microsoftDotCom = new X509Certificate2(TestData.MicrosoftDotComSslCertBytes))
            using (var microsoftDotComIssuer = new X509Certificate2(TestData.MicrosoftDotComIssuerBytes))
            using (var microsoftDotComRoot = new X509Certificate2(TestData.MicrosoftDotComRootBytes))
            using (var chainHolder = new ChainHolder())
            {
                X509Chain chain = chainHolder.Chain;

                chain.ChainPolicy.ExtraStore.Add(microsoftDotComRoot);
                chain.ChainPolicy.ExtraStore.Add(microsoftDotComIssuer);
                chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;

                // Halfway between microsoftDotCom's NotBefore and NotAfter
                // This isn't a boundary condition test.
                chain.ChainPolicy.VerificationTime = new DateTime(2021, 02, 26, 12, 01, 01, DateTimeKind.Local);
                chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;

                bool valid = chain.Build(microsoftDotCom);
                Assert.True(valid, "Precondition: Chain built validly");

                int position = 0;

                foreach (X509ChainElement chainElement in chain.ChainElements)
                {
                    X509ChainElement indexerElement = chain.ChainElements[position];

                    Assert.NotNull(chainElement);
                    Assert.NotNull(indexerElement);

                    Assert.Same(indexerElement, chainElement);
                    position++;
                }
            }
        }

        [Fact]
        public static void X509ExtensionCollection_OidIndexer_ByOidValue()
        {
            const string SubjectKeyIdentifierOidValue = "2.5.29.14";

            using (var cert = new X509Certificate2(TestData.MsCertificate))
            {
                X509ExtensionCollection extensions = cert.Extensions;
                // Stable index can be counted on by ExtensionsTests.ReadExtensions().
                X509Extension skidExtension = extensions[1];

                // Precondition: We've found the SKID extension.
                Assert.Equal(SubjectKeyIdentifierOidValue, skidExtension.Oid.Value);

                X509Extension byValue = extensions[SubjectKeyIdentifierOidValue];
                Assert.Same(skidExtension, byValue);
            }
        }

        [Fact]
        public static void X509ExtensionCollection_OidIndexer_ByOidFriendlyName()
        {
            const string SubjectKeyIdentifierOidValue = "2.5.29.14";

            using (var cert = new X509Certificate2(TestData.MsCertificate))
            {
                X509ExtensionCollection extensions = cert.Extensions;
                // Stable index can be counted on by ExtensionsTests.ReadExtensions().
                X509Extension skidExtension = extensions[1];

                // Precondition: We've found the SKID extension.
                Assert.Equal(SubjectKeyIdentifierOidValue, skidExtension.Oid.Value);

                // The friendly name of "Subject Key Identifier" is localized, but
                // we can use the invariant form to ask for the friendly name to ask
                // for the extension by friendly name.
                X509Extension byFriendlyName = extensions[new Oid(SubjectKeyIdentifierOidValue).FriendlyName];
                Assert.Same(skidExtension, byFriendlyName);
            }
        }

        [Fact]
        public static void X509ExtensionCollection_OidIndexer_NoMatchByValue()
        {
            const string RsaOidValue = "1.2.840.113549.1.1.1";

            using (var cert = new X509Certificate2(TestData.MsCertificate))
            {
                X509ExtensionCollection extensions = cert.Extensions;

                X509Extension byValue = extensions[RsaOidValue];
                Assert.Null(byValue);
            }
        }

        [Fact]
        public static void X509ExtensionCollection_OidIndexer_NoMatchByFriendlyName()
        {
            const string RsaOidValue = "1.2.840.113549.1.1.1";

            using (var cert = new X509Certificate2(TestData.MsCertificate))
            {
                X509ExtensionCollection extensions = cert.Extensions;

                // While "RSA" is actually invariant, this just guarantees that we're doing
                // the system-preferred lookup.
                X509Extension byFriendlyName = extensions[new Oid(RsaOidValue).FriendlyName];
                Assert.Null(byFriendlyName);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public static void SerializedCertDisposeDoesNotRemoveKeyFile()
        {
            using (X509Certificate2 fromPfx = new X509Certificate2(TestData.PfxData, TestData.PfxDataPassword))
            {
                Assert.True(fromPfx.HasPrivateKey, "fromPfx.HasPrivateKey - before");

                byte[] serializedCert = fromPfx.Export(X509ContentType.SerializedCert);

                X509Certificate2 fromSerialized;

                using (ImportedCollection imported = Cert.Import(serializedCert))
                {
                    fromSerialized = imported.Collection[0];
                    Assert.True(fromSerialized.HasPrivateKey, "fromSerialized.HasPrivateKey");
                    Assert.NotEqual(IntPtr.Zero, fromSerialized.Handle);
                }

                // The certificate got disposed by the collection holder.
                Assert.Equal(IntPtr.Zero, fromSerialized.Handle);

                using (RSA key = fromPfx.GetRSAPrivateKey())
                {
                    key.SignData(serializedCert, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                }
            }
        }

        [Fact]
        public static void ImportFromPem_SingleCertificate_Success()
        {
            using(ImportedCollection ic = Cert.ImportFromPem(TestData.ECDsaCertificate))
            {
                Assert.Single(ic.Collection);
                Assert.Equal("E844FA74BC8DCE46EF4F8605EA00008F161AB56F", ic.Collection[0].Thumbprint);
            }
        }

        [Fact]
        public static void ImportFromPem_SingleCertificate_IgnoresUnrelatedPems_Success()
        {
            string pemAggregate = TestData.ECDsaPkcs8Key + TestData.ECDsaCertificate;

            using(ImportedCollection ic = Cert.ImportFromPem(pemAggregate))
            {
                Assert.Single(ic.Collection);
                Assert.Equal("E844FA74BC8DCE46EF4F8605EA00008F161AB56F", ic.Collection[0].Thumbprint);
            }
        }

        [Fact]
        public static void ImportFromPem_MultiplePems_Success()
        {
            string pemAggregate = TestData.RsaCertificate + TestData.ECDsaCertificate;

            using(ImportedCollection ic = Cert.ImportFromPem(pemAggregate))
            {
                Assert.Equal(2, ic.Collection.Count);
                Assert.Equal("A33348E44A047A121F44E810E888899781E1FF19", ic.Collection[0].Thumbprint);
                Assert.Equal("E844FA74BC8DCE46EF4F8605EA00008F161AB56F", ic.Collection[1].Thumbprint);
            }
        }

        [Fact]
        public static void ImportFromPemFile_MultiplePems_Success()
        {
            string pemAggregate = TestData.RsaCertificate + TestData.ECDsaCertificate;

            using (TempFileHolder aggregatePemFile = new TempFileHolder(pemAggregate))
            using(ImportedCollection ic = Cert.ImportFromPemFile(aggregatePemFile.FilePath))
            {
                Assert.Equal(2, ic.Collection.Count);
                Assert.Equal("A33348E44A047A121F44E810E888899781E1FF19", ic.Collection[0].Thumbprint);
                Assert.Equal("E844FA74BC8DCE46EF4F8605EA00008F161AB56F", ic.Collection[1].Thumbprint);
            }
        }

        [Fact]
        public static void ImportFromPemFile_Null_Throws()
        {
            X509Certificate2Collection cc = new X509Certificate2Collection();

            AssertExtensions.Throws<ArgumentNullException>("certPemFilePath", () =>
                cc.ImportFromPemFile(null));
        }

        [Fact]
        public static void ImportFromPem_Exception_AllOrNothing()
        {
            using(ImportedCollection ic = Cert.ImportFromPem(TestData.DsaCertificate))
            {
                StringBuilder builder = new StringBuilder();
                builder.AppendLine(TestData.RsaCertificate);
                builder.AppendLine(@"
    -----BEGIN CERTIFICATE-----
    MIII
    -----END CERTIFICATE-----");
                builder.AppendLine(TestData.ECDsaCertificate);

                Assert.ThrowsAny<CryptographicException>(() => ic.Collection.ImportFromPem(builder.ToString()));
                Assert.Single(ic.Collection);
                Assert.Equal("35052C549E4E7805E4EA204C2BE7F4BC19B88EC8", ic.Collection[0].Thumbprint);
            }
        }

        [Fact]
        public static void ImportFromPem_NonCertificateContent_Pkcs12_Fails()
        {
            X509Certificate2Collection cc = new X509Certificate2Collection();

            using (X509Certificate2 cert = X509Certificate2.CreateFromPem(TestData.RsaCertificate, TestData.RsaPkcs1Key))
            {
                string content = Convert.ToBase64String(cert.Export(X509ContentType.Pkcs12));
                string certContents = $@"
-----BEGIN CERTIFICATE-----
{content}
-----END CERTIFICATE-----
";
                Assert.Throws<CryptographicException>(() => cc.ImportFromPem(certContents));
            }
        }

        [Fact]
        public static void ImportFromPem_NonCertificateContent_Pkcs7_Fails()
        {
            X509Certificate2Collection cc = new X509Certificate2Collection();

            string content = Convert.ToBase64String(TestData.Pkcs7ChainDerBytes);
            string certContents = $@"
-----BEGIN CERTIFICATE-----
{content}
-----END CERTIFICATE-----
";

            Assert.Throws<CryptographicException>(() => cc.ImportFromPem(certContents));
        }

        private static void TestExportSingleCert_SecureStringPassword(X509ContentType ct)
        {
            using (var pfxCer = new X509Certificate2(TestData.PfxData, TestData.CreatePfxDataPasswordSecureString(), Cert.EphemeralIfPossible))
            {
                TestExportSingleCert(ct, pfxCer);
            }
        }

        private static void TestExportSingleCert(X509ContentType ct)
        {
            using (var pfxCer = new X509Certificate2(TestData.PfxData, TestData.PfxDataPassword, Cert.EphemeralIfPossible))
            {
                TestExportSingleCert(ct, pfxCer);
            }
        }

        private static void TestExportSingleCert(X509ContentType ct, X509Certificate2 pfxCer)
        {
            using (var msCer = new X509Certificate2(TestData.MsCertificate))
            {
                X509Certificate2Collection cc = new X509Certificate2Collection(new X509Certificate2[] { msCer, pfxCer });

                byte[] blob = cc.Export(ct);

                Assert.Equal(ct, X509Certificate2.GetCertContentType(blob));

                using (ImportedCollection ic = Cert.Import(blob))
                {
                    X509Certificate2Collection cc2 = ic.Collection;
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
        }

        private static void TestExportStore(X509ContentType ct)
        {
            using (var msCer = new X509Certificate2(TestData.MsCertificate))
            using (var pfxCer = new X509Certificate2(TestData.PfxData, TestData.PfxDataPassword, Cert.EphemeralIfPossible))
            {
                X509Certificate2Collection cc = new X509Certificate2Collection(new X509Certificate2[] { msCer, pfxCer });

                byte[] blob = cc.Export(ct);

                Assert.Equal(ct, X509Certificate2.GetCertContentType(blob));

                using (ImportedCollection ic = Cert.Import(blob))
                {
                    X509Certificate2Collection cc2 = ic.Collection;
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
        }

        public static IEnumerable<object[]> StorageFlags => CollectionImportTests.StorageFlags;
    }
}
