// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Formats.Asn1;
using System.Runtime.CompilerServices;
using Xunit;
using Xunit.Sdk;

namespace System.Security.Cryptography.Tests
{
    // Tests the contract of X25519DiffieHellman implementations and how they interact with each other. This does not
    // require a functional implementation of X25519, it tests how public members use protected Core members, argument
    // validation, and disposal.
    public static class X25519DiffieHellmanContractTests
    {
        private static readonly PbeParameters s_aes128Pbe = new(PbeEncryptionAlgorithm.Aes128Cbc, HashAlgorithmName.SHA256, 2);

        private const string EncryptedPrivateKeyPassword = "PLACEHOLDER";
        private static ReadOnlySpan<byte> EncryptedPrivateKeyPasswordBytes => "PLACEHOLDER"u8;

        // A valid PKCS#8 PrivateKeyInfo for X25519 with an all-0x42 private key.
        private static ReadOnlySpan<byte> TestPkcs8PrivateKey =>
        [
            0x30, 0x2E, 0x02, 0x01, 0x00, 0x30, 0x05, 0x06, 0x03, 0x2B, 0x65, 0x6E,
            0x04, 0x22, 0x04, 0x20,
            0x42, 0x42, 0x42, 0x42, 0x42, 0x42, 0x42, 0x42, 0x42, 0x42, 0x42, 0x42, 0x42, 0x42, 0x42, 0x42,
            0x42, 0x42, 0x42, 0x42, 0x42, 0x42, 0x42, 0x42, 0x42, 0x42, 0x42, 0x42, 0x42, 0x42, 0x42, 0x42,
        ];

        [Theory]
        [InlineData(1)]
        [InlineData(7)]
        public static void Dispose_OnDisposing(int disposeCalls)
        {
            int count = 0;
            X25519DiffieHellmanContract xdh = new()
            {
                OnDispose = (bool disposing) =>
                {
                    count++;
                    AssertExtensions.TrueExpression(disposing);
                }
            };

            for (int i = 0; i < disposeCalls; i++)
            {
                xdh.Dispose();
            }

            Assert.Equal(1, count);
        }

        [Fact]
        public static void DeriveRawSecretAgreement_Allocated_NullOtherParty()
        {
            using X25519DiffieHellmanContract xdh = new();
            AssertExtensions.Throws<ArgumentNullException>("otherParty", () =>
                xdh.DeriveRawSecretAgreement((X25519DiffieHellman)null));
        }

        [Fact]
        public static void DeriveRawSecretAgreement_Exact_NullOtherParty()
        {
            using X25519DiffieHellmanContract xdh = new();
            AssertExtensions.Throws<ArgumentNullException>("otherParty", () =>
                xdh.DeriveRawSecretAgreement(null, new byte[X25519DiffieHellman.SecretAgreementSizeInBytes]));
        }

        [Fact]
        public static void DeriveRawSecretAgreement_Exact_WrongDestinationLength()
        {
            using X25519DiffieHellmanContract xdh = new();
            using X25519DiffieHellmanContract other = new();

            AssertExtensions.Throws<ArgumentException>("destination", () =>
                xdh.DeriveRawSecretAgreement(other, new byte[X25519DiffieHellman.SecretAgreementSizeInBytes - 1]));

            AssertExtensions.Throws<ArgumentException>("destination", () =>
                xdh.DeriveRawSecretAgreement(other, new byte[X25519DiffieHellman.SecretAgreementSizeInBytes + 1]));
        }

        [Fact]
        public static void DeriveRawSecretAgreement_Allocated_Disposed()
        {
            X25519DiffieHellmanContract xdh = new();
            xdh.Dispose();
            using X25519DiffieHellmanContract other = new();
            Assert.Throws<ObjectDisposedException>(() => xdh.DeriveRawSecretAgreement(other));
        }

        [Fact]
        public static void DeriveRawSecretAgreement_Exact_Disposed()
        {
            X25519DiffieHellmanContract xdh = new();
            xdh.Dispose();
            using X25519DiffieHellmanContract other = new();
            Assert.Throws<ObjectDisposedException>(() =>
                xdh.DeriveRawSecretAgreement(other, new byte[X25519DiffieHellman.SecretAgreementSizeInBytes]));
        }

        [Fact]
        public static void DeriveRawSecretAgreement_Allocated_Works()
        {
            using X25519DiffieHellmanContract other = new();
            using X25519DiffieHellmanContract xdh = new()
            {
                OnDeriveRawSecretAgreementCore = (X25519DiffieHellman otherParty, Span<byte> destination) =>
                {
                    Assert.Same(other, otherParty);
                    destination.Fill(0xAA);
                }
            };

            byte[] agreement = xdh.DeriveRawSecretAgreement(other);
            Assert.Equal(X25519DiffieHellman.SecretAgreementSizeInBytes, agreement.Length);
            AssertExtensions.FilledWith<byte>(0xAA, agreement);
        }

        [Fact]
        public static void DeriveRawSecretAgreement_Exact_Works()
        {
            byte[] buffer = new byte[X25519DiffieHellman.SecretAgreementSizeInBytes];
            using X25519DiffieHellmanContract other = new();
            using X25519DiffieHellmanContract xdh = new()
            {
                OnDeriveRawSecretAgreementCore = (X25519DiffieHellman otherParty, Span<byte> destination) =>
                {
                    Assert.Same(other, otherParty);
                    AssertExtensions.Same(buffer, destination);
                }
            };

            xdh.DeriveRawSecretAgreement(other, buffer);
        }

        [Fact]
        public static void ExportPrivateKey_Exact_WrongSize()
        {
            using X25519DiffieHellmanContract xdh = new();
            AssertExtensions.Throws<ArgumentException>("destination", () =>
                xdh.ExportPrivateKey(new byte[X25519DiffieHellman.PrivateKeySizeInBytes - 1]));
            AssertExtensions.Throws<ArgumentException>("destination", () =>
                xdh.ExportPrivateKey(new byte[X25519DiffieHellman.PrivateKeySizeInBytes + 1]));
        }

        [Fact]
        public static void ExportPrivateKey_Exact_Disposed()
        {
            X25519DiffieHellmanContract xdh = new();
            xdh.Dispose();
            Assert.Throws<ObjectDisposedException>(() =>
                xdh.ExportPrivateKey(new byte[X25519DiffieHellman.PrivateKeySizeInBytes]));
        }

        [Fact]
        public static void ExportPrivateKey_Exact_Works()
        {
            byte[] buffer = new byte[X25519DiffieHellman.PrivateKeySizeInBytes];
            using X25519DiffieHellmanContract xdh = new()
            {
                OnExportPrivateKeyCore = (Span<byte> destination) =>
                {
                    AssertExtensions.Same(buffer, destination);
                }
            };

            xdh.ExportPrivateKey(buffer);
        }

        [Fact]
        public static void ExportPrivateKey_Allocated_Disposed()
        {
            X25519DiffieHellmanContract xdh = new();
            xdh.Dispose();
            Assert.Throws<ObjectDisposedException>(() => xdh.ExportPrivateKey());
        }

        [Fact]
        public static void ExportPrivateKey_Allocated_Works()
        {
            using X25519DiffieHellmanContract xdh = new()
            {
                OnExportPrivateKeyCore = (Span<byte> destination) =>
                {
                    destination.Fill(0x42);
                }
            };

            byte[] exported = xdh.ExportPrivateKey();
            Assert.Equal(X25519DiffieHellman.PrivateKeySizeInBytes, exported.Length);
            AssertExtensions.FilledWith<byte>(0x42, exported);
        }

        [Fact]
        public static void ExportPublicKey_Exact_WrongSize()
        {
            using X25519DiffieHellmanContract xdh = new();
            AssertExtensions.Throws<ArgumentException>("destination", () =>
                xdh.ExportPublicKey(new byte[X25519DiffieHellman.PublicKeySizeInBytes - 1]));
            AssertExtensions.Throws<ArgumentException>("destination", () =>
                xdh.ExportPublicKey(new byte[X25519DiffieHellman.PublicKeySizeInBytes + 1]));
        }

        [Fact]
        public static void ExportPublicKey_Exact_Disposed()
        {
            X25519DiffieHellmanContract xdh = new();
            xdh.Dispose();
            Assert.Throws<ObjectDisposedException>(() =>
                xdh.ExportPublicKey(new byte[X25519DiffieHellman.PublicKeySizeInBytes]));
        }

        [Fact]
        public static void ExportPublicKey_Exact_Works()
        {
            byte[] buffer = new byte[X25519DiffieHellman.PublicKeySizeInBytes];
            using X25519DiffieHellmanContract xdh = new()
            {
                OnExportPublicKeyCore = (Span<byte> destination) =>
                {
                    AssertExtensions.Same(buffer, destination);
                }
            };

            xdh.ExportPublicKey(buffer);
        }

        [Fact]
        public static void ExportPublicKey_Allocated_Disposed()
        {
            X25519DiffieHellmanContract xdh = new();
            xdh.Dispose();
            Assert.Throws<ObjectDisposedException>(() => xdh.ExportPublicKey());
        }

        [Fact]
        public static void ExportPublicKey_Allocated_Works()
        {
            using X25519DiffieHellmanContract xdh = new()
            {
                OnExportPublicKeyCore = (Span<byte> destination) =>
                {
                    destination.Fill(0x42);
                }
            };

            byte[] exported = xdh.ExportPublicKey();
            Assert.Equal(X25519DiffieHellman.PublicKeySizeInBytes, exported.Length);
            AssertExtensions.FilledWith<byte>(0x42, exported);
        }

        [Fact]
        public static void TryExportSubjectPublicKeyInfo_Buffers()
        {
            using X25519DiffieHellmanContract xdh = new()
            {
                OnExportPublicKeyCore = (Span<byte> destination) =>
                {
                    destination.Fill(0x42);
                    Assert.Equal(X25519DiffieHellman.PublicKeySizeInBytes, destination.Length);
                }
            };

            byte[] destination = new byte[256];
            destination.AsSpan().Fill(0xFF);
            AssertExtensions.TrueExpression(xdh.TryExportSubjectPublicKeyInfo(destination, out int written));
            ReadSubjectPublicKeyInfo(
                destination.AsMemory(0, written),
                out string oid,
                out ReadOnlyMemory<byte>? parameters,
                out ReadOnlyMemory<byte> publicKey);

            Assert.Equal("1.3.101.110", oid);
            AssertExtensions.FalseExpression(parameters.HasValue);
            AssertExtensions.FilledWith<byte>(0x42, publicKey.Span);
            AssertExtensions.FilledWith<byte>(0xFF, destination.AsSpan(written));
        }

        [Fact]
        public static void TryExportSubjectPublicKeyInfo_Disposed()
        {
            X25519DiffieHellmanContract xdh = new();
            xdh.Dispose();
            Assert.Throws<ObjectDisposedException>(() => xdh.TryExportSubjectPublicKeyInfo([], out _));
        }

        [Fact]
        public static void TryExportSubjectPublicKeyInfo_DestinationTooSmall()
        {
            using X25519DiffieHellmanContract xdh = new();

            byte[] destination = new byte[1];
            AssertExtensions.FalseExpression(xdh.TryExportSubjectPublicKeyInfo(destination, out int written));
            Assert.Equal(0, written);
        }

        [Fact]
        public static void ExportSubjectPublicKeyInfo_Allocated()
        {
            using X25519DiffieHellmanContract xdh = new()
            {
                OnExportPublicKeyCore = (Span<byte> destination) =>
                {
                    destination.Fill(0x42);
                    Assert.Equal(X25519DiffieHellman.PublicKeySizeInBytes, destination.Length);
                }
            };

            byte[] spki = xdh.ExportSubjectPublicKeyInfo();
            ReadSubjectPublicKeyInfo(
                spki,
                out string oid,
                out ReadOnlyMemory<byte>? parameters,
                out ReadOnlyMemory<byte> publicKey);

            Assert.Equal("1.3.101.110", oid);
            AssertExtensions.FalseExpression(parameters.HasValue);
            AssertExtensions.FilledWith<byte>(0x42, publicKey.Span);
        }

        [Fact]
        public static void ExportSubjectPublicKeyInfoPem()
        {
            using X25519DiffieHellmanContract xdh = new()
            {
                OnExportPublicKeyCore = (Span<byte> destination) =>
                {
                    destination.Fill(0x42);
                    Assert.Equal(X25519DiffieHellman.PublicKeySizeInBytes, destination.Length);
                }
            };

            string spkiPem = xdh.ExportSubjectPublicKeyInfoPem();
            const string ExpectedPem =
                "-----BEGIN PUBLIC KEY-----\n" +
                "MCowBQYDK2VuAyEAQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkI=\n" +
                "-----END PUBLIC KEY-----";
            Assert.Equal(ExpectedPem, spkiPem);
        }

        [Fact]
        public static void ExportSubjectPublicKeyInfo_Disposed()
        {
            X25519DiffieHellmanContract xdh = new();
            xdh.Dispose();
            Assert.Throws<ObjectDisposedException>(() => xdh.ExportSubjectPublicKeyInfo());
        }

        [Fact]
        public static void TryExportPkcs8PrivateKey_EarlyExitForSmallBuffer()
        {
            X25519DiffieHellmanContract xdh = new();
            byte[] destination = new byte[47];
            AssertExtensions.FalseExpression(xdh.TryExportPkcs8PrivateKey(destination, out int written));
            Assert.Equal(0, written);
        }

        [Fact]
        public static void TryExportPkcs8PrivateKey()
        {
            // Test with various inputs of different sizes from TryExportPkcs8PrivateKeyCore that it reports
            // as-is to the public APIs. Invalid behavior like reporting more byte written than possible is handled
            // elsewhere.
            int bufferSize = Random.Shared.Next(50, 1024);
            int writtenSize = Random.Shared.Next(48, bufferSize);
            bool success = (writtenSize & 1) == 1;
            byte[] buffer = new byte[bufferSize];
            X25519DiffieHellmanContract xdh = new()
            {
                OnTryExportPkcs8PrivateKeyCore = (Span<byte> destination, out int bytesWritten) =>
                {
                    AssertExtensions.Same(buffer, destination);
                    bytesWritten = writtenSize;
                    return success;
                }
            };

            AssertExtensions.TrueExpression(success == xdh.TryExportPkcs8PrivateKey(buffer, out int written));
            Assert.Equal(writtenSize, written);
        }

        [Theory]
        [InlineData(true, 0)]
        [InlineData(true, -1)]
        [InlineData(true, int.MaxValue)]
        [InlineData(true, 65)]
        [InlineData(false, 0)]
        [InlineData(false, -1)]
        [InlineData(false, int.MaxValue)]
        public static void TryExportPkcs8PrivateKey_GarbageInGarbageOut(bool coreReturn, int coreBytesWritten)
        {
            byte[] buffer = new byte[64];

            using X25519DiffieHellmanContract xdh = new()
            {
                OnTryExportPkcs8PrivateKeyCore = (Span<byte> destination, out int bytesWritten) =>
                {
                    AssertExtensions.Same(buffer, destination);
                    bytesWritten = coreBytesWritten;
                    return coreReturn;
                }
            };

            bool publicReturn = xdh.TryExportPkcs8PrivateKey(buffer, out int publicBytesWritten);
            Assert.Equal(coreReturn, publicReturn);
            Assert.Equal(coreBytesWritten, publicBytesWritten);
            Assert.Equal(1, xdh.TryExportPkcs8PrivateKeyCoreCount);
        }

        [Fact]
        public static void ExportPkcs8PrivateKey_OneExportCall()
        {
            int size = -1;
            X25519DiffieHellmanContract xdh = new()
            {
                OnTryExportPkcs8PrivateKeyCore = (Span<byte> destination, out int bytesWritten) =>
                {
                    destination.Fill(0x88);
                    bytesWritten = destination.Length;
                    size = destination.Length;
                    return true;
                }
            };

            byte[] exported = xdh.ExportPkcs8PrivateKey();
            AssertExtensions.FilledWith<byte>(0x88, exported);
            Assert.Equal(size, exported.Length);
            Assert.Equal(1, xdh.TryExportPkcs8PrivateKeyCoreCount);
        }

        [Fact]
        public static void ExportPkcs8PrivateKey_ExpandAndRetry()
        {
            const int TargetSize = 4567;
            X25519DiffieHellmanContract xdh = new()
            {
                OnTryExportPkcs8PrivateKeyCore = (Span<byte> destination, out int bytesWritten) =>
                {
                    if (destination.Length < TargetSize)
                    {
                        bytesWritten = 0;
                        return false;
                    }

                    destination.Fill(0x88);
                    bytesWritten = TargetSize;
                    return true;
                }
            };

            byte[] exported = xdh.ExportPkcs8PrivateKey();
            AssertExtensions.FilledWith<byte>(0x88, exported);
            Assert.Equal(TargetSize, exported.Length);
            AssertExtensions.GreaterThan(xdh.TryExportPkcs8PrivateKeyCoreCount, 1);
        }

        [Fact]
        public static void ExportPkcs8PrivateKey_MisbehavingBytesWritten_Oversized()
        {
            X25519DiffieHellmanContract xdh = new()
            {
                OnTryExportPkcs8PrivateKeyCore = (Span<byte> destination, out int bytesWritten) =>
                {
                    bytesWritten = destination.Length + 1;
                    return true;
                }
            };

            Assert.Throws<CryptographicException>(() => xdh.ExportPkcs8PrivateKey());
        }

        [Fact]
        public static void ExportPkcs8PrivateKey_MisbehavingBytesWritten_Negative()
        {
            X25519DiffieHellmanContract xdh = new()
            {
                OnTryExportPkcs8PrivateKeyCore = (Span<byte> destination, out int bytesWritten) =>
                {
                    bytesWritten = -1;
                    return true;
                }
            };

            Assert.Throws<CryptographicException>(() => xdh.ExportPkcs8PrivateKey());
        }

        [Fact]
        public static void ExportPkcs8PrivateKey_Disposed()
        {
            X25519DiffieHellmanContract xdh = new();
            xdh.Dispose();
            Assert.Throws<ObjectDisposedException>(() => xdh.ExportPkcs8PrivateKey());
            Assert.Throws<ObjectDisposedException>(() => xdh.TryExportPkcs8PrivateKey(new byte[512], out _));
        }

        [Fact]
        public static void ExportPkcs8PrivateKeyPem()
        {
            using X25519DiffieHellmanContract xdh = new()
            {
                OnTryExportPkcs8PrivateKeyCore = (Span<byte> destination, out int bytesWritten) =>
                {
                    if (TestPkcs8PrivateKey.TryCopyTo(destination))
                    {
                        bytesWritten = TestPkcs8PrivateKey.Length;
                        return true;
                    }

                    bytesWritten = 0;
                    return false;
                }
            };

            string pem = xdh.ExportPkcs8PrivateKeyPem();
            byte[] pkcs8 = xdh.ExportPkcs8PrivateKey();
            PemFields fields = PemEncoding.Find(pem.AsSpan());
            Assert.Equal(Index.FromStart(0), fields.Location.Start);
            Assert.Equal(Index.FromStart(pem.Length), fields.Location.End);
            Assert.Equal("PRIVATE KEY", pem.AsSpan()[fields.Label].ToString());
            AssertExtensions.SequenceEqual(pkcs8, Convert.FromBase64String(pem.AsSpan()[fields.Base64Data].ToString()));
        }

        [Fact]
        public static void ExportEncryptedPkcs8PrivateKey_Disposed()
        {
            X25519DiffieHellmanContract xdh = new();
            xdh.Dispose();

            Assert.Throws<ObjectDisposedException>(() => xdh.TryExportEncryptedPkcs8PrivateKey(
                EncryptedPrivateKeyPassword.AsSpan(), s_aes128Pbe, new byte[2048], out _));
            Assert.Throws<ObjectDisposedException>(() => xdh.TryExportEncryptedPkcs8PrivateKey(
                EncryptedPrivateKeyPassword, s_aes128Pbe, new byte[2048], out _));
            Assert.Throws<ObjectDisposedException>(() => xdh.TryExportEncryptedPkcs8PrivateKey(
                EncryptedPrivateKeyPasswordBytes, s_aes128Pbe, new byte[2048], out _));

            Assert.Throws<ObjectDisposedException>(() => xdh.ExportEncryptedPkcs8PrivateKey(
                EncryptedPrivateKeyPassword, s_aes128Pbe));
            Assert.Throws<ObjectDisposedException>(() => xdh.ExportEncryptedPkcs8PrivateKey(
                EncryptedPrivateKeyPassword.AsSpan(), s_aes128Pbe));
            Assert.Throws<ObjectDisposedException>(() => xdh.ExportEncryptedPkcs8PrivateKey(
                EncryptedPrivateKeyPasswordBytes, s_aes128Pbe));
        }

        [Theory]
        [InlineData(TryExportPkcs8PasswordKind.StringPassword)]
        [InlineData(TryExportPkcs8PasswordKind.SpanOfBytesPassword)]
        [InlineData(TryExportPkcs8PasswordKind.SpanOfCharsPassword)]
        [SkipOnPlatform(TestPlatforms.Browser, "Browser does not support symmetric encryption")]
        public static void TryExportEncryptedPkcs8PrivateKey_ExportsPkcs8(TryExportPkcs8PasswordKind kind)
        {
            using X25519DiffieHellmanContract xdh = new()
            {
                OnTryExportPkcs8PrivateKeyCore = (Span<byte> destination, out int bytesWritten) =>
                {
                    if (TestPkcs8PrivateKey.TryCopyTo(destination))
                    {
                        bytesWritten = TestPkcs8PrivateKey.Length;
                        return true;
                    }

                    Assert.Fail("Initial buffer was not correctly sized.");
                    bytesWritten = 0;
                    return false;
                }
            };

            byte[] buffer = new byte[2048];
            bool success = TryExportEncryptedPkcs8PrivateKeyByKind(xdh, kind, buffer, out int written);
            AssertExtensions.TrueExpression(success);
            AssertExtensions.GreaterThan(written, 0);
            Assert.Equal(1, xdh.TryExportPkcs8PrivateKeyCoreCount);
        }

        [Theory]
        [InlineData(TryExportPkcs8PasswordKind.StringPassword)]
        [InlineData(TryExportPkcs8PasswordKind.SpanOfBytesPassword)]
        [InlineData(TryExportPkcs8PasswordKind.SpanOfCharsPassword)]
        [SkipOnPlatform(TestPlatforms.Browser, "Browser does not support symmetric encryption")]
        public static void TryExportEncryptedPkcs8PrivateKey_InnerBuffer_LargePkcs8(TryExportPkcs8PasswordKind kind)
        {
            using X25519DiffieHellmanContract xdh = new();
            xdh.OnTryExportPkcs8PrivateKeyCore = (Span<byte> destination, out int bytesWritten) =>
            {
                if (xdh.TryExportPkcs8PrivateKeyCoreCount < 2)
                {
                    bytesWritten = 0;
                    return false;
                }

                if (TestPkcs8PrivateKey.TryCopyTo(destination))
                {
                    bytesWritten = TestPkcs8PrivateKey.Length;
                    return true;
                }

                bytesWritten = 0;
                return false;
            };

            byte[] buffer = new byte[2048];
            bool success = TryExportEncryptedPkcs8PrivateKeyByKind(xdh, kind, buffer, out int written);
            AssertExtensions.TrueExpression(success);
            AssertExtensions.GreaterThan(written, 0);
            Assert.Equal(2, xdh.TryExportPkcs8PrivateKeyCoreCount);
        }

        [Theory]
        [InlineData(TryExportPkcs8PasswordKind.StringPassword)]
        [InlineData(TryExportPkcs8PasswordKind.SpanOfBytesPassword)]
        [InlineData(TryExportPkcs8PasswordKind.SpanOfCharsPassword)]
        [SkipOnPlatform(TestPlatforms.Browser, "Browser does not support symmetric encryption")]
        public static void TryExportEncryptedPkcs8PrivateKey_DestinationTooSmall(TryExportPkcs8PasswordKind kind)
        {
            using X25519DiffieHellmanContract xdh = new()
            {
                OnTryExportPkcs8PrivateKeyCore = (Span<byte> destination, out int bytesWritten) =>
                {
                    if (TestPkcs8PrivateKey.TryCopyTo(destination))
                    {
                        bytesWritten = TestPkcs8PrivateKey.Length;
                        return true;
                    }

                    bytesWritten = 0;
                    return false;
                }
            };

            byte[] buffer = new byte[3];
            bool success = TryExportEncryptedPkcs8PrivateKeyByKind(xdh, kind, buffer, out int written);
            AssertExtensions.FalseExpression(success);
            Assert.Equal(0, written);
        }

        [Fact]
        public static void ExportEncryptedPkcs8PrivateKey_ValidatesPbeParameters_Bad3DESHash()
        {
            byte[] buffer = new byte[2048];
            PbeParameters pbeParameters = new(PbeEncryptionAlgorithm.TripleDes3KeyPkcs12, HashAlgorithmName.SHA256, 3);
            using X25519DiffieHellmanContract xdh = new();
            Assert.Throws<CryptographicException>(() =>
                xdh.TryExportEncryptedPkcs8PrivateKey(EncryptedPrivateKeyPassword, pbeParameters, buffer, out _));
            Assert.Throws<CryptographicException>(() =>
                xdh.TryExportEncryptedPkcs8PrivateKey(EncryptedPrivateKeyPassword.AsSpan(), pbeParameters, buffer, out _));
            Assert.Throws<CryptographicException>(() =>
                xdh.TryExportEncryptedPkcs8PrivateKey(EncryptedPrivateKeyPasswordBytes, pbeParameters, buffer, out _));
            Assert.Throws<CryptographicException>(() =>
                xdh.ExportEncryptedPkcs8PrivateKey(EncryptedPrivateKeyPassword, pbeParameters));
            Assert.Throws<CryptographicException>(() =>
                xdh.ExportEncryptedPkcs8PrivateKey(EncryptedPrivateKeyPassword.AsSpan(), pbeParameters));
            Assert.Throws<CryptographicException>(() =>
                xdh.ExportEncryptedPkcs8PrivateKey(EncryptedPrivateKeyPasswordBytes, pbeParameters));
        }

        [Fact]
        public static void ExportEncryptedPkcs8PrivateKey_ValidatesPbeParameters_3DESRequiresChar()
        {
            byte[] buffer = new byte[2048];
            PbeParameters pbeParameters = new(PbeEncryptionAlgorithm.TripleDes3KeyPkcs12, HashAlgorithmName.SHA1, 3);
            using X25519DiffieHellmanContract xdh = new();
            Assert.Throws<CryptographicException>(() =>
                xdh.TryExportEncryptedPkcs8PrivateKey(EncryptedPrivateKeyPasswordBytes, pbeParameters, buffer, out _));
            Assert.Throws<CryptographicException>(() =>
                xdh.ExportEncryptedPkcs8PrivateKey(EncryptedPrivateKeyPasswordBytes, pbeParameters));
            Assert.Throws<CryptographicException>(() =>
                xdh.ExportEncryptedPkcs8PrivateKeyPem(EncryptedPrivateKeyPasswordBytes, pbeParameters));
        }

        [Fact]
        public static void ExportEncryptedPkcs8PrivateKey_NullArgs()
        {
            byte[] buffer = new byte[2048];
            using X25519DiffieHellmanContract xdh = new();
            AssertExtensions.Throws<ArgumentNullException>("pbeParameters", () =>
                xdh.TryExportEncryptedPkcs8PrivateKey(EncryptedPrivateKeyPassword, pbeParameters: null, buffer, out _));
            AssertExtensions.Throws<ArgumentNullException>("pbeParameters", () =>
                xdh.TryExportEncryptedPkcs8PrivateKey(EncryptedPrivateKeyPassword.AsSpan(), pbeParameters: null, buffer, out _));
            AssertExtensions.Throws<ArgumentNullException>("pbeParameters", () =>
                xdh.TryExportEncryptedPkcs8PrivateKey(EncryptedPrivateKeyPasswordBytes, pbeParameters: null, buffer, out _));
            AssertExtensions.Throws<ArgumentNullException>("password", () =>
                xdh.TryExportEncryptedPkcs8PrivateKey((string)null, s_aes128Pbe, buffer, out _));

            AssertExtensions.Throws<ArgumentNullException>("pbeParameters", () =>
                xdh.ExportEncryptedPkcs8PrivateKey(EncryptedPrivateKeyPassword, pbeParameters: null));
            AssertExtensions.Throws<ArgumentNullException>("pbeParameters", () =>
                xdh.ExportEncryptedPkcs8PrivateKey(EncryptedPrivateKeyPassword.AsSpan(), pbeParameters: null));
            AssertExtensions.Throws<ArgumentNullException>("pbeParameters", () =>
                xdh.ExportEncryptedPkcs8PrivateKey(EncryptedPrivateKeyPasswordBytes, pbeParameters: null));

            AssertExtensions.Throws<ArgumentNullException>("password", () =>
                xdh.ExportEncryptedPkcs8PrivateKey((string)null, s_aes128Pbe));

            AssertExtensions.Throws<ArgumentNullException>("password", () =>
                xdh.ExportEncryptedPkcs8PrivateKeyPem((string)null, s_aes128Pbe));
            AssertExtensions.Throws<ArgumentNullException>("pbeParameters", () =>
                xdh.ExportEncryptedPkcs8PrivateKeyPem(EncryptedPrivateKeyPassword, pbeParameters: null));
            AssertExtensions.Throws<ArgumentNullException>("pbeParameters", () =>
                xdh.ExportEncryptedPkcs8PrivateKeyPem(EncryptedPrivateKeyPassword.AsSpan(), pbeParameters: null));
            AssertExtensions.Throws<ArgumentNullException>("pbeParameters", () =>
                xdh.ExportEncryptedPkcs8PrivateKeyPem(EncryptedPrivateKeyPasswordBytes, pbeParameters: null));
        }

        [Fact]
        public static void ExportEncryptedPkcs8PrivateKeyPem_Disposed()
        {
            X25519DiffieHellmanContract xdh = new();
            xdh.Dispose();
            Assert.Throws<ObjectDisposedException>(() =>
                xdh.ExportEncryptedPkcs8PrivateKeyPem(EncryptedPrivateKeyPassword, s_aes128Pbe));
            Assert.Throws<ObjectDisposedException>(() =>
                xdh.ExportEncryptedPkcs8PrivateKeyPem(EncryptedPrivateKeyPassword.AsSpan(), s_aes128Pbe));
            Assert.Throws<ObjectDisposedException>(() =>
                xdh.ExportEncryptedPkcs8PrivateKeyPem(EncryptedPrivateKeyPasswordBytes, s_aes128Pbe));
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser, "Browser does not support symmetric encryption")]
        public static void ExportEncryptedPkcs8PrivateKeyPem_Works()
        {
            using X25519DiffieHellmanContract xdh = new()
            {
                OnTryExportPkcs8PrivateKeyCore = (Span<byte> destination, out int bytesWritten) =>
                {
                    if (TestPkcs8PrivateKey.TryCopyTo(destination))
                    {
                        bytesWritten = TestPkcs8PrivateKey.Length;
                        return true;
                    }

                    bytesWritten = 0;
                    return false;
                }
            };

            string pem = xdh.ExportEncryptedPkcs8PrivateKeyPem(EncryptedPrivateKeyPassword, s_aes128Pbe);
            AssertPem(pem);
            pem = xdh.ExportEncryptedPkcs8PrivateKeyPem(EncryptedPrivateKeyPasswordBytes, s_aes128Pbe);
            AssertPem(pem);
            pem = xdh.ExportEncryptedPkcs8PrivateKeyPem(EncryptedPrivateKeyPassword.AsSpan(), s_aes128Pbe);
            AssertPem(pem);

            static void AssertPem(string pem)
            {
                PemFields fields = PemEncoding.Find(pem.AsSpan());
                Assert.Equal(Index.FromStart(0), fields.Location.Start);
                Assert.Equal(Index.FromStart(pem.Length), fields.Location.End);
                Assert.Equal("ENCRYPTED PRIVATE KEY", pem.AsSpan()[fields.Label].ToString());
            }
        }

        [Fact]
        public static void ExportSubjectPublicKeyInfoPem_Disposed()
        {
            X25519DiffieHellmanContract xdh = new();
            xdh.Dispose();
            Assert.Throws<ObjectDisposedException>(() => xdh.ExportSubjectPublicKeyInfoPem());
        }

        [Fact]
        public static void ExportPkcs8PrivateKeyPem_Disposed()
        {
            X25519DiffieHellmanContract xdh = new();
            xdh.Dispose();
            Assert.Throws<ObjectDisposedException>(() => xdh.ExportPkcs8PrivateKeyPem());
        }

        private static void ReadSubjectPublicKeyInfo(
            ReadOnlyMemory<byte> source,
            out string oid,
            out ReadOnlyMemory<byte>? algorithmParameters,
            out ReadOnlyMemory<byte> subjectPublicKey)
        {
            AsnReader outer = new(source, AsnEncodingRules.DER);
            AsnReader reader = outer.ReadSequence();
            outer.ThrowIfNotEmpty();

            AsnReader spkiAlgorithm = reader.ReadSequence();
            oid = spkiAlgorithm.ReadObjectIdentifier();

            if (spkiAlgorithm.HasData)
            {
                algorithmParameters = spkiAlgorithm.ReadEncodedValue();
            }
            else
            {
                algorithmParameters = null;
            }

            spkiAlgorithm.ThrowIfNotEmpty();

            AssertExtensions.TrueExpression(reader.TryReadPrimitiveBitString(out int unusedBits, out subjectPublicKey));
            reader.ThrowIfNotEmpty();
            Assert.Equal(0, unusedBits);
        }

        private static bool TryExportEncryptedPkcs8PrivateKeyByKind(
            X25519DiffieHellman xdh,
            TryExportPkcs8PasswordKind kind,
            Span<byte> destination,
            out int bytesWritten)
        {
            return kind switch
            {
                TryExportPkcs8PasswordKind.StringPassword =>
                    xdh.TryExportEncryptedPkcs8PrivateKey(EncryptedPrivateKeyPassword, s_aes128Pbe, destination, out bytesWritten),
                TryExportPkcs8PasswordKind.SpanOfCharsPassword =>
                    xdh.TryExportEncryptedPkcs8PrivateKey(EncryptedPrivateKeyPassword.AsSpan(), s_aes128Pbe, destination, out bytesWritten),
                TryExportPkcs8PasswordKind.SpanOfBytesPassword =>
                    xdh.TryExportEncryptedPkcs8PrivateKey(EncryptedPrivateKeyPasswordBytes, s_aes128Pbe, destination, out bytesWritten),
                _ => throw new XunitException($"Unknown password kind '{kind}'."),
            };
        }

        public enum TryExportPkcs8PasswordKind
        {
            StringPassword,
            SpanOfCharsPassword,
            SpanOfBytesPassword,
        }
    }

    internal sealed class X25519DiffieHellmanContract : X25519DiffieHellman
    {
        internal DeriveRawSecretAgreementCoreCallback OnDeriveRawSecretAgreementCore { get; set; }
        internal ExportKeyCoreCallback OnExportPrivateKeyCore { get; set; }
        internal ExportKeyCoreCallback OnExportPublicKeyCore { get; set; }
        internal TryExportPkcs8PrivateKeyCoreCallback OnTryExportPkcs8PrivateKeyCore { get; set; }
        internal Action<bool> OnDispose { get; set; } = (bool disposing) => { };

        internal int DeriveRawSecretAgreementCoreCount { get; set; }
        internal int ExportPrivateKeyCoreCount { get; set; }
        internal int ExportPublicKeyCoreCount { get; set; }
        internal int TryExportPkcs8PrivateKeyCoreCount { get; set; }

        private bool _disposed;

        protected override void DeriveRawSecretAgreementCore(X25519DiffieHellman otherParty, Span<byte> destination)
        {
            DeriveRawSecretAgreementCoreCount++;
            GetCallback(OnDeriveRawSecretAgreementCore)(otherParty, destination);
        }

        protected override void ExportPrivateKeyCore(Span<byte> destination)
        {
            ExportPrivateKeyCoreCount++;
            GetCallback(OnExportPrivateKeyCore)(destination);
        }

        protected override void ExportPublicKeyCore(Span<byte> destination)
        {
            ExportPublicKeyCoreCount++;
            GetCallback(OnExportPublicKeyCore)(destination);
        }

        protected override bool TryExportPkcs8PrivateKeyCore(Span<byte> destination, out int bytesWritten)
        {
            TryExportPkcs8PrivateKeyCoreCount++;
            return GetCallback(OnTryExportPkcs8PrivateKeyCore)(destination, out bytesWritten);
        }

        protected override void Dispose(bool disposing)
        {
            GetCallback(OnDispose)(disposing);
            VerifyCalledOnDispose();
            _disposed = true;
        }

        private void VerifyCalledOnDispose()
        {
            if (OnDeriveRawSecretAgreementCore is not null && DeriveRawSecretAgreementCoreCount == 0)
            {
                Assert.Fail($"Expected call to {nameof(DeriveRawSecretAgreementCore)}.");
            }
            if (OnExportPrivateKeyCore is not null && ExportPrivateKeyCoreCount == 0)
            {
                Assert.Fail($"Expected call to {nameof(ExportPrivateKeyCore)}.");
            }
            if (OnExportPublicKeyCore is not null && ExportPublicKeyCoreCount == 0)
            {
                Assert.Fail($"Expected call to {nameof(ExportPublicKeyCore)}.");
            }
            if (OnTryExportPkcs8PrivateKeyCore is not null && TryExportPkcs8PrivateKeyCoreCount == 0)
            {
                Assert.Fail($"Expected call to {nameof(TryExportPkcs8PrivateKeyCore)}.");
            }
        }

        internal delegate void DeriveRawSecretAgreementCoreCallback(X25519DiffieHellman otherParty, Span<byte> destination);
        internal delegate void ExportKeyCoreCallback(Span<byte> destination);
        internal delegate bool TryExportPkcs8PrivateKeyCoreCallback(Span<byte> destination, out int bytesWritten);

        private T GetCallback<T>(T callback, [CallerMemberName] string caller = null) where T : Delegate
        {
            if (_disposed)
            {
                Assert.Fail($"Unexpected call to {caller} after Dispose.");
            }

            return callback ?? throw new XunitException($"Unexpected call to {caller}.");
        }
    }
}
