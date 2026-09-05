// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    public static class HpkeSuiteTests
    {
        [Theory]
        [MemberData(nameof(ValidAlgorithms))]
        public static void Constructor_ValidAlgorithms(HpkeKem kem, HpkeKdf kdf, HpkeAead aead)
        {
            HpkeSuite suite = new(kem, kdf, aead);

            Assert.Equal(kem, suite.KemAlgorithm);
            Assert.Equal(kdf, suite.KdfAlgorithm);
            Assert.Equal(aead, suite.AeadAlgorithm);
        }

        [Theory]
        [InlineData(int.MinValue)]
        [InlineData(-7)]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(15)]
        [InlineData(18)]
        [InlineData(31)]
        [InlineData(33)]
        [InlineData(63)]
        [InlineData(67)]
        [InlineData(79)]
        [InlineData(82)]
        [InlineData(ushort.MaxValue)]
        [InlineData(ushort.MaxValue + 1)]
        [InlineData(int.MaxValue)]
        public static void Constructor_InvalidKem(int kem)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>(
                nameof(kem),
                () => new HpkeSuite((HpkeKem)kem, HpkeKdf.HKDF_SHA256, HpkeAead.AES_128_GCM));
        }

        [Theory]
        [InlineData(int.MinValue)]
        [InlineData(-7)]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(4)]
        [InlineData(15)]
        [InlineData(18)]
        [InlineData(ushort.MaxValue)]
        [InlineData(ushort.MaxValue + 1)]
        [InlineData(int.MaxValue)]
        public static void Constructor_InvalidKdf(int kdf)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>(
                nameof(kdf),
                () => new HpkeSuite(HpkeKem.MLKEM_768, (HpkeKdf)kdf, HpkeAead.AES_128_GCM));
        }

        [Theory]
        [InlineData(int.MinValue)]
        [InlineData(-7)]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(4)]
        [InlineData(ushort.MaxValue)]
        [InlineData(ushort.MaxValue + 1)]
        [InlineData(int.MaxValue)]
        public static void Constructor_InvalidAead(int aead)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>(
                nameof(aead),
                () => new HpkeSuite(HpkeKem.MLKEM_768, HpkeKdf.HKDF_SHA256, (HpkeAead)aead));
        }

        [Theory]
        [MemberData(nameof(ValidAlgorithms))]
        public static void Equality_SameAlgorithms(HpkeKem kem, HpkeKdf kdf, HpkeAead aead)
        {
            HpkeSuite left = new(kem, kdf, aead);
            HpkeSuite right = new(kem, kdf, aead);

            AssertExtensions.TrueExpression(left.Equals(left));
            AssertExtensions.TrueExpression(left.Equals((object)left));
            AssertExtensions.TrueExpression(left.Equals(right));
            AssertExtensions.TrueExpression(right.Equals(left));
            AssertExtensions.TrueExpression(left.Equals((object)right));
            AssertExtensions.TrueExpression(right.Equals((object)left));
            AssertExtensions.TrueExpression(((IEquatable<HpkeSuite>)left).Equals(right));
            AssertExtensions.TrueExpression(left == right);
            AssertExtensions.TrueExpression(right == left);
            AssertExtensions.FalseExpression(left != right);
            AssertExtensions.FalseExpression(right != left);
            Assert.Equal(left.GetHashCode(), right.GetHashCode());
        }

        [Theory]
        [InlineData(HpkeKem.MLKEM_1024, HpkeKdf.HKDF_SHA256, HpkeAead.AES_128_GCM)]
        [InlineData(HpkeKem.MLKEM_768, HpkeKdf.HKDF_SHA384, HpkeAead.AES_128_GCM)]
        [InlineData(HpkeKem.MLKEM_768, HpkeKdf.HKDF_SHA256, HpkeAead.AES_256_GCM)]
        [InlineData(HpkeKem.MLKEM_1024, HpkeKdf.SHAKE256, HpkeAead.ChaCha20Poly1305)]
        public static void Equality_DifferentAlgorithms(HpkeKem kem, HpkeKdf kdf, HpkeAead aead)
        {
            HpkeSuite left = new(HpkeKem.MLKEM_768, HpkeKdf.HKDF_SHA256, HpkeAead.AES_128_GCM);
            HpkeSuite right = new(kem, kdf, aead);

            AssertExtensions.FalseExpression(left.Equals(right));
            AssertExtensions.FalseExpression(right.Equals(left));
            AssertExtensions.FalseExpression(left.Equals((object)right));
            AssertExtensions.FalseExpression(right.Equals((object)left));
            AssertExtensions.FalseExpression(((IEquatable<HpkeSuite>)left).Equals(right));
            AssertExtensions.FalseExpression(left == right);
            AssertExtensions.FalseExpression(right == left);
            AssertExtensions.TrueExpression(left != right);
            AssertExtensions.TrueExpression(right != left);
        }

        [Fact]
        public static void Equality_NullAndUnrelatedObject()
        {
            HpkeSuite suite = new(HpkeKem.MLKEM_768, HpkeKdf.HKDF_SHA256, HpkeAead.AES_128_GCM);
            HpkeSuite nullSuite = null;

            AssertExtensions.FalseExpression(suite.Equals(nullSuite));
            AssertExtensions.FalseExpression(suite.Equals((object)nullSuite));
            AssertExtensions.FalseExpression(suite.Equals(new object()));
            AssertExtensions.FalseExpression(((IEquatable<HpkeSuite>)suite).Equals(nullSuite));
            AssertExtensions.FalseExpression(suite == nullSuite);
            AssertExtensions.FalseExpression(nullSuite == suite);
            AssertExtensions.TrueExpression(suite != nullSuite);
            AssertExtensions.TrueExpression(nullSuite != suite);
            AssertExtensions.TrueExpression(nullSuite == (HpkeSuite)null);
            AssertExtensions.FalseExpression(nullSuite != (HpkeSuite)null);
        }

        public static IEnumerable<object[]> ValidAlgorithms()
        {
            foreach (HpkeKem kem in Enum.GetValues(typeof(HpkeKem)))
            foreach (HpkeKdf kdf in Enum.GetValues(typeof(HpkeKdf)))
            foreach (HpkeAead aead in Enum.GetValues(typeof(HpkeAead)))
            {
                yield return new object[] { kem, kdf, aead };
            }
        }
    }
}
