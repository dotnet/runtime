// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
    public class RSATests
    {
        [Fact]
        public void BaseVirtualsNotImplementedException()
        {
            var rsa = new EmptyRSA();
            Assert.Throws<NotImplementedException>(() => rsa.Decrypt(null, null));
            Assert.Throws<NotImplementedException>(() => rsa.Encrypt(null, null));
            Assert.Throws<NotImplementedException>(() => rsa.SignHash(null, HashAlgorithmName.SHA256, null));
            Assert.Throws<NotImplementedException>(() => rsa.VerifyHash(null, null, HashAlgorithmName.SHA256, null));
        }

        [Fact]
        public void TryDecrypt_UsesDecrypt()
        {
            var rsa = new DelegateRSA { DecryptDelegate = (data, padding) => data };
            int bytesWritten;
            byte[] actual, expected;

            Assert.False(rsa.TryDecrypt(new byte[3], new byte[2], RSAEncryptionPadding.OaepSHA1, out bytesWritten));
            Assert.Equal(0, bytesWritten);

            expected = new byte[2] { 42, 43 };
            actual = new byte[2];
            Assert.True(rsa.TryDecrypt(expected, actual, RSAEncryptionPadding.OaepSHA1, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(42, actual[0]);
            Assert.Equal(43, actual[1]);

            actual = new byte[3];
            Assert.True(rsa.TryDecrypt(expected, actual, RSAEncryptionPadding.OaepSHA1, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(42, actual[0]);
            Assert.Equal(43, actual[1]);
            Assert.Equal(0, actual[2]);
        }

        [Fact]
        public void TryEncrypt_UsesEncrypt()
        {
            var rsa = new DelegateRSA { EncryptDelegate = (data, padding) => data };
            int bytesWritten;
            byte[] actual, expected;

            Assert.False(rsa.TryEncrypt(new byte[3], new byte[2], RSAEncryptionPadding.OaepSHA1, out bytesWritten));
            Assert.Equal(0, bytesWritten);

            expected = new byte[2] { 42, 43 };
            actual = new byte[2];
            Assert.True(rsa.TryEncrypt(expected, actual, RSAEncryptionPadding.OaepSHA1, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(42, actual[0]);
            Assert.Equal(43, actual[1]);

            actual = new byte[3];
            Assert.True(rsa.TryEncrypt(expected, actual, RSAEncryptionPadding.OaepSHA1, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(42, actual[0]);
            Assert.Equal(43, actual[1]);
            Assert.Equal(0, actual[2]);
        }

        [Fact]
        public void TryHashData_UsesHashData()
        {
            var rsa = new DelegateRSA { HashDataArrayDelegate = (data, offset, count, name) => new Span<byte>(data, offset, count).ToArray() };
            int bytesWritten;
            byte[] actual, expected;

            Assert.False(rsa.TryHashData(new byte[3], new byte[2], HashAlgorithmName.SHA256, out bytesWritten));
            Assert.Equal(0, bytesWritten);

            expected = new byte[2] { 42, 43 };
            actual = new byte[2];
            Assert.True(rsa.TryHashData(expected, actual, HashAlgorithmName.SHA256, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(42, actual[0]);
            Assert.Equal(43, actual[1]);

            actual = new byte[3];
            Assert.True(rsa.TryHashData(expected, actual, HashAlgorithmName.SHA256, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(42, actual[0]);
            Assert.Equal(43, actual[1]);
            Assert.Equal(0, actual[2]);
        }

        [Fact]
        public void TrySignHash_UsesSignHash()
        {
            var rsa = new DelegateRSA { SignHashDelegate = (data, name, padding) => data };
            int bytesWritten;
            byte[] actual, expected;

            Assert.False(rsa.TrySignHash(new byte[3], new byte[2], HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1, out bytesWritten));
            Assert.Equal(0, bytesWritten);

            expected = new byte[2] { 42, 43 };
            actual = new byte[2];
            Assert.True(rsa.TrySignHash(expected, actual, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(42, actual[0]);
            Assert.Equal(43, actual[1]);

            actual = new byte[3];
            Assert.True(rsa.TrySignHash(expected, actual, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1, out bytesWritten));
            Assert.Equal(2, bytesWritten);
            Assert.Equal(42, actual[0]);
            Assert.Equal(43, actual[1]);
            Assert.Equal(0, actual[2]);
        }

        [Fact]
        public void VerifyHashSpan_UsesVerifyHashArray()
        {
            bool invoked = false;
            var rsa = new DelegateRSA { VerifyHashDelegate = delegate { invoked = true; return true; } };
            Assert.True(rsa.VerifyHash(ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty, HashAlgorithmName.SHA256, RSASignaturePadding.Pss));
            Assert.True(invoked);
        }

        [Fact]
        public void SignDataArray_UsesHashDataAndSignHash()
        {
            var rsa = new DelegateRSA();

            AssertExtensions.Throws<ArgumentNullException>("data", () => rsa.SignData((byte[])null, HashAlgorithmName.SHA256, null));
            AssertExtensions.Throws<ArgumentNullException>("data", () => rsa.SignData(null, 0, 0, HashAlgorithmName.SHA256, null));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => rsa.SignData(new byte[1], -1, 0, HashAlgorithmName.SHA256, null));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => rsa.SignData(new byte[1], 2, 0, HashAlgorithmName.SHA256, null));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => rsa.SignData(new byte[1], 0, -1, HashAlgorithmName.SHA256, null));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => rsa.SignData(new byte[1], 0, 2, HashAlgorithmName.SHA256, null));

            AssertExtensions.Throws<ArgumentNullException>("hashAlgorithm", () => rsa.SignData(new byte[1], 0, 1, new HashAlgorithmName(null), null));
            AssertExtensions.Throws<ArgumentException>("hashAlgorithm", () => rsa.SignData(new byte[1], 0, 1, new HashAlgorithmName(""), null));

            AssertExtensions.Throws<ArgumentNullException>("padding", () => rsa.SignData(new byte[1], 0, 1, new HashAlgorithmName("abc"), null));

            rsa.HashDataArrayDelegate = (data, offset, count, name) => new Span<byte>(data, offset, count).ToArray();
            rsa.SignHashDelegate = (data, name, padding) => data.Select(b => (byte)(b * 2)).ToArray();
            Assert.Equal<byte>(new byte[] { 6, 8, 10, 12, 14, 16 }, rsa.SignData(new byte[] { 3, 4, 5, 6, 7, 8 }, HashAlgorithmName.SHA256, RSASignaturePadding.Pss));
            Assert.Equal<byte>(new byte[] { 10, 12, 14 }, rsa.SignData(new byte[] { 3, 4, 5, 6, 7, 8 }, 2, 3, HashAlgorithmName.SHA256, RSASignaturePadding.Pss));
        }

        [Fact]
        public void SignDataStream_UsesHashDataAndSignHash()
        {
            var rsa = new DelegateRSA();

            AssertExtensions.Throws<ArgumentNullException>("data", () => rsa.SignData((Stream)null, HashAlgorithmName.SHA256, null));

            AssertExtensions.Throws<ArgumentNullException>("hashAlgorithm", () => rsa.SignData(Stream.Null, new HashAlgorithmName(null), null));
            AssertExtensions.Throws<ArgumentException>("hashAlgorithm", () => rsa.SignData(Stream.Null, new HashAlgorithmName(""), null));

            AssertExtensions.Throws<ArgumentNullException>("padding", () => rsa.SignData(Stream.Null, new HashAlgorithmName("abc"), null));

            rsa.HashDataStreamDelegate = (stream, name) => ((MemoryStream)stream).ToArray();
            rsa.SignHashDelegate = (data, name, padding) => data.Select(b => (byte)(b * 2)).ToArray();
            Assert.Equal<byte>(new byte[] { 6, 8, 10, 12, 14, 16 }, rsa.SignData(new MemoryStream(new byte[] { 3, 4, 5, 6, 7, 8 }), HashAlgorithmName.SHA256, RSASignaturePadding.Pss));
        }

        [Fact]
        public void VerifyDataStream_UsesHashDataAndVerifyHash()
        {
            var rsa = new DelegateRSA();

            AssertExtensions.Throws<ArgumentNullException>("data", () => rsa.VerifyData((Stream)null, null, HashAlgorithmName.SHA256, null));

            AssertExtensions.Throws<ArgumentNullException>("signature", () => rsa.VerifyData(Stream.Null, null, HashAlgorithmName.SHA256, null));

            AssertExtensions.Throws<ArgumentNullException>("hashAlgorithm", () => rsa.VerifyData(Stream.Null, new byte[1], new HashAlgorithmName(null), null));
            AssertExtensions.Throws<ArgumentException>("hashAlgorithm", () => rsa.VerifyData(Stream.Null, new byte[1], new HashAlgorithmName(""), null));

            AssertExtensions.Throws<ArgumentNullException>("padding", () => rsa.VerifyData(Stream.Null, new byte[1], new HashAlgorithmName("abc"), null));

            rsa.HashDataStreamDelegate = (stream, name) => ((MemoryStream)stream).ToArray();
            rsa.VerifyHashDelegate = (hash, signature, name, padding) => hash[0] == 42;
            Assert.True(rsa.VerifyData(new MemoryStream(new byte[] { 42 }), new byte[1] { 24 }, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
        }

        [Fact]
        public void RSAEncryptionPadding_Equality()
        {
            Assert.True(RSAEncryptionPadding.Pkcs1.Equals(RSAEncryptionPadding.Pkcs1));
            Assert.True(RSAEncryptionPadding.Pkcs1.Equals((object)RSAEncryptionPadding.Pkcs1));
            Assert.True(RSAEncryptionPadding.Pkcs1 == RSAEncryptionPadding.Pkcs1);
            Assert.False(RSAEncryptionPadding.Pkcs1 != RSAEncryptionPadding.Pkcs1);

            Assert.False(RSAEncryptionPadding.Pkcs1.Equals(RSAEncryptionPadding.OaepSHA1));
            Assert.False(RSAEncryptionPadding.Pkcs1.Equals((object)RSAEncryptionPadding.OaepSHA1));
            Assert.False(RSAEncryptionPadding.Pkcs1 == RSAEncryptionPadding.OaepSHA1);
            Assert.True(RSAEncryptionPadding.Pkcs1 != RSAEncryptionPadding.OaepSHA1);

            Assert.False(RSAEncryptionPadding.Pkcs1.Equals(null));
            Assert.False(RSAEncryptionPadding.Pkcs1.Equals((object)null));
            Assert.False(RSAEncryptionPadding.Pkcs1 == null);
            Assert.True(RSAEncryptionPadding.Pkcs1 != null);
        }

        [Fact]
        public void RSASignaturePadding_Equality()
        {
            Assert.True(RSASignaturePadding.Pkcs1.Equals(RSASignaturePadding.Pkcs1));
            Assert.True(RSASignaturePadding.Pkcs1.Equals((object)RSASignaturePadding.Pkcs1));
            Assert.True(RSASignaturePadding.Pkcs1 == RSASignaturePadding.Pkcs1);
            Assert.False(RSASignaturePadding.Pkcs1 != RSASignaturePadding.Pkcs1);

            Assert.False(RSASignaturePadding.Pkcs1.Equals(RSASignaturePadding.Pss));
            Assert.False(RSASignaturePadding.Pkcs1.Equals((object)RSASignaturePadding.Pss));
            Assert.False(RSASignaturePadding.Pkcs1 == RSASignaturePadding.Pss);
            Assert.True(RSASignaturePadding.Pkcs1 != RSASignaturePadding.Pss);

            Assert.False(RSASignaturePadding.Pkcs1.Equals(null));
            Assert.False(RSASignaturePadding.Pkcs1.Equals((object)null));
            Assert.False(RSASignaturePadding.Pkcs1 == null);
            Assert.True(RSASignaturePadding.Pkcs1 != null);
        }

        [Fact]
        public static void ExportPem_ExportRSAPublicKey()
        {
            string expectedPem =
                "-----BEGIN RSA PUBLIC KEY-----\n" +
                "cGVubnk=\n" +
                "-----END RSA PUBLIC KEY-----";

            static byte[] ExportRSAPublicKey()
            {
                return new byte[] { 0x70, 0x65, 0x6e, 0x6e, 0x79 };
            }

            using (DelegateRSA rsa = new DelegateRSA())
            {
                rsa.ExportRSAPublicKeyDelegate = ExportRSAPublicKey;
                Assert.Equal(expectedPem, rsa.ExportRSAPublicKeyPem());
            }
        }

        [Fact]
        public static void ExportPem_TryExportRSAPublicKey()
        {
            string expectedPem =
                "-----BEGIN RSA PUBLIC KEY-----\n" +
                "cGVubnk=\n" +
                "-----END RSA PUBLIC KEY-----";

            static bool TryExportRSAPublicKey(Span<byte> destination, out int bytesWritten)
            {
                ReadOnlySpan<byte> result = new byte[] { 0x70, 0x65, 0x6e, 0x6e, 0x79 };
                bytesWritten = result.Length;
                result.CopyTo(destination);
                return true;
            }

            using (DelegateRSA rsa = new DelegateRSA())
            {
                rsa.TryExportRSAPublicKeyDelegate = TryExportRSAPublicKey;

                int written;
                bool result;
                char[] buffer;

                // buffer not enough
                buffer = new char[expectedPem.Length - 1];
                result = rsa.TryExportRSAPublicKeyPem(buffer, out written);
                Assert.False(result, nameof(rsa.TryExportRSAPublicKeyPem));
                Assert.Equal(0, written);

                // buffer just enough
                buffer = new char[expectedPem.Length];
                result = rsa.TryExportRSAPublicKeyPem(buffer, out written);
                Assert.True(result, nameof(rsa.TryExportRSAPublicKeyPem));
                Assert.Equal(expectedPem.Length, written);
                Assert.Equal(expectedPem, new string(buffer));

                // buffer more than enough
                buffer = new char[expectedPem.Length + 20];
                buffer.AsSpan().Fill('!');
                Span<char> bufferSpan = buffer.AsSpan(10);
                result = rsa.TryExportRSAPublicKeyPem(bufferSpan, out written);
                Assert.True(result, nameof(rsa.TryExportRSAPublicKeyPem));
                Assert.Equal(expectedPem.Length, written);
                Assert.Equal(expectedPem, new string(bufferSpan.Slice(0, written)));

                // Ensure padding has not been touched.
                AssertExtensions.FilledWith('!', buffer[0..10]);
                AssertExtensions.FilledWith('!', buffer[^10..]);
            }
        }

        [Fact]
        public static void ExportPem_ExportRSAPrivateKey()
        {
            string expectedPem =
                "-----BEGIN RSA PRIVATE KEY-----\n" +
                "cGVubnk=\n" +
                "-----END RSA PRIVATE KEY-----";

            static byte[] ExportRSAPrivateKey()
            {
                return new byte[] { 0x70, 0x65, 0x6e, 0x6e, 0x79 };
            }

            using (DelegateRSA rsa = new DelegateRSA())
            {
                rsa.ExportRSAPrivateKeyDelegate = ExportRSAPrivateKey;
                Assert.Equal(expectedPem, rsa.ExportRSAPrivateKeyPem());
            }
        }

        [Fact]
        public static void ExportPem_TryExportRSAPrivateKey()
        {
            string expectedPem =
                "-----BEGIN RSA PRIVATE KEY-----\n" +
                "cGVubnk=\n" +
                "-----END RSA PRIVATE KEY-----";

            static bool TryExportRSAPrivateKey(Span<byte> destination, out int bytesWritten)
            {
                ReadOnlySpan<byte> result = new byte[] { 0x70, 0x65, 0x6e, 0x6e, 0x79 };
                bytesWritten = result.Length;
                result.CopyTo(destination);
                return true;
            }

            using (DelegateRSA rsa = new DelegateRSA())
            {
                rsa.TryExportRSAPrivateKeyDelegate = TryExportRSAPrivateKey;

                int written;
                bool result;
                char[] buffer;

                // buffer not enough
                buffer = new char[expectedPem.Length - 1];
                result = rsa.TryExportRSAPrivateKeyPem(buffer, out written);
                Assert.False(result, nameof(rsa.TryExportRSAPrivateKeyPem));
                Assert.Equal(0, written);

                // buffer just enough
                buffer = new char[expectedPem.Length];
                result = rsa.TryExportRSAPrivateKeyPem(buffer, out written);
                Assert.True(result, nameof(rsa.TryExportRSAPrivateKeyPem));
                Assert.Equal(expectedPem.Length, written);
                Assert.Equal(expectedPem, new string(buffer));

                // buffer more than enough
                buffer = new char[expectedPem.Length + 20];
                buffer.AsSpan().Fill('!');
                Span<char> bufferSpan = buffer.AsSpan(10);
                result = rsa.TryExportRSAPrivateKeyPem(bufferSpan, out written);
                Assert.True(result, nameof(rsa.TryExportRSAPrivateKeyPem));
                Assert.Equal(expectedPem.Length, written);
                Assert.Equal(expectedPem, new string(bufferSpan.Slice(0, written)));

                // Ensure padding has not been touched.
                AssertExtensions.FilledWith('!', buffer[0..10]);
                AssertExtensions.FilledWith('!', buffer[^10..]);
            }
        }

        [Fact]
        public static void SignData_SpanData_StandardKeySize()
        {
            const int KeySizeInBits = 2048;

            static bool TrySignData(
                ReadOnlySpan<byte> data,
                Span<byte> signature,
                HashAlgorithmName hashAlgorithm,
                RSASignaturePadding padding,
                out int bytesWritten)
            {
                Assert.Equal(KeySizeInBits / 8, signature.Length);
                signature.Fill(0xCC);
                bytesWritten = signature.Length;
                return true;
            }

            using (DelegateRSA rsa = new DelegateRSA())
            {
                rsa.KeySize = KeySizeInBits;
                rsa.TrySignDataDelegate = TrySignData;

                ReadOnlySpan<byte> data = new byte[] { 1, 2, 3 };
                byte[] signature = rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);

                Assert.Equal(KeySizeInBits / 8, signature.Length);
                AssertExtensions.FilledWith<byte>(0xCC, signature);
            }
        }

        [Fact]
        public static void SignData_SpanData_TrySignDataProducesSmallSignatures()
        {
            const int KeySizeInBits = 2048;

            static bool TrySignData(
                ReadOnlySpan<byte> data,
                Span<byte> signature,
                HashAlgorithmName hashAlgorithm,
                RSASignaturePadding padding,
                out int bytesWritten)
            {
                Assert.Equal(KeySizeInBits / 8, signature.Length);
                signature.Fill(0xCC);

                // This RSA implementation for some reason generates smaller signatures.
                bytesWritten = signature.Length - 5;
                return true;
            }

            using (DelegateRSA rsa = new DelegateRSA())
            {
                rsa.KeySize = KeySizeInBits;
                rsa.TrySignDataDelegate = TrySignData;

                ReadOnlySpan<byte> data = new byte[] { 1, 2, 3 };
                byte[] signature = rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);

                Assert.Equal(KeySizeInBits / 8 - 5, signature.Length);
                AssertExtensions.FilledWith<byte>(0xCC, signature);
            }
        }

        [Fact]
        public static void SignData_SpanData_UnusualKeySizeRoundsUp()
        {
            const int KeySizeInBits = 2049;

            static bool TrySignData(
                ReadOnlySpan<byte> data,
                Span<byte> signature,
                HashAlgorithmName hashAlgorithm,
                RSASignaturePadding padding,
                out int bytesWritten)
            {
                Assert.Equal(257, signature.Length);
                signature.Fill(0xCC);

                bytesWritten = signature.Length;
                return true;
            }

            using (DelegateRSA rsa = new DelegateRSA())
            {
                rsa.KeySize = KeySizeInBits;
                rsa.TrySignDataDelegate = TrySignData;

                ReadOnlySpan<byte> data = new byte[] { 1, 2, 3 };
                byte[] signature = rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);

                Assert.Equal(257, signature.Length);
                AssertExtensions.FilledWith<byte>(0xCC, signature);
            }
        }

        [Fact]
        public static void SignData_SpanData_ImpossibleBytesWritten()
        {
            const int KeySizeInBits = 2048;

            static bool TrySignData(
                ReadOnlySpan<byte> data,
                Span<byte> signature,
                HashAlgorithmName hashAlgorithm,
                RSASignaturePadding padding,
                out int bytesWritten)
            {
                Assert.Equal(256, signature.Length);

                // This implementation somehow writes more data than possible.
                bytesWritten = signature.Length + 1;
                return true;
            }

            using (DelegateRSA rsa = new DelegateRSA())
            {
                rsa.KeySize = KeySizeInBits;
                rsa.TrySignDataDelegate = TrySignData;

                Assert.Throws<CryptographicException>(() =>
                    rsa.SignData((ReadOnlySpan<byte>)new byte[] { 1, 2, 3 }, HashAlgorithmName.SHA256, RSASignaturePadding.Pss));
            }
        }

        [Theory]
        [InlineData(8192)] // power-of-two
        [InlineData(6144)] // not a power-of-two
        public static void SignData_SpanData_Oversized(int signatureSizeInBits)
        {
            const int KeySizeInBits = 2048;

            bool TrySignData(
                ReadOnlySpan<byte> data,
                Span<byte> signature,
                HashAlgorithmName hashAlgorithm,
                RSASignaturePadding padding,
                out int bytesWritten)
            {
                if (signature.Length <= signatureSizeInBits / 8)
                {
                    bytesWritten = 0;
                    return false;
                }

                signature.Fill(0xCC);
                bytesWritten = signatureSizeInBits / 8;
                return true;
            }

            using (DelegateRSA rsa = new DelegateRSA())
            {
                rsa.KeySize = KeySizeInBits;
                rsa.TrySignDataDelegate = TrySignData;

                ReadOnlySpan<byte> data = new byte[] { 1, 2, 3 };
                byte[] signature = rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);

                Assert.Equal(signatureSizeInBits / 8, signature.Length);
                AssertExtensions.FilledWith<byte>(0xCC, signature);
            }
        }

        [Fact]
        public static void SignData_SpanData_NonAllocating_BufferExact()
        {
            const int KeySizeInBits = 2048;

            static bool TrySignData(
                ReadOnlySpan<byte> data,
                Span<byte> signature,
                HashAlgorithmName hashAlgorithm,
                RSASignaturePadding padding,
                out int bytesWritten)
            {
                signature.Fill(0xCC);
                bytesWritten = KeySizeInBits / 8;
                return true;
            }

            using (DelegateRSA rsa = new DelegateRSA())
            {
                rsa.KeySize = KeySizeInBits;
                rsa.TrySignDataDelegate = TrySignData;

                ReadOnlySpan<byte> data = new byte[] { 1, 2, 3 };
                Span<byte> buffer = new byte[KeySizeInBits / 8];
                int written = rsa.SignData(data, buffer, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
                Assert.Equal(KeySizeInBits / 8, written);
                AssertExtensions.FilledWith<byte>(0xCC, buffer);
            }
        }

        [Fact]
        public static void SignData_SpanData_NonAllocating_BufferLarger()
        {
            const int KeySizeInBits = 2048;

            static bool TrySignData(
                ReadOnlySpan<byte> data,
                Span<byte> signature,
                HashAlgorithmName hashAlgorithm,
                RSASignaturePadding padding,
                out int bytesWritten)
            {
                bytesWritten = KeySizeInBits / 8 - 7;
                signature.Slice(0, bytesWritten).Fill(0xCC);
                return true;
            }

            using (DelegateRSA rsa = new DelegateRSA())
            {
                rsa.KeySize = KeySizeInBits;
                rsa.TrySignDataDelegate = TrySignData;

                ReadOnlySpan<byte> data = new byte[] { 1, 2, 3 };
                Span<byte> buffer = new byte[KeySizeInBits / 8];
                int written = rsa.SignData(data, buffer, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
                Assert.Equal(KeySizeInBits / 8 - 7, written);
                AssertExtensions.FilledWith<byte>(0xCC, buffer.Slice(0, written));
                AssertExtensions.FilledWith<byte>(0x00, buffer.Slice(written));
            }
        }

        [Fact]
        public static void SignData_SpanData_NonAllocating_TooSmall()
        {
            static bool TrySignData(
                ReadOnlySpan<byte> data,
                Span<byte> signature,
                HashAlgorithmName hashAlgorithm,
                RSASignaturePadding padding,
                out int bytesWritten)
            {
                bytesWritten = 0;
                return false;
            }

            using (DelegateRSA rsa = new DelegateRSA())
            {
                rsa.KeySize = 2048;
                rsa.TrySignDataDelegate = TrySignData;

                Assert.Throws<ArgumentException>("destination", () =>
                    rsa.SignData(new byte[] { 1, 2, 3 }, Span<byte>.Empty, HashAlgorithmName.SHA256, RSASignaturePadding.Pss));
            }
        }

        [Fact]
        public static void SignHash_SpanHash_StandardKeySize()
        {
            const int KeySizeInBits = 2048;

            static bool TrySignHash(
                ReadOnlySpan<byte> hash,
                Span<byte> signature,
                HashAlgorithmName hashAlgorithm,
                RSASignaturePadding padding,
                out int bytesWritten)
            {
                Assert.Equal(KeySizeInBits / 8, signature.Length);
                signature.Fill(0xCC);
                bytesWritten = signature.Length;
                return true;
            }

            using (DelegateRSA rsa = new DelegateRSA())
            {
                rsa.KeySize = KeySizeInBits;
                rsa.TrySignHashDelegate = TrySignHash;

                ReadOnlySpan<byte> hash = new byte[]
                {
                    01, 02, 03, 04, 05, 06, 07, 08, 09, 10,
                    11, 12, 13, 14, 15, 16, 17, 18, 19, 20,
                };
                byte[] signature = rsa.SignHash(hash, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);

                Assert.Equal(KeySizeInBits / 8, signature.Length);
                AssertExtensions.FilledWith<byte>(0xCC, signature);
            }
        }

        [Fact]
        public static void SignHash_SpanHash_TrySignHashProducesSmallSignatures()
        {
            const int KeySizeInBits = 2048;

            static bool TrySignHash(
                ReadOnlySpan<byte> hash,
                Span<byte> signature,
                HashAlgorithmName hashAlgorithm,
                RSASignaturePadding padding,
                out int bytesWritten)
            {
                Assert.Equal(KeySizeInBits / 8, signature.Length);
                signature.Fill(0xCC);

                // This RSA implementation for some reason generates smaller signatures.
                bytesWritten = signature.Length - 5;
                return true;
            }

            using (DelegateRSA rsa = new DelegateRSA())
            {
                rsa.KeySize = KeySizeInBits;
                rsa.TrySignHashDelegate = TrySignHash;

                ReadOnlySpan<byte> hash = new byte[]
                {
                    01, 02, 03, 04, 05, 06, 07, 08, 09, 10,
                    11, 12, 13, 14, 15, 16, 17, 18, 19, 20,
                };
                byte[] signature = rsa.SignHash(hash, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);

                Assert.Equal(KeySizeInBits / 8 - 5, signature.Length);
                AssertExtensions.FilledWith<byte>(0xCC, signature);
            }
        }

        [Fact]
        public static void SignHash_SpanHash_UnusualKeySizeRoundsUp()
        {
            const int KeySizeInBits = 2049;

            static bool TrySignHash(
                ReadOnlySpan<byte> hash,
                Span<byte> signature,
                HashAlgorithmName hashAlgorithm,
                RSASignaturePadding padding,
                out int bytesWritten)
            {
                Assert.Equal(257, signature.Length);
                signature.Fill(0xCC);

                bytesWritten = signature.Length;
                return true;
            }

            using (DelegateRSA rsa = new DelegateRSA())
            {
                rsa.KeySize = KeySizeInBits;
                rsa.TrySignHashDelegate = TrySignHash;

                ReadOnlySpan<byte> hash = new byte[]
                {
                    01, 02, 03, 04, 05, 06, 07, 08, 09, 10,
                    11, 12, 13, 14, 15, 16, 17, 18, 19, 20,
                };
                byte[] signature = rsa.SignHash(hash, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);

                Assert.Equal(257, signature.Length);
                AssertExtensions.FilledWith<byte>(0xCC, signature);
            }
        }

        [Fact]
        public static void SignHash_SpanHash_ImpossibleBytesWritten()
        {
            const int KeySizeInBits = 2048;

            static bool TrySignHash(
                ReadOnlySpan<byte> hash,
                Span<byte> signature,
                HashAlgorithmName hashAlgorithm,
                RSASignaturePadding padding,
                out int bytesWritten)
            {
                Assert.Equal(256, signature.Length);

                // This implementation somehow writes more data than possible.
                bytesWritten = signature.Length + 1;
                return true;
            }

            using (DelegateRSA rsa = new DelegateRSA())
            {
                rsa.KeySize = KeySizeInBits;
                rsa.TrySignHashDelegate = TrySignHash;

                Assert.Throws<CryptographicException>(() =>
                    rsa.SignHash((ReadOnlySpan<byte>)new byte[20], HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1));
            }
        }

        [Theory]
        [InlineData(8192)] // power-of-two
        [InlineData(6144)] // not a power-of-two
        public static void SignHash_SpanHash_Oversized(int signatureSizeInBits)
        {
            const int KeySizeInBits = 2048;

            bool TrySignHash(
                ReadOnlySpan<byte> hash,
                Span<byte> signature,
                HashAlgorithmName hashAlgorithm,
                RSASignaturePadding padding,
                out int bytesWritten)
            {
                if (signature.Length <= signatureSizeInBits / 8)
                {
                    bytesWritten = 0;
                    return false;
                }

                signature.Fill(0xCC);
                bytesWritten = signatureSizeInBits / 8;
                return true;
            }

            using (DelegateRSA rsa = new DelegateRSA())
            {
                rsa.KeySize = KeySizeInBits;
                rsa.TrySignHashDelegate = TrySignHash;

                ReadOnlySpan<byte> hash = new byte[]
                {
                    01, 02, 03, 04, 05, 06, 07, 08, 09, 10,
                    11, 12, 13, 14, 15, 16, 17, 18, 19, 20,
                };
                byte[] signature = rsa.SignHash(hash, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);

                Assert.Equal(signatureSizeInBits / 8, signature.Length);
                AssertExtensions.FilledWith<byte>(0xCC, signature);
            }
        }

        [Fact]
        public static void SignHash_SpanHash_NonAllocating_BufferExact()
        {
            const int KeySizeInBits = 2048;

            static bool TrySignHash(
                ReadOnlySpan<byte> hash,
                Span<byte> signature,
                HashAlgorithmName hashAlgorithm,
                RSASignaturePadding padding,
                out int bytesWritten)
            {
                signature.Fill(0xCC);
                bytesWritten = KeySizeInBits / 8;
                return true;
            }

            using (DelegateRSA rsa = new DelegateRSA())
            {
                rsa.KeySize = KeySizeInBits;
                rsa.TrySignHashDelegate = TrySignHash;

                ReadOnlySpan<byte> hash = new byte[]
                {
                    01, 02, 03, 04, 05, 06, 07, 08, 09, 10,
                    11, 12, 13, 14, 15, 16, 17, 18, 19, 20,
                };
                Span<byte> buffer = new byte[KeySizeInBits / 8];
                int written = rsa.SignHash(hash, buffer, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
                Assert.Equal(KeySizeInBits / 8, written);
                AssertExtensions.FilledWith<byte>(0xCC, buffer);
            }
        }

        [Fact]
        public static void SignHash_SpanHash_NonAllocating_BufferLarger()
        {
            const int KeySizeInBits = 2048;

            static bool TrySignHash(
                ReadOnlySpan<byte> hash,
                Span<byte> signature,
                HashAlgorithmName hashAlgorithm,
                RSASignaturePadding padding,
                out int bytesWritten)
            {
                bytesWritten = KeySizeInBits / 8 - 7;
                signature.Slice(0, bytesWritten).Fill(0xCC);
                return true;
            }

            using (DelegateRSA rsa = new DelegateRSA())
            {
                rsa.KeySize = KeySizeInBits;
                rsa.TrySignHashDelegate = TrySignHash;

                ReadOnlySpan<byte> hash = new byte[]
                {
                    01, 02, 03, 04, 05, 06, 07, 08, 09, 10,
                    11, 12, 13, 14, 15, 16, 17, 18, 19, 20,
                };
                Span<byte> buffer = new byte[KeySizeInBits / 8];
                int written = rsa.SignHash(hash, buffer, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
                Assert.Equal(KeySizeInBits / 8 - 7, written);
                AssertExtensions.FilledWith<byte>(0xCC, buffer.Slice(0, written));
                AssertExtensions.FilledWith<byte>(0x00, buffer.Slice(written));
            }
        }

        [Fact]
        public static void SignHash_SpanHash_NonAllocating_TooSmall()
        {
            static bool TrySignHash(
                ReadOnlySpan<byte> hash,
                Span<byte> signature,
                HashAlgorithmName hashAlgorithm,
                RSASignaturePadding padding,
                out int bytesWritten)
            {
                bytesWritten = 0;
                return false;
            }

            using (DelegateRSA rsa = new DelegateRSA())
            {
                rsa.KeySize = 2048;
                rsa.TrySignHashDelegate = TrySignHash;

                Assert.Throws<ArgumentException>("destination", () =>
                    rsa.SignHash(new byte[20], Span<byte>.Empty, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1));
            }
        }

        [Fact]
        public static void Encrypt_SpanData_StandardKeySize()
        {
            const int KeySizeInBits = 2048;

            static bool TryEncrypt(
                ReadOnlySpan<byte> data,
                Span<byte> destination,
                RSAEncryptionPadding padding,
                out int bytesWritten)
            {
                Assert.Equal(KeySizeInBits / 8, destination.Length);
                destination.Fill(0xCC);
                bytesWritten = destination.Length;
                return true;
            }

            using (DelegateRSA rsa = new DelegateRSA())
            {
                rsa.KeySize = KeySizeInBits;
                rsa.TryEncryptDelegate = TryEncrypt;

                ReadOnlySpan<byte> data = new byte[] { 1, 2, 3 };
                byte[] encrypted = rsa.Encrypt(data, RSAEncryptionPadding.Pkcs1);

                Assert.Equal(KeySizeInBits / 8, encrypted.Length);
                AssertExtensions.FilledWith<byte>(0xCC, encrypted);

                encrypted = new byte[KeySizeInBits / 8];
                int written = rsa.Encrypt(data, encrypted, RSAEncryptionPadding.Pkcs1);
                Assert.Equal(KeySizeInBits / 8, written);
                AssertExtensions.FilledWith<byte>(0xCC, encrypted);
            }
        }

        [Fact]
        public static void Encrypt_SpanData_TryEncryptProducesSmallEncryptedData()
        {
            const int KeySizeInBits = 2048;

            static bool TryEncrypt(
                ReadOnlySpan<byte> data,
                Span<byte> destination,
                RSAEncryptionPadding padding,
                out int bytesWritten)
            {
                Assert.Equal(KeySizeInBits / 8, destination.Length);
                bytesWritten = destination.Length - 12;

                // This RSA implementation for some reason generates encrypted data
                // smaller than the modulus.
                destination.Slice(0, bytesWritten).Fill(0xCC);
                return true;
            }

            using (DelegateRSA rsa = new DelegateRSA())
            {
                rsa.KeySize = KeySizeInBits;
                rsa.TryEncryptDelegate = TryEncrypt;

                ReadOnlySpan<byte> data = new byte[] { 1, 2, 3 };
                byte[] encrypted = rsa.Encrypt(data, RSAEncryptionPadding.Pkcs1);

                Assert.Equal(KeySizeInBits / 8 - 12, encrypted.Length);
                AssertExtensions.FilledWith<byte>(0xCC, encrypted);

                encrypted = new byte[KeySizeInBits / 8];
                int written = rsa.Encrypt(data, encrypted, RSAEncryptionPadding.Pkcs1);
                Assert.Equal(KeySizeInBits / 8 - 12, written);
                AssertExtensions.FilledWith<byte>(0xCC, encrypted.AsSpan(0, written));
                AssertExtensions.FilledWith<byte>(0x00, encrypted.AsSpan(written));
            }
        }

        [Fact]
        public static void Encrypt_SpanData_UnusualKeySizeRoundsUp()
        {
            const int KeySizeInBits = 2049;

            static bool TryEncrypt(
                ReadOnlySpan<byte> data,
                Span<byte> destination,
                RSAEncryptionPadding padding,
                out int bytesWritten)
            {
                Assert.Equal(257, destination.Length);
                destination.Fill(0xCC);

                bytesWritten = destination.Length;
                return true;
            }

            using (DelegateRSA rsa = new DelegateRSA())
            {
                rsa.KeySize = KeySizeInBits;
                rsa.TryEncryptDelegate = TryEncrypt;

                ReadOnlySpan<byte> data = new byte[] { 1, 2, 3 };
                byte[] encrypted = rsa.Encrypt(data, RSAEncryptionPadding.Pkcs1);

                Assert.Equal(257, encrypted.Length);
                AssertExtensions.FilledWith<byte>(0xCC, encrypted);
            }
        }

        [Fact]
        public static void Encrypt_SpanData_ImpossibleBytesWritten()
        {
            const int KeySizeInBits = 2048;

            static bool TryEncrypt(
                ReadOnlySpan<byte> data,
                Span<byte> destination,
                RSAEncryptionPadding padding,
                out int bytesWritten)
            {
                Assert.Equal(KeySizeInBits / 8, destination.Length);

                // This implementation somehow writes more data than possible.
                bytesWritten = destination.Length + 1;
                return true;
            }

            using (DelegateRSA rsa = new DelegateRSA())
            {
                rsa.KeySize = KeySizeInBits;
                rsa.TryEncryptDelegate = TryEncrypt;

                Assert.Throws<CryptographicException>(() =>
                    rsa.Encrypt((ReadOnlySpan<byte>)new byte[] { 1, 2, 3 }, RSAEncryptionPadding.Pkcs1));
            }
        }

        [Theory]
        [InlineData(8192)] // power-of-two
        [InlineData(6144)] // not a power-of-two
        public static void Encrypt_SpanData_Oversized(int encryptedDataSizeInBits)
        {
            const int KeySizeInBits = 2048;
            int encryptedDataSize = encryptedDataSizeInBits / 8;

            bool TryEncrypt(
                ReadOnlySpan<byte> data,
                Span<byte> destination,
                RSAEncryptionPadding padding,
                out int bytesWritten)
            {
                if (destination.Length <= encryptedDataSize)
                {
                    bytesWritten = 0;
                    return false;
                }

                // This implementation produces oversides RSA encrypted data larger than the modulus.
                destination.Fill(0xCC);
                bytesWritten = encryptedDataSize;
                return true;
            }

            using (DelegateRSA rsa = new DelegateRSA())
            {
                rsa.KeySize = KeySizeInBits;
                rsa.TryEncryptDelegate = TryEncrypt;

                ReadOnlySpan<byte> data = new byte[] { 1, 2, 3 };
                byte[] encrypted = rsa.Encrypt(data, RSAEncryptionPadding.Pkcs1);

                Assert.Equal(encryptedDataSize, encrypted.Length);
                AssertExtensions.FilledWith<byte>(0xCC, encrypted);
            }
        }

        [Fact]
        public static void Decrypt_SpanData_StandardKeySize()
        {
            const int KeySizeInBits = 2048;

            static bool TryDecrypt(
                ReadOnlySpan<byte> data,
                Span<byte> destination,
                RSAEncryptionPadding padding,
                out int bytesWritten)
            {
                destination[0] = 1;
                destination[1] = 2;
                destination[2] = 3;
                bytesWritten = 3;
                return true;
            }

            using (DelegateRSA rsa = new DelegateRSA())
            {
                rsa.KeySize = KeySizeInBits;
                rsa.TryDecryptDelegate = TryDecrypt;

                byte[] expected = new byte[] { 1, 2, 3 };
                ReadOnlySpan<byte> data = new byte[KeySizeInBits / 8];

                byte[] decrypted = rsa.Decrypt(data, RSAEncryptionPadding.Pkcs1);
                Assert.Equal(expected, decrypted);

                int written = rsa.Decrypt(data, decrypted, RSAEncryptionPadding.Pkcs1);
                Assert.Equal(expected.Length, written);
                Assert.Equal(expected, decrypted);
            }
        }

        [Fact]
        public static void Decrypt_SpanData_ImpossibleDecryptedSize()
        {
            const int KeySizeInBits = 2048;

            static bool TryDecrypt(
                ReadOnlySpan<byte> data,
                Span<byte> destination,
                RSAEncryptionPadding padding,
                out int bytesWritten)
            {
                // Somehow decryption succeeded but wrote more to the buffer than possible.
                bytesWritten = destination.Length + 1;
                return true;
            }

            using (DelegateRSA rsa = new DelegateRSA())
            {
                rsa.KeySize = KeySizeInBits;
                rsa.TryDecryptDelegate = TryDecrypt;

                Assert.Throws<CryptographicException>(() =>
                    rsa.Decrypt(new ReadOnlySpan<byte>(new byte[KeySizeInBits / 8]), RSAEncryptionPadding.Pkcs1));
            }
        }

        private sealed class EmptyRSA : RSA
        {
            public override RSAParameters ExportParameters(bool includePrivateParameters) => throw new NotImplementedException();
            public override void ImportParameters(RSAParameters parameters) => throw new NotImplementedException();
            public new byte[] HashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm) => base.HashData(data, offset, count, hashAlgorithm);
            public new byte[] HashData(Stream data, HashAlgorithmName hashAlgorithm) => base.HashData(data, hashAlgorithm);
        }

        private sealed class DelegateRSA : RSA
        {
            public DelegateRSA()
            {
                LegalKeySizesValue = new[]
                {
                    new KeySizes(1, 16_384, 1), // Every "reasonable" key size is legal.
                };
            }

            public delegate bool TrySignDataFunc(
                ReadOnlySpan<byte> data,
                Span<byte> signature,
                HashAlgorithmName hashAlgorithm,
                RSASignaturePadding padding,
                out int bytesWritten);
            public delegate bool TrySignHashFunc(
                ReadOnlySpan<byte> hash,
                Span<byte> signature,
                HashAlgorithmName hashAlgorithm,
                RSASignaturePadding padding,
                out int bytesWritten);
            public delegate bool TryEncryptFunc(
                ReadOnlySpan<byte> data,
                Span<byte> destination,
                RSAEncryptionPadding padding,
                out int bytesWritten);
            public delegate bool TryDecryptFunc(
                ReadOnlySpan<byte> data,
                Span<byte> destination,
                RSAEncryptionPadding padding,
                out int bytesWritten);
            public delegate bool TryExportFunc(Span<byte> destination, out int bytesWritten);
            public Func<byte[], RSAEncryptionPadding, byte[]> DecryptDelegate;
            public Func<byte[], RSAEncryptionPadding, byte[]> EncryptDelegate;
            public Func<byte[], HashAlgorithmName, RSASignaturePadding, byte[]> SignHashDelegate = null;
            public Func<byte[], byte[], HashAlgorithmName, RSASignaturePadding, bool> VerifyHashDelegate = null;
            public Func<byte[], int, int, HashAlgorithmName, byte[]> HashDataArrayDelegate = null;
            public Func<Stream, HashAlgorithmName, byte[]> HashDataStreamDelegate = null;
            public Func<byte[]> ExportRSAPublicKeyDelegate = null;
            public TryExportFunc TryExportRSAPublicKeyDelegate = null;
            public Func<byte[]> ExportRSAPrivateKeyDelegate = null;
            public TryExportFunc TryExportRSAPrivateKeyDelegate = null;
            public TrySignDataFunc TrySignDataDelegate = null;
            public TrySignHashFunc TrySignHashDelegate = null;
            public TryEncryptFunc TryEncryptDelegate = null;
            public TryDecryptFunc TryDecryptDelegate = null;

            public override byte[] Encrypt(byte[] data, RSAEncryptionPadding padding) =>
                EncryptDelegate(data, padding);

            public override byte[] Decrypt(byte[] data, RSAEncryptionPadding padding) =>
                DecryptDelegate(data, padding);

            public override byte[] SignHash(byte[] hash, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding) =>
                SignHashDelegate(hash, hashAlgorithm, padding);

            public override bool VerifyHash(byte[] hash, byte[] signature, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding) =>
                VerifyHashDelegate(hash, signature, hashAlgorithm, padding);

            protected override byte[] HashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm) =>
                HashDataArrayDelegate(data, offset, count, hashAlgorithm);

            protected override byte[] HashData(Stream data, HashAlgorithmName hashAlgorithm) =>
                HashDataStreamDelegate(data, hashAlgorithm);

            public override byte[] ExportRSAPublicKey() => ExportRSAPublicKeyDelegate();
            public override byte[] ExportRSAPrivateKey() => ExportRSAPrivateKeyDelegate();

            public override bool TryExportRSAPublicKey(Span<byte> destination, out int bytesWritten) =>
                TryExportRSAPublicKeyDelegate(destination, out bytesWritten);

            public override bool TryExportRSAPrivateKey(Span<byte> destination, out int bytesWritten) =>
                TryExportRSAPrivateKeyDelegate(destination, out bytesWritten);

            public override bool TrySignData(
                ReadOnlySpan<byte> data,
                Span<byte> signature,
                HashAlgorithmName hashAlgorithm,
                RSASignaturePadding padding,
                out int bytesWritten)
            {
                if (TrySignDataDelegate is not null)
                {
                    return TrySignDataDelegate(data, signature, hashAlgorithm, padding, out bytesWritten);
                }

                return base.TrySignData(data, signature, hashAlgorithm, padding, out bytesWritten);
            }

            public override bool TrySignHash(
                ReadOnlySpan<byte> hash,
                Span<byte> signature,
                HashAlgorithmName hashAlgorithm,
                RSASignaturePadding padding,
                out int bytesWritten)
            {
                if (TrySignHashDelegate is not null)
                {
                    return TrySignHashDelegate(hash, signature, hashAlgorithm, padding, out bytesWritten);
                }

                return base.TrySignHash(hash, signature, hashAlgorithm, padding, out bytesWritten);
            }

            public override bool TryEncrypt(ReadOnlySpan<byte> data, Span<byte> destination, RSAEncryptionPadding padding, out int bytesWritten)
            {
                if (TryEncryptDelegate is not null)
                {
                    return TryEncryptDelegate(data, destination, padding, out bytesWritten);
                }

                return base.TryEncrypt(data, destination, padding, out bytesWritten);
            }

            public override bool TryDecrypt(ReadOnlySpan<byte> data, Span<byte> destination, RSAEncryptionPadding padding, out int bytesWritten)
            {
                if (TryDecryptDelegate is not null)
                {
                    return TryDecryptDelegate(data, destination, padding, out bytesWritten);
                }

                return base.TryDecrypt(data, destination, padding, out bytesWritten);
            }

            public new bool TryHashData(ReadOnlySpan<byte> source, Span<byte> destination, HashAlgorithmName hashAlgorithm, out int bytesWritten) =>
                base.TryHashData(source, destination, hashAlgorithm, out bytesWritten);

            public override RSAParameters ExportParameters(bool includePrivateParameters) => throw new NotImplementedException();
            public override void ImportParameters(RSAParameters parameters) => throw new NotImplementedException();
        }
    }
}
