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
            AssertExtensions.Throws<ArgumentNullException>("algorithm", static () => MLKem.GenerateMLKemKey(null));
        }

        [ConditionalFact(typeof(MLKem), nameof(MLKem.IsSupported))]
        public static void ImportMLKemPrivateSeed_NullAlgorithm()
        {
            AssertExtensions.Throws<ArgumentNullException>("algorithm", static () =>
                MLKem.ImportMLKemPrivateSeed(null, new byte[MLKem.PrivateSeedSizeInBytes]));
        }

        [ConditionalFact(typeof(MLKem), nameof(MLKem.IsSupported))]
        public static void ImportMLKemPrivateSeed_WrongSize()
        {
            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportMLKemPrivateSeed(MLKemAlgorithm.MLKem512, new byte[MLKem.PrivateSeedSizeInBytes + 1]));

            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportMLKemPrivateSeed(MLKemAlgorithm.MLKem512, new byte[MLKem.PrivateSeedSizeInBytes - 1]));

            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportMLKemPrivateSeed(MLKemAlgorithm.MLKem512, []));
        }

        [ConditionalFact(typeof(MLKem), nameof(MLKem.IsSupported))]
        public static void ImportMLKemDecapsulationKey_NullAlgorithm()
        {
            AssertExtensions.Throws<ArgumentNullException>("algorithm", static () =>
                MLKem.ImportMLKemDecapsulationKey(null, new byte[MLKemAlgorithm.MLKem512.DecapsulationKeySizeInBytes]));
        }

        [ConditionalTheory(typeof(MLKem), nameof(MLKem.IsSupported))]
        [MemberData(nameof(MLKemAlgorithms))]
        public static void ImportMLKemDecapsulationKey_WrongSize(MLKemAlgorithm algorithm)
        {
            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportMLKemDecapsulationKey(algorithm, new byte[algorithm.DecapsulationKeySizeInBytes + 1]));

            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportMLKemDecapsulationKey(algorithm, new byte[algorithm.DecapsulationKeySizeInBytes - 1]));

            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportMLKemDecapsulationKey(algorithm, []));
        }

        [ConditionalFact(typeof(MLKem), nameof(MLKem.IsSupported))]
        public static void ImportMLKemEncapsulationKey_NullAlgorithm()
        {
            AssertExtensions.Throws<ArgumentNullException>("algorithm", static () =>
                MLKem.ImportMLKemEncapsulationKey(null, new byte[MLKemAlgorithm.MLKem512.EncapsulationKeySizeInBytes]));
        }

        [ConditionalTheory(typeof(MLKem), nameof(MLKem.IsSupported))]
        [MemberData(nameof(MLKemAlgorithms))]
        public static void ImportMLKemEncapsulationKey_WrongSize(MLKemAlgorithm algorithm)
        {
            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportMLKemEncapsulationKey(algorithm, new byte[algorithm.EncapsulationKeySizeInBytes + 1]));

            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportMLKemEncapsulationKey(algorithm, new byte[algorithm.EncapsulationKeySizeInBytes - 1]));

            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportMLKemEncapsulationKey(algorithm, []));
        }

        [ConditionalTheory(typeof(MLKem), nameof(MLKem.IsSupported))]
        [MemberData(nameof(MLKemAlgorithms))]
        public static void Encapsulate_WrongSize(MLKemAlgorithm algorithm)
        {
            using MLKem kem = MLKem.GenerateMLKemKey(algorithm);

            AssertExtensions.Throws<ArgumentException>("ciphertext", () => kem.Encapsulate(
                new byte[algorithm.CiphertextSizeInBytes + 1],
                new byte[MLKem.SharedSecretSizeInBytes]));

            AssertExtensions.Throws<ArgumentException>("ciphertext", () => kem.Encapsulate(
                new byte[algorithm.CiphertextSizeInBytes - 1],
                new byte[MLKem.SharedSecretSizeInBytes]));

            AssertExtensions.Throws<ArgumentException>("ciphertext", () => kem.Encapsulate(
                [],
                new byte[MLKem.SharedSecretSizeInBytes]));

            AssertExtensions.Throws<ArgumentException>("sharedSecret", () => kem.Encapsulate(
                new byte[algorithm.CiphertextSizeInBytes],
                new byte[MLKem.SharedSecretSizeInBytes + 1]));

            AssertExtensions.Throws<ArgumentException>("sharedSecret", () => kem.Encapsulate(
                new byte[algorithm.CiphertextSizeInBytes],
                new byte[MLKem.SharedSecretSizeInBytes - 1]));

            AssertExtensions.Throws<ArgumentException>("sharedSecret", () => kem.Encapsulate(
                new byte[algorithm.CiphertextSizeInBytes],
                []));
        }

        [ConditionalTheory(typeof(MLKem), nameof(MLKem.IsSupported))]
        [MemberData(nameof(MLKemAlgorithms))]
        public static void Decapsulate_WrongSize(MLKemAlgorithm algorithm)
        {
            using MLKem kem = MLKem.GenerateMLKemKey(algorithm);

            AssertExtensions.Throws<ArgumentException>("ciphertext", () => kem.Decapsulate(
                new byte[algorithm.CiphertextSizeInBytes + 1],
                new byte[MLKem.SharedSecretSizeInBytes]));

            AssertExtensions.Throws<ArgumentException>("ciphertext", () => kem.Decapsulate(
                new byte[algorithm.CiphertextSizeInBytes - 1],
                new byte[MLKem.SharedSecretSizeInBytes]));

            AssertExtensions.Throws<ArgumentException>("ciphertext", () => kem.Decapsulate(
                [],
                new byte[MLKem.SharedSecretSizeInBytes]));

            AssertExtensions.Throws<ArgumentException>("sharedSecret", () => kem.Decapsulate(
                new byte[algorithm.CiphertextSizeInBytes],
                new byte[MLKem.SharedSecretSizeInBytes + 1]));

            AssertExtensions.Throws<ArgumentException>("sharedSecret", () => kem.Decapsulate(
                new byte[algorithm.CiphertextSizeInBytes],
                new byte[MLKem.SharedSecretSizeInBytes - 1]));

            AssertExtensions.Throws<ArgumentException>("sharedSecret", () => kem.Decapsulate(
                new byte[algorithm.CiphertextSizeInBytes],
                []));
        }

        [ConditionalTheory(typeof(MLKem), nameof(MLKem.IsSupported))]
        [MemberData(nameof(MLKemAlgorithms))]
        public static void ExportMLKemPrivateSeed_WrongSize(MLKemAlgorithm algorithm)
        {
            using MLKem kem = MLKem.GenerateMLKemKey(algorithm);

            AssertExtensions.Throws<ArgumentException>("destination", () => kem.ExportMLKemPrivateSeed(
                new byte[MLKem.PrivateSeedSizeInBytes + 1]));

            AssertExtensions.Throws<ArgumentException>("destination", () => kem.ExportMLKemPrivateSeed(
                new byte[MLKem.PrivateSeedSizeInBytes - 1]));

            AssertExtensions.Throws<ArgumentException>("destination", () => kem.ExportMLKemPrivateSeed(
                []));
        }

        [ConditionalTheory(typeof(MLKem), nameof(MLKem.IsSupported))]
        [MemberData(nameof(MLKemAlgorithms))]
        public static void ExportMLKemDecapsulationKey_WrongSize(MLKemAlgorithm algorithm)
        {
            using MLKem kem = MLKem.GenerateMLKemKey(algorithm);

            AssertExtensions.Throws<ArgumentException>("destination", () => kem.ExportMLKemDecapsulationKey(
                new byte[algorithm.DecapsulationKeySizeInBytes + 1]));

            AssertExtensions.Throws<ArgumentException>("destination", () => kem.ExportMLKemDecapsulationKey(
                new byte[algorithm.DecapsulationKeySizeInBytes - 1]));

            AssertExtensions.Throws<ArgumentException>("destination", () => kem.ExportMLKemDecapsulationKey(
                []));
        }

        [ConditionalTheory(typeof(MLKem), nameof(MLKem.IsSupported))]
        [MemberData(nameof(MLKemAlgorithms))]
        public static void ExportMLKemEncapsulationKey_WrongSize(MLKemAlgorithm algorithm)
        {
            using MLKem kem = MLKem.GenerateMLKemKey(algorithm);

            AssertExtensions.Throws<ArgumentException>("destination", () => kem.ExportMLKemEncapsulationKey(
                new byte[algorithm.EncapsulationKeySizeInBytes + 1]));

            AssertExtensions.Throws<ArgumentException>("destination", () => kem.ExportMLKemEncapsulationKey(
                new byte[algorithm.EncapsulationKeySizeInBytes - 1]));

            AssertExtensions.Throws<ArgumentException>("destination", () => kem.ExportMLKemEncapsulationKey(
                []));
        }

        [ConditionalFact(typeof(MLKem), nameof(MLKem.IsSupported))]
        public static void UseAfterDispose()
        {
            MLKem kem = MLKem.GenerateMLKemKey(MLKemAlgorithm.MLKem512);
            kem.Dispose();
            kem.Dispose(); // Assert.NoThrow

            Assert.Throws<ObjectDisposedException>(() =>  kem.Encapsulate(
                new byte[MLKemAlgorithm.MLKem512.CiphertextSizeInBytes],
                new byte[MLKem.SharedSecretSizeInBytes]));

            Assert.Throws<ObjectDisposedException>(() =>  kem.Decapsulate(
                new byte[MLKemAlgorithm.MLKem512.CiphertextSizeInBytes],
                new byte[MLKem.SharedSecretSizeInBytes]));

            Assert.Throws<ObjectDisposedException>(() => kem.ExportMLKemPrivateSeed(
                new byte[MLKem.PrivateSeedSizeInBytes]));

            Assert.Throws<ObjectDisposedException>(() => kem.ExportMLKemDecapsulationKey(
                new byte[MLKemAlgorithm.MLKem512.DecapsulationKeySizeInBytes]));

            Assert.Throws<ObjectDisposedException>(() => kem.ExportMLKemEncapsulationKey(
                new byte[MLKemAlgorithm.MLKem512.EncapsulationKeySizeInBytes]));
        }
    }
}
