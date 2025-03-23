// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    public static partial class MLKemTests
    {
        [ConditionalFact(typeof(MLKem), nameof(MLKem.IsSupported))]
        public static void Generate_NullAlgorithm()
        {
            AssertExtensions.Throws<ArgumentNullException>("algorithm", static () => MLKem.GenerateKey(null));
        }

        [ConditionalFact(typeof(MLKem), nameof(MLKem.IsSupported))]
        public static void ImportPrivateSeed_NullAlgorithm()
        {
            AssertExtensions.Throws<ArgumentNullException>("algorithm", static () =>
                MLKem.ImportPrivateSeed(null, new byte[MLKemAlgorithm.MLKem512.PrivateSeedSizeInBytes]));
        }

        [ConditionalTheory(typeof(MLKem), nameof(MLKem.IsSupported))]
        [MemberData(nameof(MLKemAlgorithms))]
        public static void ImportPrivateSeed_WrongSize(MLKemAlgorithm algorithm)
        {
            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportPrivateSeed(algorithm, new byte[algorithm.PrivateSeedSizeInBytes + 1]));

            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportPrivateSeed(algorithm, new byte[algorithm.PrivateSeedSizeInBytes - 1]));

            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportPrivateSeed(algorithm, []));
        }

        [ConditionalFact(typeof(MLKem), nameof(MLKem.IsSupported))]
        public static void ImportDecapsulationKey_NullAlgorithm()
        {
            AssertExtensions.Throws<ArgumentNullException>("algorithm", static () =>
                MLKem.ImportDecapsulationKey(null, new byte[MLKemAlgorithm.MLKem512.DecapsulationKeySizeInBytes]));
        }

        [ConditionalTheory(typeof(MLKem), nameof(MLKem.IsSupported))]
        [MemberData(nameof(MLKemAlgorithms))]
        public static void ImportDecapsulationKey_WrongSize(MLKemAlgorithm algorithm)
        {
            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportDecapsulationKey(algorithm, new byte[algorithm.DecapsulationKeySizeInBytes + 1]));

            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportDecapsulationKey(algorithm, new byte[algorithm.DecapsulationKeySizeInBytes - 1]));

            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportDecapsulationKey(algorithm, []));
        }

        [ConditionalFact(typeof(MLKem), nameof(MLKem.IsSupported))]
        public static void ImportEncapsulationKey_NullAlgorithm()
        {
            AssertExtensions.Throws<ArgumentNullException>("algorithm", static () =>
                MLKem.ImportEncapsulationKey(null, new byte[MLKemAlgorithm.MLKem512.EncapsulationKeySizeInBytes]));
        }

        [ConditionalTheory(typeof(MLKem), nameof(MLKem.IsSupported))]
        [MemberData(nameof(MLKemAlgorithms))]
        public static void ImportEncapsulationKey_WrongSize(MLKemAlgorithm algorithm)
        {
            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportEncapsulationKey(algorithm, new byte[algorithm.EncapsulationKeySizeInBytes + 1]));

            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportEncapsulationKey(algorithm, new byte[algorithm.EncapsulationKeySizeInBytes - 1]));

            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportEncapsulationKey(algorithm, []));
        }

        [ConditionalTheory(typeof(MLKem), nameof(MLKem.IsSupported))]
        [MemberData(nameof(MLKemAlgorithms))]
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
                new byte[algorithm.CiphertextSizeInBytes],
                []));
        }

        [ConditionalTheory(typeof(MLKem), nameof(MLKem.IsSupported))]
        [MemberData(nameof(MLKemAlgorithms))]
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

        [ConditionalTheory(typeof(MLKem), nameof(MLKem.IsSupported))]
        [MemberData(nameof(MLKemAlgorithms))]
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

        [ConditionalTheory(typeof(MLKem), nameof(MLKem.IsSupported))]
        [MemberData(nameof(MLKemAlgorithms))]
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

        [ConditionalTheory(typeof(MLKem), nameof(MLKem.IsSupported))]
        [MemberData(nameof(MLKemAlgorithms))]
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

        [ConditionalFact(typeof(MLKem), nameof(MLKem.IsSupported))]
        public static void UseAfterDispose()
        {
            MLKem kem = MLKem.GenerateKey(MLKemAlgorithm.MLKem512);
            kem.Dispose();
            kem.Dispose(); // Assert.NoThrow

            Assert.Throws<ObjectDisposedException>(() =>  kem.Encapsulate(
                new byte[MLKemAlgorithm.MLKem512.CiphertextSizeInBytes],
                new byte[MLKemAlgorithm.MLKem512.SharedSecretSizeInBytes]));

            Assert.Throws<ObjectDisposedException>(() =>  kem.Decapsulate(
                new byte[MLKemAlgorithm.MLKem512.CiphertextSizeInBytes],
                new byte[MLKemAlgorithm.MLKem512.SharedSecretSizeInBytes]));

            Assert.Throws<ObjectDisposedException>(() => kem.ExportPrivateSeed(
                new byte[MLKemAlgorithm.MLKem512.PrivateSeedSizeInBytes]));

            Assert.Throws<ObjectDisposedException>(() => kem.ExportDecapsulationKey(
                new byte[MLKemAlgorithm.MLKem512.DecapsulationKeySizeInBytes]));

            Assert.Throws<ObjectDisposedException>(() => kem.ExportEncapsulationKey(
                new byte[MLKemAlgorithm.MLKem512.EncapsulationKeySizeInBytes]));
        }
    }
}
