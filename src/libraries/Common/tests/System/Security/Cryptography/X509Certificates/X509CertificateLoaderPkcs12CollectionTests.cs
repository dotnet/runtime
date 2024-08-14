// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO;
using System.IO.MemoryMappedFiles;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "Browser doesn't support X.509 certificates")]
    public class X509CertificateLoaderPkcs12CollectionTests_FromByteArray : X509CertificateLoaderPkcs12CollectionTests
    {
        protected override void NullInputAssert(Action action) =>
            AssertExtensions.Throws<ArgumentNullException>("data", action);

        protected override void EmptyInputAssert(Action action) =>
            Assert.Throws<CryptographicException>(action);

        protected override X509Certificate2Collection LoadPfxCore(
            byte[] bytes,
            string path,
            string password,
            X509KeyStorageFlags keyStorageFlags,
            Pkcs12LoaderLimits loaderLimits)
        {
            return X509CertificateLoader.LoadPkcs12Collection(bytes, password, keyStorageFlags, loaderLimits);
        }

        protected override X509Certificate2Collection LoadPfxFileOnlyCore(
            string path,
            string password,
            X509KeyStorageFlags keyStorageFlags,
            Pkcs12LoaderLimits loaderLimits)
        {
            return X509CertificateLoader.LoadPkcs12Collection(
                File.ReadAllBytes(path),
                password,
                keyStorageFlags,
                loaderLimits);
        }

        protected override bool TryGetContentType(byte[] bytes, string path, out X509ContentType contentType)
        {
            if (bytes is null)
            {
                contentType = X509ContentType.Unknown;
                return false;
            }

            contentType = X509Certificate2.GetCertContentType(bytes);
            return true;
        }
    }

    [SkipOnPlatform(TestPlatforms.Browser, "Browser doesn't support X.509 certificates")]
    public class X509CertificateLoaderPkcs12CollectionTests_FromByteSpan : X509CertificateLoaderPkcs12CollectionTests
    {
        protected override void NullInputAssert(Action action) =>
            Assert.ThrowsAny<CryptographicException>(action);

        protected override void EmptyInputAssert(Action action) =>
            Assert.ThrowsAny<CryptographicException>(action);

        protected override X509Certificate2Collection LoadPfxCore(
            byte[] bytes,
            string path,
            string password,
            X509KeyStorageFlags keyStorageFlags,
            Pkcs12LoaderLimits loaderLimits)
        {
            return X509CertificateLoader.LoadPkcs12Collection(
                new ReadOnlySpan<byte>(bytes),
                password.AsSpan(),
                keyStorageFlags,
                loaderLimits);
        }

        protected override X509Certificate2Collection LoadPfxAtOffsetCore(
            byte[] bytes,
            int offset,
            string password,
            X509KeyStorageFlags keyStorageFlags,
            Pkcs12LoaderLimits loaderLimits)
        {
            return X509CertificateLoader.LoadPkcs12Collection(
                bytes.AsSpan(offset),
                password.AsSpan(),
                keyStorageFlags,
                loaderLimits);
        }

        protected override X509Certificate2Collection LoadPfxFileOnlyCore(
            string path,
            string password,
            X509KeyStorageFlags keyStorageFlags,
            Pkcs12LoaderLimits loaderLimits)
        {
            // Use a strategy other than File.ReadAllBytes.

            using (FileStream stream = File.OpenRead(path))
            using (MemoryManager<byte> manager = MemoryMappedFileMemoryManager.CreateFromFileClamped(stream))
            {
                return X509CertificateLoader.LoadPkcs12Collection(
                    manager.Memory.Span,
                    password.AsSpan(),
                    keyStorageFlags,
                    loaderLimits);
            }
        }

        protected override bool TryGetContentType(byte[] bytes, string path, out X509ContentType contentType)
        {
            contentType = X509ContentType.Unknown;
            return false;
        }
    }

    [SkipOnPlatform(TestPlatforms.Browser, "Browser doesn't support X.509 certificates")]
    public class X509CertificateLoaderPkcs12CollectionTests_FromFile : X509CertificateLoaderPkcs12CollectionTests
    {
        protected override void NullInputAssert(Action action) =>
            AssertExtensions.Throws<ArgumentNullException>("path", action);

        protected override void EmptyInputAssert(Action action) =>
            AssertExtensions.Throws<ArgumentException>("path", action);

        protected override X509Certificate2Collection LoadPfxCore(
            byte[] bytes,
            string path,
            string password,
            X509KeyStorageFlags keyStorageFlags,
            Pkcs12LoaderLimits loaderLimits)
        {
            return X509CertificateLoader.LoadPkcs12CollectionFromFile(path, password, keyStorageFlags, loaderLimits);
        }

        protected override X509Certificate2Collection LoadPfxFileOnlyCore(
            string path,
            string password,
            X509KeyStorageFlags keyStorageFlags,
            Pkcs12LoaderLimits loaderLimits)
        {
            return X509CertificateLoader.LoadPkcs12CollectionFromFile(path, password, keyStorageFlags, loaderLimits);
        }

        protected override X509Certificate2Collection LoadPfxNoFileCore(
            byte[] bytes,
            string password,
            X509KeyStorageFlags keyStorageFlags,
            Pkcs12LoaderLimits loaderLimits)
        {
            using (TempFileHolder holder = new TempFileHolder(bytes))
            {
                return LoadPfx(bytes, holder.FilePath, password, keyStorageFlags, loaderLimits);
            }
        }

        protected override bool TryGetContentType(byte[] bytes, string path, out X509ContentType contentType)
        {
            if (path is null)
            {
                contentType = X509ContentType.Unknown;
                return false;
            }

            contentType = X509Certificate2.GetCertContentType(path);
            return true;
        }
    }

    public abstract partial class X509CertificateLoaderPkcs12CollectionTests
    {
        private const int ERROR_INVALID_PASSWORD = -2147024810;

        protected static readonly X509KeyStorageFlags EphemeralIfPossible =
#if NETFRAMEWORK
            X509KeyStorageFlags.DefaultKeySet;
#else
           PlatformDetection.UsesAppleCrypto ? 
                X509KeyStorageFlags.DefaultKeySet :
                X509KeyStorageFlags.EphemeralKeySet;
#endif

        protected abstract void NullInputAssert(Action action);
        protected abstract void EmptyInputAssert(Action action);

        protected X509Certificate2Collection LoadPfx(
            byte[] bytes,
            string path,
            string password = "",
            X509KeyStorageFlags? keyStorageFlags = null,
            Pkcs12LoaderLimits loaderLimits = null)
        {
            return LoadPfxCore(
                bytes,
                path,
                password,
                keyStorageFlags.GetValueOrDefault(EphemeralIfPossible),
                loaderLimits);
        }

        protected abstract X509Certificate2Collection LoadPfxCore(
            byte[] bytes,
            string path,
            string password,
            X509KeyStorageFlags keyStorageFlags,
            Pkcs12LoaderLimits loaderLimits);

        protected X509Certificate2Collection LoadPfxFileOnly(
            string path,
            string password = "",
            X509KeyStorageFlags? keyStorageFlags = null,
            Pkcs12LoaderLimits loaderLimits = null)
        {
            return LoadPfxFileOnlyCore(
                path,
                password,
                keyStorageFlags.GetValueOrDefault(EphemeralIfPossible),
                loaderLimits);
        }

        protected abstract X509Certificate2Collection LoadPfxFileOnlyCore(
            string path,
            string password,
            X509KeyStorageFlags keyStorageFlags,
            Pkcs12LoaderLimits loaderLimits);

        protected X509Certificate2Collection LoadPfxNoFile(
            byte[] bytes,
            string password = "",
            X509KeyStorageFlags? keyStorageFlags = null,
            Pkcs12LoaderLimits loaderLimits = null)
        {
            return LoadPfxNoFileCore(
                bytes,
                password,
                keyStorageFlags.GetValueOrDefault(EphemeralIfPossible),
                loaderLimits);
        }

        protected virtual X509Certificate2Collection LoadPfxNoFileCore(
            byte[] bytes,
            string password,
            X509KeyStorageFlags keyStorageFlags,
            Pkcs12LoaderLimits loaderLimits)
        {
            return LoadPfx(bytes, null, password, keyStorageFlags, loaderLimits);
        }

        protected X509Certificate2Collection LoadPfxAtOffset(
            byte[] bytes,
            int offset,
            string password = "",
            X509KeyStorageFlags? keyStorageFlags = null,
            Pkcs12LoaderLimits loaderLimits = null)
        {
            return LoadPfxAtOffsetCore(
                bytes,
                offset,
                password,
                keyStorageFlags.GetValueOrDefault(EphemeralIfPossible),
                loaderLimits);
        }

        protected virtual X509Certificate2Collection LoadPfxAtOffsetCore(
            byte[] bytes,
            int offset,
            string password,
            X509KeyStorageFlags keyStorageFlags,
            Pkcs12LoaderLimits loaderLimits)
        {
            return LoadPfxNoFile(
                bytes.AsSpan(offset).ToArray(),
                password,
                keyStorageFlags,
                loaderLimits);
        }

        protected abstract bool TryGetContentType(byte[] bytes, string path, out X509ContentType contentType);

        [Fact]
        public void LoadNull()
        {
            NullInputAssert(() => LoadPfx(null, null, null));
        }

        [Fact]
        public void LoadEmpty()
        {
            EmptyInputAssert(() => LoadPfx(Array.Empty<byte>(), string.Empty));
        }

        private void LoadKnownFormat_Fails(byte[] data, string path, X509ContentType contentType)
        {
            if (PlatformDetection.IsWindows || !X509CertificateLoaderTests.IsWindowsOnlyContentType(contentType))
            {
                if (TryGetContentType(data, path, out X509ContentType actualType))
                {
                    Assert.Equal(contentType, actualType);
                }
            }
            
            if (path is null)
            {
                Assert.ThrowsAny<CryptographicException>(() => LoadPfxNoFile(data));
            }
            else if (data is null)
            {
                Assert.ThrowsAny<CryptographicException>(() => LoadPfxFileOnly(path));
            }
            else
            {
                Assert.ThrowsAny<CryptographicException>(() => LoadPfx(data, path));
            }
        }

        [Fact]
        public void LoadCertificate_DER_Fails()
        {
            LoadKnownFormat_Fails(TestData.MsCertificate, TestFiles.MsCertificateDerFile, X509ContentType.Cert);
        }

        [Fact]
        public void LoadCertificate_PEM_Fails()
        {
            LoadKnownFormat_Fails(TestData.MsCertificatePemBytes, TestFiles.MsCertificatePemFile, X509ContentType.Cert);
        }

        [Fact]
        public void LoadPkcs7_BER_Fails()
        {
            LoadKnownFormat_Fails(TestData.Pkcs7ChainDerBytes, TestFiles.Pkcs7ChainDerFile, X509ContentType.Pkcs7);
        }

        [Fact]
        public void LoadPkcs7_PEM_Fails()
        {
            LoadKnownFormat_Fails(TestData.Pkcs7ChainPemBytes, TestFiles.Pkcs7ChainPemFile, X509ContentType.Pkcs7);
        }

        [Fact]
        public void LoadSerializedCert_Fails()
        {
            LoadKnownFormat_Fails(TestData.StoreSavedAsSerializedCerData, null, X509ContentType.SerializedCert);
        }

        [Fact]
        public void LoadSerializedStore_Fails()
        {
            LoadKnownFormat_Fails(TestData.StoreSavedAsSerializedStoreData, null, X509ContentType.SerializedStore);
        }

        [Fact]
        public void LoadSignedFile_Fails()
        {
            LoadKnownFormat_Fails(null, TestFiles.SignedMsuFile, X509ContentType.Authenticode);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void LoadPfx_Single_WithPassword(bool ignorePrivateKeys)
        {
            Pkcs12LoaderLimits loaderLimits = new Pkcs12LoaderLimits
            {
                IgnorePrivateKeys = ignorePrivateKeys,
            };

            X509Certificate2Collection coll = LoadPfx(
                TestData.PfxData,
                TestFiles.PfxFile,
                TestData.PfxDataPassword,
                EphemeralIfPossible,
                loaderLimits);

            using (new CollectionDisposer(coll))
            {
                Assert.Equal(1, coll.Count);

                X509Certificate2 cert = coll[0];
                Assert.Equal("CN=MyName", cert.Subject);
                Assert.NotEqual(ignorePrivateKeys, cert.HasPrivateKey);
            }
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void LoadPfx_Single_NoPassword(bool ignorePrivateKeys, bool useNull)
        {
            Pkcs12LoaderLimits loaderLimits = new Pkcs12LoaderLimits
            {
                IgnorePrivateKeys = ignorePrivateKeys,
            };

            string password = useNull ? null : "";

            X509Certificate2Collection coll = LoadPfxNoFile(
                TestData.PfxWithNoPassword,
                password,
                EphemeralIfPossible,
                loaderLimits);

            using (new CollectionDisposer(coll))
            {
                Assert.Equal(1, coll.Count);

                X509Certificate2 cert = coll[0];
                Assert.Equal("CN=MyName", cert.Subject);
                Assert.NotEqual(ignorePrivateKeys, cert.HasPrivateKey);
            }
        }

        [ConditionalTheory(typeof(PlatformSupport), nameof(PlatformSupport.IsRC2Supported))]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void LoadPfx_Single_NoPassword_AmbiguousDecrypt(bool ignorePrivateKeys, bool useNull)
        {
            Pkcs12LoaderLimits loaderLimits = new Pkcs12LoaderLimits
            {
                IgnorePrivateKeys = ignorePrivateKeys,
            };

            string password = useNull ? null : "";

            X509Certificate2Collection coll = LoadPfxNoFile(
                TestData.MsCertificateExportedToPfx_NullPassword,
                password,
                EphemeralIfPossible,
                loaderLimits);

            using (new CollectionDisposer(coll))
            {
                Assert.Equal(1, coll.Count);

                X509Certificate2 cert = coll[0];
                X509CertificateLoaderTests.AssertRawDataEquals(TestData.MsCertificate, cert);
                Assert.False(cert.HasPrivateKey, "cert.HasPrivateKey");
            }
        }

        [Fact]
        public void LoadPfx_Single_WrongPassword()
        {
            CryptographicException ex = Assert.Throws<CryptographicException>(
                () => LoadPfx(TestData.PfxData, TestFiles.PfxFile, "asdf"));

            Assert.Contains("password", ex.Message);
            Assert.Equal(ERROR_INVALID_PASSWORD, ex.HResult);
        }

        [Fact]
        public void LoadPfx_Single_EmptyPassword_WithWrongPassword()
        {
            CryptographicException ex = Assert.Throws<CryptographicException>(
                () => LoadPfxNoFile(TestData.PfxWithNoPassword, "asdf"));

            Assert.Contains("password", ex.Message);
            Assert.Equal(ERROR_INVALID_PASSWORD, ex.HResult);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void LoadPfx_Single_EmptyPassword_NoMac(bool useEmpty)
        {
            string password = useEmpty ? "" : null;

            X509Certificate2Collection coll = LoadPfxNoFile(
                TestData.Pkcs12OpenSslOneCertDefaultNoMac,
                password,
                EphemeralIfPossible);

            using (new CollectionDisposer(coll))
            {
                Assert.Equal(1, coll.Count);

                X509Certificate2 cert = coll[0];
                Assert.Equal("CN=test", cert.Subject);
            }
        }

        [Fact]
        public void LoadPfx_WithTrailingData()
        {
            byte[] data = TestData.PfxWithNoPassword;
            Array.Resize(ref data, data.Length + 10);

            X509Certificate2Collection coll = LoadPfxNoFile(data);

            using (new CollectionDisposer(coll))
            {
                Assert.Equal(1, coll.Count);

                X509Certificate2 cert = coll[0];
                Assert.Equal("CN=MyName", cert.Subject);
            }
        }

        [Fact]
        public void LoadPfx_Empty()
        {
            X509Certificate2Collection coll = LoadPfxNoFile(TestData.EmptyPfx);

            using (new CollectionDisposer(coll))
            {
                Assert.Equal(0, coll.Count);
            }
        }

        private void LoadPfx_VerifyLimit(
            string propertyTested,
            bool fail,
            byte[] bytes,
            string path,
            string password,
            Pkcs12LoaderLimits loaderLimits)
        {
            Func<X509Certificate2Collection> test;

            if (bytes is null)
            {
                test = () => LoadPfxFileOnly(path, password, EphemeralIfPossible, loaderLimits);
            }
            else if (path is null)
            {
                test = () => LoadPfxNoFile(bytes, password, EphemeralIfPossible, loaderLimits);
            }
            else
            {
                test = () => LoadPfx(bytes, path, password, EphemeralIfPossible, loaderLimits);
            }

            if (fail)
            {
                Pkcs12LoadLimitExceededException ex =
                    AssertExtensions.Throws<Pkcs12LoadLimitExceededException>(() => test());

                Assert.Contains(propertyTested, ex.Message);
            }
            else
            {
                // Assert.NoThrow
                (new CollectionDisposer(test())).Dispose();
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void LoadPfx_VerifyMacIterationLimit(bool failLimit)
        {
            Pkcs12LoaderLimits loaderLimits = new Pkcs12LoaderLimits
            {
                MacIterationLimit = failLimit ? 1999 : 2000,
            };

            LoadPfx_VerifyLimit(
                nameof(Pkcs12LoaderLimits.MacIterationLimit),
                failLimit,
                TestData.PfxData,
                TestFiles.PfxFile,
                TestData.PfxDataPassword,
                loaderLimits);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void LoadPfx_VerifyKdfIterationLimit(bool failLimit)
        {
            Pkcs12LoaderLimits loaderLimits = new Pkcs12LoaderLimits
            {
                IndividualKdfIterationLimit = failLimit ? 1999 : 2000,
            };

            // Both 1999 and 2000 will fail, because the key uses 2001.
            LoadPfx_VerifyLimit(
                nameof(Pkcs12LoaderLimits.IndividualKdfIterationLimit),
                fail: true,
                TestData.MixedIterationsPfx,
                null,
                TestData.PlaceholderPw,
                loaderLimits);

            loaderLimits.IgnorePrivateKeys = true;

            // Now that we're ignoring the key, 1999 will fail, 2000 will pass.
            LoadPfx_VerifyLimit(
                nameof(Pkcs12LoaderLimits.IndividualKdfIterationLimit),
                failLimit,
                TestData.MixedIterationsPfx,
                null,
                TestData.PlaceholderPw,
                loaderLimits);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void LoadPfx_VerifyTotalKdfIterationLimit(bool failLimit)
        {
            Pkcs12LoaderLimits loaderLimits = new Pkcs12LoaderLimits
            {
                TotalKdfIterationLimit = failLimit ? 3999 : 4000,
            };

            LoadPfx_VerifyLimit(
                nameof(Pkcs12LoaderLimits.TotalKdfIterationLimit),
                failLimit,
                TestData.PfxData,
                TestFiles.PfxFile,
                TestData.PfxDataPassword,
                loaderLimits);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        public void LoadPfx_VerifyCertificateLimit(int? certLimit)
        {
            Pkcs12LoaderLimits loaderLimits = new Pkcs12LoaderLimits
            {
                MaxCertificates = certLimit,
            };

            bool expectFailure = certLimit.GetValueOrDefault(int.MaxValue) < 3;

            LoadPfx_VerifyLimit(
                nameof(Pkcs12LoaderLimits.MaxCertificates),
                expectFailure,
                TestData.ChainPfxBytes,
                TestFiles.ChainPfxFile,
                TestData.ChainPfxPassword,
                loaderLimits);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(4)]
        public void LoadPfx_VerifyKeysLimit(int? keysLimit)
        {
            Pkcs12LoaderLimits loaderLimits = new Pkcs12LoaderLimits
            {
                MaxKeys = keysLimit,
            };

            bool expectFailure = keysLimit.GetValueOrDefault(int.MaxValue) < 1;

            LoadPfx_VerifyLimit(
                nameof(Pkcs12LoaderLimits.MaxKeys),
                expectFailure,
                TestData.ChainPfxBytes,
                TestFiles.ChainPfxFile,
                TestData.ChainPfxPassword,
                loaderLimits);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void LoadPfx_VerifyIgnoreEncryptedSafes(bool ignoreEncrypted)
        {
            Pkcs12LoaderLimits loaderLimits = new Pkcs12LoaderLimits
            {
                IgnoreEncryptedAuthSafes = ignoreEncrypted,
            };

            const string PlaintextSubject =
                "CN=Plaintext Test Certificate, OU=.NET Libraries, O=Microsoft Corporation";
            const string EncryptedSubject =
                "CN=Encrypted Test Certificate, OU=.NET Libraries, O=Microsoft Corporation";

            X509Certificate2Collection coll = LoadPfxNoFile(
                TestData.TwoCertsPfx_OneEncrypted,
                TestData.PlaceholderPw,
                default,
                loaderLimits);

            using (new CollectionDisposer(coll))
            {
                if (ignoreEncrypted)
                {
                    Assert.Equal(1, coll.Count);

                    X509Certificate2 cert = coll[0];
                    Assert.Equal(PlaintextSubject, cert.Subject);
                }
                else
                {
                    Assert.Equal(2, coll.Count);

                    X509Certificate2 cert = coll[0];
                    Assert.Equal(EncryptedSubject, cert.Subject);

                    cert = coll[1];
                    Assert.Equal(PlaintextSubject, cert.Subject);
                }
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void LoadPfx_VerifyIgnoreEncryptedSafes_EmptyIfIgnored(bool ignoreEncrypted)
        {
            Pkcs12LoaderLimits loaderLimits = new Pkcs12LoaderLimits
            {
                IgnoreEncryptedAuthSafes = ignoreEncrypted,
            };

            X509Certificate2Collection coll = LoadPfx(
                TestData.PfxData,
                TestFiles.PfxFile,
                TestData.PfxDataPassword,
                default,
                loaderLimits);

            using (new CollectionDisposer(coll))
            {
                if (ignoreEncrypted)
                {
                    Assert.Equal(0, coll.Count);
                }
                else
                {
                    Assert.Equal(1, coll.Count);

                    X509Certificate2 cert = coll[0];
                    Assert.Equal("CN=MyName", cert.Subject);
                }
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void LoadWithDuplicateAttributes(bool allowDuplicates)
        {
            Pkcs12LoaderLimits limits = Pkcs12LoaderLimits.Defaults;

            if (allowDuplicates)
            {
                limits = Pkcs12LoaderLimits.DangerousNoLimits;
            }

            // remove the edit lock
            limits = new Pkcs12LoaderLimits(limits)
            {
                PreserveCertificateAlias = false,
                PreserveKeyName = false,
                PreserveStorageProvider = false,
                PreserveUnknownAttributes = false,
            };

            Func<X509Certificate2Collection> func =
                () => LoadPfxNoFile(TestData.DuplicateAttributesPfx, TestData.PlaceholderPw, loaderLimits: limits);

            if (allowDuplicates)
            {
                X509Certificate2Collection coll = func();

                using (new CollectionDisposer(coll))
                {
                    Assert.Equal(1, coll.Count);
                    X509Certificate2 cert = coll[0];

                    Assert.Equal("Certificate 1", cert.GetNameInfo(X509NameType.SimpleName, false));
                    Assert.True(cert.HasPrivateKey, "cert.HasPrivateKey");
                }
            }
            else
            {
                Pkcs12LoadLimitExceededException ex = Assert.Throws<Pkcs12LoadLimitExceededException>(() => func());
                Assert.Contains("AllowDuplicateAttributes", ex.Message);
            }
        }

        private sealed class CollectionDisposer : IDisposable
        {
            private readonly X509Certificate2Collection _coll;

            internal CollectionDisposer(X509Certificate2Collection coll)
            {
                _coll = coll;
            }

            public void Dispose()
            {
                foreach (X509Certificate2 cert in _coll)
                {
                    cert.Dispose();
                }
            }
        }
    }
}
