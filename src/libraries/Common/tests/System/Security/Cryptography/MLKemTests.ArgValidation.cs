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
        [Fact]
        public static void Generate_NullAlgorithm()
        {
            AssertExtensions.Throws<ArgumentNullException>("algorithm", static () => MLKem.GenerateMLKemKey(null));
        }

        [Fact]
        public static void ImportMLKemPrivateSeed_NullAlgorithm()
        {
            AssertExtensions.Throws<ArgumentNullException>("algorithm", static () =>
                MLKem.ImportMLKemPrivateSeed(null, new byte[MLKem.PrivateSeedSizeInBytes]));
        }

        [Fact]
        public static void ImportMLKemPrivateSeed_WrongSize()
        {
            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportMLKemPrivateSeed(MLKemAlgorithm.MLKem512, new byte[MLKem.PrivateSeedSizeInBytes + 1]));

            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportMLKemPrivateSeed(MLKemAlgorithm.MLKem512, new byte[MLKem.PrivateSeedSizeInBytes - 1]));

            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportMLKemPrivateSeed(MLKemAlgorithm.MLKem512, []));
        }

        [Fact]
        public static void ImportMLKemDecapsulationKey_NullAlgorithm()
        {
            AssertExtensions.Throws<ArgumentNullException>("algorithm", static () =>
                MLKem.ImportMLKemDecapsulationKey(null, new byte[MLKemAlgorithm.MLKem512.DecapsulationKeySizeInBytes]));
        }

        [Theory]
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

        [Fact]
        public static void ImportMLKemEncapsulationKey_NullAlgorithm()
        {
            AssertExtensions.Throws<ArgumentNullException>("algorithm", static () =>
                MLKem.ImportMLKemEncapsulationKey(null, new byte[MLKemAlgorithm.MLKem512.EncapsulationKeySizeInBytes]));
        }

        [Theory]
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
    }
}
