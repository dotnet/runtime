// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    [ConditionalClass(typeof(MLKem), nameof(MLKem.IsSupported))]
    public static class MLKemTests
    {
        [Fact]
        public static void Generate_NullAlgorithm()
        {
            AssertExtensions.Throws<ArgumentNullException>("algorithm", static () => MLKem.GenerateKey(null));
        }

        [Fact]
        public static void ImportPrivateSeed_NullAlgorithm()
        {
            AssertExtensions.Throws<ArgumentNullException>("algorithm", static () =>
                MLKem.ImportPrivateSeed(null, new byte[MLKemAlgorithm.MLKem512.PrivateSeedSizeInBytes]));

            AssertExtensions.Throws<ArgumentNullException>("algorithm", static () =>
                MLKem.ImportPrivateSeed(null, new ReadOnlySpan<byte>(new byte[MLKemAlgorithm.MLKem512.PrivateSeedSizeInBytes])));
        }

        [Fact]
        public static void ImportPrivateSeed_NullSource()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", static () =>
                MLKem.ImportPrivateSeed(MLKemAlgorithm.MLKem512, null));
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void ImportPrivateSeed_WrongSize_Array(MLKemAlgorithm algorithm)
        {
            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportPrivateSeed(algorithm, new byte[algorithm.PrivateSeedSizeInBytes + 1]));

            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportPrivateSeed(algorithm, new byte[algorithm.PrivateSeedSizeInBytes - 1]));

            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportPrivateSeed(algorithm, Array.Empty<byte>()));
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void ImportPrivateSeed_WrongSize_Span(MLKemAlgorithm algorithm)
        {
            byte[] seed = new byte[algorithm.PrivateSeedSizeInBytes + 1];

            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportPrivateSeed(algorithm, seed.AsSpan()));

            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportPrivateSeed(algorithm, seed.AsSpan(0, seed.Length - 2)));

            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportPrivateSeed(algorithm, ReadOnlySpan<byte>.Empty));
        }

        [Fact]
        public static void ImportDecapsulationKey_NullAlgorithm()
        {
            AssertExtensions.Throws<ArgumentNullException>("algorithm", static () =>
                MLKem.ImportDecapsulationKey(null, new byte[MLKemAlgorithm.MLKem512.DecapsulationKeySizeInBytes]));
        }

        [Fact]
        public static void ImportDecapsulationKey_NullSource()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", static () =>
                MLKem.ImportDecapsulationKey(MLKemAlgorithm.MLKem512, null));
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void ImportDecapsulationKey_WrongSize_Array(MLKemAlgorithm algorithm)
        {
            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportDecapsulationKey(algorithm, new byte[algorithm.DecapsulationKeySizeInBytes + 1]));

            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportDecapsulationKey(algorithm, new byte[algorithm.DecapsulationKeySizeInBytes - 1]));

            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportDecapsulationKey(algorithm, Array.Empty<byte>()));
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void ImportDecapsulationKey_WrongSize_Span(MLKemAlgorithm algorithm)
        {
            byte[] destination = new byte[algorithm.DecapsulationKeySizeInBytes + 1];

            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportDecapsulationKey(algorithm, destination.AsSpan()));

            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportDecapsulationKey(algorithm, destination.AsSpan(0, destination.Length - 2)));

            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportDecapsulationKey(algorithm, ReadOnlySpan<byte>.Empty));
        }

        [Fact]
        public static void ImportEncapsulationKey_NullAlgorithm()
        {
            AssertExtensions.Throws<ArgumentNullException>("algorithm", static () =>
                MLKem.ImportEncapsulationKey(null, new byte[MLKemAlgorithm.MLKem512.EncapsulationKeySizeInBytes]));
        }

        [Fact]
        public static void ImportEncapsulationKey_NullSource()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", static () =>
                MLKem.ImportEncapsulationKey(MLKemAlgorithm.MLKem512, null));
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void ImportEncapsulationKey_WrongSize_Array(MLKemAlgorithm algorithm)
        {
            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportEncapsulationKey(algorithm, new byte[algorithm.EncapsulationKeySizeInBytes + 1]));

            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportEncapsulationKey(algorithm, new byte[algorithm.EncapsulationKeySizeInBytes - 1]));

            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportEncapsulationKey(algorithm, []));
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void ImportEncapsulationKey_WrongSize_Span(MLKemAlgorithm algorithm)
        {
            byte[] destination = new byte[algorithm.EncapsulationKeySizeInBytes + 1];

            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportEncapsulationKey(algorithm, destination.AsSpan()));

            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportEncapsulationKey(algorithm, destination.AsSpan(0, destination.Length - 2)));

            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportEncapsulationKey(algorithm, ReadOnlySpan<byte>.Empty));
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void Encapsulate_WrongSize(MLKemAlgorithm algorithm)
        {
            using MLKem kem = MLKem.GenerateKey(algorithm);

            AssertExtensions.Throws<ArgumentException>("ciphertext", () => kem.Encapsulate(
                new byte[algorithm.CiphertextSizeInBytes + 1],
                new byte[algorithm.SharedSecretSizeInBytes]));

            AssertExtensions.Throws<ArgumentException>("ciphertext", () => kem.Encapsulate(
                new byte[algorithm.CiphertextSizeInBytes - 1],
                new byte[algorithm.SharedSecretSizeInBytes]));

            AssertExtensions.Throws<ArgumentException>("ciphertext", () => kem.Encapsulate(
                [],
                new byte[algorithm.SharedSecretSizeInBytes]));

            AssertExtensions.Throws<ArgumentException>("sharedSecret", () => kem.Encapsulate(
                new byte[algorithm.CiphertextSizeInBytes],
                new byte[algorithm.SharedSecretSizeInBytes + 1]));

            AssertExtensions.Throws<ArgumentException>("sharedSecret", () => kem.Encapsulate(
                new byte[algorithm.CiphertextSizeInBytes],
                new byte[algorithm.SharedSecretSizeInBytes - 1]));

            AssertExtensions.Throws<ArgumentException>("sharedSecret", () => kem.Encapsulate(
                new byte[algorithm.SharedSecretSizeInBytes + 1]));

            AssertExtensions.Throws<ArgumentException>("sharedSecret", () => kem.Encapsulate(
                new byte[algorithm.SharedSecretSizeInBytes - 1]));
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void Encapsulate_DestinationTooSmall(MLKemAlgorithm algorithm)
        {
            using MLKem kem = MLKem.GenerateKey(algorithm);

            AssertExtensions.Throws<ArgumentException>("ciphertext", () => kem.Encapsulate(
                new byte[algorithm.CiphertextSizeInBytes - 1],
                new byte[algorithm.SharedSecretSizeInBytes],
                out _,
                out _));

            AssertExtensions.Throws<ArgumentException>("sharedSecret", () => kem.Encapsulate(
                new byte[algorithm.CiphertextSizeInBytes],
                new byte[algorithm.SharedSecretSizeInBytes - 1],
                out _,
                out _));
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void Decapsulate_WrongSize(MLKemAlgorithm algorithm)
        {
            using MLKem kem = MLKem.GenerateKey(algorithm);

            AssertExtensions.Throws<ArgumentException>("ciphertext", () => kem.Decapsulate(
                new byte[algorithm.CiphertextSizeInBytes + 1],
                new byte[algorithm.SharedSecretSizeInBytes]));

            AssertExtensions.Throws<ArgumentException>("ciphertext", () => kem.Decapsulate(
                new byte[algorithm.CiphertextSizeInBytes - 1],
                new byte[algorithm.SharedSecretSizeInBytes]));

            AssertExtensions.Throws<ArgumentException>("ciphertext", () => kem.Decapsulate(
                [],
                new byte[algorithm.SharedSecretSizeInBytes]));

            AssertExtensions.Throws<ArgumentException>("ciphertext", () => kem.Decapsulate(
                new byte[algorithm.CiphertextSizeInBytes - 1],
                new byte[algorithm.SharedSecretSizeInBytes],
                out _));

            AssertExtensions.Throws<ArgumentException>("ciphertext", () => kem.Decapsulate(
                new byte[algorithm.CiphertextSizeInBytes + 1],
                new byte[algorithm.SharedSecretSizeInBytes],
                out _));

            AssertExtensions.Throws<ArgumentException>("ciphertext", () => kem.Decapsulate(
                new byte[algorithm.CiphertextSizeInBytes - 1]));

            AssertExtensions.Throws<ArgumentException>("ciphertext", () => kem.Decapsulate(
                new byte[algorithm.CiphertextSizeInBytes + 1]));

            AssertExtensions.Throws<ArgumentException>("sharedSecret", () => kem.Decapsulate(
                new byte[algorithm.CiphertextSizeInBytes],
                new byte[algorithm.SharedSecretSizeInBytes + 1]));

            AssertExtensions.Throws<ArgumentException>("sharedSecret", () => kem.Decapsulate(
                new byte[algorithm.CiphertextSizeInBytes],
                new byte[algorithm.SharedSecretSizeInBytes - 1]));

            AssertExtensions.Throws<ArgumentException>("sharedSecret", () => kem.Decapsulate(
                new byte[algorithm.CiphertextSizeInBytes],
                []));
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void Decapsulate_DestinationTooSmall(MLKemAlgorithm algorithm)
        {
            using MLKem kem = MLKem.GenerateKey(algorithm);

            AssertExtensions.Throws<ArgumentException>("sharedSecret", () => kem.Decapsulate(
                new byte[algorithm.CiphertextSizeInBytes],
                new byte[algorithm.SharedSecretSizeInBytes - 1],
                out _));
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void Decapsulate_NullArg(MLKemAlgorithm algorithm)
        {
            using MLKem kem = MLKem.GenerateKey(algorithm);
            AssertExtensions.Throws<ArgumentNullException>("ciphertext", () => kem.Decapsulate(null));
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void ExportPrivateSeed_WrongSize(MLKemAlgorithm algorithm)
        {
            using MLKem kem = MLKem.GenerateKey(algorithm);

            AssertExtensions.Throws<ArgumentException>("destination", () => kem.ExportPrivateSeed(
                new byte[algorithm.PrivateSeedSizeInBytes + 1]));

            AssertExtensions.Throws<ArgumentException>("destination", () => kem.ExportPrivateSeed(
                new byte[algorithm.PrivateSeedSizeInBytes - 1]));

            AssertExtensions.Throws<ArgumentException>("destination", () => kem.ExportPrivateSeed(
                []));
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void ExportDecapsulationKey_WrongSize(MLKemAlgorithm algorithm)
        {
            using MLKem kem = MLKem.GenerateKey(algorithm);

            AssertExtensions.Throws<ArgumentException>("destination", () => kem.ExportDecapsulationKey(
                new byte[algorithm.DecapsulationKeySizeInBytes + 1]));

            AssertExtensions.Throws<ArgumentException>("destination", () => kem.ExportDecapsulationKey(
                new byte[algorithm.DecapsulationKeySizeInBytes - 1]));

            AssertExtensions.Throws<ArgumentException>("destination", () => kem.ExportDecapsulationKey(
                []));
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void ExportEncapsulationKey_WrongSize(MLKemAlgorithm algorithm)
        {
            using MLKem kem = MLKem.GenerateKey(algorithm);

            AssertExtensions.Throws<ArgumentException>("destination", () => kem.ExportEncapsulationKey(
                new byte[algorithm.EncapsulationKeySizeInBytes + 1]));

            AssertExtensions.Throws<ArgumentException>("destination", () => kem.ExportEncapsulationKey(
                new byte[algorithm.EncapsulationKeySizeInBytes - 1]));

            AssertExtensions.Throws<ArgumentException>("destination", () => kem.ExportEncapsulationKey(
                []));
        }

        [Fact]
        public static void ImportSubjectPublicKeyInfo_NullSource()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", static () =>
                MLKem.ImportSubjectPublicKeyInfo((byte[])null));
        }

        [Fact]
        public static void UseAfterDispose()
        {
            MLKem kem = MLKem.GenerateKey(MLKemAlgorithm.MLKem512);
            kem.Dispose();
            kem.Dispose(); // Assert.NoThrow

            Assert.Throws<ObjectDisposedException>(() =>  kem.Encapsulate(
                new byte[MLKemAlgorithm.MLKem512.CiphertextSizeInBytes],
                new byte[MLKemAlgorithm.MLKem512.SharedSecretSizeInBytes]));

            Assert.Throws<ObjectDisposedException>(() =>  kem.Encapsulate(
                new byte[MLKemAlgorithm.MLKem512.CiphertextSizeInBytes],
                new byte[MLKemAlgorithm.MLKem512.SharedSecretSizeInBytes],
                out _,
                out _));

            Assert.Throws<ObjectDisposedException>(() =>  kem.Encapsulate(
                out _));

            Assert.Throws<ObjectDisposedException>(() =>  kem.Encapsulate(
                new byte[MLKemAlgorithm.MLKem512.SharedSecretSizeInBytes]));

            Assert.Throws<ObjectDisposedException>(() =>  kem.Decapsulate(
                new byte[MLKemAlgorithm.MLKem512.CiphertextSizeInBytes],
                new byte[MLKemAlgorithm.MLKem512.SharedSecretSizeInBytes]));

            Assert.Throws<ObjectDisposedException>(() =>  kem.Decapsulate(
                new byte[MLKemAlgorithm.MLKem512.CiphertextSizeInBytes],
                new byte[MLKemAlgorithm.MLKem512.SharedSecretSizeInBytes],
                out _));

            Assert.Throws<ObjectDisposedException>(() =>  kem.Decapsulate(
                new byte[MLKemAlgorithm.MLKem512.CiphertextSizeInBytes]));

            Assert.Throws<ObjectDisposedException>(() => kem.ExportPrivateSeed(
                new byte[MLKemAlgorithm.MLKem512.PrivateSeedSizeInBytes]));

            Assert.Throws<ObjectDisposedException>(() => kem.ExportPrivateSeed());

            Assert.Throws<ObjectDisposedException>(() => kem.ExportDecapsulationKey(
                new byte[MLKemAlgorithm.MLKem512.DecapsulationKeySizeInBytes]));

            Assert.Throws<ObjectDisposedException>(() => kem.ExportDecapsulationKey());

            Assert.Throws<ObjectDisposedException>(() => kem.ExportEncapsulationKey(
                new byte[MLKemAlgorithm.MLKem512.EncapsulationKeySizeInBytes]));

            Assert.Throws<ObjectDisposedException>(() => kem.ExportEncapsulationKey());

            Assert.Throws<ObjectDisposedException>(() => kem.ExportSubjectPublicKeyInfo());
            Assert.Throws<ObjectDisposedException>(() => kem.TryExportSubjectPublicKeyInfo(
                new byte[2048],
                out _));
        }
    }
}
