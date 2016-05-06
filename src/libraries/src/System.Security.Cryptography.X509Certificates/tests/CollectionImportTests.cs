// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Linq;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    public static class CollectionImportTests
    {
        [Fact]
        public static void ImportNull()
        {
            X509Certificate2Collection cc2 = new X509Certificate2Collection();
            Assert.Throws<ArgumentNullException>(() => cc2.Import((byte[])null));
            Assert.Throws<ArgumentNullException>(() => cc2.Import((string)null));
        }

        [Fact]
        public static void ImportEmpty_Pkcs12()
        {
            using (ImportedCollection ic = Cert.Import(TestData.EmptyPfx))
            {
                X509Certificate2Collection collection = ic.Collection;
                Assert.Equal(0, collection.Count);
            }
        }

        [Fact]
        public static void ImportX509DerBytes()
        {
            using (ImportedCollection ic = Cert.Import(TestData.MsCertificate))
            {
                X509Certificate2Collection collection = ic.Collection;
                Assert.Equal(1, collection.Count);
            }
        }

        [Fact]
        public static void ImportX509PemBytes()
        {
            using (ImportedCollection ic = Cert.Import(TestData.MsCertificatePemBytes))
            {
                X509Certificate2Collection collection = ic.Collection;
                Assert.Equal(1, collection.Count);
            }
        }

        [Fact]
        public static void ImportX509DerFile()
        {
            using (ImportedCollection ic = Cert.Import(Path.Combine("TestData", "MS.cer")))
            {
                X509Certificate2Collection collection = ic.Collection;
                Assert.Equal(1, collection.Count);
            }
        }

        [Fact]
        public static void ImportX509PemFile()
        {
            using (ImportedCollection ic = Cert.Import(Path.Combine("TestData", "MS.pem")))
            {
                X509Certificate2Collection collection = ic.Collection;
                Assert.Equal(1, collection.Count);
            }
        }

        [Fact]
        public static void ImportPkcs7DerBytes_Empty()
        {
            using (ImportedCollection ic = Cert.Import(TestData.Pkcs7EmptyDerBytes))
            {
                X509Certificate2Collection collection = ic.Collection;
                Assert.Equal(0, collection.Count);
            }
        }

        [Fact]
        public static void ImportPkcs7PemBytes_Empty()
        {
            using (ImportedCollection ic = Cert.Import(TestData.Pkcs7EmptyPemBytes))
            {
                X509Certificate2Collection collection = ic.Collection;
                Assert.Equal(0, collection.Count);
            }
        }

        [Fact]
        public static void ImportPkcs7DerFile_Empty()
        {
            using (ImportedCollection ic = Cert.Import(Path.Combine("TestData", "empty.p7b")))
            {
                X509Certificate2Collection collection = ic.Collection;
                Assert.Equal(0, collection.Count);
            }
        }

        [Fact]
        public static void ImportPkcs7PemFile_Empty()
        {
            using (ImportedCollection ic = Cert.Import(Path.Combine("TestData", "empty.p7c")))
            {
                X509Certificate2Collection collection = ic.Collection;
                Assert.Equal(0, collection.Count);
            }
        }

        [Fact]
        public static void ImportPkcs7DerBytes_Single()
        {
            using (ImportedCollection ic = Cert.Import(TestData.Pkcs7SingleDerBytes))
            {
                X509Certificate2Collection collection = ic.Collection;
                Assert.Equal(1, collection.Count);
            }
        }

        [Fact]
        public static void ImportPkcs7PemBytes_Single()
        {
            using (ImportedCollection ic = Cert.Import(TestData.Pkcs7SinglePemBytes))
            {
                X509Certificate2Collection collection = ic.Collection;
                Assert.Equal(1, collection.Count);
            }
        }

        [Fact]
        public static void ImportPkcs7DerFile_Single()
        {
            using (ImportedCollection ic = Cert.Import(Path.Combine("TestData", "singlecert.p7b")))
            {
                X509Certificate2Collection collection = ic.Collection;
                Assert.Equal(1, collection.Count);
            }
        }

        [Fact]
        public static void ImportPkcs7PemFile_Single()
        {
            using (ImportedCollection ic = Cert.Import(Path.Combine("TestData", "singlecert.p7c")))
            {
                X509Certificate2Collection collection = ic.Collection;
                Assert.Equal(1, collection.Count);
            }
        }

        [Fact]
        public static void ImportPkcs7DerBytes_Chain()
        {
            using (ImportedCollection ic = Cert.Import(TestData.Pkcs7ChainDerBytes))
            {
                X509Certificate2Collection collection = ic.Collection;
                Assert.Equal(3, collection.Count);
            }
        }

        [Fact]
        public static void ImportPkcs7PemBytes_Chain()
        {
            using (ImportedCollection ic = Cert.Import(TestData.Pkcs7ChainPemBytes))
            {
                X509Certificate2Collection collection = ic.Collection;
                Assert.Equal(3, collection.Count);
            }
        }

        [Fact]
        public static void ImportPkcs7DerFile_Chain()
        {
            using (ImportedCollection ic = Cert.Import(Path.Combine("TestData", "certchain.p7b")))
            {
                X509Certificate2Collection collection = ic.Collection;
                Assert.Equal(3, collection.Count);
            }
        }

        [Fact]
        public static void ImportPkcs7PemFile_Chain()
        {
            using (ImportedCollection ic = Cert.Import(Path.Combine("TestData", "certchain.p7c")))
            {
                X509Certificate2Collection collection = ic.Collection;
                Assert.Equal(3, collection.Count);
            }
        }

        [Fact]
        public static void ImportPkcs12Bytes_Single()
        {
            using (ImportedCollection ic = Cert.Import(TestData.PfxData, TestData.PfxDataPassword, X509KeyStorageFlags.DefaultKeySet))
            {
                X509Certificate2Collection cc2 = ic.Collection;
                int count = cc2.Count;
                Assert.Equal(1, count);
            }
        }

        [Fact]
        public static void ImportPkcs12Bytes_Single_VerifyContents()
        {
            using (var pfxCer = new X509Certificate2(TestData.PfxData, TestData.PfxDataPassword))
            {
                using (ImportedCollection ic = Cert.Import(TestData.PfxData, TestData.PfxDataPassword, X509KeyStorageFlags.DefaultKeySet))
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
        public static void ImportPkcs12File_Single()
        {
            using (ImportedCollection ic = Cert.Import(Path.Combine("TestData", "My.pfx"), TestData.PfxDataPassword, X509KeyStorageFlags.DefaultKeySet))
            {
                X509Certificate2Collection cc2 = ic.Collection;
                int count = cc2.Count;
                Assert.Equal(1, count);
            }
        }

        [Fact]
        public static void ImportPkcs12Bytes_Chain()
        {
            using (ImportedCollection ic = Cert.Import(TestData.ChainPfxBytes, TestData.ChainPfxPassword, X509KeyStorageFlags.DefaultKeySet))
            {
                X509Certificate2Collection certs = ic.Collection;
                int count = certs.Count;
                Assert.Equal(3, count);
            }
        }

        [Fact]
        public static void ImportPkcs12File_Chain()
        {
            using (ImportedCollection ic = Cert.Import(Path.Combine("TestData", "test.pfx"), TestData.ChainPfxPassword, X509KeyStorageFlags.DefaultKeySet))
            {
                X509Certificate2Collection certs = ic.Collection;
                int count = certs.Count;
                Assert.Equal(3, count);
            }
        }

        [Fact]
        public static void ImportPkcs12File_Chain_VerifyContents()
        {
            using (ImportedCollection ic = Cert.Import(Path.Combine("TestData", "test.pfx"), TestData.ChainPfxPassword, X509KeyStorageFlags.DefaultKeySet))
            {
                X509Certificate2Collection certs = ic.Collection;
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
        }
    }
}
