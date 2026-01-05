// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Security.Cryptography.SLHDsa.Tests
{
    public static class SlhDsaAlgorithmTests
    {
        [Fact]
        public static void AlgorithmsHaveExpectedParameters()
        {
            SlhDsaAlgorithm algorithm;

            algorithm = SlhDsaAlgorithm.SlhDsaSha2_128s;
            Assert.Equal("SLH-DSA-SHA2-128s", algorithm.Name);
            Assert.Equal(32, algorithm.PublicKeySizeInBytes);
            Assert.Equal(64, algorithm.PrivateKeySizeInBytes);
            Assert.Equal(7856, algorithm.SignatureSizeInBytes);

            algorithm = SlhDsaAlgorithm.SlhDsaShake128s;
            Assert.Equal("SLH-DSA-SHAKE-128s", algorithm.Name);
            Assert.Equal(32, algorithm.PublicKeySizeInBytes);
            Assert.Equal(64, algorithm.PrivateKeySizeInBytes);
            Assert.Equal(7856, algorithm.SignatureSizeInBytes);

            algorithm = SlhDsaAlgorithm.SlhDsaSha2_128f;
            Assert.Equal("SLH-DSA-SHA2-128f", algorithm.Name);
            Assert.Equal(32, algorithm.PublicKeySizeInBytes);
            Assert.Equal(64, algorithm.PrivateKeySizeInBytes);
            Assert.Equal(17088, algorithm.SignatureSizeInBytes);

            algorithm = SlhDsaAlgorithm.SlhDsaShake128f;
            Assert.Equal("SLH-DSA-SHAKE-128f", algorithm.Name);
            Assert.Equal(32, algorithm.PublicKeySizeInBytes);
            Assert.Equal(64, algorithm.PrivateKeySizeInBytes);
            Assert.Equal(17088, algorithm.SignatureSizeInBytes);

            algorithm = SlhDsaAlgorithm.SlhDsaSha2_192s;
            Assert.Equal("SLH-DSA-SHA2-192s", algorithm.Name);
            Assert.Equal(48, algorithm.PublicKeySizeInBytes);
            Assert.Equal(96, algorithm.PrivateKeySizeInBytes);
            Assert.Equal(16224, algorithm.SignatureSizeInBytes);

            algorithm = SlhDsaAlgorithm.SlhDsaShake192s;
            Assert.Equal("SLH-DSA-SHAKE-192s", algorithm.Name);
            Assert.Equal(48, algorithm.PublicKeySizeInBytes);
            Assert.Equal(96, algorithm.PrivateKeySizeInBytes);
            Assert.Equal(16224, algorithm.SignatureSizeInBytes);

            algorithm = SlhDsaAlgorithm.SlhDsaSha2_192f;
            Assert.Equal("SLH-DSA-SHA2-192f", algorithm.Name);
            Assert.Equal(48, algorithm.PublicKeySizeInBytes);
            Assert.Equal(96, algorithm.PrivateKeySizeInBytes);
            Assert.Equal(35664, algorithm.SignatureSizeInBytes);

            algorithm = SlhDsaAlgorithm.SlhDsaShake192f;
            Assert.Equal("SLH-DSA-SHAKE-192f", algorithm.Name);
            Assert.Equal(48, algorithm.PublicKeySizeInBytes);
            Assert.Equal(96, algorithm.PrivateKeySizeInBytes);
            Assert.Equal(35664, algorithm.SignatureSizeInBytes);

            algorithm = SlhDsaAlgorithm.SlhDsaSha2_256s;
            Assert.Equal("SLH-DSA-SHA2-256s", algorithm.Name);
            Assert.Equal(64, algorithm.PublicKeySizeInBytes);
            Assert.Equal(128, algorithm.PrivateKeySizeInBytes);
            Assert.Equal(29792, algorithm.SignatureSizeInBytes);

            algorithm = SlhDsaAlgorithm.SlhDsaShake256s;
            Assert.Equal("SLH-DSA-SHAKE-256s", algorithm.Name);
            Assert.Equal(64, algorithm.PublicKeySizeInBytes);
            Assert.Equal(128, algorithm.PrivateKeySizeInBytes);
            Assert.Equal(29792, algorithm.SignatureSizeInBytes);

            algorithm = SlhDsaAlgorithm.SlhDsaSha2_256f;
            Assert.Equal("SLH-DSA-SHA2-256f", algorithm.Name);
            Assert.Equal(64, algorithm.PublicKeySizeInBytes);
            Assert.Equal(128, algorithm.PrivateKeySizeInBytes);
            Assert.Equal(49856, algorithm.SignatureSizeInBytes);

            algorithm = SlhDsaAlgorithm.SlhDsaShake256f;
            Assert.Equal("SLH-DSA-SHAKE-256f", algorithm.Name);
            Assert.Equal(64, algorithm.PublicKeySizeInBytes);
            Assert.Equal(128, algorithm.PrivateKeySizeInBytes);
            Assert.Equal(49856, algorithm.SignatureSizeInBytes);
        }

        [Fact]
        public static void Algorithms_AreSame()
        {
            Assert.Same(SlhDsaAlgorithm.SlhDsaSha2_128s, SlhDsaAlgorithm.SlhDsaSha2_128s);
            Assert.Same(SlhDsaAlgorithm.SlhDsaShake128s, SlhDsaAlgorithm.SlhDsaShake128s);
            Assert.Same(SlhDsaAlgorithm.SlhDsaSha2_128f, SlhDsaAlgorithm.SlhDsaSha2_128f);
            Assert.Same(SlhDsaAlgorithm.SlhDsaShake128f, SlhDsaAlgorithm.SlhDsaShake128f);
            Assert.Same(SlhDsaAlgorithm.SlhDsaSha2_192s, SlhDsaAlgorithm.SlhDsaSha2_192s);
            Assert.Same(SlhDsaAlgorithm.SlhDsaShake192s, SlhDsaAlgorithm.SlhDsaShake192s);
            Assert.Same(SlhDsaAlgorithm.SlhDsaSha2_192f, SlhDsaAlgorithm.SlhDsaSha2_192f);
            Assert.Same(SlhDsaAlgorithm.SlhDsaShake192f, SlhDsaAlgorithm.SlhDsaShake192f);
            Assert.Same(SlhDsaAlgorithm.SlhDsaSha2_256s, SlhDsaAlgorithm.SlhDsaSha2_256s);
            Assert.Same(SlhDsaAlgorithm.SlhDsaShake256s, SlhDsaAlgorithm.SlhDsaShake256s);
            Assert.Same(SlhDsaAlgorithm.SlhDsaSha2_256f, SlhDsaAlgorithm.SlhDsaSha2_256f);
            Assert.Same(SlhDsaAlgorithm.SlhDsaShake256f, SlhDsaAlgorithm.SlhDsaShake256f);
        }

        [Theory]
        [MemberData(nameof(SlhDsaAlgorithms))]
        public static void Algorithms_Equal(SlhDsaAlgorithm algorithm)
        {
            AssertExtensions.TrueExpression(algorithm.Equals(algorithm));
            AssertExtensions.TrueExpression(algorithm.Equals((object)algorithm));
            AssertExtensions.FalseExpression(algorithm.Equals(null));
        }

        [Theory]
        [MemberData(nameof(SlhDsaAlgorithms))]
        public static void Algorithms_GetHashCode(SlhDsaAlgorithm algorithm)
        {
            Assert.Equal(algorithm.Name.GetHashCode(), algorithm.GetHashCode());
        }

        [Fact]
        public static void Algorithms_Equality()
        {
            AssertExtensions.TrueExpression(SlhDsaAlgorithm.SlhDsaSha2_128s == SlhDsaAlgorithm.SlhDsaSha2_128s);
            AssertExtensions.TrueExpression(SlhDsaAlgorithm.SlhDsaShake128s == SlhDsaAlgorithm.SlhDsaShake128s);
            AssertExtensions.TrueExpression(SlhDsaAlgorithm.SlhDsaSha2_128f == SlhDsaAlgorithm.SlhDsaSha2_128f);
            AssertExtensions.TrueExpression(SlhDsaAlgorithm.SlhDsaShake128f == SlhDsaAlgorithm.SlhDsaShake128f);
            AssertExtensions.TrueExpression(SlhDsaAlgorithm.SlhDsaSha2_192s == SlhDsaAlgorithm.SlhDsaSha2_192s);
            AssertExtensions.TrueExpression(SlhDsaAlgorithm.SlhDsaShake192s == SlhDsaAlgorithm.SlhDsaShake192s);
            AssertExtensions.TrueExpression(SlhDsaAlgorithm.SlhDsaSha2_192f == SlhDsaAlgorithm.SlhDsaSha2_192f);
            AssertExtensions.TrueExpression(SlhDsaAlgorithm.SlhDsaShake192f == SlhDsaAlgorithm.SlhDsaShake192f);
            AssertExtensions.TrueExpression(SlhDsaAlgorithm.SlhDsaSha2_256s == SlhDsaAlgorithm.SlhDsaSha2_256s);
            AssertExtensions.TrueExpression(SlhDsaAlgorithm.SlhDsaShake256s == SlhDsaAlgorithm.SlhDsaShake256s);
            AssertExtensions.TrueExpression(SlhDsaAlgorithm.SlhDsaSha2_256f == SlhDsaAlgorithm.SlhDsaSha2_256f);
            AssertExtensions.TrueExpression(SlhDsaAlgorithm.SlhDsaShake256f == SlhDsaAlgorithm.SlhDsaShake256f);

            AssertExtensions.FalseExpression(SlhDsaAlgorithm.SlhDsaSha2_128s == SlhDsaAlgorithm.SlhDsaShake128s);
            AssertExtensions.FalseExpression(SlhDsaAlgorithm.SlhDsaShake128s == SlhDsaAlgorithm.SlhDsaSha2_128f);
            AssertExtensions.FalseExpression(SlhDsaAlgorithm.SlhDsaSha2_128f == SlhDsaAlgorithm.SlhDsaShake128f);
            AssertExtensions.FalseExpression(SlhDsaAlgorithm.SlhDsaShake128f == SlhDsaAlgorithm.SlhDsaSha2_192s);
            AssertExtensions.FalseExpression(SlhDsaAlgorithm.SlhDsaSha2_192s == SlhDsaAlgorithm.SlhDsaShake192s);
            AssertExtensions.FalseExpression(SlhDsaAlgorithm.SlhDsaShake192s == SlhDsaAlgorithm.SlhDsaSha2_192f);
            AssertExtensions.FalseExpression(SlhDsaAlgorithm.SlhDsaSha2_192f == SlhDsaAlgorithm.SlhDsaShake192f);
            AssertExtensions.FalseExpression(SlhDsaAlgorithm.SlhDsaShake192f == SlhDsaAlgorithm.SlhDsaSha2_256s);
            AssertExtensions.FalseExpression(SlhDsaAlgorithm.SlhDsaSha2_256s == SlhDsaAlgorithm.SlhDsaShake256s);
            AssertExtensions.FalseExpression(SlhDsaAlgorithm.SlhDsaShake256s == SlhDsaAlgorithm.SlhDsaSha2_256f);
            AssertExtensions.FalseExpression(SlhDsaAlgorithm.SlhDsaSha2_256f == SlhDsaAlgorithm.SlhDsaShake256f);
            AssertExtensions.FalseExpression(SlhDsaAlgorithm.SlhDsaShake256f == SlhDsaAlgorithm.SlhDsaSha2_128s);
        }

        [Fact]
        public static void Algorithms_Inequality()
        {
            AssertExtensions.FalseExpression(SlhDsaAlgorithm.SlhDsaSha2_128s != SlhDsaAlgorithm.SlhDsaSha2_128s);
            AssertExtensions.FalseExpression(SlhDsaAlgorithm.SlhDsaShake128s != SlhDsaAlgorithm.SlhDsaShake128s);
            AssertExtensions.FalseExpression(SlhDsaAlgorithm.SlhDsaSha2_128f != SlhDsaAlgorithm.SlhDsaSha2_128f);
            AssertExtensions.FalseExpression(SlhDsaAlgorithm.SlhDsaShake128f != SlhDsaAlgorithm.SlhDsaShake128f);
            AssertExtensions.FalseExpression(SlhDsaAlgorithm.SlhDsaSha2_192s != SlhDsaAlgorithm.SlhDsaSha2_192s);
            AssertExtensions.FalseExpression(SlhDsaAlgorithm.SlhDsaShake192s != SlhDsaAlgorithm.SlhDsaShake192s);
            AssertExtensions.FalseExpression(SlhDsaAlgorithm.SlhDsaSha2_192f != SlhDsaAlgorithm.SlhDsaSha2_192f);
            AssertExtensions.FalseExpression(SlhDsaAlgorithm.SlhDsaShake192f != SlhDsaAlgorithm.SlhDsaShake192f);
            AssertExtensions.FalseExpression(SlhDsaAlgorithm.SlhDsaSha2_256s != SlhDsaAlgorithm.SlhDsaSha2_256s);
            AssertExtensions.FalseExpression(SlhDsaAlgorithm.SlhDsaShake256s != SlhDsaAlgorithm.SlhDsaShake256s);
            AssertExtensions.FalseExpression(SlhDsaAlgorithm.SlhDsaSha2_256f != SlhDsaAlgorithm.SlhDsaSha2_256f);
            AssertExtensions.FalseExpression(SlhDsaAlgorithm.SlhDsaShake256f != SlhDsaAlgorithm.SlhDsaShake256f);

            AssertExtensions.TrueExpression(SlhDsaAlgorithm.SlhDsaSha2_128s != SlhDsaAlgorithm.SlhDsaShake128s);
            AssertExtensions.TrueExpression(SlhDsaAlgorithm.SlhDsaShake128s != SlhDsaAlgorithm.SlhDsaSha2_128f);
            AssertExtensions.TrueExpression(SlhDsaAlgorithm.SlhDsaSha2_128f != SlhDsaAlgorithm.SlhDsaShake128f);
            AssertExtensions.TrueExpression(SlhDsaAlgorithm.SlhDsaShake128f != SlhDsaAlgorithm.SlhDsaSha2_192s);
            AssertExtensions.TrueExpression(SlhDsaAlgorithm.SlhDsaSha2_192s != SlhDsaAlgorithm.SlhDsaShake192s);
            AssertExtensions.TrueExpression(SlhDsaAlgorithm.SlhDsaShake192s != SlhDsaAlgorithm.SlhDsaSha2_192f);
            AssertExtensions.TrueExpression(SlhDsaAlgorithm.SlhDsaSha2_192f != SlhDsaAlgorithm.SlhDsaShake192f);
            AssertExtensions.TrueExpression(SlhDsaAlgorithm.SlhDsaShake192f != SlhDsaAlgorithm.SlhDsaSha2_256s);
            AssertExtensions.TrueExpression(SlhDsaAlgorithm.SlhDsaSha2_256s != SlhDsaAlgorithm.SlhDsaShake256s);
            AssertExtensions.TrueExpression(SlhDsaAlgorithm.SlhDsaShake256s != SlhDsaAlgorithm.SlhDsaSha2_256f);
            AssertExtensions.TrueExpression(SlhDsaAlgorithm.SlhDsaSha2_256f != SlhDsaAlgorithm.SlhDsaShake256f);
            AssertExtensions.TrueExpression(SlhDsaAlgorithm.SlhDsaShake256f != SlhDsaAlgorithm.SlhDsaSha2_128s);
        }

        [Theory]
        [MemberData(nameof(SlhDsaAlgorithms))]
        public static void Algorithms_ToString(SlhDsaAlgorithm algorithm)
        {
            Assert.Equal(algorithm.Name, algorithm.ToString());
        }

        public static IEnumerable<object[]> SlhDsaAlgorithms()
        {
            yield return new object[] { SlhDsaAlgorithm.SlhDsaSha2_128s };
            yield return new object[] { SlhDsaAlgorithm.SlhDsaShake128s };
            yield return new object[] { SlhDsaAlgorithm.SlhDsaSha2_128f };
            yield return new object[] { SlhDsaAlgorithm.SlhDsaShake128f };
            yield return new object[] { SlhDsaAlgorithm.SlhDsaSha2_192s };
            yield return new object[] { SlhDsaAlgorithm.SlhDsaShake192s };
            yield return new object[] { SlhDsaAlgorithm.SlhDsaSha2_192f };
            yield return new object[] { SlhDsaAlgorithm.SlhDsaShake192f };
            yield return new object[] { SlhDsaAlgorithm.SlhDsaSha2_256s };
            yield return new object[] { SlhDsaAlgorithm.SlhDsaShake256s };
            yield return new object[] { SlhDsaAlgorithm.SlhDsaSha2_256f };
            yield return new object[] { SlhDsaAlgorithm.SlhDsaShake256f };
        }
    }
}
