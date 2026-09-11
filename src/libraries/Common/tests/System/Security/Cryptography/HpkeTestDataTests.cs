// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    public static class HpkeTestDataTests
    {
        [Fact]
        public static void Corpus_CoversEveryDeclaredComponent()
        {
            Assert.Equal(
                Enum.GetValues(typeof(HpkeKem)).Cast<HpkeKem>().OrderBy(value => value),
                HpkeTestData.Vectors.Select(vector => vector.Kem).Distinct().OrderBy(value => value));
            Assert.Equal(
                Enum.GetValues(typeof(HpkeKdf)).Cast<HpkeKdf>().OrderBy(value => value),
                HpkeTestData.Vectors.Select(vector => vector.Kdf).Distinct().OrderBy(value => value));
            Assert.Equal(
                Enum.GetValues(typeof(HpkeAead)).Cast<HpkeAead>().OrderBy(value => value),
                HpkeTestData.Vectors.Select(vector => vector.Aead).Distinct().OrderBy(value => value));
            Assert.Equal(
                HpkeTestData.Vectors.Count,
                HpkeTestData.Vectors.Select(vector => vector.Name).Distinct().Count());
        }

        [Theory]
        [MemberData(nameof(HpkeTestData.VectorNames), MemberType = typeof(HpkeTestData))]
        public static void Vector_HasValidShape(string name)
        {
            HpkeTestVector vector = HpkeTestData.GetVector(name);
            HpkeSuite suite = new(vector.Kem, vector.Kdf, vector.Aead);
            int hashLength = vector.Kdf switch
            {
                HpkeKdf.HKDF_SHA256 or HpkeKdf.SHAKE128 => 32,
                HpkeKdf.HKDF_SHA384 => 48,
                HpkeKdf.HKDF_SHA512 or HpkeKdf.SHAKE256 => 64,
                _ => throw new InvalidOperationException(),
            };
            int keyLength = vector.Aead == HpkeAead.AES_128_GCM ? 16 : 32;
            bool oneStage = vector.Kdf is HpkeKdf.SHAKE128 or HpkeKdf.SHAKE256;

            Assert.NotEmpty(vector.Name);
            Assert.NotEmpty(vector.Source);
            Assert.NotEmpty(vector.KeyMaterial.HexToByteArray());
            Assert.Equal(suite.DecapsulationKeySizeInBytes, vector.DecapsulationKey.HexToByteArray().Length);
            Assert.Equal(suite.EncapsulationKeySizeInBytes, vector.EncapsulationKey.HexToByteArray().Length);
            Assert.Equal(suite.EncapsulatedSecretSizeInBytes, vector.EncapsulatedSecret.HexToByteArray().Length);
            Assert.NotEmpty(vector.SharedSecret.HexToByteArray());
            Assert.Equal(keyLength, vector.AeadKey.HexToByteArray().Length);
            Assert.Equal(12, vector.BaseNonce.HexToByteArray().Length);
            Assert.Equal(hashLength, vector.ExporterSecret.HexToByteArray().Length);
            Assert.InRange(vector.Info.HexToByteArray().Length, 0, oneStage ? ushort.MaxValue : int.MaxValue);

            if (vector.UsePsk)
            {
                Assert.InRange(vector.Psk.HexToByteArray().Length, 32, oneStage ? ushort.MaxValue : int.MaxValue);
                Assert.InRange(vector.PskId.HexToByteArray().Length, 1, oneStage ? ushort.MaxValue : int.MaxValue);
            }
            else
            {
                Assert.Empty(vector.Psk);
                Assert.Empty(vector.PskId);
            }

            Assert.NotEmpty(vector.Messages);

            foreach (HpkeMessageVector message in vector.Messages)
            {
                byte[] plaintext = message.Plaintext.HexToByteArray();
                byte[] ciphertext = message.Ciphertext.HexToByteArray();
                _ = message.AssociatedData.HexToByteArray();
                Assert.Equal(suite.GetCiphertextLength(plaintext.Length), ciphertext.Length);
            }

            Assert.NotEmpty(vector.Exports);

            foreach (HpkeExportVector export in vector.Exports)
            {
                _ = export.Context.HexToByteArray();
                Assert.InRange(export.Length, 0, oneStage ? ushort.MaxValue : 255 * hashLength);
                Assert.Equal(export.Length, export.ExportedValue.HexToByteArray().Length);
            }
        }

        [Fact]
        public static void GeneratedCases_CoverMissingBoundaries()
        {
            HpkeTestVector carry = HpkeTestData.GetVector("Generated-EmptyInfo-NonceCarry");
            Assert.False(carry.UsePsk);
            Assert.Empty(carry.Info);
            Assert.Equal(257, carry.Messages.Count);
            Assert.Empty(carry.Messages[0].Plaintext);
            Assert.Empty(carry.Messages[255].Plaintext);
            Assert.Empty(carry.Messages[256].Plaintext);
            Assert.Contains(carry.Messages, message => message.Plaintext.HexToByteArray().Length == 15);
            Assert.Contains(carry.Messages, message => message.Plaintext.HexToByteArray().Length == 16);
            Assert.Contains(carry.Messages, message => message.Plaintext.HexToByteArray().Length == 17);
            Assert.Contains(carry.Exports, export => export.Length == 0);
            Assert.Contains(carry.Exports, export => export.Length == 1);
            Assert.Contains(carry.Exports, export => export.Length == 257);

            HpkeTestVector psk = HpkeTestData.GetVector("Generated-SHAKE256-Psk-MaxInputs");
            Assert.True(psk.UsePsk);
            Assert.Equal(HpkeKdf.SHAKE256, psk.Kdf);
            Assert.Equal(ushort.MaxValue, psk.Psk.HexToByteArray().Length);
            Assert.Equal(ushort.MaxValue, psk.PskId.HexToByteArray().Length);
            Assert.Equal(ushort.MaxValue, psk.Info.HexToByteArray().Length);
        }
    }
}
