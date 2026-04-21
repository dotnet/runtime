// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    public abstract class X25519DiffieHellmanBaseTests
    {
        private static readonly PbeParameters s_aes128Pbe = new(PbeEncryptionAlgorithm.Aes128Cbc, HashAlgorithmName.SHA256, 2);

        public abstract X25519DiffieHellman GenerateKey();
        public abstract X25519DiffieHellman ImportPrivateKey(ReadOnlySpan<byte> source);
        public abstract X25519DiffieHellman ImportPublicKey(ReadOnlySpan<byte> source);

        // SymCrypt, thus SCOSSL and and CNG, are stricter about keys they are willing to import. These keys fall in to
        // two buckets.
        // 1. Public keys that are non-canonical. RFC 7748 says:
        //      Implementations MUST accept non-canonical values and process them as
        //      if they had been reduced modulo the field prime.  The non-canonical
        //      values are 2^255 - 19 through 2^255 - 1 for X25519
        //    Regardless, SymCrypt rejects these non-canonical keys anyway. There are only 20 possible keys that fall in to this category.
        // 2. Public keys that are not in the prime-order subgroup. [GOrd] * P ≠ O.
        //    X25519DH doesn't strictly need this, but Windows enforces this property anyway.
        public static bool IsStrictKeyValidatingPlatform => OperatingSystem.IsWindows() || PlatformDetection.IsSymCryptOpenSsl;

        [Fact]
        public void ExportPrivateKey_Roundtrip()
        {
            using X25519DiffieHellman xdh = GenerateKey();

            byte[] privateKey = xdh.ExportPrivateKey();
            Assert.True(privateKey.AsSpan().ContainsAnyExcept((byte)0));
            Assert.Equal(X25519DiffieHellman.PrivateKeySizeInBytes, privateKey.Length);

            Span<byte> privateKeySpan = new byte[X25519DiffieHellman.PrivateKeySizeInBytes];
            xdh.ExportPrivateKey(privateKeySpan);
            AssertExtensions.SequenceEqual(privateKey.AsSpan(), privateKeySpan);

            using X25519DiffieHellman imported = ImportPrivateKey(privateKey);
            AssertExtensions.SequenceEqual(privateKey, imported.ExportPrivateKey());
        }

        [Fact]
        public void ExportPublicKey_Roundtrip()
        {
            using X25519DiffieHellman xdh = GenerateKey();

            byte[] publicKey = xdh.ExportPublicKey();
            Assert.True(publicKey.AsSpan().ContainsAnyExcept((byte)0));
            Assert.Equal(X25519DiffieHellman.PublicKeySizeInBytes, publicKey.Length);

            Span<byte> publicKeySpan = new byte[X25519DiffieHellman.PublicKeySizeInBytes];
            xdh.ExportPublicKey(publicKeySpan);
            AssertExtensions.SequenceEqual(publicKey.AsSpan(), publicKeySpan);

            using X25519DiffieHellman imported = ImportPublicKey(publicKey);
            AssertExtensions.SequenceEqual(publicKey, imported.ExportPublicKey());
        }

        [Fact]
        public void ExportPublicKey_PublicKeyOnly()
        {
            using X25519DiffieHellman xdh = GenerateKey();
            byte[] publicKey = xdh.ExportPublicKey();

            using X25519DiffieHellman publicOnly = ImportPublicKey(publicKey);
            AssertExtensions.SequenceEqual(publicKey, publicOnly.ExportPublicKey());
        }

        [Fact]
        public void ExportPrivateKey_PublicKeyOnly_Throws()
        {
            using X25519DiffieHellman xdh = GenerateKey();
            using X25519DiffieHellman publicOnly = ImportPublicKey(xdh.ExportPublicKey());

            Assert.Throws<CryptographicException>(() => publicOnly.ExportPrivateKey());
            Assert.Throws<CryptographicException>(() => publicOnly.ExportPrivateKey(new byte[X25519DiffieHellman.PrivateKeySizeInBytes]));
        }

        [Fact]
        public void DeriveRawSecretAgreement_Symmetric()
        {
            using X25519DiffieHellman key1 = GenerateKey();
            using X25519DiffieHellman key2 = GenerateKey();

            byte[] secret1 = key1.DeriveRawSecretAgreement(key2);
            byte[] secret2 = key2.DeriveRawSecretAgreement(key1);

            AssertExtensions.SequenceEqual(secret1, secret2);
        }

        [Fact]
        public void DeriveRawSecretAgreement_ExactBuffers()
        {
            using X25519DiffieHellman key1 = GenerateKey();
            using X25519DiffieHellman key2 = GenerateKey();

            byte[] secret1 = new byte[X25519DiffieHellman.SecretAgreementSizeInBytes];
            byte[] secret2 = new byte[X25519DiffieHellman.SecretAgreementSizeInBytes];
            key1.DeriveRawSecretAgreement(key2, secret1);
            key2.DeriveRawSecretAgreement(key1, secret2);

            AssertExtensions.SequenceEqual(secret1, secret2);
        }

        [Fact]
        public void DeriveRawSecretAgreement_PublicKeyOnly_Throws()
        {
            using X25519DiffieHellman xdh = GenerateKey();
            using X25519DiffieHellman publicOnly = ImportPublicKey(xdh.ExportPublicKey());
            using X25519DiffieHellman other = GenerateKey();

            Assert.Throws<CryptographicException>(() => publicOnly.DeriveRawSecretAgreement(other));
            Assert.Throws<CryptographicException>(() => publicOnly.DeriveRawSecretAgreement(other, new byte[X25519DiffieHellman.SecretAgreementSizeInBytes]));
        }

        [Fact]
        public void DeriveRawSecretAgreement_Vectors()
        {
            foreach (DeriveSecretAgreementVector vector in DeriveSecretAgreementVectors)
            {
                using X25519DiffieHellman key = ImportPrivateKey(vector.PrivateKey);
                using X25519DiffieHellman peer = ImportPublicKey(vector.PeerPublicKey);

                // Allocating
                byte[] secret = key.DeriveRawSecretAgreement(peer);
                AssertExtensions.SequenceEqual(vector.SharedSecret, secret);

                // Exact buffers
                byte[] secretBuffer = new byte[X25519DiffieHellman.SecretAgreementSizeInBytes];
                key.DeriveRawSecretAgreement(peer, secretBuffer);
                AssertExtensions.SequenceEqual(vector.SharedSecret, secretBuffer);
            }
        }

        [Fact]
        public void DeriveRawSecretAgreement_CrossImplementation()
        {
            foreach (DeriveSecretAgreementVector vector in DeriveSecretAgreementVectors)
            {
                using X25519DiffieHellman key = ImportPrivateKey(vector.PrivateKey);
                using X25519DiffieHellmanWrapper wrapper = new(ImportPublicKey(vector.PeerPublicKey));

                byte[] secret = key.DeriveRawSecretAgreement(wrapper);
                AssertExtensions.SequenceEqual(vector.SharedSecret, secret);

                byte[] secretBuffer = new byte[X25519DiffieHellman.SecretAgreementSizeInBytes];
                key.DeriveRawSecretAgreement(wrapper, secretBuffer);
                AssertExtensions.SequenceEqual(vector.SharedSecret, secretBuffer);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void SubjectPublicKeyInfo_Roundtrip(bool useTryExport)
        {
            using X25519DiffieHellman xdh = ImportPrivateKey(X25519DiffieHellmanTestData.AlicePrivateKey);
            AssertSubjectPublicKeyInfo(xdh, useTryExport, X25519DiffieHellmanTestData.AliceSpki);
        }

        [Fact]
        public void ExportSubjectPublicKeyInfo_Allocated_Independent()
        {
            using X25519DiffieHellman xdh = GenerateKey();
            xdh.ExportSubjectPublicKeyInfo().AsSpan().Clear();
            byte[] spki1 = xdh.ExportSubjectPublicKeyInfo();
            byte[] spki2 = xdh.ExportSubjectPublicKeyInfo();
            Assert.NotSame(spki1, spki2);
            AssertExtensions.SequenceEqual(spki1, spki2);
        }

        [Fact]
        public void TryExportSubjectPublicKeyInfo_Buffers()
        {
            using X25519DiffieHellman xdh = GenerateKey();
            byte[] expectedSpki = xdh.ExportSubjectPublicKeyInfo();
            byte[] buffer;
            int written;

            buffer = new byte[expectedSpki.Length - 1];
            Assert.False(xdh.TryExportSubjectPublicKeyInfo(buffer, out written));
            Assert.Equal(0, written);

            buffer = new byte[expectedSpki.Length];
            Assert.True(xdh.TryExportSubjectPublicKeyInfo(buffer, out written));
            Assert.Equal(expectedSpki.Length, written);
            AssertExtensions.SequenceEqual(expectedSpki, buffer);

            buffer = new byte[expectedSpki.Length + 42];
            Assert.True(xdh.TryExportSubjectPublicKeyInfo(buffer, out written));
            Assert.Equal(expectedSpki.Length, written);
            AssertExtensions.SequenceEqual(expectedSpki.AsSpan(), buffer.AsSpan(0, written));
        }

        [Fact]
        public void ExportPkcs8PrivateKey_Roundtrip()
        {
            using X25519DiffieHellman xdh = ImportPrivateKey(X25519DiffieHellmanTestData.AlicePrivateKey);

            AssertExportPkcs8PrivateKey(xdh, pkcs8 =>
            {
                using X25519DiffieHellman imported = X25519DiffieHellman.ImportPkcs8PrivateKey(pkcs8);
                AssertExtensions.SequenceEqual(
                    X25519DiffieHellmanTestData.AlicePrivateKey,
                    imported.ExportPrivateKey());
            });
        }

        [Fact]
        public void ExportPkcs8PrivateKey_PublicKeyOnly_Fails()
        {
            using X25519DiffieHellman xdh = ImportPublicKey(X25519DiffieHellmanTestData.AlicePublicKey);
            Assert.Throws<CryptographicException>(() => DoTryUntilDone(xdh.TryExportPkcs8PrivateKey));
            Assert.Throws<CryptographicException>(() => xdh.ExportPkcs8PrivateKey());
        }

        [Fact]
        public void ExportEncryptedPkcs8PrivateKey_Roundtrip()
        {
            using X25519DiffieHellman xdh = ImportPrivateKey(X25519DiffieHellmanTestData.AlicePrivateKey);
            AssertEncryptedExportPkcs8PrivateKey(
                xdh,
                X25519DiffieHellmanTestData.EncryptedPrivateKeyPassword,
                s_aes128Pbe,
                pkcs8 =>
                {
                    using X25519DiffieHellman imported = X25519DiffieHellman.ImportEncryptedPkcs8PrivateKey(
                        X25519DiffieHellmanTestData.EncryptedPrivateKeyPassword,
                        pkcs8);

                    AssertExtensions.SequenceEqual(
                        X25519DiffieHellmanTestData.AlicePrivateKey,
                        imported.ExportPrivateKey());
                });
        }

        [Fact]
        public void ExportEncryptedPkcs8PrivateKey_PublicKeyOnly_Fails()
        {
            using X25519DiffieHellman xdh = ImportPublicKey(X25519DiffieHellmanTestData.AlicePublicKey);

            Assert.Throws<CryptographicException>(() => DoTryUntilDone((Span<byte> destination, out int bytesWritten) =>
                xdh.TryExportEncryptedPkcs8PrivateKey(
                    X25519DiffieHellmanTestData.EncryptedPrivateKeyPassword.AsSpan(),
                    s_aes128Pbe,
                    destination,
                    out bytesWritten)));

            Assert.Throws<CryptographicException>(() => DoTryUntilDone((Span<byte> destination, out int bytesWritten) =>
                xdh.TryExportEncryptedPkcs8PrivateKey(
                    X25519DiffieHellmanTestData.EncryptedPrivateKeyPasswordBytes,
                    s_aes128Pbe,
                    destination,
                    out bytesWritten)));

            Assert.Throws<CryptographicException>(() => xdh.ExportEncryptedPkcs8PrivateKey(
                X25519DiffieHellmanTestData.EncryptedPrivateKeyPassword, s_aes128Pbe));

            Assert.Throws<CryptographicException>(() => xdh.ExportEncryptedPkcs8PrivateKey(
                X25519DiffieHellmanTestData.EncryptedPrivateKeyPassword.AsSpan(), s_aes128Pbe));

            Assert.Throws<CryptographicException>(() => xdh.ExportEncryptedPkcs8PrivateKey(
                X25519DiffieHellmanTestData.EncryptedPrivateKeyPasswordBytes, s_aes128Pbe));

            Assert.Throws<CryptographicException>(() => xdh.ExportEncryptedPkcs8PrivateKeyPem(
                X25519DiffieHellmanTestData.EncryptedPrivateKeyPasswordBytes, s_aes128Pbe));

            Assert.Throws<CryptographicException>(() => xdh.ExportEncryptedPkcs8PrivateKeyPem(
                X25519DiffieHellmanTestData.EncryptedPrivateKeyPassword, s_aes128Pbe));

            Assert.Throws<CryptographicException>(() => xdh.ExportEncryptedPkcs8PrivateKeyPem(
                X25519DiffieHellmanTestData.EncryptedPrivateKeyPassword.AsSpan(), s_aes128Pbe));
        }

        [Theory]
        [MemberData(nameof(ExportPkcs8Parameters))]
        public void ExportEncryptedPkcs8PrivateKey_PbeParameters(PbeParameters pbeParameters)
        {
            using X25519DiffieHellman xdh = ImportPrivateKey(X25519DiffieHellmanTestData.AlicePrivateKey);
            AssertEncryptedExportPkcs8PrivateKey(
                xdh,
                X25519DiffieHellmanTestData.EncryptedPrivateKeyPassword,
                pbeParameters,
                pkcs8 =>
                {
                    Pkcs8TestHelpers.AssertEncryptedPkcs8PrivateKeyContents(pbeParameters, pkcs8);
                });
        }

        public static IEnumerable<object[]> ExportPkcs8Parameters
        {
            get
            {
                yield return [new PbeParameters(PbeEncryptionAlgorithm.Aes128Cbc, HashAlgorithmName.SHA256, 42)];
                yield return [new PbeParameters(PbeEncryptionAlgorithm.Aes256Cbc, HashAlgorithmName.SHA512, 43)];
                yield return [new PbeParameters(PbeEncryptionAlgorithm.Aes192Cbc, HashAlgorithmName.SHA384, 44)];
                yield return [new PbeParameters(PbeEncryptionAlgorithm.TripleDes3KeyPkcs12, HashAlgorithmName.SHA1, 24)];
            }
        }

        [Fact]
        public void PrivateKey_Roundtrip_UnclampedScalar()
        {
            byte[] privateKey = X25519DiffieHellmanTestData.BobPrivateKey;
            using X25519DiffieHellman xdh = ImportPrivateKey(privateKey);

            AssertExtensions.SequenceEqual(privateKey, xdh.ExportPrivateKey());
            AssertExtensions.SequenceEqual(X25519DiffieHellmanTestData.BobPublicKey, xdh.ExportPublicKey());

            byte[] pkcs8 = xdh.ExportPkcs8PrivateKey();
            using X25519DiffieHellman reimported = X25519DiffieHellman.ImportPkcs8PrivateKey(pkcs8);
            AssertExtensions.SequenceEqual(privateKey, reimported.ExportPrivateKey());
        }

        [Fact]
        public void PrivateKey_Roundtrip_ClampedScalar()
        {
            byte[] privateKey = (byte[])X25519DiffieHellmanTestData.AlicePrivateKey.Clone();
            privateKey[0] &= 0b11111000;
            privateKey[^1] &= 0b01111111;
            privateKey[^1] |= 0b01000000;

            using X25519DiffieHellman xdh = ImportPrivateKey(privateKey);
            AssertExtensions.SequenceEqual(privateKey, xdh.ExportPrivateKey());
        }

        [Fact]
        public void PrivateKey_ClampedAndUnclamped_SamePublicKey()
        {
            byte[] unclamped = (byte[])X25519DiffieHellmanTestData.AlicePrivateKey.Clone();
            byte[] clamped = (byte[])unclamped.Clone();
            clamped[0] &= 0b11111000;
            clamped[^1] &= 0b01111111;
            clamped[^1] |= 0b01000000;

            using X25519DiffieHellman xdhUnclamped = ImportPrivateKey(unclamped);
            using X25519DiffieHellman xdhClamped = ImportPrivateKey(clamped);

            AssertExtensions.SequenceEqual(xdhUnclamped.ExportPublicKey(), xdhClamped.ExportPublicKey());
        }

        private static void AssertSubjectPublicKeyInfo(X25519DiffieHellman xdh, bool useTryExport, ReadOnlySpan<byte> expectedSpki)
        {
            byte[] spki;
            int written;

            if (useTryExport)
            {
                spki = new byte[X25519DiffieHellman.PublicKeySizeInBytes + 16];
                Assert.True(xdh.TryExportSubjectPublicKeyInfo(spki, out written));
            }
            else
            {
                spki = xdh.ExportSubjectPublicKeyInfo();
                written = spki.Length;
            }

            ReadOnlySpan<byte> encodedSpki = spki.AsSpan(0, written);
            AssertExtensions.SequenceEqual(expectedSpki, encodedSpki);

            using X25519DiffieHellman imported = X25519DiffieHellman.ImportSubjectPublicKeyInfo(encodedSpki);
            AssertExtensions.SequenceEqual(xdh.ExportPublicKey(), imported.ExportPublicKey());
        }

        private static void AssertExportPkcs8PrivateKey(X25519DiffieHellman xdh, Action<byte[]> callback)
        {
            callback(DoTryUntilDone(xdh.TryExportPkcs8PrivateKey));
            callback(xdh.ExportPkcs8PrivateKey());
        }

        private static void AssertEncryptedExportPkcs8PrivateKey(
            X25519DiffieHellman xdh,
            string password,
            PbeParameters pbeParameters,
            Action<byte[]> callback)
        {
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

            callback(DoTryUntilDone((Span<byte> destination, out int bytesWritten) =>
            {
                return xdh.TryExportEncryptedPkcs8PrivateKey(
                    password.AsSpan(),
                    pbeParameters,
                    destination,
                    out bytesWritten);
            }));

            callback(xdh.ExportEncryptedPkcs8PrivateKey(password, pbeParameters));
            callback(xdh.ExportEncryptedPkcs8PrivateKey(password.AsSpan(), pbeParameters));
            callback(DecodePem(xdh.ExportEncryptedPkcs8PrivateKeyPem(password, pbeParameters)));
            callback(DecodePem(xdh.ExportEncryptedPkcs8PrivateKeyPem(password.AsSpan(), pbeParameters)));

            if (pbeParameters.EncryptionAlgorithm != PbeEncryptionAlgorithm.TripleDes3KeyPkcs12)
            {
                callback(DoTryUntilDone((Span<byte> destination, out int bytesWritten) =>
                {
                    return xdh.TryExportEncryptedPkcs8PrivateKey(
                        new ReadOnlySpan<byte>(passwordBytes),
                        pbeParameters,
                        destination,
                        out bytesWritten);
                }));

                callback(xdh.ExportEncryptedPkcs8PrivateKey(new ReadOnlySpan<byte>(passwordBytes), pbeParameters));
                callback(DecodePem(xdh.ExportEncryptedPkcs8PrivateKeyPem(new ReadOnlySpan<byte>(passwordBytes), pbeParameters)));
            }

            static byte[] DecodePem(string pem)
            {
                PemFields fields = PemEncoding.Find(pem.AsSpan());
                Assert.Equal(Index.FromStart(0), fields.Location.Start);
                Assert.Equal(Index.FromStart(pem.Length), fields.Location.End);
                Assert.Equal("ENCRYPTED PRIVATE KEY", pem.AsSpan()[fields.Label].ToString());
                return Convert.FromBase64String(pem.AsSpan()[fields.Base64Data].ToString());
            }
        }

        [Fact]
        public void DeriveRawSecretAgreement_NonPlatformOtherParty()
        {
            using X25519DiffieHellman key1 = GenerateKey();
            using X25519DiffieHellman key2 = GenerateKey();

            // Wrap key2 in a non-platform wrapper. This forces the implementation to go through
            // the ExportPublicKey fallback path rather than using the native key handle directly.
            using X25519DiffieHellmanWrapper wrapper = new(key2);

            byte[] secret1 = key1.DeriveRawSecretAgreement(wrapper);
            byte[] secret2 = key2.DeriveRawSecretAgreement(key1);

            AssertExtensions.SequenceEqual(secret1, secret2);
            Assert.True(wrapper.ExportPublicKeyCoreWasCalled);
        }

        [Fact]
        public void DeriveRawSecretAgreement_NonPlatformOtherParty_ExactBuffers()
        {
            using X25519DiffieHellman key1 = GenerateKey();
            using X25519DiffieHellman key2 = GenerateKey();
            using X25519DiffieHellmanWrapper wrapper = new(key2);

            byte[] secret1 = new byte[X25519DiffieHellman.SecretAgreementSizeInBytes];
            byte[] secret2 = new byte[X25519DiffieHellman.SecretAgreementSizeInBytes];
            key1.DeriveRawSecretAgreement(wrapper, secret1);
            key2.DeriveRawSecretAgreement(key1, secret2);

            AssertExtensions.SequenceEqual(secret1, secret2);
            Assert.True(wrapper.ExportPublicKeyCoreWasCalled);
        }

        private delegate bool TryExportFunc(Span<byte> destination, out int bytesWritten);

        private static byte[] DoTryUntilDone(TryExportFunc func)
        {
            byte[] buffer = new byte[512];
            int written;

            while (!func(buffer, out written))
            {
                Array.Resize(ref buffer, buffer.Length * 2);
            }

            return buffer.AsSpan(0, written).ToArray();
        }

        /// <summary>
        /// A wrapper around an X25519DiffieHellman instance that is not the platform's
        /// internal implementation type. This forces the DeriveRawSecretAgreementCore fallback
        /// path that exports the public key and re-imports it.
        /// </summary>
        private sealed class X25519DiffieHellmanWrapper : X25519DiffieHellman
        {
            private readonly X25519DiffieHellman _inner;

            public bool ExportPublicKeyCoreWasCalled { get; private set; }

            public X25519DiffieHellmanWrapper(X25519DiffieHellman inner)
            {
                _inner = inner;
            }

            protected override void DeriveRawSecretAgreementCore(X25519DiffieHellman otherParty, Span<byte> destination)
            {
                _inner.DeriveRawSecretAgreement(otherParty, destination);
            }

            protected override void ExportPrivateKeyCore(Span<byte> destination)
            {
                _inner.ExportPrivateKey(destination);
            }

            protected override void ExportPublicKeyCore(Span<byte> destination)
            {
                ExportPublicKeyCoreWasCalled = true;
                _inner.ExportPublicKey(destination);
            }

            protected override bool TryExportPkcs8PrivateKeyCore(Span<byte> destination, out int bytesWritten)
            {
                return _inner.TryExportPkcs8PrivateKey(destination, out bytesWritten);
            }

            protected override void Dispose(bool disposing)
            {
                // Don't dispose _inner; the test owns it.
                base.Dispose(disposing);
            }
        }

        public record DeriveSecretAgreementVector(string Name, byte[] PrivateKey, byte[] PeerPublicKey, byte[] SharedSecret);

        public static IEnumerable<DeriveSecretAgreementVector> DeriveSecretAgreementVectors
        {
            get
            {
                // Wycheproof Cases 2-4: low-order shared secret results (same private key)
                byte[] wycheproofPrivate2 =
                [
                    0xa0, 0xa4, 0xf1, 0x30, 0xb9, 0x8a, 0x5b, 0xe4, 0xb1, 0xce, 0xdb, 0x7c, 0xb8, 0x55, 0x84, 0xa3,
                    0x52, 0x0e, 0x14, 0x2d, 0x47, 0x4d, 0xc9, 0xcc, 0xb9, 0x09, 0xa0, 0x73, 0xa9, 0x76, 0xbf, 0x63,
                ];

                // RFC 7748 Section 6.1
                yield return new("RFC7748_Alice",
                    X25519DiffieHellmanTestData.AlicePrivateKey,
                    X25519DiffieHellmanTestData.BobPublicKey,
                    X25519DiffieHellmanTestData.SharedSecret);

                yield return new("RFC7748_Bob",
                    X25519DiffieHellmanTestData.BobPrivateKey,
                    X25519DiffieHellmanTestData.AlicePublicKey,
                    X25519DiffieHellmanTestData.SharedSecret);

                if (!IsStrictKeyValidatingPlatform)
                {
                    // Wycheproof Case 0: near-max public key (0xF0FF...FF7F)
                    yield return new("Wycheproof_0",
                        [
                            0x28, 0x87, 0x96, 0xbc, 0x5a, 0xff, 0x4b, 0x81, 0xa3, 0x75, 0x01, 0x75, 0x7b, 0xc0, 0x75, 0x3a,
                            0x3c, 0x21, 0x96, 0x47, 0x90, 0xd3, 0x86, 0x99, 0x30, 0x8d, 0xeb, 0xc1, 0x7a, 0x6e, 0xaf, 0x8d,
                        ],
                        [
                            0xf0, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                            0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x7f,
                        ],
                        [
                            0xb4, 0xe0, 0xdd, 0x76, 0xda, 0x7b, 0x07, 0x17, 0x28, 0xb6, 0x1f, 0x85, 0x67, 0x71, 0xaa, 0x35,
                            0x6e, 0x57, 0xed, 0xa7, 0x8a, 0x5b, 0x16, 0x55, 0xcc, 0x38, 0x20, 0xfb, 0x5f, 0x85, 0x4c, 0x5c,
                        ]);

                    // Wycheproof Case 1: all-bits-set public key (0xF0FF...FFFF)
                    yield return new("Wycheproof_1",
                        [
                            0x60, 0x88, 0x7b, 0x3d, 0xc7, 0x24, 0x43, 0x02, 0x6e, 0xbe, 0xdb, 0xbb, 0xb7, 0x06, 0x65, 0xf4,
                            0x2b, 0x87, 0xad, 0xd1, 0x44, 0x0e, 0x77, 0x68, 0xfb, 0xd7, 0xe8, 0xe2, 0xce, 0x5f, 0x63, 0x9d,
                        ],
                        [
                            0xf0, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                            0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                        ],
                        [
                            0x38, 0xd6, 0x30, 0x4c, 0x4a, 0x7e, 0x6d, 0x9f, 0x79, 0x59, 0x33, 0x4f, 0xb5, 0x24, 0x5b, 0xd2,
                            0xc7, 0x54, 0x52, 0x5d, 0x4c, 0x91, 0xdb, 0x95, 0x02, 0x06, 0x92, 0x62, 0x34, 0xc1, 0xf6, 0x33,
                        ]);

                    yield return new("Wycheproof_2_LowOrder",
                        wycheproofPrivate2,
                        [
                            0x0a, 0xb4, 0xe7, 0x63, 0x80, 0xd8, 0x4d, 0xde, 0x4f, 0x68, 0x33, 0xc5, 0x8f, 0x2a, 0x9f, 0xb8,
                            0xf8, 0x3b, 0xb0, 0x16, 0x9b, 0x17, 0x2b, 0xe4, 0xb6, 0xe0, 0x59, 0x28, 0x87, 0x74, 0x1a, 0x36,
                        ],
                        [
                            0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                        ]);
                }

                yield return new("Wycheproof_3_LowOrder",
                    wycheproofPrivate2,
                    [
                        0x89, 0xe1, 0x0d, 0x57, 0x01, 0xb4, 0x33, 0x7d, 0x2d, 0x03, 0x21, 0x81, 0x53, 0x8b, 0x10, 0x64,
                        0xbd, 0x40, 0x84, 0x40, 0x1c, 0xec, 0xa1, 0xfd, 0x12, 0x66, 0x3a, 0x19, 0x59, 0x38, 0x80, 0x00,
                    ],
                    [
                        0x09, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    ]);

                yield return new("Wycheproof_4_LowOrder",
                    wycheproofPrivate2,
                    [
                        0x2b, 0x55, 0xd3, 0xaa, 0x4a, 0x8f, 0x80, 0xc8, 0xc0, 0xb2, 0xae, 0x5f, 0x93, 0x3e, 0x85, 0xaf,
                        0x49, 0xbe, 0xac, 0x36, 0xc2, 0xfa, 0x73, 0x94, 0xba, 0xb7, 0x6c, 0x89, 0x33, 0xf8, 0xf8, 0x1d,
                    ],
                    [
                        0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    ]);
            }
        }
    }
}
