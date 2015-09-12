// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
            var collection = new X509Certificate2Collection();

            collection.Import(TestData.EmptyPfx);

            Assert.Equal(0, collection.Count);
        }

        [Fact]
        public static void ImportX509DerBytes()
        {
            var collection = new X509Certificate2Collection();
            collection.Import(TestData.MsCertificate);

            Assert.Equal(1, collection.Count);
        }

        [Fact]
        public static void ImportX509PemBytes()
        {
            var collection = new X509Certificate2Collection();
            collection.Import(TestData.MsCertificatePemBytes);

            Assert.Equal(1, collection.Count);
        }

        [Fact]
        public static void ImportX509DerFile()
        {
            var collection = new X509Certificate2Collection();
            collection.Import(Path.Combine("TestData", "MS.cer"));

            Assert.Equal(1, collection.Count);
        }

        [Fact]
        [ActiveIssue(2635)]
        public static void ImportX509PemFile()
        {
            var collection = new X509Certificate2Collection();
            collection.Import(Path.Combine("TestData", "MS.pem"));

            Assert.Equal(1, collection.Count);
        }

        [Fact]
        public static void ImportPkcs7DerBytes_Empty()
        {
            var collection = new X509Certificate2Collection();
            collection.Import(TestData.Pkcs7EmptyDerBytes);

            Assert.Equal(0, collection.Count);
        }

        [Fact]
        public static void ImportPkcs7PemBytes_Empty()
        {
            var collection = new X509Certificate2Collection();
            collection.Import(TestData.Pkcs7EmptyPemBytes);

            Assert.Equal(0, collection.Count);
        }

        [Fact]
        [ActiveIssue(2635)]
        public static void ImportPkcs7DerFile_Empty()
        {
            var collection = new X509Certificate2Collection();
            collection.Import(Path.Combine("TestData", "empty.p7b"));

            Assert.Equal(0, collection.Count);
        }

        [Fact]
        [ActiveIssue(2635)]
        public static void ImportPkcs7PemFile_Empty()
        {
            var collection = new X509Certificate2Collection();
            collection.Import(Path.Combine("TestData", "empty.p7c"));

            Assert.Equal(0, collection.Count);
        }

        [Fact]
        public static void ImportPkcs7DerBytes_Single()
        {
            var collection = new X509Certificate2Collection();
            collection.Import(TestData.Pkcs7SingleDerBytes);

            Assert.Equal(1, collection.Count);
        }

        [Fact]
        public static void ImportPkcs7PemBytes_Single()
        {
            var collection = new X509Certificate2Collection();
            collection.Import(TestData.Pkcs7SinglePemBytes);

            Assert.Equal(1, collection.Count);
        }

        [Fact]
        [ActiveIssue(2635)]
        public static void ImportPkcs7DerFile_Single()
        {
            var collection = new X509Certificate2Collection();
            collection.Import(Path.Combine("TestData", "singlecert.p7b"));

            Assert.Equal(1, collection.Count);
        }

        [Fact]
        [ActiveIssue(2635)]
        public static void ImportPkcs7PemFile_Single()
        {
            var collection = new X509Certificate2Collection();
            collection.Import(Path.Combine("TestData", "singlecert.p7c"));

            Assert.Equal(1, collection.Count);
        }

        [Fact]
        public static void ImportPkcs7DerBytes_Chain()
        {
            var collection = new X509Certificate2Collection();
            collection.Import(TestData.Pkcs7ChainDerBytes);

            Assert.Equal(3, collection.Count);
        }

        [Fact]
        public static void ImportPkcs7PemBytes_Chain()
        {
            var collection = new X509Certificate2Collection();
            collection.Import(TestData.Pkcs7ChainPemBytes);

            Assert.Equal(3, collection.Count);
        }

        [Fact]
        [ActiveIssue(2635)]
        public static void ImportPkcs7DerFile_Chain()
        {
            var collection = new X509Certificate2Collection();
            collection.Import(Path.Combine("TestData", "certchain.p7b"));

            Assert.Equal(3, collection.Count);
        }

        [Fact]
        [ActiveIssue(2635)]
        public static void ImportPkcs7PemFile_Chain()
        {
            var collection = new X509Certificate2Collection();
            collection.Import(Path.Combine("TestData", "certchain.p7c"));

            Assert.Equal(3, collection.Count);
        }

        [Fact]
        public static void ImportPkcs12Bytes_Single()
        {
            X509Certificate2Collection cc2 = new X509Certificate2Collection();
            cc2.Import(TestData.PfxData, TestData.PfxDataPassword, X509KeyStorageFlags.DefaultKeySet);
            int count = cc2.Count;
            Assert.Equal(1, count);
        }

        [Fact]
        public static void ImportPkcs12Bytes_Single_VerifyContents()
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
        public static void ImportPkcs12File_Single()
        {
            X509Certificate2Collection cc2 = new X509Certificate2Collection();
            cc2.Import(Path.Combine("TestData", "My.pfx"), TestData.PfxDataPassword, X509KeyStorageFlags.DefaultKeySet);
            int count = cc2.Count;
            Assert.Equal(1, count);
        }

        [Fact]
        public static void ImportPkcs12Bytes_Chain()
        {
            X509Certificate2Collection certs = new X509Certificate2Collection();
            certs.Import(TestData.ChainPfxBytes, TestData.ChainPfxPassword, X509KeyStorageFlags.DefaultKeySet);
            int count = certs.Count;
            Assert.Equal(3, count);
        }

        [Fact]
        public static void ImportPkcs12File_Chain()
        {
            X509Certificate2Collection certs = new X509Certificate2Collection();
            certs.Import(Path.Combine("TestData", "test.pfx"), TestData.ChainPfxPassword, X509KeyStorageFlags.DefaultKeySet);
            int count = certs.Count;
            Assert.Equal(3, count);
        }

        [Fact]
        public static void ImportPkcs12File_Chain_VerifyContents()
        {
            X509Certificate2Collection certs = new X509Certificate2Collection();
            certs.Import(Path.Combine("TestData", "test.pfx"), TestData.ChainPfxPassword, X509KeyStorageFlags.DefaultKeySet);
            int count = certs.Count;
            Assert.Equal(3, count);

            const string leafCertSubject = "test.local";

            // TODO (#3207): Make this test be order-required once ordering is guaranteed on all platforms.
            string[] expectedSubjects =
            {
                "MS Passport Test Sub CA",
                "MS Passport Test Root CA",
                leafCertSubject,
            };

            string[] actualSubjects = new string[certs.Count];

            for (int i = 0; i < certs.Count; i++)
            {
                X509Certificate2 cert = certs[i];
                string subject = cert.GetNameInfo(X509NameType.SimpleName, false);
                actualSubjects[i] = subject;

                bool shouldHavePrivateKey = StringComparer.Ordinal.Equals(leafCertSubject, subject);

                if (shouldHavePrivateKey)
                {
                    Assert.True(cert.HasPrivateKey, "Certificate '" + subject + "' HasPrivateKey");
                }
                else
                {
                    Assert.False(cert.HasPrivateKey, "Certificate '" + subject + "' HasPrivateKey");
                }
            }

            // Confirm we saw each cert we expected.
            foreach (string expectedSubject in expectedSubjects)
            {
                Assert.Contains(expectedSubject, actualSubjects);
            }
        }
    }
}
