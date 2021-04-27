// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using Xunit;

namespace System.Security.Cryptography.Algorithms.Tests
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
            Assert.Throws<NotImplementedException>(() => rsa.HashData(null, 0, 0, HashAlgorithmName.SHA256));
            Assert.Throws<NotImplementedException>(() => rsa.HashData(null, HashAlgorithmName.SHA256));
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

            AssertExtensions.Throws<ArgumentException>("hashAlgorithm", () => rsa.SignData(new byte[1], 0, 1, new HashAlgorithmName(null), null));
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

            AssertExtensions.Throws<ArgumentException>("hashAlgorithm", () => rsa.SignData(Stream.Null, new HashAlgorithmName(null), null));
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

            AssertExtensions.Throws<ArgumentException>("hashAlgorithm", () => rsa.VerifyData(Stream.Null, new byte[1], new HashAlgorithmName(null), null));
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

        private sealed class EmptyRSA : RSA
        {
            public override RSAParameters ExportParameters(bool includePrivateParameters) => throw new NotImplementedException();
            public override void ImportParameters(RSAParameters parameters) => throw new NotImplementedException();
            public new byte[] HashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm) => base.HashData(data, offset, count, hashAlgorithm);
            public new byte[] HashData(Stream data, HashAlgorithmName hashAlgorithm) => base.HashData(data, hashAlgorithm);
        }

        private sealed class DelegateRSA : RSA
        {
            public Func<byte[], RSAEncryptionPadding, byte[]> DecryptDelegate;
            public Func<byte[], RSAEncryptionPadding, byte[]> EncryptDelegate;
            public Func<byte[], HashAlgorithmName, RSASignaturePadding, byte[]> SignHashDelegate = null;
            public Func<byte[], byte[], HashAlgorithmName, RSASignaturePadding, bool> VerifyHashDelegate = null;
            public Func<byte[], int, int, HashAlgorithmName, byte[]> HashDataArrayDelegate = null;
            public Func<Stream, HashAlgorithmName, byte[]> HashDataStreamDelegate = null;

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

            public new bool TryHashData(ReadOnlySpan<byte> source, Span<byte> destination, HashAlgorithmName hashAlgorithm, out int bytesWritten) =>
                base.TryHashData(source, destination, hashAlgorithm, out bytesWritten);

            public override RSAParameters ExportParameters(bool includePrivateParameters) => throw new NotImplementedException();
            public override void ImportParameters(RSAParameters parameters) => throw new NotImplementedException();
        }
    }
}
