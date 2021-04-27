// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
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
            using (ImportedCollection ic = Cert.Import(TestFiles.MsCertificateDerFile))
            {
                X509Certificate2Collection collection = ic.Collection;
                Assert.Equal(1, collection.Count);
            }
        }

        [Fact]
        public static void ImportX509PemFile()
        {
            using (ImportedCollection ic = Cert.Import(TestFiles.MsCertificatePemFile))
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
            using (ImportedCollection ic = Cert.Import(TestFiles.Pkcs7EmptyDerFile))
            {
                X509Certificate2Collection collection = ic.Collection;
                Assert.Equal(0, collection.Count);
            }
        }

        [Fact]
        public static void ImportPkcs7PemFile_Empty()
        {
            using (ImportedCollection ic = Cert.Import(TestFiles.Pkcs7EmptyPemFile))
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

                Assert.Equal("D5B5BC1C458A558845BFF51CB4DFF31C", collection[0].SerialNumber);
            }
        }

        [Fact]
        public static void ImportPkcs7PemBytes_Single()
        {
            using (ImportedCollection ic = Cert.Import(TestData.Pkcs7SinglePemBytes))
            {
                X509Certificate2Collection collection = ic.Collection;
                Assert.Equal(1, collection.Count);

                Assert.Equal("D5B5BC1C458A558845BFF51CB4DFF31C", collection[0].SerialNumber);
            }
        }

        [Fact]
        public static void ImportPkcs7DerFile_Single()
        {
            using (ImportedCollection ic = Cert.Import(TestFiles.Pkcs7SingleDerFile))
            {
                X509Certificate2Collection collection = ic.Collection;
                Assert.Equal(1, collection.Count);

                Assert.Equal("D5B5BC1C458A558845BFF51CB4DFF31C", collection[0].SerialNumber);
            }
        }

        [Fact]
        public static void ImportPkcs7PemFile_Single()
        {
            using (ImportedCollection ic = Cert.Import(TestFiles.Pkcs7SinglePemFile))
            {
                X509Certificate2Collection collection = ic.Collection;
                Assert.Equal(1, collection.Count);

                Assert.Equal("D5B5BC1C458A558845BFF51CB4DFF31C", collection[0].SerialNumber);
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
        public static void ImportPkcs7DerByteSpan_Chain()
        {
            using (ImportedCollection ic = Cert.Import(TestData.Pkcs7ChainDerBytes.AsSpan()))
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
        public static void ImportPkcs7PemByteSpan_Chain()
        {
            using (ImportedCollection ic = Cert.Import(TestData.Pkcs7ChainPemBytes.AsSpan()))
            {
                X509Certificate2Collection collection = ic.Collection;
                Assert.Equal(3, collection.Count);
            }
        }

        [Fact]
        public static void ImportPkcs7DerFile_Chain()
        {
            using (ImportedCollection ic = Cert.Import(TestFiles.Pkcs7ChainDerFile))
            {
                X509Certificate2Collection collection = ic.Collection;
                Assert.Equal(3, collection.Count);
            }
        }

        [Fact]
        public static void ImportPkcs7PemFile_Chain()
        {
            using (ImportedCollection ic = Cert.Import(TestFiles.Pkcs7ChainPemFile))
            {
                X509Certificate2Collection collection = ic.Collection;
                Assert.Equal(3, collection.Count);
            }
        }

        [Theory]
        [MemberData(nameof(StorageFlags))]
        public static void ImportPkcs12Bytes_Single(X509KeyStorageFlags keyStorageFlags)
        {
            using (ImportedCollection ic = Cert.Import(TestData.PfxData, TestData.PfxDataPassword, keyStorageFlags))
            {
                X509Certificate2Collection cc2 = ic.Collection;
                int count = cc2.Count;
                Assert.Equal(1, count);
            }
        }

        [Theory]
        [MemberData(nameof(StorageFlags))]
        public static void ImportPkcs12Bytes_Single_VerifyContents_ArrayString(X509KeyStorageFlags keyStorageFlags)
        {
            using (ImportedCollection ic = Cert.Import(TestData.PfxData, TestData.PfxDataPassword, keyStorageFlags))
            {
                ImportPkcs12Bytes_Single_VerifyContents(ic);
            }
        }

        [Theory]
        [MemberData(nameof(StorageFlags))]
        public static void ImportPkcs12Bytes_Single_VerifyContents_SpanSpan(X509KeyStorageFlags keyStorageFlags)
        {
            ReadOnlySpan<byte> rawData = TestData.PfxData.AsSpan();
            ReadOnlySpan<char> password = TestData.PfxDataPassword.AsSpan();

            using (ImportedCollection ic = Cert.Import(rawData, password, keyStorageFlags))
            {
                ImportPkcs12Bytes_Single_VerifyContents(ic);
            }
        }

        private static void ImportPkcs12Bytes_Single_VerifyContents(ImportedCollection ic)
        {
            using (var pfxCer = new X509Certificate2(TestData.PfxData, TestData.PfxDataPassword, Cert.EphemeralIfPossible))
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

        [Theory]
        [MemberData(nameof(StorageFlags))]
        public static void ImportPkcs12File_Single(X509KeyStorageFlags keyStorageFlags)
        {
            using (ImportedCollection ic = Cert.Import(TestFiles.PfxFile, TestData.PfxDataPassword, keyStorageFlags))
            {
                X509Certificate2Collection cc2 = ic.Collection;
                int count = cc2.Count;
                Assert.Equal(1, count);
            }
        }

        [Theory]
        [MemberData(nameof(StorageFlags))]
        public static void ImportPkcs12File_Single_SpanPassword(X509KeyStorageFlags keyStorageFlags)
        {
            Span<char> password = stackalloc char[30];
            password.Fill('Z');
            TestData.PfxDataPassword.AsSpan().CopyTo(password.Slice(1));
            password = password.Slice(1, TestData.PfxDataPassword.Length);

            using (ImportedCollection ic = Cert.Import(TestFiles.PfxFile, password, keyStorageFlags))
            {
                X509Certificate2Collection cc2 = ic.Collection;
                int count = cc2.Count;
                Assert.Equal(1, count);
            }
        }

        [Theory]
        [MemberData(nameof(StorageFlags))]
        public static void ImportPkcs12Bytes_Chain(X509KeyStorageFlags keyStorageFlags)
        {
            using (ImportedCollection ic = Cert.Import(TestData.ChainPfxBytes, TestData.ChainPfxPassword, keyStorageFlags))
            {
                X509Certificate2Collection certs = ic.Collection;
                int count = certs.Count;
                Assert.Equal(3, count);
            }
        }

        [Theory]
        [MemberData(nameof(StorageFlags))]
        public static void ImportPkcs12ByteSpan_Chain(X509KeyStorageFlags keyStorageFlags)
        {
            using (ImportedCollection ic = Cert.Import(TestData.ChainPfxBytes.AsSpan(), TestData.ChainPfxPassword, keyStorageFlags))
            {
                X509Certificate2Collection certs = ic.Collection;
                int count = certs.Count;
                Assert.Equal(3, count);
            }
        }

        [Theory]
        [MemberData(nameof(StorageFlags))]
        public static void ImportPkcs12ByteSpan_Chain_SpanPassword(X509KeyStorageFlags keyStorageFlags)
        {
            ReadOnlySpan<byte> data = TestData.ChainPfxBytes.AsSpan();
            ReadOnlySpan<char> password = TestData.ChainPfxPassword.AsSpan();

            using (ImportedCollection ic = Cert.Import(data, password, keyStorageFlags))
            {
                X509Certificate2Collection certs = ic.Collection;
                int count = certs.Count;
                Assert.Equal(3, count);
            }
        }

        [Theory]
        [MemberData(nameof(StorageFlags))]
        public static void ImportPkcs12File_Chain(X509KeyStorageFlags keyStorageFlags)
        {
            using (ImportedCollection ic = Cert.Import(TestFiles.ChainPfxFile, TestData.ChainPfxPassword, keyStorageFlags))
            {
                X509Certificate2Collection certs = ic.Collection;
                int count = certs.Count;
                Assert.Equal(3, count);
            }
        }

        [Theory]
        [MemberData(nameof(StorageFlags))]
        public static void ImportPkcs12File_Chain_VerifyContents(X509KeyStorageFlags keyStorageFlags)
        {
            using (ImportedCollection ic = Cert.Import(TestFiles.ChainPfxFile, TestData.ChainPfxPassword, keyStorageFlags))
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

#if !NO_EPHEMERALKEYSET_AVAILABLE
        [Fact]
        public static void InvalidStorageFlags()
        {
            X509Certificate2Collection coll = new X509Certificate2Collection();
            byte[] nonEmptyBytes = new byte[1];

            AssertExtensions.Throws<ArgumentException>(
                "keyStorageFlags",
                () => coll.Import(nonEmptyBytes, string.Empty, (X509KeyStorageFlags)0xFF));

            AssertExtensions.Throws<ArgumentException>(
                "keyStorageFlags",
                () => coll.Import(string.Empty, string.Empty, (X509KeyStorageFlags)0xFF));

            // No test is performed here for the ephemeral flag failing downlevel, because the live
            // binary is always used by default, meaning it doesn't know EphemeralKeySet doesn't exist.
        }

        [Fact]
        public static void InvalidStorageFlags_PersistedEphemeral()
        {
            const X509KeyStorageFlags PersistedEphemeral =
                X509KeyStorageFlags.EphemeralKeySet | X509KeyStorageFlags.PersistKeySet;

            byte[] nonEmptyBytes = new byte[1];
            X509Certificate2Collection coll = new X509Certificate2Collection();

            AssertExtensions.Throws<ArgumentException>(
                "keyStorageFlags",
                () => coll.Import(nonEmptyBytes, string.Empty, PersistedEphemeral));

            AssertExtensions.Throws<ArgumentException>(
                "keyStorageFlags",
                () => coll.Import(string.Empty, string.Empty, PersistedEphemeral));
        }
#endif

        public static IEnumerable<object[]> StorageFlags
        {
            get
            {
                yield return new object[] { X509KeyStorageFlags.DefaultKeySet };

#if !NO_EPHEMERALKEYSET_AVAILABLE
                if (!OperatingSystem.IsMacOS())
                    yield return new object[] { X509KeyStorageFlags.EphemeralKeySet };
#endif
            }
        }
    }
}
