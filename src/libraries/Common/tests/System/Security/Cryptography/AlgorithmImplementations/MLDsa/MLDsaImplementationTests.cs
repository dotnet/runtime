// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Security.Cryptography.Dsa.Tests;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.DotNet.XUnitExtensions;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    [ConditionalClass(typeof(MLDsa), nameof(MLDsa.IsSupported))]
    public class MLDsaImplementationTests : MLDsaTestsBase
    {
        protected override MLDsa GenerateKey(MLDsaAlgorithm algorithm) => MLDsa.GenerateKey(algorithm);
        protected override MLDsa ImportPrivateSeed(MLDsaAlgorithm algorithm, ReadOnlySpan<byte> seed) => MLDsa.ImportMLDsaPrivateSeed(algorithm, seed);
        protected override MLDsa ImportSecretKey(MLDsaAlgorithm algorithm, ReadOnlySpan<byte> source) => MLDsa.ImportMLDsaSecretKey(algorithm, source);
        protected override MLDsa ImportPublicKey(MLDsaAlgorithm algorithm, ReadOnlySpan<byte> source) => MLDsa.ImportMLDsaPublicKey(algorithm, source);

        [Fact]
        public static void GenerateImport_NullAlgorithm()
        {
            AssertExtensions.Throws<ArgumentNullException>("algorithm", static () => MLDsa.GenerateKey(null));
            AssertExtensions.Throws<ArgumentNullException>("algorithm", static () => MLDsa.ImportMLDsaPrivateSeed(null, default));
            AssertExtensions.Throws<ArgumentNullException>("algorithm", static () => MLDsa.ImportMLDsaPublicKey(null, default));
            AssertExtensions.Throws<ArgumentNullException>("algorithm", static () => MLDsa.ImportMLDsaSecretKey(null, default));
        }

        [Theory]
        [MemberData(nameof(MLDsaTestsData.AllMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public static void ImportMLDsaSecretKey_WrongSize(MLDsaAlgorithm algorithm)
        {
            AssertExtensions.Throws<ArgumentException>("source", () => MLDsa.ImportMLDsaSecretKey(algorithm, new byte[algorithm.SecretKeySizeInBytes - 1]));
            AssertExtensions.Throws<ArgumentException>("source", () => MLDsa.ImportMLDsaSecretKey(algorithm, new byte[algorithm.SecretKeySizeInBytes + 1]));
            AssertExtensions.Throws<ArgumentException>("source", () => MLDsa.ImportMLDsaSecretKey(algorithm, default));
        }

        [Theory]
        [MemberData(nameof(MLDsaTestsData.AllMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public static void ImportMLDsaPrivateSeed_WrongSize(MLDsaAlgorithm algorithm)
        {
            AssertExtensions.Throws<ArgumentException>("source", () => MLDsa.ImportMLDsaPrivateSeed(algorithm, new byte[algorithm.PrivateSeedSizeInBytes - 1]));
            AssertExtensions.Throws<ArgumentException>("source", () => MLDsa.ImportMLDsaPrivateSeed(algorithm, new byte[algorithm.PrivateSeedSizeInBytes + 1]));
            AssertExtensions.Throws<ArgumentException>("source", () => MLDsa.ImportMLDsaPrivateSeed(algorithm, default));
        }

        [Theory]
        [MemberData(nameof(MLDsaTestsData.AllMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public static void ImportMLDsaPublicKey_WrongSize(MLDsaAlgorithm algorithm)
        {
            AssertExtensions.Throws<ArgumentException>("source", () => MLDsa.ImportMLDsaPublicKey(algorithm, new byte[algorithm.PublicKeySizeInBytes - 1]));
            AssertExtensions.Throws<ArgumentException>("source", () => MLDsa.ImportMLDsaPublicKey(algorithm, new byte[algorithm.PublicKeySizeInBytes + 1]));
            AssertExtensions.Throws<ArgumentException>("source", () => MLDsa.ImportMLDsaPublicKey(algorithm, default));
        }

        [Fact]
        public static void UseAfterDispose()
        {
            MLDsa mldsa = MLDsa.GenerateKey(MLDsaAlgorithm.MLDsa44);
            mldsa.Dispose();
            mldsa.Dispose(); // no throw

            VerifyDisposed(mldsa);
        }
    }
}
