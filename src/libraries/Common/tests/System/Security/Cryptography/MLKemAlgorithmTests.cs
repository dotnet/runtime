// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    public static partial class MLKemAlgorithmTests
    {
        [Fact]
        public static void Algorithms_AreSame()
        {
            Assert.Same(MLKemAlgorithm.MLKem512, MLKemAlgorithm.MLKem512);
            Assert.Same(MLKemAlgorithm.MLKem768, MLKemAlgorithm.MLKem768);
            Assert.Same(MLKemAlgorithm.MLKem1024, MLKemAlgorithm.MLKem1024);
        }

        [Theory]
        [MemberData(nameof(MLKemAlgorithms))]
        public static void Algorithms_Equal(MLKemAlgorithm algorithm)
        {
            Assert.True(algorithm.Equals(algorithm), nameof(algorithm.Equals));
            Assert.True(algorithm.Equals((object)algorithm), nameof(algorithm.Equals));
            Assert.False(algorithm.Equals(null), nameof(algorithm.Equals));
        }

        [Theory]
        [MemberData(nameof(MLKemAlgorithms))]
        public static void Algorithms_GetHashCode(MLKemAlgorithm algorithm)
        {
            Assert.Equal(algorithm.Name.GetHashCode(), algorithm.GetHashCode());
        }

        [Fact]
        public static void Algorithms_Equality()
        {
            Assert.True(MLKemAlgorithm.MLKem512 == MLKemAlgorithm.MLKem512, "MLKemAlgorithm.MLKem512 == MLKemAlgorithm.MLKem512");
            Assert.True(MLKemAlgorithm.MLKem768 == MLKemAlgorithm.MLKem768, "MLKemAlgorithm.MLKem768 == MLKemAlgorithm.MLKem768");
            Assert.True(MLKemAlgorithm.MLKem1024 == MLKemAlgorithm.MLKem1024, "MLKemAlgorithm.MLKem1024 == MLKemAlgorithm.MLKem1024");

            Assert.False(MLKemAlgorithm.MLKem512 == MLKemAlgorithm.MLKem768, "MLKemAlgorithm.MLKem512 == MLKemAlgorithm.MLKem768");
            Assert.False(MLKemAlgorithm.MLKem768 == MLKemAlgorithm.MLKem1024, "MLKemAlgorithm.MLKem768 == MLKemAlgorithm.MLKem1024");
            Assert.False(MLKemAlgorithm.MLKem1024 == MLKemAlgorithm.MLKem512, "MLKemAlgorithm.MLKem1024 == MLKemAlgorithm.MLKem512");
        }

        [Fact]
        public static void Algorithms_Inquality()
        {
            Assert.False(MLKemAlgorithm.MLKem512 != MLKemAlgorithm.MLKem512, "MLKemAlgorithm.MLKem512 != MLKemAlgorithm.MLKem512");
            Assert.False(MLKemAlgorithm.MLKem768 != MLKemAlgorithm.MLKem768, "MLKemAlgorithm.MLKem768 != MLKemAlgorithm.MLKem768");
            Assert.False(MLKemAlgorithm.MLKem1024 != MLKemAlgorithm.MLKem1024, "MLKemAlgorithm.MLKem1024 != MLKemAlgorithm.MLKem1024");

            Assert.True(MLKemAlgorithm.MLKem512 != MLKemAlgorithm.MLKem768, "MLKemAlgorithm.MLKem512 != MLKemAlgorithm.MLKem768");
            Assert.True(MLKemAlgorithm.MLKem768 != MLKemAlgorithm.MLKem1024, "MLKemAlgorithm.MLKem768 != MLKemAlgorithm.MLKem1024");
            Assert.True(MLKemAlgorithm.MLKem1024 != MLKemAlgorithm.MLKem512, "MLKemAlgorithm.MLKem1024 != MLKemAlgorithm.MLKem512");
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
