// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    public static class CompositeMLDsaAlgorithmTests
    {
        [Fact]
        public static void AlgorithmsHaveExpectedParameters()
        {
            CompositeMLDsaAlgorithm algorithm;

            algorithm = CompositeMLDsaAlgorithm.MLDsa44WithRSA2048Pss;
            Assert.Equal("MLDSA44-RSA2048-PSS-SHA256", algorithm.Name);
            Assert.Equal(32 + 2420 + 256, algorithm.MaxSignatureSizeInBytes); // Randomizer + ML-DSA + Traditional Signature

            algorithm = CompositeMLDsaAlgorithm.MLDsa44WithRSA2048Pkcs15;
            Assert.Equal("MLDSA44-RSA2048-PKCS15-SHA256", algorithm.Name);
            Assert.Equal(32 + 2420 + 256, algorithm.MaxSignatureSizeInBytes);

            algorithm = CompositeMLDsaAlgorithm.MLDsa44WithEd25519;
            Assert.Equal("MLDSA44-Ed25519-SHA512", algorithm.Name);
            Assert.Equal(32 + 2420 + 64, algorithm.MaxSignatureSizeInBytes);

            algorithm = CompositeMLDsaAlgorithm.MLDsa44WithECDsaP256;
            Assert.Equal("MLDSA44-ECDSA-P256-SHA256", algorithm.Name);
            Assert.Equal(32 + 2420 + 72, algorithm.MaxSignatureSizeInBytes);

            algorithm = CompositeMLDsaAlgorithm.MLDsa65WithRSA3072Pss;
            Assert.Equal("MLDSA65-RSA3072-PSS-SHA512", algorithm.Name);
            Assert.Equal(32 + 3309 + 384, algorithm.MaxSignatureSizeInBytes);

            algorithm = CompositeMLDsaAlgorithm.MLDsa65WithRSA3072Pkcs15;
            Assert.Equal("MLDSA65-RSA3072-PKCS15-SHA512", algorithm.Name);
            Assert.Equal(32 + 3309 + 384, algorithm.MaxSignatureSizeInBytes);

            algorithm = CompositeMLDsaAlgorithm.MLDsa65WithRSA4096Pss;
            Assert.Equal("MLDSA65-RSA4096-PSS-SHA512", algorithm.Name);
            Assert.Equal(32 + 3309 + 512, algorithm.MaxSignatureSizeInBytes);

            algorithm = CompositeMLDsaAlgorithm.MLDsa65WithRSA4096Pkcs15;
            Assert.Equal("MLDSA65-RSA4096-PKCS15-SHA512", algorithm.Name);
            Assert.Equal(32 + 3309 + 512, algorithm.MaxSignatureSizeInBytes);

            algorithm = CompositeMLDsaAlgorithm.MLDsa65WithECDsaP256;
            Assert.Equal("MLDSA65-ECDSA-P256-SHA512", algorithm.Name);
            Assert.Equal(32 + 3309 + 72, algorithm.MaxSignatureSizeInBytes);

            algorithm = CompositeMLDsaAlgorithm.MLDsa65WithECDsaP384;
            Assert.Equal("MLDSA65-ECDSA-P384-SHA512", algorithm.Name);
            Assert.Equal(32 + 3309 + 104, algorithm.MaxSignatureSizeInBytes);

            algorithm = CompositeMLDsaAlgorithm.MLDsa65WithECDsaBrainpoolP256r1;
            Assert.Equal("MLDSA65-ECDSA-brainpoolP256r1-SHA512", algorithm.Name);
            Assert.Equal(32 + 3309 + 72, algorithm.MaxSignatureSizeInBytes);

            algorithm = CompositeMLDsaAlgorithm.MLDsa65WithEd25519;
            Assert.Equal("MLDSA65-Ed25519-SHA512", algorithm.Name);
            Assert.Equal(32 + 3309 + 64, algorithm.MaxSignatureSizeInBytes);

            algorithm = CompositeMLDsaAlgorithm.MLDsa87WithECDsaP384;
            Assert.Equal("MLDSA87-ECDSA-P384-SHA512", algorithm.Name);
            Assert.Equal(32 + 4627 + 104, algorithm.MaxSignatureSizeInBytes);

            algorithm = CompositeMLDsaAlgorithm.MLDsa87WithECDsaBrainpoolP384r1;
            Assert.Equal("MLDSA87-ECDSA-brainpoolP384r1-SHA512", algorithm.Name);
            Assert.Equal(32 + 4627 + 104, algorithm.MaxSignatureSizeInBytes);

            algorithm = CompositeMLDsaAlgorithm.MLDsa87WithEd448;
            Assert.Equal("MLDSA87-Ed448-SHAKE256", algorithm.Name);
            Assert.Equal(32 + 4627 + 114, algorithm.MaxSignatureSizeInBytes);

            algorithm = CompositeMLDsaAlgorithm.MLDsa87WithRSA3072Pss;
            Assert.Equal("MLDSA87-RSA3072-PSS-SHA512", algorithm.Name);
            Assert.Equal(32 + 4627 + 384, algorithm.MaxSignatureSizeInBytes);

            algorithm = CompositeMLDsaAlgorithm.MLDsa87WithRSA4096Pss;
            Assert.Equal("MLDSA87-RSA4096-PSS-SHA512", algorithm.Name);
            Assert.Equal(32 + 4627 + 512, algorithm.MaxSignatureSizeInBytes);

            algorithm = CompositeMLDsaAlgorithm.MLDsa87WithECDsaP521;
            Assert.Equal("MLDSA87-ECDSA-P521-SHA512", algorithm.Name);
            Assert.Equal(32 + 4627 + 139, algorithm.MaxSignatureSizeInBytes);
        }

        [Fact]
        public static void Algorithms_AreSame()
        {
            Assert.Same(CompositeMLDsaAlgorithm.MLDsa44WithRSA2048Pss, CompositeMLDsaAlgorithm.MLDsa44WithRSA2048Pss);
            Assert.Same(CompositeMLDsaAlgorithm.MLDsa44WithRSA2048Pkcs15, CompositeMLDsaAlgorithm.MLDsa44WithRSA2048Pkcs15);
            Assert.Same(CompositeMLDsaAlgorithm.MLDsa44WithEd25519, CompositeMLDsaAlgorithm.MLDsa44WithEd25519);
            Assert.Same(CompositeMLDsaAlgorithm.MLDsa44WithECDsaP256, CompositeMLDsaAlgorithm.MLDsa44WithECDsaP256);
            Assert.Same(CompositeMLDsaAlgorithm.MLDsa65WithRSA3072Pss, CompositeMLDsaAlgorithm.MLDsa65WithRSA3072Pss);
            Assert.Same(CompositeMLDsaAlgorithm.MLDsa65WithRSA3072Pkcs15, CompositeMLDsaAlgorithm.MLDsa65WithRSA3072Pkcs15);
            Assert.Same(CompositeMLDsaAlgorithm.MLDsa65WithRSA4096Pss, CompositeMLDsaAlgorithm.MLDsa65WithRSA4096Pss);
            Assert.Same(CompositeMLDsaAlgorithm.MLDsa65WithRSA4096Pkcs15, CompositeMLDsaAlgorithm.MLDsa65WithRSA4096Pkcs15);
            Assert.Same(CompositeMLDsaAlgorithm.MLDsa65WithECDsaP256, CompositeMLDsaAlgorithm.MLDsa65WithECDsaP256);
            Assert.Same(CompositeMLDsaAlgorithm.MLDsa65WithECDsaP384, CompositeMLDsaAlgorithm.MLDsa65WithECDsaP384);
            Assert.Same(CompositeMLDsaAlgorithm.MLDsa65WithECDsaBrainpoolP256r1, CompositeMLDsaAlgorithm.MLDsa65WithECDsaBrainpoolP256r1);
            Assert.Same(CompositeMLDsaAlgorithm.MLDsa65WithEd25519, CompositeMLDsaAlgorithm.MLDsa65WithEd25519);
            Assert.Same(CompositeMLDsaAlgorithm.MLDsa87WithECDsaP384, CompositeMLDsaAlgorithm.MLDsa87WithECDsaP384);
            Assert.Same(CompositeMLDsaAlgorithm.MLDsa87WithECDsaBrainpoolP384r1, CompositeMLDsaAlgorithm.MLDsa87WithECDsaBrainpoolP384r1);
            Assert.Same(CompositeMLDsaAlgorithm.MLDsa87WithEd448, CompositeMLDsaAlgorithm.MLDsa87WithEd448);
            Assert.Same(CompositeMLDsaAlgorithm.MLDsa87WithRSA3072Pss, CompositeMLDsaAlgorithm.MLDsa87WithRSA3072Pss);
            Assert.Same(CompositeMLDsaAlgorithm.MLDsa87WithRSA4096Pss, CompositeMLDsaAlgorithm.MLDsa87WithRSA4096Pss);
            Assert.Same(CompositeMLDsaAlgorithm.MLDsa87WithECDsaP521, CompositeMLDsaAlgorithm.MLDsa87WithECDsaP521);
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaAlgorithms))]
        public static void Algorithms_Equal(CompositeMLDsaAlgorithm algorithm)
        {
            AssertExtensions.TrueExpression(algorithm.Equals(algorithm));
            AssertExtensions.TrueExpression(algorithm.Equals((object)algorithm));
            AssertExtensions.FalseExpression(algorithm.Equals(null));
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaAlgorithms))]
        public static void Algorithms_GetHashCode(CompositeMLDsaAlgorithm algorithm)
        {
            Assert.Equal(algorithm.Name.GetHashCode(), algorithm.GetHashCode());
        }

        [Fact]
        public static void Algorithms_Equality()
        {
            AssertExtensions.TrueExpression(CompositeMLDsaAlgorithm.MLDsa44WithRSA2048Pss == CompositeMLDsaAlgorithm.MLDsa44WithRSA2048Pss);
            AssertExtensions.TrueExpression(CompositeMLDsaAlgorithm.MLDsa44WithRSA2048Pkcs15 == CompositeMLDsaAlgorithm.MLDsa44WithRSA2048Pkcs15);
            AssertExtensions.TrueExpression(CompositeMLDsaAlgorithm.MLDsa44WithEd25519 == CompositeMLDsaAlgorithm.MLDsa44WithEd25519);
            AssertExtensions.TrueExpression(CompositeMLDsaAlgorithm.MLDsa44WithECDsaP256 == CompositeMLDsaAlgorithm.MLDsa44WithECDsaP256);
            AssertExtensions.TrueExpression(CompositeMLDsaAlgorithm.MLDsa65WithRSA3072Pss == CompositeMLDsaAlgorithm.MLDsa65WithRSA3072Pss);
            AssertExtensions.TrueExpression(CompositeMLDsaAlgorithm.MLDsa65WithRSA3072Pkcs15 == CompositeMLDsaAlgorithm.MLDsa65WithRSA3072Pkcs15);
            AssertExtensions.TrueExpression(CompositeMLDsaAlgorithm.MLDsa65WithRSA4096Pss == CompositeMLDsaAlgorithm.MLDsa65WithRSA4096Pss);
            AssertExtensions.TrueExpression(CompositeMLDsaAlgorithm.MLDsa65WithRSA4096Pkcs15 == CompositeMLDsaAlgorithm.MLDsa65WithRSA4096Pkcs15);
            AssertExtensions.TrueExpression(CompositeMLDsaAlgorithm.MLDsa65WithECDsaP256 == CompositeMLDsaAlgorithm.MLDsa65WithECDsaP256);
            AssertExtensions.TrueExpression(CompositeMLDsaAlgorithm.MLDsa65WithECDsaP384 == CompositeMLDsaAlgorithm.MLDsa65WithECDsaP384);
            AssertExtensions.TrueExpression(CompositeMLDsaAlgorithm.MLDsa65WithECDsaBrainpoolP256r1 == CompositeMLDsaAlgorithm.MLDsa65WithECDsaBrainpoolP256r1);
            AssertExtensions.TrueExpression(CompositeMLDsaAlgorithm.MLDsa65WithEd25519 == CompositeMLDsaAlgorithm.MLDsa65WithEd25519);
            AssertExtensions.TrueExpression(CompositeMLDsaAlgorithm.MLDsa87WithECDsaP384 == CompositeMLDsaAlgorithm.MLDsa87WithECDsaP384);
            AssertExtensions.TrueExpression(CompositeMLDsaAlgorithm.MLDsa87WithECDsaBrainpoolP384r1 == CompositeMLDsaAlgorithm.MLDsa87WithECDsaBrainpoolP384r1);
            AssertExtensions.TrueExpression(CompositeMLDsaAlgorithm.MLDsa87WithEd448 == CompositeMLDsaAlgorithm.MLDsa87WithEd448);
            AssertExtensions.TrueExpression(CompositeMLDsaAlgorithm.MLDsa87WithRSA3072Pss == CompositeMLDsaAlgorithm.MLDsa87WithRSA3072Pss);
            AssertExtensions.TrueExpression(CompositeMLDsaAlgorithm.MLDsa87WithRSA4096Pss == CompositeMLDsaAlgorithm.MLDsa87WithRSA4096Pss);
            AssertExtensions.TrueExpression(CompositeMLDsaAlgorithm.MLDsa87WithECDsaP521 == CompositeMLDsaAlgorithm.MLDsa87WithECDsaP521);

            // Test some cross-combinations are false
            AssertExtensions.FalseExpression(CompositeMLDsaAlgorithm.MLDsa44WithRSA2048Pss == CompositeMLDsaAlgorithm.MLDsa44WithRSA2048Pkcs15);
            AssertExtensions.FalseExpression(CompositeMLDsaAlgorithm.MLDsa44WithEd25519 == CompositeMLDsaAlgorithm.MLDsa65WithEd25519);
            AssertExtensions.FalseExpression(CompositeMLDsaAlgorithm.MLDsa65WithRSA3072Pss == CompositeMLDsaAlgorithm.MLDsa87WithRSA3072Pss);
            AssertExtensions.FalseExpression(CompositeMLDsaAlgorithm.MLDsa87WithECDsaP384 == CompositeMLDsaAlgorithm.MLDsa65WithECDsaP384);
        }

        [Fact]
        public static void Algorithms_Inequality()
        {
            AssertExtensions.FalseExpression(CompositeMLDsaAlgorithm.MLDsa44WithRSA2048Pss != CompositeMLDsaAlgorithm.MLDsa44WithRSA2048Pss);
            AssertExtensions.FalseExpression(CompositeMLDsaAlgorithm.MLDsa44WithRSA2048Pkcs15 != CompositeMLDsaAlgorithm.MLDsa44WithRSA2048Pkcs15);
            AssertExtensions.FalseExpression(CompositeMLDsaAlgorithm.MLDsa44WithEd25519 != CompositeMLDsaAlgorithm.MLDsa44WithEd25519);
            AssertExtensions.FalseExpression(CompositeMLDsaAlgorithm.MLDsa44WithECDsaP256 != CompositeMLDsaAlgorithm.MLDsa44WithECDsaP256);

            // Test some cross-combinations are true
            AssertExtensions.TrueExpression(CompositeMLDsaAlgorithm.MLDsa44WithRSA2048Pss != CompositeMLDsaAlgorithm.MLDsa44WithRSA2048Pkcs15);
            AssertExtensions.TrueExpression(CompositeMLDsaAlgorithm.MLDsa44WithEd25519 != CompositeMLDsaAlgorithm.MLDsa65WithEd25519);
            AssertExtensions.TrueExpression(CompositeMLDsaAlgorithm.MLDsa65WithRSA3072Pss != CompositeMLDsaAlgorithm.MLDsa87WithRSA3072Pss);
            AssertExtensions.TrueExpression(CompositeMLDsaAlgorithm.MLDsa87WithECDsaP384 != CompositeMLDsaAlgorithm.MLDsa65WithECDsaP384);
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaAlgorithms))]
        public static void Algorithms_ToString(CompositeMLDsaAlgorithm algorithm)
        {
            Assert.Equal(algorithm.Name, algorithm.ToString());
        }

        public static IEnumerable<object[]> CompositeMLDsaAlgorithms()
        {
            yield return new object[] { CompositeMLDsaAlgorithm.MLDsa44WithRSA2048Pss };
            yield return new object[] { CompositeMLDsaAlgorithm.MLDsa44WithRSA2048Pkcs15 };
            yield return new object[] { CompositeMLDsaAlgorithm.MLDsa44WithEd25519 };
            yield return new object[] { CompositeMLDsaAlgorithm.MLDsa44WithECDsaP256 };
            yield return new object[] { CompositeMLDsaAlgorithm.MLDsa65WithRSA3072Pss };
            yield return new object[] { CompositeMLDsaAlgorithm.MLDsa65WithRSA3072Pkcs15 };
            yield return new object[] { CompositeMLDsaAlgorithm.MLDsa65WithRSA4096Pss };
            yield return new object[] { CompositeMLDsaAlgorithm.MLDsa65WithRSA4096Pkcs15 };
            yield return new object[] { CompositeMLDsaAlgorithm.MLDsa65WithECDsaP256 };
            yield return new object[] { CompositeMLDsaAlgorithm.MLDsa65WithECDsaP384 };
            yield return new object[] { CompositeMLDsaAlgorithm.MLDsa65WithECDsaBrainpoolP256r1 };
            yield return new object[] { CompositeMLDsaAlgorithm.MLDsa65WithEd25519 };
            yield return new object[] { CompositeMLDsaAlgorithm.MLDsa87WithECDsaP384 };
            yield return new object[] { CompositeMLDsaAlgorithm.MLDsa87WithECDsaBrainpoolP384r1 };
            yield return new object[] { CompositeMLDsaAlgorithm.MLDsa87WithEd448 };
            yield return new object[] { CompositeMLDsaAlgorithm.MLDsa87WithRSA3072Pss };
            yield return new object[] { CompositeMLDsaAlgorithm.MLDsa87WithRSA4096Pss };
            yield return new object[] { CompositeMLDsaAlgorithm.MLDsa87WithECDsaP521 };
        }
    }
}
