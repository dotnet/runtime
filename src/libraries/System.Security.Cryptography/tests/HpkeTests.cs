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

        // https://github.com/cfrg/draft-irtf-cfrg-hpke/blob/b1f7cb0cdeab6906c61b3d6574e8bdfdbe1cd3fb/test-vectors.json
        [Theory]
        [InlineData(
            HpkeKem.DHKEM_P256_HKDF_SHA256, HpkeKdf.HKDF_SHA256, HpkeAead.AES_128_GCM,
            "668b37171f1072f3cf12ea8a236a45df23fc13b82af3609ad1e354f6ef817550",
            "04a92719c6195d5085104f469a8b9814d5838ff72b60501e2c4466e5e67b325a" +
            "c98536d7b61a1af4b78e5b7f951c0900be863c403ce65c9bfcb9382657222d18c4",
            "5ad590bb8baa577f8619db35a36311226a896e7342a6d836d8b7bcd2f20b6c7f9076ac232e3ab2523f39513434")]
        [InlineData(
            HpkeKem.DHKEM_P256_HKDF_SHA256, HpkeKdf.HKDF_SHA512, HpkeAead.AES_256_GCM,
            "a2f6e7c4d9e108e03be268a64fe73e11a320963c85375a30bfc9ec4a214c6a55",
            "0404dc39344526dbfa728afba96986d575811b5af199c11f821a0e603a4d191b2554" +
            "4a402f25364964b2c129cb417b3c1dab4dfc0854f3084e843f731654392726",
            "949f58e87c39b3f55390b6a970de27dfac44aadc2fbc9d623dcde1a08b628c83ad07dbbee6aede7fcfbf955670")]
        [InlineData(
            HpkeKem.DHKEM_X25519_HKDF_SHA256, HpkeKdf.HKDF_SHA512, HpkeAead.ChaCha20Poly1305,
            "969bb169aa9c24a501ee9d962e96c310226d427fb6eb3fc579d9882dbc708315",
            "1d38fc578d4209ea0ef3ee5f1128ac4876a9549d74dc2d2f46e75942a6188244",
            "72da9627fd7eb3a8b7169c6d97419b80adefca751c6b52b39a2e084d35ce3eb4487aadaca5a9c590e0938c48b9")]
        public static void Open_KnownAnswer(
            HpkeKem kem, HpkeKdf kdf, HpkeAead aead, string ikmHex, string encHex, string ciphertextHex)
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
            }
        }

        [Theory]
        [InlineData(HpkeKem.DHKEM_P256_HKDF_SHA256)]
        [InlineData(HpkeKem.DHKEM_P384_HKDF_SHA384)]
        [InlineData(HpkeKem.DHKEM_X25519_HKDF_SHA256)]
        public static void Open_ArgumentValidation(HpkeKem kem)
        {
            HpkeSuite suite = new(kem, HpkeKdf.HKDF_SHA256, HpkeAead.AES_128_GCM);

            using (OpenValidationHpke key = new(suite))
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

            using (OpenValidationHpke key = new(suite))
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

        private sealed class OpenValidationHpke : Hpke
        {
            internal bool OpenCoreCalled { get; private set; }

            internal OpenValidationHpke(HpkeSuite suite) : base(suite)
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

            protected override void ExportDecapsulationKeyCore(Span<byte> destination) =>
                throw new InvalidOperationException("Unexpected key export.");

            protected override void ExportEncapsulationKeyCore(Span<byte> destination) =>
                throw new InvalidOperationException("Unexpected key export.");

            protected override void SealCore(
                ReadOnlySpan<byte> plaintext,
                Span<byte> encapsulatedSecret,
                Span<byte> ciphertext,
                ReadOnlySpan<byte> associatedData,
                ReadOnlySpan<byte> info) =>
                throw new InvalidOperationException("Unexpected encryption.");
        }

        [Theory]
        [InlineData(HpkeKem.DHKEM_P256_HKDF_SHA256)]
        [InlineData(HpkeKem.DHKEM_P384_HKDF_SHA384)]
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
    }
}
