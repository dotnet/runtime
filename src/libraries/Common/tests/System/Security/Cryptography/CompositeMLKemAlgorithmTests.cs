// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    public static class CompositeMLKemAlgorithmTests
    {
        [Fact]
        public static void AlgorithmsHaveExpectedParameters()
        {
            CompositeMLKemAlgorithm algorithm;

            algorithm = CompositeMLKemAlgorithm.MLKem768WithRsaOaep2048;
            Assert.Equal("MLKEM768-RSA2048-SHA3-256", algorithm.Name);
            Assert.Equal(1088 + 256, algorithm.CiphertextSizeInBytes); // ML-KEM + Traditional ciphertext
            Assert.Equal(32, algorithm.SharedSecretSizeInBytes);

            algorithm = CompositeMLKemAlgorithm.MLKem768WithRsaOaep3072;
            Assert.Equal("MLKEM768-RSA3072-SHA3-256", algorithm.Name);
            Assert.Equal(1088 + 384, algorithm.CiphertextSizeInBytes);
            Assert.Equal(32, algorithm.SharedSecretSizeInBytes);

            algorithm = CompositeMLKemAlgorithm.MLKem768WithRsaOaep4096;
            Assert.Equal("MLKEM768-RSA4096-SHA3-256", algorithm.Name);
            Assert.Equal(1088 + 512, algorithm.CiphertextSizeInBytes);
            Assert.Equal(32, algorithm.SharedSecretSizeInBytes);

            algorithm = CompositeMLKemAlgorithm.MLKem768WithX25519;
            Assert.Equal("MLKEM768-X25519-SHA3-256", algorithm.Name);
            Assert.Equal(1088 + 32, algorithm.CiphertextSizeInBytes);
            Assert.Equal(32, algorithm.SharedSecretSizeInBytes);

            algorithm = CompositeMLKemAlgorithm.MLKem768WithECDiffieHellmanP256;
            Assert.Equal("MLKEM768-ECDH-P256-SHA3-256", algorithm.Name);
            Assert.Equal(1088 + 65, algorithm.CiphertextSizeInBytes);
            Assert.Equal(32, algorithm.SharedSecretSizeInBytes);

            algorithm = CompositeMLKemAlgorithm.MLKem768WithECDiffieHellmanP384;
            Assert.Equal("MLKEM768-ECDH-P384-SHA3-256", algorithm.Name);
            Assert.Equal(1088 + 97, algorithm.CiphertextSizeInBytes);
            Assert.Equal(32, algorithm.SharedSecretSizeInBytes);

            algorithm = CompositeMLKemAlgorithm.MLKem768WithECDiffieHellmanBrainpoolP256r1;
            Assert.Equal("MLKEM768-ECDH-brainpoolP256r1-SHA3-256", algorithm.Name);
            Assert.Equal(1088 + 65, algorithm.CiphertextSizeInBytes);
            Assert.Equal(32, algorithm.SharedSecretSizeInBytes);

            algorithm = CompositeMLKemAlgorithm.MLKem1024WithRsaOaep3072;
            Assert.Equal("MLKEM1024-RSA3072-SHA3-256", algorithm.Name);
            Assert.Equal(1568 + 384, algorithm.CiphertextSizeInBytes);
            Assert.Equal(32, algorithm.SharedSecretSizeInBytes);

            algorithm = CompositeMLKemAlgorithm.MLKem1024WithECDiffieHellmanP384;
            Assert.Equal("MLKEM1024-ECDH-P384-SHA3-256", algorithm.Name);
            Assert.Equal(1568 + 97, algorithm.CiphertextSizeInBytes);
            Assert.Equal(32, algorithm.SharedSecretSizeInBytes);

            algorithm = CompositeMLKemAlgorithm.MLKem1024WithECDiffieHellmanBrainpoolP384r1;
            Assert.Equal("MLKEM1024-ECDH-brainpoolP384r1-SHA3-256", algorithm.Name);
            Assert.Equal(1568 + 97, algorithm.CiphertextSizeInBytes);
            Assert.Equal(32, algorithm.SharedSecretSizeInBytes);

            algorithm = CompositeMLKemAlgorithm.MLKem1024WithX448;
            Assert.Equal("MLKEM1024-X448-SHA3-256", algorithm.Name);
            Assert.Equal(1568 + 56, algorithm.CiphertextSizeInBytes);
            Assert.Equal(32, algorithm.SharedSecretSizeInBytes);

            algorithm = CompositeMLKemAlgorithm.MLKem1024WithECDiffieHellmanP521;
            Assert.Equal("MLKEM1024-ECDH-P521-SHA3-256", algorithm.Name);
            Assert.Equal(1568 + 133, algorithm.CiphertextSizeInBytes);
            Assert.Equal(32, algorithm.SharedSecretSizeInBytes);
        }

        [Fact]
        public static void Algorithms_AreSame()
        {
            Assert.Same(CompositeMLKemAlgorithm.MLKem768WithRsaOaep2048, CompositeMLKemAlgorithm.MLKem768WithRsaOaep2048);
            Assert.Same(CompositeMLKemAlgorithm.MLKem768WithRsaOaep3072, CompositeMLKemAlgorithm.MLKem768WithRsaOaep3072);
            Assert.Same(CompositeMLKemAlgorithm.MLKem768WithRsaOaep4096, CompositeMLKemAlgorithm.MLKem768WithRsaOaep4096);
            Assert.Same(CompositeMLKemAlgorithm.MLKem768WithX25519, CompositeMLKemAlgorithm.MLKem768WithX25519);
            Assert.Same(CompositeMLKemAlgorithm.MLKem768WithECDiffieHellmanP256, CompositeMLKemAlgorithm.MLKem768WithECDiffieHellmanP256);
            Assert.Same(CompositeMLKemAlgorithm.MLKem768WithECDiffieHellmanP384, CompositeMLKemAlgorithm.MLKem768WithECDiffieHellmanP384);
            Assert.Same(CompositeMLKemAlgorithm.MLKem768WithECDiffieHellmanBrainpoolP256r1, CompositeMLKemAlgorithm.MLKem768WithECDiffieHellmanBrainpoolP256r1);
            Assert.Same(CompositeMLKemAlgorithm.MLKem1024WithRsaOaep3072, CompositeMLKemAlgorithm.MLKem1024WithRsaOaep3072);
            Assert.Same(CompositeMLKemAlgorithm.MLKem1024WithECDiffieHellmanP384, CompositeMLKemAlgorithm.MLKem1024WithECDiffieHellmanP384);
            Assert.Same(CompositeMLKemAlgorithm.MLKem1024WithECDiffieHellmanBrainpoolP384r1, CompositeMLKemAlgorithm.MLKem1024WithECDiffieHellmanBrainpoolP384r1);
            Assert.Same(CompositeMLKemAlgorithm.MLKem1024WithX448, CompositeMLKemAlgorithm.MLKem1024WithX448);
            Assert.Same(CompositeMLKemAlgorithm.MLKem1024WithECDiffieHellmanP521, CompositeMLKemAlgorithm.MLKem1024WithECDiffieHellmanP521);
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemAlgorithms))]
        public static void Algorithms_Equal(CompositeMLKemAlgorithm algorithm)
        {
            AssertExtensions.TrueExpression(algorithm.Equals(algorithm));
            AssertExtensions.TrueExpression(algorithm.Equals((object)algorithm));
            AssertExtensions.FalseExpression(algorithm.Equals(null));
            AssertExtensions.FalseExpression(algorithm.Equals((object)algorithm.Name));
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemAlgorithms))]
        public static void Algorithms_GetHashCode(CompositeMLKemAlgorithm algorithm)
        {
            Assert.Equal(algorithm.Name.GetHashCode(), algorithm.GetHashCode());
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemAlgorithms))]
        public static void Algorithms_ToString(CompositeMLKemAlgorithm algorithm)
        {
            Assert.Equal(algorithm.Name, algorithm.ToString());
        }

        [Fact]
        public static void Algorithms_Equality()
        {
            AssertExtensions.TrueExpression(CompositeMLKemAlgorithm.MLKem768WithRsaOaep2048 == CompositeMLKemAlgorithm.MLKem768WithRsaOaep2048);
            AssertExtensions.TrueExpression(CompositeMLKemAlgorithm.MLKem768WithX25519 == CompositeMLKemAlgorithm.MLKem768WithX25519);
            AssertExtensions.TrueExpression(CompositeMLKemAlgorithm.MLKem1024WithX448 == CompositeMLKemAlgorithm.MLKem1024WithX448);
            AssertExtensions.TrueExpression(CompositeMLKemAlgorithm.MLKem1024WithECDiffieHellmanP521 == CompositeMLKemAlgorithm.MLKem1024WithECDiffieHellmanP521);

            // Test some cross-combinations are false
            AssertExtensions.FalseExpression(CompositeMLKemAlgorithm.MLKem768WithRsaOaep2048 == CompositeMLKemAlgorithm.MLKem768WithRsaOaep3072);
            AssertExtensions.FalseExpression(CompositeMLKemAlgorithm.MLKem768WithRsaOaep3072 == CompositeMLKemAlgorithm.MLKem1024WithRsaOaep3072);
            AssertExtensions.FalseExpression(CompositeMLKemAlgorithm.MLKem768WithECDiffieHellmanP384 == CompositeMLKemAlgorithm.MLKem1024WithECDiffieHellmanP384);
            AssertExtensions.FalseExpression(CompositeMLKemAlgorithm.MLKem768WithX25519 == CompositeMLKemAlgorithm.MLKem1024WithX448);

            AssertExtensions.FalseExpression(CompositeMLKemAlgorithm.MLKem768WithX25519 == null);
            AssertExtensions.FalseExpression(null == CompositeMLKemAlgorithm.MLKem768WithX25519);
            AssertExtensions.TrueExpression((CompositeMLKemAlgorithm)null == (CompositeMLKemAlgorithm)null);
        }

        [Fact]
        public static void Algorithms_Inequality()
        {
            AssertExtensions.FalseExpression(CompositeMLKemAlgorithm.MLKem768WithRsaOaep2048 != CompositeMLKemAlgorithm.MLKem768WithRsaOaep2048);
            AssertExtensions.FalseExpression(CompositeMLKemAlgorithm.MLKem768WithX25519 != CompositeMLKemAlgorithm.MLKem768WithX25519);

            // Test some cross-combinations are true
            AssertExtensions.TrueExpression(CompositeMLKemAlgorithm.MLKem768WithRsaOaep2048 != CompositeMLKemAlgorithm.MLKem768WithRsaOaep3072);
            AssertExtensions.TrueExpression(CompositeMLKemAlgorithm.MLKem768WithECDiffieHellmanP384 != CompositeMLKemAlgorithm.MLKem1024WithECDiffieHellmanP384);
            AssertExtensions.TrueExpression(CompositeMLKemAlgorithm.MLKem768WithECDiffieHellmanBrainpoolP256r1 != CompositeMLKemAlgorithm.MLKem1024WithECDiffieHellmanBrainpoolP384r1);

            AssertExtensions.TrueExpression(CompositeMLKemAlgorithm.MLKem768WithX25519 != null);
            AssertExtensions.TrueExpression(null != CompositeMLKemAlgorithm.MLKem768WithX25519);
            AssertExtensions.FalseExpression((CompositeMLKemAlgorithm)null != (CompositeMLKemAlgorithm)null);
        }

        public static IEnumerable<object[]> CompositeMLKemAlgorithms()
        {
            yield return new object[] { CompositeMLKemAlgorithm.MLKem768WithRsaOaep2048 };
            yield return new object[] { CompositeMLKemAlgorithm.MLKem768WithRsaOaep3072 };
            yield return new object[] { CompositeMLKemAlgorithm.MLKem768WithRsaOaep4096 };
            yield return new object[] { CompositeMLKemAlgorithm.MLKem768WithX25519 };
            yield return new object[] { CompositeMLKemAlgorithm.MLKem768WithECDiffieHellmanP256 };
            yield return new object[] { CompositeMLKemAlgorithm.MLKem768WithECDiffieHellmanP384 };
            yield return new object[] { CompositeMLKemAlgorithm.MLKem768WithECDiffieHellmanBrainpoolP256r1 };
            yield return new object[] { CompositeMLKemAlgorithm.MLKem1024WithRsaOaep3072 };
            yield return new object[] { CompositeMLKemAlgorithm.MLKem1024WithECDiffieHellmanP384 };
            yield return new object[] { CompositeMLKemAlgorithm.MLKem1024WithECDiffieHellmanBrainpoolP384r1 };
            yield return new object[] { CompositeMLKemAlgorithm.MLKem1024WithX448 };
            yield return new object[] { CompositeMLKemAlgorithm.MLKem1024WithECDiffieHellmanP521 };
        }
    }
}
