// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    public static class MLDsaAlgorithmTests
    {
        [Fact]
        public static void Algorithms_AreSame()
        {
            Assert.Same(MLDsaAlgorithm.MLDsa44, MLDsaAlgorithm.MLDsa44);
            Assert.Same(MLDsaAlgorithm.MLDsa65, MLDsaAlgorithm.MLDsa65);
            Assert.Same(MLDsaAlgorithm.MLDsa87, MLDsaAlgorithm.MLDsa87);
        }

        [Theory]
        [MemberData(nameof(MLDsaAlgorithms))]
        public static void Algorithms_Equal(MLDsaAlgorithm algorithm)
        {
            AssertExtensions.TrueExpression(algorithm.Equals(algorithm));
            AssertExtensions.TrueExpression(algorithm.Equals((object)algorithm));
            AssertExtensions.FalseExpression(algorithm.Equals(null));
        }

        [Theory]
        [MemberData(nameof(MLDsaAlgorithms))]
        public static void Algorithms_GetHashCode(MLDsaAlgorithm algorithm)
        {
            Assert.Equal(algorithm.Name.GetHashCode(), algorithm.GetHashCode());
        }

        [Fact]
        public static void Algorithms_Equality()
        {
            AssertExtensions.TrueExpression(MLDsaAlgorithm.MLDsa44 == MLDsaAlgorithm.MLDsa44);
            AssertExtensions.TrueExpression(MLDsaAlgorithm.MLDsa65 == MLDsaAlgorithm.MLDsa65);
            AssertExtensions.TrueExpression(MLDsaAlgorithm.MLDsa87 == MLDsaAlgorithm.MLDsa87);

            AssertExtensions.FalseExpression(MLDsaAlgorithm.MLDsa44 == MLDsaAlgorithm.MLDsa65);
            AssertExtensions.FalseExpression(MLDsaAlgorithm.MLDsa65 == MLDsaAlgorithm.MLDsa87);
            AssertExtensions.FalseExpression(MLDsaAlgorithm.MLDsa87 == MLDsaAlgorithm.MLDsa44);
        }

        [Fact]
        public static void Algorithms_Inequality()
        {
            AssertExtensions.FalseExpression(MLDsaAlgorithm.MLDsa44 != MLDsaAlgorithm.MLDsa44);
            AssertExtensions.FalseExpression(MLDsaAlgorithm.MLDsa65 != MLDsaAlgorithm.MLDsa65);
            AssertExtensions.FalseExpression(MLDsaAlgorithm.MLDsa87 != MLDsaAlgorithm.MLDsa87);

            AssertExtensions.TrueExpression(MLDsaAlgorithm.MLDsa44 != MLDsaAlgorithm.MLDsa65);
            AssertExtensions.TrueExpression(MLDsaAlgorithm.MLDsa65 != MLDsaAlgorithm.MLDsa87);
            AssertExtensions.TrueExpression(MLDsaAlgorithm.MLDsa87 != MLDsaAlgorithm.MLDsa44);
        }

        [Theory]
        [MemberData(nameof(MLDsaAlgorithms))]
        public static void Algorithms_ToString(MLDsaAlgorithm algorithm)
        {
            Assert.Equal(algorithm.Name, algorithm.ToString());
        }

        public static IEnumerable<object[]> MLDsaAlgorithms()
        {
            yield return new object[] { MLDsaAlgorithm.MLDsa44 };
            yield return new object[] { MLDsaAlgorithm.MLDsa65 };
            yield return new object[] { MLDsaAlgorithm.MLDsa87 };
        }
    }
}
