// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    [ConditionalClass(typeof(MLKem), nameof(MLKem.IsSupported))]
    public static partial class MLKemTests
    {
        [Theory]
        [MemberData(nameof(MLKemAlgorithms))]
        public static void Generate_MlKemKeys(MLKemAlgorithm algorithm)
        {
            using MLKem kem = MLKem.GenerateMLKemKey(algorithm);
            Span<byte> seed = stackalloc byte[MLKem.PrivateSeedSizeInBytes];
            seed.Clear();

            kem.ExportMLKemPrivateSeed(seed);
            Assert.True(seed.ContainsAnyExcept((byte)0));
        }

        [Theory]
        [MemberData(nameof(MLKemAlgorithms))]
        public static void Generate_Import(MLKemAlgorithm algorithm)
        {
            using MLKem kem = MLKem.GenerateMLKemKey(algorithm);
            Span<byte> seed = stackalloc byte[MLKem.PrivateSeedSizeInBytes];
            seed.Clear();

            kem.ExportMLKemPrivateSeed(seed);
            Assert.True(seed.ContainsAnyExcept((byte)0));

            using MLKem kem2 = MLKem.ImportMLKemPrivateSeed(algorithm, seed);
            Span<byte> seed2 = stackalloc byte[MLKem.PrivateSeedSizeInBytes];
            kem2.ExportMLKemPrivateSeed(seed2);
            AssertExtensions.SequenceEqual(seed, seed2);
        }

        [Theory]
        [MemberData(nameof(MLKemAlgorithms))]
        public static void Encapsulate(MLKemAlgorithm algorithm)
        {
            using MLKem kem = MLKem.GenerateMLKemKey(algorithm);
            Span<byte> ciphertext = new byte[algorithm.CiphertextSizeInBytes];
            Span<byte> sharedSecret = new byte[MLKem.SharedSecretSizeInBytes];
            kem.Encapsulate(ciphertext, sharedSecret);
        }

        public static IEnumerable<object[]> MLKemAlgorithms
        {
            get
            {
                return
                [
                    [ MLKemAlgorithm.MLKem512 ],
                    [ MLKemAlgorithm.MLKem768 ],
                    [ MLKemAlgorithm.MLKem1024 ],
                ];
            }
        }
    }
}
