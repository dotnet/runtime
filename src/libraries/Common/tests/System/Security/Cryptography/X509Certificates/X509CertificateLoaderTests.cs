// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO;
using System.IO.MemoryMappedFiles;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    public class X509CertificateLoaderTests_FromByteArray : X509CertificateLoaderTests
    {
        protected override void NullInputAssert(Action action) =>
            AssertExtensions.Throws<ArgumentNullException>("data", action);

        protected override void EmptyInputAssert(Action action) =>
            Assert.ThrowsAny<CryptographicException>(action);

        protected override X509Certificate2 LoadCertificate(byte[] bytes, string path) =>
            X509CertificateLoader.LoadCertificate(bytes);

        protected override X509Certificate2 LoadCertificateFileOnly(string path) =>
            X509CertificateLoader.LoadCertificate(File.ReadAllBytes(path));

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

    public class X509CertificateLoaderTests_FromByteSpan : X509CertificateLoaderTests
    {
        protected override void NullInputAssert(Action action) =>
            Assert.ThrowsAny<CryptographicException>(action);

        protected override void EmptyInputAssert(Action action) =>
            Assert.ThrowsAny<CryptographicException>(action);

        protected override X509Certificate2 LoadCertificate(byte[] bytes, string path) =>
            X509CertificateLoader.LoadCertificate(new ReadOnlySpan<byte>(bytes));

        protected override X509Certificate2 LoadCertificateAtOffset(byte[] bytes, int offset) =>
            X509CertificateLoader.LoadCertificate(bytes.AsSpan(offset));

        protected override X509Certificate2 LoadCertificateFileOnly(string path)
        {
            // Use a strategy other than File.ReadAllBytes.
            using (FileStream stream = File.OpenRead(path))
            using (MemoryManager<byte> manager = MemoryMappedFileMemoryManager.CreateFromFileClamped(stream))
            {
                return X509CertificateLoader.LoadCertificate(manager.Memory.Span);
            }
        }

        protected override bool TryGetContentType(byte[] bytes, string path, out X509ContentType contentType)
        {
            contentType = X509ContentType.Unknown;
            return false;
        }
    }

    public class X509CertificateLoaderTests_FromFile : X509CertificateLoaderTests
    {
        protected override void NullInputAssert(Action action) =>
            AssertExtensions.Throws<ArgumentNullException>("path", action);

        protected override void EmptyInputAssert(Action action) =>
            AssertExtensions.Throws<ArgumentException>("path", action);

        protected override X509Certificate2 LoadCertificate(byte[] bytes, string path) =>
            X509CertificateLoader.LoadCertificateFromFile(path);

        protected override X509Certificate2 LoadCertificateFileOnly(string path) =>
            X509CertificateLoader.LoadCertificateFromFile(path);

        protected override X509Certificate2 LoadCertificateNoFile(byte[] bytes)
        {
            string path = Path.GetTempFileName();

            try
            {
                File.WriteAllBytes(path, bytes);
                return LoadCertificate(bytes, path);
            }
            finally
            {
                File.Delete(path);
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

    public abstract class X509CertificateLoaderTests
    {
        protected abstract void NullInputAssert(Action action);
        protected abstract void EmptyInputAssert(Action action);
        protected abstract X509Certificate2 LoadCertificate(byte[] bytes, string path);
        protected abstract X509Certificate2 LoadCertificateFileOnly(string path);

        protected virtual X509Certificate2 LoadCertificateNoFile(byte[] bytes) =>
            LoadCertificate(bytes, null);

        protected virtual X509Certificate2 LoadCertificateAtOffset(byte[] bytes, int offset) =>
            LoadCertificateNoFile(bytes.AsSpan(offset).ToArray());

        protected abstract bool TryGetContentType(byte[] bytes, string path, out X509ContentType contentType);

        [Fact]
        public void LoadNull()
        {
            NullInputAssert(() => LoadCertificate(null, null));
        }

        [Fact]
        public void LoadEmpty()
        {
            EmptyInputAssert(() => LoadCertificate(Array.Empty<byte>(), string.Empty));
        }

        private void LoadKnownFormat_Fails(byte[] data, string path, X509ContentType contentType)
        {
            if (PlatformDetection.IsWindows || !IsWindowsOnlyContentType(contentType))
            {
                if (TryGetContentType(data, path, out X509ContentType actualType))
                {
                    Assert.Equal(contentType, actualType);
                }
            }
            
            if (path is null)
            {
                Assert.ThrowsAny<CryptographicException>(() => LoadCertificateNoFile(data));
            }
            else if (data is null)
            {
                Assert.ThrowsAny<CryptographicException>(() => LoadCertificateFileOnly(path));
            }
            else
            {
                Assert.ThrowsAny<CryptographicException>(() => LoadCertificate(data, path));
            }
        }

        [Fact]
        public void LoadCertificate_DER()
        {
            using (X509Certificate2 cert = LoadCertificate(TestData.MsCertificate, TestFiles.MsCertificateDerFile))
            {
                Assert.NotNull(cert);
                AssertRawDataEquals(TestData.MsCertificate, cert);
            }
        }

        [Fact]
        public void LoadCertificate_PEM()
        {
            using (X509Certificate2 cert = LoadCertificate(TestData.MsCertificatePemBytes, TestFiles.MsCertificatePemFile))
            {
                Assert.NotNull(cert);
                AssertRawDataEquals(TestData.MsCertificate, cert);
            }
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
        public void LoadPfx_NeedsPassword_Fails()
        {
            LoadKnownFormat_Fails(TestData.PfxData, TestFiles.PfxFile, X509ContentType.Pfx);
        }

        [Fact]
        public void LoadPfx_NoPasswordNeeded_Fails()
        {
            LoadKnownFormat_Fails(TestData.PfxWithNoPassword, null, X509ContentType.Pfx);
        }

        [Fact]
        public void LoadSignedFile_Fails()
        {
            LoadKnownFormat_Fails(null, TestFiles.SignedMsuFile, X509ContentType.Authenticode);
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
        public void LoadNestedCertificates()
        {
            using (X509Certificate2 cert = LoadCertificateNoFile(TestData.NestedCertificates))
            {
                Assert.Equal("CN=outer", cert.Subject);

                X509Extension ext = cert.Extensions["0.0.1"];

                using (X509Certificate2 inner = LoadCertificateNoFile(ext.RawData))
                {
                    Assert.Equal("CN=inner", inner.Subject);
                }
            }
        }

        [Fact]
        [ActiveIssue("macOS seems to not like the PEM post-EB not followed by a newline or EOF", TestPlatforms.OSX)]
        public void LoadCertificate_WithTrailingData()
        {
            // Find the PEM-encoded certificate embedded within NestedCertificates, and
            // load only that portion of the data.
            byte[] data = TestData.NestedCertificates;

            // The offset could be hard-coded, but it's not expensive to do the find and saves on test maintenance.
            Span<byte> needle = stackalloc byte[] { 0x2D, 0x2D, 0x2D, 0x2D, 0x2D };
            int offset = data.AsSpan().IndexOf(needle);

            using (X509Certificate2 cert = LoadCertificateAtOffset(data, offset))
            {
                Assert.Equal("CN=inner", cert.Subject);
            }
        }

        [Fact]
        public void LoadCertificate_DER_WithTrailingData()
        {
            byte[] data = TestData.MsCertificate;
            Array.Resize(ref data, data.Length + 21);

            using (X509Certificate2 cert = LoadCertificateNoFile(data))
            {
                AssertRawDataEquals(TestData.MsCertificate, cert);
            }
        }

        internal static void AssertRawDataEquals(byte[] expected, X509Certificate2 cert)
        {
#if NETCOREAPP
                AssertExtensions.SequenceEqual(TestData.MsCertificate, cert.RawDataMemory.Span);
#else
                AssertExtensions.SequenceEqual(TestData.MsCertificate, cert.RawData);
#endif
        }

        internal static bool IsWindowsOnlyContentType(X509ContentType contentType)
        {
            return contentType is X509ContentType.Authenticode or X509ContentType.SerializedStore or X509ContentType.SerializedCert;
        }
    }
}
