// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    // The abstract test class that tests instance members of X25519DiffieHellman. It tests the internal "Implementation"
    // by platform, as well as any platform specific implementations that derive from X25519DiffieHellman.
    public abstract class X25519DiffieHellmanBaseTests
    {
        private static readonly PbeParameters s_aes128Pbe = new(PbeEncryptionAlgorithm.Aes128Cbc, HashAlgorithmName.SHA256, 2);

        public abstract X25519DiffieHellman GenerateKey();
        public abstract X25519DiffieHellman ImportPrivateKey(ReadOnlySpan<byte> source);
        public abstract X25519DiffieHellman ImportPublicKey(ReadOnlySpan<byte> source);

        // SymCrypt, thus SCOSSL, is stricter about keys it is willing to import. These keys fall in to
        // two buckets.
        // 1. Public keys that are non-canonical. RFC 7748 says:
        //      Implementations MUST accept non-canonical values and process them as
        //      if they had been reduced modulo the field prime.  The non-canonical
        //      values are 2^255 - 19 through 2^255 - 1 for X25519
        //    Regardless, SymCrypt rejects these non-canonical keys anyway. There are only 20 possible keys that fall in to this category.
        // 2. Public keys that are not in the prime-order subgroup. [GOrd] * P != O.
        //    X25519DH doesn't strictly need this, but SymCrypt enforces this property anyway.
        // CNG enforces both, but the Windows X25519DiffieHellmanImplementation canonicalizes
        // non-canonical public keys before import and passes BCRYPT_NO_KEY_VALIDATION.
        public static bool IsStrictKeyValidatingPlatform => PlatformDetection.IsSymCryptOpenSsl;
        public static bool IsNotStrictKeyValidatingPlatform => !IsStrictKeyValidatingPlatform;

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

        [Theory]
        [MemberData(nameof(DeriveSecretAgreementVectorsForCurrentPlatform))]
        public void DeriveRawSecretAgreement_Vectors(DeriveSecretAgreementVector vector)
        {
            using X25519DiffieHellman key = ImportPrivateKey(vector.PrivateKey);
            using X25519DiffieHellman peer = ImportPublicKey(vector.PeerPublicKey);

            byte[] secret = key.DeriveRawSecretAgreement(peer);
            AssertExtensions.SequenceEqual(vector.SharedSecret, secret);

            byte[] secretBuffer = new byte[X25519DiffieHellman.SecretAgreementSizeInBytes];
            key.DeriveRawSecretAgreement(peer, secretBuffer);
            AssertExtensions.SequenceEqual(vector.SharedSecret, secretBuffer);
        }

        [Theory]
        [MemberData(nameof(DeriveSecretAgreementVectorsForCurrentPlatform))]
        public void DeriveRawSecretAgreement_CrossImplementation(DeriveSecretAgreementVector vector)
        {
            using X25519DiffieHellman key = ImportPrivateKey(vector.PrivateKey);
            using X25519DiffieHellmanWrapper wrapper = new(ImportPublicKey(vector.PeerPublicKey));

            byte[] secret = key.DeriveRawSecretAgreement(wrapper);
            AssertExtensions.SequenceEqual(vector.SharedSecret, secret);

            byte[] secretBuffer = new byte[X25519DiffieHellman.SecretAgreementSizeInBytes];
            key.DeriveRawSecretAgreement(wrapper, secretBuffer);
            AssertExtensions.SequenceEqual(vector.SharedSecret, secretBuffer);
        }

        [ConditionalFact(nameof(IsNotStrictKeyValidatingPlatform))]
        public void DeriveRawSecretAgreement_ZeroSharedSecret_Throws()
        {
            // Wycheproof tcId 64: peer public key is a low-order point on Curve25519.
            // This low-order point produces a shared secret that is all zeros.
            byte[] privateKey = Convert.FromHexString("387355d995616090503aafad49da01fb3dc3eda962704eaee6b86f9e20c92579");
            byte[] peerPublicKey = Convert.FromHexString("5f9c95bca3508c24b1d0b1559c83ef5b04445cc4581c8e86d8224eddd09f1157");

            using X25519DiffieHellman key = ImportPrivateKey(privateKey);
            using X25519DiffieHellman peer = ImportPublicKey(peerPublicKey);

            Assert.ThrowsAny<CryptographicException>(() => key.DeriveRawSecretAgreement(peer));
            Assert.ThrowsAny<CryptographicException>(
                () => key.DeriveRawSecretAgreement(peer, new byte[X25519DiffieHellman.SecretAgreementSizeInBytes]));
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

        public record DeriveSecretAgreementVector(
            string Name,
            byte[] PrivateKey,
            byte[] PeerPublicKey,
            byte[] SharedSecret,
            bool RequiresLenientValidation = false)
        {
            public override string ToString() => Name;
        }

        public static IEnumerable<object[]> DeriveSecretAgreementVectorsForCurrentPlatform()
        {
            foreach (DeriveSecretAgreementVector vector in DeriveSecretAgreementVectors)
            {
                if (vector.RequiresLenientValidation && IsStrictKeyValidatingPlatform)
                {
                    continue;
                }

                yield return new object[] { vector };
            }
        }

        public static IEnumerable<DeriveSecretAgreementVector> DeriveSecretAgreementVectors
        {
            get
            {
                // Wycheproof Cases 2-4: low-order shared secret results (same private key)
                byte[] wycheproofPrivate2 =
                    Convert.FromHexString("a0a4f130b98a5be4b1cedb7cb85584a3520e142d474dc9ccb909a073a976bf63");

                // RFC 7748 Section 6.1
                yield return new("RFC7748_Alice",
                    X25519DiffieHellmanTestData.AlicePrivateKey,
                    X25519DiffieHellmanTestData.BobPublicKey,
                    X25519DiffieHellmanTestData.SharedSecret);

                yield return new("RFC7748_Bob",
                    X25519DiffieHellmanTestData.BobPrivateKey,
                    X25519DiffieHellmanTestData.AlicePublicKey,
                    X25519DiffieHellmanTestData.SharedSecret);

                // High-bit-set on RFC 7748 Bob public key. RFC 7748 Section 5 mandates
                // implementations mask the high bit of the u-coordinate before use, so this
                // peer key is equivalent to BobPublicKey and produces the standard SharedSecret.
                // SymCrypt-OpenSSL does not perform the mandatory mask and rejects the high-bit form.
                byte[] bobPublicKeyHighBitSet = (byte[])X25519DiffieHellmanTestData.BobPublicKey.Clone();
                bobPublicKeyHighBitSet[^1] |= 0x80;
                yield return new("RFC7748_Bob_HighBitSet",
                    X25519DiffieHellmanTestData.AlicePrivateKey,
                    bobPublicKeyHighBitSet,
                    X25519DiffieHellmanTestData.SharedSecret,
                    RequiresLenientValidation: true);

                // Non-canonical encoding of the X25519 base point: u = p + 9 (little-endian
                // 0xF6 FF...FF 7F). After modular reduction, this is u = 9 (the base point),
                // so X25519(AlicePrivateKey, p+9) == AlicePublicKey. Strict-validating
                // platforms reject the non-canonical encoding.
                yield return new("NonCanonical_BasePoint_PPlus9",
                    X25519DiffieHellmanTestData.AlicePrivateKey,
                    Convert.FromHexString("f6ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f"),
                    X25519DiffieHellmanTestData.AlicePublicKey,
                    RequiresLenientValidation: true);

                // Wycheproof Case 0: near-max public key (0xF0FF...FF7F)
                yield return new("Wycheproof_0",
                    Convert.FromHexString("288796bc5aff4b81a37501757bc0753a3c21964790d38699308debc17a6eaf8d"),
                    Convert.FromHexString("f0ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f"),
                    Convert.FromHexString("b4e0dd76da7b071728b61f856771aa356e57eda78a5b1655cc3820fb5f854c5c"),
                    RequiresLenientValidation: true);

                // Wycheproof Case 1: all-bits-set public key (0xF0FF...FFFF)
                yield return new("Wycheproof_1",
                    Convert.FromHexString("60887b3dc72443026ebedbbbb70665f42b87add1440e7768fbd7e8e2ce5f639d"),
                    Convert.FromHexString("f0ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff"),
                    Convert.FromHexString("38d6304c4a7e6d9f7959334fb5245bd2c754525d4c91db950206926234c1f633"),
                    RequiresLenientValidation: true);

                yield return new("Wycheproof_2_LowOrder",
                    wycheproofPrivate2,
                    Convert.FromHexString("0ab4e76380d84dde4f6833c58f2a9fb8f83bb0169b172be4b6e0592887741a36"),
                    Convert.FromHexString("0200000000000000000000000000000000000000000000000000000000000000"),
                    RequiresLenientValidation: true);

                yield return new("Wycheproof_3_LowOrder",
                    wycheproofPrivate2,
                    Convert.FromHexString("89e10d5701b4337d2d032181538b1064bd4084401ceca1fd12663a1959388000"),
                    Convert.FromHexString("0900000000000000000000000000000000000000000000000000000000000000"));

                yield return new("Wycheproof_4_LowOrder",
                    wycheproofPrivate2,
                    Convert.FromHexString("2b55d3aa4a8f80c8c0b2ae5f933e85af49beac36c2fa7394bab76c8933f8f81d"),
                    Convert.FromHexString("1000000000000000000000000000000000000000000000000000000000000000"));

                // Select Wycheproof X25519 test vectors for edge cases.
                yield return new("WycheproofTcId_1", // Normal
                    Convert.FromHexString("c8a9d5a91091ad851c668b0736c1c9a02936c0d3ad62670858088047ba057475"),
                    Convert.FromHexString("504a36999f489cd2fdbc08baff3d88fa00569ba986cba22548ffde80f9806829"),
                    Convert.FromHexString("436a2c040cf45fea9b29a0cb81b1f41458f863d0d61b453d0a982720d6d61320"));
                yield return new("WycheproofTcId_2", // Twist
                    Convert.FromHexString("d85d8c061a50804ac488ad774ac716c3f5ba714b2712e048491379a500211958"),
                    Convert.FromHexString("63aa40c6e38346c5caf23a6df0a5e6c80889a08647e551b3563449befcfc9733"),
                    Convert.FromHexString("279df67a7c4611db4708a0e8282b195e5ac0ed6f4b2f292c6fbd0acac30d1332"),
                    RequiresLenientValidation: true);
                yield return new("WycheproofTcId_4", // Twist
                    Convert.FromHexString("f876e34bcbe1f47fbc0fddfd7c1e1aa53d57bfe0f66d243067b424bb6210be51"),
                    Convert.FromHexString("0b8211a2b6049097f6871c6c052d3c5fc1ba17da9e32ae458403b05bb283092a"),
                    Convert.FromHexString("119d37ed4b109cbd6418b1f28dea83c836c844715cdf98a3a8c362191debd514"),
                    RequiresLenientValidation: true);
                yield return new("WycheproofTcId_7", // SpecialPublicKey,Twist
                    Convert.FromHexString("d03edde9f3e7b799045f9ac3793d4a9277dadeadc41bec0290f81f744f73775f"),
                    Convert.FromHexString("0200000000000000000000000000000000000000000000000000000000000000"),
                    Convert.FromHexString("b87a1722cc6c1e2feecb54e97abd5a22acc27616f78f6e315fd2b73d9f221e57"),
                    RequiresLenientValidation: true);
                yield return new("WycheproofTcId_19", // SpecialPublicKey,Twist
                    Convert.FromHexString("105d621e1ef339c3d99245cfb77cd3a5bd0c4427a0e4d8752c3b51f045889b4f"),
                    Convert.FromHexString("ffffff030000f8ffff1f0000c0ffffff000000feffff070000f0ffff3f000000"),
                    Convert.FromHexString("61eace52da5f5ecefafa4f199b077ff64f2e3d2a6ece6f8ec0497826b212ef5f"),
                    RequiresLenientValidation: true);
                yield return new("WycheproofTcId_31", // SpecialPublicKey,Twist
                    Convert.FromHexString("d02456e456911d3c6cd054933199807732dfdc958642ad1aebe900c793bef24a"),
                    Convert.FromHexString("eaffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f"),
                    Convert.FromHexString("07ba5fcbda21a9a17845c401492b10e6de0a168d5c94b606694c11bac39bea41"),
                    RequiresLenientValidation: true);
                yield return new("WycheproofTcId_34", // SpecialPublicKey
                    Convert.FromHexString("a8386f7f16c50731d64f82e6a170b142a4e34f31fd7768fcb8902925e7d1e25a"),
                    Convert.FromHexString("0400000000000000000000000000000000000000000000000000000000000000"),
                    Convert.FromHexString("34b7e4fa53264420d9f943d15513902342b386b172a0b0b7c8b8f2dd3d669f59"),
                    RequiresLenientValidation: true);
                yield return new("WycheproofTcId_48", // SpecialPublicKey
                    Convert.FromHexString("182191b7052e9cd630ef08007fc6b43bc7652913be6774e2fd271b71b962a641"),
                    Convert.FromHexString("ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff03"),
                    Convert.FromHexString("a0e0315175788362d4ebe05e6ac76d52d40187bd687492af05abc7ba7c70197d"));
                yield return new("WycheproofTcId_62", // SpecialPublicKey
                    Convert.FromHexString("40bd4e1caf39d9def7663823502dad3e7d30eb6eb01e9b89516d4f2f45b7cd7f"),
                    Convert.FromHexString("ebffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f"),
                    Convert.FromHexString("2cf6974b0c070e3707bf92e721d3ea9de3db6f61ed810e0a23d72d433365f631"),
                    RequiresLenientValidation: true);
                yield return new("WycheproofTcId_87", // NonCanonicalPublic,SpecialPublicKey,Twist
                    Convert.FromHexString("0016b62af5cabde8c40938ebf2108e05d27fa0533ed85d70015ad4ad39762d54"),
                    Convert.FromHexString("efffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f"),
                    Convert.FromHexString("b4d10e832714972f96bd3382e4d082a21a8333a16315b3ffb536061d2482360d"),
                    RequiresLenientValidation: true);
                yield return new("WycheproofTcId_89", // NonCanonicalPublic,SpecialPublicKey
                    Convert.FromHexString("88dd14e2711ebd0b0026c651264ca965e7e3da5082789fbab7e24425e7b4377e"),
                    Convert.FromHexString("f1ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f"),
                    Convert.FromHexString("6919992d6a591e77b3f2bacbd74caf3aea4be4802b18b2bc07eb09ade3ad6662"),
                    RequiresLenientValidation: true);
                yield return new("WycheproofTcId_91", // NonCanonicalPublic,SpecialPublicKey,Twist
                    Convert.FromHexString("c0697b6f05e0f3433b44ea352f20508eb0623098a7770853af5ca09727340c4e"),
                    Convert.FromHexString("0200000000000000000000000000000000000000000000000000000000000080"),
                    Convert.FromHexString("ed18b06da512cab63f22d2d51d77d99facd3c4502e4abf4e97b094c20a9ddf10"),
                    RequiresLenientValidation: true);
                yield return new("WycheproofTcId_94", // NonCanonicalPublic,SpecialPublicKey
                    Convert.FromHexString("285a6a7ceeb7122f2c78d99c53b2a902b490892f7dff326f89d12673c3101b53"),
                    Convert.FromHexString("daffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff"),
                    Convert.FromHexString("9b01287717d72f4cfb583ec85f8f936849b17d978dbae7b837db56a62f100a68"),
                    RequiresLenientValidation: true);
                yield return new("WycheproofTcId_97", // NonCanonicalPublic,SpecialPublicKey,Twist
                    Convert.FromHexString("9041c6e044a277df8466275ca8b5ee0da7bc028648054ade5c592add3057474e"),
                    Convert.FromHexString("eaffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff"),
                    Convert.FromHexString("13da5695a4c206115409b5277a934782fe985fa050bc902cba5616f9156fe277"),
                    RequiresLenientValidation: true);
                yield return new("WycheproofTcId_99", // NonCanonicalPublic,SpecialPublicKey
                    Convert.FromHexString("c85f08e60c845f82099141a66dc4583d2b1040462c544d33d0453b20b1a6377e"),
                    Convert.FromHexString("ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff"),
                    Convert.FromHexString("e9db74bc88d0d9bf046ddd13f943bccbe6dbb47d49323f8dfeedc4a694991a3c"),
                    RequiresLenientValidation: true);
                yield return new("WycheproofTcId_100", // Ktv
                    Convert.FromHexString("a046e36bf0527c9d3b16154b82465edd62144c0ac1fc5a18506a2244ba449a44"),
                    Convert.FromHexString("e6db6867583030db3594c1a424b15f7c726624ec26b3353b10a903a6d0ab1c4c"),
                    Convert.FromHexString("c3da55379de9c6908e94ea4df28d084f32eccf03491c71f754b4075577a28552"),
                    RequiresLenientValidation: true);
                yield return new("WycheproofTcId_101", // Ktv,Twist
                    Convert.FromHexString("4866e9d4d1b4673c5ad22691957d6af5c11b6421e0ea01d42ca4169e7918ba4d"),
                    Convert.FromHexString("e5210f12786811d3f4b7959d0538ae2c31dbe7106fc03c3efc4cd549c715a413"),
                    Convert.FromHexString("95cbde9476e8907d7aade45cb4b873f88b595a68799fa152e6f8f7647aac7957"),
                    RequiresLenientValidation: true);
                yield return new("WycheproofTcId_102", // Ktv
                    Convert.FromHexString("77076d0a7318a57d3c16c17251b26645df4c2f87ebc0992ab177fba51db92c2a"),
                    Convert.FromHexString("de9edb7d7b7dc1b4d35b61c2ece435373f8343c85b78674dadfc7e146f882b4f"),
                    Convert.FromHexString("4a5d9d5ba4ce2de1728e3bf480350f25e07e21c947d19e3376f09b3c1e161742"));
                yield return new("WycheproofTcId_103", // EdgeCaseShared,Twist
                    Convert.FromHexString("60a3a4f130b98a5be4b1cedb7cb85584a3520e142d474dc9ccb909a073a9767f"),
                    Convert.FromHexString("b7b6d39c765cb60c0c8542f4f3952ffb51d3002d4aeb9f8ff988b192043e6d0a"),
                    Convert.FromHexString("0200000000000000000000000000000000000000000000000000000000000000"),
                    RequiresLenientValidation: true);
                yield return new("WycheproofTcId_104", // EdgeCaseShared
                    Convert.FromHexString("60a3a4f130b98a5be4b1cedb7cb85584a3520e142d474dc9ccb909a073a9767f"),
                    Convert.FromHexString("3b18df1e50b899ebd588c3161cbd3bf98ebcc2c1f7df53b811bd0e91b4d5153d"),
                    Convert.FromHexString("0900000000000000000000000000000000000000000000000000000000000000"));
                yield return new("WycheproofTcId_107", // EdgeCaseShared
                    Convert.FromHexString("60a3a4f130b98a5be4b1cedb7cb85584a3520e142d474dc9ccb909a073a9767f"),
                    Convert.FromHexString("98730bc03e29e8b057fb1d20ef8c0bffc822485d3db7f45f4e3cc2c3c6d1d14c"),
                    Convert.FromHexString("fcffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff3f"));
                yield return new("WycheproofTcId_112", // EdgeCaseShared,Twist
                    Convert.FromHexString("60a3a4f130b98a5be4b1cedb7cb85584a3520e142d474dc9ccb909a073a9767f"),
                    Convert.FromHexString("8d612c5831aa64b057300e7e310f3aa332af34066fefcab2b089c9592878f832"),
                    Convert.FromHexString("e3ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f"),
                    RequiresLenientValidation: true);
                yield return new("WycheproofTcId_113", // EdgeCaseShared
                    Convert.FromHexString("60a3a4f130b98a5be4b1cedb7cb85584a3520e142d474dc9ccb909a073a9767f"),
                    Convert.FromHexString("8d44108d05d940d3dfe5647ea7a87be24d0d036c9f0a95a2386b839e7b7bf145"),
                    Convert.FromHexString("ddffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f"));
                yield return new("WycheproofTcId_116", // EdgeCaseShared,Twist
                    Convert.FromHexString("60a3a4f130b98a5be4b1cedb7cb85584a3520e142d474dc9ccb909a073a9767f"),
                    Convert.FromHexString("8e41f05ea3c76572be104ad8788e970863c6e2ca3daae64d1c2f46decfffa571"),
                    Convert.FromHexString("0000000000000000000000000000000000000000000000000000000000008000"),
                    RequiresLenientValidation: true);
                yield return new("WycheproofTcId_119", // EdgeCaseMultiplication,Twist
                    Convert.FromHexString("e0a8be63315c4f0f0a3fee607f44d30a55be63f09561d9af93e0a1c9cf0ed751"),
                    Convert.FromHexString("0200000000000000000000000000000000000000000000000000000000000000"),
                    Convert.FromHexString("0c50ac2bfb6815b47d0734c5981379882a24a2de6166853c735329d978baee4d"),
                    RequiresLenientValidation: true);
                yield return new("WycheproofTcId_120", // EdgeCaseMultiplication
                    Convert.FromHexString("0840a8af5bc4c48da8850e973d7e14220f45c192cea4020d377eecd25c7c3643"),
                    Convert.FromHexString("1200000000000000000000000000000000000000000000000000000000000000"),
                    Convert.FromHexString("77557137a2a2a651c49627a9b239ac1f2bf78b8a3e72168ccecc10a51fc5ae66"),
                    RequiresLenientValidation: true);
                yield return new("WycheproofTcId_249", // EdgeCaseMultiplication
                    Convert.FromHexString("4028802030d8a8221a7160eebbf1846116c1c253abc467d6e43cb850f1459860"),
                    Convert.FromHexString("0f13955978b93d7b9f9a2e70d96df922850a8ffd8412e236fb074aef99d37d54"),
                    Convert.FromHexString("e23d63a46be67c7443c07b9371ff6a06afcd7a5794bf2537926074b88190307a"),
                    RequiresLenientValidation: true);
                yield return new("WycheproofTcId_256", // EdgeCaseMultiplication,Twist
                    Convert.FromHexString("d0e67f68183a4c1aed9c56864b36278bb7bb75d57a78321bc7c24ff61636607a"),
                    Convert.FromHexString("d8c8e2c6f33a98525df3767d1d04430dab0bda41f1f904c95bc61cc122caca74"),
                    Convert.FromHexString("ef7612c156078dae3a81e50ef33951cab661fb07731d8f419bc0105c4d6d6050"),
                    RequiresLenientValidation: true);
                yield return new("WycheproofTcId_377", // EdgeCaseMultiplication
                    Convert.FromHexString("c01f66cb094289d728421dd46c6f9718412e1c546dad70e586851be4da58bf67"),
                    Convert.FromHexString("4e056b317a31dd96f8ec14b48474af587d195efcc2a70f01f052ef882d7b3a45"),
                    Convert.FromHexString("bad9f7b27dac64b0fc980a41f1cefa50c5ca40c714296c0c4042095c2db60e11"),
                    RequiresLenientValidation: true);
                yield return new("WycheproofTcId_387", // EdgeCaseMultiplication,Twist
                    Convert.FromHexString("204a3b5652854ff48e25cd385cabe6360f64ce44fea5621db1fa2f6e219f3063"),
                    Convert.FromHexString("ed1c82082b74cc2aaebf3dc772ba09557c0fc14139a8814fc5f9370bb8e98858"),
                    Convert.FromHexString("e0a82f313046024b3cea93b98e2f8ecf228cbfab8ae10b10292c32feccff1603"),
                    RequiresLenientValidation: true);
                yield return new("WycheproofTcId_506", // EdgeCaseMultiplication,Twist
                    Convert.FromHexString("707ee81f113a244c9d87608b12158c50f9ac1f2c8948d170ad16ab0ad866d74b"),
                    Convert.FromHexString("dcffc4c1e1fba5fda9d5c98421d99c257afa90921bc212a046d90f6683e8a467"),
                    Convert.FromHexString("7ecdd54c5e15f7b4061be2c30b5a4884a0256581f87df60d579a3345653eb641"),
                    RequiresLenientValidation: true);
                yield return new("WycheproofTcId_510", // EdgeCaseMultiplication
                    Convert.FromHexString("78ed4c9bf9f44db8d93388985191ecf59226b9c1205fe7e762c327581c75884e"),
                    Convert.FromHexString("ce7295d1227c9062aab9cf02fc5671fb81632e725367f131d4122824a6132d68"),
                    Convert.FromHexString("3740de297ff0122067951e8985247123440e0f27171da99e263d5b4450f59f3d"),
                    RequiresLenientValidation: true);
                yield return new("WycheproofTcId_511", // EdgeCasePrivateKey
                    Convert.FromHexString("a023cdd083ef5bb82f10d62e59e15a6800000000000000000000000000000050"),
                    Convert.FromHexString("6c05871352a451dbe182ed5e6ba554f2034456ffe041a054ff9cc56b8e946376"),
                    Convert.FromHexString("6c05871352a451dbe182ed5e6ba554f2034456ffe041a054ff9cc56b8e946376"));
                yield return new("WycheproofTcId_512", // Twist
                    Convert.FromHexString("58083dd261ad91eff952322ec824c682ffffffffffffffffffffffffffffff5f"),
                    Convert.FromHexString("2eae5ec3dd494e9f2d37d258f873a8e6e9d0dbd1e383ef64d98bb91b3e0be035"),
                    Convert.FromHexString("2eae5ec3dd494e9f2d37d258f873a8e6e9d0dbd1e383ef64d98bb91b3e0be035"),
                    RequiresLenientValidation: true);
                yield return new("WycheproofTcId_515", // EdgeCasePrivateKey
                    Convert.FromHexString("4855555555555555555555555555555555555555555555555555555555555555"),
                    Convert.FromHexString("be3b3edeffaf83c54ae526379b23dd79f1cb41446e3687fef347eb9b5f0dc308"),
                    Convert.FromHexString("cfa83e098829fe82fd4c14355f70829015219942c01e2b85bdd9ac4889ec2921"));
                yield return new("WycheproofTcId_518", // EdgeCasePrivateKey
                    Convert.FromHexString("b8aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa6a"),
                    Convert.FromHexString("be3b3edeffaf83c54ae526379b23dd79f1cb41446e3687fef347eb9b5f0dc308"),
                    Convert.FromHexString("e3c649beae7cc4a0698d519a0a61932ee5493cbb590dbe14db0274cc8611f914"));

            }
        }
    }
}
