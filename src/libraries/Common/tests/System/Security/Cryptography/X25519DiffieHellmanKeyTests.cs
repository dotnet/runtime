// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Formats.Asn1;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    // Tests static key-loading and generating. Static members always use the *Implementations.
    [ConditionalClass(typeof(X25519DiffieHellman), nameof(X25519DiffieHellman.IsSupported))]
    public static class X25519DiffieHellmanKeyTests
    {
        public static bool IsNotStrictKeyValidatingPlatform => !X25519DiffieHellmanBaseTests.IsStrictKeyValidatingPlatform;
        private static readonly PbeParameters s_aes128Pbe = new(PbeEncryptionAlgorithm.Aes128Cbc, HashAlgorithmName.SHA256, 2);

        [Fact]
        public static void Generate_Roundtrip()
        {
            using X25519DiffieHellman xdh = X25519DiffieHellman.GenerateKey();

            byte[] publicKey = xdh.ExportPublicKey();
            AssertExtensions.GreaterThanOrEqualTo(publicKey.IndexOfAnyExcept((byte)0), 0);

            byte[] privateKey = xdh.ExportPrivateKey();
            AssertExtensions.GreaterThanOrEqualTo(privateKey.IndexOfAnyExcept((byte)0), 0);

            Assert.Equal(X25519DiffieHellman.PublicKeySizeInBytes, publicKey.Length);
            Assert.Equal(X25519DiffieHellman.PrivateKeySizeInBytes, privateKey.Length);
            AssertExtensions.SequenceNotEqual(publicKey, privateKey);

            using X25519DiffieHellman xdh2 = X25519DiffieHellman.ImportPublicKey(publicKey);
            byte[] publicKey2 = xdh2.ExportPublicKey();
            AssertExtensions.SequenceEqual(publicKey, publicKey2);

            using X25519DiffieHellman xdh3 = X25519DiffieHellman.ImportPrivateKey(privateKey);
            byte[] privateKey2 = xdh3.ExportPrivateKey();
            AssertExtensions.SequenceEqual(privateKey, privateKey2);
        }

        [Fact]
        public static void Rfc7748_TestVector_Alice()
        {
            using X25519DiffieHellman alice = X25519DiffieHellman.ImportPrivateKey(X25519DiffieHellmanTestData.AlicePrivateKey);
            using X25519DiffieHellman bob = X25519DiffieHellman.ImportPublicKey(X25519DiffieHellmanTestData.BobPublicKey);

            byte[] sharedSecret = alice.DeriveRawSecretAgreement(bob);
            AssertExtensions.SequenceEqual(X25519DiffieHellmanTestData.SharedSecret, sharedSecret);

            AssertExtensions.SequenceEqual(X25519DiffieHellmanTestData.AlicePublicKey, alice.ExportPublicKey());
            AssertExtensions.SequenceEqual(X25519DiffieHellmanTestData.AlicePrivateKey, alice.ExportPrivateKey());
        }

        [Fact]
        public static void Rfc7748_TestVector_Bob()
        {
            using X25519DiffieHellman bob = X25519DiffieHellman.ImportPrivateKey(X25519DiffieHellmanTestData.BobPrivateKey);
            using X25519DiffieHellman alice = X25519DiffieHellman.ImportPublicKey(X25519DiffieHellmanTestData.AlicePublicKey);

            byte[] sharedSecret = bob.DeriveRawSecretAgreement(alice);
            AssertExtensions.SequenceEqual(X25519DiffieHellmanTestData.SharedSecret, sharedSecret);

            AssertExtensions.SequenceEqual(X25519DiffieHellmanTestData.BobPublicKey, bob.ExportPublicKey());
            AssertExtensions.SequenceEqual(X25519DiffieHellmanTestData.BobPrivateKey, bob.ExportPrivateKey());
        }

        [Fact]
        public static void DeriveSecretAgreement_Symmetric()
        {
            using X25519DiffieHellman key1 = X25519DiffieHellman.GenerateKey();
            using X25519DiffieHellman key2 = X25519DiffieHellman.GenerateKey();

            byte[] secret1 = key1.DeriveRawSecretAgreement(key2);
            byte[] secret2 = key2.DeriveRawSecretAgreement(key1);

            AssertExtensions.SequenceEqual(secret1, secret2);
        }

        [Fact]
        public static void ImportPrivateKey_Roundtrip_Array()
        {
            byte[] privateKeyBytes = X25519DiffieHellmanTestData.AlicePrivateKey;
            using X25519DiffieHellman xdh = X25519DiffieHellman.ImportPrivateKey(privateKeyBytes);

            byte[] exported = xdh.ExportPrivateKey();
            AssertExtensions.SequenceEqual(privateKeyBytes, exported);
        }

        [Fact]
        public static void ImportPrivateKey_Roundtrip_Span()
        {
            ReadOnlySpan<byte> privateKeyBytes = X25519DiffieHellmanTestData.AlicePrivateKey;
            using X25519DiffieHellman xdh = X25519DiffieHellman.ImportPrivateKey(privateKeyBytes);

            Span<byte> exported = new byte[X25519DiffieHellman.PrivateKeySizeInBytes];
            xdh.ExportPrivateKey(exported);
            AssertExtensions.SequenceEqual(privateKeyBytes, exported);
        }

        [Fact]
        public static void ImportPublicKey_Roundtrip_Array()
        {
            byte[] publicKeyBytes = X25519DiffieHellmanTestData.AlicePublicKey;
            using X25519DiffieHellman xdh = X25519DiffieHellman.ImportPublicKey(publicKeyBytes);

            byte[] exported = xdh.ExportPublicKey();
            AssertExtensions.SequenceEqual(publicKeyBytes, exported);
        }

        [Fact]
        public static void ImportPublicKey_Roundtrip_Span()
        {
            ReadOnlySpan<byte> publicKeyBytes = X25519DiffieHellmanTestData.AlicePublicKey;
            using X25519DiffieHellman xdh = X25519DiffieHellman.ImportPublicKey(publicKeyBytes);

            Span<byte> exported = new byte[X25519DiffieHellman.PublicKeySizeInBytes];
            xdh.ExportPublicKey(exported);
            AssertExtensions.SequenceEqual(publicKeyBytes, exported);
        }

        [Fact]
        public static void ExportSubjectPublicKeyInfo_Roundtrip()
        {
            using X25519DiffieHellman xdh = X25519DiffieHellman.ImportPrivateKey(X25519DiffieHellmanTestData.AlicePrivateKey);
            byte[] spki = xdh.ExportSubjectPublicKeyInfo();

            using X25519DiffieHellman imported = X25519DiffieHellman.ImportSubjectPublicKeyInfo(spki);
            AssertExtensions.SequenceEqual(X25519DiffieHellmanTestData.AlicePublicKey, imported.ExportPublicKey());
        }

        [Fact]
        public static void TryExportSubjectPublicKeyInfo_Roundtrip()
        {
            using X25519DiffieHellman xdh = X25519DiffieHellman.ImportPrivateKey(X25519DiffieHellmanTestData.AlicePrivateKey);
            byte[] buffer = new byte[256];
            AssertExtensions.TrueExpression(xdh.TryExportSubjectPublicKeyInfo(buffer, out int written));

            using X25519DiffieHellman imported = X25519DiffieHellman.ImportSubjectPublicKeyInfo(buffer.AsSpan(0, written));
            AssertExtensions.SequenceEqual(X25519DiffieHellmanTestData.AlicePublicKey, imported.ExportPublicKey());
        }

        [Fact]
        public static void ImportSubjectPublicKeyInfo_KnownValue()
        {
            using X25519DiffieHellman xdh = X25519DiffieHellman.ImportSubjectPublicKeyInfo(X25519DiffieHellmanTestData.AliceSpki);
            AssertExtensions.SequenceEqual(X25519DiffieHellmanTestData.AlicePublicKey, xdh.ExportPublicKey());
        }

        [Fact]
        public static void ExportPkcs8PrivateKey_Roundtrip()
        {
            using X25519DiffieHellman xdh = X25519DiffieHellman.ImportPrivateKey(X25519DiffieHellmanTestData.AlicePrivateKey);
            byte[] pkcs8 = xdh.ExportPkcs8PrivateKey();

            using X25519DiffieHellman imported = X25519DiffieHellman.ImportPkcs8PrivateKey(pkcs8);
            AssertExtensions.SequenceEqual(X25519DiffieHellmanTestData.AlicePrivateKey, imported.ExportPrivateKey());
            AssertExtensions.SequenceEqual(X25519DiffieHellmanTestData.AlicePublicKey, imported.ExportPublicKey());
        }

        [Fact]
        public static void TryExportPkcs8PrivateKey_Roundtrip()
        {
            using X25519DiffieHellman xdh = X25519DiffieHellman.ImportPrivateKey(X25519DiffieHellmanTestData.AlicePrivateKey);
            byte[] buffer = new byte[256];
            AssertExtensions.TrueExpression(xdh.TryExportPkcs8PrivateKey(buffer, out int written));

            using X25519DiffieHellman imported = X25519DiffieHellman.ImportPkcs8PrivateKey(buffer.AsSpan(0, written));
            AssertExtensions.SequenceEqual(X25519DiffieHellmanTestData.AlicePrivateKey, imported.ExportPrivateKey());
        }

        [Fact]
        public static void ImportPkcs8PrivateKey_KnownValue()
        {
            using X25519DiffieHellman xdh = X25519DiffieHellman.ImportPkcs8PrivateKey(X25519DiffieHellmanTestData.AlicePkcs8);
            AssertExtensions.SequenceEqual(X25519DiffieHellmanTestData.AlicePrivateKey, xdh.ExportPrivateKey());
            AssertExtensions.SequenceEqual(X25519DiffieHellmanTestData.AlicePublicKey, xdh.ExportPublicKey());
        }

        [Fact]
        public static void ExportEncryptedPkcs8PrivateKey_Roundtrip()
        {
            using X25519DiffieHellman xdh = X25519DiffieHellman.ImportPrivateKey(X25519DiffieHellmanTestData.AlicePrivateKey);
            byte[] encrypted = xdh.ExportEncryptedPkcs8PrivateKey("test", s_aes128Pbe);

            using X25519DiffieHellman imported = X25519DiffieHellman.ImportEncryptedPkcs8PrivateKey("test", encrypted);
            AssertExtensions.SequenceEqual(X25519DiffieHellmanTestData.AlicePrivateKey, imported.ExportPrivateKey());
        }

        [Fact]
        public static void ExportEncryptedPkcs8PrivateKey_Roundtrip_BytePassword()
        {
            using X25519DiffieHellman xdh = X25519DiffieHellman.ImportPrivateKey(X25519DiffieHellmanTestData.AlicePrivateKey);
            byte[] encrypted = xdh.ExportEncryptedPkcs8PrivateKey("test"u8, s_aes128Pbe);

            using X25519DiffieHellman imported = X25519DiffieHellman.ImportEncryptedPkcs8PrivateKey("test"u8, encrypted);
            AssertExtensions.SequenceEqual(X25519DiffieHellmanTestData.AlicePrivateKey, imported.ExportPrivateKey());
        }

        [Fact]
        public static void ImportFromPem_PublicKey()
        {
            string pem =
                "-----BEGIN PUBLIC KEY-----\n" +
                "MCowBQYDK2VuAyEAhSDwCYkwp1R0i33ctD73Wg2/Og0mOBr066SpjqqbTmo=\n" +
                "-----END PUBLIC KEY-----";

            using X25519DiffieHellman xdh = X25519DiffieHellman.ImportFromPem(pem);
            AssertExtensions.SequenceEqual(X25519DiffieHellmanTestData.AlicePublicKey, xdh.ExportPublicKey());
        }

        [Fact]
        public static void ImportFromPem_PrivateKey()
        {
            using X25519DiffieHellman xdh = X25519DiffieHellman.ImportPrivateKey(X25519DiffieHellmanTestData.AlicePrivateKey);
            string pem = xdh.ExportPkcs8PrivateKeyPem();

            using X25519DiffieHellman imported = X25519DiffieHellman.ImportFromPem(pem);
            AssertExtensions.SequenceEqual(X25519DiffieHellmanTestData.AlicePrivateKey, imported.ExportPrivateKey());
        }

        [Fact]
        public static void ImportFromEncryptedPem_Roundtrip()
        {
            using X25519DiffieHellman xdh = X25519DiffieHellman.ImportPrivateKey(X25519DiffieHellmanTestData.AlicePrivateKey);
            string pem = xdh.ExportEncryptedPkcs8PrivateKeyPem("test", s_aes128Pbe);

            using X25519DiffieHellman imported = X25519DiffieHellman.ImportFromEncryptedPem(pem, "test");
            AssertExtensions.SequenceEqual(X25519DiffieHellmanTestData.AlicePrivateKey, imported.ExportPrivateKey());
        }

        [Fact]
        public static void ExportSubjectPublicKeyInfoPem_Roundtrip()
        {
            using X25519DiffieHellman xdh = X25519DiffieHellman.ImportPrivateKey(X25519DiffieHellmanTestData.AlicePrivateKey);
            string pem = xdh.ExportSubjectPublicKeyInfoPem();

            PemFields fields = PemEncoding.Find(pem.AsSpan());
            Assert.Equal("PUBLIC KEY", pem.AsSpan()[fields.Label].ToString());

            using X25519DiffieHellman imported = X25519DiffieHellman.ImportFromPem(pem);
            AssertExtensions.SequenceEqual(X25519DiffieHellmanTestData.AlicePublicKey, imported.ExportPublicKey());
        }

        [Fact]
        public static void ExportPkcs8PrivateKeyPem_Roundtrip()
        {
            using X25519DiffieHellman xdh = X25519DiffieHellman.ImportPrivateKey(X25519DiffieHellmanTestData.AlicePrivateKey);
            string pem = xdh.ExportPkcs8PrivateKeyPem();

            PemFields fields = PemEncoding.Find(pem.AsSpan());
            Assert.Equal("PRIVATE KEY", pem.AsSpan()[fields.Label].ToString());

            using X25519DiffieHellman imported = X25519DiffieHellman.ImportFromPem(pem);
            AssertExtensions.SequenceEqual(X25519DiffieHellmanTestData.AlicePrivateKey, imported.ExportPrivateKey());
        }

        [Fact]
        public static void DeriveSecretAgreement_PublicKeyOnly_Throws()
        {
            using X25519DiffieHellman publicOnly = X25519DiffieHellman.ImportPublicKey(X25519DiffieHellmanTestData.AlicePublicKey);
            using X25519DiffieHellman other = X25519DiffieHellman.ImportPublicKey(X25519DiffieHellmanTestData.BobPublicKey);

            Assert.Throws<CryptographicException>(() => publicOnly.DeriveRawSecretAgreement(other));
        }

        [Fact]
        public static void ExportPrivateKey_PublicKeyOnly_Throws()
        {
            using X25519DiffieHellman publicOnly = X25519DiffieHellman.ImportPublicKey(X25519DiffieHellmanTestData.AlicePublicKey);

            Assert.Throws<CryptographicException>(() => publicOnly.ExportPrivateKey());
        }

        [Fact]
        public static void PrivateKey_Roundtrip_UnclampedScalar_AllPreservationBits()
        {
            // A private key where bytes[0] low 3 bits = 0b111 AND bytes[31] high 2 bits = 0b11.
            // This exercises the maximum scalar fixup on Windows CNG (all preservation bits set).
            // Bob's RFC 7748 key has this property: bytes[0]=0x5d (low 3=0b101), bytes[31]=0xeb (high 2=0b11).
            byte[] privateKey = X25519DiffieHellmanTestData.BobPrivateKey;
            Assert.Equal(0b101, privateKey[0] & 0b111);
            Assert.Equal(0b11000000, privateKey[^1] & 0b11000000);

            using X25519DiffieHellman xdh = X25519DiffieHellman.ImportPrivateKey(privateKey);

            // Private key must roundtrip with original unclamped bits preserved
            AssertExtensions.SequenceEqual(privateKey, xdh.ExportPrivateKey());

            // Public key must still be correct (computed from the clamped scalar)
            AssertExtensions.SequenceEqual(X25519DiffieHellmanTestData.BobPublicKey, xdh.ExportPublicKey());

            // PKCS#8 roundtrip must also preserve the original private key
            byte[] pkcs8 = xdh.ExportPkcs8PrivateKey();
            using X25519DiffieHellman reimported = X25519DiffieHellman.ImportPkcs8PrivateKey(pkcs8);
            AssertExtensions.SequenceEqual(privateKey, reimported.ExportPrivateKey());
        }

        [Fact]
        public static void PrivateKey_Roundtrip_UnclampedScalar_NoPreservationBits()
        {
            byte[] privateKey = (byte[])X25519DiffieHellmanTestData.AlicePrivateKey.Clone();
            privateKey[0] &= 0b11111000;
            privateKey[^1] &= 0b00111111;

            Assert.Equal(0, privateKey[0] & 0b111);
            Assert.Equal(0, privateKey[^1] & 0b11000000);

            using X25519DiffieHellman xdh = X25519DiffieHellman.ImportPrivateKey(privateKey);
            AssertExtensions.SequenceEqual(privateKey, xdh.ExportPrivateKey());

            byte[] pkcs8 = xdh.ExportPkcs8PrivateKey();
            using X25519DiffieHellman reimported = X25519DiffieHellman.ImportPkcs8PrivateKey(pkcs8);
            AssertExtensions.SequenceEqual(privateKey, reimported.ExportPrivateKey());
        }

        [Fact]
        public static void PrivateKey_Roundtrip_ClampedScalar()
        {
            // Construct a private key that is ALREADY properly clamped per RFC 7748:
            // bytes[0] low 3 bits = 0, bytes[31] bit 7 = 0 and bit 6 = 1.
            // Importing this key should be a no-op fixup; the key roundtrips unchanged.
            byte[] privateKey = (byte[])X25519DiffieHellmanTestData.AlicePrivateKey.Clone();
            privateKey[0] &= 0b11111000;
            privateKey[^1] &= 0b01111111;
            privateKey[^1] |= 0b01000000;

            Assert.Equal(0, privateKey[0] & 0b111);
            Assert.Equal(0b01000000, privateKey[^1] & 0b11000000);

            using X25519DiffieHellman xdh = X25519DiffieHellman.ImportPrivateKey(privateKey);
            AssertExtensions.SequenceEqual(privateKey, xdh.ExportPrivateKey());

            // PKCS#8 roundtrip
            byte[] pkcs8 = xdh.ExportPkcs8PrivateKey();
            using X25519DiffieHellman reimported = X25519DiffieHellman.ImportPkcs8PrivateKey(pkcs8);
            AssertExtensions.SequenceEqual(privateKey, reimported.ExportPrivateKey());
        }

        [Fact]
        public static void PrivateKey_ClampedAndUnclamped_SamePublicKey()
        {
            // The unclamped and clamped forms of the same key should produce the same public key,
            // because the DH computation always operates on the clamped scalar.
            byte[] unclamped = (byte[])X25519DiffieHellmanTestData.AlicePrivateKey.Clone();
            byte[] clamped = (byte[])unclamped.Clone();
            clamped[0] &= 0b11111000;
            clamped[^1] &= 0b01111111;
            clamped[^1] |= 0b01000000;

            using X25519DiffieHellman xdhUnclamped = X25519DiffieHellman.ImportPrivateKey(unclamped);
            using X25519DiffieHellman xdhClamped = X25519DiffieHellman.ImportPrivateKey(clamped);

            AssertExtensions.SequenceEqual(xdhUnclamped.ExportPublicKey(), xdhClamped.ExportPublicKey());
        }

        [Fact]
        public static void PrivateKey_Roundtrip_MaxPreservation()
        {
            // A key with bytes[0]=0xFF and bytes[31]=0xFF — maximum preservation needed.
            // The scalar fixup would clamp bytes[0] to 0xF8 and bytes[31] to 0x7F,
            // but the original 0xFF values must be restored on export.
            byte[] privateKey = new byte[X25519DiffieHellman.PrivateKeySizeInBytes];
            privateKey.AsSpan().Fill(0xFF);

            using X25519DiffieHellman xdh = X25519DiffieHellman.ImportPrivateKey(privateKey);
            AssertExtensions.SequenceEqual(privateKey, xdh.ExportPrivateKey());
        }

        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(9)]
        [InlineData(18)]
        [ConditionalTheory(typeof(X25519DiffieHellmanKeyTests), nameof(IsNotStrictKeyValidatingPlatform))]
        public static void PublicKey_NonCanonical_Roundtrip(int offset)
        {
            // RFC 7748 Section 5: Non-canonical u-coordinates are p through 2^255 - 1.
            // Construct p + offset in little-endian.
            // p = 2^255 - 19 = 0xED_FFFF...FFFF_7F (little-endian)
            byte[] nonCanonical = new byte[X25519DiffieHellman.PublicKeySizeInBytes];
            nonCanonical.AsSpan().Fill(0xFF);
            nonCanonical[0] = (byte)(0xED + offset);
            nonCanonical[^1] = 0x7F;

            using X25519DiffieHellman xdh = X25519DiffieHellman.ImportPublicKey(nonCanonical);
            byte[] exported = xdh.ExportPublicKey();
            AssertExtensions.SequenceEqual(nonCanonical, exported);
        }

        [InlineData(3)]
        [InlineData(18)]
        [ConditionalTheory(typeof(X25519DiffieHellmanKeyTests), nameof(IsNotStrictKeyValidatingPlatform))]
        public static void PublicKey_NonCanonical_HighBitSet_Roundtrip(int offset)
        {
            // RFC 7748 says the high bit MUST be masked, but the original
            // byte should be preserved on export for roundtripping.
            byte[] nonCanonical = new byte[X25519DiffieHellman.PublicKeySizeInBytes];
            nonCanonical.AsSpan().Fill(0xFF);
            nonCanonical[0] = (byte)(0xED + offset);

            using X25519DiffieHellman xdh = X25519DiffieHellman.ImportPublicKey(nonCanonical);
            byte[] exported = xdh.ExportPublicKey();
            AssertExtensions.SequenceEqual(nonCanonical, exported);
        }
    }
}
