// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.X509Certificates;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Pkcs.Tests
{
    public static class Pkcs8PrivateKeyInfoTests
    {
        [Fact]
        public static void EnsureAttributesParse()
        {
            byte[] windowsEcdsaKey = Convert.FromBase64String(@"
MIHJMBwGCiqGSIb3DQEMAQMwDgQI7BVCacWHggkCAgfQBIGoC6pK+GYOb6t7BQuO
9gTPw3hlK1T+AF3Dx+LxLl78f+K7Dhs4KMr+dS/WTHygSvn7xaHzbjDX0pnFK/au
ZbVLkkDMN8BOfgYzDCTpbRmme3AVpr9SwXL/6nGbkw2+MQ7rx1a9/y3yhG7pc6Zs
Y/TpEwCD1kSHs1wZQemLArbVqSlyKTdCODxizK+5lurXGh310DgO//JbpgsjOjkh
D9fVWpuVzYpEDfZm");

            Pkcs8PrivateKeyInfo pkcs8Info = Pkcs8PrivateKeyInfo.DecryptAndDecode(
                (ReadOnlySpan<char>)"Test",
                windowsEcdsaKey,
                out int bytesRead);

            Assert.Equal(windowsEcdsaKey.Length, bytesRead);
            Assert.Equal(1, pkcs8Info.Attributes.Count);
            Assert.Equal("2.5.29.15", pkcs8Info.Attributes[0].Oid.Value);

            var ku = new X509KeyUsageExtension(pkcs8Info.Attributes[0].Values[0], false);
            Assert.Equal(X509KeyUsageFlags.DigitalSignature, ku.KeyUsages);
        }

        [Fact]
        public static void EnsureAttributesRoundtrip()
        {
            Pkcs8PrivateKeyInfo pkcs8Info;

            using (ECDsa ecdsa = ECDsa.Create())
            {
                pkcs8Info = Pkcs8PrivateKeyInfo.Create(ecdsa);
            }

            string description = DateTimeOffset.UtcNow.ToString();
            pkcs8Info.Attributes.Add(new Pkcs9DocumentDescription(description));

            byte[] encoded = pkcs8Info.Encode();

            Pkcs8PrivateKeyInfo pkcs8Info2 = Pkcs8PrivateKeyInfo.Decode(encoded, out _, skipCopy: true);
            Assert.Equal(pkcs8Info.AlgorithmId.Value, pkcs8Info2.AlgorithmId.Value);

            Assert.Equal(
                pkcs8Info.AlgorithmParameters.Value.ByteArrayToHex(),
                pkcs8Info2.AlgorithmParameters.Value.ByteArrayToHex());

            Assert.Equal(
                pkcs8Info.PrivateKeyBytes.ByteArrayToHex(),
                pkcs8Info2.PrivateKeyBytes.ByteArrayToHex());

            Assert.Equal(1, pkcs8Info2.Attributes.Count);

            Pkcs9DocumentDescription descAttr =
                Assert.IsType<Pkcs9DocumentDescription>(pkcs8Info2.Attributes[0].Values[0]);

            Assert.Equal(description, descAttr.DocumentDescription);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void Decode_SkipCopyIsRespected(bool skipCopy)
        {
            Pkcs8PrivateKeyInfo pkcs8Info;

            using (ECDsa ecdsa = ECDsa.Create())
            {
                pkcs8Info = Pkcs8PrivateKeyInfo.Create(ecdsa);
            }

            byte[] encoded = pkcs8Info.Encode();
            ReadOnlySpan<byte> encodedSpan = encoded;
            Pkcs8PrivateKeyInfo pkcs8Info2 = Pkcs8PrivateKeyInfo.Decode(encoded, out _, skipCopy);

            if (skipCopy)
            {
                Assert.True(
                    encodedSpan.Overlaps(pkcs8Info2.AlgorithmParameters.Value.Span),
                    "AlgorihmParameters overlaps");

                Assert.True(
                    encodedSpan.Overlaps(pkcs8Info2.PrivateKeyBytes.Span),
                    "PrivateKeyBytes overlaps");
            }
            else
            {
                Assert.False(
                    encodedSpan.Overlaps(pkcs8Info2.AlgorithmParameters.Value.Span),
                    "AlgorihmParameters overlaps");

                Assert.False(
                    encodedSpan.Overlaps(pkcs8Info2.PrivateKeyBytes.Span),
                    "PrivateKeyBytes overlaps");
            }
        }

        [Fact]
        public static void BadPbeParameters()
        {
            Pkcs8PrivateKeyInfo privateKeyInfo;

            using (RSA rsa = RSA.Create())
            {
                privateKeyInfo = Pkcs8PrivateKeyInfo.Create(rsa);
            }

            Assert.ThrowsAny<ArgumentNullException>(
                () => privateKeyInfo.Encrypt(ReadOnlySpan<char>.Empty, null));

            Assert.ThrowsAny<ArgumentNullException>(
                () => privateKeyInfo.Encrypt(ReadOnlySpan<byte>.Empty, null));

            Assert.ThrowsAny<ArgumentNullException>(
                () => privateKeyInfo.TryEncrypt(ReadOnlySpan<char>.Empty, null, Span<byte>.Empty, out _));

            Assert.ThrowsAny<ArgumentNullException>(
                () => privateKeyInfo.TryEncrypt(ReadOnlySpan<byte>.Empty, null, Span<byte>.Empty, out _));

            // PKCS12 requires SHA-1
            Assert.ThrowsAny<CryptographicException>(
                () => privateKeyInfo.Encrypt(
                    ReadOnlySpan<byte>.Empty,
                    new PbeParameters(PbeEncryptionAlgorithm.TripleDes3KeyPkcs12, HashAlgorithmName.SHA256, 72)));

            Assert.ThrowsAny<CryptographicException>(
                () => privateKeyInfo.TryEncrypt(
                    ReadOnlySpan<byte>.Empty,
                    new PbeParameters(PbeEncryptionAlgorithm.TripleDes3KeyPkcs12, HashAlgorithmName.SHA256, 72),
                    Span<byte>.Empty,
                    out _));

            // PKCS12 requires SHA-1
            Assert.ThrowsAny<CryptographicException>(
                () => privateKeyInfo.Encrypt(
                    ReadOnlySpan<byte>.Empty,
                    new PbeParameters(PbeEncryptionAlgorithm.TripleDes3KeyPkcs12, HashAlgorithmName.MD5, 72)));

            Assert.ThrowsAny<CryptographicException>(
                () => privateKeyInfo.TryEncrypt(
                    ReadOnlySpan<byte>.Empty,
                    new PbeParameters(PbeEncryptionAlgorithm.TripleDes3KeyPkcs12, HashAlgorithmName.MD5, 72),
                    Span<byte>.Empty,
                    out _));

            // PKCS12 requires a char-based password
            Assert.ThrowsAny<CryptographicException>(
                () => privateKeyInfo.Encrypt(
                    new byte[3],
                    new PbeParameters(PbeEncryptionAlgorithm.TripleDes3KeyPkcs12, HashAlgorithmName.SHA1, 72)));

            Assert.ThrowsAny<CryptographicException>(
                () => privateKeyInfo.TryEncrypt(
                    new byte[3],
                    new PbeParameters(PbeEncryptionAlgorithm.TripleDes3KeyPkcs12, HashAlgorithmName.SHA1, 72),
                    Span<byte>.Empty,
                    out _));

            // Unknown encryption algorithm
            Assert.ThrowsAny<CryptographicException>(
                () => privateKeyInfo.Encrypt(
                    new byte[3],
                    new PbeParameters(0, HashAlgorithmName.SHA1, 72)));

            Assert.ThrowsAny<CryptographicException>(
                () => privateKeyInfo.TryEncrypt(
                    new byte[3],
                    new PbeParameters(0, HashAlgorithmName.SHA1, 72),
                    Span<byte>.Empty,
                    out _));

            // Unknown encryption algorithm (negative enum value)
            Assert.ThrowsAny<CryptographicException>(
                () => privateKeyInfo.Encrypt(
                    new byte[3],
                    new PbeParameters((PbeEncryptionAlgorithm)(-5), HashAlgorithmName.SHA1, 72)));

            Assert.ThrowsAny<CryptographicException>(
                () => privateKeyInfo.TryEncrypt(
                    new byte[3],
                    new PbeParameters((PbeEncryptionAlgorithm)(-5), HashAlgorithmName.SHA1, 72),
                    Span<byte>.Empty,
                    out _));

            // Unknown encryption algorithm (overly-large enum value)
            Assert.ThrowsAny<CryptographicException>(
                () => privateKeyInfo.Encrypt(
                    new byte[3],
                    new PbeParameters((PbeEncryptionAlgorithm)15, HashAlgorithmName.SHA1, 72)));

            Assert.ThrowsAny<CryptographicException>(
                () => privateKeyInfo.TryEncrypt(
                    new byte[3],
                    new PbeParameters((PbeEncryptionAlgorithm)15, HashAlgorithmName.SHA1, 72),
                    Span<byte>.Empty,
                    out _));

            // Unknown hash algorithm
            Assert.ThrowsAny<CryptographicException>(
                () => privateKeyInfo.Encrypt(
                    new byte[3],
                    new PbeParameters(PbeEncryptionAlgorithm.Aes192Cbc, new HashAlgorithmName("Potato"), 72)));

            Assert.ThrowsAny<CryptographicException>(
                () => privateKeyInfo.TryEncrypt(
                    new byte[3],
                    new PbeParameters(PbeEncryptionAlgorithm.Aes192Cbc, new HashAlgorithmName("Potato"), 72),
                    Span<byte>.Empty,
                    out _));
        }

        [Fact]
        public static void DecryptionFailures()
        {
            using (RSA rsa = RSA.Create())
            {
                byte[] encryptedKey = rsa.ExportEncryptedPkcs8PrivateKey(
                    (ReadOnlySpan<char>)nameof(DecryptionFailures),
                    new PbeParameters(
                        PbeEncryptionAlgorithm.TripleDes3KeyPkcs12,
                        HashAlgorithmName.SHA1,
                        1024));

                // Wrong password
                Assert.Throws<CryptographicException>(
                    () => Pkcs8PrivateKeyInfo.DecryptAndDecode((ReadOnlySpan<char>)"wrong", encryptedKey, out _));

                // Wrong password
                Assert.Throws<CryptographicException>(
                    () => Pkcs8PrivateKeyInfo.DecryptAndDecode(new byte[3], encryptedKey, out _));

                // Corrupted data
                Assert.Throws<CryptographicException>(
                    () => Pkcs8PrivateKeyInfo.DecryptAndDecode((ReadOnlySpan<char>)"initial", encryptedKey.AsMemory(1), out _));

                Assert.Throws<CryptographicException>(
                    () => Pkcs8PrivateKeyInfo.DecryptAndDecode((ReadOnlySpan<char>)"initial", encryptedKey.AsMemory(0, encryptedKey.Length - 1), out _));

                Pkcs8PrivateKeyInfo privateKey = Pkcs8PrivateKeyInfo.DecryptAndDecode(
                    (ReadOnlySpan<char>)nameof(DecryptionFailures),
                    encryptedKey,
                    out int bytesRead);

                Assert.NotNull(privateKey);
                Assert.Equal(encryptedKey.Length, bytesRead);
            }
        }

        [Fact]
        public static void ReencryptAndImport()
        {
            byte[] secret = { 42 };
            ReadOnlyMemory<byte> encryptedKey;
            byte[] encryptedData;
            byte[] newKey = { 1, 1, 2, 3, 5, 8 };
            byte[] buf = new byte[2048];
            RSAEncryptionPadding encryptionPadding = RSAEncryptionPadding.OaepSHA256;

            using (RSA rsa = RSA.Create())
            {
                encryptedData = rsa.Encrypt(secret, encryptionPadding);

                bool exported = rsa.TryExportEncryptedPkcs8PrivateKey(
                    (ReadOnlySpan<char>)"initial",
                    new PbeParameters(
                        PbeEncryptionAlgorithm.TripleDes3KeyPkcs12,
                        HashAlgorithmName.SHA1,
                        1024),
                    buf,
                    out int bytesWritten);

                Assert.True(exported, "Key exported");

                Pkcs8PrivateKeyInfo privateKey = Pkcs8PrivateKeyInfo.DecryptAndDecode(
                    (ReadOnlySpan<char>)"initial",
                    buf,
                    out int bytesRead);

                Assert.NotNull(privateKey);
                Assert.Equal(bytesWritten, bytesRead);

                exported = privateKey.TryEncrypt(
                    newKey,
                    new PbeParameters(
                        PbeEncryptionAlgorithm.Aes256Cbc,
                        HashAlgorithmName.SHA512,
                        5678),
                    buf,
                    out bytesWritten);

                Assert.True(exported, "Re-encrypted into the buffers");
                encryptedKey = buf.AsMemory(0, bytesWritten);
            }

            using (RSA rsa = RSA.Create())
            {
                rsa.ImportEncryptedPkcs8PrivateKey(newKey, buf, out int bytesRead);
                Assert.Equal(encryptedKey.Length, bytesRead);

                byte[] decrypted = rsa.Decrypt(encryptedData, encryptionPadding);
                Assert.Equal(secret.ByteArrayToHex(), decrypted.ByteArrayToHex());
            }
        }

        [Fact]
        public static void CreateNull()
        {
            AssertExtensions.Throws<ArgumentNullException>(
                "privateKey",
                () => Pkcs8PrivateKeyInfo.Create(null));
        }

        [Fact]
        public static void NullAlgorithm()
        {
            AssertExtensions.Throws<ArgumentNullException>(
                "algorithmId",
                () => new Pkcs8PrivateKeyInfo(null, null, ReadOnlyMemory<byte>.Empty));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("00.0")]
        [InlineData("0....0")]
        [InlineData("0.0...0")]
        [InlineData("potato")]
        public static void InvalidAlgorithmId(string oidValue)
        {
            Pkcs8PrivateKeyInfo info = new Pkcs8PrivateKeyInfo(
                new Oid(oidValue, "friendly name"),
                null,
                ReadOnlyMemory<byte>.Empty);

            Assert.ThrowsAny<CryptographicException>(() => info.Encode());

            Assert.ThrowsAny<CryptographicException>(
                () => info.TryEncode(Span<byte>.Empty, out _));

            PbeParameters pbeParameters = new PbeParameters(
                PbeEncryptionAlgorithm.Aes128Cbc,
                HashAlgorithmName.SHA256,
                1024);

            Assert.ThrowsAny<CryptographicException>(() => info.Encrypt((ReadOnlySpan<char>)"hi", pbeParameters));
            Assert.ThrowsAny<CryptographicException>(() => info.Encrypt(new byte[3], pbeParameters));

            Assert.ThrowsAny<CryptographicException>(
                () => info.TryEncrypt((ReadOnlySpan<char>)"hello", pbeParameters, Span<byte>.Empty, out _));

            Assert.ThrowsAny<CryptographicException>(
                () => info.TryEncrypt(new byte[3], pbeParameters, Span<byte>.Empty, out _));
        }

        [Fact]
        public static void PrivateKeyIsAnyValue()
        {
            byte[] keyBytes = { 1, 2, 3, 4, 5 };

            Pkcs8PrivateKeyInfo info = new Pkcs8PrivateKeyInfo(
                new Oid("0.0", null),
                algorithmParameters: null,
                keyBytes,
                skipCopies: true);

            Assert.True(info.PrivateKeyBytes.Span.SequenceEqual(keyBytes));

            byte[] encoded = info.Encode();

            Pkcs8PrivateKeyInfo decoded = Pkcs8PrivateKeyInfo.Decode(encoded, out _, skipCopy: true);

            Assert.True(decoded.PrivateKeyBytes.Span.SequenceEqual(keyBytes));
        }

        [Fact]
        public static void EmptyPrivateKey()
        {
            byte[] keyBytes = Array.Empty<byte>();

            Pkcs8PrivateKeyInfo info = new Pkcs8PrivateKeyInfo(
                new Oid("0.0", null),
                algorithmParameters: null,
                keyBytes,
                skipCopies: true);

            Assert.True(info.PrivateKeyBytes.Span.SequenceEqual(keyBytes));

            byte[] encoded = info.Encode();

            Pkcs8PrivateKeyInfo decoded = Pkcs8PrivateKeyInfo.Decode(encoded, out _, skipCopy: true);

            Assert.True(decoded.PrivateKeyBytes.Span.SequenceEqual(keyBytes));
        }

        [Theory]
        [InlineData("0102030405")]
        [InlineData("05000500")]
        public static void AlgorithmParametersIsSingleBer(string parametersHex)
        {
            byte[] keyBytes = Array.Empty<byte>();
            byte[] parameterBytes = parametersHex.HexToByteArray();

            Assert.Throws<CryptographicException>(
                () => new Pkcs8PrivateKeyInfo(
                    new Oid("0.0", null),
                    parameterBytes,
                    keyBytes,
                    skipCopies: true));
        }

        [Fact]
        public static void OmitEmptyAlgorithmParameters()
        {
            byte[] encoded = new Pkcs8PrivateKeyInfo(
                new Oid("0.0", null),
                ReadOnlyMemory<byte>.Empty,
                Array.Empty<byte>(),
                skipCopies: true).Encode();

            Assert.Equal("300A02010030030601000400", encoded.ByteArrayToHex());

            Pkcs8PrivateKeyInfo decoded = Pkcs8PrivateKeyInfo.Decode(encoded, out _, skipCopy: true);
            Assert.False(decoded.AlgorithmParameters.HasValue);
        }
    }
}
