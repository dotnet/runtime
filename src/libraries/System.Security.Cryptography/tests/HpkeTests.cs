// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    public static class HpkeTests
    {
        [Fact]
        public static void KeyFactories_NotSupported()
        {
            foreach (HpkeKem kem in Enum.GetValues<HpkeKem>())
            {
                HpkeSuite suite = new(kem, HpkeKdf.HKDF_SHA256, HpkeAead.AES_128_GCM);

                if (!Hpke.IsSupported(suite))
                {
                    byte[] privateKey = new byte[suite.DecapsulationKeySizeInBytes];
                    byte[] publicKey = new byte[suite.EncapsulationKeySizeInBytes];
                    Assert.Throws<PlatformNotSupportedException>(() => Hpke.GenerateKey(suite));
                    Assert.Throws<PlatformNotSupportedException>(() => Hpke.DeriveKey(suite, privateKey));
                    Assert.Throws<PlatformNotSupportedException>(() => Hpke.DeriveKey(suite, privateKey.AsSpan()));
                    Assert.Throws<PlatformNotSupportedException>(() => Hpke.ImportDecapsulationKey(suite, privateKey));
                    Assert.Throws<PlatformNotSupportedException>(
                        () => Hpke.ImportDecapsulationKey(suite, privateKey.AsSpan()));
                    Assert.Throws<PlatformNotSupportedException>(() => Hpke.ImportEncapsulationKey(suite, publicKey));
                    Assert.Throws<PlatformNotSupportedException>(
                        () => Hpke.ImportEncapsulationKey(suite, publicKey.AsSpan()));
                }
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
                byte[] secondCiphertext = second.Seal(plaintext);
                Assert.Equal(plaintext, peer.Open(secondEnc, secondCiphertext));
                Assert.Equal(plaintext, secondRecipient.Open(secondCiphertext));
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
                        // HKDF export framing adds 22 bytes; the first two cases straddle the 256-byte stack limit.
                        foreach (int contextLength in new[]
                        {
                            234, 235, HpkeTestData.MaxExporterContextLength
                        })
                        {
                            byte[] longContext = new byte[contextLength];
                            longContext.AsSpan().Fill(0x39);
                            Assert.Equal(sender.Export(longContext, 32), recipient.Export(longContext, 32));
                        }

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
                        Assert.Equal(referenceExport, recipient.Export(context, 32));
                    }
                }
            }
        }
    }
}
