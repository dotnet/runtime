// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "Browser doesn't support X.509 certificates")]
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
            // If the test data only provides data from a file, don't check the content type
            // (it will be checked by the file variant).
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
            // All test data is either from a byte[] or a file.
            // If it comes from a byte[], it'll get verified by _FromByteArray;
            // likewise with a file and _FromFile.
            //
            // Since there are no uniquely span inputs, and not all the applicable TFMs have
            // a GetContentType(ReadOnlySpan), just always return false and skip the file
            // format sanity test in the _FromByteSpan variant.
            contentType = X509ContentType.Unknown;
            return false;
        }
    }

    [SkipOnPlatform(TestPlatforms.Browser, "Browser doesn't support X.509 certificates")]
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
            using (TempFileHolder holder = new TempFileHolder(bytes))
            {
                return LoadCertificate(bytes, holder.FilePath);
            }
        }

        protected override bool TryGetContentType(byte[] bytes, string path, out X509ContentType contentType)
        {
            // If the test data only provides data from a byte[], don't check the content type
            // (it will be checked by the byte array variant).
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
        public void LoadCertificate_WithTrailingData()
        {
            // Find the PEM-encoded certificate embedded within NestedCertificates, and
            // load only that portion of the data.
            byte[] data = TestData.NestedCertificates;

            // The offset could be hard-coded, but it's not expensive to do the find and saves on test maintenance.
            Span<byte> needle = stackalloc byte[] { 0x2D, 0x2D, 0x2D, 0x2D, 0x2D };
            int offset = data.AsSpan().IndexOf(needle);

#if NET
            // The macOS PEM loader seems to be rejecting the trailing data.
            if (OperatingSystem.IsMacOS())
            {
                Assert.ThrowsAny<CryptographicException>(() => LoadCertificateAtOffset(data, offset));
                return;
            }
#endif

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

        [Fact]
        public void LoadWrappingCertificate_PEM()
        {
            byte[] data = System.Text.Encoding.ASCII.GetBytes(
                ByteUtils.PemEncode("CERTIFICATE", TestData.NestedCertificates));

            using (X509Certificate2 cert = LoadCertificateNoFile(data))
            {
                AssertRawDataEquals(TestData.NestedCertificates, cert);
            }
        }

        [Fact]
        public void LoadWrappingCertificate_PEM_WithTrailingData()
        {
            byte[] source = TestData.NestedCertificates;
            Array.Resize(ref source, source.Length + 4);

            BinaryPrimitives.WriteInt32LittleEndian(
                source.AsSpan(TestData.NestedCertificates.Length),
                Process.GetCurrentProcess().Id);

            byte[] data = System.Text.Encoding.ASCII.GetBytes(
                ByteUtils.PemEncode("CERTIFICATE", source));

#if NET
            // OpenSSL is being more strict here than other platforms.
            if (OperatingSystem.IsLinux())
            {
                Assert.Throws<CryptographicException>(() => LoadCertificateNoFile(data));
                return;
            }
#endif

            using (X509Certificate2 cert = LoadCertificateNoFile(data))
            {
                AssertRawDataEquals(TestData.NestedCertificates, cert);
            }
        }

        [Fact]
        public void LoadWrappingCertificate_PEM_WithSurroundingText()
        {
            string pem = ByteUtils.PemEncode("CERTIFICATE", TestData.NestedCertificates);

            byte[] data = System.Text.Encoding.ASCII.GetBytes(
                "Four score and seven years ago ...\n" + pem + "\n... perish from this Earth.");

            using (X509Certificate2 cert = LoadCertificateNoFile(data))
            {
                AssertRawDataEquals(TestData.NestedCertificates, cert);
            }
        }

        internal static void AssertRawDataEquals(byte[] expected, X509Certificate2 cert)
        {
#if NET
            AssertExtensions.SequenceEqual(expected, cert.RawDataMemory.Span);
#else
            AssertExtensions.SequenceEqual(expected, cert.RawData);
#endif
        }

        internal static bool IsWindowsOnlyContentType(X509ContentType contentType)
        {
            return contentType is X509ContentType.Authenticode or X509ContentType.SerializedStore or X509ContentType.SerializedCert;
        }
    }
}
