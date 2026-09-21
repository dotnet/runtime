// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    [ConditionalClass(typeof(PlatformDetection),
        nameof(PlatformDetection.IsNotBrowser),
        nameof(PlatformDetection.IsNotWasi),
        nameof(PlatformDetection.IsNotNetFramework))]
    public static class HpkeImplementationTests
    {
        public static IEnumerable<object[]> SupportedVectorNames
        {
            get
            {
                foreach (HpkeTestVector vector in HpkeTestData.Vectors)
                {
                    if (Hpke.IsSupported(Suite(vector)))
                    {
                        yield return [vector.Name];
                    }
                }
            }
        }

        public static IEnumerable<object[]> BaseVectorNames
        {
            get
            {
                foreach (HpkeTestVector vector in HpkeTestData.Vectors)
                {
                    if (!vector.UsePsk && Hpke.IsSupported(Suite(vector)))
                    {
                        yield return [vector.Name];
                    }
                }
            }
        }

        public static IEnumerable<object[]> SupportedSuites
        {
            get
            {
                foreach (HpkeKem kem in Enum.GetValues(typeof(HpkeKem)))
                foreach (HpkeKdf kdf in Enum.GetValues(typeof(HpkeKdf)))
                foreach (HpkeAead aead in Enum.GetValues(typeof(HpkeAead)))
                {
                    if (Hpke.IsSupported(new HpkeSuite(kem, kdf, aead)))
                    {
                        yield return [kem, kdf, aead];
                    }
                }
            }
        }

        public static IEnumerable<object[]> RepresentativeSuiteModes
        {
            get
            {
                foreach (HpkeSuite suite in HpkeTestData.Vectors.Select(Suite).Distinct().Where(Hpke.IsSupported))
                foreach (bool usePsk in new[] { false, true })
                {
                    yield return [suite.KemAlgorithm, suite.KdfAlgorithm, suite.AeadAlgorithm, usePsk];
                }
            }
        }

        public static IEnumerable<object[]> InvalidEncapsulatedSecrets
        {
            get
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
                    if (Hpke.IsSupported(new HpkeSuite(kem, HpkeKdf.HKDF_SHA256, HpkeAead.AES_128_GCM)))
                    {
                        byte[] prefixes = kem == HpkeKem.DHKEM_X25519_HKDF_SHA256 ? [0, 1] : [0, 4];

                        foreach (byte prefix in prefixes)
                        {
                            yield return [kem, prefix];
                        }
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(BaseVectorNames))]
        public static void Open_KnownAnswer(string name)
        {
            HpkeTestVector vector = HpkeTestData.GetVector(name);
            HpkeMessageVector message = vector.Messages[0];
            byte[] enc = vector.EncapsulatedSecret.HexToByteArray();
            byte[] info = vector.Info.HexToByteArray();
            byte[] ciphertext = message.Ciphertext.HexToByteArray();
            byte[] plaintext = message.Plaintext.HexToByteArray();
            byte[] aad = message.AssociatedData.HexToByteArray();

            using (Hpke key = Hpke.ImportDecapsulationKey(Suite(vector), vector.DecapsulationKey.HexToByteArray()))
            {
                byte[] destination = GuardedBuffer(plaintext.Length);
                key.Open(enc, ciphertext, destination.AsSpan(1, plaintext.Length), aad, info);
                AssertGuardedOutput(plaintext, destination);
            }
        }

        [Theory]
        [MemberData(nameof(SupportedVectorNames))]
        public static void Recipient_KnownAnswer(string name)
        {
            HpkeTestVector vector = HpkeTestData.GetVector(name);
            byte[] enc = vector.EncapsulatedSecret.HexToByteArray();

            using (Hpke key = Hpke.ImportDecapsulationKey(Suite(vector), vector.DecapsulationKey.HexToByteArray()))
            using (HpkeRecipient recipient = CreateRecipient(key, vector, enc))
            {
                AssertKnownExports(recipient, vector.Exports);

                foreach (HpkeMessageVector message in vector.Messages)
                {
                    byte[] plaintext = message.Plaintext.HexToByteArray();
                    byte[] ciphertext = message.Ciphertext.HexToByteArray();
                    byte[] aad = message.AssociatedData.HexToByteArray();

                    byte[] destination = GuardedBuffer(plaintext.Length);
                    recipient.Open(ciphertext, destination.AsSpan(1, plaintext.Length), aad);
                    AssertGuardedOutput(plaintext, destination);
                }

                AssertKnownExports(recipient, vector.Exports);
            }
        }

        [Theory]
        [MemberData(nameof(SupportedSuites))]
        public static void SingleShot_Roundtrip(HpkeKem kem, HpkeKdf kdf, HpkeAead aead)
        {
            HpkeSuite suite = new(kem, kdf, aead);
            byte[] info = new byte[1024];
            info.AsSpan().Fill(0x3C);

            using (Hpke privateKey = Hpke.GenerateKey(suite))
            using (Hpke publicKey = Hpke.ImportEncapsulationKey(suite, privateKey.ExportEncapsulationKey()))
            {
                foreach (int length in new[] { 0, 1, 257 })
                {
                    AssertSingleShotRoundtrip(
                        privateKey, publicKey, length, "associated data"u8, info);
                }
            }
        }

        [Theory]
        [MemberData(nameof(SupportedSuites))]
        public static void SingleShot_Roundtrip_EmptyAadAndInfo(HpkeKem kem, HpkeKdf kdf, HpkeAead aead)
        {
            HpkeSuite suite = new(kem, kdf, aead);

            using (Hpke privateKey = Hpke.GenerateKey(suite))
            using (Hpke publicKey = Hpke.ImportEncapsulationKey(suite, privateKey.ExportEncapsulationKey()))
            {
                AssertSingleShotRoundtrip(
                    privateKey, publicKey, plaintextLength: 32, ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty);
            }
        }

        [Theory]
        [MemberData(nameof(SupportedVectorNames))]
        public static void Contexts_Roundtrip(string name)
        {
            HpkeTestVector vector = HpkeTestData.GetVector(name);
            HpkeSuite suite = Suite(vector);
            byte[] info = vector.Info.HexToByteArray();
            byte[] psk = vector.Psk.HexToByteArray();
            byte[] pskId = vector.PskId.HexToByteArray();
            byte[] encBuffer = GuardedBuffer(suite.EncapsulatedSecretSizeInBytes);

            using (Hpke privateKey = Hpke.ImportDecapsulationKey(suite, vector.DecapsulationKey.HexToByteArray()))
            using (Hpke publicKey = Hpke.ImportEncapsulationKey(suite, vector.EncapsulationKey.HexToByteArray()))
            using (HpkeSender sender = CreateSender(publicKey, vector.UsePsk, psk, pskId,
                encBuffer.AsSpan(1, suite.EncapsulatedSecretSizeInBytes), info))
            {
                AssertGuards(encBuffer);
                byte[] enc = encBuffer.AsSpan(1, suite.EncapsulatedSecretSizeInBytes).ToArray();

                using (HpkeRecipient recipient = CreateRecipient(
                    privateKey, vector.UsePsk, enc.AsSpan(), psk, pskId, info))
                {
                    for (int sequence = 0; sequence < vector.Messages.Count; sequence++)
                    {
                        HpkeMessageVector message = vector.Messages[sequence];
                        byte[] plaintext = message.Plaintext.HexToByteArray();
                        byte[] aad = message.AssociatedData.HexToByteArray();
                        byte[] ciphertextBuffer = GuardedBuffer(suite.GetCiphertextLength(plaintext.Length));
                        sender.Seal(plaintext, ciphertextBuffer.AsSpan(1, ciphertextBuffer.Length - 2), aad);
                        AssertGuards(ciphertextBuffer);
                        byte[] ciphertext = ciphertextBuffer.AsSpan(1, ciphertextBuffer.Length - 2).ToArray();
                        byte[] destination = GuardedBuffer(plaintext.Length);
                        recipient.Open(ciphertext, destination.AsSpan(1, plaintext.Length), aad);
                        AssertGuardedOutput(plaintext, destination);

                        if (!vector.UsePsk && sequence == 0)
                        {
                            Assert.Equal(plaintext, privateKey.Open(enc, ciphertext,
                                associatedData: aad, info: info));
                        }
                        else if (!vector.UsePsk && sequence == 1)
                        {
                            Assert.Throws<AuthenticationTagMismatchException>(() =>
                                privateKey.Open(enc, ciphertext, associatedData: aad, info: info));
                        }
                    }

                    Assert.Equal(sender.Export(Array.Empty<byte>(), 32), recipient.Export(Array.Empty<byte>(), 32));
                }
            }
        }

        [Theory]
        [MemberData(nameof(BaseVectorNames))]
        public static void Open_AuthenticationFailure_FirstCiphertextByteModified(string name)
        {
            AssertOpenAuthenticationFailure(name, static inputs => inputs.Ciphertext[0] ^= 1);
        }

        [Theory]
        [MemberData(nameof(BaseVectorNames))]
        public static void Open_AuthenticationFailure_LastCiphertextByteModified(string name)
        {
            AssertOpenAuthenticationFailure(
                name, static inputs => inputs.Ciphertext[inputs.Ciphertext.Length - 1] ^= 1);
        }

        [Theory]
        [MemberData(nameof(BaseVectorNames))]
        public static void Open_AuthenticationFailure_DifferentAssociatedData(string name)
        {
            AssertOpenAuthenticationFailure(
                name, static inputs => inputs.AssociatedData = Different(inputs.AssociatedData));
        }

        [Theory]
        [MemberData(nameof(BaseVectorNames))]
        public static void Open_AuthenticationFailure_DifferentInfo(string name)
        {
            AssertOpenAuthenticationFailure(name, static inputs => inputs.Info = Different(inputs.Info));
        }

        [Theory]
        [MemberData(nameof(BaseVectorNames))]
        public static void Open_AuthenticationFailure_DifferentEncapsulatedSecret(string name)
        {
            AssertOpenAuthenticationFailure(
                name, static inputs => inputs.EncapsulatedSecret = inputs.DifferentEncapsulatedSecret);
        }

        [Theory]
        [MemberData(nameof(BaseVectorNames))]
        public static void Open_AuthenticationFailure_DifferentKey(string name)
        {
            AssertOpenAuthenticationFailure(name, static inputs => inputs.Recipient = inputs.WrongKey);
        }

        [Theory]
        [MemberData(nameof(RepresentativeSuiteModes))]
        public static void Recipient_AuthenticationFailureAndOrdering(
            HpkeKem kem, HpkeKdf kdf, HpkeAead aead, bool usePsk)
        {
            HpkeSuite suite = new(kem, kdf, aead);
            ReadOnlySpan<byte> info = "application context"u8;
            byte[] aad = "associated data"u8.ToArray();
            byte[] psk = new byte[32];
            ReadOnlySpan<byte> pskId = "identifier"u8;
            ReadOnlySpan<byte> first = "first"u8;
            ReadOnlySpan<byte> second = "second"u8;
            ReadOnlySpan<byte> third = "third"u8;
            byte[] enc;

            using (Hpke key = Hpke.GenerateKey(suite))
            using (Hpke wrongKey = Hpke.GenerateKey(suite))
            using (HpkeSender sender = CreateSender(key, usePsk, psk, pskId, out enc, info))
            using (HpkeRecipient recipient = CreateRecipient(key, usePsk, enc, psk, pskId, info))
            using (HpkeRecipient badKey = CreateRecipient(wrongKey, usePsk, enc, psk, pskId, info))
            using (HpkeRecipient badInfo = CreateRecipient(key, usePsk, enc, psk, pskId, Different(info)))
            using (HpkeRecipient wrongMode = CreateRecipient(key, !usePsk, enc, psk, pskId, info))
            {
                byte[] firstCiphertext = sender.Seal(first, aad);
                byte[] secondCiphertext = sender.Seal(second, aad);
                byte[] thirdCiphertext = sender.Seal(third, aad);
                byte[] export = recipient.Export(Array.Empty<byte>(), 32);

                foreach (HpkeRecipient incorrect in new[] { badKey, badInfo, wrongMode })
                {
                    AssertAuthenticationFailure(incorrect, firstCiphertext, aad);
                }

                if (usePsk)
                {
                    using (HpkeRecipient badPsk = key.CreatePskRecipient(enc, Different(psk), pskId, info))
                    using (HpkeRecipient badId = key.CreatePskRecipient(enc, psk, Different(pskId), info))
                    {
                        AssertAuthenticationFailure(badPsk, firstCiphertext, aad);
                        AssertAuthenticationFailure(badId, firstCiphertext, aad);
                    }
                }

                byte[] badTag = (byte[])firstCiphertext.Clone();
                badTag[badTag.Length - 1] ^= 1;
                AssertAuthenticationFailure(recipient, Different(firstCiphertext), aad);
                AssertAuthenticationFailure(recipient, badTag, aad);
                AssertAuthenticationFailure(recipient, firstCiphertext, Different(aad));
                Assert.Equal(export, recipient.Export(Array.Empty<byte>(), 32));
                AssertExtensions.SequenceEqual(first, recipient.Open(firstCiphertext, aad));
                AssertAuthenticationFailure(recipient, firstCiphertext, aad);
                AssertAuthenticationFailure(recipient, thirdCiphertext, aad);
                AssertExtensions.SequenceEqual(second, recipient.Open(secondCiphertext.AsSpan(), associatedData: aad));
                byte[] destination = GuardedBuffer(third.Length);
                recipient.Open(thirdCiphertext, destination.AsSpan(1, third.Length), aad);
                AssertGuardedOutput(third, destination);
            }
        }

        [Theory]
        [MemberData(nameof(RepresentativeSuiteModes))]
        public static void Contexts_IndependentLifetime(HpkeKem kem, HpkeKdf kdf, HpkeAead aead, bool usePsk)
        {
            HpkeSuite suite = new(kem, kdf, aead);
            byte[] info = "application context"u8.ToArray();
            byte[] psk = new byte[32];
            psk.AsSpan().Fill(0x3C);
            byte[] pskId = "identifier"u8.ToArray();
            ReadOnlySpan<byte> message = "message"u8;
            byte[] firstEnc;
            byte[] secondEnc;
            Hpke key = Hpke.GenerateKey(suite);
            HpkeSender first = CreateSender(key, usePsk, psk, pskId, out firstEnc, info);
            HpkeRecipient firstRecipient = CreateRecipient(key, usePsk, firstEnc, psk, pskId, info);
            HpkeSender second = CreateSender(key, usePsk, psk, pskId, out secondEnc, info);

            using (HpkeRecipient secondRecipient = CreateRecipient(key, usePsk, secondEnc, psk, pskId, info))
            {
                byte[] firstExport = first.Export(Array.Empty<byte>(), 32);
                byte[] secondExport = second.Export(Array.Empty<byte>(), 32);
                key.Dispose();
                info.AsSpan().Clear();
                psk.AsSpan().Clear();
                pskId.AsSpan().Clear();
                firstEnc.AsSpan().Clear();
                secondEnc.AsSpan().Clear();

                AssertExtensions.SequenceEqual(message, firstRecipient.Open(first.Seal(message)));
                AssertExtensions.SequenceEqual(message, firstRecipient.Open(first.Seal(message)));
                Assert.Equal(firstExport, first.Export(Array.Empty<byte>(), 32));
                Assert.Equal(firstExport, firstRecipient.Export(Array.Empty<byte>(), 32));
                first.Dispose();
                firstRecipient.Dispose();

                AssertExtensions.SequenceEqual(message, secondRecipient.Open(second.Seal(message)));
                Assert.Equal(secondExport, second.Export(Array.Empty<byte>(), 32));
                second.Dispose();
                Assert.Equal(secondExport, secondRecipient.Export(Array.Empty<byte>(), 32));
            }
        }

        [Theory]
        [MemberData(nameof(RepresentativeSuiteModes))]
        public static void Contexts_Export(HpkeKem kem, HpkeKdf kdf, HpkeAead aead, bool usePsk)
        {
            HpkeSuite suite = new(kem, kdf, aead);
            int maximumLength = (int)HpkeTestData.ExportLimits.Single(row => row[0].Equals(kdf))[1];
            ReadOnlySpan<byte> info = "application context"u8;
            byte[] psk = new byte[32];
            ReadOnlySpan<byte> pskId = "identifier"u8;
            ReadOnlySpan<byte> context = "exporter context"u8;
            byte[] enc;

            using (Hpke key = Hpke.GenerateKey(suite))
            using (HpkeSender sender = CreateSender(key, usePsk, psk, pskId, out enc, info))
            using (HpkeRecipient recipient = CreateRecipient(key, usePsk, enc, psk, pskId, info))
            {
                byte[] reference = sender.Export(context, 32);

                foreach (int length in new[] { 0, 1, 31, 32, 33, 65, maximumLength })
                {
                    byte[] expected = sender.Export(context, length);
                    Assert.Equal(length, expected.Length);
                    Assert.Equal(expected, recipient.Export(context, length));
                }

                Assert.NotEqual(reference, sender.Export(Array.Empty<byte>(), 32));
                Assert.NotEqual(reference, sender.Export([0], 32));
                Assert.False(reference.AsSpan().SequenceEqual(sender.Export(context, 33).AsSpan(0, 32)));

                foreach (int contextLength in new[] { 200, 300 }) // 300 pushes past the 256-byte stack buffer.
                {
                    byte[] longContext = new byte[contextLength];
                    longContext.AsSpan().Fill(0x39);
                    Assert.Equal(sender.Export(longContext, 32), recipient.Export(longContext, 32));
                }

                ReadOnlySpan<byte> message = "message"u8;

                for (int i = 0; i < 3; i++)
                {
                    byte[] ciphertext = sender.Seal(message);
                    Assert.Equal(reference, sender.Export(context, 32));
                    Assert.Equal(reference, recipient.Export(context, 32));
                    AssertExtensions.SequenceEqual(message, recipient.Open(ciphertext));
                    Assert.Equal(reference, recipient.Export(context, 32));
                }
            }
        }

        [Theory]
        [MemberData(nameof(InvalidEncapsulatedSecrets))]
        public static void InvalidEncapsulation_Rejected(HpkeKem kem, byte prefix)
        {
            HpkeSuite suite = new(kem, HpkeKdf.HKDF_SHA256, HpkeAead.AES_128_GCM);
            byte[] enc = new byte[suite.EncapsulatedSecretSizeInBytes];
            enc[0] = prefix;
            byte[] ciphertext = new byte[suite.AeadTagSizeInBytes];
            byte[] psk = new byte[32];
            byte[] pskId = [1];

            using (Hpke key = Hpke.GenerateKey(suite))
            {
                Assert.ThrowsAny<CryptographicException>(() => key.Open(enc, ciphertext));
                Assert.ThrowsAny<CryptographicException>(() => key.Open(enc.AsSpan(), ciphertext));
                Assert.ThrowsAny<CryptographicException>(() => key.Open(enc, ciphertext, Span<byte>.Empty));
                Assert.ThrowsAny<CryptographicException>(() => key.CreateRecipient(enc));
                Assert.ThrowsAny<CryptographicException>(() => key.CreateRecipient(enc.AsSpan()));
                Assert.ThrowsAny<CryptographicException>(() => key.CreatePskRecipient(enc, psk, pskId));
                Assert.ThrowsAny<CryptographicException>(() => key.CreatePskRecipient(enc.AsSpan(), psk, pskId));
            }
        }

        private static HpkeSuite Suite(HpkeTestVector vector) => new(vector.Kem, vector.Kdf, vector.Aead);

        private static void AssertOpenAuthenticationFailure(string name, Action<OpenFailureInputs> tamper)
        {
            HpkeTestVector vector = HpkeTestData.GetVector(name);
            HpkeSuite suite = Suite(vector);
            ReadOnlySpan<byte> plaintext = "plaintext"u8;
            byte[] associatedData = "associated data"u8.ToArray();
            byte[] info = vector.Info.HexToByteArray();
            int plaintextLength = plaintext.Length;

            using (Hpke key = Hpke.ImportDecapsulationKey(suite, vector.DecapsulationKey.HexToByteArray()))
            using (Hpke wrongKey = Hpke.GenerateKey(suite))
            using (HpkeSender unrelated = key.CreateSender(out byte[] differentEncapsulatedSecret, info))
            {
                key.Seal(plaintext, out byte[] encapsulatedSecret, out byte[] ciphertext, associatedData, info);
                OpenFailureInputs inputs = new(
                    key,
                    wrongKey,
                    encapsulatedSecret,
                    differentEncapsulatedSecret,
                    ciphertext,
                    associatedData,
                    info);
                tamper(inputs);

                Assert.Throws<AuthenticationTagMismatchException>(() =>
                    inputs.Recipient.Open(
                        inputs.EncapsulatedSecret,
                        inputs.Ciphertext,
                        associatedData: inputs.AssociatedData,
                        info: inputs.Info));
                Assert.Throws<AuthenticationTagMismatchException>(() =>
                    inputs.Recipient.Open(
                        inputs.EncapsulatedSecret.AsSpan(),
                        inputs.Ciphertext,
                        associatedData: inputs.AssociatedData,
                        info: inputs.Info));
                byte[] destination = GuardedBuffer(plaintextLength);
                Assert.Throws<AuthenticationTagMismatchException>(() =>
                    inputs.Recipient.Open(
                        inputs.EncapsulatedSecret,
                        inputs.Ciphertext,
                        destination.AsSpan(1, plaintextLength),
                        inputs.AssociatedData,
                        inputs.Info));
                AssertGuardedOutput(new byte[plaintextLength], destination);
                AssertExtensions.SequenceEqual(
                    plaintext,
                    key.Open(
                        encapsulatedSecret,
                        ciphertext,
                        associatedData: associatedData,
                        info: info));
            }
        }

        private static void AssertSingleShotRoundtrip(
            Hpke privateKey,
            Hpke publicKey,
            int plaintextLength,
            ReadOnlySpan<byte> aad,
            ReadOnlySpan<byte> info)
        {
            byte[] plaintext = new byte[plaintextLength];
            plaintext.AsSpan().Fill(0xA7);
            byte[] encBuffer = GuardedBuffer(publicKey.Suite.EncapsulatedSecretSizeInBytes);
            byte[] ciphertextBuffer = GuardedBuffer(publicKey.Suite.GetCiphertextLength(plaintextLength));

            publicKey.Seal(plaintext, encBuffer.AsSpan(1, encBuffer.Length - 2),
                ciphertextBuffer.AsSpan(1, ciphertextBuffer.Length - 2), aad, info);
            AssertGuards(encBuffer);
            AssertGuards(ciphertextBuffer);
            byte[] enc = encBuffer.AsSpan(1, encBuffer.Length - 2).ToArray();
            byte[] ciphertext = ciphertextBuffer.AsSpan(1, ciphertextBuffer.Length - 2).ToArray();
            byte[] destination = GuardedBuffer(plaintextLength);
            privateKey.Open(enc, ciphertext, destination.AsSpan(1, plaintextLength), aad, info);
            AssertGuardedOutput(plaintext, destination);

            using (HpkeRecipient recipient = privateKey.CreateRecipient(enc.AsSpan(), info))
            {
                destination.AsSpan().Fill(0xA5);
                recipient.Open(ciphertext, destination.AsSpan(1, plaintextLength), aad);
                AssertGuardedOutput(plaintext, destination);
            }
        }

        private static HpkeSender CreateSender(
            Hpke key,
            bool usePsk,
            ReadOnlySpan<byte> psk,
            ReadOnlySpan<byte> pskId,
            out byte[] enc,
            ReadOnlySpan<byte> info)
        {
            return usePsk
                ? key.CreatePskSender(psk, pskId, out enc, info)
                : key.CreateSender(out enc, info);
        }

        private static HpkeSender CreateSender(
            Hpke key,
            bool usePsk,
            ReadOnlySpan<byte> psk,
            ReadOnlySpan<byte> pskId,
            Span<byte> enc,
            ReadOnlySpan<byte> info)
        {
            return usePsk
                ? key.CreatePskSender(psk, pskId, enc, info)
                : key.CreateSender(enc, info);
        }

        private static HpkeRecipient CreateRecipient(
            Hpke key,
            bool usePsk,
            ReadOnlySpan<byte> enc,
            ReadOnlySpan<byte> psk,
            ReadOnlySpan<byte> pskId,
            ReadOnlySpan<byte> info)
        {
            return usePsk
                ? key.CreatePskRecipient(enc, psk, pskId, info)
                : key.CreateRecipient(enc, info);
        }

        private static HpkeRecipient CreateRecipient(Hpke key, HpkeTestVector vector, ReadOnlySpan<byte> enc)
        {
            byte[] info = vector.Info.HexToByteArray();

            if (vector.UsePsk)
            {
                byte[] psk = vector.Psk.HexToByteArray();
                byte[] pskId = vector.PskId.HexToByteArray();
                return key.CreatePskRecipient(enc, psk, pskId, info);
            }

            return key.CreateRecipient(enc, info);
        }

        private static void AssertKnownExports(HpkeRecipient recipient, IReadOnlyList<HpkeExportVector> exports)
        {
            foreach (HpkeExportVector export in exports)
            {
                byte[] context = export.Context.HexToByteArray();
                byte[] expected = export.ExportedValue.HexToByteArray();
                byte[] destination = GuardedBuffer(export.Length);
                recipient.Export(context, destination.AsSpan(1, export.Length));
                AssertGuardedOutput(expected, destination);
            }
        }

        private static void AssertAuthenticationFailure(HpkeRecipient recipient, byte[] ciphertext, byte[] aad)
        {
            Assert.Throws<AuthenticationTagMismatchException>(() => recipient.Open(ciphertext, aad));
            Assert.Throws<AuthenticationTagMismatchException>(() =>
                recipient.Open(ciphertext.AsSpan(), associatedData: aad));
            int length = ciphertext.Length - recipient.Suite.AeadTagSizeInBytes;
            byte[] destination = GuardedBuffer(length);
            Assert.Throws<AuthenticationTagMismatchException>(() =>
                recipient.Open(ciphertext, destination.AsSpan(1, length), aad));
            AssertGuardedOutput(new byte[length], destination);
        }

        private static byte[] Different(ReadOnlySpan<byte> input)
        {
            byte[] result = input.IsEmpty ? [1] : input.ToArray();
            result[0] ^= 0x80;
            return result;
        }

        private sealed class OpenFailureInputs
        {
            internal Hpke Recipient { get; set; }
            internal Hpke WrongKey { get; }
            internal byte[] EncapsulatedSecret { get; set; }
            internal byte[] DifferentEncapsulatedSecret { get; }
            internal byte[] Ciphertext { get; }
            internal byte[] AssociatedData { get; set; }
            internal byte[] Info { get; set; }

            internal OpenFailureInputs(
                Hpke recipient,
                Hpke wrongKey,
                byte[] encapsulatedSecret,
                byte[] differentEncapsulatedSecret,
                byte[] ciphertext,
                byte[] associatedData,
                byte[] info)
            {
                Recipient = recipient;
                WrongKey = wrongKey;
                EncapsulatedSecret = encapsulatedSecret;
                DifferentEncapsulatedSecret = differentEncapsulatedSecret;
                Ciphertext = (byte[])ciphertext.Clone();
                AssociatedData = associatedData;
                Info = info;
            }
        }

        private static byte[] GuardedBuffer(int length)
        {
            byte[] buffer = new byte[length + 2];
            buffer.AsSpan().Fill(0xA5);
            return buffer;
        }

        private static void AssertGuardedOutput(ReadOnlySpan<byte> expected, byte[] buffer)
        {
            AssertExtensions.SequenceEqual(expected, buffer.AsSpan(1, buffer.Length - 2));
            AssertGuards(buffer);
        }

        private static void AssertGuards(byte[] buffer)
        {
            Assert.Equal(0xA5, buffer[0]);
            Assert.Equal(0xA5, buffer[buffer.Length - 1]);
        }
    }
}
