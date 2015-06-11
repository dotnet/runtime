// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Collections;

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
            }
        }

        [Fact]
        public static void X509Certificate2CollectionEnumeratorModification()
        {
            using (X509Certificate2 c1 = new X509Certificate2())
            using (X509Certificate2 c2 = new X509Certificate2())
            using (X509Certificate2 c3 = new X509Certificate2())
            {
                X509CertificateCollection cc = new X509CertificateCollection(new X509Certificate[] { c1, c2, c3 });
                X509Certificate2Collection.X509CertificateEnumerator e = cc.GetEnumerator();

                cc.Add(c1);

                // Collection changed.
                Assert.Throws<InvalidOperationException>(() => e.MoveNext());
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

                il[0] = "Bogus";
                Assert.Equal("Bogus", il[0]);

                object ignored;
                Assert.Throws<InvalidCastException>(() => ignored = cc[0]);
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
        [ActiveIssue(1993, PlatformID.AnyUnix)]
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
        [ActiveIssue(1977, PlatformID.Any)]
        public static void ImportFromFileTests()
        {
            using (var pfxCer = new X509Certificate2(TestData.PfxData, TestData.PfxDataPassword))
            {
                X509Certificate2Collection cc2 = new X509Certificate2Collection();
                cc2.Import(@"TestData\My.pfx", TestData.PfxDataPassword, X509KeyStorageFlags.DefaultKeySet);
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
