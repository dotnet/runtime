// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.XUnitExtensions;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    [ConditionalClass(typeof(PlatformDetection),
        nameof(PlatformDetection.IsNotBrowser),
        nameof(PlatformDetection.IsNotWasi),
        nameof(PlatformDetection.IsNotNetFramework))]
    public static class HpkeKeyTests
    {
        private static IEnumerable<HpkeKem> SupportedKeyAlgorithms =>
            Enum.GetValues(typeof(HpkeKem)).Cast<HpkeKem>().Where(kem => Hpke.IsSupported(KeySuite(kem)));

        private static IEnumerable<HpkeKem> SupportedNistAlgorithms =>
            SupportedKeyAlgorithms.Where(kem => kem is HpkeKem.DHKEM_P256_HKDF_SHA256
                or HpkeKem.DHKEM_P384_HKDF_SHA384 or HpkeKem.DHKEM_P521_HKDF_SHA512);

        public static bool HasX25519 => Hpke.IsSupported(KeySuite(HpkeKem.DHKEM_X25519_HKDF_SHA256));

        public static IEnumerable<object[]> SupportedKems =>
            SupportedKeyAlgorithms.Select(kem => new object[] { kem });

        public static IEnumerable<object[]> SupportedNistKems =>
            SupportedNistAlgorithms.Select(kem => new object[] { kem });

        public static IEnumerable<object[]> SupportedKeyVectorNames
        {
            get
            {
                HashSet<(HpkeKem, string, string, string)> seen = new();

                foreach (HpkeTestVector vector in HpkeTestData.Vectors)
                {
                    if (Hpke.IsSupported(KeySuite(vector.Kem)) &&
                        seen.Add((vector.Kem, vector.KeyMaterial, vector.DecapsulationKey, vector.EncapsulationKey)))
                    {
                        yield return new object[] { vector.Name };
                    }
                }
            }
        }

        public static IEnumerable<object[]> KeyDerivationSuiteVariants
        {
            get
            {
                HashSet<(HpkeKem, HpkeKdf, HpkeAead)> seen = new();

                foreach (HpkeTestVector vector in HpkeTestData.Vectors)
                {
                    HpkeSuite suite = new(vector.Kem, vector.Kdf, vector.Aead);

                    if (!suite.Equals(KeySuite(vector.Kem)) &&
                        Hpke.IsSupported(suite) &&
                        Hpke.IsSupported(KeySuite(vector.Kem)) &&
                        seen.Add((vector.Kem, vector.Kdf, vector.Aead)))
                    {
                        yield return new object[] { vector.Name };
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(SupportedKems))]
        public static void GenerateKey_Roundtrip(HpkeKem kem)
        {
            HpkeSuite suite = KeySuite(kem);

            using (Hpke generated = Hpke.GenerateKey(suite))
            {
                byte[] privateKey = generated.ExportDecapsulationKey();
                byte[] publicKey = generated.ExportEncapsulationKey();

                Assert.Equal(suite, generated.Suite);
                Assert.Equal(suite.DecapsulationKeySizeInBytes, privateKey.Length);
                Assert.Equal(suite.EncapsulationKeySizeInBytes, publicKey.Length);
                AssertKeyExports(generated, privateKey, publicKey);

                foreach (bool useSpan in new[] { false, true })
                {
                    using (Hpke importedPrivate = ImportPrivate(suite, privateKey, useSpan))
                    using (Hpke importedPublic = ImportPublic(suite, publicKey, useSpan))
                    {
                        AssertKeyExports(importedPrivate, privateKey, publicKey);
                        AssertPublicKeyExports(importedPublic, publicKey);
                        AssertKeyPairWorks(generated, importedPublic);
                        AssertKeyPairWorks(importedPrivate, generated);
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(SupportedKeyVectorNames))]
        public static void DeriveKey_KnownAnswerAndInputOwnership(string name)
        {
            HpkeTestVector vector = HpkeTestData.GetVector(name);
            HpkeSuite suite = KeySuite(vector.Kem);
            byte[] material = vector.KeyMaterial.HexToByteArray();
            byte[] expectedPrivate = vector.DecapsulationKey.HexToByteArray();
            byte[] expectedPublic = vector.EncapsulationKey.HexToByteArray();

            foreach (bool useSpan in new[] { false, true })
            {
                int offset = useSpan ? 1 : 0;
                byte[] input = InputBuffer(material, offset);

                using (Hpke key = useSpan
                    ? Hpke.DeriveKey(suite, input.AsSpan(offset, material.Length))
                    : Hpke.DeriveKey(suite, input))
                {
                    input.AsSpan().Fill(0xEC);
                    Assert.Equal(suite, key.Suite);
                    AssertKeyExports(key, expectedPrivate, expectedPublic);
                }
            }
        }

        [Theory]
        [MemberData(nameof(KeyDerivationSuiteVariants))]
        public static void DeriveKey_IndependentOfOuterKdfAndAead(string name)
        {
            HpkeTestVector vector = HpkeTestData.GetVector(name);
            HpkeSuite suite = new(vector.Kem, vector.Kdf, vector.Aead);
            byte[] material = vector.KeyMaterial.HexToByteArray();
            byte[] expectedPrivate = vector.DecapsulationKey.HexToByteArray();
            byte[] expectedPublic = vector.EncapsulationKey.HexToByteArray();

            using (Hpke baseline = Hpke.DeriveKey(KeySuite(vector.Kem), material))
            using (Hpke variant = Hpke.DeriveKey(suite, material.AsSpan()))
            {
                AssertKeyExports(baseline, expectedPrivate, expectedPublic);
                AssertKeyExports(variant, expectedPrivate, expectedPublic);
            }
        }

        [Theory]
        [MemberData(nameof(SupportedKeyVectorNames))]
        public static void ImportDecapsulationKey_KnownAnswerAndInputOwnership(string name)
        {
            HpkeTestVector vector = HpkeTestData.GetVector(name);
            HpkeSuite suite = KeySuite(vector.Kem);
            byte[] expectedPrivate = vector.DecapsulationKey.HexToByteArray();
            byte[] expectedPublic = vector.EncapsulationKey.HexToByteArray();

            foreach (bool useSpan in new[] { false, true })
            {
                int offset = useSpan ? 1 : 0;
                byte[] input = InputBuffer(expectedPrivate, offset);

                using (Hpke key = useSpan
                    ? Hpke.ImportDecapsulationKey(suite, input.AsSpan(offset, expectedPrivate.Length))
                    : Hpke.ImportDecapsulationKey(suite, input))
                using (Hpke peer = Hpke.ImportEncapsulationKey(suite, expectedPublic))
                {
                    input.AsSpan().Fill(0xEC);
                    Assert.Equal(suite, key.Suite);
                    AssertKeyExports(key, expectedPrivate, expectedPublic);
                    AssertKeyPairWorks(key, peer);
                }
            }
        }

        [Theory]
        [MemberData(nameof(SupportedKeyVectorNames))]
        public static void ImportEncapsulationKey_KnownAnswerAndInputOwnership(string name)
        {
            HpkeTestVector vector = HpkeTestData.GetVector(name);
            HpkeSuite suite = KeySuite(vector.Kem);
            byte[] privateKey = vector.DecapsulationKey.HexToByteArray();
            byte[] expected = vector.EncapsulationKey.HexToByteArray();

            using (Hpke recipient = Hpke.ImportDecapsulationKey(suite, privateKey))
            {
                foreach (bool useSpan in new[] { false, true })
                {
                    int offset = useSpan ? 1 : 0;
                    byte[] input = InputBuffer(expected, offset);

                    using (Hpke key = useSpan
                        ? Hpke.ImportEncapsulationKey(suite, input.AsSpan(offset, expected.Length))
                        : Hpke.ImportEncapsulationKey(suite, input))
                    {
                        input.AsSpan().Fill(0xEC);
                        Assert.Equal(suite, key.Suite);
                        AssertPublicKeyExports(key, expected);
                        AssertKeyPairWorks(recipient, key);
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(SupportedKeyVectorNames))]
        public static void ExportKeys_IndependentBuffers(string name)
        {
            HpkeTestVector vector = HpkeTestData.GetVector(name);
            byte[] privateKey = vector.DecapsulationKey.HexToByteArray();

            using (Hpke key = Hpke.ImportDecapsulationKey(KeySuite(vector.Kem), privateKey))
            {
                Assert.NotSame(key.ExportDecapsulationKey(), key.ExportDecapsulationKey());
                Assert.NotSame(key.ExportEncapsulationKey(), key.ExportEncapsulationKey());
            }
        }

        [Theory]
        [MemberData(nameof(SupportedKems))]
        public static void PublicOnlyKey_CapabilitiesAndContextLifetimes(HpkeKem kem)
        {
            HpkeSuite suite = KeySuite(kem);
            HpkeTestVector vector = HpkeTestData.Vectors.First(value => value.Kem == kem);
            byte[] privateBytes = vector.DecapsulationKey.HexToByteArray();
            byte[] publicBytes = vector.EncapsulationKey.HexToByteArray();

            Hpke privateKey = Hpke.ImportDecapsulationKey(suite, privateBytes);
            Hpke publicKey = Hpke.ImportEncapsulationKey(suite, publicBytes);
            byte[] message = [1, 2, 3, 4, 5];
            byte[] aad = [0x71, 0x72];
            byte[] info = [0x91, 0x92, 0x93];
            byte[] psk = new byte[32];
            byte[] pskId = [1];
            publicKey.Seal(message, out byte[] enc, out byte[] ciphertext, aad, info);
            Assert.Equal(message, privateKey.Open(enc, ciphertext, associatedData: aad, info: info));
            Assert.ThrowsAny<CryptographicException>(() => publicKey.ExportDecapsulationKey());
            Assert.ThrowsAny<CryptographicException>(() =>
                publicKey.ExportDecapsulationKey(new byte[suite.DecapsulationKeySizeInBytes]));
            Assert.ThrowsAny<CryptographicException>(() =>
                publicKey.Open(enc, ciphertext, associatedData: aad, info: info));
            Assert.ThrowsAny<CryptographicException>(() => publicKey.CreateRecipient(enc, info));

            using (HpkeSender sender = publicKey.CreateSender(out byte[] baseEnc, info))
            using (HpkeRecipient recipient = privateKey.CreateRecipient(baseEnc, info))
            using (HpkeSender pskSender = publicKey.CreatePskSender(psk, pskId, out byte[] pskEnc, info))
            using (HpkeRecipient pskRecipient = privateKey.CreatePskRecipient(pskEnc, psk, pskId, info))
            {
                Assert.ThrowsAny<CryptographicException>(() =>
                    publicKey.CreatePskRecipient(pskEnc, psk, pskId, info));
                privateKey.Dispose();
                publicKey.Dispose();
                Assert.Equal(message, recipient.Open(sender.Seal(message, associatedData: aad),
                    associatedData: aad));
                Assert.Equal(message, pskRecipient.Open(pskSender.Seal(message, associatedData: aad),
                    associatedData: aad));
                AssertMatchingExports(sender, recipient);
                AssertMatchingExports(pskSender, pskRecipient);
            }
        }

        [Theory]
        [MemberData(nameof(SupportedNistKems))]
        public static void ImportDecapsulationKey_NistScalarBoundaries(HpkeKem kem)
        {
            HpkeSuite suite = KeySuite(kem);
            byte[] order = Curve(kem).Order;
            byte[] belowOrder = (byte[])order.Clone();
            byte[] aboveOrder = (byte[])order.Clone();
            belowOrder[belowOrder.Length - 1]--;
            aboveOrder[aboveOrder.Length - 1]++;
            byte[] allBitsSet = new byte[order.Length];
            allBitsSet.AsSpan().Fill(0xFF);

            foreach (byte[] invalid in new[] { new byte[order.Length], order, aboveOrder, allBitsSet })
            {
                Assert.ThrowsAny<CryptographicException>(() => Hpke.ImportDecapsulationKey(suite, invalid));
                Assert.ThrowsAny<CryptographicException>(() => Hpke.ImportDecapsulationKey(suite, invalid.AsSpan()));
            }

            foreach (bool useSpan in new[] { false, true })
            {
                using (Hpke key = ImportPrivate(suite, belowOrder, useSpan))
                using (Hpke peer = Hpke.ImportEncapsulationKey(suite, key.ExportEncapsulationKey()))
                {
                    AssertKeyExports(key, belowOrder, peer.ExportEncapsulationKey());
                    AssertKeyPairWorks(key, peer);
                }
            }
        }

        [Theory]
        [MemberData(nameof(SupportedNistKems))]
        public static void ImportDecapsulationKey_OneProducesGeneratorPoint(HpkeKem kem)
        {
            HpkeSuite suite = KeySuite(kem);
            ECCurve curve = Curve(kem);
            byte[] scalar = new byte[suite.DecapsulationKeySizeInBytes];
            scalar[scalar.Length - 1] = 1;
            byte[] expectedPublic = new byte[suite.EncapsulationKeySizeInBytes];
            expectedPublic[0] = 4;
            curve.G.X.CopyTo(expectedPublic, 1);
            curve.G.Y.CopyTo(expectedPublic, 1 + curve.G.X.Length);

            foreach (bool useSpan in new[] { false, true })
            {
                using (Hpke key = ImportPrivate(suite, scalar, useSpan))
                {
                    AssertKeyExports(key, scalar, expectedPublic);
                }
            }
        }

        [Theory]
        [MemberData(nameof(SupportedNistKems))]
        public static void ImportEncapsulationKey_RejectsInvalidNistPoints(HpkeKem kem)
        {
            HpkeSuite suite = KeySuite(kem);
            byte[] publicKey = HpkeTestData.Vectors.First(vector => vector.Kem == kem)
                .EncapsulationKey.HexToByteArray();
            List<byte[]> invalidPoints = new();

            foreach (byte prefix in new byte[] { 0, 2, 3, 6, 7, 0xFF })
            {
                byte[] invalid = (byte[])publicKey.Clone();
                invalid[0] = prefix;
                invalidPoints.Add(invalid);
            }

            byte[] zeroPoint = new byte[publicKey.Length];
            zeroPoint[0] = 4;
            invalidPoints.Add(zeroPoint);
            byte[] outOfRange = new byte[publicKey.Length];
            outOfRange.AsSpan().Fill(0xFF);
            outOfRange[0] = 4;
            invalidPoints.Add(outOfRange);

            foreach (byte[] invalid in invalidPoints)
            {
                Assert.ThrowsAny<CryptographicException>(() => Hpke.ImportEncapsulationKey(suite, invalid));
                Assert.ThrowsAny<CryptographicException>(() => Hpke.ImportEncapsulationKey(suite, invalid.AsSpan()));
            }
        }

        [ConditionalTheory(typeof(HpkeKeyTests), nameof(HasX25519))]
        [InlineData(0)]
        [InlineData(255)]
        public static void ImportDecapsulationKey_X25519RawBytes(byte value)
        {
            HpkeSuite suite = KeySuite(HpkeKem.DHKEM_X25519_HKDF_SHA256);
            byte[] scalar = new byte[suite.DecapsulationKeySizeInBytes];
            scalar.AsSpan().Fill(value);

            foreach (bool useSpan in new[] { false, true })
            {
                using (Hpke key = ImportPrivate(suite, scalar, useSpan))
                using (Hpke peer = Hpke.ImportEncapsulationKey(suite, key.ExportEncapsulationKey()))
                {
                    AssertKeyExports(key, scalar, peer.ExportEncapsulationKey());
                    AssertKeyPairWorks(key, peer);
                }
            }
        }

        [Theory]
        [MemberData(nameof(SupportedKeyVectorNames))]
        public static void Dispose_KeysFromFactories(string name)
        {
            HpkeTestVector vector = HpkeTestData.GetVector(name);
            HpkeSuite suite = KeySuite(vector.Kem);
            byte[] material = vector.KeyMaterial.HexToByteArray();
            byte[] privateKey = vector.DecapsulationKey.HexToByteArray();
            byte[] publicKey = vector.EncapsulationKey.HexToByteArray();

            using (Hpke derived = Hpke.DeriveKey(suite, material))
            using (Hpke importedPrivate = Hpke.ImportDecapsulationKey(suite, privateKey))
            using (Hpke importedPublic = Hpke.ImportEncapsulationKey(suite, publicKey))
            using (Hpke generated = Hpke.GenerateKey(suite))
            {
                foreach (Hpke key in new[] { derived, importedPrivate, importedPublic, generated })
                {
                    key.Dispose();
                    key.Dispose();
                    Assert.Throws<ObjectDisposedException>(() => key.ExportEncapsulationKey());
                    Assert.Throws<ObjectDisposedException>(() =>
                        key.ExportEncapsulationKey(new byte[suite.EncapsulationKeySizeInBytes]));
                    Assert.Throws<ObjectDisposedException>(() => key.ExportDecapsulationKey());
                    Assert.Throws<ObjectDisposedException>(() =>
                        key.ExportDecapsulationKey(new byte[suite.DecapsulationKeySizeInBytes]));
                }
            }
        }

        // Key material depends on the KEM only; do not make key coverage depend on SHAKE or ChaCha availability.
        private static HpkeSuite KeySuite(HpkeKem kem) => new(kem, HpkeKdf.HKDF_SHA256, HpkeAead.AES_128_GCM);

        private static Hpke ImportPrivate(HpkeSuite suite, byte[] key, bool useSpan) => useSpan
            ? Hpke.ImportDecapsulationKey(suite, key.AsSpan())
            : Hpke.ImportDecapsulationKey(suite, key);

        private static Hpke ImportPublic(HpkeSuite suite, byte[] key, bool useSpan) => useSpan
            ? Hpke.ImportEncapsulationKey(suite, key.AsSpan())
            : Hpke.ImportEncapsulationKey(suite, key);

        private static byte[] InputBuffer(ReadOnlySpan<byte> value, int padding)
        {
            byte[] input = new byte[value.Length + 2 * padding];
            input.AsSpan().Fill(0xA5);
            value.CopyTo(input.AsSpan(padding));
            return input;
        }

        private static void AssertKeyExports(Hpke key, ReadOnlySpan<byte> privateKey, ReadOnlySpan<byte> publicKey)
        {
            byte[] allocated = key.ExportDecapsulationKey();
            byte[] buffer = new byte[privateKey.Length + 2];
            buffer.AsSpan().Fill(0xA5);

            AssertExtensions.SequenceEqual(privateKey, allocated.AsSpan());
            key.ExportDecapsulationKey(buffer.AsSpan(1, privateKey.Length));
            AssertExtensions.SequenceEqual(privateKey, buffer.AsSpan(1, privateKey.Length));
            Assert.Equal(0xA5, buffer[0]);
            Assert.Equal(0xA5, buffer[buffer.Length - 1]);
            AssertPublicKeyExports(key, publicKey);
        }

        private static void AssertPublicKeyExports(Hpke key, ReadOnlySpan<byte> expected)
        {
            AssertExtensions.SequenceEqual(expected, key.ExportEncapsulationKey().AsSpan());
            byte[] buffer = new byte[expected.Length + 2];
            buffer.AsSpan().Fill(0xA5);
            key.ExportEncapsulationKey(buffer.AsSpan(1, expected.Length));
            AssertExtensions.SequenceEqual(expected, buffer.AsSpan(1, expected.Length));
            Assert.Equal(0xA5, buffer[0]);
            Assert.Equal(0xA5, buffer[buffer.Length - 1]);
        }

        private static void AssertKeyPairWorks(Hpke privateKey, Hpke publicKey)
        {
            byte[] message = [0, 0x11, 0x7F, 0xFF];
            byte[] aad = [0x71, 0x72];
            byte[] info = [0x91, 0x92, 0x93];
            publicKey.Seal(message, out byte[] enc, out byte[] ciphertext, aad, info);
            Assert.Equal(message, privateKey.Open(enc, ciphertext, associatedData: aad, info: info));
            byte[] plaintext = new byte[message.Length];
            privateKey.Open(enc, ciphertext, plaintext.AsSpan(), aad, info);
            Assert.Equal(message, plaintext);
        }

        private static void AssertMatchingExports(HpkeSender sender, HpkeRecipient recipient)
        {
            byte[] context = [0x11, 0x22, 0x33];
            byte[] senderSecret = sender.Export(context, 32);
            byte[] recipientSecret = recipient.Export(context, 32);

            Assert.Equal(senderSecret, recipientSecret);
        }

        private static ECCurve Curve(HpkeKem kem) => kem switch
        {
            HpkeKem.DHKEM_P256_HKDF_SHA256 => EccTestData.GetNistP256ExplicitCurve(),
            HpkeKem.DHKEM_P384_HKDF_SHA384 => EccTestData.GetNistP384ExplicitCurve(),
            HpkeKem.DHKEM_P521_HKDF_SHA512 => EccTestData.GetNistP521ExplicitCurve(),
            _ => throw new InvalidOperationException(),
        };
    }
}
