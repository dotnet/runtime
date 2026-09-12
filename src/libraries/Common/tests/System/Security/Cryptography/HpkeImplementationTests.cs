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
                Assert.Equal(plaintext, key.Open(enc, ciphertext, associatedData: aad, info: info));
                Assert.Equal(plaintext, key.Open(enc.AsSpan(), ciphertext, associatedData: aad, info: info));
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
            using (HpkeRecipient fromArray = CreateRecipient(key, vector, enc, useSpan: false))
            using (HpkeRecipient fromSpan = CreateRecipient(key, vector, enc, useSpan: true))
            using (HpkeRecipient toDestination = CreateRecipient(key, vector, enc, useSpan: true))
            {
                AssertKnownExports(fromArray, vector.Exports);

                foreach (HpkeMessageVector message in vector.Messages)
                {
                    byte[] plaintext = message.Plaintext.HexToByteArray();
                    byte[] ciphertext = message.Ciphertext.HexToByteArray();
                    byte[] aad = message.AssociatedData.HexToByteArray();

                    Assert.Equal(plaintext, fromArray.Open(ciphertext, aad));
                    Assert.Equal(plaintext, fromSpan.Open(ciphertext.AsSpan(), associatedData: aad));
                    byte[] destination = GuardedBuffer(plaintext.Length);
                    toDestination.Open(ciphertext, destination.AsSpan(1, plaintext.Length), aad);
                    AssertGuardedOutput(plaintext, destination);
                }

                AssertKnownExports(fromArray, vector.Exports);
                AssertKnownExports(fromSpan, vector.Exports);
                AssertKnownExports(toDestination, vector.Exports);
            }
        }

        [Theory]
        [MemberData(nameof(SupportedSuites))]
        public static void SingleShot_Roundtrip_Array(HpkeKem kem, HpkeKdf kdf, HpkeAead aead)
        {
            HpkeSuite suite = new(kem, kdf, aead);

            using (Hpke privateKey = Hpke.GenerateKey(suite))
            using (Hpke publicKey = Hpke.ImportEncapsulationKey(suite, privateKey.ExportEncapsulationKey()))
            {
                foreach (int length in new[] { 0, 1, 257 })
                {
                    byte[] plaintext = new byte[length];
                    plaintext.AsSpan().Fill(0xA7);
                    byte[] aad = length == 0 ? [] : "associated data"u8.ToArray();
                    byte[] info = new byte[length == 0 ? 0 : 1024];
                    info.AsSpan().Fill(0x3C);

                    publicKey.Seal(plaintext, out byte[] enc, out byte[] ciphertext,
                        length == 0 ? null : aad, length == 0 ? null : info);
                    Assert.Equal(plaintext, privateKey.Open(enc, ciphertext,
                        associatedData: length == 0 ? null : aad, info: length == 0 ? null : info));
                    Assert.Equal(plaintext, privateKey.Open(enc.AsSpan(), ciphertext,
                        associatedData: aad, info: info));
                    byte[] destination = GuardedBuffer(length);
                    privateKey.Open(enc, ciphertext, destination.AsSpan(1, length), aad, info);
                    AssertGuardedOutput(plaintext, destination);

                    using (HpkeRecipient recipient = privateKey.CreateRecipient(enc, info))
                    {
                        Assert.Equal(plaintext, recipient.Open(ciphertext, aad));
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(SupportedSuites))]
        public static void SingleShot_Roundtrip_Span(HpkeKem kem, HpkeKdf kdf, HpkeAead aead)
        {
            HpkeSuite suite = new(kem, kdf, aead);

            using (Hpke privateKey = Hpke.GenerateKey(suite))
            using (Hpke publicKey = Hpke.ImportEncapsulationKey(suite, privateKey.ExportEncapsulationKey()))
            {
                foreach (int length in new[] { 0, 1, 257 })
                {
                    byte[] plaintext = new byte[length];
                    plaintext.AsSpan().Fill(0xA7);
                    byte[] aad = length == 0 ? [] : "associated data"u8.ToArray();
                    byte[] info = new byte[length == 0 ? 0 : 1024];
                    info.AsSpan().Fill(0x3C);

                    publicKey.Seal(plaintext.AsSpan(), out byte[] enc, out byte[] ciphertext, aad, info);
                    Assert.Equal(plaintext, privateKey.Open(enc, ciphertext,
                        associatedData: length == 0 ? null : aad, info: length == 0 ? null : info));
                    Assert.Equal(plaintext, privateKey.Open(enc.AsSpan(), ciphertext,
                        associatedData: aad, info: info));
                    byte[] destination = GuardedBuffer(length);
                    privateKey.Open(enc, ciphertext, destination.AsSpan(1, length), aad, info);
                    AssertGuardedOutput(plaintext, destination);

                    using (HpkeRecipient recipient = privateKey.CreateRecipient(enc, info))
                    {
                        Assert.Equal(plaintext, recipient.Open(ciphertext, aad));
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(SupportedSuites))]
        public static void SingleShot_Roundtrip_Destination(HpkeKem kem, HpkeKdf kdf, HpkeAead aead)
        {
            HpkeSuite suite = new(kem, kdf, aead);

            using (Hpke privateKey = Hpke.GenerateKey(suite))
            using (Hpke publicKey = Hpke.ImportEncapsulationKey(suite, privateKey.ExportEncapsulationKey()))
            {
                foreach (int length in new[] { 0, 1, 257 })
                {
                    byte[] plaintext = new byte[length];
                    plaintext.AsSpan().Fill(0xA7);
                    byte[] aad = length == 0 ? [] : "associated data"u8.ToArray();
                    byte[] info = new byte[length == 0 ? 0 : 1024];
                    info.AsSpan().Fill(0x3C);
                    byte[] encBuffer = GuardedBuffer(suite.EncapsulatedSecretSizeInBytes);
                    byte[] ciphertextBuffer = GuardedBuffer(suite.GetCiphertextLength(length));

                    publicKey.Seal(plaintext, encBuffer.AsSpan(1, encBuffer.Length - 2),
                        ciphertextBuffer.AsSpan(1, ciphertextBuffer.Length - 2), aad, info);
                    AssertGuards(encBuffer);
                    AssertGuards(ciphertextBuffer);
                    byte[] enc = encBuffer.AsSpan(1, encBuffer.Length - 2).ToArray();
                    byte[] ciphertext = ciphertextBuffer.AsSpan(1, ciphertextBuffer.Length - 2).ToArray();
                    Assert.Equal(plaintext, privateKey.Open(enc, ciphertext,
                        associatedData: length == 0 ? null : aad, info: length == 0 ? null : info));
                    Assert.Equal(plaintext, privateKey.Open(enc.AsSpan(), ciphertext,
                        associatedData: aad, info: info));
                    byte[] destination = GuardedBuffer(length);
                    privateKey.Open(enc, ciphertext, destination.AsSpan(1, length), aad, info);
                    AssertGuardedOutput(plaintext, destination);

                    using (HpkeRecipient recipient = privateKey.CreateRecipient(enc, info))
                    {
                        Assert.Equal(plaintext, recipient.Open(ciphertext, aad));
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(SupportedVectorNames))]
        public static void Contexts_Roundtrip_Array(string name)
        {
            HpkeTestVector vector = HpkeTestData.GetVector(name);
            HpkeSuite suite = Suite(vector);
            byte[] info = vector.Info.HexToByteArray();
            byte[] psk = vector.Psk.HexToByteArray();
            byte[] pskId = vector.PskId.HexToByteArray();
            byte[] enc;

            using (Hpke privateKey = Hpke.ImportDecapsulationKey(suite, vector.DecapsulationKey.HexToByteArray()))
            using (Hpke publicKey = Hpke.ImportEncapsulationKey(suite, vector.EncapsulationKey.HexToByteArray()))
            using (HpkeSender sender = vector.UsePsk
                ? publicKey.CreatePskSender(psk, pskId, out enc, info)
                : publicKey.CreateSender(out enc, info))
            using (HpkeRecipient recipient = vector.UsePsk
                ? privateKey.CreatePskRecipient(enc, psk, pskId, info)
                : privateKey.CreateRecipient(enc, info))
            {
                for (int sequence = 0; sequence < vector.Messages.Count; sequence++)
                {
                    HpkeMessageVector message = vector.Messages[sequence];
                    byte[] plaintext = message.Plaintext.HexToByteArray();
                    byte[] aad = message.AssociatedData.HexToByteArray();
                    byte[] ciphertext = sender.Seal(plaintext, aad);
                    Assert.Equal(plaintext, recipient.Open(ciphertext, aad));

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

        [Theory]
        [MemberData(nameof(SupportedVectorNames))]
        public static void Contexts_Roundtrip_Span(string name)
        {
            HpkeTestVector vector = HpkeTestData.GetVector(name);
            HpkeSuite suite = Suite(vector);
            byte[] info = vector.Info.HexToByteArray();
            byte[] psk = vector.Psk.HexToByteArray();
            byte[] pskId = vector.PskId.HexToByteArray();
            byte[] enc;

            using (Hpke privateKey = Hpke.ImportDecapsulationKey(suite, vector.DecapsulationKey.HexToByteArray()))
            using (Hpke publicKey = Hpke.ImportEncapsulationKey(suite, vector.EncapsulationKey.HexToByteArray()))
            using (HpkeSender sender = vector.UsePsk
                ? publicKey.CreatePskSender(psk.AsSpan(), pskId, out enc, info)
                : publicKey.CreateSender(out enc, info))
            using (HpkeRecipient recipient = vector.UsePsk
                ? privateKey.CreatePskRecipient(enc.AsSpan(), psk, pskId, info)
                : privateKey.CreateRecipient(enc.AsSpan(), info))
            {
                for (int sequence = 0; sequence < vector.Messages.Count; sequence++)
                {
                    HpkeMessageVector message = vector.Messages[sequence];
                    byte[] plaintext = message.Plaintext.HexToByteArray();
                    byte[] aad = message.AssociatedData.HexToByteArray();
                    byte[] ciphertext = sender.Seal(plaintext.AsSpan(), associatedData: aad);
                    Assert.Equal(plaintext, recipient.Open(ciphertext.AsSpan(), associatedData: aad));

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

        [Theory]
        [MemberData(nameof(SupportedVectorNames))]
        public static void Contexts_Roundtrip_Destination(string name)
        {
            HpkeTestVector vector = HpkeTestData.GetVector(name);
            HpkeSuite suite = Suite(vector);
            byte[] info = vector.Info.HexToByteArray();
            byte[] psk = vector.Psk.HexToByteArray();
            byte[] pskId = vector.PskId.HexToByteArray();
            byte[] encBuffer = GuardedBuffer(suite.EncapsulatedSecretSizeInBytes);

            using (Hpke privateKey = Hpke.ImportDecapsulationKey(suite, vector.DecapsulationKey.HexToByteArray()))
            using (Hpke publicKey = Hpke.ImportEncapsulationKey(suite, vector.EncapsulationKey.HexToByteArray()))
            using (HpkeSender sender = vector.UsePsk
                ? publicKey.CreatePskSender(psk, pskId,
                    encBuffer.AsSpan(1, suite.EncapsulatedSecretSizeInBytes), info)
                : publicKey.CreateSender(encBuffer.AsSpan(1, suite.EncapsulatedSecretSizeInBytes), info))
            {
                AssertGuards(encBuffer);
                byte[] enc = encBuffer.AsSpan(1, suite.EncapsulatedSecretSizeInBytes).ToArray();

                using (HpkeRecipient recipient = vector.UsePsk
                    ? privateKey.CreatePskRecipient(enc.AsSpan(), psk, pskId, info)
                    : privateKey.CreateRecipient(enc.AsSpan(), info))
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
        public static void Open_AuthenticationFailure(string name)
        {
            HpkeTestVector vector = HpkeTestData.GetVector(name);
            HpkeSuite suite = Suite(vector);
            byte[] plaintext = "plaintext"u8.ToArray();
            byte[] aad = "associated data"u8.ToArray();
            byte[] info = vector.Info.HexToByteArray();

            using (Hpke key = Hpke.ImportDecapsulationKey(suite, vector.DecapsulationKey.HexToByteArray()))
            using (Hpke wrongKey = Hpke.GenerateKey(suite))
            using (HpkeSender unrelated = key.CreateSender(out byte[] differentEnc, info))
            {
                key.Seal(plaintext, out byte[] enc, out byte[] ciphertext, aad, info);

                for (int tamper = 0; tamper < 6; tamper++)
                {
                    Hpke recipient = key;
                    byte[] modifiedEnc = enc;
                    byte[] modifiedCiphertext = (byte[])ciphertext.Clone();
                    byte[] modifiedAad = aad;
                    byte[] modifiedInfo = info;

                    switch (tamper)
                    {
                        case 0:
                            modifiedCiphertext[0] ^= 1;
                            break;
                        case 1:
                            modifiedCiphertext[modifiedCiphertext.Length - 1] ^= 1;
                            break;
                        case 2:
                            modifiedAad = Different(aad);
                            break;
                        case 3:
                            modifiedInfo = Different(info);
                            break;
                        case 4:
                            modifiedEnc = differentEnc;
                            break;
                        case 5:
                            recipient = wrongKey;
                            break;
                    }

                    Assert.Throws<AuthenticationTagMismatchException>(() =>
                        recipient.Open(modifiedEnc, modifiedCiphertext,
                            associatedData: modifiedAad, info: modifiedInfo));
                    Assert.Throws<AuthenticationTagMismatchException>(() =>
                        recipient.Open(modifiedEnc.AsSpan(), modifiedCiphertext,
                            associatedData: modifiedAad, info: modifiedInfo));
                    byte[] destination = GuardedBuffer(plaintext.Length);
                    Assert.Throws<AuthenticationTagMismatchException>(() =>
                        recipient.Open(modifiedEnc, modifiedCiphertext, destination.AsSpan(1, plaintext.Length),
                            modifiedAad, modifiedInfo));
                    AssertGuardedOutput(new byte[plaintext.Length], destination);
                }

                Assert.Equal(plaintext, key.Open(enc, ciphertext, associatedData: aad, info: info));
            }
        }

        [Theory]
        [MemberData(nameof(RepresentativeSuiteModes))]
        public static void Recipient_AuthenticationFailureAndOrdering(
            HpkeKem kem, HpkeKdf kdf, HpkeAead aead, bool usePsk)
        {
            HpkeSuite suite = new(kem, kdf, aead);
            byte[] info = "application context"u8.ToArray();
            byte[] aad = "associated data"u8.ToArray();
            byte[] psk = new byte[32];
            byte[] pskId = "identifier"u8.ToArray();
            byte[] first = "first"u8.ToArray();
            byte[] second = "second"u8.ToArray();
            byte[] third = "third"u8.ToArray();
            byte[] enc;

            using (Hpke key = Hpke.GenerateKey(suite))
            using (Hpke wrongKey = Hpke.GenerateKey(suite))
            using (HpkeSender sender = usePsk
                ? key.CreatePskSender(psk, pskId, out enc, info)
                : key.CreateSender(out enc, info))
            using (HpkeRecipient recipient = usePsk
                ? key.CreatePskRecipient(enc, psk, pskId, info)
                : key.CreateRecipient(enc, info))
            using (HpkeRecipient badKey = usePsk
                ? wrongKey.CreatePskRecipient(enc, psk, pskId, info)
                : wrongKey.CreateRecipient(enc, info))
            using (HpkeRecipient badInfo = usePsk
                ? key.CreatePskRecipient(enc, psk, pskId, Different(info))
                : key.CreateRecipient(enc, Different(info)))
            using (HpkeRecipient wrongMode = usePsk
                ? key.CreateRecipient(enc, info)
                : key.CreatePskRecipient(enc, psk, pskId, info))
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
                Assert.Equal(first, recipient.Open(firstCiphertext, aad));
                AssertAuthenticationFailure(recipient, firstCiphertext, aad);
                AssertAuthenticationFailure(recipient, thirdCiphertext, aad);
                Assert.Equal(second, recipient.Open(secondCiphertext.AsSpan(), associatedData: aad));
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
            byte[] message = "message"u8.ToArray();
            byte[] firstEnc;
            byte[] secondEnc;
            Hpke key = Hpke.GenerateKey(suite);
            HpkeSender first = usePsk
                ? key.CreatePskSender(psk, pskId, out firstEnc, info)
                : key.CreateSender(out firstEnc, info);
            HpkeRecipient firstRecipient = usePsk
                ? key.CreatePskRecipient(firstEnc, psk, pskId, info)
                : key.CreateRecipient(firstEnc, info);
            HpkeSender second = usePsk
                ? key.CreatePskSender(psk, pskId, out secondEnc, info)
                : key.CreateSender(out secondEnc, info);

            using (HpkeRecipient secondRecipient = usePsk
                ? key.CreatePskRecipient(secondEnc, psk, pskId, info)
                : key.CreateRecipient(secondEnc, info))
            {
                byte[] firstExport = first.Export(Array.Empty<byte>(), 32);
                byte[] secondExport = second.Export(Array.Empty<byte>(), 32);
                key.Dispose();
                info.AsSpan().Clear();
                psk.AsSpan().Clear();
                pskId.AsSpan().Clear();
                firstEnc.AsSpan().Clear();
                secondEnc.AsSpan().Clear();

                Assert.Equal(message, firstRecipient.Open(first.Seal(message)));
                Assert.Equal(message, firstRecipient.Open(first.Seal(message)));
                Assert.Equal(firstExport, first.Export(Array.Empty<byte>(), 32));
                Assert.Equal(firstExport, firstRecipient.Export(Array.Empty<byte>(), 32));
                first.Dispose();
                firstRecipient.Dispose();

                Assert.Equal(message, secondRecipient.Open(second.Seal(message)));
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
            byte[] info = "application context"u8.ToArray();
            byte[] psk = new byte[32];
            byte[] pskId = "identifier"u8.ToArray();
            byte[] context = "exporter context"u8.ToArray();
            byte[] enc;

            using (Hpke key = Hpke.GenerateKey(suite))
            using (HpkeSender sender = usePsk
                ? key.CreatePskSender(psk, pskId, out enc, info)
                : key.CreateSender(out enc, info))
            using (HpkeRecipient recipient = usePsk
                ? key.CreatePskRecipient(enc, psk, pskId, info)
                : key.CreateRecipient(enc, info))
            {
                byte[] reference = sender.Export(context, 32);

                foreach (int length in new[] { 0, 1, 31, 32, 33, 65, maximumLength })
                {
                    byte[] expected = sender.Export(context, length);
                    Assert.Equal(length, expected.Length);
                    Assert.Equal(expected, sender.Export(context.AsSpan(), length));
                    Assert.Equal(expected, recipient.Export(context, length));
                    Assert.Equal(expected, recipient.Export(context.AsSpan(), length));
                    byte[] senderBuffer = GuardedBuffer(length);
                    byte[] recipientBuffer = GuardedBuffer(length);
                    sender.Export(context, senderBuffer.AsSpan(1, length));
                    recipient.Export(context, recipientBuffer.AsSpan(1, length));
                    AssertGuardedOutput(expected, senderBuffer);
                    AssertGuardedOutput(expected, recipientBuffer);
                }

                Assert.NotEqual(reference, sender.Export(Array.Empty<byte>(), 32));
                Assert.NotEqual(reference, sender.Export([0], 32));
                Assert.False(reference.AsSpan().SequenceEqual(sender.Export(context, 33).AsSpan(0, 32)));

                foreach (int contextLength in new[] { 234, 235, HpkeTestData.MaxExporterContextLength })
                {
                    byte[] longContext = new byte[contextLength];
                    longContext.AsSpan().Fill(0x39);
                    Assert.Equal(sender.Export(longContext, 32), recipient.Export(longContext, 32));
                }

                byte[] message = "message"u8.ToArray();

                for (int i = 0; i < 3; i++)
                {
                    byte[] ciphertext = sender.Seal(message);
                    Assert.Equal(reference, sender.Export(context, 32));
                    Assert.Equal(reference, recipient.Export(context, 32));
                    Assert.Equal(message, recipient.Open(ciphertext));
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

        private static HpkeRecipient CreateRecipient(Hpke key, HpkeTestVector vector, byte[] enc, bool useSpan)
        {
            byte[] info = vector.Info.HexToByteArray();

            if (vector.UsePsk)
            {
                byte[] psk = vector.Psk.HexToByteArray();
                byte[] pskId = vector.PskId.HexToByteArray();
                return useSpan
                    ? key.CreatePskRecipient(enc.AsSpan(), psk, pskId, info)
                    : key.CreatePskRecipient(enc, psk, pskId, info);
            }

            return useSpan ? key.CreateRecipient(enc.AsSpan(), info) : key.CreateRecipient(enc, info);
        }

        private static void AssertKnownExports(HpkeRecipient recipient, IReadOnlyList<HpkeExportVector> exports)
        {
            foreach (HpkeExportVector export in exports)
            {
                byte[] context = export.Context.HexToByteArray();
                byte[] expected = export.ExportedValue.HexToByteArray();
                Assert.Equal(expected, recipient.Export(context, export.Length));
                Assert.Equal(expected, recipient.Export(context.AsSpan(), export.Length));
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

        private static byte[] Different(byte[] input)
        {
            byte[] result = input.Length == 0 ? [1] : (byte[])input.Clone();
            result[0] ^= 0x80;
            return result;
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
