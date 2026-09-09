// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    public static class HpkeTests
    {
        [Fact]
        public static void GenerateKey_NullSuite()
        {
            AssertExtensions.Throws<ArgumentNullException>("suite", () => Hpke.GenerateKey((HpkeSuite)null));
        }

        [Fact]
        public static void DeriveKey_NullArguments()
        {
            HpkeSuite suite = new(
                HpkeKem.DHKEM_P256_HKDF_SHA256,
                HpkeKdf.HKDF_SHA256,
                HpkeAead.AES_128_GCM);

            AssertExtensions.Throws<ArgumentNullException>(
                "suite",
                () => Hpke.DeriveKey((HpkeSuite)null, Array.Empty<byte>()));
            AssertExtensions.Throws<ArgumentNullException>(
                "ikm",
                () => Hpke.DeriveKey(suite, (byte[])null));
        }

        [Theory]
        [InlineData(
            HpkeKem.DHKEM_P256_HKDF_SHA256,
            "4270e54ffd08d79d5928020af4686d8f6b7d35dbe470265f1f5aa22816ce860e",
            "4995788ef4b9d6132b249ce59a77281493eb39af373d236a1fe415cb0c2d7beb",
            "04a92719c6195d5085104f469a8b9814d5838ff72b60501e2c4466e5e67b325a" +
            "c98536d7b61a1af4b78e5b7f951c0900be863c403ce65c9bfcb9382657222d18c4")]
        [InlineData(
            HpkeKem.DHKEM_P384_HKDF_SHA384,
            "65fca3ea3b6db29a62bff28ec53c08710fab10b3798e59b678d3224296d5883f" +
            "039123471784ce57b0d85a17cd521196",
            "679172205e04663f40fda1018cd46c18ebaa876ede6998ba86b051614ca4d5e4" +
            "bfbea34b720617a4b958cc80f6305244",
            "04a5f53da8564364255bc36850df793672782a5c9e4a7fb5fb2e2146eb12e4d8" +
            "477ab1f326a361dfd1e41212109510e813380547c68c0964c1908f16f67b902a" +
            "061be27b2f8b43f1fab1bf0dbf89f5167ce80aca2c210b8fc0f040699db9ee1229")]
        [InlineData(
            HpkeKem.DHKEM_P521_HKDF_SHA512,
            "2ad954bbe39b7122529f7dde780bff626cd97f850d0784a432784e69d86eccaa" +
            "de43b6c10a8ffdb94bf943c6da479db137914ec835a7e715e36e45e29b587bab3bf1",
            "01462680369ae375e4b3791070a7458ed527842f6a98a79ff5e0d4cbde83c2719" +
            "6a3916956655523a6a2556a7af62c5cadabe2ef9da3760bb21e005202f7b2462847",
            "0401b45498c1714e2dce167d3caf162e45e0642afc7ed435df7902ccae0e84ba0f7d" +
            "373f646b7738bbbdca11ed91bdeae3cdcba3301f2457be452f271fa6837580e661" +
            "012af49583a62e48d44bed350c7118c0d8dc861c238c72a2bda17f64704f464b573" +
            "38e7f40b60959480c0e58e6559b190d81663ed816e523b6b6a418f66d2451ec64")]
        [InlineData(
            HpkeKem.DHKEM_P521_HKDF_SHA512,
            "39a28dc317c3e48b908948f99d608059f882d3d09c0541824bc25f94e6dee7aa0" +
            "df1c644296b06fbb76e84aef5008f8a908e08fbabadf70658538d74753a85f8856a",
            "009227b4b91cf1eb6eecb6c0c0bae93a272d24e11c63bd4c34a581c49f9c3ca0" +
            "1c16bbd32a0a1fac22784f2ae985c85f183baad103b2d02aee787179dfc1a94fea11",
            "0400b81073b1612cf7fdb6db07b35cf4bc17bda5854f3d270ecd9ea99f6c07b46795" +
            "b8014b66c523ceed6f4829c18bc3886c891b63fa902500ce3ddeb1fbec7e608ac7" +
            "0050b76a0a7fc081dbf1cb30b005981113e635eb501a973aba662d7f16fcc12897d" +
            "d752d657d37774bb16197c0d9724eecc1ed65349fb6ac1f280749e7669766f8cd")]
        [InlineData(
            HpkeKem.DHKEM_X25519_HKDF_SHA256,
            "7268600d403fce431561aef583ee1613527cff655c1343f29812e66706df3234",
            "52c4a758a802cd8b936eceea314432798d5baf2d7e9235dc084ab1b9cfa2f736",
            "37fda3567bdbd628e88668c3c8d7e97d1d1253b6d4ea6d44c150f741f1bf4431")]
        public static void DeriveKey_KnownAnswer(HpkeKem kem, string ikmHex, string privateKeyHex, string publicKeyHex)
        {
            HpkeSuite suite = new(kem, HpkeKdf.HKDF_SHA256, HpkeAead.AES_128_GCM);

            if (!Hpke.IsSupported(suite))
            {
                Assert.Throws<PlatformNotSupportedException>(
                    () => Hpke.DeriveKey(suite, Convert.FromHexString(ikmHex)));
                return;
            }

            byte[] ikm = Convert.FromHexString(ikmHex);
            byte[] expectedPrivateKey = Convert.FromHexString(privateKeyHex);
            byte[] expectedPublicKey = Convert.FromHexString(publicKeyHex);

            try
            {
                using (Hpke keyFromArray = Hpke.DeriveKey(suite, ikm))
                using (Hpke keyFromSpan = Hpke.DeriveKey(suite, ikm.AsSpan()))
                {
                    byte[] arrayPrivateKey = keyFromArray.ExportDecapsulationKey();
                    byte[] spanPrivateKey = new byte[suite.DecapsulationKeySizeInBytes];

                    try
                    {
                        byte[] arrayPublicKey = keyFromArray.ExportEncapsulationKey();
                        byte[] spanPublicKey = new byte[suite.EncapsulationKeySizeInBytes];

                        keyFromSpan.ExportDecapsulationKey(spanPrivateKey);
                        keyFromSpan.ExportEncapsulationKey(spanPublicKey);
                        Assert.Equal(expectedPrivateKey, arrayPrivateKey);
                        Assert.Equal(arrayPrivateKey, spanPrivateKey);
                        Assert.Equal(expectedPublicKey, arrayPublicKey);
                        Assert.Equal(arrayPublicKey, spanPublicKey);

                        using (Hpke privateFromArray = Hpke.ImportDecapsulationKey(suite, expectedPrivateKey))
                        using (Hpke privateFromSpan = Hpke.ImportDecapsulationKey(suite, expectedPrivateKey.AsSpan()))
                        using (Hpke publicFromArray = Hpke.ImportEncapsulationKey(suite, expectedPublicKey))
                        using (Hpke publicFromSpan = Hpke.ImportEncapsulationKey(suite, expectedPublicKey.AsSpan()))
                        {
                            privateFromArray.ExportDecapsulationKey(spanPrivateKey);
                            Assert.Equal(expectedPrivateKey, spanPrivateKey);
                            privateFromSpan.ExportDecapsulationKey(spanPrivateKey);
                            Assert.Equal(expectedPrivateKey, spanPrivateKey);
                            Assert.Equal(expectedPublicKey, privateFromArray.ExportEncapsulationKey());
                            Assert.Equal(expectedPublicKey, privateFromSpan.ExportEncapsulationKey());
                            Assert.Equal(expectedPublicKey, publicFromArray.ExportEncapsulationKey());
                            Assert.Equal(expectedPublicKey, publicFromSpan.ExportEncapsulationKey());
                        }
                    }
                    finally
                    {
                        CryptographicOperations.ZeroMemory(arrayPrivateKey);
                        CryptographicOperations.ZeroMemory(spanPrivateKey);
                    }
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(ikm);
                CryptographicOperations.ZeroMemory(expectedPrivateKey);
            }
        }

        [Fact]
        public static void ImportKey_ArgumentValidation()
        {
            HpkeSuite suite = new(HpkeKem.DHKEM_P256_HKDF_SHA256, HpkeKdf.HKDF_SHA256, HpkeAead.AES_128_GCM);
            AssertExtensions.Throws<ArgumentNullException>("source", () => Hpke.ImportDecapsulationKey(suite, (byte[])null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => Hpke.ImportEncapsulationKey(suite, (byte[])null));
            AssertExtensions.Throws<ArgumentNullException>("suite", () => Hpke.ImportDecapsulationKey(null, Array.Empty<byte>()));
            AssertExtensions.Throws<ArgumentNullException>("suite", () => Hpke.ImportDecapsulationKey(null, ReadOnlySpan<byte>.Empty));
            AssertExtensions.Throws<ArgumentNullException>("suite", () => Hpke.ImportEncapsulationKey(null, Array.Empty<byte>()));
            AssertExtensions.Throws<ArgumentNullException>("suite", () => Hpke.ImportEncapsulationKey(null, ReadOnlySpan<byte>.Empty));

            foreach (HpkeKem kem in Enum.GetValues<HpkeKem>())
            {
                suite = new HpkeSuite(kem, HpkeKdf.HKDF_SHA256, HpkeAead.AES_128_GCM);

                foreach (int length in new[] { 0, suite.DecapsulationKeySizeInBytes - 1, suite.DecapsulationKeySizeInBytes + 1 })
                {
                    byte[] source = new byte[length];
                    AssertExtensions.Throws<ArgumentException>("source", () => Hpke.ImportDecapsulationKey(suite, source));
                    AssertExtensions.Throws<ArgumentException>("source", () => Hpke.ImportDecapsulationKey(suite, source.AsSpan()));
                }

                foreach (int length in new[] { 0, suite.EncapsulationKeySizeInBytes - 1, suite.EncapsulationKeySizeInBytes + 1 })
                {
                    byte[] source = new byte[length];
                    AssertExtensions.Throws<ArgumentException>("source", () => Hpke.ImportEncapsulationKey(suite, source));
                    AssertExtensions.Throws<ArgumentException>("source", () => Hpke.ImportEncapsulationKey(suite, source.AsSpan()));
                }

                if (!Hpke.IsSupported(suite))
                {
                    byte[] privateKey = new byte[suite.DecapsulationKeySizeInBytes];
                    byte[] publicKey = new byte[suite.EncapsulationKeySizeInBytes];
                    Assert.Throws<PlatformNotSupportedException>(() => Hpke.ImportDecapsulationKey(suite, privateKey));
                    Assert.Throws<PlatformNotSupportedException>(() => Hpke.ImportDecapsulationKey(suite, privateKey.AsSpan()));
                    Assert.Throws<PlatformNotSupportedException>(() => Hpke.ImportEncapsulationKey(suite, publicKey));
                    Assert.Throws<PlatformNotSupportedException>(() => Hpke.ImportEncapsulationKey(suite, publicKey.AsSpan()));
                }
            }
        }

        [Theory]
        [InlineData(HpkeKem.DHKEM_P256_HKDF_SHA256)]
        [InlineData(HpkeKem.DHKEM_P384_HKDF_SHA384)]
        [InlineData(HpkeKem.DHKEM_P521_HKDF_SHA512)]
        public static void ImportDecapsulationKey_ScalarBoundaries(HpkeKem kem)
        {
            HpkeSuite suite = new(kem, HpkeKdf.HKDF_SHA256, HpkeAead.AES_128_GCM);

            if (!Hpke.IsSupported(suite))
            {
                Assert.Throws<PlatformNotSupportedException>(() => Hpke.GenerateKey(suite));
                return;
            }

            byte[] order = kem switch
            {
                HpkeKem.DHKEM_P256_HKDF_SHA256 => EccTestData.GetNistP256ExplicitCurve().Order,
                HpkeKem.DHKEM_P384_HKDF_SHA384 => EccTestData.GetNistP384ExplicitCurve().Order,
                HpkeKem.DHKEM_P521_HKDF_SHA512 => EccTestData.GetNistP521ExplicitCurve().Order,
                _ => throw new InvalidOperationException(),
            };
            byte[] allBitsSet = new byte[order.Length];
            allBitsSet.AsSpan().Fill(0xFF);
            byte[] orderPlusOne = (byte[])order.Clone();
            orderPlusOne[^1]++;

            foreach (byte[] invalid in new[] { new byte[order.Length], order, orderPlusOne, allBitsSet })
            {
                Assert.Throws<CryptographicException>(() => Hpke.ImportDecapsulationKey(suite, invalid));
                Assert.Throws<CryptographicException>(() => Hpke.ImportDecapsulationKey(suite, invalid.AsSpan()));
            }

            byte[] one = new byte[order.Length];
            one[^1] = 1;
            byte[] orderMinusOne = (byte[])order.Clone();
            orderMinusOne[^1]--;

            foreach (byte[] valid in new[] { one, orderMinusOne })
            {
                byte[] exported = new byte[valid.Length];

                using (Hpke fromArray = Hpke.ImportDecapsulationKey(suite, valid))
                using (Hpke fromSpan = Hpke.ImportDecapsulationKey(suite, valid.AsSpan()))
                {
                    fromArray.ExportDecapsulationKey(exported);
                    Assert.Equal(valid, exported);
                    fromSpan.ExportDecapsulationKey(exported);
                    Assert.Equal(valid, exported);
                    fromArray.Seal("message"u8, out byte[] enc, out byte[] ciphertext);
                    AssertExtensions.SequenceEqual("message"u8, fromSpan.Open(enc, ciphertext));
                }

                CryptographicOperations.ZeroMemory(exported);
            }
        }

        [Theory]
        [InlineData(HpkeKem.DHKEM_P256_HKDF_SHA256)]
        [InlineData(HpkeKem.DHKEM_P384_HKDF_SHA384)]
        [InlineData(HpkeKem.DHKEM_P521_HKDF_SHA512)]
        public static void ImportEncapsulationKey_InvalidNistPoint(HpkeKem kem)
        {
            HpkeSuite suite = new(kem, HpkeKdf.HKDF_SHA256, HpkeAead.AES_128_GCM);

            if (!Hpke.IsSupported(suite))
            {
                Assert.Throws<PlatformNotSupportedException>(() => Hpke.GenerateKey(suite));
                return;
            }

            foreach (byte prefix in new byte[] { 0, 2, 3, 4, 6, 7, 0xFF })
            {
                byte[] source = new byte[suite.EncapsulationKeySizeInBytes];
                source[0] = prefix;
                byte[] original = (byte[])source.Clone();
                Assert.ThrowsAny<CryptographicException>(() => Hpke.ImportEncapsulationKey(suite, source));
                Assert.ThrowsAny<CryptographicException>(() => Hpke.ImportEncapsulationKey(suite, source.AsSpan()));
                Assert.Equal(original, source);
            }
        }

        [Theory]
        [MemberData(nameof(OpenSuiteData))]
        public static void ImportKey_OperationsAndOwnership(HpkeKem kem, HpkeKdf kdf, HpkeAead aead)
        {
            HpkeSuite suite = new(kem, kdf, aead);

            if (!Hpke.IsSupported(suite))
            {
                Assert.Throws<PlatformNotSupportedException>(
                    () => Hpke.ImportDecapsulationKey(suite, new byte[suite.DecapsulationKeySizeInBytes]));
                Assert.Throws<PlatformNotSupportedException>(
                    () => Hpke.ImportEncapsulationKey(suite, new byte[suite.EncapsulationKeySizeInBytes]));
                return;
            }

            using (Hpke original = Hpke.GenerateKey(suite))
            {
                byte[] privateKey = original.ExportDecapsulationKey();
                byte[] publicKey = original.ExportEncapsulationKey();

                try
                {
                    foreach (bool useSpan in new[] { false, true })
                    {
                        int padding = useSpan ? 2 : 0;
                        int offset = useSpan ? 1 : 0;
                        byte[] privateSource = new byte[privateKey.Length + padding];
                        byte[] publicSource = new byte[publicKey.Length + padding];
                        privateSource.AsSpan().Fill(0xA5);
                        publicSource.AsSpan().Fill(0xA5);
                        privateKey.CopyTo(privateSource, offset);
                        publicKey.CopyTo(publicSource, offset);

                        try
                        {
                            using (Hpke importedPrivate = useSpan
                                ? Hpke.ImportDecapsulationKey(suite, privateSource.AsSpan(1, privateKey.Length))
                                : Hpke.ImportDecapsulationKey(suite, privateSource))
                            using (Hpke importedPublic = useSpan
                                ? Hpke.ImportEncapsulationKey(suite, publicSource.AsSpan(1, publicKey.Length))
                                : Hpke.ImportEncapsulationKey(suite, publicSource))
                            {
                                Assert.Same(suite, importedPrivate.Suite);
                                Assert.Same(suite, importedPublic.Suite);
                                Assert.Equal(publicKey, importedPrivate.ExportEncapsulationKey());
                                Assert.Equal(publicKey, importedPublic.ExportEncapsulationKey());
                                AssertExtensions.SequenceEqual(privateKey.AsSpan(), privateSource.AsSpan(offset, privateKey.Length));
                                AssertExtensions.SequenceEqual(publicKey.AsSpan(), publicSource.AsSpan(offset, publicKey.Length));

                                if (useSpan)
                                {
                                    Assert.Equal(0xA5, privateSource[0]);
                                    Assert.Equal(0xA5, privateSource[^1]);
                                    Assert.Equal(0xA5, publicSource[0]);
                                    Assert.Equal(0xA5, publicSource[^1]);
                                }

                                privateSource.AsSpan().Clear();
                                publicSource.AsSpan().Clear();
                                original.Dispose();
                                byte[] plaintext = "message"u8.ToArray();
                                byte[] aad = "associated data"u8.ToArray();
                                byte[] info = "application context"u8.ToArray();
                                importedPublic.Seal(plaintext, out byte[] enc, out byte[] ciphertext, aad, info);
                                Assert.Equal(plaintext, importedPrivate.Open(enc, ciphertext, aad, info));
                                byte[] opened = new byte[plaintext.Length];
                                importedPrivate.Open(enc, ciphertext, opened.AsSpan(), aad, info);
                                Assert.Equal(plaintext, opened);

                                byte[] psk = new byte[32];
                                byte[] pskId = [1];
                                using (HpkeSender sender = importedPublic.CreateSender(out byte[] contextEnc, info))
                                using (HpkeRecipient recipient = importedPrivate.CreateRecipient(contextEnc, info))
                                using (HpkeSender pskSender = importedPublic.CreatePskSender(psk, pskId, out byte[] pskEnc, info))
                                using (HpkeRecipient pskRecipient = importedPrivate.CreatePskRecipient(pskEnc, psk, pskId, info))
                                {
                                    Assert.ThrowsAny<CryptographicException>(() => importedPublic.ExportDecapsulationKey());
                                    Assert.ThrowsAny<CryptographicException>(
                                        () => importedPublic.ExportDecapsulationKey(new byte[privateKey.Length]));
                                    Assert.ThrowsAny<CryptographicException>(() => importedPublic.Open(enc, ciphertext, aad, info));
                                    Assert.ThrowsAny<CryptographicException>(
                                        () => importedPublic.Open(enc, ciphertext, opened.AsSpan(), aad, info));
                                    Assert.ThrowsAny<CryptographicException>(() => importedPublic.CreateRecipient(contextEnc, info));
                                    Assert.ThrowsAny<CryptographicException>(
                                        () => importedPublic.CreatePskRecipient(pskEnc, psk, pskId, info));

                                    for (int i = 0; i < 2; i++)
                                    {
                                        Assert.Equal(plaintext, recipient.Open(sender.Seal(plaintext, aad), aad));
                                        Assert.Equal(plaintext, pskRecipient.Open(pskSender.Seal(plaintext, aad), aad));
                                    }

                                    Assert.Equal(sender.Export([], 32), recipient.Export([], 32));
                                    Assert.Equal(pskSender.Export([], 32), pskRecipient.Export([], 32));
                                    importedPublic.Dispose();
                                    importedPrivate.Dispose();
                                    Assert.Throws<ObjectDisposedException>(() => importedPublic.ExportEncapsulationKey());
                                    Assert.Throws<ObjectDisposedException>(() => importedPrivate.ExportDecapsulationKey());
                                    Assert.Equal(plaintext, recipient.Open(sender.Seal(plaintext, aad), aad));
                                    Assert.Equal(plaintext, pskRecipient.Open(pskSender.Seal(plaintext, aad), aad));
                                }
                            }
                        }
                        finally
                        {
                            CryptographicOperations.ZeroMemory(privateSource);
                        }
                    }
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(privateKey);
                }
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(255)]
        public static void ImportDecapsulationKey_X25519RawKey(byte value)
        {
            HpkeSuite suite = new(HpkeKem.DHKEM_X25519_HKDF_SHA256, HpkeKdf.HKDF_SHA256, HpkeAead.AES_128_GCM);

            if (!Hpke.IsSupported(suite))
            {
                Assert.Throws<PlatformNotSupportedException>(() => Hpke.GenerateKey(suite));
                return;
            }

            byte[] source = new byte[suite.DecapsulationKeySizeInBytes];
            source.AsSpan().Fill(value);
            byte[] exported = new byte[source.Length];

            try
            {
                using (Hpke fromArray = Hpke.ImportDecapsulationKey(suite, source))
                using (Hpke fromSpan = Hpke.ImportDecapsulationKey(suite, source.AsSpan()))
                {
                    fromArray.ExportDecapsulationKey(exported);
                    Assert.Equal(source, exported);
                    fromSpan.ExportDecapsulationKey(exported);
                    Assert.Equal(source, exported);
                    fromArray.Seal("message"u8, out byte[] enc, out byte[] ciphertext);
                    AssertExtensions.SequenceEqual("message"u8, fromSpan.Open(enc, ciphertext));
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(source);
                CryptographicOperations.ZeroMemory(exported);
            }
        }

        // https://github.com/cfrg/draft-irtf-cfrg-hpke/blob/b1f7cb0cdeab6906c61b3d6574e8bdfdbe1cd3fb/test-vectors.json
        [Theory]
        [InlineData(
            HpkeKem.DHKEM_P256_HKDF_SHA256, HpkeKdf.HKDF_SHA256, HpkeAead.AES_128_GCM,
            "668b37171f1072f3cf12ea8a236a45df23fc13b82af3609ad1e354f6ef817550",
            "04a92719c6195d5085104f469a8b9814d5838ff72b60501e2c4466e5e67b325a" +
            "c98536d7b61a1af4b78e5b7f951c0900be863c403ce65c9bfcb9382657222d18c4",
            "5ad590bb8baa577f8619db35a36311226a896e7342a6d836d8b7bcd2f20b6c7f9076ac232e3ab2523f39513434",
            "fa6f037b47fc21826b610172ca9637e82d6e5801eb31cbd3748271affd4ecb06646e0329cbdf3c3cd655b28e82",
            "895cabfac50ce6c6eb02ffe6c048bf53b7f7be9a91fc559402cbc5b8dcaeb52b2ccc93e466c28fb55fed7a7fec",
            "5e9bc3d236e1911d95e65b576a8a86d478fb827e8bdfe77b741b289890490d4d",
            "6cff87658931bda83dc857e6353efe4987a201b849658d9b047aab4cf216e796",
            "d8f1ea7942adbba7412c6d431c62d01371ea476b823eb697e1f6e6cae1dab85a")]
        [InlineData(
            HpkeKem.DHKEM_P256_HKDF_SHA256, HpkeKdf.HKDF_SHA512, HpkeAead.AES_256_GCM,
            "a2f6e7c4d9e108e03be268a64fe73e11a320963c85375a30bfc9ec4a214c6a55",
            "0404dc39344526dbfa728afba96986d575811b5af199c11f821a0e603a4d191b2554" +
            "4a402f25364964b2c129cb417b3c1dab4dfc0854f3084e843f731654392726",
            "949f58e87c39b3f55390b6a970de27dfac44aadc2fbc9d623dcde1a08b628c83ad07dbbee6aede7fcfbf955670",
            "2b122485c81e76277b6fb7d96d85e1e2f0d41c8b6659dbbd2fad77d4a2318ceb88a350b02f7fdb242af6ee6222",
            "24612f7a27e9a8a0ddffcc18e769f5e03c9ebb658071b558058172d81336d151933f3d80846596d99f67994822",
            "c9d634be6e873105fc38fae1f86e195a0aa025c5cf1672acd2a358e7e2a84244",
            "d51a7dee4bb7da5e8d6271c5d6755967bbade71c4ceddab1acded3e6e5f642d0",
            "1a677fc144ec3f0df86cfebd6578a0a1a402beeb6f6c36235006369f1211edfa")]
        [InlineData(
            HpkeKem.DHKEM_P521_HKDF_SHA512, HpkeKdf.HKDF_SHA512, HpkeAead.AES_256_GCM,
            "2ad954bbe39b7122529f7dde780bff626cd97f850d0784a432784e69d86eccaa" +
            "de43b6c10a8ffdb94bf943c6da479db137914ec835a7e715e36e45e29b587bab3bf1",
            "040138b385ca16bb0d5fa0c0665fbbd7e69e3ee29f63991d3e9b5fa740aab8900aa" +
            "eed46ed73a49055758425a0ce36507c54b29cc5b85a5cee6bae0cf1c21f2731ece2" +
            "013dc3fb7c8d21654bb161b463962ca19e8c654ff24c94dd2898de12051f1ed0692" +
            "237fb02b2f8d1dc1c73e9b366b529eb436e98a996ee522aef863dd5739d2f29b0",
            "170f8beddfe949b75ef9c387e201baf4132fa7374593dfafa90768788b7b2b200aafcc6d80ea4c795a7c5b841a",
            "d9ee248e220ca24ac00bbbe7e221a832e4f7fa64c4fbab3945b6f3af0c5ecd5e16815b328be4954a05fd352256",
            "142cf1e02d1f58d9285f2af7dcfa44f7c3f2d15c73d460c48c6e0e506a3144bae35284e7e221105b61d24e1c7a",
            "05e2e5bd9f0c30832b80a279ff211cc65eceb0d97001524085d609ead60d0412",
            "fca69744bb537f5b7a1596dbf34eaa8d84bf2e3ee7f1a155d41bd3624aa92b63",
            "f389beaac6fcf6c0d9376e20f97e364f0609a88f1bc76d7328e9104df8477013")]
        [InlineData(
            HpkeKem.DHKEM_X25519_HKDF_SHA256, HpkeKdf.HKDF_SHA512, HpkeAead.ChaCha20Poly1305,
            "969bb169aa9c24a501ee9d962e96c310226d427fb6eb3fc579d9882dbc708315",
            "1d38fc578d4209ea0ef3ee5f1128ac4876a9549d74dc2d2f46e75942a6188244",
            "72da9627fd7eb3a8b7169c6d97419b80adefca751c6b52b39a2e084d35ce3eb4487aadaca5a9c590e0938c48b9",
            "bf59c5bfd8b31c3debc4a050388f7a047a24c18559902512d1146177a320616a6b527b194c92cf91d8832db1d5",
            "a80cdfe1a370a2db7e664c4acc69948d3a095be78bbfb0160f1aa0313cf0ed440154e913e5f9bc6756d7693982",
            "5b6120165c82456080db3c730b886b07129e0aec9b5f7beae9e5bbd103c67f2d",
            "30890b81a37b14b818c462ae5b680b4273cdc7a1ce5ca86d30d482fbe4323e7a",
            "b0b5c19ae0daf8d005593f5755d6e8cab29bd3c5c8245823586d009d15aa5237")]
        public static void Open_KnownAnswer(
            HpkeKem kem, HpkeKdf kdf, HpkeAead aead, string ikmHex, string encHex,
            string ciphertextHex, string secondCiphertextHex, string thirdCiphertextHex,
            string emptyContextExportHex, string zeroContextExportHex, string testContextExportHex)
        {
            HpkeSuite suite = new(kem, kdf, aead);

            if (!Hpke.IsSupported(suite))
            {
                Assert.Throws<PlatformNotSupportedException>(() => Hpke.GenerateKey(suite));
                return;
            }

            byte[] ikm = Convert.FromHexString(ikmHex);
            byte[] enc = Convert.FromHexString(encHex);
            byte[] ciphertext = Convert.FromHexString(ciphertextHex);
            byte[] plaintext = "Beauty is truth, truth beauty"u8.ToArray();
            byte[] associatedData = "Count-0"u8.ToArray();
            byte[] info = "Ode on a Grecian Urn"u8.ToArray();

            try
            {
                using (Hpke key = Hpke.DeriveKey(suite, ikm))
                {
                    Assert.Equal(plaintext, key.Open(enc, ciphertext, associatedData, info));
                    Assert.Equal(plaintext, key.Open(
                        new ReadOnlySpan<byte>(enc), ciphertext, new ReadOnlySpan<byte>(associatedData), info));

                    byte[] destination = new byte[plaintext.Length];
                    key.Open(enc, ciphertext, destination.AsSpan(), associatedData, info);
                    Assert.Equal(plaintext, destination);

                    using (HpkeRecipient recipient = key.CreateRecipient(enc, info))
                    {
                        AssertRecipientExports(recipient, emptyContextExportHex, zeroContextExportHex, testContextExportHex);
                        Assert.Equal(plaintext, recipient.Open(ciphertext, associatedData));
                        Assert.Equal(plaintext, recipient.Open(
                            new ReadOnlySpan<byte>(Convert.FromHexString(secondCiphertextHex)), "Count-1"u8));
                        recipient.Open(Convert.FromHexString(thirdCiphertextHex), destination.AsSpan(), "Count-2"u8);
                        Assert.Equal(plaintext, destination);
                        AssertRecipientExports(recipient, emptyContextExportHex, zeroContextExportHex, testContextExportHex);
                    }
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(ikm);
            }
        }

        public static IEnumerable<object[]> OpenSuiteData()
        {
            HpkeKem[] kems =
            [
                HpkeKem.DHKEM_P256_HKDF_SHA256,
                HpkeKem.DHKEM_P384_HKDF_SHA384,
                HpkeKem.DHKEM_P521_HKDF_SHA512,
                HpkeKem.DHKEM_X25519_HKDF_SHA256,
            ];

            foreach (HpkeKem kem in kems)
            {
                foreach (HpkeKdf kdf in Enum.GetValues<HpkeKdf>())
                {
                    foreach (HpkeAead aead in Enum.GetValues<HpkeAead>())
                    {
                        yield return new object[] { kem, kdf, aead };
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(OpenSuiteData))]
        public static void Open_Roundtrip(HpkeKem kem, HpkeKdf kdf, HpkeAead aead)
        {
            HpkeSuite suite = new(kem, kdf, aead);

            if (!Hpke.IsSupported(suite))
            {
                Assert.Throws<PlatformNotSupportedException>(() => Hpke.GenerateKey(suite));
                return;
            }

            using (Hpke key = Hpke.GenerateKey(suite))
            {
                foreach (int length in new[] { 0, 1, 257 })
                {
                    byte[] plaintext = new byte[length];
                    plaintext.AsSpan().Fill(0xA7);
                    byte[] associatedData = length == 0 ? [] : "associated data"u8.ToArray();
                    byte[] info = new byte[length == 0 ? 0 : 1024];
                    info.AsSpan().Fill(0x3C);

                    key.Seal(plaintext, out byte[] enc, out byte[] ciphertext, associatedData, info);
                    byte[] originalEnc = (byte[])enc.Clone();
                    byte[] originalCiphertext = (byte[])ciphertext.Clone();

                    Assert.Equal(plaintext, key.Open(
                        enc, ciphertext, length == 0 ? null : associatedData, length == 0 ? null : info));
                    Assert.Equal(plaintext, key.Open(
                        new ReadOnlySpan<byte>(enc), ciphertext, new ReadOnlySpan<byte>(associatedData), info));

                    byte[] destination = new byte[length + 2];
                    destination.AsSpan().Fill(0xA5);
                    key.Open(enc, ciphertext, destination.AsSpan(1, length), associatedData, info);
                    AssertExtensions.SequenceEqual(plaintext.AsSpan(), destination.AsSpan(1, length));
                    Assert.Equal(0xA5, destination[0]);
                    Assert.Equal(0xA5, destination[^1]);

                    Assert.Equal(plaintext, key.Open(enc, ciphertext, associatedData, info));
                    Assert.Equal(originalEnc, enc);
                    Assert.Equal(originalCiphertext, ciphertext);
                }
            }
        }

        [Theory]
        [MemberData(nameof(OpenSuiteData))]
        public static void Open_AuthenticationFailure(HpkeKem kem, HpkeKdf kdf, HpkeAead aead)
        {
            HpkeSuite suite = new(kem, kdf, aead);

            if (!Hpke.IsSupported(suite))
            {
                Assert.Throws<PlatformNotSupportedException>(() => Hpke.GenerateKey(suite));
                return;
            }

            using (Hpke key = Hpke.GenerateKey(suite))
            using (Hpke wrongKey = Hpke.GenerateKey(suite))
            {
                byte[] plaintext = "plaintext"u8.ToArray();
                byte[] associatedData = "associated data"u8.ToArray();
                byte[] info = "application context"u8.ToArray();
                key.Seal(plaintext, out byte[] enc, out byte[] ciphertext, associatedData, info);

                for (int tamper = 0; tamper < 6; tamper++)
                {
                    Hpke recipient = key;
                    byte[] modifiedEnc = (byte[])enc.Clone();
                    byte[] modifiedCiphertext = (byte[])ciphertext.Clone();
                    byte[] modifiedAssociatedData = (byte[])associatedData.Clone();
                    byte[] modifiedInfo = (byte[])info.Clone();

                    switch (tamper)
                    {
                        case 0:
                            modifiedCiphertext[0] ^= 1;
                            break;
                        case 1:
                            modifiedCiphertext[^1] ^= 1;
                            break;
                        case 2:
                            modifiedAssociatedData[0] ^= 1;
                            break;
                        case 3:
                            modifiedInfo[0] ^= 1;
                            break;
                        case 4:
                            modifiedEnc = wrongKey.ExportEncapsulationKey();
                            break;
                        case 5:
                            recipient = wrongKey;
                            break;
                    }

                    Assert.Throws<AuthenticationTagMismatchException>(() => recipient.Open(
                        modifiedEnc, modifiedCiphertext, modifiedAssociatedData, modifiedInfo));
                    Assert.Throws<AuthenticationTagMismatchException>(() => recipient.Open(
                        new ReadOnlySpan<byte>(modifiedEnc), modifiedCiphertext,
                        new ReadOnlySpan<byte>(modifiedAssociatedData), modifiedInfo));

                    byte[] destination = new byte[plaintext.Length + 2];
                    destination.AsSpan().Fill(0xA5);
                    Assert.Throws<AuthenticationTagMismatchException>(() => recipient.Open(
                        modifiedEnc, modifiedCiphertext, destination.AsSpan(1, plaintext.Length),
                        modifiedAssociatedData, modifiedInfo));
                    AssertExtensions.SequenceEqual(
                        new byte[plaintext.Length].AsSpan(), destination.AsSpan(1, plaintext.Length));
                    Assert.Equal(0xA5, destination[0]);
                    Assert.Equal(0xA5, destination[^1]);
                }

                Assert.Equal(plaintext, key.Open(enc, ciphertext, associatedData, info));
            }
        }

        [Theory]
        [InlineData(HpkeKem.DHKEM_P256_HKDF_SHA256, 0)]
        [InlineData(HpkeKem.DHKEM_P256_HKDF_SHA256, 4)]
        [InlineData(HpkeKem.DHKEM_P384_HKDF_SHA384, 0)]
        [InlineData(HpkeKem.DHKEM_P384_HKDF_SHA384, 4)]
        [InlineData(HpkeKem.DHKEM_P521_HKDF_SHA512, 0)]
        [InlineData(HpkeKem.DHKEM_P521_HKDF_SHA512, 4)]
        [InlineData(HpkeKem.DHKEM_X25519_HKDF_SHA256, 0)]
        [InlineData(HpkeKem.DHKEM_X25519_HKDF_SHA256, 1)]
        public static void Open_InvalidEncapsulatedSecret(HpkeKem kem, byte firstByte)
        {
            HpkeSuite suite = new(kem, HpkeKdf.HKDF_SHA256, HpkeAead.AES_128_GCM);

            if (!Hpke.IsSupported(suite))
            {
                Assert.Throws<PlatformNotSupportedException>(() => Hpke.GenerateKey(suite));
                return;
            }

            using (Hpke key = Hpke.GenerateKey(suite))
            {
                byte[] enc = new byte[suite.EncapsulatedSecretSizeInBytes];
                enc[0] = firstByte;
                byte[] ciphertext = new byte[suite.AeadTagSizeInBytes];

                Assert.ThrowsAny<CryptographicException>(() => key.Open(enc, ciphertext));
                Assert.ThrowsAny<CryptographicException>(() => key.Open(enc.AsSpan(), ciphertext));
                Assert.ThrowsAny<CryptographicException>(() => key.Open(enc, ciphertext, Span<byte>.Empty));
                Assert.ThrowsAny<CryptographicException>(() => key.CreateRecipient(enc));
                Assert.ThrowsAny<CryptographicException>(() => key.CreateRecipient(enc.AsSpan()));
            }
        }

        [Theory]
        [InlineData(HpkeKem.DHKEM_P256_HKDF_SHA256)]
        [InlineData(HpkeKem.DHKEM_P384_HKDF_SHA384)]
        [InlineData(HpkeKem.DHKEM_P521_HKDF_SHA512)]
        [InlineData(HpkeKem.DHKEM_X25519_HKDF_SHA256)]
        public static void Open_ArgumentValidation(HpkeKem kem)
        {
            HpkeSuite suite = new(kem, HpkeKdf.HKDF_SHA256, HpkeAead.AES_128_GCM);

            using (RecordingHpke key = new(suite))
            {
                byte[] enc = new byte[suite.EncapsulatedSecretSizeInBytes];
                byte[] ciphertext = new byte[suite.GetCiphertextLength(1)];
                byte[] plaintext = new byte[1];

                AssertExtensions.Throws<ArgumentNullException>("encapsulatedSecret", () => key.Open((byte[])null, ciphertext));
                AssertExtensions.Throws<ArgumentNullException>("ciphertext", () => key.Open(enc, (byte[])null));

                foreach (int length in new[] { 0, enc.Length - 1, enc.Length + 1 })
                {
                    byte[] invalidEnc = new byte[length];
                    AssertExtensions.Throws<ArgumentException>("encapsulatedSecret", () => key.Open(invalidEnc, ciphertext));
                    AssertExtensions.Throws<ArgumentException>(
                        "encapsulatedSecret", () => key.Open(invalidEnc.AsSpan(), ciphertext));
                    AssertExtensions.Throws<ArgumentException>(
                        "encapsulatedSecret", () => key.Open(invalidEnc, ciphertext, plaintext.AsSpan()));
                }

                foreach (int length in new[] { 0, suite.AeadTagSizeInBytes - 1 })
                {
                    byte[] invalidCiphertext = new byte[length];
                    AssertExtensions.Throws<ArgumentException>("ciphertext", () => key.Open(enc, invalidCiphertext));
                    AssertExtensions.Throws<ArgumentException>(
                        "ciphertext", () => key.Open(enc.AsSpan(), invalidCiphertext));
                    AssertExtensions.Throws<ArgumentException>(
                        "ciphertext", () => key.Open(enc, invalidCiphertext, plaintext.AsSpan()));
                }

                foreach (int length in new[] { 0, 2 })
                {
                    byte[] invalidPlaintext = new byte[length];
                    AssertExtensions.Throws<ArgumentException>(
                        "plaintext", () => key.Open(enc, ciphertext, invalidPlaintext.AsSpan()));
                }

                Assert.False(key.OpenCoreCalled);
                key.Dispose();
                Assert.Throws<ObjectDisposedException>(() => key.Open(enc, ciphertext));
                Assert.Throws<ObjectDisposedException>(() => key.Open(enc.AsSpan(), ciphertext));
                Assert.Throws<ObjectDisposedException>(() => key.Open(enc, ciphertext, plaintext.AsSpan()));
                Assert.False(key.OpenCoreCalled);
            }
        }

        [Theory]
        [InlineData(HpkeKdf.HKDF_SHA256, 65536, true)]
        [InlineData(HpkeKdf.HKDF_SHA384, 65536, true)]
        [InlineData(HpkeKdf.HKDF_SHA512, 65536, true)]
        [InlineData(HpkeKdf.SHAKE128, 65535, true)]
        [InlineData(HpkeKdf.SHAKE128, 65536, false)]
        [InlineData(HpkeKdf.SHAKE256, 65535, true)]
        [InlineData(HpkeKdf.SHAKE256, 65536, false)]
        public static void Open_InfoLength(HpkeKdf kdf, int infoLength, bool valid)
        {
            HpkeSuite suite = new(HpkeKem.DHKEM_P256_HKDF_SHA256, kdf, HpkeAead.AES_128_GCM);

            using (RecordingHpke key = new(suite))
            {
                byte[] enc = new byte[suite.EncapsulatedSecretSizeInBytes];
                byte[] ciphertext = new byte[suite.AeadTagSizeInBytes];
                byte[] info = new byte[infoLength];

                if (valid)
                {
                    Assert.Empty(key.Open(enc, ciphertext, info: info));
                    Assert.Empty(key.Open(enc.AsSpan(), ciphertext, info: info));
                    key.Open(enc, ciphertext, Span<byte>.Empty, info: info);
                }
                else
                {
                    AssertExtensions.Throws<ArgumentException>("info", () => key.Open(enc, ciphertext, info: info));
                    AssertExtensions.Throws<ArgumentException>(
                        "info", () => key.Open(enc.AsSpan(), ciphertext, info: info));
                    AssertExtensions.Throws<ArgumentException>(
                        "info", () => key.Open(enc, ciphertext, Span<byte>.Empty, info: info));
                }

                Assert.Equal(valid, key.OpenCoreCalled);
            }
        }

        [Theory]
        [InlineData(HpkeKem.DHKEM_P256_HKDF_SHA256)]
        [InlineData(HpkeKem.DHKEM_P384_HKDF_SHA384)]
        [InlineData(HpkeKem.DHKEM_P521_HKDF_SHA512)]
        [InlineData(HpkeKem.DHKEM_X25519_HKDF_SHA256)]
        [InlineData(HpkeKem.MLKEM_512)]
        [InlineData(HpkeKem.MLKEM_768)]
        [InlineData(HpkeKem.MLKEM_1024)]
        [InlineData(HpkeKem.MLKEM768_P256)]
        [InlineData(HpkeKem.MLKEM1024_P384)]
        public static void CreateSender_Overloads(HpkeKem kem)
        {
            HpkeSuite suite = new(kem, HpkeKdf.HKDF_SHA256, HpkeAead.AES_128_GCM);

            using (RecordingHpke key = new(suite))
            {
                byte[] info = [1, 2, 3];
                byte[] expected = new byte[suite.EncapsulatedSecretSizeInBytes];
                expected.AsSpan().Fill(0xD7);

                using (HpkeSender sender = key.CreateSender(out byte[] enc, info))
                {
                    Assert.IsType<RecordingHpkeSender>(sender);
                    Assert.Same(suite, sender.Suite);
                    Assert.Equal(expected, enc);
                    Assert.Equal(info, key.LastSenderInfo);
                }

                byte[] destination = new byte[expected.Length + 2];
                destination.AsSpan().Fill(0xA5);

                using (HpkeSender sender = key.CreateSender(destination.AsSpan(1, expected.Length), info))
                {
                    Assert.Same(suite, sender.Suite);
                    AssertExtensions.SequenceEqual(expected.AsSpan(), destination.AsSpan(1, expected.Length));
                    Assert.Equal(0xA5, destination[0]);
                    Assert.Equal(0xA5, destination[^1]);
                    Assert.Equal(info, key.LastSenderInfo);
                }

                using (HpkeSender sender = key.CreateSender(out _))
                {
                    Assert.Empty(key.LastSenderInfo);
                }

                using (HpkeSender sender = key.CreateSender(destination.AsSpan(1, expected.Length)))
                {
                    Assert.Empty(key.LastSenderInfo);
                }

                Assert.Equal(4, key.CreateSenderCalls);
            }
        }

        [Fact]
        public static void CreateSender_Validation()
        {
            HpkeSuite suite = new(HpkeKem.DHKEM_P256_HKDF_SHA256, HpkeKdf.HKDF_SHA256, HpkeAead.AES_128_GCM);

            using (RecordingHpke key = new(suite))
            {
                foreach (int length in new[] { 0, suite.EncapsulatedSecretSizeInBytes - 1, suite.EncapsulatedSecretSizeInBytes + 1 })
                {
                    byte[] destination = new byte[length];
                    destination.AsSpan().Fill(0xA5);
                    byte[] original = (byte[])destination.Clone();
                    AssertExtensions.Throws<ArgumentException>(
                        "encapsulatedSecret", () => key.CreateSender(destination));
                    Assert.Equal(original, destination);
                }

                key.Dispose();
                byte[] enc = new byte[suite.EncapsulatedSecretSizeInBytes];
                Assert.Throws<ObjectDisposedException>(() => key.CreateSender(out _));
                Assert.Throws<ObjectDisposedException>(() => key.CreateSender(enc));
                Assert.Equal(0, key.CreateSenderCalls);
            }
        }

        [Theory]
        [InlineData(HpkeKdf.HKDF_SHA256, 65536, true)]
        [InlineData(HpkeKdf.HKDF_SHA384, 65536, true)]
        [InlineData(HpkeKdf.HKDF_SHA512, 65536, true)]
        [InlineData(HpkeKdf.SHAKE128, 65535, true)]
        [InlineData(HpkeKdf.SHAKE128, 65536, false)]
        [InlineData(HpkeKdf.SHAKE256, 65535, true)]
        [InlineData(HpkeKdf.SHAKE256, 65536, false)]
        public static void CreateSender_InfoLength(HpkeKdf kdf, int infoLength, bool valid)
        {
            HpkeSuite suite = new(HpkeKem.DHKEM_P256_HKDF_SHA256, kdf, HpkeAead.AES_128_GCM);

            using (RecordingHpke key = new(suite))
            {
                byte[] info = new byte[infoLength];
                info.AsSpan().Fill(0x39);
                byte[] enc = new byte[suite.EncapsulatedSecretSizeInBytes];

                if (valid)
                {
                    using (HpkeSender sender = key.CreateSender(out _, info))
                    {
                        Assert.Equal(info, key.LastSenderInfo);
                    }

                    using (HpkeSender sender = key.CreateSender(enc, info))
                    {
                        Assert.Equal(info, key.LastSenderInfo);
                    }
                }
                else
                {
                    AssertExtensions.Throws<ArgumentException>("info", () => key.CreateSender(out _, info));
                    AssertExtensions.Throws<ArgumentException>("info", () => key.CreateSender(enc, info));
                }

                Assert.Equal(valid ? 2 : 0, key.CreateSenderCalls);
            }
        }

        [Fact]
        public static void CreateSender_CoreFailureDoesNotPublishOutput()
        {
            HpkeSuite suite = new(HpkeKem.DHKEM_P256_HKDF_SHA256, HpkeKdf.HKDF_SHA256, HpkeAead.AES_128_GCM);

            using (RecordingHpke key = new(suite) { ThrowOnCreateSender = true })
            {
                byte[] original = [0xA5];
                byte[] enc = original;
                Assert.Throws<CryptographicException>(() => key.CreateSender(out enc));
                Assert.Same(original, enc);
                Assert.Throws<CryptographicException>(() => key.CreateSender(new byte[suite.EncapsulatedSecretSizeInBytes]));
                Assert.Equal(2, key.CreateSenderCalls);
            }
        }

        [Theory]
        [MemberData(nameof(OpenSuiteData))]
        public static void CreateContexts_SealAndOpen(HpkeKem kem, HpkeKdf kdf, HpkeAead aead)
        {
            HpkeSuite suite = new(kem, kdf, aead);

            if (!Hpke.IsSupported(suite))
            {
                Assert.Throws<PlatformNotSupportedException>(() => Hpke.GenerateKey(suite));
                return;
            }

            using (Hpke key = Hpke.GenerateKey(suite))
            {
                byte[] info = new byte[1024];
                info.AsSpan().Fill(0x3C);
                byte[] associatedData = "associated data"u8.ToArray();

                foreach (int length in new[] { 0, 1, 257 })
                {
                    byte[] plaintext = new byte[length];
                    plaintext.AsSpan().Fill(0xA7);
                    int ciphertextLength = suite.GetCiphertextLength(length);

                    using (HpkeSender sender = key.CreateSender(out byte[] enc, info))
                    using (HpkeRecipient recipient = key.CreateRecipient(enc, info))
                    {
                        Assert.Same(suite, sender.Suite);
                        Assert.Same(suite, recipient.Suite);
                        Assert.Equal(suite.EncapsulatedSecretSizeInBytes, enc.Length);
                        AssertExtensions.Throws<ArgumentException>(
                            "ciphertext", () => sender.Seal(plaintext, new byte[ciphertextLength - 1].AsSpan(), associatedData));

                        byte[] ciphertext = sender.Seal(plaintext, associatedData);
                        Assert.Equal(plaintext, key.Open(enc, ciphertext, associatedData, info));
                        Assert.Equal(plaintext, recipient.Open(ciphertext, associatedData));

                        byte[] nextCiphertext = sender.Seal(
                            new ReadOnlySpan<byte>(plaintext), new ReadOnlySpan<byte>(associatedData));
                        Assert.Equal(ciphertextLength, nextCiphertext.Length);
                        Assert.NotEqual(ciphertext, nextCiphertext);
                        Assert.Throws<AuthenticationTagMismatchException>(
                            () => key.Open(enc, nextCiphertext, associatedData, info));
                        Assert.Equal(plaintext, recipient.Open(
                            new ReadOnlySpan<byte>(nextCiphertext), new ReadOnlySpan<byte>(associatedData)));
                    }

                    byte[] encBuffer = new byte[suite.EncapsulatedSecretSizeInBytes + 2];
                    encBuffer.AsSpan().Fill(0xA5);

                    using (HpkeSender sender = key.CreateSender(
                        encBuffer.AsSpan(1, suite.EncapsulatedSecretSizeInBytes), info))
                    {
                        Assert.Same(suite, sender.Suite);
                        Assert.Equal(0xA5, encBuffer[0]);
                        Assert.Equal(0xA5, encBuffer[^1]);
                        byte[] enc = encBuffer.AsSpan(1, suite.EncapsulatedSecretSizeInBytes).ToArray();
                        byte[] ciphertextBuffer = new byte[ciphertextLength + 2];
                        ciphertextBuffer.AsSpan().Fill(0xA5);
                        sender.Seal(plaintext, ciphertextBuffer.AsSpan(1, ciphertextLength), associatedData);
                        Assert.Equal(0xA5, ciphertextBuffer[0]);
                        Assert.Equal(0xA5, ciphertextBuffer[^1]);
                        Assert.Equal(plaintext, key.Open(
                            enc, ciphertextBuffer.AsSpan(1, ciphertextLength), new ReadOnlySpan<byte>(associatedData), info));

                        using (HpkeRecipient recipient = key.CreateRecipient(enc.AsSpan(), info))
                        {
                            Assert.Same(suite, recipient.Suite);
                            byte[] destination = new byte[length + 2];
                            destination.AsSpan().Fill(0xA5);
                            recipient.Open(ciphertextBuffer.AsSpan(1, ciphertextLength), destination.AsSpan(1, length), associatedData);
                            AssertExtensions.SequenceEqual(plaintext.AsSpan(), destination.AsSpan(1, length));
                            Assert.Equal(0xA5, destination[0]);
                            Assert.Equal(0xA5, destination[^1]);
                        }
                    }
                }
            }
        }

        [Theory]
        [InlineData(HpkeKem.DHKEM_P256_HKDF_SHA256)]
        [InlineData(HpkeKem.DHKEM_P384_HKDF_SHA384)]
        [InlineData(HpkeKem.DHKEM_P521_HKDF_SHA512)]
        [InlineData(HpkeKem.DHKEM_X25519_HKDF_SHA256)]
        public static void CreateContexts_IndependentLifetime(HpkeKem kem)
        {
            HpkeSuite suite = new(kem, HpkeKdf.HKDF_SHA256, HpkeAead.AES_128_GCM);

            if (!Hpke.IsSupported(suite))
            {
                Assert.Throws<PlatformNotSupportedException>(() => Hpke.GenerateKey(suite));
                return;
            }

            byte[] ikm = new byte[suite.DecapsulationKeySizeInBytes];

            try
            {
                using (Hpke key = Hpke.DeriveKey(suite, ikm))
                using (Hpke peer = Hpke.DeriveKey(suite, ikm))
                using (HpkeSender first = key.CreateSender(out byte[] firstEnc))
                using (HpkeSender second = key.CreateSender(out byte[] secondEnc))
                using (HpkeRecipient firstRecipient = key.CreateRecipient(firstEnc))
                using (HpkeRecipient secondRecipient = key.CreateRecipient(secondEnc.AsSpan()))
                {
                    key.Dispose();
                    Assert.NotEqual(firstEnc, secondEnc);
                    byte[] plaintext = "message"u8.ToArray();
                    byte[] firstCiphertext = first.Seal(plaintext);
                    Assert.Equal(plaintext, peer.Open(firstEnc, firstCiphertext));
                    Assert.Equal(plaintext, firstRecipient.Open(firstCiphertext));

                    first.Dispose();
                    firstRecipient.Dispose();
                    Assert.Throws<ObjectDisposedException>(() => first.Seal(plaintext));
                    Assert.Throws<ObjectDisposedException>(() => firstRecipient.Open(firstCiphertext));
                    byte[] secondCiphertext = second.Seal(plaintext);
                    Assert.Equal(plaintext, peer.Open(secondEnc, secondCiphertext));
                    Assert.Equal(plaintext, secondRecipient.Open(secondCiphertext));
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(ikm);
            }
        }

        [Theory]
        [InlineData(HpkeKem.DHKEM_P256_HKDF_SHA256)]
        [InlineData(HpkeKem.DHKEM_P384_HKDF_SHA384)]
        [InlineData(HpkeKem.DHKEM_P521_HKDF_SHA512)]
        [InlineData(HpkeKem.DHKEM_X25519_HKDF_SHA256)]
        [InlineData(HpkeKem.MLKEM_512)]
        [InlineData(HpkeKem.MLKEM_768)]
        [InlineData(HpkeKem.MLKEM_1024)]
        [InlineData(HpkeKem.MLKEM768_P256)]
        [InlineData(HpkeKem.MLKEM1024_P384)]
        public static void CreateRecipient_Overloads(HpkeKem kem)
        {
            HpkeSuite suite = new(kem, HpkeKdf.HKDF_SHA256, HpkeAead.AES_128_GCM);

            using (RecordingHpke key = new(suite))
            {
                byte[] enc = new byte[suite.EncapsulatedSecretSizeInBytes];
                enc.AsSpan().Fill(0xD7);
                byte[] info = [1, 2, 3];

                using (HpkeRecipient recipient = key.CreateRecipient(enc, info))
                {
                    Assert.IsType<RecordingHpkeRecipient>(recipient);
                    Assert.Same(suite, recipient.Suite);
                    Assert.Equal(enc, key.LastRecipientEncapsulatedSecret);
                    Assert.Equal(info, key.LastRecipientInfo);
                }

                using (HpkeRecipient recipient = key.CreateRecipient(enc.AsSpan(), info))
                {
                    Assert.Same(suite, recipient.Suite);
                    Assert.Equal(enc, key.LastRecipientEncapsulatedSecret);
                    Assert.Equal(info, key.LastRecipientInfo);
                }

                using (HpkeRecipient recipient = key.CreateRecipient(enc, info: null))
                {
                    Assert.Empty(key.LastRecipientInfo);
                }

                using (HpkeRecipient recipient = key.CreateRecipient(enc.AsSpan()))
                {
                    Assert.Empty(key.LastRecipientInfo);
                }

                Assert.Equal(4, key.CreateRecipientCalls);
            }
        }

        [Fact]
        public static void CreateRecipient_Validation()
        {
            HpkeSuite suite = new(HpkeKem.DHKEM_P256_HKDF_SHA256, HpkeKdf.HKDF_SHA256, HpkeAead.AES_128_GCM);

            using (RecordingHpke key = new(suite))
            {
                AssertExtensions.Throws<ArgumentNullException>(
                    "encapsulatedSecret", () => key.CreateRecipient((byte[])null));

                foreach (int length in new[] { 0, suite.EncapsulatedSecretSizeInBytes - 1, suite.EncapsulatedSecretSizeInBytes + 1 })
                {
                    byte[] enc = new byte[length];
                    AssertExtensions.Throws<ArgumentException>("encapsulatedSecret", () => key.CreateRecipient(enc));
                    AssertExtensions.Throws<ArgumentException>("encapsulatedSecret", () => key.CreateRecipient(enc.AsSpan()));
                }

                key.Dispose();
                byte[] validLengthEnc = new byte[suite.EncapsulatedSecretSizeInBytes];
                Assert.Throws<ObjectDisposedException>(() => key.CreateRecipient(validLengthEnc));
                Assert.Throws<ObjectDisposedException>(() => key.CreateRecipient(validLengthEnc.AsSpan()));
                Assert.Equal(0, key.CreateRecipientCalls);
            }
        }

        [Theory]
        [InlineData(HpkeKdf.HKDF_SHA256, 65536, true)]
        [InlineData(HpkeKdf.HKDF_SHA384, 65536, true)]
        [InlineData(HpkeKdf.HKDF_SHA512, 65536, true)]
        [InlineData(HpkeKdf.SHAKE128, 65535, true)]
        [InlineData(HpkeKdf.SHAKE128, 65536, false)]
        [InlineData(HpkeKdf.SHAKE256, 65535, true)]
        [InlineData(HpkeKdf.SHAKE256, 65536, false)]
        public static void CreateRecipient_InfoLength(HpkeKdf kdf, int infoLength, bool valid)
        {
            HpkeSuite suite = new(HpkeKem.DHKEM_P256_HKDF_SHA256, kdf, HpkeAead.AES_128_GCM);

            using (RecordingHpke key = new(suite))
            {
                byte[] enc = new byte[suite.EncapsulatedSecretSizeInBytes];
                byte[] info = new byte[infoLength];
                info.AsSpan().Fill(0x39);

                if (valid)
                {
                    using (HpkeRecipient recipient = key.CreateRecipient(enc, info))
                    {
                        Assert.Equal(info, key.LastRecipientInfo);
                    }

                    using (HpkeRecipient recipient = key.CreateRecipient(enc.AsSpan(), info))
                    {
                        Assert.Equal(info, key.LastRecipientInfo);
                    }
                }
                else
                {
                    AssertExtensions.Throws<ArgumentException>("info", () => key.CreateRecipient(enc, info));
                    AssertExtensions.Throws<ArgumentException>("info", () => key.CreateRecipient(enc.AsSpan(), info));
                }

                Assert.Equal(valid ? 2 : 0, key.CreateRecipientCalls);
            }
        }

        [Theory]
        [MemberData(nameof(OpenSuiteData))]
        public static void Recipient_AuthenticationFailureAndOrdering(HpkeKem kem, HpkeKdf kdf, HpkeAead aead)
        {
            HpkeSuite suite = new(kem, kdf, aead);

            if (!Hpke.IsSupported(suite))
            {
                Assert.Throws<PlatformNotSupportedException>(() => Hpke.GenerateKey(suite));
                return;
            }

            byte[] info = "application context"u8.ToArray();
            byte[] associatedData = "associated data"u8.ToArray();
            byte[] first = "first"u8.ToArray();
            byte[] second = "second"u8.ToArray();
            byte[] third = "third"u8.ToArray();

            using (Hpke key = Hpke.GenerateKey(suite))
            using (Hpke wrongKey = Hpke.GenerateKey(suite))
            using (HpkeSender sender = key.CreateSender(out byte[] enc, info))
            using (HpkeRecipient recipient = key.CreateRecipient(enc, info))
            {
                byte[] firstCiphertext = sender.Seal(first, associatedData);
                byte[] secondCiphertext = sender.Seal(second, associatedData);
                byte[] thirdCiphertext = sender.Seal(third, associatedData);

                using (HpkeRecipient wrongKeyRecipient = wrongKey.CreateRecipient(enc, info))
                using (HpkeRecipient wrongInfoRecipient = key.CreateRecipient(enc, "different context"u8.ToArray()))
                {
                    Assert.Throws<AuthenticationTagMismatchException>(() => wrongKeyRecipient.Open(firstCiphertext, associatedData));
                    Assert.Throws<AuthenticationTagMismatchException>(() => wrongInfoRecipient.Open(firstCiphertext, associatedData));
                }

                AssertExtensions.Throws<ArgumentException>(
                    "plaintext", () => recipient.Open(firstCiphertext, new byte[first.Length - 1].AsSpan(), associatedData));

                for (int tamper = 0; tamper < 3; tamper++)
                {
                    byte[] modifiedCiphertext = (byte[])firstCiphertext.Clone();
                    byte[] modifiedAssociatedData = (byte[])associatedData.Clone();

                    switch (tamper)
                    {
                        case 0:
                            modifiedCiphertext[0] ^= 1;
                            break;
                        case 1:
                            modifiedCiphertext[^1] ^= 1;
                            break;
                        case 2:
                            modifiedAssociatedData[0] ^= 1;
                            break;
                    }

                    Assert.Throws<AuthenticationTagMismatchException>(() => recipient.Open(modifiedCiphertext, modifiedAssociatedData));
                    Assert.Throws<AuthenticationTagMismatchException>(() => recipient.Open(
                        new ReadOnlySpan<byte>(modifiedCiphertext), new ReadOnlySpan<byte>(modifiedAssociatedData)));

                    byte[] destination = new byte[first.Length + 2];
                    destination.AsSpan().Fill(0xA5);
                    Assert.Throws<AuthenticationTagMismatchException>(() => recipient.Open(
                        modifiedCiphertext, destination.AsSpan(1, first.Length), modifiedAssociatedData));
                    AssertExtensions.SequenceEqual(new byte[first.Length].AsSpan(), destination.AsSpan(1, first.Length));
                    Assert.Equal(0xA5, destination[0]);
                    Assert.Equal(0xA5, destination[^1]);
                }

                Assert.Equal(first, recipient.Open(firstCiphertext, associatedData));
                Assert.Throws<AuthenticationTagMismatchException>(() => recipient.Open(firstCiphertext, associatedData));
                Assert.Throws<AuthenticationTagMismatchException>(() => recipient.Open(thirdCiphertext, associatedData));
                Assert.Equal(second, recipient.Open(new ReadOnlySpan<byte>(secondCiphertext), new ReadOnlySpan<byte>(associatedData)));
                byte[] thirdDestination = new byte[third.Length];
                recipient.Open(thirdCiphertext, thirdDestination.AsSpan(), associatedData);
                Assert.Equal(third, thirdDestination);
            }
        }

        // https://github.com/cfrg/draft-irtf-cfrg-hpke/blob/b1f7cb0cdeab6906c61b3d6574e8bdfdbe1cd3fb/test-vectors.json
        [Theory]
        [InlineData(
            HpkeKem.DHKEM_P256_HKDF_SHA256, HpkeKdf.HKDF_SHA256, HpkeAead.AES_128_GCM,
            "d42ef874c1913d9568c9405407c805baddaffd0898a00f1e84e154fa787b2429",
            "04305d35563527bce037773d79a13deabed0e8e7cde61eecee403496959e89e4d0" +
            "ca701726696d1485137ccb5341b3c1c7aaee90a4a02449725e744b1193b53b5f",
            "90c4deb5b75318530194e4bb62f890b019b1397bbf9d0d6eb918890e1fb2be1ac2603193b60a49c2126b75d0eb",
            "9e223384a3620f4a75b5a52f546b7262d8826dea18db5a365feb8b997180b22d72dc1287f7089a1073a7102c27",
            "a115a59bf4dd8dc49332d6a0093af8efca1bcbfd3627d850173f5c4a55d0c185",
            "4517eaede0669b16aac7c92d5762dd459c301fa10e02237cd5aeb9be969430c4",
            "164e02144d44b607a7722e58b0f4156e67c0c2874d74cf71da6ca48a4cbdc5e0")]
        [InlineData(
            HpkeKem.DHKEM_P256_HKDF_SHA256, HpkeKdf.HKDF_SHA512, HpkeAead.AES_256_GCM,
            "509212d2ac43d399abd9050ae3c41c030b82623da0494c0d9f8f26ac56b7e188",
            "048739ebbaea3156cbd5e39b4ef41ee7e3b52c8cb4958d087112b17b778897152c" +
            "7e99307095b1cee54b807077f6f5092970a27fbb57ce2835263132c75e52e7e0",
            "351d83aa6f2ba77c4b9b89aa22fcb18aff3f792bb04e999de9f76f03f99e92c8d9203605cc0dcbb5eb08a9db6b",
            "e9deb7896d9414ea4d3e01763e425b5bce3b43874d9121f33441f601a8f7faafb0687512f8782f23ea7aa25b4d",
            "850caf7336dd83d41fdee7cb133c7c12b62bf7111d3c5d3d60b20128484adada",
            "50121f10b5674e3dc46eed39616ff502ef0d6d7f356783808887a867f6a717c6",
            "32b9b0b8315cfc2415852b21e9353e79c233233f400def9623404e21657bdab5")]
        [InlineData(
            HpkeKem.DHKEM_P521_HKDF_SHA512, HpkeKdf.HKDF_SHA512, HpkeAead.AES_256_GCM,
            "a2a2458705e278e574f835effecd18232f8a4c459e7550a09d44348ae5d3b1ea" +
            "9d95c51995e657ad6f7cae659f5e186126a471c017f8f5e41da9eba74d4e0473e179",
            "040085eff0835cc84351f32471d32aa453cdc1f6418eaaecf1c2824210eb1d48d076" +
            "8b368110fab21407c324b8bb4bec63f042cfa4d0868d19b760eb4beba1bff793b3" +
            "0036d2c614d55730bd2a40c718f9466faf4d5f8170d22b6df98dfe0c067d02b349" +
            "ae4a142e0c03418f0a1479ff78a3db07ae2c2e89e5840f712c174ba2118e90fdcb",
            "de69e9d943a5d0b70be3359a19f317bd9aca4a2ebb4332a39bcdfc97d5fe62f3a77702f4822c3be531aa7843a1",
            "77a16162831f90de350fea9152cfc685ecfa10acb4f7994f41aed43fa5431f2382d078ec88baec53943984553e",
            "62691f0f971e34de38370bff24deb5a7d40ab628093d304be60946afcdb3a936",
            "76083c6d1b6809da088584674327b39488eaf665f0731151128452e04ce81bff",
            "0c7cfc0976e25ae7680cf909ae2de1859cd9b679610a14bec40d69b91785b2f6")]
        [InlineData(
            HpkeKem.DHKEM_X25519_HKDF_SHA256, HpkeKdf.HKDF_SHA512, HpkeAead.ChaCha20Poly1305,
            "92c0e581f1b0ad231dd7346d69071afa23eb4dacdf0b868b644a20bd5121dc07",
            "bc441a64a700843a8efd5cd574c20e9909c3a2ff7d35e260f9328cbb8e555d56",
            "65a46e483d921343f20cba85da69976b2e0e52f450db7919f7796604977d6708d884a40d5e4fd5b820211264aa",
            "02019423af9256981bc0a8a7675494efee2244faa2be5b572d9470e451ea3f831e2c08cd47bfc78d6d1f11cfb1",
            "722aa34bd26f69aa1763f46d7eae6cf461ce74b6952483f3ea7d490c88882982",
            "ea0c03bea28f6a22f5c93c52a999fdbd386572920a2838304e987d6f930d5fa4",
            "3a3980d8a63287c12db540669ded019a0643e236e25896f2f3197edda044b3ce")]
        public static void Psk_KnownAnswer(
            HpkeKem kem, HpkeKdf kdf, HpkeAead aead, string ikmHex, string encHex,
            string firstCiphertextHex, string secondCiphertextHex,
            string emptyContextExportHex, string zeroContextExportHex, string testContextExportHex)
        {
            HpkeSuite suite = new(kem, kdf, aead);

            if (!Hpke.IsSupported(suite))
            {
                Assert.Throws<PlatformNotSupportedException>(() => Hpke.GenerateKey(suite));
                return;
            }

            byte[] ikm = Convert.FromHexString(ikmHex);
            byte[] psk = Convert.FromHexString("0247fd33b913760fa1fa51e1892d9f307fbe65eb171e8132c2af18555a738b82");
            byte[] pskId = "Ennyn Durin aran Moria"u8.ToArray();
            byte[] info = "Ode on a Grecian Urn"u8.ToArray();
            byte[] enc = Convert.FromHexString(encHex);
            byte[] plaintext = "Beauty is truth, truth beauty"u8.ToArray();

            try
            {
                using (Hpke key = Hpke.DeriveKey(suite, ikm))
                using (HpkeRecipient fromArray = key.CreatePskRecipient(enc, psk, pskId, info))
                using (HpkeRecipient fromSpan = key.CreatePskRecipient(enc.AsSpan(), psk, pskId, info))
                {
                    AssertRecipientExports(fromArray, emptyContextExportHex, zeroContextExportHex, testContextExportHex);
                    byte[] firstCiphertext = Convert.FromHexString(firstCiphertextHex);
                    byte[] secondCiphertext = Convert.FromHexString(secondCiphertextHex);
                    Assert.Equal(plaintext, fromArray.Open(firstCiphertext, "Count-0"u8.ToArray()));
                    Assert.Equal(plaintext, fromArray.Open(new ReadOnlySpan<byte>(secondCiphertext), "Count-1"u8));
                    byte[] destination = new byte[plaintext.Length];
                    fromSpan.Open(firstCiphertext, destination.AsSpan(), "Count-0"u8);
                    Assert.Equal(plaintext, destination);
                    Assert.Equal(plaintext, fromSpan.Open(secondCiphertext, "Count-1"u8.ToArray()));
                    AssertRecipientExports(fromArray, emptyContextExportHex, zeroContextExportHex, testContextExportHex);
                    AssertRecipientExports(fromSpan, emptyContextExportHex, zeroContextExportHex, testContextExportHex);
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(ikm);
                CryptographicOperations.ZeroMemory(psk);
            }
        }

        [Theory]
        [MemberData(nameof(OpenSuiteData))]
        public static void Psk_RoundtripAndLifetime(HpkeKem kem, HpkeKdf kdf, HpkeAead aead)
        {
            HpkeSuite suite = new(kem, kdf, aead);

            if (!Hpke.IsSupported(suite))
            {
                Assert.Throws<PlatformNotSupportedException>(() => Hpke.GenerateKey(suite));
                return;
            }

            for (int overload = 0; overload < 3; overload++)
            {
                byte[] psk = new byte[overload == 0 ? 32 : overload == 1 ? 33 : 64];
                psk.AsSpan().Fill(0x3C);
                byte[] originalPsk = (byte[])psk.Clone();
                byte[] pskId = "psk identifier"u8.ToArray();
                byte[] originalPskId = (byte[])pskId.Clone();
                byte[] info = overload == 0 ? null : new byte[1024];

                using (Hpke key = Hpke.GenerateKey(suite))
                {
                    HpkeSender sender;
                    byte[] enc;

                    if (overload == 0)
                    {
                        sender = key.CreatePskSender(psk, pskId, out enc, info);
                    }
                    else if (overload == 1)
                    {
                        sender = key.CreatePskSender(psk.AsSpan(), pskId, out enc, info);
                    }
                    else
                    {
                        byte[] buffer = new byte[suite.EncapsulatedSecretSizeInBytes + 2];
                        buffer.AsSpan().Fill(0xA5);
                        sender = key.CreatePskSender(psk, pskId, buffer.AsSpan(1, buffer.Length - 2), info);
                        Assert.Equal(0xA5, buffer[0]);
                        Assert.Equal(0xA5, buffer[^1]);
                        enc = buffer.AsSpan(1, buffer.Length - 2).ToArray();
                    }

                    using (sender)
                    using (HpkeRecipient recipient = overload == 1
                        ? key.CreatePskRecipient(enc.AsSpan(), psk, pskId, info)
                        : key.CreatePskRecipient(enc, psk, pskId, info))
                    {
                        Assert.Same(suite, sender.Suite);
                        Assert.Same(suite, recipient.Suite);
                        Assert.Equal(originalPsk, psk);
                        Assert.Equal(originalPskId, pskId);
                        key.Dispose();
                        psk.AsSpan().Clear();
                        pskId.AsSpan().Clear();
                        enc.AsSpan().Clear();
                        info?.AsSpan().Clear();

                        foreach (int length in new[] { 0, 1, 257 })
                        {
                            byte[] plaintext = new byte[length];
                            plaintext.AsSpan().Fill(0xA7);
                            byte[] aad = length == 0 ? [] : "associated data"u8.ToArray();
                            byte[] ciphertext = sender.Seal(plaintext, aad);
                            byte[] destination = new byte[length + 2];
                            destination.AsSpan().Fill(0xA5);
                            recipient.Open(ciphertext, destination.AsSpan(1, length), aad);
                            AssertExtensions.SequenceEqual(plaintext.AsSpan(), destination.AsSpan(1, length));
                            Assert.Equal(0xA5, destination[0]);
                            Assert.Equal(0xA5, destination[^1]);
                        }
                    }
                }

                CryptographicOperations.ZeroMemory(originalPsk);
            }
        }

        [Theory]
        [MemberData(nameof(OpenSuiteData))]
        public static void Psk_AuthenticationAndModeSeparation(HpkeKem kem, HpkeKdf kdf, HpkeAead aead)
        {
            HpkeSuite suite = new(kem, kdf, aead);

            if (!Hpke.IsSupported(suite))
            {
                Assert.Throws<PlatformNotSupportedException>(() => Hpke.GenerateKey(suite));
                return;
            }

            byte[] psk = new byte[32];
            byte[] differentPsk = new byte[32];
            differentPsk[0] = 1;
            byte[] pskId = "identifier"u8.ToArray();
            byte[] info = "info"u8.ToArray();
            byte[] plaintext = "plaintext"u8.ToArray();
            byte[] aad = "associated data"u8.ToArray();

            using (Hpke key = Hpke.GenerateKey(suite))
            using (Hpke wrongKey = Hpke.GenerateKey(suite))
            using (HpkeSender sender = key.CreatePskSender(psk, pskId, out byte[] enc, info))
            using (HpkeRecipient recipient = key.CreatePskRecipient(enc, psk, pskId, info))
            using (HpkeRecipient badPsk = key.CreatePskRecipient(enc, differentPsk, pskId, info))
            using (HpkeRecipient badId = key.CreatePskRecipient(enc, psk, "different identifier"u8.ToArray(), info))
            using (HpkeRecipient badInfo = key.CreatePskRecipient(enc, psk, pskId, "different info"u8.ToArray()))
            using (HpkeRecipient badKey = wrongKey.CreatePskRecipient(enc, psk, pskId, info))
            using (HpkeRecipient baseRecipient = key.CreateRecipient(enc, info))
            using (HpkeSender baseSender = key.CreateSender(out byte[] baseEnc, info))
            using (HpkeRecipient pskRecipientForBase = key.CreatePskRecipient(baseEnc, psk, pskId, info))
            {
                byte[] ciphertext = sender.Seal(plaintext, aad);
                foreach (HpkeRecipient incorrect in new[] { badPsk, badId, badInfo, badKey, baseRecipient })
                {
                    Assert.Throws<AuthenticationTagMismatchException>(() => incorrect.Open(ciphertext, aad));
                }

                byte[] baseCiphertext = baseSender.Seal(plaintext, aad);
                Assert.Throws<AuthenticationTagMismatchException>(() => pskRecipientForBase.Open(baseCiphertext, aad));
                byte[] tamperedCiphertext = (byte[])ciphertext.Clone();
                tamperedCiphertext[^1] ^= 1;
                byte[] destination = new byte[plaintext.Length];
                destination.AsSpan().Fill(0xA5);
                Assert.Throws<AuthenticationTagMismatchException>(
                    () => recipient.Open(tamperedCiphertext, destination.AsSpan(), aad));
                Assert.Equal(new byte[destination.Length], destination);
                Assert.Equal(plaintext, recipient.Open(ciphertext, aad));
                Assert.Equal(plaintext, recipient.Open(sender.Seal(plaintext, aad), aad));
            }
        }

        [Fact]
        public static void Psk_ArgumentValidation()
        {
            HpkeSuite suite = new(HpkeKem.DHKEM_P256_HKDF_SHA256, HpkeKdf.HKDF_SHA256, HpkeAead.AES_128_GCM);

            using (RecordingHpke key = new(suite))
            {
                byte[] psk = new byte[32];
                byte[] pskId = [1];
                byte[] enc = new byte[suite.EncapsulatedSecretSizeInBytes];
                AssertExtensions.Throws<ArgumentNullException>("psk", () => key.CreatePskSender((byte[])null, pskId, out _));
                AssertExtensions.Throws<ArgumentNullException>("pskId", () => key.CreatePskSender(psk, (byte[])null, out _));
                AssertExtensions.Throws<ArgumentNullException>(
                    "encapsulatedSecret", () => key.CreatePskRecipient((byte[])null, psk, pskId));
                AssertExtensions.Throws<ArgumentNullException>("psk", () => key.CreatePskRecipient(enc, (byte[])null, pskId));
                AssertExtensions.Throws<ArgumentNullException>("pskId", () => key.CreatePskRecipient(enc, psk, (byte[])null));

                foreach (int length in new[] { 0, 1, 31 })
                {
                    AssertPskArgumentException(key, "psk", new byte[length], pskId, enc, []);
                }

                AssertPskArgumentException(key, "pskId", psk, [], enc, []);
                foreach (int length in new[] { 0, enc.Length - 1, enc.Length + 1 })
                {
                    byte[] invalidEnc = new byte[length];
                    AssertExtensions.Throws<ArgumentException>(
                        "encapsulatedSecret", () => key.CreatePskSender(psk, pskId, invalidEnc.AsSpan()));
                    AssertExtensions.Throws<ArgumentException>(
                        "encapsulatedSecret", () => key.CreatePskRecipient(invalidEnc, psk, pskId));
                    AssertExtensions.Throws<ArgumentException>(
                        "encapsulatedSecret", () => key.CreatePskRecipient(invalidEnc.AsSpan(), psk, pskId));
                }

                key.Dispose();
                Assert.Throws<ObjectDisposedException>(() => key.CreatePskSender(psk, pskId, out _));
                Assert.Throws<ObjectDisposedException>(() => key.CreatePskSender(psk.AsSpan(), pskId, out _));
                Assert.Throws<ObjectDisposedException>(() => key.CreatePskSender(psk, pskId, enc.AsSpan()));
                Assert.Throws<ObjectDisposedException>(() => key.CreatePskRecipient(enc, psk, pskId));
                Assert.Throws<ObjectDisposedException>(() => key.CreatePskRecipient(enc.AsSpan(), psk, pskId));
                Assert.Equal(0, key.PskCalls);
            }
        }

        public static IEnumerable<object[]> PskInputLengthData()
        {
            foreach (HpkeKdf kdf in Enum.GetValues<HpkeKdf>())
            {
                yield return new object[] { kdf, 32, 1, 0, null };
                yield return new object[] { kdf, 33, 1, 0, null };
                yield return new object[] { kdf, 65535, 65535, 65535, null };
                bool shake = kdf is HpkeKdf.SHAKE128 or HpkeKdf.SHAKE256;
                yield return new object[] { kdf, 65536, 1, 0, shake ? "psk" : null };
                yield return new object[] { kdf, 32, 65536, 0, shake ? "pskId" : null };
                yield return new object[] { kdf, 32, 1, 65536, shake ? "info" : null };
            }
        }

        [Theory]
        [MemberData(nameof(PskInputLengthData))]
        public static void Psk_InputLengths(HpkeKdf kdf, int pskLength, int idLength, int infoLength, string invalidParameter)
        {
            HpkeSuite suite = new(HpkeKem.DHKEM_P256_HKDF_SHA256, kdf, HpkeAead.AES_128_GCM);
            byte[] psk = new byte[pskLength];
            psk.AsSpan().Fill(0x3C);
            byte[] pskId = new byte[idLength];
            pskId.AsSpan().Fill(0x1D);
            byte[] info = new byte[infoLength];
            info.AsSpan().Fill(0x39);
            byte[] enc = new byte[suite.EncapsulatedSecretSizeInBytes];

            using (RecordingHpke key = new(suite))
            {
                if (invalidParameter is not null)
                {
                    AssertPskArgumentException(key, invalidParameter, psk, pskId, enc, info);
                    Assert.Equal(0, key.PskCalls);
                    return;
                }

                byte[] expectedEnc = new byte[enc.Length];
                expectedEnc.AsSpan().Fill(0xD7);
                using (HpkeSender sender = key.CreatePskSender(psk, pskId, out byte[] arrayEnc, info))
                {
                    Assert.Equal(expectedEnc, arrayEnc);
                    Assert.Same(suite, sender.Suite);
                }

                using (HpkeSender sender = key.CreatePskSender(psk.AsSpan(), pskId, out byte[] spanEnc, info))
                {
                    Assert.Equal(expectedEnc, spanEnc);
                }

                using (HpkeSender sender = key.CreatePskSender(psk, pskId, enc.AsSpan(), info))
                {
                    Assert.Equal(expectedEnc, enc);
                }

                Assert.Equal(info, key.LastSenderInfo);
                using (HpkeRecipient recipient = key.CreatePskRecipient(enc, psk, pskId, info))
                using (HpkeRecipient spanRecipient = key.CreatePskRecipient(enc.AsSpan(), psk, pskId, info))
                {
                    Assert.Same(suite, recipient.Suite);
                    Assert.Same(suite, spanRecipient.Suite);
                    Assert.Equal(enc, key.LastRecipientEncapsulatedSecret);
                    Assert.Equal(info, key.LastRecipientInfo);
                    Assert.Equal(psk, key.LastPsk);
                    Assert.Equal(pskId, key.LastPskId);
                }

                Assert.Equal(5, key.PskCalls);
            }
        }

        private static void AssertPskArgumentException(
            Hpke key, string parameter, byte[] psk, byte[] pskId, byte[] enc, byte[] info)
        {
            byte[] original = [0xA5];
            byte[] result = original;
            AssertExtensions.Throws<ArgumentException>(parameter, () => key.CreatePskSender(psk, pskId, out result, info));
            Assert.Same(original, result);
            AssertExtensions.Throws<ArgumentException>(
                parameter, () => key.CreatePskSender(psk.AsSpan(), pskId, out result, info));
            Assert.Same(original, result);
            byte[] originalEnc = (byte[])enc.Clone();
            AssertExtensions.Throws<ArgumentException>(parameter, () => key.CreatePskSender(psk, pskId, enc.AsSpan(), info));
            Assert.Equal(originalEnc, enc);
            AssertExtensions.Throws<ArgumentException>(parameter, () => key.CreatePskRecipient(enc, psk, pskId, info));
            AssertExtensions.Throws<ArgumentException>(parameter, () => key.CreatePskRecipient(enc.AsSpan(), psk, pskId, info));
        }

        [Fact]
        public static void Psk_CoreFailureDoesNotPublishOutput()
        {
            HpkeSuite suite = new(HpkeKem.DHKEM_P256_HKDF_SHA256, HpkeKdf.HKDF_SHA256, HpkeAead.AES_128_GCM);

            using (RecordingHpke key = new(suite) { ThrowOnCreateSender = true })
            {
                byte[] psk = new byte[32];
                byte[] pskId = [1];
                byte[] original = [0xA5];
                byte[] enc = original;
                Assert.Throws<CryptographicException>(() => key.CreatePskSender(psk, pskId, out enc));
                Assert.Same(original, enc);
                Assert.Throws<CryptographicException>(() => key.CreatePskSender(psk.AsSpan(), pskId, out enc));
                Assert.Same(original, enc);
                Assert.Throws<CryptographicException>(
                    () => key.CreatePskSender(psk, pskId, new byte[suite.EncapsulatedSecretSizeInBytes].AsSpan()));
                Assert.Equal(3, key.PskCalls);
            }
        }

        private static void AssertRecipientExports(
            HpkeRecipient recipient, string emptyContextExportHex, string zeroContextExportHex, string testContextExportHex)
        {
            byte[][] contexts = [[], [0], "TestContext"u8.ToArray()];
            string[] expectedHex = [emptyContextExportHex, zeroContextExportHex, testContextExportHex];

            for (int i = 0; i < contexts.Length; i++)
            {
                byte[] expected = Convert.FromHexString(expectedHex[i]);
                Assert.Equal(expected, recipient.Export(contexts[i], expected.Length));
                Assert.Equal(expected, recipient.Export(contexts[i].AsSpan(), expected.Length));
                byte[] destination = new byte[expected.Length + 2];
                destination.AsSpan().Fill(0xA5);
                recipient.Export(contexts[i], destination.AsSpan(1, expected.Length));
                AssertExtensions.SequenceEqual(expected.AsSpan(), destination.AsSpan(1, expected.Length));
                Assert.Equal(0xA5, destination[0]);
                Assert.Equal(0xA5, destination[^1]);
            }
        }

        [Theory]
        [MemberData(nameof(OpenSuiteData))]
        public static void Context_Export(HpkeKem kem, HpkeKdf kdf, HpkeAead aead)
        {
            HpkeSuite suite = new(kem, kdf, aead);

            if (!Hpke.IsSupported(suite))
            {
                Assert.Throws<PlatformNotSupportedException>(() => Hpke.GenerateKey(suite));
                return;
            }

            int maximumLength = kdf switch
            {
                HpkeKdf.HKDF_SHA256 => 8160,
                HpkeKdf.HKDF_SHA384 => 12240,
                HpkeKdf.HKDF_SHA512 => 16320,
                HpkeKdf.SHAKE128 or HpkeKdf.SHAKE256 => 65535,
                _ => throw new InvalidOperationException(),
            };

            foreach (bool usePsk in new[] { false, true })
            {
                using (Hpke key = Hpke.GenerateKey(suite))
                {
                    byte[] psk = new byte[32];
                    byte[] pskId = "identifier"u8.ToArray();
                    byte[] info = "application context"u8.ToArray();
                    byte[] enc;
                    using (HpkeSender sender = usePsk
                        ? key.CreatePskSender(psk, pskId, out enc, info)
                        : key.CreateSender(out enc, info))
                    using (HpkeRecipient recipient = usePsk
                        ? key.CreatePskRecipient(enc, psk, pskId, info)
                        : key.CreateRecipient(enc, info))
                    {
                        byte[] context = "exporter context"u8.ToArray();
                        byte[] originalContext = (byte[])context.Clone();
                        byte[] referenceExport = sender.Export(context, 32);
                        Assert.Equal(referenceExport, recipient.Export(context, 32));

                        key.Dispose();
                        psk.AsSpan().Clear();
                        pskId.AsSpan().Clear();
                        info.AsSpan().Clear();

                        foreach (int length in new[] { 0, 1, 31, 32, 33, 65, maximumLength })
                        {
                            byte[] expected = sender.Export(context, length);
                            Assert.Equal(length, expected.Length);
                            Assert.Equal(expected, sender.Export(context.AsSpan(), length));
                            Assert.Equal(expected, recipient.Export(context, length));
                            Assert.Equal(expected, recipient.Export(context.AsSpan(), length));

                            byte[] senderBuffer = new byte[length + 2];
                            byte[] recipientBuffer = new byte[length + 2];
                            senderBuffer.AsSpan().Fill(0xA5);
                            recipientBuffer.AsSpan().Fill(0xA5);
                            sender.Export(context, senderBuffer.AsSpan(1, length));
                            recipient.Export(context, recipientBuffer.AsSpan(1, length));
                            AssertExtensions.SequenceEqual(expected.AsSpan(), senderBuffer.AsSpan(1, length));
                            Assert.Equal(senderBuffer, recipientBuffer);
                            Assert.Equal(0xA5, senderBuffer[0]);
                            Assert.Equal(0xA5, senderBuffer[^1]);
                            expected.AsSpan().Clear();
                        }

                        Assert.Equal(originalContext, context);
                        Assert.Equal(referenceExport, sender.Export(context, 32));
                        Assert.NotEqual(referenceExport, sender.Export(Array.Empty<byte>(), 32));
                        Assert.NotEqual(referenceExport, sender.Export(new byte[] { 0 }, 32));
                        Assert.False(referenceExport.AsSpan().SequenceEqual(sender.Export(context, 33).AsSpan(0, 32)));
                        byte[] longContext = new byte[65536];
                        longContext.AsSpan().Fill(0x39);
                        Assert.Equal(sender.Export(longContext, 32), recipient.Export(longContext, 32));

                        AssertExtensions.Throws<ArgumentOutOfRangeException>(
                            "length", () => sender.Export(context, maximumLength + 1));
                        AssertExtensions.Throws<ArgumentOutOfRangeException>(
                            "length", () => recipient.Export(context, maximumLength + 1));
                        byte[] invalidDestination = new byte[maximumLength + 1];
                        invalidDestination.AsSpan().Fill(0xA5);
                        byte[] originalDestination = (byte[])invalidDestination.Clone();
                        AssertExtensions.Throws<ArgumentException>(
                            "destination", () => sender.Export(context, invalidDestination.AsSpan()));
                        AssertExtensions.Throws<ArgumentException>(
                            "destination", () => recipient.Export(context, invalidDestination.AsSpan()));
                        Assert.Equal(originalDestination, invalidDestination);

                        byte[] message = "message"u8.ToArray();
                        for (int i = 0; i < 3; i++)
                        {
                            byte[] ciphertext = sender.Seal(message);
                            Assert.Equal(referenceExport, sender.Export(context, 32));
                            Assert.Equal(referenceExport, recipient.Export(context, 32));
                            Assert.Equal(message, recipient.Open(ciphertext));
                            Assert.Equal(referenceExport, recipient.Export(context, 32));
                        }

                        sender.Dispose();
                        Assert.Throws<ObjectDisposedException>(() => sender.Export(context, 0));
                        Assert.Throws<ObjectDisposedException>(() => sender.Export(context.AsSpan(), 32));
                        Assert.Throws<ObjectDisposedException>(() => sender.Export(context, new byte[32].AsSpan()));
                        Assert.Equal(referenceExport, recipient.Export(context, 32));
                        recipient.Dispose();
                        Assert.Throws<ObjectDisposedException>(() => recipient.Export(context, 0));
                        Assert.Throws<ObjectDisposedException>(() => recipient.Export(context.AsSpan(), 32));
                        Assert.Throws<ObjectDisposedException>(() => recipient.Export(context, new byte[32].AsSpan()));
                    }
                }
            }
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(1)]
        public static void Seal_RejectsOverlappingBuffers(int offset)
        {
            HpkeSuite suite = new(HpkeKem.DHKEM_P256_HKDF_SHA256, HpkeKdf.HKDF_SHA256, HpkeAead.AES_128_GCM);
            // Indices: plaintext, associatedData, info, encapsulatedSecret, ciphertext.
            (int First, int Second)[] pairs = [(0, 3), (1, 3), (2, 3), (0, 4), (1, 4), (2, 4), (3, 4)];

            foreach ((int first, int second) in pairs)
            {
                byte[][] buffers = [new byte[128], new byte[128], new byte[128], new byte[128], new byte[128]];
                buffers[second] = buffers[first];
                buffers[first].AsSpan().Fill(0xA5);
                byte[] original = (byte[])buffers[first].Clone();
                int[] starts = [16, 16, 16, 16, 16];
                starts[second] += offset;

                using (RecordingHpke key = new(suite))
                {
                    Assert.Throws<CryptographicException>(() => key.Seal(
                        buffers[0].AsSpan(starts[0], 32),
                        buffers[3].AsSpan(starts[3], suite.EncapsulatedSecretSizeInBytes),
                        buffers[4].AsSpan(starts[4], suite.GetCiphertextLength(32)),
                        buffers[1].AsSpan(starts[1], 32),
                        buffers[2].AsSpan(starts[2], 32)));
                    Assert.Equal(0, key.SealCalls);
                    Assert.Equal(original, buffers[first]);
                }
            }
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(1)]
        public static void CreateSender_RejectsOverlappingBuffers(int offset)
        {
            HpkeSuite suite = new(HpkeKem.DHKEM_P256_HKDF_SHA256, HpkeKdf.HKDF_SHA256, HpkeAead.AES_128_GCM);
            byte[] buffer = new byte[128];
            buffer.AsSpan().Fill(0xA5);
            byte[] original = (byte[])buffer.Clone();

            using (RecordingHpke key = new(suite))
            {
                Assert.Throws<CryptographicException>(() => key.CreateSender(
                    buffer.AsSpan(16 + offset, suite.EncapsulatedSecretSizeInBytes),
                    buffer.AsSpan(16, 32)));
                Assert.Equal(0, key.CreateSenderCalls);
                Assert.Equal(original, buffer);
            }
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(1)]
        public static void CreatePskSender_RejectsOverlappingBuffers(int offset)
        {
            HpkeSuite suite = new(HpkeKem.DHKEM_P256_HKDF_SHA256, HpkeKdf.HKDF_SHA256, HpkeAead.AES_128_GCM);

            for (int input = 0; input < 3; input++)
            {
                byte[][] inputs = [new byte[128], new byte[128], new byte[128]];
                byte[] output = inputs[input];
                output.AsSpan().Fill(0xA5);
                byte[] original = (byte[])output.Clone();

                using (RecordingHpke key = new(suite))
                {
                    Assert.Throws<CryptographicException>(() => key.CreatePskSender(
                        inputs[0].AsSpan(16, 32),
                        inputs[1].AsSpan(16, 32),
                        output.AsSpan(16 + offset, suite.EncapsulatedSecretSizeInBytes),
                        inputs[2].AsSpan(16, 32)));
                    Assert.Equal(0, key.PskCalls);
                    Assert.Equal(0, key.CreateSenderCalls);
                    Assert.Equal(original, output);
                }
            }
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(1)]
        public static void Sender_SealRejectsOverlappingBuffers(int offset)
        {
            HpkeSuite suite = new(HpkeKem.DHKEM_P256_HKDF_SHA256, HpkeKdf.HKDF_SHA256, HpkeAead.AES_128_GCM);

            for (int input = 0; input < 2; input++)
            {
                byte[][] inputs = [new byte[128], new byte[128]];
                byte[] output = inputs[input];
                output.AsSpan().Fill(0xA5);
                byte[] original = (byte[])output.Clone();

                using (RecordingHpkeSender sender = new(suite))
                {
                    Assert.Throws<CryptographicException>(() => sender.Seal(
                        inputs[0].AsSpan(16, 32),
                        output.AsSpan(16 + offset, suite.GetCiphertextLength(32)),
                        inputs[1].AsSpan(16, 32)));
                    Assert.Equal(0, sender.SealCalls);
                    Assert.Equal(original, output);
                }
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(32)]
        public static void Seal_AllowsReadOnlyOverlapAndAdjacentOutputs(int inputLength)
        {
            HpkeSuite suite = new(HpkeKem.DHKEM_P256_HKDF_SHA256, HpkeKdf.HKDF_SHA256, HpkeAead.AES_128_GCM);
            int encLength = suite.EncapsulatedSecretSizeInBytes;
            int ciphertextLength = suite.GetCiphertextLength(inputLength);
            byte[] buffer = new byte[inputLength + encLength + ciphertextLength];
            buffer.AsSpan().Fill(0xA5);
            byte[] originalInput = buffer.AsSpan(0, inputLength).ToArray();

            using (RecordingHpke key = new(suite))
            {
                key.Seal(
                    buffer.AsSpan(0, inputLength),
                    buffer.AsSpan(inputLength, encLength),
                    buffer.AsSpan(inputLength + encLength, ciphertextLength),
                    buffer.AsSpan(0, inputLength),
                    buffer.AsSpan(0, inputLength));
                Assert.Equal(1, key.SealCalls);
                AssertExtensions.SequenceEqual(originalInput.AsSpan(), buffer.AsSpan(0, inputLength));
                Assert.Equal(0xD7, buffer[inputLength]);
                Assert.Equal(0xC8, buffer[^1]);
            }

            using (RecordingHpkeSender sender = new(suite))
            {
                sender.Seal(
                    buffer.AsSpan(0, inputLength),
                    buffer.AsSpan(inputLength, ciphertextLength),
                    buffer.AsSpan(0, inputLength));
                Assert.Equal(1, sender.SealCalls);
                AssertExtensions.SequenceEqual(originalInput.AsSpan(), buffer.AsSpan(0, inputLength));
            }
        }

        [Fact]
        public static void CreateSender_AllowsReadOnlyOverlapAndAdjacentOutput()
        {
            HpkeSuite suite = new(HpkeKem.DHKEM_P256_HKDF_SHA256, HpkeKdf.HKDF_SHA256, HpkeAead.AES_128_GCM);
            byte[] buffer = new byte[32 + suite.EncapsulatedSecretSizeInBytes];
            buffer.AsSpan().Fill(0xA5);
            byte[] originalInput = buffer.AsSpan(0, 32).ToArray();

            using (RecordingHpke key = new(suite))
            using (HpkeSender sender = key.CreateSender(buffer.AsSpan(32), buffer.AsSpan(0, 32)))
            using (HpkeSender pskSender = key.CreatePskSender(
                buffer.AsSpan(0, 32), buffer.AsSpan(0, 32), buffer.AsSpan(32), buffer.AsSpan(0, 32)))
            {
                Assert.Equal(2, key.CreateSenderCalls);
                Assert.Equal(1, key.PskCalls);
                Assert.Equal(originalInput, key.LastPsk);
                Assert.Equal(originalInput, key.LastPskId);
                Assert.Equal(originalInput, key.LastSenderInfo);
                AssertExtensions.SequenceEqual(originalInput.AsSpan(), buffer.AsSpan(0, 32));
                Assert.Equal(0xD7, buffer[^1]);
            }
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(null)]
        public static void Open_RejectsOverlappingBuffers(int? offset)
        {
            HpkeSuite suite = new(HpkeKem.DHKEM_P256_HKDF_SHA256, HpkeKdf.HKDF_SHA256, HpkeAead.AES_128_GCM);
            int[] lengths = [suite.EncapsulatedSecretSizeInBytes, suite.GetCiphertextLength(32), 32, 32];

            for (int input = 0; input < lengths.Length; input++)
            {
                byte[][] inputs = [new byte[128], new byte[128], new byte[128], new byte[128]];
                byte[] output = inputs[input];
                output.AsSpan().Fill(0xA5);
                byte[] original = (byte[])output.Clone();
                int outputStart = 16 + (offset ?? lengths[input] - 1);

                using (RecordingHpke key = new(suite))
                {
                    Assert.Throws<CryptographicException>(() => key.Open(
                        inputs[0].AsSpan(16, lengths[0]),
                        inputs[1].AsSpan(16, lengths[1]),
                        output.AsSpan(outputStart, 32),
                        inputs[2].AsSpan(16, lengths[2]),
                        inputs[3].AsSpan(16, lengths[3])));
                    Assert.False(key.OpenCoreCalled);
                    Assert.Equal(original, output);
                }
            }
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(null)]
        public static void Recipient_OpenRejectsOverlappingBuffers(int? offset)
        {
            HpkeSuite suite = new(HpkeKem.DHKEM_P256_HKDF_SHA256, HpkeKdf.HKDF_SHA256, HpkeAead.AES_128_GCM);
            int[] lengths = [suite.GetCiphertextLength(32), 32];

            for (int input = 0; input < lengths.Length; input++)
            {
                byte[][] inputs = [new byte[128], new byte[128]];
                byte[] output = inputs[input];
                output.AsSpan().Fill(0xA5);
                byte[] original = (byte[])output.Clone();
                int outputStart = 16 + (offset ?? lengths[input] - 1);

                using (RecordingHpkeRecipient recipient = new(suite))
                {
                    Assert.Throws<CryptographicException>(() => recipient.Open(
                        inputs[0].AsSpan(16, lengths[0]),
                        output.AsSpan(outputStart, 32),
                        inputs[1].AsSpan(16, lengths[1])));
                    Assert.Equal(0, recipient.OpenCalls);
                    Assert.Equal(original, output);
                }
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(32)]
        public static void Open_AllowsReadOnlyOverlapAndAdjacentOutput(int plaintextLength)
        {
            HpkeSuite suite = new(HpkeKem.DHKEM_P256_HKDF_SHA256, HpkeKdf.HKDF_SHA256, HpkeAead.AES_128_GCM);
            int encLength = suite.EncapsulatedSecretSizeInBytes;
            int ciphertextLength = suite.GetCiphertextLength(plaintextLength);
            int inputLength = Math.Max(encLength, ciphertextLength);
            byte[] buffer = new byte[inputLength + plaintextLength + 1];
            buffer.AsSpan().Fill(0xA5);
            byte[] originalInput = buffer.AsSpan(0, inputLength).ToArray();

            using (RecordingHpke key = new(suite))
            {
                key.Open(
                    buffer.AsSpan(0, encLength),
                    buffer.AsSpan(0, ciphertextLength),
                    buffer.AsSpan(inputLength, plaintextLength),
                    buffer.AsSpan(0, inputLength),
                    buffer.AsSpan(0, inputLength));
                Assert.True(key.OpenCoreCalled);
                AssertExtensions.SequenceEqual(originalInput.AsSpan(), buffer.AsSpan(0, inputLength));
                AssertExtensions.SequenceEqual(new byte[plaintextLength].AsSpan(), buffer.AsSpan(inputLength, plaintextLength));
                Assert.Equal(0xA5, buffer[^1]);
            }

            using (RecordingHpkeRecipient recipient = new(suite))
            {
                recipient.Open(
                    buffer.AsSpan(0, ciphertextLength),
                    buffer.AsSpan(inputLength, plaintextLength),
                    buffer.AsSpan(0, inputLength));
                Assert.Equal(1, recipient.OpenCalls);
                AssertExtensions.SequenceEqual(originalInput.AsSpan(), buffer.AsSpan(0, inputLength));
                Assert.Equal(0xA5, buffer[^1]);
            }
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(31)]
        public static void Export_RejectsOverlappingBuffers(int offset)
        {
            HpkeSuite suite = new(HpkeKem.DHKEM_P256_HKDF_SHA256, HpkeKdf.HKDF_SHA256, HpkeAead.AES_128_GCM);
            byte[] buffer = new byte[96];
            buffer.AsSpan().Fill(0xA5);
            byte[] original = (byte[])buffer.Clone();

            using (RecordingHpkeSender sender = new(suite))
            {
                Assert.Throws<CryptographicException>(() => sender.Export(
                    buffer.AsSpan(16, 32), buffer.AsSpan(16 + offset, 32)));
                Assert.Equal(0, sender.ExportCalls);
                Assert.Equal(original, buffer);
            }

            using (RecordingHpkeRecipient recipient = new(suite))
            {
                Assert.Throws<CryptographicException>(() => recipient.Export(
                    buffer.AsSpan(16, 32), buffer.AsSpan(16 + offset, 32)));
                Assert.Equal(0, recipient.ExportCalls);
                Assert.Equal(original, buffer);
            }
        }

        [Theory]
        [InlineData(0, 32)]
        [InlineData(32, 0)]
        [InlineData(32, 32)]
        public static void Export_AllowsEmptyAndAdjacentBuffers(int contextLength, int outputLength)
        {
            HpkeSuite suite = new(HpkeKem.DHKEM_P256_HKDF_SHA256, HpkeKdf.HKDF_SHA256, HpkeAead.AES_128_GCM);
            byte[] buffer = new byte[contextLength + outputLength + 1];
            buffer.AsSpan().Fill(0xA5);
            byte[] originalContext = buffer.AsSpan(0, contextLength).ToArray();
            byte[] expected = new byte[outputLength];
            expected.AsSpan().Fill(0xE7);

            using (RecordingHpkeSender sender = new(suite))
            {
                sender.Export(buffer.AsSpan(0, contextLength), buffer.AsSpan(contextLength, outputLength));
                Assert.Equal(1, sender.ExportCalls);
                Assert.Equal(originalContext, sender.LastExporterContext);
                AssertExtensions.SequenceEqual(originalContext.AsSpan(), buffer.AsSpan(0, contextLength));
                AssertExtensions.SequenceEqual(expected.AsSpan(), buffer.AsSpan(contextLength, outputLength));
                Assert.Equal(0xA5, buffer[^1]);
            }

            buffer.AsSpan(contextLength).Fill(0xA5);

            using (RecordingHpkeRecipient recipient = new(suite))
            {
                recipient.Export(buffer.AsSpan(0, contextLength), buffer.AsSpan(contextLength, outputLength));
                Assert.Equal(1, recipient.ExportCalls);
                Assert.Equal(originalContext, recipient.LastExporterContext);
                AssertExtensions.SequenceEqual(originalContext.AsSpan(), buffer.AsSpan(0, contextLength));
                AssertExtensions.SequenceEqual(expected.AsSpan(), buffer.AsSpan(contextLength, outputLength));
                Assert.Equal(0xA5, buffer[^1]);
            }
        }

        private sealed class RecordingHpke : Hpke
        {
            internal bool OpenCoreCalled { get; private set; }
            internal int SealCalls { get; private set; }
            internal int CreateSenderCalls { get; private set; }
            internal byte[] LastSenderInfo { get; private set; } = [];
            internal bool ThrowOnCreateSender { get; set; }
            internal int CreateRecipientCalls { get; private set; }
            internal byte[] LastRecipientEncapsulatedSecret { get; private set; } = [];
            internal byte[] LastRecipientInfo { get; private set; } = [];
            internal int PskCalls { get; private set; }
            internal byte[] LastPsk { get; private set; } = [];
            internal byte[] LastPskId { get; private set; } = [];

            internal RecordingHpke(HpkeSuite suite) : base(suite)
            {
            }

            protected override void OpenCore(
                ReadOnlySpan<byte> encapsulatedSecret,
                ReadOnlySpan<byte> ciphertext,
                Span<byte> plaintext,
                ReadOnlySpan<byte> associatedData,
                ReadOnlySpan<byte> info)
            {
                OpenCoreCalled = true;
                plaintext.Clear();
            }

            protected override HpkeSender CreateSenderCore(Span<byte> encapsulatedSecret, ReadOnlySpan<byte> info)
            {
                CreateSenderCalls++;
                LastSenderInfo = info.ToArray();
                encapsulatedSecret.Fill(0xD7);

                if (ThrowOnCreateSender)
                {
                    throw new CryptographicException("Sender creation test failure.");
                }

                return new RecordingHpkeSender(Suite);
            }

            protected override HpkeRecipient CreateRecipientCore(
                ReadOnlySpan<byte> encapsulatedSecret,
                ReadOnlySpan<byte> info)
            {
                CreateRecipientCalls++;
                LastRecipientEncapsulatedSecret = encapsulatedSecret.ToArray();
                LastRecipientInfo = info.ToArray();
                return new RecordingHpkeRecipient(Suite);
            }

            protected override HpkeSender CreatePskSenderCore(
                Span<byte> encapsulatedSecret,
                ReadOnlySpan<byte> info,
                ReadOnlySpan<byte> psk,
                ReadOnlySpan<byte> pskId)
            {
                RecordPskInputs(psk, pskId);
                return CreateSenderCore(encapsulatedSecret, info);
            }

            protected override HpkeRecipient CreatePskRecipientCore(
                ReadOnlySpan<byte> encapsulatedSecret,
                ReadOnlySpan<byte> info,
                ReadOnlySpan<byte> psk,
                ReadOnlySpan<byte> pskId)
            {
                RecordPskInputs(psk, pskId);
                return CreateRecipientCore(encapsulatedSecret, info);
            }

            private void RecordPskInputs(ReadOnlySpan<byte> psk, ReadOnlySpan<byte> pskId)
            {
                PskCalls++;
                LastPsk = psk.ToArray();
                LastPskId = pskId.ToArray();
            }

            protected override void ExportDecapsulationKeyCore(Span<byte> destination) =>
                throw new InvalidOperationException("Unexpected key export.");

            protected override void ExportEncapsulationKeyCore(Span<byte> destination) =>
                throw new InvalidOperationException("Unexpected key export.");

            protected override void SealCore(
                ReadOnlySpan<byte> plaintext,
                Span<byte> encapsulatedSecret,
                Span<byte> ciphertext,
                ReadOnlySpan<byte> associatedData,
                ReadOnlySpan<byte> info)
            {
                SealCalls++;
                encapsulatedSecret.Fill(0xD7);
                ciphertext.Fill(0xC8);
            }
        }

        [Theory]
        [InlineData(HpkeKem.DHKEM_P256_HKDF_SHA256)]
        [InlineData(HpkeKem.DHKEM_P384_HKDF_SHA384)]
        [InlineData(HpkeKem.DHKEM_P521_HKDF_SHA512)]
        [InlineData(HpkeKem.DHKEM_X25519_HKDF_SHA256)]
        public static void GenerateKey(HpkeKem kem)
        {
            HpkeSuite suite = new(kem, HpkeKdf.HKDF_SHA256, HpkeAead.AES_128_GCM);

            if (!Hpke.IsSupported(suite))
            {
                Assert.Throws<PlatformNotSupportedException>(() => Hpke.GenerateKey(suite));
                return;
            }

            Hpke key = Hpke.GenerateKey(suite);

            try
            {
                Assert.Same(suite, key.Suite);

                byte[] privateKey = key.ExportDecapsulationKey();

                try
                {
                    Assert.Equal(suite.DecapsulationKeySizeInBytes, privateKey.Length);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(privateKey);
                }
            }
            finally
            {
                key.Dispose();
            }

            key.Dispose();
        }

        [Theory]
        [InlineData(HpkeKem.DHKEM_P256_HKDF_SHA256)]
        [InlineData(HpkeKem.DHKEM_P384_HKDF_SHA384)]
        [InlineData(HpkeKem.DHKEM_P521_HKDF_SHA512)]
        [InlineData(HpkeKem.DHKEM_X25519_HKDF_SHA256)]
        public static void ExportDecapsulationKey_BufferAndLifetime(HpkeKem kem)
        {
            HpkeSuite suite = new(kem, HpkeKdf.HKDF_SHA256, HpkeAead.AES_128_GCM);

            if (!Hpke.IsSupported(suite))
            {
                Assert.Throws<PlatformNotSupportedException>(() => Hpke.GenerateKey(suite));
                return;
            }

            using (Hpke key = Hpke.GenerateKey(suite))
            {
                int keySize = suite.DecapsulationKeySizeInBytes;
                byte[] buffer = new byte[keySize + 2];
                byte[] exported = key.ExportDecapsulationKey();

                try
                {
                    buffer.AsSpan().Fill(0xA5);
                    key.ExportDecapsulationKey(buffer.AsSpan(1, keySize));
                    AssertExtensions.SequenceEqual(exported.AsSpan(), buffer.AsSpan(1, keySize));
                    Assert.Equal(0xA5, buffer[0]);
                    Assert.Equal(0xA5, buffer[^1]);

                    exported.AsSpan().Clear();
                    key.ExportDecapsulationKey(exported);
                    AssertExtensions.SequenceEqual(exported.AsSpan(), buffer.AsSpan(1, keySize));

                    AssertExtensions.Throws<ArgumentException>(
                        "destination", () => key.ExportDecapsulationKey(Span<byte>.Empty));
                    AssertExtensions.Throws<ArgumentException>(
                        "destination", () => key.ExportDecapsulationKey(buffer.AsSpan(0, keySize - 1)));
                    AssertExtensions.Throws<ArgumentException>(
                        "destination", () => key.ExportDecapsulationKey(buffer.AsSpan(0, keySize + 1)));
                    AssertExtensions.SequenceEqual(exported.AsSpan(), buffer.AsSpan(1, keySize));

                    key.Dispose();
                    Assert.Throws<ObjectDisposedException>(() => key.ExportDecapsulationKey());
                    Assert.Throws<ObjectDisposedException>(() => key.ExportDecapsulationKey(exported));
                    AssertExtensions.SequenceEqual(exported.AsSpan(), buffer.AsSpan(1, keySize));
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(exported);
                    CryptographicOperations.ZeroMemory(buffer);
                }
            }
        }

        [Theory]
        [InlineData(HpkeKem.DHKEM_P256_HKDF_SHA256)]
        [InlineData(HpkeKem.DHKEM_P384_HKDF_SHA384)]
        [InlineData(HpkeKem.DHKEM_P521_HKDF_SHA512)]
        [InlineData(HpkeKem.DHKEM_X25519_HKDF_SHA256)]
        public static void ExportEncapsulationKey_BufferAndLifetime(HpkeKem kem)
        {
            HpkeSuite suite = new(kem, HpkeKdf.HKDF_SHA256, HpkeAead.AES_128_GCM);

            if (!Hpke.IsSupported(suite))
            {
                Assert.Throws<PlatformNotSupportedException>(() => Hpke.GenerateKey(suite));
                return;
            }

            using (Hpke key = Hpke.GenerateKey(suite))
            {
                int keySize = suite.EncapsulationKeySizeInBytes;
                byte[] buffer = new byte[keySize + 2];
                byte[] exported = key.ExportEncapsulationKey();

                buffer.AsSpan().Fill(0xA5);
                key.ExportEncapsulationKey(buffer.AsSpan(1, keySize));
                AssertExtensions.SequenceEqual(exported.AsSpan(), buffer.AsSpan(1, keySize));
                Assert.Equal(0xA5, buffer[0]);
                Assert.Equal(0xA5, buffer[^1]);

                exported.AsSpan().Clear();
                key.ExportEncapsulationKey(exported);
                AssertExtensions.SequenceEqual(exported.AsSpan(), buffer.AsSpan(1, keySize));

                AssertExtensions.Throws<ArgumentException>(
                    "destination", () => key.ExportEncapsulationKey(Span<byte>.Empty));
                AssertExtensions.Throws<ArgumentException>(
                    "destination", () => key.ExportEncapsulationKey(buffer.AsSpan(0, keySize - 1)));
                AssertExtensions.Throws<ArgumentException>(
                    "destination", () => key.ExportEncapsulationKey(buffer.AsSpan(0, keySize + 1)));
                AssertExtensions.SequenceEqual(exported.AsSpan(), buffer.AsSpan(1, keySize));

                key.Dispose();
                Assert.Throws<ObjectDisposedException>(() => key.ExportEncapsulationKey());
                Assert.Throws<ObjectDisposedException>(() => key.ExportEncapsulationKey(exported));
                AssertExtensions.SequenceEqual(exported.AsSpan(), buffer.AsSpan(1, keySize));
            }
        }

        [Fact]
        public static void Sender_ConstructorAndDisposal()
        {
            AssertExtensions.Throws<ArgumentNullException>("suite", () => new RecordingHpkeSender(null));
            HpkeSuite suite = new(HpkeKem.DHKEM_P256_HKDF_SHA256, HpkeKdf.HKDF_SHA256, HpkeAead.AES_128_GCM);

            using (RecordingHpkeSender sender = new(suite))
            {
                Assert.Same(suite, sender.Suite);
                sender.Dispose();
                sender.Dispose();
                Assert.Equal(1, sender.DisposeCalls);

                byte[] ciphertext = new byte[suite.AeadTagSizeInBytes];
                Assert.Throws<ObjectDisposedException>(() => sender.Seal(Array.Empty<byte>()));
                Assert.Throws<ObjectDisposedException>(() => sender.Seal(ReadOnlySpan<byte>.Empty));
                Assert.Throws<ObjectDisposedException>(() => sender.Seal(ReadOnlySpan<byte>.Empty, ciphertext.AsSpan()));
                Assert.Throws<ObjectDisposedException>(() => sender.Export(Array.Empty<byte>(), 0));
                Assert.Throws<ObjectDisposedException>(() => sender.Export(ReadOnlySpan<byte>.Empty, 0));
                Assert.Throws<ObjectDisposedException>(() => sender.Export(ReadOnlySpan<byte>.Empty, Span<byte>.Empty));
                Assert.Equal(0, sender.SealCalls);
                Assert.Equal(0, sender.ExportCalls);
            }
        }

        [Theory]
        [InlineData(HpkeAead.AES_128_GCM, 0)]
        [InlineData(HpkeAead.AES_128_GCM, 5)]
        [InlineData(HpkeAead.AES_256_GCM, 5)]
        [InlineData(HpkeAead.ChaCha20Poly1305, 5)]
        public static void Sender_Seal(HpkeAead aead, int plaintextLength)
        {
            HpkeSuite suite = new(HpkeKem.DHKEM_P256_HKDF_SHA256, HpkeKdf.HKDF_SHA256, aead);

            using (RecordingHpkeSender sender = new(suite))
            {
                byte[] plaintext = new byte[plaintextLength];
                plaintext.AsSpan().Fill(0x3C);
                byte[] associatedData = [1, 2, 3];
                byte[] expected = new byte[suite.GetCiphertextLength(plaintextLength)];
                expected.AsSpan().Fill(0xD3);

                Assert.Equal(expected, sender.Seal(plaintext, associatedData));
                Assert.Equal(plaintext, sender.LastPlaintext);
                Assert.Equal(associatedData, sender.LastAssociatedData);

                Assert.Equal(expected, sender.Seal(
                    new ReadOnlySpan<byte>(plaintext), new ReadOnlySpan<byte>(associatedData)));
                Assert.Equal(plaintext, sender.LastPlaintext);
                Assert.Equal(associatedData, sender.LastAssociatedData);

                byte[] destination = new byte[expected.Length + 2];
                destination.AsSpan().Fill(0xA5);
                sender.Seal(plaintext, destination.AsSpan(1, expected.Length), associatedData);
                AssertExtensions.SequenceEqual(expected.AsSpan(), destination.AsSpan(1, expected.Length));
                Assert.Equal(0xA5, destination[0]);
                Assert.Equal(0xA5, destination[^1]);
                Assert.Equal(plaintext, sender.LastPlaintext);
                Assert.Equal(associatedData, sender.LastAssociatedData);

                Assert.Equal(expected, sender.Seal(plaintext));
                Assert.Empty(sender.LastAssociatedData);
                Assert.Equal(expected, sender.Seal(plaintext.AsSpan()));
                Assert.Empty(sender.LastAssociatedData);
                Assert.Equal(5, sender.SealCalls);
                Assert.Equal(0, sender.ExportCalls);
            }
        }

        [Fact]
        public static void Sender_SealValidation()
        {
            HpkeSuite suite = new(HpkeKem.DHKEM_P256_HKDF_SHA256, HpkeKdf.HKDF_SHA256, HpkeAead.AES_128_GCM);

            using (RecordingHpkeSender sender = new(suite))
            {
                AssertExtensions.Throws<ArgumentNullException>("plaintext", () => sender.Seal((byte[])null));

                foreach (int length in new[] { 0, suite.AeadTagSizeInBytes - 1, suite.AeadTagSizeInBytes + 1 })
                {
                    byte[] ciphertext = new byte[length];
                    ciphertext.AsSpan().Fill(0xA5);
                    byte[] originalCiphertext = (byte[])ciphertext.Clone();
                    AssertExtensions.Throws<ArgumentException>(
                        "ciphertext", () => sender.Seal(ReadOnlySpan<byte>.Empty, ciphertext.AsSpan()));
                    Assert.Equal(originalCiphertext, ciphertext);
                }

                Assert.Equal(0, sender.SealCalls);
            }
        }

        [Theory]
        [InlineData(HpkeKdf.HKDF_SHA256, 8160)]
        [InlineData(HpkeKdf.HKDF_SHA384, 12240)]
        [InlineData(HpkeKdf.HKDF_SHA512, 16320)]
        [InlineData(HpkeKdf.SHAKE128, 65535)]
        [InlineData(HpkeKdf.SHAKE256, 65535)]
        public static void Sender_Export(HpkeKdf kdf, int maximumLength)
        {
            HpkeSuite suite = new(HpkeKem.DHKEM_P256_HKDF_SHA256, kdf, HpkeAead.AES_128_GCM);

            using (RecordingHpkeSender sender = new(suite))
            {
                // Unlike setup info, a SHAKE exporter context is not length-prefixed.
                byte[] exporterContext = new byte[65536];
                exporterContext.AsSpan().Fill(0x39);

                foreach (int length in new[] { 0, 1, maximumLength })
                {
                    byte[] expected = new byte[length];
                    expected.AsSpan().Fill(0xE7);
                    Assert.Equal(expected, sender.Export(exporterContext, length));
                    Assert.Equal(exporterContext, sender.LastExporterContext);
                    Assert.Equal(expected, sender.Export(exporterContext.AsSpan(), length));
                    Assert.Equal(exporterContext, sender.LastExporterContext);

                    byte[] destination = new byte[length + 2];
                    destination.AsSpan().Fill(0xA5);
                    sender.Export(exporterContext, destination.AsSpan(1, length));
                    AssertExtensions.SequenceEqual(expected.AsSpan(), destination.AsSpan(1, length));
                    Assert.Equal(0xA5, destination[0]);
                    Assert.Equal(0xA5, destination[^1]);
                    Assert.Equal(exporterContext, sender.LastExporterContext);
                }

                Assert.Equal(9, sender.ExportCalls);
                Assert.Equal(0, sender.SealCalls);
                Assert.Empty(sender.Export(Array.Empty<byte>(), 0));
                Assert.Empty(sender.LastExporterContext);
            }
        }

        [Theory]
        [InlineData(HpkeKdf.HKDF_SHA256, 8160)]
        [InlineData(HpkeKdf.HKDF_SHA384, 12240)]
        [InlineData(HpkeKdf.HKDF_SHA512, 16320)]
        [InlineData(HpkeKdf.SHAKE128, 65535)]
        [InlineData(HpkeKdf.SHAKE256, 65535)]
        public static void Sender_ExportValidation(HpkeKdf kdf, int maximumLength)
        {
            HpkeSuite suite = new(HpkeKem.DHKEM_P256_HKDF_SHA256, kdf, HpkeAead.AES_128_GCM);

            using (RecordingHpkeSender sender = new(suite))
            {
                AssertExtensions.Throws<ArgumentNullException>("exporterContext", () => sender.Export((byte[])null, 0));

                foreach (int length in new[] { -1, int.MinValue, maximumLength + 1, int.MaxValue })
                {
                    AssertExtensions.Throws<ArgumentOutOfRangeException>(
                        "length", () => sender.Export(Array.Empty<byte>(), length));
                    AssertExtensions.Throws<ArgumentOutOfRangeException>(
                        "length", () => sender.Export(ReadOnlySpan<byte>.Empty, length));
                }

                byte[] destination = new byte[maximumLength + 1];
                destination.AsSpan().Fill(0xA5);
                byte[] originalDestination = (byte[])destination.Clone();
                AssertExtensions.Throws<ArgumentException>(
                    "destination", () => sender.Export(ReadOnlySpan<byte>.Empty, destination.AsSpan()));
                Assert.Equal(originalDestination, destination);
                Assert.Equal(0, sender.ExportCalls);
            }
        }

        [Fact]
        public static void Sender_CoreFailuresPropagate()
        {
            HpkeSuite suite = new(HpkeKem.DHKEM_P256_HKDF_SHA256, HpkeKdf.HKDF_SHA256, HpkeAead.AES_128_GCM);

            using (RecordingHpkeSender sender = new(suite) { ThrowOnCoreCall = true })
            {
                byte[] ciphertext = new byte[suite.AeadTagSizeInBytes];
                Assert.Throws<CryptographicException>(() => sender.Seal(Array.Empty<byte>()));
                Assert.Throws<CryptographicException>(() => sender.Seal(ReadOnlySpan<byte>.Empty));
                Assert.Throws<CryptographicException>(() => sender.Seal(ReadOnlySpan<byte>.Empty, ciphertext.AsSpan()));
                Assert.Throws<CryptographicException>(() => sender.Export(Array.Empty<byte>(), 1));
                Assert.Throws<CryptographicException>(() => sender.Export(ReadOnlySpan<byte>.Empty, 1));
                Assert.Throws<CryptographicException>(() => sender.Export(ReadOnlySpan<byte>.Empty, new byte[1].AsSpan()));
                Assert.Equal(3, sender.SealCalls);
                Assert.Equal(3, sender.ExportCalls);
            }
        }

        [Fact]
        public static void Recipient_ConstructorAndDisposal()
        {
            AssertExtensions.Throws<ArgumentNullException>("suite", () => new RecordingHpkeRecipient(null));
            HpkeSuite suite = new(HpkeKem.DHKEM_P256_HKDF_SHA256, HpkeKdf.HKDF_SHA256, HpkeAead.AES_128_GCM);

            using (RecordingHpkeRecipient recipient = new(suite))
            {
                Assert.Same(suite, recipient.Suite);
                recipient.Dispose();
                recipient.Dispose();
                Assert.Equal(1, recipient.DisposeCalls);

                byte[] ciphertext = new byte[suite.AeadTagSizeInBytes];
                Assert.Throws<ObjectDisposedException>(() => recipient.Open(ciphertext));
                Assert.Throws<ObjectDisposedException>(() => recipient.Open(ciphertext.AsSpan()));
                Assert.Throws<ObjectDisposedException>(() => recipient.Open(ciphertext, Span<byte>.Empty));
                Assert.Throws<ObjectDisposedException>(() => recipient.Export(Array.Empty<byte>(), 0));
                Assert.Throws<ObjectDisposedException>(() => recipient.Export(ReadOnlySpan<byte>.Empty, 0));
                Assert.Throws<ObjectDisposedException>(() => recipient.Export(ReadOnlySpan<byte>.Empty, Span<byte>.Empty));
                Assert.Equal(0, recipient.OpenCalls);
                Assert.Equal(0, recipient.ExportCalls);
            }
        }

        [Theory]
        [InlineData(HpkeAead.AES_128_GCM, 0)]
        [InlineData(HpkeAead.AES_128_GCM, 5)]
        [InlineData(HpkeAead.AES_256_GCM, 5)]
        [InlineData(HpkeAead.ChaCha20Poly1305, 5)]
        public static void Recipient_Open(HpkeAead aead, int plaintextLength)
        {
            HpkeSuite suite = new(HpkeKem.DHKEM_P256_HKDF_SHA256, HpkeKdf.HKDF_SHA256, aead);

            using (RecordingHpkeRecipient recipient = new(suite))
            {
                byte[] ciphertext = new byte[suite.GetCiphertextLength(plaintextLength)];
                ciphertext.AsSpan().Fill(0x3C);
                byte[] associatedData = [1, 2, 3];
                byte[] expected = new byte[plaintextLength];
                expected.AsSpan().Fill(0xD3);

                Assert.Equal(expected, recipient.Open(ciphertext, associatedData));
                Assert.Equal(ciphertext, recipient.LastCiphertext);
                Assert.Equal(associatedData, recipient.LastAssociatedData);

                Assert.Equal(expected, recipient.Open(
                    new ReadOnlySpan<byte>(ciphertext), new ReadOnlySpan<byte>(associatedData)));
                Assert.Equal(ciphertext, recipient.LastCiphertext);
                Assert.Equal(associatedData, recipient.LastAssociatedData);

                byte[] destination = new byte[plaintextLength + 2];
                destination.AsSpan().Fill(0xA5);
                recipient.Open(ciphertext, destination.AsSpan(1, plaintextLength), associatedData);
                AssertExtensions.SequenceEqual(expected.AsSpan(), destination.AsSpan(1, plaintextLength));
                Assert.Equal(0xA5, destination[0]);
                Assert.Equal(0xA5, destination[^1]);
                Assert.Equal(ciphertext, recipient.LastCiphertext);
                Assert.Equal(associatedData, recipient.LastAssociatedData);

                Assert.Equal(expected, recipient.Open(ciphertext));
                Assert.Empty(recipient.LastAssociatedData);
                Assert.Equal(expected, recipient.Open(ciphertext.AsSpan()));
                Assert.Empty(recipient.LastAssociatedData);
                Assert.Equal(5, recipient.OpenCalls);
                Assert.Equal(0, recipient.ExportCalls);
            }
        }

        [Fact]
        public static void Recipient_OpenValidation()
        {
            HpkeSuite suite = new(HpkeKem.DHKEM_P256_HKDF_SHA256, HpkeKdf.HKDF_SHA256, HpkeAead.AES_128_GCM);

            using (RecordingHpkeRecipient recipient = new(suite))
            {
                AssertExtensions.Throws<ArgumentNullException>("ciphertext", () => recipient.Open((byte[])null));
                byte[] plaintext = [0xA5];

                foreach (int length in new[] { 0, suite.AeadTagSizeInBytes - 1 })
                {
                    byte[] ciphertext = new byte[length];
                    AssertExtensions.Throws<ArgumentException>("ciphertext", () => recipient.Open(ciphertext));
                    AssertExtensions.Throws<ArgumentException>("ciphertext", () => recipient.Open(ciphertext.AsSpan()));
                    AssertExtensions.Throws<ArgumentException>(
                        "ciphertext", () => recipient.Open(ciphertext, plaintext.AsSpan()));
                    Assert.Equal(0xA5, plaintext[0]);
                }

                byte[] validLengthCiphertext = new byte[suite.GetCiphertextLength(1)];

                foreach (int length in new[] { 0, 2 })
                {
                    byte[] destination = new byte[length];
                    destination.AsSpan().Fill(0xA5);
                    byte[] originalDestination = (byte[])destination.Clone();
                    AssertExtensions.Throws<ArgumentException>(
                        "plaintext", () => recipient.Open(validLengthCiphertext, destination.AsSpan()));
                    Assert.Equal(originalDestination, destination);
                }

                Assert.Equal(0, recipient.OpenCalls);
            }
        }

        [Theory]
        [InlineData(HpkeKdf.HKDF_SHA256, 8160)]
        [InlineData(HpkeKdf.HKDF_SHA384, 12240)]
        [InlineData(HpkeKdf.HKDF_SHA512, 16320)]
        [InlineData(HpkeKdf.SHAKE128, 65535)]
        [InlineData(HpkeKdf.SHAKE256, 65535)]
        public static void Recipient_Export(HpkeKdf kdf, int maximumLength)
        {
            HpkeSuite suite = new(HpkeKem.DHKEM_P256_HKDF_SHA256, kdf, HpkeAead.AES_128_GCM);

            using (RecordingHpkeRecipient recipient = new(suite))
            {
                byte[] exporterContext = new byte[65536];
                exporterContext.AsSpan().Fill(0x39);

                foreach (int length in new[] { 0, 1, maximumLength })
                {
                    byte[] expected = new byte[length];
                    expected.AsSpan().Fill(0xE7);
                    Assert.Equal(expected, recipient.Export(exporterContext, length));
                    Assert.Equal(exporterContext, recipient.LastExporterContext);
                    Assert.Equal(expected, recipient.Export(exporterContext.AsSpan(), length));
                    Assert.Equal(exporterContext, recipient.LastExporterContext);

                    byte[] destination = new byte[length + 2];
                    destination.AsSpan().Fill(0xA5);
                    recipient.Export(exporterContext, destination.AsSpan(1, length));
                    AssertExtensions.SequenceEqual(expected.AsSpan(), destination.AsSpan(1, length));
                    Assert.Equal(0xA5, destination[0]);
                    Assert.Equal(0xA5, destination[^1]);
                    Assert.Equal(exporterContext, recipient.LastExporterContext);
                }

                Assert.Equal(9, recipient.ExportCalls);
                Assert.Equal(0, recipient.OpenCalls);
                Assert.Empty(recipient.Export(Array.Empty<byte>(), 0));
                Assert.Empty(recipient.LastExporterContext);
            }
        }

        [Theory]
        [InlineData(HpkeKdf.HKDF_SHA256, 8160)]
        [InlineData(HpkeKdf.HKDF_SHA384, 12240)]
        [InlineData(HpkeKdf.HKDF_SHA512, 16320)]
        [InlineData(HpkeKdf.SHAKE128, 65535)]
        [InlineData(HpkeKdf.SHAKE256, 65535)]
        public static void Recipient_ExportValidation(HpkeKdf kdf, int maximumLength)
        {
            HpkeSuite suite = new(HpkeKem.DHKEM_P256_HKDF_SHA256, kdf, HpkeAead.AES_128_GCM);

            using (RecordingHpkeRecipient recipient = new(suite))
            {
                AssertExtensions.Throws<ArgumentNullException>("exporterContext", () => recipient.Export((byte[])null, 0));

                foreach (int length in new[] { -1, int.MinValue, maximumLength + 1, int.MaxValue })
                {
                    AssertExtensions.Throws<ArgumentOutOfRangeException>(
                        "length", () => recipient.Export(Array.Empty<byte>(), length));
                    AssertExtensions.Throws<ArgumentOutOfRangeException>(
                        "length", () => recipient.Export(ReadOnlySpan<byte>.Empty, length));
                }

                byte[] destination = new byte[maximumLength + 1];
                destination.AsSpan().Fill(0xA5);
                byte[] originalDestination = (byte[])destination.Clone();
                AssertExtensions.Throws<ArgumentException>(
                    "destination", () => recipient.Export(ReadOnlySpan<byte>.Empty, destination.AsSpan()));
                Assert.Equal(originalDestination, destination);
                Assert.Equal(0, recipient.ExportCalls);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void Recipient_CoreFailuresPropagate(bool authenticationFailure)
        {
            HpkeSuite suite = new(HpkeKem.DHKEM_P256_HKDF_SHA256, HpkeKdf.HKDF_SHA256, HpkeAead.AES_128_GCM);

            using (RecordingHpkeRecipient recipient = new(suite)
            {
                ThrowOnCoreCall = true,
                AuthenticationFailure = authenticationFailure,
            })
            {
                Type expectedException = authenticationFailure
                    ? typeof(AuthenticationTagMismatchException)
                    : typeof(CryptographicException);
                byte[] ciphertext = new byte[suite.GetCiphertextLength(1)];
                byte[] plaintext = [0xA5, 0xA5, 0xA5];

                Assert.Throws(expectedException, () => recipient.Open(ciphertext));
                Assert.Throws(expectedException, () => recipient.Open(ciphertext.AsSpan()));
                Assert.Throws(expectedException, () => recipient.Open(ciphertext, plaintext.AsSpan(1, 1)));
                Assert.Equal(new byte[] { 0xA5, 0, 0xA5 }, plaintext);
                Assert.Throws<CryptographicException>(() => recipient.Export(Array.Empty<byte>(), 1));
                Assert.Throws<CryptographicException>(() => recipient.Export(ReadOnlySpan<byte>.Empty, 1));
                Assert.Throws<CryptographicException>(() => recipient.Export(ReadOnlySpan<byte>.Empty, new byte[1].AsSpan()));
                Assert.Equal(3, recipient.OpenCalls);
                Assert.Equal(3, recipient.ExportCalls);
            }
        }

        private sealed class RecordingHpkeRecipient : HpkeRecipient
        {
            internal int OpenCalls { get; private set; }
            internal int ExportCalls { get; private set; }
            internal int DisposeCalls { get; private set; }
            internal byte[] LastCiphertext { get; private set; } = [];
            internal byte[] LastAssociatedData { get; private set; } = [];
            internal byte[] LastExporterContext { get; private set; } = [];
            internal bool ThrowOnCoreCall { get; set; }
            internal bool AuthenticationFailure { get; set; }

            internal RecordingHpkeRecipient(HpkeSuite suite) : base(suite)
            {
            }

            protected override void OpenCore(
                ReadOnlySpan<byte> ciphertext,
                Span<byte> plaintext,
                ReadOnlySpan<byte> associatedData)
            {
                OpenCalls++;
                LastCiphertext = ciphertext.ToArray();
                LastAssociatedData = associatedData.ToArray();
                plaintext.Fill(0xD3);

                if (ThrowOnCoreCall)
                {
                    plaintext.Clear();

                    if (AuthenticationFailure)
                    {
                        throw new AuthenticationTagMismatchException("Recipient test authentication failure.");
                    }

                    throw new CryptographicException("Recipient test failure.");
                }
            }

            protected override void ExportCore(ReadOnlySpan<byte> exporterContext, Span<byte> destination)
            {
                ExportCalls++;
                LastExporterContext = exporterContext.ToArray();
                destination.Fill(0xE7);

                if (ThrowOnCoreCall)
                {
                    throw new CryptographicException("Recipient test failure.");
                }
            }

            protected override void Dispose(bool disposing)
            {
                Assert.True(disposing);
                DisposeCalls++;
                base.Dispose(disposing);
            }
        }

        private sealed class RecordingHpkeSender : HpkeSender
        {
            internal int SealCalls { get; private set; }
            internal int ExportCalls { get; private set; }
            internal int DisposeCalls { get; private set; }
            internal byte[] LastPlaintext { get; private set; } = [];
            internal byte[] LastAssociatedData { get; private set; } = [];
            internal byte[] LastExporterContext { get; private set; } = [];
            internal bool ThrowOnCoreCall { get; set; }

            internal RecordingHpkeSender(HpkeSuite suite) : base(suite)
            {
            }

            protected override void SealCore(
                ReadOnlySpan<byte> plaintext,
                Span<byte> ciphertext,
                ReadOnlySpan<byte> associatedData)
            {
                SealCalls++;
                LastPlaintext = plaintext.ToArray();
                LastAssociatedData = associatedData.ToArray();
                ciphertext.Fill(0xD3);

                if (ThrowOnCoreCall)
                {
                    throw new CryptographicException("Sender test failure.");
                }
            }

            protected override void ExportCore(ReadOnlySpan<byte> exporterContext, Span<byte> destination)
            {
                ExportCalls++;
                LastExporterContext = exporterContext.ToArray();
                destination.Fill(0xE7);

                if (ThrowOnCoreCall)
                {
                    throw new CryptographicException("Sender test failure.");
                }
            }

            protected override void Dispose(bool disposing)
            {
                Assert.True(disposing);
                DisposeCalls++;
                base.Dispose(disposing);
            }
        }
    }
}
